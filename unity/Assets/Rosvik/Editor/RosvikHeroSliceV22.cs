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

namespace Rosvik.Blackout.EditorTools {
    /// <summary>
    /// V22 turns the accurate V20 Rosvik diorama into a close, playable hero slice.
    /// Geography stays map-driven. Near-camera nature and props use real CC0 meshes
    /// from Kenney Nature Kit and KayKit City Builder Bits instead of primitives.
    /// </summary>
    [InitializeOnLoad]
    public static class RosvikHeroSliceV22 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_HERO_SLICE_VERSION";
        const int Version = 22;
        const long MainSchoolWay = 163199458;

        const string ModelRoot = "Assets/Rosvik/ThirdParty/V22Models";
        const string KenneyRoot = ModelRoot + "/KenneyNature";
        const string KayKitRoot = ModelRoot + "/KayKitCityBits";
        const string GeneratedRoot = "Assets/Rosvik/GeneratedV20";

        struct ModelSource {
            public string assetPath;
            public string url;
            public ModelSource(string p, string u) { assetPath = p; url = u; }
        }

        static readonly ModelSource[] Sources = {
            new ModelSource(KenneyRoot + "/tree_pineDefaultA.obj", "https://raw.githubusercontent.com/ETdoFresh/kenney.nl/master/kenney_natureKit_2.1/Models/OBJ%20format/tree_pineDefaultA.obj"),
            new ModelSource(KenneyRoot + "/tree_pineTallA.obj", "https://raw.githubusercontent.com/ETdoFresh/kenney.nl/master/kenney_natureKit_2.1/Models/OBJ%20format/tree_pineTallA.obj"),
            new ModelSource(KenneyRoot + "/tree_default_fall.obj", "https://raw.githubusercontent.com/ETdoFresh/kenney.nl/master/kenney_natureKit_2.1/Models/OBJ%20format/tree_default_fall.obj"),
            new ModelSource(KenneyRoot + "/plant_bushDetailed.obj", "https://raw.githubusercontent.com/ETdoFresh/kenney.nl/master/kenney_natureKit_2.1/Models/OBJ%20format/plant_bushDetailed.obj"),
            new ModelSource(KenneyRoot + "/rock_smallA.obj", "https://raw.githubusercontent.com/ETdoFresh/kenney.nl/master/kenney_natureKit_2.1/Models/OBJ%20format/rock_smallA.obj"),
            new ModelSource(KayKitRoot + "/streetlight.obj", "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-City-Builder-Bits-1.0/main/addons/kaykit_city_builder_bits/Assets/obj/streetlight.obj"),
            new ModelSource(KayKitRoot + "/bench.obj", "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-City-Builder-Bits-1.0/main/addons/kaykit_city_builder_bits/Assets/obj/bench.obj"),
            new ModelSource(KayKitRoot + "/car_hatchback.obj", "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-City-Builder-Bits-1.0/main/addons/kaykit_city_builder_bits/Assets/obj/car_hatchback.obj")
        };

        static RosvikHeroSliceV22() {
            // V21 must never replace the V22 root with another clean V20 after this pass.
            EditorPrefs.SetInt("ROSVIK_FORCE_DIORAMA_V21_DONE", 1);
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        [MenuItem("Rosvik/Rebuild Hero Slice V22")]
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

            EditorApplication.update -= TryApply;
            Apply();
        }

