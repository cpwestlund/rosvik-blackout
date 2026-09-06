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
    [InitializeOnLoad]
    public static class RosvikSchoolCampusV49 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_SCHOOL_CAMPUS_V49_VERSION";
        const int Version = 49;
        const string GroupName = "31 SCHOOL CAMPUS V49 - TOP LEVEL VISIBLE REBUILD";
        const string GeneratedDir = "Assets/Rosvik/GeneratedV49";
        const string CityRoot = "Assets/Rosvik/ThirdParty/V40CozyBits";
        const string NatureRoot = "Assets/Rosvik/ThirdParty/V22Models/KenneyNature";
        const long SchoolWay = 163199458;

        static RosvikSchoolCampusV49() {
            EditorApplication.delayCall -= Auto;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/Rebuild School Campus V49 - TOP LEVEL VISIBLE")]
        public static void Force() {
            EditorPrefs.DeleteKey(Key);
            BuildNow();
        }

        static void Auto() {
            if (EditorPrefs.GetInt(Key,0) >= Version) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) {
                EditorApplication.delayCall += Auto;
                return;
            }
            BuildNow();
        }

        static void BuildNow() {
            try {
                if (!File.Exists(ScenePath)) {
                    Debug.LogError("ROSVIK V49: scene not found: " + ScenePath);
                    return;
                }

                UScene scene = EditorSceneManager.GetActiveScene();
                if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

                Directory.CreateDirectory(GeneratedDir);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                var ways = RosvikOsmV15.LoadWays();
                var school = ways == null ? null : ways.FirstOrDefault(w => w.Id == SchoolWay);
                if (school == null) throw new Exception("School footprint 163199458 not found");

                Vector3 schoolCenter = RosvikOsmV15.Centroid(school); schoolCenter.y = .03f;
                Vector3 entrance, forward, right;
                ResolveEntrance(scene, school, schoolCenter, out entrance, out forward, out right);

                // IMPORTANT: V49 is a TOP-LEVEL scene object. It no longer depends on any historical
                // ROSVIK_* root being found or active. This fixes the silent no-op failure mode.
                RemoveTopLevel(scene, GroupName);
                DisableAnywhere(scene, "28 SCHOOL CAMPUS V46 - COZY DENSITY PASS");
                DisableAnywhere(scene, "29 SCHOOL CAMPUS V47 - ENTRANCE ANCHORED");
                DisableAnywhere(scene, "30 SCHOOL CAMPUS V48 - FINAL EXTERIOR REBUILD");

                GameObject root = new GameObject(GroupName);
                root.transform.position = Vector3.zero;

                Material lawn = Mat("lawn", new Color(.115f,.19f,.075f), .02f);
                Material asphalt = Mat("asphalt", new Color(.075f,.085f,.085f), .20f);
                Material worn = Mat("worn_asphalt", new Color(.125f,.13f,.12f), .08f);
                Material paving = Mat("paving", new Color(.46f,.38f,.27f), .08f);
                Material gravel = Mat("gravel", new Color(.34f,.30f,.23f), .03f);
                Material rubber = Mat("rubber", new Color(.27f,.16f,.095f), .03f);
                Material curb = Mat("curb", new Color(.58f,.57f,.52f), .10f);
                Material white = Mat("paint", new Color(.88f,.87f,.77f), .04f);
                Material snow = Mat("snow", new Color(.92f,.94f,.93f), .18f);
                Material soil = Mat("soil", new Color(.16f,.095f,.05f), .02f);
                Material wood = Mat("wood", new Color(.32f,.16f,.065f), .04f);
                Material metal = Mat("metal", new Color(.05f,.055f,.052f), .28f);
                Material proof = Emissive("proof_orange", new Color(1f,.24f,.02f), 2.4f);
                Material bulb = Emissive("bulb", new Color(1f,.55f,.18f), 3.0f);
                Material city = Textured("citybits", AssetDatabase.LoadAssetAtPath<Texture2D>(CityRoot+"/citybits_texture.png"), .18f);
                Material spruce = Mat("spruce", new Color(.035f,.11f,.052f), .02f);
                Material autumn = Mat("autumn", new Color(.39f,.29f,.075f), .02f);
                Material shrub = Mat("shrub", new Color(.085f,.16f,.055f), .02f);

                GameObject bench = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/bench.obj");
                GameObject lamp = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/streetlight.obj");
                GameObject sedan = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/car_sedan.obj");
                GameObject wagon = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/car_stationwagon.obj");
                GameObject hatch = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/car_hatchback.obj");
                GameObject pine = AssetDatabase.LoadAssetAtPath<GameObject>(NatureRoot+"/tree_pineDefaultA.obj");
                GameObject tallPine = AssetDatabase.LoadAssetAtPath<GameObject>(NatureRoot+"/tree_pineTallA.obj");
                GameObject birch = AssetDatabase.LoadAssetAtPath<GameObject>(NatureRoot+"/tree_default_fall.obj");
                GameObject bush = AssetDatabase.LoadAssetAtPath<GameObject>(NatureRoot+"/plant_bushDetailed.obj");

                BuildCampus(root.transform, entrance, forward, right, lawn, asphalt, worn, paving, gravel, rubber, curb, white, snow, soil, wood, metal,
                    city, spruce, autumn, shrub, bulb, bench, lamp, sedan, wagon, hatch, pine, tallPine, birch, bush);

                // Temporary unmistakable proof marker. If this orange post is visible, V49 definitely ran.
                Vector3 proofPos = entrance + forward*8.2f + right*13.5f;
                Box("V49 TEMP PROOF MARKER", root.transform, proofPos+Vector3.up*2.5f, new Vector3(.65f,5f,.65f), Quaternion.identity, proof);
                AddLight(root.transform, proofPos+Vector3.up*3.5f, 8f, 1.3f, proof);

                TuneMood();
                EditorPrefs.SetInt(Key,Version);
                Selection.activeGameObject = root;
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();

                if (SceneView.lastActiveSceneView != null) {
                    SceneView.lastActiveSceneView.FrameSelected();
                    SceneView.lastActiveSceneView.Repaint();
                }

                Debug.Log("ROSVIK V49 SUCCESS: top-level visible campus created at entrance " + entrance + ". Look for orange V49 proof marker.");
            }
            catch(Exception ex) {
                Debug.LogError("ROSVIK V49 FAILED: " + ex);
            }
        }

        static void ResolveEntrance(UScene scene, RosvikOsmV15.Way school, Vector3 center, out Vector3 entrance, out Vector3 forward, out Vector3 right) {
            Transform canopy = Resources.FindObjectsOfTypeAll<Transform>()
                .FirstOrDefault(t => t && t.gameObject.scene == scene && t.name == "large timber canopy");

            if (canopy) {
                entrance = canopy.position; entrance.y=.03f;
                forward = Flat(entrance-center).normalized;
                if (forward.sqrMagnitude < .1f) forward = Vector3.back;
                right = new Vector3(forward.z,0,-forward.x);
                return;
            }

            List<Vector3> pts = school.Nodes.Select(n=>n.Pos).ToList();
            if (school.Closed && pts.Count>1) pts.RemoveAt(pts.Count-1);
            float best=-1f; Vector3 a=pts[0], b=pts[1];
            for(int i=0;i<pts.Count;i++) {
                Vector3 aa=pts[i], bb=pts[(i+1)%pts.Count];
                float len=Flat(bb-aa).magnitude;
                if(len>best){best=len;a=aa;b=bb;}
            }
            entrance=(a+b)*.5f; entrance.y=.03f;
            Vector3 edge=Flat(b-a).normalized;
            Vector3 nrm=new Vector3(-edge.z,0,edge.x);
            if(Vector3.Dot(nrm,Flat(entrance-center))<0)nrm=-nrm;
            forward=nrm.normalized;
            right=new Vector3(forward.z,0,-forward.x);
        }

        static void BuildCampus(Transform root, Vector3 e, Vector3 f, Vector3 r,
            Material lawn, Material asphalt, Material worn, Material paving, Material gravel, Material rubber, Material curb, Material paint, Material snow,
            Material soil, Material wood, Material metal, Material city, Material spruce, Material autumn, Material shrub, Material bulb,
            GameObject bench, GameObject lamp, GameObject sedan, GameObject wagon, GameObject hatch,
            GameObject pine, GameObject tallPine, GameObject birch, GameObject bush) {

            // A visibly coherent campus base instead of scattered props on the world ground.
            Vector3 baseC=e+f*10f; baseC.y=.012f;
            FlatBox("V49 campus lawn base",root,baseC,r,112f,86f,.035f,lawn);

            Vector3 plaza=e+f*5.8f; plaza.y=.05f;
            FlatBox("V49 entrance plaza",root,plaza,r,30f,13f,.085f,paving);
            Curbs(root,plaza,r,f,30.5f,13.5f,curb,false);

            Vector3 parking=e+f*24f; parking.y=.045f;
            FlatBox("V49 main parking",root,parking,r,46f,21f,.075f,asphalt);
            Curbs(root,parking,r,f,46.6f,21.6f,curb,true);

            Vector3 approach=e+f*41f; approach.y=.042f;
            FlatBox("V49 road approach",root,approach,r,10f,30f,.07f,worn);
            Curbs(root,approach,r,f,10.5f,30.5f,curb,true);

            for(int i=-7;i<=7;i++)
                FlatBox("V49 parking stripe",root,parking+r*(i*2.9f)+f*5.3f,f,.12f,6.5f,.018f,paint);
            Vector3 zebra=e+f*14f;
            for(int i=-5;i<=5;i++) FlatBox("V49 zebra",root,zebra+r*(i*.62f),r,.37f,5.2f,.021f,paint);

            // Strong schoolyard block on one side.
            Vector3 yard=e-r*28f+f*7f; yard.y=.043f;
            FlatBox("V49 schoolyard gravel",root,yard,r,38f,28f,.07f,gravel);
            Curbs(root,yard,r,f,38.5f,28.5f,curb,false);
            Vector3 play=yard-r*7f+f*2f;
            FlatBox("V49 play rubber",root,play,r,13f,10f,.055f,rubber);
            Swing(root,play-r*3f,r,wood,metal);
            Picnic(root,yard+r*9f-f*6f,r,wood,metal);
            Picnic(root,yard+r*5f-f*6f,r,wood,metal);

            // Entrance planters + racks + benches.
            for(int s=-1;s<=1;s+=2) {
                Vector3 pc=plaza+r*(s*9.5f)-f*3.6f;
                FlatBox("V49 planter soil",root,pc,r,5.2f,2.2f,.15f,soil);
                Curbs(root,pc,r,f,5.5f,2.5f,curb,false);
                if(bush) for(int k=-1;k<=1;k++) Place(bush,root,"V49 entrance shrub",pc+r*(k*1.2f),20+k*37+s*11,.68f,shrub);
                if(bench) Place(bench,root,"V49 entrance bench",plaza+r*(s*11f)+f*.4f,Yaw(-r*s),.92f,city);
            }
            Vector3 rack=e+f*7.2f-r*5.5f;
            for(int i=0;i<12;i++) BikeRack(root,rack+r*i*.95f,f,metal);

            // Vehicles make the parking readable immediately.
            if(sedan) Place(sedan,root,"V49 abandoned sedan",parking-r*12f+f*3.5f,Yaw(f)+3f,1.43f,city);
            if(wagon) Place(wagon,root,"V49 caretaker wagon",parking+r*8.5f-f*3.2f,Yaw(-f)-5f,1.48f,city);
            if(hatch) Place(hatch,root,"V49 hatchback",parking+r*14f+f*3.4f,Yaw(f)-2f,1.38f,city);

            // Visible snow banks along parking and yard edges.
            for(int i=-5;i<=5;i++) {
                Vector3 q=parking+r*(i*4.1f)-f*10.5f;
                Box("V49 snowbank",root,q+Vector3.up*.22f,new Vector3(3.0f,.42f,1.05f),Rot(r),snow);
            }
            for(int i=-4;i<=4;i++) {
                Vector3 q=yard-r*19f+f*(i*3.1f);
                Box("V49 yard snowbank",root,q+Vector3.up*.18f,new Vector3(2.7f,.34f,.9f),Rot(f),snow);
            }

            // Lamps in deliberate rows, not random scatter.
            if(lamp) {
                Vector3[] lp={plaza-r*13f-f*4.8f,plaza+r*13f-f*4.8f,parking-r*21f,parking+r*21f,yard-r*17f+f*12f,yard+r*17f-f*12f};
                foreach(Vector3 q in lp){Place(lamp,root,"V49 campus lamp",q,Yaw(f),4.7f,city);AddLight(root,q+Vector3.up*3.45f,8.5f,1.0f,bulb);}
            }

            // Vegetation belts: clustered perimeter, open human spaces in the middle.
            for(int i=-4;i<=4;i++) {
                Vector3 left=e-r*48f+f*(i*8.0f+8f);
                Vector3 rightC=e+r*48f+f*(i*8.0f+8f);
                GameObject t1=(i%3==0&&tallPine)?tallPine:pine;
                GameObject t2=(i%2==0&&birch)?birch:pine;
                if(t1) Place(t1,root,"V49 west tree belt",left,18+i*27,5.2f+(Mathf.Abs(i)%2)*.5f,spruce);
                if(t2) Place(t2,root,"V49 east tree belt",rightC,61+i*31,5.0f+(Mathf.Abs(i)%3)*.35f,(t2==birch)?autumn:spruce);
                if(bush){Place(bush,root,"V49 belt shrub",left+r*2f,13+i*19,.62f,shrub);Place(bush,root,"V49 belt shrub",rightC-r*2f,44+i*17,.62f,shrub);}
            }
            for(int i=-5;i<=5;i++) {
                Vector3 rear=e-f*30f+r*(i*8.0f);
                GameObject t=(i%3==0&&birch)?birch:pine;
                if(t) Place(t,root,"V49 rear tree belt",rear,37+i*23,5.0f+(Mathf.Abs(i)%2)*.45f,(t==birch)?autumn:spruce);
            }
        }

        static void RemoveTopLevel(UScene scene,string name){
            foreach(GameObject go in scene.GetRootGameObjects()) if(go.name==name) UnityEngine.Object.DestroyImmediate(go);
        }
        static void DisableAnywhere(UScene scene,string name){
            Transform t=Resources.FindObjectsOfTypeAll<Transform>().FirstOrDefault(x=>x&&x.gameObject.scene==scene&&x.name==name);
            if(t)t.gameObject.SetActive(false);
        }
        static Vector3 Flat(Vector3 v){v.y=0;return v;}
        static float Yaw(Vector3 d){d=Flat(d).normalized;return d.sqrMagnitude<.01f?0f:Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;}
        static Quaternion Rot(Vector3 widthAxis){widthAxis=Flat(widthAxis).normalized;if(widthAxis.sqrMagnitude<.01f)widthAxis=Vector3.right;Vector3 depth=new Vector3(-widthAxis.z,0,widthAxis.x);return Quaternion.LookRotation(depth,Vector3.up);}

        static GameObject Box(string name,Transform parent,Vector3 pos,Vector3 scale,Quaternion rot,Material mat){
            GameObject go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,true);go.transform.position=pos;go.transform.rotation=rot;go.transform.localScale=scale;
            Renderer rr=go.GetComponent<Renderer>();if(rr&&mat)rr.sharedMaterial=mat;Collider c=go.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);return go;
        }
        static GameObject FlatBox(string name,Transform parent,Vector3 pos,Vector3 widthAxis,float width,float depth,float thick,Material mat){return Box(name,parent,pos,new Vector3(width,thick,depth),Rot(widthAxis),mat);}
        static void Curbs(Transform p,Vector3 c,Vector3 wa,Vector3 da,float width,float depth,Material m,bool openFront){
            wa=Flat(wa).normalized;da=Flat(da).normalized;float h=.12f,t=.20f;
            Box("curb",p,c+wa*width*.5f+Vector3.up*h*.5f,new Vector3(t,h,depth),Quaternion.LookRotation(da),m);
            Box("curb",p,c-wa*width*.5f+Vector3.up*h*.5f,new Vector3(t,h,depth),Quaternion.LookRotation(da),m);
            Box("curb",p,c-da*depth*.5f+Vector3.up*h*.5f,new Vector3(width,h,t),Rot(wa),m);
            if(!openFront)Box("curb",p,c+da*depth*.5f+Vector3.up*h*.5f,new Vector3(width,h,t),Rot(wa),m);
        }
        static void BikeRack(Transform p,Vector3 c,Vector3 axis,Material m){axis=Flat(axis).normalized;Vector3 f=new Vector3(-axis.z,0,axis.x);Box("bike rack",p,c-f*.28f+Vector3.up*.4f,new Vector3(.07f,.8f,.07f),Quaternion.identity,m);Box("bike rack",p,c+f*.28f+Vector3.up*.4f,new Vector3(.07f,.8f,.07f),Quaternion.identity,m);Box("bike rack",p,c+Vector3.up*.78f,new Vector3(.07f,.07f,.64f),Rot(f),m);}
        static void Swing(Transform p,Vector3 c,Vector3 axis,Material wood,Material metal){axis=Flat(axis).normalized;Vector3 f=new Vector3(-axis.z,0,axis.x);for(int s=-1;s<=1;s+=2){Vector3 b=c+axis*s*2f;Box("swing post",p,b+f*.55f+Vector3.up*1.25f,new Vector3(.14f,2.5f,.14f),Quaternion.identity,wood);Box("swing post",p,b-f*.55f+Vector3.up*1.25f,new Vector3(.14f,2.5f,.14f),Quaternion.identity,wood);}Box("swing beam",p,c+Vector3.up*2.45f,new Vector3(4.4f,.16f,.16f),Rot(axis),wood);}
        static void Picnic(Transform p,Vector3 c,Vector3 axis,Material wood,Material metal){axis=Flat(axis).normalized;Vector3 f=new Vector3(-axis.z,0,axis.x);Box("picnic table",p,c+Vector3.up*.72f,new Vector3(2.8f,.15f,1f),Rot(axis),wood);Box("picnic bench",p,c+f*.9f+Vector3.up*.42f,new Vector3(2.8f,.12f,.34f),Rot(axis),wood);Box("picnic bench",p,c-f*.9f+Vector3.up*.42f,new Vector3(2.8f,.12f,.34f),Rot(axis),wood);}
        static GameObject Place(GameObject prefab,Transform parent,string name,Vector3 pos,float yaw,float targetHeight,Material mat){if(!prefab)return null;GameObject go=(GameObject)UnityEngine.Object.Instantiate(prefab);go.name=name;go.transform.SetParent(parent,true);go.transform.position=pos;go.transform.rotation=Quaternion.Euler(0,yaw,0);Renderer[] rs=go.GetComponentsInChildren<Renderer>(true);if(rs.Length>0){Bounds b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);if(b.size.y>.001f)go.transform.localScale*=targetHeight/b.size.y;if(mat)foreach(Renderer rr in rs)rr.sharedMaterial=mat;}return go;}
        static void AddLight(Transform p,Vector3 pos,float range,float intensity,Material mat){GameObject b=GameObject.CreatePrimitive(PrimitiveType.Sphere);b.name="warm bulb";b.transform.SetParent(p,true);b.transform.position=pos;b.transform.localScale=Vector3.one*.14f;Renderer rr=b.GetComponent<Renderer>();if(rr&&mat)rr.sharedMaterial=mat;Collider c=b.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);Light l=b.AddComponent<Light>();l.type=LightType.Point;l.range=range;l.intensity=intensity;l.color=new Color(1f,.63f,.33f);l.shadows=LightShadows.Soft;}

        static Material Mat(string name,Color color,float smooth){string path=GeneratedDir+"/"+name+".mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;Shader s=Shader.Find("Universal Render Pipeline/Lit");if(!s)s=Shader.Find("Standard");m=new Material(s);m.color=color;if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",smooth);AssetDatabase.CreateAsset(m,path);return m;}
        static Material Textured(string name,Texture2D tex,float smooth){Material m=Mat(name,Color.white,smooth);if(tex){if(m.HasProperty("_BaseMap"))m.SetTexture("_BaseMap",tex);else if(m.HasProperty("_MainTex"))m.SetTexture("_MainTex",tex);EditorUtility.SetDirty(m);}return m;}
        static Material Emissive(string name,Color color,float intensity){Material m=Mat(name,color,.1f);if(m.HasProperty("_EmissionColor")){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*intensity);EditorUtility.SetDirty(m);}return m;}
        static void TuneMood(){RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.36f,.39f,.35f);RenderSettings.fog=true;RenderSettings.fogColor=new Color(.47f,.52f,.51f);RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=100f;RenderSettings.fogEndDistance=330f;Light sun=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l=>l.type==LightType.Directional);if(sun){sun.color=new Color(1f,.84f,.67f);sun.intensity=1.05f;sun.shadows=LightShadows.Soft;}}
    }
}
#endif
