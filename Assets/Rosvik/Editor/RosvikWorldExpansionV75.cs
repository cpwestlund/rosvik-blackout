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
    public static class RosvikWorldExpansionV75 {
        const int Version=75;
        const string Key="ROSVIK_WORLD_EXPANSION_V75";
        const string ScenePath="Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string GroupName="WORLD EXPANSION V75 - HOUSE A";

        static RosvikWorldExpansionV75(){if(EditorPrefs.GetInt(Key,0)>=Version)return;EditorApplication.delayCall+=Auto;}

        [MenuItem("Rosvik/V75 EXPAND WORLD - HOUSE A + LOCATION LOOT")]
        public static void Force(){EditorPrefs.DeleteKey(Key);EditorApplication.delayCall+=Auto;}

        static void Auto(){
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=Auto;return;}
            if(!File.Exists(ScenePath))return;
            try{Apply();}catch(Exception ex){Debug.LogError("V75 FAILED: "+ex);}
        }

        static void Apply(){
            if(EditorSceneManager.GetActiveScene().path!=ScenePath)EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            GameObject old=GameObject.Find(GroupName);if(old)UnityEngine.Object.DestroyImmediate(old);
            GameObject root=new GameObject(GroupName);

            CoziPlayerV57 player=UnityEngine.Object.FindFirstObjectByType<CoziPlayerV57>();
            if(!player)throw new Exception("PLAYER missing");
            SurvivalLootTransferV74 transfer=player.GetComponent<SurvivalLootTransferV74>();
            if(!transfer)transfer=player.gameObject.AddComponent<SurvivalLootTransferV74>();
            transfer.enabled=true;transfer.discoveryRange=5f;transfer.interactionRange=2.65f;transfer.backpackCapacityKg=34f;

            Shader shader=PickShader();
            Material grass=Mat(shader,C("31483a"));
            Material path=Mat(shader,C("777064"));
            Material wall=Mat(shader,C("c8bda4"));
            Material trim=Mat(shader,C("e0d3b7"));
            Material wood=Mat(shader,C("835839"));
            Material woodDark=Mat(shader,C("60412f"));
            Material floor=Mat(shader,C("866e53"));
            Material tile=Mat(shader,C("66736c"));
            Material green=Mat(shader,C("66766a"));
            Material metal=Mat(shader,C("596261"));
            Material glass=Mat(shader,C("55777a"));
            Material cloth=Mat(shader,C("b9aa8b"));
            Material white=Mat(shader,C("d9d2c3"));

            Transform outside=Group(root.transform,"OUTSIDE");
            Transform house=Group(root.transform,"HOUSE A");
            Transform details=Group(root.transform,"HOUSE DETAILS");

            BuildOutside(outside,grass,path,trim,woodDark);
            BuildHouse(house,details,wall,trim,wood,woodDark,floor,tile,green,metal,glass,cloth,white);

            player.SetObjective("Överlev dagen. Välj din loot med omsorg och undersök huset väster om skolan.");
            EditorUtility.SetDirty(player);EditorUtility.SetDirty(transfer);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();EditorPrefs.SetInt(Key,Version);SceneView.RepaintAll();
            Debug.Log("V75 COMPLETE — first explorable house added west of the school with kitchen, bedroom, utility room, rest spot and location-based selective loot.");
        }

        static void BuildOutside(Transform p,Material grass,Material path,Material trim,Material wood){
            Floor("house ground",p,new Vector3(-29f,-.07f,8.7f),new Vector3(18f,.12f,17f),grass);
            Floor("walk to house",p,new Vector3(-23.0f,.005f,.2f),new Vector3(13.0f,.05f,2.0f),path);
            Floor("house approach",p,new Vector3(-29f,.008f,1.0f),new Vector3(2.2f,.055f,3.1f),path);
            for(int i=0;i<6;i++)Box("path seam",p,new Vector3(-27.8f+i*2.0f,.04f,.2f),new Vector3(.035f,.012f,1.85f),trim,false);
            Box("mailbox post",p,new Vector3(-25.0f,.65f,.7f),new Vector3(.10f,1.3f,.10f),wood,true);
            Box("mailbox",p,new Vector3(-25.0f,1.22f,.7f),new Vector3(.50f,.28f,.32f),wood,true);
        }

        static void BuildHouse(Transform p,Transform d,Material wall,Material trim,Material wood,Material woodDark,Material floor,Material tile,Material green,Material metal,Material glass,Material cloth,Material white){
            const float minX=-36f,maxX=-22f,front=2.4f,back=15.6f;
            // Floors define the rooms clearly without breaking the cozy palette.
            Floor("living floor",p,new Vector3(-32.0f,.025f,6.0f),new Vector3(8.0f,.08f,7.1f),floor);
            Floor("entry utility floor",p,new Vector3(-24.8f,.025f,6.0f),new Vector3(6.4f,.08f,7.1f),tile);
            Floor("kitchen floor",p,new Vector3(-32.0f,.025f,12.2f),new Vector3(8.0f,.08f,5.1f),green);
            Floor("bedroom floor",p,new Vector3(-24.8f,.025f,12.2f),new Vector3(6.4f,.08f,5.1f),floor);

            // Intentional cutaway front wall, full rear and side walls.
            WallX(p,minX,-30.1f,front,.55f,wall,trim);WallX(p,-27.9f,maxX,front,.55f,wall,trim);
            WallX(p,minX,maxX,back,1.20f,wall,trim);WallZ(p,minX,front,back,1.20f,wall,trim);WallZ(p,maxX,front,back,1.20f,wall,trim);
            // Horizontal room split with two door gaps.
            WallX(p,minX,-33.0f,9.25f,.94f,wall,trim);WallX(p,-31.6f,-26.0f,9.25f,.94f,wall,trim);WallX(p,-24.6f,maxX,9.25f,.94f,wall,trim);
            // Kitchen / bedroom divider with gap.
            WallZ(p,-28.0f,9.25f,11.1f,.94f,wall,trim);WallZ(p,-28.0f,12.6f,back,.94f,wall,trim);

            Door(p,"husets ytterdörr",new Vector3(-29f,0,front),0,1.8f,wood,glass,trim,metal,-96f);
            Door(p,"köket",new Vector3(-32.3f,0,9.25f),0,1.25f,wood,woodDark,trim,metal,94f);
            Door(p,"sovrummet",new Vector3(-25.3f,0,9.25f),0,1.25f,wood,woodDark,trim,metal,-94f);
            Door(p,"inre dörr",new Vector3(-28.0f,0,11.85f),90,1.2f,wood,woodDark,trim,metal,94f);

            // Windows and radiators make the house read as a real place rather than another box.
            Window(p,new Vector3(-34.0f,1.55f,15.48f),0,2.2f,wall,trim,glass);
            Window(p,new Vector3(-30.8f,1.55f,15.48f),0,1.9f,wall,trim,glass);
            Window(p,new Vector3(-24.3f,1.55f,15.48f),0,2.0f,wall,trim,glass);
            Window(p,new Vector3(-35.88f,1.55f,6.4f),90,2.0f,wall,trim,glass);
            Radiator(d,new Vector3(-34.0f,.50f,15.05f),0,white,metal);
            Radiator(d,new Vector3(-24.3f,.50f,15.05f),0,white,metal);

            // Living room.
            PlaceAsset("couch_pillows","living couch",d,new Vector3(-34.2f,0,6.1f),90,.84f);
            PlaceAsset("armchair_pillows","living armchair",d,new Vector3(-31.5f,0,7.2f),180,.82f);
            PlaceAsset("table_low","coffee table",d,new Vector3(-32.8f,0,5.6f),0,.88f);
            PlaceAsset("rug_rectangle_stripes_A","living rug",d,new Vector3(-32.8f,.035f,5.9f),0,1.0f);
            PlaceAsset("lamp_standing","floor lamp",d,new Vector3(-35.0f,0,7.4f),0,.86f);
            PlaceAsset("shelf_B_small_decorated","living shelf",d,new Vector3(-35.1f,0,3.5f),0,.70f);

            // Kitchen: table/chairs + a believable counter run.
            PlaceAsset("table_medium","kitchen table",d,new Vector3(-32.0f,0,11.2f),0,.82f);
            PlaceAsset("chair_A","kitchen chair",d,new Vector3(-33.0f,0,11.2f),90,.76f);
            PlaceAsset("chair_A","kitchen chair",d,new Vector3(-31.0f,0,11.2f),-90,.76f);
            Box("counter A",d,new Vector3(-34.9f,.48f,14.75f),new Vector3(1.75f,.92f,.62f),wood,true);
            Box("counter B",d,new Vector3(-32.9f,.48f,14.75f),new Vector3(1.75f,.92f,.62f),wood,true);
            Box("counter top A",d,new Vector3(-34.9f,.98f,14.75f),new Vector3(1.82f,.10f,.68f),woodDark,false);
            Box("counter top B",d,new Vector3(-32.9f,.98f,14.75f),new Vector3(1.82f,.10f,.68f),woodDark,false);
            DoubleCabinet(d,"skafferiet",new Vector3(-35.15f,0,12.2f),90,wood,woodDark,metal,new[]{"Konservburk","Soppa","Kex","Choklad"},new[]{2,1,2,1});
            SingleCabinet(d,"kylskåpet",new Vector3(-30.25f,0,14.8f),0,new Vector3(1.0f,1.85f,.72f),white,metal,new[]{"Vattenflaska","Sportdryck","Äpple"},new[]{2,1,2});

            // Bedroom + actual rest spot.
            GameObject bed=new GameObject("house bed");bed.transform.SetParent(d,true);bed.transform.position=new Vector3(-24.3f,0,13.1f);bed.transform.rotation=Quaternion.Euler(0,90,0);
            Box("bed frame",bed.transform,new Vector3(0,.20f,0),new Vector3(2.15f,.28f,1.15f),wood,true);
            Box("mattress",bed.transform,new Vector3(0,.43f,0),new Vector3(2.02f,.22f,1.05f),white,true);
            Box("blanket",bed.transform,new Vector3(.28f,.58f,0),new Vector3(1.15f,.08f,1.02f),green,false);
            Box("pillow",bed.transform,new Vector3(-.78f,.61f,0),new Vector3(.38f,.12f,.70f),cloth,false);
            V71RestSpot rest=bed.AddComponent<V71RestSpot>();rest.displayName="sängen";rest.radius=2.2f;rest.restHours=1f;rest.sleepHours=8f;
            DoubleCabinet(d,"sovrummets garderob",new Vector3(-22.75f,0,12.8f),90,wood,woodDark,metal,new[]{"Ulltröja","Mössa","Handskar","Hoodie"},new[]{1,1,1,1});
            PlaceAsset("lamp_table","sänglampa",d,new Vector3(-26.1f,.55f,14.4f),0,.72f);
            PlaceAsset("pictureframe_medium","family picture",d,new Vector3(-24.8f,1.25f,15.38f),0,.78f);

            // Utility / bathroom side. Medicine and tools are where they make sense.
            PlaceAsset("shelf_A_big","utility shelf",d,new Vector3(-22.8f,0,7.8f),90,.68f);
            SingleCabinet(d,"medicinskåpet",new Vector3(-22.68f,1.10f,4.8f),90,new Vector3(.78f,.72f,.32f),white,metal,new[]{"Förband","Förband","Sporttejp"},new[]{1,1,1});
            Crate(d,"verktygslådan",new Vector3(-25.2f,0,4.0f),0,woodDark,metal,new[]{"Batterier","Tejp","Tändare","Multiverktyg"},new[]{2,1,1,1});
            PlaceAsset("cactus_medium_A","window plant",d,new Vector3(-23.1f,.72f,8.4f),0,.72f);
        }

        static void DoubleCabinet(Transform p,string name,Vector3 pos,float yaw,Material body,Material door,Material metal,string[] items,int[] counts){
            GameObject root=new GameObject(name);root.transform.SetParent(p,true);root.transform.position=pos;root.transform.rotation=Quaternion.Euler(0,yaw,0);
            float w=1.35f,h=1.55f,d=.58f;
            Box("back",root.transform,new Vector3(0,h*.5f,d*.42f),new Vector3(w,h,.07f),body,true);
            Box("left side",root.transform,new Vector3(-w*.48f,h*.5f,0),new Vector3(.07f,h,d),body,true);
            Box("right side",root.transform,new Vector3(w*.48f,h*.5f,0),new Vector3(.07f,h,d),body,true);
            Box("top",root.transform,new Vector3(0,h,d*.02f),new Vector3(w,.08f,d),body,true);
            Transform left=Group(root.transform,"left hinge");left.localPosition=new Vector3(-w*.48f,0,-d*.49f);
            GameObject leftDoor=Box("left door",left,new Vector3(w*.24f,h*.5f,0),new Vector3(w*.48f,h*.91f,.07f),door,true);
            Transform right=Group(root.transform,"right hinge");right.localPosition=new Vector3(w*.48f,0,-d*.49f);
            Box("right door",right,new Vector3(-w*.24f,h*.5f,0),new Vector3(w*.48f,h*.91f,.07f),door,true);
            Sphere("handle",left,new Vector3(w*.40f,h*.52f,-.08f),Vector3.one*.075f,metal);
            Sphere("handle",right,new Vector3(-w*.40f,h*.52f,-.08f),Vector3.one*.075f,metal);
            Transform reveal=Group(root.transform,"contents");Box("visible loot",reveal,new Vector3(0,.66f,.02f),new Vector3(.78f,.18f,.30f),metal,false);reveal.gameObject.SetActive(false);
            LootContainerV74 c=root.AddComponent<LootContainerV74>();c.displayName=name;c.radius=2.3f;c.movingPart=left;c.movingPart2=right;c.closedEuler=Vector3.zero;c.closedEuler2=Vector3.zero;c.openEuler=new Vector3(0,88,0);c.openEuler2=new Vector3(0,-88,0);c.revealOnOpen=reveal;c.highlightRenderer=leftDoor.GetComponent<Renderer>();c.items=items;c.counts=counts;c.animationTime=.32f;
        }

        static void SingleCabinet(Transform p,string name,Vector3 pos,float yaw,Vector3 size,Material body,Material metal,string[] items,int[] counts){
            GameObject root=new GameObject(name);root.transform.SetParent(p,true);root.transform.position=pos;root.transform.rotation=Quaternion.Euler(0,yaw,0);
            Box("body",root.transform,new Vector3(0,size.y*.5f,.05f),size,body,true);
            Transform hinge=Group(root.transform,"left hinge");hinge.localPosition=new Vector3(-size.x*.50f,0,-size.z*.52f);
            GameObject door=Box("door",hinge,new Vector3(size.x*.5f,size.y*.5f,0),new Vector3(size.x*.98f,size.y*.94f,.06f),body,true);
            Sphere("handle",hinge,new Vector3(size.x*.88f,size.y*.52f,-.08f),Vector3.one*.07f,metal);
            Transform reveal=Group(root.transform,"contents");Box("visible contents",reveal,new Vector3(0,size.y*.48f,-size.z*.18f),new Vector3(size.x*.58f,.16f,size.z*.35f),metal,false);reveal.gameObject.SetActive(false);
            LootContainerV74 c=root.AddComponent<LootContainerV74>();c.displayName=name;c.radius=2.25f;c.movingPart=hinge;c.closedEuler=Vector3.zero;c.openEuler=new Vector3(0,88,0);c.revealOnOpen=reveal;c.highlightRenderer=door.GetComponent<Renderer>();c.items=items;c.counts=counts;c.animationTime=.32f;
        }

        static void Crate(Transform p,string name,Vector3 pos,float yaw,Material body,Material metal,string[] items,int[] counts){
            GameObject root=new GameObject(name);root.transform.SetParent(p,true);root.transform.position=pos;root.transform.rotation=Quaternion.Euler(0,yaw,0);
            GameObject shell=Box("crate",root.transform,new Vector3(0,.28f,0),new Vector3(1.05f,.52f,.72f),body,true);
            Transform lid=Group(root.transform,"lid hinge");lid.localPosition=new Vector3(0,.55f,.34f);Box("lid",lid,new Vector3(0,.03f,-.34f),new Vector3(1.10f,.09f,.74f),metal,true);
            Transform reveal=Group(root.transform,"contents");Box("tools",reveal,new Vector3(0,.56f,0),new Vector3(.62f,.12f,.34f),metal,false);reveal.gameObject.SetActive(false);
            LootContainerV74 c=root.AddComponent<LootContainerV74>();c.displayName=name;c.radius=2.2f;c.movingPart=lid;c.closedEuler=Vector3.zero;c.openEuler=new Vector3(-82,0,0);c.revealOnOpen=reveal;c.highlightRenderer=shell.GetComponent<Renderer>();c.items=items;c.counts=counts;c.animationTime=.30f;
        }

        static void Door(Transform p,string name,Vector3 pos,float yaw,float width,Material body,Material inset,Material trim,Material metal,float angle){
            GameObject root=new GameObject("door — "+name);root.transform.SetParent(p,true);root.transform.position=pos;root.transform.rotation=Quaternion.Euler(0,yaw,0);
            float h=1.95f;
            Box("frame L",root.transform,new Vector3(-width*.56f,h*.5f,0),new Vector3(.13f,h,.18f),trim,true);
            Box("frame R",root.transform,new Vector3(width*.56f,h*.5f,0),new Vector3(.13f,h,.18f),trim,true);
            Box("frame top",root.transform,new Vector3(0,h,width*.0f),new Vector3(width*1.24f,.14f,.18f),trim,true);
            Transform hinge=Group(root.transform,"door hinge");hinge.localPosition=new Vector3(-width*.50f,0,-.03f);
            GameObject leaf=Box("door leaf",hinge,new Vector3(width*.5f,h*.5f,0),new Vector3(width,h,.10f),body,true);
            Box("door inset",hinge,new Vector3(width*.52f,h*.58f,-.058f),new Vector3(width*.63f,h*.36f,.02f),inset,false);
            Sphere("handle",hinge,new Vector3(width*.86f,h*.50f,-.09f),Vector3.one*.075f,metal);
            CozyInteractableV57 x=root.AddComponent<CozyInteractableV57>();x.kind=CozyInteractableV57.Kind.Door;x.displayName=name;x.radius=2.0f;x.movingPart=hinge;x.closedEuler=Vector3.zero;x.openEuler=new Vector3(0,angle,0);x.highlightRenderer=leaf.GetComponent<Renderer>();x.animationTime=.30f;
        }

        static void Window(Transform p,Vector3 pos,float yaw,float width,Material wall,Material trim,Material glass){
            GameObject root=new GameObject("window");root.transform.SetParent(p,true);root.transform.position=pos;root.transform.rotation=Quaternion.Euler(0,yaw,0);
            Box("glass",root.transform,Vector3.zero,new Vector3(width,1.05f,.07f),glass,false);
            Box("top",root.transform,new Vector3(0,.58f,-.02f),new Vector3(width+.18f,.10f,.12f),trim,false);
            Box("bottom",root.transform,new Vector3(0,-.58f,-.02f),new Vector3(width+.18f,.10f,.12f),trim,false);
            Box("left",root.transform,new Vector3(-width*.5f-.04f,0,-.02f),new Vector3(.10f,1.26f,.12f),trim,false);
            Box("right",root.transform,new Vector3(width*.5f+.04f,0,-.02f),new Vector3(.10f,1.26f,.12f),trim,false);
            Box("mullion",root.transform,new Vector3(0,0,-.03f),new Vector3(.06f,1.12f,.13f),trim,false);
        }

        static void Radiator(Transform p,Vector3 pos,float yaw,Material white,Material metal){
            GameObject r=new GameObject("radiator");r.transform.SetParent(p,true);r.transform.position=pos;r.transform.rotation=Quaternion.Euler(0,yaw,0);
            for(int i=0;i<7;i++)Box("fin",r.transform,new Vector3((i-3)*.16f,0,0),new Vector3(.11f,.70f,.12f),white,false);
            Box("pipe",r.transform,new Vector3(.62f,-.28f,.02f),new Vector3(.07f,.35f,.07f),metal,false);
        }

        static void PlaceAsset(string key,string name,Transform p,Vector3 pos,float yaw,float scale){
            string[] guids=AssetDatabase.FindAssets(key);
            foreach(string g in guids){string path=AssetDatabase.GUIDToAssetPath(g);if(!path.EndsWith(".obj",StringComparison.OrdinalIgnoreCase)&&!path.EndsWith(".fbx",StringComparison.OrdinalIgnoreCase)&&!path.EndsWith(".prefab",StringComparison.OrdinalIgnoreCase))continue;GameObject model=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!model)continue;GameObject x=(GameObject)PrefabUtility.InstantiatePrefab(model);if(!x)x=UnityEngine.Object.Instantiate(model);x.name=name;x.transform.SetParent(p,true);x.transform.position=pos;x.transform.rotation=Quaternion.Euler(0,yaw,0);x.transform.localScale=Vector3.one*scale;return;}
        }

        static Shader PickShader(){Shader s=GraphicsSettings.currentRenderPipeline!=null||GraphicsSettings.defaultRenderPipeline!=null?Shader.Find("Universal Render Pipeline/Lit"):Shader.Find("Standard");if(!s||!s.isSupported)s=Shader.Find("Universal Render Pipeline/Simple Lit");if(!s||!s.isSupported)s=Shader.Find("Standard");return s;}
        static Material Mat(Shader s,Color c){Material m=new Material(s);if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);if(m.HasProperty("_Color"))m.SetColor("_Color",c);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",.14f);return m;}
        static Color C(string hex){ColorUtility.TryParseHtmlString("#"+hex,out Color c);return c;}
        static Transform Group(Transform p,string n){GameObject g=new GameObject(n);g.transform.SetParent(p,false);return g.transform;}
        static void Floor(string n,Transform p,Vector3 pos,Vector3 size,Material m){Box(n,p,pos,size,m,true);}
        static GameObject Box(string n,Transform p,Vector3 pos,Vector3 size,Material m,bool collider){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;if(!collider){Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);}return g;}
        static GameObject Sphere(string n,Transform p,Vector3 pos,Vector3 size,Material m){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);return g;}
        static void WallX(Transform p,float x0,float x1,float z,float h,Material wall,Material trim){float len=x1-x0;float y=h*.5f;Box("wall",p,new Vector3((x0+x1)*.5f,y,z),new Vector3(len,h,.18f),wall,true);Box("wall cap",p,new Vector3((x0+x1)*.5f,h+.035f,z),new Vector3(len+.05f,.07f,.23f),trim,false);}
        static void WallZ(Transform p,float x,float z0,float z1,float h,Material wall,Material trim){float len=z1-z0;float y=h*.5f;Box("wall",p,new Vector3(x,y,(z0+z1)*.5f),new Vector3(.18f,h,len),wall,true);Box("wall cap",p,new Vector3(x,h+.035f,(z0+z1)*.5f),new Vector3(.23f,.07f,len+.05f),trim,false);}
    }
}
#endif