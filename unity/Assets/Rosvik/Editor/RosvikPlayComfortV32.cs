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
    /// V32 keeps the V31/V30 world and gameplay intact. It widens the useful camera range
    /// and adds restrained small-town dressing outside the immediate school entrance so
    /// zooming out reveals a lived-in Rosvik rather than empty map space.
    /// Runtime scripts own the exact screen-space WASD and robust zoom input fixes.
    /// </summary>
    [InitializeOnLoad]
    public static class RosvikPlayComfortV32 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_PLAY_COMFORT_V32_VERSION";
        const int Version = 32;
        const string RootName = "ROSVIK_PLAY_COMFORT_V32";
        const string GroupName = "15 PLAY COMFORT + VILLAGE DEPTH V32";
        const string GeneratedDir = "Assets/Rosvik/GeneratedV32";
        const string ModelDir = "Assets/Rosvik/ThirdParty/V32TownBits";
        const string ProvenMaterial = "Assets/Rosvik/GeneratedV20/mat_ground.mat";
        const string KenneyRoot = "Assets/Rosvik/ThirdParty/V22Models/KenneyNature";
        const long MainSchoolWay = 163199458;

        struct Source {
            public string file, url;
            public Source(string f, string u) { file=f; url=u; }
        }

        const string KayBase = "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-City-Builder-Bits-1.0/main/addons/kaykit_city_builder_bits/Assets/obj/";
        static readonly Source[] Sources = {
            new Source("car_sedan.obj", KayBase + "car_sedan.obj"),
            new Source("car_stationwagon.obj", KayBase + "car_stationwagon.obj"),
            new Source("dumpster.obj", KayBase + "dumpster.obj"),
            new Source("trash_A.obj", KayBase + "trash_A.obj")
        };

        static RosvikPlayComfortV32() {
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        [MenuItem("Rosvik/Rebuild Play Comfort V32")]
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
            return GameObject.Find("ROSVIK_CHARACTER_ATMOSPHERE_V31")
                ?? GameObject.Find("ROSVIK_FIRST_GAMEPLAY_V30")
                ?? GameObject.Find(RootName);
        }

        static void Apply(UScene scene, GameObject root) {
            try {
                Directory.CreateDirectory(GeneratedDir);
                EnsureModels();
                AssetDatabase.Refresh();

                Transform previous = Find(root.transform, GroupName);
                if (previous) UnityEngine.Object.DestroyImmediate(previous.gameObject);
                Transform group = NewGroup(root.transform, GroupName);

                List<RosvikOsmV15.Way> ways = RosvikOsmV15.LoadWays();
                RosvikOsmV15.Way main = ways != null ? ways.FirstOrDefault(w => w.Id == MainSchoolWay) : null;
                if (main == null) throw new InvalidOperationException("V32 could not find Rosviks skola in OSM data.");
                Vector3 school = RosvikOsmV15.Centroid(main);
                Transform player = Find(root.transform, "PLAYER");

                Material carBlue = Mat("car_muted_blue", new Color(.105f,.17f,.19f), .30f);
                Material carGrey = Mat("car_weathered_grey", new Color(.24f,.245f,.225f), .28f);
                Material utility = Mat("utility_dark", new Color(.095f,.115f,.10f), .18f);
                Material binMat = Mat("bin_dark", new Color(.075f,.085f,.075f), .20f);
                Material wet = Mat("roadside_wet", new Color(.045f,.075f,.082f), .92f);
                Material spruceA = Mat("outer_spruce", new Color(.05f,.125f,.07f), .03f);
                Material spruceB = Mat("outer_spruce_soft", new Color(.085f,.18f,.095f), .03f);
                Material autumn = Mat("outer_autumn", new Color(.355f,.285f,.09f), .03f);
                Material shrub = Mat("outer_shrub", new Color(.145f,.225f,.095f), .03f);

                GameObject sedan = AssetDatabase.LoadAssetAtPath<GameObject>(ModelDir + "/car_sedan.obj");
                GameObject wagon = AssetDatabase.LoadAssetAtPath<GameObject>(ModelDir + "/car_stationwagon.obj");
                GameObject dumpster = AssetDatabase.LoadAssetAtPath<GameObject>(ModelDir + "/dumpster.obj");
                GameObject trash = AssetDatabase.LoadAssetAtPath<GameObject>(ModelDir + "/trash_A.obj");
                GameObject pine = AssetDatabase.LoadAssetAtPath<GameObject>(KenneyRoot + "/tree_pineDefaultA.obj");
                GameObject tallPine = AssetDatabase.LoadAssetAtPath<GameObject>(KenneyRoot + "/tree_pineTallA.obj");
                GameObject fallTree = AssetDatabase.LoadAssetAtPath<GameObject>(KenneyRoot + "/tree_default_fall.obj");
                GameObject bush = AssetDatabase.LoadAssetAtPath<GameObject>(KenneyRoot + "/plant_bushDetailed.obj");

                List<RoadSample> roads = RoadSamples(ways, school, 120f);
                AddParkedVehicles(group, roads, ways, player, sedan, wagon, carBlue, carGrey);
                AddUtilityDetails(group, roads, ways, school, dumpster, trash, utility, binMat);
                AddRoadsideWetness(group, roads, ways, wet);
                AddOuterVegetation(group, root.transform, ways, school, pine, tallPine, fallTree, bush, spruceA, spruceB, autumn, shrub);
                TuneCamera(player);

                root.name = RootName;
                EditorPrefs.SetInt(Key, Version);
                Selection.activeObject = null;
                EditorUtility.SetDirty(root);
                AssetDatabase.SaveAssets();
                if (!Busy()) {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene, ScenePath);
                }
                Debug.Log("ROSVIK V32: exact screen-axis movement/runtime zoom enabled; camera range widened and outer school/village routes dressed with restrained vehicles, utility props, wet edges and vegetation. Geography/gameplay unchanged.");
            } catch (Exception ex) {
                Debug.LogError("ROSVIK V32 FAILED: " + ex);
            }
        }

        struct RoadSample {
            public Vector3 point, dir;
            public float distance;
            public int kind; // 0 path, 1 service/living, 2 residential/unclassified
        }

        static List<RoadSample> RoadSamples(List<RosvikOsmV15.Way> ways, Vector3 center, float radius) {
            List<RoadSample> result = new List<RoadSample>();
            if (ways == null) return result;
            foreach (var w in ways) {
                string h = w.Tag("highway");
                int kind;
                if (h=="footway" || h=="path" || h=="cycleway" || h=="pedestrian") kind=0;
                else if (h=="service" || h=="living_street") kind=1;
                else if (h=="residential" || h=="unclassified") kind=2;
                else continue;
                for (int i=0;i<w.Nodes.Count-1;i++) {
                    Vector3 a=w.Nodes[i].Pos, b=w.Nodes[i+1].Pos;
                    Vector3 p=(a+b)*.5f;
                    float d=Vector3.Distance(Flat(p),Flat(center));
                    Vector3 dir=Flat(b-a);
                    if (d>radius || dir.sqrMagnitude<.01f) continue;
                    result.Add(new RoadSample { point=p, dir=dir.normalized, distance=d, kind=kind });
                }
            }
            return result;
        }

        static void AddParkedVehicles(Transform parent, List<RoadSample> roads, List<RosvikOsmV15.Way> ways, Transform player,
            GameObject sedan, GameObject wagon, Material blue, Material grey) {
            if (!sedan && !wagon) return;
            List<Vector3> used = new List<Vector3>();
            int made=0;
            foreach (RoadSample s in roads.Where(r=>r.kind==1 || r.kind==2).OrderBy(r=>r.distance)) {
                if (made>=4 || s.distance<24f) break;
                float sideSign = made%2==0 ? 1f : -1f;
                Vector3 side=new Vector3(-s.dir.z,0f,s.dir.x)*sideSign;
                Vector3 p=s.point+side*(s.kind==2?2.9f:2.45f); p.y=.055f;
                if (InsideBuilding(p,ways) || InsidePitch(p,ways) || used.Any(u=>Flat(u-p).magnitude<16f)) continue;
                if (player && Flat(player.position-p).magnitude<12f) continue;
                GameObject src=(made%2==0 && wagon)?wagon:(sedan?sedan:wagon);
                PlaceModel(src,parent,"parked village car",p,Yaw(s.dir),1.48f,made%2==0?grey:blue);
                used.Add(p); made++;
            }
        }

        static void AddUtilityDetails(Transform parent, List<RoadSample> roads, List<RosvikOsmV15.Way> ways, Vector3 school,
            GameObject dumpster, GameObject trash, Material utility, Material binMat) {
            if (dumpster) {
                foreach (RoadSample s in roads.Where(r=>r.kind==1).OrderBy(r=>Mathf.Abs(r.distance-42f))) {
                    Vector3 side=new Vector3(-s.dir.z,0f,s.dir.x);
                    Vector3 p=s.point+side*4.0f; p.y=.04f;
                    if (InsideBuilding(p,ways) || InsidePitch(p,ways) || Flat(p-school).magnitude<18f) continue;
                    PlaceModel(dumpster,parent,"service dumpster",p,Yaw(s.dir),1.28f,utility);
                    break;
                }
            }

            if (trash) {
                int made=0;
                foreach (RoadSample s in roads.Where(r=>r.kind==0).OrderBy(r=>r.distance)) {
                    if (made>=3) break;
                    if (s.distance<13f || s.distance>75f) continue;
                    Vector3 side=new Vector3(-s.dir.z,0f,s.dir.x) * (made%2==0?1f:-1f);
                    Vector3 p=s.point+side*1.15f; p.y=.035f;
                    if (InsideBuilding(p,ways) || InsidePitch(p,ways)) continue;
                    PlaceModel(trash,parent,"path waste bin",p,Yaw(s.dir),.72f,binMat);
                    made++;
                }
            }
        }

        static void AddRoadsideWetness(Transform parent, List<RoadSample> roads, List<RosvikOsmV15.Way> ways, Material wet) {
            int made=0;
            foreach (RoadSample s in roads.Where(r=>r.kind==1 || r.kind==2).OrderBy(r=>r.distance)) {
                if (made>=9) break;
                if (s.distance<18f || (made>0 && made%2==0 && s.distance<30f)) continue;
                float sign=made%2==0?1f:-1f;
                Vector3 side=new Vector3(-s.dir.z,0f,s.dir.x)*sign;
                Vector3 p=s.point+side*(s.kind==2?2.2f:1.75f); p.y=.022f;
                if (InsideBuilding(p,ways) || InsidePitch(p,ways)) continue;
                Puddle(parent,p,Yaw(s.dir)+(made%3-1)*12f,Mathf.Lerp(.55f,1.15f,(made%4)/3f),Mathf.Lerp(.18f,.35f,(made%3)/2f),wet);
                made++;
            }
        }

        static void AddOuterVegetation(Transform parent, Transform root, List<RosvikOsmV15.Way> ways, Vector3 school,
            GameObject pine, GameObject tallPine, GameObject fallTree, GameObject bush,
            Material spruceA, Material spruceB, Material autumn, Material shrub) {
            if (!pine && !fallTree && !bush) return;

            List<Vector3> treePositions = root.GetComponentsInChildren<Transform>(true)
                .Where(t=>t.name.IndexOf("tree",StringComparison.OrdinalIgnoreCase)>=0 && t.GetComponentInChildren<Renderer>(true)!=null)
                .Select(t=>t.position).ToList();

            System.Random rng=new System.Random(3209);
            int trees=0, bushes=0;
            for (int attempt=0; attempt<700 && trees<26; attempt++) {
                float a=(float)rng.NextDouble()*Mathf.PI*2f;
                float r=42f+(float)rng.NextDouble()*58f;
                Vector3 p=school+new Vector3(Mathf.Cos(a)*r,.04f,Mathf.Sin(a)*r);
                if (!SafeForNature(p,ways,2.8f) || treePositions.Any(q=>Flat(q-p).magnitude<4.8f)) continue;
                GameObject src;
                Material mat;
                if (trees%6==0 && fallTree) { src=fallTree; mat=autumn; }
                else if (trees%4==0 && tallPine) { src=tallPine; mat=spruceA; }
                else { src=pine?pine:fallTree; mat=trees%2==0?spruceA:spruceB; }
                if (!src) continue;
                PlaceModel(src,parent,"v32 outer tree",p,(float)rng.NextDouble()*360f,Mathf.Lerp(4.8f,7.8f,(float)rng.NextDouble()),mat);
                treePositions.Add(p); trees++;
            }

            if (bush) {
                for (int attempt=0; attempt<600 && bushes<38; attempt++) {
                    float a=(float)rng.NextDouble()*Mathf.PI*2f;
                    float r=34f+(float)rng.NextDouble()*67f;
                    Vector3 p=school+new Vector3(Mathf.Cos(a)*r,.035f,Mathf.Sin(a)*r);
                    if (!SafeForNature(p,ways,1.5f)) continue;
                    PlaceModel(bush,parent,"v32 outer shrub",p,(float)rng.NextDouble()*360f,Mathf.Lerp(.42f,.95f,(float)rng.NextDouble()),shrub);
                    bushes++;
                }
            }
        }

        static void TuneCamera(Transform player) {
            Camera cam=Camera.main;
            if (!cam) return;
            Rosvik.Blackout.IsometricCameraRig rig=cam.GetComponent<Rosvik.Blackout.IsometricCameraRig>();
            if (!rig) return;
            if (player) rig.target=player;
            rig.defaultSize=8.15f;
            rig.minSize=5.2f;
            rig.maxSize=19.5f;
            rig.zoomStep=.80f;
            rig.keyboardZoomSpeed=5.5f;
            rig.zoomSharpness=11f;
            rig.orthographicSize=Mathf.Clamp(rig.orthographicSize,rig.minSize,rig.maxSize);
        }

        static void EnsureModels() {
            string fullDir=FullPath(ModelDir);
            Directory.CreateDirectory(fullDir);
            int changed=0;
            foreach (Source s in Sources) {
                string assetPath=ModelDir+"/"+s.file;
                string full=FullPath(assetPath);
                if (File.Exists(full) && new FileInfo(full).Length>100) continue;
                try {
                    using (WebClient wc=new WebClient()) {
                        wc.Headers[HttpRequestHeader.UserAgent]="RosvikBlackout-UnityEditor";
                        string text=wc.DownloadString(s.url);
                        File.WriteAllText(full,StripMaterials(text));
                        changed++;
                    }
                } catch (Exception ex) {
                    Debug.LogWarning("V32 asset download failed for "+s.file+": "+ex.Message);
                }
            }
            if (changed>0) AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (Source s in Sources) {
                string p=ModelDir+"/"+s.file;
                if (File.Exists(FullPath(p))) AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
            }
        }

        static string StripMaterials(string text) {
            return string.Join("\n",text.Replace("\r\n","\n").Replace('\r','\n').Split('\n')
                .Where(l=>!l.StartsWith("mtllib ",StringComparison.OrdinalIgnoreCase) && !l.StartsWith("usemtl ",StringComparison.OrdinalIgnoreCase)));
        }

        static GameObject PlaceModel(GameObject asset, Transform parent, string name, Vector3 pos, float yaw, float targetHeight, Material mat) {
            if (!asset) return null;
            GameObject go=(GameObject)PrefabUtility.InstantiatePrefab(asset,parent);
            if (!go) go=UnityEngine.Object.Instantiate(asset,parent);
            go.name=name; go.transform.position=pos; go.transform.rotation=Quaternion.Euler(0f,yaw,0f); go.transform.localScale=Vector3.one;
            Bounds b=BoundsOf(go); float h=Mathf.Max(.01f,b.size.y); go.transform.localScale=Vector3.one*(targetHeight/h);
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial=mat;
            foreach (Collider c in go.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
            return go;
        }

        static void Puddle(Transform parent, Vector3 pos, float yaw, float length, float width, Material mat) {
            GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            g.name="roadside puddle"; g.transform.SetParent(parent,false); g.transform.position=pos; g.transform.rotation=Quaternion.Euler(0f,yaw,0f);
            g.transform.localScale=new Vector3(length,.006f,width); g.GetComponent<Renderer>().sharedMaterial=mat;
            UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        }

        static bool SafeForNature(Vector3 p,List<RosvikOsmV15.Way> ways,float clearance) {
            return !InsideBuilding(p,ways) && !InsidePitch(p,ways) && DistanceToRoad(p,ways)>clearance;
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
                string h=w.Tag("highway"); if (string.IsNullOrEmpty(h) || w.Nodes.Count<2) continue;
                for (int i=0;i<w.Nodes.Count-1;i++) best=Mathf.Min(best,Vector3.Distance(Flat(p),Flat(ClosestPoint(p,w.Nodes[i].Pos,w.Nodes[i+1].Pos))));
            }
            return best;
        }

        static Material Mat(string name,Color color,float smooth) {
            string path=GeneratedDir+"/mat_"+name+".mat";
            Material m=AssetDatabase.LoadAssetAtPath<Material>(path); Shader s=ResolveShader();
            if (!m) { m=new Material(s){name="V32 "+name}; AssetDatabase.CreateAsset(m,path); }
            m.shader=s; m.color=color;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor",color);
            if (m.HasProperty("_Color")) m.SetColor("_Color",color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness",smooth);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic",0f);
            EditorUtility.SetDirty(m); return m;
        }

        static Shader ResolveShader() {
            Material proven=AssetDatabase.LoadAssetAtPath<Material>(ProvenMaterial);
            if (proven && proven.shader && proven.shader.isSupported) return proven.shader;
            Shader s=GraphicsSettings.defaultRenderPipeline!=null?Shader.Find("Universal Render Pipeline/Lit"):Shader.Find("Standard");
            if (!s || !s.isSupported) s=Shader.Find("Sprites/Default"); return s;
        }

        static Bounds BoundsOf(GameObject go) {
            Renderer[] rs=go.GetComponentsInChildren<Renderer>(true); if (rs.Length==0) return new Bounds(go.transform.position,Vector3.one);
            Bounds b=rs[0].bounds; for (int i=1;i<rs.Length;i++) b.Encapsulate(rs[i].bounds); return b;
        }

        static Vector3 ClosestPoint(Vector3 p,Vector3 a,Vector3 b) {
            Vector3 ab=Flat(b-a); float den=ab.sqrMagnitude; if (den<.0001f) return a;
            float t=Mathf.Clamp01(Vector3.Dot(Flat(p-a),ab)/den); return a+ab*t;
        }

        static List<Vector3> Points(RosvikOsmV15.Way w) {
            List<Vector3> p=w.Nodes.Select(n=>n.Pos).ToList(); if (p.Count>2 && w.Closed) p.RemoveAt(p.Count-1); return p;
        }

        static bool Inside(Vector3 p,List<Vector3> poly) {
            if (poly==null || poly.Count<3) return false; bool inside=false; int j=poly.Count-1;
            for (int i=0;i<poly.Count;i++) {
                float xi=poly[i].x,zi=poly[i].z,xj=poly[j].x,zj=poly[j].z;
                bool hit=((zi>p.z)!=(zj>p.z)) && (p.x<(xj-xi)*(p.z-zi)/(zj-zi+.00001f)+xi);
                if (hit) inside=!inside; j=i;
            }
            return inside;
        }

        static string FullPath(string assetPath) => Path.Combine(Directory.GetCurrentDirectory(),assetPath.Replace('/',Path.DirectorySeparatorChar));
        static Transform NewGroup(Transform parent,string name) { Transform t=new GameObject(name).transform; t.SetParent(parent,false); return t; }
        static Transform Find(Transform root,string name) { foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name==name) return t; return null; }
        static Vector3 Flat(Vector3 v) { v.y=0f; return v; }
        static float Yaw(Vector3 d) => Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;
    }
}
#endif
