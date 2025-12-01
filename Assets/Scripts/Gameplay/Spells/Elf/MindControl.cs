/*
using System.Collections.Generic;
using UnityEngine;

public class MindControl : Spell
{
    public MindControl()
    {
        SpellName = "Mind Control";
        Description = "Controls the mind of an adjacent enemy for one round.";
        ManaCost = 6;
        Cooldown = 3;

        EndsTurn = false;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        List<Vector2> validTargets = new List<Vector2>();
        if (Caster == null) return validTargets;
        Vector2 currentPos = Caster.GetCoordinates();

        // 检查周围8个方向
        Vector2[] adjacentDirections = {
            new Vector2(0, 1), new Vector2(0, -1), new Vector2(1, 0), new Vector2(-1, 0),
            new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(-1, -1)
        };

        foreach (var dir in adjacentDirections)
        {
            Vector2 targetPos = currentPos + dir;
            if (Caster.IsPositionWithinBoard(targetPos))
            {
                Piece targetPiece = LogicManager.boardMap[(int)targetPos.x, (int)targetPos.y];
                // 目标必须是敌方棋子，且不能是国王
                if (targetPiece != null && targetPiece.IsWhite != Caster.IsWhite && targetPiece.PieceType != "King" && targetPiece.IsWhite != Caster.IsWhite && targetPiece.PieceType != "Queen")
                {
                    validTargets.Add(targetPos);
                }
            }
        }
        return validTargets;
    }

    protected override void ExecuteEffect(Vector2 target)
    {
        Piece targetPiece = LogicManager.boardMap[(int)target.x, (int)target.y];
        if (targetPiece != null)
        {
            // 调用LogicManager来处理效果
            LogicManager.ApplyMindControl(Caster, targetPiece);
        }
    }
}
*/
using System.Collections.Generic;
using UnityEngine;

public class MindControl : Spell
{
    public MindControl()
    {
        SpellName = "Mind Control";
        Description = "Permanently take control of an adjacent enemy. You can act with them immediately.";
        ManaCost = 6;
        Cooldown = 3;

        // 关键修改：施法后不立即结束回合，让你有机会操作新棋子
        EndsTurn = false;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        return ConvertToVector2(GetAdjacentEnemies());
    }

    public override List<Vector2> GetCurrentValidSquares()
    {
        // 高亮所有相邻的敌人
        return ConvertToVector2(GetAdjacentEnemies());
    }

    public override bool TryHandleTargetSelection(Vector2 targetSquare, out bool castComplete)
    {
        castComplete = false;
        Vector2Int gridTarget = Vector2Int.RoundToInt(targetSquare);

        // 只要点击的是相邻敌人，就直接施法成功
        if (GetAdjacentEnemies().Contains(gridTarget))
        {
            castComplete = true;
            return true;
        }

        return false;
    }

    public override SpellCastData GetCastData(Vector2 finalTarget)
    {
        // 只需要记录目标是谁
        return SpellCastData.FromSingleTarget(finalTarget);
    }

    public override void ApplyCastData(SpellCastData data)
    {
        // 不需要额外的数据解包，ExecuteEffect 直接用 targetSquare
    }

    protected override void ExecuteEffect(Vector2 targetSquare)
    {
        if (Caster == null || LogicManager == null) return;

        Vector2Int gridPos = Vector2Int.RoundToInt(targetSquare);
        Piece targetPiece = LogicManager.boardMap[gridPos.x, gridPos.y];

        if (targetPiece != null)
        {
            // 1. 执行控制逻辑
            LogicManager.ApplyMindControl(Caster, targetPiece);

            // 2. 耗尽施法者自己的行动力
            // 因为 EndsTurn = false，系统不会自动结束回合。
            // 我们需要防止玩家用施法者放完技能后，施法者自己又跑了。
            // 强制设为已移动状态 (假设 1 代表已移动)
            // 注意：Piece 类里 HasMoved 是 int，且 set 是 private，
            // 你可能需要在 Piece.cs 里加一个 public 方法 SetHasMoved(int val)
            // 或者我们依靠 LogicManager 来处理。
            // 这里我们假设 LogicManager.ApplyMindControl 会处理好回合逻辑。
        }
    }

    private List<Vector2Int> GetAdjacentEnemies()
    {
        List<Vector2Int> targets = new List<Vector2Int>();
        if (Caster == null || LogicManager == null) return targets;

        Vector2Int casterPos = Vector2Int.RoundToInt(Caster.GetCoordinates());
        Vector2Int[] directions = {
            new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };

        foreach (var dir in directions)
        {
            Vector2Int checkPos = casterPos + dir;
            if (Caster.IsPositionWithinBoard(new Vector2(checkPos.x, checkPos.y)))
            {
                Piece p = LogicManager.boardMap[checkPos.x, checkPos.y];
                // 排除国王和王后，防止游戏直接崩坏
                if (p != null && p.IsWhite != Caster.IsWhite && p.PieceType != "King" && p.PieceType != "Queen")
                {
                    targets.Add(checkPos);
                }
            }
        }
        return targets;
    }

    private List<Vector2> ConvertToVector2(List<Vector2Int> ints)
    {
        List<Vector2> result = new List<Vector2>();
        foreach (Vector2Int value in ints)
        {
            result.Add(new Vector2(value.x, value.y));
        }
        return result;
    }
}
