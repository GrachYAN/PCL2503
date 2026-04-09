using System.Collections.Generic;
using UnityEngine;

public class BarrierPiece : Piece
{
    private int lifeTimeRounds; // 

    public void InitializeBarrier(int duration)
    {
        Initialize("Barrier", true, Faction.Elf);
        lifeTimeRounds = duration;
    }

    public override List<Vector2> GetAttackedFields()
    {
        return new List<Vector2>(); // 
    }

    protected override List<Vector2> GetPotentialMoves()
    {
        return new List<Vector2>(); // 
    }

    public override List<Vector2> GetLegalMoves()
    {
        return new List<Vector2>();
    }

    public override void OnTurnStart(bool activeTurnIsWhite)
    {

        if (IsWhite == activeTurnIsWhite)
        {
            lifeTimeRounds--;
            if (lifeTimeRounds <= 0)
            {
                Debug.Log("Barrier has expired and will be destroyed.");

                logicManager.DestroyPiece(this);
            }
        }
    }
}