
using System.Collections.Generic;
using UnityEngine;

public abstract class Piece : MonoBehaviour
{
    protected LogicManager logicManager;
    public bool IsWhite { get; private set; }
    public string PieceType { get; private set; }
    public int HasMoved { get; private set; }

    public abstract List<Vector2> GetAttackedFields();
    protected abstract List<Vector2> GetPotentialMoves();
    public virtual List<Vector2> GetLegalMoves()
    {
        List<Vector2> legalMoves = new List<Vector2>();
        bool isKingInCheck = logicManager.CheckKingStatus();
        foreach (Vector2 move in GetPotentialMoves())
        {
            if (WillMoveEndCheck(move))
            {
                legalMoves.Add(move);
            }
        }
        //Debug.Log($"Legal moves for {PieceType}: {legalMoves.Count}");

        return legalMoves;
    }

    protected bool WillMoveEndCheck(Vector2 move)
    {
        Piece originalPiece = logicManager.boardMap[(int)move.x, (int)move.y];
        Vector2 originalPosition = GetCoordinates();

        logicManager.boardMap[(int)originalPosition.x, (int)originalPosition.y] = null;
        logicManager.boardMap[(int)move.x, (int)move.y] = this;
        logicManager.UpdateCheckMap();

        bool isKingInCheck = logicManager.CheckKingStatus();

        if (this is King)
        {
            isKingInCheck = IsWhite ? logicManager.blackCheckMap[(int)move.x, (int)move.y] : logicManager.whiteCheckMap[(int)move.x, (int)move.y];
        }

        logicManager.boardMap[(int)originalPosition.x, (int)originalPosition.y] = this;
        logicManager.boardMap[(int)move.x, (int)move.y] = originalPiece;
        logicManager.UpdateCheckMap();

        return !isKingInCheck;
    }

    public void Initialize(string pieceType, bool isWhite)
    {
        PieceType = pieceType;
        IsWhite = isWhite;
        HasMoved = 0;
    }

    private void Start()
    {
        logicManager = Object.FindFirstObjectByType<LogicManager>();
        UpdateBoardMap();
    }

    public Vector2 GetCoordinates()
    {
        return new Vector2(transform.position.x, transform.position.z);
    }

    public void UpdateBoardMap()
    {
        Vector2 coordinates = GetCoordinates();
        logicManager.boardMap[(int)coordinates.x, (int)coordinates.y] = this;
    }

    public virtual void Move(Vector2 newPosition)
    {
        Vector2 currentCoordinates = GetCoordinates();
        logicManager.boardMap[(int)currentCoordinates.x, (int)currentCoordinates.y] = null;
        HasMoved = 1;
        Take(newPosition);
        transform.position = new Vector3(newPosition.x, transform.position.y, newPosition.y);
        UpdateBoardMap();
    }

    public void Take(Vector2 targetPosition)
    {
        Piece targetPiece = logicManager.boardMap[(int)targetPosition.x, (int)targetPosition.y];
        if (targetPiece != null)
        {
            Destroy(targetPiece.gameObject);
        }
        logicManager.boardMap[(int)targetPosition.x, (int)targetPosition.y] = null;
    }

    public bool IsPositionWithinBoard(Vector2 position)
    {
        return position.x >= 0 && position.x < 8 && position.y >= 0 && position.y < 8;
    }

}


