using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Xprees.SceneManagement.Events.ScriptableObjects;
using Xprees.SceneManagement.ScriptableObjects;

namespace Xprees.SceneManagement.Initialization.InitializationHandlers
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
            await LoadMenu(cancellationToken);
            await WaitUntilMenuIsLoaded(cancellationToken);
        }

        public override UniTask UnloadHandlerAsync(CancellationToken cancellationToken = default)
        {
            _menuToLoad = null;
            return UniTask.CompletedTask;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _menuToLoad = null;
        }

        private async UniTask LoadMenu(CancellationToken cancellationToken = default)
        {
            var loadMenuEventChannel = await loadMenuEventAssetReference.LoadAssetAsync<SceneEventChannelSO>()
                .ToUniTask(cancellationToken: cancellationToken);
            _menuToLoad = await menuToLoadSceneDataReference.LoadAssetAsync<SceneSO>()
                .ToUniTask(cancellationToken: cancellationToken);

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

        private UniTask WaitUntilMenuIsLoaded(CancellationToken cancellationToken = default) =>
            UniTask.WaitUntil(IsMenuLoaded, cancellationToken: cancellationToken).SuppressCancellationThrow();

        private bool IsMenuLoaded() => _menuToLoad != null && _menuToLoad.IsLoaded && _menuToLoad.sceneInstance.HasValue;
    }
}