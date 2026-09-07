#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Rosvik.Blackout;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikSchoolEntranceV973 {
        const int Version = 973;
        const string Key = "ROSVIK_SCHOOL_ENTRANCE_V973";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string CampusName = "ROSVIK CAMPUS · V92 ASTRA BENCHMARK";
        const string SchoolName = "ROSVIKS SKOLA · V92";
        const string DoorRootName = "V97.3 WORKING SCHOOL ENTRANCE";
        static int retries;

        static RosvikSchoolEntranceV973() {
            if (EditorPrefs.GetInt(Key, 0) >= Version) return;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V97.3 FIX SCHOOL ENTRANCE")]
        public static void Force() {
            EditorPrefs.DeleteKey(Key);
            retries = 0;
            EditorApplication.delayCall += Auto;
        }

        static void Auto() {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) {
                EditorApplication.delayCall += Auto;
                return;
            }
            if (!File.Exists(ScenePath)) return;
            try {
                if (!Apply() && retries++ < 20) EditorApplication.delayCall += Auto;
            } catch (Exception ex) {
                Debug.LogError("V97.3 SCHOOL ENTRANCE FAILED: " + ex);
            }
        }

        static bool Apply() {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject campus = GameObject.Find(CampusName);
            if (!campus) return false;
            Transform school = FindDeep(campus.transform, SchoolName);
            if (!school) return false;

            GameObject old = GameObject.Find(DoorRootName);
            if (old) UnityEngine.Object.DestroyImmediate(old);

            // V93 locks the school to the OSM anchor. The south entrance is centered at x=0, z≈0.73.
            Vector3 entrance = new Vector3(0f, 0f, .73f);

            // Remove only collision that crosses the actual doorway. Visual facade pieces remain intact.
            Bounds doorway = new Bounds(new Vector3(entrance.x, 1.25f, entrance.z), new Vector3(2.65f, 2.55f, 1.15f));
            int cleared = 0;
            foreach (Collider c in school.GetComponentsInChildren<Collider>(true)) {
                if (!c || c.isTrigger) continue;
                string n = c.gameObject.name.ToLowerInvariant();
                bool architecture = n.Contains("front wall") || n.Contains("entrance") || n.Contains("portal") ||
                                    n.Contains("glass") || n.Contains("plinth") || n.Contains("facade") || n.Contains("door");
                if (!architecture) continue;
                if (!doorway.Intersects(c.bounds)) continue;
                c.enabled = false;
                EditorUtility.SetDirty(c);
                cleared++;
            }

            Material doorMat = FindMaterial(school, "wood") ?? FindMaterial(school, "brick") ?? FirstMaterial(school);
            Material metalMat = FindMaterial(school, "metal") ?? doorMat;
            Material glassMat = FindMaterial(school, "glass") ?? doorMat;

            GameObject root = new GameObject(DoorRootName);
            root.transform.SetParent(school, true);
            root.transform.position = entrance;

            // Threshold makes the transition from snow to the interior smooth for the CharacterController.
            GameObject threshold = Cube("entrance threshold", root.transform,
                new Vector3(0f, .045f, .70f), new Vector3(2.25f, .09f, 1.25f), metalMat, true);
            BoxCollider tc = threshold.GetComponent<BoxCollider>();
            if (tc) tc.size = new Vector3(1f, 1f, 1f);

            // Proper hinge: pivot sits at the left jamb, leaf is offset from it, so it swings instead of rotating around its centre.
            GameObject hinge = new GameObject("school outer door hinge");
            hinge.transform.SetParent(root.transform, true);
            hinge.transform.position = new Vector3(-1.02f, 1.12f, .56f);
            hinge.transform.rotation = Quaternion.identity;

            GameObject leaf = Cube("school outer door leaf", hinge.transform,
                new Vector3(-.07f + .96f, 0f, 0f), new Vector3(1.92f, 2.18f, .11f), doorMat, true, false);
            leaf.transform.localPosition = new Vector3(.96f, 0f, 0f);
            leaf.transform.localRotation = Quaternion.identity;

            // Small upper pane and handle make it readable as a door rather than another wall panel.
            GameObject pane = Cube("door window", leaf.transform, Vector3.zero,
                new Vector3(.72f, .62f, .035f), glassMat, false, true);
            pane.transform.localPosition = new Vector3(.15f, .28f, -.075f);
            pane.transform.localScale = new Vector3(.42f, .26f, .035f);

            GameObject handle = Cube("door handle", leaf.transform, Vector3.zero,
                new Vector3(.05f, .05f, .12f), metalMat, false, true);
            handle.transform.localPosition = new Vector3(.38f, -.02f, -.12f);
            handle.transform.localScale = new Vector3(.035f, .035f, .12f);

            CozyInteractableV57 door = hinge.AddComponent<CozyInteractableV57>();
            door.kind = CozyInteractableV57.Kind.Door;
            door.displayName = "skolans ytterdörr";
            door.radius = 2.65f;
            door.movingPart = hinge.transform;
            door.closedEuler = Vector3.zero;
            door.openEuler = new Vector3(0f, -105f, 0f); // swings inward, away from the player standing outside
            door.animationTime = .28f;
            door.highlightRenderer = leaf.GetComponent<Renderer>();
            door.objectiveAfterUse = "Gå in i skolan och sök igenom rummen efter användbar utrustning.";

            // Make sure no stale school-door interactable in the same doorway competes for E focus.
            foreach (CozyInteractableV57 x in school.GetComponentsInChildren<CozyInteractableV57>(true)) {
                if (!x || x == door) continue;
                if (Vector3.Distance(x.transform.position, entrance + Vector3.up) > 2.2f) continue;
                string d = (x.displayName ?? "").ToLowerInvariant();
                if (x.kind == CozyInteractableV57.Kind.Door || d.Contains("ytterdörr") || d.Contains("skol"))
                    x.enabled = false;
            }

            CoziPlayerV57 player = UnityEngine.Object.FindFirstObjectByType<CoziPlayerV57>();
            if (player) {
                player.SetObjective("Gå in i Rosviks skola genom huvudentrén. Tryck E vid ytterdörren.");
                EditorUtility.SetDirty(player);
            }

            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(hinge);
            EditorUtility.SetDirty(door);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorPrefs.SetInt(Key, Version);
            SceneView.RepaintAll();
            Debug.Log("V97.3 COMPLETE — school entrance passage cleared (" + cleared + " blocking colliders), real hinged outer door installed, and E interaction wired.");
            return true;
        }

        static GameObject Cube(string name, Transform parent, Vector3 worldPos, Vector3 scale, Material mat, bool collider, bool local = false) {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.SetParent(parent, !local);
            if (local) g.transform.localPosition = worldPos; else g.transform.position = worldPos;
            g.transform.localRotation = Quaternion.identity;
            g.transform.localScale = scale;
            Renderer r = g.GetComponent<Renderer>(); if (r && mat) r.sharedMaterial = mat;
            Collider c = g.GetComponent<Collider>(); if (!collider && c) UnityEngine.Object.DestroyImmediate(c);
            return g;
        }

        static Material FindMaterial(Transform root, string part) {
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true)) {
                if (!r || !r.sharedMaterial) continue;
                if (r.sharedMaterial.name.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    r.gameObject.name.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0) return r.sharedMaterial;
            }
            return null;
        }

        static Material FirstMaterial(Transform root) {
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true)) if (r && r.sharedMaterial) return r.sharedMaterial;
            return null;
        }

        static Transform FindDeep(Transform root, string exact) {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == exact) return t;
            return null;
        }
    }
}
#endif
