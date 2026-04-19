using System.Collections.Generic;
using UnityEngine;

public class Pyroblast : Spell
{
    public Pyroblast()
    {
        SpellName = "Pyroblast";
        Description = "Deals 10 FR to a single target in a straight line (blocked by obstacles).";
        ManaCost = 8;
        Cooldown = 0;

        // �ױ����ǹ������ܣ��ͷź�����غ�
        EndsTurn = true;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        List<Vector2> validTargets = new List<Vector2>();

        if (Caster == null || LogicManager == null) return validTargets;

        // 1. ��ȡʩ��������
        Vector2 casterPosVec = Caster.GetCoordinates();
        Vector2Int startPos = new Vector2Int(Mathf.RoundToInt(casterPosVec.x), Mathf.RoundToInt(casterPosVec.y));

        // 2. �����ĸ����� (��, ��, ��, ��) - �� Rook ���ƶ�����
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0, 1),  // Up
            new Vector2Int(0, -1), // Down
            new Vector2Int(-1, 0), // Left
            new Vector2Int(1, 0)   // Right
        };

        // 3. ��ÿ������������߼��
        foreach (Vector2Int dir in directions)
        {
            // �����������ߴ��� 8x8�������� 1 ��ʼ����
            for (int distance = 1; distance < 8; distance++)
            {
                Vector2Int checkPos = startPos + (dir * distance);
                Vector2 checkPosVec = new Vector2(checkPos.x, checkPos.y);

                // A. ���߽磺����������̣�ֹͣ�÷�������
                if (!Caster.IsPositionWithinBoard(checkPosVec))
                {
                    break;
                }

                // B. ��ȡ��λ�õ�����
                if (LogicManager.IsPrismaticBarrierBlockingSquare(checkPosVec, Caster.IsWhite))
                {
                    break;
                }

                Piece hitPiece = LogicManager.boardMap[checkPos.x, checkPos.y];

                if (hitPiece != null)
                {
                    // C. ���߱��赲�������ǵ����ѣ����߶�����Ϊֹ��

                    // ֻ�е����ǵ���ʱ����������ЧĿ��
                    if (hitPiece.IsWhite != Caster.IsWhite)
                    {
                        validTargets.Add(checkPosVec);
                    }

                    // �����κ����ӣ��ϰ����ֹͣ��÷����������
                    break;
                }

                // D. ����ǿյ� (hitPiece == null)��ѭ�������������һ������
            }
        }

        return validTargets;
    }

    protected override void ExecuteEffect(Vector2 targetSquare)
    {
        if (LogicManager == null) return;
        if (!LogicManager.HasLineOfSight(Caster.GetCoordinates(), targetSquare, Caster.IsWhite)) return;

        int x = Mathf.RoundToInt(targetSquare.x);
        int y = Mathf.RoundToInt(targetSquare.y);

        Piece targetPiece = LogicManager.boardMap[x, y];

        if (targetPiece != null)
        {
            int finalDamage = 10 + Caster.DamageBonus;
            targetPiece.TakeDamage(finalDamage, DamageType.Fire);

            Debug.Log($"Pyroblast hit {targetPiece.PieceType} for {finalDamage} damage!");

            // TODO: ���������ﲥ����Ч
            // PlayFireEffect(targetSquare);
        }
    }
}
