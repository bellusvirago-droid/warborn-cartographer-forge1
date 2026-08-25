using UnityEngine;
using UnityEngine.Events;

/*
 * ATTACH TO: The Dragon prefab (Ice variant for Phase II) spawned at the Sundered Ford.
 * 
 * INSPECTOR FIELDS:
 * - Marks Cost: The fixed price in Marks (earned strictly through free play) to hire this dragon.
 * - Term In Phases: How many battle phases the beast remains on the field before departing.
 * - On Departure: UnityEvent triggered when the term expires (used to play Ice-clearing/flight animations).
 */

public sealed class DragonContract : MonoBehaviour
{
    [Header("Contract Terms")]
    [Tooltip("Cost in Marks to hire. Marks are earned through play, never bought.")]
    [SerializeField] [Min(0)] private int marksCost = 500;
    
    [Tooltip("The duration of the dragon's service in battle phases at the Sundered Ford.")]
    [SerializeField] [Min(1)] private int termInPhases = 3;

    [Header("Contract Binding Events")]
    [Tooltip("Fired exactly when the term expires. The dragon departs with honor.")]
    [SerializeField] private UnityEvent onDeparture;

    // Internal state: sealed by construction.
    // There is no allegiance field, no target setter, and no reference to Grogens or Daminari.
    // What does not exist cannot be manipulated. Betrayal is structurally impossible.
    private bool isHired = false;
    private int remainingPhases = 0;
    private bool hasDeparted = false;

    public bool IsHired => isHired;
    public int RemainingPhases => remainingPhases;
    public int MarksCost => marksCost;

    /// <summary>
    /// Binds the contract if the provided marks meet the cost.
    /// The contract dictates timing, never obedience. The beast fights as the beast fights.
    /// </summary>
    public void EnactContract(int marksPaid)
    {
        if (isHired || hasDeparted)
        {
            Debug.LogWarning("Contract is already enacted or fulfilled. Cannot re-hire.");
            return;
        }

        if (marksPaid >= marksCost)
        {
            isHired = true;
            remainingPhases = termInPhases;
            Debug.Log("Dragon contracted. Ice will fall upon the Sundered Ford.");
        }
        else
        {
            Debug.LogWarning("Insufficient Marks. The beast ignores the summons.");
        }
    }

    /// <summary>
    /// Advances the contract clock. To be called by the StrikeReckoner at phase resolution.
    /// </summary>
    public void TickPhase()
    {
        if (!isHired || hasDeparted) return;

        remainingPhases--;

        if (remainingPhases <= 0)
        {
            Depart();
        }
    }

    /// <summary>
    /// Structurally guarantees the dragon's exit from the field when the contract ends.
    /// Cannot be intercepted or cancelled once triggered.
    /// </summary>
    private void Depart()
    {
        if (hasDeparted) return;

        remainingPhases = 0;
        isHired = false;
        hasDeparted = true;
        
        // The contract is fulfilled. The beast departs on time.
        if (onDeparture != null)
        {
            onDeparture.Invoke();
        }
        
        // Permanently disable this script to prevent any necrotic ticks or re-bindings.
        this.enabled = false;
    }
}
