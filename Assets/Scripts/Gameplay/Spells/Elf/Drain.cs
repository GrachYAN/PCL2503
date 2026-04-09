using System.Collections.Generic;
using UnityEngine;

public class Drain : Spell
{
    public Drain()
    {
        SpellName = "Drain";
        Description = "Adjacent enemy loses 4 Mana";
        ManaCost = 3;
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
            // 目标失去4点法力值
            int manaLost = Mathf.Min(4, targetPiece.CurrentMana);
            targetPiece.LoseMana(4);
            Debug.Log($"{SpellName} 使 {targetPiece.PieceType} 失去了 {manaLost} 点法力值！");
        }
    }
}