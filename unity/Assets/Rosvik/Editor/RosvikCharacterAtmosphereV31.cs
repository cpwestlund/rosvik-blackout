#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UScene = UnityEngine.SceneManagement.Scene;

namespace Rosvik.Blackout.EditorTools {
    /// <summary>
    /// V31 is a quality pass, not another map rebuild. V30 gameplay/geography remain locked.
    /// It improves the player locomotion controller, adds subtle interaction posing, makes
    /// the spring/autumn schoolyard wetter and denser, and gives existing vegetation motion.
    /// </summary>
    [InitializeOnLoad]
    public static class RosvikCharacterAtmosphereV31 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_CHARACTER_ATMOSPHERE_V31_VERSION";
        const int Version = 31;
        const string RootName = "ROSVIK_CHARACTER_ATMOSPHERE_V31";
        const string GroupName = "14 CHARACTER + ATMOSPHERE V31";
        const string GeneratedDir = "Assets/Rosvik/GeneratedV31";
        const string ProvenMaterial = "Assets/Rosvik/GeneratedV20/mat_ground.mat";
        const string CharacterIdle = "Assets/Rosvik/ThirdParty/V23Character/idle.fbx";
        const string CharacterRun = "Assets/Rosvik/ThirdParty/V23Character/run.fbx";
        const string ControllerPath = GeneratedDir + "/RosvikPlayerV31.controller";
        const string KenneyRoot = "Assets/Rosvik/ThirdParty/V22Models/KenneyNature";
        const long MainSchoolWay = 163199458;

