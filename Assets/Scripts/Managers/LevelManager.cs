using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    [Header("Current Level")]
    public LevelData currentLevelData;

    [Header("Tilemap References")]
    public Tilemap mainTilemap;
    public TilemapRenderer tilemapRenderer;

    [Header("Tile Mapping")]
    [SerializeField] private TileBase walkableTileAsset;  // �� �� �߰�
    [SerializeField] private TileBase blockedTileAsset;

    [Header("Level Generation Settings")]
    [SerializeField] private int numberOfPlayers = 2;
    [SerializeField] private PlayerEditorData[] playerSettings = new PlayerEditorData[2];

    [System.Serializable]
    public class PlayerEditorData
    {
        public Color playerColor = Color.red;
        public Vector2Int startPosition = Vector2Int.zero;
        public Vector2Int goalPosition = Vector2Int.zero;
        public int maxSelections = 3;
    }


    private Dictionary<TileType, TileBase> tileAssets = new Dictionary<TileType, TileBase>();

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

    void Start()
    {
        InitializeTileAssets();
        // currentLevelData ���� �ε� ����
        // GameManager�� ������ �����ϵ��� ����
    }

    void InitializeTileAssets()
    {
        tileAssets[TileType.Walkable] = walkableTileAsset;  // �� �� �߰�
        tileAssets[TileType.Blocked] = blockedTileAsset;
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
        // ù ��° ������ �ƴ� ���� Ÿ�ϸ� Ŭ����
        if (GameManager.Instance != null && GameManager.Instance.currentLevel > 1)
        {
            if (mainTilemap != null)
            {
                mainTilemap.SetTilesBlock(mainTilemap.cellBounds, new TileBase[mainTilemap.cellBounds.size.x * mainTilemap.cellBounds.size.y * mainTilemap.cellBounds.size.z]);
            }
        }

        // PathInput LineRenderer ����
        PathInput pathInput = FindFirstObjectByType<PathInput>();
        if (pathInput != null)
        {
            pathInput.ClearAllLineRenderers();
        }

        // PlayerManager�� �÷��̾�/��ǥ ����
        PlayerManager.Instance.ClearAllPlayers();
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
            PlayerManager.Instance.SpawnPlayer(playerData);
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

    public LevelData GenerateLevelDataFromTilemap(string levelName = "Generated Level")
    {
        if (mainTilemap == null)
        {
            Debug.LogError("Main Tilemap is not assigned!");
            return null;
        }

        List<TileMapData> tileDataList = new List<TileMapData>();
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
                    tileData.tileType = GetTileTypeFromAsset(tile);
                    tileData.playerID = -1;
                    tileDataList.Add(tileData);
                }
            }
        }

        Debug.Log($"Generated {tileDataList.Count} tiles from Tilemap");
        return CreateLevelDataAsset(levelName, tileDataList);
    }

    private TileType GetTileTypeFromAsset(TileBase tileAsset)
    {
        if (tileAsset == blockedTileAsset) return TileType.Blocked;

        return TileType.Walkable; // �������� ��� Walkable
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

        // Inspector���� ������ �÷��̾� ������ ���
        PlayerSpawnData[] spawnDataArray = new PlayerSpawnData[numberOfPlayers];
        for (int i = 0; i < numberOfPlayers && i < playerSettings.Length; i++)
        {
            spawnDataArray[i] = new PlayerSpawnData
            {
                playerID = i + 1,
                startPosition = playerSettings[i].startPosition,
                goalPosition = playerSettings[i].goalPosition,
                playerColor = playerSettings[i].playerColor,
                maxSelections = playerSettings[i].maxSelections
            };
        }
        newLevelData.players = spawnDataArray;

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