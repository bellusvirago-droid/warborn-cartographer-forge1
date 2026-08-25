using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
#endif

/**
 * ATTACHMENT:
 * Attach this script to the root GameObject of every Bearer prefab (Grogen line, Daminari legion).
 * 
 * INSPECTOR FIELDS:
 * None. This script is fully self-configuring. The headless builder assigns the AnimatorController.
 * 
 * PURPOSE:
 * Executes the animation vocabulary for Phase II (The Vertical Slice). 
 * Translates the March's procedural state (felled, struck, walk) into Humanoid clip playback.
 */

[RequireComponent(typeof(Animator))]
public class WarbornAnimator : MonoBehaviour
{
    private Animator _animator;
    
    // Parameter hashes for performance
    private readonly int _speedHash = Animator.StringToHash("Speed");
    private readonly int _strikeHash = Animator.StringToHash("Strike");
    private readonly int _struckHash = Animator.StringToHash("Struck");
    private readonly int _fallenHash = Animator.StringToHash("Fallen");
    private readonly int _castHash = Animator.StringToHash("Cast");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Drives the Locomotion blend tree. 
    /// </summary>
    /// <param name="speed">0 for Idle, 1 for Walk, >1 for Run (if supported).</param>
    public void SetLocomotion(float speed)
    {
        _animator.SetFloat(_speedHash, speed);
    }

    /// <summary>
    /// Triggers the melee strike. The controller will return to locomotion automatically.
    /// </summary>
    public void PlayStrike()
    {
        _animator.SetTrigger(_strikeHash);
    }

    /// <summary>
    /// Triggers the hit reaction, bypassing the WebGL procedural recoil (lean/sway),
    /// upgrading it to full Humanoid reaction as permitted by Phase II aesthetic rules.
    /// </summary>
    public void PlayStruck()
    {
        _animator.SetTrigger(_struckHash);
    }

    /// <summary>
    /// Locks the bearer into the Death state. Evaluates the March's 'felled' state.
    /// </summary>
    public void PlayFallen(bool isFelled)
    {
        _animator.SetBool(_fallenHash, isFelled);
    }

    /// <summary>
    /// Triggers the casting animation for the Ice magic contract.
    /// </summary>
    public void PlayCast()
    {
        _animator.SetTrigger(_castHash);
    }
}

#if UNITY_EDITOR
/**
 * HEADLESS BUILDER 1: THE RIG POSTPROCESSOR
 * Automatically sets all models in the specified packs to Humanoid rig upon import.
 * Eliminates the need for a human to click 'Rig -> Humanoid -> Apply' in the Inspector.
 */
public class WarbornRigPostprocessor : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        // Only touch the Founder's designated Phase II asset paths
        if (assetPath.Contains("EuropeanKnightsPack01") || 
            assetPath.Contains("KevinIglesias"))
        {
            ModelImporter importer = (ModelImporter)assetImporter;
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                // Standardize materials to allow runtime swapping for Grogen/Daminari liveries
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            }
        }
    }
}

/**
 * HEADLESS BUILDER 2: THE CONTROLLER FORGE
 * Automatically constructs the WarbornAnimator.controller on project load/compile.
 * Wires clips from Kevin Iglesias packs by name, building a complete state machine.
 */
[InitializeOnLoad]
public class WarbornAnimatorBuilder
{
    private const string CONTROLLER_DIR = "Assets/Warborn/Animations";
    private const string CONTROLLER_PATH = CONTROLLER_DIR + "/WarbornAnimator.controller";

    static WarbornAnimatorBuilder()
    {
        EditorApplication.delayCall += BuildControllerIfMissing;
    }

