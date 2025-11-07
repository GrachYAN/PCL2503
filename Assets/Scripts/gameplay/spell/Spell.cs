using System.Collections.Generic;
using UnityEngine;

// 这个类不需要挂在任何GameObject上，它只是一个数据容器和逻辑处理器
[System.Serializable]
public abstract class Spell
{
    public string SpellName;
    public string Description;
    public int ManaCost;
    public int Cooldown; // 总冷却回合数 
    public int CurrentCooldown { get; protected set; } // 当前剩余冷却

    protected Piece Caster; // 施法者
    protected LogicManager LogicManager;

    public virtual void Initialize(Piece caster, LogicManager logicManager)
    {
        Caster = caster;
        LogicManager = logicManager;
        CurrentCooldown = 0; // 游戏开始时技能可用
    }

    /// <summary>
    /// 检查是否满足施法条件
    /// </summary>
    public virtual bool CanCast()
    {
        if (Caster.CurrentMana < ManaCost)
        {
            Debug.Log($"施法失败 {SpellName}：法力不足。需要 {ManaCost}，只有 {Caster.CurrentMana}");
            return false;
        }
        if (CurrentCooldown > 0)
        {
            Debug.Log($"施法失败 {SpellName}：技能冷却中。还需 {CurrentCooldown} 回合");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 获取该技能所有有效的目标格子 (由子类实现)
    /// </summary>
    public abstract List<Vector2> GetValidTargetSquares();

    /// <summary>
    /// 对选定的目标格子施法 (由子类实现)
    /// </summary>
    public abstract void Cast(Vector2 targetSquare);

    /// <summary>
    /// 在回合开始时调用，用于减少冷却
    /// </summary>
    public virtual void OnTurnStart()
    {
        if (CurrentCooldown > 0)
        {
            CurrentCooldown--;
        }
    }


    protected bool HasLineOfSight(Vector2 target)
    {
        // 委托 LogicManager 检查
        return LogicManager.HasLineOfSight(Caster.GetCoordinates(), target);
    }

    /// <summary>
    /// 辅助方法：施法成功后调用，用于消耗法力和设置冷却
    /// </summary>
    protected void CommitCast()
    {
        Caster.UseMana(ManaCost);
        CurrentCooldown = Cooldown;
    }
}