using System.Collections.Generic;
using UnityEngine;

public class PrismaticBarrier : Spell
{
    public PrismaticBarrier()
    {
        SpellName = "Prismatic Barrier";
        Description = "Places a barrier on a diagonal square for 3 rounds; it blocks enemy LoS.";
        ManaCost = 6;
        Cooldown = 0;
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
                if (LogicManager.HasAnyPrismaticBarrierAt(targetPos)) break;
                if (!Caster.IsPositionWithinBoard(targetPos)) break; // �������̣�������

                // Ŀ�������ǿյ�
                if (LogicManager.boardMap[(int)targetPos.x, (int)targetPos.y] == null)
                {
                    validTargets.Add(targetPos);
                }
                else
                {
                    // �������ӣ��赲��·������������Զ��
                    break;
                }
            }
        }
        return validTargets;
    }

    protected override void ExecuteEffect(Vector2 target)
    {
        // ����LogicManager����һ������3�غϵ�����
        LogicManager.PlacePrismaticBarrier(target, 3, Caster.IsWhite);
    }
}
