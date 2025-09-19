using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RemainingSelectionsDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject displayPanel;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI remainingSelectionsText;
    [SerializeField] private Image playerColorIndicator;

    [Header("Display Settings")]
    [SerializeField] private bool showPlayerName = true;
    [SerializeField] private string playerNameFormat = "Player {0}";
    [SerializeField] private string selectionsFormat = "Selections: {0}/{1}";

    private Player currentSelectedPlayer;
    private int usedSelections = 0;

    void Start()
    {
        PathInput.OnPlayerSelectionChanged += HandlePlayerSelection;
        PathInput.OnPathPointAdded += HandlePathPointSelected;
        PathInput.OnPlayerCompleted += HandlePlayerCompleted;

        if (displayPanel != null)
            displayPanel.SetActive(false);
    }

    void OnDestroy()
    {
        PathInput.OnPlayerSelectionChanged -= HandlePlayerSelection;
        PathInput.OnPathPointAdded -= HandlePathPointSelected;
        PathInput.OnPlayerCompleted -= HandlePlayerCompleted;
    }

    public void HandlePlayerCompleted(Player player)
    {
        if (currentSelectedPlayer == player)
        {
            HideDisplay();
        }
    }
    public void HandlePlayerSelection(Player selectedPlayer)
    {
        currentSelectedPlayer = selectedPlayer;

        if (selectedPlayer != null)
        {
            ShowDisplay();
            UpdateDisplay();
        }
        else
        {
            HideDisplay();
        }
    }

    public void HandlePathPointSelected(Vector2Int gridPos)
    {
        if (currentSelectedPlayer != null)
        {
            // PathManager�� �̺�Ʈ ������� ã�� �ʰ� PathInput���� ���� ����ؼ� �����ϴ� ������� ���� ����
            // �ϴ� ���� �ڵ� �����ϵ�, FindFirstObjectByType �ּ�ȭ
            var pathManager = GetComponent<PathManager>(); // ���� ������Ʈ ������ ã��
            if (pathManager != null)
            {
                var currentPath = pathManager.GetPlayerPath(currentSelectedPlayer);
                usedSelections = Mathf.Max(0, currentPath.Count - 1);
                UpdateDisplay();
            }
        }
    }

    void ShowDisplay()
    {
        if (displayPanel != null)
            displayPanel.SetActive(true);
    }

    void HideDisplay()
    {
        if (displayPanel != null)
            displayPanel.SetActive(false);

        currentSelectedPlayer = null;
        usedSelections = 0;
    }

    void UpdateDisplay()
    {
        if (currentSelectedPlayer == null) return;

        // �÷��̾� ������ ��������
        PlayerSpawnData playerData = PlayerManager.Instance.GetPlayerData(currentSelectedPlayer.playerID);
        if (playerData == null) return;

        int maxSelections = playerData.maxSelections;
        int remaining = maxSelections - usedSelections; // ���� Ƚ�� = �ִ� - ����� Ƚ��

        // �÷��̾� �̸� ǥ��
        if (showPlayerName && playerNameText != null)
        {
            playerNameText.text = string.Format(playerNameFormat, currentSelectedPlayer.playerID);
        }

        // ���� ���� �� ǥ��
        if (remainingSelectionsText != null)
        {
            remainingSelectionsText.text = string.Format(selectionsFormat, remaining, maxSelections);

            // ���� ������ ���� �� ���� ����
            if (remaining <= 0)
                remainingSelectionsText.color = Color.red;
            else if (remaining <= 1)
                remainingSelectionsText.color = Color.yellow;
            else
                remainingSelectionsText.color = Color.white;
        }

        // �÷��̾� ���� ǥ��
        if (playerColorIndicator != null)
        {
            playerColorIndicator.color = currentSelectedPlayer.playerColor;
        }
    }
    public void UpdateSelectionsCount(Player player, int usedCount)
    {
        if (currentSelectedPlayer == player)
        {
            usedSelections = usedCount;
            UpdateDisplay();
        }
    }
    public void ClearDisplay()
    {
        HideDisplay();
    }
}