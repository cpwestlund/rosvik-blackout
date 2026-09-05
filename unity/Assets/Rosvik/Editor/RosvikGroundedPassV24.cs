#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rosvik.Blackout.EditorTools {
    /// <summary>
    /// V24 cleans the successful grounded V23 scene without rebuilding geography.
    /// It removes the obvious macro ground blobs, suppresses the duplicate V23 warning path,
    /// and keeps the close playable camera + animated character intact.
    /// </summary>
    [InitializeOnLoad]
    public static class RosvikGroundedPassV24 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_GROUNDED_PASS_V24_VERSION";
        const int Version = 24;
        const string RootName = "ROSVIK_CLEAN_GROUNDED_V24";

        static RosvikGroundedPassV24() {
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        [MenuItem("Rosvik/Rebuild Clean Grounded V24")]
        public static void Force() {
            EditorPrefs.DeleteKey(Key);
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        static void TryApply() {
            if (EditorPrefs.GetInt(Key, 0) >= Version && File.Exists(ScenePath)) {
                EditorApplication.update -= TryApply;
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;
            if (!File.Exists(ScenePath)) return;

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject root = GameObject.Find("ROSVIK_GROUNDED_PASS_V23")
                           ?? GameObject.Find("ROSVIK_HERO_SLICE_V22")
                           ?? GameObject.Find(RootName);
            if (!root) return;

            EditorApplication.update -= TryApply;
            Apply(scene, root);
        }

        static void Apply(UnityEngine.SceneManagement.Scene scene, GameObject root) {
            try {
                // V23's macro blobs were useful to break texture repetition, but from gameplay height
                // they read as large paper cut-outs. Remove them completely and keep variation in materials.
                Transform macro = Find(root.transform, "08 GROUND VARIATION");
                if (macro) UnityEngine.Object.DestroyImmediate(macro.gameObject);

                SoftenGroundMaterial("Assets/Rosvik/GeneratedV20/mat_ground.mat", .15f);
                SoftenGroundMaterial("Assets/Rosvik/GeneratedV20/mat_grass.mat", .16f);
                SoftenGroundMaterial("Assets/Rosvik/GeneratedV20/mat_forest_floor.mat", .17f);

                Camera cam = Camera.main;
                if (cam) {
                    cam.orthographic = true;
                    cam.orthographicSize = 8.6f;
                    cam.backgroundColor = new Color(.285f,.315f,.285f);
                    IsometricCameraRig rig = cam.GetComponent<IsometricCameraRig>();
                    if (rig) {
                        rig.pitch = 39f;
                        rig.yaw = 38f;
                        rig.orthographicSize = 8.6f;
                        rig.minSize = 6.5f;
                        rig.maxSize = 14f;
                        rig.zoomStep = .65f;
                        rig.followSharpness = 12f;
                    }
                }

                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(.43f,.445f,.39f);
                foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) {
                    if (light.type != LightType.Directional) continue;
                    light.color = new Color(.94f,.88f,.76f);
                    light.intensity = 1.12f;
                    light.shadows = LightShadows.Soft;
                    light.transform.rotation = Quaternion.Euler(48f,-33f,0f);
                }

                // Retire the duplicate V23 play-pass path so it cannot emit the warning seen in the Console.
                EditorPrefs.SetInt("ROSVIK_PLAY_PASS_VERSION", 23);
                EditorPrefs.SetInt("ROSVIK_GROUNDED_PASS_VERSION", 23);

                root.name = RootName;
                EditorPrefs.SetInt(Key, Version);
                Selection.activeObject = null;
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
                Debug.Log("ROSVIK V24: grounded V23 cleaned; macro ground blobs removed, camera/light stabilized, duplicate V23 warning retired.");
            } catch (Exception ex) {
                Debug.LogError("ROSVIK V24 FAILED: " + ex);
            }
        }

        static void SoftenGroundMaterial(string path, float tiling) {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) return;
            Vector2 scale = Vector2.one * tiling;
            if (m.HasProperty("_BaseMap")) m.SetTextureScale("_BaseMap", scale);
            if (m.HasProperty("_MainTex")) m.SetTextureScale("_MainTex", scale);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", .035f);
            EditorUtility.SetDirty(m);
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
