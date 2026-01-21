using UnityEngine;

public class ObjectManager : MonoBehaviour
{
    [Header("オブジェクトの最大拡大量")]
    public float ScaleX = 14f;
    public float ScaleY = 7f;
    public float ScaleZ = 2f;

    [Header("接地・移動設定")]
    public float MaxY = 10f; // 天井の高さ
    private const float MinY = 0f;
    public LayerMask Ground;

    [Header("移動制限エリア (XZ軸)")]
    private const float MinX = -7f;
    private const float MinZ = -7f;
    private const float MaxX = 7f;
    private const float MaxZ = 7f;

    [HideInInspector] public bool isDragging = false;

    private Collider objectCollider;

    void Start()
    {
        objectCollider = GetComponent<Collider>();
        ClampObject(false);
    }

    private void Update()
    {
        if (isDragging || transform.hasChanged)
        {

            ClampObject(false);
            transform.hasChanged = false;
        }
    }

    /// <summary>
    /// オブジェクトの位置と大きさを制限エリア内に収めます。
    /// 2段階のチェックを行い、見た目のはみ出しも防ぎます。
    /// </summary>
    /// <param name="snapToFloor">trueの場合、強制的に地面の高さに合わせる</param>
    public void ClampObject(bool snapToFloor)
    {
        if (objectCollider == null) return;

        Vector3 s = transform.localScale;
        s.x = Mathf.Clamp(s.x, 0.1f, ScaleX);
        s.y = Mathf.Clamp(s.y, 0.1f, ScaleY);
        s.z = Mathf.Clamp(s.z, 0.1f, ScaleZ);
        transform.localScale = s;

        Vector3 currentPos = transform.position;

        currentPos.x = Mathf.Clamp(currentPos.x, MinX, MaxX);
        currentPos.z = Mathf.Clamp(currentPos.z, MinZ, MaxZ);

        transform.position = currentPos;

        Physics.SyncTransforms();

        Bounds bounds = objectCollider.bounds;

        float offsetX = 0f;
        float offsetZ = 0f;

        if (bounds.min.x < MinX) offsetX = MinX - bounds.min.x;
        else if (bounds.max.x > MaxX) offsetX = MaxX - bounds.max.x;

        if (bounds.min.z < MinZ) offsetZ = MinZ - bounds.min.z;
        else if (bounds.max.z > MaxZ) offsetZ = MaxZ - bounds.max.z;

        currentPos.x += offsetX;
        currentPos.z += offsetZ;

        transform.position = currentPos;
        Physics.SyncTransforms();
        bounds = objectCollider.bounds;

        float finalY = currentPos.y;

        if (snapToFloor)
        {
            float pivotToBottom = currentPos.y - bounds.min.y;
            finalY = MinY + pivotToBottom;
        }
        else
        {

            float distToBottom = currentPos.y - bounds.min.y;
            float distToTop = bounds.max.y - currentPos.y;

            float minPivotY = MinY + distToBottom;
            float maxPivotY = MaxY - distToTop;

            if (minPivotY > maxPivotY)
            {
                maxPivotY = minPivotY;
            }

            finalY = Mathf.Clamp(currentPos.y, minPivotY, maxPivotY);
        }

        transform.position = new Vector3(currentPos.x, finalY, currentPos.z);
    }
}