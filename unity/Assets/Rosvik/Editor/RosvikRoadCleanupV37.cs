#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UScene = UnityEngine.SceneManagement.Scene;

namespace Rosvik.Blackout.EditorTools {
    /// <summary>
    /// V37 fixes two visual problems exposed by the wide scene view:
    /// 1) V20's dark outer road strips read like twin rails along every road.
    /// 2) V36 house/garage approaches were aimed at the OSM road centreline, so their
    ///    geometry could intrude into the road surface instead of stopping at the edge.
    /// The underlying OSM road geometry stays untouched. We only clean presentation and
    /// rebuild nearby approaches against a width-aware road edge.
    /// </summary>
    [InitializeOnLoad]
    public static class RosvikRoadCleanupV37 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_ROAD_CLEANUP_V37_VERSION";
        const int Version = 37;
        const string RequiredRoot = "ROSVIK_RESIDENTIAL_GROUNDS_V36";
        const string RootName = "ROSVIK_ROAD_CLEANUP_V37";
        const string GroupName = "19 ROAD CLEANUP V37";
        const string V36GroupName = "18 RESIDENTIAL GROUNDS V36";
        const string RoadsGroupName = "02 ROADS";
        const long MainSchoolWay = 163199458;

        struct RoadHit {
            public bool valid;
            public Vector3 point;
            public Vector3 dir;
            public float distance;
            public float halfWidth;
            public bool asphalt;
        }

        sealed class JunctionInfo {
            public Vector3 position;
            public readonly HashSet<long> ways = new HashSet<long>();
            public float radius;
            public bool asphalt;
        }

        static RosvikRoadCleanupV37() {
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        [MenuItem("Rosvik/Rebuild Road Cleanup V37")]
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
            if (!root || Busy()) return;

            EditorApplication.update -= TryApply;
            Apply(scene, root);
        }

