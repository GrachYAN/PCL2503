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
    
    // public bool isWhiteTurn; // This will be replaced by our new system

    // --- NEW TURN LOGIC ---
    // This is the "source of truth" for the turn in ONLINE mode.
    // It's server-authoritative.
    private NetworkVariable<bool> m_IsWhiteTurn_Network = new NetworkVariable<bool>(true);
    
    // This will be the "source of truth" ONLY for OFFLINE mode.
    private bool m_IsWhiteTurn_Offline = true;
    
    /// <summary>
    /// This is the public property everyone will read from.
    /// It automatically returns the correct variable for the current mode.
    /// </summary>
    public bool IsWhiteTurn
    {
        get
        {
            // --- FIX ---
            // If the manager is MISSING (e.g. testing scene directly) OR the mode is Offline,
            // use the offline variable.
            if (GameModeManager.Instance == null || GameModeManager.Instance.CurrentMode == GameModeManager.GameMode.Offline)
            {
                return m_IsWhiteTurn_Offline;
            }
            else
            {
                // We are online, return the networked value
                return m_IsWhiteTurn_Network.Value;
            }
        }
    }
    // --- END NEW TURN LOGIC ---

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

    void Start()
    {
        // This will run immediately in OFFLINE mode.
        // In Online mode, OnNetworkSpawn() will be called shortly after.

        // --- FIX ---
        // We check for null OR offline mode
        if (GameModeManager.Instance == null || GameModeManager.Instance.CurrentMode == GameModeManager.GameMode.Offline)
        {
            // We are offline, so OnNetworkSpawn won't run.
            // Stop the music here and initialize.
            if (LoginMusicManager.Instance != null)
            {
                LoginMusicManager.Instance.StopMusic();
            }
            Initialize(); // Initializes the offline turn
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

    // --- ALL THE MISSING RPCs AND METHODS ---

    [ServerRpc(RequireOwnership = false)]
    public void RequestMoveServerRpc(int startX, int startY, int endX, int endY, ServerRpcParams rpcParams = default)
    {
        Piece pieceToMove = boardMap[startX, startY];
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        // --- 1. Security & Turn Validation ---
        if (pieceToMove == null)
        {
            Debug.LogError($"Server: Client {senderClientId} requested to move a null piece.");
            return;
        }

        // Check 1: Is it this player's piece? (Host=0=White, Client=1=Black)
        bool isHost = senderClientId == 0;
        bool isMyPiece = (isHost && pieceToMove.IsWhite) || (!isHost && !pieceToMove.IsWhite);
        if (!isMyPiece)
        {
            Debug.LogError($"Server: Client {senderClientId} tried to move opponent's piece!");
            return;
        }

        // Check 2: Is it this color's turn?
        // Use the new public property here!
        if (pieceToMove.IsWhite != IsWhiteTurn)
        {
            Debug.LogError($"Server: Client {senderClientId} tried to move out of turn!");
            return;
        }

        // Check 3: Is the move legal?
        Vector2 targetPosition = new Vector2(endX, endY);
        List<Vector2> legalMoves = pieceToMove.GetLegalMoves();
        if (!legalMoves.Contains(targetPosition))
        {
            Debug.LogError($"Server: Client {senderClientId} tried to make an illegal move.");
            return;
        }

        // --- 2. Execute Move ---
        bool isCapture = boardMap[endX, endY] != null;
        bool isEnPassant = pieceToMove is Pawn &&
                           boardMap[endX, endY] == null &&
                           Mathf.Abs(endX - startX) == 1 &&
                           Mathf.Abs(endY - startY) == 1;

        pieceToMove.Move(targetPosition);
        lastMovedPiece = pieceToMove;
        lastMovedPieceStartPosition = new Vector2(startX, startY);
        lastMovedPieceEndPosition = targetPosition;

        // --- 3. Check for Promotion ---
        if (pieceToMove is Pawn && ((pieceToMove.IsWhite && endY == 7) || (!pieceToMove.IsWhite && endY == 0)))
        {
            isPromotionActive = true;
            // Tell the specific client who made the move to show the promotion UI
            ShowPromotionUIClientRpc(startX, startY, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { senderClientId } } });
        }
        else
        {
            isPromotionActive = false;
        }

        // --- 4. Tell ALL clients about the move ---
        MovePieceClientRpc(startX, startY, endX, endY, isCapture, isEnPassant);

        // --- 5. End Turn (if not promoting) ---
        if (!isPromotionActive)
        {
            UpdateCheckMap();
            EndTurn();
        }
    }

    [ClientRpc]
    private void MovePieceClientRpc(int startX, int startY, int endX, int endY, bool isCapture, bool isEnPassant, ClientRpcParams rpcParams = default)
    {
        // Don't execute this logic on the Host/Server, it already did.
        if (IsHost && GameModeManager.Instance.CurrentMode == GameModeManager.GameMode.Online) return;

        // Everyone else (clients) executes the move
        if (boardMap[startX, startY] != null)
        {
            Piece pieceToMove = boardMap[startX, startY];
            pieceToMove.Move(new Vector2(endX, endY));

            if (isEnPassant && captureSound != null)
            {
                captureSound.Play();
            }
            else if (!isCapture && moveSound != null)
            {
                moveSound.Play();
            }
            else if (isCapture && captureSound != null)
            {
                captureSound.Play();
            }
        }
    }

    [ClientRpc]
    private void ShowPromotionUIClientRpc(int pawnX, int pawnY, ClientRpcParams rpcParams)
    {
        // This will only run on the client specified in the rpcParams
        isPromotionActive = true;
        Pawn pawnToPromote = boardMap[pawnX, pawnY] as Pawn;
        if (pawnToPromote != null)
        {
            promotionUI.Show(pawnToPromote);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void CompletePromotionServerRpc(int pawnX, int pawnY, string pieceType, ServerRpcParams rpcParams = default)
    {
        // Security check (simplified):
        Pawn pawn = boardMap[pawnX, pawnY] as Pawn;
        if (pawn == null) return;
        
        // --- 1. Destroy old pawn and create new piece ---
        Destroy(pawn.gameObject);
        boardMap[pawnX, pawnY] = null;

        Board board = FindFirstObjectByType<Board>();
        GameObject prefabToSpawn = null;
        float yOffset = board.pieceYOffset;

        switch (pieceType)
        {
            case "Queen":
                prefabToSpawn = board.PiecePrefabs[4];
                break;
            case "Rook":
                prefabToSpawn = board.PiecePrefabs[1];
                break;
            case "Bishop":
                prefabToSpawn = board.PiecePrefabs[3];
                break;
            case "Knight":
                prefabToSpawn = board.PiecePrefabs[2];
                break;
        }

        if (prefabToSpawn != null)
        {
            Material pieceMaterial = pawn.IsWhite ? board.PieceMaterials[0] : board.PieceMaterials[1];
            Vector3 position = new Vector3(pawnX, yOffset, pawnY);
            board.InstantiatePiece(prefabToSpawn, position, pieceMaterial, pieceType, pawn.IsWhite);
        }
        
        // --- 2. Tell all clients to do the same ---
        CompletePromotionClientRpc(pawnX, pawnY, pieceType, pawn.IsWhite);

        // --- 3. End the turn ---
        isPromotionActive = false;
        UpdateCheckMap();
        EndTurn();
    }
    
    // --- NEW: OFFLINE Promotion Logic ---
    /// <summary>
    /// This is called by PromotionUI ONLY in Offline Mode.
    /// </summary>
    public void CompletePromotionOffline(int pawnX, int pawnY, string pieceType)
    {
        // Security check (simplified):
        Pawn pawn = boardMap[pawnX, pawnY] as Pawn;
        if (pawn == null) return;
        
        // --- 1. Destroy old pawn and create new piece ---
        Destroy(pawn.gameObject);
        boardMap[pawnX, pawnY] = null;

        Board board = FindFirstObjectByType<Board>();
        GameObject prefabToSpawn = null;
        float yOffset = board.pieceYOffset;

        switch (pieceType)
        {
            case "Queen":
                prefabToSpawn = board.PiecePrefabs[4];
                break;
            case "Rook":
                prefabToSpawn = board.PiecePrefabs[1];
                break;
            case "Bishop":
                prefabToSpawn = board.PiecePrefabs[3];
                break;
            case "Knight":
                prefabToSpawn = board.PiecePrefabs[2];
                break;
        }

        if (prefabToSpawn != null)
        {
            Material pieceMaterial = pawn.IsWhite ? board.PieceMaterials[0] : board.PieceMaterials[1];
            Vector3 position = new Vector3(pawnX, yOffset, pawnY);
            board.InstantiatePiece(prefabToSpawn, position, pieceMaterial, pieceType, pawn.IsWhite);
        }
        
        // --- 2. End the turn ---
        // (No ClientRpc needed, just run the logic)
        isPromotionActive = false;
        UpdateCheckMap();
        EndTurn();
    }
    // --- END NEW METHOD ---


    [ClientRpc]
    private void CompletePromotionClientRpc(int pawnX, int pawnY, string pieceType, bool isWhite)
    {
        // Don't execute this logic on the Host/Server, it already did.
        if (IsHost && GameModeManager.Instance.CurrentMode == GameModeManager.GameMode.Online) return;
        
        // Destroy the old pawn
        if (boardMap[pawnX, pawnY] != null)
        {
            Destroy(boardMap[pawnX, pawnY].gameObject);
            boardMap[pawnX, pawnY] = null;
        }

        // Instantiate the new piece
        Board board = FindFirstObjectByType<Board>();
        GameObject prefabToSpawn = null;
        float yOffset = board.pieceYOffset;

        switch (pieceType)
        {
            case "Queen":
                prefabToSpawn = board.PiecePrefabs[4];
                break;
            case "Rook":
                prefabToSpawn = board.PiecePrefabs[1];
                break;
            case "Bishop":
                prefabToSpawn = board.PiecePrefabs[3];
                break;
            case "Knight":
                prefabToSpawn = board.PiecePrefabs[2];
                break;
        }

        if (prefabToSpawn != null)
        {
            Material pieceMaterial = isWhite ? board.PieceMaterials[0] : board.PieceMaterials[1];
            Vector3 position = new Vector3(pawnX, yOffset, pawnY);
            board.InstantiatePiece(prefabToSpawn, position, pieceMaterial, pieceType, isWhite);
        }
        
        isPromotionActive = false;
    }

    // --- All your original helper methods ---
    public void Initialize()
    {
        // isWhiteTurn = true; // Old logic
        m_IsWhiteTurn_Offline = true; // Initialize the offline var
    }

    public bool IsMyTurn(bool pieceIsWhite)
    {
        // Use the new public property here!
        return pieceIsWhite == IsWhiteTurn;
    }

    public void EndTurn()
    {
        // isWhiteTurn = !isWhiteTurn; // Old logic
        
        // --- NEW END TURN LOGIC ---
        // --- FIX ---
        // If the manager is MISSING (e.g. testing scene directly) OR the mode is Offline,
        // use the offline logic.
        if (GameModeManager.Instance == null || GameModeManager.Instance.CurrentMode == GameModeManager.GameMode.Offline)
        {
            // --- OFFLINE ---
            // Just flip the local offline variable.
            m_IsWhiteTurn_Offline = !m_IsWhiteTurn_Offline;
            Debug.Log(m_IsWhiteTurn_Offline ? "--- OFFLINE: White's Turn ---" : "--- OFFLINE: Black's Turn ---");
            if (isCameraRotationEnabled)
            {
                if (cameraController == null) cameraController = FindFirstObjectByType<CameraController>();
                if (cameraController != null)
                {
                    if (IsWhiteTurn) // This will read the offline var
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
        else if (IsServer) // We must be Online and Server
        {
            // --- ONLINE (SERVER) ---
            // Flip the networked variable.
            m_IsWhiteTurn_Network.Value = !m_IsWhiteTurn_Network.Value;
        }
        // Client does not change the turn, it waits for the server.
        // --- END NEW LOGIC ---
        
        // This RPC is no longer needed
        // if (IsServer && GameModeManager.Instance.CurrentMode == GameModeManager.GameMode.Online)
        // {
        //     EndTurnClientRpc(isWhiteTurn);
        // }

        CheckGameOver();
        if (Time.timeScale == 0)
        {
            return;
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
        promotionUI.Show(pawn);
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
}