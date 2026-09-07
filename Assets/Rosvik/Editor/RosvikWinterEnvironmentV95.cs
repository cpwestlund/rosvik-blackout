#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Rosvik.Blackout;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikWinterEnvironmentV95 {
        const int Version = 95;
        const string Key = "ROSVIK_WINTER_ENVIRONMENT_V95";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string WorldName = "V88 CLEAN OSM WORLD";
        const string V94Name = "V94 CAMPUS ART PASS";
        const string RootName = "V95 SOFT WINTER ENVIRONMENT";
        const string Generated = "Assets/Rosvik/GeneratedV95";
        static int retries;

        static Shader shader;
        static Material softSnow, bankSnow, packedSnow, footprint, pine, pineDark, trunk, birch, metal;
        static Mesh coneMesh, branchMesh, flakeMesh;

        static RosvikWinterEnvironmentV95() {
            if (EditorPrefs.GetInt(Key, 0) >= Version) return;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V95 SOFT SNOW + FOOTPRINTS + SNOWFALL")]
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
                if (!Apply() && retries++ < 18) EditorApplication.delayCall += Auto;
            } catch (Exception ex) {
                Debug.LogError("V95 WINTER ENVIRONMENT FAILED: " + ex);
            }
        }

        static bool Apply() {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject world = GameObject.Find(WorldName);
            GameObject v94 = GameObject.Find(V94Name);
            GameObject campus = GameObject.Find("ROSVIK CAMPUS · V92 ASTRA BENCHMARK");
            if (!world || !v94 || !campus) return false;

            Transform schoolVol = Find(campus.transform, "SCHOOL MAIN INDOOR V92");
            Transform sportVol = Find(campus.transform, "SPORTHALL INDOOR V92");
            Transform arenaVol = Find(campus.transform, "ICEHALL INDOOR V92");
            if (!schoolVol || !sportVol || !arenaVol) return false;

            GameObject old = GameObject.Find(RootName);
            if (old) UnityEngine.Object.DestroyImmediate(old);

            Directory.CreateDirectory(Generated);
            shader = PickShader(world);
            BuildMaterials();
            coneMesh = SaveMesh(BuildConeMesh(12), Generated + "/organic_cone.asset");
            branchMesh = SaveMesh(BuildBranchMesh(7), Generated + "/organic_branch.asset");
            flakeMesh = SaveMesh(BuildFlakeMesh(), Generated + "/snowflake_mesh.asset");

            // Remove the primitive-looking V94 spheres/cylinders and hard rectangular snow plates.
            Transform oldNature = Find(v94.transform, "04 WINTER VEGETATION");
            if (oldNature) oldNature.gameObject.SetActive(false);
            Transform oldGround = Find(v94.transform, "02 SNOW + HARDSTAND");
            if (oldGround) oldGround.gameObject.SetActive(false);

            GameObject root = new GameObject(RootName);
            root.transform.SetParent(world.transform, true);
            Transform snowRoot = Group(root.transform, "01 SOFT SNOW SURFACE");
            Transform bankRoot = Group(root.transform, "02 PLOWED SNOW BANKS");
            Transform pathRoot = Group(root.transform, "03 COMPRESSED SNOW PATHS");
            Transform natureRoot = Group(root.transform, "04 ORGANIC WINTER NATURE");
            Transform weatherRoot = Group(root.transform, "05 ACTIVE SNOW WEATHER");

            Vector3 schoolC = Flat(schoolVol.position);
            Vector3 sportC = Flat(sportVol.position);
            Vector3 arenaC = Flat(arenaVol.position);
            Vector3 campusC = (schoolC + sportC + arenaC) / 3f;

            BuildSoftSnowSurface(snowRoot, campusC);
            BuildPaths(pathRoot, schoolC, sportC, arenaC);
            BuildSnowBanks(bankRoot, schoolC, sportC, arenaC);
            BuildNature(natureRoot, schoolC, sportC, arenaC);
            BuildSnowfall(weatherRoot);
            AttachFootprints();
            TuneAtmosphere();

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorPrefs.SetInt(Key, Version);
            SceneView.RepaintAll();
            Debug.Log("V95 COMPLETE — primitive winter dressing replaced by soft snow mesh, rounded plow banks, organic trees, real footprint trail and active light snowfall; gameplay systems preserved.");
            return true;
        }

        static void BuildMaterials() {
            Texture2D snowTex = EnsureSnowTexture();
            softSnow = Mat("V95 soft snow", new Color(.69f,.77f,.82f), .20f, 0f, snowTex, new Vector2(7f,7f));
            bankSnow = Mat("V95 bank snow", new Color(.62f,.71f,.77f), .16f, 0f, snowTex, new Vector2(4f,4f));
            packedSnow = Mat("V95 compressed snow", new Color(.47f,.56f,.61f), .31f, .01f, snowTex, new Vector2(9f,9f));
            footprint = Mat("V95 boot print", new Color(.25f,.34f,.39f), .28f, 0f, null, Vector2.one);
            pine = Mat("V95 pine", new Color(.055f,.125f,.105f), .50f, 0f, null, Vector2.one);
            pineDark = Mat("V95 pine dark", new Color(.035f,.085f,.075f), .56f, 0f, null, Vector2.one);
            trunk = Mat("V95 spruce trunk", new Color(.18f,.105f,.060f), .62f, 0f, null, Vector2.one);
            birch = Mat("V95 birch bark", new Color(.63f,.62f,.56f), .40f, 0f, null, Vector2.one);
            metal = Mat("V95 winter metal", new Color(.18f,.22f,.23f), .68f, .45f, null, Vector2.one);
        }

        static Material Mat(string name, Color color, float smoothness, float metallic, Texture2D tex, Vector2 tiling) {
            string path = Generated + "/" + name + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(shader) { name = name }; AssetDatabase.CreateAsset(m, path); }
            else m.shader = shader;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (tex) {
                if (m.HasProperty("_BaseMap")) { m.SetTexture("_BaseMap", tex); m.SetTextureScale("_BaseMap", tiling); }
                if (m.HasProperty("_MainTex")) { m.SetTexture("_MainTex", tex); m.SetTextureScale("_MainTex", tiling); }
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        static Texture2D EnsureSnowTexture() {
            string path = Generated + "/soft_snow_noise.png";
            if (!File.Exists(path)) {
                const int s = 128;
                Texture2D t = new Texture2D(s,s,TextureFormat.RGBA32,false,true);
                for (int y=0;y<s;y++) for (int x=0;x<s;x++) {
                    float n1 = Mathf.PerlinNoise(x*.055f,y*.055f);
                    float n2 = Mathf.PerlinNoise((x+43)*.17f,(y-19)*.17f);
                    float v = Mathf.Lerp(.79f,1f,n1*.78f+n2*.22f);
                    t.SetPixel(x,y,new Color(v,v,v,1));
                }
                t.Apply();
                File.WriteAllBytes(path,t.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti != null) { ti.wrapMode = TextureWrapMode.Repeat; ti.filterMode = FilterMode.Bilinear; ti.sRGBTexture = true; ti.SaveAndReimport(); }
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void BuildSoftSnowSurface(Transform parent, Vector3 center) {
            Mesh mesh = new Mesh { name = "V95 soft campus snow" };
            const int nx=42, nz=42;
            const float w=86f, d=82f;
            Vector3[] v = new Vector3[(nx+1)*(nz+1)];
            Vector2[] uv = new Vector2[v.Length];
            int[] tri = new int[nx*nz*6];
            for(int z=0;z<=nz;z++) for(int x=0;x<=nx;x++) {
                float px=(x/(float)nx-.5f)*w;
                float pz=(z/(float)nz-.5f)*d;
                float broad=(Mathf.PerlinNoise((px+120f)*.055f,(pz-80f)*.055f)-.5f)*.16f;
                float fine=(Mathf.PerlinNoise((px-30f)*.19f,(pz+45f)*.19f)-.5f)*.035f;
                float edge=Mathf.Clamp01(Mathf.Min((x/(float)nx)*5f,(1-x/(float)nx)*5f, (z/(float)nz)*5f,(1-z/(float)nz)*5f));
                int i=z*(nx+1)+x;
                v[i]=new Vector3(px,-.025f+(broad+fine)*edge,pz);
                uv[i]=new Vector2(x/(float)nx,z/(float)nz);
            }
            int k=0;
            for(int z=0;z<nz;z++) for(int x=0;x<nx;x++) {
                int a=z*(nx+1)+x,b=a+1,c=a+nx+1,e=c+1;
                tri[k++]=a;tri[k++]=c;tri[k++]=b;tri[k++]=b;tri[k++]=c;tri[k++]=e;
            }
            mesh.vertices=v; mesh.uv=uv; mesh.triangles=tri; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            mesh=SaveMesh(mesh,Generated+"/soft_campus_snow.asset");
            GameObject g=MeshObject("soft continuous campus snow",parent,mesh,softSnow);
            g.transform.position=center;
        }

        static void BuildPaths(Transform parent, Vector3 schoolC, Vector3 sportC, Vector3 arenaC) {
            List<Vector3> main = new List<Vector3> {
                schoolC + new Vector3(0,0,-9.0f),
                Vector3.Lerp(schoolC,sportC,.55f) + new Vector3(-1.0f,0,-8.0f),
                sportC + new Vector3(-4.8f,0,-10.2f),
                arenaC + new Vector3(-2.0f,0,-15.0f)
            };
            Mesh path = BuildRibbon(main,2.45f,.035f,.14f);
            path=SaveMesh(path,Generated+"/compressed_main_walk.asset");
            MeshObject("compressed winter walkway",parent,path,packedSnow);

            List<Vector3> side = new List<Vector3> {
                schoolC + new Vector3(-9.5f,0,-8.7f), schoolC + new Vector3(-13f,0,-13f), schoolC + new Vector3(-18f,0,-15f)
            };
            Mesh sidePath=SaveMesh(BuildRibbon(side,1.65f,.03f,.10f),Generated+"/compressed_side_walk.asset");
            MeshObject("compressed side walkway",parent,sidePath,packedSnow);
        }

        static Mesh BuildRibbon(List<Vector3> points, float width, float y, float edgeNoise) {
            List<Vector3> verts=new List<Vector3>(); List<Vector2> uvs=new List<Vector2>(); List<int> tris=new List<int>();
            float traveled=0f;
            for(int i=0;i<points.Count;i++) {
                Vector3 fwd;
                if(i==0) fwd=(points[1]-points[0]).normalized;
                else if(i==points.Count-1) fwd=(points[i]-points[i-1]).normalized;
                else fwd=(points[i+1]-points[i-1]).normalized;
                fwd.y=0; Vector3 right=new Vector3(fwd.z,0,-fwd.x).normalized;
                if(i>0) traveled+=Vector3.Distance(points[i-1],points[i]);
                float wobble=(Mathf.PerlinNoise(i*.71f,3.2f)-.5f)*edgeNoise;
                float hw=width*.5f+wobble;
                verts.Add(points[i]-right*hw+Vector3.up*y); verts.Add(points[i]+right*hw+Vector3.up*y);
                uvs.Add(new Vector2(0,traveled*.18f));uvs.Add(new Vector2(1,traveled*.18f));
                if(i<points.Count-1){int a=i*2;tris.Add(a);tris.Add(a+2);tris.Add(a+1);tris.Add(a+1);tris.Add(a+2);tris.Add(a+3);}
            }
            Mesh m=new Mesh{name="V95 organic compressed snow ribbon"};m.SetVertices(verts);m.SetUVs(0,uvs);m.SetTriangles(tris,0);m.RecalculateNormals();m.RecalculateBounds();return m;
        }

        static void BuildSnowBanks(Transform parent, Vector3 schoolC, Vector3 sportC, Vector3 arenaC) {
            int id=0;
            Bank(parent, schoolC+new Vector3(-13f,0,-10.2f), schoolC+new Vector3(13f,0,-10.2f), 1.10f,.42f,id++);
            Bank(parent, sportC+new Vector3(-7f,0,-11.4f), sportC+new Vector3(7f,0,-11.4f), .95f,.34f,id++);
            Bank(parent, arenaC+new Vector3(-11.5f,0,-15.7f), arenaC+new Vector3(11.5f,0,-15.7f), 1.35f,.52f,id++);
            Bank(parent, arenaC+new Vector3(-12f,0,-10.0f), arenaC+new Vector3(-12f,0,7.5f), 1.20f,.46f,id++);
            Bank(parent, arenaC+new Vector3(12f,0,-9.0f), arenaC+new Vector3(12f,0,8.0f), 1.10f,.40f,id++);
            Bank(parent, schoolC+new Vector3(-18f,0,-13.5f), schoolC+new Vector3(-8f,0,-15.7f), .90f,.32f,id++);
        }

        static void Bank(Transform parent, Vector3 a, Vector3 b, float width, float height, int id) {
            Mesh m=SaveMesh(BuildBankMesh(a,b,width,height),Generated+"/snowbank_"+id+".asset");
            MeshObject("soft plowed snowbank",parent,m,bankSnow);
        }

        static Mesh BuildBankMesh(Vector3 a, Vector3 b, float width, float height) {
            Vector3 d=b-a; d.y=0; float len=d.magnitude; Vector3 f=d.normalized; Vector3 r=new Vector3(f.z,0,-f.x);
            const int along=12, cross=6;
            Vector3[] v=new Vector3[(along+1)*(cross+1)]; Vector2[] uv=new Vector2[v.Length]; int[] tr=new int[along*cross*6];
            for(int i=0;i<=along;i++) {
                float t=i/(float)along; Vector3 c=Vector3.Lerp(a,b,t);
                c += r*((Mathf.PerlinNoise(t*3.1f,2.4f)-.5f)*width*.22f);
                float endFade=Mathf.SmoothStep(0,1,Mathf.Min(t*4f,(1-t)*4f));
                for(int j=0;j<=cross;j++) {
                    float q=j/(float)cross; float side=(q-.5f)*width;
                    float arch=Mathf.Sin(q*Mathf.PI);
                    float rough=.88f+Mathf.PerlinNoise(t*5f,q*4f)*.18f;
                    int idx=i*(cross+1)+j;
                    v[idx]=c+r*side+Vector3.up*(arch*height*rough*endFade-.015f);
                    uv[idx]=new Vector2(t*len*.12f,q);
                }
            }
            int k=0;for(int i=0;i<along;i++)for(int j=0;j<cross;j++){int x=i*(cross+1)+j;tr[k++]=x;tr[k++]=x+cross+1;tr[k++]=x+1;tr[k++]=x+1;tr[k++]=x+cross+1;tr[k++]=x+cross+2;}
            Mesh m=new Mesh{name="V95 rounded snowbank"};m.vertices=v;m.uv=uv;m.triangles=tr;m.RecalculateNormals();m.RecalculateBounds();return m;
        }

        static void BuildNature(Transform parent, Vector3 schoolC, Vector3 sportC, Vector3 arenaC) {
            Vector3[] spruces = {
                schoolC+new Vector3(-18,0,-13), schoolC+new Vector3(-20,0,-2), schoolC+new Vector3(-17,0,13),
                schoolC+new Vector3(-8,0,16), sportC+new Vector3(11,0,-13), sportC+new Vector3(13,0,9),
                arenaC+new Vector3(-17,0,13), arenaC+new Vector3(-8,0,17), arenaC+new Vector3(9,0,17), arenaC+new Vector3(18,0,8)
            };
            for(int i=0;i<spruces.Length;i++) Spruce(parent,spruces[i],.90f+(i%4)*.09f,i);
            BareTree(parent,schoolC+new Vector3(-13,0,-14.2f),1.0f,11);
            BareTree(parent,schoolC+new Vector3(7,0,-13.0f),.90f,19);
            BareTree(parent,arenaC+new Vector3(-15,0,-12.0f),1.05f,27);
            BareTree(parent,arenaC+new Vector3(15,0,11f),.95f,33);
        }

        static void Spruce(Transform parent, Vector3 pos, float scale, int seed) {
            Transform r=Group(parent,"organic spruce");r.position=pos;r.localScale=Vector3.one*scale;r.rotation=Quaternion.Euler(0,(seed*37)%360,0);
            MeshPart("tapered trunk",r,branchMesh,new Vector3(0,1.0f,0),new Vector3(.22f,2.0f,.22f),trunk);
            for(int i=0;i<4;i++) {
                float y=1.0f+i*.62f; float s=1.42f-i*.23f;
                MeshPart("spruce foliage",r,coneMesh,new Vector3(0,y,0),new Vector3(s,.95f,s),i%2==0?pine:pineDark);
                MeshPart("soft snow on spruce",r,coneMesh,new Vector3(.03f,y+.18f,-.02f),new Vector3(s*.87f,.24f,s*.87f),softSnow);
            }
        }

        static void BareTree(Transform parent, Vector3 pos, float scale, int seed) {
            Transform r=Group(parent,"bare winter birch");r.position=pos;r.localScale=Vector3.one*scale;r.rotation=Quaternion.Euler(0,(seed*23)%360,0);
            Branch(r,new Vector3(0,0,0),new Vector3(.05f,3.6f,.02f),.16f,birch);
            Branch(r,new Vector3(.02f,1.7f,0),new Vector3(-.75f,2.65f,.12f),.085f,birch);
            Branch(r,new Vector3(.02f,2.05f,0),new Vector3(.82f,3.02f,-.18f),.075f,birch);
            Branch(r,new Vector3(-.48f,2.32f,.08f),new Vector3(-1.02f,3.12f,.18f),.048f,birch);
            Branch(r,new Vector3(.48f,2.62f,-.1f),new Vector3(1.10f,3.42f,-.28f),.045f,birch);
        }

        static void Branch(Transform parent, Vector3 a, Vector3 b, float radius, Material mat) {
            Vector3 d=b-a;float len=d.magnitude;Transform t=Group(parent,"tapered branch");t.localPosition=(a+b)*.5f;t.localRotation=Quaternion.FromToRotation(Vector3.up,d.normalized);t.localScale=new Vector3(radius,len,radius);MeshPart("branch mesh",t,branchMesh,Vector3.zero,Vector3.one,mat);
        }

        static void BuildSnowfall(Transform parent) {
            CoziPlayerV57 player=UnityEngine.Object.FindFirstObjectByType<CoziPlayerV57>();
            if(!player)return;
            GameObject g=new GameObject("light drifting snowfall");g.transform.SetParent(parent,true);g.transform.position=player.transform.position+Vector3.up*8.5f;
            ParticleSystem ps=g.AddComponent<ParticleSystem>();
            var main=ps.main;main.loop=true;main.duration=10f;main.playOnAwake=true;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startLifetime=new ParticleSystem.MinMaxCurve(5.5f,8.5f);main.startSpeed=new ParticleSystem.MinMaxCurve(.10f,.35f);main.startSize=new ParticleSystem.MinMaxCurve(.035f,.085f);main.startColor=new ParticleSystem.MinMaxGradient(new Color(.88f,.94f,.98f,.72f),new Color(.70f,.82f,.90f,.48f));main.maxParticles=700;
            var emission=ps.emission;emission.rateOverTime=58f;
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(18f,1.0f,14f);
            var vel=ps.velocityOverLifetime;vel.enabled=true;vel.space=ParticleSystemSimulationSpace.World;vel.x=.22f;vel.y=-1.05f;vel.z=.08f;
            var noise=ps.noise;noise.enabled=true;noise.strength=.24f;noise.frequency=.18f;noise.scrollSpeed=.12f;noise.damping=true;
            ParticleSystemRenderer pr=g.GetComponent<ParticleSystemRenderer>();pr.renderMode=ParticleSystemRenderMode.Mesh;pr.mesh=flakeMesh;pr.sharedMaterial=softSnow;pr.shadowCastingMode=ShadowCastingMode.Off;pr.receiveShadows=false;
            SnowfallFollowV95 follow=g.AddComponent<SnowfallFollowV95>();follow.target=player.transform;follow.height=8.5f;follow.windOffset=new Vector3(-1.0f,0,.35f);
            EditorUtility.SetDirty(g);
        }

        static void AttachFootprints() {
            CoziPlayerV57 player=UnityEngine.Object.FindFirstObjectByType<CoziPlayerV57>();if(!player)return;
            WinterFootprintsV95 fp=player.GetComponent<WinterFootprintsV95>();if(!fp)fp=player.gameObject.AddComponent<WinterFootprintsV95>();
            fp.footprintMaterial=footprint;fp.stepSpacing=.56f;fp.footSeparation=.16f;fp.footprintLength=.33f;fp.footprintWidth=.145f;fp.maxFootprints=180;fp.surfaceOffset=.018f;
            EditorUtility.SetDirty(player.gameObject);
        }

        static void TuneAtmosphere() {
            RenderSettings.fog=true;RenderSettings.fogColor=new Color(.27f,.34f,.39f);RenderSettings.fogDensity=.0062f;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.21f,.255f,.29f);
            foreach(Light l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(l&&l.gameObject.scene.IsValid()&&l.type==LightType.Directional){l.color=new Color(.72f,.80f,.88f);l.intensity=.62f;l.shadows=LightShadows.Soft;l.shadowStrength=.67f;}
            Camera cam=Camera.main;if(cam){cam.backgroundColor=new Color(.12f,.18f,.22f);EditorUtility.SetDirty(cam);}
        }

        static Mesh BuildConeMesh(int sides) {
            List<Vector3> v=new List<Vector3>{new Vector3(0,.5f,0)};for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides;v.Add(new Vector3(Mathf.Cos(a)*.5f,-.5f,Mathf.Sin(a)*.5f));}v.Add(new Vector3(0,-.5f,0));
            List<int> t=new List<int>();int bottom=sides+1;for(int i=0;i<sides;i++){int a=1+i,b=1+(i+1)%sides;t.Add(0);t.Add(b);t.Add(a);t.Add(bottom);t.Add(a);t.Add(b);}Mesh m=new Mesh{name="V95 organic cone"};m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;
        }

        static Mesh BuildBranchMesh(int sides) {
            List<Vector3> v=new List<Vector3>();for(int y=0;y<2;y++){float yy=y==0?-.5f:.5f;float rad=y==0?.5f:.32f;for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides;v.Add(new Vector3(Mathf.Cos(a)*rad,yy,Mathf.Sin(a)*rad));}}
            List<int> t=new List<int>();for(int i=0;i<sides;i++){int a=i,b=(i+1)%sides,c=sides+i,d=sides+(i+1)%sides;t.Add(a);t.Add(c);t.Add(b);t.Add(b);t.Add(c);t.Add(d);}Mesh m=new Mesh{name="V95 tapered branch"};m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;
        }

        static Mesh BuildFlakeMesh() {
            Vector3[] v={new Vector3(0,.5f,0),new Vector3(.34f,0,0),new Vector3(0,0,.34f),new Vector3(-.34f,0,0),new Vector3(0,0,-.34f),new Vector3(0,-.5f,0)};
            int[] t={0,2,1,0,3,2,0,4,3,0,1,4,5,1,2,5,2,3,5,3,4,5,4,1};Mesh m=new Mesh{name="V95 tiny snow crystal"};m.vertices=v;m.triangles=t;m.RecalculateNormals();m.RecalculateBounds();return m;
        }

        static MeshObjectHandle Dummy => default;
        struct MeshObjectHandle { }

        static GameObject MeshObject(string name, Transform parent, Mesh mesh, Material mat) {
            GameObject g=new GameObject(name);g.transform.SetParent(parent,true);MeshFilter mf=g.AddComponent<MeshFilter>();mf.sharedMesh=mesh;MeshRenderer mr=g.AddComponent<MeshRenderer>();mr.sharedMaterial=mat;mr.shadowCastingMode=ShadowCastingMode.On;mr.receiveShadows=true;return g;
        }

        static GameObject MeshPart(string name, Transform parent, Mesh mesh, Vector3 localPos, Vector3 localScale, Material mat) {
            GameObject g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=localPos;g.transform.localScale=localScale;MeshFilter mf=g.AddComponent<MeshFilter>();mf.sharedMesh=mesh;MeshRenderer mr=g.AddComponent<MeshRenderer>();mr.sharedMaterial=mat;mr.shadowCastingMode=ShadowCastingMode.On;mr.receiveShadows=true;return g;
        }

        static Mesh SaveMesh(Mesh source, string path) {
            Mesh existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing){EditorUtility.CopySerialized(source,existing);UnityEngine.Object.DestroyImmediate(source);EditorUtility.SetDirty(existing);return existing;}
            AssetDatabase.CreateAsset(source,path);return source;
        }

        static Shader PickShader(GameObject world) {
            foreach(Renderer r in world.GetComponentsInChildren<Renderer>(true))if(r&&r.sharedMaterial&&r.sharedMaterial.shader&&r.sharedMaterial.shader.isSupported)return r.sharedMaterial.shader;
            return Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard");
        }

        static Transform Group(Transform p,string name){GameObject g=new GameObject(name);g.transform.SetParent(p,true);return g.transform;}
        static Transform Find(Transform root,string exact){foreach(Transform t in root.GetComponentsInChildren<Transform>(true))if(t.name==exact)return t;return null;}
        static Vector3 Flat(Vector3 p){p.y=0;return p;}
    }
}
#endif
