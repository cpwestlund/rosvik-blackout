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
    /// V36 grows the approved V35 housing pass outward into the yards without inventing
    /// cadastral parcel lines. It uses mapped garage/shed/carport footprints and nearby road
    /// geometry to add garage facades, gravel aprons/driveways, modest entrance walks,
    /// mailboxes, bins and small hedge clusters. Geography, gameplay, controls and V35 roofs
    /// remain untouched.
    /// </summary>
    [InitializeOnLoad]
    public static class RosvikResidentialGroundsV36 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_RESIDENTIAL_GROUNDS_V36_VERSION";
        const int Version = 36;
        const string RequiredRoot = "ROSVIK_ROOF_SILHOUETTE_V35";
        const string RootName = "ROSVIK_RESIDENTIAL_GROUNDS_V36";
        const string GroupName = "18 RESIDENTIAL GROUNDS V36";
        const string GeneratedDir = "Assets/Rosvik/GeneratedV36";
        const string ProvenMaterial = "Assets/Rosvik/GeneratedV20/mat_ground.mat";
        const string TownBits = "Assets/Rosvik/ThirdParty/V32TownBits";
        const string NatureBits = "Assets/Rosvik/ThirdParty/V22Models/KenneyNature";
        const long MainSchoolWay = 163199458;

        struct RoadHit {
            public bool valid;
            public Vector3 point;
            public Vector3 dir;
            public float distance;
        }

        static RosvikResidentialGroundsV36() {
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        [MenuItem("Rosvik/Rebuild Residential Grounds V36")]
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
            // Deliberately wait for V35 rather than racing an older world pass.
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
                if (ways == null || ways.Count == 0) throw new InvalidOperationException("V36 could not load Rosvik OSM data.");
                RosvikOsmV15.Way schoolWay = ways.FirstOrDefault(w => w.Id == MainSchoolWay);
                if (schoolWay == null) throw new InvalidOperationException("V36 could not locate Rosviks skola.");
                Vector3 school = RosvikOsmV15.Centroid(schoolWay);

                Material gravel = Mat("driveway_gravel", new Color(.235f,.225f,.195f), .10f);
                Material worn = Mat("driveway_worn", new Color(.175f,.17f,.15f), .13f);
                Material concrete = Mat("garage_concrete", new Color(.34f,.34f,.31f), .15f);
                Material garageDoor = Mat("garage_door", new Color(.48f,.485f,.45f), .20f);
                Material garageDark = Mat("garage_dark", new Color(.16f,.17f,.16f), .18f);
                Material glass = Mat("garage_glass", new Color(.06f,.105f,.115f), .57f);
                Material mailbox = Mat("mailbox", new Color(.20f,.225f,.205f), .26f);
                Material mailboxRed = Mat("mailbox_red", new Color(.33f,.105f,.075f), .22f);
                Material hedge = Mat("yard_hedge", new Color(.095f,.19f,.085f), .03f);
                Material timber = Mat("yard_timber", new Color(.30f,.205f,.115f), .09f);
                Material bin = Mat("yard_bin", new Color(.075f,.085f,.075f), .18f);

                GameObject trashModel = AssetDatabase.LoadAssetAtPath<GameObject>(TownBits + "/trash_A.obj");
                GameObject bushModel = AssetDatabase.LoadAssetAtPath<GameObject>(NatureBits + "/plant_bushDetailed.obj");

                List<RosvikOsmV15.Way> roads = ways.Where(IsVehicleRoad).ToList();
                List<RosvikOsmV15.Way> buildings = ways.Where(w => w.Closed && !string.IsNullOrEmpty(w.Tag("building")) && w.Tag("building") != "no").ToList();

                int outbuildings = BuildMappedOutbuildings(group, ways, roads, school, gravel, worn, concrete, garageDoor, garageDark, glass);
                int approaches = BuildHouseApproaches(group, ways, roads, buildings, school, gravel, mailbox, mailboxRed, timber, bin, hedge, trashModel, bushModel);

                root.name = RootName;
                EditorPrefs.SetInt(Key, Version);
                Selection.activeObject = null;
                EditorUtility.SetDirty(root);
                AssetDatabase.SaveAssets();
                if (!Busy()) {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene, ScenePath);
                }
                Debug.Log("ROSVIK V36: " + outbuildings + " mapped outbuildings dressed and " + approaches + " nearby homes received road-aware driveway/entry life. No invented parcel fences; V35 roofs, controls, gameplay and geography unchanged.");
            } catch (Exception ex) {
                Debug.LogError("ROSVIK V36 FAILED: " + ex);
            }
        }

        static int BuildMappedOutbuildings(Transform parent, List<RosvikOsmV15.Way> ways, List<RosvikOsmV15.Way> roads, Vector3 school,
            Material gravel, Material worn, Material concrete, Material garageDoor, Material garageDark, Material glass) {
            int made = 0;
            foreach (RosvikOsmV15.Way w in ways.Where(IsOutbuilding)
                         .OrderBy(w => Vector3.Distance(Flat(RosvikOsmV15.Centroid(w)), Flat(school)))) {
                if (made >= 22) break;
                Vector3 center = RosvikOsmV15.Centroid(w);
                if (Vector3.Distance(Flat(center), Flat(school)) > 125f) continue;
                List<Vector3> pts = Points(w);
                if (pts.Count < 3) continue;

                RoadHit hit = NearestRoad(center, roads);
                RosvikOsmV15.OBounds ob = RosvikOsmV15.Bounds(w);
                float height = w.Tag("building") == "shed" ? 2.05f : 2.25f;
                Transform g = NewGroup(parent, "outbuilding detail " + w.Id);

                int front = EdgeNearestPoint(pts, hit.valid ? hit.point : center + Vector3.forward * 10f);
                Vector3 a = pts[front], b = pts[(front + 1) % pts.Count];
                Vector3 dir = Flat(b - a); float len = dir.magnitude;
                if (len > .5f) {
                    dir /= len;
                    float area = SignedArea(pts);
                    Vector3 left = new Vector3(-dir.z,0f,dir.x);
                    Vector3 outward = area > 0f ? -left : left;
                    Vector3 mid = (a + b) * .5f;

                    AddPanel("outbuilding foundation", g, mid + outward * .05f + Vector3.up * .18f, outward,
                        new Vector3(Mathf.Max(.8f,len-.12f), .28f, .06f), concrete);

                    if (w.Tag("building") == "garage" || w.Tag("building") == "carport") {
                        float doorWidth = Mathf.Clamp(len * .68f, 1.85f, 2.75f);
                        AddPanel("garage door frame", g, mid + outward * .07f + Vector3.up * 1.12f, outward,
                            new Vector3(doorWidth + .16f, 2.02f, .08f), garageDark);
                        AddPanel("garage door", g, mid + outward * .115f + Vector3.up * 1.10f, outward,
                            new Vector3(doorWidth, 1.86f, .045f), garageDoor);
                        for (int k=-2;k<=2;k++)
                            AddPanel("garage slat", g, mid + outward * .145f + Vector3.up * (1.10f + k*.29f), outward,
                                new Vector3(doorWidth*.92f, .028f, .02f), garageDark);
                        if ((w.Id & 1) == 0)
                            AddPanel("garage window", g, mid + dir * (len*.38f) + outward*.12f + Vector3.up*1.35f, outward,
                                new Vector3(.48f,.52f,.04f), glass);
                    } else {
                        AddPanel("shed door", g, mid + outward*.10f + Vector3.up*.98f, outward,
                            new Vector3(.90f,1.78f,.055f), garageDark);
                    }

                    if (hit.valid && hit.distance > 2.0f && hit.distance < 17.5f) {
                        Vector3 start = ClosestPointOnEdge(a,b,hit.point) + outward * .35f;
                        Vector3 end = hit.point;
                        if (SegmentClear(start,end,ways,w.Id)) {
                            Strip("mapped garage driveway", parent, start, end, Mathf.Clamp(doorWidthFor(w,len),2.35f,3.25f), .026f, (w.Id%3==0)?worn:gravel);
                            Disc("driveway apron", parent, start, Mathf.Clamp(doorWidthFor(w,len)*.62f,1.45f,2.0f), .030f, gravel, 16);
                        }
                    }
                }
                made++;
            }
            return made;
        }

        static float doorWidthFor(RosvikOsmV15.Way w, float edgeLen) {
            return w.Tag("building") == "shed" ? Mathf.Clamp(edgeLen*.48f,1.4f,2.1f) : Mathf.Clamp(edgeLen*.70f,2.25f,3.1f);
        }

        static int BuildHouseApproaches(Transform parent, List<RosvikOsmV15.Way> ways, List<RosvikOsmV15.Way> roads,
            List<RosvikOsmV15.Way> buildings, Vector3 school, Material gravel, Material mailbox, Material mailboxRed,
            Material timber, Material bin, Material hedge, GameObject trashModel, GameObject bushModel) {

            List<RosvikOsmV15.Way> houses = ways.Where(IsHouse)
                .OrderBy(w => Vector3.Distance(Flat(RosvikOsmV15.Centroid(w)), Flat(school)))
                .Where(w => Vector3.Distance(Flat(RosvikOsmV15.Centroid(w)), Flat(school)) <= 120f)
                .Take(30).ToList();

            int made = 0;
            foreach (RosvikOsmV15.Way house in houses) {
                List<Vector3> pts = Points(house);
                if (pts.Count < 3) continue;
                Vector3 center = RosvikOsmV15.Centroid(house);
                RoadHit hit = NearestRoad(center, roads);
                if (!hit.valid || hit.distance < 2.4f || hit.distance > 22f) continue;

                int front = EdgeNearestPoint(pts, hit.point);
                Vector3 a=pts[front], b=pts[(front+1)%pts.Count];
                Vector3 edge=Flat(b-a); float len=edge.magnitude;
                if (len<2f) continue; edge/=len;
                float area=SignedArea(pts);
                Vector3 left=new Vector3(-edge.z,0f,edge.x);
                Vector3 outward=area>0f?-left:left;
                Vector3 edgePoint=ClosestPointOnEdge(a,b,hit.point);
                Vector3 start=edgePoint+outward*.65f;
                Vector3 end=hit.point;

                if (!SegmentClear(start,end,buildings,house.Id)) continue;
                float approachLen=Vector3.Distance(Flat(start),Flat(end));
                if (approachLen>3.0f)
                    Strip("house entrance walk", parent, start, end, 1.05f, .025f, gravel);

                // Mailbox sits just inside the road edge, aligned to the road instead of to world axes.
                Vector3 roadSide=new Vector3(-hit.dir.z,0f,hit.dir.x);
                Vector3 mailPos=end + roadSide * (((house.Id&1)==0)?1.15f:-1.15f);
                mailPos.y=.04f;
                BuildMailbox(parent, mailPos, hit.dir, ((house.Id%4)==0)?mailboxRed:mailbox, timber);

                // One restrained yard-service object for some homes, not every house.
                if ((house.Id % 3) == 0) {
                    Vector3 service=start + edge * (((house.Id&2)==0)?1.35f:-1.35f) + outward*.35f;
                    service.y=.035f;
                    if (trashModel)
                        PlaceModel(trashModel,parent,"household bin",service,Yaw(hit.dir),.78f,bin);
                    else
                        BuildBin(parent,service,bin);
                }

                // Small hedge/bush cluster near the entrance corner only. This suggests a cared-for
                // yard without pretending we know the cadastral property boundary.
                if ((house.Id % 2) == 0) {
                    float sign=((house.Id&4)==0)?1f:-1f;
                    Vector3 hp=start + edge*sign*1.75f - outward*.10f;
                    if (bushModel) {
                        PlaceModel(bushModel,parent,"front yard shrub",hp,(float)(house.Id%360),.72f,hedge);
                        if ((house.Id%5)==0) PlaceModel(bushModel,parent,"front yard shrub",hp+edge*.75f,(float)((house.Id+37)%360),.56f,hedge);
                    } else {
                        Box("front hedge",parent,hp+Vector3.up*.36f,new Vector3(1.35f,.68f,.52f),Quaternion.LookRotation(edge,Vector3.up),hedge);
                    }
                }
                made++;
            }
            return made;
        }

        static void BuildMailbox(Transform parent, Vector3 pos, Vector3 roadDir, Material boxMat, Material postMat) {
            Quaternion rot=Quaternion.LookRotation(new Vector3(-roadDir.z,0f,roadDir.x),Vector3.up);
            Box("mailbox post",parent,pos+Vector3.up*.48f,new Vector3(.09f,.92f,.09f),rot,postMat);
            Box("mailbox",parent,pos+Vector3.up*.92f,new Vector3(.46f,.28f,.32f),rot,boxMat);
            Box("mailbox lid",parent,pos+Vector3.up*1.075f,new Vector3(.50f,.045f,.34f),rot,boxMat);
        }

        static void BuildBin(Transform parent, Vector3 pos, Material mat) {
            Box("household bin",parent,pos+Vector3.up*.42f,new Vector3(.45f,.82f,.48f),Quaternion.identity,mat);
            Box("bin lid",parent,pos+Vector3.up*.85f,new Vector3(.49f,.08f,.52f),Quaternion.identity,mat);
        }

        static bool IsHouse(RosvikOsmV15.Way w) {
            if (!w.Closed || w.Nodes.Count < 4) return false;
            string b=w.Tag("building");
            if (b=="house" || b=="residential" || b=="detached" || b=="semidetached_house" || b=="bungalow") return true;
            if (b=="yes") {
                RosvikOsmV15.OBounds ob=RosvikOsmV15.Bounds(w); float area=ob.Width*ob.Depth;
                return area>=42f && area<=330f;
            }
            return false;
        }

        static bool IsOutbuilding(RosvikOsmV15.Way w) {
            if (!w.Closed || w.Nodes.Count<4) return false;
            string b=w.Tag("building");
            return b=="garage" || b=="shed" || b=="carport";
        }

        static bool IsVehicleRoad(RosvikOsmV15.Way w) {
            string h=w.Tag("highway");
            return (h=="residential" || h=="unclassified" || h=="service" || h=="living_street" || h=="tertiary") && w.Nodes.Count>=2;
        }

        static RoadHit NearestRoad(Vector3 p, List<RosvikOsmV15.Way> roads) {
            RoadHit best=new RoadHit{valid=false,distance=float.MaxValue};
            foreach (RosvikOsmV15.Way w in roads) {
                for (int i=0;i<w.Nodes.Count-1;i++) {
                    Vector3 a=w.Nodes[i].Pos,b=w.Nodes[i+1].Pos;
                    Vector3 q=ClosestPoint(p,a,b); float d=Vector3.Distance(Flat(p),Flat(q));
                    if (d>=best.distance) continue;
                    Vector3 dir=Flat(b-a); if (dir.sqrMagnitude<.001f) continue;
                    best=new RoadHit{valid=true,point=q,dir=dir.normalized,distance=d};
                }
            }
            return best;
        }

        static int EdgeNearestPoint(List<Vector3> pts, Vector3 target) {
            int best=0; float bd=float.MaxValue;
            for(int i=0;i<pts.Count;i++) {
                Vector3 q=ClosestPoint(target,pts[i],pts[(i+1)%pts.Count]);
                float d=Vector3.Distance(Flat(q),Flat(target));
                if(d<bd){bd=d;best=i;}
            }
            return best;
        }

        static Vector3 ClosestPointOnEdge(Vector3 a,Vector3 b,Vector3 target){return ClosestPoint(target,a,b);}

        static bool SegmentClear(Vector3 a, Vector3 b, List<RosvikOsmV15.Way> buildings, long ignoreId) {
            for(int s=2;s<=8;s++) {
                float t=s/9f; Vector3 p=Vector3.Lerp(a,b,t);
                foreach(var w in buildings) {
                    if(w.Id==ignoreId || !w.Closed || string.IsNullOrEmpty(w.Tag("building")) || w.Tag("building")=="no") continue;
                    if(Inside(p,Points(w))) return false;
                }
            }
            return true;
        }

        static void AddPanel(string name,Transform parent,Vector3 pos,Vector3 outward,Vector3 scale,Material mat) {
            GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name; g.transform.SetParent(parent,false);
            g.transform.position=pos; g.transform.rotation=Quaternion.LookRotation(outward,Vector3.up); g.transform.localScale=scale;
            g.GetComponent<Renderer>().sharedMaterial=mat; UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        }

        static void Strip(string name,Transform parent,Vector3 a,Vector3 b,float width,float y,Material mat) {
            Vector3 d=Flat(b-a); float len=d.magnitude; if(len<.12f)return;
            Vector3 mid=(a+b)*.5f; mid.y=y;
            GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name; g.transform.SetParent(parent,false);
            g.transform.position=mid; g.transform.rotation=Quaternion.FromToRotation(Vector3.right,d.normalized);
            g.transform.localScale=new Vector3(len,.024f,width); g.GetComponent<Renderer>().sharedMaterial=mat;
            UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        }

        static void Disc(string name,Transform parent,Vector3 center,float radius,float y,Material mat,int segments) {
            segments=Mathf.Max(8,segments); Vector3[] v=new Vector3[segments+1]; int[] tri=new int[segments*3];
            v[0]=new Vector3(center.x,y,center.z);
            for(int i=0;i<segments;i++){float a=i*Mathf.PI*2f/segments;v[i+1]=new Vector3(center.x+Mathf.Cos(a)*radius,y,center.z+Mathf.Sin(a)*radius);int j=(i+1)%segments;tri[i*3]=0;tri[i*3+1]=j+1;tri[i*3+2]=i+1;}
            Mesh m=new Mesh{name=name+" mesh",vertices=v,triangles=tri};m.RecalculateNormals();m.RecalculateBounds();
            GameObject g=new GameObject(name);g.transform.SetParent(parent,false);g.AddComponent<MeshFilter>().sharedMesh=m;g.AddComponent<MeshRenderer>().sharedMaterial=mat;
        }

        static void Box(string name,Transform parent,Vector3 pos,Vector3 scale,Quaternion rot,Material mat) {
            GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);
            g.transform.SetPositionAndRotation(pos,rot);g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;
            UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        }

        static GameObject PlaceModel(GameObject asset,Transform parent,string name,Vector3 pos,float yaw,float targetHeight,Material material) {
            if(!asset)return null;
            GameObject go=(GameObject)PrefabUtility.InstantiatePrefab(asset,parent); if(!go)go=UnityEngine.Object.Instantiate(asset,parent);
            go.name=name;go.transform.position=pos;go.transform.rotation=Quaternion.Euler(0f,yaw,0f);go.transform.localScale=Vector3.one;
            Bounds b=BoundsOf(go);float h=Mathf.Max(.01f,b.size.y);go.transform.localScale=Vector3.one*(targetHeight/h);
            foreach(Renderer r in go.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=material;
            foreach(Collider c in go.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
            Bounds after=BoundsOf(go);Vector3 p=go.transform.position;p.y+=.035f-after.min.y;go.transform.position=p;return go;
        }

        static Bounds BoundsOf(GameObject go){Renderer[] rs=go.GetComponentsInChildren<Renderer>(true);if(rs.Length==0)return new Bounds(go.transform.position,Vector3.zero);Bounds b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;}
        static float Yaw(Vector3 d){d=Flat(d);return Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;}
        static Vector3 ClosestPoint(Vector3 p,Vector3 a,Vector3 b){Vector3 ab=Flat(b-a);float den=ab.sqrMagnitude;if(den<.0001f)return a;float t=Mathf.Clamp01(Vector3.Dot(Flat(p-a),ab)/den);return a+ab*t;}
        static List<Vector3> Points(RosvikOsmV15.Way w){List<Vector3> p=w.Nodes.Select(n=>n.Pos).ToList();if(p.Count>2&&w.Closed)p.RemoveAt(p.Count-1);return p;}
        static float SignedArea(List<Vector3> p){float a=0f;for(int i=0;i<p.Count;i++){Vector3 q=p[(i+1)%p.Count];a+=p[i].x*q.z-q.x*p[i].z;}return a*.5f;}
        static bool Inside(Vector3 p,List<Vector3> poly){if(poly==null||poly.Count<3)return false;bool inside=false;int j=poly.Count-1;for(int i=0;i<poly.Count;i++){float xi=poly[i].x,zi=poly[i].z,xj=poly[j].x,zj=poly[j].z;bool hit=((zi>p.z)!=(zj>p.z))&&(p.x<(xj-xi)*(p.z-zi)/(zj-zi+.00001f)+xi);if(hit)inside=!inside;j=i;}return inside;}
        static Vector3 Flat(Vector3 v){v.y=0f;return v;}
        static Transform NewGroup(Transform p,string n){Transform t=new GameObject(n).transform;t.SetParent(p,false);return t;}
        static Transform Find(Transform root,string n){if(!root)return null;if(root.name==n)return root;foreach(Transform t in root.GetComponentsInChildren<Transform>(true))if(t.name==n)return t;return null;}

        static Material Mat(string name,Color color,float smoothness){
            string path=GeneratedDir+"/mat_"+name+".mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(path);Shader s=ResolveShader();
            if(!m){m=new Material(s){name="V36 "+name};AssetDatabase.CreateAsset(m,path);}m.shader=s;
            if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color);if(m.HasProperty("_Color"))m.SetColor("_Color",color);
            if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smoothness);if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",smoothness);if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",0f);
            EditorUtility.SetDirty(m);return m;
        }
        static Shader ResolveShader(){Material proven=AssetDatabase.LoadAssetAtPath<Material>(ProvenMaterial);if(proven&&proven.shader&&proven.shader.isSupported)return proven.shader;Shader s=GraphicsSettings.defaultRenderPipeline!=null?Shader.Find("Universal Render Pipeline/Lit"):Shader.Find("Standard");if(!s||!s.isSupported)s=Shader.Find("Sprites/Default");return s;}
    }
}
#endif
