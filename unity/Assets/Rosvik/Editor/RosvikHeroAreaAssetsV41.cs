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
    /// V41 makes the asset-led direction unmistakable in the actual playable hero area.
    /// It is intentionally robust about the incoming root so a missed V39/V40 pass does not
    /// silently block progress. Rosvik geography, gameplay and controls are left intact.
    /// </summary>
    [InitializeOnLoad]
    public static class RosvikHeroAreaAssetsV41 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_HERO_AREA_ASSETS_V41_VERSION";
        const int Version = 41;
        const string RootName = "ROSVIK_HERO_AREA_ASSETS_V41";
        const string GroupName = "23 HERO AREA ASSETS V41";
        const string AssetRoot = "Assets/Rosvik/ThirdParty/V40CozyBits";
        const string TexturePath = AssetRoot + "/citybits_texture.png";
        const string GeneratedDir = "Assets/Rosvik/GeneratedV41";
        const string ProvenMaterial = "Assets/Rosvik/GeneratedV20/mat_ground.mat";
        const long MainSchoolWay = 163199458;
        const long OldSchoolWay = 163199461;
        const long ArenaWay = 163199454;

        struct Source {
            public string file, url;
            public Source(string f, string u) { file=f; url=u; }
        }

        static readonly Source[] Sources = {
            new Source("dumpster.obj", "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-City-Builder-Bits-1.0/main/addons/kaykit_city_builder_bits/Assets/obj/dumpster.obj"),
            new Source("trash_A.obj", "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-City-Builder-Bits-1.0/main/addons/kaykit_city_builder_bits/Assets/obj/trash_A.obj"),
            new Source("trash_B.obj", "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-City-Builder-Bits-1.0/main/addons/kaykit_city_builder_bits/Assets/obj/trash_B.obj"),
            new Source("box_A.obj", "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-City-Builder-Bits-1.0/main/addons/kaykit_city_builder_bits/Assets/obj/box_A.obj"),
            new Source("box_B.obj", "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-City-Builder-Bits-1.0/main/addons/kaykit_city_builder_bits/Assets/obj/box_B.obj"),
            new Source("bench.obj", "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-City-Builder-Bits-1.0/main/addons/kaykit_city_builder_bits/Assets/obj/bench.obj"),
            new Source("streetlight.obj", "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-City-Builder-Bits-1.0/main/addons/kaykit_city_builder_bits/Assets/obj/streetlight.obj"),
            new Source("car_sedan.obj", "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-City-Builder-Bits-1.0/main/addons/kaykit_city_builder_bits/Assets/obj/car_sedan.obj"),
            new Source("car_stationwagon.obj", "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-City-Builder-Bits-1.0/main/addons/kaykit_city_builder_bits/Assets/obj/car_stationwagon.obj")
        };
        const string TextureUrl = "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-City-Builder-Bits-1.0/main/addons/kaykit_city_builder_bits/Assets/obj/citybits_texture.png";

        static RosvikHeroAreaAssetsV41() {
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        [MenuItem("Rosvik/Rebuild Hero Area Assets V41")]
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
            return GameObject.Find("ROSVIK_ASSET_COZY_APOCALYPSE_V40")
                ?? GameObject.Find("ROSVIK_CLEAN_ROAD_NETWORK_V39")
                ?? GameObject.Find("ROSVIK_VILLAGE_FABRIC_V38")
                ?? GameObject.Find("ROSVIK_ROAD_CLEANUP_V37")
                ?? GameObject.Find(RootName);
        }

        static void Apply(UScene scene, GameObject root) {
            try {
                EnsureAssets();
                Directory.CreateDirectory(GeneratedDir);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                Transform old = Find(root.transform, GroupName);
                if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
                Transform group = NewGroup(root.transform, GroupName);

                List<RosvikOsmV15.Way> ways = RosvikOsmV15.LoadWays();
                if (ways == null || ways.Count == 0) throw new InvalidOperationException("V41 could not load Rosvik OSM data.");
                RosvikOsmV15.Way schoolWay = ways.FirstOrDefault(w => w.Id == MainSchoolWay);
                if (schoolWay == null) throw new InvalidOperationException("V41 could not find Rosviks skola.");
                Vector3 school = RosvikOsmV15.Centroid(schoolWay);

                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
                Material cityMat = TexturedMat("citybits", tex, .18f);
                Material frost = Mat("frost_patch", new Color(.62f,.67f,.66f), .22f);
                Material wet = Mat("wet_patch", new Color(.11f,.145f,.15f), .72f);

                GameObject dumpster=Model("dumpster.obj"), trashA=Model("trash_A.obj"), trashB=Model("trash_B.obj");
                GameObject boxA=Model("box_A.obj"), boxB=Model("box_B.obj"), bench=Model("bench.obj"), lamp=Model("streetlight.obj");
                GameObject sedan=Model("car_sedan.obj"), wagon=Model("car_stationwagon.obj");

                int made = BuildSchoolHeroCluster(group, root.transform, ways, school, dumpster, trashA, boxA, boxB, bench, lamp, sedan, cityMat, frost, wet);
                made += BuildArenaCluster(group, ways, school, dumpster, trashB, boxA, wagon, lamp, cityMat, frost, wet);
                made += BuildOldSchoolCluster(group, ways, school, trashA, boxB, bench, cityMat, frost);

                root.name = RootName;
                EditorPrefs.SetInt(Key,Version);
                Selection.activeObject=null;
                EditorUtility.SetDirty(root);
                AssetDatabase.SaveAssets();
                if (!Busy()) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene,ScenePath); }
                Debug.Log("ROSVIK V41: visible hero-area asset pass applied with " + made + " real KayKit props/vehicles plus restrained frost/wet story patches. Root fallback prevents silent dependency stalls.");
            } catch(Exception ex) { Debug.LogError("ROSVIK V41 FAILED: "+ex); }
        }

        static int BuildSchoolHeroCluster(Transform parent, Transform root, List<RosvikOsmV15.Way> ways, Vector3 school,
            GameObject dumpster, GameObject trash, GameObject boxA, GameObject boxB, GameObject bench, GameObject lamp, GameObject car,
            Material cityMat, Material frost, Material wet) {
            Transform court = Find(root,"school entrance court");
            Vector3 c = school, right=Vector3.right, forward=Vector3.forward;
            if (court) {
                Bounds b=BoundsOf(court.gameObject); c=b.center; c.y=.04f;
                right=Flat(court.right).normalized; forward=Flat(court.forward).normalized;
                if(right.sqrMagnitude<.1f)right=Vector3.right; if(forward.sqrMagnitude<.1f)forward=Vector3.forward;
            } else {
                RosvikOsmV15.OBounds ob=RosvikOsmV15.Bounds(ways.First(w=>w.Id==MainSchoolWay));
                right=ob.AxisX.sqrMagnitude>.1f?ob.AxisX.normalized:Vector3.right; forward=new Vector3(-right.z,0,right.x);
            }

            int n=0;
            // Deliberately obvious but not blocking the door: service cluster lives to one side of the playable court.
            Vector3 side=c+right*7.0f+forward*1.0f;
            if(dumpster && PlaceModel(dumpster,parent,"V41 school dumpster",side,Yaw(forward)+90f,1.18f,cityMat))n++;
            if(boxA && PlaceModel(boxA,parent,"V41 school crate A",side-right*1.35f+forward*.75f,16f,.72f,cityMat))n++;
            if(boxB && PlaceModel(boxB,parent,"V41 school crate B",side-right*1.75f-forward*.15f,-13f,.54f,cityMat))n++;
            if(trash && PlaceModel(trash,parent,"V41 school bin",side+right*1.45f-forward*.15f,4f,.86f,cityMat))n++;

            Vector3 sit=c-right*4.8f-forward*2.8f;
            if(bench && PlaceModel(bench,parent,"V41 school bench",sit,Yaw(right),1.0f,cityMat))n++;
            Vector3 lp=c+right*4.8f-forward*3.1f;
            GameObject lampGo = lamp ? PlaceModel(lamp,parent,"V41 warm school lamp",lp,Yaw(forward),5.0f,cityMat) : null;
            if(lampGo){ n++; AddWarmLight(parent,lp+Vector3.up*3.7f,8.5f,1.5f); }

            // A visible abandoned service car close enough to be seen during ordinary play, but kept off the entrance court.
            Vector3 carPos=c-right*8.4f+forward*5.4f;
            if(car && !InsideAnyBuilding(carPos,ways) && PlaceModel(car,parent,"V41 abandoned service sedan",carPos,Yaw(forward)+4f,1.45f,cityMat))n++;

            Disc("V41 frost edge",parent,c+right*3.8f+forward*4.0f,2.2f,.021f,frost,24);
            Disc("V41 wet thaw patch",parent,c-right*2.8f+forward*3.8f,1.55f,.024f,wet,24);
            return n;
        }

        static int BuildArenaCluster(Transform parent,List<RosvikOsmV15.Way> ways,Vector3 school,GameObject dumpster,GameObject trash,
            GameObject box,GameObject car,GameObject lamp,Material cityMat,Material frost,Material wet) {
            RosvikOsmV15.Way arena=ways.FirstOrDefault(w=>w.Id==ArenaWay); if(arena==null)return 0;
            RosvikOsmV15.OBounds ob=RosvikOsmV15.Bounds(arena); Vector3 c=ob.Center; Vector3 axis=ob.AxisX.sqrMagnitude>.1f?ob.AxisX.normalized:Vector3.right;
            Vector3 perp=new Vector3(-axis.z,0,axis.x); if(Vector3.Dot(Flat(school-c),perp)<0)perp=-perp;
            Vector3 edge=c+perp*(ob.Depth*.50f+3.2f); edge.y=.04f;
            int n=0;
            if(car && PlaceModel(car,parent,"V41 arena estate car",edge+axis*3.2f,Yaw(axis),1.50f,cityMat))n++;
            if(dumpster && PlaceModel(dumpster,parent,"V41 arena dumpster",edge-axis*3.4f,Yaw(axis),1.15f,cityMat))n++;
            if(trash && PlaceModel(trash,parent,"V41 arena bin",edge-axis*1.8f,Yaw(axis),.82f,cityMat))n++;
            if(box && PlaceModel(box,parent,"V41 arena supply box",edge-axis*.75f+perp*.55f,12f,.58f,cityMat))n++;
            if(lamp){Vector3 p=edge+perp*1.9f;GameObject g=PlaceModel(lamp,parent,"V41 arena lamp",p,Yaw(axis),4.8f,cityMat);if(g){n++;AddWarmLight(parent,p+Vector3.up*3.5f,7.5f,.95f);}}
            Disc("V41 arena frost",parent,edge+axis*5f,2.4f,.021f,frost,24); Disc("V41 arena wet",parent,edge-axis*5f,1.7f,.024f,wet,24);
            return n;
        }

        static int BuildOldSchoolCluster(Transform parent,List<RosvikOsmV15.Way> ways,Vector3 school,GameObject trash,GameObject box,GameObject bench,Material cityMat,Material frost) {
            RosvikOsmV15.Way old=ways.FirstOrDefault(w=>w.Id==OldSchoolWay); if(old==null)return 0;
            RosvikOsmV15.OBounds ob=RosvikOsmV15.Bounds(old); Vector3 c=ob.Center; Vector3 axis=ob.AxisX.sqrMagnitude>.1f?ob.AxisX.normalized:Vector3.right;
            Vector3 perp=new Vector3(-axis.z,0,axis.x); if(Vector3.Dot(Flat(school-c),perp)<0)perp=-perp;
            Vector3 p=c+perp*(ob.Depth*.50f+2.0f);p.y=.04f; int n=0;
            if(bench && PlaceModel(bench,parent,"V41 Traskolan bench",p+axis*2.0f,Yaw(axis),.95f,cityMat))n++;
            if(trash && PlaceModel(trash,parent,"V41 Traskolan bin",p-axis*1.3f,Yaw(axis),.80f,cityMat))n++;
            if(box && PlaceModel(box,parent,"V41 Traskolan box",p-axis*2.1f+perp*.35f,-9f,.52f,cityMat))n++;
            Disc("V41 Traskolan frost",parent,p+perp*1.8f,1.8f,.021f,frost,22);
            return n;
        }

        static void EnsureAssets() {
            Directory.CreateDirectory(FullPath(AssetRoot));
            using(WebClient wc=new WebClient()) {
                wc.Headers[HttpRequestHeader.UserAgent]="RosvikBlackout-UnityEditor";
                foreach(Source s in Sources) {
                    string path=AssetRoot+"/"+s.file, full=FullPath(path);
                    if(File.Exists(full)&&new FileInfo(full).Length>100)continue;
                    try { File.WriteAllText(full,StripObjMaterials(wc.DownloadString(s.url))); }
                    catch(Exception ex){Debug.LogWarning("V41 asset download failed "+s.file+": "+ex.Message);}
                }
                string tf=FullPath(TexturePath);
                if(!File.Exists(tf)||new FileInfo(tf).Length<100) try { wc.DownloadFile(TextureUrl,tf); } catch(Exception ex){Debug.LogWarning("V41 texture download failed: "+ex.Message);}
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(Source s in Sources){string p=AssetRoot+"/"+s.file;if(File.Exists(FullPath(p)))AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);}
            if(File.Exists(FullPath(TexturePath)))AssetDatabase.ImportAsset(TexturePath,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
        }

        static string StripObjMaterials(string text)=>string.Join("\n",text.Replace("\r\n","\n").Replace('\r','\n').Split('\n').Where(l=>!l.StartsWith("mtllib ",StringComparison.OrdinalIgnoreCase)&&!l.StartsWith("usemtl ",StringComparison.OrdinalIgnoreCase)));
        static string FullPath(string p)=>Path.Combine(Directory.GetCurrentDirectory(),p.Replace('/',Path.DirectorySeparatorChar));
        static GameObject Model(string file)=>AssetDatabase.LoadAssetAtPath<GameObject>(AssetRoot+"/"+file);

        static GameObject PlaceModel(GameObject asset,Transform parent,string name,Vector3 pos,float yaw,float targetHeight,Material mat) {
            if(!asset)return null; GameObject g=(GameObject)PrefabUtility.InstantiatePrefab(asset); if(!g)g=UnityEngine.Object.Instantiate(asset);
            g.name=name; g.transform.SetParent(parent,true); g.transform.position=pos; g.transform.rotation=Quaternion.Euler(0,yaw,0); g.transform.localScale=Vector3.one;
            Bounds b=BoundsOf(g); float h=Mathf.Max(.001f,b.size.y); float s=targetHeight/h; g.transform.localScale=Vector3.one*s;
            b=BoundsOf(g); g.transform.position += Vector3.up*(pos.y-b.min.y);
            foreach(Renderer r in g.GetComponentsInChildren<Renderer>(true)){r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;}
            foreach(Collider c in g.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
            return g;
        }

        static Bounds BoundsOf(GameObject g) {
            Renderer[] rs=g.GetComponentsInChildren<Renderer>(true); if(rs.Length==0)return new Bounds(g.transform.position,Vector3.one);
            Bounds b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;
        }

        static void AddWarmLight(Transform parent,Vector3 pos,float range,float intensity) {
            GameObject g=new GameObject("V41 warm battery light");g.transform.SetParent(parent,false);g.transform.position=pos;
            Light l=g.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(1f,.61f,.27f);l.range=range;l.intensity=intensity;l.shadows=LightShadows.Soft;
        }

        static Material TexturedMat(string name,Texture2D tex,float smooth) {
            Material m=Mat(name,Color.white,smooth); if(tex){if(m.HasProperty("_BaseMap"))m.SetTexture("_BaseMap",tex);if(m.HasProperty("_MainTex"))m.SetTexture("_MainTex",tex);EditorUtility.SetDirty(m);}return m;
        }
        static Material Mat(string name,Color c,float smooth) {
            Directory.CreateDirectory(GeneratedDir); string path=GeneratedDir+"/mat_"+name+".mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(path);Shader s=ResolveShader();
            if(!m){m=new Material(s){name="V41 "+name};AssetDatabase.CreateAsset(m,path);}m.shader=s;m.color=c;if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);if(m.HasProperty("_Color"))m.SetColor("_Color",c);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",0f);EditorUtility.SetDirty(m);return m;
        }
        static Shader ResolveShader(){Material p=AssetDatabase.LoadAssetAtPath<Material>(ProvenMaterial);if(p&&p.shader&&p.shader.isSupported)return p.shader;Shader s=GraphicsSettings.defaultRenderPipeline!=null?Shader.Find("Universal Render Pipeline/Lit"):Shader.Find("Standard");if(!s||!s.isSupported)s=Shader.Find("Sprites/Default");return s;}

        static void Disc(string name,Transform parent,Vector3 c,float r,float y,Material mat,int segments) {
            GameObject g=new GameObject(name);g.transform.SetParent(parent,false);Mesh m=new Mesh{name=name};Vector3[] v=new Vector3[segments+1];int[] t=new int[segments*3];v[0]=new Vector3(c.x,y,c.z);
            for(int i=0;i<segments;i++){float a=Mathf.PI*2f*i/segments;v[i+1]=new Vector3(c.x+Mathf.Cos(a)*r,y,c.z+Mathf.Sin(a)*r);int j=(i+1)%segments;t[i*3]=0;t[i*3+1]=i+1;t[i*3+2]=j+1;}m.vertices=v;m.triangles=t;m.RecalculateNormals();g.AddComponent<MeshFilter>().sharedMesh=m;g.AddComponent<MeshRenderer>().sharedMaterial=mat;
        }

        static bool InsideAnyBuilding(Vector3 p,List<RosvikOsmV15.Way> ways)=>ways.Any(w=>w.Closed&&!string.IsNullOrEmpty(w.Tag("building"))&&w.Tag("building")!="no"&&PointInPoly(p,w.Nodes.Select(n=>n.Pos).ToList()));
        static bool PointInPoly(Vector3 p,List<Vector3> pts){bool inside=false;for(int i=0,j=pts.Count-1;i<pts.Count;j=i++){Vector3 a=pts[i],b=pts[j];if(((a.z>p.z)!=(b.z>p.z))&&(p.x<(b.x-a.x)*(p.z-a.z)/(b.z-a.z+.000001f)+a.x))inside=!inside;}return inside;}
        static float Yaw(Vector3 d)=>Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;
        static Vector3 Flat(Vector3 v)=>new Vector3(v.x,0,v.z);
        static Transform NewGroup(Transform p,string name){GameObject g=new GameObject(name);g.transform.SetParent(p,false);return g.transform;}
        static Transform Find(Transform root,string name)=>root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name.Equals(name,StringComparison.OrdinalIgnoreCase));
    }
}
#endif
