#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Rosvik.Blackout;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikAuthoredWorldV90 {
        const int Version = 90;
        const string Key = "ROSVIK_AUTHORED_WORLD_V90";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string WorldRootName = "V88 CLEAN OSM WORLD";
        const string ReadabilityRoot = "V89 LANDMARK READABILITY";
        const string Generated = "Assets/Rosvik/GeneratedV90";

        static Material snow, packed, ochre, brick, blue, cream, trim, glass, warmGlass, roof, metal, wood, greenFloor, warmFloor, ice, rinkBlue, rinkRed, white;
        static Shader shader;

        static RosvikAuthoredWorldV90() {
            if (EditorPrefs.GetInt(Key, 0) >= Version) return;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V90 AUTHORED SCHOOL + ICE ARENA ON CLEAN OSM")]
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
            catch (Exception ex) { Debug.LogError("V90 AUTHORED WORLD FAILED: " + ex); }
        }

        static void Apply() {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject world = GameObject.Find(WorldRootName);
            if (!world) throw new Exception("V88 CLEAN OSM WORLD missing. Run V88 once first.");

            // V89 made orientation markers and world labels that became visually noisy and, in some pipelines, magenta.
            GameObject v89 = GameObject.Find(ReadabilityRoot);
            if (v89) UnityEngine.Object.DestroyImmediate(v89);
            foreach (WorldReadabilityV89 rr in UnityEngine.Object.FindObjectsByType<WorldReadabilityV89>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (rr) UnityEngine.Object.DestroyImmediate(rr);

            Transform landmarks = FindChild(world.transform, "04 AUTHORED LANDMARKS");
            Transform interiors = FindChild(world.transform, "05 CLEAN INTERIORS");
            Transform details = FindChild(world.transform, "07 WORLD DETAILS");
            if (!landmarks || !interiors || !details) throw new Exception("V88 world groups missing");

            // Capture arena position/orientation from the OSM-derived V88 shell, then throw the shell away.
            GameObject oldSchool = FindDeep(world.transform, "ROSVIKS SKOLA · CLEAN OSM V88");
            GameObject oldArena = FindDeep(world.transform, "NORRBOTTEN STÅL ARENA · CLEAN OSM V88");
            GameObject oldStone = FindDeep(world.transform, "STENSKOLAN · OSM V88");
            Bounds arenaBounds = oldArena ? RendererBounds(oldArena) : new Bounds(new Vector3(45,0,10), new Vector3(26,6,46));
            float arenaYaw = oldArena ? DominantWallYaw(oldArena) : 0f;
            Bounds stoneBounds = oldStone ? RendererBounds(oldStone) : new Bounds(new Vector3(-28,0,42), new Vector3(18,3,10));
            float stoneYaw = oldStone ? DominantWallYaw(oldStone) : 0f;

            ClearChildren(landmarks);
            ClearChildren(interiors);
            ClearChildren(details);

            Directory.CreateDirectory(Generated);
            shader = PickKnownGoodShader(world);
            BuildMaterials();

            GameObject school = BuildSchool(landmarks);
            GameObject arena = BuildArena(landmarks, arenaBounds.center, arenaYaw, arenaBounds.size);
            BuildStenskolan(landmarks, stoneBounds.center, stoneYaw, stoneBounds.size);
            BuildSchoolInterior(interiors);
            BuildArenaInterior(interiors, arena.transform);
            BuildCampus(details, school.transform, arena.transform);

            // Roof-only cutaway. Walls remain as context when the player enters.
            OsmWorldRuntimeV86 runtime = world.GetComponent<OsmWorldRuntimeV86>();
            if (!runtime) runtime = world.AddComponent<OsmWorldRuntimeV86>();
            GameObject schoolRoof = FindDeep(school.transform, "ROOF ROOT");
            GameObject arenaRoof = FindDeep(arena.transform, "ROOF ROOT");
            Bounds sb = new Bounds(new Vector3(0,0,7.35f), new Vector3(38,4,22));
            Bounds ab = RendererBounds(arena);
            runtime.cutaways = new [] {
                new CutawayTargetV86 { visualRoot = schoolRoof, minXZ = new Vector2(sb.min.x,sb.min.z), maxXZ = new Vector2(sb.max.x,sb.max.z), padding = .7f },
                new CutawayTargetV86 { visualRoot = arenaRoof, minXZ = new Vector2(ab.min.x,ab.min.z), maxXZ = new Vector2(ab.max.x,ab.max.z), padding = 1.1f }
            };
            runtime.attribution = "© OpenStreetMap contributors · ODbL 1.0";
            EditorUtility.SetDirty(runtime);

            FixAnyPinkMaterials(world);
            TuneLightingAndCamera();

            CoziPlayerV57 player = UnityEngine.Object.FindFirstObjectByType<CoziPlayerV57>();
            if (player) {
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc) cc.enabled = false;
                player.transform.position = new Vector3(0f,.02f,-4.2f);
                player.transform.rotation = Quaternion.identity;
                if (cc) cc.enabled = true;
                player.SetObjective("Utforska Rosvik. Skolan ligger framför dig; ishallen följer vägnätet österut.");
                EditorUtility.SetDirty(player);
            }

            CleanupMissingScripts();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorPrefs.SetInt(Key, Version);
            SceneView.RepaintAll();
            Debug.Log("V90 COMPLETE — removed V89 clutter, replaced polygon landmark shells with authored school/ice-arena geometry, kept clean OSM roads/buildings and gameplay backend.");
        }

        static GameObject BuildSchool(Transform parent) {
            GameObject root = new GameObject("ROSVIKS SKOLA · AUTHORED V90");
            root.transform.SetParent(parent, true);
            root.transform.position = new Vector3(0f,0f,7.35f);

            Transform walls = Group(root.transform,"WALLS + FACADE");
            Transform roofs = Group(root.transform,"ROOF ROOT");
            Transform facade = Group(root.transform,"DETAILS");

            // Main wing is deliberately aligned to the existing school gameplay backend.
            BuildSchoolBlock(walls, roofs, new Vector3(0,0,0), 36f, 17f, 3.05f, true);
            // Rear library/admin wing gives the OSM school a proper footprint without spanning one giant roof over everything.
            BuildSchoolBlock(walls, roofs, new Vector3(-8.5f,0,12.6f), 15f, 9.5f, 2.85f, false);
            BuildSchoolBlock(walls, roofs, new Vector3(10.5f,0,11.0f), 10f, 7.0f, 2.85f, false);

            // Main entrance: unmistakable school entrance, separate from any scaled wall.
            Box("entrance vestibule", facade, new Vector3(0,1.15f,-8.82f), new Vector3(6.0f,2.3f,.72f), brick, false);
            Box("glass double door", facade, new Vector3(0,1.05f,-9.22f), new Vector3(2.2f,2.05f,.12f), glass, false);
            Box("door mullion", facade, new Vector3(0,1.05f,-9.30f), new Vector3(.08f,2.02f,.05f), trim, false);
            Box("entrance canopy", facade, new Vector3(0,2.65f,-10.15f), new Vector3(7.2f,.18f,2.5f), roof, false);
            Box("canopy snow", facade, new Vector3(0,2.78f,-10.15f), new Vector3(6.95f,.05f,2.25f), snow, false);
            for (int s=-1;s<=1;s+=2) Box("canopy post", facade, new Vector3(s*3.0f,1.30f,-10.75f), new Vector3(.12f,2.6f,.12f), metal, false);
            Box("school sign board", facade, new Vector3(0,2.72f,-9.31f), new Vector3(3.8f,.50f,.07f), blue, false);
            WorldText(facade,"ROSVIKS SKOLA",new Vector3(0,2.72f,-9.36f),Quaternion.Euler(0,180,0),.085f);

            // Warm, sparse windows. The building now reads as a school without a giant floating label.
            for(int i=-6;i<=6;i++) {
                if(Mathf.Abs(i)<=1) continue;
                Material pane = (i== -4 || i==3) ? warmGlass : glass;
                Window(facade,new Vector3(i*2.35f,1.72f,-8.62f),0f,pane,1.45f);
            }
            for(int i=-5;i<=5;i+=2) Window(facade,new Vector3(-18.12f,1.68f,i*1.35f),90f,glass,1.35f);
            for(int i=-5;i<=5;i+=2) Window(facade,new Vector3(18.12f,1.68f,i*1.35f),90f,(i==1?warmGlass:glass),1.35f);

            // Exterior character.
            Box("bench L", facade, new Vector3(-7.0f,.36f,-10.0f), new Vector3(3.2f,.16f,.55f), wood, false);
            Box("bench R", facade, new Vector3(7.0f,.36f,-10.0f), new Vector3(3.2f,.16f,.55f), wood, false);
            for(int i=-4;i<=4;i++) Box("bike rack", facade, new Vector3(-12.0f+i*.62f,.32f,-11.0f), new Vector3(.06f,.64f,.72f), metal, false, Quaternion.Euler(0,0,18));
            return root;
        }

        static void BuildSchoolBlock(Transform walls, Transform roofs, Vector3 local, float w, float d, float h, bool frontGap) {
            Transform block = Group(walls,"school block"); block.localPosition = local;
            float t=.22f;
            Box("back wall",block,new Vector3(0,h*.5f,d*.5f),new Vector3(w,h,t),ochre,true);
            Box("left wall",block,new Vector3(-w*.5f,h*.5f,0),new Vector3(t,h,d),ochre,true);
            Box("right wall",block,new Vector3(w*.5f,h*.5f,0),new Vector3(t,h,d),ochre,true);
            if(frontGap) {
                float door=3.1f, side=(w-door)*.5f;
                Box("front wall L",block,new Vector3(-(door*.5f+side*.5f),h*.5f,-d*.5f),new Vector3(side,h,t),ochre,true);
                Box("front wall R",block,new Vector3((door*.5f+side*.5f),h*.5f,-d*.5f),new Vector3(side,h,t),ochre,true);
            } else Box("front wall",block,new Vector3(0,h*.5f,-d*.5f),new Vector3(w,h,t),ochre,true);
            // brick plinth on four sides
            Box("brick back",block,new Vector3(0,.34f,d*.5f+.02f),new Vector3(w+.12f,.64f,.24f),brick,false);
            Box("brick front",block,new Vector3(0,.34f,-d*.5f-.02f),new Vector3(w+.12f,.64f,.24f),brick,false);
            Box("brick left",block,new Vector3(-w*.5f-.02f,.34f,0),new Vector3(.24f,.64f,d),brick,false);
            Box("brick right",block,new Vector3(w*.5f+.02f,.34f,0),new Vector3(.24f,.64f,d),brick,false);

            Transform roofBlock = Group(roofs,"roof block"); roofBlock.localPosition = local;
            Box("dark roof",roofBlock,new Vector3(0,h+.15f,0),new Vector3(w+.60f,.24f,d+.60f),roof,false);
            Box("snow cap",roofBlock,new Vector3(0,h+.30f,0),new Vector3(w+.34f,.07f,d+.34f),snow,false);
        }

        static GameObject BuildArena(Transform parent, Vector3 center, float yaw, Vector3 oldSize) {
            GameObject root = new GameObject("NORRBOTTEN STÅL ARENA · AUTHORED V90"); root.transform.SetParent(parent,true);
            center.y=0; root.transform.position=center; root.transform.rotation=Quaternion.Euler(0,yaw,0);
            float w=Mathf.Clamp(Mathf.Max(oldSize.x,oldSize.z)*.55f,22f,32f);
            float d=Mathf.Clamp(Mathf.Max(oldSize.x,oldSize.z),36f,54f);
            // If wall-yaw caused bounds to swap, keep arena long and narrow.
            if(w>d){float q=w;w=d*.55f;d=q;}
            float h=5.6f;
            Transform walls=Group(root.transform,"WALLS + FACADE"); Transform roofs=Group(root.transform,"ROOF ROOT"); Transform det=Group(root.transform,"DETAILS");
            Box("arena back",walls,new Vector3(0,h*.5f,d*.5f),new Vector3(w,h,.25f),blue,true);
            Box("arena left",walls,new Vector3(-w*.5f,h*.5f,0),new Vector3(.25f,h,d),blue,true);
            Box("arena right",walls,new Vector3(w*.5f,h*.5f,0),new Vector3(.25f,h,d),blue,true);
            float opening=5.2f,side=(w-opening)*.5f;
            Box("arena front L",walls,new Vector3(-(opening*.5f+side*.5f),h*.5f,-d*.5f),new Vector3(side,h,.25f),blue,true);
            Box("arena front R",walls,new Vector3((opening*.5f+side*.5f),h*.5f,-d*.5f),new Vector3(side,h,.25f),blue,true);
            Box("arena roof",roofs,new Vector3(0,h+.18f,0),new Vector3(w+.75f,.30f,d+.75f),roof,false);
            Box("arena snow cap",roofs,new Vector3(0,h+.36f,0),new Vector3(w+.45f,.07f,d+.45f),snow,false);
            Box("arena glass entrance",det,new Vector3(0,1.55f,-d*.5f-.16f),new Vector3(4.8f,3.05f,.14f),glass,false);
            Box("arena canopy",det,new Vector3(0,3.65f,-d*.5f-1.15f),new Vector3(7.0f,.20f,2.5f),roof,false);
            Box("arena sign",det,new Vector3(0,4.35f,-d*.5f-.18f),new Vector3(6.8f,.62f,.08f),cream,false);
            WorldText(det,"NORRBOTTEN STÅL ARENA",new Vector3(0,4.35f,-d*.5f-.24f),Quaternion.Euler(0,180,0),.075f);
            for(int i=-3;i<=3;i++) Window(det,new Vector3(i*(w/8f),4.15f,-d*.5f-.14f),0f,glass,1.3f);
            return root;
        }

        static void BuildStenskolan(Transform parent, Vector3 center, float yaw, Vector3 size) {
            GameObject r=new GameObject("STENSKOLAN · AUTHORED V90");r.transform.SetParent(parent,true);center.y=0;r.transform.position=center;r.transform.rotation=Quaternion.Euler(0,yaw,0);
            float w=Mathf.Clamp(Mathf.Max(size.x,size.z),13f,22f), d=Mathf.Clamp(Mathf.Min(size.x,size.z),7f,12f), h=3.0f;
            Transform roofRoot=Group(r.transform,"ROOF ROOT");
            Box("front",r.transform,new Vector3(0,h*.5f,-d*.5f),new Vector3(w,h,.22f),cream,true);
            Box("back",r.transform,new Vector3(0,h*.5f,d*.5f),new Vector3(w,h,.22f),cream,true);
            Box("left",r.transform,new Vector3(-w*.5f,h*.5f,0),new Vector3(.22f,h,d),cream,true);
            Box("right",r.transform,new Vector3(w*.5f,h*.5f,0),new Vector3(.22f,h,d),cream,true);
            Box("roof",roofRoot,new Vector3(0,h+.18f,0),new Vector3(w+.5f,.28f,d+.5f),roof,false);
            Box("snow cap",roofRoot,new Vector3(0,h+.35f,0),new Vector3(w+.3f,.06f,d+.3f),snow,false);
            for(int i=-3;i<=3;i+=2) Window(r.transform,new Vector3(i*(w/8f),1.65f,-d*.5f-.13f),0f,glass,1.25f);
        }

        static void BuildSchoolInterior(Transform parent) {
            GameObject root=new GameObject("ROSVIKS SKOLA INTERIOR · AUTHORED V90");root.transform.SetParent(parent,true);root.transform.position=new Vector3(0,0,7.35f);
            Box("school floor",root.transform,new Vector3(0,.035f,0),new Vector3(35.5f,.07f,16.5f),greenFloor,false);
            Box("corridor floor",root.transform,new Vector3(0,.075f,-3.9f),new Vector3(35.2f,.025f,4.3f),warmFloor,false);
            // Low wall language: readable cutaway, never a wall that hides the character.
            LowWall(root.transform,new Vector3(-17.7f,0,8.0f),new Vector3(17.7f,0,8.0f),1.0f);
            LowWall(root.transform,new Vector3(-17.7f,0,-8.0f),new Vector3(-17.7f,0,8.0f),1.0f);
            LowWall(root.transform,new Vector3(17.7f,0,-8.0f),new Vector3(17.7f,0,8.0f),1.0f);
            foreach(float x in new[]{-8.0f,-.5f,7.4f,12.5f}) LowWall(root.transform,new Vector3(x,0,-1.8f),new Vector3(x,0,8.0f),.92f);
            // Ordered furniture, no random scatter.
            for(int row=0;row<3;row++) for(int col=0;col<2;col++) {
                Vector3 p=new Vector3(-14.2f+col*3.0f,.0f,1.0f+row*2.15f);
                PlaceAsset(root.transform,"table_small",p,0f,.72f);
                PlaceAsset(root.transform,"chair_A",p+new Vector3(0,0,-.75f),180f,.72f);
            }
            PlaceAsset(root.transform,"shelf_A_big",new Vector3(-17.0f,0,5.8f),90f,.78f);
            // Staff room / reading nook
            PlaceAsset(root.transform,"rug_rectangle_stripes_A",new Vector3(3.3f,.02f,3.6f),0f,1.05f);
            PlaceAsset(root.transform,"couch_pillows",new Vector3(2.2f,0,5.2f),180f,.82f);
            PlaceAsset(root.transform,"armchair_pillows",new Vector3(5.4f,0,4.0f),-90f,.82f);
            PlaceAsset(root.transform,"table_low",new Vector3(3.7f,0,3.9f),0f,.80f);
            PlaceAsset(root.transform,"lamp_standing",new Vector3(6.1f,0,5.6f),0f,.80f);
            // Library
            PlaceAsset(root.transform,"shelf_B_large_decorated",new Vector3(10.0f,0,6.2f),180f,.78f);
            PlaceAsset(root.transform,"shelf_B_small_decorated",new Vector3(10.0f,0,3.5f),180f,.78f);
            PlaceAsset(root.transform,"armchair_pillows",new Vector3(9.8f,0,.7f),0f,.78f);
            // Corridor lockers/benches
            for(int i=-6;i<=6;i++) if(i%2==0) Box("locker",root.transform,new Vector3(i*2.2f,.58f,-1.55f),new Vector3(.95f,1.1f,.35f),i%4==0?blue:cream,false);
        }

        static void BuildArenaInterior(Transform parent, Transform arena) {
            GameObject root=new GameObject("ICE RINK INTERIOR · AUTHORED V90");root.transform.SetParent(parent,true);root.transform.position=arena.position;root.transform.rotation=arena.rotation;
            Renderer[] rr=arena.GetComponentsInChildren<Renderer>(true); Bounds b=RendererBounds(arena);
            // Local proportions are intentionally standard-rink-like even if OSM footprint is a little irregular.
            float rw=Mathf.Clamp(Mathf.Min(b.size.x,b.size.z)*.72f,20f,27f); float rd=Mathf.Clamp(Mathf.Max(b.size.x,b.size.z)*.72f,34f,48f);
            Box("ice",root.transform,new Vector3(0,.04f,0),new Vector3(rw,.08f,rd),ice,false);
            float h=.82f;
            LowWall(root.transform,new Vector3(-rw*.5f,0,-rd*.5f),new Vector3(-rw*.5f,0,rd*.5f),h,white);
            LowWall(root.transform,new Vector3(rw*.5f,0,-rd*.5f),new Vector3(rw*.5f,0,rd*.5f),h,white);
            LowWall(root.transform,new Vector3(-rw*.5f,0,-rd*.5f),new Vector3(rw*.5f,0,-rd*.5f),h,white);
            LowWall(root.transform,new Vector3(-rw*.5f,0,rd*.5f),new Vector3(rw*.5f,0,rd*.5f),h,white);
            Box("center line",root.transform,new Vector3(0,.09f,0),new Vector3(rw*.96f,.018f,.12f),rinkRed,false);
            Box("blue line A",root.transform,new Vector3(0,.09f,-rd*.24f),new Vector3(rw*.96f,.018f,.12f),rinkBlue,false);
            Box("blue line B",root.transform,new Vector3(0,.09f,rd*.24f),new Vector3(rw*.96f,.018f,.12f),rinkBlue,false);
            for(int i=0;i<4;i++) Box("bleacher",root.transform,new Vector3(rw*.5f+1.2f+i*.42f,.22f+i*.18f,0),new Vector3(.55f,.16f,rd*.52f),wood,false);
        }

        static void BuildCampus(Transform parent, Transform school, Transform arena) {
            Box("school forecourt",parent,new Vector3(0,.01f,-4.3f),new Vector3(31f,.025f,8.0f),packed,false);
            for(int i=-2;i<=2;i++) Lamp(parent,new Vector3(i*5.5f,0,-8.3f));
            // A narrow, clear pedestrian connection toward the arena rather than the giant V89 slab.
            Vector3 a=new Vector3(16f,.012f,1f), b=arena.position; b.y=.012f;
            SlabBetween("campus footpath",parent,a,b,2.0f,packed);
        }

        static void TuneLightingAndCamera() {
            RenderSettings.ambientLight=new Color(.40f,.47f,.50f);
            RenderSettings.fogColor=new Color(.50f,.58f,.62f);
            RenderSettings.fogDensity=.0030f;
            Camera cam=Camera.main ? Camera.main : UnityEngine.Object.FindFirstObjectByType<Camera>();
            if(cam) {
                cam.orthographic=true; cam.orthographicSize=9.2f; cam.backgroundColor=new Color(.27f,.34f,.37f);
                CozyCameraV57 rig=cam.GetComponent<CozyCameraV57>();
                if(rig){rig.minSize=7.4f;rig.maxSize=12.8f;rig.offset=new Vector3(0,18.0f,-14.7f);rig.lookAhead=.50f;EditorUtility.SetDirty(rig);}EditorUtility.SetDirty(cam);
            }
        }

        static void FixAnyPinkMaterials(GameObject world) {
            foreach(Renderer r in world.GetComponentsInChildren<Renderer>(true)) {
                if(!r) continue;
                Material m=r.sharedMaterial; bool bad=!m || !m.shader || !m.shader.isSupported || m.shader.name.Contains("InternalError");
                if(!bad) continue;
                string n=r.gameObject.name.ToLowerInvariant();
                if(n.Contains("snow")) r.sharedMaterial=snow;
                else if(n.Contains("road")||n.Contains("asphalt")) r.sharedMaterial=packed;
                else if(n.Contains("arena")) r.sharedMaterial=blue;
                else if(n.Contains("roof")) r.sharedMaterial=roof;
                else r.sharedMaterial=cream;
                EditorUtility.SetDirty(r);
            }
        }

        static Shader PickKnownGoodShader(GameObject world) {
            string[] paths={"Assets/Rosvik/GeneratedV88/school_ochre.mat","Assets/Rosvik/GeneratedV88/arena_blue.mat","Assets/Rosvik/GeneratedV88/roof.mat"};
            foreach(string p in paths){Material m=AssetDatabase.LoadAssetAtPath<Material>(p);if(m&&m.shader&&m.shader.isSupported&&!m.shader.name.Contains("Error"))return m.shader;}
            foreach(Renderer r in world.GetComponentsInChildren<Renderer>(true)) if(r&&r.sharedMaterial&&r.sharedMaterial.shader&&r.sharedMaterial.shader.isSupported&&!r.sharedMaterial.shader.name.Contains("Error")) return r.sharedMaterial.shader;
            Shader s=Shader.Find("Universal Render Pipeline/Lit");if(!s||!s.isSupported)s=Shader.Find("Universal Render Pipeline/Simple Lit");if(!s||!s.isSupported)s=Shader.Find("Standard");if(!s||!s.isSupported)throw new Exception("No supported shader");return s;
        }

        static void BuildMaterials() {
            snow=Mat("snow","cbd6da",.12f); packed=Mat("packed","87979c",.05f); ochre=Mat("school_ochre","a4763f",.10f); brick=Mat("brick","6d4037",.08f); blue=Mat("arena_blue","365f73",.10f); cream=Mat("cream","c9c1ae",.12f); trim=Mat("trim","e1ddd0",.16f); glass=Mat("glass","324e5d",.36f); warmGlass=Emissive("warm_glass","e8a95e",1.05f); roof=Mat("roof","2e424d",.18f); metal=Mat("metal","465358",.22f); wood=Mat("wood","76563f",.10f); greenFloor=Mat("green_floor","596d67",.08f); warmFloor=Mat("warm_floor","806b55",.08f); ice=Mat("ice","b8c9cf",.40f); rinkBlue=Mat("rink_blue","4d82a8",.05f); rinkRed=Mat("rink_red","a34c49",.05f); white=Mat("rink_white","dbddd5",.06f);
        }
        static Material Mat(string name,string hex,float smooth){string path=Generated+"/"+name+".mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}if(m.shader!=shader)m.shader=shader;Color c=Hex(hex);if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);if(m.HasProperty("_Color"))m.SetColor("_Color",c);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;}
        static Material Emissive(string name,string hex,float intensity){Material m=Mat(name,hex,.15f);if(m.HasProperty("_EmissionColor")){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",Hex(hex)*intensity);}return m;}

        static void Window(Transform p,Vector3 lp,float yaw,Material pane,float width){Box("window frame",p,lp,new Vector3(width+.24f,1.34f,.16f),trim,false,Quaternion.Euler(0,yaw,0));Box("window glass",p,lp+Quaternion.Euler(0,yaw,0)*new Vector3(0,0,-.09f),new Vector3(width,1.04f,.05f),pane,false,Quaternion.Euler(0,yaw,0));Box("mullion",p,lp+Quaternion.Euler(0,yaw,0)*new Vector3(0,0,-.13f),new Vector3(.05f,1.02f,.04f),trim,false,Quaternion.Euler(0,yaw,0));}
        static void WorldText(Transform p,string text,Vector3 lp,Quaternion rot,float size){GameObject g=new GameObject("world text");g.transform.SetParent(p,false);g.transform.localPosition=lp;g.transform.localRotation=rot;TextMesh tm=g.AddComponent<TextMesh>();tm.text=text;tm.fontSize=64;tm.characterSize=size;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;tm.color=new Color(.96f,.95f,.90f);}
        static void Lamp(Transform p,Vector3 pos){Box("lamp post",p,pos+Vector3.up*1.6f,new Vector3(.10f,3.2f,.10f),metal,false);Box("lamp head",p,pos+new Vector3(.18f,3.05f,0),new Vector3(.45f,.12f,.22f),metal,false);}
        static void SlabBetween(string name,Transform p,Vector3 a,Vector3 b,float width,Material m){Vector3 d=b-a;d.y=0;float len=d.magnitude;if(len<.2f)return;float yaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;Box(name,p,(a+b)*.5f,new Vector3(width,.025f,len),m,false,Quaternion.Euler(0,yaw,0));}
        static void LowWall(Transform p,Vector3 a,Vector3 b,float h,Material m=null){Vector3 d=b-a;d.y=0;float len=d.magnitude;if(len<.15f)return;float yaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;Box("low wall",p,(a+b)*.5f+Vector3.up*(h*.5f),new Vector3(.16f,h,len),m?m:cream,false,Quaternion.Euler(0,yaw,0));}
        static GameObject Box(string name,Transform p,Vector3 lp,Vector3 scale,Material mat,bool col,Quaternion? rot=null){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(p,false);g.transform.localPosition=lp;g.transform.localRotation=rot??Quaternion.identity;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;if(!col)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
        static Transform Group(Transform p,string n){GameObject g=new GameObject(n);g.transform.SetParent(p,false);return g.transform;}
        static void ClearChildren(Transform p){for(int i=p.childCount-1;i>=0;i--)UnityEngine.Object.DestroyImmediate(p.GetChild(i).gameObject);}
        static Transform FindChild(Transform p,string n){foreach(Transform c in p)if(c.name==n)return c;return null;}
        static GameObject FindDeep(Transform p,string n){foreach(Transform t in p.GetComponentsInChildren<Transform>(true))if(t.name==n)return t.gameObject;return null;}
        static Bounds RendererBounds(GameObject go){Renderer[] rr=go.GetComponentsInChildren<Renderer>(true);Bounds b=new Bounds(go.transform.position,Vector3.zero);bool first=true;foreach(Renderer r in rr){if(!r)continue;if(first){b=r.bounds;first=false;}else b.Encapsulate(r.bounds);}return b;}
        static float DominantWallYaw(GameObject go){Transform best=null;float len=-1;foreach(Transform t in go.GetComponentsInChildren<Transform>(true)){string n=t.name.ToLowerInvariant();if(!n.Contains("wall")&&!n.Contains("front")&&!n.Contains("back")&&!n.Contains("left")&&!n.Contains("right"))continue;float l=Mathf.Max(t.lossyScale.x,t.lossyScale.z);if(l>len){len=l;best=t;}}return best?best.eulerAngles.y:0f;}
        static Color Hex(string s){Color c=Color.white;ColorUtility.TryParseHtmlString("#"+s,out c);return c;}

        static void PlaceAsset(Transform parent,string assetName,Vector3 lp,float yaw,float scale){string[] ids=AssetDatabase.FindAssets(assetName+" t:GameObject",new[]{"Assets/Rosvik/ThirdParty/V58Furniture","Assets"});GameObject prefab=null;foreach(string id in ids){string path=AssetDatabase.GUIDToAssetPath(id);if(Path.GetFileNameWithoutExtension(path).Equals(assetName,StringComparison.OrdinalIgnoreCase)){prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(prefab)break;}}if(!prefab)return;GameObject g=(GameObject)PrefabUtility.InstantiatePrefab(prefab);g.name=assetName;g.transform.SetParent(parent,false);g.transform.localPosition=lp;g.transform.localRotation=Quaternion.Euler(0,yaw,0);g.transform.localScale=Vector3.one*scale;foreach(Collider c in g.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);}
        static void CleanupMissingScripts(){foreach(GameObject g in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(g&&g.scene.IsValid())GameObjectUtility.RemoveMonoBehavioursWithMissingScript(g);}
    }
}
#endif