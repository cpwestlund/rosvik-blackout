#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Rosvik.Blackout;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikCleanWorldResetV88 {
        const int Version = 88;
        const string Key = "ROSVIK_CLEAN_WORLD_RESET_V88";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string RootName = "V88 CLEAN OSM WORLD";
        const string BackendRootName = "V88 SYSTEM BACKEND (INVISIBLE)";
        const string Generated = "Assets/Rosvik/GeneratedV88";
        const string CacheV86 = "Assets/Rosvik/GeneratedV86/rosvik_overpass_cache.xml";
        const string CacheV88 = Generated + "/rosvik_overpass_cache.xml";
        const string Overpass = "https://overpass-api.de/api/interpreter";
        const double OriginLat = 65.426266;
        const double OriginLon = 21.692577;
        const float RotationDeg = 33f;
        const float MapScale = .72f;
        const float ActiveRadius = 330f;
        const string SchoolId = "163199458";
        const string ArenaId = "163199454";
        const string OldSchoolId = "163199461";

        sealed class Feature {
            public string id, highway, building, leisure, name, surface, amenity, sport;
            public readonly List<Vector2> pts = new List<Vector2>();
            public bool IsRoad => !string.IsNullOrEmpty(highway);
            public bool IsBuilding => !string.IsNullOrEmpty(building);
        }

        struct Frame {
            public Vector3 c, right, forward;
            public float width, depth, yaw;
        }

        static Shader shader;
        static Material snow, snow2, packed, asphalt, gravel, forest, trunk;
        static Material red, ochre, blue, cream, brick, trim, glass, warmGlass, roof, metal, wood;
        static Material schoolFloor, hallFloor, ice, rinkBlue, rinkRed, white;
        static Mesh coneMesh;
        static List<Feature> features;
        static Vector3 mapOffset;

        static RosvikCleanWorldResetV88() {
            if (EditorPrefs.GetInt(Key, 0) >= Version) return;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V88 CLEAN SLATE OSM WORLD - KEEP SYSTEMS")]
        public static void Force() {
            EditorPrefs.DeleteKey(Key);
            EditorApplication.delayCall += Auto;
        }

        static void Auto() {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) {
                EditorApplication.delayCall += Auto;
                return;
            }
            if (!File.Exists(ScenePath)) return;
            try { Apply(); }
            catch (Exception ex) { Debug.LogError("V88 CLEAN WORLD FAILED: " + ex); }
        }

        static void Apply() {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            CoziPlayerV57 player = UnityEngine.Object.FindFirstObjectByType<CoziPlayerV57>();
            Camera cam = Camera.main ? Camera.main : UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (!player || !cam) throw new Exception("PLAYER or Main Camera missing");

            Directory.CreateDirectory(Generated);
            shader = PickKnownGoodShader();
            BuildMaterials();
            coneMesh = EnsureConeMesh();
            features = LoadOsm();
            if (features == null || features.Count < 20) throw new Exception("OSM data missing or invalid");

            Feature school = features.FirstOrDefault(f => f.id == SchoolId);
            Feature arena = features.FirstOrDefault(f => f.id == ArenaId);
            if (school == null || arena == null) throw new Exception("Core OSM footprints not found in cache/download");

            // Keep the actual OSM geometry. Only translate it so the school becomes the playable origin.
            Vector3 rawSchoolCenter = RawMap(Centroid(school.pts));
            mapOffset = new Vector3(0f, 0f, 7.35f) - rawSchoolCenter;

            GameObject backend = PreserveSystemsAndWipeScene(player.gameObject, cam.gameObject);
            SanitizeBackend(backend);

            GameObject root = new GameObject(RootName);
            Transform ground = Group(root.transform, "01 CONTINUOUS SNOW GROUND");
            Transform roads = Group(root.transform, "02 OSM ROADS");
            Transform town = Group(root.transform, "03 OSM BUILDINGS");
            Transform landmarks = Group(root.transform, "04 AUTHORED LANDMARKS");
            Transform interiors = Group(root.transform, "05 CLEAN INTERIORS");
            Transform nature = Group(root.transform, "06 VEGETATION");
            Transform details = Group(root.transform, "07 WORLD DETAILS");

            Bounds worldBounds = BuildTerrain(ground);
            BuildRoads(roads);
            BuildGenericBuildings(town);

            Vector3[] schoolPts = WorldPoints(school);
            Vector3[] arenaPts = WorldPoints(arena);
            Feature oldSchool = features.FirstOrDefault(f => f.id == OldSchoolId);

            GameObject schoolShell = BuildSchoolFromScratch(landmarks, schoolPts);
            GameObject arenaShell = BuildIceArenaFromScratch(landmarks, arenaPts);
            if (oldSchool != null) BuildStenskolan(landmarks, WorldPoints(oldSchool));

            BuildSchoolInterior(interiors, schoolPts);
            BuildArenaInterior(interiors, arenaPts);
            BuildCampusDetails(details, schoolPts, arenaPts);
            BuildVegetation(nature, worldBounds);
            BuildLighting(root.transform);

            OsmWorldRuntimeV86 runtime = root.AddComponent<OsmWorldRuntimeV86>();
            Bounds sb = BoundsOf(schoolPts); Bounds ab = BoundsOf(arenaPts);
            runtime.attribution = "© OpenStreetMap contributors · ODbL 1.0";
            runtime.cutaways = new [] {
                new CutawayTargetV86 {
                    visualRoot = schoolShell,
                    minXZ = new Vector2(sb.min.x, sb.min.z),
                    maxXZ = new Vector2(sb.max.x, sb.max.z),
                    padding = 1.0f
                },
                new CutawayTargetV86 {
                    visualRoot = arenaShell,
                    minXZ = new Vector2(ab.min.x, ab.min.z),
                    maxXZ = new Vector2(ab.max.x, ab.max.z),
                    padding = 1.1f
                }
            };

            Vector3 spawn = SchoolEntrancePoint(schoolPts);
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc) cc.enabled = false;
            player.transform.position = spawn;
            player.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            if (cc) cc.enabled = true;
            player.SetObjective("Utforska Rosvik. Världen är nu byggd från en ren OpenStreetMap-bas utan dubbla gamla grafiklager.");

            CozyCameraV57 cameraRig = cam.GetComponent<CozyCameraV57>();
            if (cameraRig) cameraRig.target = player.transform;
            cam.orthographic = true;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, 7.6f, 9.4f);

            CleanupMissingScripts();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorPrefs.SetInt(Key, Version);
            SceneView.RepaintAll();
            Debug.Log("V88 COMPLETE — old visual generations removed, gameplay backend preserved, and Rosvik rebuilt once from a clean OSM coordinate system.");
        }

        static GameObject PreserveSystemsAndWipeScene(GameObject player, GameObject camera) {
            GameObject playerRoot = player.transform.root.gameObject;
            GameObject cameraRoot = camera.transform.root.gameObject;
            GameObject backendBase = GameObject.Find("COZY SCHOOL GAME V58");
            if (!backendBase) backendBase = GameObject.Find(BackendRootName);

            GameObject backendRoot = GameObject.Find(BackendRootName);
            if (!backendRoot) backendRoot = new GameObject(BackendRootName);
            if (backendBase && backendBase != backendRoot && backendBase.transform.parent != backendRoot.transform)
                backendBase.transform.SetParent(backendRoot.transform, true);

            HashSet<GameObject> keep = new HashSet<GameObject> { playerRoot, cameraRoot, backendRoot };
            foreach (GameObject g in EditorSceneManager.GetActiveScene().GetRootGameObjects()) {
                if (!g || keep.Contains(g)) continue;
                if (g.name == "EventSystem") continue;
                UnityEngine.Object.DestroyImmediate(g);
            }
            return backendRoot;
        }

        static void SanitizeBackend(GameObject backend) {
            if (!backend) return;
            foreach (Renderer r in backend.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            foreach (Light l in backend.GetComponentsInChildren<Light>(true)) l.enabled = false;
            foreach (Collider c in backend.GetComponentsInChildren<Collider>(true)) {
                if (!IsInteractiveCollider(c.transform, backend.transform)) c.enabled = false;
            }
        }

        static bool IsInteractiveCollider(Transform t, Transform stop) {
            Transform cur = t;
            while (cur && cur != stop.parent) {
                foreach (MonoBehaviour mb in cur.GetComponents<MonoBehaviour>()) {
                    if (!mb) continue;
                    string n = mb.GetType().Name.ToLowerInvariant();
                    if (n.Contains("interact") || n.Contains("loot") || n.Contains("door") || n.Contains("cabinet") ||
                        n.Contains("storage") || n.Contains("shelter") || n.Contains("stove") || n.Contains("firewood") ||
                        n.Contains("restspot") || n.Contains("bed")) return true;
                }
                if (cur == stop) break;
                cur = cur.parent;
            }
            return false;
        }

        static List<Feature> LoadOsm() {
            string xml = null;
            string cachePath = File.Exists(CacheV86) ? CacheV86 : CacheV88;
            if (File.Exists(cachePath)) {
                try { xml = File.ReadAllText(cachePath); } catch { xml = null; }
            }
            if (string.IsNullOrWhiteSpace(xml) || !xml.Contains("<osm")) {
                string q = "[out:xml][timeout:40];(way[highway](65.414,21.665,65.439,21.721);way[building](65.414,21.665,65.439,21.721);way[leisure=pitch](65.414,21.665,65.439,21.721););out geom;";
                using (WebClient wc = new WebClient()) {
                    wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded; charset=UTF-8";
                    wc.Headers[HttpRequestHeader.UserAgent] = "Rosvik-Blackout-Unity-V88";
                    xml = wc.UploadString(Overpass, "POST", "data=" + Uri.EscapeDataString(q));
                }
                File.WriteAllText(CacheV88, xml);
                AssetDatabase.ImportAsset(CacheV88, ImportAssetOptions.ForceSynchronousImport);
            }
            return ParseOsm(xml);
        }

        static List<Feature> ParseOsm(string xml) {
            XDocument doc = XDocument.Parse(xml);
            List<Feature> list = new List<Feature>();
            foreach (XElement w in doc.Descendants("way")) {
                Feature f = new Feature { id = (string)w.Attribute("id") ?? "" };
                foreach (XElement tag in w.Elements("tag")) {
                    string k = (string)tag.Attribute("k") ?? "", v = (string)tag.Attribute("v") ?? "";
                    if (k == "highway") f.highway = v;
                    else if (k == "building") f.building = v;
                    else if (k == "leisure") f.leisure = v;
                    else if (k == "name") f.name = v;
                    else if (k == "surface") f.surface = v;
                    else if (k == "amenity") f.amenity = v;
                    else if (k == "sport") f.sport = v;
                }
                foreach (XElement nd in w.Elements("nd")) {
                    XAttribute la = nd.Attribute("lat"), lo = nd.Attribute("lon");
                    if (la == null || lo == null) continue;
                    double lat = double.Parse(la.Value, CultureInfo.InvariantCulture);
                    double lon = double.Parse(lo.Value, CultureInfo.InvariantCulture);
                    f.pts.Add(ToLocal(lat, lon));
                }
                if (f.pts.Count >= 2 && (f.IsRoad || f.IsBuilding || f.leisure == "pitch")) list.Add(f);
            }
            return list;
        }

        static Vector2 ToLocal(double lat, double lon) {
            double east = (lon - OriginLon) * 111320.0 * Math.Cos(OriginLat * Math.PI / 180.0);
            double north = (lat - OriginLat) * 110540.0;
            double a = RotationDeg * Math.PI / 180.0;
            double x = east * Math.Cos(a) - north * Math.Sin(a);
            double z = east * Math.Sin(a) + north * Math.Cos(a);
            return new Vector2((float)x, (float)z);
        }

        static Vector3 RawMap(Vector2 p, float y = 0f) => new Vector3(p.x * MapScale, y, p.y * MapScale);
        static Vector3 Map(Vector2 p, float y = 0f) { Vector3 v = RawMap(p, y); return v + mapOffset; }
        static Vector3[] WorldPoints(Feature f) => Clean(f.pts).Select(p => Map(p)).ToArray();

        static Bounds BuildTerrain(Transform parent) {
            Vector3 school = mapOffset;
            Bounds b = new Bounds(new Vector3(0f, 0f, 7.35f), new Vector3(ActiveRadius * 2f, 0f, ActiveRadius * 2f));
            float step = 6f;
            int nx = Mathf.CeilToInt(b.size.x / step) + 1, nz = Mathf.CeilToInt(b.size.z / step) + 1;
            Vector3[] v = new Vector3[nx * nz];
            Vector2[] uv = new Vector2[v.Length];
            int[] tr = new int[(nx - 1) * (nz - 1) * 6];
            for (int z = 0; z < nz; z++) for (int x = 0; x < nx; x++) {
                float wx = b.min.x + x * step, wz = b.min.z + z * step;
                float n = (Mathf.PerlinNoise((wx + 380f) * .018f, (wz - 210f) * .018f) - .5f) * .10f;
                float n2 = (Mathf.PerlinNoise((wx - 20f) * .072f, (wz + 90f) * .072f) - .5f) * .025f;
                int i = z * nx + x;
                v[i] = new Vector3(wx, -.08f + n + n2, wz);
                uv[i] = new Vector2(wx * .03f, wz * .03f);
            }
            int ti = 0;
            for (int z = 0; z < nz - 1; z++) for (int x = 0; x < nx - 1; x++) {
                int i = z * nx + x;
                tr[ti++] = i; tr[ti++] = i + nx; tr[ti++] = i + 1;
                tr[ti++] = i + 1; tr[ti++] = i + nx; tr[ti++] = i + nx + 1;
            }
            Mesh mesh = new Mesh { name = "V88 continuous snow terrain" };
            mesh.indexFormat = v.Length > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.vertices = v; mesh.uv = uv; mesh.triangles = tr; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            mesh = SaveMesh(mesh, Generated + "/v88_terrain.asset");
            GameObject g = MeshObj("continuous snow terrain", parent, mesh, snow, true);
            return b;
        }

        static void BuildRoads(Transform parent) {
            foreach (Feature f in features.Where(x => x.IsRoad)) {
                Vector3 c = Map(Centroid(f.pts));
                if (FlatDistance(c, new Vector3(0, 0, 7.35f)) > ActiveRadius + 40f) continue;
                float width = RoadWidth(f.highway) * MapScale;
                Material mat = (f.surface == "unpaved" || f.highway == "track") ? gravel : asphalt;
                for (int i = 0; i < f.pts.Count - 1; i++) {
                    Vector3 a = Map(f.pts[i], .015f), b = Map(f.pts[i + 1], .015f);
                    if (FlatDistance(a, b) < .08f) continue;
                    SlabBetween("ploughed edge", parent, a - Vector3.up * .02f, b - Vector3.up * .02f, width + 1.0f, snow2, false);
                    SlabBetween(string.IsNullOrEmpty(f.name) ? f.highway : f.name, parent, a, b, width, mat, false);
                    if ((f.highway == "secondary" || f.highway == "tertiary") && FlatDistance(a, b) > 7f) RoadDashes(parent, a, b);
                }
            }
        }

        static void BuildGenericBuildings(Transform parent) {
            int idx = 0;
            foreach (Feature f in features.Where(x => x.IsBuilding && x.id != SchoolId && x.id != ArenaId && x.id != OldSchoolId)) {
                Vector3 center = Map(Centroid(f.pts));
                if (FlatDistance(center, new Vector3(0, 0, 7.35f)) > ActiveRadius) continue;
                Vector3[] pts = WorldPoints(f);
                if (pts.Length < 3) continue;
                Material facade = idx % 4 == 0 ? red : idx % 4 == 1 ? cream : idx % 4 == 2 ? blue : ochre;
                float h = f.building == "school" ? 3.2f : 2.55f + (idx % 5 == 0 ? .25f : 0f);
                GameObject r = new GameObject("OSM building " + f.id + (string.IsNullOrEmpty(f.name) ? "" : " · " + f.name));
                r.transform.SetParent(parent, true);
                PolygonWalls(r.transform, pts, h, facade, true);
                if (f.building == "house" || f.building == "detached" || f.building == "residential") GableRoof(r.transform, pts, h, idx);
                else FlatRoof(r.transform, pts, h, idx);
                AddWindows(r.transform, pts, h, false, idx);
                idx++;
                if (idx >= 190) break;
            }
        }

        static GameObject BuildSchoolFromScratch(Transform parent, Vector3[] pts) {
            GameObject r = new GameObject("ROSVIKS SKOLA · CLEAN OSM V88"); r.transform.SetParent(parent, true);
            float h = 3.25f;
            PolygonWalls(r.transform, pts, h, ochre, true);
            FlatRoof(r.transform, pts, h, 88);
            AddWindows(r.transform, pts, h, true, 88);
            Frame f = MakeFrame(pts);
            Vector3 front = f.c - f.forward * (f.depth * .5f + .12f);
            Box("school entrance vestibule", r.transform, front + f.forward * .35f + Vector3.up * 1.15f,
                new Vector3(5.2f, 2.3f, .7f), brick, true, Yaw(f.yaw));
            Box("entrance canopy", r.transform, front - f.forward * 1.0f + Vector3.up * 2.55f,
                new Vector3(6.7f, .18f, 2.5f), roof, false, Yaw(f.yaw));
            Box("canopy snow", r.transform, front - f.forward * 1.0f + Vector3.up * 2.67f,
                new Vector3(6.45f, .04f, 2.2f), snow, false, Yaw(f.yaw));
            for (int s = -1; s <= 1; s += 2) Box("canopy post", r.transform, front - f.forward * 1.6f + f.right * (s * 2.75f) + Vector3.up * 1.25f,
                new Vector3(.12f, 2.5f, .12f), metal, false);
            WorldLabel(r.transform, "ROSVIKS SKOLA", front - f.forward * .08f + Vector3.up * 2.45f, f.yaw, .15f);
            return r;
        }

        static GameObject BuildIceArenaFromScratch(Transform parent, Vector3[] pts) {
            GameObject r = new GameObject("NORRBOTTEN STÅL ARENA · CLEAN OSM V88"); r.transform.SetParent(parent, true);
            float h = 5.2f;
            PolygonWalls(r.transform, pts, h, blue, true);
            FlatRoof(r.transform, pts, h, 454);
            AddWindows(r.transform, pts, h, false, 454);
            Frame f = MakeFrame(pts);
            Vector3 front = f.c - f.forward * (f.depth * .5f + .10f);
            Box("arena entrance", r.transform, front + Vector3.up * 1.55f, new Vector3(6.0f, 3.1f, .24f), metal, false, Yaw(f.yaw));
            Box("arena canopy", r.transform, front - f.forward * 1.25f + Vector3.up * 3.55f, new Vector3(7.3f, .22f, 2.7f), roof, false, Yaw(f.yaw));
            Box("arena canopy snow", r.transform, front - f.forward * 1.25f + Vector3.up * 3.68f, new Vector3(7.0f, .04f, 2.4f), snow, false, Yaw(f.yaw));
            WorldLabel(r.transform, "NORRBOTTEN STÅL ARENA", front - f.forward * .10f + Vector3.up * 4.05f, f.yaw, .14f);
            return r;
        }

        static void BuildStenskolan(Transform parent, Vector3[] pts) {
            GameObject r = new GameObject("STENSKOLAN · OSM V88"); r.transform.SetParent(parent, true);
            float h = 2.85f; PolygonWalls(r.transform, pts, h, cream, true); GableRoof(r.transform, pts, h, 461); AddWindows(r.transform, pts, h, false, 461);
        }

        static void BuildSchoolInterior(Transform parent, Vector3[] pts) {
            GameObject r = new GameObject("ROSVIKS SKOLA INTERIOR · V88"); r.transform.SetParent(parent, true);
            Frame f = MakeFrame(pts);
            float iw = f.width * .78f, id = f.depth * .74f;
            Box("school interior floor", r.transform, f.c + Vector3.up * .035f, new Vector3(iw, .07f, id), schoolFloor, false, Yaw(f.yaw));
            float corridor = Mathf.Clamp(iw * .22f, 4.0f, 6.2f);
            Box("central corridor", r.transform, f.c + Vector3.up * .075f, new Vector3(corridor, .04f, id * .92f), packed, false, Yaw(f.yaw));
            for (int side = -1; side <= 1; side += 2) {
                float x = side * (corridor * .5f + (iw - corridor) * .25f);
                for (int row = -2; row <= 2; row++) {
                    Vector3 c = LocalToWorld(f, new Vector2(x, row * id * .17f));
                    Box("classroom rug", r.transform, c + Vector3.up * .08f, new Vector3((iw - corridor) * .40f, .025f, id * .14f), row % 2 == 0 ? schoolFloor : wood, false, Yaw(f.yaw));
                    for (int d = -1; d <= 1; d++) {
                        Vector3 desk = c + f.right * (d * 1.25f);
                        Box("school desk", r.transform, desk + Vector3.up * .42f, new Vector3(.95f, .08f, .65f), wood, false, Yaw(f.yaw));
                    }
                }
            }
            // Low cutaway partitions only; no huge opaque walls.
            float wallH = .95f;
            for (int row = -2; row <= 2; row++) {
                float z = row * id * .17f + id * .08f;
                LowWallAcross(r.transform, f, -iw * .5f, -corridor * .5f, z, wallH);
                LowWallAcross(r.transform, f, corridor * .5f, iw * .5f, z, wallH);
            }
            for (int i = -4; i <= 4; i++) {
                Vector3 lp = LocalToWorld(f, new Vector2(corridor * .36f, i * id * .10f));
                Box("locker", r.transform, lp + Vector3.up * .53f, new Vector3(.72f, 1.02f, .28f), i % 3 == 0 ? blue : cream, false, Yaw(f.yaw));
            }
        }

        static void BuildArenaInterior(Transform parent, Vector3[] pts) {
            GameObject r = new GameObject("ICE RINK INTERIOR · V88"); r.transform.SetParent(parent, true);
            Frame f = MakeFrame(pts);
            float rw = Mathf.Min(f.width * .76f, 24f), rd = Mathf.Min(f.depth * .78f, 48f);
            Box("ice", r.transform, f.c + Vector3.up * .035f, new Vector3(rw, .07f, rd), ice, false, Yaw(f.yaw));
            float boardH = .82f;
            RinkBoard(r.transform, f, new Vector2(-rw * .5f, 0), new Vector2(-rw * .5f, rd * .5f), boardH);
            RinkBoard(r.transform, f, new Vector2(-rw * .5f, 0), new Vector2(-rw * .5f, -rd * .5f), boardH);
            RinkBoard(r.transform, f, new Vector2(rw * .5f, 0), new Vector2(rw * .5f, rd * .5f), boardH);
            RinkBoard(r.transform, f, new Vector2(rw * .5f, 0), new Vector2(rw * .5f, -rd * .5f), boardH);
            RinkBoard(r.transform, f, new Vector2(-rw * .5f, rd * .5f), new Vector2(rw * .5f, rd * .5f), boardH);
            RinkBoard(r.transform, f, new Vector2(-rw * .5f, -rd * .5f), new Vector2(rw * .5f, -rd * .5f), boardH);
            RinkLine(r.transform, f, 0f, rw, rinkRed);
            RinkLine(r.transform, f, rd * .25f, rw, rinkBlue);
            RinkLine(r.transform, f, -rd * .25f, rw, rinkBlue);
            for (int s = -1; s <= 1; s += 2) {
                Vector3 goal = LocalToWorld(f, new Vector2(0f, s * rd * .40f));
                Box("goal frame", r.transform, goal + Vector3.up * .45f, new Vector3(3.0f, .08f, .08f), rinkRed, false, Yaw(f.yaw));
            }
            for (int i = 0; i < 4; i++) {
                Vector3 bench = LocalToWorld(f, new Vector2(rw * .5f + 1.2f + i * .45f, 0));
                Box("bleacher", r.transform, bench + Vector3.up * (.20f + i * .18f), new Vector3(.55f, .16f, rd * .55f), wood, false, Yaw(f.yaw));
            }
        }

        static void BuildCampusDetails(Transform parent, Vector3[] school, Vector3[] arena) {
            Frame sf = MakeFrame(school);
            Vector3 entry = sf.c - sf.forward * (sf.depth * .5f + 3.0f);
            Box("school forecourt", parent, entry + Vector3.up * .012f, new Vector3(Mathf.Min(32f, sf.width * .85f), .025f, 7.2f), packed, false, Yaw(sf.yaw));
            for (int i = -2; i <= 2; i++) LampPost(parent, entry + sf.right * (i * 5.4f) - sf.forward * 2.4f);
            for (int i = -4; i <= 4; i++) {
                Vector3 rack = entry + sf.right * (i * .72f) + sf.forward * 1.4f;
                Box("bike rack", parent, rack + Vector3.up * .28f, new Vector3(.06f, .55f, .70f), metal, false, Yaw(sf.yaw + 18f));
            }
            Frame af = MakeFrame(arena);
            Vector3 apron = af.c - af.forward * (af.depth * .5f + 3.0f);
            Box("arena service apron", parent, apron + Vector3.up * .012f, new Vector3(Mathf.Min(22f, af.width * .85f), .025f, 7.5f), packed, false, Yaw(af.yaw));
        }

        static void BuildVegetation(Transform parent, Bounds b) {
            UnityEngine.Random.InitState(8807);
            List<Feature> roads = features.Where(f => f.IsRoad).ToList();
            List<Vector3[]> buildings = features.Where(f => f.IsBuilding).Select(WorldPoints).Where(p => p.Length >= 3).ToList();
            int made = 0;
            for (int tries = 0; tries < 1400 && made < 260; tries++) {
                Vector3 p = new Vector3(UnityEngine.Random.Range(-ActiveRadius, ActiveRadius), 0f, 7.35f + UnityEngine.Random.Range(-ActiveRadius, ActiveRadius));
                if (NearRoad(p, roads, 4.5f)) continue;
                bool inside = false;
                foreach (Vector3[] poly in buildings) { if (PointInPoly(p, poly, 2.0f)) { inside = true; break; } }
                if (inside) continue;
                float s = UnityEngine.Random.Range(.72f, 1.35f);
                if (UnityEngine.Random.value < .82f) Pine(parent, p, s);
                else Bush(parent, p, s);
                made++;
            }
        }

        static bool NearRoad(Vector3 p, List<Feature> roads, float d) {
            float d2 = d * d;
            foreach (Feature f in roads) {
                Vector3 c = Map(Centroid(f.pts));
                if (FlatDistance(c, p) > 75f) continue;
                for (int i = 0; i < f.pts.Count - 1; i++) {
                    Vector3 a = Map(f.pts[i]), b = Map(f.pts[i + 1]);
                    if (SegmentDistanceSqr(p, a, b) < d2) return true;
                }
            }
            return false;
        }

        static void BuildLighting(Transform parent) {
            GameObject sun = new GameObject("V88 winter sky light"); sun.transform.SetParent(parent, true);
            Light l = sun.AddComponent<Light>(); l.type = LightType.Directional; l.intensity = .66f; l.color = Hex("d4e0e3");
            sun.transform.rotation = Quaternion.Euler(52f, -34f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Hex("6f7e82");
            RenderSettings.fog = true;
            RenderSettings.fogColor = Hex("7b8b91");
            RenderSettings.fogDensity = .0058f;
        }

        static Shader PickKnownGoodShader() {
            foreach (Renderer r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                if (!r || !r.sharedMaterial || !r.sharedMaterial.shader) continue;
                Shader s = r.sharedMaterial.shader;
                if (s.isSupported && !s.name.ToLowerInvariant().Contains("error")) return s;
            }
            Shader fallback = Shader.Find("Universal Render Pipeline/Lit");
            if (!fallback || !fallback.isSupported) fallback = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (!fallback || !fallback.isSupported) fallback = Shader.Find("Standard");
            if (!fallback || !fallback.isSupported) throw new Exception("No supported shader found");
            return fallback;
        }

        static void BuildMaterials() {
            snow = Mat("snow", "cbd6da", .18f); snow2 = Mat("snow_edge", "b7c5ca", .12f); packed = Mat("packed_snow", "81949b", .08f);
            asphalt = Mat("asphalt", "30393d", .06f); gravel = Mat("gravel", "686c69", .05f); forest = Mat("pine", "314941", .05f); trunk = Mat("trunk", "493b31", .04f);
            red = Mat("red_facade", "7a4540", .08f); ochre = Mat("school_ochre", "a37943", .10f); blue = Mat("arena_blue", "3e6071", .12f); cream = Mat("cream", "c2b99f", .10f);
            brick = Mat("brick", "6a4038", .08f); trim = Mat("trim", "d5d0c2", .18f); glass = Mat("glass", "344c57", .35f); warmGlass = Emissive("warm_glass", "efb96b", 1.15f);
            roof = Mat("roof", "30414a", .18f); metal = Mat("metal", "3f4b4f", .28f); wood = Mat("wood", "7a5b43", .12f);
            schoolFloor = Mat("school_floor", "66736d", .10f); hallFloor = Mat("hall_floor", "76543d", .12f); ice = Mat("ice", "aebfc6", .42f);
            rinkBlue = Mat("rink_blue", "477da5", .08f); rinkRed = Mat("rink_red", "9b4a46", .08f); white = Mat("white", "d7d8d0", .08f);
        }

        static Material Mat(string name, string hex, float smooth) {
            string path = Generated + "/" + name + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            if (m.shader != shader) m.shader = shader;
            Color c = Hex(hex);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            EditorUtility.SetDirty(m); return m;
        }

        static Material Emissive(string name, string hex, float intensity) {
            Material m = Mat(name, hex, .16f);
            if (m.HasProperty("_EmissionColor")) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Hex(hex) * intensity); }
            return m;
        }

        static void PolygonWalls(Transform parent, Vector3[] pts, float h, Material facade, bool col) {
            for (int i = 0; i < pts.Length; i++) {
                Vector3 a = pts[i] + Vector3.up * (h * .5f), b = pts[(i + 1) % pts.Length] + Vector3.up * (h * .5f);
                Vector3 d = b - a; float len = new Vector2(d.x, d.z).magnitude; if (len < .1f) continue;
                float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                Box("wall", parent, (a + b) * .5f, new Vector3(.20f, h, len + .02f), facade, col, Quaternion.Euler(0, yaw, 0));
                Box("brick plinth", parent, new Vector3((a.x + b.x) * .5f, .34f, (a.z + b.z) * .5f), new Vector3(.23f, .65f, len + .04f), brick, false, Quaternion.Euler(0, yaw, 0));
            }
        }

        static void AddWindows(Transform parent, Vector3[] pts, float h, bool warmSparse, int seed) {
            for (int i = 0; i < pts.Length; i++) {
                Vector3 a = pts[i], b = pts[(i + 1) % pts.Length]; Vector3 d = b - a; d.y = 0;
                float len = d.magnitude; if (len < 4.4f) continue;
                Vector3 dir = d / len; float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                int n = Mathf.Clamp(Mathf.FloorToInt(len / 5.2f), 1, 5);
                for (int k = 1; k <= n; k++) {
                    Vector3 p = Vector3.Lerp(a, b, k / (float)(n + 1)); p.y = h * .58f;
                    Material pane = warmSparse && ((k + i + seed) % 4 == 0) ? warmGlass : glass;
                    Box("window frame", parent, p, new Vector3(.18f, 1.25f, 1.75f), trim, false, Quaternion.Euler(0, yaw, 0));
                    Box("window pane", parent, p + Quaternion.Euler(0, yaw, 0) * new Vector3(.02f, 0, 0), new Vector3(.20f, .96f, 1.45f), pane, false, Quaternion.Euler(0, yaw, 0));
                }
            }
        }

        static void GableRoof(Transform parent, Vector3[] pts, float eave, int seed) {
            Frame f = MakeFrame(pts); float pitch = 18f; float rad = pitch * Mathf.Deg2Rad;
            float half = f.width * .5f + .25f; float len = half / Mathf.Cos(rad); float rise = half * Mathf.Tan(rad);
            for (int side = -1; side <= 1; side += 2) {
                Vector3 pos = f.c + f.right * (side * half * .5f) + Vector3.up * (eave + rise * .5f);
                Quaternion q = Yaw(f.yaw) * Quaternion.AngleAxis(side > 0 ? -pitch : pitch, Vector3.forward);
                Box("roof", parent, pos, new Vector3(len + .1f, .18f, f.depth + .55f), roof, false, q);
                Box("roof snow", parent, pos + Vector3.up * .11f, new Vector3(len - .04f, .04f, f.depth + .30f), snow, false, q);
            }
        }

        static void FlatRoof(Transform parent, Vector3[] pts, float h, int seed) {
            Frame f = MakeFrame(pts);
            Box("low roof", parent, f.c + Vector3.up * (h + .13f), new Vector3(f.width + .45f, .22f, f.depth + .45f), roof, false, Yaw(f.yaw));
            Box("roof snow", parent, f.c + Vector3.up * (h + .27f), new Vector3(f.width + .18f, .05f, f.depth + .18f), snow, false, Yaw(f.yaw));
        }

        static Frame MakeFrame(Vector3[] pts) {
            Vector3 c = Vector3.zero; foreach (Vector3 p in pts) c += p; c /= Mathf.Max(1, pts.Length);
            Vector3 best = Vector3.right; float bestLen = -1f;
            for (int i = 0; i < pts.Length; i++) { Vector3 d = pts[(i + 1) % pts.Length] - pts[i]; d.y = 0; if (d.sqrMagnitude > bestLen) { bestLen = d.sqrMagnitude; best = d.normalized; } }
            Vector3 right = best; Vector3 forward = new Vector3(-right.z, 0, right.x).normalized;
            float minR = 99999, maxR = -99999, minF = 99999, maxF = -99999;
            foreach (Vector3 p in pts) { Vector3 d = p - c; float r = Vector3.Dot(d, right), f = Vector3.Dot(d, forward); minR = Mathf.Min(minR, r); maxR = Mathf.Max(maxR, r); minF = Mathf.Min(minF, f); maxF = Mathf.Max(maxF, f); }
            return new Frame { c = c, right = right, forward = forward, width = maxR - minR, depth = maxF - minF, yaw = Mathf.Atan2(right.x, right.z) * Mathf.Rad2Deg - 90f };
        }

        static Vector3 LocalToWorld(Frame f, Vector2 local) => f.c + f.right * local.x + f.forward * local.y;
        static void LowWallAcross(Transform p, Frame f, float a, float b, float z, float h) {
            Vector3 wa = LocalToWorld(f, new Vector2(a, z)), wb = LocalToWorld(f, new Vector2(b, z));
            SlabWall(p, wa, wb, h, cream);
        }
        static void SlabWall(Transform p, Vector3 a, Vector3 b, float h, Material m) {
            Vector3 d = b - a; float len = FlatDistance(a, b); if (len < .1f) return; float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            Box("low wall", p, (a + b) * .5f + Vector3.up * (h * .5f), new Vector3(.16f, h, len), m, false, Quaternion.Euler(0, yaw, 0));
        }

        static void RinkBoard(Transform p, Frame f, Vector2 a, Vector2 b, float h) {
            Vector3 wa = LocalToWorld(f, a), wb = LocalToWorld(f, b); SlabWall(p, wa, wb, h, white);
        }
        static void RinkLine(Transform p, Frame f, float z, float width, Material mat) {
            Vector3 c = LocalToWorld(f, new Vector2(0, z)); Box("rink line", p, c + Vector3.up * .085f, new Vector3(width * .96f, .018f, .11f), mat, false, Yaw(f.yaw));
        }

        static Vector3 SchoolEntrancePoint(Vector3[] pts) {
            Frame f = MakeFrame(pts);
            return f.c - f.forward * (f.depth * .5f + 3.0f) + Vector3.up * .07f;
        }

        static void LampPost(Transform p, Vector3 pos) {
            Cylinder("lamp post", p, pos + Vector3.up * 1.55f, .045f, 3.1f, metal, false);
            Box("lamp head", p, pos + new Vector3(.18f, 3.0f, 0), new Vector3(.45f, .13f, .20f), metal, false);
        }

        static void RoadDashes(Transform p, Vector3 a, Vector3 b) {
            Vector3 d = b - a; d.y = 0; float len = d.magnitude; if (len < 6f) return; Vector3 dir = d / len; float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            for (float t = 4f; t < len - 2f; t += 9f) Box("road dash", p, a + dir * t + Vector3.up * .024f, new Vector3(.08f, .012f, 2.6f), trim, false, Quaternion.Euler(0, yaw, 0));
        }

        static float RoadWidth(string h) {
            switch (h) { case "secondary": return 6.5f; case "tertiary": return 5.8f; case "residential": return 4.8f; case "service": return 3.3f; case "cycleway": return 2.1f; case "footway": return 1.4f; case "track": return 2.7f; default: return 3.2f; }
        }

        static void Pine(Transform p, Vector3 pos, float s) {
            GameObject r = new GameObject("pine"); r.transform.SetParent(p, true); r.transform.position = pos; r.transform.localScale = Vector3.one * s;
            Cylinder("trunk", r.transform, new Vector3(0, .72f, 0), .13f, 1.45f, trunk, false);
            for (int i = 0; i < 3; i++) {
                float y = .72f + i * .58f, rad = 1.05f - i * .20f, hh = .95f - i * .08f;
                Cone("needles", r.transform, new Vector3(0, y, 0), rad, hh, forest);
                Cone("snow", r.transform, new Vector3(0, y + .08f, 0), rad * .90f, hh * .24f, snow2);
            }
        }

        static void Bush(Transform p, Vector3 pos, float s) {
            GameObject r = new GameObject("winter shrub"); r.transform.SetParent(p, true); r.transform.position = pos; r.transform.localScale = Vector3.one * s;
            for (int i = 0; i < 3; i++) {
                GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere); g.name = "branch mass"; g.transform.SetParent(r.transform, false);
                g.transform.localPosition = new Vector3((i - 1) * .34f, .23f + (i % 2) * .09f, i % 2 == 0 ? .12f : -.12f);
                g.transform.localScale = new Vector3(.62f, .35f, .50f); g.GetComponent<Renderer>().sharedMaterial = forest; UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
            }
        }

        static bool PointInPoly(Vector3 p, Vector3[] poly, float padding) {
            bool inside = false; int j = poly.Length - 1;
            for (int i = 0; i < poly.Length; i++) {
                float xi = poly[i].x, zi = poly[i].z, xj = poly[j].x, zj = poly[j].z;
                bool hit = ((zi > p.z) != (zj > p.z)) && (p.x < (xj - xi) * (p.z - zi) / ((zj - zi) + 0.00001f) + xi);
                if (hit) inside = !inside; j = i;
            }
            if (inside) return true;
            for (int i = 0; i < poly.Length; i++) if (SegmentDistanceSqr(p, poly[i], poly[(i + 1) % poly.Length]) < padding * padding) return true;
            return false;
        }

        static float SegmentDistanceSqr(Vector3 p, Vector3 a, Vector3 b) {
            Vector2 pp = new Vector2(p.x, p.z), aa = new Vector2(a.x, a.z), bb = new Vector2(b.x, b.z), ab = bb - aa;
            float t = ab.sqrMagnitude < .0001f ? 0f : Mathf.Clamp01(Vector2.Dot(pp - aa, ab) / ab.sqrMagnitude);
            return (pp - (aa + ab * t)).sqrMagnitude;
        }

        static Bounds BoundsOf(Vector3[] pts) { Bounds b = new Bounds(pts[0], Vector3.zero); foreach (Vector3 p in pts) b.Encapsulate(p); return b; }
        static Vector2 Centroid(List<Vector2> pts) { Vector2 c = Vector2.zero; int n = 0; foreach (Vector2 p in pts) { c += p; n++; } return n == 0 ? Vector2.zero : c / n; }
        static List<Vector2> Clean(List<Vector2> pts) { List<Vector2> r = new List<Vector2>(pts); if (r.Count > 2 && Vector2.Distance(r[0], r[r.Count - 1]) < .05f) r.RemoveAt(r.Count - 1); return r; }
        static float FlatDistance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        static Quaternion Yaw(float yaw) => Quaternion.Euler(0, yaw, 0);

        static Transform Group(Transform p, string name) { GameObject g = new GameObject(name); g.transform.SetParent(p, false); return g.transform; }
        static GameObject Box(string name, Transform p, Vector3 pos, Vector3 scale, Material mat, bool col, Quaternion? rot = null) {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = name; g.transform.SetParent(p, true); g.transform.position = pos; g.transform.rotation = rot ?? Quaternion.identity; g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = mat; if (!col) UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>()); return g;
        }
        static void SlabBetween(string name, Transform p, Vector3 a, Vector3 b, float width, Material mat, bool col) {
            Vector3 d = b - a; d.y = 0; float len = d.magnitude; if (len < .02f) return; float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            Box(name, p, (a + b) * .5f, new Vector3(width, .045f, len + width * .10f), mat, col, Quaternion.Euler(0, yaw, 0));
        }
        static void Cylinder(string name, Transform p, Vector3 pos, float rad, float h, Material mat, bool col) {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cylinder); g.name = name; g.transform.SetParent(p, true); g.transform.position = pos; g.transform.localScale = new Vector3(rad * 2f, h * .5f, rad * 2f); g.GetComponent<Renderer>().sharedMaterial = mat; if (!col) UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        }
        static void Cone(string name, Transform p, Vector3 lp, float rad, float h, Material mat) {
            GameObject g = new GameObject(name); g.transform.SetParent(p, false); g.transform.localPosition = lp; g.transform.localScale = new Vector3(rad * 2f, h, rad * 2f);
            MeshFilter mf = g.AddComponent<MeshFilter>(); mf.sharedMesh = coneMesh; g.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }
        static GameObject MeshObj(string name, Transform p, Mesh mesh, Material mat, bool col) {
            GameObject g = new GameObject(name); g.transform.SetParent(p, true); MeshFilter mf = g.AddComponent<MeshFilter>(); mf.sharedMesh = mesh; g.AddComponent<MeshRenderer>().sharedMaterial = mat;
            if (col) { MeshCollider mc = g.AddComponent<MeshCollider>(); mc.sharedMesh = mesh; } return g;
        }

        static Mesh EnsureConeMesh() {
            string path = Generated + "/cone.asset"; Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (existing) return existing;
            Mesh m = new Mesh { name = "V88 cone" }; int seg = 18; List<Vector3> v = new List<Vector3> { new Vector3(0, .5f, 0), new Vector3(0, -.5f, 0) }; List<int> tr = new List<int>();
            for (int i = 0; i < seg; i++) { float a = i * Mathf.PI * 2f / seg; v.Add(new Vector3(Mathf.Cos(a) * .5f, -.5f, Mathf.Sin(a) * .5f)); }
            for (int i = 0; i < seg; i++) { int a = 2 + i, b = 2 + (i + 1) % seg; tr.Add(0); tr.Add(a); tr.Add(b); tr.Add(1); tr.Add(b); tr.Add(a); }
            m.SetVertices(v); m.SetTriangles(tr, 0); m.RecalculateNormals(); m.RecalculateBounds(); AssetDatabase.CreateAsset(m, path); return m;
        }

        static Mesh SaveMesh(Mesh mesh, string path) {
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing) { EditorUtility.CopySerialized(mesh, existing); UnityEngine.Object.DestroyImmediate(mesh); EditorUtility.SetDirty(existing); return existing; }
            AssetDatabase.CreateAsset(mesh, path); return mesh;
        }

        static void WorldLabel(Transform parent, string text, Vector3 pos, float yaw, float size) {
            GameObject anchor = new GameObject("label anchor " + text); anchor.transform.SetParent(parent, true); anchor.transform.position = pos; anchor.transform.rotation = Quaternion.Euler(0, yaw + 180f, 0);
            TextMesh tm = anchor.AddComponent<TextMesh>(); tm.text = text; tm.fontSize = 48; tm.characterSize = size; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.color = new Color(.93f, .92f, .86f, 1f);
        }

        static Color Hex(string s) { Color c = Color.white; ColorUtility.TryParseHtmlString("#" + s, out c); return c; }
        static void CleanupMissingScripts() { foreach (GameObject g in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (g && g.scene.IsValid()) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(g); }
    }
}
#endif