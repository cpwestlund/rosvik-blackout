#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Rosvik.Blackout;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikInteractionEquipmentV76 {
        const int Version=76;
        const string Key="ROSVIK_INTERACTION_EQUIPMENT_V76";
        const string ScenePath="Assets/Rosvik/Scenes/CozySchoolGame.unity";

        static RosvikInteractionEquipmentV76(){if(EditorPrefs.GetInt(Key,0)>=Version)return;EditorApplication.delayCall+=Auto;}
        [MenuItem("Rosvik/V76 INTERACTION + EQUIPMENT REBUILD")]
        public static void Force(){EditorPrefs.DeleteKey(Key);EditorApplication.delayCall+=Auto;}

        static void Auto(){
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=Auto;return;}
            if(!File.Exists(ScenePath))return;
            try{Apply();}catch(Exception ex){Debug.LogError("V76 FAILED: "+ex);}
        }

        static void Apply(){
            if(EditorSceneManager.GetActiveScene().path!=ScenePath)EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            CoziPlayerV57 player=UnityEngine.Object.FindFirstObjectByType<CoziPlayerV57>();if(!player)throw new Exception("PLAYER missing");
            SurvivalSystemsV69 survival=player.GetComponent<SurvivalSystemsV69>();if(!survival)throw new Exception("SurvivalSystemsV69 missing");
            SurvivalLootTransferV74 transfer=player.GetComponent<SurvivalLootTransferV74>();if(!transfer)transfer=player.gameObject.AddComponent<SurvivalLootTransferV74>();
            transfer.enabled=true;transfer.discoveryRange=5.0f;transfer.interactionRange=2.55f;transfer.backpackCapacityKg=34f;

            SurvivalEquipmentV76 equipment=player.GetComponent<SurvivalEquipmentV76>();if(!equipment)equipment=player.gameObject.AddComponent<SurvivalEquipmentV76>();equipment.enabled=true;
            SurvivalInventoryV76 inventory=player.GetComponent<SurvivalInventoryV76>();if(!inventory)inventory=player.gameObject.AddComponent<SurvivalInventoryV76>();inventory.enabled=true;inventory.backpackCapacityKg=34f;
            HouseShelterVisualV76 shelter=player.GetComponent<HouseShelterVisualV76>();if(!shelter)shelter=player.gameObject.AddComponent<HouseShelterVisualV76>();shelter.enabled=true;

            int doors=0;
            foreach(CozyInteractableV57 x in UnityEngine.Object.FindObjectsByType<CozyInteractableV57>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
                if(!x||x.kind!=CozyInteractableV57.Kind.Door)continue;
                DoorPassageV76 p=x.GetComponent<DoorPassageV76>();if(!p)p=x.gameObject.AddComponent<DoorPassageV76>();doors++;
                x.radius=Mathf.Max(x.radius,2.15f);
                if(string.Equals(x.displayName,"husets ytterdörr",StringComparison.OrdinalIgnoreCase)){
                    x.radius=2.55f;
                    x.openEuler=new Vector3(x.closedEuler.x,105f,x.closedEuler.z);
                    Transform root=x.transform;
                    foreach(Collider c in root.GetComponentsInChildren<Collider>(true)){
                        if(!c)continue;string n=c.gameObject.name.ToLowerInvariant();
                        if(n.Contains("frame l")||n.Contains("frame r"))c.enabled=false;
                    }
                }
                EditorUtility.SetDirty(x);EditorUtility.SetDirty(p);
            }

            player.suppressLegacyGui=true;survival.suppressLegacyGui=true;
            player.SetObjective("Överlev dagen. Gå in i huset, välj loot själv och bygg en fungerande utrustning på kroppen.");
            EditorUtility.SetDirty(player);EditorUtility.SetDirty(survival);EditorUtility.SetDirty(transfer);EditorUtility.SetDirty(equipment);EditorUtility.SetDirty(inventory);EditorUtility.SetDirty(shelter);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();EditorPrefs.SetInt(Key,Version);SceneView.RepaintAll();
            Debug.Log("V76 COMPLETE — loot opening repaired, wall-safe line of sight, upright item art, item explanations, wearable body slots/condition and house passage/shelter are active. Doors patched: "+doors+".");
        }
    }
}
#endif
