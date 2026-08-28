using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The Still Air law, made flesh.
///
/// [Attached to: a single persistent GameObject named "StillAir" in the Sundered Ford scene.]
/// [Inspector fields: none. RenderSpineBuilder.Build() seats this object headlessly.]
///
/// Photosensitive safety is non-negotiable: when stillness is asked for, no light,
/// animation or emission in the March may pulse faster than 3 Hz, and every
/// registered animator and particle system is slowed or frozen.
/// </summary>
[DisallowMultipleComponent]
public class StillAir : MonoBehaviour
{
    /// <summary>The one Still Air warden in the scene. May be null before wake.</summary>
    public static StillAir Instance { get; private set; }

    /// <summary>Global flag representing the stillness setting. True disables rapid pulsing.</summary>
    public static bool enabled = false;

    /// <summary>Readable alias for the stillness setting.</summary>
    public static bool Enabled
    {
        get { return enabled; }
        set { enabled = value; if (Instance != null) Instance.Apply(); }
    }

    private readonly List<Animator> _animators = new List<Animator>();
    private readonly List<ParticleSystem> _particles = new List<ParticleSystem>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Apply();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Bring an animator under the Still Air law.</summary>
    public void RegisterAnimator(Animator anim)
    {
        if (anim == null || _animators.Contains(anim)) return;
        _animators.Add(anim);
        ApplyTo(anim);
    }

    /// <summary>Bring a particle system under the Still Air law.</summary>
    public void RegisterParticleSystem(ParticleSystem ps)
    {
        if (ps == null || _particles.Contains(ps)) return;
        _particles.Add(ps);
        ApplyTo(ps);
    }

    /// <summary>Set stillness and re-apply to everything registered.</summary>
    public void SetStillness(bool still)
    {
        enabled = still;
        Apply();
    }

    /// <summary>Re-apply the law to every registered animator and emitter.</summary>
    public void Apply()
    {
        for (int i = _animators.Count - 1; i >= 0; i--)
        {
            if (_animators[i] == null) { _animators.RemoveAt(i); continue; }
            ApplyTo(_animators[i]);
        }
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            if (_particles[i] == null) { _particles.RemoveAt(i); continue; }
            ApplyTo(_particles[i]);
        }
    }

    private void ApplyTo(Animator anim)
    {
        // Half speed under stillness: no limb or flame cycle may read as a flicker.
        anim.speed = enabled ? 0.5f : 1f;
    }

    private void ApplyTo(ParticleSystem ps)
    {
        var main = ps.main;
        main.simulationSpeed = enabled ? 0.5f : 1f;
        var emission = ps.emission;
        emission.rateOverTimeMultiplier = enabled ? 0.35f : 1f;
    }
}
