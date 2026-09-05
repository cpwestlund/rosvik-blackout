#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UScene = UnityEngine.SceneManagement.Scene;

namespace Rosvik.Blackout.EditorTools {
    /// <summary>
    /// V34 keeps V33 controls/gameplay/geography intact and gives the surrounding Rosvik
    /// housing stock a readable Swedish small-town facade language: framed windows, sills,
    /// front doors, modest canopies/steps, foundation bands, drainpipes and occasional
    /// chimneys. All placement is derived from the existing OSM footprints; roofs are not
    /// rebuilt in this pass, so the stable world silhouette remains untouched.
    /// </summary>
    [InitializeOnLoad]
    public static class RosvikHouseDetailV34 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_HOUSE_DETAIL_V34_VERSION";
        const int Version = 34;
        const string RootName = "ROSVIK_HOUSE_DETAIL_V34";
        const string GroupName = "16 ROSVIK HOUSE DETAIL V34";
        const string GeneratedDir = "Assets/Rosvik/GeneratedV34";
        const string ProvenMaterial = "Assets/Rosvik/GeneratedV20/mat_ground.mat";
        const long MainSchoolWay = 163199458;
        const long OldSchoolWay = 163199461;
        const long ArenaWay = 163199464;

        static RosvikHouseDetailV34() {
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        [MenuItem("Rosvik/Rebuild House Detail V34")]
        public static void Force() {
            EditorPrefs.DeleteKey(Key);
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        static bool Busy() => EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating;

        static void TryApply() {
            if (EditorPrefs.GetInt(Key, 0) >= Version && File.Exists(ScenePath)) {
                EditorApplication.update -= TryApply;
                return;
            }
            if (Busy() || !File.Exists(ScenePath)) return;

            UScene scene = EditorSceneManager.GetActiveScene();
            GameObject root = FindRoot();
            if (!root) {
                if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                if (Busy()) return;
                root = FindRoot();
            }
            if (!root || Busy()) return;

            EditorApplication.update -= TryApply;
            Apply(scene, root);
        }

        static GameObject FindRoot() {
            return GameObject.Find("ROSVIK_CHARACTER_FEEL_V33")
                ?? GameObject.Find("ROSVIK_PLAY_COMFORT_V32")
                ?? GameObject.Find("ROSVIK_CHARACTER_ATMOSPHERE_V31")
                ?? GameObject.Find(RootName);
        }

        static void Apply(UScene scene, GameObject root) {
            try {
                Directory.CreateDirectory(GeneratedDir);
                AssetDatabase.Refresh();

                Transform old = Find(root.transform, GroupName);
                if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
                Transform group = NewGroup(root.transform, GroupName);

                List<RosvikOsmV15.Way> ways = RosvikOsmV15.LoadWays();
                if (ways == null || ways.Count == 0) throw new InvalidOperationException("V34 could not load Rosvik OSM data.");
                RosvikOsmV15.Way schoolWay = ways.FirstOrDefault(w => w.Id == MainSchoolWay);
                if (schoolWay == null) throw new InvalidOperationException("V34 could not locate Rosviks skola.");
                Vector3 school = RosvikOsmV15.Centroid(schoolWay);

                Material glass = Mat("house_glass", new Color(.055f,.105f,.12f), .68f);
                Material trimLight = Mat("trim_light", new Color(.76f,.75f,.67f), .16f);
                Material trimMuted = Mat("trim_muted", new Color(.46f,.45f,.40f), .14f);
                Material foundation = Mat("foundation", new Color(.16f,.17f,.16f), .12f);
                Material metal = Mat("gutter_metal", new Color(.23f,.24f,.22f), .28f);
                Material chimney = Mat("chimney", new Color(.24f,.20f,.17f), .18f);
                Material step = Mat("porch_step", new Color(.28f,.285f,.26f), .14f);
                Material doorRed = Mat("door_red", new Color(.34f,.105f,.075f), .20f);
                Material doorBlue = Mat("door_blue", new Color(.095f,.18f,.22f), .22f);
                Material doorGreen = Mat("door_green", new Color(.105f,.20f,.14f), .18f);
                Material doorWood = Mat("door_wood", new Color(.29f,.18f,.10f), .16f);
                Material[] doors = { doorRed, doorBlue, doorGreen, doorWood };

                List<RosvikOsmV15.Way> houses = ways
                    .Where(IsHouse)
                    .Where(w => w.Id != MainSchoolWay && w.Id != OldSchoolWay && w.Id != ArenaWay)
                    .OrderBy(w => Vector3.Distance(Flat(RosvikOsmV15.Centroid(w)), Flat(school)))
                    .Where(w => Vector3.Distance(Flat(RosvikOsmV15.Centroid(w)), Flat(school)) <= 118f)
                    .Take(38)
                    .ToList();

                int detailed = 0;
                foreach (RosvikOsmV15.Way house in houses) {
                    List<Vector3> pts = Points(house);
                    if (pts.Count < 3) continue;
                    string tag = house.Tag("building");
                    float height = tag == "apartments" ? 5.2f : 2.9f;
                    int seed = unchecked((int)(house.Id & 0x7fffffff));
                    System.Random rng = new System.Random(seed);
                    Material trim = (seed % 5 == 0) ? trimMuted : trimLight;
                    Material doorMat = doors[Math.Abs(seed % doors.Length)];
                    BuildHouseDetails(group, house, pts, height, tag == "apartments", trim, glass, foundation, metal, chimney, step, doorMat, ways, rng);
                    detailed++;
                }

                root.name = RootName;
                EditorPrefs.SetInt(Key, Version);
                Selection.activeObject = null;
                EditorUtility.SetDirty(root);
                AssetDatabase.SaveAssets();
                if (!Busy()) {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene, ScenePath);
                }
                Debug.Log("ROSVIK V34: " + detailed + " nearby mapped houses received stable facade detail, porches, drainage and restrained roof hardware. Controls/gameplay/geography unchanged.");
            } catch (Exception ex) {
                Debug.LogError("ROSVIK V34 FAILED: " + ex);
            }
        }

        static bool IsHouse(RosvikOsmV15.Way w) {
            if (!w.Closed || w.Nodes.Count < 4) return false;
            string b = w.Tag("building");
            if (string.IsNullOrEmpty(b) || b == "no" || b == "garage" || b == "shed" || b == "carport" || b == "roof") return false;
            if (b == "house" || b == "residential" || b == "detached" || b == "semidetached_house" || b == "terrace" || b == "apartments") return true;
            // OSM often uses building=yes for ordinary detached housing in small villages.
            if (b == "yes") {
                RosvikOsmV15.OBounds ob = RosvikOsmV15.Bounds(w);
                float areaProxy = ob.Width * ob.Depth;
                return areaProxy >= 35f && areaProxy <= 420f;
            }
            return false;
        }

        static void BuildHouseDetails(Transform parent, RosvikOsmV15.Way way, List<Vector3> pts, float height, bool apartments,
            Material trim, Material glass, Material foundation, Material metal, Material chimney, Material step, Material door,
            List<RosvikOsmV15.Way> ways, System.Random rng) {

            float area = SignedArea(pts);
            Transform h = NewGroup(parent, "house detail " + way.Id);

            int frontEdge = 0;
            float bestRoad = float.MaxValue;
            for (int i = 0; i < pts.Count; i++) {
                Vector3 mid = (pts[i] + pts[(i + 1) % pts.Count]) * .5f;
                float d = DistanceToRoad(mid, ways);
                if (d < bestRoad) { bestRoad = d; frontEdge = i; }
            }

            for (int i = 0; i < pts.Count; i++) {
                Vector3 a = pts[i], b = pts[(i + 1) % pts.Count];
                Vector3 dir = Flat(b - a);
                float len = dir.magnitude;
                if (len < 2.1f) continue;
                dir /= len;
                Vector3 left = new Vector3(-dir.z, 0f, dir.x);
                Vector3 outward = area > 0f ? -left : left;
                Vector3 center = (a + b) * .5f;

                // A dark plinth/foundation breaks the large flat wall planes immediately.
                AddPanel("foundation band", h, center + outward * .045f + Vector3.up * .20f, outward,
                    new Vector3(Mathf.Max(.4f, len - .12f), .32f, .055f), foundation);

                bool front = i == frontEdge;
                float doorAlong = front ? Mathf.Lerp(-len * .16f, len * .16f, (float)rng.NextDouble()) : 999f;
                int count = Mathf.Clamp(Mathf.FloorToInt(len / 3.0f), 1, 6);
                float margin = Mathf.Min(1.05f, len * .14f);
                float usable = Mathf.Max(.6f, len - margin * 2f);
                float spacing = usable / count;

                for (int k = 0; k < count; k++) {
                    float along = -len * .5f + margin + spacing * (k + .5f);
                    if (front && Mathf.Abs(along - doorAlong) < 1.25f) continue;
                    float width = Mathf.Clamp(spacing * .48f, .72f, 1.18f);
                    AddWindow(h, center + dir * along, outward, 1.62f, width, .94f, trim, glass);
                    if (apartments) AddWindow(h, center + dir * along, outward, 3.72f, width, .94f, trim, glass);
                }

                if (front) {
                    Vector3 doorPos = center + dir * doorAlong;
                    AddPanel("front door frame", h, doorPos + outward * .070f + Vector3.up * 1.12f, outward,
                        new Vector3(1.18f, 2.20f, .09f), trim);
                    AddPanel("front door", h, doorPos + outward * .125f + Vector3.up * 1.10f, outward,
                        new Vector3(.94f, 2.00f, .055f), door);
                    AddPanel("door glazing", h, doorPos + outward * .158f + Vector3.up * 1.60f, outward,
                        new Vector3(.42f, .48f, .025f), glass);
                    AddPanel("house number", h, doorPos + dir * .68f + outward * .165f + Vector3.up * 1.62f, outward,
                        new Vector3(.22f, .15f, .025f), trim);

                    // Small canopy + two shallow steps, deliberately compact enough for the
                    // existing gameplay collision to remain governed by the original house.
                    AddPanel("porch canopy", h, doorPos + outward * .48f + Vector3.up * 2.26f, outward,
                        new Vector3(1.55f, .10f, .86f), trim);
                    Box("porch step upper", h, doorPos + outward * .46f + Vector3.up * .10f,
                        new Vector3(1.42f, .18f, .62f), Quaternion.LookRotation(outward, Vector3.up), step);
                    Box("porch step lower", h, doorPos + outward * .82f + Vector3.up * .055f,
                        new Vector3(1.58f, .10f, .48f), Quaternion.LookRotation(outward, Vector3.up), step);
                }

                // One rainwater pipe on every second readable edge adds vertical rhythm and
                // makes the houses feel constructed rather than extruded.
                if ((i + way.Id) % 2 == 0) {
                    Vector3 corner = b + outward * .075f;
                    Box("downpipe", h, corner + Vector3.up * (height * .48f), new Vector3(.07f, height * .88f, .07f), Quaternion.identity, metal);
                }
            }

            // Restrained chimney variation. It touches only the silhouette, never the roof mesh.
            if (!apartments && (way.Id % 3 != 0)) {
                RosvikOsmV15.OBounds ob = RosvikOsmV15.Bounds(way);
                Vector3 axis = ob.AxisX.sqrMagnitude > .01f ? ob.AxisX.normalized : Vector3.right;
                Vector3 p = ob.Center + axis * Mathf.Clamp(ob.Width * .18f, .55f, 1.5f) + Vector3.up * (height + .36f);
                Box("chimney", h, p, new Vector3(.48f, .72f, .42f), Quaternion.FromToRotation(Vector3.right, axis), chimney);
                Box("chimney cap", h, p + Vector3.up * .39f, new Vector3(.58f, .08f, .52f), Quaternion.FromToRotation(Vector3.right, axis), metal);
            }
        }

        static void AddWindow(Transform parent, Vector3 basePos, Vector3 outward, float y, float width, float height, Material trim, Material glass) {
            AddPanel("window frame", parent, basePos + outward * .060f + Vector3.up * y, outward,
                new Vector3(width + .16f, height + .16f, .070f), trim);
            AddPanel("window glass", parent, basePos + outward * .105f + Vector3.up * y, outward,
                new Vector3(width, height, .040f), glass);
            AddPanel("window sill", parent, basePos + outward * .135f + Vector3.up * (y - height * .55f), outward,
                new Vector3(width + .22f, .075f, .15f), trim);
            // A central mullion is enough to stop every facade reading as black rectangles.
            AddPanel("window mullion", parent, basePos + outward * .142f + Vector3.up * y, outward,
                new Vector3(.045f, height, .025f), trim);
        }

        static void AddPanel(string name, Transform parent, Vector3 pos, Vector3 outward, Vector3 scale, Material mat) {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name; g.transform.SetParent(parent, false);
            g.transform.position = pos;
            g.transform.rotation = Quaternion.LookRotation(outward, Vector3.up);
            g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = mat;
            UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        }

        static void Box(string name, Transform parent, Vector3 pos, Vector3 scale, Quaternion rot, Material mat) {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name; g.transform.SetParent(parent, false);
            g.transform.SetPositionAndRotation(pos, rot);
            g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = mat;
            UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        }

        static float DistanceToRoad(Vector3 p, List<RosvikOsmV15.Way> ways) {
            float best = float.MaxValue;
            foreach (RosvikOsmV15.Way w in ways) {
                string highway = w.Tag("highway");
                if (string.IsNullOrEmpty(highway) || w.Nodes.Count < 2) continue;
                if (highway == "footway" || highway == "path" || highway == "cycleway") continue;
                for (int i = 0; i < w.Nodes.Count - 1; i++) {
                    Vector3 q = ClosestPoint(p, w.Nodes[i].Pos, w.Nodes[i + 1].Pos);
                    best = Mathf.Min(best, Vector3.Distance(Flat(p), Flat(q)));
                }
            }
            return best;
        }

        static Vector3 ClosestPoint(Vector3 p, Vector3 a, Vector3 b) {
            Vector3 ab = Flat(b - a);
            float den = ab.sqrMagnitude;
            if (den < .0001f) return a;
            float t = Mathf.Clamp01(Vector3.Dot(Flat(p - a), ab) / den);
            return a + ab * t;
        }

        static Material Mat(string name, Color color, float smoothness) {
            string path = GeneratedDir + "/mat_" + name + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader s = ResolveShader();
            if (!m) { m = new Material(s) { name = "V34 " + name }; AssetDatabase.CreateAsset(m, path); }
            m.shader = s;
            m.color = color;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Shader ResolveShader() {
            Material proven = AssetDatabase.LoadAssetAtPath<Material>(ProvenMaterial);
            if (proven && proven.shader && proven.shader.isSupported) return proven.shader;
            Shader s = GraphicsSettings.defaultRenderPipeline != null ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
            if (!s || !s.isSupported) s = Shader.Find("Sprites/Default");
            return s;
        }

        static List<Vector3> Points(RosvikOsmV15.Way w) {
            List<Vector3> p = w.Nodes.Select(n => n.Pos).ToList();
            if (p.Count > 2 && w.Closed) p.RemoveAt(p.Count - 1);
            return p;
        }

        static float SignedArea(List<Vector3> p) {
            float a = 0f;
            for (int i = 0; i < p.Count; i++) {
                Vector3 q = p[(i + 1) % p.Count];
                a += p[i].x * q.z - q.x * p[i].z;
            }
            return a * .5f;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        static Transform NewGroup(Transform parent, string name) { Transform t = new GameObject(name).transform; t.SetParent(parent, false); return t; }
        static Transform Find(Transform root, string name) {
            if (!root) return null;
            if (root.name == name) return root;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }
    }
}
#endif
