#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikCozyArtOverhaulV64 {
        const int Version = 64;
        const string Key = "ROSVIK_COZY_ART_V64";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string Generated58 = "Assets/Rosvik/GeneratedV58";
        const string Generated64 = "Assets/Rosvik/GeneratedV64";
        const string FurnitureDir = "Assets/Rosvik/ThirdParty/V58Furniture";
        const string GroupName = "COZY ART OVERHAUL V64";

        static RosvikCozyArtOverhaulV64() {
            if (EditorPrefs.GetInt(Key, 0) >= Version) return;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V64 COZY ART OVERHAUL - MAKE IT BEAUTIFUL")]
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
            catch (Exception ex) { Debug.LogError("V64 COZY ART OVERHAUL FAILED: " + ex); }
        }

        static void Apply() {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Directory.CreateDirectory(Generated64);
            Shader shader = PickShader();
            if (!shader) throw new Exception("No compatible lit shader found.");

            // Re-art-direct the whole base palette first. This changes every existing V58 surface,
            // not just a few props, so the scene reads as one intentional game rather than a blockout.
            Recolor58("grass", C("314b39"), .10f);
            Recolor58("grass_dark", C("22372d"), .08f);
            Recolor58("snow", C("e1e4dc"), .22f);
            Recolor58("asphalt", C("202827"), .18f);
            Recolor58("stone", C("81796b"), .20f);
            Recolor58("wall", C("cdbfa4"), .18f);
            Recolor58("wall_green", C("52685a"), .16f);
            Recolor58("trim", C("eee0b9"), .26f);
            Recolor58("wood", C("75472d"), .22f);
            Recolor58("wood_light", C("a87349"), .20f);
            Recolor58("corridor", C("697166"), .14f);
            Recolor58("classroom", C("95805f"), .15f);
            Recolor58("staff", C("687b6d"), .14f);
            Recolor58("gym", C("a16a3c"), .18f);
            Recolor58("line", C("f1ddb0"), .16f);
            Recolor58("metal", C("263234"), .28f);
            Recolor58("glass", C("426f73"), .34f);

            Material cream = Mat("cream", C("eee2c4"), shader, 0f, .22f);
            Material creamDark = Mat("cream_dark", C("c6b894"), shader, 0f, .18f);
            Material mustard = Mat("mustard", C("d39a4a"), shader, 0f, .20f);
            Material forest = Mat("forest", C("294b3d"), shader, 0f, .16f);
            Material chalk = Mat("chalkboard", C("243f37"), shader, 0f, .12f);
            Material cork = Mat("cork", C("9a6741"), shader, 0f, .12f);
            Material radiator = Mat("radiator", C("d7d2c2"), shader, .05f, .25f);
            Material darkWood = Mat("dark_wood", C("543321"), shader, 0f, .20f);
            Material burgundy = Mat("burgundy", C("7a3e3b"), shader, 0f, .16f);
            Material teal = Mat("teal", C("3f6b67"), shader, 0f, .16f);
            Material softBlue = Mat("soft_blue", C("5b7b82"), shader, 0f, .22f);
            Material linen = Mat("linen", C("c9b990"), shader, 0f, .12f);
            Material black = Mat("black", C("182020"), shader, .1f, .18f);
            Material glow = Emissive("warm_glow", C("ffab5a"), shader, 2.2f);
            Material furn = AssetDatabase.LoadAssetAtPath<Material>(Generated58 + "/furniture.mat");

            GameObject old = GameObject.Find(GroupName);
            if (old) UnityEngine.Object.DestroyImmediate(old);
            GameObject root = new GameObject(GroupName);
            Transform architecture = Group(root.transform, "ARCHITECTURE POLISH");
            Transform roomArt = Group(root.transform, "ROOM ART + PROPS");
            Transform gymArt = Group(root.transform, "SPORT HALL ART");
            Transform exterior = Group(root.transform, "ENTRANCE + EXTERIOR ART");
            Transform lights = Group(root.transform, "WARM LIGHTING");

            BuildArchitecture(architecture, cream, creamDark, darkWood, radiator, softBlue, chalk, cork, mustard, forest);
            BuildRooms(roomArt, furn, mustard, teal, burgundy, cream, darkWood, glow, black);
            BuildGym(gymArt, cream, darkWood, teal, burgundy, black, softBlue, glow);
            BuildExterior(exterior, cream, darkWood, mustard, forest, glow, black);
            BuildLighting(lights, glow);
            TuneCameraAndWorld();

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorPrefs.SetInt(Key, Version);
            SceneView.RepaintAll();
            Debug.Log("V64 COZY ART OVERHAUL COMPLETE — full palette, architectural trim, windows, boards, radiators, room identity, warm light pools, sporthall detail and entrance art added. Gameplay geometry was left intact.");
        }

        static void BuildArchitecture(Transform p, Material cream, Material creamDark, Material wood, Material radiator, Material glass, Material chalk, Material cork, Material mustard, Material forest) {
            // Strong, consistent skirting makes the dollhouse cutaway feel intentional rather than unfinished.
            SkirtX(p, -17.8f, 17.8f, 15.50f, creamDark);
            SkirtX(p, -17.8f, 17.8f, 4.13f, creamDark);
            SkirtZ(p, -17.86f, -0.8f, 15.45f, creamDark);
            SkirtZ(p, -8.12f, 4.35f, 15.45f, creamDark);
            SkirtZ(p, -.37f, 4.35f, 15.45f, creamDark);
            SkirtZ(p, 7.32f, 4.35f, 15.45f, creamDark);
            SkirtZ(p, 12.42f, 4.35f, 15.45f, creamDark);
            SkirtZ(p, 17.86f, -0.8f, 15.45f, creamDark);

            // Classroom back walls: chalkboards plus two windows each. This instantly reads as school.
            WallPanel(p, "chalkboard A", new Vector3(-13.0f, .69f, 15.585f), new Vector3(2.65f, .58f, .035f), chalk, cream);
            Window(p, new Vector3(-16.10f, .68f, 15.58f), 1.85f, .60f, glass, cream);
            Window(p, new Vector3(-9.95f, .68f, 15.58f), 1.45f, .60f, glass, cream);
            WallPanel(p, "chalkboard B", new Vector3(-4.25f, .69f, 15.585f), new Vector3(2.35f, .58f, .035f), chalk, cream);
            Window(p, new Vector3(-6.75f, .68f, 15.58f), 1.25f, .60f, glass, cream);
            Window(p, new Vector3(-1.80f, .68f, 15.58f), 1.25f, .60f, glass, cream);

            // Cozy staff/library/janitor glazing.
            Window(p, new Vector3(2.15f, .68f, 15.58f), 1.85f, .60f, glass, cream);
            Window(p, new Vector3(5.25f, .68f, 15.58f), 1.85f, .60f, glass, cream);
            Window(p, new Vector3(9.9f, .68f, 15.58f), 2.15f, .60f, glass, cream);
            Window(p, new Vector3(15.1f, .68f, 15.58f), 1.55f, .52f, glass, cream);

            // Radiators under windows give the Swedish school interior a recognizable everyday feel.
            Radiator(p, new Vector3(-16.10f, .23f, 15.38f), 1.55f, radiator);
            Radiator(p, new Vector3(-6.75f, .23f, 15.38f), 1.10f, radiator);
            Radiator(p, new Vector3(2.15f, .23f, 15.38f), 1.55f, radiator);
            Radiator(p, new Vector3(9.9f, .23f, 15.38f), 1.65f, radiator);

            // Corridor boards, low enough to read clearly with the cutaway camera.
            WallPanel(p, "notice board classes", new Vector3(-10.25f, .63f, 4.04f), new Vector3(2.20f, .48f, .032f), cork, wood);
            WallPanel(p, "notice board staff", new Vector3(6.35f, .63f, 4.04f), new Vector3(1.55f, .48f, .032f), forest, cream);
            AddNote(p, new Vector3(-10.72f, .65f, 4.00f), mustard);
            AddNote(p, new Vector3(-10.18f, .65f, 4.00f), cream);
            AddNote(p, new Vector3(-9.65f, .65f, 4.00f), forest);

            // Thresholds visually anchor every doorway and eliminate the floating-door look.
            Threshold(p, new Vector3(0, .095f, -1.0f), 2.1f, wood);
            Threshold(p, new Vector3(-13, .095f, 4.25f), 1.45f, wood);
            Threshold(p, new Vector3(-4.25f, .095f, 4.25f), 1.45f, wood);
            Threshold(p, new Vector3(3.45f, .095f, 4.25f), 1.45f, wood);
            Threshold(p, new Vector3(9.9f, .095f, 4.25f), 1.25f, wood);
            Threshold(p, new Vector3(15.05f, .095f, 4.25f), 1.25f, wood);
        }

        static void BuildRooms(Transform p, Material furn, Material mustard, Material teal, Material burgundy, Material cream, Material wood, Material glow, Material black) {
            // Classroom A — reading corner and teacher wall detail, without touching the desk footprints.
            PlaceAsset("rug_oval_A", "class A reading rug", p, new Vector3(-16.0f, .075f, 13.3f), 0, .62f, furn, false);
            PlaceAsset("armchair_pillows", "class A reading chair", p, new Vector3(-16.0f, 0, 13.25f), 155, .62f, furn, true);
            PlaceAssetAt("book_set", "books on teacher desk", p, new Vector3(-12.55f, .73f, 13.95f), 25, .48f, furn);
            PlaceAsset("pictureframe_medium", "class A wall art", p, new Vector3(-17.20f, .10f, 11.5f), 90, .60f, furn, false);

            // Classroom B — extra shelf detail and plant near the reading zone.
            PlaceAsset("cactus_medium_A", "class B plant", p, new Vector3(-6.95f, 0, 12.8f), 0, .62f, furn, true);
            PlaceAssetAt("book_set", "reading books", p, new Vector3(-6.55f, .73f, 14.0f), -15, .44f, furn);

            // Staff room — hero cozy space: side table, table lamp, pictures and coffee-corner silhouette.
            PlaceAsset("table_small", "staff side table", p, new Vector3(.95f, 0, 8.1f), 0, .55f, furn, true);
            PlaceAssetAt("lamp_table", "staff table lamp", p, new Vector3(.95f, .67f, 8.1f), 0, .62f, furn);
            PlaceAssetAt("book_set", "staff magazines", p, new Vector3(3.55f, .53f, 10.9f), 12, .42f, furn);
            PlaceAsset("pictureframe_medium", "staff wall picture one", p, new Vector3(.10f, .10f, 11.6f), 90, .62f, furn, false);
            PlaceAsset("pictureframe_medium", "staff wall picture two", p, new Vector3(.10f, .10f, 13.1f), 90, .54f, furn, false);
            // A low kitchenette strip against the back wall: deliberately wall-hugging, not free-scattered.
            Box("staff kitchenette base", p, new Vector3(4.15f, .32f, 14.95f), new Vector3(3.55f, .64f, .62f), teal, true);
            Box("staff countertop", p, new Vector3(4.15f, .67f, 14.90f), new Vector3(3.65f, .10f, .72f), cream, false);
            Box("coffee machine", p, new Vector3(5.25f, .91f, 14.82f), new Vector3(.43f, .40f, .36f), black, false);
            Sphere("coffee pot", p, new Vector3(5.25f, 1.13f, 14.74f), new Vector3(.26f, .22f, .26f), burgundy);
            PointLight("staff table lamp glow", p, new Vector3(.95f, 1.18f, 8.1f), C("ffad67"), 3.3f, .48f, glow);

            // Library — make the center feel designed, not empty.
            PlaceAsset("table_low", "library low table", p, new Vector3(9.9f, 0, 11.15f), 0, .54f, furn, true);
            PlaceAssetAt("book_set", "library display books", p, new Vector3(9.9f, .42f, 11.15f), 20, .52f, furn);
            PlaceAsset("lamp_standing", "library reading lamp", p, new Vector3(11.45f, 0, 9.45f), 0, .66f, furn, true);
            PointLight("library reading glow", p, new Vector3(11.45f, 1.30f, 9.45f), C("ffb16d"), 3.0f, .42f, glow);

            // Janitor — wall pegboard plus neat bins, deliberately against wall and away from workbench.
            WallPanel(p, "janitor pegboard", new Vector3(17.73f, .68f, 11.9f), new Vector3(.035f, .62f, 2.35f), wood, cream);
            for (int i = 0; i < 5; i++)
                Box("peg", p, new Vector3(17.67f, .65f, 11.0f + i * .42f), new Vector3(.14f, .035f, .035f), black, false);
            Box("janitor blue bin", p, new Vector3(13.55f, .27f, 13.75f), new Vector3(.70f, .54f, .70f), teal, true);
            Box("janitor red bin", p, new Vector3(14.45f, .27f, 13.75f), new Vector3(.70f, .54f, .70f), burgundy, true);
        }

        static void BuildGym(Transform p, Material cream, Material wood, Material teal, Material burgundy, Material black, Material glass, Material glow) {
            // Tall back-wall windows make the sporthall visually distinct from the school wing.
            Window(p, new Vector3(27.0f, .70f, 15.88f), 2.35f, .58f, glass, cream);
            Window(p, new Vector3(31.6f, .70f, 15.88f), 2.35f, .58f, glass, cream);
            Window(p, new Vector3(36.2f, .70f, 15.88f), 2.35f, .58f, glass, cream);
            Window(p, new Vector3(40.8f, .70f, 15.88f), 2.35f, .58f, glass, cream);

            // Handball-style goals at each end, readable from top-down without cluttering the court.
            Goal(p, new Vector3(34, .05f, 1.25f), 0, cream, black);
            Goal(p, new Vector3(34, .05f, 12.75f), 180, cream, black);

            // Wall bars on the right side. Thin, intentional structure rather than scattered props.
            for (int i = 0; i < 7; i++)
                Box("wall bar rung", p, new Vector3(44.30f, .35f + i * .10f, 8.0f), new Vector3(.05f, .045f, 3.25f), wood, false);
            Box("wall bar upright", p, new Vector3(44.30f, .67f, 6.45f), new Vector3(.08f, .82f, .08f), wood, false);
            Box("wall bar upright", p, new Vector3(44.30f, .67f, 9.55f), new Vector3(.08f, .82f, .08f), wood, false);

            // Scoreboard and padded wall accents.
            WallPanel(p, "scoreboard", new Vector3(44.31f, .82f, 3.0f), new Vector3(.035f, .72f, 2.2f), black, cream);
            Box("score light home", p, new Vector3(44.27f, .83f, 2.55f), new Vector3(.04f, .22f, .35f), burgundy, false);
            Box("score light away", p, new Vector3(44.27f, .83f, 3.45f), new Vector3(.04f, .22f, .35f), teal, false);

            PointLight("gym warm pool L", p, new Vector3(28.5f, 2.55f, 7), C("ffd098"), 6.0f, .34f, glow);
            PointLight("gym warm pool C", p, new Vector3(34.0f, 2.55f, 7), C("ffd098"), 6.0f, .34f, glow);
            PointLight("gym warm pool R", p, new Vector3(39.5f, 2.55f, 7), C("ffd098"), 6.0f, .34f, glow);
        }

        static void BuildExterior(Transform p, Material cream, Material wood, Material mustard, Material forest, Material glow, Material black) {
            // A proper little entrance composition: awning, posts, planter beds and bollard lamps.
            Box("entrance awning", p, new Vector3(0, 1.55f, -1.95f), new Vector3(4.5f, .16f, 1.0f), wood, false);
            Box("awning trim", p, new Vector3(0, 1.66f, -2.00f), new Vector3(4.70f, .08f, 1.10f), cream, false);
            Box("awning post L", p, new Vector3(-2.02f, .76f, -2.10f), new Vector3(.14f, 1.52f, .14f), wood, true);
            Box("awning post R", p, new Vector3(2.02f, .76f, -2.10f), new Vector3(.14f, 1.52f, .14f), wood, true);

            Planter(p, new Vector3(-3.55f, .14f, -3.0f), wood, forest);
            Planter(p, new Vector3(3.55f, .14f, -3.0f), wood, forest);
            Bollard(p, new Vector3(-2.75f, 0, -5.0f), black, glow);
            Bollard(p, new Vector3(2.75f, 0, -5.0f), black, glow);
            Bollard(p, new Vector3(-2.75f, 0, -8.0f), black, glow);
            Bollard(p, new Vector3(2.75f, 0, -8.0f), black, glow);

            // Warm wall lamps by the main door.
            WallLamp(p, new Vector3(-1.35f, 1.12f, -1.14f), black, glow);
            WallLamp(p, new Vector3(1.35f, 1.12f, -1.14f), black, glow);
        }

        static void BuildLighting(Transform p, Material glow) {
            // Soft room pools. No shadows: the directional sun still owns readable geometry shadows.
            RoomLight(p, "class A soft light", new Vector3(-13, 2.35f, 10.2f), C("ffd29a"), 5.0f, .33f, glow);
            RoomLight(p, "class B soft light", new Vector3(-4.25f, 2.35f, 10.2f), C("ffd29a"), 4.6f, .31f, glow);
            RoomLight(p, "staff warm light", new Vector3(3.45f, 2.35f, 10.7f), C("ffbd79"), 4.8f, .42f, glow);
            RoomLight(p, "library warm light", new Vector3(9.9f, 2.35f, 10.4f), C("ffc687"), 4.2f, .33f, glow);
            RoomLight(p, "janitor cool light", new Vector3(15.0f, 2.35f, 10.4f), C("e6e0c9"), 3.8f, .25f, glow);
        }

        static void TuneCameraAndWorld() {
            Camera cam = Camera.main;
            GameObject player = GameObject.Find("PLAYER");
            if (cam && player) {
                cam.orthographic = true;
                cam.orthographicSize = 7.25f;
                cam.backgroundColor = C("1b2321");
                Vector3 offset = new Vector3(0, 16.5f, -11.7f);
                cam.transform.position = player.transform.position + offset;
                cam.transform.LookAt(player.transform.position + Vector3.up * .28f);

                Component rig = cam.GetComponent("CozyCameraV57");
                if (rig) {
                    SerializedObject so = new SerializedObject(rig);
                    SerializedProperty po = so.FindProperty("offset"); if (po != null) po.vector3Value = offset;
                    SerializedProperty pmin = so.FindProperty("minSize"); if (pmin != null) pmin.floatValue = 6.2f;
                    SerializedProperty pmax = so.FindProperty("maxSize"); if (pmax != null) pmax.floatValue = 9.8f;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = C("5b665f");
            foreach (Light l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                if (l.type != LightType.Directional) continue;
                l.color = C("f3dfbd");
                l.intensity = .58f;
                l.shadowStrength = .28f;
                l.shadows = LightShadows.Soft;
            }
            QualitySettings.shadowDistance = 44f;
        }

        static void SkirtX(Transform p, float x1, float x2, float z, Material m) {
            Box("wood skirting", p, new Vector3((x1+x2)*.5f, .11f, z), new Vector3(x2-x1, .14f, .045f), m, false);
        }
        static void SkirtZ(Transform p, float x, float z1, float z2, Material m) {
            Box("wood skirting", p, new Vector3(x, .11f, (z1+z2)*.5f), new Vector3(.045f, .14f, z2-z1), m, false);
        }

        static void Window(Transform p, Vector3 c, float w, float h, Material glass, Material frame) {
            Box("window glass", p, c, new Vector3(w, h, .028f), glass, false);
            Box("window frame top", p, c + new Vector3(0, h*.5f+.035f, -.008f), new Vector3(w+.14f, .07f, .055f), frame, false);
            Box("window frame bottom", p, c + new Vector3(0, -h*.5f-.035f, -.008f), new Vector3(w+.14f, .07f, .055f), frame, false);
            Box("window frame L", p, c + new Vector3(-w*.5f-.035f, 0, -.008f), new Vector3(.07f, h+.14f, .055f), frame, false);
            Box("window frame R", p, c + new Vector3(w*.5f+.035f, 0, -.008f), new Vector3(.07f, h+.14f, .055f), frame, false);
            Box("window mullion", p, c + new Vector3(0,0,-.012f), new Vector3(.045f,h,.055f), frame, false);
        }

        static void WallPanel(Transform p, string name, Vector3 c, Vector3 size, Material panel, Material frame) {
            Box(name, p, c, size, panel, false);
            float horizontal = Mathf.Max(size.x, size.z);
            if (size.x > size.z) {
                Box(name+" frame top", p, c + new Vector3(0,size.y*.5f+.025f,-.008f), new Vector3(size.x+.12f,.05f,size.z+.025f), frame, false);
                Box(name+" frame bottom", p, c + new Vector3(0,-size.y*.5f-.025f,-.008f), new Vector3(size.x+.12f,.05f,size.z+.025f), frame, false);
                Box(name+" frame L", p, c + new Vector3(-size.x*.5f-.025f,0,-.008f), new Vector3(.05f,size.y+.10f,size.z+.025f), frame, false);
                Box(name+" frame R", p, c + new Vector3(size.x*.5f+.025f,0,-.008f), new Vector3(.05f,size.y+.10f,size.z+.025f), frame, false);
            } else {
                // panel is attached to a Z wall; its long horizontal axis is world Z.
                Box(name+" frame top", p, c + new Vector3(-.008f,size.y*.5f+.025f,0), new Vector3(size.x+.025f,.05f,size.z+.12f), frame, false);
                Box(name+" frame bottom", p, c + new Vector3(-.008f,-size.y*.5f-.025f,0), new Vector3(size.x+.025f,.05f,size.z+.12f), frame, false);
                Box(name+" frame A", p, c + new Vector3(-.008f,0,-size.z*.5f-.025f), new Vector3(size.x+.025f,size.y+.10f,.05f), frame, false);
                Box(name+" frame B", p, c + new Vector3(-.008f,0,size.z*.5f+.025f), new Vector3(size.x+.025f,size.y+.10f,.05f), frame, false);
            }
        }

        static void Radiator(Transform p, Vector3 c, float w, Material m) {
            Box("radiator body", p, c, new Vector3(w, .33f, .12f), m, false);
            int n = Mathf.Max(4, Mathf.RoundToInt(w/.22f));
            for (int i=1;i<n;i++) {
                float x = c.x - w*.5f + i*(w/n);
                Box("radiator rib", p, new Vector3(x,c.y,c.z-.07f), new Vector3(.025f,.28f,.025f), m, false);
            }
        }

        static void Threshold(Transform p, Vector3 c, float w, Material m) {
            Box("door threshold", p, c, new Vector3(w, .018f, .27f), m, false);
        }

        static void AddNote(Transform p, Vector3 c, Material m) {
            Box("notice paper", p, c, new Vector3(.30f,.26f,.012f), m, false);
        }

        static void Goal(Transform p, Vector3 pos, float yaw, Material frame, Material net) {
            Transform g = Group(p, "sporthall goal");
            g.position = pos; g.rotation = Quaternion.Euler(0,yaw,0);
            LocalBox("goal post L", g, new Vector3(-1.15f,.55f,0), new Vector3(.09f,1.10f,.09f), frame, false);
            LocalBox("goal post R", g, new Vector3(1.15f,.55f,0), new Vector3(.09f,1.10f,.09f), frame, false);
            LocalBox("goal crossbar", g, new Vector3(0,1.08f,0), new Vector3(2.38f,.09f,.09f), frame, false);
            for(int i=-4;i<=4;i++) LocalBox("goal net", g, new Vector3(i*.25f,.52f,.10f), new Vector3(.016f,.92f,.016f), net, false);
        }

        static void Planter(Transform p, Vector3 c, Material wood, Material green) {
            Box("timber planter", p, c + new Vector3(0,.12f,0), new Vector3(1.45f,.32f,.72f), wood, true);
            Box("planter soil", p, c + new Vector3(0,.30f,0), new Vector3(1.27f,.08f,.56f), green, false);
            Sphere("planter shrub", p, c + new Vector3(-.30f,.58f,0), new Vector3(.55f,.52f,.50f), green);
            Sphere("planter shrub", p, c + new Vector3(.28f,.55f,.02f), new Vector3(.62f,.46f,.52f), green);
        }

        static void Bollard(Transform p, Vector3 c, Material body, Material glow) {
            Box("path bollard", p, c + new Vector3(0,.38f,0), new Vector3(.13f,.76f,.13f), body, true);
            Sphere("bollard light", p, c + new Vector3(0,.78f,0), Vector3.one*.18f, glow);
            PointLight("bollard glow", p, c + new Vector3(0,.80f,0), C("ffa65b"), 2.6f, .34f, glow);
        }

        static void WallLamp(Transform p, Vector3 c, Material body, Material glow) {
            Box("wall lamp arm", p, c, new Vector3(.08f,.08f,.30f), body, false);
            Sphere("wall lamp shade", p, c + new Vector3(0,.02f,-.20f), new Vector3(.28f,.20f,.28f), glow);
            PointLight("wall lamp glow", p, c + new Vector3(0,.02f,-.28f), C("ff9e53"), 3.2f, .45f, glow);
        }

        static void RoomLight(Transform p, string name, Vector3 c, Color color, float range, float intensity, Material glow) {
            PointLight(name, p, c, color, range, intensity, glow);
            // A small cream pendant visible from the camera gives the light a physical source.
            GameObject shade = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shade.name = name + " shade"; shade.transform.SetParent(p,true); shade.transform.position = c - Vector3.up*.34f;
            shade.transform.localScale = new Vector3(.42f,.17f,.42f); shade.GetComponent<Renderer>().sharedMaterial = glow; NoCollider(shade);
        }

        static void PointLight(string name, Transform p, Vector3 pos, Color color, float range, float intensity, Material bulb) {
            GameObject g = new GameObject(name); g.transform.SetParent(p,true); g.transform.position = pos;
            Light l = g.AddComponent<Light>(); l.type = LightType.Point; l.color = color; l.range = range; l.intensity = intensity; l.shadows = LightShadows.None;
        }

        static GameObject PlaceAsset(string model, string name, Transform parent, Vector3 pos, float yaw, float scale, Material mat, bool removeColliders) {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FurnitureDir + "/" + model + ".obj");
            if (!prefab) return null;
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (!go) go = UnityEngine.Object.Instantiate(prefab);
            go.name = name; go.transform.SetParent(parent,true); go.transform.position = pos; go.transform.rotation = Quaternion.Euler(0,yaw,0); go.transform.localScale = Vector3.one*scale;
            if (mat) foreach(Renderer r in go.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = mat;
            Ground(go,pos.y);
            if (removeColliders) foreach(Collider c in go.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
            return go;
        }

        static GameObject PlaceAssetAt(string model, string name, Transform parent, Vector3 pos, float yaw, float scale, Material mat) {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FurnitureDir + "/" + model + ".obj");
            if (!prefab) return null;
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (!go) go = UnityEngine.Object.Instantiate(prefab);
            go.name = name; go.transform.SetParent(parent,true); go.transform.position = pos; go.transform.rotation = Quaternion.Euler(0,yaw,0); go.transform.localScale = Vector3.one*scale;
            if (mat) foreach(Renderer r in go.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = mat;
            foreach(Collider c in go.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
            return go;
        }

        static void Ground(GameObject go, float y) {
            Renderer[] rs = go.GetComponentsInChildren<Renderer>(true); if (rs.Length == 0) return;
            Bounds b = rs[0].bounds; for(int i=1;i<rs.Length;i++) b.Encapsulate(rs[i].bounds);
            go.transform.position += Vector3.up*(y-b.min.y);
        }

        static void Recolor58(string name, Color color, float smooth) {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(Generated58 + "/" + name + ".mat");
            if (!m) return;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor",color);
            if (m.HasProperty("_Color")) m.SetColor("_Color",color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness",smooth);
            EditorUtility.SetDirty(m);
        }

        static Shader PickShader() {
            bool srp = GraphicsSettings.currentRenderPipeline != null || GraphicsSettings.defaultRenderPipeline != null;
            Shader s = srp ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
            if (!s || !s.isSupported) s = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (!s || !s.isSupported) s = Shader.Find("Standard");
            return s;
        }

        static Material Mat(string name, Color color, Shader shader, float metal, float smooth) {
            string path = Generated64 + "/" + name + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(shader){name="V64 "+name}; AssetDatabase.CreateAsset(m,path); }
            if (m.shader != shader) m.shader = shader;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor",color);
            if (m.HasProperty("_Color")) m.SetColor("_Color",color);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic",metal);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness",smooth);
            EditorUtility.SetDirty(m); return m;
        }

        static Material Emissive(string name, Color color, Shader shader, float intensity) {
            Material m = Mat(name,color,shader,0,.22f);
            if (m.HasProperty("_EmissionColor")) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor",color*intensity); }
            return m;
        }

        static Color C(string hex) { ColorUtility.TryParseHtmlString("#"+hex,out Color c); return c; }
        static Transform Group(Transform p, string name) { GameObject g=new GameObject(name); g.transform.SetParent(p,false); return g.transform; }
        static GameObject Box(string name, Transform p, Vector3 pos, Vector3 size, Material mat, bool collider) { GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name; g.transform.SetParent(p,true); g.transform.position=pos; g.transform.localScale=size; g.GetComponent<Renderer>().sharedMaterial=mat; if(!collider) NoCollider(g); return g; }
        static GameObject LocalBox(string name, Transform p, Vector3 pos, Vector3 size, Material mat, bool collider) { GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name; g.transform.SetParent(p,false); g.transform.localPosition=pos; g.transform.localScale=size; g.GetComponent<Renderer>().sharedMaterial=mat; if(!collider) NoCollider(g); return g; }
        static void Sphere(string name, Transform p, Vector3 pos, Vector3 scale, Material mat) { GameObject g=GameObject.CreatePrimitive(PrimitiveType.Sphere); g.name=name; g.transform.SetParent(p,true); g.transform.position=pos; g.transform.localScale=scale; g.GetComponent<Renderer>().sharedMaterial=mat; NoCollider(g); }
        static void NoCollider(GameObject g) { Collider c=g.GetComponent<Collider>(); if(c) UnityEngine.Object.DestroyImmediate(c); }
    }
}
#endif
