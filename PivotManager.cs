using System.Collections.Generic;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class PivotManager : MonoBehaviour
{
    private MainManager mainmanager;
    private PivotViewManager pivotviewmanager;

    private Dictionary<Transform, Vector3> _startPositions = new Dictionary<Transform, Vector3>();
    private Dictionary<Transform, Vector3> _startScale = new Dictionary<Transform, Vector3>();
    private Dictionary<Transform, Quaternion> _startRotation = new Dictionary<Transform, Quaternion>();
    private bool _wasDraggingLastFrame = false;

    public GameObject ParentPivot;
    [SerializeField, Header("選択中のオブジェクト")]
    public List<YataiItem> _SelectObjects;

    [SerializeField]
    public Camera cameraPos;

    public GameObject autoTargetObject;
    //ピボットの生成位置
    public Transform[] PivotPosition;

    [Header("色設定")]
    public Color highlightColor;
    public Color highlightEmition;

    public Color GuideLineColor;

    public Color OriginalXColor;
    public Color OriginalYColor;
    public Color OriginalZColor;

    public Color OriginalXEmissionColor;
    public Color OriginalYEmissionColor;
    public Color OriginalZEmissionColor;

    [SerializeField, Header("移動/拡縮")]

    public LineRenderer XMoveAndScaleline;
    public LineRenderer XMoveAndScalepin;
    public LineRenderer YMoveAndScaleline, YMoveAndScalepin;
    public LineRenderer ZMoveAndScaleline, ZMoveAndScalepin;
    public CapsuleCollider XLinecapsuleCollider;
    public CapsuleCollider YLinecapsuleCollider;
    public CapsuleCollider ZLinecapsuleCollider;
    public float scale = 0.489f;
    public float length = 0.5f;
    public bool isDragging;
    public bool x, y, z;

    [SerializeField] private float RadiusScale = 0.8f;
    [SerializeField] private int segments = 1;
    [SerializeField, Header("回転")]
    public LineRenderer XRotateLine;
    public LineRenderer YRotateLine;
    public LineRenderer ZRotateLine;
    // 回転用に追加する変数
    private float totalMouseMovement = 0f;
    [Header("回転設定")]
    [SerializeField] private float snapAngle = 15f;
    [SerializeField] private float sensitivity = 10f;

    private List<GameObject> XArcColliderPrefab = new List<GameObject>();
    private List<GameObject> YArcColliderPrefab = new List<GameObject>();
    private List<GameObject> ZArcColliderPrefab = new List<GameObject>();

    [Header("回転のコライダーのオリジナル")]
    public GameObject ColliderOriginalObject;

    public GameObject XRotate_Collider;
    public GameObject YRotate_Collider;
    public GameObject ZRotate_Collider;

    [SerializeField, Header("回転ガイドライン")]
    public LineRenderer XRotateGuideline;
    public LineRenderer YRotateGuideline;
    public LineRenderer ZRotateGuideline;

    private bool isLine;
    private string flag;

    private Vector3 _hitPointWorld;
    private Vector2 _prevMousePos;
    private Vector3 _currentRotationAxis;

    public bool IsgridCheck = false;

    void Start()
    {
        mainmanager = FindObjectOfType<MainManager>();
        pivotviewmanager = FindObjectOfType<PivotViewManager>();

        if (cameraPos == null)
        {
            cameraPos = Camera.main;
        }

        OriginalXColor = XMoveAndScaleline.materials[0].color;
        OriginalYColor = YMoveAndScaleline.materials[0].color;
        OriginalZColor = ZMoveAndScaleline.materials[0].color;

        OriginalXEmissionColor = XMoveAndScaleline.materials[0].GetColor("_EmissionColor");
        OriginalYEmissionColor = YMoveAndScaleline.materials[0].GetColor("_EmissionColor");
        OriginalZEmissionColor = ZMoveAndScaleline.materials[0].GetColor("_EmissionColor");

        OriginalXEmissionColor = XRotateLine.materials[0].GetColor("_EmissionColor");
        OriginalYEmissionColor = YRotateLine.materials[0].GetColor("_EmissionColor");
        OriginalZEmissionColor = ZRotateLine.materials[0].GetColor("_EmissionColor");
    }

    void Update()
    {
        GizmoModeManager.HandleModeSwitchInput();

        _SelectObjects = mainmanager.SelectObjects;

        if (_SelectObjects != null && _SelectObjects.Count > 0)
        {
            Vector3 CenterPosition = Vector3.zero;
            foreach (var item in _SelectObjects)
            {
                CenterPosition += item.transform.position;
                if (this.transform.rotation != item.transform.rotation)
                {
                    this.transform.rotation = item.transform.rotation;
                }

            }
            CenterPosition /= _SelectObjects.Count;
            transform.position = CenterPosition;
        }

        CreatMoveAndScalePivot(true, false, false, XMoveAndScaleline, XMoveAndScalepin, XLinecapsuleCollider, OriginalXColor);
        CreatMoveAndScalePivot(false, true, false, YMoveAndScaleline, YMoveAndScalepin, YLinecapsuleCollider, OriginalYColor);
        CreatMoveAndScalePivot(false, false, true, ZMoveAndScaleline, ZMoveAndScalepin, ZLinecapsuleCollider, OriginalZColor);

        CreateRotatePivot(true, false, false, XRotateLine, OriginalXColor, XArcColliderPrefab, XRotate_Collider, XRotateGuideline);
        CreateRotatePivot(false, true, false, YRotateLine, OriginalYColor, YArcColliderPrefab, YRotate_Collider, YRotateGuideline);
        CreateRotatePivot(false, false, true, ZRotateLine, OriginalZColor, ZArcColliderPrefab, ZRotate_Collider, ZRotateGuideline);
        if (IsDragCtrl())
        {
            isDragging = true;
        }

        if (isDragging)
        {
            if (x)
            {
                EmissionChange(XMoveAndScaleline, highlightColor);
                EmissionChange(XMoveAndScalepin, highlightColor);
            }
            if (y)
            {
                EmissionChange(YMoveAndScaleline, highlightColor);
                EmissionChange(YMoveAndScalepin, highlightColor);
            }
            if (z)
            {
                EmissionChange(ZMoveAndScaleline, highlightColor);
                EmissionChange(ZMoveAndScalepin, highlightColor);
            }
        }
        switch (GizmoModeManager.CurrentMode)
        {
            case PivotStyle.Move:
            case PivotStyle.Scale:
                {
                    if (isDragging)
                    {
                        if (!_wasDraggingLastFrame)
                        {
                            _startPositions.Clear();
                            _startScale.Clear();
                            foreach (var item in _SelectObjects)
                            {
                                _startPositions[item.transform] = item.transform.position;
                                _startScale[item.transform] = item.transform.localScale;
                            }
                            _wasDraggingLastFrame = true;
                            totalMouseMovement = 0f;
                        }

                        Vector2 NowMousePos = Input.mousePosition;
                        Vector2 mouseDelta = NowMousePos - _prevMousePos;

                        float factor = GetDynamicFactor();

                        Vector3 cameraRight = cameraPos.transform.right;
                        Vector3 cameraUp = cameraPos.transform.up;
                        Vector3 D_3D = (cameraRight * mouseDelta.x + cameraUp * mouseDelta.y) * factor;

                        Vector3 targetAxis = Vector3.zero;
                        if (x) targetAxis = transform.right;
                        else if (y) targetAxis = transform.up;
                        else if (z) targetAxis = transform.forward;

                        float dotMovement = Vector3.Dot(D_3D, targetAxis);

                        float movementAmount = dotMovement * (sensitivity * 0.1f);

                        float snapStep = 0.1f;
                        float finalIncrement = 0f;

                        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || IsgridCheck)
                        {
                            finalIncrement = movementAmount;
                        }
                        else
                        {
                            totalMouseMovement += movementAmount;
                            if (Mathf.Abs(totalMouseMovement) >= snapStep)
                            {
                                finalIncrement = Mathf.Sign(totalMouseMovement) * snapStep;
                                totalMouseMovement -= finalIncrement;
                            }
                        }

                        if (finalIncrement != 0f)
                        {
                            foreach (var item in _SelectObjects)
                            {
                                if (GizmoModeManager.CurrentMode == PivotStyle.Move)
                                {
                                    item.transform.position += targetAxis * finalIncrement;
                                }
                                else
                                {
                                    Vector3 currentScale = item.transform.localScale;
                                    if (x) currentScale.x += finalIncrement;
                                    if (y) currentScale.y += finalIncrement;
                                    if (z) currentScale.z += finalIncrement;

                                    currentScale.x = Mathf.Max(currentScale.x, 0.1f);
                                    currentScale.y = Mathf.Max(currentScale.y, 0.1f);
                                    currentScale.z = Mathf.Max(currentScale.z, 0.1f);
                                    item.transform.localScale = currentScale;
                                }
                            }
                            if (GizmoModeManager.CurrentMode == PivotStyle.Move)
                            {
                                transform.position += targetAxis * finalIncrement;
                            }
                        }

                        if (x) EmissionChange(XMoveAndScaleline, highlightColor);
                        else if (y) EmissionChange(YMoveAndScaleline, highlightColor);
                        else if (z) EmissionChange(ZMoveAndScaleline, highlightColor);

                        _prevMousePos = NowMousePos;
                    }
                    else
                    {
                        if (_wasDraggingLastFrame)
                        {
                            RecordUndoCommand(GizmoModeManager.CurrentMode);
                            _wasDraggingLastFrame = false;
                        }
                        totalMouseMovement = 0f;
                        ResetEmissions();
                    }
                    break;
                }
            case PivotStyle.Rotate:
                {
                    if (isDragging)
                    {
                        if (!_wasDraggingLastFrame)
                        {
                            _startRotation.Clear();
                            foreach (var item in _SelectObjects) _startRotation[item.transform] = item.transform.localRotation; ;
                            _wasDraggingLastFrame = true;
                        }

                        Vector2 NowMousePos = Input.mousePosition;
                        Vector2 Distance = NowMousePos - _prevMousePos;

                        Vector3 C = transform.position;
                        Vector3 R_World = _hitPointWorld - C;

                        Vector3 RotateAxis = Vector3.zero;
                        if (x) RotateAxis = transform.right;
                        else if (y) RotateAxis = transform.up;
                        else if (z) RotateAxis = transform.forward;

                        if (RotateAxis != Vector3.zero && Distance.magnitude > 0)
                        {

                            Vector3 cameraRight = cameraPos.transform.right;
                            Vector3 cameraUp = cameraPos.transform.up;
                            Vector3 T = Vector3.Cross(RotateAxis, R_World).normalized;


                            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || IsgridCheck)
                            {
                                float factor = R_World.magnitude / 30f;
                                Vector3 D_3D = (cameraRight * Distance.x + cameraUp * Distance.y) * factor;
                                float Dot = Vector3.Dot(D_3D, T);
                                float R_magnitude = R_World.magnitude;
                                float rotationAngleDegrees = (Dot / R_magnitude) * sensitivity;
                                if (Mathf.Abs(rotationAngleDegrees) > 0.001f)
                                {

                                    if (RotateAxis != Vector3.zero)
                                    {
                                        foreach (var item in _SelectObjects)
                                        {
                                            item.transform.RotateAround(C, RotateAxis.normalized, rotationAngleDegrees);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                float factor = R_World.magnitude / 10f;
                                Vector3 D_3D = (cameraRight * Distance.x + cameraUp * Distance.y) * factor;
                                float Dot = Vector3.Dot(D_3D, T);
                                float R_magnitude = R_World.magnitude;
                                float rotationAngleDegrees = (Dot / R_magnitude) * sensitivity;

                                totalMouseMovement += rotationAngleDegrees;
                                float rotationAmount = 0f;
                                float requiredMovement = snapAngle;
                                if (Mathf.Abs(totalMouseMovement) >= requiredMovement)
                                {
                                    rotationAmount = Mathf.Sign(totalMouseMovement) * snapAngle;
                                    foreach (var item in _SelectObjects)
                                    {
                                        item.transform.RotateAround(C, RotateAxis.normalized, rotationAmount);
                                        ParentPivot.transform.RotateAround(C, RotateAxis.normalized, rotationAmount);


                                    }
                                    totalMouseMovement -= rotationAmount;
                                }
                            }
                        }

                        if (x) EmissionChange(XRotateLine, highlightColor);
                        else if (y) EmissionChange(YRotateLine, highlightColor);
                        else if (z) EmissionChange(ZRotateLine, highlightColor);

                        _prevMousePos = NowMousePos;

                    }
                    else
                    {
                        if (_wasDraggingLastFrame)
                        {
                            var data = new Dictionary<Transform, (Quaternion, Quaternion)>();
                            foreach (var item in _SelectObjects)
                            {
                                data[item.transform] = (_startRotation[item.transform], item.transform.localRotation);
                            }
                            UndoManager.Instance.PushExistingCommand(new RotateCommand(data));
                            _wasDraggingLastFrame = false;
                        }

                        totalMouseMovement = 0f;

                        EmissionChange(XRotateLine, OriginalXColor);
                        EmissionChange(YRotateLine, OriginalYColor);
                        EmissionChange(ZRotateLine, OriginalZColor);
                    }
                    break;
                }
        }
    }
    /// <summary>
    /// 移動・拡大のピボットの生成
    /// </summary>
    /// <param name="x">x軸方向に線を伸ばす</param>
    /// <param name="y">y軸方向に線を伸ばす</param>
    /// <param name="z">z軸方向に線を伸ばす</param>
    /// <param name="line">伸ばす線</param>
    /// <param name="pin">先端</param>
    public void CreatMoveAndScalePivot(bool x, bool y, bool z, LineRenderer line, LineRenderer pin, CapsuleCollider capsuleCollider, Color color)
    {
        float CameraDistance = Vector3.Distance(transform.position, cameraPos.transform.position);
        length = scale * CameraDistance;

        if (GizmoModeManager.CurrentMode == PivotStyle.Move || GizmoModeManager.CurrentMode == PivotStyle.Scale)
        {
            line.material.color = color;
            line.material.SetColor("_EmissionColor", color);
            line.material.EnableKeyword("_EMISSION");

            pin.material.color = color;
            pin.material.SetColor("_EmissionColor", color);
            pin.material.EnableKeyword("_EMISSION");

            line.enabled = true;
            pin.enabled = true;
            line.positionCount = 2;
            Vector3[] point = new Vector3[2]
            {
                transform.position,
                transform.position +((x?transform.right:Vector3.zero)+ (y?transform.up:Vector3.zero) + (z?transform.forward:Vector3.zero))* length
            };//始点から終点
            Vector3 normal = (point[1] - point[0]) / 7;
            line.SetPositions(point);
            pin.SetPositions(new Vector3[] { point[1], point[1] + normal });
            line.startWidth = line.endWidth = length / 20;
            if (GizmoModeManager.CurrentMode == PivotStyle.Scale) // スケールモード
            {
                Vector3 scalenormal = normal / 2;
                pin.SetPositions(new Vector3[] { point[1], point[1] + scalenormal });
                pin.startWidth = length / 12;
                pin.endWidth = length / 12;
            }
            else if (GizmoModeManager.CurrentMode == PivotStyle.Move) // 移動モード
            {
                pin.SetPositions(new Vector3[] { point[1], point[1] + normal });
                pin.startWidth = length / 12;
                pin.endWidth = 0; //尖ったピボット
            }
            float centerDistance = length * 0.75f;

            if (x)
            {
                capsuleCollider.center = new Vector3(centerDistance, 0, 0);
                capsuleCollider.direction = 0;
            }
            else if (y)
            {
                capsuleCollider.center = new Vector3(0, centerDistance, 0);
                capsuleCollider.direction = 1;
            }
            else if (z)
            {
                capsuleCollider.center = new Vector3(0, 0, centerDistance);
                capsuleCollider.direction = 2;
            }
            capsuleCollider.radius = length / 9;
            capsuleCollider.height = length;

            if (x) { capsuleCollider.direction = 0; }
            if (y) { capsuleCollider.direction = 1; }
            if (z) { capsuleCollider.direction = 2; }
        }
        else
        {
            line.enabled = false;
            pin.enabled = false;
        }

        capsuleCollider.enabled = (GizmoModeManager.CurrentMode == PivotStyle.Move || GizmoModeManager.CurrentMode == PivotStyle.Scale);
        if ((Input.GetMouseButtonDown(0) && (GizmoModeManager.CurrentMode == PivotStyle.Move) ||
            (Input.GetMouseButtonDown(0) && GizmoModeManager.CurrentMode == PivotStyle.Scale)))
        {
            Ray ray = cameraPos.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            LayerMask layerMask = LayerMask.GetMask("Pivot");

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
            {
                if (hit.transform == transform)
                {
                    isDragging = true;
                }
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
    }
    /// <summary>
    /// 回転のピボットの生成
    /// </summary>
    /// <param name="x">x軸方向に線を伸ばす</param>
    /// <param name="y">y軸方向に線を伸ばす</param>
    /// <param name="z">z軸方向に線を伸ばす</param>
    /// <param name="line">伸ばす線</param>
    /// <param name="boxcollider">コライダー</param>
    /// <param name="color">カラー</param>
    public void CreateRotatePivot(bool x, bool y, bool z, LineRenderer line, Color color, List<GameObject> arcColliderList, GameObject parentObject, LineRenderer guideline)
    {
        float CameraDistance = Vector3.Distance(transform.position, cameraPos.transform.position);
        length = scale * CameraDistance;
        float currentRadius = length * RadiusScale;

        if (GizmoModeManager.CurrentMode != PivotStyle.Rotate)
        {

            line.enabled = false;
            guideline.enabled = false;

            for (int i = 0; i < arcColliderList.Count; i++)
            {
                if (arcColliderList[i] != null) arcColliderList[i].SetActive(false);
            }
            return;
        }

        line.enabled = true;
        guideline.enabled = true;
        line.positionCount = segments + 1;

        Vector3 axis1 = Vector3.zero;
        Vector3 normalAxis = Vector3.zero;


        if (ColliderOriginalObject != null)
        {
            ColliderOriginalObject.transform.position = transform.position;
            ColliderOriginalObject.transform.rotation = transform.rotation;
        }

        if (GizmoModeManager.CurrentMode == PivotStyle.Rotate)
        {
            line.material.color = color;
            line.material.SetColor("_EmissionColor", color);
            line.material.EnableKeyword("_EMISSION");

            if (x)
            {
                axis1 = transform.up;
                normalAxis = transform.right;
                flag = "x";
                isLine = false;
            }
            else if (y)
            {
                axis1 = transform.right;
                normalAxis = -transform.up;
                flag = "y";
                isLine = true;
            }
            else if (z)
            {
                axis1 = transform.up;
                normalAxis = -transform.forward;
                flag = "z";
                isLine = true;
            }

            guideline.material.color = GuideLineColor; // 灰色（設定した色）
            guideline.material.SetColor("_EmissionColor", Color.black);
            guideline.material.DisableKeyword("_EMISSION");
            guideline.startWidth = guideline.endWidth = length / 40;
            guideline.positionCount = 2;

            Vector3 FirstPosition = Vector3.zero;
            Vector3 lastPosition = Vector3.zero;

            //円弧の描画とコライダーの生成
            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * 90f;
                Quaternion rotation = Quaternion.AngleAxis(angle, normalAxis);

                Vector3 currentDir = rotation * axis1;

                line.SetPosition(i, transform.position + currentDir * currentRadius);
                if (i == 0)
                {
                    FirstPosition = transform.position + currentDir * currentRadius;
                }
                if (i == segments)
                {
                    lastPosition = transform.position + currentDir * currentRadius;
                }

                if (ColliderOriginalObject != null && i < segments)
                {
                    Vector3 spawnPosition = transform.position + currentDir * currentRadius;
                    Quaternion spawnRotation = rotation * ColliderOriginalObject.transform.rotation;
                    string flagName = flag;
                    RotateColliderCreate(ColliderOriginalObject, spawnPosition, spawnRotation, i, flagName, arcColliderList, parentObject, currentRadius);
                }
            }
            if (isLine)
            {
                guideline.SetPositions(new Vector3[] { lastPosition, transform.position });
            }
            else
            {
                guideline.SetPositions(new Vector3[] { FirstPosition, transform.position });
            }
            line.startWidth = line.endWidth = length / 20;
        }
        else
        {
            line.enabled = false;
        }

        if (Input.GetMouseButtonDown(0) && GizmoModeManager.CurrentMode == PivotStyle.Rotate)
        {
            Ray ray = cameraPos.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            LayerMask layerMask = LayerMask.GetMask("Pivot");
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
            {
                if (hit.transform == transform)
                {
                    isDragging = true;
                }
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
    }
    /// <summary>
    /// 色を一時的に別の色に変える
    /// </summary>
    /// <param name="origin">変えるLineもしくはpin</param>
    /// <param name="color">変える色</param>
    public void EmissionChange(LineRenderer origin, Color color)
    {
        origin.material.color = color;
        origin.material.SetColor("_EmissionColor", color);
        origin.material.EnableKeyword("_EMISSION");
    }

    /// <summary>
    /// どのピボット軸がレイキャストでヒットしたか
    /// </summary>
    /// <returns>いずれかのピボット軸がドラッグ開始された場合 true</returns>
    public bool IsDragCtrl()
    {
        if (Input.GetMouseButtonUp(0))
        {
            x = y = z = false;
            isDragging = false;
        }

        if (isDragging)
        {
            return true;
        }

        bool isInputDown = Input.GetMouseButtonDown(0);
        bool isPivotMode = (GizmoModeManager.CurrentMode == PivotStyle.Move ||
                                  GizmoModeManager.CurrentMode == PivotStyle.Scale ||
                                  GizmoModeManager.CurrentMode == PivotStyle.Rotate);

        if (isInputDown && isPivotMode)
        {
            Ray ray = cameraPos.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            LayerMask layerMask = LayerMask.GetMask("Pivot");

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
            {

                Transform hitTransform = hit.collider.transform;

                x = y = z = false;

                if (GizmoModeManager.CurrentMode == PivotStyle.Move || GizmoModeManager.CurrentMode == PivotStyle.Scale)
                {
                    if (hitTransform == XLinecapsuleCollider.transform)
                    {
                        x = true;
                        isDragging = true;
                        _prevMousePos = Input.mousePosition;
                        return true;
                    }
                    else if (hitTransform == YLinecapsuleCollider.transform)
                    {
                        y = true;
                        isDragging = true;
                        _prevMousePos = Input.mousePosition;
                        return true;
                    }
                    else if (hitTransform == ZLinecapsuleCollider.transform)
                    {
                        z = true;
                        isDragging = true;
                        _prevMousePos = Input.mousePosition;
                        return true;
                    }
                }
                else if (GizmoModeManager.CurrentMode == PivotStyle.Rotate)
                {
                    Transform firstParent = hitTransform.parent;
                    Transform secondParent = null;

                    if (firstParent != null)
                    {
                        secondParent = firstParent.parent;
                    }
                    if (secondParent != null)
                    {
                        if (secondParent.name == "xCircle" || secondParent.name == "xL")
                        {
                            _hitPointWorld = hit.point;
                            _prevMousePos = Input.mousePosition;
                            x = true;
                            isDragging = true;
                            return true;
                        }
                        else if (secondParent.name == "yCircle" || secondParent.name == "yL")
                        {
                            _hitPointWorld = hit.point;
                            _prevMousePos = Input.mousePosition;
                            y = true;
                            isDragging = true;
                            return true;
                        }
                        else if (secondParent.name == "zCircle" || secondParent.name == "zL")
                        {
                            _hitPointWorld = hit.point;
                            _prevMousePos = Input.mousePosition;
                            z = true;
                            isDragging = true;
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }
    /// <summary>
    /// コライダーの作成
    /// </summary>
    /// <param name="ColliderObject"></param>
    /// <param name="spawnPosition"></param>
    /// <param name="spawnRotation"></param>
    /// <param name="segmentIndex"></param>
    /// <param name="Collider">格納するコライダー</param>
    public void RotateColliderCreate(GameObject ColliderObject, Vector3 spawnPosition, Quaternion spawnRotation, int segmentIndex, string flag, List<GameObject> arcColliderList, GameObject parentObject, float currentRadius)
    {
        GameObject currentCollider = null;

        if (arcColliderList.Count <= segmentIndex)
        {
            currentCollider = GameObject.Instantiate(ColliderObject);
            currentCollider.name = flag + "_Collider_" + segmentIndex.ToString();
            arcColliderList.Add(currentCollider);

            currentCollider.transform.SetParent(parentObject.transform);

            MeshRenderer renderer = currentCollider.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }

        }
        else
        {
            currentCollider = arcColliderList[segmentIndex];
            if (currentCollider.transform.parent != parentObject.transform)
            {
                currentCollider.transform.SetParent(parentObject.transform);
            }
        }

        if (currentCollider != null)
        {
            currentCollider.SetActive(true);
            currentCollider.transform.position = spawnPosition;
            currentCollider.transform.rotation = spawnRotation;

            float colliderScale = currentRadius * 0.06f;
            currentCollider.transform.localScale = new Vector3(colliderScale * 1.01f, colliderScale, colliderScale);
        }
    }

    private float GetDynamicFactor()
    {
        if (cameraPos == null) return 0.005f;

        float distance = Vector3.Distance(cameraPos.transform.position, transform.position);

        float frustumHeight = 2.0f * distance * Mathf.Tan(cameraPos.fieldOfView * 0.5f * Mathf.Deg2Rad);
        return frustumHeight / Screen.height;
    }

    private void RecordUndoCommand(PivotStyle style)
    {
        if (style == PivotStyle.Move)
        {
            var data = new Dictionary<Transform, (Vector3, Vector3)>();
            foreach (var item in _SelectObjects)
                data[item.transform] = (_startPositions[item.transform], item.transform.position);
            UndoManager.Instance.PushExistingCommand(new MoveCommand(data));
        }
        else if (style == PivotStyle.Scale)
        {
            var data = new Dictionary<Transform, (Vector3, Vector3)>();
            foreach (var item in _SelectObjects)
                data[item.transform] = (_startScale[item.transform], item.transform.localScale);
            UndoManager.Instance.PushExistingCommand(new ScaleCommand(data));
        }
    }

    private void ResetEmissions()
    {
        EmissionChange(XMoveAndScaleline, OriginalXColor);
        EmissionChange(XMoveAndScalepin, OriginalXColor);
        EmissionChange(YMoveAndScaleline, OriginalYColor);
        EmissionChange(YMoveAndScalepin, OriginalYColor);
        EmissionChange(ZMoveAndScaleline, OriginalZColor);
        EmissionChange(ZMoveAndScalepin, OriginalZColor);
    }

    public void IsGridCheck()
    {
        IsgridCheck = !IsgridCheck;
    }
}

public class PivotHandle : Pivot
{
    public LineRenderer line, pin;
    public Material material;

    public void initialize()
    {
        line.materials = pin.materials = new Material[1] { material };
    }
}
public class Pivot : MonoBehaviour
{
    public bool x, y, z;
    public float length = 0.5f;
    public bool isDragging;
    public float moveX = 0;
    public float moveY = 0;
    public float moveZ = 0;
}