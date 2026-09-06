using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Rosvik.Blackout {
    [DefaultExecutionOrder(1000)]
    public sealed class SurvivalPresentationV70 : MonoBehaviour {
        SurvivalSystemsV69 survival;
        CoziPlayerV57 player;
        bool panelOpen;
        Vector2 scroll;
        string selectedItem = "";

        readonly Dictionary<string,Texture2D> iconCache = new Dictionary<string,Texture2D>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string,SurvivalSystemsV69.ItemDef> defs;
        Dictionary<string,string> equipped;

        FieldInfo legacyInventoryField;
        FieldInfo v69PanelField;
        FieldInfo defsField;
        FieldInfo equippedField;
        MethodInfo useItemMethod;

        ParticleSystem rainFx;
        ParticleSystem snowFx;
        Material rainMaterial;
        Material snowMaterial;
        Texture2D rainTexture;
        Texture2D snowTexture;

        void Awake() {
            player = GetComponent<CoziPlayerV57>();
            if (!player) player = FindFirstObjectByType<CoziPlayerV57>();
            survival = GetComponent<SurvivalSystemsV69>();
            if (!survival) survival = FindFirstObjectByType<SurvivalSystemsV69>();

            legacyInventoryField = typeof(CoziPlayerV57).GetField("inventoryOpen", BindingFlags.Instance | BindingFlags.NonPublic);
            v69PanelField = typeof(SurvivalSystemsV69).GetField("panelOpen", BindingFlags.Instance | BindingFlags.NonPublic);
            defsField = typeof(SurvivalSystemsV69).GetField("defs", BindingFlags.Static | BindingFlags.NonPublic);
            equippedField = typeof(SurvivalSystemsV69).GetField("equipped", BindingFlags.Instance | BindingFlags.NonPublic);
            useItemMethod = typeof(SurvivalSystemsV69).GetMethod("UseItem", BindingFlags.Instance | BindingFlags.NonPublic);

            defs = defsField != null ? defsField.GetValue(null) as Dictionary<string,SurvivalSystemsV69.ItemDef> : null;
            equipped = survival != null && equippedField != null ? equippedField.GetValue(survival) as Dictionary<string,string> : null;

            RemoveLegacyWeatherFx();
            BuildWeatherFx();
        }

        void Update() {
            if (!player || !survival) return;
            Keyboard kb = Keyboard.current;
            if (kb != null && (kb.iKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame)) {
                panelOpen = !panelOpen;
                Cursor.visible = panelOpen;
                if (panelOpen) Cursor.lockState = CursorLockMode.None;
            }
            UpdateWeatherFx();
        }

        void LateUpdate() {
            if (player != null && legacyInventoryField != null) legacyInventoryField.SetValue(player, false);
            if (survival != null && v69PanelField != null) v69PanelField.SetValue(survival, false);
            if (panelOpen && player != null) {
                player.walkSpeed = 0f;
                player.sprintSpeed = 0f;
            }
        }

        void RemoveLegacyWeatherFx() {
            if (!player) return;
            foreach (string n in new[] { "V69 RAIN", "V69 SNOW", "V70 RAIN", "V70 SNOW" }) {
                Transform t = player.transform.Find(n);
                if (t) Destroy(t.gameObject);
            }
        }

        void BuildWeatherFx() {
            rainTexture = MakeRainTexture();
            snowTexture = MakeSnowTexture();
            rainMaterial = MakeParticleMaterial("V70 Rain Material", rainTexture, new Color(.68f,.80f,.88f,.62f));
            snowMaterial = MakeParticleMaterial("V70 Snow Material", snowTexture, new Color(.96f,.98f,1f,.88f));
            rainFx = MakeWeather("V70 RAIN", false, rainMaterial);
            snowFx = MakeWeather("V70 SNOW", true, snowMaterial);
        }

        Material MakeParticleMaterial(string name, Texture2D texture, Color color) {
            bool srp = GraphicsSettings.currentRenderPipeline != null || GraphicsSettings.defaultRenderPipeline != null;
            Shader shader = null;
            if (srp) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (!shader || !shader.isSupported) shader = Shader.Find("Particles/Standard Unlit");
            if (!shader || !shader.isSupported) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (!shader || !shader.isSupported) shader = Shader.Find("Unlit/Transparent");
            if (!shader || !shader.isSupported) shader = Shader.Find("Sprites/Default");
            if (!shader) return null;

            Material m = new Material(shader) { name = name };
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", texture);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", texture);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
            if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", 5f);
            if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", 10f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
            m.SetShaderPassEnabled("ShadowCaster", false);
            return m;
        }

        ParticleSystem MakeWeather(string name, bool snow, Material material) {
            GameObject g = new GameObject(name);
            g.transform.SetParent(transform, false);
            g.transform.localPosition = new Vector3(0, 8f, 0);
            ParticleSystem ps = g.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = snow ? 520 : 800;
            main.startLifetime = snow ? new ParticleSystem.MinMaxCurve(3.8f, 5.2f) : new ParticleSystem.MinMaxCurve(.9f, 1.35f);
            main.startSpeed = snow ? new ParticleSystem.MinMaxCurve(.55f, 1.15f) : new ParticleSystem.MinMaxCurve(9f, 13f);
            main.startSize = snow ? new ParticleSystem.MinMaxCurve(.07f, .16f) : new ParticleSystem.MinMaxCurve(.025f, .045f);
            main.startColor = Color.white;
            main.gravityModifier = snow ? .015f : .08f;

            var emission = ps.emission;
            emission.rateOverTime = snow ? 95f : 260f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(18f, .6f, 18f);
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(snow ? -.45f : -1.6f, snow ? .15f : -.7f);
            vel.y = new ParticleSystem.MinMaxCurve(snow ? -1.2f : -13f, snow ? -.65f : -9f);
            vel.z = new ParticleSystem.MinMaxCurve(snow ? -.15f : -.35f, snow ? .35f : .35f);

            ParticleSystemRenderer r = g.GetComponent<ParticleSystemRenderer>();
            r.renderMode = snow ? ParticleSystemRenderMode.Billboard : ParticleSystemRenderMode.Stretch;
            r.sortMode = ParticleSystemSortMode.Distance;
            r.material = material;
            if (!snow) { r.lengthScale = 4.4f; r.velocityScale = .09f; }
            g.SetActive(false);
            return ps;
        }

        void UpdateWeatherFx() {
            if (!survival || !player) return;
            bool outside = survival.IsOutside(player.transform.position);
            bool rain = outside && survival.weather == SurvivalSystemsV69.WeatherKind.Rain;
            bool snow = outside && (survival.weather == SurvivalSystemsV69.WeatherKind.Snow || survival.weather == SurvivalSystemsV69.WeatherKind.ColdSnap);
            if (rainFx) {
                rainFx.transform.position = player.transform.position + Vector3.up * 8f;
                if (rainFx.gameObject.activeSelf != rain) rainFx.gameObject.SetActive(rain);
            }
            if (snowFx) {
                snowFx.transform.position = player.transform.position + Vector3.up * 8f;
                if (snowFx.gameObject.activeSelf != snow) snowFx.gameObject.SetActive(snow);
            }
        }

        Texture2D MakeSnowTexture() {
            const int n = 24;
            Texture2D t = new Texture2D(n,n,TextureFormat.RGBA32,false) { name = "V70 Snowflake" };
            for (int y=0;y<n;y++) for (int x=0;x<n;x++) {
                float dx=(x-(n-1)*.5f)/((n-1)*.5f), dy=(y-(n-1)*.5f)/((n-1)*.5f);
                float d=Mathf.Sqrt(dx*dx+dy*dy);
                float a=Mathf.Clamp01((1f-d)*2.1f);
                a*=a;
                t.SetPixel(x,y,new Color(1f,1f,1f,a));
            }
            t.Apply(); return t;
        }

        Texture2D MakeRainTexture() {
            const int w=10,h=32;
            Texture2D t = new Texture2D(w,h,TextureFormat.RGBA32,false) { name = "V70 RainStreak" };
            for(int y=0;y<h;y++) for(int x=0;x<w;x++) {
                float dx=Mathf.Abs(x-(w-1)*.5f)/((w-1)*.5f);
                float edge=Mathf.Clamp01(1f-dx);
                float fade=Mathf.Sin((y/(float)(h-1))*Mathf.PI);
                float a=edge*edge*fade*.8f;
                t.SetPixel(x,y,new Color(1f,1f,1f,a));
            }
            t.Apply(); return t;
        }

        Texture2D Icon(string key) {
            if (string.IsNullOrWhiteSpace(key)) return Texture2D.whiteTexture;
            if (iconCache.TryGetValue(key,out Texture2D found) && found) return found;
            Texture2D t = Resources.Load<Texture2D>("ItemIcons/"+key);
            iconCache[key] = t;
            return t ? t : Texture2D.whiteTexture;
        }

        Dictionary<string,string> EquippedMap() {
            if (equipped == null && survival != null && equippedField != null)
                equipped = equippedField.GetValue(survival) as Dictionary<string,string>;
            return equipped;
        }

        void Activate(SurvivalSystemsV69.ItemDef d) {
            if (d == null || survival == null || useItemMethod == null) return;
            useItemMethod.Invoke(survival, new object[] { d });
        }

        bool IsEquipped(string name) {
            var e = EquippedMap();
            return e != null && e.Values.Any(v => string.Equals(v,name,StringComparison.OrdinalIgnoreCase));
        }

        float TotalInsulation() {
            float total=0f; var e=EquippedMap(); if(e==null||defs==null)return 0f;
            foreach(string v in e.Values) if(!string.IsNullOrEmpty(v)&&defs.TryGetValue(v,out var d)) total+=d.insulation;
            return total;
        }
        float TotalWaterproof() {
            float total=0f; var e=EquippedMap(); if(e==null||defs==null)return 0f;
            foreach(string v in e.Values) if(!string.IsNullOrEmpty(v)&&defs.TryGetValue(v,out var d)) total+=d.waterproof;
            return total;
        }

        void OnGUI() {
            if (!player || !survival) return;
            GUI.depth = -1000;

            GUIStyle title = new GUIStyle(GUI.skin.label) { fontSize=20, fontStyle=FontStyle.Bold };
            title.normal.textColor = new Color(.98f,.95f,.86f);
            GUIStyle section = new GUIStyle(title) { fontSize=16 };
            GUIStyle text = new GUIStyle(GUI.skin.label) { fontSize=13, wordWrap=true };
            text.normal.textColor = new Color(.91f,.89f,.82f);
            GUIStyle tiny = new GUIStyle(text) { fontSize=11 };
            GUIStyle centered = new GUIStyle(text) { alignment=TextAnchor.MiddleCenter, fontStyle=FontStyle.Bold };
            GUIStyle countStyle = new GUIStyle(centered) { alignment=TextAnchor.MiddleRight, fontSize=12 };

            if (panelOpen) DrawInventory(title,section,text,tiny,centered,countStyle);
            else DrawHud(title,section,text,tiny);
        }

        void DrawHud(GUIStyle title, GUIStyle section, GUIStyle text, GUIStyle tiny) {
            Rect left = new Rect(16,16,360,226);
            Panel(left,new Color(.055f,.07f,.065f,.94f));
            GUI.Label(new Rect(left.x+14,left.y+10,220,24),"THE DARK QUIET",title);
            GUI.Label(new Rect(left.x+14,left.y+37,left.width-28,36),"Mål: "+player.objective,text);

            float y=80f;
            StatBar(new Rect(left.x+14,left.y+y,left.width-28,18),"HÄLSA",survival.health,new Color(.66f,.22f,.18f)); y+=24;
            StatBar(new Rect(left.x+14,left.y+y,left.width-28,18),"MAT",survival.hunger,new Color(.67f,.48f,.17f)); y+=24;
            StatBar(new Rect(left.x+14,left.y+y,left.width-28,18),"VATTEN",survival.thirst,new Color(.20f,.48f,.72f)); y+=24;
            StatBar(new Rect(left.x+14,left.y+y,left.width-28,18),"VÄRME",survival.warmth,new Color(.83f,.42f,.18f)); y+=24;
            StatBar(new Rect(left.x+14,left.y+y,left.width-28,18),"STAMINA",survival.stamina,new Color(.30f,.61f,.36f));
            GUI.Label(new Rect(left.x+14,left.y+204,left.width-28,18),"Våt "+Mathf.RoundToInt(survival.wetness)+"%   •   I/Tab inventarie",tiny);

            Rect right = new Rect(Screen.width-286,16,270,154);
            Panel(right,new Color(.055f,.07f,.065f,.94f));
            GUI.Label(new Rect(right.x+14,right.y+10,240,23),WeatherLabel()+"   "+Mathf.RoundToInt(survival.outsideTemperature)+"°C",section);
            GUI.Label(new Rect(right.x+14,right.y+38,240,20),"Vind "+survival.wind.ToString("0.0")+" m/s   •   "+(survival.IsOutside(player.transform.position)?"UTOMHUS":"SKYDDAD"),text);
            GUI.Label(new Rect(right.x+14,right.y+62,240,18),"Isolering "+TotalInsulation().ToString("0.0")+"   Vattenskydd "+TotalWaterproof().ToString("0.0"),tiny);
            if (player.HasItem("Ficklampa")) {
                GUI.Label(new Rect(right.x+14,right.y+91,240,19),"Ficklampa: "+(player.FlashlightOn?"PÅ":"AV"),text);
                GUI.Label(new Rect(right.x+14,right.y+113,240,18),"Batteri "+Mathf.CeilToInt(player.flashlightBattery)+"%   •   F",tiny);
            } else GUI.Label(new Rect(right.x+14,right.y+96,240,19),"Ingen ficklampa",text);
        }

        void DrawInventory(GUIStyle title, GUIStyle section, GUIStyle text, GUIStyle tiny, GUIStyle centered, GUIStyle countStyle) {
            Color old=GUI.color;
            GUI.color=new Color(.015f,.02f,.018f,.86f);
            GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);
            GUI.color=old;

            float pw=Mathf.Min(1100f,Screen.width-48f);
            float ph=Mathf.Min(720f,Screen.height-48f);
            Rect panel=new Rect((Screen.width-pw)*.5f,(Screen.height-ph)*.5f,pw,ph);
            Panel(panel,new Color(.055f,.07f,.065f,.985f));

            GUI.Label(new Rect(panel.x+24,panel.y+18,pw-48,28),"INVENTARIE",title);
            GUI.Label(new Rect(panel.x+24,panel.y+49,pw-48,20),"Klicka mat/dryck/förband för att använda. Klicka kläder för att ta på eller av.   I / Tab stänger.",text);

            float gap=18f;
            float rightW=Mathf.Clamp(pw*.31f,290f,350f);
            float leftW=pw-rightW-gap-48f;
            Rect left=new Rect(panel.x+24,panel.y+82,leftW,ph-106);
            Rect right=new Rect(left.xMax+gap,panel.y+82,rightW,ph-106);
            Panel(left,new Color(.09f,.11f,.10f,.96f));
            Panel(right,new Color(.09f,.11f,.10f,.96f));

            float gridPad=14f;
            float viewW=left.width-gridPad*2;
            int cols=Mathf.Clamp(Mathf.FloorToInt((viewW+10f)/142f),3,5);
            float spacing=10f;
            float slotW=(viewW-(cols-1)*spacing)/cols;
            float slotH=136f;
            int count=player.Inventory.Count;
            int rows=Mathf.CeilToInt(count/(float)cols);
            float contentH=Mathf.Max(left.height-28f,rows*(slotH+spacing));
            Rect view=new Rect(left.x+gridPad,left.y+gridPad,left.width-gridPad*2,left.height-gridPad*2);
            scroll=GUI.BeginScrollView(view,scroll,new Rect(0,0,view.width-18f,contentH));

            int i=0;
            foreach(var kv in player.Inventory.OrderBy(x=>x.Key)) {
                SurvivalSystemsV69.ItemDef d = null;
                if(defs!=null) defs.TryGetValue(kv.Key,out d);
                if(d==null) d=new SurvivalSystemsV69.ItemDef(kv.Key,"misc",SurvivalSystemsV69.ItemKind.Misc,d:"Okänt föremål");
                int col=i%cols,row=i/cols;
                Rect slot=new Rect(col*(slotW+spacing),row*(slotH+spacing),slotW,slotH);
                Color tint=IsEquipped(d.name)?new Color(.35f,.48f,.38f,1f):Color.white;
                GUI.color=tint;
                bool clicked=GUI.Button(slot,GUIContent.none);
                GUI.color=Color.white;

                float iconSize=Mathf.Min(76f,slot.width-26f);
                Texture2D tex=Icon(d.icon);
                GUI.DrawTexture(new Rect(slot.x+(slot.width-iconSize)*.5f,slot.y+10,iconSize,iconSize),tex,ScaleMode.ScaleToFit,true);
                GUI.Label(new Rect(slot.x+8,slot.y+90,slot.width-16,22),d.name,centered);
                if(kv.Value>1) GUI.Label(new Rect(slot.x+8,slot.y+8,slot.width-16,18),"x"+kv.Value,countStyle);
                GUI.Label(new Rect(slot.x+8,slot.y+113,slot.width-16,17),KindLabel(d.kind)+(IsEquipped(d.name)?"  •  PÅ":""),tiny);
                if(clicked){selectedItem=d.name;Activate(d);}
                i++;
            }
            GUI.EndScrollView();

            float ry=14f;
            GUI.Label(new Rect(right.x+16,right.y+ry,right.width-32,22),"UTRUSTNING",section); ry+=34;
            var e=EquippedMap();
            foreach(string slot in new[]{"Head","Body","Mid","Hands","Feet"}) {
                string val=(e!=null&&e.TryGetValue(slot,out string found))?found:"";
                Rect rowRect=new Rect(right.x+14,right.y+ry,right.width-28,31);
                Panel(rowRect,new Color(.12f,.14f,.13f,.95f));
                GUI.Label(new Rect(rowRect.x+9,rowRect.y+6,86,20),SlotLabel(slot),tiny);
                GUI.Label(new Rect(rowRect.x+96,rowRect.y+5,rowRect.width-104,21),string.IsNullOrEmpty(val)?"—":val,text);
                ry+=37;
            }
            GUI.Label(new Rect(right.x+16,right.y+ry+3,right.width-32,18),"Isolering "+TotalInsulation().ToString("0.0")+"   •   Vattenskydd "+TotalWaterproof().ToString("0.0"),tiny);
            ry+=34;

            GUI.Label(new Rect(right.x+16,right.y+ry,right.width-32,22),"FÖREMÅL",section); ry+=30;
            if(!string.IsNullOrEmpty(selectedItem) && defs!=null && defs.TryGetValue(selectedItem,out var sel)) {
                float icon=Mathf.Min(104f,right.width-48f);
                GUI.DrawTexture(new Rect(right.x+(right.width-icon)*.5f,right.y+ry,icon,icon),Icon(sel.icon),ScaleMode.ScaleToFit,true); ry+=icon+8;
                GUI.Label(new Rect(right.x+16,right.y+ry,right.width-32,24),sel.name,section); ry+=26;
                GUI.Label(new Rect(right.x+16,right.y+ry,right.width-32,72),sel.description,text); ry+=76;
                string effect=EffectText(sel);
                if(!string.IsNullOrEmpty(effect)) GUI.Label(new Rect(right.x+16,right.y+ry,right.width-32,46),effect,tiny);
            } else {
                GUI.Label(new Rect(right.x+16,right.y+ry,right.width-32,56),"Välj ett föremål för att se vad det gör.",text);
            }
        }

        void Panel(Rect r, Color c) {
            Color old=GUI.color; GUI.color=c; GUI.Box(r,""); GUI.color=old;
        }

        void StatBar(Rect r,string label,float value,Color fillColor) {
            Panel(r,new Color(.10f,.11f,.10f,.96f));
            Rect f=new Rect(r.x+2,r.y+2,(r.width-4)*Mathf.Clamp01(value/100f),r.height-4);
            Color old=GUI.color; GUI.color=fillColor; GUI.DrawTexture(f,Texture2D.whiteTexture); GUI.color=old;
            GUIStyle s=new GUIStyle(GUI.skin.label){fontSize=11,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter}; s.normal.textColor=Color.white;
            GUI.Label(r,label+"  "+Mathf.CeilToInt(value),s);
        }

        string WeatherLabel() {
            switch(survival.weather) {
                case SurvivalSystemsV69.WeatherKind.Clear:return "KLART";
                case SurvivalSystemsV69.WeatherKind.Overcast:return "MULET";
                case SurvivalSystemsV69.WeatherKind.Rain:return "REGN";
                case SurvivalSystemsV69.WeatherKind.Snow:return "SNÖFALL";
                default:return "KÖLDKNÄPP";
            }
        }
        string KindLabel(SurvivalSystemsV69.ItemKind k) {
            switch(k){case SurvivalSystemsV69.ItemKind.Food:return "MAT";case SurvivalSystemsV69.ItemKind.Drink:return "DRYCK";case SurvivalSystemsV69.ItemKind.Medical:return "VÅRD";case SurvivalSystemsV69.ItemKind.Clothing:return "KLÄDER";case SurvivalSystemsV69.ItemKind.Tool:return "VERKTYG";case SurvivalSystemsV69.ItemKind.Material:return "MATERIAL";default:return "ÖVRIGT";}
        }
        string SlotLabel(string s){switch(s){case "Head":return "HUVUD";case "Body":return "YTTER";case "Mid":return "MELLAN";case "Hands":return "HÄNDER";case "Feet":return "FÖTTER";default:return s.ToUpperInvariant();}}
        string EffectText(SurvivalSystemsV69.ItemDef d) {
            if(d.kind==SurvivalSystemsV69.ItemKind.Clothing) return "Isolering +"+d.insulation.ToString("0.0")+"   •   Vattenskydd +"+d.waterproof.ToString("0.0");
            List<string> a=new List<string>();
            if(d.hunger!=0)a.Add("Mat "+(d.hunger>0?"+":"")+d.hunger.ToString("0"));
            if(d.thirst!=0)a.Add("Vatten "+(d.thirst>0?"+":"")+d.thirst.ToString("0"));
            if(d.health!=0)a.Add("Hälsa "+(d.health>0?"+":"")+d.health.ToString("0"));
            return string.Join("   •   ",a);
        }

        void OnDestroy() {
            if(rainMaterial) Destroy(rainMaterial);
            if(snowMaterial) Destroy(snowMaterial);
            if(rainTexture) Destroy(rainTexture);
            if(snowTexture) Destroy(snowTexture);
        }
    }
}
