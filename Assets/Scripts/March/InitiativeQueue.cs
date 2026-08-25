using System.Collections.Generic;
using UnityEngine;

/*
 * ATTACH TO: The BattleManager GameObject.
 *
 * Implements the rolling initiative system from The March Design Bible.
 * Each unit accrues HAND + d6 readiness per tick; at 100 it acts and resets to 0.
 * Fast units (low HAND) act more often but hit softer (lower MIGHT).
 * The upcoming order is always visible to the player.
 */

/// <summary>
/// Rolling initiative queue. Not fixed rounds — units act when their readiness hits 100.
/// </summary>
public class InitiativeQueue
{
    private const int ACTION_THRESHOLD = 100;
    private const int DIE_MIN = 1;
    private const int DIE_MAX = 6;

    private List<UnitInitiative> _units = new List<UnitInitiative>();
    private System.Random _rng;
    private int _seed;

    /// <summary>
    /// A single unit's initiative tracking entry.
    /// </summary>
    public class UnitInitiative
    {
        public UnitController Unit;
        public float Readiness;
        public int LastRoll;

        public UnitInitiative(UnitController unit)
        {
            Unit = unit;
            Readiness = 0f;
            LastRoll = 0;
        }
    }

    /// <summary>
    /// Creates a new initiative queue with an optional deterministic seed.
    /// </summary>
    public InitiativeQueue(int seed = -1)
    {
        _seed = seed;
        _rng = seed >= 0 ? new System.Random(seed) : new System.Random();
    }

    /// <summary>
    /// Adds a unit to the initiative queue.
    /// </summary>
    public void AddUnit(UnitController unit)
    {
        if (unit == null || unit.IsWounded) return;
        _units.Add(new UnitInitiative(unit));
    }

    /// <summary>
    /// Removes a unit from the queue (when wounded/dead).
    /// </summary>
    public void RemoveUnit(UnitController unit)
    {
        _units.RemoveAll(u => u.Unit == unit);
    }

    /// <summary>
    /// Returns the unit whose turn it is to act, or null if no one is ready.
    /// Does NOT advance the queue — call AdvanceToNext() to progress.
    /// </summary>
    public UnitController PeekNext()
    {
        // Find the unit with highest readiness
        UnitInitiative highest = null;
        foreach (var entry in _units)
        {
            if (entry.Unit.IsWounded) continue;
            if (highest == null || entry.Readiness > highest.Readiness)
            {
                highest = entry;
            }
        }
        return highest?.Unit;
    }

    /// <summary>
    /// Advances the queue by ticking readiness for all units until one reaches 100.
    /// Returns the unit that gets to act.
    /// </summary>
    public UnitController AdvanceToNext()
    {
        // Remove wounded units
        _units.RemoveAll(u => u.Unit == null || u.Unit.IsWounded);

        if (_units.Count == 0) return null;

        // Tick until someone reaches threshold
        while (true)
        {
            UnitInitiative highest = null;

            foreach (var entry in _units)
            {
                if (entry.Unit.IsWounded) continue;

                // Roll d6 and add HAND
                int roll = _rng.Next(DIE_MIN, DIE_MAX + 1);
                entry.LastRoll = roll;
                float accrual = entry.Unit.hand + roll;
                entry.Readiness += accrual;

                if (highest == null || entry.Readiness > highest.Readiness)
                {
                    highest = entry;
                }
            }

            if (highest != null && highest.Readiness >= ACTION_THRESHOLD)
            {
                highest.Readiness = 0f; // Reset after acting
                return highest.Unit;
            }
        }
    }

    /// <summary>
    /// Returns a read-only list of all units in the queue, sorted by current readiness (descending).
    /// Used for displaying the upcoming turn order.
    /// </summary>
    public List<UnitController> GetUpcomingOrder()
    {
        var sorted = new List<UnitInitiative>(_units);
        sorted.RemoveAll(u => u.Unit == null || u.Unit.IsWounded);
        sorted.Sort((a, b) => b.Readiness.CompareTo(a.Readiness));

        var result = new List<UnitController>();
        foreach (var entry in sorted)
        {
            result.Add(entry.Unit);
        }
        return result;
    }

    /// <summary>
    /// Returns the current readiness of a specific unit.
    /// </summary>
    public float GetReadiness(UnitController unit)
    {
        foreach (var entry in _units)
        {
            if (entry.Unit == unit) return entry.Readiness;
        }
        return 0f;
    }

    /// <summary>
    /// Returns the last d6 roll for a specific unit.
    /// </summary>
    public int GetLastRoll(UnitController unit)
    {
        foreach (var entry in _units)
        {
            if (entry.Unit == unit) return entry.LastRoll;
        }
        return 0;
    }

    /// <summary>
    /// Returns the number of active (non-wounded) units in the queue.
    /// </summary>
    public int ActiveCount
    {
        get
        {
            int count = 0;
            foreach (var entry in _units)
            {
                if (entry.Unit != null && !entry.Unit.IsWounded) count++;
            }
            return count;
        }
    }

    /// <summary>
    /// Clears the entire queue.
    /// </summary>
    public void Clear()
    {
        _units.Clear();
    }
}