/*
using System.Collections.Generic;
using UnityEngine;

public abstract class Piece : MonoBehaviour
{
    protected LogicManager logicManager;
    public bool IsWhite { get; private set; }
    public string PieceType { get; private set; }
    public int HasMoved { get; private set; }

    public void Initialize(bool isWhite, string pieceType)
    {
        IsWhite = isWhite;
        PieceType = pieceType;
        HasMoved = 0;
        logicManager = Object.FindFirstObjectByType<LogicManager>();
    }

    public Vector2 GetCoordinates()
    {
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (logicManager.boardMap[x, y] == this)
                {
                    return new Vector2(x, y);
                }
            }
        }
        return new Vector2(-1, -1);
    }

    public virtual void Move(Vector2 newPosition)
    {
        Vector2 oldPosition = GetCoordinates();

        // ✅ 吃子逻辑：检查目标位置是否有敌方棋子
        Piece targetPiece = logicManager.boardMap[(int)newPosition.x, (int)newPosition.y];
        if (targetPiece != null && targetPiece.IsWhite != this.IsWhite)
        {
            Debug.Log($"🍽️ {this.PieceType}({(this.IsWhite ? "白" : "黑")}) 吃掉了 {targetPiece.PieceType}({(targetPiece.IsWhite ? "白" : "黑")})");

            // ✅ 从棋盘上移除被吃的棋子
            logicManager.piecesOnBoard.Remove(targetPiece);
            Destroy(targetPiece.gameObject);
        }

        // 更新棋盘状态
        logicManager.boardMap[(int)oldPosition.x, (int)oldPosition.y] = null;
        logicManager.boardMap[(int)newPosition.x, (int)newPosition.y] = this;

        // 移动棋子到新位置
        transform.position = new Vector3(newPosition.x, transform.position.y, newPosition.y);
        HasMoved++;

        Debug.Log($"✅ {PieceType}({(IsWhite ? "白" : "黑")}) 从 ({oldPosition.x},{oldPosition.y}) 移动到 ({newPosition.x},{newPosition.y})");
    }

    public List<Vector2> GetLegalMoves()
    {
        List<Vector2> potentialMoves = GetPotentialMoves();
        List<Vector2> legalMoves = new List<Vector2>();

        foreach (Vector2 move in potentialMoves)
        {
            if (IsMoveLegal(move))
            {
                legalMoves.Add(move);
            }
        }

        return legalMoves;
    }

    protected abstract List<Vector2> GetPotentialMoves();

    public virtual List<Vector2> GetPotentialMoves()
    {
        return new List<Vector2>();
    }

    private bool IsMoveLegal(Vector2 move)
    {
        Vector2 currentPos = GetCoordinates();
        Piece targetPiece = logicManager.boardMap[(int)move.x, (int)move.y];

        // 临时移动
        logicManager.boardMap[(int)currentPos.x, (int)currentPos.y] = null;
        logicManager.boardMap[(int)move.x, (int)move.y] = this;

        // 检查是否会导致自己的国王被将军
        bool wouldBeInCheck = WouldKingBeInCheck();

        // 恢复棋盘状态
        logicManager.boardMap[(int)currentPos.x, (int)currentPos.y] = this;
        logicManager.boardMap[(int)move.x, (int)move.y] = targetPiece;

        return !wouldBeInCheck;
    }

    private bool WouldKingBeInCheck()
    {
        // 找到己方国王
        Piece king = null;
        foreach (Piece piece in logicManager.piecesOnBoard)
        {
            if (piece != null && piece.PieceType == "King" && piece.IsWhite == this.IsWhite)
            {
                king = piece;
                break;
            }
        }

        if (king == null) return false;

        Vector2 kingPos = king.GetCoordinates();

        // 检查所有敌方棋子是否能攻击到国王
        foreach (Piece piece in logicManager.piecesOnBoard)
        {
            if (piece != null && piece.IsWhite != this.IsWhite)
            {
                List<Vector2> enemyMoves = piece.GetPotentialMoves();
                if (enemyMoves.Contains(kingPos))
                {
                    return true;
                }
            }
        }

        return false;
    }

    protected bool IsWithinBoard(int x, int y)
    {
        return x >= 0 && x < 8 && y >= 0 && y < 8;
    }

    protected bool IsSquareEmpty(int x, int y)
    {
        return logicManager.boardMap[x, y] == null;
    }

    protected bool IsSquareEnemy(int x, int y)
    {
        Piece piece = logicManager.boardMap[x, y];
        return piece != null && piece.IsWhite != this.IsWhite;
    }
}
*/