#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikCampusBenchmarkV921 {
        const int Version = 921;
        const string Key = "ROSVIK_CAMPUS_BENCHMARK_V921";
        const string ScenePath = "Assets/Rosvik/Scenes/CozySchoolGame.unity";

        static RosvikCampusBenchmarkV921() {
            if (EditorPrefs.GetInt(Key,0) >= Version) return;
            EditorApplication.delayCall += Auto;
        }

        [MenuItem("Rosvik/V92.1 FIX CAMPUS SIGNS")]
        public static void Force() { EditorPrefs.DeleteKey(Key); EditorApplication.delayCall += Auto; }

        static void Auto() {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.delayCall += Auto; return; }
            if (!File.Exists(ScenePath)) return;
            try { Apply(); } catch(Exception ex) { Debug.LogError("V92.1 FAILED: "+ex); }
        }

        static void Apply() {
            if (EditorSceneManager.GetActiveScene().path != ScenePath) EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject campus = GameObject.Find("ROSVIK CAMPUS · V92 ASTRA BENCHMARK");
            if (!campus) return;
            Transform sport = Find(campus.transform,"ROSVIK SPORTHALL · V92");
            if (sport) {
                Transform sign = Find(sport,"SIGN ROSVIK SPORTHALL");
                if (sign) {
                    sign.position = new Vector3(4.34f,3.15f,-1f);
                    sign.rotation = Quaternion.Euler(0,90,0);
                    Transform board = Find(sign,"sign board");
                    if (board) board.localScale = new Vector3(5.2f,.48f,.08f);
                    Transform txt = Find(sign,"sign text");
                    if (txt) {
                        txt.localPosition = new Vector3(0,0,-.058f);
                        txt.localRotation = Quaternion.identity;
                    }
                }
            }
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            EditorPrefs.SetInt(Key,Version);
            SceneView.RepaintAll();
        }

        static Transform Find(Transform root,string exact) {
            foreach(Transform t in root.GetComponentsInChildren<Transform>(true)) if(t.name==exact) return t;
            return null;
        }
    }
}
#endif
