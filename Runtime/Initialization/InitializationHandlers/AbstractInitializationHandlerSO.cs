using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Xprees.Core;

namespace Xprees.SceneManagement.Initialization.InitializationHandlers
{
    /// <warning>: All references in this script must be Addressables to avoid duplication in build,
    /// therefore ScriptableObjects won't be duplicated in player build and in addressables build and shared logic will work</warning>
    public abstract class AbstractInitializationHandlerSO : DescriptionBaseSO, IInitializationHandler
    {
        [Header("Activation")]
        [Tooltip("Is this initialization handler active? If not, it will be skipped.")]
        [SerializeField] private bool isActive = true;

        // used to restore IsActive state in OnDisable if it was changed in OnEnable (Editor doesn't reset this value)
        private bool _isActiveOnEnable;

        /// Is this initialization handler active? If not, <see cref="TriggerInitializationAsync"/> will be skipped.
        public virtual bool IsActive
        {
            get => isActive;
            set => isActive = value;
        }

        /// Always call base.OnEnable() in derived classes
        protected virtual void OnEnable() => _isActiveOnEnable = IsActive;

        /// Always call base.OnDisable() in derived classes
        protected virtual void OnDisable() => IsActive = _isActiveOnEnable;

        /// This method is called by InitializationLoader before InitAsync is called for all handlers.
        /// Put here all checks, etc. Use this to set IsActive to false if this handler should be skipped.
        public virtual UniTask InitializeHandlerAsync(CancellationToken cancellationToken = default) => UniTask.CompletedTask;

        /// This method is called by InitializationLoader only for Active handlers.
        /// when it's time to initialize this handler and is this handler active.
        public abstract UniTask TriggerInitializationAsync(CancellationToken cancellationToken = default);

        /// This method is called by InitializationLoader on all handlers when unloading them.
        public abstract UniTask UnloadHandlerAsync(CancellationToken cancellationToken = default);
    }
}