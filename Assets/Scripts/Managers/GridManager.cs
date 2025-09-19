using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridWidth = 6;
    public int gridHeight = 12;
    public float cellSize = 1f;
    public Vector3 gridCenter = new Vector3(2.5f, 5.5f, 0f);

    private static GridManager instance;
    public static GridManager Instance { get { return instance; } }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 월드 좌표를 그리드 좌표로 변환
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector3 gridPos = worldPos - gridCenter + new Vector3(gridWidth * cellSize / 2f, gridHeight * cellSize / 2f, 0f);
        int x = Mathf.FloorToInt(gridPos.x / cellSize);
        int y = Mathf.FloorToInt(gridPos.y / cellSize);
        return new Vector2Int(x, y);
    }

    // 그리드 좌표를 월드 좌표로 변환
    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        float x = gridCenter.x - (gridWidth * cellSize / 2f) + (gridPos.x + 0.5f) * cellSize;
        float y = gridCenter.y - (gridHeight * cellSize / 2f) + (gridPos.y + 0.5f) * cellSize;
        return new Vector3(x, y, 0f);
    }

    // 그리드 범위 내인지 확인
    public bool IsValidGridPosition(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < gridWidth &&
               gridPos.y >= 0 && gridPos.y < gridHeight;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // 항상 좌표 표시 (선택 여부와 관계없이)
        if (Application.isPlaying) return; // Play 모드에서는 숨김

        Gizmos.color = Color.yellow;
        Vector3 gridStartPos = gridCenter - new Vector3(gridWidth * cellSize / 2f, gridHeight * cellSize / 2f, 0f);

        // 격자 그리기
        for (int x = 0; x <= gridWidth; x++)
        {
            Vector3 lineStart = gridStartPos + new Vector3(x * cellSize, 0, 0);
            Vector3 lineEnd = lineStart + new Vector3(0, gridHeight * cellSize, 0);
            Gizmos.DrawLine(lineStart, lineEnd);
        }

        for (int y = 0; y <= gridHeight; y++)
        {
            Vector3 lineStart = gridStartPos + new Vector3(0, y * cellSize, 0);
            Vector3 lineEnd = lineStart + new Vector3(gridWidth * cellSize, 0, 0);
            Gizmos.DrawLine(lineStart, lineEnd);
        }

        // 좌표 텍스트 표시
        UnityEditor.Handles.color = Color.white;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3 cellCenter = gridStartPos + new Vector3((x + 0.5f) * cellSize, (y + 0.5f) * cellSize, 0);
                UnityEditor.Handles.Label(cellCenter, $"({x},{y})", new GUIStyle()
                {
                    normal = { textColor = Color.white },
                    fontSize = 10,
                    alignment = TextAnchor.MiddleCenter
                });
            }
        }
    }
#endif
}