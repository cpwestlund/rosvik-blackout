using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rosvik.Blackout {
    [DefaultExecutionOrder(12950)]
    public sealed class HouseVisualSafetyV842 : MonoBehaviour {
        public Vector2 minXZ;
        public Vector2 maxXZ;
        public float enterPadding = .55f;
        public float exitPadding = 1.05f;
        public GameObject roofRoot;

        CoziPlayerV57 player;
        bool inside;
        bool initialized;
        readonly Dictionary<Renderer,bool> hidden = new Dictionary<Renderer,bool>();

        void OnEnable(){ ResolvePlayer(); Apply(true); }
        void LateUpdate(){ if(!player || !player.gameObject.activeInHierarchy) ResolvePlayer(); Apply(false); }
        void OnDisable(){ Restore(); }

        void ResolvePlayer(){
            GameObject named=GameObject.Find("PLAYER");
            if(named) player=named.GetComponent<CoziPlayerV57>();
            if(player) return;
            foreach(var p in FindObjectsByType<CoziPlayerV57>(FindObjectsInactive.Exclude,FindObjectsSortMode.None)){
                if(p && p.gameObject.activeInHierarchy){ player=p; break; }
            }
        }

        bool Contains(Vector3 p,float pad){
            return p.x>minXZ.x-pad && p.x<maxXZ.x+pad && p.z>minXZ.y-pad && p.z<maxXZ.y+pad;
        }

        void Apply(bool force){
            if(!player) return;
            bool now=Contains(player.transform.position,inside?exitPadding:enterPadding);
            if(!force && initialized && now==inside) return;
            initialized=true; inside=now;
            if(roofRoot) roofRoot.SetActive(!inside);
            if(inside) HideStrayRoofPieces(); else Restore();
        }

        void HideStrayRoofPieces(){
            Restore();
            foreach(Renderer r in FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
                if(!r || !r.gameObject.scene.IsValid() || !r.enabled) continue;
                Vector3 c=r.bounds.center;
                if(c.x<minXZ.x-2.0f || c.x>maxXZ.x+2.0f || c.z<minXZ.y-2.0f || c.z>maxXZ.y+2.0f) continue;
                if(!LooksLikeRoof(r.transform)) continue;
                hidden[r]=r.enabled;
                r.enabled=false;
            }
        }

        bool LooksLikeRoof(Transform t){
            for(Transform x=t;x!=null && x.parent!=null;x=x.parent){
                string n=x.name.ToLowerInvariant();
                if(n.Contains("roof") || n.Contains("tak") || n.Contains("barge") || n.Contains("gable") ||
                   n.Contains("ridge") || n.Contains("soffit") || n.Contains("gutter") || n.Contains("chimney")) return true;
                if(x==transform) break;
            }
            return false;
        }

        void Restore(){
            if(hidden.Count==0) return;
            foreach(var kv in hidden) if(kv.Key) kv.Key.enabled=kv.Value;
            hidden.Clear();
        }
    }
}
