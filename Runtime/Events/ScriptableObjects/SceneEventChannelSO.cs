using UnityEngine;
using Xprees.Events.ScriptableObjects.Base;
using Xprees.SceneManagement.ScriptableObjects;

namespace Xprees.SceneManagement.Events.ScriptableObjects
{
    [CreateAssetMenu(menuName = "Events/Scene Event", fileName = "SceneEvent")]
    public class SceneEventChannelSO : EventChannelBaseSO<SceneSO, bool, bool>
    {
        // this method is hiding the base method, but it's not overriding it -> for better readability of parameters
        public new void RaiseEvent(SceneSO scene, bool showTransitionScreen, bool showLoading) =>
            base.RaiseEvent(scene, showTransitionScreen, showLoading);
    }
}