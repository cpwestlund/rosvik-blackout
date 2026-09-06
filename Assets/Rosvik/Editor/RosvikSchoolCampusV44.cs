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
    /// V44 is deliberately NOT a subtle prop pass. It puts a new visible architectural shell
    /// over Rosviks skola and the adjacent smaller sporthall, connects them with the corridor,
    /// and builds one coherent cozy arrival/schoolyard chunk around them.
    /// </summary>
    [InitializeOnLoad]
    public static class RosvikSchoolCampusV44 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_SCHOOL_CAMPUS_V44_VERSION";
        const int Version = 44;
        const string GroupName = "26 SCHOOL CAMPUS V44 - ARCHITECTURE REBUILD";
        const string GeneratedDir = "Assets/Rosvik/GeneratedV44";
        const string ProvenMaterial = "Assets/Rosvik/GeneratedV20/mat_ground.mat";
        const string CityRoot = "Assets/Rosvik/ThirdParty/V40CozyBits";
        const string NatureRoot = "Assets/Rosvik/ThirdParty/V22Models/KenneyNature";
        const long SchoolWay = 163199458;
        const long OldSchoolWay = 163199461;
        const long IceArenaWay = 163199454;

        static RosvikSchoolCampusV44(){ EditorApplication.update-=TryApply; EditorApplication.update+=TryApply; }

        [MenuItem("Rosvik/Rebuild School Campus V44 - ARCHITECTURE")]
        public static void Force(){ EditorPrefs.DeleteKey(Key); EditorApplication.update-=TryApply; EditorApplication.update+=TryApply; }

        static bool Busy()=>EditorApplication.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating;

        static void TryApply(){
            if(EditorPrefs.GetInt(Key,0)>=Version&&File.Exists(ScenePath)){EditorApplication.update-=TryApply;return;}
            if(Busy()||!File.Exists(ScenePath))return;
            UScene scene=EditorSceneManager.GetActiveScene(); GameObject root=FindRoot();
            if(!root){if(scene.path!=ScenePath)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);if(Busy())return;root=FindRoot();}
            if(!root||Busy())return; EditorApplication.update-=TryApply; Apply(scene,root);
        }

        static GameObject FindRoot(){
            return GameObject.Find("ROSVIK_HERO_COMPOSITION_V42")??GameObject.Find("ROSVIK_HERO_AREA_ASSETS_V41")??
                   GameObject.Find("ROSVIK_ASSET_COZY_APOCALYPSE_V40")??GameObject.Find("ROSVIK_CLEAN_ROAD_NETWORK_V39")??
                   UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None).FirstOrDefault(g=>g.name.StartsWith("ROSVIK_"));
        }

        static void Apply(UScene scene,GameObject root){
            try{
                Directory.CreateDirectory(GeneratedDir);AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                var ways=RosvikOsmV15.LoadWays();if(ways==null||ways.Count==0)throw new Exception("No Rosvik OSM data");
                var school=ways.FirstOrDefault(w=>w.Id==SchoolWay);if(school==null)throw new Exception("School footprint missing");
                var hall=FindAdjacentSportHall(ways,school);
                if(hall==null)throw new Exception("Could not find adjacent sporthall footprint");

                Transform old=Find(root.transform,GroupName);if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
                Transform g=NewGroup(root.transform,GroupName);
                Transform v43=Find(root.transform,"25 SCHOOL CAMPUS V43 - BIG PASS");if(v43)v43.gameObject.SetActive(false);

                Material schoolWall=Mat("school_cream",new Color(.66f,.60f,.49f),.08f);
                Material schoolBase=Mat("school_brick",new Color(.39f,.22f,.14f),.08f);
                Material roof=Mat("school_roof",new Color(.25f,.12f,.09f),.16f);
                Material hallWall=Mat("hall_dark",new Color(.145f,.17f,.165f),.13f);
                Material hallBase=Mat("hall_base",new Color(.29f,.22f,.17f),.10f);
                Material hallRoof=Mat("hall_roof",new Color(.08f,.095f,.095f),.18f);
                Material trim=Mat("trim",new Color(.78f,.76f,.67f),.18f);
                Material glass=Mat("glass",new Color(.055f,.12f,.145f),.55f);
                Material warm=Emissive("warm_glass",new Color(.95f,.52f,.19f),1.65f);
                Material asphalt=Mat("asphalt",new Color(.135f,.145f,.14f),.24f);
                Material paving=Mat("paving",new Color(.37f,.34f,.28f),.10f);
                Material grass=Mat("grass",new Color(.20f,.29f,.15f),.03f);
                Material wood=Mat("wood",new Color(.33f,.18f,.09f),.06f);
                Material metal=Mat("metal",new Color(.07f,.075f,.072f),.30f);
                Material paint=Mat("paint",new Color(.74f,.72f,.62f),.07f);
                Material frost=Mat("frost",new Color(.66f,.70f,.69f),.18f);
                Material puddle=Mat("puddle",new Color(.06f,.10f,.11f),.72f);
                Material bulb=Emissive("bulb",new Color(1f,.54f,.18f),2.6f);
                Material city=Textured("citybits",AssetDatabase.LoadAssetAtPath<Texture2D>(CityRoot+"/citybits_texture.png"),.18f);
                Material spruce=Mat("spruce",new Color(.055f,.14f,.075f),.03f);
                Material autumn=Mat("autumn",new Color(.36f,.30f,.10f),.03f);
                Material shrub=Mat("shrub",new Color(.14f,.22f,.09f),.03f);

                GameObject bench=AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/bench.obj");
                GameObject lamp=AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/streetlight.obj");
                GameObject sedan=AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/car_sedan.obj");
                GameObject wagon=AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/car_stationwagon.obj");
                GameObject dumpster=AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/dumpster.obj");
                GameObject boxA=AssetDatabase.LoadAssetAtPath<GameObject>(CityRoot+"/box_A.obj");
                GameObject pine=AssetDatabase.LoadAssetAtPath<GameObject>(NatureRoot+"/tree_pineDefaultA.obj");
                GameObject tallPine=AssetDatabase.LoadAssetAtPath<GameObject>(NatureRoot+"/tree_pineTallA.obj");
                GameObject fallTree=AssetDatabase.LoadAssetAtPath<GameObject>(NatureRoot+"/tree_default_fall.obj");
                GameObject bush=AssetDatabase.LoadAssetAtPath<GameObject>(NatureRoot+"/plant_bushDetailed.obj");

                BuildSchoolShell(g,school,schoolWall,schoolBase,roof,trim,glass,warm,wood,bulb);
                BuildHallShell(g,hall,hallWall,hallBase,hallRoof,trim,glass,warm,bulb);
                BuildConnector(g,school,hall,hallWall,trim,glass,warm,bulb);
                BuildCampusChunk(g,school,hall,asphalt,paving,grass,wood,metal,paint,frost,puddle,city,spruce,autumn,shrub,bulb,
                                 bench,lamp,sedan,wagon,dumpster,boxA,pine,tallPine,fallTree,bush);
                TuneMood();

                EditorPrefs.SetInt(Key,Version);Selection.activeObject=g.gameObject;EditorUtility.SetDirty(root);AssetDatabase.SaveAssets();
                if(!Busy()){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene,ScenePath);}                
                Debug.Log("ROSVIK V44: BIG visible architecture rebuild applied. school="+school.Id+", sporthall="+hall.Id+". V43 disabled.");
            }catch(Exception ex){Debug.LogError("ROSVIK V44 FAILED: "+ex);}
        }

        static RosvikOsmV15.Way FindAdjacentSportHall(List<RosvikOsmV15.Way> ways,RosvikOsmV15.Way school){
            Vector3 sc=RosvikOsmV15.Centroid(school);List<Vector3> sp=Pts(school);
            return ways.Where(w=>w.Closed&&w.Id!=SchoolWay&&w.Id!=OldSchoolWay&&w.Id!=IceArenaWay&&!string.IsNullOrEmpty(w.Tag("building"))&&w.Tag("building")!="no")
                .Select(w=>new{w,b=RosvikOsmV15.Bounds(w),d=Flat(RosvikOsmV15.Centroid(w)-sc).magnitude,gap=BoundaryGap(sp,Pts(w))})
                .Where(x=>x.d<75f&&x.gap<28f&&x.b.Width*x.b.Depth>160f&&Mathf.Max(x.b.Width,x.b.Depth)>14f)
                .OrderBy(x=>x.gap).ThenBy(x=>x.d).Select(x=>x.w).FirstOrDefault();
        }

        static void BuildSchoolShell(Transform p,RosvikOsmV15.Way w,Material wall,Material baseMat,Material roof,Material trim,Material glass,Material warm,Material wood,Material bulb){
            Transform g=NewGroup(p,"V44 ROSVIKS SKOLA - FULL SHELL");List<Vector3> pts=Pts(w);float area=Area(pts);int n=0;
            for(int i=0;i<pts.Count;i++){
                Vector3 a=pts[i],b=pts[(i+1)%pts.Count],edge=Flat(b-a);float len=edge.magnitude;if(len<1.4f)continue;edge/=len;Vector3 left=new Vector3(-edge.z,0,edge.x);Vector3 outv=area>0?-left:left;Vector3 mid=(a+b)*.5f;
                Panel("school wall",g,mid+outv*.12f+Vector3.up*1.75f,outv,new Vector3(len+.10f,3.40f,.22f),wall);
                Panel("brick plinth",g,mid+outv*.26f+Vector3.up*.38f,outv,new Vector3(len+.14f,.66f,.12f),baseMat);
                Panel("eave trim",g,mid+outv*.25f+Vector3.up*3.34f,outv,new Vector3(len+.15f,.18f,.15f),trim);
                int count=Mathf.Clamp(Mathf.FloorToInt(len/3.1f),1,10);
                for(int k=0;k<count;k++){
                    Vector3 q=Vector3.Lerp(a,b,(k+.5f)/count);Material gm=((n++%6)==2)?warm:glass;
                    Panel("window frame",g,q+outv*.27f+Vector3.up*1.82f,outv,new Vector3(1.55f,1.42f,.11f),trim);
                    Panel("window",g,q+outv*.34f+Vector3.up*1.82f,outv,new Vector3(1.30f,1.16f,.06f),gm);
                }
            }
            RosvikOsmV15.OBounds bnd=RosvikOsmV15.Bounds(w);Vector3 ax=Flat(bnd.AxisX).normalized;Vector3 perp=new Vector3(-ax.z,0,ax.x);float longSide=bnd.Width,depth=bnd.Depth;if(depth>longSide){float t=longSide;longSide=depth;depth=t;Vector3 tv=ax;ax=perp;perp=tv;}
            float yaw=Yaw(ax);float pitch=10f;float half=depth*.51f;
            Box("roof slope A",g,bnd.Center+perp*(depth*.245f)+Vector3.up*3.72f,new Vector3(longSide+1.1f,.20f,half+1.0f),Quaternion.Euler(pitch,yaw,0),roof,false);
            Box("roof slope B",g,bnd.Center-perp*(depth*.245f)+Vector3.up*3.72f,new Vector3(longSide+1.1f,.20f,half+1.0f),Quaternion.Euler(-pitch,yaw,0),roof,false);
            Vector3 entry=LongestEdgeMid(w,out Vector3 eright,out Vector3 forward);entry.y=.04f;
            Panel("entrance surround",g,entry+forward*.38f+Vector3.up*1.43f,forward,new Vector3(5.7f,2.75f,.28f),baseMat);
            Panel("entrance glass",g,entry+forward*.58f+Vector3.up*1.30f,forward,new Vector3(3.8f,2.30f,.08f),warm);
            Box("big timber canopy",g,entry+forward*1.75f+Vector3.up*2.92f,new Vector3(6.4f,.22f,3.1f),Rot(eright),wood,false);
            AddText(g,"ROSVIKS SKOLA",entry+forward*.70f-eright*3.05f+Vector3.up*3.18f,forward,.94f,trim);
            AddLight(g,entry+forward*2.35f+Vector3.up*2.55f,11f,1.65f,bulb);
        }

        static void BuildHallShell(Transform p,RosvikOsmV15.Way w,Material wall,Material baseMat,Material roof,Material trim,Material glass,Material warm,Material bulb){
            Transform g=NewGroup(p,"V44 ROSVIK SPORTHALL - FULL SHELL");List<Vector3> pts=Pts(w);float area=Area(pts);int idx=0;
            for(int i=0;i<pts.Count;i++){
                Vector3 a=pts[i],b=pts[(i+1)%pts.Count],edge=Flat(b-a);float len=edge.magnitude;if(len<1.5f)continue;edge/=len;Vector3 left=new Vector3(-edge.z,0,edge.x);Vector3 outv=area>0?-left:left;Vector3 mid=(a+b)*.5f;
                Panel("hall wall",g,mid+outv*.14f+Vector3.up*3.70f,outv,new Vector3(len+.10f,7.25f,.24f),wall);
                Panel("hall lower warm band",g,mid+outv*.28f+Vector3.up*.72f,outv,new Vector3(len+.14f,1.22f,.13f),baseMat);
                Panel("hall fascia",g,mid+outv*.27f+Vector3.up*7.18f,outv,new Vector3(len+.18f,.20f,.15f),trim);
                int count=Mathf.Clamp(Mathf.FloorToInt(len/5.2f),1,8);
                for(int k=0;k<count;k++){
                    Vector3 q=Vector3.Lerp(a,b,(k+.5f)/count);Material gm=((idx+k)%8==2)?warm:glass;
                    Panel("high hall window frame",g,q+outv*.30f+Vector3.up*4.75f,outv,new Vector3(1.85f,1.05f,.10f),trim);
                    Panel("high hall window",g,q+outv*.37f+Vector3.up*4.75f,outv,new Vector3(1.58f,.78f,.055f),gm);
                }idx++;
            }
            RosvikOsmV15.OBounds b=RosvikOsmV15.Bounds(w);Vector3 ax=Flat(b.AxisX).normalized;float yaw=Yaw(ax);
            Box("sporthall roof",g,b.Center+Vector3.up*7.36f,new Vector3(b.Width+1.0f,.28f,b.Depth+1.0f),Quaternion.Euler(0,yaw,0),roof,false);
            Vector3 signEdge=LongestEdgeMid(w,out Vector3 r,out Vector3 f);AddText(g,"ROSVIK SPORTHALL",signEdge+f*.38f-r*3.6f+Vector3.up*5.7f,f,1.02f,trim);AddLight(g,signEdge+f*.75f+Vector3.up*2.7f,9f,1.1f,bulb);
        }

        static void BuildConnector(Transform p,RosvikOsmV15.Way school,RosvikOsmV15.Way hall,Material wall,Material trim,Material glass,Material warm,Material bulb){
            Vector3 a,b;float gap=BoundaryPair(Pts(school),Pts(hall),out a,out b);if(gap>.2f&&gap<30f){
                Transform g=NewGroup(p,"V44 SCHOOL <-> SPORTHALL CORRIDOR");Vector3 d=Flat(b-a);float len=d.magnitude;d/=Mathf.Max(.001f,len);Vector3 side=new Vector3(-d.z,0,d.x);Vector3 c=(a+b)*.5f;
                Box("corridor floor",g,c+Vector3.up*.09f,new Vector3(3.55f,.18f,len+.8f),Rot(d),trim,false);
                Box("corridor roof",g,c+Vector3.up*3.12f,new Vector3(3.75f,.24f,len+1.0f),Rot(d),wall,false);
                for(int s=-1;s<=1;s+=2){Vector3 sideC=c+side*s*1.72f+Vector3.up*1.58f;Box("corridor glazing",g,sideC,new Vector3(.08f,2.60f,len+.35f),Rot(d),glass,false);}
                for(int k=0;k<=Mathf.Max(2,Mathf.RoundToInt(len/2.0f));k++){float t=k/(float)Mathf.Max(2,Mathf.RoundToInt(len/2.0f));for(int s=-1;s<=1;s+=2)Box("corridor mullion",g,Vector3.Lerp(a,b,t)+side*s*1.75f+Vector3.up*1.58f,new Vector3(.09f,2.70f,.09f),Quaternion.identity,trim,false);}
                Panel("warm corridor end",g,a+d*.15f+Vector3.up*1.58f,-d,new Vector3(3.15f,2.55f,.08f),warm);AddLight(g,c+Vector3.up*2.45f,8f,1.0f,bulb);
            }
        }

        static void BuildCampusChunk(Transform p,RosvikOsmV15.Way school,RosvikOsmV15.Way hall,Material asphalt,Material paving,Material grass,Material wood,Material metal,Material paint,Material frost,Material puddle,Material city,Material spruce,Material autumn,Material shrub,Material bulb,GameObject bench,GameObject lamp,GameObject sedan,GameObject wagon,GameObject dumpster,GameObject boxA,GameObject pine,GameObject tallPine,GameObject fallTree,GameObject bush){
            Transform g=NewGroup(p,"V44 COZY CAMPUS CHUNK");Vector3 sc=RosvikOsmV15.Centroid(school),hc=RosvikOsmV15.Centroid(hall);Vector3 toHall=Flat(hc-sc).normalized;if(toHall.sqrMagnitude<.1f)toHall=Vector3.right;Vector3 side=new Vector3(-toHall.z,0,toHall.x);
            Vector3 entry=LongestEdgeMid(school,out Vector3 er,out Vector3 ef); // existing geometry picks the visually strongest facade
            // Put arrival on the outside of the school, schoolyard to one side, service beside the hall.
            Vector3 arrival=entry+ef*10f;arrival.y=.04f;Flat("V44 entrance forecourt",g,arrival,er,22f,14f,.08f,paving);Curbs(g,arrival,er,ef,22.4f,14.4f,metal);
            Vector3 parking=arrival+ef*14.8f;Flat("V44 parking dropoff",g,parking,er,31f,14f,.07f,asphalt);CurbsOpen(g,parking,er,ef,31.4f,14.4f,metal);
            for(int i=-4;i<=4;i++){Flat("parking stripe",g,parking+er*(i*3.0f)+ef*3.8f,ef,.10f,5.2f,.018f,paint);}
            for(int i=-3;i<=3;i++)Flat("zebra stripe",g,arrival+ef*5.7f+er*(i*.58f),er,.34f,4.2f,.019f,paint);
            if(sedan)Place(sedan,g,"staff sedan",parking-er*8.8f+ef*2.9f,Yaw(ef),1.42f,city);if(wagon)Place(wagon,g,"caretaker wagon",parking+er*10.3f-ef*2.5f,Yaw(-ef)+4f,1.46f,city);
            if(bench){Place(bench,g,"entrance bench",arrival-er*7.8f-ef*3.6f,Yaw(er),.90f,city);Place(bench,g,"entrance bench",arrival+er*7.8f-ef*3.6f,Yaw(-er),.90f,city);}
            // Bicycle racks and planter island
            Vector3 rack=arrival-er*8.6f+ef*1.9f;for(int i=0;i<8;i++)BikeRack(g,rack+ef*(i*.65f-2.25f),er,metal);
            Vector3 island=parking+er*1.8f;Flat("green parking island",g,island,er,3.5f,10.5f,.16f,grass);Curbs(g,island,er,ef,3.8f,10.8f,metal);

            Vector3 yard=sc-side*20f-ef*4f;yard.y=.04f;Flat("schoolyard",g,yard,side,26f,20f,.07f,grass);Curbs(g,yard,side,toHall,26.4f,20.4f,metal);
            // play markings + picnic table + simple swing structure
            for(int s=-1;s<=1;s+=2){Flat("court side",g,yard+side*s*5f,side,.10f,9f,.018f,paint);Flat("court end",g,yard+toHall*s*4.5f,toHall,.10f,10f,.018f,paint);}Flat("court middle",g,yard,side,.10f,9f,.018f,paint);
            Picnic(g,yard+side*8f-toHall*5f,side,wood,metal);Swing(g,yard-side*7f+toHall*4f,side,wood,metal);
            if(bench)Place(bench,g,"yard bench",yard+side*9f+toHall*6.5f,Yaw(-side),.90f,city);

            Vector3 service=hc+toHall*16f;service.y=.04f;Flat("sporthall service yard",g,service,side,18f,12f,.07f,asphalt);CurbsOpen(g,service,side,toHall,18.4f,12.4f,metal);
            if(dumpster)Place(dumpster,g,"service dumpster",service-side*5.8f+toHall*3f,Yaw(side),1.34f,city);if(boxA)Place(boxA,g,"service supplies",service-side*3.7f+toHall*3.1f,14f,.60f,city);
            Box("service container",g,service+side*5.0f+Vector3.up*1.25f,new Vector3(5.5f,2.5f,2.35f),Rot(side),Mat("container",new Color(.15f,.25f,.28f),.12f),false);

            Vector3[] lamps={arrival-er*9f-ef*4.8f,arrival+er*9f-ef*4.8f,parking-er*13.5f,parking+er*13.5f,yard-side*11f,service+side*7f-toHall*4.8f};
            if(lamp)for(int i=0;i<lamps.Length;i++){Place(lamp,g,"campus lamp",lamps[i],Yaw(ef),4.7f,city);if(i<4||i==5)AddLight(g,lamps[i]+Vector3.up*3.5f,8.5f,1.0f,bulb);}
            // deliberate vegetation frame
            Vector3[] treePos={arrival-er*11f+ef*4f,arrival+er*11f+ef*4f,parking-er*14f-ef*5f,parking+er*14f-ef*5f,yard-side*12f+toHall*7f,yard-side*11f-toHall*7f,yard+side*12f-toHall*6f,service+side*8f+toHall*5f};
            GameObject[] trees={fallTree,pine,tallPine,pine,tallPine,fallTree,pine,tallPine};Material[] mats={autumn,spruce,spruce,spruce,spruce,autumn,spruce,spruce};
            for(int i=0;i<treePos.Length;i++)if(trees[i])Place(trees[i],g,"campus tree",treePos[i],31+i*47,4.9f+(i%3)*.5f,mats[i]);
            if(bush)for(int i=-3;i<=3;i++)Place(bush,g,"campus shrub",arrival+er*i*2.0f-ef*5.4f,i*39,.58f+(Mathf.Abs(i)%2)*.12f,shrub);
            Patch("frost edge",g,arrival+er*6f+ef*2f,er,5.5f,1.5f,.025f,frost,22,4401);Patch("puddle",g,parking-er*4f-ef*1f,er,3.1f,.8f,.031f,puddle,20,4402);Patch("service thaw",g,service+side*2f-toHall*2f,side,2.5f,.7f,.031f,puddle,18,4403);
        }

        static Vector3 LongestEdgeMid(RosvikOsmV15.Way w,out Vector3 right,out Vector3 forward){List<Vector3> p=Pts(w);float area=Area(p),best=0;int bi=0;for(int i=0;i<p.Count;i++){float l=Flat(p[(i+1)%p.Count]-p[i]).magnitude;if(l>best){best=l;bi=i;}}Vector3 a=p[bi],b=p[(bi+1)%p.Count];right=Flat(b-a).normalized;Vector3 left=new Vector3(-right.z,0,right.x);forward=area>0?-left:left;return (a+b)*.5f;}
        static void BikeRack(Transform p,Vector3 c,Vector3 ax,Material m){Vector3 f=new Vector3(-ax.z,0,ax.x);Box("rack leg",p,c-f*.30f+Vector3.up*.42f,new Vector3(.07f,.84f,.07f),Quaternion.identity,m,false);Box("rack leg",p,c+f*.30f+Vector3.up*.42f,new Vector3(.07f,.84f,.07f),Quaternion.identity,m,false);Box("rack top",p,c+Vector3.up*.82f,new Vector3(.07f,.07f,.68f),Rot(f),m,false);}
        static void Picnic(Transform p,Vector3 c,Vector3 ax,Material wood,Material metal){Vector3 f=new Vector3(-ax.z,0,ax.x);Box("picnic top",p,c+Vector3.up*.72f,new Vector3(2.8f,.16f,1.1f),Rot(ax),wood,false);Box("picnic seat",p,c+f*.92f+Vector3.up*.42f,new Vector3(2.8f,.13f,.38f),Rot(ax),wood,false);Box("picnic seat",p,c-f*.92f+Vector3.up*.42f,new Vector3(2.8f,.13f,.38f),Rot(ax),wood,false);for(int s=-1;s<=1;s+=2)for(int q=-1;q<=1;q+=2)Box("picnic leg",p,c+ax*s*.9f+f*q*.35f+Vector3.up*.34f,new Vector3(.12f,.68f,.12f),Quaternion.identity,metal,false);}
        static void Swing(Transform p,Vector3 c,Vector3 ax,Material wood,Material metal){Vector3 f=new Vector3(-ax.z,0,ax.x);for(int s=-1;s<=1;s+=2){Vector3 e=c+ax*s*2.3f;Box("swing post",p,e-f*.55f+Vector3.up*1.35f,new Vector3(.16f,2.7f,.16f),Quaternion.Euler(0,Yaw(ax),12f),wood,false);Box("swing post",p,e+f*.55f+Vector3.up*1.35f,new Vector3(.16f,2.7f,.16f),Quaternion.Euler(0,Yaw(ax),-12f),wood,false);}Box("swing beam",p,c+Vector3.up*2.62f,new Vector3(5.4f,.18f,.18f),Rot(ax),wood,false);for(int s=-1;s<=1;s+=2){Vector3 seat=c+ax*s*.85f+Vector3.up*.62f;Box("swing rope",p,seat-ax*.25f+Vector3.up*.85f,new Vector3(.025f,1.7f,.025f),Quaternion.identity,metal,false);Box("swing rope",p,seat+ax*.25f+Vector3.up*.85f,new Vector3(.025f,1.7f,.025f),Quaternion.identity,metal,false);Box("swing seat",p,seat,new Vector3(.65f,.08f,.30f),Rot(ax),wood,false);}}
        static void Curbs(Transform p,Vector3 c,Vector3 r,Vector3 f,float w,float d,Material m){Box("curb",p,c+r*w*.5f+Vector3.up*.09f,new Vector3(.18f,.18f,d),Rot(f),m,false);Box("curb",p,c-r*w*.5f+Vector3.up*.09f,new Vector3(.18f,.18f,d),Rot(f),m,false);Box("curb",p,c+f*d*.5f+Vector3.up*.09f,new Vector3(w,.18f,.18f),Rot(r),m,false);Box("curb",p,c-f*d*.5f+Vector3.up*.09f,new Vector3(w,.18f,.18f),Rot(r),m,false);}
        static void CurbsOpen(Transform p,Vector3 c,Vector3 r,Vector3 f,float w,float d,Material m){Box("curb",p,c+r*w*.5f+Vector3.up*.09f,new Vector3(.18f,.18f,d),Rot(f),m,false);Box("curb",p,c-r*w*.5f+Vector3.up*.09f,new Vector3(.18f,.18f,d),Rot(f),m,false);Box("curb",p,c+f*d*.5f+Vector3.up*.09f,new Vector3(w,.18f,.18f),Rot(r),m,false);}
        static void Flat(string n,Transform p,Vector3 c,Vector3 ax,float w,float d,float h,Material m){Box(n,p,new Vector3(c.x,h*.5f,c.z),new Vector3(w,h,d),Rot(ax),m,false);}
        static GameObject Box(string n,Transform p,Vector3 pos,Vector3 scale,Quaternion rot,Material m,bool col){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,true);g.transform.position=pos;g.transform.rotation=rot;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=m;if(!col)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
        static void Panel(string n,Transform p,Vector3 pos,Vector3 outv,Vector3 scale,Material m){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,true);g.transform.position=pos;g.transform.rotation=Quaternion.LookRotation(outv,Vector3.up);g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());}
        static GameObject Place(GameObject a,Transform p,string n,Vector3 pos,float yaw,float targetH,Material m){if(!a)return null;GameObject g=(GameObject)PrefabUtility.InstantiatePrefab(a);if(!g)g=UnityEngine.Object.Instantiate(a);g.name=n;g.transform.SetParent(p,true);g.transform.position=pos;g.transform.rotation=Quaternion.Euler(0,yaw,0);g.transform.localScale=Vector3.one;Bounds b=Bounds(g);float s=targetH/Mathf.Max(.01f,b.size.y);g.transform.localScale=Vector3.one*s;b=Bounds(g);g.transform.position+=Vector3.up*(.04f-b.min.y);foreach(Renderer r in g.GetComponentsInChildren<Renderer>(true)){r.sharedMaterial=m;r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;}foreach(Collider c in g.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);return g;}
        static Bounds Bounds(GameObject g){Renderer[] rs=g.GetComponentsInChildren<Renderer>(true);if(rs.Length==0)return new Bounds(g.transform.position,Vector3.one);Bounds b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;}
        static void AddLight(Transform p,Vector3 pos,float range,float intensity,Material bulb){GameObject g=new GameObject("warm campus light");g.transform.SetParent(p,true);g.transform.position=pos;Light l=g.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(1f,.58f,.24f);l.range=range;l.intensity=intensity;l.shadows=LightShadows.Soft;GameObject orb=GameObject.CreatePrimitive(PrimitiveType.Sphere);orb.name="bulb";orb.transform.SetParent(g.transform,false);orb.transform.localScale=Vector3.one*.12f;orb.GetComponent<Renderer>().sharedMaterial=bulb;UnityEngine.Object.DestroyImmediate(orb.GetComponent<Collider>());}
        static void AddText(Transform p,string text,Vector3 pos,Vector3 outv,float size,Material mat){GameObject g=new GameObject(text);g.transform.SetParent(p,true);g.transform.position=pos;g.transform.rotation=Quaternion.LookRotation(-outv,Vector3.up);TextMesh tm=g.AddComponent<TextMesh>();tm.text=text;tm.fontSize=64;tm.characterSize=size*.10f;tm.anchor=TextAnchor.MiddleLeft;tm.color=Color.white;MeshRenderer mr=g.GetComponent<MeshRenderer>();if(mr)mr.sharedMaterial=mat;}
        static void Patch(string n,Transform p,Vector3 c,Vector3 ax,float length,float width,float y,Material m,int seg,int seed){ax=Flat(ax).normalized;Vector3 per=new Vector3(-ax.z,0,ax.x);System.Random rng=new System.Random(seed);GameObject g=new GameObject(n);g.transform.SetParent(p,false);Mesh mesh=new Mesh{name=n};Vector3[] v=new Vector3[seg+1];int[] t=new int[seg*3];v[0]=new Vector3(c.x,y,c.z);for(int i=0;i<seg;i++){float a=Mathf.PI*2*i/seg,w=.82f+(float)rng.NextDouble()*.28f;Vector3 q=c+ax*(Mathf.Cos(a)*length*.5f*w)+per*(Mathf.Sin(a)*width*.5f*w);v[i+1]=new Vector3(q.x,y,q.z);int j=(i+1)%seg;t[i*3]=0;t[i*3+1]=i+1;t[i*3+2]=j+1;}mesh.vertices=v;mesh.triangles=t;mesh.RecalculateNormals();g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=m;}
        static Material Mat(string n,Color c,float sm){string path=GeneratedDir+"/mat_"+n+".mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(path);Shader s=ShaderResolve();if(!m){m=new Material(s){name="V44 "+n};AssetDatabase.CreateAsset(m,path);}m.shader=s;m.color=c;if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);if(m.HasProperty("_Color"))m.SetColor("_Color",c);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",sm);if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",sm);if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",0);EditorUtility.SetDirty(m);return m;}
        static Material Textured(string n,Texture2D tex,float sm){Material m=Mat(n,Color.white,sm);if(tex){if(m.HasProperty("_BaseMap"))m.SetTexture("_BaseMap",tex);if(m.HasProperty("_MainTex"))m.SetTexture("_MainTex",tex);}return m;}
        static Material Emissive(string n,Color c,float mult){Material m=Mat(n,c,.30f);if(m.HasProperty("_EmissionColor")){m.SetColor("_EmissionColor",c*mult);m.EnableKeyword("_EMISSION");}return m;}
        static Shader ShaderResolve(){Material p=AssetDatabase.LoadAssetAtPath<Material>(ProvenMaterial);if(p&&p.shader&&p.shader.isSupported)return p.shader;Shader s=GraphicsSettings.defaultRenderPipeline!=null?Shader.Find("Universal Render Pipeline/Lit"):Shader.Find("Standard");if(!s||!s.isSupported)s=Shader.Find("Sprites/Default");return s;}
        static void TuneMood(){RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.32f,.30f,.255f);RenderSettings.fog=true;RenderSettings.fogColor=new Color(.39f,.39f,.36f);RenderSettings.fogDensity=.0012f;RenderSettings.reflectionIntensity=.55f;foreach(Light l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.type==LightType.Directional)){l.intensity=1.08f;l.color=new Color(1f,.80f,.60f);l.transform.rotation=Quaternion.Euler(40f,-52f,0);l.shadows=LightShadows.Soft;l.shadowStrength=.76f;}}
        static List<Vector3> Pts(RosvikOsmV15.Way w){List<Vector3> p=w.Nodes.Select(n=>n.Pos).ToList();if(p.Count>2&&w.Closed&&Vector3.Distance(p[0],p[p.Count-1])<.01f)p.RemoveAt(p.Count-1);return p;}
        static float Area(List<Vector3> p){float a=0;for(int i=0;i<p.Count;i++){Vector3 q=p[(i+1)%p.Count];a+=p[i].x*q.z-q.x*p[i].z;}return a*.5f;}
        static float BoundaryGap(List<Vector3>a,List<Vector3>b){Vector3 x,y;return BoundaryPair(a,b,out x,out y);}
        static float BoundaryPair(List<Vector3>a,List<Vector3>b,out Vector3 pa,out Vector3 pb){float best=float.MaxValue;pa=a[0];pb=b[0];for(int i=0;i<a.Count;i++)for(int j=0;j<b.Count;j++){Vector3 q=Closest(a[i],b[j],b[(j+1)%b.Count]);float d=Flat(q-a[i]).sqrMagnitude;if(d<best){best=d;pa=a[i];pb=q;}q=Closest(b[j],a[i],a[(i+1)%a.Count]);d=Flat(q-b[j]).sqrMagnitude;if(d<best){best=d;pa=q;pb=b[j];}}return Mathf.Sqrt(best);}
        static Vector3 Closest(Vector3 p,Vector3 a,Vector3 b){Vector3 ab=Flat(b-a);float d=ab.sqrMagnitude;if(d<.0001f)return a;float t=Mathf.Clamp01(Vector3.Dot(Flat(p-a),ab)/d);return a+ab*t;}
        static Vector3 Flat(Vector3 v)=>new Vector3(v.x,0,v.z);static float Yaw(Vector3 d)=>Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;static Quaternion Rot(Vector3 d)=>Quaternion.Euler(0,Yaw(d),0);static Transform NewGroup(Transform p,string n){GameObject g=new GameObject(n);g.transform.SetParent(p,false);return g.transform;}static Transform Find(Transform r,string n)=>r.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name.Equals(n,StringComparison.OrdinalIgnoreCase));
    }
}
#endif