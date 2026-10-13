using System.Collections.Generic;
using UnityEngine;

// Proof-of-concept, not a released/validated Unity scene.
public class FirstRideBootstrap : MonoBehaviour
{
    private const int Samples = 360;
    private readonly List<Vector3> points = new List<Vector3>();
    private readonly List<Pickup> pickups = new List<Pickup>();
    private Transform cart;
    private Camera followCamera;
    private AudioSource music;
    private AudioSource sfx;
    private float distance;
    private float speed;
    private float seconds;
    private int coins;
    private bool finished;
    private bool muted;
    private readonly List<float> segmentStarts = new List<float>();
    private float length;

    private class Pickup
    {
        public GameObject obj;
        public float at;
        public int kind; // 0 coin, 1 boost, 2 slowdown
        public bool used;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (FindObjectOfType<FirstRideBootstrap>() == null)
            new GameObject("FirstRideGame").AddComponent<FirstRideBootstrap>();
    }

    private void Start()
    {
        Application.targetFrameRate = 60;
        RenderSettings.ambientLight = new Color(0.75f, 0.79f, 0.92f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.58f, 0.79f, 0.99f);
        RenderSettings.fogDensity = 0.002f;
        CreateTrack();
        CreateCart();
        CreatePickups();
        CreateLighting();
        CreateAudio();
        Restart();
    }

    // Smooth roller-coaster curve with drops, turns and hills.
    private Vector3 TrackPoint(float t)
    {
        return new Vector3(110f * t - 55f,
            14f + 8f * Mathf.Sin(t * Mathf.PI * 5f) + 5f * Mathf.Sin(t * Mathf.PI * 12f),
            9f * Mathf.Sin(t * Mathf.PI * 3.2f) + 4f * Mathf.Sin(t * Mathf.PI * 8f));
    }

    private static Material Mat(Color c)
    {
        // Built-in Standard works in Built-in, and URP falls back to its default when needed.
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        Material m = new Material(shader);
        m.color = c;
        return m;
    }

    private GameObject Primitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
    {
        GameObject o = GameObject.CreatePrimitive(type);
        o.name = name;
        o.transform.position = position;
        o.transform.localScale = scale;
        o.GetComponent<Renderer>().material = Mat(color);
        return o;
    }

    private void CreateTrack()
    {
        length = 0;
        for (int i = 0; i <= Samples; i++)
        {
            Vector3 p = TrackPoint(i / (float)Samples);
            if (i > 0) length += Vector3.Distance(points[i - 1], p);
            segmentStarts.Add(length);
            points.Add(p);
        }
        GameObject rail = new GameObject("Red coaster rail");
        LineRenderer line = rail.AddComponent<LineRenderer>();
        line.positionCount = points.Count;
        line.SetPositions(points.ToArray());
        line.startWidth = line.endWidth = 1.1f;
        line.material = Mat(new Color(0.95f, 0.13f, 0.12f));
        line.useWorldSpace = true;
        Primitive("Green ground", PrimitiveType.Cube, new Vector3(0, -3, 0),
            new Vector3(160, 1, 100), new Color(0.26f, 0.72f, 0.35f));

        for (int i = 0; i < points.Count; i += 18)
        {
            Vector3 p = points[i];
            Primitive("Rail support", PrimitiveType.Cylinder,
                new Vector3(p.x, (p.y - 2.5f) / 2f, p.z),
                new Vector3(0.22f, Mathf.Max(0.1f, (p.y + 2.5f) / 2f), 0.22f),
                new Color(0.17f, 0.43f, 0.7f));
        }
    }

    private Vector3 OnTrack(float d)
    {
        int index = 0;
        while (index < segmentStarts.Count - 2 && segmentStarts[index + 1] < d) index++;
        float den = Mathf.Max(0.0001f, segmentStarts[index + 1] - segmentStarts[index]);
        return Vector3.Lerp(points[index], points[index + 1],
            Mathf.Clamp01((d - segmentStarts[index]) / den));
    }

    private void CreateCart()
    {
        cart = new GameObject("Hero cart").transform;
        GameObject body = Primitive("Cart body", PrimitiveType.Cube, Vector3.zero,
            new Vector3(2.2f, 0.85f, 1.7f), new Color(0.96f, 0.2f, 0.15f));
        body.transform.SetParent(cart, false);
        body.transform.localPosition = new Vector3(0, 0.65f, 0);
        GameObject hero = Primitive("Placeholder hero", PrimitiveType.Sphere, Vector3.zero,
            new Vector3(0.8f, 1.1f, 0.8f), new Color(1f, 0.65f, 0.13f));
        hero.transform.SetParent(cart, false);
        hero.transform.localPosition = new Vector3(0, 1.55f, 0);
        for (int s = -1; s <= 1; s += 2)
        {
            GameObject ear = Primitive("Fox ear", PrimitiveType.Capsule, Vector3.zero,
                new Vector3(0.27f, 0.46f, 0.25f), new Color(1f, 0.56f, 0.1f));
            ear.transform.SetParent(cart, false);
            ear.transform.localPosition = new Vector3(s * 0.26f, 2.35f, 0);
        }
        GameObject cam = new GameObject("First Ride Camera");
        followCamera = cam.AddComponent<Camera>();
        followCamera.tag = "MainCamera";
        followCamera.clearFlags = CameraClearFlags.Skybox;
        followCamera.fieldOfView = 67;
        AudioListener listener = cam.AddComponent<AudioListener>();
        // Only one listener is needed even if a scene already has a default camera.
        foreach (AudioListener other in FindObjectsOfType<AudioListener>())
            if (other != listener) other.enabled = false;
    }

