#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Rosvik.Blackout;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikHouseCutawayV764 {
        const int Version = 764;
        const string Key = "ROSVIK_HOUSE_CUTAWAY_V764";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";

        static RosvikHouseCutawayV764(){ if(EditorPrefs.GetInt(Key,0)>=Version)return; EditorApplication.delayCall+=Auto; }

        [MenuItem("Rosvik/V76.4 HOUSE CUTAWAY + PLAYER VISIBILITY")]
        public static void Force(){ EditorPrefs.DeleteKey(Key); EditorApplication.delayCall+=Auto; }

        static void Auto(){
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=Auto;return;}
            if(!File.Exists(ScenePath))return;
            try{Apply();}catch(Exception ex){Debug.LogError("V76.4 FAILED: "+ex);}
        }

        static void Apply(){
            if(EditorSceneManager.GetActiveScene().path!=ScenePath)EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            CoziPlayerV57 player=UnityEngine.Object.FindFirstObjectByType<CoziPlayerV57>();
            if(!player)throw new Exception("PLAYER missing");

            GameObject world=GameObject.Find("WORLD EXPANSION V75 - HOUSE A");
            if(!world)throw new Exception("HOUSE A world root missing");
            Transform shell=world.transform.Find("HOUSE A");
            Transform details=world.transform.Find("HOUSE DETAILS");
            if(!shell||!details)throw new Exception("HOUSE A shell/details missing");

            // The giant roof was visually wrong for the top-down game. Remove it entirely.
            GameObject roof=GameObject.Find("HOUSE ROOF V76.2");
            if(roof)UnityEngine.Object.DestroyImmediate(roof);
            HouseInteriorVisibilityV762 oldVis=player.GetComponent<HouseInteriorVisibilityV762>();
            if(oldVis)UnityEngine.Object.DestroyImmediate(oldVis);

            HouseCutawayV764 cutaway=player.GetComponent<HouseCutawayV764>();
            if(!cutaway)cutaway=player.gameObject.AddComponent<HouseCutawayV764>();
            cutaway.houseShell=shell;
            cutaway.houseDetails=details.gameObject;
            cutaway.minXZ=new Vector2(-36.25f,2.62f);
            cutaway.maxXZ=new Vector2(-21.75f,15.85f);
            cutaway.wallAlpha=.20f;

            PlayerVisibilityGuardV764 guard=player.GetComponent<PlayerVisibilityGuardV764>();
            if(!guard)guard=player.gameObject.AddComponent<PlayerVisibilityGuardV764>();

            // Keep selective-loot occlusion. Outside the house its interior containers stay unavailable.
            HouseLootOcclusionV763 loot=player.GetComponent<HouseLootOcclusionV763>();
            if(!loot)loot=player.gameObject.AddComponent<HouseLootOcclusionV763>();
            loot.minXZ=new Vector2(-36.25f,2.62f);
            loot.maxXZ=new Vector2(-21.75f,15.85f);
            loot.enterZ=2.62f;

            // Reassert the dedicated house door controller without touching its working animation.
            GameObject doorRoot=GameObject.Find("door — husets ytterdörr");
            if(doorRoot){
                HouseFrontDoorV763 door=doorRoot.GetComponent<HouseFrontDoorV763>();
                if(door){door.interactionDistance=3.6f;EditorUtility.SetDirty(door);}
            }

            // Make sure the player is an active, visible scene object and the camera follows it.
            player.gameObject.SetActive(true);
            foreach(MeshRenderer r in player.GetComponentsInChildren<MeshRenderer>(true)){
                if(!r)continue;string n=r.gameObject.name.ToLowerInvariant();
                if(n.Contains("weather")||n.Contains("rain")||n.Contains("snow"))continue;
                r.enabled=true;EditorUtility.SetDirty(r);
            }
            CozyCameraV57 cam=UnityEngine.Object.FindFirstObjectByType<CozyCameraV57>();
            if(cam){cam.target=player.transform;EditorUtility.SetDirty(cam);}

            EditorUtility.SetDirty(cutaway);EditorUtility.SetDirty(guard);EditorUtility.SetDirty(loot);EditorUtility.SetDirty(player);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();EditorPrefs.SetInt(Key,Version);SceneView.RepaintAll();
            Debug.Log("V76.4 COMPLETE — roof removed, house interior hidden outside, walls fade transparent inside, and player/camera visibility is guarded.");
        }
    }
}
#endif
