using System.Collections.Generic;
using UnityEngine;

public class BattleRally : Spell
{
    private const int MaxSelections = 3;
    private readonly List<Vector2Int> selectedPieces = new List<Vector2Int>();

    public BattleRally()
    {
        SpellName = "Battle Rally";
        Description = "Up to three Pieces within range 2 each move 1 forward (legal, free).";
        ManaCost = 4;
        Cooldown = 0;
    }

    public override void BeginTargeting()
    {
        selectedPieces.Clear();
    }

    public override void CancelTargeting()
    {
        // 为已选择的棋子播放落下动画
        if (LogicManager != null)
        {
            foreach (Vector2Int piecePos in selectedPieces)
            {
                if (Caster != null && Caster.IsPositionWithinBoard(piecePos))
                {
                    Piece piece = LogicManager.boardMap[piecePos.x, piecePos.y];
                    if (piece != null)
                    {
                        piece.MotionAnimator.PlayCancelDropAnimation();
                    }
                }
            }
        }
        selectedPieces.Clear();
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        return ConvertToVector2(GetSelectablePieces());
    }

    public override List<Vector2> GetCurrentValidSquares()
    {
        List<Vector2> targets = new List<Vector2>();
        List<Vector2Int> candidates = GetSelectablePieces();

        foreach (Vector2Int piece in candidates)
        {
            if (!selectedPieces.Contains(piece))
            {
                targets.Add(new Vector2(piece.x, piece.y));
            }
        }

        if (selectedPieces.Count > 0 && selectedPieces.Count < MaxSelections)
        {
            targets.Add(Caster.GetCoordinates());
        }

        return targets;
    }

    public override bool TryHandleTargetSelection(Vector2 targetSquare, out bool castComplete)
    {
        castComplete = false;
        if (Caster == null || LogicManager == null)
        {
            return false;
        }

        Vector2Int gridTarget = Vector2Int.RoundToInt(targetSquare);
        Vector2Int casterPos = Vector2Int.RoundToInt(Caster.GetCoordinates());

        if (gridTarget == casterPos)
        {
            if (selectedPieces.Count == 0)
            {
                return false;
            }

            castComplete = true;
            return true;
        }

        List<Vector2Int> candidates = GetSelectablePieces();
        if (!candidates.Contains(gridTarget) || selectedPieces.Contains(gridTarget))
        {
            return false;
        }

        // 为选中的友军播放升起动画
        Piece targetPiece = LogicManager.boardMap[gridTarget.x, gridTarget.y];
        if (targetPiece != null)
        {
            targetPiece.MotionAnimator.PlayLiftAnimation();
        }

        selectedPieces.Add(gridTarget);

        if (selectedPieces.Count >= MaxSelections || selectedPieces.Count == candidates.Count)
        {
            castComplete = true;
        }

        return true;
    }

    public override SpellCastData GetCastData(Vector2 finalTarget)
    {
        Vector2Int first = GetSelectionOrInvalid(0);
        Vector2Int second = GetSelectionOrInvalid(1);
        Vector2Int third = GetSelectionOrInvalid(2);

        return new SpellCastData
        {
            PrimaryX = first.x,
            PrimaryY = first.y,
            SecondaryX = second.x,
            SecondaryY = second.y,
            TertiaryX = third.x,
            TertiaryY = third.y
        };
    }

    public override void ApplyCastData(SpellCastData data)
    {
        selectedPieces.Clear();
        TryAddSelection(data.PrimaryX, data.PrimaryY);
        TryAddSelection(data.SecondaryX, data.SecondaryY);
        TryAddSelection(data.TertiaryX, data.TertiaryY);
    }

    public override bool IsCastDataValid(SpellCastData data)
    {
        if (Caster == null || LogicManager == null)
        {
            return false;
        }

        List<Vector2Int> candidates = GetSelectablePieces();
        HashSet<Vector2Int> seen = new HashSet<Vector2Int>();
        bool hasValidSelection = false;

        foreach (Vector2Int selection in EnumerateSelections(data))
        {
            if (selection.x < 0 || selection.y < 0)
            {
                continue;
            }

            if (!candidates.Contains(selection) || !seen.Add(selection))
            {
                return false;
            }

            hasValidSelection = true;
        }

        return hasValidSelection;
    }

