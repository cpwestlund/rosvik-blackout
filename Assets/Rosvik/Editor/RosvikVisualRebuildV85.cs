#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Rosvik.Blackout;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikVisualRebuildV85 {
        const int Version = 85;
        const string Key = "ROSVIK_VISUAL_REBUILD_V85";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string RootName = "V85 VISUAL REBUILD - HOUSE A GOLD STANDARD";
        const string Generated = "Assets/Rosvik/GeneratedV85";

        static RosvikVisualRebuildV85() {
            if (EditorPrefs.GetInt(Key, 0) >= Version) return;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V85 VISUAL REBUILD - HOUSE A GOLD STANDARD")]
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
            catch (Exception ex) { Debug.LogError("V85 VISUAL REBUILD FAILED: " + ex); }
        }

        static void Apply() {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            CoziPlayerV57 player = UnityEngine.Object.FindFirstObjectByType<CoziPlayerV57>();
            if (!player) throw new Exception("PLAYER missing");

            GameObject oldV85 = GameObject.Find(RootName);
            if (oldV85) UnityEngine.Object.DestroyImmediate(oldV85);

            // V84 was a transitional visual layer. Remove it entirely; gameplay lives in older system roots.
            GameObject v84 = GameObject.Find("V84 GRAPHICAL OVERHAUL");
            if (v84) UnityEngine.Object.DestroyImmediate(v84);

            HideLegacyHouseARenderers();
            Directory.CreateDirectory(Generated);

            Shader shader = PickShader();
            Material snow = PatternMat(shader,"snow","d7e0e1","b7c6ca",Pattern.Noise,.08f,new Vector2(3,3));
            Material packedSnow = PatternMat(shader,"packed_snow","9aaeb4","718a92",Pattern.Noise,.05f,new Vector2(4,4));
            Material siding = PatternMat(shader,"painted_red_siding","70413d","4b2e2c",Pattern.VerticalBoards,.09f,new Vector2(5,2));
            Material roof = PatternMat(shader,"matte_metal_roof","313b3e","20282b",Pattern.RoofSeams,.12f,new Vector2(4,4));
            Material trim = FlatMat(shader,"warm_white_trim","d8d2c2",.16f);
            Material foundation = PatternMat(shader,"foundation","4a5556","343e3f",Pattern.Noise,.07f,new Vector2(3,3));
            Material woodFloor = PatternMat(shader,"pine_floor","896c4d","684d36",Pattern.WoodPlanks,.16f,new Vector2(5,3));
            Material kitchenFloor = PatternMat(shader,"kitchen_floor","68746c","4e5a54",Pattern.Tile,.11f,new Vector2(5,5));
            Material plaster = PatternMat(shader,"interior_plaster","b8b3a2","9c998d",Pattern.Noise,.12f,new Vector2(3,3));
            Material sage = PatternMat(shader,"sage_wall","68796e","53645b",Pattern.Noise,.11f,new Vector2(3,3));
            Material darkWood = PatternMat(shader,"dark_wood","5b4335","3f3028",Pattern.WoodPlanks,.13f,new Vector2(3,3));
            Material warmWood = PatternMat(shader,"warm_wood","8c6a4d","624b39",Pattern.WoodPlanks,.15f,new Vector2(4,3));
            Material metal = FlatMat(shader,"dark_metal","465255",.34f);
            Material white = FlatMat(shader,"cabinet_cream","d3d0c4",.20f);
            Material glass = FlatMat(shader,"window_glass","40585e",.42f);
            Material fabric = FlatMat(shader,"muted_fabric","73635c",.18f);
            Material rug = PatternMat(shader,"rug","53665f","354a45",Pattern.Rug,.12f,new Vector2(2,3));
            Material black = FlatMat(shader,"rubber","252b2c",.04f);
            Material ceramic = FlatMat(shader,"ceramic","d8ccb5",.26f);

            GameObject root = new GameObject(RootName);
            Transform ground = Group(root.transform,"GROUND + APPROACH");
            Transform exterior = Group(root.transform,"EXTERIOR SHELL");
            Transform outsideDetails = Group(root.transform,"EXTERIOR DETAILS");
            Transform interior = Group(root.transform,"INTERIOR VISUALS");

            BuildGround(ground,snow,packedSnow,darkWood,metal);
            BuildExterior(exterior,outsideDetails,siding,roof,snow,trim,foundation,glass,darkWood,metal);
            BuildInterior(interior,woodFloor,kitchenFloor,plaster,sage,trim,darkWood,warmWood,white,metal,glass,fabric,rug,black,ceramic);
            RebuildInteractiveSkins(interior,darkWood,warmWood,white,metal,glass);
            RebuildFrontDoor(trim,darkWood,glass,metal);
            AddInteriorLighting(interior);

            HouseVisualRebuildV85 mode = root.AddComponent<HouseVisualRebuildV85>();
            mode.exteriorShell = exterior.gameObject;
            mode.exteriorDetails = outsideDetails.gameObject;
            mode.interiorVisuals = interior.gameObject;
            mode.minXZ = new Vector2(-36.25f,2.25f);
            mode.maxXZ = new Vector2(-21.75f,15.80f);
            mode.enterPadding = .22f;
            mode.exitPadding = .52f;
            EditorUtility.SetDirty(mode);

            interior.gameObject.SetActive(false);
            TuneScene();
            CleanupMissingScripts();

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorPrefs.SetInt(Key, Version);
            SceneView.RepaintAll();
            Debug.Log("V85 COMPLETE — House A graphics rebuilt from zero while inventory, loot, survival, doors, rest and progression systems remain intact.");
        }

        static void HideLegacyHouseARenderers() {
            GameObject legacy = GameObject.Find("WORLD EXPANSION V75 - HOUSE A");
            if (!legacy) return;
            foreach (Renderer r in legacy.GetComponentsInChildren<Renderer>(true)) {
                if (!r) continue;
                r.enabled = false;
                EditorUtility.SetDirty(r);
            }
            foreach (Light l in legacy.GetComponentsInChildren<Light>(true)) if (l) l.enabled = false;
        }

        static void BuildGround(Transform p,Material snow,Material packed,Material wood,Material metal) {
            Box("clean snow lot",p,new Vector3(-29f,-.055f,9.0f),new Vector3(19.0f,.08f,18.5f),snow,false);
            for (int i=0;i<12;i++) {
                float z=1.0f+i*.42f;
                float x=-29f+Mathf.Sin(i*.73f)*.08f;
                Box("packed entrance snow",p,new Vector3(x,.004f,z),new Vector3(1.55f,.025f,.46f),packed,false,Quaternion.Euler(0,Mathf.Sin(i*.5f)*2f,0));
            }
            // Deliberate snow banks: long, low and irregular rather than white circles.
            for (int i=0;i<5;i++) {
                float x=-35.3f+i*3.15f;
                Box("ploughed snow",p,new Vector3(x,.08f,1.55f+Mathf.Sin(i)*.14f),new Vector3(2.45f,.20f,.55f),snow,false,Quaternion.Euler(0,(i%2==0?4f:-5f),0));
            }
            Box("mailbox post",p,new Vector3(-25.05f,.62f,.65f),new Vector3(.10f,1.20f,.10f),wood,false);
            Box("mailbox body",p,new Vector3(-25.05f,1.14f,.65f),new Vector3(.52f,.30f,.38f),metal,false);
            Box("mailbox lid",p,new Vector3(-25.05f,1.31f,.62f),new Vector3(.56f,.07f,.42f),snow,false,Quaternion.Euler(0,0,-2f));
        }

        static void BuildExterior(Transform shell,Transform details,Material siding,Material roof,Material snow,Material trim,Material foundation,Material glass,Material wood,Material metal) {
            const float minX=-36f,maxX=-22f,front=2.4f,back=15.6f;
            float h=2.48f;
            // Proper opaque exterior envelope. Door opening is physically readable from the outside.
            WallX(shell,minX,-30.02f,front,h,siding);
            WallX(shell,-27.98f,maxX,front,h,siding);
            WallX(shell,minX,maxX,back,h,siding);
            WallZ(shell,minX,front,back,h,siding);
            WallZ(shell,maxX,front,back,h,siding);
            Box("foundation south",shell,new Vector3(-29f,.27f,front-.09f),new Vector3(14.2f,.46f,.18f),foundation,false);
            Box("foundation north",shell,new Vector3(-29f,.27f,back+.09f),new Vector3(14.2f,.46f,.18f),foundation,false);
            Box("foundation west",shell,new Vector3(minX-.09f,.27f,9f),new Vector3(.18f,.46f,13.4f),foundation,false);
            Box("foundation east",shell,new Vector3(maxX+.09f,.27f,9f),new Vector3(.18f,.46f,13.4f),foundation,false);

            Window(shell,new Vector3(-33.55f,1.38f,front-.105f),2.30f,0,trim,glass);
            Window(shell,new Vector3(-24.70f,1.38f,front-.105f),2.05f,0,trim,glass);
            Window(shell,new Vector3(-35.90f,1.42f,6.5f),2.15f,90,trim,glass);
            Window(shell,new Vector3(-22.10f,1.42f,12.2f),2.00f,90,trim,glass);
            Window(shell,new Vector3(-33.2f,1.42f,back+.105f),2.20f,0,trim,glass);
            Window(shell,new Vector3(-24.6f,1.42f,back+.105f),2.00f,0,trim,glass);

            BuildRoof(shell,new Vector3(-29f,0,9f),14f,13.2f,roof,snow,metal);
            BuildPorch(details,new Vector3(-29f,0,front),trim,wood,metal,snow);

            // Corner boards give the facade believable scale.
            foreach (Vector3 q in new[]{new Vector3(minX-.02f,1.28f,front-.05f),new Vector3(maxX+.02f,1.28f,front-.05f),new Vector3(minX-.02f,1.28f,back+.05f),new Vector3(maxX+.02f,1.28f,back+.05f)})
                Box("corner board",shell,q,new Vector3(.16f,2.55f,.16f),trim,false);
        }

        static void BuildRoof(Transform p,Vector3 c,float width,float depth,Material roof,Material snow,Material edge) {
            const float pitch=15f, eave=2.55f, overhang=.34f;
            float w=width+overhang*2f,d=depth+overhang*2f,half=w*.5f,rad=pitch*Mathf.Deg2Rad;
            float rise=half*Mathf.Tan(rad), ridge=eave+rise, len=half/Mathf.Cos(rad), xo=half*.5f, py=eave+rise*.5f;
            GameObject l=Box("west roof",p,c+new Vector3(-xo,py,0),new Vector3(len+.08f,.18f,d),roof,false,Quaternion.Euler(0,0,pitch));
            GameObject r=Box("east roof",p,c+new Vector3(xo,py,0),new Vector3(len+.08f,.18f,d),roof,false,Quaternion.Euler(0,0,-pitch));
            Box("snow west",p,c+new Vector3(-xo+.02f,py+.11f,0),new Vector3(len-.12f,.045f,d-.22f),snow,false,l.transform.rotation);
            Box("snow east",p,c+new Vector3(xo-.02f,py+.11f,0),new Vector3(len-.12f,.045f,d-.22f),snow,false,r.transform.rotation);
            Box("ridge cap",p,c+new Vector3(0,ridge+.07f,0),new Vector3(.18f,.12f,d+.03f),edge,false);
            Vector3 chimney=c+new Vector3(-2.0f,ridge+.38f,2.15f);
            Box("chimney",p,chimney,new Vector3(.58f,.88f,.58f),edge,false);
            Box("chimney snow",p,chimney+Vector3.up*.48f,new Vector3(.70f,.10f,.70f),snow,false);
        }

        static void BuildPorch(Transform p,Vector3 door,Material trim,Material wood,Material metal,Material snow) {
            Box("porch platform",p,door+new Vector3(0,.12f,-.95f),new Vector3(2.70f,.24f,1.70f),wood,false);
            Box("porch step",p,door+new Vector3(0,.065f,-1.73f),new Vector3(2.15f,.13f,.52f),wood,false);
            GameObject awning=Box("small entrance roof",p,door+new Vector3(0,2.20f,-.70f),new Vector3(2.85f,.13f,1.48f),metal,false,Quaternion.Euler(7f,0,0));
            Box("awning snow",p,awning.transform.position+new Vector3(0,.105f,0),new Vector3(2.65f,.04f,1.25f),snow,false,awning.transform.rotation);
            Box("awning post L",p,door+new Vector3(-1.15f,1.16f,-.87f),new Vector3(.10f,2.05f,.10f),trim,false);
            Box("awning post R",p,door+new Vector3(1.15f,1.16f,-.87f),new Vector3(.10f,2.05f,.10f),trim,false);
        }

        static void BuildInterior(Transform p,Material woodFloor,Material kitchenFloor,Material plaster,Material sage,Material trim,Material darkWood,Material warmWood,Material white,Material metal,Material glass,Material fabric,Material rug,Material black,Material ceramic) {
            const float minX=-36f,maxX=-22f,front=2.4f,back=15.6f;
            // Four visually distinct rooms with coherent floor direction.
            Box("living pine floor",p,new Vector3(-32f,.025f,6.0f),new Vector3(8.0f,.07f,6.85f),woodFloor,false);
            Box("entry slate floor",p,new Vector3(-24.9f,.025f,6.0f),new Vector3(6.15f,.07f,6.85f),kitchenFloor,false);
            Box("kitchen painted floor",p,new Vector3(-32f,.025f,12.25f),new Vector3(8.0f,.07f,5.0f),kitchenFloor,false);
            Box("bedroom pine floor",p,new Vector3(-24.9f,.025f,12.25f),new Vector3(6.15f,.07f,5.0f),woodFloor,false);

            // Low cutaway walls: readable rooms without hiding gameplay.
            LowWallX(p,minX,-30.05f,front,.92f,sage,trim); LowWallX(p,-27.95f,maxX,front,.92f,sage,trim);
            LowWallX(p,minX,maxX,back,1.02f,plaster,trim); LowWallZ(p,minX,front,back,1.02f,plaster,trim); LowWallZ(p,maxX,front,back,1.02f,plaster,trim);
            LowWallX(p,minX,-33.0f,9.25f,.90f,sage,trim); LowWallX(p,-31.6f,-26.0f,9.25f,.90f,sage,trim); LowWallX(p,-24.6f,maxX,9.25f,.90f,sage,trim);
            LowWallZ(p,-28.0f,9.25f,11.1f,.90f,plaster,trim); LowWallZ(p,-28.0f,12.6f,back,.90f,plaster,trim);

            // Living room: a deliberate conversation group, not random furniture.
            PlaceAsset("rug_rectangle_stripes_A","living rug V85",p,new Vector3(-32.75f,.06f,5.85f),0,1.12f);
            PlaceAsset("couch_pillows","living sofa V85",p,new Vector3(-34.35f,.06f,6.05f),90,.88f);
            PlaceAsset("armchair_pillows","living chair V85",p,new Vector3(-31.35f,.06f,6.55f),-90,.82f);
            PlaceAsset("table_low","coffee table V85",p,new Vector3(-32.75f,.06f,5.85f),0,.90f);
            PlaceAsset("lamp_standing","living lamp V85",p,new Vector3(-35.05f,.06f,7.55f),0,.86f);
            PlaceAsset("shelf_B_small_decorated","living bookcase V85",p,new Vector3(-35.15f,.06f,3.65f),0,.72f);
            PlaceAsset("pictureframe_medium","living photo V85",p,new Vector3(-35.72f,1.12f,7.20f),90,.60f);
            Box("folded throw",p,new Vector3(-34.10f,.62f,6.10f),new Vector3(.72f,.06f,.42f),fabric,false,Quaternion.Euler(0,13f,0));

            // Entry: bench, runner, boots and hooks.
            Box("entry runner",p,new Vector3(-29f,.055f,4.1f),new Vector3(1.55f,.025f,2.20f),rug,false);
            Box("entry bench",p,new Vector3(-26.75f,.34f,3.55f),new Vector3(1.55f,.12f,.46f),warmWood,false);
            Box("entry bench leg L",p,new Vector3(-27.30f,.17f,3.55f),new Vector3(.10f,.34f,.36f),darkWood,false);
            Box("entry bench leg R",p,new Vector3(-26.20f,.17f,3.55f),new Vector3(.10f,.34f,.36f),darkWood,false);
            Boot(p,new Vector3(-28.50f,.12f,3.20f),black,-7); Boot(p,new Vector3(-28.15f,.12f,3.28f),black,8);
            for(int i=0;i<4;i++) Sphere("coat hook",p,new Vector3(-27.35f+i*.35f,1.10f,2.62f),.055f,metal);

            // Kitchen: continuous run, sink and stove, plus table/chairs.
            PlaceAsset("table_medium","kitchen table V85",p,new Vector3(-32.0f,.06f,11.35f),0,.82f);
            PlaceAsset("chair_A","kitchen chair W",p,new Vector3(-33.1f,.06f,11.35f),90,.76f);
            PlaceAsset("chair_A","kitchen chair E",p,new Vector3(-30.9f,.06f,11.35f),-90,.76f);
            for(int i=0;i<4;i++) {
                float x=-34.9f+i*1.18f;
                Box("kitchen base cabinet",p,new Vector3(x,.46f,14.72f),new Vector3(1.08f,.88f,.66f),white,false);
                Box("countertop",p,new Vector3(x,.94f,14.72f),new Vector3(1.13f,.08f,.72f),warmWood,false);
            }
            Box("sink basin",p,new Vector3(-33.72f,.995f,14.70f),new Vector3(.72f,.055f,.46f),metal,false);
            Box("stove top",p,new Vector3(-31.36f,.995f,14.70f),new Vector3(.72f,.055f,.50f),black,false);
            for(int i=0;i<4;i++) Sphere("stove ring",p,new Vector3(-31.58f+(i%2)*.44f,1.035f,14.55f+(i/2)*.28f),.11f,metal,.022f);
            Mug(p,new Vector3(-34.85f,1.04f,14.55f),ceramic); Mug(p,new Vector3(-34.55f,1.04f,14.62f),ceramic);

            // Bedroom: bed is visually rebuilt but old RestSpot system remains invisibly underneath.
            Box("bed frame V85",p,new Vector3(-24.5f,.21f,13.20f),new Vector3(1.18f,.28f,2.18f),darkWood,false);
            Box("mattress V85",p,new Vector3(-24.5f,.43f,13.20f),new Vector3(1.10f,.20f,2.04f),white,false);
            Box("duvet V85",p,new Vector3(-24.5f,.57f,13.48f),new Vector3(1.05f,.08f,1.25f),fabric,false);
            Box("pillow V85",p,new Vector3(-24.5f,.57f,12.45f),new Vector3(.72f,.10f,.38f),white,false);
            PlaceAsset("table_small","bedside table V85",p,new Vector3(-26.0f,.06f,14.25f),0,.70f);
            PlaceAsset("lamp_table","bed lamp V85",p,new Vector3(-26.0f,.55f,14.25f),0,.68f);
            PlaceAsset("pictureframe_medium","bedroom picture V85",p,new Vector3(-24.70f,1.15f,15.42f),0,.64f);

            // Utility side.
            PlaceAsset("shelf_A_big","utility shelf V85",p,new Vector3(-22.8f,.06f,7.75f),90,.70f);
            Box("washing machine",p,new Vector3(-24.15f,.48f,7.75f),new Vector3(.82f,.92f,.78f),white,false);
            Sphere("washer door",p,new Vector3(-24.15f,.52f,7.34f),.27f,glass,.08f);
            Box("laundry basket",p,new Vector3(-25.35f,.30f,7.85f),new Vector3(.72f,.58f,.58f),warmWood,false);
        }

        static void RebuildFrontDoor(Material trim,Material wood,Material glass,Material metal) {
            GameObject root=GameObject.Find("door — husets ytterdörr");
            if(!root) return;
            HouseFrontDoorV763 ctl=root.GetComponent<HouseFrontDoorV763>();
            Transform hinge=ctl&&ctl.hinge?ctl.hinge:root.transform.Find("door hinge");
            if(!hinge) return;
            foreach(Renderer r in root.GetComponentsInChildren<Renderer>(true)) if(r) r.enabled=false;
            Transform old=hinge.Find("V85 DOOR VISUAL"); if(old) UnityEngine.Object.DestroyImmediate(old.gameObject);
            Transform visual=Group(hinge,"V85 DOOR VISUAL");
            GameObject leaf=BoxLocal("painted front door",visual,new Vector3(.84f,.98f,0),new Vector3(1.66f,1.92f,.11f),wood);
            BoxLocal("door inset",visual,new Vector3(.84f,.98f,-.061f),new Vector3(1.34f,1.58f,.025f),trim);
            BoxLocal("door glass",visual,new Vector3(.84f,1.40f,-.078f),new Vector3(.76f,.55f,.02f),glass);
            SphereLocal("door handle",visual,new Vector3(1.48f,.94f,-.10f),.065f,metal);
            if(ctl){ctl.closedEuler=Vector3.zero;ctl.openEuler=new Vector3(0,92f,0);ctl.animationTime=.30f;ctl.interactionDistance=3.4f;hinge.localRotation=Quaternion.identity;EditorUtility.SetDirty(ctl);}
            leaf.GetComponent<Renderer>().enabled=true;
            foreach(Renderer r in visual.GetComponentsInChildren<Renderer>(true)) r.enabled=true;
        }

        static void RebuildInteractiveSkins(Transform p,Material darkWood,Material warmWood,Material white,Material metal,Material glass) {
            SkinDouble("skafferiet",warmWood,darkWood,metal);
            SkinSingle("kylskåpet",new Vector3(1.0f,1.85f,.72f),white,metal);
            SkinDouble("sovrummets garderob",darkWood,warmWood,metal);
            SkinSingle("medicinskåpet",new Vector3(.78f,.72f,.32f),white,metal);
            SkinCrate("verktygslådan",darkWood,metal);
        }

        static LootContainerV74 FindLoot(string name) {
            return UnityEngine.Object.FindObjectsByType<LootContainerV74>(FindObjectsInactive.Include,FindObjectsSortMode.None)
                .FirstOrDefault(x=>x&&string.Equals(x.displayName,name,StringComparison.OrdinalIgnoreCase));
        }

        static void SkinDouble(string name,Material body,Material door,Material metal) {
            LootContainerV74 c=FindLoot(name); if(!c) return;
            Transform old=c.transform.Find("V85 VISUAL"); if(old) UnityEngine.Object.DestroyImmediate(old.gameObject);
            Transform v=Group(c.transform,"V85 VISUAL"); float w=1.35f,h=1.55f,d=.58f;
            BoxLocal("cabinet back",v,new Vector3(0,h*.5f,d*.40f),new Vector3(w,h,.08f),body);
            BoxLocal("cabinet left",v,new Vector3(-w*.48f,h*.5f,0),new Vector3(.08f,h,d),body);
            BoxLocal("cabinet right",v,new Vector3(w*.48f,h*.5f,0),new Vector3(.08f,h,d),body);
            BoxLocal("cabinet top",v,new Vector3(0,h,d*.02f),new Vector3(w,.09f,d),body);
            Renderer hi=null;
            if(c.movingPart){Transform g=Group(c.movingPart,"V85 LEFT DOOR");GameObject leaf=BoxLocal("door",g,new Vector3(w*.24f,h*.5f,0),new Vector3(w*.47f,h*.91f,.075f),door);SphereLocal("handle",g,new Vector3(w*.40f,h*.52f,-.08f),.07f,metal);hi=leaf.GetComponent<Renderer>();}
            if(c.movingPart2){Transform g=Group(c.movingPart2,"V85 RIGHT DOOR");BoxLocal("door",g,new Vector3(-w*.24f,h*.5f,0),new Vector3(w*.47f,h*.91f,.075f),door);SphereLocal("handle",g,new Vector3(-w*.40f,h*.52f,-.08f),.07f,metal);}
            if(hi){c.highlightRenderer=hi;c.RefreshHighlight();EditorUtility.SetDirty(c);}
        }

        static void SkinSingle(string name,Vector3 size,Material body,Material metal) {
            LootContainerV74 c=FindLoot(name); if(!c) return;
            Transform old=c.transform.Find("V85 VISUAL"); if(old) UnityEngine.Object.DestroyImmediate(old.gameObject);
            Transform v=Group(c.transform,"V85 VISUAL");
            BoxLocal("cabinet body",v,new Vector3(0,size.y*.5f,.05f),size,body);
            if(c.movingPart){Transform g=Group(c.movingPart,"V85 DOOR");GameObject leaf=BoxLocal("door",g,new Vector3(size.x*.5f,size.y*.5f,0),new Vector3(size.x*.98f,size.y*.94f,.075f),body);SphereLocal("handle",g,new Vector3(size.x*.88f,size.y*.52f,-.08f),.065f,metal);c.highlightRenderer=leaf.GetComponent<Renderer>();c.RefreshHighlight();EditorUtility.SetDirty(c);}
        }

        static void SkinCrate(string name,Material wood,Material metal) {
            LootContainerV74 c=FindLoot(name); if(!c) return;
            Transform old=c.transform.Find("V85 VISUAL"); if(old) UnityEngine.Object.DestroyImmediate(old.gameObject);
            Transform v=Group(c.transform,"V85 VISUAL");
            BoxLocal("toolbox shell",v,new Vector3(0,.28f,0),new Vector3(1.10f,.50f,.72f),wood);
            if(c.movingPart){Transform g=Group(c.movingPart,"V85 LID");GameObject lid=BoxLocal("toolbox lid",g,new Vector3(0,.03f,-.34f),new Vector3(1.10f,.10f,.74f),metal);c.highlightRenderer=lid.GetComponent<Renderer>();c.RefreshHighlight();EditorUtility.SetDirty(c);}
        }

        static void AddInteriorLighting(Transform p) {
            PointLight("living warm pool",p,new Vector3(-33.0f,2.15f,6.1f),new Color(1f,.72f,.45f),1.15f,5.2f);
            PointLight("kitchen warm pool",p,new Vector3(-32.5f,2.12f,12.3f),new Color(1f,.76f,.52f),1.00f,4.6f);
            PointLight("bedroom dim pool",p,new Vector3(-25.2f,1.72f,13.6f),new Color(1f,.68f,.43f),.75f,3.8f);
        }

        static void TuneScene() {
            RenderSettings.ambientMode=AmbientMode.Flat;
            RenderSettings.ambientLight=new Color(.30f,.36f,.38f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.0024f;RenderSettings.fogColor=new Color(.22f,.30f,.34f);
            foreach(Light l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
                if(!l||l.type!=LightType.Directional)continue;l.intensity=.62f;l.color=new Color(.79f,.86f,.91f);l.shadows=LightShadows.Soft;EditorUtility.SetDirty(l);
            }
        }

        enum Pattern { Noise, VerticalBoards, WoodPlanks, Tile, RoofSeams, Rug }

        static Material PatternMat(Shader shader,string name,string baseHex,string detailHex,Pattern pattern,float smooth,Vector2 tiling) {
            Color a=Hex(baseHex),b=Hex(detailHex);Texture2D tex=PatternTexture(name,a,b,pattern);
            Material m=FlatMat(shader,name,baseHex,smooth);
            if(m.HasProperty("_BaseMap")){m.SetTexture("_BaseMap",tex);m.SetTextureScale("_BaseMap",tiling);}
            else if(m.HasProperty("_MainTex")){m.SetTexture("_MainTex",tex);m.SetTextureScale("_MainTex",tiling);}
            EditorUtility.SetDirty(m);return m;
        }

        static Texture2D PatternTexture(string name,Color a,Color b,Pattern pattern) {
            string path=Generated+"/tex_"+name+".asset";Texture2D t=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(!t){t=new Texture2D(128,128,TextureFormat.RGBA32,false,true);t.name="V85 "+name;AssetDatabase.CreateAsset(t,path);}
            t.wrapMode=TextureWrapMode.Repeat;t.filterMode=FilterMode.Bilinear;
            for(int y=0;y<128;y++) for(int x=0;x<128;x++) {
                float n=(Mathf.PerlinNoise(x*.075f,y*.075f)-.5f)*.10f;Color c=a*(1f+n);
                bool line=false;
                switch(pattern){
                    case Pattern.VerticalBoards: line=(x%24<2); break;
                    case Pattern.WoodPlanks: line=(y%28<2)||((x+(y/28%2)*31)%64<1); break;
                    case Pattern.Tile: line=(x%32<2)||(y%32<2); break;
                    case Pattern.RoofSeams: line=(x%30<2); break;
                    case Pattern.Rug: line=((x/10+y/16)%3==0&&x%10<2); break;
                }
                if(line)c=Color.Lerp(c,b,.72f);else if(pattern==Pattern.Noise)c=Color.Lerp(c,b,Mathf.Clamp01((n+.05f)*2.5f));
                t.SetPixel(x,y,c);
            }
            t.Apply(false,false);EditorUtility.SetDirty(t);return t;
        }

        static Material FlatMat(Shader shader,string name,string hex,float smooth) {
            string path=Generated+"/mat_"+name+".mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(path);Color c=Hex(hex);
            if(!m){m=new Material(shader);m.name="V85 "+name;AssetDatabase.CreateAsset(m,path);} if(m.shader!=shader)m.shader=shader;
            if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);if(m.HasProperty("_Color"))m.SetColor("_Color",c);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;
        }

        static Shader PickShader(){bool srp=GraphicsSettings.currentRenderPipeline!=null||GraphicsSettings.defaultRenderPipeline!=null;Shader s=srp?Shader.Find("Universal Render Pipeline/Lit"):Shader.Find("Standard");if(!s||!s.isSupported)s=Shader.Find("Universal Render Pipeline/Simple Lit");if(!s||!s.isSupported)s=Shader.Find("Standard");if(!s||!s.isSupported)throw new Exception("No supported shader");return s;}
        static Color Hex(string h){Color c=Color.white;ColorUtility.TryParseHtmlString("#"+h,out c);return c;}

        static void Window(Transform p,Vector3 pos,float width,float yaw,Material trim,Material glass){GameObject g=Group(p,"window").gameObject;g.transform.position=pos;g.transform.rotation=Quaternion.Euler(0,yaw,0);BoxLocal("glass",g.transform,Vector3.zero,new Vector3(width,.92f,.05f),glass);BoxLocal("top",g.transform,new Vector3(0,.52f,-.02f),new Vector3(width+.20f,.10f,.12f),trim);BoxLocal("bottom",g.transform,new Vector3(0,-.52f,-.02f),new Vector3(width+.20f,.10f,.12f),trim);BoxLocal("left",g.transform,new Vector3(-width*.5f-.05f,0,-.02f),new Vector3(.10f,1.12f,.12f),trim);BoxLocal("right",g.transform,new Vector3(width*.5f+.05f,0,-.02f),new Vector3(.10f,1.12f,.12f),trim);BoxLocal("mullion",g.transform,new Vector3(0,0,-.04f),new Vector3(.08f,1.02f,.10f),trim);}
        static void WallX(Transform p,float a,float b,float z,float h,Material m){Box("wall",p,new Vector3((a+b)*.5f,h*.5f,z),new Vector3(b-a,h,.16f),m,false);}
        static void WallZ(Transform p,float x,float a,float b,float h,Material m){Box("wall",p,new Vector3(x,h*.5f,(a+b)*.5f),new Vector3(.16f,h,b-a),m,false);}
        static void LowWallX(Transform p,float a,float b,float z,float h,Material wall,Material trim){WallX(p,a,b,z,h,wall);Box("baseboard",p,new Vector3((a+b)*.5f,.09f,z-.09f),new Vector3(b-a,.12f,.06f),trim,false);Box("wall cap",p,new Vector3((a+b)*.5f,h+.03f,z),new Vector3(b-a+.04f,.07f,.20f),trim,false);}
        static void LowWallZ(Transform p,float x,float a,float b,float h,Material wall,Material trim){WallZ(p,x,a,b,h,wall);Box("baseboard",p,new Vector3(x-.09f,.09f,(a+b)*.5f),new Vector3(.06f,.12f,b-a),trim,false);Box("wall cap",p,new Vector3(x,h+.03f,(a+b)*.5f),new Vector3(.20f,.07f,b-a+.04f),trim,false);}
        static void Boot(Transform p,Vector3 pos,Material m,float yaw){Box("boot",p,pos,new Vector3(.22f,.22f,.40f),m,false,Quaternion.Euler(0,yaw,0));Box("boot cuff",p,pos+new Vector3(0,.17f,-.07f),new Vector3(.22f,.24f,.22f),m,false,Quaternion.Euler(0,yaw,0));}
        static void Mug(Transform p,Vector3 pos,Material m){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name="mug";g.transform.SetParent(p,true);g.transform.position=pos;g.transform.localScale=new Vector3(.09f,.08f,.09f);g.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());}
        static void Sphere(string name,Transform p,Vector3 pos,float scale,Material m,float yScale=1f){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=name;g.transform.SetParent(p,true);g.transform.position=pos;g.transform.localScale=new Vector3(scale,scale*yScale,scale);g.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());}
        static void SphereLocal(string name,Transform p,Vector3 local,float scale,Material m){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=name;g.transform.SetParent(p,false);g.transform.localPosition=local;g.transform.localScale=Vector3.one*scale;g.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());}
        static GameObject Box(string name,Transform p,Vector3 pos,Vector3 scale,Material m,bool collider,Quaternion? rot=null){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(p,true);g.transform.position=pos;g.transform.rotation=rot??Quaternion.identity;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=m;if(!collider)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
        static GameObject BoxLocal(string name,Transform p,Vector3 pos,Vector3 scale,Material m){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
        static Transform Group(Transform p,string name){GameObject g=new GameObject(name);g.transform.SetParent(p,false);return g.transform;}
        static Transform Group(Transform p,string name,bool world){GameObject g=new GameObject(name);g.transform.SetParent(p,!world);return g.transform;}
        static Transform Group(GameObject parent,string name){return Group(parent.transform,name);}
        static void PointLight(string name,Transform p,Vector3 pos,Color color,float intensity,float range){GameObject g=new GameObject(name);g.transform.SetParent(p,true);g.transform.position=pos;Light l=g.AddComponent<Light>();l.type=LightType.Point;l.color=color;l.intensity=intensity;l.range=range;l.shadows=LightShadows.Soft;}

        static void PlaceAsset(string assetName,string objectName,Transform parent,Vector3 pos,float yaw,float scale){string[] ids=AssetDatabase.FindAssets(assetName+" t:GameObject");if(ids.Length==0)return;GameObject asset=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(ids[0]));if(!asset)return;GameObject g=PrefabUtility.InstantiatePrefab(asset) as GameObject;if(!g)g=UnityEngine.Object.Instantiate(asset);g.name=objectName;g.transform.SetParent(parent,true);g.transform.position=pos;g.transform.rotation=Quaternion.Euler(0,yaw,0);g.transform.localScale=Vector3.one*scale;foreach(Collider c in g.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);}

        static void CleanupMissingScripts(){foreach(GameObject g in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(g&&g.scene.IsValid())GameObjectUtility.RemoveMonoBehavioursWithMissingScript(g);}
    }
}
#endif
