using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode; // <-- Add this

// Make LogicManager a NetworkBehaviour
public class LogicManager : NetworkBehaviour
{
    public Piece[,] boardMap = new Piece[8, 8];
    public Square[,] squares = new Square[8, 8];
    public bool[,] whiteCheckMap = new bool[8, 8];
    public bool[,] blackCheckMap = new bool[8, 8];

    // This is our new synchronized turn variable
    public NetworkVariable<bool> isWhiteTurn = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // We remove the old 'public bool isWhiteTurn;'
    
    public List<Piece> piecesOnBoard;
    private CameraController cameraController;
    private GameOverUI gameOverUI;
    private PromotionUI promotionUI;
    public bool isPromotionActive = false; // This remains a local variable for UI state
    public Piece lastMovedPiece;
    public Vector2 lastMovedPieceStartPosition;
    public Vector2 lastMovedPieceEndPosition;
    public AudioSource moveSound;
    public AudioSource captureSound;
    public bool isCameraRotationEnabled = true;
    public bool isSoundEnabled = true;
    public float soundVolume = 0.5f;

    // We'll use this to send promotion requests to the correct client
    private ulong m_ClientMakingMove;

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

    // OnNetworkSpawn is called when this object is networked
    public override void OnNetworkSpawn()
    {   
        if (LoginMusicManager.Instance != null)
        {
            LoginMusicManager.Instance.StopMusic();
        }

        // We only want to subscribe to the event on clients
        if (!IsServer)
        {
            // Subscribe to the turn variable changing
            isWhiteTurn.OnValueChanged += OnTurnChanged;
        }

        // Make sure the camera is correct when we join
        OnTurnChanged(isWhiteTurn.Value, isWhiteTurn.Value);
    }

    public override void OnNetworkDespawn()
    {
        // Unsubscribe when we're destroyed
        if (!IsServer)
        {
            isWhiteTurn.OnValueChanged -= OnTurnChanged;
        }
    }

    // This method will be called on all clients when the turn changes
    private void OnTurnChanged(bool previousValue, bool newValue)
    {
        if (isCameraRotationEnabled && cameraController != null)
        {
            if (newValue) // newValue is the 'current' turn
            {
                cameraController.WhitePerspective();
            }
            else
            {
                cameraController.BlackPerspective();
            }
        }
    }

    public void Initialize()
    {
        // The server sets the starting turn
        if (IsServer)
        {
            isWhiteTurn.Value = true;
        }
    }

    public void EndTurn()
    {
        // Only the Server can end the turn
        if (!IsServer) return;

        isWhiteTurn.Value = !isWhiteTurn.Value;
        
        // The server is responsible for checking game over
        CheckGameOver();
        if (Time.timeScale == 0)
        {
            return;
        }

        // The camera rotation is now handled by the OnValueChanged event
        // So we can remove the logic from here.
    }

    // This is the new ServerRpc that clients will call to request a move
    [ServerRpc(RequireOwnership = false)]
    public void RequestMoveServerRpc(int startX, int startY, int endX, int endY, ServerRpcParams rpcParams = default)
    {
        // Store who made this request
        m_ClientMakingMove = rpcParams.Receive.SenderClientId;
        ulong senderClientId = rpcParams.Receive.SenderClientId; // Get the client ID

        Piece piece = boardMap[startX, startY];
        if (piece == null) return; // No piece at start

        // --- NEW, STRONGER VALIDATION ---
        // We assume Host (Client 0) is White
        // We assume Client (any other ID) is Black
        bool isSenderWhite = (senderClientId == 0);

        // 1. Check if the piece color matches the sender's role
        if (piece.IsWhite != isSenderWhite)
        {
            Debug.Log($"Server: Move rejected. Client {senderClientId} cannot move {(piece.IsWhite ? "White" : "Black")} piece.");
            return;
        }

        // 2. Check if it's the correct turn for that color
        if (isWhiteTurn.Value != piece.IsWhite)
        {
            Debug.Log("Server: Move rejected. Not your turn.");
            return; // Not your turn
        }
        // --- END NEW VALIDATION ---


        // Validate the move on the server
        List<Vector2> legalMoves = piece.GetLegalMoves();
        if (legalMoves.Contains(new Vector2(endX, endY)))
        {
            // Move is legal!
            // 1. Execute the move locally on the server
            PerformMove(startX, startY, endX, endY);

            // 2. Tell all clients to execute the same move
            PerformMoveClientRpc(startX, startY, endX, endY);

            // 3. If a promotion wasn't triggered, end the turn.
            // (If promotion WAS triggered, HandlePromotion will be called
            // by the pawn's Move() method, and IT will be responsible for ending the turn
            // after the choice is made)
            if (!isPromotionActive)
            {
                EndTurn();
            }
        }
        else
        {
             Debug.Log("Server: Move rejected. Illegal move.");
        }
    }

