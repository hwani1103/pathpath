using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject gameUI;
    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private GameObject levelClearUI;

    [Header("Game UI")]
    [SerializeField] private TextMeshProUGUI currentLevelText;

    [Header("Game Over UI")]
    [SerializeField] private Button restartButton;
    [SerializeField] private TextMeshProUGUI gameOverText;

    [Header("Level Clear UI")]
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private TextMeshProUGUI levelClearText;

    void Start()
    {
        if (restartButton == null) Debug.LogWarning("RestartButton not assigned!");
        if (nextLevelButton == null) Debug.LogWarning("NextLevelButton not assigned!");

        // 이벤트 구독
        GameManager.OnGameStateChanged += HandleGameStateChanged;

        // 버튼 이벤트 연결 (startButton 제거)
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);
        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(NextLevel);

        // 초기 상태 설정 - 바로 게임 시작
        ShowGameUI();
        UpdateLevelText();
    }

    void OnDestroy()
    {
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
    }

    void HandleGameStateChanged(GameManager.GameState newState)
    {
        switch (newState)
        {
            case GameManager.GameState.Planning:
                ShowGameUI();
                break;
            case GameManager.GameState.Simulating:
                HideAllUI();
                break;
            case GameManager.GameState.GameOver:
                ShowGameOverUI();
                break;
            case GameManager.GameState.LevelClear:
                ShowLevelClearUI();
                break;
        }
    }

    void ShowGameUI()
    {
        // 현재 레벨 텍스트만 표시하고 나머지는 숨김
        gameUI?.SetActive(true);  // 레벨 텍스트가 있다면 유지
        gameOverUI?.SetActive(false);
        levelClearUI?.SetActive(false);
    }

    void ShowGameOverUI()
    {
        gameUI?.SetActive(false);
        gameOverUI?.SetActive(true);
        levelClearUI?.SetActive(false);

        if (gameOverText != null)
            gameOverText.text = "Game Over!";
    }

    void ShowLevelClearUI()
    {
        gameUI?.SetActive(false);
        gameOverUI?.SetActive(false);
        levelClearUI?.SetActive(true);

        if (levelClearText != null)
            levelClearText.text = "Level Clear!";
    }

    void HideAllUI()
    {
        gameUI?.SetActive(false);
        gameOverUI?.SetActive(false);
        levelClearUI?.SetActive(false);
    }

    void UpdateLevelText()
    {
        if (currentLevelText != null && GameManager.Instance != null)
        {
            currentLevelText.text = $"Level {GameManager.Instance.currentLevel}";
        }
    }


    void RestartGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartLevel();
        }
    }

    void NextLevel()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.NextLevel();
        }
    }
}