using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hawkstrider Dash: A multi-step spell.
/// Step 1: Select a landing square (knight-move pattern). The square must have at least one adjacent enemy.
/// Step 2: Select one adjacent enemy to deal 5 Fire damage to.
/// </summary>
public class HawkstriderDash : Spell
{
    private static readonly Vector2Int[] KnightMoves =
    {
        new Vector2Int(1, 2), new Vector2Int(1, -2), new Vector2Int(-1, 2), new Vector2Int(-1, -2),
        new Vector2Int(2, 1), new Vector2Int(2, -1), new Vector2Int(-2, 1), new Vector2Int(-2, -1)
    };

    private static readonly Vector2Int[] AdjacentOffsets =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
    };

    // Multi-step state
    private Vector2Int? pendingLandingSquare;
    private Vector2Int? pendingTargetEnemy;

    public HawkstriderDash()
    {
        SpellName = "Hawkstrider Dash";
        Description = "Jump to a target square like a knight, dealing 5 Fire damage to one adjacent enemy upon landing.";
        ManaCost = 3;
        Cooldown = 2;
    }

    public override void BeginTargeting()
    {
        pendingLandingSquare = null;
        pendingTargetEnemy = null;
    }

    public override void CancelTargeting()
    {
        pendingLandingSquare = null;
        pendingTargetEnemy = null;
    }

    /// <summary>
    /// Returns all valid landing squares that have at least one adjacent enemy.
    /// </summary>
    public override List<Vector2> GetValidTargetSquares()
    {
        List<Vector2> validTargets = new List<Vector2>();

        foreach (Vector2Int landingSquare in GetValidLandingSquares())
        {
            // Only include landing squares that have at least one adjacent enemy
            if (GetAdjacentEnemies(landingSquare).Count > 0)
            {
                validTargets.Add(new Vector2(landingSquare.x, landingSquare.y));
            }
        }

        return validTargets;
    }

    /// <summary>
    /// Returns valid squares for the current selection step.
    /// Step 1: Landing squares with adjacent enemies
    /// Step 2: Adjacent enemies at the selected landing square
    /// </summary>
    public override List<Vector2> GetCurrentValidSquares()
    {
        if (!pendingLandingSquare.HasValue)
        {
            // Step 1: Show valid landing squares
            return GetValidTargetSquares();
        }
        else
        {
            // Step 2: Show adjacent enemies at the landing square
            List<Vector2> enemyPositions = new List<Vector2>();
            foreach (Vector2Int enemyPos in GetAdjacentEnemies(pendingLandingSquare.Value))
            {
                enemyPositions.Add(new Vector2(enemyPos.x, enemyPos.y));
            }
            return enemyPositions;
        }
    }

    /// <summary>
    /// Handles target selection for multi-step casting.
    /// </summary>
    public override bool TryHandleTargetSelection(Vector2 targetSquare, out bool castComplete)
    {
        castComplete = false;
        Vector2Int gridTarget = Vector2Int.RoundToInt(targetSquare);

        if (!pendingLandingSquare.HasValue)
        {
            // Step 1: Select landing square
            List<Vector2Int> validLandingSquares = GetValidLandingSquares();
            if (!validLandingSquares.Contains(gridTarget))
            {
                return false;
            }

            // Verify this landing square has adjacent enemies
            if (GetAdjacentEnemies(gridTarget).Count == 0)
            {
                return false;
            }

            pendingLandingSquare = gridTarget;
            return true; // Continue to step 2
        }
        else
        {
            // Step 2: Select which enemy to attack
            List<Vector2Int> adjacentEnemies = GetAdjacentEnemies(pendingLandingSquare.Value);
            if (!adjacentEnemies.Contains(gridTarget))
            {
                return false;
            }

            pendingTargetEnemy = gridTarget;
            castComplete = true; // All selections complete
            return true;
        }
    }

    public override SpellCastData GetCastData(Vector2 finalTarget)
    {
        return new SpellCastData
        {
            // Primary = the enemy to attack
            PrimaryX = Mathf.RoundToInt(finalTarget.x),
            PrimaryY = Mathf.RoundToInt(finalTarget.y),
            // Secondary = the landing square
            SecondaryX = pendingLandingSquare.HasValue ? pendingLandingSquare.Value.x : -1,
            SecondaryY = pendingLandingSquare.HasValue ? pendingLandingSquare.Value.y : -1,
            TertiaryX = -1,
            TertiaryY = -1
        };
    }

    public override void ApplyCastData(SpellCastData data)
    {
        Vector2Int landingSquare = data.GetSecondary();
        Vector2Int targetEnemy = data.GetPrimary();

        pendingLandingSquare = landingSquare.x >= 0 ? landingSquare : (Vector2Int?)null;
        pendingTargetEnemy = targetEnemy.x >= 0 ? targetEnemy : (Vector2Int?)null;
    }

    public override bool IsCastDataValid(SpellCastData data)
    {
        if (Caster == null || LogicManager == null)
        {
            return false;
        }

        Vector2Int landingSquare = data.GetSecondary();
        Vector2Int targetEnemy = data.GetPrimary();

        if (landingSquare.x < 0 || targetEnemy.x < 0)
        {
            return false;
        }

        // Validate landing square is a valid knight move
        if (!GetValidLandingSquares().Contains(landingSquare))
        {
            return false;
        }

        // Validate target enemy is adjacent to landing square and is an enemy
        if (!GetAdjacentEnemies(landingSquare).Contains(targetEnemy))
        {
            return false;
        }

        return true;
    }

    protected override void ExecuteEffect(Vector2 targetSquare)
    {
        if (!pendingLandingSquare.HasValue || !pendingTargetEnemy.HasValue)
        {
            Debug.LogError("HawkstriderDash: Missing landing square or target enemy!");
            return;
        }

        // 1. Move the caster to the landing square
        Vector2 landingPos = new Vector2(pendingLandingSquare.Value.x, pendingLandingSquare.Value.y);
        Caster.Move(landingPos);

        // 2. Deal damage to the selected enemy
        Piece targetPiece = LogicManager.boardMap[pendingTargetEnemy.Value.x, pendingTargetEnemy.Value.y];
        if (targetPiece != null && targetPiece.IsWhite != Caster.IsWhite)
        {
            int finalDamage = 5 + Caster.DamageBonus;
            targetPiece.TakeDamage(finalDamage, DamageType.Fire);
        }

        // 3. Reset state
        pendingLandingSquare = null;
        pendingTargetEnemy = null;
    }

    /// <summary>
    /// Gets all valid landing squares (knight-move pattern, empty or enemy-occupied).
    /// </summary>
    private List<Vector2Int> GetValidLandingSquares()
    {
        List<Vector2Int> validSquares = new List<Vector2Int>();
        Vector2 currentPos = Caster.GetCoordinates();
        Vector2Int currentPosInt = new Vector2Int((int)currentPos.x, (int)currentPos.y);

        foreach (var move in KnightMoves)
        {
            Vector2Int targetPos = currentPosInt + move;

            if (!Caster.IsPositionWithinBoard(new Vector2(targetPos.x, targetPos.y)))
            {
                continue;
            }

            // Can only land on empty squares
            Piece occupant = LogicManager.boardMap[targetPos.x, targetPos.y];
            if (occupant == null)
            {
                validSquares.Add(targetPos);
            }
        }

        return validSquares;
    }

    /// <summary>
    /// Gets all adjacent enemy pieces from a given position.
    /// </summary>
    private List<Vector2Int> GetAdjacentEnemies(Vector2Int position)
    {
        List<Vector2Int> enemies = new List<Vector2Int>();

        foreach (var offset in AdjacentOffsets)
        {
            Vector2Int adjacentPos = position + offset;

            if (!Caster.IsPositionWithinBoard(new Vector2(adjacentPos.x, adjacentPos.y)))
            {
                continue;
            }

            Piece adjacentPiece = LogicManager.boardMap[adjacentPos.x, adjacentPos.y];
            if (adjacentPiece != null && adjacentPiece.IsWhite != Caster.IsWhite)
            {
                enemies.Add(adjacentPos);
            }
        }

        return enemies;
    }
}