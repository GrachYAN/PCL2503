// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.InputSystem;
// using Unity.Netcode; // <-- Add this

// public class InputManager : MonoBehaviour
// {
//     private Piece selectedPiece;
//     private LogicManager logicManager;
//     private List<Square> highlightedSquares = new List<Square>();
//     private Square currentlyHighlightedSquare;
//     private InputAction clickAction;

//     void Start()
//     {
//         // Find the networked LogicManager
//         logicManager = Object.FindFirstObjectByType<LogicManager>();
        
//         clickAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
//         clickAction.performed += ctx => OnMouseClick();
//         clickAction.Enable();
//     }

//     void OnDestroy()
//     {
//         clickAction.Disable();
//     }

//     private void OnMouseClick()
//     {
//         // This check is now local and uses the networked variable
//         if (logicManager.isPromotionActive)
//         {
//             return;
//         }

//         // We only want to select/move if the NetworkManager is active
//         if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient)
//         {
//             // If we're not connected, don't do anything
//             // You might want to allow local play if not connected
//             // if (NetworkManager.Singleton == null) { /* allow local logic */ }
//             // else { return; }
//         }

//         SelectPiece();
//         MovePiece();
//     }

//     void SelectPiece()
//     {
//         Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
//         if (Physics.Raycast(ray, out RaycastHit hit))
//         {
//             Piece piece = hit.transform.GetComponent<Piece>();
//             if (piece != null)
//             {
//                 // Use the networked 'isWhiteTurn.Value'
//                 if ((piece.IsWhite && logicManager.isWhiteTurn.Value) || (!piece.IsWhite && !logicManager.isWhiteTurn.Value))
//                 {
//                     if (selectedPiece == piece)
//                     {
//                         UnhighlightSelectedSquare();
//                         UnhighlightLegalMoves();
//                         selectedPiece = null;
//                     }
//                     else
//                     {
//                         UnhighlightLegalMoves(); // Clear old moves
//                         UnhighlightSelectedSquare(); // Clear old selection
//                         selectedPiece = piece;
//                         HighlightSelectedSquare();
//                         HighlightLegalMoves(selectedPiece.GetLegalMoves());
//                     }
//                 }
//             }
//             else
//             {
//                 Square square = hit.transform.GetComponent<Square>();
//                 if (square != null)
//                 {
//                     Vector2 squareCoordinates = new Vector2(square.transform.position.x, square.transform.position.z);
//                     Piece pieceOnSquare = logicManager.boardMap[(int)squareCoordinates.x, (int)squareCoordinates.y];
//                     if (pieceOnSquare != null)
//                     {
//                         // Use the networked 'isWhiteTurn.Value'
//                         if ((pieceOnSquare.IsWhite && logicManager.isWhiteTurn.Value) || (!pieceOnSquare.IsWhite && !logicManager.isWhiteTurn.Value))
//                         {
//                             if (selectedPiece == pieceOnSquare)
//                             {
//                                 UnhighlightSelectedSquare();
//                                 UnhighlightLegalMoves();
//                                 selectedPiece = null;
//                             }
//                             else
//                             {
//                                 UnhighlightLegalMoves(); // Clear old moves
//                                 UnhighlightSelectedSquare(); // Clear old selection
//                                 selectedPiece = pieceOnSquare;
//                                 HighlightSelectedSquare();
//                                 HighlightLegalMoves(selectedPiece.GetLegalMoves());
//                             }
//                         }
//                     }
//                 }
//             }
//         }
//     }

//     // --- Highlight/Unhighlight methods are unchanged ---
//     #region (Highlighting Methods - No Changes)
//     void UnhighlightSelectedSquare()
//     {
//         if (currentlyHighlightedSquare != null)
//         {
//             currentlyHighlightedSquare.Unhighlight();
//             currentlyHighlightedSquare = null;
//         }
//     }

//     void HighlightSelectedSquare()
//     {
//         UnhighlightSelectedSquare();

//         Vector2 pieceCoordinates = selectedPiece.GetCoordinates();
//         currentlyHighlightedSquare = logicManager.GetSquareAtPosition(pieceCoordinates);

