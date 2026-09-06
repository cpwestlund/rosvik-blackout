using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rosvik.Blackout {
    [DefaultExecutionOrder(6000)]
    public sealed class SurvivalInjuriesV77 : MonoBehaviour {
        public enum BodyPart { Head, Torso, LeftArm, RightArm, LeftLeg, RightLeg }

        [Serializable]
        public sealed class PartState {
            public float bleeding;
            public float sprain;
            public float pain;
            public float infection;
            public bool HasProblem => bleeding > .5f || sprain > .5f || pain > .5f || infection > .5f;
        }

        CoziPlayerV57 player;
        SurvivalSystemsV69 survival;
        readonly Dictionary<BodyPart,PartState> parts = new Dictionary<BodyPart,PartState>();
        BodyPart selected = BodyPart.Torso;
        bool panelOpen;
        float tick;
        float coldStress;
        float painReliefUntil;
        float lastMoveFactor = 1f;
        Dictionary<string,SurvivalSystemsV69.ItemDef> defs;

        public float ColdStress => coldStress;
        public IReadOnlyDictionary<BodyPart,PartState> Parts => parts;

        void Awake() {
            player = GetComponent<CoziPlayerV57>(); if (!player) player = FindFirstObjectByType<CoziPlayerV57>();
            survival = GetComponent<SurvivalSystemsV69>(); if (!survival) survival = FindFirstObjectByType<SurvivalSystemsV69>();
            foreach (BodyPart p in Enum.GetValues(typeof(BodyPart))) parts[p] = new PartState();
            RegisterMedicalDefinitions();
        }

        void RegisterMedicalDefinitions() {
            FieldInfo f = typeof(SurvivalSystemsV69).GetField("defs", BindingFlags.Static|BindingFlags.NonPublic);
            defs = f != null ? f.GetValue(null) as Dictionary<string,SurvivalSystemsV69.ItemDef> : null;
            if (defs == null) return;
            AddDef(new SurvivalSystemsV69.ItemDef("Värktabletter","painkillers",SurvivalSystemsV69.ItemKind.Medical,d:"Dämpar smärta tillfälligt. Behandlar inte själva skadan."));
            AddDef(new SurvivalSystemsV69.ItemDef("Antiseptisk spray","antiseptic",SurvivalSystemsV69.ItemKind.Medical,d:"Rengör sår och minskar infektionsrisken."));
            AddDef(new SurvivalSystemsV69.ItemDef("Elastisk linda","elasticwrap",SurvivalSystemsV69.ItemKind.Medical,d:"Stabiliserar en stukning och minskar smärta vid belastning."));
        }
        void AddDef(SurvivalSystemsV69.ItemDef d) { if (defs != null && !defs.ContainsKey(d.name)) defs[d.name] = d; }

        void Update() {
            if (!player || !survival) return;
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.mKey.wasPressedThisFrame) {
                panelOpen = !panelOpen;
                player.externalUiBlocked = panelOpen;
                Cursor.visible = panelOpen;
                if (panelOpen) Cursor.lockState = CursorLockMode.None;
            }
            if (panelOpen && kb != null && kb.escapeKey.wasPressedThisFrame) { panelOpen = false; player.externalUiBlocked = false; }

            tick += Time.deltaTime;
            if (tick >= 1f) { float dt = tick; tick = 0f; Simulate(dt); }
        }

        void Simulate(float dt) {
            if (survival.warmth < 35f) coldStress = Mathf.Clamp(coldStress + (35f-survival.warmth)*.010f*dt + survival.wetness*.0015f*dt,0,100);
            else if (survival.warmth > 55f) coldStress = Mathf.Clamp(coldStress - .55f*dt,0,100);

            float totalBleed = 0f, totalPain = 0f, worstLeg = 0f;
            foreach (var kv in parts) {
                PartState s = kv.Value;
                totalBleed += s.bleeding;
                s.infection = Mathf.Clamp(s.infection + s.bleeding*.00018f*dt,0,100);
                if (s.bleeding < .5f) s.bleeding = 0f;
                if (Time.time > painReliefUntil) s.pain = Mathf.MoveTowards(s.pain, Mathf.Max(s.sprain*.45f,s.infection*.28f), .035f*dt);
                else s.pain = Mathf.MoveTowards(s.pain, 0f, .22f*dt);
                totalPain += s.pain;
                if (kv.Key==BodyPart.LeftLeg || kv.Key==BodyPart.RightLeg) worstLeg = Mathf.Max(worstLeg,s.sprain);
            }

            if (totalBleed > 0f) survival.health = Mathf.Clamp(survival.health-totalBleed*.0016f*dt,0,100);
            float infectionTotal = parts.Values.Sum(s=>s.infection);
            if (infectionTotal > 80f) survival.health = Mathf.Clamp(survival.health-(infectionTotal-80f)*.0008f*dt,0,100);
            if (coldStress > 72f) survival.health = Mathf.Clamp(survival.health-(coldStress-72f)*.004f*dt,0,100);

            if (lastMoveFactor > .01f) { player.walkSpeed /= lastMoveFactor; player.sprintSpeed /= lastMoveFactor; }
            float painFactor = Mathf.Lerp(1f,.82f,Mathf.Clamp01(totalPain/240f));
            float legFactor = Mathf.Lerp(1f,.62f,Mathf.Clamp01(worstLeg/100f));
            lastMoveFactor = Mathf.Clamp(painFactor*legFactor,.48f,1f);
            player.walkSpeed *= lastMoveFactor; player.sprintSpeed *= lastMoveFactor;
        }

        public void CauseBleeding(BodyPart part,float severity,string reason="") {
            PartState s = parts[part]; s.bleeding = Mathf.Clamp(Mathf.Max(s.bleeding,severity),0,100); s.pain = Mathf.Clamp(Mathf.Max(s.pain,severity*.55f),0,100);
            if (player) player.ShowToast(string.IsNullOrEmpty(reason)?PartLabel(part)+" blöder":reason,2.2f);
        }
        public void CauseSprain(BodyPart part,float severity,string reason="") {
            PartState s = parts[part]; s.sprain = Mathf.Clamp(Mathf.Max(s.sprain,severity),0,100); s.pain = Mathf.Clamp(Mathf.Max(s.pain,severity*.70f),0,100);
            if (player) player.ShowToast(string.IsNullOrEmpty(reason)?"Stukning: "+PartLabel(part):reason,2.2f);
        }

        bool Use(string item,int count=1) => player && player.ConsumeItem(item,count);
        public bool Treat(string item,BodyPart part) {
            if (!player || !parts.TryGetValue(part,out PartState s)) return false;
            if (item.Equals("Förband",StringComparison.OrdinalIgnoreCase)) {
                if (s.bleeding<=.5f || !Use(item)) return false;
                s.bleeding = Mathf.Max(0,s.bleeding-72f); s.pain = Mathf.Max(0,s.pain-8f); player.ShowToast("Bandagerade "+PartLabel(part),1.8f); return true;
            }
            if (item.Equals("Antiseptisk spray",StringComparison.OrdinalIgnoreCase)) {
                if ((s.bleeding<=.5f && s.infection<=.5f) || !Use(item)) return false;
                s.infection = Mathf.Max(0,s.infection-75f); player.ShowToast("Rengjorde såret",1.6f); return true;
            }
            if (item.Equals("Elastisk linda",StringComparison.OrdinalIgnoreCase) || item.Equals("Sporttejp",StringComparison.OrdinalIgnoreCase)) {
                if (s.sprain<=.5f || !Use(item)) return false;
                s.sprain = Mathf.Max(0,s.sprain-55f); s.pain = Mathf.Max(0,s.pain-18f); player.ShowToast("Stabiliserade "+PartLabel(part),1.8f); return true;
            }
            if (item.Equals("Värktabletter",StringComparison.OrdinalIgnoreCase)) {
                if (!Use(item)) return false;
                painReliefUntil = Time.time + 240f;
                foreach (PartState p in parts.Values) p.pain = Mathf.Max(0,p.pain-22f);
                player.ShowToast("Värktabletten börjar verka",1.8f); return true;
            }
            return false;
        }

        string Status(PartState s) {
            List<string> v = new List<string>();
            if (s.bleeding>.5f) v.Add("blödning "+Mathf.RoundToInt(s.bleeding)+"%");
            if (s.sprain>.5f) v.Add("stukning "+Mathf.RoundToInt(s.sprain)+"%");
            if (s.pain>.5f) v.Add("smärta "+Mathf.RoundToInt(s.pain)+"%");
            if (s.infection>.5f) v.Add("infektionsrisk "+Mathf.RoundToInt(s.infection)+"%");
            return v.Count==0?"OK":string.Join("  •  ",v);
        }

        void OnGUI() {
            if (!panelOpen || !player) return;
            GUI.depth = -13000;
            Color old = GUI.color; GUI.color = new Color(.004f,.007f,.006f,.95f); GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture); GUI.color = old;
            Rect panel = new Rect(Screen.width*.5f-430,54,860,Screen.height-108);
            Fill(panel,new Color(.022f,.029f,.027f,.985f)); Border(panel,new Color(.30f,.33f,.29f),1);
            GUIStyle title = Style(22,FontStyle.Bold,new Color(.90f,.90f,.83f),TextAnchor.MiddleLeft,false);
            GUIStyle head = Style(13,FontStyle.Bold,new Color(.80f,.82f,.75f),TextAnchor.MiddleLeft,false);
            GUIStyle text = Style(11,FontStyle.Normal,new Color(.70f,.74f,.68f),TextAnchor.MiddleLeft,true);
            GUIStyle small = Style(10,FontStyle.Normal,new Color(.58f,.61f,.56f),TextAnchor.MiddleLeft,true);
            GUI.Label(new Rect(panel.x+24,panel.y+18,420,30),"KROPP & SKADOR",title);
            GUI.Label(new Rect(panel.xMax-300,panel.y+22,270,22),"M / ESC  STÄNG",Style(10,FontStyle.Bold,new Color(.60f,.63f,.58f),TextAnchor.MiddleRight,false));
            Line(new Rect(panel.x+24,panel.y+58,panel.width-48,1));

            float left = panel.x+28, top=panel.y+82;
            GUI.Label(new Rect(left,top,350,22),"KROPPSDELAR",head); top+=30;
            foreach (BodyPart p in Enum.GetValues(typeof(BodyPart))) {
                PartState s = parts[p]; Rect row = new Rect(left,top,390,48);
                Fill(row,selected==p?new Color(.12f,.15f,.13f):new Color(.055f,.067f,.061f)); Border(row,selected==p?new Color(.53f,.55f,.46f):new Color(.16f,.19f,.17f),1);
                GUI.Label(new Rect(row.x+10,row.y+5,100,17),PartLabel(p),head);
                GUI.Label(new Rect(row.x+112,row.y+5,row.width-122,34),Status(s),small);
                if (GUI.Button(row,GUIContent.none,GUIStyle.none)) selected=p;
                top+=54;
            }

            Rect right = new Rect(panel.x+445,panel.y+82,380,panel.height-120);
            PartState current = parts[selected];
            GUI.Label(new Rect(right.x,right.y,right.width,24),PartLabel(selected).ToUpperInvariant(),title);
            GUI.Label(new Rect(right.x,right.y+34,right.width,72),Status(current),text);
            GUI.Label(new Rect(right.x,right.y+112,right.width,22),"Nedkylning: "+Mathf.RoundToInt(coldStress)+"%",head);
            string cold = coldStress<25?"Stabil":coldStress<55?"Kall — sök värme och torra kläder":coldStress<75?"Risk för hypotermi":"Allvarlig hypotermi";
            GUI.Label(new Rect(right.x,right.y+138,right.width,42),cold,text);
            float y = right.y+205;
            GUI.Label(new Rect(right.x,y,right.width,22),"BEHANDLING",head); y+=32;
            ButtonTreat(right.x,ref y,"Förband","Stoppar blödning",current.bleeding>.5f);
            ButtonTreat(right.x,ref y,"Antiseptisk spray","Rengör sår / minskar infektion",current.bleeding>.5f||current.infection>.5f);
            ButtonTreat(right.x,ref y,"Elastisk linda","Stabiliserar stukning",current.sprain>.5f);
            ButtonTreat(right.x,ref y,"Sporttejp","Nödlösning för stukning",current.sprain>.5f);
            ButtonTreat(right.x,ref y,"Värktabletter","Dämpar smärta tillfälligt",parts.Values.Any(p=>p.pain>.5f));
            GUI.Label(new Rect(right.x,right.yMax-82,right.width,58),"Skador påverkar rörelse och hälsa. Förband, linda och läkemedel behandlar olika problem — de är inte längre bara +HP.",small);
        }

        void ButtonTreat(float x,ref float y,string item,string desc,bool relevant) {
            int count = player.CountItem(item); GUI.enabled = relevant && count>0;
            if (GUI.Button(new Rect(x,y,170,30),item+"  x"+count)) Treat(item,selected);
            GUI.enabled = true; GUI.Label(new Rect(x+180,y,190,30),desc,Style(9,FontStyle.Normal,new Color(.58f,.61f,.56f),TextAnchor.MiddleLeft,true)); y+=38;
        }
        string PartLabel(BodyPart p) { switch(p){case BodyPart.Head:return "Huvud";case BodyPart.Torso:return "Torso";case BodyPart.LeftArm:return "Vänster arm";case BodyPart.RightArm:return "Höger arm";case BodyPart.LeftLeg:return "Vänster ben";default:return "Höger ben";} }
        GUIStyle Style(int s,FontStyle f,Color c,TextAnchor a,bool wrap){GUIStyle x=new GUIStyle(GUI.skin.label){fontSize=s,fontStyle=f,alignment=a,wordWrap=wrap};x.normal.textColor=c;return x;}
        void Fill(Rect r,Color c){Color o=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=o;}
        void Border(Rect r,Color c,int w){Fill(new Rect(r.x,r.y,r.width,w),c);Fill(new Rect(r.x,r.yMax-w,r.width,w),c);Fill(new Rect(r.x,r.y,w,r.height),c);Fill(new Rect(r.xMax-w,r.y,w,r.height),c);}
        void Line(Rect r){Fill(r,new Color(.20f,.23f,.20f,.9f));}
        void OnDisable(){if(player&&panelOpen)player.externalUiBlocked=false;}
    }

    public sealed class InjuryHazardV77 : MonoBehaviour {
        public enum HazardKind { Ice, BrokenGlass }
        public HazardKind kind;
        public float cooldown = 10f;
        float nextAllowed;

        void OnTriggerEnter(Collider other) {
            if (Time.time < nextAllowed) return;
            CoziPlayerV57 p = other.GetComponentInParent<CoziPlayerV57>(); if (!p) return;
            SurvivalInjuriesV77 injuries = p.GetComponent<SurvivalInjuriesV77>(); if (!injuries) return;
            SurvivalEquipmentV76 equipment = p.GetComponent<SurvivalEquipmentV76>();
            if (kind==HazardKind.Ice) {
                Keyboard kb = Keyboard.current;
                bool sprint = kb!=null && (kb.leftShiftKey.isPressed||kb.rightShiftKey.isPressed);
                if (!sprint) { p.ShowToast("Halt underlag — spring inte här",1.5f); nextAllowed=Time.time+cooldown; return; }
                var leg = UnityEngine.Random.value<.5f?SurvivalInjuriesV77.BodyPart.LeftLeg:SurvivalInjuriesV77.BodyPart.RightLeg;
                injuries.CauseSprain(leg,UnityEngine.Random.Range(34f,58f),"Du halkade och stukade benet");
            } else {
                bool boots = equipment && !string.IsNullOrWhiteSpace(equipment.SlotItem("Feet"));
                if (boots) { p.ShowToast("Kängorna skyddade mot glasskärvorna",1.5f); nextAllowed=Time.time+cooldown; return; }
                var footLeg = UnityEngine.Random.value<.5f?SurvivalInjuriesV77.BodyPart.LeftLeg:SurvivalInjuriesV77.BodyPart.RightLeg;
                injuries.CauseBleeding(footLeg,UnityEngine.Random.Range(28f,48f),"Glasskärvor skar benet — du blöder");
            }
            nextAllowed = Time.time + cooldown;
        }
    }
}
