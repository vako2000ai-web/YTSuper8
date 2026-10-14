using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RollerCoasterHeroes
{
    public sealed class RideArt
    {
        readonly Transform root;
        readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        readonly List<Mesh> meshes = new List<Mesh>();
        public static readonly Color Coral = new Color(1, .19f, .17f);
        public static readonly Color Navy = new Color(.055f, .11f, .20f);
        public static readonly Color Gold = new Color(1, .72f, .08f);
        public static readonly Color Teal = new Color(.07f, .78f, .69f);
        public RideArt(Transform root) { this.root = root; }

        public Material Material(Color color)
        {
            if (materials.TryGetValue(color, out var value)) return value;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new System.InvalidOperationException("URP/Lit missing. Run Roller Coaster Heroes > Setup First Ride 2.0.");
            value = new Material(shader) { color = color, name = "RCH " + ColorUtility.ToHtmlStringRGB(color), enableInstancing = true };
            value.SetFloat("_Smoothness", .32f);
            materials.Add(color, value);
            return value;
        }

        public Transform Part(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent == null ? root : parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            Object.Destroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = Material(color);
            return go.transform;
        }

        public Transform Group(string name, Transform parent = null)
        {
            var value = new GameObject(name).transform;
            value.SetParent(parent == null ? root : parent, false);
            return value;
        }

        public void Beam(Transform parent, Vector3 a, Vector3 b, float width, Color color)
        {
            var beam = Part("Structural beam", PrimitiveType.Cube, parent, (a + b) * .5f,
                new Vector3(width, width, Vector3.Distance(a, b)), color);
            beam.rotation = Quaternion.LookRotation(b - a);
        }

        public void BuildTrack(RideTrack track)
        {
            var structure = Group("Sleepers and trestles");
            for (float d = 0; d <= track.Length; d += 2.6f)
            {
                var sleeper = Part("Sleeper", PrimitiveType.Cube, structure, track.Position(d) - Vector3.up * .16f,
                    new Vector3(2.15f, .19f, .34f), new Color(.20f, .29f, .39f));
                sleeper.rotation = track.Rotation(d);
            }
            for (float d = 0; d <= track.Length; d += 16)
            {
                var p = track.Position(d) - Vector3.up * .4f;
                var right = track.Rotation(d) * Vector3.right;
                for (int side = -1; side <= 1; side += 2)
                {
                    var top = p + right * side * 1.0f;
                    var bottom = new Vector3(top.x + side * .9f, -2, top.z);
                    Beam(structure, bottom, top, .32f, Teal * .75f);
                    Part("Concrete footing", PrimitiveType.Cube, structure, bottom, new Vector3(1.6f, .65f, 1.6f), new Color(.75f, .79f, .76f));
                    Beam(structure, bottom, p - right * side * .8f, .15f, Teal * .8f);
                }
                Beam(structure, p - right * 1.25f, p + right * 1.25f, .35f, Teal);
            }
            Combine(structure);
            for (int side = -1; side <= 1; side += 2)
            {
                var vertices = new List<Vector3>();
                var triangles = new List<int>();
                const int rings = 8;
                for (int i = 0; i <= RideTrack.Samples; i++)
                {
                    float d = track.Length * i / RideTrack.Samples;
                    var rotation = track.Rotation(d);
                    Vector3 center = track.Position(d) + rotation * Vector3.right * side * .72f;
                    for (int j = 0; j < rings; j++)
                    {
                        float angle = j * Mathf.PI * 2 / rings;
                        vertices.Add(center + rotation * new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * .115f);
                        if (i == 0) continue;
                        int a = (i - 1) * rings + j, b = (i - 1) * rings + (j + 1) % rings;
                        triangles.AddRange(new[] { a, b, a + rings, b, b + rings, a + rings });
                    }
                }
                var mesh = new Mesh { name = "Continuous tubular rail" };
                mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
                meshes.Add(mesh);
                var rail = Group(side < 0 ? "Left rail" : "Right rail");
                rail.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                rail.gameObject.AddComponent<MeshRenderer>().sharedMaterial = Material(Coral);
            }
            Arch(track, 0, "START", Teal);
            Arch(track, track.Length, "FINISH", Gold);
        }

        void Arch(RideTrack track, float d, string title, Color color)
        {
            var gate = Group(title + " gantry"); gate.position = track.Position(d); gate.rotation = track.Rotation(d);
            for (int s = -1; s <= 1; s += 2)
                Part("Post", PrimitiveType.Cube, gate, new Vector3(s * 2.8f, 2, 0), new Vector3(.28f, 5, .28f), color);
            Part("Header", PrimitiveType.Cube, gate, new Vector3(0, 4.5f, 0), new Vector3(6.3f, 1.1f, .35f), Navy);
            var text = Group(title, gate); text.localPosition = new Vector3(0, 4.5f, -.22f);
            text.localRotation = Quaternion.Euler(0, 180, 0);
            var label = text.gameObject.AddComponent<TextMesh>(); label.text = title; label.fontSize = 72; label.characterSize = .10f;
            label.anchor = TextAnchor.MiddleCenter; label.color = color;
        }

        public void BuildPark(RideTrack track)
        {
            Part("Meadow", PrimitiveType.Cube, null, new Vector3(0, -3, 0), new Vector3(1700, 2, 750), new Color(.31f, .67f, .36f));
            var park = Group("Green Hills park");
            var random = new System.Random(88);
            for (int i = 0; i < 210; i++)
            {
                float d = (float)random.NextDouble() * track.Length;
                Vector3 p = track.Position(d);
                p += track.Rotation(d) * Vector3.right * (i % 2 == 0 ? -1 : 1) * (12 + (float)random.NextDouble() * 70);
                p.y = -2;
                float h = 3 + (float)random.NextDouble() * 6;
                Part("Trunk", PrimitiveType.Cylinder, park, p + Vector3.up * h * .25f, new Vector3(.55f, h * .25f, .55f), new Color(.42f, .24f, .13f));
                Part("Tree crown", PrimitiveType.Sphere, park, p + Vector3.up * h * .8f, new Vector3(h * .8f, h, h * .8f),
                    i % 3 == 0 ? new Color(.16f, .49f, .30f) : new Color(.39f, .76f, .28f));
            }
            for (int i = 0; i < 32; i++)
            {
                Vector3 p = track.Position(track.Length * i / 31);
                p.z += i % 2 == 0 ? 150 : -155; p.y = -16;
                Part("Rolling hill", PrimitiveType.Sphere, park, p, new Vector3(100, 50 + i % 4 * 12, 90), new Color(.35f, .66f, .40f));
            }
            Combine(park);
        }

        // Group static primitives by material, then combine in world-space. Dynamic art stays articulated.
        void Combine(Transform group)
        {
            var batches = new Dictionary<Material, List<CombineInstance>>();
            var filters = group.GetComponentsInChildren<MeshFilter>();
            foreach (var f in filters)
            {
                var mat = f.GetComponent<Renderer>().sharedMaterial;
                if (!batches.ContainsKey(mat)) batches.Add(mat, new List<CombineInstance>());
                batches[mat].Add(new CombineInstance { mesh = f.sharedMesh, transform = group.worldToLocalMatrix * f.transform.localToWorldMatrix });
                f.gameObject.SetActive(false);
                Object.Destroy(f.gameObject);
            }
            foreach (var batch in batches)
            {
                var mesh = new Mesh { name = "Batched park structure", indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(batch.Value.ToArray(), true, true); meshes.Add(mesh);
                var child = Group("Static batch", group);
                child.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                child.gameObject.AddComponent<MeshRenderer>().sharedMaterial = batch.Key;
            }
        }

        public void Dispose()
        {
            foreach (var material in materials.Values) Object.Destroy(material);
            foreach (var mesh in meshes) Object.Destroy(mesh);
        }
    }
}
