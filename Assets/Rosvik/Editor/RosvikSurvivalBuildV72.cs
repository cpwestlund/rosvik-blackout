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
    public static class RosvikSurvivalBuildV72 {
        const int Version=72;
        const string Key="ROSVIK_SURVIVAL_BUILD_V72";
        const string ScenePath="Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string GroupName="SURVIVAL BUILD V72";

        static RosvikSurvivalBuildV72(){if(EditorPrefs.GetInt(Key,0)>=Version)return;EditorApplication.delayCall+=Auto;}
        [MenuItem("Rosvik/V72 BUILD FORWARD - UI CRAFTING STOVE MATERIALS")]
        public static void Force(){EditorPrefs.DeleteKey(Key);EditorApplication.delayCall+=Auto;}

        static void Auto(){
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=Auto;return;}
            if(!File.Exists(ScenePath))return;
            try{Apply();}catch(Exception ex){Debug.LogError("V72 BUILD FAILED: "+ex);}
        }

        static void Apply(){
            if(EditorSceneManager.GetActiveScene().path!=ScenePath)EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            GameObject old=GameObject.Find(GroupName);if(old)UnityEngine.Object.DestroyImmediate(old);
            GameObject root=new GameObject(GroupName);

            CoziPlayerV57 player=UnityEngine.Object.FindFirstObjectByType<CoziPlayerV57>();if(!player)throw new Exception("PLAYER missing");
            SurvivalSystemsV69 survival=player.GetComponent<SurvivalSystemsV69>();if(!survival)throw new Exception("SurvivalSystemsV69 missing");
            player.suppressLegacyGui=true;survival.suppressLegacyGui=true;

            SurvivalPresentationV70 p70=player.GetComponent<SurvivalPresentationV70>();if(p70)p70.enabled=false;
            SurvivalLegacyHudMaskV70 mask=player.GetComponent<SurvivalLegacyHudMaskV70>();if(mask)mask.enabled=false;
            SurvivalPresentationV72 p72=player.GetComponent<SurvivalPresentationV72>();if(!p72)p72=player.gameObject.AddComponent<SurvivalPresentationV72>();p72.enabled=true;
            SurvivalCraftingV72 craft=player.GetComponent<SurvivalCraftingV72>();if(!craft)craft=player.gameObject.AddComponent<SurvivalCraftingV72>();

            Shader shader=PickShader();Material wood=Mat(shader,C("6d4b34"));Material dark=Mat(shader,C("26302d"));Material cream=Mat(shader,C("d8ccb0"));Material metal=Mat(shader,C("65706f"));Material cloth=Mat(shader,C("6d8277"));Material rust=Mat(shader,C("98583e"));Material fuel=Mat(shader,C("c28c42"));

            Transform gameplay=Group(root.transform,"CRAFTING + HEAT");
            Transform loot=Group(root.transform,"MATERIAL LOOT");
            BuildWorkbench(gameplay,wood,dark,metal);
            BuildStove(gameplay,dark,metal,rust,fuel);

            MaterialBox(loot,"vaktmästarens materialback",new Vector3(16.65f,0,14.35f),180,wood,dark,metal,"Metallskrot",new[]{"Tygbit","Tejp"},new[]{2,2});
            MaterialBox(loot,"sporthallens reparationslåda",new Vector3(43.65f,0,6.6f),90,wood,dark,cloth,"Tygbit",new[]{"Metallskrot","Sporttejp"},new[]{2,1});
            MaterialBox(loot,"underhållslåda vid parkeringen",new Vector3(-16.0f,0,-8.8f),0,wood,dark,fuel,"Träspill",new[]{"Träspill","Metallskrot","Bränsle"},new[]{3,1,1});

            player.SetObjective("Överlev dagen. Sök material, använd arbetsbänken och få igång en säker värmekälla.");
            EditorUtility.SetDirty(player);EditorUtility.SetDirty(survival);EditorUtility.SetDirty(p72);EditorUtility.SetDirty(craft);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();EditorPrefs.SetInt(Key,Version);SceneView.RepaintAll();
            Debug.Log("V72 COMPLETE — legacy HUD suppressed, cleaner lower survival bars, rebuilt inventory, darker mood, crafting, reserve torch, stove/cooking and material loot are active.");
        }

        static void BuildWorkbench(Transform p,Material wood,Material dark,Material metal){
            Transform found=null;
            foreach(Transform t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
                if(!t||!t.gameObject.scene.IsValid())continue;string n=t.name.ToLowerInvariant();if((n.Contains("workbench")||n.Contains("arbetsbänk"))&&t.position.x>11f){found=t;break;}
            }
            if(found){if(!found.GetComponent<V72CraftStation>())found.gameObject.AddComponent<V72CraftStation>();return;}
            GameObject r=new GameObject("V72 arbetsbänk");r.transform.SetParent(p,true);r.transform.position=new Vector3(16.0f,0,12.45f);r.transform.rotation=Quaternion.Euler(0,90,0);
            LocalBox("bench top",r.transform,new Vector3(0,.82f,0),new Vector3(1.7f,.16f,.72f),wood,true);
            LocalBox("back rail",r.transform,new Vector3(0,1.25f,.30f),new Vector3(1.7f,.72f,.10f),dark,true);
            LocalBox("leg L",r.transform,new Vector3(-.70f,.40f,0),new Vector3(.15f,.80f,.56f),metal,true);
            LocalBox("leg R",r.transform,new Vector3(.70f,.40f,0),new Vector3(.15f,.80f,.56f),metal,true);
            LocalBox("vice",r.transform,new Vector3(.52f,.98f,-.28f),new Vector3(.30f,.20f,.18f),metal,false);
            r.AddComponent<V72CraftStation>();
        }

        static void BuildStove(Transform p,Material dark,Material metal,Material rust,Material fuel){
            GameObject r=new GameObject("V72 stormkök");r.transform.SetParent(p,true);r.transform.position=new Vector3(13.45f,0,13.65f);
            Cylinder("fuel canister",r.transform,new Vector3(0,.22f,0),new Vector3(.30f,.22f,.30f),fuel,true);
            Cylinder("burner",r.transform,new Vector3(0,.50f,0),new Vector3(.24f,.10f,.24f),metal,false);
            for(int i=0;i<3;i++){float a=i*Mathf.PI*2/3f;LocalBox("pot support",r.transform,new Vector3(Mathf.Cos(a)*.28f,.59f,Mathf.Sin(a)*.28f),new Vector3(.08f,.12f,.28f),dark,false).transform.rotation=Quaternion.Euler(0,-i*120,0);}
            GameObject flame=LocalBox("flame indicator",r.transform,new Vector3(0,.70f,0),new Vector3(.12f,.18f,.12f),rust,false);
            Light glow=r.AddComponent<Light>();glow.type=LightType.Point;glow.color=new Color(1f,.47f,.15f);glow.range=5.5f;glow.intensity=2.7f;glow.shadows=LightShadows.Soft;glow.enabled=false;
            V72Stove s=r.AddComponent<V72Stove>();s.glow=glow;s.flame=flame.GetComponent<Renderer>();
        }

        static void MaterialBox(Transform p,string name,Vector3 pos,float yaw,Material body,Material dark,Material accent,string primary,string[] extras,int[] counts){
            GameObject r=new GameObject(name);r.transform.SetParent(p,true);r.transform.position=pos;r.transform.rotation=Quaternion.Euler(0,yaw,0);
            GameObject shell=LocalBox("material box body",r.transform,new Vector3(0,.30f,0),new Vector3(1.08f,.52f,.68f),body,true);
            LocalBox("front strip",r.transform,new Vector3(0,.34f,-.35f),new Vector3(.72f,.16f,.035f),accent,false);
            Transform hinge=Group(r.transform,"lid hinge");hinge.localPosition=new Vector3(0,.58f,.31f);LocalBox("lid",hinge,new Vector3(0,.03f,-.31f),new Vector3(1.12f,.09f,.72f),dark,true);
            GameObject contents=new GameObject("materials");contents.transform.SetParent(r.transform,false);LocalBox("material A",contents.transform,new Vector3(-.25f,.59f,-.05f),new Vector3(.30f,.10f,.18f),accent,false);LocalBox("material B",contents.transform,new Vector3(.18f,.59f,.05f),new Vector3(.24f,.10f,.24f),dark,false);
            CozyInteractableV57 x=r.AddComponent<CozyInteractableV57>();x.kind=CozyInteractableV57.Kind.Cabinet;x.displayName=name;x.itemName=primary;x.extraItems=extras;x.extraCounts=counts;x.radius=1.9f;x.movingPart=hinge;x.closedEuler=Vector3.zero;x.openEuler=new Vector3(-82,0,0);x.revealOnOpen=contents.transform;x.highlightRenderer=shell.GetComponent<Renderer>();x.animationTime=.25f;
        }

        static Shader PickShader(){Shader s=GraphicsSettings.currentRenderPipeline!=null||GraphicsSettings.defaultRenderPipeline!=null?Shader.Find("Universal Render Pipeline/Lit"):Shader.Find("Standard");if(!s||!s.isSupported)s=Shader.Find("Universal Render Pipeline/Simple Lit");if(!s||!s.isSupported)s=Shader.Find("Standard");return s;}
        static Material Mat(Shader s,Color c){Material m=new Material(s);if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);if(m.HasProperty("_Color"))m.SetColor("_Color",c);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",.15f);return m;}
        static Color C(string h){ColorUtility.TryParseHtmlString("#"+h,out Color c);return c;}
        static Transform Group(Transform p,string n){GameObject g=new GameObject(n);g.transform.SetParent(p,false);return g.transform;}
        static GameObject LocalBox(string n,Transform p,Vector3 pos,Vector3 size,Material m,bool collider){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;if(!collider){Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);}return g;}
        static GameObject Cylinder(string n,Transform p,Vector3 pos,Vector3 scale,Material m,bool collider){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=m;if(!collider){Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);}return g;}
    }
}
#endif
