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

    // 动画控制器引用
    private PieceMotionAnimator motionAnimator;
    
    /// <summary>
    /// 获取动画控制器，如果没有则自动添加
    /// </summary>
    public PieceMotionAnimator MotionAnimator
    {
        get
        {
            if (motionAnimator == null)
            {
                motionAnimator = GetComponent<PieceMotionAnimator>();
                if (motionAnimator == null)
                {
                    motionAnimator = gameObject.AddComponent<PieceMotionAnimator>();
                }
            }
            return motionAnimator;
        }
    }
    
    /// <summary>
    /// 棋子是否正在动画中（用于输入保护）
    /// </summary>
    public bool IsAnimating => MotionAnimator != null && MotionAnimator.IsAnimating;
    
    /// <summary>
    /// 棋子是否处于选中/悬浮状态
    /// </summary>
    public bool IsSelected => MotionAnimator != null && MotionAnimator.IsSelected;

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

        if (GetComponent<PieceStatusVFXController>() == null)
        {
            gameObject.AddComponent<PieceStatusVFXController>();
        }
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

    /// <summary>
    /// 移动棋子到目标位置
    /// </summary>
    /// <param name="newPosition">目标位置</param>
    public virtual void Move(Vector2 newPosition)
    {
        Move(newPosition, true, false);
    }

    /// <summary>
    /// 移动棋子到目标位置（带动画控制）
    /// </summary>
    /// <param name="newPosition">目标位置</param>
    /// <param name="animate">是否播放动画</param>
    public virtual void Move(Vector2 newPosition, bool animate = true, bool ignoreRestrictions = false)
    {
        if (!ignoreRestrictions && IsStunned)
        {
            Debug.Log($"{PieceType} 处于眩晕状态，无法移动。");
            return;
        }

        if (!ignoreRestrictions && IsRooted)
        {
            Debug.Log($"{PieceType} 被定身，无法移动。");
            return;
        }

        if (!ignoreRestrictions && !IsDestinationAvailable(newPosition))
        {
            Debug.LogWarning($"{PieceType} 无法移动到 ({newPosition.x}, {newPosition.y})，格子被占用。");
            heartBonusTargetMap.Clear();
            return;
        }

        Vector2 currentCoordinates = GetCoordinates();

        if (heartBonusTargetMap.TryGetValue(newPosition, out Vector2 intermediatePosition))
        {
            if (!ignoreRestrictions && !IsDestinationAvailable(intermediatePosition))
            {
                Debug.LogWarning($"{PieceType} 无法执行额外步数，路径被阻挡。");
                heartBonusTargetMap.Clear();
                return;
            }

            if (animate)
            {
                // 两段移动动画
                StartCoroutine(PerformAnimatedDoubleStep(currentCoordinates, intermediatePosition, newPosition));
            }
            else
            {
                PerformSingleStep(currentCoordinates, intermediatePosition);
                PerformSingleStep(intermediatePosition, newPosition);
                HasMoved = 1;
                heartBonusTargetMap.Clear();
            }
        }
        else
        {
            if (animate)
            {
                // 单段移动动画
                StartCoroutine(PerformAnimatedSingleStep(currentCoordinates, newPosition));
            }
            else
            {
                PerformSingleStep(currentCoordinates, newPosition);
                HasMoved = 1;
                heartBonusTargetMap.Clear();
            }
        }
    }

    /// <summary>
    /// 执行带动画的单步移动
    /// </summary>
    private System.Collections.IEnumerator PerformAnimatedSingleStep(Vector2 from, Vector2 to)
    {
        // 先更新boardMap（逻辑位置）
        logicManager.boardMap[(int)from.x, (int)from.y] = null;
        
        // 计算目标世界位置（使用地面Y坐标）
        Vector3 targetWorldPos = new Vector3(to.x, 0, to.y);
        
        // 播放移动动画
        bool animationComplete = false;
        MotionAnimator.PlayMoveAnimation(targetWorldPos, () => animationComplete = true);
        
        // 等待动画完成
        while (!animationComplete)
        {
            yield return null;
        }
        
        // 动画完成后更新boardMap
        UpdateBoardMap();
        
        HasMoved = 1;
        heartBonusTargetMap.Clear();
        
        // 播放移动音效
        if (GameSoundManager.Instance != null)
        {
            GameSoundManager.Instance.PlayMoveSound();
        }
    }

    /// <summary>
    /// 执行带动画的两步移动（用于Heart of Mountain等额外步数）
    /// </summary>
    private System.Collections.IEnumerator PerformAnimatedDoubleStep(Vector2 from, Vector2 mid, Vector2 to)
    {
        // 第一段移动
        logicManager.boardMap[(int)from.x, (int)from.y] = null;
        
        // 计算中间目标世界位置（使用地面Y坐标）
        Vector3 midWorldPos = new Vector3(mid.x, 0, mid.y);
        bool animationComplete = false;
        MotionAnimator.PlayMoveAnimation(midWorldPos, () => animationComplete = true);
        
        while (!animationComplete)
        {
            yield return null;
        }
        
        UpdateBoardMap();
        
        // 第二段移动
        logicManager.boardMap[(int)mid.x, (int)mid.y] = null;
        
        // 计算最终目标世界位置（使用地面Y坐标）
        Vector3 targetWorldPos = new Vector3(to.x, 0, to.y);
        animationComplete = false;
        MotionAnimator.PlayMoveAnimation(targetWorldPos, () => animationComplete = true);
        
        while (!animationComplete)
        {
            yield return null;
        }
        
        UpdateBoardMap();
        
        HasMoved = 1;
        heartBonusTargetMap.Clear();
        
        // 播放移动音效
        if (GameSoundManager.Instance != null)
        {
            GameSoundManager.Instance.PlayMoveSound();
        }
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

        if (logicManager.IsPrismaticBarrierBlockingSquare(targetPosition, IsWhite))
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
            Debug.Log($"{PieceType} is protected by Sunwell Ward and takes no damage.");  //这个之后要插UI

            if (GameNotificationManager.Instance != null)
            {
                GameNotificationManager.Instance.ShowDamageText(transform.position, 0, damageType);
            }

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

        //简单特效 这个我还没做捏
        if (VFXManager.Instance != null)
        {
            // 在当前棋子的位置播放对应类型的特效
            VFXManager.Instance.PlayImpactVFX(this.transform.position, damageType);
        }

        // Play damage type sound
        if (GameSoundManager.Instance != null)
        {
            GameSoundManager.Instance.PlayDamageSound(damageType);

            if (GameNotificationManager.Instance != null)
            {
                GameNotificationManager.Instance.ShowDamageText(this.transform.position, amount, damageType);
            }
            // --
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

    public void SetMana(int amount)
    {
        CurrentMana = Mathf.Clamp(amount, 0, MaxMana);
    }

    public void LoseMana(int amount)
    {
        CurrentMana -= amount;
        if (CurrentMana < 0) CurrentMana = 0;
    }

    /* 旧代码回合判断有误，判断了两次
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
    */

    public virtual void OnTurnStart(bool activeTurnIsWhite)
    {
        // 1. 判断当前是否是“我”的回合
        // 如果 logicManager 为空（单机测试）或者 棋子颜色与当前回合颜色一致
        bool isMyTurn = logicManager == null || IsWhite == activeTurnIsWhite;

        // --- 只有在自己的回合才执行以下逻辑 ---
        if (isMyTurn)
        {
            // 1. 回蓝
            GainMana(1);

            // 2. 状态持续时间扣减 (眩晕/定身)
            // 如果你不加 isMyTurn 判断，对手走一步你的眩晕就减一层，你走一步又减一层，控制时间会减半
            if (stunDuration > 0)
            {
                stunDuration--;
            }

            if (rootDuration > 0)
            {
                rootDuration--;
            }

            // 3. 技能冷却扣减 (修复核心问题)
            foreach (Spell spell in Spells)
            {
                spell.OnTurnStart();
            }

            // 4. 处理持续伤害 (DoT)
            // 通常 DoT 也是在自己回合开始时结算
            ResolveDamageOverTime();
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

                if (logicManager.IsPrismaticBarrierBlockingSquare(extraPos, IsWhite))
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
            costAdjustment += 3;
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
        return 2;
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
            case "Pawn": MaxHP = 7; MaxMana = 3; break;
            case "Knight": MaxHP = 12; MaxMana = 5; break;
            case "Rook": MaxHP = 15; MaxMana = 6; break;
            case "Bishop": MaxHP = 12; MaxMana = 6; break;
            case "Queen": MaxHP = 16; MaxMana = 8; break;
            case "King": MaxHP = 18; MaxMana = 9; break;
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
                // Blood Elf spells
                switch (type)
                {
                    case "Pawn":
                        Spells.Add(new CrystallinePush());
                        break;
                    case "Knight":
                        Spells.Add(new BattleRally());
                        break;
                    case "Bishop":
                        Spells.Add(new ScorchingRay());
                        Spells.Add(new PrismaticBarrier());
                        break;
                    case "Rook":
                        Spells.Add(new FortifiedRampart());
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
                // Dwarf spells
                switch (type)
                {
                    case "Pawn":
                        Spells.Add(new CrystallinePush());
                        break;
                    case "Knight":
                        Spells.Add(new BattleRally());
                        break;
                    case "Bishop":
                        Spells.Add(new ScorchingRay());
                        Spells.Add(new PrismaticBarrier());
                        break;
                    case "Rook":
                        Spells.Add(new FortifiedRampart());
                        break;
                    case "Queen":
                        Spells.Add(new PhoenixDive());
                        Spells.Add(new CarryAlly());
                        break;
                    case "King":
                        Spells.Add(new GemstoneSmash());
                        Spells.Add(new HeartOfTheMountain());
                        break;
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
        ApplyFacingForCurrentSide();

        this.HasMoved = 0;

        Debug.Log($"{PieceType} at ({GetCoordinates().x}, {GetCoordinates().y}) is now controlled by the {(newAllegiance ? "White" : "Black")} side.");
        /*
        // TODO: 在这里更新棋子的视觉效果（如改变材质、颜色、添加状态图标）
        if (GetComponent<Renderer>() != null && logicManager != null && logicManager.board != null)
        {
            // 假设 logicManager.board 引用了 Board 脚本，且里面有材质列表
            // 这里只是示例，具体取决于你的 Board 脚本怎么存材质
            // GetComponent<Renderer>().material = newAllegiance ? logicManager.board.WhiteMaterial : logicManager.board.BlackMaterial;

            // 或者简单暴力一点，用颜色区分（临时调试用）
            GetComponent<Renderer>().material.color = newAllegiance ? Color.white : Color.black;
        }
        */
    }

    /// <summary>
    /// 强制切换阵营
    /// </summary>
    /*
    public void SwitchFaction(bool newIsWhite, Faction newFaction)
    {
        this.IsWhite = newIsWhite;
        this.PieceFaction = newFaction;
        InitializeSpells(this.PieceType);
    }
    */

    public void SwitchFaction(bool newIsWhite, Faction newFaction)
    {
        this.IsWhite = newIsWhite;
        this.PieceFaction = newFaction;

        InitializeSpells(this.PieceType);

 
    }

    /// <summary>
    /// 重置回合状态，允许棋子再次行动
    /// </summary>
    public void ResetTurnState()
    {
        this.HasMoved = 0; // 0 代表未移动
        // 也可以选择是否重置技能冷却，通常不重置技能冷却会比较平衡
        // 但如果你希望它能立刻放技能，且无视之前的冷却，可以在这里调用 ResetCooldowns()
    }

    /// <summary>
    /// 强制设置移动状态
    /// </summary>
    public void SetHasMoved(int value)
    {
        this.HasMoved = value;
    }

    /// <summary>
    /// 恢复精神控制之前的状态
    /// </summary>
    public void RevertMindControl()
    {
        if (originalAllegiance != null)
        {
            this.IsWhite = originalAllegiance.Value;
            ApplyFacingForCurrentSide();
            Debug.Log($"{PieceType} at ({GetCoordinates().x}, {GetCoordinates().y}) has reverted to its original allegiance: {(this.IsWhite ? "White" : "Black")}.");
            originalAllegiance = null;

            this.HasMoved = 1;

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

    public bool HasSunwellAnthemBuff
    {
        get
        {
            // 这是一个全队 Buff，通常存储在 LogicManager 中
            // 我们查询 LogicManager：当前阵营是否有这个 Buff
            if (logicManager != null)
            {
                return logicManager.HasSunwellAnthem(this.IsWhite);
            }
            return false;
        }
    }


    // 是否被精神控制
    public bool IsMindControlled => logicManager != null && logicManager.IsPieceUnderMindControl(this);

    // 是否有堡垒光环保护
    public bool HasRampartBuff => logicManager != null && logicManager.IsPieceProtectedByRampart(this);

    public void SetFaction(bool isWhite)
    {
        IsWhite = isWhite;
    }

    private void ApplyFacingForCurrentSide()
    {
        Quaternion facingRotation = IsWhite ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
        MotionAnimator.PlayFacingTurnAnimation(facingRotation);
    }

}
