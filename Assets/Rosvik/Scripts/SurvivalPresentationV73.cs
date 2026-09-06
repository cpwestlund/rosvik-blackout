using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rosvik.Blackout {
    [DefaultExecutionOrder(3000)]
    public sealed class SurvivalPresentationV73 : MonoBehaviour {
        sealed class ExtraMeta {
            public string icon, kind, description;
            public float weight;
            public ExtraMeta(string i,string k,string d,float w){icon=i;kind=k;description=d;weight=w;}
        }

        static readonly Dictionary<string,ExtraMeta> extras = new Dictionary<string,ExtraMeta>(StringComparer.OrdinalIgnoreCase) {
            {"Tygbit",new ExtraMeta("bandage","MATERIAL","Tyg från förråd och gamla textilier. Används vid tillverkning.",.12f)},
            {"Metallskrot",new ExtraMeta("multitool","MATERIAL","Små metalldelar som går att använda vid reparationer och crafting.",.55f)},
            {"Träspill",new ExtraMeta("misc","MATERIAL","Torrt träspill. Användbart till enkla konstruktioner och bränsle.",.35f)},
            {"Bränsle",new ExtraMeta("lighter","BRÄNSLE","Ett litet bränslepaket för stormkök och provisorisk värme.",.65f)},
            {"Improviserad fackla",new ExtraMeta("flashlight","VERKTYG","En enkel fackla. Tryck T för att tända eller släcka den.",.70f)},
            {"Varm soppa",new ExtraMeta("soup","MAT","Uppvärmd soppa. Ger mat, vätska och en snabb värmeeffekt.",.48f)}
        };

        static readonly Dictionary<string,float> weights = new Dictionary<string,float>(StringComparer.OrdinalIgnoreCase) {
            {"Ficklampa",.38f},{"Batterier",.18f},{"Vattenflaska",1.05f},{"Läsk",.38f},{"Sportdryck",.58f},
            {"Energibar",.09f},{"Konservburk",.46f},{"Soppa",.52f},{"Kex",.22f},{"Choklad",.10f},{"Äpple",.16f},
            {"Förband",.08f},{"Tejp",.16f},{"Sporttejp",.12f},{"Tändare",.05f},{"Multiverktyg",.28f},{"Nyckelknippa",.22f},
            {"Säkring",.08f},{"Penna",.01f},{"Vinterjacka",1.85f},{"Regnjacka",.72f},{"Ulltröja",.66f},{"Hoodie",.78f},
            {"Mössa",.14f},{"Handskar",.18f},{"Kängor",1.65f}
        };

        public float backpackCapacityKg = 34f;
        public float discoveryRange = 4.3f;

        CoziPlayerV57 player;
        SurvivalSystemsV69 survival;
        SurvivalProgressionV71 progression;
        SurvivalCraftingV72 crafting;
        bool panelOpen;
        string selectedItem="";
        Vector2 scroll;
        CozyInteractableV57 nearby;
        float nearbyDistance;
        float nextInteractableScan;
        Light sun;

        FieldInfo defsField, equippedField;
        MethodInfo useItemMethod;
        Dictionary<string,SurvivalSystemsV69.ItemDef> defs;
        Dictionary<string,string> equipped;
        readonly Dictionary<string,Texture2D> iconCache=new Dictionary<string,Texture2D>(StringComparer.OrdinalIgnoreCase);

        void Awake(){
            player=GetComponent<CoziPlayerV57>();if(!player)player=FindFirstObjectByType<CoziPlayerV57>();
            survival=GetComponent<SurvivalSystemsV69>();if(!survival)survival=FindFirstObjectByType<SurvivalSystemsV69>();
            progression=GetComponent<SurvivalProgressionV71>();if(!progression)progression=FindFirstObjectByType<SurvivalProgressionV71>();
            crafting=GetComponent<SurvivalCraftingV72>();if(!crafting)crafting=FindFirstObjectByType<SurvivalCraftingV72>();
            if(player)player.suppressLegacyGui=true;
            if(survival)survival.suppressLegacyGui=true;

            defsField=typeof(SurvivalSystemsV69).GetField("defs",BindingFlags.Static|BindingFlags.NonPublic);
            equippedField=typeof(SurvivalSystemsV69).GetField("equipped",BindingFlags.Instance|BindingFlags.NonPublic);
            useItemMethod=typeof(SurvivalSystemsV69).GetMethod("UseItem",BindingFlags.Instance|BindingFlags.NonPublic);
            defs=defsField!=null?defsField.GetValue(null) as Dictionary<string,SurvivalSystemsV69.ItemDef>:null;
            equipped=survival!=null&&equippedField!=null?equippedField.GetValue(survival) as Dictionary<string,string>:null;

            foreach(Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))if(l&&l.type==LightType.Directional){sun=l;if(l.name.ToLowerInvariant().Contains("sun"))break;}
            NormalizeCabinetSwings();
        }

        void Update(){
            if(!player||!survival)return;
            Keyboard kb=Keyboard.current;
            if(kb!=null&&(kb.iKey.wasPressedThisFrame||kb.tabKey.wasPressedThisFrame)){
                panelOpen=!panelOpen;
                Cursor.visible=panelOpen;
                if(panelOpen)Cursor.lockState=CursorLockMode.None;
            }
            if(Time.time>=nextInteractableScan){nextInteractableScan=Time.time+.10f;ScanNearby();}
        }

        void LateUpdate(){
            // Keep the world darker/more muted than the prototype lighting.
            RenderSettings.ambientLight*=.82f;
            if(sun)sun.intensity*=.84f;

            if(!player)return;
            float kg=CurrentWeight();
            if(kg>backpackCapacityKg){
                float over=Mathf.Clamp01((kg-backpackCapacityKg)/12f);
                player.walkSpeed*=Mathf.Lerp(.82f,.55f,over);
                player.sprintSpeed*=Mathf.Lerp(.70f,.35f,over);
            }
        }

        void NormalizeCabinetSwings(){
            foreach(CozyInteractableV57 x in FindObjectsByType<CozyInteractableV57>(FindObjectsSortMode.None)){
                if(!x||x.kind!=CozyInteractableV57.Kind.Cabinet)continue;
                x.animationTime=Mathf.Max(.30f,x.animationTime);
                Vector3 a=x.openEuler,b=x.openEuler2;
                NormalizeLeaf(x.movingPart,ref a,x.closedEuler);
                NormalizeLeaf(x.movingPart2,ref b,x.closedEuler2);
                x.openEuler=a;x.openEuler2=b;
            }
        }

        void NormalizeLeaf(Transform hinge,ref Vector3 open,Vector3 closed){
            if(!hinge||hinge.childCount==0)return;
            // Lids use X rotation and are left alone. Side-hinged cabinet doors use local Y.
            if(Mathf.Abs(open.y)<35f||Mathf.Abs(open.y)<Mathf.Abs(open.x))return;
            Transform leaf=hinge.GetChild(0);
            float extension=leaf.localPosition.x;
            if(Mathf.Abs(extension)<.035f)return;
            float angle=Mathf.Clamp(Mathf.Abs(open.y),88f,102f);
            open=new Vector3(closed.x,-Mathf.Sign(extension)*angle,closed.z);
        }

        void ScanNearby(){
            nearby=null;nearbyDistance=float.MaxValue;
            if(!player)return;
            Vector3 here=player.transform.position;
            foreach(CozyInteractableV57 x in FindObjectsByType<CozyInteractableV57>(FindObjectsSortMode.None)){
                if(!x||!x.IsAvailable)continue;
                Vector3 d=x.transform.position-here;d.y=0;
                float dist=d.magnitude;
                if(dist<=discoveryRange&&dist<nearbyDistance){nearby=x;nearbyDistance=dist;}
            }
        }

        Dictionary<string,string> Equipped(){
            if(equipped==null&&survival!=null&&equippedField!=null)equipped=equippedField.GetValue(survival) as Dictionary<string,string>;
            return equipped;
        }
        bool IsEquipped(string n){var e=Equipped();return e!=null&&e.Values.Any(v=>string.Equals(v,n,StringComparison.OrdinalIgnoreCase));}
        float TotalInsulation(){float v=0;var e=Equipped();if(e==null||defs==null)return 0;foreach(string n in e.Values)if(!string.IsNullOrEmpty(n)&&defs.TryGetValue(n,out var d))v+=d.insulation;return v;}
        float TotalWaterproof(){float v=0;var e=Equipped();if(e==null||defs==null)return 0;foreach(string n in e.Values)if(!string.IsNullOrEmpty(n)&&defs.TryGetValue(n,out var d))v+=d.waterproof;return v;}

        float ItemWeight(string item){if(extras.TryGetValue(item,out var x))return x.weight;if(weights.TryGetValue(item,out float w))return w;return .25f;}
        float CurrentWeight(){float total=0;if(player==null)return 0;foreach(var kv in player.Inventory)total+=ItemWeight(kv.Key)*kv.Value;return total;}

        string IconKey(string item){if(defs!=null&&defs.TryGetValue(item,out var d))return d.icon;if(extras.TryGetValue(item,out var x))return x.icon;return "misc";}
        string Kind(string item){if(defs!=null&&defs.TryGetValue(item,out var d))return KindLabel(d.kind);if(extras.TryGetValue(item,out var x))return x.kind;return "ÖVRIGT";}
        string Description(string item){if(defs!=null&&defs.TryGetValue(item,out var d))return d.description;if(extras.TryGetValue(item,out var x))return x.description;return "Ett föremål du har hittat.";}
        Texture2D Icon(string key){if(iconCache.TryGetValue(key,out var t)&&t)return t;t=Resources.Load<Texture2D>("ItemIcons/"+key);iconCache[key]=t;return t?t:Texture2D.whiteTexture;}

        bool CanActivate(string item){
            if(string.Equals(item,"Varm soppa",StringComparison.OrdinalIgnoreCase))return true;
            if(defs==null||!defs.TryGetValue(item,out var d))return false;
            return d.kind==SurvivalSystemsV69.ItemKind.Food||d.kind==SurvivalSystemsV69.ItemKind.Drink||d.kind==SurvivalSystemsV69.ItemKind.Medical||d.kind==SurvivalSystemsV69.ItemKind.Clothing;
        }
        string ActionLabel(string item){
            if(string.Equals(item,"Varm soppa",StringComparison.OrdinalIgnoreCase))return "ÄT";
            if(defs==null||!defs.TryGetValue(item,out var d))return "";
            if(d.kind==SurvivalSystemsV69.ItemKind.Clothing)return IsEquipped(item)?"TA AV":"TA PÅ";
            if(d.kind==SurvivalSystemsV69.ItemKind.Food)return "ÄT";
            if(d.kind==SurvivalSystemsV69.ItemKind.Drink)return "DRICK";
            if(d.kind==SurvivalSystemsV69.ItemKind.Medical)return "ANVÄND";
            return "";
        }
        void ActivateSelected(){
            if(string.IsNullOrEmpty(selectedItem)||player.CountItem(selectedItem)<=0)return;
            if(string.Equals(selectedItem,"Varm soppa",StringComparison.OrdinalIgnoreCase)){
                if(player.ConsumeItem(selectedItem,1)){survival.hunger=Mathf.Clamp(survival.hunger+40,0,100);survival.thirst=Mathf.Clamp(survival.thirst+18,0,100);survival.warmth=Mathf.Clamp(survival.warmth+24,0,100);player.ShowToast("Åt varm soppa",2f);}return;
            }
            if(defs!=null&&defs.TryGetValue(selectedItem,out var d)&&useItemMethod!=null)useItemMethod.Invoke(survival,new object[]{d});
        }

        void OnGUI(){
            if(!player||!survival)return;
            GUI.depth=-6000;
            GUIStyle title=Style(19,FontStyle.Bold,new Color(.91f,.91f,.84f),TextAnchor.MiddleLeft,false);
            GUIStyle heading=Style(14,FontStyle.Bold,new Color(.85f,.86f,.79f),TextAnchor.MiddleLeft,false);
            GUIStyle text=Style(12,FontStyle.Normal,new Color(.77f,.79f,.74f),TextAnchor.UpperLeft,true);
            GUIStyle tiny=Style(10,FontStyle.Normal,new Color(.62f,.65f,.61f),TextAnchor.MiddleLeft,false);
            GUIStyle center=Style(11,FontStyle.Bold,new Color(.88f,.89f,.83f),TextAnchor.MiddleCenter,true);
            if(panelOpen)DrawBackpack(title,heading,text,tiny,center);else DrawHud(heading,text,tiny,center);
        }

        void DrawHud(GUIStyle heading,GUIStyle text,GUIStyle tiny,GUIStyle center){
            // Objective is now a small quiet strip instead of a large card.
            Rect obj=new Rect(14,14,Mathf.Min(410,Screen.width*.36f),48);Panel(obj,new Color(.025f,.032f,.030f,.83f),new Color(.18f,.21f,.19f,.75f));
            GUI.Label(new Rect(obj.x+12,obj.y+6,145,18),"THE DARK QUIET",heading);
            GUI.Label(new Rect(obj.x+12,obj.y+24,obj.width-24,18),player.objective,tiny);

            // Small status cluster in the lower-left. Bars are intentionally thin.
            float sx=16,sy=Screen.height-102,sw=168;
            Panel(new Rect(sx,sy,sw,86),new Color(.025f,.032f,.030f,.78f),new Color(.15f,.18f,.16f,.65f));
            MiniStat(new Rect(sx+10,sy+9,sw-20,9),"H",survival.health,new Color(.60f,.20f,.18f),tiny); 
            MiniStat(new Rect(sx+10,sy+23,sw-20,9),"M",survival.hunger,new Color(.57f,.42f,.17f),tiny);
            MiniStat(new Rect(sx+10,sy+37,sw-20,9),"V",survival.thirst,new Color(.18f,.42f,.63f),tiny);
            MiniStat(new Rect(sx+10,sy+51,sw-20,9),"°",survival.warmth,new Color(.70f,.35f,.15f),tiny);
            MiniStat(new Rect(sx+10,sy+65,sw-20,9),"S",survival.stamina,new Color(.28f,.53f,.31f),tiny);

            string topRight=WeatherLabel()+"  "+Mathf.RoundToInt(survival.outsideTemperature)+"°";
            if(progression)topRight+="    DAG "+progression.day+"  "+progression.TimeLabel;
            Rect wr=new Rect(Screen.width-330,14,316,34);Panel(wr,new Color(.025f,.032f,.030f,.82f),new Color(.15f,.18f,.16f,.62f));
            GUI.Label(new Rect(wr.x+10,wr.y+6,wr.width-20,20),topRight,heading);

            if(player.HasItem("Ficklampa")){
                Rect fr=new Rect(Screen.width-190,Screen.height-48,176,32);Panel(fr,new Color(.025f,.032f,.030f,.72f),new Color(.15f,.18f,.16f,.55f));
                GUI.Label(new Rect(fr.x+9,fr.y+5,fr.width-18,20),"FICKLAMPA "+(player.FlashlightOn?"PÅ":"AV")+"  "+Mathf.CeilToInt(player.flashlightBattery)+"%",tiny);
            }

            DrawInteractionPrompt(heading,tiny,center);
        }

        void DrawInteractionPrompt(GUIStyle heading,GUIStyle tiny,GUIStyle center){
            if(!nearby||nearbyDistance>discoveryRange)return;
            float r=Mathf.Max(.65f,nearby.radius);
            bool inReach=nearbyDistance<=r;
            string verb;
            if(nearby.kind==CozyInteractableV57.Kind.Loot)verb="PLOCKA UPP / SÖK";
            else if(nearby.kind==CozyInteractableV57.Kind.Cabinet)verb=nearby.IsOpen?"STÄNG":"ÖPPNA / SÖK";
            else verb=nearby.IsOpen?"STÄNG":"ÖPPNA";

            Camera cam=Camera.main;
            if(cam){
                Vector3 wp=nearby.transform.position+Vector3.up*1.35f;
                Vector3 sp=cam.WorldToScreenPoint(wp);
                if(sp.z>0){
                    float y=Screen.height-sp.y;
                    Rect tag=new Rect(sp.x-58,y-16,116,24);
                    Panel(tag,new Color(.03f,.04f,.037f,.82f),new Color(.48f,.50f,.42f,.72f));
                    GUI.Label(new Rect(tag.x+4,tag.y+2,tag.width-8,20),inReach?"E  "+verb:"SÖKBART",center);
                }
            }

            if(inReach){
                Rect p=new Rect(Screen.width*.5f-205,Screen.height-66,410,40);
                Panel(p,new Color(.025f,.032f,.030f,.92f),new Color(.48f,.50f,.42f,.82f));
                GUI.Label(new Rect(p.x+12,p.y+5,p.width-24,29),"E   "+verb+"   "+nearby.displayName,center);
            }
        }

        void DrawBackpack(GUIStyle title,GUIStyle heading,GUIStyle text,GUIStyle tiny,GUIStyle center){
            Color old=GUI.color;GUI.color=new Color(.005f,.008f,.007f,.88f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=old;

            float margin=Mathf.Max(18,Screen.width*.018f),gap=18;
            float leftW=Mathf.Clamp(Screen.width*.365f,470,620);
            float top=54,bottom=52;
            Rect left=new Rect(margin,top,leftW,Screen.height-top-bottom);
            Rect right=new Rect(left.xMax+gap,top,Screen.width-left.xMax-gap-margin,Screen.height-top-bottom);
            Panel(left,new Color(.025f,.032f,.030f,.975f),new Color(.30f,.33f,.29f,.92f));
            Panel(right,new Color(.025f,.032f,.030f,.965f),new Color(.30f,.33f,.29f,.92f));

            GUI.Label(new Rect(left.x+16,left.y+13,220,28),"RYGGSÄCK",title);
            float kg=CurrentWeight();
            GUIStyle kgStyle=Style(11,FontStyle.Bold,kg>backpackCapacityKg?new Color(.86f,.42f,.30f):new Color(.68f,.70f,.64f),TextAnchor.MiddleRight,false);
            GUI.Label(new Rect(left.x+left.width-190,left.y+15,170,22),kg.ToString("0.0")+" / "+backpackCapacityKg.ToString("0")+" KG",kgStyle);
            Line(new Rect(left.x+16,left.y+49,left.width-32,1),new Color(.22f,.25f,.22f,.9f));

            // Fixed backpack grid: empty cells remain visible, like a real inventory container.
            int cols=6,rowsVisible=5;
            float pad=16,space=8;
            float gridW=left.width-pad*2;
            float cell=(gridW-space*(cols-1))/cols;
            cell=Mathf.Min(cell,84f);
            float gridStartY=left.y+67;
            List<KeyValuePair<string,int>> items=player.Inventory.OrderBy(x=>Kind(x.Key)).ThenBy(x=>x.Key).ToList();
            int totalSlots=Mathf.Max(cols*rowsVisible,Mathf.CeilToInt(items.Count/(float)cols)*cols);
            float contentH=Mathf.CeilToInt(totalSlots/(float)cols)*(cell+space);
            Rect view=new Rect(left.x+pad,gridStartY,left.width-pad*2,left.height-225);
            scroll=GUI.BeginScrollView(view,scroll,new Rect(0,0,view.width-18,Mathf.Max(view.height-4,contentH)));
            Vector2 mp=Event.current.mousePosition;
            string hover="";
            for(int i=0;i<totalSlots;i++){
                int c=i%cols,r=i/cols;Rect slot=new Rect(c*(cell+space),r*(cell+space),cell,cell);
                bool occupied=i<items.Count;
                bool selected=occupied&&string.Equals(selectedItem,items[i].Key,StringComparison.OrdinalIgnoreCase);
                Fill(slot,selected?new Color(.18f,.21f,.18f,.98f):new Color(.075f,.085f,.08f,.98f));
                Border(slot,selected?new Color(.58f,.60f,.49f,.95f):new Color(.25f,.28f,.25f,.85f),selected?2:1);
                if(occupied){
                    var kv=items[i];Texture2D tex=Icon(IconKey(kv.Key));float ic=cell-18;
                    GUI.DrawTexture(new Rect(slot.x+9,slot.y+8,ic,ic),tex,ScaleMode.ScaleToFit,true);
                    if(kv.Value>1){GUIStyle cs=Style(10,FontStyle.Bold,new Color(.92f,.91f,.82f),TextAnchor.LowerRight,false);GUI.Label(new Rect(slot.x+5,slot.y+cell-22,cell-10,17),"x"+kv.Value,cs);}
                    if(IsEquipped(kv.Key)){GUIStyle eq=Style(9,FontStyle.Bold,new Color(.72f,.81f,.65f),TextAnchor.UpperLeft,false);GUI.Label(new Rect(slot.x+4,slot.y+2,cell-8,14),"PÅ",eq);}
                    if(slot.Contains(mp)){hover=kv.Key;}
                    if(GUI.Button(slot,GUIContent.none,GUIStyle.none))selectedItem=kv.Key;
                }
            }
            GUI.EndScrollView();

            float infoY=left.y+left.height-143;
            string helper=!string.IsNullOrEmpty(hover)?hover:(!string.IsNullOrEmpty(selectedItem)?selectedItem:"Håll musen över ett föremål för att läsa om det.");
            GUI.Label(new Rect(left.x+16,infoY,left.width-32,22),helper,heading);
            GUI.Label(new Rect(left.x+16,infoY+28,left.width-32,38),"Klicka ett föremål för att välja det. Mat, dryck, vård och kläder används i detaljrutan.",text);
            GUI.Label(new Rect(left.x+16,infoY+73,left.width-32,18),"I / TAB   STÄNG",tiny);

            // Right side deliberately feels like field notes rather than another grid.
            GUI.Label(new Rect(right.x+24,right.y+18,right.width-48,30),"FÄLTANTECKNINGAR",title);
            Line(new Rect(right.x+24,right.y+56,right.width-48,1),new Color(.22f,.25f,.22f,.9f));

            if(string.IsNullOrEmpty(selectedItem)||player.CountItem(selectedItem)<=0){
                GUI.Label(new Rect(right.x+26,right.y+82,right.width-52,150),
                    "Sök klassrum, förråd, personalrum och sporthall.\n\nLoot är platsbaserat: mat finns där människor åt, verktyg i serviceutrymmen, kläder i skåp och medicin i första hjälpen-förvaring.\n\nNär något går att söka visas en markering i världen och ett tydligt E-kommando när du är nära.",text);
                DrawEquipment(right,heading,tiny,text,right.y+280);
                return;
            }

            Texture2D icon=Icon(IconKey(selectedItem));
            float ic=Mathf.Min(126,right.width*.18f);
            GUI.DrawTexture(new Rect(right.x+28,right.y+84,ic,ic),icon,ScaleMode.ScaleToFit,true);
            float tx=right.x+28+ic+22;
            GUI.Label(new Rect(tx,right.y+87,right.xMax-tx-24,27),selectedItem,title);
            GUI.Label(new Rect(tx,right.y+118,right.xMax-tx-24,20),Kind(selectedItem)+"   •   "+ItemWeight(selectedItem).ToString("0.00")+" KG/ST",tiny);
            GUI.Label(new Rect(tx,right.y+148,right.xMax-tx-24,86),Description(selectedItem),text);

            string action=ActionLabel(selectedItem);
            if(!string.IsNullOrEmpty(action)&&CanActivate(selectedItem)){
                Rect ab=new Rect(tx,right.y+238,Mathf.Min(190,right.xMax-tx-24),38);
                if(GUI.Button(ab,action))ActivateSelected();
            } else GUI.Label(new Rect(tx,right.y+240,right.xMax-tx-24,38),"Används i världen eller vid crafting.",tiny);

            DrawEquipment(right,heading,tiny,text,right.y+330);
        }

        void DrawEquipment(Rect right,GUIStyle heading,GUIStyle tiny,GUIStyle text,float y){
            GUI.Label(new Rect(right.x+26,y,right.width-52,22),"UTRUSTNING",heading);y+=32;
            var e=Equipped();
            string[] slots={"Head","Body","Mid","Hands","Feet"};
            foreach(string s in slots){
                string val=e!=null&&e.TryGetValue(s,out string v)?v:"";
                Rect row=new Rect(right.x+26,y,Mathf.Min(420,right.width-52),30);
                Fill(row,new Color(.055f,.065f,.06f,.88f));Border(row,new Color(.16f,.19f,.17f,.75f),1);
                GUI.Label(new Rect(row.x+8,row.y+5,82,18),SlotLabel(s),tiny);
                GUI.Label(new Rect(row.x+92,row.y+4,row.width-100,20),string.IsNullOrEmpty(val)?"—":val,text);y+=36;
            }
            GUI.Label(new Rect(right.x+26,y+4,right.width-52,20),"Isolering "+TotalInsulation().ToString("0.0")+"   •   Vattenskydd "+TotalWaterproof().ToString("0.0"),tiny);
        }

        void MiniStat(Rect r,string code,float value,Color c,GUIStyle tiny){
            GUI.Label(new Rect(r.x,r.y-3,16,14),code,tiny);
            Rect bar=new Rect(r.x+18,r.y,r.width-42,r.height);Fill(bar,new Color(.08f,.095f,.085f,.95f));
            Fill(new Rect(bar.x+1,bar.y+1,(bar.width-2)*Mathf.Clamp01(value/100f),bar.height-2),c);
            GUIStyle num=Style(9,FontStyle.Bold,new Color(.78f,.79f,.73f),TextAnchor.MiddleRight,false);
            GUI.Label(new Rect(r.x+r.width-23,r.y-3,23,14),Mathf.RoundToInt(value).ToString(),num);
        }

        GUIStyle Style(int size,FontStyle fs,Color color,TextAnchor align,bool wrap){GUIStyle s=new GUIStyle(GUI.skin.label){fontSize=size,fontStyle=fs,alignment=align,wordWrap=wrap};s.normal.textColor=color;return s;}
        void Panel(Rect r,Color fill,Color border){Fill(r,fill);Border(r,border,1);}
        void Fill(Rect r,Color c){Color o=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=o;}
        void Border(Rect r,Color c,int w){Fill(new Rect(r.x,r.y,r.width,w),c);Fill(new Rect(r.x,r.yMax-w,r.width,w),c);Fill(new Rect(r.x,r.y,w,r.height),c);Fill(new Rect(r.xMax-w,r.y,w,r.height),c);}
        void Line(Rect r,Color c){Fill(r,c);}

        string WeatherLabel(){switch(survival.weather){case SurvivalSystemsV69.WeatherKind.Clear:return"KLART";case SurvivalSystemsV69.WeatherKind.Overcast:return"MULET";case SurvivalSystemsV69.WeatherKind.Rain:return"REGN";case SurvivalSystemsV69.WeatherKind.Snow:return"SNÖ";default:return"KÖLD";}}
        string KindLabel(SurvivalSystemsV69.ItemKind k){switch(k){case SurvivalSystemsV69.ItemKind.Food:return"MAT";case SurvivalSystemsV69.ItemKind.Drink:return"DRYCK";case SurvivalSystemsV69.ItemKind.Medical:return"VÅRD";case SurvivalSystemsV69.ItemKind.Clothing:return"KLÄDER";case SurvivalSystemsV69.ItemKind.Tool:return"VERKTYG";case SurvivalSystemsV69.ItemKind.Material:return"MATERIAL";default:return"ÖVRIGT";}}
        string SlotLabel(string s){switch(s){case"Head":return"HUVUD";case"Body":return"YTTER";case"Mid":return"MELLAN";case"Hands":return"HÄNDER";case"Feet":return"FÖTTER";default:return s.ToUpperInvariant();}}
    }
}
