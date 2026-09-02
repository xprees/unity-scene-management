using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Xprees.SceneManagement.Events.ScriptableObjects;
using Xprees.SceneManagement.Extensions;
using Xprees.SceneManagement.ScriptableObjects;

namespace Xprees.SceneManagement.Initialization.InitializationHandlers
{
    [CreateAssetMenu(menuName = "CF/Initialization/Desktop Initializer", fileName = "DesktopInitializationHandler")]
    public class DesktopInitializationHandlerSO : AbstractInitializationHandlerSO
    {
        [Header("Scene References")]
        [Tooltip("Asset reference to the menu sceneData (SceneSO)")]
        [SerializeField] private AssetReferenceT<SceneSO> menuToLoadSceneDataReference;

        [Space]
        [Tooltip("Asset references to any additional scenes to load for desktop mode.")]
        [SerializeField] private List<AssetReferenceT<SceneSO>> additionalScenesToLoad;

        [Header("Broadcasting on")]
        [Tooltip("Reference to the event raised when menu should be loaded.")]
        [SerializeField] private AssetReferenceT<SceneEventChannelSO> loadMenuEventAssetReference;

        [Space]
        [Tooltip("Reference to the event raised when additional scenes should be loaded.")]
        [SerializeField] private AssetReferenceT<SceneEventChannelSO> loadAdditionalSceneEventReference;

        private SceneSO _menuToLoad;

        public override async UniTask TriggerInitializationAsync(CancellationToken cancellationToken = default)
        {
            await LoadMenu(cancellationToken);
            await WaitUntilMenuIsLoaded(cancellationToken);
            await LoadAdditionalScenes(cancellationToken);
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

        private async UniTask LoadAdditionalScenes(CancellationToken cancellationToken)
        {
            if (additionalScenesToLoad == null || additionalScenesToLoad.Count == 0) return;

            var loadAdditionalSceneEventChannel = await loadAdditionalSceneEventReference
                .LoadAssetAsync<SceneEventChannelSO>().ToUniTask(cancellationToken: cancellationToken);

            var scenesToLoadTasks = new List<UniTask>();
            foreach (var sceneRef in additionalScenesToLoad)
            {
                if (sceneRef == null) continue;

                var scene = await sceneRef.LoadAssetAsync<SceneSO>().ToUniTask(cancellationToken: cancellationToken);
                if (!scene) continue;

                RaiseLoadAdditionalSceneEvent(loadAdditionalSceneEventChannel, scene);

                scenesToLoadTasks.Add(
                    UniTask.WaitUntil(scene, s => s.IsReady(), cancellationToken: cancellationToken)
                );
            }

            await UniTask.WhenAll(scenesToLoadTasks).SuppressCancellationThrow();
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

        private void RaiseLoadAdditionalSceneEvent(SceneEventChannelSO loadAdditionalSceneEventChannel, SceneSO sceneToLoad)
        {
            if (loadAdditionalSceneEventChannel == null)
            {
                Debug.LogError("Additional scene load event channel is null. Cannot load scene.");
                return;
            }

            // Do not show screen transitions or loading spinners for additional background scenes (e.g. Camera)
            loadAdditionalSceneEventChannel.RaiseEvent(sceneToLoad, false, false);
        }

        private UniTask WaitUntilMenuIsLoaded(CancellationToken cancellationToken = default) =>
            UniTask.WaitUntil(IsMenuLoaded, cancellationToken: cancellationToken).SuppressCancellationThrow();

        private bool IsMenuLoaded() => _menuToLoad != null && _menuToLoad.IsLoaded && _menuToLoad.sceneInstance.HasValue;
    }
}