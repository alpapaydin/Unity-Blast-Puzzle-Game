using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelScene : MonoBehaviour
{
    [SerializeField] private GridController gridController;
    [SerializeField] private SpriteRenderer gridBackground;
    [SerializeField] private LevelUI levelUI;
    [SerializeField] private GameObject failPopupPrefab;
    [SerializeField] private GameObject winPopupPrefab;
    [SerializeField] private float winPopupDelay = 2f;
    [SerializeField] private GameObject particleSystemPrefab;
    [SerializeField] private Vector2 backgroundPadding = new Vector2(0.1f,0.1f);
    [SerializeField] private int debugLevel = -1;

    [Header("Grid Item Prefabs")]
    [SerializeField] private Cube cubePrefab;
    [SerializeField] private GridItem boxPrefab;
    [SerializeField] private GridItem stonePrefab;
    [SerializeField] private GridItem vasePrefab;
    [SerializeField] private GridItem tntPrefab;

    [SerializeField] private AudioClip fallClip;
    [SerializeField] private AudioClip blastClip;
    [SerializeField] private AudioClip tntCreateClip;
    [SerializeField] private AudioClip tntExplodeClip;
    [SerializeField] private AudioClip bgmClip;

    private AudioSource audioSource;
    private AudioSource bgmPlayer;
    private LevelData currentLevelData;
    private int currentLevel;
    private int remainingMoves;
    private bool canInteract = true;
    private bool isGameOver = false;
    private Camera mainCamera;

    private static event System.Action OnGridUpdatesNeeded;

    public static void TriggerGridUpdate()
    {
        OnGridUpdatesNeeded?.Invoke();
    }

    private void Awake()
    {
        if (bgmPlayer == null)
        {
            bgmPlayer = gameObject.AddComponent<AudioSource>();
            bgmPlayer.loop = true;
            bgmPlayer.clip = bgmClip;
            bgmPlayer.Play();
        }
        audioSource = gameObject.AddComponent<AudioSource>();
        mainCamera = Camera.main;
        gridController.OnBlast += PlayBlastSound;
        gridController.OnTntCreated += PlayTntSound;
        gridController.OnTntExploded += PlayTntExplodeSound;
    }

    private void OnEnable()
    {
        OnGridUpdatesNeeded += HandleGridUpdates;
    }

    private void OnDisable()
    {
        OnGridUpdatesNeeded -= HandleGridUpdates;
    }

    private void Start()
    {
        if (debugLevel > 0 && GameManager.Instance == null) { LoadDebugLevel(); }
        else { InitializeLevel(); }
    }

    private void OnGridConstructed()
    {
        if (gridBackground != null)
        {
            Vector2 gridPos = gridController.GridPosition;
            Vector2 gridSize = gridController.GridSize;
            Vector2 paddedSize = new Vector2(
                gridSize.x + (backgroundPadding.x * 2),
                gridSize.y + (backgroundPadding.y * 2)
            );
            gridBackground.size = paddedSize;
            gridBackground.transform.position = new Vector3(
                gridPos.x - backgroundPadding.x + paddedSize.x / 2,
                gridPos.y - backgroundPadding.y + paddedSize.y / 2,
                gridBackground.transform.position.z
            );
            gridBackground.sortingOrder = -1;
        }
    }

    private void LoadDebugLevel()
    {
        string formattedNumber = debugLevel.ToString("D2");
        string path = Path.Combine(Application.dataPath, "Game", "Assets", "Data", "Levels", $"level_{formattedNumber}.json");

        if (File.Exists(path))
        {
            string jsonContent = File.ReadAllText(path);
            currentLevelData = JsonUtility.FromJson<LevelData>(jsonContent);
            currentLevel = debugLevel;
            remainingMoves = currentLevelData.move_count;

            InitializeGridAndItems();
        }
        else
        {
            InitializeLevel();
        }
    }

    private void InitializeLevel()
    {
        currentLevelData = GameManager.Instance.CurrentLevelData;
        currentLevel = currentLevelData.level_number;
        remainingMoves = currentLevelData.move_count;
        InitializeGridAndItems();
    }

    private void InitializeGridAndItems()
    {
        gridController.InitializeGrid(currentLevelData.grid_width, currentLevelData.grid_height);
        SpawnGridItems();
        gridController.UpdateAllCubeStates();
        levelUI.InitializeGoals(currentLevelData);
        levelUI.UpdateMoveCount(remainingMoves);
    }

    private void SpawnGridItems()
    {
        for (int i = 0; i < currentLevelData.grid.Length; i++)
        {
            int x = i % currentLevelData.grid_width;
            int y = i / currentLevelData.grid_width;
            string itemType = currentLevelData.grid[i];
            SpawnGridItem(itemType, x, y);
        }
    }

    private void SpawnGridItem(string type, int x, int y)
    {
        GridItem prefab = type switch
        {
            "bo" => boxPrefab,
            "s" => stonePrefab,
            "v" => vasePrefab,
            "t" => tntPrefab,
            _ => cubePrefab
        };

        if (prefab != null)
        {
            GridItem item = gridController.SpawnGridItem(prefab, x, y);
            if (item is Cube cube)
            {
                Cube.CubeColor color = type switch
                {
                    "r" => Cube.CubeColor.Red,
                    "g" => Cube.CubeColor.Green,
                    "b" => Cube.CubeColor.Blue,
                    "y" => Cube.CubeColor.Yellow,
                    "rand" => GetRandomCubeColor(),
                    _ => Cube.CubeColor.Red
                };
                cube.SetColor(color);
            }
        }
    }

    private void Update()
    {
        if (!canInteract || isGameOver) return;
        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }
    }

    private void HandleClick()
    {
        Vector2 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);
        if (hit.collider != null)
        {
            GridItem clickedItem = hit.collider.transform.GetComponentInParent<GridItem>();
            if (clickedItem != null)
            {
                ProcessItemClick(clickedItem);
            }
        }
    }

    private void ProcessItemClick(GridItem clickedItem)
    {
        Vector2Int gridPosition = clickedItem.GridPosition;
        if (gridController.HandleTap(gridPosition))
        {
            remainingMoves--;
            levelUI.UpdateMoveCount(remainingMoves);
        }
    }

    private void HandleGridUpdates()
    {
        if (canInteract)
        {
            canInteract = false;
            StartCoroutine(ProcessGridUpdates());
        }
    }

    private IEnumerator ProcessGridUpdates()
    {
        yield return new WaitForSeconds(0.1f);
        bool hasChanges;
        do
        {
            hasChanges = false;
            bool needsSpawn = false;
            List<(GridItem item, Vector2Int from, Vector2Int to)> fallMoves = new List<(GridItem, Vector2Int, Vector2Int)>();
            for (int y = 1; y < currentLevelData.grid_height; y++)
            {
                for (int x = 0; x < currentLevelData.grid_width; x++)
                {
                    GridItem item = gridController.GetGridItem(x, y);
                    if (item != null && item.CanFall())
                    {
                        int lowestY = y;
                        while (lowestY > 0 && gridController.GetGridItem(x, lowestY - 1) == null)
                        {
                            lowestY--;
                        }

                        if (lowestY < y)
                        {
                            fallMoves.Add((item, new Vector2Int(x, y), new Vector2Int(x, lowestY)));
                            hasChanges = true;
                        }
                    }
                }
            }
            if (fallMoves.Count > 0)
            {
                audioSource.PlayOneShot(fallClip);
                foreach (var move in fallMoves)
                {
                    gridController.SetGridPosition(move.from.x, move.from.y, null);
                    gridController.MoveItemToPosition(move.item, move.to.x, move.to.y, true);
                }
                yield return new WaitForSeconds(gridController.FallDelay);
                while (gridController.AreItemsMoving())
                {
                    yield return null;
                }
            }
            for (int x = 0; x < currentLevelData.grid_width; x++)
            {
                if (gridController.GetGridItem(x, currentLevelData.grid_height - 1) == null)
                {
                    needsSpawn = true;
                    break;
                }
            }
            if (needsSpawn)
            {
                SpawnNewRow();
                hasChanges = true;
                yield return new WaitForSeconds(gridController.SpawnDelay);
            }
        } while (hasChanges);
        gridController.UpdateAllCubeStates();
        if (remainingMoves <= 0 || !gridController.HasValidMoves())
        {
            OnLevelFailed();
        }
        canInteract = true;
    }

    private void SpawnNewRow()
    {
        for (int x = 0; x < currentLevelData.grid_width; x++)
        {
            if (gridController.GetGridItem(x, currentLevelData.grid_height - 1) == null)
            {
                GridItem newCube = gridController.SpawnItemAboveGrid(cubePrefab, x, currentLevelData.grid_height);
                if (newCube is Cube cube)
                {
                    cube.SetColor(GetRandomCubeColor());
                }
            }
        }
    }

    public void OnObstacleDestroyed(string obstacleType)
    {
        levelUI.UpdateObstacleCount(obstacleType);
        if (levelUI.AreAllGoalsComplete())
        {
            OnLevelComplete();
        }
    }

    private Cube.CubeColor GetRandomCubeColor()
    {
        return (Cube.CubeColor)Random.Range(0, 4);
    }

    public TNT GetTNTPrefab()
    {
        return tntPrefab as TNT;
    }

    private void OnLevelComplete()
    {
        isGameOver = true;
        GameObject popup = Instantiate(winPopupPrefab, levelUI.transform);
        StartCoroutine(HandleWinSequence());
    }

    private IEnumerator HandleWinSequence()
    {
        yield return new WaitForSeconds(winPopupDelay);
        GameManager.Instance.SetCurrentLevel(currentLevel + 1);
        SceneManager.LoadScene("MainScene");
    }

    private void OnLevelFailed()
    {
        isGameOver = true;
        Instantiate(failPopupPrefab, levelUI.transform);
    }

    private void PlayBlastSound() { audioSource.PlayOneShot(blastClip); }
    private void PlayTntSound() { audioSource.PlayOneShot(tntCreateClip); }
    private void PlayTntExplodeSound() { audioSource.PlayOneShot(tntExplodeClip); }

}