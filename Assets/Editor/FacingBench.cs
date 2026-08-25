#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// THE FACING BENCH.
///
/// The Paired Strike Bench proves the reckoning's arithmetic. This bench proves
/// the one input the Unity body used to lie about: which quarter a blow came
/// from. Every case below is reasoned by hand from the grid, not generated, so
/// a future hand cannot quietly redefine "behind" and still pass.
///
/// Headless: Unity -batchmode -quit -executeMethod FacingBench.RunHeadless
/// </summary>
public static class FacingBench
{
    private struct Case
    {
        public string Told;
        public Quarter DefenderFacing;
        public int Dx;   // attacker position relative to defender, on the grid
        public int Dy;
        public StrikeReckoner.Flank Expected;
    }

    private static readonly Case[] Cases =
    {
        // A defender facing North looks toward decreasing gridY.
        new Case { Told = "Facing north, struck from the north",  DefenderFacing = Quarter.North, Dx = 0,  Dy = -3, Expected = StrikeReckoner.Flank.Front },
        new Case { Told = "Facing north, struck from the south",  DefenderFacing = Quarter.North, Dx = 0,  Dy =  3, Expected = StrikeReckoner.Flank.Back  },
        new Case { Told = "Facing north, struck from the east",   DefenderFacing = Quarter.North, Dx = 4,  Dy =  0, Expected = StrikeReckoner.Flank.Side  },
        new Case { Told = "Facing north, struck from the west",   DefenderFacing = Quarter.North, Dx = -4, Dy =  0, Expected = StrikeReckoner.Flank.Side  },

        // The two houses meet across the ford on the horizontal.
        new Case { Told = "Facing east, struck from the east",    DefenderFacing = Quarter.East,  Dx = 2,  Dy =  0, Expected = StrikeReckoner.Flank.Front },
        new Case { Told = "Facing east, struck from the west",    DefenderFacing = Quarter.East,  Dx = -2, Dy =  0, Expected = StrikeReckoner.Flank.Back  },
        new Case { Told = "Facing east, struck from the north",   DefenderFacing = Quarter.East,  Dx = 0,  Dy = -2, Expected = StrikeReckoner.Flank.Side  },
        new Case { Told = "Facing east, struck from the south",   DefenderFacing = Quarter.East,  Dx = 0,  Dy =  2, Expected = StrikeReckoner.Flank.Side  },

        new Case { Told = "Facing south, struck from the south",  DefenderFacing = Quarter.South, Dx = 0,  Dy =  1, Expected = StrikeReckoner.Flank.Front },
        new Case { Told = "Facing south, struck from the north",  DefenderFacing = Quarter.South, Dx = 0,  Dy = -1, Expected = StrikeReckoner.Flank.Back  },
        new Case { Told = "Facing west, struck from the west",    DefenderFacing = Quarter.West,  Dx = -5, Dy =  0, Expected = StrikeReckoner.Flank.Front },
        new Case { Told = "Facing west, struck from the east",    DefenderFacing = Quarter.West,  Dx = 5,  Dy =  0, Expected = StrikeReckoner.Flank.Back  },

        // Diagonals fall to the longer axis. A blow from far east and slightly
        // north is an eastern blow.
        new Case { Told = "Facing north, struck from far east and a little north", DefenderFacing = Quarter.North, Dx = 6,  Dy = -1, Expected = StrikeReckoner.Flank.Side  },
        new Case { Told = "Facing north, struck from a little east and far north", DefenderFacing = Quarter.North, Dx = 1,  Dy = -6, Expected = StrikeReckoner.Flank.Front },
        new Case { Told = "Facing east, struck from far south and a little east",  DefenderFacing = Quarter.East,  Dx = 1,  Dy =  7, Expected = StrikeReckoner.Flank.Side  },

        // A dead tie on both axes falls to the horizontal, by the stated rule.
        new Case { Told = "Facing north, struck from the exact north-east corner", DefenderFacing = Quarter.North, Dx = 3,  Dy = -3, Expected = StrikeReckoner.Flank.Side  },

        // The line facing the wrong way is the whole reason the Legion breaks.
        new Case { Told = "Facing west while the Dig erupts to the east",          DefenderFacing = Quarter.West,  Dx = 2,  Dy = -1, Expected = StrikeReckoner.Flank.Back  },
    };

    [MenuItem("Warborn/Walk the Facing Bench")]
    public static void RunHeadless()
    {
        var failures = new List<string>();

        foreach (var c in Cases)
        {
            // The defender stands at the origin of the reckoning; the attacker
            // is placed relative to him.
            Quarter blowFrom = Facing.Toward(0, 0, c.Dx, c.Dy);
            var got = Facing.Compare(c.DefenderFacing, blowFrom);

            if (got != c.Expected)
                failures.Add($"  {c.Told}: expected {c.Expected}, the body said {got} (blow read as from the {blowFrom}).");
        }

        // A blow can never be Front and Back at once, and turning to meet a
        // blow must always make the next one a Front. Prove the turn closes.
        foreach (Quarter defenderFacing in Enum.GetValues(typeof(Quarter)))
        {
            foreach (Quarter blowFrom in Enum.GetValues(typeof(Quarter)))
            {
                var before = Facing.Compare(defenderFacing, blowFrom);
                var afterTurning = Facing.Compare(blowFrom, blowFrom);
                if (afterTurning != StrikeReckoner.Flank.Front)
                    failures.Add($"  Turning to meet a blow from the {blowFrom} did not make it a Front (was {before}).");
            }
        }

        if (failures.Count > 0)
        {
            Debug.LogError($"[FacingBench] {failures.Count} case(s) failed:\n" + string.Join("\n", failures));
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }

        Debug.Log($"[FacingBench] {Cases.Length} hand-reasoned cases passed, and every turn closes. The facing is true.");
    }
}
#endif