        static RosvikCharacterAtmosphereV31() {
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        [MenuItem("Rosvik/Rebuild Character + Atmosphere V31")]
        public static void Force() {
            EditorPrefs.DeleteKey(Key);
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        static bool Busy() => EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating;

        static void TryApply() {
            if (EditorPrefs.GetInt(Key, 0) >= Version && File.Exists(ScenePath)) {
                EditorApplication.update -= TryApply;
                return;
            }
            if (Busy() || !File.Exists(ScenePath)) return;

            UScene scene = EditorSceneManager.GetActiveScene();
            GameObject root = FindRoot();
            if (!root) {
                if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                if (Busy()) return;
                root = FindRoot();
            }
            if (!root || Busy()) return;

            EditorApplication.update -= TryApply;
            Apply(scene, root);
        }

        static GameObject FindRoot() {
            return GameObject.Find("ROSVIK_FIRST_GAMEPLAY_V30")
                ?? GameObject.Find("ROSVIK_SCHOOLYARD_POLISH_V29")
                ?? GameObject.Find("ROSVIK_SCHOOL_LIFE_V28")
                ?? GameObject.Find(RootName);
        }

        static void Apply(UScene scene, GameObject root) {
            try {
                Directory.CreateDirectory(GeneratedDir);
                AssetDatabase.Refresh();

                Transform old = Find(root.transform, GroupName);
                if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
                Transform group = NewGroup(root.transform, GroupName);

                Transform player = Find(root.transform, "PLAYER");
                if (!player) throw new InvalidOperationException("V31 could not find PLAYER.");

                UpgradeCharacter(player);
                TuneExistingSurfaceMaterials();

                List<RosvikOsmV15.Way> ways = RosvikOsmV15.LoadWays();
                Transform court = Find(root.transform, "school entrance court");
                if (court) BuildEntranceMicrodetail(group, court);
                if (ways != null && ways.Count > 0) AddDenserNorthernVegetation(group, root.transform, player, court, ways);

                Rosvik.Blackout.RosvikAmbientMotion wind = group.gameObject.AddComponent<Rosvik.Blackout.RosvikAmbientMotion>();
                wind.sourceRoot = root.transform;
                wind.windStrength = .92f;
                wind.windSpeed = .54f;
                wind.maxAnimated = 110;

                TuneCamera(player);
                TuneLighting();

                root.name = RootName;
                EditorPrefs.SetInt(Key, Version);
                Selection.activeObject = null;
                EditorUtility.SetDirty(root);
                AssetDatabase.SaveAssets();
                if (!Busy()) {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene, ScenePath);
                }
                Debug.Log("ROSVIK V31: character locomotion upgraded and school area received wet-surface, leaf-litter, curb, vegetation and wind atmosphere polish. Gameplay/geography unchanged.");
            } catch (Exception ex) {
                Debug.LogError("ROSVIK V31 FAILED: " + ex);
            }
        }

        static void UpgradeCharacter(Transform player) {
            Animator animator = player.GetComponentInChildren<Animator>(true);
            if (animator) {
                RuntimeAnimatorController controller = BuildLocomotionController();
                if (controller) animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.speed = 1f;
            }

            Rosvik.Blackout.RosvikCharacterDriver driver = player.GetComponent<Rosvik.Blackout.RosvikCharacterDriver>();
            if (!driver) driver = player.gameObject.AddComponent<Rosvik.Blackout.RosvikCharacterDriver>();
            driver.animator = animator;
            if (animator) driver.visualRoot = animator.transform;
            Rosvik.Blackout.RosvikPlayerController ctl = player.GetComponent<Rosvik.Blackout.RosvikPlayerController>();
            driver.fullSpeed = ctl ? ctl.sprintSpeed : 5.6f;
            driver.animationDamp = .09f;
            driver.forwardLean = 3.0f;
            driver.turnLean = 3.8f;
            driver.idleBreath = .005f;
            driver.movingBob = .006f;
            driver.enabled = true;
        }

        static RuntimeAnimatorController BuildLocomotionController() {
            AnimationClip idle = FirstClip(CharacterIdle);
            AnimationClip run = FirstClip(CharacterRun);
            if (!idle || !run) return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Rosvik/GeneratedV23/RosvikPlayer.controller");

            AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (existing) AssetDatabase.DeleteAsset(ControllerPath);

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            AnimatorStateMachine sm = controller.layers[0].stateMachine;

            AnimatorState idleState = sm.AddState("Idle");
            idleState.motion = idle;
            idleState.speed = .94f;
            sm.defaultState = idleState;

            // Kenney ships idle/run only. Reusing the retargeted run at a deliberately lower
            // state speed produces a readable walk instead of globally slowing the Animator.
            AnimatorState walkState = sm.AddState("Walk");
            walkState.motion = run;
            walkState.speed = .60f;

            AnimatorState runState = sm.AddState("Run");
            runState.motion = run;
            runState.speed = 1.04f;

            Transition(idleState, walkState, AnimatorConditionMode.Greater, .055f, .16f);
            Transition(walkState, idleState, AnimatorConditionMode.Less, .038f, .18f);
            Transition(walkState, runState, AnimatorConditionMode.Greater, .68f, .13f);
            Transition(runState, walkState, AnimatorConditionMode.Less, .59f, .16f);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        static void Transition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, float threshold, float duration) {
            AnimatorStateTransition tr = from.AddTransition(to);
            tr.hasExitTime = false;
            tr.hasFixedDuration = true;
            tr.duration = duration;
            tr.canTransitionToSelf = false;
            tr.AddCondition(mode, threshold, "Speed");
        }

        static AnimationClip FirstClip(string path) {
            if (!File.Exists(FullPath(path))) return null;
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .FirstOrDefault(c => c && !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase));
        }

        static void TuneExistingSurfaceMaterials() {
            SetSmoothness("Assets/Rosvik/GeneratedV28/mat_entry_asphalt.mat", .54f);
            SetSmoothness("Assets/Rosvik/GeneratedV28/mat_wet_puddle.mat", .94f);
            SetSmoothness("Assets/Rosvik/GeneratedV28/mat_packed_path.mat", .18f);
            SetSmoothness("Assets/Rosvik/GeneratedV20/mat_road.mat", .36f);
            SetSmoothness("Assets/Rosvik/GeneratedV20/mat_path.mat", .17f);
        }

