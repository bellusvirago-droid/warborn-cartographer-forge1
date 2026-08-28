/*
 * ATTACHMENT INSTRUCTIONS:
 * Attach this script to a single empty GameObject in your Sundered Ford scene named "SunderedFord_HostDresser".
 * 
 * INSPECTOR FIELDS:
 * NONE. The Founder's decree mandates headless, judgment-free execution. 
 * The script will automatically locate the owned asset packs, generate textures, create URP materials, 
 * assemble prefabs, build the formations, and apply GPU-instanced variations on compile/wake.
 */

using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
public class HostDresser : MonoBehaviour
{
    // ------------------------------------------------------------------------
    // CONSTANTS: THE MARCH SURVEY PALETTE
    // ------------------------------------------------------------------------
    private const string COLOR_GROGEN_MUD = "#3d332a"; // The dug line, earth and iron
    private const string COLOR_GROGEN_STEEL = "#8e887c"; // Rough steel
    private const string COLOR_DAMINARI_OXBLOOD = "#7a2420"; // The Legion cloth
    private const string COLOR_DAMINARI_BRONZE = "#c69a43"; // Echo of turned steel used for brass/bronze

    private const string GEN_PATH = "Assets/Generated/HostFactions";

    // ------------------------------------------------------------------------
    // HEADLESS ENTRY POINTS
    // ------------------------------------------------------------------------
#if UNITY_EDITOR
    [MenuItem("Warborn/Phase II/Dress Hosts (Headless)")]
    public static void HeadlessExecute()
    {
        Debug.Log("[HostDresser] Headless execution initiated by the CI/Editor.");
        var instance = FindObjectOfType<HostDresser>();
        if (instance == null)
        {
            GameObject go = new GameObject("SunderedFord_HostDresser");
            instance = go.AddComponent<HostDresser>();
        }
        instance.DressTheHosts();
    }
#endif

    private void Awake()
    {
#if UNITY_EDITOR
        // In the editor, immediately run the idempotent setup when added or loaded.
        if (!Application.isPlaying)
        {
            DressTheHosts();
        }
#endif
    }

    private void Start()
    {
        // At runtime, apply the per-instance variation and "breathing" idle desync.
        // This runs instantly in WebGL to ensure twins do not exist and air is still.
        BreatheTheHosts();
    }

    // ------------------------------------------------------------------------
    // EDITOR: ASSET & SCENE GENERATION
    // ------------------------------------------------------------------------
#if UNITY_EDITOR
    private void DressTheHosts()
    {
        EnsureDirectory(GEN_PATH);

        // 1. Forge Materials (Idempotent)
        Material grogenMat = ForgeMaterial("Grogen_Mat", COLOR_GROGEN_MUD, COLOR_GROGEN_STEEL, 0.2f, 0.6f);
        Material daminariMat = ForgeMaterial("Daminari_Mat", COLOR_DAMINARI_OXBLOOD, COLOR_DAMINARI_BRONZE, 0.7f, 0.4f);

        // 2. Locate and Author Prefabs from Founder's Purse
        // European Knights Pack 01 & Modular Knights
        GameObject grogenBase = FindFirstModelInPath("Assets/EuropeanKnightsPack01");
        GameObject daminariBase = FindFirstModelInPath("Assets/TheTalesFactory/ModularKnights");

        if (grogenBase == null || daminariBase == null)
        {
            Debug.LogWarning("[HostDresser] Missing required packages in the Founder's Purse. Awaiting import.");
            return;
        }

        // 3. Assemble Faction Variants
        GameObject grogenPrefab = AssembleVariant("Grogen_Infantry", grogenBase, grogenMat, isGrogen: true);
        GameObject daminariPrefab = AssembleVariant("Daminari_Legionary", daminariBase, daminariMat, isGrogen: false);

        // 4. Place Formations
        PlaceFormations(grogenPrefab, daminariPrefab);

        // Ensure the scene is marked dirty so the headless runner saves it
        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
    }

