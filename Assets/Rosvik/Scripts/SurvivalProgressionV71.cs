using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rosvik.Blackout {
    [DefaultExecutionOrder(900)]
    public sealed class SurvivalProgressionV71 : MonoBehaviour {
        public int day = 1;
        [Range(0f,1439f)] public float timeMinutes = 14f * 60f + 30f;
        public float gameMinutesPerRealSecond = 1.25f;
        [Range(0f,100f)] public float fatigue = 92f;
        public float fatiguePerGameHour = 3.2f;

        public string TimeLabel {
            get {
                int h = Mathf.FloorToInt(timeMinutes / 60f) % 24;
                int m = Mathf.FloorToInt(timeMinutes) % 60;
                return h.ToString("00") + ":" + m.ToString("00");
            }
        }
        public bool IsNight => timeMinutes < 7.5f*60f || timeMinutes > 16.5f*60f;
        public bool IsResting => transition;
        public bool PowerRestored => powerPanel != null && powerPanel.IsOpen;

        CoziPlayerV57 player;
        SurvivalSystemsV69 survival;
        CozyInteractableV57 powerPanel;
        Light sun;
        float baseWalk;
        float baseSprint;
        bool transition;
        float fade;
        string transitionText = "";
        float nextPowerScan;

        void Awake() {
            player = GetComponent<CoziPlayerV57>();
            if (!player) player = FindFirstObjectByType<CoziPlayerV57>();
            survival = GetComponent<SurvivalSystemsV69>();
            if (!survival) survival = FindFirstObjectByType<SurvivalSystemsV69>();
            if (player) { baseWalk = player.walkSpeed; baseSprint = player.sprintSpeed; }
            FindSun();
            FindPowerPanel();
        }

        void Update() {
            if (!player || !survival) return;
            if (!transition) {
                AdvanceClock(gameMinutesPerRealSecond * Time.deltaTime);
                float hours = gameMinutesPerRealSecond * Time.deltaTime / 60f;
                fatigue = Mathf.Clamp(fatigue - fatiguePerGameHour * hours, 0f, 100f);
            }
            if (Time.time >= nextPowerScan) { nextPowerScan = Time.time + 2f; if (!powerPanel) FindPowerPanel(); }
            ApplyDaylight();
            ApplyHeatSources();
        }

        void LateUpdate() {
            if (!player || !survival) return;
            if (transition) {
                player.walkSpeed = 0f;
                player.sprintSpeed = 0f;
                return;
            }
            float fatigueFactor = Mathf.Lerp(.58f, 1f, Mathf.Clamp01(fatigue / 100f));
            if (fatigue < 12f) fatigueFactor *= .78f;
            player.walkSpeed *= fatigueFactor;
            player.sprintSpeed *= Mathf.Lerp(.45f, 1f, Mathf.Clamp01(fatigue / 100f));
            if (fatigue < 6f) player.sprintSpeed = player.walkSpeed;
        }

        void AdvanceClock(float minutes) {
            timeMinutes += minutes;
            while (timeMinutes >= 1440f) { timeMinutes -= 1440f; day++; }
            while (timeMinutes < 0f) { timeMinutes += 1440f; day = Mathf.Max(1, day-1); }
        }

        public void Rest(float hours) {
            if (transition || !player || !survival) return;
            StartCoroutine(RestRoutine(Mathf.Clamp(hours, .5f, 12f), hours >= 4f));
        }

        IEnumerator RestRoutine(float hours, bool sleep) {
            transition = true;
            transitionText = sleep ? "DU SOVER..." : "DU VILAR...";
            float duration = sleep ? 1.35f : .75f;
            float t = 0f;
            while (t < duration*.42f) { t += Time.unscaledDeltaTime; fade = Mathf.Clamp01(t/(duration*.42f)); yield return null; }

            AdvanceClock(hours * 60f);
            fatigue = Mathf.Clamp(fatigue + (sleep ? 12.5f : 16f) * hours, 0f, 100f);
            survival.stamina = Mathf.Clamp(survival.stamina + (sleep ? 100f : 35f) * Mathf.Clamp01(hours), 0f, 100f);
            survival.hunger = Mathf.Clamp(survival.hunger - hours * 2.7f, 0f, 100f);
            survival.thirst = Mathf.Clamp(survival.thirst - hours * 3.5f, 0f, 100f);
            survival.wetness = Mathf.Clamp(survival.wetness - hours * 12f, 0f, 100f);
            survival.warmth = Mathf.Max(survival.warmth, sleep ? 78f : 70f);
            ApplyDaylight();

            yield return new WaitForSecondsRealtime(duration*.30f);
            t = duration*.42f;
            while (t > 0f) { t -= Time.unscaledDeltaTime; fade = Mathf.Clamp01(t/(duration*.42f)); yield return null; }
            fade = 0f;
            transition = false;
            player.ShowToast((sleep ? "Sov " : "Vilade ") + hours.ToString("0.#") + " h  •  " + TimeLabel, 2.8f);
        }

        void FindPowerPanel() {
            powerPanel = null;
            foreach (var x in FindObjectsByType<CozyInteractableV57>(FindObjectsSortMode.None)) {
                if (x && string.Equals(x.displayName, "elskåpet", StringComparison.OrdinalIgnoreCase)) { powerPanel = x; break; }
            }
        }

        void FindSun() {
            foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None)) {
                if (l && l.type == LightType.Directional) { sun = l; if (l.name.ToLowerInvariant().Contains("sun")) break; }
            }
        }

        void ApplyDaylight() {
            float h = timeMinutes / 60f;
            float daylight;
            if (h < 7.5f || h > 16.5f) daylight = .04f;
            else {
                float u = Mathf.InverseLerp(7.5f,16.5f,h);
                daylight = Mathf.Sin(u * Mathf.PI);
                daylight = Mathf.Clamp01(daylight);
            }

            Color night = new Color(.075f,.095f,.12f);
            Color dayClear = new Color(.39f,.42f,.39f);
            Color dayCloud = new Color(.31f,.34f,.33f);
            Color day = survival.weather == SurvivalSystemsV69.WeatherKind.Clear ? dayClear : dayCloud;
            if (survival.weather == SurvivalSystemsV69.WeatherKind.ColdSnap) day = new Color(.25f,.30f,.34f);
            RenderSettings.ambientLight = Color.Lerp(night, day, Mathf.Lerp(.12f,1f,daylight));

            if (sun) {
                sun.intensity = Mathf.Lerp(.035f,.72f,daylight);
                sun.color = Color.Lerp(new Color(.46f,.55f,.70f),new Color(1f,.86f,.68f),daylight);
                float u = Mathf.InverseLerp(6.5f,17.5f,h);
                float pitch = Mathf.Lerp(12f,62f,Mathf.Sin(Mathf.Clamp01(u)*Mathf.PI));
                float yaw = Mathf.Lerp(-80f,70f,Mathf.Clamp01(u));
                sun.transform.rotation = Quaternion.Euler(pitch,yaw,0f);
            }
        }

        void ApplyHeatSources() {
            foreach (V71HeatSource source in FindObjectsByType<V71HeatSource>(FindObjectsSortMode.None)) {
                if (!source || !source.IsOn || !player) continue;
                Vector3 d = source.transform.position - player.transform.position; d.y = 0f;
                float dist = d.magnitude;
                if (dist > source.heatRadius) continue;
                float strength = 1f - Mathf.Clamp01(dist/source.heatRadius);
                survival.warmth = Mathf.MoveTowards(survival.warmth,100f,source.warmthPerSecond*strength*Time.deltaTime);
                survival.wetness = Mathf.MoveTowards(survival.wetness,0f,source.dryPerSecond*strength*Time.deltaTime);
            }
        }

        public void ShowPrompt(string text) { prompt = text; promptUntil = Time.time + .12f; }
        string prompt = "";
        float promptUntil;

        void OnGUI() {
            GUI.depth = -1200;
            GUIStyle text = new GUIStyle(GUI.skin.label) { fontSize=13, alignment=TextAnchor.MiddleCenter, fontStyle=FontStyle.Bold };
            text.normal.textColor = new Color(.95f,.93f,.85f);
            GUIStyle small = new GUIStyle(text) { fontSize=11, fontStyle=FontStyle.Normal };

            Rect clock = new Rect(Screen.width*.5f-105f,14f,210f,56f);
            Color old = GUI.color; GUI.color = new Color(.05f,.065f,.06f,.93f); GUI.Box(clock,""); GUI.color=old;
            GUI.Label(new Rect(clock.x+8,clock.y+5,clock.width-16,22),"DAG "+day+"   •   "+TimeLabel,text);
            GUI.Label(new Rect(clock.x+8,clock.y+28,clock.width-16,19),"ORK "+Mathf.RoundToInt(fatigue)+"%"+(IsNight?"   •   NATT":""),small);

            if (Time.time < promptUntil && !transition) {
                Rect p = new Rect(Screen.width*.5f-260f,Screen.height-78f,520f,42f);
                old=GUI.color; GUI.color=new Color(.05f,.065f,.06f,.95f); GUI.Box(p,""); GUI.color=old;
                GUI.Label(new Rect(p.x+10,p.y+7,p.width-20,28),prompt,text);
            }

            if (transition || fade > .001f) {
                old=GUI.color; GUI.color=new Color(.015f,.025f,.03f,Mathf.Clamp01(fade*.97f));
                GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture); GUI.color=old;
                if (fade>.55f) GUI.Label(new Rect(Screen.width*.5f-180f,Screen.height*.5f-24f,360f,48f),transitionText,text);
            }
        }
    }

    public sealed class V71RestSpot : MonoBehaviour {
        public string displayName = "viloplatsen";
        public float radius = 2.2f;
        public float restHours = 1f;
        public float sleepHours = 8f;
        CoziPlayerV57 player;
        SurvivalProgressionV71 progression;

        void Awake() {
            player = FindFirstObjectByType<CoziPlayerV57>();
            progression = FindFirstObjectByType<SurvivalProgressionV71>();
        }

        void Update() {
            if (!player || !progression || progression.IsResting) return;
            Vector3 d = transform.position - player.transform.position; d.y=0f;
            if (d.sqrMagnitude > radius*radius) return;
            progression.ShowPrompt("E  Vila 1 h     •     Shift + E  Sov 8 h");
            Keyboard kb = Keyboard.current;
            if (kb == null || !kb.eKey.wasPressedThisFrame) return;
            bool sleep = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
            progression.Rest(sleep ? sleepHours : restHours);
        }
    }

    public sealed class V71HeatSource : MonoBehaviour {
        public string displayName = "elementet";
        public float radius = 2.0f;
        public float heatRadius = 4.5f;
        public float warmthPerSecond = 7.5f;
        public float dryPerSecond = 5f;
        public bool requiresPower = true;
        public Light glow;
        public Renderer indicator;
        public Color offColor = new Color(.20f,.18f,.15f);
        public Color onColor = new Color(1f,.43f,.12f);
        public bool IsOn { get; private set; }

        CoziPlayerV57 player;
        SurvivalProgressionV71 progression;
        MaterialPropertyBlock block;
        int colorId = -1;

        void Awake() {
            player = FindFirstObjectByType<CoziPlayerV57>();
            progression = FindFirstObjectByType<SurvivalProgressionV71>();
            if (indicator && indicator.sharedMaterial) {
                if (indicator.sharedMaterial.HasProperty("_BaseColor")) colorId=Shader.PropertyToID("_BaseColor");
                else if (indicator.sharedMaterial.HasProperty("_Color")) colorId=Shader.PropertyToID("_Color");
                block=new MaterialPropertyBlock();
            }
            SetVisual(false);
        }

        void Update() {
            if (!player || !progression || progression.IsResting) return;
            Vector3 d = transform.position - player.transform.position; d.y=0f;
            if (d.sqrMagnitude > radius*radius) return;
            string state = IsOn ? "Stäng av" : "Slå på";
            progression.ShowPrompt("E  "+state+" "+displayName+(requiresPower&&!progression.PowerRestored&&!IsOn?"  •  kräver ström":""));
            Keyboard kb=Keyboard.current;
            if(kb==null||!kb.eKey.wasPressedThisFrame)return;
            if(requiresPower&&!progression.PowerRestored&&!IsOn){player.ShowToast("Ingen ström. Elskåpet måste återställas först.",2.5f);return;}
            IsOn=!IsOn; SetVisual(IsOn);
            player.ShowToast(IsOn?"Elementet börjar bli varmt":"Elementet stängdes av",1.8f);
        }

        void SetVisual(bool on) {
            if (glow) glow.enabled=on;
            if (indicator && colorId>=0 && block!=null) {
                indicator.GetPropertyBlock(block); block.SetColor(colorId,on?onColor:offColor); indicator.SetPropertyBlock(block);
            }
        }
    }
}