    [ClientRpc]
    private void PerformMoveClientRpc(int startX, int startY, int endX, int endY)
    {
        // The server already ran this logic, so it skips this.
        if (IsServer) return;

        PerformMove(startX, startY, endX, endY);
    }

    /// <summary>
    /// This helper function contains the *actual* move logic.
    /// It is run on the Server (from ServerRpc) and on all Clients (from ClientRpc)
    /// to keep the board state in sync.
    /// </summary>
    public void PerformMove(int startX, int startY, int endX, int endY)
    {
        Vector2 squareCoordinates = new Vector2(endX, endY);
        Vector2 startPosition = new Vector2(startX, startY);
        Piece selectedPiece = boardMap[startX, startY];

        if (selectedPiece == null)
        {
            Debug.LogError($"PerformMove error: No piece found at {startX},{startY}");
            return;
        }

        bool isCapture = boardMap[(int)squareCoordinates.x, (int)squareCoordinates.y] != null;
        bool isEnPassant = selectedPiece is Pawn &&
           boardMap[(int)squareCoordinates.x, (int)squareCoordinates.y] == null &&
           Mathf.Abs(squareCoordinates.x - startPosition.x) == 1 &&
           Mathf.Abs(squareCoordinates.y - startPosition.y) == 1;

        // This Move() call will update the boardMap, move the piece,
        // and importantly, call HandlePromotion if it's a promotion.
        selectedPiece.Move(squareCoordinates);

        lastMovedPiece = selectedPiece;
        lastMovedPieceStartPosition = startPosition;
        lastMovedPieceEndPosition = selectedPiece.GetCoordinates();

        // Play sounds locally on each machine
        if (isSoundEnabled)
        {
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

        // Everyone updates their own internal check maps
        if (!isPromotionActive)
        {
            UpdateCheckMap();
        }
    }


    // --- PROMOTION NETWORKING ---

    // 1. Called by Pawn.Move() when a promotion happens.
    // This will now only run on the SERVER.
    public void HandlePromotion(Pawn pawn)
    {
        isPromotionActive = true; // Server's local state
        
        Vector2 pawnPos = pawn.GetCoordinates();

        // 2. Tell the specific client who made the move to show their UI
        ClientRpcParams rpcParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { m_ClientMakingMove } } };
        RequestPromotionClientRpc((int)pawnPos.x, (int)pawnPos.y, rpcParams);
    }

    [ClientRpc]
    private void RequestPromotionClientRpc(int pawnX, int pawnY, ClientRpcParams rpcParams)
    {
        // 3. This runs on the specific client.
        isPromotionActive = true; // Client's local state
        Pawn pawn = boardMap[pawnX, pawnY] as Pawn;
        if (pawn != null)
        {
            promotionUI.Show(pawn);
        }
    }

    // 4. The PromotionUI button will call this ServerRpc
    [ServerRpc(RequireOwnership = false)]
    public void CompletePromotionServerRpc(int pawnX, int pawnY, string pieceType)
    {
        // 5. Server executes the promotion
        Pawn pawn = boardMap[pawnX, pawnY] as Pawn;
        PromotePawn(pawn, pieceType);

        // 6. Tell all clients to execute the promotion
        PromotePawnClientRpc(pawnX, pawnY, pieceType);

        isPromotionActive = false; // Server state
        EndTurn(); // NOW the turn ends.
    }

    [ClientRpc]
    private void PromotePawnClientRpc(int pawnX, int pawnY, string pieceType)
    {
        if (IsServer) return;

        // 7. All clients execute the promotion
        Pawn pawn = boardMap[pawnX, pawnY] as Pawn;
        PromotePawn(pawn, pieceType);
        isPromotionActive = false; // Client state
    }

    /// <summary>
    /// Helper function to actually perform the promotion.
    /// This was missing, so I'm adding it based on your Board script.
    /// This runs on Server and all Clients.
    /// </summary>
    public void PromotePawn(Pawn pawn, string pieceType)
    {
        if (pawn == null) return;

        Vector2 pos = pawn.GetCoordinates();
        bool isWhite = pawn.IsWhite;
        
        // Remove old pawn
        boardMap[(int)pos.x, (int)pos.y] = null;
        Destroy(pawn.gameObject);

        // Find board and prefabs
        Board board = FindFirstObjectByType<Board>();
        if (board == null)
        {
            Debug.LogError("Cannot find Board to get prefabs!");
            return;
        }

        GameObject prefab = null;
        switch (pieceType)
        {
            case "Queen": prefab = board.PiecePrefabs[4]; break;
            case "Rook": prefab = board.PiecePrefabs[1]; break;
            case "Bishop": prefab = board.PiecePrefabs[3]; break;
            case "Knight": prefab = board.PiecePrefabs[2]; break;
        }

        if (prefab == null)
        {
            Debug.LogError($"Invalid promotion pieceType: {pieceType}");
            return;
        }

        Material mat = isWhite ? board.PieceMaterials[0] : board.PieceMaterials[1];
        float yOffset = (pieceType == "Pawn") ? board.pawnYOffset : board.pieceYOffset; // Use correct offset

        // Instantiate new piece
        board.InstantiatePiece(prefab, new Vector3(pos.x, yOffset, pos.y), mat, pieceType, isWhite);
        
        // Update check maps on all machines
        UpdateCheckMap();
    }


    // --- All other methods (UpdateCheckMap, ResetCheckMap, GetSquareAtPosition, CheckKingStatus, UpdatePiecesOnBoard, CheckGameOver, Toggles, etc.) ---
    // --- can remain exactly as they are. ---
    // --- CheckGameOver will now only be called on the Server, which is correct. ---

    #region (Original Methods - No Changes Needed)
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
                if (piece is King && piece.IsWhite == isWhiteTurn.Value) // <-- Use .Value
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

    public void CheckGameOver()
    {
        // This now only runs on the Server, which is correct.
        if (CheckKingStatus())
        {
            UpdatePiecesOnBoard();
            List<Piece> piecesCopy = new List<Piece>(piecesOnBoard);

            bool hasValidMoves = false;
            foreach (Piece piece in piecesCopy)
            {
                if (piece != null && piece.IsWhite == isWhiteTurn.Value) // <-- Use .Value
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
                string result = isWhiteTurn.Value ? "Black wins" : "White wins"; // <-- Use .Value
                // TODO: Need to RPC the GameOverUI call
                // gameOverUI.ShowGameOver(result);
                // Time.timeScale = 0;
                Debug.Log($"Game Over: {result}");
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
                if (piece is King) kingCount++;
                else if (piece is Knight) knightCount++;
                else if (piece is Bishop) bishopCount++;
            }

            if (kingCount == 2 && (knightCount == 0 && bishopCount == 0 || knightCount == 1 && bishopCount == 0 || knightCount == 0 && bishopCount == 1))
            {
                string result = "Draw";
                // TODO: Need to RPC the GameOverUI call
                // gameOverUI.ShowGameOver(result);
                // Time.timeScale = 0;
                Debug.Log($"Game Over: {result}");
                return;
            }

            bool hasValidMoves = false;
            foreach (Piece piece in piecesCopy)
            {
                if (piece != null && piece.IsWhite == isWhiteTurn.Value) // <-- Use .Value
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
                // TODO: Need to RPC the GameOverUI call
                // gameOverUI.ShowGameOver(result);
                // Time.timeScale = 0;
                Debug.Log($"Game Over: {result}");
            }
        }
    }
    
    public void ToggleCameraRotation(bool isEnabled)
    {
        isCameraRotationEnabled = isEnabled;
        if (isCameraRotationEnabled)
        {
            if (cameraController != null)
            {
                if (isWhiteTurn.Value) // <-- Use .Value
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
    #endregion
}