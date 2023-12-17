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

        [SerializeField] private BoolReference loadGameplayScene = new(true);
        [SerializeField] private SceneSO gameplayScene;

        [Header("Listening to")]
        [SerializeField] private SceneEventChannelSO unloadSceneEvent;

        [SerializeField] private SceneEventChannelSO loadEnvironmentEvent;
        [SerializeField] private SceneEventChannelSO loadMenuEvent;
        [SerializeField] private SceneEventChannelSO loadCameraEvent;
        [SerializeField] private SceneEventChannelSO loadPlayerSceneEvent;
        [SerializeField] private SceneEventChannelSO loadGenericSceneEvent;

        [Header("Broadcasting on")]
        [Tooltip("Event raised when SceneLoader is initilialized and ready to load scenes.")]
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
            unloadSceneEvent.onEventRaised += UnLoadScene;
            loadEnvironmentEvent.onEventRaised += LoadEnvironment;
            loadMenuEvent.onEventRaised += LoadMenu;
            loadCameraEvent.onEventRaised += LoadCamera;
            loadPlayerSceneEvent.onEventRaised += LoadPlayerScene;
            loadGenericSceneEvent.onEventRaised += LoadGenericScene;

            RaiseOnLoaderReady();
        }

        private void OnDisable()
        {
            unloadSceneEvent.onEventRaised -= UnLoadScene;
            loadEnvironmentEvent.onEventRaised -= LoadEnvironment;
            loadMenuEvent.onEventRaised -= LoadMenu;
            loadPlayerSceneEvent.onEventRaised -= LoadPlayerScene;
            loadGenericSceneEvent.onEventRaised -= LoadGenericScene;
        }

        private async UniTask<SceneInstance> LoadSceneAsync(
            SceneSO scene,
            bool showTransition,
            bool showLoading,
            CancellationToken cancellationToken = default
        )
        {
            lock (scene)
            {
                if (scene.IsBeingProcessed) return default;
                scene.IsBeingProcessed = true;
            }

            if (IsSceneLoaded(scene))
            {
                Debug.LogWarning($"Scene {scene.sceneName} is already loaded. Skipping...");
                return default;
            }

            isLoadingScene.Value = true;
            if (showTransition) RaiseToggleTransitionEvent(true);
            if (showLoading) RaiseToggleLoadingIndicator(true);

            var sceneInstance = await scene.sceneReference.LoadSceneAsync(LoadSceneMode.Additive, true)
                .ToUniTask(cancellationToken: cancellationToken);

            lock (scene) scene.IsBeingProcessed = false;
            scene.IsLoaded = true;
            scene.sceneInstance = sceneInstance;

            if (showTransition) RaiseToggleTransitionEvent(false);
            if (showLoading) RaiseToggleLoadingIndicator(false);

            isLoadingScene.Value = false;
            RaiseSceneReadyEvent(scene);
            return sceneInstance;
        }

        private async UniTask UnLoadSceneAsync(SceneSO scene)
        {
            lock (scene)
            {
                if (scene.IsBeingProcessed) return;
                scene.IsBeingProcessed = true;
            }

            if (!IsSceneLoaded(scene)) return;

            isLoadingScene.Value = true;
            await scene.sceneReference.UnLoadScene().ToUniTask();

            lock (scene) scene.IsBeingProcessed = false;
            scene.IsLoaded = false;
            scene.sceneInstance = null;
            isLoadingScene.Value = false;
            RaiseSceneUnloadedEvent(scene);
        }
    }
}