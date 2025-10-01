using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using Xprees.Events.ScriptableObjects.Base;
using Xprees.SceneManagement.Extensions;
using Xprees.SceneManagement.Initialization.InitializationHandlers;
using Xprees.SceneManagement.ScriptableObjects;

namespace Xprees.SceneManagement.Initialization
{
    public class InitializationLoader : MonoBehaviour
    {
        // Warning: References must be Addressables to avoid duplication in build,
        // otherwise assets copies will be included one in player build and one in addressables build
        [Header("Scene References")]
        [Tooltip("Asset reference to the persistent managers sceneData (SceneSO)")]
        [SerializeField] private AssetReferenceT<SceneSO> managersSceneDataReference;

        [Header("Listening to")]
        [SerializeField] private AssetReferenceT<VoidEventChannelSO> startHandlersInitializationEventRef;

        [Header("Additional Initialization Handlers")]
        [SerializeField] private List<AbstractInitializationHandlerSO> initializationHandlers;

        private IEnumerable<AbstractInitializationHandlerSO> ActiveHandlers => initializationHandlers.Where(handler => handler.IsActive);


        private VoidEventChannelSO _startHandlersInitializationEvent;

        private async void Awake()
        {
            var token = this.GetCancellationTokenOnDestroy();

            // Managers scene must be loaded first and in Awake
            // to have all PersistentManagers ready for calls from the other scripts on Start
            await LoadManagersScene(token)
                .SuppressCancellationThrow();
            CheckActiveHandlers();
        }

        private void OnDisable()
        {
            _startHandlersInitializationEvent.onEventRaised -= StartHandlersInitialization;
        }

        private async void StartHandlersInitialization()
        {
            var token = this.GetCancellationTokenOnDestroy();

            await InitializeHandlers(token);

            await TriggerInitHandlers(token);

            await UnloadHandlers(token);

            UnloadInitializationScene().Forget(); // Will also unload this script we don't need to wait for that :-)
        }

        private async UniTask InitializeHandlers(CancellationToken cancellationToken)
        {
            var initTasks = initializationHandlers // Initialize all handlers - some need to initialize to decide if they are active 
                .Select(handler => handler.InitializeHandlerAsync(cancellationToken))
                .ToList();

            await UniTask.WhenAll(initTasks).SuppressCancellationThrow();
        }

        private async UniTask TriggerInitHandlers(CancellationToken cancellationToken)
        {
            var triggerTasks = ActiveHandlers
                .Select(handler => handler.TriggerInitializationAsync(cancellationToken))
                .ToList();

            await UniTask.WhenAll(triggerTasks).SuppressCancellationThrow();
        }

        private async UniTask UnloadHandlers(CancellationToken cancellationToken)
        {
            var unloadTasks = initializationHandlers
                .Select(handler => handler.UnloadHandlerAsync(cancellationToken))
                .ToList();

            await UniTask.WhenAll(unloadTasks).SuppressCancellationThrow();
        }

        private async UniTask LoadManagersScene(CancellationToken cancellationToken)
        {
            _startHandlersInitializationEvent = await startHandlersInitializationEventRef.LoadAssetAsync<VoidEventChannelSO>()
                .ToUniTask(cancellationToken: cancellationToken);

            _startHandlersInitializationEvent.onEventRaised += StartHandlersInitialization;

            var managersScene = await managersSceneDataReference.LoadAssetAsync<SceneSO>()
                .ToUniTask(cancellationToken: cancellationToken);

            // Set as active to make sure if there are any created objects they are in the right scene and initialization scene
            await LoadSceneIfNotProcessed(managersScene, true, LoadSceneMode.Additive, cancellationToken);

            await UniTask.WaitUntil(IsManagersSceneReady, cancellationToken: cancellationToken);
            return;

            bool IsManagersSceneReady() => managersScene.sceneInstance.HasValue && managersScene.IsLoaded;
        }

        private UniTask UnloadInitializationScene() => SceneManager.UnloadSceneAsync(0).ToUniTask(); // only scene in build settings

        private void CheckActiveHandlers()
        {
            var disabledHandlers = initializationHandlers.Where(h => !h.IsActive).ToList();
            foreach (var disabledHandler in disabledHandlers)
            {
                Debug.LogWarning($"Initialization handler {disabledHandler.name} is disabled and will be skipped.");
            }

            if (!ActiveHandlers.Any())
            {
                Debug.LogWarning($"{nameof(InitializationLoader)} has {initializationHandlers.Count} initialization handlers. " +
                                 $"Only {ActiveHandlers.Count()} are active.");
            }
        }

        private async UniTask LoadSceneIfNotProcessed(
            SceneSO scene,
            bool setActive = false,
            LoadSceneMode loadMode = LoadSceneMode.Additive,
            CancellationToken token = default
        )
        {
            if (scene.IsBeingProcessed || scene.IsLoaded) return;

            scene.SetAsProcessed(true);
            var sceneInstance = await scene.sceneReference
                .LoadSceneAsync(loadMode, true)
                .ToUniTask(cancellationToken: token);

            if (setActive) sceneInstance.SetAsActiveScene();

            scene.SetAsProcessed(false);
        }
    }
}