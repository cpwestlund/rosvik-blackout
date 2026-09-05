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
    public static class RosvikStylizedMapV18 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_STYLIZED_MAP_VERSION";
        const int Version = 18;
        const long MainSchoolWay = 163199458;
        const long OldSchoolWay = 163199461;
        const long ArenaWay = 163199454;

        static RosvikStylizedMapV18() {
            // Retire every older scene builder. They remain in source only as history.
            string[] oldKeys = {
                "ROSVIK_UNITY_BOOTSTRAP_VERSION",
                "ROSVIK_SITE_CALIBRATION_VERSION",
                "ROSVIK_DETAIL_PASS_VERSION",
                "ROSVIK_POLISH_PASS_VERSION",
                "ROSVIK_HERO_POLISH_VERSION",
                "ROSVIK_MAP_LOCK_VERSION",
                "ROSVIK_WORLD_PASS_VERSION",
                "ROSVIK_CLEAN_WORLD_VERSION"
            };
            foreach (string k in oldKeys) EditorPrefs.SetInt(k, 999);
            EditorApplication.delayCall += Build;
        }

        [MenuItem("Rosvik/Rebuild Stylized Rosvik V18")]
        public static void Build() {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorPrefs.GetInt(Key, 0) >= Version && File.Exists(ScenePath)) return;

            try {
                List<RosvikOsmV15.Way> ways = RosvikOsmV15.LoadWays();
                if (ways == null || ways.Count == 0) {
                    Debug.LogError("ROSVIK V18: could not load map data.");
                    return;
                }

                Directory.CreateDirectory("Assets/Rosvik/Scenes");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Transform root = new GameObject("ROSVIK_STYLIZED_MAP_V18").transform;

                // Art direction: readable illustrated map, not fake architectural realism.
                Material ground = Unlit("Ground - thawed moss", new Color(.255f,.285f,.205f));
                Material grass = Unlit("Grass - tired spring", new Color(.285f,.355f,.205f));
                Material forest = Unlit("Forest floor", new Color(.155f,.235f,.145f));
                Material schoolGround = Unlit("School ground", new Color(.33f,.32f,.27f));
                Material roadEdge = Unlit("Road edge", new Color(.075f,.083f,.085f));
                Material road = Unlit("Wet road", new Color(.125f,.137f,.14f));
                Material service = Unlit("Service road", new Color(.20f,.195f,.18f));
                Material foot = Unlit("Footpath", new Color(.37f,.34f,.285f));
                Material outline = Unlit("Building outline", new Color(.075f,.078f,.072f));
                Material shadow = Unlit("Building shadow", new Color(.10f,.105f,.095f));
                Material mainSchool = Unlit("Rosviks skola", new Color(.53f,.47f,.33f));
                Material oldSchool = Unlit("Traskolan", new Color(.72f,.54f,.22f));
                Material arena = Unlit("Norrbotten Stal Arena", new Color(.34f,.39f,.41f));
                Material pitch = Unlit("Rosvalla pitch", new Color(.235f,.445f,.19f));
                Material pitchDark = Unlit("Pitch border", new Color(.13f,.27f,.12f));
                Material pitchLine = Unlit("Pitch line", new Color(.88f,.87f,.72f));
                Material puddle = Unlit("Puddle", new Color(.16f,.235f,.255f));
                Material puddleHighlight = Unlit("Puddle highlight", new Color(.39f,.49f,.50f));
                Material snow = Unlit("Last snow", new Color(.78f,.81f,.78f));
                Material spruceDark = Unlit("Spruce shadow", new Color(.07f,.13f,.09f));
                Material spruce = Unlit("Spruce canopy", new Color(.105f,.235f,.145f));
                Material birch = Unlit("Birch canopy", new Color(.35f,.42f,.21f));
                Material bush = Unlit("Bush", new Color(.30f,.38f,.18f));
                Material playerCoat = Unlit("Player coat", new Color(.55f,.16f,.11f));
                Material playerSkin = Unlit("Player skin", new Color(.73f,.57f,.43f));
                Material playerDark = Unlit("Player dark", new Color(.095f,.10f,.095f));

                GameObject groundCollider = GameObject.CreatePrimitive(PrimitiveType.Cube);
                groundCollider.name = "GROUND COLLIDER";
                groundCollider.transform.SetParent(root,false);
                groundCollider.transform.localPosition = new Vector3(60f,-.16f,-110f);
                groundCollider.transform.localScale = new Vector3(700f,.30f,700f);
                groundCollider.GetComponent<Renderer>().sharedMaterial = ground;

                Transform land = Group(root,"01 LAND");
                Transform roads = Group(root,"02 ROADS");
                Transform sports = Group(root,"03 ROSVALLA SPORTS");
                Transform buildings = Group(root,"04 BUILDINGS");
                Transform nature = Group(root,"05 VEGETATION");
                Transform weather = Group(root,"06 PUDDLES AND LAST SNOW");

                BuildLandcover(land, ways, grass, forest, schoolGround);
                BuildRoads(roads, ways, roadEdge, road, service, foot);
                BuildPitches(sports, ways, pitchDark, pitch, pitchLine);
                BuildBuildings(buildings, ways, outline, shadow, mainSchool, oldSchool, arena);
                BuildVegetation(nature, ways, spruceDark, spruce, birch, bush);

                RosvikOsmV15.Way mainWay = ways.FirstOrDefault(w => w.Id == MainSchoolWay);
                RosvikOsmV15.Way oldWay = ways.FirstOrDefault(w => w.Id == OldSchoolWay);
                Vector3 mainCenter = mainWay != null ? RosvikOsmV15.Centroid(mainWay) : Vector3.zero;
                Vector3 oldCenter = oldWay != null ? RosvikOsmV15.Centroid(oldWay) : mainCenter + new Vector3(-80,0,-50);

                BuildWeatherAccents(weather, mainCenter, oldCenter, puddle, puddleHighlight, snow);
                GameObject player = BuildPlayer(root, mainCenter + new Vector3(-9f,.04f,14f), playerCoat, playerSkin, playerDark);
                Camera cam = BuildCamera(player.transform);
                BuildHUD(root, cam, ways);

                RenderSettings.fog = false;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = Color.white;

                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
                EditorPrefs.SetInt(Key, Version);
                Selection.activeObject = root.gameObject;
                Debug.Log("ROSVIK V18 BUILT: stylized top-down Rosvik. Geography is real; visuals are deliberately game-like and stable.");
            } catch (Exception ex) {
                Debug.LogError("ROSVIK V18 FAILED: " + ex);
            }
        }

        static void BuildLandcover(Transform parent, List<RosvikOsmV15.Way> ways, Material grass, Material forest, Material school) {
            foreach (var w in ways) {
                if (!w.Closed || w.Nodes.Count < 4 || !string.IsNullOrEmpty(w.Tag("building"))) continue;
                string land = w.Tag("landuse");
                string natural = w.Tag("natural");
                string leisure = w.Tag("leisure");
                string amenity = w.Tag("amenity");
                Material mat = null;
                float y = .006f;
                if (land == "forest" || natural == "wood") mat = forest;
                else if (land == "grass" || land == "meadow" || leisure == "park" || leisure == "garden") mat = grass;
                else if (amenity == "school") { mat = school; y = .012f; }
                if (mat) Polygon("land " + w.Id, parent, Points(w), y, mat);
            }
        }

        static void BuildRoads(Transform parent, List<RosvikOsmV15.Way> ways, Material edge, Material road, Material service, Material foot) {
            foreach (var w in ways) {
                string h = w.Tag("highway");
                if (string.IsNullOrEmpty(h) || w.Nodes.Count < 2) continue;

                float outer, inner;
                Material innerMat;
                if (h == "residential" || h == "unclassified" || h == "tertiary") { outer=6.2f; inner=5.2f; innerMat=road; }
                else if (h == "service" || h == "living_street") { outer=4.3f; inner=3.6f; innerMat=service; }
                else if (h == "footway" || h == "path" || h == "cycleway" || h == "pedestrian") { outer=2.0f; inner=1.45f; innerMat=foot; }
                else continue;

                Transform r = Group(parent,"road " + w.Id + NameSuffix(w));
                for (int i=0;i<w.Nodes.Count-1;i++) {
                    Vector3 a=w.Nodes[i].Pos, b=w.Nodes[i+1].Pos;
                    Segment("edge",r,a,b,outer,.020f,edge);
                    Segment("surface",r,a,b,inner,.025f,innerMat);
                    Disc("joint",r,a,.5f*inner,.026f,innerMat,12);
                }
                Disc("joint",r,w.Nodes[w.Nodes.Count-1].Pos,.5f*inner,.026f,innerMat,12);
            }
        }

        static void BuildPitches(Transform parent, List<RosvikOsmV15.Way> ways, Material border, Material grass, Material line) {
            foreach (var w in ways) {
                if (!w.Closed) continue;
                if (w.Tag("leisure") != "pitch" && w.Tag("sport") != "soccer") continue;
                List<Vector3> pts = Points(w);
                if (pts.Count < 3) continue;

                Polygon("pitch border " + w.Id,parent,ScaleFromCenter(pts,1.035f),.031f,border);
                Polygon("pitch " + w.Id,parent,pts,.036f,grass);
                Outline("pitch outline " + w.Id,parent,pts,.11f,.043f,line);

                RosvikOsmV15.OBounds b=RosvikOsmV15.Bounds(w);
                Vector3 axis=b.AxisX.normalized;
                Vector3 perp=new Vector3(-axis.z,0,axis.x);
                Segment("halfway",parent,b.Center-perp*b.Depth*.5f,b.Center+perp*b.Depth*.5f,.10f,.045f,line);
                Ring("centre circle",parent,b.Center,Mathf.Clamp(Mathf.Min(b.Width,b.Depth)*.11f,2.2f,8f),.10f,.046f,line,28);
            }
        }

        static void BuildBuildings(Transform parent, List<RosvikOsmV15.Way> ways, Material outline, Material shadow, Material mainSchool, Material oldSchool, Material arena) {
            Color[] housePalette = {
                new Color(.57f,.40f,.28f), new Color(.55f,.53f,.42f), new Color(.44f,.51f,.46f),
                new Color(.58f,.47f,.33f), new Color(.47f,.44f,.39f), new Color(.48f,.33f,.28f)
            };

            foreach (var w in ways) {
                string building=w.Tag("building");
                if (string.IsNullOrEmpty(building) || building=="no" || !w.Closed) continue;
                List<Vector3> pts=Points(w);
                if (pts.Count<3) continue;

                Material fill;
                if (w.Id==MainSchoolWay) fill=mainSchool;
                else if (w.Id==OldSchoolWay) fill=oldSchool;
                else if (w.Id==ArenaWay) fill=arena;
                else fill=Unlit("house " + w.Id, housePalette[Math.Abs((int)(w.Id % housePalette.Length))]);

                List<Vector3> shadowPts=pts.Select(p=>p+new Vector3(.65f,0,-.65f)).ToList();
                Polygon("shadow " + w.Id,parent,shadowPts,.041f,shadow);
                Polygon("outline " + w.Id,parent,ScaleFromCenter(pts,1.045f),.047f,outline);
                Polygon(BuildingName(w),parent,pts,.052f,fill);
                Outline("building rim " + w.Id,parent,pts,.12f,.057f,outline);

                if (w.Id==OldSchoolWay || w.Id==MainSchoolWay || w.Id==ArenaWay) {
                    RosvikOsmV15.OBounds b=RosvikOsmV15.Bounds(w);
                    Vector3 axis=b.AxisX.normalized;
                    Segment("roof cue",parent,b.Center-axis*b.Width*.40f,b.Center+axis*b.Width*.40f,.12f,.060f,outline);
                }

                AddBuildingCollider(parent,w,pts);
            }
        }

        static void AddBuildingCollider(Transform parent, RosvikOsmV15.Way w, List<Vector3> pts) {
            Mesh prism=PrismMesh(pts,2.2f);
            if (!prism) return;
            GameObject g=new GameObject("collider " + w.Id);
            g.transform.SetParent(parent,false);
            MeshCollider mc=g.AddComponent<MeshCollider>();
            mc.sharedMesh=prism;
        }

        static void BuildVegetation(Transform parent, List<RosvikOsmV15.Way> ways, Material shadow, Material spruce, Material birch, Material bush) {
            foreach (var w in ways) {
                if (!w.Closed) continue;
                string land=w.Tag("landuse"), natural=w.Tag("natural");
                if (land!="forest" && natural!="wood") continue;
                List<Vector3> poly=Points(w);
                ScatterTrees(parent,poly,w.Id,32,shadow,spruce,birch);
            }

            // School/sports area gets some game-readable greenery even when OSM has no tree nodes.
            RosvikOsmV15.Way main=ways.FirstOrDefault(w=>w.Id==MainSchoolWay);
            if (main!=null) {
                Vector3 c=RosvikOsmV15.Centroid(main);
                System.Random rng=new System.Random(18418);
                for(int i=0;i<34;i++) {
                    float a=(float)rng.NextDouble()*Mathf.PI*2f;
                    float r=28f+(float)rng.NextDouble()*58f;
                    Vector3 p=c+new Vector3(Mathf.Cos(a)*r,.01f,Mathf.Sin(a)*r);
                    if (i%4==0) Bush(parent,p,.7f+(float)rng.NextDouble()*.7f,bush);
                    else Tree(parent,p,.8f+(float)rng.NextDouble()*.65f,i%5==0?birch:spruce,shadow,i%5==0);
                }
            }
        }

        static void ScatterTrees(Transform parent,List<Vector3> poly,long seed,int max,Material shadow,Material spruce,Material birch) {
            if(poly.Count<3)return;
            float minX=poly.Min(p=>p.x),maxX=poly.Max(p=>p.x),minZ=poly.Min(p=>p.z),maxZ=poly.Max(p=>p.z);
            System.Random rng=new System.Random((int)(seed&0x7fffffff));
            int made=0;
            for(int attempt=0;attempt<max*8 && made<max;attempt++) {
                Vector3 p=new Vector3(Mathf.Lerp(minX,maxX,(float)rng.NextDouble()),.01f,Mathf.Lerp(minZ,maxZ,(float)rng.NextDouble()));
                if(!Inside(p,poly))continue;
                bool decid=made%6==0;
                Tree(parent,p,.7f+(float)rng.NextDouble()*.9f,decid?birch:spruce,shadow,decid);
                made++;
            }
        }

        static void Tree(Transform parent,Vector3 p,float s,Material canopy,Material shadow,bool deciduous) {
            Transform t=Group(parent,deciduous?"birch canopy":"spruce canopy");
            t.localPosition=p;
            Disc("tree shadow",t,new Vector3(.32f,0,-.28f),1.25f*s,.066f,shadow,14);
            Disc("tree outer",t,Vector3.zero,1.08f*s,.071f,canopy,deciduous?16:11);
            Disc("tree inner",t,new Vector3(-.18f,0,.20f),.66f*s,.073f,canopy,deciduous?14:9);
        }

        static void Bush(Transform parent,Vector3 p,float s,Material mat) {
            Transform b=Group(parent,"bush"); b.localPosition=p;
            Disc("bush a",b,new Vector3(-.25f,0,0),.55f*s,.069f,mat,10);
            Disc("bush b",b,new Vector3(.28f,0,.12f),.48f*s,.070f,mat,10);
        }

        static void BuildWeatherAccents(Transform parent,Vector3 main,Vector3 old,Material puddle,Material highlight,Material snow) {
            Blob("puddle A",parent,main+new Vector3(-15f,0,18f),new Vector2(4.7f,2.0f),12f,.064f,puddle,15,41);
            Blob("puddle B",parent,main+new Vector3(10f,0,11f),new Vector2(3.0f,1.35f),-18f,.064f,puddle,13,17);
            Blob("puddle glint",parent,main+new Vector3(-14.2f,0,18.2f),new Vector2(2.1f,.18f),12f,.066f,highlight,10,9);
            Blob("last snow old school",parent,old+new Vector3(8f,0,-9f),new Vector2(2.5f,.9f),-12f,.065f,snow,13,73);
            Blob("last snow yard",parent,main+new Vector3(-25f,0,25f),new Vector2(2.8f,.75f),25f,.065f,snow,12,31);
        }

        static GameObject BuildPlayer(Transform parent,Vector3 pos,Material coat,Material skin,Material dark) {
            GameObject p=new GameObject("PLAYER");
            p.transform.SetParent(parent,false); p.transform.localPosition=pos;
            CharacterController cc=p.AddComponent<CharacterController>(); cc.height=1.8f; cc.radius=.36f; cc.center=new Vector3(0,.9f,0); cc.stepOffset=.22f;
            p.AddComponent<RosvikTopDownPlayer>();
            Disc("player shadow",p.transform,new Vector3(.18f,.06f,-.18f),.48f,.080f,dark,16);
            Disc("coat",p.transform,Vector3.zero,.38f,.086f,coat,16);
            Disc("head",p.transform,new Vector3(0,0,.32f),.20f,.089f,skin,16);
            Segment("shoulders",p.transform,new Vector3(-.28f,0,-.02f),new Vector3(.28f,0,-.02f),.15f,.088f,coat);
            return p;
        }

        static Camera BuildCamera(Transform target) {
            GameObject go=new GameObject("Main Camera"); go.tag="MainCamera";
            Camera cam=go.AddComponent<Camera>(); cam.orthographic=true; cam.orthographicSize=24f; cam.nearClipPlane=.1f; cam.farClipPlane=120f; cam.backgroundColor=new Color(.11f,.13f,.12f); cam.clearFlags=CameraClearFlags.SolidColor;
            RosvikTopDownCamera rig=go.AddComponent<RosvikTopDownCamera>(); rig.target=target; rig.height=45f; rig.orthographicSize=24f; rig.minSize=10f; rig.maxSize=38f;
            go.transform.position=new Vector3(target.position.x,45f,target.position.z); go.transform.rotation=Quaternion.Euler(90f,0f,0f);
            return cam;
        }

        static void BuildHUD(Transform root,Camera cam,List<RosvikOsmV15.Way> ways) {
            RosvikMapHUD hud=root.gameObject.AddComponent<RosvikMapHUD>(); hud.mapCamera=cam;
            AddPoi(hud,ways,MainSchoolWay,"ROSVIKS SKOLA");
            AddPoi(hud,ways,OldSchoolWay,"TRÄSKOLAN");
            AddPoi(hud,ways,ArenaWay,"NORRBOTTEN STÅL ARENA");

            List<RosvikOsmV15.Way> pitches=ways.Where(w=>w.Closed && (w.Tag("leisure")=="pitch" || w.Tag("sport")=="soccer")).ToList();
            if(pitches.Count>0){ Vector3 c=Vector3.zero; foreach(var w in pitches)c+=RosvikOsmV15.Centroid(w); c/=pitches.Count; hud.points.Add(new RosvikMapHUD.Poi{name="ROSVALLA",worldPosition=c+Vector3.up*.15f}); }

            RosvikOsmV15.Way skolgrand=ways.FirstOrDefault(w=>w.Tag("name")=="Skolgränd");
            if(skolgrand!=null) hud.points.Add(new RosvikMapHUD.Poi{name="Skolgränd",worldPosition=RosvikOsmV15.Centroid(skolgrand)+Vector3.up*.15f});
        }

        static void AddPoi(RosvikMapHUD hud,List<RosvikOsmV15.Way> ways,long id,string name){ var w=ways.FirstOrDefault(x=>x.Id==id); if(w!=null)hud.points.Add(new RosvikMapHUD.Poi{name=name,worldPosition=RosvikOsmV15.Centroid(w)+Vector3.up*.15f}); }

        static Material Unlit(string name,Color color) {
            Shader s=GraphicsSettings.defaultRenderPipeline!=null?Shader.Find("Universal Render Pipeline/Unlit"):Shader.Find("Unlit/Color");
            if(!s||!s.isSupported)s=Shader.Find("Standard");
            Material m=new Material(s){name=name,color=color};
            if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color);
            return m;
        }

        static Transform Group(Transform parent,string name){Transform t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        static string NameSuffix(RosvikOsmV15.Way w){string n=w.Tag("name");return string.IsNullOrEmpty(n)?"":" " + n;}
        static string BuildingName(RosvikOsmV15.Way w){ if(w.Id==MainSchoolWay)return "ROSVIKS SKOLA"; if(w.Id==OldSchoolWay)return "TRÄSKOLAN"; if(w.Id==ArenaWay)return "NORRBOTTEN STÅL ARENA"; string n=w.Tag("name"); return string.IsNullOrEmpty(n)?"building " + w.Id:n; }

        static List<Vector3> Points(RosvikOsmV15.Way w){ List<Vector3> p=w.Nodes.Select(n=>n.Pos).ToList(); if(p.Count>2&&w.Closed)p.RemoveAt(p.Count-1); return p; }
        static List<Vector3> ScaleFromCenter(List<Vector3> pts,float s){Vector3 c=Vector3.zero;foreach(var p in pts)c+=p;c/=pts.Count;return pts.Select(p=>c+(p-c)*s).ToList();}

        static GameObject Polygon(string name,Transform parent,List<Vector3> pts,float y,Material mat) {
            if(pts==null||pts.Count<3)return null;
            int[] tri=Triangulate(pts); if(tri.Length<3)return null;
            Vector3[] v=pts.Select(p=>new Vector3(p.x,y,p.z)).ToArray();
            Mesh mesh=new Mesh{name=name+" mesh",vertices=v,triangles=tri};mesh.RecalculateNormals();mesh.RecalculateBounds();
            GameObject g=new GameObject(name);g.transform.SetParent(parent,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=mat;return g;
        }

        static void Outline(string name,Transform parent,List<Vector3> pts,float width,float y,Material mat){for(int i=0;i<pts.Count;i++)Segment(name,parent,pts[i],pts[(i+1)%pts.Count],width,y,mat);}

        static void Segment(string name,Transform parent,Vector3 a,Vector3 b,float width,float y,Material mat) {
            Vector3 d=b-a;d.y=0;if(d.sqrMagnitude<.0001f)return;
            Vector3 n=new Vector3(-d.z,0,d.x).normalized*width*.5f;
            Vector3[] v={new Vector3(a.x+n.x,y,a.z+n.z),new Vector3(a.x-n.x,y,a.z-n.z),new Vector3(b.x-n.x,y,b.z-n.z),new Vector3(b.x+n.x,y,b.z+n.z)};
            Mesh m=new Mesh{name=name+" mesh",vertices=v,triangles=new[]{0,2,1,0,3,2}};m.RecalculateNormals();m.RecalculateBounds();
            GameObject g=new GameObject(name);g.transform.SetParent(parent,false);g.AddComponent<MeshFilter>().sharedMesh=m;g.AddComponent<MeshRenderer>().sharedMaterial=mat;
        }

        static void Disc(string name,Transform parent,Vector3 localPos,float radius,float y,Material mat,int seg) {
            Vector3[] v=new Vector3[seg+1];int[] t=new int[seg*3];v[0]=new Vector3(localPos.x,y,localPos.z);
            for(int i=0;i<seg;i++){float a=i*Mathf.PI*2f/seg;v[i+1]=new Vector3(localPos.x+Mathf.Cos(a)*radius,y,localPos.z+Mathf.Sin(a)*radius);int j=(i+1)%seg;t[i*3]=0;t[i*3+1]=i+1;t[i*3+2]=j+1;}
            Mesh m=new Mesh{name=name+" mesh",vertices=v,triangles=t};m.RecalculateNormals();
            GameObject g=new GameObject(name);g.transform.SetParent(parent,false);g.AddComponent<MeshFilter>().sharedMesh=m;g.AddComponent<MeshRenderer>().sharedMaterial=mat;
        }

        static void Ring(string name,Transform parent,Vector3 c,float radius,float width,float y,Material mat,int seg){for(int i=0;i<seg;i++){float a=i*Mathf.PI*2f/seg,b=(i+1)*Mathf.PI*2f/seg;Segment(name,parent,c+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius),c+new Vector3(Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius),width,y,mat);}}

        static void Blob(string name,Transform parent,Vector3 center,Vector2 size,float yaw,float y,Material mat,int seg,int seed) {
            System.Random rng=new System.Random(seed);List<Vector3> p=new List<Vector3>();float r=yaw*Mathf.Deg2Rad;float cr=Mathf.Cos(r),sr=Mathf.Sin(r);
            for(int i=0;i<seg;i++){float a=i*Mathf.PI*2f/seg;float wobble=.86f+(float)rng.NextDouble()*.25f;float x=Mathf.Cos(a)*size.x*wobble,z=Mathf.Sin(a)*size.y*wobble;float rx=x*cr-z*sr,rz=x*sr+z*cr;p.Add(center+new Vector3(rx,0,rz));}
            Polygon(name,parent,p,y,mat);
        }

        static Mesh PrismMesh(List<Vector3> pts,float height) {
            int n=pts.Count;if(n<3)return null;int[] top=Triangulate(pts);if(top.Length<3)return null;
            Vector3[] v=new Vector3[n*2];for(int i=0;i<n;i++){v[i]=new Vector3(pts[i].x,0,pts[i].z);v[n+i]=new Vector3(pts[i].x,height,pts[i].z);}List<int> tri=new List<int>();
            for(int i=0;i<top.Length;i+=3){tri.Add(n+top[i]);tri.Add(n+top[i+1]);tri.Add(n+top[i+2]);tri.Add(top[i+2]);tri.Add(top[i+1]);tri.Add(top[i]);}
            for(int i=0;i<n;i++){int j=(i+1)%n;tri.Add(i);tri.Add(n+i);tri.Add(n+j);tri.Add(i);tri.Add(n+j);tri.Add(j);}Mesh m=new Mesh{name="building collider",vertices=v,triangles=tri.ToArray()};m.RecalculateNormals();m.RecalculateBounds();return m;
        }

        static int[] Triangulate(List<Vector3> pts) {
            int n=pts.Count;if(n<3)return Array.Empty<int>();List<int> V=new List<int>(n);
            if(SignedArea(pts)>0f){for(int i=0;i<n;i++)V.Add(i);}else{for(int i=n-1;i>=0;i--)V.Add(i);}List<int> result=new List<int>();int nv=n,count=2*nv,v=nv-1;
            while(nv>2){if((count--)<=0)break;int u=v;if(nv<=u)u=0;v=u+1;if(nv<=v)v=0;int w=v+1;if(nv<=w)w=0;if(Snip(pts,u,v,w,nv,V)){int a=V[u],b=V[v],c=V[w];result.Add(a);result.Add(b);result.Add(c);V.RemoveAt(v);nv--;count=2*nv;}}
            return result.ToArray();
        }

        static float SignedArea(List<Vector3> p){float a=0;for(int i=0,j=p.Count-1;i<p.Count;j=i++)a+=p[j].x*p[i].z-p[i].x*p[j].z;return a*.5f;}
        static bool Snip(List<Vector3> p,int u,int v,int w,int n,List<int> V){Vector3 A=p[V[u]],B=p[V[v]],C=p[V[w]];if(((B.x-A.x)*(C.z-A.z)-(B.z-A.z)*(C.x-A.x))<=.00001f)return false;for(int i=0;i<n;i++){if(i==u||i==v||i==w)continue;if(InsideTriangle(A,B,C,p[V[i]]))return false;}return true;}
        static bool InsideTriangle(Vector3 A,Vector3 B,Vector3 C,Vector3 P){float ax=C.x-B.x,az=C.z-B.z,bx=A.x-C.x,bz=A.z-C.z,cx=B.x-A.x,cz=B.z-A.z,apx=P.x-A.x,apz=P.z-A.z,bpx=P.x-B.x,bpz=P.z-B.z,cpx=P.x-C.x,cpz=P.z-C.z;float a=ax*bpz-az*bpx,b=bx*cpz-bz*cpx,c=cx*apz-cz*apx;return a>=0&&b>=0&&c>=0;}
        static bool Inside(Vector3 p,List<Vector3> poly){bool inside=false;int j=poly.Count-1;for(int i=0;i<poly.Count;i++){float xi=poly[i].x,zi=poly[i].z,xj=poly[j].x,zj=poly[j].z;bool hit=((zi>p.z)!=(zj>p.z))&&(p.x<(xj-xi)*(p.z-zi)/(zj-zi+.00001f)+xi);if(hit)inside=!inside;j=i;}return inside;}
    }
}
#endif
