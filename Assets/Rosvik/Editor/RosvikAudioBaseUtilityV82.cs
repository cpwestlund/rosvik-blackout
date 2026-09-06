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
    public static class RosvikAudioBaseUtilityV82 {
        const int Version=82;
        const string Key="ROSVIK_AUDIO_BASE_UTILITY_V82";
        const string ScenePath="Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string GroupName="V82 BASE UTILITY";
        const string MatDir="Assets/Rosvik/GeneratedV82";

        static RosvikAudioBaseUtilityV82(){if(EditorPrefs.GetInt(Key,0)>=Version)return;EditorApplication.delayCall+=Auto;}
        [MenuItem("Rosvik/V82 AUDIO MIX + BASE STORAGE + FIREWOOD")]
        public static void Force(){EditorPrefs.DeleteKey(Key);EditorApplication.delayCall+=Auto;}
        static void Auto(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=Auto;return;}if(!File.Exists(ScenePath))return;try{Apply();}catch(Exception ex){Debug.LogError("V82 FAILED: "+ex);}}

        static void Apply(){
            if(EditorSceneManager.GetActiveScene().path!=ScenePath)EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            CoziPlayerV57 player=UnityEngine.Object.FindFirstObjectByType<CoziPlayerV57>();if(!player)throw new Exception("PLAYER missing");

            WorldSoundscapeV80 old=player.GetComponent<WorldSoundscapeV80>();if(!old)old=UnityEngine.Object.FindFirstObjectByType<WorldSoundscapeV80>();
            if(old){old.masterVolume=.96f;old.windVolume=.66f;old.footstepVolume=.24f;old.interactionVolume=.82f;EditorUtility.SetDirty(old);}
            WorldAudioEnhancerV82 audio=player.GetComponent<WorldAudioEnhancerV82>();if(!audio)audio=player.gameObject.AddComponent<WorldAudioEnhancerV82>();audio.enabled=true;EditorUtility.SetDirty(audio);

            GameObject oldGroup=GameObject.Find(GroupName);if(oldGroup)UnityEngine.Object.DestroyImmediate(oldGroup);
            GameObject root=new GameObject(GroupName);
            Shader shader=PickShader();Material wood=Mat(shader,"wood","6a4934"),woodDark=Mat(shader,"wood_dark","3f3027"),metal=Mat(shader,"metal","4b5250"),cloth=Mat(shader,"cloth","796e5d");

            Transform houseDetails=FindTransform("V79 NORTH NEIGHBORHOOD - HOUSE B/HOUSE B/DETAILS");
            if(!houseDetails)houseDetails=root.transform;
            BuildBaseChest(houseDetails,wood,woodDark,metal,cloth);

            Transform shed=FindTransform("V79 NORTH NEIGHBORHOOD - HOUSE B/VEDBOD + YARD");
            if(!shed)shed=root.transform;
            BuildFirewoodPile(shed,wood,woodDark);

            int removed=0;
            foreach(GameObject go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
                if(!go||!go.scene.IsValid())continue;int n=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);if(n>0){removed+=n;GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);EditorUtility.SetDirty(go);}
            }

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();EditorPrefs.SetInt(Key,Version);SceneView.RepaintAll();
            Debug.Log("V82 COMPLETE — louder world mix, quieter footsteps, stronger doors/loot/wind/stove ambience, base stash and usable firewood pile. Missing scripts removed: "+removed);
        }

        static void BuildBaseChest(Transform p,Material body,Material lidMat,Material metal,Material cloth){
            GameObject root=new GameObject("BASFÖRRÅD");root.transform.SetParent(p,true);root.transform.position=new Vector3(-30.5f,0f,23.55f);root.transform.rotation=Quaternion.Euler(0,0,0);
            GameObject shell=Box("storage chest body",root.transform,new Vector3(0,.36f,0),new Vector3(1.55f,.68f,.86f),body,true);
            Box("metal bands",root.transform,new Vector3(0,.38f,-.44f),new Vector3(1.25f,.12f,.06f),metal,false);
            Transform hinge=Group(root.transform,"lid hinge");hinge.localPosition=new Vector3(0,.70f,.40f);Box("storage lid",hinge,new Vector3(0,.03f,-.40f),new Vector3(1.58f,.12f,.86f),lidMat,true);
            Box("folded blanket",root.transform,new Vector3(.38f,.78f,0),new Vector3(.42f,.10f,.36f),cloth,false);
            LootContainerV74 c=root.AddComponent<LootContainerV74>();c.displayName="basförrådet";c.radius=2.35f;c.movingPart=hinge;c.closedEuler=Vector3.zero;c.openEuler=new Vector3(-82,0,0);c.highlightRenderer=shell.GetComponent<Renderer>();c.items=Array.Empty<string>();c.counts=Array.Empty<int>();c.animationTime=.28f;c.RebuildContents();c.RefreshHighlight();
        }

        static void BuildFirewoodPile(Transform p,Material wood,Material dark){
            GameObject root=new GameObject("ANVÄNDBAR VEDTRAVE");root.transform.SetParent(p,true);root.transform.position=new Vector3(-39.55f,0f,30.65f);
            for(int row=0;row<3;row++)for(int i=0;i<4;i++){
                GameObject log=GameObject.CreatePrimitive(PrimitiveType.Cylinder);log.name="torr ved";log.transform.SetParent(root.transform,false);log.transform.localPosition=new Vector3((row-1)*.32f,.18f+i*.24f,0);log.transform.localRotation=Quaternion.Euler(0,0,90);log.transform.localScale=new Vector3(.12f,.48f,.12f);log.GetComponent<Renderer>().sharedMaterial=(i+row)%2==0?wood:dark;Collider c=log.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);
            }
            FirewoodPileV82 pile=root.AddComponent<FirewoodPileV82>();pile.remaining=18;pile.takePerUse=2;pile.radius=2.4f;EditorUtility.SetDirty(pile);
        }

        static Transform FindTransform(string path){GameObject g=GameObject.Find(path);if(g)return g.transform;string[] parts=path.Split('/');GameObject root=GameObject.Find(parts[0]);if(!root)return null;Transform t=root.transform;for(int i=1;i<parts.Length;i++){t=t.Find(parts[i]);if(!t)return null;}return t;}
        static Transform Group(Transform p,string n){GameObject g=new GameObject(n);g.transform.SetParent(p,false);return g.transform;}
        static GameObject Box(string n,Transform p,Vector3 pos,Vector3 size,Material m,bool collider){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;if(!collider){Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);}return g;}
        static Material Mat(Shader shader,string name,string hex){Directory.CreateDirectory(MatDir);string path=MatDir+"/"+name+".mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(path);Color c=Color.white;ColorUtility.TryParseHtmlString("#"+hex,out c);if(!m){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}if(m.shader!=shader)m.shader=shader;if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);if(m.HasProperty("_Color"))m.SetColor("_Color",c);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",.12f);EditorUtility.SetDirty(m);return m;}
        static Shader PickShader(){bool srp=GraphicsSettings.currentRenderPipeline!=null||GraphicsSettings.defaultRenderPipeline!=null;Shader s=srp?Shader.Find("Universal Render Pipeline/Lit"):Shader.Find("Standard");if(!s||!s.isSupported)s=Shader.Find("Universal Render Pipeline/Simple Lit");if(!s||!s.isSupported)s=Shader.Find("Standard");if(!s||!s.isSupported)throw new Exception("No supported shader");return s;}
    }
}
#endif
