using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChessMiniDemo
{
    public class InputManager : MonoBehaviour
    {
        private Piece selectedPiece;
        private LogicManager logicManager;
        private List<Vector2> currentLegalMoves = new List<Vector2>();
        private List<Square> highlightedSquares = new List<Square>();

        private InputAction clickAction;

        [Header("Highlight Colors")]
        public Color selectedColor = new Color(0.3f, 0.6f, 1f, 1f);
        public Color legalMoveColor = new Color(0.2f, 1f, 0.2f, 1f);

        void Start()
        {
            logicManager = FindFirstObjectByType<LogicManager>();

            clickAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
            clickAction.performed += ctx => OnMouseClick();
            clickAction.Enable();
        }

        void OnDestroy()
        {
            if (clickAction != null) clickAction.Disable();
        }

        private void OnMouseClick()
        {
            if (logicManager.isPromotionActive) return;

            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit)) return;

            var clickedPiece = hit.transform.GetComponent<Piece>();
            var clickedSquare = hit.transform.GetComponent<Square>();

            if (clickedPiece != null)
            {
                if (clickedPiece.IsWhite == logicManager.isWhiteTurn)
                {
                    SelectPiece(clickedPiece);
                    return;
                }
                else
                {
                    if (selectedPiece != null)
                    {
                        TryMoveTo(clickedPiece.GetCoordinates());
                        return;
                    }
                }
            }

            if (clickedSquare != null && selectedPiece != null)
            {
                Vector3 pos = clickedSquare.transform.position;
                Vector2 target = new Vector2(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.z));
                TryMoveTo(target);
            }
        }

        private void SelectPiece(Piece piece)
        {
            if (selectedPiece == piece)
            {
                ClearHighlights();
                selectedPiece = null;
                currentLegalMoves.Clear();
                return;
            }

            selectedPiece = piece;
            currentLegalMoves = selectedPiece.GetLegalMoves();

            Vector2 c = selectedPiece.GetCoordinates();
            var sq = logicManager.squares[(int)c.x, (int)c.y];
            if (sq != null)
            {
                sq.Highlight(selectedColor);
                highlightedSquares.Add(sq);
            }

            foreach (var m in currentLegalMoves)
            {
                var s = logicManager.squares[(int)m.x, (int)m.y];
                if (s != null)
                {
                    s.Highlight(legalMoveColor);
                    highlightedSquares.Add(s);
                }
            }
        }

        private void TryMoveTo(Vector2 target)
        {
            foreach (var m in currentLegalMoves)
            {
                if (Mathf.Approximately(m.x, target.x) && Mathf.Approximately(m.y, target.y))
                {
                    ClearHighlights();
                    selectedPiece.Move(target);
                    selectedPiece = null;
                    currentLegalMoves.Clear();
                    return;
                }
            }
        }

        private void ClearHighlights()
        {
            foreach (var s in highlightedSquares)
            {
                if (s != null) s.Unhighlight();
            }
            highlightedSquares.Clear();
        }
    }
}
