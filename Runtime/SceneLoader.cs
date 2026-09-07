using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using Xprees.Events.ScriptableObjects.Base;
using Xprees.Events.ScriptableObjects.Primitive;
using Xprees.SceneManagement.Events.ScriptableObjects;
using Xprees.SceneManagement.ScriptableObjects;
using Xprees.Variables.Reference.Primitive;

namespace Xprees.SceneManagement
{
    public partial class SceneLoader : MonoBehaviour
    {
        [Header("Input handling")]
        [Tooltip("Used to disable input while loading.")]
        [SerializeField] private VoidEventChannelSO disableAllInputEvent;

        [Tooltip("Used to enable UI input after loading into menu scenes.")]
        [SerializeField] private VoidEventChannelSO enableUiInputEvent;

        [Tooltip("Used to enable gameplay input after loading into gameplay scenes.")]
        [SerializeField] private VoidEventChannelSO enableGameplayInputEvent;

        [Header("Scene Management")]
        [SerializeField] private LoadedScenesTracker scenesTracker;

        [SerializeField] private BoolReference isLoadingScene;

        [Header("Utility Scene references")]
        [SerializeField] private BoolReference useElevatorScene = new(true);

        [Tooltip("Scene used while loading environment scene.")]
        [SerializeField] private SceneSO elevatorScene;

        [Space]
        [SerializeField] private BoolReference loadVRGameplayScene = new(true);

        [SerializeField] private SceneSO gameplayScene;

        // ReSharper disable once InconsistentNaming
        [Tooltip("Optional")]
        [SerializeField] private SceneSO VRGameplayScene;

        [Header("Listening to")]
        [SerializeField] private SceneEventChannelSO unloadSceneEvent;

        [SerializeField] private SceneEventChannelSO loadEnvironmentEvent;
        [SerializeField] private SceneEventChannelSO loadMenuEvent;
        [SerializeField] private SceneEventChannelSO loadCameraEvent;
        [SerializeField] private SceneEventChannelSO loadPlayerSceneEvent;
        [SerializeField] private SceneEventChannelSO loadGenericSceneEvent;

        [Header("Broadcasting on")]
        [Tooltip("Event raised when SceneLoader is initialized and ready to load scenes.")]
        [SerializeField] private VoidEventChannelSO onLoaderReady;

        [Space]
        [SerializeField] private VoidEventChannelSO onSceneReady;

        [SerializeField] private SceneEventChannelSO sceneLoadedEvent;
        [SerializeField] private SceneEventChannelSO sceneUnloadedEvent;

        [Space]
        [SerializeField] private BoolEventChannelSO toggleSceneTransition;

        [SerializeField] private BoolEventChannelSO toggleLoadingIndicator;

        private const int defaultRenderDelayMillisecond = 750; // used for delay in transition and loading indicator

        private void OnEnable()
        {
            if (unloadSceneEvent) unloadSceneEvent.onEventRaised += UnLoadScene;
            if (loadEnvironmentEvent) loadEnvironmentEvent.onEventRaised += LoadEnvironment;
            if (loadMenuEvent) loadMenuEvent.onEventRaised += LoadMenu;
            if (loadCameraEvent) loadCameraEvent.onEventRaised += LoadCamera;
            if (loadPlayerSceneEvent) loadPlayerSceneEvent.onEventRaised += LoadPlayerScene;
            if (loadGenericSceneEvent) loadGenericSceneEvent.onEventRaised += LoadGenericScene;

            RaiseOnLoaderReady(); // Important during initialization
        }

        private void OnDisable()
        {
            if (unloadSceneEvent) unloadSceneEvent.onEventRaised -= UnLoadScene;
            if (loadEnvironmentEvent) loadEnvironmentEvent.onEventRaised -= LoadEnvironment;
            if (loadMenuEvent) loadMenuEvent.onEventRaised -= LoadMenu;
            if (loadCameraEvent) loadCameraEvent.onEventRaised -= LoadCamera;
            if (loadPlayerSceneEvent) loadPlayerSceneEvent.onEventRaised -= LoadPlayerScene;
            if (loadGenericSceneEvent) loadGenericSceneEvent.onEventRaised -= LoadGenericScene;
        }

        private async UniTask<SceneInstance> LoadSceneAsync(
            SceneSO scene,
            bool showTransition,
            bool showLoading,
            CancellationToken cancellationToken = default
        )
        {
            if (!scene) return default;

            if (scene.IsBeingProcessed)
            {
                // The scene itself has to be the awaited state. Passing scene.IsBeingProcessed binds
                // WaitUntil<bool>, which captures the value once - it is true here by definition, so
                // the predicate would never be satisfied and the load would hang forever.
                await UniTask.WaitUntil(scene, static s => !s.IsBeingProcessed, cancellationToken: cancellationToken);
            }

            lock (scene)
            {
                if (scene.IsBeingProcessed) return default;
                scene.IsBeingProcessed = true;
            }

            try
            {
                if (IsSceneLoaded(scene))
                {
                    Debug.LogWarning($"Scene {scene.sceneName} is already loaded. Skipping...");
                    return scene.sceneInstance ?? default;
                }

                isLoadingScene.Value = true;
                if (showTransition) RaiseToggleTransitionEvent(true);
                if (showLoading) RaiseToggleLoadingIndicator(true);

                var sceneInstance = await scene.sceneReference.LoadSceneAsync(LoadSceneMode.Additive, true)
                    .ToUniTask(cancellationToken: cancellationToken);

                scene.IsLoaded = true;
                scene.sceneInstance = sceneInstance;

                RaiseSceneReadyEvent(scene);
                return sceneInstance;
            }
            finally
            {
                if (showTransition) RaiseToggleTransitionEvent(false);
                if (showLoading) RaiseToggleLoadingIndicator(false);

                isLoadingScene.Value = false;
                lock (scene) scene.IsBeingProcessed = false;
            }
        }

        private async UniTask UnLoadSceneAsync(SceneSO scene)
        {
            if (!scene) return;

            if (scene.IsBeingProcessed)
            {
                await UniTask.WaitUntil(scene, static s => !s.IsBeingProcessed, cancellationToken: destroyCancellationToken);
            }

            lock (scene)
            {
                if (scene.IsBeingProcessed) return;
                scene.IsBeingProcessed = true;
            }

            try
            {
                if (!IsSceneLoaded(scene)) return;

                isLoadingScene.Value = true;
                await scene.sceneReference.UnLoadScene().ToUniTask(cancellationToken: destroyCancellationToken);

                scene.IsLoaded = false;
                scene.sceneInstance = null;
                RaiseSceneUnloadedEvent(scene);
            }
            finally
            {
                isLoadingScene.Value = false;
                lock (scene) scene.IsBeingProcessed = false;
            }
        }
    }
}