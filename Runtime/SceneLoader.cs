using System.Linq;
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
    public class SceneLoader : MonoBehaviour
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
        [Tooltip("Scene used while loading environment scene.")]
        [SerializeField] private SceneSO elevatorScene;

        [SerializeField] private SceneSO gameplayScene;

        [Header("Listening to")]
        [SerializeField] private SceneEventChannelSO unloadSceneEvent;

        [SerializeField] private SceneEventChannelSO loadEnvironmentEvent;
        [SerializeField] private SceneEventChannelSO loadMenuEvent;
        [SerializeField] private SceneEventChannelSO loadCameraEvent;
        [SerializeField] private SceneEventChannelSO loadPlayerSceneEvent;
        [SerializeField] private SceneEventChannelSO loadGenericSceneEvent;

        [Header("Broadcasting on")]
        [SerializeField] private VoidEventChannelSO onSceneReady;

        [SerializeField] private SceneEventChannelSO sceneLoadedEvent;
        [SerializeField] private SceneEventChannelSO sceneUnloadedEvent;

        [Tooltip("Broadcasted when environment scene is loaded.")]
        [SerializeField] private VoidEventChannelSO onEnvironmentReady; // used by elevator to signal arrived


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
        }

        private void OnDisable()
        {
            unloadSceneEvent.onEventRaised -= UnLoadScene;
            loadEnvironmentEvent.onEventRaised -= LoadEnvironment;
            loadMenuEvent.onEventRaised -= LoadMenu;
            loadPlayerSceneEvent.onEventRaised -= LoadPlayerScene;
            loadGenericSceneEvent.onEventRaised -= LoadGenericScene;
        }

        #region Scene Loading

        private async UniTask<SceneInstance> LoadSceneAsync(SceneSO scene, bool showTransition, bool showLoading)
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

            var sceneInstance = await scene.sceneReference.LoadSceneAsync(LoadSceneMode.Additive, true);

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

        private void RaiseSceneUnloadedEvent(SceneSO scene)
        {
            if (sceneUnloadedEvent != null) sceneUnloadedEvent.RaiseEvent(scene, default, default);
        }

        private bool IsSceneLoaded(SceneSO scene)
        {
            if (scenesTracker == null || scenesTracker.LoadedScenes == null) return false;
            return scenesTracker.LoadedScenes.Contains(scene);
        }

        private bool IsSceneBeingProcessed(SceneSO scene)
        {
            lock (scene) return scene.IsBeingProcessed;
        }

        private void RaiseToggleTransitionEvent(bool start)
        {
            if (toggleSceneTransition == null) return;
            toggleSceneTransition.RaiseEvent(start);
        }

        private void RaiseToggleLoadingIndicator(bool value)
        {
            if (toggleLoadingIndicator == null) return;
            toggleLoadingIndicator.RaiseEvent(value);
        }

        private void RaiseSceneReadyEvent(SceneSO scene)
        {
            if (onSceneReady != null) onSceneReady.RaiseEvent();
            if (sceneLoadedEvent != null) sceneLoadedEvent.RaiseEvent(scene, default, default);
        }

        #endregion

        #region Event Handlers

        private async void UnLoadScene(SceneSO scene, bool _, bool __) => await UnLoadSceneAsync(scene);

        private async void LoadEnvironment(SceneSO scene, bool showTransition, bool showLoading)
        {
            if (IsSceneLoaded(scene) || IsSceneBeingProcessed(scene)) return;

            // false to not show transition or loading -> we will trigger it by ourselves
            if (showTransition)
            {
                RaiseToggleTransitionEvent(true);
                await UniTask.Delay(defaultRenderDelayMillisecond); // give it some time to render
            }

            if (showLoading) RaiseToggleLoadingIndicator(true);

            // Try to load gameplay scene -> if it's already loaded, it will be skipped
            var loadGameplayTask = LoadSceneAsync(gameplayScene, false, false);

            var loadElevatorTask = default(UniTask<SceneInstance>);
            if (showTransition)
            {
                DisableAllInput();
                loadElevatorTask = LoadSceneAsync(elevatorScene, false, false);
            }

            var loadEnvironmentTask = LoadSceneAsync(scene, false, false);

            await loadGameplayTask;
            await loadElevatorTask;

            if (showTransition)
            {
                RaiseToggleTransitionEvent(false);
                await UniTask.Delay(defaultRenderDelayMillisecond);
            }

            EnableGameplayInput(); // should be now safe to enable gameplay input as gameplay scene and elevator is loaded

            var environment = await loadEnvironmentTask;
            SetAsActiveScene(environment);

            if (showLoading) RaiseToggleLoadingIndicator(false);
            RaiseEnvironmentReadyEvent();
        }

        private static void SetAsActiveScene(SceneInstance environment)
        {
            if (!environment.Scene.IsValid()) return;
            SceneManager.SetActiveScene(environment.Scene);
        }

        private void RaiseEnvironmentReadyEvent()
        {
            if (onEnvironmentReady == null) return;
            onEnvironmentReady.RaiseEvent();
        }

        private async void LoadMenu(SceneSO scene, bool _, bool __)
        {
            DisableAllInput();

            RaiseToggleTransitionEvent(true);
            RaiseToggleLoadingIndicator(true);
            await UniTask.Delay(defaultRenderDelayMillisecond);

            var unloadGameplayScenesTask = UnloadGamePlayScenes();
            var loadMenuTask = LoadSceneAsync(scene, true, true);

            await unloadGameplayScenesTask;
            var menu = await loadMenuTask;
            SetAsActiveScene(menu);

            RaiseToggleLoadingIndicator(false);
            RaiseToggleTransitionEvent(false);
            await UniTask.Delay(defaultRenderDelayMillisecond);

            EnableUiInput();
        }

        private UniTask UnloadGamePlayScenes()
        {
            if (scenesTracker == null || scenesTracker.LoadedScenes == null) return default;

            var loadedScenes = scenesTracker.LoadedScenes;
            var loadedEnvironmentScenes =
                loadedScenes.Where(s => s.sceneType is SceneType.Environment);
            var levelLoadingScenes =
                loadedScenes.Where(s => s.sceneType is SceneType.LevelLoadingScene);
            var gameplayScenes =
                loadedScenes.Where(s => s.sceneType is SceneType.Gameplay);
            var playerScenes =
                loadedScenes.Where(s => s.sceneType is SceneType.Player);

            var unloadScenes = loadedEnvironmentScenes
                .Concat(gameplayScenes)
                .Concat(levelLoadingScenes)
                .Concat(playerScenes);

            return UniTask.WhenAll(unloadScenes.Select(UnLoadSceneAsync));
        }

        private async void LoadCamera(SceneSO scene, bool _, bool __) =>
            await LoadSceneAsync(scene, false, false);

        private async void LoadPlayerScene(SceneSO scene, bool _, bool __) =>
            await LoadSceneAsync(scene, false, false);

        private async void LoadGenericScene(SceneSO scene, bool showTransition, bool showLoading) =>
            await LoadSceneAsync(scene, showTransition, showLoading);

        #endregion

        #region Input handlers

        private void DisableAllInput()
        {
            if (disableAllInputEvent == null) return;
            disableAllInputEvent.RaiseEvent();
        }

        private void EnableGameplayInput()
        {
            if (enableGameplayInputEvent == null) return;
            enableGameplayInputEvent.RaiseEvent();
        }

        private void EnableUiInput()
        {
            if (enableUiInputEvent == null) return;
            enableUiInputEvent.RaiseEvent();
        }

        #endregion

    }
}