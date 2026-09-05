#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikAutoSetup {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const int BuildVersion = 8;
        const string BuildVersionKey = "ROSVIK_UNITY_BOOTSTRAP_VERSION";

        static RosvikAutoSetup() => EditorApplication.delayCall += Ensure;

        [MenuItem("Rosvik/Rebuild Hero Slice")]
        public static void Ensure() {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            try {
                if (EditorPrefs.GetInt(BuildVersionKey, 0) >= BuildVersion && File.Exists(ScenePath)) return;
                Directory.CreateDirectory("Assets/Rosvik/Scenes");
                BuildScene();
                EditorPrefs.SetInt(BuildVersionKey, BuildVersion);
                Debug.Log("ROSVIK UNITY EXTERIOR V8 BUILT: " + ScenePath);
            } catch (Exception ex) {
                Debug.LogError("ROSVIK UNITY SETUP FAILED: " + ex);
            }
        }

        static Material Mat(string name, Color color, float smooth = .18f) {
            Shader shader = GraphicsSettings.defaultRenderPipeline != null
                ? Shader.Find("Universal Render Pipeline/Lit")
                : Shader.Find("Standard");
            if (!shader || !shader.isSupported) shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader) { name = name, color = color };
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smooth);
            return mat;
        }

        static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool collider = true) {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static GameObject Cylinder(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool collider = false) {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static void Window(Transform parent, float x, float z, float yaw, Material frame, Material glass) {
            var root = new GameObject("Window").transform;
            root.SetParent(parent);
            root.localPosition = new Vector3(x, 1.85f, z);
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Cube("Frame", root, Vector3.zero, new Vector3(1.7f, 1.35f, .10f), frame, false);
            Cube("Glass", root, new Vector3(0f, 0f, -.061f), new Vector3(1.45f, 1.10f, .03f), glass, false);
            Cube("Mullion V", root, new Vector3(0f, 0f, -.10f), new Vector3(.07f, 1.22f, .05f), frame, false);
            Cube("Mullion H", root, new Vector3(0f, 0f, -.10f), new Vector3(1.55f, .07f, .05f), frame, false);
        }

        static void GableEnds(Transform parent, float width, float wallHeight, float depth, float rise, Material material) {
            float x = width * .5f + .01f;
            float z = depth * .5f;
            var go = new GameObject("Gable end walls");
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;

            var mesh = new Mesh { name = "Gable end mesh" };
            mesh.vertices = new[] {
                new Vector3(-x, wallHeight, -z), new Vector3(-x, wallHeight, z), new Vector3(-x, wallHeight + rise, 0f),
                new Vector3( x, wallHeight, -z), new Vector3( x, wallHeight + rise, 0f), new Vector3( x, wallHeight, z)
            };
            mesh.triangles = new[] { 0, 1, 2, 3, 4, 5 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        static void GabledBuilding(Transform parent, string name, Vector3 center, Vector3 size, Material wall, Material trim, Material roof, Material glass) {
            var b = new GameObject(name).transform;
            b.SetParent(parent);
            b.localPosition = center;

            float w = size.x;
            float h = size.y;
            float d = size.z;

            Cube("Main mass", b, new Vector3(0f, h * .5f, 0f), new Vector3(w, h, d), wall);
            Cube("Foundation", b, new Vector3(0f, .18f, 0f), new Vector3(w + .35f, .36f, d + .35f), trim);

            // Proper pitched roof geometry. V7 had the pitch signs reversed, so the
            // roof climbed toward the eaves instead of toward the ridge.
            float angle = 24f;
            float overhang = .42f;
            float halfRun = d * .5f + overhang;
            float rise = Mathf.Tan(angle * Mathf.Deg2Rad) * halfRun;
            float slopeLength = halfRun / Mathf.Cos(angle * Mathf.Deg2Rad);
            float centerY = h + rise * .5f;
            float centerZ = halfRun * .5f;

            var backRoof = Cube("Roof back slope", b,
                new Vector3(0f, centerY, -centerZ),
                new Vector3(w + overhang * 2f, .20f, slopeLength), roof, false);
            backRoof.transform.localRotation = Quaternion.Euler(-angle, 0f, 0f);

            var frontRoof = Cube("Roof front slope", b,
                new Vector3(0f, centerY, centerZ),
                new Vector3(w + overhang * 2f, .20f, slopeLength), roof, false);
            frontRoof.transform.localRotation = Quaternion.Euler(angle, 0f, 0f);

            Cube("Ridge cap", b, new Vector3(0f, h + rise + .03f, 0f),
                new Vector3(w + overhang * 2f + .04f, .16f, .18f), roof, false);
            Cube("Front fascia", b, new Vector3(0f, h + .01f, d * .5f + overhang),
                new Vector3(w + overhang * 2f, .24f, .12f), trim, false);
            Cube("Back fascia", b, new Vector3(0f, h + .01f, -d * .5f - overhang),
                new Vector3(w + overhang * 2f, .24f, .12f), trim, false);
            GableEnds(b, w, h, d, Mathf.Tan(angle * Mathf.Deg2Rad) * d * .5f, wall);

            int count = Mathf.Max(3, Mathf.FloorToInt(w / 4.1f));
            float step = w / (count + 1);
            for (int i = 1; i <= count; i++) {
                float x = -w * .5f + step * i;
                if (Mathf.Abs(x + w * .18f) < 2.2f) continue;
                Window(b, x, d * .501f, 180f, trim, glass);
            }

            float ex = -w * .18f;
            Cube("Entrance recess", b, new Vector3(ex, 1.45f, d * .507f), new Vector3(3.4f, 2.9f, .16f), trim, false);
            Cube("Door glass", b, new Vector3(ex, 1.35f, d * .60f), new Vector3(1.65f, 2.55f, .12f), glass, false);
            Cube("Door frame L", b, new Vector3(ex - .90f, 1.4f, d * .67f), new Vector3(.14f, 2.8f, .18f), trim, false);
            Cube("Door frame R", b, new Vector3(ex + .90f, 1.4f, d * .67f), new Vector3(.14f, 2.8f, .18f), trim, false);
            Cube("Entrance canopy", b, new Vector3(ex, 3.05f, d * .5f + 1.15f), new Vector3(4.2f, .22f, 2.4f), roof, false);
            Cube("Entrance step", b, new Vector3(ex, .18f, d * .5f + .85f), new Vector3(3.5f, .22f, 1.35f), trim);
        }

        static void Spruce(Transform parent, Vector3 position, float scale, Material bark, Material needles, Material snow) {
            var tree = new GameObject("Spruce").transform;
            tree.SetParent(parent);
            tree.localPosition = position;
            Cylinder("Trunk", tree, new Vector3(0f, 1.4f * scale, 0f), new Vector3(.13f * scale, 1.4f * scale, .13f * scale), bark);
            for (int i = 0; i < 4; i++) {
                float y = (1.45f + i * .72f) * scale;
                float r = (1.45f - i * .23f) * scale;
                Cylinder("Crown", tree, new Vector3(0f, y, 0f), new Vector3(r, .26f * scale, r), needles);
                Cylinder("Snow cap", tree, new Vector3(0f, y + .19f * scale, 0f), new Vector3(r * .91f, .05f * scale, r * .91f), snow);
            }
        }

        static void LampPost(Transform parent, Vector3 position, Material metal, Material warm) {
            var lamp = new GameObject("Lamp post").transform;
            lamp.SetParent(parent);
            lamp.localPosition = position;
            Cylinder("Pole", lamp, new Vector3(0f, 1.8f, 0f), new Vector3(.07f, 1.8f, .07f), metal);
            Cube("Head", lamp, new Vector3(0f, 3.55f, .10f), new Vector3(.55f, .16f, .34f), metal, false);
            Cube("Glow", lamp, new Vector3(0f, 3.47f, .12f), new Vector3(.38f, .04f, .22f), warm, false);
        }

        static void BuildScene() {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "RosvikHero";
            var root = new GameObject("ROSVIK_EXTERIOR_V8").transform;

            var snow = Mat("Snow", new Color(.78f, .82f, .83f), .20f);
            var packedSnow = Mat("Packed snow", new Color(.62f, .67f, .68f), .12f);
            var asphalt = Mat("Winter asphalt", new Color(.13f, .15f, .16f), .07f);
            var wall = Mat("School timber", new Color(.47f, .43f, .35f), .14f);
            var trim = Mat("Dark trim", new Color(.12f, .14f, .14f), .18f);
            var roof = Mat("Dark metal roof", new Color(.08f, .10f, .11f), .30f);
            var glass = Mat("Cold window glass", new Color(.18f, .28f, .31f), .42f);
            var red = Mat("Player coat", new Color(.43f, .12f, .10f), .12f);
            var skin = Mat("Skin", new Color(.62f, .47f, .36f), .15f);
            var bark = Mat("Bark", new Color(.17f, .12f, .09f), .08f);
            var pine = Mat("Pine", new Color(.10f, .19f, .16f), .10f);
            var warm = Mat("Warm lamp", new Color(1f, .58f, .27f), .35f);

            Cube("Snow terrain", root, new Vector3(0f, -.18f, 0f), new Vector3(92f, .35f, 70f), snow);
            Cube("School road", root, new Vector3(0f, .015f, 18f), new Vector3(70f, .08f, 7.5f), asphalt);
            Cube("Cleared forecourt", root, new Vector3(-5f, .045f, 10.7f), new Vector3(30f, .06f, 6.5f), packedSnow);
            Cube("Footpath", root, new Vector3(-8f, .055f, 7.0f), new Vector3(4.0f, .07f, 12f), packedSnow);

            GabledBuilding(root, "Rosviks skola - main wing", new Vector3(0f, 0f, -1.0f), new Vector3(31f, 3.6f, 9.2f), wall, trim, roof, glass);
            GabledBuilding(root, "Rosviks skola - side wing", new Vector3(12.0f, 0f, -8.0f), new Vector3(16f, 3.35f, 7.5f), wall, trim, roof, glass);

            Cube("Link block", root, new Vector3(8.5f, 1.45f, -5.2f), new Vector3(8.5f, 2.9f, 4.1f), wall);
            Cube("Link roof", root, new Vector3(8.5f, 3.0f, -5.2f), new Vector3(9.0f, .24f, 4.5f), roof, false);

            for (int i = 0; i < 6; i++) {
                var drift = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                drift.name = "Ploughed snow bank";
                drift.transform.SetParent(root);
                drift.transform.localPosition = new Vector3(-25f + i * 10f, .22f, 14.2f);
                drift.transform.localScale = new Vector3(4.3f, .55f, 1.15f);
                drift.GetComponent<Renderer>().sharedMaterial = snow;
                UnityEngine.Object.DestroyImmediate(drift.GetComponent<Collider>());
            }

            Vector3[] trees = {
                new(-32f,0f,-18f), new(-25f,0f,-20f), new(-18f,0f,-19f),
                new(24f,0f,-20f), new(31f,0f,-15f), new(35f,0f,-6f),
                new(-34f,0f,3f), new(34f,0f,7f), new(-29f,0f,25f), new(28f,0f,26f)
            };
            for (int i = 0; i < trees.Length; i++) Spruce(root, trees[i], .85f + (i % 3) * .12f, bark, pine, snow);

            LampPost(root, new Vector3(-14f, 0f, 11.5f), trim, warm);
            LampPost(root, new Vector3(4f, 0f, 11.5f), trim, warm);
            LampPost(root, new Vector3(19f, 0f, 15.0f), trim, warm);

            var player = new GameObject("PLAYER_PLACEHOLDER");
            player.transform.SetParent(root);
            player.transform.localPosition = new Vector3(-8f, .05f, 13.1f);
            Cube("Coat", player.transform, new Vector3(0f, 1.08f, 0f), new Vector3(.58f, .82f, .38f), red, false);
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(player.transform);
            head.transform.localPosition = new Vector3(0f, 1.72f, 0f);
            head.transform.localScale = Vector3.one * .38f;
            head.GetComponent<Renderer>().sharedMaterial = skin;
            UnityEngine.Object.DestroyImmediate(head.GetComponent<Collider>());
            Cube("Leg L", player.transform, new Vector3(-.15f, .48f, 0f), new Vector3(.20f, .68f, .24f), trim, false);
            Cube("Leg R", player.transform, new Vector3(.15f, .48f, 0f), new Vector3(.20f, .68f, .24f), trim, false);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.9f;
            cc.radius = .34f;
            cc.center = new Vector3(0f, .95f, 0f);
            cc.stepOffset = .25f;
            player.AddComponent<RosvikPlayerController>();

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var cam = cameraGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 10.5f;
            cam.nearClipPlane = .1f;
            cam.farClipPlane = 180f;
            cam.backgroundColor = new Color(.50f, .62f, .69f);
            var rig = cameraGo.AddComponent<IsometricCameraRig>();
            rig.target = player.transform;
            rig.yaw = 45f;
            rig.pitch = 46f;
            rig.orthographicSize = 10.5f;

            var sunGo = new GameObject("Cold winter sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(.82f, .88f, 1f);
            sun.intensity = 1.05f;
            sun.shadows = LightShadows.Soft;
            sunGo.transform.rotation = Quaternion.Euler(42f, -28f, 0f);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.40f, .45f, .48f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(.57f, .65f, .69f);
            RenderSettings.fogDensity = .0045f;

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = player;
        }
    }
}
#endif
