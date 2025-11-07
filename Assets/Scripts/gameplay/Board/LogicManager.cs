using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LogicManager : MonoBehaviour
{
    // ... (你所有的变量 boardMap, squares, whiteCheckMap ... isSoundEnabled, soundVolume 都保持不变) ...
    public Piece[,] boardMap = new Piece[8, 8];
    public Square[,] squares = new Square[8, 8];
    public bool[,] whiteCheckMap = new bool[8, 8];
    public bool[,] blackCheckMap = new bool[8, 8];
    public bool isWhiteTurn;
    public List<Piece> piecesOnBoard;
    private CameraController cameraController;
    private GameOverUI gameOverUI;
    private PromotionUI promotionUI;
    public bool isPromotionActive = false;
    public Piece lastMovedPiece;
    public Vector2 lastMovedPieceStartPosition;
    public Vector2 lastMovedPieceEndPosition;
    public AudioSource moveSound;
    public AudioSource captureSound;
    public bool isCameraRotationEnabled = true;
    public bool isSoundEnabled = true;
    public float soundVolume = 0.5f;

    // Start (无变化)
    public void Start()
    {
        cameraController = FindFirstObjectByType<CameraController>();
        gameOverUI = FindFirstObjectByType<GameOverUI>();
        promotionUI = FindFirstObjectByType<PromotionUI>();

        if (cameraController != null)
        {
            cameraController.WhitePerspective();
        }
    }

    public void Initialize()
    {
        isWhiteTurn = true;
    }

    public void EndTurn()
    {
        isWhiteTurn = !isWhiteTurn;


        // 注意：规则书说 "each piece"，所以我们为 *所有* 棋子调用
        foreach (Piece piece in piecesOnBoard)
        {
            if (piece != null)
            {
                piece.OnTurnStart();
            }
        }


        CheckGameOver();
        if (Time.timeScale == 0)
        {
            return;
        }

        if (isCameraRotationEnabled && cameraController != null)
        {
            if (isWhiteTurn)
            {
                cameraController.WhitePerspective();
            }
            else
            {
                cameraController.BlackPerspective();
            }
        }
    }

    public void UpdateCheckMap()
    {
        ResetCheckMap();
        UpdatePiecesOnBoard();

        foreach (Piece piece in piecesOnBoard)
        {
            if (piece != null)
            {
                List<Vector2> attackedFields = piece.GetAttackedFields();

                foreach (Vector2 field in attackedFields)
                {
                    if (piece.IsWhite)
                    {
                        whiteCheckMap[(int)field.x, (int)field.y] = true;
                    }
                    else
                    {
                        blackCheckMap[(int)field.x, (int)field.y] = true;
                    }
                }
            }
        }
    }

    private void ResetCheckMap()
    {
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                whiteCheckMap[x, y] = false;
                blackCheckMap[x, y] = false;
            }
        }
    }

    public Square GetSquareAtPosition(Vector2 position)
    {
        if (position.x < 0 || position.x >= squares.GetLength(0) || position.y < 0 || position.y >= squares.GetLength(1))
        {
            return null;
        }
        return squares[(int)position.x, (int)position.y];
    }

    public bool CheckKingStatus()
    {
        King king = null;

        for (int x = 0; x < boardMap.GetLength(0); x++)
        {
            for (int y = 0; y < boardMap.GetLength(1); y++)
            {
                Piece piece = boardMap[x, y];
                if (piece is King && piece.IsWhite == isWhiteTurn)
                {
                    king = (King)piece;
                    break;
                }
            }
            if (king != null)
            {
                break;
            }
        }

        if (king != null)
        {
            if (king.CheckForChecks())
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }

    private void UpdatePiecesOnBoard()
    {
        piecesOnBoard.Clear();
        for (int x = 0; x < boardMap.GetLength(0); x++)
        {
            for (int y = 0; y < boardMap.GetLength(1); y++)
            {
                Piece piece = boardMap[x, y];
                if (piece != null)
                {
                    piecesOnBoard.Add(piece);
                }
            }
        }
    }

    // TODO: King Kill By Spell 后的游戏结束检测?
    public void CheckGameOver()
    {
        // ... (你现有的 CheckGameOver 逻辑) ...
        if (CheckKingStatus())
        {
            UpdatePiecesOnBoard();
            List<Piece> piecesCopy = new List<Piece>(piecesOnBoard);

            bool hasValidMoves = false;
            foreach (Piece piece in piecesCopy)
            {
                if (piece != null && piece.IsWhite == isWhiteTurn)
                {
                    if (piece.GetLegalMoves().Count > 0)
                    {
                        hasValidMoves = true;
                        break;
                    }
                }
            }

            if (!hasValidMoves)
            {
                string result = isWhiteTurn ? "Black wins" : "White wins";
                gameOverUI.ShowGameOver(result);
                Time.timeScale = 0;
            }
        }
        else
        {
            UpdatePiecesOnBoard();
            List<Piece> piecesCopy = new List<Piece>(piecesOnBoard);

            int kingCount = 0;
            int knightCount = 0;
            int bishopCount = 0;

            foreach (Piece piece in piecesCopy)
            {
                if (piece is King)
                {
                    kingCount++;
                }
                else if (piece is Knight)
                {
                    knightCount++;
                }
                else if (piece is Bishop)
                {
                    bishopCount++;
                }
            }

            if (kingCount == 2 && (knightCount == 0 && bishopCount == 0 || knightCount == 1 && bishopCount == 0 || knightCount == 0 && bishopCount == 1))
            {
                string result = "Draw";
                gameOverUI.ShowGameOver(result);
                Time.timeScale = 0;
                return;
            }

            bool hasValidMoves = false;
            foreach (Piece piece in piecesCopy)
            {
                if (piece != null && piece.IsWhite == isWhiteTurn)
                {
                    if (piece.GetLegalMoves().Count > 0)
                    {
                        hasValidMoves = true;
                        break;
                    }
                }
            }

            if (!hasValidMoves)
            {
                string result = "Draw";
                gameOverUI.ShowGameOver(result);
                Time.timeScale = 0;
            }
        }
    }

    public void HandlePromotion(Pawn pawn)
    {
        isPromotionActive = true;
        promotionUI.Show(pawn);
    }

    public void ToggleCameraRotation(bool isEnabled)
    {
        isCameraRotationEnabled = isEnabled;
        if (isCameraRotationEnabled)
        {
            if (cameraController != null)
            {
                if (isWhiteTurn)
                {
                    cameraController.WhitePerspective();
                }
                else
                {
                    cameraController.BlackPerspective();
                }
            }
        }
    }
    public void ToggleSound(bool isEnabled)
    {
        isSoundEnabled = isEnabled;
        moveSound.mute = !isSoundEnabled;
        captureSound.mute = !isSoundEnabled;
    }
    public void SetSoundVolume(float volume)
    {
        soundVolume = volume;
        moveSound.volume = soundVolume;
        captureSound.volume = soundVolume;
    }


    //  新增：用于HP归零时销毁棋子 
    public void DestroyPiece(Piece piece)
    {
        if (piece == null) return;

        Vector2 coords = piece.GetCoordinates();
        boardMap[(int)coords.x, (int)coords.y] = null;

        if (piecesOnBoard.Contains(piece))
        {
            piecesOnBoard.Remove(piece);
        }

        Destroy(piece.gameObject);
        Debug.Log($"{piece.PieceType}已被摧毁。");

        // TODO: 检查被摧毁的是否是 King，如果是，则游戏结束
        if (piece is King)
        {
            string result = piece.IsWhite ? "Black wins" : "White wins";
            gameOverUI.ShowGameOver(result);
            Time.timeScale = 0;
        }
    }

    public bool HasLineOfSight(Vector2 start, Vector2 end)
    {
        // 此实现适用于直线和对角线。
        Vector2 direction = end - start;
        Vector2 step = Vector2.zero;

        // 确定步进方向 (dx, dy 只能是 -1, 0, 或 1)
        float dx = Mathf.Clamp(direction.x, -1, 1);
        float dy = Mathf.Clamp(direction.y, -1, 1);
        step = new Vector2(dx, dy);

        if (direction.x != 0 && direction.y != 0 && Mathf.Abs(direction.x) != Mathf.Abs(direction.y))
        {
            // 对于非直线/对角线，如 骑士(Knight)，LoS 通常不适用
            return true;
        }

        Vector2 currentPos = start + step; // 从起点后一格开始检查

        while (Vector2.Distance(currentPos, end) > 0.1f) // 检查是否到达终点
        {
            if (currentPos.x < 0 || currentPos.x >= 8 || currentPos.y < 0 || currentPos.y >= 8)
            {
                break; // 超出棋盘
            }

            // 检查格子上是否有棋子
            if (boardMap[(int)currentPos.x, (int)currentPos.y] != null)
            {
                return false; // 被阻挡！ 
            }

            currentPos += step;
        }

        // 循环完成，没有障碍物
        return true;
    }
}