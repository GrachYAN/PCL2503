using System.Collections.Generic;
using UnityEngine;

public class HawkCry : Spell
{
    public HawkCry()
    {
        SpellName = "Hawk Cry";
        Description = "All adjacent enemies get Dazed. (Daze: next spell costs +3 Mana and +2 CD)";
        ManaCost = 4;
        Cooldown = 0;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        return new List<Vector2> { Caster.GetCoordinates() };
    }

    protected override void ExecuteEffect(Vector2 target)
    {
        // Ч��Ӧ����ʩ������Χ�ĵ���
        Vector2 casterPos = Caster.GetCoordinates();
        Vector2[] directions = {
            new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1),
            new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(-1, -1)
        };

        int dazedCount = 0;
        foreach (var dir in directions)
        {
            Vector2 adjacentPos = casterPos + dir;
            if (Caster.IsPositionWithinBoard(adjacentPos))
            {
                Piece adjacentPiece = LogicManager.boardMap[(int)adjacentPos.x, (int)adjacentPos.y];
                if (adjacentPiece != null && adjacentPiece.IsWhite != Caster.IsWhite)
                {
                    // TODO: ʵ��"Dazed"״̬Ч���ĺ����߼���
                    adjacentPiece.ApplyDaze(1);
                    dazedCount++;
                    Debug.Log($"{adjacentPiece.PieceType} at {adjacentPos} is Dazed!");
                    // adjacentPiece.ApplyDaze(); // ռλ��
                }
            }
        }
        Debug.Log($"{SpellName} ѣ���� {dazedCount} ���з���λ!");
    }
}