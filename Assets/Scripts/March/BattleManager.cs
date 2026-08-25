using System.Collections.Generic;
using UnityEngine;
using WarbornMarch.PhaseII;

/*
 * ATTACH TO: An empty GameObject named "BattleManager" in the Sundered Ford scene.
 *
 * This is the game loop orchestrator for THE WARBORN MARCH.
 * It manages the full battle flow:
 *   1. Muster Phase — deploy units, scout, set formation
 *   2. Strike Phase — turn-by-turn combat via rolling initiative
 *   3. Resolution — victory/defeat, marks, chronicle, Return Current
 *
 * The BattleManager wires together all 11 Phase II systems:
 *   MusterBoard, StrikeReckoner, MeterSet, IceHand, DigSystem, LegionSystem,
 *   DragonContract, NarratorCue, WarTableUI, ReturnCurrentBridge, StillAir
 *
 * INSPECTOR FIELDS:
 * - All system references (assigned by FordRaise.WireUpSystems or in Inspector)
 * - Battle parameters (max turns, ford hold duration, etc.)
 */

/// <summary>
/// The three phases of a battle at the Sundered Ford.
/// </summary>
public enum BattlePhase { Muster, Strike, Resolution, Complete }

/// <summary>
/// The outcome of a battle.
/// </summary>
public enum BattleOutcome { InProgress, Victory, VictoryAtCost, Defeat, Retreat }

/// <summary>
/// The game loop orchestrator. Wires all systems and drives the battle forward.
/// </summary>
public class BattleManager : MonoBehaviour
{
    [Header("System References")]
    public StrikeReckoner strikeReckoner;
    public MusterBoard musterBoard;
    public LegionSystem legionSystem;
    public DigSystem digSystem;
    public IceHand iceHand;
    public DragonContract dragonContract;
    public NarratorCue narratorCue;
    public WarTableUI warTableUI;
    public ReturnCurrentBridge returnCurrentBridge;
    public StillAir stillAir;
    public StrikeBarUI strikeBarUI;

    [Header("Battle Parameters")]
    [Tooltip("Maximum turns before the battle ends automatically.")]
    [SerializeField] private int maxTurns = 20;

    [Tooltip("Turns the player must hold the ford to win by objective.")]
    [SerializeField] private int fordHoldDuration = 8;

    [Tooltip("Turn before which the player may retreat without loss.")]
    [SerializeField] private int retreatDeadline = 5;

    [Tooltip("Magical cost for casting Ice.")]
    [SerializeField] private float iceCastCost = 20f;

    [Tooltip("Marks awarded per enemy wounded.")]
    [SerializeField] private int marksPerWound = 80;

    [Tooltip("Marks awarded for holding the ford (reduced spoils).")]
    [SerializeField] private int marksForFordHold = 200;

    [Tooltip("Marks required to hire the dragon.")]
    [SerializeField] private int dragonMarksCost = 500;

    [Header("State")]
    public BattlePhase currentPhase = BattlePhase.Muster;
    public BattleOutcome outcome = BattleOutcome.InProgress;
    public int currentTurn = 0;
    public int fordHeldTurns = 0;
    public int playerMarks = 0;
    public bool playerHasRetreated = false;

    // Unit tracking
    private List<UnitController> _playerUnits = new List<UnitController>();
    private List<UnitController> _enemyUnits = new List<UnitController>();
    private InitiativeQueue _initiativeQueue;
    private UnitController _activeUnit;
    private UnitController _selectedUnit;
    private bool _firstBloodOccurred = false;

    // Grid constants
    private const int GRID_WIDTH = 12;
    private const int GRID_DEPTH = 8;
    private const float TILE_SIZE = 1.5f;
    private const float GRID_ORIGIN_X = -9f;
    private const float GRID_ORIGIN_Z = -6f;

    // Callback for when the battle ends (used by UI)
    public System.Action<BattleOutcome> OnBattleEnded;

    /// <summary>
    /// The currently active unit (whose turn it is).
    /// </summary>
    public UnitController ActiveUnit => _activeUnit;

    /// <summary>
    /// The currently selected player unit.
    /// </summary>
    public UnitController SelectedUnit => _selectedUnit;

