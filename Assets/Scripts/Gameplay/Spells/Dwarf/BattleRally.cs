using System.Collections.Generic;
using UnityEngine;

public class BattleRally : Spell
{
    private const int MaxSelections = 3;
    private readonly List<Vector2Int> selectedPieces = new List<Vector2Int>();

    public BattleRally()
    {
        SpellName = "Battle Rally";
        Description = "Up to three Pieces within range 2 each move 1 forward (legal, free).";
        ManaCost = 4;
        Cooldown = 0;
    }

    public override void BeginTargeting()
    {
        selectedPieces.Clear();
    }

    public override void CancelTargeting()
    {
        selectedPieces.Clear();
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        return ConvertToVector2(GetSelectablePieces());
    }

    public override List<Vector2> GetCurrentValidSquares()
    {
        List<Vector2> targets = new List<Vector2>();
        List<Vector2Int> candidates = GetSelectablePieces();

        foreach (Vector2Int piece in candidates)
        {
            if (!selectedPieces.Contains(piece))
            {
                targets.Add(new Vector2(piece.x, piece.y));
            }
        }

        if (selectedPieces.Count > 0 && selectedPieces.Count < MaxSelections)
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
            if (selectedPieces.Count == 0)
            {
                return false;
            }

            castComplete = true;
            return true;
        }

        List<Vector2Int> candidates = GetSelectablePieces();
        if (!candidates.Contains(gridTarget) || selectedPieces.Contains(gridTarget))
        {
            return false;
        }

        selectedPieces.Add(gridTarget);

        if (selectedPieces.Count >= MaxSelections || selectedPieces.Count == candidates.Count)
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
        selectedPieces.Clear();
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

        List<Vector2Int> candidates = GetSelectablePieces();
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

        foreach (Vector2Int piecePos in selectedPieces)
        {
            if (!Caster.IsPositionWithinBoard(piecePos))
            {
                continue;
            }

            Piece piece = LogicManager.boardMap[piecePos.x, piecePos.y];
            if (piece == null || piece.IsWhite != Caster.IsWhite)
            {
                continue;
            }

            int direction = piece.IsWhite ? 1 : -1;
            Vector2Int destination = piecePos + new Vector2Int(0, direction);
            if (!Caster.IsPositionWithinBoard(destination))
            {
                continue;
            }

            if (LogicManager.boardMap[destination.x, destination.y] != null)
            {
                continue;
            }

            piece.Move(new Vector2(destination.x, destination.y));
        }

        selectedPieces.Clear();
    }

    private List<Vector2Int> GetSelectablePieces()
    {
        List<Vector2Int> pieces = new List<Vector2Int>();
        if (Caster == null || LogicManager == null)
        {
            return pieces;
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
                if (piece == null || piece.IsWhite != Caster.IsWhite)
                {
                    continue;
                }

                // Check if this piece can move 1 forward
                int direction = piece.IsWhite ? 1 : -1;
                Vector2Int destination = pos + new Vector2Int(0, direction);
                if (!Caster.IsPositionWithinBoard(destination))
                {
                    continue;
                }

                if (LogicManager.boardMap[destination.x, destination.y] != null)
                {
                    continue;
                }

                pieces.Add(pos);
            }
        }

        return pieces;
    }

    private Vector2Int GetSelectionOrInvalid(int index)
    {
        if (index < selectedPieces.Count)
        {
            return selectedPieces[index];
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
        if (!selectedPieces.Contains(pos))
        {
            selectedPieces.Add(pos);
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