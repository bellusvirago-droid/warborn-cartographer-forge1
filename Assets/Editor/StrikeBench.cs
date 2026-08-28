using System;
using System.Text;
using UnityEditor;
using UnityEngine;
using GroundFavour = StrikeReckoner.GroundFavour;

/// <summary>
/// THE PAIRED BENCH — proof #2 of the Phase II slice.
///
/// "The Strike reckoning in Unity and the Strike reckoning in the browser
///  return the same result from the same inputs, proven by a bench of paired
///  cases."  — the Vertical Slice charter, VI.2
///
/// Every expected number below was produced by running the house's own browser
/// engine (src/march/strike/engine.ts) over these exact inputs. They are not
/// hand-written and must never be hand-edited. If a case fails, the Unity port
/// has drifted from the patent — mend the port, never the bench.
///
/// Run headless:
///   Unity.exe -batchmode -projectPath . -executeMethod StrikeBench.RunHeadless -quit
/// Or in the editor:  Warborn > Forge > Run the Paired Strike Bench
/// </summary>
public static class StrikeBench
{
    private struct Case
    {
        public readonly int Might, Guard, Vigour, HeightTiers, ExpectedGross, ExpectedFinal;
        public readonly double Commitment, Flank;
        public readonly StrikeBand Band;
        public readonly StrikeReckoner.GroundFavour Ground;
        public readonly bool Turned, ExpectedFlourish;

        public Case(int might, int guard, int vigour, StrikeBand band, double commitment,
            StrikeReckoner.GroundFavour ground, int heightTiers, double flank, bool turned,
            int expectedGross, int expectedFinal, bool expectedFlourish)
        {
            Might = might; Guard = guard; Vigour = vigour; Band = band;
            Commitment = commitment; Ground = ground; HeightTiers = heightTiers;
            Flank = flank; Turned = turned;
            ExpectedGross = expectedGross; ExpectedFinal = expectedFinal;
            ExpectedFlourish = expectedFlourish;
        }
    }

