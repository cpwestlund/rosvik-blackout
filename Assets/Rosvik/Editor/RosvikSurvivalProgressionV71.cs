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
    public static class RosvikSurvivalProgressionV71 {
        const int Version=71;
        const string Key="ROSVIK_SURVIVAL_PROGRESSION_V71";
        const string ScenePath="Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string GroupName="SURVIVAL PROGRESSION V71";
        const string MatDir="Assets/Rosvik/GeneratedV71";

        static RosvikSurvivalProgressionV71(){ if(EditorPrefs.GetInt(Key,0)>=Version)return; EditorApplication.delayCall+=Auto; }

        [MenuItem("Rosvik/V71 DAY NIGHT + SLEEP + HEAT + FATIGUE")]
        public static void Force(){EditorPrefs.DeleteKey(Key);EditorApplication.delayCall+=Auto;}

        static void Auto(){
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=Auto;return;}
            if(!File.Exists(ScenePath))return;
            try{Apply();}catch(Exception ex){Debug.LogError("V71 SURVIVAL PROGRESSION FAILED: "+ex);}
        }

        static void Apply(){
            if(EditorSceneManager.GetActiveScene().path!=ScenePath)EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            GameObject old=GameObject.Find(GroupName); if(old)UnityEngine.Object.DestroyImmediate(old);
            GameObject root=new GameObject(GroupName);

            CoziPlayerV57 player=UnityEngine.Object.FindFirstObjectByType<CoziPlayerV57>();
            if(!player)throw new Exception("PLAYER / CoziPlayerV57 not found");
            SurvivalProgressionV71 progression=player.GetComponent<SurvivalProgressionV71>();
            if(!progression)progression=player.gameObject.AddComponent<SurvivalProgressionV71>();
            progression.day=1;
            progression.timeMinutes=14f*60f+30f;
            progression.fatigue=92f;
            progression.gameMinutesPerRealSecond=1.25f;
            progression.fatiguePerGameHour=3.2f;

            Material dark=MaterialAsset("heater_dark",C("313331"));
            Material metal=MaterialAsset("heater_metal",C("676b67"));
            Material orange=MaterialAsset("heater_glow",C("e36b35"));
            Material mattress=MaterialAsset("cot_mattress",C("7e8d80"));
            Material blanket=MaterialAsset("cot_blanket",C("a56b4d"));
            Material wood=MaterialAsset("cot_wood",C("78533a"));

            Transform rest=FindByNameContains("staff sofa");
            if(rest==null||rest.GetComponent<V71RestSpot>()!=null){
                if(rest==null)rest=BuildFallbackCot(root.transform,mattress,blanket,wood);
            }
            V71RestSpot spot=rest.GetComponent<V71RestSpot>();
            if(!spot)spot=rest.gameObject.AddComponent<V71RestSpot>();
            spot.displayName="personalrummets viloplats";
            spot.radius=2.25f;
            spot.restHours=1f;
            spot.sleepHours=8f;

            Transform radiator=FindStaffRadiator();
            if(radiator==null)radiator=BuildPortableHeater(root.transform,dark,metal,orange);
            V71HeatSource heat=radiator.GetComponent<V71HeatSource>();
            if(!heat)heat=radiator.gameObject.AddComponent<V71HeatSource>();
            heat.displayName="elementet";
            heat.radius=2.1f;
            heat.heatRadius=4.8f;
            heat.warmthPerSecond=8.5f;
            heat.dryPerSecond=6f;
            heat.requiresPower=true;
            EnsureHeaterVisuals(radiator,heat,orange);

            player.SetObjective("Överlev dagen. Sök utrustning, håll dig varm och hitta en plats att vila när du blir trött.");
            EditorUtility.SetDirty(player);EditorUtility.SetDirty(progression);EditorUtility.SetDirty(spot);EditorUtility.SetDirty(heat);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();EditorPrefs.SetInt(Key,Version);SceneView.RepaintAll();
            Debug.Log("V71 COMPLETE — dygn, vinterljus, trötthet, vila/sömn och strömberoende värmekälla är nu spelbara.");
        }

        static Transform FindByNameContains(string token){
            token=token.ToLowerInvariant();
            foreach(Transform t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
                if(t&&t.gameObject.scene.IsValid()&&t.name.ToLowerInvariant().Contains(token))return t;
            }
            return null;
        }

        static Transform FindStaffRadiator(){
            Transform best=null;float bestSq=float.MaxValue;Vector3 target=new Vector3(3.45f,0,14.9f);
            foreach(Transform t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
                if(!t||!t.gameObject.scene.IsValid()||!t.name.ToLowerInvariant().Contains("radiator"))continue;
                Vector3 p=t.position;
                if(p.x<-.5f||p.x>7.4f||p.z<4.3f||p.z>16f)continue;
                float sq=(p-target).sqrMagnitude;if(sq<bestSq){best=t;bestSq=sq;}
            }
            return best;
        }

        static Transform BuildFallbackCot(Transform parent,Material mattress,Material blanket,Material wood){
            GameObject r=new GameObject("V71 PROVISORISK BÄDD");r.transform.SetParent(parent,true);r.transform.position=new Vector3(.92f,0,8.2f);
            LocalBox("bädd ram",r.transform,new Vector3(0,.20f,0),new Vector3(2.15f,.18f,.92f),wood,true);
            LocalBox("madrass",r.transform,new Vector3(0,.34f,0),new Vector3(2.02f,.16f,.84f),mattress,true);
            LocalBox("filt",r.transform,new Vector3(.38f,.445f,0),new Vector3(1.05f,.035f,.78f),blanket,false);
            LocalBox("kudde",r.transform,new Vector3(-.76f,.47f,0),new Vector3(.42f,.12f,.62f),mattress,false);
            for(int sx=-1;sx<=1;sx+=2)for(int sz=-1;sz<=1;sz+=2)LocalBox("ben",r.transform,new Vector3(sx*.82f,.09f,sz*.31f),new Vector3(.10f,.28f,.10f),wood,true);
            return r.transform;
        }

        static Transform BuildPortableHeater(Transform parent,Material dark,Material metal,Material orange){
            GameObject r=new GameObject("V71 PORTABELT ELEMENT");r.transform.SetParent(parent,true);r.transform.position=new Vector3(3.55f,0,14.85f);
            LocalBox("heater body",r.transform,new Vector3(0,.43f,0),new Vector3(.86f,.82f,.30f),dark,true);
            for(int i=-3;i<=3;i++)LocalBox("heater grille",r.transform,new Vector3(i*.09f,.45f,-.165f),new Vector3(.035f,.55f,.025f),metal,false);
            LocalBox("heater foot L",r.transform,new Vector3(-.29f,.08f,0),new Vector3(.18f,.16f,.42f),metal,true);
            LocalBox("heater foot R",r.transform,new Vector3(.29f,.08f,0),new Vector3(.18f,.16f,.42f),metal,true);
            LocalBox("heater indicator",r.transform,new Vector3(.28f,.72f,-.18f),new Vector3(.10f,.07f,.035f),orange,false);
            return r.transform;
        }

        static void EnsureHeaterVisuals(Transform radiator,V71HeatSource heat,Material orange){
            Renderer indicator=null;
            foreach(Renderer r in radiator.GetComponentsInChildren<Renderer>(true)){
                if(r.name.ToLowerInvariant().Contains("indicator")){indicator=r;break;}
            }
            if(!indicator){
                GameObject ind=LocalBox("V71 heater indicator",radiator,new Vector3(0,.25f,-.22f),new Vector3(.10f,.07f,.035f),orange,false);
                indicator=ind.GetComponent<Renderer>();
            }
            Light glow=radiator.GetComponentInChildren<Light>(true);
            if(!glow){
                GameObject g=new GameObject("V71 warm heater glow");g.transform.SetParent(radiator,false);g.transform.localPosition=new Vector3(0,.55f,-.25f);
                glow=g.AddComponent<Light>();glow.type=LightType.Point;glow.color=C("ff9a55");glow.range=4.2f;glow.intensity=2.1f;glow.shadows=LightShadows.None;glow.enabled=false;
            }
            heat.indicator=indicator;heat.glow=glow;
        }

        static Material MaterialAsset(string name,Color c){
            if(!AssetDatabase.IsValidFolder(MatDir)){
                if(!AssetDatabase.IsValidFolder("Assets/Rosvik"))AssetDatabase.CreateFolder("Assets","Rosvik");
                AssetDatabase.CreateFolder("Assets/Rosvik","GeneratedV71");
            }
            string path=MatDir+"/"+name+".mat";
            Material m=AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader s=PickShader();
            if(!m){m=new Material(s);AssetDatabase.CreateAsset(m,path);}else if(m.shader!=s)m.shader=s;
            if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);if(m.HasProperty("_Color"))m.SetColor("_Color",c);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",.18f);
            EditorUtility.SetDirty(m);return m;
        }

        static Shader PickShader(){Shader s=GraphicsSettings.currentRenderPipeline!=null||GraphicsSettings.defaultRenderPipeline!=null?Shader.Find("Universal Render Pipeline/Lit"):Shader.Find("Standard");if(!s||!s.isSupported)s=Shader.Find("Universal Render Pipeline/Simple Lit");if(!s||!s.isSupported)s=Shader.Find("Standard");return s;}
        static Color C(string h){ColorUtility.TryParseHtmlString("#"+h,out Color c);return c;}
        static GameObject LocalBox(string n,Transform p,Vector3 pos,Vector3 size,Material m,bool collider){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;if(!collider){Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);}return g;}
    }
}
#endif
