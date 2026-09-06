#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Rosvik.Blackout;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikHouseDoorHardFixV763 {
        const int Version = 763;
        const string Key = "ROSVIK_HOUSE_DOOR_HARD_FIX_V763";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";

        static RosvikHouseDoorHardFixV763() {
            if (EditorPrefs.GetInt(Key, 0) >= Version) return;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V76.3 HARD FIX HOUSE DOOR")]
        public static void Force() {
            EditorPrefs.DeleteKey(Key);
            EditorApplication.delayCall += Auto;
        }

        static void Auto() {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) {
                EditorApplication.delayCall += Auto;
                return;
            }
            if (!File.Exists(ScenePath)) return;
            try { Apply(); }
            catch (Exception ex) { Debug.LogError("V76.3 FAILED: " + ex); }
        }

        static void Apply() {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            CoziPlayerV57 player = UnityEngine.Object.FindFirstObjectByType<CoziPlayerV57>();
            if (!player) throw new Exception("PLAYER missing");

            GameObject doorRoot = GameObject.Find("door — husets ytterdörr");
            if (!doorRoot) {
                foreach (CozyInteractableV57 x in UnityEngine.Object.FindObjectsByType<CozyInteractableV57>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                    if (x && string.Equals(x.displayName, "husets ytterdörr", StringComparison.OrdinalIgnoreCase)) { doorRoot = x.gameObject; break; }
                }
            }
            if (!doorRoot) throw new Exception("house front door root missing");

            CozyInteractableV57 legacy = doorRoot.GetComponent<CozyInteractableV57>();
            Transform hinge = legacy && legacy.movingPart ? legacy.movingPart : doorRoot.transform.Find("door hinge");
            if (!hinge) throw new Exception("house front door hinge missing");

            Vector3 closed = legacy ? legacy.closedEuler : Vector3.zero;
            Vector3 open = new Vector3(closed.x, 108f, closed.z);

            HouseFrontDoorV763 hardDoor = doorRoot.GetComponent<HouseFrontDoorV763>();
            if (!hardDoor) hardDoor = doorRoot.AddComponent<HouseFrontDoorV763>();
            hardDoor.hinge = hinge;
            hardDoor.closedEuler = closed;
            hardDoor.openEuler = open;
            hardDoor.interactionDistance = 3.6f;
            hardDoor.animationTime = .26f;

            // Remove every old door controller from this entrance so one E press has one owner.
            DoorPassageV76 oldPassage = doorRoot.GetComponent<DoorPassageV76>();
            if (oldPassage) UnityEngine.Object.DestroyImmediate(oldPassage);
            if (legacy) UnityEngine.Object.DestroyImmediate(legacy);
            HouseDoorAccessV762 oldAccess = player.GetComponent<HouseDoorAccessV762>();
            if (oldAccess) UnityEngine.Object.DestroyImmediate(oldAccess);

            // Ensure the physical opening really is open. Keep only the animated leaf collider.
            Vector3 dp = doorRoot.transform.position;
            foreach (BoxCollider c in UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                if (!c || c.transform.IsChildOf(doorRoot.transform)) continue;
                Bounds b = c.bounds;
                if (Mathf.Abs(b.center.z - dp.z) > .42f) continue;
                if (b.max.x < dp.x - 1.02f || b.min.x > dp.x + 1.02f) continue;
                if (b.min.y > 1.5f) continue;
                string n = c.gameObject.name.ToLowerInvariant();
                if (n.Contains("floor") || n.Contains("ground") || n.Contains("path")) continue;
                c.enabled = false;
                EditorUtility.SetDirty(c);
            }

            HouseLootOcclusionV763 lootOcclusion = player.GetComponent<HouseLootOcclusionV763>();
            if (!lootOcclusion) lootOcclusion = player.gameObject.AddComponent<HouseLootOcclusionV763>();
            lootOcclusion.minXZ = new Vector2(-36.25f, 2.55f);
            lootOcclusion.maxXZ = new Vector2(-21.75f, 15.85f);
            lootOcclusion.enterZ = 2.58f;

            HouseInteriorVisibilityV762 vis = player.GetComponent<HouseInteriorVisibilityV762>();
            if (vis) {
                vis.minXZ = new Vector2(-36.25f, 2.58f);
                vis.maxXZ = new Vector2(-21.75f, 15.85f);
                EditorUtility.SetDirty(vis);
            }

            EditorUtility.SetDirty(hardDoor);
            EditorUtility.SetDirty(lootOcclusion);
            EditorUtility.SetDirty(player);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorPrefs.SetInt(Key, Version);
            SceneView.RepaintAll();
            Debug.Log("V76.3 COMPLETE — standalone front-door controller owns the entrance, doorway collision is cleared, and house loot is hidden/inactive until the player actually enters.");
        }
    }
}
#endif
