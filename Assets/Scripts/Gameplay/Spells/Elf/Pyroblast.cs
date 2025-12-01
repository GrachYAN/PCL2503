using System.Collections.Generic;
using UnityEngine;

public class Pyroblast : Spell
{
    public Pyroblast()
    {
        SpellName = "Pyroblast";
        Description = "Deals 10 FR to a single target in a straight line (blocked by obstacles).";
        ManaCost = 8;
        Cooldown = 5;

        // 炎爆术是攻击技能，释放后结束回合
        EndsTurn = true;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        List<Vector2> validTargets = new List<Vector2>();

        if (Caster == null || LogicManager == null) return validTargets;

        // 1. 获取施法者坐标
        Vector2 casterPosVec = Caster.GetCoordinates();
        Vector2Int startPos = new Vector2Int(Mathf.RoundToInt(casterPosVec.x), Mathf.RoundToInt(casterPosVec.y));

        // 2. 定义四个方向 (上, 下, 左, 右) - 即 Rook 的移动方向
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0, 1),  // Up
            new Vector2Int(0, -1), // Down
            new Vector2Int(-1, 0), // Left
            new Vector2Int(1, 0)   // Right
        };

        // 3. 向每个方向进行射线检测
        foreach (Vector2Int dir in directions)
        {
            // 假设棋盘最大尺寸是 8x8，步长从 1 开始延伸
            for (int distance = 1; distance < 8; distance++)
            {
                Vector2Int checkPos = startPos + (dir * distance);
                Vector2 checkPosVec = new Vector2(checkPos.x, checkPos.y);

                // A. 检查边界：如果超出棋盘，停止该方向搜索
                if (!Caster.IsPositionWithinBoard(checkPosVec))
                {
                    break;
                }

                // B. 获取该位置的棋子
                Piece hitPiece = LogicManager.boardMap[checkPos.x, checkPos.y];

                if (hitPiece != null)
                {
                    // C. 视线被阻挡（无论是敌是友，视线都到此为止）

                    // 只有当它是敌人时，才算作有效目标
                    if (hitPiece.IsWhite != Caster.IsWhite)
                    {
                        validTargets.Add(checkPosVec);
                    }

                    // 遇到任何棋子（障碍物），停止向该方向继续延伸
                    break;
                }

                // D. 如果是空地 (hitPiece == null)，循环继续，检查下一个格子
            }
        }

        return validTargets;
    }

    protected override void ExecuteEffect(Vector2 targetSquare)
    {
        if (LogicManager == null) return;

        int x = Mathf.RoundToInt(targetSquare.x);
        int y = Mathf.RoundToInt(targetSquare.y);

        Piece targetPiece = LogicManager.boardMap[x, y];

        if (targetPiece != null)
        {
            int finalDamage = 10 + Caster.DamageBonus;
            targetPiece.TakeDamage(finalDamage, DamageType.Fire);

            Debug.Log($"Pyroblast hit {targetPiece.PieceType} for {finalDamage} damage!");

            // TODO: 可以在这里播放特效
            // PlayFireEffect(targetSquare);
        }
    }
}
