using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public abstract class Spell
{
    public string SpellName;
    public string Description;
    public int ManaCost;
    public int Cooldown;
    public int CurrentCooldown { get; protected set; }

    protected Piece Caster;
    protected LogicManager LogicManager;

    public virtual void Initialize(Piece caster, LogicManager logicManager)
    {
        Caster = caster;
        LogicManager = logicManager;
        CurrentCooldown = 0;
    }

    public virtual bool CanCast()
    {
        if (Caster.CurrentMana < ManaCost)
        {
            Debug.Log($"施法失败 {SpellName}：法力不足。需要 {ManaCost}，只有 {Caster.CurrentMana}");
            return false;
        }
        if (CurrentCooldown > 0)
        {
            Debug.Log($"施法失败 {SpellName}：正在冷却。还需 {CurrentCooldown} 回合");
            return false;
        }

        return true;
    }

    public abstract List<Vector2> GetValidTargetSquares();

    public virtual void Cast(Vector2 targetSquare)
    {
        if (!CanCast()) return;

        // 消耗法力
        Caster.UseMana(ManaCost);

        // 设置冷却
        CurrentCooldown = Cooldown;

        // 执行技能效果
        ExecuteEffect(targetSquare);

        Debug.Log($"{Caster.PieceType} 使用了 {SpellName}！");
    }

    protected abstract void ExecuteEffect(Vector2 targetSquare);

    // ⭐ 新增：减少冷却
    public virtual void OnTurnStart()
    {
        if (CurrentCooldown > 0)
        {
            CurrentCooldown--;
        }
    }
}

