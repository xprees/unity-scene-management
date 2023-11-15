using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Xprees.SceneManagement.Events.ScriptableObjects;
using Xprees.SceneManagement.ScriptableObjects;

namespace Xprees.SceneManagement.Initialization
{
    [CreateAssetMenu(menuName = "CF/Scene Management/MainMenu Initializer", fileName = "MainMenuInitializationHandler")]
    public class MainMenuInitializationHandlerSO : AbstractInitializationHandlerSO
    {
        [Header("Menu References")]
        [Tooltip("Asset reference to the menu sceneData (SceneSO)")]
        [SerializeField] private AssetReferenceT<SceneSO> menuToLoadSceneDataReference;

        [Header("Broadcasting on")]
        [SerializeField] private AssetReferenceT<SceneEventChannelSO> loadMenuEventAssetReference;

        private SceneSO _menuToLoad;

        public override async UniTask TriggerInitializationAsync(CancellationToken cancellationToken = default)
        {
            await LoadMenu();
        }

        public override UniTask UnloadHandlerAsync(CancellationToken cancellationToken = default) => UniTask.CompletedTask;

        private void OnDisable()
        {
            _menuToLoad = null;
        }

        private async UniTask LoadMenu()
        {
            var loadMenuEventChannel = await loadMenuEventAssetReference.LoadAssetAsync<SceneEventChannelSO>();
            _menuToLoad = await menuToLoadSceneDataReference.LoadAssetAsync<SceneSO>();
            RaiseLoadMenuEvent(loadMenuEventChannel, _menuToLoad);
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
    }
}