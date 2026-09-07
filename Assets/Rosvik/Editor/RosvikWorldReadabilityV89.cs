#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Rosvik.Blackout;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikWorldReadabilityV89 {
        const int Version = 89;
        const string Key = "ROSVIK_WORLD_READABILITY_V89";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string WorldRoot = "V88 CLEAN OSM WORLD";
        const string PassRoot = "V89 LANDMARK READABILITY";
        const string SchoolName = "ROSVIKS SKOLA · CLEAN OSM V88";
        const string ArenaName = "NORRBOTTEN STÅL ARENA · CLEAN OSM V88";
        const string OldSchoolName = "STENSKOLAN · OSM V88";

        static RosvikWorldReadabilityV89() {
            if (EditorPrefs.GetInt(Key,0) >= Version) return;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V89 MAKE OSM WORLD READABLE")]
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
            catch (Exception ex) { Debug.LogError("V89 READABILITY FAILED: " + ex); }
        }

        static void Apply() {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject world = GameObject.Find(WorldRoot);
            if (!world) { Debug.LogWarning("V89 waits for V88 CLEAN OSM WORLD. Run V88 first if needed."); return; }
            GameObject school = FindDeep(world.transform, SchoolName);
            GameObject arena = FindDeep(world.transform, ArenaName);
            GameObject oldSchool = FindDeep(world.transform, OldSchoolName);
            if (!school || !arena) throw new Exception("School or arena landmark missing from V88 world");

            GameObject old = GameObject.Find(PassRoot);
            if (old) UnityEngine.Object.DestroyImmediate(old);
            GameObject root = new GameObject(PassRoot);

            Bounds sb = RendererBounds(school);
            Bounds ab = RendererBounds(arena);
            Bounds ob = oldSchool ? RendererBounds(oldSchool) : new Bounds();

            // V88 hides entire landmarks when inside. Replace that with roof-only cutaway so the player
            // keeps architectural context instead of seeing an unexplained empty rectangle/rink.
            OsmWorldRuntimeV86 oldCutaway = world.GetComponent<OsmWorldRuntimeV86>();
            if (oldCutaway) {
                oldCutaway.cutaways = Array.Empty<CutawayTargetV86>();
                EditorUtility.SetDirty(oldCutaway);
            }

            WorldReadabilityV89 rr = root.AddComponent<WorldReadabilityV89>();
            rr.landmarks = oldSchool
                ? new [] {
                    Zone("ROSVIKS SKOLA", school, sb, 7.0f),
                    Zone("NORRBOTTEN STÅL ARENA · ISHALL", arena, ab, 8.5f),
                    Zone("STENSKOLAN", oldSchool, ob, 5.0f)
                }
                : new [] {
                    Zone("ROSVIKS SKOLA", school, sb, 7.0f),
                    Zone("NORRBOTTEN STÅL ARENA · ISHALL", arena, ab, 8.5f)
                };

            // Three unmistakable landmarks. These are independent objects, never children of scaled walls,
            // so their text cannot stretch into the white spaghetti seen in older passes.
            BuildLandmarkSign(root.transform, "ROSVIKS SKOLA", EntranceOutside(sb, school.transform.position), new Color(.58f,.38f,.18f), 4.6f);
            BuildLandmarkSign(root.transform, "NORRBOTTEN STÅL ARENA", EntranceOutside(ab, arena.transform.position), new Color(.19f,.34f,.45f), 5.0f);
            if (oldSchool) BuildLandmarkSign(root.transform, "STENSKOLAN", new Vector3(ob.center.x,0,ob.min.z-2.4f), new Color(.42f,.37f,.29f), 3.6f);

            BuildOrientationMarkers(root.transform, sb, ab);
            ImproveLightingAndMaterials();
            ImproveCamera();

            CoziPlayerV57 player = UnityEngine.Object.FindFirstObjectByType<CoziPlayerV57>();
            if (player) {
                player.SetObjective("Orientera dig i Rosvik. Skolan är gul/ockra; Norrbotten Stål Arena är den stora blå ishallen.");
                EditorUtility.SetDirty(player);
            }

            CleanupMissingScripts();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorPrefs.SetInt(Key,Version);
            SceneView.RepaintAll();
            Debug.Log("V89 COMPLETE — school, ice arena and Stenskolan are visually distinct, roofs clear near the player, and the camera now gives enough context to understand the OSM world.");
        }

        static LandmarkZoneV89 Zone(string name, GameObject go, Bounds b, float pad) => new LandmarkZoneV89 {
            displayName=name, buildingRoot=go,
            minXZ=new Vector2(b.min.x,b.min.z), maxXZ=new Vector2(b.max.x,b.max.z), nearPadding=pad
        };

        static Vector3 EntranceOutside(Bounds b, Vector3 hint) {
            // In the current OSM build the authored entrances are on one of the long outer edges.
            // A sign offset from the nearest world-facing edge remains readable regardless of footprint rotation.
            Vector3 c=b.center; c.y=0;
            Vector3 d=new Vector3(hint.x-c.x,0,hint.z-c.z);
            if (d.sqrMagnitude < .01f) d=Vector3.back;
            d.Normalize();
            return c + d * Mathf.Max(b.extents.x,b.extents.z) + Vector3.back * 2.0f;
        }

        static void BuildOrientationMarkers(Transform p, Bounds school, Bounds arena) {
            Vector3 mid=(school.center+arena.center)*.5f; mid.y=.03f;
            Vector3 dir=arena.center-school.center; dir.y=0;
            float len=dir.magnitude;
            if(len>1f) {
                dir/=len;
                Slab("campus link",p,school.center+dir*3f,arena.center-dir*3f,2.2f,new Color(.54f,.61f,.62f));
            }
            // Warm entrance pools make the school readable from a distance; cold-white pool marks the arena.
            PointLight("school entrance light",p,new Vector3(school.center.x,2.5f,school.min.z-2f),new Color(1f,.67f,.34f),4.0f,12f);
            PointLight("arena entrance light",p,new Vector3(arena.center.x,3.2f,arena.min.z-2f),new Color(.70f,.86f,1f),4.8f,15f);
        }

        static void ImproveCamera() {
            Camera cam=Camera.main ? Camera.main : UnityEngine.Object.FindFirstObjectByType<Camera>();
            if(!cam) return;
            cam.orthographic=true;
            cam.orthographicSize=10.8f;
            CozyCameraV57 rig=cam.GetComponent<CozyCameraV57>();
            if(rig) {
                rig.minSize=8.6f;
                rig.maxSize=14.5f;
                rig.offset=new Vector3(0f,19.5f,-16.5f);
                rig.lookAhead=.45f;
                EditorUtility.SetDirty(rig);
            }
            EditorUtility.SetDirty(cam);
        }

        static void ImproveLightingAndMaterials() {
            RenderSettings.ambientLight=new Color(.48f,.55f,.58f);
            RenderSettings.fogColor=new Color(.55f,.62f,.65f);
            RenderSettings.fogDensity=.0035f;
            TintAsset("Assets/Rosvik/GeneratedV88/snow.mat", new Color(.78f,.84f,.86f));
            TintAsset("Assets/Rosvik/GeneratedV88/snow_edge.mat", new Color(.68f,.76f,.79f));
            TintAsset("Assets/Rosvik/GeneratedV88/asphalt.mat", new Color(.16f,.20f,.22f));
            TintAsset("Assets/Rosvik/GeneratedV88/school_ochre.mat", new Color(.63f,.43f,.22f));
            TintAsset("Assets/Rosvik/GeneratedV88/arena_blue.mat", new Color(.20f,.36f,.47f));
            TintAsset("Assets/Rosvik/GeneratedV88/red_facade.mat", new Color(.45f,.23f,.21f));
        }

        static void TintAsset(string path, Color c) {
            Material m=AssetDatabase.LoadAssetAtPath<Material>(path); if(!m) return;
            if(m.HasProperty("_BaseColor")) m.SetColor("_BaseColor",c);
            if(m.HasProperty("_Color")) m.SetColor("_Color",c);
            EditorUtility.SetDirty(m);
        }

        static void BuildLandmarkSign(Transform p,string text,Vector3 pos,Color color,float width) {
            GameObject r=new GameObject("LANDMARK · "+text);r.transform.SetParent(p,true);r.transform.position=pos;
            Box("post L",r.transform,new Vector3(-width*.42f,.85f,0),new Vector3(.08f,1.7f,.08f),new Color(.18f,.20f,.20f));
            Box("post R",r.transform,new Vector3(width*.42f,.85f,0),new Vector3(.08f,1.7f,.08f),new Color(.18f,.20f,.20f));
            Box("sign",r.transform,new Vector3(0,1.55f,0),new Vector3(width,.72f,.12f),color);
            GameObject label=new GameObject("text"); label.transform.SetParent(r.transform,false); label.transform.localPosition=new Vector3(0,1.55f,-.075f); label.transform.localRotation=Quaternion.Euler(0,180,0);
            TextMesh tm=label.AddComponent<TextMesh>(); tm.text=text; tm.fontSize=64; tm.characterSize=.10f; tm.anchor=TextAnchor.MiddleCenter; tm.alignment=TextAlignment.Center; tm.color=new Color(.96f,.95f,.90f); tm.fontStyle=FontStyle.Bold;
        }

        static void Slab(string name,Transform p,Vector3 a,Vector3 b,float width,Color color) {
            Vector3 d=b-a;d.y=0;float len=d.magnitude;if(len<.1f)return;float yaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;
            GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(p,true);g.transform.position=(a+b)*.5f+Vector3.up*.015f;g.transform.rotation=Quaternion.Euler(0,yaw,0);g.transform.localScale=new Vector3(width,.025f,len);g.GetComponent<Renderer>().sharedMaterial=RuntimeMat(color);UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        }

        static void PointLight(string name,Transform p,Vector3 pos,Color color,float intensity,float range) {
            GameObject g=new GameObject(name);g.transform.SetParent(p,true);g.transform.position=pos;Light l=g.AddComponent<Light>();l.type=LightType.Point;l.color=color;l.intensity=intensity;l.range=range;l.shadows=LightShadows.Soft;
        }

        static void Box(string name,Transform p,Vector3 lp,Vector3 scale,Color color) {
            GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(p,false);g.transform.localPosition=lp;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=RuntimeMat(color);UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        }

        static Material RuntimeMat(Color c) {
            Shader s=Shader.Find("Universal Render Pipeline/Lit");if(!s||!s.isSupported)s=Shader.Find("Universal Render Pipeline/Simple Lit");if(!s||!s.isSupported)s=Shader.Find("Standard");
            Material m=new Material(s);if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);if(m.HasProperty("_Color"))m.SetColor("_Color",c);return m;
        }

        static Bounds RendererBounds(GameObject go) {
            Renderer[] rr=go.GetComponentsInChildren<Renderer>(true);Bounds b=new Bounds(go.transform.position,Vector3.zero);bool first=true;foreach(Renderer r in rr){if(!r)continue;if(first){b=r.bounds;first=false;}else b.Encapsulate(r.bounds);}return b;
        }

        static GameObject FindDeep(Transform t,string exact) {
            foreach(Transform c in t.GetComponentsInChildren<Transform>(true)) if(c.name==exact) return c.gameObject;
            return null;
        }

        static void CleanupMissingScripts(){foreach(GameObject g in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(g&&g.scene.IsValid())GameObjectUtility.RemoveMonoBehavioursWithMissingScript(g);}
    }
}
#endif
