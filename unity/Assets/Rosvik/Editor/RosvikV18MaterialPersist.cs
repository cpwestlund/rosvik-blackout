#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikV18MaterialPersist {
        const string RootName = "ROSVIK_STYLIZED_MAP_V18";
        const string Folder = "Assets/Rosvik/Generated/V18Materials";

        static RosvikV18MaterialPersist() {
            // Run after scene builders have had a chance to finish.
            EditorApplication.delayCall += () => EditorApplication.delayCall += Persist;
        }

        static void Persist() {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            GameObject root = GameObject.Find(RootName);
            if (!root) return;

            EnsureFolder();
            var cache = new Dictionary<string,Material>();
            bool changed = false;

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true)) {
                Material mat = renderer.sharedMaterial;
                if (!mat || EditorUtility.IsPersistent(mat)) continue;

                Color c = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;
                string key = Safe(mat.name) + "_" + ColorUtility.ToHtmlStringRGBA(c);
                if (!cache.TryGetValue(key, out Material persistent)) {
                    string path = Folder + "/" + key + ".mat";
                    persistent = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (!persistent) {
                        persistent = new Material(mat) { name = mat.name };
                        AssetDatabase.CreateAsset(persistent, path);
                    }
                    cache[key] = persistent;
                }
                renderer.sharedMaterial = persistent;
                changed = true;
            }

            if (changed) {
                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                EditorSceneManager.SaveOpenScenes();
                Debug.Log("ROSVIK V18: generated materials persisted as project assets.");
            }
        }

        static void EnsureFolder() {
            if (!AssetDatabase.IsValidFolder("Assets/Rosvik/Generated")) AssetDatabase.CreateFolder("Assets/Rosvik", "Generated");
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Rosvik/Generated", "V18Materials");
        }

        static string Safe(string s) {
            if (string.IsNullOrWhiteSpace(s)) return "material";
            foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s.Replace('/', '_').Replace('\\', '_').Replace(':', '_').Trim();
        }
    }
}
#endif
