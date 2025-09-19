using UnityEngine;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour
{
    [Header("Player Management")]
    public GameObject playerPrefab;
    public GameObject goalPrefab;

    private List<Player> allPlayers = new List<Player>();
    private Dictionary<int, Player> playersByID = new Dictionary<int, Player>();
    private List<GameObject> allGoals = new List<GameObject>();
    private PathInput pathInput;

    private static PlayerManager instance;
    public static PlayerManager Instance { get { return instance; } }
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            pathInput = FindFirstObjectByType<PathInput>(); // 추가
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SpawnPlayer(PlayerSpawnData playerData)
    {
        // 플레이어 스폰
        Vector3 playerWorldPos = GridManager.Instance.GridToWorld(playerData.startPosition);
        GameObject playerObj = Instantiate(playerPrefab, playerWorldPos, Quaternion.identity);

        Player player = playerObj.GetComponent<Player>();
        if (player != null)
        {
            player.playerID = playerData.playerID;
            player.playerColor = playerData.playerColor;

            allPlayers.Add(player);
            playersByID[playerData.playerID] = player;

            // PathInput에 캐시 추가
            if (pathInput != null)
            {
                pathInput.AddPlayerToCache(player);
            }
        }

        // 목표 스폰
        Vector3 goalWorldPos = GridManager.Instance.GridToWorld(playerData.goalPosition);
        GameObject goalObj = Instantiate(goalPrefab, goalWorldPos, Quaternion.identity);

        SpriteRenderer goalRenderer = goalObj.GetComponent<SpriteRenderer>();
        if (goalRenderer != null)
        {
            Color goalColor = playerData.playerColor;
            goalColor.a = 0.5f;
            goalRenderer.color = goalColor;
        }

        allGoals.Add(goalObj);
    }
    public void StopAllPlayers()
    {
        foreach (var player in allPlayers)
        {
            if (player != null)
            {
                player.ForceStop();
            }
        }
    }
    public PlayerSpawnData GetPlayerData(int playerID)
    {
        if (LevelManager.Instance?.currentLevelData?.players == null) return null;

        foreach (var playerData in LevelManager.Instance.currentLevelData.players)
        {
            if (playerData.playerID == playerID)
                return playerData;
        }
        return null;
    }
    public Player GetPlayerByID(int playerID)
    {
        return playersByID.ContainsKey(playerID) ? playersByID[playerID] : null;
    }
    public List<GameObject> GetAllGoals()
    {
        return new List<GameObject>(allGoals);
    }
    public List<Player> GetAllPlayers()
    {
        return new List<Player>(allPlayers);
    }

    public void ClearAllPlayers()
    {
        foreach (var player in allPlayers)
        {
            if (player != null) DestroyImmediate(player.gameObject);
        }
        allPlayers.Clear();
        playersByID.Clear();

        foreach (var goal in allGoals)
        {
            if (goal != null) DestroyImmediate(goal);
        }
        allGoals.Clear();
    }
}