using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityToolbarExtender;
using Xprees.SceneManagement.ScriptableObjects;

namespace Xprees.SceneManagement.Editor
{
    [InitializeOnLoad]
    public static class SceneDropdownEditor
    {
        private readonly static EditorSceneTracker sceneTracker;

        static SceneDropdownEditor()
        {
            sceneTracker = new EditorSceneTracker();
            ToolbarExtender.LeftToolbarGUI.Add(OnToolbarGUI);
        }

        #region GUI

        private static void OnToolbarGUI()
        {
            GUILayout.Space(5);

            // Create the dropdown button
            if (GUILayout.Button(GetTitle(), EditorStyles.toolbarDropDown))
            {
                var menu = new GenericMenu();

                foreach (var scene in sceneTracker.Scenes.OrderByDescending(scene => scene.sceneType))
                {
                    var isSceneOpenOrPresent = sceneTracker.IsSceneOpen(scene);
                    if (isSceneOpenOrPresent == null) continue;

                    menu.AddItem(new GUIContent($"{scene.sceneName} ({scene.sceneType})"), isSceneOpenOrPresent!.Value, MenuFunction);
                    continue;

                    void MenuFunction() => ProcessScene(scene);
                }

                menu.ShowAsContext();
            }

            GUILayout.FlexibleSpace();
        }

        private static GUIContent GetTitle() =>
            new("Scenes", EditorGUIUtility.IconContent("BuildSettings.Editor").image);

        #endregion

        private static void ProcessScene(SceneSO scene)
        {
            if (sceneTracker.IsSceneOpen(scene) ?? false)
            {
                EditorSceneLoader.CloseScene(scene);
                return;
            }

            EditorSceneLoader.OpenScene(scene);
        }
    }
}