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
    /// V35 is the first controlled residential roof pass. It only touches simple, compact
    /// housing footprints that are close to rectangular, then adds a stable gable roof as
    /// an overlay above the existing flat collision-safe building. Complex/L-shaped houses,
    /// apartments, school buildings and arena remain untouched. V34 chimneys are lifted to
    /// the new ridge height where applicable.
    /// </summary>
    [InitializeOnLoad]
    public static class RosvikRoofSilhouetteV35 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_ROOF_SILHOUETTE_V35_VERSION";
        const int Version = 35;
        const string RootName = "ROSVIK_ROOF_SILHOUETTE_V35";
        const string GroupName = "17 CONTROLLED HOUSE ROOFS V35";
        const string GeneratedDir = "Assets/Rosvik/GeneratedV35";
        const string ProvenMaterial = "Assets/Rosvik/GeneratedV20/mat_ground.mat";
        const long MainSchoolWay = 163199458;
        const long OldSchoolWay = 163199461;
        const long ArenaWay = 163199464;

        static RosvikRoofSilhouetteV35() {
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        [MenuItem("Rosvik/Rebuild Controlled House Roofs V35")]
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
            return GameObject.Find("ROSVIK_HOUSE_DETAIL_V34")
                ?? GameObject.Find("ROSVIK_CHARACTER_FEEL_V33")
                ?? GameObject.Find("ROSVIK_PLAY_COMFORT_V32")
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
                if (ways == null || ways.Count == 0) throw new InvalidOperationException("V35 could not load Rosvik OSM data.");
                RosvikOsmV15.Way schoolWay = ways.FirstOrDefault(w => w.Id == MainSchoolWay);
                if (schoolWay == null) throw new InvalidOperationException("V35 could not locate Rosviks skola.");
                Vector3 school = RosvikOsmV15.Centroid(schoolWay);

                Material roofRed = Mat("roof_muted_red", new Color(.255f,.105f,.075f), .19f);
                Material roofDark = Mat("roof_charcoal", new Color(.105f,.115f,.11f), .24f);
                Material roofBrown = Mat("roof_weathered_brown", new Color(.205f,.145f,.095f), .15f);
                Material roofGrey = Mat("roof_weathered_grey", new Color(.235f,.235f,.215f), .18f);
                Material fascia = Mat("roof_fascia", new Color(.50f,.49f,.44f), .15f);
                Material metal = Mat("roof_metal", new Color(.19f,.205f,.20f), .31f);
                Material[] roofs = { roofRed, roofDark, roofBrown, roofGrey };

                List<RosvikOsmV15.Way> candidates = ways
                    .Where(IsSimpleHouseCandidate)
                    .Where(w => w.Id != MainSchoolWay && w.Id != OldSchoolWay && w.Id != ArenaWay)
                    .OrderBy(w => Vector3.Distance(Flat(RosvikOsmV15.Centroid(w)), Flat(school)))
                    .Where(w => Vector3.Distance(Flat(RosvikOsmV15.Centroid(w)), Flat(school)) <= 118f)
                    .ToList();

                int built = 0;
                foreach (RosvikOsmV15.Way w in candidates) {
                    if (built >= 30) break;
                    List<Vector3> pts = Points(w);
                    if (!RectangularEnough(w, pts)) continue;

                    RosvikOsmV15.OBounds ob = RosvikOsmV15.Bounds(w);
                    Vector3 axis = ob.AxisX.sqrMagnitude > .01f ? ob.AxisX.normalized : Vector3.right;
                    Vector3 perp = new Vector3(-axis.z, 0f, axis.x).normalized;
                    float length = ob.Width;
                    float depth = ob.Depth;
                    if (depth > length) {
                        float tmp = length; length = depth; depth = tmp;
                        Vector3 oldAxis = axis; axis = perp; perp = -oldAxis;
                    }
                    if (length < 5.0f || depth < 4.0f || length > 28f || depth > 18f) continue;

                    float height = 2.9f;
                    float overhang = .32f;
                    float rise = Mathf.Clamp(depth * .24f, .78f, 1.72f);
                    int seed = unchecked((int)(w.Id & 0x7fffffff));
                    Material roof = roofs[Math.Abs(seed % roofs.Length)];

                    Transform houseRoof = NewGroup(group, "pitched roof " + w.Id);
                    BuildGableRoof(houseRoof, ob.Center, axis, perp, length + overhang * 2f, depth + overhang * 2f,
                        height + .075f, rise, roof, fascia, metal);
                    LiftV34Chimney(root.transform, w.Id, rise);
                    built++;
                }

                root.name = RootName;
                EditorPrefs.SetInt(Key, Version);
                Selection.activeObject = null;
                EditorUtility.SetDirty(root);
                AssetDatabase.SaveAssets();
                if (!Busy()) {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene, ScenePath);
                }
                Debug.Log("ROSVIK V35: " + built + " simple nearby homes received safe pitched gable roofs, ridge/eave detail and corrected chimney height. Complex footprints, gameplay, controls and geography unchanged.");
            } catch (Exception ex) {
                Debug.LogError("ROSVIK V35 FAILED: " + ex);
            }
        }

        static bool IsSimpleHouseCandidate(RosvikOsmV15.Way w) {
            if (!w.Closed || w.Nodes.Count < 4) return false;
            string b = w.Tag("building");
            if (string.IsNullOrEmpty(b) || b == "no" || b == "garage" || b == "shed" || b == "carport" || b == "roof" || b == "apartments" || b == "terrace") return false;
            if (b == "house" || b == "residential" || b == "detached" || b == "semidetached_house" || b == "bungalow") return true;
            if (b == "yes") {
                RosvikOsmV15.OBounds ob = RosvikOsmV15.Bounds(w);
                float proxy = ob.Width * ob.Depth;
                return proxy >= 42f && proxy <= 330f;
            }
            return false;
        }

        static bool RectangularEnough(RosvikOsmV15.Way w, List<Vector3> pts) {
            if (pts == null || pts.Count < 4 || pts.Count > 8) return false;
            RosvikOsmV15.OBounds ob = RosvikOsmV15.Bounds(w);
            float boxArea = Mathf.Max(.01f, ob.Width * ob.Depth);
            float polyArea = Mathf.Abs(SignedArea(pts));
            float fill = polyArea / boxArea;
            if (fill < .79f || fill > 1.06f) return false;

            // Reject very short zig-zag edges typical of porches/L-shaped footprints. The
            // roof overlay is intentionally reserved for footprints where an OBB gable reads cleanly.
            int shortEdges = 0;
            for (int i = 0; i < pts.Count; i++) {
                float len = Flat(pts[(i + 1) % pts.Count] - pts[i]).magnitude;
                if (len < 1.45f) shortEdges++;
            }
            return shortEdges <= 1;
        }

        static void BuildGableRoof(Transform parent, Vector3 center, Vector3 axis, Vector3 perp,
            float length, float depth, float baseY, float rise, Material roof, Material fascia, Material metal) {

            axis = Flat(axis).normalized;
            perp = Flat(perp).normalized;
            Vector3 c = new Vector3(center.x, 0f, center.z);
            float hx = length * .5f;
            float hz = depth * .5f;

            Vector3[] v = new Vector3[6];
            v[0] = c - axis * hx - perp * hz + Vector3.up * baseY;
            v[1] = c + axis * hx - perp * hz + Vector3.up * baseY;
            v[2] = c - axis * hx + Vector3.up * (baseY + rise);
            v[3] = c + axis * hx + Vector3.up * (baseY + rise);
            v[4] = c - axis * hx + perp * hz + Vector3.up * baseY;
            v[5] = c + axis * hx + perp * hz + Vector3.up * baseY;

            int[] triangles = {
                0,2,1, 1,2,3,
                2,4,3, 3,4,5,
                0,4,2, 1,3,5
            };
            Vector2[] uv = new Vector2[v.Length];
            uv[0]=new Vector2(0,0); uv[1]=new Vector2(1,0); uv[2]=new Vector2(0,.5f);
            uv[3]=new Vector2(1,.5f); uv[4]=new Vector2(0,1); uv[5]=new Vector2(1,1);

            Mesh mesh = new Mesh { name = "V35 stable gable roof", vertices = v, triangles = triangles, uv = uv };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            GameObject shell = new GameObject("pitched roof shell");
            shell.transform.SetParent(parent, false);
            shell.AddComponent<MeshFilter>().sharedMesh = mesh;
            shell.AddComponent<MeshRenderer>().sharedMaterial = roof;

            Quaternion alongAxis = Quaternion.FromToRotation(Vector3.right, axis);
            Box("ridge cap", parent, c + Vector3.up * (baseY + rise + .035f), new Vector3(length + .08f,.075f,.10f), alongAxis, metal);
            Box("eave gutter", parent, c - perp * hz + Vector3.up * (baseY - .015f), new Vector3(length,.065f,.085f), alongAxis, metal);
            Box("eave gutter", parent, c + perp * hz + Vector3.up * (baseY - .015f), new Vector3(length,.065f,.085f), alongAxis, metal);

            // Thin fascia boards at the gable ends hide the remaining flat-roof edge and make
            // the silhouette read as a deliberately constructed Swedish detached-house roof.
            BuildGableFascia(parent, c - axis * hx, -axis, perp, depth, baseY, rise, fascia);
            BuildGableFascia(parent, c + axis * hx, axis, perp, depth, baseY, rise, fascia);
        }

        static void BuildGableFascia(Transform parent, Vector3 endCenter, Vector3 outward, Vector3 across,
            float depth, float baseY, float rise, Material mat) {
            float half = depth * .5f;
            Vector3 ridge = new Vector3(endCenter.x, baseY + rise, endCenter.z);
            Vector3 left = new Vector3(endCenter.x, baseY, endCenter.z) - across * half;
            Vector3 right = new Vector3(endCenter.x, baseY, endCenter.z) + across * half;
            Beam("gable fascia", parent, left, ridge, .085f, mat);
            Beam("gable fascia", parent, ridge, right, .085f, mat);
        }

        static void Beam(string name, Transform parent, Vector3 a, Vector3 b, float thickness, Material mat) {
            Vector3 d = b - a;
            float len = d.magnitude;
            if (len < .02f) return;
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.position = (a + b) * .5f;
            g.transform.rotation = Quaternion.FromToRotation(Vector3.right, d.normalized);
            g.transform.localScale = new Vector3(len, thickness, thickness);
            g.GetComponent<Renderer>().sharedMaterial = mat;
            UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
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

        static void LiftV34Chimney(Transform root, long wayId, float rise) {
            Transform detail = Find(root, "house detail " + wayId);
            if (!detail) return;
            foreach (Transform t in detail.GetComponentsInChildren<Transform>(true)) {
                if (t.name == "chimney" || t.name == "chimney cap") t.position += Vector3.up * rise;
            }
        }

        static Material Mat(string name, Color color, float smoothness) {
            string path = GeneratedDir + "/mat_" + name + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader s = ResolveShader();
            if (!m) { m = new Material(s) { name = "V35 " + name }; AssetDatabase.CreateAsset(m, path); }
            m.shader = s;
            m.color = color;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
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

        static List<Vector3> Points(RosvikOsmV15.Way w) {
            List<Vector3> pts = w.Nodes.Select(n => n.Pos).ToList();
            if (pts.Count > 2 && w.Closed) pts.RemoveAt(pts.Count - 1);
            return pts;
        }

        static float SignedArea(List<Vector3> p) {
            float a = 0f;
            for (int i = 0; i < p.Count; i++) {
                Vector3 q = p[(i + 1) % p.Count];
                a += p[i].x * q.z - q.x * p[i].z;
            }
            return a * .5f;
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
