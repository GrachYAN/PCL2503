using System.Collections.Generic;
using UnityEngine;

public class HeartOfTheMountain : Spell
{
    public HeartOfTheMountain()
    {
        SpellName = "Heart of the Mountain";
        Description = "Party buff: allies gain an extra orthogonal step for 3 rounds.";
        ManaCost = 7;
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

        LogicManager.ApplyHeartOfMountainBuff(Caster.IsWhite, 4);
    }
}
