#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UScene = UnityEngine.SceneManagement.Scene;

namespace Rosvik.Blackout.EditorTools {
    /// <summary>
    /// V29 is a visual/readability polish pass on top of V28. It does NOT move roads,
    /// building footprints or roofs. It fixes oversized street furniture, clears
    /// vegetation out of entrance courts/paths, adds modest planted edges and brings
    /// the gameplay camera closer so the schoolyard reads as a place rather than a map.
    /// </summary>
    [InitializeOnLoad]
    public static class RosvikSchoolyardPolishV29 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_SCHOOLYARD_POLISH_V29_VERSION";
        const int Version = 29;
        const string RootName = "ROSVIK_SCHOOLYARD_POLISH_V29";
        const string V28GroupName = "11 SCHOOL LIFE V28";
        const string V29GroupName = "12 SCHOOLYARD POLISH V29";
        const string KenneyRoot = "Assets/Rosvik/ThirdParty/V22Models/KenneyNature";
        const string V28MatRoot = "Assets/Rosvik/GeneratedV28";

        static RosvikSchoolyardPolishV29() {
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        [MenuItem("Rosvik/Rebuild Schoolyard Polish V29")]
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
            return GameObject.Find("ROSVIK_SCHOOL_LIFE_V28")
                ?? GameObject.Find("ROSVIK_SHADER_SAFE_V27")
                ?? GameObject.Find("ROSVIK_SCHOOL_WORLD_V26")
                ?? GameObject.Find(RootName);
        }

