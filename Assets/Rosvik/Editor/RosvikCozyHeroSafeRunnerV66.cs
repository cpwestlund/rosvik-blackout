#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Rosvik.Blackout.EditorTools {
    [InitializeOnLoad]
    public static class RosvikCozyHeroSafeRunnerV66 {
        const int Version = 66;
        const string Key = "ROSVIK_COZY_HERO_SAFE_V66";
        static bool queued;

        static RosvikCozyHeroSafeRunnerV66() {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            if (EditorPrefs.GetInt(Key, 0) < Version) {
                queued = true;
                EditorApplication.delayCall += TryRun;
            }
        }

        [MenuItem("Rosvik/V66 SAFE RUN V65 HERO POLISH")]
        public static void Force() {
            EditorPrefs.DeleteKey(Key);
            queued = true;
            EditorApplication.delayCall += TryRun;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state) {
            if (!queued) return;
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += TryRun;
        }

        static void TryRun() {
            if (!queued) return;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating) {
                EditorApplication.delayCall += TryRun;
                return;
            }

            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) {
                // Do not touch scenes/assets while entering, running or leaving Play Mode.
                // We wait for EnteredEditMode instead of repeatedly trying in the unsafe window.
                return;
            }

            queued = false;
            EditorPrefs.SetInt(Key, Version);

            // V65 failed previously because Unity entered Play Mode during its editor-only asset/scene work.
            // Trigger it now from a confirmed Edit Mode state.
            RosvikCozyHeroPolishV65.Force();
            Debug.Log("V66 SAFE RUNNER: Unity is fully in Edit Mode. Re-running V65 hero polish safely now.");
        }
    }
}
#endif
