using System;
using UnityEngine;

// ATTACH TO: The BattleManager GameObject (or any headless battle host).
// INSPECTOR FIELDS: None.
//
// SEALED BY CONSTRUCTION: no constant, multiplier or ceiling is exposed to the
// Inspector or to runtime code. What cannot be written cannot be bought.

/// <summary>
/// The patented Strike Reckoning of THE WARBORN MARCH (US Provisional 64/126,132).
///
/// A faithful port of the house's browser engine (src/march/strike/engine.ts).
/// The whole blow is settled HERE — band, commitment, oath, ground, height,
/// flank, guard and the turn. Nothing outside may multiply the result
/// afterwards; that was the old breach and it double-counted guard.
///
/// Pure reckoner: no rendering, no audio, no scene references.
/// </summary>
public sealed class StrikeReckoner : MonoBehaviour
{
    // --- PATENT-LOCKED CONSTANTS -------------------------------------------
    private const double OATH_RING = 1.15;        // the ring: each oath beats one
    private const double GROUND_ATTACKER = 1.10;
    private const double GROUND_DEFENDER = 0.92;
    private const double HEIGHT_STEP = 0.12;
    private const double HEIGHT_FLOOR = 0.76;
    private const double HEIGHT_CEIL = 1.24;
    private const double FLANK_SIDE = 1.08;
    private const double FLANK_BACK = 1.18;
    private const double BAND_CLEAN = 1.00;
    private const double BAND_TRUE = 0.70;
    private const double BAND_TURNED = 0.25;
    private const double GUARD_FLOOR = 0.30;      // a shield never soaks it all
    private const double TURN_MULT = 0.50;
    private const double RECKONING_MULT = 0.72;   // the no-timing mercy
    private const int MINIMUM_DAMAGE = 1;

    // --- FACING & GROUND ----------------------------------------------------

    /// <summary>The quarter a blow arrives from, relative to the defender's look.</summary>
    public enum Flank { Front, Side, Back }

    /// <summary>Which house holds the ground beneath the ford.</summary>
    public enum GroundFavour { Neither, Attacker, Defender }

    /// <summary>The meters a piece carries into a blow. Might already carries its edge.</summary>
    public struct UnitStats
    {
        public int Vigour;
        public int Might;
        public int Guard;
        public int Magical;
    }

    /// <summary>Everything the hand and the field contribute to one blow.</summary>
    public struct StrikeInputs
    {
        public StrikeBand Band;
        public double Commitment;
        public bool Turned;
        public GroundFavour Ground;
        public int HeightTiers;
        public Flank Flank;
        /// <summary>The Reckoning mercy: no bar at all, a flat seven tenths.</summary>
        public bool WithoutTiming;
    }

    /// <summary>The settled blow, exactly as the browser March reports it.</summary>
    public struct StrikeResult
    {
        public int Damage;
        public int RemainingVigour;
        public int Gross;
        public StrikeBand Band;
        public double Commitment;
        public bool Turned;
        public bool Flourish;
    }

    // --- THE RING -----------------------------------------------------------

    private static Oath Beaten(Oath oath)
    {
        switch (oath)
        {
            case Oath.Sworn: return Oath.Damned;
            case Oath.Damned: return Oath.Unsworn;
            default: return Oath.Sworn;
        }
    }

    private static double BandMultiplier(StrikeBand band)
    {
        switch (band)
        {
            case StrikeBand.Clean: return BAND_CLEAN;
            case StrikeBand.True: return BAND_TRUE;
            default: return BAND_TURNED;   // Turned and Miss alike
        }
    }

    private static double FlankMultiplier(Flank flank)
    {
        switch (flank)
        {
            case Flank.Side: return FLANK_SIDE;
            case Flank.Back: return FLANK_BACK;
            default: return 1.0;
        }
    }

    private static double GroundMultiplier(GroundFavour ground)
    {
        switch (ground)
        {
            case GroundFavour.Attacker: return GROUND_ATTACKER;
            case GroundFavour.Defender: return GROUND_DEFENDER;
            default: return 1.0;
        }
    }

    /// <summary>JavaScript rounding — half away from zero upward, as the browser does.</summary>
    private static int JsRound(double v)
    {
        return (int)Math.Floor(v + 0.5);
    }

    // --- THE RECKONING ------------------------------------------------------

    /// <summary>
    /// Settles one blow. The only entry. Every multiplier lives inside.
    /// </summary>
    public StrikeResult ReckonStrike(UnitStats attacker, UnitStats defender,
        Oath attackerOath, Oath defenderOath, StrikeInputs input)
    {
        double commitment = input.Commitment <= 0 ? 1.0 : input.Commitment;
        StrikeBand band = input.WithoutTiming ? StrikeBand.Clean : input.Band;
        bool turned = !input.WithoutTiming && input.Turned;

        double force = attacker.Might * commitment;
        double oath = Beaten(attackerOath) == defenderOath ? OATH_RING : 1.0;
        double ground = GroundMultiplier(input.Ground);
        double height = Math.Min(HEIGHT_CEIL,
            Math.Max(HEIGHT_FLOOR, 1.0 + HEIGHT_STEP * input.HeightTiers));
        double flank = FlankMultiplier(input.Flank);

        double gross = force * oath * ground * height * flank;

        // The guard eats the blow BEFORE the swing's quality is judged, so a bad
        // tap is punished once, never twice.
        double through = Math.Max(gross - defender.Guard, gross * GUARD_FLOOR);
        double mitigated = through * BandMultiplier(band);

        int damage = Math.Max(MINIMUM_DAMAGE,
            JsRound(mitigated * (turned ? TURN_MULT : 1.0)));

        StrikeBand reportedBand = band;
        bool flourish = band == StrikeBand.Clean && !turned;

        if (input.WithoutTiming)
        {
            damage = JsRound(damage * RECKONING_MULT);
            reportedBand = StrikeBand.True;
            flourish = false;
        }

        int remaining = defender.Vigour - damage;
        if (remaining < 0) remaining = 0;

        return new StrikeResult
        {
            Damage = damage,
            RemainingVigour = remaining,
            Gross = JsRound(gross),
            Band = reportedBand,
            Commitment = commitment,
            Turned = turned,
            Flourish = flourish,
        };
    }
}
