
using System.Collections.Generic;
using UnityEngine;

public class MindControl : Spell
{
    public MindControl()
    {
        SpellName = "Mind Control";
        Description = "Take control of an adjacent enemy for one turn. You can act with them immediately.";
        ManaCost = 5;
        Cooldown = 0;


        EndsTurn = false;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        return ConvertToVector2(GetAdjacentEnemies());
    }

    public override List<Vector2> GetCurrentValidSquares()
    {

        return ConvertToVector2(GetAdjacentEnemies());
    }

    public override bool TryHandleTargetSelection(Vector2 targetSquare, out bool castComplete)
    {
        castComplete = false;
        Vector2Int gridTarget = Vector2Int.RoundToInt(targetSquare);


        if (GetAdjacentEnemies().Contains(gridTarget))
        {
            castComplete = true;
            return true;
        }

        return false;
    }

    public override SpellCastData GetCastData(Vector2 finalTarget)
    {

        return SpellCastData.FromSingleTarget(finalTarget);
    }

    public override void ApplyCastData(SpellCastData data)
    {

    }

    protected override void ExecuteEffect(Vector2 targetSquare)
    {
        if (Caster == null || LogicManager == null) return;

        Vector2Int gridPos = Vector2Int.RoundToInt(targetSquare);
        Piece targetPiece = LogicManager.boardMap[gridPos.x, gridPos.y];

        if (targetPiece != null)
        {

            LogicManager.ApplyMindControl(Caster, targetPiece);

        }
    }

    private List<Vector2Int> GetAdjacentEnemies()
    {
        List<Vector2Int> targets = new List<Vector2Int>();
        if (Caster == null || LogicManager == null) return targets;

        Vector2Int casterPos = Vector2Int.RoundToInt(Caster.GetCoordinates());
        Vector2Int[] directions = {
            new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };

        foreach (var dir in directions)
        {
            Vector2Int checkPos = casterPos + dir;
            if (Caster.IsPositionWithinBoard(new Vector2(checkPos.x, checkPos.y)))
            {
                Piece p = LogicManager.boardMap[checkPos.x, checkPos.y];

                if (p != null && p.IsWhite != Caster.IsWhite && p.PieceType != "King" && p.PieceType != "Queen")
                {
                    targets.Add(checkPos);
                }
            }
        }
        return targets;
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
