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
    public static class RosvikGraphicalOverhaulV842 {
        const int Version=842;
        const string Key="ROSVIK_GRAPHICAL_OVERHAUL_V842";
        const string ScenePath="Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string MatDir="Assets/Rosvik/GeneratedV842";

        static RosvikGraphicalOverhaulV842(){ if(EditorPrefs.GetInt(Key,0)<Version) EditorApplication.delayCall+=Auto; }

        [MenuItem("Rosvik/V84.2 REAL HOUSE EXTERIORS + CLEAN CUTAWAY")]
        public static void Force(){ EditorPrefs.DeleteKey(Key); EditorApplication.delayCall+=Auto; }

        static void Auto(){
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode){ EditorApplication.delayCall+=Auto; return; }
            if(!File.Exists(ScenePath)) return;
            try{ Apply(); }catch(Exception ex){ Debug.LogError("V84.2 FAILED: "+ex); }
        }

        static void Apply(){
            if(EditorSceneManager.GetActiveScene().path!=ScenePath) EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            Shader shader=PickShader();
            Material facade=Mat(shader,"house_red","694844",.08f);
            Material trim=Mat(shader,"house_trim","c9c4b2",.12f);
            Material glass=Mat(shader,"window_glass","405864",.28f);
            Material roof=Mat(shader,"roof_dark","403637",.06f);
            Material snow=Mat(shader,"roof_snow","b7c5c9",.13f);
            Material edge=Mat(shader,"roof_edge","252c2f",.03f);
            Material foundation=Mat(shader,"foundation","3b4547",.04f);

            Rebuild("HOUSE A V84",new Vector2(-36.4f,2.15f),new Vector2(-21.6f,16.05f),new Vector3(-29f,0,9.10f),14.8f,13.9f,-29.15f,2.15f,facade,trim,glass,roof,snow,edge,foundation);
            Rebuild("HOUSE B V84",new Vector2(-36.3f,22.10f),new Vector2(-21.7f,35.80f),new Vector3(-29f,0,28.95f),14.6f,13.7f,-29.40f,22.10f,facade,trim,glass,roof,snow,edge,foundation);

            CleanupMissingScripts();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorPrefs.SetInt(Key,Version);
            SceneView.RepaintAll();
            Debug.Log("V84.2 COMPLETE — houses now have proper opaque exterior shells outside, clean cutaway inside and roof artifact safety.");
        }

        static void Rebuild(string houseName,Vector2 min,Vector2 max,Vector3 c,float width,float depth,float doorX,float frontZ,
            Material facade,Material trim,Material glass,Material roof,Material snow,Material edge,Material foundation){
            GameObject house=GameObject.Find(houseName); if(!house) throw new Exception(houseName+" missing");
            HouseEnvelopeV84 env=house.GetComponent<HouseEnvelopeV84>(); if(!env) env=house.AddComponent<HouseEnvelopeV84>();

            Transform oldRoof=house.transform.Find("ROOF VISUAL"); if(oldRoof) UnityEngine.Object.DestroyImmediate(oldRoof.gameObject);
            Transform ext=house.transform.Find("CAMERA FACING EXTERIOR");
            if(!ext){ GameObject g=new GameObject("CAMERA FACING EXTERIOR"); g.transform.SetParent(house.transform,true); ext=g.transform; }
            DestroyChild(ext,"V84.2 EXTERIOR SHELL");
            RemoveNamedChildren(ext,"facade batten");

            Transform perm=house.transform.Find("PERMANENT EXTERIOR DETAIL");
            if(perm) RemoveNamedChildren(perm,"wind packed snow");

            GameObject roofRoot=new GameObject("ROOF VISUAL"); roofRoot.transform.SetParent(house.transform,true);
            BuildRoof(roofRoot.transform,c,width,depth,roof,snow,edge);

            GameObject shell=new GameObject("V84.2 EXTERIOR SHELL"); shell.transform.SetParent(ext,true);
            BuildShell(shell.transform,c,width,depth,doorX,frontZ,facade,trim,glass,foundation);

            env.roofVisual=roofRoot;
            env.cameraFacingExterior=ext.gameObject;
            env.minXZ=min; env.maxXZ=max; env.enterPadding=.45f; env.exitPadding=.95f;
            EditorUtility.SetDirty(env);

            HouseVisualSafetyV842 safe=house.GetComponent<HouseVisualSafetyV842>(); if(!safe) safe=house.AddComponent<HouseVisualSafetyV842>();
            safe.minXZ=min; safe.maxXZ=max; safe.enterPadding=.45f; safe.exitPadding=.95f; safe.roofRoot=roofRoot;
            EditorUtility.SetDirty(safe);
        }

        static void BuildRoof(Transform p,Vector3 c,float width,float depth,Material roof,Material snow,Material edge){
            const float pitch=10.5f, over=.24f, eaveY=1.58f;
            float w=width+over*2f,d=depth+over*2f,half=w*.5f,rad=pitch*Mathf.Deg2Rad,rise=half*Mathf.Tan(rad),ridge=eaveY+rise,len=half/Mathf.Cos(rad),xoff=half*.5f,y=eaveY+rise*.5f;
            GameObject l=Box("roof left",p,c+new Vector3(-xoff,y,0),new Vector3(len+.08f,.20f,d),roof,Quaternion.Euler(0,0,pitch));
            GameObject r=Box("roof right",p,c+new Vector3(xoff,y,0),new Vector3(len+.08f,.20f,d),roof,Quaternion.Euler(0,0,-pitch));
            Box("snow left",p,c+new Vector3(-xoff+.02f,y+.115f,0),new Vector3(len-.10f,.045f,d-.18f),snow,l.transform.rotation);
            Box("snow right",p,c+new Vector3(xoff-.02f,y+.115f,0),new Vector3(len-.10f,.045f,d-.18f),snow,r.transform.rotation);
            Box("ridge",p,c+new Vector3(0,ridge+.05f,0),new Vector3(.16f,.10f,d+.02f),edge,Quaternion.identity);
            Vector3 ch=c+new Vector3(-2.05f,ridge+.34f,2.0f);
            Box("chimney",p,ch,new Vector3(.56f,.78f,.56f),edge,Quaternion.identity);
            Box("chimney cap",p,ch+Vector3.up*.43f,new Vector3(.68f,.10f,.68f),snow,Quaternion.identity);
        }

        static void BuildShell(Transform p,Vector3 c,float width,float depth,float doorX,float frontZ,Material facade,Material trim,Material glass,Material foundation){
            float minX=c.x-width*.5f,maxX=c.x+width*.5f,backZ=c.z+depth*.5f;
            float wallY=.88f,wallH=1.52f,th=.16f,gap=1.70f;
            float leftW=(doorX-gap*.5f)-minX,rightW=maxX-(doorX+gap*.5f);
            if(leftW>.2f) Box("front facade left",p,new Vector3(minX+leftW*.5f,wallY,frontZ),new Vector3(leftW,wallH,th),facade,Quaternion.identity);
            if(rightW>.2f) Box("front facade right",p,new Vector3(doorX+gap*.5f+rightW*.5f,wallY,frontZ),new Vector3(rightW,wallH,th),facade,Quaternion.identity);
            Box("front door header",p,new Vector3(doorX,1.50f,frontZ),new Vector3(gap,.28f,th),facade,Quaternion.identity);
            Box("back facade",p,new Vector3(c.x,wallY,backZ),new Vector3(width,wallH,th),facade,Quaternion.identity);
            Box("west facade",p,new Vector3(minX,wallY,c.z),new Vector3(th,wallH,depth),facade,Quaternion.identity);
            Box("east facade",p,new Vector3(maxX,wallY,c.z),new Vector3(th,wallH,depth),facade,Quaternion.identity);

            Box("front foundation",p,new Vector3(c.x,.18f,frontZ-.03f),new Vector3(width,.36f,.20f),foundation,Quaternion.identity);
            Box("west foundation",p,new Vector3(minX-.02f,.18f,c.z),new Vector3(.20f,.36f,depth),foundation,Quaternion.identity);
            Box("east foundation",p,new Vector3(maxX+.02f,.18f,c.z),new Vector3(.20f,.36f,depth),foundation,Quaternion.identity);

            Window(p,new Vector3(c.x-3.25f,.98f,frontZ-.095f),2.15f,1.00f,0,trim,glass);
            Window(p,new Vector3(c.x+3.25f,.98f,frontZ-.095f),2.15f,1.00f,0,trim,glass);
            Window(p,new Vector3(minX-.095f,.98f,c.z-2.6f),2.05f,1.00f,90,trim,glass);
            Window(p,new Vector3(maxX+.095f,.98f,c.z+2.4f),2.05f,1.00f,90,trim,glass);

            Box("door left casing",p,new Vector3(doorX-gap*.5f-.07f,.94f,frontZ-.10f),new Vector3(.12f,1.62f,.10f),trim,Quaternion.identity);
            Box("door right casing",p,new Vector3(doorX+gap*.5f+.07f,.94f,frontZ-.10f),new Vector3(.12f,1.62f,.10f),trim,Quaternion.identity);
            Box("door top casing",p,new Vector3(doorX,1.69f,frontZ-.10f),new Vector3(gap+.26f,.12f,.10f),trim,Quaternion.identity);

            for(float x=minX+.42f;x<maxX-.3f;x+=.68f){ if(Mathf.Abs(x-doorX)<1.15f) continue; Box("subtle siding",p,new Vector3(x,.91f,frontZ-.092f),new Vector3(.025f,1.36f,.025f),trim,Quaternion.identity); }
        }

        static void Window(Transform p,Vector3 pos,float w,float h,float yaw,Material trim,Material glass){
            Quaternion q=Quaternion.Euler(0,yaw,0);
            Vector3 glassSize=yaw==0?new Vector3(w-.20f,h-.18f,.045f):new Vector3(.045f,h-.18f,w-.20f);
            Box("window glass",p,pos,glassSize,glass,q);
            Vector3 top=yaw==0?new Vector3(w,.10f,.10f):new Vector3(.10f,.10f,w);
            Vector3 side=yaw==0?new Vector3(.10f,h,.10f):new Vector3(.10f,h,.10f);
            Box("window top",p,pos+Vector3.up*(h*.5f),top,trim,q);
            Box("window bottom",p,pos-Vector3.up*(h*.5f),top,trim,q);
            if(yaw==0){ Box("window left",p,pos+Vector3.left*(w*.5f),side,trim,q); Box("window right",p,pos+Vector3.right*(w*.5f),side,trim,q); Box("window mullion",p,pos,new Vector3(.07f,h-.10f,.08f),trim,q); }
            else { Box("window left",p,pos+Vector3.back*(w*.5f),new Vector3(.10f,h,.10f),trim,q); Box("window right",p,pos+Vector3.forward*(w*.5f),new Vector3(.10f,h,.10f),trim,q); Box("window mullion",p,pos,new Vector3(.08f,h-.10f,.07f),trim,q); }
        }

        static void RemoveNamedChildren(Transform root,string name){ if(!root)return; for(int i=root.childCount-1;i>=0;i--){ Transform c=root.GetChild(i); if(c&&c.name.Equals(name,StringComparison.OrdinalIgnoreCase)) UnityEngine.Object.DestroyImmediate(c.gameObject); } }
        static void DestroyChild(Transform root,string name){ if(!root)return; Transform t=root.Find(name); if(t) UnityEngine.Object.DestroyImmediate(t.gameObject); }
        static void CleanupMissingScripts(){ foreach(GameObject g in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include,FindObjectsSortMode.None)) if(g&&g.scene.IsValid()) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(g); }

        static GameObject Box(string name,Transform parent,Vector3 pos,Vector3 scale,Material mat,Quaternion rot){ GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name; g.transform.SetParent(parent,true); g.transform.position=pos; g.transform.rotation=rot; g.transform.localScale=scale; Renderer r=g.GetComponent<Renderer>(); if(r) r.sharedMaterial=mat; Collider c=g.GetComponent<Collider>(); if(c) UnityEngine.Object.DestroyImmediate(c); return g; }
        static Shader PickShader(){ bool srp=GraphicsSettings.currentRenderPipeline!=null||GraphicsSettings.defaultRenderPipeline!=null; Shader s=srp?Shader.Find("Universal Render Pipeline/Lit"):Shader.Find("Standard"); if(!s||!s.isSupported)s=Shader.Find("Universal Render Pipeline/Simple Lit"); if(!s||!s.isSupported)s=Shader.Find("Standard"); if(!s||!s.isSupported)throw new Exception("No supported shader"); return s; }
        static Material Mat(Shader shader,string name,string hex,float smooth){ Directory.CreateDirectory(MatDir); string path=MatDir+"/"+name+".mat"; Material m=AssetDatabase.LoadAssetAtPath<Material>(path); Color c=Color.white; ColorUtility.TryParseHtmlString("#"+hex,out c); if(!m){m=new Material(shader);AssetDatabase.CreateAsset(m,path);} if(m.shader!=shader)m.shader=shader; if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c); if(m.HasProperty("_Color"))m.SetColor("_Color",c); if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth); EditorUtility.SetDirty(m); return m; }
    }
}
#endif
