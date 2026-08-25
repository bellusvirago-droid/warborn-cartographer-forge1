using UnityEngine;

/// <summary>
/// THE WARBORN MARCH — Phase II.
///
/// THE FACING. A soldier has a front, two flanks and a back, and the browser
/// March has always known it: the reckoning takes Front, Flank or Rear and
/// multiplies 1.0 / 1.08 / 1.18. Until now the Unity body always passed Front,
/// so a blow to the back of the line landed as softly as a blow to the shield.
/// That is a drift from the sealed reckoning, and this file closes it.
///
/// This module DECIDES NOTHING about damage. It only answers "from which
/// quarter did the blow come?" The multipliers live where they have always
/// lived — inside StrikeReckoner, which no one may edit.
/// </summary>
public enum Quarter
{
    /// <summary>Facing up the grid — decreasing gridY.</summary>
    North = 0,
    /// <summary>Facing right across the grid — increasing gridX.</summary>
    East = 1,
    /// <summary>Facing down the grid — increasing gridY.</summary>
    South = 2,
    /// <summary>Facing left across the grid — decreasing gridX.</summary>
    West = 3,
}

public static class Facing
{
    /// <summary>The world rotation a quarter stands in, so the body matches the ledger.</summary>
    public static Quaternion Rotation(Quarter q)
    {
        switch (q)
        {
            case Quarter.North: return Quaternion.Euler(0f, 0f, 0f);
            case Quarter.East: return Quaternion.Euler(0f, 90f, 0f);
            case Quarter.South: return Quaternion.Euler(0f, 180f, 0f);
            default: return Quaternion.Euler(0f, 270f, 0f);
        }
    }

    /// <summary>
    /// The quarter a unit standing at (fromX, fromY) must turn to in order to
    /// look at (toX, toY). Ties fall to the longer axis; a dead tie faces the
    /// horizontal, which is how the two houses meet across the ford.
    /// </summary>
    public static Quarter Toward(int fromX, int fromY, int toX, int toY)
    {
        int dx = toX - fromX;
        int dy = toY - fromY;

        if (dx == 0 && dy == 0) return Quarter.North;

        if (Mathf.Abs(dx) >= Mathf.Abs(dy))
            return dx >= 0 ? Quarter.East : Quarter.West;

        return dy >= 0 ? Quarter.South : Quarter.North;
    }

    /// <summary>
    /// From which quarter does the attacker's blow arrive, given where the
    /// defender is looking?
    ///
    ///   same quarter as the defender's facing  → the attacker is behind  → Back
    ///   the opposite quarter                   → the attacker is in view → Front
    ///   either of the two turns                → Side
    ///
    /// Read it slowly: a defender FACING North is looking at decreasing gridY.
    /// An attacker who also lies to the defender's North is in front of him.
    /// So "Toward the attacker equals my facing" means Front, not Rear.
    /// </summary>
    public static StrikeReckoner.Flank Quarters(UnitController attacker, UnitController defender)
    {
        if (attacker == null || defender == null) return StrikeReckoner.Flank.Front;

        Quarter fromWhere = Toward(defender.gridX, defender.gridY, attacker.gridX, attacker.gridY);
        return Compare(defender.facing, fromWhere);
    }

    /// <summary>Pure form, so the bench may prove it without a scene.</summary>
    public static StrikeReckoner.Flank Compare(Quarter defenderFacing, Quarter blowFrom)
    {
        int turn = ((int)blowFrom - (int)defenderFacing + 4) % 4;

        // 0 → the blow comes from the quarter the defender is watching.
        // 2 → the blow comes from directly behind him.
        // 1 or 3 → it comes across a shoulder.
        if (turn == 0) return StrikeReckoner.Flank.Front;
        if (turn == 2) return StrikeReckoner.Flank.Back;
        return StrikeReckoner.Flank.Side;
    }
}
