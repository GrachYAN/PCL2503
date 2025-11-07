using System.Collections.Generic;
using UnityEngine;

public class Drain : Spell
{
    public Drain()
    {
        SpellName = "Drain";
        Description = "Adjacent enemy loses 4 Mana.";
        ManaCost = 3;
        Cooldown = 3;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        List<Vector2> targets = new List<Vector2>();
        Vector2 currentCoords = Caster.GetCoordinates();
        // 检查所有 8 个相邻的格子
        int[] dx = { 0, 0, 1, -1, 1, -1, 1, -1 };
        int[] dy = { 1, -1, 0, 0, 1, -1, -1, 1 };

        for (int i = 0; i < 8; i++)
        {
            Vector2 targetPos = new Vector2(currentCoords.x + dx[i], currentCoords.y + dy[i]);

            if (Caster.IsPositionWithinBoard(targetPos))
            {
                Piece targetPiece = LogicManager.boardMap[(int)targetPos.x, (int)targetPos.y];
                // 必须是敌方棋子
                if (targetPiece != null && targetPiece.IsWhite != Caster.IsWhite)
                {
                    targets.Add(targetPos);
                }
            }
        }
        return targets;
    }

    public override void Cast(Vector2 targetSquare)
    {
        // 先检查是否能施法 (法力/冷却)
        if (!CanCast()) return;

        Piece targetPiece = LogicManager.boardMap[(int)targetSquare.x, (int)targetSquare.y];
        if (targetPiece != null)
        {
            // 消耗施法者法力
            if (Caster.UseMana(ManaCost))
            {
                // 目标失去 4 法力
                targetPiece.LoseMana(4);

                // 设置冷却
                CurrentCooldown = Cooldown;

                Debug.Log($"{Caster.PieceType} 使用 {SpellName} 成功，{targetPiece.PieceType} 失去 4 法力！");
            }
        }
    }
}