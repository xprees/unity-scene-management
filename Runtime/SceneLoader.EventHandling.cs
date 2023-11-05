using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using Xprees.SceneManagement.ScriptableObjects;

namespace Xprees.SceneManagement
{
    public partial class SceneLoader
    {
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
            if (showTransition) loadElevatorTask = LoadSceneAsync(elevatorScene, false, false);

            var loadEnvironmentTask = LoadSceneAsync(scene, false, false);

            await UniTask.WhenAll(loadGameplayTask, loadElevatorTask);

            if (showTransition)
            {
                RaiseToggleTransitionEvent(false);
                await UniTask.Delay(defaultRenderDelayMillisecond);
            }

            var environment = await loadEnvironmentTask;
            SetAsActiveScene(environment);

            if (showLoading) RaiseToggleLoadingIndicator(false);
        }

        private static void SetAsActiveScene(SceneInstance scene)
        {
            if (!scene.Scene.IsValid()) return;
            SceneManager.SetActiveScene(scene.Scene);
        }

        private async void LoadMenu(SceneSO scene, bool _, bool __)
        {
            DisableAllInput();

            RaiseToggleTransitionEvent(true);
            RaiseToggleLoadingIndicator(true);
            await UniTask.Delay(defaultRenderDelayMillisecond);

            var unloadGameplayScenesTask = UnloadGameplayScenes();
            var loadMenuTask = LoadSceneAsync(scene, true, true);

            await unloadGameplayScenesTask;
            var menu = await loadMenuTask;
            SetAsActiveScene(menu);

            RaiseToggleLoadingIndicator(false);
            RaiseToggleTransitionEvent(false);
            await UniTask.Delay(defaultRenderDelayMillisecond);

            EnableUiInput();
        }

        private UniTask UnloadGameplayScenes()
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
                .Concat(playerScenes)
                .Where(scene => scene != null);

            return UniTask.WhenAll(unloadScenes.Select(UnLoadSceneAsync));
        }

        private async void LoadCamera(SceneSO scene, bool _, bool __) =>
            await LoadSceneAsync(scene, false, false);

        private async void LoadPlayerScene(SceneSO scene, bool _, bool __) =>
            await LoadSceneAsync(scene, false, false);

        private async void LoadGenericScene(SceneSO scene, bool showTransition, bool showLoading) =>
            await LoadSceneAsync(scene, showTransition, showLoading);

        private void RaiseSceneUnloadedEvent(SceneSO scene)
        {
            if (sceneUnloadedEvent == null) return;
            sceneUnloadedEvent.RaiseEvent(scene, default, default);
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
    }
}