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
    /// V34 keeps V30 gameplay and the approved V32 controls/camera completely intact.
    /// It grounds the school landmarks and gives the real OSM road network stronger
    /// northern-small-town edges: gravel shoulders, muddy drainage strips and restrained
    /// verge vegetation. No road, building, pitch or roof is moved.
    /// </summary>
    [InitializeOnLoad]
    public static class RosvikVillageDepthV34 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_VILLAGE_DEPTH_V34_VERSION";
        const int Version = 34;
        const string RootName = "ROSVIK_VILLAGE_DEPTH_V34";
        const string GroupName = "16 VILLAGE DEPTH V34";
        const string GeneratedDir = "Assets/Rosvik/GeneratedV34";
        const string ProvenMaterial = "Assets/Rosvik/GeneratedV20/mat_ground.mat";
        const string KenneyRoot = "Assets/Rosvik/ThirdParty/V22Models/KenneyNature";
        const long MainSchoolWay = 163199458;
        const long OldSchoolWay = 163199461;

        struct RoadEdge {
            public Vector3 a, b, dir, center;
            public float distance;
            public int kind;
        }

        static RosvikVillageDepthV34() {
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        [MenuItem("Rosvik/Rebuild Village Depth V34")]
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
            return GameObject.Find("ROSVIK_CHARACTER_FEEL_V33")
                ?? GameObject.Find("ROSVIK_PLAY_COMFORT_V32")
                ?? GameObject.Find("ROSVIK_CHARACTER_ATMOSPHERE_V31")
                ?? GameObject.Find(RootName);
        }

        static void Apply(UScene scene, GameObject root) {
            try {
                Directory.CreateDirectory(GeneratedDir);
                AssetDatabase.Refresh();

                Transform old = Find(root.transform, GroupName);
                if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
                Transform group = NewGroup(root.transform, GroupName);

                List<RosvikOsmV15.Way> ways = RosvikOsmV15.LoadWays();
                if (ways == null || ways.Count == 0) throw new InvalidOperationException("V34 could not load Rosvik OSM data.");

                RosvikOsmV15.Way main = ways.FirstOrDefault(w => w.Id == MainSchoolWay);
                RosvikOsmV15.Way oldSchool = ways.FirstOrDefault(w => w.Id == OldSchoolWay);
                if (main == null) throw new InvalidOperationException("V34 could not find Rosviks skola in OSM data.");
                Vector3 school = RosvikOsmV15.Centroid(main);

                Material foundation = Mat("foundation", new Color(.105f,.105f,.095f), .12f);
                Material pipe = Mat("downpipe", new Color(.31f,.32f,.29f), .30f);
                Material gravel = Mat("road_gravel", new Color(.265f,.255f,.225f), .08f);
                Material mud = Mat("road_mud", new Color(.205f,.18f,.135f), .15f);
                Material ditch = Mat("ditch_wet", new Color(.085f,.105f,.085f), .48f);
                Material shrub = Mat("verge_shrub", new Color(.13f,.215f,.095f), .03f);
                Material rockMat = Mat("verge_rock", new Color(.29f,.295f,.27f), .12f);

                BuildLandmarkGrounding(group, main, 3.25f, foundation, pipe, "Rosviks skola");
                if (oldSchool != null) BuildLandmarkGrounding(group, oldSchool, 5.6f, foundation, pipe, "Träskolan");

                List<RoadEdge> edges = CollectRoadEdges(ways, school, 118f);
                BuildRoadShoulders(group, edges, ways, gravel, mud, ditch);
                BuildVergeClusters(group, edges, ways, shrub, rockMat);

                root.name = RootName;
                EditorPrefs.SetInt(Key, Version);
                Selection.activeObject = null;
                EditorUtility.SetDirty(root);
                AssetDatabase.SaveAssets();
                if (!Busy()) {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene, ScenePath);
                }
                Debug.Log("ROSVIK V34: school landmarks grounded with foundations/drainage detail; real OSM road edges now have restrained gravel, mud, drainage and verge clusters. Controls, zoom, gameplay, geography and roofs unchanged.");
            } catch (Exception ex) {
                Debug.LogError("ROSVIK V34 FAILED: " + ex);
            }
        }

        static void BuildLandmarkGrounding(Transform parent, RosvikOsmV15.Way way, float height, Material foundation, Material pipe, string label) {
            List<Vector3> pts = Points(way);
            if (pts.Count < 3) return;

            Transform g = NewGroup(parent, label + " grounding");
            for (int i = 0; i < pts.Count; i++) {
                Vector3 a = pts[i], b = pts[(i + 1) % pts.Count];
                if (Flat(b - a).magnitude < .5f) continue;
                SegmentBox("dark foundation", g, a, b, .24f, .14f, .075f, foundation, 0f);
            }

            Vector3 centroid = RosvikOsmV15.Centroid(way);
            int pipes = 0;
            for (int i = 0; i < pts.Count && pipes < 5; i += 2) {
                Vector3 outward = Flat(pts[i] - centroid);
                if (outward.sqrMagnitude < .01f) continue;
                outward.Normalize();
                Vector3 p = pts[i] + outward * .10f;
                Box("downpipe", g, new Vector3(p.x, height * .5f, p.z), new Vector3(.075f, Mathf.Max(.8f, height - .18f), .075f), Quaternion.identity, pipe);
                Vector3 shoe = p + outward * .13f;
                Box("downpipe shoe", g, new Vector3(shoe.x, .09f, shoe.z), new Vector3(.09f,.08f,.28f), Quaternion.LookRotation(outward,Vector3.up), pipe);
                pipes++;
            }
        }

        static List<RoadEdge> CollectRoadEdges(List<RosvikOsmV15.Way> ways, Vector3 center, float radius) {
            List<RoadEdge> result = new List<RoadEdge>();
            foreach (var w in ways) {
                string h = w.Tag("highway");
                int kind;
                if (h == "service" || h == "living_street") kind = 1;
                else if (h == "residential" || h == "unclassified") kind = 2;
                else continue;

                for (int i = 0; i < w.Nodes.Count - 1; i++) {
                    Vector3 a = w.Nodes[i].Pos, b = w.Nodes[i + 1].Pos;
                    Vector3 d = Flat(b - a);
                    float len = d.magnitude;
                    if (len < 2.6f) continue;
                    Vector3 c = (a + b) * .5f;
                    float dist = Flat(c - center).magnitude;
                    if (dist > radius) continue;
                    result.Add(new RoadEdge { a=a, b=b, dir=d/len, center=c, distance=dist, kind=kind });
                }
            }
            return result;
        }

        static void BuildRoadShoulders(Transform parent, List<RoadEdge> edges, List<RosvikOsmV15.Way> ways, Material gravel, Material mud, Material ditch) {
            int made = 0;
            foreach (RoadEdge e in edges.OrderBy(x => x.distance)) {
                if (made >= 30) break;
                if (e.distance < 13f) continue;

                float halfRoad = e.kind == 2 ? 2.75f : 2.15f;
                float shoulderWidth = e.kind == 2 ? .72f : .56f;
                Vector3 side = new Vector3(-e.dir.z, 0f, e.dir.x);
                bool any = false;

                for (int s = -1; s <= 1; s += 2) {
                    Vector3 offset = side * (halfRoad + shoulderWidth * .48f) * s;
                    Vector3 probe = e.center + offset;
                    if (InsideBuilding(probe, ways) || InsidePitch(probe, ways)) continue;

                    Material mat = ((made + (s > 0 ? 1 : 0)) % 4 == 0) ? mud : gravel;
                    SegmentBox("road shoulder", parent, e.a, e.b, shoulderWidth, .028f, .018f, mat, Vector3.Dot(offset, side));
                    any = true;

                    if ((made + (s > 0 ? 1 : 0)) % 5 == 0) {
                        float ditchOffset = (halfRoad + shoulderWidth + .28f) * s;
                        Vector3 ditchProbe = e.center + side * ditchOffset;
                        if (!InsideBuilding(ditchProbe, ways) && !InsidePitch(ditchProbe, ways))
                            SegmentBox("shallow wet ditch", parent, e.a, e.b, .18f, .018f, .012f, ditch, ditchOffset);
                    }
                }
                if (any) made++;
            }
        }

        static void BuildVergeClusters(Transform parent, List<RoadEdge> edges, List<RosvikOsmV15.Way> ways, Material shrub, Material rockMat) {
            GameObject bush = AssetDatabase.LoadAssetAtPath<GameObject>(KenneyRoot + "/plant_bushDetailed.obj");
            GameObject rock = AssetDatabase.LoadAssetAtPath<GameObject>(KenneyRoot + "/rock_smallA.obj");
            if (!bush && !rock) return;

            System.Random rng = new System.Random(3419);
            List<Vector3> used = new List<Vector3>();
            int made = 0;
            foreach (RoadEdge e in edges.OrderBy(x => x.distance)) {
                if (made >= 18) break;
                if (e.distance < 19f || ((made + e.kind) % 2 == 1 && e.distance < 40f)) continue;

                float sign = made % 2 == 0 ? 1f : -1f;
                Vector3 side = new Vector3(-e.dir.z,0f,e.dir.x) * sign;
                float edgeOffset = e.kind == 2 ? 4.15f : 3.55f;
                Vector3 p = e.center + side * edgeOffset + e.dir * Mathf.Lerp(-1.4f,1.4f,(float)rng.NextDouble());
                p.y = .035f;

                if (InsideBuilding(p,ways) || InsidePitch(p,ways) || DistanceToRoad(p,ways) < 2.7f) continue;
                if (used.Any(q => Flat(q-p).magnitude < 5.0f)) continue;

                if (bush) {
                    PlaceModel(bush,parent,"road verge shrub",p,(float)rng.NextDouble()*360f,Mathf.Lerp(.55f,.92f,(float)rng.NextDouble()),shrub);
                    Vector3 p2 = p + e.dir * Mathf.Lerp(.65f,1.25f,(float)rng.NextDouble()) + side * .22f;
                    if (!InsideBuilding(p2,ways) && !InsidePitch(p2,ways))
                        PlaceModel(bush,parent,"road verge shrub",p2,(float)rng.NextDouble()*360f,Mathf.Lerp(.38f,.66f,(float)rng.NextDouble()),shrub);
                }
                if (rock && made % 3 == 0) {
                    Vector3 rp = p - e.dir * .55f - side * .18f;
                    PlaceModel(rock,parent,"road verge rock",rp,(float)rng.NextDouble()*360f,Mathf.Lerp(.28f,.48f,(float)rng.NextDouble()),rockMat);
                }
                used.Add(p);
                made++;
            }
        }

        static void SegmentBox(string name, Transform parent, Vector3 a, Vector3 b, float width, float height, float y, Material mat, float lateralOffset) {
            Vector3 d = Flat(b - a);
            float len = d.magnitude;
            if (len < .1f) return;
            d /= len;
            Vector3 side = new Vector3(-d.z,0f,d.x);
            Vector3 center = (a + b) * .5f + side * lateralOffset;
            center.y = y + height * .5f;
            Box(name,parent,center,new Vector3(width,height,len),Quaternion.LookRotation(d,Vector3.up),mat);
        }

        static GameObject PlaceModel(GameObject asset, Transform parent, string name, Vector3 pos, float yaw, float targetHeight, Material mat) {
            if (!asset) return null;
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(asset,parent);
            if (!go) go = UnityEngine.Object.Instantiate(asset,parent);
            go.name = name;
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f,yaw,0f);
            go.transform.localScale = Vector3.one;
            Bounds b = BoundsOf(go);
            go.transform.localScale = Vector3.one * (targetHeight / Mathf.Max(.01f,b.size.y));
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = mat;
            foreach (Collider c in go.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
            return go;
        }

        static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 scale, Quaternion rot, Material mat) {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.SetParent(parent,false);
            g.transform.position = pos;
            g.transform.rotation = rot;
            g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = mat;
            UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
            return g;
        }

        static Material Mat(string name, Color color, float smoothness) {
            string path = GeneratedDir + "/mat_" + name + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) {
                Material proven = AssetDatabase.LoadAssetAtPath<Material>(ProvenMaterial);
                if (proven && AssetDatabase.CopyAsset(ProvenMaterial,path)) {
                    AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                    m = AssetDatabase.LoadAssetAtPath<Material>(path);
                }
                if (!m) {
                    Shader s = ResolveShader();
                    m = new Material(s) { name = "V34 " + name };
                    AssetDatabase.CreateAsset(m,path);
                }
            }
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor",color);
            if (m.HasProperty("_Color")) m.SetColor("_Color",color);
            m.color = color;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness",smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness",smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic",0f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Shader ResolveShader() {
            Material proven = AssetDatabase.LoadAssetAtPath<Material>(ProvenMaterial);
            if (proven && proven.shader && proven.shader.isSupported) return proven.shader;
            Shader s = GraphicsSettings.defaultRenderPipeline != null ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
            if (!s || !s.isSupported) s = Shader.Find("Sprites/Default");
            return s;
        }

        static bool InsideBuilding(Vector3 p, List<RosvikOsmV15.Way> ways) {
            foreach (var w in ways) {
                if (!w.Closed || string.IsNullOrEmpty(w.Tag("building")) || w.Tag("building") == "no") continue;
                if (Inside(p,Points(w))) return true;
            }
            return false;
        }

        static bool InsidePitch(Vector3 p, List<RosvikOsmV15.Way> ways) {
            foreach (var w in ways) {
                if (!w.Closed) continue;
                if (w.Tag("leisure") != "pitch" && w.Tag("sport") != "soccer") continue;
                if (Inside(p,Points(w))) return true;
            }
            return false;
        }

        static float DistanceToRoad(Vector3 p, List<RosvikOsmV15.Way> ways) {
            float best = float.MaxValue;
            foreach (var w in ways) {
                if (string.IsNullOrEmpty(w.Tag("highway")) || w.Nodes.Count < 2) continue;
                for (int i = 0; i < w.Nodes.Count - 1; i++) {
                    Vector3 q = ClosestPoint(p,w.Nodes[i].Pos,w.Nodes[i+1].Pos);
                    best = Mathf.Min(best,Flat(p-q).magnitude);
                }
            }
            return best;
        }

        static Vector3 ClosestPoint(Vector3 p, Vector3 a, Vector3 b) {
            Vector3 ab = Flat(b-a);
            float den = ab.sqrMagnitude;
            if (den < .0001f) return a;
            float t = Mathf.Clamp01(Vector3.Dot(Flat(p-a),ab)/den);
            return a + ab*t;
        }

        static List<Vector3> Points(RosvikOsmV15.Way w) {
            List<Vector3> p = w.Nodes.Select(n=>n.Pos).ToList();
            if (p.Count > 2 && w.Closed) p.RemoveAt(p.Count-1);
            return p;
        }

        static bool Inside(Vector3 p, List<Vector3> poly) {
            if (poly == null || poly.Count < 3) return false;
            bool inside = false;
            int j = poly.Count - 1;
            for (int i = 0; i < poly.Count; i++) {
                float xi=poly[i].x, zi=poly[i].z, xj=poly[j].x, zj=poly[j].z;
                bool hit=((zi>p.z)!=(zj>p.z)) && (p.x < (xj-xi)*(p.z-zi)/(zj-zi+.00001f)+xi);
                if (hit) inside = !inside;
                j=i;
            }
            return inside;
        }

        static Bounds BoundsOf(GameObject go) {
            Renderer[] rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return new Bounds(go.transform.position,Vector3.one);
            Bounds b = rs[0].bounds;
            for (int i=1;i<rs.Length;i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        static Transform NewGroup(Transform parent, string name) {
            Transform t = new GameObject(name).transform;
            t.SetParent(parent,false);
            return t;
        }

        static Transform Find(Transform root, string name) {
            if (!root) return null;
            if (root.name == name) return root;
            foreach (Transform child in root) {
                Transform hit = Find(child,name);
                if (hit) return hit;
            }
            return null;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
    }
}
#endif
