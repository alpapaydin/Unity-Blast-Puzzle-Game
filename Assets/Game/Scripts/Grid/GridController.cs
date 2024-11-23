using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

public class GridController : MonoBehaviour
{
    [SerializeField] private GameObject backgroundTilePrefab;
    [SerializeField] private float screenMarginPercentage = 10f;
    [SerializeField] private Vector2 gridAlignment = new Vector2(0.5f, 0.5f);
    [SerializeField] private float topUIPercentage = 30f;
    [SerializeField] private float fallDuration = 0.15f;
    [SerializeField] private float fallDelay = 0.05f;
    [SerializeField] private float spawnDelay = 0.1f;
    [SerializeField, Range(0f, 1f)] private float itemScaleMultiplier = 1f;
    private int width;
    private int height;
    private Transform[,] backgroundTiles;
    private GridItem[,] gridItems;
    private HashSet<Vector2Int> reservedPositions = new HashSet<Vector2Int>();
    private HashSet<Cube> cubesNeedingTNTUpdate = new HashSet<Cube>();
    private const int BACKGROUND_LAYER = 0;
    private const int ITEM_LAYER = 1;
    private const int MOVING_ITEM_LAYER = 2;

    public event Action OnTntCreated;
    public event Action OnBlast;
    public event Action OnTntExploded;

    public LevelScene levelScene;
    public float FallDelay => fallDelay;
    public float SpawnDelay => spawnDelay;
    public Vector2 GridSize { get; private set; }
    public Vector2 GridPosition { get; private set; }
    public float CellSize { get; private set; }

    private void Awake()
    {
        levelScene = GetComponentInParent<LevelScene>();
    }

    public void InitializeGrid(int width, int height)
    {
        this.width = width;
        this.height = height;
        backgroundTiles = new Transform[width, height];
        gridItems = new GridItem[width, height];
        CreateBackgroundGrid();
    }

