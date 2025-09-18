using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public int maxSelectionsPerPlayer = 3;
    public int currentLevel = 1;

    [Header("Managers")]
    [SerializeField] private GridManager gridManager;

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
        // TODO: ����� UI ǥ��
    }

    // ���� �����
    public void RestartLevel()
    {
        // TODO: ���� ����
    }
}