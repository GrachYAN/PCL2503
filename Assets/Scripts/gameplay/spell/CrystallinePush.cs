using System.Collections.Generic;
using UnityEngine;

public class CrystallinePush : Spell
{
    public CrystallinePush()
    {
        SpellName = "Crystalline Push";
        Description = "Deal 3 Arcane damage to an adjacent target.";
        ManaCost = 0;
        Cooldown = 0;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        List<Vector2> targets = new List<Vector2>();
        Vector2 currentCoords = Caster.GetCoordinates();
        // 上、下、左、右 四个方向的偏移
        int[] dx = { 0, 0, 1, -1 };
        int[] dy = { 1, -1, 0, 0 };

        for (int i = 0; i < 4; i++)
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
        if (!CanCast()) return;

        Piece targetPiece = LogicManager.boardMap[(int)targetSquare.x, (int)targetSquare.y];
        if (targetPiece != null)
        {
            // 造成 3 点伤害
            targetPiece.TakeDamage(3);

            // 消耗法力并设置冷却 (即使是 0/0)
            Caster.UseMana(ManaCost);
            CurrentCooldown = Cooldown;

            Debug.Log($"{Caster.PieceType} 使用 {SpellName} 对 {targetPiece.PieceType} 造成 3 点伤害！");
        }
    }
}