        static void Apply(UScene scene, GameObject root) {
            try {
                List<RosvikOsmV15.Way> ways = RosvikOsmV15.LoadWays();
                if (ways == null || ways.Count == 0) throw new InvalidOperationException("V37 could not load Rosvik OSM data.");
                RosvikOsmV15.Way schoolWay = ways.FirstOrDefault(w => w.Id == MainSchoolWay);
                if (schoolWay == null) throw new InvalidOperationException("V37 could not locate Rosviks skola.");
                Vector3 school = RosvikOsmV15.Centroid(schoolWay);

                Transform previous = Find(root.transform, GroupName);
                if (previous) UnityEngine.Object.DestroyImmediate(previous.gameObject);
                Transform group = NewGroup(root.transform, GroupName);

                int hiddenEdges = HideLegacyRoadEdges(root.transform);
                int removedIntrusions = RemoveV36CentrelineIntrusions(root.transform);

                Material asphalt = AssetDatabase.LoadAssetAtPath<Material>("Assets/Rosvik/GeneratedV20/mat_wet_asphalt.mat");
                Material gravel = AssetDatabase.LoadAssetAtPath<Material>("Assets/Rosvik/GeneratedV36/mat_driveway_gravel.mat");
                if (!gravel) gravel = AssetDatabase.LoadAssetAtPath<Material>("Assets/Rosvik/GeneratedV20/mat_gravel.mat");
                Material mailbox = AssetDatabase.LoadAssetAtPath<Material>("Assets/Rosvik/GeneratedV36/mat_mailbox.mat");
                Material mailboxRed = AssetDatabase.LoadAssetAtPath<Material>("Assets/Rosvik/GeneratedV36/mat_mailbox_red.mat");
                Material timber = AssetDatabase.LoadAssetAtPath<Material>("Assets/Rosvik/GeneratedV36/mat_yard_timber.mat");

                if (!asphalt) throw new InvalidOperationException("V37 could not find the proven V20 asphalt material.");
                if (!gravel) gravel = asphalt;
                if (!mailbox) mailbox = gravel;
                if (!mailboxRed) mailboxRed = mailbox;
                if (!timber) timber = gravel;

                List<RosvikOsmV15.Way> roads = ways.Where(IsVehicleRoad).ToList();
                List<RosvikOsmV15.Way> buildings = ways.Where(w => w.Closed && !string.IsNullOrEmpty(w.Tag("building")) && w.Tag("building") != "no").ToList();

                int junctions = BuildJunctionCaps(group, roads, school, asphalt, gravel);
                int connectors = BuildSafeRoadsideConnectors(group, ways, roads, buildings, school, gravel, mailbox, mailboxRed, timber);

                root.name = RootName;
                EditorPrefs.SetInt(Key, Version);
                Selection.activeObject = null;
                EditorUtility.SetDirty(root);
                AssetDatabase.SaveAssets();
                if (!Busy()) {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene, ScenePath);
                }
                Debug.Log("ROSVIK V37: hid " + hiddenEdges + " legacy dark road-edge strips, removed " + removedIntrusions +
                          " centreline intrusions, capped " + junctions + " nearby junctions and rebuilt " + connectors +
                          " approaches to stop at the actual visible road edge. OSM road layout/gameplay unchanged.");
            } catch (Exception ex) {
                Debug.LogError("ROSVIK V37 FAILED: " + ex);
            }
        }

        static int HideLegacyRoadEdges(Transform root) {
            Transform roads = Find(root, RoadsGroupName);
            if (!roads) return 0;
            int count = 0;
            foreach (Transform t in roads.GetComponentsInChildren<Transform>(true)) {
                if (t == roads || t.name != "edge") continue;
                Renderer r = t.GetComponent<Renderer>();
                if (!r) continue;
                r.enabled = false;
                EditorUtility.SetDirty(r);
                count++;
            }
            return count;
        }

        static int RemoveV36CentrelineIntrusions(Transform root) {
            Transform v36 = Find(root, V36GroupName);
            if (!v36) return 0;
            HashSet<string> badNames = new HashSet<string>(StringComparer.Ordinal) {
                "mapped garage driveway", "house entrance walk",
                "mailbox post", "mailbox", "mailbox lid"
            };
            List<GameObject> kill = v36.GetComponentsInChildren<Transform>(true)
                .Where(t => t != v36 && badNames.Contains(t.name))
                .Select(t => t.gameObject)
                .Distinct()
                .ToList();
            foreach (GameObject go in kill) UnityEngine.Object.DestroyImmediate(go);
            return kill.Count;
        }

        static int BuildJunctionCaps(Transform parent, List<RosvikOsmV15.Way> roads, Vector3 school, Material asphalt, Material gravel) {
            Dictionary<long,JunctionInfo> map = new Dictionary<long,JunctionInfo>();
            foreach (RosvikOsmV15.Way w in roads) {
                float radius = RoadHalfWidth(w);
                bool paved = IsAsphaltRoad(w);
                foreach (RosvikOsmV15.Node node in w.Nodes) {
                    if (!map.TryGetValue(node.Id, out JunctionInfo info)) {
                        info = new JunctionInfo { position = node.Pos, radius = radius, asphalt = paved };
                        map[node.Id] = info;
                    }
                    info.ways.Add(w.Id);
                    info.radius = Mathf.Max(info.radius, radius);
                    info.asphalt |= paved;
                }
            }

            int made = 0;
            foreach (JunctionInfo info in map.Values) {
                if (info.ways.Count < 2) continue;
                if (Vector3.Distance(Flat(info.position), Flat(school)) > 175f) continue;
                Disc("clean road junction", parent, info.position, info.radius + .14f, .0415f, info.asphalt ? asphalt : gravel, 22);
                made++;
            }
            return made;
        }

        static int BuildSafeRoadsideConnectors(Transform parent, List<RosvikOsmV15.Way> ways, List<RosvikOsmV15.Way> roads,
            List<RosvikOsmV15.Way> buildings, Vector3 school, Material gravel, Material mailbox, Material mailboxRed, Material timber) {

            int made = 0;

            foreach (RosvikOsmV15.Way w in ways.Where(IsOutbuilding)
                         .OrderBy(w => Vector3.Distance(Flat(RosvikOsmV15.Centroid(w)), Flat(school)))
                         .Take(20)) {
                Vector3 center = RosvikOsmV15.Centroid(w);
                if (Vector3.Distance(Flat(center), Flat(school)) > 125f) continue;
                RoadHit hit = NearestRoad(center, roads);
                if (!hit.valid || hit.distance < hit.halfWidth + .8f || hit.distance > 18f) continue;
                List<Vector3> pts = Points(w);
                if (pts.Count < 3) continue;

                Vector3 roadEdge = RoadEdgeToward(hit, center, .18f);
                Vector3 boundary = ClosestPointOnPolygon(pts, roadEdge);
                Vector3 outward = Flat(roadEdge - boundary);
                if (outward.sqrMagnitude < .01f) continue;
                outward.Normalize();
                Vector3 start = boundary + outward * .35f;
                if (!SegmentClear(start, roadEdge, buildings, w.Id)) continue;
                float len = Vector3.Distance(Flat(start), Flat(roadEdge));
                if (len < 1.0f || len > 16f) continue;
                float width = w.Tag("building") == "shed" ? 1.75f : 2.55f;
                Strip("safe garage driveway", parent, start, roadEdge, width, .027f, gravel);
                made++;
            }

            foreach (RosvikOsmV15.Way house in ways.Where(IsHouse)
                         .OrderBy(w => Vector3.Distance(Flat(RosvikOsmV15.Centroid(w)), Flat(school)))
                         .Take(28)) {
                Vector3 center = RosvikOsmV15.Centroid(house);
                if (Vector3.Distance(Flat(center), Flat(school)) > 120f) continue;
                RoadHit hit = NearestRoad(center, roads);
                if (!hit.valid || hit.distance < hit.halfWidth + .7f || hit.distance > 22f) continue;
                List<Vector3> pts = Points(house);
                if (pts.Count < 3) continue;

                Vector3 roadEdge = RoadEdgeToward(hit, center, .22f);
                Vector3 boundary = ClosestPointOnPolygon(pts, roadEdge);
                Vector3 outward = Flat(roadEdge - boundary);
                if (outward.sqrMagnitude < .01f) continue;
                outward.Normalize();
                Vector3 start = boundary + outward * .45f;
                if (!SegmentClear(start, roadEdge, buildings, house.Id)) continue;
                float len = Vector3.Distance(Flat(start), Flat(roadEdge));
                if (len > 1.35f && len < 19f) {
                    Strip("safe house entrance walk", parent, start, roadEdge, .92f, .0275f, gravel);
                    made++;
                }

                // Put the mailbox along the verge, never toward the centreline.
                float along = ((house.Id & 1) == 0) ? .70f : -.70f;
                Vector3 mailPos = roadEdge + hit.dir * along;
                mailPos.y = .04f;
                BuildMailbox(parent, mailPos, hit.dir, (house.Id % 4 == 0) ? mailboxRed : mailbox, timber);
            }
            return made;
        }

        static bool IsVehicleRoad(RosvikOsmV15.Way w) {
            string h = w.Tag("highway");
            return (h == "residential" || h == "unclassified" || h == "tertiary" || h == "service" || h == "living_street") && w.Nodes.Count >= 2;
        }

        static bool IsAsphaltRoad(RosvikOsmV15.Way w) {
            string h = w.Tag("highway");
            return h == "residential" || h == "unclassified" || h == "tertiary";
        }

        static float RoadHalfWidth(RosvikOsmV15.Way w) {
            string h = w.Tag("highway");
            if (h == "residential" || h == "unclassified" || h == "tertiary") return 2.65f;
            if (h == "service" || h == "living_street") return 1.80f;
            return 1.0f;
        }

        static RoadHit NearestRoad(Vector3 p, List<RosvikOsmV15.Way> roads) {
            RoadHit best = new RoadHit { valid = false, distance = float.MaxValue };
            foreach (RosvikOsmV15.Way w in roads) {
                for (int i = 0; i < w.Nodes.Count - 1; i++) {
                    Vector3 a = w.Nodes[i].Pos, b = w.Nodes[i + 1].Pos;
                    Vector3 q = ClosestPoint(p, a, b);
                    float d = Vector3.Distance(Flat(p), Flat(q));
                    if (d >= best.distance) continue;
                    Vector3 dir = Flat(b - a);
                    if (dir.sqrMagnitude < .001f) continue;
                    best = new RoadHit {
                        valid = true,
                        point = q,
                        dir = dir.normalized,
                        distance = d,
                        halfWidth = RoadHalfWidth(w),
                        asphalt = IsAsphaltRoad(w)
                    };
                }
            }
            return best;
        }

        static Vector3 RoadEdgeToward(RoadHit hit, Vector3 origin, float outside) {
            Vector3 side = Flat(origin - hit.point);
            if (side.sqrMagnitude < .001f) side = new Vector3(-hit.dir.z, 0f, hit.dir.x);
            side.Normalize();
            Vector3 p = hit.point + side * (hit.halfWidth + outside);
            p.y = .02f;
            return p;
        }

        static bool IsHouse(RosvikOsmV15.Way w) {
            if (!w.Closed || w.Nodes.Count < 4) return false;
            string b = w.Tag("building");
            if (string.IsNullOrEmpty(b) || b == "no" || b == "garage" || b == "shed" || b == "carport" || b == "roof") return false;
            if (b == "house" || b == "residential" || b == "detached" || b == "semidetached_house" || b == "bungalow") return true;
            if (b == "yes") {
                RosvikOsmV15.OBounds ob = RosvikOsmV15.Bounds(w);
                float proxy = ob.Width * ob.Depth;
                return proxy >= 35f && proxy <= 420f;
            }
            return false;
        }

        static bool IsOutbuilding(RosvikOsmV15.Way w) {
            if (!w.Closed || w.Nodes.Count < 4) return false;
            string b = w.Tag("building");
            return b == "garage" || b == "shed" || b == "carport";
        }

        static bool SegmentClear(Vector3 a, Vector3 b, List<RosvikOsmV15.Way> buildings, long ignoreId) {
            for (int s = 1; s < 10; s++) {
                Vector3 p = Vector3.Lerp(a, b, s / 10f);
                foreach (RosvikOsmV15.Way w in buildings) {
                    if (w.Id == ignoreId) continue;
                    if (Inside(p, Points(w))) return false;
                }
            }
            return true;
        }

        static Vector3 ClosestPointOnPolygon(List<Vector3> pts, Vector3 target) {
            Vector3 best = pts[0];
            float bestD = float.MaxValue;
            for (int i = 0; i < pts.Count; i++) {
                Vector3 q = ClosestPoint(target, pts[i], pts[(i + 1) % pts.Count]);
                float d = (Flat(q) - Flat(target)).sqrMagnitude;
                if (d < bestD) { bestD = d; best = q; }
            }
            return best;
        }

        static Vector3 ClosestPoint(Vector3 p, Vector3 a, Vector3 b) {
            Vector3 ab = Flat(b - a);
            float den = ab.sqrMagnitude;
            if (den < .0001f) return a;
            float t = Mathf.Clamp01(Vector3.Dot(Flat(p - a), ab) / den);
            return a + ab * t;
        }

        static bool Inside(Vector3 p, List<Vector3> poly) {
            if (poly == null || poly.Count < 3) return false;
            bool inside = false;
            int j = poly.Count - 1;
            for (int i = 0; i < poly.Count; i++) {
                float xi = poly[i].x, zi = poly[i].z, xj = poly[j].x, zj = poly[j].z;
                bool hit = ((zi > p.z) != (zj > p.z)) && (p.x < (xj - xi) * (p.z - zi) / (zj - zi + .00001f) + xi);
                if (hit) inside = !inside;
                j = i;
            }
            return inside;
        }

        static void BuildMailbox(Transform parent, Vector3 pos, Vector3 roadDir, Material boxMat, Material postMat) {
            Quaternion rot = Quaternion.LookRotation(new Vector3(-roadDir.z, 0f, roadDir.x), Vector3.up);
            Box("safe mailbox post", parent, pos + Vector3.up * .48f, new Vector3(.09f,.92f,.09f), rot, postMat);
            Box("safe mailbox", parent, pos + Vector3.up * .92f, new Vector3(.46f,.28f,.32f), rot, boxMat);
            Box("safe mailbox lid", parent, pos + Vector3.up * 1.075f, new Vector3(.50f,.045f,.34f), rot, boxMat);
        }

        static void Strip(string name, Transform parent, Vector3 a, Vector3 b, float width, float y, Material mat) {
            Vector3 d = Flat(b - a);
            if (d.sqrMagnitude < .0001f) return;
            Vector3 n = new Vector3(-d.z,0f,d.x).normalized * width * .5f;
            Vector3[] v = {
                new Vector3(a.x+n.x,y,a.z+n.z), new Vector3(a.x-n.x,y,a.z-n.z),
                new Vector3(b.x-n.x,y,b.z-n.z), new Vector3(b.x+n.x,y,b.z+n.z)
            };
            Mesh mesh = new Mesh { name = name + " mesh", vertices = v, triangles = new[] {0,1,2,0,2,3} };
            mesh.RecalculateNormals();
            if (mesh.normals.Length > 0 && mesh.normals.Average(q => q.y) < 0f) {
                mesh.triangles = new[] {0,2,1,0,3,2};
                mesh.RecalculateNormals();
            }
            GameObject g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            g.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }

        static void Disc(string name, Transform parent, Vector3 c, float r, float y, Material mat, int seg) {
            List<Vector3> pts = new List<Vector3>();
            for (int i = 0; i < seg; i++) {
                float a = i * Mathf.PI * 2f / seg;
                pts.Add(new Vector3(c.x + Mathf.Cos(a) * r, y, c.z + Mathf.Sin(a) * r));
            }
            Vector3[] v = new Vector3[pts.Count + 1];
            v[0] = new Vector3(c.x, y, c.z);
            for (int i = 0; i < pts.Count; i++) v[i + 1] = pts[i];
            int[] tri = new int[pts.Count * 3];
            for (int i = 0; i < pts.Count; i++) {
                int j = (i + 1) % pts.Count;
                tri[i*3] = 0; tri[i*3+1] = i+1; tri[i*3+2] = j+1;
            }
            Mesh mesh = new Mesh { name = name + " mesh", vertices = v, triangles = tri };
            mesh.RecalculateNormals();
            if (mesh.normals.Length > 0 && mesh.normals.Average(q => q.y) < 0f) {
                for (int i = 0; i < tri.Length; i += 3) { int t = tri[i+1]; tri[i+1] = tri[i+2]; tri[i+2] = t; }
                mesh.triangles = tri;
                mesh.RecalculateNormals();
            }
            GameObject g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            g.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }

        static void Box(string name, Transform parent, Vector3 pos, Vector3 scale, Quaternion rot, Material mat) {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.SetPositionAndRotation(pos, rot);
            g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = mat;
            UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        }

        static List<Vector3> Points(RosvikOsmV15.Way w) {
            List<Vector3> p = w.Nodes.Select(n => n.Pos).ToList();
            if (p.Count > 2 && w.Closed) p.RemoveAt(p.Count - 1);
            return p;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        static Transform NewGroup(Transform parent, string name) { Transform t = new GameObject(name).transform; t.SetParent(parent, false); return t; }
        static Transform Find(Transform root, string name) {
            if (!root) return null;
            if (root.name == name) return root;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }
    }
}
#endif
