using UnityEngine;
using WarbornMarch.PhaseII;

/*
 * ATTACH TO: Each knight/unit prefab in the scene.
 *
 * This is the glue that turns a decorative knight model into a real, playable game unit.
 * It wires MeterSet (stats), WarbornAnimator (animations), and connects to
 * BattleManager, StrikeReckoner, LegionSystem, StillAir, and MusterBoard.
 *
 * INSPECTOR FIELDS:
 * - Banner House: Grogen or Daminari (determines recovery curves)
 * - Oath: Sworn, Unsworn, or Damned (affinity system)
 * - Unit Class: Housecarl, Spear-Levy, Knight-Errant, etc.
 * - Max Vigour/Might/Guard: Starting stats
 * - Starting Magical: Ice magic capacity (sealed at init)
 * - Hand: Strike-bar sweep speed (lower = slower = easier) and initiative
 * - Reach: Tiles the unit may strike from (1-4)
 * - Stride: Tiles moved per turn (2-7)
 * - Will: Resistance to fear/rout/Damned effects (0-30)
 * - Weapon Edge: Bonus damage from the carried SKU weapon
 * - Is Heavy Weapon: If true, Strike bar requires second tap for Commitment
 */

/// <summary>
/// The three Oaths of the March — a ring, not a ladder.
/// Each beats one and loses to one.
/// </summary>
public enum Oath { Sworn, Unsworn, Damned }

/// <summary>
/// The twelve unit classes of the March (4 per Oath).
/// Each has a distinct tactical identity and one signature Flourish.
/// </summary>
public enum UnitClass
{
    // The Sworn
    Housecarl,
    SpearLevy,
    KnightErrant,
    Standard,
    // The Unsworn
    Raider,
    Hunter,
    Scout,
    Herald,
    // The Damned
    Berserker,
    Oathbreaker,
    FeverTouched,
    Revenant
}

