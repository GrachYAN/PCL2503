using System.Collections.Generic;
using UnityEngine;

public class Pawn : Piece
{

    public override void Move(Vector2 newPosition)
    {
        base.Move(newPosition);

    }
    protected override List<Vector2> GetPotentialMoves()
    {
        List<Vector2> legalMoves = new List<Vector2>();
        Vector2 currentCoordinates = GetCoordinates();
        int direction = IsWhite ? 1 : -1;

        Vector2 forwardMove = new Vector2(currentCoordinates.x, currentCoordinates.y + direction);
        if (IsPositionWithinBoard(forwardMove)
            && !logicManager.IsPrismaticBarrierBlockingSquare(forwardMove, IsWhite)
            && logicManager.boardMap[(int)forwardMove.x, (int)forwardMove.y] == null)
        {
            legalMoves.Add(forwardMove);
        }

        if (HasMoved == 0)
        {
            Vector2 doubleForwardMove = new Vector2(currentCoordinates.x, currentCoordinates.y + (2 * direction));
            if (IsPositionWithinBoard(doubleForwardMove)
                && !logicManager.IsPrismaticBarrierBlockingSquare(forwardMove, IsWhite)
                && !logicManager.IsPrismaticBarrierBlockingSquare(doubleForwardMove, IsWhite)
                && logicManager.boardMap[(int)forwardMove.x, (int)forwardMove.y] == null
                && logicManager.boardMap[(int)doubleForwardMove.x, (int)doubleForwardMove.y] == null)
            {
                legalMoves.Add(doubleForwardMove);
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
