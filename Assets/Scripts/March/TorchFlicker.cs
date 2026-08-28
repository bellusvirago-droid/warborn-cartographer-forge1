/* 
 * THE WARBORN MARCH - PHASE II: THE VERTICAL SLICE
 * FordLighting.cs
 * 
 * Usage:
 * - Runtime: The TorchFlicker MonoBehaviour attaches to point lights to provide < 3Hz animation.
 * - Editor: The FordLightingBuilder provides a headless, idempotent routine to author the scene's 
 *   entire lighting rig, post-processing volume, skybox, probes, and trigger a GPU lightmap bake.
 * 
 * Headless Invocation:
 * Unity -quit -batchmode -projectPath . -executeMethod FordLightingBuilder.AuthorAndBakeHeadless
 */

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;
using System.Collections.Generic;
#endif

/// <summary>
/// Attaches to Torch point lights. Obeys the < 3Hz limit and the global stillness requirement.
/// Never strobes red; only fluctuates intensity of an amber light.
/// </summary>
[RequireComponent(typeof(Light))]
public class TorchFlicker : MonoBehaviour
{
    [Tooltip("Must be under 3.0 to comply with epilepsy/stillness guidelines.")]
    [Range(0.1f, 2.9f)]
    public float flickerFrequency = 2.4f;
    public float baseIntensity = 1.5f;
    public float flickerAmplitude = 0.5f;
    
    // Bound to the global setting. When true, all lights freeze immediately.
    public static bool GlobalStillness = false;

    private Light _light;
    private float _seed;

    void Start()
    {
        _light = GetComponent<Light>();
        _seed = Random.Range(0f, 100f);
        
        // The Survey demands Torch Amber
        ColorUtility.TryParseHtmlString("#FF9B30", out Color amber);
        _light.color = amber;
    }

    void Update()
    {
        if (GlobalStillness)
        {
            _light.intensity = baseIntensity;
            return;
        }

        // Perlin noise guarantees smooth, continuous fluctuation (no strobing), locked under 3Hz.
        float noise = Mathf.PerlinNoise(Time.time * flickerFrequency, _seed);
        _light.intensity = baseIntensity + ((noise - 0.5f) * flickerAmplitude);
    }
}

#if UNITY_EDITOR
/// <summary>
/// The Cartographer's idempotent lighting rig builder. Never requires a click.
/// Generates the Bruised Sky, the Raking Sun, the Exponential Fog, the Probe Grid,
/// the URP Post-Processing Volume, and fires the Progressive GPU lightmapper.
/// </summary>
public static class FordLightingBuilder
{
    private const string SETTINGS_PATH = "Assets/Settings/Lighting";

    [MenuItem("Warborn/Lighting/Author and Bake Sundered Ford")]
    public static void AuthorAndBakeHeadless()
    {
        Debug.Log("[Warborn] Authoring Sundered Ford lighting rig...");
        
        EnsureFolder(SETTINGS_PATH);
        
        AuthorSkybox();
        AuthorSun();
        AuthorFogAndAmbient();
        AuthorPostProcessingVolume();
        AuthorProbes();
        AuthorTorches();
        
        ConfigureAndBakeLightmaps();
    }

    private static void AuthorSkybox()
    {
        string matPath = $"{SETTINGS_PATH}/BruisedSky.mat";
        Material skyMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        
        if (skyMat == null)
        {
            skyMat = new Material(Shader.Find("Skybox/Procedural"));
            AssetDatabase.CreateAsset(skyMat, matPath);
        }

        // The Bruised Sky: Late hour, epic, mournful.
        skyMat.SetFloat("_SunSize", 0.04f);
        skyMat.SetFloat("_SunSizeConvergence", 5f);
        skyMat.SetFloat("_AtmosphereThickness", 1.1f);
        
        ColorUtility.TryParseHtmlString("#2A2333", out Color skyTint);
        skyMat.SetColor("_SkyTint", skyTint);
        
        ColorUtility.TryParseHtmlString("#120F14", out Color groundColor);
        skyMat.SetColor("_GroundColor", groundColor);
        
        skyMat.SetFloat("_Exposure", 1.2f);
        
        RenderSettings.skybox = skyMat;
        EditorUtility.SetDirty(skyMat);
    }

