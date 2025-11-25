using System.Collections.Generic;
using UnityEngine;

public class BarrierPiece : Piece
{
    private int lifeTimeRounds; // 存活回合数

    public void InitializeBarrier(int duration)
    {
        // 屏障初始化逻辑
        // 阵营设为 Elf，确保 LogicManager 认为它是棋子
        Initialize("Barrier", true, Faction.Elf);
        lifeTimeRounds = duration;

        // 屏障通常没有法力，血量可以设高一点防止被误伤，或者在 TakeDamage 里特判
        // 由于你在 LogicManager 中 DestroyPiece 做了检查，屏障不会进墓地，这点很好
    }

    public override List<Vector2> GetAttackedFields()
    {
        return new List<Vector2>(); // 屏障不攻击任何人
    }

    protected override List<Vector2> GetPotentialMoves()
    {
        return new List<Vector2>(); // 屏障不能移动
    }

    // 必须重写，防止屏障去计算“是否导致国王被将军”等逻辑
    public override List<Vector2> GetLegalMoves()
    {
        return new List<Vector2>();
    }

    public override void OnTurnStart(bool activeTurnIsWhite)
    {
        // 屏障随时间自动消亡
        // 为了简化，假设每次轮到施法者阵营开始时减少寿命
        if (IsWhite == activeTurnIsWhite)
        {
            lifeTimeRounds--;
            if (lifeTimeRounds <= 0)
            {
                Debug.Log("屏障自然消失了。");
                // 调用 LogicManager 销毁自己
                logicManager.DestroyPiece(this);
            }
        }
    }
}