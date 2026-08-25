#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Warborn.Builder
{
    /// <summary>
    /// THE RAISING. The Founder never opens a menu she was not told about, and
    /// never drags a thing into an Inspector. When this project is opened and no
    /// Sundered Ford stands, the Ford is raised, saved, and opened for her.
    /// </summary>
    [InitializeOnLoad]
    public static class FordRaise
    {
        private const string SCENE_PATH = "Assets/Scenes/SunderedFord.unity";
        private const string RAISED_KEY = "Warborn.FordRaised";

        static FordRaise()
        {
            if (Application.isBatchMode) return; // the forge raises the Ford in order, not by callback
            EditorApplication.delayCall += Raise;
        }

        [MenuItem("Warborn/Raise the Sundered Ford")]
        public static void RaiseByHand()
        {
            SessionState.SetBool(RAISED_KEY, false);
            Raise();
        }

        private static void Raise()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                if (Application.isBatchMode) return; // the forge raises the Ford in order, not by callback
            EditorApplication.delayCall += Raise;
                return;
            }
            if (SessionState.GetBool(RAISED_KEY, false)) return;
            SessionState.SetBool(RAISED_KEY, true);

            if (!File.Exists(SCENE_PATH))
            {
                Debug.Log("[Warborn] No Sundered Ford stands. Raising it.");
                FordBuilder.BuildHeadless();
                AssetDatabase.Refresh();
            }

            if (!File.Exists(SCENE_PATH))
            {
                Debug.LogWarning("[Warborn] The Ford could not be raised. Use Warborn > Forge > Build Sundered Ford and read the console.");
                return;
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(SCENE_PATH, true) };
            var open = EditorSceneManager.GetActiveScene();
            if (open.path != SCENE_PATH && !open.isDirty)
            {
                EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            }
            Debug.Log("[Warborn] The Sundered Ford stands. Press Play.");
        }
    }
}
#endif
