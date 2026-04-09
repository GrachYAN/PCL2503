using System.Collections.Generic;
using UnityEngine;

public class ScorchingRay : Spell
{
    public ScorchingRay()
    {
        SpellName = "Scorching Ray";
        Description = "Diagonal ray; first enemy hit takes 5 damage, the rest takes 3 damage.";
        ManaCost = 5;
        Cooldown = 0;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        List<Vector2> validTargets = new List<Vector2>();
        Vector2 casterPos = Caster.GetCoordinates();

        int[] directionsX = { 1, 1, -1, -1 };
        int[] directionsY = { 1, -1, 1, -1 };

        for (int i = 0; i < 4; i++)
        {
            int step = 1;
            while (true)
            {
                Vector2 targetPos = new Vector2(casterPos.x + step * directionsX[i], casterPos.y + step * directionsY[i]);

                if (!Caster.IsPositionWithinBoard(targetPos)) break;

                Piece pieceAtTarget = LogicManager.boardMap[(int)targetPos.x, (int)targetPos.y];
                if (pieceAtTarget != null)
                {
                    if (pieceAtTarget.IsWhite != Caster.IsWhite)
                    {
                        validTargets.Add(targetPos);
                    }
                    break;
                }
                step++;
            }
        }
        return validTargets;
    }

    protected override void ExecuteEffect(Vector2 target)
    {
        Vector2 casterPos = Caster.GetCoordinates();
        Vector2 direction = (target - casterPos).normalized;
        direction = new Vector2(Mathf.Round(direction.x), Mathf.Round(direction.y));

        int firstDamage = 5 + Caster.DamageBonus;
        int subsequentDamage = 3 + Caster.DamageBonus;

        // Determine damage type based on faction: Fire for Elf, Holy for Dwarf
        DamageType damageType = (Caster.ResolvedFaction == Faction.Dwarf) ? DamageType.Holy : DamageType.Fire;

        // First target takes 5 damage
        Piece firstTargetPiece = LogicManager.boardMap[(int)target.x, (int)target.y];
        if (firstTargetPiece != null)
        {
            firstTargetPiece.TakeDamage(firstDamage, damageType);
        }

        // Continue ray and deal 3 damage to subsequent enemies
        int step = 1;
        while (true)
        {
            Vector2 nextPos = target + (direction * step);
            if (!Caster.IsPositionWithinBoard(nextPos)) break;

            Piece nextPiece = LogicManager.boardMap[(int)nextPos.x, (int)nextPos.y];
            if (nextPiece != null)
            {
                if (nextPiece.IsWhite != Caster.IsWhite)
                {
                    nextPiece.TakeDamage(subsequentDamage, damageType);
                }
                else
                {
                    break; // Blocked by friendly piece
                }
            }
            step++;
        }
    }
}
