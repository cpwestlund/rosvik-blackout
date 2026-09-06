using UnityEngine;

namespace Rosvik.Blackout {
    [DefaultExecutionOrder(8000)]
    public sealed class CabinetSwingRuntimeV74 : MonoBehaviour {
        void Awake(){FixAll();}

        public void FixAll(){
            foreach(CozyInteractableV57 x in FindObjectsByType<CozyInteractableV57>(FindObjectsSortMode.None)){
                if(!x||x.kind!=CozyInteractableV57.Kind.Cabinet)continue;
                x.openEuler=Correct(x.movingPart,x.openEuler,x.closedEuler);
                x.openEuler2=Correct(x.movingPart2,x.openEuler2,x.closedEuler2);
                x.animationTime=Mathf.Max(.30f,x.animationTime);
            }
        }

        static Vector3 Correct(Transform hinge,Vector3 open,Vector3 closed){
            if(!hinge||hinge.childCount==0)return open;
            if(Mathf.Abs(open.y)<35f||Mathf.Abs(open.y)<Mathf.Abs(open.x))return open;
            float angle=Mathf.Clamp(Mathf.Abs(open.y),88f,108f);
            float sign=0f;
            string n=hinge.name.ToLowerInvariant();
            if(n.Contains("left")||n.Contains("vänster"))sign=1f;
            else if(n.Contains("right")||n.Contains("höger"))sign=-1f;
            else{
                Transform leaf=hinge.GetChild(0);
                if(Mathf.Abs(leaf.localPosition.x)>.02f)sign=Mathf.Sign(leaf.localPosition.x);
            }
            if(Mathf.Approximately(sign,0f))sign=Mathf.Sign(open.y);
            return new Vector3(closed.x,sign*angle,closed.z);
        }
    }
}
