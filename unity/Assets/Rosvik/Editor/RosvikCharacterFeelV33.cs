#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UScene = UnityEngine.SceneManagement.Scene;

namespace Rosvik.Blackout.EditorTools {
    /// <summary>
    /// V33 is deliberately narrow: keep the now-approved V32 controls/camera/world intact
    /// and only tune the character's secondary motion so starts, stops and gait cadence feel
    /// more grounded. No geography, WASD mapping or zoom behaviour is changed here.
    /// </summary>
    [InitializeOnLoad]
    public static class RosvikCharacterFeelV33 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_CHARACTER_FEEL_V33_VERSION";
        const int Version = 33;
        const string RootName = "ROSVIK_CHARACTER_FEEL_V33";

        static RosvikCharacterFeelV33() {
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        [MenuItem("Rosvik/Rebuild Character Feel V33")]
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
            return GameObject.Find("ROSVIK_PLAY_COMFORT_V32")
                ?? GameObject.Find("ROSVIK_CHARACTER_ATMOSPHERE_V31")
                ?? GameObject.Find(RootName);
        }

        static void Apply(UScene scene, GameObject root) {
            Transform player = Find(root.transform, "PLAYER");
            if (!player) {
                Debug.LogError("ROSVIK V33 FAILED: PLAYER not found.");
                return;
            }

            Rosvik.Blackout.RosvikCharacterDriver driver = player.GetComponent<Rosvik.Blackout.RosvikCharacterDriver>();
            if (!driver) driver = player.gameObject.AddComponent<Rosvik.Blackout.RosvikCharacterDriver>();

            Animator animator = player.GetComponentInChildren<Animator>(true);
            if (animator) {
                driver.animator = animator;
                driver.visualRoot = animator.transform;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }

            Rosvik.Blackout.RosvikPlayerController controller = player.GetComponent<Rosvik.Blackout.RosvikPlayerController>();
            if (controller) driver.fullSpeed = controller.sprintSpeed;

            // The values are intentionally restrained. V32 already feels good to control;
            // V33 only removes the remaining rigid/skating read from the imported humanoid.
            driver.animationDamp = .085f;
            driver.forwardLean = 2.65f;
            driver.turnLean = 3.25f;
            driver.accelerationLean = 1.55f;
            driver.idleBreath = .0045f;
            driver.movingBob = .0048f;
            driver.brakingDip = .0085f;
            driver.strideCyclesPerMeter = .36f;
            driver.fallbackBob = .015f;
            driver.fallbackLean = 1.0f;
            driver.enabled = true;

            root.name = RootName;
            EditorPrefs.SetInt(Key, Version);
            EditorUtility.SetDirty(driver);
            EditorUtility.SetDirty(root);
            AssetDatabase.SaveAssets();
            if (!Busy()) {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            Debug.Log("ROSVIK V33: character cadence now follows travelled distance, with subtler acceleration/braking/turn response. V32 movement axes, zoom and geography remain unchanged.");
        }

        static Transform Find(Transform root, string name) {
            if (!root) return null;
            if (root.name == name) return root;
            foreach (Transform child in root) {
                Transform found = Find(child, name);
                if (found) return found;
            }
            return null;
        }
    }
}
#endif
