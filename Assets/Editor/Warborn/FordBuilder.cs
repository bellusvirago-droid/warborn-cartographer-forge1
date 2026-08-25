using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Warborn.Builder
{
    /// <summary>
    /// THE CARTOGRAPHER'S FORGE: SUNDERED FORD
    /// Idempotent, headless builder for the Phase II Vertical Slice scene.
    /// Generates the terrain, the broken crossing, lighting (Still Air compliant),
    /// the post-processing Grade, and the Muster anchors for the Grogens and Daminari.
    /// </summary>
    public static class FordBuilder
    {
        // ------------------------------------------------------------------------
        // CONSTANTS & PATHS
        // ------------------------------------------------------------------------
        private const string SCENE_DIR = "Assets/Scenes";
        private const string SCENE_PATH = "Assets/Scenes/SunderedFord.unity";
        private const string SETTINGS_DIR = "Assets/Settings";
        private const string PROFILE_PATH = "Assets/Settings/TheGrade_SunderedFord.asset";

        // Required Founder's Purse Asset Paths
        private const string ASSET_CASTLE_WALL = "Assets/MedievalCastleKit/Prefabs/Ruins/Wall_Ruined_01.prefab";
        private const string ASSET_CASTLE_PILLAR = "Assets/MedievalCastleKit/Prefabs/Ruins/Pillar_Broken_02.prefab";
        private const string ASSET_VFX_ICE = "Assets/UltimateVFX/Prefabs/Magic/Frost/VFX_Ice_Burst.prefab";
        private const string ASSET_VFX_BLOOD = "Assets/UltimateVFX/Prefabs/Combat/Blood/VFX_Blood_Hit_Directional.prefab";
        private const string ASSET_VFX_DUST = "Assets/UltimateVFX/Prefabs/Environment/Dust/VFX_Dust_Impact.prefab";

        // ------------------------------------------------------------------------
        // THE HEADLESS ENTRY POINT
        // ------------------------------------------------------------------------
        [MenuItem("Warborn/Forge/Build Sundered Ford")]
        public static void BuildHeadless()
        {
            Debug.Log("[FordBuilder] Striking the anvil. Forging the Sundered Ford...");

            EnsureDirectories();

            // 1. Clear the board. Idempotent creation.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "SunderedFord";

            // 2. The Grade (Post-processing mapped exactly to Survey specifications)
            BuildTheGrade();

            // 3. The Watchful Eye (Camera Rig)
            BuildCameraRig();

            // 4. Torch-warm raking light (Still Air compliant)
            BuildLighting();

            // 5. The Ground & Ruins
            BuildFordTerrain();
            BuildBrokenCrossing();

            // 6. The Muster (Banners & spawn anchors)
            BuildMusterAnchors();

            // 7. VFX Hooks (Bound to the Return Current & Strike Reckoning)
            BuildVFXCoordinator();

            // Save and seal.
            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            Debug.Log($"[FordBuilder] The Sundered Ford is built. Sealed at {SCENE_PATH}");
        }

        // ------------------------------------------------------------------------
        // ARCHITECTURE METHODS
        // ------------------------------------------------------------------------

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            if (!AssetDatabase.IsValidFolder("Assets/Settings")) AssetDatabase.CreateFolder("Assets", "Settings");
        }

        private static void BuildTheGrade()
        {
            // Extracting values from Survey: "The Grade (Post-processing)"
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PROFILE_PATH);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, PROFILE_PATH);
            }
            profile.components.Clear();

            // Bloom (intensity: 0.45, threshold: 0.62, smoothing: 0.28)
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.45f);
            bloom.threshold.Override(0.62f);
            bloom.scatter.Override(0.28f); // URP maps scatter closely to 'smoothing'

            // Vignette (offset/center: 0.42 roughly translates to intensity/smoothness mapping, darkness: 0.5)
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.5f);
            vignette.smoothness.Override(0.42f);

            // Film Grain (Noise opacity: 0.02)
            var grain = profile.Add<FilmGrain>(true);
            grain.type.Override(FilmGrainLookup.Thin2);
            grain.intensity.Override(0.02f);

            // Tone Mapping Exposure = 1.35
            var colorAdjust = profile.Add<ColorAdjustments>(true);
            colorAdjust.postExposure.Override(1.35f);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            // Attach to a global volume in scene
            GameObject volumeObj = new GameObject("TheGrade_Volume");
            var volume = volumeObj.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.profile = profile;
        }

        private static void BuildCameraRig()
        {
            // Survey Init: fov = 42, position = (0, 13.5, 12)
            GameObject camObj = new GameObject("TheWatchfulEye");
            camObj.transform.position = new Vector3(0, 13.5f, 12f);
            // Looking towards the center of the field (0,0,0)
            camObj.transform.LookAt(Vector3.zero);

            Camera cam = camObj.AddComponent<Camera>();
            cam.fieldOfView = 42f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            ColorUtility.TryParseHtmlString("#1f1915", out Color bg); cam.backgroundColor = bg; // FALLEN_TONE

            var camData = camObj.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        }

        private static void BuildLighting()
        {
            GameObject sunObj = new GameObject("Sun_TorchWarm");
            Light sun = sunObj.AddComponent<Light>();
            sun.type = LightType.Directional;
            
            // Raking light: low angle, warm tone. 
            // Still Air compliance: No animation attached, static intensity.
            sunObj.transform.rotation = Quaternion.Euler(22f, -45f, 0f);
            sun.color = new Color(1.0f, 0.85f, 0.7f); // Warm torch-like proxy
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;

            // Environmental lighting
            RenderSettings.ambientMode = AmbientMode.Flat;
            ColorUtility.TryParseHtmlString("#241a12", out Color ambient); // LEATHER_DARK
            RenderSettings.ambientLight = ambient;

            // Fog to mask the edges of the board seamlessly
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = ambient;
            RenderSettings.fogDensity = 0.015f;
        }

        private static void BuildFordTerrain()
        {
            // A backing terrain to hold the hex grid visually.
            // East is high ground, West is diggable/low.
            GameObject terrainObj = new GameObject("Ground_SunderedFord");
            TerrainData tData = new TerrainData();
            tData.heightmapResolution = 129;
            tData.size = new Vector3(60, 10, 60);

            float[,] heights = new float[129, 129];
            for (int y = 0; y < 129; y++)
            {
                for (int x = 0; x < 129; x++)
                {
                    float normalizedX = x / 128f;
                    // 0.0 (West) to 1.0 (East)
                    if (normalizedX < 0.35f) heights[y, x] = 0.1f; // Diggable West
                    else if (normalizedX > 0.65f) heights[y, x] = 0.4f; // High East
                    else heights[y, x] = 0.05f; // Central river dip
                }
            }
            tData.SetHeights(0, 0, heights);

            TerrainCollider tCollider = terrainObj.AddComponent<TerrainCollider>();
            tCollider.terrainData = tData;
            Terrain terrain = terrainObj.AddComponent<Terrain>();
            terrain.terrainData = tData;

            // Center the terrain under the origin (board center is 0,0,0)
            terrainObj.transform.position = new Vector3(-30, -1f, -30);

            // Paint it Mud/Stone tones from Survey
            ColorUtility.TryParseHtmlString("#3d332a", out Color mudColor); // mud
            Material groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            groundMat.color = mudColor;
            groundMat.SetFloat("_Smoothness", 0.08f); // 1 - roughness(0.92)
            terrain.materialTemplate = groundMat;
        }

        private static void BuildBrokenCrossing()
        {
            // Instantiate ruins from the Castle Kit to form the crossing.
            GameObject crossingRoot = new GameObject("BrokenCrossing_Ruins");
            
            GameObject wallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ASSET_CASTLE_WALL);
            GameObject pillarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ASSET_CASTLE_PILLAR);

            if (wallPrefab != null)
            {
                // East Bank fortification remains
                InstantiatePrefabHeadless(wallPrefab, new Vector3(8, 0, 2), Quaternion.Euler(0, -15, 0), crossingRoot.transform);
                InstantiatePrefabHeadless(wallPrefab, new Vector3(9, 0, -3), Quaternion.Euler(0, 25, 0), crossingRoot.transform);
            }
            else
            {
                Debug.LogWarning("[FordBuilder] Castle Kit wall missing. Fallback placed.");
                CreateFallbackCube(new Vector3(8, 0.5f, 2), crossingRoot.transform);
            }

            if (pillarPrefab != null)
            {
                // Broken pillars in the ford
                InstantiatePrefabHeadless(pillarPrefab, new Vector3(2, -0.2f, 0), Quaternion.Euler(15, 0, 10), crossingRoot.transform);
                InstantiatePrefabHeadless(pillarPrefab, new Vector3(-1, -0.4f, 1), Quaternion.Euler(-10, 45, 0), crossingRoot.transform);
            }
        }

        private static void BuildMusterAnchors()
        {
            // Grogens (The Deep Dig) spawn West
            GameObject grogenAnchor = new GameObject("Muster_Grogens");
            grogenAnchor.transform.position = new Vector3(-6, 0, 0);
            grogenAnchor.tag = "Respawn";

            // Daminari (The Legion) spawn East
            GameObject daminariAnchor = new GameObject("Muster_Daminari");
            daminariAnchor.transform.position = new Vector3(6, 1.5f, 0);
            daminariAnchor.tag = "Respawn";
        }

        private static void BuildVFXCoordinator()
        {
            // Acts as the registry for the Strike engine to pull Ultimate VFX preloads
            GameObject vfxRoot = new GameObject("StrikeVFXCoordinator");
            
            // In a real system, this would be a custom MonoBehaviour holding these refs.
            // Here we attach a string/object dictionary or simply create child empty objects with references.
            // To remain headless and zero-human, we create empty children named exactly what the engine searches for.
            LoadVFXAsChild(ASSET_VFX_ICE, "Preload_IceMagic", vfxRoot.transform);
            LoadVFXAsChild(ASSET_VFX_BLOOD, "Preload_BloodHit", vfxRoot.transform);
            LoadVFXAsChild(ASSET_VFX_DUST, "Preload_DustImpact", vfxRoot.transform);
        }

        // ------------------------------------------------------------------------
        // UTILITIES
        // ------------------------------------------------------------------------
        
        private static void InstantiatePrefabHeadless(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = pos;
            instance.transform.rotation = rot;
            instance.transform.SetParent(parent);
        }

        private static void LoadVFXAsChild(string path, string childName, Transform parent)
        {
            GameObject vfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            GameObject hook = new GameObject(childName);
            hook.transform.SetParent(parent);
            
            if (vfxPrefab != null)
            {
                // We attach the prefab as a disabled child so it's loaded in memory for the scene
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(vfxPrefab);
                instance.transform.SetParent(hook.transform);
                instance.SetActive(false);
            }
            else
            {
                Debug.LogWarning($"[FordBuilder] Ultimate VFX missing at {path}. Engine will fall back to basic impacts.");
            }
        }

        private static void CreateFallbackCube(Vector3 pos, Transform parent)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = pos;
            cube.transform.SetParent(parent);
            cube.transform.localScale = new Vector3(2, 3, 0.5f);
        }
    }
}
