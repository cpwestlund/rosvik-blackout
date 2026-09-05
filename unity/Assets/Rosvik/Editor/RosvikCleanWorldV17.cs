#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikCleanWorldV17 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_CLEAN_WORLD_VERSION";
        const int Version = 17;
        const long MainSchoolWay = 163199458;
        const long OldSchoolWay = 163199461;
        const long ArenaWay = 163199454;
        static readonly BindingFlags HiddenStatic = BindingFlags.NonPublic | BindingFlags.Static;

        static RosvikCleanWorldV17() {
            // The old V10-V16 chain was useful for exploration but is now retired.
            // Set all previous generation keys before their delayed callbacks run.
            EditorPrefs.SetInt("ROSVIK_UNITY_BOOTSTRAP_VERSION", 999);
            EditorPrefs.SetInt("ROSVIK_SITE_CALIBRATION_VERSION", 999);
            EditorPrefs.SetInt("ROSVIK_DETAIL_PASS_VERSION", 999);
            EditorPrefs.SetInt("ROSVIK_POLISH_PASS_VERSION", 999);
            EditorPrefs.SetInt("ROSVIK_HERO_POLISH_VERSION", 999);
            EditorPrefs.SetInt("ROSVIK_MAP_LOCK_VERSION", 999);
            EditorPrefs.SetInt("ROSVIK_WORLD_PASS_VERSION", 999);
            EditorApplication.delayCall += Build;
        }

        [MenuItem("Rosvik/Rebuild Clean World V17")]
        public static void Build() {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorPrefs.GetInt(Key, 0) >= Version && File.Exists(ScenePath)) return;

            try {
                List<RosvikOsmV15.Way> ways = RosvikOsmV15.LoadWays();
                if (ways == null || ways.Count == 0) {
                    Debug.LogError("ROSVIK V17: OSM data unavailable.");
                    return;
                }

                Directory.CreateDirectory("Assets/Rosvik/Scenes");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                scene.name = "RosvikHero";
                Transform root = new GameObject("ROSVIK_CLEAN_WORLD_V17").transform;

                Material ground = Mat("V17 damp ground", new Color(.25f,.28f,.18f), .03f);
                Material asphalt = Mat("V17 wet asphalt", new Color(.105f,.115f,.115f), .30f);
                Material service = Mat("V17 service road", new Color(.17f,.17f,.16f), .16f);
                Material path = Mat("V17 footpath", new Color(.31f,.29f,.24f), .10f);
                Material genericWall = Mat("V17 generic wall", new Color(.45f,.43f,.38f), .06f);
                Material houseWall = Mat("V17 house wall", new Color(.49f,.45f,.37f), .06f);
                Material roof = Mat("V17 roof", new Color(.15f,.16f,.16f), .20f);
                Material mainWall = Mat("V17 school wall", new Color(.42f,.39f,.31f), .07f);
                Material oldYellow = Mat("V17 old school yellow", new Color(.63f,.48f,.20f), .07f);
                Material oldWhite = Mat("V17 old school trim", new Color(.82f,.81f,.75f), .12f);
                Material arenaWall = Mat("V17 arena wall", new Color(.34f,.36f,.36f), .09f);
                Material pitch = Mat("V17 football grass", new Color(.18f,.38f,.14f), .04f);
                Material pitchLine = Mat("V17 pitch line", new Color(.78f,.80f,.70f), .08f);
                Material metal = Mat("V17 metal", new Color(.10f,.11f,.115f), .25f);

                Cube("Ground collider", root, new Vector3(60f,-.25f,-110f), new Vector3(650f,.5f,650f), ground, true);

                Transform roads = Group(root,"01 ROADS AND PATHS - OSM");
                Transform buildings = Group(root,"02 BUILDINGS - OSM FOOTPRINTS");
                Transform sports = Group(root,"03 SPORTS - OSM");

                Invoke(typeof(RosvikMapLockV15), "BuildRoads", roads, ways, asphalt, service, path);
                Invoke(typeof(RosvikWorldPassV16), "BuildExactBuildings", buildings, ways, genericWall, houseWall, roof);
                Invoke(typeof(RosvikWorldPassV16), "BuildExactPitches", sports, ways, pitch, pitchLine, metal);

                BuildMappedLandmark(buildings, ways, MainSchoolWay, "ROSVIKS SKOLA - exact OSM footprint", 3.25f, mainWall, roof, 1.10f, .35f, metal);
                BuildMappedLandmark(buildings, ways, OldSchoolWay, "TRÄSKOLAN - exact OSM footprint", 6.0f, oldYellow, roof, 3.00f, .45f, metal);
                BuildMappedLandmark(buildings, ways, ArenaWay, "NORRBOTTEN STÅL ARENA - exact OSM footprint", 7.2f, arenaWall, roof, 2.0f, .45f, metal);
                AddOldSchoolTrim(buildings, ways, oldWhite);

                RosvikOsmV15.Way main = ways.FirstOrDefault(w => w.Id == MainSchoolWay);
                Vector3 spawn = main != null ? RosvikOsmV15.Centroid(main) + new Vector3(-8f,.15f,12f) : Vector3.zero;
                GameObject player = BuildPlayer(root, spawn, metal);
                BuildCamera(player.transform);
                BuildLighting();

                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
                EditorPrefs.SetInt(Key, Version);
                Selection.activeObject = root.gameObject;
                Debug.Log("ROSVIK V17 CLEAN WORLD BUILT: one authoritative scene, real OSM roads/buildings/pitches, no stacked V10-V16 prototype objects.");
            } catch (Exception ex) {
                Debug.LogError("ROSVIK V17 FAILED: " + ex);
            }
        }

        static void Invoke(Type type, string method, params object[] args) {
            MethodInfo mi = type.GetMethod(method, HiddenStatic);
            if (mi == null) throw new MissingMethodException(type.FullName, method);
            mi.Invoke(null, args);
        }

        static void BuildMappedLandmark(Transform parent, List<RosvikOsmV15.Way> ways, long id, string name, float height, Material wall, Material roof, float rise, float overhang, Material metal) {
            RosvikOsmV15.Way w = ways.FirstOrDefault(x => x.Id == id);
            if (w == null || !w.Closed) return;
            List<Vector3> pts = w.Nodes.Select(n => n.Pos).ToList();
            if (pts.Count > 2) pts.RemoveAt(pts.Count-1);

            MethodInfo extrude = typeof(RosvikWorldPassV16).GetMethod("ExtrudedPolygon", HiddenStatic);
            if (extrude == null) throw new MissingMethodException(typeof(RosvikWorldPassV16).FullName, "ExtrudedPolygon");
            GameObject body = (GameObject)extrude.Invoke(null, new object[]{ name, parent, pts, height, wall, wall });
            if (!body) return;

            // Hide the flat cap made by ExtrudedPolygon. Use one clean, symmetrical roof instead.
            Transform flatRoof = body.transform.Find("roof");
            if (flatRoof) flatRoof.gameObject.SetActive(false);
            AddGableRoof(body.transform, w, height, rise, overhang, roof, metal);
        }

        static void AddGableRoof(Transform parent, RosvikOsmV15.Way w, float wallH, float rise, float overhang, Material roof, Material ridge) {
            RosvikOsmV15.OBounds b = RosvikOsmV15.Bounds(w);
            float width = b.Width + overhang*2f;
            float depth = b.Depth + overhang*2f;
            Mesh mesh = GableMesh(width, depth, rise);
            GameObject roofGo = new GameObject("clean symmetrical gable roof");
            roofGo.transform.SetParent(parent,false);
            roofGo.transform.position = new Vector3(b.Center.x,wallH,b.Center.z);
            roofGo.transform.rotation = Quaternion.FromToRotation(Vector3.right,b.AxisX);
            roofGo.AddComponent<MeshFilter>().sharedMesh = mesh;
            roofGo.AddComponent<MeshRenderer>().sharedMaterial = roof;

            GameObject cap = Cube("ridge cap",parent,new Vector3(b.Center.x,wallH+rise+.035f,b.Center.z),new Vector3(width+.08f,.10f,.13f),ridge,false);
            cap.transform.rotation = Quaternion.FromToRotation(Vector3.right,b.AxisX);
        }

        static Mesh GableMesh(float width, float depth, float rise) {
            float x=width*.5f, z=depth*.5f;
            Vector3[] v={
                new Vector3(-x,0,-z),new Vector3(-x,rise,0),new Vector3(-x,0,z),
                new Vector3(x,0,-z),new Vector3(x,rise,0),new Vector3(x,0,z)
            };
            int[] t={
                0,4,3, 0,1,4, 1,5,4, 1,2,5,
                3,4,0, 4,1,0, 4,5,1, 5,2,1,
                0,2,1, 3,4,5
            };
            Mesh m=new Mesh{name="V17 clean gable",vertices=v,triangles=t};
            m.RecalculateNormals();m.RecalculateBounds();return m;
        }

        static void AddOldSchoolTrim(Transform parent, List<RosvikOsmV15.Way> ways, Material white) {
            RosvikOsmV15.Way w=ways.FirstOrDefault(x=>x.Id==OldSchoolWay); if(w==null)return;
            Transform b=Find(parent,"TRÄSKOLAN - exact OSM footprint"); if(!b)return;
            RosvikOsmV15.OBounds box=RosvikOsmV15.Bounds(w);
            Vector3 axis=box.AxisX.normalized, perp=new Vector3(-axis.z,0f,axis.x);
            Vector3[] c={
                box.Center+axis*box.Width*.5f+perp*box.Depth*.5f,
                box.Center-axis*box.Width*.5f+perp*box.Depth*.5f,
                box.Center+axis*box.Width*.5f-perp*box.Depth*.5f,
                box.Center-axis*box.Width*.5f-perp*box.Depth*.5f
            };
            foreach(Vector3 p in c) Cube("white corner board",b,new Vector3(p.x,3.0f,p.z),new Vector3(.18f,5.55f,.18f),white,false);
        }

        static GameObject BuildPlayer(Transform parent, Vector3 pos, Material dark) {
            GameObject p=new GameObject("PLAYER"); p.transform.SetParent(parent); p.transform.position=pos;
            CharacterController cc=p.AddComponent<CharacterController>(); cc.height=1.85f; cc.radius=.32f; cc.center=new Vector3(0,.93f,0); cc.stepOffset=.25f;
            p.AddComponent<RosvikPlayerController>();
            Material coat=Mat("V17 player coat",new Color(.38f,.10f,.08f),.08f);
            Material skin=Mat("V17 skin",new Color(.60f,.46f,.35f),.10f);
            Cube("body",p.transform,new Vector3(0,1.08f,0),new Vector3(.52f,.82f,.34f),coat,false);
            GameObject head=GameObject.CreatePrimitive(PrimitiveType.Sphere); head.name="head"; head.transform.SetParent(p.transform); head.transform.localPosition=new Vector3(0,1.70f,0); head.transform.localScale=Vector3.one*.35f; head.GetComponent<Renderer>().sharedMaterial=skin; UnityEngine.Object.DestroyImmediate(head.GetComponent<Collider>());
            Cube("leg L",p.transform,new Vector3(-.13f,.48f,0),new Vector3(.18f,.68f,.22f),dark,false);
            Cube("leg R",p.transform,new Vector3(.13f,.48f,0),new Vector3(.18f,.68f,.22f),dark,false);
            return p;
        }

        static void BuildCamera(Transform target) {
            GameObject go=new GameObject("Main Camera"); go.tag="MainCamera";
            Camera cam=go.AddComponent<Camera>(); cam.orthographic=true; cam.orthographicSize=10.5f; cam.nearClipPlane=.1f; cam.farClipPlane=500f; cam.backgroundColor=new Color(.50f,.58f,.60f);
            IsometricCameraRig rig=go.AddComponent<IsometricCameraRig>(); rig.target=target; rig.yaw=43f; rig.pitch=44f; rig.orthographicSize=10.5f; rig.minSize=7f; rig.maxSize=18f;
        }

        static void BuildLighting() {
            GameObject sunGo=new GameObject("Overcast directional light");
            Light sun=sunGo.AddComponent<Light>(); sun.type=LightType.Directional; sun.intensity=.72f; sun.color=new Color(.88f,.91f,.92f); sun.shadows=LightShadows.Soft; sun.shadowStrength=.55f;
            sunGo.transform.rotation=Quaternion.Euler(48f,-35f,0f);
            RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=new Color(.48f,.50f,.49f);
            RenderSettings.fog=true; RenderSettings.fogColor=new Color(.57f,.61f,.61f); RenderSettings.fogDensity=.0018f;
        }

        static Transform Group(Transform root,string name){Transform t=new GameObject(name).transform;t.SetParent(root,false);return t;}
        static Transform Find(Transform root,string name){foreach(Transform t in root.GetComponentsInChildren<Transform>(true))if(t.name==name)return t;return null;}
        static GameObject Cube(string n,Transform p,Vector3 pos,Vector3 sc,Material m,bool col){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=sc;g.GetComponent<Renderer>().sharedMaterial=m;if(!col)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
        static Material Mat(string n,Color c,float sm){Shader s=GraphicsSettings.defaultRenderPipeline!=null?Shader.Find("Universal Render Pipeline/Lit"):Shader.Find("Standard");if(!s||!s.isSupported)s=Shader.Find("Sprites/Default");Material m=new Material(s){name=n,color=c};if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",sm);return m;}
    }
}
#endif