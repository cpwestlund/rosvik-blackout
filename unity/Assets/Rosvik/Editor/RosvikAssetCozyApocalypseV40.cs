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
    /// V40 starts the real cozy-apocalypse art direction without rebuilding Rosvik.
    /// V39 roads/geography/gameplay stay locked. Visible clutter is deliberately asset-led:
    /// KayKit textured meshes replace the old habit of creating every prop from cubes.
    /// The pass adds restrained service clutter, household objects, roadside vehicles and a
    /// few warm battery/generator light islands while keeping most of the blackout dark.
    /// </summary>
    [InitializeOnLoad]
    public static class RosvikAssetCozyApocalypseV40 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_ASSET_COZY_APOCALYPSE_V40_VERSION";
        const int Version = 40;
        const string RequiredRoot = "ROSVIK_CLEAN_ROAD_NETWORK_V39";
        const string RootName = "ROSVIK_ASSET_COZY_APOCALYPSE_V40";
        const string GroupName = "22 ASSET COZY APOCALYPSE V40";
        const string GeneratedDir = "Assets/Rosvik/GeneratedV40";
        const string AssetRoot = "Assets/Rosvik/ThirdParty/V40CozyBits";
        const string ProvenMaterial = "Assets/Rosvik/GeneratedV20/mat_ground.mat";
        const string TexturePath = AssetRoot + "/citybits_texture.png";
        const long MainSchoolWay = 163199458;
        const long OldSchoolWay = 163199461;
        const long ArenaWay = 163199454;

        struct Source {
            public string file, url;
            public Source(string file, string url) { this.file = file; this.url = url; }
        }

        struct RoadHit {
            public bool valid;
            public Vector3 point;
            public Vector3 dir;
            public float distance;
            public float halfWidth;
        }

        static readonly Source[] Sources = {
            new Source("dumpster.obj", "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-City-Builder-Bits-1.0/main/addons/kaykit_city_builder_bits/Assets/obj/dumpster.obj"),
            new Source("trash_A.obj", "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-City-Builder-Bits-1.0/main/addons/kaykit_city_builder_bits/Assets/obj/trash_A.obj"),
            new Source("trash_B.obj", "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-City-Builder-Bits-1.0/main/addons/kaykit_city_builder_bits/Assets/obj/trash_B.obj"),
            new Source("box_A.obj", "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-City-Builder-Bits-1.0/main/addons/kaykit_city_builder_bits/Assets/obj/box_A.obj"),
            new Source("box_B.obj", "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-City-Builder-Bits-1.0/main/addons/kaykit_city_builder_bits/Assets/obj/box_B.obj"),
            new Source("bench.obj", "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-City-Builder-Bits-1.0/main/addons/kaykit_city_builder_bits/Assets/obj/bench.obj"),
            new Source("streetlight.obj", "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-City-Builder-Bits-1.0/main/addons/kaykit_city_builder_bits/Assets/obj/streetlight.obj"),
            new Source("car_hatchback.obj", "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-City-Builder-Bits-1.0/main/addons/kaykit_city_builder_bits/Assets/obj/car_hatchback.obj"),
            new Source("car_sedan.obj", "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-City-Builder-Bits-1.0/main/addons/kaykit_city_builder_bits/Assets/obj/car_sedan.obj"),
            new Source("car_stationwagon.obj", "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-City-Builder-Bits-1.0/main/addons/kaykit_city_builder_bits/Assets/obj/car_stationwagon.obj")
        };

        const string TextureUrl = "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-City-Builder-Bits-1.0/main/addons/kaykit_city_builder_bits/Assets/obj/citybits_texture.png";

        static RosvikAssetCozyApocalypseV40() {
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        [MenuItem("Rosvik/Rebuild Asset Cozy Apocalypse V40")]
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
                EnsureAssets();
                Directory.CreateDirectory(GeneratedDir);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                Transform old = Find(root.transform, GroupName);
                if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
                Transform group = NewGroup(root.transform, GroupName);

                List<RosvikOsmV15.Way> ways = RosvikOsmV15.LoadWays();
                if (ways == null || ways.Count == 0) throw new InvalidOperationException("V40 could not load Rosvik OSM data.");
                RosvikOsmV15.Way schoolWay = ways.FirstOrDefault(w => w.Id == MainSchoolWay);
                if (schoolWay == null) throw new InvalidOperationException("V40 could not locate Rosviks skola.");
                Vector3 school = RosvikOsmV15.Centroid(schoolWay);

                Texture2D cityTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
                Material cityMat = TexturedMat("kaykit_citybits", cityTexture, .22f);

                GameObject dumpster = Model("dumpster.obj");
                GameObject trashA = Model("trash_A.obj");
                GameObject trashB = Model("trash_B.obj");
                GameObject boxA = Model("box_A.obj");
                GameObject boxB = Model("box_B.obj");
                GameObject bench = Model("bench.obj");
                GameObject lamp = Model("streetlight.obj");
                GameObject hatch = Model("car_hatchback.obj");
                GameObject sedan = Model("car_sedan.obj");
                GameObject wagon = Model("car_stationwagon.obj");

                int schoolProps = BuildSchoolServiceStory(group, root.transform, ways, school, dumpster, trashA, boxA, boxB, bench, lamp, cityMat);
                int yardProps = BuildResidentialLife(group, root.transform, ways, school, trashA, trashB, boxA, boxB, dumpster, cityMat);
                int cars = BuildRoadsideVehicles(group, ways, school, new [] { sedan, wagon, hatch }, cityMat);
                int lights = BuildWarmIslands(group, root.transform, ways, school);

                root.name = RootName;
                EditorPrefs.SetInt(Key, Version);
                Selection.activeObject = null;
                EditorUtility.SetDirty(root);
                AssetDatabase.SaveAssets();
                if (!Busy()) {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene, ScenePath);
                }
                Debug.Log("ROSVIK V40: asset-led cozy apocalypse layer added — " + schoolProps + " school service props, " +
                          yardProps + " residential props, " + cars + " vehicles and " + lights +
                          " restrained warm light islands. V39 roads/geography/gameplay unchanged.");
            } catch (Exception ex) {
                Debug.LogError("ROSVIK V40 FAILED: " + ex);
            }
        }

        static int BuildSchoolServiceStory(Transform parent, Transform root, List<RosvikOsmV15.Way> ways, Vector3 school,
            GameObject dumpster, GameObject trash, GameObject boxA, GameObject boxB, GameObject bench, GameObject lamp, Material cityMat) {

            Transform court = Find(root, "school entrance court");
            Vector3 center = school;
            Vector3 right = Vector3.right;
            Vector3 forward = Vector3.forward;
            float halfX = 5.5f, halfZ = 4.0f;
            if (court) {
                Bounds b = BoundsOf(court.gameObject);
                center = b.center; center.y = .04f;
                right = Flat(court.right).normalized;
                forward = Flat(court.forward).normalized;
                if (right.sqrMagnitude < .1f) right = Vector3.right;
                if (forward.sqrMagnitude < .1f) forward = Vector3.forward;
                halfX = Mathf.Max(3.5f, b.extents.x);
                halfZ = Mathf.Max(3.0f, b.extents.z);
            }

            int made = 0;
            Vector3 service = center + right * (halfX + 1.45f) + forward * .35f;
            if (!InsideBuilding(service, ways)) {
                if (dumpster && PlaceModel(dumpster, parent, "asset service dumpster", service, Yaw(forward) + 90f, 1.10f, cityMat)) made++;
                if (boxA && PlaceModel(boxA, parent, "asset supply crate", service - right * 1.30f + forward * .55f, 18f, .62f, cityMat)) made++;
                if (boxB && PlaceModel(boxB, parent, "asset supply crate", service - right * 1.72f - forward * .15f, -11f, .48f, cityMat)) made++;
                if (trash && PlaceModel(trash, parent, "asset service bin", service + right * 1.38f - forward * .15f, 4f, .78f, cityMat)) made++;
            }

            // A real mesh bench/lamp cluster is only added if V28 did not already put one very close.
            Vector3 quiet = center - right * (halfX - .8f) - forward * (halfZ - .6f);
            if (bench && !HasNamedObjectNear(root, "bench", quiet, 2.4f) && !InsideBuilding(quiet, ways)) {
                if (PlaceModel(bench, parent, "asset waiting bench", quiet, Yaw(forward), .92f, cityMat)) made++;
            }
            Vector3 lightPos = center + right * (halfX - .55f) - forward * (halfZ - .35f);
            if (lamp && !HasNamedObjectNear(root, "lamp", lightPos, 2.4f) && !InsideBuilding(lightPos, ways)) {
                GameObject go = PlaceModel(lamp, parent, "asset school lamp", lightPos, Yaw(forward), 4.8f, cityMat);
                if (go) made++;
            }
            return made;
        }

        static int BuildResidentialLife(Transform parent, Transform root, List<RosvikOsmV15.Way> ways, Vector3 school,
            GameObject trashA, GameObject trashB, GameObject boxA, GameObject boxB, GameObject dumpster, Material cityMat) {

            List<RosvikOsmV15.Way> roads = ways.Where(IsVehicleRoad).ToList();
            List<RosvikOsmV15.Way> houses = ways.Where(IsHouse)
                .Where(w => w.Id != MainSchoolWay && w.Id != OldSchoolWay && w.Id != ArenaWay)
                .OrderBy(w => Flat(RosvikOsmV15.Centroid(w) - school).sqrMagnitude)
                .Where(w => {
                    float d = Flat(RosvikOsmV15.Centroid(w) - school).magnitude;
                    return d > 24f && d < 175f;
                }).Take(34).ToList();

            int made = 0, housesDressed = 0;
            foreach (RosvikOsmV15.Way house in houses) {
                if (housesDressed >= 14) break;
                if ((house.Id % 3) == 1) continue;

                Vector3 hc = RosvikOsmV15.Centroid(house);
                RoadHit hit = NearestRoad(hc, roads);
                if (!hit.valid || hit.distance > 26f) continue;
                List<Vector3> pts = Points(house);
                Vector3 front = ClosestPointOnPolygon(pts, hit.point);
                Vector3 towardRoad = Flat(hit.point - front);
                if (towardRoad.sqrMagnitude < .05f) continue;
                towardRoad.Normalize();
                Vector3 tangent = new Vector3(-towardRoad.z, 0f, towardRoad.x);

                Vector3 p = front + towardRoad * 1.05f + tangent * (((house.Id & 1) == 0) ? 1.15f : -1.15f);
                p.y = .04f;
                if (InsideBuilding(p, ways) || DistanceToRoad(p, roads) < hit.halfWidth + .55f) continue;

                GameObject bin = ((house.Id & 2) == 0) ? trashA : trashB;
                if (bin && PlaceModel(bin, parent, "asset household bin", p, Yaw(hit.dir), .80f, cityMat)) made++;

                if ((house.Id % 4) == 0) {
                    GameObject crate = ((house.Id & 4) == 0) ? boxA : boxB;
                    Vector3 cp = p - tangent * .72f - towardRoad * .18f;
                    if (crate && !InsideBuilding(cp, ways) && PlaceModel(crate, parent, "asset porch crate", cp, (float)(house.Id % 41) - 20f, .46f, cityMat)) made++;
                }

                // Dumpster only at a small minority of homes and only where the road setback is generous.
                if ((house.Id % 13) == 0 && dumpster && hit.distance > 10f) {
                    Vector3 dp = p + tangent * 1.45f - towardRoad * .35f;
                    if (!InsideBuilding(dp, ways) && DistanceToRoad(dp, roads) > hit.halfWidth + .65f)
                        if (PlaceModel(dumpster, parent, "asset temporary dumpster", dp, Yaw(hit.dir) + 90f, 1.05f, cityMat)) made++;
                }
                housesDressed++;
            }
            return made;
        }

        static int BuildRoadsideVehicles(Transform parent, List<RosvikOsmV15.Way> ways, Vector3 school, GameObject[] cars, Material cityMat) {
            if (cars == null || cars.All(c => !c)) return 0;
            List<RosvikOsmV15.Way> roads = ways.Where(IsVehicleRoad).ToList();
            List<RosvikOsmV15.Way> buildings = ways.Where(w => w.Closed && !string.IsNullOrEmpty(w.Tag("building")) && w.Tag("building") != "no").ToList();
            int made = 0, sample = 0;

            foreach (RosvikOsmV15.Way road in roads.OrderBy(w => Flat(RosvikOsmV15.Centroid(w) - school).sqrMagnitude)) {
                if (made >= 5) break;
                string h = road.Tag("highway");
                if (h == "service" && (sample++ % 2) != 0) continue;
                for (int i = 0; i < road.Nodes.Count - 1 && made < 5; i++) {
                    Vector3 a = road.Nodes[i].Pos, b = road.Nodes[i + 1].Pos;
                    Vector3 d = Flat(b - a); float len = d.magnitude;
                    if (len < 15f) continue;
                    d /= len;
                    Vector3 mid = Vector3.Lerp(a, b, .58f);
                    float fromSchool = Flat(mid - school).magnitude;
                    if (fromSchool < 38f || fromSchool > 185f) continue;
                    if (((i + road.Id) & 3) != 0) continue;

                    float hw = RoadHalfWidth(road);
                    Vector3 normal = new Vector3(-d.z, 0f, d.x);
                    float side = ((road.Id + i) & 1) == 0 ? 1f : -1f;
                    Vector3 p = mid + normal * side * Mathf.Max(1.55f, hw - .55f);
                    p.y = .045f;
                    if (DistanceToBuilding(p, buildings) < 2.8f) continue;

                    GameObject car = cars[made % cars.Length];
                    if (!car) car = cars.FirstOrDefault(c => c);
                    if (!car) break;
                    float yaw = Yaw(d) + ((made % 3 == 0) ? 5.5f : -2.5f);
                    if (PlaceModel(car, parent, made == 0 ? "asset abandoned estate car" : "asset parked village car", p, yaw, 1.42f, cityMat)) made++;
                }
            }
            return made;
        }

        static int BuildWarmIslands(Transform parent, Transform root, List<RosvikOsmV15.Way> ways, Vector3 school) {
            int made = 0;
            Transform station = Find(root, "reserve power cabinet");
            if (station) {
                AddWarmLight(parent, "battery lantern glow", station.position + Vector3.up * 1.25f, 4.6f, .82f);
                made++;
            }

            // A few homes retain weak local light. Most doors remain dark so the blackout still reads.
            List<Transform> doors = root.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name == "front door")
                .Where(t => Flat(t.position - school).magnitude > 34f && Flat(t.position - school).magnitude < 145f)
                .OrderBy(t => Flat(t.position - school).sqrMagnitude)
                .ToList();
            for (int i = 0; i < doors.Count && made < 4; i += 5) {
                Transform door = doors[i];
                Vector3 p = door.position + Vector3.up * .82f;
                AddWarmLight(parent, "weak porch battery light", p, 4.1f, .58f);
                made++;
            }
            return made;
        }

        static void AddWarmLight(Transform parent, string name, Vector3 pos, float range, float intensity) {
            GameObject g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.transform.position = pos;
            Light l = g.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, .64f, .31f);
            l.range = range;
            l.intensity = intensity;
            l.shadows = LightShadows.Soft;
        }

        static void EnsureAssets() {
            string dir = FullPath(AssetRoot);
            Directory.CreateDirectory(dir);
            using (WebClient wc = new WebClient()) {
                wc.Headers[HttpRequestHeader.UserAgent] = "RosvikBlackout-UnityEditor";
                foreach (Source s in Sources) {
                    string assetPath = AssetRoot + "/" + s.file;
                    string full = FullPath(assetPath);
                    if (File.Exists(full) && new FileInfo(full).Length > 100) continue;
                    try {
                        string text = wc.DownloadString(s.url);
                        File.WriteAllText(full, StripMaterials(text));
                    } catch (Exception ex) {
                        Debug.LogWarning("V40 asset download failed for " + s.file + ": " + ex.Message);
                    }
                }
                string textureFull = FullPath(TexturePath);
                if (!File.Exists(textureFull) || new FileInfo(textureFull).Length < 1000) {
                    try { wc.DownloadFile(TextureUrl, textureFull); }
                    catch (Exception ex) { Debug.LogWarning("V40 texture download failed: " + ex.Message); }
                }
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (Source s in Sources) {
                string p = AssetRoot + "/" + s.file;
                if (File.Exists(FullPath(p))) AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            }
            if (File.Exists(FullPath(TexturePath))) AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        static string StripMaterials(string text) {
            return string.Join("\n", text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')
                .Where(l => !l.StartsWith("mtllib ", StringComparison.OrdinalIgnoreCase) &&
                            !l.StartsWith("usemtl ", StringComparison.OrdinalIgnoreCase)));
        }

        static GameObject Model(string file) => AssetDatabase.LoadAssetAtPath<GameObject>(AssetRoot + "/" + file);

        static Material TexturedMat(string name, Texture2D texture, float smoothness) {
            string path = GeneratedDir + "/mat_" + name + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = ResolveShader();
            if (!m) { m = new Material(shader) { name = "V40 " + name }; AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            Color white = Color.white;
            m.color = white;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", white);
            if (m.HasProperty("_Color")) m.SetColor("_Color", white);
            if (texture) {
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", texture);
                if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", texture);
            }
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
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

        static GameObject PlaceModel(GameObject asset, Transform parent, string name, Vector3 pos, float yaw, float targetHeight, Material mat) {
            if (!asset) return null;
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            if (!go) go = UnityEngine.Object.Instantiate(asset, parent);
            go.name = name;
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one;
            Bounds before = BoundsOf(go);
            float h = Mathf.Max(.01f, before.size.y);
            float scale = targetHeight / h;
            go.transform.localScale = Vector3.one * scale;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true)) {
                r.sharedMaterial = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                r.receiveShadows = true;
            }
            foreach (Collider c in go.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
            Bounds after = BoundsOf(go);
            Vector3 p = go.transform.position;
            p.y += .035f - after.min.y;
            go.transform.position = p;
            return go;
        }

        static bool HasNamedObjectNear(Transform root, string token, Vector3 p, float distance) {
            float sq = distance * distance;
            return root.GetComponentsInChildren<Transform>(true).Any(t =>
                t.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0 && Flat(t.position - p).sqrMagnitude < sq);
        }

        static bool IsVehicleRoad(RosvikOsmV15.Way w) {
            string h = w.Tag("highway");
            return (h == "residential" || h == "unclassified" || h == "tertiary" || h == "service" || h == "living_street") && w.Nodes.Count >= 2;
        }

        static float RoadHalfWidth(RosvikOsmV15.Way w) {
            string h = w.Tag("highway");
            if (h == "residential" || h == "unclassified" || h == "tertiary") return 2.65f;
            if (h == "service" || h == "living_street") return 1.80f;
            return 1.0f;
        }

        static bool IsHouse(RosvikOsmV15.Way w) {
            if (!w.Closed || w.Nodes.Count < 4) return false;
            string b = w.Tag("building");
            if (string.IsNullOrEmpty(b) || b == "no" || b == "garage" || b == "shed" || b == "carport" || b == "roof" || b == "apartments") return false;
            if (b == "house" || b == "residential" || b == "detached" || b == "semidetached_house" || b == "bungalow") return true;
            if (b == "yes") {
                RosvikOsmV15.OBounds ob = RosvikOsmV15.Bounds(w);
                float proxy = ob.Width * ob.Depth;
                return proxy >= 35f && proxy <= 420f;
            }
            return false;
        }

        static RoadHit NearestRoad(Vector3 p, List<RosvikOsmV15.Way> roads) {
            RoadHit best = new RoadHit { valid = false, distance = float.MaxValue };
            foreach (RosvikOsmV15.Way w in roads) {
                for (int i = 0; i < w.Nodes.Count - 1; i++) {
                    Vector3 a = w.Nodes[i].Pos, b = w.Nodes[i + 1].Pos;
                    Vector3 q = ClosestPoint(p, a, b);
                    float d = Flat(p - q).magnitude;
                    if (d >= best.distance) continue;
                    Vector3 dir = Flat(b - a);
                    if (dir.sqrMagnitude < .001f) continue;
                    best = new RoadHit { valid = true, point = q, dir = dir.normalized, distance = d, halfWidth = RoadHalfWidth(w) };
                }
            }
            return best;
        }

        static float DistanceToRoad(Vector3 p, List<RosvikOsmV15.Way> roads) {
            float best = float.MaxValue;
            foreach (RosvikOsmV15.Way w in roads) {
                for (int i = 0; i < w.Nodes.Count - 1; i++) {
                    Vector3 q = ClosestPoint(p, w.Nodes[i].Pos, w.Nodes[i + 1].Pos);
                    best = Mathf.Min(best, Flat(p - q).magnitude);
                }
            }
            return best;
        }

        static float DistanceToBuilding(Vector3 p, List<RosvikOsmV15.Way> buildings) {
            float best = float.MaxValue;
            foreach (RosvikOsmV15.Way w in buildings) {
                List<Vector3> pts = Points(w);
                if (Inside(p, pts)) return 0f;
                for (int i = 0; i < pts.Count; i++) {
                    Vector3 q = ClosestPoint(p, pts[i], pts[(i + 1) % pts.Count]);
                    best = Mathf.Min(best, Flat(p - q).magnitude);
                }
            }
            return best;
        }

        static bool InsideBuilding(Vector3 p, List<RosvikOsmV15.Way> ways) {
            foreach (RosvikOsmV15.Way w in ways) {
                if (!w.Closed || string.IsNullOrEmpty(w.Tag("building")) || w.Tag("building") == "no") continue;
                if (Inside(p, Points(w))) return true;
            }
            return false;
        }

        static Vector3 ClosestPointOnPolygon(List<Vector3> pts, Vector3 target) {
            Vector3 best = pts[0];
            float bestD = float.MaxValue;
            for (int i = 0; i < pts.Count; i++) {
                Vector3 q = ClosestPoint(target, pts[i], pts[(i + 1) % pts.Count]);
                float d = Flat(q - target).sqrMagnitude;
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

        static float Yaw(Vector3 dir) => Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        static Bounds BoundsOf(GameObject go) {
            Renderer[] rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        static List<Vector3> Points(RosvikOsmV15.Way w) {
            List<Vector3> pts = w.Nodes.Select(n => n.Pos).ToList();
            if (pts.Count > 2 && w.Closed) pts.RemoveAt(pts.Count - 1);
            return pts;
        }

        static string FullPath(string assetPath) => Path.Combine(Directory.GetCurrentDirectory(), assetPath.Replace('/', Path.DirectorySeparatorChar));
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