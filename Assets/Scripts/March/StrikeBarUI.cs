using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/*
 * ATTACH TO: A Canvas GameObject named "StrikeBarCanvas" in the Sundered Ford scene.
 *
 * This is THE core game hook of The March — the swing meter.
 * When a unit attacks, a horizontal bar appears with a marker sweeping left-to-right.
 * The player taps SPACE (or clicks) to stop the marker.
 *
 * Three bands on the bar:
 *   CLEAN (14% center) — full damage, triggers Flourish
 *   TRUE  (11% each side of CLEAN) — 70% damage, no Flourish
 *   TURNED (remainder) — 25% damage, attacker exposed
 *
 * Heavy weapons (axes, mauls) require a SECOND tap for Commitment (×0.6 to ×1.8).
 *
 * Defensive timing: When attacked, a reversed bar appears — tap in the 7% window
 * to Turn the blow (halves damage, staggers attacker if you have a shield).
 *
 * ASSIST MODES:
 *   Steady Hand — widens all bands 60%, slows sweep 35%
 *   The Reckoning — no timing, resolves on stats alone at flat ×0.72
 *
 * STILL AIR: No flashing > 3Hz. Color transitions are instant, no pulsing.
 *
 * INSPECTOR FIELDS:
 * - BarBackground: Image for the full bar
 * - CleanBandImage: Image for the CLEAN zone (gold)
 * - TrueBandLeftImage: Image for the left TRUE zone (steel)
 * - TrueBandRightImage: Image for the right TRUE zone (steel)
 * - TurnedBandLeftImage: Image for the left TURNED zone (dark)
 * - TurnedBandRightImage: Image for the right TURNED zone (dark)
 * - MarkerImage: Image for the sweeping marker (white line)
 * - CommitmentBar: Second bar for heavy weapon commitment (hidden for light weapons)
 * - CommitmentMarker: Marker for the commitment bar
 * - DefensiveBar: The defensive Turn bar (hidden unless defending)
 * - DefensiveMarker: Marker for the defensive bar
 * - DefensiveWindow: Image for the 7% Turn window on the defensive bar
 */

/// <summary>
/// The result of a strike bar tap.
/// </summary>
public enum StrikeBand { Clean, True, Turned, Miss }

/// <summary>
/// The result of a defensive Turn attempt.
/// </summary>
public enum DefensiveResult { Turned, Failed }

/// <summary>
/// Assist mode for the Strike timing bar.
/// </summary>
public enum AssistMode { None, SteadyHand, TheReckoning }

/// <summary>
/// The Strike timing bar — the core hook of The March.
/// A marker sweeps across bands; the player taps to stop it.
/// </summary>
public class StrikeBarUI : MonoBehaviour
{
    [Header("Band Images")]
    [SerializeField] private Image barBackground;
    [SerializeField] private Image cleanBandImage;      // Gold — CLEAN
    [SerializeField] private Image trueBandLeftImage;   // Steel — TRUE (left)
    [SerializeField] private Image trueBandRightImage;  // Steel — TRUE (right)
    [SerializeField] private Image turnedBandLeftImage; // Dark — TURNED (left)
    [SerializeField] private Image turnedBandRightImage;// Dark — TURNED (right)

    [Header("Marker")]
    [SerializeField] private Image markerImage;         // White vertical line

    [Header("Commitment (Heavy Weapons)")]
    [SerializeField] private GameObject commitmentBar;
    [SerializeField] private Image commitmentMarker;
    [SerializeField] private Image commitmentZoneImage;  // The target zone for good commitment

    [Header("Defensive Turn Bar")]
    [SerializeField] private GameObject defensiveBar;
    [SerializeField] private Image defensiveMarker;
    [SerializeField] private Image defensiveWindowImage; // The 7% Turn window

    [Header("Assist Mode")]
    [Tooltip("None = full timing. SteadyHand = wider bands, slower sweep. TheReckoning = no timing, flat 0.72x.")]
    public AssistMode assistMode = AssistMode.None;

