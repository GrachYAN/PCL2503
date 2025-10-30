using System.Collections.Generic;
using UnityEngine;

namespace ChessMiniDemo
{
    public abstract class Piece : MonoBehaviour
    {
        protected LogicManager logicManager;
        public bool IsWhite { get; private set; }
        public string PieceType { get; private set; }
        public int HasMoved { get; private set; }

        protected virtual void Awake()
        {
            logicManager = FindFirstObjectByType<LogicManager>();
        }

        public virtual void Initialize(string pieceType, bool isWhite)
        {
            PieceType = pieceType;
            IsWhite = isWhite;

            if (logicManager == null)
                logicManager = FindFirstObjectByType<LogicManager>();

            logicManager.RegisterPiece(this);
        }

        public Vector2 GetCoordinates()
        {
            return new Vector2(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.z));
        }

        protected bool IsPositionWithinBoard(Vector2 pos)
        {
            return pos.x >= 0 && pos.x < 8 && pos.y >= 0 && pos.y < 8;
        }

        public abstract List<Vector2> GetAttackedFields();
        protected abstract List<Vector2> GetPotentialMoves();

        public virtual List<Vector2> GetLegalMoves()
        {
            List<Vector2> legalMoves = new List<Vector2>();

            foreach (var move in GetPotentialMoves())
            {
                if (WouldMoveBeLegal(move))
                {
                    legalMoves.Add(move);
                }
            }

            return legalMoves;
        }

        protected bool WouldMoveBeLegal(Vector2 target)
        {
            Vector2 from = GetCoordinates();

            Piece captured = logicManager.boardMap[(int)target.x, (int)target.y];

            logicManager.boardMap[(int)from.x, (int)from.y] = null;
            logicManager.boardMap[(int)target.x, (int)target.y] = this;

            Vector3 oldPos = transform.position;
            transform.position = new Vector3(target.x, oldPos.y, target.y);

            logicManager.UpdateCheckMap();
            bool isSelfInCheck = false;

            Piece myKing = null;
            foreach (var p in logicManager.piecesOnBoard)
            {
                if (p != null && p.PieceType == "King" && p.IsWhite == this.IsWhite)
                {
                    myKing = p;
                    break;
                }
            }
            if (myKing != null)
            {
                Vector2 kc = myKing.GetCoordinates();
                int kx = (int)kc.x, ky = (int)kc.y;
                bool[,] oppMap = this.IsWhite ? logicManager.blackCheckMap : logicManager.whiteCheckMap;
                isSelfInCheck = oppMap[kx, ky];
            }

            transform.position = oldPos;
            logicManager.boardMap[(int)from.x, (int)from.y] = this;
            logicManager.boardMap[(int)target.x, (int)target.y] = captured;
            logicManager.UpdateCheckMap();

            return !isSelfInCheck;
        }

        public virtual void Move(Vector2 newPosition)
        {
            Vector2 oldPos = GetCoordinates();

            logicManager.boardMap[(int)oldPos.x, (int)oldPos.y] = null;

            Piece target = logicManager.boardMap[(int)newPosition.x, (int)newPosition.y];
            if (target != null && target.IsWhite != IsWhite)
            {
                logicManager.piecesOnBoard.Remove(target);
                Object.Destroy(target.gameObject);
            }

            transform.position = new Vector3(newPosition.x, transform.position.y, newPosition.y);
            logicManager.boardMap[(int)newPosition.x, (int)newPosition.y] = this;

            HasMoved++;

            logicManager.lastMovedPieceStartPosition = oldPos;
            logicManager.lastMovedPieceEndPosition = newPosition;
            logicManager.lastMovedPiece = this;

            logicManager.UpdateCheckMap();
            logicManager.EndTurn();
        }
    }
}
