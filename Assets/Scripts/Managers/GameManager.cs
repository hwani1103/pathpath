using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public int maxSelectionsPerPlayer = 3;
    public int currentLevel = 1;

    [Header("Managers")]
    [SerializeField] private GridManager gridManager;
    private PathInput pathInput;

    [Header("Collision Detection")]
    private bool isSimulationRunning = false;

    private PlayerSelectionIndicator cachedPlayerIndicator;
    private GoalSelectionIndicator cachedGoalIndicator;
    private RemainingSelectionsDisplay cachedSelectionsDisplay;

    // ������ ��ųʸ� (GC ����)
    private Dictionary<Vector2Int, List<Player>> reusablePositionTracker = new Dictionary<Vector2Int, List<Player>>();
    // �̱��� ����
    private static GameManager instance;
    public static GameManager Instance { get { return instance; } }

    void Awake()
    {
        // �̱��� ����
        if (instance == null)
        {
            instance = this;
            transform.SetParent(null); // Root�� �̵�
            DontDestroyOnLoad(gameObject);
            InitializeGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializeGame()
    {
        // GridManager �ڵ� ã��
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        // PathInput ĳ��
        if (pathInput == null)
            pathInput = FindFirstObjectByType<PathInput>();
    }
    public void StartSimulation()
    {
        if (pathInput != null && pathInput.AreAllPlayersComplete())
        {
            isSimulationRunning = true;
            reusablePositionTracker.Clear(); // positionTracker �� reusablePositionTracker

            ClearAllSelectionUI();
            pathInput.ExecuteAllPaths();
            StartCoroutine(CollisionDetectionCoroutine());
        }
    }
    void ClearAllSelectionUI()
    {
        // ù ���� �ÿ��� ĳ��
        if (cachedPlayerIndicator == null)
            cachedPlayerIndicator = FindFirstObjectByType<PlayerSelectionIndicator>();
        if (cachedGoalIndicator == null)
            cachedGoalIndicator = FindFirstObjectByType<GoalSelectionIndicator>();
        if (cachedSelectionsDisplay == null)
            cachedSelectionsDisplay = FindFirstObjectByType<RemainingSelectionsDisplay>();

        cachedPlayerIndicator?.ClearSelection();
        cachedGoalIndicator?.ClearSelection();
        cachedSelectionsDisplay?.ClearDisplay();
    }

    // ������ ���� �� ��ȯ
    public int GetMaxSelections()
    {
        return maxSelectionsPerPlayer;
    }

    // ���� �Ϸ� ó��
    public void CompleteLevel()
    {
        // TODO: ���� ���� �ε�, ���� ��� ��
    }

    // ���� ���� ó��
    public void FailLevel()
    {
        Debug.Log("FailLevel() called - Game Over!");
        // TODO: ����� UI ǥ��
    }

    // ���� �����
    public void RestartLevel()
    {
        // TODO: ���� ����
    }

    void CheckForCollisions()
    {
        // ���� �����͸� Ŭ���� (��ųʸ� ����� ����)
        foreach (var list in reusablePositionTracker.Values)
        {
            list.Clear();
        }
        reusablePositionTracker.Clear();

        List<Player> allPlayers = PlayerManager.Instance.GetAllPlayers();

        foreach (Player player in allPlayers)
        {
            Vector2Int gridPos = player.GetGridPosition();

            if (!reusablePositionTracker.ContainsKey(gridPos))
            {
                reusablePositionTracker[gridPos] = new List<Player>();
            }
            reusablePositionTracker[gridPos].Add(player);
        }

        foreach (var kvp in reusablePositionTracker)
        {
            if (kvp.Value.Count > 1)
            {
                HandleCollision(kvp.Key, kvp.Value);
                return;
            }
        }
    }
    bool AreAllPlayersAtGoal()
    {
        List<Player> allPlayers = PlayerManager.Instance.GetAllPlayers();

        foreach (Player player in allPlayers)
        {
            PlayerSpawnData playerData = PlayerManager.Instance.GetPlayerData(player.playerID);
            if (playerData == null) continue;

            Vector2Int playerCurrentPos = player.GetGridPosition();
            Vector2Int playerGoalPos = playerData.goalPosition;

            if (playerCurrentPos != playerGoalPos)
            {
                return false; // �ϳ��� Goal�� ������ ����
            }
        }

        return true; // ��� �÷��̾ Goal�� ����
    }
    void HandleCollision(Vector2Int collisionPos, List<Player> collidedPlayers)
    {
        isSimulationRunning = false;

        // ��� �÷��̾� ��� ����
        PlayerManager.Instance.StopAllPlayers();

        Debug.Log($"Collision detected at {collisionPos}!");
        Debug.Log("Game Over - Players collided!");

        FailLevel();
    }

    System.Collections.IEnumerator CollisionDetectionCoroutine()
    {
        while (isSimulationRunning)
        {
            CheckForCollisions(); // ���⼭ �浹 üũ
            // ��� �÷��̾ �̵��� �Ϸ��ߴ��� Ȯ��
            bool allPlayersFinished = true;
            List<Player> allPlayers = PlayerManager.Instance.GetAllPlayers();

            foreach (Player player in allPlayers)
            {
                if (player.IsMoving())
                {
                    allPlayersFinished = false;
                    break;
                }
            }

            if (allPlayersFinished)
            {
                // �߰� ����: ��� �÷��̾ ���� Goal ��ġ�� �ִ��� Ȯ��
                if (AreAllPlayersAtGoal())
                {
                    isSimulationRunning = false;
                    Debug.Log("All players reached their destinations!");
                    CompleteLevel();
                }
                else
                {
                    isSimulationRunning = false;
                    Debug.Log("Players stopped moving but not all reached goals!");
                    FailLevel();
                }
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

}