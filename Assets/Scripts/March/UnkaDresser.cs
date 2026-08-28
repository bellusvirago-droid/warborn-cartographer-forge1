/*
 * FILE: UnkaDresser.cs
 * ATTACHES TO: NONE (This is an Editor-only static build script, not a MonoBehaviour)
 * INSPECTOR FIELDS: NONE (Designed strictly for headless automation; zero human judgement)
 * PURPOSE: Automates the placement and configuration of the 'Unka the Dragon' end-boss 
 *          for the Sundered Ford vertical slice. Resolves prefabs, applies URP materials,
 *          sets the DragonContract, and logs comprehensively for CI/CD runners.
 */

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Rendering;
using System;
using System.Linq;
using System.Collections.Generic;
#endif

public static class UnkaDresser
{
#if UNITY_EDITOR
    // The exact path as defined by the house ruling.
    private const string UNKA_PREFAB_DIR = "Assets/Malbers Animations/Dragons/4 - Unka the Dragon/Prefabs";
    private const string ROOT_NAME = "EndBoss_Unka";

    [MenuItem("Warborn/Build/Dress Sundered Ford (Unka)")]
    public static void DressEndBoss()
    {
        Debug.Log("[UnkaDresser] Commencing headless dressing of Unka the Dragon.");

        // 1. Locate the correct prefab based on priority: PBR > Poly Art > any Unka
        GameObject prefab = FindBestUnkaPrefab();
        if (prefab == null)
        {
            Debug.LogError($"[UnkaDresser] FATAL: No Unka prefab found in {UNKA_PREFAB_DIR}. Ensure the Malbers asset is imported.");
            return;
        }

        // 2. Prepare the Scene Root (idempotent for re-runs)
        GameObject existingRoot = GameObject.Find(ROOT_NAME);
        if (existingRoot != null)
        {
            Debug.Log($"[UnkaDresser] Existing {ROOT_NAME} found. Destroying for fresh instantiation.");
            GameObject.DestroyImmediate(existingRoot);
        }

        GameObject root = new GameObject(ROOT_NAME);
        
        // 3. Set world transforms
        // Positioned at the eastern high ground over the ford.
        root.transform.position = new Vector3(8f, 6f, -4f);
        root.transform.localScale = Vector3.one;
        // Rotate to look exactly at the ford center (0,0,0).
        Vector3 lookDir = Vector3.zero - root.transform.position;
        root.transform.rotation = Quaternion.LookRotation(lookDir);

        Debug.Log($"[UnkaDresser] Root instantiated at {root.transform.position}, looking towards center.");

        // 4. Instantiate the actual Malbers Prefab as a child
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        
        Debug.Log($"[UnkaDresser] Prefab '{prefab.name}' successfully instantiated under root.");

        // 5. Attach the sealed DragonContract if it exists in the assembly
        AttachDragonContract(root);

        // 6. Set the 'Dragon' layer if configured in the project
        int dragonLayer = LayerMask.NameToLayer("Dragon");
        if (dragonLayer != -1)
        {
            SetLayerRecursive(root, dragonLayer);
            Debug.Log("[UnkaDresser] Applied 'Dragon' layer to Unka and all children.");
        }
        else
        {
            Debug.LogWarning("[UnkaDresser] Layer 'Dragon' does not exist in project settings. Using default layer.");
        }

        // 7. Apply URP overrides if the current pipeline is URP
        if (IsURPActive())
        {
            Debug.Log("[UnkaDresser] URP pipeline detected. Attempting to apply URP material overrides.");
            ApplyURPMaterials(instance);
        }

        // Ensure the scene is marked dirty so the headless build saves these changes
        UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);

        Debug.Log("[UnkaDresser] Unka the Dragon dressing complete. Scene marked dirty.");
    }

    private static GameObject FindBestUnkaPrefab()
    {
        // Search for all prefabs in the specific Malbers directory
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { UNKA_PREFAB_DIR });
        List<GameObject> candidates = new List<GameObject>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("Unka"))
            {
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go != null) candidates.Add(go);
            }
        }

        if (candidates.Count == 0) return null;

        // Evaluate based on the Founder's strict priority rules
        GameObject pbr = candidates.FirstOrDefault(c => c.name.Contains("PBR"));
        if (pbr != null) 
        {
            Debug.Log("[UnkaDresser] Selected PBR variant.");
            return pbr;
        }

        GameObject polyArt = candidates.FirstOrDefault(c => c.name.Contains("Poly Art"));
        if (polyArt != null) 
        {
            Debug.Log("[UnkaDresser] Selected Poly Art variant as fallback.");
            return polyArt;
        }

        Debug.Log("[UnkaDresser] Selected standard Unka variant.");
        return candidates.First();
    }

    private static void AttachDragonContract(GameObject root)
    {
        // We use Reflection to avoid compile errors if DragonContract.cs hasn't been written or imported yet.
        // The rule states: "Add the sealed DragonContract component... if it exists."
        Type contractType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.Name == "DragonContract");

        if (contractType != null)
        {
            if (root.GetComponent(contractType) == null)
            {
                root.AddComponent(contractType);
                Debug.Log("[UnkaDresser] Sealed DragonContract component successfully attached.");
            }
        }
        else
        {
            Debug.LogWarning("[UnkaDresser] 'DragonContract' type not found in assemblies. Component not attached.");
        }
    }

    private static void SetLayerRecursive(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, newLayer);
        }
    }

    private static bool IsURPActive()
    {
        // Check if a Render Pipeline is active and contains "Universal" in its type name
        RenderPipelineAsset currentPipeline = GraphicsSettings.currentRenderPipeline;
        return currentPipeline != null && currentPipeline.GetType().Name.Contains("Universal");
    }

    private static void ApplyURPMaterials(GameObject instance)
    {
        // Safely scan renderers and attempt to swap standard materials for their Malbers URP equivalents
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer rend in renderers)
        {
            Material[] mats = rend.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < mats.Length; i++)
            {
                Material mat = mats[i];
                if (mat != null && !mat.name.Contains("URP"))
                {
                    // Search the AssetDatabase for a material with the same name + "URP"
                    string searchStr = $"{mat.name} URP t:Material";
                    string[] urpGuids = AssetDatabase.FindAssets(searchStr);
                    
                    if (urpGuids.Length > 0)
                    {
                        string urpPath = AssetDatabase.GUIDToAssetPath(urpGuids[0]);
                        Material urpMat = AssetDatabase.LoadAssetAtPath<Material>(urpPath);
                        if (urpMat != null)
                        {
                            mats[i] = urpMat;
                            changed = true;
                            Debug.Log($"[UnkaDresser] Swapped material {mat.name} -> {urpMat.name} on {rend.name}.");
                        }
                    }
                }
            }

            if (changed)
            {
                rend.sharedMaterials = mats;
            }
        }
    }
#endif
}
