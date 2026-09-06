#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikMissingScriptCleanupV761 {
        const int Version = 761;
        const string Key = "ROSVIK_MISSING_SCRIPT_CLEANUP_V761";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";

        static RosvikMissingScriptCleanupV761() {
            if (EditorPrefs.GetInt(Key,0) >= Version) return;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V76.1 CLEAN MISSING SCRIPTS")]
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
            catch (Exception ex) { Debug.LogError("V76.1 missing-script cleanup failed: " + ex); }
        }

        static void Apply() {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Scene scene = EditorSceneManager.GetActiveScene();
            int removed = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
                removed += CleanRecursive(root.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            EditorPrefs.SetInt(Key, Version);
            SceneView.RepaintAll();
            Debug.Log("V76.1 COMPLETE — removed " + removed + " missing MonoBehaviour component(s) from CozySchoolGame.");
        }

        static int CleanRecursive(Transform t) {
            int removed = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
            if (removed > 0) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            for (int i = 0; i < t.childCount; i++) removed += CleanRecursive(t.GetChild(i));
            return removed;
        }
    }
}
#endif
