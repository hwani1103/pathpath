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
        // ���� ��ư �̺�Ʈ ����
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartGame);
        }
    }

    void StartGame()
    {
        // Game ������ ��ȯ
        SceneManager.LoadScene(gameSceneName);
    }
}