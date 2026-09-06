using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Rosvik.Blackout {
    [DefaultExecutionOrder(2200)]
    public sealed class SurvivalPresentationV72 : MonoBehaviour {
        sealed class ExtraMeta {
            public string icon, kind, description;
            public ExtraMeta(string i,string k,string d){icon=i;kind=k;description=d;}
        }

        static readonly Dictionary<string,ExtraMeta> extras = new Dictionary<string,ExtraMeta>(StringComparer.OrdinalIgnoreCase) {
            {"Tygbit",new ExtraMeta("bandage","MATERIAL","Tyg från förråd och gamla textilier. Används vid tillverkning.")},
            {"Metallskrot",new ExtraMeta("multitool","MATERIAL","Små metalldelar som går att använda vid reparationer och crafting.")},
            {"Träspill",new ExtraMeta("misc","MATERIAL","Torrt träspill. Användbart till enkla konstruktioner och bränsle.")},
            {"Bränsle",new ExtraMeta("lighter","BRÄNSLE","Ett litet bränslepaket för stormkök och provisorisk värme.")},
            {"Improviserad fackla",new ExtraMeta("flashlight","VERKTYG","En enkel fackla. Tryck T för att tända den när du behöver reservljus.")},
            {"Varm soppa",new ExtraMeta("soup","MAT","Uppvärmd soppa. Ger både mat, vätska och en snabb värmeeffekt.")}
        };

        SurvivalSystemsV69 survival;
        SurvivalProgressionV71 progression;
        SurvivalCraftingV72 crafting;
        CoziPlayerV57 player;
        bool panelOpen;
        Vector2 scroll;
        string selectedItem="";

        FieldInfo defsField, equippedField;
        MethodInfo useItemMethod;
        Dictionary<string,SurvivalSystemsV69.ItemDef> defs;
        Dictionary<string,string> equipped;
        readonly Dictionary<string,Texture2D> iconCache=new Dictionary<string,Texture2D>(StringComparer.OrdinalIgnoreCase);

        ParticleSystem rainFx, snowFx;
        Material rainMat, snowMat;
        Texture2D rainTex, snowTex;
        Light sun;

        void Awake() {
            player=GetComponent<CoziPlayerV57>(); if(!player)player=FindFirstObjectByType<CoziPlayerV57>();
            survival=GetComponent<SurvivalSystemsV69>(); if(!survival)survival=FindFirstObjectByType<SurvivalSystemsV69>();
            progression=GetComponent<SurvivalProgressionV71>(); if(!progression)progression=FindFirstObjectByType<SurvivalProgressionV71>();
            crafting=GetComponent<SurvivalCraftingV72>(); if(!crafting)crafting=FindFirstObjectByType<SurvivalCraftingV72>();
            if(player) player.suppressLegacyGui=true;
            if(survival) survival.suppressLegacyGui=true;

            defsField=typeof(SurvivalSystemsV69).GetField("defs",BindingFlags.Static|BindingFlags.NonPublic);
            equippedField=typeof(SurvivalSystemsV69).GetField("equipped",BindingFlags.Instance|BindingFlags.NonPublic);
            useItemMethod=typeof(SurvivalSystemsV69).GetMethod("UseItem",BindingFlags.Instance|BindingFlags.NonPublic);
            defs=defsField!=null?defsField.GetValue(null) as Dictionary<string,SurvivalSystemsV69.ItemDef>:null;
            equipped=survival!=null&&equippedField!=null?equippedField.GetValue(survival) as Dictionary<string,string>:null;

            foreach(Light l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if(l&&l.type==LightType.Directional){sun=l;if(l.name.ToLowerInvariant().Contains("sun"))break;}
            RemoveOldWeather();
            BuildWeatherFx();
        }

        void Update() {
            if(!player||!survival)return;
            Keyboard kb=Keyboard.current;
            if(kb!=null&&(kb.iKey.wasPressedThisFrame||kb.tabKey.wasPressedThisFrame)){
                panelOpen=!panelOpen;
                Cursor.visible=panelOpen;
                if(panelOpen)Cursor.lockState=CursorLockMode.None;
            }
            UpdateWeatherFx();
        }

        void LateUpdate() {
            // V71 establishes the daylight every Update. V72 deliberately grades it darker afterwards.
            RenderSettings.ambientLight*=.76f;
            if(sun) sun.intensity*=.78f;
        }

        void RemoveOldWeather(){
            if(!player)return;
            foreach(string n in new[]{"V69 RAIN","V69 SNOW","V70 RAIN","V70 SNOW","V72 RAIN","V72 SNOW"}){
                Transform t=player.transform.Find(n);if(t)Destroy(t.gameObject);
            }
        }

        void BuildWeatherFx(){
            rainTex=RainTexture(); snowTex=SnowTexture();
            rainMat=ParticleMat("V72 Rain",rainTex,new Color(.63f,.72f,.78f,.48f));
            snowMat=ParticleMat("V72 Snow",snowTex,new Color(.92f,.95f,.98f,.76f));
            rainFx=Weather("V72 RAIN",false,rainMat); snowFx=Weather("V72 SNOW",true,snowMat);
        }

        Material ParticleMat(string name,Texture2D tex,Color col){
            bool srp=GraphicsSettings.currentRenderPipeline!=null||GraphicsSettings.defaultRenderPipeline!=null;
            Shader s=srp?Shader.Find("Universal Render Pipeline/Particles/Unlit"):Shader.Find("Particles/Standard Unlit");
            if(!s||!s.isSupported)s=Shader.Find("Universal Render Pipeline/Unlit");
            if(!s||!s.isSupported)s=Shader.Find("Unlit/Transparent");
            if(!s||!s.isSupported)s=Shader.Find("Sprites/Default");
            if(!s)return null;
            Material m=new Material(s){name=name};
            if(m.HasProperty("_BaseMap"))m.SetTexture("_BaseMap",tex); if(m.HasProperty("_MainTex"))m.SetTexture("_MainTex",tex);
            if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",col); if(m.HasProperty("_Color"))m.SetColor("_Color",col);
            if(m.HasProperty("_Surface"))m.SetFloat("_Surface",1f); if(m.HasProperty("_ZWrite"))m.SetFloat("_ZWrite",0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=3000;m.SetShaderPassEnabled("ShadowCaster",false);return m;
        }

        ParticleSystem Weather(string name,bool snow,Material mat){
            GameObject g=new GameObject(name);g.transform.SetParent(transform,false);g.transform.localPosition=new Vector3(0,8,0);
            ParticleSystem ps=g.AddComponent<ParticleSystem>();var main=ps.main;main.loop=true;main.simulationSpace=ParticleSystemSimulationSpace.World;
            main.maxParticles=snow?430:650;main.startLifetime=snow?new ParticleSystem.MinMaxCurve(4f,5.5f):new ParticleSystem.MinMaxCurve(.9f,1.3f);
            main.startSize=snow?new ParticleSystem.MinMaxCurve(.06f,.14f):new ParticleSystem.MinMaxCurve(.02f,.035f);main.startColor=Color.white;
            var em=ps.emission;em.rateOverTime=snow?72f:210f;var sh=ps.shape;sh.shapeType=ParticleSystemShapeType.Box;sh.scale=new Vector3(17,1,17);
            var vel=ps.velocityOverLifetime;vel.enabled=true;vel.space=ParticleSystemSimulationSpace.World;
            vel.x=new ParticleSystem.MinMaxCurve(snow?-.35f:-1.4f,snow?.18f:-.6f);vel.y=new ParticleSystem.MinMaxCurve(snow?-1.0f:-12f,snow?-.55f:-8f);vel.z=new ParticleSystem.MinMaxCurve(-.2f,.3f);
            var r=g.GetComponent<ParticleSystemRenderer>();r.renderMode=snow?ParticleSystemRenderMode.Billboard:ParticleSystemRenderMode.Stretch;r.material=mat;r.sortMode=ParticleSystemSortMode.Distance;
            if(!snow){r.lengthScale=4f;r.velocityScale=.08f;}g.SetActive(false);return ps;
        }

        void UpdateWeatherFx(){
            bool outside=survival.IsOutside(player.transform.position);
            bool rain=outside&&survival.weather==SurvivalSystemsV69.WeatherKind.Rain;
            bool snow=outside&&(survival.weather==SurvivalSystemsV69.WeatherKind.Snow||survival.weather==SurvivalSystemsV69.WeatherKind.ColdSnap);
            if(rainFx){rainFx.transform.position=player.transform.position+Vector3.up*8f;if(rainFx.gameObject.activeSelf!=rain)rainFx.gameObject.SetActive(rain);}
            if(snowFx){snowFx.transform.position=player.transform.position+Vector3.up*8f;if(snowFx.gameObject.activeSelf!=snow)snowFx.gameObject.SetActive(snow);}
        }

        Texture2D SnowTexture(){const int n=24;Texture2D t=new Texture2D(n,n,TextureFormat.RGBA32,false);for(int y=0;y<n;y++)for(int x=0;x<n;x++){float dx=(x-11.5f)/11.5f,dy=(y-11.5f)/11.5f,d=Mathf.Sqrt(dx*dx+dy*dy);float a=Mathf.Clamp01((1-d)*2.2f);t.SetPixel(x,y,new Color(1,1,1,a*a));}t.Apply();return t;}
        Texture2D RainTexture(){const int w=8,h=30;Texture2D t=new Texture2D(w,h,TextureFormat.RGBA32,false);for(int y=0;y<h;y++)for(int x=0;x<w;x++){float dx=Mathf.Abs(x-3.5f)/3.5f;float a=Mathf.Clamp01(1-dx);a*=Mathf.Sin(y/(float)(h-1)*Mathf.PI);t.SetPixel(x,y,new Color(1,1,1,a*.72f));}t.Apply();return t;}

        Dictionary<string,string> Equipped(){if(equipped==null&&survival!=null&&equippedField!=null)equipped=equippedField.GetValue(survival) as Dictionary<string,string>;return equipped;}
        bool IsEquipped(string n){var e=Equipped();return e!=null&&e.Values.Any(v=>string.Equals(v,n,StringComparison.OrdinalIgnoreCase));}
        float TotalInsulation(){float v=0;var e=Equipped();if(e==null||defs==null)return 0;foreach(string n in e.Values)if(!string.IsNullOrEmpty(n)&&defs.TryGetValue(n,out var d))v+=d.insulation;return v;}
        float TotalWaterproof(){float v=0;var e=Equipped();if(e==null||defs==null)return 0;foreach(string n in e.Values)if(!string.IsNullOrEmpty(n)&&defs.TryGetValue(n,out var d))v+=d.waterproof;return v;}

        string IconKey(string item){if(defs!=null&&defs.TryGetValue(item,out var d))return d.icon;if(extras.TryGetValue(item,out var x))return x.icon;return "misc";}
        string Kind(string item){if(defs!=null&&defs.TryGetValue(item,out var d))return KindLabel(d.kind);if(extras.TryGetValue(item,out var x))return x.kind;return "ÖVRIGT";}
        string Description(string item){if(defs!=null&&defs.TryGetValue(item,out var d))return d.description;if(extras.TryGetValue(item,out var x))return x.description;return "Ett föremål du har hittat.";}
        Texture2D Icon(string key){if(iconCache.TryGetValue(key,out var t)&&t)return t;t=Resources.Load<Texture2D>("ItemIcons/"+key);iconCache[key]=t;return t?t:Texture2D.whiteTexture;}

        void ActivateSelected(){
            if(string.IsNullOrEmpty(selectedItem)||player.CountItem(selectedItem)<=0)return;
            if(string.Equals(selectedItem,"Varm soppa",StringComparison.OrdinalIgnoreCase)){
                if(player.ConsumeItem(selectedItem,1)){survival.hunger=Mathf.Clamp(survival.hunger+40,0,100);survival.thirst=Mathf.Clamp(survival.thirst+18,0,100);survival.warmth=Mathf.Clamp(survival.warmth+24,0,100);player.ShowToast("Åt varm soppa",2f);}return;
            }
            if(defs!=null&&defs.TryGetValue(selectedItem,out var d)&&useItemMethod!=null)useItemMethod.Invoke(survival,new object[]{d});
        }

        bool CanActivate(string item){
            if(string.Equals(item,"Varm soppa",StringComparison.OrdinalIgnoreCase))return true;
            if(defs==null||!defs.TryGetValue(item,out var d))return false;
            return d.kind==SurvivalSystemsV69.ItemKind.Food||d.kind==SurvivalSystemsV69.ItemKind.Drink||d.kind==SurvivalSystemsV69.ItemKind.Medical||d.kind==SurvivalSystemsV69.ItemKind.Clothing;
        }

        string ActionLabel(string item){
            if(string.Equals(item,"Varm soppa",StringComparison.OrdinalIgnoreCase))return "ÄT";
            if(defs==null||!defs.TryGetValue(item,out var d))return "";
            if(d.kind==SurvivalSystemsV69.ItemKind.Clothing)return IsEquipped(item)?"TA AV":"TA PÅ";
            if(d.kind==SurvivalSystemsV69.ItemKind.Food)return "ÄT";if(d.kind==SurvivalSystemsV69.ItemKind.Drink)return "DRICK";if(d.kind==SurvivalSystemsV69.ItemKind.Medical)return "ANVÄND";return "";
        }

        void OnGUI(){
            if(!player||!survival)return;GUI.depth=-4000;
            GUIStyle title=new GUIStyle(GUI.skin.label){fontSize=20,fontStyle=FontStyle.Bold};title.normal.textColor=new Color(.95f,.91f,.80f);
            GUIStyle section=new GUIStyle(title){fontSize=15};
            GUIStyle text=new GUIStyle(GUI.skin.label){fontSize=12,wordWrap=true};text.normal.textColor=new Color(.88f,.87f,.80f);
            GUIStyle tiny=new GUIStyle(text){fontSize=10};
            GUIStyle center=new GUIStyle(text){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold};
            GUIStyle count=new GUIStyle(center){alignment=TextAnchor.UpperRight,fontSize=11};
            if(panelOpen)DrawInventory(title,section,text,tiny,center,count);else DrawHud(title,section,text,tiny);
        }

        void DrawHud(GUIStyle title,GUIStyle section,GUIStyle text,GUIStyle tiny){
            Rect objective=new Rect(16,16,355,76);Panel(objective,new Color(.035f,.045f,.042f,.94f));
            GUI.Label(new Rect(objective.x+14,objective.y+9,190,24),"THE DARK QUIET",title);
            GUI.Label(new Rect(objective.x+14,objective.y+36,objective.width-28,34),player.objective,text);

            Rect stats=new Rect(16,Screen.height-174,322,158);Panel(stats,new Color(.035f,.045f,.042f,.94f));
            float y=12;Stat(new Rect(stats.x+12,stats.y+y,stats.width-24,16),"HÄLSA",survival.health,new Color(.63f,.20f,.17f));y+=22;
            Stat(new Rect(stats.x+12,stats.y+y,stats.width-24,16),"MAT",survival.hunger,new Color(.61f,.44f,.17f));y+=22;
            Stat(new Rect(stats.x+12,stats.y+y,stats.width-24,16),"VATTEN",survival.thirst,new Color(.17f,.43f,.66f));y+=22;
            Stat(new Rect(stats.x+12,stats.y+y,stats.width-24,16),"VÄRME",survival.warmth,new Color(.75f,.35f,.14f));y+=22;
            Stat(new Rect(stats.x+12,stats.y+y,stats.width-24,16),"STAMINA",survival.stamina,new Color(.27f,.55f,.31f));
            string tail="Våt "+Mathf.RoundToInt(survival.wetness)+"%"+(progression?"   •   Ork "+Mathf.RoundToInt(progression.fatigue)+"%":"")+"   •   I/Tab";
            GUI.Label(new Rect(stats.x+12,stats.y+132,stats.width-24,18),tail,tiny);

            Rect weather=new Rect(Screen.width-272,16,256,116);Panel(weather,new Color(.035f,.045f,.042f,.94f));
            GUI.Label(new Rect(weather.x+13,weather.y+10,230,22),WeatherLabel()+"   "+Mathf.RoundToInt(survival.outsideTemperature)+"°C",section);
            GUI.Label(new Rect(weather.x+13,weather.y+38,230,18),"Vind "+survival.wind.ToString("0.0")+" m/s   •   "+(survival.IsOutside(player.transform.position)?"UTOMHUS":"SKYDDAD"),text);
            GUI.Label(new Rect(weather.x+13,weather.y+61,230,17),"Isolering "+TotalInsulation().ToString("0.0")+"   Vattenskydd "+TotalWaterproof().ToString("0.0"),tiny);
            if(player.HasItem("Ficklampa"))GUI.Label(new Rect(weather.x+13,weather.y+86,230,18),"Ficklampa "+(player.FlashlightOn?"PÅ":"AV")+"   •   "+Mathf.CeilToInt(player.flashlightBattery)+"%",text);

            if(crafting&&crafting.TorchFuel>0){Rect tr=new Rect(Screen.width-208,Screen.height-70,192,54);Panel(tr,new Color(.035f,.045f,.042f,.92f));GUI.Label(new Rect(tr.x+10,tr.y+8,172,18),"Fackla   "+Mathf.CeilToInt(crafting.TorchFuel)+" s",text);GUI.Label(new Rect(tr.x+10,tr.y+28,172,16),"T tänd/släck",tiny);}
        }

        void DrawInventory(GUIStyle title,GUIStyle section,GUIStyle text,GUIStyle tiny,GUIStyle center,GUIStyle count){
            Color old=GUI.color;GUI.color=new Color(.012f,.016f,.015f,.97f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=old;
            float pw=Mathf.Min(1180,Screen.width-36),ph=Mathf.Min(760,Screen.height-36);Rect panel=new Rect((Screen.width-pw)/2,(Screen.height-ph)/2,pw,ph);Panel(panel,new Color(.035f,.045f,.042f,.99f));
            GUI.Label(new Rect(panel.x+24,panel.y+16,400,28),"RYGGSÄCK",title);GUI.Label(new Rect(panel.x+24,panel.y+47,pw-48,20),"Välj ett föremål. Använd det från detaljrutan till höger.   I / Tab stänger.",text);

            float rightW=Mathf.Clamp(pw*.30f,300,360),gap=16,leftW=pw-rightW-gap-48;Rect left=new Rect(panel.x+24,panel.y+78,leftW,ph-102),right=new Rect(left.xMax+gap,panel.y+78,rightW,ph-102);Panel(left,new Color(.065f,.078f,.072f,.98f));Panel(right,new Color(.065f,.078f,.072f,.98f));
            GUI.Label(new Rect(left.x+14,left.y+10,left.width-28,22),"FÖREMÅL   •   "+player.Inventory.Sum(x=>x.Value)+" st",section);
            Rect view=new Rect(left.x+12,left.y+40,left.width-24,left.height-52);int cols=Mathf.Clamp(Mathf.FloorToInt((view.width+8)/128),3,6);float space=8,slotW=(view.width-18-(cols-1)*space)/cols,slotH=124;int rows=Mathf.CeilToInt(player.Inventory.Count/(float)cols);float contentH=Mathf.Max(view.height-4,rows*(slotH+space));
            scroll=GUI.BeginScrollView(view,scroll,new Rect(0,0,view.width-18,contentH));int i=0;
            foreach(var kv in player.Inventory.OrderBy(x=>Kind(x.Key)).ThenBy(x=>x.Key)){
                int c=i%cols,r=i/cols;Rect slot=new Rect(c*(slotW+space),r*(slotH+space),slotW,slotH);bool selected=string.Equals(selectedItem,kv.Key,StringComparison.OrdinalIgnoreCase);Color bc=selected?new Color(.33f,.43f,.34f,1):IsEquipped(kv.Key)?new Color(.24f,.34f,.27f,1):Color.white;GUI.color=bc;bool click=GUI.Button(slot,GUIContent.none);GUI.color=Color.white;
                float icon=Mathf.Min(66,slot.width-22);GUI.DrawTexture(new Rect(slot.x+(slot.width-icon)/2,slot.y+8,icon,icon),Icon(IconKey(kv.Key)),ScaleMode.ScaleToFit,true);
                GUI.Label(new Rect(slot.x+6,slot.y+77,slot.width-12,30),kv.Key,new GUIStyle(center){wordWrap=true});GUI.Label(new Rect(slot.x+7,slot.y+105,slot.width-14,16),Kind(kv.Key),tiny);if(kv.Value>1)GUI.Label(new Rect(slot.x+7,slot.y+6,slot.width-14,18),"x"+kv.Value,count);if(click)selectedItem=kv.Key;i++;
            }GUI.EndScrollView();

            float y=12;GUI.Label(new Rect(right.x+15,right.y+y,right.width-30,22),"UTRUSTNING",section);y+=32;var e=Equipped();foreach(string s in new[]{"Head","Body","Mid","Hands","Feet"}){string val=e!=null&&e.TryGetValue(s,out var v)?v:"";Rect rr=new Rect(right.x+13,right.y+y,right.width-26,30);Panel(rr,new Color(.09f,.105f,.097f,.98f));GUI.Label(new Rect(rr.x+8,rr.y+5,74,18),SlotLabel(s),tiny);GUI.Label(new Rect(rr.x+82,rr.y+5,rr.width-90,19),string.IsNullOrEmpty(val)?"—":val,text);y+=35;}GUI.Label(new Rect(right.x+15,right.y+y+2,right.width-30,18),"Isolering "+TotalInsulation().ToString("0.0")+"   •   Vattenskydd "+TotalWaterproof().ToString("0.0"),tiny);y+=34;
            GUI.Label(new Rect(right.x+15,right.y+y,right.width-30,22),"VALT FÖREMÅL",section);y+=28;
            if(!string.IsNullOrEmpty(selectedItem)&&player.CountItem(selectedItem)>0){float ic=Mathf.Min(94,right.width-60);GUI.DrawTexture(new Rect(right.x+(right.width-ic)/2,right.y+y,ic,ic),Icon(IconKey(selectedItem)),ScaleMode.ScaleToFit,true);y+=ic+8;GUI.Label(new Rect(right.x+15,right.y+y,right.width-30,24),selectedItem,section);y+=26;GUI.Label(new Rect(right.x+15,right.y+y,right.width-30,80),Description(selectedItem),text);y+=84;string action=ActionLabel(selectedItem);if(!string.IsNullOrEmpty(action)){if(GUI.Button(new Rect(right.x+15,right.y+y,right.width-30,38),action))ActivateSelected();}else GUI.Label(new Rect(right.x+15,right.y+y,right.width-30,40),"Det här föremålet används i världen eller vid crafting.",tiny);}else GUI.Label(new Rect(right.x+15,right.y+y,right.width-30,50),"Klicka på ett föremål i ryggsäcken.",text);
        }

        void Panel(Rect r,Color c){Color o=GUI.color;GUI.color=c;GUI.Box(r,"");GUI.color=o;}
        void Stat(Rect r,string label,float value,Color c){Panel(r,new Color(.075f,.085f,.08f,.98f));Rect f=new Rect(r.x+2,r.y+2,(r.width-4)*Mathf.Clamp01(value/100f),r.height-4);Color o=GUI.color;GUI.color=c;GUI.DrawTexture(f,Texture2D.whiteTexture);GUI.color=o;GUIStyle s=new GUIStyle(GUI.skin.label){fontSize=10,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};s.normal.textColor=Color.white;GUI.Label(r,label+"  "+Mathf.CeilToInt(value),s);}
        string WeatherLabel(){switch(survival.weather){case SurvivalSystemsV69.WeatherKind.Clear:return"KLART";case SurvivalSystemsV69.WeatherKind.Overcast:return"MULET";case SurvivalSystemsV69.WeatherKind.Rain:return"REGN";case SurvivalSystemsV69.WeatherKind.Snow:return"SNÖFALL";default:return"KÖLDKNÄPP";}}
        string KindLabel(SurvivalSystemsV69.ItemKind k){switch(k){case SurvivalSystemsV69.ItemKind.Food:return"MAT";case SurvivalSystemsV69.ItemKind.Drink:return"DRYCK";case SurvivalSystemsV69.ItemKind.Medical:return"VÅRD";case SurvivalSystemsV69.ItemKind.Clothing:return"KLÄDER";case SurvivalSystemsV69.ItemKind.Tool:return"VERKTYG";case SurvivalSystemsV69.ItemKind.Material:return"MATERIAL";default:return"ÖVRIGT";}}
        string SlotLabel(string s){switch(s){case"Head":return"HUVUD";case"Body":return"YTTER";case"Mid":return"MELLAN";case"Hands":return"HÄNDER";case"Feet":return"FÖTTER";default:return s.ToUpperInvariant();}}

        void OnDestroy(){if(rainMat)Destroy(rainMat);if(snowMat)Destroy(snowMat);if(rainTex)Destroy(rainTex);if(snowTex)Destroy(snowTex);}
    }
}
