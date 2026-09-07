#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikPremiumSchoolAlignmentV972 {
        const int Version = 972;
        const string Key = "ROSVIK_PREMIUM_SCHOOL_ALIGNMENT_V972";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string RootName = "V97 PREMIUM SCHOOL + INVENTORY";
        const string Generated = "Assets/Rosvik/GeneratedV972";
        static int retries;

        static RosvikPremiumSchoolAlignmentV972() {
            if (EditorPrefs.GetInt(Key, 0) >= Version) return;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V97.2 ALIGN PREMIUM SCHOOL + KILL PINK")]
        public static void Force() {
            EditorPrefs.DeleteKey(Key);
            retries = 0;
            EditorApplication.delayCall += Auto;
        }

        static void Auto() {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) {
                EditorApplication.delayCall += Auto;
                return;
            }
            if (!File.Exists(ScenePath)) return;
            try {
                if (!Apply() && retries++ < 20) EditorApplication.delayCall += Auto;
            } catch (Exception ex) {
                Debug.LogError("V97.2 ALIGNMENT FAILED: " + ex);
            }
        }

        static bool Apply() {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject root = GameObject.Find(RootName);
            if (!root) return false;

            // V93 moved the authored V92 school from (-10,-4) onto the OSM school anchor (0,7.35).
            // V97 was authored in the old V92 coordinates, so move the entire interior/presentation root
            // by the exact same correction instead of letting rooms sit outside the building.
            root.transform.position = new Vector3(10f, 0f, 11.35f);
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            // Floor-written room names were useful while blocking rooms out, but look like debug art.
            foreach (TextMesh tm in root.GetComponentsInChildren<TextMesh>(true)) {
                if (tm && (tm.text == "KLASSRUM" || tm.text == "LÄSRUM" || tm.text == "PERSONALRUM"))
                    UnityEngine.Object.DestroyImmediate(tm.gameObject);
            }

            Directory.CreateDirectory(Generated);
            Shader shader = PickShader();
            if (!shader || !shader.isSupported) throw new Exception("No supported scene shader found.");

            // Sanitize every material used by V97 into a local, pipeline-compatible copy.
            // This avoids touching shared third-party materials elsewhere in the project.
            Dictionary<Material, Material> replacements = new Dictionary<Material, Material>();
            int swapped = 0;
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true)) {
                if (!r) continue;
                Material[] mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++) {
                    Material src = mats[i];
                    if (!src) continue;
                    if (!replacements.TryGetValue(src, out Material dst)) {
                        dst = MakeSafeCopy(src, shader, replacements.Count);
                        replacements[src] = dst;
                    }
                    if (dst && mats[i] != dst) {
                        mats[i] = dst;
                        changed = true;
                        swapped++;
                    }
                }
                if (changed) {
                    r.sharedMaterials = mats;
                    EditorUtility.SetDirty(r);
                }
            }

            EditorUtility.SetDirty(root.transform);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorPrefs.SetInt(Key, Version);
            SceneView.RepaintAll();
            Debug.Log("V97.2 COMPLETE — premium school aligned to the V93 OSM campus, debug room labels removed, and " + swapped + " renderer material slots replaced with safe local pipeline copies.");
            return true;
        }

        static Shader PickShader() {
            bool srp = GraphicsSettings.currentRenderPipeline != null || GraphicsSettings.defaultRenderPipeline != null;
            Shader s = null;
            if (srp) {
                s = Shader.Find("Universal Render Pipeline/Lit");
                if (!s || !s.isSupported) s = Shader.Find("Universal Render Pipeline/Simple Lit");
            } else s = Shader.Find("Standard");
            if (!s || !s.isSupported) {
                s = Shader.Find("Universal Render Pipeline/Lit");
                if (!s || !s.isSupported) s = Shader.Find("Universal Render Pipeline/Simple Lit");
                if (!s || !s.isSupported) s = Shader.Find("Standard");
            }
            return s;
        }

        static Material MakeSafeCopy(Material src, Shader shader, int index) {
            string srcPath = AssetDatabase.GetAssetPath(src);
            string seed = string.IsNullOrEmpty(srcPath) ? src.name : srcPath + "_" + src.name;
            string safe = Sanitize(src.name);
            string path = Generated + "/" + index.ToString("D3") + "_" + safe + ".mat";
            Material dst = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!dst) {
                dst = new Material(shader) { name = "V972 " + src.name };
                AssetDatabase.CreateAsset(dst, path);
            } else dst.shader = shader;

            Color color = Color.white;
            if (src.HasProperty("_BaseColor")) color = src.GetColor("_BaseColor");
            else if (src.HasProperty("_Color")) color = src.GetColor("_Color");
            Texture tex = null;
            if (src.HasProperty("_BaseMap")) tex = src.GetTexture("_BaseMap");
            if (!tex && src.HasProperty("_MainTex")) tex = src.GetTexture("_MainTex");
            float metallic = src.HasProperty("_Metallic") ? src.GetFloat("_Metallic") : 0f;
            float smooth = src.HasProperty("_Smoothness") ? src.GetFloat("_Smoothness") : .25f;
            Color emission = src.HasProperty("_EmissionColor") ? src.GetColor("_EmissionColor") : Color.black;

            if (dst.HasProperty("_BaseColor")) dst.SetColor("_BaseColor", color);
            if (dst.HasProperty("_Color")) dst.SetColor("_Color", color);
            if (tex) {
                if (dst.HasProperty("_BaseMap")) dst.SetTexture("_BaseMap", tex);
                if (dst.HasProperty("_MainTex")) dst.SetTexture("_MainTex", tex);
            }
            if (dst.HasProperty("_Metallic")) dst.SetFloat("_Metallic", metallic);
            if (dst.HasProperty("_Smoothness")) dst.SetFloat("_Smoothness", smooth);
            if (dst.HasProperty("_EmissionColor")) {
                if (emission.maxColorComponent > .001f) {
                    dst.EnableKeyword("_EMISSION");
                    dst.SetColor("_EmissionColor", emission);
                } else {
                    dst.DisableKeyword("_EMISSION");
                    dst.SetColor("_EmissionColor", Color.black);
                }
            }
            EditorUtility.SetDirty(dst);
            return dst;
        }

        static string Sanitize(string s) {
            if (string.IsNullOrWhiteSpace(s)) return "material";
            foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            s = s.Replace('/', '_').Replace('\\', '_').Replace(':', '_');
            return s.Length > 70 ? s.Substring(0, 70) : s;
        }
    }
}
#endif
