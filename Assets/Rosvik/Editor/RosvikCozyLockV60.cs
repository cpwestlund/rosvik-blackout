#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikCozyLockV60 {
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string LockKey = "ROSVIK_V60_COZY_SCENE_LOCK";
        static double nextCheck;
        static int stableChecks;

        static RosvikCozyLockV60() {
            // The reboot is now the authoritative work scene. Keep the old InitializeOnLoad
            // passes from silently reopening RosvikHero after V58/V59 has created the new game.
            if (!EditorPrefs.HasKey(LockKey)) EditorPrefs.SetInt(LockKey, 1);
            nextCheck = EditorApplication.timeSinceStartup + 1.0;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        [MenuItem("Rosvik/V60 OPEN + LOCK COZY GAME", priority = 1)]
        public static void OpenAndLock() {
            EditorPrefs.SetInt(LockKey, 1);
            stableChecks = 0;
            nextCheck = 0;
            EnsureCozyScene(true);
        }

        [MenuItem("Rosvik/V60 Disable Cozy Scene Lock", priority = 2)]
        public static void DisableLock() {
            EditorPrefs.SetInt(LockKey, 0);
            Debug.Log("V60: Cozy scene lock disabled.");
        }

        static void Tick() {
            if (EditorPrefs.GetInt(LockKey, 1) == 0) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.timeSinceStartup < nextCheck) return;
            nextCheck = EditorApplication.timeSinceStartup + 0.8;
            EnsureCozyScene(false);
        }

        static void EnsureCozyScene(bool forced) {
            try {
                if (!File.Exists(ScenePath)) {
                    stableChecks = 0;
                    if (forced) {
                        Debug.Log("V60: CozySchoolGame does not exist yet; running V58 builder now.");
                        RosvikCozyRebootV58.Force();
                    }
                    return;
                }

                var active = EditorSceneManager.GetActiveScene();
                if (active.path != ScenePath) {
                    stableChecks = 0;
                    EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                    SceneView.RepaintAll();
                    Debug.Log("V60 LOCK: CozySchoolGame is now active. A legacy RosvikHero pass tried to own the editor and was overridden.");
                    return;
                }

                stableChecks++;
                // Keep monitoring indefinitely but at a relaxed cadence after the scene stays stable.
                if (stableChecks > 8) nextCheck = EditorApplication.timeSinceStartup + 2.5;
            }
            catch (Exception ex) {
                Debug.LogError("V60 cozy scene lock failed: " + ex);
                stableChecks = 0;
            }
        }
    }
}
#endif
