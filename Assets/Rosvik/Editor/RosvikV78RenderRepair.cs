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
    public static class RosvikV78RenderRepair {
        const int Version = 781;
        const string Key = "ROSVIK_V78_RENDER_REPAIR_781";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string MatDir = "Assets/Rosvik/GeneratedV78";
        const string GroupName = "V78 WORLD - GARAGE + VEGETATION";

        static readonly Dictionary<string,string> Palette = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase) {
            {"ground","2e4437"}, {"grass_dark","24382d"},
            {"pine","2f4b39"}, {"pine_dark","23382d"}, {"bark","564536"},
            {"birch","8f8a7d"}, {"branch","4f463c"}, {"snow","aeb7b1"},
            {"road","555754"}, {"road_edge","77766f"}, {"concrete","626865"},
            {"garage_wall","948a78"}, {"trim","c8bea7"}, {"wood","75543b"},
            {"metal","505a59"}, {"dark_metal","303838"}, {"rust","744735"},
            {"glass","4e6a70"}, {"warm","c59650"}
        };

        static RosvikV78RenderRepair() {
            if (EditorPrefs.GetInt(Key, 0) >= Version) return;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V78.1 FIX PINK + WHITE VEGETATION")]
        public static void Force() {
            EditorPrefs.DeleteKey(Key);
            EditorApplication.delayCall += Auto;
        }

        static void Auto() {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) {
                EditorApplication.delayCall += Auto;
                return;
            }
            if (!File.Exists(ScenePath)) return;
            try { Apply(); }
            catch (Exception ex) { Debug.LogError("V78.1 RENDER REPAIR FAILED: " + ex); }
        }

        static void Apply() {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Shader shader = PickShader();
            if (!shader || !shader.isSupported) throw new Exception("No supported shader for active render pipeline.");

            int materials = 0;
            if (Directory.Exists(MatDir)) {
                foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { MatDir })) {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (!m) continue;
                    RepairMaterial(m, shader, Path.GetFileNameWithoutExtension(path));
                    materials++;
                }
            }

            GameObject group = GameObject.Find(GroupName);
            if (group) {
                foreach (Renderer r in group.GetComponentsInChildren<Renderer>(true)) {
                    if (!r) continue;
                    Material[] mats = r.sharedMaterials;
                    bool dirty = false;
                    for (int i = 0; i < mats.Length; i++) {
                        Material m = mats[i];
                        if (!m) continue;
                        string path = AssetDatabase.GetAssetPath(m);
                        if (!path.StartsWith(MatDir, StringComparison.OrdinalIgnoreCase)) continue;
                        RepairMaterial(m, shader, Path.GetFileNameWithoutExtension(path));
                        dirty = true;
                    }
                    if (dirty) EditorUtility.SetDirty(r);
                }

                // Winter birches looked like white blobs from the camera. Keep the pale trunk,
                // but remove the spherical snow crown so they read as bare northern birches.
                foreach (Transform t in group.GetComponentsInChildren<Transform>(true)) {
                    if (!t || !string.Equals(t.gameObject.name, "björk", StringComparison.OrdinalIgnoreCase)) continue;
                    foreach (Renderer r in t.GetComponentsInChildren<Renderer>(true)) {
                        if (!r || r.transform == t) continue;
                        string matPath = r.sharedMaterial ? AssetDatabase.GetAssetPath(r.sharedMaterial) : "";
                        if (r.gameObject.name.IndexOf("crown", StringComparison.OrdinalIgnoreCase) >= 0 &&
                            matPath.IndexOf("snow", StringComparison.OrdinalIgnoreCase) >= 0) {
                            r.gameObject.SetActive(false);
                        }
                    }
                }
            }

            // Clean stale MonoBehaviour references as well; V78 screenshot showed one remaining.
            int missing = 0;
            foreach (GameObject go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                if (!go || !go.scene.IsValid()) continue;
                missing += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            EditorPrefs.SetInt(Key, Version);
            SceneView.RepaintAll();
            Debug.Log("V78.1 COMPLETE — repaired " + materials + " V78 materials with '" + shader.name + "', removed " + missing + " missing scripts, and changed birches from white snow-balls to bare winter trees.");
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

        static void RepairMaterial(Material m, Shader shader, string materialName) {
            if (!m) return;
            if (m.shader != shader) m.shader = shader;

            string key = materialName;
            if (Palette.TryGetValue(key, out string hex)) {
                Color c = Color.white;
                if (ColorUtility.TryParseHtmlString("#" + hex, out c)) {
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
                    if (m.HasProperty("_Color")) m.SetColor("_Color", c);
                }
            }

            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 0f);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 1f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", key == "snow" ? .10f : .18f);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", (key.Contains("metal") || key == "rust") ? .22f : 0f);
            m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHABLEND_ON");
            m.renderQueue = -1;
            EditorUtility.SetDirty(m);
        }
    }
}
#endif
