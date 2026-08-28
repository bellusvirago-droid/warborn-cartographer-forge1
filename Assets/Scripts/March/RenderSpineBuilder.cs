using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
#endif

// ------------------------------------------------------------------------
// EDITOR-ONLY: Headless Pipeline & Rendering Builder
// Run headlessly: -executeMethod RenderSpineBuilder.Build
// ------------------------------------------------------------------------

#if UNITY_EDITOR
public static class RenderSpineBuilder
{
    /// <summary>
    /// Configures URP settings, global volumes, and quality settings for the vertical slice.
    /// Can be invoked headlessly via: -executeMethod RenderSpineBuilder.Build
    /// </summary>
    public static void Build()
    {
        Debug.Log("[RenderSpineBuilder] Beginning URP and Post-Processing configuration for the Sundered Ford...");

        // 1. Ensure target directory exists
        if (!AssetDatabase.IsValidFolder("Assets/Settings"))
        {
            AssetDatabase.CreateFolder("Assets", "Settings");
            Debug.Log("[RenderSpineBuilder] Created Assets/Settings directory.");
        }

        // 2. Setup URP Post-Processing Volume Profile (The Grade)
        string profilePath = "Assets/Settings/SunderedFord_VolumeProfile.asset";
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, profilePath);
            Debug.Log($"[RenderSpineBuilder] Generated new VolumeProfile at {profilePath}");
        }

        // Constant: Tone Mapping Exposure = 1.35
        AddOrUpdate<Tonemapping>(profile).mode.Override(TonemappingMode.ACES);
        AddOrUpdate<ColorAdjustments>(profile).postExposure.Override(1.35f);

        // Constant: Bloom (intensity controlled by StillAirEnforcer, threshold = 0.62, smoothing/scatter = 0.28)
        var bloom = AddOrUpdate<Bloom>(profile);
        bloom.intensity.Override(0.85f); 
        bloom.threshold.Override(0.62f);
        bloom.scatter.Override(0.28f);

        // Constant: Vignette (offset/smoothness = 0.42, darkness/intensity = 0.5)
        var vignette = AddOrUpdate<Vignette>(profile);
        vignette.intensity.Override(0.5f);
        vignette.smoothness.Override(0.42f);

        // Constant: Noise (opacity controlled by StillAirEnforcer)
        var grain = AddOrUpdate<FilmGrain>(profile);
        grain.type.Override(FilmGrainLookup.Thin1);
        grain.intensity.Override(0.045f);

        // Add base Depth of Field to anchor the camera focus mechanics later
        var dof = AddOrUpdate<DepthOfField>(profile);
        dof.mode.Override(DepthOfFieldMode.Gaussian);
        dof.gaussianStart.Override(12f);
        dof.gaussianEnd.Override(42f); // Max camera orbit distance

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        Debug.Log("[RenderSpineBuilder] Volume profile authored with exact Survey constants.");

        // 3. Locate baseline URP Asset to duplicate for WebGL / Desktop
        UniversalRenderPipelineAsset baselineUrp = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        if (baselineUrp == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset");
            if (guids.Length > 0)
            {
                baselineUrp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
        }

        if (baselineUrp != null)
        {
            string desktopPath = "Assets/Settings/URP_Desktop.asset";
            string webglPath = "Assets/Settings/URP_WebGL.asset";

            if (AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(desktopPath) == null)
                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(baselineUrp), desktopPath);
            
            if (AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(webglPath) == null)
                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(baselineUrp), webglPath);

            UniversalRenderPipelineAsset desktopAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(desktopPath);
            UniversalRenderPipelineAsset webglAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(webglPath);

            // Configure Desktop: 2x MSAA, 2048 shadow resolution
            SerializedObject desktopSO = new SerializedObject(desktopAsset);
            desktopSO.FindProperty("m_MSAA").intValue = (int)MsaaQuality._2x;
            desktopSO.FindProperty("m_MainLightShadowmapResolution").intValue = 2048;
            desktopSO.ApplyModifiedProperties();

            // Configure WebGL: No MSAA, 1024 shadow resolution
            SerializedObject webglSO = new SerializedObject(webglAsset);
            webglSO.FindProperty("m_MSAA").intValue = (int)MsaaQuality.Disabled;
            webglSO.FindProperty("m_MainLightShadowmapResolution").intValue = 1024;
            webglSO.ApplyModifiedProperties();

            // Inject into system QualitySettings
            SerializedObject qualitySO = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);
            SerializedProperty m_QualitySettings = qualitySO.FindProperty("m_QualitySettings");
            
            // Enforce WebGL bindings to Index 0 (lowest standard slot)
            if (m_QualitySettings.arraySize > 0)
            {
                m_QualitySettings.GetArrayElementAtIndex(0).FindPropertyRelative("renderPipeline").objectReferenceValue = webglAsset;
            }
            
            // Enforce Desktop bindings to Index 1 or highest standard slot
            int highTierIndex = m_QualitySettings.arraySize > 1 ? m_QualitySettings.arraySize - 1 : 0;
            if (m_QualitySettings.arraySize > 1)
            {
                m_QualitySettings.GetArrayElementAtIndex(highTierIndex).FindPropertyRelative("renderPipeline").objectReferenceValue = desktopAsset;
            }
            
            qualitySO.ApplyModifiedProperties();

            // Assign Desktop as fallback runtime default pipeline
            GraphicsSettings.defaultRenderPipeline = desktopAsset;
            Debug.Log("[RenderSpineBuilder] Successfully branched and assigned WebGL and Desktop pipeline assets.");
        }
        else
        {
            Debug.LogWarning("[RenderSpineBuilder] No base UniversalRenderPipelineAsset found. Skipping pipeline bifurcation.");
        }

        // 4. Force Global Lighting Configuration
        Lightmapping.bakedGI = false; // Requirement: baked GI disabled for WebGL perf targeting
        Debug.Log("[RenderSpineBuilder] Baked GI globally disabled.");

        // 5. Build the Scene Volume Framework
        GameObject volumeGo = GameObject.Find("GlobalVolume_SunderedFord");
        if (volumeGo == null)
        {
            volumeGo = new GameObject("GlobalVolume_SunderedFord");
        }
        
        Volume vol = volumeGo.GetComponent<Volume>();
        if (vol == null) vol = volumeGo.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.profile = profile;

        // Attach Enforcer for the Still Air mandate
        StillAirEnforcer enforcer = volumeGo.GetComponent<StillAirEnforcer>();
        if (enforcer == null) volumeGo.AddComponent<StillAirEnforcer>();

        Debug.Log("[RenderSpineBuilder] Scene GameObject 'GlobalVolume_SunderedFord' compiled. Build routine concluded.");
    }

    private static T AddOrUpdate<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (!profile.TryGet(out T component))
        {
            component = profile.Add<T>(true);
        }
        return component;
    }
}
#endif
