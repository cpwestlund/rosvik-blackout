#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UScene = UnityEngine.SceneManagement.Scene;

namespace Rosvik.Blackout.EditorTools {
    /// <summary>
    /// V42 is a composition pass, not another map rebuild. It keeps Rosvik geography,
    /// gameplay, controls and the V39 road network locked. The school hero area is cleaned
    /// of procedural vegetation confetti, then rebuilt as intentional edge clusters using
    /// existing Kenney/KayKit assets. Frost/wet ground breakup and small warm light islands
    /// establish the late-autumn-to-first-snow cozy-apocalypse direction.
    /// </summary>
    [InitializeOnLoad]
    public static class RosvikHeroCompositionV42 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_HERO_COMPOSITION_V42_VERSION";
        const int Version = 42;
        const string RootName = "ROSVIK_HERO_COMPOSITION_V42";
        const string GroupName = "24 HERO COMPOSITION V42";
        const string GeneratedDir = "Assets/Rosvik/GeneratedV42";
        const string ProvenMaterial = "Assets/Rosvik/GeneratedV20/mat_ground.mat";
        const string NatureRoot = "Assets/Rosvik/ThirdParty/V22Models/KenneyNature";
        const string CityRoot = "Assets/Rosvik/ThirdParty/V40CozyBits";
        const string CityTexture = CityRoot + "/citybits_texture.png";
        const long MainSchoolWay = 163199458;

        static readonly string[] GeneratedNatureGroups = {
            "07 HERO SLICE - REAL ASSETS",
            "11 SCHOOL LIFE V28",
            "12 SCHOOLYARD POLISH V29",
            "14 CHARACTER + ATMOSPHERE V31",
            "20 OUTER VILLAGE FABRIC V38"
        };

        static RosvikHeroCompositionV42() {
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        [MenuItem("Rosvik/Rebuild Hero Composition V42")]
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
            return GameObject.Find(RootName)
                ?? GameObject.Find("ROSVIK_HERO_AREA_ASSETS_V41")
                ?? GameObject.Find("ROSVIK_ASSET_COZY_APOCALYPSE_V40")
                ?? GameObject.Find("ROSVIK_CLEAN_ROAD_NETWORK_V39")
                ?? GameObject.Find("ROSVIK_VILLAGE_FABRIC_V38")
                ?? GameObject.Find("ROSVIK_ROAD_CLEANUP_V37");
        }

