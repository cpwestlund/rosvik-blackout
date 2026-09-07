#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikPremiumMaterialRepairV971 {
        const int Version = 971;
        const string Key = "ROSVIK_PREMIUM_MATERIAL_REPAIR_V971";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string GeneratedDir = "Assets/Rosvik/GeneratedV97";
        const string RootName = "V97 PREMIUM SCHOOL + INVENTORY";

        static RosvikPremiumMaterialRepairV971() {
            if (EditorPrefs.GetInt(Key, 0) >= Version) return;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V97.1 FIX PREMIUM SCHOOL MATERIALS")]
        public static void Force() {
            EditorPrefs.DeleteKey(Key);
            EditorApplication.delayCall += Auto;
        }

        static void Auto() {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) {
                EditorApplication.delayCall += Auto;
                return;
            }
            try { Apply(); }
            catch (Exception ex) { Debug.LogError("V97.1 MATERIAL REPAIR FAILED: " + ex); }
        }

        static void Apply() {
            Shader shader = PickShader();
            if (!shader || !shader.isSupported) throw new Exception("No supported Lit shader found for the active render pipeline.");

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

            if (File.Exists(ScenePath) && EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject root = GameObject.Find(RootName);
            if (root) {
                foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true)) {
                    if (!r) continue;
                    Material[] mats = r.sharedMaterials;
                    bool changed = false;
                    for (int i = 0; i < mats.Length; i++) {
                        Material m = mats[i];
                        if (!m) continue;
                        string path = AssetDatabase.GetAssetPath(m);
                        bool generated = !string.IsNullOrEmpty(path) && path.StartsWith(GeneratedDir, StringComparison.OrdinalIgnoreCase);
                        bool broken = !m.shader || !m.shader.isSupported || m.shader.name.IndexOf("InternalError", StringComparison.OrdinalIgnoreCase) >= 0;
                        if (!generated && !broken) continue;
                        Repair(m, shader);
                        changed = true;
                    }
                    if (changed) EditorUtility.SetDirty(r);
                }
            }

            AssetDatabase.SaveAssets();
            if (EditorSceneManager.GetActiveScene().path == ScenePath)
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            EditorPrefs.SetInt(Key, Version);
            SceneView.RepaintAll();
            Debug.Log("V97.1 COMPLETE — repaired " + repaired + " premium-school materials with supported shader '" + shader.name + "'. Pink surfaces removed.");
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
            float smooth = m.HasProperty("_Smoothness") ? m.GetFloat("_Smoothness") : .25f;
            Color emission = m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor") : Color.black;

            m.shader = shader;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            if (tex) {
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
                if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            }
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (m.HasProperty("_EmissionColor") && emission.maxColorComponent > .001f) {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission);
            }
            EditorUtility.SetDirty(m);
        }
    }
}
#endif
