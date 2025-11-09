/*
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

    [Header("棋子缩放设置")]
    public float pieceScale = 0.1f;

    [Header("棋子高度设置")]
    public float pieceYOffset = 0.5f;     // ✅ 棋子 Y 轴偏移（调整贴合度）
    public float pawnYOffset = 0.4f;      // ✅ Pawn 的 Y 轴偏移

    void Start()
    {
        // 修正后的执行顺序
        if (logicManager != null)
        {
            // 1. 首先初始化逻辑管理器，确保内部数组（如 squares）已准备好
            logicManager.Initialize();

            // 2. 然后生成棋盘，此时可以安全地访问 logicManager.squares
            GenerateBoard();

            // 3. 最后放置棋子
            PlaceStartingPosition();
        }
        else
        {
            Debug.LogError("LogicManager 未分配！请在Unity编辑器中将LogicManager对象拖拽到Board脚本的对应字段上。");
        }
    }

    public void GenerateBoard()
    {
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
                GameObject squareObject = Instantiate(Square, new Vector3(i, 0, j), Quaternion.identity);
                squareObject.transform.parent = this.transform;
                Square square = squareObject.GetComponent<Square>();

                // 这行代码现在可以安全执行了
                logicManager.squares[i, j] = square;

                Renderer renderer = square.GetComponent<Renderer>();

                if ((i + j) % 2 == 0)
                {
                    renderer.material = blackSquareMaterial;
                }
                else
                {
                    renderer.material = whiteSquareMaterial;
                }
            }
        }
    }

    public void PlaceStartingPosition()
    {
        if (ElfPiecePrefabs.Length < 6)
        {
            Debug.LogError($"Elf Prefab 数量不足！需要 6 个，当前只有 {ElfPiecePrefabs.Length} 个");
            return;
        }
        if (DwarfPiecePrefabs.Length < 6)
        {
            Debug.LogError($"Dwarf Prefab 数量不足！需要 6 个，当前只有 {DwarfPiecePrefabs.Length} 个");
            return;
        }

        if (PieceMaterials.Length < 2)
        {
            Debug.LogError($"材质数量不足！需要 2 个，当前只有 {PieceMaterials.Length} 个");
            return;
        }

        // 白方阵营 (Elf) 使用 ElfPiecePrefabs
        // 黑方阵营 (Dwarf) 使用 DwarfPiecePrefabs

        // Pawn (兵)
        for (int i = 0; i < Width; i++)
        {
            InstantiatePiece(ElfPiecePrefabs[0], new Vector3(i, pawnYOffset, 1), PieceMaterials[0], "Pawn", true);
            InstantiatePiece(DwarfPiecePrefabs[0], new Vector3(i, pawnYOffset, 6), PieceMaterials[1], "Pawn", false);
        }

        // Rook (车)
        InstantiatePiece(ElfPiecePrefabs[1], new Vector3(0, pieceYOffset, 0), PieceMaterials[0], "Rook", true);
        InstantiatePiece(ElfPiecePrefabs[1], new Vector3(7, pieceYOffset, 0), PieceMaterials[0], "Rook", true);
        InstantiatePiece(DwarfPiecePrefabs[1], new Vector3(0, pieceYOffset, 7), PieceMaterials[1], "Rook", false);
        InstantiatePiece(DwarfPiecePrefabs[1], new Vector3(7, pieceYOffset, 7), PieceMaterials[1], "Rook", false);

        // Knight (马)
        InstantiatePiece(ElfPiecePrefabs[2], new Vector3(1, pieceYOffset, 0), PieceMaterials[0], "Knight", true);
        InstantiatePiece(ElfPiecePrefabs[2], new Vector3(6, pieceYOffset, 0), PieceMaterials[0], "Knight", true);
        InstantiatePiece(DwarfPiecePrefabs[2], new Vector3(1, pieceYOffset, 7), PieceMaterials[1], "Knight", false);
        InstantiatePiece(DwarfPiecePrefabs[2], new Vector3(6, pieceYOffset, 7), PieceMaterials[1], "Knight", false);

        // Bishop (象)
        InstantiatePiece(ElfPiecePrefabs[3], new Vector3(2, pieceYOffset, 0), PieceMaterials[0], "Bishop", true);
        InstantiatePiece(ElfPiecePrefabs[3], new Vector3(5, pieceYOffset, 0), PieceMaterials[0], "Bishop", true);
        InstantiatePiece(DwarfPiecePrefabs[3], new Vector3(2, pieceYOffset, 7), PieceMaterials[1], "Bishop", false);
        InstantiatePiece(DwarfPiecePrefabs[3], new Vector3(5, pieceYOffset, 7), PieceMaterials[1], "Bishop", false);

        // Queen (后)
        InstantiatePiece(ElfPiecePrefabs[4], new Vector3(3, pieceYOffset, 0), PieceMaterials[0], "Queen", true);
        InstantiatePiece(DwarfPiecePrefabs[4], new Vector3(3, pieceYOffset, 7), PieceMaterials[1], "Queen", false);

        // King (王)
        InstantiatePiece(ElfPiecePrefabs[5], new Vector3(4, pieceYOffset, 0), PieceMaterials[0], "King", true);
        InstantiatePiece(DwarfPiecePrefabs[5], new Vector3(4, pieceYOffset, 7), PieceMaterials[1], "King", false);

        Debug.Log("棋盘摆放完成！");
    }

    public void InstantiatePiece(GameObject piecePrefab, Vector3 position, Material material, string pieceType, bool isWhite)
    {
        Quaternion rotation = isWhite ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);

        GameObject pieceObject = Instantiate(piecePrefab, position, rotation);
        pieceObject.transform.parent = this.transform;
        pieceObject.transform.localScale = Vector3.one * pieceScale;

        Renderer renderer = pieceObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = material;
        }

        Piece piece = pieceObject.GetComponent<Piece>();
        if (piece != null)
        {
            piece.Initialize(pieceType, isWhite);
        }
    }
}
*/
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

    [Header("棋子缩放设置")]
    public float pieceScale = 0.1f;

    [Header("棋子高度设置")]
    public float pieceYOffset = 0.5f; // ✅ 棋子 Y 轴偏移（调整贴合度）
    public float pawnYOffset = 0.4f;  // ✅ Pawn 的 Y 轴偏移

    void Start()
    {
        if (logicManager != null)
        {
            // 1. 首先初始化逻辑管理器，确保内部数组（如 squares）已准备好
            logicManager.Initialize();

            // 2. 然后生成棋盘，此时可以安全地访问 logicManager.squares
            GenerateBoard();

            // 3. 最后放置棋子
            PlaceStartingPosition();
        }
        else
        {
            Debug.LogError("LogicManager 未分配！请在Unity编辑器中将LogicManager对象拖拽到Board脚本的对应字段上。");
        }
    }

    public void GenerateBoard()
    {
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
                GameObject squareObject = Instantiate(Square, new Vector3(i, 0, j), Quaternion.identity);
                squareObject.transform.parent = this.transform;
                Square square = squareObject.GetComponent<Square>();

                // 这行代码现在可以安全执行了
                logicManager.squares[i, j] = square;

                Renderer renderer = square.GetComponent<Renderer>();

                if ((i + j) % 2 == 0)
                {
                    renderer.material = blackSquareMaterial;
                }
                else
                {
                    renderer.material = whiteSquareMaterial;
                }
            }
        }
    }

    public void PlaceStartingPosition()
    {
        if (ElfPiecePrefabs.Length < 6)
        {
            Debug.LogError($"Elf Prefab 数量不足！需要 6 个，当前只有 {ElfPiecePrefabs.Length} 个");
            return;
        }
        if (DwarfPiecePrefabs.Length < 6)
        {
            Debug.LogError($"Dwarf Prefab 数量不足！需要 6 个，当前只有 {DwarfPiecePrefabs.Length} 个");
            return;
        }
        if (PieceMaterials.Length < 2)
        {
            Debug.LogError($"材质数量不足！需要 2 个，当前只有 {PieceMaterials.Length} 个");
            return;
        }

        // --- 放置白方阵营 (Elf) ---
        // Pawns
        for (int i = 0; i < Width; i++)
        {
            InstantiatePiece(ElfPiecePrefabs[0], new Vector3(i, pawnYOffset, 1), PieceMaterials[0], "Pawn", true, Faction.Elf);
        }
        // Rooks
        InstantiatePiece(ElfPiecePrefabs[1], new Vector3(0, pieceYOffset, 0), PieceMaterials[0], "Rook", true, Faction.Elf);
        InstantiatePiece(ElfPiecePrefabs[1], new Vector3(7, pieceYOffset, 0), PieceMaterials[0], "Rook", true, Faction.Elf);
        // Knights
        InstantiatePiece(ElfPiecePrefabs[2], new Vector3(1, pieceYOffset, 0), PieceMaterials[0], "Knight", true, Faction.Elf);
        InstantiatePiece(ElfPiecePrefabs[2], new Vector3(6, pieceYOffset, 0), PieceMaterials[0], "Knight", true, Faction.Elf);
        // Bishops
        InstantiatePiece(ElfPiecePrefabs[3], new Vector3(2, pieceYOffset, 0), PieceMaterials[0], "Bishop", true, Faction.Elf);
        InstantiatePiece(ElfPiecePrefabs[3], new Vector3(5, pieceYOffset, 0), PieceMaterials[0], "Bishop", true, Faction.Elf);
        // Queen
        InstantiatePiece(ElfPiecePrefabs[4], new Vector3(3, pieceYOffset, 0), PieceMaterials[0], "Queen", true, Faction.Elf);
        // King
        InstantiatePiece(ElfPiecePrefabs[5], new Vector3(4, pieceYOffset, 0), PieceMaterials[0], "King", true, Faction.Elf);


        // --- 放置黑方阵营 (Dwarf) ---
        // Pawns
        for (int i = 0; i < Width; i++)
        {
            InstantiatePiece(DwarfPiecePrefabs[0], new Vector3(i, pawnYOffset, 6), PieceMaterials[1], "Pawn", false, Faction.Dwarf);
        }
        // Rooks
        InstantiatePiece(DwarfPiecePrefabs[1], new Vector3(0, pieceYOffset, 7), PieceMaterials[1], "Rook", false, Faction.Dwarf);
        InstantiatePiece(DwarfPiecePrefabs[1], new Vector3(7, pieceYOffset, 7), PieceMaterials[1], "Rook", false, Faction.Dwarf);
        // Knights
        InstantiatePiece(DwarfPiecePrefabs[2], new Vector3(1, pieceYOffset, 7), PieceMaterials[1], "Knight", false, Faction.Dwarf);
        InstantiatePiece(DwarfPiecePrefabs[2], new Vector3(6, pieceYOffset, 7), PieceMaterials[1], "Knight", false, Faction.Dwarf);
        // Bishops
        InstantiatePiece(DwarfPiecePrefabs[3], new Vector3(2, pieceYOffset, 7), PieceMaterials[1], "Bishop", false, Faction.Dwarf);
        InstantiatePiece(DwarfPiecePrefabs[3], new Vector3(5, pieceYOffset, 7), PieceMaterials[1], "Bishop", false, Faction.Dwarf);
        // Queen
        InstantiatePiece(DwarfPiecePrefabs[4], new Vector3(3, pieceYOffset, 7), PieceMaterials[1], "Queen", false, Faction.Dwarf);
        // King
        InstantiatePiece(DwarfPiecePrefabs[5], new Vector3(4, pieceYOffset, 7), PieceMaterials[1], "King", false, Faction.Dwarf);


        Debug.Log("棋盘摆放完成！");
    }

    // ▼▼▼ 修改 InstantiatePiece 方法以接收 Faction ▼▼▼
    public void InstantiatePiece(GameObject piecePrefab, Vector3 position, Material material, string pieceType, bool isWhite, Faction faction)
    {
        // 黑方棋子旋转 180 度
        Quaternion rotation = isWhite ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);

        GameObject pieceObject = Instantiate(piecePrefab, position, rotation);
        pieceObject.transform.parent = this.transform;
        pieceObject.transform.localScale = Vector3.one * pieceScale;

        Renderer renderer = pieceObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = material;
        }

        Piece piece = pieceObject.GetComponent<Piece>();
        if (piece != null)
        {
            // 传递阵营信息
            piece.Initialize(pieceType, isWhite, faction);
        }
    }
    // ▲▲▲
}