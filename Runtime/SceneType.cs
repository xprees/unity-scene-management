namespace Xprees.SceneManagement
{
    public enum SceneType
    {
        // UI scenes
        UI,
        Menu,

        // GamePlay scenes
        Environment,
        Gameplay,
        Player,
        Camera,

        LevelLoadingScene, // elevator scene

        // Game backed scenes
        PersistentManagers,
        Initialization,

        // other scenes that don't need to be played
        Testing,
    }
}