using UnityEngine;
using System.Collections.Generic;

public class Player : MonoBehaviour
{
    [Header("Player Settings")]
    public int playerID = 1;
    public Color playerColor = Color.red;

    [Header("Movement")]
    public float moveSpeed = 2f;

    private Vector2Int currentGridPos;
    private Vector2Int targetGridPos;
    private List<Vector2Int> plannedPath = new List<Vector2Int>();
    private bool isMoving = false;

    void Start()
    {
        // 현재 월드 위치를 그리드 좌표로 변환
        currentGridPos = GridManager.Instance.WorldToGrid(transform.position);
        targetGridPos = currentGridPos;

        // 정확한 그리드 위치로 스냅
        transform.position = GridManager.Instance.GridToWorld(currentGridPos);

        // 플레이어 색상 설정
        GetComponent<SpriteRenderer>().color = playerColor;
    }
    public void ForceStop()
    {
        plannedPath.Clear();
        isMoving = false;

        // 현재 transform.position을 그대로 유지 (스냅하지 않음)
        // 단순히 이동만 중지하고 현재 위치에서 멈춤
        currentGridPos = GridManager.Instance.WorldToGrid(transform.position);
        targetGridPos = currentGridPos;

        // transform.position은 건드리지 않아서 자연스럽게 현재 위치에서 멈춤
    }
    void Update()
    {
        if (isMoving)
        {
            Vector3 targetWorldPos = GridManager.Instance.GridToWorld(targetGridPos);
            transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, moveSpeed * Time.deltaTime);

            // 도착 확인
            if ((transform.position - targetWorldPos).sqrMagnitude < 0.0001f)
            {
                transform.position = targetWorldPos;
                currentGridPos = targetGridPos;
                isMoving = false;

                // 다음 경로가 있으면 계속 이동
                if (plannedPath.Count > 0)
                {
                    MoveToNextPosition();
                }
            }
        }
    }

    public void SetPath(List<Vector2Int> path)
    {
        plannedPath = new List<Vector2Int>(path);
    }

    public void StartMoving()
    {
        if (plannedPath.Count > 0 && !isMoving)
        {
            MoveToNextPosition();
        }
    }

    private void MoveToNextPosition()
    {
        if (plannedPath.Count > 0)
        {
            targetGridPos = plannedPath[0];
            plannedPath.RemoveAt(0);
            isMoving = true;
        }
    }

    public Vector2Int GetGridPosition()
    {
        // 실제 현재 위치를 Grid 좌표로 변환
        return GridManager.Instance.WorldToGrid(transform.position);
    }

    public bool IsMoving()
    {
        return isMoving;
    }

    public void ClearPath()
    {
        plannedPath.Clear();
        isMoving = false;
    }
}