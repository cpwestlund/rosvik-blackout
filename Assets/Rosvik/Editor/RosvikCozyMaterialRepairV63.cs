#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikCozyMaterialRepairV63 {
        const string Key = "ROSVIK_COZY_MATERIAL_REPAIR_V63";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string GeneratedDir = "Assets/Rosvik/GeneratedV58";

        static RosvikCozyMaterialRepairV63() {
            if (EditorPrefs.GetInt(Key, 0) >= 63) return;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V63 FIX PINK COZY MATERIALS")]
        public static void Force() {
            EditorPrefs.DeleteKey(Key);
            EditorApplication.delayCall += Auto;
        }

        static void Auto() {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) {
                EditorApplication.delayCall += Auto;
                return;
            }

            try {
                Shader shader = PickShader();
                if (!shader) throw new Exception("No compatible Lit/Standard shader could be found.");

                int repaired = 0;
                if (Directory.Exists(GeneratedDir)) {
                    foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { GeneratedDir })) {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
                        if (!m) continue;
                        Repair(m, shader);
                        repaired++;
                    }
                }

                // Also repair any V58 materials currently referenced in the scene, including
                // materials that may have been instantiated/imported during an interrupted rebuild.
                if (File.Exists(ScenePath) && EditorSceneManager.GetActiveScene().path != ScenePath)
                    EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

                foreach (Renderer r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                    Material[] mats = r.sharedMaterials;
                    bool changed = false;
                    for (int i = 0; i < mats.Length; i++) {
                        Material m = mats[i];
                        if (!m) continue;
                        string path = AssetDatabase.GetAssetPath(m);
                        bool isV58 = path.StartsWith(GeneratedDir, StringComparison.OrdinalIgnoreCase) ||
                                     m.name.IndexOf("V58", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     m.name.IndexOf("KayKit Furniture", StringComparison.OrdinalIgnoreCase) >= 0;
                        if (!isV58) continue;
                        Repair(m, shader);
                        changed = true;
                    }
                    if (changed) EditorUtility.SetDirty(r);
                }

                AssetDatabase.SaveAssets();
                if (EditorSceneManager.GetActiveScene().path == ScenePath)
                    EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

                EditorPrefs.SetInt(Key, 63);
                SceneView.RepaintAll();
                Debug.Log("V63 MATERIAL REPAIR: repaired " + repaired + " generated cozy materials with shader '" + shader.name + "'. Pink surfaces should now be gone.");
            }
            catch (Exception ex) {
                Debug.LogError("V63 MATERIAL REPAIR FAILED: " + ex);
            }
        }

        static Shader PickShader() {
            bool srp = GraphicsSettings.currentRenderPipeline != null || GraphicsSettings.defaultRenderPipeline != null;
            Shader s = null;
            if (srp) {
                s = Shader.Find("Universal Render Pipeline/Lit");
                if (!s || !s.isSupported) s = Shader.Find("Universal Render Pipeline/Simple Lit");
            } else {
                s = Shader.Find("Standard");
            }
            if (!s || !s.isSupported) {
                s = Shader.Find("Universal Render Pipeline/Lit");
                if (!s || !s.isSupported) s = Shader.Find("Universal Render Pipeline/Simple Lit");
                if (!s || !s.isSupported) s = Shader.Find("Standard");
            }
            return s;
        }

        static void Repair(Material m, Shader shader) {
            Color color = Color.white;
            if (m.HasProperty("_BaseColor")) color = m.GetColor("_BaseColor");
            else if (m.HasProperty("_Color")) color = m.GetColor("_Color");

            Texture tex = null;
            if (m.HasProperty("_BaseMap")) tex = m.GetTexture("_BaseMap");
            else if (m.HasProperty("_MainTex")) tex = m.GetTexture("_MainTex");

            float metallic = m.HasProperty("_Metallic") ? m.GetFloat("_Metallic") : 0f;
            float smooth = m.HasProperty("_Smoothness") ? m.GetFloat("_Smoothness") : .2f;
            Color emission = m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor") : Color.black;

            if (m.shader != shader) m.shader = shader;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            if (tex) {
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
                if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            }
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (m.HasProperty("_EmissionColor") && emission.maxColorComponent > 0.001f) {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission);
            }
            EditorUtility.SetDirty(m);
        }
    }
}
#endif
