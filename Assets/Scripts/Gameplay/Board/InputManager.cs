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
    private bool clickQueued;

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

    [Header("Cooldown Visuals")] // 新增CD遮罩方法捏
    public Image spellButton1CDOverlay; 
    public TextMeshProUGUI spellButton1CDText;
    public Image spellButton2CDOverlay;
    public TextMeshProUGUI spellButton2CDText;

    [Header("Assets Library")]
    public Sprite dwarfMoveIcon;
    public Sprite elfMoveIcon;
    public Sprite defaultMoveIcon;
    public Sprite defaultSpellIcon;
    public Sprite defaultPortrait;

    public List<SpellIconData> spellIcons;
    public List<PiecePortraitData> piecePortraits;
    public List<FactionMoveIconData> factionMoveIcons;

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

    [System.Serializable]
    public struct FactionMoveIconData
    {
        public Faction faction;
        public Sprite icon;
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
        clickAction.performed += ctx => clickQueued = true;
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

        if (clickQueued)
        {
            clickQueued = false;
            HandleMouseClick();
        }
    }

    private void UpdateSpellCooldownUI(Spell spell, Image cdOverlay, TextMeshProUGUI cdText)
    {
        if (spell == null || cdOverlay == null) return;

        // 1. 检查是否有冷却
        if (spell.CurrentCooldown > 0)
        {
            // 开启遮罩
            cdOverlay.gameObject.SetActive(true);

            // 计算填充比例 (0.0 ~ 1.0)
            // 如果 Cooldown 是 0 (防除以0错误)，则直接填满
            float fillAmount = (spell.Cooldown > 0)
                ? (float)spell.CurrentCooldown / spell.Cooldown
                : 1f;

            cdOverlay.fillAmount = fillAmount;

            // 更新文字 (如果有)
            if (cdText != null)
            {
                cdText.gameObject.SetActive(true);
                cdText.text = spell.CurrentCooldown.ToString();
            }
        }
        else
        {
            // 没有冷却，隐藏遮罩和文字
            cdOverlay.gameObject.SetActive(false);
            cdOverlay.fillAmount = 0f;

            if (cdText != null)
            {
                cdText.gameObject.SetActive(false);
            }
        }
    }   

    // ========================= INPUT HANDLING =========================

    private void HandleMouseClick()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;

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
        SpellCastFailReason failReason = spell.GetCastFailReason();

        if (failReason == SpellCastFailReason.None)
        {
            currentState = InputState.CastingSpell;
            selectedSpell = spell;
            selectedSpell.BeginTargeting();
            HighlightLegalMoves(selectedSpell.GetCurrentValidSquares());
        }
        else
        {
            // Play appropriate error sound based on failure reason
            PlaySpellErrorSound(failReason);
        }
    }

    /// <summary>
    /// Plays the appropriate error sound based on the spell cast failure reason.
    /// </summary>
    private void PlaySpellErrorSound(SpellCastFailReason reason)
    {
        if (GameSoundManager.Instance == null) return;

        switch (reason)
        {
            case SpellCastFailReason.NotEnoughMana:
                GameSoundManager.Instance.PlayNotEnoughManaSound();
                break;
            case SpellCastFailReason.OnCooldown:
                GameSoundManager.Instance.PlayCooldownNotReadySound();
                break;
            case SpellCastFailReason.InvalidTarget:
                GameSoundManager.Instance.PlayCannotTargetSound();
                break;
            // Stunned and NoCaster don't need sounds (UI should prevent these)
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

                // 4. 结束回合
                logicManager.EndTurn();
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

                // 施法后结束回合
                logicManager.EndTurn();

                // --- 【新增】施法成功后，立即刷新 UI ---
                // 这样 CD 遮罩会瞬间出现，蓝量条也会瞬间扣减
                if (selectedPiece != null)
                {
                    ShowUI(selectedPiece);
                    // 或者只调用 ConfigureActionButtons(selectedPiece); 也可以
                }

                // --- 【新增】施法成功后，立即刷新 UI ---
                // 这样 CD 遮罩会瞬间出现，蓝量条也会瞬间扣减
                if (selectedPiece != null)
                {
                    ShowUI(selectedPiece);
                    // 或者只调用 ConfigureActionButtons(selectedPiece); 也可以
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
            // 点击了无效区域：播放错误音效，取消施法，回到选中棋子状态
            PlaySpellErrorSound(SpellCastFailReason.InvalidTarget);

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
            moveButtonIcon.sprite = GetMoveIconForFaction(piece.ResolvedFaction);
        }

        TooltipTrigger moveTrigger = moveButton.GetComponent<TooltipTrigger>();
        if (moveTrigger != null) moveTrigger.SetContent("<b>Make a movement</b>\n<size=80%>");

        ConfigureSingleSpellButton(spellButton1, spellButton1Icon, spellButton1CDOverlay, spellButton1CDText, piece, 0);
        ConfigureSingleSpellButton(spellButton2, spellButton2Icon, spellButton2CDOverlay, spellButton2CDText, piece, 1);
    }
    /*
    private void ConfigureSingleSpellButton(Button btn, Image iconImg, Image cdOverlay, TextMeshProUGUI cdText,Piece piece, int index)
    {
        // 1. 先把 CD UI 隐藏，防止没有技能时残留显示
        if (cdOverlay != null) cdOverlay.gameObject.SetActive(false);
        if (cdText != null) cdText.gameObject.SetActive(false);

        if (index >= piece.Spells.Count)
        {
            btn.gameObject.SetActive(false);
            return;
        }

        Spell spell = piece.Spells[index];

        btn.gameObject.SetActive(true);
        // Always keep button interactable so we can play error sounds when clicked
        // Visual feedback (CD overlay, mana color) will indicate if spell is unusable
        btn.interactable = true;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnSpellButton(index));

        if (iconImg != null) iconImg.sprite = GetIconForSpell(spell.SpellName);
        UpdateSpellCooldownUI(spell, cdOverlay, cdText);
        TooltipTrigger trigger = btn.GetComponent<TooltipTrigger>();
      
        if (trigger != null)
        {
            string desc = $"<b>{spell.SpellName}</b> <size=80%><color=#4287f5>({spell.ManaCost} Mana)</color></size>\nCD: {spell.Cooldown} Turn(s)\n{spell.Description}";
            trigger.SetContent(desc);
        }
        
    }
    */
    private void ConfigureSingleSpellButton(Button btn, Image iconImg, Image cdOverlay, TextMeshProUGUI cdText, Piece piece, int index)
    {
        // 1. 初始化隐藏 CD 遮罩
        if (cdOverlay != null) cdOverlay.gameObject.SetActive(false);
        if (cdText != null) cdText.gameObject.SetActive(false);

        if (index >= piece.Spells.Count)
        {
            btn.gameObject.SetActive(false);
            return;
        }

        Spell spell = piece.Spells[index];

        btn.gameObject.SetActive(true);
        // 【关键】保持按钮可交互，这样点击才能播放错误音效
        btn.interactable = true;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnSpellButton(index));

        if (iconImg != null)
        {
            iconImg.sprite = GetIconForSpell(spell.SpellName);

            // ========================= 核心修改：手动应用颜色 =========================

            // 1. 获取你在 Inspector 里设置的颜色配置
            // 这样你以后想改颜色，直接在 Unity 编辑器里改 Button 的 Disabled Color 即可，不用改代码
            var colors = btn.colors;

            // 2. 判断是否缺蓝
            if (piece.CurrentMana < spell.ManaCost)
            {
                // 缺蓝：手动把图标染成你在 Inspector 里设置的 "Disabled Color"
                // 注意：因为 Button 是 Tint 模式，这里修改 Image 颜色会和 Button 的 Normal Color 叠加
                // 只要 Normal Color 是白色，这里就会显示你设置的紫色
                iconImg.color = colors.disabledColor;
            }
            else
            {
                // 蓝量充足：恢复白色（原色）
                iconImg.color = Color.white;
            }
            // ====================================================================
        }

        UpdateSpellCooldownUI(spell, cdOverlay, cdText);

        TooltipTrigger trigger = btn.GetComponent<TooltipTrigger>();
        if (trigger != null)
        {
            // 顺便把 Tooltip 里的蓝量消耗也变成红色，提示更明显
            string manaColor = (piece.CurrentMana < spell.ManaCost) ? "#ff4444" : "#4287f5";
            string desc = $"<b>{spell.SpellName}</b> <size=80%><color={manaColor}>({spell.ManaCost} Mana)</color></size>\nCD: {spell.Cooldown} Turn(s)\n{spell.Description}";
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

    private Sprite GetMoveIconForFaction(Faction faction)
    {
        Faction resolvedFaction = FactionSelectionManager.Instance != null
            ? FactionSelectionManager.Instance.ResolveFaction(faction)
            : faction;

        Sprite icon = null;

        if (factionMoveIcons != null && factionMoveIcons.Count > 0)
        {
            icon = factionMoveIcons.FirstOrDefault(data => data.faction == resolvedFaction).icon;
        }

        if (icon == null)
        {
            switch (resolvedFaction)
            {
                case Faction.Dwarf:
                    icon = dwarfMoveIcon;
                    break;
                case Faction.Elf:
                default:
                    icon = elfMoveIcon != null ? elfMoveIcon : defaultMoveIcon;
                    break;
            }
        }

        if (icon == null)
        {
            icon = defaultMoveIcon;
        }

        return icon;
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