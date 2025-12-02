using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SunwellWard : Spell
{
    public SunwellWard()
    {
        SpellName = "Sunwell Ward";
        Description = "Create a 5x5 aura buff centered on the Rook for 1 round. Allies inside cannot be damaged.";
        ManaCost = 4;
        Cooldown = 0;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        // Self-cast aura, no specific target squares needed.
        List<Vector2> validSquares = new List<Vector2>();

        if (Caster != null)
        {
            validSquares.Add(Caster.GetCoordinates());
        }

        return validSquares;
    }

    protected override void ExecuteEffect(Vector2 targetSquare)
    {
        // The caster is the Rook itself
        LogicManager.ApplySunwellWard(Caster);
    }

    // This spell targets the caster, so it doesn't need complex validation.
    public override bool IsCastDataValid(SpellCastData data)
    {
        return true;
    }
}