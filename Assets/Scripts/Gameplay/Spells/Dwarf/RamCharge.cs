

using System.Collections.Generic;
using UnityEngine;

public class RamCharge : Spell
{
    private static readonly Vector2Int[] KnightMoves =
    {
        new Vector2Int(1, 2), new Vector2Int(1, -2), new Vector2Int(-1, 2), new Vector2Int(-1, -2),
        new Vector2Int(2, 1), new Vector2Int(2, -1), new Vector2Int(-2, 1), new Vector2Int(-2, -1)
    };

    private static readonly Vector2Int[] AdjacentOffsets =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
    };

    private Vector2Int? pendingLanding;
    private Vector2Int? pendingEnemy;

    public RamCharge()
    {
        SpellName = "Ram Charge";
        Description = "Jump like a knight, then scorch one adjacent enemy for 5 Fire.";
        ManaCost = 3;
        Cooldown = 2;
    }

    public override void BeginTargeting()
    {
        pendingLanding = null;
        pendingEnemy = null;
    }

    public override void CancelTargeting()
    {
        pendingLanding = null;
        pendingEnemy = null;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        List<Vector2> targets = new List<Vector2>();
        foreach (Vector2Int landing in GetLandingSquares())
        {
            foreach (Vector2Int enemy in GetEnemiesAround(landing))
            {
                Vector2 enemyVec = new Vector2(enemy.x, enemy.y);
                if (!targets.Contains(enemyVec))
                {
                    targets.Add(enemyVec);
                }
            }
        }

        return targets;
    }

    public override List<Vector2> GetCurrentValidSquares()
    {
        if (!pendingLanding.HasValue)
        {
            return ConvertToVector2(GetLandingSquares());
        }

        return ConvertToVector2(GetEnemiesAround(pendingLanding.Value));
    }

    public override bool TryHandleTargetSelection(Vector2 targetSquare, out bool castComplete)
    {
        castComplete = false;
        Vector2Int gridTarget = Vector2Int.RoundToInt(targetSquare);

        if (!pendingLanding.HasValue)
        {
            if (!GetLandingSquares().Contains(gridTarget))
            {
                return false;
            }

            pendingLanding = gridTarget;
            return true;
        }

        if (!GetEnemiesAround(pendingLanding.Value).Contains(gridTarget))
        {
            return false;
        }

        pendingEnemy = gridTarget;
        castComplete = true;
        return true;
    }

    public override SpellCastData GetCastData(Vector2 finalTarget)
    {
        return new SpellCastData
        {
            PrimaryX = Mathf.RoundToInt(finalTarget.x),
            PrimaryY = Mathf.RoundToInt(finalTarget.y),
            SecondaryX = pendingLanding.HasValue ? pendingLanding.Value.x : -1,
            SecondaryY = pendingLanding.HasValue ? pendingLanding.Value.y : -1,
            TertiaryX = -1,
            TertiaryY = -1
        };
    }

    public override void ApplyCastData(SpellCastData data)
    {
        Vector2Int landing = data.GetSecondary();
        Vector2Int enemy = data.GetPrimary();

        pendingLanding = landing.x >= 0 ? landing : (Vector2Int?)null;
        pendingEnemy = enemy.x >= 0 ? enemy : (Vector2Int?)null;
    }

    public override bool IsCastDataValid(SpellCastData data)
    {
        if (Caster == null || LogicManager == null)
        {
            return false;
        }

        Vector2Int landing = data.GetSecondary();
        Vector2Int enemy = data.GetPrimary();

        if (landing.x < 0 || enemy.x < 0)
        {
            return false;
        }

        if (!GetLandingSquares().Contains(landing))
        {
            return false;
        }

        if (!GetEnemiesAround(landing).Contains(enemy))
        {
            return false;
        }

        return true;
    }

    protected override void ExecuteEffect(Vector2 targetSquare)
    {
        if (Caster == null || LogicManager == null || !pendingLanding.HasValue || !pendingEnemy.HasValue)
        {
            return;
        }

        Vector2 landingPos = new Vector2(pendingLanding.Value.x, pendingLanding.Value.y);
        Vector2Int enemyPos = pendingEnemy.Value;

        Piece enemy = LogicManager.boardMap[enemyPos.x, enemyPos.y];
        if (enemy != null && enemy.IsWhite != Caster.IsWhite)
        {
            Caster.Move(landingPos);
            enemy.TakeDamage(5, DamageType.Fire);
        }

        pendingLanding = null;
        pendingEnemy = null;
    }

    private List<Vector2Int> GetLandingSquares()
    {
        List<Vector2Int> landings = new List<Vector2Int>();
        if (Caster == null || LogicManager == null)
        {
            return landings;
        }

        Vector2Int casterPos = Vector2Int.RoundToInt(Caster.GetCoordinates());
        foreach (Vector2Int offset in KnightMoves)
        {
            Vector2Int landing = casterPos + offset;
            if (!Caster.IsPositionWithinBoard(landing))
            {
                continue;
            }

            if (LogicManager.boardMap[landing.x, landing.y] != null)
            {
                continue;
            }

            if (GetEnemiesAround(landing).Count == 0)
            {
                continue;
            }

            landings.Add(landing);
        }

        return landings;
    }

    private List<Vector2Int> GetEnemiesAround(Vector2Int center)
    {
        List<Vector2Int> enemies = new List<Vector2Int>();
        if (Caster == null || LogicManager == null)
        {
            return enemies;
        }

        foreach (Vector2Int offset in AdjacentOffsets)
        {
            Vector2Int pos = center + offset;
            if (!Caster.IsPositionWithinBoard(pos))
            {
                continue;
            }

            Piece enemy = LogicManager.boardMap[pos.x, pos.y];
            if (enemy != null && enemy.IsWhite != Caster.IsWhite)
            {
                enemies.Add(pos);
            }
        }

        return enemies;
    }

    private List<Vector2> ConvertToVector2(List<Vector2Int> ints)
    {
        List<Vector2> result = new List<Vector2>();
        foreach (Vector2Int value in ints)
        {
            result.Add(new Vector2(value.x, value.y));
        }

        return result;
    }
}