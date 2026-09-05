#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikSiteCalibrationV11 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string CalibrationKey = "ROSVIK_SITE_CALIBRATION_VERSION";
        const int Version = 11;

        static RosvikSiteCalibrationV11() => EditorApplication.delayCall += Apply;

        [MenuItem("Rosvik/Apply Site Calibration V11")]
        public static void Apply() {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorPrefs.GetInt(CalibrationKey, 0) >= Version && File.Exists(ScenePath)) return;

            try {
                RosvikAutoSetup.Ensure();
                if (!File.Exists(ScenePath)) return;

                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                Transform root = FindRoot();
                if (!root) {
                    Debug.LogWarning("ROSVIK V11: base Rosvik scene root not found yet; will retry on next domain reload.");
                    return;
                }

                Transform main = Child(root, "Huvudbyggnaden - 1970-tal");
                Transform timber = Child(root, "Träskolan - ca 1900");
                Transform stone = Child(root, "Stenskolan - 1940/50-tal");
                Transform module = Child(root, "Tillfällig skolmodul");
                Transform arena = Child(root, "Norrbotten Stål Arena - background landmark");

                Material packed = Mat("V11 packed snow", new Color(.65f,.70f,.72f), .08f);
                Material concrete = Mat("V11 concrete", new Color(.43f,.45f,.46f), .07f);
                Material dark = Mat("V11 dark trim", new Color(.12f,.14f,.15f), .16f);
                Material brick = Mat("V11 70s brick", new Color(.27f,.26f,.23f), .06f);
                Material white = Mat("V11 old white trim", new Color(.84f,.84f,.79f), .12f);
                Material stoneBand = Mat("V11 stone base", new Color(.34f,.35f,.34f), .06f);
                Material warm = Mat("V11 warm lamp", new Color(.98f,.62f,.31f), .28f);

                Move(root, "Snow terrain", new Vector3(0f,-.20f,0f), new Vector3(145f,.40f,112f));
                Move(root, "Skolgränd road", new Vector3(0f,.015f,31f), new Vector3(105f,.08f,8.2f));
                Move(root, "Main cleared yard", new Vector3(-3f,.045f,18f), new Vector3(76f,.06f,16f));

                DestroyChild(root, "Path to old school");
                DestroyChild(root, "Path to stone school");

                Cube("Large central schoolyard", root, new Vector3(0f,.055f,-9f), new Vector3(80f,.07f,32f), packed, true);
                Cube("West play yard", root, new Vector3(-32f,.050f,-7f), new Vector3(20f,.06f,25f), packed, true);

                float rise = .72f;
                float run = 27f;
                float angle = Mathf.Atan2(rise, run) * Mathf.Rad2Deg;
                SlopedCube("Broad uphill transition", root, new Vector3(-25f,.39f,-21f), new Vector3(24f,.20f,run), angle, packed, true);
                SlopedCube("Path uphill to Träskolan", root, new Vector3(-28f,.43f,-21f), new Vector3(5.4f,.14f,run+2f), angle, concrete, true);
                Cube("Upper Träskolan terrace", root, new Vector3(-30f,.62f,-36f), new Vector3(34f,.18f,21f), packed, true);

                SlopedCube("Gentle path to Stenskolan", root, new Vector3(25f,.18f,-19f), new Vector3(5.2f,.13f,21f), .7f, concrete, true);
                Cube("Stenskolan terrace", root, new Vector3(29f,.22f,-30f), new Vector3(30f,.16f,18f), packed, true);

                if (main) {
                    main.localPosition = new Vector3(0f,.08f,6f);
                    main.localScale = new Vector3(1.08f,1f,1.03f);
                    AddSeventiesFacadeDetail(main, brick, dark);
                }
                if (timber) {
                    timber.localPosition = new Vector3(-30f,.72f,-36f);
                    timber.localRotation = Quaternion.Euler(0f,-7f,0f);
                    AddTimberFacadeDetail(timber, white);
                }
                if (stone) {
                    stone.localPosition = new Vector3(29f,.30f,-30f);
                    stone.localRotation = Quaternion.Euler(0f,5f,0f);
                    AddStoneFacadeDetail(stone, stoneBand, dark);
                }
                if (module) module.localPosition = new Vector3(41f,.08f,5f);

                if (arena) {
                    arena.localPosition = new Vector3(58f,0f,-52f);
                    arena.localScale = new Vector3(1.22f,1f,1.16f);
                }

                Sandbox(root, new Vector3(-8f,.16f,-13f), white);
                LampPost(root, new Vector3(-18f,0f,15f), dark, warm);
                LampPost(root, new Vector3(8f,0f,15f), dark, warm);
                LampPost(root, new Vector3(-17f,.28f,-25f), dark, warm);
                LampPost(root, new Vector3(18f,.12f,-23f), dark, warm);

                int driftIndex = 0;
                foreach (Transform t in root) {
                    if (!t.name.StartsWith("Ploughed snow bank", StringComparison.Ordinal)) continue;
                    t.localPosition = new Vector3(-40f + driftIndex * 12f, .28f, 25.8f);
                    t.localScale = new Vector3(4.8f,.62f,1.20f);
                    driftIndex++;
                }

                Transform player = Child(root, "PLAYER_PLACEHOLDER");
                if (player) player.localPosition = new Vector3(-12f,.12f,18f);
                Camera cam = Camera.main;
                if (cam) cam.orthographicSize = 13.5f;
                IsometricCameraRig rig = UnityEngine.Object.FindFirstObjectByType<IsometricCameraRig>();
                if (rig) rig.orthographicSize = 13.5f;

                root.name = "ROSVIK_SITE_CALIBRATION_V11";
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
                EditorPrefs.SetInt(CalibrationKey, Version);
                Debug.Log("ROSVIK V11 APPLIED: larger schoolyard, real spacing, uphill Träskolan transition and differentiated facades.");
            } catch (Exception ex) {
                Debug.LogError("ROSVIK V11 CALIBRATION FAILED: " + ex);
            }
        }

        static Transform FindRoot() {
            GameObject exact = GameObject.Find("ROSVIK_REAL_CAMPUS_V10");
            if (exact) return exact.transform;
            exact = GameObject.Find("ROSVIK_REAL_CAMPUS_V9");
            if (exact) return exact.transform;
            exact = GameObject.Find("ROSVIK_SITE_CALIBRATION_V11");
            return exact ? exact.transform : null;
        }

        static Transform Child(Transform root, string name) {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        static void DestroyChild(Transform root, string name) {
            Transform t = Child(root, name);
            if (t) UnityEngine.Object.DestroyImmediate(t.gameObject);
        }

        static void Move(Transform root, string name, Vector3 pos, Vector3 scale) {
            Transform t = Child(root, name);
            if (!t) return;
            t.localPosition = pos;
            t.localScale = scale;
        }

        static Material Mat(string name, Color color, float smooth) {
            Shader shader = GraphicsSettings.defaultRenderPipeline != null
                ? Shader.Find("Universal Render Pipeline/Lit")
                : Shader.Find("Standard");
            if (!shader || !shader.isSupported) shader = Shader.Find("Sprites/Default");
            Material mat = new Material(shader) { name = name, color = color };
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smooth);
            return mat;
        }

        static GameObject Cube(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool collider) {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static GameObject SlopedCube(string name, Transform parent, Vector3 pos, Vector3 scale, float angle, Material mat, bool collider) {
            GameObject go = Cube(name,parent,pos,scale,mat,collider);
            go.transform.localRotation = Quaternion.Euler(angle,0f,0f);
            return go;
        }

        static void AddSeventiesFacadeDetail(Transform b, Material brick, Material trim) {
            DestroyChild(b, "V11 brick base band");
            Cube("V11 brick base band", b, new Vector3(0f,.62f,5.47f), new Vector3(35f,1.10f,.10f), brick, false);
            Cube("V11 horizontal facade band", b, new Vector3(0f,3.00f,5.49f), new Vector3(35f,.12f,.08f), trim, false);
        }

        static void AddTimberFacadeDetail(Transform b, Material white) {
            DestroyChild(b, "V11 timber eave band");
            Cube("V11 timber eave band", b, new Vector3(0f,5.72f,4.34f), new Vector3(14.5f,.16f,.10f), white, false);
            for (float x=-6.2f; x<=6.2f; x+=1.55f)
                Cube("V11 vertical timber batten", b, new Vector3(x,3.2f,4.34f), new Vector3(.045f,5.1f,.045f), white, false);
        }

        static void AddStoneFacadeDetail(Transform b, Material baseMat, Material trim) {
            DestroyChild(b, "V11 stone base band");
            Cube("V11 stone base band", b, new Vector3(0f,.58f,4.14f), new Vector3(17.5f,1.0f,.10f), baseMat, false);
            Cube("V11 stone upper band", b, new Vector3(0f,3.68f,4.15f), new Vector3(17.5f,.12f,.08f), trim, false);
        }

        static void Sandbox(Transform root, Vector3 center, Material edge) {
            Cube("Sandbox N",root,center+new Vector3(0f,0f,3f),new Vector3(8f,.25f,.35f),edge,false);
            Cube("Sandbox S",root,center+new Vector3(0f,0f,-3f),new Vector3(8f,.25f,.35f),edge,false);
            Cube("Sandbox W",root,center+new Vector3(-4f,0f,0f),new Vector3(.35f,.25f,6f),edge,false);
            Cube("Sandbox E",root,center+new Vector3(4f,0f,0f),new Vector3(.35f,.25f,6f),edge,false);
        }

        static void LampPost(Transform root, Vector3 pos, Material metal, Material lightMat) {
            Transform p = new GameObject("V11 lamp post").transform;
            p.SetParent(root);
            p.localPosition = pos;
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(p);
            pole.transform.localPosition = new Vector3(0f,1.9f,0f);
            pole.transform.localScale = new Vector3(.065f,1.9f,.065f);
            pole.GetComponent<Renderer>().sharedMaterial = metal;
            UnityEngine.Object.DestroyImmediate(pole.GetComponent<Collider>());
            Cube("Head",p,new Vector3(0f,3.74f,.12f),new Vector3(.54f,.15f,.34f),metal,false);
            Cube("Lamp",p,new Vector3(0f,3.67f,.14f),new Vector3(.38f,.04f,.22f),lightMat,false);
        }
    }
}
#endif
