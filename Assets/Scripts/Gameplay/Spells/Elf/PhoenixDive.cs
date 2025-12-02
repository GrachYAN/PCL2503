using System.Collections.Generic;
using UnityEngine;

public class PhoenixDive : Spell
{
    public PhoenixDive()
    {
        SpellName = "Queen Dive";
        Description = "Place a Flame Mark up to 3 squares in any one direction ignoring LoS. The enemy takes a stun for 3 rounds and burns (DoT=4) for 3 rounds.";
        ManaCost = 5;
        Cooldown = 0;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        List<Vector2> validTargets = new List<Vector2>();
        if (Caster == null) return validTargets;

        Vector2 startPos = Caster.GetCoordinates();

        Vector2[] directions = {
            new Vector2(0, 1), new Vector2(0, -1), new Vector2(1, 0), new Vector2(-1, 0),
            new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(-1, -1)
        };

        foreach (var dir in directions)
        {
            for (int i = 1; i <= 3; i++) 
            {
                Vector2 targetPos = startPos + dir * i;
                if (!Caster.IsPositionWithinBoard(targetPos)) break;


                Piece targetPiece = LogicManager.boardMap[(int)targetPos.x, (int)targetPos.y];
                if (targetPiece != null && targetPiece.IsWhite != Caster.IsWhite)
                {
                    validTargets.Add(targetPos);
                }
            }
        }
        return validTargets;
    }

    protected override void ExecuteEffect(Vector2 target)
    {
        Piece targetPiece = LogicManager.boardMap[(int)target.x, (int)target.y];
        if (targetPiece != null)
        {
            // Determine damage type based on faction: Fire for Elf, Holy for Dwarf
            DamageType damageType = (Caster.ResolvedFaction == Faction.Dwarf) ? DamageType.Holy : DamageType.Fire;

            targetPiece.ApplyStun(3); 
            targetPiece.ApplyDamageOverTime(4, damageType, 3); 

            LogicManager.PlaceFlameMark(target, 3);
        }
    }
}