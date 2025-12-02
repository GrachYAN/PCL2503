using System.Collections.Generic;
using UnityEngine;

public class HammerSlam : Spell
{
    public HammerSlam()
    {
        SpellName = "Crystalline Push";
        Description = "Adjacent enemy takes 3 Physical damage.";
        ManaCost = 0;
        Cooldown = 0;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        List<Vector2> targets = new List<Vector2>();
        if (Caster == null || LogicManager == null)
        {
            return targets;
        }

        Vector2 casterPos = Caster.GetCoordinates();
        Vector2[] directions =
        {
            new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1),
            new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(-1, -1)
        };

        foreach (Vector2 dir in directions)
        {
            Vector2 targetPos = casterPos + dir;
            if (!Caster.IsPositionWithinBoard(targetPos))
            {
                continue;
            }

            Piece targetPiece = LogicManager.boardMap[(int)targetPos.x, (int)targetPos.y];
            if (targetPiece != null && targetPiece.IsWhite != Caster.IsWhite)
            {
                targets.Add(targetPos);
            }
        }

        return targets;
    }

    protected override void ExecuteEffect(Vector2 targetSquare)
    {
        Piece target = LogicManager.boardMap[(int)targetSquare.x, (int)targetSquare.y];
        if (target != null && target.IsWhite != Caster.IsWhite)
        {
            target.TakeDamage(3, DamageType.Physical);
        }
    }
}
