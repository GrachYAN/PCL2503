using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PieceStatusDisplay : MonoBehaviour
{
    [Header("References")]
    public Piece targetPiece;
    public Canvas statusCanvas;
    public Transform iconContainer;

    [Header("Status Icons Prefabs")]
    public GameObject stunIconPrefab;
    public GameObject rootIconPrefab;
    public GameObject dazeIconPrefab;
    public GameObject shieldIconPrefab;

    [Header("DoT Icons")]
    public GameObject dotFireIconPrefab;    // Fire (FR)
    public GameObject dotArcaneIconPrefab;  // Arcane (AR)
    public GameObject dotHolyIconPrefab;    // Holy (HR)
    public GameObject dotPhysicalIconPrefab;// Physical (PR)

    [Header("Buff Icons")]
    public GameObject buffDefenseIconPrefab;
    public GameObject buffAttackIconPrefab;

    // 运行时实例化的图标缓存
    private Dictionary<string, GameObject> activeIcons = new Dictionary<string, GameObject>();

    private DamageType? currentDisplayedDoTType = null;

    private Camera mainCamera;

    void Start()
    {
        if (targetPiece == null)
            targetPiece = GetComponentInParent<Piece>();

        mainCamera = Camera.main;

        if (statusCanvas != null)
        {
            statusCanvas.renderMode = RenderMode.WorldSpace;
        }
    }

    void Update()
    {
        if (targetPiece == null) return;

        /*
        if (statusCanvas != null && mainCamera != null)
        {
            statusCanvas.transform.rotation = Quaternion.LookRotation(statusCanvas.transform.position - mainCamera.transform.position);
        }
        还是不要跟随镜头了
        */

        UpdateStatusIcons();
    }

    private void UpdateStatusIcons()
    {
        SetIconState("Stun", targetPiece.IsStunned, stunIconPrefab);

        bool showRoot = targetPiece.IsRooted && !targetPiece.IsStunned;
        SetIconState("Root", showRoot, rootIconPrefab);

        SetIconState("Daze", targetPiece.GetDazeStacks() > 0, dazeIconPrefab);

        UpdateShieldIcon();
        UpdateDoTIcon();

        SetIconState("Buff_Defense", targetPiece.IsProtectedBySunwell, buffDefenseIconPrefab);
        SetIconState("Buff_Attack", targetPiece.IsBuffedByHeartOfMountain(), buffAttackIconPrefab);
    }

    private void SetIconState(string key, bool isActive, GameObject prefab)
    {
        if (prefab == null) return;

        if (isActive)
        {
            if (!activeIcons.ContainsKey(key))
            {
                GameObject icon = Instantiate(prefab, iconContainer);
                activeIcons.Add(key, icon);
            }
        }
        else
        {
            if (activeIcons.ContainsKey(key))
            {
                Destroy(activeIcons[key]);
                activeIcons.Remove(key);
            }
        }
    }

    private void UpdateShieldIcon()
    {
        int shieldVal = targetPiece.GetShieldValue();
        bool hasShield = shieldVal > 0;
        string key = "Shield";

        if (hasShield)
        {
            if (!activeIcons.ContainsKey(key))
            {
                if (shieldIconPrefab != null)
                {
                    GameObject icon = Instantiate(shieldIconPrefab, iconContainer);
                    activeIcons.Add(key, icon);
                }
            }

            if (activeIcons.ContainsKey(key))
            {
                TextMeshProUGUI text = activeIcons[key].GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = shieldVal.ToString();
                }
            }
        }
        else
        {
            if (activeIcons.ContainsKey(key))
            {
                Destroy(activeIcons[key]);
                activeIcons.Remove(key);
            }
        }
    }

    private void UpdateDoTIcon()
    {
        bool hasDoT = targetPiece.HasActiveDoT();
        string key = "DoT";

        if (hasDoT)
        {
            var dotType = targetPiece.GetFirstDoTType();

            if (currentDisplayedDoTType != dotType)
            {
                if (activeIcons.ContainsKey(key))
                {
                    Destroy(activeIcons[key]);
                    activeIcons.Remove(key);
                }
                currentDisplayedDoTType = dotType;
            }

            if (!activeIcons.ContainsKey(key))
            {
                GameObject prefabToUse = null;

                if (dotType.HasValue)
                {
                    switch (dotType.Value)
                    {
                        case DamageType.Fire:
                            prefabToUse = dotFireIconPrefab;
                            break;
                        case DamageType.Arcane:
                            prefabToUse = dotArcaneIconPrefab;
                            break;
                        case DamageType.Holy:
                            prefabToUse = dotHolyIconPrefab;
                            break;
                        case DamageType.Physical:
                            prefabToUse = dotPhysicalIconPrefab;
                            break;
                        default:
                            prefabToUse = dotFireIconPrefab;
                            break;
                    }
                }

                if (prefabToUse != null)
                {
                    GameObject icon = Instantiate(prefabToUse, iconContainer);
                    activeIcons.Add(key, icon);
                }
            }
        }
        else
        {
            if (activeIcons.ContainsKey(key))
            {
                Destroy(activeIcons[key]);
                activeIcons.Remove(key);
            }
            currentDisplayedDoTType = null;
        }
    }
}