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
    public static class RosvikCozyRebootV57 {
        const int Version = 57;
        const string Key = "ROSVIK_COZY_REBOOT_V57";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolReboot.unity";
        const string GeneratedDir = "Assets/Rosvik/GeneratedV57";
        const string FurnitureDir = "Assets/Rosvik/ThirdParty/V57Furniture";
        const string FurnitureBase = "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-Furniture-Bits-1.0/main/addons/kaykit_furniture_bits/Assets/obj/";
        const string FurnitureLicense = "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-Furniture-Bits-1.0/main/LICENSE.txt";

        static readonly string[] FurnitureFiles = {
            "chair_A","chair_B","table_small","table_medium","table_low",
            "cabinet_medium_decorated","cabinet_small_decorated",
            "shelf_A_big","shelf_B_large_decorated","shelf_B_small_decorated",
            "couch_pillows","armchair_pillows","lamp_standing","lamp_table",
            "pictureframe_medium","book_set","rug_rectangle_stripes_A","rug_oval_A","cactus_medium_A"
        };

        static readonly Dictionary<string,List<Rect>> occupied = new Dictionary<string,List<Rect>>();

        static RosvikCozyRebootV57() {
            EditorApplication.delayCall -= Auto;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V57 COZY REBOOT - REBUILD FROM ZERO")]
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
                EnsureFolders();
                EnsureFurnitureAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                occupied.Clear();

                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                scene.name = "CozySchoolReboot";

                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (!shader || !shader.isSupported) shader = Shader.Find("Standard");
                if (!shader) throw new Exception("No supported Lit shader found");

                Material grass = Mat("grass",new Color(.22f,.30f,.20f),shader,.04f,.15f);
                Material grassDark = Mat("grass_dark",new Color(.13f,.22f,.16f),shader,.02f,.12f);
                Material asphalt = Mat("asphalt",new Color(.095f,.105f,.11f),shader,.0f,.18f);
                Material stone = Mat("stone",new Color(.48f,.43f,.36f),shader,.0f,.20f);
                Material snow = Mat("snow",new Color(.88f,.91f,.91f),shader,.0f,.28f);
                Material wall = Mat("wall_warm",new Color(.73f,.68f,.56f),shader,.0f,.22f);
                Material wallAccent = Mat("wall_green",new Color(.26f,.34f,.28f),shader,.0f,.18f);
                Material trim = Mat("trim_cream",new Color(.90f,.84f,.69f),shader,.0f,.28f);
                Material wood = Mat("wood",new Color(.46f,.28f,.15f),shader,.0f,.28f);
                Material woodLight = Mat("wood_light",new Color(.62f,.42f,.23f),shader,.0f,.26f);
                Material woodFloor = Mat("wood_floor",new Color(.43f,.31f,.20f),shader,.0f,.18f);
                Material woodFloorAlt = Mat("wood_floor_alt",new Color(.50f,.36f,.23f),shader,.0f,.18f);
                Material corridor = Mat("corridor",new Color(.46f,.47f,.39f),shader,.0f,.18f);
                Material classFloor = Mat("class_floor",new Color(.53f,.46f,.33f),shader,.0f,.16f);
                Material staffFloor = Mat("staff_floor",new Color(.35f,.40f,.34f),shader,.0f,.16f);
                Material gymFloor = Mat("gym_floor",new Color(.58f,.39f,.22f),shader,.0f,.26f);
                Material gymLine = Mat("gym_line",new Color(.90f,.84f,.66f),shader,.0f,.18f);
                Material metal = Mat("metal",new Color(.14f,.16f,.16f),shader,.35f,.38f);
                Material glass = Mat("glass",new Color(.20f,.36f,.38f),shader,.0f,.60f);
                Material orange = Mat("player_coat",new Color(.72f,.31f,.13f),shader,.0f,.30f);
                Material navy = Mat("player_dark",new Color(.10f,.15f,.18f),shader,.0f,.30f);
                Material skin = Mat("player_skin",new Color(.76f,.55f,.42f),shader,.0f,.24f);
                Material warmLamp = Emissive("warm_lamp",new Color(1f,.54f,.18f),shader,2.2f);
                Material red = Mat("red",new Color(.62f,.20f,.16f),shader,.0f,.22f);
                Material blue = Mat("blue",new Color(.22f,.43f,.48f),shader,.0f,.22f);
                Material furnitureMat = FurnitureMaterial(shader);

                GameObject root = new GameObject("COZY SCHOOL REBOOT V57");
                Transform exterior = Group(root.transform,"EXTERIOR CAMPUS");
                Transform school = Group(root.transform,"SCHOOL CUTAWAY");
                Transform hall = Group(root.transform,"SPORT HALL");
                Transform decor = Group(root.transform,"LIGHTING + DECOR");

                BuildExterior(exterior,grass,grassDark,asphalt,stone,snow,trim,wood,metal,furnitureMat);
                BuildSchool(school,wall,wallAccent,trim,wood,woodLight,corridor,classFloor,staffFloor,glass,metal,red,blue,furnitureMat,warmLamp);
                BuildSportHall(hall,wall,wallAccent,trim,wood,woodLight,gymFloor,gymLine,metal,furnitureMat);
                BuildDecor(decor,warmLamp);

                GameObject player = BuildPlayer(orange,navy,skin);
                player.transform.position = new Vector3(0f,.02f,-11.8f);
                BuildCamera(player.transform);
                BuildLighting();

                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                EditorSceneManager.SaveScene(scene,ScenePath);
                EnsureBuildSettings(ScenePath);
                EditorPrefs.SetInt(Key,Version);
                Selection.activeGameObject = player;
                AssetDatabase.SaveAssets();
                SceneView.RepaintAll();
                Debug.Log("ROSVIK V57 COZY REBOOT: brand-new asset-led hand-built school slice created. Old Rosvik scene untouched.");
            }
            catch(Exception ex) {
                Debug.LogError("ROSVIK V57 COZY REBOOT FAILED: "+ex);
            }
        }

        static void EnsureFolders() {
            Directory.CreateDirectory(GeneratedDir);
            Directory.CreateDirectory(FurnitureDir);
            Directory.CreateDirectory("Assets/Rosvik/Scenes");
        }

        static void EnsureFurnitureAssets() {
            string texturePath = FurnitureDir+"/furniturebits_texture.png";
            using (WebClient wc = new WebClient()) {
                wc.Headers.Add("User-Agent","Rosvik-Unity-Reboot");
                if (!File.Exists(texturePath)) TryDownload(wc,FurnitureBase+"furniturebits_texture.png",texturePath);
                string lic = FurnitureDir+"/LICENSE_KAYKIT_FURNITURE.txt";
                if (!File.Exists(lic)) TryDownload(wc,FurnitureLicense,lic);
                foreach(string n in FurnitureFiles) {
                    string obj = FurnitureDir+"/"+n+".obj";
                    string mtl = FurnitureDir+"/"+n+".mtl";
                    if (!File.Exists(obj)) TryDownload(wc,FurnitureBase+n+".obj",obj);
                    if (!File.Exists(mtl)) TryDownload(wc,FurnitureBase+n+".mtl",mtl);
                }
            }
        }

        static void TryDownload(WebClient wc,string url,string path) {
            try { wc.DownloadFile(url,path); }
            catch(Exception e) { Debug.LogWarning("V57 asset download skipped: "+Path.GetFileName(path)+" — "+e.Message); }
        }

        static void BuildExterior(Transform p,Material grass,Material grassDark,Material asphalt,Material stone,Material snow,Material trim,Material wood,Material metal,Material furnitureMat) {
            Floor("campus lawn",p,new Vector3(8f,-.08f,-8.5f),new Vector3(82f,.16f,23f),grass);
            Floor("school foundation",p,new Vector3(0f,-.04f,7.5f),new Vector3(37.5f,.08f,18.5f),grassDark);
            Floor("hall foundation",p,new Vector3(33.5f,-.04f,7f),new Vector3(23f,.08f,19.5f),grassDark);

            Floor("arrival path",p,new Vector3(0f,.01f,-7.0f),new Vector3(5.2f,.04f,12f),stone);
            for(int i=-5;i<=5;i++) Floor("path joint",p,new Vector3(0f,.035f,-7.0f+i*.95f),new Vector3(5.0f,.015f,.045f),trim);

            Floor("parking",p,new Vector3(-14.7f,.01f,-10.0f),new Vector3(18f,.04f,9.2f),asphalt);
            for(int i=-2;i<=2;i++) Floor("parking line",p,new Vector3(-14.7f+i*3.0f,.035f,-10.0f),new Vector3(.055f,.016f,6.2f),trim);

            // Outdoor assets use the already imported city/nature packs when available.
            GameObject car = FindProjectModel("car_stationwagon","car_sedan","car_hatchback");
            if (car) {
                PlaceModel(car,"parked wagon",p,new Vector3(-18f,0,-10f),0f,1.0f,null);
                PlaceModel(car,"parked car",p,new Vector3(-12f,0,-10f),180f,1.0f,null);
            }
            GameObject bench = FindProjectModel("bench");
            if (bench) {
                PlaceModel(bench,"entry bench",p,new Vector3(-5.3f,0,-4.2f),90f,1.0f,null);
                PlaceModel(bench,"entry bench",p,new Vector3(5.3f,0,-4.2f),-90f,1.0f,null);
            } else {
                Asset("couch_pillows","entry seat",p,new Vector3(-5.5f,0,-4.0f),90f,.78f,furnitureMat);
                Asset("couch_pillows","entry seat",p,new Vector3(5.5f,0,-4.0f),-90f,.78f,furnitureMat);
            }

            GameObject tree = FindProjectModel("tree_pineDefaultA","tree_pineTallA","tree_default_fall");
            GameObject bush = FindProjectModel("plant_bushDetailed","bush");
            Vector3[] trees = {
                new Vector3(-26,0,-15),new Vector3(-24,0,-4),new Vector3(-21,0,-17),
                new Vector3(12,0,-16),new Vector3(17,0,-14),new Vector3(22,0,-16),
                new Vector3(48,0,-10),new Vector3(50,0,2),new Vector3(47,0,15)
            };
            foreach(Vector3 pos in trees) {
                if (tree) PlaceModel(tree,"pine",p,pos,0f,3.2f,null);
                else PineFallback(p,pos,wood,grassDark);
            }
            Vector3[] bushes = { new Vector3(-6.7f,0,-2.8f),new Vector3(6.7f,0,-2.8f),new Vector3(9.5f,0,-13f),new Vector3(15f,0,-13.5f) };
            foreach(Vector3 pos in bushes) {
                if (bush) PlaceModel(bush,"shrub",p,pos,0f,.75f,null);
                else ShrubFallback(p,pos,grassDark);
            }

            // Soft snow shapes only at edges, never across gameplay paths.
            SnowBlob(p,new Vector3(-27f,.02f,-10f),new Vector3(5.0f,.10f,2.0f),snow);
            SnowBlob(p,new Vector3(20f,.02f,-17f),new Vector3(5.5f,.10f,1.8f),snow);
            SnowBlob(p,new Vector3(47f,.02f,-15f),new Vector3(4.0f,.10f,1.4f),snow);
            SnowBlob(p,new Vector3(-8f,.02f,-18f),new Vector3(3.2f,.10f,1.2f),snow);
        }

        static void BuildSchool(Transform p,Material wall,Material accent,Material trim,Material wood,Material woodLight,Material corridor,Material classFloor,Material staffFloor,Material glass,Material metal,Material red,Material blue,Material furnitureMat,Material warmLamp) {
            // Floors: one continuous architectural footprint with room-specific finishes.
            Floor("main corridor",p,new Vector3(0f,.02f,2.0f),new Vector3(35.5f,.08f,5.6f),corridor);
            RoomFloor(p,"classroom A",new Vector3(-12.75f,.025f,10f),new Vector3(9.5f,.09f,11.4f),classFloor,woodLight,true);
            RoomFloor(p,"classroom B",new Vector3(-4.0f,.025f,10f),new Vector3(7.5f,.09f,11.4f),classFloor,woodLight,true);
            RoomFloor(p,"staff room",new Vector3(3.75f,.025f,10f),new Vector3(7.5f,.09f,11.4f),staffFloor,woodLight,false);
            RoomFloor(p,"library",new Vector3(10.0f,.025f,10f),new Vector3(5.0f,.09f,11.4f),classFloor,woodLight,true);
            RoomFloor(p,"janitor",new Vector3(15.0f,.025f,10f),new Vector3(5.0f,.09f,11.4f),corridor,woodLight,false);

            // Outer shell. South/camera-facing wall is intentionally low cutaway, not missing.
            WallX(p,-18f,-2.1f,-1f,.62f,wall,trim);
            WallX(p,2.1f,18f,-1f,.62f,wall,trim);
            WallX(p,-18f,18f,15.7f,1.42f,wall,trim);
            WallZ(p,-18f,-1f,15.7f,1.42f,wall,trim);
            WallZ(p,18f,-1f,1.0f,1.42f,wall,trim);
            WallZ(p,18f,4.0f,15.7f,1.42f,wall,trim);

            // Corridor-to-room wall with five intentional door gaps.
            WallX(p,-18f,-13.5f,4.25f,1.30f,accent,trim);
            WallX(p,-12.0f,-8.1f,4.25f,1.30f,accent,trim);
            WallX(p,-8.1f,-4.75f,4.25f,1.30f,accent,trim);
            WallX(p,-3.25f,-.15f,4.25f,1.30f,accent,trim);
            WallX(p,-.15f,3.05f,4.25f,1.30f,accent,trim);
            WallX(p,4.55f,9.35f,4.25f,1.30f,accent,trim);
            WallX(p,10.65f,14.35f,4.25f,1.30f,accent,trim);
            WallX(p,15.65f,18f,4.25f,1.30f,accent,trim);

            // Room dividers run full depth and make the plan visually complete.
            WallZ(p,-8.1f,4.25f,15.7f,1.30f,wall,trim);
            WallZ(p,-.15f,4.25f,15.7f,1.30f,wall,trim);
            WallZ(p,7.5f,4.25f,15.7f,1.30f,wall,trim);
            WallZ(p,12.5f,4.25f,15.7f,1.30f,wall,trim);

            // Real doors with frame, inset panel and handle.
            Door(p,"huvudentrén",new Vector3(0f,0,-1f),0f,1.8f,wood,woodLight,trim,metal,true);
            Door(p,"klassrum A",new Vector3(-12.75f,0,4.25f),0f,1.35f,wood,woodLight,trim,metal,false);
            Door(p,"klassrum B",new Vector3(-4.0f,0,4.25f),0f,1.35f,wood,woodLight,trim,metal,false);
            Door(p,"personalrummet",new Vector3(3.8f,0,4.25f),0f,1.35f,wood,woodLight,trim,metal,false);
            Door(p,"biblioteket",new Vector3(10.0f,0,4.25f),0f,1.20f,wood,woodLight,trim,metal,false);
            Door(p,"vaktmästarrummet",new Vector3(15.0f,0,4.25f),0f,1.20f,wood,woodLight,trim,metal,false);
            Door(p,"sporthallen",new Vector3(18f,0,2.5f),90f,1.45f,wood,woodLight,trim,metal,false);

            // Reserve generous door swing / approach zones BEFORE furnishing.
            Reserve("classA",new Vector3(-12.75f,0,5.4f),new Vector2(3.2f,2.8f));
            Reserve("classB",new Vector3(-4f,0,5.4f),new Vector2(3.2f,2.8f));
            Reserve("staff",new Vector3(3.8f,0,5.4f),new Vector2(3.2f,2.8f));
            Reserve("library",new Vector3(10f,0,5.4f),new Vector2(2.8f,2.8f));
            Reserve("janitor",new Vector3(15f,0,5.4f),new Vector2(2.8f,2.8f));

            // Hallway cozy details: rugs, seating, plants, wall art. Nothing scattered.
            SafeAsset("rug_rectangle_stripes_A","corridor runner",p,"corridor",new Vector3(-7f,.07f,1.6f),0f,1f,new Vector2(3.2f,2.2f),furnitureMat);
            SafeAsset("rug_rectangle_stripes_A","corridor runner",p,"corridor",new Vector3(7f,.07f,1.6f),0f,1f,new Vector2(3.2f,2.2f),furnitureMat);
            SafeAsset("couch_pillows","waiting couch",p,"corridor",new Vector3(-15.4f,0,1.65f),90f,.80f,new Vector2(1.5f,2.4f),furnitureMat);
            SafeAsset("cactus_medium_A","hall plant",p,"corridor",new Vector3(-10.2f,0,1.25f),0f,.85f,new Vector2(1.0f,1.0f),furnitureMat);
            SafeAsset("cactus_medium_A","hall plant",p,"corridor",new Vector3(12.3f,0,1.25f),0f,.85f,new Vector2(1.0f,1.0f),furnitureMat);

            // Classroom A: six deliberate desk clusters, all spaced on a grid.
            StudentDesk(p,"classA",new Vector3(-15.3f,0,8.0f),0f,furnitureMat);
            StudentDesk(p,"classA",new Vector3(-12.7f,0,8.0f),0f,furnitureMat);
            StudentDesk(p,"classA",new Vector3(-10.1f,0,8.0f),0f,furnitureMat);
            StudentDesk(p,"classA",new Vector3(-15.3f,0,11.1f),0f,furnitureMat);
            StudentDesk(p,"classA",new Vector3(-12.7f,0,11.1f),0f,furnitureMat);
            StudentDesk(p,"classA",new Vector3(-10.1f,0,11.1f),0f,furnitureMat);
            SafeAsset("table_small","teacher desk",p,"classA",new Vector3(-12.7f,0,14.2f),0f,.95f,new Vector2(1.5f,1.5f),furnitureMat);
            SafeAsset("shelf_B_large_decorated","book shelf",p,"classA",new Vector3(-16.8f,0,13.8f),90f,.9f,new Vector2(1.2f,2.1f),furnitureMat);

            // Classroom B: fewer tables + soft reading corner to make rooms visually distinct.
            StudentDesk(p,"classB",new Vector3(-6.5f,0,8.0f),0f,furnitureMat);
            StudentDesk(p,"classB",new Vector3(-3.9f,0,8.0f),0f,furnitureMat);
            StudentDesk(p,"classB",new Vector3(-1.4f,0,8.0f),0f,furnitureMat);
            StudentDesk(p,"classB",new Vector3(-6.5f,0,11.0f),0f,furnitureMat);
            StudentDesk(p,"classB",new Vector3(-3.9f,0,11.0f),0f,furnitureMat);
            SafeAsset("rug_oval_A","reading rug",p,"classB",new Vector3(-1.9f,.07f,13.1f),0f,.75f,new Vector2(2.8f,2.2f),furnitureMat);
            SafeAsset("armchair_pillows","reading chair",p,"classB",new Vector3(-1.6f,0,13.2f),180f,.78f,new Vector2(1.4f,1.4f),furnitureMat,false);
            SafeAsset("shelf_B_small_decorated","class shelf",p,"classB",new Vector3(-6.8f,0,14.2f),0f,.9f,new Vector2(1.4f,1.0f),furnitureMat);

            // Staff room: couch, armchairs, coffee table, rug, lamp and one REAL opening cabinet.
            SafeAsset("rug_rectangle_stripes_A","staff rug",p,"staff",new Vector3(3.7f,.07f,10.2f),0f,.90f,new Vector2(3.4f,2.5f),furnitureMat);
            SafeAsset("couch_pillows","staff couch",p,"staff",new Vector3(2.3f,0,13.4f),0f,.90f,new Vector2(2.6f,1.4f),furnitureMat);
            SafeAsset("armchair_pillows","staff chair",p,"staff",new Vector3(5.8f,0,12.0f),-90f,.82f,new Vector2(1.5f,1.5f),furnitureMat);
            SafeAsset("table_low","coffee table",p,"staff",new Vector3(3.8f,0,11.0f),0f,.68f,new Vector2(2.0f,1.2f),furnitureMat);
            SafeAsset("lamp_standing","standing lamp",p,"staff",new Vector3(6.2f,0,14.2f),0f,.86f,new Vector2(.9f,.9f),furnitureMat);
            SafeAsset("cactus_medium_A","staff plant",p,"staff",new Vector3(1.0f,0,14.2f),0f,.92f,new Vector2(1f,1f),furnitureMat);
            InteractiveCabinet(p,"personalrummets skåp",new Vector3(6.1f,0,7.1f),180f,wood,woodLight,metal,furnitureMat,"Batterier");

            // Library/resource room: real asset shelves carry the room visually.
            SafeAsset("rug_oval_A","library rug",p,"library",new Vector3(10f,.07f,9.5f),0f,.82f,new Vector2(2.6f,2.1f),furnitureMat);
            SafeAsset("shelf_B_large_decorated","library shelf",p,"library",new Vector3(8.4f,0,13.8f),0f,.90f,new Vector2(1.7f,1.1f),furnitureMat);
            SafeAsset("shelf_B_large_decorated","library shelf",p,"library",new Vector3(11.2f,0,13.8f),0f,.90f,new Vector2(1.7f,1.1f),furnitureMat);
            SafeAsset("armchair_pillows","library chair",p,"library",new Vector3(9.8f,0,9.5f),180f,.78f,new Vector2(1.4f,1.4f),furnitureMat,false);
            InteractiveCabinet(p,"materialskåpet",new Vector3(11.1f,0,7.1f),180f,wood,woodLight,metal,furnitureMat,"Förband");

            // Janitor room: workbench, shelf and another tactile cabinet.
            SafeAsset("table_medium","workbench",p,"janitor",new Vector3(14.9f,0,13.3f),0f,.72f,new Vector2(2.4f,2.0f),furnitureMat);
            SafeAsset("shelf_A_big","tool shelf",p,"janitor",new Vector3(16.3f,0,10.2f),90f,.88f,new Vector2(1.2f,1.9f),furnitureMat);
            InteractiveCabinet(p,"verktygsskåpet",new Vector3(13.8f,0,7.2f),180f,wood,woodLight,metal,furnitureMat,"Ficklampa");

            // A few warm practical lights; no debug signage or ground text.
            PointLight("corridor emergency light",p,new Vector3(-7f,2.0f,2.1f),new Color(1f,.48f,.18f),5.5f,1.1f,warmLamp);
            PointLight("corridor emergency light",p,new Vector3(7f,2.0f,2.1f),new Color(1f,.48f,.18f),5.5f,1.1f,warmLamp);
            PointLight("staff lamp glow",p,new Vector3(5.9f,1.7f,13.8f),new Color(1f,.55f,.24f),4.0f,.95f,warmLamp);
        }

        static void BuildSportHall(Transform p,Material wall,Material accent,Material trim,Material wood,Material woodLight,Material floor,Material line,Material metal,Material furnitureMat) {
            Floor("connector",p,new Vector3(20.7f,.02f,2.5f),new Vector3(5.5f,.08f,3.0f),accent);
            WallX(p,18f,23.5f,1f,1.20f,wall,trim);
            WallX(p,18f,23.5f,4f,1.20f,wall,trim);

            Floor("sporthall floor",p,new Vector3(34f,.02f,7f),new Vector3(21f,.09f,18f),floor);
            // Complete shell with a clear connector opening on the west side.
            WallX(p,23.5f,44.5f,-2f,.62f,wall,trim);
            WallX(p,23.5f,44.5f,16f,1.42f,wall,trim);
            WallZ(p,23.5f,-2f,1f,1.42f,wall,trim);
            WallZ(p,23.5f,4f,16f,1.42f,wall,trim);
            WallZ(p,44.5f,-2f,16f,1.42f,wall,trim);

            // Court markings are restrained and aligned.
            Floor("court north line",p,new Vector3(34f,.075f,13.1f),new Vector3(16.8f,.015f,.10f),line);
            Floor("court south line",p,new Vector3(34f,.075f,.8f),new Vector3(16.8f,.015f,.10f),line);
            Floor("court west line",p,new Vector3(25.6f,.075f,7f),new Vector3(.10f,.015f,12.3f),line);
            Floor("court east line",p,new Vector3(42.4f,.075f,7f),new Vector3(.10f,.015f,12.3f),line);
            Floor("center line",p,new Vector3(34f,.075f,7f),new Vector3(.10f,.015f,12.3f),line);
            RingLine(p,new Vector3(34f,.078f,7f),2.2f,line);

            // Small long-side bleacher with clear steps, visually distinct from box clutter.
            Transform bleachers = Group(p,"SMALL LONG-SIDE BLEACHER");
            for(int row=0;row<3;row++) {
                RoundedBench(bleachers,new Vector3(34f,.20f+row*.24f,14.2f+row*.42f),new Vector3(10.8f,.16f,.72f),wood,woodLight);
            }
            for(int i=-2;i<=2;i++) Box("bleacher leg",bleachers,new Vector3(34f+i*2.2f,.28f,14.6f),new Vector3(.12f,.56f,.65f),metal,true);

            // Gear corner uses actual furniture pack shelves; kept off the court.
            SafeAsset("shelf_A_big","sports gear shelf",p,"hall",new Vector3(42.9f,0,14.0f),90f,.95f,new Vector2(1.2f,2.0f),furnitureMat);
            SafeAsset("cabinet_medium_decorated","sports cabinet",p,"hall",new Vector3(42.9f,0,-.3f),90f,.90f,new Vector2(1.2f,1.4f),furnitureMat);
            GameObject bench = FindProjectModel("bench");
            if (bench) {
                PlaceModel(bench,"locker bench",p,new Vector3(26.4f,0,-.3f),0f,1.0f,null);
                PlaceModel(bench,"locker bench",p,new Vector3(29.8f,0,-.3f),0f,1.0f,null);
            }
        }

        static void BuildDecor(Transform p,Material warmLamp) {
            // Warm entrance pools contrast the cool exterior without flooding the whole map.
            PointLight("entrance light L",p,new Vector3(-1.4f,2.2f,-.8f),new Color(1f,.52f,.20f),5.0f,1.15f,warmLamp);
            PointLight("entrance light R",p,new Vector3(1.4f,2.2f,-.8f),new Color(1f,.52f,.20f),5.0f,1.15f,warmLamp);
        }

        static GameObject BuildPlayer(Material coat,Material dark,Material skin) {
            GameObject root = new GameObject("PLAYER");
            CharacterController cc = root.AddComponent<CharacterController>();
            cc.radius=.31f; cc.height=1.42f; cc.center=new Vector3(0,.71f,0); cc.stepOffset=.24f; cc.skinWidth=.04f;
            root.AddComponent<CoziPlayerV57>();

            GameObject body=GameObject.CreatePrimitive(PrimitiveType.Capsule); body.name="winter coat"; body.transform.SetParent(root.transform,false); body.transform.localPosition=new Vector3(0,.72f,0); body.transform.localScale=new Vector3(.52f,.56f,.44f); body.GetComponent<Renderer>().sharedMaterial=coat; RemoveCollider(body);
            GameObject head=GameObject.CreatePrimitive(PrimitiveType.Sphere); head.name="head"; head.transform.SetParent(root.transform,false); head.transform.localPosition=new Vector3(0,1.42f,0); head.transform.localScale=Vector3.one*.40f; head.GetComponent<Renderer>().sharedMaterial=skin; RemoveCollider(head);
            GameObject hat=GameObject.CreatePrimitive(PrimitiveType.Sphere); hat.name="beanie"; hat.transform.SetParent(root.transform,false); hat.transform.localPosition=new Vector3(0,1.57f,0); hat.transform.localScale=new Vector3(.42f,.23f,.42f); hat.GetComponent<Renderer>().sharedMaterial=dark; RemoveCollider(hat);
            GameObject pack=GameObject.CreatePrimitive(PrimitiveType.Capsule); pack.name="backpack"; pack.transform.SetParent(root.transform,false); pack.transform.localPosition=new Vector3(0,.83f,-.24f); pack.transform.localScale=new Vector3(.34f,.38f,.20f); pack.GetComponent<Renderer>().sharedMaterial=dark; RemoveCollider(pack);
            return root;
        }

        static void BuildCamera(Transform player) {
            GameObject go=new GameObject("Main Camera");
            go.tag="MainCamera";
            Camera cam=go.AddComponent<Camera>();
            cam.orthographic=true; cam.orthographicSize=8.6f; cam.nearClipPlane=.1f; cam.farClipPlane=120f;
            cam.backgroundColor=new Color(.11f,.14f,.15f);
            go.transform.position=player.position+new Vector3(0f,17.5f,-14.5f);
            go.transform.LookAt(player.position+Vector3.up*.4f);
            CozyCameraV57 rig=go.AddComponent<CozyCameraV57>(); rig.target=player; rig.offset=new Vector3(0f,17.5f,-14.5f); rig.minSize=6.7f; rig.maxSize=11.2f;
            go.AddComponent<AudioListener>();
        }

        static void BuildLighting() {
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight=new Color(.46f,.48f,.44f);
            RenderSettings.fog=false;
            GameObject sunGo=new GameObject("Winter Sun");
            Light sun=sunGo.AddComponent<Light>(); sun.type=LightType.Directional; sun.intensity=.72f; sun.color=new Color(.93f,.88f,.78f); sun.shadows=LightShadows.Soft; sun.shadowStrength=.42f;
            sunGo.transform.rotation=Quaternion.Euler(52f,-28f,0f);
            QualitySettings.shadowDistance=55f;
        }

        static void StudentDesk(Transform p,string room,Vector3 pos,float yaw,Material furnitureMat) {
            if(!Reserve(room,pos,new Vector2(1.9f,2.0f))) return;
            Asset("table_small","student table",p,pos,yaw,.72f,furnitureMat);
            Vector3 back=Quaternion.Euler(0,yaw,0)*new Vector3(0,0,-.88f);
            Asset("chair_A","student chair",p,pos+back,yaw,.62f,furnitureMat);
        }

        static void InteractiveCabinet(Transform p,string display,Vector3 pos,float yaw,Material body,Material doorMat,Material metal,Material furnitureMat,string loot) {
            string key="cabinet_"+display;
            Transform holder=Group(p,key); holder.position=pos; holder.rotation=Quaternion.Euler(0,yaw,0);
            float w=1.42f,h=1.62f,d=.58f;
            LocalBox("cabinet back",holder,new Vector3(0,h*.5f,d*.42f),new Vector3(w,h,.08f),body,true);
            LocalBox("cabinet left",holder,new Vector3(-w*.48f,h*.5f,0),new Vector3(.08f,h,d),body,true);
            LocalBox("cabinet right",holder,new Vector3(w*.48f,h*.5f,0),new Vector3(.08f,h,d),body,true);
            LocalBox("cabinet top",holder,new Vector3(0,h-.04f,0),new Vector3(w,.08f,d),body,true);
            LocalBox("cabinet bottom",holder,new Vector3(0,.05f,0),new Vector3(w,.10f,d),body,true);

            Transform contents=Group(holder,"visible contents");
            contents.localPosition=Vector3.zero;
            GameObject shelf=Model("shelf_B_small_decorated");
            if(shelf) {
                GameObject inst=(GameObject)PrefabUtility.InstantiatePrefab(shelf); if(!inst)inst=UnityEngine.Object.Instantiate(shelf);
                inst.name="real shelf contents"; inst.transform.SetParent(contents,false); inst.transform.localPosition=new Vector3(0,.02f,.02f); inst.transform.localRotation=Quaternion.identity; inst.transform.localScale=Vector3.one*.62f; ApplyMaterial(inst,furnitureMat);
            } else {
                for(int i=0;i<3;i++) LocalBox("shelf",contents,new Vector3(0,.38f+i*.38f,.02f),new Vector3(1.18f,.05f,.44f),doorMat,false);
            }

            Transform lp=Group(holder,"left door pivot"); lp.localPosition=new Vector3(-w*.48f,0,-d*.49f);
            GameObject ld=LocalBox("left cabinet door",lp,new Vector3(w*.24f,h*.52f,0),new Vector3(w*.48f,h*.92f,.07f),doorMat,true);
            Transform rp=Group(holder,"right door pivot"); rp.localPosition=new Vector3(w*.48f,0,-d*.49f);
            GameObject rd=LocalBox("right cabinet door",rp,new Vector3(-w*.24f,h*.52f,0),new Vector3(w*.48f,h*.92f,.07f),doorMat,true);
            LocalSphere("handle",lp,new Vector3(w*.40f,h*.52f,-.07f),Vector3.one*.09f,metal);
            LocalSphere("handle",rp,new Vector3(-w*.40f,h*.52f,-.07f),Vector3.one*.09f,metal);

            CozyInteractableV57 x=holder.gameObject.AddComponent<CozyInteractableV57>(); x.kind=CozyInteractableV57.Kind.Cabinet; x.displayName=display; x.itemName=loot; x.radius=2.0f; x.movingPart=lp; x.movingPart2=rp; x.closedEuler=Vector3.zero; x.closedEuler2=Vector3.zero; x.openEuler=new Vector3(0,-105f,0); x.openEuler2=new Vector3(0,105f,0); x.revealOnOpen=contents; x.highlightRenderer=ld.GetComponent<Renderer>();
            contents.gameObject.SetActive(false);
        }

        static void Door(Transform p,string display,Vector3 pos,float yaw,float width,Material wood,Material panel,Material trim,Material metal,bool glassDoor) {
            Transform holder=Group(p,"door "+display); holder.position=pos; holder.rotation=Quaternion.Euler(0,yaw,0);
            float h=1.38f;
            LocalBox("frame left",holder,new Vector3(-width*.5f-.08f,h*.5f,0),new Vector3(.14f,h+.10f,.20f),trim,true);
            LocalBox("frame right",holder,new Vector3(width*.5f+.08f,h*.5f,0),new Vector3(.14f,h+.10f,.20f),trim,true);
            LocalBox("frame top",holder,new Vector3(0,h+.04f,0),new Vector3(width+.30f,.14f,.20f),trim,true);
            Transform pivot=Group(holder,"hinge"); pivot.localPosition=new Vector3(-width*.5f,0,0);
            GameObject leaf=LocalBox("door leaf",pivot,new Vector3(width*.5f,h*.48f,0),new Vector3(width,h*.92f,.10f),glassDoor?panel:wood,true);
            if(!glassDoor) LocalBox("inset panel",pivot,new Vector3(width*.52f,h*.60f,-.065f),new Vector3(width*.68f,h*.42f,.025f),panel,false);
            else LocalBox("glass pane",pivot,new Vector3(width*.52f,h*.60f,-.065f),new Vector3(width*.64f,h*.45f,.025f),panel,false);
            LocalSphere("handle",pivot,new Vector3(width*.83f,h*.52f,-.10f),Vector3.one*.10f,metal);
            CozyInteractableV57 x=holder.gameObject.AddComponent<CozyInteractableV57>(); x.kind=CozyInteractableV57.Kind.Door; x.displayName=display; x.radius=2.0f; x.movingPart=pivot; x.closedEuler=Vector3.zero; x.openEuler=new Vector3(0,-96f,0); x.highlightRenderer=leaf.GetComponent<Renderer>();
        }

        static void RoomFloor(Transform p,string name,Vector3 c,Vector3 size,Material baseMat,Material accent,bool planks) {
            Floor(name+" floor",p,c,size,baseMat);
            if(!planks)return;
            float minX=c.x-size.x*.5f+.45f, maxX=c.x+size.x*.5f-.45f;
            int n=Mathf.Max(3,Mathf.FloorToInt((maxX-minX)/1.05f));
            for(int i=0;i<n;i++) {
                float x=Mathf.Lerp(minX,maxX,(i+.5f)/n);
                Floor(name+" plank seam",p,new Vector3(x,c.y+.055f,c.z),new Vector3(.025f,.008f,size.z-.35f),accent);
            }
        }

        static void WallX(Transform p,float x1,float x2,float z,float h,Material wall,Material trim) {
            float len=x2-x1;if(len<=.05f)return;float x=(x1+x2)*.5f;
            Box("wall",p,new Vector3(x,h*.5f,z),new Vector3(len,h,.18f),wall,true);
            Box("wall cap",p,new Vector3(x,h+.035f,z),new Vector3(len+.05f,.07f,.23f),trim,false);
        }
        static void WallZ(Transform p,float x,float z1,float z2,float h,Material wall,Material trim) {
            float len=z2-z1;if(len<=.05f)return;float z=(z1+z2)*.5f;
            Box("wall",p,new Vector3(x,h*.5f,z),new Vector3(.18f,h,len),wall,true);
            Box("wall cap",p,new Vector3(x,h+.035f,z),new Vector3(.23f,.07f,len+.05f),trim,false);
        }

        static void RingLine(Transform p,Vector3 c,float radius,Material mat) {
            int segments=32;
            for(int i=0;i<segments;i++) {
                float a0=(i/(float)segments)*Mathf.PI*2f,a1=((i+1)/(float)segments)*Mathf.PI*2f;
                Vector3 p0=c+new Vector3(Mathf.Cos(a0)*radius,0,Mathf.Sin(a0)*radius);
                Vector3 p1=c+new Vector3(Mathf.Cos(a1)*radius,0,Mathf.Sin(a1)*radius);
                Vector3 mid=(p0+p1)*.5f;Vector3 d=p1-p0;float len=d.magnitude;
                GameObject g=Box("center circle",p,new Vector3(mid.x,c.y,mid.z),new Vector3(.08f,.014f,len),mat,false);g.transform.rotation=Quaternion.LookRotation(d.normalized,Vector3.up);
            }
        }

        static void RoundedBench(Transform p,Vector3 pos,Vector3 size,Material wood,Material edge) {
            Box("bleacher seat",p,pos,size,wood,true);
            float r=size.y*.58f;
            Sphere("seat end",p,pos+new Vector3(-size.x*.5f,0,0),new Vector3(r,size.y*.9f,size.z*.85f),edge);
            Sphere("seat end",p,pos+new Vector3(size.x*.5f,0,0),new Vector3(r,size.y*.9f,size.z*.85f),edge);
        }

        static void SnowBlob(Transform p,Vector3 pos,Vector3 size,Material mat) {
            GameObject s=GameObject.CreatePrimitive(PrimitiveType.Sphere);s.name="soft snowbank";s.transform.SetParent(p,true);s.transform.position=pos;s.transform.localScale=size;s.GetComponent<Renderer>().sharedMaterial=mat;RemoveCollider(s);
        }
        static void PineFallback(Transform p,Vector3 pos,Material trunk,Material green) {
            Box("pine trunk",p,pos+Vector3.up*.65f,new Vector3(.24f,1.3f,.24f),trunk,true);
            for(int i=0;i<3;i++){GameObject c=GameObject.CreatePrimitive(PrimitiveType.Cylinder);c.name="pine crown";c.transform.SetParent(p,true);c.transform.position=pos+Vector3.up*(1.1f+i*.5f);c.transform.localScale=new Vector3(1.4f-i*.25f,.28f,1.4f-i*.25f);c.GetComponent<Renderer>().sharedMaterial=green;RemoveCollider(c);}
        }
        static void ShrubFallback(Transform p,Vector3 pos,Material green) {
            Sphere("shrub",p,pos+Vector3.up*.35f,new Vector3(1.0f,.65f,.85f),green);
            Sphere("shrub",p,pos+new Vector3(.4f,.32f,.15f),new Vector3(.65f,.55f,.65f),green);
        }

        static bool SafeAsset(string model,string name,Transform p,string room,Vector3 pos,float yaw,float scale,Vector2 footprint,Material mat,bool reserve=true) {
            if(reserve && !Reserve(room,pos,footprint)) { Debug.LogWarning("V57 skipped overlapping prop: "+name); return false; }
            return Asset(model,name,p,pos,yaw,scale,mat)!=null;
        }
        static bool Reserve(string room,Vector3 pos,Vector2 footprint) {
            if(!occupied.TryGetValue(room,out List<Rect> list)){list=new List<Rect>();occupied[room]=list;}
            Rect r=new Rect(pos.x-footprint.x*.5f-.12f,pos.z-footprint.y*.5f-.12f,footprint.x+.24f,footprint.y+.24f);
            foreach(Rect q in list) if(r.Overlaps(q)) return false;
            list.Add(r);return true;
        }

        static GameObject Asset(string model,string name,Transform p,Vector3 pos,float yaw,float scale,Material mat) {
            GameObject prefab=Model(model);if(!prefab)return null;
            GameObject go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);if(!go)go=UnityEngine.Object.Instantiate(prefab);
            go.name=name;go.transform.SetParent(p,true);go.transform.position=pos;go.transform.rotation=Quaternion.Euler(0,yaw,0);go.transform.localScale=Vector3.one*scale;
            if(mat)ApplyMaterial(go,mat);Ground(go,pos.y);return go;
        }
        static GameObject Model(string n) { return AssetDatabase.LoadAssetAtPath<GameObject>(FurnitureDir+"/"+n+".obj"); }

        static GameObject FindProjectModel(params string[] names) {
            foreach(string n in names) {
                string[] guids=AssetDatabase.FindAssets(n+" t:GameObject");
                foreach(string guid in guids) {
                    string path=AssetDatabase.GUIDToAssetPath(guid);GameObject g=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(g)return g;
                }
            }
            return null;
        }
        static GameObject PlaceModel(GameObject prefab,string name,Transform p,Vector3 pos,float yaw,float targetHeight,Material mat) {
            if(!prefab)return null;GameObject go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);if(!go)go=UnityEngine.Object.Instantiate(prefab);go.name=name;go.transform.SetParent(p,true);go.transform.position=pos;go.transform.rotation=Quaternion.Euler(0,yaw,0);
            Bounds b=BoundsOf(go);if(b.size.y>.02f){float s=targetHeight/b.size.y;go.transform.localScale*=s;}if(mat)ApplyMaterial(go,mat);Ground(go,pos.y);return go;
        }
        static void ApplyMaterial(GameObject go,Material mat){foreach(Renderer r in go.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=mat;}
        static void Ground(GameObject go,float y){Bounds b=BoundsOf(go);go.transform.position+=Vector3.up*(y-b.min.y);}
        static Bounds BoundsOf(GameObject go){Renderer[] rs=go.GetComponentsInChildren<Renderer>(true);if(rs.Length==0)return new Bounds(go.transform.position,Vector3.one);Bounds b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;}

        static Material FurnitureMaterial(Shader shader) {
            string path=GeneratedDir+"/kaykit_furniture.mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(path);
            Texture2D tex=AssetDatabase.LoadAssetAtPath<Texture2D>(FurnitureDir+"/furniturebits_texture.png");
            if(!m){m=new Material(shader){name="KayKit Furniture V57"};AssetDatabase.CreateAsset(m,path);}
            if(tex){if(m.HasProperty("_BaseMap"))m.SetTexture("_BaseMap",tex);if(m.HasProperty("_MainTex"))m.SetTexture("_MainTex",tex);}if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",.20f);return m;
        }
        static Material Mat(string name,Color c,Shader shader,float metallic,float smooth){string path=GeneratedDir+"/"+name+".mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(shader){name="V57 "+name};AssetDatabase.CreateAsset(m,path);}if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);if(m.HasProperty("_Color"))m.SetColor("_Color",c);if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",metallic);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;}
        static Material Emissive(string name,Color c,Shader shader,float intensity){Material m=Mat(name,c,shader,0,.25f);Color e=c*intensity;if(m.HasProperty("_EmissionColor")){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",e);}return m;}

        static GameObject Floor(string name,Transform p,Vector3 pos,Vector3 size,Material mat){return Box(name,p,pos,size,mat,false);}
        static GameObject Box(string name,Transform p,Vector3 pos,Vector3 size,Material mat,bool collider){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(p,true);g.transform.position=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;if(!collider)RemoveCollider(g);return g;}
        static GameObject LocalBox(string name,Transform p,Vector3 localPos,Vector3 size,Material mat,bool collider){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(p,false);g.transform.localPosition=localPos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;if(!collider)RemoveCollider(g);return g;}
        static void Sphere(string name,Transform p,Vector3 pos,Vector3 scale,Material mat){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=name;g.transform.SetParent(p,true);g.transform.position=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;RemoveCollider(g);}
        static void LocalSphere(string name,Transform p,Vector3 pos,Vector3 scale,Material mat){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=name;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;RemoveCollider(g);}
        static void RemoveCollider(GameObject g){Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);}
        static Transform Group(Transform p,string name){GameObject g=new GameObject(name);g.transform.SetParent(p,false);return g.transform;}

        static void PointLight(string name,Transform p,Vector3 pos,Color c,float range,float intensity,Material bulbMat){GameObject g=new GameObject(name);g.transform.SetParent(p,true);g.transform.position=pos;Light l=g.AddComponent<Light>();l.type=LightType.Point;l.color=c;l.range=range;l.intensity=intensity;l.shadows=LightShadows.None;GameObject bulb=GameObject.CreatePrimitive(PrimitiveType.Sphere);bulb.name="warm bulb";bulb.transform.SetParent(g.transform,false);bulb.transform.localScale=Vector3.one*.12f;bulb.GetComponent<Renderer>().sharedMaterial=bulbMat;RemoveCollider(bulb);}

        static void EnsureBuildSettings(string scenePath){List<EditorBuildSettingsScene> scenes=EditorBuildSettings.scenes.ToList();if(!scenes.Any(s=>s.path==scenePath))scenes.Insert(0,new EditorBuildSettingsScene(scenePath,true));EditorBuildSettings.scenes=scenes.ToArray();}
    }
}
#endif
