using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Unity.Netcode;

public class InputManager : MonoBehaviour
{
    // --- State & Logic Variables ---
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

    // --- UI References ---
    [Header("UI 引用")]
    public GameObject actionPanel;
    public GameObject actionButtonPrefab;
    public Transform buttonsContainer;
    public Slider healthSlider;
    public Slider manaSlider;
    public TextMeshProUGUI healthValueText;
    public TextMeshProUGUI manaValueText;
    private RectTransform actionPanelRectTransform;

    // --- Online/Offline Variables ---
    private bool isHost;
    private bool isOfflineMode = false;

    void Start()
    {
        logicManager = Object.FindFirstObjectByType<LogicManager>();
        mainCamera = Camera.main;

        // 1. Determine Game Mode
        if (GameModeManager.Instance != null && GameModeManager.Instance.CurrentMode == GameModeManager.GameMode.Offline)
        {
            isOfflineMode = true;
        }

        if (!isOfflineMode)
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("ERROR: In Online Mode but NetworkManager is missing!");
            }
            else
            {
                isHost = NetworkManager.Singleton.IsHost;
            }
        }
        
        // --- ADD THIS DEBUG LINE ---
        Debug.Log("InputManager Initialized. Mode = " + (isOfflineMode ? "OFFLINE" : "ONLINE"));

        // 2. Setup Input Action
        clickAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
        clickAction.performed += ctx => OnMouseClick();
        clickAction.Enable();

        // 3. Setup UI
        if (actionPanel != null)
        {
            actionPanel.SetActive(false);
            actionPanelRectTransform = actionPanel.GetComponent<RectTransform>();
        }
        if (healthSlider != null) healthSlider.gameObject.SetActive(false);
        if (manaSlider != null) manaSlider.gameObject.SetActive(false);
        if (healthValueText != null) healthValueText.gameObject.SetActive(false);
        if (manaValueText != null) manaValueText.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        clickAction.Disable();
    }

    void Update()
    {
        // This UI update logic is now used in both modes
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

    /// <summary>
    /// This is the main UNIFIED click handler.
    /// It always uses the advanced state-machine logic.
    /// </summary>
    private void OnMouseClick()
    {
        // 1. Check if clicking on UI (Universal)
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Mouse.current.position.ReadValue();
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        if (results.Count > 0)
        {
            return; // Clicked on UI
        }

        // 2. Check for promotion (Universal)
        if (logicManager.isPromotionActive)
        {
            return;
        }

        // 3. ---!!!--- UNIFIED LOGIC PATH ---!!!---
        // We no longer fork here. We always use the state machine.
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit))
        {
            // Clicked on empty space, reset selection
            ResetSelection(currentState == InputState.CastingSpell);
            return;
        }

        // Handle click based on the current state
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

    #region ========== UNIFIED ACTION LOGIC ==========
    // These methods are now used for BOTH Online and Offline modes.

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

        if (piece != null)
        {
            // --- COMBINED TURN & OWNERSHIP CHECK ---
            
            // 1. Check if it's this color's turn (works for both modes)
            if (!((piece.IsWhite && logicManager.IsWhiteTurn) || (!piece.IsWhite && !logicManager.IsWhiteTurn)))
            {
                return; // Not this color's turn
            }

            // 2. If ONLINE, check if it's our piece
            if (!isOfflineMode)
            {
                bool isMyPiece = (isHost && piece.IsWhite) || (!isHost && !piece.IsWhite);
                if (!isMyPiece)
                {
                    return; // Not your piece, do nothing
                }
            }
            // --- END OF CHECK ---

            // If we passed checks, select the piece
            if (selectedPiece == piece)
            {
                ResetSelection(currentState == InputState.CastingSpell);
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
            // Clicked on an empty square
            ResetSelection(currentState == InputState.CastingSpell);
        }
    }

    private void ShowActionPanel(Piece piece)
    {
        if (actionPanel == null || actionButtonPrefab == null || buttonsContainer == null) return;

        foreach (Transform child in buttonsContainer)
        {
            Destroy(child.gameObject);
        }

        actionPanel.SetActive(true);

        if (healthSlider != null && manaSlider != null)
        {
            healthSlider.gameObject.SetActive(true);
            manaSlider.gameObject.SetActive(true);
            healthSlider.maxValue = piece.MaxHP;
            healthSlider.value = piece.CurrentHP;
            manaSlider.maxValue = piece.MaxMana;
            manaSlider.value = piece.CurrentMana;
        }

        if (healthValueText != null)
        {
            healthValueText.gameObject.SetActive(true);
            healthValueText.text = $"{piece.CurrentHP}/{piece.MaxHP}";
        }

        if (manaValueText != null)
        {
            manaValueText.gameObject.SetActive(true);
            manaValueText.text = $"{piece.CurrentMana}/{piece.MaxMana}";
        }

        GameObject moveButtonObj = Instantiate(actionButtonPrefab, buttonsContainer);
        moveButtonObj.GetComponentInChildren<TextMeshProUGUI>().text = "Move";
        moveButtonObj.GetComponent<Button>().onClick.AddListener(OnMoveButton);

        for (int i = 0; i < piece.Spells.Count; i++)
        {
            int spellIndex = i;
            Spell spell = piece.Spells[spellIndex];
            GameObject spellButtonObj = Instantiate(actionButtonPrefab, buttonsContainer);
            // Use legacy Text if you get errors here
            spellButtonObj.GetComponentInChildren<TextMeshProUGUI>().text = spell.SpellName; 
            Button spellButton = spellButtonObj.GetComponent<Button>();
            spellButton.onClick.AddListener(() => OnSpellButton(spellIndex));
            if (!spell.CanCast())
            {
                spellButton.interactable = false;
            }
        }
    }

    private void ResetSelection(bool cancelSpell = false)
    {
        UnhighlightSelectedSquare();
        UnhighlightLegalMoves();

        if (cancelSpell && currentState == InputState.CastingSpell && selectedSpell != null)
        {
            selectedSpell.CancelTargeting();
        }

        selectedPiece = null;
        selectedSpell = null;
        currentState = InputState.None;

        if (actionPanel != null) actionPanel.SetActive(false);
        if (healthSlider != null) healthSlider.gameObject.SetActive(false);
        if (manaSlider != null) manaSlider.gameObject.SetActive(false);
        if (healthValueText != null) healthValueText.gameObject.SetActive(false);
        if (manaValueText != null) manaValueText.gameObject.SetActive(false);
    }

    public void OnMoveButton()
    {
        if (selectedPiece == null || currentState != InputState.PieceSelected) return;
        currentState = InputState.Moving;
        actionPanel.SetActive(false);
        HighlightLegalMoves(selectedPiece.GetLegalMoves());
    }

    public void OnSpellButton(int spellIndex)
    {
        if (selectedPiece == null || spellIndex >= selectedPiece.Spells.Count || currentState != InputState.PieceSelected) return;
        Spell spell = selectedPiece.Spells[spellIndex];
        if (spell.CanCast()) // Client-side check for immediate feedback
        {
            currentState = InputState.CastingSpell;
            selectedSpell = spell;
            actionPanel.SetActive(false);
            selectedSpell.BeginTargeting();
            List<Vector2> targets = selectedSpell.GetCurrentValidSquares();
            if (targets == null || targets.Count == 0)
            {
                selectedSpell.CancelTargeting();
                selectedSpell = null;
                currentState = InputState.PieceSelected;
                ShowActionPanel(selectedPiece);
                return;
            }
            HighlightLegalMoves(targets);
        }
    }

    private void TryMoveToTarget(RaycastHit hit)
    {
        Vector2 targetCoords = GetCoordinatesFromHit(hit);
        if (IsTargetInHighlightedList(targetCoords))
        {
            Vector2 startPosition = selectedPiece.GetCoordinates();

            // --- !! CRITICAL ONLINE/OFFLINE BRANCH !! ---
            if (isOfflineMode)
            {
                // --- OFFLINE: Execute move locally ---
                selectedPiece.Move(targetCoords);

                logicManager.lastMovedPiece = selectedPiece;
                logicManager.lastMovedPieceStartPosition = startPosition;
                logicManager.lastMovedPieceEndPosition = selectedPiece.GetCoordinates();

                if (logicManager.moveSound != null)
                {
                    logicManager.moveSound.Play();
                }
                
                if (!logicManager.isPromotionActive)
                {
                    logicManager.UpdateCheckMap();
                    logicManager.EndTurn();
                }
            }
            else
            {
                // --- ONLINE: Send RPC request to server ---
                logicManager.RequestMoveServerRpc((int)startPosition.x, (int)startPosition.y, (int)targetCoords.x, (int)targetCoords.y);
            }
            // --- !! END OF BRANCH !! ---

            ResetSelection();
        }
        else
        {
            // Clicked outside valid moves, go back to piece selection
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
            // --- !! CRITICAL ONLINE/OFFLINE BRANCH !! ---
            if (selectedSpell == null)
            {
                return;
            }

            if (!selectedSpell.TryHandleTargetSelection(targetCoords, out bool castComplete))
            {
                return;
            }

            if (!castComplete)
            {
                HighlightLegalMoves(selectedSpell.GetCurrentValidSquares());
                return;
            }

            SpellCastData castData = selectedSpell.GetCastData(targetCoords);

            if (isOfflineMode)
            {
                selectedSpell.ApplyCastData(castData);
                Vector2 primary = new Vector2(castData.PrimaryX, castData.PrimaryY);
                selectedSpell.Cast(primary);
                if (!logicManager.isPromotionActive)
                {
                    logicManager.EndTurn();
                }
            }
            else
            {
                Vector2 pieceCoords = selectedPiece.GetCoordinates();
                int spellIndex = selectedPiece.Spells.IndexOf(selectedSpell);

                if (spellIndex == -1)
                {
                    Debug.LogError("Error: Could not find spell index!");
                    return;
                }

                logicManager.RequestCastSpellServerRpc(
                    (int)pieceCoords.x, (int)pieceCoords.y,
                    spellIndex,
                    castData
                );
            }
            // --- !! END OF BRANCH !! ---

            ResetSelection();
        }
        else
        {
            // Clicked outside valid moves, go back to piece selection
            currentState = InputState.PieceSelected;
            if (selectedSpell != null)
            {
                selectedSpell.CancelTargeting();
                selectedSpell = null;
            }
            UnhighlightLegalMoves();
            ShowActionPanel(selectedPiece);
        }
    }

    // Helper to get coordinates from a raycast hit
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

    // Helper to check if a target is in the highlighted list
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

    #endregion

    #region ========== SHARED HELPER METHODS ==========

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
        if (selectedPiece == null) return;

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
                square.Highlight(Color.red); // Capture
            }
            else
            {
                square.Highlight(Color.cyan); // Move
            }

            highlightedSquares.Add(square);
        }
    }

    void UnhighlightLegalMoves()
    {
        foreach (Square square in highlightedSquares)
        {
            if (square != null)
            {
                square.Unhighlight();
            }
        }
        highlightedSquares.Clear();
    }
    #endregion
}