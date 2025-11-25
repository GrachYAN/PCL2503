using System.Collections.Generic;
using UnityEngine;

public class HolyRadiance : Spell
{
    private const int MaxTargets = 2;
    private readonly List<Vector2Int> selectedTargets = new List<Vector2Int>();

    public HolyRadiance()
    {
        SpellName = "Holy Radiance";
        Description = "Heal up to two allies on diagonals within 2 for 4 HP each.";
        ManaCost = 5;
        Cooldown = 3;
    }

    public override void BeginTargeting()
    {
        selectedTargets.Clear();
    }

    public override void CancelTargeting()
    {
        selectedTargets.Clear();
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        return ConvertToVector2(GetHealableTargets());
    }

    public override List<Vector2> GetCurrentValidSquares()
    {
        List<Vector2> targets = new List<Vector2>();
        List<Vector2Int> candidates = GetHealableTargets();

        foreach (Vector2Int pos in candidates)
        {
            if (!selectedTargets.Contains(pos))
            {
                targets.Add(new Vector2(pos.x, pos.y));
            }
        }

        if (selectedTargets.Count > 0 && selectedTargets.Count < MaxTargets)
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
            if (selectedTargets.Count == 0)
            {
                return false;
            }

            castComplete = true;
            return true;
        }

        List<Vector2Int> candidates = GetHealableTargets();
        if (!candidates.Contains(gridTarget) || selectedTargets.Contains(gridTarget))
        {
            return false;
        }

        selectedTargets.Add(gridTarget);

        if (selectedTargets.Count >= MaxTargets || selectedTargets.Count == candidates.Count)
        {
            castComplete = true;
        }

        return true;
    }

    public override SpellCastData GetCastData(Vector2 finalTarget)
    {
        Vector2Int first = GetSelectionOrInvalid(0);
        Vector2Int second = GetSelectionOrInvalid(1);

        return new SpellCastData
        {
            PrimaryX = first.x,
            PrimaryY = first.y,
            SecondaryX = second.x,
            SecondaryY = second.y,
            TertiaryX = -1,
            TertiaryY = -1
        };
    }

    public override void ApplyCastData(SpellCastData data)
    {
        selectedTargets.Clear();
        TryAddSelection(data.PrimaryX, data.PrimaryY);
        TryAddSelection(data.SecondaryX, data.SecondaryY);
    }

    public override bool IsCastDataValid(SpellCastData data)
    {
        if (Caster == null || LogicManager == null)
        {
            return false;
        }

        List<Vector2Int> candidates = GetHealableTargets();
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

        foreach (Vector2Int target in selectedTargets)
        {
            if (!Caster.IsPositionWithinBoard(target))
            {
                continue;
            }

            Piece piece = LogicManager.boardMap[target.x, target.y];
            if (piece != null && piece.IsWhite == Caster.IsWhite)
            {
                piece.Heal(4);
            }
        }

        selectedTargets.Clear();
    }

    private List<Vector2Int> GetHealableTargets()
    {
        List<Vector2Int> targets = new List<Vector2Int>();
        if (Caster == null || LogicManager == null)
        {
            return targets;
        }

        Vector2Int origin = Vector2Int.RoundToInt(Caster.GetCoordinates());

        for (int distance = 1; distance <= 2; distance++)
        {
            Vector2Int[] offsets =
            {
                new Vector2Int(distance, distance), new Vector2Int(distance, -distance),
                new Vector2Int(-distance, distance), new Vector2Int(-distance, -distance)
            };

            foreach (Vector2Int offset in offsets)
            {
                Vector2Int pos = origin + offset;
                if (!Caster.IsPositionWithinBoard(pos))
                {
                    continue;
                }

                Piece piece = LogicManager.boardMap[pos.x, pos.y];
                if (piece != null && piece.IsWhite == Caster.IsWhite)
                {
                    targets.Add(pos);
                }
            }
        }

        return targets;
    }

    private Vector2Int GetSelectionOrInvalid(int index)
    {
        if (index < selectedTargets.Count)
        {
            return selectedTargets[index];
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
        if (!selectedTargets.Contains(pos))
        {
            selectedTargets.Add(pos);
        }
    }

    private IEnumerable<Vector2Int> EnumerateSelections(SpellCastData data)
    {
        yield return new Vector2Int(data.PrimaryX, data.PrimaryY);
        yield return new Vector2Int(data.SecondaryX, data.SecondaryY);
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