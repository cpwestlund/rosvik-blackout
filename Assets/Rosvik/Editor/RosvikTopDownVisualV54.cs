#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Rosvik.Blackout;
using UScene = UnityEngine.SceneManagement.Scene;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikTopDownVisualV54 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_TOPDOWN_V54_VERSION";
        const int Version = 54;
        const string GroupName = "35 TOPDOWN V54 - CLEAN VISUAL CAMPUS";
        const string GeneratedDir = "Assets/Rosvik/GeneratedV54";
        const long SchoolWay = 163199458;

        static RosvikTopDownVisualV54() {
            EditorApplication.delayCall -= Auto;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V54 TOPDOWN - CLEAN VISUAL CAMPUS")]
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
                GameObject bag = FindSceneObject(scene,"V52 SEARCH BACKPACK");
                GameObject fuse = FindSceneObject(scene,"V52 SEARCH FUSE CABINET");
                GameObject aid = FindSceneObject(scene,"V52 SEARCH FIRST AID");
                GameObject entranceSpot = FindSceneObject(scene,"V52 SCHOOL ENTRANCE");
                if (!player || !bag || !fuse || !aid || !entranceSpot)
                    throw new Exception("V52/V53 gameplay objects missing. Run V53 once first.");

                RemoveTopLevel(scene,GroupName);

                Vector3 schoolCenter = RosvikOsmV15.Centroid(school); schoolCenter.y=0f;
                Vector3 entrance = ResolveEntrance(scene,schoolCenter);
                Vector3 f = Flat(entrance-schoolCenter).normalized;
                if (f.sqrMagnitude < .1f) f=Vector3.back;
                Vector3 r = new Vector3(f.z,0f,-f.x).normalized;

                Directory.CreateDirectory(GeneratedDir);
                AssetDatabase.Refresh();
                Shader shader = FindGoodShader(scene);
                if (!shader) throw new Exception("No supported shader found");

                Material grass = Mat("grass",new Color(.20f,.25f,.15f),shader);
                Material asphalt = Mat("asphalt",new Color(.095f,.105f,.105f),shader);
                Material path = Mat("path",new Color(.42f,.37f,.29f),shader);
                Material curb = Mat("curb",new Color(.72f,.70f,.62f),shader);
                Material snow = Mat("snow",new Color(.88f,.91f,.88f),shader);
                Material wood = Mat("wood",new Color(.38f,.21f,.10f),shader);
                Material metal = Mat("metal",new Color(.09f,.11f,.12f),shader);
                Material darkGreen = Mat("dark_green",new Color(.055f,.15f,.075f),shader);
                Material pine = Mat("pine",new Color(.07f,.22f,.10f),shader);
                Material amber = Mat("amber",new Color(.92f,.56f,.12f),shader);
                Material blue = Mat("blue",new Color(.13f,.43f,.64f),shader);
                Material red = Mat("red",new Color(.66f,.18f,.15f),shader);
                Material door = Mat("door",new Color(.66f,.43f,.14f),shader);
                Material glass = Mat("glass",new Color(.23f,.38f,.40f),shader);
                Material paint = Mat("paint",new Color(.90f,.86f,.70f),shader);

                GameObject root = new GameObject(GroupName);
                Vector3 zone = entrance + f*8.0f; zone.y=.018f;

                ClearNearbyClutter(scene,zone,17f,bag,fuse,aid,entranceSpot,player.gameObject);
                HideOldV52Geometry(scene,bag,fuse,aid,entranceSpot);

                // Re-anchor all gameplay objects into deliberate, reachable positions.
                entranceSpot.transform.position = entrance + f*2.0f;
                bag.transform.position = zone-r*4.3f+f*.8f;
                fuse.transform.position = zone+r*4.2f+f*1.0f;
                aid.transform.position = zone+r*2.0f-f*3.2f;
                StyleSpot(bag,amber);
                StyleSpot(fuse,blue);
                StyleSpot(aid,red);
                StyleSpot(entranceSpot,door);

                // One clean readable top-down composition. Nothing random, nothing stacked.
                FlatBox("campus grass",root.transform,zone,r,27f,22f,.035f,grass);
                FlatBox("entrance plaza",root.transform,entrance+f*4.0f,r,12.5f,7.0f,.052f,path);
                Border(root.transform,entrance+f*4.0f,r,f,12.5f,7.0f,curb);

                Vector3 parking = zone+f*4.3f;
                FlatBox("small parking",root.transform,parking,r,15f,7.5f,.050f,asphalt);
                Border(root.transform,parking,r,f,15f,7.5f,curb);
                for(int i=-2;i<=2;i++) FlatBox("parking line",root.transform,parking+r*(i*2.55f)+f*.6f,f,.10f,5.4f,.015f,paint);

                FlatBox("walkway",root.transform,entrance+f*9.2f,f,3.2f,10.5f,.055f,path);

                // Proper readable entrance: canopy, two doors, light pools and a ground label.
                FlatBox("entrance canopy",root.transform,entrance+f*.9f,r,5.8f,1.7f,.18f,wood,.50f);
                Box("left entrance door",root.transform,entrance-r*1.15f+f*.25f+Vector3.up*.55f,new Vector3(1.75f,1.1f,.20f),Rot(r),glass,false);
                Box("right entrance door",root.transform,entrance+r*1.15f+f*.25f+Vector3.up*.55f,new Vector3(1.75f,1.1f,.20f),Rot(r),glass,false);
                Ring(root.transform,entranceSpot.transform.position,.95f,amber);
                GroundLabel(root.transform,"INGÅNG",entrance+f*3.0f+Vector3.up*.07f,2.2f,new Color(.95f,.73f,.24f));

                // Furniture with strict spacing.
                Bench(root.transform,entrance-r*4.5f+f*4.3f,r,wood,metal);
                Bench(root.transform,entrance+r*4.5f+f*4.3f,-r,wood,metal);
                Planter(root.transform,entrance-r*5.0f+f*1.8f,r,wood,darkGreen);
                Planter(root.transform,entrance+r*5.0f+f*1.8f,r,wood,darkGreen);

                // Edge vegetation only: open center for gameplay.
                Tree(root.transform,zone-r*11.0f-f*7.5f,1.8f,metal,pine);
                Tree(root.transform,zone-r*10.5f+f*7.4f,1.55f,metal,darkGreen);
                Tree(root.transform,zone+r*10.6f-f*7.4f,1.7f,metal,pine);
                Tree(root.transform,zone+r*11.0f+f*7.2f,1.6f,metal,darkGreen);
                Tree(root.transform,zone-r*7.8f+f*9.0f,1.45f,metal,pine);
                Tree(root.transform,zone+r*7.8f+f*9.0f,1.45f,metal,pine);

                // Small snow remnants give Norrbotten identity without covering gameplay.
                SnowPatch(root.transform,zone-r*9.2f-f*4.8f,r,3.0f,1.0f,snow);
                SnowPatch(root.transform,zone+r*8.8f+f*5.8f,r,2.5f,.8f,snow);
                SnowPatch(root.transform,parking-r*6.8f-f*3.4f,r,2.4f,.65f,snow);

                TopDownGameplayV53 gameplay = player.GetComponent<TopDownGameplayV53>();
                if (gameplay) {
                    gameplay.interactDistance=2.6f;
                    EditorUtility.SetDirty(gameplay);
                }

                TopDownCameraRigV51 rig = Camera.main ? Camera.main.GetComponent<TopDownCameraRigV51>() : null;
                if (rig) {
                    rig.pitch=88.7f;
                    rig.orthographicSize=7.6f;
                    rig.minSize=6.2f;
                    rig.maxSize=11.5f;
                    rig.followSharpness=15f;
                    EditorUtility.SetDirty(rig);
                }

                RenderSettings.fog=false;
                RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight=new Color(.48f,.50f,.44f);

                EditorPrefs.SetInt(Key,Version);
                Selection.activeGameObject=player.gameObject;
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene,ScenePath);
                AssetDatabase.SaveAssets();
                SceneView.RepaintAll();
                Debug.Log("ROSVIK V54 SUCCESS: entrance re-anchored and readable; clean top-down campus presentation built.");
            }
            catch(Exception ex) {
                Debug.LogError("ROSVIK V54 FAILED: "+ex);
            }
        }

        static void HideOldV52Geometry(UScene scene, params GameObject[] keep) {
            GameObject root=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="33 TOPDOWN GAMEPLAY V52 - FIRST LOOP");
            if(!root)return;
            foreach(Renderer rr in root.GetComponentsInChildren<Renderer>(true)) {
                bool preserve=false;
                foreach(GameObject k in keep) if(k && rr.transform.IsChildOf(k.transform)){preserve=true;break;}
                if(!preserve) rr.enabled=false;
            }
        }

        static void ClearNearbyClutter(UScene scene,Vector3 center,float radius,params GameObject[] keep) {
            string[] tokens={"tree","pine","spruce","birch","bush","plant","shrub","rock","stone","bench","bike","rack","lamp","puddle","debris","trash","snowbank","picnic","planter"};
            float r2=radius*radius;
            foreach(Transform t in Resources.FindObjectsOfTypeAll<Transform>()) {
                if(!t || t.gameObject.scene!=scene || !t.gameObject.activeInHierarchy)continue;
                bool preserved=false;foreach(GameObject k in keep)if(k && (t.gameObject==k || t.IsChildOf(k.transform))){preserved=true;break;}if(preserved)continue;
                if(t.name=="large timber canopy")continue;
                Vector3 d=Flat(t.position-center);if(d.sqrMagnitude>r2)continue;
                string n=t.name.ToLowerInvariant();bool match=false;foreach(string token in tokens)if(n.Contains(token)){match=true;break;}if(match)t.gameObject.SetActive(false);
            }
        }

        static void StyleSpot(GameObject spot,Material m) {
            if(!spot)return;
            spot.SetActive(true);
            foreach(Renderer r in spot.GetComponentsInChildren<Renderer>(true)){r.enabled=true;r.sharedMaterial=m;}
        }

        static void Bench(Transform p,Vector3 pos,Vector3 axis,Material wood,Material metal) {
            axis=Flat(axis).normalized;
            Box("bench seat",p,pos+Vector3.up*.32f,new Vector3(2.1f,.14f,.55f),Rot(axis),wood,false);
            Box("bench back",p,pos-Perp(axis)*.22f+Vector3.up*.66f,new Vector3(2.1f,.60f,.10f),Rot(axis),wood,false);
            Box("bench leg",p,pos-axis*.72f+Vector3.up*.15f,new Vector3(.10f,.30f,.10f),Quaternion.identity,metal,false);
            Box("bench leg",p,pos+axis*.72f+Vector3.up*.15f,new Vector3(.10f,.30f,.10f),Quaternion.identity,metal,false);
        }

        static void Planter(Transform p,Vector3 pos,Vector3 axis,Material boxMat,Material plantMat) {
            Box("planter",p,pos+Vector3.up*.20f,new Vector3(2.2f,.40f,.80f),Rot(axis),boxMat,false);
            for(int i=-1;i<=1;i++) Sphere("planter shrub",p,pos+axis*(i*.55f)+Vector3.up*.55f,new Vector3(.65f,.50f,.65f),plantMat);
        }

        static void Tree(Transform p,Vector3 pos,float radius,Material trunk,Material canopy) {
            Box("tree trunk",p,pos+Vector3.up*.42f,new Vector3(.28f,.84f,.28f),Quaternion.identity,trunk,false);
            Sphere("tree canopy",p,pos+Vector3.up*1.10f,new Vector3(radius,.72f,radius),canopy);
            Sphere("tree canopy",p,pos+new Vector3(radius*.35f,1.00f,-radius*.15f),new Vector3(radius*.72f,.62f,radius*.72f),canopy);
        }

        static void SnowPatch(Transform p,Vector3 pos,Vector3 axis,float w,float d,Material m) {
            FlatBox("snow patch",p,pos+Vector3.up*.008f,axis,w,d,.018f,m);
        }

        static void Ring(Transform p,Vector3 pos,float diameter,Material m) {
            GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name="entrance interaction halo";g.transform.SetParent(p,true);g.transform.position=new Vector3(pos.x,.055f,pos.z);g.transform.localScale=new Vector3(diameter,.018f,diameter);g.GetComponent<Renderer>().sharedMaterial=m;Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);
        }

        static void GroundLabel(Transform p,string text,Vector3 pos,float size,Color color) {
            GameObject go=new GameObject("label "+text);go.transform.SetParent(p,true);go.transform.position=pos;go.transform.rotation=Quaternion.Euler(90f,0f,0f);
            TextMesh tm=go.AddComponent<TextMesh>();tm.text=text;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;tm.fontSize=64;tm.characterSize=size/10f;tm.color=color;
        }

        static void Border(Transform p,Vector3 c,Vector3 r,Vector3 f,float w,float d,Material m) {
            FlatBox("curb",p,c+r*(w*.5f),f,d,.16f,.06f,m,.08f);
            FlatBox("curb",p,c-r*(w*.5f),f,d,.16f,.06f,m,.08f);
            FlatBox("curb",p,c+f*(d*.5f),r,w,.16f,.06f,m,.08f);
            FlatBox("curb",p,c-f*(d*.5f),r,w,.16f,.06f,m,.08f);
        }

        static GameObject FlatBox(string name,Transform parent,Vector3 pos,Vector3 widthAxis,float width,float depth,float thickness,Material mat,float yOffset=0f) {
            GameObject go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,true);go.transform.position=new Vector3(pos.x,pos.y+yOffset,pos.z);
            widthAxis=Flat(widthAxis).normalized;if(widthAxis.sqrMagnitude<.01f)widthAxis=Vector3.right;Vector3 depthAxis=Perp(widthAxis);
            go.transform.rotation=Quaternion.LookRotation(depthAxis,Vector3.up);go.transform.localScale=new Vector3(width,thickness,depth);go.GetComponent<Renderer>().sharedMaterial=mat;Collider c=go.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);return go;
        }

        static GameObject Box(string name,Transform parent,Vector3 pos,Vector3 scale,Quaternion rot,Material mat,bool collider) {
            GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,true);g.transform.position=pos;g.transform.rotation=rot;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;if(!collider){Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);}return g;
        }

        static void Sphere(string name,Transform p,Vector3 pos,Vector3 scale,Material m) {
            GameObject g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=name;g.transform.SetParent(p,true);g.transform.position=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=m;Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);
        }

        static Material Mat(string name,Color c,Shader shader) {
            string path=GeneratedDir+"/"+name+".mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;m=new Material(shader){name="V54 "+name};if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);if(m.HasProperty("_Color"))m.SetColor("_Color",c);AssetDatabase.CreateAsset(m,path);return m;
        }

        static Shader FindGoodShader(UScene scene) {
            foreach(GameObject root in scene.GetRootGameObjects())foreach(Renderer r in root.GetComponentsInChildren<Renderer>(true))foreach(Material m in r.sharedMaterials){if(!m||!m.shader||!m.shader.isSupported)continue;string s=m.shader.name??"";if(s.IndexOf("InternalError",StringComparison.OrdinalIgnoreCase)>=0||s.StartsWith("Hidden/"))continue;return m.shader;}Shader standard=Shader.Find("Standard");if(standard&&standard.isSupported)return standard;Shader urp=Shader.Find("Universal Render Pipeline/Lit");if(urp&&urp.isSupported)return urp;return null;
        }

        static Vector3 ResolveEntrance(UScene scene,Vector3 schoolCenter) {
            Transform canopy=Resources.FindObjectsOfTypeAll<Transform>().FirstOrDefault(t=>t&&t.gameObject.scene==scene&&t.name=="large timber canopy");if(canopy){Vector3 p=canopy.position;p.y=.03f;return p;}return schoolCenter+new Vector3(-12f,.03f,0f);
        }

        static Quaternion Rot(Vector3 axis){axis=Flat(axis).normalized;if(axis.sqrMagnitude<.01f)axis=Vector3.right;return Quaternion.FromToRotation(Vector3.right,axis);}
        static Vector3 Perp(Vector3 v){v=Flat(v).normalized;return new Vector3(-v.z,0f,v.x);}
        static Vector3 Flat(Vector3 v){v.y=0f;return v;}
        static Transform FindSceneTransform(UScene scene,string name){return Resources.FindObjectsOfTypeAll<Transform>().FirstOrDefault(t=>t&&t.gameObject.scene==scene&&t.name==name);}
        static GameObject FindSceneObject(UScene scene,string name){Transform t=FindSceneTransform(scene,name);return t?t.gameObject:null;}
        static void RemoveTopLevel(UScene scene,string name){GameObject g=scene.GetRootGameObjects().FirstOrDefault(x=>x.name==name);if(g)UnityEngine.Object.DestroyImmediate(g);}
    }
}
#endif
