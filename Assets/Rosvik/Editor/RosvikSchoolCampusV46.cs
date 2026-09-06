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
    public static class RosvikSchoolCampusV46 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_SCHOOL_CAMPUS_V46_VERSION";
        const int Version = 46;
        const string GroupName = "28 SCHOOL CAMPUS V46 - COZY DENSITY PASS";
        const string GeneratedDir = "Assets/Rosvik/GeneratedV46";
        const string ProvenMaterial = "Assets/Rosvik/GeneratedV20/mat_ground.mat";
        const string CityRoot = "Assets/Rosvik/ThirdParty/V40CozyBits";
        const string NatureRoot = "Assets/Rosvik/ThirdParty/V22Models/KenneyNature";
        const long SchoolWay = 163199458;
        const long OldSchoolWay = 163199461;
        const long IceArenaWay = 163199454;

        static RosvikSchoolCampusV46() { EditorApplication.update -= TryApply; EditorApplication.update += TryApply; }

        [MenuItem("Rosvik/Rebuild School Campus V46 - COZY DENSITY")]
        public static void Force() { EditorPrefs.DeleteKey(Key); EditorApplication.update -= TryApply; EditorApplication.update += TryApply; }

        static bool Busy() => EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating;

        static void TryApply() {
            if (EditorPrefs.GetInt(Key,0) >= Version && File.Exists(ScenePath)) { EditorApplication.update -= TryApply; return; }
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

                List<RosvikOsmV15.Way> ways = RosvikOsmV15.LoadWays();
                if (ways == null || ways.Count == 0) throw new Exception("No OSM data");
                RosvikOsmV15.Way school = ways.FirstOrDefault(w => w.Id == SchoolWay);
                if (school == null) throw new Exception("School footprint missing");
                RosvikOsmV15.Way hall = FindSportHall(ways, school);

                Transform old = Find(root.transform, GroupName);
                if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
                Transform group = NewGroup(root.transform, GroupName);

                Material asphalt = Mat("asphalt_dense", new Color(.115f,.125f,.12f), .20f);
                Material paving = Mat("paving_warm", new Color(.39f,.35f,.29f), .08f);
                Material gravel = Mat("gravel", new Color(.34f,.31f,.25f), .04f);
                Material curb = Mat("curb", new Color(.48f,.47f,.42f), .10f);
                Material paint = Mat("paint", new Color(.72f,.70f,.60f), .05f);
                Material grass = Mat("grass", new Color(.17f,.25f,.125f), .02f);
                Material soil = Mat("soil", new Color(.18f,.12f,.075f), .02f);
                Material wood = Mat("wood", new Color(.33f,.18f,.085f), .05f);
                Material metal = Mat("metal", new Color(.065f,.07f,.068f), .28f);
                Material frost = Mat("frost", new Color(.70f,.74f,.73f), .15f);
                Material snow = Mat("snow", new Color(.82f,.84f,.82f), .20f);
                Material puddle = Mat("puddle", new Color(.05f,.09f,.10f), .72f);
                Material bulb = Emissive("bulb", new Color(1f,.55f,.19f), 2.8f);
                Material city = Textured("citybits", AssetDatabase.LoadAssetAtPath<Texture2D>(CityRoot+"/citybits_texture.png"), .18f);
                Material spruceMat = Mat("spruce", new Color(.045f,.125f,.065f), .02f);
                Material autumnMat = Mat("autumn", new Color(.34f,.27f,.09f), .02f);
                Material shrubMat = Mat("shrub", new Color(.11f,.18f,.075f), .02f);

                GameObject bench = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/bench.obj");
                GameObject lamp = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/streetlight.obj");
                GameObject dumpster = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/dumpster.obj");
                GameObject trashA = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/trash_A.obj");
                GameObject trashB = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/trash_B.obj");
                GameObject boxA = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/box_A.obj");
                GameObject boxB = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/box_B.obj");
                GameObject sedan = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/car_sedan.obj");
                GameObject wagon = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/car_stationwagon.obj");
                GameObject hatch = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/car_hatchback.obj");
                GameObject pine = AssetDatabase.LoadAssetAtPath<GameObject>(NatureRoot+"/tree_pineDefaultA.obj");
                GameObject tallPine = AssetDatabase.LoadAssetAtPath<GameObject>(NatureRoot+"/tree_pineTallA.obj");
                GameObject fallTree = AssetDatabase.LoadAssetAtPath<GameObject>(NatureRoot+"/tree_default_fall.obj");
                GameObject bush = AssetDatabase.LoadAssetAtPath<GameObject>(NatureRoot+"/plant_bushDetailed.obj");

                Vector3 schoolCenter = RosvikOsmV15.Centroid(school); schoolCenter.y = .04f;
                Vector3 hallCenter = hall != null ? RosvikOsmV15.Centroid(hall) : schoolCenter + Vector3.right*24f; hallCenter.y = .04f;
                Vector3 toHall = Flat(hallCenter-schoolCenter).normalized; if (toHall.sqrMagnitude < .1f) toHall = Vector3.right;
                Vector3 side = new Vector3(-toHall.z,0,toHall.x);
                Vector3 away = -toHall;

                BuildArrival(group, schoolCenter, away, side, asphalt, paving, curb, paint, grass, soil, metal, wood, city,
                    bench, lamp, sedan, hatch, fallTree, bush, autumnMat, shrubMat, bulb);
                BuildSchoolyard(group, schoolCenter, side, toHall, gravel, curb, paint, wood, metal, city, bench, lamp,
                    pine, tallPine, fallTree, bush, spruceMat, autumnMat, shrubMat, bulb);
                BuildService(group, hallCenter, toHall, side, asphalt, curb, wood, metal, city, dumpster, trashA, trashB,
                    boxA, boxB, wagon, lamp, bulb);
                BuildVegetationFrame(group, schoolCenter, hallCenter, side, toHall, pine, tallPine, fallTree, bush,
                    spruceMat, autumnMat, shrubMat);
                BuildWeather(group, schoolCenter, hallCenter, side, away, frost, snow, puddle);

                TuneMood();
                EditorPrefs.SetInt(Key,Version);
                Selection.activeObject = group.gameObject;
                EditorUtility.SetDirty(root);
                AssetDatabase.SaveAssets();
                if (!Busy()) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene,ScenePath); }
                Debug.Log("ROSVIK V46: cozy density pass applied around school/sporthall. V45 architecture retained.");
            } catch (Exception ex) { Debug.LogError("ROSVIK V46 FAILED: "+ex); }
        }

        static RosvikOsmV15.Way FindSportHall(List<RosvikOsmV15.Way> ways, RosvikOsmV15.Way school) {
            Vector3 sc = RosvikOsmV15.Centroid(school);
            return ways.Where(w => w.Closed && w.Id != SchoolWay && w.Id != OldSchoolWay && w.Id != IceArenaWay && !string.IsNullOrEmpty(w.Tag("building")) && w.Tag("building") != "no")
                .Select(w => new { Way=w, Bounds=RosvikOsmV15.Bounds(w), Dist=Flat(RosvikOsmV15.Centroid(w)-sc).magnitude })
                .Where(x => x.Dist < 80f && x.Bounds.Width*x.Bounds.Depth > 150f && Mathf.Max(x.Bounds.Width,x.Bounds.Depth) > 13f)
                .OrderBy(x => x.Dist).Select(x => x.Way).FirstOrDefault();
        }

        static void BuildArrival(Transform p, Vector3 sc, Vector3 away, Vector3 side,
            Material asphalt, Material paving, Material curb, Material paint, Material grass, Material soil, Material metal, Material wood, Material city,
            GameObject bench, GameObject lamp, GameObject sedan, GameObject hatch, GameObject fallTree, GameObject bush, Material autumn, Material shrub, Material bulb) {
            Transform g = NewGroup(p,"V46 ARRIVAL COURT + DROP OFF");
            Vector3 forecourt = sc + away*18f; forecourt.y=.04f;
            FlatBox("entrance paving",g,forecourt,side,24f,13f,.08f,paving); AddCurbs(g,forecourt,side,away,24.4f,13.4f,curb,false);
            Vector3 parking = forecourt + away*14.2f; parking.y=.04f;
            FlatBox("dropoff parking",g,parking,side,34f,15f,.07f,asphalt); AddCurbs(g,parking,side,away,34.4f,15.4f,curb,true);

            for (int i=-5;i<=5;i++) FlatBox("parking bay line",g,parking+side*(i*2.85f)+away*3.8f,away,.10f,5.4f,.018f,paint);
            for (int i=-4;i<=4;i++) FlatBox("crosswalk",g,forecourt+away*5.4f+side*(i*.58f),side,.34f,4.3f,.019f,paint);

            Vector3 island = parking + side*2.1f;
            FlatBox("green island",g,island,side,4.0f,11.0f,.16f,grass); AddCurbs(g,island,side,away,4.3f,11.3f,curb,false);
            if (fallTree) Place(fallTree,g,"arrival birch",island-away*2.8f,24f,5.4f,autumn);
            if (bush) { Place(bush,g,"arrival shrub",island+away*2.0f+side*.7f,15f,.70f,shrub); Place(bush,g,"arrival shrub",island+away*.3f-side*.8f,62f,.62f,shrub); }

            if (sedan) Place(sedan,g,"abandoned staff sedan",parking-side*10.5f+away*2.9f,Yaw(away)+2f,1.42f,city);
            if (hatch) Place(hatch,g,"small abandoned car",parking+side*11.6f-away*2.6f,Yaw(-away)-4f,1.38f,city);
            if (bench) {
                Place(bench,g,"entrance bench L",forecourt-side*7.8f-away*3.8f,Yaw(side),.90f,city);
                Place(bench,g,"entrance bench R",forecourt+side*7.8f-away*3.8f,Yaw(-side),.90f,city);
                Place(bench,g,"quiet bench",forecourt+side*8.7f+away*3.0f,Yaw(-side),.90f,city);
            }

            Vector3 racks = forecourt-side*8.8f+away*1.5f;
            for(int i=0;i<10;i++) BikeRack(g,racks+away*(i*.64f-2.8f),side,metal);
            Planter(g,forecourt-side*4.6f-away*4.3f,side,3.1f,1.4f,curb,soil,bush,shrub);
            Planter(g,forecourt+side*4.6f-away*4.3f,side,3.1f,1.4f,curb,soil,bush,shrub);

            if (lamp) {
                Vector3[] lp = { forecourt-side*10.0f-away*4.7f, forecourt+side*10.0f-away*4.7f, parking-side*15.2f, parking+side*15.2f, parking+away*6.1f };
                for(int i=0;i<lp.Length;i++) { Place(lamp,g,"campus lamp",lp[i],Yaw(away),4.7f,city); if(i<4) AddLight(g,lp[i]+Vector3.up*3.45f,8.5f,1.05f,bulb); }
            }
        }

        static void BuildSchoolyard(Transform p, Vector3 sc, Vector3 side, Vector3 toHall,
            Material gravel, Material curb, Material paint, Material wood, Material metal, Material city, GameObject bench, GameObject lamp,
            GameObject pine, GameObject tallPine, GameObject fallTree, GameObject bush, Material spruce, Material autumn, Material shrub, Material bulb) {
            Transform g = NewGroup(p,"V46 SCHOOLYARD - DENSE COZY");
            Vector3 yard = sc - side*24f - toHall*4f; yard.y=.04f;
            FlatBox("schoolyard gravel",g,yard,side,30f,22f,.07f,gravel); AddCurbs(g,yard,side,toHall,30.4f,22.4f,curb,false);

            Vector3 court = yard+side*4.5f+toHall*2.0f;
            FlatBox("court outer A",g,court+side*5f,side,.10f,9.0f,.018f,paint); FlatBox("court outer B",g,court-side*5f,side,.10f,9.0f,.018f,paint);
            FlatBox("court outer C",g,court+toHall*4.5f,toHall,.10f,10.0f,.018f,paint); FlatBox("court outer D",g,court-toHall*4.5f,toHall,.10f,10.0f,.018f,paint);
            FlatBox("court center",g,court,side,.10f,9.0f,.018f,paint);

            Picnic(g,yard+side*10.2f-toHall*6.4f,side,wood,metal);
            Picnic(g,yard+side*6.3f-toHall*6.5f,side,wood,metal);
            Swing(g,yard-side*9.0f+toHall*4.2f,side,wood,metal);
            if (bench) { Place(bench,g,"yard bench A",yard+side*12f+toHall*7f,Yaw(-side),.90f,city); Place(bench,g,"yard bench B",yard-side*12f-toHall*7f,Yaw(side),.90f,city); }
            if (lamp) { Vector3 lp=yard-side*13f+toHall*8f; Place(lamp,g,"yard lamp",lp,Yaw(toHall),4.7f,city); AddLight(g,lp+Vector3.up*3.45f,8f,.9f,bulb); }

            Vector3[] trees={yard-side*13f+toHall*8f,yard-side*13f-toHall*8f,yard+side*13f-toHall*8f,yard+side*12f+toHall*8f};
            GameObject[] src={tallPine,fallTree,pine,tallPine}; Material[] mats={spruce,autumn,spruce,spruce};
            for(int i=0;i<trees.Length;i++) if(src[i]) Place(src[i],g,"yard edge tree",trees[i],25+i*67,5.0f+(i%3)*.45f,mats[i]);
            if(bush) for(int i=-4;i<=4;i++) Place(bush,g,"yard shrub",yard-side*13.5f+toHall*(i*1.9f),i*31,.55f+(Mathf.Abs(i)%2)*.10f,shrub);
        }

        static void BuildService(Transform p, Vector3 hc, Vector3 toHall, Vector3 side,
            Material asphalt, Material curb, Material wood, Material metal, Material city, GameObject dumpster, GameObject trashA, GameObject trashB,
            GameObject boxA, GameObject boxB, GameObject wagon, GameObject lamp, Material bulb) {
            Transform g=NewGroup(p,"V46 SPORTHALL SERVICE YARD");
            Vector3 service=hc+toHall*18f;service.y=.04f;
            FlatBox("service asphalt",g,service,side,20f,13f,.07f,asphalt);AddCurbs(g,service,side,toHall,20.4f,13.4f,curb,true);
            if(dumpster)Place(dumpster,g,"dumpster",service-side*6.3f+toHall*3.8f,Yaw(side),1.35f,city);
            if(trashA)Place(trashA,g,"trash bin A",service-side*4.6f+toHall*4.0f,Yaw(toHall),.82f,city);
            if(trashB)Place(trashB,g,"trash bin B",service-side*3.7f+toHall*4.1f,Yaw(toHall),.82f,city);
            if(boxA)Place(boxA,g,"supply crate A",service-side*1.2f+toHall*4.0f,12f,.62f,city);
            if(boxB)Place(boxB,g,"supply crate B",service-side*.2f+toHall*3.6f,-8f,.50f,city);
            if(wagon)Place(wagon,g,"caretaker wagon",service-side*4.8f-toHall*3.1f,Yaw(side)+5f,1.46f,city);
            for(int y=0;y<2;y++)for(int i=-2;i<=2;i++)Box("pallet slat",g,service+side*(i*.42f)+Vector3.up*(.08f+y*.14f),new Vector3(.34f,.07f,1.5f),Rot(side),wood,false);
            if(lamp){Vector3 lp=service+side*8.2f-toHall*4.7f;Place(lamp,g,"service lamp",lp,Yaw(-toHall),4.7f,city);AddLight(g,lp+Vector3.up*3.45f,8.5f,1.0f,bulb);}
            for(int i=-4;i<=4;i++)Box("service fence post",g,service+side*(i*2.1f)+toHall*6.0f+Vector3.up*.72f,new Vector3(.08f,1.42f,.08f),Quaternion.identity,metal,false);
            for(int y=0;y<3;y++)Box("service fence rail",g,service+toHall*6.0f+Vector3.up*(.34f+y*.42f),new Vector3(17.2f,.05f,.05f),Rot(side),metal,false);
        }

        static void BuildVegetationFrame(Transform p, Vector3 sc, Vector3 hc, Vector3 side, Vector3 toHall,
            GameObject pine, GameObject tallPine, GameObject fallTree, GameObject bush, Material spruce, Material autumn, Material shrub) {
            Transform g=NewGroup(p,"V46 VEGETATION CLUSTERS");
            Vector3[] centers={sc-side*34f-toHall*8f,sc+side*31f-toHall*10f,hc+side*30f+toHall*4f,hc-side*28f+toHall*8f};
            System.Random rng=new System.Random(4601);
            for(int c=0;c<centers.Length;c++){
                for(int i=0;i<7;i++){
                    float ox=(float)(rng.NextDouble()*10.0-5.0), oz=(float)(rng.NextDouble()*10.0-5.0);
                    Vector3 pos=centers[c]+side*ox+toHall*oz;GameObject src=(i%5==1&&fallTree)?fallTree:((i%3==0&&tallPine)?tallPine:pine);Material mat=(i%5==1)?autumn:spruce;
                    if(src)Place(src,g,"cluster tree",pos,(float)rng.NextDouble()*360f,4.6f+(float)rng.NextDouble()*1.6f,mat);
                    if(bush&&i<5)Place(bush,g,"cluster shrub",pos+side*(float)(rng.NextDouble()*2.4-1.2),i*37,.50f+(float)rng.NextDouble()*.25f,shrub);
                }
            }
        }

        static void BuildWeather(Transform p, Vector3 sc, Vector3 hc, Vector3 side, Vector3 away, Material frost, Material snow, Material puddle){
            Transform g=NewGroup(p,"V46 FIRST SNOW + WET GROUND");
            Patch("frost entrance",g,sc+away*18f+side*8f,side,7f,1.8f,.025f,frost,24,4601);
            Patch("frost schoolyard",g,sc-side*24f-away*4f,side,8f,2.2f,.025f,frost,25,4602);
            Patch("first snow bank",g,sc+away*30f-side*15f,away,9f,2.1f,.028f,snow,26,4603);
            Patch("first snow bank",g,hc+away*15f+side*12f,side,7f,1.8f,.028f,snow,24,4604);
            Patch("parking puddle",g,sc+away*31f+side*5f,side,3.4f,.85f,.031f,puddle,20,4605);
            Patch("service puddle",g,hc+away*18f-side*2f,side,2.7f,.72f,.031f,puddle,19,4606);
        }

        static void TuneMood(){
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.29f,.285f,.255f);RenderSettings.fog=true;RenderSettings.fogColor=new Color(.40f,.405f,.395f);RenderSettings.fogDensity=.00125f;RenderSettings.reflectionIntensity=.52f;
            foreach(Light l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(x=>x.type==LightType.Directional)){l.intensity=1.05f;l.color=new Color(1f,.78f,.56f);l.shadows=LightShadows.Soft;l.shadowStrength=.78f;l.transform.rotation=Quaternion.Euler(38f,-48f,0f);}
        }

        static void BikeRack(Transform p,Vector3 c,Vector3 ax,Material m){Vector3 f=new Vector3(-ax.z,0,ax.x);Box("rack leg",p,c-f*.30f+Vector3.up*.42f,new Vector3(.07f,.84f,.07f),Quaternion.identity,m,false);Box("rack leg",p,c+f*.30f+Vector3.up*.42f,new Vector3(.07f,.84f,.07f),Quaternion.identity,m,false);Box("rack top",p,c+Vector3.up*.82f,new Vector3(.07f,.07f,.68f),Rot(f),m,false);}
        static void Picnic(Transform p,Vector3 c,Vector3 ax,Material wood,Material metal){Vector3 f=new Vector3(-ax.z,0,ax.x);Box("picnic top",p,c+Vector3.up*.72f,new Vector3(2.8f,.16f,1.1f),Rot(ax),wood,false);Box("picnic seat",p,c+f*.92f+Vector3.up*.42f,new Vector3(2.8f,.13f,.38f),Rot(ax),wood,false);Box("picnic seat",p,c-f*.92f+Vector3.up*.42f,new Vector3(2.8f,.13f,.38f),Rot(ax),wood,false);for(int s=-1;s<=1;s+=2)for(int q=-1;q<=1;q+=2)Box("picnic leg",p,c+ax*(s*.85f)+f*(q*.32f)+Vector3.up*.33f,new Vector3(.11f,.66f,.11f),Quaternion.identity,metal,false);}
        static void Swing(Transform p,Vector3 c,Vector3 ax,Material wood,Material metal){Vector3 f=new Vector3(-ax.z,0,ax.x);for(int s=-1;s<=1;s+=2){Vector3 e=c+ax*s*2.4f;Bar(p,"swing frame",e-f*.62f+Vector3.up*1.45f,(Vector3.up*.95f+f*.42f).normalized,.15f,3.1f,wood);Bar(p,"swing frame",e+f*.62f+Vector3.up*1.45f,(Vector3.up*.95f-f*.42f).normalized,.15f,3.1f,wood);}Bar(p,"swing beam",c+Vector3.up*2.72f,ax,.17f,5.7f,wood);for(int s=-1;s<=1;s+=2){Vector3 seat=c+ax*s*.92f+Vector3.up*.60f;Bar(p,"rope",seat-ax*.26f+Vector3.up*.96f,Vector3.up,.025f,1.86f,metal);Bar(p,"rope",seat+ax*.26f+Vector3.up*.96f,Vector3.up,.025f,1.86f,metal);Box("swing seat",p,seat,new Vector3(.68f,.09f,.30f),Rot(ax),wood,false);}}
        static void Planter(Transform p,Vector3 c,Vector3 ax,float w,float d,Material rim,Material soil,GameObject bush,Material shrub){Vector3 f=new Vector3(-ax.z,0,ax.x);FlatBox("planter soil",p,c,ax,w,d,.18f,soil);AddCurbs(p,c,ax,f,w+.22f,d+.22f,rim,false);if(bush)Place(bush,p,"planter shrub",c+Vector3.up*.16f,45f,.68f,shrub);}
        static void AddCurbs(Transform p,Vector3 c,Vector3 r,Vector3 f,float w,float d,Material m,bool open){Box("curb",p,c+r*w*.5f+Vector3.up*.09f,new Vector3(.20f,.18f,d),Rot(f),m,false);Box("curb",p,c-r*w*.5f+Vector3.up*.09f,new Vector3(.20f,.18f,d),Rot(f),m,false);Box("curb",p,c+f*d*.5f+Vector3.up*.09f,new Vector3(w,.18f,.20f),Rot(r),m,false);if(!open)Box("curb",p,c-f*d*.5f+Vector3.up*.09f,new Vector3(w,.18f,.20f),Rot(r),m,false);}
        static void Bar(Transform p,string n,Vector3 c,Vector3 d,float t,float len,Material m){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,true);g.transform.position=c;g.transform.rotation=Quaternion.FromToRotation(Vector3.up,d.normalized);g.transform.localScale=new Vector3(t,len,t);g.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());}
        static void FlatBox(string n,Transform p,Vector3 c,Vector3 ax,float w,float d,float h,Material m){Box(n,p,new Vector3(c.x,h*.5f,c.z),new Vector3(w,h,d),Rot(ax),m,false);}
        static GameObject Box(string n,Transform p,Vector3 pos,Vector3 scale,Quaternion rot,Material m,bool col){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,true);g.transform.position=pos;g.transform.rotation=rot;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=m;if(!col)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
        static GameObject Place(GameObject asset,Transform p,string n,Vector3 pos,float yaw,float targetHeight,Material mat){if(!asset)return null;GameObject g=(GameObject)PrefabUtility.InstantiatePrefab(asset);if(!g)g=UnityEngine.Object.Instantiate(asset);g.name=n;g.transform.SetParent(p,true);g.transform.position=pos;g.transform.rotation=Quaternion.Euler(0,yaw,0);g.transform.localScale=Vector3.one;Bounds b=BoundsOf(g);float s=targetHeight/Mathf.Max(.01f,b.size.y);g.transform.localScale=Vector3.one*s;Ground(g,.04f);foreach(Renderer r in g.GetComponentsInChildren<Renderer>(true)){r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;}foreach(Collider c in g.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);return g;}
        static Bounds BoundsOf(GameObject g){Renderer[] rs=g.GetComponentsInChildren<Renderer>(true);if(rs.Length==0)return new Bounds(g.transform.position,Vector3.one);Bounds b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;}
        static void Ground(GameObject g,float y){Bounds b=BoundsOf(g);g.transform.position+=Vector3.up*(y-b.min.y);}
        static void AddLight(Transform p,Vector3 pos,float range,float intensity,Material bulb){GameObject glow=new GameObject("V46 warm light");glow.transform.SetParent(p,true);glow.transform.position=pos;Light l=glow.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(1f,.58f,.24f);l.range=range;l.intensity=intensity;l.shadows=LightShadows.Soft;GameObject orb=GameObject.CreatePrimitive(PrimitiveType.Sphere);orb.transform.SetParent(glow.transform,false);orb.transform.localScale=Vector3.one*.11f;orb.GetComponent<Renderer>().sharedMaterial=bulb;UnityEngine.Object.DestroyImmediate(orb.GetComponent<Collider>());}
        static void Patch(string n,Transform p,Vector3 c,Vector3 ax,float length,float width,float y,Material m,int seg,int seed){ax=Flat(ax).normalized;if(ax.sqrMagnitude<.1f)ax=Vector3.right;Vector3 perp=new Vector3(-ax.z,0,ax.x);System.Random rng=new System.Random(seed);GameObject g=new GameObject(n);g.transform.SetParent(p,false);Mesh mesh=new Mesh{name=n};Vector3[] v=new Vector3[seg+1];int[] tri=new int[seg*3];v[0]=new Vector3(c.x,y,c.z);for(int i=0;i<seg;i++){float a=Mathf.PI*2f*i/seg;float wob=.82f+(float)rng.NextDouble()*.28f;Vector3 q=c+ax*(Mathf.Cos(a)*length*.5f*wob)+perp*(Mathf.Sin(a)*width*.5f*wob);v[i+1]=new Vector3(q.x,y,q.z);int j=(i+1)%seg;tri[i*3]=0;tri[i*3+1]=i+1;tri[i*3+2]=j+1;}mesh.vertices=v;mesh.triangles=tri;mesh.RecalculateNormals();mesh.RecalculateBounds();g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=m;}
        static Material Mat(string n,Color c,float smooth){string path=GeneratedDir+"/mat_"+n+".mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(path);Shader s=ResolveShader();if(!m){m=new Material(s){name="V46 "+n};AssetDatabase.CreateAsset(m,path);}m.shader=s;m.color=c;if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);if(m.HasProperty("_Color"))m.SetColor("_Color",c);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",smooth);EditorUtility.SetDirty(m);return m;}
        static Material Textured(string n,Texture2D tex,float smooth){Material m=Mat(n,Color.white,smooth);if(tex){if(m.HasProperty("_BaseMap"))m.SetTexture("_BaseMap",tex);if(m.HasProperty("_MainTex"))m.SetTexture("_MainTex",tex);}EditorUtility.SetDirty(m);return m;}
        static Material Emissive(string n,Color c,float mult){Material m=Mat(n,c,.30f);if(m.HasProperty("_EmissionColor")){m.SetColor("_EmissionColor",c*mult);m.EnableKeyword("_EMISSION");}EditorUtility.SetDirty(m);return m;}
        static Shader ResolveShader(){Material pm=AssetDatabase.LoadAssetAtPath<Material>(ProvenMaterial);if(pm&&pm.shader&&pm.shader.isSupported)return pm.shader;Shader s=GraphicsSettings.defaultRenderPipeline!=null?Shader.Find("Universal Render Pipeline/Lit"):Shader.Find("Standard");if(!s||!s.isSupported)s=Shader.Find("Sprites/Default");return s;}
        static Quaternion Rot(Vector3 d)=>Quaternion.Euler(0,Yaw(d),0);
        static float Yaw(Vector3 d)=>Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;
        static Vector3 Flat(Vector3 v)=>new Vector3(v.x,0,v.z);
        static Transform NewGroup(Transform p,string n){GameObject g=new GameObject(n);g.transform.SetParent(p,false);return g.transform;}
        static Transform Find(Transform root,string n)=>root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name.Equals(n,StringComparison.OrdinalIgnoreCase));
    }
}
#endif