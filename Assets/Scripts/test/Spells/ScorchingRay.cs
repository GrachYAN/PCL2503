using System.Collections.Generic;
using UnityEngine;

public class ScorchingRay : Spell
{
    public ScorchingRay()
    {
        SpellName = "Scorching Ray";
        Description = "Diagonal ray; first enemy hit takes 4 FR, the rest takes 2 FR.";
        ManaCost = 4;
        Cooldown = 2;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        List<Vector2> validTargets = new List<Vector2>();
        Vector2 casterPos = Caster.GetCoordinates();

        int[] directionsX = { 1, 1, -1, -1 };
        int[] directionsY = { 1, -1, 1, -1 };

        for (int i = 0; i < 4; i++)
        {
            int step = 1;
            while (true)
            {
                Vector2 targetPos = new Vector2(casterPos.x + step * directionsX[i], casterPos.y + step * directionsY[i]);

                if (!Caster.IsPositionWithinBoard(targetPos)) break;

                Piece pieceAtTarget = LogicManager.boardMap[(int)targetPos.x, (int)targetPos.y];
                if (pieceAtTarget != null)
                {
                    if (pieceAtTarget.IsWhite != Caster.IsWhite)
                    {
                        validTargets.Add(targetPos);
                    }
                    break;
                }
                step++;
            }
        }
        return validTargets;
    }

    protected override void ExecuteEffect(Vector2 target)
    {
        Vector2 casterPos = Caster.GetCoordinates();
        Vector2 direction = (target - casterPos).normalized;
        direction = new Vector2(Mathf.Round(direction.x), Mathf.Round(direction.y)); // 确保是标准的对角线方向

        // 第一个目标受到4点伤害
        Piece firstTargetPiece = LogicManager.boardMap[(int)target.x, (int)target.y];
        if (firstTargetPiece != null)
        {
            firstTargetPiece.TakeDamage(4);
        }

        // 沿射线继续对后续目标造成伤害
        int step = 1;
        while (true)
        {
            Vector2 nextPos = target + (direction * step);
            if (!Caster.IsPositionWithinBoard(nextPos)) break;

            Piece nextPiece = LogicManager.boardMap[(int)nextPos.x, (int)nextPos.y];
            if (nextPiece != null)
            {
                if (nextPiece.IsWhite != Caster.IsWhite)
                {
                    nextPiece.TakeDamage(2);
                }
                else
                {
                    break; // 射线被友方单位阻挡
                }
            }
            step++;
        }
    }
}