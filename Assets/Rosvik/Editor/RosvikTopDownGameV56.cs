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
    public static class RosvikTopDownGameV56 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_TOPDOWN_GAME_V56";
        const int Version = 56;
        const string GroupName = "37 TOPDOWN V56 - CLEAN GAME LEVEL";
        const string GeneratedDir = "Assets/Rosvik/GeneratedV56";
        static readonly Vector3 O = new Vector3(2000f,0f,2000f);

        class Guard {
            readonly List<Rect> used = new List<Rect>();
            public bool Reserve(Vector3 p,float w,float d,float pad=.18f) {
                Rect r = new Rect(p.x-w*.5f-pad,p.z-d*.5f-pad,w+pad*2f,d+pad*2f);
                foreach(Rect q in used) if(r.Overlaps(q)) return false;
                used.Add(r); return true;
            }
            public void Block(Vector3 p,float w,float d,float pad=.2f){ Reserve(p,w,d,pad); }
        }

        static RosvikTopDownGameV56(){ EditorApplication.delayCall -= Auto; EditorApplication.delayCall += Auto; }

        [MenuItem("Rosvik/V56 BUILD CLEAN TOPDOWN GAME")]
        public static void Force(){ EditorPrefs.DeleteKey(Key); Build(); }

        static void Auto(){
            if(EditorPrefs.GetInt(Key,0)>=Version) return;
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating){ EditorApplication.delayCall += Auto; return; }
            Build();
        }

        static void Build(){
            try {
                if(!File.Exists(ScenePath)) return;
                UScene scene=EditorSceneManager.GetActiveScene();
                if(scene.path!=ScenePath) scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
                Transform player=FindSceneTransform(scene,"PLAYER");
                if(!player) throw new Exception("PLAYER missing");

                RemoveTopLevel(scene,GroupName);
                DisableOldVisualPasses(scene);
                DisableOldGameplay(player);
                RemoveOldPlayerMarkers(player);

                Directory.CreateDirectory(GeneratedDir); AssetDatabase.Refresh();
                Shader shader=FindGoodShader(scene); if(!shader) throw new Exception("No supported shader");

                Material grass=Mat("grass",new Color(.18f,.245f,.135f),shader);
                Material grassDark=Mat("grass_dark",new Color(.10f,.17f,.09f),shader);
                Material asphalt=Mat("asphalt",new Color(.075f,.083f,.085f),shader);
                Material path=Mat("paving",new Color(.40f,.355f,.285f),shader);
                Material curb=Mat("curb",new Color(.70f,.68f,.60f),shader);
                Material snow=Mat("snow",new Color(.84f,.875f,.86f),shader);
                Material floor=Mat("school_floor",new Color(.31f,.30f,.255f),shader);
                Material classFloor=Mat("class_floor",new Color(.39f,.35f,.245f),shader);
                Material staffFloor=Mat("staff_floor",new Color(.285f,.345f,.31f),shader);
                Material utilityFloor=Mat("utility_floor",new Color(.255f,.285f,.29f),shader);
                Material gymFloor=Mat("gym_floor",new Color(.47f,.315f,.18f),shader);
                Material wall=Mat("wall",new Color(.76f,.72f,.61f),shader);
                Material wallTop=Mat("wall_top",new Color(.90f,.86f,.73f),shader);
                Material window=Mat("window",new Color(.10f,.25f,.30f),shader);
                Material wood=Mat("wood",new Color(.38f,.215f,.105f),shader);
                Material woodLight=Mat("wood_light",new Color(.52f,.34f,.17f),shader);
                Material metal=Mat("metal",new Color(.075f,.09f,.095f),shader);
                Material cabinet=Mat("cabinet",new Color(.17f,.29f,.32f),shader);
                Material cabinet2=Mat("cabinet_light",new Color(.28f,.40f,.40f),shader);
                Material white=Mat("white",new Color(.88f,.865f,.77f),shader);
                Material red=Mat("red",new Color(.61f,.16f,.13f),shader);
                Material amber=Mat("amber",new Color(.94f,.55f,.12f),shader);
                Material dark=Mat("dark",new Color(.055f,.062f,.06f),shader);
                Material blue=Mat("blue",new Color(.12f,.38f,.54f),shader);
                Material green=Mat("green",new Color(.07f,.24f,.11f),shader);

                GameObject root=new GameObject(GroupName);
                Transform terrain=Group(root.transform,"V56 CAMPUS");
                Transform school=Group(root.transform,"V56 SCHOOL");
                Transform hall=Group(root.transform,"V56 SPORTHALL");
                Transform props=Group(root.transform,"V56 EXTERIOR ASSETS");

                // A completely clean playfield, far from the old prototype. The old Rosvik work remains reversible.
                Box("ground base",terrain,O+new Vector3(4,-.18f,7),new Vector3(112,.32f,96),Quaternion.identity,grass,true);
                BuildExterior(terrain,props,grassDark,asphalt,path,curb,snow,wood,metal,white,green);

                List<TopDownGameplayV56.Spot> spots=new List<TopDownGameplayV56.Spot>();
                BuildSchool(school,spots,floor,classFloor,staffFloor,utilityFloor,wall,wallTop,window,wood,woodLight,metal,cabinet,cabinet2,white,red,amber,blue,dark);
                BuildSportsHall(hall,spots,gymFloor,utilityFloor,wall,wallTop,window,wood,metal,cabinet,white,amber,blue);

                player.position=O+new Vector3(-6,.16f,-14.5f);
                AddPlayerMarker(player,amber,dark);

                TopDownGameplayV56 gameplay=player.GetComponent<TopDownGameplayV56>();
                if(!gameplay) gameplay=player.gameObject.AddComponent<TopDownGameplayV56>();
                gameplay.enabled=true; gameplay.spots.Clear(); gameplay.spots.AddRange(spots); gameplay.animationTime=.18f;
                EditorUtility.SetDirty(gameplay);

                SetupCamera(scene,player);
                SetupLighting(scene);

                EditorPrefs.SetInt(Key,Version);
                Selection.activeGameObject=player.gameObject;
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene,ScenePath);
                AssetDatabase.SaveAssets(); SceneView.RepaintAll();
                Debug.Log("ROSVIK V56 SUCCESS: clean hand-built top-down game level. Old prototype isolated; no procedural scatter; doors and cabinets are physical animated props.");
            } catch(Exception ex){ Debug.LogError("ROSVIK V56 FAILED: "+ex); }
        }

        static void BuildExterior(Transform p,Transform assets,Material grassDark,Material asphalt,Material path,Material curb,Material snow,Material wood,Material metal,Material white,Material green){
            // Parking and arrival.
            Vector3 parking=O+new Vector3(-14,0,-24);
            Flat("parking",p,parking,new Vector3(36,.055f,14),asphalt);
            BorderRect(p,parking,36,14,curb);
            for(int i=-5;i<=5;i++) Flat("parking stripe",p,parking+new Vector3(i*3f,.04f,0),new Vector3(.10f,.018f,11f),white);
            Flat("dropoff lane",p,O+new Vector3(7,0,-17),new Vector3(54,.052f,6f),asphalt);
            Flat("entrance plaza",p,O+new Vector3(-6,0,-11.5f),new Vector3(14,.060f,6f),path);
            Flat("entrance walk",p,O+new Vector3(-6,0,-16.5f),new Vector3(4,.058f,5f),path);
            Flat("schoolyard",p,O+new Vector3(-11,0,31),new Vector3(42,.050f,19f),path);
            BorderRect(p,O+new Vector3(-11,0,31),42,19,curb);
            Flat("service yard",p,O+new Vector3(34,0,-18),new Vector3(22,.052f,10f),asphalt);
            BorderRect(p,O+new Vector3(34,0,-18),22,10,curb);

            // Court / playground markings.
            Flat("yard line",p,O+new Vector3(-11,.04f,31),new Vector3(34,.015f,.10f),white);
            Flat("yard line",p,O+new Vector3(-11,.04f,25),new Vector3(34,.015f,.10f),white);
            Flat("yard line",p,O+new Vector3(-11,.04f,37),new Vector3(34,.015f,.10f),white);
            Swing(p,O+new Vector3(-25,0,33),metal,wood);
            Picnic(p,O+new Vector3(3,0,33),wood,metal);

            // Snow remnants deliberately kept at edges.
            Flat("snow",p,O+new Vector3(-42,.035f,-34),new Vector3(13,.025f,2.2f),snow);
            Flat("snow",p,O+new Vector3(42,.035f,34),new Vector3(12,.025f,2.0f),snow);
            Flat("snow",p,O+new Vector3(31,.035f,-25),new Vector3(9,.025f,1.5f),snow);

            Guard outside=new Guard();
            GameObject car=FindModel("car_stationwagon","car_sedan","car_hatchback");
            GameObject bench=FindModel("bench");
            GameObject lamp=FindModel("streetlight");
            GameObject tree=FindModel("tree_pineDefaultA","tree_pineTallA","tree_default_fall");
            GameObject bush=FindModel("plant_bushDetailed","bush");
            GameObject dumpster=FindModel("dumpster");

            Vector3[] cars={O+new Vector3(-27,0,-24),O+new Vector3(-15,0,-24),O+new Vector3(-3,0,-24)};
            foreach(Vector3 q in cars) if(outside.Reserve(q,2.4f,5.0f,.35f)) { if(car) PlaceModel(car,"parked car",assets,q,Vector3.forward,1.45f); else CarFallback(assets,q,metal); }

            Vector3[] benches={O+new Vector3(-16,0,23),O+new Vector3(-4,0,23),O+new Vector3(-13,0,-12)};
            foreach(Vector3 q in benches) if(outside.Reserve(q,2.5f,1.2f,.35f)){ if(bench) PlaceModel(bench,"bench asset",assets,q,Vector3.right,.92f); else Bench(assets,q,wood,metal); }

            Vector3[] lamps={O+new Vector3(-13,0,-14),O+new Vector3(1,0,-14),O+new Vector3(-31,0,23),O+new Vector3(9,0,23),O+new Vector3(25,0,-15)};
            foreach(Vector3 q in lamps) if(outside.Reserve(q,1.0f,1.0f,.25f)){ if(lamp) PlaceModel(lamp,"lamp asset",assets,q,Vector3.forward,4.4f); else LampFallback(assets,q,metal,white); }

            Vector3[] trees={O+new Vector3(-45,0,-8),O+new Vector3(-45,0,10),O+new Vector3(-42,0,30),O+new Vector3(-35,0,43),O+new Vector3(-15,0,44),O+new Vector3(8,0,44),O+new Vector3(28,0,42),O+new Vector3(48,0,31),O+new Vector3(49,0,11),O+new Vector3(49,0,-9),O+new Vector3(46,0,-32),O+new Vector3(15,0,-38),O+new Vector3(-12,0,-39),O+new Vector3(-39,0,-35)};
            foreach(Vector3 q in trees) if(outside.Reserve(q,3.2f,3.2f,.4f)){ if(tree) PlaceModel(tree,"tree asset",assets,q,Vector3.forward,4.3f); else TreeFallback(assets,q,wood,green); }

            Vector3[] bushes={O+new Vector3(-14,0,-10.5f),O+new Vector3(2,0,-10.5f),O+new Vector3(-31,0,20.5f),O+new Vector3(10,0,20.5f),O+new Vector3(22,0,-11.5f),O+new Vector3(42,0,-11.5f)};
            foreach(Vector3 q in bushes) if(outside.Reserve(q,1.7f,1.7f,.25f)){ if(bush) PlaceModel(bush,"bush asset",assets,q,Vector3.forward,1.15f); else Shrub(assets,q,green); }

            if(outside.Reserve(O+new Vector3(41,0,-18),2.5f,1.6f,.35f)){ if(dumpster) PlaceModel(dumpster,"service dumpster",assets,O+new Vector3(41,0,-18),Vector3.right,1.25f); else DumpsterFallback(assets,O+new Vector3(41,0,-18),metal); }
        }

        static void BuildSchool(Transform p,List<TopDownGameplayV56.Spot> spots,Material floor,Material classFloor,Material staffFloor,Material utilityFloor,Material wall,Material wallTop,Material window,Material wood,Material woodLight,Material metal,Material cabinet,Material cabinet2,Material white,Material red,Material amber,Material blue,Material dark){
            // Building: x -30..14, z -9..19. No roof: top-down reads the real rooms directly.
            Flat("school base",p,O+new Vector3(-8,0,5),new Vector3(44,.060f,28),floor);
            Flat("class A floor",p,O+new Vector3(-22.5f,.025f,10.5f),new Vector3(13,.025f,15),classFloor);
            Flat("class B floor",p,O+new Vector3(-8.5f,.025f,10.5f),new Vector3(13,.025f,15),classFloor);
            Flat("staff floor",p,O+new Vector3(6.5f,.025f,10.5f),new Vector3(14,.025f,15),staffFloor);
            Flat("janitor floor",p,O+new Vector3(-24,.025f,-5.5f),new Vector3(10,.025f,6),utilityFloor);
            Flat("storage floor",p,O+new Vector3(-13.5f,.025f,-5.5f),new Vector3(7,.025f,6),utilityFloor);
            Flat("office floor",p,O+new Vector3(3,.025f,-5.5f),new Vector3(7,.025f,6),staffFloor);
            Flat("resource floor",p,O+new Vector3(10,.025f,-5.5f),new Vector3(6,.025f,6),utilityFloor);

            // Outer shell with deliberate gaps only where real doors exist.
            WallH(p,-9,-30,-7.9f,wall,wallTop); WallH(p,-9,-4.1f,14,wall,wallTop);
            WallH(p,19,-30,14,wall,wallTop); WallV(p,-30,-9,19,wall,wallTop);
            WallV(p,14,-9,1.9f,wall,wallTop); WallV(p,14,4.1f,19,wall,wallTop);
            AddWindowH(p,19,-23,5.2f,window); AddWindowH(p,19,-8.5f,5.2f,window); AddWindowH(p,19,6.5f,5.2f,window);
            AddWindowV(p,-30,10,5.0f,window); AddWindowV(p,14,11,4.6f,window);

            // North room front wall at z=2, three door gaps.
            WallH(p,2,-30,-23.5f,wall,wallTop); WallH(p,2,-21.5f,-9.5f,wall,wallTop); WallH(p,2,-7.5f,5.2f,wall,wallTop); WallH(p,2,7.2f,14,wall,wallTop);
            WallV(p,-16,2,19,wall,wallTop); WallV(p,-1,2,19,wall,wallTop);

            // South room front wall at z=-2. Foyer remains open from x -10..-2.
            WallH(p,-2,-30,-25.0f,wall,wallTop); WallH(p,-2,-23.0f,-15.0f,wall,wallTop); WallH(p,-2,-13.0f,-10,wall,wallTop);
            WallH(p,-2,-2,2.0f,wall,wallTop); WallH(p,-2,4.0f,8.5f,wall,wallTop); WallH(p,-2,10.5f,14,wall,wallTop);
            WallV(p,-19,-9,-2,wall,wallTop); WallV(p,-10,-9,-2,wall,wallTop); WallV(p,-2,-9,-2,wall,wallTop); WallV(p,6.5f,-9,-2,wall,wallTop);

            // Entry doors and room doors. Door swing zones are kept clear by furniture layout below.
            spots.Add(DoubleDoorH(p,"huvudentrén",O+new Vector3(-6,0,-9),3.8f,woodLight,metal,amber));
            spots.Add(DoubleDoorH(p,"innerdörrarna",O+new Vector3(-6,0,-2),3.5f,woodLight,metal,amber));
            spots.Add(SingleDoorH(p,"klassrum A",O+new Vector3(-22.5f,0,2),1.9f,wood,metal,amber));
            spots.Add(SingleDoorH(p,"klassrum B",O+new Vector3(-8.5f,0,2),1.9f,wood,metal,amber));
            spots.Add(SingleDoorH(p,"personalrummet",O+new Vector3(6.2f,0,2),1.9f,wood,metal,amber));
            spots.Add(SingleDoorH(p,"vaktmästarrummet",O+new Vector3(-24,0,-2),1.9f,wood,metal,amber));
            spots.Add(SingleDoorH(p,"materialförrådet",O+new Vector3(-14,0,-2),1.9f,wood,metal,amber));
            spots.Add(SingleDoorH(p,"kontoret",O+new Vector3(3,0,-2),1.9f,wood,metal,amber));
            spots.Add(SingleDoorH(p,"resursrummet",O+new Vector3(9.5f,0,-2),1.9f,wood,metal,amber));
            spots.Add(SingleDoorV(p,"korridoren mot sporthallen",O+new Vector3(14,0,3),1.9f,wood,metal,amber));

            // Entrance canopy / facade identity, still visible in the top-down art style.
            Box("entrance canopy",p,O+new Vector3(-6,1.15f,-10.35f),new Vector3(6.2f,.18f,2.4f),Quaternion.identity,woodLight,false);
            Box("school sign",p,O+new Vector3(-6,.80f,-9.22f),new Vector3(4.8f,.55f,.12f),Quaternion.identity,dark,false);
            GroundText(p,"ROSVIKS SKOLA",O+new Vector3(-6,.08f,-12.2f),.50f,new Color(.92f,.80f,.52f));

            // Classrooms: each desk+chair is one reserved footprint. No overlap possible.
            Guard a=new Guard(); a.Block(O+new Vector3(-22.5f,0,2.8f),3f,2.2f);
            Vector3[] da={O+new Vector3(-26,0,6.2f),O+new Vector3(-22.5f,0,6.2f),O+new Vector3(-19,0,6.2f),O+new Vector3(-26,0,10.1f),O+new Vector3(-22.5f,0,10.1f),O+new Vector3(-19,0,10.1f)};
            foreach(Vector3 q in da) if(a.Reserve(q,2.0f,2.0f,.25f)) StudentDesk(p,q,wood,metal);
            if(a.Reserve(O+new Vector3(-22.5f,0,15.8f),3.6f,1.7f,.3f)) TeacherZone(p,O+new Vector3(-22.5f,0,15.8f),wood,metal,white);
            var cabA=Cabinet(p,"klassrumsskåpet A",O+new Vector3(-28.4f,0,15.5f),1.6f,1.1f,cabinet,woodLight,amber); cabA.itemName="Batterier"; spots.Add(cabA);
            Bookcase(p,O+new Vector3(-17.0f,0,15.6f),1.6f,cabinet,woodLight);

            Guard b=new Guard(); b.Block(O+new Vector3(-8.5f,0,2.8f),3f,2.2f);
            Vector3[] db={O+new Vector3(-12,0,6.2f),O+new Vector3(-8.5f,0,6.2f),O+new Vector3(-5,0,6.2f),O+new Vector3(-12,0,10.1f),O+new Vector3(-8.5f,0,10.1f),O+new Vector3(-5,0,10.1f)};
            foreach(Vector3 q in db) if(b.Reserve(q,2.0f,2.0f,.25f)) StudentDesk(p,q,wood,metal);
            if(b.Reserve(O+new Vector3(-8.5f,0,15.8f),3.6f,1.7f,.3f)) TeacherZone(p,O+new Vector3(-8.5f,0,15.8f),wood,metal,white);
            var cabB=Cabinet(p,"lärarskåpet",O+new Vector3(-14.4f,0,15.5f),1.6f,1.1f,cabinet2,woodLight,amber); cabB.itemName="Nyckelknippa"; spots.Add(cabB);
            Bookcase(p,O+new Vector3(-2.2f,0,15.6f),1.6f,cabinet,woodLight);

            Guard staff=new Guard(); staff.Block(O+new Vector3(6.2f,0,2.8f),3f,2.2f);
            if(staff.Reserve(O+new Vector3(5.8f,0,9.0f),6.0f,4.2f,.35f)) StaffTable(p,O+new Vector3(5.8f,0,9.0f),wood,metal);
            if(staff.Reserve(O+new Vector3(10.8f,0,15.2f),3.2f,1.4f,.25f)) Kitchenette(p,O+new Vector3(10.8f,0,15.2f),cabinet,metal);
            var staffCab=Cabinet(p,"nyckelskåpet",O+new Vector3(1.0f,0,15.4f),1.8f,1.1f,cabinet2,woodLight,amber); staffCab.itemName="Huvudnyckel"; spots.Add(staffCab);
            Bookcase(p,O+new Vector3(4.0f,0,15.6f),2.0f,cabinet,woodLight);

            // South rooms, deliberately sparse and readable.
            Workbench(p,O+new Vector3(-25,0,-6.4f),wood,metal);
            var tool=Cabinet(p,"verktygsskåpet",O+new Vector3(-20.2f,0,-6.0f),1.6f,1.1f,cabinet,woodLight,amber); tool.itemName="Skruvmejsel"; spots.Add(tool);
            Bookcase(p,O+new Vector3(-15.0f,0,-6.0f),1.6f,cabinet,woodLight);
            Bookcase(p,O+new Vector3(-12.0f,0,-6.0f),1.6f,cabinet,woodLight);
            OfficeDesk(p,O+new Vector3(2.2f,0,-5.7f),wood,metal);
            var aid=Cabinet(p,"första hjälpen-skåpet",O+new Vector3(5.2f,0,-7.2f),1.25f,.8f,red,white,amber); aid.itemName="Förband"; spots.Add(aid);
            LockerRow(p,O+new Vector3(10.3f,0,-6.4f),4,cabinet2,metal);

            // Corridor furniture uses wall-side pockets only, leaving a clear 3m movement spine.
            Bench(p,O+new Vector3(-17.5f,0,.25f),woodLight,metal);
            Bench(p,O+new Vector3(-1.5f,0,.25f),woodLight,metal);
            NoticeBoard(p,O+new Vector3(-13,0,1.75f),wood,white);
            NoticeBoard(p,O+new Vector3(1.5f,0,1.75f),wood,white);
        }

        static void BuildSportsHall(Transform p,List<TopDownGameplayV56.Spot> spots,Material gymFloor,Material utilityFloor,Material wall,Material wallTop,Material window,Material wood,Material metal,Material cabinet,Material white,Material amber,Material blue){
            // Hall x 19..45, z -9..19. Connector x 14..19, z 1..5.
            Flat("connector floor",p,O+new Vector3(16.5f,0,3),new Vector3(5,.060f,4),utilityFloor);
            WallH(p,1,14,19,wall,wallTop); WallH(p,5,14,19,wall,wallTop);

            Flat("sporthall floor",p,O+new Vector3(32,0,5),new Vector3(26,.060f,28),gymFloor);
            WallH(p,-9,19,45,wall,wallTop); WallH(p,19,19,45,wall,wallTop);
            WallV(p,19,-9,1.9f,wall,wallTop); WallV(p,19,4.1f,19,wall,wallTop); WallV(p,45,-9,19,wall,wallTop);
            spots.Add(SingleDoorV(p,"sporthallsdörren",O+new Vector3(19,0,3),1.9f,wood,metal,amber));
            AddWindowV(p,45,5,7.0f,window); AddWindowH(p,19,30,7.0f,window);

            // Court kept clear: only markings inside play area.
            Vector3 c=O+new Vector3(31,0,5);
            CourtLineH(p,c+new Vector3(0,.04f,-9),20,white); CourtLineH(p,c+new Vector3(0,.04f,9),20,white);
            CourtLineV(p,c+new Vector3(-10,.04f,0),18,white); CourtLineV(p,c+new Vector3(10,.04f,0),18,white);
            CourtLineV(p,c+new Vector3(0,.04f,0),18,white);
            CenterCircle(p,c+Vector3.up*.04f,2.8f,white);

            // Small bleacher on the long side, exactly as requested, outside court footprint.
            Bleachers(p,O+new Vector3(42.3f,0,5),wood,metal);
            LockerRow(p,O+new Vector3(23.0f,0,-7.4f),5,cabinet,metal);
            var equip=Cabinet(p,"redskapsskåpet",O+new Vector3(38.0f,0,-7.4f),2.2f,1.1f,cabinet,wood,amber); equip.itemName="Sporttejp"; spots.Add(equip);
            EquipmentRack(p,O+new Vector3(33.0f,0,-7.4f),metal,blue);
        }

        static TopDownGameplayV56.Spot DoubleDoorH(Transform p,string name,Vector3 pos,float width,Material door,Material metal,Material marker){
            GameObject spot=new GameObject("DOOR "+name); spot.transform.SetParent(p,true); spot.transform.position=pos;
            float leafW=width*.5f;
            Transform left=DoorLeafH(spot.transform,"left leaf",new Vector3(-width*.5f,0,0),leafW,true,door,metal);
            Transform right=DoorLeafH(spot.transform,"right leaf",new Vector3(width*.5f,0,0),leafW,false,door,metal);
            GameObject m=InteractionMarker(spot.transform,marker,.80f);
            return new TopDownGameplayV56.Spot{spot=spot,marker=m,displayName=name,kind=TopDownGameplayV56.SpotKind.Door,interactRadius=2.8f,movingPart=left,movingPart2=right,closedEuler=Vector3.zero,openEuler=new Vector3(0,-88,0),closedEuler2=Vector3.zero,openEuler2=new Vector3(0,88,0)};
        }

        static TopDownGameplayV56.Spot SingleDoorH(Transform p,string name,Vector3 pos,float width,Material door,Material metal,Material marker){
            GameObject spot=new GameObject("DOOR "+name);spot.transform.SetParent(p,true);spot.transform.position=pos;
            Transform leaf=DoorLeafH(spot.transform,"door leaf",new Vector3(-width*.5f,0,0),width,true,door,metal);
            GameObject m=InteractionMarker(spot.transform,marker,.68f);
            return new TopDownGameplayV56.Spot{spot=spot,marker=m,displayName=name,kind=TopDownGameplayV56.SpotKind.Door,interactRadius=2.25f,movingPart=leaf,closedEuler=Vector3.zero,openEuler=new Vector3(0,-92,0)};
        }

        static TopDownGameplayV56.Spot SingleDoorV(Transform p,string name,Vector3 pos,float width,Material door,Material metal,Material marker){
            GameObject spot=new GameObject("DOOR "+name);spot.transform.SetParent(p,true);spot.transform.position=pos;
            GameObject pivot=new GameObject("hinge");pivot.transform.SetParent(spot.transform,false);pivot.transform.localPosition=new Vector3(0,0,-width*.5f);
            Box("door slab",pivot.transform,pivot.transform.position+new Vector3(0,.62f,width*.5f),new Vector3(.16f,1.24f,width),Quaternion.identity,door,true);
            Box("handle",pivot.transform,pivot.transform.position+new Vector3(-.12f,.67f,width*.82f),new Vector3(.10f,.10f,.10f),Quaternion.identity,metal,false);
            GameObject m=InteractionMarker(spot.transform,marker,.68f);
            return new TopDownGameplayV56.Spot{spot=spot,marker=m,displayName=name,kind=TopDownGameplayV56.SpotKind.Door,interactRadius=2.25f,movingPart=pivot.transform,closedEuler=Vector3.zero,openEuler=new Vector3(0,92,0)};
        }

        static Transform DoorLeafH(Transform parent,string name,Vector3 hinge,float width,bool fromLeft,Material door,Material metal){
            GameObject pivot=new GameObject(name+" hinge");pivot.transform.SetParent(parent,false);pivot.transform.localPosition=hinge;
            float sign=fromLeft?1f:-1f;
            Box(name,pivot.transform,pivot.transform.position+new Vector3(sign*width*.5f,.62f,0),new Vector3(width,1.24f,.16f),Quaternion.identity,door,true);
            Box("handle",pivot.transform,pivot.transform.position+new Vector3(sign*width*.80f,.67f,-.12f),new Vector3(.10f,.10f,.10f),Quaternion.identity,metal,false);
            return pivot.transform;
        }

        static TopDownGameplayV56.Spot Cabinet(Transform p,string name,Vector3 pos,float width,float depth,Material body,Material door,Material marker){
            GameObject spot=new GameObject("CABINET "+name);spot.transform.SetParent(p,true);spot.transform.position=pos;
            float h=1.35f;
            Box("cabinet back",spot.transform,pos+new Vector3(0,h*.5f,depth*.42f),new Vector3(width,h,.10f),Quaternion.identity,body,true);
            Box("cabinet left",spot.transform,pos+new Vector3(-width*.47f,h*.5f,0),new Vector3(.10f,h,depth),Quaternion.identity,body,true);
            Box("cabinet right",spot.transform,pos+new Vector3(width*.47f,h*.5f,0),new Vector3(.10f,h,depth),Quaternion.identity,body,true);
            for(int i=0;i<3;i++) Box("shelf",spot.transform,pos+new Vector3(0,.25f+i*.42f,.02f),new Vector3(width-.12f,.06f,depth-.12f),Quaternion.identity,body,true);
            GameObject pivot=new GameObject("cabinet hinge");pivot.transform.SetParent(spot.transform,false);pivot.transform.localPosition=new Vector3(-width*.47f,0,-depth*.48f);
            Box("cabinet door",pivot.transform,pivot.transform.position+new Vector3(width*.47f,h*.5f,0),new Vector3(width-.08f,h,.08f),Quaternion.identity,door,true);
            GameObject m=InteractionMarker(spot.transform,marker,.62f);
            return new TopDownGameplayV56.Spot{spot=spot,marker=m,displayName=name,kind=TopDownGameplayV56.SpotKind.Cabinet,interactRadius=2.05f,movingPart=pivot.transform,closedEuler=Vector3.zero,openEuler=new Vector3(0,-105,0)};
        }

        static void StudentDesk(Transform p,Vector3 q,Material wood,Material metal){
            Box("student desk",p,q+new Vector3(0,.42f,0),new Vector3(1.35f,.12f,.72f),Quaternion.identity,wood,true);
            Box("desk legs",p,q+new Vector3(0,.20f,0),new Vector3(1.10f,.40f,.08f),Quaternion.identity,metal,true);
            Box("chair seat",p,q+new Vector3(0,.28f,-.82f),new Vector3(.58f,.10f,.55f),Quaternion.identity,wood,true);
            Box("chair back",p,q+new Vector3(0,.58f,-1.05f),new Vector3(.58f,.58f,.09f),Quaternion.identity,wood,true);
        }
        static void TeacherZone(Transform p,Vector3 q,Material wood,Material metal,Material white){
            Box("teacher desk",p,q+new Vector3(0,.43f,0),new Vector3(2.5f,.14f,.85f),Quaternion.identity,wood,true);
            Box("teacher desk base",p,q+new Vector3(-.95f,.22f,0),new Vector3(.10f,.44f,.65f),Quaternion.identity,metal,true);
            Box("teacher desk base",p,q+new Vector3(.95f,.22f,0),new Vector3(.10f,.44f,.65f),Quaternion.identity,metal,true);
            Box("whiteboard",p,q+new Vector3(0,.75f,1.35f),new Vector3(4.4f,1.0f,.08f),Quaternion.identity,white,false);
        }
        static void StaffTable(Transform p,Vector3 q,Material wood,Material metal){
            Box("staff table",p,q+Vector3.up*.44f,new Vector3(4.2f,.16f,1.45f),Quaternion.identity,wood,true);
            Vector3[] chairs={new Vector3(-1.5f,0,-1.2f),new Vector3(0,0,-1.2f),new Vector3(1.5f,0,-1.2f),new Vector3(-1.5f,0,1.2f),new Vector3(0,0,1.2f),new Vector3(1.5f,0,1.2f)};
            foreach(Vector3 c in chairs){Box("chair",p,q+c+Vector3.up*.3f,new Vector3(.58f,.60f,.58f),Quaternion.identity,wood,true);}
        }
        static void Kitchenette(Transform p,Vector3 q,Material cabinet,Material metal){
            Box("kitchen counter",p,q+Vector3.up*.45f,new Vector3(3.0f,.90f,.70f),Quaternion.identity,cabinet,true);
            Box("sink",p,q+new Vector3(.55f,.93f,0),new Vector3(.80f,.07f,.48f),Quaternion.identity,metal,false);
        }
        static void Workbench(Transform p,Vector3 q,Material wood,Material metal){Box("workbench",p,q+Vector3.up*.45f,new Vector3(3.2f,.90f,.80f),Quaternion.identity,wood,true);Box("pegboard",p,q+new Vector3(0,1.05f,.42f),new Vector3(3.0f,1.0f,.08f),Quaternion.identity,metal,false);}
        static void OfficeDesk(Transform p,Vector3 q,Material wood,Material metal){Box("office desk",p,q+Vector3.up*.43f,new Vector3(2.4f,.86f,.85f),Quaternion.identity,wood,true);Box("office chair",p,q+new Vector3(0,.34f,-1.0f),new Vector3(.70f,.68f,.70f),Quaternion.identity,metal,true);}
        static void Bookcase(Transform p,Vector3 q,float width,Material body,Material shelf){
            Box("bookcase back",p,q+Vector3.up*.80f,new Vector3(width,1.6f,.10f),Quaternion.identity,body,true);
            for(int i=0;i<4;i++) Box("book shelf",p,q+new Vector3(0,.20f+i*.42f,-.28f),new Vector3(width,.07f,.56f),Quaternion.identity,shelf,true);
            for(int i=-2;i<=2;i++) Box("books",p,q+new Vector3(i*width/6f,.48f,-.38f),new Vector3(.16f,.42f,.18f),Quaternion.identity,(i%2==0?body:shelf),false);
        }
        static void LockerRow(Transform p,Vector3 q,int count,Material body,Material metal){
            float w=.78f; for(int i=0;i<count;i++){Vector3 x=q+new Vector3((i-(count-1)*.5f)*w,0,0);Box("locker",p,x+Vector3.up*.78f,new Vector3(.70f,1.56f,.48f),Quaternion.identity,body,true);Box("locker vent",p,x+new Vector3(0,1.10f,-.25f),new Vector3(.35f,.08f,.03f),Quaternion.identity,metal,false);}
        }
        static void Bench(Transform p,Vector3 q,Material wood,Material metal){Box("bench seat",p,q+Vector3.up*.32f,new Vector3(2.3f,.13f,.55f),Quaternion.identity,wood,true);Box("bench back",p,q+new Vector3(0,.68f,.25f),new Vector3(2.3f,.62f,.10f),Quaternion.identity,wood,true);}
        static void NoticeBoard(Transform p,Vector3 q,Material wood,Material paper){Box("notice board",p,q+Vector3.up*.78f,new Vector3(2.2f,.95f,.07f),Quaternion.identity,wood,false);Box("notice paper",p,q+new Vector3(-.45f,.80f,-.05f),new Vector3(.55f,.55f,.03f),Quaternion.identity,paper,false);Box("notice paper",p,q+new Vector3(.40f,.72f,-.05f),new Vector3(.62f,.40f,.03f),Quaternion.identity,paper,false);}
        static void EquipmentRack(Transform p,Vector3 q,Material metal,Material blue){Box("equipment rack",p,q+Vector3.up*.65f,new Vector3(3.2f,1.3f,.65f),Quaternion.identity,metal,true);for(int i=-2;i<=2;i++) Sphere("ball",p,q+new Vector3(i*.55f,.45f,-.42f),new Vector3(.42f,.42f,.42f),blue);}
        static void Bleachers(Transform p,Vector3 q,Material wood,Material metal){for(int i=0;i<3;i++)Box("bleacher",p,q+new Vector3(i*.55f,.18f+i*.18f,0),new Vector3(.72f,.14f,13f),Quaternion.identity,wood,true);for(int z=-2;z<=2;z++)Box("bleacher support",p,q+new Vector3(.55f,.28f,z*2.5f),new Vector3(1.2f,.55f,.10f),Quaternion.identity,metal,true);}

        static void Swing(Transform p,Vector3 q,Material metal,Material wood){Box("swing posts",p,q+new Vector3(-1.5f,1,0),new Vector3(.12f,2,.12f),Quaternion.identity,metal,true);Box("swing posts",p,q+new Vector3(1.5f,1,0),new Vector3(.12f,2,.12f),Quaternion.identity,metal,true);Box("swing beam",p,q+Vector3.up*2,new Vector3(3.2f,.12f,.12f),Quaternion.identity,metal,true);Box("swing seat",p,q+new Vector3(0,.55f,0),new Vector3(.70f,.10f,.34f),Quaternion.identity,wood,true);}
        static void Picnic(Transform p,Vector3 q,Material wood,Material metal){Box("picnic table",p,q+Vector3.up*.55f,new Vector3(2.6f,.14f,.90f),Quaternion.identity,wood,true);Box("picnic bench",p,q+new Vector3(0,.33f,1),new Vector3(2.6f,.12f,.35f),Quaternion.identity,wood,true);Box("picnic bench",p,q+new Vector3(0,.33f,-1),new Vector3(2.6f,.12f,.35f),Quaternion.identity,wood,true);}
        static void CarFallback(Transform p,Vector3 q,Material m){Box("car",p,q+Vector3.up*.55f,new Vector3(1.8f,1.1f,4.0f),Quaternion.identity,m,true);}
        static void BenchFallback(Transform p,Vector3 q,Material wood,Material metal){Bench(p,q,wood,metal);}
        static void LampFallback(Transform p,Vector3 q,Material metal,Material light){Box("lamp post",p,q+Vector3.up*2,new Vector3(.14f,4,.14f),Quaternion.identity,metal,true);Sphere("lamp head",p,q+new Vector3(0,4,.0f),new Vector3(.45f,.25f,.45f),light);}
        static void TreeFallback(Transform p,Vector3 q,Material trunk,Material canopy){Box("tree trunk",p,q+Vector3.up*.8f,new Vector3(.28f,1.6f,.28f),Quaternion.identity,trunk,true);Sphere("tree crown",p,q+Vector3.up*2.2f,new Vector3(2.2f,1.1f,2.2f),canopy);}
        static void Shrub(Transform p,Vector3 q,Material m){Sphere("shrub",p,q+Vector3.up*.35f,new Vector3(1.2f,.65f,1.2f),m);}
        static void DumpsterFallback(Transform p,Vector3 q,Material m){Box("dumpster",p,q+Vector3.up*.55f,new Vector3(2.0f,1.1f,1.3f),Quaternion.identity,m,true);}

        static void WallH(Transform p,float z,float x1,float x2,Material wall,Material top){float len=x2-x1;if(len<=.05f)return;Vector3 c=O+new Vector3((x1+x2)*.5f,.62f,z);Box("wall",p,c,new Vector3(len,1.24f,.20f),Quaternion.identity,wall,true);Box("wall cap",p,O+new Vector3((x1+x2)*.5f,1.27f,z),new Vector3(len+.04f,.10f,.28f),Quaternion.identity,top,false);}
        static void WallV(Transform p,float x,float z1,float z2,Material wall,Material top){float len=z2-z1;if(len<=.05f)return;Vector3 c=O+new Vector3(x,.62f,(z1+z2)*.5f);Box("wall",p,c,new Vector3(.20f,1.24f,len),Quaternion.identity,wall,true);Box("wall cap",p,O+new Vector3(x,1.27f,(z1+z2)*.5f),new Vector3(.28f,.10f,len+.04f),Quaternion.identity,top,false);}
        static void AddWindowH(Transform p,float z,float x,float width,Material m){Box("window strip",p,O+new Vector3(x,.68f,z-.13f),new Vector3(width,.48f,.05f),Quaternion.identity,m,false);}
        static void AddWindowV(Transform p,float x,float z,float width,Material m){Box("window strip",p,O+new Vector3(x-.13f,.68f,z),new Vector3(.05f,.48f,width),Quaternion.identity,m,false);}
        static void CourtLineH(Transform p,Vector3 q,float w,Material m){Flat("court line",p,q,new Vector3(w,.015f,.10f),m);} static void CourtLineV(Transform p,Vector3 q,float d,Material m){Flat("court line",p,q,new Vector3(.10f,.015f,d),m);}
        static void CenterCircle(Transform p,Vector3 q,float diameter,Material m){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name="center circle";g.transform.SetParent(p,true);g.transform.position=q;g.transform.localScale=new Vector3(diameter,.012f,diameter);g.GetComponent<Renderer>().sharedMaterial=m;Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);GameObject cut=GameObject.CreatePrimitive(PrimitiveType.Cylinder);cut.name="center inset";cut.transform.SetParent(p,true);cut.transform.position=q+Vector3.up*.01f;cut.transform.localScale=new Vector3(diameter*.83f,.014f,diameter*.83f);cut.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(GeneratedDir+"/gym_floor.mat");Collider cc=cut.GetComponent<Collider>();if(cc)UnityEngine.Object.DestroyImmediate(cc);}

        static GameObject InteractionMarker(Transform p,Material m,float size){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name="interaction marker";g.transform.SetParent(p,false);g.transform.localPosition=new Vector3(0,.03f,0);g.transform.localScale=new Vector3(size,.012f,size);g.GetComponent<Renderer>().sharedMaterial=m;Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);g.SetActive(false);return g;}
        static void AddPlayerMarker(Transform player,Material amber,Material dark){GameObject shadow=GameObject.CreatePrimitive(PrimitiveType.Cylinder);shadow.name="PLAYER SHADOW V56";shadow.transform.SetParent(player,false);shadow.transform.localPosition=new Vector3(0,-.13f,0);shadow.transform.localScale=new Vector3(.72f,.012f,.72f);shadow.GetComponent<Renderer>().sharedMaterial=dark;Collider c=shadow.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);GameObject ring=GameObject.CreatePrimitive(PrimitiveType.Cylinder);ring.name="PLAYER RING V56";ring.transform.SetParent(player,false);ring.transform.localPosition=new Vector3(0,-.12f,0);ring.transform.localScale=new Vector3(.50f,.014f,.50f);ring.GetComponent<Renderer>().sharedMaterial=amber;Collider c2=ring.GetComponent<Collider>();if(c2)UnityEngine.Object.DestroyImmediate(c2);}

        static void BorderRect(Transform p,Vector3 c,float w,float d,Material m){Box("curb",p,c+new Vector3(0,.07f,d*.5f),new Vector3(w,.12f,.18f),Quaternion.identity,m,true);Box("curb",p,c-new Vector3(0,-.07f,d*.5f),new Vector3(w,.12f,.18f),Quaternion.identity,m,true);Box("curb",p,c+new Vector3(w*.5f,.07f,0),new Vector3(.18f,.12f,d),Quaternion.identity,m,true);Box("curb",p,c-new Vector3(w*.5f,-.07f,0),new Vector3(.18f,.12f,d),Quaternion.identity,m,true);}
        static void Flat(string name,Transform p,Vector3 pos,Vector3 scale,Material m){Box(name,p,new Vector3(pos.x,scale.y*.5f+.01f,pos.z),scale,Quaternion.identity,m,false);}
        static GameObject Box(string name,Transform p,Vector3 pos,Vector3 scale,Quaternion rot,Material m,bool collider){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(p,true);g.transform.position=pos;g.transform.rotation=rot;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=m;if(!collider){Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);}return g;}
        static void Sphere(string name,Transform p,Vector3 pos,Vector3 scale,Material m){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=name;g.transform.SetParent(p,true);g.transform.position=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=m;Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);}
        static Transform Group(Transform p,string name){GameObject g=new GameObject(name);g.transform.SetParent(p,true);return g.transform;}

        static void GroundText(Transform p,string text,Vector3 pos,float size,Color color){GameObject g=new GameObject("ground text "+text);g.transform.SetParent(p,true);g.transform.position=pos;g.transform.rotation=Quaternion.Euler(90,0,0);TextMesh t=g.AddComponent<TextMesh>();t.text=text;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.fontSize=64;t.characterSize=size;t.color=color;}

        static GameObject FindModel(params string[] names){foreach(string n in names){string[] ids=AssetDatabase.FindAssets(n+" t:GameObject");foreach(string id in ids){string path=AssetDatabase.GUIDToAssetPath(id);GameObject go=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(go)return go;}}return null;}
        static GameObject PlaceModel(GameObject prefab,string name,Transform parent,Vector3 pos,Vector3 forward,float targetHeight){if(!prefab)return null;GameObject go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);if(!go)go=UnityEngine.Object.Instantiate(prefab);go.name=name;go.transform.SetParent(parent,true);go.transform.position=pos;Vector3 f=new Vector3(forward.x,0,forward.z);if(f.sqrMagnitude<.01f)f=Vector3.forward;go.transform.rotation=Quaternion.LookRotation(f.normalized,Vector3.up);Bounds b=RenderBounds(go);float h=Mathf.Max(.01f,b.size.y);go.transform.localScale*=targetHeight/h;b=RenderBounds(go);go.transform.position+=Vector3.up*(pos.y-b.min.y);return go;}
        static Bounds RenderBounds(GameObject g){Renderer[] rs=g.GetComponentsInChildren<Renderer>(true);if(rs.Length==0)return new Bounds(g.transform.position,Vector3.one);Bounds b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;}

        static Material Mat(string name,Color color,Shader shader){string path=GeneratedDir+"/"+name+".mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(shader){name="V56 "+name};AssetDatabase.CreateAsset(m,path);}if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color);if(m.HasProperty("_Color"))m.SetColor("_Color",color);EditorUtility.SetDirty(m);return m;}
        static Shader FindGoodShader(UScene scene){foreach(GameObject r in scene.GetRootGameObjects())foreach(Renderer rr in r.GetComponentsInChildren<Renderer>(true))foreach(Material m in rr.sharedMaterials){if(!m||!m.shader||!m.shader.isSupported)continue;string n=m.shader.name??"";if(n.StartsWith("Hidden/")||n.IndexOf("InternalError",StringComparison.OrdinalIgnoreCase)>=0)continue;return m.shader;}Shader u=Shader.Find("Universal Render Pipeline/Lit");if(u&&u.isSupported)return u;Shader s=Shader.Find("Standard");return s&&s.isSupported?s:null;}

        static void DisableOldVisualPasses(UScene scene){string[] names={"32 TOPDOWN GAMEPLAY V51 - PROTOTYPE","33 TOPDOWN GAMEPLAY V52 - FIRST LOOP","34 TOPDOWN V53 - SCHOOL INTERIOR SLICE","35 TOPDOWN V54 - CLEAN VISUAL CAMPUS","36 TOPDOWN V55 - FULL SCHOOL OVERHAUL","31 SCHOOL CAMPUS V49 - TOP LEVEL VISIBLE REBUILD","30 SCHOOL CAMPUS V48 - FINAL EXTERIOR REBUILD","29 SCHOOL CAMPUS V47 - ENTRANCE ANCHORED","28 SCHOOL CAMPUS V46 - COZY DENSITY PASS","27 SCHOOL CAMPUS V45 - VISIBLE REBUILD"};foreach(string n in names){Transform t=Resources.FindObjectsOfTypeAll<Transform>().FirstOrDefault(x=>x&&x.gameObject.scene==scene&&x.name==n);if(t)t.gameObject.SetActive(false);}}
        static void DisableOldGameplay(Transform player){foreach(MonoBehaviour mb in player.GetComponents<MonoBehaviour>()){if(!mb)continue;string n=mb.GetType().Name;if(n.StartsWith("TopDownGameplayV")&&n!="TopDownGameplayV56")mb.enabled=false;}}
        static void RemoveOldPlayerMarkers(Transform player){List<Transform> kill=new List<Transform>();foreach(Transform c in player)if(c.name.Contains("MARKER V52")||c.name.Contains("PLAYER RING")||c.name.Contains("PLAYER SHADOW"))kill.Add(c);foreach(Transform t in kill)UnityEngine.Object.DestroyImmediate(t.gameObject);}
        static void RemoveTopLevel(UScene scene,string name){GameObject g=scene.GetRootGameObjects().FirstOrDefault(x=>x.name==name);if(g)UnityEngine.Object.DestroyImmediate(g);}
        static Transform FindSceneTransform(UScene scene,string name){return Resources.FindObjectsOfTypeAll<Transform>().FirstOrDefault(t=>t&&t.gameObject.scene==scene&&t.name==name);}

        static void SetupCamera(UScene scene,Transform player){Camera cam=Resources.FindObjectsOfTypeAll<Camera>().FirstOrDefault(c=>c&&c.gameObject.scene==scene&&c.CompareTag("MainCamera"));if(!cam)cam=Resources.FindObjectsOfTypeAll<Camera>().FirstOrDefault(c=>c&&c.gameObject.scene==scene);if(!cam)return;foreach(MonoBehaviour mb in cam.GetComponents<MonoBehaviour>()){if(mb&&mb.GetType().Name=="IsometricCameraRig")mb.enabled=false;}TopDownCameraRigV51 rig=cam.GetComponent<TopDownCameraRigV51>();if(!rig)rig=cam.gameObject.AddComponent<TopDownCameraRigV51>();rig.enabled=true;rig.target=player;rig.pitch=89.25f;rig.yaw=0;rig.distance=34f;rig.orthographicSize=9.2f;rig.minSize=6.6f;rig.maxSize=14f;rig.followSharpness=16f;rig.focusOffset=Vector3.zero;cam.orthographic=true;cam.orthographicSize=9.2f;cam.backgroundColor=new Color(.055f,.065f,.064f);EditorUtility.SetDirty(rig);EditorUtility.SetDirty(cam);}
        static void SetupLighting(UScene scene){RenderSettings.fog=false;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.57f,.57f,.50f);Light sun=Resources.FindObjectsOfTypeAll<Light>().FirstOrDefault(l=>l&&l.gameObject.scene==scene&&l.type==LightType.Directional);if(sun){sun.intensity=.78f;sun.color=new Color(.96f,.91f,.80f);sun.shadows=LightShadows.Soft;sun.shadowStrength=.26f;sun.transform.rotation=Quaternion.Euler(52,-32,0);}QualitySettings.shadowDistance=55f;}
    }
}
#endif