    /// <summary>
    /// All player units (including wounded).
    /// </summary>
    public IReadOnlyList<UnitController> PlayerUnits => _playerUnits.AsReadOnly();

    /// <summary>
    /// All enemy units (including wounded).
    /// </summary>
    public IReadOnlyList<UnitController> EnemyUnits => _enemyUnits.AsReadOnly();

    /// <summary>
    /// The initiative queue for display purposes.
    /// </summary>
    public InitiativeQueue Initiative => _initiativeQueue;

    private void Start()
    {
        // Initialize the initiative queue with a deterministic seed
        _initiativeQueue = new InitiativeQueue(seed: 42);

        // Trigger the opening narrator cue
        if (narratorCue != null)
        {
            narratorCue.TriggerCue(NarratorCue.NarratorEvent.Opening);
        }

        // The Return Current is bound BEFORE the first blow: the piece on the
        // board is named as a real ware in the armoury. In Phase II the player
        // fights as the Grogens, so she carries the Grogen steel.
        if (returnCurrentBridge != null)
        {
            returnCurrentBridge.CarryTheSteel(HouseName.Grogen);
        }

        // Begin in Muster phase — units are deployed but concealed
        currentPhase = BattlePhase.Muster;
    }

    // -----------------------------------------------------------------------
    // MUSTER PHASE
    // -----------------------------------------------------------------------

    /// <summary>
    /// Registers a unit with the BattleManager during the Muster phase.
    /// Called by FordRaise or scene setup.
    /// </summary>
    public void RegisterUnit(UnitController unit)
    {
        if (unit == null) return;

        if (unit.faction == HouseName.Grogen)
        {
            _playerUnits.Add(unit);
        }
        else
        {
            _enemyUnits.Add(unit);
        }

        // Register with LegionSystem if Daminari
        if (unit.faction == HouseName.Daminari && legionSystem != null)
        {
            LegionBanner banner = unit.GetComponent<LegionBanner>();
            if (banner != null)
            {
                legionSystem.RegisterBanner(banner);
            }
        }

        // Add to initiative queue
        _initiativeQueue.AddUnit(unit);
    }

    /// <summary>
    /// Executes the player's single scout action during Muster.
    /// Reveals one enemy banner's stats.
    /// </summary>
    public bool ScoutEnemy(UnitController target)
    {
        if (currentPhase != BattlePhase.Muster) return false;
        if (target == null || target.faction == HouseName.Grogen) return false;

        // Use the MusterBoard's scout system
        // Since MusterBoard uses instance IDs, we'd need to map this
        // For now, log the scout
        Debug.Log($"[BattleManager] Scouting enemy {target.unitName}.");
        return true;
    }

    /// <summary>
    /// Ends the Muster phase and begins the Strike (combat) phase.
    /// Called when the player stabs the dagger or presses the commence button.
    /// </summary>
    public void CommenceStrike()
    {
        if (currentPhase != BattlePhase.Muster) return;

        currentPhase = BattlePhase.Strike;
        currentTurn = 1;

        // Lock the WarTable
        if (warTableUI != null)
        {
            warTableUI.isBattleActive = true;
        }

        // Activate the first unit in the initiative queue
        AdvanceTurn();

        Debug.Log("[BattleManager] The Strike begins. The Sundered Ford runs red.");
    }

    // -----------------------------------------------------------------------
    // STRIKE PHASE — TURN MANAGEMENT
    // -----------------------------------------------------------------------

    /// <summary>
    /// Advances to the next unit's turn via the rolling initiative queue.
    /// </summary>
    public void AdvanceTurn()
    {
        if (currentPhase != BattlePhase.Strike) return;

        // Check win/loss before each turn
        if (CheckVictoryConditions()) return;

        // Get the next unit to act
        _activeUnit = _initiativeQueue.AdvanceToNext();

        if (_activeUnit == null)
        {
            // No units left — battle should end
            CheckVictoryConditions();
            return;
        }

        // Reset the active unit's acted flag
        _activeUnit.ResetTurn();

        // If it's a player unit, select it for control
        if (_activeUnit.faction == HouseName.Grogen)
        {
            _selectedUnit = _activeUnit;
        }
        else
        {
            // Enemy AI takes its turn
            StartCoroutine(EnemyTurnCoroutine(_activeUnit));
        }

        // Tick the dragon contract
        if (dragonContract != null)
        {
            dragonContract.TickPhase();
            if (!dragonContract.IsHired && dragonContract.RemainingPhases == 0)
            {
                narratorCue?.TriggerCue(NarratorCue.NarratorEvent.DragonPaid);
            }
        }
    }

