using UnityEngine;

public class InputHandler : MonoBehaviour
{
    public delegate void PlayerSelectedHandler(Player player);
    public delegate void PathPointSelectedHandler(Vector2Int gridPos);

    public event PlayerSelectedHandler OnPlayerSelected;
    public event PathPointSelectedHandler OnPathPointSelected;

    [Header("Input Settings")]
    public LayerMask playerLayerMask = -1;

    private Camera mainCamera;
    private bool isDragging = false;
    private Vector3 lastInputPosition;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        HandleInput();

        Vector3 currentInputPosition = GetCurrentInputPosition();
        if (currentInputPosition != lastInputPosition)
        {
            if (isDragging)
            {
                // 드래그 이벤트는 PathVisualizer에서 처리
            }
            lastInputPosition = currentInputPosition;
        }
    }

    Vector3 GetCurrentInputPosition()
    {
        if (Input.touchCount > 0)
        {
            return Input.GetTouch(0).position;
        }
        else
        {
            return Input.mousePosition;
        }
    }

    void HandleInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                HandleTouchBegan();
            }
            else if (touch.phase == TouchPhase.Moved)
            {
                isDragging = true;
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                HandleTouchEnded();
                isDragging = false;
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                HandleTouchBegan();
            }
            if (Input.GetMouseButton(0))
            {
                isDragging = true;
            }
            if (Input.GetMouseButtonUp(0))
            {
                HandleTouchEnded();
                isDragging = false;
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            GameManager.Instance.StartSimulation();
        }
    }

    void HandleTouchBegan()
    {
        Vector3 worldPos = GetWorldPosition();

        Collider2D hitCollider = Physics2D.OverlapPoint(worldPos, playerLayerMask);
        if (hitCollider != null)
        {
            Player player = hitCollider.GetComponent<Player>();
            if (player != null)
            {
                OnPlayerSelected?.Invoke(player);
                return;
            }
        }
    }

    void HandleTouchEnded()
    {
        Vector3 worldPos = GetWorldPosition();
        Vector2Int gridPos = GridManager.Instance.WorldToGrid(worldPos);
        OnPathPointSelected?.Invoke(gridPos);
    }

    public Vector3 GetWorldPosition()
    {
        Vector3 screenPos;

        if (Input.touchCount > 0)
        {
            screenPos = Input.GetTouch(0).position;
        }
        else
        {
            screenPos = Input.mousePosition;
        }

        screenPos.z = 10f;
        return mainCamera.ScreenToWorldPoint(screenPos);
    }

    public bool IsDragging()
    {
        return isDragging;
    }
}