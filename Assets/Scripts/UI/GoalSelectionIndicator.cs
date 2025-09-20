using UnityEngine;
using System.Collections.Generic;

public class GoalSelectionIndicator : MonoBehaviour
{
    [Header("Goal Visual Settings")]
    [SerializeField] private float scaleMultiplier = 1.2f;
    [SerializeField] private SelectionVisualType visualType = SelectionVisualType.Scale;

    private Dictionary<int, GameObject> playerGoalObjects = new Dictionary<int, GameObject>();
    private Dictionary<GameObject, Vector3> originalGoalScales = new Dictionary<GameObject, Vector3>();
    private Player currentSelectedPlayer;

    public enum SelectionVisualType
    {
        Scale,
        Pulse,
        ColorIntensify
    }
    void Start()
    {
        PathInput.OnPlayerSelectionChanged += HandlePlayerSelection;
        PathInput.OnPlayerCompleted += HandlePlayerCompleted;
        StartCoroutine(DelayedFindGoals());
    }

    void OnDestroy()
    {
        PathInput.OnPlayerSelectionChanged -= HandlePlayerSelection;
        PathInput.OnPlayerCompleted -= HandlePlayerCompleted;
    }

    public void HandlePlayerCompleted(Player player)
    {
        if (currentSelectedPlayer == player)
        {
            ResetGoalVisual(player.playerID);
            currentSelectedPlayer = null;
        }
    }
    System.Collections.IEnumerator DelayedFindGoals()
    {
        yield return new WaitForSeconds(0.5f);
        FindAllGoalObjects();

        if (playerGoalObjects.Count == 0)
        {
            yield return new WaitForSeconds(0.5f);
            FindAllGoalObjects();
        }
    }
    void FindAllGoalObjects()
    {
        // 기존 참조들 완전히 정리
        playerGoalObjects.Clear();
        originalGoalScales.Clear();

        if (PlayerManager.Instance != null && LevelManager.Instance?.currentLevelData?.players != null)
        {
            List<GameObject> allGoals = PlayerManager.Instance.GetAllGoals();

            for (int i = 0; i < allGoals.Count && i < LevelManager.Instance.currentLevelData.players.Length; i++)
            {
                GameObject goalObj = allGoals[i];

                // null 체크 추가
                if (goalObj != null)
                {
                    var playerData = LevelManager.Instance.currentLevelData.players[i];
                    playerGoalObjects[playerData.playerID] = goalObj;
                    originalGoalScales[goalObj] = goalObj.transform.localScale;
                }
            }
        }
    }

    public void HandlePlayerSelection(Player selectedPlayer)
    {
        if (currentSelectedPlayer != null)
        {
            ResetGoalVisual(currentSelectedPlayer.playerID);
        }

        currentSelectedPlayer = selectedPlayer;
        if (selectedPlayer != null)
        {
            ApplyGoalVisual(selectedPlayer.playerID);
        }
    }

    void ApplyGoalVisual(int playerID)
    {
        if (playerGoalObjects.ContainsKey(playerID))
        {
            GameObject goalObj = playerGoalObjects[playerID];

            switch (visualType)
            {
                case SelectionVisualType.Scale:
                    goalObj.transform.localScale = originalGoalScales[goalObj] * scaleMultiplier;
                    break;
                case SelectionVisualType.Pulse:
                    StartCoroutine(PulseGoal(goalObj));
                    break;
                case SelectionVisualType.ColorIntensify:
                    IntensifyGoalColor(goalObj);
                    break;
            }
        }
    }

    void ResetGoalVisual(int playerID)
    {
        if (playerGoalObjects.ContainsKey(playerID))
        {
            GameObject goalObj = playerGoalObjects[playerID];
            goalObj.transform.localScale = originalGoalScales[goalObj];

            // Pulse 중단
            StopAllCoroutines();
        }
    }

    System.Collections.IEnumerator PulseGoal(GameObject goalObj)
    {
        Vector3 originalScale = originalGoalScales[goalObj];

        while (currentSelectedPlayer != null)
        {
            float pulse = (Mathf.Sin(Time.time * 2f) + 1f) / 2f;
            goalObj.transform.localScale = Vector3.Lerp(originalScale, originalScale * scaleMultiplier, pulse);
            yield return null;
        }
    }

    void IntensifyGoalColor(GameObject goalObj)
    {
        SpriteRenderer renderer = goalObj.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            Color color = renderer.color;
            color.a = Mathf.Min(1f, color.a + 0.3f); // 더 불투명하게
            renderer.color = color;
        }
    }

    public void ClearSelection()
    {
        if (currentSelectedPlayer != null)
        {
            ResetGoalVisual(currentSelectedPlayer.playerID);
            currentSelectedPlayer = null;
        }
    }
}