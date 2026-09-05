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
    /// V28 keeps Rosvik geography and the V27 school facades locked, then turns the
    /// school/sports area into a readable game place: entrances, approach paths,
    /// benches, bicycle racks, bins, lamps, vegetation clusters and playable pitches.
    /// No roofs or building footprints are rebuilt here.
    /// </summary>
    [InitializeOnLoad]
    public static class RosvikSchoolLifeV28 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_SCHOOL_LIFE_V28_VERSION";
        const int Version = 28;
        const string RootName = "ROSVIK_SCHOOL_LIFE_V28";
        const string GroupName = "11 SCHOOL LIFE V28";
        const string GeneratedDir = "Assets/Rosvik/GeneratedV28";
        const string ProvenMaterial = "Assets/Rosvik/GeneratedV20/mat_ground.mat";
        const string KenneyRoot = "Assets/Rosvik/ThirdParty/V22Models/KenneyNature";
        const string KayKitRoot = "Assets/Rosvik/ThirdParty/V22Models/KayKitCityBits";
        const long MainSchoolWay = 163199458;
        const long OldSchoolWay = 163199461;
        const long ArenaWay = 163199454;

        static RosvikSchoolLifeV28() {
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        [MenuItem("Rosvik/Rebuild School Life V28")]
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
                if (Busy()) return;
                if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                if (Busy()) return;
                root = FindRoot();
            }
            if (!root || Busy()) return;

            EditorApplication.update -= TryApply;
            Apply(scene, root);
        }

        static GameObject FindRoot() {
            return GameObject.Find("ROSVIK_SHADER_SAFE_V27")
                ?? GameObject.Find("ROSVIK_SCHOOL_WORLD_V26")
                ?? GameObject.Find("ROSVIK_PLAYABLE_V25")
                ?? GameObject.Find("ROSVIK_CLEAN_GROUNDED_V24")
                ?? GameObject.Find(RootName);
        }

        static void Apply(UScene scene, GameObject root) {
            try {
                Directory.CreateDirectory(GeneratedDir);
                AssetDatabase.Refresh();

                List<RosvikOsmV15.Way> ways = RosvikOsmV15.LoadWays();
                if (ways == null || ways.Count == 0) throw new InvalidOperationException("V28 could not load Rosvik map data.");

                RosvikOsmV15.Way main = ways.FirstOrDefault(w => w.Id == MainSchoolWay);
                RosvikOsmV15.Way oldSchool = ways.FirstOrDefault(w => w.Id == OldSchoolWay);
                RosvikOsmV15.Way arena = ways.FirstOrDefault(w => w.Id == ArenaWay);
                if (main == null) throw new InvalidOperationException("V28 could not find Rosviks skola footprint.");

                Transform previous = Find(root.transform, GroupName);
                if (previous) UnityEngine.Object.DestroyImmediate(previous.gameObject);
                Transform group = NewGroup(root.transform, GroupName);

                Material asphalt = Mat("entry_asphalt", new Color(.145f,.155f,.15f), .28f);
                Material path = Mat("packed_path", new Color(.28f,.255f,.205f), .10f);
                Material wet = Mat("wet_puddle", new Color(.075f,.12f,.13f), .70f);
                Material pitch = Mat("pitch", new Color(.19f,.285f,.145f), .06f);
                Material line = Mat("pitch_line", new Color(.77f,.77f,.67f), .12f);
                Material metal = Mat("dark_metal", new Color(.075f,.08f,.075f), .32f);
                Material wood = Mat("weathered_wood", new Color(.29f,.16f,.085f), .08f);
                Material sign = Mat("sign_board", new Color(.20f,.255f,.21f), .16f);
                Material spruce = Mat("spruce", new Color(.075f,.17f,.095f), .04f);
                Material fall = Mat("fall_tree", new Color(.34f,.31f,.13f), .04f);
                Material bush = Mat("bush", new Color(.19f,.27f,.115f), .04f);

                GameObject bench = AssetDatabase.LoadAssetAtPath<GameObject>(KayKitRoot + "/bench.obj");
                GameObject lamp = AssetDatabase.LoadAssetAtPath<GameObject>(KayKitRoot + "/streetlight.obj");
                GameObject pine = AssetDatabase.LoadAssetAtPath<GameObject>(KenneyRoot + "/tree_pineDefaultA.obj");
                GameObject fallTree = AssetDatabase.LoadAssetAtPath<GameObject>(KenneyRoot + "/tree_default_fall.obj");
                GameObject bushModel = AssetDatabase.LoadAssetAtPath<GameObject>(KenneyRoot + "/plant_bushDetailed.obj");

                EntryInfo mainEntry = BuildEntrance(group, main, false, ways, asphalt, path, wet);
                EntryInfo oldEntry = oldSchool != null ? BuildEntrance(group, oldSchool, true, ways, asphalt, path, wet) : default;
                EntryInfo arenaEntry = arena != null ? BuildEntrance(group, arena, false, ways, asphalt, path, wet, true) : default;

                BuildSchoolFurniture(group, mainEntry, bench, lamp, metal, wood, sign);
                if (oldEntry.valid) BuildOldSchoolFurniture(group, oldEntry, bench, lamp, metal, wood);
                if (arenaEntry.valid && lamp) {
                    PlaceModel(lamp, group, "arena lamp", arenaEntry.center + arenaEntry.right * 3.2f, Yaw(arenaEntry.forward), 4.5f, metal);
                }

                BuildEdgeVegetation(group, ways, main, oldSchool, pine, fallTree, bushModel, spruce, fall, bush);
                BuildPitches(group, ways, RosvikOsmV15.Centroid(main), pitch, line);

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
                Debug.Log("ROSVIK V28: school entrances, approach paths, schoolyard furniture, vegetation clusters and sports pitches added. Geography/building footprints/roofs unchanged.");
            } catch (Exception ex) {
                Debug.LogError("ROSVIK V28 FAILED: " + ex);
            }
        }

        struct EntryInfo {
            public bool valid;
            public Vector3 center, forward, right, door;
        }

        static EntryInfo BuildEntrance(Transform parent, RosvikOsmV15.Way building, bool oldSchool,
            List<RosvikOsmV15.Way> ways, Material asphalt, Material path, Material wet, bool arena = false) {

            List<Vector3> pts = Points(building);
            if (pts.Count < 3) return default;
            float area = SignedArea(pts);
            int longest = 0; float longestLen = 0f;
            for (int i=0;i<pts.Count;i++) {
                float len = Flat(pts[(i+1)%pts.Count]-pts[i]).magnitude;
                if (len > longestLen) { longestLen = len; longest = i; }
            }

            Vector3 a=pts[longest], b=pts[(longest+1)%pts.Count];
            Vector3 edge=Flat(b-a).normalized;
            Vector3 left=new Vector3(-edge.z,0f,edge.x);
            Vector3 outward=area>0f?-left:left;
            Vector3 wall=(a+b)*.5f;
            float outDist = arena ? 4.8f : (oldSchool ? 3.2f : 3.8f);
            Vector3 plazaCenter=wall+outward*outDist; plazaCenter.y=.055f;
            Vector3 right=edge;
            float plazaW=arena?8.5f:(oldSchool?5.5f:7.2f);
            float plazaD=arena?5.6f:(oldSchool?4.3f:5.0f);
            FlatBox((arena?"arena forecourt":oldSchool?"Träskolan entrance court":"school entrance court"), parent,
                plazaCenter, right, plazaW, plazaD, .045f, asphalt);

            Vector3 target; Vector3 pathDir;
            if (NearestWalkPoint(plazaCenter, ways, 38f, out target, out pathDir)) {
                Vector3 start=plazaCenter+outward*(plazaD*.42f);
                StripBox("entry approach",parent,start,target,oldSchool?2.0f:2.5f,.035f,path);
            }

            if (!arena) {
                AddPuddle(parent, plazaCenter + right*(plazaW*.22f) + outward*.25f, right, oldSchool?1.15f:1.55f, .48f, wet);
                if (!oldSchool) AddPuddle(parent, plazaCenter - right*(plazaW*.26f) - outward*.35f, right, 1.05f, .34f, wet);
            }

            return new EntryInfo { valid=true, center=plazaCenter, forward=outward, right=right, door=wall+outward*.25f };
        }

        static void BuildSchoolFurniture(Transform parent, EntryInfo e, GameObject bench, GameObject lamp, Material metal, Material wood, Material sign) {
            if (!e.valid) return;
            if (bench) {
                PlaceModel(bench,parent,"school bench",e.center-e.right*3.8f-e.forward*.7f,Yaw(e.right),.85f,wood);
                PlaceModel(bench,parent,"school bench",e.center+e.right*3.8f-e.forward*.7f,Yaw(-e.right),.85f,wood);
            }
            if (lamp) {
                PlaceModel(lamp,parent,"school lamp",e.center-e.right*4.5f+e.forward*.35f,Yaw(e.forward),4.5f,metal);
                PlaceModel(lamp,parent,"school lamp",e.center+e.right*4.5f+e.forward*.35f,Yaw(e.forward),4.5f,metal);
            }

            // Stable stylised bike rack: deliberately simple, low-profile geometry.
            Transform rack=NewGroup(parent,"bike racks");
            Vector3 rackCenter=e.center+e.right*5.1f-e.forward*1.7f;
            for(int i=0;i<6;i++) {
                Vector3 p=rackCenter+e.forward*(i*.48f-1.2f);
                Bar("rack upright",rack,p+Vector3.up*.42f,e.right,.055f,.82f,metal,true);
            }
            Bar("rack rail",rack,rackCenter+Vector3.up*.28f,e.forward,.06f,3.1f,metal,false);

            // Notice board and waste bin create a recognisable school-entry cluster.
            Vector3 board=e.center-e.right*5.1f+e.forward*1.35f;
            Bar("notice post",parent,board-e.right*.7f+Vector3.up*.9f,Vector3.up,.08f,1.8f,metal,false);
            Bar("notice post",parent,board+e.right*.7f+Vector3.up*.9f,Vector3.up,.08f,1.8f,metal,false);
            Box("school notice board",parent,board+Vector3.up*1.35f,new Vector3(1.7f,.95f,.10f),RotationForFace(e.forward),sign,false);
            Bin(parent,e.center-e.right*2.5f-e.forward*1.3f,metal);
        }

        static void BuildOldSchoolFurniture(Transform parent, EntryInfo e, GameObject bench, GameObject lamp, Material metal, Material wood) {
            if (bench) PlaceModel(bench,parent,"Träskolan bench",e.center-e.right*3.1f-e.forward*.6f,Yaw(e.right),.82f,wood);
            if (lamp) PlaceModel(lamp,parent,"Träskolan lamp",e.center+e.right*3.0f,Yaw(e.forward),4.25f,metal);
            Bin(parent,e.center+e.right*2.0f-e.forward*1.1f,metal);
        }

        static void BuildEdgeVegetation(Transform parent, List<RosvikOsmV15.Way> ways, RosvikOsmV15.Way main,
            RosvikOsmV15.Way oldSchool, GameObject pine, GameObject fallTree, GameObject bushModel,
            Material spruce, Material fall, Material bush) {

            System.Random rng=new System.Random(2805);
            List<RosvikOsmV15.Way> buildings=new List<RosvikOsmV15.Way>();
            if(main!=null)buildings.Add(main);if(oldSchool!=null)buildings.Add(oldSchool);
            int treeCount=0,bushCount=0;

            foreach(var bw in buildings) {
                List<Vector3> pts=Points(bw);float area=SignedArea(pts);
                for(int i=0;i<pts.Count;i++) {
                    Vector3 a=pts[i],b=pts[(i+1)%pts.Count];Vector3 edge=Flat(b-a);float len=edge.magnitude;if(len<5f)continue;edge/=len;
                    Vector3 left=new Vector3(-edge.z,0,edge.x);Vector3 outward=area>0?-left:left;
                    int samples=Mathf.Clamp(Mathf.FloorToInt(len/6f),1,5);
                    for(int s=0;s<samples;s++) {
                        float t=(s+1f)/(samples+1f);Vector3 baseP=Vector3.Lerp(a,b,t)+outward*Mathf.Lerp(3.2f,5.8f,(float)rng.NextDouble());baseP.y=.04f;
                        if(!SafePlant(baseP,ways,2.3f))continue;
                        if(bushModel && bushCount<18) { PlaceModel(bushModel,parent,"school edge bush",baseP,(float)rng.NextDouble()*360f,Mathf.Lerp(.65f,1.05f,(float)rng.NextDouble()),bush);bushCount++; }
                        if((s%3)==0 && treeCount<10) {
                            GameObject src=(treeCount%4==0&&fallTree)?fallTree:pine;Material mat=(treeCount%4==0)?fall:spruce;
                            if(src){Vector3 tp=baseP+outward*Mathf.Lerp(2.0f,4.0f,(float)rng.NextDouble());if(SafePlant(tp,ways,3.4f)){PlaceModel(src,parent,"school edge tree",tp,(float)rng.NextDouble()*360f,Mathf.Lerp(4.6f,6.5f,(float)rng.NextDouble()),mat);treeCount++;}}
                        }
                    }
                }
            }
        }

        static void BuildPitches(Transform parent, List<RosvikOsmV15.Way> ways, Vector3 school, Material pitch, Material line) {
            int index=0;
            foreach(var w in ways) {
                if(!w.Closed)continue;
                if(w.Tag("leisure")!="pitch" && w.Tag("sport")!="soccer")continue;
                Vector3 center=RosvikOsmV15.Centroid(w);if(Vector3.Distance(Flat(center),Flat(school))>210f)continue;
                RosvikOsmV15.OBounds b=RosvikOsmV15.Bounds(w);if(b.Width<3f||b.Depth<3f)continue;
                Vector3 axis=b.AxisX.normalized;Vector3 perp=new Vector3(-axis.z,0,axis.x);
                FlatBox("Rosvalla pitch "+(++index),parent,new Vector3(b.Center.x,.052f,b.Center.z),axis,b.Width,b.Depth,.035f,pitch);
                RectOutline("pitch lines",parent,new Vector3(b.Center.x,.078f,b.Center.z),axis,b.Width*.94f,b.Depth*.90f,.075f,line);

                Vector3 longAxis=b.Width>=b.Depth?axis:perp;float longLen=Mathf.Max(b.Width,b.Depth);float shortLen=Mathf.Min(b.Width,b.Depth);
                float goalWidth=Mathf.Clamp(shortLen*.28f,2.3f,5.4f);
                Vector3 g1=b.Center-longAxis*(longLen*.44f);Vector3 g2=b.Center+longAxis*(longLen*.44f);
                Goal(parent,g1,longAxis,goalWidth,line);Goal(parent,g2,-longAxis,goalWidth,line);
            }
        }

        static void TuneMood() {
            RenderSettings.ambientMode=AmbientMode.Flat;
            RenderSettings.ambientLight=new Color(.35f,.37f,.315f);
            RenderSettings.fog=true;RenderSettings.fogColor=new Color(.43f,.455f,.415f);RenderSettings.fogDensity=.00125f;
            foreach(Light l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) {
                if(l.type!=LightType.Directional)continue;
                l.color=new Color(.94f,.89f,.79f);l.intensity=1.03f;l.shadowStrength=.62f;l.shadows=LightShadows.Soft;
                l.transform.rotation=Quaternion.Euler(50f,-36f,0f);
            }
        }

        static bool NearestWalkPoint(Vector3 from,List<RosvikOsmV15.Way> ways,float max, out Vector3 best, out Vector3 bestDir) {
            best=from;bestDir=Vector3.forward;float bd=max;
            foreach(var w in ways){string h=w.Tag("highway");if(h!="footway"&&h!="path"&&h!="pedestrian"&&h!="cycleway"&&h!="service")continue;
                for(int i=0;i<w.Nodes.Count-1;i++){Vector3 a=w.Nodes[i].Pos,b=w.Nodes[i+1].Pos;Vector3 q=ClosestPoint(from,a,b);float d=Vector3.Distance(Flat(from),Flat(q));if(d<bd){bd=d;best=q;best.y=.06f;bestDir=Flat(b-a).normalized;}}}
            return bd<max;
        }

        static bool SafePlant(Vector3 p,List<RosvikOsmV15.Way> ways,float roadClear) {
            if(InsideBuilding(p,ways)||InsidePitch(p,ways))return false;
            return DistanceToRoad(p,ways)>roadClear;
        }
        static bool InsideBuilding(Vector3 p,List<RosvikOsmV15.Way> ways){foreach(var w in ways){if(!w.Closed||string.IsNullOrEmpty(w.Tag("building"))||w.Tag("building")=="no")continue;if(Inside(p,Points(w)))return true;}return false;}
        static bool InsidePitch(Vector3 p,List<RosvikOsmV15.Way> ways){foreach(var w in ways){if(!w.Closed)continue;if(w.Tag("leisure")!="pitch"&&w.Tag("sport")!="soccer")continue;if(Inside(p,Points(w)))return true;}return false;}
        static float DistanceToRoad(Vector3 p,List<RosvikOsmV15.Way> ways){float best=float.MaxValue;foreach(var w in ways){if(string.IsNullOrEmpty(w.Tag("highway")))continue;for(int i=0;i<w.Nodes.Count-1;i++){Vector3 q=ClosestPoint(p,w.Nodes[i].Pos,w.Nodes[i+1].Pos);best=Mathf.Min(best,Vector3.Distance(Flat(p),Flat(q)));}}return best;}

        static void FlatBox(string name,Transform parent,Vector3 center,Vector3 xAxis,float width,float depth,float height,Material mat) {
            xAxis=Flat(xAxis).normalized;float yaw=Mathf.Atan2(xAxis.x,xAxis.z)*Mathf.Rad2Deg-90f;
            GameObject g=Box(name,parent,center,new Vector3(width,height,depth),Quaternion.Euler(0,yaw,0),mat,false);g.transform.position=center;
        }
        static void StripBox(string name,Transform parent,Vector3 a,Vector3 b,float width,float height,Material mat){Vector3 d=Flat(b-a);float len=d.magnitude;if(len<.2f)return;Vector3 c=(a+b)*.5f;c.y=Mathf.Max(a.y,b.y);FlatBox(name,parent,c,d.normalized,len,width,height,mat);}
        static void RectOutline(string name,Transform parent,Vector3 center,Vector3 axis,float width,float depth,float thickness,Material mat){axis=Flat(axis).normalized;Vector3 perp=new Vector3(-axis.z,0,axis.x);StripBox(name,parent,center-perp*(depth*.5f),center+axis*(width*.5f)-perp*(depth*.5f),thickness,.022f,mat);StripBox(name,parent,center+perp*(depth*.5f),center+axis*(width*.5f)+perp*(depth*.5f),thickness,.022f,mat);StripBox(name,parent,center-axis*(width*.5f),center-axis*(width*.5f)+perp*(depth*.5f),thickness,.022f,mat);StripBox(name,parent,center+axis*(width*.5f),center+axis*(width*.5f)+perp*(depth*.5f),thickness,.022f,mat);}

        static void AddPuddle(Transform parent,Vector3 center,Vector3 axis,float length,float width,Material mat){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name="small puddle";g.transform.SetParent(parent,false);g.transform.position=new Vector3(center.x,.072f,center.z);g.transform.rotation=Quaternion.identity;g.transform.localScale=new Vector3(length,.008f,width);g.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());}
        static void Bin(Transform parent,Vector3 p,Material mat){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name="waste bin";g.transform.SetParent(parent,false);g.transform.position=p+Vector3.up*.48f;g.transform.localScale=new Vector3(.30f,.48f,.30f);g.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());}
        static void Goal(Transform parent,Vector3 c,Vector3 forward,float width,Material mat){forward=Flat(forward).normalized;Vector3 right=new Vector3(forward.z,0,-forward.x);Vector3 l=c-right*width*.5f,r=c+right*width*.5f;l.y=r.y=.08f;Bar("goal post",parent,l+Vector3.up*.75f,Vector3.up,.06f,1.5f,mat,false);Bar("goal post",parent,r+Vector3.up*.75f,Vector3.up,.06f,1.5f,mat,false);StripBox("goal bar",parent,l+Vector3.up*1.5f,r+Vector3.up*1.5f,.06f,.06f,mat);}

        static GameObject PlaceModel(GameObject asset,Transform parent,string name,Vector3 pos,float yaw,float targetHeight,Material material){if(!asset)return null;GameObject go=(GameObject)PrefabUtility.InstantiatePrefab(asset,parent);if(!go)go=UnityEngine.Object.Instantiate(asset,parent);go.name=name;go.transform.position=pos;go.transform.rotation=Quaternion.Euler(0,yaw,0);go.transform.localScale=Vector3.one;Bounds b=RendererBounds(go);float s=targetHeight/Mathf.Max(.01f,b.size.y);go.transform.localScale=Vector3.one*s;foreach(Renderer r in go.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=material;foreach(Collider c in go.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);return go;}
        static Bounds RendererBounds(GameObject go){Renderer[] rs=go.GetComponentsInChildren<Renderer>(true);if(rs.Length==0)return new Bounds(go.transform.position,Vector3.one);Bounds b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;}

        static Material Mat(string name,Color color,float smooth){string path=GeneratedDir+"/mat_"+name+".mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(path);Shader s=ResolveShader();if(!m){m=new Material(s){name="V28 "+name};AssetDatabase.CreateAsset(m,path);}m.shader=s;m.color=color;if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color);if(m.HasProperty("_Color"))m.SetColor("_Color",color);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",0f);EditorUtility.SetDirty(m);return m;}
        static Shader ResolveShader(){Material proven=AssetDatabase.LoadAssetAtPath<Material>(ProvenMaterial);if(proven&&proven.shader&&proven.shader.isSupported)return proven.shader;Shader s=GraphicsSettings.defaultRenderPipeline!=null?Shader.Find("Universal Render Pipeline/Lit"):Shader.Find("Standard");if(!s||!s.isSupported)s=Shader.Find("Sprites/Default");return s;}

        static GameObject Box(string n,Transform p,Vector3 pos,Vector3 scale,Quaternion rot,Material m,bool col){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,false);g.transform.position=pos;g.transform.rotation=rot;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=m;if(!col)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
        static void Bar(string n,Transform p,Vector3 pos,Vector3 axis,float thickness,float length,Material m,bool horizontal){axis=axis.normalized;if(horizontal){StripBox(n,p,pos-axis*(length*.5f),pos+axis*(length*.5f),thickness,thickness,m);}else{GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=n;g.transform.SetParent(p,false);g.transform.position=pos;g.transform.localScale=new Vector3(thickness,length*.5f,thickness);g.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());}}
        static Quaternion RotationForFace(Vector3 outward){return Quaternion.LookRotation(Flat(outward).normalized,Vector3.up);}
        static Transform NewGroup(Transform p,string n){Transform t=new GameObject(n).transform;t.SetParent(p,false);return t;}
        static Transform Find(Transform root,string n){if(!root)return null;foreach(Transform t in root.GetComponentsInChildren<Transform>(true))if(t.name==n)return t;return null;}
        static float Yaw(Vector3 d){return Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;}
        static Vector3 Flat(Vector3 v){v.y=0;return v;}
        static Vector3 ClosestPoint(Vector3 p,Vector3 a,Vector3 b){Vector3 ab=Flat(b-a);float den=ab.sqrMagnitude;if(den<.0001f)return a;float t=Mathf.Clamp01(Vector3.Dot(Flat(p-a),ab)/den);return a+ab*t;}
        static List<Vector3> Points(RosvikOsmV15.Way w){List<Vector3> p=w.Nodes.Select(n=>n.Pos).ToList();if(p.Count>2&&w.Closed)p.RemoveAt(p.Count-1);return p;}
        static float SignedArea(List<Vector3> p){float a=0f;for(int i=0;i<p.Count;i++){Vector3 q=p[(i+1)%p.Count];a+=p[i].x*q.z-q.x*p[i].z;}return a*.5f;}
        static bool Inside(Vector3 p,List<Vector3> poly){if(poly==null||poly.Count<3)return false;bool inside=false;int j=poly.Count-1;for(int i=0;i<poly.Count;i++){float xi=poly[i].x,zi=poly[i].z,xj=poly[j].x,zj=poly[j].z;bool hit=((zi>p.z)!=(zj>p.z))&&(p.x<(xj-xi)*(p.z-zi)/(zj-zi+.00001f)+xi);if(hit)inside=!inside;j=i;}return inside;}
    }
}
#endif
