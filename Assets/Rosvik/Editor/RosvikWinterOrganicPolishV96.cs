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
    public static class RosvikWinterOrganicPolishV96 {
        const int Version = 96;
        const string Key = "ROSVIK_WINTER_ORGANIC_POLISH_V96";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string Root95 = "V95 SOFT WINTER ENVIRONMENT";
        const string Root96 = "V96 ORGANIC WINTER POLISH";
        const string Generated = "Assets/Rosvik/GeneratedV96";
        static int retries;

        static Material pine, pineDark, trunk, birch, snow, metal;
        static Mesh spruceMesh, trunkMesh, branchMesh;

        static RosvikWinterOrganicPolishV96() {
            if (EditorPrefs.GetInt(Key, 0) >= Version) return;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V96 ORGANIC WINTER POLISH")]
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
                if (!Apply() && retries++ < 14) EditorApplication.delayCall += Auto;
            } catch (Exception ex) {
                Debug.LogError("V96 ORGANIC POLISH FAILED: " + ex);
            }
        }

        static bool Apply() {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject v95 = GameObject.Find(Root95);
            GameObject campus = GameObject.Find("ROSVIK CAMPUS · V92 ASTRA BENCHMARK");
            if (!v95 || !campus) return false;

            Transform schoolVol = Find(campus.transform, "SCHOOL MAIN INDOOR V92");
            Transform sportVol = Find(campus.transform, "SPORTHALL INDOOR V92");
            Transform arenaVol = Find(campus.transform, "ICEHALL INDOOR V92");
            if (!schoolVol || !sportVol || !arenaVol) return false;

            Directory.CreateDirectory(Generated);
            pine = LoadMat("Assets/Rosvik/GeneratedV95/V95 pine.mat");
            pineDark = LoadMat("Assets/Rosvik/GeneratedV95/V95 pine dark.mat") ?? pine;
            trunk = LoadMat("Assets/Rosvik/GeneratedV95/V95 spruce trunk.mat");
            birch = LoadMat("Assets/Rosvik/GeneratedV95/V95 birch bark.mat") ?? trunk;
            snow = LoadMat("Assets/Rosvik/GeneratedV95/V95 soft snow.mat");
            metal = LoadMat("Assets/Rosvik/GeneratedV94/V94 metal.mat");
            if (!pine || !trunk || !snow) return false;

            spruceMesh = SaveMesh(BuildSpruceMesh(16), Generated + "/continuous_spruce.asset");
            trunkMesh = SaveMesh(BuildTaperedCylinder(9, .50f, .31f), Generated + "/tapered_trunk.asset");
            branchMesh = SaveMesh(BuildTaperedCylinder(7, .50f, .18f), Generated + "/tapered_branch.asset");

            GameObject old = GameObject.Find(Root96);
            if (old) UnityEngine.Object.DestroyImmediate(old);
            GameObject root = new GameObject(Root96);
            root.transform.SetParent(v95.transform.parent, true);

            // Make the actual soft snow mesh physical so footprints do not randomly miss it.
            Transform snowSurface = Find(v95.transform, "soft continuous campus snow");
            if (snowSurface) {
                MeshFilter mf = snowSurface.GetComponent<MeshFilter>();
                MeshCollider mc = snowSurface.GetComponent<MeshCollider>();
                if (!mc) mc = snowSurface.gameObject.AddComponent<MeshCollider>();
                if (mf) mc.sharedMesh = mf.sharedMesh;
                mc.convex = false;
            }

            // Retire the layered-cone trees from V95; keep its snow, banks, paths and snowfall.
            Transform oldNature = Find(v95.transform, "04 ORGANIC WINTER NATURE");
            if (oldNature) oldNature.gameObject.SetActive(false);

            Vector3 schoolC = Flat(schoolVol.position);
            Vector3 sportC = Flat(sportVol.position);
            Vector3 arenaC = Flat(arenaVol.position);
            Transform nature = Group(root.transform, "01 CONTINUOUS TREE SILHOUETTES");
            BuildNature(nature, schoolC, sportC, arenaC);

            Transform details = Group(root.transform, "02 SOFTER CAMPUS DETAILS");
            BuildGroundHummocks(details, schoolC, sportC, arenaC);
            ReplaceUglyBikeStands(details, schoolC);

            WinterFootprintsV95 fp = UnityEngine.Object.FindFirstObjectByType<WinterFootprintsV95>();
            if (fp) {
                fp.stepSpacing = .43f;
                fp.footSeparation = .155f;
                fp.maxFootprints = 260;
                fp.surfaceOffset = .022f;
                EditorUtility.SetDirty(fp);
            }

            ParticleSystem snowPs = null;
            Transform weather = Find(v95.transform, "05 ACTIVE SNOW WEATHER");
            if (weather) snowPs = weather.GetComponentInChildren<ParticleSystem>(true);
            if (snowPs) {
                var emission = snowPs.emission; emission.rateOverTime = 72f;
                var main = snowPs.main; main.startSize = new ParticleSystem.MinMaxCurve(.028f,.070f); main.maxParticles = 900;
            }

            CleanupMissingScripts();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorPrefs.SetInt(Key, Version);
            SceneView.RepaintAll();
            Debug.Log("V96 COMPLETE — continuous spruce silhouettes, reliable snow collider/footprints, softer campus detail and denser fine snowfall.");
            return true;
        }

        static void BuildNature(Transform parent, Vector3 schoolC, Vector3 sportC, Vector3 arenaC) {
            Vector3[] spruces = {
                schoolC+new Vector3(-18,0,-13), schoolC+new Vector3(-20,0,-2), schoolC+new Vector3(-17,0,13),
                schoolC+new Vector3(-8,0,16), sportC+new Vector3(11,0,-13), sportC+new Vector3(13,0,9),
                arenaC+new Vector3(-17,0,13), arenaC+new Vector3(-8,0,17), arenaC+new Vector3(9,0,17), arenaC+new Vector3(18,0,8)
            };
            for (int i=0;i<spruces.Length;i++) Spruce(parent, spruces[i], .88f + (i%4)*.08f, i*43+7);

            BareTree(parent, schoolC+new Vector3(-13,0,-14.2f), 1.0f, 11);
            BareTree(parent, schoolC+new Vector3(7,0,-13.0f), .90f, 19);
            BareTree(parent, arenaC+new Vector3(-15,0,-12.0f), 1.05f, 27);
            BareTree(parent, arenaC+new Vector3(15,0,11f), .95f, 33);
        }

        static void Spruce(Transform parent, Vector3 pos, float scale, int seed) {
            Transform r = Group(parent, "natural spruce");
            r.position = pos;
            r.localScale = Vector3.one * scale;
            r.rotation = Quaternion.Euler(0f, seed % 360, 0f);

            MeshObj("tapered spruce trunk", r, trunkMesh, new Vector3(0,1.0f,0), new Vector3(.28f,2.1f,.28f), trunk);
            MeshObj("continuous spruce canopy", r, spruceMesh, new Vector3(0,2.15f,0), new Vector3(2.65f,3.45f,2.65f), (seed%2==0)?pine:pineDark);

            // A few irregular snow rests, deliberately sparse so the tree does not become a layer cake again.
            SnowPatch(r, new Vector3(.22f,2.18f,-.12f), .92f, .28f, seed+1);
            SnowPatch(r, new Vector3(-.18f,2.72f,.10f), .68f, .21f, seed+2);
        }

        static void SnowPatch(Transform parent, Vector3 localPos, float radius, float height, int seed) {
            Mesh m = BuildSnowPillow(12, radius, height, seed);
            GameObject g = new GameObject("irregular branch snow");
            g.transform.SetParent(parent,false);
            g.transform.localPosition = localPos;
            MeshFilter mf=g.AddComponent<MeshFilter>(); mf.sharedMesh=m;
            MeshRenderer mr=g.AddComponent<MeshRenderer>(); mr.sharedMaterial=snow; mr.shadowCastingMode=ShadowCastingMode.On;
        }

        static void BareTree(Transform parent, Vector3 pos, float scale, int seed) {
            Transform r = Group(parent, "natural bare birch");
            r.position = pos; r.localScale = Vector3.one*scale; r.rotation = Quaternion.Euler(0,seed*19%360,0);
            Branch(r,new Vector3(0,0,0),new Vector3(.02f,3.7f,.03f),.16f,birch);
            Branch(r,new Vector3(0,1.45f,0),new Vector3(-.72f,2.55f,.14f),.080f,birch);
            Branch(r,new Vector3(.02f,1.82f,0),new Vector3(.83f,2.95f,-.18f),.073f,birch);
            Branch(r,new Vector3(-.46f,2.18f,.08f),new Vector3(-1.02f,3.22f,.21f),.044f,birch);
            Branch(r,new Vector3(.46f,2.40f,-.10f),new Vector3(1.10f,3.52f,-.30f),.042f,birch);
            Branch(r,new Vector3(.02f,2.64f,.02f),new Vector3(-.28f,3.72f,.55f),.036f,birch);
        }

        static void Branch(Transform parent, Vector3 a, Vector3 b, float radius, Material mat) {
            Vector3 d=b-a; if(d.sqrMagnitude<.001f)return;
            GameObject g=new GameObject("tapered branch"); g.transform.SetParent(parent,false);
            g.transform.localPosition=(a+b)*.5f;
            g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,d.normalized);
            g.transform.localScale=new Vector3(radius,d.magnitude,radius);
            MeshFilter mf=g.AddComponent<MeshFilter>(); mf.sharedMesh=branchMesh;
            MeshRenderer mr=g.AddComponent<MeshRenderer>(); mr.sharedMaterial=mat; mr.shadowCastingMode=ShadowCastingMode.On;
        }

        static void BuildGroundHummocks(Transform parent, Vector3 schoolC, Vector3 sportC, Vector3 arenaC) {
            Vector3[] pts = {
                schoolC+new Vector3(-14,0,-10), schoolC+new Vector3(13,0,-11),
                sportC+new Vector3(9,0,-11), arenaC+new Vector3(-12,0,-15), arenaC+new Vector3(12,0,-14)
            };
            for(int i=0;i<pts.Length;i++) {
                Mesh m=BuildSnowPillow(18,1.4f+(i%2)*.35f,.20f+(i%3)*.05f,100+i);
                GameObject g=new GameObject("soft windblown snow mound"); g.transform.SetParent(parent,true); g.transform.position=pts[i];
                MeshFilter mf=g.AddComponent<MeshFilter>(); mf.sharedMesh=m;
                MeshRenderer mr=g.AddComponent<MeshRenderer>(); mr.sharedMaterial=snow; mr.shadowCastingMode=ShadowCastingMode.On;
            }
        }

        static void ReplaceUglyBikeStands(Transform parent, Vector3 schoolC) {
            GameObject v94=GameObject.Find("V94 CAMPUS ART PASS");
            if(v94) foreach(Transform t in v94.GetComponentsInChildren<Transform>(true)) if(t.name=="bike stand") t.gameObject.SetActive(false);
            Vector3 c=schoolC+new Vector3(-9.4f,0,-9.6f);
            for(int i=0;i<7;i++) {
                float x=i*.62f;
                GameObject rail=new GameObject("rounded bike rack hoop"); rail.transform.SetParent(parent,true); rail.transform.position=c+new Vector3(x,.34f,0);
                LineRenderer lr=rail.AddComponent<LineRenderer>(); lr.useWorldSpace=false; lr.positionCount=9; lr.widthMultiplier=.045f; lr.sharedMaterial=metal;
                for(int k=0;k<9;k++){float a=Mathf.PI*k/8f;lr.SetPosition(k,new Vector3(Mathf.Cos(a)*.27f,Mathf.Sin(a)*.34f-.34f,0));}
                lr.numCapVertices=3; lr.numCornerVertices=3;
            }
        }

        static Mesh BuildSpruceMesh(int sides) {
            float[] ys={-.50f,-.38f,-.24f,-.10f,.05f,.20f,.34f,.49f};
            float[] rs={.48f,.42f,.50f,.34f,.39f,.25f,.28f,.12f};
            List<Vector3> v=new List<Vector3>(); List<int> tr=new List<int>();
            for(int r=0;r<ys.Length;r++) for(int s=0;s<sides;s++) {
                float a=s*Mathf.PI*2f/sides;
                float irregular=1f + .10f*Mathf.Sin(a*3f+r*.9f) + .05f*Mathf.Sin(a*5f-r*.4f);
                v.Add(new Vector3(Mathf.Cos(a)*rs[r]*irregular,ys[r],Mathf.Sin(a)*rs[r]*irregular));
            }
            int top=v.Count; v.Add(new Vector3(0,.56f,0));
            for(int r=0;r<ys.Length-1;r++) for(int s=0;s<sides;s++) {
                int a=r*sides+s,b=r*sides+(s+1)%sides,c=(r+1)*sides+s,d=(r+1)*sides+(s+1)%sides;
                tr.Add(a);tr.Add(c);tr.Add(b); tr.Add(b);tr.Add(c);tr.Add(d);
            }
            int last=(ys.Length-1)*sides; for(int s=0;s<sides;s++){tr.Add(last+s);tr.Add(top);tr.Add(last+(s+1)%sides);}
            Mesh m=new Mesh{name="V96 continuous irregular spruce"};m.SetVertices(v);m.SetTriangles(tr,0);m.RecalculateNormals();m.RecalculateBounds();return m;
        }

        static Mesh BuildTaperedCylinder(int sides,float bottomRadius,float topRadius) {
            List<Vector3> v=new List<Vector3>(); List<int> tr=new List<int>();
            for(int y=0;y<2;y++) for(int s=0;s<sides;s++){float a=s*Mathf.PI*2f/sides;float r=y==0?bottomRadius:topRadius;v.Add(new Vector3(Mathf.Cos(a)*r,y-.5f,Mathf.Sin(a)*r));}
            for(int s=0;s<sides;s++){int a=s,b=(s+1)%sides,c=sides+s,d=sides+(s+1)%sides;tr.Add(a);tr.Add(c);tr.Add(b);tr.Add(b);tr.Add(c);tr.Add(d);}
            Mesh m=new Mesh{name="V96 tapered wood"};m.SetVertices(v);m.SetTriangles(tr,0);m.RecalculateNormals();m.RecalculateBounds();return m;
        }

        static Mesh BuildSnowPillow(int sides,float radius,float height,int seed) {
            List<Vector3> v=new List<Vector3>{new Vector3(0,height,0)}; List<int> tr=new List<int>();
            for(int s=0;s<sides;s++){float a=s*Mathf.PI*2f/sides;float rr=radius*(.82f+.18f*Mathf.PerlinNoise(seed*.07f+s*.31f,seed*.11f));v.Add(new Vector3(Mathf.Cos(a)*rr,0,Mathf.Sin(a)*rr));}
            for(int s=0;s<sides;s++){tr.Add(0);tr.Add(1+(s+1)%sides);tr.Add(1+s);}
            Mesh m=new Mesh{name="V96 soft snow pillow"};m.SetVertices(v);m.SetTriangles(tr,0);m.RecalculateNormals();m.RecalculateBounds();return m;
        }

        static GameObject MeshObj(string name,Transform parent,Mesh mesh,Vector3 localPos,Vector3 localScale,Material mat) {
            GameObject g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=localPos;g.transform.localScale=localScale;
            MeshFilter mf=g.AddComponent<MeshFilter>();mf.sharedMesh=mesh;MeshRenderer mr=g.AddComponent<MeshRenderer>();mr.sharedMaterial=mat;mr.shadowCastingMode=ShadowCastingMode.On;mr.receiveShadows=true;return g;
        }

        static Material LoadMat(string path){return AssetDatabase.LoadAssetAtPath<Material>(path);}
        static Mesh SaveMesh(Mesh source,string path){Mesh old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(source,old);UnityEngine.Object.DestroyImmediate(source);EditorUtility.SetDirty(old);return old;}AssetDatabase.CreateAsset(source,path);return source;}
        static Transform Group(Transform p,string name){GameObject g=new GameObject(name);g.transform.SetParent(p,true);return g.transform;}
        static Transform Find(Transform root,string exact){foreach(Transform t in root.GetComponentsInChildren<Transform>(true))if(t.name==exact)return t;return null;}
        static Vector3 Flat(Vector3 p){p.y=0;return p;}
        static void CleanupMissingScripts(){foreach(GameObject g in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(g&&g.scene.IsValid())GameObjectUtility.RemoveMonoBehavioursWithMissingScript(g);}
    }
}
#endif
