using UnityEngine;

public class Board : MonoBehaviour
{
    [Header("棋盘尺寸")]
    [Range(2, 16)]
    public int size = 8;

    [Header("配色")]
    public Color lightColor = new Color(0.9f, 0.9f, 0.9f);
    public Color darkColor = new Color(0.35f, 0.35f, 0.35f);

    [Header("格子缩放")]
    public Vector2 squareScale = new Vector2(1f, 1f); // X/Z 缩放

    void Start()
    {
        GenerateSquares();
        CenterBoardUnderOrigin();
    }

    void GenerateSquares()
    {
        // 清空旧子物体（便于反复运行或参数调整后重新生成）
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            DestroyImmediate(transform.GetChild(i).gameObject);
#else
            Destroy(transform.GetChild(i).gameObject);
#endif
        }

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                GameObject square = GameObject.CreatePrimitive(PrimitiveType.Quad);
                square.name = $"Square_{x}_{y}";
                square.transform.SetParent(transform);

                // Quad 默认朝向+Z，将其旋转为水平面朝上
                square.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                // 位置与缩放
                square.transform.position = new Vector3(x, 0f, y);
                square.transform.localScale = new Vector3(squareScale.x, squareScale.y, 1f);

                // 组件与属性
                var s = square.AddComponent<Square>();
                s.x = x;
                s.y = y;

                var renderer = square.GetComponent<Renderer>();
                bool isLight = (x + y) % 2 == 0;
                renderer.material.color = isLight ? lightColor : darkColor;

                // 自带 MeshCollider（随 Primitive），保留即可，便于后续点击/射线检测
            }
        }
    }

    void CenterBoardUnderOrigin()
    {
        // 让棋盘整体居中到世界原点附近（视觉更整齐）
        float offsetX = (size - 1) * 0.5f;
        float offsetY = (size - 1) * 0.5f;

        // 将父对象移到负偏移，子对象局部不变；也可改为移动每个格子
        transform.position = new Vector3(-offsetX, 0f, -offsetY);
    }
}
