#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Rosvik.Blackout;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikPremiumSchoolV97 {
        const int Version=97;
        const string Key="ROSVIK_PREMIUM_SCHOOL_V97";
        const string ScenePath="Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string RootName="V97 PREMIUM SCHOOL + INVENTORY";
        const string Generated="Assets/Rosvik/GeneratedV97";
        static int retries;
        static Material floorWarm,floorClass,floorStaff,wall,wood,metal,rugGreen,rugRust,whiteboard;
        static readonly Dictionary<string,GameObject> modelCache=new Dictionary<string,GameObject>(StringComparer.OrdinalIgnoreCase);

        static RosvikPremiumSchoolV97(){if(EditorPrefs.GetInt(Key,0)>=Version)return;EditorApplication.delayCall+=Auto;}
        [MenuItem("Rosvik/V97 PREMIUM SCHOOL + INVENTORY")]
        public static void Force(){EditorPrefs.DeleteKey(Key);retries=0;EditorApplication.delayCall+=Auto;}

        static void Auto(){
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=Auto;return;}
            if(!File.Exists(ScenePath))return;
            try{if(!Apply()&&retries++<20)EditorApplication.delayCall+=Auto;}catch(Exception ex){Debug.LogError("V97 PREMIUM SCHOOL FAILED: "+ex);}
        }

        static bool Apply(){
            if(EditorSceneManager.GetActiveScene().path!=ScenePath)EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            GameObject campus=GameObject.Find("ROSVIK CAMPUS · V92 ASTRA BENCHMARK");
            GameObject world=GameObject.Find("V88 CLEAN OSM WORLD");
            CoziPlayerV57 player=UnityEngine.Object.FindFirstObjectByType<CoziPlayerV57>();
            if(!campus||!world||!player)return false;

            CleanupFalseInteractions(campus.transform);
            CleanupUnreadableCampusProps(campus.transform);
            CleanupUnreadableSceneProps(player.transform.position);

            Transform interiorParent=FindContaining(world.transform,"CLEAN INTERIORS");
            if(!interiorParent)return false;
            Transform oldSchool=FindDeep(interiorParent,"SCHOOL INTERIOR V92");if(oldSchool)oldSchool.gameObject.SetActive(false);
            GameObject old=GameObject.Find(RootName);if(old)UnityEngine.Object.DestroyImmediate(old);

            Directory.CreateDirectory(Generated);
            BuildMaterials(world);

            GameObject root=new GameObject(RootName);root.transform.SetParent(interiorParent,true);
            Transform school=Group(root.transform,"01 AUTHORED SCHOOL INTERIOR");
            BuildSchoolInterior(school);
            Transform logic=Group(root.transform,"02 GAMEPLAY CLEANUP");
            BuildReadableEntranceDetails(logic);

            PremiumInventoryV97 premium=player.GetComponent<PremiumInventoryV97>();if(!premium)premium=player.gameObject.AddComponent<PremiumInventoryV97>();premium.enabled=true;premium.backpackCapacityKg=34f;
            SurvivalInventoryV76 legacy=player.GetComponent<SurvivalInventoryV76>();if(legacy)legacy.enabled=false;
            player.suppressLegacyGui=true;
            SurvivalSystemsV69 survival=player.GetComponent<SurvivalSystemsV69>();if(survival)survival.suppressLegacyGui=true;
            player.SetObjective("Utforska Rosviks skola. Sök riktiga skåp och rum — kläder bärs på kroppen, inte i ryggsäcken.");

            CleanupMissingScripts();
            EditorUtility.SetDirty(player);EditorUtility.SetDirty(premium);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();EditorPrefs.SetInt(Key,Version);SceneView.RepaintAll();
            Debug.Log("V97 COMPLETE — false searchable architecture removed, unreadable rack clutter cleaned, authored school rooms/loot installed, and premium backpack/equipment/loot UI activated without replacing survival backend.");
            return true;
        }

        static void BuildSchoolInterior(Transform root){
            Box("school interior floor",root,new Vector3(-10,.035f,-4),new Vector3(24.35f,.07f,12.25f),floorWarm,false);
            Box("north wing floor",root,new Vector3(-16,.038f,5.05f),new Vector3(11.35f,.075f,6.85f),floorClass,false);
            Box("main corridor floor",root,new Vector3(-10,.078f,-1.15f),new Vector3(23.6f,.035f,2.05f),floorClass,false);
            Box("entry runner",root,new Vector3(-10,.082f,-7.65f),new Vector3(2.5f,.025f,4.1f),rugGreen,false);

            LowWall(root,new Vector3(-14.6f,.62f,-5.25f),new Vector3(.11f,1.24f,6.2f));
            LowWall(root,new Vector3(-7.6f,.62f,-5.25f),new Vector3(.11f,1.24f,6.2f));
            LowWall(root,new Vector3(-1.2f,.62f,-5.25f),new Vector3(.11f,1.24f,6.2f));
            LowWall(root,new Vector3(-18.2f,.62f,1.15f),new Vector3(7.2f,1.24f,.11f));
            LowWall(root,new Vector3(-9.2f,.62f,1.15f),new Vector3(6.0f,1.24f,.11f));
            LowWall(root,new Vector3(-2.9f,.62f,1.15f),new Vector3(3.9f,1.24f,.11f));

            Transform classA=Group(root,"CLASSROOM A");
            RoomLabel(classA,"KLASSRUM",new Vector3(-18.2f,.11f,-8.9f));
            for(int row=0;row<2;row++)for(int col=0;col<3;col++){
                float x=-20.8f+col*1.85f,z=-7.1f+row*1.7f;
                Place(classA,"table_small",new Vector3(x,0,z),0,.92f);
                Place(classA,"chair_A",new Vector3(x,0,z+.62f),180,.92f);
            }
            Place(classA,"table_medium",new Vector3(-18.0f,0,-9.0f),0,.95f);
            Place(classA,"shelf_A_big",new Vector3(-21.1f,0,-3.0f),90,.92f);
            Board(classA,new Vector3(-17.7f,1.15f,-10.05f),new Vector3(3.3f,1.25f,.07f));

            Transform classB=Group(root,"CLASSROOM B + READING");
            RoomLabel(classB,"LÄSRUM",new Vector3(-11.1f,.11f,-8.9f));
            for(int row=0;row<2;row++)for(int col=0;col<2;col++){
                float x=-13.0f+col*2.05f,z=-6.7f+row*1.9f;
                Place(classB,"table_small",new Vector3(x,0,z),0,.92f);Place(classB,"chair_B",new Vector3(x,0,z+.64f),180,.92f);
            }
            Place(classB,"shelf_B_large_decorated",new Vector3(-13.8f,0,-3.0f),0,.95f);
            Place(classB,"shelf_B_small_decorated",new Vector3(-9.0f,0,-3.0f),0,.95f);
            Box("reading rug",classB,new Vector3(-9.7f,.09f,-7.7f),new Vector3(2.5f,.025f,2.1f),rugGreen,false);
            Place(classB,"armchair_pillows",new Vector3(-9.7f,0,-7.6f),180,.95f);

            Transform staff=Group(root,"STAFF ROOM");
            RoomLabel(staff,"PERSONALRUM",new Vector3(-4.2f,.11f,-8.9f));
            Box("staff rug",staff,new Vector3(-4.5f,.09f,-6.2f),new Vector3(3.8f,.025f,2.7f),rugRust,false);
            Place(staff,"couch_pillows",new Vector3(-6.0f,0,-6.3f),90,1f);
            Place(staff,"armchair_pillows",new Vector3(-3.1f,0,-6.6f),270,.96f);
            Place(staff,"table_low",new Vector3(-4.5f,0,-6.1f),0,.96f);
            Place(staff,"lamp_standing",new Vector3(-2.1f,0,-8.2f),0,.95f);
            Place(staff,"cactus_medium_A",new Vector3(-6.6f,0,-8.3f),0,.9f);
            PlaceContainer(staff,"cabinet_medium_decorated",new Vector3(-6.3f,0,-3.0f),0,.95f,"personalrummets skåp",new[]{"Förband","Tejp","Penna"},new[]{1,1,2});
            WarmLight(staff,new Vector3(-4.5f,2.0f,-6.2f),4.8f,1.15f);

            Transform entry=Group(root,"ENTRY + COAT CORRIDOR");
            Place(entry,"bench",new Vector3(-12.8f,0,-1.4f),0,.9f);
            Place(entry,"bench",new Vector3(-6.7f,0,-1.4f),0,.9f);
            CoatRail(entry,new Vector3(-18.6f,1.15f,-1.3f),4.2f);
            PlaceContainer(entry,"cabinet_medium_decorated",new Vector3(-20.6f,0,-1.0f),90,.92f,"kvarglömda kläder",new[]{"Ulltröja","Handskar","Mössa"},new[]{1,1,1});
            Place(entry,"cactus_medium_A",new Vector3(-1.8f,0,-1.5f),0,.86f);

            Transform library=Group(root,"LIBRARY WING");
            Box("library rug",library,new Vector3(-16,.09f,5.1f),new Vector3(4.6f,.025f,3.1f),rugGreen,false);
            Place(library,"shelf_A_big",new Vector3(-20.3f,0,5.9f),90,.95f);
            Place(library,"shelf_B_large_decorated",new Vector3(-11.9f,0,5.9f),270,.95f);
            Place(library,"armchair_pillows",new Vector3(-16.8f,0,4.6f),90,.96f);
            Place(library,"armchair_pillows",new Vector3(-14.9f,0,4.6f),270,.96f);
            Place(library,"table_low",new Vector3(-15.85f,0,4.65f),0,.9f);
            Place(library,"lamp_standing",new Vector3(-13.4f,0,6.6f),0,.9f);
            PlaceContainer(library,"cabinet_small_decorated",new Vector3(-18.8f,0,7.6f),0,.92f,"bibliotekets materialskåp",new[]{"Batterier","Säkring","Penna"},new[]{2,1,2});
            WarmLight(library,new Vector3(-15.8f,2.1f,4.9f),4.4f,.95f);

            Transform service=Group(root,"JANITOR + MATERIAL");
            PlaceContainer(service,"cabinet_medium_decorated",new Vector3(.35f,0,-8.0f),90,.92f,"vaktmästarens skåp",new[]{"Multiverktyg","Batterier","Tejp","Metallskrot"},new[]{1,2,1,2});
            Place(service,"shelf_A_big",new Vector3(.35f,0,-4.8f),90,.90f);
            Place(service,"table_medium",new Vector3(-.3f,0,-6.1f),90,.88f);

            Transform pantry=Group(root,"STAFF PANTRY");
            PlaceContainer(pantry,"cabinet_small_decorated",new Vector3(-2.0f,0,-3.0f),0,.92f,"personalrummets köksskåp",new[]{"Energibar","Vattenflaska","Kex"},new[]{2,2,1});
        }

        static void CleanupFalseInteractions(Transform campus){
            foreach(CozyInteractableV57 x in campus.GetComponentsInChildren<CozyInteractableV57>(true).ToArray()){
                if(!x)continue;string path=HierarchyPath(x.transform).ToLowerInvariant();
                if(IsArchitecture(path)&&!IsRealInteractive(path))UnityEngine.Object.DestroyImmediate(x);
            }
            foreach(LootContainerV74 x in campus.GetComponentsInChildren<LootContainerV74>(true).ToArray()){
                if(!x)continue;string path=HierarchyPath(x.transform).ToLowerInvariant();
                if(IsArchitecture(path)&&!IsRealInteractive(path))UnityEngine.Object.DestroyImmediate(x);
            }
        }
        static bool IsArchitecture(string s){return s.Contains("wall")||s.Contains("facade")||s.Contains("roof")||s.Contains("plinth")||s.Contains("floor")||s.Contains("window")||s.Contains("mullion")||s.Contains("canopy")||s.Contains("post")||s.Contains("seam")||s.Contains("snow");}
        static bool IsRealInteractive(string s){return s.Contains("door")||s.Contains("cabinet")||s.Contains("locker")||s.Contains("skåp")||s.Contains("shelf")||s.Contains("låda");}
        static string HierarchyPath(Transform t){string s=t.name;while(t.parent){t=t.parent;s=t.name+"/"+s;}return s;}

        static void CleanupUnreadableCampusProps(Transform campus){
            foreach(Transform t in campus.GetComponentsInChildren<Transform>(true).ToArray()){
                if(!t||t==campus)continue;string n=t.name.ToLowerInvariant();
                if(n=="bike rack"||n.Contains("debug")||n.Contains("placeholder"))UnityEngine.Object.DestroyImmediate(t.gameObject);
            }
        }
        static void CleanupUnreadableSceneProps(Vector3 playerPos){
            foreach(GameObject g in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
                if(!g||!g.scene.IsValid())continue;string n=g.name.ToLowerInvariant();
                if((n=="bike rack"||n.Contains("placeholder"))&&Vector3.Distance(g.transform.position,playerPos)<50f)UnityEngine.Object.DestroyImmediate(g);
            }
        }

        static void BuildReadableEntranceDetails(Transform root){
            Transform r=Group(root,"READABLE SCHOOL ENTRANCE DETAILS");
            Box("salt sand box",r,new Vector3(-13.8f,.28f,-11.9f),new Vector3(.85f,.55f,.65f),wood,false);
            Box("waste bin",r,new Vector3(-7.0f,.38f,-11.8f),new Vector3(.55f,.76f,.55f),metal,false);
            for(int i=0;i<4;i++){
                float x=-18.6f+i*.78f;
                Box("bike rack base",r,new Vector3(x,.04f,-11.7f),new Vector3(.52f,.08f,.72f),metal,false);
            }
        }

        static void BuildMaterials(GameObject world){
            floorWarm=LoadMat("Assets/Rosvik/GeneratedV92/warm floor.mat",new Color(.28f,.22f,.18f));
            floorClass=MakeMat("V97 muted linoleum",new Color(.22f,.29f,.29f),.38f);
            floorStaff=MakeMat("V97 staff floor",new Color(.30f,.24f,.20f),.42f);
            wall=LoadMat("Assets/Rosvik/GeneratedV92/cream.mat",new Color(.68f,.66f,.58f));
            wood=LoadMat("Assets/Rosvik/GeneratedV92/wood.mat",new Color(.40f,.25f,.15f));
            metal=LoadMat("Assets/Rosvik/GeneratedV92/metal.mat",new Color(.24f,.28f,.29f));
            rugGreen=MakeMat("V97 rug green",new Color(.20f,.31f,.29f),.30f);
            rugRust=MakeMat("V97 rug rust",new Color(.38f,.22f,.17f),.32f);
            whiteboard=MakeMat("V97 whiteboard",new Color(.82f,.83f,.78f),.22f);
        }
        static Material LoadMat(string path,Color fallback){Material m=AssetDatabase.LoadAssetAtPath<Material>(path);return m?m:MakeMat(System.IO.Path.GetFileNameWithoutExtension(path),fallback,.4f);}
        static Material MakeMat(string name,Color color,float smooth){string p=Generated+"/"+name+".mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(p);if(!m){Shader s=Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard");m=new Material(s){name=name};AssetDatabase.CreateAsset(m,p);}if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color);if(m.HasProperty("_Color"))m.SetColor("_Color",color);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;}

        static GameObject Place(Transform p,string assetName,Vector3 pos,float yaw,float scale){
            GameObject source=FindModel(assetName);GameObject g=null;
            if(source){g=PrefabUtility.InstantiatePrefab(source) as GameObject;if(!g)g=UnityEngine.Object.Instantiate(source);}
            if(!g){g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.transform.localScale=new Vector3(.8f,.7f,.8f);Renderer rr=g.GetComponent<Renderer>();if(rr)rr.sharedMaterial=wood;Collider cc=g.GetComponent<Collider>();if(cc)UnityEngine.Object.DestroyImmediate(cc);}
            g.name=assetName;g.transform.SetParent(p,true);g.transform.position=pos;g.transform.rotation=Quaternion.Euler(0,yaw,0);g.transform.localScale*=scale;return g;
        }
        static GameObject PlaceContainer(Transform p,string assetName,Vector3 pos,float yaw,float scale,string display,string[] items,int[] counts){
            GameObject g=Place(p,assetName,pos,yaw,scale);LootContainerV74 c=g.GetComponent<LootContainerV74>();if(!c)c=g.AddComponent<LootContainerV74>();c.displayName=display;c.radius=2.15f;c.items=items;c.counts=counts;c.RebuildContents();c.highlightRenderer=g.GetComponentInChildren<Renderer>();c.RefreshHighlight();return g;
        }
        static GameObject FindModel(string name){if(modelCache.TryGetValue(name,out var cached)&&cached)return cached;string[] ids=AssetDatabase.FindAssets(name+" t:GameObject");foreach(string id in ids){string p=AssetDatabase.GUIDToAssetPath(id);GameObject g=AssetDatabase.LoadAssetAtPath<GameObject>(p);if(g&&g.name.IndexOf(name,StringComparison.OrdinalIgnoreCase)>=0){modelCache[name]=g;return g;}}modelCache[name]=null;return null;}

        static void LowWall(Transform p,Vector3 pos,Vector3 scale){Box("interior wall",p,pos,scale,wall,false);}
        static void Board(Transform p,Vector3 pos,Vector3 scale){Box("whiteboard",p,pos,scale,whiteboard,false);Box("whiteboard frame",p,pos+new Vector3(0,-scale.y*.52f,0),new Vector3(scale.x+.08f,.06f,.10f),metal,false);}
        static void RoomLabel(Transform p,string text,Vector3 pos){GameObject g=new GameObject(text);g.transform.SetParent(p,true);g.transform.position=pos;g.transform.rotation=Quaternion.Euler(90,0,0);TextMesh tm=g.AddComponent<TextMesh>();tm.text=text;tm.characterSize=.14f;tm.fontSize=40;tm.color=new Color(.55f,.62f,.59f,.45f);tm.anchor=TextAnchor.MiddleCenter;}
        static void CoatRail(Transform p,Vector3 pos,float width){Box("coat rail",p,pos,new Vector3(width,.08f,.08f),wood,false);for(int i=0;i<8;i++){float x=pos.x-width*.44f+i*(width*.88f/7f);Box("coat hook",p,new Vector3(x,pos.y-.12f,pos.z-.09f),new Vector3(.055f,.30f,.055f),metal,false);}}
        static void WarmLight(Transform p,Vector3 pos,float range,float intensity){GameObject g=new GameObject("warm interior pool");g.transform.SetParent(p,true);g.transform.position=pos;Light l=g.AddComponent<Light>();l.type=LightType.Point;l.range=range;l.intensity=intensity;l.color=new Color(1f,.72f,.46f);l.shadows=LightShadows.Soft;}
        static GameObject Box(string name,Transform p,Vector3 pos,Vector3 scale,Material mat,bool collider){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(p,true);g.transform.position=pos;g.transform.localScale=scale;Renderer r=g.GetComponent<Renderer>();if(r)r.sharedMaterial=mat;if(!collider){Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);}return g;}
        static Transform Group(Transform p,string name){GameObject g=new GameObject(name);g.transform.SetParent(p,true);return g.transform;}
        static Transform FindDeep(Transform p,string exact){foreach(Transform t in p.GetComponentsInChildren<Transform>(true))if(t.name==exact)return t;return null;}
        static Transform FindContaining(Transform p,string term){foreach(Transform t in p.GetComponentsInChildren<Transform>(true))if(t.name.IndexOf(term,StringComparison.OrdinalIgnoreCase)>=0)return t;return null;}
        static void CleanupMissingScripts(){foreach(GameObject g in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(g&&g.scene.IsValid())GameObjectUtility.RemoveMonoBehavioursWithMissingScript(g);}
    }
}
#endif
