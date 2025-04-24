using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Transform))]
public class BlocksSpawner : MonoBehaviour
{
    [Header("Shape Configuration")]
    [SerializeField] private List<GameObject> shapePrefabs = new List<GameObject>();
    [SerializeField] private List<Color> availableColors = new List<Color>();
    [SerializeField] private GridSpawner gridSpawner;
    [SerializeField] private int maxPlacementAttempts = 100;

    private bool[,] gridOccupied;
    private int columns, rows;
    private float tileSize;
    private Vector3[,] gridPositions;

    private void Start()
    {
        if (!ValidateReferences())
            return;

        InitializeGridData();
        SpawnShapes();
    }

    private bool ValidateReferences()
    {
        if (gridSpawner == null)
        {
            Debug.LogError("GridSpawner reference is missing in BlocksSpawner!");
            return false;
        }

        if (shapePrefabs.Count == 0)
        {
            Debug.LogError("No shape prefabs assigned to BlocksSpawner!");
            return false;
        }

        if (availableColors.Count == 0)
        {
            Debug.LogWarning("No colors defined in BlocksSpawner. Using default color.");
            availableColors.Add(Color.white);
        }

        return true;
    }

    private void InitializeGridData()
    {
        columns = gridSpawner.Columns;
        rows = gridSpawner.Rows;
        tileSize = gridSpawner.TileSize;
        gridPositions = gridSpawner.GridPositions;
        gridOccupied = new bool[columns, rows];
    }

    private void SpawnShapes()
    {
        int totalGridCells = columns * rows;
        int shapesToSpawn = CalculateShapesToSpawn(totalGridCells);

        List<GameObject> shuffledShapes = new List<GameObject>(shapePrefabs);
        List<Color> shuffledColors = new List<Color>(availableColors);

        ShuffleList(shuffledShapes);
        ShuffleList(shuffledColors);

        int shapesToActuallySpawn = Mathf.Min(shapesToSpawn, shuffledShapes.Count, shuffledColors.Count);
        int shapesPlaced = 0;

        for (int i = 0; i < shapesToActuallySpawn; i++)
        {
            GameObject shapePrefab = shuffledShapes[i];
            Color shapeColor = shuffledColors[i];

            if (TryPlaceShape(shapePrefab, shapeColor))
            {
                shapesPlaced++;
            }
        }

        Debug.Log($"Successfully placed {shapesPlaced} shapes out of {shapesToActuallySpawn} attempted");
    }

    private int CalculateShapesToSpawn(int totalGridCells)
    {
        if (totalGridCells <= 9) return 1;    
        if (totalGridCells <= 16) return 2;    
        if (totalGridCells <= 36) return 3;     
        if (totalGridCells <= 64) return 4;      
        return 5;                              
    }

    private bool TryPlaceShape(GameObject shapePrefab, Color shapeColor)
    {
        for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
        {
            Vector2Int randomPos = new Vector2Int(
                Random.Range(0, columns),
                Random.Range(0, rows)
            );

            if (PlaceShape(shapePrefab, randomPos, shapeColor))
            {
                return true;
            }
        }

        Debug.LogWarning($"Could not place shape after {maxPlacementAttempts} attempts: {shapePrefab.name}");
        return false;
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    private bool PlaceShape(GameObject shapePrefab, Vector2Int gridPos, Color shapeColor)
    {
        GameObject tempShape = Instantiate(shapePrefab);
        tempShape.SetActive(false);
        tempShape.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        List<Vector2Int> blockOffsets = CalculateBlockOffsets(tempShape);

        if (!CanPlaceAtPosition(gridPos, blockOffsets))
        {
            Destroy(tempShape);
            return false;
        }

        foreach (Vector2Int offset in blockOffsets)
        {
            Vector2Int finalPos = new Vector2Int(gridPos.x + offset.x, gridPos.y + offset.y);
            if (IsValidGridPosition(finalPos))
            {
                gridOccupied[finalPos.x, finalPos.y] = true;
            }
        }

        tempShape.transform.position = gridPositions[gridPos.x, gridPos.y];
        tempShape.transform.SetParent(transform);
        tempShape.SetActive(true);
        tempShape.name = $"Shape_{gridPos.x}_{gridPos.y}";

        ApplyColorToBlocks(tempShape, shapeColor);

        return true;
    }

    private List<Vector2Int> CalculateBlockOffsets(GameObject shape)
    {
        List<Vector2Int> blockOffsets = new List<Vector2Int>();

        foreach (Transform child in shape.transform)
        {
            // Convert local position to world space
            Vector3 worldPos = shape.transform.TransformPoint(child.localPosition);

            // Calculate grid offset based on tileSize
            int gridX = Mathf.RoundToInt(worldPos.x / tileSize);
            int gridZ = Mathf.RoundToInt(worldPos.z / tileSize);

            Vector2Int offset = new Vector2Int(gridX, gridZ);
            if (!blockOffsets.Contains(offset))
            {
                blockOffsets.Add(offset);
            }
        }

        return blockOffsets;
    }

    private bool CanPlaceAtPosition(Vector2Int gridPos, List<Vector2Int> blockOffsets)
    {
        foreach (Vector2Int offset in blockOffsets)
        {
            Vector2Int finalPos = new Vector2Int(gridPos.x + offset.x, gridPos.y + offset.y);

            if (!IsValidGridPosition(finalPos) || gridOccupied[finalPos.x, finalPos.y])
            {
                return false;
            }
        }
        return true;
    }

    private bool IsValidGridPosition(Vector2Int pos)
    {
        return pos.x >= 0 && pos.y >= 0 && pos.x < columns && pos.y < rows;
    }

    private void ApplyColorToBlocks(GameObject shape, Color color)
    {
        foreach (Transform child in shape.transform)
        {
            MeshRenderer blockRenderer = child.GetComponent<MeshRenderer>();
            if (blockRenderer != null)
            {
                // Create a new material instance to avoid modifying the shared material
                blockRenderer.material = new Material(blockRenderer.material);
                blockRenderer.material.color = color;
            }
        }
    }
}