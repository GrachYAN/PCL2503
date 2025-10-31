using UnityEngine;

public class Square : MonoBehaviour
{
    private Renderer squareRenderer;
    private Color originalColor;

    void Start()
    {
        squareRenderer = GetComponent<Renderer>();
        originalColor = squareRenderer.material.color;
    }

    public void Highlight(Color highlightColor)
    {
        squareRenderer.material.color = highlightColor;
    }

    public void Unhighlight()
    {
        squareRenderer.material.color = originalColor;
    }
}