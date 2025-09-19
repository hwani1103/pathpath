using UnityEngine;
using System.Collections.Generic;

public class PathManager : MonoBehaviour
{
    private Dictionary<Player, List<Vector2Int>> playerPaths = new Dictionary<Player, List<Vector2Int>>();
    private List<Player> cachedPlayers = new List<Player>();

    void Start()
    {
        CacheAllPlayers();
    }

    public void AddPlayerToCache(Player player)
    {
        if (!cachedPlayers.Contains(player))
        {
            cachedPlayers.Add(player);
            InitializePlayerPath(player);
        }
    }

    void CacheAllPlayers()
    {
        cachedPlayers.Clear();
        Player[] allPlayers = FindObjectsByType<Player>(FindObjectsSortMode.None);

        foreach (Player player in allPlayers)
        {
            cachedPlayers.Add(player);
            InitializePlayerPath(player);
        }
    }

    void InitializePlayerPath(Player player)
    {
        if (!playerPaths.ContainsKey(player))
        {
            playerPaths[player] = new List<Vector2Int>();
        }
    }

    public List<Vector2Int> GetPlayerPath(Player player)
    {
        if (playerPaths.ContainsKey(player))
        {
            return playerPaths[player];
        }
        return new List<Vector2Int>();
    }

    public void SetPlayerPath(Player player, List<Vector2Int> path)
    {
        if (!playerPaths.ContainsKey(player))
        {
            playerPaths[player] = new List<Vector2Int>();
        }
        playerPaths[player] = new List<Vector2Int>(path);
    }

    public void AddPointToPath(Player player, Vector2Int point)
    {
        if (!playerPaths.ContainsKey(player))
        {
            playerPaths[player] = new List<Vector2Int>();
        }
        playerPaths[player].Add(point);
    }

    public void ClearPlayerPath(Player player)
    {
        if (playerPaths.ContainsKey(player))
        {
            playerPaths[player].Clear();
        }
    }

    public void ClearAllPaths()
    {
        foreach (var path in playerPaths.Values)
        {
            path.Clear();
        }
    }

    public bool AreAllPlayersComplete()
    {
        if (LevelManager.Instance?.currentLevelData?.players == null) return false;

        foreach (var playerData in LevelManager.Instance.currentLevelData.players)
        {
            Player playerObj = null;
            foreach (var kvp in playerPaths)
            {
                if (kvp.Key.playerID == playerData.playerID)
                {
                    playerObj = kvp.Key;
                    break;
                }
            }

            if (playerObj == null) return false;

            List<Vector2Int> path = playerPaths[playerObj];
            if (path.Count == 0) return false;

            Vector2Int lastPoint = path[path.Count - 1];
            if (lastPoint != playerData.goalPosition) return false;
        }

        return true;
    }

    public void ExecuteAllPaths()
    {
        foreach (var kvp in playerPaths)
        {
            Player player = kvp.Key;
            List<Vector2Int> fullPath = kvp.Value;

            if (fullPath.Count > 1)
            {
                List<Vector2Int> playerPath = new List<Vector2Int>(fullPath);
                playerPath.RemoveAt(0);

                player.SetPath(playerPath);
                player.StartMoving();
            }
        }

        // 실행 후 경로 데이터 정리
        playerPaths.Clear();
    }

    public bool IsAnyPlayerMoving()
    {
        foreach (Player player in cachedPlayers)
        {
            if (player != null && player.IsMoving()) return true;
        }
        return false;
    }
}