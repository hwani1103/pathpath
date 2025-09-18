using UnityEngine;
using System.Collections.Generic;

public class PathInput : MonoBehaviour
{
    [Header("Input Settings")]
    public LayerMask playerLayerMask = -1;

    [Header("Path Colors")]
    public Color validPathColor = Color.yellow;
    public Color invalidPathColor = Color.red;

    // 캐싱된 컴포넌트들
    private Camera mainCamera;
    private Player selectedPlayer = null;
    private LineRenderer hoverLineRenderer;

    // 경로 데이터
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

        // Sprites-Default 재질 생성
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

            // 각 플레이어용 LineRenderer 생성
            GameObject pathObj = new GameObject($"PathLine_Player{player.playerID}");
            pathObj.transform.SetParent(transform);

            LineRenderer pathRenderer = pathObj.AddComponent<LineRenderer>();

            // Sprites-Default 재질 생성
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

            // 1. 플레이어 선택 체크
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

            // 2. 경로 추가
            if (selectedPlayer != null)
            {
                Vector2Int gridPos = GridManager.Instance.WorldToGrid(worldPos);
                if (IsValidPathPoint(gridPos))
                {
                    AddToPath(gridPos);
                }
            }
        }

        // 스페이스바로 시뮬레이션 시작
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

        // Goal을 선택한 후에는 호버링 비활성화 (추가된 부분)
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

        // 현재 위치가 경로에 없으면 추가
        if (currentPath.Count == 0)
        {
            currentPath.Add(player.GetGridPosition());
        }
    }
    bool IsValidPathPoint(Vector2Int gridPos)
    {
        // 타일이 존재하는지 확인
        if (!HasTileAtPosition(gridPos)) return false;

        // 다른 플레이어의 Goal 위치인지 확인
        if (IsOtherPlayerGoal(gridPos)) return false;

        // 다른 플레이어의 시작지점인지 확인
        if (IsOtherPlayerStart(gridPos)) return false;

        // 선택 수 제한 확인
        if (!CanAddMorePoints()) return false;

        // 중복 방지
        if (currentPath.Count > 0 && currentPath[currentPath.Count - 1] == gridPos) return false;

        // 마지막-1 선택에서 Goal로의 직선 체크
        if (!CanReachGoalAfterThisSelection(gridPos)) return false;

        return true;
    }

    void AddToPath(Vector2Int gridPos)
    {
        currentPath.Add(gridPos);
        playerPaths[selectedPlayer] = new List<Vector2Int>(currentPath);
        UpdatePathVisualization();

        // Goal에 도달했으면 완료 처리
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
        // 다른 플레이어 Goal
        if (IsOtherPlayerGoal(targetPos)) return invalidPathColor;

        // 다른 플레이어 시작점
        if (IsOtherPlayerStart(targetPos)) return invalidPathColor;

        // 마지막-1 선택에서 Goal 도달 불가능
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
        // 모든 플레이어가 Goal에 도달했는지 확인
        if (!AreAllPlayersComplete())
        {
            return; // 완성되지 않았으면 시뮬레이션 시작하지 않음
        }

        foreach (var kvp in playerPaths)
        {
            Player player = kvp.Key;
            List<Vector2Int> fullPath = kvp.Value;

            if (fullPath.Count > 1)
            {
                List<Vector2Int> playerPath = new List<Vector2Int>(fullPath);
                playerPath.RemoveAt(0); // 시작점 제거

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
        int usedAfterThis = currentPath.Count + 1; // 이번 선택 포함
        int remainingAfterThis = maxSelections + 1 - usedAfterThis;

        Vector2Int ownGoalPos = GetOwnGoalPosition();

        if (remainingAfterThis == 0) // 마지막 선택
        {
            return targetPos == ownGoalPos;
        }

        if (remainingAfterThis == 1) // 마지막에서 두 번째 선택
        {
            Vector2Int diff = ownGoalPos - targetPos;
            return (diff.x == 0) || (diff.y == 0); // 직선인지 확인
        }

        return true; // 그 외 선택은 자유
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

        // 레벨의 모든 플레이어 확인
        foreach (var playerData in LevelManager.Instance.currentLevelData.players)
        {
            // 해당 플레이어 오브젝트 찾기
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

            // 이 플레이어의 경로가 Goal까지 완성되었는지 확인
            List<Vector2Int> path = playerPaths[playerObj];
            if (path.Count == 0) return false;

            Vector2Int lastPoint = path[path.Count - 1];
            if (lastPoint != playerData.goalPosition) return false;
        }

        return true; // 모든 플레이어가 Goal까지 완성됨
    }
    void CompletePlayerPath()
    {
        if (!playerPathRenderers.ContainsKey(selectedPlayer)) return;

        LineRenderer pathRenderer = playerPathRenderers[selectedPlayer];

        // 완료된 경로 색상을 검은색으로 변경
        pathRenderer.startColor = Color.black;
        pathRenderer.endColor = Color.black;

        // 2초 후 숨기기
        StartCoroutine(HidePathAfterDelay(pathRenderer, 1.0f));
    }

    System.Collections.IEnumerator HidePathAfterDelay(LineRenderer pathRenderer, float delay)
    {
        yield return new WaitForSeconds(delay);
        pathRenderer.positionCount = 0;
    }
}
