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
    public static class RosvikSurvivalV77 {
        const int Version = 77;
        const string Key = "ROSVIK_SURVIVAL_V77";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string GroupName = "V77 INJURY HAZARDS";
        const string IconsDir = "Assets/Rosvik/Resources/ItemIcons";

        static RosvikSurvivalV77(){ if(EditorPrefs.GetInt(Key,0)>=Version)return; EditorApplication.delayCall+=Auto; }
        [MenuItem("Rosvik/V77 FIX HOUSE DOORS + INJURIES + MEDICINE")]
        public static void Force(){EditorPrefs.DeleteKey(Key);EditorApplication.delayCall+=Auto;}

        static void Auto(){
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=Auto;return;}
            if(!File.Exists(ScenePath))return;
            try{Apply();}catch(Exception ex){Debug.LogError("V77 FAILED: "+ex);}
        }

        static void Apply(){
            if(EditorSceneManager.GetActiveScene().path!=ScenePath)EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            CoziPlayerV57 player=UnityEngine.Object.FindFirstObjectByType<CoziPlayerV57>();if(!player)throw new Exception("PLAYER missing");

            GenerateMedicalIcons();
            FixInteriorDoors(player);

            SurvivalInjuriesV77 injuries=player.GetComponent<SurvivalInjuriesV77>();if(!injuries)injuries=player.gameObject.AddComponent<SurvivalInjuriesV77>();injuries.enabled=true;

            AddMedicalLoot();
            BuildHazards();

            player.SetObjective("Överlev dagen. Undersök huset och skolan. M visar kropp/skador — behandla rätt problem med rätt utrustning.");
            EditorUtility.SetDirty(player);EditorUtility.SetDirty(injuries);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();EditorPrefs.SetInt(Key,Version);SceneView.RepaintAll();
            Debug.Log("V77 COMPLETE — house interior doors use a dedicated door network; body injuries, cold stress, medical treatment and physical hazards are active.");
        }

        static void FixInteriorDoors(CoziPlayerV57 player){
            HouseInteriorDoorNetworkV77 network=player.GetComponent<HouseInteriorDoorNetworkV77>();if(!network)network=player.gameObject.AddComponent<HouseInteriorDoorNetworkV77>();network.enabled=true;network.interactionDistance=2.35f;
            string[] names={"köket","sovrummet","inre dörr"};
            float[] angles={94f,-94f,94f};
            int fixedCount=0;
            for(int i=0;i<names.Length;i++){
                GameObject root=GameObject.Find("door — "+names[i]);
                CozyInteractableV57 legacy=null;
                if(!root){
                    foreach(CozyInteractableV57 x in UnityEngine.Object.FindObjectsByType<CozyInteractableV57>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
                        if(x&&x.kind==CozyInteractableV57.Kind.Door&&string.Equals(x.displayName,names[i],StringComparison.OrdinalIgnoreCase)){root=x.gameObject;legacy=x;break;}
                    }
                }
                if(!root)continue;
                if(!legacy)legacy=root.GetComponent<CozyInteractableV57>();
                Transform hinge=legacy&&legacy.movingPart?legacy.movingPart:root.transform.Find("door hinge");if(!hinge)continue;
                Vector3 closed=legacy?legacy.closedEuler:Vector3.zero;
                HouseInteriorDoorV77 d=root.GetComponent<HouseInteriorDoorV77>();if(!d)d=root.AddComponent<HouseInteriorDoorV77>();
                d.displayName=names[i];d.hinge=hinge;d.closedEuler=closed;d.openEuler=new Vector3(closed.x,angles[i],closed.z);d.animationTime=.24f;
                DoorPassageV76 oldPass=root.GetComponent<DoorPassageV76>();if(oldPass)UnityEngine.Object.DestroyImmediate(oldPass);
                if(legacy)UnityEngine.Object.DestroyImmediate(legacy);
                ClearDoorOpening(root.transform);
                EditorUtility.SetDirty(d);fixedCount++;
            }
            network.Refresh();EditorUtility.SetDirty(network);
            Debug.Log("V77 internal house doors converted: "+fixedCount);
        }

        static void ClearDoorOpening(Transform root){
            Vector3 dp=root.position;
            foreach(BoxCollider c in UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
                if(!c||c.transform.IsChildOf(root))continue;Bounds b=c.bounds;
                if(Mathf.Abs(b.center.y)>.9f&&b.min.y>1.45f)continue;
                Vector3 flat=b.center-dp;flat.y=0;
                if(flat.sqrMagnitude>1.35f*1.35f)continue;
                string n=c.gameObject.name.ToLowerInvariant();
                if(n.Contains("floor")||n.Contains("ground")||n.Contains("rug")||n.Contains("path"))continue;
                // Only remove suspicious thin wall blockers intersecting the actual opening.
                bool thinWall=(b.size.x<.35f||b.size.z<.35f)&&(b.size.x>1.0f||b.size.z>1.0f);
                if(thinWall){c.enabled=false;EditorUtility.SetDirty(c);}
            }
        }

        static void AddMedicalLoot(){
            foreach(LootContainerV74 c in UnityEngine.Object.FindObjectsByType<LootContainerV74>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
                if(!c)continue;string n=c.displayName.ToLowerInvariant();
                if(n.Contains("medicinskåp"))Append(c,new[]{"Värktabletter","Antiseptisk spray","Elastisk linda"},new[]{2,1,1});
                else if(n.Contains("första hjälpen")||n.Contains("förstahjälp"))Append(c,new[]{"Värktabletter","Elastisk linda"},new[]{1,1});
            }
        }

        static void Append(LootContainerV74 c,string[] extra,int[] counts){
            List<string> items=c.items!=null?c.items.ToList():new List<string>();List<int> nums=c.counts!=null?c.counts.ToList():new List<int>();
            while(nums.Count<items.Count)nums.Add(1);
            for(int i=0;i<extra.Length;i++){
                int idx=items.FindIndex(x=>string.Equals(x,extra[i],StringComparison.OrdinalIgnoreCase));
                if(idx>=0){nums[idx]=Mathf.Max(nums[idx],counts[i]);continue;}
                items.Add(extra[i]);nums.Add(counts[i]);
            }
            c.items=items.ToArray();c.counts=nums.ToArray();EditorUtility.SetDirty(c);
        }

        static void BuildHazards(){
            GameObject old=GameObject.Find(GroupName);if(old)UnityEngine.Object.DestroyImmediate(old);
            GameObject root=new GameObject(GroupName);
            Material mat=FindMaterial();
            // Ice on the path: safe when walking, dangerous when sprinting.
            GameObject ice=new GameObject("hal isfläck");ice.transform.SetParent(root.transform,true);ice.transform.position=new Vector3(-25.8f,.045f,.25f);
            BoxCollider ic=ice.AddComponent<BoxCollider>();ic.isTrigger=true;ic.size=new Vector3(2.5f,.35f,1.65f);
            InjuryHazardV77 ih=ice.AddComponent<InjuryHazardV77>();ih.kind=InjuryHazardV77.HazardKind.Ice;ih.cooldown=12f;
            for(int i=0;i<5;i++)Shard("ice shine",ice.transform,new Vector3(-.85f+i*.42f,.02f,(i%2==0?-.28f:.28f)),new Vector3(.32f,.025f,.07f),i%2==0?18f:-13f,mat);

            // Broken glass in the utility side of House A. Boots protect the player.
            GameObject glass=new GameObject("glasskärvor");glass.transform.SetParent(root.transform,true);glass.transform.position=new Vector3(-24.55f,.055f,6.75f);
            BoxCollider gc=glass.AddComponent<BoxCollider>();gc.isTrigger=true;gc.size=new Vector3(1.45f,.35f,1.15f);
            InjuryHazardV77 gh=glass.AddComponent<InjuryHazardV77>();gh.kind=InjuryHazardV77.HazardKind.BrokenGlass;gh.cooldown=14f;
            for(int i=0;i<7;i++)Shard("glass",glass.transform,new Vector3((i%4-.5f)*.24f,.02f,(i/4-.35f)*.35f),new Vector3(.18f,.035f,.05f),i*27f,mat);
        }

        static void Shard(string name,Transform p,Vector3 local,Vector3 scale,float yaw,Material mat){
            GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(p,false);g.transform.localPosition=local;g.transform.localScale=scale;g.transform.localRotation=Quaternion.Euler(0,yaw,0);
            Renderer r=g.GetComponent<Renderer>();if(r&&mat)r.sharedMaterial=mat;Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);
        }

        static Material FindMaterial(){
            foreach(Renderer r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
                if(!r||!r.sharedMaterial)continue;string n=r.gameObject.name.ToLowerInvariant();if(n.Contains("metal")||n.Contains("glass"))return r.sharedMaterial;
            }
            return null;
        }

        static void GenerateMedicalIcons(){
            Directory.CreateDirectory(IconsDir);
            MakeIcon("painkillers",DrawPills());MakeIcon("antiseptic",DrawSpray());MakeIcon("elasticwrap",DrawWrap());
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(string key in new[]{"painkillers","antiseptic","elasticwrap"}){
                string path=IconsDir+"/"+key+".png";TextureImporter ti=AssetImporter.GetAtPath(path) as TextureImporter;if(ti==null)continue;
                ti.alphaIsTransparency=true;ti.mipmapEnabled=false;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.filterMode=FilterMode.Bilinear;ti.SaveAndReimport();
            }
        }
        static void MakeIcon(string key,Texture2D t){File.WriteAllBytes(IconsDir+"/"+key+".png",t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);}
        static Texture2D BaseIcon(){const int N=96;Texture2D t=new Texture2D(N,N,TextureFormat.RGBA32,false);Color[] p=new Color[N*N];for(int i=0;i<p.Length;i++)p[i]=new Color(.09f,.11f,.10f,1);t.SetPixels(p);Rect(t,5,5,86,86,new Color(.16f,.18f,.16f,1));Rect(t,8,8,80,80,new Color(.075f,.09f,.083f,1));return t;}
        static Texture2D DrawPills(){Texture2D t=BaseIcon();Color cream=new Color(.86f,.83f,.72f),rust=new Color(.67f,.32f,.25f);Capsule(t,23,38,48,16,cream,rust);Capsule(t,30,59,40,14,rust,cream);t.Apply();return t;}
        static Texture2D DrawSpray(){Texture2D t=BaseIcon();Color c=new Color(.48f,.66f,.63f),hi=new Color(.82f,.84f,.76f),dk=new Color(.24f,.31f,.30f);Rect(t,33,31,30,45,c);Rect(t,38,22,20,12,hi);Rect(t,56,25,15,5,dk);Rect(t,39,43,18,15,hi);t.Apply();return t;}
        static Texture2D DrawWrap(){Texture2D t=BaseIcon();Color c=new Color(.79f,.73f,.60f),dk=new Color(.45f,.42f,.35f);Circle(t,48,50,26,c);Circle(t,48,50,11,new Color(.08f,.095f,.087f,1));Rect(t,56,62,24,10,c);Rect(t,62,65,18,4,dk);t.Apply();return t;}
        static void Capsule(Texture2D t,int x,int y,int w,int h,Color a,Color b){int half=w/2;Rect(t,x,y,half,h,a);Rect(t,x+half,y,w-half,h,b);Circle(t,x,y+h/2,h/2,a);Circle(t,x+w,y+h/2,h/2,b);}
        static void Rect(Texture2D t,int x,int y,int w,int h,Color c){for(int yy=Mathf.Max(0,y);yy<Mathf.Min(t.height,y+h);yy++)for(int xx=Mathf.Max(0,x);xx<Mathf.Min(t.width,x+w);xx++)t.SetPixel(xx,yy,c);}
        static void Circle(Texture2D t,int cx,int cy,int r,Color c){int rr=r*r;for(int y=-r;y<=r;y++)for(int x=-r;x<=r;x++)if(x*x+y*y<=rr){int px=cx+x,py=cy+y;if(px>=0&&py>=0&&px<t.width&&py<t.height)t.SetPixel(px,py,c);}}
    }
}
#endif
