#if UNITY_EDITOR
namespace Rosvik.Blackout.EditorTools {
    // Temporary compile shim for the procedural scene builder.
    // RosvikAutoSetup historically used the unqualified name `Scene` without
    // importing UnityEngine.SceneManagement. Keep that legacy code compiling,
    // while exposing the scene members used by newer editor passes.
    public struct Scene {
        UnityEngine.SceneManagement.Scene inner;

        public static implicit operator Scene(UnityEngine.SceneManagement.Scene value) {
            return new Scene { inner = value };
        }

        public static implicit operator UnityEngine.SceneManagement.Scene(Scene value) {
            return value.inner;
        }

        public string name {
            get { return inner.name; }
            set { /* The scene asset name is established by SaveScene(path). */ }
        }

        public string path {
            get { return inner.path; }
        }

        public bool isLoaded {
            get { return inner.isLoaded; }
        }

        public bool IsValid() {
            return inner.IsValid();
        }
    }
}
#endif