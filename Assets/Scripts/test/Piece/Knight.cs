using System.Collections.Generic;
using UnityEngine;

public class Knight : Piece
{
    private static readonly Vector2[] MoveOffsets = new Vector2[]
    {
        new Vector2(2, 1), 
        new Vector2(2, -1),
        new Vector2(-2, 1), 
        new Vector2(-2, -1),
        new Vector2(1, 2), 
        new Vector2(1, -2),
        new Vector2(-1, 2), 
        new Vector2(-1, -2)
    };

    protected override List<Vector2> GetPotentialMoves()
    {
        List<Vector2> legalMoves = new List<Vector2>();
        Vector2 currentCoordinates = GetCoordinates();

        foreach (Vector2 offset in MoveOffsets)
        {
            Vector2 targetPosition = currentCoordinates + offset;

            if (IsPositionWithinBoard(targetPosition))
            {
                Piece pieceAtTarget = logicManager.boardMap[(int)targetPosition.x, (int)targetPosition.y];

                if (pieceAtTarget == null || pieceAtTarget.IsWhite != IsWhite)
                {
                    legalMoves.Add(targetPosition);
                }
            }
        }

        return legalMoves;
    }

    public override List<Vector2> GetAttackedFields()
    {
        List<Vector2> attackedFields = new List<Vector2>();
        Vector2 currentCoordinates = GetCoordinates();

        foreach (Vector2 offset in MoveOffsets)
        {
            Vector2 targetPosition = currentCoordinates + offset;

            if (IsPositionWithinBoard(targetPosition))
            {
                attackedFields.Add(targetPosition);
            }
        }

        return attackedFields;
    }
}
