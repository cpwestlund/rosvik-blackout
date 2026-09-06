#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UScene = UnityEngine.SceneManagement.Scene;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikSchoolCampusV47 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_SCHOOL_CAMPUS_V47_VERSION";
        const int Version = 47;
        const string GroupName = "29 SCHOOL CAMPUS V47 - ENTRANCE ANCHORED";
        const string GeneratedDir = "Assets/Rosvik/GeneratedV47";
        const string ProvenMaterial = "Assets/Rosvik/GeneratedV20/mat_ground.mat";
        const string CityRoot = "Assets/Rosvik/ThirdParty/V40CozyBits";
        const string NatureRoot = "Assets/Rosvik/ThirdParty/V22Models/KenneyNature";
        const long SchoolWay = 163199458;

        static RosvikSchoolCampusV47() { EditorApplication.update -= TryApply; EditorApplication.update += TryApply; }

        [MenuItem("Rosvik/Rebuild School Campus V47 - ENTRANCE ANCHORED")]
        public static void Force() { EditorPrefs.DeleteKey(Key); EditorApplication.update -= TryApply; EditorApplication.update += TryApply; }

        static bool Busy() => EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating;

        static void TryApply() {
            if (EditorPrefs.GetInt(Key,0) >= Version && File.Exists(ScenePath)) { EditorApplication.update -= TryApply; return; }
            if (Busy() || !File.Exists(ScenePath)) return;
            UScene scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (Busy()) return;
            GameObject root = FindRoot();
            if (!root) return;
            EditorApplication.update -= TryApply;
            Apply(scene, root);
        }

        static GameObject FindRoot() {
            GameObject g = GameObject.Find("ROSVIK_HERO_COMPOSITION_V42");
            if (!g) g = GameObject.Find("ROSVIK_HERO_AREA_ASSETS_V41");
            if (!g) g = GameObject.Find("ROSVIK_ASSET_COZY_APOCALYPSE_V40");
            if (!g) g = GameObject.Find("ROSVIK_CLEAN_ROAD_NETWORK_V39");
            if (!g) g = GameObject.Find("ROSVIK_VILLAGE_FABRIC_V38");
            return g;
        }

        static void Apply(UScene scene, GameObject root) {
            try {
                Directory.CreateDirectory(GeneratedDir);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                var ways = RosvikOsmV15.LoadWays();
                var school = ways == null ? null : ways.FirstOrDefault(w => w.Id == SchoolWay);
                if (school == null) throw new Exception("V47 could not find Rosviks skola footprint");
                Vector3 schoolCenter = RosvikOsmV15.Centroid(school); schoolCenter.y = .04f;

                Transform old = Find(root.transform, GroupName);
                if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
                Transform v46 = Find(root.transform, "28 SCHOOL CAMPUS V46 - COZY DENSITY PASS");
                if (v46) v46.gameObject.SetActive(false);
                Transform group = NewGroup(root.transform, GroupName);

                // Crucial V47 fix: anchor everything to the actual V45 entrance canopy that is visibly
                // on the school facade, instead of estimating from the building centroid.
                Transform canopy = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "large timber canopy");
                Vector3 entrance = canopy ? canopy.position : schoolCenter + Vector3.left * 12f;
                entrance.y = .04f;
                Vector3 forward = Flat(entrance - schoolCenter).normalized;
                if (forward.sqrMagnitude < .1f) forward = Vector3.left;
                Vector3 right = new Vector3(forward.z,0,-forward.x);

                Material asphalt = Mat("asphalt", new Color(.105f,.115f,.11f), .22f);
                Material paving = Mat("paving", new Color(.44f,.39f,.31f), .08f);
                Material curb = Mat("curb", new Color(.56f,.54f,.48f), .10f);
                Material grass = Mat("grass", new Color(.16f,.24f,.115f), .02f);
                Material soil = Mat("soil", new Color(.17f,.105f,.065f), .02f);
                Material wood = Mat("wood", new Color(.35f,.19f,.09f), .05f);
                Material metal = Mat("metal", new Color(.055f,.06f,.058f), .30f);
                Material paint = Mat("paint", new Color(.78f,.76f,.66f), .04f);
                Material snow = Mat("first_snow", new Color(.86f,.88f,.86f), .16f);
                Material puddle = Mat("puddle", new Color(.045f,.08f,.09f), .75f);
                Material bulb = Emissive("bulb", new Color(1f,.56f,.20f), 3.1f);
                Material city = Textured("citybits", AssetDatabase.LoadAssetAtPath<Texture2D>(CityRoot+"/citybits_texture.png"), .18f);
                Material spruce = Mat("spruce", new Color(.04f,.115f,.058f), .02f);
                Material autumn = Mat("autumn", new Color(.38f,.28f,.085f), .02f);
                Material shrub = Mat("shrub", new Color(.10f,.17f,.07f), .02f);

                GameObject bench = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/bench.obj");
                GameObject lamp = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/streetlight.obj");
                GameObject sedan = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/car_sedan.obj");
                GameObject hatch = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/car_hatchback.obj");
                GameObject boxA = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/box_A.obj");
                GameObject boxB = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/box_B.obj");
                GameObject dumpster = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/dumpster.obj");
                GameObject pine = AssetDatabase.LoadAssetAtPath<GameObject>(NatureRoot+"/tree_pineDefaultA.obj");
                GameObject tallPine = AssetDatabase.LoadAssetAtPath<GameObject>(NatureRoot+"/tree_pineTallA.obj");
                GameObject fallTree = AssetDatabase.LoadAssetAtPath<GameObject>(NatureRoot+"/tree_default_fall.obj");
                GameObject bush = AssetDatabase.LoadAssetAtPath<GameObject>(NatureRoot+"/plant_bushDetailed.obj");

                BuildEntrance(group, entrance, forward, right, paving, curb, soil, wood, metal, city, bench, lamp, bush, shrub, bulb);
                BuildParking(group, entrance, forward, right, asphalt, curb, paint, grass, city, sedan, hatch, lamp, fallTree, pine, autumn, spruce, bulb);
                BuildSchoolyard(group, entrance, forward, right, grass, curb, paint, wood, metal, city, bench, lamp, pine, tallPine, fallTree, bush, spruce, autumn, shrub, bulb);
                BuildServiceStory(group, entrance, forward, right, asphalt, curb, wood, city, dumpster, boxA, boxB);
                BuildSnowAndWear(group, entrance, forward, right, snow, puddle);
                TuneMood();

                EditorPrefs.SetInt(Key,Version);
                Selection.activeObject = group.gameObject;
                EditorUtility.SetDirty(root);
                AssetDatabase.SaveAssets();
                if (!Busy()) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene,ScenePath); }
                Debug.Log("ROSVIK V47: entrance-anchored campus pass applied at "+entrance+". V46 disabled.");
            } catch(Exception ex) { Debug.LogError("ROSVIK V47 FAILED: "+ex); }
        }

        static void BuildEntrance(Transform p, Vector3 e, Vector3 f, Vector3 r, Material paving, Material curb, Material soil, Material wood, Material metal, Material city, GameObject bench, GameObject lamp, GameObject bush, Material shrub, Material bulb) {
            Transform g=NewGroup(p,"V47 HERO ENTRANCE COURT");
            Vector3 court=e+f*5.4f; court.y=.04f;
            Flat("broad warm entrance paving",g,court,r,24f,10.5f,.09f,paving);
            Curbs(g,court,r,f,24.4f,10.9f,curb,false);

            // Strong facade-side composition: planters, benches, racks and pools of warm light.
            for(int s=-1;s<=1;s+=2) {
                Vector3 planter=court+r*(s*7.2f)-f*3.2f;
                Flat("deep planter soil",g,planter,r,4.0f,1.8f,.18f,soil); Curbs(g,planter,r,f,4.25f,2.05f,curb,false);
                if(bush) {
                    Place(bush,g,"dense entrance shrub",planter-r*(s*.9f)+Vector3.up*.12f,21+s*17,.72f,shrub);
                    Place(bush,g,"dense entrance shrub",planter+r*(s*.9f)+Vector3.up*.12f,74-s*13,.62f,shrub);
                }
                if(bench) Place(bench,g,"entrance bench",court+r*(s*8.7f)+f*.3f,Yaw(-r*s),.92f,city);
            }

            Vector3 rackStart=court-r*4.4f+f*2.2f;
            for(int i=0;i<10;i++) BikeRack(g,rackStart+r*(i*.95f),f,metal);
            AddSign(g,e+f*.9f+r*4.8f+Vector3.up*1.55f,r,wood,"ROSVIKS SKOLA");

            if(lamp) {
                Vector3[] lp={court-r*10.5f-f*3.7f,court+r*10.5f-f*3.7f,court-r*10.5f+f*3.5f,court+r*10.5f+f*3.5f};
                for(int i=0;i<lp.Length;i++){Place(lamp,g,"entrance lamp",lp[i],Yaw(f),4.7f,city);AddLight(g,lp[i]+Vector3.up*3.45f,9.0f,1.15f,bulb);}
            }
        }

        static void BuildParking(Transform p, Vector3 e, Vector3 f, Vector3 r, Material asphalt, Material curb, Material paint, Material grass, Material city, GameObject sedan, GameObject hatch, GameObject lamp, GameObject birch, GameObject pine, Material autumn, Material spruce, Material bulb) {
            Transform g=NewGroup(p,"V47 REAL DROPOFF + PARKING");
            Vector3 c=e+f*18.5f;c.y=.04f;
            Flat("parking asphalt",g,c,r,34f,15f,.075f,asphalt);CurbsOpen(g,c,r,f,34.5f,15.5f,curb);
            for(int i=-5;i<=5;i++) Flat("parking bay",g,c+r*(i*2.85f)+f*3.7f,f,.11f,5.2f,.018f,paint);
            Vector3 zebra=e+f*11.1f;
            for(int i=-4;i<=4;i++)Flat("crosswalk",g,zebra+r*(i*.62f),r,.38f,4.4f,.020f,paint);

            Vector3 island=c+r*1.8f;Flat("parking green island",g,island,r,4.0f,10.8f,.17f,grass);Curbs(g,island,r,f,4.3f,11.1f,curb,false);
            if(birch)Place(birch,g,"parking birch",island-f*2.5f,12f,5.5f,autumn);
            if(pine)Place(pine,g,"parking spruce",island+f*2.5f,65f,5.0f,spruce);
            if(sedan)Place(sedan,g,"abandoned school car",c-r*10.8f+f*2.7f,Yaw(f)+3f,1.43f,city);
            if(hatch)Place(hatch,g,"abandoned hatchback",c+r*11.0f-f*2.5f,Yaw(-f)-4f,1.38f,city);
            if(lamp){Vector3[] lp={c-r*15f,c+r*15f};foreach(Vector3 q in lp){Place(lamp,g,"parking lamp",q,Yaw(f),4.7f,city);AddLight(g,q+Vector3.up*3.45f,8.5f,.95f,bulb);}}
        }

        static void BuildSchoolyard(Transform p, Vector3 e, Vector3 f, Vector3 r, Material grass, Material curb, Material paint, Material wood, Material metal, Material city, GameObject bench, GameObject lamp, GameObject pine, GameObject tallPine, GameObject birch, GameObject bush, Material spruce, Material autumn, Material shrub, Material bulb) {
            Transform g=NewGroup(p,"V47 DENSE SCHOOLYARD");
            Vector3 c=e-r*20f+f*6f;c.y=.04f;
            Flat("schoolyard lawn",g,c,r,28f,20f,.075f,grass);Curbs(g,c,r,f,28.4f,20.4f,curb,false);
            Vector3 court=c+r*3.5f+f*1.5f;
            for(int s=-1;s<=1;s+=2){Flat("court side",g,court+r*(s*5f),f,.10f,9f,.019f,paint);Flat("court end",g,court+f*(s*4.5f),r,10f,.10f,.019f,paint);}Flat("court center",g,court,r,10f,.10f,.019f,paint);
            Swing(g,c-r*8f+f*3f,r,wood,metal);
            Picnic(g,c+r*8.5f-f*5f,r,wood,metal);Picnic(g,c+r*4.8f-f*5.2f,r,wood,metal);
            if(bench){Place(bench,g,"yard bench",c-r*11f-f*6.5f,Yaw(r),.92f,city);Place(bench,g,"yard bench",c+r*11f+f*6.2f,Yaw(-r),.92f,city);}
            if(lamp){Vector3 q=c-r*12f+f*7.5f;Place(lamp,g,"yard lamp",q,Yaw(f),4.7f,city);AddLight(g,q+Vector3.up*3.45f,8.5f,.90f,bulb);}

            Vector3[] tp={c-r*13f+f*8f,c-r*13f-f*8f,c+r*13f-f*8f,c+r*12f+f*8f,c-r*9f-f*9f};
            GameObject[] trees={tallPine,pine,birch,pine,tallPine};Material[] mats={spruce,spruce,autumn,spruce,spruce};
            for(int i=0;i<tp.Length;i++)if(trees[i])Place(trees[i],g,"schoolyard edge tree",tp[i],17+i*51,5.0f+(i%2)*.6f,mats[i]);
            if(bush)for(int i=-3;i<=3;i++)Place(bush,g,"schoolyard hedge",c-r*12.4f+f*(i*2.2f),i*31,.60f+(Mathf.Abs(i)%2)*.12f,shrub);
        }

        static void BuildServiceStory(Transform p, Vector3 e, Vector3 f, Vector3 r, Material asphalt, Material curb, Material wood, Material city, GameObject dumpster, GameObject boxA, GameObject boxB) {
            Transform g=NewGroup(p,"V47 SMALL SERVICE STORY");Vector3 c=e+r*17f-f*2f;c.y=.04f;
            Flat("service pad",g,c,r,13f,8f,.07f,asphalt);CurbsOpen(g,c,r,f,13.4f,8.4f,curb);
            if(dumpster)Place(dumpster,g,"school dumpster",c-r*4.0f+f*2.2f,Yaw(r),1.33f,city);
            if(boxA)Place(boxA,g,"supply crate",c+r*1.6f+f*2.3f,17f,.62f,city);
            if(boxB)Place(boxB,g,"supply crate",c+r*2.3f+f*2.0f,-13f,.49f,city);
            for(int i=-2;i<=2;i++)Box("pallet slat",g,c+r*(i*.45f)-f*2.6f+Vector3.up*.09f,new Vector3(.35f,.08f,1.6f),Rot(r),wood,false);
        }

        static void BuildSnowAndWear(Transform p, Vector3 e, Vector3 f, Vector3 r, Material snow, Material puddle) {
            Transform g=NewGroup(p,"V47 FIRST SNOW + WEAR");
            Patch("snow bank",g,e-r*11.8f+f*4.6f,r,6.5f,1.2f,.028f,snow,24,4701);
            Patch("snow bank",g,e+r*12f+f*7.2f,r,5.8f,1.1f,.028f,snow,23,4702);
            Patch("snow edge",g,e-r*4f+f*18f,r,4.2f,.9f,.029f,snow,21,4703);
            Patch("entrance puddle",g,e+r*2.2f+f*7f,r,2.5f,.75f,.033f,puddle,19,4704);
            Patch("parking puddle",g,e-r*7f+f*19f,r,3.3f,.85f,.033f,puddle,20,4705);
        }

        static void TuneMood(){RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.30f,.285f,.245f);RenderSettings.fog=true;RenderSettings.fogColor=new Color(.37f,.37f,.345f);RenderSettings.fogDensity=.00125f;RenderSettings.reflectionIntensity=.54f;foreach(Light l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(x=>x.type==LightType.Directional)){l.intensity=1.05f;l.color=new Color(1f,.79f,.58f);l.shadows=LightShadows.Soft;l.shadowStrength=.77f;l.transform.rotation=Quaternion.Euler(40f,-48f,0f);}}

        static void BikeRack(Transform p,Vector3 c,Vector3 ax,Material m){Vector3 q=new Vector3(-ax.z,0,ax.x);Box("rack leg",p,c-q*.31f+Vector3.up*.42f,new Vector3(.07f,.84f,.07f),Quaternion.identity,m,false);Box("rack leg",p,c+q*.31f+Vector3.up*.42f,new Vector3(.07f,.84f,.07f),Quaternion.identity,m,false);Box("rack top",p,c+Vector3.up*.82f,new Vector3(.07f,.07f,.72f),Rot(q),m,false);}
        static void Swing(Transform p,Vector3 c,Vector3 ax,Material wood,Material metal){Vector3 q=new Vector3(-ax.z,0,ax.x);for(int s=-1;s<=1;s+=2){Vector3 end=c+ax*s*2.3f;Bar(p,"swing leg",end-q*.62f+Vector3.up*1.43f,(Vector3.up*.95f+q*.43f).normalized,.15f,3.05f,wood);Bar(p,"swing leg",end+q*.62f+Vector3.up*1.43f,(Vector3.up*.95f-q*.43f).normalized,.15f,3.05f,wood);}Bar(p,"swing beam",c+Vector3.up*2.70f,ax,.18f,5.5f,wood);for(int s=-1;s<=1;s+=2){Vector3 seat=c+ax*s*.85f+Vector3.up*.62f;Bar(p,"swing chain",seat-q*.25f+Vector3.up*.92f,Vector3.up,.025f,1.82f,metal);Bar(p,"swing chain",seat+q*.25f+Vector3.up*.92f,Vector3.up,.025f,1.82f,metal);Box("swing seat",p,seat,new Vector3(.68f,.09f,.30f),Rot(ax),wood,false);}}
        static void Picnic(Transform p,Vector3 c,Vector3 ax,Material wood,Material metal){Vector3 q=new Vector3(-ax.z,0,ax.x);Box("picnic top",p,c+Vector3.up*.72f,new Vector3(2.8f,.16f,1.08f),Rot(ax),wood,false);Box("picnic seat",p,c+q*.90f+Vector3.up*.42f,new Vector3(2.8f,.13f,.38f),Rot(ax),wood,false);Box("picnic seat",p,c-q*.90f+Vector3.up*.42f,new Vector3(2.8f,.13f,.38f),Rot(ax),wood,false);for(int s=-1;s<=1;s+=2)for(int t=-1;t<=1;t+=2)Box("picnic leg",p,c+ax*s*.85f+q*t*.30f+Vector3.up*.34f,new Vector3(.12f,.68f,.12f),Quaternion.identity,metal,false);}
        static void AddSign(Transform p,Vector3 c,Vector3 axis,Material wood,string text){Box("school sign",p,c,new Vector3(4.8f,1.4f,.18f),Rot(axis),wood,false);GameObject go=new GameObject(text);go.transform.SetParent(p,true);go.transform.position=c+axis*.11f;go.transform.rotation=Quaternion.LookRotation(-axis,Vector3.up);TextMesh tm=go.AddComponent<TextMesh>();tm.text=text;tm.fontSize=54;tm.characterSize=.085f;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;tm.color=Color.white;}
        static void Bar(Transform p,string n,Vector3 c,Vector3 dir,float th,float len,Material m){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,true);g.transform.position=c;g.transform.rotation=Quaternion.FromToRotation(Vector3.up,dir.normalized);g.transform.localScale=new Vector3(th,len,th);g.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());}
        static void Flat(string n,Transform p,Vector3 c,Vector3 ax,float w,float d,float h,Material m){Box(n,p,new Vector3(c.x,h*.5f,c.z),new Vector3(w,h,d),Rot(ax),m,false);}
        static void Curbs(Transform p,Vector3 c,Vector3 r,Vector3 f,float w,float d,Material m,bool open){Box("curb",p,c+r*w*.5f+Vector3.up*.09f,new Vector3(.20f,.18f,d),Rot(f),m,false);Box("curb",p,c-r*w*.5f+Vector3.up*.09f,new Vector3(.20f,.18f,d),Rot(f),m,false);Box("curb",p,c+f*d*.5f+Vector3.up*.09f,new Vector3(w,.18f,.20f),Rot(r),m,false);if(!open)Box("curb",p,c-f*d*.5f+Vector3.up*.09f,new Vector3(w,.18f,.20f),Rot(r),m,false);}
        static void CurbsOpen(Transform p,Vector3 c,Vector3 r,Vector3 f,float w,float d,Material m){Curbs(p,c,r,f,w,d,m,true);}
        static void Patch(string n,Transform p,Vector3 c,Vector3 ax,float len,float wid,float y,Material m,int seg,int seed){ax=Flat(ax).normalized;Vector3 q=new Vector3(-ax.z,0,ax.x);System.Random rng=new System.Random(seed);GameObject g=new GameObject(n);g.transform.SetParent(p,false);Mesh mesh=new Mesh{name=n};Vector3[] v=new Vector3[seg+1];int[] tri=new int[seg*3];v[0]=new Vector3(c.x,y,c.z);for(int i=0;i<seg;i++){float a=Mathf.PI*2*i/seg;float wob=.82f+(float)rng.NextDouble()*.28f;Vector3 z=c+ax*(Mathf.Cos(a)*len*.5f*wob)+q*(Mathf.Sin(a)*wid*.5f*wob);v[i+1]=new Vector3(z.x,y,z.z);int j=(i+1)%seg;tri[i*3]=0;tri[i*3+1]=i+1;tri[i*3+2]=j+1;}mesh.vertices=v;mesh.triangles=tri;mesh.RecalculateNormals();g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=m;}
        static void AddLight(Transform p,Vector3 pos,float range,float intensity,Material bulb){GameObject g=new GameObject("V47 warm light");g.transform.SetParent(p,true);g.transform.position=pos;Light l=g.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(1f,.57f,.23f);l.range=range;l.intensity=intensity;l.shadows=LightShadows.Soft;GameObject o=GameObject.CreatePrimitive(PrimitiveType.Sphere);o.name="bulb";o.transform.SetParent(g.transform,false);o.transform.localScale=Vector3.one*.12f;o.GetComponent<Renderer>().sharedMaterial=bulb;UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());}
        static GameObject Place(GameObject a,Transform p,string n,Vector3 pos,float yaw,float targetH,Material m){if(!a)return null;GameObject g=(GameObject)PrefabUtility.InstantiatePrefab(a);if(!g)g=UnityEngine.Object.Instantiate(a);g.name=n;g.transform.SetParent(p,true);g.transform.position=pos;g.transform.rotation=Quaternion.Euler(0,yaw,0);g.transform.localScale=Vector3.one;Bounds b=BoundsOf(g);g.transform.localScale=Vector3.one*(targetH/Mathf.Max(.01f,b.size.y));b=BoundsOf(g);g.transform.position+=Vector3.up*(.04f-b.min.y);foreach(Renderer rr in g.GetComponentsInChildren<Renderer>(true)){rr.sharedMaterial=m;rr.shadowCastingMode=ShadowCastingMode.On;rr.receiveShadows=true;}foreach(Collider c in g.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);return g;}
        static GameObject Box(string n,Transform p,Vector3 pos,Vector3 sc,Quaternion rot,Material m,bool col){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,true);g.transform.position=pos;g.transform.localScale=sc;g.transform.rotation=rot;g.GetComponent<Renderer>().sharedMaterial=m;if(!col)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
        static Bounds BoundsOf(GameObject g){Renderer[] rs=g.GetComponentsInChildren<Renderer>(true);if(rs.Length==0)return new Bounds(g.transform.position,Vector3.one);Bounds b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;}
        static Material Mat(string n,Color c,float sm){string path=GeneratedDir+"/mat_"+n+".mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(path);Shader s=ResolveShader();if(!m){m=new Material(s){name="V47 "+n};AssetDatabase.CreateAsset(m,path);}m.shader=s;m.color=c;if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);if(m.HasProperty("_Color"))m.SetColor("_Color",c);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",sm);if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",sm);EditorUtility.SetDirty(m);return m;}
        static Material Textured(string n,Texture2D t,float sm){Material m=Mat(n,Color.white,sm);if(t){if(m.HasProperty("_BaseMap"))m.SetTexture("_BaseMap",t);if(m.HasProperty("_MainTex"))m.SetTexture("_MainTex",t);}return m;}
        static Material Emissive(string n,Color c,float mult){Material m=Mat(n,c,.30f);if(m.HasProperty("_EmissionColor")){m.SetColor("_EmissionColor",c*mult);m.EnableKeyword("_EMISSION");}return m;}
        static Shader ResolveShader(){Material p=AssetDatabase.LoadAssetAtPath<Material>(ProvenMaterial);if(p&&p.shader&&p.shader.isSupported)return p.shader;Shader s=GraphicsSettings.defaultRenderPipeline!=null?Shader.Find("Universal Render Pipeline/Lit"):Shader.Find("Standard");if(!s||!s.isSupported)s=Shader.Find("Sprites/Default");return s;}
        static Transform NewGroup(Transform p,string n){GameObject g=new GameObject(n);g.transform.SetParent(p,false);return g.transform;}
        static Transform Find(Transform root,string n)=>root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name.Equals(n,StringComparison.OrdinalIgnoreCase));
        static Quaternion Rot(Vector3 d)=>Quaternion.Euler(0,Yaw(d),0);static float Yaw(Vector3 d)=>Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;static Vector3 Flat(Vector3 v)=>new Vector3(v.x,0,v.z);
    }
}
#endif