#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikCozyStabilizeV61 {
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string Key = "ROSVIK_COZY_STABILIZE_V61";

        static RosvikCozyStabilizeV61() {
            if (EditorPrefs.GetInt(Key, 0) >= 61) return;
            EditorApplication.delayCall += Stabilize;
        }

        [MenuItem("Rosvik/V61 OPEN CLEAN COZY GAME")]
        public static void Force() {
            EditorPrefs.DeleteKey(Key);
            EditorApplication.delayCall += Stabilize;
        }

        static void Stabilize() {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) {
                EditorApplication.delayCall += Stabilize;
                return;
            }

            if (!File.Exists(ScenePath)) {
                Debug.Log("V61: CozySchoolGame does not exist yet; running the V58 reboot builder once.");
                RosvikCozyRebootV58.Force();
            }

            if (!File.Exists(ScenePath)) {
                Debug.LogWarning("V61: CozySchoolGame is still not available. Wait for asset import to finish, then use Rosvik > V61 OPEN CLEAN COZY GAME once.");
                return;
            }

            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject player = GameObject.Find("PLAYER");
            if (player) {
                Selection.activeGameObject = player;
                SceneView view = SceneView.lastActiveSceneView;
                if (view) {
                    view.LookAt(player.transform.position + new Vector3(0f, 0f, 5.5f), Quaternion.Euler(58f, 0f, 0f), 10f, false, false);
                    view.Repaint();
                }
            }

            EditorPrefs.SetInt(Key, 61);
            Debug.Log("V61 CLEAN REBOOT: legacy V45-V56 editor passes and temporary V59/V60 loops are gone. CozySchoolGame is the only active reboot scene.");
        }
    }
}
#endif
