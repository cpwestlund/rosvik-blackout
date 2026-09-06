#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Rosvik.Blackout;
using UScene = UnityEngine.SceneManagement.Scene;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikTopDownGameplayV52 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_TOPDOWN_V52_VERSION";
        const int Version = 52;
        const string GroupName = "33 TOPDOWN GAMEPLAY V52 - FIRST LOOP";
        const string GeneratedDir = "Assets/Rosvik/GeneratedV52";
        const long SchoolWay = 163199458;

        static RosvikTopDownGameplayV52() {
            EditorApplication.delayCall -= Auto;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V52 TOPDOWN - FIRST GAMEPLAY LOOP")]
        public static void Force() {
            EditorPrefs.DeleteKey(Key);
            Build();
        }

        static void Auto() {
            if (EditorPrefs.GetInt(Key,0) >= Version) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) {
                EditorApplication.delayCall += Auto;
                return;
            }
            Build();
        }

        static void Build() {
            try {
                if (!File.Exists(ScenePath)) return;
                UScene scene = EditorSceneManager.GetActiveScene();
                if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

                var ways = RosvikOsmV15.LoadWays();
                var school = ways == null ? null : ways.FirstOrDefault(w => w.Id == SchoolWay);
                if (school == null) throw new Exception("School footprint missing");

                Transform player = FindSceneTransform(scene,"PLAYER");
                if (!player) throw new Exception("PLAYER not found");

                Vector3 center = RosvikOsmV15.Centroid(school); center.y=.03f;
                Vector3 entrance = ResolveEntrance(scene,center);
                Vector3 forward = Flat(entrance-center).normalized;
                if (forward.sqrMagnitude < .1f) forward=Vector3.back;
                Vector3 right = new Vector3(forward.z,0f,-forward.x);

                SetTopLevelActive(scene,"32 TOPDOWN GAMEPLAY V51 - PROTOTYPE",false);
                RemoveTopLevel(scene,GroupName);

                Vector3 zone = entrance + forward*7.0f; zone.y=.03f;
                ClearLocalClutter(scene,zone,11.5f);
                DisableLegacyGui(scene);

                Directory.CreateDirectory(GeneratedDir);
                AssetDatabase.Refresh();
                Shader shader = FindGoodShader(scene);
                if (!shader) throw new Exception("No supported scene shader found");

                Material ground = MakeMat("ground",new Color(.24f,.27f,.20f),shader);
                Material edge = MakeMat("edge",new Color(.13f,.145f,.12f),shader);
                Material amber = MakeMat("amber",new Color(.80f,.49f,.12f),shader);
                Material blue = MakeMat("blue",new Color(.17f,.39f,.52f),shader);
                Material red = MakeMat("red",new Color(.58f,.17f,.14f),shader);
                Material doorMat = MakeMat("door",new Color(.67f,.60f,.27f),shader);
                Material playerMat = MakeMat("player_marker",new Color(.92f,.57f,.18f),shader);

                GameObject root = new GameObject(GroupName);
                FlatBox("V52 CLEAN PLAY SPACE",root.transform,zone,right,17f,13f,.045f,ground);
                Border(root.transform,zone,right,forward,17f,13f,edge);

                GameObject bag = SearchSpot(root.transform,"V52 SEARCH BACKPACK",zone-right*4.3f+forward*1.5f,new Vector3(.80f,.26f,.55f),amber,1.1f);
                GameObject fuse = SearchSpot(root.transform,"V52 SEARCH FUSE CABINET",zone+right*3.8f+forward*1.8f,new Vector3(.72f,.92f,.45f),blue,1.15f);
                GameObject aid = SearchSpot(root.transform,"V52 SEARCH FIRST AID",zone+right*1.8f-forward*3.4f,new Vector3(.68f,.72f,.42f),red,1.05f);
                GameObject door = DoorSpot(root.transform,"V52 SCHOOL ENTRANCE",entrance+forward*.8f,right,doorMat);

                player.position = zone-forward*2.2f;
                player.position = new Vector3(player.position.x,.16f,player.position.z);
                AddPlayerMarker(player,playerMat);

                TopDownGameplayV51 oldGameplay = player.GetComponent<TopDownGameplayV51>();
                if (oldGameplay) oldGameplay.enabled=false;
                TopDownGameplayV52 gameplay = player.GetComponent<TopDownGameplayV52>();
                if (!gameplay) gameplay = player.gameObject.AddComponent<TopDownGameplayV52>();
                gameplay.enabled=true;
                gameplay.spots.Clear();
                gameplay.spots.Add(new TopDownGameplayV52.Spot { spot=bag, displayName="ryggsäcken", itemName="Ficklampa", requiredForDoor=true });
                gameplay.spots.Add(new TopDownGameplayV52.Spot { spot=fuse, displayName="elskåpet", itemName="Säkring", requiredForDoor=true });
                gameplay.spots.Add(new TopDownGameplayV52.Spot { spot=aid, displayName="första hjälpen-lådan", itemName="Förband", requiredForDoor=false });
                gameplay.spots.Add(new TopDownGameplayV52.Spot { spot=door, displayName="skolentrén", itemName="", isDoor=true });
                EditorUtility.SetDirty(gameplay);

                SetupCamera(scene,player);
                SetupLighting(scene);

                EditorPrefs.SetInt(Key,Version);
                Selection.activeGameObject=player.gameObject;
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene,ScenePath);
                AssetDatabase.SaveAssets();
                SceneView.RepaintAll();
                Debug.Log("ROSVIK V52 SUCCESS: tighter true top-down camera, old GUI disabled, clean play space and first objective loop built.");
            }
            catch(Exception ex) {
                Debug.LogError("ROSVIK V52 FAILED: "+ex);
            }
        }

        static void SetupCamera(UScene scene, Transform player) {
            Camera cam = Resources.FindObjectsOfTypeAll<Camera>().FirstOrDefault(c=>c && c.gameObject.scene==scene && c.CompareTag("MainCamera"));
            if (!cam) cam=Resources.FindObjectsOfTypeAll<Camera>().FirstOrDefault(c=>c && c.gameObject.scene==scene);
            if (!cam) return;

            foreach(MonoBehaviour mb in cam.GetComponents<MonoBehaviour>()) {
                if (!mb) continue;
                string n=mb.GetType().Name;
                if (n=="IsometricCameraRig") mb.enabled=false;
            }

            TopDownCameraRigV51 rig=cam.GetComponent<TopDownCameraRigV51>();
            if (!rig) rig=cam.gameObject.AddComponent<TopDownCameraRigV51>();
            rig.enabled=true;
            rig.target=player;
            rig.pitch=88.3f;
            rig.yaw=0f;
            rig.distance=32f;
            rig.orthographicSize=8.4f;
            rig.minSize=6.4f;
            rig.maxSize=13.5f;
            rig.zoomStep=.65f;
            rig.focusOffset=Vector3.zero;
            rig.followSharpness=14f;
            cam.orthographic=true;
            cam.orthographicSize=8.4f;
            cam.backgroundColor=new Color(.25f,.29f,.27f);
            EditorUtility.SetDirty(cam);
            EditorUtility.SetDirty(rig);
        }

        static void DisableLegacyGui(UScene scene) {
            foreach(MonoBehaviour mb in Resources.FindObjectsOfTypeAll<MonoBehaviour>()) {
                if (!mb || mb.gameObject.scene!=scene) continue;
                Type t=mb.GetType();
                if (t==typeof(TopDownGameplayV52)) continue;
                MethodInfo gui=t.GetMethod("OnGUI",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly);
                if (gui!=null) mb.enabled=false;
            }
        }

        static void ClearLocalClutter(UScene scene, Vector3 center, float radius) {
            string[] tokens={"tree","pine","spruce","birch","bush","plant","shrub","rock","stone","bench","bike","rack","lamp","puddle","debris","trash","snowbank"};
            float r2=radius*radius;
            foreach(Transform t in Resources.FindObjectsOfTypeAll<Transform>()) {
                if (!t || t.gameObject.scene!=scene || !t.gameObject.activeInHierarchy) continue;
                if (t.name=="PLAYER" || t.name=="large timber canopy") continue;
                Vector3 d=Flat(t.position-center);
                if (d.sqrMagnitude>r2) continue;
                string n=t.name.ToLowerInvariant();
                bool match=false;
                foreach(string token in tokens) if(n.Contains(token)){match=true;break;}
                if(!match) continue;
                t.gameObject.SetActive(false);
            }
        }

        static GameObject SearchSpot(Transform parent,string name,Vector3 pos,Vector3 size,Material mat,float ringSize) {
            GameObject holder=new GameObject(name);
            holder.transform.SetParent(parent,true);
            holder.transform.position=new Vector3(pos.x,.05f,pos.z);

            GameObject body=GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name="object";
            body.transform.SetParent(holder.transform,false);
            body.transform.localPosition=new Vector3(0,size.y*.5f,0);
            body.transform.localScale=size;
            body.GetComponent<Renderer>().sharedMaterial=mat;
            Collider bc=body.GetComponent<Collider>(); if(bc) UnityEngine.Object.DestroyImmediate(bc);

            GameObject ring=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name="interaction marker";
            ring.transform.SetParent(holder.transform,false);
            ring.transform.localPosition=new Vector3(0,.01f,0);
            ring.transform.localScale=new Vector3(ringSize,.018f,ringSize);
            ring.GetComponent<Renderer>().sharedMaterial=mat;
            Collider rc=ring.GetComponent<Collider>(); if(rc) UnityEngine.Object.DestroyImmediate(rc);
            return holder;
        }

        static GameObject DoorSpot(Transform parent,string name,Vector3 pos,Vector3 right,Material mat) {
            GameObject holder=new GameObject(name);
            holder.transform.SetParent(parent,true);
            holder.transform.position=new Vector3(pos.x,.055f,pos.z);
            FlatBox("door interaction strip",holder.transform,holder.transform.position,right,3.4f,.75f,.035f,mat);
            return holder;
        }

        static void AddPlayerMarker(Transform player, Material mat) {
            Transform old=player.Find("PLAYER MARKER V52");
            if(old) UnityEngine.Object.DestroyImmediate(old.gameObject);
            GameObject marker=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name="PLAYER MARKER V52";
            marker.transform.SetParent(player,false);
            marker.transform.localPosition=new Vector3(0,-.12f,0);
            marker.transform.localScale=new Vector3(.62f,.018f,.62f);
            marker.GetComponent<Renderer>().sharedMaterial=mat;
            Collider c=marker.GetComponent<Collider>(); if(c) UnityEngine.Object.DestroyImmediate(c);
        }

        static void Border(Transform p,Vector3 c,Vector3 r,Vector3 f,float width,float depth,Material m) {
            FlatBox("border",p,c+r*(width*.5f),f,depth,.14f,.06f,m);
            FlatBox("border",p,c-r*(width*.5f),f,depth,.14f,.06f,m);
            FlatBox("border",p,c+f*(depth*.5f),r,width,.14f,.06f,m);
            FlatBox("border",p,c-f*(depth*.5f),r,width,.14f,.06f,m);
        }

        static void SetupLighting(UScene scene) {
            RenderSettings.fog=false;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight=new Color(.54f,.55f,.48f);
            Light sun=Resources.FindObjectsOfTypeAll<Light>().FirstOrDefault(l=>l && l.gameObject.scene==scene && l.type==LightType.Directional);
            if(sun){sun.intensity=.72f;sun.color=new Color(.96f,.92f,.82f);sun.shadows=LightShadows.Soft;sun.shadowStrength=.32f;}
            QualitySettings.shadowDistance=45f;
        }

        static Vector3 ResolveEntrance(UScene scene,Vector3 schoolCenter) {
            Transform canopy=Resources.FindObjectsOfTypeAll<Transform>().FirstOrDefault(t=>t && t.gameObject.scene==scene && t.name=="large timber canopy");
            if(canopy){Vector3 p=canopy.position;p.y=.03f;return p;}
            return schoolCenter+new Vector3(-12f,.03f,0f);
        }

        static Shader FindGoodShader(UScene scene) {
            foreach(GameObject root in scene.GetRootGameObjects()) foreach(Renderer r in root.GetComponentsInChildren<Renderer>(true)) foreach(Material m in r.sharedMaterials) {
                if(!m || !m.shader || !m.shader.isSupported) continue;
                string s=m.shader.name??"";
                if(s.IndexOf("InternalError",StringComparison.OrdinalIgnoreCase)>=0 || s.StartsWith("Hidden/")) continue;
                return m.shader;
            }
            Shader standard=Shader.Find("Standard");if(standard&&standard.isSupported)return standard;
            Shader urp=Shader.Find("Universal Render Pipeline/Lit");if(urp&&urp.isSupported)return urp;
            return null;
        }

        static Material MakeMat(string name,Color color,Shader shader) {
            string path=GeneratedDir+"/"+name+".mat";
            Material m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m)return m;
            m=new Material(shader){name="V52 "+name};
            if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color);
            if(m.HasProperty("_Color"))m.SetColor("_Color",color);
            AssetDatabase.CreateAsset(m,path);
            return m;
        }

        static GameObject FlatBox(string name,Transform parent,Vector3 pos,Vector3 widthAxis,float width,float depth,float thickness,Material mat) {
            GameObject go=GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name=name;go.transform.SetParent(parent,true);go.transform.position=pos;
            widthAxis=Flat(widthAxis).normalized;if(widthAxis.sqrMagnitude<.01f)widthAxis=Vector3.right;
            Vector3 depthAxis=new Vector3(-widthAxis.z,0,widthAxis.x);
            go.transform.rotation=Quaternion.LookRotation(depthAxis,Vector3.up);
            go.transform.localScale=new Vector3(width,thickness,depth);
            go.GetComponent<Renderer>().sharedMaterial=mat;
            Collider c=go.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);
            return go;
        }

        static Transform FindSceneTransform(UScene scene,string name){return Resources.FindObjectsOfTypeAll<Transform>().FirstOrDefault(t=>t&&t.gameObject.scene==scene&&t.name==name);}
        static void SetTopLevelActive(UScene scene,string name,bool active){GameObject g=scene.GetRootGameObjects().FirstOrDefault(x=>x.name==name);if(g)g.SetActive(active);}
        static void RemoveTopLevel(UScene scene,string name){GameObject g=scene.GetRootGameObjects().FirstOrDefault(x=>x.name==name);if(g)UnityEngine.Object.DestroyImmediate(g);}
        static Vector3 Flat(Vector3 v){v.y=0f;return v;}
    }
}
#endif
