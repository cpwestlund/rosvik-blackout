#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Rosvik.Blackout;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikGraphicalOverhaulV84 {
        const int Version = 84;
        const string Key = "ROSVIK_GRAPHICAL_OVERHAUL_V84";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string GroupName = "V84 GRAPHICAL OVERHAUL";
        const string MatDir = "Assets/Rosvik/GeneratedV84";

        static RosvikGraphicalOverhaulV84() {
            if (EditorPrefs.GetInt(Key, 0) >= Version) return;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V84 GRAPHICAL OVERHAUL - ASTRA PRINCIPLES")]
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
            catch (Exception ex) { Debug.LogError("V84 GRAPHICAL OVERHAUL FAILED: " + ex); }
        }

        static void Apply() {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            CoziPlayerV57 player = UnityEngine.Object.FindFirstObjectByType<CoziPlayerV57>();
            if (!player) throw new Exception("PLAYER missing");

            GameObject old = GameObject.Find(GroupName);
            if (old) UnityEngine.Object.DestroyImmediate(old);
            RemoveLegacyRoofs();

            Directory.CreateDirectory(MatDir);
            Shader shader = PickShader();
            Material roof = Mat(shader, "roof_base", "4a3432", .10f);
            Material roofEdge = Mat(shader, "roof_edge", "252c2e", .04f);
            Material snow = Mat(shader, "roof_snow", "a8b7bd", .20f);
            Material snowShade = Mat(shader, "snow_shadow", "748991", .08f);
            Material foundation = Mat(shader, "foundation", "3e4848", .06f);
            Material trim = Mat(shader, "cream_trim", "c5c0ad", .12f);
            Material darkWood = Mat(shader, "dark_wood", "5a4032", .10f);
            Material warmWood = Mat(shader, "warm_wood", "82644d", .14f);
            Material rubber = Mat(shader, "rubber", "292f30", .03f);
            Material rug = Mat(shader, "runner", "4b5e5c", .12f);
            Material cloth = Mat(shader, "blanket", "7c675d", .18f);
            Material ceramic = Mat(shader, "ceramic", "d3c9ae", .26f);
            Material metal = Mat(shader, "metal", "536164", .34f);
            Material track = Mat(shader, "trampled_snow", "6e838b", .06f);

            GameObject root = new GameObject(GroupName);
            Transform houses = Group(root.transform, "HOUSES - AUTHORED ENVELOPES");
            Transform world = Group(root.transform, "WINTER STORY DRESSING");

            BuildHouse(houses, "HOUSE A V84",
                new Vector2(-36.4f, 2.15f), new Vector2(-21.6f, 16.05f),
                new Vector3(-29f, 0f, 9.10f), 14.8f, 13.9f, -29.15f, 2.25f,
                roof, roofEdge, snow, snowShade, foundation, trim, darkWood, warmWood, rubber, rug, cloth, ceramic, metal, false);

            BuildHouse(houses, "HOUSE B V84",
                new Vector2(-36.3f, 22.10f), new Vector2(-21.7f, 35.80f),
                new Vector3(-29f, 0f, 28.95f), 14.6f, 13.7f, -29.40f, 22.40f,
                roof, roofEdge, snow, snowShade, foundation, trim, darkWood, warmWood, rubber, rug, cloth, ceramic, metal, true);

            BuildWinterGroundStory(world, track, snowShade, darkWood, metal);
            TuneCameraAndLight();

            ItemDescriptionPolishV84 desc = player.GetComponent<ItemDescriptionPolishV84>();
            if (!desc) desc = player.gameObject.AddComponent<ItemDescriptionPolishV84>();
            desc.enabled = true;
            EditorUtility.SetDirty(desc);

            WinterPresentationV84 grade = player.GetComponent<WinterPresentationV84>();
            if (!grade) grade = player.gameObject.AddComponent<WinterPresentationV84>();
            grade.enabled = true;
            grade.useFog = true;
            grade.fogDensity = .0028f;
            EditorUtility.SetDirty(grade);

            CleanupMissingScripts();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorPrefs.SetInt(Key, Version);
            SceneView.RepaintAll();
            Debug.Log("V84 COMPLETE — first Astra-driven graphical overhaul pass: authored gable roofs, robust cutaway, exterior house detail, interior storytelling and colder winter grade.");
        }

        static void RemoveLegacyRoofs() {
            foreach (RoofCutawayV83 r in UnityEngine.Object.FindObjectsByType<RoofCutawayV83>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (r && r.gameObject.scene.IsValid()) UnityEngine.Object.DestroyImmediate(r.gameObject);
            foreach (RoofCutawayV831 r in UnityEngine.Object.FindObjectsByType<RoofCutawayV831>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (r && r.gameObject.scene.IsValid()) UnityEngine.Object.DestroyImmediate(r.gameObject);

            GameObject v83 = GameObject.Find("V83 ASTRA REFERENCE POLISH");
            if (v83) {
                DestroyNamed(v83.transform, "HOUSE A ROOF");
                DestroyNamed(v83.transform, "HOUSE B ROOF");
            }
        }

        static void BuildHouse(Transform parent, string name, Vector2 min, Vector2 max, Vector3 center,
            float width, float depth, float doorX, float frontZ,
            Material roof, Material roofEdge, Material snow, Material snowShade, Material foundation,
            Material trim, Material darkWood, Material warmWood, Material rubber, Material rug,
            Material cloth, Material ceramic, Material metal, bool houseB) {

            GameObject mgr = new GameObject(name);
            mgr.transform.SetParent(parent, true);
            Transform roofVisual = Group(mgr.transform, "ROOF VISUAL");
            Transform exterior = Group(mgr.transform, "CAMERA FACING EXTERIOR");
            Transform permanent = Group(mgr.transform, "PERMANENT EXTERIOR DETAIL");
            Transform interior = Group(mgr.transform, "INTERIOR STORY DETAIL");

            BuildGableRoof(roofVisual, name, center, width, depth, roof, roofEdge, snow);
            BuildFoundation(permanent, center, width, depth, foundation, trim);
            BuildExteriorDressing(exterior, permanent, center, width, depth, doorX, frontZ, trim, darkWood, metal, snowShade, houseB);
            BuildInteriorStory(interior, center, doorX, frontZ, darkWood, warmWood, rubber, rug, cloth, ceramic, metal, houseB);

            HouseEnvelopeV84 cut = mgr.AddComponent<HouseEnvelopeV84>();
            cut.roofVisual = roofVisual.gameObject;
            cut.cameraFacingExterior = exterior.gameObject;
            cut.interiorDetails = interior.gameObject;
            cut.minXZ = min;
            cut.maxXZ = max;
            cut.enterPadding = .85f;
            cut.exitPadding = 1.35f;
            EditorUtility.SetDirty(cut);

            interior.gameObject.SetActive(false);
        }

        static void BuildGableRoof(Transform p, string houseName, Vector3 center, float width, float depth,
            Material roof, Material edge, Material snow) {
            float overhang = .38f;
            float eaveY = 1.52f;
            float ridgeY = 3.04f;
            float roofW = width + overhang * 2f;
            float roofD = depth + overhang * 2f;

            Mesh baseMesh = CreateGableTop(roofW, roofD, eaveY, ridgeY);
            SaveMeshAsset(baseMesh, houseName.Replace(" ", "_") + "_roof.asset");
            MeshObject("roof planes", p, center, baseMesh, roof, true);

            Mesh snowMesh = CreateGableTop(roofW - .28f, roofD - .30f, eaveY + .085f, ridgeY + .085f);
            SaveMeshAsset(snowMesh, houseName.Replace(" ", "_") + "_snow.asset");
            MeshObject("snow blanket", p, center, snowMesh, snow, false);

            Box("ridge cap", p, center + new Vector3(0f, ridgeY + .055f, 0f), new Vector3(.18f, .12f, roofD + .05f), edge, false);
            Box("west gutter", p, center + new Vector3(-roofW * .5f + .02f, eaveY - .055f, 0f), new Vector3(.11f, .10f, roofD), edge, false);
            Box("east gutter", p, center + new Vector3(roofW * .5f - .02f, eaveY - .055f, 0f), new Vector3(.11f, .10f, roofD), edge, false);

            Box("front barge left", p, center + new Vector3(-roofW * .255f, (eaveY + ridgeY) * .5f, -roofD * .5f),
                new Vector3(roofW * .52f, .10f, .10f), edge, false, Quaternion.Euler(0,0,14.2f));
            Box("front barge right", p, center + new Vector3(roofW * .255f, (eaveY + ridgeY) * .5f, -roofD * .5f),
                new Vector3(roofW * .52f, .10f, .10f), edge, false, Quaternion.Euler(0,0,-14.2f));
            Box("rear barge left", p, center + new Vector3(-roofW * .255f, (eaveY + ridgeY) * .5f, roofD * .5f),
                new Vector3(roofW * .52f, .10f, .10f), edge, false, Quaternion.Euler(0,0,14.2f));
            Box("rear barge right", p, center + new Vector3(roofW * .255f, (eaveY + ridgeY) * .5f, roofD * .5f),
                new Vector3(roofW * .52f, .10f, .10f), edge, false, Quaternion.Euler(0,0,-14.2f));

            Vector3 chimney = center + new Vector3(-2.15f, 0f, 2.35f);
            Box("chimney", p, new Vector3(chimney.x, ridgeY + .24f, chimney.z), new Vector3(.62f, 1.15f, .62f), edge, false);
            Box("chimney cap", p, new Vector3(chimney.x, ridgeY + .84f, chimney.z), new Vector3(.78f, .12f, .78f), snow, false);
        }

        static Mesh CreateGableTop(float width, float depth, float eaveY, float ridgeY) {
            float hw = width * .5f, hd = depth * .5f;
            Mesh m = new Mesh();
            m.name = "V84 gable roof";
            m.vertices = new[] {
                new Vector3(-hw,eaveY,-hd), new Vector3(0,ridgeY,-hd), new Vector3(-hw,eaveY,hd), new Vector3(0,ridgeY,hd),
                new Vector3(0,ridgeY,-hd), new Vector3(hw,eaveY,-hd), new Vector3(0,ridgeY,hd), new Vector3(hw,eaveY,hd)
            };
            m.uv = new[] {
                new Vector2(0,0),new Vector2(.5f,0),new Vector2(0,1),new Vector2(.5f,1),
                new Vector2(.5f,0),new Vector2(1,0),new Vector2(.5f,1),new Vector2(1,1)
            };
            m.triangles = new[] { 0,2,1, 1,2,3, 4,6,5, 5,6,7 };
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        static void BuildFoundation(Transform p, Vector3 c, float width, float depth, Material foundation, Material trim) {
            float y = .28f;
            Box("foundation south", p, new Vector3(c.x,y,c.z-depth*.5f+.04f), new Vector3(width,.48f,.18f), foundation, false);
            Box("foundation north", p, new Vector3(c.x,y,c.z+depth*.5f-.04f), new Vector3(width,.48f,.18f), foundation, false);
            Box("foundation west", p, new Vector3(c.x-width*.5f+.04f,y,c.z), new Vector3(.18f,.48f,depth), foundation, false);
            Box("foundation east", p, new Vector3(c.x+width*.5f-.04f,y,c.z), new Vector3(.18f,.48f,depth), foundation, false);
            foreach (Vector3 q in new[] {
                new Vector3(c.x-width*.5f,.78f,c.z-depth*.5f), new Vector3(c.x+width*.5f,.78f,c.z-depth*.5f),
                new Vector3(c.x-width*.5f,.78f,c.z+depth*.5f), new Vector3(c.x+width*.5f,.78f,c.z+depth*.5f)})
                Box("corner board", p, q, new Vector3(.16f,1.55f,.16f), trim, false);
        }

        static void BuildExteriorDressing(Transform cut, Transform permanent, Vector3 c, float width, float depth,
            float doorX, float frontZ, Material trim, Material wood, Material metal, Material snowShade, bool houseB) {
            // Door canopy and believable entrance details. This group disappears indoors so it never obscures play.
            GameObject canopy = Box("entrance canopy", cut, new Vector3(doorX,1.64f,frontZ-.44f), new Vector3(2.65f,.13f,1.15f), wood, false);
            canopy.transform.rotation = Quaternion.Euler(7f,0,0);
            Box("canopy fascia", cut, new Vector3(doorX,1.55f,frontZ-.98f), new Vector3(2.75f,.16f,.10f), trim, false);
            Box("left canopy bracket", cut, new Vector3(doorX-1.03f,1.22f,frontZ-.20f), new Vector3(.10f,.76f,.10f), trim, false);
            Box("right canopy bracket", cut, new Vector3(doorX+1.03f,1.22f,frontZ-.20f), new Vector3(.10f,.76f,.10f), trim, false);

            Box("door step", permanent, new Vector3(doorX,.10f,frontZ-.62f), new Vector3(2.20f,.20f,1.10f), wood, false);
            Box("door mat outside", permanent, new Vector3(doorX,.215f,frontZ-.79f), new Vector3(1.42f,.028f,.64f), metal, false);

            // Gutters feed real-looking downpipes instead of floating roof slabs.
            float x = c.x - width*.5f - .18f;
            Box("downpipe north-west", permanent, new Vector3(x,.78f,c.z+depth*.5f-.35f), new Vector3(.10f,1.46f,.10f), metal, false);
            Box("downpipe shoe", permanent, new Vector3(x+.13f,.10f,c.z+depth*.5f-.35f), new Vector3(.36f,.10f,.10f), metal, false);

            // Window sills line up with the existing windows without covering the glass.
            if (houseB) {
                Sill(permanent,new Vector3(-33.1f,.78f,35.34f),2.25f,0,trim);
                Sill(permanent,new Vector3(-25.2f,.78f,35.34f),1.95f,0,trim);
                Sill(permanent,new Vector3(-36.02f,.78f,26.7f),2.10f,90,trim);
                Sill(permanent,new Vector3(-21.98f,.78f,27.0f),1.95f,90,trim);
            } else {
                Sill(permanent,new Vector3(-33.0f,.78f,16.08f),2.15f,0,trim);
                Sill(permanent,new Vector3(-25.2f,.78f,16.08f),2.00f,0,trim);
                Sill(permanent,new Vector3(-36.42f,.78f,9.1f),2.00f,90,trim);
                Sill(permanent,new Vector3(-21.58f,.78f,9.2f),1.90f,90,trim);
            }

            // Fine vertical facade battens give the large wall planes scale, without masking old windows/doors.
            float southZ = c.z - depth*.5f - .025f;
            for (float px = c.x-width*.5f+.55f; px < c.x+width*.5f-.4f; px += .72f) {
                if (Mathf.Abs(px-doorX) < 1.35f) continue;
                Box("facade batten", cut, new Vector3(px,.88f,southZ), new Vector3(.035f,1.50f,.035f), wood, false);
            }

            // Snow tucked against foundation; low and angular rather than large white spheres.
            for (int i=0;i<5;i++) {
                float px=c.x-width*.42f+i*(width*.84f/4f);
                Box("wind packed snow", permanent, new Vector3(px,.055f,c.z-depth*.5f-.16f), new Vector3(1.65f,.09f,.38f), snowShade, false,
                    Quaternion.Euler(0,(i%2==0?3f:-4f),0));
            }
        }

        static void Sill(Transform p, Vector3 pos, float length, float yaw, Material trim) {
            Vector3 size = yaw == 0 ? new Vector3(length,.10f,.18f) : new Vector3(.18f,.10f,length);
            Box("window sill", p, pos, size, trim, false);
        }

        static void BuildInteriorStory(Transform p, Vector3 c, float doorX, float frontZ, Material darkWood,
            Material warmWood, Material rubber, Material rug, Material cloth, Material ceramic, Material metal, bool houseB) {
            Box("hall runner", p, new Vector3(doorX,.055f,frontZ+1.25f), new Vector3(1.75f,.025f,2.25f), rug, false);
            Boot(p,new Vector3(doorX-.55f,.12f,frontZ+.62f),rubber,-8f);
            Boot(p,new Vector3(doorX-.24f,.12f,frontZ+.69f),rubber,6f);

            // Small everyday objects make the room read as recently occupied, not as an asset dump.
            if (houseB) {
                Box("folded blanket", p, new Vector3(-34.05f,.56f,27.18f), new Vector3(.82f,.08f,.58f), cloth, false, Quaternion.Euler(0,7f,0));
                Mug(p,new Vector3(-32.52f,.53f,26.58f),ceramic);
                Mug(p,new Vector3(-32.86f,.53f,26.72f),ceramic);
                BookStack(p,new Vector3(-32.60f,.53f,26.98f),warmWood,cloth);
                Plate(p,new Vector3(-24.50f,1.07f,34.78f),ceramic);
                Plate(p,new Vector3(-25.05f,1.07f,34.78f),ceramic);
                Box("tea tin",p,new Vector3(-23.62f,1.16f,34.78f),new Vector3(.28f,.30f,.28f),metal,false);
                PlaceAsset("book_set","house B loose books",p,new Vector3(-35.08f,.58f,23.72f),0,.56f);
                PlaceAsset("pictureframe_medium","house B framed photo",p,new Vector3(-35.74f,1.28f,28.15f),90,.62f);
            } else {
                Box("folded scarf", p, new Vector3(-28.05f,.33f,4.00f), new Vector3(.58f,.08f,.38f), cloth, false, Quaternion.Euler(0,-11f,0));
                BookStack(p,new Vector3(-30.10f,.40f,4.15f),warmWood,cloth);
                Mug(p,new Vector3(-30.48f,.40f,4.15f),ceramic);
                PlaceAsset("pictureframe_medium","house A framed photo",p,new Vector3(-35.76f,1.22f,10.1f),90,.60f);
            }
        }

        static void Boot(Transform p, Vector3 pos, Material m, float yaw) {
            Box("winter boot",p,pos,new Vector3(.22f,.24f,.42f),m,false,Quaternion.Euler(0,yaw,0));
            Box("boot cuff",p,pos+new Vector3(0,.18f,-.07f),new Vector3(.22f,.25f,.24f),m,false,Quaternion.Euler(0,yaw,0));
        }

        static void Mug(Transform p, Vector3 pos, Material m) {
            GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name="mug";g.transform.SetParent(p,true);g.transform.position=pos;g.transform.localScale=new Vector3(.09f,.08f,.09f);g.GetComponent<Renderer>().sharedMaterial=m;Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);
        }

        static void Plate(Transform p, Vector3 pos, Material m) {
            GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name="plate";g.transform.SetParent(p,true);g.transform.position=pos;g.transform.localScale=new Vector3(.19f,.018f,.19f);g.GetComponent<Renderer>().sharedMaterial=m;Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);
        }

        static void BookStack(Transform p, Vector3 pos, Material a, Material b) {
            Box("book",p,pos,new Vector3(.34f,.055f,.24f),a,false,Quaternion.Euler(0,6f,0));
            Box("book",p,pos+new Vector3(.02f,.06f,-.01f),new Vector3(.31f,.05f,.22f),b,false,Quaternion.Euler(0,-5f,0));
        }

        static void BuildWinterGroundStory(Transform p, Material track, Material snowShade, Material wood, Material metal) {
            // A coherent walked route between the two houses and the road, not random white blobs.
            for(int i=0;i<13;i++) {
                float z=15.4f+i*.58f;
                float x=-29.25f+Mathf.Sin(i*.62f)*.14f;
                Box("compressed snow path",p,new Vector3(x,.018f,z),new Vector3(1.18f,.018f,.48f),track,false,Quaternion.Euler(0,Mathf.Sin(i*.8f)*4f,0));
            }
            for(int i=0;i<9;i++) {
                float x=-29.2f+i*.83f;
                Box("yard footpath",p,new Vector3(x,.019f,20.55f+Mathf.Sin(i*.7f)*.12f),new Vector3(.56f,.018f,.92f),track,false,Quaternion.Euler(0,88f+(i%2==0?3f:-3f),0));
            }

            // A restrained roadside service cluster gives scale and a visual landmark.
            Box("utility post",p,new Vector3(-20.55f,.72f,18.65f),new Vector3(.12f,1.44f,.12f),wood,true);
            Box("utility cabinet",p,new Vector3(-20.55f,.54f,18.20f),new Vector3(.72f,1.02f,.46f),metal,true);
            Box("cabinet snow cap",p,new Vector3(-20.55f,1.08f,18.20f),new Vector3(.82f,.08f,.56f),snowShade,false);
        }

        static void TuneCameraAndLight() {
            Camera cam = Camera.main;
            if (!cam) cam = UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (cam) { cam.backgroundColor = new Color(.105f,.145f,.16f); EditorUtility.SetDirty(cam); }
            foreach (Light l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude,FindObjectsSortMode.None)) {
                if (!l || l.type != LightType.Directional) continue;
                l.color = new Color(.76f,.83f,.88f);
                l.intensity = Mathf.Clamp(l.intensity,.52f,.72f);
                l.shadows = LightShadows.Soft;
                EditorUtility.SetDirty(l);
                break;
            }
        }

        static void PlaceAsset(string query,string name,Transform parent,Vector3 pos,float yaw,float scale){
            string[] guids=AssetDatabase.FindAssets(query+" t:GameObject");if(guids==null||guids.Length==0)return;
            GameObject prefab=null;foreach(string guid in guids){string path=AssetDatabase.GUIDToAssetPath(guid);GameObject candidate=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(candidate&&candidate.name.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0){prefab=candidate;break;}}if(!prefab)return;
            GameObject o=(GameObject)PrefabUtility.InstantiatePrefab(prefab);if(!o)return;o.name=name;o.transform.SetParent(parent,true);o.transform.position=pos;o.transform.rotation=Quaternion.Euler(0,yaw,0);o.transform.localScale=Vector3.one*scale;foreach(Collider c in o.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
        }

        static void SaveMeshAsset(Mesh mesh,string file) {
            string path=MatDir+"/"+file;
            Mesh existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);}
            else AssetDatabase.CreateAsset(mesh,path);
        }

        static GameObject MeshObject(string name,Transform parent,Vector3 center,Mesh source,Material mat,bool shadows) {
            string path=AssetDatabase.GetAssetPath(source);
            Mesh mesh=string.IsNullOrEmpty(path)?source:AssetDatabase.LoadAssetAtPath<Mesh>(path);
            GameObject g=new GameObject(name);g.transform.SetParent(parent,true);g.transform.position=center;MeshFilter mf=g.AddComponent<MeshFilter>();mf.sharedMesh=mesh;MeshRenderer mr=g.AddComponent<MeshRenderer>();mr.sharedMaterial=mat;mr.shadowCastingMode=shadows?ShadowCastingMode.On:ShadowCastingMode.Off;mr.receiveShadows=true;return g;
        }

        static Transform Group(Transform p,string name){GameObject g=new GameObject(name);g.transform.SetParent(p,true);return g.transform;}

        static GameObject Box(string n,Transform p,Vector3 pos,Vector3 size,Material m,bool collider){return Box(n,p,pos,size,m,collider,Quaternion.identity);}
        static GameObject Box(string n,Transform p,Vector3 pos,Vector3 size,Material m,bool collider,Quaternion rotation){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,true);g.transform.position=pos;g.transform.rotation=rotation;g.transform.localScale=size;Renderer r=g.GetComponent<Renderer>();if(r)r.sharedMaterial=m;Collider c=g.GetComponent<Collider>();if(c&&!collider)UnityEngine.Object.DestroyImmediate(c);return g;}

        static void DestroyNamed(Transform root,string target){if(!root)return;for(int i=root.childCount-1;i>=0;i--){Transform c=root.GetChild(i);if(c&&c.name==target)UnityEngine.Object.DestroyImmediate(c.gameObject);}}

        static Shader PickShader(){bool srp=GraphicsSettings.currentRenderPipeline!=null||GraphicsSettings.defaultRenderPipeline!=null;Shader s=srp?Shader.Find("Universal Render Pipeline/Lit"):Shader.Find("Standard");if(!s||!s.isSupported)s=Shader.Find("Universal Render Pipeline/Simple Lit");if(!s||!s.isSupported)s=Shader.Find("Standard");if(!s||!s.isSupported)throw new Exception("No supported shader");return s;}

        static Material Mat(Shader s,string n,string hex,float smooth){Directory.CreateDirectory(MatDir);string path=MatDir+"/"+n+".mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(path);Color c=Color.white;ColorUtility.TryParseHtmlString("#"+hex,out c);if(!m){m=new Material(s);AssetDatabase.CreateAsset(m,path);}if(m.shader!=s)m.shader=s;if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);if(m.HasProperty("_Color"))m.SetColor("_Color",c);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;}

        static void CleanupMissingScripts(){foreach(GameObject g in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(g&&g.scene.IsValid())GameObjectUtility.RemoveMonoBehavioursWithMissingScript(g);}
    }
}
#endif
