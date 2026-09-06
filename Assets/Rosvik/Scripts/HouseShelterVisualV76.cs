using UnityEngine;

namespace Rosvik.Blackout {
    [DefaultExecutionOrder(9500)]
    public sealed class HouseShelterVisualV76 : MonoBehaviour {
        CoziPlayerV57 player;
        SurvivalSystemsV69 survival;
        void Awake(){player=GetComponent<CoziPlayerV57>();if(!player)player=FindFirstObjectByType<CoziPlayerV57>();survival=GetComponent<SurvivalSystemsV69>();if(!survival)survival=FindFirstObjectByType<SurvivalSystemsV69>();}
        void LateUpdate(){
            if(!player||!survival||!InHouse(player.transform.position))return;
            survival.wetness=Mathf.MoveTowards(survival.wetness,0f,1.4f*Time.deltaTime);
            survival.warmth=Mathf.MoveTowards(survival.warmth,72f,.18f*Time.deltaTime);
            foreach(string n in new[]{"V69 RAIN","V69 SNOW","V70 RAIN","V70 SNOW","V72 RAIN","V72 SNOW"}){Transform t=player.transform.Find(n);if(t&&t.gameObject.activeSelf)t.gameObject.SetActive(false);}
        }
        bool InHouse(Vector3 p){return p.x>-36.25f&&p.x<-21.75f&&p.z>2.15f&&p.z<15.85f;}
    }
}
