using System.Collections.Generic;
using UnityEngine;

public class King : Piece
{
    public override List<Vector2> GetLegalMoves()
    {
        List<Vector2> legalMoves = base.GetLegalMoves();

        if (HasMoved == 0 && !logicManager.CheckKingStatus())
        {
            if (CanCastleKingside())
            {
                legalMoves.Add(new Vector2(6, GetCoordinates().y));
            }
            if (CanCastleQueenside())
            {
                legalMoves.Add(new Vector2(2, GetCoordinates().y));
            }
        }

        return legalMoves;
    }

    private bool CanCastleKingside()
    {
        int y = (int)GetCoordinates().y;
        Piece rook = logicManager.boardMap[7, y];
        if (rook is Rook && rook.HasMoved == 0)
        {
            bool[,] checkMap = IsWhite ? logicManager.blackCheckMap : logicManager.whiteCheckMap;

            bool canCastle = logicManager.boardMap[5, y] == null &&
                             logicManager.boardMap[6, y] == null &&
                             !checkMap[5, y] &&
                             !checkMap[6, y];
            return canCastle;
        }
        return false;
    }
    private bool CanCastleQueenside()
    {
        int y = (int)GetCoordinates().y;
        Piece rook = logicManager.boardMap[0, y];
        if (rook is Rook && rook.HasMoved == 0)
        {
            bool[,] checkMap = IsWhite ? logicManager.blackCheckMap : logicManager.whiteCheckMap;

            bool canCastle = logicManager.boardMap[1, y] == null &&
                             logicManager.boardMap[2, y] == null &&
                             logicManager.boardMap[3, y] == null &&
                             !checkMap[2, y] &&
                             !checkMap[3, y];
            return canCastle;
        }
        return false;
    }
    public override void Move(Vector2 newPosition)
    {
        Vector2 currentPosition = GetCoordinates();

        if (HasMoved == 0 && Mathf.Abs(newPosition.x - currentPosition.x) == 2)
        {
            int y = (int)currentPosition.y;

            if (newPosition.x == 6)
            {
                Piece rook = logicManager.boardMap[7, y];
                if (rook is Rook)
                {
                    rook.Move(new Vector2(5, y));
                }
            }
            else if (newPosition.x == 2)
            {
                Piece rook = logicManager.boardMap[0, y];
                if (rook is Rook)
                {
                    rook.Move(new Vector2(3, y));
                }
            }
        }
        base.Move(newPosition);
    }
    public bool CheckForChecks()
    {
        Vector2 currentCoordinates = GetCoordinates();
        bool[,] checkMap = IsWhite ? logicManager.blackCheckMap : logicManager.whiteCheckMap;

        bool isInCheck = checkMap[(int)currentCoordinates.x, (int)currentCoordinates.y];
        //Debug.Log($"Checking position ({currentCoordinates.x}, {currentCoordinates.y}) for checks: {isInCheck}");
        return isInCheck;
    }
    protected override List<Vector2> GetPotentialMoves()
    {
        List<Vector2> legalMoves = new List<Vector2>();
        int[] directionsX = { 1, -1, 0, 0, 1, -1, 1, -1 };
        int[] directionsY = { 0, 0, 1, -1, 1, -1, -1, 1 };

        Vector2 currentCoordinates = GetCoordinates();

        for (int i = 0; i < 8; i++)
        {
            Vector2 newPosition = new Vector2(
                currentCoordinates.x + directionsX[i],
                currentCoordinates.y + directionsY[i]
            );

            if (!IsPositionWithinBoard(newPosition))
                continue;

            Piece pieceAtNewPosition = logicManager.boardMap[(int)newPosition.x, (int)newPosition.y];

            if (pieceAtNewPosition == null || pieceAtNewPosition.IsWhite != IsWhite)
            {
                legalMoves.Add(newPosition);
            }
        }

        return legalMoves;
    }

    public override List<Vector2> GetAttackedFields()
    {
        List<Vector2> attackedFields = new List<Vector2>();
        int[] directionsX = { 1, -1, 0, 0, 1, -1, 1, -1 };
        int[] directionsY = { 0, 0, 1, -1, 1, -1, -1, 1 };

        Vector2 currentCoordinates = GetCoordinates();

        for (int i = 0; i < 8; i++)
        {
            Vector2 attackedPosition = new Vector2(
                currentCoordinates.x + directionsX[i],
                currentCoordinates.y + directionsY[i]
            );

            if (IsPositionWithinBoard(attackedPosition))
            {
                attackedFields.Add(attackedPosition);
            }
        }

        return attackedFields;
    }


}
