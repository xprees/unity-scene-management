using UnityEngine;
using Xprees.SceneManagement.Events.ScriptableObjects;
using Xprees.SceneManagement.ScriptableObjects;
using Xprees.Variables.Reference.Primitive;

namespace Xprees.SceneManagement.Utils
{
    public class OnStartScenesLoader : MonoBehaviour
    {
        [Header("Active/Disable")]
        [Tooltip("WARNING: This will completely disable this loader when set to True.")]
        [SerializeField] private BoolReference disableLoader = new(false);

        [Header("Scenes to load")]
        [SerializeField] private SceneSO[] scenes;

        [Header("Broadcasting on")]
        [SerializeField] private SceneEventChannelSO loadSceneEvent;

        [Header("Settings")]
        public bool showTransition;

        public bool showLoading;

        private void Start()
        {
            if (disableLoader) return;

            foreach (var scene in scenes) LoadSceneIfNotAlreadyLoaded(scene);
        }

        private void LoadSceneIfNotAlreadyLoaded(SceneSO scene)
        {
            bool shouldNotLoadScene;
            lock (scene) shouldNotLoadScene = scene.IsBeingProcessed || scene.IsLoaded;

            if (shouldNotLoadScene) return;

            RaiseLoadSceneEvent(scene);
        }

        private void RaiseLoadSceneEvent(SceneSO scene)
        {
            if (loadSceneEvent == null) return;
            loadSceneEvent.RaiseEvent(scene, showTransition, showLoading);
        }
    }
}