    private static void AuthorSun()
    {
        Light sun = RenderSettings.sun;
        if (sun == null)
        {
            GameObject sunGO = GameObject.Find("Directional Light");
            if (sunGO == null) sunGO = new GameObject("Directional Light");
            
            sun = sunGO.GetComponent<Light>();
            if (sun == null) sun = sunGO.AddComponent<Light>();
        }

        sun.type = LightType.Directional;
        // Raking directional key low on the horizon.
        sun.transform.rotation = Quaternion.Euler(18f, -40f, 0f);
        
        // Pale dying gold to contrast the bruised sky.
        ColorUtility.TryParseHtmlString("#D4C3A3", out Color sunColor);
        sun.color = sunColor;
        sun.intensity = 1.8f;
        sun.shadows = LightShadows.Soft;
        sun.shadowNormalBias = 1.2f;
        sun.lightmapBakeType = LightmapBakeType.Mixed;
        
        RenderSettings.sun = sun;
    }

    private static void AuthorFogAndAmbient()
    {
        // Cold ambient fill from the sky
        RenderSettings.ambientMode = AmbientMode.Trilight;
        
        ColorUtility.TryParseHtmlString("#342D3D", out Color sky);    // Bruised
        ColorUtility.TryParseHtmlString("#221E29", out Color equator);
        ColorUtility.TryParseHtmlString("#120E15", out Color ground); // Wet earth
        
        RenderSettings.ambientSkyColor = sky;
        RenderSettings.ambientEquatorColor = equator;
        RenderSettings.ambientGroundColor = ground;

        // Volumetric-feeling exponential height fog (separates near from far bank)
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.012f;
        ColorUtility.TryParseHtmlString("#1F1C26", out Color fogColor);
        RenderSettings.fogColor = fogColor;
    }

