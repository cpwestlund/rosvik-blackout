using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rosvik.Blackout {
    [DefaultExecutionOrder(2100)]
    public sealed class SurvivalCraftingV72 : MonoBehaviour {
        [Serializable]
        public sealed class Recipe {
            public string output;
            public int amount;
            public string[] ingredients;
            public int[] counts;
            public string description;
            public Recipe(string o,int a,string[] i,int[] c,string d){output=o;amount=a;ingredients=i;counts=c;description=d;}
        }

        public float TorchFuel => torchFuel;
        public bool TorchOn => torchOn;
        public bool PanelOpen => panelOpen;

        readonly List<Recipe> recipes=new List<Recipe>{
            new Recipe("Förband",1,new[]{"Tygbit","Tejp"},new[]{2,1},"Gör ett enkelt förband av tyg och tejp."),
            new Recipe("Bränsle",1,new[]{"Träspill","Tygbit"},new[]{2,1},"Binder ihop torrt trä och tyg till ett bränslepaket för stormköket."),
            new Recipe("Improviserad fackla",1,new[]{"Träspill","Tygbit","Bränsle"},new[]{1,1,1},"Reservljus när batterierna tar slut. Tryck T för att tända den.")
        };

        CoziPlayerV57 player;
        SurvivalSystemsV69 survival;
        bool panelOpen;
        float torchFuel;
        bool torchOn;
        Light torchLight;
        int selected;

        void Awake(){
            player=GetComponent<CoziPlayerV57>();if(!player)player=FindFirstObjectByType<CoziPlayerV57>();
            survival=GetComponent<SurvivalSystemsV69>();if(!survival)survival=FindFirstObjectByType<SurvivalSystemsV69>();
            BuildTorch();
        }

        void BuildTorch(){
            if(!player)return;
            Transform old=player.transform.Find("V72 TORCH LIGHT");if(old)Destroy(old.gameObject);
            GameObject g=new GameObject("V72 TORCH LIGHT");g.transform.SetParent(player.transform,false);g.transform.localPosition=new Vector3(.18f,.82f,.20f);
            torchLight=g.AddComponent<Light>();torchLight.type=LightType.Point;torchLight.color=new Color(1f,.55f,.22f);torchLight.range=6.2f;torchLight.intensity=3.1f;torchLight.shadows=LightShadows.Soft;torchLight.shadowStrength=.24f;torchLight.enabled=false;
        }

        void Update(){
            if(!player)return;
            Keyboard kb=Keyboard.current;if(kb==null)return;
            if(kb.tKey.wasPressedThisFrame&&!panelOpen)ToggleTorch();
            if(panelOpen&&(kb.escapeKey.wasPressedThisFrame||kb.cKey.wasPressedThisFrame)){panelOpen=false;Cursor.visible=false;}
            if(torchOn){torchFuel=Mathf.Max(0,torchFuel-Time.deltaTime);if(torchFuel<=0){torchOn=false;if(torchLight)torchLight.enabled=false;player.ShowToast("Facklan brann ut",2f);}}
        }

        void LateUpdate(){if(panelOpen&&player){player.walkSpeed=0;player.sprintSpeed=0;}}

        public void OpenWorkbench(){panelOpen=true;Cursor.visible=true;Cursor.lockState=CursorLockMode.None;}

        void ToggleTorch(){
            if(torchOn){torchOn=false;if(torchLight)torchLight.enabled=false;return;}
            if(torchFuel<=0){
                if(player.CountItem("Improviserad fackla")<=0){player.ShowToast("Du har ingen improviserad fackla",1.8f);return;}
                player.ConsumeItem("Improviserad fackla",1);torchFuel=180f;
            }
            torchOn=true;if(torchLight)torchLight.enabled=true;player.ShowToast("Tände den improviserade facklan",1.6f);
        }

        bool CanCraft(Recipe r){if(player==null)return false;for(int i=0;i<r.ingredients.Length;i++){int n=i<r.counts.Length?r.counts[i]:1;if(player.CountItem(r.ingredients[i])<n)return false;}return true;}
        void Craft(Recipe r){
            if(!CanCraft(r)){player.ShowToast("Saknar material",1.5f);return;}
            for(int i=0;i<r.ingredients.Length;i++){int n=i<r.counts.Length?r.counts[i]:1;player.ConsumeItem(r.ingredients[i],n);}player.AddItem(r.output,Mathf.Max(1,r.amount));player.ShowToast("Tillverkade: "+r.output,2f);
        }

        void OnGUI(){
            if(!panelOpen||!player)return;GUI.depth=-4500;
            GUIStyle title=new GUIStyle(GUI.skin.label){fontSize=20,fontStyle=FontStyle.Bold};title.normal.textColor=new Color(.96f,.92f,.81f);
            GUIStyle text=new GUIStyle(GUI.skin.label){fontSize=12,wordWrap=true};text.normal.textColor=new Color(.88f,.86f,.78f);
            GUIStyle section=new GUIStyle(title){fontSize=15};
            Color old=GUI.color;GUI.color=new Color(.01f,.014f,.013f,.94f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=old;
            float w=Mathf.Min(820,Screen.width-50),h=Mathf.Min(560,Screen.height-50);Rect p=new Rect((Screen.width-w)/2,(Screen.height-h)/2,w,h);Panel(p,new Color(.035f,.045f,.042f,.99f));
            GUI.Label(new Rect(p.x+22,p.y+16,w-44,28),"ARBETSBÄNK",title);GUI.Label(new Rect(p.x+22,p.y+48,w-44,20),"Välj recept. C eller Esc stänger.",text);
            Rect list=new Rect(p.x+20,p.y+82,w*.48f,h-104),detail=new Rect(list.xMax+16,p.y+82,w-list.width-56,h-104);Panel(list,new Color(.065f,.078f,.072f,.98f));Panel(detail,new Color(.065f,.078f,.072f,.98f));
            float y=14;for(int i=0;i<recipes.Count;i++){Recipe r=recipes[i];Rect row=new Rect(list.x+12,list.y+y,list.width-24,62);GUI.color=i==selected?new Color(.34f,.44f,.35f):Color.white;if(GUI.Button(row,GUIContent.none))selected=i;GUI.color=Color.white;GUI.Label(new Rect(row.x+10,row.y+8,row.width-20,20),r.output,section);GUI.Label(new Rect(row.x+10,row.y+31,row.width-20,18),CanCraft(r)?"KAN TILLVERKAS":"SAKNAR MATERIAL",text);y+=70;}
            Recipe sel=recipes[Mathf.Clamp(selected,0,recipes.Count-1)];float dy=14;GUI.Label(new Rect(detail.x+14,detail.y+dy,detail.width-28,24),sel.output,section);dy+=34;GUI.Label(new Rect(detail.x+14,detail.y+dy,detail.width-28,56),sel.description,text);dy+=68;GUI.Label(new Rect(detail.x+14,detail.y+dy,detail.width-28,20),"KRÄVER",section);dy+=28;
            for(int i=0;i<sel.ingredients.Length;i++){int need=i<sel.counts.Length?sel.counts[i]:1,have=player.CountItem(sel.ingredients[i]);GUI.Label(new Rect(detail.x+18,detail.y+dy,detail.width-36,20),sel.ingredients[i]+"   "+have+" / "+need,text);dy+=24;}
            dy+=16;GUI.enabled=CanCraft(sel);if(GUI.Button(new Rect(detail.x+14,detail.y+dy,detail.width-28,42),"TILLVERKA"))Craft(sel);GUI.enabled=true;
            GUI.Label(new Rect(detail.x+14,detail.y+detail.height-52,detail.width-28,36),"Fackla: "+(torchOn?"PÅ":"AV")+"   •   Brinntid "+Mathf.CeilToInt(torchFuel)+" s   •   T",text);
        }
        void Panel(Rect r,Color c){Color o=GUI.color;GUI.color=c;GUI.Box(r,"");GUI.color=o;}
    }

    public sealed class V72CraftStation : MonoBehaviour {
        public float radius=2.1f;
        CoziPlayerV57 player;SurvivalProgressionV71 progression;SurvivalCraftingV72 crafting;
        void Awake(){player=FindFirstObjectByType<CoziPlayerV57>();progression=FindFirstObjectByType<SurvivalProgressionV71>();crafting=FindFirstObjectByType<SurvivalCraftingV72>();}
        void Update(){if(!player||!progression||!crafting||crafting.PanelOpen)return;Vector3 d=transform.position-player.transform.position;d.y=0;if(d.sqrMagnitude>radius*radius)return;progression.ShowPrompt("E  Använd arbetsbänken");Keyboard kb=Keyboard.current;if(kb!=null&&kb.eKey.wasPressedThisFrame)crafting.OpenWorkbench();}
    }

    public sealed class V72Stove : MonoBehaviour {
        public float radius=2.0f;
        public float heatRadius=4.3f;
        public float fuelSeconds=0f;
        public bool IsOn{get;private set;}
        public Light glow;
        public Renderer flame;
        CoziPlayerV57 player;SurvivalSystemsV69 survival;SurvivalProgressionV71 progression;
        MaterialPropertyBlock block;int colorId=-1;

        void Awake(){
            player=FindFirstObjectByType<CoziPlayerV57>();survival=FindFirstObjectByType<SurvivalSystemsV69>();progression=FindFirstObjectByType<SurvivalProgressionV71>();
            if(flame&&flame.sharedMaterial){if(flame.sharedMaterial.HasProperty("_BaseColor"))colorId=Shader.PropertyToID("_BaseColor");else if(flame.sharedMaterial.HasProperty("_Color"))colorId=Shader.PropertyToID("_Color");block=new MaterialPropertyBlock();}Visual(false);
        }

        void Update(){
            if(IsOn){fuelSeconds=Mathf.Max(0,fuelSeconds-Time.deltaTime);if(fuelSeconds<=0){IsOn=false;Visual(false);if(player)player.ShowToast("Stormköket slocknade",1.8f);}WarmPlayer();}
            if(!player||!progression)return;Vector3 d=transform.position-player.transform.position;d.y=0;if(d.sqrMagnitude>radius*radius)return;
            string p=IsOn?"E  Släck stormköket   •   Shift+E  Värm soppa":"E  Tänd stormköket   •   kräver Tändare + Bränsle";progression.ShowPrompt(p);
            Keyboard kb=Keyboard.current;if(kb==null||!kb.eKey.wasPressedThisFrame)return;bool shift=kb.leftShiftKey.isPressed||kb.rightShiftKey.isPressed;
            if(IsOn&&shift){Cook();return;}if(IsOn){IsOn=false;Visual(false);return;}
            if(player.CountItem("Tändare")<=0){player.ShowToast("Du behöver en tändare",1.7f);return;}if(player.CountItem("Bränsle")<=0){player.ShowToast("Du behöver bränsle",1.7f);return;}
            player.ConsumeItem("Bränsle",1);fuelSeconds=210f;IsOn=true;Visual(true);player.ShowToast("Stormköket brinner",1.7f);
        }

        void Cook(){if(player.CountItem("Soppa")<=0){player.ShowToast("Du har ingen soppa att värma",1.7f);return;}player.ConsumeItem("Soppa",1);player.AddItem("Varm soppa",1);player.ShowToast("Värmde en portion soppa",2f);}
        void WarmPlayer(){if(!player||!survival)return;Vector3 d=transform.position-player.transform.position;d.y=0;float dist=d.magnitude;if(dist>heatRadius)return;float k=1-Mathf.Clamp01(dist/heatRadius);survival.warmth=Mathf.MoveTowards(survival.warmth,100f,8f*k*Time.deltaTime);survival.wetness=Mathf.MoveTowards(survival.wetness,0f,4f*k*Time.deltaTime);}
        void Visual(bool on){if(glow)glow.enabled=on;if(flame&&block!=null&&colorId>=0){flame.GetPropertyBlock(block);block.SetColor(colorId,on?new Color(1f,.42f,.08f):new Color(.18f,.16f,.13f));flame.SetPropertyBlock(block);}}
    }
}
