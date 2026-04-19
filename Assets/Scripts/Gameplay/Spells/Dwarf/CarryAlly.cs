using System.Collections.Generic;
using UnityEngine;

public class CarryAlly : Spell
{
    private static readonly Vector2Int[] AdjacentOffsets =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
    };

    private const float SpiralLiftHeight = 2f;
    private const float SpiralHorizontalRadius = 0.2f;
    private const float SpiralDuration = 1.05f;
    private const float SettleDuration = 0.28f;

    private Vector2Int? pendingAlly;
    private Vector2Int? pendingDestination;
    private Vector2Int? pendingDrop;

    public override bool HandlesCasterDeselectionAnimation => true;

    public CarryAlly()
    {
        SpellName = "Carry Ally";
        Description = "Pick an adjacent ally, move up to 3 in a straight line, then drop them on an adjacent square.";
        ManaCost = 5;
        Cooldown = 0;
    }

    public override void BeginTargeting()
    {
        pendingAlly = null;
        pendingDestination = null;
        pendingDrop = null;
    }

    public override void CancelTargeting()
    {
        if (pendingAlly.HasValue && LogicManager != null)
        {
            Piece allyPiece = LogicManager.boardMap[pendingAlly.Value.x, pendingAlly.Value.y];
            if (allyPiece != null)
            {
                allyPiece.MotionAnimator.PlayCancelDropAnimation();
            }
        }

        pendingAlly = null;
        pendingDestination = null;
        pendingDrop = null;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        List<Vector2> drops = new List<Vector2>();
        foreach (Vector2Int ally in GetAdjacentAllies())
        {
            foreach (Vector2Int dest in GetDestinationsForAlly(ally))
            {
                foreach (Vector2Int drop in GetDropSquares(dest))
                {
                    Vector2 dropVec = new Vector2(drop.x, drop.y);
                    if (!drops.Contains(dropVec))
                    {
                        drops.Add(dropVec);
                    }
                }
            }
        }

        return drops;
    }

    public override List<Vector2> GetCurrentValidSquares()
    {
        if (!pendingAlly.HasValue)
        {
            return ConvertToVector2(GetAdjacentAllies());
        }

        if (!pendingDestination.HasValue)
        {
            return ConvertToVector2(GetDestinationsForAlly(pendingAlly.Value));
        }

        return ConvertToVector2(GetDropSquares(pendingDestination.Value));
    }

    public override bool TryHandleTargetSelection(Vector2 targetSquare, out bool castComplete)
    {
        castComplete = false;
        Vector2Int gridTarget = Vector2Int.RoundToInt(targetSquare);

        if (!pendingAlly.HasValue)
        {
            if (!GetAdjacentAllies().Contains(gridTarget))
            {
                return false;
            }

            if (LogicManager != null)
            {
                Piece allyPiece = LogicManager.boardMap[gridTarget.x, gridTarget.y];
                if (allyPiece != null)
                {
                    allyPiece.MotionAnimator.PlayLiftAnimation();
                }
            }

            pendingAlly = gridTarget;
            return true;
        }

        if (!pendingDestination.HasValue)
        {
            if (!GetDestinationsForAlly(pendingAlly.Value).Contains(gridTarget))
            {
                return false;
            }

            pendingDestination = gridTarget;
            return true;
        }

        if (!GetDropSquares(pendingDestination.Value).Contains(gridTarget))
        {
            return false;
        }

        pendingDrop = gridTarget;
        castComplete = true;
        return true;
    }

    public override SpellCastData GetCastData(Vector2 finalTarget)
    {
        return new SpellCastData
        {
            PrimaryX = pendingDrop.HasValue ? pendingDrop.Value.x : Mathf.RoundToInt(finalTarget.x),
            PrimaryY = pendingDrop.HasValue ? pendingDrop.Value.y : Mathf.RoundToInt(finalTarget.y),
            SecondaryX = pendingDestination.HasValue ? pendingDestination.Value.x : -1,
            SecondaryY = pendingDestination.HasValue ? pendingDestination.Value.y : -1,
            TertiaryX = pendingAlly.HasValue ? pendingAlly.Value.x : -1,
            TertiaryY = pendingAlly.HasValue ? pendingAlly.Value.y : -1
        };
    }

    public override void ApplyCastData(SpellCastData data)
    {
        Vector2Int ally = data.GetTertiary();
        Vector2Int dest = data.GetSecondary();
        Vector2Int drop = data.GetPrimary();

        pendingAlly = ally.x >= 0 ? ally : (Vector2Int?)null;
        pendingDestination = dest.x >= 0 ? dest : (Vector2Int?)null;
        pendingDrop = drop.x >= 0 ? drop : (Vector2Int?)null;
    }

    public override bool IsCastDataValid(SpellCastData data)
    {
        if (Caster == null || LogicManager == null)
        {
            return false;
        }

        Vector2Int ally = data.GetTertiary();
        Vector2Int dest = data.GetSecondary();
        Vector2Int drop = data.GetPrimary();

        if (ally.x < 0 || dest.x < 0 || drop.x < 0)
        {
            return false;
        }

        if (!GetAdjacentAllies().Contains(ally))
        {
            return false;
        }

        if (!GetDestinationsForAlly(ally).Contains(dest))
        {
            return false;
        }

        if (!GetDropSquares(dest).Contains(drop))
        {
            return false;
        }

        return true;
    }

    public override void Cast(Vector2 targetSquare)
    {
        if (!CanCast())
        {
            return;
        }

        int manaCost = GetEffectiveManaCost();
        int cooldown = GetEffectiveCooldown();

        if (!Caster.UseMana(manaCost))
        {
            return;
        }

        CurrentCooldown = cooldown;

        if (GameSoundManager.Instance != null)
        {
            GameSoundManager.Instance.BeginSpellExecution();
        }

        if (SpellVFXManager.Instance != null)
        {
            SpellVFXManager.Instance.PlaySpellVFX(this, Caster, LogicManager, targetSquare);
        }

        if (Caster != null && !Caster.MotionAnimator.IsLifted)
        {
            Caster.MotionAnimator.PlayLiftAnimation(() => ExecuteEffect(targetSquare));
        }
        else
        {
            ExecuteEffect(targetSquare);
        }

        if (GameSoundManager.Instance != null)
        {
            GameSoundManager.Instance.EndSpellExecution();
        }

        Caster.OnSpellCast();
    }

    protected override void ExecuteEffect(Vector2 targetSquare)
    {
        if (Caster == null || LogicManager == null || !pendingAlly.HasValue || !pendingDestination.HasValue || !pendingDrop.HasValue)
        {
            return;
        }

        Vector2Int allyPos = pendingAlly.Value;
        Vector2Int destPos = pendingDestination.Value;
        Vector2Int dropPos = pendingDrop.Value;

        Piece ally = LogicManager.boardMap[allyPos.x, allyPos.y];
        if (ally == null || ally.IsWhite != Caster.IsWhite)
        {
            pendingAlly = null;
            pendingDestination = null;
            pendingDrop = null;
            return;
        }

        Caster.StartCoroutine(ExecuteCarryAllyAnimation(ally, destPos, dropPos));

        pendingAlly = null;
        pendingDestination = null;
        pendingDrop = null;
    }

    private System.Collections.IEnumerator ExecuteCarryAllyAnimation(Piece ally, Vector2Int destPos, Vector2Int dropPos)
    {
        PieceMotionAnimator casterAnimator = Caster.MotionAnimator;
        float casterGroundY = casterAnimator.GroundY;
        Quaternion casterGroundRotation = casterAnimator.GroundRotation;

        if (!casterAnimator.IsLifted)
        {
            bool gryphonLiftComplete = false;
            casterAnimator.PlayLiftAnimation(() => gryphonLiftComplete = true);
            while (!gryphonLiftComplete)
            {
                yield return null;
            }
        }

        while (ally.MotionAnimator.CurrentState == PieceAnimationState.Lifting)
        {
            yield return null;
        }

        yield return Caster.StartCoroutine(PlayGryphonLiftOffAnimation());

        bool allyMoveComplete = false;
        System.Action<Vector3> onAllyMoveComplete = _ => allyMoveComplete = true;
        ally.MotionAnimator.OnMoveComplete += onAllyMoveComplete;
        ally.Move(new Vector2(dropPos.x, dropPos.y), true);

        while (!allyMoveComplete)
        {
            yield return null;
        }

        ally.MotionAnimator.OnMoveComplete -= onAllyMoveComplete;

        bool gryphonMoveComplete = false;
        System.Action<Vector3> onGryphonMoveComplete = _ => gryphonMoveComplete = true;
        Caster.MotionAnimator.OnMoveComplete += onGryphonMoveComplete;
        Caster.Move(new Vector2(destPos.x, destPos.y), true);

        while (!gryphonMoveComplete)
        {
            yield return null;
        }

        Caster.MotionAnimator.OnMoveComplete -= onGryphonMoveComplete;
        EnsureCasterGrounded(destPos, casterGroundY, casterGroundRotation);
        yield return null;
    }

    private System.Collections.IEnumerator PlayGryphonLiftOffAnimation()
    {
        Vector3 startPosition = Caster.transform.position;
        float startY = startPosition.y;
        Quaternion groundRotation = Caster.MotionAnimator.GroundRotation;
        float targetY = startY + SpiralLiftHeight;
        float settleRise = Mathf.Clamp(Caster.MotionAnimator.PieceHeight, 0.35f, 0.85f);
        float settleTargetY = targetY + settleRise;
        float elapsed = 0f;

        while (elapsed < SpiralDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / SpiralDuration);
            float easeT = Mathf.SmoothStep(0f, 1f, t);
            float y = Mathf.Lerp(startY, targetY, easeT);
            float rotation = t * 360f;
            float offsetX = Mathf.Sin(t * Mathf.PI * 4f) * SpiralHorizontalRadius;
            float offsetZ = Mathf.Cos(t * Mathf.PI * 4f) * SpiralHorizontalRadius;

            Caster.transform.position = new Vector3(
                startPosition.x + offsetX,
                y,
                startPosition.z + offsetZ
            );

            Caster.transform.rotation = GetLiftOffRotation(groundRotation, rotation);
            yield return null;
        }

        Vector3 settleStartPosition = Caster.transform.position;
        Quaternion settleStartRotation = Caster.transform.rotation;
        Vector3 settleTargetPosition = new Vector3(startPosition.x, settleTargetY, startPosition.z);

        elapsed = 0f;
        while (elapsed < SettleDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / SettleDuration);
            float easeT = Mathf.SmoothStep(0f, 1f, t);

            Caster.transform.position = Vector3.Lerp(settleStartPosition, settleTargetPosition, easeT);
            Caster.transform.rotation = Quaternion.Slerp(settleStartRotation, groundRotation, easeT);

            yield return null;
        }

        Caster.MotionAnimator.ForceSetState(settleTargetPosition, groundRotation, PieceAnimationState.Lifted);
    }

    private static Quaternion GetLiftOffRotation(Quaternion groundRotation, float spinDegrees)
    {
        return groundRotation * Quaternion.Euler(0f, spinDegrees, 0f);
    }

    private static Vector3 GetBoardWorldPosition(Vector2Int boardPosition, float y)
    {
        return new Vector3(boardPosition.x, y, boardPosition.y);
    }

    private void EnsureCasterGrounded(Vector2Int destPos, float groundY, Quaternion groundRotation)
    {
        Vector3 groundedPosition = GetBoardWorldPosition(destPos, groundY);
        bool positionMismatch = Vector3.Distance(Caster.transform.position, groundedPosition) > 0.01f;
        bool rotationMismatch = Quaternion.Angle(Caster.transform.rotation, groundRotation) > 0.5f;
        bool needsReset = Caster.MotionAnimator.CurrentState != PieceAnimationState.Idle || positionMismatch || rotationMismatch;

        if (needsReset)
        {
            Caster.MotionAnimator.ForceSetState(groundedPosition, groundRotation, PieceAnimationState.Idle);
        }
    }

    private List<Vector2Int> GetAdjacentAllies()
    {
        List<Vector2Int> allies = new List<Vector2Int>();
        if (Caster == null || LogicManager == null)
        {
            return allies;
        }

        Vector2Int casterPos = Vector2Int.RoundToInt(Caster.GetCoordinates());
        foreach (Vector2Int offset in AdjacentOffsets)
        {
            Vector2Int pos = casterPos + offset;
            if (!Caster.IsPositionWithinBoard(pos))
            {
                continue;
            }

            Piece ally = LogicManager.boardMap[pos.x, pos.y];
            if (ally != null && ally.IsWhite == Caster.IsWhite && GetDestinationsForAlly(pos).Count > 0)
            {
                allies.Add(pos);
            }
        }

        return allies;
    }

    private List<Vector2Int> GetDestinationsForAlly(Vector2Int allyPos)
    {
        List<Vector2Int> destinations = new List<Vector2Int>();
        if (Caster == null || LogicManager == null)
        {
            return destinations;
        }

        Vector2Int casterPos = Vector2Int.RoundToInt(Caster.GetCoordinates());
        foreach (Vector2Int dir in AdjacentOffsets)
        {
            if (dir == Vector2Int.zero)
            {
                continue;
            }

            for (int distance = 1; distance <= 3; distance++)
            {
                Vector2Int dest = casterPos + dir * distance;
                if (!Caster.IsPositionWithinBoard(dest))
                {
                    break;
                }

                if (!IsLineClear(casterPos, dir, distance, allyPos))
                {
                    break;
                }

                if (LogicManager.boardMap[dest.x, dest.y] != null)
                {
                    break;
                }

                if (GetDropSquares(dest).Count == 0)
                {
                    continue;
                }

                destinations.Add(dest);
            }
        }

        return destinations;
    }

    private bool IsLineClear(Vector2Int start, Vector2Int direction, int distance, Vector2Int allyPos)
    {
        for (int step = 1; step <= distance; step++)
        {
            Vector2Int pos = start + direction * step;
            if (pos == allyPos)
            {
                continue;
            }

            if (!Caster.IsPositionWithinBoard(pos))
            {
                return false;
            }

            if (LogicManager.boardMap[pos.x, pos.y] != null)
            {
                return false;
            }
        }

        return true;
    }

    private List<Vector2Int> GetDropSquares(Vector2Int queenDest)
    {
        List<Vector2Int> drops = new List<Vector2Int>();
        if (Caster == null || LogicManager == null)
        {
            return drops;
        }

        foreach (Vector2Int offset in AdjacentOffsets)
        {
            Vector2Int pos = queenDest + offset;
            if (!Caster.IsPositionWithinBoard(pos) || pos == queenDest)
            {
                continue;
            }

            if (LogicManager.boardMap[pos.x, pos.y] == null)
            {
                drops.Add(pos);
            }
        }

        return drops;
    }

    private List<Vector2> ConvertToVector2(List<Vector2Int> ints)
    {
        List<Vector2> result = new List<Vector2>();
        foreach (Vector2Int value in ints)
        {
            result.Add(new Vector2(value.x, value.y));
        }

        return result;
    }
}
