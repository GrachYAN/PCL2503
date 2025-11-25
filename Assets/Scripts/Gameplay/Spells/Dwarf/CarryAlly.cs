


using System.Collections.Generic;
using UnityEngine;

public class CarryAlly : Spell
{
    private static readonly Vector2Int[] AdjacentOffsets =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
    };

    private Vector2Int? pendingAlly;
    private Vector2Int? pendingDestination;
    private Vector2Int? pendingDrop;

    public CarryAlly()
    {
        SpellName = "Carry Ally";
        Description = "Pick an adjacent ally, move up to 3 in a straight line, then drop them on an adjacent square.";
        ManaCost = 4;
        Cooldown = 3;
    }

    public override void BeginTargeting()
    {
        pendingAlly = null;
        pendingDestination = null;
        pendingDrop = null;
    }

    public override void CancelTargeting()
    {
        pendingAlly = null;
        pendingDestination = null;
        pendingDrop = null;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        List<Vector2> drops = new List<Vector2>();
        foreach (Vector2Int ally in GetAdjacentAllies())
        {
            foreach (Vector2Int dest in GetDestinationsForAlly(ally))
            {
                foreach (Vector2Int drop in GetDropSquares(dest))
                {
                    Vector2 dropVec = new Vector2(drop.x, drop.y);
                    if (!drops.Contains(dropVec))
                    {
                        drops.Add(dropVec);
                    }
                }
            }
        }

        return drops;
    }

    public override List<Vector2> GetCurrentValidSquares()
    {
        if (!pendingAlly.HasValue)
        {
            return ConvertToVector2(GetAdjacentAllies());
        }

        if (!pendingDestination.HasValue)
        {
            return ConvertToVector2(GetDestinationsForAlly(pendingAlly.Value));
        }

        return ConvertToVector2(GetDropSquares(pendingDestination.Value));
    }

    public override bool TryHandleTargetSelection(Vector2 targetSquare, out bool castComplete)
    {
        castComplete = false;
        Vector2Int gridTarget = Vector2Int.RoundToInt(targetSquare);

        if (!pendingAlly.HasValue)
        {
            if (!GetAdjacentAllies().Contains(gridTarget))
            {
                return false;
            }

            pendingAlly = gridTarget;
            return true;
        }

        if (!pendingDestination.HasValue)
        {
            if (!GetDestinationsForAlly(pendingAlly.Value).Contains(gridTarget))
            {
                return false;
            }

            pendingDestination = gridTarget;
            return true;
        }

        if (!GetDropSquares(pendingDestination.Value).Contains(gridTarget))
        {
            return false;
        }

        pendingDrop = gridTarget;
        castComplete = true;
        return true;
    }

    public override SpellCastData GetCastData(Vector2 finalTarget)
    {
        return new SpellCastData
        {
            PrimaryX = pendingDrop.HasValue ? pendingDrop.Value.x : Mathf.RoundToInt(finalTarget.x),
            PrimaryY = pendingDrop.HasValue ? pendingDrop.Value.y : Mathf.RoundToInt(finalTarget.y),
            SecondaryX = pendingDestination.HasValue ? pendingDestination.Value.x : -1,
            SecondaryY = pendingDestination.HasValue ? pendingDestination.Value.y : -1,
            TertiaryX = pendingAlly.HasValue ? pendingAlly.Value.x : -1,
            TertiaryY = pendingAlly.HasValue ? pendingAlly.Value.y : -1
        };
    }

    public override void ApplyCastData(SpellCastData data)
    {
        Vector2Int ally = data.GetTertiary();
        Vector2Int dest = data.GetSecondary();
        Vector2Int drop = data.GetPrimary();

        pendingAlly = ally.x >= 0 ? ally : (Vector2Int?)null;
        pendingDestination = dest.x >= 0 ? dest : (Vector2Int?)null;
        pendingDrop = drop.x >= 0 ? drop : (Vector2Int?)null;
    }

    public override bool IsCastDataValid(SpellCastData data)
    {
        if (Caster == null || LogicManager == null)
        {
            return false;
        }

        Vector2Int ally = data.GetTertiary();
        Vector2Int dest = data.GetSecondary();
        Vector2Int drop = data.GetPrimary();

        if (ally.x < 0 || dest.x < 0 || drop.x < 0)
        {
            return false;
        }

        if (!GetAdjacentAllies().Contains(ally))
        {
            return false;
        }

        if (!GetDestinationsForAlly(ally).Contains(dest))
        {
            return false;
        }

        if (!GetDropSquares(dest).Contains(drop))
        {
            return false;
        }

        return true;
    }

    protected override void ExecuteEffect(Vector2 targetSquare)
    {
        if (Caster == null || LogicManager == null || !pendingAlly.HasValue || !pendingDestination.HasValue || !pendingDrop.HasValue)
        {
            return;
        }

        Vector2Int allyPos = pendingAlly.Value;
        Vector2Int destPos = pendingDestination.Value;
        Vector2Int dropPos = pendingDrop.Value;

        Piece ally = LogicManager.boardMap[allyPos.x, allyPos.y];
        if (ally == null || ally.IsWhite != Caster.IsWhite)
        {
            pendingAlly = null;
            pendingDestination = null;
            pendingDrop = null;
            return;
        }

        Caster.TeleportTo(new Vector2(destPos.x, destPos.y));
        ally.TeleportTo(new Vector2(dropPos.x, dropPos.y));

        pendingAlly = null;
        pendingDestination = null;
        pendingDrop = null;
    }

    private List<Vector2Int> GetAdjacentAllies()
    {
        List<Vector2Int> allies = new List<Vector2Int>();
        if (Caster == null || LogicManager == null)
        {
            return allies;
        }

        Vector2Int casterPos = Vector2Int.RoundToInt(Caster.GetCoordinates());
        foreach (Vector2Int offset in AdjacentOffsets)
        {
            Vector2Int pos = casterPos + offset;
            if (!Caster.IsPositionWithinBoard(pos))
            {
                continue;
            }

            Piece ally = LogicManager.boardMap[pos.x, pos.y];
            if (ally != null && ally.IsWhite == Caster.IsWhite)
            {
                if (GetDestinationsForAlly(pos).Count > 0)
                {
                    allies.Add(pos);
                }
            }
        }

        return allies;
    }

    private List<Vector2Int> GetDestinationsForAlly(Vector2Int allyPos)
    {
        List<Vector2Int> destinations = new List<Vector2Int>();
        if (Caster == null || LogicManager == null)
        {
            return destinations;
        }

        Vector2Int casterPos = Vector2Int.RoundToInt(Caster.GetCoordinates());

        foreach (Vector2Int dir in AdjacentOffsets)
        {
            if (dir == Vector2Int.zero)
            {
                continue;
            }

            for (int distance = 1; distance <= 3; distance++)
            {
                Vector2Int dest = casterPos + dir * distance;
                if (!Caster.IsPositionWithinBoard(dest))
                {
                    break;
                }

                if (!IsLineClear(casterPos, dir, distance, allyPos))
                {
                    break;
                }

                if (LogicManager.boardMap[dest.x, dest.y] != null)
                {
                    break;
                }

                if (GetDropSquares(dest).Count == 0)
                {
                    continue;
                }

                destinations.Add(dest);
            }
        }

        return destinations;
    }

    private bool IsLineClear(Vector2Int start, Vector2Int direction, int distance, Vector2Int allyPos)
    {
        for (int step = 1; step <= distance; step++)
        {
            Vector2Int pos = start + direction * step;
            if (pos == allyPos)
            {
                continue;
            }

            if (!Caster.IsPositionWithinBoard(pos))
            {
                return false;
            }

            if (LogicManager.boardMap[pos.x, pos.y] != null)
            {
                return false;
            }
        }

        return true;
    }

    private List<Vector2Int> GetDropSquares(Vector2Int queenDest)
    {
        List<Vector2Int> drops = new List<Vector2Int>();
        if (Caster == null || LogicManager == null)
        {
            return drops;
        }

        foreach (Vector2Int offset in AdjacentOffsets)
        {
            Vector2Int pos = queenDest + offset;
            if (!Caster.IsPositionWithinBoard(pos))
            {
                continue;
            }

            if (pos == queenDest)
            {
                continue;
            }

            if (LogicManager.boardMap[pos.x, pos.y] == null)
            {
                drops.Add(pos);
            }
        }

        return drops;
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