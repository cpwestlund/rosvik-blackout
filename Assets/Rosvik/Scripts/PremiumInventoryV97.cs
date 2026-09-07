using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rosvik.Blackout {
    [DefaultExecutionOrder(9500)]
    public sealed class PremiumInventoryV97 : MonoBehaviour {
        static readonly Dictionary<string,float> weights = new Dictionary<string,float>(StringComparer.OrdinalIgnoreCase) {
            {"Ficklampa",.38f},{"Batterier",.18f},{"Vattenflaska",1.05f},{"Läsk",.38f},{"Sportdryck",.58f},{"Energibar",.09f},{"Konservburk",.46f},{"Soppa",.52f},{"Kex",.22f},{"Choklad",.10f},{"Äpple",.16f},{"Förband",.08f},{"Tejp",.16f},{"Sporttejp",.12f},{"Tändare",.05f},{"Multiverktyg",.28f},{"Nyckelknippa",.22f},{"Säkring",.08f},{"Penna",.01f},{"Vinterjacka",1.85f},{"Regnjacka",.72f},{"Ulltröja",.66f},{"Hoodie",.78f},{"Mössa",.14f},{"Handskar",.18f},{"Kängor",1.65f},{"Tygbit",.12f},{"Metallskrot",.55f},{"Träspill",.35f},{"Bränsle",.65f},{"Improviserad fackla",.70f},{"Varm soppa",.48f}
        };
        static readonly Dictionary<string,string> extraIcons = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase) {
            {"Tygbit","bandage"},{"Metallskrot","multitool"},{"Träspill","misc"},{"Bränsle","lighter"},{"Improviserad fackla","flashlight"},{"Varm soppa","soup"}
        };
        static readonly Dictionary<string,string> extraDesc = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase) {
            {"Tygbit","Tyg från gamla textilier. Kan bli förband, lagningar eller användas vid tillverkning."},
            {"Metallskrot","Små metalldelar. Bra att spara för reparationer och enklare konstruktioner."},
            {"Träspill","Torrt trä som kan användas till enkla byggen eller som bränsle."},
            {"Bränsle","Bränsle till stormkök och små värmekällor. Håll det torrt."},
            {"Improviserad fackla","Ett enkelt reservljus med begränsad brinntid."},
            {"Varm soppa","Varm mat som ger mättnad, vätska och hjälper kroppstemperaturen."}
        };

        public float backpackCapacityKg = 34f;

        CoziPlayerV57 player;
        SurvivalSystemsV69 survival;
        SurvivalEquipmentV76 equipment;
        SurvivalLootTransferV74 transfer;
        SurvivalPresentationV73 presentation;
        SurvivalInventoryV76 legacyInventory;

        FieldInfo defsField, panelOpenField, legacyPlayerInvField, transferActiveField;
        MethodInfo useItemMethod, takeMethod, returnMethod, takeAllMethod;
        Dictionary<string,SurvivalSystemsV69.ItemDef> defs;
        readonly Dictionary<string,Texture2D> icons = new Dictionary<string,Texture2D>(StringComparer.OrdinalIgnoreCase);

        bool open;
        bool transferTakeover;
        LootContainerV74 activeContainer;
        string selected = "";
        bool selectedFromContainer;
        Vector2 backpackScroll, containerScroll;

        void Awake() {
            player = GetComponent<CoziPlayerV57>(); if(!player) player = FindFirstObjectByType<CoziPlayerV57>();
            survival = GetComponent<SurvivalSystemsV69>(); if(!survival) survival = FindFirstObjectByType<SurvivalSystemsV69>();
            equipment = GetComponent<SurvivalEquipmentV76>(); if(!equipment && player) equipment = player.gameObject.AddComponent<SurvivalEquipmentV76>();
            transfer = GetComponent<SurvivalLootTransferV74>(); if(!transfer) transfer = FindFirstObjectByType<SurvivalLootTransferV74>();
            presentation = GetComponent<SurvivalPresentationV73>(); if(!presentation) presentation = FindFirstObjectByType<SurvivalPresentationV73>();
            legacyInventory = GetComponent<SurvivalInventoryV76>();

            defsField = typeof(SurvivalSystemsV69).GetField("defs", BindingFlags.Static|BindingFlags.NonPublic);
            defs = defsField != null ? defsField.GetValue(null) as Dictionary<string,SurvivalSystemsV69.ItemDef> : null;
            useItemMethod = typeof(SurvivalSystemsV69).GetMethod("UseItem", BindingFlags.Instance|BindingFlags.NonPublic);
            panelOpenField = typeof(SurvivalPresentationV73).GetField("panelOpen", BindingFlags.Instance|BindingFlags.NonPublic);
            legacyPlayerInvField = typeof(CoziPlayerV57).GetField("inventoryOpen", BindingFlags.Instance|BindingFlags.NonPublic);

            if(transfer) {
                transferActiveField = typeof(SurvivalLootTransferV74).GetField("active", BindingFlags.Instance|BindingFlags.NonPublic);
                takeMethod = typeof(SurvivalLootTransferV74).GetMethod("Take", BindingFlags.Instance|BindingFlags.NonPublic);
                returnMethod = typeof(SurvivalLootTransferV74).GetMethod("Return", BindingFlags.Instance|BindingFlags.NonPublic);
                takeAllMethod = typeof(SurvivalLootTransferV74).GetMethod("TakeAll", BindingFlags.Instance|BindingFlags.NonPublic);
            }
            if(legacyInventory) legacyInventory.enabled = false;
        }

        void Update() {
            if(!player) return;
            if(legacyPlayerInvField != null) legacyPlayerInvField.SetValue(player,false);
            if(presentation && panelOpenField != null) panelOpenField.SetValue(presentation,false);

            if(!transferTakeover && transfer && transfer.enabled && transfer.IsOpen) BeginTransferTakeover();

            Keyboard kb = Keyboard.current;
            if(transferTakeover) {
                player.externalUiBlocked = true;
                if(kb != null && (kb.escapeKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame)) EndTransferTakeover();
                return;
            }

            if(kb != null && (kb.iKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame)) {
                if(open) CloseInventory(); else OpenInventory();
            }
            if(open && kb != null && kb.escapeKey.wasPressedThisFrame) CloseInventory();
            if(open) player.externalUiBlocked = true;
        }

        void BeginTransferTakeover() {
            activeContainer = transferActiveField != null ? transferActiveField.GetValue(transfer) as LootContainerV74 : null;
            if(!activeContainer) return;
            transferTakeover = true; open = false; selected = ""; selectedFromContainer = true;
            backpackScroll = containerScroll = Vector2.zero;
            transfer.enabled = false;
            player.externalUiBlocked = true;
            Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
        }
        void EndTransferTakeover() {
            if(!transferTakeover) return;
            transferTakeover = false; selected = ""; activeContainer = null;
            if(transfer) { transfer.Close(); transfer.enabled = true; }
            if(player) player.externalUiBlocked = false;
        }
        void OpenInventory() { open = true; selected = ""; backpackScroll = Vector2.zero; player.externalUiBlocked = true; Cursor.visible = true; Cursor.lockState = CursorLockMode.None; }
        void CloseInventory() { open = false; selected = ""; if(player && !transferTakeover) player.externalUiBlocked = false; }
        void OnDisable() { if(player) player.externalUiBlocked = false; if(transferTakeover && transfer) transfer.enabled = true; }

        float Weight(string item) => weights.TryGetValue(item,out float w) ? w : .25f;
        float TotalWeight() { float v=0f; if(player!=null) foreach(var kv in player.Inventory) v += Weight(kv.Key)*kv.Value; return v; }
        string IconKey(string item) { if(defs!=null && defs.TryGetValue(item,out var d)) return d.icon; if(extraIcons.TryGetValue(item,out string k)) return k; return "misc"; }
        Texture2D Icon(string item) { string k=IconKey(item); if(icons.TryGetValue(k,out var t) && t) return t; t=Resources.Load<Texture2D>("ItemIcons/"+k); icons[k]=t; return t?t:Texture2D.whiteTexture; }
        string Description(string item) { if(defs!=null&&defs.TryGetValue(item,out var d)) return d.description; if(extraDesc.TryGetValue(item,out string s)) return s; return "Ett föremål som kan vara användbart senare."; }
        string Kind(string item) {
            if(defs==null||!defs.TryGetValue(item,out var d)) return "ÖVRIGT";
            switch(d.kind){case SurvivalSystemsV69.ItemKind.Food:return"MAT";case SurvivalSystemsV69.ItemKind.Drink:return"DRYCK";case SurvivalSystemsV69.ItemKind.Medical:return"VÅRD";case SurvivalSystemsV69.ItemKind.Clothing:return"KLÄDER";case SurvivalSystemsV69.ItemKind.Tool:return"VERKTYG";case SurvivalSystemsV69.ItemKind.Material:return"MATERIAL";default:return"ÖVRIGT";}
        }
        string Effects(string item) {
            if(defs==null||!defs.TryGetValue(item,out var d)) return "";
            List<string> x=new List<string>();
            if(Mathf.Abs(d.hunger)>.01f)x.Add("Mat "+Signed(d.hunger)); if(Mathf.Abs(d.thirst)>.01f)x.Add("Vätska "+Signed(d.thirst)); if(Mathf.Abs(d.health)>.01f)x.Add("Hälsa "+Signed(d.health));
            if(d.kind==SurvivalSystemsV69.ItemKind.Clothing){x.Add("Isolering "+d.insulation.ToString("0.0"));x.Add("Vattenskydd "+d.waterproof.ToString("0.0"));}
            return string.Join("   •   ",x);
        }
        string Signed(float v)=>(v>=0?"+":"")+Mathf.RoundToInt(v);

        void ActivateSelected() {
            if(string.IsNullOrEmpty(selected) || player.CountItem(selected)<=0) return;
            if(defs!=null&&defs.TryGetValue(selected,out var d)&&d.kind==SurvivalSystemsV69.ItemKind.Clothing){equipment.ToggleClothing(selected);return;}
            if(string.Equals(selected,"Varm soppa",StringComparison.OrdinalIgnoreCase)){
                if(player.ConsumeItem(selected,1)){survival.hunger=Mathf.Clamp(survival.hunger+40,0,100);survival.thirst=Mathf.Clamp(survival.thirst+18,0,100);survival.warmth=Mathf.Clamp(survival.warmth+24,0,100);player.ShowToast("Åt varm soppa",1.7f);}return;
            }
            if(defs!=null&&defs.TryGetValue(selected,out var def)&&useItemMethod!=null) useItemMethod.Invoke(survival,new object[]{def});
        }
        string ActionLabel() {
            if(string.IsNullOrEmpty(selected)) return "";
            if(defs!=null&&defs.TryGetValue(selected,out var d)){
                if(d.kind==SurvivalSystemsV69.ItemKind.Clothing) return equipment.IsEquipped(selected)?"TA AV":"TA PÅ";
                if(d.kind==SurvivalSystemsV69.ItemKind.Food)return"ÄT"; if(d.kind==SurvivalSystemsV69.ItemKind.Drink)return"DRICK"; if(d.kind==SurvivalSystemsV69.ItemKind.Medical)return"ANVÄND";
            }
            if(string.Equals(selected,"Varm soppa",StringComparison.OrdinalIgnoreCase))return"ÄT";
            return "";
        }

        void TransferTake(string item,int count){if(transfer&&takeMethod!=null)takeMethod.Invoke(transfer,new object[]{item,count});}
        void TransferReturn(string item,int count){if(transfer&&returnMethod!=null)returnMethod.Invoke(transfer,new object[]{item,count});}
        void TransferTakeAll(){if(transfer&&takeAllMethod!=null)takeAllMethod.Invoke(transfer,null);}

        void OnGUI(){
            if(!player || (!open && !transferTakeover)) return;
            GUI.depth=-25000;
            if(transferTakeover) DrawTransfer(); else DrawInventory();
        }

        void DrawInventory(){
            Backdrop();
            float m=Mathf.Max(24,Screen.width*.025f), top=34, gap=18;
            Rect frame=new Rect(m,top,Screen.width-m*2,Screen.height-top-34); Panel(frame,new Color(.035f,.055f,.061f,.985f),new Color(.32f,.43f,.43f,.92f));
            GUI.Label(new Rect(frame.x+24,frame.y+18,520,34),"DET DU BÄR MED DIG",Style(25,FontStyle.Bold,Ink(),TextAnchor.MiddleLeft,false));
            GUI.Label(new Rect(frame.x+24,frame.y+54,420,22),TotalWeight().ToString("0.00")+" / "+backpackCapacityKg.ToString("0")+" kg",Style(12,FontStyle.Bold,Muted(),TextAnchor.MiddleLeft,false));
            float meterW=220; Rect meter=new Rect(frame.x+250,frame.y+61,meterW,7); Fill(meter,new Color(.08f,.12f,.13f)); Fill(new Rect(meter.x,meter.y,meter.width*Mathf.Clamp01(TotalWeight()/backpackCapacityKg),meter.height),TotalWeight()>backpackCapacityKg?new Color(.75f,.34f,.25f):new Color(.48f,.68f,.66f));
            Line(new Rect(frame.x+24,frame.y+88,frame.width-48,1),new Color(.24f,.34f,.34f,.7f));

            float bodyY=frame.y+108, bodyH=frame.height-140;
            Rect bag=new Rect(frame.x+24,bodyY,frame.width*.43f,bodyH); Rect detail=new Rect(bag.xMax+gap,bodyY,frame.width-bag.width-gap-48,bodyH);
            SectionTitle(bag,"RYGGSÄCK");
            Rect list=new Rect(bag.x,bag.y+38,bag.width,bag.height-38); backpackScroll=DrawItemList(list,player.Inventory.OrderBy(x=>Kind(x.Key)).ThenBy(x=>x.Key).ToList(),backpackScroll,false);

            SectionTitle(detail,"UTRUSTNING & FÄLTINFO");
            Rect gear=new Rect(detail.x,detail.y+38,detail.width,210); DrawGear(gear);
            Rect info=new Rect(detail.x,gear.yMax+18,detail.width,detail.yMax-gear.yMax-18); DrawSelectedInfo(info,false);
            GUI.Label(new Rect(frame.xMax-260,frame.y+23,230,22),"I / TAB / ESC  STÄNG",Style(11,FontStyle.Bold,Muted(),TextAnchor.MiddleRight,false));
        }

        void DrawTransfer(){
            Backdrop(); if(!activeContainer){EndTransferTakeover();return;}
            float m=Mathf.Max(24,Screen.width*.022f), top=34, gap=18;
            Rect frame=new Rect(m,top,Screen.width-m*2,Screen.height-top-34); Panel(frame,new Color(.035f,.055f,.061f,.99f),new Color(.32f,.43f,.43f,.92f));
            GUI.Label(new Rect(frame.x+24,frame.y+16,640,34),"SÖKER:  "+activeContainer.displayName.ToUpperInvariant(),Style(24,FontStyle.Bold,Ink(),TextAnchor.MiddleLeft,false));
            GUI.Label(new Rect(frame.xMax-300,frame.y+23,270,22),"E / ESC  STÄNG",Style(11,FontStyle.Bold,Muted(),TextAnchor.MiddleRight,false));
            Line(new Rect(frame.x+24,frame.y+72,frame.width-48,1),new Color(.24f,.34f,.34f,.7f));

            float y=frame.y+92, h=frame.height-205; float col=(frame.width-66-gap)/2f;
            Rect bag=new Rect(frame.x+24,y,col,h); Rect box=new Rect(bag.xMax+gap,y,col,h);
            SectionTitle(bag,"RYGGSÄCK   "+TotalWeight().ToString("0.0")+" / "+backpackCapacityKg.ToString("0")+" kg");
            SectionTitle(box,activeContainer.displayName.ToUpperInvariant());
            backpackScroll=DrawItemList(new Rect(bag.x,bag.y+38,bag.width,bag.height-38),player.Inventory.OrderBy(x=>x.Key).ToList(),backpackScroll,false);
            containerScroll=DrawItemList(new Rect(box.x,box.y+38,box.width,box.height-38),activeContainer.Remaining.OrderBy(x=>x.Key).ToList(),containerScroll,true);

            Rect info=new Rect(frame.x+24,frame.yMax-98,frame.width-48,72); Panel(info,new Color(.025f,.039f,.044f,.98f),new Color(.20f,.30f,.31f,.86f));
            if(!string.IsNullOrEmpty(selected)){
                Texture2D t=Icon(selected); DrawIcon(new Rect(info.x+10,info.y+8,56,56),t);
                GUI.Label(new Rect(info.x+78,info.y+8,260,22),selected,Style(15,FontStyle.Bold,Ink(),TextAnchor.MiddleLeft,false));
                GUI.Label(new Rect(info.x+78,info.y+31,420,18),Kind(selected)+"  •  "+Weight(selected).ToString("0.00")+" kg/st",Style(10,FontStyle.Bold,Muted(),TextAnchor.MiddleLeft,false));
                GUI.Label(new Rect(info.x+350,info.y+8,Mathf.Max(200,info.width-760),50),Description(selected),Style(11,FontStyle.Normal,Soft(),TextAnchor.UpperLeft,true));
                float bx=info.xMax-340;
                if(selectedFromContainer){if(ActionButton(new Rect(bx,info.y+10,100,42),"TA 1"))TransferTake(selected,1);if(ActionButton(new Rect(bx+108,info.y+10,110,42),"TA ALLA"))TransferTake(selected,Mathf.Max(1,activeContainer.Count(selected)));}
                else {if(ActionButton(new Rect(bx,info.y+10,100,42),"LÄGG 1"))TransferReturn(selected,1);if(ActionButton(new Rect(bx+108,info.y+10,110,42),"LÄGG ALLA"))TransferReturn(selected,Mathf.Max(1,player.CountItem(selected)));}
            } else GUI.Label(new Rect(info.x+16,info.y+23,info.width-32,24),"Välj exakt vad du vill flytta. Ingenting tas automatiskt.",Style(12,FontStyle.Bold,Muted(),TextAnchor.MiddleLeft,false));
        }

        Vector2 DrawItemList(Rect area,IList<KeyValuePair<string,int>> items,Vector2 scroll,bool fromContainer){
            Panel(area,new Color(.024f,.038f,.042f,.96f),new Color(.15f,.24f,.25f,.82f));
            float rowH=74, innerW=area.width-24; float content=Mathf.Max(area.height-12,items.Count*rowH+8);
            Rect view=new Rect(area.x+6,area.y+6,area.width-12,area.height-12); scroll=GUI.BeginScrollView(view,scroll,new Rect(0,0,innerW-16,content));
            for(int i=0;i<items.Count;i++){
                var kv=items[i]; Rect row=new Rect(4,i*rowH+4,innerW-28,rowH-8); bool sel=string.Equals(selected,kv.Key,StringComparison.OrdinalIgnoreCase)&&selectedFromContainer==fromContainer;
                Fill(row,sel?new Color(.11f,.17f,.18f,.98f):new Color(.045f,.066f,.069f,.98f)); Border(row,sel?new Color(.55f,.70f,.66f,.95f):new Color(.16f,.24f,.25f,.9f),sel?2:1);
                DrawIcon(new Rect(row.x+8,row.y+7,54,54),Icon(kv.Key));
                GUI.Label(new Rect(row.x+74,row.y+10,row.width-170,23),kv.Key+(kv.Value>1?"  ×"+kv.Value:""),Style(13,FontStyle.Bold,Ink(),TextAnchor.MiddleLeft,false));
                GUI.Label(new Rect(row.x+74,row.y+35,row.width-170,17),Kind(kv.Key)+"   •   "+Weight(kv.Key).ToString("0.00")+" kg",Style(9,FontStyle.Bold,Muted(),TextAnchor.MiddleLeft,false));
                if(defs!=null&&defs.TryGetValue(kv.Key,out var d)&&d.kind==SurvivalSystemsV69.ItemKind.Clothing&&equipment.IsEquipped(kv.Key)) GUI.Label(new Rect(row.xMax-82,row.y+10,70,18),"PÅ KROPP",Style(9,FontStyle.Bold,new Color(.65f,.80f,.63f),TextAnchor.MiddleRight,false));
                if(GUI.Button(row,GUIContent.none,GUIStyle.none)){selected=kv.Key;selectedFromContainer=fromContainer;}
            }
            GUI.EndScrollView(); return scroll;
        }

        void DrawGear(Rect r){
            Panel(r,new Color(.024f,.038f,.042f,.96f),new Color(.15f,.24f,.25f,.82f));
            string[] slots={"Head","Body","Mid","Hands","Feet"}; string[] labels={"HUVUD","YTTERLAGER","MELLANLAGER","HÄNDER","FÖTTER"};
            float left=r.x+18, top=r.y+14, row=(r.height-28)/slots.Length;
            for(int i=0;i<slots.Length;i++){
                string item=equipment!=null?equipment.SlotItem(slots[i]):""; Rect rr=new Rect(left,top+i*row,r.width-36,row-5);
                Fill(rr,new Color(.045f,.066f,.069f,.97f)); Border(rr,new Color(.14f,.22f,.23f),1);
                GUI.Label(new Rect(rr.x+10,rr.y+5,110,18),labels[i],Style(9,FontStyle.Bold,Muted(),TextAnchor.MiddleLeft,false));
                GUI.Label(new Rect(rr.x+126,rr.y+4,rr.width-200,20),string.IsNullOrEmpty(item)?"—":item,Style(11,FontStyle.Bold,Ink(),TextAnchor.MiddleLeft,false));
                if(!string.IsNullOrEmpty(item)&&equipment!=null){GUI.Label(new Rect(rr.xMax-165,rr.y+4,150,18),Mathf.RoundToInt(equipment.Condition(item))+"% skick  •  "+Mathf.RoundToInt(equipment.Wetness(item))+"% fukt",Style(9,FontStyle.Bold,Muted(),TextAnchor.MiddleRight,false));if(GUI.Button(rr,GUIContent.none,GUIStyle.none)){selected=item;selectedFromContainer=false;}}
            }
        }

        void DrawSelectedInfo(Rect r,bool transferMode){
            Panel(r,new Color(.024f,.038f,.042f,.96f),new Color(.15f,.24f,.25f,.82f));
            if(string.IsNullOrEmpty(selected)){
                GUI.Label(new Rect(r.x+18,r.y+18,r.width-36,26),"Välj ett föremål",Style(14,FontStyle.Bold,Ink(),TextAnchor.MiddleLeft,false));
                GUI.Label(new Rect(r.x+18,r.y+52,r.width-36,72),"Klicka i ryggsäcken eller på ett plagg. Här visas vad föremålet gör, vikt, skick och hur det påverkar överlevnaden.",Style(11,FontStyle.Normal,Soft(),TextAnchor.UpperLeft,true)); return;
            }
            DrawIcon(new Rect(r.x+16,r.y+16,92,92),Icon(selected));
            GUI.Label(new Rect(r.x+124,r.y+16,r.width-146,26),selected,Style(17,FontStyle.Bold,Ink(),TextAnchor.MiddleLeft,false));
            GUI.Label(new Rect(r.x+124,r.y+46,r.width-146,20),Kind(selected)+"   •   "+Weight(selected).ToString("0.00")+" kg/st",Style(10,FontStyle.Bold,Muted(),TextAnchor.MiddleLeft,false));
            GUI.Label(new Rect(r.x+124,r.y+76,r.width-146,64),Description(selected),Style(11,FontStyle.Normal,Soft(),TextAnchor.UpperLeft,true));
            string fx=Effects(selected); if(!string.IsNullOrEmpty(fx))GUI.Label(new Rect(r.x+16,r.y+126,r.width-32,24),fx,Style(10,FontStyle.Bold,new Color(.68f,.78f,.69f),TextAnchor.MiddleLeft,false));
            if(equipment!=null&&equipment.IsEquipped(selected))GUI.Label(new Rect(r.x+16,r.y+153,r.width-32,20),"Skick "+Mathf.RoundToInt(equipment.Condition(selected))+"%   •   Fukt "+Mathf.RoundToInt(equipment.Wetness(selected))+"%   •   Effektiv isolering "+equipment.EffectiveInsulation(selected).ToString("0.0"),Style(10,FontStyle.Bold,Muted(),TextAnchor.MiddleLeft,false));
            string action=ActionLabel(); if(!string.IsNullOrEmpty(action)&&player.CountItem(selected)>0) ActionButton(new Rect(r.x+16,r.yMax-50,150,34),action,ActivateSelected);
            if(equipment!=null&&equipment.IsEquipped(selected)&&player.CountItem(selected)==0) ActionButton(new Rect(r.x+16,r.yMax-50,150,34),"TA AV",()=>equipment.ToggleClothing(selected));
        }

        void Backdrop(){Color o=GUI.color;GUI.color=new Color(.006f,.014f,.017f,.86f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=o;}
        void SectionTitle(Rect r,string s){GUI.Label(new Rect(r.x+4,r.y+4,r.width-8,26),s,Style(13,FontStyle.Bold,Ink(),TextAnchor.MiddleLeft,false));}
        bool ActionButton(Rect r,string text){Fill(r,new Color(.10f,.18f,.19f));Border(r,new Color(.45f,.62f,.60f),1);GUI.Label(r,text,Style(10,FontStyle.Bold,Ink(),TextAnchor.MiddleCenter,false));return GUI.Button(r,GUIContent.none,GUIStyle.none);}
        void ActionButton(Rect r,string text,Action action){if(ActionButton(r,text))action?.Invoke();}
        void DrawIcon(Rect r,Texture2D t){if(!t)return;Fill(r,new Color(.055f,.085f,.091f));Border(r,new Color(.17f,.28f,.29f),1);GUI.DrawTexture(r,t,ScaleMode.ScaleToFit,true);}
        GUIStyle Style(int size,FontStyle fs,Color color,TextAnchor align,bool wrap){GUIStyle s=new GUIStyle(GUI.skin.label){fontSize=size,fontStyle=fs,alignment=align,wordWrap=wrap};s.normal.textColor=color;return s;}
        void Panel(Rect r,Color fill,Color border){Fill(r,fill);Border(r,border,1);} void Fill(Rect r,Color c){Color o=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=o;} void Border(Rect r,Color c,int w){Fill(new Rect(r.x,r.y,r.width,w),c);Fill(new Rect(r.x,r.yMax-w,r.width,w),c);Fill(new Rect(r.x,r.y,w,r.height),c);Fill(new Rect(r.xMax-w,r.y,w,r.height),c);} void Line(Rect r,Color c){Fill(r,c);}
        Color Ink()=>new Color(.90f,.93f,.90f); Color Soft()=>new Color(.72f,.78f,.76f); Color Muted()=>new Color(.55f,.65f,.64f);
    }
}
