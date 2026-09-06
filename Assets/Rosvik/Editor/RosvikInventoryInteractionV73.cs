#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Rosvik.Blackout;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikInventoryInteractionV73 {
        const int Version=73;
        const string Key="ROSVIK_INVENTORY_INTERACTION_V73";
        const string ScenePath="Assets/Rosvik/Scenes/CozySchoolGame.unity";

        static RosvikInventoryInteractionV73(){if(EditorPrefs.GetInt(Key,0)>=Version)return;EditorApplication.delayCall+=Auto;}

        [MenuItem("Rosvik/V73 BACKPACK + PICKUP + CABINET FIX")]
        public static void Force(){EditorPrefs.DeleteKey(Key);EditorApplication.delayCall+=Auto;}

        static void Auto(){
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=Auto;return;}
            if(!File.Exists(ScenePath))return;
            try{Apply();}catch(Exception ex){Debug.LogError("V73 FAILED: "+ex);}
        }

        static void Apply(){
            if(EditorSceneManager.GetActiveScene().path!=ScenePath)EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            CoziPlayerV57 player=UnityEngine.Object.FindFirstObjectByType<CoziPlayerV57>();
            if(!player)throw new Exception("PLAYER missing");
            SurvivalSystemsV69 survival=player.GetComponent<SurvivalSystemsV69>();
            if(!survival)throw new Exception("SurvivalSystemsV69 missing");

            player.suppressLegacyGui=true;
            survival.suppressLegacyGui=true;

            SurvivalPresentationV70 p70=player.GetComponent<SurvivalPresentationV70>();if(p70)p70.enabled=false;
            SurvivalPresentationV72 p72=player.GetComponent<SurvivalPresentationV72>();if(p72)p72.enabled=false;
            SurvivalLegacyHudMaskV70 mask=player.GetComponent<SurvivalLegacyHudMaskV70>();if(mask)mask.enabled=false;

            SurvivalPresentationV73 p73=player.GetComponent<SurvivalPresentationV73>();
            if(!p73)p73=player.gameObject.AddComponent<SurvivalPresentationV73>();
            p73.enabled=true;
            p73.backpackCapacityKg=34f;
            p73.discoveryRange=4.3f;

            player.SetObjective("Överlev dagen. Sök rum och skåp efter det du faktiskt behöver.");

            EditorUtility.SetDirty(player);EditorUtility.SetDirty(survival);EditorUtility.SetDirty(p73);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorPrefs.SetInt(Key,Version);
            SceneView.RepaintAll();
            Debug.Log("V73 COMPLETE — compact HUD, backpack-style inventory, weight/capacity, world pickup/search prompts and corrected cabinet swing directions are active.");
        }
    }
}
#endif
