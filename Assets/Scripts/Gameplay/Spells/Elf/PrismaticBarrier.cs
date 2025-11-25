using System.Collections.Generic;
using UnityEngine;

public class PrismaticBarrier : Spell
{
    public PrismaticBarrier()
    {
        SpellName = "Prismatic Barrier";
        Description = "Places a barrier on a diagonal square for 3 rounds; it blocks enemy LoS.";
        ManaCost = 6;
        Cooldown = 5;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        List<Vector2> validTargets = new List<Vector2>();
        if (Caster == null) return validTargets;

        Vector2 startPos = Caster.GetCoordinates();
        Vector2[] directions = { new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(-1, -1) };

        foreach (var dir in directions)
        {
            for (int i = 1; i < 8; i++)
            {
                Vector2 targetPos = startPos + dir * i;
                if (!Caster.IsPositionWithinBoard(targetPos)) break; // 超出棋盘，换方向

                // 目标点必须是空的
                if (LogicManager.boardMap[(int)targetPos.x, (int)targetPos.y] == null)
                {
                    validTargets.Add(targetPos);
                }
                else
                {
                    // 遇到棋子，阻挡了路径，不能再往远放
                    break;
                }
            }
        }
        return validTargets;
    }

    protected override void ExecuteEffect(Vector2 target)
    {
        // 请求LogicManager放置一个持续3回合的屏障
        LogicManager.PlacePrismaticBarrier(target, 3);
    }
}