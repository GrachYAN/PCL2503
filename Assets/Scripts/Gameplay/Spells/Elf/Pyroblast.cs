using System.Collections.Generic;
using UnityEngine;

public class Pyroblast : Spell
{
    public Pyroblast()
    {
        SpellName = "Pyroblast";
        Description = "Deals 10 FR to a single target";
        ManaCost = 8;
        Cooldown = 5;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        List<Vector2> validTargets = new List<Vector2>();

        if (Caster == null || LogicManager == null)
        {
            return validTargets;
        }

        Vector2 casterPos = Caster.GetCoordinates();

        // 遍历整个棋盘寻找有效目标
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                Vector2 targetPos = new Vector2(x, y);

                // 跳过施法者自己的位置
                if (targetPos == casterPos)
                {
                    continue;
                }

                Piece targetPiece = LogicManager.boardMap[x, y];

                // 必须有敌方棋子
                if (targetPiece == null || targetPiece.IsWhite == Caster.IsWhite)
                {
                    continue;
                }

                // 检查视线
                if (LogicManager.HasLineOfSight(casterPos, targetPos))
                {
                    validTargets.Add(targetPos);
                }
            }
        }

        return validTargets;
    }

    protected override void ExecuteEffect(Vector2 targetSquare)
    {
        if (LogicManager == null)
        {
            Debug.LogError($"{SpellName}: LogicManager 为空!");
            return;
        }

        Piece targetPiece = LogicManager.boardMap[(int)targetSquare.x, (int)targetSquare.y];

        if (targetPiece == null)
        {
            Debug.LogWarning($"{SpellName}: 目标位置 ({targetSquare.x}, {targetSquare.y}) 没有棋子");
            return;
        }

        // 造成 10 点火焰伤害
        int finalDamage = 10 + Caster.DamageBonus;
        targetPiece.TakeDamage(finalDamage, DamageType.Fire);

        Debug.Log($"{Caster.PieceType} 对 {targetPiece.PieceType} 施放了 {SpellName}，造成 10 点火焰伤害!");

        // TODO: 播放火焰特效
        // PlayFireEffect(targetSquare);
    }

    // 可选: 如果需要自定义验证逻辑
    public override bool IsCastDataValid(SpellCastData data)
    {
        Vector2 target = new Vector2(data.PrimaryX, data.PrimaryY);

        // 检查目标是否在有效列表中
        if (!GetValidTargetSquares().Contains(target))
        {
            Debug.LogError($"{SpellName}: 无效的目标 ({data.PrimaryX}, {data.PrimaryY})");
            return false;
        }

        // 确保目标位置有敌方棋子
        Piece targetPiece = LogicManager.boardMap[data.PrimaryX, data.PrimaryY];
        if (targetPiece == null || targetPiece.IsWhite == Caster.IsWhite)
        {
            Debug.LogError($"{SpellName}: 目标不是敌方棋子");
            return false;
        }

        return true;
    }
}
