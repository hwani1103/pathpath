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

    // 디버그용 그리드 그리기
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // 선택된 상태에서만 그리기
        if (UnityEditor.Selection.activeGameObject == gameObject)
        {
            Gizmos.color = Color.green;
            Vector3 gridStartPos = gridCenter - new Vector3(gridWidth * cellSize / 2f, gridHeight * cellSize / 2f, 0f);

            // 세로 선들
            for (int x = 0; x <= gridWidth; x++)
            {
                Vector3 lineStart = gridStartPos + new Vector3(x * cellSize, 0, 0);
                Vector3 lineEnd = lineStart + new Vector3(0, gridHeight * cellSize, 0);
                Gizmos.DrawLine(lineStart, lineEnd);
            }

            // 가로 선들  
            for (int y = 0; y <= gridHeight; y++)
            {
                Vector3 lineStart = gridStartPos + new Vector3(0, y * cellSize, 0);
                Vector3 lineEnd = lineStart + new Vector3(gridWidth * cellSize, 0, 0);
                Gizmos.DrawLine(lineStart, lineEnd);
            }
        }
    }
#endif
}