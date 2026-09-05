#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UScene = UnityEngine.SceneManagement.Scene;

namespace Rosvik.Blackout.EditorTools {
    /// <summary>
    /// V30 stops being a scenery-only pass. Geography, buildings and V29 dressing stay
    /// locked; this adds the first playable objective: search the real school/sports
    /// cluster for three reserve-power parts and restore emergency lighting at school.
    /// </summary>
    [InitializeOnLoad]
    public static class RosvikBlackoutGameplayV30 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_BLACKOUT_GAMEPLAY_V30_VERSION";
        const int Version = 30;
        const string RootName = "ROSVIK_FIRST_GAMEPLAY_V30";
        const string GroupName = "13 BLACKOUT GAMEPLAY V30";
        const string GeneratedDir = "Assets/Rosvik/GeneratedV30";
        const string KayRoot = "Assets/Rosvik/ThirdParty/V30Gameplay";
        const string ProvenMaterial = "Assets/Rosvik/GeneratedV20/mat_ground.mat";
        const long MainSchoolWay = 163199458;
        const long OldSchoolWay = 163199461;
        const long ArenaWay = 163199454;

        struct Source {
            public string file, url;
            public Source(string f, string u) { file=f; url=u; }
        }

        static readonly Source[] Sources = {
            new Source("box_A.obj", "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-City-Builder-Bits-1.0/main/addons/kaykit_city_builder_bits/Assets/obj/box_A.obj"),
            new Source("box_B.obj", "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-City-Builder-Bits-1.0/main/addons/kaykit_city_builder_bits/Assets/obj/box_B.obj")
        };

