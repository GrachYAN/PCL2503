using System.Collections.Generic;
using UnityEngine;

namespace ChessMiniDemo
{
    public class Bishop : Piece
    {
        // 斜线四个方向的增量
        private static readonly int[] dx = { 1, 1, -1, -1 };
        private static readonly int[] dy = { 1, -1, 1, -1 };

        // 获取主教的合法走法：沿四个斜线方向射线前进，直到被阻挡
        public override List<Vector2> GetLegalMoves(Vector2 from)
        {
            List<Vector2> legalMoves = new List<Vector2>();

            int sx = (int)from.x;
            int sy = (int)from.y;

            // 防御性边界
            if (!InBounds(sx, sy)) return legalMoves;

            for (int dir = 0; dir < 4; dir++)
            {
                int x = sx + dx[dir];
                int y = sy + dy[dir];

                // 沿方向持续推进，直到越界或遇到阻挡
                while (InBounds(x, y))
                {
                    Piece target = logicManager.boardMap[x, y];

                    if (target == null)
                    {
                        // 空格可走
                        legalMoves.Add(new Vector2(x, y));
                    }
                    else
                    {
                        // 有棋子：若是敌方则可吃，随后停止该方向继续
                        if (target.IsWhite != this.IsWhite)
                            legalMoves.Add(new Vector2(x, y));
                        break;
                    }

                    x += dx[dir];
                    y += dy[dir];
                }
            }

            // 如果你项目里需要过滤“走后自家王被将军”的不合法步，可在此调用逻辑管理器的校验
            // 例如：legalMoves = logicManager.FilterMovesThatLeaveKingInCheck(this, from, legalMoves);
            // 具体实现看你 LogicManager 是否有类似方法

            return legalMoves;
        }

        // 实际落子与吃子（如你的框架已有 TryMove，可复用基类或照 King 的风格）
        public override bool TryMove(Vector2 from, Vector2 to)
        {
            // 基本合法性：是否在列表中
            var legal = GetLegalMoves(from);
            bool found = false;
            for (int i = 0; i < legal.Count; i++)
            {
                if ((int)legal[i].x == (int)to.x && (int)legal[i].y == (int)to.y)
                {
                    found = true;
                    break;
                }
            }
            if (!found) return false;

            int fx = (int)from.x;
            int fy = (int)from.y;
            int tx = (int)to.x;
            int ty = (int)to.y;

            // 处理吃子
            Piece target = logicManager.boardMap[tx, ty];
            if (target != null && target.IsWhite != this.IsWhite)
            {
                // 从场景与数据结构移除敌方棋子
                logicManager.piecesOnBoard.Remove(target);
                Destroy(target.gameObject);
            }

            // 更新棋盘映射
            logicManager.boardMap[fx, fy] = null;
            logicManager.boardMap[tx, ty] = this;

            // 移动物体到目标格的世界位置
            Square destSquare = logicManager.squares[tx, ty];
            if (destSquare != null)
            {
                transform.position = destSquare.transform.position;
            }

            // 标记移动次数、记录最后移动者、切换行棋方
            IncrementMoved();
            logicManager.lastMovedPiece = this;
            logicManager.isWhiteTurn = !logicManager.isWhiteTurn;

            // 如果你有将军地图或状态更新，在此通知逻辑管理器刷新
            // logicManager.UpdateCheckMaps();

            return true;
        }

        // 可选：初始化棋子类型与颜色（与 Piece.Initialize 一致）
        public override void Initialize(string pieceType, bool isWhite)
        {
            base.Initialize("Bishop", isWhite);
            // 如需特定材质或外观，可在此设置
            // e.g. SetMaterialByColor();
        }

        // 工具：边界判断（与项目中常用逻辑保持一致）
        private bool InBounds(int x, int y)
        {
            return x >= 0 && x < 8 && y >= 0 && y < 8;
        }
    }
}
