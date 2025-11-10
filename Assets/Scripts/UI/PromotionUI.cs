using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class PromotionUI : MonoBehaviour
{
    public GameObject panel;
    private Pawn promotingPawn;
    private LogicManager logicManager;
    private Board board;

    public void Start()
    {
        logicManager = FindFirstObjectByType<LogicManager>();
        board = FindFirstObjectByType<Board>();
        panel.SetActive(false);
    }

    public void ShowPromotionPanel(Pawn pawnToPromote)
    {
        promotingPawn = pawnToPromote;
        panel.SetActive(true);
        logicManager.isPromotionActive = true;
    }

    public void Promote(string pieceType)
    {
        if (promotingPawn == null) return;

        // ��ȡ����λ�á���ɫ����Ӫ
        Vector2 coords = promotingPawn.GetCoordinates();
        bool isWhite = promotingPawn.IsWhite;
        Faction faction = promotingPawn.PieceFaction; // <-- ��ȡ��Ӫ��Ϣ

        // ���پɵı�
        logicManager.DestroyPiece(promotingPawn);

        // ȷ��ʹ���ĸ� Prefab ����
        GameObject[] prefabs = (faction == Faction.Elf) ? board.ElfPiecePrefabs : board.DwarfPiecePrefabs;
        GameObject newPiecePrefab = null;

        switch (pieceType)
        {
            case "Queen":
                newPiecePrefab = prefabs[4];
                break;
            case "Rook":
                newPiecePrefab = prefabs[1];
                break;
            case "Bishop":
                newPiecePrefab = prefabs[3];
                break;
            case "Knight":
                newPiecePrefab = prefabs[2];
                break;
        }

        if (newPiecePrefab != null)
        {
            // ����������
            Vector3 position = new Vector3(coords.x, board.pieceYOffset, coords.y);
            Material material = isWhite ? board.PieceMaterials[0] : board.PieceMaterials[1];

            // ������ �����޸ģ����������� faction ���� ������
            board.InstantiatePiece(newPiecePrefab, position, material, pieceType, isWhite, faction);
            // ������
        }

        // ������岢�����غ�
        panel.SetActive(false);
        logicManager.isPromotionActive = false;
        logicManager.EndTurn();
    }
}