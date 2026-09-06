using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rosvik.Blackout {
    public sealed class BuildingCutawayV79 : MonoBehaviour {
        public Transform shell;
        public GameObject details;
        public Vector2 minXZ;
        public Vector2 maxXZ;
        [Range(.05f,.8f)] public float insideWallAlpha = .18f;

        CoziPlayerV57 player;
        readonly List<Renderer> walls = new List<Renderer>();
        readonly Dictionary<Renderer,Material[]> originals = new Dictionary<Renderer,Material[]>();
        readonly Dictionary<Renderer,Material[]> faded = new Dictionary<Renderer,Material[]>();
        bool lastInside;
        bool initialized;

        void Awake(){
            player=FindFirstObjectByType<CoziPlayerV57>();
            Cache();
            Apply(true);
        }

        void Cache(){
            walls.Clear();originals.Clear();faded.Clear();
            if(!shell)return;
            foreach(Renderer r in shell.GetComponentsInChildren<Renderer>(true)){
                if(!r)continue;
                string n=r.gameObject.name.ToLowerInvariant();
                if(!(n=="wall"||n.Contains("wall cap")||n.Contains("outer wall")))continue;
                walls.Add(r);
                Material[] src=r.sharedMaterials;originals[r]=src;
                Material[] dst=new Material[src.Length];
                for(int i=0;i<src.Length;i++)dst[i]=MakeTransparent(src[i],insideWallAlpha);
                faded[r]=dst;
            }
        }

        Material MakeTransparent(Material src,float alpha){
            if(!src)return null;
            Material m=new Material(src);Color c=Color.white;
            if(m.HasProperty("_BaseColor"))c=m.GetColor("_BaseColor");else if(m.HasProperty("_Color"))c=m.GetColor("_Color");
            c.a=alpha;if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);if(m.HasProperty("_Color"))m.SetColor("_Color",c);
            if(m.HasProperty("_Surface")){
                m.SetFloat("_Surface",1f);if(m.HasProperty("_Blend"))m.SetFloat("_Blend",0f);if(m.HasProperty("_SrcBlend"))m.SetFloat("_SrcBlend",5f);if(m.HasProperty("_DstBlend"))m.SetFloat("_DstBlend",10f);if(m.HasProperty("_ZWrite"))m.SetFloat("_ZWrite",0f);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=3000;
            } else {
                if(m.HasProperty("_Mode"))m.SetFloat("_Mode",3f);if(m.HasProperty("_SrcBlend"))m.SetFloat("_SrcBlend",5f);if(m.HasProperty("_DstBlend"))m.SetFloat("_DstBlend",10f);if(m.HasProperty("_ZWrite"))m.SetFloat("_ZWrite",0f);m.DisableKeyword("_ALPHATEST_ON");m.EnableKeyword("_ALPHABLEND_ON");m.renderQueue=3000;
            }
            return m;
        }

        bool Inside(){if(!player)return false;Vector3 p=player.transform.position;return p.x>minXZ.x&&p.x<maxXZ.x&&p.z>minXZ.y&&p.z<maxXZ.y;}
        void LateUpdate(){Apply(false);}
        void Apply(bool force){
            if(!player)player=FindFirstObjectByType<CoziPlayerV57>();if(!player)return;
            bool inside=Inside();if(!force&&initialized&&inside==lastInside)return;initialized=true;lastInside=inside;
            if(details)details.SetActive(inside);
            foreach(Renderer r in walls){if(!r)continue;if(inside&&faded.TryGetValue(r,out Material[] fm))r.sharedMaterials=fm;else if(originals.TryGetValue(r,out Material[] om))r.sharedMaterials=om;}
        }
        void OnDestroy(){foreach(var kv in faded)if(kv.Value!=null)foreach(Material m in kv.Value)if(m)Destroy(m);}
    }

    public sealed class BackpackUpgradeV79 : MonoBehaviour {
        public float radius=2.2f;
        public float newCapacityKg=45f;
        CoziPlayerV57 player;
        bool claimed;

        void Awake(){player=FindFirstObjectByType<CoziPlayerV57>();}
        void Update(){
            if(claimed||!player||player.externalUiBlocked)return;
            Vector3 d=transform.position-player.transform.position;d.y=0;if(d.sqrMagnitude>radius*radius)return;
            Keyboard kb=Keyboard.current;if(kb!=null&&kb.eKey.wasPressedThisFrame)Claim();
        }
        void Claim(){
            if(claimed||!player)return;claimed=true;
            SurvivalInventoryV76 inv=player.GetComponent<SurvivalInventoryV76>();if(inv)inv.backpackCapacityKg=Mathf.Max(inv.backpackCapacityKg,newCapacityKg);
            SurvivalLootTransferV74 transfer=player.GetComponent<SurvivalLootTransferV74>();if(transfer)transfer.backpackCapacityKg=Mathf.Max(transfer.backpackCapacityKg,newCapacityKg);
            player.ShowToast("VANDRINGSRYGGSÄCK — kapacitet "+Mathf.RoundToInt(newCapacityKg)+" kg",2.8f);
            player.SetObjective("Du kan bära mer nu. Fortsätt söka bostäder, uthus och verkstäder efter mat, värme och utrustning.");
            foreach(Renderer r in GetComponentsInChildren<Renderer>(true))if(r)r.enabled=false;
            foreach(Collider c in GetComponentsInChildren<Collider>(true))if(c)c.enabled=false;
            enabled=false;
        }
        void OnGUI(){
            if(claimed||!player||player.externalUiBlocked)return;Vector3 d=transform.position-player.transform.position;d.y=0;if(d.sqrMagnitude>radius*radius)return;
            Rect r=new Rect(Screen.width*.5f-180f,Screen.height-68f,360f,36f);Color o=GUI.color;GUI.color=new Color(.015f,.021f,.018f,.95f);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=o;
            GUIStyle s=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontSize=12,fontStyle=FontStyle.Bold};s.normal.textColor=new Color(.92f,.90f,.81f);GUI.Label(r,"E   BYT TILL VANDRINGSRYGGSÄCK — 45 KG",s);
        }
    }

    [DefaultExecutionOrder(9350)]
    public sealed class WorldExplorationV79 : MonoBehaviour {
        public Vector2 houseBMinXZ=new Vector2(-36.2f,22.2f);
        public Vector2 houseBMaxXZ=new Vector2(-21.8f,35.6f);
        CoziPlayerV57 player;
        SurvivalSystemsV69 survival;
        bool discovered;
        bool wasInside;

        void Awake(){player=GetComponent<CoziPlayerV57>();if(!player)player=FindFirstObjectByType<CoziPlayerV57>();survival=GetComponent<SurvivalSystemsV69>();if(!survival)survival=FindFirstObjectByType<SurvivalSystemsV69>();}
        void LateUpdate(){
            if(!player||!survival)return;Vector3 p=player.transform.position;bool inside=p.x>houseBMinXZ.x&&p.x<houseBMaxXZ.x&&p.z>houseBMinXZ.y&&p.z<houseBMaxXZ.y;
            if(inside){
                survival.wetness=Mathf.MoveTowards(survival.wetness,0f,1.35f*Time.deltaTime);survival.warmth=Mathf.MoveTowards(survival.warmth,70f,.14f*Time.deltaTime);
                foreach(string n in new[]{"V69 RAIN","V69 SNOW","V70 RAIN","V70 SNOW","V72 RAIN","V72 SNOW"}){Transform t=player.transform.Find(n);if(t&&t.gameObject.activeSelf)t.gameObject.SetActive(false);}
            }
            if(inside&&!discovered){discovered=true;player.ShowToast("HUS B — ett övergivet hem",2.5f);player.SetObjective("Sök igenom huset. Någon verkar ha lämnat kvar en större ryggsäck och vinterförråd.");}
            if(wasInside&&!inside&&discovered)player.SetObjective("Fortsätt utforska samhället. Platser har olika typer av loot — välj vad som är värt vikten.");
            wasInside=inside;
        }
    }
}
