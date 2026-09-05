#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikHeroPolishV14 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string VersionKey = "ROSVIK_HERO_POLISH_VERSION";
        const string GeneratedFolder = "Assets/Rosvik/Generated/V14";
        const int Version = 14;

        static RosvikHeroPolishV14() => EditorApplication.delayCall += Apply;

        [MenuItem("Rosvik/Apply Hero Polish V14")]
        public static void Apply() {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorPrefs.GetInt(VersionKey, 0) >= Version && File.Exists(ScenePath)) return;

            try {
                RosvikPolishV13.Apply();
                if (!File.Exists(ScenePath)) return;

                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                Transform root = FindRoot();
                if (!root) return;

                EnsureFolder();

                Texture2D snowTex = MakeNoiseTexture("V14SnowNoise", .90f, .055f, .025f);
                Texture2D packedTex = MakeNoiseTexture("V14PackedSnowNoise", .79f, .075f, .045f);
                Texture2D woodTex = MakeWoodTexture("V14FineWood");
                Texture2D brickTex = MakeBrickTexture("V14FineMasonry");

                Material snow = MakeMat("V14 Snow", new Color(.88f,.92f,.95f), snowTex, .11f, new Vector2(3.4f,3.4f));
                Material packed = MakeMat("V14 Packed Snow", new Color(.74f,.78f,.80f), packedTex, .06f, new Vector2(2.7f,2.7f));
                Material dirty = MakeMat("V14 Dirty Snow", new Color(.64f,.67f,.67f), packedTex, .035f, new Vector2(2.1f,2.1f));
                Material timber = MakeMat("V14 Timber", new Color(.65f,.50f,.23f), woodTex, .08f, new Vector2(2.6f,1.4f));
                Material masonry = MakeMat("V14 Masonry", new Color(.43f,.40f,.34f), brickTex, .055f, new Vector2(4.8f,2.8f));
                Material oldWhite = LoadMat("Assets/Rosvik/Generated/V12/OldWhitePaint.mat", new Color(.82f,.82f,.77f));
                Material dark = LoadMat("Assets/Rosvik/Generated/V12/DarkMetal.mat", new Color(.10f,.12f,.13f));
                Material roof = LoadMat("Assets/Rosvik/Generated/V12/AgedMetalRoof.mat", new Color(.11f,.12f,.13f));
                Material spruce = LoadMat("Assets/Rosvik/Generated/V12/SpruceNeedles.mat", new Color(.07f,.14f,.11f));
                Material bark = LoadMat("Assets/Rosvik/Generated/V12/Bark.mat", new Color(.18f,.13f,.09f));
                Material birch = LoadMat("Assets/Rosvik/Generated/V12/BirchBark.mat", new Color(.78f,.77f,.70f));
                Material woodDark = LoadMat("Assets/Rosvik/Generated/V12/BenchWood.mat", new Color(.28f,.19f,.12f));

                GroundPass(root, snow, packed, dirty);

                Transform main = Child(root, "Huvudbyggnaden - 1970-tal");
                Transform timberSchool = Child(root, "Träskolan - ca 1900");
                if (main) MainSchoolPass(main, masonry, roof, snow, dark);
                if (timberSchool) TimberSchoolPass(timberSchool, timber, roof, snow, oldWhite, dark);

                ReplaceDrifts(root, dirty, snow);
                ReplaceVegetation(root, spruce, bark, snow, birch, dark);
                YardPass(root, packed, snow, woodDark, dark);
                TunePresentation();

                root.name = "ROSVIK_HERO_POLISH_V14";
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
                EditorPrefs.SetInt(VersionKey, Version);
                Debug.Log("ROSVIK V14 APPLIED: cleaner snow, irregular roof cover, natural drifts, continuous spruce silhouettes and improved Träskolan gable detail.");
            } catch (Exception ex) {
                Debug.LogError("ROSVIK V14 FAILED: " + ex);
            }
        }

        static Transform FindRoot() {
            string[] names={"ROSVIK_HERO_POLISH_V14","ROSVIK_POLISH_PASS_V13","ROSVIK_DETAIL_PASS_V12","ROSVIK_SITE_CALIBRATION_V11"};
            foreach(string n in names){ GameObject g=GameObject.Find(n); if(g) return g.transform; }
            return null;
        }

        static Transform Child(Transform root,string name){
            foreach(Transform t in root.GetComponentsInChildren<Transform>(true)) if(t.name==name) return t;
            return null;
        }

        static void DisablePrefix(Transform root,string prefix){
            foreach(Transform t in root.GetComponentsInChildren<Transform>(true))
                if(t!=root && t.name.StartsWith(prefix,StringComparison.Ordinal)) t.gameObject.SetActive(false);
        }

        static void DestroyPrefix(Transform root,string prefix){
            Transform[] all=root.GetComponentsInChildren<Transform>(true);
            for(int i=all.Length-1;i>=0;i--){ Transform t=all[i]; if(t!=root && t.name.StartsWith(prefix,StringComparison.Ordinal)) UnityEngine.Object.DestroyImmediate(t.gameObject); }
        }

        static void EnsureFolder(){
            if(!AssetDatabase.IsValidFolder("Assets/Rosvik/Generated")) AssetDatabase.CreateFolder("Assets/Rosvik","Generated");
            if(!AssetDatabase.IsValidFolder(GeneratedFolder)) AssetDatabase.CreateFolder("Assets/Rosvik/Generated","V14");
        }

        static float Noise(int x,int y){
            unchecked{ int h=x*374761393+y*668265263; h=(h^(h>>13))*1274126177; h^=h>>16; return ((h&4095)/4095f)-.5f; }
        }

        static Texture2D MakeNoiseTexture(string name,float baseV,float fineAmp,float broadAmp){
            string path=GeneratedFolder+"/"+name+".asset";
            Texture2D existing=AssetDatabase.LoadAssetAtPath<Texture2D>(path); if(existing) return existing;
            const int s=256; Texture2D t=new Texture2D(s,s,TextureFormat.RGB24,true){name=name,wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Bilinear};
            for(int y=0;y<s;y++) for(int x=0;x<s;x++){
                float broad=Mathf.Sin(x*.021f+y*.014f)*.45f+Mathf.Sin(x*.008f-y*.017f)*.28f;
                float v=Mathf.Clamp01(baseV+Noise(x,y)*fineAmp+broad*broadAmp);
                t.SetPixel(x,y,new Color(v,v,v));
            }
            t.Apply(true,false); AssetDatabase.CreateAsset(t,path); return t;
        }

        static Texture2D MakeWoodTexture(string name){
            string path=GeneratedFolder+"/"+name+".asset"; Texture2D e=AssetDatabase.LoadAssetAtPath<Texture2D>(path); if(e) return e;
            const int s=256; Texture2D t=new Texture2D(s,s,TextureFormat.RGB24,true){name=name,wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Bilinear};
            for(int y=0;y<s;y++) for(int x=0;x<s;x++){
                int board=x%22; float seam=board==0 ? -.13f : board==1 ? -.045f : 0f;
                float grain=Mathf.Sin(y*.11f+Mathf.Sin(x*.07f)*1.7f)*.020f;
                float v=Mathf.Clamp01(.80f+seam+grain+Noise(x,y)*.025f); t.SetPixel(x,y,new Color(v,v,v));
            }
            t.Apply(true,false); AssetDatabase.CreateAsset(t,path); return t;
        }

        static Texture2D MakeBrickTexture(string name){
            string path=GeneratedFolder+"/"+name+".asset"; Texture2D e=AssetDatabase.LoadAssetAtPath<Texture2D>(path); if(e) return e;
            const int s=256; Texture2D t=new Texture2D(s,s,TextureFormat.RGB24,true){name=name,wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Bilinear};
            for(int y=0;y<s;y++) for(int x=0;x<s;x++){
                int rh=13,bw=42,row=y/rh,ox=(row%2)*(bw/2),bx=(x+ox)%bw,by=y%rh;
                bool mortar=bx<1||by<1; float v=mortar?.55f:.79f+Noise(x,y)*.045f; v=Mathf.Clamp01(v); t.SetPixel(x,y,new Color(v,v,v));
            }
            t.Apply(true,false); AssetDatabase.CreateAsset(t,path); return t;
        }

        static Material MakeMat(string name,Color tint,Texture2D tex,float smooth,Vector2 tiling){
            string path=GeneratedFolder+"/"+name+".mat"; Material m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){ Shader sh=GraphicsSettings.defaultRenderPipeline!=null?Shader.Find("Universal Render Pipeline/Lit"):Shader.Find("Standard"); if(!sh||!sh.isSupported) sh=Shader.Find("Sprites/Default"); m=new Material(sh){name=name}; AssetDatabase.CreateAsset(m,path); }
            m.color=tint; if(m.HasProperty("_BaseColor")) m.SetColor("_BaseColor",tint);
            if(m.HasProperty("_BaseMap")){m.SetTexture("_BaseMap",tex);m.SetTextureScale("_BaseMap",tiling);} else {m.mainTexture=tex;m.mainTextureScale=tiling;}
            if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth); if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",0f); EditorUtility.SetDirty(m); return m;
        }

        static Material LoadMat(string path,Color fallback){ Material m=AssetDatabase.LoadAssetAtPath<Material>(path); return m?m:MakeMat(Path.GetFileNameWithoutExtension(path)+" fallback",fallback,MakeNoiseTexture("Fallback",.75f,.05f,.02f),.06f,Vector2.one); }

        static void SetMat(Transform root,string name,Material mat){ Transform t=Child(root,name); if(!t)return; Renderer r=t.GetComponent<Renderer>(); if(r)r.sharedMaterial=mat; }

        static GameObject Cube(string name,Transform parent,Vector3 pos,Vector3 scale,Material mat,bool collider=false){
            GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name; g.transform.SetParent(parent,false); g.transform.localPosition=pos; g.transform.localScale=scale; g.GetComponent<Renderer>().sharedMaterial=mat; if(!collider)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>()); return g;
        }

        static GameObject Cylinder(string name,Transform parent,Vector3 pos,Vector3 scale,Quaternion rot,Material mat){
            GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cylinder); g.name=name; g.transform.SetParent(parent,false); g.transform.localPosition=pos; g.transform.localScale=scale; g.transform.localRotation=rot; g.GetComponent<Renderer>().sharedMaterial=mat; UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>()); return g;
        }

        static void GroundPass(Transform root,Material snow,Material packed,Material dirty){
            SetMat(root,"Snow terrain",snow);
            string[] p={"Main cleared yard","Large central schoolyard","West play yard","Broad uphill transition","Upper Träskolan terrace","Stenskolan terrace","V12 yard worn snow strip"};
            foreach(string n in p) SetMat(root,n,packed);
            SetMat(root,"Path uphill to Träskolan",dirty); SetMat(root,"Gentle path to Stenskolan",dirty);
            DisablePrefix(root,"V13 track");
            DestroyPrefix(root,"V14 snow wear");
            GameObject a=Cube("V14 snow wear path",root,new Vector3(-16f,.098f,-6f),new Vector3(3.1f,.015f,24f),dirty,false); a.transform.localRotation=Quaternion.Euler(0f,-6f,0f);
            GameObject b=Cube("V14 snow wear service",root,new Vector3(16f,.094f,9f),new Vector3(3.6f,.015f,21f),packed,false); b.transform.localRotation=Quaternion.Euler(0f,7f,0f);
        }

        static void MainSchoolPass(Transform b,Material facade,Material roof,Material snow,Material metal){
            SetMat(b,"Main mass",facade); SetMat(b,"Roof",roof);
            DisablePrefix(b,"V13 main roof snow"); DestroyPrefix(b,"V14 main roof snow");
            IrregularRoofSnow("V14 main roof snow",b,35f,10.8f,3.25f,1.15f,.45f,.68f,.10f,snow,19);
            Cube("V14 main eave shadow",b,new Vector3(0f,3.12f,5.55f),new Vector3(35.8f,.12f,.22f),metal,false);
        }

        static void TimberSchoolPass(Transform b,Material facade,Material roof,Material snow,Material white,Material metal){
            SetMat(b,"Two storey timber body",facade); SetMat(b,"Roof",roof);
            DisablePrefix(b,"V13 timber roof snow"); DestroyPrefix(b,"V14 timber");
            IrregularRoofSnow("V14 timber roof snow",b,14.5f,8.6f,6f,3.15f,.55f,.61f,.14f,snow,13);

            float half=8.6f*.5f+.55f, rise=3.15f, angle=Mathf.Atan2(rise,half)*Mathf.Rad2Deg, slope=Mathf.Sqrt(half*half+rise*rise);
            foreach(float x in new float[]{-7.34f,7.34f}){
                GameObject f=Cube("V14 timber gable trim front",b,new Vector3(x,6f+rise*.5f,half*.5f),new Vector3(.15f,.12f,slope+.18f),white,false); f.transform.localRotation=Quaternion.Euler(angle,0f,0f);
                GameObject bk=Cube("V14 timber gable trim back",b,new Vector3(x,6f+rise*.5f,-half*.5f),new Vector3(.15f,.12f,slope+.18f),white,false); bk.transform.localRotation=Quaternion.Euler(-angle,0f,0f);
            }
            Cube("V14 timber gable ridge L",b,new Vector3(-7.34f,9.15f,0f),new Vector3(.16f,.18f,.28f),metal,false);
            Cube("V14 timber gable ridge R",b,new Vector3(7.34f,9.15f,0f),new Vector3(.16f,.18f,.28f),metal,false);
        }

        static void IrregularRoofSnow(string prefix,Transform b,float width,float depth,float wallH,float rise,float overhang,float coverage,float jitter,Material snow,int segments){
            float halfRun=depth*.5f+overhang, angle=Mathf.Atan2(rise,halfRun)*Mathf.Rad2Deg, slope=Mathf.Sqrt(halfRun*halfRun+rise*rise);
            for(int side=-1;side<=1;side+=2){
                Vector3[] v=new Vector3[(segments+1)*2]; int[] tri=new int[segments*6];
                for(int i=0;i<=segments;i++){
                    float u=i/(float)segments; float x=Mathf.Lerp(-width*.5f-overhang*.75f,width*.5f+overhang*.75f,u);
                    float edge=Mathf.Clamp01(coverage + Mathf.Sin(i*1.73f+side*.8f)*jitter + Mathf.Sin(i*.61f)*jitter*.45f);
                    float zRidge=0f, zEdge=halfRun*edge*side;
                    float yRidge=wallH+rise+.055f, yEdge=wallH+rise*(1f-edge)+.055f;
                    v[i*2]=new Vector3(x,yRidge,zRidge); v[i*2+1]=new Vector3(x,yEdge,zEdge);
                }
                int ti=0; for(int i=0;i<segments;i++){int a=i*2,b0=i*2+1,c=(i+1)*2,d=(i+1)*2+1; if(side>0){tri[ti++]=a;tri[ti++]=c;tri[ti++]=d;tri[ti++]=a;tri[ti++]=d;tri[ti++]=b0;}else{tri[ti++]=a;tri[ti++]=d;tri[ti++]=c;tri[ti++]=a;tri[ti++]=b0;tri[ti++]=d;}}
                Mesh m=new Mesh{name=prefix+(side>0?" front mesh":" back mesh"),vertices=v,triangles=tri}; m.RecalculateNormals();m.RecalculateBounds();
                GameObject g=new GameObject(prefix+(side>0?" front":" back")); g.transform.SetParent(b,false); g.AddComponent<MeshFilter>().sharedMesh=m; g.AddComponent<MeshRenderer>().sharedMaterial=snow;
            }
        }

        static void ReplaceDrifts(Transform root,Material dirty,Material clean){
            DisablePrefix(root,"V13 drift"); DestroyPrefix(root,"V14 drift");
            Vector3[] p={new Vector3(-39f,.12f,25.7f),new Vector3(-28f,.12f,25.9f),new Vector3(-16f,.10f,25.6f),new Vector3(-4f,.11f,25.8f),new Vector3(9f,.10f,25.6f),new Vector3(22f,.12f,25.9f),new Vector3(-34f,.30f,-24f),new Vector3(-27f,.36f,-26.3f),new Vector3(-20f,.42f,-28.1f)};
            for(int i=0;i<p.Length;i++) DriftCluster(root,p[i],.85f+(i%3)*.12f,i<6?dirty:clean,"V14 drift "+i,i);
        }

        static void DriftCluster(Transform parent,Vector3 pos,float scale,Material mat,string name,int seed){
            Transform r=new GameObject(name).transform; r.SetParent(parent,false); r.localPosition=pos; r.localRotation=Quaternion.Euler(0f,(seed*31)%180,0f);
            for(int i=0;i<3;i++){
                float x=(i-1)*1.35f*scale + Mathf.Sin(seed*1.7f+i)*.35f; float z=Mathf.Cos(seed*.9f+i*1.3f)*.35f;
                float sx=(2.0f+i*.35f)*scale, sz=(.65f+(i%2)*.18f)*scale, h=(.40f+(i==1?.14f:0f))*scale;
                Mound(r,new Vector3(x,h*.18f,z),sx,sz,h,mat,"lobe "+i,seed+i*7);
            }
        }

        static void Mound(Transform parent,Vector3 pos,float rx,float rz,float h,Material mat,string name,int seed){
            const int seg=18; Vector3[] v=new Vector3[seg+2]; int[] tri=new int[seg*6]; v[0]=new Vector3(0f,h,0f); v[1]=new Vector3(0f,-.02f,0f);
            for(int i=0;i<seg;i++){float a=i*Mathf.PI*2f/seg; float rr=1f+.12f*Mathf.Sin(i*2.1f+seed)+.07f*Mathf.Sin(i*.7f); v[i+2]=new Vector3(Mathf.Cos(a)*rx*rr,.02f*Mathf.Sin(i*1.4f),Mathf.Sin(a)*rz*rr);}
            int ti=0;for(int i=0;i<seg;i++){int a=2+i,b=2+((i+1)%seg);tri[ti++]=0;tri[ti++]=a;tri[ti++]=b;tri[ti++]=1;tri[ti++]=b;tri[ti++]=a;}
            Mesh m=new Mesh{name=name+" mesh",vertices=v,triangles=tri};m.RecalculateNormals();m.RecalculateBounds();GameObject g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.AddComponent<MeshFilter>().sharedMesh=m;g.AddComponent<MeshRenderer>().sharedMaterial=mat;
        }

        static void ReplaceVegetation(Transform root,Material needles,Material bark,Material snow,Material birch,Material branch){
            DisablePrefix(root,"V13 spruce"); DisablePrefix(root,"V12 birch"); DestroyPrefix(root,"V14 spruce"); DestroyPrefix(root,"V14 birch");
            Vector3[] sp={new Vector3(-48f,0f,-40f),new Vector3(-42f,0f,-46f),new Vector3(-34f,.72f,-50f),new Vector3(-20f,.3f,-53f),new Vector3(6f,.15f,-51f),new Vector3(39f,.25f,-47f),new Vector3(52f,0f,-22f),new Vector3(-54f,0f,3f),new Vector3(-45f,0f,29f),new Vector3(46f,0f,31f),new Vector3(57f,0f,11f)};
            for(int i=0;i<sp.Length;i++) ContinuousSpruce(root,sp[i],.92f+(i%4)*.10f,needles,bark,snow,"V14 spruce "+i,i);
            Vector3[] bp={new Vector3(-22f,.20f,-18f),new Vector3(-13f,.12f,-20f),new Vector3(12f,.10f,-18f),new Vector3(22f,.14f,-15f),new Vector3(-36f,.72f,-32f),new Vector3(35f,.30f,-31f)};
            for(int i=0;i<bp.Length;i++) NaturalBirch(root,bp[i],.95f+(i%3)*.10f,birch,branch,"V14 birch "+i,i);
        }

        static void ContinuousSpruce(Transform parent,Vector3 pos,float scale,Material needles,Material bark,Material snow,string name,int seed){
            Transform r=new GameObject(name).transform;r.SetParent(parent,false);r.localPosition=pos; Cylinder("trunk",r,new Vector3(0f,2.45f*scale,0f),new Vector3(.14f,2.45f*scale,.14f),Quaternion.identity,bark);
            const int rings=11,seg=16; Vector3[] v=new Vector3[(rings+1)*seg+1]; int[] tri=new int[rings*seg*6];
            for(int ring=0;ring<=rings;ring++){float y=(.55f+ring*.47f)*scale; float t=ring/(float)rings; float baseR=Mathf.Lerp(2.25f,.16f,t)*scale; float flare=(ring%2==0?1.10f:.91f); for(int j=0;j<seg;j++){float a=j*Mathf.PI*2f/seg; float irr=1f+.10f*Mathf.Sin(j*1.9f+ring*.7f+seed)+.06f*Mathf.Sin(j*.73f+ring); float rr=baseR*flare*irr; v[ring*seg+j]=new Vector3(Mathf.Cos(a)*rr,y,Mathf.Sin(a)*rr);}}
            int tip=(rings+1)*seg;v[tip]=new Vector3(0f,6.05f*scale,0f);int ti=0;for(int ring=0;ring<rings;ring++)for(int j=0;j<seg;j++){int a=ring*seg+j,b=ring*seg+(j+1)%seg,c=(ring+1)*seg+j,d=(ring+1)*seg+(j+1)%seg;tri[ti++]=a;tri[ti++]=c;tri[ti++]=d;tri[ti++]=a;tri[ti++]=d;tri[ti++]=b;} Mesh m=new Mesh{name=name+" mesh",vertices=v,triangles=tri};m.RecalculateNormals();m.RecalculateBounds();GameObject crown=new GameObject("continuous crown");crown.transform.SetParent(r,false);crown.AddComponent<MeshFilter>().sharedMesh=m;crown.AddComponent<MeshRenderer>().sharedMaterial=needles;
            if(seed%2==0){ Mound(r,new Vector3(.18f,1.25f*scale,-.05f),1.3f*scale,.95f*scale,.16f*scale,snow,"branch snow",seed); }
        }

        static void NaturalBirch(Transform parent,Vector3 pos,float scale,Material bark,Material branch,string name,int seed){
            Transform r=new GameObject(name).transform;r.SetParent(parent,false);r.localPosition=pos; Cylinder("trunk",r,new Vector3(0f,2.75f*scale,0f),new Vector3(.11f,2.75f*scale,.11f),Quaternion.Euler(0f,0f,(seed%3-1)*2.5f),bark);
            Branch(r,new Vector3(0f,3.5f*scale,0f),1.45f*scale,-38f,18f,branch); Branch(r,new Vector3(0f,4.0f*scale,0f),1.25f*scale,43f,-24f,branch); Branch(r,new Vector3(0f,4.45f*scale,0f),1.05f*scale,-31f,-36f,branch); Branch(r,new Vector3(0f,4.85f*scale,0f),.90f*scale,28f,42f,branch); Branch(r,new Vector3(0f,5.15f*scale,0f),.72f*scale,-20f,12f,branch);
        }

        static void Branch(Transform r,Vector3 pos,float len,float zAngle,float yAngle,Material mat){ Cylinder("branch",r,pos,new Vector3(.04f,len*.5f,.04f),Quaternion.Euler(0f,yAngle,zAngle),mat); }

        static void YardPass(Transform root,Material packed,Material snow,Material wood,Material metal){
            string[] sandbox={"Sandbox N","Sandbox S","Sandbox W","Sandbox E"}; foreach(string n in sandbox) SetMat(root,n,wood);
            DestroyPrefix(root,"V14 sandbox"); Cube("V14 sandbox snow fill",root,new Vector3(-8f,.105f,-13f),new Vector3(7.3f,.045f,5.3f),snow,false);
            DestroyPrefix(root,"V14 yard");
            Cube("V14 yard low kerb A",root,new Vector3(-3f,.13f,18.1f),new Vector3(24f,.12f,.18f),metal,false);
            Cube("V14 yard low kerb B",root,new Vector3(20f,.13f,18.1f),new Vector3(15f,.12f,.18f),metal,false);
        }

        static void TunePresentation(){
            Camera cam=Camera.main; if(cam&&cam.orthographic)cam.orthographicSize=11.2f;
            IsometricCameraRig rig=UnityEngine.Object.FindFirstObjectByType<IsometricCameraRig>(); if(rig)rig.orthographicSize=11.2f;
            foreach(Light l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) if(l.type==LightType.Directional){l.intensity=.78f;l.color=new Color(.88f,.92f,1f);l.shadowStrength=.68f;break;}
            RenderSettings.ambientLight=new Color(.46f,.49f,.51f); RenderSettings.fogColor=new Color(.63f,.69f,.72f); RenderSettings.fogDensity=.0028f;
        }
    }
}
#endif