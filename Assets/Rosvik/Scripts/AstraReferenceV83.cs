using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rosvik.Blackout {
    [DefaultExecutionOrder(12000)]
    public sealed class BackpackLootPresentationV83 : MonoBehaviour {
        static readonly Dictionary<string,float> fallbackWeights=new Dictionary<string,float>(StringComparer.OrdinalIgnoreCase){
            {"Ficklampa",.38f},{"Batterier",.18f},{"Vattenflaska",1.05f},{"Läsk",.38f},{"Sportdryck",.58f},{"Energibar",.09f},{"Konservburk",.46f},{"Soppa",.52f},{"Kex",.22f},{"Choklad",.10f},{"Äpple",.16f},{"Förband",.08f},{"Tejp",.16f},{"Sporttejp",.12f},{"Tändare",.05f},{"Multiverktyg",.28f},{"Nyckelknippa",.22f},{"Säkring",.08f},{"Penna",.01f},{"Vinterjacka",1.85f},{"Regnjacka",.72f},{"Ulltröja",.66f},{"Hoodie",.78f},{"Mössa",.14f},{"Handskar",.18f},{"Kängor",1.65f},{"Tygbit",.12f},{"Metallskrot",.55f},{"Träspill",.35f},{"Bränsle",.65f},{"Improviserad fackla",.70f},{"Varm soppa",.48f},{"Värktabletter",.06f},{"Antiseptisk spray",.18f},{"Elastisk linda",.11f}
        };
        static readonly Dictionary<string,string> extraDesc=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase){
            {"Tygbit","Tyg från gamla textilier. Bra till förband, reparation och crafting."},{"Metallskrot","Små metalldelar för reparationer och tillverkning."},{"Träspill","Torrt trä. Kan eldas i vedspisen och användas vid crafting."},{"Bränsle","Bränsle till små värmekällor och framtida motorer."},{"Improviserad fackla","En enkel reservljuskälla med begränsad brinntid."},{"Varm soppa","Uppvärmd mat som ger mättnad, vätska och kroppsvärme."},{"Värktabletter","Dämpar smärta tillfälligt. Behandlar inte själva skadan."},{"Antiseptisk spray","Rengör sår och minskar risken för infektion."},{"Elastisk linda","Stabiliserar en stukning och hjälper en skadad kroppsdel."},{"Vandringsryggsäck","En större ryggsäck som låter dig bära mer utrustning."}
        };
        static readonly Dictionary<string,string> extraIcons=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase){
            {"Tygbit","bandage"},{"Metallskrot","multitool"},{"Träspill","misc"},{"Bränsle","lighter"},{"Improviserad fackla","flashlight"},{"Varm soppa","soup"},{"Värktabletter","bandage"},{"Antiseptisk spray","bandage"},{"Elastisk linda","bandage"}
        };

        CoziPlayerV57 player;
        SurvivalSystemsV69 survival;
        SurvivalEquipmentV76 equipment;
        SurvivalInventoryV76 legacyInventory;
        SurvivalLootTransferV74 transfer;
        SurvivalPresentationV73 presentation;
        Dictionary<string,SurvivalSystemsV69.ItemDef> defs;
        FieldInfo transferActiveField, panelField, playerInvField, survivalPanelField, weightsField;
        Dictionary<string,float> legacyWeights;
        MethodInfo useItemMethod;
        bool inventoryOpen;
        bool ownsTransfer;
        LootContainerV74 activeContainer;
        string selected="";
        bool selectedFromContainer;
        Vector2 leftScroll,rightScroll;
        readonly Dictionary<string,Texture2D> icons=new Dictionary<string,Texture2D>(StringComparer.OrdinalIgnoreCase);

        public bool IsOpen => inventoryOpen || activeContainer!=null;

        void Awake(){
            player=GetComponent<CoziPlayerV57>();if(!player)player=FindFirstObjectByType<CoziPlayerV57>();
            survival=GetComponent<SurvivalSystemsV69>();if(!survival)survival=FindFirstObjectByType<SurvivalSystemsV69>();
            equipment=GetComponent<SurvivalEquipmentV76>();if(!equipment)equipment=gameObject.AddComponent<SurvivalEquipmentV76>();
            legacyInventory=GetComponent<SurvivalInventoryV76>();
            transfer=GetComponent<SurvivalLootTransferV74>();
            presentation=GetComponent<SurvivalPresentationV73>();
            var df=typeof(SurvivalSystemsV69).GetField("defs",BindingFlags.Static|BindingFlags.NonPublic);defs=df!=null?df.GetValue(null) as Dictionary<string,SurvivalSystemsV69.ItemDef>:null;
            transferActiveField=typeof(SurvivalLootTransferV74).GetField("active",BindingFlags.Instance|BindingFlags.NonPublic);
            panelField=typeof(SurvivalPresentationV73).GetField("panelOpen",BindingFlags.Instance|BindingFlags.NonPublic);
            playerInvField=typeof(CoziPlayerV57).GetField("inventoryOpen",BindingFlags.Instance|BindingFlags.NonPublic);
            survivalPanelField=typeof(SurvivalSystemsV69).GetField("panelOpen",BindingFlags.Instance|BindingFlags.NonPublic);
            weightsField=typeof(SurvivalInventoryV76).GetField("weights",BindingFlags.Static|BindingFlags.NonPublic);
            legacyWeights=weightsField!=null?weightsField.GetValue(null) as Dictionary<string,float>:null;
            useItemMethod=typeof(SurvivalSystemsV69).GetMethod("UseItem",BindingFlags.Instance|BindingFlags.NonPublic);
            if(legacyInventory)legacyInventory.enabled=false;
        }

        void Update(){
            if(!player)return;
            SuppressOldInventory();
            CaptureLootTransfer();
            Keyboard kb=Keyboard.current;
            if(kb==null)return;
            if(activeContainer!=null){
                player.externalUiBlocked=true;
                if(kb.escapeKey.wasPressedThisFrame||kb.iKey.wasPressedThisFrame||kb.tabKey.wasPressedThisFrame)CloseLoot();
                return;
            }
            if(kb.iKey.wasPressedThisFrame||kb.tabKey.wasPressedThisFrame){if(inventoryOpen)CloseInventory();else OpenInventory();}
            if(inventoryOpen&&kb.escapeKey.wasPressedThisFrame)CloseInventory();
            if(inventoryOpen)player.externalUiBlocked=true;
        }

        void SuppressOldInventory(){
            if(playerInvField!=null)playerInvField.SetValue(player,false);
            if(presentation&&panelField!=null)panelField.SetValue(presentation,false);
            if(survival&&survivalPanelField!=null)survivalPanelField.SetValue(survival,false);
            if(legacyInventory&&legacyInventory.enabled)legacyInventory.enabled=false;
        }

        void CaptureLootTransfer(){
            if(!transfer)return;
            if(activeContainer==null&&transfer.IsOpen){
                activeContainer=transferActiveField!=null?transferActiveField.GetValue(transfer) as LootContainerV74:null;
                if(activeContainer){
                    ownsTransfer=true;transfer.enabled=false;inventoryOpen=false;selected="";selectedFromContainer=true;leftScroll=rightScroll=Vector2.zero;player.externalUiBlocked=true;Cursor.visible=true;Cursor.lockState=CursorLockMode.None;
                }
            }
        }

        void OpenInventory(){inventoryOpen=true;selected="";leftScroll=Vector2.zero;player.externalUiBlocked=true;Cursor.visible=true;Cursor.lockState=CursorLockMode.None;}
        void CloseInventory(){inventoryOpen=false;selected="";if(activeContainer==null&&player)player.externalUiBlocked=false;}
        void CloseLoot(){
            selected="";activeContainer=null;
            if(transfer&&ownsTransfer){transfer.enabled=true;ownsTransfer=false;transfer.Close();}
            if(player)player.externalUiBlocked=false;
        }
        void OnDisable(){if(transfer&&ownsTransfer)transfer.enabled=true;if(player)player.externalUiBlocked=false;}

        float Weight(string item){if(legacyWeights!=null&&legacyWeights.TryGetValue(item,out float w))return w;if(fallbackWeights.TryGetValue(item,out w))return w;return .25f;}
        float CurrentWeight(){float t=0;if(player!=null)foreach(var kv in player.Inventory)t+=Weight(kv.Key)*kv.Value;return t;}
        float Capacity(){if(legacyInventory)return legacyInventory.backpackCapacityKg;if(transfer)return transfer.backpackCapacityKg;return 34f;}
        string IconKey(string item){if(defs!=null&&defs.TryGetValue(item,out var d)&&!string.IsNullOrWhiteSpace(d.icon))return d.icon;if(extraIcons.TryGetValue(item,out string k))return k;return "misc";}
        Texture2D Icon(string item){string k=IconKey(item);if(icons.TryGetValue(k,out var t)&&t)return t;t=Resources.Load<Texture2D>("ItemIcons/"+k);icons[k]=t;return t?t:Texture2D.whiteTexture;}
        string Description(string item){if(defs!=null&&defs.TryGetValue(item,out var d)&&!string.IsNullOrWhiteSpace(d.description))return d.description;if(extraDesc.TryGetValue(item,out string s))return s;return "Ett användbart föremål. Var du hittade det kan säga något om vad det är bra till.";}
        string Kind(string item){if(defs==null||!defs.TryGetValue(item,out var d))return "ÖVRIGT";switch(d.kind){case SurvivalSystemsV69.ItemKind.Food:return "MAT";case SurvivalSystemsV69.ItemKind.Drink:return "DRYCK";case SurvivalSystemsV69.ItemKind.Medical:return "VÅRD";case SurvivalSystemsV69.ItemKind.Clothing:return "KLÄDER";case SurvivalSystemsV69.ItemKind.Tool:return "VERKTYG";case SurvivalSystemsV69.ItemKind.Material:return "MATERIAL";default:return "ÖVRIGT";}}
        int Condition(string item){if(equipment&&equipment.IsEquipped(item))return Mathf.RoundToInt(equipment.Condition(item));unchecked{int h=23;foreach(char c in item)h=h*31+c;return 72+Mathf.Abs(h%29);}}
        string Effects(string item){
            if(defs==null||!defs.TryGetValue(item,out var d))return "";List<string> p=new List<string>();
            if(Mathf.Abs(d.hunger)>.01f)p.Add("Mat "+Sign(d.hunger));if(Mathf.Abs(d.thirst)>.01f)p.Add("Vätska "+Sign(d.thirst));if(Mathf.Abs(d.health)>.01f)p.Add("Hälsa "+Sign(d.health));
            if(d.kind==SurvivalSystemsV69.ItemKind.Clothing){p.Add("Isolering "+d.insulation.ToString("0.0"));p.Add("Vattenskydd "+d.waterproof.ToString("0.0"));}
            return string.Join("  •  ",p);
        }
        string Sign(float v)=>(v>=0?"+":"")+Mathf.RoundToInt(v);

        int MaxByWeight(string item,int wanted){float w=Weight(item);if(w<=.001f)return wanted;float free=Mathf.Max(0,Capacity()-CurrentWeight());return Mathf.Clamp(Mathf.FloorToInt((free+.0001f)/w),0,wanted);}
        void TakeSelected(bool all){
            if(activeContainer==null||!selectedFromContainer||string.IsNullOrEmpty(selected))return;int have=activeContainer.Count(selected);if(have<=0)return;int wanted=all?have:1;int take=MaxByWeight(selected,wanted);if(take<=0){player.ShowToast("Ryggsäcken är för tung",1.6f);return;}int removed=activeContainer.Remove(selected,take);if(removed>0){player.AddItem(selected,removed);player.ShowToast("Tog "+selected+(removed>1?" x"+removed:""),1.2f);}if(activeContainer.Count(selected)<=0)selected="";
        }
        void PutSelected(bool all){
            if(activeContainer==null||selectedFromContainer||string.IsNullOrEmpty(selected))return;int have=player.CountItem(selected);if(have<=0)return;int n=all?have:1;if(player.ConsumeItem(selected,n)){activeContainer.Add(selected,n);player.ShowToast("Lade tillbaka "+selected+(n>1?" x"+n:""),1.2f);}if(player.CountItem(selected)<=0)selected="";
        }
        void UseSelected(){
            if(string.IsNullOrEmpty(selected)||selectedFromContainer||!inventoryOpen)return;
            if(defs!=null&&defs.TryGetValue(selected,out var d)&&d.kind==SurvivalSystemsV69.ItemKind.Clothing){equipment.ToggleClothing(selected);return;}
            if(string.Equals(selected,"Varm soppa",StringComparison.OrdinalIgnoreCase)){if(player.ConsumeItem(selected,1)){survival.hunger=Mathf.Clamp(survival.hunger+40,0,100);survival.thirst=Mathf.Clamp(survival.thirst+18,0,100);survival.warmth=Mathf.Clamp(survival.warmth+24,0,100);player.ShowToast("Åt varm soppa",1.4f);}return;}
            if(defs!=null&&defs.TryGetValue(selected,out var def)&&useItemMethod!=null)useItemMethod.Invoke(survival,new object[]{def});
        }
        string Action(){if(string.IsNullOrEmpty(selected)||selectedFromContainer)return "";if(defs!=null&&defs.TryGetValue(selected,out var d)){if(d.kind==SurvivalSystemsV69.ItemKind.Clothing)return equipment.IsEquipped(selected)?"TA AV":"TA PÅ";if(d.kind==SurvivalSystemsV69.ItemKind.Food)return "ÄT";if(d.kind==SurvivalSystemsV69.ItemKind.Drink)return "DRICK";if(d.kind==SurvivalSystemsV69.ItemKind.Medical)return "ANVÄND";}if(selected.Equals("Varm soppa",StringComparison.OrdinalIgnoreCase))return "ÄT";return "";}

        void OnGUI(){if(!IsOpen||!player)return;GUI.depth=-20000;if(activeContainer)DrawLoot();else DrawInventory();}

        void DrawInventory(){
            Dim();float m=Mathf.Max(22,Screen.width*.02f),top=44,bottom=42,gap=22;Rect panel=new Rect(m,top,Screen.width-2*m,Screen.height-top-bottom);Panel(panel);
            var title=Style(23,FontStyle.Bold,new Color(.93f,.94f,.91f),TextAnchor.MiddleLeft,false);var head=Style(14,FontStyle.Bold,new Color(.81f,.84f,.80f),TextAnchor.MiddleLeft,false);var text=Style(12,FontStyle.Normal,new Color(.73f,.76f,.73f),TextAnchor.UpperLeft,true);var tiny=Style(10,FontStyle.Normal,new Color(.57f,.61f,.58f),TextAnchor.MiddleLeft,false);
            GUI.Label(new Rect(panel.x+26,panel.y+18,420,30),"DET DU BÄR MED DIG",title);GUI.Label(new Rect(panel.x+26,panel.y+52,420,22),CurrentWeight().ToString("0.00")+" / "+Capacity().ToString("0")+" kg",head);Line(new Rect(panel.x+24,panel.y+84,panel.width-48,1));
            float colW=(panel.width-gap*3)/2f;Rect left=new Rect(panel.x+22,panel.y+104,colW,panel.height-142);Rect right=new Rect(left.xMax+gap,panel.y+104,colW,panel.height-142);
            GUI.Label(new Rect(left.x,left.y,220,24),"RYGGSÄCK",head);GUI.Label(new Rect(right.x,right.y,220,24),"PÅ KROPPEN / INFO",head);
            DrawItemList(new Rect(left.x,left.y+32,left.width,left.height-32),player.Inventory.OrderBy(k=>Kind(k.Key)).ThenBy(k=>k.Key),false,ref leftScroll);
            DrawEquipmentAndDetail(new Rect(right.x,right.y+32,right.width,right.height-32),head,text,tiny);
            GUI.Label(new Rect(panel.x+24,panel.yMax-30,panel.width-48,20),"I / TAB / ESC  tillbaka",tiny);
        }

        void DrawLoot(){
            Dim();float m=Mathf.Max(22,Screen.width*.02f),top=34,bottom=34,gap=24;Rect panel=new Rect(m,top,Screen.width-2*m,Screen.height-top-bottom);Panel(panel);
            var title=Style(23,FontStyle.Bold,new Color(.93f,.94f,.91f),TextAnchor.MiddleLeft,false);var head=Style(14,FontStyle.Bold,new Color(.81f,.84f,.80f),TextAnchor.MiddleLeft,false);var text=Style(12,FontStyle.Normal,new Color(.73f,.76f,.73f),TextAnchor.UpperLeft,true);var tiny=Style(10,FontStyle.Normal,new Color(.57f,.61f,.58f),TextAnchor.MiddleLeft,false);
            GUI.Label(new Rect(panel.x+26,panel.y+18,420,30),"DET DU BÄR MED DIG",title);GUI.Label(new Rect(panel.x+26,panel.y+51,520,22),CurrentWeight().ToString("0.00")+" / "+Capacity().ToString("0")+" kg",head);Line(new Rect(panel.x+24,panel.y+82,panel.width-48,1));
            float colW=(panel.width-gap*3)/2f;Rect left=new Rect(panel.x+22,panel.y+103,colW,panel.height-230);Rect right=new Rect(left.xMax+gap,panel.y+103,colW,panel.height-230);
            GUI.Label(new Rect(left.x,left.y,260,24),"RYGGSÄCK",head);GUI.Label(new Rect(right.x,right.y,right.width,24),(activeContainer.displayName??"BEHÅLLARE").ToUpperInvariant(),head);
            DrawItemList(new Rect(left.x,left.y+32,left.width,left.height-32),player.Inventory.OrderBy(k=>k.Key),false,ref leftScroll);
            DrawItemList(new Rect(right.x,right.y+32,right.width,right.height-32),activeContainer.Remaining.OrderBy(k=>k.Key),true,ref rightScroll);
            Rect detail=new Rect(panel.x+24,panel.yMax-118,panel.width-48,76);DrawSelectedDetail(detail,text,tiny);
            Rect lb=new Rect(left.x,panel.yMax-37,left.width,30),rb=new Rect(right.x,panel.yMax-37,right.width,30);
            bool canPut=!string.IsNullOrEmpty(selected)&&!selectedFromContainer;bool canTake=!string.IsNullOrEmpty(selected)&&selectedFromContainer;
            GUI.enabled=canPut;if(GUI.Button(lb,canPut?"Lägg tillbaka  •  SHIFT = alla":"Välj något i ryggsäcken")){PutSelected(Keyboard.current!=null&&(Keyboard.current.leftShiftKey.isPressed||Keyboard.current.rightShiftKey.isPressed));}GUI.enabled=true;
            GUI.enabled=canTake;if(GUI.Button(rb,canTake?"Ta  •  SHIFT = alla":"Välj något att ta")){TakeSelected(Keyboard.current!=null&&(Keyboard.current.leftShiftKey.isPressed||Keyboard.current.rightShiftKey.isPressed));}GUI.enabled=true;
        }

        void DrawItemList(Rect area,IEnumerable<KeyValuePair<string,int>> source,bool fromContainer,ref Vector2 scroll){
            var list=source.Where(k=>k.Value>0).ToList();float rowH=76f,content=Mathf.Max(area.height,list.Count*rowH);scroll=GUI.BeginScrollView(area,scroll,new Rect(0,0,area.width-18,content));
            for(int i=0;i<list.Count;i++){var kv=list[i];Rect row=new Rect(0,i*rowH,area.width-20,rowH-3);bool sel=string.Equals(selected,kv.Key,StringComparison.OrdinalIgnoreCase)&&selectedFromContainer==fromContainer;Fill(row,sel?new Color(.10f,.15f,.16f,.98f):new Color(.045f,.060f,.064f,.96f));Border(row,sel?new Color(.35f,.52f,.54f):new Color(.12f,.17f,.18f),sel?2:1);Rect ic=new Rect(row.x+7,row.y+6,62,62);DrawIcon(ic,Icon(kv.Key));GUI.Label(new Rect(row.x+78,row.y+10,row.width-88,24),kv.Key+" ×"+kv.Value,Style(13,FontStyle.Bold,new Color(.82f,.84f,.81f),TextAnchor.MiddleLeft,false));GUI.Label(new Rect(row.x+78,row.y+37,row.width-88,18),Condition(kv.Key)+"% skick   •   "+Weight(kv.Key).ToString("0.00")+" kg",Style(10,FontStyle.Normal,new Color(.60f,.64f,.61f),TextAnchor.MiddleLeft,false));if(GUI.Button(row,GUIContent.none,GUIStyle.none)){selected=kv.Key;selectedFromContainer=fromContainer;}}
            GUI.EndScrollView();
        }

        void DrawEquipmentAndDetail(Rect r,GUIStyle head,GUIStyle text,GUIStyle tiny){
            float y=r.y;string[] slots={"Head","Body","Mid","Hands","Feet"};foreach(string slot in slots){string item=equipment?equipment.SlotItem(slot):"";Rect row=new Rect(r.x,y,r.width,42);Fill(row,new Color(.045f,.060f,.064f,.96f));Border(row,new Color(.12f,.17f,.18f),1);GUI.Label(new Rect(row.x+8,row.y+4,90,16),SlotName(slot),tiny);GUI.Label(new Rect(row.x+102,row.y+4,row.width-110,18),string.IsNullOrEmpty(item)?"—":item,text);if(!string.IsNullOrEmpty(item)){GUI.Label(new Rect(row.x+102,row.y+22,row.width-110,16),Mathf.RoundToInt(equipment.Condition(item))+"% skick  •  "+Mathf.RoundToInt(equipment.Wetness(item))+"% fukt",tiny);if(GUI.Button(row,GUIContent.none,GUIStyle.none)){selected=item;selectedFromContainer=false;}}y+=47;}
            Rect d=new Rect(r.x,y+12,r.width,r.yMax-y-52);DrawSelectedDetail(d,text,tiny);string a=Action();if(!string.IsNullOrEmpty(a)&&GUI.Button(new Rect(r.x,r.yMax-36,150,30),a))UseSelected();
        }
        void DrawSelectedDetail(Rect d,GUIStyle text,GUIStyle tiny){if(string.IsNullOrEmpty(selected)){GUI.Label(d,"Välj ett föremål för att läsa vad det är, vad det väger och vad det gör.",text);return;}Rect ic=new Rect(d.x,d.y,64,64);DrawIcon(ic,Icon(selected));GUI.Label(new Rect(d.x+76,d.y,d.width-76,22),selected,Style(14,FontStyle.Bold,new Color(.87f,.89f,.85f),TextAnchor.MiddleLeft,false));GUI.Label(new Rect(d.x+76,d.y+22,d.width-76,18),Kind(selected)+"  •  "+Weight(selected).ToString("0.00")+" kg  •  "+Condition(selected)+"% skick",tiny);GUI.Label(new Rect(d.x+76,d.y+42,d.width-76,44),Description(selected),text);string fx=Effects(selected);if(!string.IsNullOrEmpty(fx))GUI.Label(new Rect(d.x+76,d.y+82,d.width-76,18),fx,tiny);}

        void Dim(){Color o=GUI.color;GUI.color=new Color(.008f,.018f,.022f,.94f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=o;}
        void Panel(Rect r){Fill(r,new Color(.035f,.075f,.088f,.985f));Border(r,new Color(.34f,.44f,.45f),1);}
        void DrawIcon(Rect r,Texture2D t){if(t)GUI.DrawTextureWithTexCoords(r,t,new Rect(0,1,1,-1),true);}
        GUIStyle Style(int s,FontStyle f,Color c,TextAnchor a,bool wrap){GUIStyle x=new GUIStyle(GUI.skin.label){fontSize=s,fontStyle=f,alignment=a,wordWrap=wrap};x.normal.textColor=c;return x;}
        void Fill(Rect r,Color c){Color o=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=o;}
        void Border(Rect r,Color c,int w){Fill(new Rect(r.x,r.y,r.width,w),c);Fill(new Rect(r.x,r.yMax-w,r.width,w),c);Fill(new Rect(r.x,r.y,w,r.height),c);Fill(new Rect(r.xMax-w,r.y,w,r.height),c);}
        void Line(Rect r){Fill(r,new Color(.22f,.31f,.32f,.9f));}
        string SlotName(string s){switch(s){case "Head":return "HUVUD";case "Body":return "YTTERPLAGG";case "Mid":return "MELLANLAGER";case "Hands":return "HÄNDER";case "Feet":return "FÖTTER";default:return s.ToUpperInvariant();}}
    }

    public sealed class RoofCutawayV83 : MonoBehaviour {
        public GameObject roofVisual;public Vector2 minXZ;public Vector2 maxXZ;CoziPlayerV57 player;bool lastInside;
        void Awake(){player=FindFirstObjectByType<CoziPlayerV57>();Apply(true);}void LateUpdate(){Apply(false);}void Apply(bool force){if(!player||!roofVisual)return;Vector3 p=player.transform.position;bool inside=p.x>minXZ.x&&p.x<maxXZ.x&&p.z>minXZ.y&&p.z<maxXZ.y;if(force||inside!=lastInside){lastInside=inside;roofVisual.SetActive(!inside);}}
    }

    [DefaultExecutionOrder(11000)]
    public sealed class FootprintTrailV83 : MonoBehaviour {
        public float spacing=.72f;public int poolSize=44;CoziPlayerV57 player;GameObject[] pool;int cursor;Vector3 lastStamp;float travel;Material mat;
        void Awake(){player=GetComponent<CoziPlayerV57>();if(!player)player=FindFirstObjectByType<CoziPlayerV57>();BuildPool();if(player)lastStamp=player.transform.position;}
        void BuildPool(){Shader s=Shader.Find("Universal Render Pipeline/Lit");if(!s||!s.isSupported)s=Shader.Find("Universal Render Pipeline/Simple Lit");if(!s||!s.isSupported)s=Shader.Find("Standard");if(!s)return;mat=new Material(s);Color c=new Color(.16f,.23f,.26f,1);if(mat.HasProperty("_BaseColor"))mat.SetColor("_BaseColor",c);if(mat.HasProperty("_Color"))mat.SetColor("_Color",c);pool=new GameObject[poolSize];for(int i=0;i<poolSize;i++){GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name="footprint";g.transform.localScale=new Vector3(.13f,.008f,.27f);var col=g.GetComponent<Collider>();if(col)Destroy(col);g.GetComponent<Renderer>().sharedMaterial=mat;g.SetActive(false);pool[i]=g;}}
        void Update(){if(!player||pool==null)return;Vector3 p=player.transform.position;Vector3 d=p-lastStamp;d.y=0;float m=d.magnitude;if(m>2f){lastStamp=p;travel=0;return;}travel+=m;lastStamp=p;if(travel<spacing)return;travel=0;if(IsIndoors(p))return;Stamp(p);}
        bool IsIndoors(Vector3 p){if(p.x>-19.5f&&p.x<45.5f&&p.z>-2.5f&&p.z<16.8f)return true;if(p.x>-36.5f&&p.x<-21.5f&&p.z>2.2f&&p.z<16.2f)return true;if(p.x>-36.3f&&p.x<-21.7f&&p.z>22.1f&&p.z<35.8f)return true;if(p.x>51.5f&&p.x<65.4f&&p.z>4.3f&&p.z<15.5f)return true;return false;}
        void Stamp(Vector3 p){GameObject g=pool[cursor++%pool.Length];g.SetActive(true);Vector3 side=player.transform.right*((cursor&1)==0?.16f:-.16f);g.transform.position=p-side+Vector3.up*.012f-player.transform.forward*.18f;g.transform.rotation=Quaternion.Euler(0,player.transform.eulerAngles.y,0);}
        void OnDestroy(){if(mat)Destroy(mat);}
    }

    [DefaultExecutionOrder(10500)]
    public sealed class AtmosphereMixV83 : MonoBehaviour {
        WorldSoundscapeV80 v80;CoziPlayerV57 player;AudioSource source;AudioClip gust,houseTick,metalRattle;float next;
        void Awake(){player=GetComponent<CoziPlayerV57>();if(!player)player=FindFirstObjectByType<CoziPlayerV57>();v80=FindFirstObjectByType<WorldSoundscapeV80>();if(v80){v80.masterVolume=.96f;v80.windVolume=.66f;v80.footstepVolume=.24f;v80.interactionVolume=.82f;}source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0f;source.priority=80;gust=NoiseBurst("V83 wind gust",2.7f,.34f,831);houseTick=ToneNoise("V83 house tick",.22f,.25f,832);metalRattle=ToneNoise("V83 distant metal",.55f,.18f,833);next=Time.time+4f;}
        void Update(){if(!player||!source)return;if(Time.time<next)return;bool inside=Inside(player.transform.position);next=Time.time+UnityEngine.Random.Range(inside?8f:5f,inside?17f:12f);AudioClip c=inside?houseTick:(UnityEngine.Random.value<.72f?gust:metalRattle);source.pitch=UnityEngine.Random.Range(.92f,1.06f);source.PlayOneShot(c,inside?.34f:.52f);}
        bool Inside(Vector3 p){if(p.x>-19.5f&&p.x<45.5f&&p.z>-2.5f&&p.z<16.8f)return true;if(p.x>-36.5f&&p.x<-21.5f&&p.z>2.2f&&p.z<16.2f)return true;if(p.x>-36.3f&&p.x<-21.7f&&p.z>22.1f&&p.z<35.8f)return true;if(p.x>51.5f&&p.x<65.4f&&p.z>4.3f&&p.z<15.5f)return true;return false;}
        AudioClip NoiseBurst(string n,float sec,float amp,int seed){int sr=22050,count=Mathf.RoundToInt(sr*sec);float[] data=new float[count];System.Random r=new System.Random(seed);float low=0;for(int i=0;i<count;i++){float u=i/(float)(count-1);float env=Mathf.Sin(Mathf.PI*u);float x=(float)(r.NextDouble()*2-1);low=low*.975f+x*.025f;data[i]=low*env*amp;}AudioClip c=AudioClip.Create(n,count,1,sr,false);c.SetData(data,0);return c;}
        AudioClip ToneNoise(string n,float sec,float amp,int seed){int sr=22050,count=Mathf.RoundToInt(sr*sec);float[] data=new float[count];System.Random r=new System.Random(seed);float lp=0;for(int i=0;i<count;i++){float u=i/(float)(count-1);float env=Mathf.Pow(1-u,2);float x=(float)(r.NextDouble()*2-1);lp=lp*.82f+x*.18f;data[i]=lp*env*amp;}AudioClip c=AudioClip.Create(n,count,1,sr,false);c.SetData(data,0);return c;}
    }
}
