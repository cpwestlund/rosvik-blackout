#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikCozyCleanupV67 {
        const int Version = 67;
        const string Key = "ROSVIK_COZY_CLEANUP_V67";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";

        static RosvikCozyCleanupV67() {
            if (EditorPrefs.GetInt(Key, 0) >= Version) return;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V67 COZY CLEANUP - ROOM LOGIC + ZERO OVERLAP")]
        public static void Force() {
            EditorPrefs.DeleteKey(Key);
            EditorApplication.delayCall += Auto;
        }

        static void Auto() {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) {
                EditorApplication.delayCall += Auto;
                return;
            }
            if (!File.Exists(ScenePath)) return;
            try { Apply(); }
            catch (Exception ex) { Debug.LogError("V67 COZY CLEANUP FAILED: " + ex); }
        }

        static void Apply() {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // Keep the V64/V65 art direction. Remove only props that break room logic,
            // block circulation, or visibly double up with earlier dressing.
            int removed = 0;

            // Corridor: keep the two original V58 rugs only. V65 added a second runner layer.
            removed += DestroyAllNamed("corridor runner");
            removed += DestroyAllNamed("runner top border");
            removed += DestroyAllNamed("runner bottom border");

            // Corridor must read as circulation, not a lounge / plant display.
            removed += DestroyAllNamed("waiting sofa");
            removed += DestroyAllNamed("hall plant");
            removed += DestroyAllNamed("corridor plant A");
            removed += DestroyAllNamed("corridor plant B");
            removed += DestroyAllNamed("corridor tiny plant");

            // Classrooms are classrooms. Remove all lounge-chair / rug reading nooks and
            // floor plants introduced by V58/V64/V65. Shelves, desks, boards and posters stay.
            removed += DestroyAllNamed("reading rug");
            removed += DestroyAllNamed("reading chair");
            removed += DestroyAllNamed("class A reading rug");
            removed += DestroyAllNamed("class A reading chair");
            removed += DestroyAllNamed("class A window plant");
            removed += DestroyAllNamed("class B plant");
            removed += DestroyAllNamed("class B plant two");

            // Make the surviving classroom layout feel intentional by snapping the obvious
            // school furniture to clean wall-adjacent positions. No new scatter is added.
            MoveIfFound("class A side shelf", new Vector3(-17.05f, 0f, 8.35f), 90f);
            MoveIfFound("reading shelf", new Vector3(-6.85f, 0f, 14.15f), 0f);
            MoveIfFound("class B side shelf", new Vector3(-7.55f, 0f, 12.95f), 90f);

            // Staff room is the only lounge-heavy room. Keep sofa/armchair/plant there,
            // but make sure the plant and lamp hug the back/side walls rather than float in circulation.
            MoveIfFound("staff plant", new Vector3(.80f, 0f, 14.55f), 0f);
            MoveIfFound("floor lamp", new Vector3(6.25f, 0f, 14.45f), 0f);
            MoveIfFound("staff spare chair", new Vector3(6.20f, 0f, 8.65f), 90f);

            // Library may have one reading rug/chair, but keep plants and furniture against edges.
            MoveIfFound("library plant", new Vector3(8.15f, 0f, 14.55f), 0f);
            MoveIfFound("library reading lamp", new Vector3(11.55f, 0f, 9.25f), 0f);

            // Safety cleanup: remove any duplicate decor objects at essentially the same XZ position.
            // This catches accidental repeated pushes while preserving deliberately stacked assemblies
            // such as doors, windows, tables and cabinets.
            removed += RemoveDuplicateLooseDecor();

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorPrefs.SetInt(Key, Version);
            SceneView.RepaintAll();
            Debug.Log("V67 COZY CLEANUP COMPLETE — removed " + removed + " illogical/overlapping loose props. V64/V65 style retained; classrooms are school-only, corridor is clear, lounge furniture stays in staff/library.");
        }

        static int DestroyAllNamed(string exactName) {
            int n = 0;
            Transform[] all = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var victims = new List<GameObject>();
            foreach (Transform t in all) {
                if (!t || t.name != exactName) continue;
                if (!t.gameObject.scene.IsValid()) continue;
                victims.Add(t.gameObject);
            }
            foreach (GameObject g in victims) {
                if (!g) continue;
                UnityEngine.Object.DestroyImmediate(g);
                n++;
            }
            return n;
        }

        static void MoveIfFound(string exactName, Vector3 pos, float yaw) {
            Transform[] all = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Transform t in all) {
                if (!t || t.name != exactName || !t.gameObject.scene.IsValid()) continue;
                t.position = pos;
                t.rotation = Quaternion.Euler(0f, yaw, 0f);
                GroundTo(t.gameObject, pos.y);
            }
        }

        static void GroundTo(GameObject go, float y) {
            Renderer[] rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return;
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            go.transform.position += Vector3.up * (y - b.min.y);
        }

        static int RemoveDuplicateLooseDecor() {
            int removed = 0;
            string[] duplicateNames = {
                "corridor display books", "corridor tiny plant", "class A desk books one",
                "class A desk books two", "class B desk books", "staff shelf books",
                "staff shelf plant", "library table books A", "library table books B"
            };

            foreach (string name in duplicateNames) {
                Transform[] all = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                var kept = new List<Transform>();
                var kill = new List<GameObject>();
                foreach (Transform t in all) {
                    if (!t || t.name != name || !t.gameObject.scene.IsValid()) continue;
                    bool dup = false;
                    foreach (Transform k in kept) {
                        Vector2 a = new Vector2(t.position.x, t.position.z);
                        Vector2 b = new Vector2(k.position.x, k.position.z);
                        if ((a - b).sqrMagnitude < .04f) { dup = true; break; }
                    }
                    if (dup) kill.Add(t.gameObject); else kept.Add(t);
                }
                foreach (GameObject g in kill) {
                    if (!g) continue;
                    UnityEngine.Object.DestroyImmediate(g);
                    removed++;
                }
            }
            return removed;
        }
    }
}
#endif
