using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Rosvik.Blackout {
    [DefaultExecutionOrder(9800)]
    public sealed class WorldSoundscapeV80 : MonoBehaviour {
        public float masterVolume = .78f;
        public float windVolume = .30f;
        public float footstepVolume = .42f;
        public float interactionVolume = .52f;

        CoziPlayerV57 player;
        SurvivalSystemsV69 survival;
        AudioSource windSource;
        AudioSource oneShotSource;
        AudioClip windLoop;
        AudioClip snowStepA, snowStepB, indoorStepA, indoorStepB, gravelStepA, gravelStepB;
        AudioClip doorOpen, doorClose, cabinetOpen, cabinetClose, pickup, distantCreak;
        Vector3 lastPos;
        float stepTravel;
        float nextStatePoll;
        float nextDistantSound;
        int lastInventoryCount = -1;
        readonly Dictionary<int,bool> doorStates = new Dictionary<int,bool>();
        readonly Dictionary<int,bool> containerStates = new Dictionary<int,bool>();
        System.Random rng = new System.Random(8026);

        void Awake() {
            player = GetComponent<CoziPlayerV57>();
            if (!player) player = FindFirstObjectByType<CoziPlayerV57>();
            survival = GetComponent<SurvivalSystemsV69>();
            if (!survival) survival = FindFirstObjectByType<SurvivalSystemsV69>();
            BuildClips();
            windSource = gameObject.AddComponent<AudioSource>();
            windSource.playOnAwake = false; windSource.loop = true; windSource.spatialBlend = 0f; windSource.priority = 180;
            windSource.clip = windLoop; windSource.volume = 0f; windSource.Play();
            oneShotSource = gameObject.AddComponent<AudioSource>();
            oneShotSource.playOnAwake = false; oneShotSource.loop = false; oneShotSource.spatialBlend = 0f; oneShotSource.priority = 120;
            if (player) lastPos = player.transform.position;
            PrimeStates();
            nextDistantSound = Time.time + 8f;
        }

        void Update() {
            if (!player) return;
            UpdateWind();
            UpdateFootsteps();
            if (Time.time >= nextStatePoll) {
                nextStatePoll = Time.time + .15f;
                PollInteractions();
                PollInventory();
            }
            if (Time.time >= nextDistantSound) {
                nextDistantSound = Time.time + UnityEngine.Random.Range(11f, 24f);
                if (!IsSheltered(player.transform.position)) Play(distantCreak, .20f + WindFactor() * .14f, UnityEngine.Random.Range(.88f, 1.08f));
            }
        }

        void UpdateWind() {
            if (!windSource) return;
            bool inside = IsSheltered(player.transform.position);
            float target = masterVolume * windVolume * WindFactor() * (inside ? .15f : 1f);
            windSource.volume = Mathf.MoveTowards(windSource.volume, target, Time.deltaTime * .22f);
            windSource.pitch = Mathf.Lerp(.90f, 1.10f, Mathf.Clamp01(WindFactor() * .75f));
        }

        float WindFactor() {
            if (!survival) return .65f;
            try {
                FieldInfo f = survival.GetType().GetField("wind", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
                if (f != null) {
                    object v = f.GetValue(survival);
                    if (v is float x) return Mathf.Clamp(.42f + x * .13f, .45f, 1.35f);
                }
            } catch {}
            return .72f;
        }

        void UpdateFootsteps() {
            Vector3 p = player.transform.position;
            Vector3 flat = p - lastPos; flat.y = 0f;
            float d = flat.magnitude;
            lastPos = p;
            if (d > 1.5f) { stepTravel = 0f; return; }
            stepTravel += d;
            float speed = d / Mathf.Max(.001f, Time.deltaTime);
            if (speed < .35f) return;
            float spacing = speed > 4.6f ? 1.05f : 1.35f;
            if (stepTravel < spacing) return;
            stepTravel = 0f;
            Surface s = SurfaceAt(p);
            AudioClip clip;
            switch(s) {
                case Surface.Indoor: clip = UnityEngine.Random.value < .5f ? indoorStepA : indoorStepB; break;
                case Surface.Gravel: clip = UnityEngine.Random.value < .5f ? gravelStepA : gravelStepB; break;
                default: clip = UnityEngine.Random.value < .5f ? snowStepA : snowStepB; break;
            }
            Play(clip, footstepVolume * masterVolume * (speed > 4.6f ? 1.0f : .82f), UnityEngine.Random.Range(.94f, 1.07f));
        }

        enum Surface { Snow, Gravel, Indoor }
        Surface SurfaceAt(Vector3 p) {
            if (IsSheltered(p)) return Surface.Indoor;
            RaycastHit hit;
            if (Physics.Raycast(p + Vector3.up*.7f, Vector3.down, out hit, 2f, ~0, QueryTriggerInteraction.Ignore)) {
                string n = hit.collider ? hit.collider.gameObject.name.ToLowerInvariant() : "";
                if (n.Contains("road") || n.Contains("lane") || n.Contains("gravel") || n.Contains("driveway") || n.Contains("parking") || n.Contains("path")) return Surface.Gravel;
                if (n.Contains("floor")) return Surface.Indoor;
            }
            return Surface.Snow;
        }

        bool IsSheltered(Vector3 p) {
            // School / sporthall block.
            if (p.x > -19.5f && p.x < 45.2f && p.z > -2.8f && p.z < 16.8f) return true;
            // House A.
            if (p.x > -36.5f && p.x < -21.5f && p.z > 2.45f && p.z < 16.1f) return true;
            // House B.
            if (p.x > -36.3f && p.x < -21.7f && p.z > 22.2f && p.z < 35.7f) return true;
            // Garage.
            if (p.x > 51.5f && p.x < 65.4f && p.z > 4.3f && p.z < 15.5f) return true;
            return false;
        }

        void PrimeStates() {
            doorStates.Clear(); containerStates.Clear();
            foreach (HouseInteriorDoorV77 d in FindObjectsByType<HouseInteriorDoorV77>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (d) doorStates[d.GetInstanceID()] = d.IsOpen;
            foreach (HouseFrontDoorV763 d in FindObjectsByType<HouseFrontDoorV763>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (d) doorStates[d.GetInstanceID()] = d.IsOpen;
            foreach (LootContainerV74 c in FindObjectsByType<LootContainerV74>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (c) containerStates[c.GetInstanceID()] = c.IsOpen;
            lastInventoryCount = InventoryCount();
        }

        void PollInteractions() {
            foreach (HouseInteriorDoorV77 d in FindObjectsByType<HouseInteriorDoorV77>(FindObjectsSortMode.None)) {
                if (!d) continue; int id=d.GetInstanceID(); bool state=d.IsOpen;
                if (!doorStates.TryGetValue(id,out bool prev)) { doorStates[id]=state; continue; }
                if (state!=prev) { doorStates[id]=state; Play(state?doorOpen:doorClose, interactionVolume*masterVolume, UnityEngine.Random.Range(.94f,1.04f)); }
            }
            foreach (HouseFrontDoorV763 d in FindObjectsByType<HouseFrontDoorV763>(FindObjectsSortMode.None)) {
                if (!d) continue; int id=d.GetInstanceID(); bool state=d.IsOpen;
                if (!doorStates.TryGetValue(id,out bool prev)) { doorStates[id]=state; continue; }
                if (state!=prev) { doorStates[id]=state; Play(state?doorOpen:doorClose, interactionVolume*masterVolume, UnityEngine.Random.Range(.90f,1.00f)); }
            }
            foreach (LootContainerV74 c in FindObjectsByType<LootContainerV74>(FindObjectsSortMode.None)) {
                if (!c) continue; int id=c.GetInstanceID(); bool state=c.IsOpen;
                if (!containerStates.TryGetValue(id,out bool prev)) { containerStates[id]=state; continue; }
                if (state!=prev) { containerStates[id]=state; Play(state?cabinetOpen:cabinetClose, interactionVolume*masterVolume*.72f, UnityEngine.Random.Range(.95f,1.06f)); }
            }
        }

        void PollInventory() {
            int n = InventoryCount();
            if (lastInventoryCount >= 0 && n > lastInventoryCount) Play(pickup, interactionVolume*masterVolume*.68f, UnityEngine.Random.Range(.96f,1.08f));
            lastInventoryCount=n;
        }
        int InventoryCount() { if (!player) return 0; int n=0; foreach (var kv in player.Inventory) n += Mathf.Max(0,kv.Value); return n; }

        void Play(AudioClip clip,float volume,float pitch=1f) {
            if (!clip || !oneShotSource) return;
            oneShotSource.pitch=pitch;
            oneShotSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        void BuildClips() {
            windLoop = NoiseLoop("V80 winter wind", 8f, .20f, .015f, 5);
            snowStepA = Crunch("snow crunch A", .19f, .78f, 41); snowStepB = Crunch("snow crunch B", .21f, .70f, 77);
            gravelStepA = Crunch("gravel step A", .16f, .52f, 113); gravelStepB = Crunch("gravel step B", .17f, .48f, 171);
            indoorStepA = Knock("indoor step A", .13f, 110f, .34f, 2); indoorStepB = Knock("indoor step B", .14f, 96f, .32f, 7);
            doorOpen = Creak("door open", .58f, 78f, 145f, 11); doorClose = Thump("door close", .32f, 68f, 13);
            cabinetOpen = Creak("cabinet open", .34f, 130f, 205f, 19); cabinetClose = Thump("cabinet close", .20f, 125f, 23);
            pickup = Knock("pickup", .11f, 520f, .18f, 29);
            distantCreak = Creak("distant tree creak", 1.7f, 42f, 74f, 31);
        }

        AudioClip NoiseLoop(string name,float seconds,float amp,float smooth,int seed) {
            int sr=22050, count=Mathf.RoundToInt(sr*seconds); float[] data=new float[count]; System.Random r=new System.Random(seed); float x=0f, slow=0f;
            for(int i=0;i<count;i++) { float n=(float)(r.NextDouble()*2.0-1.0); x=Mathf.Lerp(x,n,smooth); slow=.9985f*slow+.0015f*n; data[i]=(x*.72f+slow*.55f)*amp; }
            AudioClip c=AudioClip.Create(name,count,1,sr,false); c.SetData(data,0); return c;
        }
        AudioClip Crunch(string name,float seconds,float amp,int seed) {
            int sr=22050,count=Mathf.RoundToInt(sr*seconds);float[] data=new float[count];System.Random r=new System.Random(seed);float lp=0f;
            for(int i=0;i<count;i++){float t=i/(float)count;float env=Mathf.Pow(1f-t,2.4f);float n=(float)(r.NextDouble()*2-1);lp=lp*.55f+n*.45f;float grit=(i%Mathf.Max(2,18+(seed%7))==0?(float)r.NextDouble()*.9f:0f);data[i]=(lp*.72f+grit)*env*amp;}
            AudioClip c=AudioClip.Create(name,count,1,sr,false);c.SetData(data,0);return c;
        }
        AudioClip Knock(string name,float seconds,float hz,float amp,int seed) {
            int sr=22050,count=Mathf.RoundToInt(sr*seconds);float[] data=new float[count];System.Random r=new System.Random(seed);float phase=0f;
            for(int i=0;i<count;i++){float t=i/(float)sr;float env=Mathf.Exp(-t*26f);phase+=2f*Mathf.PI*hz/sr;float n=(float)(r.NextDouble()*2-1);data[i]=(Mathf.Sin(phase)*.72f+n*.16f)*env*amp;}
            AudioClip c=AudioClip.Create(name,count,1,sr,false);c.SetData(data,0);return c;
        }
        AudioClip Thump(string name,float seconds,float hz,int seed) { return Knock(name,seconds,hz,.46f,seed); }
        AudioClip Creak(string name,float seconds,float fromHz,float toHz,int seed) {
            int sr=22050,count=Mathf.RoundToInt(sr*seconds);float[] data=new float[count];System.Random r=new System.Random(seed);float phase=0f,lp=0f;
            for(int i=0;i<count;i++){float u=i/(float)(count-1);float hz=Mathf.Lerp(fromHz,toHz,u)+Mathf.Sin(u*17f)*9f;phase+=2f*Mathf.PI*hz/sr;float env=Mathf.Sin(Mathf.PI*u);float n=(float)(r.NextDouble()*2-1);lp=lp*.93f+n*.07f;data[i]=(Mathf.Sin(phase)*.42f+lp*.22f)*env*.55f;}
            AudioClip c=AudioClip.Create(name,count,1,sr,false);c.SetData(data,0);return c;
        }
    }
}
