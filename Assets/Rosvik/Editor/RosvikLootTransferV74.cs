#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Rosvik.Blackout;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikLootTransferV74 {
        const int Version=74;
        const string Key="ROSVIK_LOOT_TRANSFER_V74";
        const string ScenePath="Assets/Rosvik/Scenes/CozySchoolGame.unity";

        static RosvikLootTransferV74(){if(EditorPrefs.GetInt(Key,0)>=Version)return;EditorApplication.delayCall+=Auto;}

        [MenuItem("Rosvik/V74 SELECTIVE LOOT + CABINET SWING + STASH")]
        public static void Force(){EditorPrefs.DeleteKey(Key);EditorApplication.delayCall+=Auto;}

        static void Auto(){
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=Auto;return;}
            if(!File.Exists(ScenePath))return;
            try{Apply();}catch(Exception ex){Debug.LogError("V74 FAILED: "+ex);}
        }

        static void Apply(){
            if(EditorSceneManager.GetActiveScene().path!=ScenePath)EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            CoziPlayerV57 player=UnityEngine.Object.FindFirstObjectByType<CoziPlayerV57>();
            if(!player)throw new Exception("PLAYER missing");

            SurvivalLootTransferV74 ui=player.GetComponent<SurvivalLootTransferV74>();
            if(!ui)ui=player.gameObject.AddComponent<SurvivalLootTransferV74>();
            ui.enabled=true;ui.backpackCapacityKg=34f;ui.discoveryRange=4.5f;
            CabinetSwingRuntimeV74 swingGuard=player.GetComponent<CabinetSwingRuntimeV74>();
            if(!swingGuard)swingGuard=player.gameObject.AddComponent<CabinetSwingRuntimeV74>();
            swingGuard.enabled=true;

            int converted=0,fixedSwings=0;
            CozyInteractableV57[] interactables=UnityEngine.Object.FindObjectsByType<CozyInteractableV57>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            foreach(CozyInteractableV57 x in interactables){
                if(!x||!x.gameObject.scene.IsValid())continue;

                if(x.kind==CozyInteractableV57.Kind.Cabinet){
                    Vector3 a=CorrectSideSwing(x.movingPart,x.openEuler,x.closedEuler);
                    Vector3 b=CorrectSideSwing(x.movingPart2,x.openEuler2,x.closedEuler2);
                    if(a!=x.openEuler||b!=x.openEuler2){x.openEuler=a;x.openEuler2=b;fixedSwings++;EditorUtility.SetDirty(x);}
                }

                bool hasPrimary=!string.IsNullOrWhiteSpace(x.itemName);
                bool hasExtras=false;
                if(x.extraItems!=null)foreach(string s in x.extraItems)if(!string.IsNullOrWhiteSpace(s)){hasExtras=true;break;}
                bool hasLoot=hasPrimary||hasExtras;
                if(!hasLoot||(x.kind!=CozyInteractableV57.Kind.Cabinet&&x.kind!=CozyInteractableV57.Kind.Loot))continue;

                LootContainerV74 c=x.GetComponent<LootContainerV74>();
                if(!c)c=x.gameObject.AddComponent<LootContainerV74>();
                c.displayName=x.displayName;
                c.radius=Mathf.Max(1.65f,x.radius);
                c.movingPart=x.movingPart;c.movingPart2=x.movingPart2;
                c.closedEuler=x.closedEuler;c.closedEuler2=x.closedEuler2;
                c.openEuler=CorrectSideSwing(x.movingPart,x.openEuler,x.closedEuler);
                c.openEuler2=CorrectSideSwing(x.movingPart2,x.openEuler2,x.closedEuler2);
                c.revealOnOpen=x.revealOnOpen;
                c.highlightRenderer=x.highlightRenderer;
                c.animationTime=Mathf.Max(.30f,x.animationTime);

                List<string> items=new List<string>();List<int> counts=new List<int>();
                if(hasPrimary){items.Add(x.itemName);counts.Add(1);}
                if(x.extraItems!=null){
                    for(int i=0;i<x.extraItems.Length;i++){
                        string item=x.extraItems[i];if(string.IsNullOrWhiteSpace(item))continue;
                        int count=x.extraCounts!=null&&i<x.extraCounts.Length?Mathf.Max(1,x.extraCounts[i]):1;
                        items.Add(item);counts.Add(count);
                    }
                }
                c.items=items.ToArray();c.counts=counts.ToArray();
                EditorUtility.SetDirty(c);
                UnityEngine.Object.DestroyImmediate(x);
                converted++;
            }

            player.SetObjective("Överlev dagen. Öppna behållare och välj själv vad som ska följa med i ryggsäcken.");
            EditorUtility.SetDirty(player);EditorUtility.SetDirty(ui);EditorUtility.SetDirty(swingGuard);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorPrefs.SetInt(Key,Version);
            SceneView.RepaintAll();
            Debug.Log("V74 COMPLETE — "+converted+" loot containers now use selective two-way transfer, cabinet doors swing outward correctly, backpack weight is enforced and stash play is active. Corrected remaining cabinet swings: "+fixedSwings+".");
        }

        static Vector3 CorrectSideSwing(Transform hinge,Vector3 open,Vector3 closed){
            if(!hinge||hinge.childCount==0)return open;
            if(Mathf.Abs(open.y)<35f||Mathf.Abs(open.y)<Mathf.Abs(open.x))return open;
            float angle=Mathf.Clamp(Mathf.Abs(open.y),88f,108f);
            float sign=0f;
            string n=hinge.name.ToLowerInvariant();
            if(n.Contains("left")||n.Contains("vänster"))sign=1f;
            else if(n.Contains("right")||n.Contains("höger"))sign=-1f;
            else{
                Transform leaf=hinge.GetChild(0);
                if(Mathf.Abs(leaf.localPosition.x)>.02f)sign=Mathf.Sign(leaf.localPosition.x);
            }
            if(Mathf.Approximately(sign,0f))sign=Mathf.Sign(open.y);
            return new Vector3(closed.x,sign*angle,closed.z);
        }
    }
}
#endif
