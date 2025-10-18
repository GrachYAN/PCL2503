using UnityEngine;
using System;


public class Chessboard : MonoBehaviour
{
    //logic
    [Header("Art stuff")]
    [SerializeField] private Material tileMaterial;

    private const int TILE_COUNT_X= 8;
    private const int TILE_COUNT_Y= 8;
    private GameObject[,] tiles;
    private void Awake()
    {
        GenerateALLTiles(1, TILE_COUNT_X, TILE_COUNT_Y);
    }

    private void GenerateALLTiles(float tileSize, int tileCountX, int tileCountY)
    {
        //NEW GAME OBJECT&MESH
        tiles = new GameObject[tileCountX, tileCountY];
        for ( int x = 0; x < tileCountX; x++)
        {
            for (int y = 0; y < tileCountY; y++)
            {
                tiles[x, y] = GenerateSingleTile(tileSize, x, y);
            }
        }
    }

    private GameObject GenerateSingleTile(float tileSize, int x, int y)
    {
        GameObject tileObject = new GameObject(string.Format("X:{0}, Y:{1}", x, y));
        tileObject.transform.parent = transform;   //OBJ gen beneth that; mesh move with obj

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

        Vector3[] vertices = new Vector3[4]; //4 vertices -> each corner
        vertices[0] = new Vector3(x * tileSize, 0, y * tileSize);
        vertices[1] = new Vector3(x * tileSize, 0, (y +1)* tileSize);
        vertices[2] = new Vector3((x +1)* tileSize, 0, y * tileSize);
        vertices[3] = new Vector3((x +1)* tileSize, 0, (y +1)* tileSize);

        int[] triangles = new int[6]
        {
            0,1,2,
            2,1,3
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();

        tileObject.AddComponent<BoxCollider>();

        return tileObject; 
    }
    /*
    private void Update()
    {
        if (!currentCamera || !boardActive)
        {
            currentCamera = Camera.main;
            return;
        }

        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover")))
        {
            // Get the indexes of the tile i've hit
            Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);

            // If we're hovering a tile after not hovering any tiles
            if (currentHover == -Vector2Int.one)
            {
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }

            // If we were already hovering a tile, change the previous one
            if (currentHover != hitPosition)
            {
                tiles[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Tile");
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }
            else
            {
                if (currentHover != -Vector2Int.one)
                {
                    tiles[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Tile");
                    currentHover = -Vector2Int.one;
                }
            }
        }
    }
    */

}