//         if (currentlyHighlightedSquare != null)
//         {
//             currentlyHighlightedSquare.Highlight(new Color(0f, 0.6f, 0.6f));
//         }
//     }

//     void HighlightLegalMoves(List<Vector2> legalMoves)
//     {
//         UnhighlightLegalMoves();
//         foreach (Vector2 move in legalMoves)
//         {
//             Square square = logicManager.GetSquareAtPosition(move);
//             Piece pieceOnSquare = logicManager.boardMap[(int)move.x, (int)move.y];
//             if (pieceOnSquare != null)
//             {
//                 square.Highlight(Color.red);
//             }
//             else
//             {
//                 square.Highlight(Color.cyan);
//             }

//             highlightedSquares.Add(square);
//         }
//     }

//     void UnhighlightLegalMoves()
//     {
//         foreach (Square square in highlightedSquares)
//         {
//             if(square != null) square.Unhighlight();
//         }
//         highlightedSquares.Clear();
//     }
//     #endregion


//     void MovePiece()
//     {
//         // We must have a piece selected to move
//         if (selectedPiece == null) return;

//         Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
//         if (Physics.Raycast(ray, out RaycastHit hit))
//         {
//             Square targetSquare = hit.transform.GetComponent<Square>();
//             Piece targetPiece = hit.transform.GetComponent<Piece>();

//             Vector2 targetCoordinates = -Vector2.one; // Invalid default
//             Square targetSquareComponent = null;

//             if (targetSquare != null)
//             {
//                 targetCoordinates = new Vector2(targetSquare.transform.position.x, targetSquare.transform.position.z);
//                 targetSquareComponent = targetSquare;
//             }
//             else if (targetPiece != null)
//             {
//                 targetCoordinates = new Vector2(targetPiece.transform.position.x, targetPiece.transform.position.z);
//                 targetSquareComponent = logicManager.GetSquareAtPosition(targetCoordinates);
//             }

//             // If we have a valid target square that is in our highlighted moves list
//             if (targetSquareComponent != null && highlightedSquares.Contains(targetSquareComponent))
//             {
//                 Vector2 startPosition = selectedPiece.GetCoordinates();
                
//                 // --- THIS IS THE KEY CHANGE ---
//                 // Instead of moving the piece, we REQUEST the server to move it.
//                 logicManager.RequestMoveServerRpc(
//                     (int)startPosition.x, 
//                     (int)startPosition.y, 
//                     (int)targetCoordinates.x, 
//                     (int)targetCoordinates.y
//                 );
//                 // --- END OF KEY CHANGE ---
                
//                 // We locally un-select and un-highlight. The piece will be moved
//                 // by the RPC from the server, which will update everyone.
//                 UnhighlightLegalMoves();
//                 UnhighlightSelectedSquare();
//                 selectedPiece = null;

//                 // All logic for sounds, EndTurn, etc., is GONE.
//                 // It now lives in LogicManager.PerformMove() and LogicManager.RequestMoveServerRpc()
//             }
//         }
//     }
// }

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode; // <-- Add this

public class InputManager : MonoBehaviour
{
    private Piece selectedPiece;
    private LogicManager logicManager;
    private List<Square> highlightedSquares = new List<Square>();
    private Square currentlyHighlightedSquare;
    private InputAction clickAction;

    void Start()
    {
        // Find the networked LogicManager
        logicManager = Object.FindFirstObjectByType<LogicManager>();
        
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
        // This check is now local and uses the networked variable
        if (logicManager.isPromotionActive)
        {
            return;
        }

        // We only want to select/move if the NetworkManager is active
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient)
        {
            // If we're not connected, don't do anything
            // You might want to allow local play if not connected
            // if (NetworkManager.Singleton == null) { /* allow local logic */ }
            // else { return; }
            return; // Added return here to be safe
        }
        
        // --- NEW CHECK ---
        // Prevent input if it's not our turn
        // We assume Host is White, Client-only is Black
        bool isMyTurn = (logicManager.isWhiteTurn.Value && NetworkManager.Singleton.IsHost) || 
                        (!logicManager.isWhiteTurn.Value && !NetworkManager.Singleton.IsHost && NetworkManager.Singleton.IsClient);

