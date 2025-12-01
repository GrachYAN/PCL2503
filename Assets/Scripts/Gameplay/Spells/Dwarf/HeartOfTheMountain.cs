using System.Collections.Generic;
using UnityEngine;

public class HeartOfTheMountain : Spell
{
    public HeartOfTheMountain()
    {
        SpellName = "Heart of the Mountain";
        Description = "Party buff: allies gain an extra orthogonal step and root immunity for 3 rounds.";
        ManaCost = 9;
        Cooldown = 8;
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

        LogicManager.ApplyHeartOfMountainBuff(Caster.IsWhite, 4);
    }
}
