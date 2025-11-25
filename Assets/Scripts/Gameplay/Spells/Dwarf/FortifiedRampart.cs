using System.Collections.Generic;
using UnityEngine;

public class FortifiedRampart : Spell
{
    public FortifiedRampart()
    {
        SpellName = "Fortified Rampart";
        Description = "Allies in a 3x3 aura centered on the rook take 3 less damage for 2 rounds.";
        ManaCost = 4;
        Cooldown = 3;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        return new List<Vector2> { Caster.GetCoordinates() };
    }

    protected override void ExecuteEffect(Vector2 targetSquare)
    {
        if (Caster == null || LogicManager == null)
        {
            return;
        }

        LogicManager.RegisterRampartAura(Caster, 3, 2);
    }
}
