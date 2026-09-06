using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rosvik.Blackout {
    [DefaultExecutionOrder(-4300)]
    public sealed class WoodStoveV81 : MonoBehaviour {
        public string displayName = "vedspisen";
        public float interactionDistance = 2.45f;
        public float heatRadius = 3.6f;
        public float secondsPerFuel = 110f;
        public float maxBurnSeconds = 440f;
        public Transform flameVisual;
        public Light fireLight;

        CoziPlayerV57 player;
        SurvivalSystemsV69 survival;
        AudioSource fireSource;
        AudioSource oneShot;
        AudioClip crackleClip;
        AudioClip igniteClip;
        AudioClip feedClip;
        AudioClip cookClip;
        float burnRemaining;
        bool lit;
        bool ownsBlock;
        float flickerSeed;
        bool introducedBase;

        public bool IsLit => lit;
        public float BurnRemaining => burnRemaining;

        void Awake() {
            player = FindFirstObjectByType<CoziPlayerV57>();
            survival = player ? player.GetComponent<SurvivalSystemsV69>() : FindFirstObjectByType<SurvivalSystemsV69>();
            BuildAudio();
            fireSource = gameObject.AddComponent<AudioSource>();
            fireSource.playOnAwake = false;
            fireSource.loop = true;
            fireSource.clip = crackleClip;
            fireSource.volume = 0f;
            fireSource.spatialBlend = .18f;
            fireSource.minDistance = 2f;
            fireSource.maxDistance = 28f;
            fireSource.rolloffMode = AudioRolloffMode.Linear;
            fireSource.priority = 95;
            fireSource.Play();

            oneShot = gameObject.AddComponent<AudioSource>();
            oneShot.playOnAwake = false;
            oneShot.loop = false;
            oneShot.spatialBlend = .12f;
            oneShot.priority = 85;
            flickerSeed = UnityEngine.Random.Range(1f,100f);
            ApplyVisuals();
        }

        void Update() {
            if (!player) player = FindFirstObjectByType<CoziPlayerV57>();
            if (!survival && player) survival = player.GetComponent<SurvivalSystemsV69>();
            if (!player) return;

            if (lit) {
                burnRemaining = Mathf.Max(0f, burnRemaining - Time.deltaTime);
                if (burnRemaining <= 0f) Extinguish(false);
                ApplyHeat();
                Flicker();
            }

            if (fireSource) fireSource.volume = Mathf.MoveTowards(fireSource.volume, lit ? .34f : 0f, Time.deltaTime * .55f);

            if (!NearPlayer() || player.externalUiBlocked) return;
            Keyboard kb = Keyboard.current;
            if (kb == null || !kb.eKey.wasPressedThisFrame) return;

            bool shift = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
            player.externalUiBlocked = true;
            ownsBlock = true;
            if (shift) CookSoup();
            else if (!lit) Ignite();
            else AddFuel();
        }

        void LateUpdate() {
            if (!ownsBlock || !player) return;
            ownsBlock = false;
            player.externalUiBlocked = false;
        }

        bool NearPlayer() {
            if (!player) return false;
            Vector3 d = transform.position - player.transform.position;
            d.y = 0f;
            return d.sqrMagnitude <= interactionDistance * interactionDistance;
        }

        void Ignite() {
            if (!player) return;
            if (!player.HasItem("Tändare")) {
                player.ShowToast("Du behöver en tändare", 1.8f);
                return;
            }
            if (!player.ConsumeItem("Träspill",1)) {
                player.ShowToast("Du behöver torr ved / träspill", 2.0f);
                return;
            }
            lit = true;
            burnRemaining = Mathf.Min(maxBurnSeconds, burnRemaining + secondsPerFuel);
            ApplyVisuals();
            PlayOne(igniteClip,.72f,UnityEngine.Random.Range(.96f,1.04f));
            player.ShowToast("Vedspisen är tänd", 1.8f);
            if (!introducedBase) {
                introducedBase = true;
                player.SetObjective("Hus B kan fungera som bas. Håll vedspisen igång, torka kläder och värm mat innan du ger dig ut igen.");
            }
        }

        void AddFuel() {
            if (!player) return;
            if (burnRemaining > maxBurnSeconds - 18f) {
                player.ShowToast("Spisen är redan välfylld", 1.5f);
                return;
            }
            if (!player.ConsumeItem("Träspill",1)) {
                player.ShowToast("Ingen torr ved / träspill i ryggsäcken", 1.8f);
                return;
            }
            burnRemaining = Mathf.Min(maxBurnSeconds, burnRemaining + secondsPerFuel);
            PlayOne(feedClip,.58f,UnityEngine.Random.Range(.92f,1.05f));
            player.ShowToast("Lade på mer ved", 1.3f);
        }

        void CookSoup() {
            if (!player) return;
            if (!lit) {
                player.ShowToast("Tänd vedspisen först", 1.7f);
                return;
            }
            if (!player.ConsumeItem("Soppa",1)) {
                player.ShowToast("Du har ingen soppa att värma", 1.8f);
                return;
            }
            player.AddItem("Varm soppa",1);
            PlayOne(cookClip,.54f,UnityEngine.Random.Range(.97f,1.04f));
            player.ShowToast("Värmde en portion soppa", 1.8f);
        }

        void ApplyHeat() {
            if (!player || !survival) return;
            Vector3 d = transform.position - player.transform.position;
            d.y = 0f;
            float dist = d.magnitude;
            if (dist > heatRadius) return;
            float strength = 1f - Mathf.Clamp01(dist / heatRadius);
            survival.warmth = Mathf.MoveTowards(survival.warmth, 100f, (1.8f + 3.2f * strength) * Time.deltaTime);
            survival.wetness = Mathf.MoveTowards(survival.wetness, 0f, (1.2f + 2.3f * strength) * Time.deltaTime);
        }

        void Flicker() {
            float n = Mathf.PerlinNoise(flickerSeed, Time.time * 6.5f);
            if (fireLight) {
                fireLight.intensity = Mathf.Lerp(1.15f,2.15f,n);
                fireLight.range = Mathf.Lerp(4.2f,5.4f,n);
            }
            if (flameVisual && flameVisual.gameObject.activeSelf) {
                float s = Mathf.Lerp(.91f,1.10f,n);
                flameVisual.localScale = new Vector3(s,Mathf.Lerp(.88f,1.18f,n),s);
            }
        }

        void Extinguish(bool toast) {
            lit = false;
            burnRemaining = 0f;
            ApplyVisuals();
            if (toast && player) player.ShowToast("Vedspisen slocknade",1.5f);
        }

        void ApplyVisuals() {
            if (flameVisual) flameVisual.gameObject.SetActive(lit);
            if (fireLight) fireLight.enabled = lit;
        }

        void PlayOne(AudioClip clip,float volume,float pitch) {
            if (!oneShot || !clip) return;
            oneShot.pitch = pitch;
            oneShot.PlayOneShot(clip,volume);
        }

        void BuildAudio() {
            crackleClip = FireLoop("wood stove crackle",5f,810);
            igniteClip = FireBurst("stove ignite",.46f,.50f,811);
            feedClip = SoftWood("add firewood",.28f,.42f,812);
            cookClip = SoftWood("pot on stove",.22f,.28f,813);
        }

        AudioClip FireLoop(string name,float seconds,int seed) {
            int sr=22050,count=Mathf.RoundToInt(sr*seconds);
            float[] data=new float[count];
            System.Random r=new System.Random(seed);
            float low=0f;
            for(int i=0;i<count;i++) {
                float n=(float)(r.NextDouble()*2-1);
                low=low*.83f+n*.17f;
                float pop=r.NextDouble()<.0018 ? (float)r.NextDouble()*.75f : 0f;
                float hiss=(float)(r.NextDouble()*2-1)*.11f;
                data[i]=(low*.22f+hiss+pop)*.38f;
            }
            AudioClip c=AudioClip.Create(name,count,1,sr,false);c.SetData(data,0);return c;
        }

        AudioClip FireBurst(string name,float seconds,float amp,int seed) {
            int sr=22050,count=Mathf.RoundToInt(sr*seconds);float[] data=new float[count];System.Random r=new System.Random(seed);float lp=0f;
            for(int i=0;i<count;i++){float u=i/(float)(count-1);float env=Mathf.Sin(Mathf.PI*u);float n=(float)(r.NextDouble()*2-1);lp=lp*.72f+n*.28f;data[i]=(lp*.7f+n*.15f)*env*amp;}
            AudioClip c=AudioClip.Create(name,count,1,sr,false);c.SetData(data,0);return c;
        }

        AudioClip SoftWood(string name,float seconds,float amp,int seed) {
            int sr=22050,count=Mathf.RoundToInt(sr*seconds);float[] data=new float[count];System.Random r=new System.Random(seed);float low=0f;
            for(int i=0;i<count;i++){float u=i/(float)(count-1);float env=Mathf.Pow(1f-u,2f);float n=(float)(r.NextDouble()*2-1);low=low*.78f+n*.22f;data[i]=low*env*amp;}
            AudioClip c=AudioClip.Create(name,count,1,sr,false);c.SetData(data,0);return c;
        }

        void OnGUI() {
            if (!player || !NearPlayer() || player.externalUiBlocked && !ownsBlock) return;
            Rect r = new Rect(Screen.width*.5f-245f,Screen.height-72f,490f,40f);
            Color old=GUI.color;
            GUI.color=new Color(.014f,.020f,.017f,.96f);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;
            GUIStyle s=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontSize=12,fontStyle=FontStyle.Bold};s.normal.textColor=new Color(.94f,.90f,.79f);
            string text;
            if (!lit) text="E   TÄND VEDSPIS   •   kräver tändare + torrt trä";
            else {
                int sec=Mathf.CeilToInt(burnRemaining);string time=(sec/60).ToString("0")+":"+(sec%60).ToString("00");
                text="E   LÄGG PÅ VED   •   SHIFT+E VÄRM SOPPA   •   "+time;
            }
            GUI.Label(r,text,s);
        }
    }
}
