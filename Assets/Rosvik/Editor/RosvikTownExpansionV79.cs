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
    public static class RosvikTownExpansionV79 {
        const int Version=79;
        const string Key="ROSVIK_TOWN_EXPANSION_V79";
        const string ScenePath="Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string GroupName="V79 NORTH NEIGHBORHOOD - HOUSE B";
        const string MatDir="Assets/Rosvik/GeneratedV79";

        static RosvikTownExpansionV79(){if(EditorPrefs.GetInt(Key,0)>=Version)return;EditorApplication.delayCall+=Auto;}
        [MenuItem("Rosvik/V79 NORTH NEIGHBORHOOD + HOUSE B + BACKPACK")]
        public static void Force(){EditorPrefs.DeleteKey(Key);EditorApplication.delayCall+=Auto;}
        static void Auto(){
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=Auto;return;}
            if(!File.Exists(ScenePath))return;
            try{Apply();}catch(Exception ex){Debug.LogError("V79 FAILED: "+ex);}
        }

        static void Apply(){
            if(EditorSceneManager.GetActiveScene().path!=ScenePath)EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            CoziPlayerV57 player=UnityEngine.Object.FindFirstObjectByType<CoziPlayerV57>();if(!player)throw new Exception("PLAYER missing");
            GameObject old=GameObject.Find(GroupName);if(old)UnityEngine.Object.DestroyImmediate(old);
            GameObject root=new GameObject(GroupName);

            Shader shader=PickShader();
            Material ground=Mat(shader,"ground","2c4336",.10f);
            Material groundDark=Mat(shader,"ground_dark","22352c",.08f);
            Material gravel=Mat(shader,"gravel","706b61",.15f);
            Material snow=Mat(shader,"snow","aebbb8",.12f);
            Material wall=Mat(shader,"house_wall","a99b80",.12f);
            Material wallDark=Mat(shader,"house_wall_dark","746a5c",.10f);
            Material trim=Mat(shader,"trim","d2c6ab",.16f);
            Material wood=Mat(shader,"wood","7b563c",.15f);
            Material woodDark=Mat(shader,"wood_dark","513a2d",.12f);
            Material floor=Mat(shader,"floor","81694f",.12f);
            Material greenFloor=Mat(shader,"green_floor","5e6f61",.10f);
            Material tile=Mat(shader,"tile","66716b",.14f);
            Material metal=Mat(shader,"metal","505958",.22f);
            Material darkMetal=Mat(shader,"dark_metal","303938",.20f);
            Material rust=Mat(shader,"rust","78503b",.14f);
            Material pine=Mat(shader,"pine","294735",.06f);
            Material pineDark=Mat(shader,"pine_dark","20392c",.05f);
            Material bark=Mat(shader,"bark","564536",.08f);
            Material birch=Mat(shader,"birch","aaa694",.08f);
            Material branch=Mat(shader,"branch","4b4034",.06f);
            Material shrub=Mat(shader,"shrub","354f3c",.06f);
            Material red=Mat(shader,"car_red","6d4037",.16f);
            Material blue=Mat(shader,"blue","445d63",.13f);
            Material cloth=Mat(shader,"cloth","9c8c70",.08f);

            Transform land=Group(root.transform,"LAND + ROADS");
            Transform house=Group(root.transform,"HOUSE B");
            Transform shed=Group(root.transform,"VEDBOD + YARD");
            Transform veg=Group(root.transform,"NORTH VEGETATION");
            Transform dressing=Group(root.transform,"STREET DRESSING");

            BuildLand(land,ground,groundDark,gravel,snow);
            BuildHouseB(house,wall,wallDark,trim,wood,woodDark,floor,greenFloor,tile,metal,darkMetal,blue,cloth);
            BuildShed(shed,wood,woodDark,darkMetal,rust,gravel,snow);
            BuildVegetation(veg,bark,pine,pineDark,birch,branch,shrub,snow);
            BuildDressing(dressing,wood,metal,darkMetal,rust,red,gravel,snow);

            HouseInteriorDoorNetworkV77 network=player.GetComponent<HouseInteriorDoorNetworkV77>();if(!network)network=player.gameObject.AddComponent<HouseInteriorDoorNetworkV77>();network.enabled=true;network.interactionDistance=2.75f;network.Refresh();EditorUtility.SetDirty(network);
            WorldExplorationV79 explore=player.GetComponent<WorldExplorationV79>();if(!explore)explore=player.gameObject.AddComponent<WorldExplorationV79>();explore.enabled=true;EditorUtility.SetDirty(explore);
            player.SetObjective("Följ grusvägen norrut. Fler bostäder betyder mer mat och kläder — men också längre väg tillbaka i kylan.");
            EditorUtility.SetDirty(player);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();EditorPrefs.SetInt(Key,Version);SceneView.RepaintAll();
            Debug.Log("V79 COMPLETE — north neighborhood added with House B, open woodshed, backpack progression, roads, fences, mailboxes, abandoned car, snowbanks, denser vegetation and location-based loot.");
        }

        static void BuildLand(Transform p,Material ground,Material dark,Material gravel,Material snow){
            Floor("north ground",p,new Vector3(-29f,-.09f,29f),new Vector3(30f,.14f,25f),ground,true);
            Floor("west forest ground",p,new Vector3(-46f,-.10f,29f),new Vector3(10f,.12f,28f),dark,true);
            Floor("north lane",p,new Vector3(-28.8f,-.005f,19.3f),new Vector3(4.0f,.065f,8.0f),gravel,true);
            Floor("house B lane",p,new Vector3(-30.0f,.0f,20.7f),new Vector3(18f,.065f,3.4f),gravel,true);
            Floor("house B approach",p,new Vector3(-29.4f,.015f,22.8f),new Vector3(2.4f,.075f,4.5f),gravel,true);
            // shallow ditch and irregular snowbanks make the road feel embedded in terrain
            Floor("ditch west",p,new Vector3(-39.3f,-.025f,20.4f),new Vector3(1.4f,.04f,15.5f),dark,false);
            for(int i=0;i<9;i++)SnowMound("road snowbank",p,new Vector3(-38f+i*2.1f,.03f,18.75f+(i%2)*.18f),new Vector3(1.8f,.18f,.55f),snow);
            for(int i=0;i<6;i++)SnowMound("yard snowbank",p,new Vector3(-20.4f,.02f,24f+i*2.1f),new Vector3(.60f,.16f,1.65f),snow);
        }

        static void BuildHouseB(Transform parent,Material wall,Material wallDark,Material trim,Material wood,Material woodDark,Material floor,Material green,Material tile,Material metal,Material darkMetal,Material blue,Material cloth){
            const float minX=-36f,maxX=-22f,front=22.4f,back=35.4f,h=1.28f;
            Transform shell=Group(parent,"SHELL");Transform details=Group(parent,"DETAILS");
            Floor("living floor",shell,new Vector3(-32.2f,.025f,27.0f),new Vector3(7.6f,.08f,8.7f),floor,true);
            Floor("kitchen floor",shell,new Vector3(-25.9f,.025f,27.0f),new Vector3(5.0f,.08f,8.7f),green,true);
            Floor("bedroom floor",shell,new Vector3(-32.2f,.025f,32.7f),new Vector3(7.6f,.08f,4.6f),floor,true);
            Floor("utility floor",shell,new Vector3(-25.9f,.025f,32.7f),new Vector3(5.0f,.08f,4.6f),tile,true);

            // outer shell — front has a real door gap
            WallX(shell,minX,-30.6f,front,h,wall,trim);WallX(shell,-28.2f,maxX,front,h,wall,trim);
            WallX(shell,minX,maxX,back,h,wallDark,trim);WallZ(shell,minX,front,back,h,wall,trim);WallZ(shell,maxX,front,back,h,wall,trim);
            // internal partitions with three deliberate door gaps
            WallX(shell,minX,-33.3f,30.45f,.92f,wall,trim);WallX(shell,-31.8f,-26.8f,30.45f,.92f,wall,trim);WallX(shell,-25.3f,maxX,30.45f,.92f,wall,trim);
            WallZ(shell,-28.0f,front,25.3f,.92f,wall,trim);WallZ(shell,-28.0f,26.7f,back,.92f,wall,trim);

            Door(shell,"hus B ytterdörr",new Vector3(-29.4f,0,front),0,1.65f,wood,blue,trim,metal,-94f);
            Door(shell,"hus B sovrum",new Vector3(-32.55f,0,30.45f),0,1.20f,wood,woodDark,trim,metal,92f);
            Door(shell,"hus B badrum",new Vector3(-26.05f,0,30.45f),0,1.18f,wood,woodDark,trim,metal,-92f);
            Door(shell,"hus B kök",new Vector3(-28.0f,0,26.0f),90,1.15f,wood,woodDark,trim,metal,92f);

            // windows read from outside even when interior details are hidden
            Window(shell,new Vector3(-33.1f,1.52f,35.28f),0,2.1f,wallDark,trim,blue);
            Window(shell,new Vector3(-25.2f,1.52f,35.28f),0,1.8f,wallDark,trim,blue);
            Window(shell,new Vector3(-35.88f,1.52f,26.7f),90,2.0f,wall,trim,blue);
            Window(shell,new Vector3(-22.12f,1.52f,27.0f),90,1.8f,wall,trim,blue);

            // living room, deliberately arranged rather than scattered
            PlaceAsset("couch_pillows","hus B soffa",details,new Vector3(-34.3f,0,27.2f),90,.83f);
            PlaceAsset("armchair_pillows","hus B fåtölj",details,new Vector3(-31.2f,0,28.4f),180,.80f);
            PlaceAsset("table_low","hus B soffbord",details,new Vector3(-32.7f,0,26.7f),0,.82f);
            PlaceAsset("rug_rectangle_stripes_A","hus B vardagsrumsmatta",details,new Vector3(-32.8f,.03f,27.0f),0,.95f);
            PlaceAsset("lamp_standing","hus B golvlampa",details,new Vector3(-35.0f,0,29.0f),0,.82f);
            PlaceAsset("shelf_B_small_decorated","hus B bokhylla",details,new Vector3(-35.2f,0,23.7f),0,.68f);
            PlaceAsset("pictureframe_medium","hus B tavla",details,new Vector3(-33.0f,1.10f,35.20f),0,.70f);

            // kitchen
            PlaceAsset("table_medium","hus B köksbord",details,new Vector3(-25.5f,0,26.8f),0,.78f);
            PlaceAsset("chair_A","hus B köksstol A",details,new Vector3(-26.5f,0,26.8f),90,.72f);
            PlaceAsset("chair_A","hus B köksstol B",details,new Vector3(-24.5f,0,26.8f),-90,.72f);
            Box("kitchen counter",details,new Vector3(-24.6f,.48f,34.85f),new Vector3(4.4f,.92f,.62f),wood,true);
            Box("counter top",details,new Vector3(-24.6f,.98f,34.85f),new Vector3(4.5f,.10f,.68f),woodDark,false);
            Crate(details,"hus B skafferi",new Vector3(-23.0f,0,24.1f),90,wood,woodDark,new[]{"Konservburk","Soppa","Kex","Choklad","Vattenflaska"},new[]{3,2,2,1,2});

            // bedroom, clothing and backpack progression
            Bed(details,new Vector3(-34.0f,0,33.1f),90,wood,cloth,green);
            Crate(details,"hus B klädförvaring",new Vector3(-30.0f,0,33.8f),0,wood,woodDark,new[]{"Ulltröja","Mössa","Handskar","Hoodie"},new[]{1,1,1,1});
            Backpack(details,new Vector3(-30.2f,.02f,31.55f),180,green,darkMetal,cloth);

            // utility / bathroom supplies
            Box("utility shelf",details,new Vector3(-22.8f,.75f,33.0f),new Vector3(.65f,1.45f,2.0f),metal,true);
            Crate(details,"hus B medicinlåda",new Vector3(-25.0f,0,32.15f),0,metal,trim,new[]{"Förband","Värktabletter","Antiseptisk spray","Elastisk linda"},new[]{2,2,1,1});
            Crate(details,"hus B städskåp",new Vector3(-23.0f,0,29.15f),90,wood,woodDark,new[]{"Tejp","Tygbit","Tändare"},new[]{1,2,1});

            BuildingCutawayV79 cut=parent.gameObject.AddComponent<BuildingCutawayV79>();cut.shell=shell;cut.details=details.gameObject;cut.minXZ=new Vector2(minX-.2f,front-.2f);cut.maxXZ=new Vector2(maxX+.2f,back+.2f);cut.insideWallAlpha=.18f;
            EditorUtility.SetDirty(cut);
        }

        static void BuildShed(Transform p,Material wood,Material woodDark,Material metal,Material rust,Material gravel,Material snow){
            // open-front woodshed: no roof sheet hiding gameplay from the top-down camera
            Floor("shed floor",p,new Vector3(-40.2f,.015f,31.0f),new Vector3(5.2f,.08f,6.0f),gravel,true);
            Box("shed back",p,new Vector3(-42.65f,.75f,31.0f),new Vector3(.16f,1.5f,6.0f),wood,true);
            Box("shed side north",p,new Vector3(-40.2f,.75f,33.9f),new Vector3(5.0f,1.5f,.16f),wood,true);
            Box("shed side south",p,new Vector3(-40.2f,.75f,28.1f),new Vector3(5.0f,1.5f,.16f),wood,true);
            for(int row=0;row<3;row++)for(int i=0;i<6;i++){
                GameObject log=GameObject.CreatePrimitive(PrimitiveType.Cylinder);log.name="vedträ";log.transform.SetParent(p,true);log.transform.position=new Vector3(-41.6f+row*.48f,.18f+i*.27f,29.1f);log.transform.rotation=Quaternion.Euler(0,0,90);log.transform.localScale=new Vector3(.13f,.52f,.13f);log.GetComponent<Renderer>().sharedMaterial=woodDark;Collider c=log.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);
            }
            Crate(p,"vedbodens verktyg",new Vector3(-39.2f,0,32.7f),0,wood,metal,new[]{"Multiverktyg","Tejp","Träspill","Bränsle"},new[]{1,1,4,1});
            Box("old fuel can",p,new Vector3(-39.0f,.35f,29.2f),new Vector3(.55f,.70f,.36f),rust,true);
            SnowMound("shed drift",p,new Vector3(-43.0f,.02f,34.1f),new Vector3(3.8f,.26f,.75f),snow);
        }

        static void BuildVegetation(Transform p,Material bark,Material pine,Material pineDark,Material birch,Material branch,Material shrub,Material snow){
            Vector3[] pines={
                new Vector3(-47,0,19),new Vector3(-49,0,23),new Vector3(-47,0,27),new Vector3(-50,0,31),new Vector3(-47,0,35),new Vector3(-49,0,39),
                new Vector3(-18,0,20),new Vector3(-16,0,24),new Vector3(-17,0,29),new Vector3(-15,0,34),new Vector3(-18,0,39),
                new Vector3(-42,0,41),new Vector3(-37,0,42),new Vector3(-31,0,41),new Vector3(-25,0,42),new Vector3(-20,0,41)
            };
            for(int i=0;i<pines.Length;i++)Pine("gran",p,pines[i],.78f+(i%4)*.08f,bark,i%2==0?pine:pineDark,snow);
            Vector3[] birches={new Vector3(-44,0,21),new Vector3(-45,0,30),new Vector3(-43,0,37),new Vector3(-19,0,26),new Vector3(-20,0,36),new Vector3(-34,0,40)};
            for(int i=0;i<birches.Length;i++)Birch("björk",p,birches[i],.86f+(i%3)*.07f,birch,branch);
            Vector3[] bushes={new Vector3(-41,0,22),new Vector3(-45,0,25),new Vector3(-44,0,34),new Vector3(-39,0,38),new Vector3(-21,0,22),new Vector3(-19,0,31),new Vector3(-22,0,39),new Vector3(-26,0,39)};
            for(int i=0;i<bushes.Length;i++)Shrub("buske",p,bushes[i],.65f+(i%3)*.10f,shrub,snow);
            for(int i=0;i<12;i++)GrassTuft("vintergräs",p,new Vector3(-45f+(i%4)*8.2f,0,20.0f+(i/4)*9.6f),.55f+(i%2)*.10f,shrub);
        }

        static void BuildDressing(Transform p,Material wood,Material metal,Material darkMetal,Material rust,Material car,Material gravel,Material snow){
            // fences frame the yard but leave road and doors clear
            for(int i=0;i<6;i++){float x=-36f+i*2.2f;Box("yard fence post",p,new Vector3(x,.48f,19.0f),new Vector3(.10f,.96f,.10f),wood,true);if(i<5)Box("yard fence rail",p,new Vector3(x+1.1f,.66f,19.0f),new Vector3(2.1f,.10f,.08f),wood,false);}
            // mailbox, bin, two dead street lamps
            Box("mailbox post",p,new Vector3(-34.8f,.62f,20.5f),new Vector3(.10f,1.24f,.10f),wood,true);Box("mailbox",p,new Vector3(-34.8f,1.15f,20.5f),new Vector3(.55f,.32f,.34f),metal,true);
            Box("wheelie bin",p,new Vector3(-23.1f,.55f,20.9f),new Vector3(.62f,1.10f,.70f),darkMetal,true);Box("bin lid",p,new Vector3(-23.1f,1.12f,20.85f),new Vector3(.68f,.10f,.76f),darkMetal,false);
            LampPost(p,new Vector3(-37.5f,0,20.2f),metal,darkMetal);LampPost(p,new Vector3(-20.5f,0,20.2f),metal,darkMetal);
            Car(p,new Vector3(-24.0f,.06f,18.9f),8f,car,darkMetal,rust);
            // tire tracks and footprints hint that someone left recently
            for(int i=0;i<7;i++){Floor("tire track",p,new Vector3(-31.0f+i*1.15f,.035f,20.0f),new Vector3(.72f,.016f,.10f),darkMetal,false);}
            for(int i=0;i<7;i++){Floor("footprint",p,new Vector3(-30.2f+(i%2)*.22f,.04f,21.0f+i*.42f),new Vector3(.17f,.018f,.30f),gravel,false);}
            SnowMound("mailbox drift",p,new Vector3(-35.6f,.02f,19.8f),new Vector3(1.5f,.18f,.65f),snow);
        }

        static void Backpack(Transform p,Vector3 pos,float yaw,Material body,Material metal,Material cloth){
            GameObject r=new GameObject("vandringsryggsäck 45 kg");r.transform.SetParent(p,true);r.transform.position=pos;r.transform.rotation=Quaternion.Euler(0,yaw,0);
            Box("pack body",r.transform,new Vector3(0,.62f,0),new Vector3(.72f,1.05f,.42f),body,true);Box("top flap",r.transform,new Vector3(0,.99f,-.05f),new Vector3(.76f,.32f,.46f),cloth,false);Box("pocket",r.transform,new Vector3(0,.48f,-.25f),new Vector3(.48f,.42f,.16f),body,false);Box("strap L",r.transform,new Vector3(-.25f,.60f,.25f),new Vector3(.08f,.85f,.08f),metal,false);Box("strap R",r.transform,new Vector3(.25f,.60f,.25f),new Vector3(.08f,.85f,.08f),metal,false);
            BackpackUpgradeV79 u=r.AddComponent<BackpackUpgradeV79>();u.radius=2.15f;u.newCapacityKg=45f;
        }
        static void Bed(Transform p,Vector3 pos,float yaw,Material wood,Material cloth,Material blanket){GameObject r=new GameObject("hus B säng");r.transform.SetParent(p,true);r.transform.position=pos;r.transform.rotation=Quaternion.Euler(0,yaw,0);Box("bed frame",r.transform,new Vector3(0,.20f,0),new Vector3(2.10f,.28f,1.10f),wood,true);Box("mattress",r.transform,new Vector3(0,.43f,0),new Vector3(2.0f,.22f,1.02f),cloth,true);Box("blanket",r.transform,new Vector3(.30f,.58f,0),new Vector3(1.20f,.08f,1.0f),blanket,false);Box("pillow",r.transform,new Vector3(-.78f,.61f,0),new Vector3(.38f,.12f,.68f),cloth,false);V71RestSpot rest=r.AddComponent<V71RestSpot>();rest.displayName="sängen";rest.radius=2.2f;rest.restHours=1f;rest.sleepHours=8f;}

        static void Door(Transform p,string name,Vector3 pos,float yaw,float width,Material body,Material inset,Material trim,Material metal,float angle){GameObject root=new GameObject("door — "+name);root.transform.SetParent(p,true);root.transform.position=pos;root.transform.rotation=Quaternion.Euler(0,yaw,0);float h=1.92f;Box("frame L",root.transform,new Vector3(-width*.56f,h*.5f,0),new Vector3(.13f,h,.18f),trim,true);Box("frame R",root.transform,new Vector3(width*.56f,h*.5f,0),new Vector3(.13f,h,.18f),trim,true);Box("frame top",root.transform,new Vector3(0,h,0),new Vector3(width*1.24f,.14f,.18f),trim,true);Transform hinge=Group(root.transform,"door hinge");hinge.localPosition=new Vector3(-width*.50f,0,-.03f);GameObject leaf=Box("door leaf",hinge,new Vector3(width*.5f,h*.5f,0),new Vector3(width,h,.10f),body,true);Box("door inset",hinge,new Vector3(width*.52f,h*.58f,-.058f),new Vector3(width*.62f,h*.34f,.02f),inset,false);Sphere("handle",hinge,new Vector3(width*.86f,h*.50f,-.09f),Vector3.one*.075f,metal,false);HouseInteriorDoorV77 d=root.AddComponent<HouseInteriorDoorV77>();d.displayName=name;d.hinge=hinge;d.closedEuler=Vector3.zero;d.openEuler=new Vector3(0,angle,0);d.animationTime=.23f;EditorUtility.SetDirty(d);}
        static void Window(Transform p,Vector3 pos,float yaw,float width,Material wall,Material trim,Material glass){GameObject r=new GameObject("window");r.transform.SetParent(p,true);r.transform.position=pos;r.transform.rotation=Quaternion.Euler(0,yaw,0);Box("window trim",r.transform,Vector3.zero,new Vector3(width,.82f,.08f),trim,false);Box("window glass",r.transform,new Vector3(0,0,-.05f),new Vector3(width-.18f,.62f,.025f),glass,false);}
        static void Crate(Transform p,string name,Vector3 pos,float yaw,Material body,Material lidMat,string[] items,int[] counts){GameObject root=new GameObject(name);root.transform.SetParent(p,true);root.transform.position=pos;root.transform.rotation=Quaternion.Euler(0,yaw,0);GameObject shell=Box("crate body",root.transform,new Vector3(0,.34f,0),new Vector3(1.12f,.64f,.76f),body,true);Transform lid=Group(root.transform,"lid hinge");lid.localPosition=new Vector3(0,.66f,.36f);Box("lid",lid,new Vector3(0,.03f,-.36f),new Vector3(1.15f,.10f,.76f),lidMat,true);Transform reveal=Group(root.transform,"contents");Box("visible contents",reveal,new Vector3(0,.58f,0),new Vector3(.68f,.12f,.34f),lidMat,false);reveal.gameObject.SetActive(false);LootContainerV74 c=root.AddComponent<LootContainerV74>();c.displayName=name;c.radius=2.25f;c.movingPart=lid;c.closedEuler=Vector3.zero;c.openEuler=new Vector3(-82,0,0);c.revealOnOpen=reveal;c.highlightRenderer=shell.GetComponent<Renderer>();c.items=items;c.counts=counts;c.animationTime=.28f;}

        static void Pine(string n,Transform p,Vector3 pos,float s,Material bark,Material needles,Material snow){GameObject r=new GameObject(n);r.transform.SetParent(p,true);r.transform.position=pos;r.transform.localScale=Vector3.one*s;GameObject trunk=GameObject.CreatePrimitive(PrimitiveType.Cylinder);trunk.name="trunk";trunk.transform.SetParent(r.transform,false);trunk.transform.localPosition=new Vector3(0,1.05f,0);trunk.transform.localScale=new Vector3(.19f,1.05f,.19f);trunk.GetComponent<Renderer>().sharedMaterial=bark;Foliage(r.transform,new Vector3(0,1.62f,0),new Vector3(2.15f,.70f,2.15f),needles);Foliage(r.transform,new Vector3(0,2.28f,0),new Vector3(1.65f,.62f,1.65f),needles);Foliage(r.transform,new Vector3(0,2.83f,0),new Vector3(1.08f,.52f,1.08f),needles);Foliage(r.transform,new Vector3(0,3.18f,0),new Vector3(.48f,.16f,.48f),snow);}
        static void Birch(string n,Transform p,Vector3 pos,float s,Material white,Material branch){GameObject r=new GameObject(n);r.transform.SetParent(p,true);r.transform.position=pos;r.transform.localScale=Vector3.one*s;GameObject trunk=GameObject.CreatePrimitive(PrimitiveType.Cylinder);trunk.name="birch trunk";trunk.transform.SetParent(r.transform,false);trunk.transform.localPosition=new Vector3(0,1.42f,0);trunk.transform.localScale=new Vector3(.12f,1.42f,.12f);trunk.GetComponent<Renderer>().sharedMaterial=white;for(int i=0;i<6;i++){float y=1.65f+i*.30f;GameObject b=Box("branch",r.transform,new Vector3((i%2==0?.25f:-.25f),y,0),new Vector3(.65f,.055f,.055f),branch,false);b.transform.localRotation=Quaternion.Euler(0,(i*31)%120,i%2==0?30:-30);}}
        static void Shrub(string n,Transform p,Vector3 pos,float s,Material m,Material snow){GameObject r=new GameObject(n);r.transform.SetParent(p,true);r.transform.position=pos;for(int i=0;i<3;i++)Foliage(r.transform,new Vector3((i-1)*.28f,.28f+(i%2)*.10f,(i%2)*.15f),new Vector3(.68f,.38f,.60f)*s,m);Foliage(r.transform,new Vector3(0,.54f,0),new Vector3(.45f,.08f,.34f)*s,snow);}
        static void GrassTuft(string n,Transform p,Vector3 pos,float s,Material m){GameObject r=new GameObject(n);r.transform.SetParent(p,true);r.transform.position=pos;for(int i=0;i<4;i++){GameObject g=Box("blade",r.transform,new Vector3((i-1.5f)*.07f,.20f,0),new Vector3(.04f,.40f,.05f)*s,m,false);g.transform.localRotation=Quaternion.Euler(i%2==0?10:-10,i*41,i%2==0?12:-12);}}
        static void LampPost(Transform p,Vector3 pos,Material metal,Material dark){Box("street lamp post",p,pos+new Vector3(0,1.35f,0),new Vector3(.10f,2.7f,.10f),metal,true);Box("dead lamp head",p,pos+new Vector3(.25f,2.62f,0),new Vector3(.55f,.18f,.28f),dark,false);}
        static void Car(Transform p,Vector3 pos,float yaw,Material body,Material dark,Material rust){GameObject r=new GameObject("övergiven kombi");r.transform.SetParent(p,true);r.transform.position=pos;r.transform.rotation=Quaternion.Euler(0,yaw,0);Box("car body",r.transform,new Vector3(0,.40f,0),new Vector3(3.8f,.60f,1.65f),body,true);Box("car cabin",r.transform,new Vector3(-.25f,.88f,0),new Vector3(2.0f,.56f,1.42f),body,true);Box("rust patch",r.transform,new Vector3(1.1f,.72f,-.84f),new Vector3(.72f,.07f,.04f),rust,false);for(int i=0;i<4;i++){float x=i<2?-1.20f:1.20f;float z=i%2==0?-.78f:.78f;GameObject w=GameObject.CreatePrimitive(PrimitiveType.Cylinder);w.name="wheel";w.transform.SetParent(r.transform,false);w.transform.localPosition=new Vector3(x,.26f,z);w.transform.localRotation=Quaternion.Euler(90,0,0);w.transform.localScale=new Vector3(.31f,.15f,.31f);w.GetComponent<Renderer>().sharedMaterial=dark;}}
        static void SnowMound(string n,Transform p,Vector3 pos,Vector3 scale,Material m){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=n;g.transform.SetParent(p,true);g.transform.position=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=m;Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);}
        static void Foliage(Transform p,Vector3 lp,Vector3 sc,Material m){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name="crown";g.transform.SetParent(p,false);g.transform.localPosition=lp;g.transform.localScale=sc;g.GetComponent<Renderer>().sharedMaterial=m;Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);}

        static GameObject PlaceAsset(string search,string name,Transform p,Vector3 pos,float yaw,float scale){string[] guids=AssetDatabase.FindAssets(search+" t:GameObject");string path=guids.Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(x=>x.IndexOf("ThirdParty",StringComparison.OrdinalIgnoreCase)>=0)||guids.Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault();if(string.IsNullOrEmpty(path))return null;GameObject prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!prefab)return null;GameObject g=(GameObject)PrefabUtility.InstantiatePrefab(prefab);g.name=name;g.transform.SetParent(p,true);g.transform.position=pos;g.transform.rotation=Quaternion.Euler(0,yaw,0);g.transform.localScale=Vector3.one*scale;return g;}
        static Material Mat(Shader shader,string name,string hex,float smooth){Directory.CreateDirectory(MatDir);string path=MatDir+"/"+name+".mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(path);Color c=Color.white;ColorUtility.TryParseHtmlString("#"+hex,out c);if(!m){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}if(m.shader!=shader)m.shader=shader;if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);if(m.HasProperty("_Color"))m.SetColor("_Color",c);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",0f);EditorUtility.SetDirty(m);return m;}
        static Shader PickShader(){bool srp=GraphicsSettings.currentRenderPipeline!=null||GraphicsSettings.defaultRenderPipeline!=null;Shader s=null;if(srp){s=Shader.Find("Universal Render Pipeline/Lit");if(!s||!s.isSupported)s=Shader.Find("Universal Render Pipeline/Simple Lit");}else s=Shader.Find("Standard");if(!s||!s.isSupported){s=Shader.Find("Universal Render Pipeline/Lit");if(!s||!s.isSupported)s=Shader.Find("Universal Render Pipeline/Simple Lit");if(!s||!s.isSupported)s=Shader.Find("Standard");}if(!s||!s.isSupported)throw new Exception("No supported active-pipeline shader");return s;}
        static Transform Group(Transform p,string n){GameObject g=new GameObject(n);g.transform.SetParent(p,true);return g.transform;}
        static GameObject Box(string n,Transform p,Vector3 pos,Vector3 size,Material m,bool collider){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;if(!collider){Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);}return g;}
        static GameObject Sphere(string n,Transform p,Vector3 pos,Vector3 size,Material m,bool collider){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;if(!collider){Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);}return g;}
        static void Floor(string n,Transform p,Vector3 pos,Vector3 size,Material m,bool collider){Box(n,p,pos,size,m,collider);}
        static void WallX(Transform p,float x0,float x1,float z,float h,Material wall,Material trim){float len=x1-x0;Box("wall",p,new Vector3((x0+x1)*.5f,h*.5f,z),new Vector3(len,h,.18f),wall,true);Box("wall cap",p,new Vector3((x0+x1)*.5f,h+.035f,z),new Vector3(len+.05f,.07f,.23f),trim,false);}
        static void WallZ(Transform p,float x,float z0,float z1,float h,Material wall,Material trim){float len=z1-z0;Box("wall",p,new Vector3(x,h*.5f,(z0+z1)*.5f),new Vector3(.18f,h,len),wall,true);Box("wall cap",p,new Vector3(x,h+.035f,(z0+z1)*.5f),new Vector3(.23f,.07f,len+.05f),trim,false);}
    }
}
#endif
