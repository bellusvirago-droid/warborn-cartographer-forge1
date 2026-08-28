using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Attaches to the Main Camera GameObject in the War-Table scene.
/// Requires no Inspector setup; self-initializes its URP Volume and profile.
/// 
/// The Cartographer's Rule: This class maps the exact tracking math, Killing Lean, 
/// Ground Shudder, and orbit limits of the browser March. It reads the reckoning 
/// but cannot alter a single outcome. It obeys the Still Air setting strictly by 
/// snapping focuses and nullifying all shakes when stillness is requested.
/// </summary>
[RequireComponent(typeof(Camera))]
public sealed class WarTableCamera : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // CONSTANTS (Survey — The Body: Constants & Configuration)
    // -------------------------------------------------------------------------
    private const float KILL_LEAN_MS = 900f;
    private const float FOV_DEFAULT = 42f;
    private readonly Vector3 INIT_POS = new Vector3(0f, 13.5f, 12f);

    private const float MIN_POLAR = 0.35f;
    private const float MAX_POLAR = 1.16f;
    private const float MIN_DIST = 9f;
    private const float MAX_DIST = 42f;

    // -------------------------------------------------------------------------
    // STATE
    // -------------------------------------------------------------------------
    private Camera _cam;
    private Volume _postVolume;
    private Bloom _bloom;
    private DepthOfField _dof;
    private Vignette _vignette;
    private FilmGrain _grain;

    private Vector3 _currentTarget = Vector3.zero;
    private Vector3 _wantTarget = Vector3.zero;
    
    private float _struckAtMs = 0f;
    
    // Toggles for house accessibility
    public bool StillnessMode { get; set; } = false;
    public bool IsPointerHeld { get; set; } = false;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        _cam.fieldOfView = FOV_DEFAULT;
        transform.position = INIT_POS;

        EnsurePostProcessing();
    }

    /// <summary>
    /// Called by the scene orchestrator when the span or window changes to frame the field.
    /// Translates "Fit The Ground" (Algorithm 3).
    /// </summary>
    public void FitTheGround(float span, float screenWidth, float screenHeight)
    {
        float fovRad = _cam.fieldOfView * Mathf.Deg2Rad;
        float aspect = Mathf.Max(0.4f, screenWidth / Mathf.Max(1f, screenHeight));
        
        float across = span / (2f * Mathf.Tan(fovRad / 2f) * Mathf.Min(aspect, 2.2f));
        float down = (span * 0.82f) / (2f * Mathf.Tan(fovRad / 2f));
        
        float d = Mathf.Max(11f, Mathf.Min(40f, Mathf.Max(across, down) * 1.8f));
        
        Vector3 fitPos = new Vector3(0f, d * 0.78f, d * 0.6f);
        
        if (StillnessMode)
        {
            transform.position = fitPos;
            _currentTarget = Vector3.zero;
            transform.LookAt(_currentTarget);
        }
        else
        {
            // The script smoothly lerps here natively in LateUpdate, but we set the baseline.
            transform.position = fitPos;
            _wantTarget = Vector3.zero;
        }
    }

    /// <summary>
    /// Tells the camera a bearer was focused.
    /// Translates the (focus.x * 0.55, 0, focus.y * 0.55) rule. (y mapped to z in Unity)
    /// </summary>
    public void FocusOn(Vector2 boardPosition)
    {
        _wantTarget = new Vector3(boardPosition.x * 0.55f, 0f, boardPosition.y * 0.55f);
    }

    /// <summary>
    /// Clears focus, returning the eye to the center of the board.
    /// </summary>
    public void ClearFocus()
    {
        _wantTarget = Vector3.zero;
    }

    /// <summary>
    /// Registers a strike for the Killing Lean theatre.
    /// The camera will physically lean into the blow and shudder if stillness permits.
    /// </summary>
    public void NotifyStruck()
    {
        _struckAtMs = Time.time * 1000f;
    }

    private void LateUpdate()
    {
        float delta = Mathf.Min(Time.deltaTime, 0.05f);
        float nowMs = Time.time * 1000f;

        // 1. The Watchful Eye: Target resolution
        if (StillnessMode)
        {
            _currentTarget = _wantTarget;
        }
        else if (!IsPointerHeld)
        {
            float ease = 1f - Mathf.Pow(0.001f, delta);
            _currentTarget = Vector3.Lerp(_currentTarget, _wantTarget, ease);
        }

        Vector3 actualLookTarget = _currentTarget;
        Vector3 camPos = transform.position;

        // 2. The Killing Lean & Ground Shudder
        if (_struckAtMs > 0)
        {
            float age = (nowMs - _struckAtMs) / KILL_LEAN_MS;
            
            if (age >= 1f)
            {
                _struckAtMs = 0f;
            }
            else if (!StillnessMode)
            {
                float lean = Mathf.Sin(age * Mathf.PI) * 0.16f;
                Vector3 arm = camPos - _currentTarget;
                float reach = arm.magnitude;

                if (reach > 9.2f)
                {
                    arm *= ((reach - reach * lean * delta * 9f) / reach);
                    camPos = _currentTarget + arm;
                }

                // Ground Shudder
                if (age < 0.34f)
                {
                    float fall = 1f - (age / 0.34f);
                    float amp = 0.05f * fall * fall;
                    actualLookTarget.x += Mathf.Sin(nowMs * 0.028f) * amp;
                    actualLookTarget.z += Mathf.Cos(nowMs * 0.023f) * amp;
                }
            }
        }

        // Apply transforms
        transform.position = EnforceOrbitLimits(camPos, actualLookTarget);
        transform.LookAt(actualLookTarget);

        // 3. The Grade (Post-Processing Sync)
        SyncGrade();
    }

    /// <summary>
    /// Ensures the camera arm does not violate the minimum/maximum bounds or polar angles.
    /// </summary>
    private Vector3 EnforceOrbitLimits(Vector3 pos, Vector3 target)
    {
        Vector3 arm = pos - target;
        float distance = arm.magnitude;
        
        if (distance < 0.001f) return pos; // Prevent division by zero

        distance = Mathf.Clamp(distance, MIN_DIST, MAX_DIST);
        
        float polar = Mathf.Acos(arm.y / distance);
        polar = Mathf.Clamp(polar, MIN_POLAR, MAX_POLAR);

        // Reconstruct position after clamping
        float horizontalDist = distance * Mathf.Sin(polar);
        float yaw = Mathf.Atan2(arm.x, arm.z);

        Vector3 restrictedArm = new Vector3(
            Mathf.Sin(yaw) * horizontalDist,
            Mathf.Cos(polar) * distance,
            Mathf.Cos(yaw) * horizontalDist
        );

        return target + restrictedArm;
    }

    /// <summary>
    /// Binds the Grade (Post-processing) values defined in the Survey.
    /// Creates the Volume headlessly if one does not exist.
    /// </summary>
    private void EnsurePostProcessing()
    {
        _postVolume = gameObject.GetComponent<Volume>();
        if (_postVolume == null)
        {
            _postVolume = gameObject.AddComponent<Volume>();
            _postVolume.isGlobal = true;
            _postVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        }

        if (!_postVolume.profile.TryGet(out _bloom)) 
            _bloom = _postVolume.profile.Add<Bloom>(true);
            
        if (!_postVolume.profile.TryGet(out _dof)) 
            _dof = _postVolume.profile.Add<DepthOfField>(true);
            
        if (!_postVolume.profile.TryGet(out _vignette))
            _vignette = _postVolume.profile.Add<Vignette>(true);
            
        if (!_postVolume.profile.TryGet(out _grain))
            _grain = _postVolume.profile.Add<FilmGrain>(true);

        // Apply Survey Constants
        _bloom.threshold.Override(0.62f);
        _bloom.scatter.Override(0.28f); // Equivalent to luminanceSmoothing roughly in URP
        
        _vignette.center.Override(new Vector2(0.5f, 0.5f));
        _vignette.intensity.Override(0.42f); // offset approximation
        _vignette.smoothness.Override(0.5f); // darkness approximation

        _dof.mode.Override(DepthOfFieldMode.Bokeh);
    }

    private void SyncGrade()
    {
        if (_bloom != null)
        {
            _bloom.intensity.Override(StillnessMode ? 0.45f : 0.85f);
        }

        if (_grain != null)
        {
            _grain.intensity.Override(StillnessMode ? 0.02f : 0.045f);
        }

        if (_dof != null)
        {
            // Depth of field pulls bound to the strike / target distance
            float dist = Vector3.Distance(transform.position, _currentTarget);
            _dof.focusDistance.Override(dist);
            _dof.focalLength.Override(StillnessMode ? 35f : 50f);
        }
    }
}
