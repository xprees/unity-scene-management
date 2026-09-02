using System;
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
        // otherwise asset copies will be included one in player build and one in addressables build
        [Header("Scene References")]
        [Tooltip("Asset reference to the persistent managers sceneData (SceneSO)")]
        [SerializeField] private AssetReferenceT<SceneSO> managersSceneDataReference;

        [Header("Listening to")]
        [SerializeField] private AssetReferenceT<VoidEventChannelSO> startHandlersInitializationEventRef;

        [Header("Additional Initialization Handlers")]
        [SerializeField] private List<AbstractInitializationHandlerSO> initializationHandlers;

        private IEnumerable<AbstractInitializationHandlerSO> ActiveHandlers =>
            initializationHandlers.Where(handler => handler && handler.IsActive);

        /// Event raised when the Managers scene is ready and the initialization process can start
        private VoidEventChannelSO _startHandlersInitializationEvent;

        /// Guard Flag to prevent double initialization
        private bool _hasStartedInitialization;

        private async void Awake()
        {
            try
            {
                // Persistent Managers scene must be loaded first and in Awake
                // to have all PersistentManagers ready for calls from the other scripts on Start
                await LoadManagersScene(destroyCancellationToken);

                // Fallback: If PersistentManagers was already loaded or event fired before subscription
                if (!_hasStartedInitialization)
                {
                    StartHandlersInitialization();
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when Initialization scene is unloaded
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during Managers scene loading: \n{e.Message}", this);
            }
        }

        private async UniTask LoadManagersScene(CancellationToken cancellationToken)
        {
            _startHandlersInitializationEvent = await startHandlersInitializationEventRef.LoadAssetAsync<VoidEventChannelSO>()
                .ToUniTask(cancellationToken: cancellationToken);

            // Subscribe to the event to start initialization when the Managers scene is ready and the event is raised
            _startHandlersInitializationEvent.onEventRaised += StartHandlersInitialization;

            var managersScene = await managersSceneDataReference.LoadAssetAsync<SceneSO>()
                .ToUniTask(cancellationToken: cancellationToken);

            // Set as active to make sure if there are any created objects they are in the right scene and not initialization scene
            await LoadManagersSceneIfNotAlreadyProcessed(managersScene, true, LoadSceneMode.Additive, cancellationToken);

            await UniTask.WaitUntil(managersScene, predicate: SceneSOExtensions.IsReady, cancellationToken: cancellationToken);
        }

        private void OnDisable()
        {
            if (_startHandlersInitializationEvent) _startHandlersInitializationEvent.onEventRaised -= StartHandlersInitialization;
        }

        #region Initialization Handlers

        private void CheckActiveHandlers()
        {
            var disabledHandlers = initializationHandlers.Where(h => !h || !h.IsActive).ToList();
            foreach (var disabledHandler in disabledHandlers)
            {
                var disabledHandlerName = disabledHandler ? disabledHandler.name : "Already destroyed handler";
                Debug.LogWarning($"Initialization handler {disabledHandlerName} is disabled and will be skipped.");
            }

            if (!ActiveHandlers.Any())
            {
                Debug.LogWarning($"{nameof(InitializationLoader)} has {initializationHandlers.Count} initialization handlers. " +
                                 $"Only {ActiveHandlers.Count()} are active.");
            }
        }

        // Starts the initialization process
        private async void StartHandlersInitialization()
        {
            try
            {
                // Guard against double initialization
                if (_hasStartedInitialization) return;
                _hasStartedInitialization = true;

                Debug.Log("Started init");
                // Check here to be sure it ran before initialization
                CheckActiveHandlers();

                // Each step suppresses cancellation internally, so probe the token between them instead.
                // Cached - destroyCancellationToken throws once this MonoBehaviour is destroyed
                var cancellationToken = destroyCancellationToken;

                await InitializeHandlers(cancellationToken);
                if (cancellationToken.IsCancellationRequested) return;

                await TriggerInitHandlers(cancellationToken);
                if (cancellationToken.IsCancellationRequested) return;

                await UnloadHandlers(cancellationToken);
                if (cancellationToken.IsCancellationRequested) return;

                UnloadInitializationScene().Forget(); // Will also unload this script we don't need to wait for that :-)
            }
            catch (OperationCanceledException)
            {
                // Expected when Initialization scene is unloaded
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during start handlers init: \n{e.Message}", this);
            }
        }

        private async UniTask InitializeHandlers(CancellationToken cancellationToken)
        {
            var initTasks = initializationHandlers
                .Where(handler => handler)
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
                .Where(h => h)
                .Select(handler => handler.UnloadHandlerAsync(cancellationToken))
                .ToList();

            await UniTask.WhenAll(unloadTasks).SuppressCancellationThrow();
        }

        #endregion

        private UniTask UnloadInitializationScene() => SceneManager.UnloadSceneAsync(0).ToUniTask(); // only scene in build settings

        private async UniTask LoadManagersSceneIfNotAlreadyProcessed(
            SceneSO scene,
            bool setActive = false,
            LoadSceneMode loadMode = LoadSceneMode.Additive,
            CancellationToken token = default
        )
        {
            if (scene.IsBeingProcessed || scene.IsLoaded) return;

            try
            {
                scene.SetAsProcessed(true);
                var sceneInstance = await scene.sceneReference
                    .LoadSceneAsync(loadMode, true)
                    .ToUniTask(cancellationToken: token);

                scene.sceneInstance = sceneInstance; // Usually this is done by SceneLoader, but we are loading the Managers scene manually
                scene.SetAsLoaded(true); // IsReady() checks both - without this the WaitUntil below never completes
                if (setActive) sceneInstance.SetAsActiveScene();
            }
            finally
            {
                scene.SetAsProcessed(false);
            }
        }
    }
}