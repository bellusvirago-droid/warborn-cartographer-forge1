using UnityEngine;
using WarbornMarch.PhaseII;

/*
 * FILE: IceHand.cs
 *
 * ATTACHMENT:
 * Attach this script to the 'CommanderAvatar' GameObject in the Sundered Ford scene.
 *
 * INSPECTOR FIELDS REQUIRED:
 * - Reckoner: Reference to the existing, sealed StrikeReckoner component.
 * - Meters: Reference to the Commander's MeterSet component.
 * - Ice Vfx: A ParticleSystem for the cast. (MUST NOT strobe red. Max emission/loop < 3 Hz).
 * - Static Ice Overlay: A SpriteRenderer for the stillness fallback.
 * - Stillness Setting: Boolean to skip VFX for accessibility compliance.
 */

public class IceHand : MonoBehaviour
{
    [Header("Core Rulings (Patent-Locked)")]
    [Tooltip("The sealed StrikeReckoner. IceHand feeds inputs here and NEVER calculates outcomes.")]
    [SerializeField] private StrikeReckoner reckoner;

    [Tooltip("The player's MeterSet. Every cast drains from Magical.")]
    [SerializeField] private MeterSet meters;

    [Tooltip("Fixed magical cost to cast Ice. Governed by the economic constants of the March.")]
    [SerializeField] private float magicalCost = 20f;

    [Header("Aesthetics & Accessibility")]
    [Tooltip("Standard Ice cast VFX. Strict enforcement: no flashing faster than 3 Hz, no red.")]
    [SerializeField] private ParticleSystem iceVfx;

    [Tooltip("Static visual fallback used when the Stillness setting is enabled.")]
    [SerializeField] private SpriteRenderer staticIceOverlay;

    [Tooltip("If true, skips all VFX to comply with the stillness setting.")]
    [SerializeField] private bool stillnessSetting = false;

    [Tooltip("Duration the static overlay remains visible before fading, in seconds.")]
    [SerializeField] private float staticOverlayDuration = 1.5f;

    /// <summary>
    /// Set true when Ice is channeled this strike. The BattleManager reads this
    /// flag and passes it to StrikeReckoner.ReckonStrike as isIceInvoked.
    /// IceHand never overrules the reckoning — it only signals intent.
    /// </summary>
    public bool IsIceChanneled { get; private set; }

    /// <summary>
    /// Attempts to channel Ice into the current Strike at the Sundered Ford.
    /// This may be triggered by the InputSystem or a UI event.
    /// </summary>
    public void TryCastIce()
    {
        // We read the Magical meter. It is the ONLY way to pay for this cast.
        // No bypasses, no alternative currencies. The trial remains free, but in-game physics apply.
        if (meters.TryCastIce(magicalCost))
        {
            // IceHand NEVER overrules the reckoning. It merely signals that Ice was channeled.
            // The sealed StrikeReckoner determines how Ice interacts with Grogen/Daminari modifiers
            // when the BattleManager calls ReckonStrike with isIceInvoked = true.
            IsIceChanneled = true;

            // Trigger the aesthetic payload, strictly obeying the stillness rules.
            PlayCastVisuals();
        }
    }

    /// <summary>
    /// Clears the Ice channeled flag after the Strike resolves.
    /// Called by the BattleManager between strikes.
    /// </summary>
    public void ClearIceChannel()
    {
        IsIceChanneled = false;
    }

    /// <summary>
    /// Handles the aesthetic representation of the cast while rigorously respecting the 3 Hz limit and stillness settings.
    /// </summary>
    private void PlayCastVisuals()
    {
        // Check the stillness setting to see if the player requires stillness.
        if (stillnessSetting)
        {
            // If stillness is required, we bypass the particle system entirely.
            // Instead, we display a static sprite overlay to indicate the cast.
            if (staticIceOverlay != null)
            {
                staticIceOverlay.enabled = true;

                // Schedule the overlay to be disabled after a set duration, avoiding loops.
                Invoke(nameof(DisableStaticOverlay), staticOverlayDuration);
            }
        }
        else
        {
            // If stillness is not enforced, play the standard particle effect.
            // Note for the art team: the prefab itself must still obey the < 3 Hz non-strobing rule.
            if (iceVfx != null)
            {
                iceVfx.Play();
            }
        }
    }

    /// <summary>
    /// Clears the static overlay. Called via Invoke to keep logic simple and non-looping.
    /// </summary>
    private void DisableStaticOverlay()
    {
        if (staticIceOverlay != null)
        {
            staticIceOverlay.enabled = false;
        }
    }
}