    protected override void ExecuteEffect(Vector2 targetSquare)
    {
        if (Caster == null || LogicManager == null)
        {
            return;
        }

        // 计算所有需要移动的棋子
        List<Piece> movingPieces = new List<Piece>();
        List<Vector2> destinations = new List<Vector2>();

        foreach (Vector2Int piecePos in selectedPieces)
        {
            if (!Caster.IsPositionWithinBoard(piecePos))
            {
                continue;
            }

            Piece piece = LogicManager.boardMap[piecePos.x, piecePos.y];
            if (piece == null || piece.IsWhite != Caster.IsWhite)
            {
                continue;
            }

            int direction = piece.IsWhite ? 1 : -1;
            Vector2Int destination = piecePos + new Vector2Int(0, direction);
            if (!Caster.IsPositionWithinBoard(destination))
            {
                continue;
            }

            if (LogicManager.boardMap[destination.x, destination.y] != null)
            {
                continue;
            }

            movingPieces.Add(piece);
            destinations.Add(new Vector2(destination.x, destination.y));
        }

        // 执行移动动画，并在所有棋子完成移动后让 Knight 落下
        if (movingPieces.Count > 0)
        {
            // 直接启动协程，在协程中处理 Knight 的升起和落下
            Caster.StartCoroutine(ExecuteMovementWithDelay(movingPieces, destinations));
        }
        else
        {
            // 如果没有棋子需要移动，Knight 直接落下
            Caster.MotionAnimator.PlayCancelDropAnimation();
        }

        selectedPieces.Clear();
    }

    private System.Collections.IEnumerator ExecuteMovementWithDelay(List<Piece> pieces, List<Vector2> destinations)
    {
        if (pieces.Count == 0)
        {
            // 如果没有棋子需要移动，Knight 直接落下
            Caster.MotionAnimator.PlayCancelDropAnimation();
            yield break;
        }

        // Knight 释放技能后升起并保持悬浮
        bool knightLiftComplete = false;
        Caster.MotionAnimator.PlayLiftAnimation(() => knightLiftComplete = true);
        
        // 等待 Knight 完成升起动画
        while (!knightLiftComplete)
        {
            yield return null;
        }

        // 按选择顺序依次执行每个棋子的移动
        for (int i = 0; i < pieces.Count; i++)
        {
            Piece piece = pieces[i];
            Vector2 destination = destinations[i];

            // 等待友军完成升起动画（选中时升起的动画）
            while (piece.MotionAnimator.CurrentState == PieceAnimationState.Lifting)
            {
                yield return null;
            }

            // 等待当前棋子的移动动画完成（移动+落下）
            bool moveComplete = false;
            System.Action<Vector3> onMoveComplete = (pos) => moveComplete = true;
            piece.MotionAnimator.OnMoveComplete += onMoveComplete;
            
            // 开始移动
            piece.Move(destination, true);
            
            // 等待移动完成（包括落下）
            while (!moveComplete)
            {
                yield return null;
            }
            
            // 移除回调
            piece.MotionAnimator.OnMoveComplete -= onMoveComplete;
        }

        // 确保所有棋子都已完成移动和落下
        yield return new WaitForSeconds(0.1f);

        // Knight 最后落下
        Caster.MotionAnimator.PlayCancelDropAnimation();
    }

    private List<Vector2Int> GetSelectablePieces()
    {
        List<Vector2Int> pieces = new List<Vector2Int>();
        if (Caster == null || LogicManager == null)
        {
            return pieces;
        }

        Vector2Int origin = Vector2Int.RoundToInt(Caster.GetCoordinates());

        for (int dx = -2; dx <= 2; dx++)
        {
            for (int dy = -2; dy <= 2; dy++)
            {
                Vector2Int pos = origin + new Vector2Int(dx, dy);
                if (!Caster.IsPositionWithinBoard(pos))
                {
                    continue;
                }

                Piece piece = LogicManager.boardMap[pos.x, pos.y];
                if (piece == null || piece.IsWhite != Caster.IsWhite)
                {
                    continue;
                }

                // Check if this piece can move 1 forward
                int direction = piece.IsWhite ? 1 : -1;
                Vector2Int destination = pos + new Vector2Int(0, direction);
                if (!Caster.IsPositionWithinBoard(destination))
                {
                    continue;
                }

                if (LogicManager.boardMap[destination.x, destination.y] != null)
                {
                    continue;
                }

                pieces.Add(pos);
            }
        }

        return pieces;
    }

    private Vector2Int GetSelectionOrInvalid(int index)
    {
        if (index < selectedPieces.Count)
        {
            return selectedPieces[index];
        }

        return new Vector2Int(-1, -1);
    }

    private void TryAddSelection(int x, int y)
    {
        if (x < 0 || y < 0)
        {
            return;
        }

        Vector2Int pos = new Vector2Int(x, y);
        if (!selectedPieces.Contains(pos))
        {
            selectedPieces.Add(pos);
        }
    }

    private IEnumerable<Vector2Int> EnumerateSelections(SpellCastData data)
    {
        yield return new Vector2Int(data.PrimaryX, data.PrimaryY);
        yield return new Vector2Int(data.SecondaryX, data.SecondaryY);
        yield return new Vector2Int(data.TertiaryX, data.TertiaryY);
    }

    private List<Vector2> ConvertToVector2(List<Vector2Int> ints)
    {
        List<Vector2> values = new List<Vector2>();
        foreach (Vector2Int value in ints)
        {
            values.Add(new Vector2(value.x, value.y));
        }

        return values;
    }
}