using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour
{
    private enum InputState
    {
        None,
        PieceSelected,
        Moving,
        CastingSpell
    }

    private InputState currentState = InputState.None;
    private Piece selectedPiece;
    private Spell selectedSpell;

    private LogicManager logicManager;
    private List<Square> highlightedSquares = new List<Square>();
    private Square currentlyHighlightedSquare;
    private InputAction clickAction;
    private Camera mainCamera;

    [Header("UI 引用")]
    public GameObject actionPanel;
    public GameObject actionButtonPrefab; // ⬅️ 修改：使用按钮模板

    private RectTransform actionPanelRectTransform;

    void Start()
    {
        logicManager = Object.FindFirstObjectByType<LogicManager>();
        clickAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
        clickAction.performed += ctx => OnMouseClick();
        clickAction.Enable();

        mainCamera = Camera.main;
        if (actionPanel != null)
        {
            actionPanel.SetActive(false);
            actionPanelRectTransform = actionPanel.GetComponent<RectTransform>();
        }
    }

    void OnDestroy()
    {
        clickAction.Disable();
    }

    void Update()
    {
        if (actionPanel != null && actionPanel.activeSelf && selectedPiece != null)
        {
            UpdateActionPanelPosition();
        }
    }

    private void UpdateActionPanelPosition()
    {
        Vector3 pieceWorldPos = selectedPiece.transform.position + new Vector3(0, 1.5f, 0);
        Vector2 screenPos = mainCamera.WorldToScreenPoint(pieceWorldPos);
        actionPanelRectTransform.position = screenPos;
    }

    private void OnMouseClick()
    {
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (logicManager.isPromotionActive) return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit))
        {
            ResetSelection();
            return;
        }

        switch (currentState)
        {
            case InputState.None:
            case InputState.PieceSelected:
                TrySelectPiece(hit);
                break;
            case InputState.Moving:
                TryMoveToTarget(hit);
                break;
            case InputState.CastingSpell:
                TryCastAtTarget(hit);
                break;
        }
    }

    public void OnMoveButton()
    {
        if (selectedPiece == null || currentState != InputState.PieceSelected) return;

        currentState = InputState.Moving;
        selectedSpell = null;
        actionPanel.SetActive(false);

        Debug.Log("选择移动...请选择目标格子。");
        HighlightLegalMoves(selectedPiece.GetLegalMoves());
    }

    public void OnSpellButton(int spellIndex)
    {
        if (selectedPiece == null || spellIndex >= selectedPiece.Spells.Count || currentState != InputState.PieceSelected)
        {
            Debug.LogWarning($"无法施法: 棋子未选或索引无效 {spellIndex}");
            return;
        }

        Spell spell = selectedPiece.Spells[spellIndex];
        if (spell.CanCast())
        {
            currentState = InputState.CastingSpell;
            selectedSpell = spell;
            actionPanel.SetActive(false);

            Debug.Log($"选择施法: {spell.SpellName}...请选择目标。");
            HighlightLegalMoves(spell.GetValidTargetSquares());
        }
        else
        {
            Debug.Log($"无法施放该技能: {spell.SpellName} (法力不足或冷却中)");
        }
    }

    private void TrySelectPiece(RaycastHit hit)
    {
        Piece piece = hit.transform.GetComponent<Piece>();
        if (piece == null)
        {
            Square square = hit.transform.GetComponent<Square>();
            if (square != null)
            {
                Vector2 coords = new Vector2(square.transform.position.x, square.transform.position.z);
                piece = logicManager.boardMap[(int)coords.x, (int)coords.y];
            }
        }

        if (piece != null && ((piece.IsWhite && logicManager.isWhiteTurn) || (!piece.IsWhite && !logicManager.isWhiteTurn)))
        {
            if (selectedPiece == piece)
            {
                ResetSelection();
                return;
            }

            ResetSelection();
            selectedPiece = piece;
            currentState = InputState.PieceSelected;
            HighlightSelectedSquare();
            ShowActionPanel(piece);
        }
        else
        {
            ResetSelection();
        }
    }

    // ⬇️⬇️⬇️ 这是被大幅修改的方法 ⬇️⬇️⬇️
    private void ShowActionPanel(Piece piece)
    {
        if (actionPanel == null || actionButtonPrefab == null) return;

        // 1. 清空所有旧的按钮
        foreach (Transform child in actionPanel.transform)
        {
            Destroy(child.gameObject);
        }

        actionPanel.SetActive(true);

        // 2. 创建移动按钮
        GameObject moveButtonObj = Instantiate(actionButtonPrefab, actionPanel.transform);
        moveButtonObj.GetComponentInChildren<TextMeshProUGUI>().text = "Move";
        moveButtonObj.GetComponent<Button>().onClick.AddListener(() => {
            OnMoveButton();
        });

        // 3. 循环创建技能按钮
        for (int i = 0; i < piece.Spells.Count; i++)
        {
            int spellIndex = i; // 关键！防止C#闭包问题
            Spell spell = piece.Spells[spellIndex];

            GameObject spellButtonObj = Instantiate(actionButtonPrefab, actionPanel.transform);
            spellButtonObj.GetComponentInChildren<TextMeshProUGUI>().text = spell.SpellName;

            Button spellButton = spellButtonObj.GetComponent<Button>();
            spellButton.onClick.AddListener(() => {
                OnSpellButton(spellIndex);
            });

            // 如果技能无法释放（例如法力不够），让按钮变灰不可用
            if (!spell.CanCast())
            {
                spellButton.interactable = false;
            }
        }
    }

    private void ResetSelection()
    {
        UnhighlightSelectedSquare();
        UnhighlightLegalMoves();
        selectedPiece = null;
        selectedSpell = null;
        currentState = InputState.None;

        if (actionPanel != null)
        {
            actionPanel.SetActive(false);
        }
    }

    private void TryMoveToTarget(RaycastHit hit)
    {
        Vector2 targetCoords = GetCoordinatesFromHit(hit);
        if (IsTargetInHighlightedList(targetCoords))
        {
            bool isCapture = logicManager.boardMap[(int)targetCoords.x, (int)targetCoords.y] != null;
            selectedPiece.Move(targetCoords);
            if (!isCapture && logicManager.moveSound != null) { logicManager.moveSound.Play(); }
            else if (isCapture && logicManager.captureSound != null) { logicManager.captureSound.Play(); }
            logicManager.lastMovedPiece = selectedPiece;
            if (!logicManager.isPromotionActive)
            {
                logicManager.EndTurn();
            }
            ResetSelection();
        }
        else
        {
            currentState = InputState.PieceSelected;
            UnhighlightLegalMoves();
            ShowActionPanel(selectedPiece);
        }
    }

    private void TryCastAtTarget(RaycastHit hit)
    {
        Vector2 targetCoords = GetCoordinatesFromHit(hit);
        if (IsTargetInHighlightedList(targetCoords))
        {
            selectedSpell.Cast(targetCoords);
            if (!logicManager.isPromotionActive)
            {
                logicManager.EndTurn();
            }
            ResetSelection();
        }
        else
        {
            currentState = InputState.PieceSelected;
            UnhighlightLegalMoves();
            ShowActionPanel(selectedPiece);
        }
    }

    private Vector2 GetCoordinatesFromHit(RaycastHit hit)
    {
        Square targetSquare = hit.transform.GetComponent<Square>();
        if (targetSquare != null)
        {
            return new Vector2(targetSquare.transform.position.x, targetSquare.transform.position.z);
        }
        Piece targetPiece = hit.transform.GetComponent<Piece>();
        if (targetPiece != null)
        {
            return targetPiece.GetCoordinates();
        }
        return new Vector2(-1, -1);
    }

    private bool IsTargetInHighlightedList(Vector2 targetCoords)
    {
        if (targetCoords.x == -1) return false;
        foreach (Square sq in highlightedSquares)
        {
            if (Mathf.Approximately(sq.transform.position.x, targetCoords.x) && Mathf.Approximately(sq.transform.position.z, targetCoords.y))
            {
                return true;
            }
        }
        return false;
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
            if (square == null) continue;
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
}
