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
    /// V38 expands detail beyond the school bubble after the V37 road cleanup.
    /// Existing OSM geography, gameplay, controls and cleaned roads stay locked.
    /// The outer village receives lightweight house facades/roofs, roadside power poles,
    /// a few believable parked/abandoned cars and a broader northern vegetation layer.
    /// </summary>
    [InitializeOnLoad]
    public static class RosvikVillageFabricV38 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_VILLAGE_FABRIC_V38_VERSION";
        const int Version = 38;
        const string RequiredRoot = "ROSVIK_ROAD_CLEANUP_V37";
        const string RootName = "ROSVIK_VILLAGE_FABRIC_V38";
        const string GroupName = "20 OUTER VILLAGE FABRIC V38";
        const string GeneratedDir = "Assets/Rosvik/GeneratedV38";
        const string ProvenMaterial = "Assets/Rosvik/GeneratedV20/mat_ground.mat";
        const string NatureRoot = "Assets/Rosvik/ThirdParty/V22Models/KenneyNature";
        const string TownRoot = "Assets/Rosvik/ThirdParty/V32TownBits";
        const long MainSchoolWay = 163199458;
        const long OldSchoolWay = 163199461;
        const long ArenaWay = 163199454;

        struct RoadSample {
            public Vector3 point;
            public Vector3 dir;
            public RosvikOsmV15.Way way;
            public float distance;
        }

        static RosvikVillageFabricV38() {
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        [MenuItem("Rosvik/Rebuild Outer Village Fabric V38")]
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
            GameObject root = GameObject.Find(RequiredRoot) ?? GameObject.Find(RootName);
            if (!root) {
                if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                if (Busy()) return;
                root = GameObject.Find(RequiredRoot) ?? GameObject.Find(RootName);
            }
            if (!root || (root.name != RequiredRoot && root.name != RootName) || Busy()) return;

            EditorApplication.update -= TryApply;
            Apply(scene, root);
        }

        static void Apply(UScene scene, GameObject root) {
            try {
                Directory.CreateDirectory(GeneratedDir);
                AssetDatabase.Refresh();

                Transform old = Find(root.transform, GroupName);
                if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
                Transform group = NewGroup(root.transform, GroupName);

                List<RosvikOsmV15.Way> ways = RosvikOsmV15.LoadWays();
                if (ways == null || ways.Count == 0) throw new InvalidOperationException("V38 could not load Rosvik OSM data.");
                RosvikOsmV15.Way schoolWay = ways.FirstOrDefault(w => w.Id == MainSchoolWay);
                if (schoolWay == null) throw new InvalidOperationException("V38 could not locate Rosviks skola.");
                Vector3 school = RosvikOsmV15.Centroid(schoolWay);

                Material glass = Mat("outer_glass", new Color(.055f,.105f,.12f), .66f);
                Material trim = Mat("outer_trim", new Color(.72f,.71f,.64f), .14f);
                Material foundation = Mat("outer_foundation", new Color(.15f,.16f,.15f), .11f);
                Material doorA = Mat("outer_door_red", new Color(.33f,.10f,.075f), .18f);
                Material doorB = Mat("outer_door_blue", new Color(.09f,.17f,.21f), .19f);
                Material roofA = Mat("outer_roof_red", new Color(.25f,.105f,.075f), .18f);
                Material roofB = Mat("outer_roof_dark", new Color(.105f,.115f,.11f), .22f);
                Material roofC = Mat("outer_roof_brown", new Color(.20f,.14f,.09f), .14f);
                Material metal = Mat("outer_metal", new Color(.18f,.195f,.19f), .28f);
                Material wood = Mat("powerpole_wood", new Color(.235f,.155f,.085f), .06f);
                Material cable = Mat("power_cable", new Color(.035f,.038f,.035f), .10f);
                Material insulator = Mat("pole_insulator", new Color(.30f,.34f,.31f), .30f);
                Material spruceA = Mat("outer_spruce_dark", new Color(.05f,.125f,.07f), .03f);
                Material spruceB = Mat("outer_spruce_soft", new Color(.085f,.18f,.095f), .03f);
                Material autumn = Mat("outer_autumn", new Color(.36f,.285f,.095f), .03f);
                Material shrub = Mat("outer_shrub", new Color(.145f,.225f,.095f), .03f);
                Material carBlue = Mat("outer_car_blue", new Color(.10f,.17f,.19f), .30f);
                Material carGrey = Mat("outer_car_grey", new Color(.24f,.245f,.225f), .28f);
                Material carRust = Mat("outer_car_rust", new Color(.31f,.12f,.075f), .22f);

                int houses = BuildOuterHouses(group, root.transform, ways, school, glass, trim, foundation,
                    new[] { doorA, doorB }, new[] { roofA, roofB, roofC }, metal);
                int poles = BuildRoadsidePower(group, ways, school, wood, cable, insulator);
                int vegetation = BuildOuterVegetation(group, root.transform, ways, school, spruceA, spruceB, autumn, shrub);
                int cars = BuildOuterCars(group, root.transform, ways, school, carBlue, carGrey, carRust);

                root.name = RootName;
                EditorPrefs.SetInt(Key, Version);
                Selection.activeObject = null;
                EditorUtility.SetDirty(root);
                AssetDatabase.SaveAssets();
                if (!Busy()) {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene, ScenePath);
                }
                Debug.Log("ROSVIK V38: expanded village fabric with " + houses + " outer houses, " + poles +
                          " roadside power poles, " + vegetation + " outer vegetation pieces and " + cars +
                          " restrained vehicles. V37 roads/geography/gameplay unchanged.");
            } catch (Exception ex) {
                Debug.LogError("ROSVIK V38 FAILED: " + ex);
            }
        }

        static int BuildOuterHouses(Transform parent, Transform root, List<RosvikOsmV15.Way> ways, Vector3 school,
            Material glass, Material trim, Material foundation, Material[] doors, Material[] roofs, Material metal) {

            List<RosvikOsmV15.Way> roads = ways.Where(IsVehicleRoad).ToList();
            List<RosvikOsmV15.Way> candidates = ways.Where(IsHouse)
                .Where(w => w.Id != MainSchoolWay && w.Id != OldSchoolWay && w.Id != ArenaWay)
                .OrderBy(w => Vector3.Distance(Flat(RosvikOsmV15.Centroid(w)), Flat(school)))
                .ToList();

            int made = 0;
            foreach (RosvikOsmV15.Way w in candidates) {
                if (made >= 28) break;
                float dist = Vector3.Distance(Flat(RosvikOsmV15.Centroid(w)), Flat(school));
                if (dist < 105f || dist > 235f) continue;
                if (Find(root, "house detail " + w.Id) || Find(root, "outer house detail " + w.Id)) continue;

                List<Vector3> pts = Points(w);
                if (pts.Count < 3) continue;
                Transform h = NewGroup(parent, "outer house detail " + w.Id);
                float area = SignedArea(pts);

                int front = 0;
                float best = float.MaxValue;
                for (int i = 0; i < pts.Count; i++) {
                    Vector3 mid = (pts[i] + pts[(i + 1) % pts.Count]) * .5f;
                    float d = DistanceToRoad(mid, roads);
                    if (d < best) { best = d; front = i; }
                }

                int seed = unchecked((int)(w.Id & 0x7fffffff));
                Material door = doors[Math.Abs(seed % doors.Length)];
                for (int i = 0; i < pts.Count; i++) {
                    Vector3 a = pts[i], b = pts[(i + 1) % pts.Count];
                    Vector3 dir = Flat(b - a); float len = dir.magnitude;
                    if (len < 2.2f) continue;
                    dir /= len;
                    Vector3 left = new Vector3(-dir.z, 0f, dir.x);
                    Vector3 outward = area > 0f ? -left : left;
                    Vector3 center = (a + b) * .5f;

                    AddPanel("outer foundation", h, center + outward*.045f + Vector3.up*.20f, outward,
                        new Vector3(Mathf.Max(.5f,len-.12f), .32f, .055f), foundation);

                    int count = Mathf.Clamp(Mathf.FloorToInt(len / 3.2f), 1, 5);
                    float margin = Mathf.Min(1.0f, len*.14f);
                    float usable = Mathf.Max(.5f, len - margin*2f);
                    float spacing = usable / count;
                    for (int k = 0; k < count; k++) {
                        float along = -len*.5f + margin + spacing*(k+.5f);
                        if (i == front && Mathf.Abs(along) < 1.0f) continue;
                        float width = Mathf.Clamp(spacing*.46f,.70f,1.12f);
                        AddWindow(h, center + dir*along, outward, 1.58f, width, .88f, trim, glass);
                    }

                    if (i == front) {
                        AddPanel("outer front door frame", h, center + outward*.07f + Vector3.up*1.10f, outward,
                            new Vector3(1.15f,2.16f,.085f), trim);
                        AddPanel("outer front door", h, center + outward*.12f + Vector3.up*1.08f, outward,
                            new Vector3(.92f,1.96f,.05f), door);
                        AddPanel("outer porch canopy", h, center + outward*.44f + Vector3.up*2.22f, outward,
                            new Vector3(1.48f,.09f,.78f), trim);
                    }
                }

                if (!Find(root, "pitched roof " + w.Id) && RectangularEnough(w, pts)) {
                    RosvikOsmV15.OBounds ob = RosvikOsmV15.Bounds(w);
                    Vector3 axis = ob.AxisX.sqrMagnitude > .01f ? ob.AxisX.normalized : Vector3.right;
                    Vector3 perp = new Vector3(-axis.z,0f,axis.x).normalized;
                    float length = ob.Width, depth = ob.Depth;
                    if (depth > length) { float t=length; length=depth; depth=t; Vector3 old=axis; axis=perp; perp=-old; }
                    if (length >= 5f && depth >= 4f && length <= 28f && depth <= 18f) {
                        float rise = Mathf.Clamp(depth*.23f,.72f,1.58f);
                        BuildGableRoof(h, ob.Center, axis, perp, length+.58f, depth+.58f, 2.98f, rise,
                            roofs[Math.Abs(seed % roofs.Length)], metal);
                    }
                }
                made++;
            }
            return made;
        }

        static int BuildRoadsidePower(Transform parent, List<RosvikOsmV15.Way> ways, Vector3 school,
            Material wood, Material cable, Material insulator) {

            List<RosvikOsmV15.Way> roads = ways.Where(w => IsVehicleRoad(w) && IsAsphaltRoad(w)).ToList();
            int made = 0;
            List<Vector3> global = new List<Vector3>();

            foreach (RosvikOsmV15.Way w in roads.OrderBy(w => DistanceWayToPoint(w, school))) {
                if (made >= 20) break;
                Vector3 previousTop = Vector3.zero;
                bool havePrevious = false;
                int local = 0;
                for (int i=0; i<w.Nodes.Count-1 && made<20; i++) {
                    Vector3 a=w.Nodes[i].Pos, b=w.Nodes[i+1].Pos;
                    Vector3 dir=Flat(b-a); float len=dir.magnitude;
                    if (len < 8f) continue;
                    dir/=len;
                    Vector3 mid=(a+b)*.5f;
                    float d=Vector3.Distance(Flat(mid),Flat(school));
                    if (d < 88f || d > 235f) continue;
                    if (global.Any(p => Vector3.Distance(Flat(p),Flat(mid)) < 23f)) continue;

                    float sign=((w.Id + i) & 1) == 0 ? 1f : -1f;
                    Vector3 side=new Vector3(-dir.z,0f,dir.x)*sign;
                    Vector3 p=mid + side*3.85f; p.y=.035f;
                    if (!SafeRoadside(p,ways,2.2f)) continue;

                    Transform pole = NewGroup(parent, "v38 power pole");
                    BuildPowerPole(pole,p,dir,wood,insulator);
                    Vector3 top=p+Vector3.up*4.55f;
                    if (havePrevious) {
                        float span=Vector3.Distance(Flat(previousTop),Flat(top));
                        if (span > 12f && span < 42f) Beam("overhead cable",parent,previousTop,top,.028f,cable);
                    }
                    previousTop=top; havePrevious=true;
                    global.Add(p); made++; local++;
                    if (local >= 5) break;
                }
            }
            return made;
        }

        static void BuildPowerPole(Transform parent, Vector3 pos, Vector3 roadDir, Material wood, Material insulator) {
            Cylinder("wood utility pole",parent,pos+Vector3.up*2.30f,.11f,4.60f,wood);
            Vector3 across=new Vector3(-roadDir.z,0f,roadDir.x).normalized;
            Quaternion rot=Quaternion.FromToRotation(Vector3.right,across);
            Box("crossarm",parent,pos+Vector3.up*4.47f,new Vector3(1.42f,.10f,.10f),rot,wood);
            for (int i=-1;i<=1;i++) {
                Vector3 q=pos+across*(i*.48f)+Vector3.up*4.60f;
                Sphere("insulator",parent,q,new Vector3(.10f,.12f,.10f),insulator);
            }
        }

        static int BuildOuterVegetation(Transform parent, Transform root, List<RosvikOsmV15.Way> ways, Vector3 school,
            Material spruceA, Material spruceB, Material autumn, Material shrub) {

            GameObject pine=AssetDatabase.LoadAssetAtPath<GameObject>(NatureRoot+"/tree_pineDefaultA.obj");
            GameObject tall=AssetDatabase.LoadAssetAtPath<GameObject>(NatureRoot+"/tree_pineTallA.obj");
            GameObject fall=AssetDatabase.LoadAssetAtPath<GameObject>(NatureRoot+"/tree_default_fall.obj");
            GameObject bush=AssetDatabase.LoadAssetAtPath<GameObject>(NatureRoot+"/plant_bushDetailed.obj");
            if (!pine && !fall && !bush) return 0;

            List<Vector3> existing=root.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.IndexOf("tree",StringComparison.OrdinalIgnoreCase)>=0 && t.GetComponentInChildren<Renderer>(true)!=null)
                .Select(t=>t.position).ToList();
            System.Random rng=new System.Random(3807);
            int trees=0,bushes=0;

            for (int attempt=0; attempt<700 && trees<34; attempt++) {
                float a=(float)rng.NextDouble()*Mathf.PI*2f;
                float r=110f+(float)rng.NextDouble()*125f;
                Vector3 p=school+new Vector3(Mathf.Cos(a)*r,.035f,Mathf.Sin(a)*r);
                if (!SafeNature(p,ways,4.1f) || existing.Any(q=>Vector3.Distance(Flat(q),Flat(p))<5.0f)) continue;
                GameObject src; Material mat;
                if (trees%7==0 && fall) { src=fall; mat=autumn; }
                else if (trees%3==0 && tall) { src=tall; mat=spruceA; }
                else { src=pine ? pine : fall; mat=(trees%2==0)?spruceA:spruceB; }
                if (!src) continue;
                PlaceModel(src,parent,"v38 outer tree",p,(float)rng.NextDouble()*360f,Mathf.Lerp(4.8f,7.6f,(float)rng.NextDouble()),mat);
                existing.Add(p); trees++;
            }

            if (bush) {
                for (int attempt=0; attempt<650 && bushes<38; attempt++) {
                    float a=(float)rng.NextDouble()*Mathf.PI*2f;
                    float r=100f+(float)rng.NextDouble()*135f;
                    Vector3 p=school+new Vector3(Mathf.Cos(a)*r,.03f,Mathf.Sin(a)*r);
                    if (!SafeNature(p,ways,2.2f)) continue;
                    PlaceModel(bush,parent,"v38 outer shrub",p,(float)rng.NextDouble()*360f,Mathf.Lerp(.48f,1.05f,(float)rng.NextDouble()),shrub);
                    bushes++;
                }
            }
            return trees+bushes;
        }

        static int BuildOuterCars(Transform parent, Transform root, List<RosvikOsmV15.Way> ways, Vector3 school,
            Material blue, Material grey, Material rust) {

            GameObject sedan=AssetDatabase.LoadAssetAtPath<GameObject>(TownRoot+"/car_sedan.obj");
            GameObject wagon=AssetDatabase.LoadAssetAtPath<GameObject>(TownRoot+"/car_stationwagon.obj");
            if (!sedan && !wagon) return 0;

            List<Vector3> existing=root.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.IndexOf("car",StringComparison.OrdinalIgnoreCase)>=0 && t.GetComponentInChildren<Renderer>(true)!=null)
                .Select(t=>t.position).ToList();
            List<RoadSample> samples=RoadSamples(ways,school)
                .Where(s=>s.distance>=120f && s.distance<=235f && IsAsphaltRoad(s.way))
                .OrderBy(s=>s.distance).ToList();

            int made=0;
            foreach (RoadSample s in samples) {
                if (made>=7) break;
                float sign=(made%2==0)?1f:-1f;
                Vector3 side=new Vector3(-s.dir.z,0f,s.dir.x)*sign;
                Vector3 p=s.point+side*3.25f; p.y=.045f;
                if (!SafeRoadside(p,ways,1.2f) || existing.Any(q=>Vector3.Distance(Flat(q),Flat(p))<18f)) continue;
                GameObject src=(made%2==0 && wagon)?wagon:(sedan?sedan:wagon);
                Material mat=(made==3||made==6)?rust:(made%2==0?grey:blue);
                float yaw=Yaw(s.dir) + ((made==3)?8f:0f);
                PlaceModel(src,parent,(made==3?"slightly abandoned car":"outer parked car"),p,yaw,1.48f,mat);
                existing.Add(p); made++;
            }
            return made;
        }

        static List<RoadSample> RoadSamples(List<RosvikOsmV15.Way> ways, Vector3 school) {
            List<RoadSample> result=new List<RoadSample>();
            foreach (RosvikOsmV15.Way w in ways.Where(IsVehicleRoad)) {
                for (int i=0;i<w.Nodes.Count-1;i++) {
                    Vector3 a=w.Nodes[i].Pos,b=w.Nodes[i+1].Pos;
                    Vector3 dir=Flat(b-a); if (dir.sqrMagnitude<.01f) continue;
                    Vector3 p=(a+b)*.5f;
                    result.Add(new RoadSample{point=p,dir=dir.normalized,way=w,distance=Vector3.Distance(Flat(p),Flat(school))});
                }
            }
            return result;
        }

        static bool IsHouse(RosvikOsmV15.Way w) {
            if (!w.Closed || w.Nodes.Count<4) return false;
            string b=w.Tag("building");
            if (string.IsNullOrEmpty(b)||b=="no"||b=="garage"||b=="shed"||b=="carport"||b=="roof"||b=="apartments"||b=="terrace") return false;
            if (b=="house"||b=="residential"||b=="detached"||b=="semidetached_house"||b=="bungalow") return true;
            if (b=="yes") {
                RosvikOsmV15.OBounds ob=RosvikOsmV15.Bounds(w);
                float proxy=ob.Width*ob.Depth;
                return proxy>=35f && proxy<=420f;
            }
            return false;
        }

        static bool IsVehicleRoad(RosvikOsmV15.Way w) {
            string h=w.Tag("highway");
            return (h=="residential"||h=="unclassified"||h=="tertiary"||h=="service"||h=="living_street") && w.Nodes.Count>=2;
        }

        static bool IsAsphaltRoad(RosvikOsmV15.Way w) {
            string h=w.Tag("highway");
            return h=="residential"||h=="unclassified"||h=="tertiary";
        }

        static bool RectangularEnough(RosvikOsmV15.Way w,List<Vector3> pts) {
            if (pts==null||pts.Count<4||pts.Count>8) return false;
            RosvikOsmV15.OBounds ob=RosvikOsmV15.Bounds(w);
            float boxArea=Mathf.Max(.01f,ob.Width*ob.Depth);
            float polyArea=Mathf.Abs(SignedArea(pts));
            float fill=polyArea/boxArea;
            if (fill<.79f||fill>1.06f) return false;
            int shortEdges=0;
            for(int i=0;i<pts.Count;i++) if(Flat(pts[(i+1)%pts.Count]-pts[i]).magnitude<1.45f) shortEdges++;
            return shortEdges<=1;
        }

        static void BuildGableRoof(Transform parent,Vector3 center,Vector3 axis,Vector3 perp,float length,float depth,float baseY,float rise,Material roof,Material metal) {
            axis=Flat(axis).normalized; perp=Flat(perp).normalized;
            Vector3 c=new Vector3(center.x,0f,center.z); float hx=length*.5f,hz=depth*.5f;
            Vector3[] v={
                c-axis*hx-perp*hz+Vector3.up*baseY,
                c+axis*hx-perp*hz+Vector3.up*baseY,
                c-axis*hx+Vector3.up*(baseY+rise),
                c+axis*hx+Vector3.up*(baseY+rise),
                c-axis*hx+perp*hz+Vector3.up*baseY,
                c+axis*hx+perp*hz+Vector3.up*baseY
            };
            int[] tri={0,2,1,1,2,3,2,4,3,3,4,5,0,4,2,1,3,5};
            Mesh mesh=new Mesh{name="V38 outer gable roof",vertices=v,triangles=tri}; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            GameObject shell=new GameObject("outer pitched roof shell"); shell.transform.SetParent(parent,false);
            shell.AddComponent<MeshFilter>().sharedMesh=mesh; shell.AddComponent<MeshRenderer>().sharedMaterial=roof;
            Quaternion along=Quaternion.FromToRotation(Vector3.right,axis);
            Box("outer ridge cap",parent,c+Vector3.up*(baseY+rise+.03f),new Vector3(length+.06f,.07f,.09f),along,metal);
        }

        static void AddWindow(Transform parent,Vector3 pos,Vector3 outward,float y,float width,float height,Material trim,Material glass) {
            AddPanel("outer window frame",parent,pos+outward*.06f+Vector3.up*y,outward,new Vector3(width+.15f,height+.15f,.068f),trim);
            AddPanel("outer window glass",parent,pos+outward*.105f+Vector3.up*y,outward,new Vector3(width,height,.038f),glass);
            AddPanel("outer window sill",parent,pos+outward*.13f+Vector3.up*(y-height*.55f),outward,new Vector3(width+.20f,.07f,.14f),trim);
            AddPanel("outer window mullion",parent,pos+outward*.14f+Vector3.up*y,outward,new Vector3(.04f,height,.023f),trim);
        }

        static void AddPanel(string name,Transform parent,Vector3 pos,Vector3 outward,Vector3 scale,Material mat) {
            GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name; g.transform.SetParent(parent,false);
            g.transform.position=pos; g.transform.rotation=Quaternion.LookRotation(outward,Vector3.up); g.transform.localScale=scale;
            g.GetComponent<Renderer>().sharedMaterial=mat; UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        }

        static bool SafeRoadside(Vector3 p,List<RosvikOsmV15.Way> ways,float buildingClearance) {
            if (InsideBuilding(p,ways)||InsidePitch(p,ways)) return false;
            return DistanceToBuildings(p,ways)>buildingClearance;
        }

        static bool SafeNature(Vector3 p,List<RosvikOsmV15.Way> ways,float roadClearance) {
            if (InsideBuilding(p,ways)||InsidePitch(p,ways)) return false;
            return DistanceToRoad(p,ways)>roadClearance;
        }

        static bool InsideBuilding(Vector3 p,List<RosvikOsmV15.Way> ways) {
            foreach(var w in ways) {
                string b=w.Tag("building");
                if(!w.Closed||string.IsNullOrEmpty(b)||b=="no") continue;
                if(Inside(p,Points(w))) return true;
            }
            return false;
        }

        static bool InsidePitch(Vector3 p,List<RosvikOsmV15.Way> ways) {
            foreach(var w in ways) {
                if(!w.Closed) continue;
                if(w.Tag("leisure")!="pitch"&&w.Tag("sport")!="soccer") continue;
                if(Inside(p,Points(w))) return true;
            }
            return false;
        }

        static float DistanceToBuildings(Vector3 p,List<RosvikOsmV15.Way> ways) {
            float best=float.MaxValue;
            foreach(var w in ways) {
                string b=w.Tag("building"); if(!w.Closed||string.IsNullOrEmpty(b)||b=="no") continue;
                List<Vector3> pts=Points(w);
                for(int i=0;i<pts.Count;i++) best=Mathf.Min(best,Vector3.Distance(Flat(p),Flat(ClosestPoint(p,pts[i],pts[(i+1)%pts.Count]))));
            }
            return best;
        }

        static float DistanceToRoad(Vector3 p,List<RosvikOsmV15.Way> roads) {
            float best=float.MaxValue;
            foreach(var w in roads) {
                string h=w.Tag("highway"); if(string.IsNullOrEmpty(h)||w.Nodes.Count<2) continue;
                for(int i=0;i<w.Nodes.Count-1;i++) best=Mathf.Min(best,Vector3.Distance(Flat(p),Flat(ClosestPoint(p,w.Nodes[i].Pos,w.Nodes[i+1].Pos))));
            }
            return best;
        }

        static float DistanceWayToPoint(RosvikOsmV15.Way w,Vector3 p) {
            float best=float.MaxValue;
            for(int i=0;i<w.Nodes.Count-1;i++) best=Mathf.Min(best,Vector3.Distance(Flat(p),Flat(ClosestPoint(p,w.Nodes[i].Pos,w.Nodes[i+1].Pos))));
            return best;
        }

        static bool Inside(Vector3 p,List<Vector3> poly) {
            if(poly==null||poly.Count<3) return false; bool inside=false; int j=poly.Count-1;
            for(int i=0;i<poly.Count;i++) {
                float xi=poly[i].x,zi=poly[i].z,xj=poly[j].x,zj=poly[j].z;
                bool hit=((zi>p.z)!=(zj>p.z))&&(p.x<(xj-xi)*(p.z-zi)/(zj-zi+.00001f)+xi);
                if(hit) inside=!inside; j=i;
            }
            return inside;
        }

        static Vector3 ClosestPoint(Vector3 p,Vector3 a,Vector3 b) {
            Vector3 ab=Flat(b-a); float den=ab.sqrMagnitude; if(den<.0001f) return a;
            float t=Mathf.Clamp01(Vector3.Dot(Flat(p-a),ab)/den); return a+ab*t;
        }

        static GameObject PlaceModel(GameObject asset,Transform parent,string name,Vector3 pos,float yaw,float targetHeight,Material mat) {
            if(!asset) return null;
            GameObject go=(GameObject)PrefabUtility.InstantiatePrefab(asset,parent);
            if(!go) go=UnityEngine.Object.Instantiate(asset,parent);
            go.name=name; go.transform.position=pos; go.transform.rotation=Quaternion.Euler(0f,yaw,0f); go.transform.localScale=Vector3.one;
            Bounds b=BoundsOf(go); float h=Mathf.Max(.01f,b.size.y); go.transform.localScale=Vector3.one*(targetHeight/h);
            foreach(Renderer r in go.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial=mat;
            foreach(Collider c in go.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
            Bounds after=BoundsOf(go); Vector3 p=go.transform.position; p.y += .035f-after.min.y; go.transform.position=p;
            return go;
        }

        static Bounds BoundsOf(GameObject go) {
            Renderer[] rs=go.GetComponentsInChildren<Renderer>(true);
            if(rs.Length==0) return new Bounds(go.transform.position,Vector3.zero);
            Bounds b=rs[0].bounds; for(int i=1;i<rs.Length;i++) b.Encapsulate(rs[i].bounds); return b;
        }

        static void Beam(string name,Transform parent,Vector3 a,Vector3 b,float thickness,Material mat) {
            Vector3 d=b-a; float len=d.magnitude; if(len<.02f) return;
            GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name; g.transform.SetParent(parent,false);
            g.transform.position=(a+b)*.5f; g.transform.rotation=Quaternion.FromToRotation(Vector3.right,d.normalized);
            g.transform.localScale=new Vector3(len,thickness,thickness); g.GetComponent<Renderer>().sharedMaterial=mat;
            UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        }

        static void Box(string name,Transform parent,Vector3 pos,Vector3 scale,Quaternion rot,Material mat) {
            GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name; g.transform.SetParent(parent,false);
            g.transform.SetPositionAndRotation(pos,rot); g.transform.localScale=scale; g.GetComponent<Renderer>().sharedMaterial=mat;
            UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        }

        static void Cylinder(string name,Transform parent,Vector3 pos,float radius,float height,Material mat) {
            GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cylinder); g.name=name; g.transform.SetParent(parent,false);
            g.transform.position=pos; g.transform.localScale=new Vector3(radius*2f,height*.5f,radius*2f); g.GetComponent<Renderer>().sharedMaterial=mat;
            UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        }

        static void Sphere(string name,Transform parent,Vector3 pos,Vector3 scale,Material mat) {
            GameObject g=GameObject.CreatePrimitive(PrimitiveType.Sphere); g.name=name; g.transform.SetParent(parent,false);
            g.transform.position=pos; g.transform.localScale=scale; g.GetComponent<Renderer>().sharedMaterial=mat;
            UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        }

        static Material Mat(string name,Color color,float smoothness) {
            string path=GeneratedDir+"/mat_"+name+".mat"; Material m=AssetDatabase.LoadAssetAtPath<Material>(path); Shader s=ResolveShader();
            if(!m){m=new Material(s){name="V38 "+name};AssetDatabase.CreateAsset(m,path);} m.shader=s; m.color=color;
            if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color); if(m.HasProperty("_Color"))m.SetColor("_Color",color);
            if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smoothness); if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",smoothness);
            if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",0f); EditorUtility.SetDirty(m); return m;
        }

        static Shader ResolveShader() {
            Material proven=AssetDatabase.LoadAssetAtPath<Material>(ProvenMaterial);
            if(proven&&proven.shader&&proven.shader.isSupported) return proven.shader;
            Shader s=GraphicsSettings.defaultRenderPipeline!=null?Shader.Find("Universal Render Pipeline/Lit"):Shader.Find("Standard");
            if(!s||!s.isSupported)s=Shader.Find("Sprites/Default"); return s;
        }

        static float Yaw(Vector3 dir) { return Mathf.Atan2(dir.x,dir.z)*Mathf.Rad2Deg; }
        static List<Vector3> Points(RosvikOsmV15.Way w){List<Vector3> p=w.Nodes.Select(n=>n.Pos).ToList();if(p.Count>2&&w.Closed)p.RemoveAt(p.Count-1);return p;}
        static float SignedArea(List<Vector3> p){float a=0f;for(int i=0;i<p.Count;i++){Vector3 q=p[(i+1)%p.Count];a+=p[i].x*q.z-q.x*p[i].z;}return a*.5f;}
        static Vector3 Flat(Vector3 v){v.y=0f;return v;}
        static Transform NewGroup(Transform p,string n){Transform t=new GameObject(n).transform;t.SetParent(p,false);return t;}
        static Transform Find(Transform root,string n){if(!root)return null;if(root.name==n)return root;foreach(Transform t in root.GetComponentsInChildren<Transform>(true))if(t.name==n)return t;return null;}
    }
}
#endif