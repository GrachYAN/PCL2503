using UnityEngine;

public abstract class Piece : MonoBehaviour
{
    public int x;
    public int y;
    public bool isWhite;

    public virtual void Init(int x, int y, bool isWhite)
    {
        this.x = x;
        this.y = y;
        this.isWhite = isWhite;
        name = $"{GetType().Name} {(isWhite ? "White" : "Black")} ({x},{y})";
        GetComponent<Renderer>().material.color = isWhite ? new Color(0.95f, 0.85f, 0.2f) : Color.black;
    }

    public virtual void SetBoardPosition(int nx, int ny)
    {
        x = nx; y = ny;
        transform.position = new Vector3(nx, 0.5f, ny);
    }
    /*
     * 抽象基类-> 棋子
     * 属性：位置（x,y），颜色（isWhite）
     * TODO: Movement?
     */

}
