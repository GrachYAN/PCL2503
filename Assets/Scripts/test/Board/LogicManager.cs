using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using UnityEngine.UI; // Make sure this is included if you reference GameOverUI/PromotionUI

public class LogicManager : NetworkBehaviour
{
    // --- All your original variables ---
    public Piece[,] boardMap = new Piece[8, 8];
    public Square[,] squares = new Square[8, 8];
    public bool[,] whiteCheckMap = new bool[8, 8];
    public bool[,] blackCheckMap = new bool[8, 8];

    // --- NEW TURN LOGIC ---
    // This is the "source of truth" for the turn in ONLINE mode.
    // It's server-authoritative.
    private NetworkVariable<bool> m_IsWhiteTurn_Network = new NetworkVariable<bool>(true);
    
    private bool isOfflineMode = false;
    private bool m_IsWhiteTurn_Offline = true;


    public bool IsWhiteTurn
    {
        get
        {
            if (isOfflineMode)
                return m_IsWhiteTurn_Offline;
            else
                return m_IsWhiteTurn_Network.Value;
        }
    }

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

    // --- REVISED: Awake() and Start() for proper initialization ---
    void Awake()
    {
        // Find UI elements here. Awake() runs before Start() and OnNetworkSpawn().
        // This ensures they are found in BOTH modes.
        cameraController = FindFirstObjectByType<CameraController>();
        gameOverUI = FindFirstObjectByType<GameOverUI>();
        promotionUI = FindFirstObjectByType<PromotionUI>();
    }

    public void Start()
    {
        cameraController = FindFirstObjectByType<CameraController>();
        gameOverUI = FindFirstObjectByType<GameOverUI>();
        promotionUI = FindFirstObjectByType<PromotionUI>();

        // This check is now required
        if (GameModeManager.Instance != null && GameModeManager.Instance.CurrentMode == GameModeManager.GameMode.Offline)
        {
            isOfflineMode = true;
        }

        if (cameraController != null)
        {
            cameraController.WhitePerspective();
        }
    }

    // --- OnNetworkSpawn() updated for Online Mode ---
    public override void OnNetworkSpawn()
    {
        // This will run in ONLINE mode.
        // Stop the login music if it's playing
        if (LoginMusicManager.Instance != null)
        {
            LoginMusicManager.Instance.StopMusic();
        }
        
        // Note: UI elements were already found in Awake()

        // We only want to subscribe to the event on clients
        if (IsClient)
        {
            m_IsWhiteTurn_Network.OnValueChanged += OnTurnChanged;
            // MovePieceClientRpc(0, 0, 0, 0, false, false, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { NetworkManager.Singleton.LocalClientId } } });
        }

        // Initialize must be called after OnNetworkSpawn for online mode
        // Initialize(); // This is called in Start() for offline, let's just initialize the network var
        if (IsServer)
        {
            m_IsWhiteTurn_Network.Value = true;
        }
    }

    private void OnTurnChanged(bool previousValue, bool newValue)
    {
        // We are in Online Mode, so we just run the camera logic
        if (isCameraRotationEnabled)
        {
            if (cameraController == null) cameraController = FindFirstObjectByType<CameraController>();
            if (cameraController != null)
            {
                // Use the 'newValue' from the event
                if (newValue) // If it's now White's turn
                {
                    cameraController.WhitePerspective();
                }
                else // If it's now Black's turn
                {
                    cameraController.BlackPerspective();
                }
            }
        }
    }

