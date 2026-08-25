using System;
using System.Collections.Generic;
using UnityEngine;

/*
 * ATTACHMENT:
 * Attach this script to an empty GameObject named "MusterBoard" in the Sundered Ford scene.
 * 
 * INSPECTOR FIELDS TO SET:
 * - Grogen Banner Prefab: Prefab with an IBannerRenderer, defaults to Grogen back-art.
 * - Daminari Banner Prefab: Prefab with an IBannerRenderer, defaults to Daminari back-art.
 * - West Dig Zone: Transform representing the diggable ground west (Grogen spawn).
 * - East High Zone: Transform representing the high ground east (Daminari spawn).
 * - Obey Stillness: Boolean. When true, reveal animations execute instantly (0 frames) with no interpolation.
 */

/// <summary>
/// The authoritative ledger for the Muster phase at the Sundered Ford.
/// Enforces strict data-concealment: rendering views are denied access to banner identity 
/// until the reveal resolution is strictly executed.
/// </summary>
public class MusterBoard : MonoBehaviour
{
    [Header("Armoury & Renderers")]
    [SerializeField] private GameObject grogenBannerPrefab;
    [SerializeField] private GameObject daminariBannerPrefab;

    [Header("Sundered Ford Geography")]
    [SerializeField] private Transform westDigZone;
    [SerializeField] private Transform eastHighZone;

    [Header("Accessibility")]
    [Tooltip("If true, skips all visual flip animations to prevent strobing/motion.")]
    [SerializeField] private bool obeyStillness = true;

    // The sealed ledger. Concealment is enforced in data.
    // Unity's serialization and the camera cannot see what is inside this dictionary.
    private readonly Dictionary<int, BannerIdentity> _sealedLedger = new Dictionary<int, BannerIdentity>();
    private readonly Dictionary<int, IBannerRenderer> _activeRenderers = new Dictionary<int, IBannerRenderer>();

    private int _instanceCounter = 0;
    private bool _scoutActionConsumed = false;

    public enum Faction { Grogen, Daminari }

    /// <summary>
    /// The true data of a blade/banner. Struct is private to prevent external mutation.
    /// Bound strictly to an exact SKU for the Return Current.
    /// </summary>
    private struct BannerIdentity
    {
        public string SkuId;
        public int Vigour;
        public int Might;
        public int Guard;
        public int Magical;
        public bool IsIceMagic; // Phase II scope restricts live magic to Ice only.
    }

    /// <summary>
    /// Places a banner face-down in the correct geographical zone.
    /// The renderer is instantiated but given NO data other than its instance ID.
    /// </summary>
    public int PlaceConcealedBanner(Faction allegiance, string sku, int vigour, int might, int guard, int magical, bool isIce)
    {
        _instanceCounter++;
        int currentId = _instanceCounter;

        // 1. Seal the true data in the ledger. 
        _sealedLedger.Add(currentId, new BannerIdentity
        {
            SkuId = sku,
            Vigour = vigour,
            Might = might,
            Guard = guard,
            Magical = magical,
            IsIceMagic = isIce
        });

        // 2. Instantiate the blind view.
        GameObject prefabToUse = allegiance == Faction.Grogen ? grogenBannerPrefab : daminariBannerPrefab;
        Transform zoneToUse = allegiance == Faction.Grogen ? westDigZone : eastHighZone;

        GameObject bannerObj = Instantiate(prefabToUse, zoneToUse);
        IBannerRenderer renderer = bannerObj.GetComponent<IBannerRenderer>();

        if (renderer == null)
        {
            Debug.LogError("[MusterBoard] Prefab lacks IBannerRenderer. Acceptance law for Phase II broken.");
            return currentId;
        }

        // 3. Inform the view of its ID, but hand it ZERO identity data.
        renderer.InitializeFaceDown(currentId, allegiance.ToString());
        _activeRenderers.Add(currentId, renderer);

        return currentId;
    }

    /// <summary>
    /// Executes the single allowed scout action of the Muster phase.
    /// </summary>
    /// <param name="targetId">The instance ID of the enemy banner chosen to be scouted.</param>
    /// <returns>True if the scout was successful, false if the action was already consumed or invalid.</returns>
    public bool ExecuteSingleScout(int targetId)
    {
        if (_scoutActionConsumed)
        {
            Debug.LogWarning("[MusterBoard] Scout action already consumed this Muster.");
            return false;
        }

        if (!_sealedLedger.ContainsKey(targetId))
        {
            Debug.LogError("[MusterBoard] Scout targeted a non-existent drift or breach.");
            return false;
        }

        _scoutActionConsumed = true;
        ResolveReveal(targetId);
        return true;
    }

    /// <summary>
    /// The absolute resolution of a reveal. 
    /// This is the ONLY time a face is handed to the renderer/animator.
    /// </summary>
    public void ResolveReveal(int targetId)
    {
        if (!_sealedLedger.TryGetValue(targetId, out BannerIdentity trueIdentity))
        {
            Debug.LogError($"[MusterBoard] Attempted to reveal an unknown ID {targetId}. Breaches must be zero.");
            return;
        }

        if (_activeRenderers.TryGetValue(targetId, out IBannerRenderer renderer))
        {
            // Hand the exact piece data to the view, applying the stillness law.
            renderer.ExecuteReveal(
                trueIdentity.SkuId, 
                trueIdentity.Vigour, 
                trueIdentity.Might, 
                trueIdentity.Guard, 
                trueIdentity.Magical, 
                trueIdentity.IsIceMagic, 
                obeyStillness
            );
        }
    }

    /// <summary>
    /// Adds a unit to a zone for the Deep Dig ambush. Used by DigSystem.
    /// The banner is instantiated in the zone and its data will be read by
    /// the sealed StrikeReckoner naturally when it evaluates the zone.
    /// </summary>
    public void AddUnitToZone(int zoneIndex, GameObject bannerPrefab)
    {
        Transform zone = zoneIndex == 0 ? westDigZone : eastHighZone;
        if (zone != null && bannerPrefab != null)
        {
            Instantiate(bannerPrefab, zone);
        }
    }

    /// <summary>
    /// Returns the world position of a zone's center. Used by DigSystem for VFX placement.
    /// </summary>
    public Vector3 GetZoneCenter(int zoneIndex)
    {
        Transform zone = zoneIndex == 0 ? westDigZone : eastHighZone;
        return zone != null ? zone.position : Vector3.zero;
    }
}

/// <summary>
/// The interface required on the Banner View components.
/// Defined here to ensure the architectural boundary is clear to any view implementer.
/// </summary>
public interface IBannerRenderer
{
    /// <summary>
    /// Prepares the visual back-art. MUST NOT receive stats.
    /// </summary>
    void InitializeFaceDown(int instanceId, string factionString);

    /// <summary>
    /// Receives the true SKU and stats only when the game logic authorizes the reveal.
    /// </summary>
    void ExecuteReveal(string skuId, int vigour, int might, int guard, int magical, bool hasIce, bool enforceStillness);
}
