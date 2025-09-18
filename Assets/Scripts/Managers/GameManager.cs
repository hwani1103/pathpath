using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public int maxSelectionsPerPlayer = 3;
    public int currentLevel = 1;

    [Header("Managers")]
    [SerializeField] private GridManager gridManager;

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
        // TODO: 재시작 UI 표시
    }

    // 게임 재시작
    public void RestartLevel()
    {
        // TODO: 레벨 리셋
    }
}