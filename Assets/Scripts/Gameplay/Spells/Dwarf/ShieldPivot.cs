


using System.Collections.Generic;
using UnityEngine;

public class ShieldPivot : Spell
{
    private static readonly Vector2Int[] HorizontalDirections =
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0)
    };

    private static readonly Vector2Int[] AdjacentOffsets =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
    };

    private Vector2Int? pendingMove;
    private Vector2Int? pendingAlly;

    public ShieldPivot()
    {
        SpellName = "Shield Pivot";
        Description = "Move 1 horizontal, then shield an adjacent ally for 4.";
        ManaCost = 3;
        Cooldown = 0;
    }

    public override void BeginTargeting()
    {
        pendingMove = null;
        pendingAlly = null;
    }

    public override void CancelTargeting()
    {
        pendingMove = null;
        pendingAlly = null;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        List<Vector2> targets = new List<Vector2>();
        foreach (Vector2Int move in GetHorizontalMoveSquares())
        {
            foreach (Vector2Int ally in GetAdjacentAllies(move))
            {
                Vector2 allyVec = new Vector2(ally.x, ally.y);
                if (!targets.Contains(allyVec))
                {
                    targets.Add(allyVec);
                }
            }
        }

        return targets;
    }

    public override List<Vector2> GetCurrentValidSquares()
    {
        if (!pendingMove.HasValue)
        {
            return ConvertToVector2(GetHorizontalMoveSquares());
        }

        return ConvertToVector2(GetAdjacentAllies(pendingMove.Value));
    }

    public override bool TryHandleTargetSelection(Vector2 targetSquare, out bool castComplete)
    {
        castComplete = false;

        Vector2Int gridTarget = Vector2Int.RoundToInt(targetSquare);

        if (!pendingMove.HasValue)
        {
            if (!GetHorizontalMoveSquares().Contains(gridTarget))
            {
                return false;
            }

            pendingMove = gridTarget;
            return true;
        }

        if (!GetAdjacentAllies(pendingMove.Value).Contains(gridTarget))
        {
            return false;
        }

        pendingAlly = gridTarget;
        castComplete = true;
        return true;
    }

    public override SpellCastData GetCastData(Vector2 finalTarget)
    {
        return new SpellCastData
        {
            PrimaryX = Mathf.RoundToInt(finalTarget.x),
            PrimaryY = Mathf.RoundToInt(finalTarget.y),
            SecondaryX = pendingMove.HasValue ? pendingMove.Value.x : -1,
            SecondaryY = pendingMove.HasValue ? pendingMove.Value.y : -1,
            TertiaryX = -1,
            TertiaryY = -1
        };
    }

    public override void ApplyCastData(SpellCastData data)
    {
        Vector2Int move = data.GetSecondary();
        Vector2Int ally = data.GetPrimary();

        pendingMove = move.x >= 0 ? move : (Vector2Int?)null;
        pendingAlly = ally.x >= 0 ? ally : (Vector2Int?)null;
    }

    public override bool IsCastDataValid(SpellCastData data)
    {
        if (Caster == null || LogicManager == null)
        {
            return false;
        }

        Vector2Int move = data.GetSecondary();
        Vector2Int allyPos = data.GetPrimary();

        if (move.x < 0 || allyPos.x < 0)
        {
            return false;
        }

        if (!GetHorizontalMoveSquares().Contains(move))
        {
            return false;
        }

        if (!GetAdjacentAllies(move).Contains(allyPos))
        {
            return false;
        }

        return true;
    }

    protected override void ExecuteEffect(Vector2 targetSquare)
    {
        if (Caster == null || LogicManager == null || !pendingMove.HasValue || !pendingAlly.HasValue)
        {
            return;
        }

        Vector2 movePos = new Vector2(pendingMove.Value.x, pendingMove.Value.y);
        Vector2Int allyPos = pendingAlly.Value;

        Piece ally = LogicManager.boardMap[allyPos.x, allyPos.y];
        if (ally != null && ally.IsWhite == Caster.IsWhite)
        {
            Caster.Move(movePos);
            ally.ApplyShield(4);
        }

        pendingMove = null;
        pendingAlly = null;
    }

    private List<Vector2Int> GetHorizontalMoveSquares()
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        if (Caster == null || LogicManager == null)
        {
            return moves;
        }

        Vector2Int casterPos = Vector2Int.RoundToInt(Caster.GetCoordinates());
        foreach (Vector2Int dir in HorizontalDirections)
        {
            Vector2Int target = casterPos + dir;
            if (!Caster.IsPositionWithinBoard(target))
            {
                continue;
            }

            if (LogicManager.boardMap[target.x, target.y] != null)
            {
                continue;
            }

            if (GetAdjacentAllies(target).Count == 0)
            {
                continue;
            }

            moves.Add(target);
        }

        return moves;
    }

    private List<Vector2Int> GetAdjacentAllies(Vector2Int center)
    {
        List<Vector2Int> allies = new List<Vector2Int>();
        if (Caster == null || LogicManager == null)
        {
            return allies;
        }

        foreach (Vector2Int offset in AdjacentOffsets)
        {
            Vector2Int pos = center + offset;
            if (!Caster.IsPositionWithinBoard(pos))
            {
                continue;
            }

            Piece ally = LogicManager.boardMap[pos.x, pos.y];
            if (ally != null && ally.IsWhite == Caster.IsWhite)
            {
                allies.Add(pos);
            }
        }

        return allies;
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