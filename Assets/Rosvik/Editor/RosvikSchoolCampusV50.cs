#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UScene = UnityEngine.SceneManagement.Scene;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikSchoolCampusV50 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_SCHOOL_CAMPUS_V50_VERSION";
        const int Version = 50;
        const string V49Group = "31 SCHOOL CAMPUS V49 - TOP LEVEL VISIBLE REBUILD";
        const string GeneratedDir = "Assets/Rosvik/GeneratedV50";

        static RosvikSchoolCampusV50() {
            EditorApplication.delayCall -= Auto;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/Fix V49 Pink Materials V50")]
        public static void Force() {
            EditorPrefs.DeleteKey(Key);
            Run();
        }

        static void Auto() {
            if (EditorPrefs.GetInt(Key,0) >= Version) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) {
                EditorApplication.delayCall += Auto;
                return;
            }
            Run();
        }

        static void Run() {
            try {
                if (!File.Exists(ScenePath)) return;
                UScene scene = EditorSceneManager.GetActiveScene();
                if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

                GameObject v49 = scene.GetRootGameObjects().FirstOrDefault(g => g.name == V49Group);
                if (!v49) {
                    Debug.LogError("ROSVIK V50: V49 top-level group not found. Run V49 once first.");
                    return;
                }

                Shader goodShader = FindKnownGoodShader(scene, v49.transform);
                if (!goodShader || !goodShader.isSupported) {
                    Debug.LogError("ROSVIK V50: could not find any supported shader from the already-working scene materials.");
                    return;
                }

                Directory.CreateDirectory(GeneratedDir);
                AssetDatabase.Refresh();

                var fixedCache = new Dictionary<Material,Material>();
                Renderer[] renderers = v49.GetComponentsInChildren<Renderer>(true);
                int repaired = 0;

                foreach (Renderer r in renderers) {
                    Material[] src = r.sharedMaterials;
                    bool changed = false;
                    Material[] dst = new Material[src.Length];
                    for (int i=0;i<src.Length;i++) {
                        Material old = src[i];
                        if (!old) { dst[i] = old; continue; }
                        if (!fixedCache.TryGetValue(old, out Material repl)) {
                            repl = MakeCompatible(old, goodShader, fixedCache.Count);
                            fixedCache[old] = repl;
                        }
                        dst[i] = repl;
                        if (repl != old) changed = true;
                    }
                    if (changed) {
                        r.sharedMaterials = dst;
                        EditorUtility.SetDirty(r);
                        repaired++;
                    }
                }

                // Remove the diagnostic proof marker now that execution has been proven.
                Transform marker = v49.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "V49 TEMP PROOF MARKER");
                if (marker) UnityEngine.Object.DestroyImmediate(marker.gameObject);

                EditorPrefs.SetInt(Key,Version);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene,ScenePath);
                AssetDatabase.SaveAssets();
                SceneView.RepaintAll();

                Debug.Log("ROSVIK V50 SUCCESS: repaired " + repaired + " V49 renderers using known-good shader '" + goodShader.name + "'. Pink should be gone.");
            }
            catch(Exception ex) {
                Debug.LogError("ROSVIK V50 FAILED: " + ex);
            }
        }

        static Shader FindKnownGoodShader(UScene scene, Transform exclude) {
            // Do not guess the render pipeline. Reuse the shader of something Unity is already
            // rendering correctly in this exact scene.
            foreach (GameObject root in scene.GetRootGameObjects()) {
                if (root.transform == exclude) continue;
                foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true)) {
                    foreach (Material m in r.sharedMaterials) {
                        if (!m || !m.shader || !m.shader.isSupported) continue;
                        string sn = m.shader.name ?? "";
                        if (sn.IndexOf("InternalError", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                        if (sn.IndexOf("Hidden/", StringComparison.OrdinalIgnoreCase) == 0) continue;
                        return m.shader;
                    }
                }
            }

            Shader standard = Shader.Find("Standard");
            if (standard && standard.isSupported) return standard;
            Shader urp = Shader.Find("Universal Render Pipeline/Lit");
            if (urp && urp.isSupported) return urp;
            return null;
        }

        static Material MakeCompatible(Material old, Shader shader, int index) {
            string safe = Sanitize(old.name);
            string path = GeneratedDir + "/fixed_" + index.ToString("D2") + "_" + safe + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing) return existing;

            Material m = new Material(shader);
            m.name = "V50 " + old.name;

            Color c = Color.white;
            if (old.HasProperty("_BaseColor")) c = old.GetColor("_BaseColor");
            else if (old.HasProperty("_Color")) c = old.GetColor("_Color");
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor",c);
            if (m.HasProperty("_Color")) m.SetColor("_Color",c);

            Texture tex = null;
            if (old.HasProperty("_BaseMap")) tex = old.GetTexture("_BaseMap");
            if (!tex && old.HasProperty("_MainTex")) tex = old.GetTexture("_MainTex");
            if (tex) {
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap",tex);
                if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex",tex);
            }

            float smooth = .08f;
            if (old.HasProperty("_Smoothness")) smooth = old.GetFloat("_Smoothness");
            else if (old.HasProperty("_Glossiness")) smooth = old.GetFloat("_Glossiness");
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness",smooth);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness",smooth);

            if (old.IsKeywordEnabled("_EMISSION") && old.HasProperty("_EmissionColor") && m.HasProperty("_EmissionColor")) {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor",old.GetColor("_EmissionColor"));
            }

            AssetDatabase.CreateAsset(m,path);
            return m;
        }

        static string Sanitize(string s) {
            if (string.IsNullOrEmpty(s)) return "material";
            char[] bad = Path.GetInvalidFileNameChars();
            foreach(char c in bad) s = s.Replace(c,'_');
            return s.Replace('/','_').Replace('\\','_').Replace(':','_').Replace(' ','_');
        }
    }
}
#endif
