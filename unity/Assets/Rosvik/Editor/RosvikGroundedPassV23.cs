#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rosvik.Blackout.EditorTools {
    /// <summary>
    /// V23 is a surgical close-camera pass over V22. It does not touch Rosvik geography.
    /// It removes the detached facade placeholders exposed by the closer camera, breaks
    /// the obvious ground tiling, tightens framing and replaces the primitive red pawn
    /// with a real CC0 rigged Kenney character with idle/run locomotion.
    /// </summary>
    [InitializeOnLoad]
    public static class RosvikGroundedPassV23 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_GROUNDED_PASS_VERSION";
        const int Version = 23;
        const string RootName = "ROSVIK_GROUNDED_PASS_V23";

        const string GeneratedRoot = "Assets/Rosvik/GeneratedV20";
        const string CharacterRoot = "Assets/Rosvik/ThirdParty/V23Character";
        const string CharacterModel = CharacterRoot + "/characterMedium.fbx";
        const string CharacterIdle = CharacterRoot + "/idle.fbx";
        const string CharacterRun = CharacterRoot + "/run.fbx";
        const string CharacterSkin = CharacterRoot + "/skaterMaleA.png";
        const string ControllerDir = "Assets/Rosvik/GeneratedV23";
        const string ControllerPath = ControllerDir + "/RosvikPlayer.controller";
        const string PlayerMaterialPath = ControllerDir + "/mat_player_skater.mat";

        struct Download {
            public string path, url;
            public Download(string p, string u) { path = p; url = u; }
        }

        static readonly Download[] CharacterDownloads = {
            new Download(CharacterModel, "https://raw.githubusercontent.com/ETdoFresh/kenney.nl/master/animated-characters-2/Model/characterMedium.fbx"),
            new Download(CharacterIdle,  "https://raw.githubusercontent.com/ETdoFresh/kenney.nl/master/animated-characters-2/Animations/idle.fbx"),
            new Download(CharacterRun,   "https://raw.githubusercontent.com/ETdoFresh/kenney.nl/master/animated-characters-2/Animations/run.fbx"),
            new Download(CharacterSkin,  "https://raw.githubusercontent.com/ETdoFresh/kenney.nl/master/animated-characters-2/Skins/skaterMaleA.png")
        };

        static RosvikGroundedPassV23() {
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        [MenuItem("Rosvik/Rebuild Grounded Pass V23")]
        public static void Force() {
            EditorPrefs.DeleteKey(Key);
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        static void TryApply() {
            if (EditorPrefs.GetInt(Key, 0) >= Version && File.Exists(ScenePath)) {
                EditorApplication.update -= TryApply;
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            // V22 owns the base/asset pass. Wait for it rather than ever rebuilding geography here.
            if (!File.Exists(ScenePath)) return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject root = GameObject.Find("ROSVIK_HERO_SLICE_V22") ?? GameObject.Find(RootName);
            if (!root) return;

            EditorApplication.update -= TryApply;
            Apply(scene, root);
        }

        static void Apply(UnityEngine.SceneManagement.Scene scene, GameObject root) {
            try {
                Directory.CreateDirectory(CharacterRoot);
                Directory.CreateDirectory(ControllerDir);
                AssetDatabase.Refresh();

                RemoveDetachedFacadeCues(root.transform);
                FixGroundMaterials();
                BuildGroundVariation(root.transform);

                Transform player = Find(root.transform, "PLAYER");
                if (!player) throw new InvalidOperationException("V23 could not find PLAYER in the V22 scene.");

                EnsureCharacterAssets();
                BuildRealCharacter(player);
                TuneCamera(player);
                TuneLighting();

                root.name = RootName;
                EditorPrefs.SetInt(Key, Version);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Selection.activeObject = null;
                Debug.Log("ROSVIK V23: detached facade cues removed, ground repetition reduced, camera tightened and real animated player installed.");
            } catch (Exception ex) {
                Debug.LogError("ROSVIK V23 FAILED: " + ex);
            }
        }

        static void RemoveDetachedFacadeCues(Transform root) {
            // V20's OBounds facade markers were fine from far away, but concave school
            // footprints expose them as black floating slabs. They are not fences.
            Transform[] doomed = root.GetComponentsInChildren<Transform>(true)
                .Where(t => t != root && (t.name == "window cue" || t.name == "entrance cue"))
                .ToArray();
            foreach (Transform t in doomed) UnityEngine.Object.DestroyImmediate(t.gameObject);
        }

        static void FixGroundMaterials() {
            string[] names = { "mat_ground.mat", "mat_grass.mat", "mat_forest_floor.mat", "mat_pitch.mat" };
            foreach (string name in names) {
                string path = GeneratedRoot + "/" + name;
                Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!m) continue;
                // Geometry UVs are world-space * .12. A .22 material scale expands the
                // visible noise period from ~8 m to ~38 m and kills the checkerboard read.
                Vector2 scale = Vector2.one * .22f;
                if (m.HasProperty("_BaseMap")) m.SetTextureScale("_BaseMap", scale);
                if (m.HasProperty("_MainTex")) m.SetTextureScale("_MainTex", scale);
                EditorUtility.SetDirty(m);
            }
        }

        static void BuildGroundVariation(Transform root) {
            Transform old = Find(root, "08 GROUND VARIATION");
            if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
            Transform group = Group(root, "08 GROUND VARIATION");

            Transform school = Find(root, "ROSVIKS SKOLA");
            Transform player = Find(root, "PLAYER");
            Vector3 center = school ? RendererBounds(school.gameObject).center : (player ? player.position : Vector3.zero);
            center.y = 0f;

            Material mossA = MaterialAsset(ControllerDir + "/mat_ground_macro_a.mat", new Color(.315f,.345f,.245f), .025f, 0f);
            Material mossB = MaterialAsset(ControllerDir + "/mat_ground_macro_b.mat", new Color(.365f,.355f,.255f), .025f, 0f);
            Material worn  = MaterialAsset(ControllerDir + "/mat_ground_macro_worn.mat", new Color(.395f,.355f,.265f), .02f, 0f);

            System.Random rng = new System.Random(2305);
            for (int i = 0; i < 19; i++) {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float r = 9f + (float)rng.NextDouble() * 66f;
                Vector3 p = center + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                Vector2 size = new Vector2(4.5f + (float)rng.NextDouble() * 10f, 3f + (float)rng.NextDouble() * 7f);
                Material m = i % 7 == 0 ? worn : (i % 2 == 0 ? mossA : mossB);
                Blob("subtle ground variation", group, p, size, (float)rng.NextDouble() * 180f, .0145f, m, 13, 23050 + i * 17);
            }
        }

        static void EnsureCharacterAssets() {
            bool changed = false;
            foreach (Download d in CharacterDownloads) {
                string full = FullPath(d.path);
                Directory.CreateDirectory(Path.GetDirectoryName(full));
                long minBytes = d.path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? 500 : 2000;
                if (File.Exists(full) && new FileInfo(full).Length > minBytes) continue;
                try {
                    using (WebClient wc = new WebClient()) {
                        wc.Headers[HttpRequestHeader.UserAgent] = "RosvikBlackout-UnityEditor";
                        byte[] data = wc.DownloadData(d.url);
                        if (data == null || data.Length <= minBytes)
                            throw new InvalidDataException("download was unexpectedly small (" + (data == null ? 0 : data.Length) + " bytes)");
                        File.WriteAllBytes(full, data);
                        changed = true;
                    }
                } catch (Exception ex) {
                    Debug.LogWarning("V23 character download failed for " + d.path + ": " + ex.Message);
                }
            }
            if (changed) AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            if (!File.Exists(FullPath(CharacterModel)))
                throw new FileNotFoundException("Kenney character model was not downloaded.", CharacterModel);

            ConfigureBaseHumanoid(CharacterModel);
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(CharacterModel).OfType<Avatar>().FirstOrDefault(a => a && a.isValid);
            if (avatar) {
                ConfigureHumanoidClip(CharacterIdle, avatar, true);
                ConfigureHumanoidClip(CharacterRun, avatar, true);
            } else {
                Debug.LogWarning("V23: humanoid Avatar was not generated. The real model will still be used with procedural fallback motion.");
            }
        }

        static void ConfigureBaseHumanoid(string path) {
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (!importer) return;
            bool needs = importer.animationType != ModelImporterAnimationType.Human || importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel;
            if (!needs) return;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = false;
            importer.SaveAndReimport();
        }

        static void ConfigureHumanoidClip(string path, Avatar sourceAvatar, bool loop) {
            if (!File.Exists(FullPath(path))) return;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (!importer) return;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = sourceAvatar;
            importer.importAnimation = true;
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            if (clips != null && clips.Length > 0) {
                foreach (ModelImporterClipAnimation c in clips) {
                    c.loopTime = loop;
                    c.loopPose = loop;
                }
                importer.clipAnimations = clips;
            }
            importer.SaveAndReimport();
        }

        static void BuildRealCharacter(Transform player) {
            Transform existing = Find(player, "CHARACTER VISUAL V23");
            if (existing) UnityEngine.Object.DestroyImmediate(existing.gameObject);

            // Hide only the obsolete V20 pawn pieces; keep CharacterController/gameplay intact.
            string[] oldParts = { "body", "head", "leg L", "leg R" };
            foreach (string part in oldParts) {
                Transform t = Find(player, part);
                if (t) t.gameObject.SetActive(false);
            }

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterModel);
            if (!model) throw new InvalidOperationException("V23 imported characterMedium.fbx but Unity did not expose a model prefab.");

            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model, player);
            if (!visual) visual = UnityEngine.Object.Instantiate(model, player);
            visual.name = "CHARACTER VISUAL V23";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            Bounds before = RendererBounds(visual);
            float targetHeight = 1.72f;
            float scale = targetHeight / Mathf.Max(.01f, before.size.y);
            visual.transform.localScale = Vector3.one * scale;

            Bounds after = RendererBounds(visual);
            visual.transform.position += Vector3.up * (player.position.y - after.min.y);

            Material playerMat = BuildPlayerMaterial();
            if (playerMat) {
                foreach (Renderer r in visual.GetComponentsInChildren<Renderer>(true)) {
                    Material[] mats = r.sharedMaterials;
                    if (mats == null || mats.Length == 0) r.sharedMaterial = playerMat;
                    else {
                        for (int i = 0; i < mats.Length; i++) mats[i] = playerMat;
                        r.sharedMaterials = mats;
                    }
                }
            }

            Animator animator = visual.GetComponent<Animator>();
            if (!animator) animator = visual.AddComponent<Animator>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            RuntimeAnimatorController controller = BuildAnimatorController();
            if (controller) animator.runtimeAnimatorController = controller;

            RosvikCharacterDriver driver = player.GetComponent<RosvikCharacterDriver>();
            if (!driver) driver = player.gameObject.AddComponent<RosvikCharacterDriver>();
            driver.enabled = false;
            driver.animator = animator;
            driver.visualRoot = visual.transform;
            RosvikPlayerController ctl = player.GetComponent<RosvikPlayerController>();
            driver.fullSpeed = ctl ? ctl.sprintSpeed : 6f;
            driver.enabled = true;
        }

        static Material BuildPlayerMaterial() {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(CharacterSkin);
            Material m = AssetDatabase.LoadAssetAtPath<Material>(PlayerMaterialPath);
            Shader s = GraphicsSettings.defaultRenderPipeline != null ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
            if (!s || !s.isSupported) s = Shader.Find("Sprites/Default");
            if (!m) {
                m = new Material(s) { name = "Rosvik player - skater" };
                AssetDatabase.CreateAsset(m, PlayerMaterialPath);
            }
            m.shader = s;
            m.color = Color.white;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            if (tex) {
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
                if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            }
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", .10f);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static RuntimeAnimatorController BuildAnimatorController() {
            AnimationClip idle = FirstClip(CharacterIdle);
            AnimationClip run = FirstClip(CharacterRun);
            if (!idle || !run) return null;

            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath))
                AssetDatabase.DeleteAsset(ControllerPath);

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            AnimatorState idleState = sm.AddState("Idle");
            idleState.motion = idle;
            sm.defaultState = idleState;
            AnimatorState runState = sm.AddState("Run");
            runState.motion = run;

            AnimatorStateTransition toRun = idleState.AddTransition(runState);
            toRun.hasExitTime = false;
            toRun.duration = .10f;
            toRun.AddCondition(AnimatorConditionMode.Greater, .065f, "Speed");

            AnimatorStateTransition toIdle = runState.AddTransition(idleState);
            toIdle.hasExitTime = false;
            toIdle.duration = .12f;
            toIdle.AddCondition(AnimatorConditionMode.Less, .045f, "Speed");

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        static AnimationClip FirstClip(string path) {
            if (!File.Exists(FullPath(path))) return null;
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .FirstOrDefault(c => c && !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase));
        }

        static void TuneCamera(Transform player) {
            Camera cam = Camera.main;
            if (!cam) return;
            cam.orthographic = true;
            cam.orthographicSize = 8.8f;
            cam.backgroundColor = new Color(.30f,.34f,.30f);
            IsometricCameraRig rig = cam.GetComponent<IsometricCameraRig>();
            if (!rig) return;
            rig.target = player;
            rig.yaw = 38f;
            rig.pitch = 40f;
            rig.orthographicSize = 8.8f;
            rig.minSize = 6.4f;
            rig.maxSize = 13.5f;
            rig.zoomStep = .65f;
            rig.focusOffset = new Vector3(0f, .92f, 0f);
            rig.followSharpness = 12f;
        }

        static void TuneLighting() {
            Light sun = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
                .FirstOrDefault(l => l && l.type == LightType.Directional);
            if (sun) {
                sun.intensity = 1.03f;
                sun.shadowStrength = .70f;
                sun.color = new Color(.95f,.92f,.84f);
                sun.shadows = LightShadows.Soft;
                sun.transform.rotation = Quaternion.Euler(49f,-37f,0f);
            }
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.29f,.33f,.29f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(.42f,.46f,.43f);
            RenderSettings.fogDensity = .00115f;
        }

        static Material MaterialAsset(string path, Color color, float smooth, float metal) {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader s = GraphicsSettings.defaultRenderPipeline != null ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
            if (!s || !s.isSupported) s = Shader.Find("Sprites/Default");
            if (!m) {
                m = new Material(s) { name = Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = s;
            m.color = color;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
            EditorUtility.SetDirty(m);
            return m;
        }

        static void Blob(string name, Transform parent, Vector3 center, Vector2 size, float yaw, float y, Material mat, int seg, int seed) {
            System.Random rng = new System.Random(seed);
            List<Vector3> pts = new List<Vector3>();
            float rr = yaw * Mathf.Deg2Rad, cr = Mathf.Cos(rr), sr = Mathf.Sin(rr);
            for (int i = 0; i < seg; i++) {
                float a = i * Mathf.PI * 2f / seg;
                float wob = .82f + (float)rng.NextDouble() * .30f;
                float x = Mathf.Cos(a) * size.x * wob;
                float z = Mathf.Sin(a) * size.y * wob;
                pts.Add(center + new Vector3(x * cr - z * sr, 0f, x * sr + z * cr));
            }
            Polygon(name, parent, pts, y, mat);
        }

        static void Polygon(string name, Transform parent, List<Vector3> pts, float y, Material mat) {
            if (pts == null || pts.Count < 3) return;
            Vector3 c = Vector3.zero;
            foreach (Vector3 p in pts) c += p;
            c /= pts.Count;
            Vector3[] v = new Vector3[pts.Count + 1];
            v[0] = new Vector3(c.x, y, c.z);
            for (int i = 0; i < pts.Count; i++) v[i + 1] = new Vector3(pts[i].x, y, pts[i].z);
            int[] tri = new int[pts.Count * 3];
            for (int i = 0; i < pts.Count; i++) {
                tri[i * 3] = 0;
                tri[i * 3 + 1] = i + 1;
                tri[i * 3 + 2] = ((i + 1) % pts.Count) + 1;
            }
            Mesh mesh = new Mesh { name = name + " mesh", vertices = v, triangles = tri };
            mesh.RecalculateNormals();
            if (mesh.normals.Length > 0 && mesh.normals.Average(n => n.y) < 0f) {
                for (int i = 0; i < tri.Length; i += 3) { int q = tri[i + 1]; tri[i + 1] = tri[i + 2]; tri[i + 2] = q; }
                mesh.triangles = tri;
                mesh.RecalculateNormals();
            }
            mesh.RecalculateBounds();
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }

        static Bounds RendererBounds(GameObject go) {
            Renderer[] rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        static string FullPath(string assetPath) {
            return Path.Combine(Directory.GetCurrentDirectory(), assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        static Transform Group(Transform parent, string name) {
            Transform t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        static Transform Find(Transform root, string name) {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }
    }
}
#endif
