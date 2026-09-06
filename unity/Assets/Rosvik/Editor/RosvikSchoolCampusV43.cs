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
    /// V43 is the first large school-campus art pass. It keeps the mapped Rosvik world,
    /// gameplay and road network intact, but replaces the sparse school hero dressing with
    /// a coherent campus: a warm school facade, the nearby sports hall, their connector,
    /// arrival/drop-off, parking, schoolyard, service yard and intentional vegetation.
    /// </summary>
    [InitializeOnLoad]
    public static class RosvikSchoolCampusV43 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_SCHOOL_CAMPUS_V43_VERSION";
        const int Version = 43;
        const string GroupName = "25 SCHOOL CAMPUS V43 - BIG PASS";
        const string GeneratedDir = "Assets/Rosvik/GeneratedV43";
        const string ProvenMaterial = "Assets/Rosvik/GeneratedV20/mat_ground.mat";
        const string CityRoot = "Assets/Rosvik/ThirdParty/V40CozyBits";
        const string NatureRoot = "Assets/Rosvik/ThirdParty/V22Models/KenneyNature";
        const string CityTexture = CityRoot + "/citybits_texture.png";
        const long MainSchoolWay = 163199458;
        const long OldSchoolWay = 163199461;
        const long ArenaWay = 163199454;

        struct EntryInfo {
            public bool valid;
            public Vector3 wall, center, forward, right;
        }

        static RosvikSchoolCampusV43() {
            EditorApplication.update -= TryApply;
            EditorApplication.update += TryApply;
        }

        [MenuItem("Rosvik/Rebuild School Campus V43 - BIG PASS")]
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
            return GameObject.Find("ROSVIK_HERO_COMPOSITION_V42")
                ?? GameObject.Find("ROSVIK_HERO_AREA_ASSETS_V41")
                ?? GameObject.Find("ROSVIK_ASSET_COZY_APOCALYPSE_V40")
                ?? GameObject.Find("ROSVIK_CLEAN_ROAD_NETWORK_V39")
                ?? GameObject.Find("ROSVIK_VILLAGE_FABRIC_V38");
        }

        static void Apply(UScene scene, GameObject root) {
            try {
                Directory.CreateDirectory(GeneratedDir);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                List<RosvikOsmV15.Way> ways = RosvikOsmV15.LoadWays();
                if (ways == null || ways.Count == 0) throw new InvalidOperationException("V43 could not load Rosvik OSM data.");
                RosvikOsmV15.Way school = ways.FirstOrDefault(w => w.Id == MainSchoolWay);
                if (school == null) throw new InvalidOperationException("V43 could not locate Rosviks skola.");

                EntryInfo entry = ResolveMainEntry(root.transform, school);
                RosvikOsmV15.Way sportHall = FindSportsHall(ways, school);

                Transform old = Find(root.transform, GroupName);
                if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
                Transform group = NewGroup(root.transform, GroupName);

                // V43 owns the school hero area. Hide the small earlier dressing passes to avoid
                // the old 'props sprinkled on a rectangle' look while preserving the mapped world.
                SetActive(root.transform, "23 HERO AREA ASSETS V41", false);
                SetActive(root.transform, "24 HERO COMPOSITION V42", false);
                SetActive(root.transform, "11 SCHOOL LIFE V28", false);
                SetActive(root.transform, "12 SCHOOLYARD POLISH V29", false);
                SetActive(root.transform, "ROSVIKS SKOLA facade", false);

                Material schoolWall = Mat("school_warm_plaster", new Color(.61f,.58f,.49f), .10f);
                Material schoolAccent = Mat("school_warm_brick", new Color(.36f,.20f,.13f), .09f);
                Material trim = Mat("school_trim", new Color(.79f,.77f,.67f), .18f);
                Material hallWall = Mat("sporthall_charcoal", new Color(.12f,.135f,.135f), .15f);
                Material hallAccent = Mat("sporthall_lower", new Color(.22f,.19f,.16f), .10f);
                Material glass = Mat("cold_glass", new Color(.055f,.105f,.125f), .55f);
                Material warmGlass = EmissiveMat("warm_window", new Color(.84f,.49f,.20f), 1.45f);
                Material asphalt = Mat("campus_asphalt", new Color(.13f,.14f,.135f), .26f);
                Material paving = Mat("entrance_paving", new Color(.35f,.335f,.29f), .12f);
                Material gravel = Mat("yard_gravel", new Color(.38f,.34f,.27f), .06f);
                Material curb = Mat("curb", new Color(.48f,.47f,.42f), .12f);
                Material paint = Mat("road_paint", new Color(.72f,.71f,.63f), .08f);
                Material metal = Mat("campus_metal", new Color(.075f,.08f,.078f), .32f);
                Material wood = Mat("campus_wood", new Color(.34f,.19f,.10f), .08f);
                Material container = Mat("service_container", new Color(.16f,.25f,.27f), .16f);
                Material grass = Mat("campus_grass", new Color(.19f,.265f,.14f), .04f);
                Material frost = Mat("first_frost", new Color(.66f,.70f,.69f), .18f);
                Material wet = Mat("wet_puddle", new Color(.065f,.105f,.115f), .72f);
                Material bulb = EmissiveMat("warm_bulb", new Color(1f,.54f,.18f), 2.4f);

                Texture2D cityTex = AssetDatabase.LoadAssetAtPath<Texture2D>(CityTexture);
                Material city = TexturedMat("citybits", cityTex, .20f);

                GameObject bench = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot + "/bench.obj");
                GameObject lamp = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot + "/streetlight.obj");
                GameObject dumpster = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot + "/dumpster.obj");
                GameObject trashA = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot + "/trash_A.obj");
                GameObject trashB = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot + "/trash_B.obj");
                GameObject boxA = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot + "/box_A.obj");
                GameObject boxB = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot + "/box_B.obj");
                GameObject sedan = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot + "/car_sedan.obj");
                GameObject wagon = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot + "/car_stationwagon.obj");
                GameObject hatch = AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot + "/car_hatchback.obj");
                GameObject pine = AssetDatabase.LoadAssetAtPath<GameObject>(NatureRoot + "/tree_pineDefaultA.obj");
                GameObject tallPine = AssetDatabase.LoadAssetAtPath<GameObject>(NatureRoot + "/tree_pineTallA.obj");
                GameObject fallTree = AssetDatabase.LoadAssetAtPath<GameObject>(NatureRoot + "/tree_default_fall.obj");
                GameObject bush = AssetDatabase.LoadAssetAtPath<GameObject>(NatureRoot + "/plant_bushDetailed.obj");

                BuildSchoolFacade(group, school, entry, schoolWall, schoolAccent, trim, glass, warmGlass, wood, bulb);
                if (sportHall != null) {
                    BuildSportHallFacade(group, sportHall, hallWall, hallAccent, trim, glass, warmGlass, bulb);
                    BuildConnector(group, school, sportHall, hallWall, trim, glass, warmGlass, bulb);
                }

                BuildArrivalAndParking(group, entry, ways, asphalt, paving, curb, paint, metal, wood, grass,
                    city, bench, lamp, sedan, hatch, pine, fallTree, bush, bulb);
                BuildSchoolyard(group, entry, ways, gravel, curb, paint, wood, metal, city, bench, lamp,
                    pine, tallPine, fallTree, bush, bulb);
                BuildServiceArea(group, entry, sportHall, ways, asphalt, curb, metal, wood, container, city,
                    dumpster, trashA, trashB, boxA, boxB, wagon, lamp, bulb);
                BuildCampusVegetation(group, entry, ways, pine, tallPine, fallTree, bush, grass);
                BuildGroundStory(group, entry, frost, wet);
                TuneMood();

                EditorPrefs.SetInt(Key, Version);
                Selection.activeObject = group.gameObject;
                EditorUtility.SetDirty(root);
                AssetDatabase.SaveAssets();
                if (!Busy()) {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene, ScenePath);
                }

                string hallInfo = sportHall == null ? "sports hall not confidently detected" :
                    "sports hall way " + sportHall.Id + (string.IsNullOrEmpty(sportHall.Tag("name")) ? "" : " (" + sportHall.Tag("name") + ")");
                Debug.Log("ROSVIK V43 BIG PASS: coherent school campus built; " + hallInfo + ". School, connector, arrival/parking, schoolyard, service yard, vegetation and cozy lighting rebuilt. Roads/gameplay/controls unchanged.");
            } catch (Exception ex) {
                Debug.LogError("ROSVIK V43 FAILED: " + ex);
            }
        }

        static EntryInfo ResolveMainEntry(Transform root, RosvikOsmV15.Way school) {
            Vector3 c = RosvikOsmV15.Centroid(school); c.y = .04f;
            Transform court = Find(root, "school entrance court");
            if (court) {
                Bounds cb = BoundsOf(court.gameObject);
                Vector3 outward = Flat(cb.center - c).normalized;
                if (outward.sqrMagnitude > .1f) {
                    Vector3 right = new Vector3(outward.z, 0f, -outward.x);
                    Vector3 wall = ClosestPointOnPolygon(c + outward * 30f, Points(school)); wall.y = .04f;
                    return new EntryInfo { valid=true, wall=wall, center=wall+outward*4.8f, forward=outward, right=right };
                }
            }

            List<Vector3> pts = Points(school);
            float area = SignedArea(pts);
            int longest = 0; float best = 0f;
            for (int i=0;i<pts.Count;i++) {
                Vector3 a=pts[i], b=pts[(i+1)%pts.Count]; float len=Flat(b-a).magnitude;
                if (len>best) { best=len; longest=i; }
            }
            Vector3 p0=pts[longest], p1=pts[(longest+1)%pts.Count];
            Vector3 rightEdge=Flat(p1-p0).normalized;
            Vector3 left=new Vector3(-rightEdge.z,0f,rightEdge.x);
            Vector3 forward=area>0f?-left:left;
            Vector3 wallMid=(p0+p1)*.5f; wallMid.y=.04f;
            return new EntryInfo { valid=true, wall=wallMid, center=wallMid+forward*4.8f, forward=forward, right=rightEdge };
        }

        static RosvikOsmV15.Way FindSportsHall(List<RosvikOsmV15.Way> ways, RosvikOsmV15.Way school) {
            Vector3 sc = RosvikOsmV15.Centroid(school);
            var buildings = ways.Where(w => w.Closed && w.Id != MainSchoolWay && w.Id != OldSchoolWay && w.Id != ArenaWay &&
                !string.IsNullOrEmpty(w.Tag("building")) && w.Tag("building") != "no").ToList();

            RosvikOsmV15.Way named = buildings
                .Where(w => {
                    string n=(w.Tag("name") ?? "").ToLowerInvariant();
                    float d=Flat(RosvikOsmV15.Centroid(w)-sc).magnitude;
                    return d<120f && (n.Contains("sporthall") || n.Contains("sport hall") || n.Contains("gym"));
                })
                .OrderBy(w => Flat(RosvikOsmV15.Centroid(w)-sc).sqrMagnitude).FirstOrDefault();
            if (named != null) return named;

            return buildings
                .Select(w => new { w, b=RosvikOsmV15.Bounds(w), d=Flat(RosvikOsmV15.Centroid(w)-sc).magnitude })
                .Where(x => x.d>9f && x.d<78f && x.b.Width>9f && x.b.Depth>9f && x.b.Width*x.b.Depth>180f)
                .OrderBy(x => x.d).Select(x => x.w).FirstOrDefault();
        }

        static void BuildSchoolFacade(Transform parent, RosvikOsmV15.Way school, EntryInfo entry,
            Material wall, Material brick, Material trim, Material glass, Material warmGlass, Material wood, Material bulb) {
            Transform g=NewGroup(parent,"V43 SCHOOL - WARM HERO BUILDING");
            List<Vector3> pts=Points(school); float area=SignedArea(pts); int wi=0;
            for(int i=0;i<pts.Count;i++) {
                Vector3 a=pts[i], b=pts[(i+1)%pts.Count]; Vector3 edge=Flat(b-a); float len=edge.magnitude; if(len<1.8f)continue;
                edge/=len; Vector3 left=new Vector3(-edge.z,0f,edge.x); Vector3 outward=area>0f?-left:left;
                Vector3 mid=(a+b)*.5f;

                Panel("warm facade skin",g,mid+outward*.065f+Vector3.up*1.68f,outward,new Vector3(len-.10f,3.12f,.11f),wall);
                Panel("brick plinth",g,mid+outward*.13f+Vector3.up*.34f,outward,new Vector3(len-.06f,.58f,.10f),brick);
                Panel("top fascia",g,mid+outward*.13f+Vector3.up*3.02f,outward,new Vector3(len,.18f,.13f),trim);

                int count=Mathf.Clamp(Mathf.FloorToInt(len/3.0f),1,12); float margin=Mathf.Min(1.25f,len*.15f);
                float usable=Mathf.Max(.5f,len-margin*2f); float step=usable/count;
                for(int k=0;k<count;k++) {
                    float along=-len*.5f+margin+step*(k+.5f); Vector3 p=mid+edge*along;
                    Material gm=((wi%7)==2 || (wi%11)==6)?warmGlass:glass; wi++;
                    Panel("window frame",g,p+outward*.16f+Vector3.up*1.72f,outward,new Vector3(Mathf.Min(1.62f,step*.67f),1.34f,.09f),trim);
                    Panel("school window",g,p+outward*.22f+Vector3.up*1.72f,outward,new Vector3(Mathf.Min(1.42f,step*.58f),1.12f,.055f),gm);
                }
            }

            // Deliberately oversized/readable entrance cluster.
            Vector3 f=entry.forward, r=entry.right, w=entry.wall;
            Panel("entrance dark surround",g,w+f*.28f+Vector3.up*1.38f,f,new Vector3(5.2f,2.62f,.20f),brick);
            Panel("entrance glass left",g,w+f*.40f-r*.92f+Vector3.up*1.25f,f,new Vector3(1.48f,2.20f,.08f),warmGlass);
            Panel("entrance glass right",g,w+f*.40f+r*.92f+Vector3.up*1.25f,f,new Vector3(1.48f,2.20f,.08f),warmGlass);
            Panel("entrance mullion",g,w+f*.47f+Vector3.up*1.25f,f,new Vector3(.12f,2.24f,.10f),trim);
            Box("timber canopy",g,w+f*1.15f+Vector3.up*2.83f,new Vector3(5.8f,.20f,2.35f),RotY(Yaw(r)),wood,false);
            for(int s=-1;s<=1;s+=2) Box("canopy post",g,w+f*1.9f+r*(s*2.35f)+Vector3.up*1.38f,new Vector3(.15f,2.55f,.15f),Quaternion.identity,wood,false);
            AddWorldText(g,"ROSVIKS SKOLA",w+f*.52f-r*3.5f+Vector3.up*3.00f,f,1.02f,trim);
            AddWarmLight(g,w+f*2.05f+Vector3.up*2.55f,10.5f,1.65f,bulb,"warm entrance light");
        }

        static void BuildSportHallFacade(Transform parent, RosvikOsmV15.Way hall,
            Material wall, Material lower, Material trim, Material glass, Material warmGlass, Material bulb) {
            Transform g=NewGroup(parent,"V43 SPORT HALL - DARK MASS");
            List<Vector3> pts=Points(hall); float area=SignedArea(pts); int edgeIndex=0;
            for(int i=0;i<pts.Count;i++) {
                Vector3 a=pts[i],b=pts[(i+1)%pts.Count];Vector3 edge=Flat(b-a);float len=edge.magnitude;if(len<2f)continue;
                edge/=len;Vector3 left=new Vector3(-edge.z,0f,edge.x);Vector3 outward=area>0f?-left:left;Vector3 mid=(a+b)*.5f;
                Panel("sporthall charcoal skin",g,mid+outward*.08f+Vector3.up*3.65f,outward,new Vector3(len-.08f,6.90f,.14f),wall);
                Panel("sporthall warm lower band",g,mid+outward*.16f+Vector3.up*.65f,outward,new Vector3(len-.06f,1.15f,.10f),lower);
                Panel("sporthall roof fascia",g,mid+outward*.16f+Vector3.up*6.88f,outward,new Vector3(len,.20f,.15f),trim);
                int count=Mathf.Clamp(Mathf.FloorToInt(len/5.5f),1,8);
                for(int k=0;k<count;k++) {
                    float t=(k+.5f)/count;Vector3 p=Vector3.Lerp(a,b,t);
                    Panel("high hall window frame",g,p+outward*.18f+Vector3.up*4.25f,outward,new Vector3(1.55f,.92f,.09f),trim);
                    Panel("high hall window",g,p+outward*.24f+Vector3.up*4.25f,outward,new Vector3(1.34f,.72f,.055f),((edgeIndex+k)%9)==3?warmGlass:glass);
                }
                edgeIndex++;
            }

            // Put identity/signage on the side facing the school.
            Vector3 hc=RosvikOsmV15.Centroid(hall); Vector3 sc=FindClosestBoundaryPoint(hall, hc + Flat(-hc)*.01f);
            Vector3 towardSchool=Vector3.zero;
            // Main school is not passed here; choose facade direction from bounds as fallback.
            RosvikOsmV15.OBounds hb=RosvikOsmV15.Bounds(hall); towardSchool=new Vector3(-hb.AxisX.z,0,hb.AxisX.x).normalized;
            Vector3 signPos=hb.Center+towardSchool*(Mathf.Min(hb.Width,hb.Depth)*.5f+.20f)+Vector3.up*3.15f;
            AddWorldText(g,"ROSVIK SPORTHALL",signPos,towardSchool,1.08f,trim);
            AddWarmLight(g,signPos-Vector3.up*1.55f+towardSchool*.7f,9.5f,1.25f,bulb,"hall door light");
        }

        static void BuildConnector(Transform parent, RosvikOsmV15.Way school, RosvikOsmV15.Way hall,
            Material dark, Material trim, Material glass, Material warmGlass, Material bulb) {
            Vector3 a,b; float gap=ClosestBoundaryPair(Points(school),Points(hall),out a,out b);
            if(gap<.55f || gap>28f)return;
            Vector3 dir=Flat(b-a);float len=dir.magnitude;if(len<.5f)return;dir/=len;Vector3 side=new Vector3(-dir.z,0f,dir.x);Vector3 mid=(a+b)*.5f;
            Transform g=NewGroup(parent,"V43 SCHOOL-SPORTHALL CONNECTOR");
            Box("connector floor",g,mid+Vector3.up*.08f,new Vector3(3.35f,.16f,len+.65f),RotY(Yaw(dir)),trim,false);
            Box("connector roof",g,mid+Vector3.up*3.05f,new Vector3(3.55f,.22f,len+.85f),RotY(Yaw(dir)),dark,false);
            for(int s=-1;s<=1;s+=2) {
                Vector3 sideCenter=mid+side*(s*1.63f)+Vector3.up*1.55f;
                Box("connector glass side",g,sideCenter,new Vector3(len,.07f,2.55f),Quaternion.Euler(0,Yaw(dir)-90f,0),glass,false);
                int mullions=Mathf.Max(2,Mathf.RoundToInt(len/2.1f));
                for(int k=0;k<=mullions;k++) {
                    float t=(float)k/mullions;Vector3 p=Vector3.Lerp(a,b,t)+side*(s*1.66f)+Vector3.up*1.55f;
                    Box("connector mullion",g,p,new Vector3(.09f,2.72f,.09f),Quaternion.identity,trim,false);
                }
            }
            Panel("warm connector end",g,a+dir*.16f+Vector3.up*1.55f,-dir,new Vector3(3.05f,2.50f,.08f),warmGlass);
            AddWarmLight(g,mid+Vector3.up*2.35f,8.5f,1.15f,bulb,"connector warm light");
        }

        static void BuildArrivalAndParking(Transform parent, EntryInfo e, List<RosvikOsmV15.Way> ways,
            Material asphalt, Material paving, Material curb, Material paint, Material metal, Material wood, Material grass,
            Material city, GameObject bench, GameObject lamp, GameObject sedan, GameObject hatch,
            GameObject pine, GameObject fallTree, GameObject bush, Material bulb) {
            Transform g=NewGroup(parent,"V43 ARRIVAL + PARKING");Vector3 r=e.right,f=e.forward;
            Vector3 plaza=e.wall+f*5.5f; plaza.y=.05f;
            FlatBox("large entrance plaza",g,plaza,r,18.5f,9.2f,.075f,paving);
            AddCurbs(g,plaza,r,f,18.8f,9.5f,curb,false);

            Vector3 parking=plaza+f*12.9f-r*1.8f;parking.y=.045f;
            FlatBox("dropoff and parking",g,parking,r,28f,15.8f,.07f,asphalt);
            AddCurbs(g,parking,r,f,28.3f,16.1f,curb,true);

            // Planted island breaks the giant asphalt slab and makes the approach read as a real campus.
            Vector3 island=parking+r*1.3f;FlatBox("planting island",g,island,r,3.5f,10.8f,.16f,grass);AddCurbs(g,island,r,f,3.75f,11.05f,curb,false);
            if(fallTree) PlaceModel(fallTree,g,"parking birch",island+f*2.9f,17f,5.2f,Mat("late_birch",new Color(.34f,.29f,.10f),.03f));
            if(pine) PlaceModel(pine,g,"parking spruce",island-f*2.9f,91f,4.9f,Mat("spruce",new Color(.055f,.135f,.075f),.03f));
            if(bush) { PlaceModel(bush,g,"parking shrub",island+r*.5f,.0f,.70f,Mat("shrub",new Color(.14f,.22f,.09f),.03f)); PlaceModel(bush,g,"parking shrub",island-r*.6f+f*1.2f,63f,.60f,Mat("shrub",new Color(.14f,.22f,.09f),.03f)); }

            // Zebra crossing between drop-off and entrance.
            Vector3 zebra=plaza+f*5.2f-r*5.6f;
            for(int i=-3;i<=3;i++) FlatBox("crosswalk stripe",g,zebra+r*(i*.60f),r,.35f,4.0f,.016f,paint);

            // Parking bays on both sides; intentionally a few empty for apocalypse storytelling.
            for(int side=-1;side<=1;side+=2) {
                Vector3 row=parking+r*(side*9.2f);
                for(int i=-2;i<=2;i++) FlatBox("parking line",g,row+f*(i*2.65f),f,.10f,5.0f,.015f,paint);
            }
            if(sedan) PlaceModel(sedan,g,"abandoned staff sedan",parking-r*9.0f-f*2.9f,Yaw(f)+2f,1.42f,city);
            if(hatch) PlaceModel(hatch,g,"abandoned small car",parking+r*9.0f+f*3.0f,Yaw(-f)-3f,1.38f,city);

            // Proper bicycle zone.
            Vector3 bikes=plaza-r*7.1f-f*2.6f;
            for(int i=0;i<8;i++) BikeRack(g,bikes+f*(i*.68f-2.35f),r,metal);
            Box("bike rack back rail",g,bikes+Vector3.up*.28f,new Vector3(.07f,.07f,5.7f),RotY(Yaw(f)),metal,false);

            if(bench) {
                PlaceModel(bench,g,"entrance bench A",plaza-r*6.5f+f*.5f,Yaw(r),.90f,city);
                PlaceModel(bench,g,"entrance bench B",plaza+r*6.4f+f*.5f,Yaw(-r),.90f,city);
            }
            if(lamp) {
                Vector3[] lp={plaza-r*8.0f-f*2.2f,plaza+r*8.0f-f*2.2f,parking-r*12.4f,parking+r*12.4f,parking+f*6.4f};
                for(int i=0;i<lp.Length;i++) { PlaceModel(lamp,g,"campus lamp",lp[i],Yaw(f),4.7f,city); if(i<3)AddWarmLight(g,lp[i]+Vector3.up*3.5f,8.5f,1.05f,bulb,"campus pool light"); }
            }

            // Two low planters make the entrance feel designed, not procedurally scattered.
            Planter(g,plaza-r*4.3f+f*2.8f,r,2.5f,1.15f,curb,grass,bush);
            Planter(g,plaza+r*4.3f+f*2.8f,r,2.5f,1.15f,curb,grass,bush);
        }

        static void BuildSchoolyard(Transform parent, EntryInfo e, List<RosvikOsmV15.Way> ways,
            Material gravel, Material curb, Material paint, Material wood, Material metal, Material city,
            GameObject bench, GameObject lamp, GameObject pine, GameObject tallPine, GameObject fallTree, GameObject bush, Material bulb) {
            Transform g=NewGroup(parent,"V43 SCHOOLYARD - LIVED IN");Vector3 r=e.right,f=e.forward;
            Vector3 yard=e.wall-r*19.5f+f*7.0f;yard.y=.04f;
            FlatBox("schoolyard gravel",g,yard,r,23.5f,16.5f,.06f,gravel);AddCurbs(g,yard,r,f,23.8f,16.8f,curb,false);

            // Timber swing set, deliberately simple but recognisable from gameplay distance.
            Vector3 swing=yard-r*5.8f+f*2.0f;
            for(int s=-1;s<=1;s+=2) {
                Vector3 end=swing+r*(s*2.35f);
                Bar(g,"swing A frame",end-r*.65f+Vector3.up*1.45f,(Vector3.up*.95f+r*.42f).normalized,.16f,3.1f,wood);
                Bar(g,"swing A frame",end+r*.65f+Vector3.up*1.45f,(Vector3.up*.95f-r*.42f).normalized,.16f,3.1f,wood);
            }
            Bar(g,"swing top beam",swing+Vector3.up*2.72f,r,.18f,5.6f,wood);
            for(int s=-1;s<=1;s+=2) {
                Vector3 seat=swing+r*(s*.92f)+Vector3.up*.62f;
                Bar(g,"swing rope",seat-r*.27f+Vector3.up*.95f,Vector3.up,.025f,1.85f,metal);
                Bar(g,"swing rope",seat+r*.27f+Vector3.up*.95f,Vector3.up,.025f,1.85f,metal);
                Box("swing seat",g,seat,new Vector3(.70f,.09f,.30f),RotY(Yaw(r)),wood,false);
            }

            PicnicTable(g,yard+r*3.8f-f*1.8f,r,wood,metal);
            if(bench) PlaceModel(bench,g,"schoolyard bench",yard+r*8.2f+f*4.8f,Yaw(-r),.90f,city);
            if(lamp) { Vector3 lp=yard-r*9.2f-f*6.0f;PlaceModel(lamp,g,"schoolyard lamp",lp,Yaw(f),4.6f,city);AddWarmLight(g,lp+Vector3.up*3.45f,8f,.85f,bulb,"yard warm light"); }

            // Faded four-square / play markings.
            Vector3 mark=yard+r*4.4f+f*3.0f;
            FlatBox("faded court line",g,mark,r,7.0f,.09f,.014f,paint);
            FlatBox("faded court line",g,mark,f,6.0f,.09f,.014f,paint);
            for(int s=-1;s<=1;s+=2) { FlatBox("faded court edge",g,mark+r*(s*3.45f),f,.09f,6.0f,.014f,paint); FlatBox("faded court edge",g,mark+f*(s*2.95f),r,7.0f,.09f,.014f,paint); }

            Material spruce=Mat("spruce",new Color(.055f,.135f,.075f),.03f), autumn=Mat("late_birch",new Color(.34f,.29f,.10f),.03f), shrub=Mat("shrub",new Color(.14f,.22f,.09f),.03f);
            Vector3[] trees={yard-r*10.2f+f*5.9f,yard-r*9.5f-f*5.8f,yard+r*10.0f-f*5.7f};
            GameObject[] src={fallTree,tallPine,pine};
            for(int i=0;i<trees.Length;i++)if(src[i]&&!InsideBuilding(trees[i],ways))PlaceModel(src[i],g,"schoolyard edge tree",trees[i],31+i*76,5.0f+i*.4f,i==0?autumn:spruce);
            if(bush)for(int i=0;i<4;i++)PlaceModel(bush,g,"schoolyard shrub",yard-r*10.0f+f*(-3f+i*2f),i*43,.62f,shrub);
        }

        static void BuildServiceArea(Transform parent, EntryInfo e, RosvikOsmV15.Way hall, List<RosvikOsmV15.Way> ways,
            Material asphalt, Material curb, Material metal, Material wood, Material container, Material city,
            GameObject dumpster, GameObject trashA, GameObject trashB, GameObject boxA, GameObject boxB,
            GameObject wagon, GameObject lamp, Material bulb) {
            Transform g=NewGroup(parent,"V43 SERVICE YARD - APOCALYPSE STORY");Vector3 r=e.right,f=e.forward;
            Vector3 c;
            if(hall!=null) {
                RosvikOsmV15.OBounds hb=RosvikOsmV15.Bounds(hall);Vector3 hc=hb.Center;Vector3 away=Flat(hc-RosvikOsmV15.Centroid(ways.First(w=>w.Id==MainSchoolWay))).normalized;if(away.sqrMagnitude<.1f)away=f;
                Vector3 tangent=new Vector3(away.z,0,-away.x);c=hc+away*(Mathf.Min(hb.Width,hb.Depth)*.5f+7f)+tangent*1.8f;
            } else c=e.wall+r*20f-f*3f;
            c.y=.045f;
            FlatBox("service asphalt",g,c,r,18f,11.5f,.07f,asphalt);AddCurbs(g,c,r,f,18.3f,11.8f,curb,true);

            Vector3 cont=c+r*5.8f+f*2.3f;
            Box("blue service container",g,cont+Vector3.up*1.28f,new Vector3(5.8f,2.55f,2.35f),RotY(Yaw(r)),container,false);
            for(int i=-4;i<=4;i++) Box("container rib",g,cont+r*(i*.58f)+f*1.19f+Vector3.up*1.28f,new Vector3(.055f,2.38f,.06f),Quaternion.identity,metal,false);
            Panel("container doors",g,cont-r*2.94f+Vector3.up*1.28f,-r,new Vector3(2.18f,2.25f,.07f),metal);

            // Mesh props become one readable service story rather than random litter.
            if(dumpster)PlaceModel(dumpster,g,"school dumpster",c-r*5.8f+f*2.8f,Yaw(f)+90f,1.35f,city);
            if(trashA)PlaceModel(trashA,g,"service bin A",c-r*4.2f+f*3.4f,Yaw(f),.82f,city);
            if(trashB)PlaceModel(trashB,g,"service bin B",c-r*3.4f+f*3.3f,Yaw(f),.82f,city);
            if(boxA)PlaceModel(boxA,g,"emergency supplies A",c-r*1.0f+f*3.3f,12f,.62f,city);
            if(boxB)PlaceModel(boxB,g,"emergency supplies B",c-r*.25f+f*3.0f,-9f,.48f,city);
            if(wagon)PlaceModel(wagon,g,"abandoned caretaker wagon",c-r*3.4f-f*2.6f,Yaw(r)+6f,1.45f,city);
            if(lamp){Vector3 lp=c+r*7.3f-f*4.1f;PlaceModel(lamp,g,"service lamp",lp,Yaw(-f),4.7f,city);AddWarmLight(g,lp+Vector3.up*3.5f,9f,1.1f,bulb,"service emergency light");}

            // Pallets and fallen notice board: restrained apocalypse, not Hollywood ruin.
            for(int y=0;y<2;y++)for(int i=-2;i<=2;i++)Box("pallet slat",g,c+r*(i*.42f)+f*.1f+Vector3.up*(.08f+y*.15f),new Vector3(.34f,.07f,1.45f),RotY(Yaw(r)),wood,false);
            Box("fallen notice board",g,c+r*2.2f-f*3.7f+Vector3.up*.10f,new Vector3(2.8f,.12f,1.25f),Quaternion.Euler(4f,Yaw(r)+15f,8f),wood,false);

            // Low service fence on the back edge.
            for(int i=-4;i<=4;i++) Box("service fence post",g,c+r*(i*2.0f)+f*5.3f+Vector3.up*.72f,new Vector3(.08f,1.42f,.08f),Quaternion.identity,metal,false);
            for(int y=0;y<3;y++) Box("service fence rail",g,c+f*5.3f+Vector3.up*(.35f+y*.42f),new Vector3(16.2f,.045f,.045f),RotY(Yaw(r)),metal,false);
        }

        static void BuildCampusVegetation(Transform parent, EntryInfo e, List<RosvikOsmV15.Way> ways,
            GameObject pine, GameObject tallPine, GameObject fallTree, GameObject bush, Material grass) {
            Transform g=NewGroup(parent,"V43 INTENTIONAL CAMPUS GREEN");Vector3 r=e.right,f=e.forward;
            Material spruce=Mat("spruce",new Color(.055f,.135f,.075f),.03f),autumn=Mat("late_birch",new Color(.34f,.29f,.10f),.03f),shrub=Mat("shrub",new Color(.14f,.22f,.09f),.03f);
            Vector3[] anchors={
                e.wall-r*25f+f*1f,e.wall-r*24f+f*16f,e.wall-r*17f+f*23f,
                e.wall+r*18f+f*22f,e.wall+r*25f+f*13f,e.wall+r*23f-f*2f,
                e.wall-r*5f+f*25f,e.wall+r*8f+f*27f,e.wall-r*29f+f*10f,e.wall+r*29f+f*7f
            };
            for(int i=0;i<anchors.Length;i++) {
                Vector3 p=anchors[i];p.y=.04f;if(InsideBuilding(p,ways))continue;
                GameObject src=(i%5==1&&fallTree)?fallTree:((i%3==0&&tallPine)?tallPine:pine);Material m=(i%5==1)?autumn:spruce;
                if(src)PlaceModel(src,g,"campus edge tree",p,17+i*49f,4.8f+(i%4)*.45f,m);
                if(bush){Vector3 bp=p+r*((i%2==0)?1.5f:-1.3f)+f*.7f;if(!InsideBuilding(bp,ways))PlaceModel(bush,g,"campus edge shrub",bp,33+i*28f,.55f+(i%3)*.10f,shrub);}
            }
        }

        static void BuildGroundStory(Transform parent, EntryInfo e, Material frost, Material wet) {
            Transform g=NewGroup(parent,"V43 WEATHER + WEAR");Vector3 r=e.right,f=e.forward;
            Patch("thin frost edge",g,e.wall-r*9f+f*8.5f,r,5.8f,1.5f,.024f,frost,23,4301);
            Patch("thin frost edge",g,e.wall+r*10f+f*11f,r,4.6f,1.25f,.024f,frost,22,4302);
            Patch("cold puddle",g,e.wall-r*2f+f*7f,r,2.7f,.75f,.031f,wet,20,4303);
            Patch("parking puddle",g,e.wall+r*10f+f*18f,r,3.2f,.85f,.031f,wet,20,4304);
            Patch("service thaw",g,e.wall+r*18f+f*4f,r,2.3f,.65f,.031f,wet,18,4305);
        }

        static void TuneMood() {
            RenderSettings.ambientMode=AmbientMode.Flat;
            RenderSettings.ambientLight=new Color(.31f,.30f,.265f);
            RenderSettings.fog=true;RenderSettings.fogColor=new Color(.38f,.385f,.365f);RenderSettings.fogDensity=.00135f;
            RenderSettings.reflectionIntensity=.55f;
            foreach(Light l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(x=>x.type==LightType.Directional)) {
                l.intensity=1.02f;l.color=new Color(1.0f,.80f,.59f);l.shadows=LightShadows.Soft;l.shadowStrength=.76f;
                l.transform.rotation=Quaternion.Euler(42f,-52f,0f);
            }
        }

        static void BikeRack(Transform p,Vector3 pos,Vector3 axis,Material mat) {
            Vector3 f=new Vector3(-axis.z,0,axis.x);Vector3 a=pos-f*.32f,b=pos+f*.32f;
            Cylinder("bike rack leg",p,a+Vector3.up*.42f,.045f,.84f,mat);Cylinder("bike rack leg",p,b+Vector3.up*.42f,.045f,.84f,mat);
            Box("bike rack top",p,pos+Vector3.up*.82f,new Vector3(.08f,.08f,.72f),RotY(Yaw(f)),mat,false);
        }

        static void PicnicTable(Transform p,Vector3 c,Vector3 axis,Material wood,Material metal) {
            Vector3 f=new Vector3(-axis.z,0,axis.x);
            Box("picnic tabletop",p,c+Vector3.up*.74f,new Vector3(2.8f,.16f,1.15f),RotY(Yaw(axis)),wood,false);
            Box("picnic bench",p,c+f*.96f+Vector3.up*.43f,new Vector3(2.8f,.13f,.42f),RotY(Yaw(axis)),wood,false);
            Box("picnic bench",p,c-f*.96f+Vector3.up*.43f,new Vector3(2.8f,.13f,.42f),RotY(Yaw(axis)),wood,false);
            for(int s=-1;s<=1;s+=2)for(int q=-1;q<=1;q+=2)Box("picnic leg",p,c+axis*(s*.9f)+f*(q*.36f)+Vector3.up*.34f,new Vector3(.12f,.68f,.12f),Quaternion.Euler(0,Yaw(axis),q*12f),metal,false);
        }

        static void Planter(Transform p,Vector3 c,Vector3 axis,float w,float d,Material rim,Material soil,GameObject bush) {
            Vector3 f=new Vector3(-axis.z,0,axis.x);FlatBox("planter soil",p,c,axis,w,d,.18f,soil);AddCurbs(p,c,axis,f,w+.22f,d+.22f,rim,false);
            if(bush)PlaceModel(bush,p,"planter shrub",c+Vector3.up*.16f,41f,.65f,Mat("shrub",new Color(.14f,.22f,.09f),.03f));
        }

        static void AddCurbs(Transform p,Vector3 c,Vector3 r,Vector3 f,float w,float d,Material mat,bool opening) {
            Box("curb",p,c+r*(w*.5f)+Vector3.up*.09f,new Vector3(.20f,.18f,d),RotY(Yaw(f)),mat,false);
            Box("curb",p,c-r*(w*.5f)+Vector3.up*.09f,new Vector3(.20f,.18f,d),RotY(Yaw(f)),mat,false);
            Box("curb",p,c+f*(d*.5f)+Vector3.up*.09f,new Vector3(w,.18f,.20f),RotY(Yaw(r)),mat,false);
            if(!opening)Box("curb",p,c-f*(d*.5f)+Vector3.up*.09f,new Vector3(w,.18f,.20f),RotY(Yaw(r)),mat,false);
        }

        static void Bar(Transform p,string n,Vector3 center,Vector3 direction,float thickness,float length,Material mat) {
            direction=direction.normalized;GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,true);g.transform.position=center;
            g.transform.rotation=Quaternion.FromToRotation(Vector3.up,direction);g.transform.localScale=new Vector3(thickness,length,thickness);g.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        }

        static void Cylinder(string n,Transform p,Vector3 pos,float radius,float height,Material mat) {
            GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=n;g.transform.SetParent(p,true);g.transform.position=pos;g.transform.localScale=new Vector3(radius,height*.5f,radius);g.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        }

        static void FlatBox(string n,Transform p,Vector3 c,Vector3 axis,float w,float d,float h,Material mat) {
            Box(n,p,new Vector3(c.x,h*.5f,c.z),new Vector3(w,h,d),RotY(Yaw(axis)),mat,false);
        }

        static GameObject Box(string n,Transform p,Vector3 pos,Vector3 scale,Quaternion rot,Material mat,bool col) {
            GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,true);g.transform.position=pos;g.transform.rotation=rot;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;if(!col)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;
        }

        static void Panel(string n,Transform p,Vector3 pos,Vector3 outward,Vector3 scale,Material mat) {
            GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,true);g.transform.position=pos;g.transform.rotation=Quaternion.LookRotation(outward,Vector3.up);g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        }

        static void AddWorldText(Transform p,string text,Vector3 pos,Vector3 outward,float charSize,Material mat) {
            GameObject go=new GameObject(text);go.transform.SetParent(p,true);go.transform.position=pos;go.transform.rotation=Quaternion.LookRotation(-outward,Vector3.up);
            TextMesh tm=go.AddComponent<TextMesh>();tm.text=text;tm.fontSize=64;tm.characterSize=charSize*.10f;tm.anchor=TextAnchor.MiddleLeft;tm.alignment=TextAlignment.Left;tm.color=Color.white;
            MeshRenderer mr=go.GetComponent<MeshRenderer>();if(mr&&mat)mr.sharedMaterial=mat;
        }

        static void AddWarmLight(Transform p,Vector3 pos,float range,float intensity,Material bulb,string name) {
            GameObject glow=new GameObject(name);glow.transform.SetParent(p,true);glow.transform.position=pos;Light l=glow.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(1f,.58f,.24f);l.range=range;l.intensity=intensity;l.shadows=LightShadows.Soft;
            GameObject orb=GameObject.CreatePrimitive(PrimitiveType.Sphere);orb.name="warm bulb";orb.transform.SetParent(glow.transform,false);orb.transform.localScale=Vector3.one*.12f;orb.GetComponent<Renderer>().sharedMaterial=bulb;UnityEngine.Object.DestroyImmediate(orb.GetComponent<Collider>());
        }

        static GameObject PlaceModel(GameObject asset,Transform parent,string name,Vector3 pos,float yaw,float targetHeight,Material mat) {
            if(!asset)return null;GameObject g=(GameObject)PrefabUtility.InstantiatePrefab(asset);if(!g)g=UnityEngine.Object.Instantiate(asset);g.name=name;g.transform.SetParent(parent,true);g.transform.position=pos;g.transform.rotation=Quaternion.Euler(0,yaw,0);g.transform.localScale=Vector3.one;
            Bounds b=BoundsOf(g);float scale=targetHeight/Mathf.Max(.01f,b.size.y);g.transform.localScale=Vector3.one*scale;Ground(g,.04f);foreach(Renderer r in g.GetComponentsInChildren<Renderer>(true)){r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;}foreach(Collider c in g.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);return g;
        }

        static void Ground(GameObject g,float y){Bounds b=BoundsOf(g);g.transform.position+=Vector3.up*(y-b.min.y);}
        static Bounds BoundsOf(GameObject g){Renderer[] rs=g.GetComponentsInChildren<Renderer>(true);if(rs.Length==0)return new Bounds(g.transform.position,Vector3.one);Bounds b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;}

        static void Patch(string name,Transform parent,Vector3 c,Vector3 axis,float length,float width,float y,Material mat,int segments,int seed) {
            axis=Flat(axis).normalized;if(axis.sqrMagnitude<.1f)axis=Vector3.right;Vector3 perp=new Vector3(-axis.z,0,axis.x);System.Random rng=new System.Random(seed);GameObject g=new GameObject(name);g.transform.SetParent(parent,false);Mesh mesh=new Mesh{name=name};
            Vector3[] v=new Vector3[segments+1];int[] tri=new int[segments*3];v[0]=new Vector3(c.x,y,c.z);for(int i=0;i<segments;i++){float a=Mathf.PI*2*i/segments;float wobble=.82f+(float)rng.NextDouble()*.28f;Vector3 q=c+axis*(Mathf.Cos(a)*length*.5f*wobble)+perp*(Mathf.Sin(a)*width*.5f*wobble);v[i+1]=new Vector3(q.x,y,q.z);int j=(i+1)%segments;tri[i*3]=0;tri[i*3+1]=i+1;tri[i*3+2]=j+1;}mesh.vertices=v;mesh.triangles=tri;mesh.RecalculateNormals();mesh.RecalculateBounds();g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=mat;
        }

        static Material Mat(string name,Color color,float smooth) {
            string path=GeneratedDir+"/mat_"+name+".mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(path);Shader s=ResolveShader();if(!m){m=new Material(s){name="V43 "+name};AssetDatabase.CreateAsset(m,path);}m.shader=s;m.color=color;if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color);if(m.HasProperty("_Color"))m.SetColor("_Color",color);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",smooth);if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",0f);EditorUtility.SetDirty(m);return m;
        }
        static Material TexturedMat(string name,Texture2D tex,float smooth){Material m=Mat(name,Color.white,smooth);if(tex){if(m.HasProperty("_BaseMap"))m.SetTexture("_BaseMap",tex);if(m.HasProperty("_MainTex"))m.SetTexture("_MainTex",tex);EditorUtility.SetDirty(m);}return m;}
        static Material EmissiveMat(string name,Color c,float mult){Material m=Mat(name,c,.32f);if(m.HasProperty("_EmissionColor")){m.SetColor("_EmissionColor",c*mult);m.EnableKeyword("_EMISSION");}EditorUtility.SetDirty(m);return m;}
        static Shader ResolveShader(){Material p=AssetDatabase.LoadAssetAtPath<Material>(ProvenMaterial);if(p&&p.shader&&p.shader.isSupported)return p.shader;Shader s=GraphicsSettings.defaultRenderPipeline!=null?Shader.Find("Universal Render Pipeline/Lit"):Shader.Find("Standard");if(!s||!s.isSupported)s=Shader.Find("Sprites/Default");return s;}

        static void SetActive(Transform root,string name,bool value){Transform t=Find(root,name);if(t)t.gameObject.SetActive(value);}
        static Transform NewGroup(Transform p,string name){GameObject g=new GameObject(name);g.transform.SetParent(p,false);return g.transform;}
        static Transform Find(Transform root,string name)=>root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name.Equals(name,StringComparison.OrdinalIgnoreCase));
        static Quaternion RotY(float yaw)=>Quaternion.Euler(0f,yaw,0f);
        static float Yaw(Vector3 d)=>Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;
        static Vector3 Flat(Vector3 v)=>new Vector3(v.x,0,v.z);
        static List<Vector3> Points(RosvikOsmV15.Way w){List<Vector3> p=w.Nodes.Select(n=>n.Pos).ToList();if(p.Count>2&&w.Closed&&Vector3.Distance(p[0],p[p.Count-1])<.01f)p.RemoveAt(p.Count-1);return p;}
        static float SignedArea(List<Vector3> p){float a=0;for(int i=0;i<p.Count;i++){Vector3 q=p[(i+1)%p.Count];a+=p[i].x*q.z-q.x*p[i].z;}return a*.5f;}
        static bool InsideBuilding(Vector3 p,List<RosvikOsmV15.Way> ways)=>ways.Any(w=>w.Closed&&!string.IsNullOrEmpty(w.Tag("building"))&&w.Tag("building")!="no"&&Inside(p,Points(w)));
        static bool Inside(Vector3 p,List<Vector3> poly){if(poly==null||poly.Count<3)return false;bool inside=false;for(int i=0,j=poly.Count-1;i<poly.Count;j=i++){Vector3 a=poly[i],b=poly[j];if(((a.z>p.z)!=(b.z>p.z))&&(p.x<(b.x-a.x)*(p.z-a.z)/(b.z-a.z+.000001f)+a.x))inside=!inside;}return inside;}
        static Vector3 ClosestPoint(Vector3 p,Vector3 a,Vector3 b){Vector3 ab=Flat(b-a);float d=ab.sqrMagnitude;if(d<.0001f)return a;float t=Mathf.Clamp01(Vector3.Dot(Flat(p-a),ab)/d);return a+ab*t;}
        static Vector3 ClosestPointOnPolygon(Vector3 p,List<Vector3> poly){Vector3 best=poly[0];float d=float.MaxValue;for(int i=0;i<poly.Count;i++){Vector3 q=ClosestPoint(p,poly[i],poly[(i+1)%poly.Count]);float dd=Flat(q-p).sqrMagnitude;if(dd<d){d=dd;best=q;}}return best;}
        static Vector3 FindClosestBoundaryPoint(RosvikOsmV15.Way w,Vector3 p)=>ClosestPointOnPolygon(p,Points(w));
        static float ClosestBoundaryPair(List<Vector3> a,List<Vector3> b,out Vector3 pa,out Vector3 pb){float best=float.MaxValue;pa=a[0];pb=b[0];for(int i=0;i<a.Count;i++){Vector3 aa=a[i],ab=a[(i+1)%a.Count];for(int j=0;j<b.Count;j++){Vector3 q=ClosestPoint(aa,b[j],b[(j+1)%b.Count]);float d=Flat(q-aa).sqrMagnitude;if(d<best){best=d;pa=aa;pb=q;}q=ClosestPoint(b[j],aa,ab);d=Flat(q-b[j]).sqrMagnitude;if(d<best){best=d;pa=q;pb=b[j];}}}return Mathf.Sqrt(best);}
    }
}
#endif