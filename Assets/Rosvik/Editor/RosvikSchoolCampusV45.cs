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
    public static class RosvikSchoolCampusV45 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_SCHOOL_CAMPUS_V45_VERSION";
        const int Version = 45;
        const string GroupName = "27 SCHOOL CAMPUS V45 - VISIBLE REBUILD";
        const string GeneratedDir = "Assets/Rosvik/GeneratedV45";
        const string ProvenMaterial = "Assets/Rosvik/GeneratedV20/mat_ground.mat";
        const string CityRoot = "Assets/Rosvik/ThirdParty/V40CozyBits";
        const string NatureRoot = "Assets/Rosvik/ThirdParty/V22Models/KenneyNature";
        const long SchoolWay = 163199458;
        const long OldSchoolWay = 163199461;
        const long IceArenaWay = 163199454;

        static RosvikSchoolCampusV45() {
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        [MenuItem("Rosvik/Rebuild School Campus V45 - VISIBLE REBUILD")]
        public static void Force() {
            EditorPrefs.DeleteKey(Key);
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        static bool Busy() {
            return EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating;
        }

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
            GameObject root = GameObject.Find("ROSVIK_HERO_COMPOSITION_V42");
            if (!root) root = GameObject.Find("ROSVIK_HERO_AREA_ASSETS_V41");
            if (!root) root = GameObject.Find("ROSVIK_ASSET_COZY_APOCALYPSE_V40");
            if (!root) root = GameObject.Find("ROSVIK_CLEAN_ROAD_NETWORK_V39");
            if (!root) root = GameObject.Find("ROSVIK_VILLAGE_FABRIC_V38");
            return root;
        }

        static void Apply(UScene scene, GameObject root) {
            try {
                Directory.CreateDirectory(GeneratedDir);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                List<RosvikOsmV15.Way> ways = RosvikOsmV15.LoadWays();
                if (ways == null || ways.Count == 0) throw new Exception("No Rosvik OSM data available");

                RosvikOsmV15.Way school = ways.FirstOrDefault(x => x.Id == SchoolWay);
                if (school == null) throw new Exception("Rosviks skola footprint was not found");
                RosvikOsmV15.Way sportHall = FindSportHall(ways, school);

                Transform existing = Find(root.transform, GroupName);
                if (existing) UnityEngine.Object.DestroyImmediate(existing.gameObject);
                Transform group = NewGroup(root.transform, GroupName);

                Disable(root.transform, "25 SCHOOL CAMPUS V43 - BIG PASS");
                Disable(root.transform, "26 SCHOOL CAMPUS V44 - ARCHITECTURE REBUILD");
                Disable(root.transform, "24 HERO COMPOSITION V42");
                Disable(root.transform, "23 HERO AREA ASSETS V41");

                Material schoolWall = Mat("school_wall", new Color(.68f, .61f, .48f), .08f);
                Material brick = Mat("brick", new Color(.40f, .22f, .13f), .07f);
                Material schoolRoof = Mat("school_roof", new Color(.25f, .12f, .085f), .13f);
                Material hallWall = Mat("hall_wall", new Color(.13f, .155f, .15f), .12f);
                Material hallBase = Mat("hall_base", new Color(.30f, .22f, .16f), .08f);
                Material hallRoof = Mat("hall_roof", new Color(.075f, .09f, .09f), .16f);
                Material trim = Mat("trim", new Color(.79f, .76f, .66f), .16f);
                Material glass = Mat("glass", new Color(.045f, .105f, .13f), .52f);
                Material warm = Emissive("warm_window", new Color(.96f, .54f, .20f), 1.75f);
                Material asphalt = Mat("asphalt", new Color(.13f, .14f, .135f), .22f);
                Material paving = Mat("paving", new Color(.38f, .34f, .28f), .09f);
                Material grass = Mat("grass", new Color(.19f, .28f, .14f), .03f);
                Material wood = Mat("wood", new Color(.34f, .18f, .085f), .05f);
                Material metal = Mat("metal", new Color(.065f, .072f, .07f), .28f);
                Material paint = Mat("paint", new Color(.75f, .73f, .64f), .05f);
                Material frost = Mat("frost", new Color(.68f, .72f, .71f), .16f);
                Material puddle = Mat("puddle", new Color(.055f, .095f, .105f), .70f);
                Material bulb = Emissive("bulb", new Color(1f, .55f, .19f), 2.6f);
                Material city = Textured("citybits", AssetDatabase.LoadAssetAtPath<Texture2D>(CityRoot + "/citybits_texture.png"), .18f);
                Material spruceMat = Mat("spruce", new Color(.055f, .14f, .075f), .03f);
                Material autumnMat = Mat("autumn", new Color(.36f, .30f, .10f), .03f);
                Material shrubMat = Mat("shrub", new Color(.14f, .22f, .09f), .03f);

                BuildSchool(group, school, sportHall, schoolWall, brick, schoolRoof, trim, glass, warm, wood, bulb);
                if (sportHall != null) {
                    BuildHall(group, sportHall, school, hallWall, hallBase, hallRoof, trim, glass, warm, bulb);
                    BuildConnector(group, school, sportHall, hallWall, trim, glass, warm, bulb);
                }
                BuildCampus(group, school, sportHall, asphalt, paving, grass, wood, metal, paint, frost, puddle,
                    city, spruceMat, autumnMat, shrubMat, bulb);

                TuneMood();
                EditorPrefs.SetInt(Key, Version);
                Selection.activeObject = group.gameObject;
                EditorUtility.SetDirty(root);
                AssetDatabase.SaveAssets();
                if (!Busy()) {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene, ScenePath);
                }
                Debug.Log("ROSVIK V45: visible school-campus rebuild applied. Sporthall=" + (sportHall == null ? "fallback missing" : sportHall.Id.ToString()));
            } catch (Exception ex) {
                Debug.LogError("ROSVIK V45 FAILED: " + ex);
            }
        }

        static RosvikOsmV15.Way FindSportHall(List<RosvikOsmV15.Way> ways, RosvikOsmV15.Way school) {
            Vector3 schoolCenter = RosvikOsmV15.Centroid(school);
            List<Vector3> schoolPoly = Points(school);
            return ways
                .Where(x => x.Closed && x.Id != SchoolWay && x.Id != OldSchoolWay && x.Id != IceArenaWay && !string.IsNullOrEmpty(x.Tag("building")) && x.Tag("building") != "no")
                .Select(x => new Candidate { Way=x, Bounds=RosvikOsmV15.Bounds(x), Distance=Flat(RosvikOsmV15.Centroid(x)-schoolCenter).magnitude, Gap=BoundaryGap(schoolPoly, Points(x)) })
                .Where(x => x.Distance < 80f && x.Gap < 30f && x.Bounds.Width * x.Bounds.Depth > 150f && Mathf.Max(x.Bounds.Width, x.Bounds.Depth) > 13f)
                .OrderBy(x => x.Gap).ThenBy(x => x.Distance)
                .Select(x => x.Way).FirstOrDefault();
        }

        class Candidate {
            public RosvikOsmV15.Way Way;
            public RosvikOsmV15.OBounds Bounds;
            public float Distance;
            public float Gap;
        }

        static void BuildSchool(Transform parent, RosvikOsmV15.Way school, RosvikOsmV15.Way hall,
            Material wall, Material baseMat, Material roof, Material trim, Material glass, Material warm, Material wood, Material bulb) {
            Transform group = NewGroup(parent, "V45 ROSVIKS SKOLA - ARCHITECTURE");
            List<Vector3> pts = Points(school);
            float signedArea = SignedArea(pts);
            int windowIndex = 0;

            for (int i=0; i<pts.Count; i++) {
                Vector3 pointA = pts[i];
                Vector3 pointB = pts[(i+1)%pts.Count];
                Vector3 edge = Flat(pointB-pointA);
                float length = edge.magnitude;
                if (length < 1.3f) continue;
                edge /= length;
                Vector3 left = new Vector3(-edge.z,0f,edge.x);
                Vector3 outward = signedArea > 0f ? -left : left;
                Vector3 mid = (pointA+pointB)*.5f;

                Panel("school wall", group, mid+outward*.12f+Vector3.up*1.78f, outward, new Vector3(length+.12f,3.45f,.23f), wall);
                Panel("brick plinth", group, mid+outward*.27f+Vector3.up*.38f, outward, new Vector3(length+.16f,.68f,.12f), baseMat);
                Panel("eave trim", group, mid+outward*.26f+Vector3.up*3.40f, outward, new Vector3(length+.18f,.18f,.14f), trim);

                int count = Mathf.Clamp(Mathf.FloorToInt(length/3.0f),1,10);
                for (int k=0; k<count; k++) {
                    Vector3 pos = Vector3.Lerp(pointA,pointB,(k+.5f)/count);
                    Material pane = (windowIndex++ % 6 == 2) ? warm : glass;
                    Panel("window frame",group,pos+outward*.28f+Vector3.up*1.84f,outward,new Vector3(1.58f,1.44f,.11f),trim);
                    Panel("window",group,pos+outward*.35f+Vector3.up*1.84f,outward,new Vector3(1.32f,1.17f,.06f),pane);
                }
            }

            RosvikOsmV15.OBounds bounds = RosvikOsmV15.Bounds(school);
            Vector3 axis = Flat(bounds.AxisX).normalized;
            Vector3 perpendicular = new Vector3(-axis.z,0f,axis.x);
            float width = bounds.Width;
            float depth = bounds.Depth;
            if (depth > width) {
                float swap = width; width = depth; depth = swap;
                Vector3 axisSwap = axis; axis = perpendicular; perpendicular = axisSwap;
            }
            float yaw = Yaw(axis);
            Box("school roof A",group,bounds.Center+perpendicular*(depth*.245f)+Vector3.up*3.82f,new Vector3(width+1.2f,.22f,depth*.56f+1.0f),Quaternion.Euler(10f,yaw,0f),roof,false);
            Box("school roof B",group,bounds.Center-perpendicular*(depth*.245f)+Vector3.up*3.82f,new Vector3(width+1.2f,.22f,depth*.56f+1.0f),Quaternion.Euler(-10f,yaw,0f),roof,false);

            Vector3 schoolCenter = RosvikOsmV15.Centroid(school);
            Vector3 away = hall != null ? Flat(schoolCenter-RosvikOsmV15.Centroid(hall)).normalized : perpendicular;
            if (away.sqrMagnitude < .1f) away = perpendicular;
            Vector3 entrance = ExtremePoint(pts, schoolCenter, away);
            Vector3 entranceRight = new Vector3(away.z,0f,-away.x);
            Panel("entrance surround",group,entrance+away*.42f+Vector3.up*1.45f,away,new Vector3(6.0f,2.82f,.28f),baseMat);
            Panel("entrance glass",group,entrance+away*.62f+Vector3.up*1.31f,away,new Vector3(4.0f,2.34f,.08f),warm);
            Box("large timber canopy",group,entrance+away*1.85f+Vector3.up*2.98f,new Vector3(6.7f,.24f,3.25f),Rot(entranceRight),wood,false);
            AddLight(group,entrance+away*2.45f+Vector3.up*2.58f,11f,1.7f,bulb);
        }

        static void BuildHall(Transform parent, RosvikOsmV15.Way hall, RosvikOsmV15.Way school,
            Material wall, Material baseMat, Material roof, Material trim, Material glass, Material warm, Material bulb) {
            Transform group = NewGroup(parent,"V45 ROSVIK SPORTHALL - ARCHITECTURE");
            List<Vector3> pts = Points(hall);
            float signedArea = SignedArea(pts);
            int index = 0;
            for (int i=0; i<pts.Count; i++) {
                Vector3 pointA = pts[i];
                Vector3 pointB = pts[(i+1)%pts.Count];
                Vector3 edge = Flat(pointB-pointA);
                float length = edge.magnitude;
                if (length < 1.3f) continue;
                edge /= length;
                Vector3 left = new Vector3(-edge.z,0f,edge.x);
                Vector3 outward = signedArea > 0f ? -left : left;
                Vector3 mid = (pointA+pointB)*.5f;
                Panel("hall wall",group,mid+outward*.14f+Vector3.up*3.70f,outward,new Vector3(length+.12f,7.25f,.24f),wall);
                Panel("hall base",group,mid+outward*.29f+Vector3.up*.72f,outward,new Vector3(length+.16f,1.22f,.13f),baseMat);
                Panel("hall fascia",group,mid+outward*.28f+Vector3.up*7.18f,outward,new Vector3(length+.18f,.20f,.15f),trim);
                int count = Mathf.Clamp(Mathf.FloorToInt(length/5.0f),1,8);
                for (int k=0; k<count; k++) {
                    Vector3 pos = Vector3.Lerp(pointA,pointB,(k+.5f)/count);
                    Material pane = ((index+k)%8==2) ? warm : glass;
                    Panel("hall window frame",group,pos+outward*.31f+Vector3.up*4.78f,outward,new Vector3(1.9f,1.08f,.10f),trim);
                    Panel("hall window",group,pos+outward*.38f+Vector3.up*4.78f,outward,new Vector3(1.62f,.80f,.055f),pane);
                }
                index++;
            }
            RosvikOsmV15.OBounds hallBounds = RosvikOsmV15.Bounds(hall);
            Vector3 hallAxis = Flat(hallBounds.AxisX).normalized;
            Box("sporthall roof",group,hallBounds.Center+Vector3.up*7.38f,new Vector3(hallBounds.Width+1.1f,.30f,hallBounds.Depth+1.1f),Rot(hallAxis),roof,false);
            Vector3 towardSchool = Flat(RosvikOsmV15.Centroid(school)-RosvikOsmV15.Centroid(hall)).normalized;
            if (towardSchool.sqrMagnitude > .1f) {
                Vector3 door = ExtremePoint(pts,RosvikOsmV15.Centroid(hall),towardSchool);
                Panel("sporthall doors",group,door+towardSchool*.34f+Vector3.up*1.35f,towardSchool,new Vector3(3.0f,2.55f,.10f),warm);
                AddLight(group,door+towardSchool*.75f+Vector3.up*2.75f,9f,1.15f,bulb);
            }
        }

        static void BuildConnector(Transform parent, RosvikOsmV15.Way school, RosvikOsmV15.Way hall,
            Material wall, Material trim, Material glass, Material warm, Material bulb) {
            Vector3 schoolPoint;
            Vector3 hallPoint;
            float gap = BoundaryPair(Points(school),Points(hall),out schoolPoint,out hallPoint);
            if (gap < .35f || gap > 30f) return;
            Vector3 direction = Flat(hallPoint-schoolPoint);
            float length = direction.magnitude;
            if (length < .35f) return;
            direction /= length;
            Vector3 side = new Vector3(-direction.z,0f,direction.x);
            Vector3 center = (schoolPoint+hallPoint)*.5f;
            Transform group = NewGroup(parent,"V45 SCHOOL-SPORTHALL CORRIDOR");
            Box("corridor floor",group,center+Vector3.up*.10f,new Vector3(3.6f,.20f,length+.9f),Rot(direction),trim,false);
            Box("corridor roof",group,center+Vector3.up*3.16f,new Vector3(3.8f,.25f,length+1.1f),Rot(direction),wall,false);
            for (int s=-1; s<=1; s+=2) {
                Vector3 sideCenter = center+side*(s*1.74f)+Vector3.up*1.60f;
                Box("corridor glass",group,sideCenter,new Vector3(.08f,2.62f,length+.4f),Rot(direction),glass,false);
            }
            Panel("warm corridor end",group,schoolPoint+direction*.16f+Vector3.up*1.60f,-direction,new Vector3(3.2f,2.56f,.08f),warm);
            AddLight(group,center+Vector3.up*2.47f,8.5f,1.05f,bulb);
        }

        static void BuildCampus(Transform parent, RosvikOsmV15.Way school, RosvikOsmV15.Way hall,
            Material asphalt, Material paving, Material grass, Material wood, Material metal, Material paint, Material frost, Material puddle,
            Material city, Material spruceMat, Material autumnMat, Material shrubMat, Material bulb) {
            Transform group = NewGroup(parent,"V45 COZY SCHOOL CAMPUS");
            Vector3 schoolCenter = RosvikOsmV15.Centroid(school);
            Vector3 towardHall = hall != null ? Flat(RosvikOsmV15.Centroid(hall)-schoolCenter).normalized : Vector3.right;
            if (towardHall.sqrMagnitude < .1f) towardHall = Vector3.right;
            Vector3 arrivalForward = -towardHall;
            Vector3 right = new Vector3(arrivalForward.z,0f,-arrivalForward.x);
            Vector3 entrance = ExtremePoint(Points(school),schoolCenter,arrivalForward);

            Vector3 forecourt = entrance+arrivalForward*8.0f; forecourt.y=.04f;
            FlatBox("entrance forecourt",group,forecourt,right,23f,13f,.08f,paving);
            Curbs(group,forecourt,right,arrivalForward,23.4f,13.4f,metal,false);

            Vector3 parking = forecourt+arrivalForward*15.0f; parking.y=.04f;
            FlatBox("parking dropoff",group,parking,right,32f,15f,.07f,asphalt);
            Curbs(group,parking,right,arrivalForward,32.4f,15.4f,metal,true);
            for (int i=-4; i<=4; i++) FlatBox("parking stripe",group,parking+right*(i*3.05f)+arrivalForward*3.8f,arrivalForward,.10f,5.2f,.018f,paint);
            for (int i=-3; i<=3; i++) FlatBox("zebra stripe",group,forecourt+arrivalForward*5.2f+right*(i*.58f),right,.34f,4.2f,.019f,paint);

            Vector3 island = parking+right*1.8f;
            FlatBox("green parking island",group,island,right,3.7f,10.8f,.16f,grass);
            Curbs(group,island,right,arrivalForward,4.0f,11.1f,metal,false);

            GameObject bench = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/bench.obj");
            GameObject lamp = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/streetlight.obj");
            GameObject sedan = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/car_sedan.obj");
            GameObject wagon = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/car_stationwagon.obj");
            GameObject dumpster = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/dumpster.obj");
            GameObject pine = AssetDatabase.LoadAssetAtPath<GameObject>(NatureRoot+"/tree_pineDefaultA.obj");
            GameObject tallPine = AssetDatabase.LoadAssetAtPath<GameObject>(NatureRoot+"/tree_pineTallA.obj");
            GameObject fallTree = AssetDatabase.LoadAssetAtPath<GameObject>(NatureRoot+"/tree_default_fall.obj");
            GameObject bush = AssetDatabase.LoadAssetAtPath<GameObject>(NatureRoot+"/plant_bushDetailed.obj");

            if (sedan) Place(sedan,group,"abandoned staff car",parking-right*9.2f+arrivalForward*3.0f,Yaw(arrivalForward),1.42f,city);
            if (wagon) Place(wagon,group,"caretaker wagon",parking+right*10.5f-arrivalForward*2.5f,Yaw(-arrivalForward)+4f,1.46f,city);
            if (bench) {
                Place(bench,group,"entrance bench A",forecourt-right*7.6f-arrivalForward*3.5f,Yaw(right),.90f,city);
                Place(bench,group,"entrance bench B",forecourt+right*7.6f-arrivalForward*3.5f,Yaw(-right),.90f,city);
            }
            Vector3 rack = forecourt-right*8.6f+arrivalForward*1.4f;
            for (int i=0;i<8;i++) BikeRack(group,rack+arrivalForward*(i*.65f-2.25f),right,metal);

            Vector3 yard = schoolCenter+right*20f+towardHall*2f; yard.y=.04f;
            FlatBox("schoolyard",group,yard,right,27f,21f,.07f,grass);
            Curbs(group,yard,right,towardHall,27.4f,21.4f,metal,false);
            Picnic(group,yard+right*7.8f-towardHall*5.2f,right,wood,metal);
            Swing(group,yard-right*7.2f+towardHall*4.4f,right,wood,metal);
            if (bench) Place(bench,group,"schoolyard bench",yard+right*9.5f+towardHall*7f,Yaw(-right),.90f,city);
            for (int s=-1;s<=1;s+=2) {
                FlatBox("play court side",group,yard+right*(s*5f),towardHall,.10f,9f,.018f,paint);
                FlatBox("play court end",group,yard+towardHall*(s*4.5f),right,10f,.10f,.018f,paint);
            }
            FlatBox("play court middle",group,yard,right,10f,.10f,.018f,paint);

            Vector3 service = hall != null ? RosvikOsmV15.Centroid(hall)+towardHall*15f : schoolCenter+towardHall*28f;
            service.y=.04f;
            FlatBox("service yard",group,service,right,19f,12f,.07f,asphalt);
            Curbs(group,service,right,towardHall,19.4f,12.4f,metal,true);
            Box("blue service container",group,service+right*5.2f+Vector3.up*1.25f,new Vector3(5.6f,2.5f,2.35f),Rot(right),Mat("container",new Color(.15f,.25f,.28f),.12f),false);
            if (dumpster) Place(dumpster,group,"school dumpster",service-right*5.8f+towardHall*3f,Yaw(right),1.34f,city);

            Vector3[] lampPos = { forecourt-right*9f-arrivalForward*4.6f, forecourt+right*9f-arrivalForward*4.6f, parking-right*13.8f, parking+right*13.8f, yard-right*11.5f, service+right*7f-towardHall*4.8f };
            if (lamp) {
                for (int i=0;i<lampPos.Length;i++) {
                    Place(lamp,group,"campus lamp",lampPos[i],Yaw(arrivalForward),4.65f,city);
                    if (i<4 || i==5) AddLight(group,lampPos[i]+Vector3.up*3.45f,8.5f,1.0f,bulb);
                }
            }

            Vector3[] treePos = { forecourt-right*11f+arrivalForward*3.5f, forecourt+right*11f+arrivalForward*3.5f, parking-right*14.5f-arrivalForward*5f, parking+right*14.5f-arrivalForward*5f, yard-right*12.5f+towardHall*7f, yard+right*12.5f-towardHall*7f, service+right*8f+towardHall*5f };
            GameObject[] treeSrc = { fallTree,pine,tallPine,pine,tallPine,fallTree,pine };
            Material[] treeMat = { autumnMat,spruceMat,spruceMat,spruceMat,spruceMat,autumnMat,spruceMat };
            for (int i=0;i<treePos.Length;i++) if (treeSrc[i]) Place(treeSrc[i],group,"campus tree",treePos[i],31+i*47,4.9f+(i%3)*.5f,treeMat[i]);
            if (bush) for (int i=-3;i<=3;i++) Place(bush,group,"entrance shrub",forecourt+right*i*2.0f-arrivalForward*5.2f,i*39,.58f+(Mathf.Abs(i)%2)*.12f,shrubMat);

            Patch("frost edge",group,forecourt+right*6f+arrivalForward*2f,right,5.5f,1.5f,.025f,frost,22,4501);
            Patch("parking puddle",group,parking-right*4f-arrivalForward*1f,right,3.1f,.8f,.031f,puddle,20,4502);
            Patch("service thaw",group,service+right*2f-towardHall*2f,right,2.5f,.7f,.031f,puddle,18,4503);
        }

        static void BikeRack(Transform parent,Vector3 center,Vector3 axis,Material material) {
            Vector3 forward = new Vector3(-axis.z,0f,axis.x);
            Box("rack leg",parent,center-forward*.30f+Vector3.up*.42f,new Vector3(.07f,.84f,.07f),Quaternion.identity,material,false);
            Box("rack leg",parent,center+forward*.30f+Vector3.up*.42f,new Vector3(.07f,.84f,.07f),Quaternion.identity,material,false);
            Box("rack top",parent,center+Vector3.up*.82f,new Vector3(.07f,.07f,.68f),Rot(forward),material,false);
        }

        static void Picnic(Transform parent,Vector3 center,Vector3 axis,Material wood,Material metal) {
            Vector3 forward = new Vector3(-axis.z,0f,axis.x);
            Box("picnic top",parent,center+Vector3.up*.72f,new Vector3(2.8f,.16f,1.1f),Rot(axis),wood,false);
            Box("picnic seat",parent,center+forward*.92f+Vector3.up*.42f,new Vector3(2.8f,.13f,.38f),Rot(axis),wood,false);
            Box("picnic seat",parent,center-forward*.92f+Vector3.up*.42f,new Vector3(2.8f,.13f,.38f),Rot(axis),wood,false);
            for (int s=-1;s<=1;s+=2) for (int q=-1;q<=1;q+=2) Box("picnic leg",parent,center+axis*(s*.9f)+forward*(q*.35f)+Vector3.up*.34f,new Vector3(.12f,.68f,.12f),Quaternion.identity,metal,false);
        }

        static void Swing(Transform parent,Vector3 center,Vector3 axis,Material wood,Material metal) {
            Vector3 forward = new Vector3(-axis.z,0f,axis.x);
            Box("swing beam",parent,center+Vector3.up*2.65f,new Vector3(5.6f,.18f,.18f),Rot(axis),wood,false);
            for (int s=-1;s<=1;s+=2) {
                Box("swing post",parent,center+axis*(s*2.45f)+forward*.55f+Vector3.up*1.35f,new Vector3(.16f,2.70f,.16f),Quaternion.identity,wood,false);
                Box("swing post",parent,center+axis*(s*2.45f)-forward*.55f+Vector3.up*1.35f,new Vector3(.16f,2.70f,.16f),Quaternion.identity,wood,false);
            }
            for (int s=-1;s<=1;s+=2) {
                Vector3 seat = center+axis*(s*.9f)+Vector3.up*.58f;
                Box("swing rope",parent,seat-axis*.25f+Vector3.up*.95f,new Vector3(.025f,1.9f,.025f),Quaternion.identity,metal,false);
                Box("swing rope",parent,seat+axis*.25f+Vector3.up*.95f,new Vector3(.025f,1.9f,.025f),Quaternion.identity,metal,false);
                Box("swing seat",parent,seat,new Vector3(.72f,.09f,.30f),Rot(axis),wood,false);
            }
        }

        static void Curbs(Transform parent,Vector3 center,Vector3 right,Vector3 forward,float width,float depth,Material material,bool openBack) {
            Box("curb",parent,center+right*(width*.5f)+Vector3.up*.09f,new Vector3(.20f,.18f,depth),Rot(forward),material,false);
            Box("curb",parent,center-right*(width*.5f)+Vector3.up*.09f,new Vector3(.20f,.18f,depth),Rot(forward),material,false);
            Box("curb",parent,center+forward*(depth*.5f)+Vector3.up*.09f,new Vector3(width,.18f,.20f),Rot(right),material,false);
            if (!openBack) Box("curb",parent,center-forward*(depth*.5f)+Vector3.up*.09f,new Vector3(width,.18f,.20f),Rot(right),material,false);
        }

        static void FlatBox(string name,Transform parent,Vector3 center,Vector3 axis,float width,float depth,float height,Material material) {
            Box(name,parent,new Vector3(center.x,height*.5f,center.z),new Vector3(width,height,depth),Rot(axis),material,false);
        }

        static GameObject Box(string name,Transform parent,Vector3 position,Vector3 scale,Quaternion rotation,Material material,bool collider) {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name; obj.transform.SetParent(parent,true); obj.transform.position = position; obj.transform.rotation = rotation; obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
            return obj;
        }

        static void Panel(string name,Transform parent,Vector3 position,Vector3 outward,Vector3 scale,Material material) {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name; obj.transform.SetParent(parent,true); obj.transform.position = position; obj.transform.rotation = Quaternion.LookRotation(outward,Vector3.up); obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
        }

        static void AddLight(Transform parent,Vector3 position,float range,float intensity,Material bulb) {
            GameObject lightObj = new GameObject("warm campus light"); lightObj.transform.SetParent(parent,true); lightObj.transform.position = position;
            Light light = lightObj.AddComponent<Light>(); light.type=LightType.Point; light.color=new Color(1f,.58f,.23f); light.range=range; light.intensity=intensity; light.shadows=LightShadows.Soft;
            GameObject orb=GameObject.CreatePrimitive(PrimitiveType.Sphere); orb.name="warm bulb"; orb.transform.SetParent(lightObj.transform,false); orb.transform.localScale=Vector3.one*.12f; orb.GetComponent<Renderer>().sharedMaterial=bulb; UnityEngine.Object.DestroyImmediate(orb.GetComponent<Collider>());
        }

        static GameObject Place(GameObject asset,Transform parent,string name,Vector3 position,float yaw,float targetHeight,Material material) {
            if (!asset) return null;
            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(asset); if (!obj) obj=UnityEngine.Object.Instantiate(asset);
            obj.name=name; obj.transform.SetParent(parent,true); obj.transform.position=position; obj.transform.rotation=Quaternion.Euler(0f,yaw,0f); obj.transform.localScale=Vector3.one;
            Bounds bounds=BoundsOf(obj); float scale=targetHeight/Mathf.Max(.01f,bounds.size.y); obj.transform.localScale=Vector3.one*scale; Ground(obj,.04f);
            foreach (Renderer renderer in obj.GetComponentsInChildren<Renderer>(true)) { renderer.sharedMaterial=material; renderer.shadowCastingMode=ShadowCastingMode.On; renderer.receiveShadows=true; }
            foreach (Collider collider in obj.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
            return obj;
        }

        static Bounds BoundsOf(GameObject obj) {
            Renderer[] renderers=obj.GetComponentsInChildren<Renderer>(true); if(renderers.Length==0)return new Bounds(obj.transform.position,Vector3.one);
            Bounds bounds=renderers[0].bounds; for(int i=1;i<renderers.Length;i++)bounds.Encapsulate(renderers[i].bounds); return bounds;
        }
        static void Ground(GameObject obj,float y){Bounds bounds=BoundsOf(obj);obj.transform.position+=Vector3.up*(y-bounds.min.y);}

        static void Patch(string name,Transform parent,Vector3 center,Vector3 axis,float length,float width,float y,Material material,int segments,int seed) {
            axis=Flat(axis).normalized; if(axis.sqrMagnitude<.1f)axis=Vector3.right; Vector3 perp=new Vector3(-axis.z,0f,axis.x); System.Random rng=new System.Random(seed);
            GameObject obj=new GameObject(name); obj.transform.SetParent(parent,false); Mesh mesh=new Mesh(); mesh.name=name; Vector3[] vertices=new Vector3[segments+1]; int[] triangles=new int[segments*3]; vertices[0]=new Vector3(center.x,y,center.z);
            for(int i=0;i<segments;i++){float angle=Mathf.PI*2f*i/segments;float wobble=.82f+(float)rng.NextDouble()*.28f;Vector3 point=center+axis*(Mathf.Cos(angle)*length*.5f*wobble)+perp*(Mathf.Sin(angle)*width*.5f*wobble);vertices[i+1]=new Vector3(point.x,y,point.z);int next=(i+1)%segments;triangles[i*3]=0;triangles[i*3+1]=i+1;triangles[i*3+2]=next+1;}
            mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=material;
        }

        static Material Mat(string name,Color color,float smoothness) {
            string path=GeneratedDir+"/mat_"+name+".mat"; Material material=AssetDatabase.LoadAssetAtPath<Material>(path); Shader shader=ResolveShader();
            if(!material){material=new Material(shader);material.name="V45 "+name;AssetDatabase.CreateAsset(material,path);} material.shader=shader; material.color=color;
            if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",color);if(material.HasProperty("_Color"))material.SetColor("_Color",color);if(material.HasProperty("_Smoothness"))material.SetFloat("_Smoothness",smoothness);if(material.HasProperty("_Glossiness"))material.SetFloat("_Glossiness",smoothness);if(material.HasProperty("_Metallic"))material.SetFloat("_Metallic",0f);EditorUtility.SetDirty(material);return material;
        }
        static Material Emissive(string name,Color color,float multiplier){Material material=Mat(name,color,.30f);if(material.HasProperty("_EmissionColor")){material.SetColor("_EmissionColor",color*multiplier);material.EnableKeyword("_EMISSION");}EditorUtility.SetDirty(material);return material;}
        static Material Textured(string name,Texture2D texture,float smoothness){Material material=Mat(name,Color.white,smoothness);if(texture){if(material.HasProperty("_BaseMap"))material.SetTexture("_BaseMap",texture);if(material.HasProperty("_MainTex"))material.SetTexture("_MainTex",texture);EditorUtility.SetDirty(material);}return material;}
        static Shader ResolveShader(){Material proven=AssetDatabase.LoadAssetAtPath<Material>(ProvenMaterial);if(proven&&proven.shader&&proven.shader.isSupported)return proven.shader;Shader shader=GraphicsSettings.defaultRenderPipeline!=null?Shader.Find("Universal Render Pipeline/Lit"):Shader.Find("Standard");if(!shader||!shader.isSupported)shader=Shader.Find("Sprites/Default");return shader;}

        static Vector3 ExtremePoint(List<Vector3> poly,Vector3 center,Vector3 direction){float best=float.MinValue;Vector3 result=poly[0];for(int i=0;i<poly.Count;i++){float score=Vector3.Dot(Flat(poly[i]-center),direction);if(score>best){best=score;result=poly[i];}}return result;}
        static float BoundaryGap(List<Vector3> a,List<Vector3> b){Vector3 pa,pb;return BoundaryPair(a,b,out pa,out pb);}
        static float BoundaryPair(List<Vector3> a,List<Vector3> b,out Vector3 pa,out Vector3 pb){float best=float.MaxValue;pa=a[0];pb=b[0];for(int i=0;i<a.Count;i++){Vector3 a0=a[i];Vector3 a1=a[(i+1)%a.Count];for(int j=0;j<b.Count;j++){Vector3 b0=b[j];Vector3 b1=b[(j+1)%b.Count];Vector3 q=ClosestPoint(a0,b0,b1);float d=Flat(q-a0).sqrMagnitude;if(d<best){best=d;pa=a0;pb=q;}q=ClosestPoint(b0,a0,a1);d=Flat(q-b0).sqrMagnitude;if(d<best){best=d;pa=q;pb=b0;}}}return Mathf.Sqrt(best);}
        static Vector3 ClosestPoint(Vector3 p,Vector3 a,Vector3 b){Vector3 ab=Flat(b-a);float denom=ab.sqrMagnitude;if(denom<.0001f)return a;float t=Mathf.Clamp01(Vector3.Dot(Flat(p-a),ab)/denom);return a+ab*t;}
        static List<Vector3> Points(RosvikOsmV15.Way way){List<Vector3> pts=way.Nodes.Select(node=>node.Pos).ToList();if(pts.Count>2&&way.Closed&&Vector3.Distance(pts[0],pts[pts.Count-1])<.01f)pts.RemoveAt(pts.Count-1);return pts;}
        static float SignedArea(List<Vector3> pts){float area=0f;for(int i=0;i<pts.Count;i++){Vector3 next=pts[(i+1)%pts.Count];area+=pts[i].x*next.z-next.x*pts[i].z;}return area*.5f;}
        static Vector3 Flat(Vector3 value){return new Vector3(value.x,0f,value.z);}
        static float Yaw(Vector3 direction){return Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;}
        static Quaternion Rot(Vector3 direction){return Quaternion.Euler(0f,Yaw(direction),0f);}
        static Transform NewGroup(Transform parent,string name){GameObject obj=new GameObject(name);obj.transform.SetParent(parent,false);return obj.transform;}
        static Transform Find(Transform root,string name){return root.GetComponentsInChildren<Transform>(true).FirstOrDefault(x=>x.name.Equals(name,StringComparison.OrdinalIgnoreCase));}
        static void Disable(Transform root,string name){Transform found=Find(root,name);if(found)found.gameObject.SetActive(false);}

        static void TuneMood(){RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.31f,.30f,.265f);RenderSettings.fog=true;RenderSettings.fogColor=new Color(.39f,.39f,.365f);RenderSettings.fogDensity=.0013f;RenderSettings.reflectionIntensity=.55f;foreach(Light light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(x=>x.type==LightType.Directional)){light.intensity=1.04f;light.color=new Color(1f,.80f,.58f);light.shadows=LightShadows.Soft;light.shadowStrength=.76f;light.transform.rotation=Quaternion.Euler(42f,-52f,0f);}}
    }
}
#endif