/// <summary>
/// Makes a knight model into a real, playable game unit with stats, movement, and death.
/// </summary>
[RequireComponent(typeof(MeterSet))]
public class UnitController : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("Grogen or Daminari — determines recovery curves and deployment zone.")]
    public HouseName faction;

    [Tooltip("Sworn, Unsworn, or Damned — the affinity ring.")]
    public Oath oath;

    [Tooltip("Housecarl, Spear-Levy, Knight-Errant, etc. — determines Flourish.")]
    public UnitClass unitClass;

    [Header("Combat Stats (from the Design Bible)")]
    [Tooltip("Strike-bar sweep speed. Lower = slower = easier. Also drives initiative order.")]
    [Range(1, 20)] public int hand = 10;

    [Tooltip("Tiles the unit may strike from.")]
    [Range(1, 4)] public int reach = 1;

    [Tooltip("Tiles moved per turn.")]
    [Range(2, 7)] public int stride = 3;

    [Tooltip("Resistance to fear, rout, and Damned effects.")]
    [Range(0, 30)] public int will = 10;

    [Tooltip("Bonus damage from the carried Return Current weapon (the real SKU).")]
    public float weaponEdge = 0f;

    [Tooltip("Heavy weapons (axes, mauls, greatswords) require a second tap for Commitment.")]
    public bool isHeavyWeapon = false;

    [Header("Grid Position")]
    [Tooltip("Current grid column (0-based).")]
    public int gridX;

    [Tooltip("Current grid row (0-based).")]
    public int gridY;

    [Tooltip("Height tier (0-3). Affects damage: +12% per tier advantage.")]
    [Range(0, 3)] public int heightTier = 0;

    [Tooltip("The quarter this unit is looking at. The reckoning reads it to tell a blow to the shield from a blow to the back.")]
    public Quarter facing = Quarter.North;

    [Header("State")]
    [Tooltip("True when Vigour reaches 0. Unit is removed from the field.")]
    public bool IsWounded { get; private set; }

    [Tooltip("True when the unit has acted this turn (moved or attacked).")]
    public bool HasActed { get; set; }

    [Tooltip("The unit's display name for the Chronicle.")]
    public string unitName = "Unnamed";

    // Cached components
    private MeterSet _meters;
    private WarbornAnimator _animator;
    private LegionBanner _legionBanner; // Only for Daminari

    // The grid position in world space
    private Vector3 _worldPos;

    /// <summary>
    /// The unit's current Vigour (health). 0 = wounded.
    /// </summary>
    public float Vigour => _meters != null ? _meters.Vigour : 0f;

    /// <summary>
    /// The unit's current Might (attack power).
    /// </summary>
    public float Might => _meters != null ? _meters.Might : 0f;

    /// <summary>
    /// The unit's current Guard (damage reduction).
    /// </summary>
    public float Guard => _meters != null ? _meters.Guard : 0f;

    /// <summary>
    /// The unit's current Magical (Ice magic capacity).
    /// </summary>
    public float Magical => _meters != null ? _meters.Magical : 0f;

    /// <summary>
    /// Effective Guard, accounting for Legion adjacency pooling (Daminari only).
    /// </summary>
    public float EffectiveGuard
    {
        get
        {
            float held = HeldLineGuard;
            if (_legionBanner != null && LegionSystem.Instance != null)
            {
                return LegionSystem.Instance.GetEffectiveGuard(_legionBanner) + held;
            }
            return Guard + held;
        }
    }

    private void Awake()
    {
        _meters = GetComponent<MeterSet>();
        _animator = GetComponent<WarbornAnimator>();

        // Daminari units participate in the Legion shield-wall
        if (faction == HouseName.Daminari)
        {
            _legionBanner = GetComponent<LegionBanner>();
            if (_legionBanner == null)
            {
                _legionBanner = gameObject.AddComponent<LegionBanner>();
            }
        }
    }

    private void Start()
    {
        // Register with StillAir for photosafety
        if (StillAir.Instance != null)
        {
            Animator anim = GetComponent<Animator>();
            if (anim != null) StillAir.Instance.RegisterAnimator(anim);

            ParticleSystem ps = GetComponentInChildren<ParticleSystem>();
            if (ps != null) StillAir.Instance.RegisterParticleSystem(ps);
        }

        _worldPos = transform.position;
    }

    /// <summary>
    /// Moves the unit to a new grid position over Stride tiles.
    /// Returns true if the move is valid (within Stride range).
    /// </summary>
    public bool MoveTo(int newGridX, int newGridY, Vector3 newWorldPos)
    {
        int dx = Mathf.Abs(newGridX - gridX);
        int dy = Mathf.Abs(newGridY - gridY);
        int distance = dx + dy; // Manhattan distance on the grid

        if (distance > stride)
        {
            Debug.LogWarning($"[UnitController] {unitName} cannot move {distance} tiles (Stride = {stride}).");
            return false;
        }

        if (IsWounded)
        {
            Debug.LogWarning($"[UnitController] {unitName} is wounded and cannot move.");
            return false;
        }

        // A soldier looks where he walks. Facing is set BEFORE the position is
        // taken, from the old tile to the new, so a unit that steps sideways
        // turns its shoulder to whatever it left behind.
        if (newGridX != gridX || newGridY != gridY)
            FaceQuarter(Facing.Toward(gridX, gridY, newGridX, newGridY));

        TilesCrossedThisTurn += distance;
        gridX = newGridX;
        gridY = newGridY;
        _worldPos = newWorldPos;
        transform.position = newWorldPos;

        if (_animator != null)
        {
            _animator.SetLocomotion(1f);
        }

        HasActed = true;
        return true;
    }

    /// <summary>
    /// Turns the unit to a quarter, in the ledger and in the body both.
    /// </summary>
    public void FaceQuarter(Quarter q)
    {
        facing = q;
        transform.rotation = Facing.Rotation(q);
    }

    /// <summary>
    /// Turns to look at another unit. A defender that survives a blow turns to
    /// meet it, which is why a flank is worth taking but never worth repeating.
    /// </summary>
    public void FaceToward(UnitController other)
    {
        if (other == null) return;
        FaceQuarter(Facing.Toward(gridX, gridY, other.gridX, other.gridY));
    }

    /// <summary>
    /// Builds a StrikeReckoner.UnitStats from this unit's current meters.
    /// </summary>
    public StrikeReckoner.UnitStats GetCombatStats()
    {
        return new StrikeReckoner.UnitStats
        {
            Vigour = Mathf.RoundToInt(Vigour),
            Might = Mathf.RoundToInt(Might + weaponEdge),
            Guard = Mathf.RoundToInt(EffectiveGuard),
            Magical = Mathf.RoundToInt(Magical)
        };
    }

    /// <summary>
    /// Applies strike damage to this unit's meters.
    /// </summary>
    public void TakeDamage(int vigourDamage, int guardDamage = 0)
    {
        if (_meters == null || IsWounded) return;

        _meters.SufferStrike(vigourDamage, guardDamage);

        if (_animator != null)
        {
            _animator.PlayStruck();
        }

        if (Vigour <= 0f)
        {
            Wound();
        }
    }

    /// <summary>
    /// Marks the unit as wounded (dead for the remainder of this battle).
    /// Plays the death animation and disables the unit.
    /// </summary>
    public void Wound()
    {
        IsWounded = true;
        HasActed = true;

        if (_animator != null)
        {
            _animator.PlayFallen(true);
        }

        // Unregister from Legion system if Daminari
        if (_legionBanner != null && LegionSystem.Instance != null)
        {
            LegionSystem.Instance.UnregisterBanner(_legionBanner);
        }

        // Darken the tile (per design bible: "its tile darkens permanently")
        // This is a visual effect that can be handled by a TileDarkener component
        Debug.Log($"[UnitController] {unitName} has fallen. Their blade returns to the armoury.");
    }

    /// <summary>
    /// Rests for the given duration, recovering meters per faction-specific rates.
    /// </summary>
    public void Rest(float restSeconds)
    {
        if (_meters != null && !IsWounded)
        {
            _meters.Rest(restSeconds);
        }

        if (_animator != null)
        {
            _animator.SetLocomotion(0f);
        }

        HasActed = true;
    }

    /// <summary>
    /// Attempts to cast Ice magic. Returns true if the cast succeeds.
    /// </summary>
    public bool TryCastIce(float cost)
    {
        if (_meters == null || IsWounded) return false;
        return _meters.TryCastIce(cost);
    }

    /// <summary>
    /// Expends Might for an attack. Returns true if sufficient Might is available.
    /// </summary>
    public bool TryExpendMight(float cost)
    {
        if (_meters == null || IsWounded) return false;
        return _meters.TryExpendMight(cost);
    }

    /// <summary>
    /// Triggers this unit's Flourish (signature ability), only on a CLEAN strike.
    /// </summary>
    public void TriggerFlourish()
    {
        switch (unitClass)
        {
            case UnitClass.Housecarl:
                // Hold the Line — adjacent allies gain +6 Guard until this unit's next turn
                Debug.Log($"[Flourish] {unitName} holds the line. Adjacent allies gain +6 Guard.");
                foreach (var ally in AdjacentAllies()) ally.GrantHeldLine(6f);
                break;

            case UnitClass.SpearLevy:
                // Set Against Charge — free strike on any unit that closes to melee
                Debug.Log($"[Flourish] {unitName} sets against charge. Free strike on approaching enemy.");
                SetAgainstCharge = true;
                break;

            case UnitClass.KnightErrant:
                // The Charge — damage scales +9% per tile crossed before the blow
                Debug.Log($"[Flourish] {unitName} charges. Damage scales with distance.");
                // The charge is spent as weapon edge, never as a new multiplier
                // inside the reckoning. The sealed formula is not touched.
                float edge = Mathf.Min(TilesCrossedThisTurn, 4) * 0.9f;
                weaponEdge += edge;
                _chargeEdge += edge;
                break;

            case UnitClass.Standard:
                // Rally — adjacent allies recover Will
                Debug.Log($"[Flourish] {unitName} rallies the line. Allies recover Will.");
                foreach (var ally in AdjacentAllies()) ally.RecoverWill(3);
                break;

            default:
                Debug.Log($"[Flourish] {unitName} flourishes.");
                break;
        }

        if (_animator != null)
        {
            _animator.PlayStrike();
        }
    }

    /// <summary>
    /// Plays the strike animation.
    /// </summary>
    public void PlayStrikeAnimation()
    {
        if (_animator != null)
        {
            _animator.PlayStrike();
        }
    }

    /// <summary>
    /// Plays the cast animation (for Ice magic).
    /// </summary>
    public void PlayCastAnimation()
    {
        if (_animator != null)
        {
            _animator.PlayCast();
        }
    }

    /// <summary>
    /// Resets the unit's acted flag for a new turn.
    /// </summary>
    public void ResetTurn()
    {
        HasActed = false;

        // Everything a flourish lent is taken back at the turn's end. A
        // Housecarl must hold the line again to keep holding it, and a charge
        // is worth only the ground it actually crossed.
        HeldLineGuard = 0f;
        TilesCrossedThisTurn = 0;
        weaponEdge -= _chargeEdge;
        _chargeEdge = 0f;
    }

    /// <summary>
    /// Returns true if this unit can still act (not wounded, not yet acted).
    /// </summary>
    public bool CanAct => !IsWounded && !HasActed;

    /// <summary>
    /// Returns true if the unit is on height tier 2 or higher (high ground).
    /// </summary>
    public bool IsOnHighGround => heightTier >= 2;

    // -----------------------------------------------------------------------
    // THE FLOURISHES, BODIED
    //
    // Every one of these spends itself OUTSIDE the reckoning — on guard, on
    // will, on weapon edge — never as a fresh multiplier inside it. The sealed
    // formula stays sealed; only its inputs move.
    // -----------------------------------------------------------------------

    /// <summary>Guard lent by a Housecarl's held line. Falls away on the unit's next turn.</summary>
    public float HeldLineGuard { get; private set; }

    /// <summary>True while a Spear Levy is set, owed one free strike at the next unit that closes.</summary>
    public bool SetAgainstCharge { get; set; }

    /// <summary>Tiles walked this turn, read by the Knight Errant's charge.</summary>
    public int TilesCrossedThisTurn { get; private set; }

    /// <summary>Weapon edge added by a charge, given back when the turn ends.</summary>
    private float _chargeEdge;

    public void GrantHeldLine(float guard)
    {
        HeldLineGuard += guard;
    }

    public void RecoverWill(int amount)
    {
        will = Mathf.Clamp(will + amount, 0, 30);
    }

    /// <summary>Every living ally on a tile orthogonally touching this one.</summary>
    public System.Collections.Generic.List<UnitController> AdjacentAllies()
    {
        var found = new System.Collections.Generic.List<UnitController>();
        foreach (var other in FindObjectsByType<UnitController>(FindObjectsSortMode.None))
        {
            if (other == this || other.IsWounded) continue;
            if (other.faction != faction) continue;
            if (Mathf.Abs(other.gridX - gridX) + Mathf.Abs(other.gridY - gridY) == 1)
                found.Add(other);
        }
        return found;
    }
}
