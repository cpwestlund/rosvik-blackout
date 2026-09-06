using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Rosvik.Blackout {
    [DefaultExecutionOrder(9800)]
    public sealed class WorldSoundscapeV80 : MonoBehaviour {
        public float masterVolume = .90f;
        public float windVolume = .58f;
        public float footstepVolume = .28f;
        public float interactionVolume = .70f;

        CoziPlayerV57 player;
        SurvivalSystemsV69 survival;
        AudioSource windSource;
        AudioSource oneShotSource;
        AudioClip windLoop;
        AudioClip snowStepA, snowStepB, indoorStepA, indoorStepB, gravelStepA, gravelStepB;
        AudioClip doorOpen, doorClose, cabinetOpen, cabinetClose, pickup, distantCreak, distantGust, houseTick;
        Vector3 lastPos;
        float stepTravel;
        float nextStatePoll;
        float nextDistantSound;
        int lastInventoryCount = -1;
        readonly Dictionary<int,bool> doorStates = new Dictionary<int,bool>();
        readonly Dictionary<int,bool> containerStates = new Dictionary<int,bool>();

        void Awake() {
            player = GetComponent<CoziPlayerV57>();
            if (!player) player = FindFirstObjectByType<CoziPlayerV57>();
            survival = GetComponent<SurvivalSystemsV69>();
            if (!survival) survival = FindFirstObjectByType<SurvivalSystemsV69>();
            BuildClips();

            windSource = gameObject.AddComponent<AudioSource>();
            windSource.playOnAwake = false;
            windSource.loop = true;
            windSource.spatialBlend = 0f;
            windSource.priority = 185;
            windSource.clip = windLoop;
            windSource.volume = 0f;
            windSource.Play();

            oneShotSource = gameObject.AddComponent<AudioSource>();
            oneShotSource.playOnAwake = false;
            oneShotSource.loop = false;
            oneShotSource.spatialBlend = 0f;
            oneShotSource.priority = 105;

            if (player) lastPos = player.transform.position;
            PrimeStates();
            nextDistantSound = Time.time + 5f;
        }

        void Update() {
            if (!player) return;
            UpdateWind();
            UpdateFootsteps();
            if (Time.time >= nextStatePoll) {
                nextStatePoll = Time.time + .13f;
                PollInteractions();
                PollInventory();
            }
            if (Time.time >= nextDistantSound) {
                bool inside = IsSheltered(player.transform.position);
                nextDistantSound = Time.time + UnityEngine.Random.Range(6.5f, 14.5f);
                float roll = UnityEngine.Random.value;
                if (!inside) {
                    if (roll < .42f) Play(distantGust, .20f + WindFactor() * .10f, UnityEngine.Random.Range(.90f,1.07f));
                    else Play(distantCreak, .16f + WindFactor() * .10f, UnityEngine.Random.Range(.90f,1.08f));
                } else if (roll < .55f) {
                    Play(houseTick, .13f, UnityEngine.Random.Range(.91f,1.08f));
                }
            }
        }

        void UpdateWind() {
            if (!windSource) return;
            bool inside = IsSheltered(player.transform.position);
            float target = masterVolume * windVolume * WindFactor() * (inside ? .10f : 1f);
            windSource.volume = Mathf.MoveTowards(windSource.volume, target, Time.deltaTime * .30f);
            windSource.pitch = Mathf.Lerp(.88f, 1.08f, Mathf.Clamp01(WindFactor() * .68f));
        }

        float WindFactor() {
            if (!survival) return .72f;
            try {
                FieldInfo f = survival.GetType().GetField("wind", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
                if (f != null && f.GetValue(survival) is float x) return Mathf.Clamp(.48f + x * .14f, .48f, 1.42f);
            } catch {}
            return .75f;
        }

        void UpdateFootsteps() {
            Vector3 p = player.transform.position;
            Vector3 flat = p - lastPos;
            flat.y = 0f;
            float d = flat.magnitude;
            lastPos = p;
            if (d > 1.5f) { stepTravel = 0f; return; }
            stepTravel += d;
            float speed = d / Mathf.Max(.001f, Time.deltaTime);
            if (speed < .32f) return;

            bool running = speed > 4.6f;
            float spacing = running ? 1.18f : 1.52f;
            if (stepTravel < spacing) return;
            stepTravel = 0f;

            Surface s = SurfaceAt(p);
            AudioClip clip;
            float surfaceGain = 1f;
            switch(s) {
                case Surface.Indoor:
                    clip = UnityEngine.Random.value < .5f ? indoorStepA : indoorStepB;
                    surfaceGain = .78f;
                    break;
                case Surface.Gravel:
                    clip = UnityEngine.Random.value < .5f ? gravelStepA : gravelStepB;
                    surfaceGain = .88f;
                    break;
                default:
                    clip = UnityEngine.Random.value < .5f ? snowStepA : snowStepB;
                    surfaceGain = 1f;
                    break;
            }
            float gain = footstepVolume * masterVolume * surfaceGain * (running ? 1.10f : .80f);
            Play(clip, gain, UnityEngine.Random.Range(.91f, 1.08f));
        }

        enum Surface { Snow, Gravel, Indoor }
        Surface SurfaceAt(Vector3 p) {
            if (IsSheltered(p)) return Surface.Indoor;
            if (Physics.Raycast(p + Vector3.up*.7f, Vector3.down, out RaycastHit hit, 2f, ~0, QueryTriggerInteraction.Ignore)) {
                string n = hit.collider ? hit.collider.gameObject.name.ToLowerInvariant() : "";
                if (n.Contains("road") || n.Contains("lane") || n.Contains("gravel") || n.Contains("driveway") || n.Contains("parking") || n.Contains("path")) return Surface.Gravel;
                if (n.Contains("floor")) return Surface.Indoor;
            }
            return Surface.Snow;
        }

        bool IsSheltered(Vector3 p) {
            if (p.x > -19.5f && p.x < 45.2f && p.z > -2.8f && p.z < 16.8f) return true;
            if (p.x > -36.5f && p.x < -21.5f && p.z > 2.45f && p.z < 16.1f) return true;
            if (p.x > -36.3f && p.x < -21.7f && p.z > 22.2f && p.z < 35.7f) return true;
            if (p.x > 51.5f && p.x < 65.4f && p.z > 4.3f && p.z < 15.5f) return true;
            return false;
        }

        void PrimeStates() {
            doorStates.Clear();
            containerStates.Clear();
            foreach (HouseInteriorDoorV77 d in FindObjectsByType<HouseInteriorDoorV77>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (d) doorStates[d.GetInstanceID()] = d.IsOpen;
            foreach (HouseFrontDoorV763 d in FindObjectsByType<HouseFrontDoorV763>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (d) doorStates[d.GetInstanceID()] = d.IsOpen;
            foreach (LootContainerV74 c in FindObjectsByType<LootContainerV74>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (c) containerStates[c.GetInstanceID()] = c.IsOpen;
            lastInventoryCount = InventoryCount();
        }

        void PollInteractions() {
            foreach (HouseInteriorDoorV77 d in FindObjectsByType<HouseInteriorDoorV77>(FindObjectsSortMode.None)) {
                if (!d) continue;
                int id=d.GetInstanceID(); bool state=d.IsOpen;
                if (!doorStates.TryGetValue(id,out bool prev)) { doorStates[id]=state; continue; }
                if (state!=prev) { doorStates[id]=state; Play(state?doorOpen:doorClose, interactionVolume*masterVolume, UnityEngine.Random.Range(.93f,1.04f)); }
            }
            foreach (HouseFrontDoorV763 d in FindObjectsByType<HouseFrontDoorV763>(FindObjectsSortMode.None)) {
                if (!d) continue;
                int id=d.GetInstanceID(); bool state=d.IsOpen;
                if (!doorStates.TryGetValue(id,out bool prev)) { doorStates[id]=state; continue; }
                if (state!=prev) { doorStates[id]=state; Play(state?doorOpen:doorClose, interactionVolume*masterVolume*1.05f, UnityEngine.Random.Range(.90f,1.00f)); }
            }
            foreach (LootContainerV74 c in FindObjectsByType<LootContainerV74>(FindObjectsSortMode.None)) {
                if (!c) continue;
                int id=c.GetInstanceID(); bool state=c.IsOpen;
                if (!containerStates.TryGetValue(id,out bool prev)) { containerStates[id]=state; continue; }
                if (state!=prev) { containerStates[id]=state; Play(state?cabinetOpen:cabinetClose, interactionVolume*masterVolume*.82f, UnityEngine.Random.Range(.94f,1.06f)); }
            }
        }

        void PollInventory() {
            int n = InventoryCount();
            if (lastInventoryCount >= 0 && n > lastInventoryCount) Play(pickup, interactionVolume*masterVolume*.76f, UnityEngine.Random.Range(.96f,1.08f));
            lastInventoryCount=n;
        }

        int InventoryCount() {
            if (!player) return 0;
            int n=0;
            foreach (var kv in player.Inventory) n += Mathf.Max(0,kv.Value);
            return n;
        }

        void Play(AudioClip clip,float volume,float pitch=1f) {
            if (!clip || !oneShotSource) return;
            oneShotSource.pitch=pitch;
            oneShotSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        void BuildClips() {
            windLoop = Wind("V80.1 winter wind", 9f, .34f, 5);
            snowStepA = SoftCrunch("snow step A", .28f, .52f, 41, .84f);
            snowStepB = SoftCrunch("snow step B", .30f, .48f, 77, .80f);
            gravelStepA = SoftCrunch("gravel step A", .22f, .39f, 113, .58f);
            gravelStepB = SoftCrunch("gravel step B", .24f, .36f, 171, .54f);
            indoorStepA = SoftStep("indoor step A", .20f, 74f, .24f, 2);
            indoorStepB = SoftStep("indoor step B", .22f, 66f, .22f, 7);
            doorOpen = Creak("door open", .60f, 76f, 142f, 11);
            doorClose = Thud("door close", .28f, 62f, .34f, 13);
            cabinetOpen = Creak("cabinet open", .36f, 128f, 202f, 19);
            cabinetClose = Thud("cabinet close", .18f, 105f, .24f, 23);
            pickup = SoftStep("pickup", .12f, 390f, .19f, 29);
            distantCreak = Creak("distant tree creak", 1.55f, 40f, 72f, 31);
            distantGust = Gust("distant wind gust", 1.9f, .25f, 47);
            houseTick = Thud("house settling", .16f, 72f, .12f, 53);
        }

        AudioClip Wind(string name,float seconds,float amp,int seed) {
            int sr=22050, count=Mathf.RoundToInt(sr*seconds);
            float[] data=new float[count];
            System.Random r=new System.Random(seed);
            float slow=0f, mid=0f, drift=0f;
            for(int i=0;i<count;i++) {
                float n=(float)(r.NextDouble()*2.0-1.0);
                slow = slow*.992f + n*.008f;
                mid = mid*.86f + n*.14f;
                float u=i/(float)count;
                drift = .72f + .18f*Mathf.Sin(u*Mathf.PI*2f*1.3f) + .10f*Mathf.Sin(u*Mathf.PI*2f*3.1f);
                data[i]=(slow*.78f+mid*.22f)*amp*drift;
            }
            AudioClip c=AudioClip.Create(name,count,1,sr,false); c.SetData(data,0); return c;
        }

        AudioClip Gust(string name,float seconds,float amp,int seed) {
            int sr=22050,count=Mathf.RoundToInt(sr*seconds);
            float[] data=new float[count];
            System.Random r=new System.Random(seed);
            float low=0f;
            for(int i=0;i<count;i++) {
                float u=i/(float)(count-1);
                float env=Mathf.Sin(Mathf.PI*u);
                env*=env;
                float n=(float)(r.NextDouble()*2-1);
                low=low*.975f+n*.025f;
                data[i]=low*env*amp;
            }
            AudioClip c=AudioClip.Create(name,count,1,sr,false);c.SetData(data,0);return c;
        }

        AudioClip SoftCrunch(string name,float seconds,float amp,int seed,float softness) {
            int sr=22050,count=Mathf.RoundToInt(sr*seconds);
            float[] data=new float[count];
            System.Random r=new System.Random(seed);
            float low=0f,mid=0f;
            for(int i=0;i<count;i++) {
                float u=i/(float)(count-1);
                float attack=Mathf.Clamp01(u/.12f);
                float release=Mathf.Pow(1f-u,2.0f);
                float env=attack*release;
                float n=(float)(r.NextDouble()*2-1);
                low=low*.82f+n*.18f;
                mid=mid*.48f+n*.52f;
                float tiny=(r.NextDouble()<.006?(float)(r.NextDouble()*2-1)*.18f:0f);
                data[i]=(low*softness+mid*(1f-softness)*.45f+tiny)*env*amp;
            }
            AudioClip c=AudioClip.Create(name,count,1,sr,false);c.SetData(data,0);return c;
        }

        AudioClip SoftStep(string name,float seconds,float hz,float amp,int seed) {
            int sr=22050,count=Mathf.RoundToInt(sr*seconds);
            float[] data=new float[count];
            System.Random r=new System.Random(seed);
            float phase=0f,noise=0f;
            for(int i=0;i<count;i++) {
                float u=i/(float)(count-1);
                float env=Mathf.Sin(Mathf.PI*Mathf.Clamp01(u*1.18f))*Mathf.Pow(1f-u,.72f);
                phase+=2f*Mathf.PI*hz/sr;
                float n=(float)(r.NextDouble()*2-1);
                noise=noise*.78f+n*.22f;
                data[i]=(noise*.60f+Mathf.Sin(phase)*.16f)*env*amp;
            }
            AudioClip c=AudioClip.Create(name,count,1,sr,false);c.SetData(data,0);return c;
        }

        AudioClip Thud(string name,float seconds,float hz,float amp,int seed) {
            int sr=22050,count=Mathf.RoundToInt(sr*seconds);
            float[] data=new float[count];
            System.Random r=new System.Random(seed);
            float phase=0f,noise=0f;
            for(int i=0;i<count;i++) {
                float t=i/(float)sr;
                float env=Mathf.Exp(-t*18f);
                phase+=2f*Mathf.PI*hz/sr;
                float n=(float)(r.NextDouble()*2-1);
                noise=noise*.70f+n*.30f;
                data[i]=(Mathf.Sin(phase)*.48f+noise*.16f)*env*amp;
            }
            AudioClip c=AudioClip.Create(name,count,1,sr,false);c.SetData(data,0);return c;
        }

        AudioClip Creak(string name,float seconds,float fromHz,float toHz,int seed) {
            int sr=22050,count=Mathf.RoundToInt(sr*seconds);
            float[] data=new float[count];
            System.Random r=new System.Random(seed);
            float phase=0f,lp=0f;
            for(int i=0;i<count;i++) {
                float u=i/(float)(count-1);
                float hz=Mathf.Lerp(fromHz,toHz,u)+Mathf.Sin(u*17f)*8f;
                phase+=2f*Mathf.PI*hz/sr;
                float env=Mathf.Sin(Mathf.PI*u);
                float n=(float)(r.NextDouble()*2-1);
                lp=lp*.94f+n*.06f;
                data[i]=(Mathf.Sin(phase)*.36f+lp*.19f)*env*.52f;
            }
            AudioClip c=AudioClip.Create(name,count,1,sr,false);c.SetData(data,0);return c;
        }
    }
}
