#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Warborn.Builder
{
    /// <summary>
    /// THE RAISING BEFORE THE BUILD. Called by the forge runner, in order,
    /// before BuildPipeline.BuildPlayer — never as a build callback.
    /// A callback that throws kills the build; this one only reports, so the
    /// compiler's own words always reach the house.
    /// </summary>
    public static class WarbornCloudBuild
    {
        public const string ScenePath = "Assets/Scenes/SunderedFord.unity";

        public static bool RaiseTheFord()
        {
            if (!File.Exists(ScenePath))
            {
                try
                {
                    FordBuilder.BuildHeadless();
                }
                catch (System.Exception err)
                {
                    Debug.LogError("[Warborn] The Ford would not rise: " + err.Message);
                }
                AssetDatabase.Refresh();
            }

            if (!File.Exists(ScenePath))
            {
                Debug.LogError("[Warborn] No Sundered Ford stands after the raising.");
                return false;
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Warborn] The Sundered Ford is raised and registered.");
            return true;
        }
    }
}
#endif