        [ServerRpc(RequireOwnership = false)]
    public void RequestMoveServerRpc(int startX, int startY, int endX, int endY, ServerRpcParams rpcParams = default)
    {
        Debug.Log($"Server: Received move request from {rpcParams.Receive.SenderClientId}");

        // --- 1. Validation ---
        Piece piece = boardMap[startX, startY];
        if (piece == null) return;
        Vector2 targetCoords = new Vector2(endX, endY);

        // --- 2. Security & Turn Validation ---
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        bool isHost = senderClientId == 0;
        bool isMyPiece = (isHost && piece.IsWhite) || (!isHost && !piece.IsWhite);
        if (!isMyPiece)
        {
            Debug.LogError($"Server: Client {senderClientId} tried to move opponent's piece!");
            return;
        }
        if (piece.IsWhite != IsWhiteTurn) // Checks m_IsWhiteTurn_Network.Value
        {
            Debug.LogError($"Server: Client {senderClientId} tried to move out of turn!");
            return;
        }
        if (!piece.GetLegalMoves().Contains(targetCoords))
        {
            Debug.LogError($"Server: Client {senderClientId} sent an illegal move!");
            return;
        }

        // --- 3. Execute Move ---
        bool isCapture = boardMap[endX, endY] != null;
        bool isEnPassant = piece is Pawn &&
           boardMap[endX, endY] == null &&
           Mathf.Abs(endX - startX) == 1 &&
           Mathf.Abs(endY - startY) == 1;

        piece.Move(targetCoords);
        MovePieceClientRpc(startX, startY, endX, endY);

        lastMovedPiece = piece;
        lastMovedPieceStartPosition = new Vector2(startX, startY);
        lastMovedPieceEndPosition = targetCoords;

        // Sounds should be triggered by a ClientRpc, but for now, this is fine
        if (isEnPassant && captureSound != null) { captureSound.Play(); }
        else if (!isCapture && moveSound != null) { moveSound.Play(); }
        else if (isCapture && captureSound != null) { captureSound.Play(); }

        // --- 4. End Turn ---
        if (!isPromotionActive)
        {
            UpdateCheckMap();
            EndTurn(); // This will flip m_IsWhiteTurn_Network.Value
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestCastSpellServerRpc(int pieceX, int pieceY, int spellIndex, int targetX, int targetY, ServerRpcParams rpcParams = default)
    {
        Debug.Log($"Server: Received spell cast request from {rpcParams.Receive.SenderClientId}");

        // --- 1. Validation ---
        Piece piece = boardMap[pieceX, pieceY];
        if (piece == null)
        {
            Debug.LogError("Server: Client tried to cast from a null piece.");
            return;
        }

        if (spellIndex < 0 || spellIndex >= piece.Spells.Count)
        {
            Debug.LogError("Server: Client sent invalid spell index.");
            return;
        }

        Spell spell = piece.Spells[spellIndex];
        Vector2 targetCoords = new Vector2(targetX, targetY);

        // --- 2. Security & Turn Validation ---
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        
        bool isHost = senderClientId == 0;
        bool isMyPiece = (isHost && piece.IsWhite) || (!isHost && !piece.IsWhite);
        if (!isMyPiece)
        {
            Debug.LogError($"Server: Client {senderClientId} tried to cast with opponent's piece!");
            return;
        }

        if (piece.IsWhite != IsWhiteTurn)
        {
            Debug.LogError($"Server: Client {senderClientId} tried to cast out of turn!");
            return;
        }

        if (!spell.CanCast())
        {
            Debug.LogError($"Server: Client {senderClientId} tried to cast spell, but CanCast() is false (cooldown/mana).");
            return;
        }

        if (!spell.GetValidTargetSquares().Contains(targetCoords))
        {
            Debug.LogError($"Server: Client {senderClientId} tried to cast at an invalid target.");
            return;
        }

        // --- 3. Execute Spell ---
        Debug.Log($"Server: Executing spell '{spell.SpellName}'");
        spell.Cast(targetCoords);
        CastSpellClientRpc(pieceX, pieceY, spellIndex, targetX, targetY);
        // --- 4. End Turn ---
        if (!isPromotionActive)
        {
            UpdateCheckMap();
            EndTurn(); // This flips the m_IsWhiteTurn_Network variable
        }
    }

    [ClientRpc]
    private void MovePieceClientRpc(int startX, int startY, int endX, int endY)
    {
        // This code now runs on EVERY client (including the server)
        Piece piece = boardMap[startX, startY];
        if (piece == null) return; // Should not happen if validation passed

        Vector2 targetCoords = new Vector2(endX, endY);
        
        // --- Execute the move logic for everyone ---
        bool isCapture = boardMap[endX, endY] != null;
        bool isEnPassant = piece is Pawn &&
           boardMap[endX, endY] == null &&
           Mathf.Abs(endX - startX) == 1 &&
           Mathf.Abs(endY - startY) == 1;

        // This moves the piece on everyone's local game
        piece.Move(targetCoords); 
        
        lastMovedPiece = piece;
        lastMovedPieceStartPosition = new Vector2(startX, startY);
        lastMovedPieceEndPosition = targetCoords;

        // Everyone plays the sound
        if (isEnPassant && captureSound != null) { captureSound.Play(); }
        else if (!isCapture && moveSound != null) { moveSound.Play(); }
        else if (isCapture && captureSound != null) { captureSound.Play(); }
    }

    [ClientRpc]
    private void CastSpellClientRpc(int pieceX, int pieceY, int spellIndex, int targetX, int targetY)
    {
        // This code now runs on EVERY client (including the server)
        Piece piece = boardMap[pieceX, pieceY];
        if (piece == null) return;
        if (spellIndex < 0 || spellIndex >= piece.Spells.Count) return;

        Spell spell = piece.Spells[spellIndex];
        Vector2 targetCoords = new Vector2(targetX, targetY);

        // This casts the spell on everyone's local game
        spell.Cast(targetCoords);
    }

    [ClientRpc]
    private void ShowPromotionUIClientRpc(int pawnX, int pawnY, ClientRpcParams rpcParams)
    {
        // This will only run on the client specified in the rpcParams
        isPromotionActive = true;
        Pawn pawnToPromote = boardMap[pawnX, pawnY] as Pawn;
        if (pawnToPromote != null)
        {
            // promotionUI.Show(pawnToPromote);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void CompletePromotionServerRpc(int pawnX, int pawnY, string pieceType, ServerRpcParams rpcParams = default){}

    public void CompletePromotionOffline(int pawnX, int pawnY, string pieceType) { }
    
    [ClientRpc]
    private void CompletePromotionClientRpc(int pawnX, int pawnY, string pieceType, bool isWhite){}

    // --- All your original helper methods ---
    public void Initialize()
    {
        // isWhiteTurn = true; // Old logic
        m_IsWhiteTurn_Offline = true; // Initialize the offline var
        if (IsServer) // Only the server can set the network variable's default
        {
            m_IsWhiteTurn_Network.Value = true;
        }
    }

    public bool IsMyTurn(bool pieceIsWhite)
    {
        // Use the new public property here!
        return pieceIsWhite == IsWhiteTurn;
    }


    public void EndTurn()
    {
        // --- This is the new turn-flipping logic ---
        if (isOfflineMode)
        {
            // In OFFLINE, just flip the local variable
            m_IsWhiteTurn_Offline = !m_IsWhiteTurn_Offline;
        }
        else
        {
            // In ONLINE, only the SERVER can flip the turn
            if (IsServer)
            {
                m_IsWhiteTurn_Network.Value = !m_IsWhiteTurn_Network.Value;
            }
            else
            {
                // A client should never call EndTurn directly
                Debug.LogWarning("Client tried to call EndTurn!");
                return;
            }
        }
        // --- End of new logic ---

        // All the rest of your EndTurn logic runs for everyone
        // (Clients will run this when they detect the network variable changed,
        // but for now, this server-driven approach is simpler)
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
            if (IsWhiteTurn) // This will use the correct property
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
                // Use the new public property here!
                if (piece is King && piece.IsWhite == IsWhiteTurn)
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
        if (gameOverUI == null) gameOverUI = FindFirstObjectByType<GameOverUI>();
        
        if (CheckKingStatus())
        {
            UpdatePiecesOnBoard();
            List<Piece> piecesCopy = new List<Piece>(piecesOnBoard);

            bool hasValidMoves = false;
            foreach (Piece piece in piecesCopy)
            {
                // Use the new public property here!
                if (piece != null && piece.IsWhite == IsWhiteTurn)
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
                string result = IsWhiteTurn ? "Black wins" : "White wins";
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
                // Use the new public property here!
                if (piece != null && piece.IsWhite == IsWhiteTurn)
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
        if (promotionUI == null) promotionUI = FindFirstObjectByType<PromotionUI>();
        // promotionUI.Show(pawn);
    }
    
    public void ToggleCameraRotation(bool isEnabled)
    {
        isCameraRotationEnabled = isEnabled;
        if (isCameraRotationEnabled)
        {
            if (cameraController != null)
            {
                // --- FIX ---
                // Use the new public property 'IsWhiteTurn'
                if (IsWhiteTurn)
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