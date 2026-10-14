using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RollerCoasterHeroes
{
    [DefaultExecutionOrder(-100)]
    public sealed class FirstRideBootstrap : MonoBehaviour
    {
        public RideModel Model { get; private set; }
        public RideCamera Director { get; private set; }
        public RideHud Hud { get; private set; }
        public bool Paused { get; private set; }
        public bool MusicMuted => audio != null && audio.Muted;
        RideArt art;
        HeroCart cart;
        RideAudio audio;
        readonly List<Transform> pickupViews = new List<Transform>();
        readonly List<Transform> sparks = new List<Transform>();
        readonly Vector3[] sparkVelocity = new Vector3[24];
        float burstLife, previousDistance, renderDistance, visualTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Initialize()
        {
            if (FindFirstObjectByType<FirstRideBootstrap>() == null)
                new GameObject("First Ride 2.0").AddComponent<FirstRideBootstrap>();
        }

        void Start()
        {
            Application.targetFrameRate = 60;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.68f, .81f, 1);
            RenderSettings.ambientEquatorColor = new Color(.62f, .72f, .72f);
            RenderSettings.ambientGroundColor = new Color(.33f, .4f, .28f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(.53f, .80f, .96f); RenderSettings.fogStartDistance = 160; RenderSettings.fogEndDistance = 750;
            foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None)) light.enabled = false;
            var sun = new GameObject("Green Hills sun"); sun.transform.SetParent(transform); sun.transform.rotation = Quaternion.Euler(48, -25, 0);
            var sunlight = sun.AddComponent<Light>(); sunlight.type = LightType.Directional; sunlight.intensity = 1.5f;
            sunlight.shadows = LightShadows.Soft;
            art = new RideArt(transform);
            Model = new RideModel(new RideTrack());
            art.BuildTrack(Model.Track); art.BuildPark(Model.Track);
            cart = new HeroCart(art); Director = new RideCamera(transform);
            CreatePickups();
            for (int i = 0; i < sparkVelocity.Length; i++)
            {
                var spark = art.Part("Pooled celebration spark", PrimitiveType.Sphere, transform, Vector3.zero, Vector3.one * .17f, RideArt.Gold);
                spark.gameObject.SetActive(false); sparks.Add(spark);
            }
            audio = new RideAudio(gameObject); Hud = new RideHud(transform);
            Model.Collected += OnCollected;
            Model.Completed += OnCompleted;
            Restart();
        }

        void CreatePickups()
        {
            foreach (var pickup in Model.Pickups)
            {
                var view = art.Group(pickup.Kind.ToString());
                view.position = Model.Track.Position(pickup.Distance) + Model.Track.Rotation(pickup.Distance) * Vector3.up * 2.1f;
                view.rotation = Model.Track.Rotation(pickup.Distance);
                if (pickup.Kind == PickupKind.Coin)
                {
                    var disc = art.Part("Gold coin rim", PrimitiveType.Cylinder, view, Vector3.zero, new Vector3(.92f, .09f, .92f), RideArt.Gold);
                    disc.localRotation = Quaternion.Euler(90, 0, 0);
                    for (int s = -1; s <= 1; s += 2)
                    {
                        var face = art.Part("Coin inset", PrimitiveType.Cylinder, view, new Vector3(0, 0, s * .1f), new Vector3(.72f, .012f, .72f), new Color(1, .88f, .29f));
                        face.localRotation = Quaternion.Euler(90, 0, 0);
                        var star = art.Part("Embossed diamond", PrimitiveType.Cube, view, new Vector3(0, 0, s * .125f), new Vector3(.29f, .29f, .045f), RideArt.Gold);
                        star.localRotation = Quaternion.Euler(0, 0, 45);
                    }
                }
                else
                {
                    bool boost = pickup.Kind == PickupKind.Boost;
                    Color color = boost ? RideArt.Teal : RideArt.Coral;
                    var plate = art.Part(boost ? "Turbo badge" : "Brake badge", PrimitiveType.Cube, view, Vector3.zero, new Vector3(1.4f, 1.4f, .2f), color);
                    plate.localRotation = Quaternion.Euler(0, 0, 45);
                    if (boost)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            var bolt = art.Part("Lightning bolt", PrimitiveType.Cube, view, new Vector3(j == 1 ? 0 : j == 0 ? .1f : -.1f, .35f - j * .34f, -.16f), new Vector3(.24f, .55f, .08f), Color.white);
                            bolt.localRotation = Quaternion.Euler(0, 0, j == 1 ? 70 : -25);
                        }
                    }
                    else for (int s = -1; s <= 1; s += 2)
                        art.Part("Brake bars", PrimitiveType.Cube, view, new Vector3(s * .23f, 0, -.16f), new Vector3(.20f, .85f, .08f), Color.white);
                }
                pickupViews.Add(view);
            }
        }

        void OnCollected(int index, PickupKind kind)
        {
            Vector3 origin = pickupViews[index].position;
            pickupViews[index].gameObject.SetActive(false); audio.Play(kind); cart.React();
            Hud.Announce(kind == PickupKind.Coin ? "+100  COIN COLLECTED!" : kind == PickupKind.Boost ? "SPEED BOOST!" : "SLOW DOWN!",
                kind == PickupKind.Coin ? RideArt.Gold : kind == PickupKind.Boost ? RideArt.Teal : RideArt.Coral);
            Burst(origin);
        }
        void OnCompleted() { audio.Finish(); Burst(Model.Track.Position(Model.Distance) + Vector3.up * 3); }
        void Burst(Vector3 origin)
        {
            burstLife = 1;
            for (int i = 0; i < sparks.Count; i++)
            {
                float angle = i * Mathf.PI * 2 / sparks.Count;
                sparks[i].position = origin; sparks[i].gameObject.SetActive(true);
                sparkVelocity[i] = new Vector3(Mathf.Cos(angle) * 3, 2 + i % 4, Mathf.Sin(angle) * 3);
            }
        }
        public void Restart()
        {
            AudioListener.pause = false;
            Model.Reset(); Paused = false; previousDistance = renderDistance = visualTime = burstLife = 0;
            foreach (var view in pickupViews) view.gameObject.SetActive(true);
            foreach (var spark in sparks) spark.gameObject.SetActive(false);
            cart.Reset(); Hud.Reset(); audio.Reset(); Director.Reset(Model); PlaceCart(0);
        }
        public void TogglePause() { Paused = !Paused; AudioListener.pause = Paused; }
        public void ToggleCamera() { Director.Toggle(); }
        public void ToggleMusic() { audio.ToggleMusic(); }

        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.rKey.wasPressedThisFrame) { Restart(); AudioListener.pause = false; }
                if (keyboard.mKey.wasPressedThisFrame) ToggleMusic();
                if (keyboard.cKey.wasPressedThisFrame) ToggleCamera();
                if (keyboard.spaceKey.wasPressedThisFrame) TogglePause();
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.R)) { Restart(); AudioListener.pause = false; }
            if (Input.GetKeyDown(KeyCode.M)) ToggleMusic();
            if (Input.GetKeyDown(KeyCode.C)) ToggleCamera();
            if (Input.GetKeyDown(KeyCode.Space)) TogglePause();
