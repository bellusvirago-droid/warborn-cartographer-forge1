using UnityEngine;
using System.Collections;

/// <summary>
/// THE WARBORN MARCH - PHASE II: VERTICAL SLICE
/// ATTACHMENT: Attach to the "GrogenCommander" or "SignatureSystems" GameObject in the Sundered Ford scene.
/// 
/// REQUIRED INSPECTOR FIELDS:
/// - MusterBoard: The active board tracking unit placements.
/// - StrikeReckoner: The sealed combat evaluator (referenced for reading combat constants only).
/// - DigVFX: The particle system for the dirt eruption when the banner emerges.
/// - StillnessSetting: Boolean to disable looping/fast effects for accessibility compliance.
/// </summary>
public class DigSystem : MonoBehaviour
{
    [Header("Core Systems")]
    [Tooltip("The board where the banner will be injected upon ambush.")]
    [SerializeField] private MusterBoard musterBoard;
    
    [Tooltip("The sealed reckoner. We do not alter it, but bind to it to read phase timings if needed.")]
    [SerializeField] private StrikeReckoner strikeReckoner;

    [Header("Aesthetics & Compliance")]
    [Tooltip("Visuals for the dirt breaking. Must not strobe or exceed 3Hz.")]
    [SerializeField] private ParticleSystem digVFX;
    
    [Tooltip("If true, skips all VFX to comply with the stillness setting rule.")]
    [SerializeField] private bool stillnessSetting = false;

    // State tracking for the buried banner
    private bool hasBuriedBanner = false;
    private int buriedZoneIndex = -1;
    private GameObject buriedBannerPrefab; // Bound to the real SKU via Return Current mapping
    private bool isWasted = false;

    /// <summary>
    /// Buries a Grogen banner beneath a specific zone before the Muster.
    /// Called during the Pre-Muster deployment phase.
    /// </summary>
    /// <param name="zoneIndex">The index of the zone (e.g., west bank of the Sundered Ford).</param>
    /// <param name="bannerPrefab">The verified real SKU prefab from the armoury.</param>
    public void BuryBanner(int zoneIndex, GameObject bannerPrefab)
    {
        if (hasBuriedBanner)
        {
            Debug.LogWarning("Deep Dig constraint: Only ONE banner may be buried before the Muster.");
            return;
        }

        buriedZoneIndex = zoneIndex;
        buriedBannerPrefab = bannerPrefab;
        hasBuriedBanner = true;
        isWasted = false;
        
        // The banner is mathematically removed from hand, but not yet present on the MusterBoard.
        // StrikeReckoner currently sees nothing in this zone regarding this banner.
    }

    /// <summary>
    /// Binds to the battle flow. Must be called by the orchestrator right before the StrikeReckoner
    /// evaluates a contested zone. This ensures the banner is placed on the board purely via 
    /// standard MusterBoard methods, preventing any illegal setters on the sealed StrikeReckoner.
    /// </summary>
    /// <param name="activeZoneIndex">The zone currently being contested.</param>
    /// <param name="currentRound">The current round of the battle.</param>
    /// <param name="totalRounds">The total rounds of the battle.</param>
    public void CheckForAmbush(int activeZoneIndex, int currentRound, int totalRounds)
    {
        if (!hasBuriedBanner || isWasted) return;

        // Rule: A buried banner unspent by the last third of the battle is wasted.
        int twoThirdsMark = (totalRounds * 2) / 3;
        if (currentRound > twoThirdsMark)
        {
            isWasted = true;
            hasBuriedBanner = false; // Cannot be used anymore
            Debug.Log("Deep Dig failed: The battle progressed past the second third. The buried banner is wasted.");
            return;
        }

        // Rule: It fights from beneath, once, when the ground above it is contested.
        if (activeZoneIndex == buriedZoneIndex)
        {
            SpringAmbush(activeZoneIndex);
        }
    }

    /// <summary>
    /// Executes the Deep Dig ambush, revealing the banner and adding its Vigour/Might/Guard to the board
    /// just in time for the StrikeReckoner to read it organically.
    /// </summary>
    private void SpringAmbush(int zoneIndex)
    {
        hasBuriedBanner = false; // Fights from beneath once.

        // Add the unit to the board. The MusterBoard accepts it, meaning when the 
        // sealed StrikeReckoner calculates the zone, the Grogen forces include this unit naturally.
        musterBoard.AddUnitToZone(zoneIndex, buriedBannerPrefab);

        // Handle visual effects in strict compliance with the stillness setting.
        if (!stillnessSetting && digVFX != null)
        {
            // Ensure the VFX is positioned at the contested zone (assuming MusterBoard provides coordinates)
            Vector3 zonePosition = musterBoard.GetZoneCenter(zoneIndex);
            digVFX.transform.position = zonePosition;
            
            // Enforce max 3Hz limit on any looping aspects within the particle system itself (pre-configured in inspector),
            // but we ensure it just plays a single burst here.
            digVFX.Play();
        }

        Debug.Log($"Deep Dig successful! Grogen banner emerged at zone {zoneIndex} before the Strike.");
    }

    /// <summary>
    /// Allows the UI or orchestration system to check if the Grogens have a banner lying in wait.
    /// Used by the Eye to report zero breaches and zero open drifts by confirming state.
    /// </summary>
    public bool IsBannerCurrentlyBuried() 
    {
        return hasBuriedBanner && !isWasted;
    }
}
