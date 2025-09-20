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

        // �̺�Ʈ ����
        GameManager.OnGameStateChanged += HandleGameStateChanged;

        // ��ư �̺�Ʈ ���� (startButton ����)
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);
        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(NextLevel);

        // �ʱ� ���� ���� - �ٷ� ���� ����
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
        // ���� ���� �ؽ�Ʈ�� ǥ���ϰ� �������� ����
        gameUI?.SetActive(true);  // ���� �ؽ�Ʈ�� �ִٸ� ����
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