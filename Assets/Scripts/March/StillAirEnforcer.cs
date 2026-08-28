using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Enforces the Still Air law on the post-processing grade.
///
/// [Attached to: GlobalVolume_SunderedFord]
/// [Inspector fields: none. Seated by RenderSpineBuilder.Build().]
/// </summary>
[RequireComponent(typeof(Volume))]
public class StillAirEnforcer : MonoBehaviour
{
    private Volume _volume;
    private Bloom _bloom;
    private FilmGrain _grain;

    private void Awake()
    {
        _volume = GetComponent<Volume>();
        FetchEffects();
    }

    private void FetchEffects()
    {
        if (_volume != null && _volume.profile != null)
        {
            _volume.profile.TryGet(out _bloom);
            _volume.profile.TryGet(out _grain);
        }
    }

    private void Update()
    {
        if (_bloom == null || _grain == null) FetchEffects();

        // The Grade constants from the Survey.
        if (_bloom != null) _bloom.intensity.value = StillAir.enabled ? 0.45f : 0.85f;
        if (_grain != null) _grain.intensity.value = StillAir.enabled ? 0.02f : 0.045f;
    }
}
