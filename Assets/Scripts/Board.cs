using UnityEngine;

public class Board : MonoBehaviour
{
    public int size = 8;
    void Start()
    {
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                GameObject square = GameObject.CreatePrimitive(PrimitiveType.Quad);
                square.transform.position = new Vector3(x, 0, y);
                square.transform.parent = transform;
                Square sq = square.AddComponent<Square>();
                sq.x = x;
                sq.y = y;
                square.GetComponent<Renderer>().material.color = (x + y) % 2 == 0 ? Color.white : Color.gray;
            }
        }
    }
}
