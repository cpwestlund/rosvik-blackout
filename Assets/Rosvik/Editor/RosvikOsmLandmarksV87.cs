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
    public static class RosvikOsmLandmarksV87 {
        const int Version = 87;
        const string Key = "ROSVIK_OSM_LANDMARKS_V87";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string V86RootName = "V86 OSM WORLD REBUILD";
        const string CalName = "V87 OSM MAP CALIBRATION";
        const string HeroName = "V87 ROSVIK LANDMARK REBUILD";
        const string Generated = "Assets/Rosvik/GeneratedV87";
        const float V86Scale = .72f;
        const float V86ZOffset = 7.3f;

        // Curated OSM footprints from the same Rosvik snapshot used by the Astra reference.
        // They are OSM-derived local metres after the 33-degree map rotation.
        static readonly Vector2[] SchoolOsm = {
            new Vector2(11.314f,57.640f),new Vector2(-19.657f,57.597f),new Vector2(-19.573f,-4.455f),
            new Vector2(-9.414f,-4.441f),new Vector2(-9.400f,-15.157f),new Vector2(-17.374f,-15.159f),
            new Vector2(-17.327f,-47.183f),new Vector2(30.134f,-47.116f),new Vector2(30.090f,-15.276f),
            new Vector2(-.573f,-15.318f),new Vector2(-.583f,-4.414f),new Vector2(11.400f,-4.397f)
        };
        static readonly Vector2[] ArenaOsm = {
            new Vector2(61.142f,85.844f),new Vector2(25.521f,86.152f),new Vector2(24.894f,12.224f),
            new Vector2(60.512f,11.914f),new Vector2(60.636f,26.197f)
        };
        static readonly Vector2[] OldSchoolOsm = {
            new Vector2(-27.894f,-121.581f),new Vector2(10.590f,-119.326f),new Vector2(9.717f,-104.509f),new Vector2(-28.763f,-106.761f)
        };

        static readonly Vector3 SchoolTarget = new Vector3(0f,0f,7.35f);
        static readonly Vector3 ArenaTarget = new Vector3(34f,0f,7f);

        static Shader shader;
        static Material snow, packedSnow, ochre, brick, blue, cream, trim, glass, warmGlass, roof, metal, ice, rinkBlue, rinkRed, rinkLine, wood, floorGreen, floorWarm, wallWarm;
        static float worldScale;
        static float worldYaw;
        static Vector3 rawSchool;

        static RosvikOsmLandmarksV87() {
            if (EditorPrefs.GetInt(Key,0) >= Version) return;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V87 OSM ALIGN + SCHOOL + ICE ARENA REBUILD")]
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
            catch (Exception ex) { Debug.LogError("V87 FAILED: " + ex); }
        }

        static void Apply() {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject v86 = GameObject.Find(V86RootName);
            if (!v86) throw new Exception("V86 OSM world is missing. Run V86 first.");
            CoziPlayerV57 player = UnityEngine.Object.FindFirstObjectByType<CoziPlayerV57>();
            if (!player) throw new Exception("PLAYER missing");

            Directory.CreateDirectory(Generated);
            shader = PickShader();
            BuildMaterials();
            ComputeCalibration();
            CalibrateOsmWorld(v86.transform);

            GameObject oldHero = GameObject.Find(HeroName);
            if (oldHero) UnityEngine.Object.DestroyImmediate(oldHero);
            Transform v86Hero = v86.transform.Find("06 AUTHORED CORE EXTERIORS");
            if (v86Hero) UnityEngine.Object.DestroyImmediate(v86Hero.gameObject);

            GameObject hero = new GameObject(HeroName);
            Transform exteriors = Group(hero.transform,"01 EXTERIORS");
            Transform interiors = Group(hero.transform,"02 NEW CORE INTERIORS");
            Transform grounds = Group(hero.transform,"03 SCHOOL GROUNDS DETAILS");

            Vector3[] school = SchoolOsm.Select(Calibrated).ToArray();
            Vector3[] arena = ArenaOsm.Select(Calibrated).ToArray();
            Vector3[] oldSchool = OldSchoolOsm.Select(Calibrated).ToArray();

            GameObject schoolShell = BuildSchoolFromFootprint(exteriors, school);
            GameObject arenaShell = BuildIceArenaFromFootprint(exteriors, arena);
            BuildOldSchool(exteriors, oldSchool);
            BuildSchoolGrounds(grounds, school, arena);
            RebuildSchoolInteriorArchitecture(interiors);
            RebuildIceRinkInterior(interiors);
            CleanLegacyCoreRenderers();

            OsmWorldRuntimeV86 runtime = v86.GetComponent<OsmWorldRuntimeV86>();
            if (!runtime) runtime = v86.AddComponent<OsmWorldRuntimeV86>();
            runtime.attribution = "© OpenStreetMap contributors · ODbL 1.0";
            Bounds sb = BoundsOf(school); Bounds ab = BoundsOf(arena);
            runtime.cutaways = new [] {
                new CutawayTargetV86 { visualRoot=schoolShell, minXZ=new Vector2(-18.7f,-1.5f), maxXZ=new Vector2(18.7f,16.1f), padding=.75f },
                new CutawayTargetV86 { visualRoot=arenaShell, minXZ=new Vector2(22.8f,-2.7f), maxXZ=new Vector2(45.2f,16.8f), padding=.85f }
            };
            EditorUtility.SetDirty(runtime);

            // A clean school entry should be the visual anchor at the start of the game.
            player.SetObjective("Utforska Rosvik. Skolan och Norrbotten Stål Arena ligger nu efter den riktiga kartstrukturen.");
            EditorUtility.SetDirty(player);

            CleanupMissingScripts();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorPrefs.SetInt(Key,Version);
            SceneView.RepaintAll();
            Debug.Log("V87 COMPLETE — OSM map calibrated to the playable core, school rebuilt from OSM footprint, and sport hall converted visually into an ice-hockey arena while systems remain intact.");
        }

        static void ComputeCalibration() {
            rawSchool = RawMap(Centroid(SchoolOsm));
            Vector3 rawArena = RawMap(Centroid(ArenaOsm));
            Vector3 rv = rawArena - rawSchool; rv.y = 0f;
            Vector3 tv = ArenaTarget - SchoolTarget; tv.y = 0f;
            float rl = new Vector2(rv.x,rv.z).magnitude;
            float tl = new Vector2(tv.x,tv.z).magnitude;
            worldScale = Mathf.Clamp(tl / Mathf.Max(.01f,rl), .55f, 1.35f);
            float ra = Mathf.Atan2(rv.z,rv.x) * Mathf.Rad2Deg;
            float ta = Mathf.Atan2(tv.z,tv.x) * Mathf.Rad2Deg;
            worldYaw = ta - ra;
        }

        static void CalibrateOsmWorld(Transform v86) {
            Transform cal = v86.Find(CalName);
            if (!cal) {
                cal = Group(v86,CalName);
                cal.position = rawSchool;
                string[] names = {"01 CONTINUOUS WINTER GROUND","02 OPENSTREETMAP ROADS","03 OPENSTREETMAP BUILDINGS","04 SCHOOL + SPORTS GROUNDS","05 VEGETATION","07 WORLD DETAILS"};
                foreach (string n in names) {
                    Transform t = v86.Find(n);
                    if (t) t.SetParent(cal,true);
                }
                cal.position = SchoolTarget;
                cal.rotation = Quaternion.Euler(0,worldYaw,0);
                cal.localScale = new Vector3(worldScale,1f,worldScale);
            }
        }

        static Vector3 Calibrated(Vector2 p) {
            Vector3 raw = RawMap(p);
            Vector3 d = raw - rawSchool;
            Vector3 r = Quaternion.Euler(0,worldYaw,0) * d;
            return SchoolTarget + new Vector3(r.x*worldScale, raw.y, r.z*worldScale);
        }
        static Vector3 RawMap(Vector2 p) => new Vector3(p.x*V86Scale,0f,p.y*V86Scale+V86ZOffset);
        static Vector2 Centroid(IEnumerable<Vector2> pts) { float x=0,z=0; int n=0; foreach(Vector2 p in pts){x+=p.x;z+=p.y;n++;} return n==0?Vector2.zero:new Vector2(x/n,z/n); }

        static GameObject BuildSchoolFromFootprint(Transform parent,Vector3[] pts) {
            GameObject root = new GameObject("ROSVIKS SKOLA · OSM FOOTPRINT V87"); root.transform.SetParent(parent,true);
            float h=3.15f;
            BuildPolygonWalls(root.transform,pts,h,ochre,true,true);
            BuildPolygonRoof(root.transform,pts,h+.03f,roof,snow,"school roof");
            EdgeInfo front = FrontEdge(pts);
            AddSchoolEntrance(root.transform,front,h);
            AddFacadeWindows(root.transform,pts,h,true);
            // Warm windows are sparse because the power is unstable, not every room is lit.
            for(int i=0;i<pts.Length;i+=3) AddWarmWindowNearEdge(root.transform,pts,i,h);
            return root;
        }

        static GameObject BuildIceArenaFromFootprint(Transform parent,Vector3[] pts) {
            GameObject root = new GameObject("NORRBOTTEN STÅL ARENA · OSM FOOTPRINT V87"); root.transform.SetParent(parent,true);
            float h=5.0f;
            BuildPolygonWalls(root.transform,pts,h,blue,true,true);
            BuildPolygonRoof(root.transform,pts,h+.03f,roof,snow,"arena roof");
            EdgeInfo front = FrontEdge(pts);
            Vector3 mid=front.mid, dir=front.dir;
            float yaw=Mathf.Atan2(dir.x,dir.z)*Mathf.Rad2Deg;
            Box("arena service door",root.transform,mid+Vector3.up*1.45f,new Vector3(.20f,2.75f,4.6f),metal,false,Quaternion.Euler(0,yaw,0));
            Box("arena entrance canopy",root.transform,mid+front.outward*.75f+Vector3.up*3.55f,new Vector3(2.0f,.18f,6.2f),roof,false,Quaternion.Euler(0,yaw,0));
            for(int i=0;i<pts.Length;i++) AddArenaHighWindows(root.transform,pts,i,h);
            WorldLabel(root.transform,"NORRBOTTEN STÅL ARENA",mid+front.outward*.14f+Vector3.up*3.75f,yaw,.16f);
            return root;
        }

        static void BuildOldSchool(Transform parent,Vector3[] pts) {
            GameObject root=new GameObject("STENSKOLAN · OSM 163199461");root.transform.SetParent(parent,true);
            float h=2.75f;BuildPolygonWalls(root.transform,pts,h,cream,true,true);BuildPolygonRoof(root.transform,pts,h+.03f,roof,snow,"stenskolan roof");AddFacadeWindows(root.transform,pts,h,false);
        }

        static void BuildSchoolGrounds(Transform p,Vector3[] school,Vector3[] arena) {
            Bounds s=BoundsOf(school),a=BoundsOf(arena);
            // Make the immediate campus feel authored while the surrounding geometry still comes from OSM.
            Box("school forecourt",p,new Vector3(0,-.018f,-5.4f),new Vector3(32f,.035f,7.5f),packedSnow,false);
            for(int i=-2;i<=2;i++) LampPost(p,new Vector3(i*6.2f,0,-8.8f+(Mathf.Abs(i)%2)*.25f));
            for(int i=0;i<5;i++) {
                Box("bike rack",p,new Vector3(-12f+i*.72f,.30f,-3.6f),new Vector3(.05f,.60f,.72f),metal,false,Quaternion.Euler(0,0,18f));
            }
            Box("arena service apron",p,new Vector3(34f,-.02f,-4.7f),new Vector3(19f,.035f,5.8f),packedSnow,false);
            Box("snow bank",p,new Vector3(17f,.10f,-10.5f),new Vector3(14f,.22f,.85f),snow,false,Quaternion.Euler(0,-3f,0));
        }

        static void RebuildSchoolInteriorArchitecture(Transform parent) {
            GameObject root=new GameObject("SCHOOL INTERIOR ARCHITECTURE V87");root.transform.SetParent(parent,true);
            // Preserve doors, loot, furniture scripts and colliders. Replace only the old architectural skin.
            GameObject baseRoot=GameObject.Find("COZY SCHOOL GAME V58");
            Transform legacy=baseRoot?baseRoot.transform.Find("SCHOOL INTERIOR"):null;
            if(legacy) foreach(Renderer r in legacy.GetComponentsInChildren<Renderer>(true)) {
                string n=r.gameObject.name.ToLowerInvariant();
                if(n.Contains("floor")||n.Contains("wall")||n.Contains("cap")||n.Contains("trim")||n.Contains("window")) r.enabled=false;
            }
            Box("corridor floor",root.transform,new Vector3(0,.025f,2f),new Vector3(35.6f,.07f,5.65f),floorGreen,false);
            Box("classroom A floor",root.transform,new Vector3(-13f,.025f,10f),new Vector3(9.45f,.07f,11.2f),floorWarm,false);
            Box("classroom B floor",root.transform,new Vector3(-4.25f,.025f,10f),new Vector3(7.55f,.07f,11.2f),floorWarm,false);
            Box("staff floor",root.transform,new Vector3(3.45f,.025f,10f),new Vector3(7.55f,.07f,11.2f),floorGreen,false);
            Box("library floor",root.transform,new Vector3(9.9f,.025f,10f),new Vector3(5.15f,.07f,11.2f),floorWarm,false);
            Box("janitor floor",root.transform,new Vector3(15.05f,.025f,10f),new Vector3(5.05f,.07f,11.2f),floorGreen,false);
            // Low readable cutaway partitions.
            LowWallX(root.transform,-18f,18f,15.7f,1.05f);
            LowWallZ(root.transform,-18f,-1f,15.7f,1.05f); LowWallZ(root.transform,18f,-1f,15.7f,1.05f);
            LowWallZ(root.transform,-8.2f,4.25f,15.7f,.92f); LowWallZ(root.transform,-.45f,4.25f,15.7f,.92f); LowWallZ(root.transform,7.4f,4.25f,15.7f,.92f); LowWallZ(root.transform,12.5f,4.25f,15.7f,.92f);
            // Corridor wainscot / lockers add school identity without covering gameplay.
            for(int i=0;i<11;i++) Box("corridor locker",root.transform,new Vector3(-15f+i*3.0f,.55f,4.02f),new Vector3(1.0f,1.05f,.30f),i%3==0?blue:cream,false);
            Box("notice board",root.transform,new Vector3(2.8f,.88f,4.00f),new Vector3(3.1f,1.0f,.10f),wood,false);
        }

        static void RebuildIceRinkInterior(Transform parent) {
            GameObject root=new GameObject("ICE RINK INTERIOR V87");root.transform.SetParent(parent,true);
            GameObject baseRoot=GameObject.Find("COZY SCHOOL GAME V58");
            Transform legacy=baseRoot?baseRoot.transform.Find("SPORT HALL"):null;
            if(legacy) foreach(Renderer r in legacy.GetComponentsInChildren<Renderer>(true)) {
                string n=r.gameObject.name.ToLowerInvariant();
                if(n.Contains("floor")||n.Contains("court")||n.Contains("line")||n.Contains("wall")||n.Contains("cap")||n.Contains("bleacher")) r.enabled=false;
            }
            Vector3 c=new Vector3(34f,.025f,7f);
            Box("ice sheet",root.transform,c,new Vector3(19.2f,.065f,16.0f),ice,false);
            // Boards.
            Box("boards north",root.transform,new Vector3(34f,.48f,15.2f),new Vector3(19.7f,.92f,.16f),cream,false);
            Box("boards south",root.transform,new Vector3(34f,.48f,-1.2f),new Vector3(19.7f,.92f,.16f),cream,false);
            Box("boards west",root.transform,new Vector3(24.15f,.48f,7f),new Vector3(.16f,.92f,16.55f),cream,false);
            Box("boards east",root.transform,new Vector3(43.85f,.48f,7f),new Vector3(.16f,.92f,16.55f),cream,false);
            // Hockey markings.
            Box("centre red",root.transform,c+Vector3.up*.045f,new Vector3(.08f,.012f,15.7f),rinkRed,false);
            Box("blue line west",root.transform,new Vector3(30.7f,.078f,7f),new Vector3(.12f,.012f,15.7f),rinkBlue,false);
            Box("blue line east",root.transform,new Vector3(37.3f,.078f,7f),new Vector3(.12f,.012f,15.7f),rinkBlue,false);
            CircleMark(root.transform,new Vector3(34f,.081f,7f),1.55f,rinkBlue);
            CircleMark(root.transform,new Vector3(28.2f,.081f,3.2f),1.15f,rinkRed);CircleMark(root.transform,new Vector3(28.2f,.081f,10.8f),1.15f,rinkRed);
            CircleMark(root.transform,new Vector3(39.8f,.081f,3.2f),1.15f,rinkRed);CircleMark(root.transform,new Vector3(39.8f,.081f,10.8f),1.15f,rinkRed);
            Goal(root.transform,new Vector3(25.1f,.12f,7f),90f); Goal(root.transform,new Vector3(42.9f,.12f,7f),-90f);
            // Long-side bench / small stand.
            for(int row=0;row<3;row++) Box("stand row",root.transform,new Vector3(34f,.22f+row*.24f,14.25f-row*.34f),new Vector3(8.6f,.18f,.42f),wood,false);
        }

        static void CleanLegacyCoreRenderers() {
            // V86 school sign was parented to a non-uniformly scaled cube, which is why the text stretched.
            foreach(TextMesh tm in UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include,FindObjectsSortMode.None)) {
                if(!tm)continue; string n=(tm.text??"").ToLowerInvariant();
                if(n.Contains("rosviks skola") && tm.transform.root.name!=HeroName) tm.gameObject.SetActive(false);
            }
        }

        struct EdgeInfo { public Vector3 a,b,mid,dir,outward; }
        static EdgeInfo FrontEdge(Vector3[] pts) {
            EdgeInfo best=new EdgeInfo(); float score=float.PositiveInfinity;
            Vector3 centre=BoundsOf(pts).center;
            for(int i=0;i<pts.Length;i++) {
                Vector3 a=pts[i],b=pts[(i+1)%pts.Length],mid=(a+b)*.5f; float len=Vector3.Distance(a,b); if(len<2f)continue;
                float s=mid.z - len*.03f; if(s<score){score=s;Vector3 d=(b-a).normalized;Vector3 n=new Vector3(-d.z,0,d.x);if(Vector3.Dot(n,mid-centre)<0)n=-n;best=new EdgeInfo{a=a,b=b,mid=mid,dir=d,outward=n};}
            }
            return best;
        }

        static void AddSchoolEntrance(Transform p,EdgeInfo e,float h) {
            float yaw=Mathf.Atan2(e.dir.x,e.dir.z)*Mathf.Rad2Deg;
            Box("school entrance dark recess",p,e.mid+Vector3.up*1.25f,new Vector3(.22f,2.35f,4.4f),metal,false,Quaternion.Euler(0,yaw,0));
            Box("school entrance canopy",p,e.mid+e.outward*.85f+Vector3.up*2.65f,new Vector3(2.1f,.18f,6.2f),roof,false,Quaternion.Euler(0,yaw,0));
            Box("school entrance snow",p,e.mid+e.outward*.85f+Vector3.up*2.78f,new Vector3(1.85f,.045f,5.9f),snow,false,Quaternion.Euler(0,yaw,0));
            Box("entry light",p,e.mid+e.outward*.18f+Vector3.up*1.85f,new Vector3(.18f,.28f,.28f),warmGlass,false,Quaternion.Euler(0,yaw,0));
            WorldLabel(p,"ROSVIKS SKOLA",e.mid+e.outward*.14f+Vector3.up*2.25f,yaw,.15f);
        }

        static void AddFacadeWindows(Transform p,Vector3[] pts,float h,bool school) {
            for(int i=0;i<pts.Length;i++) {
                Vector3 a=pts[i],b=pts[(i+1)%pts.Length],d=b-a; float len=d.magnitude; if(len<4.2f)continue;
                Vector3 dir=d.normalized; int n=Mathf.Clamp(Mathf.FloorToInt(len/4.1f),1,8); float yaw=Mathf.Atan2(dir.x,dir.z)*Mathf.Rad2Deg;
                for(int k=1;k<=n;k++) {
                    Vector3 q=Vector3.Lerp(a,b,k/(float)(n+1))+Vector3.up*(h*.54f);
                    Box("window trim",p,q,new Vector3(.15f,1.22f,1.55f),trim,false,Quaternion.Euler(0,yaw,0));
                    Box("window glass",p,q,new Vector3(.17f,.92f,1.30f),glass,false,Quaternion.Euler(0,yaw,0));
                }
            }
        }
        static void AddWarmWindowNearEdge(Transform p,Vector3[] pts,int i,float h) {
            Vector3 a=pts[i%pts.Length],b=pts[(i+1)%pts.Length]; if(Vector3.Distance(a,b)<5f)return; Vector3 d=(b-a).normalized;float yaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;
            Vector3 q=(a+b)*.5f+Vector3.up*(h*.54f);Box("warm window",p,q,new Vector3(.18f,.82f,1.0f),warmGlass,false,Quaternion.Euler(0,yaw,0));
        }
        static void AddArenaHighWindows(Transform p,Vector3[] pts,int i,float h) {
            Vector3 a=pts[i],b=pts[(i+1)%pts.Length],d=b-a;float len=d.magnitude;if(len<8f)return;Vector3 dir=d.normalized;float yaw=Mathf.Atan2(dir.x,dir.z)*Mathf.Rad2Deg;int n=Mathf.Clamp(Mathf.FloorToInt(len/7f),1,5);
            for(int k=1;k<=n;k++){Vector3 q=Vector3.Lerp(a,b,k/(float)(n+1))+Vector3.up*(h*.66f);Box("arena high window trim",p,q,new Vector3(.16f,.85f,1.7f),trim,false,Quaternion.Euler(0,yaw,0));Box("arena high window",p,q,new Vector3(.18f,.62f,1.45f),glass,false,Quaternion.Euler(0,yaw,0));}
        }

        static void BuildPolygonWalls(Transform p,Vector3[] pts,float h,Material facade,bool plinth,bool colliders) {
            for(int i=0;i<pts.Length;i++) {
                Vector3 a=pts[i],b=pts[(i+1)%pts.Length],d=b-a;float len=d.magnitude;if(len<.15f)continue;float yaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;Vector3 mid=(a+b)*.5f+Vector3.up*(h*.5f);
                Box("facade",p,mid,new Vector3(.22f,h,len+.02f),facade,colliders,Quaternion.Euler(0,yaw,0));
                if(plinth)Box("brick plinth",p,new Vector3(mid.x,.34f,mid.z),new Vector3(.25f,.68f,len+.04f),brick,false,Quaternion.Euler(0,yaw,0));
            }
        }

        static void BuildPolygonRoof(Transform p,Vector3[] pts,float y,Material roofMat,Material snowMat,string name) {
            Mesh m=TriangulatedPolygon(pts,y,name); GameObject g=MeshObject(name,p,m,roofMat,false);
            Vector3[] snowPts=pts.Select(q=>new Vector3(q.x,y+.10f,q.z)).ToArray(); Mesh sm=TriangulatedPolygon(snowPts,y+.10f,name+" snow mesh"); MeshObject(name+" snow",p,sm,snowMat,false);
        }

        static Mesh TriangulatedPolygon(Vector3[] src,float y,string name) {
            List<Vector2> poly=src.Select(v=>new Vector2(v.x,v.z)).ToList();
            if(SignedArea(poly)<0)poly.Reverse();
            List<int> idx=Enumerable.Range(0,poly.Count).ToList();List<int> tris=new List<int>();int guard=0;
            while(idx.Count>2 && guard++<500) {
                bool clipped=false;
                for(int ii=0;ii<idx.Count;ii++) {
                    int ia=idx[(ii-1+idx.Count)%idx.Count],ib=idx[ii],ic=idx[(ii+1)%idx.Count];Vector2 a=poly[ia],b=poly[ib],c=poly[ic];
                    if(Cross(b-a,c-b)<=.0001f)continue;bool inside=false;
                    for(int j=0;j<idx.Count;j++){int ip=idx[j];if(ip==ia||ip==ib||ip==ic)continue;if(PointInTri(poly[ip],a,b,c)){inside=true;break;}}
                    if(inside)continue;tris.Add(ia);tris.Add(ic);tris.Add(ib);idx.RemoveAt(ii);clipped=true;break;
                }
                if(!clipped)break;
            }
            if(tris.Count<3){tris.Clear();for(int i=1;i<poly.Count-1;i++){tris.Add(0);tris.Add(i+1);tris.Add(i);}}
            Vector3[] v=poly.Select(q=>new Vector3(q.x,y,q.y)).ToArray();Vector2[] uv=poly.Select(q=>q*.04f).ToArray();
            Mesh m=new Mesh();m.name=name;m.vertices=v;m.uv=uv;m.triangles=tris.ToArray();m.RecalculateNormals();m.RecalculateBounds();return m;
        }
        static float SignedArea(List<Vector2> p){float a=0;for(int i=0;i<p.Count;i++){Vector2 q=p[i],r=p[(i+1)%p.Count];a+=q.x*r.y-r.x*q.y;}return a*.5f;}
        static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        static bool PointInTri(Vector2 p,Vector2 a,Vector2 b,Vector2 c){float c1=Cross(b-a,p-a),c2=Cross(c-b,p-b),c3=Cross(a-c,p-c);bool neg=c1<0||c2<0||c3<0,pos=c1>0||c2>0||c3>0;return !(neg&&pos);}

        static void LowWallX(Transform p,float x1,float x2,float z,float h){Box("low school wall",p,new Vector3((x1+x2)*.5f,h*.5f,z),new Vector3(Mathf.Abs(x2-x1),h,.18f),wallWarm,false);}
        static void LowWallZ(Transform p,float x,float z1,float z2,float h){Box("low school wall",p,new Vector3(x,h*.5f,(z1+z2)*.5f),new Vector3(.18f,h,Mathf.Abs(z2-z1)),wallWarm,false);}

        static void CircleMark(Transform p,Vector3 c,float r,Material m){int n=24;for(int i=0;i<n;i++){float a=i*Mathf.PI*2f/n,b=(i+1)*Mathf.PI*2f/n;Vector3 p0=c+new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r),p1=c+new Vector3(Mathf.Cos(b)*r,0,Mathf.Sin(b)*r);SlabBetween("rink circle",p,p0,p1,.055f,m);}}
        static void Goal(Transform p,Vector3 c,float yaw){GameObject g=new GameObject("hockey goal");g.transform.SetParent(p,true);g.transform.position=c;g.transform.rotation=Quaternion.Euler(0,yaw,0);Cylinder("goal post L",g.transform,new Vector3(-.72f,.52f,0),.035f,1.04f,rinkRed);Cylinder("goal post R",g.transform,new Vector3(.72f,.52f,0),.035f,1.04f,rinkRed);Cylinder("goal bar",g.transform,new Vector3(0,1.03f,0),.035f,1.44f,rinkRed,Quaternion.Euler(0,0,90));}

        static Bounds BoundsOf(Vector3[] p){Bounds b=new Bounds(p[0],Vector3.zero);foreach(Vector3 q in p)b.Encapsulate(q);return b;}

        static void BuildMaterials() {
            snow=Mat("snow","c9d6da",.18f);packedSnow=Mat("packed_snow","879ca4",.12f);ochre=Mat("school_ochre","9b7548",.18f);brick=Mat("brick","6d453f",.16f);blue=Mat("arena_blue","35596a",.20f);cream=Mat("cream","d5d0bf",.18f);trim=Mat("trim","e0dccd",.22f);glass=Mat("glass","3c5964",.38f);warmGlass=Emissive("warm_glass","efb76a",1.55f);roof=Mat("roof","30434d",.32f);metal=Mat("metal","3a484d",.36f);ice=Mat("ice","a9c8d2",.42f);rinkBlue=Mat("rink_blue","416f93",.20f);rinkRed=Mat("rink_red","9b4a45",.18f);rinkLine=Mat("rink_white","e3e7df",.18f);wood=Mat("wood","806047",.18f);floorGreen=Mat("school_green","65776f",.14f);floorWarm=Mat("school_warm_floor","88705a",.16f);wallWarm=Mat("school_wall","c6c0ac",.16f);
        }
        static Shader PickShader(){Shader s=Shader.Find("Universal Render Pipeline/Lit");if(s&&s.isSupported)return s;s=Shader.Find("Standard");if(s&&s.isSupported)return s;return Shader.Find("Hidden/InternalErrorShader");}
        static Material Mat(string name,string hex,float smooth){string path=Generated+"/"+name+".mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(shader){name="V87 "+name};AssetDatabase.CreateAsset(m,path);}Color c=ColorUtility.TryParseHtmlString("#"+hex,out Color cc)?cc:Color.gray;if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);if(m.HasProperty("_Color"))m.SetColor("_Color",c);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;}
        static Material Emissive(string name,string hex,float intensity){Material m=Mat(name,hex,.18f);Color c=ColorUtility.TryParseHtmlString("#"+hex,out Color cc)?cc:Color.white;if(m.HasProperty("_EmissionColor")){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",c*intensity);}return m;}

        static Transform Group(Transform p,string n){GameObject g=new GameObject(n);g.transform.SetParent(p,true);return g.transform;}
        static GameObject Box(string n,Transform p,Vector3 pos,Vector3 scale,Material m,bool col,Quaternion? rot=null){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,true);g.transform.position=pos;g.transform.rotation=rot??Quaternion.identity;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=m;if(!col)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
        static GameObject MeshObject(string n,Transform p,Mesh mesh,Material m,bool col){GameObject g=new GameObject(n);g.transform.SetParent(p,true);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=m;if(col){MeshCollider c=g.AddComponent<MeshCollider>();c.sharedMesh=mesh;}return g;}
        static void SlabBetween(string n,Transform p,Vector3 a,Vector3 b,float w,Material m){Vector3 d=b-a;float len=new Vector2(d.x,d.z).magnitude;if(len<.01f)return;float yaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;Box(n,p,(a+b)*.5f,new Vector3(w,.018f,len),m,false,Quaternion.Euler(0,yaw,0));}
        static void Cylinder(string n,Transform p,Vector3 pos,float rad,float h,Material m,Quaternion? rot=null){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localRotation=rot??Quaternion.identity;g.transform.localScale=new Vector3(rad*2f,h*.5f,rad*2f);g.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());}
        static void LampPost(Transform p,Vector3 pos){Cylinder("lamp post",p,pos+Vector3.up*1.55f,.045f,3.1f,metal);Box("lamp head",p,pos+new Vector3(.18f,3.0f,0),new Vector3(.45f,.13f,.20f),metal,false);}
        static void WorldLabel(Transform p,string text,Vector3 pos,float yaw,float size){GameObject g=new GameObject("world label · "+text);g.transform.SetParent(p,true);g.transform.position=pos;g.transform.rotation=Quaternion.Euler(0,yaw+180f,0);TextMesh tm=g.AddComponent<TextMesh>();tm.text=text;tm.fontSize=44;tm.characterSize=size;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;tm.color=new Color(.94f,.93f,.86f);}

        static void CleanupMissingScripts(){foreach(GameObject g in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include,FindObjectsSortMode.None)){if(!g)continue;try{GameObjectUtility.RemoveMonoBehavioursWithMissingScript(g);}catch{}}}
    }
}
#endif
