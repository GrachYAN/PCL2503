using UnityEngine;

public class PieceStatusVFXController : MonoBehaviour
{
    private Piece piece;
    private GameObject stunEffect;
    private GameObject mindControlEffect;
    private GameObject burningEffect;

    private void Awake()
    {
        piece = GetComponent<Piece>();
    }

    private void LateUpdate()
    {
        if (piece == null)
        {
            piece = GetComponent<Piece>();
            if (piece == null)
            {
                return;
            }
        }

        SyncStatusEffect(ref stunEffect, piece.IsStunned, "BUFF/xuanyun", SpellVFXManager.StunColor, 0.78f, 0.10f);
        SyncStatusEffect(ref mindControlEffect, piece.IsMindControlled, "BUFF/shuimian", SpellVFXManager.MindControlColor, 0.56f, 0.18f);
        SyncBodyStatusEffect(ref burningEffect, piece.IsBurning, "BUFF/ranshao", SpellVFXManager.FireColor, 0.42f, -0.20f);
    }

    private void OnDisable()
    {
        Cleanup(ref stunEffect);
        Cleanup(ref mindControlEffect);
        Cleanup(ref burningEffect);
    }

    private void OnDestroy()
    {
        Cleanup(ref stunEffect);
        Cleanup(ref mindControlEffect);
        Cleanup(ref burningEffect);
    }

    private void SyncStatusEffect(ref GameObject effect, bool isActive, string prefabPath, Color tint, float scale, float extraHeight)
    {
        if (!isActive || piece == null)
        {
            Cleanup(ref effect);
            return;
        }

        if (effect == null)
        {
            effect = SpellVFXManager.Instance.CreateAttachedStatusEffect(piece, prefabPath, tint, scale, extraHeight);
        }

        SpellVFXManager.Instance.UpdateAttachedStatusEffect(effect, piece, scale, extraHeight);
    }

    private void SyncBodyStatusEffect(ref GameObject effect, bool isActive, string prefabPath, Color tint, float scale, float extraHeight)
    {
        if (!isActive || piece == null)
        {
            Cleanup(ref effect);
            return;
        }

        if (effect == null)
        {
            effect = SpellVFXManager.Instance.CreateAttachedBodyEffect(piece, prefabPath, tint, scale, extraHeight);
        }

        SpellVFXManager.Instance.UpdateAttachedBodyEffect(effect, piece, scale, extraHeight);
    }

    private void Cleanup(ref GameObject effect)
    {
        if (effect != null)
        {
            Destroy(effect);
            effect = null;
        }
    }
}