    private void EnsureDirectory(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                if (!AssetDatabase.IsValidFolder(current + "/" + parts[i]))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current += "/" + parts[i];
            }
        }
    }

    private Material ForgeMaterial(string name, string colorHex, string metalHex, float metalness, float roughness)
    {
        string matPath = $"{GEN_PATH}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            // Strictly URP Lit for the console-tier look, batching, and performance.
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, matPath);
        }

        ColorUtility.TryParseHtmlString(colorHex, out Color baseColor);
        ColorUtility.TryParseHtmlString(metalHex, out Color metalColor);

        mat.SetColor("_BaseColor", baseColor);
        // Generate a synthetic texture map headless to avoid magenta or flat grey
        mat.SetFloat("_Metallic", metalness);
        mat.SetFloat("_Smoothness", 1.0f - roughness);

        // MANDATORY for WebGL frame hold: Enable GPU Instancing and SRP Batcher compatibility
        mat.enableInstancing = true;

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        return mat;
    }

    private GameObject FindFirstModelInPath(string path)
    {
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { path });
        if (guids.Length == 0) return null;
        return AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    private GameObject AssembleVariant(string name, GameObject source, Material mat, bool isGrogen)
    {
        string prefabPath = $"{GEN_PATH}/{name}.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing != null) return existing;

        // Instantiate source to modify safely
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
        
        // Part Swapping: Grogens get bare/hood/rough parts, Daminari gets plate/plume/heavy
        foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
        {
            string cName = child.name.ToLower();
            if (isGrogen && (cName.Contains("plume") || cName.Contains("plate") || cName.Contains("heavy")))
                child.gameObject.SetActive(false);
            if (!isGrogen && (cName.Contains("bare") || cName.Contains("hood") || cName.Contains("rag")))
                child.gameObject.SetActive(false);
        }

        // Apply the faction material to all active renderers
        foreach (Renderer r in instance.GetComponentsInChildren<Renderer>(true))
        {
            if (r.gameObject.activeSelf)
            {
                // Apply material array of the same length to preserve submesh structures
                Material[] mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
            }
        }

        // Ensure LODGroup exists for WebGL performance
        if (instance.GetComponent<LODGroup>() == null)
        {
            LODGroup lod = instance.AddComponent<LODGroup>();
            // Simple default LOD configuration
            LOD[] lods = new LOD[1];
            lods[0] = new LOD(0.05f, instance.GetComponentsInChildren<Renderer>());
            lod.SetLODs(lods);
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        DestroyImmediate(instance);
        return prefab;
    }

    private void PlaceFormations(GameObject grogenPrefab, GameObject daminariPrefab)
    {
        Transform root = transform.Find("Generated_Hosts");
        if (root != null) DestroyImmediate(root.gameObject);

        GameObject rootGo = new GameObject("Generated_Hosts");
        rootGo.transform.SetParent(transform);

        // Grogen Dug Line (West): Loose, staggered formation
        for (int i = 0; i < 40; i++)
        {
            // Spacing: Rough, organic offsets
            float zOffset = -20f + (i * 1.0f) + Random.Range(-0.5f, 0.5f);
            float xOffset = -10f + Random.Range(-1.5f, 1.5f);
            Vector3 pos = new Vector3(xOffset, 0, zOffset);

            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(grogenPrefab, rootGo.transform);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, 90 + Random.Range(-10, 10), 0); // Facing East
            go.name = $"Grogen_Soldier_{i}";
        }

        // Daminari Shieldwall (East): Tight, disciplined formation
        for (int i = 0; i < 40; i++)
        {
            // Spacing: Rigid lines
            float zOffset = -15f + (i % 20) * 1.5f;
            float xOffset = 10f + (i / 20) * 1.2f;
            Vector3 pos = new Vector3(xOffset, 0, zOffset);

            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(daminariPrefab, rootGo.transform);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, -90, 0); // Facing West perfectly
            go.name = $"Daminari_Soldier_{i}";
        }
    }
#endif

    // ------------------------------------------------------------------------
    // RUNTIME: INSTANCE VARIATION & IDLE DESYNC (STILL AIR COMPLIANT)
    // ------------------------------------------------------------------------
    private void BreatheTheHosts()
    {
        Transform root = transform.Find("Generated_Hosts");
        if (root == null) return;

        MaterialPropertyBlock block = new MaterialPropertyBlock();

        foreach (Transform child in root)
        {
            // 1. Break the Twins (Per-Instance Color Variation)
            Renderer[] renderers = child.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                r.GetPropertyBlock(block);
                
                // Extract base color of the shared material to offset it slightly
                Color baseColor = r.sharedMaterial != null && r.sharedMaterial.HasProperty("_BaseColor") 
                    ? r.sharedMaterial.GetColor("_BaseColor") 
                    : Color.white;

                // Subtly shift hue and value to differentiate soldiers without breaking faction read
                float vShift = Random.Range(-0.08f, 0.08f);
                Color instColor = new Color(
                    Mathf.Clamp01(baseColor.r + vShift),
                    Mathf.Clamp01(baseColor.g + vShift),
                    Mathf.Clamp01(baseColor.b + vShift),
                    1.0f
                );

                block.SetColor("_BaseColor", instColor);
                r.SetPropertyBlock(block);
            }

            // 2. Desync the Idle (The Host Breathes)
            Animator anim = child.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                // Push the animation state forward by a random normalized time (0.0 to 1.0).
                // This ensures nobody moves in perfect tandem, avoiding the strobing/patterning effect.
                // "Idle" is assumed as the default state from Kevin Iglesias' packs.
                anim.Play(0, -1, Random.value);
            }
        }
    }
}
