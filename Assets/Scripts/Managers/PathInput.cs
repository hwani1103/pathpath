using UnityEngine;
using System.Collections.Generic;

public class PathInput : MonoBehaviour
{
    [Header("Input Settings")]
    public LayerMask playerLayerMask = -1;

    [Header("Path Colors")]
    public Color validPathColor = Color.yellow;
    public Color invalidPathColor = Color.red;

    // ĳ�̵� ������Ʈ��
    private Camera mainCamera;
    private Player selectedPlayer = null;
    private LineRenderer hoverLineRenderer;

    // ��� ������
    private Dictionary<Player, List<Vector2Int>> playerPaths = new Dictionary<Player, List<Vector2Int>>();
    private Dictionary<Player, LineRenderer> playerPathRenderers = new Dictionary<Player, LineRenderer>();
    private List<Vector2Int> currentPath = new List<Vector2Int>();

    void Start()
    {
        mainCamera = Camera.main;
        CreateHoverLineRenderer();
        InitializePlayerPaths();
    }

    void CreateHoverLineRenderer()
    {
        GameObject hoverObj = new GameObject("HoverPathLine");
        hoverObj.transform.SetParent(transform);

        hoverLineRenderer = hoverObj.AddComponent<LineRenderer>();

        // Sprites-Default ���� ����
        Material lineMat = new Material(Shader.Find("Sprites/Default"));
        hoverLineRenderer.material = lineMat;

        hoverLineRenderer.startWidth = 0.15f;
        hoverLineRenderer.endWidth = 0.15f;
        hoverLineRenderer.positionCount = 0;
        hoverLineRenderer.sortingOrder = 2;
        hoverLineRenderer.useWorldSpace = true;
        hoverLineRenderer.startColor = validPathColor;
        hoverLineRenderer.endColor = validPathColor;
    }

    void InitializePlayerPaths()
    {
        Player[] allPlayers = FindObjectsByType<Player>(FindObjectsSortMode.None);

        foreach (Player player in allPlayers)
        {
            playerPaths[player] = new List<Vector2Int>();

            // �� �÷��̾�� LineRenderer ����
            GameObject pathObj = new GameObject($"PathLine_Player{player.playerID}");
            pathObj.transform.SetParent(transform);

            LineRenderer pathRenderer = pathObj.AddComponent<LineRenderer>();

            // Sprites-Default ���� ����
            Material pathMat = new Material(Shader.Find("Sprites/Default"));
            pathRenderer.material = pathMat;

            pathRenderer.startColor = player.playerColor;
            pathRenderer.endColor = player.playerColor;
            pathRenderer.startWidth = 0.12f;
            pathRenderer.endWidth = 0.12f;
            pathRenderer.positionCount = 0;
            pathRenderer.sortingOrder = 1;
            pathRenderer.useWorldSpace = true;

            playerPathRenderers[player] = pathRenderer;
        }
    }

    void Update()
    {
        HandleInput();
        HandleHover();
    }

    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 worldPos = GetWorldPosition();

            // 1. �÷��̾� ���� üũ
            Collider2D hitCollider = Physics2D.OverlapPoint(worldPos, playerLayerMask);
            if (hitCollider != null)
            {
                Player player = hitCollider.GetComponent<Player>();
                if (player != null)
                {
                    SelectPlayer(player);
                    return;
                }
            }