        static void Apply(UScene scene, GameObject root) {
            try {
                Directory.CreateDirectory(GeneratedDir);
                AssetDatabase.Refresh();

                Transform old = Find(root.transform, GroupName);
                if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
                Transform group = NewGroup(root.transform, GroupName);

                List<RosvikOsmV15.Way> ways = RosvikOsmV15.LoadWays();
                if (ways == null || ways.Count == 0) throw new InvalidOperationException("V42 could not load Rosvik OSM data.");
                RosvikOsmV15.Way schoolWay = ways.FirstOrDefault(w => w.Id == MainSchoolWay);
                if (schoolWay == null) throw new InvalidOperationException("V42 could not locate Rosviks skola.");

                Transform court = Find(root.transform, "school entrance court");
                Vector3 school = RosvikOsmV15.Centroid(schoolWay);
                Vector3 center = court ? BoundsOf(court.gameObject).center : school;
                center.y = .04f;
                Vector3 right = court ? Flat(court.right).normalized : RosvikOsmV15.Bounds(schoolWay).AxisX.normalized;
                if (right.sqrMagnitude < .1f) right = Vector3.right;
                Vector3 forward = court ? Flat(court.forward).normalized : new Vector3(-right.z, 0f, right.x);
                if (forward.sqrMagnitude < .1f) forward = new Vector3(-right.z, 0f, right.x);

                Material frost = Mat("first_frost", new Color(.68f,.715f,.70f), .18f);
                Material thaw = Mat("cold_thaw", new Color(.09f,.125f,.135f), .76f);
                Material worn = Mat("worn_ground", new Color(.27f,.255f,.21f), .08f);
                Material spruceDark = Mat("spruce_dark", new Color(.045f,.115f,.07f), .03f);
                Material spruceSoft = Mat("spruce_soft", new Color(.075f,.17f,.095f), .03f);
                Material autumn = Mat("late_autumn", new Color(.34f,.275f,.085f), .03f);
                Material shrub = Mat("edge_shrub", new Color(.12f,.205f,.095f), .03f);
                Texture2D cityTex = AssetDatabase.LoadAssetAtPath<Texture2D>(CityTexture);
                Material city = TexturedMat("citybits", cityTex, .20f);
                Material bulb = EmissiveMat("warm_bulb", new Color(1f,.52f,.18f), 2.4f);

                int hidden = ClearProceduralClutter(root.transform, center);
                int moved = RecomposeV41(root.transform, center, right, forward, city);
                int nature = BuildIntentionalNature(group, ways, center, right, forward, spruceDark, spruceSoft, autumn, shrub);
                int props = BuildHeroProps(group, ways, center, right, forward, city, bulb);
                BuildGroundBreakup(group, center, right, forward, frost, thaw, worn);
                TuneMood();

                root.name = RootName;
                EditorPrefs.SetInt(Key, Version);
                Selection.activeObject = null;
                EditorUtility.SetDirty(root);
                AssetDatabase.SaveAssets();
                if (!Busy()) {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene, ScenePath);
                }
                Debug.Log("ROSVIK V42: hero composition rebuilt — " + hidden + " nearby procedural nature pieces hidden, " + moved +
                          " V41 assets recomposed, " + nature + " intentional nature assets and " + props +
                          " readable hero props added. Geography/gameplay/controls/roads unchanged.");
            } catch (Exception ex) {
                Debug.LogError("ROSVIK V42 FAILED: " + ex);
            }
        }

        static int ClearProceduralClutter(Transform root, Vector3 center) {
            int hidden = 0;
            Transform[] all = root.GetComponentsInChildren<Transform>(true).ToArray();
            foreach (Transform t in all) {
                if (!t || t == root || !t.gameObject.activeSelf) continue;
                string n = t.name.ToLowerInvariant();
                bool vegetation = n.Contains("tree") || n.Contains("shrub") || n.Contains("bush") || n.Contains("stone") || n.Contains("rock");
                if (!vegetation || !HasAncestorNamed(t, GeneratedNatureGroups)) continue;
                Vector3 p = BoundsOf(t.gameObject).center;
                if (Flat(p - center).magnitude > 15.5f) continue;
                t.gameObject.SetActive(false);
                hidden++;
            }
            return hidden;
        }

        static int RecomposeV41(Transform root, Vector3 c, Vector3 right, Vector3 forward, Material city) {
            int moved = 0;
            Vector3 service = c + right * 7.7f + forward * 1.9f;
            moved += MoveNamed(root, "V41 school dumpster", service, city);
            moved += MoveNamed(root, "V41 school crate A", service - right * 1.30f + forward * .72f, city);
            moved += MoveNamed(root, "V41 school crate B", service - right * 1.75f - forward * .10f, city);
            moved += MoveNamed(root, "V41 school bin", service + right * 1.40f - forward * .18f, city);
            moved += MoveNamed(root, "V41 school bench", c - right * 4.7f - forward * 2.55f, city);
            moved += MoveNamed(root, "V41 warm school lamp", c + right * 4.8f - forward * 2.75f, city);
            moved += MoveNamed(root, "V41 abandoned service sedan", c - right * 9.2f + forward * 5.5f, city);

            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)
                         .Where(x => x.name == "V41 warm battery light" && Flat(x.position - c).magnitude < 22f))
                t.gameObject.SetActive(false);
            return moved;
        }

        static int MoveNamed(Transform root, string name, Vector3 pos, Material mat) {
            Transform t = Find(root, name);
            if (!t) return 0;
            t.position = new Vector3(pos.x, .04f, pos.z);
            Ground(t.gameObject, .04f);
            foreach (Renderer r in t.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = mat;
            return 1;
        }

        static int BuildIntentionalNature(Transform parent, List<RosvikOsmV15.Way> ways, Vector3 c, Vector3 right, Vector3 forward,
            Material spruceDark, Material spruceSoft, Material autumn, Material shrubMat) {
            GameObject pine = AssetDatabase.LoadAssetAtPath<GameObject>(NatureRoot + "/tree_pineDefaultA.obj");
            GameObject tall = AssetDatabase.LoadAssetAtPath<GameObject>(NatureRoot + "/tree_pineTallA.obj");
            GameObject fall = AssetDatabase.LoadAssetAtPath<GameObject>(NatureRoot + "/tree_default_fall.obj");
            GameObject bush = AssetDatabase.LoadAssetAtPath<GameObject>(NatureRoot + "/plant_bushDetailed.obj");
            if (!pine && !tall && !fall && !bush) return 0;

            Vector3[] anchors = {
                c - right*12.5f + forward*7.4f,
                c + right*12.0f + forward*7.8f,
                c - right*13.8f - forward*5.2f,
                c + right*13.4f - forward*5.8f,
                c - right*9.8f + forward*11.8f,
                c + right*9.6f + forward*12.3f
            };

            int made = 0;
            for (int i=0; i<anchors.Length; i++) {
                Vector3 p = anchors[i]; p.y=.04f;
                if (!SafeNature(p, ways)) continue;
                GameObject src = i%4==2 && fall ? fall : (i%3==0 && tall ? tall : pine);
                Material m = i%4==2 ? autumn : (i%2==0 ? spruceDark : spruceSoft);
                if (src && PlaceModel(src,parent,"V42 edge tree",p,31f+i*57f,4.9f+(i%3)*.55f,m)) made++;
                if (bush) {
                    Vector3 tangent = i%2==0 ? right : -right;
                    Vector3 b0=p+tangent*1.55f-forward*.40f; b0.y=.04f;
                    Vector3 b1=p-tangent*1.10f+forward*.75f; b1.y=.04f;
                    if (SafeNature(b0,ways) && PlaceModel(bush,parent,"V42 edge shrub",b0,18f+i*43f,.72f,shrubMat)) made++;
                    if (SafeNature(b1,ways) && PlaceModel(bush,parent,"V42 edge shrub",b1,64f+i*29f,.58f,shrubMat)) made++;
                }
            }
            return made;
        }

        static int BuildHeroProps(Transform parent, List<RosvikOsmV15.Way> ways, Vector3 c, Vector3 right, Vector3 forward,
            Material city, Material bulb) {
            GameObject wagon = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot + "/car_stationwagon.obj");
            GameObject bench = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot + "/bench.obj");
            GameObject lamp = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot + "/streetlight.obj");
            GameObject boxA = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot + "/box_A.obj");
            GameObject boxB = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot + "/box_B.obj");
            int made = 0;

            Vector3 car = c + right*10.5f + forward*6.0f; car.y=.04f;
            if (wagon && SafeProp(car,ways) && PlaceModel(wagon,parent,"V42 parked estate car",car,Yaw(forward)+3f,1.48f,city)) made++;

            Vector3 quiet = c - right*6.2f + forward*5.7f; quiet.y=.04f;
            if (bench && SafeProp(quiet,ways) && PlaceModel(bench,parent,"V42 waiting bench",quiet,Yaw(right),.96f,city)) made++;
            if (boxA && SafeProp(quiet-forward*1.25f,ways) && PlaceModel(boxA,parent,"V42 emergency supplies",quiet-forward*1.25f,14f,.55f,city)) made++;
            if (boxB && SafeProp(quiet-forward*1.25f-right*.70f,ways) && PlaceModel(boxB,parent,"V42 emergency supplies",quiet-forward*1.25f-right*.70f,-9f,.43f,city)) made++;

            Vector3 lampPos = c - right*4.6f - forward*2.7f; lampPos.y=.04f;
            if (lamp && PlaceModel(lamp,parent,"V42 battery-lit lamp",lampPos,Yaw(forward),4.9f,city)) {
                made++;
                AddWarmLight(parent,lampPos+Vector3.up*3.55f,8.8f,1.65f,bulb);
            }
            Vector3 other = c + right*4.8f - forward*2.75f; other.y=.04f;
            AddWarmLight(parent,other+Vector3.up*3.55f,8.5f,1.45f,bulb);
            return made;
        }

        static void BuildGroundBreakup(Transform parent, Vector3 c, Vector3 right, Vector3 forward,
            Material frost, Material wet, Material worn) {
            Patch("V42 frost",parent,c+right*5.2f+forward*4.4f,right,3.5f,1.45f,.024f,frost,23,4201);
            Patch("V42 frost",parent,c-right*5.8f+forward*4.9f,right,2.8f,1.15f,.024f,frost,21,4202);
            Patch("V42 frost",parent,c+right*7.0f-forward*2.9f,right,2.6f,1.05f,.024f,frost,19,4203);
            Patch("V42 worn grass",parent,c-right*8.0f+forward*1.8f,right,3.8f,1.35f,.020f,worn,22,4204);
            Patch("V42 thaw",parent,c-right*2.1f+forward*2.6f,right,2.25f,.70f,.030f,wet,20,4205);
            Patch("V42 thaw",parent,c+right*2.9f-forward*.75f,right,1.65f,.55f,.030f,wet,18,4206);

            Transform approach = parent.root.GetComponentsInChildren<Transform>(true)
                .Where(t=>t.name=="entry approach")
                .OrderBy(t=>Flat(t.position-c).sqrMagnitude).FirstOrDefault();
            if (approach) {
                Vector3 ac=approach.position; ac.y=.04f;
                Vector3 side=new Vector3(forward.z,0,-forward.x);
                Patch("V42 path frost",parent,ac+side*1.6f,forward,2.8f,.75f,.023f,frost,20,4211);
                Patch("V42 path frost",parent,ac-side*1.7f,forward,2.4f,.65f,.023f,frost,20,4212);
            }
        }

        static void TuneMood() {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.235f,.265f,.285f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(.315f,.345f,.36f);
            RenderSettings.fogDensity = .00155f;
            RenderSettings.reflectionIntensity = .50f;
            foreach (Light sun in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.type==LightType.Directional)) {
                sun.intensity=.84f;
                sun.color=new Color(.88f,.91f,.97f);
                sun.shadowStrength=.80f;
                sun.shadows=LightShadows.Soft;
            }
        }

        static bool SafeNature(Vector3 p,List<RosvikOsmV15.Way> ways) {
            return !InsideBuilding(p,ways) && !InsidePitch(p,ways) && DistanceToVehicleRoad(p,ways) > 3.0f;
        }
        static bool SafeProp(Vector3 p,List<RosvikOsmV15.Way> ways) {
            return !InsideBuilding(p,ways) && !InsidePitch(p,ways);
        }
        static bool InsideBuilding(Vector3 p,List<RosvikOsmV15.Way> ways) {
            return ways.Any(w=>w.Closed&&!string.IsNullOrEmpty(w.Tag("building"))&&w.Tag("building")!="no"&&Inside(p,Points(w)));
        }
        static bool InsidePitch(Vector3 p,List<RosvikOsmV15.Way> ways) {
            return ways.Any(w=>w.Closed&&(w.Tag("leisure")=="pitch"||w.Tag("sport")=="soccer")&&Inside(p,Points(w)));
        }
        static float DistanceToVehicleRoad(Vector3 p,List<RosvikOsmV15.Way> ways) {
            float best=float.MaxValue;
            foreach(var w in ways) {
                string h=w.Tag("highway");
                if(string.IsNullOrEmpty(h)||h=="footway"||h=="path"||h=="cycleway"||h=="steps"||w.Nodes.Count<2)continue;
                for(int i=0;i<w.Nodes.Count-1;i++)best=Mathf.Min(best,Vector3.Distance(Flat(p),Flat(ClosestPoint(p,w.Nodes[i].Pos,w.Nodes[i+1].Pos))));
            }
            return best;
        }

        static void AddWarmLight(Transform parent,Vector3 pos,float range,float intensity,Material bulb) {
            GameObject glow=new GameObject("V42 warm battery light");glow.transform.SetParent(parent,false);glow.transform.position=pos;
            Light l=glow.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(1f,.60f,.27f);l.range=range;l.intensity=intensity;l.shadows=LightShadows.Soft;
            GameObject orb=GameObject.CreatePrimitive(PrimitiveType.Sphere);orb.name="warm bulb";orb.transform.SetParent(glow.transform,false);orb.transform.localPosition=Vector3.zero;orb.transform.localScale=Vector3.one*.13f;
            orb.GetComponent<Renderer>().sharedMaterial=bulb;UnityEngine.Object.DestroyImmediate(orb.GetComponent<Collider>());
        }

        static void Patch(string name,Transform parent,Vector3 c,Vector3 axis,float length,float width,float y,Material mat,int segments,int seed) {
            axis=Flat(axis).normalized;if(axis.sqrMagnitude<.1f)axis=Vector3.right;Vector3 perp=new Vector3(-axis.z,0,axis.x);
            System.Random rng=new System.Random(seed);GameObject g=new GameObject(name);g.transform.SetParent(parent,false);Mesh mesh=new Mesh{name=name};
            Vector3[] v=new Vector3[segments+1];int[] tri=new int[segments*3];v[0]=new Vector3(c.x,y,c.z);
            for(int i=0;i<segments;i++){
                float a=Mathf.PI*2f*i/segments;float wobble=.82f+(float)rng.NextDouble()*.28f;
                Vector3 p=c+axis*(Mathf.Cos(a)*length*.5f*wobble)+perp*(Mathf.Sin(a)*width*.5f*wobble);v[i+1]=new Vector3(p.x,y,p.z);
                int j=(i+1)%segments;tri[i*3]=0;tri[i*3+1]=i+1;tri[i*3+2]=j+1;
            }
            mesh.vertices=v;mesh.triangles=tri;mesh.RecalculateNormals();mesh.RecalculateBounds();g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=mat;
        }

        static GameObject PlaceModel(GameObject asset,Transform parent,string name,Vector3 pos,float yaw,float targetHeight,Material mat) {
            if(!asset)return null;GameObject g=(GameObject)PrefabUtility.InstantiatePrefab(asset);if(!g)g=UnityEngine.Object.Instantiate(asset);
            g.name=name;g.transform.SetParent(parent,true);g.transform.position=pos;g.transform.rotation=Quaternion.Euler(0,yaw,0);g.transform.localScale=Vector3.one;
            Bounds b=BoundsOf(g);float scale=targetHeight/Mathf.Max(.01f,b.size.y);g.transform.localScale=Vector3.one*scale;Ground(g,.04f);
            foreach(Renderer r in g.GetComponentsInChildren<Renderer>(true)){r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;}
            foreach(Collider col in g.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(col);return g;
        }
        static void Ground(GameObject g,float y){Bounds b=BoundsOf(g);g.transform.position+=Vector3.up*(y-b.min.y);}
        static Bounds BoundsOf(GameObject g){Renderer[] rs=g.GetComponentsInChildren<Renderer>(true);if(rs.Length==0)return new Bounds(g.transform.position,Vector3.one);Bounds b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;}

        static Material TexturedMat(string name,Texture2D tex,float smooth){Material m=Mat(name,Color.white,smooth);if(tex){if(m.HasProperty("_BaseMap"))m.SetTexture("_BaseMap",tex);if(m.HasProperty("_MainTex"))m.SetTexture("_MainTex",tex);EditorUtility.SetDirty(m);}return m;}
        static Material EmissiveMat(string name,Color c,float multiplier){Material m=Mat(name,c,.35f);Color e=c*multiplier;if(m.HasProperty("_EmissionColor")){m.SetColor("_EmissionColor",e);m.EnableKeyword("_EMISSION");}EditorUtility.SetDirty(m);return m;}
        static Material Mat(string name,Color color,float smooth){string path=GeneratedDir+"/mat_"+name+".mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(path);Shader s=ResolveShader();if(!m){m=new Material(s){name="V42 "+name};AssetDatabase.CreateAsset(m,path);}m.shader=s;m.color=color;if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color);if(m.HasProperty("_Color"))m.SetColor("_Color",color);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",smooth);if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",0f);EditorUtility.SetDirty(m);return m;}
        static Shader ResolveShader(){Material p=AssetDatabase.LoadAssetAtPath<Material>(ProvenMaterial);if(p&&p.shader&&p.shader.isSupported)return p.shader;Shader s=GraphicsSettings.defaultRenderPipeline!=null?Shader.Find("Universal Render Pipeline/Lit"):Shader.Find("Standard");if(!s||!s.isSupported)s=Shader.Find("Sprites/Default");return s;}

        static bool HasAncestorNamed(Transform t,IEnumerable<string> names){HashSet<string> set=new HashSet<string>(names);for(Transform p=t.parent;p!=null;p=p.parent)if(set.Contains(p.name))return true;return false;}
        static List<Vector3> Points(RosvikOsmV15.Way w){List<Vector3> p=w.Nodes.Select(n=>n.Pos).ToList();if(p.Count>2&&w.Closed&&Vector3.Distance(p[0],p[p.Count-1])<.01f)p.RemoveAt(p.Count-1);return p;}
        static bool Inside(Vector3 p,List<Vector3> poly){if(poly==null||poly.Count<3)return false;bool inside=false;for(int i=0,j=poly.Count-1;i<poly.Count;j=i++){Vector3 a=poly[i],b=poly[j];if(((a.z>p.z)!=(b.z>p.z))&&(p.x<(b.x-a.x)*(p.z-a.z)/(b.z-a.z+.000001f)+a.x))inside=!inside;}return inside;}
        static Vector3 ClosestPoint(Vector3 p,Vector3 a,Vector3 b){Vector3 ab=Flat(b-a);float d=ab.sqrMagnitude;if(d<.0001f)return a;float t=Mathf.Clamp01(Vector3.Dot(Flat(p-a),ab)/d);return a+ab*t;}
        static float Yaw(Vector3 d)=>Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;
        static Vector3 Flat(Vector3 v)=>new Vector3(v.x,0f,v.z);
        static Transform NewGroup(Transform p,string name){GameObject g=new GameObject(name);g.transform.SetParent(p,false);return g.transform;}
        static Transform Find(Transform root,string name)=>root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name.Equals(name,StringComparison.OrdinalIgnoreCase));
    }
}
#endif