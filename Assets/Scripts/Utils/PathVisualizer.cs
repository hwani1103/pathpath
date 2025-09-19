using UnityEngine;
using System.Collections.Generic;

public class PathVisualizer : MonoBehaviour
{
    [Header("Path Colors")]
    public Color validPathColor = Color.yellow;
    public Color invalidPathColor = Color.red;

    private LineRenderer hoverLineRenderer;
    private Dictionary<Player, LineRenderer> playerPathRenderers = new Dictionary<Player, LineRenderer>();

    void Start()
    {
        CreateHoverLineRenderer();
    }

    void CreateHoverLineRenderer()
    {
        GameObject hoverObj = new GameObject("HoverPathLine");
        hoverObj.transform.SetParent(transform);

        hoverLineRenderer = hoverObj.AddComponent<LineRenderer>();

        Material lineMat = new Material(Shader.Find("Sprites/Default"));
        hoverLineRenderer.material = lineMat;

        hoverLineRenderer.startWidth = 0.15f;
        hoverLineRenderer.endWidth = 0.15f;
        hoverLineRenderer.positionCount = 0;
        hoverLineRenderer.sortingOrder = 2;
        hoverLineRenderer.useWorldSpace = true;
        hoverLineRenderer.startColor = validPathColor;
        hoverLineRenderer.endColor = validPathColor;
    }

    public void InitializePlayerPathRenderer(Player player)
    {
        if (playerPathRenderers.ContainsKey(player)) return;

        GameObject pathObj = new GameObject($"PathLine_Player{player.playerID}");
        pathObj.transform.SetParent(transform);

        LineRenderer pathRenderer = pathObj.AddComponent<LineRenderer>();

        Material pathMat = new Material(Shader.Find("Sprites/Default"));
        pathRenderer.material = pathMat;

        pathRenderer.startColor = player.playerColor;
        pathRenderer.endColor = player.playerColor;
        pathRenderer.startWidth = 0.12f;
        pathRenderer.endWidth = 0.12f;
        pathRenderer.positionCount = 0;
        pathRenderer.sortingOrder = 1;
        pathRenderer.useWorldSpace = true;

        playerPathRenderers[player] = pathRenderer;
    }

    public void UpdatePlayerPath(Player player, List<Vector2Int> path)
    {
        if (!playerPathRenderers.ContainsKey(player)) return;

        LineRenderer pathRenderer = playerPathRenderers[player];

        if (path.Count < 2)
        {
            pathRenderer.positionCount = 0;
            return;
        }

        pathRenderer.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
        {
            Vector3 worldPos = GridManager.Instance.GridToWorld(path[i]);
            pathRenderer.SetPosition(i, worldPos);
        }
    }

    public void ShowHoverPath(Vector2Int from, Vector2Int to, bool isValid)
    {
        Color color = isValid ? validPathColor : invalidPathColor;
        hoverLineRenderer.startColor = color;
        hoverLineRenderer.endColor = color;
        hoverLineRenderer.positionCount = 2;

        Vector3 startWorldPos = GridManager.Instance.GridToWorld(from);
        Vector3 endWorldPos = GridManager.Instance.GridToWorld(to);

        hoverLineRenderer.SetPosition(0, startWorldPos);
        hoverLineRenderer.SetPosition(1, endWorldPos);
    }

    public void HideHoverPath()
    {
        hoverLineRenderer.positionCount = 0;
    }

    public void CompletePlayerPath(Player player)
    {
        if (!playerPathRenderers.ContainsKey(player)) return;

        LineRenderer pathRenderer = playerPathRenderers[player];
        pathRenderer.startColor = Color.black;
        pathRenderer.endColor = Color.black;

        StartCoroutine(HidePathAfterDelay(pathRenderer, 1.0f));
    }

    // HidePathAfterDelay 메서드 수정 - 선택 해제 코드 제거
    System.Collections.IEnumerator HidePathAfterDelay(LineRenderer pathRenderer, float delay)
    {
        yield return new WaitForSeconds(delay);
        pathRenderer.positionCount = 0;

        // 기존의 선택 해제 코드 제거됨
        // OnPlayerCompleted 이벤트가 모든 완료 처리를 담당
    }

    public void ClearAllLineRenderers()
    {
        foreach (var pathRenderer in playerPathRenderers.Values)
        {
            if (pathRenderer != null && pathRenderer.gameObject != null)
            {
                DestroyImmediate(pathRenderer.gameObject);
            }
        }

        playerPathRenderers.Clear();

        if (hoverLineRenderer != null)
        {
            hoverLineRenderer.positionCount = 0;
        }
    }
}