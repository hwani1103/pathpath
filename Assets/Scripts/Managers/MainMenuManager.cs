using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button startButton;

    [Header("Scene Management")]
    [SerializeField] private string gameSceneName = "SampleScene";
    void Start()
    {
        // 시작 버튼 이벤트 연결
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartGame);
        }
    }

    void StartGame()
    {
        // Game 씬으로 전환
        SceneManager.LoadScene(gameSceneName);
    }
}