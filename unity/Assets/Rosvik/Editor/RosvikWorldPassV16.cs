#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikWorldPassV16 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_WORLD_PASS_VERSION";
        const int Version = 16;

        static RosvikWorldPassV16() => EditorApplication.delayCall += Apply;

        [MenuItem("Rosvik/Apply World Pass V16")]
        public static void Apply() {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorPrefs.GetInt(Key, 0) >= Version && File.Exists(ScenePath)) return;

            try {
                RosvikMapLockV15.Apply();
                if (!File.Exists(ScenePath)) return;

                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                Transform root = FindRoot();
                if (!root) return;

                List<RosvikOsmV15.Way> ways = RosvikOsmV15.LoadWays();
                if (ways == null || ways.Count == 0) return;

                Transform map = Child(root, "OSM_ACTUAL_LAYOUT_V15");
                if (!map) return;

                DisableV15Placeholders(map);
                Transform exact = Child(map, "OSM_EXACT_WORLD_V16");
                if (exact) UnityEngine.Object.DestroyImmediate(exact.gameObject);
                exact = new GameObject("OSM_EXACT_WORLD_V16").transform;
                exact.SetParent(map, false);

                Material earth = Mat("V16 damp ground", new Color(.22f,.25f,.16f), .04f);
                Material grass = Mat("V16 worn grass", new Color(.20f,.31f,.14f), .04f);
                Material field = Mat("V16 football grass", new Color(.16f,.34f,.13f), .05f);
                Material forest = Mat("V16 rough vegetation", new Color(.12f,.22f,.11f), .03f);
                Material schoolGround = Mat("V16 school ground", new Color(.24f,.27f,.21f), .05f);
                Material building = Mat("V16 mapped walls", new Color(.39f,.40f,.37f), .07f);
                Material house = Mat("V16 house walls", new Color(.46f,.43f,.36f), .07f);
                Material roof = Mat("V16 mapped roofs", new Color(.14f,.15f,.15f), .18f);
                Material pitchLine = Mat("V16 pitch markings", new Color(.78f,.79f,.69f), .10f);
                Material metal = Mat("V16 dark metal", new Color(.09f,.10f,.105f), .25f);
                Material puddle = Mat("V16 puddle", new Color(.13f,.18f,.19f), .72f);
                Material remnantSnow = Mat("V16 remnant snow", new Color(.72f,.76f,.75f), .10f);

                Transform terrain = Child(map, "V15 thaw terrain");
                if (terrain) {
                    terrain.localPosition = new Vector3(80f,-.22f,-110f);
                    terrain.localScale = new Vector3(620f,.35f,620f);
                    Renderer tr = terrain.GetComponent<Renderer>(); if (tr) tr.sharedMaterial = earth;
                }

                BuildLandCover(exact, ways, grass, forest, schoolGround);
                int buildingCount = BuildExactBuildings(exact, ways, building, house, roof);
                int pitchCount = BuildExactPitches(exact, ways, field, pitchLine, metal);
                AddSchoolAreaDetails(exact, ways, puddle, remnantSnow, metal);

                Transform timber = Child(root, "Träskolan - ca 1900");
                if (timber) RebuildTimberRoof(timber, roof, metal);

                Transform main = Child(root, "Huvudbyggnaden - 1970-tal");
                if (main) {
                    var mainWay = ways.FirstOrDefault(w => w.Id == 163199458);
                    if (mainWay != null) {
                        Transform player = Child(root, "PLAYER_PLACEHOLDER");
                        if (player) player.localPosition = RosvikOsmV15.Centroid(mainWay) + new Vector3(-7f,.12f,11f);
                    }
                }

                Camera cam = Camera.main;
                if (cam && cam.orthographic) cam.orthographicSize = 12.2f;
                IsometricCameraRig rig = UnityEngine.Object.FindFirstObjectByType<IsometricCameraRig>();
                if (rig) rig.orthographicSize = 12.2f;

                root.name = "ROSVIK_WORLD_PASS_V16";
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
                EditorPrefs.SetInt(Key, Version);
                Debug.Log("ROSVIK V16 APPLIED: exact OSM footprints, expanded village map, pitch geometry and rebuilt Träskolan roof. Buildings=" + buildingCount + " pitches=" + pitchCount);
            } catch (Exception ex) {
                Debug.LogError("ROSVIK V16 WORLD PASS FAILED: " + ex);
            }
        }

        static void DisableV15Placeholders(Transform map) {
            foreach (Transform t in map.GetComponentsInChildren<Transform>(true)) {
                if (t == map) continue;
                if (t.name.StartsWith("OSM building ", StringComparison.Ordinal) ||
                    t.name.StartsWith("OSM pitch ", StringComparison.Ordinal) ||
                    t.name.StartsWith("V15 puddle", StringComparison.Ordinal) ||
                    t.name.StartsWith("V15 snow patch", StringComparison.Ordinal))
                    t.gameObject.SetActive(false);
            }
        }

        static void BuildLandCover(Transform parent, List<RosvikOsmV15.Way> ways, Material grass, Material forest, Material schoolGround) {
            foreach (var w in ways) {
                if (!w.Closed || w.Nodes.Count < 4 || !string.IsNullOrEmpty(w.Tag("building"))) continue;
                Material mat = null;
                string land = w.Tag("landuse");
                string natural = w.Tag("natural");
                string leisure = w.Tag("leisure");
                string amenity = w.Tag("amenity");

                if (land == "forest" || natural == "wood") mat = forest;
                else if (land == "grass" || land == "meadow" || leisure == "park" || leisure == "garden") mat = grass;
                else if (amenity == "school") mat = schoolGround;
                if (!mat) continue;

                GameObject g = PolygonSurface("V16 landcover " + w.Id, parent, Points(w), .015f, mat);
                if (g && (land == "forest" || natural == "wood")) ScatterVegetation(g.transform, Points(w), forest, w.Id);
            }
        }

        static int BuildExactBuildings(Transform parent, List<RosvikOsmV15.Way> ways, Material generic, Material house, Material roof) {
            int count = 0;
            foreach (var w in ways) {
                string tag = w.Tag("building");
                if (string.IsNullOrEmpty(tag) || tag == "no" || !w.Closed) continue;
                if (w.Id == 163199461 || w.Id == 163199458 || w.Id == 163199454) continue;
                List<Vector3> pts = Points(w);
                if (pts.Count < 3) continue;

                float h = 3.4f;
                if (tag == "garage" || tag == "shed" || tag == "carport") h = 2.5f;
                else if (tag == "house" || tag == "residential" || tag == "detached") h = 4.3f;
                else if (tag == "apartments") h = 6.5f;
                Material wall = (tag == "house" || tag == "residential" || tag == "detached") ? house : generic;

                GameObject go = ExtrudedPolygon("V16 building " + w.Id + " " + tag, parent, pts, h, wall, roof);
                if (go) count++;
            }
            return count;
        }

        static int BuildExactPitches(Transform parent, List<RosvikOsmV15.Way> ways, Material field, Material line, Material metal) {
            int count = 0;
            foreach (var w in ways) {
                if (!w.Closed) continue;
                string leisure = w.Tag("leisure");
                string sport = w.Tag("sport");
                if (leisure != "pitch" && sport != "soccer") continue;
                List<Vector3> pts = Points(w);
                if (pts.Count < 3) continue;

                Transform p = new GameObject("V16 pitch " + w.Id + NameSuffix(w)).transform;
                p.SetParent(parent, false);
                PolygonSurface("playing surface", p, pts, .055f, field);

                var b = RosvikOsmV15.Bounds(w);
                Quaternion rot = Quaternion.FromToRotation(Vector3.right, b.AxisX);
                PitchLine(p, b, rot, new Vector3(b.Width,.018f,.09f), new Vector3(0,.075f,b.Depth*.5f), line);
                PitchLine(p, b, rot, new Vector3(b.Width,.018f,.09f), new Vector3(0,.075f,-b.Depth*.5f), line);
                PitchLine(p, b, rot, new Vector3(.09f,.018f,b.Depth), new Vector3(b.Width*.5f,.075f,0), line);
                PitchLine(p, b, rot, new Vector3(.09f,.018f,b.Depth), new Vector3(-b.Width*.5f,.075f,0), line);
                PitchLine(p, b, rot, new Vector3(.09f,.018f,b.Depth), new Vector3(0,.075f,0), line);

                Goal(p, b.Center + rot * new Vector3(0,.10f,b.Depth*.5f-.35f), rot, Mathf.Clamp(b.Width*.14f,2.2f,5.5f), metal);
                Goal(p, b.Center + rot * new Vector3(0,.10f,-b.Depth*.5f+.35f), rot * Quaternion.Euler(0,180,0), Mathf.Clamp(b.Width*.14f,2.2f,5.5f), metal);
                count++;
            }
            return count;
        }

        static void AddSchoolAreaDetails(Transform parent, List<RosvikOsmV15.Way> ways, Material puddle, Material snow, Material metal) {
            var main = ways.FirstOrDefault(w => w.Id == 163199458);
            var old = ways.FirstOrDefault(w => w.Id == 163199461);
            if (main != null) {
                Vector3 c = RosvikOsmV15.Centroid(main);
                FlatDisc("V16 puddle schoolyard A", parent, c + new Vector3(-14f,.045f,18f), new Vector2(4.2f,2.1f), -12f, puddle);
                FlatDisc("V16 puddle schoolyard B", parent, c + new Vector3(12f,.045f,11f), new Vector2(2.8f,1.5f), 20f, puddle);
                FlatDisc("V16 snow remnant A", parent, c + new Vector3(-22f,.055f,27f), new Vector2(3.5f,1.3f), 7f, snow);
            }
            if (old != null) {
                Vector3 c = RosvikOsmV15.Centroid(old);
                FlatDisc("V16 snow remnant Träskolan", parent, c + new Vector3(8f,.78f,-9f), new Vector2(2.6f,1.0f), -20f, snow);
            }
        }

        static void RebuildTimberRoof(Transform b, Material roof, Material metal) {
            foreach (Transform t in b.GetComponentsInChildren<Transform>(true)) {
                if (t == b) continue;
                string n = t.name.ToLowerInvariant();
                if (t.parent == b && (t.name == "Roof" || t.name == "Ridge cap" || t.name == "Front eave edge" || t.name == "Back eave edge"))
                    t.gameObject.SetActive(false);
                if (n.Contains("roof snow") || n.Contains("windboard") || n.Contains("wind board"))
                    t.gameObject.SetActive(false);
            }

            Transform old = Child(b, "V16 CLEAN TIMBER ROOF");
            if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
            Transform r = new GameObject("V16 CLEAN TIMBER ROOF").transform;
            r.SetParent(b, false);

            float width = 14.5f, depth = 8.6f, wallH = 6.0f, rise = 3.15f, overhang = .55f;
            Mesh mesh = GableMesh(width, depth, wallH, rise, overhang);
            GameObject roofGo = new GameObject("Clean symmetrical gable roof");
            roofGo.transform.SetParent(r, false);
            roofGo.AddComponent<MeshFilter>().sharedMesh = mesh;
            roofGo.AddComponent<MeshRenderer>().sharedMaterial = roof;

            Cube("Clean ridge", r, new Vector3(0f,wallH+rise+.035f,0f), new Vector3(width+overhang*2f+.08f,.10f,.13f), metal, false);
            Cube("Clean front eave", r, new Vector3(0f,wallH+.025f,depth*.5f+overhang), new Vector3(width+overhang*2f,.09f,.10f), metal, false);
            Cube("Clean back eave", r, new Vector3(0f,wallH+.025f,-depth*.5f-overhang), new Vector3(width+overhang*2f,.09f,.10f), metal, false);
        }

        static Mesh GableMesh(float width, float depth, float wallH, float rise, float overhang) {
            float x0 = -width*.5f-overhang, x1 = width*.5f+overhang;
            float z0 = -depth*.5f-overhang, z1 = depth*.5f+overhang;
            float y0 = wallH, y1 = wallH+rise;
            Vector3[] v = {
                new Vector3(x0,y0,z0), new Vector3(x0,y1,0), new Vector3(x0,y0,z1),
                new Vector3(x1,y0,z0), new Vector3(x1,y1,0), new Vector3(x1,y0,z1)
            };
            int[] tri = { 0,4,3, 0,1,4, 1,5,4, 1,2,5, 0,2,1, 3,4,5 };
            Mesh m = new Mesh { name = "V16 clean gable", vertices = v, triangles = tri };
            m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        static List<Vector3> Points(RosvikOsmV15.Way w) {
            List<Vector3> pts = w.Nodes.Select(n => n.Pos).ToList();
            if (pts.Count > 2 && w.Closed) pts.RemoveAt(pts.Count-1);
            return pts;
        }

        static GameObject PolygonSurface(string name, Transform parent, List<Vector3> pts, float y, Material mat) {
            if (pts == null || pts.Count < 3) return null;
            Vector3 center = Vector3.zero; foreach (Vector3 p in pts) center += p; center /= pts.Count;
            Vector3[] verts = new Vector3[pts.Count + 1];
            verts[0] = new Vector3(center.x,y,center.z);
            for (int i=0;i<pts.Count;i++) verts[i+1] = new Vector3(pts[i].x,y,pts[i].z);
            int[] tri = new int[pts.Count*3];
            for (int i=0;i<pts.Count;i++) { tri[i*3]=0; tri[i*3+1]=i+1; tri[i*3+2]=((i+1)%pts.Count)+1; }
            Mesh mesh = new Mesh { name=name+" mesh", vertices=verts, triangles=tri };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            GameObject go = new GameObject(name); go.transform.SetParent(parent,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh; go.AddComponent<MeshRenderer>().sharedMaterial=mat;
            return go;
        }

        static GameObject ExtrudedPolygon(string name, Transform parent, List<Vector3> pts, float height, Material wall, Material roof) {
            if (pts == null || pts.Count < 3) return null;
            Transform root = new GameObject(name).transform; root.SetParent(parent,false);
            int n = pts.Count;
            Vector3 center = Vector3.zero; foreach (Vector3 p in pts) center += p; center /= n;

            Vector3[] v = new Vector3[n*2 + 2];
            v[0] = new Vector3(center.x,0,center.z);
            v[1] = new Vector3(center.x,height,center.z);
            for (int i=0;i<n;i++) { v[2+i] = new Vector3(pts[i].x,0,pts[i].z); v[2+n+i] = new Vector3(pts[i].x,height,pts[i].z); }

            List<int> wallTri = new List<int>();
            for (int i=0;i<n;i++) {
                int j=(i+1)%n; int b0=2+i,b1=2+j,t0=2+n+i,t1=2+n+j;
                wallTri.Add(b0);wallTri.Add(t0);wallTri.Add(t1);
                wallTri.Add(b0);wallTri.Add(t1);wallTri.Add(b1);
            }
            Mesh wm = new Mesh { name=name+" walls", vertices=v, triangles=wallTri.ToArray() }; wm.RecalculateNormals(); wm.RecalculateBounds();
            GameObject wg = new GameObject("walls"); wg.transform.SetParent(root,false); wg.AddComponent<MeshFilter>().sharedMesh=wm; wg.AddComponent<MeshRenderer>().sharedMaterial=wall;

            Vector3[] rv = new Vector3[n+1]; rv[0]=new Vector3(center.x,height+.05f,center.z);
            for(int i=0;i<n;i++)rv[i+1]=new Vector3(pts[i].x,height+.05f,pts[i].z);
            int[] rt=new int[n*3];
            for(int i=0;i<n;i++){rt[i*3]=0;rt[i*3+1]=((i+1)%n)+1;rt[i*3+2]=i+1;}
            Mesh rm=new Mesh{name=name+" roof",vertices=rv,triangles=rt};rm.RecalculateNormals();rm.RecalculateBounds();
            GameObject rg=new GameObject("roof");rg.transform.SetParent(root,false);rg.AddComponent<MeshFilter>().sharedMesh=rm;rg.AddComponent<MeshRenderer>().sharedMaterial=roof;
            return root.gameObject;
        }

        static void PitchLine(Transform p, RosvikOsmV15.OBounds b, Quaternion rot, Vector3 scale, Vector3 offset, Material mat) {
            GameObject l = Cube("line", p, b.Center + rot*offset, scale, mat, false); l.transform.localRotation=rot;
        }

        static void Goal(Transform parent, Vector3 pos, Quaternion rot, float width, Material metal) {
            Transform g = new GameObject("goal").transform; g.SetParent(parent,false); g.localPosition=pos; g.localRotation=rot;
            Cube("left post",g,new Vector3(-width*.5f,1.0f,0),new Vector3(.07f,2f,.07f),metal,false);
            Cube("right post",g,new Vector3(width*.5f,1.0f,0),new Vector3(.07f,2f,.07f),metal,false);
            Cube("crossbar",g,new Vector3(0,2f,0),new Vector3(width,.07f,.07f),metal,false);
        }

        static void FlatDisc(string name, Transform parent, Vector3 pos, Vector2 size, float yaw, Material mat) {
            GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localRotation=Quaternion.Euler(0,yaw,0);g.transform.localScale=new Vector3(size.x,.012f,size.y);g.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        }

        static void ScatterVegetation(Transform parent, List<Vector3> polygon, Material mat, long seed) {
            if (polygon.Count < 3) return;
            float minX=polygon.Min(p=>p.x), maxX=polygon.Max(p=>p.x), minZ=polygon.Min(p=>p.z), maxZ=polygon.Max(p=>p.z);
            System.Random rng = new System.Random((int)(seed & 0x7fffffff));
            int made=0;
            for(int attempt=0;attempt<80 && made<18;attempt++) {
                Vector3 p=new Vector3(Mathf.Lerp(minX,maxX,(float)rng.NextDouble()),.05f,Mathf.Lerp(minZ,maxZ,(float)rng.NextDouble()));
                if(!Inside(p,polygon)) continue;
                float s=.35f+(float)rng.NextDouble()*.45f;
                GameObject tuft=GameObject.CreatePrimitive(PrimitiveType.Cylinder);tuft.name="vegetation tuft";tuft.transform.SetParent(parent,false);tuft.transform.localPosition=p;tuft.transform.localScale=new Vector3(s,.10f,s);tuft.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(tuft.GetComponent<Collider>());made++;
            }
        }

        static bool Inside(Vector3 p, List<Vector3> poly) {
            bool inside=false; int j=poly.Count-1;
            for(int i=0;i<poly.Count;i++) { float xi=poly[i].x, zi=poly[i].z, xj=poly[j].x, zj=poly[j].z; bool hit=((zi>p.z)!=(zj>p.z)) && (p.x < (xj-xi)*(p.z-zi)/(zj-zi+.00001f)+xi); if(hit)inside=!inside; j=i; }
            return inside;
        }

        static string NameSuffix(RosvikOsmV15.Way w){string n=w.Tag("name");return string.IsNullOrEmpty(n)?"":" "+n;}
        static Material Mat(string n, Color c, float sm){Shader s=GraphicsSettings.defaultRenderPipeline!=null?Shader.Find("Universal Render Pipeline/Lit"):Shader.Find("Standard");if(!s||!s.isSupported)s=Shader.Find("Sprites/Default");Material m=new Material(s){name=n,color=c};if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",sm);return m;}
        static GameObject Cube(string n,Transform p,Vector3 pos,Vector3 sc,Material m,bool col){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=sc;g.GetComponent<Renderer>().sharedMaterial=m;if(!col)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
        static Transform FindRoot(){string[] n={"ROSVIK_WORLD_PASS_V16","ROSVIK_MAP_LOCK_V15","ROSVIK_HERO_POLISH_V14","ROSVIK_POLISH_PASS_V13"};foreach(string s in n){GameObject g=GameObject.Find(s);if(g)return g.transform;}return null;}
        static Transform Child(Transform r,string n){foreach(Transform t in r.GetComponentsInChildren<Transform>(true))if(t.name==n)return t;return null;}
    }
}
#endif
