using System;
using System.IO;
using System.Linq;
using RollerCoasterHeroes;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace FirstRide.Editor
{
    public static class FirstRideProject
    {
        const string ScenePath = "Assets/Scenes/FirstRide.unity";
        const string PipelinePath = "Assets/Settings/FirstRideURP.asset";
        static bool smoke, restarted, captured;
        static int errors;
        static double deadline;
        static float firstFinishTime, firstFinishScore;
        static double finishCaptureAt;
        static double pauseUntil;
        static float pausedDistance;
        static double smokeStartAt;
        static bool previousPlayModeOptionsEnabled;
        static EnterPlayModeOptions previousPlayModeOptions;

        [MenuItem("Roller Coaster Heroes/Setup First Ride 2.0")]
        public static void Setup()
        {
            Directory.CreateDirectory("Assets/Settings"); Directory.CreateDirectory("Assets/Scenes"); Directory.CreateDirectory("Assets/Resources");
            AssetDatabase.Refresh();
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, "Assets/Settings/FirstRideRenderer.asset");
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.name = "First Ride URP";
                pipeline.msaaSampleCount = 4; pipeline.shadowDistance = 85;
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }
            GraphicsSettings.defaultRenderPipeline = pipeline; QualitySettings.renderPipeline = pipeline;
            QualitySettings.vSyncCount = 0;
            // Runtime-created materials must survive player shader stripping.
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("Install URP 17.5 before setup.");
            if (AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/FirstRideLit.mat") == null)
                AssetDatabase.CreateAsset(new Material(shader), "Assets/Resources/FirstRideLit.mat");
            PlayerSettings.companyName = "YTSuper8"; PlayerSettings.productName = "Roller Coaster Heroes";
            PlayerSettings.defaultScreenWidth = 1920; PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.runInBackground = true; PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            if (!File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("First Ride 2.0").AddComponent<FirstRideBootstrap>();
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("FIRST_RIDE_SETUP_OK Unity " + Application.unityVersion);
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("First Ride validation: " + message);
        }

        [MenuItem("Roller Coaster Heroes/Validate gameplay")]
        public static void Validate()
        {
            Setup();
            var track = new RideTrack(); var model = new RideModel(track); var twin = new RideModel(track);
            Require(track.Length > 1250, "track length");
            Require(model.Pickups.Count == 70, "70 seeded pickups");
            Require(track.Position(-10) == track.Position(0) && track.Position(track.Length + 10) == track.Position(track.Length), "endpoint clamping");
            for (int i = 0; i < 70; i++) Require(model.Pickups[i].Distance == twin.Pickups[i].Distance, "seed reproducibility");
            int events = 0, finishes = 0, boosts = 0, brakes = 0;
            model.Collected += (i, k) => { events++; if (k == PickupKind.Boost) boosts++; if (k == PickupKind.Brake) brakes++; };
            model.Completed += () => finishes++;
            for (int i = 0; i < 15000 && !model.Finished; i++)
            {
                model.Step(.02f); twin.Step(.02f);
                Require(model.Speed >= RideModel.MinSpeed && model.Speed <= RideModel.MaxSpeed, "bounded speed");
                Require(model.Distance == twin.Distance && model.Coins == twin.Coins, "deterministic simulation");
            }
            Require(model.Finished && finishes == 1, "automatic finish once");
            Require(events == 70 && boosts == 5 && brakes == 3 && model.Coins == 62, "all pickups exactly once");
            Require(Mathf.Abs(model.Score - (Mathf.Max(0, 10000 - 50 * model.Seconds) + 6200)) < .01f, "original score formula");
            float finishSeconds = model.Seconds, score = model.Score;
            model.Step(100); Require(model.Seconds == finishSeconds && model.Score == score && events == 70, "frozen result");
            model.Reset(); Require(model.Distance == 0 && model.Coins == 0 && model.Speed == 13 && !model.Finished && model.Pickups.All(p => !p.Collected), "full restart");
            // Large steps must not skip crossing pickups.
            model.Step(200); Require(model.Finished && model.Coins == 62 && finishes == 2, "swept collection on long step");
            Directory.CreateDirectory("TestResults");
            File.WriteAllText("TestResults/gameplay.txt", "PASS Unity " + Application.unityVersion + "\n70 pickups: 62 coins, 5 boosts, 3 brakes\n" +
                "Determinism, bounds, swept collection, finish, frozen score, restart: PASS\n" +
                "Fixed-step finish: " + finishSeconds.ToString("F3") + "s; score: " + score.ToString("F2") + "\n");
            Debug.Log("FIRST_RIDE_VALIDATION_OK " + finishSeconds + "s / " + score + " points");
        }

        public static void PlaySmoke()
        {
            try
            {
                Validate();
                EditorSceneManager.OpenScene(ScenePath);
                // Allow editor startup/import callbacks to settle before measuring Play Mode.
                smokeStartAt = EditorApplication.timeSinceStartup + 2;
                EditorApplication.update += WaitForSmokeStart;
            }
            catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        }
        static void WaitForSmokeStart()
        {
            if (EditorApplication.timeSinceStartup < smokeStartAt || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            EditorApplication.update -= WaitForSmokeStart;
            try
            {
                smoke = true; restarted = captured = false; errors = 0; finishCaptureAt = pauseUntil = 0; deadline = EditorApplication.timeSinceStartup + 180;
                Application.logMessageReceived += OnLog;
                EditorApplication.update += SmokeTick;
                previousPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
                previousPlayModeOptions = EditorSettings.enterPlayModeOptions;
                EditorSettings.enterPlayModeOptionsEnabled = true;
                EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
                EditorApplication.isPlaying = true;
            }
            catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        }
        static void OnLog(string message, string trace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++;
        }
        static void SmokeTick()
        {
            if (!smoke) return;
            try
            {
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("PlayMode smoke timed out.");
                if (!EditorApplication.isPlaying) return;
                var game = UnityEngine.Object.FindFirstObjectByType<FirstRideBootstrap>();
                if (game == null || game.Model == null) return;
                Time.timeScale = 8;
                Require(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(x => x.enabled) == 1, "one active listener");
                Require(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(x => x.enabled) == 1, "one active camera");
                Require(game.Hud.Canvas != null && game.Director.Camera != null, "camera and HUD initialized");
                if (pauseUntil > 0)
                {
                    Require(game.Model.Distance == pausedDistance, "simulation stays paused across frames");
                    if (EditorApplication.timeSinceStartup < pauseUntil) return;
                    game.TogglePause(); Require(!game.Paused && !AudioListener.pause, "resume audio");
                    pauseUntil = 0;
                }
                if (!captured && game.Model.Seconds > 10)
                {
                    Capture(game, "TestResults/first-ride-2.0.png"); captured = true;
                    bool wasWide = game.Director.Wide, wasMuted = game.MusicMuted;
                    game.ToggleCamera(); Require(game.Director.Wide != wasWide, "camera mode toggle");
                    game.ToggleMusic(); Require(game.MusicMuted != wasMuted, "music mute");
                    game.ToggleMusic(); Require(game.MusicMuted == wasMuted, "music unmute");
                    game.TogglePause(); Require(game.Paused && AudioListener.pause, "pause audio");
                    pausedDistance = game.Model.Distance; pauseUntil = EditorApplication.timeSinceStartup + .25;
                    return;
                }
                if (!game.Model.Finished) return;
                if (finishCaptureAt == 0) finishCaptureAt = EditorApplication.timeSinceStartup + 2;
                if (EditorApplication.timeSinceStartup < finishCaptureAt) return;
                Require(game.Model.Coins == 62, "PlayMode collected all coins");
                Require(game.Hud.Canvas.transform.Find("Finish results").gameObject.activeSelf, "finish results visible");
                if (!restarted)
                {
                    Capture(game, "TestResults/first-ride-finish.png");
                    firstFinishTime = game.Model.Seconds; firstFinishScore = game.Model.Score;
                    game.Restart(); Require(game.Model.Coins == 0 && !game.Model.Finished, "PlayMode restart");
                    restarted = true; finishCaptureAt = 0; return;
                }
                Require(Mathf.Abs(game.Model.Seconds - firstFinishTime) < .001f && game.Model.Score == firstFinishScore, "matching replay results");
                Require(errors == 0, "no runtime errors");
                File.WriteAllText("TestResults/playmode.txt", "PASS Unity " + Application.unityVersion +
                    "\nTwo complete actual PlayMode rides, restart, camera mode, music toggle, pause/resume, HUD, listeners.\n" +
                    "Coins: 62; finish: " + firstFinishTime.ToString("F3") + "s; score: " + firstFinishScore.ToString("F2") + "; runtime errors: " + errors);
                Debug.Log("FIRST_RIDE_PLAYMODE_OK"); FinishSmoke(0);
            }
            catch (Exception ex) { Debug.LogException(ex); FinishSmoke(1); }
        }
        static void Capture(FirstRideBootstrap game, string path)
        {
            var camera = game.Director.Camera; var canvas = game.Hud.Canvas;
            var rt = new RenderTexture(1920, 1080, 24); var texture = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            camera.targetTexture = rt; Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = rt;
            texture.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = previous; canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            UnityEngine.Object.DestroyImmediate(texture); rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
        }
        static void FinishSmoke(int code)
        {
            smoke = false; Time.timeScale = 1; Application.logMessageReceived -= OnLog; EditorApplication.update -= SmokeTick;
            EditorSettings.enterPlayModeOptionsEnabled = previousPlayModeOptionsEnabled;
            EditorSettings.enterPlayModeOptions = previousPlayModeOptions;
            EditorApplication.isPlaying = false; EditorApplication.Exit(code);
        }

        [MenuItem("Roller Coaster Heroes/Build Windows x64")]
        public static void BuildWindows()
        {
            Setup();
            var report = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, "Builds/Windows/RollerCoasterHeroes.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Windows build failed: " + report.summary.result);
            Debug.Log("FIRST_RIDE_BUILD_OK");
        }
    }
}