        static void SetSmoothness(string path, float value) {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) return;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", value);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", value);
            EditorUtility.SetDirty(m);
        }

        static void BuildEntranceMicrodetail(Transform parent, Transform court) {
            Renderer rr = court.GetComponent<Renderer>();
            if (!rr) return;

            Vector3 center = rr.bounds.center;
            center.y = rr.bounds.max.y;
            Vector3 right = Flat(court.right).normalized;
            Vector3 forward = Flat(court.forward).normalized;
            if (right.sqrMagnitude < .1f) right = Vector3.right;
            if (forward.sqrMagnitude < .1f) forward = Vector3.forward;

            float width = Mathf.Max(5.5f, Mathf.Abs(court.lossyScale.x));
            float depth = Mathf.Max(4.0f, Mathf.Abs(court.lossyScale.z));
            float surfaceY = rr.bounds.max.y + .012f;

            Material curb = Mat("weathered_curb", new Color(.31f,.315f,.285f), .20f);
            Material drain = Mat("drain", new Color(.055f,.06f,.058f), .42f);
            Material wet = Mat("fresh_puddle", new Color(.055f,.095f,.105f), .93f);
            Material leafA = Mat("leaf_brown", new Color(.30f,.16f,.065f), .04f);
            Material leafB = Mat("leaf_gold", new Color(.47f,.36f,.095f), .04f);
            Material leafC = Mat("leaf_dark", new Color(.20f,.235f,.09f), .03f);

            Box("curb left", parent, center - right*(width*.5f+.08f) + Vector3.up*.055f,
                new Vector3(.15f,.11f,depth+.25f), Quaternion.LookRotation(forward,Vector3.up), curb);
            Box("curb right", parent, center + right*(width*.5f+.08f) + Vector3.up*.055f,
                new Vector3(.15f,.11f,depth+.25f), Quaternion.LookRotation(forward,Vector3.up), curb);

            Vector3 d0 = center + right*(width*.34f) + forward*(depth*.32f); d0.y = surfaceY;
            Vector3 d1 = center - right*(width*.37f) - forward*(depth*.20f); d1.y = surfaceY;
            Box("storm drain", parent, d0, new Vector3(.52f,.018f,.22f), Quaternion.LookRotation(right,Vector3.up), drain);
            Box("storm drain", parent, d1, new Vector3(.46f,.018f,.20f), Quaternion.LookRotation(right,Vector3.up), drain);

            Disc("wet asphalt", parent, center + right*1.35f + forward*.75f, surfaceY+.003f, 1.55f,.55f,18f,wet);
            Disc("wet asphalt", parent, center - right*2.05f - forward*.55f, surfaceY+.003f, 1.05f,.38f,-23f,wet);
            Disc("wet asphalt", parent, center + right*2.75f - forward*1.35f, surfaceY+.003f, .72f,.28f,41f,wet);

            System.Random rng = new System.Random(3105);
            Material[] leaves = { leafA, leafB, leafC };
            for (int i=0;i<34;i++) {
                float side = i%2==0 ? -1f : 1f;
                float along = Mathf.Lerp(-depth*.44f, depth*.44f, (float)rng.NextDouble());
                float inset = Mathf.Lerp(.22f,.78f,(float)rng.NextDouble());
                Vector3 p = center + right*side*(width*.5f-inset) + forward*along;
                p.y = surfaceY + .008f;
                float sx = Mathf.Lerp(.075f,.16f,(float)rng.NextDouble());
                float sz = Mathf.Lerp(.025f,.065f,(float)rng.NextDouble());
                Box("fallen leaf", parent, p, new Vector3(sx,.008f,sz), Quaternion.Euler(0f,(float)rng.NextDouble()*180f,0f), leaves[i%leaves.Length]);
            }
        }

        static void AddDenserNorthernVegetation(Transform parent, Transform root, Transform player, Transform court, List<RosvikOsmV15.Way> ways) {
            RosvikOsmV15.Way main = ways.FirstOrDefault(w => w.Id == MainSchoolWay);
            if (main == null) return;
            Vector3 school = RosvikOsmV15.Centroid(main);

            GameObject pine = AssetDatabase.LoadAssetAtPath<GameObject>(KenneyRoot + "/tree_pineDefaultA.obj");
            GameObject tallPine = AssetDatabase.LoadAssetAtPath<GameObject>(KenneyRoot + "/tree_pineTallA.obj");
            GameObject fallTree = AssetDatabase.LoadAssetAtPath<GameObject>(KenneyRoot + "/tree_default_fall.obj");
            GameObject bush = AssetDatabase.LoadAssetAtPath<GameObject>(KenneyRoot + "/plant_bushDetailed.obj");
            GameObject rock = AssetDatabase.LoadAssetAtPath<GameObject>(KenneyRoot + "/rock_smallA.obj");
            if (!pine && !fallTree && !bush) return;

            Material spruceA = Mat("spruce_deep", new Color(.055f,.135f,.075f), .035f);
            Material spruceB = Mat("spruce_soft", new Color(.095f,.205f,.115f), .035f);
            Material autumn = Mat("autumn_muted", new Color(.39f,.315f,.10f), .03f);
            Material shrub = Mat("shrub", new Color(.16f,.245f,.105f), .03f);
            Material stone = Mat("stone", new Color(.30f,.31f,.285f), .12f);

            Vector3 courtCenter = court ? BoundsOf(court.gameObject).center : school;
            System.Random rng = new System.Random(3117);
            int trees=0, bushes=0, rocks=0;

            for (int attempt=0; attempt<420 && trees<15; attempt++) {
                float a=(float)rng.NextDouble()*Mathf.PI*2f;
                float r=15f+(float)rng.NextDouble()*34f;
                Vector3 p=school+new Vector3(Mathf.Cos(a)*r,.04f,Mathf.Sin(a)*r);
                if (!SafeForNature(p,ways,3.4f) || Flat(p-courtCenter).magnitude<9.5f || Flat(p-player.position).magnitude<4.0f) continue;
                GameObject src;
                Material mat;
                if (trees%5==0 && fallTree) { src=fallTree; mat=autumn; }
                else if (trees%3==0 && tallPine) { src=tallPine; mat=spruceA; }
                else { src=pine ? pine : fallTree; mat=trees%2==0?spruceA:spruceB; }
                if (!src) continue;
                PlaceModel(src,parent,"v31 tree",p,(float)rng.NextDouble()*360f,Mathf.Lerp(4.7f,7.2f,(float)rng.NextDouble()),mat);
                trees++;
            }

            if (bush) {
                for (int attempt=0; attempt<360 && bushes<28; attempt++) {
                    float a=(float)rng.NextDouble()*Mathf.PI*2f;
                    float r=10f+(float)rng.NextDouble()*37f;
                    Vector3 p=school+new Vector3(Mathf.Cos(a)*r,.04f,Mathf.Sin(a)*r);
                    if (!SafeForNature(p,ways,1.8f) || Flat(p-courtCenter).magnitude<7.5f) continue;
                    PlaceModel(bush,parent,"v31 shrub",p,(float)rng.NextDouble()*360f,Mathf.Lerp(.45f,1.0f,(float)rng.NextDouble()),shrub);
                    bushes++;
                }
            }

            if (rock) {
                for (int attempt=0; attempt<180 && rocks<11; attempt++) {
                    float a=(float)rng.NextDouble()*Mathf.PI*2f;
                    float r=13f+(float)rng.NextDouble()*38f;
                    Vector3 p=school+new Vector3(Mathf.Cos(a)*r,.035f,Mathf.Sin(a)*r);
                    if (!SafeForNature(p,ways,1.5f) || Flat(p-courtCenter).magnitude<6.5f) continue;
                    PlaceModel(rock,parent,"v31 stone",p,(float)rng.NextDouble()*360f,Mathf.Lerp(.18f,.42f,(float)rng.NextDouble()),stone);
                    rocks++;
                }
            }
        }

        static bool SafeForNature(Vector3 p, List<RosvikOsmV15.Way> ways, float clearance) {
            if (InsideBuilding(p,ways) || InsidePitch(p,ways)) return false;
            return DistanceToRoad(p,ways) > clearance;
        }

        static bool InsideBuilding(Vector3 p,List<RosvikOsmV15.Way> ways) {
            foreach (var w in ways) {
                if (!w.Closed || string.IsNullOrEmpty(w.Tag("building")) || w.Tag("building")=="no") continue;
                if (Inside(p,Points(w))) return true;
            }
            return false;
        }

        static bool InsidePitch(Vector3 p,List<RosvikOsmV15.Way> ways) {
            foreach (var w in ways) {
                if (!w.Closed) continue;
                if (w.Tag("leisure")!="pitch" && w.Tag("sport")!="soccer") continue;
                if (Inside(p,Points(w))) return true;
            }
            return false;
        }

        static float DistanceToRoad(Vector3 p,List<RosvikOsmV15.Way> ways) {
            float best=float.MaxValue;
            foreach (var w in ways) {
                string h=w.Tag("highway");
                if (string.IsNullOrEmpty(h) || w.Nodes.Count<2) continue;
                for (int i=0;i<w.Nodes.Count-1;i++) {
                    Vector3 q=ClosestPoint(p,w.Nodes[i].Pos,w.Nodes[i+1].Pos);
                    best=Mathf.Min(best,Vector3.Distance(Flat(p),Flat(q)));
                }
            }
            return best;
        }

        static void TuneCamera(Transform player) {
            Camera cam=Camera.main;
            if(!cam)return;
            cam.orthographic=true;
            cam.orthographicSize=8.15f;
            cam.backgroundColor=new Color(.255f,.285f,.27f);
            Rosvik.Blackout.IsometricCameraRig rig=cam.GetComponent<Rosvik.Blackout.IsometricCameraRig>();
            if(rig){rig.target=player;rig.yaw=38f;rig.pitch=51f;rig.orthographicSize=8.15f;rig.minSize=5.9f;rig.maxSize=13.5f;rig.zoomStep=.60f;rig.focusOffset=new Vector3(0f,.88f,0f);rig.followSharpness=12.5f;}
        }

        static void TuneLighting() {
            foreach(Light sun in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.type==LightType.Directional)) {
                sun.intensity=.91f;
                sun.shadowStrength=.74f;
                sun.color=new Color(.95f,.885f,.78f);
                sun.shadows=LightShadows.Soft;
                sun.transform.rotation=Quaternion.Euler(50f,-37f,0f);
            }
            RenderSettings.ambientMode=AmbientMode.Flat;
            RenderSettings.ambientLight=new Color(.285f,.315f,.285f);
            RenderSettings.fog=true;
            RenderSettings.fogColor=new Color(.36f,.395f,.375f);
            RenderSettings.fogDensity=.00138f;
            RenderSettings.reflectionIntensity=.58f;
        }

        static Material Mat(string name,Color color,float smooth) {
            string path=GeneratedDir+"/mat_"+name+".mat";
            Material m=AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader s=ResolveShader();
            if(!m){m=new Material(s){name="V31 "+name};AssetDatabase.CreateAsset(m,path);}
            m.shader=s;m.color=color;
            if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color);
            if(m.HasProperty("_Color"))m.SetColor("_Color",color);
            if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);
            if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",smooth);
            if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",0f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Shader ResolveShader() {
            Material proven=AssetDatabase.LoadAssetAtPath<Material>(ProvenMaterial);
            if(proven&&proven.shader&&proven.shader.isSupported)return proven.shader;
            Shader s=GraphicsSettings.defaultRenderPipeline!=null?Shader.Find("Universal Render Pipeline/Lit"):Shader.Find("Standard");
            if(!s||!s.isSupported)s=Shader.Find("Sprites/Default");
            return s;
        }

        static void Box(string name,Transform parent,Vector3 pos,Vector3 scale,Quaternion rot,Material mat) {
            GameObject go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);
            go.transform.SetPositionAndRotation(pos,rot);go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=mat;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        static void Disc(string name,Transform parent,Vector3 pos,float y,float width,float depth,float yaw,Material mat) {
            GameObject go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.transform.SetParent(parent,false);
            pos.y=y;go.transform.position=pos;go.transform.rotation=Quaternion.Euler(0f,yaw,0f);go.transform.localScale=new Vector3(width,.006f,depth);
            go.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        static GameObject PlaceModel(GameObject asset,Transform parent,string name,Vector3 pos,float yaw,float targetHeight,Material material) {
            if(!asset)return null;
            GameObject go=(GameObject)PrefabUtility.InstantiatePrefab(asset,parent);
            if(!go)go=UnityEngine.Object.Instantiate(asset,parent);
            go.name=name;go.transform.position=pos;go.transform.rotation=Quaternion.Euler(0f,yaw,0f);go.transform.localScale=Vector3.one;
            Bounds b=BoundsOf(go);float h=Mathf.Max(.01f,b.size.y);float s=targetHeight/h;go.transform.localScale=Vector3.one*s;
            foreach(Renderer r in go.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=material;
            foreach(Collider c in go.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
            Bounds after=BoundsOf(go);Vector3 p=go.transform.position;p.y += .035f-after.min.y;go.transform.position=p;
            return go;
        }

        static Bounds BoundsOf(GameObject go) {
            Renderer[] rs=go.GetComponentsInChildren<Renderer>(true);
            if(rs.Length==0)return new Bounds(go.transform.position,Vector3.zero);
            Bounds b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;
        }

        static List<Vector3> Points(RosvikOsmV15.Way w){List<Vector3> p=w.Nodes.Select(n=>n.Pos).ToList();if(p.Count>2&&w.Closed)p.RemoveAt(p.Count-1);return p;}
        static bool Inside(Vector3 p,List<Vector3> poly){if(poly==null||poly.Count<3)return false;bool inside=false;int j=poly.Count-1;for(int i=0;i<poly.Count;i++){float xi=poly[i].x,zi=poly[i].z,xj=poly[j].x,zj=poly[j].z;bool hit=((zi>p.z)!=(zj>p.z))&&(p.x<(xj-xi)*(p.z-zi)/(zj-zi+.00001f)+xi);if(hit)inside=!inside;j=i;}return inside;}
        static Vector3 ClosestPoint(Vector3 p,Vector3 a,Vector3 b){Vector3 ab=Flat(b-a);float den=ab.sqrMagnitude;if(den<.0001f)return a;float t=Mathf.Clamp01(Vector3.Dot(Flat(p-a),ab)/den);return a+ab*t;}
        static Vector3 Flat(Vector3 v){v.y=0f;return v;}
        static Transform NewGroup(Transform p,string n){Transform t=new GameObject(n).transform;t.SetParent(p,false);return t;}
        static Transform Find(Transform root,string n){if(!root)return null;if(root.name==n)return root;foreach(Transform t in root.GetComponentsInChildren<Transform>(true))if(t.name==n)return t;return null;}
        static string FullPath(string assetPath){return Path.Combine(Directory.GetCurrentDirectory(),assetPath.Replace('/',Path.DirectorySeparatorChar));}
    }
}
#endif
