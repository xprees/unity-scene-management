﻿using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
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

        [Header("Additional Initialization Handlers")]
        [SerializeField] private List<AbstractInitializationHandlerSO> initializationHandlers;

        private IEnumerable<AbstractInitializationHandlerSO> ActiveHandlers => initializationHandlers.Where(handler => handler.IsActive);

        private async void Awake()
        {
            // Managers scene must be loaded first and in Awake
            // to have all PersistentManagers ready for calls from the other scripts on Start
            await LoadManagersScene();
            CheckActiveHandlers();
        }

        private async void Start()
        {
            await InitializeHandlers();

            await TriggerInitHandlers();

            await UnloadHandlers();

            await UnloadInitializationScene();
        }

        private async UniTask InitializeHandlers()
        {
            var cancellationToken = this.GetCancellationTokenOnDestroy();

            var initTasks = initializationHandlers
                .Select(handler => handler.InitializeHandlerAsync(cancellationToken))
                .ToList();

            await UniTask.WhenAll(initTasks).SuppressCancellationThrow();
        }

        private async UniTask TriggerInitHandlers()
        {
            var cancellationToken = this.GetCancellationTokenOnDestroy();

            var triggerTasks = ActiveHandlers
                .Select(handler => handler.TriggerInitializationAsync(cancellationToken))
                .ToList();

            await UniTask.WhenAll(triggerTasks).SuppressCancellationThrow();
        }

        private async UniTask UnloadHandlers()
        {
            var cancellationToken = this.GetCancellationTokenOnDestroy();

            var unloadTasks = initializationHandlers
                .Select(handler => handler.UnloadHandlerAsync(cancellationToken))
                .ToList();

            await UniTask.WhenAll(unloadTasks).SuppressCancellationThrow();
        }

        private async UniTask LoadManagersScene()
        {
            var managersScene = await managersSceneDataReference.LoadAssetAsync<SceneSO>();
            await managersScene.sceneReference.LoadSceneAsync(LoadSceneMode.Additive, true);
            await UniTask.WaitUntil(() => managersScene.sceneInstance.HasValue && managersScene.IsLoaded);
        }

        private async UniTask UnloadInitializationScene() => await SceneManager.UnloadSceneAsync(0).ToUniTask(); // only scene in build settings

        private void CheckActiveHandlers()
        {
            if (!ActiveHandlers.Any())
            {
                Debug.LogWarning($"{nameof(InitializationLoader)} has {initializationHandlers.Count} initialization handlers. " +
                                 $"Only {ActiveHandlers.Count()} are active.");
            }
        }
    }
}