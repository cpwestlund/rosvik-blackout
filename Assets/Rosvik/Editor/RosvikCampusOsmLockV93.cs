#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Rosvik.Blackout;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikCampusOsmLockV93 {
        const int Version = 93;
        const string Key = "ROSVIK_CAMPUS_OSM_LOCK_V93";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string CacheV86 = "Assets/Rosvik/GeneratedV86/rosvik_overpass_cache.xml";
        const string CacheV88 = "Assets/Rosvik/GeneratedV88/rosvik_overpass_cache.xml";
        const string SchoolId = "163199458";
        const string ArenaId = "163199454";
        const double OriginLat = 65.426266;
        const double OriginLon = 21.692577;
        const float RotationDeg = 33f;
        const float MapScale = .72f;
        static int retries;

        static RosvikCampusOsmLockV93() {
            if (EditorPrefs.GetInt(Key, 0) >= Version) return;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V93 LOCK CAMPUS TO OSM + STABLE ROOFS")]
        public static void Force() {
            EditorPrefs.DeleteKey(Key);
            retries = 0;
            EditorApplication.delayCall += Auto;
        }

        static void Auto() {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) {
                EditorApplication.delayCall += Auto;
                return;
            }
            if (!File.Exists(ScenePath)) return;
            try {
                if (!Apply() && retries++ < 12) EditorApplication.delayCall += Auto;
            } catch (Exception ex) {
                Debug.LogError("V93 OSM LOCK FAILED: " + ex);
            }
        }

        static bool Apply() {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject campus = GameObject.Find("ROSVIK CAMPUS · V92 ASTRA BENCHMARK");
            if (!campus) return false; // V92 may still be finishing its one-shot build after a domain reload.

            Transform school = Find(campus.transform, "ROSVIKS SKOLA · V92");
            Transform connector = Find(campus.transform, "HEATED SCHOOL-SPORTHALL CONNECTOR · V92");
            Transform sporthall = Find(campus.transform, "ROSVIK SPORTHALL · V92");
            Transform icehall = Find(campus.transform, "NORRBOTTEN STÅL ARENA · ISHALL V92");
            if (!school || !connector || !sporthall || !icehall) return false;

            // V88's clean OSM world is anchored so the real Rosviks skola centroid is (0, 7.35).
            // Move the authored school/sporthall cluster onto that anchor instead of inventing a second coordinate system.
            Vector3 schoolTarget = new Vector3(0f, 0f, 7.35f);
            Vector3 oldSchoolAnchor = new Vector3(-10f, 0f, -4f);
            Vector3 clusterDelta = schoolTarget - oldSchoolAnchor;

            Vector3 osmArenaDelta = ReadOsmArenaDelta();
            Vector3 arenaTarget = schoolTarget + osmArenaDelta;
            Vector3 oldArenaAnchor = new Vector3(11f, 0f, 25f);
            Vector3 arenaDelta = arenaTarget - oldArenaAnchor;

            // Idempotent: roots are assigned their correction offsets, never incremented.
            school.position = clusterDelta;
            connector.position = clusterDelta;
            sporthall.position = clusterDelta;
            icehall.position = arenaDelta;

            Transform schoolInterior = FindGlobal("SCHOOL INTERIOR V92");
            Transform sportInterior = FindGlobal("SPORTHALL INTERIOR V92");
            Transform iceInterior = FindGlobal("ICEHALL INTERIOR V92");
            if (schoolInterior) schoolInterior.position = clusterDelta;
            if (sportInterior) sportInterior.position = clusterDelta;
            if (iceInterior) iceInterior.position = arenaDelta;

            // Indoor cutaway volumes must follow the same real positions.
            SetWorldPosition(Find(campus.transform, "SCHOOL MAIN INDOOR V92"), new Vector3(0f, 1.45f, 7.35f));
            SetWorldPosition(Find(campus.transform, "SCHOOL WING INDOOR V92"), new Vector3(-6f, 1.4f, 16.45f));
            SetWorldPosition(Find(campus.transform, "CONNECTOR INDOOR V92"), new Vector3(13.5f, 1.35f, 10.35f));
            SetWorldPosition(Find(campus.transform, "SPORTHALL INDOOR V92"), new Vector3(22f, 2.0f, 10.35f));
            SetWorldPosition(Find(campus.transform, "ICEHALL INDOOR V92"), new Vector3(arenaTarget.x, 2.6f, arenaTarget.z));

            // The rotated cube roofs were the source of the recurring giant/skewed planes.
            // Replace them with restrained low-pitch roof slabs; no rotated/scaled parent can distort them.
            Material roofMat = FindMaterialByNamePart(campus.transform, "roof") ?? FirstMaterial(campus.transform);
            Material snowMat = FindMaterialByNamePart(campus.transform, "snow") ?? roofMat;
            RebuildSchoolRoof(Find(school, "ROOF ROOT"), roofMat, snowMat, clusterDelta);
            RebuildSingleRoof(Find(connector, "ROOF ROOT"), roofMat, snowMat, new Vector3(13.5f, 2.58f, 10.35f), new Vector2(2.5f, 3.5f));
            RebuildSingleRoof(Find(sporthall, "ROOF ROOT"), roofMat, snowMat, new Vector3(22f, 4.42f, 10.35f), new Vector2(15.7f, 18.7f));
            RebuildSingleRoof(Find(icehall, "ROOF ROOT"), roofMat, snowMat, new Vector3(arenaTarget.x, 5.74f, arenaTarget.z), new Vector2(26.8f, 24.8f));

            // Bring the authored forecourt/dressing with the school cluster; keep the actual OSM roads underneath untouched.
            Transform dressing = FindGlobal("V92 CAMPUS DRESSING");
            if (dressing) dressing.position = clusterDelta;
            Transform forecourt = FindGlobal("school forecourt");
            if (forecourt) forecourt.position = new Vector3(0f, forecourt.position.y, -1.85f);
            Transform lane = FindGlobal("campus lane");
            if (lane) UnityEngine.Object.DestroyImmediate(lane.gameObject);
            Transform apron = FindGlobal("icehall apron");
            if (apron) apron.position = new Vector3(arenaTarget.x, apron.position.y, arenaTarget.z - 14.5f);

            // Reposition the three authored warm lights to their actual buildings.
            Transform lighting = FindGlobal("V92 LIGHTING");
            if (lighting) {
                Light[] ls = lighting.GetComponentsInChildren<Light>(true);
                if (ls.Length > 0) ls[0].transform.position = new Vector3(0f, 2.4f, -0.75f);
                if (ls.Length > 1) ls[1].transform.position = new Vector3(arenaTarget.x, 3.2f, arenaTarget.z - 13.6f);
                if (ls.Length > 2) ls[2].transform.position = new Vector3(13.5f, 2.2f, 10.35f);
            }

            CoziPlayerV57 player = UnityEngine.Object.FindFirstObjectByType<CoziPlayerV57>();
            if (player) {
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc) cc.enabled = false;
                player.transform.position = new Vector3(0f, .03f, -4.15f);
                if (cc) cc.enabled = true;
                player.SetObjective("Utforska skolområdet. Byggnaderna följer nu OpenStreetMap-layouten.");
                EditorUtility.SetDirty(player);
            }

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorPrefs.SetInt(Key, Version);
            SceneView.RepaintAll();
            Debug.Log($"V93 COMPLETE — campus locked to OSM; arena delta from school = ({osmArenaDelta.x:F1}, {osmArenaDelta.z:F1}) m in Unity map space; unstable gable roof cubes removed.");
            return true;
        }

        static Vector3 ReadOsmArenaDelta() {
            try {
                string path = File.Exists(CacheV86) ? CacheV86 : CacheV88;
                if (File.Exists(path)) {
                    XDocument doc = XDocument.Parse(File.ReadAllText(path));
                    Vector2 s = WayCentroid(doc, SchoolId);
                    Vector2 a = WayCentroid(doc, ArenaId);
                    if (s != Vector2.zero && a != Vector2.zero) {
                        Vector2 ps = Project(s.x, s.y);
                        Vector2 pa = Project(a.x, a.y);
                        Vector2 d = pa - ps;
                        return new Vector3(d.x, 0f, d.y);
                    }
                }
            } catch (Exception ex) { Debug.LogWarning("V93 OSM cache parse fallback: " + ex.Message); }

            // Current OSM centroids for way 163199458 and 163199454, only used if the local cache is unavailable.
            Vector2 sf = Project(65.42625f, 21.69258f);
            Vector2 af = Project(65.42611f, 21.69393f);
            Vector2 fd = af - sf;
            return new Vector3(fd.x, 0f, fd.y);
        }

        static Vector2 WayCentroid(XDocument doc, string id) {
            XElement w = doc.Descendants("way").FirstOrDefault(e => ((string)e.Attribute("id") ?? "") == id);
            if (w == null) return Vector2.zero;
            double lat = 0, lon = 0; int n = 0;
            foreach (XElement nd in w.Elements("nd")) {
                XAttribute la = nd.Attribute("lat"), lo = nd.Attribute("lon");
                if (la == null || lo == null) continue;
                lat += double.Parse(la.Value, CultureInfo.InvariantCulture);
                lon += double.Parse(lo.Value, CultureInfo.InvariantCulture);
                n++;
            }
            return n > 0 ? new Vector2((float)(lat / n), (float)(lon / n)) : Vector2.zero;
        }

        static Vector2 Project(double lat, double lon) {
            double east = (lon - OriginLon) * 111320.0 * Math.Cos(OriginLat * Math.PI / 180.0);
            double north = (lat - OriginLat) * 110540.0;
            double a = RotationDeg * Math.PI / 180.0;
            double x = east * Math.Cos(a) - north * Math.Sin(a);
            double z = east * Math.Sin(a) + north * Math.Cos(a);
            return new Vector2((float)(x * MapScale), (float)(z * MapScale));
        }

        static void RebuildSchoolRoof(Transform root, Material roofMat, Material snowMat, Vector3 d) {
            if (!root) return;
            Clear(root);
            StableRoof(root, new Vector3(-10f, 3.25f, -4f) + d, new Vector2(25.7f, 13.7f), roofMat, snowMat);
            StableRoof(root, new Vector3(-16f, 2.95f, 5.1f) + d, new Vector2(12.6f, 8.1f), roofMat, snowMat);
        }

        static void RebuildSingleRoof(Transform root, Material roofMat, Material snowMat, Vector3 center, Vector2 size) {
            if (!root) return;
            Clear(root);
            StableRoof(root, center, size, roofMat, snowMat);
        }

        static void StableRoof(Transform parent, Vector3 center, Vector2 size, Material roofMat, Material snowMat) {
            Cube("stable roof deck", parent, center, new Vector3(size.x, .22f, size.y), roofMat);
            Cube("stable snow cap", parent, center + Vector3.up * .145f, new Vector3(size.x - .16f, .07f, size.y - .16f), snowMat);
            // Dark fascia gives roof depth without rotated geometry.
            float y = center.y - .04f;
            Cube("fascia north", parent, new Vector3(center.x, y, center.z + size.y * .5f), new Vector3(size.x + .08f, .18f, .10f), roofMat);
            Cube("fascia south", parent, new Vector3(center.x, y, center.z - size.y * .5f), new Vector3(size.x + .08f, .18f, .10f), roofMat);
            Cube("fascia east", parent, new Vector3(center.x + size.x * .5f, y, center.z), new Vector3(.10f, .18f, size.y), roofMat);
            Cube("fascia west", parent, new Vector3(center.x - size.x * .5f, y, center.z), new Vector3(.10f, .18f, size.y), roofMat);
        }

        static GameObject Cube(string name, Transform parent, Vector3 worldPos, Vector3 scale, Material mat) {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.SetParent(parent, true);
            g.transform.position = worldPos;
            g.transform.rotation = Quaternion.identity;
            g.transform.localScale = scale;
            Renderer r = g.GetComponent<Renderer>(); if (r && mat) r.sharedMaterial = mat;
            Collider c = g.GetComponent<Collider>(); if (c) UnityEngine.Object.DestroyImmediate(c);
            return g;
        }

        static Material FindMaterialByNamePart(Transform root, string part) {
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true)) {
                if (!r || !r.sharedMaterial) continue;
                if (r.gameObject.name.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    r.sharedMaterial.name.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0) return r.sharedMaterial;
            }
            return null;
        }

        static Material FirstMaterial(Transform root) {
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true)) if (r && r.sharedMaterial) return r.sharedMaterial;
            return null;
        }

        static void SetWorldPosition(Transform t, Vector3 p) { if (t) t.position = p; }
        static void Clear(Transform t) { for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(t.GetChild(i).gameObject); }

        static Transform Find(Transform root, string exact) {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == exact) return t;
            return null;
        }

        static Transform FindGlobal(string exact) {
            foreach (Transform t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t && t.gameObject.scene.IsValid() && t.name == exact) return t;
            return null;
        }
    }
}
#endif
