﻿using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public abstract class Piece : MonoBehaviour
{
    protected LogicManager logicManager;
    public bool IsWhite { get; private set; }
    [field: SerializeField] public string PieceType { get; private set; }
    public int HasMoved { get; private set; }
    public Faction PieceFaction { get; private set; }
    public Faction ResolvedFaction { get; private set; }
    public int MaxHP { get; private set; }
    public int CurrentHP { get; private set; }
    public int MaxMana { get; private set; }
    public int CurrentMana { get; private set; }
    public List<Spell> Spells = new List<Spell>();

    public int DamageBonus { get; private set; }

    private int shieldValue;
    private int stunDuration;
    private int rootDuration;
    private int dazeStacks;

    private class DamageOverTimeEffect
    {
        public int Damage;
        public DamageType DamageType;
        public int Duration;
    }

    private readonly List<DamageOverTimeEffect> damageOverTimeEffects = new List<DamageOverTimeEffect>();
    protected readonly Dictionary<Vector2, Vector2> heartBonusTargetMap = new Dictionary<Vector2, Vector2>();

    public bool IsStunned => stunDuration > 0;
    public bool IsRooted => rootDuration > 0;

    public abstract List<Vector2> GetAttackedFields();
    protected abstract List<Vector2> GetPotentialMoves();
    public virtual List<Vector2> GetLegalMoves()
    {
        heartBonusTargetMap.Clear();

        List<Vector2> legalMoves = new List<Vector2>();

        if (IsStunned || IsRooted)
        {
            return legalMoves;
        }

        foreach (Vector2 move in GetPotentialMoves())
        {
            legalMoves.Add(move);
        }

        if (HasHeartOfMountainBuff())
        {
            AddHeartBonusMoves(legalMoves);
        }

        return legalMoves;
    }

    public void Initialize(string pieceType, bool isWhite)
    {
        PieceType = pieceType;
        IsWhite = isWhite;
        HasMoved = 0;
        logicManager = Object.FindFirstObjectByType<LogicManager>();
    }

    public void Initialize(string pieceType, bool isWhite, Faction faction)
    {
        this.PieceType = pieceType;
        this.IsWhite = isWhite;
        this.PieceFaction = faction; // 保存阵营信息
        this.ResolvedFaction = ResolveFactionForAssets(faction);
        this.HasMoved = 0;
        logicManager = Object.FindFirstObjectByType<LogicManager>();

        SetStatsBasedOnType(pieceType);
        this.CurrentHP = this.MaxHP;
        this.CurrentMana = 0;

        InitializeSpells(pieceType); // 使用解析后的阵营为棋子配置技能
    }

    private void Start()
    {
        logicManager = Object.FindFirstObjectByType<LogicManager>();
        UpdateBoardMap();
    }

    public Vector2 GetCoordinates()
    {
        // Ensure board coordinates remain integral even if the transform drifts off-grid.
        int roundedX = Mathf.RoundToInt(transform.position.x);
        int roundedY = Mathf.RoundToInt(transform.position.z);
        return new Vector2(roundedX, roundedY);
    }

    public void UpdateBoardMap()
    {
        Vector2 coordinates = GetCoordinates();
        logicManager.boardMap[(int)coordinates.x, (int)coordinates.y] = this;
    }

    public virtual void Move(Vector2 newPosition)
    {
        if (IsStunned)
        {
            Debug.Log($"{PieceType} 处于眩晕状态，无法移动。");
            return;
        }

        if (IsRooted)
        {
            Debug.Log($"{PieceType} 被定身，无法移动。");
            return;
        }

        if (!IsDestinationAvailable(newPosition))
        {
            Debug.LogWarning($"{PieceType} 无法移动到 ({newPosition.x}, {newPosition.y})，格子被占用。");
            heartBonusTargetMap.Clear();
            return;
        }

        Vector2 currentCoordinates = GetCoordinates();

        if (heartBonusTargetMap.TryGetValue(newPosition, out Vector2 intermediatePosition))
        {
            if (!IsDestinationAvailable(intermediatePosition))
            {
                Debug.LogWarning($"{PieceType} 无法执行额外步数，路径被阻挡。");
                heartBonusTargetMap.Clear();
                return;
            }

            PerformSingleStep(currentCoordinates, intermediatePosition);
            PerformSingleStep(intermediatePosition, newPosition);
        }
        else
        {
            PerformSingleStep(currentCoordinates, newPosition);
        }

        HasMoved = 1;
        heartBonusTargetMap.Clear();
    }

    private void PerformSingleStep(Vector2 sourcePosition, Vector2 targetPosition)
    {
        logicManager.boardMap[(int)sourcePosition.x, (int)sourcePosition.y] = null;
        transform.position = new Vector3(targetPosition.x, transform.position.y, targetPosition.y);
        UpdateBoardMap();
    }

    public void TeleportTo(Vector2 newPosition)
    {
        if (!IsDestinationAvailable(newPosition))
        {
            Debug.LogWarning($"{PieceType} 无法传送到 ({newPosition.x}, {newPosition.y})，格子被占用。");
            return;
        }

        Vector2 currentCoordinates = GetCoordinates();
        PerformSingleStep(currentCoordinates, newPosition);
        HasMoved = 1;
        heartBonusTargetMap.Clear();
    }

    private bool IsDestinationAvailable(Vector2 targetPosition)
    {
        if (logicManager == null)
        {
            return false;
        }

        Piece occupant = logicManager.boardMap[(int)targetPosition.x, (int)targetPosition.y];
        return occupant == null;
    }

    public bool IsPositionWithinBoard(Vector2 position)
    {
        return position.x >= 0 && position.x < 8 && position.y >= 0 && position.y < 8;
    }

    public void TakeDamage(int amount, DamageType damageType = DamageType.Physical)
    {

        if (logicManager != null && logicManager.IsPieceProtectedBySunwellWard(this))
        {
            Debug.Log($"{PieceType} is protected by Sunwell Ward and takes no damage.");
            return; // 免疫伤害
        }


        if (logicManager != null)
        {
            amount = Mathf.Max(0, amount - logicManager.GetDamageReductionForPiece(this));
        }

        if (shieldValue > 0 && amount > 0)
        {
            int absorbed = Mathf.Min(shieldValue, amount);
            shieldValue -= absorbed;
            amount -= absorbed;
        }

        if (amount <= 0)
        {
            return;
        }

        CurrentHP -= amount;
        Debug.Log($"{PieceType} 受到了 {amount} 点{damageType}伤害, 剩余HP: {CurrentHP}/{MaxHP}");

        if (CurrentHP <= 0)
        {
            CurrentHP = 0;
            logicManager.DestroyPiece(this);
        }
    }

    public void Heal(int amount)
    {
        CurrentHP += amount;
        if (CurrentHP > MaxHP)
        {
            CurrentHP = MaxHP;
        }
    }

    public bool UseMana(int amount)
    {
        if (CurrentMana >= amount)
        {
            CurrentMana -= amount;
            return true;
        }
        return false;
    }

    public void GainMana(int amount)
    {
        CurrentMana += amount;
        if (CurrentMana > MaxMana) CurrentMana = MaxMana;
    }

    public void LoseMana(int amount)
    {
        CurrentMana -= amount;
        if (CurrentMana < 0) CurrentMana = 0;
    }

    public virtual void OnTurnStart(bool activeTurnIsWhite)
    {
        bool shouldGainMana = logicManager == null || IsWhite == activeTurnIsWhite;
        if (shouldGainMana)
        {
            GainMana(1); // 每回合回蓝
        }

        if (stunDuration > 0)
        {
            stunDuration--;
        }

        if (rootDuration > 0)
        {
            rootDuration--;
        }

        ResolveDamageOverTime();

        foreach (Spell spell in Spells)
        {
            spell.OnTurnStart();
        }
    }

    private void ResolveDamageOverTime()
    {
        for (int i = damageOverTimeEffects.Count - 1; i >= 0; i--)
        {
            DamageOverTimeEffect effect = damageOverTimeEffects[i];
            TakeDamage(effect.Damage, effect.DamageType);
            effect.Duration--;
            if (effect.Duration <= 0)
            {
                damageOverTimeEffects.RemoveAt(i);
            }
        }
    }

    private void AddHeartBonusMoves(List<Vector2> legalMoves)
    {
        if (logicManager == null)
        {
            return;
        }

        Vector2[] directions =
        {
            new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1)
        };

        List<Vector2> baseMoves = new List<Vector2>(legalMoves);
        List<Vector2> bonusTargets = new List<Vector2>();

        foreach (Vector2 move in baseMoves)
        {
            Piece occupant = logicManager.boardMap[(int)move.x, (int)move.y];
            if (occupant != null)
            {
                continue; // 只能在第一段移动落在空格时追加额外步数
            }

            foreach (Vector2 dir in directions)
            {
                Vector2 extraPos = move + dir;
                if (!IsPositionWithinBoard(extraPos))
                {
                    continue;
                }

                Piece targetPiece = logicManager.boardMap[(int)extraPos.x, (int)extraPos.y];
                if (targetPiece != null)
                {
                    continue;
                }

                if (!bonusTargets.Contains(extraPos) && !legalMoves.Contains(extraPos))
                {
                    bonusTargets.Add(extraPos);
                }

                heartBonusTargetMap[extraPos] = move;
            }
        }

        legalMoves.AddRange(bonusTargets);
    }

    protected bool HasHeartOfMountainBuff()
    {
        return logicManager != null && logicManager.HasHeartOfMountainBuff(IsWhite);
    }

    public void ApplyShield(int amount)
    {
        shieldValue += amount;
    }

    public int GetShieldValue()
    {
        return shieldValue;
    }

    public void ApplyStun(int duration)
    {
        stunDuration = Mathf.Max(stunDuration, duration);
    }

    public void ApplyRoot(int duration)
    {
        if (HasHeartOfMountainBuff())
        {
            return;
        }
        rootDuration = Mathf.Max(rootDuration, duration);
    }

    public void ClearRoot()
    {
        rootDuration = 0;
    }

    public void ApplyDaze(int stacks = 1)
    {
        dazeStacks += Mathf.Max(1, stacks);
    }

    /*
    public int GetAdditionalSpellManaCost()
    {
        return dazeStacks * 3;
    }
    */

    public int GetAdditionalSpellManaCost()
    {
        int costAdjustment = 0;
        if (dazeStacks > 0)
        {
            costAdjustment += dazeStacks * 3;
        }

        // --- 新增代码块 ---
        if (logicManager != null && logicManager.HasSunwellAnthem(this.IsWhite))
        {
            costAdjustment -= 3;
        }
        // --- 新增代码块结束 ---

        return costAdjustment;
    }


    public int GetAdditionalSpellCooldown()
    {
        return dazeStacks * 2;
    }

    public void OnSpellCast()
    {
        if (dazeStacks > 0)
        {
            dazeStacks--;
        }
    }

    public void ApplyDamageOverTime(int damage, DamageType damageType, int duration)
    {
        damageOverTimeEffects.Add(new DamageOverTimeEffect
        {
            Damage = damage,
            DamageType = damageType,
            Duration = Mathf.Max(1, duration)
        });
    }

    private void SetStatsBasedOnType(string type)
    {
        switch (type)
        {
            case "Pawn": MaxHP = 8; MaxMana = 3; break;
            case "Knight": MaxHP = 15; MaxMana = 7; break;
            case "Rook": MaxHP = 18; MaxMana = 8; break;
            case "Bishop": MaxHP = 12; MaxMana = 9; break;
            case "Queen": MaxHP = 18; MaxMana = 10; break;
            case "King": MaxHP = 24; MaxMana = 12; break;
            default:
                Debug.LogError("unknown: " + type);
                MaxHP = 1; MaxMana = 0; break;
        }
    }

    private Faction ResolveFactionForAssets(Faction faction)
    {
        switch (faction)
        {
            case Faction.Undead:
            case Faction.Pandaren:
                return Faction.Elf;
            default:
                return faction;
        }
    }

    private void InitializeSpells(string type)
    {
        Spells.Clear();
        if (logicManager == null)
        {
            logicManager = Object.FindFirstObjectByType<LogicManager>();
        }

        Faction resolvedFaction = ResolvedFaction;

        switch (resolvedFaction)
        {
            case Faction.Elf:
                // 精灵阵营的技能
                switch (type)
                {
                    case "Pawn":
                        Spells.Add(new CrystallinePush());
                        Spells.Add(new Drain());
                        break;
                    case "Knight":
                        Spells.Add(new HawkstriderDash());
                        Spells.Add(new HawkCry());
                        break;
                    case "Bishop":
                        Spells.Add(new ScorchingRay());
                        Spells.Add(new PrismaticBarrier());
                        break;
                    case "Rook":
                        Spells.Add(new SunwellWard());
                        Spells.Add(new Pyroblast());
                        break;
                    case "Queen":
                        Spells.Add(new PhoenixDive());
                        Spells.Add(new AshenRebirth());
                        break;
                    case "King":
                        Spells.Add(new MindControl());
                        Spells.Add(new SunwellAnthem());
                        break;

                }
                break;

            case Faction.Dwarf:
                // 矮人阵营的技能
                switch (type)
                {
                    case "Pawn":
                        Spells.Add(new HammerSlam());
                        Spells.Add(new ShieldPivot());
                        break;
                    case "Knight":
                        Spells.Add(new RamCharge());
                        Spells.Add(new BattleRally());
                        break;
                    case "Bishop":
                        Spells.Add(new HolyRadiance());
                        Spells.Add(new Smite());
                        break;
                    case "Rook":
                        Spells.Add(new FortifiedRampart());
                        Spells.Add(new SeismicShock());
                        break;
                    case "Queen":
                        Spells.Add(new CarryAlly());
                        Spells.Add(new SkyshatterScreech());
                        break;
                    case "King":
                        Spells.Add(new GemstoneSmash());
                        Spells.Add(new HeartOfTheMountain());
                        break;
                        // ... 其他矮人棋子技能
                }
                break;
        }

        // 为所有添加的技能设置施法者和逻辑管理器
        foreach (Spell spell in Spells)
        {
            spell.Initialize(this, logicManager);
        }
    }

    /// <summary>
    /// 设置当前生命值(用于复活等特殊情况)
    /// </summary>
    public void SetCurrentHP(int value)
    {
        CurrentHP = Mathf.Clamp(value, 0, MaxHP);
        Debug.Log($"{PieceType} 生命值设置为 {CurrentHP}/{MaxHP}");
    }

    public void SetDamageBonus(int bonus)
    {
        DamageBonus = bonus;
    }

    // --- 新增变量 ---
    private bool? originalAllegiance = null; // 用于存储原始阵营

    // --- 新增方法 ---
    /// <summary>
    /// 施加精神控制效果
    /// </summary>
    /// <param name="newAllegiance">新的阵营 (通常是施法者的阵营)</param>
    public void MindControl(bool newAllegiance)
    {
        if (originalAllegiance == null) // 防止重复施加
        {
            originalAllegiance = this.IsWhite;
        }
        this.IsWhite = newAllegiance;

        Debug.Log($"{PieceType} at ({GetCoordinates().x}, {GetCoordinates().y}) is now controlled by the {(newAllegiance ? "White" : "Black")} side.");
        // TODO: 在这里更新棋子的视觉效果（如改变材质、颜色、添加状态图标）
    }

    /// <summary>
    /// 恢复精神控制之前的状态
    /// </summary>
    public void RevertMindControl()
    {
        if (originalAllegiance != null)
        {
            this.IsWhite = originalAllegiance.Value;
            Debug.Log($"{PieceType} at ({GetCoordinates().x}, {GetCoordinates().y}) has reverted to its original allegiance: {(this.IsWhite ? "White" : "Black")}.");
            originalAllegiance = null;

            // TODO: 在这里恢复棋子的原始视觉效果
        }
    }

    //--------------封装给UI调用的接口-----------------
    // 1. 获取护盾值
    /*
    public int GetShieldValue()
    {
        return shieldValue;
    }
    */
    // 2. 获取 Daze (迷惑) 层数
    public int GetDazeStacks()
    {
        return dazeStacks;
    }

    // 3. 检查是否有持续伤害 (DoT)
    public bool HasActiveDoT()
    {
        return damageOverTimeEffects != null && damageOverTimeEffects.Count > 0;
    }

    // 4. 获取具体的 DoT 类型（可选，用于显示是中毒还是燃烧）
    // 如果有多种，返回第一个作为代表
    public DamageType? GetFirstDoTType()
    {
        if (damageOverTimeEffects != null && damageOverTimeEffects.Count > 0)
        {
            return damageOverTimeEffects[0].DamageType;
        }
        return null;
    }

    // 5. 检查是否有“山丘之心” (Heart of Mountain) Buff
    // 逻辑已经在 HasHeartOfMountainBuff() 中实现了，可以直接用，但为了统一命名规范，可以封装一下
    public bool IsBuffedByHeartOfMountain()
    {
        return HasHeartOfMountainBuff();
    }

    // 6. 检查是否被 Sunwell Ward 保护
    /*
    public bool IsProtectedBySunwell()
    {
        if (logicManager != null)
        {
            return logicManager.IsPieceProtectedBySunwellWard(this);
        }
        return false;
    }
    */
    
    // ==================================================
    //  UI 接口区域 
    // ==================================================

    // 1. 护盾 (UI读取用)
    public int CurrentShieldValue => shieldValue;

    // 2. 迷茫 Daze (UI读取用)
    public bool IsDazed => dazeStacks > 0;
    public int DazeStackCount => dazeStacks; // 如果后续想在图标上显示层数可以用这个

    // 3. 持续伤害状态检测 (UI读取用)
    // 检测是否处于燃烧状态 (Fire)
    public bool IsBurning => damageOverTimeEffects != null && damageOverTimeEffects.Any(x => x.DamageType == DamageType.Fire);

    // 检测是否处于奥术腐蚀状态 (Arcane) - 对应你的 Blood Elf 设定
    public bool IsArcaneCorroded => damageOverTimeEffects != null && damageOverTimeEffects.Any(x => x.DamageType == DamageType.Arcane);

    // 4. 特殊 Buff 检测
    // 太阳之井的守护 (Sunwell Ward) - 需要调用 LogicManager 查询
    public bool IsProtectedBySunwell => logicManager != null && logicManager.IsPieceProtectedBySunwellWard(this);

    // 山丘之心 (Heart of Mountain) - 包装原本的 protected 方法
    public bool IsHeartOfMountainActive => HasHeartOfMountainBuff();

}