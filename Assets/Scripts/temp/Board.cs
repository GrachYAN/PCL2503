using UnityEngine;

public class Board : MonoBehaviour
{
    public GameObject Square;
    public GameObject[] PiecePrefabs;
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
    public float pieceYOffset = 0.5f;      // ✅ 棋子 Y 轴偏移（调整贴合度）
    public float pawnYOffset = 0.4f;       // ✅ Pawn 的 Y 轴偏移

    void Start()
    {
        GenerateBoard();
        if (logicManager != null)
        {
            logicManager.Initialize();
            PlaceStartingPosition();
        }
        else
        {
            Debug.LogError("LogicManager 未分配！");
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
        if (PiecePrefabs.Length < 6)
        {
            Debug.LogError($"Prefab 数量不足！需要 6 个，当前只有 {PiecePrefabs.Length} 个");
            return;
        }

        if (PieceMaterials.Length < 2)
        {
            Debug.LogError($"材质数量不足！需要 2 个，当前只有 {PieceMaterials.Length} 个");
            return;
        }

        // ✅ Pawn (兵) - 白方朝上，黑方朝下
        for (int i = 0; i < Width; i++)
        {
            InstantiatePiece(PiecePrefabs[0], new Vector3(i, pawnYOffset, 1), PieceMaterials[0], "Pawn", true);
            InstantiatePiece(PiecePrefabs[0], new Vector3(i, pawnYOffset, 6), PieceMaterials[1], "Pawn", false);
        }

        // ✅ Rook (车)
        InstantiatePiece(PiecePrefabs[1], new Vector3(0, pieceYOffset, 0), PieceMaterials[0], "Rook", true);
        InstantiatePiece(PiecePrefabs[1], new Vector3(7, pieceYOffset, 0), PieceMaterials[0], "Rook", true);
        InstantiatePiece(PiecePrefabs[1], new Vector3(0, pieceYOffset, 7), PieceMaterials[1], "Rook", false);
        InstantiatePiece(PiecePrefabs[1], new Vector3(7, pieceYOffset, 7), PieceMaterials[1], "Rook", false);

        // ✅ Knight (马)
        InstantiatePiece(PiecePrefabs[2], new Vector3(1, pieceYOffset, 0), PieceMaterials[0], "Knight", true);
        InstantiatePiece(PiecePrefabs[2], new Vector3(6, pieceYOffset, 0), PieceMaterials[0], "Knight", true);
        InstantiatePiece(PiecePrefabs[2], new Vector3(1, pieceYOffset, 7), PieceMaterials[1], "Knight", false);
        InstantiatePiece(PiecePrefabs[2], new Vector3(6, pieceYOffset, 7), PieceMaterials[1], "Knight", false);

        // ✅ Bishop (象)
        InstantiatePiece(PiecePrefabs[3], new Vector3(2, pieceYOffset, 0), PieceMaterials[0], "Bishop", true);
        InstantiatePiece(PiecePrefabs[3], new Vector3(5, pieceYOffset, 0), PieceMaterials[0], "Bishop", true);
        InstantiatePiece(PiecePrefabs[3], new Vector3(2, pieceYOffset, 7), PieceMaterials[1], "Bishop", false);
        InstantiatePiece(PiecePrefabs[3], new Vector3(5, pieceYOffset, 7), PieceMaterials[1], "Bishop", false);

        // ✅ Queen (后)
        InstantiatePiece(PiecePrefabs[4], new Vector3(3, pieceYOffset, 0), PieceMaterials[0], "Queen", true);
        InstantiatePiece(PiecePrefabs[4], new Vector3(3, pieceYOffset, 7), PieceMaterials[1], "Queen", false);

        // ✅ King (王)
        InstantiatePiece(PiecePrefabs[5], new Vector3(4, pieceYOffset, 0), PieceMaterials[0], "King", true);
        InstantiatePiece(PiecePrefabs[5], new Vector3(4, pieceYOffset, 7), PieceMaterials[1], "King", false);

        Debug.Log("棋盘摆放完成！");
    }

    // ✅ 修改方法：添加旋转逻辑
    public void InstantiatePiece(GameObject piecePrefab, Vector3 position, Material material, string pieceType, bool isWhite)
    {
        // ✅ 黑方棋子旋转 180 度
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