        static RosvikBlackoutGameplayV30() {
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        [MenuItem("Rosvik/Rebuild First Gameplay V30")]
        public static void Force() {
            EditorPrefs.DeleteKey(Key);
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        static bool Busy() => EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating;

        static void TryApply() {
            if (EditorPrefs.GetInt(Key,0) >= Version && File.Exists(ScenePath)) { EditorApplication.update -= TryApply; return; }
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
            return GameObject.Find("ROSVIK_SCHOOLYARD_POLISH_V29")
                ?? GameObject.Find("ROSVIK_SCHOOL_LIFE_V28")
                ?? GameObject.Find("ROSVIK_SHADER_SAFE_V27")
                ?? GameObject.Find(RootName);
        }

        static void Apply(UScene scene, GameObject root) {
            try {
                EnsureModels();
                Directory.CreateDirectory(GeneratedDir);
                AssetDatabase.Refresh();

                Transform old = Find(root.transform, GroupName);
                if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
                Transform group = NewGroup(root.transform, GroupName);

                Transform player = Find(root.transform, "PLAYER");
                if (!player) throw new InvalidOperationException("V30 could not find PLAYER.");

                List<RosvikOsmV15.Way> ways = RosvikOsmV15.LoadWays();
                if (ways == null || ways.Count == 0) throw new InvalidOperationException("V30 could not load Rosvik map data.");

                Transform mainCourt = Find(root.transform, "school entrance court");
                Transform oldCourt = Find(root.transform, "Träskolan entrance court");
                Transform arenaCourt = Find(root.transform, "arena forecourt");
                if (!mainCourt) throw new InvalidOperationException("V30 needs the V28/V29 school entrance court.");

                Material stationMat = Mat("reserve_station", new Color(.11f,.125f,.115f), .30f);
                Material fuseMat = Mat("pickup_fuse", new Color(.73f,.48f,.10f), .22f);
                Material batteryMat = Mat("pickup_battery", new Color(.42f,.12f,.09f), .28f);
                Material toolMat = Mat("pickup_tools", new Color(.12f,.28f,.34f), .24f);
                Material markerMat = Mat("pickup_marker", new Color(.64f,.54f,.18f), .45f);

                GameObject boxA = AssetDatabase.LoadAssetAtPath<GameObject>(KayRoot + "/box_A.obj");
                GameObject boxB = AssetDatabase.LoadAssetAtPath<GameObject>(KayRoot + "/box_B.obj");

                Transform station = BuildStation(group, root.transform, mainCourt, boxA, stationMat);

                Vector3 oldPos = CourtPoint(oldCourt, ways, OldSchoolWay, new Vector3(1.7f,0f,-1.0f));
                Vector3 arenaPos = CourtPoint(arenaCourt, ways, ArenaWay, new Vector3(-2.0f,0f,1.1f));
                Vector3 pitchPos = FindPitchPickupPosition(root.transform, ways);

                Transform p0 = BuildPickup(group, boxB ? boxB : boxA, "pickup fuse", oldPos, .62f, fuseMat, markerMat, new Color(.95f,.62f,.16f));
                Transform p1 = BuildPickup(group, boxA, "pickup battery", arenaPos, .72f, batteryMat, markerMat, new Color(.88f,.25f,.12f));
                Transform p2 = BuildPickup(group, boxB ? boxB : boxA, "pickup tools", pitchPos, .66f, toolMat, markerMat, new Color(.20f,.58f,.70f));

                Light[] lights = BuildEmergencyLights(group, root.transform, station);

                Rosvik.Blackout.RosvikBlackoutLoop loop = group.gameObject.AddComponent<Rosvik.Blackout.RosvikBlackoutLoop>();
                loop.player = player;
                loop.restoreStation = station;
                loop.pickups = new [] { p0, p1, p2 };
                loop.pickupNames = new [] { "Säkring (Träskolan)", "Startbatteri (ishallen)", "Verktygslåda (Rosvalla)" };
                loop.emergencyLights = lights;
                loop.pickupDistance = 2.4f;
                loop.stationDistance = 3.0f;

                // Keep place labels useful for the scavenging objective without turning them
                // into objective arrows. The player still explores the real Rosvik layout.
                Rosvik.Blackout.RosvikMapHUD hud = root.GetComponent<Rosvik.Blackout.RosvikMapHUD>();
                if (hud) hud.maxLabelDistance = 120f;

                root.name = RootName;
                EditorPrefs.SetInt(Key, Version);
                Selection.activeObject = null;
                EditorUtility.SetDirty(root);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
                Debug.Log("ROSVIK V30: first gameplay loop added — collect 3 reserve-power parts around Träskolan/arena/Rosvalla, then restore school emergency lights with E. Geography unchanged.");
            } catch (Exception ex) {
                Debug.LogError("ROSVIK V30 FAILED: " + ex);
            }
        }

        static Transform BuildStation(Transform parent, Transform root, Transform court, GameObject box, Material mat) {
            Renderer cr = court.GetComponent<Renderer>();
            Vector3 center = cr ? cr.bounds.center : court.position;
            Transform door = FindNearest(root, "main entrance", center);
            Vector3 outward = door ? Flat(center - door.position).normalized : Flat(court.forward).normalized;
            if (outward.sqrMagnitude < .1f) outward = Vector3.forward;
            Vector3 right = new Vector3(outward.z,0f,-outward.x);
            Vector3 pos = (door ? door.position : center) + outward*1.15f + right*2.4f;
            pos.y = .05f;

            GameObject go = PlaceModel(box, parent, "reserve power cabinet", pos, Mathf.Atan2(outward.x,outward.z)*Mathf.Rad2Deg, 1.35f, mat);
            if (!go) {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name="reserve power cabinet"; go.transform.SetParent(parent,false);
                go.transform.position=pos+Vector3.up*.65f; go.transform.localScale=new Vector3(.75f,1.30f,.50f);
                go.GetComponent<Renderer>().sharedMaterial=mat; UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            }
            return go.transform;
        }

        static Transform BuildPickup(Transform parent, GameObject asset, string name, Vector3 pos, float height,
            Material mat, Material marker, Color glow) {
            pos.y=.05f;
            GameObject go=PlaceModel(asset,parent,name,pos,25f,height,mat);
            if(!go) {
                go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);
                go.transform.position=pos+Vector3.up*(height*.45f);go.transform.localScale=new Vector3(.55f,height*.8f,.45f);
                go.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            }

            GameObject ring=GameObject.CreatePrimitive(PrimitiveType.Cylinder);ring.name="pickup marker";ring.transform.SetParent(go.transform,false);
            ring.transform.position=new Vector3(pos.x,.055f,pos.z);ring.transform.localScale=new Vector3(.72f,.012f,.72f);
            ring.GetComponent<Renderer>().sharedMaterial=marker;UnityEngine.Object.DestroyImmediate(ring.GetComponent<Collider>());
            ring.transform.SetParent(parent,true); ring.transform.SetParent(go.transform,true);

            GameObject lightGo=new GameObject("pickup glint");lightGo.transform.SetParent(go.transform,false);lightGo.transform.position=go.transform.position+Vector3.up*.75f;
            Light l=lightGo.AddComponent<Light>();l.type=LightType.Point;l.color=glow;l.range=2.8f;l.intensity=.75f;l.shadows=LightShadows.None;
            return go.transform;
        }

        static Light[] BuildEmergencyLights(Transform parent, Transform root, Transform station) {
            List<Light> lights=new List<Light>();
            Transform[] lampModels=root.GetComponentsInChildren<Transform>(true)
                .Where(t=>t.name.IndexOf("lamp",StringComparison.OrdinalIgnoreCase)>=0 && t.GetComponent<Light>()==null)
                .OrderBy(t=>Flat(t.position-station.position).sqrMagnitude).Take(5).ToArray();

            foreach(Transform t in lampModels) {
                GameObject g=new GameObject("emergency warm light");g.transform.SetParent(parent,false);g.transform.position=t.position+Vector3.up*3.15f;
                Light l=g.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(1f,.70f,.38f);l.range=9.5f;l.intensity=2.35f;l.shadows=LightShadows.Soft;l.enabled=false;lights.Add(l);
            }
            if(lights.Count==0) {
                GameObject g=new GameObject("emergency warm light");g.transform.SetParent(parent,false);g.transform.position=station.position+Vector3.up*2.2f;
                Light l=g.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(1f,.70f,.38f);l.range=9f;l.intensity=2.5f;l.shadows=LightShadows.Soft;l.enabled=false;lights.Add(l);
            }
            return lights.ToArray();
        }

        static Vector3 CourtPoint(Transform court, List<RosvikOsmV15.Way> ways, long wayId, Vector3 localOffset) {
            if(court) {
                Renderer r=court.GetComponent<Renderer>();Vector3 c=r?r.bounds.center:court.position;
                Vector3 right=Flat(court.right).normalized, forward=Flat(court.forward).normalized;
                Vector3 p=c+right*localOffset.x+forward*localOffset.z;p.y=.05f;return p;
            }
            RosvikOsmV15.Way w=ways.FirstOrDefault(x=>x.Id==wayId);
            Vector3 fallback=w!=null?RosvikOsmV15.Centroid(w):Vector3.zero;fallback+=localOffset;fallback.y=.05f;return fallback;
        }

        static Vector3 FindPitchPickupPosition(Transform root, List<RosvikOsmV15.Way> ways) {
            Transform pitch=root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name.StartsWith("Rosvalla pitch",StringComparison.OrdinalIgnoreCase));
            if(pitch) { Renderer r=pitch.GetComponent<Renderer>();Vector3 p=r?r.bounds.center:pitch.position;p+=Flat(pitch.right).normalized*2.2f;p.y=.05f;return p; }
            var main=ways.FirstOrDefault(w=>w.Id==MainSchoolWay);Vector3 school=main!=null?RosvikOsmV15.Centroid(main):Vector3.zero;
            var wp=ways.Where(w=>w.Closed&&(w.Tag("leisure")=="pitch"||w.Tag("sport")=="soccer"))
                .OrderBy(w=>Flat(RosvikOsmV15.Centroid(w)-school).sqrMagnitude).FirstOrDefault();
            Vector3 q=wp!=null?RosvikOsmV15.Centroid(wp):school+new Vector3(24f,0f,20f);q+=new Vector3(2.2f,.05f,0f);return q;
        }

