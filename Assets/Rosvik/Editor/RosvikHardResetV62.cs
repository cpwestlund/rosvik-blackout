#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikHardResetV62 {
        const string StageKey = "ROSVIK_HARD_RESET_V62_STAGE";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string EditorDir = "Assets/Rosvik/Editor";
        const string QuarantineDir = "Library/RosvikLegacyEditorV62";

        static readonly string[] Keep = {
            "RosvikCozyRebootV58.cs",
            "RosvikHardResetV62.cs"
        };

        static RosvikHardResetV62() {
            EditorApplication.delayCall -= Run;
            EditorApplication.delayCall += Run;
        }

        [MenuItem("Rosvik/V62 HARD RESET - REMOVE OLD + REBUILD COZY GAME")]
        public static void Force() {
            EditorPrefs.SetInt(StageKey, 0);
            EditorApplication.delayCall -= Run;
            EditorApplication.delayCall += Run;
        }

        static void Run() {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) {
                EditorApplication.delayCall += Run;
                return;
            }

            int stage = EditorPrefs.GetInt(StageKey, 0);
            try {
                if (stage == 0) {
                    int moved = QuarantineLegacyEditorScripts();
                    EditorPrefs.SetInt(StageKey, 1);
                    if (moved > 0) {
                        Debug.Log("V62 HARD RESET: quarantined " + moved + " legacy editor scripts from Assets/Rosvik/Editor. Unity will recompile once, then rebuild the clean game.");
                        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                        return;
                    }
                    EditorApplication.delayCall += Run;
                    return;
                }

                if (stage == 1) {
                    // Deliberately throw away any partially generated/stale cozy scene and rebuild it
                    // now that legacy InitializeOnLoad scripts are no longer inside Assets.
                    if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                        AssetDatabase.DeleteAsset(ScenePath);
                    else {
                        if (File.Exists(ScenePath)) File.Delete(ScenePath);
                        if (File.Exists(ScenePath + ".meta")) File.Delete(ScenePath + ".meta");
                    }

                    EditorPrefs.SetInt(StageKey, 2);
                    Debug.Log("V62 HARD RESET: rebuilding CozySchoolGame from zero with only the new reboot builder active.");
                    RosvikCozyRebootV58.Force();
                    EditorApplication.delayCall += Run;
                    return;
                }

                if (!File.Exists(ScenePath)) {
                    Debug.LogWarning("V62: CozySchoolGame was not saved yet. Retrying the clean V58 builder once.");
                    RosvikCozyRebootV58.Force();
                    EditorApplication.delayCall += Run;
                    return;
                }

                if (EditorSceneManager.GetActiveScene().path != ScenePath)
                    EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

                GameObject player = GameObject.Find("PLAYER");
                if (player) {
                    Selection.activeGameObject = player;
                    SceneView view = SceneView.lastActiveSceneView;
                    if (view) {
                        view.LookAt(player.transform.position + new Vector3(0f, 0f, 5.0f), Quaternion.Euler(58f, 0f, 0f), 9.5f, false, false);
                        view.Repaint();
                    }
                }

                EditorPrefs.SetInt(StageKey, 3);
                Debug.Log("V62 HARD RESET COMPLETE: legacy editor scripts are outside Assets, CozySchoolGame was rebuilt from zero, and RosvikHero can no longer steal the editor scene.");
            }
            catch (Exception ex) {
                Debug.LogError("V62 HARD RESET FAILED: " + ex);
            }
        }

        static int QuarantineLegacyEditorScripts() {
            if (!Directory.Exists(EditorDir)) return 0;
            Directory.CreateDirectory(QuarantineDir);
            int moved = 0;

            foreach (string file in Directory.GetFiles(EditorDir, "*.cs", SearchOption.TopDirectoryOnly)) {
                string name = Path.GetFileName(file);
                if (Array.IndexOf(Keep, name) >= 0) continue;

                string destination = Path.Combine(QuarantineDir, name);
                try {
                    if (File.Exists(destination)) File.Delete(destination);
                    File.Move(file, destination);

                    string meta = file + ".meta";
                    if (File.Exists(meta)) {
                        string metaDestination = destination + ".meta";
                        if (File.Exists(metaDestination)) File.Delete(metaDestination);
                        File.Move(meta, metaDestination);
                    }
                    moved++;
                }
                catch (Exception ex) {
                    Debug.LogWarning("V62 could not quarantine " + name + ": " + ex.Message);
                }
            }
            return moved;
        }
    }
}
#endif
