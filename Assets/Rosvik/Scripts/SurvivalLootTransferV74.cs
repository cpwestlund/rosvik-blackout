using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rosvik.Blackout {
    public sealed class LootContainerV74 : MonoBehaviour {
        public string displayName = "behållaren";
        public float radius = 2.25f;
        public Transform movingPart;
        public Transform movingPart2;
        public Vector3 closedEuler;
        public Vector3 openEuler;
        public Vector3 closedEuler2;
        public Vector3 openEuler2;
        public Transform revealOnOpen;
        public Renderer highlightRenderer;
        public float animationTime = .30f;
        public string[] items = Array.Empty<string>();
        public int[] counts = Array.Empty<int>();

        readonly Dictionary<string,int> remaining = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
        bool opened;
        bool animating;
        MaterialPropertyBlock block;
        Color originalColor = Color.white;
        int colorProperty = -1;

        public bool IsOpen => opened;
        public bool IsAnimating => animating;
        public bool HasLoot => remaining.Any(kv => kv.Value > 0);
        public IReadOnlyDictionary<string,int> Remaining => remaining;

        void Awake() {
            RebuildContents();
            if (revealOnOpen) revealOnOpen.gameObject.SetActive(opened);
            CacheHighlight();
        }

        public void RebuildContents() {
            remaining.Clear();
            if (items == null) return;
            for (int i=0;i<items.Length;i++) {
                string item = items[i];
                if (string.IsNullOrWhiteSpace(item)) continue;
                int count = counts != null && i < counts.Length ? Mathf.Max(1, counts[i]) : 1;
                if (!remaining.ContainsKey(item)) remaining[item] = 0;
                remaining[item] += count;
            }
        }

        public void RefreshHighlight() { CacheHighlight(); }

        void CacheHighlight() {
            if (!highlightRenderer || !highlightRenderer.sharedMaterial) return;
            Material m = highlightRenderer.sharedMaterial;
            if (m.HasProperty("_BaseColor")) { colorProperty = Shader.PropertyToID("_BaseColor"); originalColor = m.GetColor("_BaseColor"); }
            else if (m.HasProperty("_Color")) { colorProperty = Shader.PropertyToID("_Color"); originalColor = m.GetColor("_Color"); }
            block = new MaterialPropertyBlock();
        }

        public void SetFocused(bool focused) {
            if (!highlightRenderer || colorProperty < 0 || block == null) return;
            highlightRenderer.GetPropertyBlock(block);
            block.SetColor(colorProperty, focused ? Color.Lerp(originalColor, new Color(.85f,.72f,.36f), .42f) : originalColor);
            highlightRenderer.SetPropertyBlock(block);
        }

        public int Count(string item) => !string.IsNullOrWhiteSpace(item) && remaining.TryGetValue(item,out int n) ? n : 0;

        public int Remove(string item,int count) {
            int have = Count(item);
            int take = Mathf.Clamp(count,0,have);
            if (take <= 0) return 0;
            remaining[item] = have - take;
            if (remaining[item] <= 0) remaining.Remove(item);
            return take;
        }

        public void Add(string item,int count) {
            if (string.IsNullOrWhiteSpace(item) || count <= 0) return;
            if (!remaining.ContainsKey(item)) remaining[item] = 0;
            remaining[item] += count;
        }

        public void Interact(SurvivalLootTransferV74 ui, bool forceClose=false) {
            if (animating || !ui) return;
            if (forceClose && opened) { StartCoroutine(Animate(false,null)); return; }
            if (!opened && (movingPart || movingPart2)) {
                StartCoroutine(Animate(true, () => { if (HasLoot) ui.Open(this); }));
                return;
            }
            if (HasLoot) ui.Open(this);
            else if (opened && (movingPart || movingPart2)) StartCoroutine(Animate(false,null));
            else ui.Open(this);
        }

        IEnumerator Animate(bool targetOpen, Action done) {
            animating = true;
            Quaternion a0 = movingPart ? movingPart.localRotation : Quaternion.identity;
            Quaternion a1 = movingPart ? Quaternion.Euler(targetOpen ? openEuler : closedEuler) : Quaternion.identity;
            Quaternion b0 = movingPart2 ? movingPart2.localRotation : Quaternion.identity;
            Quaternion b1 = movingPart2 ? Quaternion.Euler(targetOpen ? openEuler2 : closedEuler2) : Quaternion.identity;
            float t=0f;
            while (t<1f) {
                t += Time.deltaTime / Mathf.Max(.08f,animationTime);
                float e = 1f - Mathf.Pow(1f-Mathf.Clamp01(t),3f);
                if (movingPart) movingPart.localRotation = Quaternion.Slerp(a0,a1,e);
                if (movingPart2) movingPart2.localRotation = Quaternion.Slerp(b0,b1,e);
                yield return null;
            }
            if (movingPart) movingPart.localRotation = a1;
            if (movingPart2) movingPart2.localRotation = b1;
            opened = targetOpen;
            if (revealOnOpen) revealOnOpen.gameObject.SetActive(opened);
            animating = false;
            done?.Invoke();
        }
    }

    [DefaultExecutionOrder(7000)]
    public sealed class SurvivalLootTransferV74 : MonoBehaviour {
        static readonly Dictionary<string,float> weights = new Dictionary<string,float>(StringComparer.OrdinalIgnoreCase) {
            {"Ficklampa",.38f},{"Batterier",.18f},{"Vattenflaska",1.05f},{"Läsk",.38f},{"Sportdryck",.58f},
            {"Energibar",.09f},{"Konservburk",.46f},{"Soppa",.52f},{"Kex",.22f},{"Choklad",.10f},{"Äpple",.16f},
            {"Förband",.08f},{"Tejp",.16f},{"Sporttejp",.12f},{"Tändare",.05f},{"Multiverktyg",.28f},{"Nyckelknippa",.22f},
            {"Säkring",.08f},{"Penna",.01f},{"Vinterjacka",1.85f},{"Regnjacka",.72f},{"Ulltröja",.66f},{"Hoodie",.78f},
            {"Mössa",.14f},{"Handskar",.18f},{"Kängor",1.65f},{"Tygbit",.12f},{"Metallskrot",.55f},{"Träspill",.35f},
            {"Bränsle",.65f},{"Improviserad fackla",.70f},{"Varm soppa",.48f}
        };
        static readonly Dictionary<string,string> extraIcons = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase) {
            {"Tygbit","bandage"},{"Metallskrot","multitool"},{"Träspill","misc"},{"Bränsle","lighter"},
            {"Improviserad fackla","flashlight"},{"Varm soppa","soup"}
        };

        public float backpackCapacityKg = 34f;
        public float discoveryRange = 5.0f;
        public float interactionRange = 2.65f;

        CoziPlayerV57 player;
        SurvivalPresentationV73 presentation;
        LootContainerV74 nearby;
        LootContainerV74 active;
        float nearbyDistance;
        float nextScan;
        Vector2 leftScroll;
        Vector2 rightScroll;
        string selectedItem = "";
        bool selectedFromContainer = true;
        readonly Dictionary<string,Texture2D> iconCache = new Dictionary<string,Texture2D>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string,SurvivalSystemsV69.ItemDef> defs;
        FieldInfo defsField;

        public bool IsOpen => active != null;

        void Awake() {
            player = GetComponent<CoziPlayerV57>(); if (!player) player = FindFirstObjectByType<CoziPlayerV57>();
            presentation = GetComponent<SurvivalPresentationV73>(); if (!presentation) presentation = FindFirstObjectByType<SurvivalPresentationV73>();
            defsField = typeof(SurvivalSystemsV69).GetField("defs",BindingFlags.Static|BindingFlags.NonPublic);
            defs = defsField != null ? defsField.GetValue(null) as Dictionary<string,SurvivalSystemsV69.ItemDef> : null;
            RepairLegacyContainersAtRuntime();
            FixAllCabinetSwings();
            Scan();
        }

        void RepairLegacyContainersAtRuntime() {
            CozyInteractableV57[] legacy = FindObjectsByType<CozyInteractableV57>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            foreach (CozyInteractableV57 x in legacy) {
                if (!x || !x.gameObject.scene.IsValid()) continue;
                bool hasPrimary=!string.IsNullOrWhiteSpace(x.itemName);
                bool hasExtras=x.extraItems!=null&&x.extraItems.Any(s=>!string.IsNullOrWhiteSpace(s));
                if (!hasPrimary && !hasExtras) continue;
                if (x.kind!=CozyInteractableV57.Kind.Cabinet && x.kind!=CozyInteractableV57.Kind.Loot) continue;
                LootContainerV74 c=x.GetComponent<LootContainerV74>();
                if (!c) c=x.gameObject.AddComponent<LootContainerV74>();
                c.displayName=x.displayName;c.radius=Mathf.Max(2.15f,x.radius);
                c.movingPart=x.movingPart;c.movingPart2=x.movingPart2;
                c.closedEuler=x.closedEuler;c.closedEuler2=x.closedEuler2;
                c.openEuler=x.openEuler;c.openEuler2=x.openEuler2;
                c.revealOnOpen=x.revealOnOpen;c.highlightRenderer=x.highlightRenderer;c.animationTime=Mathf.Max(.30f,x.animationTime);
                List<string> its=new List<string>();List<int> cnt=new List<int>();
                if(hasPrimary){its.Add(x.itemName);cnt.Add(1);}
                if(x.extraItems!=null)for(int i=0;i<x.extraItems.Length;i++){
                    string item=x.extraItems[i];if(string.IsNullOrWhiteSpace(item))continue;
                    int n=x.extraCounts!=null&&i<x.extraCounts.Length?Mathf.Max(1,x.extraCounts[i]):1;
                    its.Add(item);cnt.Add(n);
                }
                c.items=its.ToArray();c.counts=cnt.ToArray();c.RebuildContents();c.RefreshHighlight();
                Destroy(x);
            }
        }

        void FixAllCabinetSwings() {
            foreach (LootContainerV74 c in FindObjectsByType<LootContainerV74>(FindObjectsSortMode.None)) {
                if(!c)continue;
                c.radius=Mathf.Max(c.radius,2.15f);
                c.openEuler=CorrectSwing(c.movingPart,c.openEuler,c.closedEuler);
                c.openEuler2=CorrectSwing(c.movingPart2,c.openEuler2,c.closedEuler2);
            }
        }

        Vector3 CorrectSwing(Transform hinge,Vector3 open,Vector3 closed) {
            if(!hinge||hinge.childCount==0)return open;
            if(Mathf.Abs(open.y)<30f||Mathf.Abs(open.y)<Mathf.Abs(open.x))return open;
            string n=hinge.name.ToLowerInvariant();
            float sign=0f;
            if(n.Contains("left")||n.Contains("vänster"))sign=1f;
            else if(n.Contains("right")||n.Contains("höger"))sign=-1f;
            else {
                Transform leaf=hinge.GetChild(0);
                if(Mathf.Abs(leaf.localPosition.x)>.02f)sign=Mathf.Sign(leaf.localPosition.x);
            }
            if(Mathf.Approximately(sign,0f))sign=Mathf.Sign(open.y);
            return new Vector3(closed.x,sign*88f,closed.z);
        }

        void Update() {
            if (!player) return;
            Keyboard kb = Keyboard.current;
            if (active) {
                if (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame)) Close();
                return;
            }
            if (Time.time >= nextScan) { nextScan=Time.time+.06f; Scan(); }
            if (!nearby || kb == null || !kb.eKey.wasPressedThisFrame) return;
            float reach=Mathf.Max(interactionRange,nearby.radius);
            if(nearbyDistance>reach)return;
            bool close = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
            nearby.Interact(this,close);
        }

        void LateUpdate() {
            if (!active || !player) return;
            player.walkSpeed = 0f;
            player.sprintSpeed = 0f;
        }

        void Scan() {
            LootContainerV74 best=null; float bestD=float.MaxValue;
            Vector3 here=player?player.transform.position:Vector3.zero;
            foreach (LootContainerV74 c in FindObjectsByType<LootContainerV74>(FindObjectsSortMode.None)) {
                if (!c || c.IsAnimating) continue;
                Vector3 d=c.transform.position-here; d.y=0; float dist=d.magnitude;
                if (dist<=discoveryRange && dist<bestD) { best=c; bestD=dist; }
            }
            if (nearby!=best) { if (nearby) nearby.SetFocused(false); nearby=best; if (nearby) nearby.SetFocused(true); }
            nearbyDistance=bestD;
        }

        public void Open(LootContainerV74 container) {
            if (!container || !player) return;
            active=container;
            selectedItem="";
            selectedFromContainer=true;
            leftScroll=rightScroll=Vector2.zero;
            Cursor.visible=true;
            Cursor.lockState=CursorLockMode.None;
            if (presentation) presentation.enabled=false;
            player.externalUiBlocked=true;
        }

        public void Close() {
            active=null;
            selectedItem="";
            if (presentation) presentation.enabled=true;
            if (player) player.externalUiBlocked=false;
        }

        float ItemWeight(string item) => weights.TryGetValue(item,out float w) ? w : .25f;
        float CurrentWeight() { float total=0f; foreach(var kv in player.Inventory) total+=ItemWeight(kv.Key)*kv.Value; return total; }
        string IconKey(string item) { if (defs!=null && defs.TryGetValue(item,out var d)) return d.icon; if (extraIcons.TryGetValue(item,out string k)) return k; return "misc"; }
        Texture2D Icon(string item) { string key=IconKey(item); if(iconCache.TryGetValue(key,out var t)&&t)return t; t=Resources.Load<Texture2D>("ItemIcons/"+key); iconCache[key]=t; return t?t:Texture2D.whiteTexture; }

        int MaxTakeByWeight(string item,int wanted) {
            float w=ItemWeight(item); if(w<=.001f)return wanted;
            float free=Mathf.Max(0f,backpackCapacityKg-CurrentWeight());
            return Mathf.Clamp(Mathf.FloorToInt((free+.0001f)/w),0,wanted);
        }

        void Take(string item,int requested) {
            if (!active || string.IsNullOrEmpty(item)) return;
            int available=active.Count(item); if(available<=0)return;
            int n=MaxTakeByWeight(item,Mathf.Min(requested,available));
            if(n<=0){player.ShowToast("Ryggsäcken är för tung",1.8f);return;}
            n=active.Remove(item,n); if(n<=0)return;
            player.AddItem(item,n);
            player.ShowToast("Tog "+item+(n>1?" x"+n:""),1.2f);
            if(active.Count(item)<=0)selectedItem="";
        }

        void Return(string item,int requested) {
            if (!active || string.IsNullOrEmpty(item)) return;
            int have=player.CountItem(item); int n=Mathf.Min(requested,have); if(n<=0)return;
            if(player.ConsumeItem(item,n)) active.Add(item,n);
            if(player.CountItem(item)<=0)selectedItem="";
        }

        void TakeAll() {
            if(!active)return;
            foreach(var kv in active.Remaining.ToList()) {
                int before=active.Count(kv.Key);
                Take(kv.Key,before);
                if(active.Count(kv.Key)==before) break;
            }
        }

        void OnGUI() {
            if (!player) return;
            GUI.depth=-10000;
            if (!active) { DrawPrompt(); return; }
            DrawTransfer();
        }

        void DrawPrompt() {
            if(!nearby || nearbyDistance>discoveryRange)return;
            GUIStyle tiny=Style(10,FontStyle.Bold,new Color(.80f,.80f,.72f),TextAnchor.MiddleCenter,false);
            bool reach=nearbyDistance<=Mathf.Max(interactionRange,nearby.radius);
            Camera cam=Camera.main;
            if(cam){Vector3 sp=cam.WorldToScreenPoint(nearby.transform.position+Vector3.up*1.25f);if(sp.z>0){float y=Screen.height-sp.y;Rect tag=new Rect(sp.x-68,y-14,136,24);Panel(tag,new Color(.02f,.03f,.027f,.86f),new Color(.42f,.44f,.37f,.8f));GUI.Label(tag,reach?(nearby.HasLoot?"E  SÖK":"E  ÖPPNA/STÄNG"):"SÖKBART",tiny);}}
            if(reach){Rect p=new Rect(Screen.width*.5f-220,Screen.height-62,440,36);Panel(p,new Color(.02f,.03f,.027f,.94f),new Color(.46f,.48f,.40f,.9f));string action=nearby.HasLoot?"E  ÖPPNA / SÖK":"E  ÖPPNA / STÄNG";if(nearby.IsOpen&&nearby.HasLoot)action="E  SÖK   •   SHIFT+E STÄNG";GUI.Label(p,action+"   "+nearby.displayName,tiny);}
        }

        void DrawTransfer() {
            Color old=GUI.color;GUI.color=new Color(.004f,.007f,.006f,.92f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=old;
            GUIStyle title=Style(20,FontStyle.Bold,new Color(.90f,.90f,.83f),TextAnchor.MiddleLeft,false);
            GUIStyle tiny=Style(10,FontStyle.Normal,new Color(.63f,.66f,.61f),TextAnchor.MiddleLeft,false);
            GUIStyle center=Style(10,FontStyle.Bold,new Color(.86f,.87f,.80f),TextAnchor.MiddleCenter,true);

            float margin=Mathf.Max(18,Screen.width*.025f),gap=100,top=54,bottom=52;
            float width=(Screen.width-margin*2-gap)*.5f;
            Rect left=new Rect(margin,top,width,Screen.height-top-bottom);
            Rect right=new Rect(left.xMax+gap,top,width,Screen.height-top-bottom);
            Panel(left,new Color(.024f,.031f,.029f,.985f),new Color(.31f,.34f,.30f,.95f));
            Panel(right,new Color(.024f,.031f,.029f,.985f),new Color(.31f,.34f,.30f,.95f));

            GUI.Label(new Rect(left.x+18,left.y+14,left.width-36,28),active.displayName.ToUpperInvariant(),title);
            GUI.Label(new Rect(right.x+18,right.y+14,220,28),"RYGGSÄCK",title);
            GUIStyle kg=Style(11,FontStyle.Bold,CurrentWeight()>backpackCapacityKg?new Color(.90f,.40f,.28f):new Color(.68f,.70f,.64f),TextAnchor.MiddleRight,false);
            GUI.Label(new Rect(right.x+right.width-190,right.y+17,170,22),CurrentWeight().ToString("0.0")+" / "+backpackCapacityKg.ToString("0")+" KG",kg);
            Line(new Rect(left.x+18,left.y+50,left.width-36,1),new Color(.22f,.25f,.22f,.9f));
            Line(new Rect(right.x+18,right.y+50,right.width-36,1),new Color(.22f,.25f,.22f,.9f));

            DrawGrid(left,active.Remaining.ToList(),true,ref leftScroll,center,tiny);
            DrawGrid(right,player.Inventory.ToList(),false,ref rightScroll,center,tiny);

            float cx=left.xMax+gap*.5f-39,cy=Screen.height*.5f-82;
            if(!string.IsNullOrEmpty(selectedItem)){
                if(selectedFromContainer){
                    int stack=active.Count(selectedItem);
                    if(GUI.Button(new Rect(cx,cy,78,32),"TA 1  >"))Take(selectedItem,1);
                    if(GUI.Button(new Rect(cx,cy+40,78,32),"TA ALLA >"))Take(selectedItem,Mathf.Max(1,stack));
                } else {
                    int stack=player.CountItem(selectedItem);
                    if(GUI.Button(new Rect(cx,cy,78,32),"<  LÄGG 1"))Return(selectedItem,1);
                    if(GUI.Button(new Rect(cx,cy+40,78,32),"<  ALLA"))Return(selectedItem,Mathf.Max(1,stack));
                }
            }

            Rect footer=new Rect(Screen.width*.5f-260,Screen.height-42,520,30);
            GUI.Label(footer,"KLICKA FÖREMÅL = VÄLJ   •   FLYTTA MED KNAPPARNA   •   E / ESC STÄNGER",center);
            if(GUI.Button(new Rect(left.x+18,left.yMax-52,122,30),"TA ALLT"))TakeAll();
            if(GUI.Button(new Rect(right.xMax-112,right.yMax-52,94,30),"STÄNG"))Close();

            string detail=string.IsNullOrEmpty(selectedItem)?"Välj vad du vill flytta":selectedItem+"   •   "+ItemWeight(selectedItem).ToString("0.00")+" kg/st";
            Rect d=new Rect(Screen.width*.5f-210,top+8,420,30);Panel(d,new Color(.02f,.028f,.025f,.92f),new Color(.22f,.25f,.22f,.8f));GUI.Label(d,detail,center);
        }

        void DrawGrid(Rect panel,IList<KeyValuePair<string,int>> list,bool fromContainer,ref Vector2 scroll,GUIStyle center,GUIStyle tiny) {
            int cols=5;float pad=18,space=9;float gridW=panel.width-pad*2;float cell=Mathf.Min(92f,(gridW-space*(cols-1))/cols);float y=68;
            int slots=Mathf.Max(cols*5,Mathf.CeilToInt(list.Count/(float)cols)*cols);float contentH=Mathf.CeilToInt(slots/(float)cols)*(cell+space);
            Rect view=new Rect(panel.x+pad,panel.y+y,panel.width-pad*2,panel.height-y-115);
            scroll=GUI.BeginScrollView(view,scroll,new Rect(0,0,view.width-18,Mathf.Max(view.height-4,contentH)));
            for(int i=0;i<slots;i++){
                int c=i%cols,r=i/cols;Rect slot=new Rect(c*(cell+space),r*(cell+space),cell,cell);bool occupied=i<list.Count;
                bool selected=occupied&&string.Equals(selectedItem,list[i].Key,StringComparison.OrdinalIgnoreCase)&&selectedFromContainer==fromContainer;
                Fill(slot,selected?new Color(.18f,.21f,.18f,.98f):new Color(.07f,.082f,.076f,.98f));Border(slot,selected?new Color(.60f,.61f,.50f,.95f):new Color(.24f,.27f,.24f,.86f),selected?2:1);
                if(!occupied)continue;
                var kv=list[i];float icon=cell-20;GUI.DrawTexture(new Rect(slot.x+10,slot.y+7,icon,icon),Icon(kv.Key),ScaleMode.ScaleToFit,true);
                GUI.Label(new Rect(slot.x+4,slot.y+cell-21,slot.width-8,17),kv.Value>1?"x"+kv.Value:"",Style(9,FontStyle.Bold,new Color(.92f,.90f,.82f),TextAnchor.LowerRight,false));
                if(GUI.Button(slot,GUIContent.none,GUIStyle.none)){selectedItem=kv.Key;selectedFromContainer=fromContainer;}
            }
            GUI.EndScrollView();
            string hint=fromContainer?"Välj vad du vill ta med":"Välj vad du vill lägga tillbaka";
            GUI.Label(new Rect(panel.x+18,panel.yMax-86,panel.width-36,20),hint,tiny);
        }

        GUIStyle Style(int size,FontStyle fs,Color c,TextAnchor a,bool wrap){GUIStyle s=new GUIStyle(GUI.skin.label){fontSize=size,fontStyle=fs,alignment=a,wordWrap=wrap};s.normal.textColor=c;return s;}
        void Panel(Rect r,Color fill,Color border){Fill(r,fill);Border(r,border,1);}
        void Fill(Rect r,Color c){Color o=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=o;}
        void Border(Rect r,Color c,int w){Fill(new Rect(r.x,r.y,r.width,w),c);Fill(new Rect(r.x,r.yMax-w,r.width,w),c);Fill(new Rect(r.x,r.y,w,r.height),c);Fill(new Rect(r.xMax-w,r.y,w,r.height),c);}
        void Line(Rect r,Color c){Fill(r,c);}

        void OnDisable(){if(player)player.externalUiBlocked=false;if(presentation)presentation.enabled=true;}
    }
}