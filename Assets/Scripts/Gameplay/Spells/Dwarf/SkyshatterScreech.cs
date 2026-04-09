using System.Collections.Generic;
using UnityEngine;

public class SkyshatterScreech : Spell
{
    public SkyshatterScreech()
    {
        SpellName = "Skyshatter Screech";
        Description = "All enemies within 2 take 5 Fire damage and are Dazed.";
        ManaCost = 6;
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

        Vector2 casterPos = Caster.GetCoordinates();
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                Piece piece = LogicManager.boardMap[x, y];
                if (piece == null || piece.IsWhite == Caster.IsWhite)
                {
                    continue;
                }

                if (Mathf.Abs(casterPos.x - x) <= 2 && Mathf.Abs(casterPos.y - y) <= 2)
                {
                    piece.TakeDamage(5, DamageType.Fire);
                    piece.ApplyDaze();
                }
            }
        }
    }
}
