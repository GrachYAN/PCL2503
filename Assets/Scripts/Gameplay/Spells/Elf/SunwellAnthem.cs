using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SunwellAnthem : Spell
{
    public SunwellAnthem()
    {
        SpellName = "Sunwell Anthem";
        Description = "Party-wide buff for 2 rounds: allies gain +2 damage, and -3 Mana cost. The buff also applies a shield of 5 HP.";
        ManaCost = 9;
        Cooldown = 8;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        // Party-wide buff, no target needed.
        return new List<Vector2>();
    }

    protected override void ExecuteEffect(Vector2 targetSquare)
    {
        LogicManager.ApplySunwellAnthem(Caster.IsWhite);
    }

    public override bool IsCastDataValid(SpellCastData data)
    {
        return true;
    }
}