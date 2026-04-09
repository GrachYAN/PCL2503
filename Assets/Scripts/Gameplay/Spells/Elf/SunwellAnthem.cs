using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SunwellAnthem : Spell
{
    public SunwellAnthem()
    {
        SpellName = "Sunwell Anthem";
        Description = "Party-wide buff for 2 rounds: allies gain +3 damage, and -3 Mana cost. ";
        ManaCost = 7;
        Cooldown = 0;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        // Party-wide buff, no target needed.

        List<Vector2> validSquares = new List<Vector2>();

        if (Caster != null)
        {
            validSquares.Add(Caster.GetCoordinates());
        }

        return validSquares;
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