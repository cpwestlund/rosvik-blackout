using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rosvik.Blackout {
    [DefaultExecutionOrder(8200)]
    public sealed class SurvivalEquipmentV76 : MonoBehaviour {
        CoziPlayerV57 player;
        SurvivalSystemsV69 survival;
        Dictionary<string,SurvivalSystemsV69.ItemDef> defs;
        Dictionary<string,string> equipped;
        FieldInfo defsField, equippedField;
        readonly Dictionary<string,float> condition = new Dictionary<string,float>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string,float> garmentWetness = new Dictionary<string,float>(StringComparer.OrdinalIgnoreCase);
        float tick;
        bool reconciled;

        public IReadOnlyDictionary<string,string> EquippedItems => equipped;

        void Awake(){
            player=GetComponent<CoziPlayerV57>();if(!player)player=FindFirstObjectByType<CoziPlayerV57>();
            survival=GetComponent<SurvivalSystemsV69>();if(!survival)survival=FindFirstObjectByType<SurvivalSystemsV69>();
            defsField=typeof(SurvivalSystemsV69).GetField("defs",BindingFlags.Static|BindingFlags.NonPublic);
            equippedField=typeof(SurvivalSystemsV69).GetField("equipped",BindingFlags.Instance|BindingFlags.NonPublic);
            defs=defsField!=null?defsField.GetValue(null) as Dictionary<string,SurvivalSystemsV69.ItemDef>:null;
            equipped=survival!=null&&equippedField!=null?equippedField.GetValue(survival) as Dictionary<string,string>:null;
        }

        void Start(){ReconcileLegacyEquipment();}

        void Update(){
            if(!player||!survival)return;
            if(!reconciled)ReconcileLegacyEquipment();
            tick+=Time.deltaTime;if(tick<1f)return;float dt=tick;tick=0f;
            bool sheltered=IsSheltered(player.transform.position);
            bool precip=survival.weather==SurvivalSystemsV69.WeatherKind.Rain||survival.weather==SurvivalSystemsV69.WeatherKind.Snow||survival.weather==SurvivalSystemsV69.WeatherKind.ColdSnap;
            foreach(string item in Worn().ToList()){
                EnsureState(item);
                if(!defs.TryGetValue(item,out var d))continue;
                float wet=garmentWetness[item];
                if(!sheltered&&precip){
                    float resistance=Mathf.Clamp01(d.waterproof/5.5f);
                    float rate=(survival.weather==SurvivalSystemsV69.WeatherKind.Rain?1.55f:.52f)*Mathf.Lerp(1f,.18f,resistance);
                    if(d.slot=="Mid")rate*=.45f;
                    wet=Mathf.Clamp(wet+rate*dt,0,100);
                } else wet=Mathf.Clamp(wet-(sheltered?1.9f:.32f)*dt,0,100);
                garmentWetness[item]=wet;
                float wear=.0008f*dt+(wet>70f?.0020f*dt:0f);
                condition[item]=Mathf.Clamp(condition[item]-wear,0,100);
            }

            if(sheltered){
                survival.wetness=Mathf.MoveTowards(survival.wetness,0f,1.25f*dt);
                foreach(string n in new[]{"V69 RAIN","V69 SNOW","V70 RAIN","V70 SNOW","V72 RAIN","V72 SNOW"}){
                    Transform t=player.transform.Find(n);if(t)t.gameObject.SetActive(false);
                }
            }

            float loss=0f;
            foreach(string item in Worn()){
                if(!defs.TryGetValue(item,out var d))continue;
                float baseIns=d.insulation;
                loss+=Mathf.Max(0f,baseIns-EffectiveInsulation(item));
            }
            if(loss>.01f)survival.warmth=Mathf.Max(0f,survival.warmth-loss*.012f*dt);
        }

        public bool IsSheltered(Vector3 p){
            bool houseA=p.x>-36.25f&&p.x<-21.75f&&p.z>2.15f&&p.z<15.85f;
            bool school=p.x>-18.4f&&p.x<18.4f&&p.z>-1.4f&&p.z<16.1f;
            bool connector=p.x>=18.0f&&p.x<22.6f&&p.z>4.0f&&p.z<10.2f;
            bool hall=p.x>=22.0f&&p.x<45.5f&&p.z>-1.3f&&p.z<16.3f;
            return houseA||school||connector||hall;
        }

        void ReconcileLegacyEquipment(){
            if(reconciled||player==null||equipped==null)return;
            foreach(string item in Worn().ToList()){
                EnsureState(item);
                if(player.CountItem(item)>0)player.ConsumeItem(item,1);
            }
            reconciled=true;
        }

        IEnumerable<string> Worn(){
            if(equipped==null)yield break;
            foreach(string v in equipped.Values)if(!string.IsNullOrWhiteSpace(v))yield return v;
        }

        void EnsureState(string item){
            if(string.IsNullOrWhiteSpace(item))return;
            if(!condition.ContainsKey(item))condition[item]=100f;
            if(!garmentWetness.ContainsKey(item))garmentWetness[item]=survival?Mathf.Clamp(survival.wetness*.35f,0,100):0f;
        }

        public bool IsEquipped(string item){return equipped!=null&&equipped.Values.Any(v=>string.Equals(v,item,StringComparison.OrdinalIgnoreCase));}
        public string SlotItem(string slot){return equipped!=null&&equipped.TryGetValue(slot,out string v)?v:"";}
        public float Condition(string item){EnsureState(item);return condition.TryGetValue(item,out float v)?v:100f;}
        public float Wetness(string item){EnsureState(item);return garmentWetness.TryGetValue(item,out float v)?v:0f;}

        public float EffectiveInsulation(string item){
            if(defs==null||!defs.TryGetValue(item,out var d))return 0f;
            float c=Mathf.Lerp(.45f,1f,Condition(item)/100f);
            float wet=Wetness(item)/100f;
            float wetFactor=item.IndexOf("ull",StringComparison.OrdinalIgnoreCase)>=0?Mathf.Lerp(1f,.78f,wet):Mathf.Lerp(1f,.58f,wet);
            return d.insulation*c*wetFactor;
        }
        public float EffectiveWaterproof(string item){
            if(defs==null||!defs.TryGetValue(item,out var d))return 0f;
            return d.waterproof*Mathf.Lerp(.35f,1f,Condition(item)/100f);
        }

        public bool ToggleClothing(string item){
            if(player==null||defs==null||equipped==null||!defs.TryGetValue(item,out var d)||d.kind!=SurvivalSystemsV69.ItemKind.Clothing)return false;
            EnsureState(item);
            string slot=d.slot;
            if(equipped.TryGetValue(slot,out string now)&&string.Equals(now,item,StringComparison.OrdinalIgnoreCase)){
                equipped[slot]="";player.AddItem(item,1);player.ShowToast("Tog av: "+item,1.6f);return true;
            }
            if(player.CountItem(item)<=0)return false;
            if(!string.IsNullOrWhiteSpace(now)){
                EnsureState(now);player.AddItem(now,1);
            }
            if(!player.ConsumeItem(item,1))return false;
            equipped[slot]=item;
            player.ShowToast("Tog på: "+item,1.6f);
            return true;
        }
    }

    public sealed class DoorPassageV76 : MonoBehaviour {
        CozyInteractableV57 door;
        bool lastPass;
        void Awake(){door=GetComponent<CozyInteractableV57>();Apply(true);}
        void LateUpdate(){Apply(false);}
        void Apply(bool force){
            if(!door||door.kind!=CozyInteractableV57.Kind.Door||!door.movingPart)return;
            float angle=Quaternion.Angle(door.movingPart.localRotation,Quaternion.Euler(door.closedEuler));
            bool pass=door.IsOpen||angle>8f;
            if(!force&&pass==lastPass)return;lastPass=pass;
            foreach(Collider c in door.movingPart.GetComponentsInChildren<Collider>(true))if(c)c.enabled=!pass;
        }
    }

    [DefaultExecutionOrder(9000)]
    public sealed class SurvivalInventoryV76 : MonoBehaviour {
        static readonly Dictionary<string,float> weights=new Dictionary<string,float>(StringComparer.OrdinalIgnoreCase){
            {"Ficklampa",.38f},{"Batterier",.18f},{"Vattenflaska",1.05f},{"Läsk",.38f},{"Sportdryck",.58f},{"Energibar",.09f},{"Konservburk",.46f},{"Soppa",.52f},{"Kex",.22f},{"Choklad",.10f},{"Äpple",.16f},{"Förband",.08f},{"Tejp",.16f},{"Sporttejp",.12f},{"Tändare",.05f},{"Multiverktyg",.28f},{"Nyckelknippa",.22f},{"Säkring",.08f},{"Penna",.01f},{"Vinterjacka",1.85f},{"Regnjacka",.72f},{"Ulltröja",.66f},{"Hoodie",.78f},{"Mössa",.14f},{"Handskar",.18f},{"Kängor",1.65f},{"Tygbit",.12f},{"Metallskrot",.55f},{"Träspill",.35f},{"Bränsle",.65f},{"Improviserad fackla",.70f},{"Varm soppa",.48f}
        };
        static readonly Dictionary<string,string> extraDesc=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase){
            {"Tygbit","Tyg från gamla textilier. Används till förband, reparation och crafting."},{"Metallskrot","Små användbara metalldelar för reparationer och tillverkning."},{"Träspill","Torrt trä som kan användas i enkla konstruktioner eller som bränsle."},{"Bränsle","Bränsle till stormkök och andra små värmekällor."},{"Improviserad fackla","Reservljus som kan tändas med T. Brinner bara en begränsad tid."},{"Varm soppa","Varm mat som ger mättnad, vätska och hjälper kroppsvärmen."}
        };
        static readonly Dictionary<string,string> extraIcons=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase){
            {"Tygbit","bandage"},{"Metallskrot","multitool"},{"Träspill","misc"},{"Bränsle","lighter"},{"Improviserad fackla","flashlight"},{"Varm soppa","soup"}
        };

        public float backpackCapacityKg=34f;
        CoziPlayerV57 player;
        SurvivalSystemsV69 survival;
        SurvivalEquipmentV76 equipment;
        SurvivalLootTransferV74 transfer;
        SurvivalPresentationV73 oldPresentation;
        Dictionary<string,SurvivalSystemsV69.ItemDef> defs;
        FieldInfo defsField,usePanelField,playerInvField;
        MethodInfo useItemMethod;
        bool open;
        string selected="";
        Vector2 scroll;
        readonly Dictionary<string,Texture2D> icons=new Dictionary<string,Texture2D>(StringComparer.OrdinalIgnoreCase);

        void Awake(){
            player=GetComponent<CoziPlayerV57>();if(!player)player=FindFirstObjectByType<CoziPlayerV57>();
            survival=GetComponent<SurvivalSystemsV69>();if(!survival)survival=FindFirstObjectByType<SurvivalSystemsV69>();
            equipment=GetComponent<SurvivalEquipmentV76>();if(!equipment)equipment=gameObject.AddComponent<SurvivalEquipmentV76>();
            transfer=GetComponent<SurvivalLootTransferV74>();
            oldPresentation=GetComponent<SurvivalPresentationV73>();
            defsField=typeof(SurvivalSystemsV69).GetField("defs",BindingFlags.Static|BindingFlags.NonPublic);
            defs=defsField!=null?defsField.GetValue(null) as Dictionary<string,SurvivalSystemsV69.ItemDef>:null;
            useItemMethod=typeof(SurvivalSystemsV69).GetMethod("UseItem",BindingFlags.Instance|BindingFlags.NonPublic);
            usePanelField=typeof(SurvivalPresentationV73).GetField("panelOpen",BindingFlags.Instance|BindingFlags.NonPublic);
            playerInvField=typeof(CoziPlayerV57).GetField("inventoryOpen",BindingFlags.Instance|BindingFlags.NonPublic);
        }

        void Update(){
            if(!player)return;
            if(playerInvField!=null)playerInvField.SetValue(player,false);
            if(oldPresentation&&usePanelField!=null)usePanelField.SetValue(oldPresentation,false);
            if(transfer&&transfer.IsOpen){if(open)Close();return;}
            Keyboard kb=Keyboard.current;
            if(kb!=null&&(kb.iKey.wasPressedThisFrame||kb.tabKey.wasPressedThisFrame)){if(open)Close();else Open();}
            if(open&&kb!=null&&kb.escapeKey.wasPressedThisFrame)Close();
            if(open)player.externalUiBlocked=true;
        }

        void Open(){open=true;selected="";scroll=Vector2.zero;player.externalUiBlocked=true;Cursor.visible=true;Cursor.lockState=CursorLockMode.None;}
        void Close(){open=false;selected="";if(player&&!(transfer&&transfer.IsOpen))player.externalUiBlocked=false;}
        void OnDisable(){if(player&&!(transfer&&transfer.IsOpen))player.externalUiBlocked=false;}

        float Weight(string item)=>weights.TryGetValue(item,out float w)?w:.25f;
        float TotalWeight(){float v=0f;if(player!=null)foreach(var kv in player.Inventory)v+=Weight(kv.Key)*kv.Value;return v;}
        string IconKey(string item){if(defs!=null&&defs.TryGetValue(item,out var d))return d.icon;if(extraIcons.TryGetValue(item,out string k))return k;return "misc";}
        Texture2D Icon(string item){string k=IconKey(item);if(icons.TryGetValue(k,out var t)&&t)return t;t=Resources.Load<Texture2D>("ItemIcons/"+k);icons[k]=t;return t?t:Texture2D.whiteTexture;}
        string Description(string item){if(defs!=null&&defs.TryGetValue(item,out var d))return d.description;if(extraDesc.TryGetValue(item,out string x))return x;return "Ett föremål du har hittat. Det kan vara användbart senare.";}
        string Kind(string item){if(defs==null||!defs.TryGetValue(item,out var d))return "ÖVRIGT";switch(d.kind){case SurvivalSystemsV69.ItemKind.Food:return "MAT";case SurvivalSystemsV69.ItemKind.Drink:return "DRYCK";case SurvivalSystemsV69.ItemKind.Medical:return "VÅRD";case SurvivalSystemsV69.ItemKind.Clothing:return "KLÄDER";case SurvivalSystemsV69.ItemKind.Tool:return "VERKTYG";case SurvivalSystemsV69.ItemKind.Material:return "MATERIAL";default:return "ÖVRIGT";}}
        string Effects(string item){
            if(defs==null||!defs.TryGetValue(item,out var d))return "";
            List<string> p=new List<string>();
            if(Mathf.Abs(d.hunger)>.01f)p.Add("Mat "+Sign(d.hunger));if(Mathf.Abs(d.thirst)>.01f)p.Add("Vätska "+Sign(d.thirst));if(Mathf.Abs(d.health)>.01f)p.Add("Hälsa "+Sign(d.health));
            if(d.kind==SurvivalSystemsV69.ItemKind.Clothing){p.Add("Isolering "+d.insulation.ToString("0.0"));p.Add("Vattenskydd "+d.waterproof.ToString("0.0"));p.Add(SlotLabel(d.slot));}
            return string.Join("   •   ",p);
        }
        string Sign(float v)=>(v>=0?"+":"")+Mathf.RoundToInt(v);

        void Activate(){
            if(string.IsNullOrEmpty(selected))return;
            if(defs!=null&&defs.TryGetValue(selected,out var d)&&d.kind==SurvivalSystemsV69.ItemKind.Clothing){equipment.ToggleClothing(selected);return;}
            if(string.Equals(selected,"Varm soppa",StringComparison.OrdinalIgnoreCase)){
                if(player.ConsumeItem(selected,1)){survival.hunger=Mathf.Clamp(survival.hunger+40,0,100);survival.thirst=Mathf.Clamp(survival.thirst+18,0,100);survival.warmth=Mathf.Clamp(survival.warmth+24,0,100);player.ShowToast("Åt varm soppa",1.8f);}return;
            }
            if(defs!=null&&defs.TryGetValue(selected,out var def)&&useItemMethod!=null)useItemMethod.Invoke(survival,new object[]{def});
        }
        string Action(){
            if(string.IsNullOrEmpty(selected))return "";
            if(defs!=null&&defs.TryGetValue(selected,out var d)){
                if(d.kind==SurvivalSystemsV69.ItemKind.Clothing)return equipment.IsEquipped(selected)?"TA AV":"TA PÅ";
                if(d.kind==SurvivalSystemsV69.ItemKind.Food)return "ÄT";if(d.kind==SurvivalSystemsV69.ItemKind.Drink)return "DRICK";if(d.kind==SurvivalSystemsV69.ItemKind.Medical)return "ANVÄND";
            }
            if(string.Equals(selected,"Varm soppa",StringComparison.OrdinalIgnoreCase))return "ÄT";
            return "";
        }

        void OnGUI(){if(!open||!player)return;GUI.depth=-12000;Draw();}
        void Draw(){
            Color o=GUI.color;GUI.color=new Color(.004f,.007f,.006f,.94f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=o;
            GUIStyle title=Style(22,FontStyle.Bold,new Color(.90f,.90f,.83f),TextAnchor.MiddleLeft,false);
            GUIStyle head=Style(13,FontStyle.Bold,new Color(.78f,.81f,.74f),TextAnchor.MiddleLeft,false);
            GUIStyle text=Style(12,FontStyle.Normal,new Color(.72f,.75f,.69f),TextAnchor.UpperLeft,true);
            GUIStyle tiny=Style(10,FontStyle.Normal,new Color(.55f,.59f,.54f),TextAnchor.MiddleLeft,false);
            GUIStyle center=Style(10,FontStyle.Bold,new Color(.85f,.86f,.79f),TextAnchor.MiddleCenter,true);
            float margin=Mathf.Max(18,Screen.width*.025f),gap=22,top=54,bottom=50;
            float leftW=Mathf.Clamp(Screen.width*.37f,470,610);
            Rect left=new Rect(margin,top,leftW,Screen.height-top-bottom);
            Rect right=new Rect(left.xMax+gap,top,Screen.width-left.xMax-gap-margin,Screen.height-top-bottom);
            Panel(left);Panel(right);
            GUI.Label(new Rect(left.x+18,left.y+14,220,30),"RYGGSÄCK",title);
            GUIStyle kg=Style(11,FontStyle.Bold,TotalWeight()>backpackCapacityKg?new Color(.90f,.40f,.28f):new Color(.66f,.69f,.62f),TextAnchor.MiddleRight,false);
            GUI.Label(new Rect(left.xMax-210,left.y+17,190,22),TotalWeight().ToString("0.0")+" / "+backpackCapacityKg.ToString("0")+" KG",kg);
            Line(new Rect(left.x+18,left.y+52,left.width-36,1));

            int cols=6;float pad=18,space=8,gridW=left.width-pad*2;float cell=Mathf.Min(84,(gridW-space*(cols-1))/cols);float startY=left.y+70;
            var items=player.Inventory.OrderBy(k=>Kind(k.Key)).ThenBy(k=>k.Key).ToList();int slots=Mathf.Max(30,Mathf.CeilToInt(items.Count/(float)cols)*cols);float contentH=Mathf.CeilToInt(slots/(float)cols)*(cell+space);
            Rect view=new Rect(left.x+pad,startY,left.width-pad*2,left.height-180);scroll=GUI.BeginScrollView(view,scroll,new Rect(0,0,view.width-18,Mathf.Max(view.height-4,contentH)));
            for(int i=0;i<slots;i++){
                int c=i%cols,r=i/cols;Rect slot=new Rect(c*(cell+space),r*(cell+space),cell,cell);bool occupied=i<items.Count;bool sel=occupied&&string.Equals(selected,items[i].Key,StringComparison.OrdinalIgnoreCase);
                Fill(slot,sel?new Color(.17f,.20f,.17f,.98f):new Color(.065f,.078f,.071f,.98f));Border(slot,sel?new Color(.58f,.60f,.49f):new Color(.22f,.25f,.22f),sel?2:1);
                if(!occupied)continue;var kv=items[i];Rect ir=new Rect(slot.x+9,slot.y+7,cell-18,cell-18);DrawIcon(ir,Icon(kv.Key));if(kv.Value>1)GUI.Label(new Rect(slot.x+4,slot.y+cell-20,cell-8,16),"x"+kv.Value,Style(9,FontStyle.Bold,new Color(.9f,.88f,.80f),TextAnchor.LowerRight,false));if(GUI.Button(slot,GUIContent.none,GUIStyle.none))selected=kv.Key;
            }
            GUI.EndScrollView();
            GUI.Label(new Rect(left.x+18,left.yMax-72,left.width-36,20),"I / TAB / ESC   STÄNG",tiny);

            GUI.Label(new Rect(right.x+24,right.y+16,right.width-48,30),"FÄLTANTECKNINGAR",title);Line(new Rect(right.x+24,right.y+54,right.width-48,1));
            float bodyTop=right.y+78;
            DrawBody(right,bodyTop,head,text,tiny,center);
            float detailTop=bodyTop+300;
            if(!string.IsNullOrEmpty(selected)){
                Rect iconRect=new Rect(right.x+28,detailTop,104,104);DrawIcon(iconRect,Icon(selected));
                GUI.Label(new Rect(right.x+150,detailTop,right.width-178,26),selected,head);
                GUI.Label(new Rect(right.x+150,detailTop+29,right.width-178,18),Kind(selected)+"   •   "+Weight(selected).ToString("0.00")+" KG/ST",tiny);
                GUI.Label(new Rect(right.x+150,detailTop+54,right.width-178,62),Description(selected),text);
                string fx=Effects(selected);if(!string.IsNullOrEmpty(fx))GUI.Label(new Rect(right.x+28,detailTop+116,right.width-56,30),fx,tiny);
                if(equipment.IsEquipped(selected))GUI.Label(new Rect(right.x+28,detailTop+146,right.width-56,20),"Skick "+Mathf.RoundToInt(equipment.Condition(selected))+"%   •   Fukt "+Mathf.RoundToInt(equipment.Wetness(selected))+"%   •   Effektiv isolering "+equipment.EffectiveInsulation(selected).ToString("0.0"),tiny);
                string action=Action();if(!string.IsNullOrEmpty(action)&&GUI.Button(new Rect(right.x+28,right.yMax-58,150,34),action))Activate();
            } else GUI.Label(new Rect(right.x+28,detailTop,right.width-56,70),"Välj ett föremål i ryggsäcken eller klicka på ett plagg i kroppsöversikten för att läsa om det.",text);
        }

        void DrawBody(Rect right,float top,GUIStyle head,GUIStyle text,GUIStyle tiny,GUIStyle center){
            GUI.Label(new Rect(right.x+28,top,right.width-56,22),"PÅ KROPPEN",head);
            float cx=right.x+92,cy=top+70;
            Fill(new Rect(cx-18,cy-18,36,36),new Color(.20f,.24f,.21f));
            Fill(new Rect(cx-26,cy+22,52,72),new Color(.16f,.20f,.18f));
            Fill(new Rect(cx-48,cy+28,18,76),new Color(.13f,.17f,.15f));Fill(new Rect(cx+30,cy+28,18,76),new Color(.13f,.17f,.15f));
            Fill(new Rect(cx-24,cy+98,20,88),new Color(.13f,.17f,.15f));Fill(new Rect(cx+4,cy+98,20,88),new Color(.13f,.17f,.15f));
            string[] slots={"Head","Body","Mid","Hands","Feet"};float x=right.x+170,y=top+32,w=right.width-198;
            foreach(string slot in slots){
                string item=equipment.SlotItem(slot);Rect row=new Rect(x,y,w,42);Fill(row,new Color(.055f,.067f,.061f));Border(row,new Color(.16f,.19f,.17f),1);
                GUI.Label(new Rect(row.x+8,row.y+4,78,16),SlotLabel(slot),tiny);
                GUI.Label(new Rect(row.x+88,row.y+4,row.width-96,18),string.IsNullOrEmpty(item)?"—":item,text);
                if(!string.IsNullOrEmpty(item)){GUI.Label(new Rect(row.x+88,row.y+22,row.width-96,16),Mathf.RoundToInt(equipment.Condition(item))+"% skick   •   "+Mathf.RoundToInt(equipment.Wetness(item))+"% fukt",tiny);if(GUI.Button(row,GUIContent.none,GUIStyle.none))selected=item;}
                y+=48;
            }
        }

        void DrawIcon(Rect r,Texture2D t){if(!t)return;GUI.DrawTextureWithTexCoords(r,t,new Rect(0,1,1,-1),true);}
        GUIStyle Style(int s,FontStyle f,Color c,TextAnchor a,bool wrap){GUIStyle x=new GUIStyle(GUI.skin.label){fontSize=s,fontStyle=f,alignment=a,wordWrap=wrap};x.normal.textColor=c;return x;}
        void Panel(Rect r){Fill(r,new Color(.022f,.029f,.027f,.985f));Border(r,new Color(.29f,.32f,.28f),1);}
        void Fill(Rect r,Color c){Color o=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=o;}
        void Border(Rect r,Color c,int w){Fill(new Rect(r.x,r.y,r.width,w),c);Fill(new Rect(r.x,r.yMax-w,r.width,w),c);Fill(new Rect(r.x,r.y,w,r.height),c);Fill(new Rect(r.xMax-w,r.y,w,r.height),c);}
        void Line(Rect r){Fill(r,new Color(.20f,.23f,.20f,.9f));}
        string SlotLabel(string s){switch(s){case "Head":return "HUVUD";case "Body":return "YTTER";case "Mid":return "MELLAN";case "Hands":return "HÄNDER";case "Feet":return "FÖTTER";default:return s.ToUpperInvariant();}}
    }
}
