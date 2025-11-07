using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode; // Added for networking

public class InputManager : MonoBehaviour
{
    private Piece selectedPiece;
    private LogicManager logicManager;
    private List<Square> highlightedSquares = new List<Square>();
    private Square currentlyHighlightedSquare;
    private InputAction clickAction;

    // --- New variables for Online/Offline ---
    private bool isHost;
    private bool isOfflineMode = false;

    void Start()
    {
        logicManager = Object.FindFirstObjectByType<LogicManager>();

        // --- This new code is necessary ---
        if (GameModeManager.Instance != null && GameModeManager.Instance.CurrentMode == GameModeManager.GameMode.Offline)
        {
            isOfflineMode = true;
        }

        if (!isOfflineMode)
        {
            // We are in Online Mode, find out who we are.
            // This check assumes NetworkManager is valid, which it should be in Online Mode.
            if(NetworkManager.Singleton == null)
            {
                Debug.LogError("ERROR: In Online Mode but NetworkManager is missing!");
                return;
            }
            isHost = NetworkManager.Singleton.IsHost;
        }
        // --- End of new code ---

        clickAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
        clickAction.performed += ctx => OnMouseClick();
        clickAction.Enable();
    }

    void OnDestroy()
    {
        clickAction.Disable();
    }

    private void OnMouseClick()
    {
        if (logicManager.isPromotionActive)
        {
            return;
        }

        SelectPiece();
        MovePiece();
    }

