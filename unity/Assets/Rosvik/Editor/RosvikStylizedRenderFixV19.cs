#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikStylizedRenderFixV19 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_STYLIZED_RENDER_FIX_VERSION";
        const int Version = 19;

        static RosvikStylizedRenderFixV19() => EditorApplication.delayCall += Apply;

        [MenuItem("Rosvik/Apply Stylized Render Fix V19")]
        public static void Apply() {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorPrefs.GetInt(Key, 0) >= Version && File.Exists(ScenePath)) return;

            try {
                if (!File.Exists(ScenePath)) {
                    RosvikStylizedMapV18.Build();
                    if (!File.Exists(ScenePath)) return;
                }

                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                GameObject root = GameObject.Find("ROSVIK_STYLIZED_MAP_V18");
                if (!root) root = GameObject.Find("ROSVIK_STYLIZED_GAME_V19");
                if (!root) {
                    Debug.LogError("ROSVIK V19: stylized root not found.");
                    return;
                }

                int repaired = 0;
                foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true)) {
                    Mesh mesh = mf.sharedMesh;
                    if (!mesh || mesh.vertexCount < 3) continue;

                    // V18's flat XZ meshes were triangulated with the opposite winding
                    // for a camera looking down from +Y. Unity therefore culled the fills,
                    // leaving only line geometry visible. Fix only downward-facing meshes.
                    Vector3[] normals = mesh.normals;
                    if (normals == null || normals.Length == 0) {
                        mesh.RecalculateNormals();
                        normals = mesh.normals;
                    }

                    float avgY = 0f;
                    for (int i = 0; i < normals.Length; i++) avgY += normals[i].y;
                    avgY /= Mathf.Max(1, normals.Length);
                    if (avgY >= -0.05f) continue;

                    int[] tri = mesh.triangles;
                    for (int i = 0; i + 2 < tri.Length; i += 3) {
                        int tmp = tri[i + 1];
                        tri[i + 1] = tri[i + 2];
                        tri[i + 2] = tmp;
                    }
                    mesh.triangles = tri;
                    mesh.RecalculateNormals();
                    mesh.RecalculateBounds();
                    repaired++;
                }

                Camera cam = Camera.main;
                if (cam) {
                    cam.orthographic = true;
                    cam.orthographicSize = 18f;
                    cam.backgroundColor = new Color(.105f, .12f, .105f);
                }

                RosvikTopDownCamera rig = UnityEngine.Object.FindFirstObjectByType<RosvikTopDownCamera>();
                if (rig) {
                    rig.orthographicSize = 18f;
                    rig.minSize = 9f;
                    rig.maxSize = 34f;
                    rig.zoomStep = 1.25f;
                }

                RosvikMapHUD hud = root.GetComponent<RosvikMapHUD>();
                if (hud) hud.maxLabelDistance = 115f;

                Transform player = Find(root.transform, "PLAYER");
                if (player) {
                    foreach (MeshRenderer r in player.GetComponentsInChildren<MeshRenderer>(true)) {
                        // Keep the gameplay collider at real scale while making the marker
                        // a touch easier to read from above.
                        if (r.transform != player) r.transform.localScale *= 1.18f;
                    }
                }

                root.name = "ROSVIK_STYLIZED_GAME_V19";
                Selection.activeObject = null;
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
                EditorPrefs.SetInt(Key, Version);
                Debug.Log("ROSVIK V19 APPLIED: repaired " + repaired + " culled flat meshes; map fills, vegetation, puddles and player marker now face the top-down camera.");
            } catch (Exception ex) {
                Debug.LogError("ROSVIK V19 FAILED: " + ex);
            }
        }

        static Transform Find(Transform root, string name) {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }
    }
}
#endif
