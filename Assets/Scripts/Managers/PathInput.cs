using UnityEngine;
using System.Collections.Generic;

public class PathInput : MonoBehaviour
{
    public static event System.Action<Player> OnPlayerSelectionChanged;
    public static event System.Action<Vector2Int> OnPathPointAdded;
    public static event System.Action<Player> OnPlayerCompleted;

    private InputHandler inputHandler;
    private PathValidator pathValidator;
    private PathVisualizer pathVisualizer;
    private PathManager pathManager;

    private Player selectedPlayer = null;
    private List<Vector2Int> currentPath = new List<Vector2Int>();

    void Awake()
    {
        InitializeComponents();
    }
    void Start()
    {
        InitializeComponents();
        SetupEventHandlers();
    }

    void InitializeComponents()
    {
        if (inputHandler == null)
            inputHandler = GetComponent<InputHandler>() ?? gameObject.AddComponent<InputHandler>();

        if (pathValidator == null)
            pathValidator = GetComponent<PathValidator>() ?? gameObject.AddComponent<PathValidator>();

        if (pathVisualizer == null)
            pathVisualizer = GetComponent<PathVisualizer>() ?? gameObject.AddComponent<PathVisualizer>();

        if (pathManager == null)
            pathManager = GetComponent<PathManager>() ?? gameObject.AddComponent<PathManager>();
    }

    void SetupEventHandlers()
    {
        inputHandler.OnPlayerSelected += HandlePlayerSelection;
        inputHandler.OnPathPointSelected += HandlePathPointSelection;
    }

    void Update()
    {
        HandleHoverAndDrag();
    }

    void HandlePathPointSelection(Vector2Int gridPos)
    {
        if (selectedPlayer == null || pathManager.IsAnyPlayerMoving()) return;
        if (IsPathCompleteToGoal()) return;

        if (pathValidator.IsValidPathPoint(gridPos, selectedPlayer, currentPath))
        {
            AddToPath(gridPos);
            // 이벤트 발생
            OnPathPointAdded?.Invoke(gridPos);
        }
    }

    // HandlePlayerSelection 메서드 수정 - 완료된 플레이어 선택 차단
    void HandlePlayerSelection(Player player)
    {
        var completionManager = GetComponent<PlayerCompletionManager>();
        if (completionManager != null && completionManager.IsPlayerCompleted(player.playerID))
        {
            return;
        }

        // 현재 선택된 플레이어를 다시 터치 → 선택 해제
        if (selectedPlayer == player)
        {
            // 현재 경로가 완료되지 않았다면 초기화
            if (!IsCurrentPathCompleteToGoal())
            {
                ResetPlayerPath(selectedPlayer);
            }

            // 선택 해제
            selectedPlayer = null;
            currentPath.Clear();
            OnPlayerSelectionChanged?.Invoke(null);
            return;
        }

        // 다른 플레이어가 이미 선택되어 있다면 선택 차단
        if (selectedPlayer != null)
        {
            Debug.Log("다른 플레이어의 경로를 먼저 완료하거나 선택을 해제해주세요.");
            return;
        }

        // 새로운 플레이어 선택
        selectedPlayer = player;
        currentPath = new List<Vector2Int>(pathManager.GetPlayerPath(player));

        if (currentPath.Count == 0)
        {
            currentPath.Add(player.GetGridPosition());
        }

        pathVisualizer.InitializePlayerPathRenderer(player);
        OnPlayerSelectionChanged?.Invoke(player);
    }

    // 새로운 메서드 추가
    private void ResetPlayerPath(Player player)
    {
        // 경로 데이터 초기화 (시작점만 남김)
        List<Vector2Int> resetPath = new List<Vector2Int> { player.GetGridPosition() };
        pathManager.SetPlayerPath(player, resetPath);

        // LineRenderer 초기화
        pathVisualizer.UpdatePlayerPath(player, resetPath);

        // UI 업데이트 추가 - 선택 횟수 0으로 리셋
        var selectionsDisplay = GetComponent<RemainingSelectionsDisplay>();
        if (selectionsDisplay != null)
        {
            selectionsDisplay.UpdateSelectionsCount(player, 0); // 0으로 리셋 (시작점 제외하면 선택 0개)
        }
    }