            // 2. ��� �߰�
            if (selectedPlayer != null)
            {
                Vector2Int gridPos = GridManager.Instance.WorldToGrid(worldPos);
                if (IsValidPathPoint(gridPos))
                {
                    AddToPath(gridPos);
                }
            }
        }

        // �����̽��ٷ� �ùķ��̼� ����
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartSimulation();
        }
    }

    void HandleHover()
    {
        if (selectedPlayer == null || IsAnyPlayerMoving())
        {
            hoverLineRenderer.positionCount = 0;
            return;
        }

        // Goal�� ������ �Ŀ��� ȣ���� ��Ȱ��ȭ (�߰��� �κ�)
        if (IsPathCompleteToGoal())
        {
            hoverLineRenderer.positionCount = 0;
            return;
        }

        Vector3 mouseWorldPos = GetWorldPosition();
        Vector2Int mouseGridPos = GridManager.Instance.WorldToGrid(mouseWorldPos);

        if (!HasTileAtPosition(mouseGridPos) || !CanAddMorePoints())
        {
            hoverLineRenderer.positionCount = 0;
            return;
        }

        Vector2Int startPos = GetLastPathPoint();
        if (IsStraightLine(startPos, mouseGridPos))
        {
            ShowHoverPath(startPos, mouseGridPos, GetHoverColor(mouseGridPos));
        }
        else
        {
            hoverLineRenderer.positionCount = 0;
        }
    }

    Vector3 GetWorldPosition()
    {
        Vector3 screenPos = Input.mousePosition;
        screenPos.z = 10f;
        return mainCamera.ScreenToWorldPoint(screenPos);
    }

    void SelectPlayer(Player player)
    {
        selectedPlayer = player;

        if (playerPaths.ContainsKey(player))
        {
            currentPath = new List<Vector2Int>(playerPaths[player]);
        }
        else
        {
            currentPath = new List<Vector2Int>();
            playerPaths[player] = currentPath;
        }

        // ���� ��ġ�� ��ο� ������ �߰�
        if (currentPath.Count == 0)
        {
            currentPath.Add(player.GetGridPosition());
        }
    }
    bool IsValidPathPoint(Vector2Int gridPos)
    {
        // Ÿ���� �����ϴ��� Ȯ��
        if (!HasTileAtPosition(gridPos)) return false;

        // �ٸ� �÷��̾��� Goal ��ġ���� Ȯ��
        if (IsOtherPlayerGoal(gridPos)) return false;

        // �ٸ� �÷��̾��� ������������ Ȯ��
        if (IsOtherPlayerStart(gridPos)) return false;

        // ���� �� ���� Ȯ��
        if (!CanAddMorePoints()) return false;

        // �ߺ� ����
        if (currentPath.Count > 0 && currentPath[currentPath.Count - 1] == gridPos) return false;

        // ������-1 ���ÿ��� Goal���� ���� üũ
        if (!CanReachGoalAfterThisSelection(gridPos)) return false;

        return true;
    }

    void AddToPath(Vector2Int gridPos)
    {
        currentPath.Add(gridPos);
        playerPaths[selectedPlayer] = new List<Vector2Int>(currentPath);
        UpdatePathVisualization();

        // Goal�� ���������� �Ϸ� ó��
        if (IsPlayerOwnGoal(gridPos))
        {
            CompletePlayerPath();
        }
    }

    void UpdatePathVisualization()
    {
        if (selectedPlayer == null || !playerPathRenderers.ContainsKey(selectedPlayer)) return;

        LineRenderer pathRenderer = playerPathRenderers[selectedPlayer];
        List<Vector2Int> path = currentPath;

        if (path.Count < 2)
        {
            pathRenderer.positionCount = 0;
            return;
        }

        pathRenderer.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
        {
            Vector3 worldPos = GridManager.Instance.GridToWorld(path[i]);
            pathRenderer.SetPosition(i, worldPos);
        }
    }

    void ShowHoverPath(Vector2Int from, Vector2Int to, Color color)
    {
        hoverLineRenderer.startColor = color;
        hoverLineRenderer.endColor = color;
        hoverLineRenderer.positionCount = 2;

        Vector3 startWorldPos = GridManager.Instance.GridToWorld(from);
        Vector3 endWorldPos = GridManager.Instance.GridToWorld(to);

        hoverLineRenderer.SetPosition(0, startWorldPos);
        hoverLineRenderer.SetPosition(1, endWorldPos);
    }

    Color GetHoverColor(Vector2Int targetPos)
    {
        // �ٸ� �÷��̾� Goal
        if (IsOtherPlayerGoal(targetPos)) return invalidPathColor;

        // �ٸ� �÷��̾� ������
        if (IsOtherPlayerStart(targetPos)) return invalidPathColor;

        // ������-1 ���ÿ��� Goal ���� �Ұ���
        if (!CanReachGoalAfterThisSelection(targetPos)) return invalidPathColor;

        return validPathColor;
    }

    Vector2Int GetLastPathPoint()
    {
        if (currentPath.Count > 0)
            return currentPath[currentPath.Count - 1];
        else
            return selectedPlayer.GetGridPosition();
    }

    bool HasTileAtPosition(Vector2Int gridPos)
    {
        if (LevelManager.Instance?.mainTilemap == null) return false;

        Vector3 worldPos = GridManager.Instance.GridToWorld(gridPos);
        Vector3Int tilemapPos = LevelManager.Instance.mainTilemap.WorldToCell(worldPos);
        return LevelManager.Instance.mainTilemap.GetTile(tilemapPos) != null;
    }

    bool IsOtherPlayerGoal(Vector2Int gridPos)
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

    bool CanAddMorePoints()
    {
        int maxSelections = GameManager.Instance.GetMaxSelections();
        return currentPath.Count < maxSelections + 1;
    }

    bool IsStraightLine(Vector2Int from, Vector2Int to)
    {
        Vector2Int diff = to - from;
        return (diff.x == 0 && diff.y != 0) || (diff.y == 0 && diff.x != 0);
    }


    bool IsAnyPlayerMoving()
    {
        Player[] allPlayers = FindObjectsByType<Player>(FindObjectsSortMode.None);
        foreach (Player player in allPlayers)
        {
            if (player.IsMoving()) return true;
        }
        return false;
    }

    void StartSimulation()
    {
        // ��� �÷��̾ Goal�� �����ߴ��� Ȯ��
        if (!AreAllPlayersComplete())
        {
            return; // �ϼ����� �ʾ����� �ùķ��̼� �������� ����
        }

        foreach (var kvp in playerPaths)
        {
            Player player = kvp.Key;
            List<Vector2Int> fullPath = kvp.Value;

            if (fullPath.Count > 1)
            {
                List<Vector2Int> playerPath = new List<Vector2Int>(fullPath);
                playerPath.RemoveAt(0); // ������ ����

                player.SetPath(playerPath);
                player.StartMoving();
            }
        }
    }
    bool IsOtherPlayerStart(Vector2Int gridPos)
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

    bool CanReachGoalAfterThisSelection(Vector2Int targetPos)
    {
        int maxSelections = GameManager.Instance.GetMaxSelections();
        int usedAfterThis = currentPath.Count + 1; // �̹� ���� ����
        int remainingAfterThis = maxSelections + 1 - usedAfterThis;

        Vector2Int ownGoalPos = GetOwnGoalPosition();

        if (remainingAfterThis == 0) // ������ ����
        {
            return targetPos == ownGoalPos;
        }

        if (remainingAfterThis == 1) // ���������� �� ��° ����
        {
            Vector2Int diff = ownGoalPos - targetPos;
            return (diff.x == 0) || (diff.y == 0); // �������� Ȯ��
        }

        return true; // �� �� ������ ����
    }
    Vector2Int GetOwnGoalPosition()
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
    bool IsPathCompleteToGoal()
    {
        if (currentPath.Count == 0) return false;

        Vector2Int lastPoint = currentPath[currentPath.Count - 1];
        return IsPlayerOwnGoal(lastPoint);
    }

    bool IsPlayerOwnGoal(Vector2Int gridPos)
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

    bool AreAllPlayersComplete()
    {
        if (LevelManager.Instance?.currentLevelData?.players == null) return false;

        // ������ ��� �÷��̾� Ȯ��
        foreach (var playerData in LevelManager.Instance.currentLevelData.players)
        {
            // �ش� �÷��̾� ������Ʈ ã��
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

            // �� �÷��̾��� ��ΰ� Goal���� �ϼ��Ǿ����� Ȯ��
            List<Vector2Int> path = playerPaths[playerObj];
            if (path.Count == 0) return false;

            Vector2Int lastPoint = path[path.Count - 1];
            if (lastPoint != playerData.goalPosition) return false;
        }

        return true; // ��� �÷��̾ Goal���� �ϼ���
    }
    void CompletePlayerPath()
    {
        if (!playerPathRenderers.ContainsKey(selectedPlayer)) return;

        LineRenderer pathRenderer = playerPathRenderers[selectedPlayer];

        // �Ϸ�� ��� ������ ���������� ����
        pathRenderer.startColor = Color.black;
        pathRenderer.endColor = Color.black;

        // 2�� �� �����
        StartCoroutine(HidePathAfterDelay(pathRenderer, 1.0f));
    }

    System.Collections.IEnumerator HidePathAfterDelay(LineRenderer pathRenderer, float delay)
    {
        yield return new WaitForSeconds(delay);
        pathRenderer.positionCount = 0;
    }
}
