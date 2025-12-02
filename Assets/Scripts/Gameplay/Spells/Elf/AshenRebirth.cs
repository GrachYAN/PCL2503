using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 灰烬重生技能：
/// 规则：CD8, 9M. 在己方后排的任意空格复活最近被击毁的友军棋子，复活时生命值为7且法力全满。
/// </summary>
public class AshenRebirth : Spell
{
    public AshenRebirth()
    {
        SpellName = "Ashen Rebirth";
        Description = "Revive the most recently destroyed friendly piece on any empty square of your back rank at 7 HP and full mana.";
        ManaCost = 7;
        Cooldown = 0;
    }

    public override List<Vector2> GetValidTargetSquares()
    {
        var validSquares = new List<Vector2>();
        if (Caster == null || LogicManager == null) return validSquares;

        // 确定后排的 y 坐标
        int backRank = Caster.IsWhite ? 0 : 7;

        for (int i = 0; i < 8; i++)
        {
            Vector2 squarePos = new Vector2(i, backRank);
            // 检查格子是否为空
            if (LogicManager.boardMap[(int)squarePos.x, (int)squarePos.y] == null)
            {
                validSquares.Add(squarePos);
            }
        }
        return validSquares;
    }

    public override bool CanCast()
    {
        if (!base.CanCast())
        {
            return false;
        }

        // 使用 LogicManager 的方法检查是否有可复活的单位
        if (!LogicManager.HasDestroyedPiece(Caster.IsWhite))
        {
            Debug.Log("施法失败：没有可复活的友方单位。");
            return false;
        }

        return true;
    }

    protected override void ExecuteEffect(Vector2 targetSquare)
    {
        // 从 LogicManager 获取最后阵亡的棋子信息
        LogicManager.DestroyedPieceInfo pieceToReviveInfo = LogicManager.GetLastDestroyedPiece(Caster.IsWhite);

        if (pieceToReviveInfo == null)
        {
            Debug.LogError("Ashen Rebirth: 尝试复活棋子失败，因为 GetLastDestroyedPiece 返回了 null。");
            return;
        }

        // 调用 LogicManager 的复活方法
        // 规则：生命值为7，法力全满 (fullMana = true)
        bool success = LogicManager.RevivePiece(pieceToReviveInfo, targetSquare, 7, true);

        if (success)
        {
            Debug.Log($"成功施放 {SpellName}！复活了 {pieceToReviveInfo.PieceType} 到 ({targetSquare.x}, {targetSquare.y})");
        }
        else
        {
            Debug.LogError($"施放 {SpellName} 失败，RevivePiece 方法返回 false。");
        }
    }
}
