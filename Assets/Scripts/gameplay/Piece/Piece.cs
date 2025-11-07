using System.Collections.Generic;
using UnityEngine;

public abstract class Piece : MonoBehaviour
{
    protected LogicManager logicManager;
    public bool IsWhite { get; private set; }
    public string PieceType { get; private set; }
    public int HasMoved { get; private set; }

    [Header("战斗属性")]
    public int MaxHP { get; private set; }
    public int CurrentHP { get; private set; }
    public int MaxMana { get; private set; }
    public int CurrentMana { get; private set; }

    public List<Spell> Spells = new List<Spell>();

    public abstract List<Vector2> GetAttackedFields();
    protected abstract List<Vector2> GetPotentialMoves();

    public virtual List<Vector2> GetLegalMoves()
    {
        List<Vector2> legalMoves = new List<Vector2>();
        bool isKingInCheck = logicManager.CheckKingStatus();
        foreach (Vector2 move in GetPotentialMoves())
        {
            if (WillMoveEndCheck(move))
            {
                legalMoves.Add(move);
            }
        }
        return legalMoves;
    }

    protected bool WillMoveEndCheck(Vector2 move)
    {
        Piece originalPiece = logicManager.boardMap[(int)move.x, (int)move.y];
        Vector2 originalPosition = GetCoordinates();

        logicManager.boardMap[(int)originalPosition.x, (int)originalPosition.y] = null;
        logicManager.boardMap[(int)move.x, (int)move.y] = this;
        logicManager.UpdateCheckMap();

        bool isKingInCheck = logicManager.CheckKingStatus();

        if (this is King)
        {
            isKingInCheck = IsWhite ? logicManager.blackCheckMap[(int)move.x, (int)move.y] : logicManager.whiteCheckMap[(int)move.x, (int)move.y];
        }

        logicManager.boardMap[(int)originalPosition.x, (int)originalPosition.y] = this;
        logicManager.boardMap[(int)move.x, (int)move.y] = originalPiece;
        logicManager.UpdateCheckMap();

        return !isKingInCheck;
    }

    public void Initialize(string pieceType, bool isWhite)
    {
        PieceType = pieceType;
        IsWhite = isWhite;
        HasMoved = 0;

        SetStatsBasedOnType(pieceType);
        CurrentHP = MaxHP;
        CurrentMana = 0;

        InitializeSpells(pieceType);
    }

    private void Start()
    {
        logicManager = Object.FindFirstObjectByType<LogicManager>();
        UpdateBoardMap();
    }

    public Vector2 GetCoordinates()
    {
        return new Vector2(transform.position.x, transform.position.z);
    }

    public void UpdateBoardMap()
    {
        Vector2 coordinates = GetCoordinates();
        logicManager.boardMap[(int)coordinates.x, (int)coordinates.y] = this;
    }

    public virtual void Move(Vector2 newPosition)
    {
        Vector2 currentCoordinates = GetCoordinates();
        logicManager.boardMap[(int)currentCoordinates.x, (int)currentCoordinates.y] = null;
        HasMoved = 1;
        Take(newPosition);
        transform.position = new Vector3(newPosition.x, transform.position.y, newPosition.y);
        UpdateBoardMap();
    }

    public void Take(Vector2 targetPosition)
    {
        Piece targetPiece = logicManager.boardMap[(int)targetPosition.x, (int)targetPosition.y];
        if (targetPiece != null)
        {
            Destroy(targetPiece.gameObject);
        }
        logicManager.boardMap[(int)targetPosition.x, (int)targetPosition.y] = null;
    }

    public bool IsPositionWithinBoard(Vector2 position)
    {
        return position.x >= 0 && position.x < 8 && position.y >= 0 && position.y < 8;
    }

    private void SetStatsBasedOnType(string type)
    {
        switch (type)
        {
            case "Pawn":
                MaxHP = 8; MaxMana = 3;
                break;
            case "Knight":
                MaxHP = 15; MaxMana = 7;
                break;
            case "Rook":
                MaxHP = 18; MaxMana = 8;
                break;
            case "Bishop":
                MaxHP = 12; MaxMana = 9;
                break;
            case "Queen":
                MaxHP = 18; MaxMana = 10;
                break;
            case "King":
                MaxHP = 24; MaxMana = 12;
                break;
            default:
                Debug.LogError("unknown: " + type);
                MaxHP = 1; MaxMana = 0;
                break;
        }
    }

    private void InitializeSpells(string type)
    {
        Spells.Clear();
        if (logicManager == null)
        {
            logicManager = Object.FindFirstObjectByType<LogicManager>();
        }

        switch (type)
        {
            case "Pawn":
                Spells.Add(new CrystallinePush());
                Spells.Add(new Drain());
                break;
            case "Knight":
                break;
            case "Bishop":
                break;
            case "Rook":
                break;
            case "Queen":
                break;
            case "King":
                break;
        }

        foreach (Spell spell in Spells)
        {
            spell.Initialize(this, logicManager);
        }
    }

    public void TakeDamage(int amount)
    {
        CurrentHP -= amount;
        Debug.Log($"{PieceType}受到了 {amount} 点伤害, 剩余HP: {CurrentHP}/{MaxHP}");

        if (CurrentHP <= 0)
        {
            CurrentHP = 0;
            logicManager.DestroyPiece(this);
        }
    }

    public bool UseMana(int amount)
    {
        if (CurrentMana < amount) return false;
        CurrentMana -= amount;
        return true;
    }

    public void GainMana(int amount)
    {
        CurrentMana += amount;
        if (CurrentMana > MaxMana) CurrentMana = MaxMana;
    }

    public void LoseMana(int amount)
    {
        CurrentMana -= amount;
        if (CurrentMana < 0) CurrentMana = 0;
    }

    public void OnTurnStart()
    {
        GainMana(1);

        foreach (Spell spell in Spells)
        {
            spell.OnTurnStart();
        }

    }
}