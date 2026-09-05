#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikDioramaV20 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string GeneratedDir = "Assets/Rosvik/GeneratedV20";
        const string Key = "ROSVIK_DIORAMA_VERSION";
        const int Version = 20;
        const long MainSchoolWay = 163199458;
        const long OldSchoolWay = 163199461;
        const long ArenaWay = 163199454;

        static RosvikDioramaV20() {
            string[] retired = {
                "ROSVIK_UNITY_BOOTSTRAP_VERSION", "ROSVIK_SITE_CALIBRATION_VERSION",
                "ROSVIK_DETAIL_PASS_VERSION", "ROSVIK_POLISH_PASS_VERSION",
                "ROSVIK_HERO_POLISH_VERSION", "ROSVIK_MAP_LOCK_VERSION",
                "ROSVIK_WORLD_PASS_VERSION", "ROSVIK_CLEAN_WORLD_VERSION",
                "ROSVIK_STYLIZED_MAP_VERSION", "ROSVIK_STYLIZED_RENDER_FIX_VERSION"
            };
            foreach (string k in retired) EditorPrefs.SetInt(k, 999);
            EditorApplication.delayCall += Build;
        }

        [MenuItem("Rosvik/Rebuild Diorama V20")]
        public static void Build() {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorPrefs.GetInt(Key, 0) >= Version && File.Exists(ScenePath)) return;

            try {
                List<RosvikOsmV15.Way> ways = RosvikOsmV15.LoadWays();
                if (ways == null || ways.Count == 0) {
                    Debug.LogError("ROSVIK V20: map data unavailable.");
                    return;
                }

                Directory.CreateDirectory("Assets/Rosvik/Scenes");
                Directory.CreateDirectory(GeneratedDir);
                AssetDatabase.Refresh();

                Texture2D groundTex = NoiseTexture("ground", new Color(.72f,.70f,.64f), new Color(1f,.96f,.88f), 17, .055f, .13f);
                Texture2D roadTex = NoiseTexture("road", new Color(.68f,.68f,.68f), new Color(.98f,.98f,.96f), 41, .075f, .07f);
                Texture2D roofTex = NoiseTexture("roof", new Color(.70f,.72f,.72f), new Color(.98f,.98f,.96f), 73, .09f, .05f);
                Texture2D grassTex = NoiseTexture("grass", new Color(.68f,.76f,.62f), new Color(1f,.98f,.86f), 97, .065f, .12f);

                Material ground = Mat("ground", new Color(.34f,.35f,.23f), groundTex, .05f, 0f);
                Material grass = Mat("grass", new Color(.29f,.38f,.19f), grassTex, .06f, 0f);
                Material forestFloor = Mat("forest_floor", new Color(.16f,.24f,.14f), grassTex, .04f, 0f);
                Material road = Mat("wet_asphalt", new Color(.15f,.16f,.16f), roadTex, .48f, 0f);
                Material roadEdge = Mat("road_edge", new Color(.09f,.095f,.09f), roadTex, .18f, 0f);
                Material gravel = Mat("gravel", new Color(.35f,.31f,.25f), groundTex, .10f, 0f);
                Material foot = Mat("footpath", new Color(.39f,.34f,.27f), groundTex, .12f, 0f);
                Material roof = Mat("dark_roof", new Color(.18f,.19f,.18f), roofTex, .24f, .03f);
                Material genericWall = Mat("house_walls", new Color(.53f,.45f,.35f), null, .06f, 0f);
                Material mainWall = Mat("school_walls", new Color(.47f,.43f,.31f), null, .05f, 0f);
                Material oldWall = Mat("traskolan_yellow", new Color(.71f,.52f,.20f), null, .05f, 0f);
                Material arenaWall = Mat("arena_walls", new Color(.34f,.38f,.39f), null, .09f, .02f);
                Material pitch = Mat("pitch", new Color(.23f,.42f,.16f), grassTex, .04f, 0f);
                Material pitchLine = Mat("pitch_lines", new Color(.90f,.88f,.72f), null, .10f, 0f);
                Material puddle = Mat("puddle", new Color(.13f,.20f,.22f), null, .88f, .03f);
                Material snow = Mat("last_snow", new Color(.78f,.80f,.77f), null, .20f, 0f);
                Material bark = Mat("bark", new Color(.18f,.12f,.08f), null, .03f, 0f);
                Material birchBark = Mat("birch_bark", new Color(.72f,.70f,.62f), null, .10f, 0f);
                Material spruce = Mat("spruce", new Color(.08f,.20f,.12f), null, .05f, 0f);
                Material spruce2 = Mat("spruce_light", new Color(.12f,.28f,.16f), null, .05f, 0f);
                Material deciduous = Mat("spring_birch", new Color(.37f,.42f,.18f), null, .04f, 0f);
                Material bush = Mat("bush", new Color(.27f,.35f,.15f), null, .04f, 0f);
                Material dark = Mat("dark_detail", new Color(.07f,.075f,.07f), null, .18f, .03f);
                Material goalWhite = Mat("goal_white", new Color(.82f,.82f,.75f), null, .18f, 0f);

                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Transform root = new GameObject("ROSVIK_DIORAMA_GAME_V20").transform;

                GameObject groundCollider = Cube("WORLD GROUND COLLIDER", root, new Vector3(60f,-.28f,-110f), new Vector3(700f,.55f,700f), ground, true);
                groundCollider.GetComponent<Renderer>().enabled = false;
                Polygon("WORLD GROUND VISUAL", root, new List<Vector3>{
                    new Vector3(-290f,0,240f), new Vector3(410f,0,240f), new Vector3(410f,0,-460f), new Vector3(-290f,0,-460f)
                }, .001f, ground);

                Transform land = Group(root,"01 LANDSCAPE");
                Transform roads = Group(root,"02 ROADS");
                Transform sports = Group(root,"03 ROSVALLA");
                Transform buildings = Group(root,"04 BUILDINGS");
                Transform nature = Group(root,"05 VEGETATION");
                Transform weather = Group(root,"06 WET SPRING");

                BuildLandcover(land, ways, grass, forestFloor);
                BuildRoads(roads, ways, roadEdge, road, gravel, foot);
                BuildSports(sports, ways, pitch, pitchLine, goalWhite);
                BuildBuildings(buildings, ways, genericWall, mainWall, oldWall, arenaWall, roof, dark);

                RosvikOsmV15.Way main = ways.FirstOrDefault(w => w.Id == MainSchoolWay);
                RosvikOsmV15.Way old = ways.FirstOrDefault(w => w.Id == OldSchoolWay);
                Vector3 mainCenter = main != null ? RosvikOsmV15.Centroid(main) : Vector3.zero;
                Vector3 oldCenter = old != null ? RosvikOsmV15.Centroid(old) : mainCenter + new Vector3(-70f,0,-50f);

                BuildNature(nature, ways, mainCenter, bark, birchBark, spruce, spruce2, deciduous, bush);
                BuildWeather(weather, mainCenter, oldCenter, puddle, snow);

                Vector3 spawn = FindSpawnNearSchool(ways, mainCenter);
                GameObject player = BuildPlayer(root, spawn, dark);
                BuildCamera(player.transform);
                BuildLighting();
                BuildHUD(root, ways);

                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorPrefs.SetInt(Key, Version);
                Selection.activeObject = null;
                Debug.Log("ROSVIK V20 BUILT: stable 2.5D diorama using real Rosvik geography; flat reliable roofs, wet spring landscape and readable landmarks.");
            } catch (Exception ex) {
                Debug.LogError("ROSVIK V20 FAILED: " + ex);
            }
        }

        static void BuildLandcover(Transform parent, List<RosvikOsmV15.Way> ways, Material grass, Material forest) {
            foreach (var w in ways) {
                if (!w.Closed || w.Nodes.Count < 4 || !string.IsNullOrEmpty(w.Tag("building"))) continue;
                string land=w.Tag("landuse"), natural=w.Tag("natural"), leisure=w.Tag("leisure");
                Material m=null;
                if (land=="forest" || natural=="wood") m=forest;
                else if (land=="grass" || land=="meadow" || leisure=="park" || leisure=="garden") m=grass;
                if (m) Polygon("land " + w.Id,parent,Points(w),.012f,m);
            }
        }

        static void BuildRoads(Transform parent, List<RosvikOsmV15.Way> ways, Material edge, Material road, Material gravel, Material foot) {
            foreach (var w in ways) {
                string h=w.Tag("highway");
                if (string.IsNullOrEmpty(h) || w.Nodes.Count<2) continue;
                float outer, inner; Material surface;
                if (h=="residential" || h=="unclassified" || h=="tertiary") { outer=6.6f; inner=5.3f; surface=road; }
                else if (h=="service" || h=="living_street") { outer=4.5f; inner=3.6f; surface=gravel; }
                else if (h=="footway" || h=="path" || h=="cycleway" || h=="pedestrian") { outer=2.1f; inner=1.45f; surface=foot; }
                else continue;
                Transform g=Group(parent,"road " + w.Id + Suffix(w));
                for (int i=0;i<w.Nodes.Count-1;i++) {
                    Vector3 a=w.Nodes[i].Pos,b=w.Nodes[i+1].Pos;
                    Strip("edge",g,a,b,outer,.024f,edge);
                    Strip("surface",g,a,b,inner,.032f,surface);
                    Disc("joint",g,a,inner*.5f,.033f,surface,16);
                }
                Disc("joint",g,w.Nodes[w.Nodes.Count-1].Pos,inner*.5f,.033f,surface,16);
            }
        }

        static void BuildSports(Transform parent, List<RosvikOsmV15.Way> ways, Material pitch, Material line, Material goal) {
            foreach (var w in ways) {
                if (!w.Closed || (w.Tag("leisure")!="pitch" && w.Tag("sport")!="soccer")) continue;
                List<Vector3> pts=Points(w); if(pts.Count<3) continue;
                Polygon("pitch " + w.Id,parent,pts,.040f,pitch);
                Outline("touchline",parent,pts,.11f,.048f,line);
                RosvikOsmV15.OBounds b=RosvikOsmV15.Bounds(w);
                Vector3 axis=b.AxisX.normalized, perp=new Vector3(-axis.z,0,axis.x);
                Strip("halfway",parent,b.Center-perp*b.Depth*.5f,b.Center+perp*b.Depth*.5f,.10f,.050f,line);
                Ring("centre circle",parent,b.Center,Mathf.Clamp(Mathf.Min(b.Width,b.Depth)*.11f,2.2f,8f),.10f,.051f,line,28);
                Goal(parent,b.Center+axis*b.Width*.48f,axis,Mathf.Clamp(b.Depth*.22f,2.0f,5.5f),goal);
                Goal(parent,b.Center-axis*b.Width*.48f,-axis,Mathf.Clamp(b.Depth*.22f,2.0f,5.5f),goal);
            }
        }

        static void BuildBuildings(Transform parent, List<RosvikOsmV15.Way> ways, Material generic, Material main, Material old, Material arena, Material roof, Material trim) {
            Color[] houseColors={
                new Color(.54f,.35f,.25f), new Color(.56f,.50f,.37f), new Color(.39f,.48f,.42f),
                new Color(.60f,.44f,.29f), new Color(.43f,.40f,.36f), new Color(.50f,.31f,.25f)
            };
            foreach(var w in ways) {
                string tag=w.Tag("building"); if(string.IsNullOrEmpty(tag)||tag=="no"||!w.Closed)continue;
                List<Vector3> pts=Points(w); if(pts.Count<3)continue;
                float h=2.9f; Material wall=generic;
                if(w.Id==MainSchoolWay){h=3.25f;wall=main;}
                else if(w.Id==OldSchoolWay){h=5.6f;wall=old;}
                else if(w.Id==ArenaWay){h=7.0f;wall=arena;}
                else if(tag=="garage"||tag=="shed"||tag=="carport")h=2.1f;
                else if(tag=="apartments")h=5.2f;
                else wall=Mat("house_"+Math.Abs((int)(w.Id%997)),houseColors[Math.Abs((int)(w.Id%houseColors.Length))],null,.05f,0f);

                Transform b=ExtrudedBuilding(parent,BuildingName(w),pts,h,wall,roof);
                if(!b)continue;
                RosvikOsmV15.OBounds bounds=RosvikOsmV15.Bounds(w);
                if(w.Id==OldSchoolWay) {
                    AddFacadeCue(b,bounds,h,trim,true);
                } else if(w.Id==MainSchoolWay) {
                    AddFacadeCue(b,bounds,h,trim,false);
                } else if(w.Id==ArenaWay) {
                    BoxOnWorld("arena rooftop plant",b,bounds.Center+new Vector3(0,h+.28f,0),new Vector3(Mathf.Min(8f,bounds.Width*.25f),.45f,Mathf.Min(4f,bounds.Depth*.22f)),trim,false);
                }
            }
        }

        static Transform ExtrudedBuilding(Transform parent,string name,List<Vector3> pts,float height,Material wall,Material roof) {
            pts = new List<Vector3>(pts);
            if (SignedArea(pts) < 0f) pts.Reverse();
            int n=pts.Count; int[] top=Triangulate(pts); if(n<3||top.Length<3)return null;
            Transform root=Group(parent,name);

            Vector3[] wv=new Vector3[n*2];
            for(int i=0;i<n;i++){wv[i]=new Vector3(pts[i].x,.055f,pts[i].z);wv[n+i]=new Vector3(pts[i].x,height,pts[i].z);}            
            List<int> wt=new List<int>();
            for(int i=0;i<n;i++){int j=(i+1)%n;wt.Add(i);wt.Add(n+i);wt.Add(n+j);wt.Add(i);wt.Add(n+j);wt.Add(j);}            
            Vector2[] wuv=new Vector2[wv.Length];for(int i=0;i<wv.Length;i++)wuv[i]=new Vector2(wv[i].x*.12f,wv[i].z*.12f);
            Mesh wm=new Mesh{name=name+" walls",vertices=wv,triangles=wt.ToArray(),uv=wuv};wm.RecalculateNormals();wm.RecalculateBounds();
            GameObject walls=new GameObject("walls");walls.transform.SetParent(root,false);walls.AddComponent<MeshFilter>().sharedMesh=wm;walls.AddComponent<MeshRenderer>().sharedMaterial=wall;

            Vector3[] rv=pts.Select(p=>new Vector3(p.x,height+.035f,p.z)).ToArray();
            Vector2[] ruv=rv.Select(q=>new Vector2(q.x*.12f,q.z*.12f)).ToArray();
            Mesh rm=new Mesh{name=name+" flat roof",vertices=rv,triangles=top,uv=ruv};rm.RecalculateNormals();
            if(rm.normals.Length>0&&rm.normals.Average(x=>x.y)<0){int[] t=rm.triangles;for(int i=0;i<t.Length;i+=3){int q=t[i+1];t[i+1]=t[i+2];t[i+2]=q;}rm.triangles=t;rm.RecalculateNormals();}
            GameObject rg=new GameObject("flat stylized roof");rg.transform.SetParent(root,false);rg.AddComponent<MeshFilter>().sharedMesh=rm;rg.AddComponent<MeshRenderer>().sharedMaterial=roof;

            Mesh colliderMesh=PrismMesh(pts,height); if(colliderMesh){MeshCollider mc=root.gameObject.AddComponent<MeshCollider>();mc.sharedMesh=colliderMesh;}
            return root;
        }

        static void AddFacadeCue(Transform b,RosvikOsmV15.OBounds bounds,float h,Material trim,bool oldSchool) {
            Vector3 axis=bounds.AxisX.normalized;
            Vector3 perp=new Vector3(-axis.z,0,axis.x);
            float frontSign=1f;
            int count=oldSchool?5:Mathf.Clamp(Mathf.RoundToInt(bounds.Width/5f),4,10);
            for(int i=0;i<count;i++) {
                float t=(i+.5f)/count-.5f;
                Vector3 pos=bounds.Center+axis*(t*bounds.Width*.82f)+perp*(bounds.Depth*.5f+.04f*frontSign)+new Vector3(0,oldSchool?h*.55f:h*.52f,0);
                Vector3 size=new Vector3(Mathf.Max(.7f,bounds.Width/count*.38f),oldSchool?1.1f:.85f,.08f);
                GameObject q=BoxOnWorld("window cue",b,pos,size,trim,false);
                q.transform.rotation=Quaternion.FromToRotation(Vector3.right,axis);
            }
            if(oldSchool) {
                Vector3 door=bounds.Center+perp*(bounds.Depth*.5f+.06f)+new Vector3(0,1.1f,0);
                GameObject d=BoxOnWorld("entrance cue",b,door,new Vector3(1.25f,2.15f,.10f),trim,false);
                d.transform.rotation=Quaternion.FromToRotation(Vector3.right,axis);
            }
        }

        static void BuildNature(Transform parent,List<RosvikOsmV15.Way> ways,Vector3 school,Material bark,Material birchBark,Material spruce,Material spruce2,Material deciduous,Material bush) {
            foreach(var w in ways) {
                if(!w.Closed)continue; string land=w.Tag("landuse"), natural=w.Tag("natural");
                if(land!="forest"&&natural!="wood")continue;
                ScatterTrees(parent,Points(w),w.Id,30,bark,birchBark,spruce,spruce2,deciduous);
            }
            System.Random rng=new System.Random(2020);
            for(int i=0;i<54;i++) {
                float a=(float)rng.NextDouble()*Mathf.PI*2f;
                float r=24f+(float)rng.NextDouble()*82f;
                Vector3 p=school+new Vector3(Mathf.Cos(a)*r,.03f,Mathf.Sin(a)*r);
                if(i%5==0) Bush(parent,p,.75f+(float)rng.NextDouble()*.8f,bush);
                else Tree(parent,p,.85f+(float)rng.NextDouble()*.75f,i%6==0,bark,birchBark,spruce,spruce2,deciduous);
            }
        }

        static void ScatterTrees(Transform parent,List<Vector3> poly,long seed,int max,Material bark,Material birchBark,Material spruce,Material spruce2,Material deciduous) {
            if(poly.Count<3)return; float minX=poly.Min(p=>p.x),maxX=poly.Max(p=>p.x),minZ=poly.Min(p=>p.z),maxZ=poly.Max(p=>p.z);
            System.Random rng=new System.Random((int)(seed&0x7fffffff));int made=0;
            for(int tries=0;tries<max*9&&made<max;tries++) {
                Vector3 p=new Vector3(Mathf.Lerp(minX,maxX,(float)rng.NextDouble()),.03f,Mathf.Lerp(minZ,maxZ,(float)rng.NextDouble()));
                if(!Inside(p,poly))continue;Tree(parent,p,.75f+(float)rng.NextDouble()*.8f,made%7==0,bark,birchBark,spruce,spruce2,deciduous);made++;
            }
        }

        static void Tree(Transform parent,Vector3 p,float s,bool birch,Material bark,Material birchBark,Material spruce,Material spruce2,Material deciduous) {
            Transform t=Group(parent,birch?"birch":"spruce");t.position=p;
            Material trunkMat=birch?birchBark:bark;
            Cylinder("trunk",t,new Vector3(0,1.25f*s,0),new Vector3(.12f*s,1.25f*s,.12f*s),trunkMat,false);
            if(birch) {
                Sphere("crown A",t,new Vector3(-.25f*s,2.65f*s,.05f*s),new Vector3(.85f*s,.62f*s,.78f*s),deciduous,false);
                Sphere("crown B",t,new Vector3(.35f*s,2.85f*s,.10f*s),new Vector3(.72f*s,.58f*s,.70f*s),deciduous,false);
                Sphere("crown C",t,new Vector3(.02f*s,3.20f*s,-.18f*s),new Vector3(.60f*s,.50f*s,.62f*s),deciduous,false);
            } else {
                Sphere("crown lower",t,new Vector3(0,1.85f*s,0),new Vector3(1.10f*s,.60f*s,1.00f*s),spruce,false);
                Sphere("crown middle",t,new Vector3(.08f*s,2.55f*s,-.04f*s),new Vector3(.87f*s,.60f*s,.82f*s),spruce2,false);
                Sphere("crown top",t,new Vector3(-.05f*s,3.12f*s,.04f*s),new Vector3(.58f*s,.58f*s,.55f*s),spruce,false);
            }
        }

        static void Bush(Transform parent,Vector3 p,float s,Material mat) {
            Transform b=Group(parent,"bush");b.position=p;
            Sphere("a",b,new Vector3(-.25f*s,.35f*s,0),new Vector3(.60f*s,.42f*s,.55f*s),mat,false);
            Sphere("b",b,new Vector3(.30f*s,.32f*s,.12f*s),new Vector3(.52f*s,.38f*s,.50f*s),mat,false);
        }

        static void BuildWeather(Transform parent,Vector3 main,Vector3 old,Material puddle,Material snow) {
            Blob("puddle schoolyard",parent,main+new Vector3(-13f,0,17f),new Vector2(4.8f,2.1f),12f,.045f,puddle,18,51);
            Blob("puddle path",parent,main+new Vector3(12f,0,10f),new Vector2(2.7f,1.2f),-20f,.045f,puddle,15,11);
            Blob("snow remnant",parent,old+new Vector3(7f,0,-8f),new Vector2(2.6f,.85f),-8f,.046f,snow,15,81);
        }

        static Vector3 FindSpawnNearSchool(List<RosvikOsmV15.Way> ways,Vector3 school) {
            float best=float.MaxValue; Vector3 bestPoint=school+new Vector3(-18f,.15f,18f);
            foreach(var w in ways) {
                string h=w.Tag("highway"); if(string.IsNullOrEmpty(h)||w.Nodes.Count<2)continue;
                if(h!="footway"&&h!="path"&&h!="service"&&h!="residential"&&h!="living_street")continue;
                for(int i=0;i<w.Nodes.Count-1;i++) {
                    Vector3 p=ClosestPoint(school,w.Nodes[i].Pos,w.Nodes[i+1].Pos);float d=(p-school).sqrMagnitude;
                    if(d<best){best=d;bestPoint=p;}
                }
            }
            bestPoint.y=.18f; return bestPoint;
        }

        static Vector3 ClosestPoint(Vector3 p,Vector3 a,Vector3 b){Vector3 ab=b-a;ab.y=0;float den=ab.sqrMagnitude;if(den<.0001f)return a;float t=Mathf.Clamp01(Vector3.Dot(p-a,ab)/den);return a+ab*t;}

        static GameObject BuildPlayer(Transform root,Vector3 pos,Material dark) {
            GameObject p=new GameObject("PLAYER");p.transform.SetParent(root,false);p.transform.position=pos;
            CharacterController cc=p.AddComponent<CharacterController>();cc.height=1.78f;cc.radius=.31f;cc.center=new Vector3(0,.89f,0);cc.stepOffset=.24f;
            RosvikPlayerController ctl=p.AddComponent<RosvikPlayerController>();ctl.walkSpeed=3.7f;ctl.sprintSpeed=6.0f;
            Material coat=Mat("player_coat",new Color(.50f,.12f,.09f),null,.07f,0f);Material skin=Mat("player_skin",new Color(.68f,.50f,.37f),null,.10f,0f);
            Capsule("body",p.transform,new Vector3(0,1.0f,0),new Vector3(.42f,.66f,.34f),coat,false);
            Sphere("head",p.transform,new Vector3(0,1.62f,0),Vector3.one*.30f,skin,false);
            Cube("leg L",p.transform,new Vector3(-.12f,.38f,0),new Vector3(.15f,.55f,.18f),dark,false);
            Cube("leg R",p.transform,new Vector3(.12f,.38f,0),new Vector3(.15f,.55f,.18f),dark,false);
            return p;
        }

        static void BuildCamera(Transform target) {
            GameObject go=new GameObject("Main Camera");go.tag="MainCamera";
            Camera cam=go.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=16f;cam.nearClipPlane=.1f;cam.farClipPlane=220f;cam.backgroundColor=new Color(.36f,.40f,.36f);cam.clearFlags=CameraClearFlags.SolidColor;
            IsometricCameraRig rig=go.AddComponent<IsometricCameraRig>();rig.target=target;rig.yaw=37f;rig.pitch=55f;rig.focusOffset=new Vector3(0,1.0f,0);rig.orthographicSize=16f;rig.minSize=8f;rig.maxSize=25f;rig.zoomStep=.85f;rig.followSharpness=10f;
        }

        static void BuildLighting() {
            GameObject sunGo=new GameObject("Soft spring light");Light sun=sunGo.AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=.85f;sun.color=new Color(.90f,.92f,.89f);sun.shadows=LightShadows.Soft;sun.shadowStrength=.48f;sunGo.transform.rotation=Quaternion.Euler(50f,-28f,0);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.43f,.46f,.42f);RenderSettings.fog=true;RenderSettings.fogColor=new Color(.47f,.51f,.49f);RenderSettings.fogDensity=.0013f;
        }

        static void BuildHUD(Transform root,List<RosvikOsmV15.Way> ways) {
            RosvikMapHUD hud=root.gameObject.AddComponent<RosvikMapHUD>();hud.mapCamera=Camera.main;hud.maxLabelDistance=95f;
            AddPoi(hud,ways,MainSchoolWay,"ROSVIKS SKOLA");AddPoi(hud,ways,OldSchoolWay,"TRÄSKOLAN");AddPoi(hud,ways,ArenaWay,"NORRBOTTEN STÅL ARENA");
            List<RosvikOsmV15.Way> p=ways.Where(w=>w.Closed&&(w.Tag("leisure")=="pitch"||w.Tag("sport")=="soccer")).ToList();
            if(p.Count>0){Vector3 c=Vector3.zero;foreach(var w in p)c+=RosvikOsmV15.Centroid(w);c/=p.Count;hud.points.Add(new RosvikMapHUD.Poi{name="ROSVALLA",worldPosition=c+Vector3.up*.2f});}
        }

        static void AddPoi(RosvikMapHUD hud,List<RosvikOsmV15.Way> ways,long id,string name){var w=ways.FirstOrDefault(x=>x.Id==id);if(w!=null)hud.points.Add(new RosvikMapHUD.Poi{name=name,worldPosition=RosvikOsmV15.Centroid(w)+Vector3.up*.25f});}

        static Texture2D NoiseTexture(string name,Color a,Color b,int seed,float scale,float speckle) {
            string path=GeneratedDir+"/tex_"+name+".asset";
            Texture2D existing=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(existing)return existing;
            int n=128;Texture2D t=new Texture2D(n,n,TextureFormat.RGBA32,false,true){name="tex_"+name,wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Bilinear};
            System.Random rng=new System.Random(seed);
            for(int y=0;y<n;y++)for(int x=0;x<n;x++){float v=Mathf.PerlinNoise((x+seed)*scale,(y-seed)*scale);Color c=Color.Lerp(a,b,v);if(rng.NextDouble()<speckle*.018)c*=.72f;t.SetPixel(x,y,c);}t.Apply();AssetDatabase.CreateAsset(t,path);return t;
        }

        static Material Mat(string name,Color color,Texture2D tex,float smooth,float metal) {
            string path=GeneratedDir+"/mat_"+Safe(name)+".mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader s=GraphicsSettings.defaultRenderPipeline!=null?Shader.Find("Universal Render Pipeline/Lit"):Shader.Find("Standard");if(!s||!s.isSupported)s=Shader.Find("Sprites/Default");
            if(!m){m=new Material(s){name=name};AssetDatabase.CreateAsset(m,path);}m.shader=s;m.color=color;if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",metal);if(tex){if(m.HasProperty("_BaseMap"))m.SetTexture("_BaseMap",tex);else if(m.HasProperty("_MainTex"))m.SetTexture("_MainTex",tex);}EditorUtility.SetDirty(m);return m;
        }

        static string Safe(string s){foreach(char c in Path.GetInvalidFileNameChars())s=s.Replace(c,'_');return s.Replace(' ','_');}
        static Transform Group(Transform p,string n){Transform t=new GameObject(n).transform;t.SetParent(p,false);return t;}
        static string Suffix(RosvikOsmV15.Way w){string n=w.Tag("name");return string.IsNullOrEmpty(n)?"":" "+n;}
        static string BuildingName(RosvikOsmV15.Way w){if(w.Id==MainSchoolWay)return "ROSVIKS SKOLA";if(w.Id==OldSchoolWay)return "TRÄSKOLAN";if(w.Id==ArenaWay)return "NORRBOTTEN STÅL ARENA";string n=w.Tag("name");return string.IsNullOrEmpty(n)?"building "+w.Id:n;}
        static List<Vector3> Points(RosvikOsmV15.Way w){List<Vector3> p=w.Nodes.Select(n=>n.Pos).ToList();if(p.Count>2&&w.Closed)p.RemoveAt(p.Count-1);return p;}

        static GameObject Cube(string n,Transform p,Vector3 pos,Vector3 scale,Material m,bool col){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=m;if(!col)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
        static GameObject BoxOnWorld(string n,Transform p,Vector3 worldPos,Vector3 scale,Material m,bool col){GameObject g=Cube(n,p,Vector3.zero,scale,m,col);g.transform.position=worldPos;return g;}
        static GameObject Cylinder(string n,Transform p,Vector3 pos,Vector3 scale,Material m,bool col){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=m;if(!col)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
        static GameObject Sphere(string n,Transform p,Vector3 pos,Vector3 scale,Material m,bool col){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=m;if(!col)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
        static GameObject Capsule(string n,Transform p,Vector3 pos,Vector3 scale,Material m,bool col){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Capsule);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=m;if(!col)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}

        static GameObject Polygon(string name,Transform parent,List<Vector3> pts,float y,Material mat){if(pts==null||pts.Count<3)return null;int[] tri=Triangulate(pts);if(tri.Length<3)return null;Vector3[] v=pts.Select(q=>new Vector3(q.x,y,q.z)).ToArray();Vector2[] uv=pts.Select(q=>new Vector2(q.x*.12f,q.z*.12f)).ToArray();Mesh mesh=new Mesh{name=name+" mesh",vertices=v,triangles=tri,uv=uv};mesh.RecalculateNormals();if(mesh.normals.Length>0&&mesh.normals.Average(q=>q.y)<0){int[] t=mesh.triangles;for(int i=0;i<t.Length;i+=3){int z=t[i+1];t[i+1]=t[i+2];t[i+2]=z;}mesh.triangles=t;mesh.RecalculateNormals();}mesh.RecalculateBounds();GameObject g=new GameObject(name);g.transform.SetParent(parent,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=mat;return g;}
        static void Outline(string name,Transform parent,List<Vector3> pts,float width,float y,Material mat){for(int i=0;i<pts.Count;i++)Strip(name,parent,pts[i],pts[(i+1)%pts.Count],width,y,mat);}
        static void Strip(string name,Transform parent,Vector3 a,Vector3 b,float width,float y,Material mat){Vector3 d=b-a;d.y=0;if(d.sqrMagnitude<.0001f)return;Vector3 n=new Vector3(-d.z,0,d.x).normalized*width*.5f;Vector3[] v={new Vector3(a.x+n.x,y,a.z+n.z),new Vector3(a.x-n.x,y,a.z-n.z),new Vector3(b.x-n.x,y,b.z-n.z),new Vector3(b.x+n.x,y,b.z+n.z)};Vector2[] uv=v.Select(q=>new Vector2(q.x*.16f,q.z*.16f)).ToArray();Mesh m=new Mesh{name=name+" mesh",vertices=v,triangles=new[]{0,1,2,0,2,3},uv=uv};m.RecalculateNormals();if(m.normals.Average(q=>q.y)<0){m.triangles=new[]{0,2,1,0,3,2};m.RecalculateNormals();}GameObject g=new GameObject(name);g.transform.SetParent(parent,false);g.AddComponent<MeshFilter>().sharedMesh=m;g.AddComponent<MeshRenderer>().sharedMaterial=mat;}
        static void Disc(string name,Transform parent,Vector3 c,float r,float y,Material mat,int seg){List<Vector3> p=new List<Vector3>();for(int i=0;i<seg;i++){float a=i*Mathf.PI*2/seg;p.Add(new Vector3(c.x+Mathf.Cos(a)*r,0,c.z+Mathf.Sin(a)*r));}Polygon(name,parent,p,y,mat);}
        static void Ring(string name,Transform parent,Vector3 c,float r,float width,float y,Material mat,int seg){for(int i=0;i<seg;i++){float a=i*Mathf.PI*2/seg,b=(i+1)*Mathf.PI*2/seg;Strip(name,parent,c+new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r),c+new Vector3(Mathf.Cos(b)*r,0,Mathf.Sin(b)*r),width,y,mat);}}
        static void Blob(string name,Transform parent,Vector3 center,Vector2 size,float yaw,float y,Material mat,int seg,int seed){System.Random rng=new System.Random(seed);List<Vector3> p=new List<Vector3>();float rr=yaw*Mathf.Deg2Rad,cr=Mathf.Cos(rr),sr=Mathf.Sin(rr);for(int i=0;i<seg;i++){float a=i*Mathf.PI*2/seg;float wob=.86f+(float)rng.NextDouble()*.25f;float x=Mathf.Cos(a)*size.x*wob,z=Mathf.Sin(a)*size.y*wob;p.Add(center+new Vector3(x*cr-z*sr,0,x*sr+z*cr));}Polygon(name,parent,p,y,mat);}

        static void Goal(Transform parent,Vector3 center,Vector3 forward,float width,Material mat){forward.y=0;forward.Normalize();Vector3 right=new Vector3(forward.z,0,-forward.x);Transform g=Group(parent,"goal");g.position=center;Vector3 l=-right*width*.5f,r=right*width*.5f;GameObject a=Cube("left",g,l+Vector3.up*.75f,new Vector3(.07f,1.5f,.07f),mat,false);GameObject b=Cube("right",g,r+Vector3.up*.75f,new Vector3(.07f,1.5f,.07f),mat,false);GameObject c=Cube("bar",g,Vector3.up*1.5f,new Vector3(width,.07f,.07f),mat,false);Quaternion rot=Quaternion.LookRotation(forward,Vector3.up);a.transform.rotation=rot;b.transform.rotation=rot;c.transform.rotation=rot;}

        static Mesh PrismMesh(List<Vector3> pts,float height){int n=pts.Count;if(n<3)return null;int[] top=Triangulate(pts);if(top.Length<3)return null;Vector3[] v=new Vector3[n*2];for(int i=0;i<n;i++){v[i]=new Vector3(pts[i].x,.04f,pts[i].z);v[n+i]=new Vector3(pts[i].x,height,pts[i].z);}List<int> tri=new List<int>();for(int i=0;i<top.Length;i+=3){tri.Add(n+top[i]);tri.Add(n+top[i+1]);tri.Add(n+top[i+2]);tri.Add(top[i+2]);tri.Add(top[i+1]);tri.Add(top[i]);}for(int i=0;i<n;i++){int j=(i+1)%n;tri.Add(i);tri.Add(n+i);tri.Add(n+j);tri.Add(i);tri.Add(n+j);tri.Add(j);}Mesh m=new Mesh{name="building collider",vertices=v,triangles=tri.ToArray()};m.RecalculateNormals();m.RecalculateBounds();return m;}
        static int[] Triangulate(List<Vector3> pts){int n=pts.Count;if(n<3)return Array.Empty<int>();List<int> V=new List<int>(n);if(SignedArea(pts)>0){for(int i=0;i<n;i++)V.Add(i);}else{for(int i=n-1;i>=0;i--)V.Add(i);}List<int> result=new List<int>();int nv=n,count=2*nv,v=nv-1;while(nv>2){if((count--)<=0)break;int u=v;if(nv<=u)u=0;v=u+1;if(nv<=v)v=0;int w=v+1;if(nv<=w)w=0;if(Snip(pts,u,v,w,nv,V)){result.Add(V[u]);result.Add(V[v]);result.Add(V[w]);V.RemoveAt(v);nv--;count=2*nv;}}return result.ToArray();}
        static float SignedArea(List<Vector3> p){float a=0;for(int i=0,j=p.Count-1;i<p.Count;j=i++)a+=p[j].x*p[i].z-p[i].x*p[j].z;return a*.5f;}
        static bool Snip(List<Vector3> p,int u,int v,int w,int n,List<int> V){Vector3 A=p[V[u]],B=p[V[v]],C=p[V[w]];if(((B.x-A.x)*(C.z-A.z)-(B.z-A.z)*(C.x-A.x))<=.00001f)return false;for(int i=0;i<n;i++){if(i==u||i==v||i==w)continue;if(InsideTriangle(A,B,C,p[V[i]]))return false;}return true;}
        static bool InsideTriangle(Vector3 A,Vector3 B,Vector3 C,Vector3 P){float ax=C.x-B.x,az=C.z-B.z,bx=A.x-C.x,bz=A.z-C.z,cx=B.x-A.x,cz=B.z-A.z,apx=P.x-A.x,apz=P.z-A.z,bpx=P.x-B.x,bpz=P.z-B.z,cpx=P.x-C.x,cpz=P.z-C.z;float a=ax*bpz-az*bpx,b=bx*cpz-bz*cpx,c=cx*apz-cz*apx;return a>=0&&b>=0&&c>=0;}
        static bool Inside(Vector3 p,List<Vector3> poly){bool inside=false;int j=poly.Count-1;for(int i=0;i<poly.Count;i++){float xi=poly[i].x,zi=poly[i].z,xj=poly[j].x,zj=poly[j].z;bool hit=((zi>p.z)!=(zj>p.z))&&(p.x<(xj-xi)*(p.z-zi)/(zj-zi+.00001f)+xi);if(hit)inside=!inside;j=i;}return inside;}
    }
}
#endif
