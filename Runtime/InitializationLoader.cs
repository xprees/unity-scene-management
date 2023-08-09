using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using Xprees.SceneManagement.Events.ScriptableObjects;
using Xprees.SceneManagement.ScriptableObjects;

namespace Xprees.SceneManagement
{
    public class InitializationLoader : MonoBehaviour
    {
        // Warning: References must be Addressables to avoid duplication in build,
        // otherwise assets copies will be included one in player build and one in addressables build
        [Header("Scene References")]
        [Tooltip("Asset reference to the persistent managers sceneData (SceneSO)")]
        [SerializeField] private AssetReference managersSceneDataReference;

        [Tooltip("Asset reference to the menu sceneData (SceneSO)")]
        [SerializeField] private AssetReference menuToLoadSceneDataReference;

        [Header("Listening on")]
        [SerializeField] private AssetReference sceneLoadedEventAssetReference;

        [Header("Broadcasting on")]
        [SerializeField] private AssetReference loadMenuEventAssetReference;

        private SceneSO _menuToLoad;
        private SceneEventChannelSO _sceneLoadedEventChannel;

        private async void Start()
        {
            var managersScene = await managersSceneDataReference.LoadAssetAsync<SceneSO>();
            await managersScene.sceneReference.LoadSceneAsync(LoadSceneMode.Additive, true);

            var loadMenuEventChannel = await loadMenuEventAssetReference.LoadAssetAsync<SceneEventChannelSO>();
            _menuToLoad = await menuToLoadSceneDataReference.LoadAssetAsync<SceneSO>();
            RaiseLoadMenuEvent(loadMenuEventChannel, _menuToLoad);

            await UnloadWhenMenuIsLoaded();
        }

        private async UniTask UnloadWhenMenuIsLoaded()
        {
            _sceneLoadedEventChannel = await sceneLoadedEventAssetReference.LoadAssetAsync<SceneEventChannelSO>();
            _sceneLoadedEventChannel.onEventRaised += OnSceneLoaded;
        }

        private async void OnSceneLoaded(SceneSO scene, bool _, bool __)
        {
            if (scene != _menuToLoad) return;

            _menuToLoad = null;
            await SceneManager.UnloadSceneAsync(0); // only scene in build settings
        }

        private void RaiseLoadMenuEvent(SceneEventChannelSO loadMenuEventChannel, SceneSO menuToLoad)
        {
            if (loadMenuEventChannel == null)
            {
                Debug.LogError("Menu load event channel is null. Cannot load menu.");
                return;
            }

            loadMenuEventChannel.RaiseEvent(menuToLoad, true, false);
        }

        private void OnDisable()
        {
            _sceneLoadedEventChannel.onEventRaised -= OnSceneLoaded;
            _sceneLoadedEventChannel = null;
            _menuToLoad = null;
        }
    }
}