    /// <summary>
    /// Called when the active unit has finished its action (moved, attacked, or rested).
    /// </summary>
    public void EndActiveUnitTurn()
    {
        if (_activeUnit == null) return;

        _activeUnit.HasActed = true;
        AdvanceTurn();
    }

    // -----------------------------------------------------------------------
    // COMBAT RESOLUTION
    // -----------------------------------------------------------------------

    /// <summary>
    /// Initiates an attack from the attacker to the defender.
    /// Triggers the Strike Bar UI for the player, or auto-resolves for AI.
    /// </summary>
    public void InitiateAttack(UnitController attacker, UnitController defender)
    {
        if (attacker == null || defender == null) return;
        if (attacker.IsWounded || defender.IsWounded) return;
        if (attacker.HasActed) return;

        // Check reach
        int distance = GridDistance(attacker, defender);
        if (distance > attacker.reach)
        {
            Debug.LogWarning($"[BattleManager] {attacker.unitName} cannot reach {defender.unitName} (distance {distance} > reach {attacker.reach}).");
            return;
        }

        // Check Might cost
        if (!attacker.TryExpendMight(10f))
        {
            Debug.LogWarning($"[BattleManager] {attacker.unitName} lacks Might to attack.");
            return;
        }

        // If player is attacking, show the Strike Bar
        if (attacker.faction == HouseName.Grogen && strikeBarUI != null)
        {
            strikeBarUI.BeginStrikeBar(attacker.hand, attacker.isHeavyWeapon, (band, commitment) =>
            {
                ResolveStrike(attacker, defender, band, commitment, false);
            });
        }
        else
        {
            // AI attack: auto-resolve with a simulated band
            StrikeBand aiBand = SimulateAIBand(attacker.hand);
            float aiCommitment = 1.0f;
            if (attacker.isHeavyWeapon)
                aiCommitment = SimulateAICommitment(attacker.hand);

            ResolveStrike(attacker, defender, aiBand, aiCommitment, false);
        }
    }

    /// <summary>
    /// Resolves a strike with the given band and commitment multiplier.
    /// Applies the full damage formula from the Design Bible.
    /// </summary>
    private void ResolveStrike(UnitController attacker, UnitController defender,
        StrikeBand band, float commitment, bool defenderTurned)
    {
        // THE WHOLE BLOW IS SETTLED IN ONE PLACE.
        // StrikeReckoner owns band, commitment, oath, ground, height, flank,
        // guard and the turn. Nothing here may multiply the result afterwards —
        // that was the old breach, and it double-counted guard and the band.
        StrikeReckoner.UnitStats attackerStats = attacker.GetCombatStats();
        StrikeReckoner.UnitStats defenderStats = defender.GetCombatStats();

        var strikeInput = new StrikeReckoner.StrikeInputs
        {
            Band = band,
            Commitment = attacker.isHeavyWeapon ? commitment : 1.0,
            Turned = defenderTurned,
            Ground = GetGroundFavour(attacker, defender),
            HeightTiers = attacker.heightTier - defender.heightTier,
            // THE FACING, at last. Front / Side / Back is read from where the
            // defender is looking, not assumed. The multipliers themselves are
            // still the reckoner's alone and are not touched here.
            Flank = Facing.Quarters(attacker, defender),
            WithoutTiming = false,
        };

        StrikeReckoner.StrikeResult blow = strikeReckoner.ReckonStrike(
            attackerStats, defenderStats, attacker.oath, defender.oath, strikeInput);

        int finalDamage = blow.Damage;

        // 13. Apply damage
        int guardDamage = Mathf.RoundToInt(finalDamage * 0.3f); // 30% of damage hits Guard
        defender.TakeDamage(finalDamage, guardDamage);

        // 14. Play attack animation
        attacker.PlayStrikeAnimation();

        // 15. Trigger Flourish on CLEAN strike
        if (blow.Flourish)
        {
            attacker.TriggerFlourish();
        }

        // 16. First Blood narrator cue
        if (!_firstBloodOccurred)
        {
            _firstBloodOccurred = true;
            narratorCue?.TriggerCue(NarratorCue.NarratorEvent.FirstBlood);
        }

        // 17. Check if defender was wounded
        if (defender.IsWounded)
        {
            _initiativeQueue.RemoveUnit(defender);
            playerMarks += marksPerWound;

            // Check if the dragon should be hireable
            if (playerMarks >= dragonMarksCost && dragonContract != null && !dragonContract.IsHired)
            {
                Debug.Log("[BattleManager] The dragon may now be hired.");
            }
        }

        // 17b. A defender that lives turns to meet the blow. This is why a
        // flank is worth taking once and never worth taking twice.
        if (!defender.IsWounded) defender.FaceToward(attacker);

        // 18. End the attacker's turn
        attacker.HasActed = true;

        Debug.Log($"[BattleManager] {attacker.unitName} struck {defender.unitName} for {finalDamage} damage. " +
                  $"Band: {blow.Band}, Commitment: {blow.Commitment:F2}, Gross: {blow.Gross}, " +
                  $"Turned: {blow.Turned}, Flourish: {blow.Flourish}. " +
                  $"Defender Vigour: {defender.Vigour}");
    }

