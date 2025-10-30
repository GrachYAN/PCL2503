// 在 LogicManager.cs 的 namespace ChessMiniDemo 内部、class LogicManager 中增补
public Piece lastMovedPiece;

public void MovePiece(Piece piece, Vector2 from, Vector2 to)
{
    int fx = (int)from.x; int fy = (int)from.y;
    int tx = (int)to.x; int ty = (int)to.y;

    // 更新数据表
    boardMap[fx, fy] = null;
    boardMap[tx, ty] = piece;

    // 同步世界坐标
    Square dest = squares[tx, ty];
    if (dest != null)
        piece.transform.position = dest.transform.position;

    // 状态
    piece.IncrementMoved();
    lastMovedPiece = piece;
    isWhiteTurn = !isWhiteTurn;

    UpdateCheckMaps();
}

public void CapturePiece(Piece target)
{
    if (target == null) return;
    // 从 piecesOnBoard 中移除
    if (piecesOnBoard != null) piecesOnBoard.Remove(target);

    // 从映射中清理（谨慎：调用方 MovePiece 会覆盖目标格）
    // 这里只负责销毁与列表清理
    Destroy(target.gameObject);
}

public void UpdateCheckMaps()
{
    // 简版：清空并重算被攻击格（若你已有实现，保留原实现）
    System.Array.Clear(whiteCheckMap, 0, whiteCheckMap.Length);
    System.Array.Clear(blackCheckMap, 0, blackCheckMap.Length);

    if (piecesOnBoard == null) return;

    foreach (var p in piecesOnBoard)
    {
        if (p == null) continue;

        // 需要 Piece 能提供当前位置（通常通过遍历 boardMap 找到，或在 Piece 中缓存坐标）
        Vector2 pos = GetPiecePosition(p);
        var moves = p.GetLegalMoves(pos);

        bool isWhite = p.IsWhite;
        foreach (var m in moves)
        {
            int x = (int)m.x; int y = (int)m.y;
            if (x < 0 || x >= 8 || y < 0 || y >= 8) continue;
            if (isWhite) whiteCheckMap[x, y] = true;
            else blackCheckMap[x, y] = true;
        }
    }
}

// 工具：从 boardMap 查找棋子当前位置
public Vector2 GetPiecePosition(Piece piece)
{
    for (int x = 0; x < 8; x++)
        for (int y = 0; y < 8; y++)
            if (boardMap[x, y] == piece)
                return new Vector2(x, y);
    return new Vector2(-1, -1);
}

// 可选：过滤会让己方王暴露在将军下的走法（如果已存在同名方法，请忽略或合并）
public List<Vector2> FilterMovesThatLeaveKingInCheck(Piece piece, Vector2 from, List<Vector2> candidateMoves)
{
    List<Vector2> filtered = new List<Vector2>(candidateMoves);

    // 寻找己方王
    Piece myKing = null;
    foreach (var p in piecesOnBoard)
    {
        if (p != null && p.IsWhite == piece.IsWhite && p.PieceType == "King")
        {
            myKing = p;
            break;
        }
    }
    if (myKing == null) return filtered;

    for (int i = filtered.Count - 1; i >= 0; i--)
    {
        Vector2 to = filtered[i];

        // 备份现场
        Piece captured = boardMap[(int)to.x, (int)to.y];
        Piece[,] backup = (Piece[,])boardMap.Clone();

        // 模拟移动
        boardMap[(int)from.x, (int)from.y] = null;
        boardMap[(int)to.x, (int)to.y] = piece;

        // 计算王是否在被攻击
        bool inCheck = IsKingInCheck(myKing);

        // 回滚
        boardMap = backup;

        if (inCheck) filtered.RemoveAt(i);
    }

    return filtered;
}

// 用当前 boardMap 判断指定 king 是否被将军（简化：遍历敌方棋子步伐）
public bool IsKingInCheck(Piece myKing)
{
    Vector2 kingPos = GetPiecePosition(myKing);
    if (kingPos.x < 0) return false;

    foreach (var p in piecesOnBoard)
    {
        if (p == null || p.IsWhite == myKing.IsWhite) continue;

        var enemyMoves = p.GetLegalMoves(GetPiecePosition(p));
        for (int i = 0; i < enemyMoves.Count; i++)
        {
            if ((int)enemyMoves[i].x == (int)kingPos.x && (int)enemyMoves[i].y == (int)kingPos.y)
                return true;
        }
    }
    return false;
}
