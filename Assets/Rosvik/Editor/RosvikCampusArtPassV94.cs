#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Rosvik.Blackout;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikCampusArtPassV94 {
        const int Version = 94;
        const string Key = "ROSVIK_CAMPUS_ART_PASS_V94";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string WorldName = "V88 CLEAN OSM WORLD";
        const string RootName = "V94 CAMPUS ART PASS";
        const string Generated = "Assets/Rosvik/GeneratedV94";
        static int retries;

        static Shader shader;
        static Material snow, snowShade, packed, asphalt, schoolWall, schoolAccent, brick, arenaBlue, arenaDark, cream, roof, glass, warmGlass, metal, wood, pine, trunk, black;

        static RosvikCampusArtPassV94() {
            if (EditorPrefs.GetInt(Key, 0) >= Version) return;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V94 CAMPUS ART PASS")]
        public static void Force() {
            EditorPrefs.DeleteKey(Key);
            retries = 0;
            EditorApplication.delayCall += Auto;
        }

        static void Auto() {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) {
                EditorApplication.delayCall += Auto;
                return;
            }
            if (!File.Exists(ScenePath)) return;
            try {
                if (!Apply() && retries++ < 16) EditorApplication.delayCall += Auto;
            } catch (Exception ex) {
                Debug.LogError("V94 CAMPUS ART PASS FAILED: " + ex);
            }
        }

        static bool Apply() {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject world = GameObject.Find(WorldName);
            GameObject campus = GameObject.Find("ROSVIK CAMPUS · V92 ASTRA BENCHMARK");
            if (!world || !campus) return false;

            Transform school = Find(campus.transform, "ROSVIKS SKOLA · V92");
            Transform sport = Find(campus.transform, "ROSVIK SPORTHALL · V92");
            Transform arena = Find(campus.transform, "NORRBOTTEN STÅL ARENA · ISHALL V92");
            Transform connector = Find(campus.transform, "HEATED SCHOOL-SPORTHALL CONNECTOR · V92");
            Transform schoolVol = Find(campus.transform, "SCHOOL MAIN INDOOR V92");
            Transform sportVol = Find(campus.transform, "SPORTHALL INDOOR V92");
            Transform arenaVol = Find(campus.transform, "ICEHALL INDOOR V92");
            if (!school || !sport || !arena || !connector || !schoolVol || !sportVol || !arenaVol) return false;

            GameObject old = GameObject.Find(RootName);
            if (old) UnityEngine.Object.DestroyImmediate(old);
            GameObject root = new GameObject(RootName);
            root.transform.SetParent(world.transform, true);

            Directory.CreateDirectory(Generated);
            shader = PickShader(world);
            BuildMaterials();

            Vector3 schoolC = Flat(schoolVol.position);
            Vector3 sportC = Flat(sportVol.position);
            Vector3 arenaC = Flat(arenaVol.position);

            // Clear raw generic OSM boxes from the immediate campus. OSM remains the placement authority;
            // this only removes generic shells that visually collide with the authored school/sport/arena slice.
            CleanupRawCampusBuildings(world.transform, schoolC, sportC, arenaC);

            ApplyBuildingPalette(school, schoolWall, schoolAccent, brick, roof, glass, warmGlass);
            ApplyBuildingPalette(sport, cream, schoolAccent, brick, roof, glass, warmGlass);
            ApplyBuildingPalette(arena, arenaBlue, arenaDark, brick, roof, glass, warmGlass);

            Transform facade = Group(root.transform, "01 FACADE DETAIL");
            BuildSchoolFacade(facade, schoolC);
            BuildConnectorFacade(facade, connector);
            BuildSporthallFacade(facade, sportC);
            BuildArenaFacade(facade, arenaC);

            Transform ground = Group(root.transform, "02 SNOW + HARDSTAND");
            BuildGroundArt(ground, schoolC, sportC, arenaC);

            Transform props = Group(root.transform, "03 CAMPUS PROPS");
            BuildCampusProps(props, schoolC, sportC, arenaC);

            Transform nature = Group(root.transform, "04 WINTER VEGETATION");
            BuildWinterNature(nature, schoolC, sportC, arenaC);

            Transform lights = Group(root.transform, "05 LIGHTING");
            BuildCampusLighting(lights, schoolC, sportC, arenaC);
            TuneGlobalLighting();

            CoziPlayerV57 player = UnityEngine.Object.FindFirstObjectByType<CoziPlayerV57>();
            if (player) {
                player.SetObjective("Utforska skolområdet. Följ den upplysta entrén och undersök byggnaderna.");
                EditorUtility.SetDirty(player);
            }

            CleanupMissingScripts();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorPrefs.SetInt(Key, Version);
            SceneView.RepaintAll();
            Debug.Log("V94 COMPLETE — campus art pass applied on top of OSM-locked V93 layout; generic campus-overlap buildings removed, systems untouched.");
            return true;
        }

        static void BuildMaterials() {
            snow = Mat("V94 snow", new Color(.67f,.75f,.80f), .22f, 0f);
            snowShade = Mat("V94 drift snow", new Color(.56f,.66f,.72f), .19f, 0f);
            packed = Mat("V94 packed snow", new Color(.43f,.51f,.55f), .30f, 0f);
            asphalt = Mat("V94 frozen asphalt", new Color(.15f,.19f,.21f), .42f, .02f);
            schoolWall = Mat("V94 school ochre", new Color(.55f,.39f,.20f), .34f, 0f);
            schoolAccent = Mat("V94 school brown", new Color(.31f,.20f,.14f), .43f, 0f);
            brick = Mat("V94 dark brick", new Color(.30f,.12f,.095f), .47f, 0f);
            arenaBlue = Mat("V94 arena blue", new Color(.13f,.25f,.30f), .48f, .06f);
            arenaDark = Mat("V94 arena dark", new Color(.075f,.13f,.16f), .56f, .08f);
            cream = Mat("V94 sport cream", new Color(.58f,.58f,.53f), .31f, 0f);
            roof = Mat("V94 roof", new Color(.105f,.14f,.17f), .54f, .16f);
            glass = Mat("V94 cold glass", new Color(.13f,.22f,.27f), .13f, .02f);
            warmGlass = Mat("V94 warm glass", new Color(.82f,.57f,.28f), .12f, .01f, new Color(.42f,.22f,.06f));
            metal = Mat("V94 metal", new Color(.20f,.24f,.25f), .70f, .55f);
            wood = Mat("V94 timber", new Color(.32f,.20f,.12f), .38f, 0f);
            pine = Mat("V94 pine", new Color(.055f,.13f,.11f), .55f, 0f);
            trunk = Mat("V94 trunk", new Color(.19f,.12f,.075f), .60f, 0f);
            black = Mat("V94 black", new Color(.045f,.055f,.06f), .55f, .05f);
        }

        static Material Mat(string name, Color c, float smoothness, float metallic, Color? emission = null) {
            string path = Generated + "/" + name + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) {
                m = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(m, path);
            } else m.shader = shader;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (emission.HasValue) {
                m.EnableKeyword("_EMISSION");
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", emission.Value);
            } else m.DisableKeyword("_EMISSION");
            EditorUtility.SetDirty(m);
            return m;
        }

        static void CleanupRawCampusBuildings(Transform world, Vector3 schoolC, Vector3 sportC, Vector3 arenaC) {
            Transform town = Find(world, "03 OSM BUILDINGS");
            if (!town) return;
            for (int i = town.childCount - 1; i >= 0; i--) {
                Transform child = town.GetChild(i);
                Bounds b;
                if (!TryBounds(child, out b)) continue;
                Vector3 c = Flat(b.center);
                bool campusConflict = Dist(c, schoolC) < 30f || Dist(c, sportC) < 23f || Dist(c, arenaC) < 27f;
                if (campusConflict) UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        static void ApplyBuildingPalette(Transform root, Material wallMat, Material accentMat, Material plinthMat, Material roofMat, Material coldGlass, Material hotGlass) {
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true)) {
                if (!r || !r.sharedMaterial) continue;
                string n = (r.gameObject.name + " " + r.sharedMaterial.name).ToLowerInvariant();
                if (n.Contains("snow")) r.sharedMaterial = snow;
                else if (n.Contains("roof") || n.Contains("fascia") || n.Contains("ridge")) r.sharedMaterial = roofMat;
                else if (n.Contains("plinth") || n.Contains("brick") || n.Contains("portal")) r.sharedMaterial = plinthMat;
                else if (n.Contains("warm glass")) r.sharedMaterial = hotGlass;
                else if (n.Contains("glass")) r.sharedMaterial = coldGlass;
                else if (n.Contains("metal") || n.Contains("mullion") || n.Contains("post") || n.Contains("seam")) r.sharedMaterial = metal;
                else if (n.Contains("sign board") || n.Contains("dark ochre")) r.sharedMaterial = accentMat;
                else if (n.Contains("wall")) r.sharedMaterial = wallMat;
            }
        }

        static void BuildSchoolFacade(Transform p, Vector3 c) {
            // South entrance hierarchy and facade rhythm around the authored V93 school position.
            Vector3 south = c + new Vector3(0,0,-6.65f);
            Box("school entrance apron", p, south + new Vector3(0,.025f,-2.2f), new Vector3(8.0f,.05f,4.0f), packed, false);
            Box("school entry step", p, south + new Vector3(0,.12f,-.65f), new Vector3(3.1f,.18f,.75f), asphalt, false);
            for (int s=-1; s<=1; s+=2) Bollard(p, south + new Vector3(s*2.0f,0,-1.85f));
            for (int i=0;i<5;i++) {
                float x = c.x - 9.2f + i*4.6f;
                Box("school foundation vent", p, new Vector3(x,.42f,south.z+.05f), new Vector3(.70f,.26f,.06f), black, false);
            }
            // Modest vertical timber trim gives the long facade scale and depth.
            for (int i=0;i<11;i++) {
                float x = c.x - 11.7f + i*2.35f;
                Box("school facade batten", p, new Vector3(x,1.75f,south.z+.08f), new Vector3(.045f,2.25f,.055f), schoolAccent, false);
            }
            Bench(p, south + new Vector3(5.0f,0,-1.35f), 0f);
            Bin(p, south + new Vector3(-4.5f,0,-1.15f));
        }

        static void BuildConnectorFacade(Transform p, Transform connector) {
            Bounds b;
            if (!TryBounds(connector, out b)) return;
            Vector3 c = Flat(b.center);
            Box("connector base curb", p, c + new Vector3(0,.10f,-1.65f), new Vector3(2.7f,.18f,.20f), brick, false);
            Box("connector base curb", p, c + new Vector3(0,.10f,1.65f), new Vector3(2.7f,.18f,.20f), brick, false);
        }

        static void BuildSporthallFacade(Transform p, Vector3 c) {
            Vector3 south = c + new Vector3(0,0,-9.15f);
            Box("sporthall apron", p, south + new Vector3(0,.02f,-2.0f), new Vector3(10f,.045f,3.4f), packed, false);
            for(int i=-3;i<=3;i++) {
                Box("sporthall upper slit", p, new Vector3(c.x+i*1.75f,3.05f,south.z+.06f), new Vector3(1.05f,.48f,.055f), glass, false);
            }
            Bench(p, south + new Vector3(5.2f,0,-1.05f), 0f);
        }

        static void BuildArenaFacade(Transform p, Vector3 c) {
            Vector3 south = c + new Vector3(0,0,-12.15f);
            Box("arena arrival apron", p, south + new Vector3(0,.025f,-3.0f), new Vector3(18f,.05f,5.4f), asphalt, false);
            Box("arena snow edge", p, south + new Vector3(-9.5f,.11f,-2.9f), new Vector3(1.1f,.20f,5.8f), snowShade, false);
            Box("arena snow edge", p, south + new Vector3(9.5f,.11f,-2.9f), new Vector3(1.1f,.20f,5.8f), snowShade, false);
            // Strong steel-panel rhythm and utility details make the hall read as an ice arena, not a house.
            for(int i=-9;i<=9;i++) {
                Box("arena vertical seam", p, new Vector3(c.x+i*1.32f,2.85f,south.z+.08f), new Vector3(.035f,4.7f,.045f), arenaDark, false);
            }
            Box("arena service door", p, new Vector3(c.x+8.3f,1.25f,south.z+.04f), new Vector3(2.4f,2.45f,.08f), arenaDark, false);
            Box("arena utility box", p, new Vector3(c.x+11.2f,.72f,south.z-.08f), new Vector3(1.05f,1.25f,.45f), metal, false);
            for(int s=-1;s<=1;s+=2) Bollard(p, south + new Vector3(s*3.2f,0,-2.1f));
        }

        static void BuildGroundArt(Transform p, Vector3 schoolC, Vector3 sportC, Vector3 arenaC) {
            // Ploughed pedestrian route from school toward sporthall/arena, kept narrow and integrated with the snow.
            Vector3 a = schoolC + new Vector3(0,0,-8.7f);
            Vector3 b = sportC + new Vector3(-7f,0,-9.5f);
            SlabBetween("ploughed school-sport walk", p, a, b, 2.3f, packed);
            Vector3 c = arenaC + new Vector3(0,0,-15f);
            SlabBetween("ploughed arena approach", p, b, c, 2.7f, packed);

            // Parking bay by the arena without giant rectangular snow plates.
            Vector3 lotC = arenaC + new Vector3(-13.5f,0,-9.5f);
            Box("arena compact parking", p, lotC + new Vector3(0,.015f,0), new Vector3(13f,.035f,8.5f), asphalt, false);
            for(int i=-2;i<=2;i++) {
                Box("parking stripe", p, lotC + new Vector3(i*2.35f,.04f,0), new Vector3(.045f,.015f,7.2f), cream, false);
            }

            // Uneven snow banks at ploughed edges.
            for(int i=0;i<9;i++) {
                float t=i/8f;
                Vector3 pos=Vector3.Lerp(a,c,t)+new Vector3(-2.2f-(i%2)*.35f,0,0);
                Drift(p,pos,1.3f+.18f*(i%3));
            }
            for(int i=0;i<7;i++) {
                Vector3 pos=arenaC+new Vector3(-20f+i*5.5f,0,5.5f+Mathf.Sin(i*.8f)*2f);
                Drift(p,pos,1.6f+.22f*(i%3));
            }
        }

        static void BuildCampusProps(Transform p, Vector3 schoolC, Vector3 sportC, Vector3 arenaC) {
            // Bike rack and schoolyard furniture.
            Vector3 bike = schoolC + new Vector3(-9.5f,0,-9.6f);
            for(int i=0;i<9;i++) {
                Box("bike stand", p, bike+new Vector3(i*.58f,.33f,0), new Vector3(.055f,.66f,.62f), metal, false, Quaternion.Euler(0,0,18));
            }
            Bench(p, schoolC+new Vector3(8f,0,-9.2f), 0f);
            Bench(p, schoolC+new Vector3(-2f,0,9.0f), 90f);
            Bin(p, schoolC+new Vector3(6.5f,0,-8.8f));

            // Simple fenced edge / service separation near arena.
            Vector3 f0 = arenaC + new Vector3(14.6f,0,-6f);
            Vector3 f1 = arenaC + new Vector3(14.6f,0,8f);
            Fence(p,f0,f1);

            // Small snow-covered maintenance pallet grouping, deliberate rather than random scatter.
            Box("service pallet", p, arenaC+new Vector3(13.0f,.16f,6.5f), new Vector3(1.4f,.28f,1.0f), wood, false);
            Box("service crate", p, arenaC+new Vector3(12.6f,.55f,6.5f), new Vector3(.72f,.65f,.72f), wood, false);
            Box("crate snow", p, arenaC+new Vector3(12.6f,.91f,6.5f), new Vector3(.76f,.08f,.76f), snow, false);
        }

        static void BuildWinterNature(Transform p, Vector3 schoolC, Vector3 sportC, Vector3 arenaC) {
            // Deliberately frame the campus rather than filling it with random trees.
            Vector3[] trees = {
                schoolC+new Vector3(-17,0,-12), schoolC+new Vector3(-19,0,-3), schoolC+new Vector3(-16,0,12),
                schoolC+new Vector3(-10,0,15), schoolC+new Vector3(8,0,15), sportC+new Vector3(12,0,-12),
                sportC+new Vector3(13,0,8), arenaC+new Vector3(-17,0,13), arenaC+new Vector3(-8,0,16),
                arenaC+new Vector3(8,0,16), arenaC+new Vector3(18,0,10), arenaC+new Vector3(18,0,-5)
            };
            for(int i=0;i<trees.Length;i++) Tree(p,trees[i],.88f+(i%4)*.08f);
            for(int i=0;i<12;i++) {
                Vector3 b = schoolC+new Vector3(-15f+i*2.8f,0,11.2f+Mathf.Sin(i*.75f)*1.5f);
                Shrub(p,b,.55f+.08f*(i%3));
            }
        }

        static void BuildCampusLighting(Transform p, Vector3 schoolC, Vector3 sportC, Vector3 arenaC) {
            Vector3[] lamps = {
                schoolC+new Vector3(-8,0,-11), schoolC+new Vector3(0,0,-11), schoolC+new Vector3(8,0,-11),
                sportC+new Vector3(-6,0,-11), arenaC+new Vector3(-7,0,-16), arenaC+new Vector3(0,0,-16), arenaC+new Vector3(7,0,-16)
            };
            for(int i=0;i<lamps.Length;i++) Lamp(p,lamps[i], i>=4 ? .85f : .72f);
            WarmPoint(p, schoolC+new Vector3(0,2.3f,-7.4f), 7f, 1.6f);
            WarmPoint(p, sportC+new Vector3(-7.5f,2.4f,-9.2f), 6f, 1.25f);
            WarmPoint(p, arenaC+new Vector3(0,3.0f,-12.8f), 8f, 1.8f);
        }

        static void TuneGlobalLighting() {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.235f,.285f,.31f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(.32f,.39f,.43f);
            RenderSettings.fogDensity = .0048f;

            foreach(Light l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None)) {
                if (!l || !l.gameObject.scene.IsValid() || l.type != LightType.Directional) continue;
                l.color = new Color(.78f,.84f,.90f);
                l.intensity = .72f;
                l.shadows = LightShadows.Soft;
                l.shadowStrength = .72f;
            }

            Camera cam = Camera.main;
            if (cam) {
                cam.backgroundColor = new Color(.14f,.21f,.25f);
                if (cam.orthographic) cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, 8.5f, 10.5f);
                EditorUtility.SetDirty(cam);
            }
        }

        static void SlabBetween(string name, Transform p, Vector3 a, Vector3 b, float width, Material mat) {
            Vector3 d=b-a; d.y=0; float len=d.magnitude; if(len<.1f)return;
            Vector3 mid=(a+b)*.5f+Vector3.up*.018f;
            float yaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;
            Box(name,p,mid,new Vector3(width,.035f,len),mat,false,Quaternion.Euler(0,yaw,0));
        }

        static void Fence(Transform p, Vector3 a, Vector3 b) {
            Vector3 d=b-a; d.y=0; float len=d.magnitude; if(len<.1f)return;
            int posts=Mathf.Max(2,Mathf.RoundToInt(len/2.2f));
            for(int i=0;i<=posts;i++) {
                Vector3 q=Vector3.Lerp(a,b,i/(float)posts);
                Box("fence post",p,q+Vector3.up*.72f,new Vector3(.09f,1.44f,.09f),metal,false);
            }
            Vector3 mid=(a+b)*.5f; float yaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;
            for(int r=0;r<2;r++) Box("fence rail",p,mid+Vector3.up*(.52f+r*.55f),new Vector3(.07f,.07f,len),metal,false,Quaternion.Euler(0,yaw,0));
        }

        static void Bench(Transform p, Vector3 pos, float yaw) {
            Transform r=Group(p,"bench"); r.position=pos; r.rotation=Quaternion.Euler(0,yaw,0);
            Box("bench seat",r,new Vector3(0,.42f,0),new Vector3(2.0f,.13f,.48f),wood,false);
            Box("bench back",r,new Vector3(0,.78f,.20f),new Vector3(2.0f,.58f,.10f),wood,false);
            for(int s=-1;s<=1;s+=2) Box("bench leg",r,new Vector3(s*.75f,.21f,0),new Vector3(.10f,.42f,.10f),metal,false);
            Box("bench snow",r,new Vector3(0,.50f,-.02f),new Vector3(1.88f,.055f,.40f),snow,false);
        }

        static void Bin(Transform p, Vector3 pos) {
            Box("waste bin",p,pos+new Vector3(0,.45f,0),new Vector3(.48f,.90f,.48f),metal,false);
            Box("bin lid",p,pos+new Vector3(0,.93f,0),new Vector3(.55f,.10f,.55f),black,false);
        }

        static void Bollard(Transform p, Vector3 pos) {
            Box("bollard",p,pos+new Vector3(0,.43f,0),new Vector3(.18f,.86f,.18f),metal,false);
            Box("bollard cap",p,pos+new Vector3(0,.88f,0),new Vector3(.23f,.08f,.23f),black,false);
        }

        static void Lamp(Transform p, Vector3 pos, float intensity) {
            Box("lamp post",p,pos+new Vector3(0,1.8f,0),new Vector3(.09f,3.6f,.09f),metal,false);
            Box("lamp arm",p,pos+new Vector3(.28f,3.47f,0),new Vector3(.62f,.08f,.08f),metal,false);
            Box("lamp head",p,pos+new Vector3(.58f,3.40f,0),new Vector3(.38f,.13f,.25f),black,false);
            WarmPoint(p,pos+new Vector3(.58f,3.27f,0),4.5f,intensity);
        }

        static void WarmPoint(Transform p, Vector3 pos, float range, float intensity) {
            GameObject g=new GameObject("warm campus light"); g.transform.SetParent(p,true); g.transform.position=pos;
            Light l=g.AddComponent<Light>(); l.type=LightType.Point; l.range=range; l.intensity=intensity;
            l.color=new Color(1f,.68f,.38f); l.shadows=LightShadows.Soft; l.shadowStrength=.65f;
        }

        static void Tree(Transform p, Vector3 pos, float scale) {
            Transform r=Group(p,"spruce"); r.position=pos; r.localScale=Vector3.one*scale;
            Primitive(PrimitiveType.Cylinder,"trunk",r,new Vector3(0,.9f,0),new Vector3(.18f,.9f,.18f),trunk,false);
            for(int i=0;i<4;i++) {
                float s=1.18f-i*.18f;
                Primitive(PrimitiveType.Sphere,"crown",r,new Vector3(0,1.35f+i*.55f,0),new Vector3(s,.46f,s),pine,false);
            }
            Primitive(PrimitiveType.Sphere,"snow crown",r,new Vector3(0,2.92f,0),new Vector3(.52f,.10f,.52f),snow,false);
        }

        static void Shrub(Transform p, Vector3 pos, float scale) {
            Primitive(PrimitiveType.Sphere,"winter shrub",p,pos+new Vector3(0,.30f,0),new Vector3(scale,.34f,scale*.85f),pine,false);
            Primitive(PrimitiveType.Sphere,"shrub snow",p,pos+new Vector3(0,.48f,0),new Vector3(scale*.78f,.09f,scale*.66f),snow,false);
        }

        static void Drift(Transform p, Vector3 pos, float scale) {
            Primitive(PrimitiveType.Sphere,"snow bank",p,pos+new Vector3(0,.12f,0),new Vector3(scale,.22f,scale*.66f),snowShade,false);
        }

        static GameObject Box(string name, Transform p, Vector3 pos, Vector3 scale, Material mat, bool collider, Quaternion? rot=null) {
            return Primitive(PrimitiveType.Cube,name,p,pos,scale,mat,collider,rot);
        }

        static GameObject Primitive(PrimitiveType type, string name, Transform p, Vector3 pos, Vector3 scale, Material mat, bool collider, Quaternion? rot=null) {
            GameObject g=GameObject.CreatePrimitive(type); g.name=name; g.transform.SetParent(p,true); g.transform.position=pos; g.transform.rotation=rot??Quaternion.identity; g.transform.localScale=scale;
            Renderer r=g.GetComponent<Renderer>(); if(r) r.sharedMaterial=mat;
            if(!collider){Collider c=g.GetComponent<Collider>(); if(c) UnityEngine.Object.DestroyImmediate(c);}
            return g;
        }

        static Transform Group(Transform p,string name){GameObject g=new GameObject(name);g.transform.SetParent(p,true);return g.transform;}
        static Vector3 Flat(Vector3 p){p.y=0;return p;}
        static float Dist(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}

        static bool TryBounds(Transform t, out Bounds b) {
            Renderer[] rs=t.GetComponentsInChildren<Renderer>(true);
            b=new Bounds(); bool any=false;
            foreach(Renderer r in rs){if(!r)continue;if(!any){b=r.bounds;any=true;}else b.Encapsulate(r.bounds);}
            return any;
        }

        static Shader PickShader(GameObject world) {
            foreach(Renderer r in world.GetComponentsInChildren<Renderer>(true)) if(r&&r.sharedMaterial&&r.sharedMaterial.shader&&r.sharedMaterial.shader.isSupported) return r.sharedMaterial.shader;
            return Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        }

        static Transform Find(Transform root,string exact) {
            foreach(Transform t in root.GetComponentsInChildren<Transform>(true)) if(t.name==exact) return t;
            return null;
        }

        static void CleanupMissingScripts() {
            foreach(GameObject g in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(g&&g.scene.IsValid()) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(g);
        }
    }
}
#endif