    private static void AuthorPostProcessingVolume()
    {
        string profilePath = $"{SETTINGS_PATH}/FordPostProcessing.asset";
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, profilePath);
        }

        // ACES Tonemapping & Exposure matching the Survey (1.35)
        if (!profile.TryGet(out ColorAdjustments colorAdjustments)) colorAdjustments = profile.Add<ColorAdjustments>();
        colorAdjustments.postExposure.Override(1.35f);
        
        if (!profile.TryGet(out Tonemapping tonemapping)) tonemapping = profile.Add<Tonemapping>();
        tonemapping.mode.Override(TonemappingMode.ACES);

        // Bloom matching the Survey constants (Threshold 0.62, Intensity 0.85)
        if (!profile.TryGet(out Bloom bloom)) bloom = profile.Add<Bloom>();
        bloom.threshold.Override(0.62f);
        bloom.intensity.Override(0.85f);
        bloom.scatter.Override(0.5f);

        // Vignette matching the Survey (Darkness/Intensity 0.5)
        if (!profile.TryGet(out Vignette vignette)) vignette = profile.Add<Vignette>();
        vignette.intensity.Override(0.5f);
        vignette.smoothness.Override(0.42f);

        // Film Grain matching Survey (Noise opacity 0.045)
        if (!profile.TryGet(out FilmGrain grain)) grain = profile.Add<FilmGrain>();
        grain.intensity.Override(0.045f);
        grain.type.Override(FilmGrainLookup.Medium1);

        EditorUtility.SetDirty(profile);

        // Apply to global volume
        Volume globalVol = GameObject.FindAnyObjectByType<Volume>();
        if (globalVol == null)
        {
            GameObject volGO = new GameObject("Global PostProcessing");
            globalVol = volGO.AddComponent<Volume>();
            globalVol.isGlobal = true;
        }
        globalVol.profile = profile;
    }

    private static void AuthorProbes()
    {
        // 1. Light Probes for dynamic bodies (The Legion and The Deep Dig)
        LightProbeGroup probeGroup = GameObject.FindAnyObjectByType<LightProbeGroup>();
        if (probeGroup == null)
        {
            GameObject probeGO = new GameObject("Ford Light Probes");
            probeGroup = probeGO.AddComponent<LightProbeGroup>();
        }

        // Generate a grid over the 10x6 hex area of the Sundered Ford crossing
        List<Vector3> probePositions = new List<Vector3>();
        for (float x = -5f; x <= 25f; x += 3f)
        {
            for (float z = -8f; z <= 8f; z += 3f)
            {
                probePositions.Add(new Vector3(x, 0.5f, z)); // Core body height
                probePositions.Add(new Vector3(x, 2.5f, z)); // Overhead height
            }
        }
        probeGroup.probePositions = probePositions.ToArray();

        // 2. Reflection Probe (Steel MUST be the brightest thing on the field)
        ReflectionProbe refProbe = GameObject.FindAnyObjectByType<ReflectionProbe>();
        if (refProbe == null)
        {
            GameObject refGO = new GameObject("Ford Reflection Probe");
            refProbe = refGO.AddComponent<ReflectionProbe>();
        }
        refProbe.transform.position = new Vector3(10f, 2f, 0f);
        refProbe.size = new Vector3(40f, 20f, 30f);
        refProbe.mode = ReflectionProbeMode.Baked;
        refProbe.intensityMultiplier = 1.4f; // Push steel brightness
    }

    private static void AuthorTorches()
    {
        // We assume 4 torch markers in the scene, but if absent, we'll place structural dummies.
        // Idempotent: clear previous generated torches to prevent duplicates on re-runs.
        GameObject torchRoot = GameObject.Find("Generated_Torches");
        if (torchRoot != null) Object.DestroyImmediate(torchRoot);

        torchRoot = new GameObject("Generated_Torches");

        Vector3[] fordCorners = new Vector3[] {
            new Vector3(2f, 1f, -4f), new Vector3(2f, 1f, 4f),
            new Vector3(18f, 1f, -4f), new Vector3(18f, 1f, 4f)
        };

        for (int i = 0; i < fordCorners.Length; i++)
        {
            GameObject tGO = new GameObject($"Torch_{i}");
            tGO.transform.SetParent(torchRoot.transform);
            tGO.transform.position = fordCorners[i];

            Light l = tGO.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 8f;
            l.lightmapBakeType = LightmapBakeType.Realtime;

            TorchFlicker flicker = tGO.AddComponent<TorchFlicker>();
            flicker.flickerFrequency = 2.4f; // Strictly < 3Hz
            flicker.baseIntensity = 1.2f;
            flicker.flickerAmplitude = 0.4f;
        }
    }

    private static void ConfigureAndBakeLightmaps()
    {
        // Configure for WebGL mid-laptop capability with Progressive GPU
        LightmapEditorSettings.lightmapper = LightmapEditorSettings.Lightmapper.ProgressiveGPU;
        LightmapEditorSettings.bounces = 2;
        LightmapEditorSettings.bakeResolution = 10; // Texels per unit (deliberate WebGL optimization)
        LightmapEditorSettings.padding = 2;
        LightmapEditorSettings.textureCompression = true;
        LightmapEditorSettings.enableAmbientOcclusion = true;
        LightmapEditorSettings.aoMaxDistance = 1.5f;
        
        // Denoising ensures clean bake even at lower WebGL resolutions
        LightmapEditorSettings.filteringAtrousPositionSigma = 1f;
        LightmapEditorSettings.filteringMode = LightmapEditorSettings.FilterMode.Auto;

        Debug.Log("[Warborn] Firing Progressive GPU Lightmap Bake...");
        Lightmapping.Bake();
        Debug.Log("[Warborn] Lighting Authored and Baked. Steel is bright, the ground is cold.");
    }

    private static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string[] parts = path.Split('/');
            string currentPath = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                if (!AssetDatabase.IsValidFolder(currentPath + "/" + parts[i]))
                {
                    AssetDatabase.CreateFolder(currentPath, parts[i]);
                }
                currentPath += "/" + parts[i];
            }
        }
    }
}
#endif