    [Header("Colors (Still Air compliant — no flashing)")]
    [SerializeField] private Color cleanColor = new Color(0.85f, 0.7f, 0.2f, 1f);   // Gold
    [SerializeField] private Color trueColor = new Color(0.5f, 0.5f, 0.55f, 1f);     // Steel
    [SerializeField] private Color turnedColor = new Color(0.15f, 0.12f, 0.1f, 1f);  // Dark
    [SerializeField] private Color markerColor = new Color(0.95f, 0.95f, 0.95f, 1f); // White
    [SerializeField] private Color defenseWindowColor = new Color(0.2f, 0.6f, 0.9f, 1f); // Blue

    // Band proportions (from Design Bible)
    private const float CLEAN_WIDTH = 0.14f;      // 14% of bar
    private const float TRUE_WIDTH_EACH = 0.11f;  // 11% each side
    // TURNED = remainder: (1 - 0.14 - 0.22) / 2 = 0.32 each side

    // Defensive window
    private const float DEFENSIVE_WINDOW_BASE = 0.07f; // 7% base
    private const float DEFENSIVE_WINDOW_PER_GUARD = 0.002f; // +0.2% per Guard point

    // Commitment bar
    private const float COMMITMENT_GOOD_ZONE = 0.30f; // 30% center zone = ×1.0 to ×1.8
    private const float COMMITMENT_OK_ZONE = 0.55f;    // 55% = ×0.8 to ×1.0
    // Outside = ×0.6

    // State machine
    public enum BarState { Idle, Sweeping, CommitmentSweep, Defensive, Resolved }
    public BarState CurrentState { get; private set; } = BarState.Idle;

    // Sweep state
    private float _markerPosition = 0f;       // 0 to 1
    private float _sweepSpeed = 1f;           // Units per second
    private bool _sweepingRight = true;
    private bool _isHeavyWeapon = false;
    private int _attackerHand = 10;
    private int _defenderGuard = 0;

    // Results
    private StrikeBand _strikeResult;
    private float _commitmentMultiplier = 1.0f;
    private DefensiveResult _defensiveResult;

    // Callbacks
    private Action<StrikeBand, float> _onStrikeResolved; // (band, commitmentMultiplier)
    private Action<DefensiveResult> _onDefensiveResolved;

    // Still Air tracking
    private float _lastColorChangeTime = 0f;
    private const float MIN_COLOR_INTERVAL = 0.334f; // 3 Hz limit