        static void Apply() {
            try {
                EnsureDiorama();
                EnsureExternalModels();

                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                GameObject root = GameObject.Find("ROSVIK_DIORAMA_GAME_V20");
                if (!root) root = GameObject.Find("ROSVIK_HERO_SLICE_V22");
                if (!root) throw new InvalidOperationException("V22 could not find the V20 Rosvik diorama root.");

                List<RosvikOsmV15.Way> ways = RosvikOsmV15.LoadWays();
                RosvikOsmV15.Way main = ways.FirstOrDefault(w => w.Id == MainSchoolWay);
                if (main == null) throw new InvalidOperationException("V22 could not find Rosviks skola in map data.");
                Vector3 school = RosvikOsmV15.Centroid(main);

                Transform oldNature = Find(root.transform, "05 VEGETATION");
                if (oldNature) oldNature.gameObject.SetActive(false);
                Transform existing = Find(root.transform, "07 HERO SLICE - REAL ASSETS");
                if (existing) UnityEngine.Object.DestroyImmediate(existing.gameObject);
                Transform hero = Group(root.transform, "07 HERO SLICE - REAL ASSETS");

                Material spruceDark = MaterialAsset(GeneratedRoot + "/mat_v22_spruce_dark.mat", new Color(.07f,.16f,.09f), .04f, 0f);
                Material spruceLight = MaterialAsset(GeneratedRoot + "/mat_v22_spruce_light.mat", new Color(.11f,.24f,.13f), .05f, 0f);
                Material fallLeaf = MaterialAsset(GeneratedRoot + "/mat_v22_fall_leaf.mat", new Color(.38f,.35f,.14f), .04f, 0f);
                Material bush = MaterialAsset(GeneratedRoot + "/mat_v22_bush.mat", new Color(.23f,.31f,.12f), .04f, 0f);
                Material rock = MaterialAsset(GeneratedRoot + "/mat_v22_rock.mat", new Color(.30f,.31f,.28f), .10f, 0f);
                Material metal = MaterialAsset(GeneratedRoot + "/mat_v22_metal.mat", new Color(.075f,.08f,.08f), .30f, .12f);
                Material wood = MaterialAsset(GeneratedRoot + "/mat_v22_wood.mat", new Color(.34f,.19f,.10f), .09f, 0f);
                Material carRed = MaterialAsset(GeneratedRoot + "/mat_v22_car_red.mat", new Color(.40f,.09f,.065f), .34f, .10f);
                Material carBlue = MaterialAsset(GeneratedRoot + "/mat_v22_car_blue.mat", new Color(.10f,.19f,.24f), .34f, .10f);

                GameObject pine = LoadModel(KenneyRoot + "/tree_pineDefaultA.obj");
                GameObject tallPine = LoadModel(KenneyRoot + "/tree_pineTallA.obj");
                GameObject fallTree = LoadModel(KenneyRoot + "/tree_default_fall.obj");
                GameObject bushModel = LoadModel(KenneyRoot + "/plant_bushDetailed.obj");
                GameObject rockModel = LoadModel(KenneyRoot + "/rock_smallA.obj");
                GameObject lampModel = LoadModel(KayKitRoot + "/streetlight.obj");
                GameObject benchModel = LoadModel(KayKitRoot + "/bench.obj");
                GameObject carModel = LoadModel(KayKitRoot + "/car_hatchback.obj");

                bool haveNature = pine && tallPine && fallTree && bushModel;
                if (!haveNature && oldNature) oldNature.gameObject.SetActive(true);

                if (haveNature)
                    PopulateNature(hero, ways, school, pine, tallPine, fallTree, bushModel, rockModel, spruceDark, spruceLight, fallLeaf, bush, rock);

                PopulateStreetFurniture(hero, ways, school, lampModel, benchModel, carModel, metal, wood, carRed, carBlue);

                Transform player = Find(root.transform, "PLAYER");
                if (player) {
                    Vector3 spawn = FindHeroSpawn(ways, school);
                    player.position = spawn;
                    player.rotation = Quaternion.identity;
                }

                TuneCamera(player);
                TuneLighting();

                RosvikMapHUD hud = root.GetComponent<RosvikMapHUD>();
                if (hud) hud.maxLabelDistance = 72f;

                root.name = "ROSVIK_HERO_SLICE_V22";
                EditorPrefs.SetInt("ROSVIK_FORCE_DIORAMA_V21_DONE", 1);
                EditorPrefs.SetInt(Key, Version);
                Selection.activeObject = null;
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
                Debug.Log("ROSVIK V22: close school hero slice built with real CC0 Kenney/KayKit meshes; map geography untouched.");
            } catch (Exception ex) {
                Debug.LogError("ROSVIK V22 FAILED: " + ex);
            }
        }

        static void EnsureDiorama() {
            if (File.Exists(ScenePath)) {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                if (GameObject.Find("ROSVIK_DIORAMA_GAME_V20") || GameObject.Find("ROSVIK_HERO_SLICE_V22")) return;
            }
            EditorPrefs.DeleteKey("ROSVIK_DIORAMA_VERSION");
            RosvikDioramaV20.Build();
            if (!GameObject.Find("ROSVIK_DIORAMA_GAME_V20"))
                throw new InvalidOperationException("Could not build V20 base scene for V22.");
        }