    void SelectPiece()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Piece piece = hit.transform.GetComponent<Piece>();
            if (piece != null)
            {
                // --- THIS IS THE NEW LOGIC BLOCK ---

                // 1. Check if we are in Online Mode and if this piece belongs to us
                if (!isOfflineMode)
                {
                    // We are ONLINE. Check if it's our piece.
                    bool isMyPiece = (isHost && piece.IsWhite) || (!isHost && !piece.IsWhite);
                    if (!isMyPiece)
                    {
                        return; // Not your piece, do nothing
                    }
                }

                // 2. Check if it's this color's turn
                // (This logic is from your original file and works for both modes)
                // Use the new public property 'IsWhiteTurn'
                if (!((piece.IsWhite && logicManager.IsWhiteTurn) || (!piece.IsWhite && !logicManager.IsWhiteTurn)))
                {
                    return; // Not this color's turn
                }
                
                // --- END OF NEW LOGIC BLOCK ---

                // This is the original logic, which is correct
                if (selectedPiece == piece)
                {
                    UnhighlightSelectedSquare();
                    UnhighlightLegalMoves();
                    selectedPiece = null;
                }
                else
                {
                    selectedPiece = piece;
                    HighlightSelectedSquare();
                    HighlightLegalMoves(selectedPiece.GetLegalMoves());
                }
            }
            else
            {
                Square square = hit.transform.GetComponent<Square>();
                if (square != null)
                {
                    Vector2 squareCoordinates = new Vector2(square.transform.position.x, square.transform.position.z);
                    Piece pieceOnSquare = logicManager.boardMap[(int)squareCoordinates.x, (int)squareCoordinates.y];
                    if (pieceOnSquare != null)
                    {
                        // --- THIS IS THE NEW LOGIC BLOCK ---
                        
                        // 1. Check if we are in Online Mode and if this piece belongs to us
                        if (!isOfflineMode)
                        {
                            bool isMyPiece = (isHost && pieceOnSquare.IsWhite) || (!isHost && !pieceOnSquare.IsWhite);
                            if (!isMyPiece)
                            {
                                return; // Not your piece
                            }
                        }

                        // 2. Check if it's this color's turn
                        // Use the new public property 'IsWhiteTurn'
                        if (!((pieceOnSquare.IsWhite && logicManager.IsWhiteTurn) || (!pieceOnSquare.IsWhite && !logicManager.IsWhiteTurn)))
                        {
                            return; // Not this color's turn
                        }

                        // --- END OF NEW LOGIC BLOCK ---

                        // This is the original logic, which is correct
                        if (selectedPiece == pieceOnSquare)
                        {
                            UnhighlightSelectedSquare();
                            UnhighlightLegalMoves();
                            selectedPiece = null;
                        }
                        else
                        {
                            selectedPiece = pieceOnSquare;
                            HighlightSelectedSquare();
                            HighlightLegalMoves(selectedPiece.GetLegalMoves());
                        }
                    }
                }
            }
        }
    }

    void UnhighlightSelectedSquare()
    {
        if (currentlyHighlightedSquare != null)
        {
            currentlyHighlightedSquare.Unhighlight();
            currentlyHighlightedSquare = null;
        }
    }

    void HighlightSelectedSquare()
    {
        UnhighlightSelectedSquare();

        Vector2 pieceCoordinates = selectedPiece.GetCoordinates();
        currentlyHighlightedSquare = logicManager.GetSquareAtPosition(pieceCoordinates);

        if (currentlyHighlightedSquare != null)
        {
            currentlyHighlightedSquare.Highlight(new Color(0f, 0.6f, 0.6f));
        }
    }

    void HighlightLegalMoves(List<Vector2> legalMoves)
    {
        UnhighlightLegalMoves();
        foreach (Vector2 move in legalMoves)
        {
            Square square = logicManager.GetSquareAtPosition(move);
            Piece pieceOnSquare = logicManager.boardMap[(int)move.x, (int)move.y];
            if (pieceOnSquare != null)
            {
                square.Highlight(Color.red);
            }
            else
            {
                square.Highlight(Color.cyan);
            }

            highlightedSquares.Add(square);
        }
    }

    void UnhighlightLegalMoves()
    {
        foreach (Square square in highlightedSquares)
        {
            square.Unhighlight();
        }
        highlightedSquares.Clear();
    }

    void MovePiece()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Square targetSquare = hit.transform.GetComponent<Square>();
            Piece targetPiece = hit.transform.GetComponent<Piece>();

            Vector2 targetCoordinates = new Vector2(-1, -1);
            Square targetPieceSquare = null;

            if (targetSquare != null)
            {
                targetCoordinates = new Vector2(targetSquare.transform.position.x, targetSquare.transform.position.z);
            }
            else if (targetPiece != null)
            {
                targetCoordinates = new Vector2(targetPiece.transform.position.x, targetPiece.transform.position.z);
                targetPieceSquare = logicManager.GetSquareAtPosition(targetCoordinates);
            }
            else
            {
                return; // Didn't click a valid target
            }

            bool isValidTarget = (targetSquare != null && highlightedSquares.Contains(targetSquare)) ||
                                 (targetPieceSquare != null && highlightedSquares.Contains(targetPieceSquare));

            if (isValidTarget && selectedPiece != null)
            {
                Vector2 startPosition = selectedPiece.GetCoordinates();
                
                // --- THIS IS THE CRITICAL LOGIC BRANCH ---
                if (isOfflineMode)
                {
                    // --- OFFLINE: Run all logic locally ---
                    bool isCapture = logicManager.boardMap[(int)targetCoordinates.x, (int)targetCoordinates.y] != null;
                    bool isEnPassant = selectedPiece is Pawn &&
                       logicManager.boardMap[(int)targetCoordinates.x, (int)targetCoordinates.y] == null &&
                       Mathf.Abs(targetCoordinates.x - startPosition.x) == 1 &&
                       Mathf.Abs(targetCoordinates.y - startPosition.y) == 1;

                    selectedPiece.Move(targetCoordinates);
                    logicManager.lastMovedPiece = selectedPiece;
                    logicManager.lastMovedPieceStartPosition = startPosition;
                    logicManager.lastMovedPieceEndPosition = selectedPiece.GetCoordinates();

                    if (isEnPassant && logicManager.captureSound != null)
                    {
                        logicManager.captureSound.Play();
                    }
                    else if (!isCapture && logicManager.moveSound != null)
                    {
                        logicManager.moveSound.Play();
                    }
                    else if (isCapture && logicManager.captureSound != null)
                    {
                        logicManager.captureSound.Play();
                    }

                    if (!logicManager.isPromotionActive)
                    {
                        logicManager.UpdateCheckMap();
                        logicManager.EndTurn();
                    }

                }
                else
                {
                    // --- ONLINE: Send an RPC to the server ---
                    // The server will validate the move, execute it, and tell all clients.
                    logicManager.RequestMoveServerRpc((int)startPosition.x, (int)startPosition.y, (int)targetCoordinates.x, (int)targetCoordinates.y);
                }
                // --- END OF LOGIC BRANCH ---

                // This runs in both modes
                UnhighlightLegalMoves();
                UnhighlightSelectedSquare();
                selectedPiece = null;
            }
        }
    }
}