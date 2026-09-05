#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Rosvik.Blackout.EditorTools {
    /// <summary>
    /// One-shot migration guard. V20 is the new authoritative scene, but a script reload
    /// that happens while Play Mode is active can leave the previously saved V19 scene open.
    /// Keep waiting until the editor is idle, then force exactly one clean V20 rebuild.
    /// </summary>
    [InitializeOnLoad]
    public static class RosvikForceDioramaV21 {
        const string MigrationKey = "ROSVIK_FORCE_DIORAMA_V21_DONE";

        static RosvikForceDioramaV21() {
            EditorApplication.update -= TryMigrate;
            EditorApplication.update += TryMigrate;
        }

        [DidReloadScripts]
        static void AfterReload() {
            EditorApplication.update -= TryMigrate;
            EditorApplication.update += TryMigrate;
        }

        [MenuItem("Rosvik/Force Diorama V20 Now")]
        public static void ForceNow() {
            EditorPrefs.DeleteKey(MigrationKey);
            EditorPrefs.DeleteKey("ROSVIK_DIORAMA_VERSION");
            TryMigrate();
        }

        static void TryMigrate() {
            if (EditorPrefs.GetInt(MigrationKey, 0) == 1) {
                EditorApplication.update -= TryMigrate;
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            GameObject current = GameObject.Find("ROSVIK_DIORAMA_GAME_V20");
            if (current != null) {
                EditorPrefs.SetInt(MigrationKey, 1);
                EditorApplication.update -= TryMigrate;
                return;
            }

            // V20 may have been skipped because its first delayed callback happened during Play Mode.
            // Clear only its own guard and rebuild from the real Rosvik map data.
            EditorPrefs.DeleteKey("ROSVIK_DIORAMA_VERSION");
            RosvikDioramaV20.Build();

            if (GameObject.Find("ROSVIK_DIORAMA_GAME_V20") != null) {
                EditorPrefs.SetInt(MigrationKey, 1);
                EditorApplication.update -= TryMigrate;
                Debug.Log("ROSVIK V21 MIGRATION: V20 diorama is now the authoritative scene.");
            }
        }
    }
}
#endif
