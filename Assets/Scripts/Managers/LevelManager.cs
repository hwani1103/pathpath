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

    private Dictionary<TileType, TileBase> tileAssets = new Dictionary<TileType, TileBase>();

    // 싱글톤 패턴
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

    // 나중에 Level 디자인할 때, 우클릭으로 Grid좌표확인하면서 Player랑 Goal위치 지정하게하는 메서드
    //void Update()
    //{
    //    // 우클릭으로 Grid 좌표 확인
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
        // TileData를 실제 TileAsset으로 변환하는 작업
        // 지금은 임시로 기존 타일들 사용
        var walkableTiles = Resources.FindObjectsOfTypeAll<TileBase>();

        // 임시 구현: 첫 번째 타일을 각 타입으로 지정
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

        // 기존 레벨 정리
        ClearLevel();

        // Grid 설정 업데이트
        UpdateGridSettings();

        // 타일맵 생성
        GenerateTilemap();

        // 플레이어 및 목표 스폰
        SpawnPlayersAndGoals();

        Debug.Log($"Level {levelData.levelID} loaded: {levelData.levelName}");
    }

    void ClearLevel()
    {
        // 기존 타일맵 클리어
        if (mainTilemap != null)
        {
            mainTilemap.SetTilesBlock(mainTilemap.cellBounds, new TileBase[mainTilemap.cellBounds.size.x * mainTilemap.cellBounds.size.y * mainTilemap.cellBounds.size.z]);
        }

        // PathInput LineRenderer 정리
        PathInput pathInput = FindFirstObjectByType<PathInput>();
        if (pathInput != null)
        {
            pathInput.ClearAllLineRenderers();
        }

        // PlayerManager로 플레이어/목표 정리 (기존 코드 교체)
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
        // Tilemap 스캔
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
                    tileData.tileType = TileType.Walkable; // 단순화
                    tileData.playerID = -1;
                    tileDataList.Add(tileData);
                }
            }
        }
        Debug.Log($"Generated {tileDataList.Count} tiles from Tilemap");
        return CreateLevelDataAsset(levelName, tileDataList);
    }

    // LevelData ScriptableObject 생성
    private LevelData CreateLevelDataAsset(string levelName, List<TileMapData> tiles)
    {
        LevelData newLevelData = ScriptableObject.CreateInstance<LevelData>();

        // GridManager 안전 확인
        GridManager gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError("GridManager not found!");
            return null;
        }

        // 기본 설정
        newLevelData.levelName = levelName;
        newLevelData.gridWidth = gridManager.gridWidth;
        newLevelData.gridHeight = gridManager.gridHeight;
        newLevelData.gridCenter = gridManager.gridCenter;

        // 타일 데이터 설정
        newLevelData.tileMap = tiles.ToArray();

        // 기본 플레이어 데이터 (수동으로 설정 필요)
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