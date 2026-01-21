using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraCtrler : MonoBehaviour
{
    [Header("感度設定")]
    [SerializeField, Range(0.1f, 10f)] private float wheelSpeed = 1.0f;
    [SerializeField, Range(0.1f, 10f)] private float moveSpeed = 0.5f;
    [SerializeField, Range(0.1f, 10f)] private float rotateSpeed = 0.2f;

    [SerializeField] private float cameraRadius = 0.2f;
    [SerializeField] private LayerMask collisionLayer;
    [SerializeField] private float minNearClip = 0.01f;

    [SerializeField] private Vector3 minBounds = new Vector3(-15f, 0.1f, -15f);
    [SerializeField] private Vector3 maxBounds = new Vector3(15f, 15f, 15f);

    private Vector3 preMousePos;

    private void Awake()
    {
        Camera cam = GetComponent<Camera>();
        cam.nearClipPlane = minNearClip;
    }

    private void Update()
    {
        HandleMouseInput();
    }

    private void HandleMouseInput()
    {
        //スクロールホイールでのズーム
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f) ApplyZoom(scroll);

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
        {
            preMousePos = Input.mousePosition;
        }

        ApplyDragNavigation(Input.mousePosition);
    }

    private void ApplyZoom(float delta)
    {
        Vector3 moveDir = transform.forward * delta * wheelSpeed;
        MoveWithCollision(moveDir);
    }

    private void ApplyDragNavigation(Vector3 mousePos)
    {
        Vector3 diff = mousePos - preMousePos;
        if (diff.sqrMagnitude < Vector3.kEpsilon) return;

        bool isAltPressed = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        //中ボタン
        if (Input.GetMouseButton(2))
        {
            Vector3 move = -diff * Time.deltaTime * moveSpeed;
            Vector3 moveDir = transform.TransformDirection(move.x, move.y, 0f);
            MoveWithCollision(moveDir);
        }
        //右ボタン
        else if (Input.GetMouseButton(1) && !isAltPressed)
        {
            RotateCamera(diff);
        }
        //Alt + 左ボタン
        else if (Input.GetMouseButton(0) && isAltPressed)
        {
            RotateCamera(diff);
        }

        preMousePos = mousePos;
    }

    private void MoveWithCollision(Vector3 moveDir)
    {
        Vector3 targetPos = transform.position + moveDir;

        RaycastHit hit;
        if (Physics.SphereCast(transform.position, cameraRadius, moveDir.normalized, out hit, moveDir.magnitude, collisionLayer))
        {
            targetPos = transform.position + moveDir.normalized * (hit.distance - 0.01f);
        }

        transform.position = ClampPosition(targetPos);
    }

    private void RotateCamera(Vector3 mouseDiff)
    {
        Vector2 angle = new Vector2(-mouseDiff.y, mouseDiff.x) * rotateSpeed;
        transform.RotateAround(transform.position, transform.right, angle.x);
        transform.RotateAround(transform.position, Vector3.up, angle.y);
    }

    private Vector3 ClampPosition(Vector3 targetPos)
    {
        return new Vector3(
            Mathf.Clamp(targetPos.x, minBounds.x, maxBounds.x),
            Mathf.Clamp(targetPos.y, minBounds.y, maxBounds.y),
            Mathf.Clamp(targetPos.z, minBounds.z, maxBounds.z)
        );
    }
}