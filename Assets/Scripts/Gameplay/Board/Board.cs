using UnityEngine;

public class Board : MonoBehaviour
{
    public GameObject Square;

    [Header("Elf Prefabs")]
    public GameObject[] ElfPiecePrefabs;

    [Header("Dwarf Prefabs")]
    public GameObject[] DwarfPiecePrefabs;

    public Material[] PieceMaterials;
    public int Width = 8;
    public int Height = 8;
    public LogicManager logicManager;

    [Header("棋盘格子材质")]
    public Material blackSquareMaterial;
    public Material whiteSquareMaterial;

    [Header("棋盘尺寸设置")]
    [Tooltip("格子的大小/间距。设置为 1 是默认大小，设置为 2 则面积大 4 倍")]
    public float squareSpacing = 1.5f; // <--- 【新增】控制格子大小和间距

    [Header("棋子缩放设置")]
    //public float pieceScale = 0.1f;
    public float pawnScale = 0.1f;        // 前排士兵的大小
    public float majorPieceScale = 0.12f; // 后排大棋子的大小（建议比兵大 20% 左右）


    [Header("棋子高度设置")]
    //public float majorPieceYOffset = 0.6f;
    public float pieceYOffset = 0.5f;
    public float pawnYOffset = 0.4f;

    // 这是我为展示泡佛教临时打的补丁。。。
    [Header("阵营微调 (Faction Adjustments)")]
    [Tooltip("给白方(Elf)额外增加的高度，解决陷地问题")]
    public float elfHeightCorrection = 0.0f;

    [Tooltip("给黑方(Dwarf)额外增加的高度")]
    public float dwarfHeightCorrection = 0.0f;

    void Start()
    {
        if (logicManager != null)
        {
            logicManager.Initialize();
            GenerateBoard();
            PlaceStartingPosition();
        }
        else
        {
            Debug.LogError("LogicManager 未分配！");
        }
    }

    // --- 【新增】辅助方法：获取逻辑坐标对应的世界坐标中心 ---
    public Vector3 GetTileCenter(int x, int z, float yOffset = 0f)
    {
        // x * 间距, y高度, z * 间距
        return new Vector3(x * squareSpacing, yOffset, z * squareSpacing);
    }

    public void GenerateBoard()
    {
        // (材质初始化代码保持不变...)
        if (blackSquareMaterial == null)
        {
            blackSquareMaterial = new Material(Shader.Find("Standard"));
            blackSquareMaterial.color = Color.black;
        }
        if (whiteSquareMaterial == null)
        {
            whiteSquareMaterial = new Material(Shader.Find("Standard"));
            whiteSquareMaterial.color = Color.white;
        }

        for (int i = 0; i < Width; i++)
        {
            for (int j = 0; j < Height; j++)
            {
                // 1. 【修改】位置：使用 squareSpacing 计算位置
                Vector3 position = GetTileCenter(i, j, 0);

                GameObject squareObject = Instantiate(Square, position, Quaternion.identity);
                squareObject.transform.parent = this.transform;

                // 2. 【修改】缩放：改变 X 和 Z (面积)，保持 Y (厚度) 不变
                // 假设你的 Square Prefab 原始大小是 1x1x1
                Vector3 originalScale = squareObject.transform.localScale;
                squareObject.transform.localScale = new Vector3(squareSpacing, originalScale.y, squareSpacing);

                Square square = squareObject.GetComponent<Square>();
                logicManager.squares[i, j] = square;

                Renderer renderer = square.GetComponent<Renderer>();
                if ((i + j) % 2 == 0)
                    renderer.material = blackSquareMaterial;
                else
                    renderer.material = whiteSquareMaterial;
            }
        }
    }

