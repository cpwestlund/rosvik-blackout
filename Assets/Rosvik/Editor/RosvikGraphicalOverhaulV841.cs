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
    public static class RosvikGraphicalOverhaulV841 {
        const int Version = 841;
        const string Key = "ROSVIK_GRAPHICAL_OVERHAUL_V841";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string MatDir = "Assets/Rosvik/GeneratedV841";

        static RosvikGraphicalOverhaulV841() {
            if (EditorPrefs.GetInt(Key, 0) >= Version) return;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V84.1 FIX HOUSE SILHOUETTES + ROOFS")]
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
            catch (Exception ex) { Debug.LogError("V84.1 FAILED: " + ex); }
        }

        static void Apply() {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // V84 is now the owner of house/world presentation. Remove the old V83 scene dressing
            // so snow mounds, roof fragments and yard props cannot stack underneath the new pass.
            GameObject legacyV83 = GameObject.Find("V83 ASTRA REFERENCE POLISH");
            if (legacyV83) UnityEngine.Object.DestroyImmediate(legacyV83);

            Shader shader = PickShader();
            Material roof = Mat(shader, "roof_dark_red", "493635", .08f);
            Material edge = Mat(shader, "roof_edge", "273033", .04f);
            Material snow = Mat(shader, "roof_snow", "b5c2c6", .16f);
            Material facade = Mat(shader, "gable_facade", "76504a", .10f);
            Material soffit = Mat(shader, "soffit", "b8b3a2", .10f);

            RebuildHouse("HOUSE A V84", new Vector3(-29f, 0f, 9.10f), 14.8f, 13.9f, roof, edge, snow, facade, soffit);
            RebuildHouse("HOUSE B V84", new Vector3(-29f, 0f, 28.95f), 14.6f, 13.7f, roof, edge, snow, facade, soffit);

            CleanupMissingScripts();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorPrefs.SetInt(Key, Version);
            SceneView.RepaintAll();
            Debug.Log("V84.1 COMPLETE — low solid snowy gable roofs, filled gable ends and legacy V83 dressing removed.");
        }

        static void RebuildHouse(string houseName, Vector3 center, float width, float depth,
            Material roof, Material edge, Material snow, Material facade, Material soffit) {
            GameObject house = GameObject.Find(houseName);
            if (!house) throw new Exception(houseName + " missing");

            Transform oldRoof = house.transform.Find("ROOF VISUAL");
            if (oldRoof) UnityEngine.Object.DestroyImmediate(oldRoof.gameObject);

            GameObject visual = new GameObject("ROOF VISUAL");
            visual.transform.SetParent(house.transform, true);
            BuildLowGable(visual.transform, center, width, depth, roof, edge, snow, facade, soffit);

            HouseEnvelopeV84 env = house.GetComponent<HouseEnvelopeV84>();
            if (!env) env = house.AddComponent<HouseEnvelopeV84>();
            env.roofVisual = visual;
            EditorUtility.SetDirty(env);
        }

        static void BuildLowGable(Transform p, Vector3 center, float width, float depth,
            Material roof, Material edge, Material snow, Material facade, Material soffit) {
            const float pitch = 7.0f;
            const float overhang = .22f;
            const float eaveY = 1.52f;

            float roofW = width + overhang * 2f;
            float roofD = depth + overhang * 2f;
            float halfRun = roofW * .5f;
            float rad = pitch * Mathf.Deg2Rad;
            float rise = halfRun * Mathf.Tan(rad);
            float ridgeY = eaveY + rise;
            float panelLength = halfRun / Mathf.Cos(rad);
            float xOffset = halfRun * .5f;
            float panelY = eaveY + rise * .5f;

            // Solid roof panels. Their thickness hides the skeletal/see-through eave that the V84 mesh produced.
            GameObject left = Box("left solid roof", p,
                center + new Vector3(-xOffset, panelY, 0f),
                new Vector3(panelLength + .10f, .18f, roofD), roof, Quaternion.Euler(0f, 0f, pitch));
            GameObject right = Box("right solid roof", p,
                center + new Vector3(xOffset, panelY, 0f),
                new Vector3(panelLength + .10f, .18f, roofD), roof, Quaternion.Euler(0f, 0f, -pitch));

            // Snow is slightly inset so a dark roof line remains visible around the silhouette.
            float snowLength = panelLength - .12f;
            float snowDepth = roofD - .18f;
            Box("snow left", p,
                center + new Vector3(-xOffset + .02f, panelY + .105f, 0f),
                new Vector3(snowLength, .042f, snowDepth), snow, left.transform.rotation);
            Box("snow right", p,
                center + new Vector3(xOffset - .02f, panelY + .105f, 0f),
                new Vector3(snowLength, .042f, snowDepth), snow, right.transform.rotation);

            // Filled gable ends are essential in this camera angle; without them the house reads like an open roof truss.
            TriangleGable("front gable", p, center, roofW - .20f, eaveY - .01f, ridgeY - .05f, -roofD * .5f + .10f, facade);
            TriangleGable("rear gable", p, center, roofW - .20f, eaveY - .01f, ridgeY - .05f, roofD * .5f - .10f, facade);

            // A pale soffit closes the underside at the visible eaves.
            Box("west soffit", p, center + new Vector3(-roofW*.5f+.05f, eaveY-.10f, 0f), new Vector3(.22f,.10f,roofD-.10f), soffit, Quaternion.identity);
            Box("east soffit", p, center + new Vector3(roofW*.5f-.05f, eaveY-.10f, 0f), new Vector3(.22f,.10f,roofD-.10f), soffit, Quaternion.identity);

            Box("ridge cap", p, center + new Vector3(0f, ridgeY + .055f, 0f), new Vector3(.17f,.11f,roofD+.04f), edge, Quaternion.identity);

            // Clean bargeboards trace the actual pitch instead of making a heavy triangular frame.
            float barY = eaveY + rise*.5f;
            float barX = roofW*.25f;
            foreach (float z in new[] { center.z - roofD*.5f - .005f, center.z + roofD*.5f + .005f }) {
                Box("barge left", p, new Vector3(center.x-barX, barY, z), new Vector3(panelLength,.09f,.10f), edge, Quaternion.Euler(0f,0f,pitch));
                Box("barge right", p, new Vector3(center.x+barX, barY, z), new Vector3(panelLength,.09f,.10f), edge, Quaternion.Euler(0f,0f,-pitch));
            }

            // Chimney is kept low and broad so it reads from the orthographic camera without dominating the roof.
            Vector3 chimney = center + new Vector3(-2.05f, ridgeY + .30f, 2.10f);
            Box("chimney", p, chimney, new Vector3(.58f,.82f,.58f), facade, Quaternion.identity);
            Box("chimney cap", p, chimney + Vector3.up*.44f, new Vector3(.70f,.10f,.70f), snow, Quaternion.identity);
        }

        static void TriangleGable(string name, Transform parent, Vector3 center, float width, float eaveY, float ridgeY, float z, Material mat) {
            float hw = width * .5f;
            Mesh m = new Mesh();
            m.name = name + " mesh";
            m.vertices = new[] {
                new Vector3(center.x-hw, eaveY, z),
                new Vector3(center.x+hw, eaveY, z),
                new Vector3(center.x, ridgeY, z)
            };
            // Both windings so the gable is visible from either side regardless of material culling.
            m.triangles = new[] { 0,1,2, 2,1,0 };
            m.uv = new[] { new Vector2(0,0), new Vector2(1,0), new Vector2(.5f,1) };
            m.RecalculateNormals();
            m.RecalculateBounds();
            GameObject g = new GameObject(name);
            g.transform.SetParent(parent, true);
            MeshFilter mf = g.AddComponent<MeshFilter>(); mf.sharedMesh = m;
            MeshRenderer mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat;
        }

        static void CleanupMissingScripts() {
            foreach (GameObject g in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (g && g.scene.IsValid()) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(g);
        }

        static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material, Quaternion rotation) {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.SetParent(parent, true);
            g.transform.position = position;
            g.transform.rotation = rotation;
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

        static Material Mat(Shader shader, string name, string hex, float smooth) {
            Directory.CreateDirectory(MatDir);
            string path = MatDir + "/" + name + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            Color c = Color.white; ColorUtility.TryParseHtmlString("#" + hex, out c);
            if (!m) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            if (m.shader != shader) m.shader = shader;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
#endif
