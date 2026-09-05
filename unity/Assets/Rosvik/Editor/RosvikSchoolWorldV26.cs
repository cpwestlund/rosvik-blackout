#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rosvik.Blackout.EditorTools {
    /// <summary>
    /// V26 is the first content pass after the technical stabilization. Geography stays locked.
    /// It uses OSM semantics for real school/parking/playground surfaces and adds reliable,
    /// flat wall details to the two school landmarks. No procedural roof changes.
    /// </summary>
    [InitializeOnLoad]
    public static class RosvikSchoolWorldV26 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_SCHOOL_WORLD_V26_VERSION";
        const int Version = 26;
        const string RootName = "ROSVIK_SCHOOL_WORLD_V26";
        const string GeneratedDir = "Assets/Rosvik/GeneratedV26";
        const long MainSchoolWay = 163199458;
        const long OldSchoolWay = 163199461;

        static RosvikSchoolWorldV26() {
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        [MenuItem("Rosvik/Rebuild School World V26")]
        public static void Force() {
            EditorPrefs.DeleteKey(Key);
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        static bool Busy() => EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating;

        static void TryApply() {
            if (EditorPrefs.GetInt(Key,0) >= Version && File.Exists(ScenePath)) { EditorApplication.update -= TryApply; return; }
            if (Busy() || !File.Exists(ScenePath)) return;

            Scene scene = EditorSceneManager.GetActiveScene();
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
            return GameObject.Find("ROSVIK_PLAYABLE_V25")
                ?? GameObject.Find("ROSVIK_CLEAN_GROUNDED_V24")
                ?? GameObject.Find("ROSVIK_GROUNDED_PASS_V23")
                ?? GameObject.Find("ROSVIK_HERO_SLICE_V22")
                ?? GameObject.Find(RootName);
        }

        static void Apply(Scene scene, GameObject root) {
            try {
                Directory.CreateDirectory(GeneratedDir);
                AssetDatabase.Refresh();

                List<RosvikOsmV15.Way> ways = RosvikOsmV15.LoadWays();
                if (ways == null || ways.Count == 0) throw new InvalidOperationException("V26 could not load Rosvik OSM data.");

                Transform old = Find(root.transform, "10 SCHOOL WORLD V26");
                if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
                Transform group = Group(root.transform, "10 SCHOOL WORLD V26");

                Material yard = Mat(GeneratedDir+"/mat_schoolyard.mat", new Color(.235f,.245f,.22f), .10f);
                Material parking = Mat(GeneratedDir+"/mat_parking.mat", new Color(.18f,.19f,.18f), .22f);
                Material playground = Mat(GeneratedDir+"/mat_playground.mat", new Color(.31f,.29f,.23f), .08f);
                Material fence = Mat(GeneratedDir+"/mat_fence.mat", new Color(.10f,.105f,.095f), .12f);
                Material glass = Mat(GeneratedDir+"/mat_window.mat", new Color(.075f,.12f,.14f), .48f);
                Material frame = Mat(GeneratedDir+"/mat_frame.mat", new Color(.68f,.68f,.60f), .18f);
                Material white = Mat(GeneratedDir+"/mat_traskola_trim.mat", new Color(.80f,.79f,.70f), .16f);
                Material door = Mat(GeneratedDir+"/mat_door.mat", new Color(.16f,.15f,.125f), .18f);

                BuildMappedGround(group, ways, yard, parking, playground, fence);

                RosvikOsmV15.Way main = ways.FirstOrDefault(w=>w.Id==MainSchoolWay);
                RosvikOsmV15.Way oldSchool = ways.FirstOrDefault(w=>w.Id==OldSchoolWay);
                Transform mainBuilding = Find(root.transform, "ROSVIKS SKOLA");
                Transform oldBuilding = Find(root.transform, "TRÄSKOLAN");
                if (main != null && mainBuilding) BuildFacade(group, main, 3.25f, glass, frame, door, false);
                if (oldSchool != null && oldBuilding) BuildFacade(group, oldSchool, 5.6f, glass, white, door, true);

                root.name = RootName;
                EditorPrefs.SetInt(Key,Version);
                Selection.activeObject = null;
                if (!Busy()) {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene, ScenePath);
                    AssetDatabase.SaveAssets();
                }
                Debug.Log("ROSVIK V26: real mapped school/parking/playground surfaces added; school facades now have stable windows, doors and Träskolan trim. Geography/roofs unchanged.");
            } catch(Exception ex) {
                Debug.LogError("ROSVIK V26 FAILED: "+ex);
            }
        }

        static void BuildMappedGround(Transform parent, List<RosvikOsmV15.Way> ways, Material yard, Material parking, Material playground, Material fence) {
            foreach (var w in ways) {
                if (w.Nodes.Count < 2) continue;
                string amenity = w.Tag("amenity");
                string leisure = w.Tag("leisure");
                string barrier = w.Tag("barrier");

                if (w.Closed && w.Nodes.Count >= 4) {
                    if (amenity == "school") PolygonFan("mapped school grounds", parent, Points(w), .018f, yard);
                    else if (amenity == "parking") PolygonFan("mapped parking", parent, Points(w), .020f, parking);
                    else if (leisure == "playground") PolygonFan("mapped playground", parent, Points(w), .021f, playground);
                }

                if (barrier == "fence" || barrier == "wall" || barrier == "hedge") {
                    float height = barrier == "hedge" ? .75f : 1.05f;
                    float thickness = barrier == "wall" ? .16f : .07f;
                    for (int i=0;i<w.Nodes.Count-1;i++) WallSegment("mapped "+barrier, parent, w.Nodes[i].Pos, w.Nodes[i+1].Pos, height, thickness, fence);
                }
            }
        }

        static void BuildFacade(Transform parent, RosvikOsmV15.Way way, float height, Material glass, Material frame, Material door, bool oldSchool) {
            List<Vector3> pts = Points(way);
            if (pts.Count < 3) return;
            float area = SignedArea(pts);
            Transform facade = Group(parent, oldSchool ? "TRÄSKOLAN facade" : "ROSVIKS SKOLA facade");

            int longest = 0;
            float longestLen = 0f;
            for(int i=0;i<pts.Count;i++) {
                float len = Vector3.Distance(Flat(pts[i]), Flat(pts[(i+1)%pts.Count]));
                if(len>longestLen){longestLen=len;longest=i;}
            }

            for(int i=0;i<pts.Count;i++) {
                Vector3 a=pts[i], b=pts[(i+1)%pts.Count];
                Vector3 dir=Flat(b-a); float len=dir.magnitude;
                if(len<2.2f) continue;
                dir/=len;
                Vector3 left=new Vector3(-dir.z,0f,dir.x);
                Vector3 outward = area>0f ? -left : left;

                int count=Mathf.Clamp(Mathf.FloorToInt(len/(oldSchool?3.4f:3.0f)),1,10);
                float margin=Mathf.Min(1.3f,len*.16f);
                float usable=Mathf.Max(.5f,len-margin*2f);
                float step=usable/count;
                for(int k=0;k<count;k++) {
                    float along=-len*.5f+margin+step*(k+.5f);
                    Vector3 mid=(a+b)*.5f + dir*along;
                    float y=oldSchool?3.0f:1.85f;
                    float ww=Mathf.Min(oldSchool?1.15f:1.35f,step*.62f);
                    AddPanel("window frame",facade,mid+outward*.055f+Vector3.up*y,outward,new Vector3(ww+.16f,oldSchool?1.30f:1.02f,.075f),frame);
                    AddPanel("window glass",facade,mid+outward*.10f+Vector3.up*y,outward,new Vector3(ww,oldSchool?1.12f:.86f,.045f),glass);
                }

                if(oldSchool) {
                    AddPanel("white sill",facade,(a+b)*.5f+outward*.06f+Vector3.up*.48f,outward,new Vector3(Mathf.Max(.5f,len-.25f),.10f,.07f),frame);
                }
            }

            // One deliberate entrance on the longest readable facade. It is an attached visual cue,
            // not a gameplay doorway yet, so it cannot break collision/geography.
            {
                Vector3 a=pts[longest], b=pts[(longest+1)%pts.Count];
                Vector3 dir=Flat(b-a).normalized;
                Vector3 left=new Vector3(-dir.z,0f,dir.x);
                Vector3 outward=area>0f?-left:left;
                Vector3 mid=(a+b)*.5f;
                AddPanel("main entrance frame",facade,mid+outward*.07f+Vector3.up*1.15f,outward,new Vector3(oldSchool?1.55f:2.15f,2.28f,.10f),frame);
                AddPanel("main entrance",facade,mid+outward*.13f+Vector3.up*1.13f,outward,new Vector3(oldSchool?1.30f:1.88f,2.05f,.065f),door);
                AddPanel("entrance canopy",facade,mid+outward*.55f+Vector3.up*2.42f,outward,new Vector3(oldSchool?2.5f:3.2f,.12f,1.05f),frame);
            }

            if(oldSchool) {
                foreach(Vector3 p in pts) {
                    GameObject trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    trim.name="white corner trim"; trim.transform.SetParent(facade,false);
                    trim.transform.position=p+Vector3.up*(height*.5f);
                    trim.transform.localScale=new Vector3(.14f,height-.18f,.14f);
                    trim.GetComponent<Renderer>().sharedMaterial=frame;
                    UnityEngine.Object.DestroyImmediate(trim.GetComponent<Collider>());
                }
            }
        }

        static void AddPanel(string name, Transform parent, Vector3 pos, Vector3 outward, Vector3 scale, Material mat) {
            GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name; g.transform.SetParent(parent,false);
            g.transform.position=pos; g.transform.rotation=Quaternion.LookRotation(outward,Vector3.up); g.transform.localScale=scale;
            g.GetComponent<Renderer>().sharedMaterial=mat; UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        }

        static void WallSegment(string name, Transform parent, Vector3 a, Vector3 b, float height, float thickness, Material mat) {
            Vector3 d=Flat(b-a); float len=d.magnitude; if(len<.15f)return;
            GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name; g.transform.SetParent(parent,false);
            g.transform.position=(a+b)*.5f+Vector3.up*(height*.5f+.035f);
            g.transform.rotation=Quaternion.Euler(0f,Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg-90f,0f);
            g.transform.localScale=new Vector3(len,height,thickness); g.GetComponent<Renderer>().sharedMaterial=mat;
            UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        }

        static void PolygonFan(string name, Transform parent, List<Vector3> pts, float y, Material mat) {
            if(pts.Count<3)return;
            Vector3 c=Vector3.zero; foreach(Vector3 p in pts)c+=p; c/=pts.Count; c.y=y;
            Vector3[] v=new Vector3[pts.Count+1]; v[0]=c;
            for(int i=0;i<pts.Count;i++)v[i+1]=new Vector3(pts[i].x,y,pts[i].z);
            int[] t=new int[pts.Count*3];
            for(int i=0;i<pts.Count;i++){int j=(i+1)%pts.Count;t[i*3]=0;t[i*3+1]=i+1;t[i*3+2]=j+1;}
            Mesh mesh=new Mesh{name=name+" mesh",vertices=v,triangles=t}; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            GameObject g=new GameObject(name);g.transform.SetParent(parent,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=mat;
        }

        static Material Mat(string path, Color color, float smoothness) {
            Material m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){Shader s=Shader.Find("Universal Render Pipeline/Lit");if(!s)s=Shader.Find("Standard");m=new Material(s);AssetDatabase.CreateAsset(m,path);}
            if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color);if(m.HasProperty("_Color"))m.SetColor("_Color",color);
            if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smoothness);if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",0f);EditorUtility.SetDirty(m);return m;
        }

        static List<Vector3> Points(RosvikOsmV15.Way w){List<Vector3> p=w.Nodes.Select(n=>n.Pos).ToList();if(p.Count>2&&w.Closed)p.RemoveAt(p.Count-1);return p;}
        static float SignedArea(List<Vector3> p){float a=0f;for(int i=0;i<p.Count;i++){Vector3 q=p[(i+1)%p.Count];a+=p[i].x*q.z-q.x*p[i].z;}return a*.5f;}
        static Vector3 Flat(Vector3 v){v.y=0f;return v;}
        static Transform Group(Transform p,string n){Transform t=new GameObject(n).transform;t.SetParent(p,false);return t;}
        static Transform Find(Transform root,string name){if(!root)return null;if(root.name==name)return root;for(int i=0;i<root.childCount;i++){Transform hit=Find(root.GetChild(i),name);if(hit)return hit;}return null;}
    }
}
#endif
