#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Rosvik.Blackout;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikWorldEnvironmentV78 {
        const int Version = 78;
        const string Key = "ROSVIK_WORLD_ENVIRONMENT_V78";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string GroupName = "V78 WORLD - GARAGE + VEGETATION";
        const string MatDir = "Assets/Rosvik/GeneratedV78";

        static RosvikWorldEnvironmentV78(){ if(EditorPrefs.GetInt(Key,0)>=Version)return; EditorApplication.delayCall+=Auto; }
        [MenuItem("Rosvik/V78 WORLD + GARAGE + VEGETATION")]
        public static void Force(){ EditorPrefs.DeleteKey(Key); EditorApplication.delayCall+=Auto; }

        static void Auto(){
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=Auto;return;}
            if(!File.Exists(ScenePath))return;
            try{Apply();}catch(Exception ex){Debug.LogError("V78 FAILED: "+ex);}
        }

        static void Apply(){
            if(EditorSceneManager.GetActiveScene().path!=ScenePath)EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            CoziPlayerV57 player=UnityEngine.Object.FindFirstObjectByType<CoziPlayerV57>();
            if(!player)throw new Exception("PLAYER missing");

            RepairInteriorDoors(player);

            GameObject old=GameObject.Find(GroupName);if(old)UnityEngine.Object.DestroyImmediate(old);
            GameObject root=new GameObject(GroupName);

            Shader shader=PickShader();
            Material grass=Mat(shader,"ground","2e4437");
            Material grassDark=Mat(shader,"grass_dark","24382d");
            Material pine=Mat(shader,"pine","324f3d");
            Material pineDark=Mat(shader,"pine_dark","263d31");
            Material bark=Mat(shader,"bark","5d4938");
            Material birch=Mat(shader,"birch","c9c3ae");
            Material branch=Mat(shader,"branch","58493b");
            Material snow=Mat(shader,"snow","c9d0c9");
            Material road=Mat(shader,"road","5b5c57");
            Material roadEdge=Mat(shader,"road_edge","858077");
            Material concrete=Mat(shader,"concrete","686d69");
            Material wall=Mat(shader,"garage_wall","9d927d");
            Material trim=Mat(shader,"trim","d0c4a8");
            Material wood=Mat(shader,"wood","79563c");
            Material metal=Mat(shader,"metal","555e5d");
            Material darkMetal=Mat(shader,"dark_metal","343c3c");
            Material rust=Mat(shader,"rust","7b4b35");
            Material glass=Mat(shader,"glass","557278");
            Material warm=Mat(shader,"warm","c99b52");

            Transform land=Group(root.transform,"LANDSCAPE");
            Transform garage=Group(root.transform,"GARAGE B");
            Transform veg=Group(root.transform,"VEGETATION");
            Transform dressing=Group(root.transform,"WORLD DRESSING");

            BuildLand(land,grass,road,roadEdge,snow);
            BuildGarage(garage,wall,trim,concrete,wood,metal,darkMetal,rust,glass,warm);
            BuildVegetation(veg,bark,pine,pineDark,birch,branch,grassDark,snow);
            BuildWorldDressing(dressing,wood,metal,rust,snow,roadEdge);

            WorldExplorationV78 exploration=player.GetComponent<WorldExplorationV78>();
            if(!exploration)exploration=player.gameObject.AddComponent<WorldExplorationV78>();
            exploration.enabled=true;
            exploration.garageMinXZ=new Vector2(51.7f,4.55f);
            exploration.garageMaxXZ=new Vector2(65.2f,15.25f);

            player.SetObjective("Utforska vidare. Ett garage öster om sporthallen kan innehålla verktyg, bränsle och reservdelar.");
            EditorUtility.SetDirty(player);EditorUtility.SetDirty(exploration);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();EditorPrefs.SetInt(Key,Version);SceneView.RepaintAll();
            Debug.Log("V78 COMPLETE — interior doors hardened, east garage/road added, and the world now has a real winter treeline with pines, birches, shrubs, rocks, snow and roadside dressing.");
        }

        static void RepairInteriorDoors(CoziPlayerV57 player){
            HouseInteriorDoorNetworkV77 network=player.GetComponent<HouseInteriorDoorNetworkV77>();
            if(!network)network=player.gameObject.AddComponent<HouseInteriorDoorNetworkV77>();
            network.enabled=true;network.interactionDistance=2.85f;
            string[] names={"köket","sovrummet","inre dörr"};float[] angles={94f,-94f,94f};
            for(int i=0;i<names.Length;i++){
                GameObject root=GameObject.Find("door — "+names[i]);if(!root)continue;
                CozyInteractableV57 legacy=root.GetComponent<CozyInteractableV57>();
                Transform hinge=legacy&&legacy.movingPart?legacy.movingPart:root.transform.Find("door hinge");if(!hinge)continue;
                HouseInteriorDoorV77 d=root.GetComponent<HouseInteriorDoorV77>();if(!d)d=root.AddComponent<HouseInteriorDoorV77>();
                d.displayName=names[i];d.hinge=hinge;d.closedEuler=Vector3.zero;d.openEuler=new Vector3(0,angles[i],0);d.animationTime=.22f;
                if(legacy)UnityEngine.Object.DestroyImmediate(legacy);
                DoorPassageV76 pass=root.GetComponent<DoorPassageV76>();if(pass)UnityEngine.Object.DestroyImmediate(pass);
                ClearOpening(root.transform);
                EditorUtility.SetDirty(d);
            }
            network.Refresh();EditorUtility.SetDirty(network);
        }

        static void ClearOpening(Transform door){
            Vector3 p=door.position;
            foreach(BoxCollider c in UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
                if(!c||c.transform.IsChildOf(door))continue;Bounds b=c.bounds;
                Vector3 flat=b.center-p;flat.y=0;if(flat.sqrMagnitude>1.18f*1.18f)continue;
                string n=c.gameObject.name.ToLowerInvariant();if(n.Contains("floor")||n.Contains("ground")||n.Contains("rug"))continue;
                bool blocker=(b.size.x<.34f&&b.size.z>1.0f)||(b.size.z<.34f&&b.size.x>1.0f);
                if(blocker){c.enabled=false;EditorUtility.SetDirty(c);}
            }
        }

        static void BuildLand(Transform p,Material grass,Material road,Material edge,Material snow){
            Floor("east ground",p,new Vector3(60f,-.085f,6f),new Vector3(38f,.14f,34f),grass);
            Floor("north green link",p,new Vector3(40f,-.09f,19.5f),new Vector3(38f,.12f,8f),grass);
            Floor("garage road",p,new Vector3(57f,-.005f,.1f),new Vector3(28f,.07f,5.5f),road);
            Floor("garage driveway",p,new Vector3(58.5f,.005f,3.25f),new Vector3(8.2f,.075f,6.1f),road);
            Floor("road shoulder north",p,new Vector3(57f,.012f,3.05f),new Vector3(28f,.03f,.34f),edge);
            Floor("road shoulder south",p,new Vector3(57f,.012f,-2.85f),new Vector3(28f,.03f,.34f),edge);
            for(int i=0;i<11;i++)Floor("snow shoulder",p,new Vector3(46f+i*2.45f,.024f,-3.25f+(i%2)*.16f),new Vector3(1.65f,.018f,.42f),snow);
        }

        static void BuildGarage(Transform p,Material wall,Material trim,Material concrete,Material wood,Material metal,Material darkMetal,Material rust,Material glass,Material warm){
            const float minX=51.7f,maxX=65.2f,front=4.45f,back=15.2f,h=1.28f;
            Floor("garage floor",p,new Vector3(58.45f,.025f,9.83f),new Vector3(maxX-minX,.08f,back-front),concrete);
            WallX(p,minX,maxX,back,h,wall,trim);WallZ(p,minX,front,back,h,wall,trim);WallZ(p,maxX,front,back,h,wall,trim);
            WallX(p,minX,54.7f,front,h,wall,trim);WallX(p,62.2f,maxX,front,h,wall,trim);
            Box("garage header",p,new Vector3(58.45f,1.72f,front),new Vector3(7.55f,.30f,.22f),trim,true);
            Box("garage door rail L",p,new Vector3(54.88f,.84f,4.52f),new Vector3(.11f,1.55f,.18f),metal,false);
            Box("garage door rail R",p,new Vector3(62.02f,.84f,4.52f),new Vector3(.11f,1.55f,.18f),metal,false);

            // Workbench zone.
            Box("workbench top",p,new Vector3(54.0f,.78f,13.85f),new Vector3(3.2f,.16f,.82f),wood,true);
            Box("bench leg L",p,new Vector3(52.75f,.38f,13.85f),new Vector3(.18f,.78f,.64f),metal,true);
            Box("bench leg R",p,new Vector3(55.25f,.38f,13.85f),new Vector3(.18f,.78f,.64f),metal,true);
            for(int i=0;i<6;i++)Box("tool peg",p,new Vector3(52.75f+i*.48f,1.38f,14.72f),new Vector3(.08f,.48f,.08f),i%2==0?metal:rust,false);
            Box("wall tool board",p,new Vector3(54.0f,1.34f,14.93f),new Vector3(3.4f,.82f,.07f),wood,false);

            // Storage shelves.
            for(int s=0;s<3;s++){
                float z=7.0f+s*2.1f;
                Box("shelf upright",p,new Vector3(64.35f,.82f,z),new Vector3(.10f,1.6f,.72f),metal,true);
                Box("shelf",p,new Vector3(63.70f,.42f,z),new Vector3(1.3f,.08f,.72f),metal,true);
                Box("shelf",p,new Vector3(63.70f,.90f,z),new Vector3(1.3f,.08f,.72f),metal,true);
                Box("shelf",p,new Vector3(63.70f,1.38f,z),new Vector3(1.3f,.08f,.72f),metal,true);
            }

            // Battery work lamp: warm pool in otherwise dim garage.
            Box("lamp stand",p,new Vector3(59.0f,.65f,13.9f),new Vector3(.08f,1.3f,.08f),darkMetal,false);
            GameObject bulb=Sphere("battery work lamp",p,new Vector3(59.0f,1.35f,13.9f),new Vector3(.20f,.16f,.20f),warm,false);
            Light l=bulb.AddComponent<Light>();l.type=LightType.Point;l.range=5.8f;l.intensity=2.2f;l.color=new Color(1f,.72f,.40f);l.shadows=LightShadows.Soft;

            Crate(p,"garagets verktygslåda",new Vector3(55.0f,0,11.55f),0,wood,metal,new[]{"Multiverktyg","Tejp","Metallskrot","Batterier"},new[]{1,2,3,2});
            Crate(p,"bränslelådan",new Vector3(61.2f,0,12.4f),90,darkMetal,rust,new[]{"Bränsle","Tändare","Träspill"},new[]{2,1,2});
            Crate(p,"garagets första hjälpen",new Vector3(52.65f,0,6.1f),0,metal,trim,new[]{"Förband","Antiseptisk spray","Elastisk linda"},new[]{2,1,1});
            Crate(p,"arbetskläder",new Vector3(63.7f,0,5.5f),90,wood,darkMetal,new[]{"Handskar","Regnjacka","Kängor"},new[]{1,1,1});

            // Small dirty window and side bench for visual identity.
            Box("garage window frame",p,new Vector3(58.3f,1.35f,15.06f),new Vector3(2.5f,.95f,.10f),trim,false);
            Box("garage window",p,new Vector3(58.3f,1.35f,15.00f),new Vector3(2.18f,.70f,.035f),glass,false);
            Box("side bench",p,new Vector3(61.0f,.42f,6.0f),new Vector3(2.2f,.16f,.70f),wood,true);
        }

        static void BuildVegetation(Transform p,Material bark,Material pine,Material pineDark,Material birch,Material branch,Material grass,Material snow){
            Vector3[] pines={
                new Vector3(70,-.02f,-7),new Vector3(74,-.02f,-4),new Vector3(77,-.02f,0),new Vector3(72,-.02f,4),new Vector3(76,-.02f,8),new Vector3(71,-.02f,12),new Vector3(75,-.02f,16),new Vector3(69,-.02f,20),new Vector3(73,-.02f,23),
                new Vector3(49,-.02f,21),new Vector3(54,-.02f,23),new Vector3(59,-.02f,21),new Vector3(64,-.02f,24),
                new Vector3(47,-.02f,-8),new Vector3(52,-.02f,-10),new Vector3(58,-.02f,-8),new Vector3(64,-.02f,-10),new Vector3(69,-.02f,-8),
                new Vector3(-40,-.02f,3),new Vector3(-42,-.02f,8),new Vector3(-40,-.02f,14),new Vector3(-38,-.02f,18),
                new Vector3(18,-.02f,22),new Vector3(25,-.02f,23),new Vector3(33,-.02f,22),new Vector3(42,-.02f,23)
            };
            for(int i=0;i<pines.Length;i++)Pine("gran",p,pines[i],.88f+(i%5)*.065f,bark,i%2==0?pine:pineDark,snow);

            Vector3[] birches={new Vector3(67,0,-5),new Vector3(68,0,7),new Vector3(66,0,18),new Vector3(78,0,13),new Vector3(56,0,19),new Vector3(45,0,20),new Vector3(-39,0,10),new Vector3(-41,0,16),new Vector3(11,0,21)};
            for(int i=0;i<birches.Length;i++)Birch("björk",p,birches[i],.9f+(i%3)*.08f,birch,branch,snow);

            Vector3[] shrubs={new Vector3(49,0,17),new Vector3(52,0,18),new Vector3(66,0,16),new Vector3(68,0,14),new Vector3(69,0,5),new Vector3(70,0,-3),new Vector3(61,0,-6),new Vector3(55,0,-5),new Vector3(-38,0,5),new Vector3(-39,0,12),new Vector3(-37,0,16),new Vector3(44,0,18)};
            for(int i=0;i<shrubs.Length;i++)Shrub("buske",p,shrubs[i],.75f+(i%4)*.08f,grass,snow);

            Vector3[] rocks={new Vector3(68,0,2),new Vector3(73,0,11),new Vector3(48,0,18),new Vector3(63,0,20),new Vector3(-39,0,7),new Vector3(46,0,-6)};
            for(int i=0;i<rocks.Length;i++)Rock("sten",p,rocks[i],.55f+(i%3)*.12f,branch,snow);

            // Small deliberate grass clumps along untouched edges, never in the road/doorway.
            for(int i=0;i<18;i++){
                float x=47f+(i%6)*4.1f;float z=i<6?17.8f:(i<12?-5.4f:20.2f);
                GrassTuft("vintergräs",p,new Vector3(x,0,z+(i%2)*.35f),.55f+(i%3)*.08f,grass);
            }
        }

        static void BuildWorldDressing(Transform p,Material wood,Material metal,Material rust,Material snow,Material edge){
            // Fence and wood pile make the garage feel owned/used before the blackout.
            for(int i=0;i<7;i++){
                float x=49f+i*2.0f;Box("fence post",p,new Vector3(x,.55f,17.2f),new Vector3(.10f,1.1f,.10f),wood,true);
                if(i<6){Box("fence rail",p,new Vector3(x+1f,.62f,17.2f),new Vector3(1.9f,.10f,.08f),wood,false);Box("fence rail",p,new Vector3(x+1f,.95f,17.2f),new Vector3(1.9f,.10f,.08f),wood,false);}
            }
            for(int i=0;i<8;i++){
                GameObject log=GameObject.CreatePrimitive(PrimitiveType.Cylinder);log.name="vedträ";log.transform.SetParent(p,true);log.transform.position=new Vector3(65.9f+(i%4)*.32f,.20f,13.2f+(i/4)*.34f);log.transform.rotation=Quaternion.Euler(0,0,90);log.transform.localScale=new Vector3(.15f,.55f,.15f);log.GetComponent<Renderer>().sharedMaterial=wood;Collider c=log.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);
            }
            Car(p,new Vector3(69.0f,.10f,5.2f),90,metal,rust);
            Box("road sign post",p,new Vector3(47.0f,.72f,-2.1f),new Vector3(.09f,1.45f,.09f),metal,true);
            Box("road sign",p,new Vector3(47.0f,1.35f,-2.1f),new Vector3(.62f,.40f,.08f),edge,false);
            Floor("snow drift",p,new Vector3(68.8f,.035f,6.5f),new Vector3(3.2f,.03f,.75f),snow);
        }

        static void Crate(Transform p,string name,Vector3 pos,float yaw,Material body,Material lidMat,string[] items,int[] counts){
            GameObject root=new GameObject(name);root.transform.SetParent(p,true);root.transform.position=pos;root.transform.rotation=Quaternion.Euler(0,yaw,0);
            GameObject shell=Box("crate body",root.transform,new Vector3(0,.34f,0),new Vector3(1.15f,.64f,.78f),body,true);
            Transform lid=Group(root.transform,"lid hinge");lid.localPosition=new Vector3(0,.66f,.37f);Box("lid",lid,new Vector3(0,.03f,-.37f),new Vector3(1.18f,.10f,.78f),lidMat,true);
            Transform reveal=Group(root.transform,"contents");Box("visible contents",reveal,new Vector3(0,.58f,0),new Vector3(.70f,.12f,.36f),lidMat,false);reveal.gameObject.SetActive(false);
            LootContainerV74 c=root.AddComponent<LootContainerV74>();c.displayName=name;c.radius=2.25f;c.movingPart=lid;c.closedEuler=Vector3.zero;c.openEuler=new Vector3(-82,0,0);c.revealOnOpen=reveal;c.highlightRenderer=shell.GetComponent<Renderer>();c.items=items;c.counts=counts;c.animationTime=.28f;
        }

        static void Pine(string n,Transform p,Vector3 pos,float s,Material bark,Material needles,Material snow){
            GameObject root=new GameObject(n);root.transform.SetParent(p,true);root.transform.position=pos;root.transform.localScale=Vector3.one*s;
            GameObject trunk=GameObject.CreatePrimitive(PrimitiveType.Cylinder);trunk.name="trunk";trunk.transform.SetParent(root.transform,false);trunk.transform.localPosition=new Vector3(0,1.1f,0);trunk.transform.localScale=new Vector3(.20f,1.1f,.20f);trunk.GetComponent<Renderer>().sharedMaterial=bark;
            Foliage(root.transform,new Vector3(0,1.65f,0),new Vector3(2.35f,.72f,2.35f),needles);
            Foliage(root.transform,new Vector3(0,2.35f,0),new Vector3(1.85f,.68f,1.85f),needles);
            Foliage(root.transform,new Vector3(0,2.95f,0),new Vector3(1.25f,.60f,1.25f),needles);
            Foliage(root.transform,new Vector3(0,3.38f,0),new Vector3(.64f,.46f,.64f),snow);
        }
        static void Foliage(Transform p,Vector3 lp,Vector3 sc,Material m){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name="crown";g.transform.SetParent(p,false);g.transform.localPosition=lp;g.transform.localScale=sc;g.GetComponent<Renderer>().sharedMaterial=m;Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);}
        static void Birch(string n,Transform p,Vector3 pos,float s,Material white,Material branch,Material snow){
            GameObject root=new GameObject(n);root.transform.SetParent(p,true);root.transform.position=pos;root.transform.localScale=Vector3.one*s;
            GameObject trunk=GameObject.CreatePrimitive(PrimitiveType.Cylinder);trunk.name="white trunk";trunk.transform.SetParent(root.transform,false);trunk.transform.localPosition=new Vector3(0,1.45f,0);trunk.transform.localScale=new Vector3(.13f,1.45f,.13f);trunk.GetComponent<Renderer>().sharedMaterial=white;
            for(int i=0;i<5;i++){float y=1.8f+i*.34f;GameObject b=Box("branch",root.transform,new Vector3((i%2==0?.28f:-.28f),y,0),new Vector3(.72f,.065f,.065f),branch,false);b.transform.localRotation=Quaternion.Euler(0,(i*37)%120,i%2==0?28:-28);}
            Foliage(root.transform,new Vector3(0,3.0f,0),new Vector3(1.05f,.40f,1.05f),snow);
        }
        static void Shrub(string n,Transform p,Vector3 pos,float s,Material m,Material snow){GameObject r=new GameObject(n);r.transform.SetParent(p,true);r.transform.position=pos;for(int i=0;i<3;i++){Foliage(r.transform,new Vector3((i-1)*.32f,.28f+(i%2)*.12f,(i%2)*.18f),new Vector3(.72f,.40f,.64f)*s,m);}Foliage(r.transform,new Vector3(0,.55f,0),new Vector3(.62f,.12f,.48f)*s,snow);}
        static void Rock(string n,Transform p,Vector3 pos,float s,Material rock,Material snow){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=n;g.transform.SetParent(p,true);g.transform.position=pos+Vector3.up*.22f;g.transform.localScale=new Vector3(1.1f,.55f,.85f)*s;g.GetComponent<Renderer>().sharedMaterial=rock;Foliage(g.transform,new Vector3(0,.32f,0),new Vector3(.72f,.12f,.55f),snow);}
        static void GrassTuft(string n,Transform p,Vector3 pos,float s,Material m){GameObject root=new GameObject(n);root.transform.SetParent(p,true);root.transform.position=pos;for(int i=0;i<4;i++){GameObject g=Box("blade",root.transform,new Vector3((i-1.5f)*.08f,.22f,0),new Vector3(.045f,.44f,.06f)*s,m,false);g.transform.localRotation=Quaternion.Euler(i%2==0?10:-10,i*43,i%2==0?12:-12);}}
        static void Car(Transform p,Vector3 pos,float yaw,Material body,Material rust){GameObject r=new GameObject("övergiven bil");r.transform.SetParent(p,true);r.transform.position=pos;r.transform.rotation=Quaternion.Euler(0,yaw,0);Box("car body",r.transform,new Vector3(0,.42f,0),new Vector3(3.6f,.62f,1.65f),body,true);Box("car cabin",r.transform,new Vector3(-.15f,.90f,0),new Vector3(1.9f,.58f,1.45f),body,true);Box("rust patch",r.transform,new Vector3(.95f,.76f,-.84f),new Vector3(.72f,.08f,.04f),rust,false);for(int i=0;i<4;i++){float x=i<2?-1.15f:1.15f;float z=i%2==0?-.78f:.78f;GameObject w=GameObject.CreatePrimitive(PrimitiveType.Cylinder);w.name="wheel";w.transform.SetParent(r.transform,false);w.transform.localPosition=new Vector3(x,.28f,z);w.transform.localRotation=Quaternion.Euler(90,0,0);w.transform.localScale=new Vector3(.32f,.16f,.32f);w.GetComponent<Renderer>().sharedMaterial=rust;}}

        static Material Mat(Shader shader,string name,string hex){Directory.CreateDirectory(MatDir);string path=MatDir+"/"+name+".mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;m=new Material(shader);Color c=Color.white;ColorUtility.TryParseHtmlString("#"+hex,out c);if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);if(m.HasProperty("_Color"))m.SetColor("_Color",c);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",.18f);AssetDatabase.CreateAsset(m,path);return m;}
        static Shader PickShader(){Shader s=Shader.Find("Universal Render Pipeline/Lit");if(s)return s;s=Shader.Find("Standard");if(s)return s;throw new Exception("No supported lit shader");}
        static Transform Group(Transform p,string n){GameObject g=new GameObject(n);g.transform.SetParent(p,true);return g.transform;}
        static GameObject Box(string n,Transform p,Vector3 pos,Vector3 size,Material m,bool collider){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;if(!collider){Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);}return g;}
        static GameObject Sphere(string n,Transform p,Vector3 pos,Vector3 size,Material m,bool collider){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;if(!collider){Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);}return g;}
        static void Floor(string n,Transform p,Vector3 pos,Vector3 size,Material m){Box(n,p,pos,size,m,true);}
        static void WallX(Transform p,float x0,float x1,float z,float h,Material wall,Material trim){float len=x1-x0;Box("wall",p,new Vector3((x0+x1)*.5f,h*.5f,z),new Vector3(len,h,.18f),wall,true);Box("wall cap",p,new Vector3((x0+x1)*.5f,h+.035f,z),new Vector3(len+.05f,.07f,.23f),trim,false);}
        static void WallZ(Transform p,float x,float z0,float z1,float h,Material wall,Material trim){float len=z1-z0;Box("wall",p,new Vector3(x,h*.5f,(z0+z1)*.5f),new Vector3(.18f,h,len),wall,true);Box("wall cap",p,new Vector3(x,h+.035f,(z0+z1)*.5f),new Vector3(.23f,.07f,len+.05f),trim,false);}
    }
}
#endif
