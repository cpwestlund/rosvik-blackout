#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Rosvik.Blackout;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikVisualFixV901 {
        const int Version = 901;
        const string Key = "ROSVIK_VISUAL_FIX_V901";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string WorldName = "V88 CLEAN OSM WORLD";

        static RosvikVisualFixV901() {
            if (EditorPrefs.GetInt(Key, 0) >= Version) return;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V90.1 FIX ROOFS + SIGNS")]
        public static void Force() {
            EditorPrefs.DeleteKey(Key);
            EditorApplication.delayCall += Auto;
        }

        static void Auto() {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) {
                EditorApplication.delayCall += Auto;
                return;
            }
            if (!File.Exists(ScenePath)) return;
            try { Apply(); }
            catch (Exception ex) { Debug.LogError("V90.1 VISUAL FIX FAILED: " + ex); }
        }

        static void Apply() {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject world = GameObject.Find(WorldName);
            if (!world) throw new Exception("V88 CLEAN OSM WORLD missing");

            GameObject school = FindDeep(world.transform, "ROSVIKS SKOLA · AUTHORED V90");
            GameObject arena = FindDeep(world.transform, "NORRBOTTEN STÅL ARENA · AUTHORED V90");
            if (!school || !arena) throw new Exception("V90 school or arena missing");

            // Always leave roofs visible in the saved scene. Runtime cutaway now hides them only after the player
            // is genuinely inside the footprint, never just because they walk around the exterior.
            GameObject schoolRoof = FindDeep(school.transform, "ROOF ROOT");
            GameObject arenaRoof = FindDeep(arena.transform, "ROOF ROOT");
            if (schoolRoof) schoolRoof.SetActive(true);
            if (arenaRoof) arenaRoof.SetActive(true);

            FixSchoolSign(school);
            FixArenaSign(arena);
            NormalizeSchoolWindows(school);
            RemoveStrayFacadeBlocks(school);

            CleanupMissingScripts();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorPrefs.SetInt(Key, Version);
            SceneView.RepaintAll();
            Debug.Log("V90.1 COMPLETE — roof cutaway is inside-only, school/arena signs are facade-mounted and readable, stray blocky facade leftovers removed.");
        }

        static void FixSchoolSign(GameObject school) {
            Transform board = FindTransform(school.transform, "school sign board");
            if (board) {
                board.localPosition = new Vector3(0f, 2.48f, -8.69f);
                board.localRotation = Quaternion.identity;
                board.localScale = new Vector3(4.7f, .42f, .08f);
            }

            foreach (TextMesh tm in school.GetComponentsInChildren<TextMesh>(true)) {
                if (!tm || tm.text != "ROSVIKS SKOLA") continue;
                tm.transform.localPosition = new Vector3(0f, 2.48f, -8.745f);
                tm.transform.localRotation = Quaternion.identity;
                tm.fontSize = 64;
                tm.characterSize = .070f;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.fontStyle = FontStyle.Bold;
                tm.color = new Color(.96f, .95f, .90f, 1f);
                EditorUtility.SetDirty(tm);
            }
        }

        static void FixArenaSign(GameObject arena) {
            foreach (TextMesh tm in arena.GetComponentsInChildren<TextMesh>(true)) {
                if (!tm || !tm.text.Contains("NORRBOTTEN STÅL ARENA")) continue;
                tm.transform.localRotation = Quaternion.identity;
                tm.characterSize = .065f;
                tm.fontStyle = FontStyle.Bold;
                tm.color = new Color(.18f, .25f, .29f, 1f);
                EditorUtility.SetDirty(tm);
            }
        }

        static void NormalizeSchoolWindows(GameObject school) {
            Material glass = AssetDatabase.LoadAssetAtPath<Material>("Assets/Rosvik/GeneratedV90/glass.mat");
            if (!glass) return;
            foreach (Renderer r in school.GetComponentsInChildren<Renderer>(true)) {
                if (!r) continue;
                string n = r.gameObject.name.ToLowerInvariant();
                if (n == "window glass") {
                    r.sharedMaterial = glass;
                    EditorUtility.SetDirty(r);
                }
            }
        }

        static void RemoveStrayFacadeBlocks(GameObject school) {
            foreach (Transform t in school.GetComponentsInChildren<Transform>(true)) {
                if (!t || !t.gameObject) continue;
                string n = t.name.ToLowerInvariant();
                // Old cube benches read as unexplained floating rectangles from the game camera.
                if (n == "bench l" || n == "bench r") UnityEngine.Object.DestroyImmediate(t.gameObject);
            }
        }

        static Transform FindTransform(Transform root, string exact) {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == exact) return t;
            return null;
        }

        static GameObject FindDeep(Transform root, string exact) {
            Transform t = FindTransform(root, exact);
            return t ? t.gameObject : null;
        }

        static void CleanupMissingScripts() {
            foreach (GameObject g in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (g && g.scene.IsValid()) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(g);
        }
    }
}
#endif
