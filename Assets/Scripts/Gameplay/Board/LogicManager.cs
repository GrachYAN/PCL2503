using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using UnityEngine.UI;

public class LogicManager : NetworkBehaviour
{
    // --- All your original variables ---
    public Piece[,] boardMap = new Piece[8, 8];
    public Square[,] squares = new Square[8, 8];

    private class MindControlEffect
    {
        public Piece ControlledPiece;
        public int RemainingTurns; // 剩余半回合数
    }
    private readonly List<MindControlEffect> activeMindControlEvents = new List<MindControlEffect>();

    [Header("Board Reference")]
    public Board board;  // 引用 Board 脚本

    //barrier prefab
    [Header("Spell Prefabs")]
    public GameObject prismaticBarrierPrefab;
    public GameObject flameMarkPrefab; // 火焰标记的

    private class PrismaticBarrierInstance
    {
        public Vector2 Position;
        public int RemainingRounds;
        public GameObject PrefabInstance;
    }
    private readonly List<PrismaticBarrierInstance> activeBarriers = new List<PrismaticBarrierInstance>();

    private class FlameMarkInstance
    {
        public Vector2 Position;
        public int RemainingRounds;
        public GameObject PrefabInstance;
    }
    private readonly List<FlameMarkInstance> activeFlameMarks = new List<FlameMarkInstance>();



    // --- NEW TURN LOGIC ---
    // This is the "source of truth" for the turn in ONLINE mode.
    // It's server-authoritative.
    private NetworkVariable<bool> m_IsWhiteTurn_Network = new NetworkVariable<bool>(true);

    private bool isOfflineMode = false;
    private bool m_IsWhiteTurn_Offline = true;


    public bool IsWhiteTurn
    {
        get
        {
            if (isOfflineMode)
                return m_IsWhiteTurn_Offline;
            else
                return m_IsWhiteTurn_Network.Value;
        }
    }

    public List<Piece> piecesOnBoard = new List<Piece>();
    private class RampartAura
    {
        public Piece Source;
        public int RemainingRounds;
        public int ReductionAmount;
    }

    private readonly List<RampartAura> activeRampartAuras = new List<RampartAura>();

    private class HeartOfMountainBuff
    {
        public bool AppliesToWhite;
        public int RemainingRounds;
    }

