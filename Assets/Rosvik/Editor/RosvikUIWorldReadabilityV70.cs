#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Rosvik.Blackout;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikUIWorldReadabilityV70 {
        const int Version = 70;
        const string Key = "ROSVIK_UI_WORLD_READABILITY_V70";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";

        static RosvikUIWorldReadabilityV70() {
            if (EditorPrefs.GetInt(Key,0) >= Version) return;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V70 UI + WEATHER + FURNITURE READABILITY")]
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
            catch (Exception ex) { Debug.LogError("V70 READABILITY FAILED: " + ex); }
        }

        static void Apply() {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            CoziPlayerV57 player = UnityEngine.Object.FindFirstObjectByType<CoziPlayerV57>();
            if (!player) throw new Exception("CoziPlayerV57 missing from CozySchoolGame.");
            if (!player.GetComponent<SurvivalSystemsV69>()) throw new Exception("SurvivalSystemsV69 missing; V69 must exist first.");

            SurvivalPresentationV70[] presentations = player.GetComponents<SurvivalPresentationV70>();
            SurvivalPresentationV70 presentation = presentations.Length > 0 ? presentations[0] : player.gameObject.AddComponent<SurvivalPresentationV70>();
            for (int i=1;i<presentations.Length;i++) UnityEngine.Object.DestroyImmediate(presentations[i]);
            EditorUtility.SetDirty(presentation);

            // V69 generated storage: make every door face the room, never the wall.
            Pose("personalrummets skafferi V69", new Vector3(6.78f,0,8.05f), 90f);
            Pose("klassrum B mellanmål", new Vector3(-1.02f,0,13.45f), 90f);
            Pose("sporthall dryckesförråd", new Vector3(23.05f,0,13.35f), -90f);
            Pose("omklädningsskåp A", new Vector3(43.15f,0,2.45f), 90f);
            Pose("omklädningsskåp B", new Vector3(43.15f,0,4.05f), 90f);
            Pose("vaktmästarens klädskåp", new Vector3(17.15f,0,13.55f), 90f);

            // Wall furniture from the cozy passes. Left-wall furniture faces right into its room;
            // right-wall furniture faces left. This fixes the obvious backwards shelf/cabinet read.
            Yaw("class A side shelf", -90f);
            Yaw("class B side shelf", -90f);
            Yaw("staff wall shelf", -90f);
            Yaw("library low shelf L", -90f);
            Yaw("library low shelf R", 90f);

            // Keep teacher/student seating internally consistent: chair belongs behind the desk,
            // while storage stays wall-facing. No random rotations are introduced here.
            foreach (GameObject g in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                if (!g || !g.scene.IsValid()) continue;
                string n = g.name.ToLowerInvariant();
                if (n == "student chair") g.transform.rotation = Quaternion.Euler(0,0,0);
                if (n == "student desk") g.transform.rotation = Quaternion.Euler(0,0,0);
            }

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorPrefs.SetInt(Key,Version);
            SceneView.RepaintAll();
            Debug.Log("V70 COMPLETE — polished responsive inventory/HUD attached, legacy overlapping inventory suppressed at runtime, shader-safe snow/rain installed, and obvious backwards wall furniture corrected.");
        }

        static void Pose(string name, Vector3 pos, float yaw) {
            GameObject g = GameObject.Find(name);
            if (!g) { Debug.LogWarning("V70 could not find: " + name); return; }
            g.transform.position = pos;
            g.transform.rotation = Quaternion.Euler(0,yaw,0);
            EditorUtility.SetDirty(g.transform);
        }

        static void Yaw(string name, float yaw) {
            GameObject g = GameObject.Find(name);
            if (!g) return;
            Vector3 e = g.transform.eulerAngles;
            g.transform.rotation = Quaternion.Euler(e.x,yaw,e.z);
            EditorUtility.SetDirty(g.transform);
        }
    }
}
#endif
