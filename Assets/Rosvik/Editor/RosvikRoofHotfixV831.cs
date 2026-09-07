#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Rosvik.Blackout;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikRoofHotfixV831 {
        const int Version = 831;
        const string Key = "ROSVIK_ROOF_HOTFIX_V831";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string ParentName = "V83 ASTRA REFERENCE POLISH";
        const string MatDir = "Assets/Rosvik/GeneratedV83";

        static RosvikRoofHotfixV831() {
            if (EditorPrefs.GetInt(Key, 0) >= Version) return;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V83.1 FIX ROOFS + CUTAWAY")]
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
            catch (Exception ex) { Debug.LogError("V83.1 ROOF FIX FAILED: " + ex); }
        }

        static void Apply() {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            CoziPlayerV57 player = UnityEngine.Object.FindFirstObjectByType<CoziPlayerV57>();
            if (!player) throw new Exception("PLAYER missing");

            GameObject parent = GameObject.Find(ParentName);
            if (!parent) parent = new GameObject(ParentName);

            DestroyNamed(parent.transform, "HOUSE A ROOF");
            DestroyNamed(parent.transform, "HOUSE B ROOF");

            Shader shader = PickShader();
            Material roof = Mat(shader, "roof_red_dark", "4f3031");
            Material edge = Mat(shader, "roof_edge", "292d2e");
            Material snow = Mat(shader, "roof_snow", "aab9bd");

            BuildRoof(parent.transform, "HOUSE A ROOF",
                new Vector2(-36.4f, 2.2f), new Vector2(-21.6f, 16.1f),
                new Vector3(-29f, 0f, 9.15f), 14.9f, 14.2f, roof, edge, snow);

            BuildRoof(parent.transform, "HOUSE B ROOF",
                new Vector2(-36.3f, 22.15f), new Vector2(-21.7f, 35.75f),
                new Vector3(-29f, 0f, 28.9f), 14.7f, 13.7f, roof, edge, snow);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorPrefs.SetInt(Key, Version);
            SceneView.RepaintAll();
            Debug.Log("V83.1 COMPLETE — roofs rebuilt right-side-up and robust cutaway installed.");
        }

        static void BuildRoof(Transform parent, string name, Vector2 min, Vector2 max, Vector3 center,
            float width, float depth, Material roof, Material edge, Material snow) {

            GameObject mgr = new GameObject(name);
            mgr.transform.SetParent(parent, true);

            GameObject visual = new GameObject("ROOF VISUAL");
            visual.transform.SetParent(mgr.transform, true);

            const float pitch = 14f;
            float halfRun = width * .5f;
            float panelLength = halfRun / Mathf.Cos(pitch * Mathf.Deg2Rad) + .16f;
            float xOffset = halfRun * .5f;
            float eaveY = 1.34f;
            float rise = halfRun * Mathf.Tan(pitch * Mathf.Deg2Rad);
            float panelY = eaveY + rise * .5f;
            float ridgeY = eaveY + rise;

            // Correct gable: inner edges rise toward the ridge, outer edges fall toward the eaves.
            GameObject left = Box("left roof", visual.transform,
                center + new Vector3(-xOffset, panelY, 0f),
                new Vector3(panelLength, .16f, depth + .48f), roof);
            left.transform.rotation = Quaternion.Euler(0f, 0f, pitch);

            GameObject right = Box("right roof", visual.transform,
                center + new Vector3(xOffset, panelY, 0f),
                new Vector3(panelLength, .16f, depth + .48f), roof);
            right.transform.rotation = Quaternion.Euler(0f, 0f, -pitch);

            GameObject snowLeft = Box("snow left", visual.transform,
                center + new Vector3(-xOffset, panelY + .11f, 0f),
                new Vector3(panelLength - .12f, .045f, depth + .22f), snow);
            snowLeft.transform.rotation = left.transform.rotation;

            GameObject snowRight = Box("snow right", visual.transform,
                center + new Vector3(xOffset, panelY + .11f, 0f),
                new Vector3(panelLength - .12f, .045f, depth + .22f), snow);
            snowRight.transform.rotation = right.transform.rotation;

            Box("ridge", visual.transform, center + new Vector3(0f, ridgeY + .06f, 0f),
                new Vector3(.18f, .16f, depth + .60f), edge);

            Box("front fascia", visual.transform, center + new Vector3(0f, eaveY - .04f, -depth * .5f - .25f),
                new Vector3(width + .35f, .13f, .12f), edge);
            Box("rear fascia", visual.transform, center + new Vector3(0f, eaveY - .04f, depth * .5f + .25f),
                new Vector3(width + .35f, .13f, .12f), edge);

            RoofCutawayV831 cut = mgr.AddComponent<RoofCutawayV831>();
            cut.roofVisual = visual;
            cut.minXZ = min;
            cut.maxXZ = max;
            cut.inset = .35f;
            EditorUtility.SetDirty(cut);
        }

        static void DestroyNamed(Transform root, string target) {
            if (!root) return;
            for (int i = root.childCount - 1; i >= 0; i--) {
                Transform c = root.GetChild(i);
                if (c && c.name == target) UnityEngine.Object.DestroyImmediate(c.gameObject);
            }
        }

        static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material) {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.SetParent(parent, true);
            g.transform.position = position;
            g.transform.localScale = scale;
            Renderer r = g.GetComponent<Renderer>(); if (r) r.sharedMaterial = material;
            Collider c = g.GetComponent<Collider>(); if (c) UnityEngine.Object.DestroyImmediate(c);
            return g;
        }

        static Shader PickShader() {
            bool srp = GraphicsSettings.currentRenderPipeline != null || GraphicsSettings.defaultRenderPipeline != null;
            Shader s = srp ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
            if (!s || !s.isSupported) s = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (!s || !s.isSupported) s = Shader.Find("Standard");
            if (!s || !s.isSupported) throw new Exception("No supported shader");
            return s;
        }

        static Material Mat(Shader shader, string name, string hex) {
            Directory.CreateDirectory(MatDir);
            string path = MatDir + "/" + name + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            Color c = Color.white; ColorUtility.TryParseHtmlString("#" + hex, out c);
            if (!m) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            if (m.shader != shader) m.shader = shader;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", .08f);
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
#endif
