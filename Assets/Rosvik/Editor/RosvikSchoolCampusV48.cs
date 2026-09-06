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
    public static class RosvikSchoolCampusV48 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_SCHOOL_CAMPUS_V48_VERSION";
        const int Version = 48;
        const string GroupName = "30 SCHOOL CAMPUS V48 - FINAL EXTERIOR REBUILD";
        const string GeneratedDir = "Assets/Rosvik/GeneratedV48";
        const string CityRoot = "Assets/Rosvik/ThirdParty/V40CozyBits";
        const string NatureRoot = "Assets/Rosvik/ThirdParty/V22Models/KenneyNature";
        const long SchoolWay = 163199458;

        static RosvikSchoolCampusV48() {
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        [MenuItem("Rosvik/Rebuild School Campus V48 - FINAL EXTERIOR")]
        public static void Force() {
            EditorPrefs.DeleteKey(Key);
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        static bool Busy() {
            return EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode ||
                   EditorApplication.isCompiling || EditorApplication.isUpdating;
        }

        static void TryApply() {
            if (EditorPrefs.GetInt(Key, 0) >= Version && File.Exists(ScenePath)) {
                EditorApplication.update -= TryApply;
                return;
            }
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

                List<RosvikOsmV15.Way> ways = RosvikOsmV15.LoadWays();
                RosvikOsmV15.Way school = ways == null ? null : ways.FirstOrDefault(w => w.Id == SchoolWay);
                if (school == null) throw new Exception("V48 could not find Rosviks skola footprint.");

                Vector3 schoolCenter = RosvikOsmV15.Centroid(school);
                schoolCenter.y = 0.03f;

                Transform old = Find(root.transform, GroupName);
                if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);

                Disable(root.transform, "28 SCHOOL CAMPUS V46 - COZY DENSITY PASS");
                Disable(root.transform, "29 SCHOOL CAMPUS V47 - ENTRANCE ANCHORED");

                Transform canopy = root.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(t => t.name == "large timber canopy");
                Vector3 entrance = canopy ? canopy.position : schoolCenter + Vector3.left * 12f;
                entrance.y = 0.03f;

                Vector3 forward = Flat(entrance - schoolCenter).normalized;
                if (forward.sqrMagnitude < 0.1f) forward = Vector3.left;
                Vector3 right = new Vector3(forward.z, 0f, -forward.x);

                ClearLegacyScatter(root.transform, schoolCenter, 82f);

                Transform group = NewGroup(root.transform, GroupName);

                Material lawn = Mat("campus_lawn", new Color(.145f,.215f,.105f), .03f);
                Material moss = Mat("moss_edge", new Color(.105f,.16f,.075f), .02f);
                Material asphalt = Mat("asphalt", new Color(.095f,.105f,.105f), .20f);
                Material wornAsphalt = Mat("worn_asphalt", new Color(.14f,.145f,.135f), .10f);
                Material paving = Mat("warm_paving", new Color(.39f,.34f,.27f), .08f);
                Material gravel = Mat("schoolyard_gravel", new Color(.31f,.285f,.235f), .03f);
                Material rubber = Mat("play_rubber", new Color(.31f,.24f,.16f), .04f);
                Material curb = Mat("curb", new Color(.50f,.49f,.44f), .12f);
                Material paint = Mat("paint", new Color(.76f,.74f,.64f), .05f);
                Material soil = Mat("soil", new Color(.155f,.095f,.055f), .02f);
                Material wood = Mat("wood", new Color(.31f,.165f,.075f), .05f);
                Material metal = Mat("metal", new Color(.055f,.06f,.058f), .30f);
                Material fence = Mat("fence", new Color(.115f,.125f,.115f), .18f);
                Material snow = Mat("first_snow", new Color(.86f,.88f,.865f), .18f);
                Material frost = Mat("frost", new Color(.67f,.71f,.70f), .16f);
                Material puddle = Mat("puddle", new Color(.038f,.072f,.082f), .75f);
                Material bulb = Emissive("bulb", new Color(1f,.55f,.19f), 3.0f);
                Material city = Textured("citybits", AssetDatabase.LoadAssetAtPath<Texture2D>(CityRoot+"/citybits_texture.png"), .18f);
                Material spruce = Mat("spruce", new Color(.035f,.105f,.052f), .02f);
                Material autumn = Mat("autumn", new Color(.34f,.255f,.075f), .02f);
                Material shrub = Mat("shrub", new Color(.085f,.15f,.06f), .02f);

                GameObject bench = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/bench.obj");
                GameObject lamp = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/streetlight.obj");
                GameObject sedan = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/car_sedan.obj");
                GameObject wagon = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/car_stationwagon.obj");
                GameObject hatch = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/car_hatchback.obj");
                GameObject dumpster = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/dumpster.obj");
                GameObject boxA = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/box_A.obj");
                GameObject boxB = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/box_B.obj");
                GameObject pine = AssetDatabase.LoadAssetAtPath<GameObject>(NatureRoot+"/tree_pineDefaultA.obj");
                GameObject tallPine = AssetDatabase.LoadAssetAtPath<GameObject>(NatureRoot+"/tree_pineTallA.obj");
                GameObject birch = AssetDatabase.LoadAssetAtPath<GameObject>(NatureRoot+"/tree_default_fall.obj");
                GameObject bush = AssetDatabase.LoadAssetAtPath<GameObject>(NatureRoot+"/plant_bushDetailed.obj");

                BuildCampusFoundation(group, entrance, forward, right, lawn, moss, paving, curb);
                BuildHeroEntrance(group, entrance, forward, right, paving, curb, soil, wood, metal, city, bench, lamp, bush, shrub, bulb);
                BuildParkingAndDropoff(group, entrance, forward, right, asphalt, wornAsphalt, curb, paint, lawn, soil, city,
                    sedan, wagon, hatch, lamp, birch, bush, autumn, shrub, bulb);
                BuildSchoolyard(group, entrance, forward, right, gravel, rubber, curb, paint, wood, metal, fence, city,
                    bench, lamp, pine, tallPine, birch, bush, spruce, autumn, shrub, bulb);
                BuildServiceCorner(group, entrance, forward, right, asphalt, curb, wood, metal, city, wagon, dumpster, boxA, boxB, lamp, bulb);
                BuildPathNetwork(group, entrance, forward, right, paving, curb);
                BuildTreeBelts(group, entrance, forward, right, pine, tallPine, birch, bush, spruce, autumn, shrub);
                BuildApocalypseDetails(group, entrance, forward, right, wood, metal, city, bench, boxA, boxB);
                BuildWeather(group, entrance, forward, right, snow, frost, puddle);

                TuneMood();

                EditorPrefs.SetInt(Key, Version);
                Selection.activeObject = group.gameObject;
                EditorUtility.SetDirty(root);
                AssetDatabase.SaveAssets();
                if (!Busy()) {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene, ScenePath);
                }
                Debug.Log("ROSVIK V48: FINAL EXTERIOR rebuild applied. Random hero scatter cleared; campus, paths, parking, schoolyard and tree belts rebuilt.");
            } catch (Exception ex) {
                Debug.LogError("ROSVIK V48 FAILED: " + ex);
            }
        }

        static void ClearLegacyScatter(Transform root, Vector3 center, float radius) {
            string[] tokens = { "tree", "pine", "spruce", "birch", "bush", "plant", "shrub" };
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in all) {
                if (!t || t == root || !t.gameObject.activeInHierarchy) continue;
                string n = t.name.ToLowerInvariant();
                if (n.Contains("v45") || n.Contains("architecture") || n.Contains("canopy")) continue;
                bool vegetation = false;
                for (int i=0; i<tokens.Length; i++) {
                    if (n.Contains(tokens[i])) { vegetation = true; break; }
                }
                if (!vegetation) continue;
                Vector3 d = Flat(t.position - center);
                if (d.magnitude <= radius) t.gameObject.SetActive(false);
            }
        }

        static void BuildCampusFoundation(Transform p, Vector3 e, Vector3 f, Vector3 r,
            Material lawn, Material moss, Material paving, Material curb) {
            Transform g = NewGroup(p, "V48 CAMPUS FOUNDATION");

            Vector3 campus = e - f*10f;
            campus.y = .012f;
            Flat("continuous campus lawn", g, campus, r, 104f, 82f, .035f, lawn);

            Flat("left moss edge", g, campus-r*46f-f*1f, f, 76f, 10f, .018f, moss);
            Flat("right moss edge", g, campus+r*46f-f*2f, f, 76f, 10f, .018f, moss);
            Flat("rear moss edge", g, campus-f*35f, r, 96f, 10f, .018f, moss);

            Vector3 apron = e + f*5.3f;
            Flat("entrance apron", g, apron, r, 28f, 12f, .070f, paving);
            Curbs(g, apron, r, f, 28.5f, 12.5f, curb, false);
        }

        static void BuildHeroEntrance(Transform p, Vector3 e, Vector3 f, Vector3 r,
            Material paving, Material curb, Material soil, Material wood, Material metal, Material city,
            GameObject bench, GameObject lamp, GameObject bush, Material shrub, Material bulb) {
            Transform g = NewGroup(p, "V48 HERO ENTRANCE");

            Vector3 court = e + f*6.0f;
            court.y = .05f;
            Flat("hero entrance paving", g, court, r, 26f, 11.5f, .085f, paving);

            for (int s=-1; s<=1; s+=2) {
                Vector3 planter = court + r*(s*8.0f) - f*3.2f;
                Planter(g, planter, r, 4.6f, 2.0f, curb, soil);
                if (bush) {
                    Place(bush,g,"entrance shrub",planter-r*(s*.95f),28+s*9,.70f,shrub);
                    Place(bush,g,"entrance shrub",planter+r*(s*.95f),73-s*11,.60f,shrub);
                    Place(bush,g,"entrance shrub",planter+f*.45f,41+s*13,.54f,shrub);
                }
                if (bench) Place(bench,g,"entrance bench",court+r*(s*9.4f)+f*.3f,Yaw(-r*s),.92f,city);
            }

            Vector3 racks = court - r*5.6f + f*2.0f;
            for (int i=0;i<12;i++) BikeRack(g, racks+r*(i*1.02f), f, metal);

            for (int i=-4;i<=4;i++) {
                Vector3 q = e + f*3.1f + r*(i*1.55f);
                Box("timber bollard", g, q+Vector3.up*.42f, new Vector3(.16f,.84f,.16f), Quaternion.identity, wood);
            }

            if (lamp) {
                Vector3[] lp = {
                    court-r*11.2f-f*4.2f, court+r*11.2f-f*4.2f,
                    court-r*11.2f+f*4.2f, court+r*11.2f+f*4.2f
                };
                foreach (Vector3 q in lp) {
                    Place(lamp,g,"entrance lamp",q,Yaw(f),4.65f,city);
                    AddLight(g,q+Vector3.up*3.42f,8.8f,1.1f,bulb);
                }
            }
        }

        static void BuildParkingAndDropoff(Transform p, Vector3 e, Vector3 f, Vector3 r,
            Material asphalt, Material wornAsphalt, Material curb, Material paint, Material lawn, Material soil, Material city,
            GameObject sedan, GameObject wagon, GameObject hatch, GameObject lamp, GameObject birch, GameObject bush,
            Material autumn, Material shrub, Material bulb) {
            Transform g = NewGroup(p, "V48 PARKING + DROPOFF");

            Vector3 parking = e + f*23f;
            parking.y = .04f;
            Flat("main parking", g, parking, r, 42f, 19f, .075f, asphalt);
            CurbsOpen(g, parking, r, f, 42.5f, 19.5f, curb);

            Vector3 drive = e + f*38f;
            Flat("dropoff approach", g, drive, r, 12f, 25f, .070f, wornAsphalt);
            CurbsOpen(g, drive, r, f, 12.4f, 25.4f, curb);

            for (int i=-6;i<=6;i++) {
                Flat("parking stripe", g, parking+r*(i*2.85f)+f*4.8f, f, .11f, 6.1f, .018f, paint);
            }

            Vector3 zebra = e + f*13.1f;
            for (int i=-5;i<=5;i++) {
                Flat("zebra crossing", g, zebra+r*(i*.58f), r, .34f, 5.0f, .020f, paint);
            }

            for (int s=-1;s<=1;s+=2) {
                Vector3 island = parking+r*(s*15.6f);
                Flat("parking island lawn",g,island,f,16.0f,3.4f,.16f,lawn);
                Curbs(g,island,f,r,16.3f,3.7f,curb,false);
                if (birch) Place(birch,g,"parking birch",island-f*3.6f,17+s*21,5.2f,autumn);
                if (bush) {
                    Place(bush,g,"parking island shrub",island+f*2.7f,51+s*7,.62f,shrub);
                    Place(bush,g,"parking island shrub",island,87-s*9,.55f,shrub);
                }
            }

            if (sedan) Place(sedan,g,"abandoned staff sedan",parking-r*11.4f+f*3.7f,Yaw(f)+3f,1.43f,city);
            if (hatch) Place(hatch,g,"abandoned hatchback",parking+r*8.5f-f*3.4f,Yaw(-f)-4f,1.38f,city);
            if (wagon) Place(wagon,g,"caretaker wagon",parking+r*2.4f+f*3.4f,Yaw(f)-2f,1.46f,city);

            if (lamp) {
                Vector3[] lp = { parking-r*19f, parking+r*19f, drive-r*5f+f*8f, drive+r*5f+f*8f };
                for (int i=0;i<lp.Length;i++) {
                    Place(lamp,g,"parking lamp",lp[i],Yaw(f),4.7f,city);
                    if (i<2) AddLight(g,lp[i]+Vector3.up*3.45f,8.5f,.90f,bulb);
                }
            }
        }

        static void BuildSchoolyard(Transform p, Vector3 e, Vector3 f, Vector3 r,
            Material gravel, Material rubber, Material curb, Material paint, Material wood, Material metal, Material fence, Material city,
            GameObject bench, GameObject lamp, GameObject pine, GameObject tallPine, GameObject birch, GameObject bush,
            Material spruce, Material autumn, Material shrub, Material bulb) {
            Transform g = NewGroup(p, "V48 FINISHED SCHOOLYARD");

            Vector3 yard = e-r*27f-f*1.5f;
            yard.y = .04f;
            Flat("schoolyard gravel", g, yard, r, 36f, 29f, .075f, gravel);
            Curbs(g, yard, r, f, 36.5f, 29.5f, curb, false);

            Vector3 play = yard-r*7f+f*3.0f;
            Flat("rubber play zone",g,play,r,13f,11f,.035f,rubber);
            Curbs(g,play,r,f,13.3f,11.3f,curb,false);
            Swing(g,play-r*3.3f,r,wood,metal);
            Swing(g,play+r*3.3f,r,wood,metal);

            Vector3 court = yard+r*8.2f+f*1.0f;
            Flat("painted court base",g,court,r,13f,11f,.030f,gravel);
            for (int s=-1;s<=1;s+=2) {
                Flat("court side",g,court+r*(s*6.1f),f,.10f,10.8f,.019f,paint);
                Flat("court end",g,court+f*(s*5.0f),r,12.2f,.10f,.019f,paint);
            }
            Flat("court center",g,court,r,12.2f,.10f,.019f,paint);

            Picnic(g,yard+r*7f-f*10f,r,wood,metal);
            Picnic(g,yard+r*2.8f-f*10f,r,wood,metal);
            Picnic(g,yard-r*1.4f-f*10f,r,wood,metal);

            if (bench) {
                Place(bench,g,"yard bench",yard-r*15f-f*10.5f,Yaw(r),.92f,city);
                Place(bench,g,"yard bench",yard+r*15f+f*10.5f,Yaw(-r),.92f,city);
                Place(bench,g,"yard bench",yard-r*14.5f+f*9.8f,Yaw(r),.92f,city);
            }

            FenceLine(g,yard-r*17.8f,f,28f,1.15f,fence);
            FenceLine(g,yard-f*14.3f,r,35f,1.15f,fence);

            if (lamp) {
                Vector3[] lp = { yard-r*15f+f*11f, yard+r*15f-f*11f };
                foreach (Vector3 q in lp) {
                    Place(lamp,g,"schoolyard lamp",q,Yaw(f),4.7f,city);
                    AddLight(g,q+Vector3.up*3.45f,8.2f,.85f,bulb);
                }
            }

            Vector3[] clusterCenters = {
                yard-r*16f+f*9f, yard-r*16f-f*5f, yard+r*14f-f*12f
            };
            for(int i=0;i<clusterCenters.Length;i++) {
                TreeCluster(g,clusterCenters[i],r,f, i==2 ? birch : pine, tallPine, bush,
                    i==2 ? autumn : spruce, shrub, 5+i, 6);
            }
        }

        static void BuildServiceCorner(Transform p, Vector3 e, Vector3 f, Vector3 r,
            Material asphalt, Material curb, Material wood, Material metal, Material city,
            GameObject wagon, GameObject dumpster, GameObject boxA, GameObject boxB, GameObject lamp, Material bulb) {
            Transform g = NewGroup(p, "V48 SERVICE CORNER");

            Vector3 c = e+r*27f-f*7f;
            c.y = .04f;
            Flat("service asphalt",g,c,r,22f,14f,.07f,asphalt);
            CurbsOpen(g,c,r,f,22.4f,14.4f,curb);

            Box("service shed",g,c+r*7.0f-f*3.5f+Vector3.up*1.25f,new Vector3(5.8f,2.5f,2.4f),Rot(r),wood);
            Box("shed door",g,c+r*7.0f-f*2.25f+Vector3.up*1.15f,new Vector3(2.1f,2.2f,.12f),Rot(r),metal);

            if (dumpster) Place(dumpster,g,"service dumpster",c-r*7f+f*3.8f,Yaw(r),1.34f,city);
            if (boxA) Place(boxA,g,"service crate",c-r*4.5f+f*3.4f,16f,.62f,city);
            if (boxB) Place(boxB,g,"service crate",c-r*3.3f+f*3.8f,74f,.58f,city);
            if (wagon) Place(wagon,g,"abandoned caretaker car",c+r*1.0f-f*2.8f,Yaw(-f)+5f,1.46f,city);

            if (lamp) {
                Vector3 q=c-r*9f-f*5f;
                Place(lamp,g,"service lamp",q,Yaw(f),4.7f,city);
                AddLight(g,q+Vector3.up*3.45f,8.0f,.8f,bulb);
            }
        }

        static void BuildPathNetwork(Transform p, Vector3 e, Vector3 f, Vector3 r, Material paving, Material curb) {
            Transform g = NewGroup(p, "V48 WALKWAY NETWORK");

            PathStrip(g,e+f*7f,e+f*15f,3.5f,paving,curb);
            PathStrip(g,e-r*1f+f*3f,e-r*22f+f*1f,2.8f,paving,curb);
            PathStrip(g,e+r*1f-f*1f,e+r*24f-f*6f,2.5f,paving,curb);
            PathStrip(g,e-r*17f+f*2f,e-r*26f+f*11f,2.1f,paving,curb);
        }

        static void BuildTreeBelts(Transform p, Vector3 e, Vector3 f, Vector3 r,
            GameObject pine, GameObject tallPine, GameObject birch, GameObject bush,
            Material spruce, Material autumn, Material shrub) {
            Transform g = NewGroup(p, "V48 TREE BELTS + CLUSTERS");

            Vector3[] belts = {
                e-r*42f-f*10f,
                e-r*39f+f*15f,
                e+r*42f-f*14f,
                e+r*39f+f*12f,
                e-f*36f-r*18f,
                e-f*37f+r*12f,
                e-f*34f+r*32f
            };

            for (int i=0;i<belts.Length;i++) {
                GameObject main = (i==1 || i==5) ? birch : pine;
                Material mat = (i==1 || i==5) ? autumn : spruce;
                TreeCluster(g,belts[i],r,f,main,tallPine,bush,mat,shrub,40+i*13,7);
            }

            if (bush) {
                for (int i=-7;i<=7;i++) {
                    Place(bush,g,"campus hedge",e-f*28f+r*(i*4.1f),i*29,.58f+(Mathf.Abs(i)%3)*.08f,shrub);
                }
            }
        }

        static void BuildApocalypseDetails(Transform p, Vector3 e, Vector3 f, Vector3 r,
            Material wood, Material metal, Material city, GameObject bench, GameObject boxA, GameObject boxB) {
            Transform g = NewGroup(p, "V48 QUIET APOCALYPSE DETAILS");

            Vector3 q = e+f*17f+r*13f;
            if (bench) {
                GameObject tipped = Place(bench,g,"tipped bench",q,Yaw(r)+9f,.92f,city);
                if (tipped) tipped.transform.rotation *= Quaternion.Euler(0f,0f,18f);
            }
            if (boxA) Place(boxA,g,"lost supply box",q+r*2.0f+f*.8f,31f,.58f,city);
            if (boxB) Place(boxB,g,"lost supply box",q+r*2.8f-f*.4f,77f,.52f,city);

            Vector3 sign = e-r*9f+f*14f;
            Box("fallen notice board",g,sign+Vector3.up*.28f,new Vector3(2.7f,.12f,1.3f),Quaternion.Euler(78f,Yaw(r),8f),wood);
            Box("notice board post",g,sign-r*.8f+Vector3.up*.45f,new Vector3(.12f,.9f,.12f),Quaternion.Euler(8f,0f,12f),metal);
        }

        static void BuildWeather(Transform p, Vector3 e, Vector3 f, Vector3 r,
            Material snow, Material frost, Material puddle) {
            Transform g = NewGroup(p, "V48 FIRST SNOW + WET GROUND");

            Patch(g,"snow bank",e-f*28f-r*22f,r,8.0f,2.2f,.045f,snow,4801);
            Patch(g,"snow bank",e-f*30f+r*18f,r,6.5f,1.8f,.045f,snow,4802);
            Patch(g,"frost by entrance",e+f*4f+r*10f,r,5.2f,1.2f,.035f,frost,4803);
            Patch(g,"frost by yard",e-r*29f-f*4f,f,7.0f,1.4f,.035f,frost,4804);
            Patch(g,"parking puddle",e+f*23f-r*6f,r,4.2f,1.0f,.030f,puddle,4805);
            Patch(g,"service puddle",e+r*27f-f*8f,r,3.4f,.8f,.030f,puddle,4806);
        }

        static void TreeCluster(Transform p, Vector3 center, Vector3 r, Vector3 f,
            GameObject mainTree, GameObject tallTree, GameObject bush,
            Material treeMat, Material shrubMat, int seed, int count) {
            System.Random rng = new System.Random(seed);
            for (int i=0;i<count;i++) {
                float x = (float)(rng.NextDouble()*8.0-4.0);
                float z = (float)(rng.NextDouble()*7.0-3.5);
                Vector3 q = center+r*x+f*z;
                GameObject tree = (i%4==0 && tallTree) ? tallTree : mainTree;
                if (tree) Place(tree,p,"tree cluster",q,rng.Next(0,360),4.5f+(float)rng.NextDouble()*1.4f,treeMat);
            }
            if (bush) {
                for(int i=0;i<count+2;i++) {
                    float x=(float)(rng.NextDouble()*9.0-4.5);
                    float z=(float)(rng.NextDouble()*8.0-4.0);
                    Place(bush,p,"cluster undergrowth",center+r*x+f*z,rng.Next(0,360),.48f+(float)rng.NextDouble()*.25f,shrubMat);
                }
            }
        }

        static void FenceLine(Transform p, Vector3 center, Vector3 axis, float length, float height, Material m) {
            axis = Flat(axis).normalized;
            if (axis.sqrMagnitude < .1f) axis = Vector3.right;
            int posts = Mathf.Max(2, Mathf.CeilToInt(length/2.4f));
            for(int i=0;i<=posts;i++) {
                float t = i/(float)posts;
                Vector3 q = center+axis*((t-.5f)*length);
                Box("fence post",p,q+Vector3.up*(height*.5f),new Vector3(.08f,height,.08f),Quaternion.identity,m);
            }
            Vector3 railCenter = center+Vector3.up*(height*.72f);
            Box("fence top rail",p,railCenter,new Vector3(length,.08f,.08f),Rot(axis),m);
            Box("fence middle rail",p,center+Vector3.up*(height*.38f),new Vector3(length,.07f,.07f),Rot(axis),m);
        }

        static void PathStrip(Transform p, Vector3 a, Vector3 b, float width, Material surface, Material curb) {
            Vector3 d = Flat(b-a);
            float len = d.magnitude;
            if (len < .1f) return;
            d /= len;
            Vector3 c = (a+b)*.5f;
            c.y = .055f;
            Vector3 widthAxis = new Vector3(d.z,0f,-d.x);
            Flat("walkway",p,c,widthAxis,width,len,.06f,surface);
            Vector3 edge = widthAxis*(width*.5f+.10f);
            Box("walk curb",p,c+edge+Vector3.up*.035f,new Vector3(.12f,.11f,len+.12f),Quaternion.LookRotation(d),curb);
            Box("walk curb",p,c-edge+Vector3.up*.035f,new Vector3(.12f,.11f,len+.12f),Quaternion.LookRotation(d),curb);
        }

        static void Planter(Transform p, Vector3 c, Vector3 axis, float width, float depth, Material curb, Material soil) {
            Flat("planter soil",p,c,axis,width,depth,.15f,soil);
            Curbs(p,c,axis,new Vector3(-axis.z,0,axis.x),width+.28f,depth+.28f,curb,false);
        }

        static void BikeRack(Transform p, Vector3 c, Vector3 axis, Material m) {
            axis = Flat(axis).normalized;
            Vector3 f = new Vector3(-axis.z,0,axis.x);
            Box("bike rack leg",p,c-f*.28f+Vector3.up*.40f,new Vector3(.07f,.80f,.07f),Quaternion.identity,m);
            Box("bike rack leg",p,c+f*.28f+Vector3.up*.40f,new Vector3(.07f,.80f,.07f),Quaternion.identity,m);
            Box("bike rack top",p,c+Vector3.up*.78f,new Vector3(.07f,.07f,.64f),Rot(f),m);
        }

        static void Swing(Transform p, Vector3 c, Vector3 axis, Material wood, Material metal) {
            axis = Flat(axis).normalized;
            Vector3 f = new Vector3(-axis.z,0,axis.x);
            float half = 2.0f;
            for(int s=-1;s<=1;s+=2) {
                Vector3 basePos = c+axis*(s*half);
                Box("swing post",p,basePos+f*.55f+Vector3.up*1.25f,new Vector3(.14f,2.5f,.14f),Quaternion.Euler(0,Yaw(axis),s*8f),wood);
                Box("swing post",p,basePos-f*.55f+Vector3.up*1.25f,new Vector3(.14f,2.5f,.14f),Quaternion.Euler(0,Yaw(axis),-s*8f),wood);
            }
            Box("swing beam",p,c+Vector3.up*2.45f,new Vector3(4.4f,.16f,.16f),Rot(axis),wood);
            for(int s=-1;s<=1;s+=2) {
                Vector3 seat = c+axis*(s*.72f)+Vector3.up*.72f;
                Box("swing seat",p,seat,new Vector3(.72f,.10f,.34f),Rot(axis),metal);
                Box("swing chain",p,seat+f*.20f+Vector3.up*.80f,new Vector3(.025f,1.55f,.025f),Quaternion.identity,metal);
                Box("swing chain",p,seat-f*.20f+Vector3.up*.80f,new Vector3(.025f,1.55f,.025f),Quaternion.identity,metal);
            }
        }

        static void Picnic(Transform p, Vector3 c, Vector3 axis, Material wood, Material metal) {
            axis = Flat(axis).normalized;
            Vector3 f = new Vector3(-axis.z,0,axis.x);
            Box("picnic table",p,c+Vector3.up*.72f,new Vector3(2.8f,.15f,1.0f),Rot(axis),wood);
            Box("picnic bench",p,c+f*.90f+Vector3.up*.42f,new Vector3(2.8f,.12f,.34f),Rot(axis),wood);
            Box("picnic bench",p,c-f*.90f+Vector3.up*.42f,new Vector3(2.8f,.12f,.34f),Rot(axis),wood);
            for(int s=-1;s<=1;s+=2) {
                Box("picnic leg",p,c+axis*(s*.92f)+Vector3.up*.34f,new Vector3(.12f,.68f,.12f),Quaternion.identity,metal);
            }
        }

        static void Curbs(Transform p, Vector3 c, Vector3 widthAxis, Vector3 depthAxis,
            float width, float depth, Material m, bool openFront) {
            widthAxis = Flat(widthAxis).normalized;
            depthAxis = Flat(depthAxis).normalized;
            float h=.11f, t=.18f;
            Box("curb side",p,c+widthAxis*(width*.5f)+Vector3.up*(h*.5f),new Vector3(t,h,depth),Quaternion.LookRotation(depthAxis),m);
            Box("curb side",p,c-widthAxis*(width*.5f)+Vector3.up*(h*.5f),new Vector3(t,h,depth),Quaternion.LookRotation(depthAxis),m);
            Box("curb back",p,c-depthAxis*(depth*.5f)+Vector3.up*(h*.5f),new Vector3(width,h,t),Rot(widthAxis),m);
            if(!openFront) Box("curb front",p,c+depthAxis*(depth*.5f)+Vector3.up*(h*.5f),new Vector3(width,h,t),Rot(widthAxis),m);
        }

        static void CurbsOpen(Transform p, Vector3 c, Vector3 widthAxis, Vector3 depthAxis,
            float width, float depth, Material m) {
            Curbs(p,c,widthAxis,depthAxis,width,depth,m,true);
        }

        static void Patch(Transform p, string name, Vector3 c, Vector3 axis, float width, float depth, float y, Material m, int seed) {
            System.Random rng = new System.Random(seed);
            int parts = 5;
            for(int i=0;i<parts;i++) {
                float ox=(float)(rng.NextDouble()-.5)*width*.55f;
                float oz=(float)(rng.NextDouble()-.5)*depth*.55f;
                float w=width*(.34f+(float)rng.NextDouble()*.30f);
                float d=depth*(.34f+(float)rng.NextDouble()*.30f);
                Vector3 f = new Vector3(-axis.z,0,axis.x);
                Flat(name,p,c+axis*ox+f*oz+Vector3.up*y,axis,w,d,.014f,m);
            }
        }

        static Transform NewGroup(Transform parent, string name) {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static Transform Find(Transform root, string name) {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            return all.FirstOrDefault(t => t.name == name);
        }

        static void Disable(Transform root, string name) {
            Transform t = Find(root,name);
            if (t) t.gameObject.SetActive(false);
        }

        static Vector3 Flat(Vector3 v) {
            v.y=0f;
            return v;
        }

        static float Yaw(Vector3 direction) {
            direction = Flat(direction).normalized;
            if (direction.sqrMagnitude < .01f) return 0f;
            return Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;
        }

        static Quaternion Rot(Vector3 widthAxis) {
            widthAxis = Flat(widthAxis).normalized;
            if(widthAxis.sqrMagnitude < .01f) widthAxis = Vector3.right;
            Vector3 depth = new Vector3(-widthAxis.z,0,widthAxis.x);
            return Quaternion.LookRotation(depth,Vector3.up);
        }

        static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 scale, Quaternion rotation, Material mat) {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name=name;
            go.transform.SetParent(parent,true);
            go.transform.position=pos;
            go.transform.rotation=rotation;
            go.transform.localScale=scale;
            Renderer rend=go.GetComponent<Renderer>();
            if(rend && mat) rend.sharedMaterial=mat;
            Collider col=go.GetComponent<Collider>();
            if(col) UnityEngine.Object.DestroyImmediate(col);
            return go;
        }

        static GameObject Flat(string name, Transform parent, Vector3 pos, Vector3 widthAxis,
            float width, float depth, float thickness, Material mat) {
            return Box(name,parent,pos,new Vector3(width,thickness,depth),Rot(widthAxis),mat);
        }

        static GameObject Place(GameObject prefab, Transform parent, string name, Vector3 pos, float yaw, float targetHeight, Material mat) {
            if(!prefab) return null;
            GameObject go = (GameObject)UnityEngine.Object.Instantiate(prefab);
            go.name=name;
            go.transform.SetParent(parent,true);
            go.transform.position=pos;
            go.transform.rotation=Quaternion.Euler(0f,yaw,0f);

            Renderer[] rs=go.GetComponentsInChildren<Renderer>(true);
            if(rs.Length>0) {
                Bounds b=rs[0].bounds;
                for(int i=1;i<rs.Length;i++) b.Encapsulate(rs[i].bounds);
                if(b.size.y>.001f) {
                    float factor=targetHeight/b.size.y;
                    go.transform.localScale=go.transform.localScale*factor;
                }
                if(mat) {
                    foreach(Renderer rr in rs) rr.sharedMaterial=mat;
                }
            }
            return go;
        }

        static void AddLight(Transform parent, Vector3 pos, float range, float intensity, Material bulbMat) {
            GameObject bulb=GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.name="warm bulb";
            bulb.transform.SetParent(parent,true);
            bulb.transform.position=pos;
            bulb.transform.localScale=Vector3.one*.13f;
            Renderer br=bulb.GetComponent<Renderer>();
            if(br && bulbMat) br.sharedMaterial=bulbMat;
            Collider bc=bulb.GetComponent<Collider>();
            if(bc) UnityEngine.Object.DestroyImmediate(bc);

            Light l=bulb.AddComponent<Light>();
            l.type=LightType.Point;
            l.range=range;
            l.intensity=intensity;
            l.color=new Color(1f,.63f,.34f);
            l.shadows=LightShadows.Soft;
        }

        static Material Mat(string name, Color color, float smoothness) {
            string path=GeneratedDir+"/"+name+".mat";
            Material existing=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(existing) return existing;
            Shader shader=Shader.Find("Universal Render Pipeline/Lit");
            if(!shader) shader=Shader.Find("Standard");
            Material m=new Material(shader);
            m.name=name;
            m.color=color;
            if(m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness",smoothness);
            if(m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness",smoothness);
            AssetDatabase.CreateAsset(m,path);
            return m;
        }

        static Material Textured(string name, Texture2D tex, float smoothness) {
            Material m=Mat(name,Color.white,smoothness);
            if(tex) {
                if(m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap",tex);
                else if(m.HasProperty("_MainTex")) m.SetTexture("_MainTex",tex);
                EditorUtility.SetDirty(m);
            }
            return m;
        }

        static Material Emissive(string name, Color color, float intensity) {
            Material m=Mat(name,color,.1f);
            Color e=color*intensity;
            if(m.HasProperty("_EmissionColor")) {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor",e);
                EditorUtility.SetDirty(m);
            }
            return m;
        }

        static void TuneMood() {
            RenderSettings.ambientMode=AmbientMode.Flat;
            RenderSettings.ambientLight=new Color(.37f,.40f,.36f);
            RenderSettings.fog=true;
            RenderSettings.fogColor=new Color(.47f,.52f,.51f);
            RenderSettings.fogMode=FogMode.Linear;
            RenderSettings.fogStartDistance=95f;
            RenderSettings.fogEndDistance=320f;

            Light sun=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
                .FirstOrDefault(l=>l.type==LightType.Directional);
            if(sun) {
                sun.color=new Color(1f,.84f,.67f);
                sun.intensity=1.05f;
                sun.shadows=LightShadows.Soft;
            }
        }
    }
}
#endif
