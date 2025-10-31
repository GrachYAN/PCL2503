using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private Piece selectedPiece;
    private LogicManager logicManager;
    private List<Square> highlightedSquares = new List<Square>();
    private Square currentlyHighlightedSquare;
    private InputAction clickAction;

    void Start()
    {
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
                if ((piece.IsWhite && logicManager.isWhiteTurn) || (!piece.IsWhite && !logicManager.isWhiteTurn))
                {
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
                        if ((pieceOnSquare.IsWhite && logicManager.isWhiteTurn) || (!pieceOnSquare.IsWhite && !logicManager.isWhiteTurn))
                        {
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

            if (targetSquare != null && highlightedSquares.Contains(targetSquare))
            {
                Vector2 squareCoordinates = new Vector2(targetSquare.transform.position.x, targetSquare.transform.position.z);
                Vector2 startPosition = selectedPiece.GetCoordinates();
                bool isCapture = logicManager.boardMap[(int)squareCoordinates.x, (int)squareCoordinates.y] != null;
                bool isEnPassant = selectedPiece is Pawn &&
                   logicManager.boardMap[(int)squareCoordinates.x, (int)squareCoordinates.y] == null &&
                   Mathf.Abs(squareCoordinates.x - startPosition.x) == 1 &&
                   Mathf.Abs(squareCoordinates.y - startPosition.y) == 1;

                selectedPiece.Move(squareCoordinates);
                logicManager.lastMovedPiece = selectedPiece;
                logicManager.lastMovedPieceStartPosition = startPosition;
                logicManager.lastMovedPieceEndPosition = selectedPiece.GetCoordinates();
                UnhighlightLegalMoves();
                UnhighlightSelectedSquare();
                selectedPiece = null;

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
            else if (targetPiece != null)
            {
                Vector2 targetCoordinates = new Vector2(targetPiece.transform.position.x, targetPiece.transform.position.z);
                Square targetPieceSquare = logicManager.GetSquareAtPosition(targetCoordinates);

                if (highlightedSquares.Contains(targetPieceSquare))
                {
                    Vector2 startPosition = selectedPiece.GetCoordinates();
                    bool isCapture = logicManager.boardMap[(int)targetCoordinates.x, (int)targetCoordinates.y] != null;

                    selectedPiece.Move(targetCoordinates);
                    logicManager.lastMovedPiece = selectedPiece;
                    logicManager.lastMovedPieceStartPosition = startPosition;
                    logicManager.lastMovedPieceEndPosition = selectedPiece.GetCoordinates();
                    UnhighlightLegalMoves();
                    UnhighlightSelectedSquare();
                    selectedPiece = null;

                    if (!isCapture && logicManager.moveSound != null)
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
            }
        }
    }
}