    private void Update()
    {
        switch (CurrentState)
        {
            case BarState.Sweeping:
                UpdateSweep();
                break;
            case BarState.CommitmentSweep:
                UpdateCommitmentSweep();
                break;
            case BarState.Defensive:
                UpdateDefensiveSweep();
                break;
        }

        // Handle input
        if (CurrentState == BarState.Sweeping || CurrentState == BarState.CommitmentSweep || CurrentState == BarState.Defensive)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                HandleTap();
            }
        }
    }

    // -----------------------------------------------------------------------
    // OFFENSIVE STRIKE BAR
    // -----------------------------------------------------------------------

    /// <summary>
    /// Begins the offensive strike bar sweep for the attacking unit.
    /// </summary>
    /// <param name="attackerHand">The attacker's HAND stat (1-20). Lower = slower sweep = easier.</param>
    /// <param name="isHeavyWeapon">If true, a second tap for Commitment is required.</param>
    /// <param name="onResolved">Callback with (band, commitmentMultiplier) when the strike resolves.</param>
    public void BeginStrikeBar(int attackerHand, bool isHeavyWeapon, Action<StrikeBand, float> onResolved)
    {
        _attackerHand = Mathf.Clamp(attackerHand, 1, 20);
        _isHeavyWeapon = isHeavyWeapon;
        _onStrikeResolved = onResolved;

        // The Reckoning: no timing, flat ×0.72
        if (assistMode == AssistMode.TheReckoning)
        {
            CurrentState = BarState.Resolved;
            _onStrikeResolved?.Invoke(StrikeBand.True, 0.72f);
            ResetBar();
            return;
        }

        // Calculate sweep speed: HAND determines speed. Lower HAND = slower = easier.
        // Base speed: HAND 1 = 0.3/sec (very slow), HAND 20 = 2.0/sec (very fast)
        float baseSpeed = 0.3f + (_attackerHand - 1) * (1.7f / 19f);

        // Steady Hand: 35% slower
        if (assistMode == AssistMode.SteadyHand)
            baseSpeed *= 0.65f;

        _sweepSpeed = baseSpeed;
        _markerPosition = 0f;
        _sweepingRight = true;

        // Layout the bands
        LayoutBands();

        // Show bar
        SetBarVisible(true);
        SetDefensiveBarVisible(false);
        SetCommitmentBarVisible(false);

        CurrentState = BarState.Sweeping;
    }

    private void UpdateSweep()
    {
        float dt = Time.deltaTime;
        float move = _sweepSpeed * dt;

        if (_sweepingRight)
        {
            _markerPosition += move;
            if (_markerPosition >= 1f)
            {
                _markerPosition = 1f;
                _sweepingRight = false;
            }
        }
        else
        {
            _markerPosition -= move;
            if (_markerPosition <= 0f)
            {
                _markerPosition = 0f;
                _sweepingRight = true;
            }
        }

        UpdateMarkerPosition();
    }

    private void HandleTap()
    {
        if (CurrentState == BarState.Sweeping)
        {
            // First tap: determine the strike band
            _strikeResult = DetermineBand(_markerPosition);

            if (_isHeavyWeapon)
            {
                // Heavy weapon: proceed to commitment sweep
                BeginCommitmentSweep();
            }
            else
            {
                // Light weapon: resolve immediately with ×1.0 commitment
                CurrentState = BarState.Resolved;
                _onStrikeResolved?.Invoke(_strikeResult, 1.0f);
                ResetBar();
            }
        }
        else if (CurrentState == BarState.CommitmentSweep)
        {
            // Second tap: determine commitment multiplier
            _commitmentMultiplier = DetermineCommitment(_markerPosition);

            CurrentState = BarState.Resolved;
            _onStrikeResolved?.Invoke(_strikeResult, _commitmentMultiplier);
            ResetBar();
        }
        else if (CurrentState == BarState.Defensive)
        {
            // Defensive tap: check if within Turn window
            _defensiveResult = DetermineDefensiveResult(_markerPosition);

            CurrentState = BarState.Resolved;
            _onDefensiveResolved?.Invoke(_defensiveResult);
            ResetBar();
        }
    }

    /// <summary>
    /// Determines which band the marker landed in.
    /// </summary>
    private StrikeBand DetermineBand(float pos)
    {
        // Band layout (with Steady Hand widening):
        float cleanWidth = CLEAN_WIDTH;
        float trueWidthEach = TRUE_WIDTH_EACH;

        if (assistMode == AssistMode.SteadyHand)
        {
            cleanWidth *= 1.6f;
            trueWidthEach *= 1.6f;
        }

        // Center the CLEAN band
        float cleanStart = 0.5f - cleanWidth / 2f;
        float cleanEnd = 0.5f + cleanWidth / 2f;
        float trueLeftStart = cleanStart - trueWidthEach;
        float trueRightEnd = cleanEnd + trueWidthEach;

        if (pos >= cleanStart && pos <= cleanEnd)
            return StrikeBand.Clean;
        else if (pos >= trueLeftStart && pos <= trueRightEnd)
            return StrikeBand.True;
        else
            return StrikeBand.Turned;
    }

    // -----------------------------------------------------------------------
    // COMMITMENT BAR (Heavy Weapons)
    // -----------------------------------------------------------------------

    private void BeginCommitmentSweep()
    {
        _markerPosition = 0f;
        _sweepingRight = true;

        // Commitment sweep speed is slightly faster than strike
        float commitSpeed = _sweepSpeed * 1.2f;

        SetCommitmentBarVisible(true);

        CurrentState = BarState.CommitmentSweep;
    }

    private void UpdateCommitmentSweep()
    {
        float dt = Time.deltaTime;
        float move = _sweepSpeed * 1.2f * dt;

        if (_sweepingRight)
        {
            _markerPosition += move;
            if (_markerPosition >= 1f)
            {
                _markerPosition = 1f;
                _sweepingRight = false;
            }
        }
        else
        {
            _markerPosition -= move;
            if (_markerPosition <= 0f)
            {
                _markerPosition = 0f;
                _sweepingRight = true;
            }
        }

        UpdateCommitmentMarkerPosition();
    }

    /// <summary>
    /// Determines the commitment multiplier from the second tap position.
    /// Center 30% = ×1.0 to ×1.8 (scales with how close to dead center)
    /// Mid 55% = ×0.8 to ×1.0
    /// Outside = ×0.6
    /// </summary>
    private float DetermineCommitment(float pos)
    {
        float goodZone = COMMITMENT_GOOD_ZONE;
        float okZone = COMMITMENT_OK_ZONE;

        if (assistMode == AssistMode.SteadyHand)
        {
            goodZone *= 1.6f;
            okZone *= 1.3f;
        }

        float goodStart = 0.5f - goodZone / 2f;
        float goodEnd = 0.5f + goodZone / 2f;
        float okStart = 0.5f - okZone / 2f;
        float okEnd = 0.5f + okZone / 2f;

        if (pos >= goodStart && pos <= goodEnd)
        {
            // Scale from ×1.0 at edges to ×1.8 at dead center
            float distanceFromCenter = Mathf.Abs(pos - 0.5f) / (goodZone / 2f);
            return Mathf.Lerp(1.8f, 1.0f, distanceFromCenter);
        }
        else if (pos >= okStart && pos <= okEnd)
        {
            // Scale from ×0.8 at edges to ×1.0 at inner boundary
            float distanceFromCenter = Mathf.Abs(pos - 0.5f) / (okZone / 2f);
            return Mathf.Lerp(1.0f, 0.8f, distanceFromCenter);
        }
        else
        {
            // Outside: weak commitment
            return 0.6f;
        }
    }

    // -----------------------------------------------------------------------
    // DEFENSIVE TURN BAR
    // -----------------------------------------------------------------------

    /// <summary>
    /// Begins the defensive Turn bar. The defender must tap within a narrow window
    /// to Turn the blow (halve damage). The window is widened by the defender's Guard stat.
    /// </summary>
    /// <param name="defenderGuard">The defender's Guard stat (0-34). Widens the Turn window.</param>
    /// <param name="onResolved">Callback with the defensive result.</param>
    public void BeginDefensiveBar(int defenderGuard, Action<DefensiveResult> onResolved)
    {
        _defenderGuard = defenderGuard;
        _onDefensiveResolved = onResolved;

        // The Reckoning: no defensive timing
        if (assistMode == AssistMode.TheReckoning)
        {
            CurrentState = BarState.Resolved;
            _onDefensiveResolved?.Invoke(DefensiveResult.Failed);
            return;
        }

        // Calculate window width
        float windowWidth = DEFENSIVE_WINDOW_BASE + (_defenderGuard * DEFENSIVE_WINDOW_PER_GUARD);

        // Steady Hand: 60% wider
        if (assistMode == AssistMode.SteadyHand)
            windowWidth *= 1.6f;

        // Layout defensive window
        LayoutDefensiveWindow(windowWidth);

        // Sweep speed for defensive bar is faster (more challenging)
        float baseSpeed = 0.5f + (_attackerHand - 1) * (1.5f / 19f);
        if (assistMode == AssistMode.SteadyHand)
            baseSpeed *= 0.65f;

        _sweepSpeed = baseSpeed;
        _markerPosition = 0f;
        _sweepingRight = true;

        SetBarVisible(false);
        SetDefensiveBarVisible(true);
        SetCommitmentBarVisible(false);

        CurrentState = BarState.Defensive;
    }

    private void UpdateDefensiveSweep()
    {
        float dt = Time.deltaTime;
        float move = _sweepSpeed * dt;

        if (_sweepingRight)
        {
            _markerPosition += move;
            if (_markerPosition >= 1f)
            {
                // Missed the window — auto-fail
                CurrentState = BarState.Resolved;
                _onDefensiveResolved?.Invoke(DefensiveResult.Failed);
                ResetBar();
                return;
            }
        }

        UpdateDefensiveMarkerPosition();
    }

    /// <summary>
    /// Determines if the defensive tap was within the Turn window.
    /// </summary>
    private DefensiveResult DetermineDefensiveResult(float pos)
    {
        float windowWidth = DEFENSIVE_WINDOW_BASE + (_defenderGuard * DEFENSIVE_WINDOW_PER_GUARD);
        if (assistMode == AssistMode.SteadyHand)
            windowWidth *= 1.6f;

        float windowStart = 0.5f - windowWidth / 2f;
        float windowEnd = 0.5f + windowWidth / 2f;

        if (pos >= windowStart && pos <= windowEnd)
            return DefensiveResult.Turned;
        return DefensiveResult.Failed;
    }

    // -----------------------------------------------------------------------
    // LAYOUT & VISUALS
    // -----------------------------------------------------------------------

    private void LayoutBands()
    {
        float cleanWidth = CLEAN_WIDTH;
        float trueWidthEach = TRUE_WIDTH_EACH;

        if (assistMode == AssistMode.SteadyHand)
        {
            cleanWidth *= 1.6f;
            trueWidthEach *= 1.6f;
        }

        float turnedWidthEach = (1f - cleanWidth - trueWidthEach * 2f) / 2f;

        // CLEAN (center)
        if (cleanBandImage != null)
        {
            cleanBandImage.color = cleanColor;
            cleanBandImage.rectTransform.anchorMin = new Vector2(0.5f - cleanWidth / 2f, 0);
            cleanBandImage.rectTransform.anchorMax = new Vector2(0.5f + cleanWidth / 2f, 1);
            cleanBandImage.rectTransform.offsetMin = Vector2.zero;
            cleanBandImage.rectTransform.offsetMax = Vector2.zero;
        }

        // TRUE (left of CLEAN)
        float trueLeftStart = 0.5f - cleanWidth / 2f - trueWidthEach;
        if (trueBandLeftImage != null)
        {
            trueBandLeftImage.color = trueColor;
            trueBandLeftImage.rectTransform.anchorMin = new Vector2(trueLeftStart, 0);
            trueBandLeftImage.rectTransform.anchorMax = new Vector2(0.5f - cleanWidth / 2f, 1);
            trueBandLeftImage.rectTransform.offsetMin = Vector2.zero;
            trueBandLeftImage.rectTransform.offsetMax = Vector2.zero;
        }

        // TRUE (right of CLEAN)
        float trueRightEnd = 0.5f + cleanWidth / 2f + trueWidthEach;
        if (trueBandRightImage != null)
        {
            trueBandRightImage.color = trueColor;
            trueBandRightImage.rectTransform.anchorMin = new Vector2(0.5f + cleanWidth / 2f, 0);
            trueBandRightImage.rectTransform.anchorMax = new Vector2(trueRightEnd, 1);
            trueBandRightImage.rectTransform.offsetMin = Vector2.zero;
            trueBandRightImage.rectTransform.offsetMax = Vector2.zero;
        }

        // TURNED (left edge)
        if (turnedBandLeftImage != null)
        {
            turnedBandLeftImage.color = turnedColor;
            turnedBandLeftImage.rectTransform.anchorMin = new Vector2(0, 0);
            turnedBandLeftImage.rectTransform.anchorMax = new Vector2(trueLeftStart, 1);
            turnedBandLeftImage.rectTransform.offsetMin = Vector2.zero;
            turnedBandLeftImage.rectTransform.offsetMax = Vector2.zero;
        }

        // TURNED (right edge)
        if (turnedBandRightImage != null)
        {
            turnedBandRightImage.color = turnedColor;
            turnedBandRightImage.rectTransform.anchorMin = new Vector2(trueRightEnd, 0);
            turnedBandRightImage.rectTransform.anchorMax = new Vector2(1, 1);
            turnedBandRightImage.rectTransform.offsetMin = Vector2.zero;
            turnedBandRightImage.rectTransform.offsetMax = Vector2.zero;
        }

        // Marker
        if (markerImage != null)
        {
            markerImage.color = markerColor;
        }
    }

    private void LayoutDefensiveWindow(float windowWidth)
    {
        if (defensiveWindowImage != null)
        {
            defensiveWindowImage.color = defenseWindowColor;
            defensiveWindowImage.rectTransform.anchorMin = new Vector2(0.5f - windowWidth / 2f, 0);
            defensiveWindowImage.rectTransform.anchorMax = new Vector2(0.5f + windowWidth / 2f, 1);
            defensiveWindowImage.rectTransform.offsetMin = Vector2.zero;
            defensiveWindowImage.rectTransform.offsetMax = Vector2.zero;
        }

        if (defensiveMarker != null)
        {
            defensiveMarker.color = markerColor;
        }
    }

    private void UpdateMarkerPosition()
    {
        if (markerImage != null)
        {
            markerImage.rectTransform.anchorMin = new Vector2(_markerPosition - 0.005f, 0);
            markerImage.rectTransform.anchorMax = new Vector2(_markerPosition + 0.005f, 1);
            markerImage.rectTransform.offsetMin = Vector2.zero;
            markerImage.rectTransform.offsetMax = Vector2.zero;
        }
    }

    private void UpdateCommitmentMarkerPosition()
    {
        if (commitmentMarker != null)
        {
            commitmentMarker.rectTransform.anchorMin = new Vector2(_markerPosition - 0.005f, 0);
            commitmentMarker.rectTransform.anchorMax = new Vector2(_markerPosition + 0.005f, 1);
            commitmentMarker.rectTransform.offsetMin = Vector2.zero;
            commitmentMarker.rectTransform.offsetMax = Vector2.zero;
        }
    }

    private void UpdateDefensiveMarkerPosition()
    {
        if (defensiveMarker != null)
        {
            defensiveMarker.rectTransform.anchorMin = new Vector2(_markerPosition - 0.005f, 0);
            defensiveMarker.rectTransform.anchorMax = new Vector2(_markerPosition + 0.005f, 1);
            defensiveMarker.rectTransform.offsetMin = Vector2.zero;
            defensiveMarker.rectTransform.offsetMax = Vector2.zero;
        }
    }

    // -----------------------------------------------------------------------
    // VISIBILITY
    // -----------------------------------------------------------------------

    private void SetBarVisible(bool visible)
    {
        if (barBackground != null) barBackground.gameObject.SetActive(visible);
        if (cleanBandImage != null) cleanBandImage.gameObject.SetActive(visible);
        if (trueBandLeftImage != null) trueBandLeftImage.gameObject.SetActive(visible);
        if (trueBandRightImage != null) trueBandRightImage.gameObject.SetActive(visible);
        if (turnedBandLeftImage != null) turnedBandLeftImage.gameObject.SetActive(visible);
        if (turnedBandRightImage != null) turnedBandRightImage.gameObject.SetActive(visible);
        if (markerImage != null) markerImage.gameObject.SetActive(visible);
    }

    private void SetCommitmentBarVisible(bool visible)
    {
        if (commitmentBar != null) commitmentBar.SetActive(visible);
    }

    private void SetDefensiveBarVisible(bool visible)
    {
        if (defensiveBar != null) defensiveBar.SetActive(visible);
    }

    private void ResetBar()
    {
        CurrentState = BarState.Idle;
        SetBarVisible(false);
        SetCommitmentBarVisible(false);
        SetDefensiveBarVisible(false);
        _markerPosition = 0f;
    }

    /// <summary>
    /// Sets the assist mode for the Strike bar.
    /// </summary>
    public void SetAssistMode(AssistMode mode)
    {
        assistMode = mode;
    }

    /// <summary>
    /// Returns true if the bar is currently active (sweeping or defending).
    /// </summary>
    public bool IsActive => CurrentState == BarState.Sweeping ||
                            CurrentState == BarState.CommitmentSweep ||
                            CurrentState == BarState.Defensive;
}
