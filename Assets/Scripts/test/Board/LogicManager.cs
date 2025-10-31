/*
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LogicManager : MonoBehaviour
{
    public Piece[,] boardMap = new Piece[8,8];
    public Square[,] squares = new Square[8, 8];
    public bool[,] whiteCheckMap = new bool[8,8]; //potencjalne szachy dla czarnego krola
    public bool[,] blackCheckMap = new bool[8, 8]; //potencjlane szachy dla bialego krola
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


    public void Start()
    {
        cameraController = FindFirstObjectByType<CameraController>();
        gameOverUI = FindFirstObjectByType<GameOverUI>();
        promotionUI = FindFirstObjectByType<PromotionUI>();
    }
    public void Initialize()
    {
        isWhiteTurn = true;
    }

    public void EndTurn()
    {
        isWhiteTurn = !isWhiteTurn;
        //Debug.Log(isWhiteTurn ? "White's turn" : "Black's turn");
        CheckGameOver();
        if (Time.timeScale == 0)
        {
            return;
        }

        if (isCameraRotationEnabled)
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
            //Debug.Log("Checking if king is in check...");
            if (king.CheckForChecks())
            {
                //Debug.Log("King is in check after the move!");
                return true;
            }
            else
            {
                //Debug.Log("King is not in check after the move.");
                return false;
            }
        } else
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

    public void CheckGameOver()
    {
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
}
*/


using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LogicManager : MonoBehaviour
{
    public Piece[,] boardMap = new Piece[8, 8];
    public Square[,] squares = new Square[8, 8];
    public bool[,] whiteCheckMap = new bool[8, 8]; //potencjalne szachy dla czarnego krola
    public bool[,] blackCheckMap = new bool[8, 8]; //potencjlane szachy dla bialego krola
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


    public void Start()
    {
        cameraController = FindFirstObjectByType<CameraController>();
        gameOverUI = FindFirstObjectByType<GameOverUI>();
        promotionUI = FindFirstObjectByType<PromotionUI>();

        // ✅ 添加：游戏开始时设置白方视角
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
        //Debug.Log(isWhiteTurn ? "White's turn" : "Black's turn");
        CheckGameOver();
        if (Time.timeScale == 0)
        {
            return;
        }

        // ✅ 修改：自动切换视角
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
            //Debug.Log("Checking if king is in check...");
            if (king.CheckForChecks())
            {
                //Debug.Log("King is in check after the move!");
                return true;
            }
            else
            {
                //Debug.Log("King is not in check after the move.");
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

    public void CheckGameOver()
    {
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
}
