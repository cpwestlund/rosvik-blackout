#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Rosvik.Blackout;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikCozyRebootV58 {
        const int Version = 58;
        const string Key = "ROSVIK_COZY_REBOOT_V58";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string GeneratedDir = "Assets/Rosvik/GeneratedV58";
        const string FurnitureDir = "Assets/Rosvik/ThirdParty/V58Furniture";
        const string RawBase = "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-Furniture-Bits-1.0/main/addons/kaykit_furniture_bits/Assets/obj/";
        const string LicenseUrl = "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-Furniture-Bits-1.0/main/LICENSE.txt";

        static readonly string[] Furniture = {
            "chair_A","chair_B","table_small","table_medium","table_low",
            "cabinet_medium_decorated","cabinet_small_decorated",
            "shelf_A_big","shelf_B_large_decorated","shelf_B_small_decorated",
            "couch_pillows","armchair_pillows","lamp_standing","lamp_table",
            "pictureframe_medium","book_set","rug_rectangle_stripes_A","rug_oval_A","cactus_medium_A"
        };

        static readonly Dictionary<string,List<Rect>> Occupied = new Dictionary<string,List<Rect>>();

        static RosvikCozyRebootV58() {
            EditorApplication.delayCall -= Auto;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V58 COZY GAME REBOOT - BUILD FROM ZERO")]
        public static void Force() {
            EditorPrefs.DeleteKey(Key);
            Build();
        }

        static void Auto() {
            if (EditorPrefs.GetInt(Key,0) >= Version) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) {
                EditorApplication.delayCall += Auto;
                return;
            }
            Build();
        }

        static void Build() {
            try {
                Directory.CreateDirectory(GeneratedDir);
                Directory.CreateDirectory(FurnitureDir);
                Directory.CreateDirectory("Assets/Rosvik/Scenes");
                EnsureFurniture();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                Occupied.Clear();

                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (!shader || !shader.isSupported) shader = Shader.Find("Standard");
                if (!shader) throw new Exception("V58 could not find a supported lit shader");

                Material grass = Mat("grass",C("435a3e"),shader,0,.14f);
                Material grassDark = Mat("grass_dark",C("2f4534"),shader,0,.12f);
                Material snow = Mat("snow",C("dce3df"),shader,0,.30f);
                Material asphalt = Mat("asphalt",C("25292a"),shader,0,.22f);
                Material stone = Mat("stone",C("7d7464"),shader,0,.22f);
                Material wall = Mat("wall",C("c0b49b"),shader,0,.24f);
                Material wallGreen = Mat("wall_green",C("52665a"),shader,0,.20f);
                Material trim = Mat("trim",C("e4d8b7"),shader,0,.30f);
                Material wood = Mat("wood",C("80502f"),shader,0,.26f);
                Material wood2 = Mat("wood_light",C("a77443"),shader,0,.24f);
                Material corridor = Mat("corridor",C("737568"),shader,0,.18f);
                Material classroom = Mat("classroom",C("8d785a"),shader,0,.18f);
                Material staff = Mat("staff",C("687568"),shader,0,.18f);
                Material gym = Mat("gym",C("9e6738"),shader,0,.26f);
                Material line = Mat("line",C("e8dcb4"),shader,0,.20f);
                Material metal = Mat("metal",C("293133"),shader,.3f,.38f);
                Material glass = Mat("glass",C("477579"),shader,0,.52f);
                Material red = Mat("red",C("a8463d"),shader,0,.24f);
                Material blue = Mat("blue",C("477a82"),shader,0,.24f);
                Material orange = Mat("player_orange",C("bf5b2d"),shader,0,.30f);
                Material navy = Mat("player_navy",C("25343d"),shader,0,.30f);
                Material skin = Mat("player_skin",C("c98c68"),shader,0,.24f);
                Material glow = Emissive("warm_glow",C("ff9b45"),shader,2.1f);
                Material furnitureMat = FurnitureMaterial(shader);

                GameObject root = new GameObject("COZY SCHOOL GAME V58");
                Transform ext = Group(root.transform,"CAMPUS OUTSIDE");
                Transform school = Group(root.transform,"SCHOOL INTERIOR");
                Transform hall = Group(root.transform,"SPORT HALL");
                Transform lights = Group(root.transform,"COZY LIGHTS");

                BuildCampus(ext,grass,grassDark,snow,asphalt,stone,trim,wood,metal,furnitureMat);
                BuildSchool(school,wall,wallGreen,trim,wood,wood2,corridor,classroom,staff,metal,glass,red,blue,furnitureMat,glow);
                BuildHall(hall,wall,wallGreen,trim,wood,wood2,gym,line,metal,furnitureMat);
                BuildWarmLights(lights,glow);

                GameObject player = BuildPlayer(orange,navy,skin);
                player.transform.position = new Vector3(0,.05f,-12f);
                BuildCamera(player.transform);
                BuildLighting();

                EditorSceneManager.SaveScene(scene,ScenePath);
                EnsureBuildSettings();
                EditorPrefs.SetInt(Key,Version);
                Selection.activeGameObject = player;
                AssetDatabase.SaveAssets();
                SceneView.RepaintAll();
                Debug.Log("V58 COZY REBOOT SUCCESS — new scene, hand-built layout, KayKit furniture, cutaway walls, physical doors/cabinets and no procedural overlap.");
            }
            catch(Exception ex) {
                Debug.LogError("V58 COZY REBOOT FAILED: "+ex);
            }
        }

        static void EnsureFurniture() {
            using(WebClient wc = new WebClient()) {
                wc.Headers.Add("User-Agent","Rosvik-Cozy-Reboot");
                string tex=FurnitureDir+"/furniturebits_texture.png";
                if(!File.Exists(tex)) Download(wc,RawBase+"furniturebits_texture.png",tex);
                string lic=FurnitureDir+"/LICENSE_KAYKIT_FURNITURE.txt";
                if(!File.Exists(lic)) Download(wc,LicenseUrl,lic);
                foreach(string n in Furniture) {
                    string obj=FurnitureDir+"/"+n+".obj";
                    string mtl=FurnitureDir+"/"+n+".mtl";
                    if(!File.Exists(obj)) Download(wc,RawBase+n+".obj",obj);
                    if(!File.Exists(mtl)) Download(wc,RawBase+n+".mtl",mtl);
                }
            }
        }

        static void Download(WebClient wc,string url,string path) {
            try { wc.DownloadFile(url,path); }
            catch(Exception e) { Debug.LogWarning("V58 asset download skipped: "+Path.GetFileName(path)+" — "+e.Message); }
        }

        static void BuildCampus(Transform p,Material grass,Material grassDark,Material snow,Material asphalt,Material stone,Material trim,Material wood,Material metal,Material furnitureMat) {
            Floor("campus ground",p,new Vector3(9,-.10f,-8),new Vector3(84,.20f,23),grass);
            Floor("school bed",p,new Vector3(0,-.04f,7.3f),new Vector3(37.5f,.08f,18.8f),grassDark);
            Floor("hall bed",p,new Vector3(34,-.04f,7),new Vector3(23,.08f,19.5f),grassDark);

            Floor("arrival path",p,new Vector3(0,.01f,-7),new Vector3(5.0f,.05f,12.2f),stone);
            for(int i=-5;i<=5;i++) Decorative("path seam",p,new Vector3(0,.04f,-7+i*.98f),new Vector3(4.85f,.012f,.035f),trim);

            Floor("parking",p,new Vector3(-14.5f,.01f,-10.5f),new Vector3(18,.05f,8.5f),asphalt);
            for(int i=-2;i<=2;i++) Decorative("parking stripe",p,new Vector3(-14.5f+i*3.0f,.045f,-10.5f),new Vector3(.05f,.012f,5.8f),trim);

            GameObject car=FindProjectModel("car_stationwagon","car_sedan","car_hatchback");
            if(car){PlaceProject(car,"parked car",p,new Vector3(-18,0,-10.5f),0,1.35f);PlaceProject(car,"parked car",p,new Vector3(-12,0,-10.5f),180,1.35f);}
            GameObject bench=FindProjectModel("bench");
            if(bench){PlaceProject(bench,"school bench",p,new Vector3(-5.2f,0,-4.0f),90,.85f);PlaceProject(bench,"school bench",p,new Vector3(5.2f,0,-4.0f),-90,.85f);}

            GameObject tree=FindProjectModel("tree_pineDefaultA","tree_pineTallA","tree_default_fall");
            GameObject bush=FindProjectModel("plant_bushDetailed","bush");
            Vector3[] tp={new Vector3(-27,0,-15),new Vector3(-25,0,-4),new Vector3(-23,0,-17),new Vector3(11,0,-16),new Vector3(16,0,-14),new Vector3(21,0,-16),new Vector3(48,0,-12),new Vector3(50,0,0),new Vector3(48,0,14)};
            foreach(Vector3 v in tp){if(tree)PlaceProject(tree,"pine",p,v,0,3.0f);else Pine(p,v,wood,grassDark);}
            Vector3[] bp={new Vector3(-7,0,-2.8f),new Vector3(7,0,-2.8f),new Vector3(10,0,-13.5f),new Vector3(15,0,-13.5f)};
            foreach(Vector3 v in bp){if(bush)PlaceProject(bush,"bush",p,v,0,.75f);else Shrub(p,v,grassDark);}

            Snow(p,new Vector3(-28,.02f,-10),new Vector3(5,.12f,2),snow);
            Snow(p,new Vector3(20,.02f,-17),new Vector3(5.4f,.12f,1.8f),snow);
            Snow(p,new Vector3(47,.02f,-15),new Vector3(4,.12f,1.5f),snow);

            // Outdoor cozy corner with real furniture assets if old city props are unavailable.
            if(!bench){Asset("couch_pillows","outdoor seat",p,new Vector3(-5.3f,0,-4.0f),90,.72f,furnitureMat);Asset("couch_pillows","outdoor seat",p,new Vector3(5.3f,0,-4.0f),-90,.72f,furnitureMat);}
        }

        static void BuildSchool(Transform p,Material wall,Material accent,Material trim,Material wood,Material wood2,Material corridor,Material classroom,Material staff,Material metal,Material glass,Material red,Material blue,Material furn,Material glow) {
            // One clean footprint. Floors always have colliders; decorative seams never do.
            Floor("corridor floor",p,new Vector3(0,.02f,2),new Vector3(35.6f,.08f,5.7f),corridor);
            RoomFloor(p,"class A",new Vector3(-13,.025f,10),new Vector3(9.5f,.09f,11.3f),classroom,trim,true);
            RoomFloor(p,"class B",new Vector3(-4.25f,.025f,10),new Vector3(7.6f,.09f,11.3f),classroom,trim,true);
            RoomFloor(p,"staff",new Vector3(3.45f,.025f,10),new Vector3(7.6f,.09f,11.3f),staff,trim,false);
            RoomFloor(p,"library",new Vector3(9.9f,.025f,10),new Vector3(5.2f,.09f,11.3f),classroom,trim,true);
            RoomFloor(p,"janitor",new Vector3(15.05f,.025f,10),new Vector3(5.1f,.09f,11.3f),corridor,trim,false);

            // Deliberate doll-house cutaway: front wall low, every other wall complete.
            WallX(p,-18,-2.15f,-1,.48f,wall,trim); WallX(p,2.15f,18,-1,.48f,wall,trim);
            WallX(p,-18,18,15.7f,1.18f,wall,trim);
            WallZ(p,-18,-1,15.7f,1.18f,wall,trim);
            WallZ(p,18,-1,1.05f,1.18f,wall,trim); WallZ(p,18,3.95f,15.7f,1.18f,wall,trim);

            // Room fronts with real door gaps — no missing walls.
            WallX(p,-18,-13.75f,4.25f,.92f,accent,trim); WallX(p,-12.25f,-8.2f,4.25f,.92f,accent,trim);
            WallX(p,-8.2f,-5.0f,4.25f,.92f,accent,trim); WallX(p,-3.5f,-.45f,4.25f,.92f,accent,trim);
            WallX(p,-.45f,2.7f,4.25f,.92f,accent,trim); WallX(p,4.2f,9.25f,4.25f,.92f,accent,trim);
            WallX(p,10.55f,14.4f,4.25f,.92f,accent,trim); WallX(p,15.7f,18,4.25f,.92f,accent,trim);
            WallZ(p,-8.2f,4.25f,15.7f,.92f,wall,trim); WallZ(p,-.45f,4.25f,15.7f,.92f,wall,trim); WallZ(p,7.4f,4.25f,15.7f,.92f,wall,trim); WallZ(p,12.5f,4.25f,15.7f,.92f,wall,trim);

            Door(p,"huvudentrén",new Vector3(0,0,-1),0,1.85f,wood,glass,trim,metal,true);
            Door(p,"klassrum A",new Vector3(-13,0,4.25f),0,1.35f,wood,wood2,trim,metal,false);
            Door(p,"klassrum B",new Vector3(-4.25f,0,4.25f),0,1.35f,wood,wood2,trim,metal,false);
            Door(p,"personalrummet",new Vector3(3.45f,0,4.25f),0,1.35f,wood,wood2,trim,metal,false);
            Door(p,"biblioteket",new Vector3(9.9f,0,4.25f),0,1.15f,wood,wood2,trim,metal,false);
            Door(p,"vaktmästarrummet",new Vector3(15.05f,0,4.25f),0,1.15f,wood,wood2,trim,metal,false);
            Door(p,"sporthallen",new Vector3(18,0,2.5f),90,1.45f,wood,wood2,trim,metal,false);

            // Reserve approach/swing space first. Furniture placement is refused if it overlaps a reservation.
            Reserve("classA",new Vector3(-13,0,5.6f),new Vector2(3.1f,2.7f));
            Reserve("classB",new Vector3(-4.25f,0,5.6f),new Vector2(3.1f,2.7f));
            Reserve("staff",new Vector3(3.45f,0,5.6f),new Vector2(3.1f,2.7f));
            Reserve("library",new Vector3(9.9f,0,5.6f),new Vector2(2.6f,2.7f));
            Reserve("janitor",new Vector3(15.05f,0,5.6f),new Vector2(2.6f,2.7f));

            // Corridor: assets, not debug blocks.
            SafeAsset("rug_rectangle_stripes_A","hall runner",p,"corridor",new Vector3(-7.0f,.07f,1.55f),0,.92f,new Vector2(3.0f,2.0f),furn,false);
            SafeAsset("rug_rectangle_stripes_A","hall runner",p,"corridor",new Vector3(7.0f,.07f,1.55f),0,.92f,new Vector2(3.0f,2.0f),furn,false);
            SafeAsset("couch_pillows","waiting sofa",p,"corridor",new Vector3(-15.0f,0,1.5f),90,.75f,new Vector2(1.4f,2.3f),furn,true);
            SafeAsset("cactus_medium_A","hall plant",p,"corridor",new Vector3(-10.2f,0,1.25f),0,.82f,new Vector2(.9f,.9f),furn,true);
            SafeAsset("cactus_medium_A","hall plant",p,"corridor",new Vector3(12.2f,0,1.25f),0,.82f,new Vector2(.9f,.9f),furn,true);

            // Classroom A: six clean desk/chair clusters.
            DeskCluster(p,"classA",new Vector3(-15.5f,0,8.0f),furn); DeskCluster(p,"classA",new Vector3(-12.8f,0,8.0f),furn); DeskCluster(p,"classA",new Vector3(-10.1f,0,8.0f),furn);
            DeskCluster(p,"classA",new Vector3(-15.5f,0,11.0f),furn); DeskCluster(p,"classA",new Vector3(-12.8f,0,11.0f),furn); DeskCluster(p,"classA",new Vector3(-10.1f,0,11.0f),furn);
            SafeAsset("table_small","teacher desk",p,"classA",new Vector3(-12.9f,0,14.0f),0,.82f,new Vector2(1.3f,1.3f),furn,true);
            SafeAsset("shelf_B_large_decorated","class bookshelf",p,"classA",new Vector3(-16.8f,0,13.6f),90,.82f,new Vector2(1.0f,1.8f),furn,true);

            // Classroom B: varied layout with actual reading furniture.
            DeskCluster(p,"classB",new Vector3(-6.5f,0,8.0f),furn); DeskCluster(p,"classB",new Vector3(-3.9f,0,8.0f),furn); DeskCluster(p,"classB",new Vector3(-1.6f,0,8.0f),furn);
            DeskCluster(p,"classB",new Vector3(-6.5f,0,11.0f),furn); DeskCluster(p,"classB",new Vector3(-3.9f,0,11.0f),furn);
            SafeAsset("rug_oval_A","reading rug",p,"classB",new Vector3(-1.8f,.07f,13.1f),0,.68f,new Vector2(2.6f,2.0f),furn,false);
            SafeAsset("armchair_pillows","reading chair",p,"classB",new Vector3(-1.8f,0,13.0f),180,.72f,new Vector2(1.2f,1.2f),furn,true);
            SafeAsset("shelf_B_small_decorated","reading shelf",p,"classB",new Vector3(-6.7f,0,14.0f),0,.82f,new Vector2(1.2f,.9f),furn,true);

            // Staff room — this is intentionally the visual cozy hero room.
            SafeAsset("rug_rectangle_stripes_A","staff rug",p,"staff",new Vector3(3.4f,.07f,10.5f),0,.88f,new Vector2(3.0f,2.2f),furn,false);
            SafeAsset("couch_pillows","staff sofa",p,"staff",new Vector3(2.0f,0,13.4f),0,.82f,new Vector2(2.3f,1.2f),furn,true);
            SafeAsset("armchair_pillows","staff armchair",p,"staff",new Vector3(5.7f,0,12.2f),-90,.72f,new Vector2(1.2f,1.2f),furn,true);
            SafeAsset("table_low","coffee table",p,"staff",new Vector3(3.5f,0,10.9f),0,.55f,new Vector2(1.7f,1.0f),furn,true);
            SafeAsset("lamp_standing","floor lamp",p,"staff",new Vector3(6.0f,0,14.2f),0,.80f,new Vector2(.8f,.8f),furn,true);
            SafeAsset("cactus_medium_A","staff plant",p,"staff",new Vector3(.8f,0,14.1f),0,.82f,new Vector2(.8f,.8f),furn,true);
            Cabinet(p,"personalrummets skåp",new Vector3(5.9f,0,7.2f),180,wood,wood2,metal,furn,"Batterier");

            // Library — shelves and books define the room instead of cubes.
            SafeAsset("rug_oval_A","library rug",p,"library",new Vector3(9.9f,.07f,9.7f),0,.70f,new Vector2(2.2f,1.8f),furn,false);
            SafeAsset("shelf_B_large_decorated","library shelf",p,"library",new Vector3(8.6f,0,13.8f),0,.78f,new Vector2(1.5f,.9f),furn,true);
            SafeAsset("shelf_B_large_decorated","library shelf",p,"library",new Vector3(11.2f,0,13.8f),0,.78f,new Vector2(1.5f,.9f),furn,true);
            SafeAsset("armchair_pillows","library chair",p,"library",new Vector3(9.8f,0,9.7f),180,.70f,new Vector2(1.2f,1.2f),furn,true);
            Cabinet(p,"materialskåpet",new Vector3(10.9f,0,7.2f),180,wood,wood2,metal,furn,"Förband");

            // Janitor room.
            SafeAsset("table_medium","workbench",p,"janitor",new Vector3(15.1f,0,13.0f),0,.62f,new Vector2(2.0f,1.8f),furn,true);
            SafeAsset("shelf_A_big","tool shelf",p,"janitor",new Vector3(16.3f,0,10.0f),90,.78f,new Vector2(1.0f,1.7f),furn,true);
            Cabinet(p,"verktygsskåpet",new Vector3(13.8f,0,7.2f),180,wood,wood2,metal,furn,"Ficklampa");

            PointLight("hall emergency L",p,new Vector3(-7,1.9f,2),C("ff8d42"),5.5f,1.0f,glow);
            PointLight("hall emergency R",p,new Vector3(7,1.9f,2),C("ff8d42"),5.5f,1.0f,glow);
            PointLight("staff warm lamp",p,new Vector3(5.7f,1.6f,13.7f),C("ff9c54"),4f,.9f,glow);
        }

        static void BuildHall(Transform p,Material wall,Material accent,Material trim,Material wood,Material wood2,Material gym,Material line,Material metal,Material furn) {
            Floor("connector floor",p,new Vector3(20.75f,.02f,2.5f),new Vector3(5.5f,.08f,3.0f),accent);
            WallX(p,18,23.5f,1.0f,.9f,wall,trim); WallX(p,18,23.5f,4.0f,.9f,wall,trim);

            Floor("sporthall floor",p,new Vector3(34,.02f,7),new Vector3(21,.09f,18),gym);
            WallX(p,23.5f,44.5f,-2,.48f,wall,trim); WallX(p,23.5f,44.5f,16,1.18f,wall,trim);
            WallZ(p,23.5f,-2,1.0f,1.18f,wall,trim); WallZ(p,23.5f,4.0f,16,1.18f,wall,trim); WallZ(p,44.5f,-2,16,1.18f,wall,trim);

            Decorative("court north",p,new Vector3(34,.078f,13.0f),new Vector3(16.7f,.012f,.09f),line);
            Decorative("court south",p,new Vector3(34,.078f,1.0f),new Vector3(16.7f,.012f,.09f),line);
            Decorative("court west",p,new Vector3(25.65f,.078f,7),new Vector3(.09f,.012f,12f),line);
            Decorative("court east",p,new Vector3(42.35f,.078f,7),new Vector3(.09f,.012f,12f),line);
            Decorative("center line",p,new Vector3(34,.078f,7),new Vector3(.09f,.012f,12f),line);
            Circle(p,new Vector3(34,.08f,7),2.1f,line);

            // Small long-side stand, exactly the requested role, with soft rounded ends.
            Transform stand=Group(p,"small long-side bleacher");
            for(int row=0;row<3;row++) RoundBench(stand,new Vector3(34,.20f+row*.23f,14.0f+row*.40f),new Vector3(10.6f,.15f,.68f),wood,wood2);
            for(int i=-2;i<=2;i++) Box("bleacher support",stand,new Vector3(34+i*2.15f,.26f,14.5f),new Vector3(.11f,.52f,.58f),metal,true);

            SafeAsset("shelf_A_big","sports shelf",p,"hall",new Vector3(43.0f,0,14.0f),90,.82f,new Vector2(1.0f,1.7f),furn,true);
            SafeAsset("cabinet_medium_decorated","sports cabinet",p,"hall",new Vector3(43.0f,0,-.3f),90,.78f,new Vector2(1.0f,1.2f),furn,true);
            GameObject bench=FindProjectModel("bench");
            if(bench){PlaceProject(bench,"locker bench",p,new Vector3(26.3f,0,-.4f),0,.8f);PlaceProject(bench,"locker bench",p,new Vector3(29.6f,0,-.4f),0,.8f);}
        }

        static void BuildWarmLights(Transform p,Material glow) {
            PointLight("entry L",p,new Vector3(-1.35f,2.0f,-.8f),C("ff9a48"),4.5f,1.05f,glow);
            PointLight("entry R",p,new Vector3(1.35f,2.0f,-.8f),C("ff9a48"),4.5f,1.05f,glow);
        }

        static GameObject BuildPlayer(Material coat,Material dark,Material skin) {
            GameObject root=new GameObject("PLAYER");
            CharacterController cc=root.AddComponent<CharacterController>();
            cc.radius=.27f;cc.height=1.08f;cc.center=new Vector3(0,.54f,0);cc.stepOffset=.20f;cc.skinWidth=.035f;
            root.AddComponent<CoziPlayerV57>();

            GameObject body=GameObject.CreatePrimitive(PrimitiveType.Capsule);body.name="winter jacket";body.transform.SetParent(root.transform,false);body.transform.localPosition=new Vector3(0,.63f,0);body.transform.localScale=new Vector3(.46f,.48f,.40f);body.GetComponent<Renderer>().sharedMaterial=coat;NoCollider(body);
            GameObject head=GameObject.CreatePrimitive(PrimitiveType.Sphere);head.name="head";head.transform.SetParent(root.transform,false);head.transform.localPosition=new Vector3(0,1.17f,0);head.transform.localScale=Vector3.one*.35f;head.GetComponent<Renderer>().sharedMaterial=skin;NoCollider(head);
            GameObject hat=GameObject.CreatePrimitive(PrimitiveType.Sphere);hat.name="beanie";hat.transform.SetParent(root.transform,false);hat.transform.localPosition=new Vector3(0,1.31f,0);hat.transform.localScale=new Vector3(.38f,.20f,.38f);hat.GetComponent<Renderer>().sharedMaterial=dark;NoCollider(hat);
            GameObject bag=GameObject.CreatePrimitive(PrimitiveType.Capsule);bag.name="small backpack";bag.transform.SetParent(root.transform,false);bag.transform.localPosition=new Vector3(0,.68f,-.22f);bag.transform.localScale=new Vector3(.30f,.30f,.16f);bag.GetComponent<Renderer>().sharedMaterial=dark;NoCollider(bag);
            return root;
        }

        static void BuildCamera(Transform player) {
            GameObject go=new GameObject("Main Camera");go.tag="MainCamera";
            Camera cam=go.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=7.9f;cam.nearClipPlane=.1f;cam.farClipPlane=120;cam.backgroundColor=C("1b2325");
            go.transform.position=player.position+new Vector3(0,18f,-13.5f);go.transform.LookAt(player.position+Vector3.up*.35f);
            CozyCameraV57 rig=go.AddComponent<CozyCameraV57>();rig.target=player;rig.offset=new Vector3(0,18f,-13.5f);rig.minSize=6.4f;rig.maxSize=10.8f;rig.zoomSpeed=.65f;rig.followSharpness=11f;
            go.AddComponent<AudioListener>();
        }

        static void BuildLighting() {
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=C("788078");RenderSettings.fog=false;
            GameObject sunGo=new GameObject("soft winter sun");Light sun=sunGo.AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=.68f;sun.color=C("f2dfc0");sun.shadows=LightShadows.Soft;sun.shadowStrength=.36f;sunGo.transform.rotation=Quaternion.Euler(50,-32,0);QualitySettings.shadowDistance=50;
        }

        static void DeskCluster(Transform p,string room,Vector3 pos,Material furn) {
            if(!Reserve(room,pos,new Vector2(1.8f,1.9f)))return;
            Asset("table_small","student desk",p,pos,0,.66f,furn);
            Asset("chair_A","student chair",p,pos+new Vector3(0,0,-.78f),0,.56f,furn);
        }

        static void Door(Transform p,string display,Vector3 pos,float yaw,float width,Material wood,Material panel,Material trim,Material metal,bool glass) {
            Transform h=Group(p,"door — "+display);h.position=pos;h.rotation=Quaternion.Euler(0,yaw,0);
            float height=1.32f;
            LocalBox("frame left",h,new Vector3(-width*.5f-.075f,height*.5f,0),new Vector3(.13f,height+.08f,.18f),trim,true);
            LocalBox("frame right",h,new Vector3(width*.5f+.075f,height*.5f,0),new Vector3(.13f,height+.08f,.18f),trim,true);
            LocalBox("frame top",h,new Vector3(0,height+.02f,0),new Vector3(width+.26f,.13f,.18f),trim,true);
            Transform pivot=Group(h,"hinge");pivot.localPosition=new Vector3(-width*.5f,0,0);
            GameObject leaf=LocalBox("door leaf",pivot,new Vector3(width*.5f,height*.47f,0),new Vector3(width,height*.90f,.09f),wood,true);
            LocalBox(glass?"glass inset":"wood inset",pivot,new Vector3(width*.52f,height*.58f,-.052f),new Vector3(width*.62f,height*.38f,.018f),panel,false);
            LocalSphere("handle",pivot,new Vector3(width*.84f,height*.50f,-.09f),Vector3.one*.085f,metal);
            CozyInteractableV57 x=h.gameObject.AddComponent<CozyInteractableV57>();x.kind=CozyInteractableV57.Kind.Door;x.displayName=display;x.radius=1.8f;x.movingPart=pivot;x.closedEuler=Vector3.zero;x.openEuler=new Vector3(0,-98f,0);x.highlightRenderer=leaf.GetComponent<Renderer>();
        }

        static void Cabinet(Transform p,string display,Vector3 pos,float yaw,Material body,Material door,Material metal,Material furn,string item) {
            Transform h=Group(p,"cabinet — "+display);h.position=pos;h.rotation=Quaternion.Euler(0,yaw,0);
            float w=1.32f,ht=1.45f,d=.54f;
            LocalBox("back",h,new Vector3(0,ht*.5f,d*.42f),new Vector3(w,ht,.07f),body,true);
            LocalBox("left side",h,new Vector3(-w*.48f,ht*.5f,0),new Vector3(.07f,ht,d),body,true);
            LocalBox("right side",h,new Vector3(w*.48f,ht*.5f,0),new Vector3(.07f,ht,d),body,true);
            LocalBox("top",h,new Vector3(0,ht-.035f,0),new Vector3(w,.07f,d),body,true);
            LocalBox("bottom",h,new Vector3(0,.05f,0),new Vector3(w,.10f,d),body,true);

            Transform reveal=Group(h,"contents");
            GameObject shelf=Model("shelf_B_small_decorated");
            if(shelf){GameObject s=(GameObject)PrefabUtility.InstantiatePrefab(shelf);if(!s)s=UnityEngine.Object.Instantiate(shelf);s.name="real shelf inside";s.transform.SetParent(reveal,false);s.transform.localPosition=new Vector3(0,.02f,.02f);s.transform.localScale=Vector3.one*.54f;ApplyMaterial(s,furn);}

            Transform lp=Group(h,"left hinge");lp.localPosition=new Vector3(-w*.48f,0,-d*.49f);
            GameObject ld=LocalBox("left door",lp,new Vector3(w*.24f,ht*.50f,0),new Vector3(w*.48f,ht*.90f,.065f),door,true);
            Transform rp=Group(h,"right hinge");rp.localPosition=new Vector3(w*.48f,0,-d*.49f);
            LocalBox("right door",rp,new Vector3(-w*.24f,ht*.50f,0),new Vector3(w*.48f,ht*.90f,.065f),door,true);
            LocalSphere("handle",lp,new Vector3(w*.40f,ht*.50f,-.07f),Vector3.one*.075f,metal);LocalSphere("handle",rp,new Vector3(-w*.40f,ht*.50f,-.07f),Vector3.one*.075f,metal);
            CozyInteractableV57 x=h.gameObject.AddComponent<CozyInteractableV57>();x.kind=CozyInteractableV57.Kind.Cabinet;x.displayName=display;x.itemName=item;x.radius=1.75f;x.movingPart=lp;x.movingPart2=rp;x.closedEuler=Vector3.zero;x.closedEuler2=Vector3.zero;x.openEuler=new Vector3(0,-108,0);x.openEuler2=new Vector3(0,108,0);x.revealOnOpen=reveal;x.highlightRenderer=ld.GetComponent<Renderer>();reveal.gameObject.SetActive(false);
        }

        static void RoomFloor(Transform p,string name,Vector3 c,Vector3 size,Material mat,Material seam,bool planks) {
            Floor(name+" floor",p,c,size,mat);
            if(!planks)return;
            int count=Mathf.Max(4,Mathf.FloorToInt(size.x/1.05f));
            for(int i=1;i<count;i++){float x=c.x-size.x*.5f+i*(size.x/count);Decorative(name+" floor seam",p,new Vector3(x,c.y+.052f,c.z),new Vector3(.018f,.008f,size.z-.25f),seam);}
        }

        static void WallX(Transform p,float x1,float x2,float z,float height,Material wall,Material trim){if(x2-x1<.05f)return;float x=(x1+x2)*.5f;Box("wall",p,new Vector3(x,height*.5f,z),new Vector3(x2-x1,height,.17f),wall,true);Box("wall cap",p,new Vector3(x,height+.025f,z),new Vector3(x2-x1+.04f,.06f,.21f),trim,false);}
        static void WallZ(Transform p,float x,float z1,float z2,float height,Material wall,Material trim){if(z2-z1<.05f)return;float z=(z1+z2)*.5f;Box("wall",p,new Vector3(x,height*.5f,z),new Vector3(.17f,height,z2-z1),wall,true);Box("wall cap",p,new Vector3(x,height+.025f,z),new Vector3(.21f,.06f,z2-z1+.04f),trim,false);}

        static bool SafeAsset(string model,string name,Transform parent,string room,Vector3 pos,float yaw,float scale,Vector2 footprint,Material mat,bool reserve){if(reserve&&!Reserve(room,pos,footprint)){Debug.LogWarning("V58 overlap prevented: "+name);return false;}return Asset(model,name,parent,pos,yaw,scale,mat)!=null;}
        static bool Reserve(string room,Vector3 pos,Vector2 size){if(!Occupied.TryGetValue(room,out List<Rect> list)){list=new List<Rect>();Occupied[room]=list;}Rect r=new Rect(pos.x-size.x*.5f-.10f,pos.z-size.y*.5f-.10f,size.x+.20f,size.y+.20f);foreach(Rect q in list)if(r.Overlaps(q))return false;list.Add(r);return true;}

        static GameObject Asset(string model,string name,Transform parent,Vector3 pos,float yaw,float scale,Material mat){GameObject prefab=Model(model);if(!prefab)return null;GameObject go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);if(!go)go=UnityEngine.Object.Instantiate(prefab);go.name=name;go.transform.SetParent(parent,true);go.transform.position=pos;go.transform.rotation=Quaternion.Euler(0,yaw,0);go.transform.localScale=Vector3.one*scale;if(mat)ApplyMaterial(go,mat);Ground(go,pos.y);return go;}
        static GameObject Model(string n){return AssetDatabase.LoadAssetAtPath<GameObject>(FurnitureDir+"/"+n+".obj");}
        static GameObject FindProjectModel(params string[] names){foreach(string n in names){foreach(string guid in AssetDatabase.FindAssets(n+" t:GameObject")){GameObject g=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));if(g)return g;}}return null;}
        static GameObject PlaceProject(GameObject prefab,string name,Transform parent,Vector3 pos,float yaw,float targetHeight){if(!prefab)return null;GameObject go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);if(!go)go=UnityEngine.Object.Instantiate(prefab);go.name=name;go.transform.SetParent(parent,true);go.transform.position=pos;go.transform.rotation=Quaternion.Euler(0,yaw,0);Bounds b=Bounds(go);if(b.size.y>.02f)go.transform.localScale*=targetHeight/b.size.y;Ground(go,pos.y);return go;}
        static void ApplyMaterial(GameObject go,Material mat){foreach(Renderer r in go.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=mat;}
        static Bounds Bounds(GameObject go){Renderer[] rs=go.GetComponentsInChildren<Renderer>(true);if(rs.Length==0)return new Bounds(go.transform.position,Vector3.one);Bounds b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;}
        static void Ground(GameObject go,float y){Bounds b=Bounds(go);go.transform.position+=Vector3.up*(y-b.min.y);}

        static Material FurnitureMaterial(Shader shader){string path=GeneratedDir+"/furniture.mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(shader){name="KayKit Furniture V58"};AssetDatabase.CreateAsset(m,path);}Texture2D tex=AssetDatabase.LoadAssetAtPath<Texture2D>(FurnitureDir+"/furniturebits_texture.png");if(tex){if(m.HasProperty("_BaseMap"))m.SetTexture("_BaseMap",tex);if(m.HasProperty("_MainTex"))m.SetTexture("_MainTex",tex);}if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",.18f);EditorUtility.SetDirty(m);return m;}
        static Material Mat(string name,Color c,Shader shader,float metal,float smooth){string path=GeneratedDir+"/"+name+".mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(shader){name="V58 "+name};AssetDatabase.CreateAsset(m,path);}if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);if(m.HasProperty("_Color"))m.SetColor("_Color",c);if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",metal);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;}
        static Material Emissive(string name,Color c,Shader shader,float intensity){Material m=Mat(name,c,shader,0,.25f);if(m.HasProperty("_EmissionColor")){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",c*intensity);}return m;}
        static Color C(string hex){ColorUtility.TryParseHtmlString("#"+hex,out Color c);return c;}

        static GameObject Floor(string name,Transform p,Vector3 pos,Vector3 size,Material mat){return Box(name,p,pos,size,mat,size.y>=.04f);}
        static GameObject Decorative(string name,Transform p,Vector3 pos,Vector3 size,Material mat){return Box(name,p,pos,size,mat,false);}
        static GameObject Box(string name,Transform p,Vector3 pos,Vector3 size,Material mat,bool collider){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(p,true);g.transform.position=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;if(!collider)NoCollider(g);return g;}
        static GameObject LocalBox(string name,Transform p,Vector3 pos,Vector3 size,Material mat,bool collider){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;if(!collider)NoCollider(g);return g;}
        static void Sphere(string name,Transform p,Vector3 pos,Vector3 scale,Material mat){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=name;g.transform.SetParent(p,true);g.transform.position=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;NoCollider(g);}
        static void LocalSphere(string name,Transform p,Vector3 pos,Vector3 scale,Material mat){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=name;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;NoCollider(g);}
        static void NoCollider(GameObject g){Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);}
        static Transform Group(Transform p,string name){GameObject g=new GameObject(name);g.transform.SetParent(p,false);return g.transform;}

        static void Snow(Transform p,Vector3 pos,Vector3 scale,Material mat){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name="soft snow";g.transform.SetParent(p,true);g.transform.position=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;NoCollider(g);}
        static void Pine(Transform p,Vector3 pos,Material trunk,Material green){Box("trunk",p,pos+Vector3.up*.55f,new Vector3(.22f,1.1f,.22f),trunk,true);for(int i=0;i<3;i++){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name="pine crown";g.transform.SetParent(p,true);g.transform.position=pos+Vector3.up*(1.05f+i*.45f);g.transform.localScale=new Vector3(1.2f-i*.22f,.25f,1.2f-i*.22f);g.GetComponent<Renderer>().sharedMaterial=green;NoCollider(g);}}
        static void Shrub(Transform p,Vector3 pos,Material green){Sphere("shrub",p,pos+Vector3.up*.28f,new Vector3(.9f,.55f,.75f),green);Sphere("shrub",p,pos+new Vector3(.35f,.25f,.15f),new Vector3(.55f,.45f,.55f),green);}
        static void RoundBench(Transform p,Vector3 pos,Vector3 size,Material wood,Material cap){Box("bleacher seat",p,pos,size,wood,true);float r=size.y*.6f;Sphere("seat end",p,pos+new Vector3(-size.x*.5f,0,0),new Vector3(r,size.y*.9f,size.z*.82f),cap);Sphere("seat end",p,pos+new Vector3(size.x*.5f,0,0),new Vector3(r,size.y*.9f,size.z*.82f),cap);}
        static void Circle(Transform p,Vector3 c,float radius,Material mat){int n=32;for(int i=0;i<n;i++){float a0=i*Mathf.PI*2/n,a1=(i+1)*Mathf.PI*2/n;Vector3 a=c+new Vector3(Mathf.Cos(a0)*radius,0,Mathf.Sin(a0)*radius);Vector3 b=c+new Vector3(Mathf.Cos(a1)*radius,0,Mathf.Sin(a1)*radius);Vector3 d=b-a;GameObject g=Decorative("center circle",p,(a+b)*.5f,new Vector3(.07f,.012f,d.magnitude),mat);g.transform.rotation=Quaternion.LookRotation(d.normalized,Vector3.up);}}
        static void PointLight(string name,Transform p,Vector3 pos,Color color,float range,float intensity,Material bulb){GameObject g=new GameObject(name);g.transform.SetParent(p,true);g.transform.position=pos;Light l=g.AddComponent<Light>();l.type=LightType.Point;l.color=color;l.range=range;l.intensity=intensity;l.shadows=LightShadows.None;GameObject b=GameObject.CreatePrimitive(PrimitiveType.Sphere);b.name="warm bulb";b.transform.SetParent(g.transform,false);b.transform.localScale=Vector3.one*.10f;b.GetComponent<Renderer>().sharedMaterial=bulb;NoCollider(b);}
        static void EnsureBuildSettings(){List<EditorBuildSettingsScene> scenes=EditorBuildSettings.scenes.ToList();if(!scenes.Any(s=>s.path==ScenePath))scenes.Insert(0,new EditorBuildSettingsScene(ScenePath,true));EditorBuildSettings.scenes=scenes.ToArray();}
    }
}
#endif
