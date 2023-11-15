using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Xprees.Core;

namespace Xprees.SceneManagement.Initialization
{
    /// <warning>: All references in this script must be Addressables to avoid duplication in build,
    /// therefore ScriptableObjects won't be duplicated in player build and in addressables build and shared logic will work</warning>
    public abstract class AbstractInitializationHandlerSO : DescriptionBaseSO, IInitializationHandler
    {
        [Header("Activation")]
        [Tooltip("Is this initialization handler active? If not, it will be skipped.")]
        [SerializeField] private bool isActive = true;

        /// Is this initialization handler active? If not, it will be skipped.
        public virtual bool IsActive
        {
            get => isActive;
            set => isActive = value;
        }

        /// This method is called by InitializationLoader before InitAsync is called.
        /// Put here all checks, etc. Use this to set IsActive to false if this handler should be skipped.
        public virtual UniTask InitializeHandlerAsync(CancellationToken cancellationToken = default) => UniTask.CompletedTask;

        /// This method is called by InitializationLoader
        /// when it's time to initialize this handler and is this handler active.
        public abstract UniTask TriggerInitializationAsync(CancellationToken cancellationToken = default);


        /// This method is called by InitializationLoader when unloading this handler.
        public abstract UniTask UnloadHandlerAsync(CancellationToken cancellationToken = default);
    }
}