    // Attacker is Sworn, defender is Damned throughout, so the oath ring is live
    // (1.15) on every case — the browser cases were generated the same way.
    private static readonly Case[] Cases =
    {
        new Case(19, 10, 34, StrikeBand.Clean, 1d, GroundFavour.Neither, 0, 1d, false, 22, 12, true),
        new Case(21, 9, 34, StrikeBand.Clean, 1d, GroundFavour.Neither, 1, 1d, true, 27, 9, false),
        new Case(18, 12, 34, StrikeBand.Clean, 1d, GroundFavour.Neither, -2, 1.08d, false, 17, 5, true),
        new Case(20, 11, 34, StrikeBand.Clean, 1d, GroundFavour.Attacker, 0, 1.08d, true, 27, 8, false),
        new Case(22, 10, 34, StrikeBand.Clean, 1d, GroundFavour.Attacker, 1, 1.18d, false, 37, 27, true),
        new Case(19, 9, 34, StrikeBand.Clean, 1d, GroundFavour.Attacker, -2, 1.18d, true, 22, 6, false),
        new Case(21, 12, 34, StrikeBand.Clean, 1d, GroundFavour.Defender, 1, 1d, false, 25, 13, true),
        new Case(18, 11, 34, StrikeBand.Clean, 1d, GroundFavour.Defender, -2, 1d, true, 14, 2, false),
        new Case(20, 10, 34, StrikeBand.Clean, 0.6d, GroundFavour.Neither, 0, 1.08d, false, 15, 5, true),
        new Case(22, 9, 34, StrikeBand.Clean, 0.6d, GroundFavour.Neither, 1, 1.08d, true, 18, 5, false),
        new Case(19, 12, 34, StrikeBand.Clean, 0.6d, GroundFavour.Neither, -2, 1.18d, false, 12, 4, true),
        new Case(21, 11, 34, StrikeBand.Clean, 0.6d, GroundFavour.Attacker, 0, 1.18d, true, 19, 4, false),
        new Case(18, 10, 34, StrikeBand.Clean, 0.6d, GroundFavour.Attacker, -2, 1d, false, 10, 3, true),
        new Case(20, 9, 34, StrikeBand.Clean, 0.6d, GroundFavour.Defender, 0, 1d, true, 13, 2, false),
        new Case(22, 12, 34, StrikeBand.Clean, 0.6d, GroundFavour.Defender, 1, 1.08d, false, 17, 5, true),
        new Case(19, 11, 34, StrikeBand.Clean, 0.6d, GroundFavour.Defender, -2, 1.08d, true, 10, 1, false),
        new Case(21, 10, 34, StrikeBand.Clean, 1.8d, GroundFavour.Neither, 0, 1.18d, false, 51, 41, true),
        new Case(18, 9, 34, StrikeBand.Clean, 1.8d, GroundFavour.Neither, 1, 1.18d, true, 49, 20, false),
        new Case(20, 12, 34, StrikeBand.Clean, 1.8d, GroundFavour.Attacker, 0, 1d, false, 46, 34, true),
        new Case(22, 11, 34, StrikeBand.Clean, 1.8d, GroundFavour.Attacker, 1, 1d, true, 56, 23, false),
        new Case(19, 10, 34, StrikeBand.Clean, 1.8d, GroundFavour.Attacker, -2, 1.08d, false, 36, 26, true),
        new Case(21, 9, 34, StrikeBand.Clean, 1.8d, GroundFavour.Defender, 0, 1.08d, true, 43, 17, false),
        new Case(18, 12, 34, StrikeBand.Clean, 1.8d, GroundFavour.Defender, 1, 1.18d, false, 45, 33, true),
        new Case(20, 11, 34, StrikeBand.Clean, 1.8d, GroundFavour.Defender, -2, 1.18d, true, 34, 12, false),
        new Case(22, 10, 34, StrikeBand.True, 1d, GroundFavour.Neither, 1, 1d, false, 28, 13, false),
        new Case(19, 9, 34, StrikeBand.True, 1d, GroundFavour.Neither, -2, 1d, true, 17, 3, false),
        new Case(21, 12, 34, StrikeBand.True, 1d, GroundFavour.Attacker, 0, 1.08d, false, 29, 12, false),
        new Case(18, 11, 34, StrikeBand.True, 1d, GroundFavour.Attacker, 1, 1.08d, true, 28, 6, false),
        new Case(20, 10, 34, StrikeBand.True, 1d, GroundFavour.Attacker, -2, 1.18d, false, 23, 9, false),
        new Case(22, 9, 34, StrikeBand.True, 1d, GroundFavour.Defender, 0, 1.18d, true, 27, 6, false),
        new Case(19, 12, 34, StrikeBand.True, 1d, GroundFavour.Defender, -2, 1d, false, 15, 3, false),
        new Case(21, 11, 34, StrikeBand.True, 0.6d, GroundFavour.Neither, 0, 1d, true, 14, 2, false),
        new Case(18, 10, 34, StrikeBand.True, 0.6d, GroundFavour.Neither, 1, 1.08d, false, 15, 4, false),
        new Case(20, 9, 34, StrikeBand.True, 0.6d, GroundFavour.Neither, -2, 1.08d, true, 11, 1, false),
        new Case(22, 12, 34, StrikeBand.True, 0.6d, GroundFavour.Attacker, 0, 1.18d, false, 20, 5, false),
        new Case(19, 11, 34, StrikeBand.True, 0.6d, GroundFavour.Attacker, 1, 1.18d, true, 19, 3, false),
        new Case(21, 10, 34, StrikeBand.True, 0.6d, GroundFavour.Defender, 0, 1d, false, 13, 3, false),
        new Case(18, 9, 34, StrikeBand.True, 0.6d, GroundFavour.Defender, 1, 1d, true, 13, 1, false),
        new Case(20, 12, 34, StrikeBand.True, 0.6d, GroundFavour.Defender, -2, 1.08d, false, 10, 2, false),
        new Case(22, 11, 34, StrikeBand.True, 1.8d, GroundFavour.Neither, 0, 1.08d, true, 49, 13, false),
        new Case(19, 10, 34, StrikeBand.True, 1.8d, GroundFavour.Neither, 1, 1.18d, false, 52, 29, false),
        new Case(21, 9, 34, StrikeBand.True, 1.8d, GroundFavour.Neither, -2, 1.18d, true, 39, 10, false),
        new Case(18, 12, 34, StrikeBand.True, 1.8d, GroundFavour.Attacker, 1, 1d, false, 46, 24, false),
        new Case(20, 11, 34, StrikeBand.True, 1.8d, GroundFavour.Attacker, -2, 1d, true, 35, 8, false),
        new Case(22, 10, 34, StrikeBand.True, 1.8d, GroundFavour.Defender, 0, 1.08d, false, 45, 25, false),
        new Case(19, 9, 34, StrikeBand.True, 1.8d, GroundFavour.Defender, 1, 1.08d, true, 44, 12, false),
        new Case(21, 12, 34, StrikeBand.True, 1.8d, GroundFavour.Defender, -2, 1.18d, false, 36, 17, false),
        new Case(18, 11, 34, StrikeBand.Turned, 1d, GroundFavour.Neither, 0, 1.18d, true, 24, 2, false),
        new Case(20, 10, 34, StrikeBand.Turned, 1d, GroundFavour.Neither, -2, 1d, false, 17, 2, false),
        new Case(22, 9, 34, StrikeBand.Turned, 1d, GroundFavour.Attacker, 0, 1d, true, 28, 2, false),
        new Case(19, 12, 34, StrikeBand.Turned, 1d, GroundFavour.Attacker, 1, 1.08d, false, 29, 4, false),
        new Case(21, 11, 34, StrikeBand.Turned, 1d, GroundFavour.Attacker, -2, 1.08d, true, 22, 1, false),
        new Case(18, 10, 34, StrikeBand.Turned, 1d, GroundFavour.Defender, 0, 1.18d, false, 22, 3, false),
        new Case(20, 9, 34, StrikeBand.Turned, 1d, GroundFavour.Defender, 1, 1.18d, true, 28, 2, false),
        new Case(22, 12, 34, StrikeBand.Turned, 0.6d, GroundFavour.Neither, 0, 1d, false, 15, 1, false),
        new Case(19, 11, 34, StrikeBand.Turned, 0.6d, GroundFavour.Neither, 1, 1d, true, 15, 1, false),
        new Case(21, 10, 34, StrikeBand.Turned, 0.6d, GroundFavour.Neither, -2, 1.08d, false, 12, 1, false),
        new Case(18, 9, 34, StrikeBand.Turned, 0.6d, GroundFavour.Attacker, 0, 1.08d, true, 15, 1, false),
        new Case(20, 12, 34, StrikeBand.Turned, 0.6d, GroundFavour.Attacker, 1, 1.18d, false, 20, 2, false),
        new Case(22, 11, 34, StrikeBand.Turned, 0.6d, GroundFavour.Attacker, -2, 1.18d, true, 15, 1, false),
        new Case(19, 10, 34, StrikeBand.Turned, 0.6d, GroundFavour.Defender, 1, 1d, false, 14, 1, false),
        new Case(21, 9, 34, StrikeBand.Turned, 0.6d, GroundFavour.Defender, -2, 1d, true, 10, 1, false),
        new Case(18, 12, 34, StrikeBand.Turned, 1.8d, GroundFavour.Neither, 0, 1.08d, false, 40, 7, false),
        new Case(20, 11, 34, StrikeBand.Turned, 1.8d, GroundFavour.Neither, 1, 1.08d, true, 50, 5, false),
        new Case(22, 10, 34, StrikeBand.Turned, 1.8d, GroundFavour.Neither, -2, 1.18d, false, 41, 8, false),
        new Case(19, 9, 34, StrikeBand.Turned, 1.8d, GroundFavour.Attacker, 0, 1.18d, true, 51, 5, false),
        new Case(21, 12, 34, StrikeBand.Turned, 1.8d, GroundFavour.Attacker, -2, 1d, false, 36, 6, false),
        new Case(18, 11, 34, StrikeBand.Turned, 1.8d, GroundFavour.Defender, 0, 1d, true, 34, 3, false),
        new Case(20, 10, 34, StrikeBand.Turned, 1.8d, GroundFavour.Defender, 1, 1.08d, false, 46, 9, false),
        new Case(22, 9, 34, StrikeBand.Turned, 1.8d, GroundFavour.Defender, -2, 1.08d, true, 34, 3, false),
    };

