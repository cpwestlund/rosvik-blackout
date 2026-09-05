#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikPolishV13 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string VersionKey = "ROSVIK_POLISH_PASS_VERSION";
        const string GeneratedFolder = "Assets/Rosvik/Generated/V13";
        const int Version = 13;

        static RosvikPolishV13() => EditorApplication.delayCall += Apply;

        [MenuItem("Rosvik/Apply Polish Pass V13")]
        public static void Apply() {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorPrefs.GetInt(VersionKey, 0) >= Version && File.Exists(ScenePath)) return;

            try {
                RosvikDetailPassV12.Apply();
                if (!File.Exists(ScenePath)) return;

                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                Transform root = FindRoot();
                if (!root) {
                    Debug.LogWarning("ROSVIK V13: V12 root not found; retry after compilation.");
                    return;
                }

                EnsureGeneratedFolder();

                Texture2D organicSnowTex = MakeOrganicTexture("OrganicSnow", SurfaceKind.Snow);
                Texture2D packedSnowTex = MakeOrganicTexture("OrganicPackedSnow", SurfaceKind.PackedSnow);
                Texture2D dirtySnowTex = MakeOrganicTexture("DirtyPackedSnow", SurfaceKind.DirtySnow);
                Texture2D fineBrickTex = MakeOrganicTexture("FineMasonry", SurfaceKind.Brick);
                Texture2D fineWoodTex = MakeOrganicTexture("FineTimber", SurfaceKind.Wood);

                Material snow = MaterialAsset("V13 Snow", new Color(.87f,.91f,.94f), organicSnowTex, .12f, new Vector2(7f,7f));
                Material packed = MaterialAsset("V13 Packed Snow", new Color(.72f,.77f,.79f), packedSnowTex, .07f, new Vector2(6f,6f));
                Material dirty = MaterialAsset("V13 Ploughed Snow", new Color(.63f,.66f,.66f), dirtySnowTex, .04f, new Vector2(5f,5f));
                Material roofSnow = MaterialAsset("V13 Roof Snow", new Color(.84f,.89f,.92f), organicSnowTex, .10f, new Vector2(5f,5f));
                Material timber = MaterialAsset("V13 Timber Yellow", new Color(.66f,.51f,.24f), fineWoodTex, .08f, new Vector2(3.2f,1.6f));
                Material masonry = MaterialAsset("V13 Seventies Masonry", new Color(.43f,.40f,.34f), fineBrickTex, .06f, new Vector2(7.5f,4.0f));
                Material dark = LoadMat("Assets/Rosvik/Generated/V12/DarkMetal.mat", new Color(.11f,.13f,.14f));
                Material oldWhite = LoadMat("Assets/Rosvik/Generated/V12/OldWhitePaint.mat", new Color(.80f,.80f,.75f));
                Material spruce = LoadMat("Assets/Rosvik/Generated/V12/SpruceNeedles.mat", new Color(.075f,.14f,.115f));
                Material bark = LoadMat("Assets/Rosvik/Generated/V12/Bark.mat", new Color(.18f,.13f,.09f));

                CleanGround(root, snow, packed, dirty);
                ReplaceSnowbanks(root, dirty, snow);

                Transform main = Child(root, "Huvudbyggnaden - 1970-tal");
                Transform timberSchool = Child(root, "Träskolan - ca 1900");
                Transform stone = Child(root, "Stenskolan - 1940/50-tal");
                Transform arena = Child(root, "Norrbotten Stål Arena - background landmark");

                if (main) PolishMain(main, masonry, roofSnow, dark);
                if (timberSchool) PolishTimber(timberSchool, timber, oldWhite, roofSnow, dark, dirty);
                if (stone) PolishStone(stone, roofSnow, dark);
                if (arena) PolishArena(arena, roofSnow);

                ReplaceSpruces(root, spruce, bark, snow);
                AddSubtleSnowTracks(root, packed, dirty);
                TuneCameraAndLight();

                root.name = "ROSVIK_POLISH_PASS_V13";
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
                EditorPrefs.SetInt(VersionKey, Version);
                Debug.Log("ROSVIK V13 APPLIED: cleaner facades, believable roof snow, organic snowbanks, non-striped ground and improved spruce silhouettes.");
            } catch (Exception ex) {
                Debug.LogError("ROSVIK V13 POLISH FAILED: " + ex);
            }
        }

        enum SurfaceKind { Snow, PackedSnow, DirtySnow, Brick, Wood }

        static Transform FindRoot() {
            string[] names = { "ROSVIK_POLISH_PASS_V13", "ROSVIK_DETAIL_PASS_V12", "ROSVIK_SITE_CALIBRATION_V11", "ROSVIK_REAL_CAMPUS_V10" };
            foreach (string n in names) {
                GameObject go = GameObject.Find(n);
                if (go) return go.transform;
            }
            return null;
        }

        static Transform Child(Transform root, string name) {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        static void DestroyByPrefix(Transform root, string prefix) {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = all.Length - 1; i >= 0; i--) {
                Transform t = all[i];
                if (t == root) continue;
                if (t.name.StartsWith(prefix, StringComparison.Ordinal))
                    UnityEngine.Object.DestroyImmediate(t.gameObject);
            }
        }

        static void EnsureGeneratedFolder() {
            if (!AssetDatabase.IsValidFolder("Assets/Rosvik/Generated")) AssetDatabase.CreateFolder("Assets/Rosvik", "Generated");
            if (!AssetDatabase.IsValidFolder(GeneratedFolder)) AssetDatabase.CreateFolder("Assets/Rosvik/Generated", "V13");
        }

        static float Hash(int x, int y) {
            unchecked {
                int h = x * 374761393 + y * 668265263;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return ((h & 2047) / 2047f) - .5f;
            }
        }

        static Texture2D MakeOrganicTexture(string name, SurfaceKind kind) {
            string path = GeneratedFolder + "/" + name + ".asset";
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing) return existing;

            const int size = 256;
            Texture2D tex = new Texture2D(size,size,TextureFormat.RGB24,true) { name=name, wrapMode=TextureWrapMode.Repeat, filterMode=FilterMode.Bilinear };
            for (int y=0;y<size;y++) {
                for (int x=0;x<size;x++) {
                    float fine = Hash(x,y);
                    float broad = Mathf.Sin(x*.035f) * Mathf.Sin(y*.027f) * .5f + Mathf.Sin((x+y)*.018f)*.25f;
                    float v = .8f;
                    switch (kind) {
                        case SurfaceKind.Snow:
                            v = .90f + fine*.045f + broad*.025f;
                            break;
                        case SurfaceKind.PackedSnow:
                            v = .79f + fine*.055f + broad*.035f;
                            break;
                        case SurfaceKind.DirtySnow:
                            v = .69f + fine*.07f + broad*.055f;
                            break;
                        case SurfaceKind.Brick: {
                            int rowH=10, brickW=28;
                            int row=y/rowH;
                            int ox=(row%2)*(brickW/2);
                            int bx=(x+ox)%brickW;
                            int by=y%rowH;
                            bool mortar = bx<1 || by<1;
                            v = mortar ? .55f : .77f + fine*.07f + broad*.02f;
                            break;
                        }
                        case SurfaceKind.Wood: {
                            int board=11;
                            int bx=x%board;
                            float seam = bx==0 ? -.18f : 0f;
                            float grain = Mathf.Sin(y*.18f + Mathf.Sin(x*.11f)*1.8f)*.025f;
                            v = .80f + seam + grain + fine*.035f;
                            break;
                        }
                    }
                    v=Mathf.Clamp01(v);
                    tex.SetPixel(x,y,new Color(v,v,v));
                }
            }
            tex.Apply(true,false);
            AssetDatabase.CreateAsset(tex,path);
            return tex;
        }

        static Material MaterialAsset(string name, Color tint, Texture2D tex, float smooth, Vector2 tiling) {
            string path = GeneratedFolder + "/" + name + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat) {
                Shader shader = GraphicsSettings.defaultRenderPipeline != null ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
                if (!shader || !shader.isSupported) shader = Shader.Find("Sprites/Default");
                mat = new Material(shader) { name=name };
                AssetDatabase.CreateAsset(mat,path);
            }
            mat.color=tint;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor",tint);
            if (mat.HasProperty("_BaseMap")) { mat.SetTexture("_BaseMap",tex); mat.SetTextureScale("_BaseMap",tiling); }
            else { mat.mainTexture=tex; mat.mainTextureScale=tiling; }
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness",smooth);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic",0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material LoadMat(string path, Color fallback) {
            Material mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat) return mat;
            return MaterialAsset(Path.GetFileNameWithoutExtension(path)+" fallback",fallback,MakeOrganicTexture("Fallback",SurfaceKind.DirtySnow),.08f,Vector2.one);
        }

        static void SetMaterial(Transform root, string name, Material mat) {
            Transform t=Child(root,name);
            if (!t) return;
            Renderer r=t.GetComponent<Renderer>();
            if (r) r.sharedMaterial=mat;
        }

        static GameObject Cube(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool collider=false) {
            GameObject go=GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name=name; go.transform.SetParent(parent,false); go.transform.localPosition=pos; go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=mat;
            if(!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static GameObject Cylinder(string name, Transform parent, Vector3 pos, Vector3 scale, Quaternion rot, Material mat) {
            GameObject go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name=name; go.transform.SetParent(parent,false); go.transform.localPosition=pos; go.transform.localScale=scale; go.transform.localRotation=rot;
            go.GetComponent<Renderer>().sharedMaterial=mat;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static void CleanGround(Transform root, Material snow, Material packed, Material dirty) {
            SetMaterial(root,"Snow terrain",snow);
            string[] packedNames={"Main cleared yard","Large central schoolyard","West play yard","Broad uphill transition","Upper Träskolan terrace","Stenskolan terrace"};
            foreach(string n in packedNames) SetMaterial(root,n,packed);
            SetMaterial(root,"Path uphill to Träskolan",dirty);
            SetMaterial(root,"Gentle path to Stenskolan",dirty);
            SetMaterial(root,"V12 yard worn snow strip",packed);
        }

        static void PolishMain(Transform b, Material facade, Material snow, Material metal) {
            SetMaterial(b,"Main mass",facade);
            DestroyByPrefix(b,"V12 main facade joint");
            DestroyByPrefix(b,"V12 main roof snow");
            AddRoofSnow("V13 main roof snow",b,35f,10.8f,3.25f,1.15f,.45f,.90f,snow);
            DestroyByPrefix(b,"V12 main gutter");
            DestroyByPrefix(b,"V12 main downpipe");
            Gutter("V13 main gutter",b,35.6f,3.29f,5.72f,metal);
            Downpipe("V13 main downpipe L",b,new Vector3(-17.15f,1.55f,5.58f),3.0f,metal);
            Downpipe("V13 main downpipe R",b,new Vector3(17.15f,1.55f,5.58f),3.0f,metal);
        }

        static void PolishTimber(Transform b, Material timber, Material white, Material snow, Material metal, Material dirtySnow) {
            SetMaterial(b,"Two storey timber body",timber);
            DestroyByPrefix(b,"V12 timber board groove");
            DestroyByPrefix(b,"V11 timber eave band");
            DestroyByPrefix(b,"V12 timber roof snow");
            DestroyByPrefix(b,"V12 timber gutter");
            DestroyByPrefix(b,"V12 timber downpipe");

            AddRoofSnow("V13 timber roof snow",b,14.5f,8.6f,6f,3.15f,.55f,.87f,snow);
            Gutter("V13 timber gutter front",b,15.0f,6.00f,4.69f,metal);
            Gutter("V13 timber gutter back",b,15.0f,6.00f,-4.69f,metal);
            Downpipe("V13 timber downpipe FL",b,new Vector3(-7.22f,2.72f,4.55f),5.3f,metal);
            Downpipe("V13 timber downpipe FR",b,new Vector3(7.22f,2.72f,4.55f),5.3f,metal);

            DestroyByPrefix(b,"V13 timber");
            Cube("V13 timber porch left post",b,new Vector3(-1.40f,1.52f,5.35f),new Vector3(.18f,2.85f,.18f),white,false);
            Cube("V13 timber porch right post",b,new Vector3(1.40f,1.52f,5.35f),new Vector3(.18f,2.85f,.18f),white,false);
            Cube("V13 timber porch header",b,new Vector3(0f,2.83f,5.35f),new Vector3(3.0f,.18f,.18f),white,false);
            Cube("V13 timber snow at foundation A",b,new Vector3(-4.8f,.17f,4.52f),new Vector3(3.5f,.18f,.55f),dirtySnow,false);
            Cube("V13 timber snow at foundation B",b,new Vector3(4.7f,.15f,4.50f),new Vector3(2.9f,.16f,.50f),dirtySnow,false);
        }

        static void PolishStone(Transform b, Material snow, Material metal) {
            DestroyByPrefix(b,"V12 stone roof snow");
            DestroyByPrefix(b,"V12 stone gutter");
            DestroyByPrefix(b,"V12 stone downpipe");
            AddRoofSnow("V13 stone roof snow",b,17.5f,8.2f,4.2f,2.35f,.48f,.89f,snow);
            Gutter("V13 stone gutter",b,18.0f,4.20f,4.54f,metal);
            Downpipe("V13 stone downpipe L",b,new Vector3(-8.45f,2.0f,4.42f),3.9f,metal);
            Downpipe("V13 stone downpipe R",b,new Vector3(8.45f,2.0f,4.42f),3.9f,metal);
        }

        static void PolishArena(Transform b, Material snow) {
            DestroyByPrefix(b,"V12 arena snow cap");
            Cube("V13 arena snow cap",b,new Vector3(0f,7.72f,0f),new Vector3(27.4f,.045f,15.4f),snow,false);
        }

        static void AddRoofSnow(string prefix, Transform b, float width, float depth, float wallH, float rise, float overhang, float coverage, Material snow) {
            float halfRun=depth*.5f+overhang;
            float angle=Mathf.Atan2(rise,halfRun)*Mathf.Rad2Deg;
            float slope=Mathf.Sqrt(halfRun*halfRun+rise*rise);
            float coveredSlope=slope*coverage;
            float halfCoveredRun=halfRun*coverage;
            float cy=wallH+rise*(coverage*.5f)+.06f;
            float cz=halfRun-halfCoveredRun*.5f-.05f;
            float w=(width+overhang*2f)*.985f;
            GameObject back=Cube(prefix+" back",b,new Vector3(0f,cy,-cz),new Vector3(w,.035f,coveredSlope),snow,false);
            back.transform.localRotation=Quaternion.Euler(-angle,0f,0f);
            GameObject front=Cube(prefix+" front",b,new Vector3(0f,cy,cz),new Vector3(w,.035f,coveredSlope),snow,false);
            front.transform.localRotation=Quaternion.Euler(angle,0f,0f);
        }

        static void Gutter(string name, Transform parent, float length, float y, float z, Material metal) {
            Cylinder(name,parent,new Vector3(0f,y,z),new Vector3(.045f,length*.5f,.045f),Quaternion.Euler(0f,0f,90f),metal);
        }

        static void Downpipe(string name, Transform parent, Vector3 pos, float height, Material metal) {
            Cylinder(name,parent,pos,new Vector3(.045f,height*.5f,.045f),Quaternion.identity,metal);
        }

        static void ReplaceSnowbanks(Transform root, Material dirty, Material clean) {
            foreach(Transform t in root.GetComponentsInChildren<Transform>(true)) {
                if(t.name.StartsWith("Ploughed snow bank",StringComparison.Ordinal) || t.name.StartsWith("V12 yard transition snowbank",StringComparison.Ordinal))
                    t.gameObject.SetActive(false);
            }
            DestroyByPrefix(root,"V13 drift");
            Vector3[] p={
                new Vector3(-39f,.18f,25.7f),new Vector3(-28f,.19f,25.9f),new Vector3(-16f,.16f,25.6f),
                new Vector3(-4f,.18f,25.8f),new Vector3(9f,.17f,25.6f),new Vector3(22f,.20f,25.9f),
                new Vector3(-34f,.38f,-24f),new Vector3(-27f,.46f,-26.2f),new Vector3(-20f,.50f,-28.1f)
            };
            for(int i=0;i<p.Length;i++) SnowDrift(root,p[i],1f+(i%3)*.16f,i<6?dirty:clean,"V13 drift "+i);
        }

        static void SnowDrift(Transform parent, Vector3 pos, float scale, Material mat, string name) {
            const int seg=14;
            Vector3[] v=new Vector3[seg+2];
            int[] tri=new int[seg*6];
            v[0]=new Vector3(0f,.72f*scale,0f);
            v[1]=new Vector3(0f,-.03f,0f);
            for(int i=0;i<seg;i++) {
                float a=i*Mathf.PI*2f/seg;
                float radial=(2.5f + .42f*Mathf.Sin(i*2.17f+1.3f) + .25f*Mathf.Sin(i*.83f))*scale;
                float zScale=.45f + .07f*Mathf.Sin(i*1.31f);
                v[i+2]=new Vector3(Mathf.Cos(a)*radial, .04f*Mathf.Sin(i*1.71f), Mathf.Sin(a)*radial*zScale);
            }
            int ti=0;
            for(int i=0;i<seg;i++) {
                int a=2+i,b=2+((i+1)%seg);
                tri[ti++]=0;tri[ti++]=a;tri[ti++]=b;
                tri[ti++]=1;tri[ti++]=b;tri[ti++]=a;
            }
            Mesh mesh=new Mesh { name=name+" mesh", vertices=v, triangles=tri };
            mesh.RecalculateNormals();mesh.RecalculateBounds();
            GameObject go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localRotation=Quaternion.Euler(0f,(name.GetHashCode()&31)*7f,0f);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;
        }

        static void ReplaceSpruces(Transform root, Material needles, Material bark, Material snow) {
            foreach(Transform t in root.GetComponentsInChildren<Transform>(true)) {
                if(t.name.StartsWith("V12 spruce",StringComparison.Ordinal)) t.gameObject.SetActive(false);
            }
            DestroyByPrefix(root,"V13 spruce");
            Vector3[] positions={
                new Vector3(-48f,0f,-40f),new Vector3(-42f,0f,-46f),new Vector3(-34f,.72f,-50f),
                new Vector3(-20f,.3f,-53f),new Vector3(6f,.15f,-51f),new Vector3(39f,.25f,-47f),
                new Vector3(52f,0f,-22f),new Vector3(-54f,0f,3f),new Vector3(-45f,0f,29f),
                new Vector3(46f,0f,31f),new Vector3(57f,0f,11f)
            };
            Mesh cone=IrregularConeMesh();
            for(int i=0;i<positions.Length;i++) Spruce(root,positions[i],.92f+(i%4)*.11f,cone,needles,bark,snow,"V13 spruce "+i);
        }

        static Mesh IrregularConeMesh() {
            const int seg=18;
            Vector3[] v=new Vector3[seg+2];int[] tri=new int[seg*6];
            v[0]=new Vector3(0f,1f,0f);v[1]=Vector3.zero;
            for(int i=0;i<seg;i++) {
                float a=i*Mathf.PI*2f/seg;
                float r=.88f + .10f*Mathf.Sin(i*2.13f) + .07f*Mathf.Sin(i*.77f+1f);
                v[i+2]=new Vector3(Mathf.Cos(a)*r,0f,Mathf.Sin(a)*r);
            }
            int ti=0;for(int i=0;i<seg;i++){int a=2+i,b=2+((i+1)%seg);tri[ti++]=0;tri[ti++]=b;tri[ti++]=a;tri[ti++]=1;tri[ti++]=a;tri[ti++]=b;}
            Mesh m=new Mesh{name="V13 irregular spruce cone",vertices=v,triangles=tri};m.RecalculateNormals();m.RecalculateBounds();return m;
        }

        static void Spruce(Transform parent, Vector3 pos, float scale, Mesh cone, Material needles, Material bark, Material snow, string name) {
            Transform r=new GameObject(name).transform;r.SetParent(parent,false);r.localPosition=pos;
            Cylinder("Trunk",r,new Vector3(0f,2.2f*scale,0f),new Vector3(.14f,2.2f*scale,.14f),Quaternion.identity,bark);
            for(int i=0;i<7;i++) {
                float y=(.75f+i*.66f)*scale;
                float radius=(2.05f-i*.22f)*scale;
                float height=(1.65f-i*.04f)*scale;
                GameObject layer=new GameObject("Irregular branch layer");layer.transform.SetParent(r,false);layer.transform.localPosition=new Vector3(0f,y,0f);layer.transform.localRotation=Quaternion.Euler(0f,i*23f,0f);layer.transform.localScale=new Vector3(radius,height,radius);
                layer.AddComponent<MeshFilter>().sharedMesh=cone;layer.AddComponent<MeshRenderer>().sharedMaterial=needles;
                if(i==1 || i==3) {
                    GameObject cap=new GameObject("Patchy branch snow");cap.transform.SetParent(r,false);cap.transform.localPosition=new Vector3(.10f*scale,y+.08f*scale,-.08f*scale);cap.transform.localRotation=Quaternion.Euler(0f,i*37f,0f);cap.transform.localScale=new Vector3(radius*.72f,.10f*scale,radius*.72f);cap.AddComponent<MeshFilter>().sharedMesh=cone;cap.AddComponent<MeshRenderer>().sharedMaterial=snow;
                }
            }
        }

        static void AddSubtleSnowTracks(Transform root, Material packed, Material dirty) {
            DestroyByPrefix(root,"V13 track");
            GameObject a=Cube("V13 track footpath A",root,new Vector3(-11f,.093f,-4f),new Vector3(2.1f,.018f,18f),packed,false);a.transform.localRotation=Quaternion.Euler(0f,-7f,0f);
            GameObject b=Cube("V13 track service A",root,new Vector3(18f,.087f,10f),new Vector3(2.5f,.018f,20f),dirty,false);b.transform.localRotation=Quaternion.Euler(0f,8f,0f);
        }

        static void TuneCameraAndLight() {
            Camera cam=Camera.main;
            if(cam && cam.orthographic) cam.orthographicSize=11.8f;
            IsometricCameraRig rig=UnityEngine.Object.FindFirstObjectByType<IsometricCameraRig>();
            if(rig) rig.orthographicSize=11.8f;
            foreach(Light l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) {
                if(l.type==LightType.Directional) {
                    l.intensity=.72f;l.color=new Color(.88f,.92f,1f);l.shadowStrength=.66f;break;
                }
            }
            RenderSettings.ambientLight=new Color(.43f,.46f,.48f);
            RenderSettings.fogColor=new Color(.62f,.68f,.71f);
            RenderSettings.fogDensity=.0028f;
        }
    }
}
#endif