        static void EnsureExternalModels() {
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), KenneyRoot));
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), KayKitRoot));

            int downloaded = 0;
            foreach (ModelSource src in Sources) {
                string full = Path.Combine(Directory.GetCurrentDirectory(), src.assetPath.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(full) && new FileInfo(full).Length > 100) continue;
                try {
                    using (WebClient wc = new WebClient()) {
                        wc.Headers[HttpRequestHeader.UserAgent] = "RosvikBlackout-UnityEditor";
                        string text = wc.DownloadString(src.url);
                        File.WriteAllText(full, StripObjMaterials(text));
                        downloaded++;
                    }
                } catch (Exception ex) {
                    Debug.LogWarning("V22 asset download failed for " + src.assetPath + ": " + ex.Message);
                }
            }

            if (downloaded > 0) AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (ModelSource src in Sources)
                if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), src.assetPath.Replace('/', Path.DirectorySeparatorChar))))
                    AssetDatabase.ImportAsset(src.assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        static string StripObjMaterials(string text) {
            string[] lines = text.Replace("\r\n", "\n").Replace('\r','\n').Split('\n');
            return string.Join("\n", lines.Where(l => !l.StartsWith("mtllib ", StringComparison.OrdinalIgnoreCase)
                                                    && !l.StartsWith("usemtl ", StringComparison.OrdinalIgnoreCase)));
        }

        static GameObject LoadModel(string path) {
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        static void PopulateNature(Transform parent, List<RosvikOsmV15.Way> ways, Vector3 school,
            GameObject pine, GameObject tallPine, GameObject fallTree, GameObject bushModel, GameObject rockModel,
            Material spruceDark, Material spruceLight, Material fallLeaf, Material bush, Material rock) {

            System.Random rng = new System.Random(2205);
            int trees = 0, bushes = 0, rocks = 0;
            for (int attempt = 0; attempt < 320 && trees < 34; attempt++) {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float r = 22f + (float)rng.NextDouble() * 72f;
                Vector3 p = school + new Vector3(Mathf.Cos(a)*r, .04f, Mathf.Sin(a)*r);
                if (!SafeForNature(p, ways, 4.3f)) continue;

                int kind = trees % 6;
                GameObject source = kind == 0 ? fallTree : (kind % 3 == 0 ? tallPine : pine);
                Material mat = kind == 0 ? fallLeaf : (kind % 2 == 0 ? spruceLight : spruceDark);
                float height = kind == 0 ? Mathf.Lerp(4.2f, 5.5f, (float)rng.NextDouble()) : Mathf.Lerp(5.0f, 7.6f, (float)rng.NextDouble());
                PlaceModel(source, parent, "tree", p, (float)rng.NextDouble()*360f, height, mat);
                trees++;
            }

            for (int attempt = 0; attempt < 220 && bushes < 22; attempt++) {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float r = 15f + (float)rng.NextDouble() * 68f;
                Vector3 p = school + new Vector3(Mathf.Cos(a)*r, .035f, Mathf.Sin(a)*r);
                if (!SafeForNature(p, ways, 2.0f)) continue;
                PlaceModel(bushModel, parent, "bush", p, (float)rng.NextDouble()*360f, Mathf.Lerp(.65f,1.15f,(float)rng.NextDouble()), bush);
                bushes++;
            }

            if (rockModel) {
                for (int attempt = 0; attempt < 100 && rocks < 9; attempt++) {
                    float a=(float)rng.NextDouble()*Mathf.PI*2f, r=28f+(float)rng.NextDouble()*62f;
                    Vector3 p=school+new Vector3(Mathf.Cos(a)*r,.03f,Mathf.Sin(a)*r);
                    if(!SafeForNature(p,ways,2.4f))continue;
                    PlaceModel(rockModel,parent,"rock",p,(float)rng.NextDouble()*360f,Mathf.Lerp(.35f,.70f,(float)rng.NextDouble()),rock);
                    rocks++;
                }
            }
        }

        static void PopulateStreetFurniture(Transform parent, List<RosvikOsmV15.Way> ways, Vector3 school,
            GameObject lamp, GameObject bench, GameObject car, Material metal, Material wood, Material carRed, Material carBlue) {

            List<RoadSample> near = RoadSamples(ways, school, 90f);
            if (lamp) {
                int made=0;
                foreach (RoadSample s in near.Where(x => x.kind==0 || x.kind==1).OrderBy(x => x.distance)) {
                    if (made>=7) break;
                    if (s.distance < 10f) continue;
                    Vector3 side = new Vector3(-s.dir.z,0,s.dir.x).normalized * 2.7f;
                    Vector3 p = s.point + side; p.y=.04f;
                    if (InsideBuilding(p,ways)) continue;
                    PlaceModel(lamp,parent,"streetlight",p,Yaw(s.dir),4.6f,metal);
                    made++;
                }
            }

            if (bench) {
                Vector3[] offsets={new Vector3(-18,0,15),new Vector3(16,0,17),new Vector3(-24,0,-11),new Vector3(22,0,-15)};
                int made=0;
                foreach(Vector3 o in offsets){Vector3 p=school+o;p.y=.04f;if(!SafeForProp(p,ways))continue;PlaceModel(bench,parent,"bench",p,35f+made*70f,.90f,wood);if(++made>=3)break;}
            }

            if (car && near.Count>0) {
                int made=0;
                foreach(RoadSample s in near.Where(x=>x.kind==1 || x.kind==2).OrderBy(x=>x.distance)) {
                    if(made>=2)break;if(s.distance<18f)continue;
                    Vector3 side=new Vector3(-s.dir.z,0,s.dir.x).normalized*1.55f;Vector3 p=s.point+side;p.y=.06f;
                    if(InsideBuilding(p,ways))continue;
                    PlaceModel(car,parent,"parked car",p,Yaw(s.dir),1.45f,made==0?carRed:carBlue);
                    made++;
                }
            }
        }

        struct RoadSample { public Vector3 point,dir; public float distance; public int kind; }

        static List<RoadSample> RoadSamples(List<RosvikOsmV15.Way> ways, Vector3 center, float radius) {
            List<RoadSample> result=new List<RoadSample>();
            foreach(var w in ways){string h=w.Tag("highway");int kind;
                if(h=="footway"||h=="path"||h=="cycleway"||h=="pedestrian")kind=0;
                else if(h=="service"||h=="living_street")kind=1;
                else if(h=="residential"||h=="unclassified")kind=2;else continue;
                for(int i=0;i<w.Nodes.Count-1;i++){Vector3 a=w.Nodes[i].Pos,b=w.Nodes[i+1].Pos;Vector3 p=(a+b)*.5f;float d=Vector3.Distance(Flat(p),Flat(center));if(d>radius)continue;Vector3 dir=b-a;dir.y=0;if(dir.sqrMagnitude<.01f)continue;result.Add(new RoadSample{point=p,dir=dir.normalized,distance=d,kind=kind});}
            }
            return result;
        }

        static Vector3 FindHeroSpawn(List<RosvikOsmV15.Way> ways, Vector3 school) {
            Vector3 best = school + new Vector3(-14f,.18f,14f);
            float bestScore=float.MaxValue;
            foreach(var w in ways){string h=w.Tag("highway");float bias;
                if(h=="footway"||h=="path"||h=="pedestrian"||h=="cycleway")bias=0f;
                else if(h=="service"||h=="living_street")bias=12f;
                else continue;
                for(int i=0;i<w.Nodes.Count-1;i++){
                    Vector3 p=ClosestPoint(school,w.Nodes[i].Pos,w.Nodes[i+1].Pos);p.y=.18f;
                    float d=Vector3.Distance(Flat(p),Flat(school));
                    if(d<4f||d>42f||InsideBuilding(p,ways))continue;
                    float score=d+bias;if(score<bestScore){bestScore=score;best=p;}
                }
            }
            return best;
        }

        static bool SafeForNature(Vector3 p,List<RosvikOsmV15.Way> ways,float roadClearance) {
            if(InsideBuilding(p,ways)||InsidePitch(p,ways))return false;
            return DistanceToRoad(p,ways)>roadClearance;
        }
        static bool SafeForProp(Vector3 p,List<RosvikOsmV15.Way> ways){return !InsideBuilding(p,ways)&&!InsidePitch(p,ways)&&DistanceToRoad(p,ways)>1.4f;}

        static bool InsideBuilding(Vector3 p,List<RosvikOsmV15.Way> ways){foreach(var w in ways){if(!w.Closed||string.IsNullOrEmpty(w.Tag("building"))||w.Tag("building")=="no")continue;if(Inside(p,Points(w)))return true;}return false;}
        static bool InsidePitch(Vector3 p,List<RosvikOsmV15.Way> ways){foreach(var w in ways){if(!w.Closed)continue;if(w.Tag("leisure")!="pitch"&&w.Tag("sport")!="soccer")continue;if(Inside(p,Points(w)))return true;}return false;}

        static float DistanceToRoad(Vector3 p,List<RosvikOsmV15.Way> ways) {
            float best=float.MaxValue;
            foreach(var w in ways){string h=w.Tag("highway");if(string.IsNullOrEmpty(h)||w.Nodes.Count<2)continue;
                for(int i=0;i<w.Nodes.Count-1;i++){Vector3 q=ClosestPoint(p,w.Nodes[i].Pos,w.Nodes[i+1].Pos);best=Mathf.Min(best,Vector3.Distance(Flat(p),Flat(q)));}}
            return best;
        }

        static GameObject PlaceModel(GameObject asset,Transform parent,string name,Vector3 pos,float yaw,float targetHeight,Material material) {
            if(!asset)return null;
            GameObject go=(GameObject)PrefabUtility.InstantiatePrefab(asset,parent);
            if(!go)go=UnityEngine.Object.Instantiate(asset,parent);
            go.name=name;go.transform.position=pos;go.transform.rotation=Quaternion.Euler(0,yaw,0);go.transform.localScale=Vector3.one;
            Bounds b=RendererBounds(go);float h=Mathf.Max(.01f,b.size.y);float s=targetHeight/h;go.transform.localScale=Vector3.one*s;
            foreach(Renderer r in go.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=material;
            foreach(Collider c in go.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
            return go;
        }

        static Bounds RendererBounds(GameObject go) {
            Renderer[] r=go.GetComponentsInChildren<Renderer>(true);if(r.Length==0)return new Bounds(go.transform.position,Vector3.one);
            Bounds b=r[0].bounds;for(int i=1;i<r.Length;i++)b.Encapsulate(r[i].bounds);return b;
        }

        static void TuneCamera(Transform player) {
            Camera cam=Camera.main;if(!cam)return;cam.orthographic=true;cam.orthographicSize=10.5f;cam.backgroundColor=new Color(.32f,.36f,.31f);
            IsometricCameraRig rig=cam.GetComponent<IsometricCameraRig>();if(rig){if(player)rig.target=player;rig.yaw=38f;rig.pitch=43f;rig.orthographicSize=10.5f;rig.minSize=7.2f;rig.maxSize=17f;rig.zoomStep=.75f;rig.focusOffset=new Vector3(0,1.0f,0);rig.followSharpness=11f;}
        }

        static void TuneLighting() {
            Light sun=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l=>l.type==LightType.Directional);
            if(sun){sun.intensity=.98f;sun.shadowStrength=.64f;sun.color=new Color(.94f,.92f,.86f);sun.transform.rotation=Quaternion.Euler(52f,-34f,0);}
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.34f,.38f,.33f);RenderSettings.fog=true;RenderSettings.fogColor=new Color(.43f,.47f,.44f);RenderSettings.fogDensity=.00115f;
        }

        static Material MaterialAsset(string path,Color color,float smooth,float metal) {
            Material m=AssetDatabase.LoadAssetAtPath<Material>(path);Shader s=GraphicsSettings.defaultRenderPipeline!=null?Shader.Find("Universal Render Pipeline/Lit"):Shader.Find("Standard");if(!s||!s.isSupported)s=Shader.Find("Sprites/Default");
            if(!m){m=new Material(s){name=Path.GetFileNameWithoutExtension(path)};AssetDatabase.CreateAsset(m,path);}m.shader=s;m.color=color;if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",metal);EditorUtility.SetDirty(m);return m;
        }

        static Transform Group(Transform p,string n){Transform t=new GameObject(n).transform;t.SetParent(p,false);return t;}
        static Transform Find(Transform root,string n){foreach(Transform t in root.GetComponentsInChildren<Transform>(true))if(t.name==n)return t;return null;}
        static float Yaw(Vector3 d){return Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;}
        static Vector3 Flat(Vector3 v){v.y=0;return v;}
        static Vector3 ClosestPoint(Vector3 p,Vector3 a,Vector3 b){Vector3 ab=Flat(b-a);float den=ab.sqrMagnitude;if(den<.0001f)return a;float t=Mathf.Clamp01(Vector3.Dot(Flat(p-a),ab)/den);return a+ab*t;}
        static List<Vector3> Points(RosvikOsmV15.Way w){List<Vector3> p=w.Nodes.Select(n=>n.Pos).ToList();if(p.Count>2&&w.Closed)p.RemoveAt(p.Count-1);return p;}
        static bool Inside(Vector3 p,List<Vector3> poly){if(poly==null||poly.Count<3)return false;bool inside=false;int j=poly.Count-1;for(int i=0;i<poly.Count;i++){float xi=poly[i].x,zi=poly[i].z,xj=poly[j].x,zj=poly[j].z;bool hit=((zi>p.z)!=(zj>p.z))&&(p.x<(xj-xi)*(p.z-zi)/(zj-zi+.00001f)+xi);if(hit)inside=!inside;j=i;}return inside;}
    }
}
#endif
