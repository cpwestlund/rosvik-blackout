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
    public static class RosvikAstraReferenceV83 {
        const int Version=83;
        const string Key="ROSVIK_ASTRA_REFERENCE_V83";
        const string ScenePath="Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string GroupName="V83 ASTRA REFERENCE POLISH";
        const string MatDir="Assets/Rosvik/GeneratedV83";
        static RosvikAstraReferenceV83(){if(EditorPrefs.GetInt(Key,0)>=Version)return;EditorApplication.delayCall+=Auto;}
        [MenuItem("Rosvik/V83 ASTRA REFERENCE - INVENTORY + ROOFS + ATMOSPHERE")]
        public static void Force(){EditorPrefs.DeleteKey(Key);EditorApplication.delayCall+=Auto;}
        static void Auto(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=Auto;return;}if(!File.Exists(ScenePath))return;try{Apply();}catch(Exception ex){Debug.LogError("V83 FAILED: "+ex);}}

        static void Apply(){
            if(EditorSceneManager.GetActiveScene().path!=ScenePath)EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            CoziPlayerV57 player=UnityEngine.Object.FindFirstObjectByType<CoziPlayerV57>();if(!player)throw new Exception("PLAYER missing");
            GameObject old=GameObject.Find(GroupName);if(old)UnityEngine.Object.DestroyImmediate(old);
            GameObject root=new GameObject(GroupName);

            var inv=player.GetComponent<BackpackLootPresentationV83>();if(!inv)inv=player.gameObject.AddComponent<BackpackLootPresentationV83>();inv.enabled=true;EditorUtility.SetDirty(inv);
            var fp=player.GetComponent<FootprintTrailV83>();if(!fp)fp=player.gameObject.AddComponent<FootprintTrailV83>();fp.enabled=true;fp.spacing=.70f;fp.poolSize=46;EditorUtility.SetDirty(fp);
            var atm=player.GetComponent<AtmosphereMixV83>();if(!atm)atm=player.gameObject.AddComponent<AtmosphereMixV83>();atm.enabled=true;EditorUtility.SetDirty(atm);

            Shader shader=PickShader();Material roof=Mat(shader,"roof_red_dark","4f3031"),roofEdge=Mat(shader,"roof_edge","292d2e"),snow=Mat(shader,"roof_snow","aab9bd"),track=Mat(shader,"snow_track","6f858c"),wood=Mat(shader,"yard_wood","5d4638"),metal=Mat(shader,"yard_metal","3d4b4d");
            BuildHouseRoof(root.transform,"HOUSE A ROOF",new Vector2(-36.4f,2.2f),new Vector2(-21.6f,16.1f),new Vector3(-29f,0,9.15f),14.9f,14.2f,roof,roofEdge,snow);
            BuildHouseRoof(root.transform,"HOUSE B ROOF",new Vector2(-36.3f,22.15f),new Vector2(-21.7f,35.75f),new Vector3(-29f,0,28.9f),14.7f,13.7f,roof,roofEdge,snow);
            BuildWinterStory(root.transform,track,wood,metal,snow);

            // Old inventory presentation remains in the scene for data/functions, but V83 owns its visuals.
            SurvivalInventoryV76 oldInv=player.GetComponent<SurvivalInventoryV76>();if(oldInv){oldInv.enabled=false;EditorUtility.SetDirty(oldInv);}
            CleanupMissingScripts();
            player.SetObjective("Fortsätt utforska. Hus, garage och förråd har olika resurser — bygg upp en trygg bas innan kylan blir värre.");EditorUtility.SetDirty(player);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();EditorPrefs.SetInt(Key,Version);SceneView.RepaintAll();Debug.Log("V83 COMPLETE — Astra-reference inventory/loot presentation, real exterior roofs, footprint trail, stronger atmosphere and winter dressing applied.");
        }

        static void BuildHouseRoof(Transform parent,string name,Vector2 min,Vector2 max,Vector3 center,float width,float depth,Material roof,Material edge,Material snow){
            GameObject mgr=new GameObject(name);mgr.transform.SetParent(parent,true);GameObject visual=new GameObject("ROOF VISUAL");visual.transform.SetParent(mgr.transform,true);
            float half=width*.255f;GameObject a=Box("left roof",visual.transform,center+new Vector3(-half,2.12f,0),new Vector3(width*.54f,.16f,depth+.5f),roof,false);a.transform.rotation=Quaternion.Euler(0,0,-18f);
            GameObject b=Box("right roof",visual.transform,center+new Vector3(half,2.12f,0),new Vector3(width*.54f,.16f,depth+.5f),roof,false);b.transform.rotation=Quaternion.Euler(0,0,18f);
            Box("ridge",visual.transform,center+new Vector3(0,3.28f,0),new Vector3(.18f,.16f,depth+.6f),edge,false);
            GameObject sa=Box("snow left",visual.transform,center+new Vector3(-half,2.22f,0),new Vector3(width*.50f,.045f,depth+.25f),snow,false);sa.transform.rotation=Quaternion.Euler(0,0,-18f);
            GameObject sb=Box("snow right",visual.transform,center+new Vector3(half,2.22f,0),new Vector3(width*.50f,.045f,depth+.25f),snow,false);sb.transform.rotation=Quaternion.Euler(0,0,18f);
            RoofCutawayV83 cut=mgr.AddComponent<RoofCutawayV83>();cut.roofVisual=visual;cut.minXZ=min;cut.maxXZ=max;EditorUtility.SetDirty(cut);
        }

        static void BuildWinterStory(Transform p,Material track,Material wood,Material metal,Material snow){
            // Worn footpaths and small believable yard details around House A/B without blocking navigation.
            for(int i=0;i<10;i++){
                float z=17.0f+i*.55f;GameObject t=Box("trampled snow",p,new Vector3(-29.3f,.012f,z),new Vector3(1.05f,.014f,.42f),track,false);t.transform.rotation=Quaternion.Euler(0,(i%3-1)*4f,0);
            }
            for(int i=0;i<8;i++){
                float x=-27.4f+i*.75f;GameObject t=Box("yard track",p,new Vector3(x,.014f,20.5f),new Vector3(.52f,.014f,.92f),track,false);t.transform.rotation=Quaternion.Euler(0,90+(i%2==0?3:-3),0);
            }
            Box("mail post",p,new Vector3(-25.1f,.62f,18.0f),new Vector3(.10f,1.24f,.10f),wood,true);Box("mail box",p,new Vector3(-25.1f,1.20f,18.0f),new Vector3(.48f,.28f,.32f),metal,false);
            Box("snow fence rail",p,new Vector3(-42.2f,.52f,24.3f),new Vector3(5.0f,.09f,.10f),wood,true);for(int i=0;i<4;i++)Box("snow fence post",p,new Vector3(-44.4f+i*1.5f,.62f,24.3f),new Vector3(.10f,1.24f,.10f),wood,true);
            SnowMound("plow bank A",p,new Vector3(-18.7f,.03f,18.3f),new Vector3(4.8f,.24f,.65f),snow);SnowMound("plow bank B",p,new Vector3(-39.4f,.03f,18.25f),new Vector3(4.2f,.22f,.62f),snow);
        }

        static void CleanupMissingScripts(){foreach(GameObject g in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(g&&g.scene.IsValid())GameObjectUtility.RemoveMonoBehavioursWithMissingScript(g);}
        static Shader PickShader(){bool srp=GraphicsSettings.currentRenderPipeline!=null||GraphicsSettings.defaultRenderPipeline!=null;Shader s=srp?Shader.Find("Universal Render Pipeline/Lit"):Shader.Find("Standard");if(!s||!s.isSupported)s=Shader.Find("Universal Render Pipeline/Simple Lit");if(!s||!s.isSupported)s=Shader.Find("Standard");if(!s||!s.isSupported)throw new Exception("No supported shader");return s;}
        static Material Mat(Shader s,string n,string hex){Directory.CreateDirectory(MatDir);string path=MatDir+"/"+n+".mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(path);Color c=Color.white;ColorUtility.TryParseHtmlString("#"+hex,out c);if(!m){m=new Material(s);AssetDatabase.CreateAsset(m,path);}if(m.shader!=s)m.shader=s;if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);if(m.HasProperty("_Color"))m.SetColor("_Color",c);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",.08f);EditorUtility.SetDirty(m);return m;}
        static GameObject Box(string n,Transform p,Vector3 pos,Vector3 size,Material m,bool collider){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,true);g.transform.position=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;if(!collider){Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);}return g;}
        static void SnowMound(string n,Transform p,Vector3 pos,Vector3 scale,Material m){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=n;g.transform.SetParent(p,true);g.transform.position=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=m;Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);}
    }
}
#endif
