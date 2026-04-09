
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Enum representing possible reasons a spell cannot be cast.
/// </summary>
public enum SpellCastFailReason
{
    None,           // No failure, spell can be cast
    NotEnoughMana,  // Insufficient mana
    OnCooldown,     // Spell is on cooldown
    InvalidTarget,  // Target is not valid
    Stunned,        // Caster is stunned
    NoCaster        // No caster assigned
}

public struct SpellCastData : INetworkSerializable
{
    public int PrimaryX;
    public int PrimaryY;
    public int SecondaryX;
    public int SecondaryY;
    public int TertiaryX;
    public int TertiaryY;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref PrimaryX);
        serializer.SerializeValue(ref PrimaryY);
        serializer.SerializeValue(ref SecondaryX);
        serializer.SerializeValue(ref SecondaryY);
        serializer.SerializeValue(ref TertiaryX);
        serializer.SerializeValue(ref TertiaryY);
    }

    public static SpellCastData FromSingleTarget(Vector2 target)
    {
        return new SpellCastData
        {
            PrimaryX = Mathf.RoundToInt(target.x),
            PrimaryY = Mathf.RoundToInt(target.y),
            SecondaryX = -1,
            SecondaryY = -1,
            TertiaryX = -1,
            TertiaryY = -1
        };
    }

    public Vector2Int GetPrimary() => new Vector2Int(PrimaryX, PrimaryY);
    public Vector2Int GetSecondary() => new Vector2Int(SecondaryX, SecondaryY);
    public Vector2Int GetTertiary() => new Vector2Int(TertiaryX, TertiaryY);
}

[System.Serializable]
public abstract class Spell
{
    public string SpellName;
    public string Description;
    public int ManaCost;
    public int Cooldown;
    public int CurrentCooldown { get; protected set; }
    public bool EndsTurn = true;

    protected Piece Caster;
    protected LogicManager LogicManager;

    public virtual void Initialize(Piece caster, LogicManager logicManager)
    {
        Caster = caster;
        LogicManager = logicManager;
        CurrentCooldown = 0;
    }

    protected virtual int GetEffectiveManaCost()
    {
        if (Caster == null) return ManaCost;

        int effectiveCost = ManaCost + Caster.GetAdditionalSpellManaCost();
        return Mathf.Max(0, effectiveCost);
    }

    protected virtual int GetEffectiveCooldown()
    {
        if (Caster == null) return Cooldown;
        return Mathf.Max(0, Cooldown + Caster.GetAdditionalSpellCooldown());
    }

    public virtual bool CanCast()
    {
        return GetCastFailReason() == SpellCastFailReason.None;
    }

    /// <summary>
    /// Returns the specific reason why this spell cannot be cast, or None if it can be cast.
    /// Priority order: NoCaster > Stunned > OnCooldown > NotEnoughMana
    /// </summary>
    public virtual SpellCastFailReason GetCastFailReason()
    {
        if (Caster == null)
        {
            return SpellCastFailReason.NoCaster;
        }

        if (Caster.IsStunned)
        {
            Debug.Log($"施法失败 {SpellName}：{Caster.PieceType} 处于眩晕状态");
            return SpellCastFailReason.Stunned;
        }

        // Check cooldown FIRST (higher priority than mana)
        if (CurrentCooldown > 0)
        {
            Debug.Log($"施法失败 {SpellName}：正在冷却。还需 {CurrentCooldown} 回合");
            return SpellCastFailReason.OnCooldown;
        }

        int manaCost = GetEffectiveManaCost();

        if (Caster.CurrentMana < manaCost)
        {
            Debug.Log($"施法失败 {SpellName}：法力不足。需要 {manaCost}，只有 {Caster.CurrentMana}");
            return SpellCastFailReason.NotEnoughMana;
        }

        return SpellCastFailReason.None;
    }

    public abstract List<Vector2> GetValidTargetSquares();

    public virtual void BeginTargeting() { }

    public virtual List<Vector2> GetCurrentValidSquares()
    {
        return GetValidTargetSquares();
    }

    public virtual bool TryHandleTargetSelection(Vector2 targetSquare, out bool castComplete)
    {
        castComplete = true;
        return true;
    }

    public virtual void CancelTargeting() { }

    public virtual SpellCastData GetCastData(Vector2 finalTarget)
    {
        return SpellCastData.FromSingleTarget(finalTarget);
    }

    public virtual void ApplyCastData(SpellCastData data) { }

    public virtual bool IsCastDataValid(SpellCastData data)
    {
        Vector2 primary = new Vector2(data.PrimaryX, data.PrimaryY);
        return GetValidTargetSquares().Contains(primary);
    }

    public virtual void Cast(Vector2 targetSquare)
    {
        if (!CanCast()) return;

        int manaCost = GetEffectiveManaCost();
        int cooldown = GetEffectiveCooldown();

        if (!Caster.UseMana(manaCost))
        {
            Debug.Log($"{Caster.PieceType} 法力不足，无法施放 {SpellName}");
            return;
        }

        CurrentCooldown = cooldown;

        // Suppress move sound during spell execution
        // (spells that move AND deal damage should only play damage sound)
        if (GameSoundManager.Instance != null)
        {
            GameSoundManager.Instance.BeginSpellExecution();
        }

        // 执行技能效果
        ExecuteEffect(targetSquare);

        // Re-enable move sound after spell execution
        if (GameSoundManager.Instance != null)
        {
            GameSoundManager.Instance.EndSpellExecution();
        }

        Debug.Log($"{Caster.PieceType} 使用了 {SpellName}！");

        Caster.OnSpellCast();
    }

    protected abstract void ExecuteEffect(Vector2 targetSquare);

    public virtual void OnTurnStart()
    {
        if (CurrentCooldown > 0)
        {
            CurrentCooldown--;
        }
    }
}
