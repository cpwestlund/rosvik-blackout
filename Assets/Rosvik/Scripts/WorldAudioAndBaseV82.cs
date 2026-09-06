using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rosvik.Blackout {
    [DefaultExecutionOrder(9900)]
    public sealed class WorldAudioEnhancerV82 : MonoBehaviour {
        CoziPlayerV57 player;
        SurvivalSystemsV69 survival;
        WorldSoundscapeV80 existing;
        AudioSource ambience;
        AudioSource detail;
        AudioSource stoveLayer;
        AudioClip windBed, gust, houseCreak, branchSnap, fireBed;
        float nextDetail;

        void Awake() {
            player=GetComponent<CoziPlayerV57>(); if(!player) player=FindFirstObjectByType<CoziPlayerV57>();
            survival=GetComponent<SurvivalSystemsV69>(); if(!survival) survival=FindFirstObjectByType<SurvivalSystemsV69>();
            existing=GetComponent<WorldSoundscapeV80>(); if(!existing) existing=FindFirstObjectByType<WorldSoundscapeV80>();
            TuneExisting(); BuildClips();
            ambience=gameObject.AddComponent<AudioSource>(); ambience.loop=true; ambience.playOnAwake=false; ambience.spatialBlend=0f; ambience.priority=165; ambience.clip=windBed; ambience.volume=0f; ambience.Play();
            detail=gameObject.AddComponent<AudioSource>(); detail.loop=false; detail.playOnAwake=false; detail.spatialBlend=0f; detail.priority=115;
            stoveLayer=gameObject.AddComponent<AudioSource>(); stoveLayer.loop=true; stoveLayer.playOnAwake=false; stoveLayer.spatialBlend=0f; stoveLayer.priority=105; stoveLayer.clip=fireBed; stoveLayer.volume=0f; stoveLayer.Play();
            nextDetail=Time.time+UnityEngine.Random.Range(5f,9f);
        }

        void TuneExisting() {
            if(!existing)return;
            existing.masterVolume=.96f;
            existing.windVolume=.66f;
            existing.footstepVolume=.24f;
            existing.interactionVolume=.82f;
        }

        void Update() {
            if(!player)return;
            if(!existing){existing=FindFirstObjectByType<WorldSoundscapeV80>();TuneExisting();}
            bool inside=IsSheltered(player.transform.position);
            float wind=WindFactor();
            float target=(inside?.045f:.16f)*wind;
            if(ambience){ambience.volume=Mathf.MoveTowards(ambience.volume,target,Time.deltaTime*.18f);ambience.pitch=Mathf.Lerp(.90f,1.06f,Mathf.Clamp01(wind*.7f));}
            UpdateStoveLayer();
            if(Time.time>=nextDetail){
                nextDetail=Time.time+UnityEngine.Random.Range(7f,15f);
                AudioClip clip=inside?houseCreak:(UnityEngine.Random.value<.58f?gust:branchSnap);
                float vol=inside?.24f:Mathf.Lerp(.27f,.46f,Mathf.Clamp01(wind));
                PlayDetail(clip,vol,UnityEngine.Random.Range(.91f,1.07f));
            }
        }

        void UpdateStoveLayer(){
            WoodStoveV81 stove=FindFirstObjectByType<WoodStoveV81>();
            float target=0f;
            if(stove&&stove.IsLit){Vector3 d=stove.transform.position-player.transform.position;d.y=0;float dist=d.magnitude;if(dist<10f)target=Mathf.Lerp(.42f,.06f,Mathf.Clamp01(dist/10f));}
            if(stoveLayer)stoveLayer.volume=Mathf.MoveTowards(stoveLayer.volume,target,Time.deltaTime*.65f);
        }

        float WindFactor(){return survival?Mathf.Clamp(.55f+survival.wind*.12f,.65f,1.45f):.9f;}
        bool IsSheltered(Vector3 p){
            if(p.x>-19.5f&&p.x<45.2f&&p.z>-2.8f&&p.z<16.8f)return true;
            if(p.x>-36.5f&&p.x<-21.5f&&p.z>2.45f&&p.z<16.1f)return true;
            if(p.x>-36.3f&&p.x<-21.7f&&p.z>22.2f&&p.z<35.7f)return true;
            if(p.x>51.5f&&p.x<65.4f&&p.z>4.3f&&p.z<15.5f)return true;
            return false;
        }
        void PlayDetail(AudioClip c,float v,float p){if(!detail||!c)return;detail.pitch=p;detail.PlayOneShot(c,Mathf.Clamp01(v));}

        void BuildClips(){windBed=Wind("V82 audible winter ambience",7f,820);gust=Gust("V82 wind gust",2.2f,821);houseCreak=Creak("V82 house settling",1.1f,62f,113f,822);branchSnap=Snap("V82 branch snap",.45f,823);fireBed=Fire("V82 stove crackle layer",4f,824);}
        AudioClip Wind(string n,float sec,int seed){int sr=22050,cnt=Mathf.RoundToInt(sr*sec);float[] d=new float[cnt];System.Random r=new System.Random(seed);float low=0,slow=0;for(int i=0;i<cnt;i++){float x=(float)(r.NextDouble()*2-1);low=low*.965f+x*.035f;slow=slow*.9975f+x*.0025f;d[i]=(low*.55f+slow*.85f)*.24f;}AudioClip c=AudioClip.Create(n,cnt,1,sr,false);c.SetData(d,0);return c;}
        AudioClip Gust(string n,float sec,int seed){int sr=22050,cnt=Mathf.RoundToInt(sr*sec);float[] d=new float[cnt];System.Random r=new System.Random(seed);float low=0;for(int i=0;i<cnt;i++){float u=i/(float)(cnt-1),env=Mathf.Sin(Mathf.PI*u);float x=(float)(r.NextDouble()*2-1);low=low*.94f+x*.06f;d[i]=low*env*.46f;}AudioClip c=AudioClip.Create(n,cnt,1,sr,false);c.SetData(d,0);return c;}
        AudioClip Creak(string n,float sec,float a,float b,int seed){int sr=22050,cnt=Mathf.RoundToInt(sr*sec);float[] d=new float[cnt];float ph=0;System.Random r=new System.Random(seed);for(int i=0;i<cnt;i++){float u=i/(float)(cnt-1),hz=Mathf.Lerp(a,b,u)+Mathf.Sin(u*15f)*7f;ph+=2*Mathf.PI*hz/sr;float env=Mathf.Sin(Mathf.PI*u);d[i]=(Mathf.Sin(ph)*.38f+(float)(r.NextDouble()*2-1)*.06f)*env*.55f;}AudioClip c=AudioClip.Create(n,cnt,1,sr,false);c.SetData(d,0);return c;}
        AudioClip Snap(string n,float sec,int seed){int sr=22050,cnt=Mathf.RoundToInt(sr*sec);float[] d=new float[cnt];System.Random r=new System.Random(seed);float lp=0;for(int i=0;i<cnt;i++){float u=i/(float)cnt,env=Mathf.Exp(-u*11f);float x=(float)(r.NextDouble()*2-1);lp=lp*.55f+x*.45f;d[i]=lp*env*.62f;}AudioClip c=AudioClip.Create(n,cnt,1,sr,false);c.SetData(d,0);return c;}
        AudioClip Fire(string n,float sec,int seed){int sr=22050,cnt=Mathf.RoundToInt(sr*sec);float[] d=new float[cnt];System.Random r=new System.Random(seed);float lp=0;for(int i=0;i<cnt;i++){float x=(float)(r.NextDouble()*2-1);lp=lp*.82f+x*.18f;float pop=r.NextDouble()<.0024?(float)r.NextDouble()*.8f:0;d[i]=(lp*.20f+x*.07f+pop)*.48f;}AudioClip c=AudioClip.Create(n,cnt,1,sr,false);c.SetData(d,0);return c;}
    }

    public sealed class FirewoodPileV82 : MonoBehaviour {
        public int remaining=18;
        public int takePerUse=2;
        public float radius=2.3f;
        CoziPlayerV57 player;
        void Awake(){player=FindFirstObjectByType<CoziPlayerV57>();}
        void Update(){if(remaining<=0||!player||player.externalUiBlocked)return;Vector3 d=transform.position-player.transform.position;d.y=0;if(d.sqrMagnitude>radius*radius)return;Keyboard k=Keyboard.current;if(k!=null&&k.eKey.wasPressedThisFrame)Take();}
        void Take(){int n=Mathf.Min(takePerUse,remaining);remaining-=n;player.AddItem("Träspill",n);player.ShowToast("Tog torr ved x"+n+"  •  kvar "+remaining,1.7f);if(remaining<=0)player.SetObjective("Vedboden är tömd på lättillgänglig ved. Leta efter mer bränsle innan kylan blir värre.");}
        void OnGUI(){if(remaining<=0||!player||player.externalUiBlocked)return;Vector3 d=transform.position-player.transform.position;d.y=0;if(d.sqrMagnitude>radius*radius)return;Rect r=new Rect(Screen.width*.5f-205,Screen.height-70,410,38);Color o=GUI.color;GUI.color=new Color(.014f,.02f,.017f,.96f);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=o;GUIStyle s=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontSize=12,fontStyle=FontStyle.Bold};s.normal.textColor=new Color(.94f,.90f,.79f);GUI.Label(r,"E   TA VED   •   KVAR "+remaining,s);}
    }
}
