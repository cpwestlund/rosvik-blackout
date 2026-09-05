#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rosvik.Blackout.EditorTools {
    /// <summary>
    /// V23 is deliberately not another rebuild. It keeps the real Rosvik geography from V20/V22
    /// and improves the actual playable view: removes the loud OSM land-cover polygons, adds
    /// denser CC0 nature around the school slice, and tightens camera/light for a game-like frame.
    /// </summary>
    [InitializeOnLoad]
    public static class RosvikPlayPassV23 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_PLAY_PASS_VERSION";
        const int Version = 23;
        const long MainSchoolWay = 163199458;
        const string GeneratedRoot = "Assets/Rosvik/GeneratedV20";
        const string KenneyRoot = "Assets/Rosvik/ThirdParty/V22Models/KenneyNature";

        static RosvikPlayPassV23() {
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        [MenuItem("Rosvik/Rebuild Play Pass V23")]
        public static void Force() {
            EditorPrefs.DeleteKey(Key);
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        static void TryApply() {
            if (EditorPrefs.GetInt(Key, 0) >= Version && File.Exists(ScenePath)) {
                EditorApplication.update -= TryApply;
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            EditorApplication.update -= TryApply;
            Apply();
        }

        static void Apply() {
            try {
                if (!File.Exists(ScenePath)) {
                    Debug.LogWarning("ROSVIK V23: hero scene does not exist yet; waiting for V22.");
                    return;
                }

                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                GameObject root = GameObject.Find("ROSVIK_HERO_SLICE_V22");
                if (!root) root = GameObject.Find("ROSVIK_PLAY_PASS_V23");
                if (!root) {
                    Debug.LogWarning("ROSVIK V23: V22 root not found. Use Rosvik/Rebuild Hero Slice V22 once, then V23.");
                    return;
                }

                List<RosvikOsmV15.Way> ways = RosvikOsmV15.LoadWays();
                RosvikOsmV15.Way main = ways.FirstOrDefault(w => w.Id == MainSchoolWay);
                if (main == null) throw new InvalidOperationException("V23 could not locate Rosviks skola in OSM cache.");
                Vector3 school = RosvikOsmV15.Centroid(main);

                Material localGrass = MaterialAsset(GeneratedRoot + "/mat_v23_local_grass.mat", new Color(.30f,.33f,.22f), .03f, 0f);
                Material localForest = MaterialAsset(GeneratedRoot + "/mat_v23_local_forest.mat", new Color(.245f,.285f,.185f), .025f, 0f);
                Material spruceA = MaterialAsset(GeneratedRoot + "/mat_v23_spruce_a.mat", new Color(.065f,.145f,.080f), .03f, 0f);
                Material spruceB = MaterialAsset(GeneratedRoot + "/mat_v23_spruce_b.mat", new Color(.095f,.205f,.105f), .03f, 0f);
                Material autumn = MaterialAsset(GeneratedRoot + "/mat_v23_autumn.mat", new Color(.33f,.30f,.105f), .025f, 0f);
                Material bush = MaterialAsset(GeneratedRoot + "/mat_v23_bush.mat", new Color(.18f,.27f,.105f), .025f, 0f);
                Material rock = MaterialAsset(GeneratedRoot + "/mat_v23_rock.mat", new Color(.285f,.285f,.255f), .06f, 0f);

                BlendLandcover(root.transform, school, localGrass, localForest);

                Transform oldPass = Find(root.transform, "08 PLAY PASS V23");
                if (oldPass) UnityEngine.Object.DestroyImmediate(oldPass.gameObject);
                Transform pass = new GameObject("08 PLAY PASS V23").transform;
                pass.SetParent(root.transform, false);

                AddNature(pass, ways, school, spruceA, spruceB, autumn, bush, rock);
                TuneCamera();
                TuneLighting();

                RosvikMapHUD hud = root.GetComponent<RosvikMapHUD>();
                if (hud) hud.maxLabelDistance = 58f;

                root.name = "ROSVIK_PLAY_PASS_V23";
                EditorPrefs.SetInt(Key, Version);
                Selection.activeObject = null;
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
                Debug.Log("ROSVIK V23: landcover blended, real nature densified, camera/light tightened. Geography unchanged.");
            } catch (Exception ex) {
                Debug.LogError("ROSVIK V23 FAILED: " + ex);
            }
        }

        static void BlendLandcover(Transform root, Vector3 school, Material grass, Material forest) {
            Transform land = Find(root, "01 LANDSCAPE");
            if (!land) return;

            foreach (Renderer r in land.GetComponentsInChildren<Renderer>(true)) {
                // OSM polygons are useful for geography but the old contrast made them look like
                // giant paper patches. Keep them, just make them part of one visual language.
                float d = Vector3.Distance(Flat(r.bounds.center), Flat(school));
                if (d > 145f) continue;
                string n = r.sharedMaterial ? r.sharedMaterial.name.ToLowerInvariant() : "";
                r.sharedMaterial = n.Contains("forest") ? forest : grass;
            }
        }

        static void AddNature(Transform parent, List<RosvikOsmV15.Way> ways, Vector3 school,
                              Material spruceA, Material spruceB, Material autumn, Material bush, Material rock) {
            GameObject pine = AssetDatabase.LoadAssetAtPath<GameObject>(KenneyRoot + "/tree_pineDefaultA.obj");
            GameObject tall = AssetDatabase.LoadAssetAtPath<GameObject>(KenneyRoot + "/tree_pineTallA.obj");
            GameObject fall = AssetDatabase.LoadAssetAtPath<GameObject>(KenneyRoot + "/tree_default_fall.obj");
            GameObject bushAsset = AssetDatabase.LoadAssetAtPath<GameObject>(KenneyRoot + "/plant_bushDetailed.obj");
            GameObject rockAsset = AssetDatabase.LoadAssetAtPath<GameObject>(KenneyRoot + "/rock_smallA.obj");
            if (!pine || !tall || !fall || !bushAsset) {
                Debug.LogWarning("ROSVIK V23: cached Kenney models missing; V22 must import them first.");
                return;
            }

            System.Random rng = new System.Random(2309);
            int trees = 0;
            for (int attempt = 0; attempt < 900 && trees < 48; attempt++) {
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float radius = 34f + (float)rng.NextDouble() * 82f;
                Vector3 p = school + new Vector3(Mathf.Cos(angle) * radius, .045f, Mathf.Sin(angle) * radius);
                if (!SafeForNature(p, ways, 3.8f)) continue;

                int kind = trees % 8;
                GameObject src = kind == 0 ? fall : (kind % 3 == 0 ? tall : pine);
                Material mat = kind == 0 ? autumn : (kind % 2 == 0 ? spruceB : spruceA);
                float height = kind == 0 ? Rand(rng, 4.0f, 5.8f) : Rand(rng, 4.8f, 8.2f);
                PlaceModel(src, parent, "v23 tree", p, Rand(rng, 0f, 360f), height, mat);
                trees++;
            }

            int bushes = 0;
            for (int attempt = 0; attempt < 600 && bushes < 42; attempt++) {
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float radius = 24f + (float)rng.NextDouble() * 88f;
                Vector3 p = school + new Vector3(Mathf.Cos(angle) * radius, .035f, Mathf.Sin(angle) * radius);
                if (!SafeForNature(p, ways, 2.0f)) continue;
                PlaceModel(bushAsset, parent, "v23 bush", p, Rand(rng, 0f, 360f), Rand(rng, .55f, 1.25f), bush);
                bushes++;
            }

            if (rockAsset) {
                int rocks = 0;
                for (int attempt = 0; attempt < 250 && rocks < 11; attempt++) {
                    float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                    float radius = 32f + (float)rng.NextDouble() * 78f;
                    Vector3 p = school + new Vector3(Mathf.Cos(angle) * radius, .03f, Mathf.Sin(angle) * radius);
                    if (!SafeForNature(p, ways, 2.2f)) continue;
                    PlaceModel(rockAsset, parent, "v23 rock", p, Rand(rng, 0f, 360f), Rand(rng, .30f, .72f), rock);
                    rocks++;
                }
            }
        }

        static bool SafeForNature(Vector3 p, List<RosvikOsmV15.Way> ways, float roadClearance) {
            if (InsideBuilding(p, ways) || InsidePitch(p, ways)) return false;
            return DistanceToRoad(p, ways) > roadClearance;
        }

        static bool InsideBuilding(Vector3 p, List<RosvikOsmV15.Way> ways) {
            foreach (var w in ways) {
                if (!w.Closed || string.IsNullOrEmpty(w.Tag("building")) || w.Tag("building") == "no") continue;
                if (Inside(p, Points(w))) return true;
            }
            return false;
        }

        static bool InsidePitch(Vector3 p, List<RosvikOsmV15.Way> ways) {
            foreach (var w in ways) {
                if (!w.Closed) continue;
                if (w.Tag("leisure") != "pitch" && w.Tag("sport") != "soccer") continue;
                if (Inside(p, Points(w))) return true;
            }
            return false;
        }

        static float DistanceToRoad(Vector3 p, List<RosvikOsmV15.Way> ways) {
            float best = float.MaxValue;
            foreach (var w in ways) {
                string h = w.Tag("highway");
                if (string.IsNullOrEmpty(h) || w.Nodes.Count < 2) continue;
                for (int i = 0; i < w.Nodes.Count - 1; i++) {
                    Vector3 q = ClosestPoint(p, w.Nodes[i].Pos, w.Nodes[i + 1].Pos);
                    best = Mathf.Min(best, Vector3.Distance(Flat(p), Flat(q)));
                }
            }
            return best;
        }

        static List<Vector3> Points(RosvikOsmV15.Way w) {
            List<Vector3> pts = w.Nodes.Select(n => n.Pos).ToList();
            if (pts.Count > 2 && w.Closed) pts.RemoveAt(pts.Count - 1);
            return pts;
        }

        static bool Inside(Vector3 p, List<Vector3> poly) {
            bool c = false;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++) {
                Vector3 a = poly[i], b = poly[j];
                if (((a.z > p.z) != (b.z > p.z)) &&
                    (p.x < (b.x - a.x) * (p.z - a.z) / ((b.z - a.z) + .000001f) + a.x)) c = !c;
            }
            return c;
        }

        static Vector3 ClosestPoint(Vector3 p, Vector3 a, Vector3 b) {
            Vector3 ab = b - a; ab.y = 0f;
            float den = ab.sqrMagnitude;
            if (den < .0001f) return a;
            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / den);
            return a + ab * t;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        static float Rand(System.Random r, float a, float b) { return Mathf.Lerp(a, b, (float)r.NextDouble()); }

        static GameObject PlaceModel(GameObject asset, Transform parent, string name, Vector3 pos, float yaw, float targetHeight, Material material) {
            if (!asset) return null;
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            if (!go) go = UnityEngine.Object.Instantiate(asset, parent);
            go.name = name;
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one;

            Bounds b = RendererBounds(go);
            float h = Mathf.Max(.01f, b.size.y);
            float scale = targetHeight / h;
            go.transform.localScale = Vector3.one * scale;
            b = RendererBounds(go);
            go.transform.position += Vector3.up * (pos.y - b.min.y);

            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true)) {
                r.sharedMaterial = material;
                r.shadowCastingMode = ShadowCastingMode.On;
                r.receiveShadows = true;
            }
            foreach (Collider c in go.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
            return go;
        }

        static Bounds RendererBounds(GameObject go) {
            Renderer[] rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        static void TuneCamera() {
            Camera cam = Camera.main;
            if (!cam) return;
            cam.orthographic = true;
            cam.orthographicSize = 9.2f;
            cam.backgroundColor = new Color(.255f,.285f,.255f);
            IsometricCameraRig rig = cam.GetComponent<IsometricCameraRig>();
            if (rig) {
                rig.yaw = 38f;
                rig.pitch = 39f;
                rig.orthographicSize = 9.2f;
                rig.minSize = 6.8f;
                rig.maxSize = 15f;
                rig.zoomStep = .70f;
                rig.focusOffset = new Vector3(0f,.9f,0f);
                rig.followSharpness = 12f;
            }
        }

        static void TuneLighting() {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.45f,.47f,.40f);
            foreach (Light l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) {
                if (l.type != LightType.Directional) continue;
                l.color = new Color(.93f,.87f,.74f);
                l.intensity = 1.18f;
                l.shadows = LightShadows.Soft;
                l.transform.rotation = Quaternion.Euler(47f,-32f,0f);
            }
        }

        static Material MaterialAsset(string path, Color color, float smoothness, float metallic) {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (!shader) shader = Shader.Find("Standard");
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Transform Find(Transform root, string name) {
            if (!root) return null;
            if (root.name == name) return root;
            foreach (Transform child in root) {
                Transform f = Find(child, name);
                if (f) return f;
            }
            return null;
        }
    }
}
#endif
