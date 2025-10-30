using System.Collections.Generic;
using UnityEngine;

namespace ChessMiniDemo
{
    public class King : Piece
    {
        private static readonly int[] dx = { -1, 0, 1, -1, 1, -1, 0, 1 };
        private static readonly int[] dy = { -1, -1, -1, 0, 0, 1, 1, 1 };

        protected override List<Vector2> GetPotentialMoves()
        {
            List<Vector2> moves = new List<Vector2>();
            Vector2 c = GetCoordinates();

            for (int i = 0; i < 8; i++)
            {
                Vector2 np = new Vector2(c.x + dx[i], c.y + dy[i]);
                if (!IsPositionWithinBoard(np)) continue;

                Piece p = logicManager.boardMap[(int)np.x, (int)np.y];
                if (p == null || p.IsWhite != IsWhite)
                {
                    moves.Add(np);
                }
            }
            return moves;
        }

        public override List<Vector2> GetAttackedFields()
        {
            List<Vector2> fields = new List<Vector2>();
            Vector2 c = GetCoordinates();
            for (int i = 0; i < 8; i++)
            {
                Vector2 np = new Vector2(c.x + dx[i], c.y + dy[i]);
                if (np.x >= 0 && np.x < 8 && np.y >= 0 && np.y < 8)
                {
                    fields.Add(np);
                }
            }
            return fields;
        }
    }
}
