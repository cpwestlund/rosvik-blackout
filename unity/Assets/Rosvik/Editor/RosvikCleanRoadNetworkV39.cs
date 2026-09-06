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
    /// V39 replaces the old layered V20 road visuals with one clean, deduplicated road deck.
    /// The OSM alignment is preserved; this is presentation-only. Exact and near-collinear
    /// duplicate road segments are removed, legacy road renderers are disabled, and junctions
    /// are capped once globally instead of once per OSM way. Paths remain separate where they
    /// are genuinely separate, but centerline duplicates are suppressed.
    /// </summary>
    [InitializeOnLoad]
    public static class RosvikCleanRoadNetworkV39 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_CLEAN_ROAD_NETWORK_V39_VERSION";
        const int Version = 39;
        const string RequiredRoot = "ROSVIK_VILLAGE_FABRIC_V38";
        const string RootName = "ROSVIK_CLEAN_ROAD_NETWORK_V39";
        const string GroupName = "21 CLEAN ROAD NETWORK V39";
        const string LegacyRoadGroup = "02 ROADS";
        const string V37Group = "19 ROAD CLEANUP V37";
        const string GeneratedDir = "Assets/Rosvik/GeneratedV39";
        const string ProvenMaterial = "Assets/Rosvik/GeneratedV20/mat_ground.mat";

        sealed class Seg {
            public Vector3 a, b;
            public float width;
            public int priority;
            public bool vehicle;
            public bool asphalt;
            public string surface;
            public long wayId;
            public float Length => Flat(b - a).magnitude;
            public Vector3 Dir => Flat(b - a).normalized;
            public Vector3 Mid => (a + b) * .5f;
        }

        sealed class Junction {
            public Vector3 p;
            public int count;
            public float radius;
            public bool asphalt;
            public bool gravel;
        }

        static RosvikCleanRoadNetworkV39() {
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        [MenuItem("Rosvik/Rebuild Clean Road Network V39")]
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
            GameObject root = GameObject.Find(RequiredRoot) ?? GameObject.Find(RootName);
            if (!root) {
                if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                if (Busy()) return;
                root = GameObject.Find(RequiredRoot) ?? GameObject.Find(RootName);
            }
            if (!root || Busy()) return;

            EditorApplication.update -= TryApply;
            Apply(scene, root);
        }

        static void Apply(UScene scene, GameObject root) {
            try {
                Directory.CreateDirectory(GeneratedDir);
                AssetDatabase.Refresh();

                List<RosvikOsmV15.Way> ways = RosvikOsmV15.LoadWays();
                if (ways == null || ways.Count == 0) throw new InvalidOperationException("V39 could not load Rosvik OSM data.");

                Transform old = Find(root.transform, GroupName);
                if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
                Transform group = NewGroup(root.transform, GroupName);

                int hiddenLegacy = DisableLegacyRoadVisuals(root.transform);
                int hiddenCaps = DisableOldJunctionCaps(root.transform);

                Material asphalt = Mat("wet_asphalt_clean", new Color(.125f,.145f,.15f), .50f);
                Material gravel = Mat("gravel_lane_clean", new Color(.285f,.265f,.225f), .12f);
                Material foot = Mat("footpath_clean", new Color(.315f,.285f,.245f), .10f);
                Material shoulder = Mat("road_verge_clean", new Color(.235f,.235f,.195f), .07f);

                List<Seg> raw = CollectSegments(ways);
                List<Seg> vehicle = Deduplicate(raw.Where(s => s.vehicle).OrderByDescending(s => s.priority).ThenByDescending(s => s.Length).ToList(), .44f, .992f, .54f);
                List<Seg> paths = Deduplicate(raw.Where(s => !s.vehicle).OrderByDescending(s => s.priority).ThenByDescending(s => s.Length).ToList(), .34f, .992f, .58f);

                // Suppress path/cycleway geometry that is effectively painted on the same centerline
                // as a vehicle road. Genuine adjacent paths remain because their centerlines differ.
                paths = paths.Where(p => !OverlapsVehicleCenterline(p, vehicle)).ToList();

                int renderedVehicle = 0;
                foreach (Seg s in vehicle) {
                    Material surface = s.asphalt ? asphalt : gravel;
                    float y = s.asphalt ? .043f : .041f;
                    // A restrained verge layer gives the roads a soft rural edge without the old black rails.
                    Strip("clean road verge", group, s.a, s.b, s.width + .55f, .028f, shoulder);
                    Strip("clean road surface", group, s.a, s.b, s.width, y, surface);
                    renderedVehicle++;
                }

                int renderedPaths = 0;
                foreach (Seg s in paths) {
                    Strip("clean path", group, s.a, s.b, s.width, .051f, foot);
                    renderedPaths++;
                }

                int junctions = BuildVehicleJunctions(group, vehicle, asphalt, gravel);
                int pathJoints = BuildPathJunctions(group, paths, foot);

                root.name = RootName;
                EditorPrefs.SetInt(Key, Version);
                Selection.activeObject = null;
                EditorUtility.SetDirty(root);
                AssetDatabase.SaveAssets();
                if (!Busy()) {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene, ScenePath);
                }

                Debug.Log("ROSVIK V39: disabled " + hiddenLegacy + " legacy road renderers and " + hiddenCaps +
                          " old junction caps; rebuilt " + renderedVehicle + " deduplicated vehicle segments, " +
                          renderedPaths + " separate paths, " + junctions + " road joints and " + pathJoints +
                          " path joints. OSM alignment, buildings, gameplay and controls unchanged.");
            } catch (Exception ex) {
                Debug.LogError("ROSVIK V39 FAILED: " + ex);
            }
        }

        static List<Seg> CollectSegments(List<RosvikOsmV15.Way> ways) {
            List<Seg> list = new List<Seg>();
            foreach (RosvikOsmV15.Way w in ways) {
                string h = w.Tag("highway");
                if (string.IsNullOrEmpty(h) || w.Nodes.Count < 2) continue;

                bool vehicle = IsVehicle(h);
                bool path = IsPath(h);
                if (!vehicle && !path) continue;

                float width = WidthFor(w, h);
                int priority = PriorityFor(h);
                bool asphalt = IsAsphalt(w, h);

                for (int i = 0; i < w.Nodes.Count - 1; i++) {
                    Vector3 a = w.Nodes[i].Pos;
                    Vector3 b = w.Nodes[i + 1].Pos;
                    a.y = 0f; b.y = 0f;
                    if (Flat(b - a).sqrMagnitude < .06f) continue;
                    list.Add(new Seg {
                        a = a, b = b, width = width, priority = priority,
                        vehicle = vehicle, asphalt = asphalt, surface = w.Tag("surface"), wayId = w.Id
                    });
                }
            }
            return list;
        }

        static bool IsVehicle(string h) {
            return h == "residential" || h == "unclassified" || h == "tertiary" || h == "service" || h == "living_street" || h == "track";
        }

        static bool IsPath(string h) {
            return h == "footway" || h == "path" || h == "cycleway" || h == "pedestrian";
        }

        static int PriorityFor(string h) {
            if (h == "tertiary") return 70;
            if (h == "residential") return 60;
            if (h == "unclassified") return 55;
            if (h == "living_street") return 45;
            if (h == "service") return 35;
            if (h == "track") return 25;
            if (h == "cycleway") return 18;
            if (h == "pedestrian") return 17;
            if (h == "footway") return 16;
            return 15;
        }

        static float WidthFor(RosvikOsmV15.Way w, string h) {
            if (float.TryParse(w.Tag("width"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float explicitWidth))
                return Mathf.Clamp(explicitWidth, .8f, 8f);
            if (h == "tertiary") return 5.8f;
            if (h == "residential" || h == "unclassified") return 5.25f;
            if (h == "living_street") return 4.2f;
            if (h == "service") return 3.55f;
            if (h == "track") return 2.9f;
            if (h == "pedestrian") return 2.2f;
            if (h == "cycleway") return 1.75f;
            return 1.45f;
        }

        static bool IsAsphalt(RosvikOsmV15.Way w, string h) {
            string s = (w.Tag("surface") ?? string.Empty).ToLowerInvariant();
            if (s == "gravel" || s == "fine_gravel" || s == "unpaved" || s == "ground" || s == "dirt") return false;
            if (s == "asphalt" || s == "paved" || s == "concrete") return true;
            return h == "tertiary" || h == "residential" || h == "unclassified";
        }

        static List<Seg> Deduplicate(List<Seg> source, float maxLateral, float parallelDot, float minOverlapFraction) {
            List<Seg> accepted = new List<Seg>();
            HashSet<string> exact = new HashSet<string>(StringComparer.Ordinal);

            foreach (Seg s in source) {
                string key = SegmentKey(s.a, s.b, .12f);
                if (!exact.Add(key)) continue;

                bool duplicate = false;
                foreach (Seg a in accepted) {
                    if (NearCollinearOverlap(s, a, maxLateral, parallelDot, minOverlapFraction)) {
                        duplicate = true;
                        break;
                    }
                }
                if (!duplicate) accepted.Add(s);
            }
            return accepted;
        }

        static bool NearCollinearOverlap(Seg x, Seg y, float maxLateral, float parallelDot, float minOverlapFraction) {
            Vector3 dx = x.Dir, dy = y.Dir;
            if (dx.sqrMagnitude < .5f || dy.sqrMagnitude < .5f) return false;
            if (Mathf.Abs(Vector3.Dot(dx, dy)) < parallelDot) return false;

            float lateral = DistancePointSegment(x.Mid, y.a, y.b);
            if (lateral > maxLateral) return false;

            float yLen = y.Length;
            if (yLen < .05f) return false;
            Vector3 axis = dy;
            float x0 = Vector3.Dot(Flat(x.a - y.a), axis);
            float x1 = Vector3.Dot(Flat(x.b - y.a), axis);
            float lo = Mathf.Max(0f, Mathf.Min(x0, x1));
            float hi = Mathf.Min(yLen, Mathf.Max(x0, x1));
            float overlap = Mathf.Max(0f, hi - lo);
            float minLen = Mathf.Min(x.Length, yLen);
            return minLen > .05f && overlap >= minLen * minOverlapFraction;
        }

        static bool OverlapsVehicleCenterline(Seg path, List<Seg> vehicle) {
            foreach (Seg road in vehicle) {
                if (Mathf.Abs(Vector3.Dot(path.Dir, road.Dir)) < .985f) continue;
                float d = DistancePointSegment(path.Mid, road.a, road.b);
                float threshold = Mathf.Min(2.05f, road.width * .38f + .10f);
                if (d > threshold) continue;
                if (NearCollinearOverlap(path, road, threshold, .985f, .42f)) return true;
            }
            return false;
        }

        static int BuildVehicleJunctions(Transform parent, List<Seg> segs, Material asphalt, Material gravel) {
            Dictionary<string, Junction> map = new Dictionary<string, Junction>();
            foreach (Seg s in segs) {
                AddJunction(map, s.a, s.width * .5f, s.asphalt);
                AddJunction(map, s.b, s.width * .5f, s.asphalt);
            }
            int made = 0;
            foreach (Junction j in map.Values) {
                if (j.count < 2) continue;
                Disc("clean road joint", parent, j.p, j.radius + .08f, .046f, j.asphalt ? asphalt : gravel, 20);
                made++;
            }
            return made;
        }

        static int BuildPathJunctions(Transform parent, List<Seg> segs, Material foot) {
            Dictionary<string, Junction> map = new Dictionary<string, Junction>();
            foreach (Seg s in segs) {
                AddJunction(map, s.a, s.width * .5f, false);
                AddJunction(map, s.b, s.width * .5f, false);
            }
            int made = 0;
            foreach (Junction j in map.Values) {
                if (j.count < 2) continue;
                Disc("clean path joint", parent, j.p, j.radius + .04f, .053f, foot, 16);
                made++;
            }
            return made;
        }

        static void AddJunction(Dictionary<string, Junction> map, Vector3 p, float r, bool asphalt) {
            string key = PointKey(p, .14f);
            if (!map.TryGetValue(key, out Junction j)) {
                j = new Junction { p = p, radius = r, count = 0, asphalt = asphalt, gravel = !asphalt };
                map[key] = j;
            }
            j.count++;
            j.radius = Mathf.Max(j.radius, r);
            j.asphalt |= asphalt;
            j.gravel |= !asphalt;
        }

        static int DisableLegacyRoadVisuals(Transform root) {
            Transform roads = Find(root, LegacyRoadGroup);
            if (!roads) return 0;
            int n = 0;
            foreach (Renderer r in roads.GetComponentsInChildren<Renderer>(true)) {
                if (!r.enabled) continue;
                r.enabled = false;
                EditorUtility.SetDirty(r);
                n++;
            }
            return n;
        }

        static int DisableOldJunctionCaps(Transform root) {
            Transform v37 = Find(root, V37Group);
            if (!v37) return 0;
            int n = 0;
            foreach (Transform t in v37.GetComponentsInChildren<Transform>(true)) {
                if (t.name != "clean road junction") continue;
                Renderer r = t.GetComponent<Renderer>();
                if (!r || !r.enabled) continue;
                r.enabled = false;
                EditorUtility.SetDirty(r);
                n++;
            }
            return n;
        }

        static float DistancePointSegment(Vector3 p, Vector3 a, Vector3 b) {
            Vector3 ab = Flat(b - a);
            float den = ab.sqrMagnitude;
            if (den < .0001f) return Vector3.Distance(Flat(p), Flat(a));
            float t = Mathf.Clamp01(Vector3.Dot(Flat(p - a), ab) / den);
            Vector3 q = a + ab * t;
            return Vector3.Distance(Flat(p), Flat(q));
        }

        static string SegmentKey(Vector3 a, Vector3 b, float step) {
            string ka = PointKey(a, step), kb = PointKey(b, step);
            return string.CompareOrdinal(ka, kb) <= 0 ? ka + "|" + kb : kb + "|" + ka;
        }

        static string PointKey(Vector3 p, float step) {
            int x = Mathf.RoundToInt(p.x / step);
            int z = Mathf.RoundToInt(p.z / step);
            return x + ":" + z;
        }

        static void Strip(string name, Transform parent, Vector3 a, Vector3 b, float width, float y, Material mat) {
            Vector3 d = Flat(b - a);
            if (d.sqrMagnitude < .0001f) return;
            Vector3 n = new Vector3(-d.z, 0f, d.x).normalized * width * .5f;
            Vector3[] v = {
                new Vector3(a.x+n.x,y,a.z+n.z), new Vector3(a.x-n.x,y,a.z-n.z),
                new Vector3(b.x-n.x,y,b.z-n.z), new Vector3(b.x+n.x,y,b.z+n.z)
            };
            Mesh mesh = new Mesh { name = name + " mesh", vertices = v, triangles = new[] {0,1,2,0,2,3} };
            mesh.RecalculateNormals();
            if (mesh.normals.Length > 0 && mesh.normals.Average(q => q.y) < 0f) {
                mesh.triangles = new[] {0,2,1,0,3,2};
                mesh.RecalculateNormals();
            }
            GameObject g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            g.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }

        static void Disc(string name, Transform parent, Vector3 c, float r, float y, Material mat, int seg) {
            Vector3[] v = new Vector3[seg + 1];
            v[0] = new Vector3(c.x, y, c.z);
            for (int i = 0; i < seg; i++) {
                float a = i * Mathf.PI * 2f / seg;
                v[i + 1] = new Vector3(c.x + Mathf.Cos(a) * r, y, c.z + Mathf.Sin(a) * r);
            }
            int[] tri = new int[seg * 3];
            for (int i = 0; i < seg; i++) {
                int j = (i + 1) % seg;
                tri[i*3] = 0; tri[i*3+1] = i+1; tri[i*3+2] = j+1;
            }
            Mesh mesh = new Mesh { name = name + " mesh", vertices = v, triangles = tri };
            mesh.RecalculateNormals();
            if (mesh.normals.Length > 0 && mesh.normals.Average(q => q.y) < 0f) {
                for (int i = 0; i < tri.Length; i += 3) { int t = tri[i+1]; tri[i+1] = tri[i+2]; tri[i+2] = t; }
                mesh.triangles = tri;
                mesh.RecalculateNormals();
            }
            GameObject g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            g.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }

        static Material Mat(string name, Color color, float smoothness) {
            string path = GeneratedDir + "/mat_" + name + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader s = ResolveShader();
            if (!m) { m = new Material(s) { name = "V39 " + name }; AssetDatabase.CreateAsset(m, path); }
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