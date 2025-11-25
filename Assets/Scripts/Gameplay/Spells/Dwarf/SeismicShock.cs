using System.Collections.Generic;
using UnityEngine;

public class SeismicShock : Spell
{
    private static readonly Vector2[] CardinalDirections =
    {
        new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1)
    };

    public SeismicShock()
    {
        SpellName = "Seismic Shock";
        Description = "Enemies on the same rank or file within 3 take 4 Arcane and are Rooted for 1 round.";
        ManaCost = 6;
        Cooldown = 4;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        return new List<Vector2> { Caster.GetCoordinates() };
    }

    protected override void ExecuteEffect(Vector2 targetSquare)
    {
        if (Caster == null || LogicManager == null)
        {
            return;
        }

        Vector2 casterPos = Caster.GetCoordinates();
        foreach (Vector2 dir in CardinalDirections)
        {
            for (int distance = 1; distance <= 3; distance++)
            {
                Vector2 pos = casterPos + dir * distance;
                if (!Caster.IsPositionWithinBoard(pos)) break;

                Piece blocker = LogicManager.boardMap[(int)pos.x, (int)pos.y];
                if (blocker != null)
                {
                    if (blocker.IsWhite != Caster.IsWhite)
                    {
                        blocker.TakeDamage(4, DamageType.Arcane);
                        blocker.ApplyRoot(1);
                    }
                    break;
                }
            }
        }
    }
}
