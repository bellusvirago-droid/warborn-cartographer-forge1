using UnityEngine;
using System.Collections.Generic;

/*
 * ATTACH TO: An empty GameObject named "StillAir" in the root of the initialization scene.
 * 
 * INSPECTOR FIELDS:
 * - GlobalFreezeSwitch (bool): The single master switch. When true, freezes all registered motion everywhere at once.
 * 
 * CONTEXT & LAW:
 * Phase II — The Vertical Slice. Grogens vs Daminari at the Sundered Ford.
 * The March is played FREE; blades test exactly as the SKU sold.
 * Safety laws strictly enforced: No loops > 3 Hz. No strobing red. 
 * The system clamps breaches; it does not trust the components to behave.
 */

public class StillAir : MonoBehaviour
{
    public static StillAir Instance { get; private set; }

    [Header("The Master Switch")]
    [Tooltip("Instantly freezes all registered animations, particles, and light shifts when true.")]
    public bool GlobalFreezeSwitch = false;

    // 3 Hz limit mathematically dictates a minimum of 0.333 seconds per full cycle.
    private const float MaxAllowedFrequencyHz = 3.0f;
    private const float MinAllowedPeriodSeconds = 1.0f / MaxAllowedFrequencyHz;

    // Registries for elements that produce motion or visual oscillation.
    private List<Animator> registeredAnimators = new List<Animator>();
    private List<ParticleSystem> registeredParticles = new List<ParticleSystem>();
    
    // Wrapper to track Light state over time to definitively catch and block strobing.
    private class TrackedLight 
    {
        public Light Source;
        public Color LastSafeColor;
        public float LastSafeIntensity;
        public float TimeOfLastChange;
    }
    private List<TrackedLight> registeredLights = new List<TrackedLight>();

    private void Awake()
    {
        // Ensure singleton survival across scene changes (e.g., loading into the Sundered Ford mid-battle).
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    #region Public Registration Methods

    public void RegisterAnimator(Animator anim)
    {
        if (anim != null && !registeredAnimators.Contains(anim))
            registeredAnimators.Add(anim);
    }

    // Ice magic and environmental dust must be registered here.
    public void RegisterParticleSystem(ParticleSystem ps)
    {
        if (ps != null && !registeredParticles.Contains(ps))
            registeredParticles.Add(ps);
    }

    public void RegisterLight(Light light)
    {
        if (light != null)
        {
            // Initialize safe baseline upon registration.
            registeredLights.Add(new TrackedLight 
            {
                Source = light,
                LastSafeColor = light.color,
                LastSafeIntensity = light.intensity,
                TimeOfLastChange = Time.unscaledTime
            });
        }
    }

    #endregion

    private void LateUpdate()
    {
        // LateUpdate ensures we clamp values after other scripts (like StrikeReckoner logic) have attempted to modify them.
        EnforceSafetyLaws();
    }

    private void EnforceSafetyLaws()
    {
        // 1. Process Animators (Character movement, UI pulses)
        for (int i = registeredAnimators.Count - 1; i >= 0; i--)
        {
            Animator anim = registeredAnimators[i];
            if (anim == null)
            {
                registeredAnimators.RemoveAt(i);
                continue;
            }

            if (GlobalFreezeSwitch)
            {
                // The stillness setting overrides all.
                anim.speed = 0f;
                continue;
            }

            // Enforce 3 Hz maximum cycle rate on the active clip.
            AnimatorClipInfo[] clipInfo = anim.GetCurrentAnimatorClipInfo(0);
            if (clipInfo.Length > 0)
            {
                float clipLength = clipInfo[0].clip.length;
                float currentSpeed = anim.speed;
                
                // If speed is pushing the loop faster than 0.333s, clamp it down.
                if (currentSpeed > 0f && (clipLength / currentSpeed) < MinAllowedPeriodSeconds)
                {
                    anim.speed = clipLength / MinAllowedPeriodSeconds;
                }
            }
        }

        // 2. Process Particle Systems (Ice Magic)
        for (int i = registeredParticles.Count - 1; i >= 0; i--)
        {
            ParticleSystem ps = registeredParticles[i];
            if (ps == null)
            {
                registeredParticles.RemoveAt(i);
                continue;
            }

            if (GlobalFreezeSwitch)
            {
                if (!ps.isPaused) ps.Pause(true); // Freeze all child systems too.
                continue;
            }
            else if (ps.isPaused)
            {
                ps.Play(true);
            }

            // Clamp overall simulation speed to ensure rapid flashing via timescale is blocked.
            var main = ps.main;
            if (main.simulationSpeed > MaxAllowedFrequencyHz)
            {
                main.simulationSpeed = MaxAllowedFrequencyHz;
            }
        }

        // 3. Process Lights (Environment and Impact Flashes)
        for (int i = registeredLights.Count - 1; i >= 0; i--)
        {
            TrackedLight tl = registeredLights[i];
            if (tl.Source == null)
            {
                registeredLights.RemoveAt(i);
                continue;
            }

            if (GlobalFreezeSwitch)
            {
                // Lock emission to last known safe state during stillness.
                tl.Source.color = tl.LastSafeColor;
                tl.Source.intensity = tl.LastSafeIntensity;
                continue;
            }

            bool colorChanged = tl.Source.color != tl.LastSafeColor;
            // Use epsilon to prevent floating point inaccuracies from flagging as a change.
            bool intensityChanged = !Mathf.Approximately(tl.Source.intensity, tl.LastSafeIntensity);

            if (colorChanged || intensityChanged)
            {
                // Unscaled time is used so pausing the game doesn't cheat the real-world flicker rate.
                float timeSinceChange = Time.unscaledTime - tl.TimeOfLastChange;

                if (timeSinceChange < MinAllowedPeriodSeconds && timeSinceChange > 0f)
                {
                    // Frequency breach detected. 
                    // Check specifically for Strobing Red (R is dominant channel).
                    Color c = tl.Source.color;
                    bool isStrobingRed = c.r > 0.5f && (c.r > c.g * 1.5f) && (c.r > c.b * 1.5f);

                    if (isStrobingRed || intensityChanged)
                    {
                        // Clamp by reverting the material/light change entirely. 
                        // It is clamped by the system rather than trusted to behave.
                        tl.Source.color = tl.LastSafeColor;
                        tl.Source.intensity = tl.LastSafeIntensity;
                    }
                }
                else
                {
                    // Update was slow enough to be safe. Record the new baseline.
                    tl.LastSafeColor = tl.Source.color;
                    tl.LastSafeIntensity = tl.Source.intensity;
                    tl.TimeOfLastChange = Time.unscaledTime;
                }
            }
        }
    }
}