    // -----------------------------------------------------------------------
    // ICE MAGIC
    // -----------------------------------------------------------------------

    /// <summary>
    /// Attempts to cast Ice magic from the given unit.
    /// </summary>
    public bool TryCastIce(UnitController caster)
    {
        if (caster == null || caster.IsWounded) return false;
        if (caster.faction != HouseName.Grogen) return false; // Phase II: player only

        if (!caster.TryCastIce(iceCastCost))
        {
            Debug.LogWarning($"[BattleManager] {caster.unitName} lacks Magical reserve to cast Ice.");
            return false;
        }

        // Play cast animation
        caster.PlayCastAnimation();

        // Trigger narrator cue
        narratorCue?.TriggerCue(NarratorCue.NarratorEvent.IceUnleashed);

        // Trigger IceHand visuals
        if (iceHand != null)
        {
            iceHand.TryCastIce();
        }

        Debug.Log($"[BattleManager] {caster.unitName} channeled Ice upon the Ford.");
        return true;
    }

    // -----------------------------------------------------------------------
    // DRAGON CONTRACT
    // -----------------------------------------------------------------------

    /// <summary>
    /// Attempts to hire the dragon with accumulated Marks.
    /// </summary>
    public bool TryHireDragon()
    {
        if (dragonContract == null) return false;
        if (playerMarks < dragonMarksCost) return false;

        dragonContract.EnactContract(playerMarks);
        playerMarks -= dragonMarksCost;

        Debug.Log("[BattleManager] Dragon hired. Ice will fall upon the Ford.");
        return true;
    }

    // -----------------------------------------------------------------------
    // DEEP DIG
    // -----------------------------------------------------------------------

    /// <summary>
    /// Checks for and triggers the Grogen Deep Dig ambush at the active zone.
    /// </summary>
    public void CheckDeepDig(int zoneIndex)
    {
        if (digSystem != null)
        {
            digSystem.CheckForAmbush(zoneIndex, currentTurn, maxTurns);
        }
    }

    // -----------------------------------------------------------------------
    // WIN / LOSS CHECKING
    // -----------------------------------------------------------------------