#endif
            float dt = Paused ? 0 : Time.deltaTime;
            visualTime += dt;
            for (int i = 0; i < pickupViews.Count; i++)
            {
                if (Model.Pickups[i].Collected) continue;
                var p = Model.Pickups[i]; var frame = Model.Track.Rotation(p.Distance);
                pickupViews[i].position = Model.Track.Position(p.Distance) + frame * Vector3.up * (2.1f + Mathf.Sin(visualTime * 3 + i) * .13f);
                pickupViews[i].rotation = frame * Quaternion.Euler(0, p.Kind == PickupKind.Coin ? visualTime * 100 : Mathf.Sin(visualTime * 2) * 15, 0);
            }
            burstLife -= dt;
            for (int i = 0; i < sparks.Count; i++)
            {
                if (!sparks[i].gameObject.activeSelf) continue;
                sparks[i].gameObject.SetActive(burstLife > 0);
                sparkVelocity[i] += Vector3.down * 8 * dt; sparks[i].position += sparkVelocity[i] * dt;
                sparks[i].localScale = Vector3.one * Mathf.Max(0, burstLife) * .17f;
            }
            audio.Tick(Paused ? 0 : Model.Speed, Model.Finished);
            Hud.Tick(Model, audio, Director.Wide, Paused, dt);
        }
        void FixedUpdate()
        {
            if (Model == null || Paused) return;
            previousDistance = Model.Distance; Model.Step(Time.fixedDeltaTime);
        }
        void PlaceCart(float distance)
        {
            cart.Root.position = Model.Track.Position(distance); cart.Root.rotation = Model.Track.Rotation(distance);
        }
        void LateUpdate()
        {
            if (Model == null) return;
            float dt = Paused ? 0 : Time.deltaTime;
            float alpha = Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime);
            if (!Paused) renderDistance = Model.Finished ? Model.Distance : Mathf.Lerp(previousDistance, Model.Distance, alpha);
            PlaceCart(renderDistance);
            cart.Animate(dt, Model.Finished || Paused ? 0 : Model.Speed, Model.Finished, Model.Effect, Model.EffectSeconds, visualTime);
            Director.Tick(Model, dt, false, renderDistance);
        }
        void OnDestroy()
        {
            AudioListener.pause = false;
            art?.Dispose(); audio?.Dispose();
        }
    }
}
