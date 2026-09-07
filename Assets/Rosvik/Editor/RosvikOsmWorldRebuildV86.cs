#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Rosvik.Blackout;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikOsmWorldRebuildV86 {
        const int Version = 86;
        const string Key = "ROSVIK_OSM_WORLD_REBUILD_V86";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string RootName = "V86 OSM WORLD REBUILD";
        const string Generated = "Assets/Rosvik/GeneratedV86";
        const string OsmCache = Generated + "/rosvik_overpass_cache.xml";
        const string Overpass = "https://overpass-api.de/api/interpreter";
        const double OriginLat = 65.426266;
        const double OriginLon = 21.692577;
        const float RotationDeg = 33f;
        const float MapScale = .72f;
        const float MapZOffset = 7.3f;

        sealed class Feature {
            public string id, highway, building, leisure, name, surface, amenity, sport;
            public readonly List<Vector2> points = new List<Vector2>();
            public bool IsRoad => !string.IsNullOrEmpty(highway);
            public bool IsBuilding => !string.IsNullOrEmpty(building);
            public bool IsPitch => leisure == "pitch";
        }

        static Shader shader;
        static Material snow, snowBank, packedSnow, asphalt, gravel, roadEdge, forest;
        static Material red, ochre, blue, cream, brick, trim, glass, warmGlass, roof, metal, wood, darkWood, pitch, line, schoolFloor, hallFloor;
        static Mesh coneMesh;
        static List<Feature> features;

        static RosvikOsmWorldRebuildV86() {
            if (EditorPrefs.GetInt(Key,0) >= Version) return;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V86 OSM WORLD REBUILD - KEEP ALL SYSTEMS")]
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
            catch (Exception ex) { Debug.LogError("V86 OSM WORLD REBUILD FAILED: " + ex); }
        }

        static void Apply() {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);

            CoziPlayerV57 player = UnityEngine.Object.FindFirstObjectByType<CoziPlayerV57>();
            if (!player) throw new Exception("PLAYER missing");

            GameObject old = GameObject.Find(RootName);
            if (old) UnityEngine.Object.DestroyImmediate(old);

            Directory.CreateDirectory(Generated);
            shader = PickShader();
            BuildMaterials();
            coneMesh = EnsureConeMesh();
            features = LoadOsm();
            if (features.Count < 20) throw new Exception("OSM download/cache contained too few features: " + features.Count);

            SuppressLegacyOutsideVisuals();
            RestyleCoreInteriors();

            GameObject root = new GameObject(RootName);
            Transform terrain = Group(root.transform,"01 CONTINUOUS WINTER GROUND");
            Transform roads = Group(root.transform,"02 OPENSTREETMAP ROADS");
            Transform buildings = Group(root.transform,"03 OPENSTREETMAP BUILDINGS");
            Transform grounds = Group(root.transform,"04 SCHOOL + SPORTS GROUNDS");
            Transform nature = Group(root.transform,"05 VEGETATION");
            Transform hero = Group(root.transform,"06 AUTHORED CORE EXTERIORS");
            Transform details = Group(root.transform,"07 WORLD DETAILS");

            Bounds mapBounds = ComputeBounds();
            BuildSnowTerrain(terrain,mapBounds);
            BuildRoads(roads,details);
            BuildBuildings(buildings);
            BuildGroundFeatures(grounds);
            BuildForest(nature,mapBounds);
            BuildCoreWorldDetails(details);

            GameObject schoolShell = BuildSchoolShell(hero);
            GameObject hallShell = BuildSportHallShell(hero);

            OsmWorldRuntimeV86 runtime = root.AddComponent<OsmWorldRuntimeV86>();
            runtime.attribution = "© OpenStreetMap contributors · ODbL 1.0";
            runtime.cutaways = new [] {
                new CutawayTargetV86 { visualRoot=schoolShell, minXZ=new Vector2(-18.5f,-1.4f), maxXZ=new Vector2(18.5f,16.0f), padding=.72f },
                new CutawayTargetV86 { visualRoot=hallShell, minXZ=new Vector2(23.0f,-2.5f), maxXZ=new Vector2(45.0f,16.6f), padding=.82f }
            };
            EditorUtility.SetDirty(runtime);

            TuneScene();
            CleanupMissingScripts();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorPrefs.SetInt(Key,Version);
            SceneView.RepaintAll();
            Debug.Log("V86 COMPLETE — Rosvik world rebuilt from OpenStreetMap while gameplay, inventory, loot, weather, clothing, survival and progression systems stay intact.");
        }

        static List<Feature> LoadOsm() {
            string xml = null;
            if (File.Exists(OsmCache)) {
                try { xml = File.ReadAllText(OsmCache); }
                catch { xml = null; }
            }
            if (string.IsNullOrWhiteSpace(xml) || !xml.Contains("<osm")) {
                string q = "[out:xml][timeout:35];(way[highway](65.414,21.665,65.439,21.721);way[building](65.414,21.665,65.439,21.721);way[leisure=pitch](65.414,21.665,65.439,21.721););out geom;";
                using (WebClient wc = new WebClient()) {
                    wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded; charset=UTF-8";
                    wc.Headers[HttpRequestHeader.UserAgent] = "Rosvik-Blackout-Unity-V86";
                    xml = wc.UploadString(Overpass,"POST","data=" + Uri.EscapeDataString(q));
                }
                File.WriteAllText(OsmCache,xml);
                AssetDatabase.ImportAsset(OsmCache,ImportAssetOptions.ForceSynchronousImport);
            }
            return ParseOsm(xml);
        }

        static List<Feature> ParseOsm(string xml) {
            XDocument doc = XDocument.Parse(xml);
            List<Feature> list = new List<Feature>();
            foreach (XElement w in doc.Descendants("way")) {
                Feature f = new Feature();
                f.id = (string)w.Attribute("id") ?? "";
                foreach (XElement tag in w.Elements("tag")) {
                    string k=(string)tag.Attribute("k")??"", v=(string)tag.Attribute("v")??"";
                    if(k=="highway")f.highway=v; else if(k=="building")f.building=v; else if(k=="leisure")f.leisure=v;
                    else if(k=="name")f.name=v; else if(k=="surface")f.surface=v; else if(k=="amenity")f.amenity=v; else if(k=="sport")f.sport=v;
                }
                if (string.IsNullOrEmpty(f.highway) && string.IsNullOrEmpty(f.building) && f.leisure!="pitch") continue;
                foreach (XElement nd in w.Elements("nd")) {
                    XAttribute la=nd.Attribute("lat"), lo=nd.Attribute("lon"); if(la==null||lo==null)continue;
                    double lat=double.Parse(la.Value,CultureInfo.InvariantCulture), lon=double.Parse(lo.Value,CultureInfo.InvariantCulture);
                    f.points.Add(ToLocal(lat,lon));
                }
                if(f.points.Count>=2)list.Add(f);
            }
            return list;
        }

        static Vector2 ToLocal(double lat,double lon) {
            double east=(lon-OriginLon)*111320.0*Math.Cos(OriginLat*Math.PI/180.0);
            double north=(lat-OriginLat)*110540.0;
            double a=RotationDeg*Math.PI/180.0;
            double x=east*Math.Cos(a)-north*Math.Sin(a);
            double z=east*Math.Sin(a)+north*Math.Cos(a);
            return new Vector2((float)x,(float)z);
        }

        static Vector3 Map(Vector2 p,float y=0f) => new Vector3(p.x*MapScale,y,p.y*MapScale+MapZOffset);

        static void BuildMaterials() {
            snow=PatternMat("snow_terrain","cad5d8","aebfc4",Pattern.Noise,.08f,4f);
            snowBank=PatternMat("snow_bank","b9c8cc","91a5ac",Pattern.Noise,.06f,3f);
            packedSnow=PatternMat("packed_snow","849aa2","687e86",Pattern.Noise,.05f,5f);
            asphalt=PatternMat("asphalt","353d40","23292d",Pattern.Noise,.07f,7f);
            gravel=PatternMat("gravel","70716d","525856",Pattern.Noise,.04f,8f);
            roadEdge=PatternMat("road_edge","6c858d","526b73",Pattern.Noise,.05f,5f);
            forest=PatternMat("forest_green","324d43","203a33",Pattern.Noise,.05f,4f);
            red=PatternMat("facade_red","74433e","50312d",Pattern.Boards,.09f,4f);
            ochre=PatternMat("facade_ochre","a57d45","735732",Pattern.Boards,.09f,4f);
            blue=PatternMat("facade_blue","426272","2b4654",Pattern.Boards,.09f,4f);
            cream=PatternMat("facade_cream","c4b99e","9d9278",Pattern.Boards,.09f,4f);
            brick=PatternMat("brick","71473f","49302c",Pattern.Brick,.08f,6f);
            trim=FlatMat("warm_trim","d8d2c2",.15f);
            glass=FlatMat("cold_glass","39535e",.40f);
            warmGlass=EmissiveMat("warm_window","f1bf70",1.25f);
            roof=PatternMat("roof","364852","26353e",Pattern.Roof,.12f,5f);
            metal=FlatMat("metal","3b4b50",.32f);
            wood=PatternMat("wood","896849","604936",Pattern.Wood,.12f,5f);
            darkWood=PatternMat("dark_wood","594235","3a2e28",Pattern.Wood,.11f,5f);
            pitch=FlatMat("pitch","62786f",.05f);
            line=FlatMat("pitch_line","d8d8ca",.07f);
            schoolFloor=PatternMat("school_linoleum","6b756f","535e59",Pattern.Noise,.11f,5f);
            hallFloor=PatternMat("hall_floor","916a49","654b35",Pattern.Wood,.15f,4f);
        }

        static Bounds ComputeBounds() {
            bool first=true;Bounds b=new Bounds();
            foreach(Feature f in features)foreach(Vector2 p in f.points){Vector3 w=Map(p);if(first){b=new Bounds(w,Vector3.zero);first=false;}else b.Encapsulate(w);}
            b.Expand(new Vector3(48f,0,48f));
            return b;
        }

        static void BuildSnowTerrain(Transform p,Bounds b) {
            float step=9f; int nx=Mathf.CeilToInt(b.size.x/step)+1,nz=Mathf.CeilToInt(b.size.z/step)+1;
            Vector3[] v=new Vector3[nx*nz];Vector2[] uv=new Vector2[v.Length];int[] tr=new int[(nx-1)*(nz-1)*6];
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++){
                float wx=b.min.x+x*step,wz=b.min.z+z*step;
                float n=(Mathf.PerlinNoise((wx+300f)*.024f,(wz+300f)*.024f)-.5f)*.11f;
                float n2=(Mathf.PerlinNoise((wx-40f)*.075f,(wz+90f)*.075f)-.5f)*.025f;
                int i=z*nx+x;v[i]=new Vector3(wx,-.085f+n+n2,wz);uv[i]=new Vector2(wx*.035f,wz*.035f);
            }
            int ti=0;for(int z=0;z<nz-1;z++)for(int x=0;x<nx-1;x++){int i=z*nx+x;tr[ti++]=i;tr[ti++]=i+nx;tr[ti++]=i+1;tr[ti++]=i+1;tr[ti++]=i+nx;tr[ti++]=i+nx+1;}
            Mesh m=new Mesh();m.name="V86 continuous snow";m.vertices=v;m.uv=uv;m.triangles=tr;m.RecalculateNormals();m.RecalculateBounds();
            GameObject g=MeshObject("continuous snow terrain",p,m,snow,false);
            BoxCollider c=g.AddComponent<BoxCollider>();c.center=new Vector3(b.center.x,-.18f,b.center.z);c.size=new Vector3(b.size.x,.18f,b.size.z);
        }

        static void BuildRoads(Transform p,Transform detail) {
            int signs=0;
            foreach(Feature f in features.Where(x=>x.IsRoad)) {
                float width=RoadWidth(f.highway)*MapScale;Material mat=f.surface=="unpaved"?gravel:asphalt;
                for(int i=0;i<f.points.Count-1;i++){
                    Vector3 a=Map(f.points[i],.012f),b=Map(f.points[i+1],.012f);if(Vector3.Distance(a,b)<.05f)continue;
                    SlabBetween("ploughed road shoulder",p,a-Vector3.up*.018f,b-Vector3.up*.018f,width+1.18f,roadEdge,false);
                    SlabBetween((string.IsNullOrEmpty(f.name)?f.highway:f.name),p,a,b,width,mat,false);
                    if((f.highway=="secondary"||f.highway=="tertiary")&&Vector3.Distance(a,b)>6f)RoadDashes(p,a,b);
                }
                if(!string.IsNullOrEmpty(f.name)&&signs<10){Vector3 q=Map(f.points[Mathf.Min(1,f.points.Count-1)]);if(new Vector2(q.x,q.z).sqrMagnitude<190f*190f){StreetSign(detail,q+new Vector3(1.4f,0,1f),f.name);signs++;}}
            }
        }

        static float RoadWidth(string h){switch(h){case"secondary":return 6.5f;case"tertiary":return 5.8f;case"residential":return 4.8f;case"service":return 3.3f;case"cycleway":return 2.1f;case"footway":return 1.4f;case"track":return 2.7f;default:return 3.2f;}}
        static void RoadDashes(Transform p,Vector3 a,Vector3 b){Vector3 d=b-a;float len=d.magnitude;if(len<6f)return;Vector3 dir=d/len;float yaw=Mathf.Atan2(dir.x,dir.z)*Mathf.Rad2Deg;for(float t=4f;t<len-2f;t+=9f)Box("road dash",p,a+dir*t+Vector3.up*.024f,new Vector3(.08f,.012f,2.6f),trim,false,Quaternion.Euler(0,yaw,0));}

        static void BuildBuildings(Transform parent) {
            List<Feature> list=features.Where(f=>f.IsBuilding&&f.points.Count>=4&&!IsCoreOsmBuilding(f)).OrderBy(f=>Centroid(f.points).sqrMagnitude).Take(170).ToList();
            int i=0;foreach(Feature f in list){List<Vector2> pts=Clean(f.points);if(pts.Count<3)continue;Transform r=Group(parent,"OSM building "+f.id+(string.IsNullOrEmpty(f.name)?"":" · "+f.name));Material facade=FacadeFor(f,i++);float h=HeightFor(f);Walls(r,pts,h,facade);RoofFor(r,pts,h);FacadeWindows(r,pts,h);}
        }

        static bool IsCoreOsmBuilding(Feature f){string n=(f.name??"").ToLowerInvariant();return n.Contains("rosviks skola")||n.Contains("norrbotten stål arena")||f.sport=="ice_hockey";}
        static Material FacadeFor(Feature f,int i){if(f.building=="school")return ochre;if((f.amenity??"").Contains("kindergarten"))return cream;switch(i%4){case 0:return red;case 1:return cream;case 2:return blue;default:return ochre;}}
        static float HeightFor(Feature f){if(f.building=="school")return 3.15f;if((f.amenity??"")=="kindergarten")return 2.7f;return 2.55f;}

        static void Walls(Transform p,List<Vector2> pts,float h,Material facade){for(int i=0;i<pts.Count;i++){Vector3 a=Map(pts[i],h*.5f),b=Map(pts[(i+1)%pts.Count],h*.5f);Vector3 d=b-a;float len=new Vector2(d.x,d.z).magnitude;if(len<.08f)continue;float yaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;Vector3 mid=(a+b)*.5f;Box("wall",p,mid,new Vector3(.18f,h,len+.03f),facade,true,Quaternion.Euler(0,yaw,0));Box("brick/plinth",p,new Vector3(mid.x,.31f,mid.z),new Vector3(.205f,.60f,len+.05f),brick,false,Quaternion.Euler(0,yaw,0));}}

        static void RoofFor(Transform p,List<Vector2> pts,float h){
            Vector2 c2=Centroid(pts);Vector3 center=Map(c2);int li=0;float best=0;for(int i=0;i<pts.Count;i++){float l=Vector2.Distance(pts[i],pts[(i+1)%pts.Count]);if(l>best){best=l;li=i;}}
            Vector2 e=pts[(li+1)%pts.Count]-pts[li];float yaw=Mathf.Atan2(e.x,e.y)*Mathf.Rad2Deg;Quaternion q=Quaternion.Euler(0,yaw,0),inv=Quaternion.Inverse(q);
            float minX=999,maxX=-999,minZ=999,maxZ=-999;foreach(Vector2 pp in pts){Vector3 l=inv*(Map(pp)-center);minX=Mathf.Min(minX,l.x);maxX=Mathf.Max(maxX,l.x);minZ=Mathf.Min(minZ,l.z);maxZ=Mathf.Max(maxZ,l.z);}
            float w=maxX-minX,d=maxZ-minZ;if(w<1f||d<1f)return;if(w>d){float t=w;w=d;d=t;yaw+=90f;q=Quaternion.Euler(0,yaw,0);}
            float pitchDeg=Mathf.Clamp(13f-w*.11f,7f,14f),half=w*.5f,rad=pitchDeg*Mathf.Deg2Rad,rise=half*Mathf.Tan(rad),len=half/Mathf.Cos(rad);
            GameObject rr=new GameObject("gable roof");rr.transform.SetParent(p,true);rr.transform.position=center;rr.transform.rotation=q;
            GameObject lft=BoxLocal("roof left",rr.transform,new Vector3(-half*.5f,h+rise*.5f,0),new Vector3(len+.10f,.15f,d+.32f),roof,false,Quaternion.Euler(0,0,pitchDeg));
            GameObject rgt=BoxLocal("roof right",rr.transform,new Vector3(half*.5f,h+rise*.5f,0),new Vector3(len+.10f,.15f,d+.32f),roof,false,Quaternion.Euler(0,0,-pitchDeg));
            BoxLocal("snow left",rr.transform,new Vector3(-half*.5f+.015f,h+rise*.5f+.095f,0),new Vector3(len-.05f,.035f,d+.17f),snow,false,lft.transform.localRotation);
            BoxLocal("snow right",rr.transform,new Vector3(half*.5f-.015f,h+rise*.5f+.095f,0),new Vector3(len-.05f,.035f,d+.17f),snow,false,rgt.transform.localRotation);
            BoxLocal("ridge",rr.transform,new Vector3(0,h+rise+.065f,0),new Vector3(.13f,.10f,d+.38f),metal,false,Quaternion.identity);
        }

        static void FacadeWindows(Transform p,List<Vector2> pts,float h){int li=0;float best=0;for(int i=0;i<pts.Count;i++){float l=Vector2.Distance(pts[i],pts[(i+1)%pts.Count]);if(l>best){best=l;li=i;}}Vector3 a=Map(pts[li]),b=Map(pts[(li+1)%pts.Count]),d=(b-a).normalized;if(Vector3.Distance(a,b)<4.8f)return;int count=Mathf.Clamp(Mathf.FloorToInt(Vector3.Distance(a,b)/3.1f),2,5);for(int i=1;i<=count;i++)WindowPanel(p,Vector3.Lerp(a,b,i/(float)(count+1))+Vector3.up*(h*.57f),d,h>3f?1.2f:.92f,i%3==0?warmGlass:glass);}

        static void BuildGroundFeatures(Transform p){foreach(Feature f in features.Where(x=>x.IsPitch)){List<Vector2> pts=Clean(f.points);if(pts.Count<3)continue;Mesh m=Polygon(pts,.018f);GameObject g=MeshObject(string.IsNullOrEmpty(f.name)?"sports pitch":f.name,p,m,pitch,false);Bounds b=g.GetComponent<Renderer>().bounds;Box("pitch center line",p,new Vector3(b.center.x,.035f,b.center.z),new Vector3(.07f,.012f,b.size.z*.88f),line,false);}}

        static void BuildForest(Transform p,Bounds b){System.Random rng=new System.Random(8609);int placed=0,attempts=0;while(placed<175&&attempts<2600){attempts++;float x=Mathf.Lerp(b.min.x,b.max.x,(float)rng.NextDouble()),z=Mathf.Lerp(b.min.z,b.max.z,(float)rng.NextDouble());Vector2 q=new Vector2(x,z);if(q.magnitude<34f||NearRoad(q,5f)||NearBuilding(q,3.5f))continue;Pine(p,new Vector3(x,0,z),.70f+(float)rng.NextDouble()*.78f,placed%6==0);placed++;}for(int i=0;i<105;i++){float x=Mathf.Lerp(b.min.x,b.max.x,(float)rng.NextDouble()),z=Mathf.Lerp(b.min.z,b.max.z,(float)rng.NextDouble());Vector2 q=new Vector2(x,z);if(q.magnitude<27f||NearRoad(q,2.8f)||NearBuilding(q,2.5f))continue;Bush(p,new Vector3(x,.03f,z),.42f+(float)rng.NextDouble()*.6f);}}
        static bool NearRoad(Vector2 p,float dist){float d2=dist*dist;foreach(Feature f in features.Where(x=>x.IsRoad))for(int i=0;i<f.points.Count-1;i++){Vector2 a=W2(f.points[i]),b=W2(f.points[i+1]);if(DistSegSq(p,a,b)<d2)return true;}return false;}
        static bool NearBuilding(Vector2 p,float dist){foreach(Feature f in features.Where(x=>x.IsBuilding)){List<Vector2> pts=Clean(f.points);if(pts.Count<3)continue;Vector2 c=W2(Centroid(pts));float r=dist;foreach(Vector2 q in pts)r=Mathf.Max(r,Vector2.Distance(c,W2(q))+dist);if(Vector2.Distance(p,c)<r)return true;}return false;}
        static Vector2 W2(Vector2 q)=>new Vector2(q.x*MapScale,q.y*MapScale+MapZOffset);
        static float DistSegSq(Vector2 p,Vector2 a,Vector2 b){Vector2 ab=b-a;float t=ab.sqrMagnitude<.0001f?0:Mathf.Clamp01(Vector2.Dot(p-a,ab)/ab.sqrMagnitude);return(p-(a+ab*t)).sqrMagnitude;}

        static void Pine(Transform p,Vector3 pos,float scale,bool heavy){GameObject r=new GameObject("pine");r.transform.SetParent(p,true);r.transform.position=pos;r.transform.localScale=Vector3.one*scale;Cylinder("trunk",r.transform,new Vector3(0,.72f,0),.13f,1.45f,darkWood,false);for(int i=0;i<3;i++){float y=.75f+i*.58f,rad=1.08f-i*.21f,hh=.96f-i*.08f;Cone("needles",r.transform,new Vector3(0,y,0),rad,hh,forest);if(heavy||i==2)Cone("snow",r.transform,new Vector3(0,y+.08f,0),rad*.91f,hh*.24f,snowBank);}}
        static void Bush(Transform p,Vector3 pos,float s){GameObject r=new GameObject("winter shrub");r.transform.SetParent(p,true);r.transform.position=pos;r.transform.localScale=Vector3.one*s;for(int i=0;i<3;i++){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name="branch mass";g.transform.SetParent(r.transform,false);g.transform.localPosition=new Vector3((i-1)*.34f,.23f+(i%2)*.09f,(i%2==0?.12f:-.12f));g.transform.localScale=new Vector3(.62f,.35f,.50f);g.GetComponent<Renderer>().sharedMaterial=forest;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());}}

        static void BuildCoreWorldDetails(Transform p){Box("school forecourt",p,new Vector3(0,-.012f,-5.0f),new Vector3(34f,.035f,7.0f),packedSnow,false);Box("school entrance walk",p,new Vector3(0,.005f,-1.6f),new Vector3(5.0f,.025f,7.0f),roadEdge,false);for(int i=0;i<6;i++)LampPost(p,new Vector3(-18f+i*7.2f,0,-9.2f+(i%2)*.35f));FenceRun(p,new Vector3(-39f,0,20f),new Vector3(-52f,0,20f));}

        static GameObject BuildSchoolShell(Transform p){GameObject r=new GameObject("ROSVIKS SKOLA EXTERIOR V86");r.transform.SetParent(p,true);Transform t=r.transform;float h=2.75f,front=-1.1f,back=15.85f,min=-18.2f,max=18.2f;WallSegment(t,new Vector3(min,h*.5f,front),new Vector3(-2.0f,h*.5f,front),h,ochre,true);WallSegment(t,new Vector3(2.0f,h*.5f,front),new Vector3(max,h*.5f,front),h,ochre,true);WallSegment(t,new Vector3(min,h*.5f,back),new Vector3(max,h*.5f,back),h,ochre,true);WallSegment(t,new Vector3(min,h*.5f,front),new Vector3(min,h*.5f,back),h,ochre,true);WallSegment(t,new Vector3(max,h*.5f,front),new Vector3(max,h*.5f,1.18f),h,ochre,true);WallSegment(t,new Vector3(max,h*.5f,3.82f),new Vector3(max,h*.5f,back),h,ochre,true);Box("front brick L",t,new Vector3(-10.1f,.38f,front-.11f),new Vector3(16.1f,.72f,.20f),brick,false);Box("front brick R",t,new Vector3(10.1f,.38f,front-.11f),new Vector3(16.1f,.72f,.20f),brick,false);for(int i=0;i<5;i++)WindowPanel(t,new Vector3(-14f+i*7f,1.55f,front-.15f),Vector3.right,1.65f,i%2==0?warmGlass:glass);SchoolRoof(t,new Vector3(0,0,7.35f),36.6f,17.3f,2.83f);SchoolEntrance(t,new Vector3(0,0,front));return r;}
        static GameObject BuildSportHallShell(Transform p){GameObject r=new GameObject("SPORT HALL EXTERIOR V86");r.transform.SetParent(p,true);Transform t=r.transform;float h=3.65f,minX=23.3f,maxX=44.7f,front=-2.15f,back=16.2f;WallSegment(t,new Vector3(minX,h*.5f,front),new Vector3(maxX,h*.5f,front),h,blue,true);WallSegment(t,new Vector3(minX,h*.5f,back),new Vector3(maxX,h*.5f,back),h,blue,true);WallSegment(t,new Vector3(minX,h*.5f,front),new Vector3(minX,h*.5f,1.18f),h,blue,true);WallSegment(t,new Vector3(minX,h*.5f,3.82f),new Vector3(minX,h*.5f,back),h,blue,true);WallSegment(t,new Vector3(maxX,h*.5f,front),new Vector3(maxX,h*.5f,back),h,blue,true);Box("hall brick base",t,new Vector3(34f,.42f,front-.11f),new Vector3(21.5f,.78f,.20f),brick,false);for(int i=0;i<4;i++)WindowPanel(t,new Vector3(27f+i*4.8f,2.15f,front-.14f),Vector3.right,1.35f,glass);Box("hall roof",t,new Vector3(34f,h+.12f,7f),new Vector3(21.8f,.22f,18.8f),roof,false);Box("hall snow",t,new Vector3(34f,h+.255f,7f),new Vector3(21.55f,.05f,18.55f),snow,false);return r;}
        static void SchoolEntrance(Transform p,Vector3 d){Box("entrance canopy",p,d+new Vector3(0,2.42f,-1.05f),new Vector3(6f,.18f,2.15f),roof,false,Quaternion.Euler(4f,0,0));Box("canopy snow",p,d+new Vector3(0,2.54f,-1.05f),new Vector3(5.7f,.04f,1.95f),snow,false,Quaternion.Euler(4f,0,0));Box("post L",p,d+new Vector3(-2.5f,1.18f,-1.5f),new Vector3(.12f,2.35f,.12f),metal,false);Box("post R",p,d+new Vector3(2.5f,1.18f,-1.5f),new Vector3(.12f,2.35f,.12f),metal,false);Box("entrance glow",p,d+new Vector3(0,1.72f,-.22f),new Vector3(.42f,.42f,.18f),warmGlass,false);GameObject sign=Box("school sign",p,d+new Vector3(0,2.04f,-.17f),new Vector3(3.7f,.46f,.10f),metal,false);TextLabel(sign.transform,"ROSVIKS SKOLA",new Vector3(0,0,-.065f),Quaternion.Euler(0,180,0),.18f);}
        static void SchoolRoof(Transform p,Vector3 c,float w,float d,float eave){float pd=7.5f,half=w*.5f,rad=pd*Mathf.Deg2Rad,rise=half*Mathf.Tan(rad),len=half/Mathf.Cos(rad),xo=half*.5f,py=eave+rise*.5f;GameObject l=Box("school roof west",p,c+new Vector3(-xo,py,0),new Vector3(len+.12f,.18f,d+.45f),roof,false,Quaternion.Euler(0,0,pd));GameObject r=Box("school roof east",p,c+new Vector3(xo,py,0),new Vector3(len+.12f,.18f,d+.45f),roof,false,Quaternion.Euler(0,0,-pd));Box("school snow west",p,c+new Vector3(-xo+.02f,py+.11f,0),new Vector3(len-.10f,.04f,d+.18f),snow,false,l.transform.rotation);Box("school snow east",p,c+new Vector3(xo-.02f,py+.11f,0),new Vector3(len-.10f,.04f,d+.18f),snow,false,r.transform.rotation);}

        static void RestyleCoreInteriors(){GameObject root=GameObject.Find("COZY SCHOOL GAME V58");if(!root)return;Transform s=root.transform.Find("SCHOOL INTERIOR"),h=root.transform.Find("SPORT HALL");if(s)foreach(Renderer r in s.GetComponentsInChildren<Renderer>(true)){string n=r.gameObject.name.ToLowerInvariant();if(n.Contains("wall")||n.Contains("cap")||n.Contains("trim"))r.sharedMaterial=cream;else if(n.Contains("corridor")||n.Contains("class")||n.Contains("staff")||n.Contains("library")||n.Contains("janitor"))r.sharedMaterial=schoolFloor;}if(h)foreach(Renderer r in h.GetComponentsInChildren<Renderer>(true)){string n=r.gameObject.name.ToLowerInvariant();if(n.Contains("wall")||n.Contains("cap"))r.sharedMaterial=cream;else if(n.Contains("floor")||n.Contains("court"))r.sharedMaterial=hallFloor;}}

        static void SuppressLegacyOutsideVisuals(){GameObject v58=GameObject.Find("COZY SCHOOL GAME V58");if(v58){Transform c=v58.transform.Find("CAMPUS OUTSIDE");if(c)DisableVisuals(c,true);}foreach(string n in new[]{"V80 COZY WINTER WORLD + SOUND","V83 ASTRA REFERENCE POLISH","V84 GRAPHICAL OVERHAUL"}){GameObject g=GameObject.Find(n);if(g)DisableVisuals(g.transform,true);}foreach(string n in new[]{"V78 WORLD - GARAGE + VEGETATION","V79 NORTH NEIGHBORHOOD - HOUSE B"}){GameObject g=GameObject.Find(n);if(!g)continue;foreach(Transform t in g.GetComponentsInChildren<Transform>(true)){string q=t.name.ToLowerInvariant();if(!LegacyDecor(q))continue;Renderer r=t.GetComponent<Renderer>();if(r)r.enabled=false;Collider c=t.GetComponent<Collider>();if(c)c.enabled=false;}}}
        static bool LegacyDecor(string n){foreach(string w in new[]{"pine","birch","tree","bush","shrub","grass","snow drift","snowbank","forest","road","driveway","parking","footpath","fence","streetlight","lamp post","mailbox","trash","bench","stump","rock"})if(n.Contains(w))return true;return false;}
        static void DisableVisuals(Transform t,bool col){foreach(Renderer r in t.GetComponentsInChildren<Renderer>(true))r.enabled=false;if(col)foreach(Collider c in t.GetComponentsInChildren<Collider>(true))c.enabled=false;}

        static void TuneScene(){RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=Hex("71818a")*.58f;RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=Hex("7f929c");RenderSettings.fogDensity=.0022f;Light sun=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(x=>x&&x.type==LightType.Directional);if(sun){sun.color=Hex("d7e0df");sun.intensity=.52f;sun.transform.rotation=Quaternion.Euler(43f,-32f,0);EditorUtility.SetDirty(sun);}Camera cam=Camera.main;if(cam){cam.backgroundColor=Hex("607681");EditorUtility.SetDirty(cam);}}

        static List<Vector2> Clean(List<Vector2> src){List<Vector2> r=new List<Vector2>(src);if(r.Count>1&&Vector2.Distance(r[0],r[r.Count-1])<.05f)r.RemoveAt(r.Count-1);return r;}
        static Vector2 Centroid(IList<Vector2> pts){Vector2 c=Vector2.zero;for(int i=0;i<pts.Count;i++)c+=pts[i];return c/Mathf.Max(1,pts.Count);}
        static Mesh Polygon(List<Vector2> pts,float y){Vector3 c=Vector3.zero;foreach(Vector2 p in pts)c+=Map(p,y);c/=pts.Count;Vector3[] v=new Vector3[pts.Count+1];v[0]=c;for(int i=0;i<pts.Count;i++)v[i+1]=Map(pts[i],y);int[] tr=new int[pts.Count*3];for(int i=0;i<pts.Count;i++){tr[i*3]=0;tr[i*3+1]=i+1;tr[i*3+2]=(i+1)%pts.Count+1;}Mesh m=new Mesh();m.name="OSM polygon";m.vertices=v;m.triangles=tr;m.RecalculateNormals();m.RecalculateBounds();return m;}

        static void WindowPanel(Transform p,Vector3 c,Vector3 along,float width,Material pane){float yaw=Mathf.Atan2(along.x,along.z)*Mathf.Rad2Deg;Quaternion q=Quaternion.Euler(0,yaw,0);Box("window frame",p,c,new Vector3(.15f,1.28f,width+.24f),trim,false,q);Box("window glass",p,c,new Vector3(.17f,.98f,width),pane,false,q);}
        static void WallSegment(Transform p,Vector3 a,Vector3 b,float h,Material m,bool col){Vector3 d=b-a;float len=new Vector2(d.x,d.z).magnitude;if(len<.02f)return;float yaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;Box("wall",p,(a+b)*.5f,new Vector3(.18f,h,len),m,col,Quaternion.Euler(0,yaw,0));}
        static void StreetSign(Transform p,Vector3 pos,string text){Box("sign post",p,pos+Vector3.up*.72f,new Vector3(.08f,1.45f,.08f),metal,false);GameObject b=Box("street sign",p,pos+Vector3.up*1.38f,new Vector3(1.9f,.36f,.08f),blue,false);TextLabel(b.transform,text,new Vector3(0,0,-.05f),Quaternion.Euler(0,180,0),.12f);}
        static void TextLabel(Transform p,string text,Vector3 lp,Quaternion rot,float size){GameObject g=new GameObject("label");g.transform.SetParent(p,false);g.transform.localPosition=lp;g.transform.localRotation=rot;TextMesh tm=g.AddComponent<TextMesh>();tm.text=text;tm.fontSize=48;tm.characterSize=size;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;tm.color=new Color(.93f,.92f,.86f);}
        static void LampPost(Transform p,Vector3 pos){Cylinder("lamp post",p,pos+Vector3.up*1.55f,.045f,3.1f,metal,false);Box("lamp head",p,pos+new Vector3(.18f,3.0f,0),new Vector3(.45f,.13f,.20f),metal,false);}
        static void FenceRun(Transform p,Vector3 a,Vector3 b){float len=Vector3.Distance(a,b);int n=Mathf.Max(2,Mathf.CeilToInt(len/2.3f));for(int i=0;i<=n;i++)Box("fence post",p,Vector3.Lerp(a,b,i/(float)n)+Vector3.up*.48f,new Vector3(.08f,.96f,.08f),wood,false);SlabBetween("fence rail",p,a+Vector3.up*.62f,b+Vector3.up*.62f,.08f,wood,false);SlabBetween("fence rail",p,a+Vector3.up*.28f,b+Vector3.up*.28f,.08f,wood,false);}
        static void SlabBetween(string name,Transform p,Vector3 a,Vector3 b,float width,Material mat,bool col){Vector3 d=b-a;float len=new Vector2(d.x,d.z).magnitude;if(len<.02f)return;float yaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;Box(name,p,(a+b)*.5f,new Vector3(width,.045f,len+width*.10f),mat,col,Quaternion.Euler(0,yaw,0));}

        static Mesh EnsureConeMesh(){string path=Generated+"/cone.asset";Mesh m=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(m)return m;m=new Mesh();m.name="V86 cone";int seg=18;List<Vector3> v=new List<Vector3>{new Vector3(0,.5f,0),new Vector3(0,-.5f,0)};List<int> tr=new List<int>();for(int i=0;i<seg;i++){float a=i*Mathf.PI*2f/seg;v.Add(new Vector3(Mathf.Cos(a)*.5f,-.5f,Mathf.Sin(a)*.5f));}for(int i=0;i<seg;i++){int a=2+i,b=2+(i+1)%seg;tr.Add(0);tr.Add(a);tr.Add(b);tr.Add(1);tr.Add(b);tr.Add(a);}m.SetVertices(v);m.SetTriangles(tr,0);m.RecalculateNormals();m.RecalculateBounds();AssetDatabase.CreateAsset(m,path);return m;}
        static void Cone(string name,Transform p,Vector3 lp,float rad,float h,Material mat){GameObject g=new GameObject(name);g.transform.SetParent(p,false);g.transform.localPosition=lp;g.transform.localScale=new Vector3(rad*2f,h,rad*2f);MeshFilter mf=g.AddComponent<MeshFilter>();mf.sharedMesh=coneMesh;g.AddComponent<MeshRenderer>().sharedMaterial=mat;}
        static void Cylinder(string name,Transform p,Vector3 lp,float rad,float h,Material mat,bool col){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=name;g.transform.SetParent(p,false);g.transform.localPosition=lp;g.transform.localScale=new Vector3(rad*2f,h*.5f,rad*2f);g.GetComponent<Renderer>().sharedMaterial=mat;if(!col)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());}
        static GameObject MeshObject(string name,Transform p,Mesh mesh,Material mat,bool col){GameObject g=new GameObject(name);g.transform.SetParent(p,true);MeshFilter mf=g.AddComponent<MeshFilter>();mf.sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=mat;if(col){MeshCollider mc=g.AddComponent<MeshCollider>();mc.sharedMesh=mesh;}return g;}
        static GameObject Box(string name,Transform p,Vector3 pos,Vector3 size,Material mat,bool col){return Box(name,p,pos,size,mat,col,Quaternion.identity);}
        static GameObject Box(string name,Transform p,Vector3 pos,Vector3 size,Material mat,bool col,Quaternion rot){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(p,true);g.transform.position=pos;g.transform.rotation=rot;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;if(!col)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
        static GameObject BoxLocal(string name,Transform p,Vector3 lp,Vector3 size,Material mat,bool col,Quaternion rot){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(p,false);g.transform.localPosition=lp;g.transform.localRotation=rot;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;if(!col)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
        static Transform Group(Transform p,string n){GameObject g=new GameObject(n);g.transform.SetParent(p,true);return g.transform;}

        enum Pattern { Noise, Boards, Roof, Brick, Wood }
        static Material PatternMat(string name,string ah,string bh,Pattern pat,float smooth,float tile){string tp=Generated+"/"+Safe(name)+".png";Texture2D tex=AssetDatabase.LoadAssetAtPath<Texture2D>(tp);if(!tex){Texture2D t=new Texture2D(64,64,TextureFormat.RGBA32,false);Color a=Hex(ah),b=Hex(bh);for(int y=0;y<64;y++)for(int x=0;x<64;x++){float k;switch(pat){case Pattern.Boards:k=(x%10<2)?.20f:Mathf.PerlinNoise(x*.13f,y*.07f)*.16f;break;case Pattern.Roof:k=(x%15<2)?.28f:Mathf.PerlinNoise(x*.08f,y*.08f)*.10f;break;case Pattern.Brick:k=((y%12<2)||((x+(y/12%2)*7)%18<2))?.28f:Mathf.PerlinNoise(x*.11f,y*.09f)*.12f;break;case Pattern.Wood:k=(y%10<1)?.22f:Mathf.PerlinNoise(x*.05f,y*.15f)*.18f;break;default:k=Mathf.PerlinNoise(x*.11f,y*.11f)*.22f;break;}t.SetPixel(x,y,Color.Lerp(a,b,k));}t.Apply();File.WriteAllBytes(tp,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);AssetDatabase.ImportAsset(tp,ImportAssetOptions.ForceSynchronousImport);tex=AssetDatabase.LoadAssetAtPath<Texture2D>(tp);}Material m=MatAsset(name);if(m.shader!=shader)m.shader=shader;if(m.HasProperty("_BaseMap")){m.SetTexture("_BaseMap",tex);m.SetTextureScale("_BaseMap",Vector2.one*tile);}if(m.HasProperty("_MainTex")){m.SetTexture("_MainTex",tex);m.SetTextureScale("_MainTex",Vector2.one*tile);}if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",Color.white);if(m.HasProperty("_Color"))m.SetColor("_Color",Color.white);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;}
        static Material FlatMat(string name,string h,float smooth){Material m=MatAsset(name);if(m.shader!=shader)m.shader=shader;Color c=Hex(h);if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);if(m.HasProperty("_Color"))m.SetColor("_Color",c);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;}
        static Material EmissiveMat(string name,string h,float e){Material m=FlatMat(name,h,.16f);if(m.HasProperty("_EmissionColor")){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",Hex(h)*e);}return m;}
        static Material MatAsset(string n){string p=Generated+"/"+Safe(n)+".mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(p);if(!m){m=new Material(shader);AssetDatabase.CreateAsset(m,p);}return m;}
        static string Safe(string s)=>new string(s.ToLowerInvariant().Select(c=>char.IsLetterOrDigit(c)?c:'_').ToArray());
        static Color Hex(string s){Color c=Color.white;ColorUtility.TryParseHtmlString("#"+s,out c);return c;}
        static Shader PickShader(){bool srp=GraphicsSettings.currentRenderPipeline!=null||GraphicsSettings.defaultRenderPipeline!=null;Shader s=srp?Shader.Find("Universal Render Pipeline/Lit"):Shader.Find("Standard");if(!s||!s.isSupported)s=Shader.Find("Universal Render Pipeline/Simple Lit");if(!s||!s.isSupported)s=Shader.Find("Standard");if(!s||!s.isSupported)throw new Exception("No supported shader");return s;}
        static void CleanupMissingScripts(){foreach(GameObject g in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(g&&g.scene.IsValid())GameObjectUtility.RemoveMonoBehavioursWithMissingScript(g);}
    }
}
#endif
