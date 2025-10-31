using System.Collections.Generic;
using UnityEngine;

public class Rook : Piece
{
    protected override List<Vector2> GetPotentialMoves()
    {
        List<Vector2> legalMoves = new List<Vector2>();
        int[] directionsX = { 1, -1, 0, 0 };
        int[] directionsY = { 0, 0, 1, -1 };

        Vector2 currentCoordinates = GetCoordinates();

        for (int i = 0; i < 4; i++)
        {
            int step = 1;

            while (true)
            {
                Vector2 newPosition = new Vector2(
                    currentCoordinates.x + step * directionsX[i],
                    currentCoordinates.y + step * directionsY[i]
                );

                if (!IsPositionWithinBoard(newPosition))
                    break;

                Piece pieceAtNewPosition = logicManager.boardMap[(int)newPosition.x, (int)newPosition.y];

                if (pieceAtNewPosition == null)
                {
                    legalMoves.Add(newPosition);
                }
                else
                {
                    if (pieceAtNewPosition.IsWhite != IsWhite)
                    {
                        legalMoves.Add(newPosition);
                    }
                    break;
                }

                step++;
            }
        }

        return legalMoves;
    }

    public override List<Vector2> GetAttackedFields()
    {
        List<Vector2> attackedFields = new List<Vector2>();
        int[] directionsX = { 1, -1, 0, 0 };
        int[] directionsY = { 0, 0, 1, -1 };

        Vector2 currentCoordinates = GetCoordinates();

        for (int i = 0; i < 4; i++)
        {
            int step = 1;

            while (true)
            {
                Vector2 attackedPosition = new Vector2(
                    currentCoordinates.x + step * directionsX[i],
                    currentCoordinates.y + step * directionsY[i]
                );

                if (!IsPositionWithinBoard(attackedPosition))
                    break;

                attackedFields.Add(attackedPosition);

                Piece pieceAtAttackedPosition = logicManager.boardMap[(int)attackedPosition.x, (int)attackedPosition.y];
                if (pieceAtAttackedPosition != null)
                    break;

                step++;
            }
        }

        return attackedFields;
    }
}