    /// <summary>
    /// Checks all victory and defeat conditions. Returns true if the battle has ended.
    /// </summary>
    private bool CheckVictoryConditions()
    {
        // Count active units
        int activePlayer = CountActiveUnits(_playerUnits);
        int activeEnemy = CountActiveUnits(_enemyUnits);

        // DEFEAT: All player units wounded
        if (activePlayer == 0)
        {
            EndBattle(BattleOutcome.Defeat);
            return true;
        }

        // VICTORY: All enemy units wounded or routed
        if (activeEnemy == 0)
        {
            // Check if it was at cost (all player units wounded except one)
            if (activePlayer == 1)
            {
                EndBattle(BattleOutcome.VictoryAtCost);
            }
            else
            {
                EndBattle(BattleOutcome.Victory);
            }
            return true;
        }

        // OBJECTIVE VICTORY: Ford held for N turns
        if (fordHeldTurns >= fordHoldDuration)
        {
            EndBattle(BattleOutcome.Victory);
            return true;
        }

        // MAX TURNS: Battle ends — check who holds the field
        if (currentTurn >= maxTurns)
        {
            if (activePlayer > activeEnemy)
            {
                EndBattle(BattleOutcome.Victory);
            }
            else if (activePlayer < activeEnemy)
            {
                EndBattle(BattleOutcome.Defeat);
            }
            else
            {
                // Equal — stalemate counts as defeat for the attacker
                EndBattle(BattleOutcome.Defeat);
            }
            return true;
        }

        return false;
    }

    /// <summary>
    /// Ends the battle with the given outcome.
    /// </summary>
    private void EndBattle(BattleOutcome result)
    {
        outcome = result;
        currentPhase = BattlePhase.Resolution;

        // Trigger appropriate narrator cue
        if (narratorCue != null)
        {
            switch (result)
            {
                case BattleOutcome.Victory:
                    narratorCue.TriggerCue(NarratorCue.NarratorEvent.Victory);
                    break;
                case BattleOutcome.VictoryAtCost:
                    narratorCue.TriggerCue(NarratorCue.NarratorEvent.VictoryAtCost);
                    break;
                case BattleOutcome.Defeat:
                    narratorCue.TriggerCue(NarratorCue.NarratorEvent.Defeat);
                    break;
            }
        }

        // Report victory to the Return Current bridge
        if (result == BattleOutcome.Victory || result == BattleOutcome.VictoryAtCost)
        {
            if (returnCurrentBridge != null)
            {
                returnCurrentBridge.ReportVictory();
            }

            // Award marks
            if (result == BattleOutcome.Victory)
                playerMarks += marksForFordHold;
            else
                playerMarks += marksForFordHold / 2;
        }

        // Handle permanent loss on defeat
        if (result == BattleOutcome.Defeat && !playerHasRetreated)
        {
            // One random wounded player unit is lost forever
            List<UnitController> wounded = _playerUnits.FindAll(u => u.IsWounded);
            if (wounded.Count > 0)
            {
                UnitController lost = wounded[Random.Range(0, wounded.Count)];
                Debug.Log($"[BattleManager] {lost.unitName} is lost forever. They join the Hall of the Fallen.");
            }
        }

        // Unlock the WarTable
        if (warTableUI != null)
        {
            warTableUI.isBattleActive = false;
        }

        currentPhase = BattlePhase.Complete;

        Debug.Log($"[BattleManager] Battle ended: {result}. Marks earned: {playerMarks}.");
        OnBattleEnded?.Invoke(result);
    }

    /// <summary>
    /// Player retreats before the deadline. No spoils, no permanent losses.
    /// </summary>
    public void Retreat()
    {
        if (currentPhase != BattlePhase.Strike) return;
        if (currentTurn >= retreatDeadline)
        {
            Debug.LogWarning("[BattleManager] Cannot retreat — deadline has passed.");
            return;
        }

        playerHasRetreated = true;
        EndBattle(BattleOutcome.Retreat);
    }

    // -----------------------------------------------------------------------
    // ENEMY AI
    // -----------------------------------------------------------------------

