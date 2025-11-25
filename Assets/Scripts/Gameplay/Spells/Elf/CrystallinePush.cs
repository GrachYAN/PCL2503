using System.Collections.Generic;
using UnityEngine;

public class CrystallinePush : Spell
{
    public CrystallinePush()
    {
        SpellName = "Crystalline Push";
        Description = "Deal 3 Arcane damage to an adjacent enemy";
        ManaCost = 0;
        Cooldown = 0;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        List<Vector2> validTargets = new List<Vector2>();

        // 检查所有相邻格子（8方向）
        Vector2[] directions = {
            new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1),
            new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(-1, -1)
        };

        Vector2 casterPos = Caster.GetCoordinates();

        foreach (Vector2 dir in directions)
        {
            Vector2 targetPos = casterPos + dir;

            // 检查是否在棋盘范围内
            if (!Caster.IsPositionWithinBoard(targetPos))
                continue;

            // 检查目标位置是否有敌方棋子
            Piece targetPiece = LogicManager.boardMap[(int)targetPos.x, (int)targetPos.y];
            if (targetPiece != null && targetPiece.IsWhite != Caster.IsWhite)
            {
                validTargets.Add(targetPos);
            }
        }

        return validTargets;
    }

    protected override void ExecuteEffect(Vector2 targetSquare)
    {
        Piece targetPiece = LogicManager.boardMap[(int)targetSquare.x, (int)targetSquare.y];

        if (targetPiece != null && targetPiece.IsWhite != Caster.IsWhite)
        {
            // 造成3点奥术伤害
            int finalDamage = 3 + Caster.DamageBonus;
            targetPiece.TakeDamage(finalDamage, DamageType.Arcane);
            Debug.Log($"{SpellName} 对 {targetPiece.PieceType} 造成了 3 点奥术伤害！");
        }
    }
}