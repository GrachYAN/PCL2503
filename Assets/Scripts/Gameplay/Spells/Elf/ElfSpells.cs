/*
using System.Collections.Generic;
using UnityEngine;

// 兵卒技能
public class CrystallinePush : Spell
{
    public CrystallinePush()
    {
        SpellName = "Crystalline Push";
        Description = "Deal 3 Arcane damage";
        ManaCost = 0;
        Cooldown = 0;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        List<Vector2> validSquares = new List<Vector2>();
        Vector2 casterPos = Caster.GetCoordinates();

        // 检查所有相邻格子
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;

                Vector2 target = casterPos + new Vector2(x, y);
                if (Caster.IsPositionWithinBoard(target))
                {
                    Piece targetPiece = LogicManager.boardMap[(int)target.x, (int)target.y];
                    if (targetPiece != null && targetPiece.IsWhite != Caster.IsWhite)
                    {
                        validSquares.Add(target);
                    }
                }
            }
        }

        return validSquares;
    }

    protected override void ExecuteEffect(Vector2 targetSquare)
    {
        Piece targetPiece = LogicManager.boardMap[(int)targetSquare.x, (int)targetSquare.y];
        if (targetPiece != null && targetPiece.IsWhite != Caster.IsWhite)
        {
            int damage = 3 + Caster.GetDamageBonus(); // 应用伤害加成
            targetPiece.TakeDamage(damage, DamageType.Arcane);
        }
    }
}

public class Drain : Spell
{
    public Drain()
    {
        SpellName = "Drain";
        Description = "Adjacent enemy loses 4 Mana";
        ManaCost = 3;
        Cooldown = 3;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        List<Vector2> validSquares = new List<Vector2>();
        Vector2 casterPos = Caster.GetCoordinates();

        // 检查所有相邻格子
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;

                Vector2 target = casterPos + new Vector2(x, y);
                if (Caster.IsPositionWithinBoard(target))
                {
                    Piece targetPiece = LogicManager.boardMap[(int)target.x, (int)target.y];
                    if (targetPiece != null && targetPiece.IsWhite != Caster.IsWhite)
                    {
                        validSquares.Add(target);
                    }
                }
            }
        }

        return validSquares;
    }

    protected override void ExecuteEffect(Vector2 targetSquare)
    {
        Piece targetPiece = LogicManager.boardMap[(int)targetSquare.x, (int)targetSquare.y];
        if (targetPiece != null && targetPiece.IsWhite != Caster.IsWhite)
        {
            targetPiece.LoseMana(4);
        }
    }
}

// 骑士技能
public class HawkstriderDash : Spell
{
    public HawkstriderDash()
    {
        SpellName = "Hawkstrider Dash";
        Description = "After a normal jump, deal 5 Fire damage to one adjacent enemy at landing";
        ManaCost = 3;
        Cooldown = 2;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        // 这个技能需要在移动后使用，所以这里返回空列表
        // 实际实现需要在移动系统里特殊处理
        return new List<Vector2>();
    }

    protected override void ExecuteEffect(Vector2 targetSquare)
    {
        // 这个技能的执行逻辑需要与移动系统结合
        // 在骑士完成移动后，检查落点相邻的敌人
        Piece targetPiece = LogicManager.boardMap[(int)targetSquare.x, (int)targetSquare.y];
        if (targetPiece != null && targetPiece.IsWhite != Caster.IsWhite)
        {
            targetPiece.TakeDamage(5, DamageType.Fire);
        }
    }
}

public class HawkCry : Spell
{
    public HawkCry()
    {
        SpellName = "Hawk Cry";
        Description = "All adjacent enemies get Dazed";
        ManaCost = 4;
        Cooldown = 3;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        // 以自身为中心的目标
        List<Vector2> validSquares = new List<Vector2> { Caster.GetCoordinates() };
        return validSquares;
    }

    protected override void ExecuteEffect(Vector2 targetSquare)
    {
        Vector2 casterPos = Caster.GetCoordinates();

        // 影响所有相邻敌人
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;

                Vector2 target = casterPos + new Vector2(x, y);
                if (Caster.IsPositionWithinBoard(target))
                {
                    Piece targetPiece = LogicManager.boardMap[(int)target.x, (int)target.y];
                    if (targetPiece != null && targetPiece.IsWhite != Caster.IsWhite)
                    {
                        targetPiece.ApplyDaze(1);
                    }
                }
            }
        }
    }
}

// 主教技能
public class ScorchingRay : Spell
{
    public ScorchingRay()
    {
        SpellName = "Scorching Ray";
        Description = "Diagonal ray; first enemy hit takes 4 Fire damage, the rest takes 2 Fire damage";
        ManaCost = 4;
        Cooldown = 2;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        List<Vector2> validSquares = new List<Vector2>();
        Vector2 casterPos = Caster.GetCoordinates();

        // 添加所有对角线方向的可达格子
        for (int distance = 1; distance < 8; distance++)
        {
            Vector2[] directions = {
                new Vector2(distance, distance),
                new Vector2(distance, -distance),
                new Vector2(-distance, distance),
                new Vector2(-distance, -distance)
            };

            foreach (Vector2 dir in directions)
            {
                Vector2 target = casterPos + dir;
                if (Caster.IsPositionWithinBoard(target) &&
                    LogicManager.HasLineOfSight(casterPos, target))
                {
                    validSquares.Add(target);
                }
            }
        }

        return validSquares;
    }

    protected override void ExecuteEffect(Vector2 targetSquare)
    {
        Vector2 casterPos = Caster.GetCoordinates();
        Vector2 direction = (targetSquare - casterPos).normalized;

        // 沿着射线方向检查命中的敌人
        bool firstHit = true;
        Vector2 currentPos = casterPos + direction;

        while (Caster.IsPositionWithinBoard(currentPos) &&
               Vector2.Distance(currentPos, casterPos) <= Vector2.Distance(targetSquare, casterPos))
        {
            Piece targetPiece = LogicManager.boardMap[(int)currentPos.x, (int)currentPos.y];
            if (targetPiece != null && targetPiece.IsWhite != Caster.IsWhite)
            {
                if (firstHit)
                {
                    targetPiece.TakeDamage(4, DamageType.Fire);
                    firstHit = false;
                }
                else
                {
                    targetPiece.TakeDamage(2, DamageType.Fire);
                }
            }

            currentPos += direction;
        }
    }
}

public class PrismaticBarrier : Spell
{
    private Vector2 barrierPosition;
    private int remainingRounds = 0;
    private GameObject barrierObject;

    public PrismaticBarrier()
    {
        SpellName = "Prismatic Barrier";
        Description = "Place a barrier on a diagonal square for 3 rounds; it blocks enemy LoS";
        ManaCost = 6;
        Cooldown = 5;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        List<Vector2> validSquares = new List<Vector2>();
        Vector2 casterPos = Caster.GetCoordinates();

        // 只能放在对角线的空格子上
        for (int distance = 1; distance < 8; distance++)
        {
            Vector2[] directions = {
                new Vector2(distance, distance),
                new Vector2(distance, -distance),
                new Vector2(-distance, distance),
                new Vector2(-distance, -distance)
            };

            foreach (Vector2 dir in directions)
            {
                Vector2 target = casterPos + dir;
                if (Caster.IsPositionWithinBoard(target) &&
                    LogicManager.boardMap[(int)target.x, (int)target.y] == null)
                {
                    validSquares.Add(target);
                }
            }
        }

        return validSquares;
    }

    protected override void ExecuteEffect(Vector2 targetSquare)
    {
        barrierPosition = targetSquare;
        remainingRounds = 3;

        // 注册屏障到LogicManager
        LogicManager.RegisterBarrier(this);

        // 生成屏障Prefab
        LogicManager.CreateBarrierEffect(targetSquare, 3);

        Debug.Log($"Prismatic Barrier placed at {targetSquare} for 3 rounds");
    }

    public override void OnTurnStart()
    {
        base.OnTurnStart();

        if (remainingRounds > 0)
        {
            remainingRounds--;
            if (remainingRounds <= 0)
            {
                LogicManager.UnregisterBarrier(this);
                Debug.Log("Prismatic Barrier expired");
            }
        }
    }

    // 添加一个方法来检查位置是否被屏障阻挡
    public bool IsPositionBlocked(Vector2 position)
    {
        return remainingRounds > 0 && position == barrierPosition;
    }
}

// 城堡技能
public class SunwellWard : Spell
{
    private int remainingRounds = 0;

    public SunwellWard()
    {
        SpellName = "Sunwell Ward";
        Description = "Create a 3×3 aura buff centered on the Rook for 1 round. Allies inside cannot be damaged";
        ManaCost = 4;
        Cooldown = 3;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        // 以自身为中心
        List<Vector2> validSquares = new List<Vector2> { Caster.GetCoordinates() };
        return validSquares;
    }

    protected override void ExecuteEffect(Vector2 targetSquare)
    {
        remainingRounds = 1;
        LogicManager.ActivateSunwellWard(this);
        Debug.Log("Sunwell Ward activated for 1 round");
    }

    public override void OnTurnStart()
    {
        base.OnTurnStart();

        if (remainingRounds > 0)
        {
            remainingRounds--;
        }
    }

    // 检查位置是否在结界保护范围内
    public bool IsPositionProtected(Vector2 position, bool isAlly)
    {
        if (remainingRounds <= 0 || !isAlly) return false;

        Vector2 casterPos = Caster.GetCoordinates();
        return Mathf.Abs(position.x - casterPos.x) <= 1 &&
               Mathf.Abs(position.y - casterPos.y) <= 1;
    }
}

public class Pyroblast : Spell
{
    public Pyroblast()
    {
        SpellName = "Pyroblast";
        Description = "Deals 10 Fire damage to a single target";
        ManaCost = 8;
        Cooldown = 5;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        List<Vector2> validSquares = new List<Vector2>();
        Vector2 casterPos = Caster.GetCoordinates();

        // 可以攻击棋盘上任何敌人
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                Vector2 target = new Vector2(x, y);
                Piece targetPiece = LogicManager.boardMap[x, y];
                if (targetPiece != null && targetPiece.IsWhite != Caster.IsWhite &&
                    LogicManager.HasLineOfSight(casterPos, target))
                {
                    validSquares.Add(target);
                }
            }
        }

        return validSquares;
    }

    protected override void ExecuteEffect(Vector2 targetSquare)
    {
        Piece targetPiece = LogicManager.boardMap[(int)targetSquare.x, (int)targetSquare.y];
        if (targetPiece != null && targetPiece.IsWhite != Caster.IsWhite)
        {
            int damage = 10 + Caster.GetDamageBonus();
            targetPiece.TakeDamage(10, DamageType.Fire);
        }
    }
}

// 皇后技能
public class PhoenixDive : Spell
{
    public PhoenixDive()
    {
        SpellName = "Phoenix Dive";
        Description = "Place a Flame Mark up to 3 squares ignoring LoS. The enemy takes stun for 3 rounds and burn (DoT=3) for 3 rounds";
        ManaCost = 5;
        Cooldown = 4;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        List<Vector2> validSquares = new List<Vector2>();
        Vector2 casterPos = Caster.GetCoordinates();

        // 无视视线，3格范围内的所有敌人
        for (int x = -3; x <= 3; x++)
        {
            for (int y = -3; y <= 3; y++)
            {
                if (x == 0 && y == 0) continue;
                if (Mathf.Abs(x) + Mathf.Abs(y) > 3) continue;

                Vector2 target = casterPos + new Vector2(x, y);
                if (Caster.IsPositionWithinBoard(target))
                {
                    Piece targetPiece = LogicManager.boardMap[(int)target.x, (int)target.y];
                    if (targetPiece != null && targetPiece.IsWhite != Caster.IsWhite)
                    {
                        validSquares.Add(target);
                    }
                }
            }
        }

        return validSquares;
    }

    protected override void ExecuteEffect(Vector2 targetSquare)
    {
        Piece targetPiece = LogicManager.boardMap[(int)targetSquare.x, (int)targetSquare.y];
        if (targetPiece != null && targetPiece.IsWhite != Caster.IsWhite)
        {
            // 施加眩晕和持续伤害
            targetPiece.ApplyStun(3);
            targetPiece.ApplyDamageOverTime(3, DamageType.Fire, 3);

            // 生成烈焰标记Prefab
            LogicManager.CreateFlameMarkEffect(targetSquare, 3);
            flameMarkPositions.Add(targetSquare);
            remainingRounds = 3;
            Debug.Log($"Phoenix Dive: Stunned and burning {targetPiece.PieceType} at {targetSquare}");
        }
    }

    public override void OnTurnStart()
    {
        base.OnTurnStart();

        if (remainingRounds > 0)
        {
            remainingRounds--;
            if (remainingRounds <= 0)
            {
                flameMarkPositions.Clear();
            }
        }
    }

    public class AshenRebirth : Spell
    {
        public AshenRebirth()
        {
            SpellName = "Ashen Rebirth";
            Description = "Revive the most recently destroyed friendly piece on any empty square of your back rank at 7 HP and full mana";
            ManaCost = 9;
            Cooldown = 8;
        }

        public override List<Vector2> GetValidTargetSquares()
        {
            List<Vector2> validSquares = new List<Vector2>();

            var destroyedInfo = LogicManager.GetMostRecentlyDestroyedFriendlyPiece(Caster.IsWhite);
            if (!destroyedInfo.HasValue)
            {
                return validSquares; // 没有可复活的棋子
            }


            // 只能放在己方后排的空格子上
            int backRankY = Caster.IsWhite ? 0 : 7;
            for (int x = 0; x < 8; x++)
            {
                Vector2 target = new Vector2(x, backRankY);
                if (LogicManager.boardMap[x, backRankY] == null)
                {
                    validSquares.Add(target);
                }
            }

            return validSquares;
        }

        protected override void ExecuteEffect(Vector2 targetSquare)
        {
            var destroyedInfo = LogicManager.GetMostRecentlyDestroyedFriendlyPiece(Caster.IsWhite);
            if (destroyedInfo.HasValue)
            {
                int maxMana = GetMaxManaByType(destroyedInfo.Value.PieceType);
                LogicManager.RevivePiece(destroyedInfo.Value, targetSquare, 7, maxMana);
                Debug.Log($"Revived {revivedPiece.PieceType} at {targetSquare}");
            }
            else
            {
                Debug.Log("No recently destroyed friendly piece to revive");
            }
        }

        private int GetMaxManaByType(string pieceType)
        {
            switch (pieceType)
            {
                case "Pawn": return 3;
                case "Knight": return 7;
                case "Rook": return 8;
                case "Bishop": return 9;
                case "Queen": return 10;
                case "King": return 12;
                default: return 0;
            }
        }
    }

    // 国王技能
    public class MindControl : Spell
    {
        public MindControl()
        {
            SpellName = "Mind Control";
            Description = "Controls the mind of an adjacent enemy for one round";
            ManaCost = 6;
            Cooldown = 3;
        }

        public override List<Vector2> GetValidTargetSquares()
        {
            List<Vector2> validSquares = new List<Vector2>();
            Vector2 casterPos = Caster.GetCoordinates();

            // 只能控制相邻的敌人
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue;

                    Vector2 target = casterPos + new Vector2(x, y);
                    if (Caster.IsPositionWithinBoard(target))
                    {
                        Piece targetPiece = LogicManager.boardMap[(int)target.x, (int)target.y];
                        if (targetPiece != null && targetPiece.IsWhite != Caster.IsWhite)
                        {
                            validSquares.Add(target);
                        }
                    }
                }
            }

            return validSquares;
        }

        protected override void ExecuteEffect(Vector2 targetSquare)
        {
            Piece targetPiece = LogicManager.boardMap[(int)targetSquare.x, (int)targetSquare.y];
            if (targetPiece != null && targetPiece.IsWhite != Caster.IsWhite)
            {
                targetPiece.ApplyMindControl(1);
                Debug.Log($"Mind controlling {targetPiece.PieceType} for 1 round");
            }
        }
    }

    public class SunwellAnthem : Spell
    {
        private int remainingRounds = 0;

        public SunwellAnthem()
        {
            SpellName = "Sunwell Anthem";
            Description = "Party-wide buff for 2 rounds: allies gain +2 damage, -3 Mana cost, and 5 HP shield";
            ManaCost = 9;
            Cooldown = 8;
        }

        public override List<Vector2> GetValidTargetSquares()
        {
            // 全队增益，以自身为目标
            List<Vector2> validSquares = new List<Vector2> { Caster.GetCoordinates() };
            return validSquares;
        }

        protected override void ExecuteEffect(Vector2 targetSquare)
        {
            remainingRounds = 2;

            // 为所有友方棋子施加增益
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    Piece piece = LogicManager.boardMap[x, y];
                    if (piece != null && piece.IsWhite == Caster.IsWhite)
                    {
                        piece.ApplySunwellAnthemBuff(2);
                    }
                }
            }

            Debug.Log("Sunwell Anthem activated for 2 rounds");
        }

        public override void OnTurnStart()
        {
            base.OnTurnStart();

            if (remainingRounds > 0)
            {
                remainingRounds--;
                if (remainingRounds <= 0)
                {
                    Debug.Log("Sunwell Anthem expired");
                }
            }
        }

        // 检查增益是否激活
        public bool IsBuffActive()
        {
            return remainingRounds > 0;
        }
    }
}
*/