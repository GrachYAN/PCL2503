// 在 Board.cs 的初始化布子函数中（例如 SetupPieces 或 InitializeBoard）
void PlaceBishops()
{
    // 以 PiecePrefabs 中的 Bishop 索引为例：BishopIndex
    int bishopIndex = GetPieceIndexByName("Bishop"); // 如果你没有这个方法，直接用常量索引

    // 白方
    SpawnPiece(bishopIndex, true, 2, 0); // c1
    SpawnPiece(bishopIndex, true, 5, 0); // f1

    // 黑方
    SpawnPiece(bishopIndex, false, 2, 7); // c8
    SpawnPiece(bishopIndex, false, 5, 7); // f8
}

// 通用生成函数（若已有，请复用）
void SpawnPiece(int prefabIndex, bool isWhite, int x, int y)
{
    var prefab = PiecePrefabs[prefabIndex];
    var go = Instantiate(prefab, squares[x, y].transform.position, Quaternion.identity, transform);
    var piece = go.GetComponent<Piece>();
    piece.Initialize(prefab.name, isWhite);

    logicManager.boardMap[x, y] = piece;
    if (logicManager.piecesOnBoard != null) logicManager.piecesOnBoard.Add(piece);
}
