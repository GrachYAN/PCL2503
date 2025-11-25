using System.Collections.Generic;
using UnityEngine;

public class PhoenixDive : Spell
{
    public PhoenixDive()
    {
        SpellName = "Phoenix Dive";
        Description = "Place a Flame Mark up to 3 squares in any one direction ignoring LoS. The enemy takes a stun for 3 rounds and burns (DoT=3) for 3 rounds.";
        ManaCost = 5;
        Cooldown = 4;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        List<Vector2> validTargets = new List<Vector2>();
        if (Caster == null) return validTargets;

        Vector2 startPos = Caster.GetCoordinates();
        // 8个方向
        Vector2[] directions = {
            new Vector2(0, 1), new Vector2(0, -1), new Vector2(1, 0), new Vector2(-1, 0),
            new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(-1, -1)
        };

        foreach (var dir in directions)
        {
            for (int i = 1; i <= 3; i++) // 最远3格
            {
                Vector2 targetPos = startPos + dir * i;
                if (!Caster.IsPositionWithinBoard(targetPos)) break;

                // 目标必须是敌方单位 (忽略LoS)
                Piece targetPiece = LogicManager.boardMap[(int)targetPos.x, (int)targetPos.y];
                if (targetPiece != null && targetPiece.IsWhite != Caster.IsWhite)
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
            // 施加效果
            targetPiece.ApplyStun(3); // 眩晕3回合
            targetPiece.ApplyDamageOverTime(3, DamageType.Fire, 3); // 3点火焰伤害，持续3回合

            // 在LogicManager中生成视觉标记
            LogicManager.PlaceFlameMark(target, 3);
        }
    }
}