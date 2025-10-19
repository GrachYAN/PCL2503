using UnityEngine;

public class Square : MonoBehaviour
{
    public int x;
    public int y;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.01f, new Vector3(0.95f, 0.01f, 0.95f));
    }
    //ÏÔÊ¾¸ñ×Ó±ß¿ò
}
