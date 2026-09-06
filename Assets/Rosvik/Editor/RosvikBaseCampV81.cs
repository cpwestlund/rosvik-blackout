#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Rosvik.Blackout;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikBaseCampV81 {
        const int Version=81;
        const string Key="ROSVIK_BASE_CAMP_V81";
        const string ScenePath="Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string GroupName="V81 HOUSE B SAFEHOUSE";
        const string MatDir="Assets/Rosvik/GeneratedV81";

        static RosvikBaseCampV81(){if(EditorPrefs.GetInt(Key,0)>=Version)return;EditorApplication.delayCall+=Auto;}
        [MenuItem("Rosvik/V81 WOOD STOVE + COOKING + SAFEHOUSE")]
        public static void Force(){EditorPrefs.DeleteKey(Key);EditorApplication.delayCall+=Auto;}

        static void Auto(){
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=Auto;return;}
            if(!File.Exists(ScenePath))return;
            try{Apply();}catch(Exception ex){Debug.LogError("V81 FAILED: "+ex);}
        }

        static void Apply(){
            if(EditorSceneManager.GetActiveScene().path!=ScenePath)EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            CoziPlayerV57 player=UnityEngine.Object.FindFirstObjectByType<CoziPlayerV57>();if(!player)throw new Exception("PLAYER missing");
            GameObject town=GameObject.Find("V79 NORTH NEIGHBORHOOD - HOUSE B");if(!town)throw new Exception("V79 town root missing — run V79 first");
            Transform details=town.transform.Find("HOUSE B/DETAILS");if(!details)throw new Exception("House B DETAILS missing");

            Transform old=details.Find(GroupName);if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            GameObject root=new GameObject(GroupName);root.transform.SetParent(details,true);

            Shader shader=PickShader();
            Material iron=Mat(shader,"stove_iron","252c2b",.20f);
            Material ironEdge=Mat(shader,"stove_edge","46504d",.24f);
            Material glass=Mat(shader,"stove_glass","51372a",.10f);
            Material warm=Mat(shader,"fire_warm","d57d32",.05f);
            Material ember=Mat(shader,"fire_ember","8f3824",.05f);
            Material wood=Mat(shader,"basket_wood","705039",.12f);
            Material kettle=Mat(shader,"kettle","555f5d",.28f);

            Vector3 stovePos=new Vector3(-23.45f,0f,33.72f);
            GameObject stove=new GameObject("vedspis");stove.transform.SetParent(root.transform,true);stove.transform.position=stovePos;
            Box("stove body",stove.transform,new Vector3(0,.47f,0),new Vector3(.92f,.92f,.72f),iron,true);
            Box("stove top",stove.transform,new Vector3(0,.96f,0),new Vector3(1.02f,.10f,.80f),ironEdge,false);
            Box("stove door",stove.transform,new Vector3(0,.48f,-.375f),new Vector3(.58f,.46f,.05f),ironEdge,false);
            Box("fire window",stove.transform,new Vector3(0,.50f,-.407f),new Vector3(.42f,.28f,.025f),glass,false);
            Sphere("door handle",stove.transform,new Vector3(.34f,.55f,-.43f),Vector3.one*.07f,ironEdge,false);
            Box("ash drawer",stove.transform,new Vector3(0,.18f,-.39f),new Vector3(.62f,.15f,.05f),ironEdge,false);

            GameObject pipe=GameObject.CreatePrimitive(PrimitiveType.Cylinder);pipe.name="stove pipe";pipe.transform.SetParent(stove.transform,false);pipe.transform.localPosition=new Vector3(.22f,1.66f,.18f);pipe.transform.localScale=new Vector3(.12f,.72f,.12f);pipe.GetComponent<Renderer>().sharedMaterial=iron;Collider pc=pipe.GetComponent<Collider>();if(pc)UnityEngine.Object.DestroyImmediate(pc);
            Box("pipe elbow",stove.transform,new Vector3(.22f,2.25f,.36f),new Vector3(.23f,.22f,.52f),iron,false);

            // Simple kettle makes the stove read as a usable kitchen heat source.
            GameObject pot=GameObject.CreatePrimitive(PrimitiveType.Cylinder);pot.name="kettle";pot.transform.SetParent(stove.transform,false);pot.transform.localPosition=new Vector3(-.22f,1.16f,0);pot.transform.localScale=new Vector3(.23f,.20f,.23f);pot.GetComponent<Renderer>().sharedMaterial=kettle;Collider kc=pot.GetComponent<Collider>();if(kc)UnityEngine.Object.DestroyImmediate(kc);
            Box("kettle handle",stove.transform,new Vector3(-.22f,1.40f,0),new Vector3(.38f,.05f,.05f),ironEdge,false);

            Transform flame=new GameObject("fire visual").transform;flame.SetParent(stove.transform,false);flame.localPosition=new Vector3(0,.50f,-.43f);
            Sphere("ember",flame,new Vector3(0,-.07f,0),new Vector3(.38f,.10f,.12f),ember,false);
            Sphere("flame A",flame,new Vector3(-.10f,.05f,0),new Vector3(.18f,.30f,.10f),warm,false);
            Sphere("flame B",flame,new Vector3(.11f,.08f,0),new Vector3(.15f,.36f,.09f),warm,false);
            flame.gameObject.SetActive(false);

            GameObject lightGo=new GameObject("fire light");lightGo.transform.SetParent(stove.transform,false);lightGo.transform.localPosition=new Vector3(0,1.1f,-.2f);
            Light light=lightGo.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1f,.58f,.27f);light.range=5.0f;light.intensity=1.6f;light.shadows=LightShadows.Soft;light.enabled=false;

            WoodStoveV81 ws=stove.AddComponent<WoodStoveV81>();ws.flameVisual=flame;ws.fireLight=light;ws.interactionDistance=2.45f;ws.heatRadius=3.7f;ws.secondsPerFuel=110f;ws.maxBurnSeconds=440f;

            // Firewood basket is visual; actual usable wood comes from the woodshed loot.
            GameObject basket=new GameObject("vedkorg");basket.transform.SetParent(root.transform,true);basket.transform.position=new Vector3(-24.55f,0f,33.68f);
            Box("basket base",basket.transform,new Vector3(0,.10f,0),new Vector3(.72f,.12f,.48f),wood,false);
            Box("basket side L",basket.transform,new Vector3(-.34f,.28f,0),new Vector3(.08f,.48f,.52f),wood,false);
            Box("basket side R",basket.transform,new Vector3(.34f,.28f,0),new Vector3(.08f,.48f,.52f),wood,false);
            for(int i=0;i<4;i++){
                GameObject log=GameObject.CreatePrimitive(PrimitiveType.Cylinder);log.name="torr ved";log.transform.SetParent(basket.transform,false);log.transform.localPosition=new Vector3(-.20f+i*.13f,.26f,(i%2==0?-.08f:.08f));log.transform.localRotation=Quaternion.Euler(0,0,90);log.transform.localScale=new Vector3(.08f,.28f,.08f);log.GetComponent<Renderer>().sharedMaterial=wood;Collider c=log.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);
            }

            BoostWoodshedFuel();
            player.SetObjective("Hus B kan bli en tryggare bas. Hitta tändare och torrt trä i området, tänd vedspisen och värm mat innan nästa tur ut.");
            EditorUtility.SetDirty(player);EditorUtility.SetDirty(ws);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();EditorPrefs.SetInt(Key,Version);SceneView.RepaintAll();
            Debug.Log("V81 COMPLETE — House B now has a functional wood stove with fire, heat, drying, cooking, crackle audio and woodshed fuel progression.");
        }

        static void BoostWoodshedFuel(){
            foreach(LootContainerV74 c in UnityEngine.Object.FindObjectsByType<LootContainerV74>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
                if(!c||c.displayName.IndexOf("vedbodens verktyg",StringComparison.OrdinalIgnoreCase)<0)continue;
                List<string> items=c.items!=null?c.items.ToList():new List<string>();List<int> counts=c.counts!=null?c.counts.ToList():new List<int>();while(counts.Count<items.Count)counts.Add(1);
                int idx=items.FindIndex(x=>string.Equals(x,"Träspill",StringComparison.OrdinalIgnoreCase));
                if(idx>=0)counts[idx]=Mathf.Max(counts[idx],10);else{items.Add("Träspill");counts.Add(10);}
                c.items=items.ToArray();c.counts=counts.ToArray();c.RebuildContents();EditorUtility.SetDirty(c);break;
            }
        }

        static Shader PickShader(){bool srp=GraphicsSettings.currentRenderPipeline!=null||GraphicsSettings.defaultRenderPipeline!=null;Shader s=null;if(srp){s=Shader.Find("Universal Render Pipeline/Lit");if(!s||!s.isSupported)s=Shader.Find("Universal Render Pipeline/Simple Lit");}else s=Shader.Find("Standard");if(!s||!s.isSupported){s=Shader.Find("Universal Render Pipeline/Lit");if(!s||!s.isSupported)s=Shader.Find("Universal Render Pipeline/Simple Lit");if(!s||!s.isSupported)s=Shader.Find("Standard");}if(!s||!s.isSupported)throw new Exception("No supported active-pipeline shader");return s;}
        static Material Mat(Shader shader,string name,string hex,float smooth){Directory.CreateDirectory(MatDir);string path=MatDir+"/"+name+".mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(path);Color col=Color.white;ColorUtility.TryParseHtmlString("#"+hex,out col);if(!m){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}if(m.shader!=shader)m.shader=shader;if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",col);if(m.HasProperty("_Color"))m.SetColor("_Color",col);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;}
        static GameObject Box(string n,Transform p,Vector3 local,Vector3 scale,Material m,bool collider){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=local;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=m;if(!collider){Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);}return g;}
        static GameObject Sphere(string n,Transform p,Vector3 local,Vector3 scale,Material m,bool collider){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=local;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=m;if(!collider){Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);}return g;}
    }
}
#endif
