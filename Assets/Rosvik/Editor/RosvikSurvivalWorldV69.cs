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
    public static class RosvikSurvivalWorldV69 {
        const int Version=69;
        const string Key="ROSVIK_SURVIVAL_WORLD_V69";
        const string ScenePath="Assets/Rosvik/Scenes/CozySchoolGame.unity";
        const string IconsDir="Assets/Rosvik/Resources/ItemIcons";
        const string GroupName="SURVIVAL WORLD V69";

        struct IconSpec { public string key,kind; public Color color; public IconSpec(string k,string t,string c){key=k;kind=t;color=C(c);} }
        static readonly IconSpec[] Icons={
            new IconSpec("flashlight","flashlight","e4b75f"),new IconSpec("batteries","battery","80b7d1"),new IconSpec("water","bottle","5aa8c8"),
            new IconSpec("soda","can","b84d4a"),new IconSpec("sportsdrink","bottle","e59345"),new IconSpec("energybar","packet","d49a48"),
            new IconSpec("can","can","8a9a87"),new IconSpec("soup","can","c66b4d"),new IconSpec("crackers","packet","d8bf79"),
            new IconSpec("chocolate","chocolate","7a4b35"),new IconSpec("apple","apple","b64d45"),new IconSpec("bandage","medical","e6ddd0"),
            new IconSpec("tape","tape","b8b3a2"),new IconSpec("sporttape","tape","e7ddd1"),new IconSpec("lighter","lighter","d97742"),
            new IconSpec("multitool","tool","8b9ba0"),new IconSpec("keys","keys","d7b85b"),new IconSpec("fuse","fuse","d6d0b9"),
            new IconSpec("pencil","pencil","d39a49"),new IconSpec("winterjacket","jacket","4d6976"),new IconSpec("rainjacket","jacket","d8a43f"),
            new IconSpec("sweater","shirt","765b50"),new IconSpec("hoodie","hoodie","557263"),new IconSpec("beanie","beanie","7d4f4c"),
            new IconSpec("gloves","gloves","584b43"),new IconSpec("boots","boots","4d3c31"),new IconSpec("misc","box","777267")
        };

        static RosvikSurvivalWorldV69(){ if(EditorPrefs.GetInt(Key,0)>=Version)return; EditorApplication.delayCall+=Auto; }
        [MenuItem("Rosvik/V69 SURVIVAL WORLD - ICONS CLOTHES WEATHER FOOD")]
        public static void Force(){EditorPrefs.DeleteKey(Key);EditorApplication.delayCall+=Auto;}
        static void Auto(){
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=Auto;return;}
            if(!File.Exists(ScenePath))return;
            try{Apply();}catch(Exception ex){Debug.LogError("V69 SURVIVAL WORLD FAILED: "+ex);}
        }

        static void Apply(){
            if(EditorSceneManager.GetActiveScene().path!=ScenePath)EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            GenerateIcons();
            GameObject old=GameObject.Find(GroupName); if(old)UnityEngine.Object.DestroyImmediate(old);
            GameObject root=new GameObject(GroupName); Transform containers=Group(root.transform,"CLOTHING + FOOD LOOT");

            Shader shader=PickShader();
            Material wood=MatTemp(shader,C("795034")); Material dark=MatTemp(shader,C("293231")); Material cream=MatTemp(shader,C("e5d8b6"));
            Material blue=MatTemp(shader,C("587b86")); Material rust=MatTemp(shader,C("9c5a42")); Material green=MatTemp(shader,C("3f6654")); Material yellow=MatTemp(shader,C("d3a24f"));

            CoziPlayerV57 player=UnityEngine.Object.FindFirstObjectByType<CoziPlayerV57>(); if(!player)throw new Exception("CoziPlayerV57 not found");
            SurvivalSystemsV69 survival=player.GetComponent<SurvivalSystemsV69>(); if(!survival)survival=player.gameObject.AddComponent<SurvivalSystemsV69>();

            // Clothing is concentrated where it makes sense: sporthall changing area and service storage.
            Locker(containers,"omklädningsskåp A",new Vector3(43.15f,0,2.45f),90,blue,dark,cream,"Vinterjacka",new[]{"Mössa","Handskar"},new[]{1,1});
            Locker(containers,"omklädningsskåp B",new Vector3(43.15f,0,4.05f),90,green,dark,cream,"Kängor",new[]{"Hoodie"},new[]{1});
            Locker(containers,"vaktmästarens klädskåp",new Vector3(17.15f,0,13.55f),90,yellow,dark,cream,"Regnjacka",new[]{"Ulltröja","Tejp"},new[]{1,1});

            // Food now lives in believable food/storage places instead of random boxes.
            Pantry(containers,"personalrummets skafferi V69",new Vector3(6.55f,0,7.25f),180,wood,dark,cream,rust,"Soppa",new[]{"Konservburk","Kex","Choklad"},new[]{1,2,2});
            Pantry(containers,"klassrum B mellanmål",new Vector3(-1.05f,0,14.10f),0,wood,dark,cream,green,"Äpple",new[]{"Vattenflaska","Energibar"},new[]{2,1});
            Pantry(containers,"sporthall dryckesförråd",new Vector3(23.25f,0,13.85f),180,blue,dark,cream,yellow,"Sportdryck",new[]{"Vattenflaska","Läsk","Energibar"},new[]{2,2,2});

            MedicalBox(containers,"sporthall första hjälpen",new Vector3(44.15f,.78f,11.0f),cream,rust,dark,"Förband",new[]{"Sporttejp"},new[]{2});

            player.SetObjective("Överlev: hitta mat, kläder och utrustning. Vädret och kylan påverkar dig nu.");
            EditorUtility.SetDirty(player); EditorUtility.SetDirty(survival);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene()); AssetDatabase.SaveAssets(); EditorPrefs.SetInt(Key,Version); SceneView.RepaintAll();
            Debug.Log("V69 SURVIVAL WORLD COMPLETE — illustrated loot icons, clothing equipment, food/drink effects, temperature, rain/snow, wetness, stamina and new logical loot locations are active.");
        }

        static void Locker(Transform p,string name,Vector3 pos,float yaw,Material body,Material dark,Material trim,string primary,string[] extra,int[] counts){
            GameObject r=new GameObject(name);r.transform.SetParent(p,true);r.transform.position=pos;r.transform.rotation=Quaternion.Euler(0,yaw,0);
            GameObject shell=LocalBox("locker body",r.transform,new Vector3(0,.82f,0),new Vector3(.82f,1.64f,.62f),body,true);
            LocalBox("locker top",r.transform,new Vector3(0,1.67f,0),new Vector3(.88f,.08f,.66f),trim,false);
            Transform hinge=Group(r.transform,"locker hinge"); hinge.localPosition=new Vector3(-.42f,.82f,-.325f);
            LocalBox("locker door",hinge,new Vector3(.42f,0,0),new Vector3(.80f,1.55f,.055f),trim,true);
            for(int i=0;i<4;i++)LocalBox("vent",hinge,new Vector3(.42f,.48f-i*.10f,-.04f),new Vector3(.30f,.025f,.018f),dark,false);
            LocalBox("handle",hinge,new Vector3(.70f,0,-.07f),new Vector3(.06f,.28f,.06f),dark,false);
            GameObject contents=new GameObject("clothing contents");contents.transform.SetParent(r.transform,false);LootPreview(contents.transform,primary,new Vector3(0,.82f,0),body,dark,trim);
            CozyInteractableV57 x=r.AddComponent<CozyInteractableV57>();x.kind=CozyInteractableV57.Kind.Cabinet;x.displayName=name;x.itemName=primary;x.extraItems=extra;x.extraCounts=counts;x.radius=2f;x.movingPart=hinge;x.closedEuler=Vector3.zero;x.openEuler=new Vector3(0,-105,0);x.revealOnOpen=contents.transform;x.highlightRenderer=shell.GetComponent<Renderer>();x.animationTime=.28f;
        }

        static void Pantry(Transform p,string name,Vector3 pos,float yaw,Material body,Material dark,Material trim,Material accent,string primary,string[] extra,int[] counts){
            GameObject r=new GameObject(name);r.transform.SetParent(p,true);r.transform.position=pos;r.transform.rotation=Quaternion.Euler(0,yaw,0);
            GameObject shell=LocalBox("pantry body",r.transform,new Vector3(0,.62f,0),new Vector3(1.20f,1.22f,.52f),body,true);
            LocalBox("shelf",r.transform,new Vector3(0,.62f,-.04f),new Vector3(1.05f,.06f,.43f),dark,false);
            Transform h1=Group(r.transform,"left door hinge");h1.localPosition=new Vector3(-.60f,.62f,-.28f);LocalBox("left door",h1,new Vector3(.30f,0,0),new Vector3(.58f,1.16f,.05f),trim,true);
            Transform h2=Group(r.transform,"right door hinge");h2.localPosition=new Vector3(.60f,.62f,-.28f);LocalBox("right door",h2,new Vector3(-.30f,0,0),new Vector3(.58f,1.16f,.05f),trim,true);
            LocalBox("label",h1,new Vector3(.30f,.23f,-.035f),new Vector3(.28f,.14f,.018f),accent,false);
            GameObject contents=new GameObject("food contents");contents.transform.SetParent(r.transform,false);LootPreview(contents.transform,primary,new Vector3(0,.76f,-.02f),accent,dark,trim);
            CozyInteractableV57 x=r.AddComponent<CozyInteractableV57>();x.kind=CozyInteractableV57.Kind.Cabinet;x.displayName=name;x.itemName=primary;x.extraItems=extra;x.extraCounts=counts;x.radius=2f;x.movingPart=h1;x.movingPart2=h2;x.closedEuler=Vector3.zero;x.openEuler=new Vector3(0,-100,0);x.closedEuler2=Vector3.zero;x.openEuler2=new Vector3(0,100,0);x.revealOnOpen=contents.transform;x.highlightRenderer=shell.GetComponent<Renderer>();
        }

        static void MedicalBox(Transform p,string name,Vector3 pos,Material body,Material cross,Material dark,string primary,string[] extra,int[] counts){
            GameObject r=new GameObject(name);r.transform.SetParent(p,true);r.transform.position=pos;
            GameObject shell=LocalBox("medical cabinet",r.transform,Vector3.zero,new Vector3(.18f,.86f,.86f),body,true);
            LocalBox("cross v",r.transform,new Vector3(-.105f,0,0),new Vector3(.03f,.42f,.13f),cross,false);LocalBox("cross h",r.transform,new Vector3(-.105f,0,0),new Vector3(.03f,.13f,.42f),cross,false);
            Transform hinge=Group(r.transform,"medical hinge");hinge.localPosition=new Vector3(-.11f,0,-.43f);LocalBox("medical door",hinge,new Vector3(-.05f,0,.43f),new Vector3(.07f,.82f,.84f),body,true);
            CozyInteractableV57 x=r.AddComponent<CozyInteractableV57>();x.kind=CozyInteractableV57.Kind.Cabinet;x.displayName=name;x.itemName=primary;x.extraItems=extra;x.extraCounts=counts;x.radius=2f;x.movingPart=hinge;x.closedEuler=Vector3.zero;x.openEuler=new Vector3(0,-105,0);x.highlightRenderer=shell.GetComponent<Renderer>();
        }

        static void LootPreview(Transform p,string item,Vector3 c,Material accent,Material dark,Material light){
            string n=item.ToLowerInvariant();
            if(n.Contains("jacka")||n.Contains("tröja")||n.Contains("hoodie")){LocalBox("folded clothing",p,c,new Vector3(.62f,.18f,.42f),accent,false);LocalBox("sleeve",p,c+new Vector3(.31f,.02f,0),new Vector3(.22f,.12f,.25f),accent,false);}
            else if(n.Contains("käng")){LocalBox("boot",p,c,new Vector3(.24f,.30f,.42f),dark,false);LocalBox("boot foot",p,c+new Vector3(0,-.09f,-.20f),new Vector3(.24f,.14f,.30f),dark,false);}
            else if(n.Contains("flaska")||n.Contains("dryck")){Cylinder("bottle",p,c,new Vector3(.22f,.48f,.22f),accent);Cylinder("cap",p,c+new Vector3(0,.29f,0),new Vector3(.11f,.10f,.11f),dark);}
            else if(n.Contains("soppa")||n.Contains("konserv")){Cylinder("can",p,c,new Vector3(.28f,.38f,.28f),accent);}
            else {LocalBox("loot",p,c,new Vector3(.42f,.18f,.30f),accent,false);}
        }

        static void GenerateIcons(){
            Directory.CreateDirectory(IconsDir);
            foreach(var s in Icons){string path=IconsDir+"/"+s.key+".png";Texture2D t=DrawIcon(s);File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);}
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(var s in Icons){string path=IconsDir+"/"+s.key+".png";TextureImporter ti=AssetImporter.GetAtPath(path) as TextureImporter;if(ti==null)continue;ti.alphaIsTransparency=true;ti.mipmapEnabled=false;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.filterMode=FilterMode.Bilinear;ti.SaveAndReimport();}
        }

        static Texture2D DrawIcon(IconSpec s){
            const int N=96;Texture2D t=new Texture2D(N,N,TextureFormat.RGBA32,false);Color bg=new Color(.10f,.12f,.11f,1);Color[] px=new Color[N*N];for(int i=0;i<px.Length;i++)px[i]=bg;t.SetPixels(px);
            Color c=s.color, hi=Color.Lerp(c,Color.white,.45f), dk=Color.Lerp(c,Color.black,.35f);
            RectF(t,5,5,86,86,new Color(.16f,.18f,.16f,1));RectF(t,8,8,80,80,new Color(.09f,.11f,.10f,1));
            switch(s.kind){
                case "flashlight": RectF(t,30,40,35,15,c); Circle(t,68,47,15,hi);RectF(t,18,43,17,9,dk);break;
                case "battery": RectF(t,22,28,19,42,c);RectF(t,27,70,9,5,hi);RectF(t,55,28,19,42,c);RectF(t,60,70,9,5,hi);break;
                case "bottle": RectF(t,35,27,27,47,c);RectF(t,41,19,15,10,hi);RectF(t,38,43,21,12,Color.Lerp(c,Color.white,.2f));break;
                case "can": RectF(t,29,27,38,44,c);Ellipse(t,29,22,38,12,hi);Ellipse(t,29,65,38,12,dk);RectF(t,34,42,28,13,hi);break;
                case "packet": RectF(t,23,31,50,36,c);Line(t,25,36,70,61,hi,4);Line(t,25,61,70,36,dk,3);break;
                case "chocolate": RectF(t,24,25,48,48,c);for(int x=1;x<4;x++)RectF(t,24+x*12,27,2,44,dk);for(int y=1;y<4;y++)RectF(t,26,25+y*12,44,2,dk);break;
                case "apple": Circle(t,42,48,20,c);Circle(t,56,48,20,c);RectF(t,47,20,5,16,dk);Ellipse(t,52,18,18,9,new Color(.28f,.55f,.28f));break;
                case "medical": RectF(t,22,30,52,38,hi);RectF(t,43,34,10,30,c);RectF(t,32,44,32,10,c);break;
                case "tape": Circle(t,48,48,27,c);Circle(t,48,48,13,bg);break;
                case "lighter": RectF(t,31,30,35,44,c);RectF(t,37,22,22,10,dk);Triangle(t,new Vector2(48,14),new Vector2(39,28),new Vector2(57,28),new Color(1,.65f,.2f));break;
                case "tool": Line(t,27,70,66,28,c,10);Circle(t,68,27,12,hi);Circle(t,68,27,5,bg);break;
                case "keys": Circle(t,32,38,13,c);Circle(t,32,38,6,bg);Line(t,41,47,70,70,c,7);RectF(t,62,64,10,6,c);RectF(t,56,58,7,7,c);break;
                case "fuse": RectF(t,35,24,26,48,hi);RectF(t,31,26,6,44,c);RectF(t,59,26,6,44,c);RectF(t,39,39,18,17,dk);break;
                case "pencil": Line(t,26,70,67,29,c,9);Triangle(t,new Vector2(72,24),new Vector2(64,34),new Vector2(69,37),hi);break;
                case "jacket": Jacket(t,c,dk,false);break; case "hoodie": Jacket(t,c,dk,true);break;
                case "shirt": RectF(t,31,28,34,48,c);Triangle(t,new Vector2(31,31),new Vector2(15,45),new Vector2(31,51),c);Triangle(t,new Vector2(65,31),new Vector2(81,45),new Vector2(65,51),c);break;
                case "beanie": Ellipse(t,24,30,48,34,c);RectF(t,22,52,52,13,dk);Circle(t,48,25,7,hi);break;
                case "gloves": RectF(t,24,37,20,34,c);RectF(t,52,37,20,34,c);for(int i=0;i<4;i++){RectF(t,22+i*5,23,4,18,c);RectF(t,54+i*5,23,4,18,c);}break;
                case "boots": Boot(t,20,29,c,dk);Boot(t,52,29,c,dk);break;
                default: RectF(t,25,28,46,42,c);Line(t,25,28,48,16,hi,4);Line(t,71,28,48,16,hi,4);break;
            }
            t.Apply();return t;
        }
        static void Jacket(Texture2D t,Color c,Color dk,bool hood){RectF(t,31,30,34,48,c);Triangle(t,new Vector2(31,33),new Vector2(12,49),new Vector2(24,58),c);Triangle(t,new Vector2(65,33),new Vector2(84,49),new Vector2(72,58),c);Line(t,48,31,48,75,dk,3);if(hood){Ellipse(t,34,15,28,24,c);Circle(t,48,26,8,new Color(.09f,.11f,.10f));}}
        static void Boot(Texture2D t,int x,int y,Color c,Color dk){RectF(t,x+6,y,15,35,c);RectF(t,x+6,y+27,27,14,c);RectF(t,x+4,y+39,31,6,dk);}

        static void RectF(Texture2D t,int x,int y,int w,int h,Color c){for(int yy=Mathf.Max(0,y);yy<Mathf.Min(t.height,y+h);yy++)for(int xx=Mathf.Max(0,x);xx<Mathf.Min(t.width,x+w);xx++)t.SetPixel(xx,yy,c);}
        static void Circle(Texture2D t,int cx,int cy,int r,Color c){int rr=r*r;for(int y=-r;y<=r;y++)for(int x=-r;x<=r;x++)if(x*x+y*y<=rr)Put(t,cx+x,cy+y,c);}
        static void Ellipse(Texture2D t,int x,int y,int w,int h,Color c){float rx=w*.5f,ry=h*.5f,cx=x+rx,cy=y+ry;for(int yy=y;yy<y+h;yy++)for(int xx=x;xx<x+w;xx++){float dx=(xx-cx)/rx,dy=(yy-cy)/ry;if(dx*dx+dy*dy<=1)Put(t,xx,yy,c);}}
        static void Line(Texture2D t,int x0,int y0,int x1,int y1,Color c,int thick){int dx=Math.Abs(x1-x0),sx=x0<x1?1:-1,dy=-Math.Abs(y1-y0),sy=y0<y1?1:-1,err=dx+dy;while(true){Circle(t,x0,y0,Mathf.Max(1,thick/2),c);if(x0==x1&&y0==y1)break;int e2=2*err;if(e2>=dy){err+=dy;x0+=sx;}if(e2<=dx){err+=dx;y0+=sy;}}}
        static void Triangle(Texture2D t,Vector2 a,Vector2 b,Vector2 c,Color col){int minX=Mathf.FloorToInt(Mathf.Min(a.x,Mathf.Min(b.x,c.x))),maxX=Mathf.CeilToInt(Mathf.Max(a.x,Mathf.Max(b.x,c.x)));int minY=Mathf.FloorToInt(Mathf.Min(a.y,Mathf.Min(b.y,c.y))),maxY=Mathf.CeilToInt(Mathf.Max(a.y,Mathf.Max(b.y,c.y)));float Sign(Vector2 p1,Vector2 p2,Vector2 p3)=>(p1.x-p3.x)*(p2.y-p3.y)-(p2.x-p3.x)*(p1.y-p3.y);for(int y=minY;y<=maxY;y++)for(int x=minX;x<=maxX;x++){Vector2 p=new Vector2(x,y);bool b1=Sign(p,a,b)<0,b2=Sign(p,b,c)<0,b3=Sign(p,c,a)<0;if(b1==b2&&b2==b3)Put(t,x,y,col);}}
        static void Put(Texture2D t,int x,int y,Color c){if(x>=0&&y>=0&&x<t.width&&y<t.height)t.SetPixel(x,y,c);}

        static Shader PickShader(){Shader s=GraphicsSettings.currentRenderPipeline!=null||GraphicsSettings.defaultRenderPipeline!=null?Shader.Find("Universal Render Pipeline/Lit"):Shader.Find("Standard");if(!s||!s.isSupported)s=Shader.Find("Universal Render Pipeline/Simple Lit");if(!s||!s.isSupported)s=Shader.Find("Standard");return s;}
        static Material MatTemp(Shader s,Color c){Material m=new Material(s);if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);if(m.HasProperty("_Color"))m.SetColor("_Color",c);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",.18f);return m;}
        static Color C(string h){ColorUtility.TryParseHtmlString("#"+h,out Color c);return c;}
        static Transform Group(Transform p,string n){GameObject g=new GameObject(n);g.transform.SetParent(p,false);return g.transform;}
        static GameObject LocalBox(string n,Transform p,Vector3 pos,Vector3 size,Material m,bool collider){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;if(!collider){Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);}return g;}
        static void Cylinder(string n,Transform p,Vector3 pos,Vector3 scale,Material m){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=m;Collider c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);}
    }
}
#endif
