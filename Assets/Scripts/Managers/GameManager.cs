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

    void InitializeGame()
    {
        // GridManager 자동 찾기
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        // PathInput 캐시
        if (pathInput == null)
            pathInput = FindFirstObjectByType<PathInput>();
    }
    public void StartSimulation()
    {
        if (pathInput != null && pathInput.AreAllPlayersComplete())
        {
            isSimulationRunning = true;
            reusablePositionTracker.Clear(); // positionTracker → reusablePositionTracker

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
        // TODO: 다음 레벨 로딩, 별점 계산 등
    }

    // 레벨 실패 처리
    public void FailLevel()
    {
        Debug.Log("FailLevel() called - Game Over!");
        // TODO: 재시작 UI 표시
    }

    // 게임 재시작
    public void RestartLevel()
    {
        // TODO: 레벨 리셋
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