    // 기존 IsPathCompleteToGoal을 현재 경로용으로 분리
    private bool IsCurrentPathCompleteToGoal()
    {
        if (currentPath.Count == 0) return false;
        Vector2Int lastPoint = currentPath[currentPath.Count - 1];
        return pathValidator.IsPlayerOwnGoal(lastPoint, selectedPlayer);
    }

    void AddToPath(Vector2Int gridPos)
    {
        currentPath.Add(gridPos);
        pathManager.SetPlayerPath(selectedPlayer, currentPath);
        pathVisualizer.UpdatePlayerPath(selectedPlayer, currentPath);

        // 선택 횟수 UI 업데이트 추가
        var selectionsDisplay = GetComponent<RemainingSelectionsDisplay>();
        if (selectionsDisplay != null)
        {
            selectionsDisplay.UpdateSelectionsCount(selectedPlayer, currentPath.Count - 1); // 시작점 제외
        }

        if (pathValidator.IsPlayerOwnGoal(gridPos, selectedPlayer))
        {
            CompletePlayerPath();
        }
    }
    public void ClearPlayerSelection()
    {
        OnPlayerSelectionChanged?.Invoke(null);
    }

    // CompletePlayerPath 메서드 수정 - 통합 이벤트 발생
    void CompletePlayerPath()
    {
        pathVisualizer.HideHoverPath();
        pathVisualizer.CompletePlayerPath(selectedPlayer);

        // 통합된 완료 이벤트 발생 (모든 완료 처리를 이것 하나로 통합)
        OnPlayerCompleted?.Invoke(selectedPlayer);

        selectedPlayer = null;
        currentPath.Clear();
    }

    void HandleHoverAndDrag()
    {
        if (selectedPlayer == null || pathManager == null || pathManager.IsAnyPlayerMoving()) return;
        if (IsPathCompleteToGoal()) return;

        // inputHandler null 체크 추가
        if (inputHandler == null) return;

        Vector3 inputWorldPos = inputHandler.GetWorldPosition();

        // GridManager null 체크 추가
        if (GridManager.Instance == null) return;

        Vector2Int inputGridPos = GridManager.Instance.WorldToGrid(inputWorldPos);

        // pathValidator null 체크 추가
        if (pathValidator == null) return;

        if (!pathValidator.HasTileAtPosition(inputGridPos) ||
            !pathValidator.CanAddMorePoints(selectedPlayer, currentPath))
        {
            // pathVisualizer null 체크 추가
            if (pathVisualizer != null)
                pathVisualizer.HideHoverPath();
            return;
        }

        Vector2Int startPos = pathValidator.GetLastPathPoint(selectedPlayer, currentPath);
        if (pathValidator.IsStraightLine(startPos, inputGridPos))
        {
            bool isValid = pathValidator.IsValidPathPoint(inputGridPos, selectedPlayer, currentPath);
            if (pathVisualizer != null)
                pathVisualizer.ShowHoverPath(startPos, inputGridPos, isValid);
        }
        else
        {
            if (pathVisualizer != null)
                pathVisualizer.HideHoverPath();
        }
    }

    bool IsPathCompleteToGoal()
    {
        if (currentPath.Count == 0) return false;
        Vector2Int lastPoint = currentPath[currentPath.Count - 1];
        return pathValidator.IsPlayerOwnGoal(lastPoint, selectedPlayer);
    }

    public bool AreAllPlayersComplete()
    {
        return pathManager.AreAllPlayersComplete();
    }

    public void ExecuteAllPaths()
    {
        // 선택 상태 초기화
        selectedPlayer = null;
        currentPath.Clear();

        pathManager.ExecuteAllPaths();
    }

    public void ClearAllLineRenderers()
    {
        // pathVisualizer null 체크 추가
        if (pathVisualizer != null)
        {
            pathVisualizer.ClearAllLineRenderers();
        }

        // pathManager null 체크 추가
        if (pathManager != null)
        {
            pathManager.ClearAllPaths();
        }
    }
    public void AddPlayerToCache(Player player)
    {
        pathManager.AddPlayerToCache(player);
    }
    public void ResetAllPaths()
    {
        selectedPlayer = null;
        currentPath.Clear();

        pathManager?.ClearAllPaths();
        pathVisualizer?.ClearAllLineRenderers();

        OnPlayerSelectionChanged?.Invoke(null);
    }
}