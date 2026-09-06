#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Rosvik.Blackout;
using UScene = UnityEngine.SceneManagement.Scene;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikTopDownPivotV51 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_TOPDOWN_V51_VERSION";
        const int Version = 51;
        const string GroupName = "32 TOPDOWN GAMEPLAY V51 - PROTOTYPE";
        const string GeneratedDir = "Assets/Rosvik/GeneratedV51";
        const long SchoolWay = 163199458;

        static RosvikTopDownPivotV51() {
            EditorApplication.delayCall -= Auto;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V51 PIVOT TO TOPDOWN GAMEPLAY")]
        public static void Force() {
            EditorPrefs.DeleteKey(Key);
            Build();
        }

        static void Auto() {
            if (EditorPrefs.GetInt(Key, 0) >= Version) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) {
                EditorApplication.delayCall += Auto;
                return;
            }
            Build();
        }

        static void Build() {
            try {
                if (!File.Exists(ScenePath)) return;
                UScene scene = EditorSceneManager.GetActiveScene();
                if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

                var ways = RosvikOsmV15.LoadWays();
                var school = ways == null ? null : ways.FirstOrDefault(w => w.Id == SchoolWay);
                if (school == null) throw new Exception("School footprint missing");

                Transform player = FindSceneTransform(scene, "PLAYER");
                if (!player) throw new Exception("PLAYER not found in RosvikHero");

                Vector3 schoolCenter = RosvikOsmV15.Centroid(school); schoolCenter.y = .03f;
                Vector3 entrance = ResolveEntrance(scene, schoolCenter);
                Vector3 forward = Flat(entrance - schoolCenter).normalized;
                if (forward.sqrMagnitude < .1f) forward = Vector3.back;
                Vector3 right = new Vector3(forward.z, 0f, -forward.x);

                // Hide the experimental exterior dressing. Nothing is deleted, so the pivot is reversible.
                SetTopLevelActive(scene, "31 SCHOOL CAMPUS V49 - TOP LEVEL VISIBLE REBUILD", false);
                SetAnywhereActive(scene, "30 SCHOOL CAMPUS V48 - FINAL EXTERIOR REBUILD", false);
                SetAnywhereActive(scene, "29 SCHOOL CAMPUS V47 - ENTRANCE ANCHORED", false);
                SetAnywhereActive(scene, "28 SCHOOL CAMPUS V46 - COZY DENSITY PASS", false);

                GameObject old = scene.GetRootGameObjects().FirstOrDefault(g => g.name == GroupName);
                if (old) UnityEngine.Object.DestroyImmediate(old);
                GameObject root = new GameObject(GroupName);

                Directory.CreateDirectory(GeneratedDir);
                AssetDatabase.Refresh();
                Shader shader = FindGoodShader(scene);
                if (!shader) throw new Exception("No supported shader found");

                Material floorMat = MakeMat("playable_apron", new Color(.30f,.31f,.25f), shader);
                Material lootMat = MakeMat("loot", new Color(.63f,.42f,.12f), shader);
                Material lootRed = MakeMat("first_aid", new Color(.58f,.16f,.12f), shader);
                Material markerMat = MakeMat("interaction_marker", new Color(.78f,.72f,.38f), shader);

                // One compact, controlled gameplay zone. No procedural scatter and no overlapping props.
                Vector3 zone = entrance + forward*7.5f; zone.y = .025f;
                FlatBox("V51 PLAYABLE ENTRANCE ZONE", root.transform, zone, right, 15f, 9f, .045f, floorMat);

                GameObject bag = LootBox(root.transform, "V51 SEARCH - SCHOOL BAG", zone-right*4.0f+forward*.7f, new Vector3(.72f,.22f,.48f), lootMat);
                GameObject maintenance = LootBox(root.transform, "V51 SEARCH - MAINTENANCE BOX", zone, new Vector3(.82f,.48f,.62f), lootMat);
                GameObject aid = LootBox(root.transform, "V51 SEARCH - FIRST AID", zone+right*4.0f-forward*.6f, new Vector3(.68f,.85f,.38f), lootRed);

                Disc(root.transform, bag.transform.position + Vector3.up*.01f, 1.15f, markerMat);
                Disc(root.transform, maintenance.transform.position + Vector3.up*.01f, 1.15f, markerMat);
                Disc(root.transform, aid.transform.position + Vector3.up*.01f, 1.15f, markerMat);

                // Spawn the player in the compact prototype so Play immediately tests the new direction.
                player.position = entrance + forward*4.1f;
                player.position = new Vector3(player.position.x, .16f, player.position.z);

                TopDownGameplayV51 gameplay = player.GetComponent<TopDownGameplayV51>();
                if (!gameplay) gameplay = player.gameObject.AddComponent<TopDownGameplayV51>();
                gameplay.loot.Clear();
                gameplay.loot.Add(new TopDownGameplayV51.LootEntry { spot=bag, displayName="skolväskan", lootText="Chokladbit" });
                gameplay.loot.Add(new TopDownGameplayV51.LootEntry { spot=maintenance, displayName="vaktmästarlådan", lootText="Ficklampa" });
                gameplay.loot.Add(new TopDownGameplayV51.LootEntry { spot=aid, displayName="första hjälpen-skåpet", lootText="Förband" });
                EditorUtility.SetDirty(gameplay);

                SetupCamera(scene, player);
                SimplifyLighting(scene);

                EditorPrefs.SetInt(Key, Version);
                Selection.activeGameObject = player.gameObject;
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
                SceneView.RepaintAll();
                Debug.Log("ROSVIK V51 SUCCESS: pivoted to near-top-down gameplay. Press Play: WASD, Shift, E to search, mouse wheel zoom.");
            }
            catch(Exception ex) {
                Debug.LogError("ROSVIK V51 FAILED: " + ex);
            }
        }

        static void SetupCamera(UScene scene, Transform player) {
            Camera cam = Resources.FindObjectsOfTypeAll<Camera>().FirstOrDefault(c => c && c.gameObject.scene == scene && c.CompareTag("MainCamera"));
            if (!cam) cam = Resources.FindObjectsOfTypeAll<Camera>().FirstOrDefault(c => c && c.gameObject.scene == scene);
            if (!cam) {
                GameObject go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }

            foreach (MonoBehaviour mb in cam.GetComponents<MonoBehaviour>()) {
                if (mb && mb.GetType().Name == "IsometricCameraRig") mb.enabled = false;
            }

            TopDownCameraRigV51 rig = cam.GetComponent<TopDownCameraRigV51>();
            if (!rig) rig = cam.gameObject.AddComponent<TopDownCameraRigV51>();
            rig.target = player;
            rig.pitch = 80f;
            rig.yaw = 0f;
            rig.distance = 28f;
            rig.orthographicSize = 11.5f;
            rig.minSize = 7.5f;
            rig.maxSize = 18f;
            cam.orthographic = true;
            cam.orthographicSize = 11.5f;
            cam.backgroundColor = new Color(.34f,.38f,.36f);
            EditorUtility.SetDirty(cam);
            EditorUtility.SetDirty(rig);
        }

        static Vector3 ResolveEntrance(UScene scene, Vector3 schoolCenter) {
            Transform canopy = Resources.FindObjectsOfTypeAll<Transform>()
                .FirstOrDefault(t => t && t.gameObject.scene == scene && t.name == "large timber canopy");
            if (canopy) { Vector3 p = canopy.position; p.y=.03f; return p; }

            // Fallback intentionally uses the same known side as the existing playable school slice.
            return schoolCenter + new Vector3(-12f,.03f,0f);
        }

        static void SimplifyLighting(UScene scene) {
            RenderSettings.fog = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.46f,.48f,.43f);
            Light sun = Resources.FindObjectsOfTypeAll<Light>().FirstOrDefault(l => l && l.gameObject.scene == scene && l.type == LightType.Directional);
            if (sun) { sun.intensity=.92f; sun.color=new Color(.96f,.91f,.80f); }
        }

        static GameObject LootBox(Transform parent, string name, Vector3 position, Vector3 size, Material mat) {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, true);
            go.transform.position = new Vector3(position.x, size.y*.5f+.055f, position.z);
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static void Disc(Transform parent, Vector3 pos, float diameter, Material mat) {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "V51 interaction ring";
            go.transform.SetParent(parent, true);
            go.transform.position = new Vector3(pos.x,.048f,pos.z);
            go.transform.localScale = new Vector3(diameter,.018f,diameter);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            Collider c = go.GetComponent<Collider>(); if (c) UnityEngine.Object.DestroyImmediate(c);
        }

        static GameObject FlatBox(string name, Transform parent, Vector3 pos, Vector3 widthAxis, float width, float depth, float thickness, Material mat) {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, true);
            go.transform.position = pos;
            widthAxis = Flat(widthAxis).normalized;
            if (widthAxis.sqrMagnitude < .01f) widthAxis = Vector3.right;
            Vector3 depthAxis = new Vector3(-widthAxis.z,0,widthAxis.x);
            go.transform.rotation = Quaternion.LookRotation(depthAxis, Vector3.up);
            go.transform.localScale = new Vector3(width, thickness, depth);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            Collider c = go.GetComponent<Collider>(); if (c) UnityEngine.Object.DestroyImmediate(c);
            return go;
        }

        static Material MakeMat(string name, Color color, Shader shader) {
            string path = GeneratedDir + "/" + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing) return existing;
            Material m = new Material(shader) { name = "V51 " + name };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static Shader FindGoodShader(UScene scene) {
            foreach (GameObject root in scene.GetRootGameObjects()) {
                foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true)) {
                    foreach (Material m in r.sharedMaterials) {
                        if (!m || !m.shader || !m.shader.isSupported) continue;
                        string s = m.shader.name ?? "";
                        if (s.IndexOf("InternalError", StringComparison.OrdinalIgnoreCase)>=0 || s.StartsWith("Hidden/")) continue;
                        return m.shader;
                    }
                }
            }
            Shader standard = Shader.Find("Standard"); if (standard && standard.isSupported) return standard;
            Shader urp = Shader.Find("Universal Render Pipeline/Lit"); if (urp && urp.isSupported) return urp;
            return null;
        }

        static Transform FindSceneTransform(UScene scene, string name) {
            return Resources.FindObjectsOfTypeAll<Transform>().FirstOrDefault(t => t && t.gameObject.scene == scene && t.name == name);
        }

        static void SetTopLevelActive(UScene scene, string name, bool active) {
            GameObject go = scene.GetRootGameObjects().FirstOrDefault(g => g.name == name);
            if (go) go.SetActive(active);
        }

        static void SetAnywhereActive(UScene scene, string name, bool active) {
            Transform t = Resources.FindObjectsOfTypeAll<Transform>().FirstOrDefault(x => x && x.gameObject.scene == scene && x.name == name);
            if (t) t.gameObject.SetActive(active);
        }

        static Vector3 Flat(Vector3 v) { v.y=0f; return v; }
    }
}
#endif
