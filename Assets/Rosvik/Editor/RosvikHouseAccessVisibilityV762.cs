#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Rosvik.Blackout;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikHouseAccessVisibilityV762 {
        const int Version = 762;
        const string Key = "ROSVIK_HOUSE_ACCESS_VISIBILITY_V762";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string RoofName = "HOUSE ROOF V76.2";

        static RosvikHouseAccessVisibilityV762() {
            if (EditorPrefs.GetInt(Key, 0) >= Version) return;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V76.2 FIX HOUSE DOOR + HIDE INTERIOR OUTSIDE")]
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
            catch (Exception ex) { Debug.LogError("V76.2 FAILED: " + ex); }
        }

        static void Apply() {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            CoziPlayerV57 player = UnityEngine.Object.FindFirstObjectByType<CoziPlayerV57>();
            if (!player) throw new Exception("PLAYER missing");

            CozyInteractableV57 houseDoor = FindHouseDoor();
            if (!houseDoor) throw new Exception("husets ytterdörr missing");

            // Make the front door deterministic instead of relying on the old generic proximity scanner.
            houseDoor.enabled = true;
            houseDoor.radius = 2.65f;
            houseDoor.openEuler = new Vector3(houseDoor.closedEuler.x, 105f, houseDoor.closedEuler.z);
            houseDoor.animationTime = .28f;
            DoorPassageV76 passage = houseDoor.GetComponent<DoorPassageV76>();
            if (!passage) passage = houseDoor.gameObject.AddComponent<DoorPassageV76>();

            // Keep the frame posts physical but make sure no accidental wall collider fills the doorway.
            ClearAccidentalEntranceWallColliders(houseDoor.transform);

            HouseDoorAccessV762 access = player.GetComponent<HouseDoorAccessV762>();
            if (!access) access = player.gameObject.AddComponent<HouseDoorAccessV762>();
            access.houseDoor = houseDoor;
            access.interactionDistance = 2.80f;

            GameObject oldRoof = GameObject.Find(RoofName);
            if (oldRoof) UnityEngine.Object.DestroyImmediate(oldRoof);
            GameObject roof = BuildRoof();

            HouseInteriorVisibilityV762 visibility = player.GetComponent<HouseInteriorVisibilityV762>();
            if (!visibility) visibility = player.gameObject.AddComponent<HouseInteriorVisibilityV762>();
            visibility.roofRoot = roof;
            visibility.minXZ = new Vector2(-36.2f, 2.55f);
            visibility.maxXZ = new Vector2(-21.8f, 15.8f);

            EditorUtility.SetDirty(houseDoor);
            EditorUtility.SetDirty(passage);
            EditorUtility.SetDirty(access);
            EditorUtility.SetDirty(visibility);
            EditorUtility.SetDirty(player);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorPrefs.SetInt(Key, Version);
            SceneView.RepaintAll();
            Debug.Log("V76.2 COMPLETE — house front door now has dedicated interaction/pass-through and the interior is roof-hidden until the player enters.");
        }

        static CozyInteractableV57 FindHouseDoor() {
            foreach (CozyInteractableV57 x in UnityEngine.Object.FindObjectsByType<CozyInteractableV57>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                if (x && x.kind == CozyInteractableV57.Kind.Door && string.Equals(x.displayName, "husets ytterdörr", StringComparison.OrdinalIgnoreCase))
                    return x;
            }
            return null;
        }

        static void ClearAccidentalEntranceWallColliders(Transform doorRoot) {
            Vector3 door = doorRoot.position;
            foreach (BoxCollider c in UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                if (!c || c.transform.IsChildOf(doorRoot)) continue;
                string n = c.gameObject.name.ToLowerInvariant();
                if (!n.Contains("wall")) continue;
                Bounds b = c.bounds;
                if (Mathf.Abs(b.center.z - door.z) > .32f) continue;
                if (b.max.x < door.x - .82f || b.min.x > door.x + .82f) continue;
                if (b.min.y > 1.25f) continue;
                c.enabled = false;
                EditorUtility.SetDirty(c);
            }
        }

        static GameObject BuildRoof() {
            GameObject root = new GameObject(RoofName);
            Material mat = FindRoofMaterial();

            // Main pitched roof starts behind the entrance so the front door remains visible from outside.
            RoofBox("roof left", root.transform, new Vector3(-32.45f, 2.42f, 9.55f), new Vector3(7.25f, .20f, 12.25f), new Vector3(0, 0, -7f), mat);
            RoofBox("roof right", root.transform, new Vector3(-25.55f, 2.42f, 9.55f), new Vector3(7.25f, .20f, 12.25f), new Vector3(0, 0, 7f), mat);

            // Front corners cover the rooms but intentionally leave a clear visual notch over the entrance.
            RoofBox("front roof left", root.transform, new Vector3(-33.25f, 2.38f, 3.05f), new Vector3(5.45f, .18f, 1.25f), new Vector3(0, 0, -5f), mat);
            RoofBox("front roof right", root.transform, new Vector3(-24.75f, 2.38f, 3.05f), new Vector3(5.45f, .18f, 1.25f), new Vector3(0, 0, 5f), mat);

            GameObject chimney = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chimney.name = "chimney";
            chimney.transform.SetParent(root.transform, true);
            chimney.transform.position = new Vector3(-24.7f, 2.83f, 12.7f);
            chimney.transform.localScale = new Vector3(.55f, .82f, .62f);
            Renderer cr = chimney.GetComponent<Renderer>();
            if (cr && mat) cr.sharedMaterial = mat;
            Collider cc = chimney.GetComponent<Collider>();
            if (cc) UnityEngine.Object.DestroyImmediate(cc);

            return root;
        }

        static void RoofBox(string name, Transform parent, Vector3 pos, Vector3 scale, Vector3 euler, Material mat) {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.SetParent(parent, true);
            g.transform.position = pos;
            g.transform.rotation = Quaternion.Euler(euler);
            g.transform.localScale = scale;
            Renderer r = g.GetComponent<Renderer>();
            if (r && mat) r.sharedMaterial = mat;
            Collider c = g.GetComponent<Collider>();
            if (c) UnityEngine.Object.DestroyImmediate(c);
        }

        static Material FindRoofMaterial() {
            Renderer fallback = null;
            foreach (Renderer r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                if (!r || !r.sharedMaterial) continue;
                if (!fallback) fallback = r;
                string n = r.gameObject.name.ToLowerInvariant();
                if (n.Contains("counter top") || n.Contains("mailbox") || n.Contains("wood")) return r.sharedMaterial;
            }
            return fallback ? fallback.sharedMaterial : null;
        }
    }
}
#endif
