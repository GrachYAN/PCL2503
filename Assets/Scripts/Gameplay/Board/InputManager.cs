/*
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Unity.Netcode;
using System.Linq; // 需要用到 Linq

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

    // --- NEW UI References (新的UI引用) ---
    [Header("Unit Frame (Top Left)")]
    public GameObject unitFramePanel; // 整个左上角面板
    public Image portraitImage;       // 头像(可选)
    public Slider healthSlider;
    public Slider manaSlider;
    public TextMeshProUGUI healthValueText;
    public TextMeshProUGUI manaValueText;

    [Header("Action Bar (Bottom Center)")]
    public GameObject actionBarPanel; // 整个下方技能栏面板
    // 我们有3个固定的槽位：1个移动，2个技能
    public Button moveButton;
    public Image moveButtonIcon; // 移动按钮的图标Image组件

    public Button spellButton1;
    public Image spellButton1Icon; // 技能1的图标Image组件

    public Button spellButton2;
    public Image spellButton2Icon; // 技能2的图标Image组件

    [Header("Assets Library")]
    // 这里是为了解决纯代码Spell类无法直接引用Sprite的问题
    // 在Inspector中配置这个列表
    public Sprite moveIcon; // 移动图标
    public Sprite defaultSpellIcon; // 默认图标（防报错）
    public Sprite defaultPortrait; // <--- 新增：默认头像（防白块）

    public List<SpellIconData> spellIcons;
    public List<PiecePortraitData> piecePortraits;

    [System.Serializable]
    public struct SpellIconData
    {
        public string SpellName; // 必须与代码中的 SpellName 完全一致 (例如 "Mind Control")
        public Sprite Icon;
    }

    [System.Serializable]
    public struct PiecePortraitData
    {
        public string PieceType; 
        public Sprite Portrait;
    }

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

        if (!isOfflineMode && NetworkManager.Singleton != null)
        {
            isHost = NetworkManager.Singleton.IsHost;
        }

        // 2. Setup Input Action
        clickAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
        clickAction.performed += ctx => OnMouseClick();
        clickAction.Enable();

        // 3. Initial UI State (Hide everything)
        HideUI();
    }

    void OnDestroy()
    {
        clickAction.Disable();
    }

    void Update()
    {
        // 不再需要 UpdateActionPanelPosition，因为UI现在是固定在屏幕上的
        // 只需要实时更新血量蓝量
        if (selectedPiece != null && unitFramePanel.activeSelf)
        {
            UpdateUnitFrameValues(selectedPiece);
        }
    }

    /// <summary>
    /// 新增：根据棋子类型查找头像
    /// </summary>
    private Sprite GetPortraitForPiece(string pieceType)
    {
        var data = piecePortraits.FirstOrDefault(x => x.PieceType == pieceType);
        if (data.Portrait != null) return data.Portrait;
        return defaultPortrait;
    }


    /// <summary>
    /// 根据名字查找图标
    /// </summary>
    private Sprite GetIconForSpell(string spellName)
    {
        var data = spellIcons.FirstOrDefault(x => x.SpellName == spellName);
        if (data.Icon != null) return data.Icon;
        return defaultSpellIcon;
    }

    private void OnMouseClick()
    {
        // 1. UI Click Check
        if (EventSystem.current.IsPointerOverGameObject()) return;

        // 2. Promotion Check
        if (logicManager.isPromotionActive) return;

        // 3. Raycast
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit))
        {
            ResetSelection(currentState == InputState.CastingSpell);
            return;
        }

        // 4. State Machine
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
            // Turn & Ownership Check
            if (!((piece.IsWhite && logicManager.IsWhiteTurn) || (!piece.IsWhite && !logicManager.IsWhiteTurn))) return;
            if (!isOfflineMode)
            {
                bool isMyPiece = (isHost && piece.IsWhite) || (!isHost && !piece.IsWhite);
                if (!isMyPiece) return;
            }

            if (selectedPiece == piece)
            {
                ResetSelection(currentState == InputState.CastingSpell);
                return;
            }

            ResetSelection();
            selectedPiece = piece;
            currentState = InputState.PieceSelected;
            HighlightSelectedSquare();

            // --- NEW: Update Fixed UI ---
            UpdateFixedUI(piece);
        }
        else
        {
            ResetSelection(currentState == InputState.CastingSpell);
        }
    }

    // 新的 UI 更新逻辑
    private void UpdateFixedUI(Piece piece)
    {
        // 1. 显示面板
        unitFramePanel.SetActive(true);
        actionBarPanel.SetActive(true);

        if (portraitImage != null)
        {
            portraitImage.sprite = GetPortraitForPiece(piece.PieceType);
            // 如果你希望没有头像时隐藏 Image，可以用下面这句：
            // portraitImage.gameObject.SetActive(portraitImage.sprite != null);
        }

        // 2. 初始化数值
        if (healthSlider != null) healthSlider.maxValue = piece.MaxHP;
        if (manaSlider != null) manaSlider.maxValue = piece.MaxMana;
        UpdateUnitFrameValues(piece);

        // 3. 配置移动按钮 (左一槽位)
        moveButton.onClick.RemoveAllListeners();
        moveButton.onClick.AddListener(OnMoveButton);
        moveButton.interactable = true;
        if (moveButtonIcon != null) moveButtonIcon.sprite = moveIcon; // 设置移动图标

        // 4. 配置技能按钮 (中间和右边槽位)
        // 先重置/隐藏
        spellButton1.gameObject.SetActive(false);
        spellButton2.gameObject.SetActive(false);

        // 你的设计有3个圈，左边是移动，剩下两个给技能。
        // 大部分棋子有2个技能。

        // 处理第一个技能
        if (piece.Spells.Count > 0)
        {
            ConfigureSpellButton(spellButton1, spellButton1Icon, piece.Spells[0], 0);
        }

        // 处理第二个技能
        if (piece.Spells.Count > 1)
        {
            ConfigureSpellButton(spellButton2, spellButton2Icon, piece.Spells[1], 1);
        }
    }

    private void ConfigureSpellButton(Button btn, Image iconImg, Spell spell, int index)
    {
        btn.gameObject.SetActive(true);
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnSpellButton(index));

        // 设置图标
        if (iconImg != null)
        {
            iconImg.sprite = GetIconForSpell(spell.SpellName);
        }

        // 设置交互性 (蓝量/CD检查)
        btn.interactable = spell.CanCast();

        // 如果你需要显示CD遮罩，可以在这里扩展逻辑
    }

    private void UpdateUnitFrameValues(Piece piece)
    {
        if (healthSlider != null) healthSlider.value = piece.CurrentHP;
        if (manaSlider != null) manaSlider.value = piece.CurrentMana;


        // --- 文字更新 (这就是你要的功能) ---
        if (healthValueText != null)
            healthValueText.text = $"{piece.CurrentHP}/{piece.MaxHP}";

        if (manaValueText != null)
            manaValueText.text = $"{piece.CurrentMana}/{piece.MaxMana}";
    }

    private void HideUI()
    {
        if (unitFramePanel != null) unitFramePanel.SetActive(false);
        if (actionBarPanel != null) actionBarPanel.SetActive(false);
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

        HideUI();
    }

    // ... (OnMoveButton, OnSpellButton, TryMoveToTarget, TryCastAtTarget 等逻辑保持不变，直接复用你原来的代码即可) ...

    public void OnMoveButton()
    {
        if (selectedPiece == null || currentState != InputState.PieceSelected) return;
        currentState = InputState.Moving;
        // actionPanel.SetActive(false); // 不需要隐藏UI了
        HighlightLegalMoves(selectedPiece.GetLegalMoves());
    }

    public void OnSpellButton(int spellIndex)
    {
        if (selectedPiece == null || spellIndex >= selectedPiece.Spells.Count || currentState != InputState.PieceSelected) return;
        Spell spell = selectedPiece.Spells[spellIndex];
        if (spell.CanCast())
        {
            currentState = InputState.CastingSpell;
            selectedSpell = spell;
            // actionPanel.SetActive(false); // 不需要隐藏UI
            selectedSpell.BeginTargeting();
            List<Vector2> targets = selectedSpell.GetCurrentValidSquares();
            if (targets == null || targets.Count == 0)
            {
                selectedSpell.CancelTargeting();
                selectedSpell = null;
                currentState = InputState.PieceSelected;
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

            if (isOfflineMode)
            {
                selectedPiece.Move(targetCoords);
                logicManager.lastMovedPiece = selectedPiece;
                logicManager.lastMovedPieceStartPosition = startPosition;
                logicManager.lastMovedPieceEndPosition = selectedPiece.GetCoordinates();

                if (logicManager.moveSound != null) logicManager.moveSound.Play();

                if (!logicManager.isPromotionActive)
                {
                    logicManager.UpdateCheckMap();
                    logicManager.EndTurn();
                }
            }
            else
            {
                logicManager.RequestMoveServerRpc((int)startPosition.x, (int)startPosition.y, (int)targetCoords.x, (int)targetCoords.y);
            }

            ResetSelection();
        }
        else
        {
            // 取消移动，回到选中状态
            currentState = InputState.PieceSelected;
            UnhighlightLegalMoves();
            // UI 保持显示
        }
    }

    private void TryCastAtTarget(RaycastHit hit)
    {
        Vector2 targetCoords = GetCoordinatesFromHit(hit);
        if (IsTargetInHighlightedList(targetCoords))
        {
            if (selectedSpell == null) return;

            if (!selectedSpell.TryHandleTargetSelection(targetCoords, out bool castComplete)) return;

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
                if (!logicManager.isPromotionActive) logicManager.EndTurn();
            }
            else
            {
                Vector2 pieceCoords = selectedPiece.GetCoordinates();
                int spellIndex = selectedPiece.Spells.IndexOf(selectedSpell);
                if (spellIndex != -1)
                {
                    logicManager.RequestCastSpellServerRpc((int)pieceCoords.x, (int)pieceCoords.y, spellIndex, castData);
                }
            }
            ResetSelection();
        }
        else
        {
            currentState = InputState.PieceSelected;
            if (selectedSpell != null)
            {
                selectedSpell.CancelTargeting();
                selectedSpell = null;
            }
            UnhighlightLegalMoves();
            // UI 保持显示
        }
    }

    // ... GetCoordinatesFromHit, IsTargetInHighlightedList, Highlight Helper Methods 保持原样 ...
    private Vector2 GetCoordinatesFromHit(RaycastHit hit)
    {
        Square targetSquare = hit.transform.GetComponent<Square>();
        if (targetSquare != null) return new Vector2(targetSquare.transform.position.x, targetSquare.transform.position.z);
        Piece targetPiece = hit.transform.GetComponent<Piece>();
        if (targetPiece != null) return targetPiece.GetCoordinates();
        return new Vector2(-1, -1);
    }

    private bool IsTargetInHighlightedList(Vector2 targetCoords)
    {
        if (targetCoords.x == -1) return false;
        foreach (Square sq in highlightedSquares)
        {
            if (Mathf.Approximately(sq.transform.position.x, targetCoords.x) && Mathf.Approximately(sq.transform.position.z, targetCoords.y)) return true;
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
        if (selectedPiece == null) return;
        Vector2 pieceCoordinates = selectedPiece.GetCoordinates();
        currentlyHighlightedSquare = logicManager.GetSquareAtPosition(pieceCoordinates);
        if (currentlyHighlightedSquare != null) currentlyHighlightedSquare.Highlight(new Color(0f, 0.6f, 0.6f));
    }

    void HighlightLegalMoves(List<Vector2> legalMoves)
    {
        UnhighlightLegalMoves();
        foreach (Vector2 move in legalMoves)
        {
            Square square = logicManager.GetSquareAtPosition(move);
            if (square == null) continue;
            Piece pieceOnSquare = logicManager.boardMap[(int)move.x, (int)move.y];
            if (pieceOnSquare != null) square.Highlight(Color.red);
            else square.Highlight(Color.cyan);
            highlightedSquares.Add(square);
        }
    }

    void UnhighlightLegalMoves()
    {
        foreach (Square square in highlightedSquares) if (square != null) square.Unhighlight();
        highlightedSquares.Clear();
    }

    #endregion
}
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Unity.Netcode;
using System.Linq;

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
    private Camera mainCamera;
    private InputAction clickAction;

    // --- Highlighting ---
    private List<Square> highlightedSquares = new List<Square>();
    private Square currentlyHighlightedSquare;

    // ========================= UI REFERENCES =========================

    [Header("Unit Frame (Top Left)")]
    public GameObject unitFramePanel;
    public Image portraitImage;
    //public Slider healthSlider;
    //public Slider manaSlider;
    public Image healthFillImage;
    public Image manaFillImage;   
    public TextMeshProUGUI healthValueText;
    public TextMeshProUGUI manaValueText;

    [Header("Status Effects (Buffs/Debuffs)")]
    public Transform buffBarContainer; // 挂载点：UnitFrame 下的 BuffContainer
    public GameObject buffIconPrefab;  // 资源：做好的 Icon Prefab

    // --- 【核心修改】状态效果配置库 ---
    [System.Serializable]
    public struct StatusEffectData
    {
        public string effectID;      // 唯一标识符，例如 "Stun", "Daze", "Fire"
        public string displayName;   // 显示名称，例如 "眩晕 (Stun)"
        public Sprite icon;          // 图标
        public Color iconTint;       // 图标颜色（如果没有专用图标，可以用颜色区分）
        [TextArea] public string description; // 鼠标悬停时的描述
    }

    [Header("Status Library Configuration")]
    public List<StatusEffectData> statusLibrary; // 在 Inspector 中配置所有可能的状态

    

    [Header("Action Bar (Bottom Center)")]
    public GameObject actionBarPanel;
    public Button moveButton;
    public Image moveButtonIcon;
    public Button spellButton1;
    public Image spellButton1Icon;
    public Button spellButton2;
    public Image spellButton2Icon;

    [Header("Assets Library")]
    public Sprite dwarfMoveIcon; 
    public Sprite elfMoveIcon;   
    public Sprite defaultSpellIcon;
    public Sprite defaultPortrait;

    public List<SpellIconData> spellIcons;
    public List<PiecePortraitData> piecePortraits;

    [System.Serializable]
    public struct SpellIconData
    {
        public string SpellName;
        public Faction faction;
        public Sprite Icon;
    }

    [System.Serializable]
    public struct PiecePortraitData
    {
        public string PieceType;
        public Faction faction;
        public Sprite Portrait;
    }

    // --- Online/Offline Variables ---
    private bool isHost;
    private bool isOfflineMode = false;

    

    // ========================= UNITY LIFECYCLE =========================

    void Start()
    {
        logicManager = Object.FindFirstObjectByType<LogicManager>();
        mainCamera = Camera.main;

        if (GameModeManager.Instance != null && GameModeManager.Instance.CurrentMode == GameModeManager.GameMode.Offline)
        {
            isOfflineMode = true;
        }

        if (!isOfflineMode && NetworkManager.Singleton != null)
        {
            isHost = NetworkManager.Singleton.IsHost;
        }

        clickAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
        clickAction.performed += ctx => OnMouseClick();
        clickAction.Enable();

        HideUI();
    }

    void OnDestroy()
    {
        clickAction.Disable();
    }

    void Update()
    {
        // 只有当面板显示且有选中棋子时，才实时更新血量/蓝量数值
        if (selectedPiece != null && unitFramePanel.activeSelf)
        {
            // ❌ 绝对不要在这里调用包含 Instantiate/Destroy 的方法
            UpdateSlidersAndTextOnly(selectedPiece);
        }
    }

    // ========================= INPUT HANDLING =========================

    private void OnMouseClick()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        if (logicManager.isPromotionActive) return;

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit))
        {
            ResetSelection(currentState == InputState.CastingSpell);
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

    // ========================= SELECTION LOGIC =========================

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
            if (!((piece.IsWhite && logicManager.IsWhiteTurn) || (!piece.IsWhite && !logicManager.IsWhiteTurn))) return;

            if (!isOfflineMode)
            {
                bool isMyPiece = (isHost && piece.IsWhite) || (!isHost && !piece.IsWhite);
                if (!isMyPiece) return;
            }

            if (selectedPiece == piece)
            {
                ResetSelection(currentState == InputState.CastingSpell);
                return;
            }

            ResetSelection();
            selectedPiece = piece;
            currentState = InputState.PieceSelected;

            HighlightSelectedSquare();
            ShowUI(selectedPiece);
        }
        else
        {
            ResetSelection(currentState == InputState.CastingSpell);
        }
    }

    // ========================= MOVEMENT & SPELL LOGIC =========================

    public void OnMoveButton()
    {
        if (selectedPiece == null) return;
        currentState = InputState.Moving;
        HighlightLegalMoves(selectedPiece.GetLegalMoves());
    }

    public void OnSpellButton(int index)
    {
        if (selectedPiece == null || index >= selectedPiece.Spells.Count) return;

        Spell spell = selectedPiece.Spells[index];
        if (spell.CanCast())
        {
            currentState = InputState.CastingSpell;
            selectedSpell = spell;
            selectedSpell.BeginTargeting();
            HighlightLegalMoves(selectedSpell.GetCurrentValidSquares());
        }

    }



    private void TryMoveToTarget(RaycastHit hit)
    {
        Vector2 targetCoords = GetCoordinatesFromHit(hit);

        // 检查点击的位置是否在“高亮的可移动列表”里
        if (IsTargetInHighlightedList(targetCoords))
        {
            Vector2 startPosition = selectedPiece.GetCoordinates();

            // --- 核心移动逻辑开始 ---
            if (isOfflineMode)
            {
                // 1. 执行移动
                selectedPiece.Move(targetCoords);

                // 2. 记录移动数据（给 LogicManager 用）
                logicManager.lastMovedPiece = selectedPiece;
                logicManager.lastMovedPieceStartPosition = startPosition;
                logicManager.lastMovedPieceEndPosition = selectedPiece.GetCoordinates();

                // 3. 播放音效
                if (logicManager.moveSound != null) logicManager.moveSound.Play();

                // 4. 检查是否升变 / 结束回合
                if (!logicManager.isPromotionActive)
                {
                    logicManager.UpdateCheckMap();
                    logicManager.EndTurn();
                }
            }
            else
            {
                // 联机模式逻辑
                logicManager.RequestMoveServerRpc((int)startPosition.x, (int)startPosition.y, (int)targetCoords.x, (int)targetCoords.y);
            }
            // --- 核心移动逻辑结束 ---

            ResetSelection();
        }
        else
        {
            // 点了非法格子，取消移动状态，回到选中状态
            currentState = InputState.PieceSelected;
            UnhighlightLegalMoves();
            ConfigureActionButtons(selectedPiece);
        }
    }

    private void TryCastAtTarget(RaycastHit hit)
    {
        Vector2 targetCoords = GetCoordinatesFromHit(hit);
        
        // 检查点击的位置是否有效
        if (IsTargetInHighlightedList(targetCoords))
        {
            if (selectedSpell == null) return;

            // 1. 处理多阶段目标选择 (虽然HawkCry不需要，但为了兼容其他技能最好加上)
            if (!selectedSpell.TryHandleTargetSelection(targetCoords, out bool castComplete)) return;

            // 如果还需要选择更多目标，刷新高亮并返回
            if (!castComplete)
            {
                HighlightLegalMoves(selectedSpell.GetCurrentValidSquares());
                return;
            }

            // 2. 获取施法数据
            SpellCastData castData = selectedSpell.GetCastData(targetCoords);

            // 3. 根据模式执行施法
            if (isOfflineMode)
            {
                // 单机模式：直接执行
                selectedSpell.ApplyCastData(castData);

                // 注意：SpellCastData存的是int，Cast需要Vector2
                Vector2 primaryTarget = new Vector2(castData.PrimaryX, castData.PrimaryY);

                // --- 真正执行施法的地方 ---
                selectedSpell.Cast(primaryTarget);

                // 施法后结束回合 (除非是升变状态)
                if (!logicManager.isPromotionActive)
                {
                    logicManager.EndTurn();
                }
            }
            else
            {
                // 联机模式：发送 RPC 请求给服务器
                Vector2 pieceCoords = selectedPiece.GetCoordinates();
                int spellIndex = selectedPiece.Spells.IndexOf(selectedSpell);

                if (spellIndex != -1)
                {
                    logicManager.RequestCastSpellServerRpc(
                        (int)pieceCoords.x,
                        (int)pieceCoords.y,
                        spellIndex,
                        castData
                    );
                }
            }

            // 4. 重置选择状态
            ResetSelection();
        }
        else
        {
            // 点击了无效区域：取消施法，回到选中棋子状态
            if (selectedSpell != null)
            {
                selectedSpell.CancelTargeting();
                selectedSpell = null;
            }
            currentState = InputState.PieceSelected;
            UnhighlightLegalMoves();

            // 重新显示该棋子的UI按钮状态
            if (selectedPiece != null)
            {
                ConfigureActionButtons(selectedPiece);
            }
        }
    }
    // ========================= UI METHODS =========================

    private void HideUI()
    {
        unitFramePanel.SetActive(false);
        actionBarPanel.SetActive(false);
    }

    private void ShowUI(Piece piece)
    {
        unitFramePanel.SetActive(true);
        actionBarPanel.SetActive(true);

        // 1. 设置头像 (只设置一次)
        if (portraitImage != null) portraitImage.sprite = GetPortraitForPiece(piece);

        // 2. 初始化数值
        UpdateSlidersAndTextOnly(piece);

        // 3. 生成 Buff 图标 (只生成一次！！！)
        // 注意：这个方法里有 Destroy 和 Instantiate，绝对不能放在 Update 里
        UpdateStatusIcons(piece);

        // 4. 配置按钮
        ConfigureActionButtons(piece);
    }

    private void UpdateSlidersAndTextOnly(Piece piece)
    {
        /*
        if (healthSlider != null)
        {
            healthSlider.maxValue = piece.MaxHP;
            healthSlider.value = piece.CurrentHP;
        }
        if (manaSlider != null)
        {
            manaSlider.maxValue = piece.MaxMana;
            manaSlider.value = piece.CurrentMana;
        }
        */

        if (healthFillImage != null)
        {
            // 计算血量百分比 (0.0 ~ 1.0)
            healthFillImage.fillAmount = (float)piece.CurrentHP / piece.MaxHP;
        }

        if (manaFillImage != null)
        {
            // 计算蓝量百分比
            manaFillImage.fillAmount = (float)piece.CurrentMana / piece.MaxMana;
        }

        if (healthValueText != null) healthValueText.text = $"{piece.CurrentHP}/{piece.MaxHP}";
        if (manaValueText != null) manaValueText.text = $"{piece.CurrentMana}/{piece.MaxMana}";
    }

    // --- 核心逻辑：状态图标更新 ---
    private void UpdateStatusIcons(Piece piece)
    {
        if (buffBarContainer == null || buffIconPrefab == null) return;

        // 1. 清空旧图标
        foreach (Transform child in buffBarContainer)
        {
            Destroy(child.gameObject);
        }

        // ================== 状态检测列表 ==================

        // 1. 基础控制 (Stun / Root)
        if (piece.IsStunned) TryAddStatusIcon("Stun");
        if (piece.IsRooted) TryAddStatusIcon("Root");

        // 2. 迷茫 (Daze)
        if (piece.IsDazed)
        {
            // 如果你想显示层数，可以在这里扩展，目前先只显示图标
            TryAddStatusIcon("Daze");
        }

        // 3. 护盾 (Shield)
        if (piece.CurrentShieldValue > 0)
        {
            // 这里的 "Shield" 对应你在 Inspector 列表里填的 ID
            TryAddStatusIcon("Shield");
        }

        // 4. 持续伤害 (DoT)
        if (piece.IsBurning) TryAddStatusIcon("Fire");
        // 如果你有 Arcane 的图标，也可以加:
        // if (piece.IsArcaneCorroded) TryAddStatusIcon("Arcane");

        // 5. 特殊 Buff
        if (piece.IsProtectedBySunwell) TryAddStatusIcon("SunwellWard"); // 记得在 Inspector 里添加 ID 为 "SunwellWard" 的配置
        if (piece.IsHeartOfMountainActive) TryAddStatusIcon("HeartOfMountain"); // 记得添加 ID 为 "HeartOfMountain" 的配置
    }


    // 辅助方法：根据ID从库里查找并显示图标
    private void TryAddStatusIcon(string effectID)
    {
        // 从 Inspector 配置的列表中查找数据
        var data = statusLibrary.FirstOrDefault(x => x.effectID == effectID);

        // 如果没找到配置，就不显示（或者显示一个默认的）
        if (string.IsNullOrEmpty(data.effectID)) return;

        GameObject iconObj = Instantiate(buffIconPrefab, buffBarContainer);

        Image img = iconObj.GetComponent<Image>();
        if (img != null)
        {
            if (data.icon != null) img.sprite = data.icon;
            img.color = data.iconTint;
        }

        TooltipTrigger trigger = iconObj.GetComponent<TooltipTrigger>();
        if (trigger != null)
        {
            trigger.SetContent($"<b>{data.displayName}</b>\n{data.description}");
        }
    }

    private void ConfigureActionButtons(Piece piece)
    {
        moveButton.interactable = true;
        moveButton.onClick.RemoveAllListeners();
        moveButton.onClick.AddListener(OnMoveButton);
        if (moveButtonIcon != null)
        {
 
            moveButtonIcon.sprite = piece.IsWhite ? dwarfMoveIcon : elfMoveIcon;
        }
        // --- 修改结束 ---

        TooltipTrigger moveTrigger = moveButton.GetComponent<TooltipTrigger>();
        if (moveTrigger != null) moveTrigger.SetContent("<b>Make a movement</b>\n<size=80%>");

        ConfigureSingleSpellButton(spellButton1, spellButton1Icon, piece, 0);
        ConfigureSingleSpellButton(spellButton2, spellButton2Icon, piece, 1);
    }

    private void ConfigureSingleSpellButton(Button btn, Image iconImg, Piece piece, int index)
    {
        if (index >= piece.Spells.Count)
        {
            btn.gameObject.SetActive(false);
            return;
        }

        Spell spell = piece.Spells[index];

        btn.gameObject.SetActive(true);
        btn.interactable = spell.CanCast();

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnSpellButton(index));

        if (iconImg != null) iconImg.sprite = GetIconForSpell(spell.SpellName);

        TooltipTrigger trigger = btn.GetComponent<TooltipTrigger>();
      
        if (trigger != null)
        {
            string desc = $"<b>{spell.SpellName}</b> <size=80%><color=#4287f5>({spell.ManaCost} Mana)</color></size>\nCD: {spell.Cooldown} Turn(s)\n{spell.Description}";
            trigger.SetContent(desc);
        }
        
    }

    // ========================= HELPER METHODS =========================

    private void ResetSelection(bool cancelSpell = false)
    {
        UnhighlightSelectedSquare();
        UnhighlightLegalMoves();

        if (cancelSpell && selectedSpell != null) selectedSpell.CancelTargeting();

        selectedPiece = null;
        selectedSpell = null;
        currentState = InputState.None;

        HideUI();
    }

    private Sprite GetPortraitForPiece(Piece piece)
    {
        // 1. 确定这个棋子是哪个阵营
        // 假设：白色是矮人(Dwarf)，黑色是精灵(Elf)
        Faction targetFaction = piece.IsWhite ? Faction.Dwarf : Faction.Elf;

        // 2. 在列表里查找匹配项
        var data = piecePortraits.FirstOrDefault(x =>
            x.PieceType == piece.PieceType && // 注意：这里必须是大写 P (PieceType)
            x.faction == targetFaction
        );

        // 3. 返回结果
        if (data.Portrait != null) return data.Portrait;
        return defaultPortrait;
    }

    private Sprite GetIconForSpell(string spellName)
    {
        var data = spellIcons.FirstOrDefault(x => x.SpellName == spellName);
        return (data.Icon != null) ? data.Icon : defaultSpellIcon;
    }

    private Vector2 GetCoordinatesFromHit(RaycastHit hit)
    {
        Square targetSquare = hit.transform.GetComponent<Square>();
        if (targetSquare != null) return new Vector2(targetSquare.transform.position.x, targetSquare.transform.position.z);

        Piece targetPiece = hit.transform.GetComponent<Piece>();
        if (targetPiece != null) return targetPiece.GetCoordinates();

        return new Vector2(-1, -1);
    }

    private void HighlightSelectedSquare()
    {
        if (selectedPiece == null) return;
        Vector2 coords = selectedPiece.GetCoordinates();
        currentlyHighlightedSquare = logicManager.GetSquareAtPosition(coords);
        if (currentlyHighlightedSquare != null) currentlyHighlightedSquare.Highlight(new Color(0f, 0.6f, 0.6f));
    }

    private void UnhighlightSelectedSquare()
    {
        if (currentlyHighlightedSquare != null)
        {
            currentlyHighlightedSquare.Unhighlight();
            currentlyHighlightedSquare = null;
        }
    }
    
    private void HighlightLegalMoves(List<Vector2> moves)
    {
        UnhighlightLegalMoves();
        if (moves == null) return;

        foreach (Vector2 move in moves)
        {
            Square sq = logicManager.GetSquareAtPosition(move);
            if (sq != null)
            {
                Piece p = logicManager.boardMap[(int)move.x, (int)move.y];
                sq.Highlight(p != null ? Color.red : Color.cyan);
                highlightedSquares.Add(sq);
            }
        }
    }

    private void UnhighlightLegalMoves()
    {
        foreach (var sq in highlightedSquares) sq.Unhighlight();
        highlightedSquares.Clear();
    }

    private bool IsTargetInHighlightedList(Vector2 targetCoords)
    {
        if (targetCoords.x == -1) return false;
        foreach (Square sq in highlightedSquares)
        {
            if (Mathf.Approximately(sq.transform.position.x, targetCoords.x) &&
                Mathf.Approximately(sq.transform.position.z, targetCoords.y))
                return true;
        }
        return false;
    }
}
