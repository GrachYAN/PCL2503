using System.Collections.Generic;
using UnityEngine;

public class CrystallinePush : Spell
{
    public CrystallinePush()
    {
        SpellName = "Crystalline Push";
        Description = "Deal 3 damage to an adjacent enemy.";
        ManaCost = 0;
        Cooldown = 0;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        List<Vector2> validTargets = new List<Vector2>();

        // Check all adjacent squares (8 directions)
        Vector2[] directions = {
            new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1),
            new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(-1, -1)
        };

        Vector2 casterPos = Caster.GetCoordinates();

        foreach (Vector2 dir in directions)
        {
            Vector2 targetPos = casterPos + dir;

            if (!Caster.IsPositionWithinBoard(targetPos))
                continue;

            Piece targetPiece = LogicManager.boardMap[(int)targetPos.x, (int)targetPos.y];
            if (targetPiece != null && targetPiece.IsWhite != Caster.IsWhite)
            {
                validTargets.Add(targetPos);
            }
        }

        return validTargets;
    }

    protected override void ExecuteEffect(Vector2 targetSquare)
    {
        Piece targetPiece = LogicManager.boardMap[(int)targetSquare.x, (int)targetSquare.y];

        if (targetPiece != null && targetPiece.IsWhite != Caster.IsWhite)
        {
            // Determine damage type based on faction: Arcane for Elf, Physical for Dwarf
            DamageType damageType = (Caster.ResolvedFaction == Faction.Dwarf) ? DamageType.Physical : DamageType.Arcane;
            
            int finalDamage = 3 + Caster.DamageBonus;
            targetPiece.TakeDamage(finalDamage, damageType);
            Debug.Log($"{SpellName} dealt {finalDamage} damage to {targetPiece.PieceType}!");
        }
    }
}