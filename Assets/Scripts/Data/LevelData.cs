using UnityEngine;

[System.Serializable]
public class PlayerSpawnData
{
    public int playerID;
    public Vector2Int startPosition;
    public Vector2Int goalPosition;
    public Color playerColor;
    public int maxSelections;
}

[System.Serializable]
public class TileMapData
{
    public Vector2Int position;
    public TileType tileType;
    public int playerID = -1; // 시작점/목표점일 때만 사용 (-1은 해당 없음)
}

[CreateAssetMenu(fileName = "New Level Data", menuName = "PathPath/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level Info")]
    public int levelID;
    public string levelName;
    public int difficulty = 1;

    [Header("Grid Settings")]
    public int gridWidth = 6;
    public int gridHeight = 12;
    public Vector3 gridCenter = new Vector3(2.5f, 5.5f, 0f);

    [Header("Players")]
    public PlayerSpawnData[] players;

    [Header("Tile Map")]
    public TileMapData[] tileMap;

    [Header("Game Rules")]
    public float timeLimit = 60f; // 계획 시간 제한
    public bool allowDiagonalMovement = false;

    // 유효성 검증
    public bool IsValid()
    {
        if (players == null || players.Length == 0) return false;
        if (tileMap == null || tileMap.Length == 0) return false;

        foreach (var player in players)
        {
            if (player.maxSelections <= 0) return false;
        }

        return true;
    }

    // 특정 위치의 타일 타입 반환
    public TileType GetTileTypeAt(Vector2Int position)
    {
        foreach (var tile in tileMap)
        {
            if (tile.position == position)
                return tile.tileType;
        }
        return TileType.Blocked; // 기본값은 막힌 타일
    }

    // 이동 가능한 타일인지 확인
    public bool IsWalkableTile(Vector2Int position)
    {
        TileType tileType = GetTileTypeAt(position);
        return tileType == TileType.Walkable || tileType == TileType.Start || tileType == TileType.Goal;
    }
}