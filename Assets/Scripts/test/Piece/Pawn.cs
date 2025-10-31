using System.Collections.Generic;
using UnityEngine;

public class Pawn : Piece
{
    public override void Move(Vector2 newPosition)
    {
        if (Mathf.Abs(newPosition.x - GetCoordinates().x) == 1 && Mathf.Abs(newPosition.y - GetCoordinates().y) == 1)
        {
            Piece capturedPiece = logicManager.boardMap[(int)newPosition.x, (int)GetCoordinates().y];
            if (capturedPiece is Pawn && capturedPiece == logicManager.lastMovedPiece)
            {
                logicManager.boardMap[(int)newPosition.x, (int)GetCoordinates().y] = null;
                Destroy(capturedPiece.gameObject);
            }
        }

        base.Move(newPosition);

        if ((IsWhite && newPosition.y == 7) || (!IsWhite && newPosition.y == 0))
        {
            logicManager.HandlePromotion(this);
        }
    }
    protected override List<Vector2> GetPotentialMoves()
    {
        List<Vector2> legalMoves = new List<Vector2>();
        Vector2 currentCoordinates = GetCoordinates();
        int direction = IsWhite ? 1 : -1;

        if (logicManager.lastMovedPiece is Pawn lastMovedPawn)
        {
            Vector2 lastMoveStart = logicManager.lastMovedPieceStartPosition;
            Vector2 lastMoveEnd = logicManager.lastMovedPieceEndPosition;

            if (Mathf.Abs(lastMoveStart.y - lastMoveEnd.y) == 2)
            {
                Vector2 ourPosition = GetCoordinates();
                if (Mathf.Abs(ourPosition.x - lastMoveEnd.x) == 1 && ourPosition.y == lastMoveEnd.y)
                {
                    Vector2 enPassantMove = new Vector2(lastMoveEnd.x, ourPosition.y + (IsWhite ? 1 : -1));
                    legalMoves.Add(enPassantMove);
                }
            }
        }

        Vector2 forwardMove = new Vector2(currentCoordinates.x, currentCoordinates.y + direction);
        if (IsPositionWithinBoard(forwardMove) && logicManager.boardMap[(int)forwardMove.x, (int)forwardMove.y] == null)
        {
            legalMoves.Add(forwardMove);
        }

        if (HasMoved == 0)
        {
            Vector2 doubleForwardMove = new Vector2(currentCoordinates.x, currentCoordinates.y + (2 * direction));
            if (IsPositionWithinBoard(doubleForwardMove) && logicManager.boardMap[(int)forwardMove.x, (int)forwardMove.y] == null && logicManager.boardMap[(int)doubleForwardMove.x, (int)doubleForwardMove.y] == null)
            {
                legalMoves.Add(doubleForwardMove);
            }
        }

        Vector2 captureLeft = new Vector2(currentCoordinates.x - 1, currentCoordinates.y + direction);
        Vector2 captureRight = new Vector2(currentCoordinates.x + 1, currentCoordinates.y + direction);

        if (IsPositionWithinBoard(captureLeft) && logicManager.boardMap[(int)captureLeft.x, (int)captureLeft.y] != null)
        {
            Piece targetPiece = logicManager.boardMap[(int)captureLeft.x, (int)captureLeft.y];
            if (targetPiece != null && targetPiece.IsWhite != IsWhite)
            {
                legalMoves.Add(captureLeft);
            }
        }

        if (IsPositionWithinBoard(captureRight) && logicManager.boardMap[(int)captureRight.x, (int)captureRight.y] != null)
        {
            Piece targetPiece = logicManager.boardMap[(int)captureRight.x, (int)captureRight.y];
            if (targetPiece != null && targetPiece.IsWhite != IsWhite)
            {
                legalMoves.Add(captureRight);
            }
        }
        return legalMoves;
    }

    public override List<Vector2> GetAttackedFields()
    {
        List<Vector2> attackedFields = new List<Vector2>();
        int direction = IsWhite ? 1 : -1;
        
        Vector2 leftAttackMove = new Vector2(transform.position.x - 1, transform.position.z + direction);
        Vector2 rightAttackMove = new Vector2(transform.position.x + 1, transform.position.z + direction);

        if (IsPositionWithinBoard(leftAttackMove))
        {
            attackedFields.Add(leftAttackMove);
        }

        if (IsPositionWithinBoard(rightAttackMove))
        {
            attackedFields.Add(rightAttackMove);
        }

        return attackedFields;
    }


}
