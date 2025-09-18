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
        // ���� ���� ��ġ�� �׸��� ��ǥ�� ��ȯ
        currentGridPos = GridManager.Instance.WorldToGrid(transform.position);
        targetGridPos = currentGridPos;

        // ��Ȯ�� �׸��� ��ġ�� ����
        transform.position = GridManager.Instance.GridToWorld(currentGridPos);

        // �÷��̾� ���� ����
        GetComponent<SpriteRenderer>().color = playerColor;
    }

    void Update()
    {
        if (isMoving)
        {
            Vector3 targetWorldPos = GridManager.Instance.GridToWorld(targetGridPos);
            transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, moveSpeed * Time.deltaTime);

            // ���� Ȯ��
            if ((transform.position - targetWorldPos).sqrMagnitude < 0.0001f)
            {
                transform.position = targetWorldPos;
                currentGridPos = targetGridPos;
                isMoving = false;

                // ���� ��ΰ� ������ ��� �̵�
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
        return currentGridPos;
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