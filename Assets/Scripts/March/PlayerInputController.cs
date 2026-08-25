using UnityEngine;
using WarbornMarch.PhaseII;

/*
 * ATTACH TO: The main Camera GameObject or a dedicated "InputController" GameObject.
 *
 * Handles all player input and routes it to the BattleManager based on the current phase.
 *
 * CONTROLS:
 *   Mouse Left Click — Select unit / Move to tile / Attack target
 *   Space — Confirm action / Stop Strike Bar (handled by StrikeBarUI)
 *   Tab — Cycle through player units
 *   Q — Deep Dig (Grogen signature)
 *   W — Cast Ice
 *   E — Charge (Knight-Errant flourish)
 *   R — Rest (recover meters)
 *   Esc — Withdraw/Retreat menu
 *   F — Toggle Steady Hand assist mode
 *   H — Toggle The Reckoning assist mode
 *
 * INSPECTOR FIELDS:
 * - BattleManager: Reference to the BattleManager
 * - Camera: The camera used for raycasting (defaults to Camera.main)
 * - TileLayerMask: Layer mask for clickable tiles/ground
 * - UnitLayerMask: Layer mask for clickable units
 */

/// <summary>
/// Handles all player input and routes it to the BattleManager.
/// </summary>
public class PlayerInputController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private StrikeBarUI strikeBarUI;

    [Header("Layer Masks")]
    [Tooltip("Layer mask for clickable ground/tiles.")]
    [SerializeField] private LayerMask tileLayerMask = ~0;

    [Tooltip("Layer mask for clickable units.")]
    [SerializeField] private LayerMask unitLayerMask = ~0;

    [Header("Visual Feedback")]
    [Tooltip("Prefab for the movement range indicator.")]
    [SerializeField] private GameObject moveIndicatorPrefab;

    [Tooltip("Prefab for the attack range indicator.")]
    [SerializeField] private GameObject attackIndicatorPrefab;

    [Tooltip("Material for selected unit highlight.")]
    [SerializeField] private Material selectedHighlightMaterial;

    // State
    private enum InputMode { Select, Move, AttackTarget, Wait }
    private InputMode _inputMode = InputMode.Select;
    private UnitController _hoveredUnit;
    private Vector3 _hoveredTile;

    // Input flags
    private bool _inputEnabled = true;

    private void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        if (battleManager == null) battleManager = FindObjectOfType<BattleManager>();
        if (strikeBarUI == null) strikeBarUI = FindObjectOfType<StrikeBarUI>();
    }

    private void Update()
    {
        if (!_inputEnabled || battleManager == null) return;

        // If the Strike Bar is active, it handles its own input — we just don't interfere
        if (strikeBarUI != null && strikeBarUI.IsActive)
        {
            _inputMode = InputMode.Wait;
            return;
        }

        // If battle is not in Strike phase, only allow basic interaction
        if (battleManager.currentPhase != BattlePhase.Strike)
        {
            HandleMusterInput();
            return;
        }

        // Strike phase input
        HandleStrikeInput();
    }

    // -----------------------------------------------------------------------
    // MUSTER PHASE INPUT
    // -----------------------------------------------------------------------

    private void HandleMusterInput()
    {
        // During Muster, clicking on enemy units scouts them
        if (Input.GetMouseButtonDown(0))
        {
            UnitController clicked = RaycastUnit();
            if (clicked != null && clicked.faction == HouseName.Daminari)
            {
                battleManager.ScoutEnemy(clicked);
            }
        }

        // Space or Enter to commence the Strike
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            battleManager.CommenceStrike();
        }
    }

    // -----------------------------------------------------------------------
    // STRIKE PHASE INPUT
    // -----------------------------------------------------------------------

    private void HandleStrikeInput()
    {
        // Tab: cycle player units
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            CyclePlayerUnits();
            return;
        }

        // Esc: retreat
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            battleManager.Retreat();
            return;
        }

        // Q: Deep Dig
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (battleManager.SelectedUnit != null && battleManager.SelectedUnit.faction == HouseName.Grogen)
            {
                battleManager.CheckDeepDig(battleManager.SelectedUnit.gridX < 6 ? 0 : 1);
            }
            return;
        }

        // W: Cast Ice
        if (Input.GetKeyDown(KeyCode.W))
        {
            if (battleManager.SelectedUnit != null)
            {
                battleManager.TryCastIce(battleManager.SelectedUnit);
            }
            return;
        }

        // E: Charge (Knight-Errant flourish)
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (battleManager.SelectedUnit != null && battleManager.SelectedUnit.unitClass == UnitClass.KnightErrant)
            {
                battleManager.SelectedUnit.TriggerFlourish();
            }
            return;
        }

        // R: Rest
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (battleManager.SelectedUnit != null && battleManager.SelectedUnit.CanAct)
            {
                battleManager.SelectedUnit.Rest(3f);
                battleManager.EndActiveUnitTurn();
            }
            return;
        }

        // F: Toggle Steady Hand
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (strikeBarUI != null)
            {
                if (strikeBarUI.assistMode == AssistMode.SteadyHand)
                {
                    strikeBarUI.SetAssistMode(AssistMode.None);
                    Debug.Log("[Input] Steady Hand disabled.");
                }
                else
                {
                    strikeBarUI.SetAssistMode(AssistMode.SteadyHand);
                    Debug.Log("[Input] Steady Hand enabled — bands widened 60%, sweep slowed 35%.");
                }
            }
            return;
        }

        // H: Toggle The Reckoning
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (strikeBarUI != null)
            {
                if (strikeBarUI.assistMode == AssistMode.TheReckoning)
                {
                    strikeBarUI.SetAssistMode(AssistMode.None);
                    Debug.Log("[Input] The Reckoning disabled.");
                }
                else
                {
                    strikeBarUI.SetAssistMode(AssistMode.TheReckoning);
                    Debug.Log("[Input] The Reckoning enabled — timing removed, flat ×0.72.");
                }
            }
            return;
        }

        // Mouse click: select / move / attack
        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }

        // Right click: end turn
        if (Input.GetMouseButtonDown(1))
        {
            if (battleManager.SelectedUnit != null && battleManager.SelectedUnit.CanAct)
            {
                battleManager.EndActiveUnitTurn();
            }
        }

        // Update hover state
        UpdateHover();
    }

    private void HandleMouseClick()
    {
        // First, check if we clicked a unit
        UnitController clickedUnit = RaycastUnit();

        if (clickedUnit != null)
        {
            // Clicked on a unit
            if (clickedUnit.faction == HouseName.Grogen && !clickedUnit.IsWounded)
            {
                // Select player unit
                battleManager.SelectUnit(clickedUnit);
                _inputMode = InputMode.Select;
            }
            else if (clickedUnit.faction == HouseName.Daminari && !clickedUnit.IsWounded)
            {
                // Attack enemy unit (if we have a selected unit that can act)
                if (battleManager.SelectedUnit != null && battleManager.SelectedUnit.CanAct)
                {
                    battleManager.InitiateAttack(battleManager.SelectedUnit, clickedUnit);
                }
            }
            return;
        }

        // Otherwise, check if we clicked a tile to move
        Vector3? tilePos = RaycastTile();
        if (tilePos.HasValue && battleManager.SelectedUnit != null && battleManager.SelectedUnit.CanAct)
        {
            // Convert world position to grid coordinates
            Vector3 worldPos = tilePos.Value;
            int gridX = Mathf.RoundToInt((worldPos.x - (-9f)) / 1.5f);
            int gridY = Mathf.RoundToInt((worldPos.z - (-6f)) / 1.5f);

            gridX = Mathf.Clamp(gridX, 0, 11);
            gridY = Mathf.Clamp(gridY, 0, 7);

            battleManager.MovePlayerUnit(gridX, gridY);
        }
    }

    // -----------------------------------------------------------------------
    // HELPERS
    // -----------------------------------------------------------------------

    /// <summary>
    /// Cycles through active player units with Tab.
    /// </summary>
    private void CyclePlayerUnits()
    {
        var playerUnits = battleManager.PlayerUnits;
        if (playerUnits.Count == 0) return;

        // Find current selected index
        int currentIndex = -1;
        for (int i = 0; i < playerUnits.Count; i++)
        {
            if (playerUnits[i] == battleManager.SelectedUnit)
            {
                currentIndex = i;
                break;
            }
        }

        // Find next active unit
        int nextIndex = (currentIndex + 1) % playerUnits.Count;
        int attempts = 0;
        while (attempts < playerUnits.Count)
        {
            UnitController candidate = playerUnits[nextIndex];
            if (candidate != null && !candidate.IsWounded)
            {
                battleManager.SelectUnit(candidate);
                return;
            }
            nextIndex = (nextIndex + 1) % playerUnits.Count;
            attempts++;
        }
    }

    /// <summary>
    /// Updates the hovered unit/tile for visual feedback.
    /// </summary>
    private void UpdateHover()
    {
        UnitController hovered = RaycastUnit();
        if (hovered != _hoveredUnit)
        {
            _hoveredUnit = hovered;
            // TODO: Show hover highlight
        }

        Vector3? tile = RaycastTile();
        if (tile.HasValue)
        {
            _hoveredTile = tile.Value;
            // TODO: Show tile hover indicator
        }
    }

    /// <summary>
    /// Raycasts against unit colliders and returns the hit unit, if any.
    /// </summary>
    private UnitController RaycastUnit()
    {
        if (playerCamera == null) return null;

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, unitLayerMask))
        {
            return hit.collider.GetComponent<UnitController>();
        }
        return null;
    }

    /// <summary>
    /// Raycasts against tile/ground colliders and returns the hit position, if any.
    /// </summary>
    private Vector3? RaycastTile()
    {
        if (playerCamera == null) return null;

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, tileLayerMask))
        {
            return hit.point;
        }
        return null;
    }

    /// <summary>
    /// Enables or disables input (e.g., during animations or cutscenes).
    /// </summary>
    public void SetInputEnabled(bool enabled)
    {
        _inputEnabled = enabled;
    }
}
