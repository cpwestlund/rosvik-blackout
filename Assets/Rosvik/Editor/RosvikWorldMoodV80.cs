#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Rosvik.Blackout;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikWorldMoodV80 {
        const int Version=80;
        const string Key="ROSVIK_WORLD_MOOD_V80";
        const string ScenePath="Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string GroupName="V80 COZY WINTER WORLD + SOUND";
        const string MatDir="Assets/Rosvik/GeneratedV80";

        static RosvikWorldMoodV80(){if(EditorPrefs.GetInt(Key,0)>=Version)return;EditorApplication.delayCall+=Auto;}
        [MenuItem("Rosvik/V80 COZY WINTER WORLD + SOUND")]
        public static void Force(){EditorPrefs.DeleteKey(Key);EditorApplication.delayCall+=Auto;}
        static void Auto(){
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=Auto;return;}
            if(!File.Exists(ScenePath))return;
            try{Apply();}catch(Exception ex){Debug.LogError("V80 FAILED: "+ex);}
        }

        static void Apply(){
            if(EditorSceneManager.GetActiveScene().path!=ScenePath)EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            CoziPlayerV57 player=UnityEngine.Object.FindFirstObjectByType<CoziPlayerV57>();if(!player)throw new Exception("PLAYER missing");
            GameObject old=GameObject.Find(GroupName);if(old)UnityEngine.Object.DestroyImmediate(old);
            GameObject root=new GameObject(GroupName);

            Shader shader=PickShader();
            Material pine=Mat(shader,"pine","233d2f",.04f);Material pine2=Mat(shader,"pine2","2d4937",.05f);
            Material bark=Mat(shader,"bark","514235",.07f);Material birch=Mat(shader,"birch","918f82",.06f);Material branch=Mat(shader,"branch","403830",.05f);
            Material shrub=Mat(shader,"shrub","304938",.04f);Material dry=Mat(shader,"drygrass","6f694f",.03f);Material snow=Mat(shader,"snow","9eada9",.10f);
            Material wood=Mat(shader,"wood","6c4b37",.10f);Material woodDark=Mat(shader,"wood_dark","46352b",.08f);Material metal=Mat(shader,"metal","4c5757",.18f);Material rust=Mat(shader,"rust","714b38",.12f);
            Material road=Mat(shader,"road_patch","535954",.10f);Material blue=Mat(shader,"bluegrey","53656a",.08f);Material black=Mat(shader,"black","252d2c",.12f);

            Transform forest=Group(root.transform,"FOREST CLUSTERS");
            Transform school=Group(root.transform,"SCHOOL EDGE DETAILS");
            Transform houseA=Group(root.transform,"HOUSE A YARD");
            Transform north=Group(root.transform,"NORTH NEIGHBORHOOD DETAILS");
            Transform garage=Group(root.transform,"GARAGE YARD DETAILS");
            Transform roads=Group(root.transform,"ROAD + SNOW DETAILS");

            BuildForest(forest,bark,pine,pine2,birch,branch,shrub,dry,snow);
            BuildSchoolEdge(school,wood,metal,rust,shrub,snow,dry);
            BuildHouseAYard(houseA,wood,woodDark,metal,rust,shrub,snow);
            BuildNorthDetails(north,wood,metal,rust,shrub,snow,dry);
            BuildGarageYard(garage,wood,woodDark,metal,rust,black,snow);
            BuildRoadDetails(roads,road,metal,blue,snow);

            WorldSoundscapeV80 audio=player.GetComponent<WorldSoundscapeV80>();if(!audio)audio=player.gameObject.AddComponent<WorldSoundscapeV80>();
            audio.enabled=true;audio.masterVolume=.82f;audio.windVolume=.34f;audio.footstepVolume=.48f;audio.interactionVolume=.55f;EditorUtility.SetDirty(audio);

            // Keep the blackout mood, but avoid a completely flat black world at dusk/night.
            RenderSettings.ambientIntensity=Mathf.Min(RenderSettings.ambientIntensity,.82f);
            foreach(Light l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
                if(!l||l.type!=LightType.Directional)continue;
                l.shadowStrength=Mathf.Max(l.shadowStrength,.72f);
                EditorUtility.SetDirty(l);
                break;
            }

            player.SetObjective("Fortsätt utforska samhället. Lyssna efter väder och miljö — skydd, utrustning och avstånd spelar större roll ju längre du går.");
            EditorUtility.SetDirty(player);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();EditorPrefs.SetInt(Key,Version);SceneView.RepaintAll();
            Debug.Log("V80 COMPLETE — winter world density upgraded with forest clusters, yard/street storytelling and a procedural soundscape: wind, snow/gravel/indoor footsteps, doors, cabinets, loot and distant winter creaks.");
        }

        static void BuildForest(Transform p,Material bark,Material pine,Material pine2,Material birch,Material branch,Material shrub,Material dry,Material snow){
            // Dense edges instead of isolated trees. Centers stay readable and walkable.
            Vector3[] a={
                new Vector3(-50,0,-10),new Vector3(-47,0,-7),new Vector3(-51,0,-3),new Vector3(-46,0,1),new Vector3(-49,0,5),new Vector3(-45,0,9),new Vector3(-50,0,13),new Vector3(-46,0,17),new Vector3(-49,0,21),new Vector3(-45,0,25),new Vector3(-50,0,29),new Vector3(-46,0,34),new Vector3(-49,0,39),
                new Vector3(8,0,27),new Vector3(13,0,29),new Vector3(18,0,27),new Vector3(23,0,30),new Vector3(28,0,28),new Vector3(34,0,30),new Vector3(40,0,28),new Vector3(46,0,31),
                new Vector3(72,0,-12),new Vector3(76,0,-9),new Vector3(80,0,-5),new Vector3(78,0,0),new Vector3(81,0,5),new Vector3(78,0,10),new Vector3(81,0,15),new Vector3(77,0,20),new Vector3(80,0,25)
            };
            for(int i=0;i<a.Length;i++)Pine(p,a[i],.78f+(i%5)*.075f,bark,i%2==0?pine:pine2,snow);
            Vector3[] b={new Vector3(-43,0,-5),new Vector3(-44,0,12),new Vector3(-43,0,28),new Vector3(11,0,25),new Vector3(31,0,26),new Vector3(44,0,27),new Vector3(74,0,-5),new Vector3(75,0,13),new Vector3(74,0,24)};
            for(int i=0;i<b.Length;i++)Birch(p,b[i],.78f+(i%3)*.08f,birch,branch);
            Vector3[] s={new Vector3(-42,0,-2),new Vector3(-43,0,4),new Vector3(-42,0,16),new Vector3(-43,0,23),new Vector3(14,0,26),new Vector3(20,0,27),new Vector3(38,0,26),new Vector3(47,0,27),new Vector3(73,0,2),new Vector3(74,0,7),new Vector3(72,0,17),new Vector3(74,0,22)};
            for(int i=0;i<s.Length;i++)Shrub(p,s[i],.65f+(i%4)*.07f,shrub,snow);
            for(int i=0;i<24;i++){float x=-43f+(i%6)*4.2f;float z=i<12?37.5f:-8.2f;Grass(p,new Vector3(x,0,z+(i%2)*.35f),.55f+(i%3)*.07f,dry);}
        }

        static void BuildSchoolEdge(Transform p,Material wood,Material metal,Material rust,Material shrub,Material snow,Material dry){
            // Arrival side gets clutter that implies daily school life before the outage.
            Bench(p,new Vector3(-10.8f,0,-7.8f),0,wood,metal);Bench(p,new Vector3(8.5f,0,-7.8f),180,wood,metal);
            BikeRack(p,new Vector3(13.3f,0,-8.7f),metal);
            Bin(p,new Vector3(-5.2f,0,-8.6f),metal,rust);Bin(p,new Vector3(5.0f,0,-8.6f),metal,rust);
            UtilityBox(p,new Vector3(19.0f,0,-8.3f),90,metal,rust);
            for(int i=0;i<5;i++)Shrub(p,new Vector3(-15f+i*6.8f,0,-10.5f+(i%2)*.25f),.62f,shrub,snow);
            for(int i=0;i<8;i++)Grass(p,new Vector3(-17f+i*4.7f,0,-11.5f+(i%2)*.2f),.52f,dry);
            SnowBank(p,new Vector3(-14f,.04f,-12.0f),new Vector3(7f,.16f,.75f),snow);SnowBank(p,new Vector3(14f,.04f,-12.0f),new Vector3(7f,.16f,.75f),snow);
        }

        static void BuildHouseAYard(Transform p,Material wood,Material woodDark,Material metal,Material rust,Material shrub,Material snow){
            // Fence fragments, mailbox and stacked firewood make House A feel owned rather than generated.
            for(int i=0;i<7;i++){float x=-36.8f+i*2.25f;Post(p,new Vector3(x,0,17.8f),wood);if(i<6){Rail(p,new Vector3(x+1.12f,.55f,17.8f),2.1f,wood);Rail(p,new Vector3(x+1.12f,.88f,17.8f),2.1f,wood);}}
            Mailbox(p,new Vector3(-27.8f,0,1.0f),0,wood,metal);Bin(p,new Vector3(-22.3f,0,3.1f),metal,rust);
            WoodPile(p,new Vector3(-37.4f,0,13.8f),90,woodDark);
            for(int i=0;i<6;i++)Shrub(p,new Vector3(-38.2f+(i%2)*.5f,0,3.5f+i*2.4f),.60f+(i%2)*.07f,shrub,snow);
            SnowBank(p,new Vector3(-29.2f,.04f,1.45f),new Vector3(4.8f,.16f,.60f),snow);
        }

        static void BuildNorthDetails(Transform p,Material wood,Material metal,Material rust,Material shrub,Material snow,Material dry){
            Bench(p,new Vector3(-19.0f,0,20.1f),90,wood,metal);
            Bin(p,new Vector3(-20.5f,0,22.1f),metal,rust);UtilityBox(p,new Vector3(-39.2f,0,22.0f),0,metal,rust);
            for(int i=0;i<7;i++)Shrub(p,new Vector3(-21.0f+(i%2)*.7f,0,24f+i*1.8f),.56f,shrub,snow);
            for(int i=0;i<8;i++)Grass(p,new Vector3(-39.3f+(i%2)*.4f,0,22f+i*1.8f),.48f,dry);
            SnowBank(p,new Vector3(-37.8f,.035f,20.4f),new Vector3(3.2f,.15f,.55f),snow);SnowBank(p,new Vector3(-20.8f,.035f,20.5f),new Vector3(3.0f,.15f,.55f),snow);
        }

        static void BuildGarageYard(Transform p,Material wood,Material woodDark,Material metal,Material rust,Material black,Material snow){
            Pallet(p,new Vector3(66.1f,0,11.8f),90,wood);Pallet(p,new Vector3(66.1f,.12f,10.7f),90,woodDark);
            TireStack(p,new Vector3(67.0f,0,8.8f),black);Barrel(p,new Vector3(66.6f,0,13.8f),metal,rust);Barrel(p,new Vector3(67.5f,0,13.4f),metal,rust);
            UtilityBox(p,new Vector3(50.5f,0,15.4f),90,metal,rust);
            WoodPile(p,new Vector3(66.7f,0,16.1f),0,woodDark);
            SnowBank(p,new Vector3(58.5f,.04f,3.55f),new Vector3(6.0f,.15f,.60f),snow);
        }

        static void BuildRoadDetails(Transform p,Material road,Material metal,Material blue,Material snow){
            // Broken road patches, shallow snow islands and utility poles add readable direction without blocking travel.
            for(int i=0;i<9;i++)Box("road patch",p,new Vector3(43f+i*3.4f,.018f,.1f+(i%3-1)*.45f),new Vector3(1.3f,.02f,.55f),road,false);
            for(int i=0;i<6;i++)SnowBank(p,new Vector3(44f+i*5.0f,.025f,-3.7f+(i%2)*.18f),new Vector3(2.0f,.13f,.48f),snow);
            UtilityPole(p,new Vector3(45.0f,0,-1.9f),metal,blue);UtilityPole(p,new Vector3(58.0f,0,-1.9f),metal,blue);UtilityPole(p,new Vector3(-28.0f,0,18.2f),metal,blue);
        }

        static void Pine(Transform p,Vector3 pos,float s,Material bark,Material needles,Material snow){GameObject r=new GameObject("gran");r.transform.SetParent(p,true);r.transform.position=pos;r.transform.localScale=Vector3.one*s;Cylinder("trunk",r.transform,new Vector3(0,1.0f,0),new Vector3(.18f,1.0f,.18f),bark,false);Foliage(r.transform,new Vector3(0,1.55f,0),new Vector3(2.1f,.66f,2.1f),needles);Foliage(r.transform,new Vector3(0,2.12f,0),new Vector3(1.65f,.62f,1.65f),needles);Foliage(r.transform,new Vector3(0,2.65f,0),new Vector3(1.12f,.55f,1.12f),needles);Foliage(r.transform,new Vector3(0,3.02f,0),new Vector3(.48f,.18f,.48f),snow);}
        static void Birch(Transform p,Vector3 pos,float s,Material white,Material branch){GameObject r=new GameObject("björk");r.transform.SetParent(p,true);r.transform.position=pos;r.transform.localScale=Vector3.one*s;Cylinder("trunk",r.transform,new Vector3(0,1.45f,0),new Vector3(.11f,1.45f,.11f),white,false);for(int i=0;i<6;i++){GameObject b=Box("branch",r.transform,new Vector3((i%2==0?.25f:-.25f),1.7f+i*.31f,0),new Vector3(.70f,.055f,.055f),branch,false);b.transform.localRotation=Quaternion.Euler(0,i*29,i%2==0?31:-31);}}
        static void Shrub(Transform p,Vector3 pos,float s,Material m,Material snow){GameObject r=new GameObject("buske");r.transform.SetParent(p,true);r.transform.position=pos;for(int i=0;i<4;i++)Foliage(r.transform,new Vector3((i-1.5f)*.22f,.26f+(i%2)*.10f,(i%2)*.18f),new Vector3(.52f,.34f,.48f)*s,m);Foliage(r.transform,new Vector3(0,.49f,0),new Vector3(.46f,.08f,.34f)*s,snow);}
        static void Grass(Transform p,Vector3 pos,float s,Material m){GameObject r=new GameObject("vintergräs");r.transform.SetParent(p,true);r.transform.position=pos;for(int i=0;i<5;i++){GameObject g=Box("blade",r.transform,new Vector3((i-2)*.055f,.18f,0),new Vector3(.035f,.36f,.045f)*s,m,false);g.transform.localRotation=Quaternion.Euler(i%2==0?10:-10,i*35,i%2==0?10:-10);}}
        static void Bench(Transform p,Vector3 pos,float yaw,Material wood,Material metal){GameObject r=new GameObject("bänk");r.transform.SetParent(p,true);r.transform.position=pos;r.transform.rotation=Quaternion.Euler(0,yaw,0);Box("seat",r.transform,new Vector3(0,.48f,0),new Vector3(2.0f,.16f,.52f),wood,true);Box("back",r.transform,new Vector3(0,.92f,.22f),new Vector3(2.0f,.58f,.12f),wood,true);Box("leg L",r.transform,new Vector3(-.72f,.23f,0),new Vector3(.12f,.46f,.42f),metal,true);Box("leg R",r.transform,new Vector3(.72f,.23f,0),new Vector3(.12f,.46f,.42f),metal,true);}
        static void BikeRack(Transform p,Vector3 pos,Material metal){GameObject r=new GameObject("cykelställ");r.transform.SetParent(p,true);r.transform.position=pos;Box("base",r.transform,new Vector3(0,.06f,0),new Vector3(3.1f,.10f,.18f),metal,false);for(int i=0;i<6;i++){Box("rack",r.transform,new Vector3(-1.25f+i*.5f,.35f,0),new Vector3(.06f,.70f,.06f),metal,false);}}
        static void Bin(Transform p,Vector3 pos,Material metal,Material rust){GameObject r=new GameObject("soptunna");r.transform.SetParent(p,true);r.transform.position=pos;Box("bin",r.transform,new Vector3(0,.52f,0),new Vector3(.72f,1.0f,.70f),metal,true);Box("lid",r.transform,new Vector3(0,1.04f,0),new Vector3(.78f,.10f,.76f),rust,false);}
        static void UtilityBox(Transform p,Vector3 pos,float yaw,Material metal,Material rust){GameObject r=new GameObject("elskåp ute");r.transform.SetParent(p,true);r.transform.position=pos;r.transform.rotation=Quaternion.Euler(0,yaw,0);Box("box",r.transform,new Vector3(0,.62f,0),new Vector3(.72f,1.22f,.40f),metal,true);Box("door",r.transform,new Vector3(0,.64f,-.215f),new Vector3(.60f,.98f,.035f),rust,false);}
        static void Mailbox(Transform p,Vector3 pos,float yaw,Material wood,Material metal){GameObject r=new GameObject("brevlåda");r.transform.SetParent(p,true);r.transform.position=pos;r.transform.rotation=Quaternion.Euler(0,yaw,0);Box("post",r.transform,new Vector3(0,.48f,0),new Vector3(.10f,.96f,.10f),wood,true);Box("mail",r.transform,new Vector3(0,.96f,0),new Vector3(.55f,.42f,.70f),metal,false);}
        static void WoodPile(Transform p,Vector3 pos,float yaw,Material wood){GameObject r=new GameObject("vedstapel");r.transform.SetParent(p,true);r.transform.position=pos;r.transform.rotation=Quaternion.Euler(0,yaw,0);for(int row=0;row<3;row++)for(int i=0;i<6;i++)Cylinder("vedträ",r.transform,new Vector3((i-2.5f)*.26f,.14f+row*.25f,0),new Vector3(.10f,.34f,.10f),wood,false,new Vector3(0,0,90));}
        static void Pallet(Transform p,Vector3 pos,float yaw,Material wood){GameObject r=new GameObject("lastpall");r.transform.SetParent(p,true);r.transform.position=pos;r.transform.rotation=Quaternion.Euler(0,yaw,0);for(int i=0;i<5;i++)Box("slat",r.transform,new Vector3((i-2)*.28f,.08f,0),new Vector3(.20f,.10f,1.15f),wood,false);Box("rail",r.transform,new Vector3(0,.01f,-.38f),new Vector3(1.5f,.12f,.12f),wood,false);Box("rail",r.transform,new Vector3(0,.01f,.38f),new Vector3(1.5f,.12f,.12f),wood,false);}
        static void TireStack(Transform p,Vector3 pos,Material m){GameObject r=new GameObject("däckstapel");r.transform.SetParent(p,true);r.transform.position=pos;for(int i=0;i<3;i++){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name="däck";g.transform.SetParent(r.transform,false);g.transform.localPosition=new Vector3(0,.16f+i*.28f,0);g.transform.localScale=new Vector3(.58f,.12f,.58f);g.GetComponent<Renderer>().sharedMaterial=m;Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);}}
        static void Barrel(Transform p,Vector3 pos,Material m,Material rust){GameObject r=GameObject.CreatePrimitive(PrimitiveType.Cylinder);r.name="tunna";r.transform.SetParent(p,true);r.transform.position=pos+Vector3.up*.55f;r.transform.localScale=new Vector3(.45f,.55f,.45f);r.GetComponent<Renderer>().sharedMaterial=m;Box("rust stripe",r.transform,new Vector3(0,.18f,-.47f),new Vector3(.55f,.10f,.03f),rust,false);}
        static void UtilityPole(Transform p,Vector3 pos,Material metal,Material sign){GameObject r=new GameObject("lyktstolpe släckt");r.transform.SetParent(p,true);r.transform.position=pos;Box("pole",r.transform,new Vector3(0,1.45f,0),new Vector3(.10f,2.9f,.10f),metal,true);Box("arm",r.transform,new Vector3(.32f,2.76f,0),new Vector3(.72f,.08f,.08f),metal,false);Box("lamp",r.transform,new Vector3(.65f,2.68f,0),new Vector3(.38f,.16f,.22f),sign,false);}
        static void Post(Transform p,Vector3 pos,Material m){Box("staketstolpe",p,new Vector3(pos.x,.55f,pos.z),new Vector3(.10f,1.1f,.10f),m,true);}
        static void Rail(Transform p,Vector3 pos,float len,Material m){Box("staketribba",p,pos,new Vector3(len,.09f,.08f),m,false);}
        static void SnowBank(Transform p,Vector3 pos,Vector3 scale,Material snow){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name="snövall";g.transform.SetParent(p,true);g.transform.position=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=snow;Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);}

        static Shader PickShader(){bool srp=GraphicsSettings.currentRenderPipeline!=null||GraphicsSettings.defaultRenderPipeline!=null;Shader s=null;if(srp){s=Shader.Find("Universal Render Pipeline/Lit");if(!s||!s.isSupported)s=Shader.Find("Universal Render Pipeline/Simple Lit");}else s=Shader.Find("Standard");if(!s||!s.isSupported){s=Shader.Find("Universal Render Pipeline/Lit");if(!s||!s.isSupported)s=Shader.Find("Universal Render Pipeline/Simple Lit");if(!s||!s.isSupported)s=Shader.Find("Standard");}if(!s||!s.isSupported)throw new Exception("No supported active-pipeline shader");return s;}
        static Material Mat(Shader shader,string name,string hex,float smooth){Directory.CreateDirectory(MatDir);string path=MatDir+"/"+name+".mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(path);Color c=Color.white;ColorUtility.TryParseHtmlString("#"+hex,out c);if(!m){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}if(m.shader!=shader)m.shader=shader;if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);if(m.HasProperty("_Color"))m.SetColor("_Color",c);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",0f);EditorUtility.SetDirty(m);return m;}
        static Transform Group(Transform p,string n){GameObject g=new GameObject(n);g.transform.SetParent(p,true);return g.transform;}
        static GameObject Box(string n,Transform p,Vector3 pos,Vector3 size,Material m,bool collider){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;if(!collider){Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);}return g;}
        static GameObject Cylinder(string n,Transform p,Vector3 pos,Vector3 scale,Material m,bool collider){return Cylinder(n,p,pos,scale,m,collider,Vector3.zero);} static GameObject Cylinder(string n,Transform p,Vector3 pos,Vector3 scale,Material m,bool collider,Vector3 rot){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=scale;g.transform.localRotation=Quaternion.Euler(rot);g.GetComponent<Renderer>().sharedMaterial=m;if(!collider){Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);}return g;}
        static void Foliage(Transform p,Vector3 lp,Vector3 sc,Material m){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name="crown";g.transform.SetParent(p,false);g.transform.localPosition=lp;g.transform.localScale=sc;g.GetComponent<Renderer>().sharedMaterial=m;Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);}
    }
}
#endif