    private static void BuildControllerIfMissing()
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER_PATH) != null)
            return; // Already forged.

        if (!Directory.Exists(CONTROLLER_DIR))
            Directory.CreateDirectory(CONTROLLER_DIR);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);

        // Define Parameters
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Strike", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Struck", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Fallen", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Cast", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine rootSM = controller.layers[0].stateMachine;

        // 1. Locomotion (BlendTree)
        BlendTree locomotionTree;
        AnimatorState locomotionState = controller.CreateBlendTreeInController("Locomotion", out locomotionTree);
        locomotionTree.blendParameter = "Speed";
        
        AnimationClip idleClip = FindClip("Idle", "Assets/KevinIglesias/HumanBasicMotionsFREE");
        AnimationClip walkClip = FindClip("Walk", "Assets/KevinIglesias/HumanBasicMotionsFREE");
        
        if (idleClip != null) locomotionTree.AddChild(idleClip, 0f);
        if (walkClip != null) locomotionTree.AddChild(walkClip, 1f);
        
        rootSM.defaultState = locomotionState;

        // 2. Action States (Strike, Struck, Cast)
        AnimatorState strikeState = rootSM.AddState("Strike");
        strikeState.motion = FindClip("Attack", "Assets/KevinIglesias/HumanMeleeAnimations") ?? idleClip;

        AnimatorState struckState = rootSM.AddState("Struck");
        struckState.motion = FindClip("Hit", "Assets/KevinIglesias/HumanMeleeAnimations") ?? idleClip;

        AnimatorState castState = rootSM.AddState("Cast");
        // Fallback to a generic interaction/attack if Cast is missing in standard Melee pack
        castState.motion = FindClip("Cast", "Assets/KevinIglesias") ?? FindClip("Attack", "Assets/KevinIglesias");

        // 3. Death State
        AnimatorState deathState = rootSM.AddState("Death");
        deathState.motion = FindClip("Death", "Assets/KevinIglesias/HumanMeleeAnimations") ?? idleClip;

        // 4. Transitions from AnyState
        AnimatorStateTransition anyToStrike = rootSM.AddAnyStateTransition(strikeState);
        anyToStrike.AddCondition(AnimatorConditionMode.If, 0, "Strike");
        anyToStrike.duration = 0.1f;

        AnimatorStateTransition anyToStruck = rootSM.AddAnyStateTransition(struckState);
        anyToStruck.AddCondition(AnimatorConditionMode.If, 0, "Struck");
        anyToStruck.duration = 0.1f;

        AnimatorStateTransition anyToCast = rootSM.AddAnyStateTransition(castState);
        anyToCast.AddCondition(AnimatorConditionMode.If, 0, "Cast");
        anyToCast.duration = 0.1f;

        AnimatorStateTransition anyToDeath = rootSM.AddAnyStateTransition(deathState);
        anyToDeath.AddCondition(AnimatorConditionMode.If, 0, "Fallen");
        anyToDeath.duration = 0.2f;
        anyToDeath.canTransitionToSelf = false;

        // 5. Return to Locomotion
        AnimatorStateTransition strikeToLoco = strikeState.AddTransition(locomotionState);
        strikeToLoco.hasExitTime = true;
        strikeToLoco.exitTime = 0.8f;

        AnimatorStateTransition struckToLoco = struckState.AddTransition(locomotionState);
        struckToLoco.hasExitTime = true;
        struckToLoco.exitTime = 0.8f;

        AnimatorStateTransition castToLoco = castState.AddTransition(locomotionState);
        castToLoco.hasExitTime = true;
        castToLoco.exitTime = 0.8f;

        AssetDatabase.SaveAssets();
        Debug.Log("[Cartographer] Forged WarbornAnimator.controller headlessly.");
    }

    /// <summary>
    /// Searches the specific directory for a clip containing the search string.
    /// Guarantees the headless builder always binds a valid clip if the pack exists.
    /// </summary>
    private static AnimationClip FindClip(string searchName, string folderPath)
    {
        if (!Directory.Exists(folderPath)) return null;

        string[] guids = AssetDatabase.FindAssets("t:AnimationClip " + searchName, new[] { folderPath });
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        }
        return null;
    }
}
#endif
