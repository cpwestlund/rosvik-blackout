#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikCozyHeroPolishV65 {
        const int Version = 65;
        const string Key = "ROSVIK_COZY_HERO_V65";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string Generated58 = "Assets/Rosvik/GeneratedV58";
        const string Generated65 = "Assets/Rosvik/GeneratedV65";
        const string FurnitureDir = "Assets/Rosvik/ThirdParty/V58Furniture";
        const string GroupName = "COZY HERO POLISH V65";

        static RosvikCozyHeroPolishV65() {
            if (EditorPrefs.GetInt(Key, 0) >= Version) return;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V65 COZY HERO POLISH - MAKE IT FEEL FINISHED")]
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
            catch (Exception ex) { Debug.LogError("V65 COZY HERO POLISH FAILED: " + ex); }
        }

        static void Apply() {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Directory.CreateDirectory(Generated65);
            Shader shader = PickShader();
            if (!shader) throw new Exception("No compatible shader found");

            Material oak = Mat("oak", C("8b5a36"), shader, 0f, .22f);
            Material oakDark = Mat("oak_dark", C("513522"), shader, 0f, .18f);
            Material ivory = Mat("ivory", C("efe2bf"), shader, 0f, .20f);
            Material sage = Mat("sage", C("657a67"), shader, 0f, .14f);
            Material forest = Mat("forest", C("2b493c"), shader, 0f, .14f);
            Material runner = Mat("runner", C("7a4b42"), shader, 0f, .16f);
            Material runnerGold = Mat("runner_gold", C("c38a46"), shader, 0f, .18f);
            Material slate = Mat("slate", C("263936"), shader, 0f, .12f);
            Material blue = Mat("school_blue", C("547982"), shader, 0f, .18f);
            Material rust = Mat("rust", C("9a5940"), shader, 0f, .16f);
            Material linen = Mat("linen", C("cbbd9e"), shader, 0f, .12f);
            Material metal = Mat("metal", C("243032"), shader, .18f, .28f);
            Material paper = Mat("paper", C("e9dfca"), shader, 0f, .08f);
            Material warm = Emissive("warm", C("ffb05e"), shader, 2.6f);
            Material furn = AssetDatabase.LoadAssetAtPath<Material>(Generated58 + "/furniture.mat");

            GameObject old = GameObject.Find(GroupName);
            if (old) UnityEngine.Object.DestroyImmediate(old);
            GameObject root = new GameObject(GroupName);
            Transform corridor = Group(root.transform, "CORRIDOR HERO");
            Transform classrooms = Group(root.transform, "CLASSROOM HERO");
            Transform staff = Group(root.transform, "STAFF HERO");
            Transform library = Group(root.transform, "LIBRARY HERO");
            Transform janitor = Group(root.transform, "JANITOR HERO");
            Transform hall = Group(root.transform, "SPORT HALL HERO");
            Transform atmosphere = Group(root.transform, "ATMOSPHERE");

            BuildCorridor(corridor, furn, oak, oakDark, ivory, runner, runnerGold, forest, sage, warm, metal);
            BuildClassrooms(classrooms, furn, oak, oakDark, ivory, sage, blue, rust, paper, warm);
            BuildStaff(staff, furn, oak, oakDark, ivory, sage, forest, runnerGold, warm, metal);
            BuildLibrary(library, furn, oak, oakDark, ivory, sage, blue, runnerGold, warm);
            BuildJanitor(janitor, furn, oak, oakDark, ivory, slate, blue, rust, metal, paper);
            BuildHall(hall, furn, oak, oakDark, ivory, forest, rust, blue, metal, warm);
            BuildAtmosphere(atmosphere, warm);
            TuneWorld();

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorPrefs.SetInt(Key, Version);
            SceneView.RepaintAll();
            Debug.Log("V65 COZY HERO POLISH COMPLETE — stronger room identity, corridor composition, authored wall detail, denser real furniture, warm light hierarchy and zero random scatter.");
        }

        static void BuildCorridor(Transform p, Material furn, Material oak, Material oakDark, Material ivory, Material runner, Material gold, Material forest, Material sage, Material warm, Material metal) {
            // A real corridor needs rhythm. The runner is broken at door approaches so it never fights gameplay.
            Runner(p, -12.1f, 1.35f, 5.6f, 1.45f, runner, gold);
            Runner(p, -5.9f, 1.35f, 4.8f, 1.45f, runner, gold);
            Runner(p, .7f, 1.35f, 5.2f, 1.45f, runner, gold);
            Runner(p, 7.2f, 1.35f, 4.5f, 1.45f, runner, gold);
            Runner(p, 13.2f, 1.35f, 3.8f, 1.45f, runner, gold);

            // Wall-hugging furniture only. The central 2m circulation strip remains completely clear.
            PlaceAsset("cabinet_small_decorated", "corridor cubby A", p, new Vector3(-16.45f, 0, 3.25f), 0, .62f, furn);
            PlaceAsset("cactus_medium_A", "corridor plant A", p, new Vector3(-15.2f, 0, 3.22f), 0, .62f, furn);
            PlaceAsset("cabinet_small_decorated", "corridor cubby B", p, new Vector3(-9.55f, 0, 3.25f), 0, .60f, furn);
            PlaceAsset("cactus_medium_A", "corridor plant B", p, new Vector3(6.9f, 0, 3.20f), 0, .58f, furn);
            PlaceAsset("cabinet_small_decorated", "corridor cubby C", p, new Vector3(12.45f, 0, 3.25f), 0, .58f, furn);

            // Coat rail / hooks: school-specific detail instead of generic decoration.
            CoatRail(p, new Vector3(-7.25f, .66f, 4.02f), 2.0f, oak, metal);
            CoatRail(p, new Vector3(.55f, .66f, 4.02f), 1.8f, oak, metal);

            // Small display shelf and objects, all attached to the wall plane.
            Box("corridor display shelf", p, new Vector3(6.45f, .38f, 3.94f), new Vector3(1.55f, .10f, .34f), oak, false);
            PlaceAssetAt("book_set", "corridor display books", p, new Vector3(6.20f, .48f, 3.80f), 0, .38f, furn);
            PlaceAssetAt("cactus_medium_A", "corridor tiny plant", p, new Vector3(6.82f, .48f, 3.82f), 0, .32f, furn);

            // Pendant sequence creates light rhythm and visual depth while leaving floor navigation untouched.
            Pendant(p, new Vector3(-12.0f, 2.20f, 1.65f), ivory, warm, 4.0f, .70f);
            Pendant(p, new Vector3(-4.0f, 2.20f, 1.65f), ivory, warm, 4.0f, .70f);
            Pendant(p, new Vector3(4.0f, 2.20f, 1.65f), ivory, warm, 4.0f, .70f);
            Pendant(p, new Vector3(12.0f, 2.20f, 1.65f), ivory, warm, 4.0f, .70f);

            // Wainscot strips give the corridor wall a handcrafted lower band.
            WainscotX(p, -17.7f, 17.7f, 4.06f, forest, oakDark);
        }

        static void BuildClassrooms(Transform p, Material furn, Material oak, Material oakDark, Material ivory, Material sage, Material blue, Material rust, Material paper, Material warm) {
            // CLASS A: dense but ordered. Add wall storage rather than cluttering circulation.
            PlaceAsset("shelf_B_small_decorated", "class A side shelf", p, new Vector3(-17.05f, 0, 8.0f), 90, .72f, furn);
            PlaceAsset("cactus_medium_A", "class A window plant", p, new Vector3(-16.65f, 0, 14.45f), 0, .52f, furn);
            PlaceAssetAt("book_set", "class A desk books one", p, new Vector3(-15.50f, .55f, 8.05f), 10, .31f, furn);
            PlaceAssetAt("book_set", "class A desk books two", p, new Vector3(-10.10f, .55f, 11.05f), -8, .31f, furn);
            MakeSchoolPoster(p, new Vector3(-17.69f, .70f, 9.65f), 90, blue, ivory, paper);
            MakeSchoolPoster(p, new Vector3(-17.69f, .70f, 10.55f), 90, rust, ivory, paper);
            Pendant(p, new Vector3(-13.0f, 2.28f, 10.2f), ivory, warm, 4.6f, .78f);

            // CLASS B: reading / art identity.
            PlaceAsset("shelf_A_big", "class B side shelf", p, new Vector3(-7.70f, 0, 12.85f), 90, .67f, furn);
            PlaceAsset("cactus_medium_A", "class B plant two", p, new Vector3(-1.05f, 0, 14.15f), 0, .54f, furn);
            PlaceAssetAt("book_set", "class B desk books", p, new Vector3(-3.90f, .55f, 8.05f), 16, .31f, furn);
            MakeSchoolPoster(p, new Vector3(-.58f, .70f, 9.8f), 90, sage, ivory, paper);
            MakeSchoolPoster(p, new Vector3(-.58f, .70f, 10.8f), 90, blue, ivory, paper);
            Pendant(p, new Vector3(-4.25f, 2.28f, 10.2f), ivory, warm, 4.4f, .74f);

            // Warm wood rails across the side walls make the room shells less box-like.
            Box("class A picture rail", p, new Vector3(-17.74f, .89f, 10.8f), new Vector3(.045f, .055f, 5.3f), oak, false);
            Box("class B picture rail", p, new Vector3(-.50f, .89f, 10.9f), new Vector3(.045f, .055f, 5.1f), oak, false);
        }

        static void BuildStaff(Transform p, Material furn, Material oak, Material oakDark, Material ivory, Material sage, Material forest, Material gold, Material warm, Material metal) {
            // Complete the kitchenette so it reads as cabinetry rather than a single block.
            for (int i = 0; i < 4; i++) {
                float x = 2.65f + i * .95f;
                Box("staff cabinet door", p, new Vector3(x, .36f, 14.58f), new Vector3(.82f, .50f, .035f), sage, false);
                Sphere("staff cabinet knob", p, new Vector3(x+.28f, .38f, 14.54f), Vector3.one*.055f, metal);
            }
            Box("staff splashback", p, new Vector3(4.15f, .93f, 15.28f), new Vector3(3.60f, .42f, .025f), ivory, false);
            for (int i=0;i<7;i++) Box("staff splash tile", p, new Vector3(2.55f+i*.52f,.93f,15.255f), new Vector3(.025f,.38f,.018f), oakDark,false);

            // Cozy dining corner and layered wall storage.
            PlaceAsset("chair_B", "staff spare chair", p, new Vector3(6.15f, 0, 9.2f), 90, .62f, furn);
            PlaceAsset("shelf_B_small_decorated", "staff wall shelf", p, new Vector3(.62f, 0, 13.75f), 90, .60f, furn);
            PlaceAssetAt("book_set", "staff shelf books", p, new Vector3(.72f, .82f, 13.75f), 90, .34f, furn);
            PlaceAssetAt("cactus_medium_A", "staff shelf plant", p, new Vector3(.72f, .78f, 13.1f), 0, .34f, furn);
            MakeSchoolPoster(p, new Vector3(.12f, .72f, 8.7f), 90, forest, ivory, gold);
            Pendant(p, new Vector3(3.45f, 2.25f, 10.4f), ivory, warm, 4.8f, .92f);
        }

        static void BuildLibrary(Transform p, Material furn, Material oak, Material oakDark, Material ivory, Material sage, Material blue, Material gold, Material warm) {
            // More books, more symmetry, still keeping a clear center path.
            PlaceAsset("shelf_B_small_decorated", "library low shelf L", p, new Vector3(8.05f, 0, 8.1f), 90, .62f, furn);
            PlaceAsset("shelf_B_small_decorated", "library low shelf R", p, new Vector3(11.70f, 0, 8.1f), -90, .62f, furn);
            PlaceAssetAt("book_set", "library table books A", p, new Vector3(9.65f, .43f, 11.15f), 15, .35f, furn);
            PlaceAssetAt("book_set", "library table books B", p, new Vector3(10.15f, .43f, 11.15f), -20, .31f, furn);
            PlaceAsset("cactus_medium_A", "library plant", p, new Vector3(8.15f, 0, 14.15f), 0, .50f, furn);
            MakeSchoolPoster(p, new Vector3(7.53f, .72f, 11.45f), 90, blue, ivory, gold);
            Pendant(p, new Vector3(9.90f, 2.25f, 10.5f), ivory, warm, 4.6f, .86f);
        }

        static void BuildJanitor(Transform p, Material furn, Material oak, Material oakDark, Material ivory, Material slate, Material blue, Material rust, Material metal, Material paper) {
            // Organized service room: labels, shallow wall storage, no free-scattered junk.
            Box("janitor wall shelf one", p, new Vector3(16.55f, .62f, 15.06f), new Vector3(1.65f,.09f,.42f), oak, false);
            Box("janitor wall shelf two", p, new Vector3(16.55f, 1.02f, 15.06f), new Vector3(1.65f,.09f,.42f), oak, false);
            for (int i=0;i<3;i++) {
                Box("janitor labelled box", p, new Vector3(16.0f+i*.55f,.76f,14.84f), new Vector3(.44f,.24f,.34f), i%2==0?blue:rust,false);
                Box("janitor paper label", p, new Vector3(16.0f+i*.55f,.76f,14.66f), new Vector3(.22f,.09f,.012f), paper,false);
            }
            PlaceAsset("cabinet_small_decorated", "janitor extra cabinet", p, new Vector3(13.25f,0,10.25f),90,.55f,furn);
        }

        static void BuildHall(Transform p, Material furn, Material oak, Material oakDark, Material ivory, Material forest, Material rust, Material blue, Material metal, Material warm) {
            // The gym gets strong architectural rhythm so it doesn't read as an empty brown rectangle.
            for (int i=0;i<5;i++) {
                float x=26.0f+i*4.0f;
                Box("gym ceiling beam", p, new Vector3(x,1.55f,7f), new Vector3(.13f,.13f,15.2f), oakDark,false);
            }
            for (int i=0;i<4;i++) {
                float x=28.0f+i*4.0f;
                Pendant(p,new Vector3(x,2.65f,7f),ivory,warm,5.2f,.72f);
            }
            // Equipment nook along the far right wall, kept out of the court.
            PlaceAsset("cabinet_medium_decorated","gym equipment cabinet A",p,new Vector3(43.0f,0,11.7f),90,.60f,furn);
            PlaceAsset("cabinet_medium_decorated","gym equipment cabinet B",p,new Vector3(43.0f,0,9.9f),90,.60f,furn);
            Box("gym crash mat red",p,new Vector3(42.75f,.13f,5.25f),new Vector3(.30f,.26f,2.1f),rust,false);
            Box("gym crash mat blue",p,new Vector3(42.75f,.13f,7.65f),new Vector3(.30f,.26f,2.1f),blue,false);
        }

        static void BuildAtmosphere(Transform p, Material warm) {
            // Warm pools are intentionally stronger than V64; lower ambient makes them matter.
            Glow(p,"corridor warmth L",new Vector3(-8.0f,1.8f,1.6f),C("ffc987"),5.2f,.78f);
            Glow(p,"corridor warmth R",new Vector3(8.0f,1.8f,1.6f),C("ffc987"),5.2f,.78f);
            Glow(p,"staff hero warmth",new Vector3(3.45f,1.9f,11f),C("ffb86b"),5.0f,.92f);
            Glow(p,"library hero warmth",new Vector3(9.9f,1.9f,10.7f),C("ffc17b"),4.5f,.82f);
        }

        static void TuneWorld() {
            // Slightly closer / lower camera: still top-down, but furniture now has visible volume.
            Camera cam = Camera.main;
            GameObject player = GameObject.Find("PLAYER");
            if (cam && player) {
                Vector3 offset = new Vector3(0f, 15.2f, -10.5f);
                cam.orthographic = true;
                cam.orthographicSize = 6.65f;
                cam.allowHDR = true;
                cam.backgroundColor = C("18201e");
                cam.transform.position = player.transform.position + offset;
                cam.transform.LookAt(player.transform.position + Vector3.up*.25f);

                Component rig = cam.GetComponent("CozyCameraV57");
                if (rig) {
                    SerializedObject so = new SerializedObject(rig);
                    SerializedProperty po = so.FindProperty("offset"); if(po!=null) po.vector3Value=offset;
                    SerializedProperty pmin=so.FindProperty("minSize"); if(pmin!=null) pmin.floatValue=5.8f;
                    SerializedProperty pmax=so.FindProperty("maxSize"); if(pmax!=null) pmax.floatValue=9.0f;
                    SerializedProperty pla=so.FindProperty("lookAhead"); if(pla!=null) pla.floatValue=.45f;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = C("46534c");
            foreach(Light l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None)) {
                if(l.type!=LightType.Directional) continue;
                l.color=C("f2d7ab");
                l.intensity=.44f;
                l.shadowStrength=.34f;
                l.shadows=LightShadows.Soft;
            }
            QualitySettings.shadowDistance=38f;
        }

        static void Runner(Transform p,float x,float z,float w,float d,Material main,Material border) {
            Box("corridor runner",p,new Vector3(x,.081f,z),new Vector3(w,.016f,d),main,false);
            Box("runner top border",p,new Vector3(x,.092f,z+d*.5f-.055f),new Vector3(w-.10f,.010f,.055f),border,false);
            Box("runner bottom border",p,new Vector3(x,.092f,z-d*.5f+.055f),new Vector3(w-.10f,.010f,.055f),border,false);
        }

        static void CoatRail(Transform p,Vector3 c,float width,Material wood,Material metal) {
            Box("coat rail",p,c,new Vector3(width,.09f,.055f),wood,false);
            int n=Mathf.Max(3,Mathf.RoundToInt(width/.34f));
            for(int i=0;i<n;i++) {
                float x=c.x-width*.43f+i*(width*.86f/Mathf.Max(1,n-1));
                Box("coat hook",p,new Vector3(x,c.y-.12f,c.z-.075f),new Vector3(.045f,.22f,.08f),metal,false);
                Sphere("hook end",p,new Vector3(x,c.y-.23f,c.z-.11f),Vector3.one*.06f,metal);
            }
        }

        static void WainscotX(Transform p,float x1,float x2,float z,Material panel,Material rail) {
            Box("corridor wainscot",p,new Vector3((x1+x2)*.5f,.34f,z),new Vector3(x2-x1,.43f,.024f),panel,false);
            Box("corridor chair rail",p,new Vector3((x1+x2)*.5f,.58f,z-.018f),new Vector3(x2-x1,.065f,.065f),rail,false);
        }

        static void MakeSchoolPoster(Transform p,Vector3 c,float yaw,Material field,Material frame,Material accent) {
            Transform g=Group(p,"school wall poster"); g.position=c; g.rotation=Quaternion.Euler(0,yaw,0);
            LocalBox("poster frame",g,Vector3.zero,new Vector3(.62f,.75f,.035f),frame,false);
            LocalBox("poster field",g,new Vector3(0,0,-.025f),new Vector3(.52f,.65f,.018f),field,false);
            LocalBox("poster accent",g,new Vector3(0,.17f,-.039f),new Vector3(.34f,.08f,.010f),accent,false);
            LocalBox("poster paper",g,new Vector3(0,-.12f,-.039f),new Vector3(.38f,.18f,.010f),frame,false);
        }

        static void Pendant(Transform p,Vector3 pos,Material shade,Material glow,float range,float intensity) {
            Box("pendant cord",p,pos+Vector3.up*.32f,new Vector3(.025f,.65f,.025f),shade,false);
            GameObject s=GameObject.CreatePrimitive(PrimitiveType.Sphere); s.name="pendant shade"; s.transform.SetParent(p,true); s.transform.position=pos; s.transform.localScale=new Vector3(.42f,.17f,.42f); s.GetComponent<Renderer>().sharedMaterial=shade; RemoveAllColliders(s);
            Sphere("pendant bulb",p,pos-Vector3.up*.09f,Vector3.one*.09f,glow);
            Glow(p,"pendant warm pool",pos-Vector3.up*.16f,C("ffc987"),range,intensity);
        }

        static void Glow(Transform p,string name,Vector3 pos,Color color,float range,float intensity) {
            GameObject g=new GameObject(name); g.transform.SetParent(p,true); g.transform.position=pos;
            Light l=g.AddComponent<Light>(); l.type=LightType.Point; l.color=color; l.range=range; l.intensity=intensity; l.shadows=LightShadows.None;
        }

        static GameObject PlaceAsset(string model,string name,Transform parent,Vector3 pos,float yaw,float scale,Material mat) {
            GameObject prefab=AssetDatabase.LoadAssetAtPath<GameObject>(FurnitureDir+"/"+model+".obj");
            if(!prefab)return null;
            GameObject go=(GameObject)PrefabUtility.InstantiatePrefab(prefab); if(!go)go=UnityEngine.Object.Instantiate(prefab);
            go.name=name; go.transform.SetParent(parent,true); go.transform.position=pos; go.transform.rotation=Quaternion.Euler(0,yaw,0); go.transform.localScale=Vector3.one*scale;
            if(mat)foreach(Renderer r in go.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=mat;
            Ground(go,pos.y); RemoveAllColliders(go); return go;
        }

        static GameObject PlaceAssetAt(string model,string name,Transform parent,Vector3 pos,float yaw,float scale,Material mat) {
            GameObject prefab=AssetDatabase.LoadAssetAtPath<GameObject>(FurnitureDir+"/"+model+".obj");
            if(!prefab)return null;
            GameObject go=(GameObject)PrefabUtility.InstantiatePrefab(prefab); if(!go)go=UnityEngine.Object.Instantiate(prefab);
            go.name=name; go.transform.SetParent(parent,true); go.transform.position=pos; go.transform.rotation=Quaternion.Euler(0,yaw,0); go.transform.localScale=Vector3.one*scale;
            if(mat)foreach(Renderer r in go.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=mat;
            RemoveAllColliders(go); return go;
        }

        static void Ground(GameObject go,float y) {
            Renderer[] rs=go.GetComponentsInChildren<Renderer>(true); if(rs.Length==0)return;
            Bounds b=rs[0].bounds; for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);
            go.transform.position+=Vector3.up*(y-b.min.y);
        }

        static Shader PickShader() {
            bool srp=GraphicsSettings.currentRenderPipeline!=null||GraphicsSettings.defaultRenderPipeline!=null;
            Shader s=srp?Shader.Find("Universal Render Pipeline/Lit"):Shader.Find("Standard");
            if(!s||!s.isSupported)s=Shader.Find("Universal Render Pipeline/Simple Lit");
            if(!s||!s.isSupported)s=Shader.Find("Standard"); return s;
        }

        static Material Mat(string name,Color color,Shader shader,float metal,float smooth) {
            string path=Generated65+"/"+name+".mat"; Material m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(shader){name="V65 "+name};AssetDatabase.CreateAsset(m,path);} if(m.shader!=shader)m.shader=shader;
            if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color); if(m.HasProperty("_Color"))m.SetColor("_Color",color);
            if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",metal); if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth); EditorUtility.SetDirty(m); return m;
        }

        static Material Emissive(string name,Color color,Shader shader,float intensity) {
            Material m=Mat(name,color,shader,0,.2f); if(m.HasProperty("_EmissionColor")){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*intensity);} return m;
        }

        static Color C(string hex){ColorUtility.TryParseHtmlString("#"+hex,out Color c);return c;}
        static Transform Group(Transform p,string name){GameObject g=new GameObject(name);g.transform.SetParent(p,false);return g.transform;}
        static GameObject Box(string name,Transform p,Vector3 pos,Vector3 size,Material mat,bool collider){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(p,true);g.transform.position=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;if(!collider)RemoveAllColliders(g);return g;}
        static GameObject LocalBox(string name,Transform p,Vector3 pos,Vector3 size,Material mat,bool collider){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;if(!collider)RemoveAllColliders(g);return g;}
        static void Sphere(string name,Transform p,Vector3 pos,Vector3 scale,Material mat){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=name;g.transform.SetParent(p,true);g.transform.position=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;RemoveAllColliders(g);}
        static void RemoveAllColliders(GameObject g){foreach(Collider c in g.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);}
    }
}
#endif