    /// <summary>
    /// Simple enemy AI: moves toward nearest player unit and attacks if in reach.
    /// </summary>
    private System.Collections.IEnumerator EnemyTurnCoroutine(UnitController enemy)
    {
        // Wait a moment for visual clarity
        yield return new WaitForSeconds(0.5f);

        // Find nearest player unit
        UnitController nearestPlayer = FindNearestUnit(enemy, _playerUnits);

        if (nearestPlayer != null)
        {
            int distance = GridDistance(enemy, nearestPlayer);

            if (distance <= enemy.reach)
            {
                // Attack!
                InitiateAttack(enemy, nearestPlayer);
            }
            else
            {
                // Move toward the nearest player
                MoveToward(enemy, nearestPlayer);

                // After moving, check if can attack
                distance = GridDistance(enemy, nearestPlayer);
                if (distance <= enemy.reach && !enemy.HasActed)
                {
                    yield return new WaitForSeconds(0.3f);
                    InitiateAttack(enemy, nearestPlayer);
                }
            }
        }

        yield return new WaitForSeconds(0.3f);

        // End enemy turn
        if (!enemy.HasActed)
            enemy.HasActed = true;

        AdvanceTurn();
    }

    /// <summary>
    /// Simulates an AI strike band based on the unit's HAND stat.
    /// Lower HAND = better chance of CLEAN.
    /// </summary>
    private StrikeBand SimulateAIBand(int hand)
    {
        // Base chances scaled by HAND
        float cleanChance = Mathf.Max(0.05f, 0.25f - (hand * 0.01f));
        float trueChance = Mathf.Max(0.15f, 0.40f - (hand * 0.015f));

        float roll = Random.value;

        if (roll < cleanChance)
            return StrikeBand.Clean;
        else if (roll < cleanChance + trueChance)
            return StrikeBand.True;
        else
            return StrikeBand.Turned;
    }

    /// <summary>
    /// Simulates an AI commitment roll for heavy weapons.
    /// </summary>
    private float SimulateAICommitment(int hand)
    {
        float baseCommit = Mathf.Max(0.6f, 1.8f - (hand * 0.05f));
        return baseCommit + Random.Range(-0.2f, 0.2f);
    }

    // -----------------------------------------------------------------------
    // MOVEMENT HELPERS
    // -----------------------------------------------------------------------

    /// <summary>
    /// Moves a unit toward a target unit, up to the mover's Stride.
    /// </summary>
    private void MoveToward(UnitController mover, UnitController target)
    {
        int dx = target.gridX - mover.gridX;
        int dy = target.gridY - mover.gridY;
        int distance = Mathf.Abs(dx) + Mathf.Abs(dy);

        int moveDistance = Mathf.Min(distance, mover.stride);

        // Move in the direction with the largest delta first
        int newX = mover.gridX;
        int newY = mover.gridY;

        int remaining = moveDistance;
        while (remaining > 0)
        {
            if (Mathf.Abs(target.gridX - newX) > Mathf.Abs(target.gridY - newY))
            {
                newX += Mathf.Sign(target.gridX - newX) > 0 ? 1 : -1;
            }
            else if (target.gridY != newY)
            {
                newY += Mathf.Sign(target.gridY - newY) > 0 ? 1 : -1;
            }
            else
            {
                break;
            }
            remaining--;
        }

        Vector3 worldPos = GridToWorld(newX, newY);
        mover.MoveTo(newX, newY, worldPos);
    }

    /// <summary>
    /// Moves the selected player unit to a grid position.
    /// </summary>
    public bool MovePlayerUnit(int gridX, int gridY)
    {
        if (_selectedUnit == null || _selectedUnit.IsWounded) return false;
        if (_selectedUnit.faction != HouseName.Grogen) return false;
        if (_selectedUnit.HasActed) return false;

        Vector3 worldPos = GridToWorld(gridX, gridY);
        bool moved = _selectedUnit.MoveTo(gridX, gridY, worldPos);
        if (moved)
        {
            Debug.Log($"[BattleManager] {_selectedUnit.unitName} moved to ({gridX}, {gridY}).");
        }
        return moved;
    }

    /// <summary>
    /// Selects a player unit for control.
    /// </summary>
    public void SelectUnit(UnitController unit)
    {
        if (unit == null || unit.IsWounded) return;
        if (unit.faction != HouseName.Grogen) return;
        _selectedUnit = unit;
        Debug.Log($"[BattleManager] Selected {unit.unitName}.");
    }

