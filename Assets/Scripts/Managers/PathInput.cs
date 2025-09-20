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
            // �̺�Ʈ �߻�
            OnPathPointAdded?.Invoke(gridPos);
        }
    }

    // HandlePlayerSelection �޼��� ���� - �Ϸ�� �÷��̾� ���� ����
    void HandlePlayerSelection(Player player)
    {
        var completionManager = GetComponent<PlayerCompletionManager>();
        if (completionManager != null && completionManager.IsPlayerCompleted(player.playerID))
        {
            return;
        }

        // ���� ���õ� �÷��̾ �ٽ� ��ġ �� ���� ����
        if (selectedPlayer == player)
        {
            // ���� ��ΰ� �Ϸ���� �ʾҴٸ� �ʱ�ȭ
            if (!IsCurrentPathCompleteToGoal())
            {
                ResetPlayerPath(selectedPlayer);
            }

            // ���� ����
            selectedPlayer = null;
            currentPath.Clear();
            OnPlayerSelectionChanged?.Invoke(null);
            return;
        }

        // �ٸ� �÷��̾ �̹� ���õǾ� �ִٸ� ���� ����
        if (selectedPlayer != null)
        {
            Debug.Log("�ٸ� �÷��̾��� ��θ� ���� �Ϸ��ϰų� ������ �������ּ���.");
            return;
        }

        // ���ο� �÷��̾� ����
        selectedPlayer = player;
        currentPath = new List<Vector2Int>(pathManager.GetPlayerPath(player));

        if (currentPath.Count == 0)
        {
            currentPath.Add(player.GetGridPosition());
        }

        pathVisualizer.InitializePlayerPathRenderer(player);
        OnPlayerSelectionChanged?.Invoke(player);
    }

    // ���ο� �޼��� �߰�
    private void ResetPlayerPath(Player player)
    {
        // ��� ������ �ʱ�ȭ (�������� ����)
        List<Vector2Int> resetPath = new List<Vector2Int> { player.GetGridPosition() };
        pathManager.SetPlayerPath(player, resetPath);

        // LineRenderer �ʱ�ȭ
        pathVisualizer.UpdatePlayerPath(player, resetPath);

        // UI ������Ʈ �߰� - ���� Ƚ�� 0���� ����
        var selectionsDisplay = GetComponent<RemainingSelectionsDisplay>();
        if (selectionsDisplay != null)
        {
            selectionsDisplay.UpdateSelectionsCount(player, 0); // 0���� ���� (������ �����ϸ� ���� 0��)
        }
    }

    // ���� IsPathCompleteToGoal�� ���� ��ο����� �и�
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

        // ���� Ƚ�� UI ������Ʈ �߰�
        var selectionsDisplay = GetComponent<RemainingSelectionsDisplay>();
        if (selectionsDisplay != null)
        {
            selectionsDisplay.UpdateSelectionsCount(selectedPlayer, currentPath.Count - 1); // ������ ����
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

    // CompletePlayerPath �޼��� ���� - ���� �̺�Ʈ �߻�
    void CompletePlayerPath()
    {
        pathVisualizer.HideHoverPath();
        pathVisualizer.CompletePlayerPath(selectedPlayer);

        // ���յ� �Ϸ� �̺�Ʈ �߻� (��� �Ϸ� ó���� �̰� �ϳ��� ����)
        OnPlayerCompleted?.Invoke(selectedPlayer);

        selectedPlayer = null;
        currentPath.Clear();
    }

    void HandleHoverAndDrag()
    {
        if (selectedPlayer == null || pathManager == null || pathManager.IsAnyPlayerMoving()) return;
        if (IsPathCompleteToGoal()) return;

        // inputHandler null üũ �߰�
        if (inputHandler == null) return;

        Vector3 inputWorldPos = inputHandler.GetWorldPosition();

        // GridManager null üũ �߰�
        if (GridManager.Instance == null) return;

        Vector2Int inputGridPos = GridManager.Instance.WorldToGrid(inputWorldPos);

        // pathValidator null üũ �߰�
        if (pathValidator == null) return;

        if (!pathValidator.HasTileAtPosition(inputGridPos) ||
            !pathValidator.CanAddMorePoints(selectedPlayer, currentPath))
        {
            // pathVisualizer null üũ �߰�
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
        // ���� ���� �ʱ�ȭ
        selectedPlayer = null;
        currentPath.Clear();

        pathManager.ExecuteAllPaths();
    }

    public void ClearAllLineRenderers()
    {
        // pathVisualizer null üũ �߰�
        if (pathVisualizer != null)
        {
            pathVisualizer.ClearAllLineRenderers();
        }

        // pathManager null üũ �߰�
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