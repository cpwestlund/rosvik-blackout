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
    public static class RosvikTopDownInteriorV53 {
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Key = "ROSVIK_TOPDOWN_V53_VERSION";
        const int Version = 53;
        const string GroupName = "34 TOPDOWN V53 - SCHOOL INTERIOR SLICE";
        const string GeneratedDir = "Assets/Rosvik/GeneratedV53";
        const long SchoolWay = 163199458;

        static RosvikTopDownInteriorV53() {
            EditorApplication.delayCall -= Auto;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V53 TOPDOWN - SCHOOL INTERIOR")]
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

                GameObject extBag = FindSceneObject(scene,"V52 SEARCH BACKPACK");
                GameObject extFuse = FindSceneObject(scene,"V52 SEARCH FUSE CABINET");
                GameObject extAid = FindSceneObject(scene,"V52 SEARCH FIRST AID");
                GameObject extEntrance = FindSceneObject(scene,"V52 SCHOOL ENTRANCE");
                if (!extBag || !extFuse || !extAid || !extEntrance)
                    throw new Exception("V52 exterior gameplay spots missing. Run V52 once first.");

                RemoveTopLevel(scene,GroupName);

                Directory.CreateDirectory(GeneratedDir);
                AssetDatabase.Refresh();
                Shader shader = FindGoodShader(scene);
                if (!shader) throw new Exception("No supported shader found");

                Material voidMat = MakeMat("interior_void",new Color(.055f,.060f,.055f),shader);
                Material corridorMat = MakeMat("corridor_floor",new Color(.31f,.30f,.25f),shader);
                Material classroomMat = MakeMat("classroom_floor",new Color(.37f,.34f,.25f),shader);
                Material staffMat = MakeMat("staff_floor",new Color(.28f,.32f,.29f),shader);
                Material wallMat = MakeMat("walls",new Color(.72f,.69f,.58f),shader);
                Material trimMat = MakeMat("wall_trim",new Color(.20f,.22f,.19f),shader);
                Material doorMat = MakeMat("door",new Color(.31f,.17f,.085f),shader);
                Material deskMat = MakeMat("desk",new Color(.33f,.20f,.10f),shader);
                Material lockerMat = MakeMat("locker",new Color(.18f,.28f,.31f),shader);
                Material searchMat = MakeMat("search",new Color(.82f,.53f,.15f),shader);
                Material keyMat = MakeMat("key",new Color(.74f,.68f,.24f),shader);
                Material redMat = MakeMat("aid",new Color(.58f,.16f,.13f),shader);

                Vector3 center = RosvikOsmV15.Centroid(school); center.y=0f;
                Vector3 entrance = ResolveEntrance(scene,center);
                Vector3 f = Flat(entrance-center).normalized;
                if (f.sqrMagnitude < .1f) f=Vector3.back;

                // The interior lives far away from the exterior world in the same scene.
                // Camera follows the player, so this gives us a clean, controlled gameplay space
                // without fighting the old roof/building meshes.
                Vector3 o = center + new Vector3(800f,0f,800f);
                GameObject root = new GameObject(GroupName);
                root.transform.position=Vector3.zero;

                // Large dark base fills the entire camera view. Keep collider so the player is grounded.
                Box("V53 interior ground",root.transform,o+new Vector3(1.8f,-.12f,3.2f),new Vector3(34f,.24f,22f),Quaternion.identity,voidMat,true);

                // Playable floor zones.
                Flat("V53 corridor floor",root.transform,o,new Vector3(21f,.055f,3.2f),corridorMat);
                Flat("V53 classroom floor",root.transform,o+new Vector3(-5.5f,.004f,5.0f),new Vector3(9f,.058f,6.8f),classroomMat);
                Flat("V53 staff floor",root.transform,o+new Vector3(2.95f,.004f,5.0f),new Vector3(6.5f,.058f,6.8f),staffMat);
                Flat("V53 sporthall threshold",root.transform,o+new Vector3(12.5f,.004f,0f),new Vector3(4f,.058f,3.2f),corridorMat);

                // Outer shell and corridor walls. All wall pieces keep colliders.
                Wall(root.transform,o+new Vector3(0,0,8.4f),new Vector3(21f,1.45f,.22f),wallMat);
                Wall(root.transform,o+new Vector3(0,0,-1.6f),new Vector3(21f,1.45f,.22f),wallMat);

                Wall(root.transform,o+new Vector3(-10.5f,0,-1.18f),new Vector3(.22f,1.45f,.84f),wallMat);
                Wall(root.transform,o+new Vector3(-10.5f,0,4.58f),new Vector3(.22f,1.45f,7.64f),wallMat);
                Wall(root.transform,o+new Vector3(10.5f,0,-1.18f),new Vector3(.22f,1.45f,.84f),wallMat);
                Wall(root.transform,o+new Vector3(10.5f,0,4.58f),new Vector3(.22f,1.45f,7.64f),wallMat);

                // Corridor north wall with two deliberate door gaps.
                Wall(root.transform,o+new Vector3(-8.35f,0,1.6f),new Vector3(4.3f,1.45f,.22f),wallMat);
                Wall(root.transform,o+new Vector3(-1.25f,0,1.6f),new Vector3(7.1f,1.45f,.22f),wallMat);
                Wall(root.transform,o+new Vector3(7.1f,0,1.6f),new Vector3(6.8f,1.45f,.22f),wallMat);
                Wall(root.transform,o+new Vector3(-.5f,0,5.0f),new Vector3(.22f,1.45f,6.8f),wallMat);
                Wall(root.transform,o+new Vector3(6.35f,0,5.0f),new Vector3(.22f,1.45f,6.8f),wallMat);

                // Short corridor toward future sports hall, ending in a hard blocker for V53.
                Wall(root.transform,o+new Vector3(12.5f,0,1.6f),new Vector3(4f,1.45f,.22f),wallMat);
                Wall(root.transform,o+new Vector3(12.5f,0,-1.6f),new Vector3(4f,1.45f,.22f),wallMat);
                Wall(root.transform,o+new Vector3(14.5f,0,0f),new Vector3(.22f,1.45f,3.2f),trimMat);

                // Actual interactive door gates.
                GameObject exitGate = Wall(root.transform,o+new Vector3(-10.5f,0,0f),new Vector3(.24f,1.35f,1.5f),doorMat);
                GameObject classGate = Wall(root.transform,o+new Vector3(-5.5f,0,1.6f),new Vector3(1.4f,1.35f,.24f),doorMat);
                GameObject staffGate = Wall(root.transform,o+new Vector3(3.0f,0,1.6f),new Vector3(1.4f,1.35f,.24f),doorMat);
                GameObject sportGate = Wall(root.transform,o+new Vector3(10.5f,0,0f),new Vector3(.24f,1.35f,1.5f),doorMat);

                // Furniture is intentionally sparse and hand-placed to avoid the overlap problems.
                Desk(root.transform,o+new Vector3(-7.4f,0,4.0f),deskMat);
                Desk(root.transform,o+new Vector3(-4.9f,0,4.0f),deskMat);
                Desk(root.transform,o+new Vector3(-7.4f,0,6.0f),deskMat);
                Desk(root.transform,o+new Vector3(-4.9f,0,6.0f),deskMat);
                Desk(root.transform,o+new Vector3(2.0f,0,4.3f),deskMat);
                Box("V53 staff shelf",root.transform,o+new Vector3(4.9f,.60f,7.25f),new Vector3(1.8f,1.2f,.55f),Quaternion.identity,lockerMat,true);
                Box("V53 classroom lockers",root.transform,o+new Vector3(-8.9f,.70f,7.35f),new Vector3(1.6f,1.4f,.55f),Quaternion.identity,lockerMat,true);

                // Interactions are separate from furniture and placed on clear floor positions.
                GameObject classDoorSpot = Marker(root.transform,"V53 DOOR CLASSROOM",o+new Vector3(-5.5f,.04f,.75f),searchMat);
                GameObject staffDoorSpot = Marker(root.transform,"V53 DOOR STAFF",o+new Vector3(3.0f,.04f,.75f),keyMat);
                GameObject sportsDoorSpot = Marker(root.transform,"V53 DOOR SPORTSHALL",o+new Vector3(9.65f,.04f,0f),keyMat);
                GameObject exitSpot = Marker(root.transform,"V53 EXIT SCHOOL",o+new Vector3(-9.65f,.04f,0f),searchMat);

                GameObject teacherKeys = SearchObject(root.transform,"V53 SEARCH TEACHER DESK",o+new Vector3(-3.0f,0,6.4f),new Vector3(.85f,.42f,.58f),deskMat,searchMat);
                GameObject batteries = SearchObject(root.transform,"V53 SEARCH CLASS LOCKER",o+new Vector3(-8.4f,0,6.8f),new Vector3(.65f,.75f,.42f),lockerMat,searchMat);
                GameObject masterKey = SearchObject(root.transform,"V53 SEARCH STAFF CABINET",o+new Vector3(4.35f,0,5.8f),new Vector3(.70f,.85f,.44f),lockerMat,keyMat);

                // Reuse the proven V52 exterior gameplay objects, but replace the runtime state machine.
                TopDownGameplayV51 v51=player.GetComponent<TopDownGameplayV51>(); if(v51)v51.enabled=false;
                TopDownGameplayV52 v52=player.GetComponent<TopDownGameplayV52>(); if(v52)v52.enabled=false;
                TopDownGameplayV53 v53=player.GetComponent<TopDownGameplayV53>();
                if(!v53)v53=player.gameObject.AddComponent<TopDownGameplayV53>();
                v53.enabled=true;
                v53.spots.Clear();

                Vector3 interiorSpawn=o+new Vector3(-8.7f,.16f,0f);
                Vector3 exteriorSpawn=entrance+f*3.0f; exteriorSpawn.y=.16f;

                v53.spots.Add(new TopDownGameplayV53.Spot{spot=extBag,displayName="ryggsäcken",itemName="Ficklampa",kind=TopDownGameplayV53.SpotKind.Loot});
                v53.spots.Add(new TopDownGameplayV53.Spot{spot=extFuse,displayName="elskåpet",itemName="Säkring",kind=TopDownGameplayV53.SpotKind.Loot});
                v53.spots.Add(new TopDownGameplayV53.Spot{spot=extAid,displayName="första hjälpen-lådan",itemName="Förband",kind=TopDownGameplayV53.SpotKind.Loot});
                v53.spots.Add(new TopDownGameplayV53.Spot{spot=extEntrance,displayName="skolentrén",kind=TopDownGameplayV53.SpotKind.SchoolEntrance,teleportTarget=interiorSpawn});

                v53.spots.Add(new TopDownGameplayV53.Spot{spot=exitSpot,displayName="ytterdörren",kind=TopDownGameplayV53.SpotKind.InteriorExit,gate=exitGate,teleportTarget=exteriorSpawn});
                v53.spots.Add(new TopDownGameplayV53.Spot{spot=classDoorSpot,displayName="klassrummet",kind=TopDownGameplayV53.SpotKind.OpenDoor,gate=classGate});
                v53.spots.Add(new TopDownGameplayV53.Spot{spot=staffDoorSpot,displayName="personalrummet",kind=TopDownGameplayV53.SpotKind.LockedDoor,requiredItem="Nyckelknippa",gate=staffGate});
                v53.spots.Add(new TopDownGameplayV53.Spot{spot=sportsDoorSpot,displayName="dörren mot sporthallen",kind=TopDownGameplayV53.SpotKind.SportsHallDoor,requiredItem="Huvudnyckel",gate=sportGate});
                v53.spots.Add(new TopDownGameplayV53.Spot{spot=teacherKeys,displayName="lärarbordet",itemName="Nyckelknippa",kind=TopDownGameplayV53.SpotKind.Loot});
                v53.spots.Add(new TopDownGameplayV53.Spot{spot=batteries,displayName="klassrumsskåpet",itemName="Batterier",kind=TopDownGameplayV53.SpotKind.Loot});
                v53.spots.Add(new TopDownGameplayV53.Spot{spot=masterKey,displayName="nyckelskåpet",itemName="Huvudnyckel",kind=TopDownGameplayV53.SpotKind.Loot});
                EditorUtility.SetDirty(v53);

                EditorPrefs.SetInt(Key,Version);
                Selection.activeGameObject=player.gameObject;
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene,ScenePath);
                AssetDatabase.SaveAssets();
                SceneView.RepaintAll();
                Debug.Log("ROSVIK V53 SUCCESS: first playable school interior built. Exterior gear -> school -> classroom keys -> staff room -> master key -> sporthall door.");
            }
            catch(Exception ex) {
                Debug.LogError("ROSVIK V53 FAILED: "+ex);
            }
        }

        static void Desk(Transform p,Vector3 basePos,Material mat) {
            Box("desk",p,basePos+new Vector3(0,.38f,0),new Vector3(1.55f,.10f,.72f),Quaternion.identity,mat,true);
            Box("desk leg",p,basePos+new Vector3(-.58f,.19f,-.22f),new Vector3(.08f,.38f,.08f),Quaternion.identity,mat,true);
            Box("desk leg",p,basePos+new Vector3(.58f,.19f,-.22f),new Vector3(.08f,.38f,.08f),Quaternion.identity,mat,true);
        }

        static GameObject SearchObject(Transform p,string name,Vector3 pos,Vector3 size,Material bodyMat,Material markerMat) {
            GameObject holder=new GameObject(name);holder.transform.SetParent(p,true);holder.transform.position=pos;
            GameObject body=Box("search object",holder.transform,new Vector3(pos.x,size.y*.5f+.04f,pos.z),size,Quaternion.identity,bodyMat,false);
            GameObject ring=GameObject.CreatePrimitive(PrimitiveType.Cylinder);ring.name="search marker";ring.transform.SetParent(holder.transform,true);ring.transform.position=new Vector3(pos.x,.055f,pos.z);ring.transform.localScale=new Vector3(1.0f,.018f,1.0f);ring.GetComponent<Renderer>().sharedMaterial=markerMat;Collider c=ring.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);
            return holder;
        }

        static GameObject Marker(Transform p,string name,Vector3 pos,Material mat) {
            GameObject holder=new GameObject(name);holder.transform.SetParent(p,true);holder.transform.position=pos;
            GameObject ring=GameObject.CreatePrimitive(PrimitiveType.Cylinder);ring.name="interaction marker";ring.transform.SetParent(holder.transform,false);ring.transform.localPosition=Vector3.zero;ring.transform.localScale=new Vector3(.78f,.018f,.78f);ring.GetComponent<Renderer>().sharedMaterial=mat;Collider c=ring.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);
            return holder;
        }

        static GameObject Wall(Transform p,Vector3 basePos,Vector3 size,Material mat) {
            return Box("wall",p,basePos+Vector3.up*(size.y*.5f),size,Quaternion.identity,mat,true);
        }

        static void Flat(string name,Transform p,Vector3 pos,Vector3 scale,Material mat) {
            Box(name,p,pos,new Vector3(scale.x,scale.y,scale.z),Quaternion.identity,mat,false);
        }

        static GameObject Box(string name,Transform p,Vector3 pos,Vector3 scale,Quaternion rot,Material mat,bool collider) {
            GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(p,true);g.transform.position=pos;g.transform.rotation=rot;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;if(!collider){Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);}return g;
        }

        static Vector3 ResolveEntrance(UScene scene,Vector3 schoolCenter) {
            Transform canopy=Resources.FindObjectsOfTypeAll<Transform>().FirstOrDefault(t=>t&&t.gameObject.scene==scene&&t.name=="large timber canopy");
            if(canopy){Vector3 p=canopy.position;p.y=.03f;return p;}
            return schoolCenter+new Vector3(-12f,.03f,0f);
        }

        static Shader FindGoodShader(UScene scene) {
            foreach(GameObject root in scene.GetRootGameObjects()) foreach(Renderer r in root.GetComponentsInChildren<Renderer>(true)) foreach(Material m in r.sharedMaterials) {
                if(!m||!m.shader||!m.shader.isSupported)continue;string s=m.shader.name??"";if(s.IndexOf("InternalError",StringComparison.OrdinalIgnoreCase)>=0||s.StartsWith("Hidden/"))continue;return m.shader;
            }
            Shader standard=Shader.Find("Standard");if(standard&&standard.isSupported)return standard;
            Shader urp=Shader.Find("Universal Render Pipeline/Lit");if(urp&&urp.isSupported)return urp;
            return null;
        }

        static Material MakeMat(string name,Color color,Shader shader) {
            string path=GeneratedDir+"/"+name+".mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;m=new Material(shader){name="V53 "+name};if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color);if(m.HasProperty("_Color"))m.SetColor("_Color",color);AssetDatabase.CreateAsset(m,path);return m;
        }

        static GameObject FindSceneObject(UScene scene,string name){Transform t=Resources.FindObjectsOfTypeAll<Transform>().FirstOrDefault(x=>x&&x.gameObject.scene==scene&&x.name==name);return t?t.gameObject:null;}
        static Transform FindSceneTransform(UScene scene,string name){return Resources.FindObjectsOfTypeAll<Transform>().FirstOrDefault(x=>x&&x.gameObject.scene==scene&&x.name==name);}
        static void RemoveTopLevel(UScene scene,string name){GameObject g=scene.GetRootGameObjects().FirstOrDefault(x=>x.name==name);if(g)UnityEngine.Object.DestroyImmediate(g);}
        static Vector3 Flat(Vector3 v){v.y=0f;return v;}
    }
}
#endif
