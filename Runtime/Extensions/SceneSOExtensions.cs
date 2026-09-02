using Xprees.SceneManagement.ScriptableObjects;

namespace Xprees.SceneManagement.Extensions
{
    public static class SceneSOExtensions
    {
        /// Thread-safe method to set the IsBeingProcessed property of the SceneSO
        public static void SetAsProcessed(this SceneSO scene, bool isProcessed)
        {
            lock (scene) scene.IsBeingProcessed = isProcessed;
        }

        /// Thread-safe method to set the IsLoaded property of the SceneSO
        public static void SetAsLoaded(this SceneSO scene, bool isLoaded)
        {
            lock (scene) scene.IsLoaded = isLoaded;
        }

        /// Whether the scene is loaded and ready 
        public static bool IsReady(this SceneSO scene) => scene && scene.IsLoaded && scene.sceneInstance.HasValue;
    }
}