    private void CreatePickups()
    {
        System.Random rng = new System.Random(20261013);
        for (int i = 0; i < 70; i++)
        {
            float at = length * (0.06f + 0.88f * ((i + 0.5f) / 70f));
            int kind = (i % 17 == 0) ? 1 : (i % 23 == 0 ? 2 : 0);
            Vector3 p = OnTrack(at) + Vector3.up * 2.0f;
            GameObject o = Primitive(kind == 0 ? "Coin" : kind == 1 ? "Boost" : "Slow",
                kind == 0 ? PrimitiveType.Sphere : PrimitiveType.Cube,
                p, kind == 0 ? Vector3.one * 0.8f : Vector3.one * 1.15f,
                kind == 0 ? new Color(1f, 0.83f, 0.06f) :
                kind == 1 ? Color.green : Color.red);
            pickups.Add(new Pickup { obj = o, at = at + (float)rng.NextDouble() * 0.08f, kind = kind });
        }
    }

    private void CreateLighting()
    {
        GameObject sun = new GameObject("Sun");
        Light light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.3f;
        sun.transform.rotation = Quaternion.Euler(48f, -25f, 0);
    }

    private void CreateAudio()
    {
        music = gameObject.AddComponent<AudioSource>();
        music.loop = true;
        music.volume = 0.10f;
        music.clip = Tune();
        music.Play();
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.volume = 0.3f;
    }

    private AudioClip Tune()
    {
        // Generated original cheerful eight-second loop; no external audio files.
        const int hz = 22050;
        const int secondsPerLoop = 8;
        float[] buffer = new float[hz * secondsPerLoop];
        int[] notes = { 60, 64, 67, 72, 67, 64, 62, 67, 60, 64, 69, 72, 71, 67, 64, 60 };
        for (int i = 0; i < buffer.Length; i++)
        {
            float t = i / (float)hz;
            int index = Mathf.Min(notes.Length - 1, Mathf.FloorToInt(t * 2));
            float frequency = 440f * Mathf.Pow(2, (notes[index] - 69) / 12f);
            float noteTime = (t * 2) % 1f;
            float env = Mathf.Pow(1f - noteTime, 1.5f);
            buffer[i] = 0.22f * env * (Mathf.Sin(2 * Mathf.PI * frequency * t) +
                0.25f * Mathf.Sin(4 * Mathf.PI * frequency * t));
        }
        AudioClip clip = AudioClip.Create("Cheerful demo tune", buffer.Length, 1, hz, false);
        clip.SetData(buffer, 0);
        return clip;
    }

    private void Restart()
    {
        distance = 0;
        speed = 13f;
        seconds = 0;
        coins = 0;
        finished = false;
        foreach (Pickup p in pickups) { p.used = false; p.obj.SetActive(true); }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) Restart();
        if (Input.GetKeyDown(KeyCode.M)) { muted = !muted; music.mute = muted; }
        foreach (Pickup p in pickups)
            if (!p.used && p.obj.activeSelf) p.obj.transform.Rotate(0, 110f * Time.deltaTime, 0);
    }

    private void FixedUpdate()
    {
        if (finished) return;
        float dt = Time.fixedDeltaTime;
        Vector3 current = OnTrack(distance);
        Vector3 ahead = OnTrack(Mathf.Min(length, distance + 0.5f));
        float slope = (ahead.y - current.y) / Mathf.Max(0.5f, Vector3.Distance(current, ahead));
        float accel = 4f - slope * 12f - speed * 0.07f;
        speed = Mathf.Clamp(speed + accel * dt, 7f, 37f);
        distance = Mathf.Min(length, distance + speed * dt);
        seconds += dt;
        foreach (Pickup p in pickups)
        {
            if (p.used || distance < p.at) continue;
            p.used = true;
            p.obj.SetActive(false);
            if (p.kind == 0) { coins++; sfx.PlayOneShot(music.clip, 0.035f); }
            if (p.kind == 1) speed = Mathf.Min(37f, speed * 1.4f);
            if (p.kind == 2) speed = Mathf.Max(7f, speed * 0.7f);
        }
        if (distance >= length) finished = true;
    }

    private void LateUpdate()
    {
        if (cart == null) return;
        Vector3 at = OnTrack(distance);
        Vector3 direction = (OnTrack(Mathf.Min(length, distance + 0.8f)) -
            OnTrack(Mathf.Max(0, distance - 0.8f))).normalized;
        if (direction.sqrMagnitude < 0.1f) direction = Vector3.right;
        cart.position = at;
        cart.rotation = Quaternion.LookRotation(direction, Vector3.up);
        Vector3 desiredCamera = at - direction * 8f + Vector3.up * 4f;
        followCamera.transform.position = Vector3.Lerp(followCamera.transform.position,
            desiredCamera, Mathf.Clamp01(Time.deltaTime * 5f));
        followCamera.transform.LookAt(at + Vector3.up * 1.4f);
    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 23;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.white;
        GUI.Box(new Rect(12, 12, 400, 153), "");
        GUI.Label(new Rect(25, 20, 450, 37), "ROLLER COASTER HEROES", style);
        GUI.Label(new Rect(25, 60, 450, 37), "TIME  " + seconds.ToString("F1") +
            "     SPEED  " + speed.ToString("F0"), style);
        GUI.Label(new Rect(25, 98, 450, 37), "COINS  " + coins +
            "     SCORE  " + (Mathf.Max(0, 10000 - 50 * seconds) + 100 * coins).ToString("F0"), style);
        if (finished) GUI.Label(new Rect(25, 140, 450, 40), "FINISH! PRESS R TO RESTART", style);
        GUI.Label(new Rect(25, Screen.height - 40, 480, 35), "R: RESTART   M: MUTE MUSIC", style);
    }
}