    private void CreateBackgroundGrid()
    {
        Camera mainCamera = Camera.main;
        float screenHeight = mainCamera.orthographicSize * 2f;
        float screenWidth = screenHeight * mainCamera.aspect;
        float marginX = screenWidth * (screenMarginPercentage / 100f);
        float marginY = screenHeight * (screenMarginPercentage / 100f);
        float uiHeight = screenHeight * (topUIPercentage / 100f);
        float usableScreenHeight = screenHeight - uiHeight;
        float availableWidth = screenWidth - (marginX * 2);
        float availableHeight = usableScreenHeight - (marginY * 2);
        float cellWidth = availableWidth / width;
        float cellHeight = availableHeight / height;
        float cellSize = Mathf.Min(cellWidth, cellHeight);
        CellSize = cellSize;
        float totalWidth = width * cellSize;
        float totalHeight = height * cellSize;
        GridSize = new Vector2(totalWidth, totalHeight);
        float screenLeft = -screenWidth / 2;
        float screenBottom = -screenHeight / 2;
        float remainingWidth = availableWidth - totalWidth;
        float remainingHeight = availableHeight - totalHeight;
        float usableBottom = screenBottom + marginY;
        float usableTop = screenHeight / 2 - uiHeight - marginY;
        float verticalSpace = usableTop - usableBottom - totalHeight;
        float startY = usableBottom + (verticalSpace * gridAlignment.y);
        float startX = screenLeft + marginX + (remainingWidth * gridAlignment.x);
        GridPosition = new Vector2(startX, startY);
        GameObject gridParent = new GameObject("GridTiles");
        gridParent.transform.SetParent(transform);
        gridParent.transform.localPosition = Vector3.zero;
        float backgroundSpriteSize = backgroundTilePrefab.GetComponent<SpriteRenderer>().bounds.size.x;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 position = new Vector3(
                    startX + (x * cellSize) + (cellSize / 2),
                    startY + (y * cellSize) + (cellSize / 2),
                    0f
                );
                GameObject tile = Instantiate(backgroundTilePrefab, position, Quaternion.identity, gridParent.transform);
                tile.name = $"BackgroundTile_{x}_{y}";
                float scale = cellSize / backgroundSpriteSize;
                tile.transform.localScale = new Vector3(scale, scale, 1f);
                tile.GetComponent<SpriteRenderer>().sortingOrder = BACKGROUND_LAYER;
                backgroundTiles[x, y] = tile.transform;
            }
        }
        SendMessageUpwards("OnGridConstructed", SendMessageOptions.DontRequireReceiver);
    }

    public GridItem SpawnGridItem(GridItem itemPrefab, int x, int y)
    {
        if (!IsValidPosition(x, y) || backgroundTiles[x, y] == null) return null;
        Vector3 tileCenter = backgroundTiles[x, y].position;
        GridItem item = Instantiate(itemPrefab, tileCenter, Quaternion.identity, transform);
        item.name = $"Item_{x}_{y}";
        float backgroundSize = backgroundTiles[x, y].GetComponent<SpriteRenderer>().bounds.size.x;
        float itemSize = item.spriteRenderer.bounds.size.x;
        float scale = backgroundSize / itemSize * itemScaleMultiplier;
        item.transform.localScale = new Vector3(scale, scale, 1f);
        item.SetSortingOrder(ITEM_LAYER);
        item.Initialize(new Vector2Int(x, y));
        gridItems[x, y] = item;
        return item;
    }

    public void SetGridPosition(int x, int y, GridItem item)
    {
        if (IsValidPosition(x, y))
        {
            gridItems[x, y] = item;
        }
    }

    public bool HandleTap(Vector2Int tapPosition)
    {
        if (!IsValidPosition(tapPosition.x, tapPosition.y))
            return false;
        GridItem tappedItem = gridItems[tapPosition.x, tapPosition.y];
        if (tappedItem == null)
            return false;
        if (tappedItem is Cube cube)
        {
            HashSet<Cube> connectedCubes = cube.GetConnectedCubes();
            if (connectedCubes.Count >= 2)
            {
                OnBlast?.Invoke();
                cube.TakeDamage(DamageType.Blast);
                return true;
            }
        }
        else if (tappedItem is TNT tnt)
        {
            tnt.TakeDamage(DamageType.TNT);
            return true;
        }
        return false;
    }

    public void TntExploded()
    {
        OnTntExploded?.Invoke();
    }

    public void SpawnTNT(Vector2Int position)
    {
        if (!IsValidPosition(position.x, position.y))
            return;
        if (gridItems[position.x, position.y] != null)
        {
            Destroy(gridItems[position.x, position.y].gameObject);
            gridItems[position.x, position.y] = null;
        }
        TNT tntPrefab = levelScene.GetTNTPrefab();
        GridItem newTNT = SpawnGridItem(tntPrefab, position.x, position.y);
        if (newTNT != null)
        {
            gridItems[position.x, position.y] = newTNT;
        }
        OnTntCreated?.Invoke();
    }

    public void MoveItemToPosition(GridItem item, int x, int y, bool isMoving = false)
    {
        if (!IsValidPosition(x, y)) return;
        gridItems[x, y] = item;
        float distance = Mathf.Abs(item.GridPosition.y - y);
        float adjustedDuration = fallDuration * Mathf.Sqrt(distance);
        StartCoroutine(FallToPosition(item, x, y, adjustedDuration));
        item.SetSortingOrder(isMoving ? MOVING_ITEM_LAYER : ITEM_LAYER);
    }

    private IEnumerator FallToPosition(GridItem item, int x, int y, float duration)
    {
        Vector3 startPos = item.transform.position;
        Vector3 endPos = backgroundTiles[x, y].position;
        float elapsed = 0;
        item.isMoving = true;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float easedT = t * t;
            item.transform.position = Vector3.Lerp(startPos, endPos, easedT);
            yield return null;
        }
        item.transform.position = endPos;
        item.GridPosition = new Vector2Int(x, y);
        item.isMoving = false;
        item.SetSortingOrder(ITEM_LAYER);
    }

    public bool HasValidMoves()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (gridItems[x, y] is Cube cube && cube.CheckMatchable())
                {
                    return true;
                }
            }
        }
        return false;
    }

    private int FindTopEmptyPosition(int x)
    {
        for (int y = height - 1; y >= 0; y--)
        {
            if (GetGridItem(x, y) == null)
            {
                return y;
            }
        }
        return -1;
    }

    public GridItem SpawnItemAboveGrid(GridItem itemPrefab, int x, int spawnY)
    {
        if (!IsValidPosition(x, 0)) return null;
        int targetY = FindTopEmptyPosition(x);
        if (targetY < 0) return null;
        Vector3 spawnPosition = GetSpawnPosition(x, spawnY);
        GridItem item = Instantiate(itemPrefab, spawnPosition, Quaternion.identity, transform);
        item.name = $"Item_{x}_{spawnY}";
        float backgroundSize = backgroundTiles[x, 0].GetComponent<SpriteRenderer>().bounds.size.x;
        float itemSize = item.spriteRenderer.bounds.size.x;
        float scale = backgroundSize / itemSize * itemScaleMultiplier;
        item.transform.localScale = new Vector3(scale, scale, 1f);
        item.Initialize(new Vector2Int(x, targetY));
        item.SetSortingOrder(MOVING_ITEM_LAYER);
        gridItems[x, targetY] = item;
        StartCoroutine(FallToPosition(item, x, targetY));
        return item;
    }

    private IEnumerator FallToPosition(GridItem item, int x, int targetY)
    {
        Vector3 startPos = item.transform.position;
        Vector3 endPos = backgroundTiles[x, targetY].position;
        float elapsed = 0;
        item.isMoving = true;
        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fallDuration;
            float easedT = 1 - (1 - t) * (1 - t);
            item.transform.position = Vector3.Lerp(startPos, endPos, easedT);
            yield return null;
        }
        item.transform.position = endPos;
        item.isMoving = false;
        item.SetSortingOrder(ITEM_LAYER);
    }

    private Vector3 GetSpawnPosition(int x, int spawnY)
    {
        float xPos = backgroundTiles[x, 0].position.x;
        float yOffset = CellSize * (spawnY - height + 1);
        float yPos = backgroundTiles[x, height - 1].position.y + yOffset;
        return new Vector3(xPos, yPos, 0f);
    }

    public void UpdateAllCubeStates()
    {
        // reset all TNT indicators
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (gridItems[x, y] is Cube cube)
                {
                    cube.CanCreateTNT = false;
                }
            }
        }
        // check for TNT-eligible groups
        HashSet<Cube> processedCubes = new HashSet<Cube>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (gridItems[x, y] is Cube cube && !processedCubes.Contains(cube))
                {
                    HashSet<Cube> connectedGroup = cube.GetConnectedCubes();
                    if (connectedGroup.Count >= 5)
                    {
                        foreach (var groupCube in connectedGroup)
                        {
                            groupCube.CanCreateTNT = true;
                        }
                    }
                    processedCubes.UnionWith(connectedGroup);
                }
            }
        }
    }

    public bool AreItemsMoving()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GridItem item = GetGridItem(x, y);
                if (item != null && item.IsMoving)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public GridItem GetGridItem(int x, int y)
    {
        return IsValidPosition(x, y) ? gridItems[x, y] : null;
    }

    public Transform GetSlotTransform(int x, int y)
    {
        return IsValidPosition(x, y) ? backgroundTiles[x, y] : null;
    }

    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }
}