        static void Apply(UScene scene, GameObject root) {
            try {
                Transform v28 = Find(root.transform, V28GroupName);
                if (!v28) throw new InvalidOperationException("V29 requires the V28 school-life group.");

                Transform old = Find(root.transform, V29GroupName);
                if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
                Transform group = NewGroup(root.transform, V29GroupName);

                FixBenchScale(v28);
                ClearEntranceZones(v28);
                AddEdgePlanting(group, v28);
                AddDoorThresholds(group, v28);
                TuneCamera(root.transform);
                TuneLighting();

                root.name = RootName;
                EditorPrefs.SetInt(Key, Version);
                Selection.activeObject = null;
                EditorUtility.SetDirty(root);
                if (!Busy()) {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene, ScenePath);
                    AssetDatabase.SaveAssets();
                }
                Debug.Log("ROSVIK V29: schoolyard scale/pacing polished; giant benches corrected, entrance paths cleared, planted edges added, camera tightened. Geography/roofs unchanged.");
            } catch (Exception ex) {
                Debug.LogError("ROSVIK V29 FAILED: " + ex);
            }
        }

        static void FixBenchScale(Transform v28) {
            Transform[] benches = v28.GetComponentsInChildren<Transform>(true)
                .Where(t => t != v28 && t.name.IndexOf("bench", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();

            foreach (Transform t in benches) {
                Bounds b = BoundsOf(t.gameObject);
                float horizontal = Mathf.Max(b.size.x, b.size.z);
                float height = b.size.y;
                if (horizontal <= .01f || height <= .01f) continue;

                float s = Mathf.Min(1f, 2.25f / horizontal, .72f / height);
                if (s < .99f) t.localScale *= s;
                GroundObject(t.gameObject, .045f);
            }
        }

        static void ClearEntranceZones(Transform v28) {
            List<Bounds> protectedZones = new List<Bounds>();
            foreach (Transform t in v28.GetComponentsInChildren<Transform>(true)) {
                if (!t) continue;
                if (t.name == "school entrance court" || t.name == "Träskolan entrance court" ||
                    t.name == "arena forecourt" || t.name == "entry approach") {
                    Renderer r = t.GetComponent<Renderer>();
                    if (!r) continue;
                    Bounds b = r.bounds;
                    b.Expand(new Vector3(2.0f, 4f, 2.0f));
                    protectedZones.Add(b);
                }
            }

            Transform[] vegetation = v28.GetComponentsInChildren<Transform>(true)
                .Where(t => t != v28 && (t.name == "school edge bush" || t.name == "school edge tree"))
                .ToArray();

            foreach (Transform t in vegetation) {
                Vector3 p = BoundsOf(t.gameObject).center;
                if (protectedZones.Any(b => b.Contains(p)))
                    UnityEngine.Object.DestroyImmediate(t.gameObject);
            }
        }

        static void AddEdgePlanting(Transform parent, Transform v28) {
            GameObject bushAsset = AssetDatabase.LoadAssetAtPath<GameObject>(KenneyRoot + "/plant_bushDetailed.obj");
            if (!bushAsset) return;
            Material bushMat = AssetDatabase.LoadAssetAtPath<Material>(V28MatRoot + "/mat_bush.mat");
            if (!bushMat) return;

            Transform[] courts = v28.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name == "school entrance court" || t.name == "Träskolan entrance court")
                .ToArray();

            int count = 0;
            foreach (Transform court in courts) {
                Renderer rr = court.GetComponent<Renderer>();
                if (!rr) continue;
                Bounds b = rr.bounds;
                Vector3 right = Flat(court.right).normalized;
                Vector3 forward = Flat(court.forward).normalized;
                if (right.sqrMagnitude < .1f) right = Vector3.right;
                if (forward.sqrMagnitude < .1f) forward = Vector3.forward;

                float halfW = Mathf.Max(2.5f, court.localScale.x * .5f);
                Vector3[] positions = {
                    b.center + right * (halfW + 1.25f) + forward * .55f,
                    b.center - right * (halfW + 1.25f) + forward * .55f,
                    b.center + right * (halfW + 1.55f) - forward * 1.15f,
                    b.center - right * (halfW + 1.55f) - forward * 1.15f
                };

                foreach (Vector3 p0 in positions) {
                    Vector3 p = new Vector3(p0.x, .04f, p0.z);
                    GameObject go = PlaceModel(bushAsset, parent, "entrance shrub", p, 35f + count * 57f, .72f + (count % 2) * .12f, bushMat);
                    if (go) count++;
                }
            }
        }

        static void AddDoorThresholds(Transform parent, Transform v28) {
            Material pathMat = AssetDatabase.LoadAssetAtPath<Material>(V28MatRoot + "/mat_packed_path.mat");
            if (!pathMat) return;

            Transform[] courts = v28.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name == "school entrance court" || t.name == "Träskolan entrance court")
                .ToArray();

            foreach (Transform court in courts) {
                Renderer rr = court.GetComponent<Renderer>();
                if (!rr) continue;
                Bounds b = rr.bounds;
                Transform door = FindNearestNamed(root: court.root, name: "main entrance", point: b.center);
                if (!door) continue;

                Vector3 towardDoor = Flat(door.position - b.center);
                if (towardDoor.sqrMagnitude < .05f) continue;
                towardDoor.Normalize();
                Vector3 right = new Vector3(towardDoor.z, 0f, -towardDoor.x);
                Vector3 center = door.position - towardDoor * .55f;
                center.y = .09f;

                GameObject step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.name = "entrance threshold";
                step.transform.SetParent(parent, false);
                step.transform.position = center;
                step.transform.rotation = Quaternion.LookRotation(towardDoor, Vector3.up);
                step.transform.localScale = new Vector3(court.name.StartsWith("Trä") ? 1.55f : 2.15f, .11f, .62f);
                step.GetComponent<Renderer>().sharedMaterial = pathMat;
                UnityEngine.Object.DestroyImmediate(step.GetComponent<Collider>());
            }
        }

        static Transform FindNearestNamed(Transform root, string name, Vector3 point) {
            Transform best = null; float bestD = float.MaxValue;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) {
                if (t.name != name) continue;
                float d = (Flat(t.position - point)).sqrMagnitude;
                if (d < bestD) { bestD = d; best = t; }
            }
            return best;
        }

        static void TuneCamera(Transform root) {
            Transform player = Find(root, "PLAYER");
            Camera cam = Camera.main;
            if (!cam) return;
            cam.orthographic = true;
            cam.orthographicSize = 8.35f;
            IsometricCameraRig rig = cam.GetComponent<IsometricCameraRig>();
            if (rig) {
                if (player) rig.target = player;
                rig.pitch = 51f;
                rig.yaw = 38f;
                rig.orthographicSize = 8.35f;
                rig.minSize = 6.0f;
                rig.maxSize = 14.5f;
                rig.zoomStep = .65f;
                rig.focusOffset = new Vector3(0f, .90f, 0f);
                rig.followSharpness = 12f;
            }
        }

        static void TuneLighting() {
            foreach (Light l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) {
                if (l.type != LightType.Directional) continue;
                l.intensity = .98f;
                l.shadowStrength = .58f;
                l.color = new Color(.93f, .89f, .80f);
                l.transform.rotation = Quaternion.Euler(52f, -36f, 0f);
            }
            RenderSettings.ambientLight = new Color(.36f, .385f, .34f);
            RenderSettings.fogColor = new Color(.43f, .455f, .42f);
            RenderSettings.fogDensity = .00105f;
        }

        static GameObject PlaceModel(GameObject asset, Transform parent, string name, Vector3 pos, float yaw, float targetHeight, Material material) {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            if (!go) go = UnityEngine.Object.Instantiate(asset, parent);
            go.name = name;
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one;
            Bounds b = BoundsOf(go);
            float s = targetHeight / Mathf.Max(.01f, b.size.y);
            go.transform.localScale = Vector3.one * s;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = material;
            foreach (Collider c in go.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
            GroundObject(go, .04f);
            return go;
        }

        static void GroundObject(GameObject go, float targetY) {
            Bounds b = BoundsOf(go);
            if (b.size.sqrMagnitude < .0001f) return;
            Vector3 p = go.transform.position;
            p.y += targetY - b.min.y;
            go.transform.position = p;
        }

        static Bounds BoundsOf(GameObject go) {
            Renderer[] rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        static Transform NewGroup(Transform p, string n) { Transform t = new GameObject(n).transform; t.SetParent(p, false); return t; }
        static Transform Find(Transform root, string n) {
            if (!root) return null;
            if (root.name == n) return root;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == n) return t;
            return null;
        }
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
    }
}
#endif
