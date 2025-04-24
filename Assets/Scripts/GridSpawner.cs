using UnityEngine;

[RequireComponent(typeof(Transform))]
public class GridSpawner : MonoBehaviour
{
    [Header("Grid Configuration")]
    [SerializeField] private Sprite[] tileSprites;
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private int minGridSize = 4;
    [SerializeField] private int maxGridSize = 7;
    [SerializeField] private float tileSize = 1f;

    private int columns;
    private int rows;
    private Vector3[,] gridPositions;

    public int Columns => columns;
    public int Rows => rows;
    public float TileSize => tileSize;
    public Vector3[,] GridPositions => gridPositions;

    private void Awake()
    {
        GenerateGrid();
    }

    private void GenerateGrid()
    {
        columns = Random.Range(minGridSize, maxGridSize + 1);
        rows = columns;

        gridPositions = new Vector3[columns, rows];

        float offsetX = (columns - 1) * tileSize / 2f;
        float offsetZ = (rows - 1) * tileSize / 2f;

        for (int x = 0; x < columns; x++)
        {
            for (int z = 0; z < rows; z++)
            {
                Vector3 position = new Vector3(
                    x * tileSize - offsetX,
                    0,
                    z * tileSize - offsetZ
                );

                gridPositions[x, z] = position;
                SpawnTile(position, x, z);
            }
        }
    }

    private void SpawnTile(Vector3 position, int x, int z)
    {
        if (tilePrefab == null || tileSprites == null || tileSprites.Length == 0)
        {
            Debug.LogError("Tile prefab or sprites not assigned in GridSpawner.");
            return;
        }

        Quaternion rotation = Quaternion.Euler(90f, 0f, 0f);
        GameObject tile = Instantiate(tilePrefab, position, rotation, transform);

        int spriteIndex = (x + z) % tileSprites.Length;
        SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sprite = tileSprites[spriteIndex];
        }

        tile.name = $"Tile_{x}_{z}";
    }

    public void ValidateParameters()
    {
        minGridSize = Mathf.Max(2, minGridSize);
        maxGridSize = Mathf.Max(minGridSize, maxGridSize);
        tileSize = Mathf.Max(0.1f, tileSize);
    }

    private void OnValidate()
    {
        ValidateParameters();
    }
}