        static void EnsureModels() {
            string dir=Path.Combine(Directory.GetCurrentDirectory(),KayRoot.Replace('/',Path.DirectorySeparatorChar));Directory.CreateDirectory(dir);
            int downloaded=0;
            foreach(Source s in Sources) {
                string assetPath=KayRoot+"/"+s.file;string full=Path.Combine(Directory.GetCurrentDirectory(),assetPath.Replace('/',Path.DirectorySeparatorChar));
                if(!File.Exists(full)||new FileInfo(full).Length<100) {
                    try { using(WebClient wc=new WebClient()){wc.Headers[HttpRequestHeader.UserAgent]="RosvikBlackout-UnityEditor";File.WriteAllText(full,StripMaterials(wc.DownloadString(s.url)));downloaded++;} }
                    catch(Exception ex){Debug.LogWarning("V30 asset download failed: "+s.file+" — "+ex.Message);}
                }
            }
            if(downloaded>0)AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(Source s in Sources){string p=KayRoot+"/"+s.file;if(File.Exists(Path.Combine(Directory.GetCurrentDirectory(),p.Replace('/',Path.DirectorySeparatorChar))))AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);}
        }

        static string StripMaterials(string text) {
            return string.Join("\n",text.Replace("\r\n","\n").Replace('\r','\n').Split('\n')
                .Where(l=>!l.StartsWith("mtllib ",StringComparison.OrdinalIgnoreCase)&&!l.StartsWith("usemtl ",StringComparison.OrdinalIgnoreCase)));
        }

        static Material Mat(string name, Color color, float smooth) {
            string path=GeneratedDir+"/mat_"+name+".mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(path);Shader s=ResolveShader();
            if(!m){m=new Material(s){name="V30 "+name};AssetDatabase.CreateAsset(m,path);}m.shader=s;m.color=color;
            if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color);if(m.HasProperty("_Color"))m.SetColor("_Color",color);
            if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",0f);EditorUtility.SetDirty(m);return m;
        }

        static Shader ResolveShader() {
            Material proven=AssetDatabase.LoadAssetAtPath<Material>(ProvenMaterial);if(proven&&proven.shader&&proven.shader.isSupported)return proven.shader;
            Shader s=GraphicsSettings.defaultRenderPipeline!=null?Shader.Find("Universal Render Pipeline/Lit"):Shader.Find("Standard");if(!s||!s.isSupported)s=Shader.Find("Sprites/Default");return s;
        }

        static GameObject PlaceModel(GameObject asset,Transform parent,string name,Vector3 pos,float yaw,float targetHeight,Material mat) {
            if(!asset)return null;GameObject go=(GameObject)PrefabUtility.InstantiatePrefab(asset,parent);if(!go)go=UnityEngine.Object.Instantiate(asset,parent);
            go.name=name;go.transform.position=pos;go.transform.rotation=Quaternion.Euler(0f,yaw,0f);go.transform.localScale=Vector3.one;
            Bounds b=BoundsOf(go);float s=targetHeight/Mathf.Max(.01f,b.size.y);go.transform.localScale=Vector3.one*s;
            foreach(Renderer r in go.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=mat;foreach(Collider c in go.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
            Ground(go,.05f);return go;
        }

        static void Ground(GameObject go,float y){Bounds b=BoundsOf(go);Vector3 p=go.transform.position;p.y+=y-b.min.y;go.transform.position=p;}
        static Bounds BoundsOf(GameObject go){Renderer[] rs=go.GetComponentsInChildren<Renderer>(true);if(rs.Length==0)return new Bounds(go.transform.position,Vector3.one);Bounds b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;}
        static Transform FindNearest(Transform root,string name,Vector3 point){Transform best=null;float bd=float.MaxValue;foreach(Transform t in root.GetComponentsInChildren<Transform>(true)){if(t.name!=name)continue;float d=Flat(t.position-point).sqrMagnitude;if(d<bd){bd=d;best=t;}}return best;}
        static Transform NewGroup(Transform parent,string name){Transform t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        static Transform Find(Transform root,string name){if(!root)return null;if(root.name==name)return root;foreach(Transform t in root.GetComponentsInChildren<Transform>(true))if(t.name==name)return t;return null;}
        static Vector3 Flat(Vector3 v){v.y=0f;return v;}
    }
}
#endif