    // -----------------------------------------------------------------------
    // CALCULATION HELPERS
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns the oath advantage multiplier.
    /// The Sworn beats The Damned, The Unsworn beats The Sworn, The Damned beats The Unsworn.
    /// </summary>
    private float GetOathMultiplier(Oath attacker, Oath defender)
    {
        bool beats = false;
        switch (attacker)
        {
            case Oath.Sworn:
                beats = defender == Oath.Damned;
                break;
            case Oath.Unsworn:
                beats = defender == Oath.Sworn;
                break;
            case Oath.Damned:
                beats = defender == Oath.Unsworn;
                break;
        }
        return beats ? 1.15f : 1.0f;
    }

    /// <summary>
    /// Returns terrain advantage for the attacker.
    /// Simplified for Phase II: Grogens get +0.10 on the west bank (digging),
    /// Daminari get +0.10 on the east bank (high ground).
    /// </summary>
    private StrikeReckoner.GroundFavour GetGroundFavour(UnitController attacker, UnitController defender)
    {
        // The browser field reads ground as a multiplier on the blow (1.1 for
        // the attacker's ground, 0.92 for the defender's), never as an additive
        // advantage. Grogens own the west bank; Daminari hold the east.
        bool attackerOnOwnBank =
            (attacker.faction == HouseName.Grogen && attacker.gridX < GRID_WIDTH / 2) ||
            (attacker.faction == HouseName.Daminari && attacker.gridX >= GRID_WIDTH / 2);

        bool defenderOnOwnBank =
            (defender.faction == HouseName.Grogen && defender.gridX < GRID_WIDTH / 2) ||
            (defender.faction == HouseName.Daminari && defender.gridX >= GRID_WIDTH / 2);

        if (attackerOnOwnBank && !defenderOnOwnBank) return StrikeReckoner.GroundFavour.Attacker;
        if (defenderOnOwnBank && !attackerOnOwnBank) return StrikeReckoner.GroundFavour.Defender;
        return StrikeReckoner.GroundFavour.Neither;
    }

    /// <summary>
    /// Returns the height multiplier: +12% per tier of advantage, clamped to ±24%.
    /// </summary>
    private float GetHeightMultiplier(UnitController attacker, UnitController defender)
    {
        int tierDiff = attacker.heightTier - defender.heightTier;
        float multiplier = 1.0f + (0.12f * tierDiff);
        return Mathf.Clamp(multiplier, 0.76f, 1.24f);
    }

    /// <summary>
    /// Returns the Manhattan distance between two units on the grid.
    /// </summary>
    private int GridDistance(UnitController a, UnitController b)
    {
        return Mathf.Abs(a.gridX - b.gridX) + Mathf.Abs(a.gridY - b.gridY);
    }

    /// <summary>
    /// Converts grid coordinates to world position.
    /// </summary>
    public Vector3 GridToWorld(int gridX, int gridY)
    {
        return new Vector3(
            GRID_ORIGIN_X + gridX * TILE_SIZE,
            0f,
            GRID_ORIGIN_Z + gridY * TILE_SIZE
        );
    }

    /// <summary>
    /// Finds the nearest active unit from a list to the given source.
    /// </summary>
    private UnitController FindNearestUnit(UnitController source, List<UnitController> candidates)
    {
        UnitController nearest = null;
        int minDist = int.MaxValue;

        foreach (var unit in candidates)
        {
            if (unit == null || unit.IsWounded) continue;
            int dist = GridDistance(source, unit);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = unit;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Counts active (non-wounded) units in a list.
    /// </summary>
    private int CountActiveUnits(List<UnitController> units)
    {
        int count = 0;
        foreach (var unit in units)
        {
            if (unit != null && !unit.IsWounded) count++;
        }
        return count;
    }

    /// <summary>
    /// Increments the turn counter and checks for ford hold victory.
    /// </summary>
    public void IncrementTurn()
    {
        currentTurn++;

        // Check if player holds the ford (all player units on east bank)
        bool holdsFord = true;
        foreach (var unit in _playerUnits)
        {
            if (unit != null && !unit.IsWounded && unit.gridX < GRID_WIDTH / 2)
            {
                holdsFord = false;
                break;
            }
        }

        if (holdsFord && CountActiveUnits(_enemyUnits) > 0)
        {
            fordHeldTurns++;
        }
        else
        {
            fordHeldTurns = 0;
        }
    }
}
