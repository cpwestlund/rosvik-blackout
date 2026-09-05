#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikAutoSetup {
        const string Root = "Assets/Rosvik/External/KenneyFurniture";
        const string ScenePath = "Assets/Rosvik/Scenes/RosvikHero.unity";
        const string Base = "https://raw.githubusercontent.com/ETdoFresh/kenney.nl/master/furniturekit_updated/Models/FBX%20format/";

        static readonly string[] Assets = {
            "bench.fbx","bookcaseOpen.fbx","bookcaseOpenLow.fbx","books.fbx",
            "chair.fbx","chairDesk.fbx","chairCushion.fbx","table.fbx",
            "trashcan.fbx","rugRectangle.fbx","rugDoormat.fbx","lampSquareCeiling.fbx",
            "lampRoundTable.fbx","pottedPlant.fbx","kitchenCabinet.fbx",
            "kitchenCabinetDrawer.fbx","kitchenCabinetUpper.fbx","wallWindow.fbx",
            "wallDoorwayWide.fbx","wall.fbx"
        };

        static RosvikAutoSetup() {
            EditorApplication.delayCall += Ensure;
        }

        [MenuItem("Rosvik/Rebuild Hero Slice")]
        public static void Ensure() {
            try {
                Directory.CreateDirectory(Root);
                Directory.CreateDirectory("Assets/Rosvik/Scenes");
                bool downloaded = false;
                using var wc = new WebClient();
                foreach (string file in Assets) {
                    string local = Path.Combine(Root,file);
                    if (File.Exists(local) && new FileInfo(local).Length > 1000) continue;
                    wc.DownloadFile(Base + file, local);
                    downloaded = true;
                }
                if (downloaded) {
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                }
                BuildScene();
            } catch (Exception ex) {
                Debug.LogError("ROSVIK UNITY SETUP FAILED: " + ex);
            }
        }

        static Material Mat(string name, Color c, float smooth=.25f, bool emission=false) {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var m = new Material(shader) { name = name, color = c };
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness",smooth);
            if (emission) {
                m.EnableKeyword("_EMISSION");
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor",c*2.2f);
            }
            return m;
        }

        static GameObject Cube(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool collider=true) {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name=name; go.transform.SetParent(parent); go.transform.localPosition=pos; go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=mat;
            if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static GameObject Prefab(string file, Transform parent, Vector3 pos, Vector3 scale, float yaw=0) {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/{file}");
            if (!prefab) { Debug.LogWarning("Missing asset "+file); return null; }
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name=Path.GetFileNameWithoutExtension(file);
            go.transform.SetParent(parent); go.transform.localPosition=pos; go.transform.localScale=scale; go.transform.localRotation=Quaternion.Euler(0,yaw,0);
            return go;
        }

        static void BuildScene() {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            scene.name="RosvikHero";
            var root=new GameObject("ROSVIK_HERO_SLICE").transform;

            var snow=Mat("Snow",new Color(.72f,.79f,.81f),.25f);
            var asphalt=Mat("Asphalt",new Color(.115f,.14f,.15f),.08f);
            var wood=Mat("School timber",new Color(.42f,.40f,.35f),.16f);
            var dark=Mat("Anthracite",new Color(.07f,.09f,.10f),.25f);
            var interior=Mat("Interior warm wall",new Color(.63f,.61f,.54f),.2f);
            var floor=Mat("Linoleum",new Color(.29f,.34f,.34f),.15f);
            var warm=Mat("Warm window",new Color(1f,.52f,.20f),.35f,true);
            var red=Mat("Muted red",new Color(.42f,.15f,.12f),.2f);

            // Ground and the one street we actually need.
            Cube("Snow ground",root,Vector3.zero,new Vector3(58,.25f,44),snow);
            Cube("School road",root,new Vector3(0,.15f,14),new Vector3(46,.08f,6.2f),asphalt);
            Cube("Ploughed walk",root,new Vector3(0,.20f,7.2f),new Vector3(38,.06f,5.4f),Mat("Packed snow",new Color(.59f,.65f,.66f),.12f));

            // SCHOOL: one clean authoritative shell. No duplicate generators.
            var school=new GameObject("Rosviks skola").transform; school.SetParent(root); school.localPosition=new Vector3(0,.25f,0);
            Cube("Back wall",school,new Vector3(0,1.8f,-5.2f),new Vector3(34,3.4f,.25f),wood);
            Cube("Left gable",school,new Vector3(-17,1.8f,0),new Vector3(.25f,3.4f,10.4f),wood);
            Cube("Right gable",school,new Vector3(17,1.8f,0),new Vector3(.25f,3.4f,10.4f),wood);
            // Front wall is split around a generous real doorway.
            Cube("Front L",school,new Vector3(-11.2f,1.8f,5.2f),new Vector3(11.6f,3.4f,.25f),wood);
            Cube("Front R",school,new Vector3(7.1f,1.8f,5.2f),new Vector3(19.8f,3.4f,.25f),wood);
            Cube("Roof",school,new Vector3(0,3.72f,0),new Vector3(34.6f,.28f,10.9f),dark,false);
            Cube("Floor",school,new Vector3(0,.08f,0),new Vector3(33.7f,.12f,10),floor);

            // Window rhythm + warm pockets.
            for(int i=0;i<8;i++) {
                float x=-14.6f+i*4.15f;
                if(x>-7 && x<-3) continue;
                var frame=Prefab("wallWindow.fbx",school,new Vector3(x,1.55f,5.05f),Vector3.one*1.02f,180);
                if(frame) frame.name="School window";
                Cube("Warm glass",school,new Vector3(x,1.62f,5.10f),new Vector3(1.48f,1.18f,.05f), i==4||i==5?warm:dark,false);
            }

            // Entrance canopy + two open leaves. The physical opening is unobstructed.
            var entrance=new GameObject("ENTRANCE - WALKABLE").transform; entrance.SetParent(school);
            Cube("Canopy",entrance,new Vector3(-5,3.12f,6.45f),new Vector3(5.8f,.24f,2.5f),dark);
            Cube("Snow canopy",entrance,new Vector3(-5,3.30f,6.45f),new Vector3(5.6f,.12f,2.3f),snow,false);
            Cube("Door left",entrance,new Vector3(-6.25f,1.35f,5.65f),new Vector3(1.0f,2.55f,.10f),warm).transform.localRotation=Quaternion.Euler(0,58,0);
            Cube("Door right",entrance,new Vector3(-3.75f,1.35f,5.65f),new Vector3(1.0f,2.55f,.10f),warm).transform.localRotation=Quaternion.Euler(0,-58,0);
            Cube("Mat",entrance,new Vector3(-5,.22f,6.10f),new Vector3(2.3f,.04f,1.3f),dark);
            var entryLight=new GameObject("Warm entrance light").AddComponent<Light>();
            entryLight.transform.SetParent(entrance); entryLight.transform.localPosition=new Vector3(-5,2.55f,6.25f);
            entryLight.type=LightType.Point; entryLight.color=new Color(1f,.55f,.27f); entryLight.intensity=950f; entryLight.range=7f; entryLight.shadows=LightShadows.Soft;

            // Connected corridor and one classroom. No fake teleports.
            Cube("Corridor divider",school,new Vector3(2.5f,.7f,2.4f),new Vector3(23f,1.1f,.18f),interior);
            Cube("Classroom divider",school,new Vector3(2.0f,.7f,-1.6f),new Vector3(.18f,1.1f,7.0f),interior);
            for(int r=0;r<2;r++) for(int col=0;col<4;col++) {
                Prefab("chairDesk.fbx",school,new Vector3(5.0f+col*2.25f,.22f,-.2f-r*2.4f),Vector3.one*.92f,180);
            }
            Prefab("bookcaseOpen.fbx",school,new Vector3(14.8f,.22f,-4.5f),Vector3.one*.95f,90);
            Prefab("bookcaseOpenLow.fbx",school,new Vector3(12.6f,.22f,-4.5f),Vector3.one*.95f,90);
            Prefab("table.fbx",school,new Vector3(14.0f,.22f,-2.2f),Vector3.one*.95f,90);
            Prefab("chair.fbx",school,new Vector3(14.0f,.22f,-1.1f),Vector3.one*.95f,0);
            Prefab("lampRoundTable.fbx",school,new Vector3(13.7f,1.03f,-2.25f),Vector3.one*.8f,0);
            Prefab("rugRectangle.fbx",school,new Vector3(9.2f,.23f,-2.2f),Vector3.one*1.8f,0);
            Prefab("pottedPlant.fbx",school,new Vector3(15.4f,.22f,-.5f),Vector3.one*.9f,0);
            Prefab("trashcan.fbx",school,new Vector3(3.2f,.22f,-4.6f),Vector3.one*.9f,0);

            // Hall clutter/coat zone.
            for(int i=0;i<4;i++) Prefab("bookcaseClosed.fbx",school,new Vector3(-14.6f+i*2.1f,.22f,3.65f),Vector3.one*.92f,180);
            Prefab("bench.fbx",school,new Vector3(-10.2f,.22f,2.9f),Vector3.one*.95f,90);
            Prefab("rugDoormat.fbx",school,new Vector3(-5,.23f,4.5f),Vector3.one*1.35f,0);
            Prefab("trashcan.fbx",school,new Vector3(-7.6f,.22f,3.75f),Vector3.one*.9f,0);

            // Schoolyard authored scenes, not random scatter.
            var yard=new GameObject("Schoolyard dressing").transform; yard.SetParent(root);
            Prefab("bench.fbx",yard,new Vector3(-11,.25f,8.2f),Vector3.one,90);
            Prefab("bench.fbx",yard,new Vector3(8.5f,.25f,8.2f),Vector3.one,90);
            Prefab("trashcan.fbx",yard,new Vector3(-8.2f,.25f,7.2f),Vector3.one,0);
            Prefab("trashcan.fbx",yard,new Vector3(11.3f,.25f,7.2f),Vector3.one,0);
            for(int i=0;i<3;i++) {
                var drift=GameObject.CreatePrimitive(PrimitiveType.Sphere);
                drift.name="Low snow drift"; drift.transform.SetParent(yard); drift.transform.localPosition=new Vector3(-14+i*10,.25f,5.8f);
                drift.transform.localScale=new Vector3(3.8f,.32f,.95f); drift.GetComponent<Renderer>().sharedMaterial=snow; UnityEngine.Object.DestroyImmediate(drift.GetComponent<Collider>());
            }

            // Player capsule placeholder, deliberately clean and temporary.
            var player=GameObject.CreatePrimitive(PrimitiveType.Capsule); player.name="PLAYER_PLACEHOLDER";
            player.transform.SetParent(root); player.transform.localPosition=new Vector3(-5,1.1f,11.0f);
            UnityEngine.Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
            var cc=player.AddComponent<CharacterController>(); cc.height=1.9f; cc.radius=.36f; cc.center=new Vector3(0,.95f,0);
            player.AddComponent<RosvikPlayerController>();
            player.GetComponent<Renderer>().sharedMaterial=red;

            // Camera.
            var cameraGo=new GameObject("Isometric Camera");
            var cam=cameraGo.AddComponent<Camera>(); cam.orthographic=true; cam.orthographicSize=11.5f; cam.nearClipPlane=.1f; cam.farClipPlane=150f;
            var rig=cameraGo.AddComponent<IsometricCameraRig>(); rig.target=player.transform; rig.yaw=45f; rig.pitch=48f; rig.distance=18f;
            Camera.SetupCurrent(cam);

            // Sun/fill.
            var sun=new GameObject("Cold winter sun").AddComponent<Light>(); sun.type=LightType.Directional; sun.color=new Color(.68f,.80f,.90f); sun.intensity=1.15f;
            sun.transform.rotation=Quaternion.Euler(43,-38,0); sun.shadows=LightShadows.Soft;
            RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=new Color(.20f,.27f,.30f);

            // Save.
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            Selection.activeGameObject=player;
            Debug.Log("ROSVIK UNITY HERO SLICE BUILT: "+ScenePath);
        }
    }
}
#endif