    private static StrikeReckoner.Flank FlankOf(double f)
    {
        if (Math.Abs(f - 1.18) < 0.0001) return StrikeReckoner.Flank.Back;
        if (Math.Abs(f - 1.08) < 0.0001) return StrikeReckoner.Flank.Side;
        return StrikeReckoner.Flank.Front;
    }

    /// <summary>Walks the bench. Returns the number of cases that disagreed.</summary>
    public static int Walk(out string report)
    {
        var host = new GameObject("StrikeBench_Host");
        var reckoner = host.AddComponent<StrikeReckoner>();
        var sb = new StringBuilder();
        int failed = 0;

        for (int i = 0; i < Cases.Length; i++)
        {
            Case c = Cases[i];

            var attacker = new StrikeReckoner.UnitStats
            { Vigour = 40, Might = c.Might, Guard = 0, Magical = 0 };
            var defender = new StrikeReckoner.UnitStats
            { Vigour = c.Vigour, Might = 12, Guard = c.Guard, Magical = 0 };

            var input = new StrikeReckoner.StrikeInputs
            {
                Band = c.Band,
                Commitment = c.Commitment,
                Turned = c.Turned,
                Ground = c.Ground,
                HeightTiers = c.HeightTiers,
                Flank = FlankOf(c.Flank),
                WithoutTiming = false,
            };

            StrikeReckoner.StrikeResult r =
                reckoner.ReckonStrike(attacker, defender, Oath.Sworn, Oath.Damned, input);

            bool ok = r.Damage == c.ExpectedFinal
                   && r.Gross == c.ExpectedGross
                   && r.Flourish == c.ExpectedFlourish;

            if (!ok)
            {
                failed++;
                sb.AppendLine(string.Format(
                    "  CASE {0,2} DISAGREES  band={1} commit={2} ground={3} height={4} flank={5} turned={6}\n" +
                    "      browser: gross={7} final={8} flourish={9}\n" +
                    "      unity:   gross={10} final={11} flourish={12}",
                    i, c.Band, c.Commitment, c.Ground, c.HeightTiers, c.Flank, c.Turned,
                    c.ExpectedGross, c.ExpectedFinal, c.ExpectedFlourish,
                    r.Gross, r.Damage, r.Flourish));
            }
        }

        if (Application.isPlaying) UnityEngine.Object.Destroy(host);
        else UnityEngine.Object.DestroyImmediate(host);

        sb.Insert(0, string.Format(
            "THE PAIRED STRIKE BENCH — {0} cases, {1} agree, {2} disagree.\n",
            Cases.Length, Cases.Length - failed, failed));

        report = sb.ToString();
        return failed;
    }

    [MenuItem("Warborn/Forge/Run the Paired Strike Bench")]
    public static void RunFromMenu()
    {
        string report;
        int failed = Walk(out report);
        if (failed == 0) Debug.Log(report);
        else Debug.LogError(report);
    }

    /// <summary>Headless entry. Exits non-zero when the bodies disagree, so the Forge fails loudly.</summary>
    public static void RunHeadless()
    {
        string report;
        int failed = Walk(out report);
        Debug.Log(report);
                EditorApplication.Exit(failed == 0 ? 0 : 1);
    }
}
