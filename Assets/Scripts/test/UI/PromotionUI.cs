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

    public void Show(Pawn pawn)
    {
        promotingPawn = pawn;
        panel.SetActive(true);
    }

    public void PromotePawn(string pieceName)
    {
        int prefabIndex = -1;
        string pieceType = "";
        switch (pieceName)
        {
            case "Queen":
                prefabIndex = promotingPawn.IsWhite ? 8 : 9;
                pieceType = "Queen";
                break;
            case "Rook":
                prefabIndex = promotingPawn.IsWhite ? 2 : 3;
                pieceType = "Rook";
                break;
            case "Bishop":
                prefabIndex = promotingPawn.IsWhite ? 6 : 7;
                pieceType = "Bishop";
                break;
            case "Knight":
                prefabIndex = promotingPawn.IsWhite ? 4 : 5;
                pieceType = "Knight";
                break;
        }

        if (prefabIndex != -1)
        {
            Vector3 pawnPosition = promotingPawn.transform.position;
            Material pieceMaterial = promotingPawn.IsWhite ? board.PieceMaterials[0] : board.PieceMaterials[1];
            board.InstantiatePiece(board.PiecePrefabs[prefabIndex], pawnPosition, pieceMaterial, pieceType, promotingPawn.IsWhite);

            Vector2 pawnCoordinates = promotingPawn.GetCoordinates();
            board.logicManager.boardMap[(int)pawnCoordinates.x, (int)pawnCoordinates.y] = null;

            Destroy(promotingPawn.gameObject);
        }

        panel.SetActive(false);
        logicManager.isPromotionActive = false;
        logicManager.UpdateCheckMap();
        logicManager.EndTurn();
    }
}