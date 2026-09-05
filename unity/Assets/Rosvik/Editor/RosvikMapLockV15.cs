#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikMapLockV15 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_MAP_LOCK_VERSION";
        const int Version = 15;

        static RosvikMapLockV15() => EditorApplication.delayCall += Apply;

        [MenuItem("Rosvik/Apply Map Lock V15")]
        public static void Apply() {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorPrefs.GetInt(Key,0) >= Version && System.IO.File.Exists(ScenePath)) return;
            try {
                RosvikHeroPolishV14.Apply();
                var scene = EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
                Transform root = FindRoot();
                if (!root) return;

                List<RosvikOsmV15.Way> ways = RosvikOsmV15.LoadWays();
                if (ways == null || ways.Count == 0) return;

                DisableGuessedLayout(root);
                Transform map = Child(root,"OSM_ACTUAL_LAYOUT_V15");
                if (map) UnityEngine.Object.DestroyImmediate(map.gameObject);
                map = new GameObject("OSM_ACTUAL_LAYOUT_V15").transform;
                map.SetParent(root,false);

                Material ground=Mat("V15 thaw ground",new Color(.25f,.29f,.19f),.05f);
                Material road=Mat("V15 wet asphalt",new Color(.11f,.12f,.12f),.22f);
                Material service=Mat("V15 service road",new Color(.16f,.16f,.15f),.15f);
                Material foot=Mat("V15 footpath",new Color(.28f,.27f,.23f),.10f);
                Material wall=Mat("V15 mapped building",new Color(.42f,.43f,.40f),.07f);
                Material roof=Mat("V15 mapped roof",new Color(.18f,.19f,.19f),.12f);
                Material pitch=Mat("V15 football grass",new Color(.24f,.38f,.19f),.04f);
                Material line=Mat("V15 pitch line",new Color(.76f,.78f,.69f),.08f);
                Material water=Mat("V15 puddle",new Color(.18f,.24f,.26f),.62f);
                Material snow=Mat("V15 snow remnant",new Color(.76f,.80f,.80f),.10f);

                Cube("V15 thaw terrain",map,new Vector3(65f,-.22f,-95f),new Vector3(430f,.35f,430f),ground,true);
                BuildRoads(map,ways,road,service,foot);
                BuildBuildings(map,ways,wall,roof);
                int pitchCount=BuildPitches(map,ways,pitch,line);
                Puddles(map,water); SnowPatches(map,snow);

                var oldWay=ways.FirstOrDefault(w=>w.Id==163199461);
                var mainWay=ways.FirstOrDefault(w=>w.Id==163199458);
                var arenaWay=ways.FirstOrDefault(w=>w.Id==163199454);

                Fit(Child(root,"Träskolan - ca 1900"),oldWay,14.5f,8.6f,.72f);
                Fit(Child(root,"Huvudbyggnaden - 1970-tal"),mainWay,35f,10.8f,.05f);
                Fit(Child(root,"Norrbotten Stål Arena - background landmark"),arenaWay,27f,15f,.02f);

                Transform stone=Child(root,"Stenskolan - 1940/50-tal"); if(stone) stone.gameObject.SetActive(false);
                Transform module=Child(root,"Tillfällig skolmodul"); if(module) module.gameObject.SetActive(false);

                if(mainWay!=null){
                    Vector3 c=RosvikOsmV15.Centroid(mainWay);
                    Transform player=Child(root,"PLAYER_PLACEHOLDER"); if(player) player.localPosition=c+new Vector3(-8f,.10f,14f);
                }

                Camera cam=Camera.main; if(cam&&cam.orthographic)cam.orthographicSize=15f;
                IsometricCameraRig rig=UnityEngine.Object.FindFirstObjectByType<IsometricCameraRig>(); if(rig)rig.orthographicSize=15f;

                root.name="ROSVIK_MAP_LOCK_V15";
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene,ScenePath);
                AssetDatabase.SaveAssets();
                EditorPrefs.SetInt(Key,Version);
                Debug.Log("ROSVIK V15: real OSM layout imported. Roads/buildings/pitches use map coordinates; Rosviks skola ways 163199461/163199458 and Norrbotten Stål Arena way 163199454 are locked to measured metre positions. Mapped pitches: "+pitchCount);
            } catch(Exception ex){ Debug.LogError("ROSVIK V15 MAP LOCK FAILED: "+ex); }
        }

        static void DisableGuessedLayout(Transform root){
            string[] exact={"Snow terrain","Skolgränd road","Main cleared yard","Large central schoolyard","West play yard","Broad uphill transition","Path uphill to Träskolan","Upper Träskolan terrace","Gentle path to Stenskolan","Stenskolan terrace"};
            foreach(string n in exact){Transform t=Child(root,n);if(t)t.gameObject.SetActive(false);}
            string[] pref={"Ploughed snow bank","V11 lamp post","V12 yard","V13 drift","V13 track","V14 drift","V14 yard","V12 spruce","V13 spruce","V14 spruce","V12 birch","V14 birch"};
            foreach(Transform t in root.GetComponentsInChildren<Transform>(true)){
                if(t==root)continue;
                if(pref.Any(p=>t.name.StartsWith(p,StringComparison.Ordinal)))t.gameObject.SetActive(false);
                if(t.name.IndexOf("roof snow",StringComparison.OrdinalIgnoreCase)>=0)t.gameObject.SetActive(false);
            }
        }

        static void BuildRoads(Transform parent,List<RosvikOsmV15.Way> ways,Material road,Material service,Material foot){
            foreach(var w in ways){
                string h=w.Tag("highway"); if(string.IsNullOrEmpty(h))continue;
                float width; Material mat;
                if(h=="residential"||h=="unclassified"){width=5.2f;mat=road;}
                else if(h=="service"||h=="living_street"){width=3.7f;mat=service;}
                else if(h=="footway"||h=="path"||h=="cycleway"){width=1.8f;mat=foot;}
                else continue;
                Transform r=new GameObject("OSM road "+w.Id+Suffix(w)).transform;r.SetParent(parent,false);
                for(int i=0;i<w.Nodes.Count-1;i++)Segment(r,w.Nodes[i].Pos,w.Nodes[i+1].Pos,width,mat);
            }
        }

        static void Segment(Transform parent,Vector3 a,Vector3 b,float width,Material mat){
            Vector3 d=b-a;if(d.sqrMagnitude<.01f)return;
            GameObject g=Cube("segment",parent,(a+b)*.5f+Vector3.up*.025f,new Vector3(width,.08f,d.magnitude),mat,false);
            g.transform.localRotation=Quaternion.LookRotation(d.normalized,Vector3.up);
        }

        static void BuildBuildings(Transform parent,List<RosvikOsmV15.Way> ways,Material wall,Material roof){
            foreach(var w in ways){
                string tag=w.Tag("building");if(string.IsNullOrEmpty(tag)||tag=="no"||!w.Closed)continue;
                if(w.Id==163199461||w.Id==163199458||w.Id==163199454)continue;
                var b=RosvikOsmV15.Bounds(w);if(b.Width<1f||b.Depth<1f)continue;
                float h=(tag=="garage"||tag=="shed")?2.5f:(tag=="house"||tag=="residential")?4.2f:3.4f;
                Quaternion rot=Quaternion.FromToRotation(Vector3.right,b.AxisX);
                Transform r=new GameObject("OSM building "+w.Id+" "+tag+Suffix(w)).transform;r.SetParent(parent,false);
                GameObject body=Cube("body",r,b.Center+Vector3.up*(h*.5f),new Vector3(b.Width,h,b.Depth),wall,false);body.transform.localRotation=rot;
                GameObject cap=Cube("roof",r,b.Center+Vector3.up*(h+.08f),new Vector3(b.Width+.15f,.16f,b.Depth+.15f),roof,false);cap.transform.localRotation=rot;
            }
        }

        static int BuildPitches(Transform parent,List<RosvikOsmV15.Way> ways,Material grass,Material lines){
            int count=0;
            foreach(var w in ways){
                if((w.Tag("leisure")!="pitch"&&w.Tag("sport")!="soccer")||!w.Closed)continue;
                var b=RosvikOsmV15.Bounds(w);if(b.Width<5f||b.Depth<5f)continue;
                Quaternion rot=Quaternion.FromToRotation(Vector3.right,b.AxisX);
                Transform p=new GameObject("OSM pitch "+w.Id+Suffix(w)).transform;p.SetParent(parent,false);
                GameObject f=Cube("field",p,b.Center+Vector3.up*.06f,new Vector3(b.Width,.10f,b.Depth),grass,false);f.transform.localRotation=rot;
                PitchLine(p,b,rot,new Vector3(b.Width,.02f,.10f),new Vector3(0,.13f,b.Depth*.5f),lines);
                PitchLine(p,b,rot,new Vector3(b.Width,.02f,.10f),new Vector3(0,.13f,-b.Depth*.5f),lines);
                PitchLine(p,b,rot,new Vector3(.10f,.02f,b.Depth),new Vector3(b.Width*.5f,.13f,0),lines);
                PitchLine(p,b,rot,new Vector3(.10f,.02f,b.Depth),new Vector3(-b.Width*.5f,.13f,0),lines);
                count++;
            }
            return count;
        }

        static void PitchLine(Transform p,RosvikOsmV15.OBounds b,Quaternion rot,Vector3 scale,Vector3 offset,Material mat){GameObject l=Cube("line",p,b.Center+rot*offset,scale,mat,false);l.transform.localRotation=rot;}

        static void Fit(Transform model,RosvikOsmV15.Way way,float nominalW,float nominalD,float y){
            if(!model||way==null)return;
            var b=RosvikOsmV15.Bounds(way);
            model.localPosition=new Vector3(b.Center.x,y,b.Center.z);
            model.localRotation=Quaternion.FromToRotation(Vector3.right,b.AxisX);
            Vector3 s=model.localScale;s.x=Mathf.Clamp(b.Width/nominalW,.55f,2.2f);s.z=Mathf.Clamp(b.Depth/nominalD,.55f,2.2f);s.y=1f;model.localScale=s;
        }

        static void Puddles(Transform p,Material m){Vector3[] a={new Vector3(50,.04f,-70),new Vector3(80,.04f,-105),new Vector3(115,.04f,-90)};for(int i=0;i<a.Length;i++){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name="V15 puddle "+i;g.transform.SetParent(p,false);g.transform.localPosition=a[i];g.transform.localScale=new Vector3(4+i,.018f,1.8f+i*.25f);g.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());}}
        static void SnowPatches(Transform p,Material m){Vector3[] a={new Vector3(-7,.05f,-5),new Vector3(60,.05f,-110),new Vector3(140,.05f,-125)};for(int i=0;i<a.Length;i++){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name="V15 snow patch "+i;g.transform.SetParent(p,false);g.transform.localPosition=a[i];g.transform.localScale=new Vector3(3.5f+i,.15f,1.1f+i*.2f);g.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());}}

        static string Suffix(RosvikOsmV15.Way w){string n=w.Tag("name");return string.IsNullOrEmpty(n)?"":" "+n;}
        static Material Mat(string n,Color c,float sm){Shader s=GraphicsSettings.defaultRenderPipeline!=null?Shader.Find("Universal Render Pipeline/Lit"):Shader.Find("Standard");if(!s||!s.isSupported)s=Shader.Find("Sprites/Default");Material m=new Material(s){name=n,color=c};if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",sm);return m;}
        static GameObject Cube(string n,Transform p,Vector3 pos,Vector3 sc,Material m,bool col){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=sc;g.GetComponent<Renderer>().sharedMaterial=m;if(!col)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
        static Transform FindRoot(){string[] n={"ROSVIK_MAP_LOCK_V15","ROSVIK_HERO_POLISH_V14","ROSVIK_POLISH_PASS_V13","ROSVIK_DETAIL_PASS_V12","ROSVIK_SITE_CALIBRATION_V11"};foreach(string s in n){GameObject g=GameObject.Find(s);if(g)return g.transform;}return null;}
        static Transform Child(Transform r,string n){foreach(Transform t in r.GetComponentsInChildren<Transform>(true))if(t.name==n)return t;return null;}
    }
}
#endif