    private HeartOfMountainBuff whiteHeartBuff;
    private HeartOfMountainBuff blackHeartBuff;
    private CameraController cameraController;
    private GameOverUI gameOverUI;
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
    }

    public void Start()
    {
        cameraController = FindFirstObjectByType<CameraController>();
        gameOverUI = FindFirstObjectByType<GameOverUI>();

        // This check is now required
        if (GameModeManager.Instance != null && GameModeManager.Instance.CurrentMode == GameModeManager.GameMode.Offline)
        {
            isOfflineMode = true;
        }

        if (cameraController != null)
        {
            cameraController.WhitePerspective();
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

    [ServerRpc(RequireOwnership = false)]
    public void RequestMoveServerRpc(int startX, int startY, int endX, int endY, ServerRpcParams rpcParams = default)
    {
        Debug.Log($"Server: Received move request from {rpcParams.Receive.SenderClientId}");

        // Always rebuild the authoritative board before validating to avoid any stale client-side data.
        RebuildBoardState();

        // --- 1. Validation ---
        Piece piece = boardMap[startX, startY];
        if (piece == null) return;
        Vector2 targetCoords = new Vector2(endX, endY);

        // --- 2. Security & Turn Validation ---
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        bool isHost = senderClientId == 0;
        bool isMyPiece = (isHost && piece.IsWhite) || (!isHost && !piece.IsWhite);
        if (!isMyPiece)
        {
            Debug.LogError($"Server: Client {senderClientId} tried to move opponent's piece!");
            return;
        }
        if (piece.IsWhite != IsWhiteTurn) // Checks m_IsWhiteTurn_Network.Value
        {
            Debug.LogError($"Server: Client {senderClientId} tried to move out of turn!");
            return;
        }

        // Captures are not allowed in this rule set; only empty squares are valid destinations.
        if (boardMap[endX, endY] != null)
        {
            Debug.LogError($"Server: Client {senderClientId} tried to move into an occupied square (captures disabled).");
            return;
        }

        List<Vector2> legalMoves = piece.GetLegalMoves();
        if (!legalMoves.Contains(targetCoords))
        {
            Debug.LogError($"Server: Client {senderClientId} sent an illegal move for {piece.PieceType} to ({endX},{endY}).");
            return;
        }

        // --- 3. Execute Move ---
        piece.Move(targetCoords);
        MovePieceClientRpc(startX, startY, endX, endY);

        lastMovedPiece = piece;
        lastMovedPieceStartPosition = new Vector2(startX, startY);
        lastMovedPieceEndPosition = targetCoords;

        if (moveSound != null)
        {
            moveSound.Play();
        }

        // --- 4. End Turn ---
        EndTurn(); // This will flip m_IsWhiteTurn_Network.Value
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestCastSpellServerRpc(int pieceX, int pieceY, int spellIndex, SpellCastData castData, ServerRpcParams rpcParams = default)
    {
        Debug.Log($"Server: Received spell cast request from {rpcParams.Receive.SenderClientId}");

        // --- 1. Validation ---
        Piece piece = boardMap[pieceX, pieceY];
        if (piece == null)
        {
            Debug.LogError("Server: Client tried to cast from a null piece.");
            return;
        }

        if (spellIndex < 0 || spellIndex >= piece.Spells.Count)
        {
            Debug.LogError("Server: Client sent invalid spell index.");
            return;
        }

        Spell spell = piece.Spells[spellIndex];
        Vector2 targetCoords = new Vector2(castData.PrimaryX, castData.PrimaryY);

        // --- 2. Security & Turn Validation ---
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        bool isHost = senderClientId == 0;
        bool isMyPiece = (isHost && piece.IsWhite) || (!isHost && !piece.IsWhite);
        if (!isMyPiece)
        {
            Debug.LogError($"Server: Client {senderClientId} tried to cast with opponent's piece!");
            return;
        }

        if (piece.IsWhite != IsWhiteTurn)
        {
            Debug.LogError($"Server: Client {senderClientId} tried to cast out of turn!");
            return;
        }

        if (!spell.CanCast())
        {
            Debug.LogError($"Server: Client {senderClientId} tried to cast spell, but CanCast() is false (cooldown/mana).");
            return;
        }

        if (!spell.IsCastDataValid(castData))
        {
            Debug.LogError($"Server: Client {senderClientId} sent invalid targeting data.");
            return;
        }

        // --- 3. Execute Spell ---
        Debug.Log($"Server: Executing spell '{spell.SpellName}'");
        spell.ApplyCastData(castData);
        spell.Cast(targetCoords);
        CastSpellClientRpc(pieceX, pieceY, spellIndex, castData);
        // --- 4. End Turn ---
        EndTurn(); // This flips the m_IsWhiteTurn_Network variable
    }

    [ClientRpc]
    private void MovePieceClientRpc(int startX, int startY, int endX, int endY)
    {
        // This code now runs on EVERY client (including the server)
        Piece piece = boardMap[startX, startY];
        if (piece == null) return; // Should not happen if validation passed

        Vector2 targetCoords = new Vector2(endX, endY);

        // --- Execute the move logic for everyone ---
        // This moves the piece on everyone's local game
        piece.Move(targetCoords);

        lastMovedPiece = piece;
        lastMovedPieceStartPosition = new Vector2(startX, startY);
        lastMovedPieceEndPosition = targetCoords;

        if (moveSound != null)
        {
            moveSound.Play();
        }
    }

    [ClientRpc]
    private void CastSpellClientRpc(int pieceX, int pieceY, int spellIndex, SpellCastData castData)
    {
        // This code now runs on EVERY client (including the server)
        Piece piece = boardMap[pieceX, pieceY];
        if (piece == null) return;
        if (spellIndex < 0 || spellIndex >= piece.Spells.Count) return;

        Spell spell = piece.Spells[spellIndex];
        Vector2 targetCoords = new Vector2(castData.PrimaryX, castData.PrimaryY);

        // This casts the spell on everyone's local game
        spell.ApplyCastData(castData);
        spell.Cast(targetCoords);
    }

    [ClientRpc]
    private void SyncTurnStartClientRpc(bool isWhiteTurnPhase, bool shouldTick)
    {
        if (IsServer)
        {
            return;
        }

        RunTurnStartPhase(isWhiteTurnPhase);

        if (shouldTick)
        {
            TickRampartAuras();
            TickSunwellWardAuras();
            TickHeartOfMountainBuffs();
            TickSunwellAnthemBuffs();
            TickMindControlEvents();
            TickPrismaticBarriers();
            TickFlameMarks();
        }
    }

    // --- All your original helper methods ---
    public void Initialize()
    {
        // isWhiteTurn = true; // Old logic
        m_IsWhiteTurn_Offline = true; // Initialize the offline var
        if (IsServer) // Only the server can set the network variable's default
        {
            m_IsWhiteTurn_Network.Value = true;
        }
    }

    public bool IsMyTurn(bool pieceIsWhite)
    {
        // Use the new public property here!
        return pieceIsWhite == IsWhiteTurn;
    }


    public void EndTurn()
    {
        currentTurnNumber++;
        // --- This is the new turn-flipping logic ---
        if (isOfflineMode)
        {
            // In OFFLINE, just flip the local variable
            m_IsWhiteTurn_Offline = !m_IsWhiteTurn_Offline;
        }
        else
        {
            // In ONLINE, only the SERVER can flip the turn
            if (IsServer)
            {
                m_IsWhiteTurn_Network.Value = !m_IsWhiteTurn_Network.Value;
            }
            else
            {
                // A client should never call EndTurn directly
                Debug.LogWarning("Client tried to call EndTurn!");
                return;
            }
        }
        // --- End of new logic ---

        // All the rest of your EndTurn logic runs for everyone
        // (Clients will run this when they detect the network variable changed,
        // but for now, this server-driven approach is simpler)
        RunTurnStartPhase(IsWhiteTurn);

        TickRampartAuras();
        TickSunwellWardAuras();
        TickHeartOfMountainBuffs();
        TickSunwellAnthemBuffs();
        TickMindControlEvents();
        TickPrismaticBarriers();
        TickFlameMarks();

        if (!isOfflineMode && IsServer)
        {
            SyncTurnStartClientRpc(IsWhiteTurn, true);
        }

        if (isCameraRotationEnabled && cameraController != null)
        {
            if (IsWhiteTurn) // This will use the correct property
            {
                cameraController.WhitePerspective();
            }
            else
            {
                cameraController.BlackPerspective();
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

    private void RebuildBoardState()
    {
        // Clear the authoritative map first.
        for (int x = 0; x < boardMap.GetLength(0); x++)
        {
            for (int y = 0; y < boardMap.GetLength(1); y++)
            {
                boardMap[x, y] = null;
            }
        }

        // Place every active piece onto the map according to its scene position.
        foreach (Piece piece in FindObjectsOfType<Piece>())
        {
            Vector2 coords = piece.GetCoordinates();
            int x = Mathf.Clamp(Mathf.RoundToInt(coords.x), 0, 7);
            int y = Mathf.Clamp(Mathf.RoundToInt(coords.y), 0, 7);
            boardMap[x, y] = piece;
        }

        UpdatePiecesOnBoard();
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

        DestroyedPieceInfo info = new DestroyedPieceInfo
        {
            PieceType = piece.PieceType,
            IsWhite = piece.IsWhite,
            PieceFaction = piece.ResolvedFaction,
            DestroyedTurn = currentTurnNumber,
            MaxMana = piece.MaxMana
        };

        if (piece.IsWhite)
        {
            whiteDestroyedPieces.Add(info);
        }
        else
        {
            blackDestroyedPieces.Add(info);
        }

        Debug.Log($" {piece.PieceType} 被击毁,已记录到复活列表");

        if (piecesOnBoard.Contains(piece))
        {
            piecesOnBoard.Remove(piece);
        }

        boardMap[(int)coords.x, (int)coords.y] = null;
        piecesOnBoard.Remove(piece);
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

    public void RegisterRampartAura(Piece source, int reductionAmount, int duration)
    {
        if (source == null)
        {
            return;
        }

        activeRampartAuras.RemoveAll(aura => aura.Source == null || aura.Source == source);
        activeRampartAuras.Add(new RampartAura
        {
            Source = source,
            RemainingRounds = duration,
            ReductionAmount = reductionAmount
        });
    }

    private void TickRampartAuras()
    {
        for (int i = activeRampartAuras.Count - 1; i >= 0; i--)
        {
            RampartAura aura = activeRampartAuras[i];
            if (aura.Source == null)
            {
                activeRampartAuras.RemoveAt(i);
                continue;
            }

            aura.RemainingRounds--;
            if (aura.RemainingRounds <= 0)
            {
                activeRampartAuras.RemoveAt(i);
            }
        }
    }

    public int GetDamageReductionForPiece(Piece piece)
    {
        if (piece == null)
        {
            return 0;
        }

        Vector2 piecePos = piece.GetCoordinates();
        int reduction = 0;

        for (int i = activeRampartAuras.Count - 1; i >= 0; i--)
        {
            RampartAura aura = activeRampartAuras[i];
            if (aura.Source == null)
            {
                activeRampartAuras.RemoveAt(i);
                continue;
            }

            Vector2 center = aura.Source.GetCoordinates();
            if (Mathf.Abs(center.x - piecePos.x) <= 1 && Mathf.Abs(center.y - piecePos.y) <= 1)
            {
                reduction += aura.ReductionAmount;
            }
        }

        return reduction;
    }

    public void ApplyHeartOfMountainBuff(bool isWhite, int duration)
    {
        HeartOfMountainBuff buff = new HeartOfMountainBuff
        {
            AppliesToWhite = isWhite,
            RemainingRounds = duration
        };

        if (isWhite)
        {
            whiteHeartBuff = buff;
        }
        else
        {
            blackHeartBuff = buff;
        }

        UpdatePiecesOnBoard();
        foreach (Piece piece in piecesOnBoard)
        {
            if (piece != null && piece.IsWhite == isWhite)
            {
                piece.ClearRoot();
            }
        }
    }

    public bool HasHeartOfMountainBuff(bool isWhite)
    {
        HeartOfMountainBuff buff = isWhite ? whiteHeartBuff : blackHeartBuff;
        return buff != null && buff.RemainingRounds > 0;
    }

    private void RunTurnStartPhase(bool activeTurnIsWhite)
    {
        UpdatePiecesOnBoard();
        foreach (Piece piece in piecesOnBoard)
        {
            piece?.OnTurnStart(activeTurnIsWhite);
        }
    }

    public void TriggerInitialTurnStartPhase()
    {
        if (IsClient && !IsServer)
        {
            return;
        }

        RunTurnStartPhase(IsWhiteTurn);

        if (!isOfflineMode && IsServer)
        {
            SyncTurnStartClientRpc(IsWhiteTurn, false);
        }
    }

    private void TickHeartOfMountainBuffs()
    {
        if (whiteHeartBuff != null)
        {
            whiteHeartBuff.RemainingRounds--;
            if (whiteHeartBuff.RemainingRounds <= 0)
            {
                whiteHeartBuff = null;
            }
        }

        if (blackHeartBuff != null)
        {
            blackHeartBuff.RemainingRounds--;
            if (blackHeartBuff.RemainingRounds <= 0)
            {
                blackHeartBuff = null;
            }
        }
    }

    /*
    public bool HasLineOfSight(Vector2 start, Vector2 end)
    {
        // 此实现适用于直线和对角线。
        Vector2 direction = end - start;
        Vector2 step = Vector2.zero;

        // 确定步进方向 (dx, dy 只能是 -1, 0, 或 1)
        float dx = Mathf.Clamp(direction.x, -1, 1);
        float dy = Mathf.Clamp(direction.y, -1, 1);
        step = new Vector2(dx, dy);

        if (direction.x != 0 && direction.y != 0 && Mathf.Abs(direction.x) != Mathf.Abs(direction.y))
        {
            // 对于非直线/对角线，如 骑士(Knight)，LoS 通常不适用
            return true;
        }

        Vector2 currentPos = start + step; // 从起点后一格开始检查

        while (Vector2.Distance(currentPos, end) > 0.1f) // 检查是否到达终点
        {
            if (currentPos.x < 0 || currentPos.x >= 8 || currentPos.y < 0 || currentPos.y >= 8)
            {
                break; // 超出棋盘
            }

            // 检查格子上是否有棋子
            if (boardMap[(int)currentPos.x, (int)currentPos.y] != null)
            {
                return false; // 被阻挡！ 
            }

            currentPos += step;
        }

        // 循环完成，没有障碍物
        return true;
    }
    */
    /// <summary>
    /// 检查两点之间是否有视线（LoS），会同时检查路径上的棋子和棱镜屏障。
    /// </summary>
    public bool HasLineOfSight(Vector2 start, Vector2 end)
    {
        Vector2 direction = end - start;

        // 确定总步数（取X和Y方向移动距离的较大值）
        int steps = (int)Mathf.Max(Mathf.Abs(direction.x), Mathf.Abs(direction.y));

        // 如果起点和终点重合，或者根本不是直线/对角线移动，则认为有视线
        if (steps == 0) return true;
        if (Mathf.Abs(direction.x) != 0 && Mathf.Abs(direction.y) != 0 && Mathf.Abs(direction.x) != Mathf.Abs(direction.y))
        {
            // 这不是一条标准的直线或对角线（例如骑士移动），我们认为它“穿透”了障碍
            return true;
        }

        // 获取单位步进方向
        Vector2 stepDirection = direction / steps;

        // 从起点后一格开始，到终点前一格结束
        for (int i = 1; i < steps; i++)
        {
            Vector2 currentPos = start + stepDirection * i;
            int x = (int)Mathf.Round(currentPos.x); // 使用Round更安全
            int y = (int)Mathf.Round(currentPos.y);

            // 检查格子上是否有棋子
            if (boardMap[x, y] != null)
            {
                return false; // 被棋子阻挡
            }

            // ⭐ 关键修改：检查格子上是否有屏障
            if (activeBarriers.Any(b => (int)b.Position.x == x && (int)b.Position.y == y))
            {
                return false; // 被屏障阻挡
            }
        }

        // 循环完成，路径上没有障碍物
        return true;
    }
    // ==================== 复活系统 (Revival System) ====================
    // 暂时不支持网络同步

    /// <summary>
    /// 用于存储被击毁棋子的信息
    /// </summary>
    public class DestroyedPieceInfo
    {
        public string PieceType;
        public bool IsWhite;
        public Faction PieceFaction;
        public int DestroyedTurn;
        public int MaxMana;
    }

    // 这两个列表将公开，以便技能脚本可以访问
    public List<DestroyedPieceInfo> whiteDestroyedPieces = new List<DestroyedPieceInfo>();
    public List<DestroyedPieceInfo> blackDestroyedPieces = new List<DestroyedPieceInfo>();

    private int currentTurnNumber = 0; // 用于追踪当前回合数

    // ==================== 复活系统 - 核心方法 ====================
    //todo: 1.网络同步 2. 拓展： 后排12排？
    /// <summary>
    /// 检查是否有任何已阵亡的棋子可供复活
    /// </summary>
    public bool HasDestroyedPiece(bool isWhite)
    {
        return isWhite ? whiteDestroyedPieces.Count > 0 : blackDestroyedPieces.Count > 0;
    }

    /// <summary>
    /// 获取最后阵亡的棋子信息
    /// </summary>
    public DestroyedPieceInfo GetLastDestroyedPiece(bool isWhite)
    {
        var list = isWhite ? whiteDestroyedPieces : blackDestroyedPieces;
        if (list.Count == 0) return null;
        // 返回列表中的最后一个元素，即最近被摧毁的棋子
        return list[list.Count - 1];
    }

    /// <summary>
    /// 在指定位置复活一个棋子
    /// </summary>
   
    public bool RevivePiece(DestroyedPieceInfo pieceInfo, Vector2 position, int hp, bool fullMana)
    {
        if (pieceInfo == null || boardMap[(int)position.x, (int)position.y] != null)
        {
            Debug.LogError("复活失败：信息为空或目标位置不为空！");
            return false;
        }

        // 通过 Board 脚本找到对应的 Prefab
        GameObject prefab = board.FindPiecePrefab(pieceInfo.PieceType, pieceInfo.PieceFaction);
        if (prefab == null)
        {
            Debug.LogError($"复活失败：在 Board.cs 中找不到类型为 {pieceInfo.PieceType} 的 Prefab！");
            return false;
        }

        // 通过 Board 脚本来实例化棋子
        Material pieceMaterial = pieceInfo.IsWhite ? board.PieceMaterials[0] : board.PieceMaterials[1];
        //GameObject newPieceObject = board.InstantiatePiecePublic(prefab, new Vector3(position.x, board.pieceYOffset, position.y), pieceMaterial, pieceInfo.PieceType, pieceInfo.IsWhite, pieceInfo.PieceFaction);
        GameObject newPieceObject = board.InstantiatePiecePublic(prefab, new Vector3(position.x, board.pieceYOffset, position.y), pieceInfo.PieceType, pieceInfo.IsWhite, pieceInfo.PieceFaction);

        Piece newPiece = newPieceObject.GetComponent<Piece>();

        // 设置复活后的状态
        newPiece.SetCurrentHP(hp);
        if (fullMana)
        {
            newPiece.GainMana(newPiece.MaxMana);
        }

        // 更新棋盘数据
        boardMap[(int)position.x, (int)position.y] = newPiece;
        piecesOnBoard.Add(newPiece);

        // 从阵亡列表中移除该棋子
        var list = pieceInfo.IsWhite ? whiteDestroyedPieces : blackDestroyedPieces;
        list.Remove(pieceInfo);

        Debug.Log($" 成功复活 {pieceInfo.PieceType} 在 ({position.x}, {position.y})");
        return true;
    }

    // ==================== 光环 ===================
    // 1. 在 RampartAura 类的下方，添加 SunwellWardAura 类
    private class SunwellWardAura
    {
        public Piece Source;
        public int RemainingRounds;
    }

    // 2. 在 activeRampartAuras 列表的下方，添加新的光环列表
    private readonly List<SunwellWardAura> activeSunwellWardAuras = new List<SunwellWardAura>();

    // 3. 添加一个新的 public 方法来施加光环
    public void ApplySunwellWard(Piece sourceRook)
    {
        if (sourceRook == null) return;

        activeSunwellWardAuras.Add(new SunwellWardAura
        {
            Source = sourceRook,
            RemainingRounds = 1 // 持续1回合
        });
        Debug.Log($"{sourceRook.PieceType} created a Sunwell Ward aura.");
    }

    private void TickSunwellWardAuras()
    {
        for (int i = activeSunwellWardAuras.Count - 1; i >= 0; i--)
        {
            var aura = activeSunwellWardAuras[i];
            if (aura.Source == null || aura.Source.CurrentHP <= 0)
            {
                activeSunwellWardAuras.RemoveAt(i);
                continue;
            }

            // 光环在施法者的回合开始时生效，到下一个他的回合开始时结束
            if (aura.Source.IsWhite == IsWhiteTurn)
            {
                aura.RemainingRounds--;
            }

            if (aura.RemainingRounds < 0)
            {
                activeSunwellWardAuras.RemoveAt(i);
                Debug.Log("Sunwell Ward aura has expired.");
            }
        }
    }

    public bool IsPieceProtectedBySunwellWard(Piece piece)
    {
        foreach (var aura in activeSunwellWardAuras)
        {
            if (aura.Source != null && aura.Source.IsWhite == piece.IsWhite)
            {
                Vector2 sourcePos = aura.Source.GetCoordinates();
                Vector2 piecePos = piece.GetCoordinates();

                if (Mathf.Abs(sourcePos.x - piecePos.x) <= 1 && Mathf.Abs(sourcePos.y - piecePos.y) <= 1)
                {
                    return true; // 棋子在3x3范围内
                }
            }
        }
        return false;
    }


    private class SunwellAnthemBuff
    {
        public int RemainingRounds;
    }

    // 2. 在 blackHeartBuff 字段的下方，添加新的Buff字段
    private SunwellAnthemBuff whiteAnthemBuff;
    private SunwellAnthemBuff blackAnthemBuff;

    // 3. 添加一个新的 public 方法来施加 Buff
    public void ApplySunwellAnthem(bool isWhiteTeam)
    {
        var buff = new SunwellAnthemBuff { RemainingRounds = 2 };
        if (isWhiteTeam)
        {
            whiteAnthemBuff = buff;
        }
        else
        {
            blackAnthemBuff = buff;
        }

        // 遍历棋盘上所有棋子，为友方棋子施加效果
        UpdatePiecesOnBoard(); // 确保 piecesOnBoard 列表是最新
        foreach (Piece p in piecesOnBoard)
        {
            if (p != null && p.IsWhite == isWhiteTeam)
            {
                p.ApplyShield(5); // 施加5点护盾
                p.SetDamageBonus(2);  // 设置伤害加成
            }
        }
        Debug.Log((isWhiteTeam ? "White" : "Black") + " team is affected by Sunwell Anthem.");
    }

    private void TickSunwellAnthemBuffs()
    {
        // 处理白队Buff
        if (whiteAnthemBuff != null)
        {
            if (IsWhiteTurn) // 只在轮到白队时减少持续时间
            {
                whiteAnthemBuff.RemainingRounds--;
            }
            if (whiteAnthemBuff.RemainingRounds < 0)
            {
                whiteAnthemBuff = null;
                // 移除Buff效果
                foreach (Piece p in piecesOnBoard)
                {
                    if (p != null && p.IsWhite) p.SetDamageBonus(0);
                }
                Debug.Log("White team's Sunwell Anthem has expired.");
            }
        }

        // 处理黑队Buff
        if (blackAnthemBuff != null)
        {
            if (!IsWhiteTurn) // 只在轮到黑队时减少持续时间
            {
                blackAnthemBuff.RemainingRounds--;
            }
            if (blackAnthemBuff.RemainingRounds < 0)
            {
                blackAnthemBuff = null;
                // 移除Buff效果
                foreach (Piece p in piecesOnBoard)
                {
                    if (p != null && !p.IsWhite) p.SetDamageBonus(0);
                }
                Debug.Log("Black team's Sunwell Anthem has expired.");
            }
        }
    }

    // 6. 添加一个 public 方法供其他类查询 Buff 状态
    public bool HasSunwellAnthem(bool isWhiteTeam)
    {
        var buff = isWhiteTeam ? whiteAnthemBuff : blackAnthemBuff;
        return buff != null && buff.RemainingRounds >= 0;
    }


    // mind control ------------
    /// <summary>
    /// 公共接口，用于施加精神控制效果
    /// </summary>
    public void ApplyMindControl(Piece caster, Piece target)
    {
        if (target == null) return;

        // 施加控制效果，持续时间为2个“半回合”（即直到施法者下个回合开始）
        target.MindControl(caster.IsWhite);
        activeMindControlEvents.Add(new MindControlEffect
        {
            ControlledPiece = target,
            RemainingTurns = 2
        });

        // TODO: 在此播放精神控制的视觉和声音特效
    }

    /// <summary>
    /// 在回合结束时处理精神控制效果的持续时间
    /// </summary>
    private void TickMindControlEvents()
    {
        // 从后往前遍历，方便移除元素
        for (int i = activeMindControlEvents.Count - 1; i >= 0; i--)
        {
            var effect = activeMindControlEvents[i];
            effect.RemainingTurns--;

            if (effect.RemainingTurns <= 0)
            {
                // 效果结束，恢复棋子
                effect.ControlledPiece.RevertMindControl();
                activeMindControlEvents.RemoveAt(i);
            }
        }
    }

    // ==================== 屏障 ====================

    public void PlacePrismaticBarrier(Vector2 position, int duration)
    {
        if (prismaticBarrierPrefab == null)
        {
            Debug.LogError("Prismatic Barrier Prefab is not assigned in LogicManager!");
            return;
        }

        // 在棋盘上创建屏障的视觉效果
        GameObject barrierGO = Instantiate(prismaticBarrierPrefab, new Vector3(position.x, 0.5f, position.y), Quaternion.identity);

        var barrier = new PrismaticBarrierInstance
        {
            Position = position,
            RemainingRounds = duration * 2, // 持续3个完整回合 (6个半回合)
            PrefabInstance = barrierGO
        };
        activeBarriers.Add(barrier);
    }

    private void TickPrismaticBarriers()
    {
        for (int i = activeBarriers.Count - 1; i >= 0; i--)
        {
            var barrier = activeBarriers[i];
            barrier.RemainingRounds--;
            if (barrier.RemainingRounds <= 0)
            {
                Destroy(barrier.PrefabInstance);
                activeBarriers.RemoveAt(i);
            }
        }
    }

    public void PlaceFlameMark(Vector2 position, int duration)
    {
        if (flameMarkPrefab == null)
        {
            Debug.LogError("Flame Mark Prefab is not assigned in LogicManager!");
            return;
        }
        GameObject markGO = Instantiate(flameMarkPrefab, new Vector3(position.x, 0, position.y), Quaternion.identity);
        activeFlameMarks.Add(new FlameMarkInstance
        {
            Position = position,
            RemainingRounds = duration * 2,
            PrefabInstance = markGO
        });
    }

    private void TickFlameMarks()
    {
        for (int i = activeFlameMarks.Count - 1; i >= 0; i--)
        {
            var mark = activeFlameMarks[i];
            mark.RemainingRounds--;
            if (mark.RemainingRounds <= 0)
            {
                Destroy(mark.PrefabInstance);
                activeFlameMarks.RemoveAt(i);
            }
        }
    }
}