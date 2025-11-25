using System.Collections.Generic;
using UnityEngine;

public class GemstoneSmash : Spell
{
    private static readonly Vector2[] AdjacentOffsets =
    {
        new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1),
        new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(-1, -1)
    };

    public GemstoneSmash()
    {
        SpellName = "Gemstone Smash";
        Description = "Deal 9 Holy to an adjacent enemy and heal 6 HP.";
        ManaCost = 6;
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
        foreach (Vector2 offset in AdjacentOffsets)
        {
            Vector2 pos = casterPos + offset;
            if (!Caster.IsPositionWithinBoard(pos)) continue;
            Piece piece = LogicManager.boardMap[(int)pos.x, (int)pos.y];
            if (piece != null && piece.IsWhite != Caster.IsWhite)
            {
                targets.Add(pos);
            }
        }

        return targets;
    }

    protected override void ExecuteEffect(Vector2 targetSquare)
    {
        Piece target = LogicManager.boardMap[(int)targetSquare.x, (int)targetSquare.y];
        if (target != null && target.IsWhite != Caster.IsWhite)
        {
            target.TakeDamage(9, DamageType.Holy);
            Caster.Heal(6);
        }
    }
}
