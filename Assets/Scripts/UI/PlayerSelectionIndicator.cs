using UnityEngine;

public class PlayerSelectionIndicator : MonoBehaviour
{
    [Header("Selection Visual Settings")]
    [SerializeField] private SelectionVisualType visualType = SelectionVisualType.Scale;
    [SerializeField] private float scaleMultiplier = 1.2f;
    [SerializeField] private Color highlightColor = Color.white;
    [SerializeField] private float pulseSpeed = 2f;

    private Player currentSelectedPlayer;
    private Vector3 originalScale;
    private Color originalColor;
    private SpriteRenderer spriteRenderer;

    public enum SelectionVisualType
    {
        Scale,
        ColorChange,
        Pulse,
        TextBox
    }

    void Start()
    {
        PathInput.OnPlayerSelectionChanged += HandlePlayerSelection;
        PathInput.OnPlayerCompleted += HandlePlayerCompleted;
    }

    void OnDestroy()
    {
        PathInput.OnPlayerSelectionChanged -= HandlePlayerSelection;
        PathInput.OnPlayerCompleted -= HandlePlayerCompleted;
    }

    public void HandlePlayerSelection(Player selectedPlayer)
    {
        if (currentSelectedPlayer != null)
        {
            ResetPlayerVisual();
        }

        currentSelectedPlayer = selectedPlayer;
        if (selectedPlayer != null)
        {
            ApplySelectionVisual();
        }
    }

    public void HandlePlayerCompleted(Player player)
    {
        if (currentSelectedPlayer == player)
        {
            ResetPlayerVisual();
            currentSelectedPlayer = null;
        }
    }

    void ApplySelectionVisual()
    {
        if (currentSelectedPlayer == null) return;

        var completionManager = GetComponent<PlayerCompletionManager>();
        if (completionManager != null && completionManager.IsPlayerCompleted(currentSelectedPlayer.playerID))
        {
            return;
        }

        spriteRenderer = currentSelectedPlayer.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) return;

        originalScale = currentSelectedPlayer.transform.localScale;
        originalColor = spriteRenderer.color;

        switch (visualType)
        {
            case SelectionVisualType.Scale:
                currentSelectedPlayer.transform.localScale = originalScale * scaleMultiplier;
                break;
            case SelectionVisualType.ColorChange:
                spriteRenderer.color = highlightColor;
                break;
            case SelectionVisualType.Pulse:
                StartCoroutine(PulseEffect());
                break;
        }
    }

    System.Collections.IEnumerator PulseEffect()
    {
        while (currentSelectedPlayer != null)
        {
            float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
            currentSelectedPlayer.transform.localScale = Vector3.Lerp(originalScale, originalScale * scaleMultiplier, pulse);
            yield return null;
        }
    }

    void ResetPlayerVisual()
    {
        if (currentSelectedPlayer == null) return;

        currentSelectedPlayer.transform.localScale = originalScale;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        StopAllCoroutines();
    }

    public void ClearSelection()
    {
        if (currentSelectedPlayer != null)
        {
            ResetPlayerVisual();
            currentSelectedPlayer = null;
        }
    }
}