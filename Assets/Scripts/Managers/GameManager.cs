using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        Planning,    // ��� ��ȹ ��
        Simulating,  // �ùķ��̼� ���� ��  
        GameOver,    // ���� ����
        LevelClear   // ���� Ŭ����
    }

    [Header("Game State")]
    public GameState currentGameState = GameState.Planning;
    public static event System.Action<GameState> OnGameStateChanged;

    [Header("Game Settings")]
    public int maxSelectionsPerPlayer = 3;
    public int currentLevel = 1;

    [Header("Managers")]
    [SerializeField] private GridManager gridManager;
    private PathInput pathInput;

    [Header("Collision Detection")]
    private bool isSimulationRunning = false;

    [Header("Level Management")]
    [SerializeField] private LevelData[] allLevels; // Inspector���� ������ �Ҵ�
    [SerializeField] private int maxLevels = 2;    // �� ���� ��

    private PlayerSelectionIndicator cachedPlayerIndicator;
    private GoalSelectionIndicator cachedGoalIndicator;
    private RemainingSelectionsDisplay cachedSelectionsDisplay;
    private PlayerCompletionManager cachedCompletionManager;

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
    void Start()
    {
        StartCoroutine(DelayedLevelLoad());
    }

    System.Collections.IEnumerator DelayedLevelLoad()
    {
        yield return new WaitForEndOfFrame();
        LoadLevel(currentLevel);
    }


    void InitializeGame()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (pathInput == null)
            pathInput = FindFirstObjectByType<PathInput>();

        if (cachedCompletionManager == null)
            cachedCompletionManager = FindFirstObjectByType<PlayerCompletionManager>();

        if (cachedGoalIndicator == null)
            cachedGoalIndicator = FindFirstObjectByType<GoalSelectionIndicator>();
    }
    public void StartSimulation()
    {
        if (pathInput != null && pathInput.AreAllPlayersComplete())
        {
            currentGameState = GameState.Simulating;
            OnGameStateChanged?.Invoke(currentGameState);

            isSimulationRunning = true;
            reusablePositionTracker.Clear();
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
        currentGameState = GameState.LevelClear;
        OnGameStateChanged?.Invoke(currentGameState);
        Debug.Log("Level Complete!");
    }

    // ���� ���� ó��
    public void FailLevel()
    {
        currentGameState = GameState.GameOver;
        OnGameStateChanged?.Invoke(currentGameState);
        Debug.Log("Game Over!");
    }

    // ���� �����
    public void RestartLevel()
    {
        // �ùķ��̼� ����
        isSimulationRunning = false;
        StopAllCoroutines();

        // ���� �ʱ�ȭ
        currentGameState = GameState.Planning;
        OnGameStateChanged?.Invoke(currentGameState);

        // �÷��̾�� ���� �� �ʱ�ȭ
        PlayerManager.Instance.StopAllPlayers();

        // ��� �� UI ����
        if (pathInput != null)
        {
            pathInput.ClearAllLineRenderers();
            pathInput.ResetAllPaths();
        }

        // �÷��̾���� ���� ��ġ�� �̵�
        ResetPlayersToStartPosition();

        // �Ϸ� ���� �ʱ�ȭ
        if (cachedCompletionManager != null)
        {
            cachedCompletionManager.ResetAllPlayers();
        }

        Debug.Log("Level Restarted");
    }
    public void NextLevel()
    {
        currentLevel++;

        // ��� ���� Ŭ���� Ȯ��
        if (currentLevel > maxLevels)
        {
            Debug.Log("All levels completed! Game Complete!");
            // TODO: ���� ���� UI ǥ��
            return;
        }

        // ���� ���� �ε�
        LoadLevel(currentLevel);
    }

    void LoadLevel(int levelNumber)
    {
        LevelData targetLevel = GetLevelData(levelNumber);

        if (targetLevel != null)
        {
            isSimulationRunning = false;
            StopAllCoroutines();

            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.LoadLevel(targetLevel);
            }
            // �Ϸ� ���� �ʱ�ȭ �߰�
            if (cachedCompletionManager != null)
            {
                cachedCompletionManager.ResetAllPlayers();
            }

            // PathManager�� �� �÷��̾���� ĳ���ϵ��� ����
            StartCoroutine(RefreshPathManagerCache());

            currentGameState = GameState.Planning;
            OnGameStateChanged?.Invoke(currentGameState);

            Debug.Log($"Level {levelNumber} loaded");
        }
        else
        {
            Debug.LogError($"Level {levelNumber} data not found!");
        }
    }

    System.Collections.IEnumerator RefreshPathManagerCache()
    {
        yield return new WaitForEndOfFrame();

        // ĳ�õ� ���� ���
        if (cachedGoalIndicator != null)
        {
            cachedGoalIndicator.SendMessage("FindAllGoalObjects", SendMessageOptions.DontRequireReceiver);
        }

        if (pathInput != null)
        {
            var pathManager = pathInput.GetComponent<PathManager>();
            if (pathManager != null)
            {
                var allPlayers = PlayerManager.Instance.GetAllPlayers();
                foreach (var player in allPlayers)
                {
                    pathManager.AddPlayerToCache(player);
                }
            }
        }
    }

    LevelData GetLevelData(int levelNumber)
    {
        // allLevels �迭���� ���� ã��
        if (allLevels != null && levelNumber > 0 && levelNumber <= allLevels.Length)
        {
            return allLevels[levelNumber - 1]; // �迭�� 0���� ����
        }
        return null;
    }
    void ResetPlayersToStartPosition()
    {
        if (LevelManager.Instance?.currentLevelData?.players == null) return;

        foreach (var playerData in LevelManager.Instance.currentLevelData.players)
        {
            Player player = PlayerManager.Instance.GetPlayerByID(playerData.playerID);
            if (player != null)
            {
                Vector3 startWorldPos = GridManager.Instance.GridToWorld(playerData.startPosition);
                player.transform.position = startWorldPos;
                player.ClearPath();
            }
        }
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