    public void PlaceStartingPosition()
    {
        // (检查 Prefab 数量的代码保持不变...)
        if (ElfPiecePrefabs.Length < 6 || DwarfPiecePrefabs.Length < 6 || PieceMaterials.Length < 2)
        {
            Debug.LogError("资源不足，无法生成棋子");
            return;
        }

        // --- 1. 预先计算好双方的最终高度 ---
        // 白方高度 = 通用高度 + 白方修正
        float elfPawnY = pawnYOffset + elfHeightCorrection;
        float elfMajorY = pieceYOffset + elfHeightCorrection; // 如果你启用了 majorPieceYOffset，这里就用 majorPieceYOffset

        // 黑方高度 = 通用高度 + 黑方修正
        float dwarfPawnY = pawnYOffset + dwarfHeightCorrection;
        float dwarfMajorY = pieceYOffset + dwarfHeightCorrection;


        // --- 放置白方阵营 (Elf) ---
        // Pawns (使用 elfPawnY)
        for (int i = 0; i < Width; i++)
        {
            InstantiatePiece(ElfPiecePrefabs[0], GetTileCenter(i, 1, elfPawnY),"Pawn", true, Faction.Elf);
        }
        // Rooks (使用 elfMajorY)
        InstantiatePiece(ElfPiecePrefabs[1], GetTileCenter(0, 0, elfMajorY),"Rook", true, Faction.Elf);
        InstantiatePiece(ElfPiecePrefabs[1], GetTileCenter(7, 0, elfMajorY),"Rook", true, Faction.Elf);
        // Knights
        InstantiatePiece(ElfPiecePrefabs[2], GetTileCenter(1, 0, elfMajorY),"Knight", true, Faction.Elf);
        InstantiatePiece(ElfPiecePrefabs[2], GetTileCenter(6, 0, elfMajorY),"Knight", true, Faction.Elf);
        // Bishops
        InstantiatePiece(ElfPiecePrefabs[3], GetTileCenter(2, 0, elfMajorY),"Bishop", true, Faction.Elf);
        InstantiatePiece(ElfPiecePrefabs[3], GetTileCenter(5, 0, elfMajorY),"Bishop", true, Faction.Elf);
        // Queen
        InstantiatePiece(ElfPiecePrefabs[4], GetTileCenter(3, 0, elfMajorY),"Queen", true, Faction.Elf);
        // King
        InstantiatePiece(ElfPiecePrefabs[5], GetTileCenter(4, 0, elfMajorY),"King", true, Faction.Elf);


        // --- 放置黑方阵营 (Dwarf) ---
        // Pawns (使用 dwarfPawnY)
        for (int i = 0; i < Width; i++)
        {
            InstantiatePiece(DwarfPiecePrefabs[0], GetTileCenter(i, 6, dwarfPawnY),"Pawn", false, Faction.Dwarf);
        }
        // Rooks (使用 dwarfMajorY)
        InstantiatePiece(DwarfPiecePrefabs[1], GetTileCenter(0, 7, dwarfMajorY),"Rook", false, Faction.Dwarf);
        InstantiatePiece(DwarfPiecePrefabs[1], GetTileCenter(7, 7, dwarfMajorY),"Rook", false, Faction.Dwarf);
        // Knights
        InstantiatePiece(DwarfPiecePrefabs[2], GetTileCenter(1, 7, dwarfMajorY),"Knight", false, Faction.Dwarf);
        InstantiatePiece(DwarfPiecePrefabs[2], GetTileCenter(6, 7, dwarfMajorY),"Knight", false, Faction.Dwarf);
        // Bishops
        InstantiatePiece(DwarfPiecePrefabs[3], GetTileCenter(2, 7, dwarfMajorY),"Bishop", false, Faction.Dwarf);
        InstantiatePiece(DwarfPiecePrefabs[3], GetTileCenter(5, 7, dwarfMajorY),"Bishop", false, Faction.Dwarf);
        // Queen
        InstantiatePiece(DwarfPiecePrefabs[4], GetTileCenter(3, 7, dwarfMajorY),"Queen", false, Faction.Dwarf);
        // King
        InstantiatePiece(DwarfPiecePrefabs[5], GetTileCenter(4, 7, dwarfMajorY),"King", false, Faction.Dwarf);

        Debug.Log("棋盘摆放完成！");
        if (logicManager != null) logicManager.TriggerInitialTurnStartPhase();
    }

    // InstantiatePiece 方法保持不变，因为它接收的是计算好的 Vector3 position
    public void InstantiatePiece(GameObject piecePrefab, Vector3 position, string pieceType, bool isWhite, Faction faction)
    {
        Quaternion rotation = isWhite ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
        GameObject pieceObject = Instantiate(piecePrefab, position, rotation);
        pieceObject.transform.parent = this.transform;

        //pieceObject.transform.localScale = Vector3.one * pieceScale; 

        float currentScale = (pieceType == "Pawn") ? pawnScale : majorPieceScale;
        pieceObject.transform.localScale = Vector3.one * currentScale;

        /*
        Renderer renderer = pieceObject.GetComponent<Renderer>();
        if (renderer != null) renderer.material = material;
        */
        Piece piece = pieceObject.GetComponent<Piece>();
        if (piece != null)
        {
            piece.Initialize(pieceType, isWhite, faction);
            piece.UpdateBoardMap();
        }
    }

    // ==================== 公共方法 (Public Methods) - 已修正 ====================

    /// <summary>
    /// 根据棋子类型和阵营查找对应的 Prefab。
    /// LogicManager 将调用此方法来获取正确的棋子预制件。
    /// </summary>
    public GameObject FindPiecePrefab(string pieceType, Faction pieceFaction)
    {
        // 决定要在哪个预制件数组里查找
        GameObject[] prefabsToSearch = null;
        if (pieceFaction == Faction.Elf)
        {
            prefabsToSearch = ElfPiecePrefabs;
        }
        else if (pieceFaction == Faction.Dwarf)
        {
            prefabsToSearch = DwarfPiecePrefabs;
        }

        if (prefabsToSearch != null)
        {
            foreach (var prefab in prefabsToSearch)
            {
                Piece pieceComponent = prefab.GetComponent<Piece>();
                if (pieceComponent != null && pieceComponent.PieceType == pieceType)
                {
                    // 找到了匹配的类型和阵营，返回它
                    return prefab;
                }
            }
        }

        // 如果循环结束了还没找到，就报错
        Debug.LogError($"在 Board 的 Prefab 列表中找不到类型为 {pieceType} 且阵营为 {pieceFaction} 的 Prefab！");
        return null;
    }

    /// <summary>
    /// 公开的棋子实例化方法。
    /// LogicManager 将调用此方法来在棋盘上创建新的棋子实例。
    /// </summary>
    public GameObject InstantiatePiecePublic(GameObject prefab, Vector3 position,string pieceType, bool isWhite, Faction faction)
    {
        // 根据颜色决定旋转
        Quaternion rotation = isWhite ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);

        GameObject newPieceObject = Instantiate(prefab, position, rotation);
        newPieceObject.transform.SetParent(this.transform); // 将棋子设为 Board 的子对象

        //newPieceObject.transform.localScale = Vector3.one * pieceScale;
        float currentScale = (pieceType == "Pawn") ? pawnScale : majorPieceScale;
        newPieceObject.transform.localScale = Vector3.one * currentScale;

        Piece newPiece = newPieceObject.GetComponent<Piece>();
        if (newPiece != null)
        {
            // 修正：调用你项目中实际使用的 3 参数 Initialize 方法
            newPiece.Initialize(pieceType, isWhite, faction);
            //newPiece.GetComponent<Renderer>().material = material;
        }
        else
        {
            Debug.LogError($"Prefab {prefab.name} 上缺少 Piece 组件!");
        }

        return newPieceObject;
    }
}
