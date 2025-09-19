using UnityEngine;
using System.Collections.Generic;

public class PathValidator : MonoBehaviour
{
    public bool IsValidPathPoint(Vector2Int gridPos, Player selectedPlayer, List<Vector2Int> currentPath)
    {
        if (!HasTileAtPosition(gridPos)) return false;

        Vector2Int lastPos = GetLastPathPoint(selectedPlayer, currentPath);
        if (!IsStraightLine(lastPos, gridPos)) return false;

        if (IsOtherPlayerGoal(gridPos, selectedPlayer)) return false;
        if (IsOtherPlayerStart(gridPos, selectedPlayer)) return false;
        if (!CanAddMorePoints(selectedPlayer, currentPath)) return false;
        if (IsDuplicatePoint(gridPos, currentPath)) return false;
        if (!CanReachGoalAfterThisSelection(gridPos, selectedPlayer, currentPath)) return false;

        return true;
    }

    public bool HasTileAtPosition(Vector2Int gridPos)
    {
        if (LevelManager.Instance?.mainTilemap == null) return false;

        Vector3 worldPos = GridManager.Instance.GridToWorld(gridPos);
        Vector3Int tilemapPos = LevelManager.Instance.mainTilemap.WorldToCell(worldPos);
        return LevelManager.Instance.mainTilemap.GetTile(tilemapPos) != null;
    }

    public bool IsStraightLine(Vector2Int from, Vector2Int to)
    {
        Vector2Int diff = to - from;
        return (diff.x == 0 && diff.y != 0) || (diff.y == 0 && diff.x != 0);
    }

    public bool IsOtherPlayerGoal(Vector2Int gridPos, Player selectedPlayer)
    {
        if (LevelManager.Instance?.currentLevelData?.players == null) return false;

        foreach (var playerData in LevelManager.Instance.currentLevelData.players)
        {
            if (playerData.playerID != selectedPlayer.playerID &&
                playerData.goalPosition == gridPos)
            {
                return true;
            }
        }
        return false;
    }

    public bool IsOtherPlayerStart(Vector2Int gridPos, Player selectedPlayer)
    {
        if (LevelManager.Instance?.currentLevelData?.players == null) return false;

        foreach (var playerData in LevelManager.Instance.currentLevelData.players)
        {
            if (playerData.playerID != selectedPlayer.playerID &&
                playerData.startPosition == gridPos)
            {
                return true;
            }
        }
        return false;
    }

    public bool CanAddMorePoints(Player selectedPlayer, List<Vector2Int> currentPath)
    {
        PlayerSpawnData playerData = PlayerManager.Instance.GetPlayerData(selectedPlayer.playerID);
        int maxSelections = playerData != null ? playerData.maxSelections : GameManager.Instance.GetMaxSelections();
        return currentPath.Count < maxSelections + 1;
    }

    public bool IsDuplicatePoint(Vector2Int gridPos, List<Vector2Int> currentPath)
    {
        if (currentPath.Count > 0 && currentPath[currentPath.Count - 1] == gridPos) return true;
        return false;
    }

    public bool CanReachGoalAfterThisSelection(Vector2Int targetPos, Player selectedPlayer, List<Vector2Int> currentPath)
    {
        PlayerSpawnData playerData = PlayerManager.Instance.GetPlayerData(selectedPlayer.playerID);
        int maxSelections = playerData != null ? playerData.maxSelections : GameManager.Instance.GetMaxSelections();
        int usedAfterThis = currentPath.Count + 1;
        int remainingAfterThis = maxSelections + 1 - usedAfterThis;

        Vector2Int ownGoalPos = GetOwnGoalPosition(selectedPlayer);

        if (remainingAfterThis == 0)
        {
            return targetPos == ownGoalPos;
        }

        if (remainingAfterThis == 1)
        {
            Vector2Int diff = ownGoalPos - targetPos;
            return (diff.x == 0) || (diff.y == 0);
        }

        return true;
    }

    public Vector2Int GetLastPathPoint(Player selectedPlayer, List<Vector2Int> currentPath)
    {
        if (currentPath.Count > 0)
            return currentPath[currentPath.Count - 1];
        else
            return selectedPlayer.GetGridPosition();
    }

    public Vector2Int GetOwnGoalPosition(Player selectedPlayer)
    {
        if (LevelManager.Instance?.currentLevelData?.players == null) return Vector2Int.zero;

        foreach (var playerData in LevelManager.Instance.currentLevelData.players)
        {
            if (playerData.playerID == selectedPlayer.playerID)
            {
                return playerData.goalPosition;
            }
        }
        return Vector2Int.zero;
    }

    public bool IsPlayerOwnGoal(Vector2Int gridPos, Player selectedPlayer)
    {
        if (LevelManager.Instance?.currentLevelData?.players == null) return false;

        foreach (var playerData in LevelManager.Instance.currentLevelData.players)
        {
            if (playerData.playerID == selectedPlayer.playerID &&
                playerData.goalPosition == gridPos)
            {
                return true;
            }
        }
        return false;
    }
}