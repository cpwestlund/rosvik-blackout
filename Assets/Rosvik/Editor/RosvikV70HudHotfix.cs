#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Rosvik.Blackout;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikV70HudHotfix {
        const int Version = 701;
        const string Key = "ROSVIK_V70_HUD_HOTFIX_701";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";

        static RosvikV70HudHotfix() {
            if (EditorPrefs.GetInt(Key, 0) >= Version) return;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V70.1 FIX DOUBLE HUD")]
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
            catch (Exception ex) { Debug.LogError("V70.1 HUD HOTFIX FAILED: " + ex); }
        }

        static void Apply() {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            CoziPlayerV57 player = UnityEngine.Object.FindFirstObjectByType<CoziPlayerV57>();
            if (!player) throw new Exception("CoziPlayerV57 not found");

            SurvivalPresentationV70 presentation = player.GetComponent<SurvivalPresentationV70>();
            if (!presentation) presentation = player.gameObject.AddComponent<SurvivalPresentationV70>();

            SurvivalLegacyHudMaskV70 mask = player.GetComponent<SurvivalLegacyHudMaskV70>();
            if (!mask) mask = player.gameObject.AddComponent<SurvivalLegacyHudMaskV70>();

            EditorUtility.SetDirty(player);
            EditorUtility.SetDirty(presentation);
            EditorUtility.SetDirty(mask);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorPrefs.SetInt(Key, Version);
            SceneView.RepaintAll();
            Debug.Log("V70.1 DOUBLE HUD FIX COMPLETE — legacy V57/V69 HUD is masked behind the polished V70 presentation.");
        }
    }
}
#endif
