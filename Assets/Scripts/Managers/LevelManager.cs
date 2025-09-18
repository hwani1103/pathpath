using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    [Header("Current Level")]
    public LevelData currentLevelData;

    [Header("Tile References")]
    public TileData walkableTileData;
    public TileData blockedTileData;
    public TileData startTileData;
    public TileData goalTileData;

    [Header("Tilemap References")]
    public Tilemap mainTilemap;
    public TilemapRenderer tilemapRenderer;

    [Header("Player Prefab")]
    public GameObject playerPrefab;
    public GameObject goalPrefab;

    private Dictionary<TileType, TileBase> tileAssets = new Dictionary<TileType, TileBase>();
    private List<Player> spawnedPlayers = new List<Player>();
    private List<GameObject> spawnedGoals = new List<GameObject>();

    // �̱��� ����
    private static LevelManager instance;
    public static LevelManager Instance { get { return instance; } }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // ���߿� Level �������� ��, ��Ŭ������ Grid��ǥȮ���ϸ鼭 Player�� Goal��ġ �����ϰ��ϴ� �޼���
    //void Update()
    //{
    //    // ��Ŭ������ Grid ��ǥ Ȯ��
    //    if (Input.GetMouseButtonDown(1))
    //    {
    //        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    //        mouseWorldPos.z = 0;
    //        Vector2Int gridPos = GridManager.Instance.WorldToGrid(mouseWorldPos);
    //        Debug.Log($"Mouse Grid Position: {gridPos}");
    //    }
    //}
    void Start()
    {
        InitializeTileAssets();

        if (currentLevelData != null)
        {
            LoadLevel(currentLevelData);
        }
    }

    void InitializeTileAssets()
    {
        // TileData�� ���� TileAsset���� ��ȯ�ϴ� �۾�
        // ������ �ӽ÷� ���� Ÿ�ϵ� ���
        var walkableTiles = Resources.FindObjectsOfTypeAll<TileBase>();

        // �ӽ� ����: ù ��° Ÿ���� �� Ÿ������ ����
        if (walkableTiles.Length > 0)
        {
            tileAssets[TileType.Walkable] = walkableTiles[0];
            tileAssets[TileType.Blocked] = walkableTiles[0];
            tileAssets[TileType.Start] = walkableTiles[0];
            tileAssets[TileType.Goal] = walkableTiles[0];
        }
    }

    public void LoadLevel(LevelData levelData)
    {
        if (!levelData.IsValid())
        {
            Debug.LogError("Invalid level data!");
            return;
        }

        currentLevelData = levelData;

        // ���� ���� ����
        ClearLevel();

        // Grid ���� ������Ʈ
        UpdateGridSettings();

        // Ÿ�ϸ� ����
        GenerateTilemap();

        // �÷��̾� �� ��ǥ ����
        SpawnPlayersAndGoals();

        Debug.Log($"Level {levelData.levelID} loaded: {levelData.levelName}");
    }

    void ClearLevel()
    {
        // ���� Ÿ�ϸ� Ŭ����
        if (mainTilemap != null)
        {
            mainTilemap.SetTilesBlock(mainTilemap.cellBounds, new TileBase[mainTilemap.cellBounds.size.x * mainTilemap.cellBounds.size.y * mainTilemap.cellBounds.size.z]);
        }

        // ���� �÷��̾�� ����
        foreach (var player in spawnedPlayers)
        {
            if (player != null) DestroyImmediate(player.gameObject);
        }
        spawnedPlayers.Clear();

        // ���� ��ǥ�� ����
        foreach (var goal in spawnedGoals)
        {
            if (goal != null) DestroyImmediate(goal);
        }
        spawnedGoals.Clear();
    }

    void UpdateGridSettings()
    {
        if (GridManager.Instance != null)
        {
            GridManager gridManager = GridManager.Instance;
            gridManager.gridWidth = currentLevelData.gridWidth;
            gridManager.gridHeight = currentLevelData.gridHeight;
            gridManager.gridCenter = currentLevelData.gridCenter;
        }
    }

    void GenerateTilemap()
    {
        if (mainTilemap == null) return;

        foreach (var tileData in currentLevelData.tileMap)
        {
            Vector3Int cellPosition = new Vector3Int(tileData.position.x, tileData.position.y, 0);

            if (tileAssets.ContainsKey(tileData.tileType))
            {
                mainTilemap.SetTile(cellPosition, tileAssets[tileData.tileType]);
            }
        }
    }

    void SpawnPlayersAndGoals()
    {
        foreach (var playerData in currentLevelData.players)
        {
            // �÷��̾� ����
            Vector3 playerWorldPos = GridManager.Instance.GridToWorld(playerData.startPosition);
            GameObject playerObj = Instantiate(playerPrefab, playerWorldPos, Quaternion.identity);

            Player player = playerObj.GetComponent<Player>();
            if (player != null)
            {
                player.playerID = playerData.playerID;
                player.playerColor = playerData.playerColor;
                spawnedPlayers.Add(player);
            }

            // ��ǥ ����
            Vector3 goalWorldPos = GridManager.Instance.GridToWorld(playerData.goalPosition);
            GameObject goalObj = Instantiate(goalPrefab, goalWorldPos, Quaternion.identity);

            // ��ǥ ���� ����
            SpriteRenderer goalRenderer = goalObj.GetComponent<SpriteRenderer>();
            if (goalRenderer != null)
            {
                Color goalColor = playerData.playerColor;
                goalColor.a = 0.5f; // ������
                goalRenderer.color = goalColor;
            }

            spawnedGoals.Add(goalObj);
        }
    }

    public PlayerSpawnData GetPlayerData(int playerID)
    {
        foreach (var playerData in currentLevelData.players)
        {
            if (playerData.playerID == playerID)
                return playerData;
        }
        return null;
    }

    public bool IsValidPosition(Vector2Int gridPos)
    {
        return currentLevelData.IsWalkableTile(gridPos);
    }

    // Tilemap�� ��ĵ�ؼ� LevelData ����
    public LevelData GenerateLevelDataFromTilemap(string levelName = "Generated Level")
    {
        if (mainTilemap == null)
        {
            Debug.LogError("Main Tilemap is not assigned!");
            return null;
        }

        List<TileMapData> tileDataList = new List<TileMapData>();

        // Tilemap ��ĵ
        BoundsInt bounds = mainTilemap.cellBounds;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cellPos = new Vector3Int(x, y, 0);
                TileBase tile = mainTilemap.GetTile(cellPos);

                if (tile != null)
                {
                    TileMapData tileData = new TileMapData();
                    tileData.position = new Vector2Int(x, y);
                    tileData.tileType = TileType.Walkable; // �⺻��
                    tileData.playerID = -1;

                    tileDataList.Add(tileData);
                }
            }
        }

        Debug.Log($"Generated {tileDataList.Count} tiles from Tilemap");
        return CreateLevelDataAsset(levelName, tileDataList);
    }

    // LevelData ScriptableObject ����
    private LevelData CreateLevelDataAsset(string levelName, List<TileMapData> tiles)
    {
        LevelData newLevelData = ScriptableObject.CreateInstance<LevelData>();

        // GridManager ���� Ȯ��
        GridManager gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError("GridManager not found!");
            return null;
        }

        // �⺻ ����
        newLevelData.levelName = levelName;
        newLevelData.gridWidth = gridManager.gridWidth;
        newLevelData.gridHeight = gridManager.gridHeight;
        newLevelData.gridCenter = gridManager.gridCenter;

        // Ÿ�� ������ ����
        newLevelData.tileMap = tiles.ToArray();

        // �⺻ �÷��̾� ������ (�������� ���� �ʿ�)
        newLevelData.players = new PlayerSpawnData[]
        {
            new PlayerSpawnData
            {
                playerID = 1,
                startPosition = new Vector2Int(2, 2),
                goalPosition = new Vector2Int(2, 8),
                playerColor = Color.red,
                maxSelections = 3
            },
            new PlayerSpawnData
            {
                playerID = 2,
                startPosition = new Vector2Int(4, 2),
                goalPosition = new Vector2Int(4, 8),
                playerColor = Color.blue,
                maxSelections = 3
            }
        };

        return newLevelData;
    }

#if UNITY_EDITOR
    [UnityEngine.ContextMenu("Generate Level Data")]
    public void GenerateAndSaveLevelData()
    {
        LevelData generated = GenerateLevelDataFromTilemap("Generated_" + System.DateTime.Now.ToString("yyyyMMdd_HHmm"));

        if (generated != null)
        {
            string path = "Assets/Data/" + generated.levelName + ".asset";
            UnityEditor.AssetDatabase.CreateAsset(generated, path);
            UnityEditor.AssetDatabase.SaveAssets();

            Debug.Log($"Level Data saved to: {path}");
        }
    }
#endif
}