using System.Collections.Generic;
using UnityEngine;

public class Smite : Spell
{
    private static readonly Vector2[] DiagonalDirections =
    {
        new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(-1, -1)
    };

    public Smite()
    {
        SpellName = "Smite";
        Description = "Diagonal ray up to 3. First enemy hit takes 5 Holy and is Dazed.";
        ManaCost = 5;
        Cooldown = 3;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        List<Vector2> targets = new List<Vector2>();
        if (Caster == null || LogicManager == null)
        {
            return targets;
        }

        Vector2 casterPos = Caster.GetCoordinates();
        foreach (Vector2 dir in DiagonalDirections)
        {
            for (int distance = 1; distance <= 3; distance++)
            {
                Vector2 targetPos = casterPos + dir * distance;
                if (!Caster.IsPositionWithinBoard(targetPos)) break;

                if (!LogicManager.HasLineOfSight(casterPos, targetPos))
                {
                    break;
                }

                Piece piece = LogicManager.boardMap[(int)targetPos.x, (int)targetPos.y];
                if (piece != null)
                {
                    if (piece.IsWhite != Caster.IsWhite)
                    {
                        targets.Add(targetPos);
                    }
                    break;
                }
            }
        }

        return targets;
    }

    protected override void ExecuteEffect(Vector2 targetSquare)
    {
        Piece target = LogicManager.boardMap[(int)targetSquare.x, (int)targetSquare.y];
        if (target != null && target.IsWhite != Caster.IsWhite)
        {
            target.TakeDamage(5, DamageType.Holy);
            target.ApplyDaze();
        }
    }
}
