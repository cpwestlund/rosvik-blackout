#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Rosvik.Blackout;
using UScene = UnityEngine.SceneManagement.Scene;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikTopDownOverhaulV55 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_TOPDOWN_V55_VERSION";
        const int Version = 55;
        const string GroupName = "36 TOPDOWN V55 - FULL SCHOOL OVERHAUL";
        const string GeneratedDir = "Assets/Rosvik/GeneratedV55";
        const long SchoolWay = 163199458;
        const long OldSchoolWay = 163199461;
        const long ArenaWay = 163199454;

        class DoorBuild {
            public GameObject spot;
            public GameObject marker;
            public Transform leaf;
            public Transform leaf2;
            public Vector3 openEuler;
            public Vector3 openEuler2;
        }

        class CabinetBuild {
            public GameObject spot;
            public GameObject marker;
            public Transform door;
            public Vector3 openEuler;
        }

        static RosvikTopDownOverhaulV55() {
            EditorApplication.delayCall -= Auto;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V55 TOPDOWN - FULL GRAPHIC OVERHAUL")]
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
                if (!File.Exists(ScenePath)) return;
                UScene scene = EditorSceneManager.GetActiveScene();
                if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

                List<RosvikOsmV15.Way> ways = RosvikOsmV15.LoadWays();
                if (ways == null || ways.Count == 0) throw new Exception("OSM data missing");
                RosvikOsmV15.Way school = ways.FirstOrDefault(w => w.Id == SchoolWay);
                if (school == null) throw new Exception("Rosviks skola footprint missing");
                RosvikOsmV15.Way hall = FindSportHall(ways, school);

                Transform player = FindSceneTransform(scene,"PLAYER");
                if (!player) throw new Exception("PLAYER missing");

                RemoveTopLevel(scene,GroupName);
                DisableOldPasses(scene);
                HideMappedBuilding(scene,school);
                if (hall != null) HideMappedBuilding(scene,hall);

                Directory.CreateDirectory(GeneratedDir);
                AssetDatabase.Refresh();
                Shader shader = FindGoodShader(scene);
                if (!shader) throw new Exception("No supported shader found");

                Material grass = Mat("grass",new Color(.17f,.23f,.12f),shader);
                Material grass2 = Mat("grass_dark",new Color(.11f,.18f,.09f),shader);
                Material asphalt = Mat("asphalt",new Color(.075f,.085f,.088f),shader);
                Material path = Mat("path",new Color(.38f,.34f,.28f),shader);
                Material curb = Mat("curb",new Color(.66f,.65f,.59f),shader);
                Material snow = Mat("snow",new Color(.82f,.86f,.84f),shader);
                Material schoolWall = Mat("school_wall",new Color(.56f,.48f,.36f),shader);
                Material schoolRoof = Mat("school_roof",new Color(.16f,.115f,.095f),shader);
                Material hallWall = Mat("hall_wall",new Color(.115f,.135f,.135f),shader);
                Material hallRoof = Mat("hall_roof",new Color(.075f,.085f,.09f),shader);
                Material trim = Mat("trim",new Color(.80f,.77f,.66f),shader);
                Material glass = Mat("glass",new Color(.07f,.16f,.18f),shader);
                Material wood = Mat("wood",new Color(.31f,.17f,.075f),shader);
                Material metal = Mat("metal",new Color(.065f,.075f,.075f),shader);
                Material white = Mat("line_white",new Color(.86f,.84f,.75f),shader);
                Material amber = Mat("amber",new Color(.92f,.52f,.11f),shader);
                Material blue = Mat("blue",new Color(.12f,.36f,.52f),shader);
                Material red = Mat("red",new Color(.58f,.15f,.12f),shader);
                Material interiorFloor = Mat("interior_floor",new Color(.27f,.27f,.225f),shader);
                Material classFloor = Mat("class_floor",new Color(.34f,.31f,.23f),shader);
                Material staffFloor = Mat("staff_floor",new Color(.25f,.30f,.27f),shader);
                Material gymFloor = Mat("gym_floor",new Color(.43f,.29f,.16f),shader);
                Material interiorWall = Mat("interior_wall",new Color(.67f,.64f,.54f),shader);
                Material interiorTrim = Mat("interior_trim",new Color(.18f,.20f,.18f),shader);
                Material doorMat = Mat("door",new Color(.36f,.19f,.075f),shader);
                Material cabinetMat = Mat("cabinet",new Color(.15f,.24f,.27f),shader);
                Material shelfMat = Mat("shelf",new Color(.30f,.20f,.12f),shader);
                Material markerMat = Mat("marker",new Color(.94f,.57f,.12f),shader);
                Material green = Mat("green",new Color(.07f,.22f,.10f),shader);

                GameObject root = new GameObject(GroupName);
                Transform exterior = Group(root.transform,"V55 EXTERIOR - ROSVIK SCHOOL CAMPUS");
                Transform interior = Group(root.transform,"V55 INTERIOR - SCHOOL + SPORTHALL");

                Vector3 schoolCenter = RosvikOsmV15.Centroid(school); schoolCenter.y=0f;
                Vector3 hallCenter = hall != null ? RosvikOsmV15.Centroid(hall) : schoolCenter + new Vector3(28f,0f,0f);
                hallCenter.y=0f;
                Vector3 entrance = ResolveEntrance(scene,school,hall);
                Vector3 f = Flat(entrance-schoolCenter).normalized;
                if (f.sqrMagnitude < .1f) f=Vector3.back;
                Vector3 r = new Vector3(f.z,0f,-f.x).normalized;

                BuildExteriorCampus(exterior,school,hall,schoolCenter,hallCenter,entrance,f,r,
                    grass,grass2,asphalt,path,curb,snow,schoolWall,schoolRoof,hallWall,hallRoof,trim,glass,wood,metal,white,green);

                Vector3 exteriorStart = entrance + f*8.2f; exteriorStart.y=.16f;
                Vector3 exteriorReturn = entrance + f*3.0f; exteriorReturn.y=.16f;
                player.position = exteriorStart;

                DoorBuild exteriorDoor = BuildDoubleEntrance(exterior,entrance+f*.42f,r,f,doorMat,glass,trim,markerMat);
                GameObject backpack = BuildLootProp(exterior,"V55 SEARCH BACKPACK",entrance-r*5.3f+f*6.2f,amber,markerMat);
                CabinetBuild fuseCab = BuildCabinet(exterior,"V55 FUSE CABINET",entrance+r*4.7f+f*2.0f,r,-f,new Vector3(1.0f,1.25f,.42f),blue,markerMat);
                CabinetBuild aidCab = BuildCabinet(exterior,"V55 FIRST AID CABINET",entrance+r*5.3f+f*5.8f,r,-f,new Vector3(.90f,1.15f,.40f),red,markerMat);

                Vector3 interiorOrigin = schoolCenter + new Vector3(900f,0f,900f);
                List<TopDownGameplayV55.Spot> runtimeSpots = new List<TopDownGameplayV55.Spot>();
                Vector3 interiorSpawn;
                BuildInterior(interior,interiorOrigin,exteriorReturn,runtimeSpots,
                    interiorFloor,classFloor,staffFloor,gymFloor,interiorWall,interiorTrim,doorMat,cabinetMat,shelfMat,wood,metal,white,markerMat,
                    out interiorSpawn);

                runtimeSpots.Insert(0,new TopDownGameplayV55.Spot {
                    spot=exteriorDoor.spot, marker=exteriorDoor.marker, displayName="skolentrén", kind=TopDownGameplayV55.SpotKind.SchoolEntrance,
                    interactRadius=3.4f, movingPart=exteriorDoor.leaf, movingPart2=exteriorDoor.leaf2,
                    closedEuler=Vector3.zero,openEuler=exteriorDoor.openEuler,closedEuler2=Vector3.zero,openEuler2=exteriorDoor.openEuler2,
                    teleportTarget=interiorSpawn
                });
                runtimeSpots.Insert(0,new TopDownGameplayV55.Spot {
                    spot=aidCab.spot, marker=aidCab.marker, displayName="första hjälpen-skåpet", itemName="Förband", kind=TopDownGameplayV55.SpotKind.Cabinet,
                    interactRadius=2.4f,movingPart=aidCab.door,closedEuler=Vector3.zero,openEuler=aidCab.openEuler
                });
                runtimeSpots.Insert(0,new TopDownGameplayV55.Spot {
                    spot=fuseCab.spot, marker=fuseCab.marker, displayName="elskåpet", itemName="Säkring", kind=TopDownGameplayV55.SpotKind.Cabinet,
                    interactRadius=2.4f,movingPart=fuseCab.door,closedEuler=Vector3.zero,openEuler=fuseCab.openEuler
                });
                runtimeSpots.Insert(0,new TopDownGameplayV55.Spot {
                    spot=backpack, marker=FindMarker(backpack), displayName="ryggsäcken", itemName="Ficklampa", kind=TopDownGameplayV55.SpotKind.Loot,
                    interactRadius=2.3f
                });

                DisableGameplayOnPlayer(player);
                TopDownGameplayV55 gameplay = player.GetComponent<TopDownGameplayV55>();
                if (!gameplay) gameplay = player.gameObject.AddComponent<TopDownGameplayV55>();
                gameplay.enabled=true;
                gameplay.spots.Clear();
                gameplay.spots.AddRange(runtimeSpots);
                gameplay.defaultInteractDistance=2.4f;
                gameplay.animationTime=.20f;
                EditorUtility.SetDirty(gameplay);

                SetupCamera(scene,player);
                SetupLighting(scene);

                EditorPrefs.SetInt(Key,Version);
                Selection.activeGameObject=player.gameObject;
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene,ScenePath);
                AssetDatabase.SaveAssets();
                SceneView.RepaintAll();
                Debug.Log("ROSVIK V55 SUCCESS: full top-down school campus overhaul built with exact OSM footprints, asset-led exterior, visible animated doors/cabinets/shelves and school-to-sporthall gameplay.");
            }
            catch(Exception ex) {
                Debug.LogError("ROSVIK V55 FAILED: "+ex);
            }
        }

        static void BuildExteriorCampus(Transform p,RosvikOsmV15.Way school,RosvikOsmV15.Way hall,Vector3 schoolCenter,Vector3 hallCenter,
            Vector3 entrance,Vector3 f,Vector3 r,Material grass,Material grass2,Material asphalt,Material path,Material curb,Material snow,
            Material schoolWall,Material schoolRoof,Material hallWall,Material hallRoof,Material trim,Material glass,Material wood,Material metal,Material white,Material green) {

            Vector3 campus = schoolCenter + f*5f;
            FlatBox("campus lawn",p,campus,r,62f,48f,.035f,grass);

            BuildFootprintBuilding(p,school,"ROSVIKS SKOLA V55",schoolWall,schoolRoof,trim,glass,1.55f);
            if (hall != null) BuildFootprintBuilding(p,hall,"ROSVIK SPORTHALL V55",hallWall,hallRoof,trim,glass,2.05f);

            Vector3 plaza=entrance+f*4.0f;
            FlatBox("entrance plaza",p,plaza,r,15f,7f,.052f,path);
            Border(p,plaza,r,f,15f,7f,curb);
            FlatBox("main approach",p,entrance+f*10.5f,f,3.4f,14f,.050f,path);

            Vector3 parking=entrance+f*13.2f+r*10.0f;
            FlatBox("school parking",p,parking,r,21f,12f,.052f,asphalt);
            Border(p,parking,r,f,21f,12f,curb);
            for(int i=-3;i<=3;i++) FlatBox("parking stripe",p,parking+r*(i*2.65f),f,.10f,5.2f,.015f,white);

            GameObject car=FindModel("car_stationwagon","car_sedan","car_hatchback");
            if(car) {
                PlaceModel(car,"parked car",p,parking-r*5.2f+f*.4f,f,1.45f);
                PlaceModel(car,"parked car",p,parking+r*.1f-f*.2f,-f,1.45f);
                PlaceModel(car,"abandoned car",p,parking+r*5.4f+f*.8f,f,1.45f);
            }

            GameObject bench=FindModel("bench");
            if(bench) {
                PlaceModel(bench,"school bench",p,entrance-r*5.1f+f*3.6f,r,.90f);
                PlaceModel(bench,"school bench",p,entrance+r*5.1f+f*3.6f,-r,.90f);
            } else {
                BenchFallback(p,entrance-r*5.1f+f*3.6f,r,wood,metal);
                BenchFallback(p,entrance+r*5.1f+f*3.6f,-r,wood,metal);
            }

            GameObject lamp=FindModel("streetlight");
            if(lamp) {
                PlaceModel(lamp,"entrance lamp",p,entrance-r*6.5f+f*5.0f,f,4.2f);
                PlaceModel(lamp,"entrance lamp",p,entrance+r*6.5f+f*5.0f,f,4.2f);
            }

            // Schoolyard sits on the side away from the parking and stays deliberately open.
            Vector3 yard=schoolCenter-r*18f+f*3f;
            FlatBox("schoolyard hardcourt",p,yard,r,21f,15f,.045f,path);
            Border(p,yard,r,f,21f,15f,curb);
            FlatBox("court center line",p,yard,r,18f,.10f,.012f,white);
            FlatBox("court side line",p,yard+f*5.5f,r,18f,.10f,.012f,white);
            FlatBox("court side line",p,yard-f*5.5f,r,18f,.10f,.012f,white);

            if(bench) {
                PlaceModel(bench,"yard bench",p,yard-r*8.0f-f*6.0f,r,.88f);
                PlaceModel(bench,"yard bench",p,yard+r*8.0f-f*6.0f,-r,.88f);
            }

            // Simple playground geometry with readable top-down silhouettes.
            SwingSet(p,yard-r*6.0f+f*3.4f,r,metal,wood);
            Picnic(p,yard+r*5.0f+f*3.6f,r,wood,metal);

            GameObject tree=FindModel("tree_pineDefaultA","tree_pineTallA","tree_default_fall");
            GameObject bush=FindModel("plant_bushDetailed","bush");
            Vector3[] treePos={
                campus-r*28f-f*18f,campus-r*27f-f*7f,campus-r*28f+f*10f,campus-r*24f+f*19f,
                campus+r*27f-f*17f,campus+r*28f-f*5f,campus+r*27f+f*9f,campus+r*24f+f*18f
            };
            foreach(Vector3 tp in treePos) {
                if(tree) PlaceModel(tree,"campus pine",p,tp,f,4.3f);
                else TreeFallback(p,tp,metal,green);
            }
            if(bush) {
                for(int i=-2;i<=2;i++) {
                    PlaceModel(bush,"entrance shrub",p,entrance+r*(i*1.8f)-f*.7f,f,.75f);
                }
            }

            GameObject dumpster=FindModel("dumpster");
            Vector3 service=(hall!=null?hallCenter:schoolCenter+r*26f)-f*9f;
            FlatBox("service pad",p,service,r,9f,6f,.048f,asphalt);
            if(dumpster) PlaceModel(dumpster,"service dumpster",p,service-r*2.3f,r,1.35f);

            SnowPatch(p,entrance-r*8.0f+f*2.0f,r,4f,.8f,snow);
            SnowPatch(p,parking+r*9.2f-f*5.1f,r,3.8f,.75f,snow);
            SnowPatch(p,yard-r*9.2f+f*6.7f,r,3.2f,.65f,snow);
        }

        static void BuildInterior(Transform p,Vector3 o,Vector3 exteriorReturn,List<TopDownGameplayV55.Spot> spots,
            Material floor,Material classFloor,Material staffFloor,Material gymFloor,Material wall,Material trim,Material doorMat,Material cabinetMat,
            Material shelfMat,Material wood,Material metal,Material white,Material markerMat,out Vector3 spawn) {

            Box("interior void",p,o+new Vector3(7f,-.16f,3f),new Vector3(58f,.30f,34f),Quaternion.identity,trim,true);

            // School wing: vestibule + long corridor + classrooms/staff/storage.
            Flat("entrance vestibule",p,o+new Vector3(-14f,.01f,0f),new Vector3(5f,.06f,5f),floor);
            Flat("main corridor",p,o+new Vector3(-2f,.01f,0f),new Vector3(24f,.06f,3.4f),floor);
            Flat("classroom A",p,o+new Vector3(-10f,.01f,6f),new Vector3(8f,.06f,8f),classFloor);
            Flat("classroom B",p,o+new Vector3(-1f,.01f,6f),new Vector3(8f,.06f,8f),classFloor);
            Flat("staff room",p,o+new Vector3(8f,.01f,6f),new Vector3(8f,.06f,8f),staffFloor);
            Flat("library storage",p,o+new Vector3(-8f,.01f,-5.5f),new Vector3(9f,.06f,6.5f),staffFloor);
            Flat("janitor room",p,o+new Vector3(2f,.01f,-5.5f),new Vector3(8f,.06f,6.5f),staffFloor);
            Flat("connector",p,o+new Vector3(15f,.01f,0f),new Vector3(10f,.06f,3.4f),floor);

            // Sports hall with small stand on the long side closest to the connector/player approach.
            Flat("sporthall court",p,o+new Vector3(27f,.01f,1f),new Vector3(20f,.06f,14f),gymFloor);
            CourtLines(p,o+new Vector3(27f,.05f,1f),white);
            Bleachers(p,o+new Vector3(27f,0f,-5.1f),wood,metal);

            // Outer school walls.
            Wall(p,o+new Vector3(-14f,0,-2.5f),new Vector3(5f,1.5f,.22f),wall);
            Wall(p,o+new Vector3(-14f,0,2.5f),new Vector3(5f,1.5f,.22f),wall);
            Wall(p,o+new Vector3(-16.5f,0,0f),new Vector3(.22f,1.5f,5f),wall);
            Wall(p,o+new Vector3(10f,0,-1.7f),new Vector3(48f,1.5f,.22f),wall);
            Wall(p,o+new Vector3(10f,0,10f),new Vector3(48f,1.5f,.22f),wall);
            Wall(p,o+new Vector3(-14f,0,6f),new Vector3(.22f,1.5f,8f),wall);
            Wall(p,o+new Vector3(12f,0,6f),new Vector3(.22f,1.5f,8f),wall);

            // Room separators. Door gaps are left deliberately.
            Wall(p,o+new Vector3(-6f,0,6f),new Vector3(.22f,1.5f,8f),wall);
            Wall(p,o+new Vector3(3f,0,6f),new Vector3(.22f,1.5f,8f),wall);
            Wall(p,o+new Vector3(12f,0,6f),new Vector3(.22f,1.5f,8f),wall);
            Wall(p,o+new Vector3(-3.5f,0,-5.5f),new Vector3(.22f,1.5f,6.5f),wall);
            Wall(p,o+new Vector3(6f,0,-5.5f),new Vector3(.22f,1.5f,6.5f),wall);
            Wall(p,o+new Vector3(-8f,0,-8.75f),new Vector3(9f,1.5f,.22f),wall);
            Wall(p,o+new Vector3(2f,0,-8.75f),new Vector3(8f,1.5f,.22f),wall);

            // North/south corridor walls broken around doors.
            SegmentedHorizontalWall(p,o,new float[]{-14f,-11.1f,-9.7f,-2.2f,-.8f,6.6f,8.0f,12f},1.7f,wall);
            SegmentedHorizontalWall(p,o,new float[]{-14f,-10.2f,-8.8f,-.7f,.7f,4.2f,5.6f,12f},-1.7f,wall);

            // Sporthall shell.
            Wall(p,o+new Vector3(27f,0,8f),new Vector3(20f,1.8f,.24f),wall);
            Wall(p,o+new Vector3(27f,0,-6f),new Vector3(20f,1.8f,.24f),wall);
            Wall(p,o+new Vector3(37f,0,1f),new Vector3(.24f,1.8f,14f),wall);
            Wall(p,o+new Vector3(17f,0,5.2f),new Vector3(.24f,1.8f,5.6f),wall);
            Wall(p,o+new Vector3(17f,0,-3.2f),new Vector3(.24f,1.8f,5.6f),wall);

            // Exterior/vestibule door inside is a visible door too.
            DoorBuild exitDoor=BuildSingleDoor(p,"V55 INTERIOR EXIT",o+new Vector3(-16.38f,0f,0f),Vector3.forward,doorMat,markerMat,false);
            spots.Add(new TopDownGameplayV55.Spot {spot=exitDoor.spot,marker=exitDoor.marker,displayName="ytterdörren",kind=TopDownGameplayV55.SpotKind.InteriorExit,interactRadius=2.7f,movingPart=exitDoor.leaf,closedEuler=Vector3.zero,openEuler=exitDoor.openEuler,teleportTarget=exteriorReturn});

            DoorBuild classA=BuildSingleDoor(p,"V55 DOOR CLASS A",o+new Vector3(-10.4f,0f,1.7f),Vector3.right,doorMat,markerMat,true);
            spots.Add(DoorSpot(classA,"klassrum A",TopDownGameplayV55.SpotKind.Door,""));

            DoorBuild classB=BuildSingleDoor(p,"V55 DOOR CLASS B",o+new Vector3(-1.5f,0f,1.7f),Vector3.right,doorMat,markerMat,true);
            spots.Add(DoorSpot(classB,"klassrum B",TopDownGameplayV55.SpotKind.Door,""));

            DoorBuild staff=BuildSingleDoor(p,"V55 DOOR STAFF",o+new Vector3(7.4f,0f,1.7f),Vector3.right,doorMat,markerMat,true);
            spots.Add(DoorSpot(staff,"personalrummet",TopDownGameplayV55.SpotKind.LockedDoor,"Nyckelknippa"));

            DoorBuild library=BuildSingleDoor(p,"V55 DOOR LIBRARY",o+new Vector3(-9.5f,0f,-1.7f),Vector3.right,doorMat,markerMat,false);
            spots.Add(DoorSpot(library,"biblioteksförrådet",TopDownGameplayV55.SpotKind.Door,""));

            DoorBuild janitor=BuildSingleDoor(p,"V55 DOOR JANITOR",o+new Vector3(0f,0f,-1.7f),Vector3.right,doorMat,markerMat,false);
            spots.Add(DoorSpot(janitor,"vaktmästarrummet",TopDownGameplayV55.SpotKind.Door,""));

            DoorBuild gym=BuildSingleDoor(p,"V55 DOOR SPORTHALL",o+new Vector3(17f,0f,1f),Vector3.forward,doorMat,markerMat,true);
            TopDownGameplayV55.Spot gymSpot=DoorSpot(gym,"dörren mot sporthallen",TopDownGameplayV55.SpotKind.SportsHallDoor,"Huvudnyckel");
            gymSpot.interactRadius=2.8f;spots.Add(gymSpot);

            // Classroom furniture — spaced, readable and no scatter.
            ClassroomFurniture(p,o+new Vector3(-10f,0,6f),wood,metal,cabinetMat);
            ClassroomFurniture(p,o+new Vector3(-1f,0,6f),wood,metal,cabinetMat);
            StaffFurniture(p,o+new Vector3(8f,0,6f),wood,metal,cabinetMat,shelfMat);
            LibraryFurniture(p,o+new Vector3(-8f,0,-5.5f),shelfMat,metal);
            JanitorFurniture(p,o+new Vector3(2f,0,-5.5f),cabinetMat,metal);

            // Openable shelves/cabinets. These are the interaction objects, not invisible triggers.
            CabinetBuild teacherCab=BuildCabinet(p,"V55 TEACHER CABINET",o+new Vector3(-12.2f,0,8.3f),Vector3.right,Vector3.back,new Vector3(1.4f,1.45f,.48f),cabinetMat,markerMat);
            spots.Add(CabinetSpot(teacherCab,"lärarskåpet","Nyckelknippa",TopDownGameplayV55.SpotKind.Cabinet,""));

            CabinetBuild classShelf=BuildCabinet(p,"V55 CLASSROOM SHELF",o+new Vector3(-2.7f,0,8.3f),Vector3.right,Vector3.back,new Vector3(1.5f,1.35f,.48f),shelfMat,markerMat);
            spots.Add(CabinetSpot(classShelf,"klassrumshyllan","Batterier",TopDownGameplayV55.SpotKind.Cabinet,""));

            CabinetBuild master=BuildCabinet(p,"V55 MASTER KEY CABINET",o+new Vector3(10.1f,0,8.4f),Vector3.right,Vector3.back,new Vector3(1.2f,1.45f,.45f),cabinetMat,markerMat);
            spots.Add(CabinetSpot(master,"nyckelskåpet","Huvudnyckel",TopDownGameplayV55.SpotKind.LockedCabinet,"Nyckelknippa"));

            CabinetBuild janitorShelf=BuildCabinet(p,"V55 JANITOR SHELF",o+new Vector3(4.2f,0,-7.4f),Vector3.right,Vector3.forward,new Vector3(1.5f,1.35f,.50f),shelfMat,markerMat);
            spots.Add(CabinetSpot(janitorShelf,"vaktmästarhyllan","Verktyg",TopDownGameplayV55.SpotKind.Cabinet,""));

            CabinetBuild gymStore=BuildCabinet(p,"V55 GYM STORAGE",o+new Vector3(34.8f,0,6.0f),Vector3.right,Vector3.back,new Vector3(1.8f,1.45f,.55f),cabinetMat,markerMat);
            spots.Add(CabinetSpot(gymStore,"materialskåpet","Sporttejp",TopDownGameplayV55.SpotKind.Cabinet,""));

            spawn=o+new Vector3(-14.4f,.16f,0f);
        }

        static TopDownGameplayV55.Spot DoorSpot(DoorBuild d,string name,TopDownGameplayV55.SpotKind kind,string required) {
            return new TopDownGameplayV55.Spot {spot=d.spot,marker=d.marker,displayName=name,kind=kind,requiredItem=required,interactRadius=2.45f,movingPart=d.leaf,closedEuler=Vector3.zero,openEuler=d.openEuler};
        }

        static TopDownGameplayV55.Spot CabinetSpot(CabinetBuild c,string name,string item,TopDownGameplayV55.SpotKind kind,string required) {
            return new TopDownGameplayV55.Spot {spot=c.spot,marker=c.marker,displayName=name,itemName=item,kind=kind,requiredItem=required,interactRadius=2.3f,movingPart=c.door,closedEuler=Vector3.zero,openEuler=c.openEuler};
        }

        static DoorBuild BuildDoubleEntrance(Transform p,Vector3 center,Vector3 axis,Vector3 outward,Material doorMat,Material glass,Material trim,Material markerMat) {
            GameObject holder=new GameObject("V55 SCHOOL ENTRANCE DOUBLE DOORS");holder.transform.SetParent(p,true);holder.transform.position=center;
            Box("entrance frame",holder.transform,center+Vector3.up*.9f,new Vector3(4.4f,1.9f,.26f),Rot(axis),trim,false);
            Box("entrance glass",holder.transform,center+outward*.05f+Vector3.up*.9f,new Vector3(3.85f,1.55f,.10f),Rot(axis),glass,false);

            Transform left=DoorPivot(holder.transform,"left door",center-axis*1.65f,axis,1.65f,doorMat);
            Transform right=DoorPivot(holder.transform,"right door",center+axis*1.65f,-axis,1.65f,doorMat);
            GameObject spot=SpotObject(holder.transform,"entrance interaction",center+outward*1.45f);
            GameObject marker=Marker(spot.transform,markerMat,1.15f);
            return new DoorBuild{spot=spot,marker=marker,leaf=left,leaf2=right,openEuler=new Vector3(0,-92f,0),openEuler2=new Vector3(0,92f,0)};
        }

        static DoorBuild BuildSingleDoor(Transform p,string name,Vector3 center,Vector3 axis,Material doorMat,Material markerMat,bool openPositive) {
            axis=Flat(axis).normalized;
            Vector3 hinge=center-axis*.65f;
            Transform pivot=DoorPivot(p,name+" leaf",hinge,axis,1.30f,doorMat);
            GameObject spot=SpotObject(p,name+" interaction",center);
            GameObject marker=Marker(spot.transform,markerMat,.72f);
            return new DoorBuild{spot=spot,marker=marker,leaf=pivot,openEuler=new Vector3(0,openPositive?92f:-92f,0)};
        }

        static Transform DoorPivot(Transform parent,string name,Vector3 hinge,Vector3 axis,float width,Material mat) {
            GameObject pivot=new GameObject(name+" pivot");pivot.transform.SetParent(parent,true);pivot.transform.position=hinge;pivot.transform.rotation=Rot(axis);
            Box(name,pivot.transform,pivot.transform.TransformPoint(new Vector3(width*.5f,.72f,0f)),new Vector3(width,1.44f,.12f),pivot.transform.rotation,mat,true);
            // Box was created in world space; reparent local geometry cleanly.
            Transform leaf=pivot.transform.GetChild(pivot.transform.childCount-1);
            leaf.SetParent(pivot.transform,true);
            return pivot.transform;
        }

        static CabinetBuild BuildCabinet(Transform p,string name,Vector3 pos,Vector3 axis,Vector3 front,Vector3 size,Material mat,Material markerMat) {
            axis=Flat(axis).normalized;front=Flat(front).normalized;
            if(axis.sqrMagnitude<.1f)axis=Vector3.right;if(front.sqrMagnitude<.1f)front=Vector3.forward;
            GameObject holder=new GameObject(name);holder.transform.SetParent(p,true);holder.transform.position=pos;holder.transform.rotation=Quaternion.LookRotation(front,Vector3.up);

            float w=size.x,h=size.y,d=size.z;
            LocalBox("cabinet back",holder.transform,new Vector3(0,h*.5f,-d*.45f),new Vector3(w,h,.08f),mat,true);
            LocalBox("cabinet left",holder.transform,new Vector3(-w*.48f,h*.5f,0),new Vector3(.08f,h,d),mat,true);
            LocalBox("cabinet right",holder.transform,new Vector3(w*.48f,h*.5f,0),new Vector3(.08f,h,d),mat,true);
            LocalBox("cabinet top",holder.transform,new Vector3(0,h-.04f,0),new Vector3(w,.08f,d),mat,true);
            for(int i=1;i<=2;i++) LocalBox("shelf",holder.transform,new Vector3(0,h*i/3f,-.02f),new Vector3(w*.90f,.06f,d*.82f),mat,false);

            GameObject pivot=new GameObject("cabinet door pivot");pivot.transform.SetParent(holder.transform,false);pivot.transform.localPosition=new Vector3(-w*.47f,0,d*.48f);
            LocalBox("cabinet door",pivot.transform,new Vector3(w*.47f,h*.5f,0),new Vector3(w*.94f,h,.07f),mat,true);

            GameObject spot=SpotObject(holder.transform,"cabinet interaction",holder.transform.TransformPoint(new Vector3(0,0,d*.85f)));
            GameObject marker=Marker(spot.transform,markerMat,.75f);
            return new CabinetBuild{spot=spot,marker=marker,door=pivot.transform,openEuler=new Vector3(0,-108f,0)};
        }

        static GameObject BuildLootProp(Transform p,string name,Vector3 pos,Material body,Material markerMat) {
            GameObject holder=new GameObject(name);holder.transform.SetParent(p,true);holder.transform.position=pos;
            Box("backpack body",holder.transform,pos+Vector3.up*.25f,new Vector3(.72f,.46f,.55f),Quaternion.Euler(0,20f,0),body,true);
            Box("backpack flap",holder.transform,pos+new Vector3(0,.45f,.08f),new Vector3(.60f,.10f,.42f),Quaternion.Euler(0,20f,0),body,false);
            Marker(holder.transform,markerMat,.72f);
            return holder;
        }

        static GameObject FindMarker(GameObject root) {
            if(!root)return null;Transform t=root.GetComponentsInChildren<Transform>(true).FirstOrDefault(x=>x.name=="interaction marker");return t?t.gameObject:null;
        }

        static GameObject SpotObject(Transform p,string name,Vector3 pos) {
            GameObject g=new GameObject(name);g.transform.SetParent(p,true);g.transform.position=new Vector3(pos.x,.06f,pos.z);return g;
        }

        static GameObject Marker(Transform p,Material mat,float size) {
            GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name="interaction marker";g.transform.SetParent(p,false);g.transform.localPosition=new Vector3(0,.02f,0);g.transform.localScale=new Vector3(size,.015f,size);g.GetComponent<Renderer>().sharedMaterial=mat;Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);return g;
        }

        static void BuildFootprintBuilding(Transform p,RosvikOsmV15.Way w,string name,Material wallMat,Material roofMat,Material trim,Material glass,float height) {
            List<Vector3> pts=Points(w);if(pts.Count<3)return;
            Transform g=Group(p,name);
            Polygon(name+" roof",g,pts,height,roofMat);
            float signed=SignedArea(pts);
            for(int i=0;i<pts.Count;i++) {
                Vector3 a=pts[i],b=pts[(i+1)%pts.Count];Vector3 edge=Flat(b-a);float len=edge.magnitude;if(len<.2f)continue;edge/=len;
                Vector3 left=new Vector3(-edge.z,0,edge.x);Vector3 outv=signed>0?-left:left;Vector3 mid=(a+b)*.5f;
                Box("wall",g,mid+Vector3.up*(height*.5f),new Vector3(len,height,.18f),Rot(edge),wallMat,true);
                Box("roof trim",g,mid+outv*.04f+Vector3.up*(height+.04f),new Vector3(len+.08f,.10f,.22f),Rot(edge),trim,false);
                int count=Mathf.Clamp(Mathf.FloorToInt(len/4.0f),0,8);
                for(int k=0;k<count;k++) {
                    Vector3 wp=Vector3.Lerp(a,b,(k+.5f)/Mathf.Max(1,count));
                    Box("window",g,wp+outv*.12f+Vector3.up*.78f,new Vector3(1.35f,.58f,.05f),Rot(edge),glass,false);
                }
            }
        }

        static void ClassroomFurniture(Transform p,Vector3 c,Material wood,Material metal,Material cabinet) {
            Vector3[] seats={new Vector3(-2.2f,0,-1.5f),new Vector3(0,0,-1.5f),new Vector3(2.2f,0,-1.5f),new Vector3(-2.2f,0,1.0f),new Vector3(0,0,1.0f),new Vector3(2.2f,0,1.0f)};
            foreach(Vector3 q in seats) Desk(p,c+q,wood,metal);
            Box("teacher desk",p,c+new Vector3(0,.38f,3.0f),new Vector3(2.1f,.76f,.75f),Quaternion.identity,wood,true);
            Box("whiteboard",p,c+new Vector3(0,.85f,3.82f),new Vector3(3.6f,.92f,.08f),Quaternion.identity,cabinet,false);
        }

        static void StaffFurniture(Transform p,Vector3 c,Material wood,Material metal,Material cabinet,Material shelf) {
            Box("staff table",p,c+Vector3.up*.42f,new Vector3(3.2f,.16f,1.25f),Quaternion.identity,wood,true);
            for(int i=-1;i<=1;i++) Chair(p,c+new Vector3(i*1.05f,0,-1.2f),metal,wood);
            Box("staff counter",p,c+new Vector3(2.8f,.45f,2.6f),new Vector3(1.5f,.9f,.55f),Quaternion.identity,cabinet,true);
            ShelfStatic(p,c+new Vector3(-2.8f,0,2.8f),1.6f,shelf,metal);
        }

        static void LibraryFurniture(Transform p,Vector3 c,Material shelf,Material metal) {
            ShelfStatic(p,c+new Vector3(-2.8f,0,1.6f),2.3f,shelf,metal);
            ShelfStatic(p,c+new Vector3(0f,0,1.6f),2.3f,shelf,metal);
            ShelfStatic(p,c+new Vector3(2.8f,0,1.6f),2.3f,shelf,metal);
            ShelfStatic(p,c+new Vector3(-1.5f,0,-1.6f),2.0f,shelf,metal);
            ShelfStatic(p,c+new Vector3(1.5f,0,-1.6f),2.0f,shelf,metal);
        }

        static void JanitorFurniture(Transform p,Vector3 c,Material cabinet,Material metal) {
            Box("workbench",p,c+new Vector3(-2.2f,.45f,1.8f),new Vector3(2.8f,.9f,.75f),Quaternion.identity,cabinet,true);
            Box("tool cart",p,c+new Vector3(2.6f,.42f,-1.5f),new Vector3(1.2f,.84f,.72f),Quaternion.identity,metal,true);
        }

        static void ShelfStatic(Transform p,Vector3 pos,float width,Material shelf,Material metal) {
            Box("shelf back",p,pos+Vector3.up*.75f,new Vector3(width,1.5f,.08f),Quaternion.identity,metal,true);
            for(int i=0;i<4;i++) Box("shelf board",p,pos+new Vector3(0,.18f+i*.40f,.28f),new Vector3(width,.06f,.55f),Quaternion.identity,shelf,true);
        }

        static void Desk(Transform p,Vector3 pos,Material wood,Material metal) {
            Box("desk top",p,pos+Vector3.up*.38f,new Vector3(1.45f,.12f,.70f),Quaternion.identity,wood,true);
            Box("desk base",p,pos+new Vector3(-.55f,.18f,0),new Vector3(.08f,.36f,.55f),Quaternion.identity,metal,true);
            Box("desk base",p,pos+new Vector3(.55f,.18f,0),new Vector3(.08f,.36f,.55f),Quaternion.identity,metal,true);
            Chair(p,pos+new Vector3(0,0,-.85f),metal,wood);
        }

        static void Chair(Transform p,Vector3 pos,Material metal,Material wood) {
            Box("chair seat",p,pos+Vector3.up*.28f,new Vector3(.55f,.10f,.55f),Quaternion.identity,wood,true);
            Box("chair back",p,pos+new Vector3(0,.58f,.25f),new Vector3(.55f,.62f,.09f),Quaternion.identity,wood,true);
        }

        static void CourtLines(Transform p,Vector3 c,Material white) {
            FlatBox("court line",p,c+new Vector3(0,.02f,-5.7f),Vector3.right,18f,.10f,.015f,white);
            FlatBox("court line",p,c+new Vector3(0,.02f,5.7f),Vector3.right,18f,.10f,.015f,white);
            FlatBox("court line",p,c+new Vector3(-8.7f,.02f,0),Vector3.forward,11.5f,.10f,.015f,white);
            FlatBox("court line",p,c+new Vector3(8.7f,.02f,0),Vector3.forward,11.5f,.10f,.015f,white);
            FlatBox("center line",p,c+Vector3.up*.02f,Vector3.forward,11.5f,.10f,.015f,white);
        }

        static void Bleachers(Transform p,Vector3 c,Material wood,Material metal) {
            for(int row=0;row<3;row++) {
                Box("bleacher row",p,c+new Vector3(0,.20f+row*.24f,row*.48f),new Vector3(10.5f,.14f,.72f),Quaternion.identity,wood,true);
            }
            for(int i=-2;i<=2;i++) Box("bleacher support",p,c+new Vector3(i*2.2f,.24f,.45f),new Vector3(.10f,.48f,.70f),Quaternion.identity,metal,true);
        }

        static void SwingSet(Transform p,Vector3 pos,Vector3 axis,Material metal,Material wood) {
            axis=Flat(axis).normalized;Vector3 side=Perp(axis);
            Box("swing post",p,pos-axis*1.4f+Vector3.up*1.0f,new Vector3(.12f,2f,.12f),Quaternion.identity,metal,true);
            Box("swing post",p,pos+axis*1.4f+Vector3.up*1.0f,new Vector3(.12f,2f,.12f),Quaternion.identity,metal,true);
            Box("swing beam",p,pos+Vector3.up*2.0f,new Vector3(3.0f,.12f,.12f),Rot(axis),metal,true);
            Box("swing seat",p,pos+Vector3.up*.55f,new Vector3(.65f,.10f,.32f),Rot(axis),wood,true);
        }

        static void Picnic(Transform p,Vector3 pos,Vector3 axis,Material wood,Material metal) {
            Box("picnic top",p,pos+Vector3.up*.55f,new Vector3(2.4f,.14f,.85f),Rot(axis),wood,true);
            Box("picnic bench",p,pos+Perp(axis)*.85f+Vector3.up*.33f,new Vector3(2.4f,.12f,.35f),Rot(axis),wood,true);
            Box("picnic bench",p,pos-Perp(axis)*.85f+Vector3.up*.33f,new Vector3(2.4f,.12f,.35f),Rot(axis),wood,true);
        }

        static void BenchFallback(Transform p,Vector3 pos,Vector3 axis,Material wood,Material metal) {
            Box("bench",p,pos+Vector3.up*.34f,new Vector3(2.0f,.14f,.52f),Rot(axis),wood,true);
            Box("bench back",p,pos-Perp(axis)*.22f+Vector3.up*.70f,new Vector3(2.0f,.62f,.10f),Rot(axis),wood,true);
        }

        static void TreeFallback(Transform p,Vector3 pos,Material trunk,Material canopy) {
            Box("tree trunk",p,pos+Vector3.up*.65f,new Vector3(.24f,1.3f,.24f),Quaternion.identity,trunk,true);
            Sphere("tree crown",p,pos+Vector3.up*1.75f,new Vector3(2.0f,1.1f,2.0f),canopy);
        }

        static GameObject FindModel(params string[] names) {
            foreach(string n in names) {
                string[] guids=AssetDatabase.FindAssets(n+" t:GameObject");
                foreach(string guid in guids) {
                    string path=AssetDatabase.GUIDToAssetPath(guid);
                    if(string.IsNullOrEmpty(path))continue;
                    GameObject go=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if(go)return go;
                }
            }
            return null;
        }

        static GameObject PlaceModel(GameObject prefab,string name,Transform parent,Vector3 pos,Vector3 forward,float targetHeight) {
            if(!prefab)return null;
            GameObject go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if(!go)go=UnityEngine.Object.Instantiate(prefab);
            go.name=name;go.transform.SetParent(parent,true);go.transform.position=pos;go.transform.rotation=Quaternion.LookRotation(Flat(forward).sqrMagnitude>.01f?Flat(forward).normalized:Vector3.forward,Vector3.up);
            Bounds b=RenderBounds(go);float h=b.size.y>.01f?b.size.y:Mathf.Max(b.size.x,b.size.z);if(h>.01f){float scale=targetHeight/h;go.transform.localScale*=scale;b=RenderBounds(go);}go.transform.position+=Vector3.up*(pos.y-b.min.y);
            return go;
        }

        static Bounds RenderBounds(GameObject go) {
            Renderer[] rs=go.GetComponentsInChildren<Renderer>(true);if(rs.Length==0)return new Bounds(go.transform.position,Vector3.one);Bounds b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;
        }

        static RosvikOsmV15.Way FindSportHall(List<RosvikOsmV15.Way> ways,RosvikOsmV15.Way school) {
            Vector3 sc=RosvikOsmV15.Centroid(school);List<Vector3> sp=Points(school);
            return ways.Where(x=>x.Closed&&x.Id!=SchoolWay&&x.Id!=OldSchoolWay&&x.Id!=ArenaWay&&!string.IsNullOrEmpty(x.Tag("building"))&&x.Tag("building")!="no")
                .Select(x=>new {Way=x,B=RosvikOsmV15.Bounds(x),D=Flat(RosvikOsmV15.Centroid(x)-sc).magnitude,G=BoundaryGap(sp,Points(x))})
                .Where(x=>x.D<80f&&x.G<30f&&x.B.Width*x.B.Depth>150f&&Mathf.Max(x.B.Width,x.B.Depth)>13f)
                .OrderBy(x=>x.G).ThenBy(x=>x.D).Select(x=>x.Way).FirstOrDefault();
        }

        static Vector3 ResolveEntrance(UScene scene,RosvikOsmV15.Way school,RosvikOsmV15.Way hall) {
            Transform old=Resources.FindObjectsOfTypeAll<Transform>().FirstOrDefault(t=>t&&t.gameObject.scene==scene&&(t.name=="entrance interaction"||t.name=="large timber canopy"));
            if(old){Vector3 p=old.position;p.y=0f;return p;}
            Vector3 sc=RosvikOsmV15.Centroid(school);Vector3 away=hall!=null?Flat(sc-RosvikOsmV15.Centroid(hall)).normalized:Vector3.back;List<Vector3> pts=Points(school);return pts.OrderByDescending(x=>Vector3.Dot(Flat(x-sc),away)).First();
        }

        static float BoundaryGap(List<Vector3>a,List<Vector3>b){float best=float.MaxValue;foreach(Vector3 x in a)foreach(Vector3 y in b){float d=Flat(x-y).sqrMagnitude;if(d<best)best=d;}return Mathf.Sqrt(best);}
        static List<Vector3> Points(RosvikOsmV15.Way w){List<Vector3> p=w.Nodes.Select(n=>n.Pos).ToList();if(p.Count>2&&Flat(p[0]-p[p.Count-1]).sqrMagnitude<.01f)p.RemoveAt(p.Count-1);return p;}
        static float SignedArea(List<Vector3> p){float a=0;for(int i=0;i<p.Count;i++){Vector3 x=p[i],y=p[(i+1)%p.Count];a+=x.x*y.z-y.x*x.z;}return a*.5f;}

        static void Polygon(string name,Transform p,List<Vector3> pts,float y,Material mat) {
            if(pts.Count<3)return;List<Vector2> poly=pts.Select(v=>new Vector2(v.x,v.z)).ToList();int[] tris=Triangulate(poly);Vector3[] v=pts.Select(q=>new Vector3(q.x,y,q.z)).ToArray();Mesh m=new Mesh{name=name,vertices=v,triangles=tris};m.RecalculateNormals();m.RecalculateBounds();GameObject g=new GameObject(name);g.transform.SetParent(p,true);g.AddComponent<MeshFilter>().sharedMesh=m;g.AddComponent<MeshRenderer>().sharedMaterial=mat;
        }

        static int[] Triangulate(List<Vector2> p) {
            List<int> V=Enumerable.Range(0,p.Count).ToList();if(Area2(p)<0)V.Reverse();List<int> t=new List<int>();int guard=0;
            while(V.Count>3&&guard++<1000){bool cut=false;for(int i=0;i<V.Count;i++){int ia=V[(i-1+V.Count)%V.Count],ib=V[i],ic=V[(i+1)%V.Count];Vector2 a=p[ia],b=p[ib],c=p[ic];if(Cross(b-a,c-b)<=.00001f)continue;bool contains=false;for(int j=0;j<V.Count;j++){int ip=V[j];if(ip==ia||ip==ib||ip==ic)continue;if(InTri(p[ip],a,b,c)){contains=true;break;}}if(contains)continue;t.Add(ia);t.Add(ib);t.Add(ic);V.RemoveAt(i);cut=true;break;}if(!cut)break;}
            if(V.Count==3){t.Add(V[0]);t.Add(V[1]);t.Add(V[2]);}
            if(t.Count<3){t.Clear();for(int i=1;i<p.Count-1;i++){t.Add(0);t.Add(i);t.Add(i+1);}}
            return t.ToArray();
        }
        static float Area2(List<Vector2> p){float a=0;for(int i=0;i<p.Count;i++){Vector2 x=p[i],y=p[(i+1)%p.Count];a+=x.x*y.y-y.x*x.y;}return a;}
        static float Cross(Vector2 a,Vector2 b){return a.x*b.y-a.y*b.x;}
        static bool InTri(Vector2 p,Vector2 a,Vector2 b,Vector2 c){float c1=Cross(b-a,p-a),c2=Cross(c-b,p-b),c3=Cross(a-c,p-c);return c1>=0&&c2>=0&&c3>=0;}

        static void SegmentedHorizontalWall(Transform p,Vector3 o,float[] x,float z,Material m){for(int i=0;i+1<x.Length;i+=2){float a=x[i],b=x[i+1];Wall(p,o+new Vector3((a+b)*.5f,0,z),new Vector3(Mathf.Abs(b-a),1.5f,.22f),m);}}
        static void Wall(Transform p,Vector3 pos,Vector3 size,Material m){Box("wall",p,pos+Vector3.up*(size.y*.5f),size,Quaternion.identity,m,true);}
        static void Flat(string n,Transform p,Vector3 pos,Vector3 size,Material m){Box(n,p,pos+Vector3.up*(size.y*.5f),size,Quaternion.identity,m,true);}
        static void Border(Transform p,Vector3 c,Vector3 r,Vector3 f,float w,float d,Material m){FlatBox("curb",p,c+r*w*.5f,f,d,.16f,.06f,m);FlatBox("curb",p,c-r*w*.5f,f,d,.16f,.06f,m);FlatBox("curb",p,c+f*d*.5f,r,w,.16f,.06f,m);FlatBox("curb",p,c-f*d*.5f,r,w,.16f,.06f,m);}
        static void SnowPatch(Transform p,Vector3 pos,Vector3 axis,float w,float d,Material m){FlatBox("snow",p,pos+Vector3.up*.008f,axis,w,d,.018f,m);}

        static GameObject FlatBox(string n,Transform p,Vector3 pos,Vector3 widthAxis,float width,float depth,float thick,Material m){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,true);g.transform.position=pos;widthAxis=Flat(widthAxis).normalized;if(widthAxis.sqrMagnitude<.01f)widthAxis=Vector3.right;g.transform.rotation=Rot(widthAxis);g.transform.localScale=new Vector3(width,thick,depth);g.GetComponent<Renderer>().sharedMaterial=m;Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);return g;}
        static GameObject Box(string n,Transform p,Vector3 pos,Vector3 scale,Quaternion rot,Material m,bool col){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,true);g.transform.position=pos;g.transform.rotation=rot;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=m;if(!col){Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);}return g;}
        static GameObject LocalBox(string n,Transform p,Vector3 localPos,Vector3 scale,Material m,bool col){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=localPos;g.transform.localRotation=Quaternion.identity;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=m;if(!col){Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);}return g;}
        static GameObject Sphere(string n,Transform p,Vector3 pos,Vector3 scale,Material m){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=n;g.transform.SetParent(p,true);g.transform.position=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=m;Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);return g;}
        static Transform Group(Transform p,string n){GameObject g=new GameObject(n);g.transform.SetParent(p,false);return g.transform;}
        static Vector3 Flat(Vector3 v){v.y=0;return v;}
        static Vector3 Perp(Vector3 v){v=Flat(v).normalized;return new Vector3(-v.z,0,v.x);}
        static Quaternion Rot(Vector3 axis){axis=Flat(axis).normalized;if(axis.sqrMagnitude<.01f)axis=Vector3.right;return Quaternion.FromToRotation(Vector3.right,axis);}

        static Material Mat(string name,Color c,Shader shader){string path=GeneratedDir+"/"+name+".mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;m=new Material(shader){name="V55 "+name};if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);if(m.HasProperty("_Color"))m.SetColor("_Color",c);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",.08f);AssetDatabase.CreateAsset(m,path);return m;}
        static Shader FindGoodShader(UScene scene){foreach(GameObject root in scene.GetRootGameObjects())foreach(Renderer r in root.GetComponentsInChildren<Renderer>(true))foreach(Material m in r.sharedMaterials){if(!m||!m.shader||!m.shader.isSupported)continue;string s=m.shader.name??"";if(s.IndexOf("InternalError",StringComparison.OrdinalIgnoreCase)>=0||s.StartsWith("Hidden/"))continue;return m.shader;}Shader urp=Shader.Find("Universal Render Pipeline/Lit");if(urp&&urp.isSupported)return urp;Shader std=Shader.Find("Standard");return std&&std.isSupported?std:null;}

        static void DisableOldPasses(UScene scene){string[] names={"32 TOPDOWN GAMEPLAY V51 - PROTOTYPE","33 TOPDOWN GAMEPLAY V52 - FIRST LOOP","34 TOPDOWN V53 - SCHOOL INTERIOR SLICE","35 TOPDOWN V54 - CLEAN VISUAL CAMPUS","31 SCHOOL CAMPUS V49 - TOP LEVEL VISIBLE REBUILD","30 SCHOOL CAMPUS V48 - FINAL EXTERIOR REBUILD","29 SCHOOL CAMPUS V47 - ENTRANCE ANCHORED","28 SCHOOL CAMPUS V46 - COZY DENSITY PASS","27 SCHOOL CAMPUS V45 - VISIBLE REBUILD"};foreach(string n in names){Transform t=Resources.FindObjectsOfTypeAll<Transform>().FirstOrDefault(x=>x&&x.gameObject.scene==scene&&x.name==n);if(t)t.gameObject.SetActive(false);}}
        static void DisableGameplayOnPlayer(Transform player){foreach(MonoBehaviour mb in player.GetComponents<MonoBehaviour>()){if(!mb)continue;string n=mb.GetType().Name;if(n.StartsWith("TopDownGameplayV")&&n!="TopDownGameplayV55")mb.enabled=false;}}

        static void HideMappedBuilding(UScene scene,RosvikOsmV15.Way way){Transform buildings=Resources.FindObjectsOfTypeAll<Transform>().FirstOrDefault(t=>t&&t.gameObject.scene==scene&&t.name=="02 BUILDINGS - OSM FOOTPRINTS");if(!buildings)return;Vector3 target=RosvikOsmV15.Centroid(way);Transform best=null;float bestD=float.MaxValue;foreach(Transform c in buildings){Renderer rr=c.GetComponentInChildren<Renderer>(true);Vector3 center=rr?rr.bounds.center:c.position;float d=Flat(center-target).sqrMagnitude;if(d<bestD){bestD=d;best=c;}}if(best&&bestD<225f)best.gameObject.SetActive(false);}
        static Transform FindSceneTransform(UScene scene,string n){return Resources.FindObjectsOfTypeAll<Transform>().FirstOrDefault(t=>t&&t.gameObject.scene==scene&&t.name==n);}
        static void RemoveTopLevel(UScene scene,string n){GameObject g=scene.GetRootGameObjects().FirstOrDefault(x=>x.name==n);if(g)UnityEngine.Object.DestroyImmediate(g);}

        static void SetupCamera(UScene scene,Transform player){Camera cam=Resources.FindObjectsOfTypeAll<Camera>().FirstOrDefault(c=>c&&c.gameObject.scene==scene&&c.CompareTag("MainCamera"));if(!cam)return;foreach(MonoBehaviour mb in cam.GetComponents<MonoBehaviour>()){if(mb&&mb.GetType().Name=="IsometricCameraRig")mb.enabled=false;}TopDownCameraRigV51 rig=cam.GetComponent<TopDownCameraRigV51>();if(!rig)rig=cam.gameObject.AddComponent<TopDownCameraRigV51>();rig.enabled=true;rig.target=player;rig.pitch=84.8f;rig.yaw=0f;rig.distance=30f;rig.orthographicSize=8.6f;rig.minSize=6.2f;rig.maxSize=12.5f;rig.zoomStep=.65f;rig.followSharpness=15f;rig.focusOffset=Vector3.zero;cam.orthographic=true;cam.orthographicSize=8.6f;cam.backgroundColor=new Color(.19f,.22f,.20f);EditorUtility.SetDirty(cam);EditorUtility.SetDirty(rig);}
        static void SetupLighting(UScene scene){RenderSettings.fog=false;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.48f,.49f,.43f);Light sun=Resources.FindObjectsOfTypeAll<Light>().FirstOrDefault(l=>l&&l.gameObject.scene==scene&&l.type==LightType.Directional);if(sun){sun.intensity=.78f;sun.color=new Color(.96f,.90f,.78f);sun.shadows=LightShadows.Soft;sun.shadowStrength=.27f;}QualitySettings.shadowDistance=40f;}
    }
}
#endif
