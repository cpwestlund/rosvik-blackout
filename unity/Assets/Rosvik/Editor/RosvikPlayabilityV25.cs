#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Rosvik.Blackout.EditorTools {
    /// <summary>
    /// V25 is intentionally small: keep V24 geography/assets intact, fix the close-game
    /// framing and let the runtime character driver handle any bind/T-pose fallback.
    /// </summary>
    [InitializeOnLoad]
    public static class RosvikPlayabilityV25 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_PLAYABILITY_V25_VERSION";
        const int Version = 25;
        const string RootName = "ROSVIK_PLAYABLE_V25";

        static RosvikPlayabilityV25() {
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        [MenuItem("Rosvik/Rebuild Playability V25")]
        public static void Force() {
            EditorPrefs.DeleteKey(Key);
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        static bool EditorBusyOrPlaying() {
            return EditorApplication.isPlaying
                || EditorApplication.isPlayingOrWillChangePlaymode
                || EditorApplication.isCompiling
                || EditorApplication.isUpdating;
        }

        static void TryApply() {
            if (EditorPrefs.GetInt(Key, 0) >= Version && File.Exists(ScenePath)) {
                EditorApplication.update -= TryApply;
                return;
            }
            if (EditorBusyOrPlaying() || !File.Exists(ScenePath)) return;

            // Prefer the scene that is already open. Calling EditorSceneManager.OpenScene while
            // Unity is crossing into Play Mode causes the InvalidOperationException seen in V25.
            Scene scene = EditorSceneManager.GetActiveScene();
            GameObject root = FindRoot();

            if (!root) {
                if (EditorBusyOrPlaying()) return;
                if (scene.path != ScenePath) {
                    scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                    if (EditorBusyOrPlaying()) return;
                }
                root = FindRoot();
            }
            if (!root || EditorBusyOrPlaying()) return;

            EditorApplication.update -= TryApply;
            Apply(scene, root);
        }

        static GameObject FindRoot() {
            return GameObject.Find("ROSVIK_CLEAN_GROUNDED_V24")
                ?? GameObject.Find("ROSVIK_GROUNDED_PASS_V23")
                ?? GameObject.Find("ROSVIK_HERO_SLICE_V22")
                ?? GameObject.Find(RootName);
        }

        static void Apply(Scene scene, GameObject root) {
            try {
                if (EditorBusyOrPlaying()) return;

                Transform player = Find(root.transform, "PLAYER");
                Camera cam = Camera.main;
                if (cam) {
                    cam.orthographic = true;
                    cam.orthographicSize = 9.25f;
                    cam.backgroundColor = new Color(.275f,.305f,.275f);
                    cam.nearClipPlane = .05f;
                    cam.farClipPlane = 500f;

                    IsometricCameraRig rig = cam.GetComponent<IsometricCameraRig>();
                    if (rig) {
                        if (player) rig.target = player;
                        // A steeper angle keeps the school readable without its roof swallowing
                        // the upper third of the game view, while retaining the 2.5D feel.
                        rig.pitch = 49f;
                        rig.yaw = 38f;
                        rig.orthographicSize = 9.25f;
                        rig.minSize = 6.7f;
                        rig.maxSize = 15.5f;
                        rig.zoomStep = .70f;
                        rig.focusOffset = new Vector3(0f,.75f,0f);
                        rig.followSharpness = 12f;
                    }
                }

                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(.44f,.455f,.405f);
                foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) {
                    if (light.type != LightType.Directional) continue;
                    light.color = new Color(.94f,.885f,.78f);
                    light.intensity = 1.10f;
                    light.shadows = LightShadows.Soft;
                    light.transform.rotation = Quaternion.Euler(50f,-34f,0f);
                }

                RosvikMapHUD hud = root.GetComponent<RosvikMapHUD>();
                if (hud) hud.maxLabelDistance = 52f;

                root.name = RootName;
                EditorPrefs.SetInt(Key, Version);
                Selection.activeObject = null;

                if (!EditorBusyOrPlaying()) {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene, ScenePath);
                    AssetDatabase.SaveAssets();
                }
                Debug.Log("ROSVIK V25: gameplay camera reframed and character T-pose fallback enabled. Geography/assets unchanged.");
            } catch (Exception ex) {
                Debug.LogError("ROSVIK V25 FAILED: " + ex);
            }
        }

        static Transform Find(Transform root, string name) {
            if (!root) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++) {
                Transform hit = Find(root.GetChild(i), name);
                if (hit) return hit;
            }
            return null;
        }
    }
}
#endif
