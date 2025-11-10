using UnityEngine;

public class CursorManager : MonoBehaviour
{
    [SerializeField] private Texture2D customCursor;
    [SerializeField] private CursorMode cursorMode = CursorMode.Auto; // Auto is usually fine
    [SerializeField] private Vector2 hotSpot = Vector2.zero; // Set this in Inspector based on the image

    void Start()
    {
        if (customCursor != null)
        {
            // Apply the custom cursor
            Cursor.SetCursor(customCursor, hotSpot, cursorMode);
        }
        else
        {
            Debug.LogWarning("Custom cursor texture is not assigned in CursorManager.");
        }
    }
}