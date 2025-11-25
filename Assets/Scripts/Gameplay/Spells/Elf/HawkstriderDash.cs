using System.Collections.Generic;
using UnityEngine;

public class HawkstriderDash : Spell
{
    public HawkstriderDash()
    {
        SpellName = "Hawkstrider Dash";
        Description = "Jump to a target square like a knight, dealing 5 Fire damage to all adjacent enemies upon landing.";
        ManaCost = 3;
        Cooldown = 2;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        List<Vector2> validTargets = new List<Vector2>();
        Vector2 currentPos = Caster.GetCoordinates();

        Vector2[] moves = {
            new Vector2(1, 2), new Vector2(1, -2), new Vector2(-1, 2), new Vector2(-1, -2),
            new Vector2(2, 1), new Vector2(2, -1), new Vector2(-2, 1), new Vector2(-2, -1)
        };

        foreach (var move in moves)
        {
            Vector2 targetPos = currentPos + move;
            if (Caster.IsPositionWithinBoard(targetPos))
            {
                Piece targetPiece = LogicManager.boardMap[(int)targetPos.x, (int)targetPos.y];
                if (targetPiece == null || targetPiece.IsWhite != Caster.IsWhite)
                {
                    validTargets.Add(targetPos);
                }
            }
        }
        return validTargets;
    }

    protected override void ExecuteEffect(Vector2 target)
    {
        // 1. 对落点周围的敌人造成伤害
        Vector2[] directions = {
            new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1),
            new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(-1, -1)
        };

        int finalDamage = 5 + Caster.DamageBonus;

        foreach (var dir in directions)
        {
            Vector2 adjacentPos = target + dir;
            if (Caster.IsPositionWithinBoard(adjacentPos))
            {
                Piece adjacentPiece = LogicManager.boardMap[(int)adjacentPos.x, (int)adjacentPos.y];
                if (adjacentPiece != null && adjacentPiece.IsWhite != Caster.IsWhite)
                {
                    adjacentPiece.TakeDamage(finalDamage, DamageType.Fire);
                }
            }
        }

        // 2. 将施法者移动到目标位置
        Caster.Move(target);
    }
}