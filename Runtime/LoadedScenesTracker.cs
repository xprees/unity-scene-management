using System.Collections.Generic;
using UnityEngine;
using Xprees.EditorTools.Attributes.ReadOnly;
using Xprees.SceneManagement.Events.ScriptableObjects;
using Xprees.SceneManagement.ScriptableObjects;

namespace Xprees.SceneManagement
{
    /// Will show all loaded scenes except the PersistentManagers scene in which this script live on was loaded first.
    public class LoadedScenesTracker : MonoBehaviour
    {
        [ReadOnly]
        [SerializeField] private List<SceneSO> loadedScenes = new();

        [Header("Listening to")]
        public SceneEventChannelSO sceneLoadedEvent;

        public SceneEventChannelSO sceneUnloadedEvent;

        public IReadOnlyCollection<SceneSO> LoadedScenes => loadedScenes;

        private void OnEnable()
        {
            if (sceneLoadedEvent) sceneLoadedEvent.onEventRaised += OnSceneLoaded;
            if (sceneUnloadedEvent) sceneUnloadedEvent.onEventRaised += OnSceneUnloaded;
        }

        private void OnDisable()
        {
            if (sceneLoadedEvent) sceneLoadedEvent.onEventRaised -= OnSceneLoaded;
            if (sceneUnloadedEvent) sceneUnloadedEvent.onEventRaised -= OnSceneUnloaded;
        }

        private void OnSceneLoaded(SceneSO scene, bool _, bool __)
        {
            if (loadedScenes.Contains(scene)) return;
            loadedScenes.Add(scene);
        }

        private void OnSceneUnloaded(SceneSO scene, bool _, bool __) => loadedScenes.Remove(scene);
    }
}