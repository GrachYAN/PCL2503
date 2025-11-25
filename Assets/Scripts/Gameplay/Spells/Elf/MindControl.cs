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