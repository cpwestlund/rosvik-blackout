#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Rosvik.Blackout.EditorTools {
    /// <summary>
    /// V27 is a repair pass for V26 only. The bright magenta geometry in V26 is Unity's
    /// missing/unsupported shader fallback. This pass rebinds every V26 material to a
    /// shader already proven to render in the active project. It also removes the
    /// naive fan-triangulated school-yard overlay, because the real school boundary is
    /// concave and a centroid fan can create giant wedges outside the polygon.
    /// Geography, buildings, roads and roofs are untouched.
    /// </summary>
    [InitializeOnLoad]
    public static class RosvikShaderSafetyV27 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_SHADER_SAFETY_V27_VERSION";
        const int Version = 27;
        const string RootName = "ROSVIK_SHADER_SAFE_V27";
        const string V26Dir = "Assets/Rosvik/GeneratedV26";
        const string ProvenMaterial = "Assets/Rosvik/GeneratedV20/mat_ground.mat";

        struct Spec {
            public string file;
            public Color color;
            public float smooth;
            public Spec(string f, Color c, float s) { file=f; color=c; smooth=s; }
        }

        static readonly Spec[] Specs = {
            new Spec("mat_schoolyard.mat",      new Color(.235f,.245f,.22f),  .10f),
            new Spec("mat_parking.mat",        new Color(.18f,.19f,.18f),    .22f),
            new Spec("mat_playground.mat",     new Color(.31f,.29f,.23f),    .08f),
            new Spec("mat_fence.mat",          new Color(.10f,.105f,.095f),  .12f),
            new Spec("mat_window.mat",         new Color(.075f,.12f,.14f),   .48f),
            new Spec("mat_frame.mat",          new Color(.68f,.68f,.60f),    .18f),
            new Spec("mat_traskola_trim.mat",  new Color(.80f,.79f,.70f),    .16f),
            new Spec("mat_door.mat",           new Color(.16f,.15f,.125f),   .18f),
        };

        static RosvikShaderSafetyV27() {
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        [MenuItem("Rosvik/Repair V26 Pink Materials (V27)")]
        public static void Force() {
            EditorPrefs.DeleteKey(Key);
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        static bool Busy() => EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating;

        static void TryApply() {
            if (EditorPrefs.GetInt(Key,0) >= Version && File.Exists(ScenePath)) {
                EditorApplication.update -= TryApply;
                return;
            }
            if (Busy() || !File.Exists(ScenePath)) return;

            Scene scene = EditorSceneManager.GetActiveScene();
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
            return GameObject.Find("ROSVIK_SCHOOL_WORLD_V26")
                ?? GameObject.Find("ROSVIK_PLAYABLE_V25")
                ?? GameObject.Find("ROSVIK_CLEAN_GROUNDED_V24")
                ?? GameObject.Find(RootName);
        }

        static void Apply(Scene scene, GameObject root) {
            try {
                Shader shader = ResolveProvenShader();
                if (!shader || !shader.isSupported)
                    throw new InvalidOperationException("V27 could not resolve a supported project shader.");

                foreach (Spec spec in Specs)
                    RepairMaterial(V26Dir + "/" + spec.file, shader, spec.color, spec.smooth);

                // Remove only the V26 school-boundary colour overlay. The underlying Rosvik
                // ground remains intact. The V26 PolygonFan method is not safe for concave
                // polygons, which is why the screenshot showed a large triangular wedge.
                Transform v26 = Find(root.transform, "10 SCHOOL WORLD V26");
                if (v26) {
                    Transform[] badSchoolOverlays = v26.GetComponentsInChildren<Transform>(true)
                        .Where(t => t != v26 && t.name == "mapped school grounds")
                        .ToArray();
                    foreach (Transform t in badSchoolOverlays)
                        UnityEngine.Object.DestroyImmediate(t.gameObject);

                    // Rebind scene renderers immediately; this also fixes objects already
                    // serialized with the old pink material instance.
                    foreach (Renderer r in v26.GetComponentsInChildren<Renderer>(true)) {
                        if (!r || r.sharedMaterials == null) continue;
                        Material[] mats = r.sharedMaterials;
                        bool changed = false;
                        for (int i=0;i<mats.Length;i++) {
                            Material m = mats[i];
                            if (!m) continue;
                            string p = AssetDatabase.GetAssetPath(m);
                            if (p.StartsWith(V26Dir, StringComparison.OrdinalIgnoreCase)) {
                                Material repaired = AssetDatabase.LoadAssetAtPath<Material>(p);
                                if (repaired) { mats[i] = repaired; changed = true; }
                            }
                        }
                        if (changed) r.sharedMaterials = mats;
                    }
                }

                root.name = RootName;
                EditorPrefs.SetInt(Key, Version);
                Selection.activeObject = null;
                EditorUtility.SetDirty(root);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                if (!Busy()) {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene, ScenePath);
                }
                Debug.Log("ROSVIK V27: V26 magenta shaders repaired with a proven project shader; invalid concave schoolyard overlay removed. Geography unchanged.");
            } catch (Exception ex) {
                Debug.LogError("ROSVIK V27 FAILED: " + ex);
            }
        }

        static Shader ResolveProvenShader() {
            Material proven = AssetDatabase.LoadAssetAtPath<Material>(ProvenMaterial);
            if (proven && proven.shader && proven.shader.isSupported)
                return proven.shader;

            Shader s = null;
            if (GraphicsSettings.defaultRenderPipeline != null) {
                s = Shader.Find("Universal Render Pipeline/Lit");
                if (!s || !s.isSupported) s = Shader.Find("Universal Render Pipeline/Simple Lit");
            } else {
                s = Shader.Find("Standard");
            }
            if (!s || !s.isSupported) s = Shader.Find("Sprites/Default");
            return s;
        }

        static void RepairMaterial(string path, Shader shader, Color color, float smoothness) {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) return;

            m.shader = shader;
            m.color = color;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(m);
        }

        static Transform Find(Transform root, string name) {
            if (!root) return null;
            if (root.name == name) return root;
            for (int i=0;i<root.childCount;i++) {
                Transform hit = Find(root.GetChild(i), name);
                if (hit) return hit;
            }
            return null;
        }
    }
}
#endif
