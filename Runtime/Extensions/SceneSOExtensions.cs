using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
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

        /// Loads the scene using Addressables.LoadSceneAsync with the scene's AssetReference.
        /// Directly calls Addressables.LoadSceneAsync instead of AssetReference.LoadSceneAsync,
        /// avoiding caching the operation handle on the AssetReference instance which causes
        /// errors when Domain Reload is disabled.
        public static AsyncOperationHandle<SceneInstance> LoadSceneAsync(
            this SceneSO scene,
            LoadSceneMode loadMode = LoadSceneMode.Additive,
            bool activateOnLoad = true,
            int priority = 100
        ) => scene.sceneReference.LoadSceneAddressableAsync(loadMode, activateOnLoad, priority);

        /// Unloads the scene using Addressables.UnloadSceneAsync.
        public static AsyncOperationHandle<SceneInstance> UnloadSceneAsync(
            this SceneSO scene,
            bool autoReleaseHandle = true
        )
        {
            if (scene.sceneInstance.HasValue)
            {
                return Addressables.UnloadSceneAsync(scene.sceneInstance.Value, autoReleaseHandle);
            }

            if (scene.sceneReference != null && scene.sceneReference.OperationHandle.IsValid())
            {
                return Addressables.UnloadSceneAsync(scene.sceneReference.OperationHandle, autoReleaseHandle);
            }

            return default;
        }
    }
}