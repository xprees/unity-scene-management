using System.Threading;
using Cysharp.Threading.Tasks;

namespace Xprees.SceneManagement.Initialization.InitializationHandlers
{
    /// This interface defines the contract for initialization handlers.
    /// Their lifecycle is managed by InitializationLoader.
    /// First InitializeHandlerAsync is called on all handlers.
    /// Second TriggerInitializationAsync is called on all active handlers.
    /// Finally on scene unload UnloadHandlerAsync is called on all handlers.
    public interface IInitializationHandler
    {
        /// Is this initialization handler active? If not, it will be skipped.
        bool IsActive { get; set; }

        /// This method is called by InitializationLoader before InitAsync is called.
        /// Put here all checks, etc. Use this to set IsActive to false if this handler should be skipped.
        UniTask InitializeHandlerAsync(CancellationToken cancellationToken = default);

        /// This method is called by InitializationLoader
        /// when it's time to initialize this handler and is this handler active.
        UniTask TriggerInitializationAsync(CancellationToken cancellationToken = default);

        /// This method is called by InitializationLoader when unloading this handler.
        UniTask UnloadHandlerAsync(CancellationToken cancellationToken = default);
    }
}