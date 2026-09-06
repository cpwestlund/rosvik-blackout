using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rosvik.Blackout {
    public sealed class SurvivalSystemsV69 : MonoBehaviour {
        public enum WeatherKind { Clear, Overcast, Rain, Snow, ColdSnap }
        public enum ItemKind { Tool, Food, Drink, Medical, Clothing, Material, Misc }

        [Serializable]
        public sealed class ItemDef {
            public string name;
            public string icon;
            public ItemKind kind;
            public string slot;
            public float hunger;
            public float thirst;
            public float health;
            public float insulation;
            public float waterproof;
            public string description;
            public ItemDef(string n,string i,ItemKind k,string s="",float hu=0,float th=0,float he=0,float ins=0,float wp=0,string d="") {
                name=n; icon=i; kind=k; slot=s; hunger=hu; thirst=th; health=he; insulation=ins; waterproof=wp; description=d;
            }
        }

        static readonly Dictionary<string,ItemDef> defs = BuildDefs();
        static Dictionary<string,ItemDef> BuildDefs() {
            var d = new Dictionary<string,ItemDef>(StringComparer.OrdinalIgnoreCase);
            void A(ItemDef x){d[x.name]=x;}
            A(new ItemDef("Ficklampa","flashlight",ItemKind.Tool,d:"En riktig ficklampa. F använder den."));
            A(new ItemDef("Batterier","batteries",ItemKind.Material,d:"Reservbatterier till ficklampan."));
            A(new ItemDef("Vattenflaska","water",ItemKind.Drink,th:34,d:"Rent vatten."));
            A(new ItemDef("Läsk","soda",ItemKind.Drink,hu:4,th:22,d:"Söt läsk. Hjälper törsten en stund."));
            A(new ItemDef("Sportdryck","sportsdrink",ItemKind.Drink,hu:5,th:30,d:"Vätska och lite energi."));
            A(new ItemDef("Energibar","energybar",ItemKind.Food,hu:27,th:-2,d:"Kompakt mat med mycket energi."));
            A(new ItemDef("Konservburk","can",ItemKind.Food,hu:38,th:4,d:"Matkonserv. Tung men mättande."));
            A(new ItemDef("Soppa","soup",ItemKind.Food,hu:30,th:14,d:"Konserverad soppa."));
            A(new ItemDef("Kex","crackers",ItemKind.Food,hu:18,th:-4,d:"Torra kex."));
            A(new ItemDef("Choklad","chocolate",ItemKind.Food,hu:22,d:"Snabb energi."));
            A(new ItemDef("Äpple","apple",ItemKind.Food,hu:13,th:7,d:"Ett fortfarande ätbart äpple."));
            A(new ItemDef("Förband","bandage",ItemKind.Medical,he:28,d:"Stoppar blödning och återställer hälsa."));
            A(new ItemDef("Tejp","tape",ItemKind.Material,d:"Alltid användbart."));
            A(new ItemDef("Sporttejp","sporttape",ItemKind.Material,d:"Stark tejp från sporthallen."));
            A(new ItemDef("Tändare","lighter",ItemKind.Tool,d:"Kan tända eld när det systemet kommer in."));
            A(new ItemDef("Multiverktyg","multitool",ItemKind.Tool,d:"Tång, kniv och mejslar i ett."));
            A(new ItemDef("Nyckelknippa","keys",ItemKind.Tool,d:"En bunt skolnycklar."));
            A(new ItemDef("Säkring","fuse",ItemKind.Material,d:"Reservsäkring till elskåpet."));
            A(new ItemDef("Penna","pencil",ItemKind.Misc,d:"En vanlig penna."));
            A(new ItemDef("Vinterjacka","winterjacket",ItemKind.Clothing,"Body",ins:4.8f,wp:1.4f,d:"Varm vinterjacka."));
            A(new ItemDef("Regnjacka","rainjacket",ItemKind.Clothing,"Body",ins:2.0f,wp:5.0f,d:"Håller regnet ute."));
            A(new ItemDef("Ulltröja","sweater",ItemKind.Clothing,"Mid",ins:3.0f,wp:.2f,d:"Varm även när den är lite fuktig."));
            A(new ItemDef("Hoodie","hoodie",ItemKind.Clothing,"Mid",ins:1.7f,wp:.2f,d:"Bekväm men inte särskilt vädertålig."));
            A(new ItemDef("Mössa","beanie",ItemKind.Clothing,"Head",ins:1.5f,wp:.3f,d:"Minskar värmeförlust från huvudet."));
            A(new ItemDef("Handskar","gloves",ItemKind.Clothing,"Hands",ins:1.2f,wp:.5f,d:"Håller händerna varma."));
            A(new ItemDef("Kängor","boots",ItemKind.Clothing,"Feet",ins:1.4f,wp:1.8f,d:"Varma och vattentåliga kängor."));
            return d;
        }

        public float health = 100f;
        public float hunger = 100f;
        public float thirst = 100f;
        public float warmth = 100f;
        public float wetness = 0f;
        public float stamina = 100f;
        public WeatherKind weather = WeatherKind.Overcast;
        public float outsideTemperature = -4f;
        public float wind = 2.5f;
        public float weatherDuration = 75f;
        public bool suppressLegacyGui = false;

        CoziPlayerV57 player;
        bool panelOpen;
        float weatherTimer;
        int weatherIndex = 1;
        readonly Dictionary<string,string> equipped = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string,Texture2D> iconCache = new Dictionary<string,Texture2D>(StringComparer.OrdinalIgnoreCase);
        ParticleSystem rainFx, snowFx;
        float baseWalk, baseSprint;
        Vector2 scroll;
        string selectedItem = "";

        void Awake() {
            player = GetComponent<CoziPlayerV57>();
            if (!player) player = FindFirstObjectByType<CoziPlayerV57>();
            if (player) { baseWalk = player.walkSpeed; baseSprint = player.sprintSpeed; }
            equipped["Body"]=""; equipped["Mid"]=""; equipped["Head"]=""; equipped["Hands"]=""; equipped["Feet"]="";
            SetupWeatherFx();
            ApplyWeather(WeatherKind.Overcast, true);
        }

        void Update() {
            if (!player) return;
            Keyboard kb = Keyboard.current;
            if (kb != null && (kb.iKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame)) panelOpen = !panelOpen;

            weatherTimer += Time.deltaTime;
            if (weatherTimer >= weatherDuration) {
                weatherTimer = 0f;
                weatherIndex = (weatherIndex + 1) % 5;
                ApplyWeather((WeatherKind)weatherIndex, false);
            }

            bool outside = IsOutside(player.transform.position);
            UpdateWeatherFx(outside);
            UpdateNeeds(outside);
            UpdateMovementPenalty();
        }

        void UpdateNeeds(bool outside) {
            float dt = Time.deltaTime;
            hunger = Mathf.Clamp(hunger - .014f*dt,0,100);
            thirst = Mathf.Clamp(thirst - .021f*dt,0,100);

            float insulation = TotalInsulation();
            float waterproof = TotalWaterproof();
            bool precip = weather == WeatherKind.Rain || weather == WeatherKind.Snow;
            if (outside && precip) {
                float wetRate = weather == WeatherKind.Rain ? 1.15f : .42f;
                wetness = Mathf.Clamp(wetness + wetRate * Mathf.Max(.08f,1f-waterproof/7f) * dt,0,100);
            } else wetness = Mathf.Clamp(wetness - (outside ? .10f : .55f)*dt,0,100);

            float apparent = outside ? outsideTemperature - wind*.55f : 18f;
            float protection = insulation * 2.35f;
            float wetPenalty = wetness * .055f;
            float targetWarmth = Mathf.Clamp(62f + apparent*1.7f + protection*3.2f - wetPenalty,0,100);
            float speed = targetWarmth < warmth ? .065f : .13f;
            warmth = Mathf.MoveTowards(warmth,targetWarmth,speed*dt*10f);

            Keyboard kb = Keyboard.current;
            bool moving = kb != null && (kb.wKey.isPressed||kb.aKey.isPressed||kb.sKey.isPressed||kb.dKey.isPressed);
            bool sprint = moving && kb != null && (kb.leftShiftKey.isPressed||kb.rightShiftKey.isPressed) && !panelOpen;
            stamina = Mathf.Clamp(stamina + (sprint ? -13f : moving ? 5.5f : 15f)*dt,0,100);

            float damage = 0f;
            if (thirst < 8f) damage += .8f;
            if (hunger < 5f) damage += .35f;
            if (warmth < 12f) damage += .85f;
            health = Mathf.Clamp(health-damage*dt,0,100);
        }

        void UpdateMovementPenalty() {
            if (!player) return;
            float condition = Mathf.Clamp01((stamina*.45f + warmth*.25f + hunger*.15f + thirst*.15f)/100f);
            float wetPenalty = Mathf.Lerp(1f,.86f,wetness/100f);
            player.walkSpeed = baseWalk * Mathf.Lerp(.72f,1f,condition) * wetPenalty;
            player.sprintSpeed = baseSprint * Mathf.Lerp(.58f,1f,condition) * wetPenalty;
            if (stamina < 4f) player.sprintSpeed = player.walkSpeed;
        }

        public bool IsOutside(Vector3 p) {
            bool school = p.x>-18.4f && p.x<18.4f && p.z>-1.4f && p.z<16.1f;
            bool connector = p.x>=18.0f && p.x<22.6f && p.z>4.0f && p.z<10.2f;
            bool hall = p.x>=22.0f && p.x<45.5f && p.z>-1.3f && p.z<16.3f;
            return !(school||connector||hall);
        }

        float TotalInsulation() {
            float v=0; foreach(var kv in equipped){ if(!string.IsNullOrEmpty(kv.Value) && defs.TryGetValue(kv.Value,out var d)) v+=d.insulation; } return v;
        }
        float TotalWaterproof() {
            float v=0; foreach(var kv in equipped){ if(!string.IsNullOrEmpty(kv.Value) && defs.TryGetValue(kv.Value,out var d)) v+=d.waterproof; } return v;
        }

        void ApplyWeather(WeatherKind w,bool silent) {
            weather=w;
            switch(w) {
                case WeatherKind.Clear: outsideTemperature=-2f; wind=1.2f; break;
                case WeatherKind.Overcast: outsideTemperature=-4f; wind=2.8f; break;
                case WeatherKind.Rain: outsideTemperature=3f; wind=4.0f; break;
                case WeatherKind.Snow: outsideTemperature=-6f; wind=3.4f; break;
                case WeatherKind.ColdSnap: outsideTemperature=-17f; wind=5.5f; break;
            }
            RenderSettings.ambientLight = w==WeatherKind.Clear ? new Color(.43f,.46f,.42f) : w==WeatherKind.ColdSnap ? new Color(.25f,.31f,.35f) : new Color(.34f,.37f,.35f);
            if(!silent && player) player.ShowToast("Vädret ändras: "+WeatherLabel()+"  "+Mathf.RoundToInt(outsideTemperature)+"°C",3f);
        }

        string WeatherLabel() {
            switch(weather){case WeatherKind.Clear:return "Klart";case WeatherKind.Overcast:return "Mulet";case WeatherKind.Rain:return "Regn";case WeatherKind.Snow:return "Snöfall";default:return "Köldknäpp";}
        }

        void SetupWeatherFx() {
            rainFx = MakeWeather("V69 RAIN", false);
            snowFx = MakeWeather("V69 SNOW", true);
        }
        ParticleSystem MakeWeather(string name,bool snow) {
            GameObject g=new GameObject(name); g.transform.SetParent(transform,false); g.transform.localPosition=new Vector3(0,8,0);
            var ps=g.AddComponent<ParticleSystem>();
            var main=ps.main; main.loop=true; main.simulationSpace=ParticleSystemSimulationSpace.World; main.maxParticles=snow?900:1200; main.startLifetime=snow?4.2f:1.2f; main.startSpeed=snow?.9f:13f; main.startSize=snow?.10f:.045f; main.startColor=snow?new Color(.92f,.96f,1f,.75f):new Color(.65f,.76f,.82f,.62f); main.gravityModifier=snow?.02f:.1f;
            var em=ps.emission; em.rateOverTime=snow?150:430;
            var sh=ps.shape; sh.shapeType=ParticleSystemShapeType.Box; sh.scale=new Vector3(18,1,18);
            var vel=ps.velocityOverLifetime; vel.enabled=true; vel.space=ParticleSystemSimulationSpace.World; vel.x=new ParticleSystem.MinMaxCurve(snow?-.35f:-1.8f); vel.y=new ParticleSystem.MinMaxCurve(snow?-1.0f:-11f); vel.z=new ParticleSystem.MinMaxCurve(snow?.12f:.4f);
            var r=g.GetComponent<ParticleSystemRenderer>(); r.renderMode=snow?ParticleSystemRenderMode.Billboard:ParticleSystemRenderMode.Stretch; if(!snow){r.lengthScale=3f;r.velocityScale=.08f;}
            g.SetActive(false); return ps;
        }
        void UpdateWeatherFx(bool outside) {
            if(rainFx){ rainFx.transform.position=transform.position+Vector3.up*8f; rainFx.gameObject.SetActive(outside&&weather==WeatherKind.Rain); }
            if(snowFx){ snowFx.transform.position=transform.position+Vector3.up*8f; snowFx.gameObject.SetActive(outside&&(weather==WeatherKind.Snow||weather==WeatherKind.ColdSnap)); }
        }

        Texture2D Icon(string key) {
            if(string.IsNullOrEmpty(key)) return Texture2D.whiteTexture;
            if(iconCache.TryGetValue(key,out var t)&&t) return t;
            t=Resources.Load<Texture2D>("ItemIcons/"+key); iconCache[key]=t; return t?t:Texture2D.whiteTexture;
        }

        void UseItem(ItemDef d) {
            if(!player || player.CountItem(d.name)<=0) return;
            if(d.kind==ItemKind.Food||d.kind==ItemKind.Drink||d.kind==ItemKind.Medical) {
                if(!player.ConsumeItem(d.name,1)) return;
                hunger=Mathf.Clamp(hunger+d.hunger,0,100); thirst=Mathf.Clamp(thirst+d.thirst,0,100); health=Mathf.Clamp(health+d.health,0,100);
                player.ShowToast("Använde: "+d.name,1.8f);
            } else if(d.kind==ItemKind.Clothing) {
                if(equipped.TryGetValue(d.slot,out string now) && string.Equals(now,d.name,StringComparison.OrdinalIgnoreCase)) equipped[d.slot]="";
                else equipped[d.slot]=d.name;
            }
        }

        bool Equipped(string name) { return equipped.Values.Any(v=>string.Equals(v,name,StringComparison.OrdinalIgnoreCase)); }

        void Bar(Rect r,string label,float value,Color c) {
            GUI.Box(r,"");
            Rect fill=new Rect(r.x+2,r.y+2,(r.width-4)*Mathf.Clamp01(value/100f),r.height-4);
            Color old=GUI.color; GUI.color=c; GUI.DrawTexture(fill,Texture2D.whiteTexture); GUI.color=old;
            GUIStyle s=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontSize=12,fontStyle=FontStyle.Bold}; s.normal.textColor=Color.white;
            GUI.Label(r,label+" "+Mathf.CeilToInt(value),s);
        }

        void OnGUI() {
            if(suppressLegacyGui || !player) return;
            GUIStyle small=new GUIStyle(GUI.skin.label){fontSize=13}; small.normal.textColor=new Color(.95f,.93f,.86f);
            GUIStyle title=new GUIStyle(GUI.skin.label){fontSize=18,fontStyle=FontStyle.Bold}; title.normal.textColor=Color.white;
            GUIStyle center=new GUIStyle(small){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold};

            float bx=18, by=112, bw=220;
            GUI.Box(new Rect(bx,by,bw,164),"");
            GUI.Label(new Rect(bx+12,by+8,bw-24,20),"ÖVERLEVNAD",title);
            Bar(new Rect(bx+12,by+34,bw-24,18),"Hälsa",health,new Color(.65f,.22f,.18f));
            Bar(new Rect(bx+12,by+57,bw-24,18),"Mat",hunger,new Color(.66f,.48f,.18f));
            Bar(new Rect(bx+12,by+80,bw-24,18),"Vatten",thirst,new Color(.18f,.46f,.70f));
            Bar(new Rect(bx+12,by+103,bw-24,18),"Värme",warmth,new Color(.78f,.38f,.16f));
            Bar(new Rect(bx+12,by+126,bw-24,18),"Stamina",stamina,new Color(.30f,.62f,.36f));
            GUI.Label(new Rect(bx+12,by+145,bw-24,18),"Våt " + Mathf.RoundToInt(wetness)+"%",small);

            Rect wr=new Rect(Screen.width-250,76,232,80); GUI.Box(wr,"");
            GUI.Label(new Rect(wr.x+12,wr.y+8,210,22),WeatherLabel()+"  "+Mathf.RoundToInt(outsideTemperature)+"°C",title);
            GUI.Label(new Rect(wr.x+12,wr.y+34,210,18),"Vind "+wind.ToString("0.0")+" m/s  •  "+(IsOutside(player.transform.position)?"Utomhus":"Skyddad"),small);
            GUI.Label(new Rect(wr.x+12,wr.y+54,210,18),"Isolering "+TotalInsulation().ToString("0.0")+"  Vattenskydd "+TotalWaterproof().ToString("0.0"),small);

            if(!panelOpen) return;
            Rect panel=new Rect(Screen.width*.5f-430,Screen.height*.5f-300,860,600);
            Color oldC=GUI.color; GUI.color=new Color(.08f,.10f,.09f,.97f); GUI.Box(panel,""); GUI.color=oldC;
            GUI.Label(new Rect(panel.x+22,panel.y+16,500,26),"INVENTARIE + KLÄDER",title);
            GUI.Label(new Rect(panel.x+22,panel.y+44,500,20),"Klicka mat/dryck för att använda. Klicka kläder för att ta på/av.",small);

            Rect list=new Rect(panel.x+20,panel.y+76,535,500);
            float contentH=Mathf.Max(500,Mathf.Ceil(player.Inventory.Count/4f)*118f);
            scroll=GUI.BeginScrollView(list,scroll,new Rect(0,0,510,contentH));
            int i=0;
            foreach(var kv in player.Inventory.OrderBy(x=>x.Key)) {
                if(!defs.TryGetValue(kv.Key,out var d)) d=new ItemDef(kv.Key,"misc",ItemKind.Misc,d:"Okänt föremål");
                int col=i%4,row=i/4; Rect slot=new Rect(col*126,row*118,116,108); GUI.Box(slot,"");
                Texture2D tex=Icon(d.icon); if(tex) GUI.DrawTexture(new Rect(slot.x+26,slot.y+7,64,64),tex,ScaleMode.ScaleToFit,true);
                GUI.Label(new Rect(slot.x+4,slot.y+70,108,18),d.name+(kv.Value>1?" x"+kv.Value:""),center);
                if(Equipped(d.name)) GUI.Label(new Rect(slot.x+4,slot.y+88,108,16),"PÅ",center);
                if(GUI.Button(slot,"",GUIStyle.none)){ selectedItem=d.name; UseItem(d); }
                i++;
            }
            GUI.EndScrollView();

            Rect detail=new Rect(panel.x+575,panel.y+76,260,500); GUI.Box(detail,"");
            GUI.Label(new Rect(detail.x+14,detail.y+12,230,22),"UTRUSTNING",title);
            int sy=44; foreach(string slot in new[]{"Head","Body","Mid","Hands","Feet"}) { string val=equipped[slot]; GUI.Label(new Rect(detail.x+14,detail.y+sy,230,20),SlotLabel(slot)+": "+(string.IsNullOrEmpty(val)?"—":val),small); sy+=24; }
            sy+=8;
            if(!string.IsNullOrEmpty(selectedItem)&&defs.TryGetValue(selectedItem,out var sel)) {
                Texture2D tex=Icon(sel.icon); if(tex) GUI.DrawTexture(new Rect(detail.x+82,detail.y+sy,96,96),tex,ScaleMode.ScaleToFit,true); sy+=102;
                GUI.Label(new Rect(detail.x+14,detail.y+sy,230,22),sel.name,title); sy+=26;
                GUI.Label(new Rect(detail.x+14,detail.y+sy,230,70),sel.description,new GUIStyle(small){wordWrap=true}); sy+=74;
                if(sel.kind==ItemKind.Clothing) GUI.Label(new Rect(detail.x+14,detail.y+sy,230,42),"Isolering +"+sel.insulation.ToString("0.0")+"  •  Vattenskydd +"+sel.waterproof.ToString("0.0"),new GUIStyle(small){wordWrap=true});
            }
            GUI.Label(new Rect(detail.x+14,detail.y+452,230,34),"I / Tab stänger",center);
        }

        string SlotLabel(string s){switch(s){case "Head":return "Huvud";case "Body":return "Ytterplagg";case "Mid":return "Mellanlager";case "Hands":return "Händer";case "Feet":return "Fötter";default:return s;}}
    }
}
