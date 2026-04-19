using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [TextArea]
    public string content;

    public void SetContent(string newContent)
    {
        content = newContent;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (string.IsNullOrEmpty(content))
        {
            return;
        }

        TooltipSystem tooltipSystem = TooltipSystem.Instance;
        RectTransform targetRect = GetComponent<RectTransform>();
        if (tooltipSystem == null || targetRect == null)
        {
            return;
        }

        tooltipSystem.Show(content, targetRect);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipSystem.Instance != null)
        {
            TooltipSystem.Instance.Hide();
        }
    }
}
