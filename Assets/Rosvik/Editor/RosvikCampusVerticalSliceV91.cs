#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Rosvik.Blackout;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikCampusVerticalSliceV91 {
        const int Version = 91;
        const string Key = "ROSVIK_CAMPUS_VERTICAL_SLICE_V91";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string WorldName = "V88 CLEAN OSM WORLD";
        const string Generated = "Assets/Rosvik/GeneratedV91";

        static Shader shader;
        static Material snow, packedSnow, asphalt, ochre, ochreDark, brick, cream, blueSteel, darkBlue, roof, glass, warmGlass, metal, wood, warmFloor, greenFloor, sportFloor, ice, rinkBlue, rinkRed, white, black;

        static RosvikCampusVerticalSliceV91() {
            if (EditorPrefs.GetInt(Key, 0) >= Version) return;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V91 REAL CAMPUS + VERTICAL SLICE")]
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
            catch (Exception ex) { Debug.LogError("V91 CAMPUS BUILD FAILED: " + ex); }
        }

        static void Apply() {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject world = GameObject.Find(WorldName);
            if (!world) throw new Exception("V88 CLEAN OSM WORLD missing");

            Transform landmarks = FindChildContaining(world.transform, "AUTHORED LANDMARKS");
            Transform interiors = FindChildContaining(world.transform, "CLEAN INTERIORS");
            Transform details = FindChildContaining(world.transform, "WORLD DETAILS");
            if (!landmarks || !interiors || !details) throw new Exception("V88 visual groups missing");

            // One visual generation only. Gameplay scripts live elsewhere and are deliberately preserved.
            ClearChildren(landmarks);
            ClearChildren(interiors);
            ClearChildren(details);

            Directory.CreateDirectory(Generated);
            shader = PickKnownGoodShader(world);
            BuildMaterials();

            // Remove raw OSM building shells that occupy the authored campus footprint, but retain roads/map layout.
            DisableRawBuildingsInsideCampus(world.transform);

            GameObject campus = new GameObject("ROSVIK SCHOOL CAMPUS · V91");
            campus.transform.SetParent(landmarks, true);

            GameObject school = BuildSchool(campus.transform);
            GameObject corridor = BuildCorridor(campus.transform);
            GameObject sporthall = BuildSporthall(campus.transform);
            GameObject icehall = BuildIceHall(campus.transform);

            GameObject interiorRoot = new GameObject("V91 CAMPUS INTERIORS");
            interiorRoot.transform.SetParent(interiors, true);
            BuildSchoolInterior(interiorRoot.transform);
            BuildCorridorInterior(interiorRoot.transform);
            BuildSporthallInterior(interiorRoot.transform);
            BuildIceHallInterior(interiorRoot.transform);

            BuildCampusGround(details);
            BuildCampusDressing(details);
            BuildLighting(details);

            OsmWorldRuntimeV86 runtime = world.GetComponent<OsmWorldRuntimeV86>();
            if (!runtime) runtime = world.AddComponent<OsmWorldRuntimeV86>();

            GameObject schoolRoof = FindDeep(school.transform, "ROOF ROOT");
            GameObject corridorRoof = FindDeep(corridor.transform, "ROOF ROOT");
            GameObject sportRoof = FindDeep(sporthall.transform, "ROOF ROOT");
            GameObject iceRoof = FindDeep(icehall.transform, "ROOF ROOT");

            Collider schoolMainVolume = IndoorVolume(campus.transform, "SCHOOL MAIN INDOOR", new Vector3(-10f, 1.45f, -3f), new Vector3(27.0f, 2.9f, 13.0f));
            Collider schoolWingVolume = IndoorVolume(campus.transform, "SCHOOL WING INDOOR", new Vector3(-19f, 1.45f, 5.2f), new Vector3(9.0f, 2.9f, 8.0f));
            Collider corridorVolume = IndoorVolume(campus.transform, "LINK CORRIDOR INDOOR", new Vector3(3.2f, 1.4f, 4.8f), new Vector3(5.2f, 2.8f, 2.2f));
            Collider sportVolume = IndoorVolume(campus.transform, "SPORTHALL INDOOR", new Vector3(10f, 2.0f, 15f), new Vector3(15.4f, 4.0f, 19.4f));
            Collider iceVolume = IndoorVolume(campus.transform, "ICEHALL INDOOR", new Vector3(10f, 2.6f, 42f), new Vector3(27.0f, 5.2f, 27.0f));

            runtime.cutaways = new[] {
                new CutawayTargetV86 { visualRoot = schoolRoof, insideVolumes = new [] { schoolMainVolume, schoolWingVolume }, padding = 0f },
                new CutawayTargetV86 { visualRoot = corridorRoof, insideVolumes = new [] { corridorVolume }, padding = 0f },
                new CutawayTargetV86 { visualRoot = sportRoof, insideVolumes = new [] { sportVolume }, padding = 0f },
                new CutawayTargetV86 { visualRoot = iceRoof, insideVolumes = new [] { iceVolume }, padding = 0f }
            };
            runtime.attribution = "© OpenStreetMap contributors · ODbL 1.0";
            EditorUtility.SetDirty(runtime);

            TuneCameraAndLighting();

            CoziPlayerV57 player = UnityEngine.Object.FindFirstObjectByType<CoziPlayerV57>();
            if (player) {
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc) cc.enabled = false;
                player.transform.position = new Vector3(-10f, .02f, -14.5f);
                player.transform.rotation = Quaternion.identity;
                if (cc) cc.enabled = true;
                player.SetObjective("Utforska skolan, sporthallen och ishallen. Alla befintliga överlevnadssystem är kvar.");
                EditorUtility.SetDirty(player);
            }

            CleanupMissingScripts();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorPrefs.SetInt(Key, Version);
            SceneView.RepaintAll();
            Debug.Log("V91 COMPLETE — real campus relationship: school -> indoor link corridor -> sporthall, with separate ice hall north of the complex; OSM roads and all gameplay systems preserved.");
        }

        static GameObject BuildSchool(Transform parent) {
            GameObject root = new GameObject("ROSVIKS SKOLA · V91 HERO"); root.transform.SetParent(parent, true);
            Transform walls = Group(root.transform, "WALLS + FACADE");
            Transform roofs = Group(root.transform, "ROOF ROOT");
            Transform det = Group(root.transform, "DETAILS");

            // Main school block: low Swedish school profile, warm ochre, brick plinth, shallow dark roof.
            ShellRect(walls, new Vector3(-10f,0,-3f), 28f, 14f, 3.15f, ochre, brick, true, 3.0f);
            RoofRect(roofs, new Vector3(-10f,3.27f,-3f), 28.7f, 14.7f);

            // Smaller attached wing following the real campus character rather than one giant rectangle.
            ShellRect(walls, new Vector3(-19f,0,5.2f), 10f, 9f, 2.95f, ochreDark, brick, false, 0f);
            RoofRect(roofs, new Vector3(-19f,3.07f,5.2f), 10.7f, 9.7f);

            // Main entrance, south facade.
            Box("entrance surround", det, new Vector3(-10f,1.35f,-10.12f), new Vector3(5.8f,2.7f,.36f), brick, false);
            Box("entrance glass", det, new Vector3(-10f,1.18f,-10.35f), new Vector3(2.45f,2.15f,.10f), glass, false);
            Box("door mullion", det, new Vector3(-10f,1.18f,-10.41f), new Vector3(.08f,2.10f,.05f), metal, false);
            Box("entrance canopy", det, new Vector3(-10f,2.72f,-11.08f), new Vector3(6.8f,.20f,2.05f), roof, false);
            Box("entrance canopy snow", det, new Vector3(-10f,2.84f,-11.08f), new Vector3(6.55f,.06f,1.80f), snow, false);
            for (int s=-1;s<=1;s+=2) Box("canopy post", det, new Vector3(-10f+s*2.65f,1.32f,-11.45f), new Vector3(.12f,2.55f,.12f), metal, false);

            Sign(det, "ROSVIKS SKOLA", new Vector3(-10f,2.53f,-10.34f), new Vector3(4.55f,.48f,.09f), darkBlue, white, .067f);

            // Warm/cold window rhythm gives the blackout scene life without making every room glow.
            float[] xs = {-21.7f,-19.2f,-16.7f,-14.2f,-5.8f,-3.3f, -.8f,1.7f};
            for(int i=0;i<xs.Length;i++) Window(det, new Vector3(xs[i],1.70f,-10.03f), 0f, (i==2||i==5)?warmGlass:glass, 1.45f, .85f);
            for(int i=-2;i<=2;i++) Window(det, new Vector3(-24.05f,1.65f,-3f+i*2.2f), 90f, glass, 1.30f, .82f);

            // Facade details that read as a school rather than a generic OSM box.
            Box("notice board", det, new Vector3(-14.0f,1.45f,-10.18f), new Vector3(1.15f,.80f,.08f), wood, false);
            Box("bench seat", det, new Vector3(-4.9f,.43f,-11.0f), new Vector3(2.7f,.14f,.52f), wood, false);
            Box("bench back", det, new Vector3(-4.9f,.78f,-10.82f), new Vector3(2.7f,.58f,.12f), wood, false);
            for(int i=0;i<8;i++) Box("bike rack", det, new Vector3(-20.8f+i*.62f,.30f,-11.15f), new Vector3(.055f,.62f,.72f), metal, false, Quaternion.Euler(0,0,16));
            return root;
        }

        static GameObject BuildCorridor(Transform parent) {
            GameObject root = new GameObject("INDOOR LINK CORRIDOR · V91"); root.transform.SetParent(parent, true);
            Transform walls = Group(root.transform, "WALLS + GLASS");
            Transform roofs = Group(root.transform, "ROOF ROOT");
            // Short enclosed heated link between school and sporthall.
            Box("corridor floor shell", walls, new Vector3(3.2f,.10f,4.8f), new Vector3(5.5f,.20f,2.5f), cream, false);
            Box("corridor left low wall", walls, new Vector3(.45f,.70f,4.8f), new Vector3(.18f,1.40f,2.5f), brick, false);
            Box("corridor right low wall", walls, new Vector3(5.95f,.70f,4.8f), new Vector3(.18f,1.40f,2.5f), brick, false);
            Box("corridor north glass", walls, new Vector3(3.2f,1.45f,6.02f), new Vector3(5.3f,1.85f,.10f), warmGlass, false);
            Box("corridor south glass", walls, new Vector3(3.2f,1.45f,3.58f), new Vector3(5.3f,1.85f,.10f), glass, false);
            for(int i=-2;i<=2;i++) {
                Box("corridor mullion north", walls, new Vector3(3.2f+i*1.0f,1.45f,6.00f), new Vector3(.055f,1.90f,.06f), metal, false);
                Box("corridor mullion south", walls, new Vector3(3.2f+i*1.0f,1.45f,3.60f), new Vector3(.055f,1.90f,.06f), metal, false);
            }
            Box("corridor roof", roofs, new Vector3(3.2f,2.52f,4.8f), new Vector3(5.8f,.22f,2.85f), roof, false);
            Box("corridor roof snow", roofs, new Vector3(3.2f,2.66f,4.8f), new Vector3(5.55f,.06f,2.60f), snow, false);
            return root;
        }

        static GameObject BuildSporthall(Transform parent) {
            GameObject root = new GameObject("ROSVIK SPORTHALL · V91"); root.transform.SetParent(parent, true);
            Transform walls = Group(root.transform, "WALLS + FACADE");
            Transform roofs = Group(root.transform, "ROOF ROOT");
            Transform det = Group(root.transform, "DETAILS");

            ShellRect(walls, new Vector3(10f,0,15f), 16f, 20f, 4.25f, cream, brick, false, 0f);
            RoofRect(roofs, new Vector3(10f,4.38f,15f), 16.8f, 20.8f);
            Sign(det, "ROSVIK SPORTHALL", new Vector3(10f,3.42f,4.90f), new Vector3(5.0f,.52f,.10f), ochreDark, white, .061f);
            Box("sporthall entrance", det, new Vector3(10f,1.35f,4.82f), new Vector3(3.1f,2.65f,.12f), glass, false);
            Box("sporthall canopy", det, new Vector3(10f,3.05f,3.95f), new Vector3(5.0f,.20f,1.75f), roof, false);
            Box("sporthall canopy snow", det, new Vector3(10f,3.17f,3.95f), new Vector3(4.75f,.06f,1.50f), snow, false);
            for(int i=-2;i<=2;i++) Window(det, new Vector3(10f+i*2.4f,2.55f,4.88f), 0f, glass, 1.35f, .72f);
            return root;
        }

        static GameObject BuildIceHall(Transform parent) {
            GameObject root = new GameObject("NORRBOTTEN STÅL ARENA · ISHALL V91"); root.transform.SetParent(parent, true);
            Transform walls = Group(root.transform, "WALLS + STEEL FACADE");
            Transform roofs = Group(root.transform, "ROOF ROOT");
            Transform det = Group(root.transform, "DETAILS");

            ShellRect(walls, new Vector3(10f,0,42f), 28f, 28f, 5.65f, blueSteel, brick, true, 4.8f);
            RoofRect(roofs, new Vector3(10f,5.80f,42f), 28.9f, 28.9f);
            // Steel seam rhythm makes the large hall read as an ice arena from a distance.
            for(int i=-6;i<=6;i++) Box("steel seam", det, new Vector3(10f+i*2.0f,3.15f,27.92f), new Vector3(.035f,4.6f,.035f), darkBlue, false);
            Box("icehall entrance glass", det, new Vector3(10f,1.70f,27.78f), new Vector3(4.4f,3.25f,.12f), glass, false);
            Box("icehall canopy", det, new Vector3(10f,4.05f,26.80f), new Vector3(7.5f,.24f,2.2f), darkBlue, false);
            Box("icehall canopy snow", det, new Vector3(10f,4.20f,26.80f), new Vector3(7.2f,.06f,1.95f), snow, false);
            Sign(det, "NORRBOTTEN STÅL ARENA", new Vector3(10f,4.78f,27.80f), new Vector3(7.8f,.62f,.10f), cream, darkBlue, .057f);
            for(int i=-4;i<=4;i++) if(Mathf.Abs(i)>1) Window(det, new Vector3(10f+i*2.4f,3.55f,27.86f), 0f, glass, 1.30f, .74f);
            Box("service door", det, new Vector3(23.9f,1.45f,45f), new Vector3(.12f,2.9f,3.4f), metal, false);
            return root;
        }

        static void BuildSchoolInterior(Transform parent) {
            Transform root = Group(parent, "SCHOOL INTERIOR · V91");
            Box("school main floor", root, new Vector3(-10f,.045f,-3f), new Vector3(27.5f,.09f,13.5f), warmFloor, false);
            Box("school wing floor", root, new Vector3(-19f,.045f,5.2f), new Vector3(9.5f,.09f,8.5f), greenFloor, false);

            // Central corridor and low cutaway walls.
            Box("corridor runner", root, new Vector3(-10f,.095f,-1.0f), new Vector3(24f,.025f,2.6f), greenFloor, false);
            LowWall(root, new Vector3(-15.5f,.82f,-4.3f), new Vector3(.16f,1.55f,5.0f));
            LowWall(root, new Vector3(-7.0f,.82f,-4.3f), new Vector3(.16f,1.55f,5.0f));
            LowWall(root, new Vector3(-1.0f,.82f,-4.3f), new Vector3(.16f,1.55f,5.0f));
            LowWall(root, new Vector3(-15.5f,.82f,2.2f), new Vector3(.16f,1.55f,3.5f));

            // Two classrooms with deliberately aligned furniture.
            for(int row=0;row<3;row++) for(int col=0;col<2;col++) {
                float x=-21.2f+col*2.25f, z=-7.0f+row*2.25f;
                AssetOrDesk(root, x, z, 0f);
            }
            for(int row=0;row<3;row++) for(int col=0;col<2;col++) {
                float x=-12.8f+col*2.25f, z=-7.0f+row*2.25f;
                AssetOrDesk(root, x, z, 0f);
            }

            Box("teacher desk A", root, new Vector3(-20.0f,.38f,-2.2f), new Vector3(2.5f,.72f,1.0f), wood, false);
            Box("teacher desk B", root, new Vector3(-11.7f,.38f,-2.2f), new Vector3(2.5f,.72f,1.0f), wood, false);

            // Staff / library side with cozy furniture assets where available.
            Asset(root,"couch_pillows",new Vector3(-4.5f,.05f,-5.7f),180f,.78f);
            Asset(root,"armchair_pillows",new Vector3(-1.7f,.05f,-5.6f),180f,.78f);
            Asset(root,"table_low",new Vector3(-3.2f,.05f,-4.1f),0f,.80f);
            Asset(root,"shelf_B_large_decorated",new Vector3(-1.2f,.05f,1.5f),180f,.78f);
            Asset(root,"cabinet_medium_decorated",new Vector3(-5.5f,.05f,1.5f),180f,.78f);
            Asset(root,"rug_rectangle_stripes_A",new Vector3(-3.3f,.06f,-5.0f),0f,.90f);
        }

        static void BuildCorridorInterior(Transform parent) {
            Transform root = Group(parent,"LINK CORRIDOR INTERIOR · V91");
            Box("link warm floor",root,new Vector3(3.2f,.05f,4.8f),new Vector3(5.2f,.10f,2.2f),warmFloor,false);
            for(int i=-1;i<=1;i++) Box("link ceiling lamp",root,new Vector3(3.2f+i*1.55f,2.28f,4.8f),new Vector3(.52f,.06f,.30f),warmGlass,false);
        }

        static void BuildSporthallInterior(Transform parent) {
            Transform root = Group(parent,"SPORTHALL INTERIOR · V91");
            Box("sport floor",root,new Vector3(10f,.05f,15f),new Vector3(15.4f,.10f,19.4f),sportFloor,false);
            // Court markings.
            Box("center line",root,new Vector3(10f,.115f,15f),new Vector3(14.2f,.015f,.07f),white,false);
            Box("side line L",root,new Vector3(3.1f,.115f,15f),new Vector3(.07f,.015f,17.8f),white,false);
            Box("side line R",root,new Vector3(16.9f,.115f,15f),new Vector3(.07f,.015f,17.8f),white,false);
            // Long-side bleacher, matching the requested school/sporthall identity.
            for(int r=0;r<4;r++) Box("bleacher step",root,new Vector3(16.0f-r*.55f,.18f+r*.20f,15f),new Vector3(1.0f,.18f,10.5f),wood,false);
            Box("equipment bench",root,new Vector3(3.4f,.35f,10f),new Vector3(.65f,.65f,4.2f),blueSteel,false);
        }

        static void BuildIceHallInterior(Transform parent) {
            Transform root = Group(parent,"ICEHALL INTERIOR · V91");
            Box("rink ice",root,new Vector3(10f,.055f,42f),new Vector3(24.5f,.11f,21.5f),ice,false);
            // Boards.
            Box("board south",root,new Vector3(10f,.48f,31.1f),new Vector3(25.0f,.85f,.18f),white,false);
            Box("board north",root,new Vector3(10f,.48f,52.9f),new Vector3(25.0f,.85f,.18f),white,false);
            Box("board west",root,new Vector3(-2.4f,.48f,42f),new Vector3(.18f,.85f,21.6f),white,false);
            Box("board east",root,new Vector3(22.4f,.48f,42f),new Vector3(.18f,.85f,21.6f),white,false);
            // Hockey markings.
            Box("red center",root,new Vector3(10f,.122f,42f),new Vector3(24.0f,.018f,.09f),rinkRed,false);
            Box("blue line south",root,new Vector3(10f,.122f,37.0f),new Vector3(24.0f,.018f,.12f),rinkBlue,false);
            Box("blue line north",root,new Vector3(10f,.122f,47.0f),new Vector3(24.0f,.018f,.12f),rinkBlue,false);
            Ring(root,"center circle",new Vector3(10f,.125f,42f),3.2f,rinkBlue);
            Goal(root,new Vector3(10f,.25f,33.0f),0f);
            Goal(root,new Vector3(10f,.25f,51.0f),180f);
            for(int r=0;r<5;r++) Box("ice bleacher",root,new Vector3(21.2f-r*.55f,.20f+r*.20f,42f),new Vector3(1.0f,.18f,13.0f),wood,false);
        }

        static void BuildCampusGround(Transform parent) {
            Transform root = Group(parent,"CAMPUS GROUND · V91");
            Box("school forecourt",root,new Vector3(-9f,-.015f,-13.0f),new Vector3(36f,.03f,7.0f),packedSnow,false);
            Box("school-sporthall yard",root,new Vector3(0f,-.012f,11f),new Vector3(30f,.025f,18f),packedSnow,false);
            Box("icehall apron",root,new Vector3(10f,-.010f,25.5f),new Vector3(34f,.022f,7f),asphalt,false);
            Box("parking",root,new Vector3(-19f,-.008f,17f),new Vector3(14f,.018f,18f),asphalt,false);
            for(int i=-3;i<=3;i++) Box("parking stripe",root,new Vector3(-19f+i*1.9f,.008f,17f),new Vector3(.045f,.01f,7.2f),white,false);

            // Irregular, partially buried snow banks instead of flat white plates.
            SnowMound(root,new Vector3(-27f,.10f,-11.5f),new Vector3(5.2f,.55f,1.7f));
            SnowMound(root,new Vector3(-1f,.12f,-16.1f),new Vector3(7.0f,.65f,1.8f));
            SnowMound(root,new Vector3(-27f,.12f,20f),new Vector3(2.0f,.65f,6.4f));
            SnowMound(root,new Vector3(25f,.13f,25.5f),new Vector3(2.2f,.70f,6.8f));
            SnowMound(root,new Vector3(-6f,.11f,27f),new Vector3(6.5f,.58f,1.8f));
        }

        static void BuildCampusDressing(Transform parent) {
            Transform root = Group(parent,"CAMPUS DRESSING · V91");
            for(int i=0;i<5;i++) LampPost(root,new Vector3(-22f+i*8f,0,-15.8f));
            LampPost(root,new Vector3(-24f,0,11f));
            LampPost(root,new Vector3(-24f,0,22f));
            LampPost(root,new Vector3(26f,0,27f));
            LampPost(root,new Vector3(26f,0,40f));

            // Waste bins and benches at believable destinations, never random floor scatter.
            Box("school bin",root,new Vector3(-13.7f,.48f,-11.9f),new Vector3(.55f,.95f,.55f),darkBlue,false);
            Box("sporthall bin",root,new Vector3(13.2f,.48f,3.6f),new Vector3(.55f,.95f,.55f),darkBlue,false);
            Bench(root,new Vector3(-3.2f,0,-13.5f),0f);
            Bench(root,new Vector3(-22f,0,7f),90f);

            // A small fence edge keeps the campus readable against the broad OSM map.
            for(int i=0;i<9;i++) {
                Box("fence post",root,new Vector3(-30f+i*2.3f,.55f,27.5f),new Vector3(.09f,1.1f,.09f),wood,false);
                if(i<8) Box("fence rail",root,new Vector3(-28.85f+i*2.3f,.62f,27.5f),new Vector3(2.3f,.08f,.08f),wood,false);
            }
        }

        static void BuildLighting(Transform parent) {
            Transform root = Group(parent,"CAMPUS LIGHTING · V91");
            PointLight(root,"school entrance warmth",new Vector3(-10f,2.45f,-12.1f),new Color(1f,.68f,.38f),2.8f,8.5f);
            PointLight(root,"link warmth",new Vector3(3.2f,2.1f,4.8f),new Color(1f,.72f,.45f),2.1f,6.0f);
            PointLight(root,"sporthall door",new Vector3(10f,2.8f,3.5f),new Color(.95f,.78f,.55f),2.0f,7.0f);
            PointLight(root,"icehall cold entry",new Vector3(10f,3.8f,25.9f),new Color(.60f,.79f,1f),2.5f,9.0f);
        }

        static void DisableRawBuildingsInsideCampus(Transform world) {
            Transform osmBuildings = FindChildContaining(world,"OSM BUILDINGS");
            if (!osmBuildings) return;
            foreach(Transform child in osmBuildings) {
                Bounds b = BoundsOf(child.gameObject);
                if (b.size == Vector3.zero) continue;
                Vector3 c=b.center;
                if(c.x>-35f && c.x<32f && c.z>-20f && c.z<60f) child.gameObject.SetActive(false);
            }
        }

        static Collider IndoorVolume(Transform parent, string name, Vector3 pos, Vector3 size) {
            GameObject g = new GameObject(name); g.transform.SetParent(parent,true); g.transform.position=pos;
            BoxCollider b=g.AddComponent<BoxCollider>(); b.isTrigger=true; b.size=size; b.center=Vector3.zero;
            return b;
        }

        static void ShellRect(Transform parent, Vector3 center, float w, float d, float h, Material wall, Material baseMat, bool frontGap, float opening) {
            float t=.24f;
            Box("back wall",parent,new Vector3(center.x,h*.5f,center.z+d*.5f),new Vector3(w,h,t),wall,true);
            Box("left wall",parent,new Vector3(center.x-w*.5f,h*.5f,center.z),new Vector3(t,h,d),wall,true);
            Box("right wall",parent,new Vector3(center.x+w*.5f,h*.5f,center.z),new Vector3(t,h,d),wall,true);
            if(frontGap) {
                float side=(w-opening)*.5f;
                Box("front wall L",parent,new Vector3(center.x-(opening*.5f+side*.5f),h*.5f,center.z-d*.5f),new Vector3(side,h,t),wall,true);
                Box("front wall R",parent,new Vector3(center.x+(opening*.5f+side*.5f),h*.5f,center.z-d*.5f),new Vector3(side,h,t),wall,true);
            } else Box("front wall",parent,new Vector3(center.x,h*.5f,center.z-d*.5f),new Vector3(w,h,t),wall,true);

            Box("base front",parent,new Vector3(center.x,.34f,center.z-d*.5f-.03f),new Vector3(w+.12f,.66f,.27f),baseMat,false);
            Box("base back",parent,new Vector3(center.x,.34f,center.z+d*.5f+.03f),new Vector3(w+.12f,.66f,.27f),baseMat,false);
            Box("base left",parent,new Vector3(center.x-w*.5f-.03f,.34f,center.z),new Vector3(.27f,.66f,d),baseMat,false);
            Box("base right",parent,new Vector3(center.x+w*.5f+.03f,.34f,center.z),new Vector3(.27f,.66f,d),baseMat,false);
        }

        static void RoofRect(Transform parent, Vector3 center, float w, float d) {
            Box("dark roof",parent,center,new Vector3(w,.24f,d),roof,false);
            Box("snow cap",parent,center+Vector3.up*.145f,new Vector3(w-.30f,.07f,d-.30f),snow,false);
            // subtle parapet / roof edge avoids the giant floating-slab look.
            Box("roof south trim",parent,new Vector3(center.x,center.y-.02f,center.z-d*.5f),new Vector3(w,.28f,.10f),darkBlue,false);
            Box("roof north trim",parent,new Vector3(center.x,center.y-.02f,center.z+d*.5f),new Vector3(w,.28f,.10f),darkBlue,false);
        }

        static void Window(Transform parent, Vector3 p, float yaw, Material pane, float w, float h) {
            GameObject root=new GameObject("window");root.transform.SetParent(parent,true);root.transform.position=p;root.transform.rotation=Quaternion.Euler(0,yaw,0);
            Box("frame",root.transform,new Vector3(0,0,0),new Vector3(w+.18f,h+.18f,.13f),cream,false);
            Box("window glass",root.transform,new Vector3(0,0,-.075f),new Vector3(w,h,.045f),pane,false);
            Box("mullion V",root.transform,new Vector3(0,0,-.105f),new Vector3(.055f,h,.035f),cream,false);
        }

        static void Sign(Transform parent, string text, Vector3 pos, Vector3 size, Material boardMat, Material textMat, float charSize) {
            GameObject root=new GameObject("SIGN · "+text);root.transform.SetParent(parent,true);root.transform.position=pos;
            Box("sign board",root.transform,Vector3.zero,size,boardMat,false);
            GameObject tg=new GameObject("sign text");tg.transform.SetParent(root.transform,false);tg.transform.localPosition=new Vector3(0,0,-size.z*.60f);
            tg.transform.localRotation=Quaternion.identity;
            TextMesh tm=tg.AddComponent<TextMesh>();tm.text=text;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;tm.fontSize=72;tm.characterSize=charSize;tm.fontStyle=FontStyle.Bold;
            tm.color = textMat ? textMat.color : Color.white;
        }

        static void LowWall(Transform parent, Vector3 p, Vector3 size) { Box("interior wall",parent,p,size,cream,true); }

        static void AssetOrDesk(Transform parent,float x,float z,float yaw) {
            GameObject table=Asset(parent,"table_small",new Vector3(x,.05f,z),yaw,.78f);
            if(!table) Box("student desk",parent,new Vector3(x,.42f,z),new Vector3(1.15f,.78f,.72f),wood,false);
            Asset(parent,"chair_A",new Vector3(x,.05f,z+.75f),180f+yaw,.72f);
        }

        static GameObject Asset(Transform parent,string search,Vector3 pos,float yaw,float scale) {
            string[] ids=AssetDatabase.FindAssets(search);
            foreach(string id in ids) {
                string path=AssetDatabase.GUIDToAssetPath(id);
                GameObject prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(!prefab) continue;
                GameObject g=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
                if(!g) g=UnityEngine.Object.Instantiate(prefab);
                g.name=search;g.transform.SetParent(parent,true);g.transform.position=pos;g.transform.rotation=Quaternion.Euler(0,yaw,0);g.transform.localScale*=scale;
                return g;
            }
            return null;
        }

        static void Ring(Transform parent,string name,Vector3 center,float radius,Material mat) {
            int n=28;float thickness=.055f;
            for(int i=0;i<n;i++) {
                float a0=i*Mathf.PI*2f/n,a1=(i+1)*Mathf.PI*2f/n,am=(a0+a1)*.5f;
                Vector3 p=center+new Vector3(Mathf.Cos(am)*radius,0,Mathf.Sin(am)*radius);
                float len=2f*radius*Mathf.Sin(Mathf.PI/n);
                Box(name,parent,p,new Vector3(len,.018f,thickness),mat,false,Quaternion.Euler(0,-am*Mathf.Rad2Deg,0));
            }
        }

        static void Goal(Transform parent,Vector3 p,float yaw) {
            GameObject r=new GameObject("hockey goal");r.transform.SetParent(parent,true);r.transform.position=p;r.transform.rotation=Quaternion.Euler(0,yaw,0);
            Box("goal bar L",r.transform,new Vector3(-1.05f,.55f,0),new Vector3(.07f,1.1f,.07f),rinkRed,false);
            Box("goal bar R",r.transform,new Vector3(1.05f,.55f,0),new Vector3(.07f,1.1f,.07f),rinkRed,false);
            Box("goal crossbar",r.transform,new Vector3(0,1.08f,0),new Vector3(2.15f,.07f,.07f),rinkRed,false);
            Box("goal back",r.transform,new Vector3(0,.55f,.60f),new Vector3(2.15f,1.05f,.04f),white,false);
        }

        static void SnowMound(Transform parent,Vector3 pos,Vector3 scale) {
            GameObject g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name="soft snowbank";g.transform.SetParent(parent,true);g.transform.position=pos;g.transform.localScale=scale;
            Renderer r=g.GetComponent<Renderer>();if(r)r.sharedMaterial=snow;Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);
        }

        static void LampPost(Transform parent,Vector3 p) {
            Box("lamp post",parent,p+Vector3.up*1.55f,new Vector3(.10f,3.10f,.10f),metal,false);
            Box("lamp head",parent,p+new Vector3(.28f,3.05f,0),new Vector3(.60f,.14f,.24f),darkBlue,false);
            PointLight(parent,"lamp glow",p+new Vector3(.30f,2.92f,0),new Color(1f,.75f,.48f),.75f,5.5f);
        }

        static void Bench(Transform parent,Vector3 p,float yaw) {
            GameObject r=new GameObject("bench");r.transform.SetParent(parent,true);r.transform.position=p;r.transform.rotation=Quaternion.Euler(0,yaw,0);
            Box("seat",r.transform,new Vector3(0,.45f,0),new Vector3(2.3f,.14f,.55f),wood,false);
            Box("back",r.transform,new Vector3(0,.82f,.20f),new Vector3(2.3f,.60f,.12f),wood,false);
            Box("leg L",r.transform,new Vector3(-.78f,.23f,0),new Vector3(.10f,.45f,.10f),metal,false);
            Box("leg R",r.transform,new Vector3(.78f,.23f,0),new Vector3(.10f,.45f,.10f),metal,false);
        }

        static void PointLight(Transform parent,string name,Vector3 p,Color color,float intensity,float range) {
            GameObject g=new GameObject(name);g.transform.SetParent(parent,true);g.transform.position=p;Light l=g.AddComponent<Light>();l.type=LightType.Point;l.color=color;l.intensity=intensity;l.range=range;l.shadows=LightShadows.Soft;
        }

        static GameObject Box(string name,Transform parent,Vector3 pos,Vector3 scale,Material mat,bool collider,Quaternion? rot=null) {
            GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,true);g.transform.position=pos;g.transform.rotation=rot??Quaternion.identity;g.transform.localScale=scale;
            Renderer r=g.GetComponent<Renderer>();if(r)r.sharedMaterial=mat;
            Collider c=g.GetComponent<Collider>();if(!collider && c)UnityEngine.Object.DestroyImmediate(c);
            return g;
        }

        static Transform Group(Transform parent,string name) { GameObject g=new GameObject(name);g.transform.SetParent(parent,true);return g.transform; }

        static void BuildMaterials() {
            snow=Mat("snow",new Color(.72f,.79f,.82f),0f,.28f);
            packedSnow=Mat("packed snow",new Color(.58f,.66f,.69f),0f,.20f);
            asphalt=Mat("winter asphalt",new Color(.19f,.24f,.26f),0f,.24f);
            ochre=Mat("school ochre",new Color(.50f,.34f,.18f),0f,.30f);
            ochreDark=Mat("school deep ochre",new Color(.38f,.25f,.14f),0f,.28f);
            brick=Mat("brick",new Color(.31f,.16f,.12f),0f,.35f);
            cream=Mat("cream trim",new Color(.72f,.70f,.61f),0f,.30f);
            blueSteel=Mat("icehall blue steel",new Color(.16f,.27f,.31f),.12f,.42f);
            darkBlue=Mat("dark blue",new Color(.08f,.14f,.18f),.08f,.38f);
            roof=Mat("dark roof",new Color(.10f,.14f,.16f),.10f,.44f);
            glass=Mat("cold glass",new Color(.16f,.27f,.32f),.18f,.70f);
            warmGlass=Mat("warm window",new Color(.84f,.58f,.29f),.05f,.55f,true);
            metal=Mat("metal",new Color(.18f,.21f,.22f),.55f,.46f);
            wood=Mat("wood",new Color(.35f,.22f,.14f),0f,.34f);
            warmFloor=Mat("warm floor",new Color(.37f,.27f,.20f),0f,.27f);
            greenFloor=Mat("green floor",new Color(.22f,.31f,.29f),0f,.24f);
            sportFloor=Mat("sport floor",new Color(.48f,.34f,.22f),0f,.32f);
            ice=Mat("rink ice",new Color(.64f,.79f,.85f),.05f,.68f);
            rinkBlue=Mat("rink blue",new Color(.10f,.34f,.62f),0f,.30f);
            rinkRed=Mat("rink red",new Color(.62f,.12f,.10f),0f,.30f);
            white=Mat("white",new Color(.90f,.90f,.84f),0f,.28f);
            black=Mat("black",new Color(.04f,.05f,.055f),0f,.30f);
        }

        static Material Mat(string name,Color color,float metallic,float smooth,bool emission=false) {
            string path=Generated+"/"+name.Replace(' ','_')+".mat";
            Material m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m) { m=new Material(shader);m.name=name;AssetDatabase.CreateAsset(m,path); }
            else if(m.shader!=shader) m.shader=shader;
            if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color); if(m.HasProperty("_Color"))m.SetColor("_Color",color);
            if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",metallic); if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth); if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",smooth);
            if(emission) {
                if(m.HasProperty("_EmissionColor")) { m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*1.3f); }
            }
            EditorUtility.SetDirty(m);return m;
        }

        static Shader PickKnownGoodShader(GameObject root) {
            foreach(Renderer r in root.GetComponentsInChildren<Renderer>(true)) if(r && r.sharedMaterial && r.sharedMaterial.shader && r.sharedMaterial.shader.isSupported) return r.sharedMaterial.shader;
            return Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
        }

        static void TuneCameraAndLighting() {
            Camera cam=Camera.main;
            if(cam) { cam.orthographic=true;cam.orthographicSize=10.8f;cam.backgroundColor=new Color(.10f,.16f,.18f);EditorUtility.SetDirty(cam); }
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight=new Color(.25f,.31f,.33f);
            RenderSettings.fog=true;RenderSettings.fogColor=new Color(.30f,.38f,.42f);RenderSettings.fogDensity=.0065f;
            foreach(Light l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None)) if(l && l.type==LightType.Directional) { l.color=new Color(.73f,.81f,.86f);l.intensity=.48f; }
        }

        static Bounds BoundsOf(GameObject g) {
            Renderer[] rs=g.GetComponentsInChildren<Renderer>(true);if(rs.Length==0)return new Bounds(g.transform.position,Vector3.zero);
            Bounds b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;
        }

        static Transform FindChildContaining(Transform root,string token) {
            foreach(Transform t in root.GetComponentsInChildren<Transform>(true)) if(t.name.IndexOf(token,StringComparison.OrdinalIgnoreCase)>=0) return t;
            return null;
        }

        static GameObject FindDeep(Transform root,string exact) {
            foreach(Transform t in root.GetComponentsInChildren<Transform>(true)) if(t.name==exact)return t.gameObject;return null;
        }

        static void ClearChildren(Transform t) { for(int i=t.childCount-1;i>=0;i--)UnityEngine.Object.DestroyImmediate(t.GetChild(i).gameObject); }

        static void CleanupMissingScripts() {
            foreach(GameObject g in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include,FindObjectsSortMode.None)) if(g && g.scene.IsValid())GameObjectUtility.RemoveMonoBehavioursWithMissingScript(g);
        }
    }
}
#endif
