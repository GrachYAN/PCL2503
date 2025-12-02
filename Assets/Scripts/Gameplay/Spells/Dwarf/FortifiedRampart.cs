using System.Collections.Generic;
using UnityEngine;

public class FortifiedRampart : Spell
{
    public FortifiedRampart()
    {
        SpellName = "Fortified Rampart";
        Description = "Allies in a 5x5 aura centered on the rook take 3 less damage for 3 rounds.";
        ManaCost = 5;
        Cooldown = 0;
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

        LogicManager.RegisterRampartAura(Caster, 3, 3);
    }
}
