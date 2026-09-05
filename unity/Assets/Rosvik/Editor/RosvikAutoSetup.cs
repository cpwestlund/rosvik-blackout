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
        const int BuildVersion = 9;
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
                Debug.Log("ROSVIK REAL CAMPUS V9 BUILT: " + ScenePath);
            } catch (Exception ex) {
                Debug.LogError("ROSVIK UNITY SETUP FAILED: " + ex);
            }
        }

        static Material Mat(string name, Color color, float smooth = .16f) {
            Shader shader = GraphicsSettings.defaultRenderPipeline != null
                ? Shader.Find("Universal Render Pipeline/Lit")
                : Shader.Find("Standard");
            if (!shader || !shader.isSupported) shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader) { name = name, color = color };
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smooth);
            return mat;
        }

        static GameObject Cube(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool collider = true) {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static GameObject Cylinder(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool collider = false) {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static Mesh GableRoofMesh(float width, float depth, float wallHeight, float rise, float overhang) {
            float x0 = -width * .5f - overhang;
            float x1 = width * .5f + overhang;
            float z0 = -depth * .5f - overhang;
            float z1 = depth * .5f + overhang;
            float y0 = wallHeight;
            float y1 = wallHeight + rise;

            Mesh mesh = new Mesh();
            mesh.name = "GableRoofMesh";
            mesh.vertices = new Vector3[] {
                new Vector3(x0,y0,z0), new Vector3(x0,y1,0f), new Vector3(x0,y0,z1),
                new Vector3(x1,y0,z0), new Vector3(x1,y1,0f), new Vector3(x1,y0,z1)
            };
            mesh.triangles = new int[] {
                0,3,4, 0,4,1,
                1,4,5, 1,5,2,
                0,1,2, 3,5,4
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static void AddRoof(Transform parent, float width, float depth, float wallHeight, float rise, float overhang, Material roof) {
            GameObject roofGo = new GameObject("Roof");
            roofGo.transform.SetParent(parent, false);
            roofGo.AddComponent<MeshFilter>().sharedMesh = GableRoofMesh(width, depth, wallHeight, rise, overhang);
            roofGo.AddComponent<MeshRenderer>().sharedMaterial = roof;
        }

        static void Window(Transform parent, Vector3 pos, float yaw, Vector2 size, Material trim, Material glass, Material sill) {
            Transform root = new GameObject("Window").transform;
            root.SetParent(parent);
            root.localPosition = pos;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Cube("Frame", root, Vector3.zero, new Vector3(size.x + .18f, size.y + .18f, .11f), trim, false);
            Cube("Glass", root, new Vector3(0f, 0f, -.055f), new Vector3(size.x, size.y, .03f), glass, false);
            Cube("Mullion V", root, new Vector3(0f, 0f, -.08f), new Vector3(.055f, size.y, .04f), trim, false);
            Cube("Mullion H", root, new Vector3(0f, 0f, -.08f), new Vector3(size.x, .055f, .04f), trim, false);
            Cube("Sill", root, new Vector3(0f, -size.y * .5f - .15f, .04f), new Vector3(size.x + .24f, .08f, .22f), sill, false);
        }

        static void Door(Transform parent, Vector3 pos, float yaw, float width, float height, Material trim, Material glass, Material panel) {
            Transform root = new GameObject("Entrance door").transform;
            root.SetParent(parent);
            root.localPosition = pos;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Cube("Door frame", root, Vector3.zero, new Vector3(width + .26f, height + .22f, .14f), trim, false);
            Cube("Door panel", root, new Vector3(0f, 0f, -.075f), new Vector3(width, height, .06f), panel, false);
            Cube("Door glass upper", root, new Vector3(0f, height * .18f, -.115f), new Vector3(width * .72f, height * .45f, .025f), glass, false);
            Cube("Handle", root, new Vector3(width * .28f, -.10f, -.15f), new Vector3(.045f, .38f, .04f), trim, false);
        }

        static void MainSeventiesSchool(Transform parent, Vector3 center, Material wall, Material trim, Material roof, Material glass, Material concrete) {
            Transform b = new GameObject("Huvudbyggnaden - 1970-tal").transform;
            b.SetParent(parent);
            b.localPosition = center;

            float w = 35f, h = 3.25f, d = 10.8f;
            Cube("Main mass", b, new Vector3(0f, h*.5f, 0f), new Vector3(w,h,d), wall);
            Cube("Dark foundation", b, new Vector3(0f,.22f,0f), new Vector3(w+.35f,.44f,d+.35f), trim);

            // Low 1970s roof: much flatter than the old wooden school.
            AddRoof(b, w, d, h, 1.15f, .45f, roof);
            Cube("Front eave", b, new Vector3(0f,h+.06f,d*.5f+.25f), new Vector3(w+.8f,.12f,.14f), trim, false);

            // Public municipal material says the main building had several classrooms with separate entrances.
            float startX = -14.2f;
            for (int i=0;i<7;i++) {
                float x = startX + i*4.55f;
                Window(b, new Vector3(x,1.75f,d*.5f+.02f), 180f, new Vector2(1.65f,1.15f), trim, glass, trim);
                if (i==1 || i==3 || i==5) {
                    Door(b, new Vector3(x+1.65f,1.22f,d*.5f+.11f), 180f, .95f,2.15f, trim, glass, wall);
                    Cube("Small canopy", b, new Vector3(x+1.65f,2.65f,d*.5f+.78f), new Vector3(1.7f,.13f,1.45f), roof, false);
                    Cube("Concrete step", b, new Vector3(x+1.65f,.12f,d*.5f+.70f), new Vector3(1.7f,.20f,1.05f), concrete);
                }
            }

            // More formal main entrance toward the left side.
            float ex = -10.8f;
            Cube("Main entrance recess", b, new Vector3(ex,1.40f,d*.5f+.08f), new Vector3(3.9f,2.8f,.16f), trim, false);
            Door(b, new Vector3(ex-.55f,1.25f,d*.5f+.18f), 180f,1.0f,2.18f,trim,glass,wall);
            Door(b, new Vector3(ex+.55f,1.25f,d*.5f+.18f), 180f,1.0f,2.18f,trim,glass,wall);
            Cube("Main canopy", b, new Vector3(ex,3.00f,d*.5f+1.05f), new Vector3(4.8f,.18f,2.25f), roof, false);
            Cube("Main step", b, new Vector3(ex,.12f,d*.5f+.85f), new Vector3(4.0f,.20f,1.55f), concrete);
        }

        static void OldWoodSchool(Transform parent, Vector3 center, Material yellow, Material white, Material roof, Material glass, Material concrete) {
            Transform b = new GameObject("Träskolan - ca 1900").transform;
            b.SetParent(parent);
            b.localPosition = center;
            b.localRotation = Quaternion.Euler(0f, -9f, 0f);

            float w=14.5f, h=6.0f, d=8.6f;
            Cube("Two storey timber body", b, new Vector3(0f,h*.5f,0f), new Vector3(w,h,d), yellow);
            Cube("Stone plinth", b, new Vector3(0f,.32f,0f), new Vector3(w+.28f,.64f,d+.28f), concrete);
            AddRoof(b,w,d,h,3.15f,.55f,roof);

            // White corner boards make the old yellow timber school read instantly.
            float xEdge=w*.5f+.04f, zEdge=d*.5f+.04f;
            foreach(float x in new float[]{-xEdge,xEdge}) {
                Cube("Corner board front", b, new Vector3(x,h*.52f,zEdge), new Vector3(.20f,h*.92f,.10f), white, false);
                Cube("Corner board back", b, new Vector3(x,h*.52f,-zEdge), new Vector3(.20f,h*.92f,.10f), white, false);
            }
            Cube("White floor band", b, new Vector3(0f,3.02f,zEdge), new Vector3(w,.15f,.09f), white, false);

            float[] xs = {-5.0f,-1.7f,1.7f,5.0f};
            foreach(float x in xs) {
                Window(b,new Vector3(x,1.75f,zEdge),180f,new Vector2(1.25f,1.45f),white,glass,white);
                Window(b,new Vector3(x,4.35f,zEdge),180f,new Vector2(1.20f,1.35f),white,glass,white);
            }
            Door(b,new Vector3(0f,1.25f,zEdge+.10f),180f,1.15f,2.20f,white,glass,yellow);
            Cube("Entrance porch roof", b, new Vector3(0f,3.0f,zEdge+1.0f), new Vector3(3.5f,.18f,2.1f), roof, false);
            Cube("Porch post L", b, new Vector3(-1.45f,1.45f,zEdge+1.6f), new Vector3(.13f,2.8f,.13f), white, false);
            Cube("Porch post R", b, new Vector3(1.45f,1.45f,zEdge+1.6f), new Vector3(.13f,2.8f,.13f), white, false);
            Cube("Porch step", b, new Vector3(0f,.15f,zEdge+.92f), new Vector3(2.9f,.22f,1.45f), concrete);
        }

        static void StoneSchool(Transform parent, Vector3 center, Material masonry, Material trim, Material roof, Material glass, Material concrete) {
            Transform b = new GameObject("Stenskolan - 1940/50-tal").transform;
            b.SetParent(parent);
            b.localPosition = center;
            b.localRotation = Quaternion.Euler(0f, 7f, 0f);

            float w=17.5f,h=4.2f,d=8.2f;
            Cube("Masonry body", b, new Vector3(0f,h*.5f,0f), new Vector3(w,h,d), masonry);
            Cube("Foundation", b, new Vector3(0f,.25f,0f), new Vector3(w+.25f,.50f,d+.25f), concrete);
            AddRoof(b,w,d,h,2.35f,.48f,roof);

            float[] xs={-6.0f,-3.0f,0f,3.0f,6.0f};
            foreach(float x in xs)
                Window(b,new Vector3(x,2.0f,d*.5f+.02f),180f,new Vector2(1.35f,1.55f),trim,glass,trim);

            Door(b,new Vector3(-6.2f,1.25f,d*.5f+.12f),180f,1.05f,2.20f,trim,glass,masonry);
            Cube("Door canopy",b,new Vector3(-6.2f,2.82f,d*.5f+.75f),new Vector3(2.2f,.16f,1.55f),roof,false);
        }

        static void ModuleBuilding(Transform parent, Vector3 center, Material wall, Material trim, Material glass) {
            Transform b = new GameObject("Tillfällig skolmodul").transform;
            b.SetParent(parent);
            b.localPosition = center;
            Cube("Module body",b,new Vector3(0f,1.45f,0f),new Vector3(13f,2.9f,5.2f),wall);
            Cube("Module roof",b,new Vector3(0f,2.98f,0f),new Vector3(13.4f,.18f,5.6f),trim,false);
            for(int i=0;i<4;i++) Window(b,new Vector3(-4.6f+i*3.0f,1.55f,2.62f),180f,new Vector2(1.25f,1.05f),trim,glass,trim);
        }

        static void Spruce(Transform parent, Vector3 pos, float scale, Material bark, Material needles, Material snow) {
            Transform tree = new GameObject("Spruce").transform;
            tree.SetParent(parent);
            tree.localPosition = pos;
            Cylinder("Trunk",tree,new Vector3(0f,1.5f*scale,0f),new Vector3(.13f*scale,1.5f*scale,.13f*scale),bark);
            for(int i=0;i<4;i++) {
                float y=(1.55f+i*.72f)*scale;
                float r=(1.55f-i*.24f)*scale;
                Cylinder("Crown",tree,new Vector3(0f,y,0f),new Vector3(r,.25f*scale,r),needles);
                Cylinder("Snow cap",tree,new Vector3(0f,y+.17f*scale,0f),new Vector3(r*.9f,.05f*scale,r*.9f),snow);
            }
        }

        static void BuildScene() {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "RosvikHero";
            Transform root = new GameObject("ROSVIK_REAL_CAMPUS_V9").transform;

            Material snow=Mat("Snow",new Color(.79f,.83f,.85f),.18f);
            Material packed=Mat("Packed snow",new Color(.64f,.68f,.70f),.10f);
            Material asphalt=Mat("Asphalt",new Color(.13f,.15f,.16f),.06f);
            Material mainWall=Mat("70s school facade",new Color(.52f,.49f,.41f),.11f);
            Material yellow=Mat("Old school yellow",new Color(.66f,.54f,.28f),.10f);
            Material white=Mat("Old white trim",new Color(.82f,.82f,.77f),.14f);
            Material stone=Mat("Stenskolan masonry",new Color(.57f,.58f,.55f),.11f);
            Material module=Mat("Module facade",new Color(.42f,.45f,.43f),.10f);
            Material trim=Mat("Dark trim",new Color(.12f,.14f,.15f),.18f);
            Material roof=Mat("Dark roof",new Color(.09f,.10f,.11f),.27f);
            Material glass=Mat("Cold glass",new Color(.18f,.29f,.33f),.40f);
            Material concrete=Mat("Concrete",new Color(.43f,.45f,.46f),.08f);
            Material pine=Mat("Pine",new Color(.10f,.19f,.16f),.08f);
            Material bark=Mat("Bark",new Color(.16f,.12f,.09f),.06f);
            Material red=Mat("Player coat",new Color(.43f,.12f,.10f),.10f);
            Material skin=Mat("Skin",new Color(.62f,.47f,.36f),.12f);

            // Actual Rosvik references used for this pass:
            // municipal material describes three adjacent school buildings from ca 1900, 1940/50s and the 1970s,
            // plus later temporary modules. This scene is therefore a campus, not one generic school block.
            Cube("Snow terrain",root,new Vector3(0f,-.18f,0f),new Vector3(115f,.35f,90f),snow);
            Cube("Skolgränd road",root,new Vector3(0f,.015f,24f),new Vector3(90f,.08f,7.5f),asphalt);
            Cube("Main cleared yard",root,new Vector3(0f,.045f,10f),new Vector3(55f,.06f,18f),packed);
            Cube("Path to old school",root,new Vector3(-17f,.055f,-8f),new Vector3(5f,.07f,28f),packed);
            Cube("Path to stone school",root,new Vector3(18f,.055f,-7f),new Vector3(5f,.07f,28f),packed);

            MainSeventiesSchool(root,new Vector3(0f,0f,1f),mainWall,trim,roof,glass,concrete);
            OldWoodSchool(root,new Vector3(-20f,0f,-18f),yellow,white,roof,glass,concrete);
            StoneSchool(root,new Vector3(20f,0f,-17f),stone,trim,roof,glass,concrete);
            ModuleBuilding(root,new Vector3(30f,0f,6f),module,trim,glass);

            // Nearby sport/ice complex is deliberately present as a background landmark,
            // because public mapping places Norrbotten Stål Arena just southeast of the school.
            Transform arena=new GameObject("Norrbotten Stål Arena - background landmark").transform;
            arena.SetParent(root);
            arena.localPosition=new Vector3(44f,0f,-34f);
            Cube("Arena hall",arena,new Vector3(0f,3.7f,0f),new Vector3(27f,7.4f,15f),module);
            Cube("Arena roof",arena,new Vector3(0f,7.55f,0f),new Vector3(28f,.28f,16f),roof,false);

            Vector3[] trees={
                new Vector3(-40f,0f,-31f),new Vector3(-33f,0f,-34f),new Vector3(-27f,0f,-32f),
                new Vector3(0f,0f,-38f),new Vector3(9f,0f,-36f),new Vector3(31f,0f,-30f),
                new Vector3(-43f,0f,4f),new Vector3(43f,0f,13f),new Vector3(-35f,0f,31f),new Vector3(35f,0f,32f)
            };
            for(int i=0;i<trees.Length;i++) Spruce(root,trees[i],.9f+(i%3)*.12f,bark,pine,snow);

            // Snow banks at edge of ploughed yard.
            for(int i=0;i<7;i++) {
                GameObject drift=GameObject.CreatePrimitive(PrimitiveType.Sphere);
                drift.name="Ploughed snow bank";
                drift.transform.SetParent(root);
                drift.transform.localPosition=new Vector3(-30f+i*10f,.25f,18f);
                drift.transform.localScale=new Vector3(4.2f,.55f,1.1f);
                drift.GetComponent<Renderer>().sharedMaterial=snow;
                UnityEngine.Object.DestroyImmediate(drift.GetComponent<Collider>());
            }

            GameObject player=new GameObject("PLAYER_PLACEHOLDER");
            player.transform.SetParent(root);
            player.transform.localPosition=new Vector3(-10.8f,.05f,11.8f);
            Cube("Coat",player.transform,new Vector3(0f,1.08f,0f),new Vector3(.58f,.82f,.38f),red,false);
            GameObject head=GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name="Head";
            head.transform.SetParent(player.transform);
            head.transform.localPosition=new Vector3(0f,1.72f,0f);
            head.transform.localScale=Vector3.one*.38f;
            head.GetComponent<Renderer>().sharedMaterial=skin;
            UnityEngine.Object.DestroyImmediate(head.GetComponent<Collider>());
            Cube("Leg L",player.transform,new Vector3(-.15f,.48f,0f),new Vector3(.20f,.68f,.24f),trim,false);
            Cube("Leg R",player.transform,new Vector3(.15f,.48f,0f),new Vector3(.20f,.68f,.24f),trim,false);
            CharacterController cc=player.AddComponent<CharacterController>();
            cc.height=1.9f; cc.radius=.34f; cc.center=new Vector3(0f,.95f,0f); cc.stepOffset=.25f;
            player.AddComponent<RosvikPlayerController>();

            GameObject cameraGo=new GameObject("Main Camera");
            cameraGo.tag="MainCamera";
            Camera cam=cameraGo.AddComponent<Camera>();
            cam.orthographic=true; cam.orthographicSize=12.5f; cam.nearClipPlane=.1f; cam.farClipPlane=220f;
            cam.backgroundColor=new Color(.50f,.62f,.69f);
            IsometricCameraRig rig=cameraGo.AddComponent<IsometricCameraRig>();
            rig.target=player.transform; rig.yaw=45f; rig.pitch=46f; rig.orthographicSize=12.5f;

            GameObject sunGo=new GameObject("Cold winter sun");
            Light sun=sunGo.AddComponent<Light>();
            sun.type=LightType.Directional; sun.color=new Color(.82f,.88f,1f); sun.intensity=1.05f; sun.shadows=LightShadows.Soft;
            sunGo.transform.rotation=Quaternion.Euler(42f,-28f,0f);

            RenderSettings.ambientMode=AmbientMode.Flat;
            RenderSettings.ambientLight=new Color(.40f,.45f,.48f);
            RenderSettings.fog=true;
            RenderSettings.fogColor=new Color(.57f,.65f,.69f);
            RenderSettings.fogDensity=.0038f;

            EditorSceneManager.SaveScene(scene,ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeObject=player;
        }
    }
}
#endif
