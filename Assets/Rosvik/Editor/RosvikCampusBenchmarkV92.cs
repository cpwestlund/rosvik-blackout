#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Rosvik.Blackout;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikCampusBenchmarkV92 {
        const int Version = 92;
        const string Key = "ROSVIK_CAMPUS_BENCHMARK_V92";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string WorldName = "V88 CLEAN OSM WORLD";
        const string Generated = "Assets/Rosvik/GeneratedV92";

        static Shader shader;
        static Material snow, snowShade, packed, asphalt, ochre, ochreDark, brick, cream, blue, blueDark, roof, glass, warmGlass, metal, wood, pine, trunk, sportFloor, ice, red, white, warmFloor;

        static RosvikCampusBenchmarkV92() {
            if (EditorPrefs.GetInt(Key, 0) >= Version) return;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V92 ASTRA BENCHMARK CAMPUS REBUILD")]
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
            catch (Exception ex) { Debug.LogError("V92 CAMPUS BENCHMARK FAILED: " + ex); }
        }

        static void Apply() {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject world = GameObject.Find(WorldName);
            if (!world) throw new Exception("V88 CLEAN OSM WORLD missing");

            Transform landmarks = FindChildContaining(world.transform, "AUTHORED LANDMARKS");
            Transform interiors = FindChildContaining(world.transform, "CLEAN INTERIORS");
            Transform details = FindChildContaining(world.transform, "WORLD DETAILS");
            if (!landmarks || !interiors || !details) throw new Exception("V88 world groups missing");

            // Hard visual reset of the campus only. Gameplay systems live outside these authored visual groups.
            ClearChildren(landmarks);
            ClearChildren(interiors);
            ClearChildren(details);

            Directory.CreateDirectory(Generated);
            shader = PickShader(world);
            BuildMaterials();

            GameObject campus = new GameObject("ROSVIK CAMPUS · V92 ASTRA BENCHMARK");
            campus.transform.SetParent(landmarks, true);

            GameObject school = BuildSchool(campus.transform);
            GameObject corridor = BuildConnector(campus.transform);
            GameObject sporthall = BuildSporthall(campus.transform);
            GameObject icehall = BuildIcehall(campus.transform);

            Transform insideRoot = Group(interiors, "V92 CLEAN INTERIORS");
            BuildSchoolInterior(insideRoot);
            BuildSportInterior(insideRoot);
            BuildIceInterior(insideRoot);

            BuildTerrain(details);
            BuildCampusDressing(details);
            BuildLighting(details);

            OsmWorldRuntimeV86 runtime = world.GetComponent<OsmWorldRuntimeV86>();
            if (!runtime) runtime = world.AddComponent<OsmWorldRuntimeV86>();

            GameObject schoolRoof = FindDeep(school.transform, "ROOF ROOT");
            GameObject corridorRoof = FindDeep(corridor.transform, "ROOF ROOT");
            GameObject sportRoof = FindDeep(sporthall.transform, "ROOF ROOT");
            GameObject iceRoof = FindDeep(icehall.transform, "ROOF ROOT");

            Collider schoolMain = IndoorVolume(campus.transform, "SCHOOL MAIN INDOOR V92", new Vector3(-10f,1.45f,-4f), new Vector3(25f,2.9f,13f));
            Collider schoolWing = IndoorVolume(campus.transform, "SCHOOL WING INDOOR V92", new Vector3(-16f,1.4f,5.1f), new Vector3(12f,2.8f,7.5f));
            Collider link = IndoorVolume(campus.transform, "CONNECTOR INDOOR V92", new Vector3(3.5f,1.35f,-1f), new Vector3(2.2f,2.7f,3.2f));
            Collider sport = IndoorVolume(campus.transform, "SPORTHALL INDOOR V92", new Vector3(12f,2.0f,-1f), new Vector3(15f,4.0f,18f));
            Collider iceVol = IndoorVolume(campus.transform, "ICEHALL INDOOR V92", new Vector3(11f,2.6f,25f), new Vector3(26f,5.2f,24f));

            runtime.cutaways = new[] {
                new CutawayTargetV86 { visualRoot = schoolRoof, insideVolumes = new[]{schoolMain,schoolWing}, padding = 0f },
                new CutawayTargetV86 { visualRoot = corridorRoof, insideVolumes = new[]{link}, padding = 0f },
                new CutawayTargetV86 { visualRoot = sportRoof, insideVolumes = new[]{sport}, padding = 0f },
                new CutawayTargetV86 { visualRoot = iceRoof, insideVolumes = new[]{iceVol}, padding = 0f }
            };
            runtime.attribution = "© OpenStreetMap contributors · ODbL 1.0";
            EditorUtility.SetDirty(runtime);

            CoziPlayerV57 player = UnityEngine.Object.FindFirstObjectByType<CoziPlayerV57>();
            if (player) {
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc) cc.enabled = false;
                player.transform.position = new Vector3(-10f,.03f,-15.5f);
                player.transform.rotation = Quaternion.identity;
                if (cc) cc.enabled = true;
                player.SetObjective("Utforska skolområdet. Skolan sitter ihop med sporthallen; ishallen ligger norr om dem.");
                EditorUtility.SetDirty(player);
            }

            TuneCamera();
            CleanupMissingScripts();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorPrefs.SetInt(Key, Version);
            SceneView.RepaintAll();
            Debug.Log("V92 COMPLETE — campus rebuilt from one clean authored generation, with school -> indoor connector -> sporthall and separate ice hall north; OSM world and gameplay backend preserved.");
        }

        static GameObject BuildSchool(Transform parent) {
            GameObject root = new GameObject("ROSVIKS SKOLA · V92"); root.transform.SetParent(parent,true);
            Transform walls = Group(root.transform,"WALLS + FACADE");
            Transform roofs = Group(root.transform,"ROOF ROOT");
            Transform det = Group(root.transform,"DETAILS");

            // Main low school building plus a smaller north wing; proportions deliberately compact so it reads like a real campus, not giant map blocks.
            Shell(walls,new Vector3(-10,0,-4),25,13,3.15f,ochre,brick,4.0f);
            GableRoof(roofs,new Vector3(-10,3.15f,-4),25.7f,13.7f,1.25f);
            Shell(walls,new Vector3(-16,0,5.1f),12,7.5f,2.85f,ochreDark,brick,0f);
            GableRoof(roofs,new Vector3(-16,2.85f,5.1f),12.6f,8.1f,1.0f);

            // South-facing entrance.
            Box("entrance brick portal",det,new Vector3(-10,1.35f,-10.62f),new Vector3(5.2f,2.7f,.32f),brick,false);
            Box("entrance glass",det,new Vector3(-10,1.15f,-10.82f),new Vector3(2.25f,2.05f,.10f),glass,false);
            Box("entrance mullion",det,new Vector3(-10,1.15f,-10.88f),new Vector3(.07f,2.0f,.05f),metal,false);
            Box("entrance canopy",det,new Vector3(-10,2.62f,-11.65f),new Vector3(6.2f,.18f,2.1f),roof,false);
            Box("entrance canopy snow",det,new Vector3(-10,2.74f,-11.65f),new Vector3(5.95f,.06f,1.85f),snow,false);
            for(int s=-1;s<=1;s+=2) Box("canopy post",det,new Vector3(-10+s*2.4f,1.28f,-12.0f),new Vector3(.11f,2.5f,.11f),metal,false);
            Sign(det,"ROSVIKS SKOLA",new Vector3(-10,2.36f,-10.82f),new Vector3(4.3f,.42f,.07f),blueDark,white,.060f);

            float[] wx={-20.8f,-18.4f,-16f,-13.6f,-6.4f,-4f,-1.6f};
            for(int i=0;i<wx.Length;i++) Window(det,new Vector3(wx[i],1.65f,-10.55f),0,(i==2||i==5)?warmGlass:glass,1.45f,.82f);
            for(int i=-2;i<=2;i++) Window(det,new Vector3(-22.62f,1.62f,-4f+i*2.1f),90,glass,1.3f,.80f);

            // Details that communicate school use at a glance.
            Box("notice board",det,new Vector3(-14.0f,1.35f,-10.72f),new Vector3(1.3f,.78f,.08f),wood,false);
            Bench(det,new Vector3(-5.2f,0,-11.55f),0);
            for(int i=0;i<8;i++) Box("bike rack",det,new Vector3(-20.4f+i*.58f,.28f,-11.6f),new Vector3(.05f,.56f,.65f),metal,false,Quaternion.Euler(0,0,16));
            return root;
        }

        static GameObject BuildConnector(Transform parent) {
            GameObject root=new GameObject("HEATED SCHOOL-SPORTHALL CONNECTOR · V92"); root.transform.SetParent(parent,true);
            Transform walls=Group(root.transform,"WALLS + GLASS"); Transform roofs=Group(root.transform,"ROOF ROOT");
            Box("connector floor",walls,new Vector3(3.5f,.08f,-1),new Vector3(2.2f,.16f,3.2f),cream,false);
            Box("connector north glass",walls,new Vector3(3.5f,1.35f,.57f),new Vector3(2.0f,2.2f,.10f),warmGlass,false);
            Box("connector south glass",walls,new Vector3(3.5f,1.35f,-2.57f),new Vector3(2.0f,2.2f,.10f),glass,false);
            Box("connector west jamb",walls,new Vector3(2.43f,1.25f,-1),new Vector3(.14f,2.5f,3.2f),brick,false);
            Box("connector east jamb",walls,new Vector3(4.57f,1.25f,-1),new Vector3(.14f,2.5f,3.2f),brick,false);
            for(int i=-1;i<=1;i++) {
                Box("glass mullion north",walls,new Vector3(3.5f+i*.65f,1.35f,.52f),new Vector3(.045f,2.15f,.06f),metal,false);
                Box("glass mullion south",walls,new Vector3(3.5f+i*.65f,1.35f,-2.52f),new Vector3(.045f,2.15f,.06f),metal,false);
            }
            Box("connector roof",roofs,new Vector3(3.5f,2.56f,-1),new Vector3(2.5f,.20f,3.5f),roof,false);
            Box("connector snow",roofs,new Vector3(3.5f,2.68f,-1),new Vector3(2.25f,.055f,3.25f),snow,false);
            return root;
        }

        static GameObject BuildSporthall(Transform parent) {
            GameObject root=new GameObject("ROSVIK SPORTHALL · V92"); root.transform.SetParent(parent,true);
            Transform walls=Group(root.transform,"WALLS + FACADE"); Transform roofs=Group(root.transform,"ROOF ROOT"); Transform det=Group(root.transform,"DETAILS");
            Shell(walls,new Vector3(12,0,-1),15,18,4.3f,cream,brick,0f);
            GableRoof(roofs,new Vector3(12,4.3f,-1),15.7f,18.7f,1.3f);
            Box("sport entrance",det,new Vector3(4.42f,1.25f,-1),new Vector3(.12f,2.4f,2.8f),glass,false);
            Sign(det,"ROSVIK SPORTHALL",new Vector3(4.34f,3.15f,-1),new Vector3(.08f,.48f,5.2f),ochreDark,white,.050f,Quaternion.Euler(0,90,0));
            for(int i=-2;i<=2;i++) Window(det,new Vector3(12+i*2.3f,2.55f,-10.05f),0,glass,1.35f,.72f);
            return root;
        }

        static GameObject BuildIcehall(Transform parent) {
            GameObject root=new GameObject("NORRBOTTEN STÅL ARENA · ISHALL V92"); root.transform.SetParent(parent,true);
            Transform walls=Group(root.transform,"WALLS + STEEL FACADE"); Transform roofs=Group(root.transform,"ROOF ROOT"); Transform det=Group(root.transform,"DETAILS");
            Shell(walls,new Vector3(11,0,25),26,24,5.6f,blue,brick,4.8f);
            GableRoof(roofs,new Vector3(11,5.6f,25),26.8f,24.8f,1.7f);
            // Vertical steel seam rhythm.
            for(float x=-1;x<=23;x+=1.35f) Box("steel seam",det,new Vector3(x,2.9f,12.92f),new Vector3(.035f,5.0f,.045f),blueDark,false);
            Box("arena glass entrance",det,new Vector3(11,1.55f,12.84f),new Vector3(4.6f,3.0f,.10f),glass,false);
            Box("arena canopy",det,new Vector3(11,3.6f,11.8f),new Vector3(6.6f,.18f,2.2f),roof,false);
            Box("arena canopy snow",det,new Vector3(11,3.72f,11.8f),new Vector3(6.35f,.055f,1.95f),snow,false);
            Sign(det,"NORRBOTTEN STÅL ARENA",new Vector3(11,4.35f,12.82f),new Vector3(7.0f,.55f,.07f),cream,blueDark,.058f);
            return root;
        }

        static void BuildSchoolInterior(Transform parent) {
            Transform r=Group(parent,"SCHOOL INTERIOR V92");
            Box("school floor",r,new Vector3(-10,.02f,-4),new Vector3(24.5f,.08f,12.5f),warmFloor,false);
            Box("wing floor",r,new Vector3(-16,.02f,5.1f),new Vector3(11.5f,.08f,7.0f),warmFloor,false);
            // Corridor and room dividers kept low for intentional cutaway readability.
            Box("main corridor",r,new Vector3(-10,.08f,-1.2f),new Vector3(23f,.08f,2.2f),packed,false);
            for(int i=-2;i<=2;i++) {
                float x=-17.2f+i*3.6f;
                Box("class divider",r,new Vector3(x,.65f,-5.0f),new Vector3(.10f,1.3f,6.0f),cream,false);
            }
            // Ordered classroom furniture; no random scatter.
            for(int row=0;row<2;row++) for(int col=0;col<3;col++) {
                float x=-18.0f+col*2.2f, z=-7.0f+row*2.0f;
                Desk(r,new Vector3(x,0,z));
            }
            for(int row=0;row<2;row++) for(int col=0;col<3;col++) {
                float x=-8.0f+col*2.2f, z=-7.0f+row*2.0f;
                Desk(r,new Vector3(x,0,z));
            }
        }

        static void BuildSportInterior(Transform parent) {
            Transform r=Group(parent,"SPORTHALL INTERIOR V92");
            Box("sport floor",r,new Vector3(12,.03f,-1),new Vector3(14.4f,.10f,17.4f),sportFloor,false);
            Box("center line",r,new Vector3(12,.095f,-1),new Vector3(13.6f,.012f,.08f),white,false);
            for(int i=0;i<4;i++) Box("bleacher",r,new Vector3(18.3f,.18f+i*.16f,-5f+i*.30f),new Vector3(1.3f,.18f,6.0f),wood,false);
        }

        static void BuildIceInterior(Transform parent) {
            Transform r=Group(parent,"ICEHALL INTERIOR V92");
            Box("rink ice",r,new Vector3(11,.04f,25),new Vector3(22f,.08f,18f),ice,false);
            Box("center red",r,new Vector3(11,.09f,25),new Vector3(21.4f,.015f,.10f),red,false);
            Box("blue line A",r,new Vector3(11,.09f,20.7f),new Vector3(21.4f,.015f,.12f),blueDark,false);
            Box("blue line B",r,new Vector3(11,.09f,29.3f),new Vector3(21.4f,.015f,.12f),blueDark,false);
            // Simple boards.
            Box("rink board north",r,new Vector3(11,.55f,34.1f),new Vector3(22.7f,1.0f,.16f),white,false);
            Box("rink board south",r,new Vector3(11,.55f,15.9f),new Vector3(22.7f,1.0f,.16f),white,false);
            Box("rink board west",r,new Vector3(-.25f,.55f,25),new Vector3(.16f,1.0f,18.2f),white,false);
            Box("rink board east",r,new Vector3(22.25f,.55f,25),new Vector3(.16f,1.0f,18.2f),white,false);
            for(int i=0;i<4;i++) Box("ice bleacher",r,new Vector3(22.8f+i*.35f,.22f+i*.18f,25),new Vector3(.7f,.20f,8.0f),wood,false);
        }

        static void BuildTerrain(Transform parent) {
            Transform r=Group(parent,"V92 CAMPUS TERRAIN");
            Mesh snowMesh=GridMesh("V92 continuous campus snow",72f,72f,18,18,.11f);
            GameObject g=new GameObject("continuous snow terrain"); g.transform.SetParent(r,true); g.transform.position=new Vector3(0,-.08f,12);
            var mf=g.AddComponent<MeshFilter>(); mf.sharedMesh=snowMesh; var mr=g.AddComponent<MeshRenderer>(); mr.sharedMaterial=snow;
            // Cleared/ploughed surfaces as restrained overlays rather than giant plates.
            Box("school forecourt",r,new Vector3(-10,.005f,-13.2f),new Vector3(26f,.03f,4.0f),packed,false);
            Box("campus lane",r,new Vector3(0,.008f,9.5f),new Vector3(5.0f,.035f,34f),packed,false);
            Box("icehall apron",r,new Vector3(11,.010f,10.5f),new Vector3(17f,.035f,4.5f),packed,false);
            // Soft drifts around edges.
            for(int i=0;i<10;i++) {
                float x=-28f+i*6f; Drift(r,new Vector3(x,0,19f+Mathf.Sin(i*.7f)*7f),1.8f+((i%3)*.45f));
            }
        }

        static void BuildCampusDressing(Transform parent) {
            Transform r=Group(parent,"V92 CAMPUS DRESSING");
            Vector3[] trees={new(-27,0,-8),new(-25,0,2),new(-24,0,15),new(-19,0,20),new(-12,0,23),new(-3,0,18),new(27,0,8),new(28,0,20),new(25,0,32),new(-3,0,39),new(-12,0,34)};
            foreach(var p in trees) Tree(r,p,1f+((Mathf.Abs(p.x+p.z)%3f)*.08f));
            Bench(r,new Vector3(-2.5f,0,-13.4f),0); Bench(r,new Vector3(18.0f,0,9.5f),90); Bench(r,new Vector3(-2f,0,12.5f),90);
            for(int i=0;i<6;i++) Lamp(r,new Vector3(-21f+i*7.5f,0,-14.6f));
            for(int i=0;i<4;i++) Lamp(r,new Vector3(1.0f,0,5+i*8f));
            // A compact playground reads as a schoolyard without dominating the scene.
            Box("swing crossbar",r,new Vector3(-5,2.1f,3.0f),new Vector3(5.4f,.12f,.12f),wood,false);
            for(int s=-1;s<=1;s+=2) {
                Box("swing leg",r,new Vector3(-5+s*2.3f,1.1f,3),new Vector3(.12f,2.2f,.12f),wood,false,Quaternion.Euler(0,0,s*12));
                Box("swing rope",r,new Vector3(-5+s*.9f,1.35f,3),new Vector3(.035f,1.35f,.035f),metal,false);
                Box("swing seat",r,new Vector3(-5+s*.9f,.66f,3),new Vector3(.75f,.10f,.28f),wood,false);
            }
        }

        static void BuildLighting(Transform parent) {
            Transform r=Group(parent,"V92 LIGHTING");
            WarmPoint(r,new Vector3(-10,2.4f,-12.1f),6.5f,2.2f);
            WarmPoint(r,new Vector3(11,3.2f,11.4f),7.0f,2.3f);
            WarmPoint(r,new Vector3(3.5f,2.2f,-1),4.0f,1.2f);
        }

        static void BuildMaterials() {
            snow=Mat("snow",new Color(.72f,.80f,.86f),.16f,0f);
            snowShade=Mat("snow shade",new Color(.61f,.70f,.76f),.18f,0f);
            packed=Mat("packed snow",new Color(.58f,.66f,.70f),.26f,0f);
            asphalt=Mat("asphalt",new Color(.16f,.20f,.22f),.55f,0f);
            ochre=Mat("school ochre",new Color(.52f,.39f,.23f),.38f,0f);
            ochreDark=Mat("school dark ochre",new Color(.40f,.29f,.19f),.42f,0f);
            brick=Mat("brick",new Color(.35f,.16f,.12f),.44f,0f);
            cream=Mat("cream",new Color(.68f,.66f,.58f),.32f,0f);
            blue=Mat("arena blue",new Color(.18f,.29f,.34f),.46f,.08f);
            blueDark=Mat("dark blue",new Color(.10f,.17f,.21f),.52f,.10f);
            roof=Mat("roof",new Color(.15f,.19f,.22f),.56f,.16f);
            glass=Mat("cold glass",new Color(.17f,.27f,.32f),.12f,.05f);
            warmGlass=Mat("warm glass",new Color(.78f,.53f,.25f),.08f,.03f,new Color(.52f,.29f,.08f));
            metal=Mat("metal",new Color(.24f,.28f,.29f),.72f,.62f);
            wood=Mat("wood",new Color(.40f,.25f,.15f),.42f,0f);
            pine=Mat("pine",new Color(.08f,.18f,.15f),.58f,0f);
            trunk=Mat("trunk",new Color(.22f,.14f,.09f),.62f,0f);
            sportFloor=Mat("sport floor",new Color(.44f,.29f,.18f),.38f,0f);
            ice=Mat("ice",new Color(.65f,.77f,.82f),.10f,.05f);
            red=Mat("rink red",new Color(.52f,.10f,.10f),.42f,0f);
            white=Mat("white",new Color(.90f,.90f,.86f),.28f,0f);
            warmFloor=Mat("warm floor",new Color(.34f,.25f,.20f),.44f,0f);
        }

        static Material Mat(string name, Color c, float smooth, float metallic, Color? emission=null) {
            string path=$"{Generated}/{name}.mat";
            Material m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(shader){name=name};AssetDatabase.CreateAsset(m,path);} else m.shader=shader;
            if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c); if(m.HasProperty("_Color"))m.SetColor("_Color",c);
            if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth); if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",metallic);
            if(emission.HasValue){m.EnableKeyword("_EMISSION"); if(m.HasProperty("_EmissionColor"))m.SetColor("_EmissionColor",emission.Value);} else m.DisableKeyword("_EMISSION");
            EditorUtility.SetDirty(m); return m;
        }

        static Shader PickShader(GameObject world) {
            foreach(Renderer r in world.GetComponentsInChildren<Renderer>(true)) if(r && r.sharedMaterial && r.sharedMaterial.shader && r.sharedMaterial.shader.isSupported) return r.sharedMaterial.shader;
            return Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        }

        static void Shell(Transform p, Vector3 c, float w,float d,float h,Material wall,Material plinth,float southOpening) {
            float t=.20f;
            Box("back wall",p,c+new Vector3(0,h*.5f,d*.5f),new Vector3(w,h,t),wall,true);
            Box("left wall",p,c+new Vector3(-w*.5f,h*.5f,0),new Vector3(t,h,d),wall,true);
            Box("right wall",p,c+new Vector3(w*.5f,h*.5f,0),new Vector3(t,h,d),wall,true);
            if(southOpening>0){float side=(w-southOpening)*.5f;Box("front wall L",p,c+new Vector3(-(southOpening*.5f+side*.5f),h*.5f,-d*.5f),new Vector3(side,h,t),wall,true);Box("front wall R",p,c+new Vector3((southOpening*.5f+side*.5f),h*.5f,-d*.5f),new Vector3(side,h,t),wall,true);} else Box("front wall",p,c+new Vector3(0,h*.5f,-d*.5f),new Vector3(w,h,t),wall,true);
            Box("plinth south",p,c+new Vector3(0,.31f,-d*.5f-.02f),new Vector3(w+.08f,.58f,.22f),plinth,false);
            Box("plinth north",p,c+new Vector3(0,.31f,d*.5f+.02f),new Vector3(w+.08f,.58f,.22f),plinth,false);
        }

        static void GableRoof(Transform p, Vector3 baseCenter, float w,float d,float rise) {
            // Two solid-looking sloped roof planes and a snow skin. Low pitch keeps Nordic school/arena proportions.
            float half=d*.5f; float angle=Mathf.Atan2(rise,half)*Mathf.Rad2Deg; float slope=Mathf.Sqrt(half*half+rise*rise);
            Box("roof south",p,baseCenter+new Vector3(0,rise*.5f,-d*.25f),new Vector3(w,.20f,slope+.12f),roof,false,Quaternion.Euler(angle,0,0));
            Box("roof north",p,baseCenter+new Vector3(0,rise*.5f,d*.25f),new Vector3(w,.20f,slope+.12f),roof,false,Quaternion.Euler(-angle,0,0));
            Box("snow south",p,baseCenter+new Vector3(0,rise*.5f+.12f,-d*.25f),new Vector3(w-.12f,.055f,slope-.02f),snow,false,Quaternion.Euler(angle,0,0));
            Box("snow north",p,baseCenter+new Vector3(0,rise*.5f+.12f,d*.25f),new Vector3(w-.12f,.055f,slope-.02f),snow,false,Quaternion.Euler(-angle,0,0));
            Box("ridge",p,baseCenter+new Vector3(0,rise+.07f,0),new Vector3(w+.08f,.10f,.16f),metal,false);
        }

        static void Window(Transform p,Vector3 pos,float yaw,Material pane,float w,float h){
            Transform r=Group(p,"window"); r.position=pos;r.rotation=Quaternion.Euler(0,yaw,0);
            Box("window frame",r,new Vector3(0,0,0),new Vector3(w+.16f,h+.16f,.09f),cream,false);
            Box("window glass",r,new Vector3(0,0,-.055f),new Vector3(w,h,.035f),pane,false);
            Box("window mullion",r,new Vector3(0,0,-.08f),new Vector3(.055f,h,.025f),cream,false);
        }

        static void Sign(Transform p,string text,Vector3 pos,Vector3 size,Material board,Material letters,float charSize,Quaternion? rot=null){
            Transform r=Group(p,"SIGN "+text); r.position=pos; r.rotation=rot??Quaternion.identity;
            Box("sign board",r,Vector3.zero,size,board,false);
            GameObject go=new GameObject("sign text");go.transform.SetParent(r,false); go.transform.localPosition=new Vector3(0,0,-size.z*.56f-.012f);
            TextMesh tm=go.AddComponent<TextMesh>();tm.text=text;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;tm.fontStyle=FontStyle.Bold;tm.fontSize=64;tm.characterSize=charSize;tm.color=letters.color;
        }

        static void Desk(Transform p,Vector3 pos){Box("desk top",p,pos+new Vector3(0,.62f,0),new Vector3(1.15f,.10f,.70f),wood,false);for(int sx=-1;sx<=1;sx+=2)for(int sz=-1;sz<=1;sz+=2)Box("desk leg",p,pos+new Vector3(sx*.47f,.30f,sz*.26f),new Vector3(.07f,.60f,.07f),metal,false);Box("chair",p,pos+new Vector3(0,.38f,.76f),new Vector3(.55f,.75f,.50f),wood,false);}
        static void Bench(Transform p,Vector3 pos,float yaw){Transform r=Group(p,"bench");r.position=pos;r.rotation=Quaternion.Euler(0,yaw,0);Box("seat",r,new Vector3(0,.40f,0),new Vector3(2.2f,.14f,.50f),wood,false);Box("back",r,new Vector3(0,.73f,.20f),new Vector3(2.2f,.55f,.10f),wood,false);for(int s=-1;s<=1;s+=2)Box("leg",r,new Vector3(s*.85f,.20f,0),new Vector3(.10f,.40f,.10f),metal,false);}
        static void Lamp(Transform p,Vector3 pos){Box("lamp post",p,pos+new Vector3(0,1.8f,0),new Vector3(.10f,3.6f,.10f),metal,false);Box("lamp arm",p,pos+new Vector3(.30f,3.45f,0),new Vector3(.65f,.10f,.10f),metal,false);WarmPoint(p,pos+new Vector3(.60f,3.32f,0),4.2f,.75f);}
        static void WarmPoint(Transform p,Vector3 pos,float range,float intensity){GameObject g=new GameObject("warm light");g.transform.SetParent(p,true);g.transform.position=pos;Light l=g.AddComponent<Light>();l.type=LightType.Point;l.range=range;l.intensity=intensity;l.color=new Color(1f,.72f,.42f);l.shadows=LightShadows.Soft;}
        static void Tree(Transform p,Vector3 pos,float scale){Transform r=Group(p,"spruce");r.position=pos;r.localScale=Vector3.one*scale;Primitive(PrimitiveType.Cylinder,"trunk",r,new Vector3(0,.9f,0),new Vector3(.22f,.9f,.22f),trunk,false);for(int i=0;i<3;i++){GameObject c=Primitive(PrimitiveType.Cylinder,"crown",r,new Vector3(0,1.25f+i*.70f,0),new Vector3(1.15f-i*.18f,.36f,1.15f-i*.18f),pine,false);c.transform.localScale=new Vector3(1.1f-i*.15f,.45f,1.1f-i*.15f);} }
        static void Drift(Transform p,Vector3 pos,float s){Primitive(PrimitiveType.Sphere,"snow drift",p,pos+new Vector3(0,.05f,0),new Vector3(s,.18f,s*.72f),snowShade,false);}

        static Mesh GridMesh(string name,float w,float d,int nx,int nz,float amp){Mesh m=new Mesh{name=name};var v=new List<Vector3>();var uv=new List<Vector2>();var tr=new List<int>();for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++){float fx=(x/(float)nx-.5f)*w,fz=(z/(float)nz-.5f)*d;float y=(Mathf.Sin(fx*.17f)+Mathf.Cos(fz*.13f)+Mathf.Sin((fx+fz)*.09f))*.333f*amp;v.Add(new Vector3(fx,y,fz));uv.Add(new Vector2(x/(float)nx,z/(float)nz));}for(int z=0;z<nz;z++)for(int x=0;x<nx;x++){int a=z*(nx+1)+x,b=a+1,c=a+(nx+1),e=c+1;tr.Add(a);tr.Add(c);tr.Add(b);tr.Add(b);tr.Add(c);tr.Add(e);}m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(tr,0);m.RecalculateNormals();m.RecalculateBounds();return m;}

        static Collider IndoorVolume(Transform p,string name,Vector3 pos,Vector3 size){GameObject g=new GameObject(name);g.transform.SetParent(p,true);g.transform.position=pos;BoxCollider c=g.AddComponent<BoxCollider>();c.size=size;c.isTrigger=true;return c;}
        static void TuneCamera(){Camera c=Camera.main;if(!c)return;c.orthographic=true;c.orthographicSize=12.5f;c.backgroundColor=new Color(.11f,.17f,.20f);EditorUtility.SetDirty(c);RenderSettings.fog=true;RenderSettings.fogColor=new Color(.27f,.35f,.40f);RenderSettings.fogDensity=.0065f;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.31f,.36f,.38f);}

        static GameObject Box(string name,Transform p,Vector3 pos,Vector3 scale,Material mat,bool collider,Quaternion? rot=null){return Primitive(PrimitiveType.Cube,name,p,pos,scale,mat,collider,rot);}
        static GameObject Primitive(PrimitiveType type,string name,Transform p,Vector3 pos,Vector3 scale,Material mat,bool collider,Quaternion? rot=null){GameObject g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(p,true);g.transform.position=pos;g.transform.rotation=rot??Quaternion.identity;g.transform.localScale=scale;Renderer r=g.GetComponent<Renderer>();if(r)r.sharedMaterial=mat;if(!collider){Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);}return g;}
        static Transform Group(Transform p,string name){GameObject g=new GameObject(name);g.transform.SetParent(p,true);return g.transform;}
        static Transform FindChildContaining(Transform p,string term){foreach(Transform t in p)if(t.name.IndexOf(term,StringComparison.OrdinalIgnoreCase)>=0)return t;return null;}
        static GameObject FindDeep(Transform p,string exact){foreach(Transform t in p.GetComponentsInChildren<Transform>(true))if(t.name==exact)return t.gameObject;return null;}
        static void ClearChildren(Transform t){for(int i=t.childCount-1;i>=0;i--)UnityEngine.Object.DestroyImmediate(t.GetChild(i).gameObject);}
        static void CleanupMissingScripts(){foreach(GameObject g in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(g&&g.scene.IsValid())GameObjectUtility.RemoveMonoBehavioursWithMissingScript(g);}
    }
}
#endif
