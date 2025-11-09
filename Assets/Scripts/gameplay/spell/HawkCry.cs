using System.Collections.Generic;
using UnityEngine;

public class HawkCry : Spell
{
    public HawkCry()
    {
        SpellName = "Hawk Cry";
        Description = "All adjacent enemies get Dazed. (Daze: next spell costs +3 Mana and +2 CD)";
        ManaCost = 4;
        Cooldown = 3;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        // 这是一个以自身为中心范围效果法术，返回施法者自己的格子作为目标。
        return new List<Vector2> { Caster.GetCoordinates() };
    }

    protected override void ExecuteEffect(Vector2 target)
    {
        // 效果应用于施法者周围的敌人
        Vector2 casterPos = Caster.GetCoordinates();
        Vector2[] directions = {
            new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1),
            new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(-1, -1)
        };

        foreach (var dir in directions)
        {
            Vector2 adjacentPos = casterPos + dir;
            if (Caster.IsPositionWithinBoard(adjacentPos))
            {
                Piece adjacentPiece = LogicManager.boardMap[(int)adjacentPos.x, (int)adjacentPos.y];
                if (adjacentPiece != null && adjacentPiece.IsWhite != Caster.IsWhite)
                {
                    // TODO: 实现"Dazed"状态效果的核心逻辑。
                    Debug.Log($"{adjacentPiece.PieceType} at {adjacentPos} is Dazed!");
                    // adjacentPiece.ApplyDaze(); // 占位符
                }
            }
        }
    }
}