using System.Collections.Generic;
using UnityEngine;

public class BattleRally : Spell
{
    private const int MaxSelections = 3;
    private readonly List<Vector2Int> selectedPawns = new List<Vector2Int>();

    public BattleRally()
    {
        SpellName = "Battle Rally";
        Description = "Select up to three allied pawns within 2 to move them forward 1 square for free.";
        ManaCost = 4;
        Cooldown = 4;
    }

    public override void BeginTargeting()
    {
        selectedPawns.Clear();
    }

    public override void CancelTargeting()
    {
        selectedPawns.Clear();
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        return ConvertToVector2(GetSelectablePawns());
    }

    public override List<Vector2> GetCurrentValidSquares()
    {
        List<Vector2> targets = new List<Vector2>();
        List<Vector2Int> candidates = GetSelectablePawns();

        foreach (Vector2Int pawn in candidates)
        {
            if (!selectedPawns.Contains(pawn))
            {
                targets.Add(new Vector2(pawn.x, pawn.y));
            }
        }

        if (selectedPawns.Count > 0 && selectedPawns.Count < MaxSelections)
        {
            targets.Add(Caster.GetCoordinates());
        }

        return targets;
    }

    public override bool TryHandleTargetSelection(Vector2 targetSquare, out bool castComplete)
    {
        castComplete = false;
        if (Caster == null)
        {
            return false;
        }

        Vector2Int gridTarget = Vector2Int.RoundToInt(targetSquare);
        Vector2Int casterPos = Vector2Int.RoundToInt(Caster.GetCoordinates());

        if (gridTarget == casterPos)
        {
            if (selectedPawns.Count == 0)
            {
                return false;
            }

            castComplete = true;
            return true;
        }

        List<Vector2Int> candidates = GetSelectablePawns();
        if (!candidates.Contains(gridTarget) || selectedPawns.Contains(gridTarget))
        {
            return false;
        }

        selectedPawns.Add(gridTarget);

        if (selectedPawns.Count >= MaxSelections || selectedPawns.Count == candidates.Count)
        {
            castComplete = true;
        }

        return true;
    }

    public override SpellCastData GetCastData(Vector2 finalTarget)
    {
        Vector2Int first = GetSelectionOrInvalid(0);
        Vector2Int second = GetSelectionOrInvalid(1);
        Vector2Int third = GetSelectionOrInvalid(2);

        return new SpellCastData
        {
            PrimaryX = first.x,
            PrimaryY = first.y,
            SecondaryX = second.x,
            SecondaryY = second.y,
            TertiaryX = third.x,
            TertiaryY = third.y
        };
    }

    public override void ApplyCastData(SpellCastData data)
    {
        selectedPawns.Clear();
        TryAddSelection(data.PrimaryX, data.PrimaryY);
        TryAddSelection(data.SecondaryX, data.SecondaryY);
        TryAddSelection(data.TertiaryX, data.TertiaryY);
    }

    public override bool IsCastDataValid(SpellCastData data)
    {
        if (Caster == null || LogicManager == null)
        {
            return false;
        }

        List<Vector2Int> candidates = GetSelectablePawns();
        HashSet<Vector2Int> seen = new HashSet<Vector2Int>();
        bool hasValidSelection = false;

        foreach (Vector2Int selection in EnumerateSelections(data))
        {
            if (selection.x < 0 || selection.y < 0)
            {
                continue;
            }

            if (!candidates.Contains(selection) || !seen.Add(selection))
            {
                return false;
            }

            hasValidSelection = true;
        }

        return hasValidSelection;
    }

    protected override void ExecuteEffect(Vector2 targetSquare)
    {
        if (Caster == null || LogicManager == null)
        {
            return;
        }

        foreach (Vector2Int pawnPos in selectedPawns)
        {
            if (!Caster.IsPositionWithinBoard(pawnPos))
            {
                continue;
            }

            Piece piece = LogicManager.boardMap[pawnPos.x, pawnPos.y];
            if (piece is not Pawn pawn || pawn.IsWhite != Caster.IsWhite)
            {
                continue;
            }

            int direction = pawn.IsWhite ? 1 : -1;
            Vector2Int destination = pawnPos + new Vector2Int(0, direction);
            if (!Caster.IsPositionWithinBoard(destination))
            {
                continue;
            }

            if (LogicManager.boardMap[destination.x, destination.y] != null)
            {
                continue;
            }

            pawn.Move(new Vector2(destination.x, destination.y));
        }

        selectedPawns.Clear();
    }

    private List<Vector2Int> GetSelectablePawns()
    {
        List<Vector2Int> pawns = new List<Vector2Int>();
        if (Caster == null || LogicManager == null)
        {
            return pawns;
        }

        Vector2Int origin = Vector2Int.RoundToInt(Caster.GetCoordinates());

        for (int dx = -2; dx <= 2; dx++)
        {
            for (int dy = -2; dy <= 2; dy++)
            {
                Vector2Int pos = origin + new Vector2Int(dx, dy);
                if (!Caster.IsPositionWithinBoard(pos))
                {
                    continue;
                }

                Piece piece = LogicManager.boardMap[pos.x, pos.y];
                if (piece is not Pawn pawn || pawn.IsWhite != Caster.IsWhite)
                {
                    continue;
                }

                Vector2Int destination = pos + new Vector2Int(0, pawn.IsWhite ? 1 : -1);
                if (!Caster.IsPositionWithinBoard(destination))
                {
                    continue;
                }

                if (LogicManager.boardMap[destination.x, destination.y] != null)
                {
                    continue;
                }

                pawns.Add(pos);
            }
        }

        return pawns;
    }

    private Vector2Int GetSelectionOrInvalid(int index)
    {
        if (index < selectedPawns.Count)
        {
            return selectedPawns[index];
        }

        return new Vector2Int(-1, -1);
    }

    private void TryAddSelection(int x, int y)
    {
        if (x < 0 || y < 0)
        {
            return;
        }

        Vector2Int pos = new Vector2Int(x, y);
        if (!selectedPawns.Contains(pos))
        {
            selectedPawns.Add(pos);
        }
    }

    private IEnumerable<Vector2Int> EnumerateSelections(SpellCastData data)
    {
        yield return new Vector2Int(data.PrimaryX, data.PrimaryY);
        yield return new Vector2Int(data.SecondaryX, data.SecondaryY);
        yield return new Vector2Int(data.TertiaryX, data.TertiaryY);
    }

    private List<Vector2> ConvertToVector2(List<Vector2Int> ints)
    {
        List<Vector2> values = new List<Vector2>();
        foreach (Vector2Int value in ints)
        {
            values.Add(new Vector2(value.x, value.y));
        }

        return values;
    }
}