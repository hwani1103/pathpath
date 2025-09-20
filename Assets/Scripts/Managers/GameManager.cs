using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        Planning,    // 경로 계획 중
        Simulating,  // 시뮬레이션 실행 중  
        GameOver,    // 게임 오버
        LevelClear   // 레벨 클리어
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
    [SerializeField] private LevelData[] allLevels; // Inspector에서 레벨들 할당
    [SerializeField] private int maxLevels = 2;    // 총 레벨 수

    private PlayerSelectionIndicator cachedPlayerIndicator;
    private GoalSelectionIndicator cachedGoalIndicator;
    private RemainingSelectionsDisplay cachedSelectionsDisplay;
    private PlayerCompletionManager cachedCompletionManager;

    // 재사용할 딕셔너리 (GC 방지)
    private Dictionary<Vector2Int, List<Player>> reusablePositionTracker = new Dictionary<Vector2Int, List<Player>>();
    // 싱글톤 패턴
    private static GameManager instance;
    public static GameManager Instance { get { return instance; } }

    void Awake()
    {
        // 싱글톤 설정
        if (instance == null)
        {
            instance = this;
            transform.SetParent(null); // Root로 이동
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
        // 첫 실행 시에만 캐시
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

    // 레벨별 선택 수 반환
    public int GetMaxSelections()
    {
        return maxSelectionsPerPlayer;
    }

    // 레벨 완료 처리
    public void CompleteLevel()
    {
        currentGameState = GameState.LevelClear;
        OnGameStateChanged?.Invoke(currentGameState);
        Debug.Log("Level Complete!");
    }

    // 레벨 실패 처리
    public void FailLevel()
    {
        currentGameState = GameState.GameOver;
        OnGameStateChanged?.Invoke(currentGameState);
        Debug.Log("Game Over!");
    }

    // 게임 재시작
    public void RestartLevel()
    {
        // 시뮬레이션 중지
        isSimulationRunning = false;
        StopAllCoroutines();

        // 상태 초기화
        currentGameState = GameState.Planning;
        OnGameStateChanged?.Invoke(currentGameState);

        // 플레이어들 정지 및 초기화
        PlayerManager.Instance.StopAllPlayers();

        // 경로 및 UI 정리
        if (pathInput != null)
        {
            pathInput.ClearAllLineRenderers();
            pathInput.ResetAllPaths();
        }

        // 플레이어들을 시작 위치로 이동
        ResetPlayersToStartPosition();

        // 완료 상태 초기화
        if (cachedCompletionManager != null)
        {
            cachedCompletionManager.ResetAllPlayers();
        }

        Debug.Log("Level Restarted");
    }
    public void NextLevel()
    {
        currentLevel++;

        // 모든 레벨 클리어 확인
        if (currentLevel > maxLevels)
        {
            Debug.Log("All levels completed! Game Complete!");
            // TODO: 게임 완주 UI 표시
            return;
        }

        // 다음 레벨 로딩
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
            // 완료 상태 초기화 추가
            if (cachedCompletionManager != null)
            {
                cachedCompletionManager.ResetAllPlayers();
            }

            // PathManager가 새 플레이어들을 캐시하도록 강제
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

        // 캐시된 참조 사용
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
        // allLevels 배열에서 레벨 찾기
        if (allLevels != null && levelNumber > 0 && levelNumber <= allLevels.Length)
        {
            return allLevels[levelNumber - 1]; // 배열은 0부터 시작
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
        // 기존 데이터만 클리어 (딕셔너리 재생성 안함)
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
                return false; // 하나라도 Goal에 없으면 실패
            }
        }

        return true; // 모든 플레이어가 Goal에 있음
    }
    void HandleCollision(Vector2Int collisionPos, List<Player> collidedPlayers)
    {
        isSimulationRunning = false;

        // 모든 플레이어 즉시 정지
        PlayerManager.Instance.StopAllPlayers();

        Debug.Log($"Collision detected at {collisionPos}!");
        Debug.Log("Game Over - Players collided!");

        FailLevel();
    }

    System.Collections.IEnumerator CollisionDetectionCoroutine()
    {
        while (isSimulationRunning)
        {
            CheckForCollisions(); // 여기서 충돌 체크
            // 모든 플레이어가 이동을 완료했는지 확인
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
                // 추가 검증: 모든 플레이어가 실제 Goal 위치에 있는지 확인
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