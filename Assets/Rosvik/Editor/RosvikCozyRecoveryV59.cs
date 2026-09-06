#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikCozyRecoveryV59 {
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string FurnitureDir = "Assets/Rosvik/ThirdParty/V58Furniture";
        const string RecoveryKey = "ROSVIK_COZY_RECOVERY_V59_DONE";
        static double nextAttempt;
        static int attempts;

        static RosvikCozyRecoveryV59() {
            nextAttempt = EditorApplication.timeSinceStartup + 1.5;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        [MenuItem("Rosvik/V59 RECOVER + OPEN COZY GAME")]
        public static void ForceRecovery() {
            EditorPrefs.DeleteKey(RecoveryKey);
            attempts = 0;
            nextAttempt = 0;
            Tick(true);
        }

        static void Tick() { Tick(false); }

        static void Tick(bool forced) {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!forced && EditorApplication.timeSinceStartup < nextAttempt) return;

            try {
                if (File.Exists(ScenePath)) {
                    OpenScene();
                    EditorPrefs.SetInt(RecoveryKey, 59);
                    EditorApplication.update -= Tick;
                    return;
                }

                // V58 deliberately downloads real KayKit furniture. Import those files in a
                // separate editor pass so AssetDatabase refreshes cannot interrupt scene creation.
                bool importedAnything = ImportDownloadedFurniture();
                if (importedAnything) {
                    nextAttempt = EditorApplication.timeSinceStartup + 1.25;
                    return;
                }

                attempts++;
                Debug.Log("V59 recovery: CozySchoolGame scene is not present yet. Running the V58 builder again after its asset-import phase. Attempt " + attempts + ".");
                RosvikCozyRebootV58.Force();

                if (File.Exists(ScenePath)) {
                    OpenScene();
                    EditorPrefs.SetInt(RecoveryKey, 59);
                    EditorApplication.update -= Tick;
                    return;
                }

                // If an import/domain reload interrupted V58, this class is initialized again.
                // Without a reload, try again shortly. Cap only the current domain, not future reloads.
                nextAttempt = EditorApplication.timeSinceStartup + 2.0;
                if (attempts >= 4) {
                    Debug.LogWarning("V59 recovery is still waiting for V58 to finish. Use Rosvik > V59 RECOVER + OPEN COZY GAME once after Unity finishes importing.");
                    EditorApplication.update -= Tick;
                }
            }
            catch (Exception ex) {
                Debug.LogError("V59 cozy recovery failed: " + ex);
                nextAttempt = EditorApplication.timeSinceStartup + 2.0;
            }
        }

        static bool ImportDownloadedFurniture() {
            if (!Directory.Exists(FurnitureDir)) return false;
            bool didWork = false;

            string texture = FurnitureDir + "/furniturebits_texture.png";
            if (File.Exists(texture) && AssetDatabase.LoadAssetAtPath<Texture2D>(texture) == null) {
                AssetDatabase.ImportAsset(texture, ImportAssetOptions.ForceSynchronousImport);
                didWork = true;
            }

            string[] objs = Directory.GetFiles(FurnitureDir, "*.obj", SearchOption.TopDirectoryOnly);
            foreach (string file in objs) {
                string assetPath = file.Replace('\\','/');
                if (AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) != null) continue;
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
                didWork = true;
            }

            return didWork;
        }

        static void OpenScene() {
            string active = EditorSceneManager.GetActiveScene().path;
            if (active == ScenePath) return;
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SceneView.lastActiveSceneView?.FrameSelected();
            Debug.Log("V59 recovery: opened the NEW CozySchoolGame scene. The old RosvikHero scene is no longer the active working scene.");
        }
    }
}
#endif
