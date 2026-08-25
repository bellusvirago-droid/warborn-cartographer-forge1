#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// RAISE THE BODY — the one door the cloud runner knocks on before it compiles.
///
/// THE FAULT THIS CLOSES. SunderedFord.unity is committed to the tree as an
/// empty shell: the whole field is drawn at build time by FordBuilder, and
/// FordRaise is the hand that draws it. But the WebGL workflow was compiling
/// the tree WITHOUT ever calling that hand — and the scene was not in the
/// build settings either. The runner would have produced a real player
/// containing an empty world. This method raises the ford, saves it, and puts
/// it first in the build list, so what compiles is what was drawn.
///
/// Called headlessly:
///   Unity -batchmode -quit -executeMethod RaiseTheBody.ForTheBrowser
///
/// THE FOUNDER NEVER OPENS UNITY. Everything below is done by script.
/// </summary>
public static class RaiseTheBody
{
    private const string ScenePath = "Assets/Scenes/SunderedFord.unity";

    [MenuItem("Warborn/Raise the body for the browser")]
    public static void ForTheBrowser()
    {
        Debug.Log("[RaiseTheBody] Drawing the Sundered Ford before the compiler sees it.");

        // 1. Draw the field. FordRaise saves the scene to ScenePath itself.
        FordRaise.Raise();

        // 2. Prove something actually stands in it. An empty scene compiled
        //    into a player is the worst kind of green build: it passes, and it
        //    ships nothing.
        var scene = EditorSceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        Debug.Log($"[RaiseTheBody] The scene '{scene.name}' holds {roots.Length} root objects.");

        if (roots.Length < 4)
        {
            Fail($"The Sundered Ford raised only {roots.Length} root objects. " +
                 "The field did not draw. Refusing to compile an empty world.");
            return;
        }

        string[] mustStand = { "StillAir", "StrikeReckoner", "MusterBoard" };
        var standing = roots.Select(r => r.name).ToHashSet();
        var missing = mustStand.Where(n => !standing.Contains(n)).ToList();
        if (missing.Count > 0)
        {
            Fail("The field is missing systems the slice cannot be played without: " +
                 string.Join(", ", missing));
            return;
        }

        // 3. Save it where the compiler will find it.
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 4. Put the ford first in the build list, and nothing else in it.
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        Debug.Log($"[RaiseTheBody] Build settings now carry one scene: {ScenePath}");

        // 5. The browser's own settings. The March must run in a page without
        //    asking the visitor to install anything, and it must not shout.
        PlayerSettings.companyName = "Warborn Blades";
        PlayerSettings.productName = "The Warborn March";
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        PlayerSettings.runInBackground = false;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
        PlayerSettings.stripEngineCode = true;

        AssetDatabase.SaveAssets();
        Debug.Log("[RaiseTheBody] The body is drawn, saved, and listed. The compiler may proceed.");
    }

    private static void Fail(string why)
    {
        Debug.LogError("[RaiseTheBody] " + why);
        if (Application.isBatchMode) EditorApplication.Exit(1);
    }
}
#endif
