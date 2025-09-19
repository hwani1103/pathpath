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
            // PathManager를 이벤트 기반으로 찾지 않고 PathInput에서 직접 계산해서 전달하는 방식으로 변경 예정
            // 일단 기존 코드 유지하되, FindFirstObjectByType 최소화
            var pathManager = GetComponent<PathManager>(); // 같은 오브젝트 내에서 찾기
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

        // 플레이어 데이터 가져오기
        PlayerSpawnData playerData = PlayerManager.Instance.GetPlayerData(currentSelectedPlayer.playerID);
        if (playerData == null) return;

        int maxSelections = playerData.maxSelections;
        int remaining = maxSelections - usedSelections; // 남은 횟수 = 최대 - 사용한 횟수

        // 플레이어 이름 표시
        if (showPlayerName && playerNameText != null)
        {
            playerNameText.text = string.Format(playerNameFormat, currentSelectedPlayer.playerID);
        }

        // 남은 선택 수 표시
        if (remainingSelectionsText != null)
        {
            remainingSelectionsText.text = string.Format(selectionsFormat, remaining, maxSelections);

            // 남은 선택이 적을 때 색상 변경
            if (remaining <= 0)
                remainingSelectionsText.color = Color.red;
            else if (remaining <= 1)
                remainingSelectionsText.color = Color.yellow;
            else
                remainingSelectionsText.color = Color.white;
        }

        // 플레이어 색상 표시
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