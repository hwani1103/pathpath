using UnityEngine;
using System.Collections.Generic;

public class PlayerCompletionManager : MonoBehaviour
{
    private HashSet<int> completedPlayerIDs = new HashSet<int>();

    void Start()
    {
        PathInput.OnPlayerCompleted += HandlePlayerCompleted;
    }

    void OnDestroy()
    {
        PathInput.OnPlayerCompleted -= HandlePlayerCompleted;
    }

    public void HandlePlayerCompleted(Player player)
    {
        if (player == null) return;
        completedPlayerIDs.Add(player.playerID);
    }

    public bool IsPlayerCompleted(int playerID)
    {
        return completedPlayerIDs.Contains(playerID);
    }

    public void ResetAllPlayers()
    {
        completedPlayerIDs.Clear();
    }
}