        if (!isMyTurn)
        {
            // It's not our turn, so deselect anything and stop
            if (selectedPiece != null)
            {
                UnhighlightSelectedSquare();
                UnhighlightLegalMoves();
                selectedPiece = null;
            }
            return;
        }
        // --- END NEW CHECK ---

        SelectPiece();
        MovePiece();
    }

    void SelectPiece()
    {
        // --- NEW LOCAL PLAYER COLOR CHECK ---
        // We can only select pieces of our own color.
        // Host (IsHost) is White. Client-only (!IsHost) is Black.
        bool amIWhite = NetworkManager.Singleton.IsHost;
        // --- END NEW CHECK ---

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Piece piece = hit.transform.GetComponent<Piece>();
            if (piece != null)
            {
                // NEW check: We check if the piece color matches our assigned color (amIWhite)
                if (piece.IsWhite == amIWhite)
                {
                    if (selectedPiece == piece)
                    {
                        UnhighlightSelectedSquare();
                        UnhighlightLegalMoves();
                        selectedPiece = null;
                    }
                    else
                    {
                        UnhighlightLegalMoves(); // Clear old moves
                        UnhighlightSelectedSquare(); // Clear old selection
                        selectedPiece = piece;
                        HighlightSelectedSquare();
                        HighlightLegalMoves(selectedPiece.GetLegalMoves());
                    }
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
                        // NEW check:
                        if (pieceOnSquare.IsWhite == amIWhite)
                        {
                            if (selectedPiece == pieceOnSquare)
                            {
                                UnhighlightSelectedSquare();
                                UnhighlightLegalMoves();
                                selectedPiece = null;
                            }
                            else
                            {
                                UnhighlightLegalMoves(); // Clear old moves
                                UnhighlightSelectedSquare(); // Clear old selection
                                selectedPiece = pieceOnSquare;
                                HighlightSelectedSquare();
                                HighlightLegalMoves(selectedPiece.GetLegalMoves());
                            }
                        }
                    }
                }
            }
        }
    }

    // --- Highlight/Unhighlight methods are unchanged ---
    #region (Highlighting Methods - No Changes)
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
            if(square != null) square.Unhighlight();
        }
        highlightedSquares.Clear();
    }
    #endregion


    void MovePiece()
    {
        // We must have a piece selected to move
        if (selectedPiece == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Square targetSquare = hit.transform.GetComponent<Square>();
            Piece targetPiece = hit.transform.GetComponent<Piece>();

            Vector2 targetCoordinates = -Vector2.one; // Invalid default
            Square targetSquareComponent = null;

            if (targetSquare != null)
            {
                targetCoordinates = new Vector2(targetSquare.transform.position.x, targetSquare.transform.position.z);
                targetSquareComponent = targetSquare;
            }
            else if (targetPiece != null)
            {
                targetCoordinates = new Vector2(targetPiece.transform.position.x, targetPiece.transform.position.z);
                targetSquareComponent = logicManager.GetSquareAtPosition(targetCoordinates);
            }

            // If we have a valid target square that is in our highlighted moves list
            if (targetSquareComponent != null && highlightedSquares.Contains(targetSquareComponent))
            {
                Vector2 startPosition = selectedPiece.GetCoordinates();
                
                // --- THIS IS THE KEY CHANGE ---
                // Instead of moving the piece, we REQUEST the server to move it.
                logicManager.RequestMoveServerRpc(
                    (int)startPosition.x, 
                    (int)startPosition.y, 
                    (int)targetCoordinates.x, 
                    (int)targetCoordinates.y
                );
                // --- END OF KEY CHANGE ---
                
                // We locally un-select and un-highlight. The piece will be moved
                // by the RPC from the server, which will update everyone.
                UnhighlightLegalMoves();
                UnhighlightSelectedSquare();
                selectedPiece = null;

                // All logic for sounds, EndTurn, etc., is GONE.
                // It now lives in LogicManager.PerformMove() and LogicManager.RequestMoveServerRpc()
            }
        }
    }
}