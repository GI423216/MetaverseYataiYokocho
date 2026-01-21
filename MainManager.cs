using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class MainManager : MonoBehaviour
{

    public static MainManager mainmanager;
    public YataiGroups items;
    public Pivot pivot;
    public Material outlineMaterial;

    public Camera MainCamera;
    public Camera PivotCamera;

    [SerializeField, Header("一番親のカテゴリーアセット")]
    private ItemCategory rootCategory;
    public ItemCategory yataiCategory;
    public ItemCategory foodCategory;

    [SerializeField, Header("屋台オブジェクトの親")]
    public GameObject YataiParent;

    [SerializeField, Header("各項目の設定")]
    public Dropdown CreatePrimitive_T0;
    public Dropdown CreatePrimitive_T1;
    public Dropdown CreatePrimitive_T2;

    [SerializeField, Header("選択中のオブジェクト")]
    public List<YataiItem> SelectObjects;
    [Header("コピーされたオブジェクトデータ")]
    public List<CopyObjectData> CopyObjectsData = new List<CopyObjectData>();

    [SerializeField, Header("生成した全オブジェクト")]
    public List<GameObject> YataiObjects;
    [SerializeField, Header("非選択用レイヤーマスク")]
    private LayerMask SelectableLayers;

    [Header("アップロード設定 (Xserver連携用)")]
    private string fileName = "upload_data.pictureyatai";
    private string filePath;
    private string uploadDirectoryPath; // uploadフォルダのパス用

    [Header("セーブ設定")]
    [SerializeField] private string saveFolderName = "MyProjects";
    [SerializeField] private string defaultFileName = "NewProject";

    [Header("インスペクター")]
    public GameObject Inspector;

    private bool isSelectionChanged = false;

    [Header("UI設定")]
    public Text modeButtonText;

    // 変化前の状態を一時保存する用
    private Dictionary<Transform, Vector3> prePositions = new Dictionary<Transform, Vector3>();
    private Dictionary<Transform, Vector3> preScales = new Dictionary<Transform, Vector3>();
    private Dictionary<Transform, Quaternion> preRotations = new Dictionary<Transform, Quaternion>();

    void Start()
    {
        mainmanager = this;
        rootCategory = yataiCategory;

        CreatePrimitive_T0.onValueChanged.AddListener(_ => UpdateT1());
        CreatePrimitive_T1.onValueChanged.AddListener(_ => UpdateT2());

        InitializeDropdowns();
    }
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            else
            {
                BeginTransformChange();

            }
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            LayerMask pivotLayer = LayerMask.GetMask("Pivot");

            if (Physics.Raycast(ray, out hit, float.MaxValue, pivotLayer))
            {
                return;
            }

            if (Physics.Raycast(ray, out hit, float.MaxValue, SelectableLayers))
            {
                ClickObjects(hit.transform);
            }
            else
            {
                ClickBackGround();
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            EndTransformChange();
        }
        if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace)) { DeleteCtrl(); }
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.S))
        {
            string folderPath = Path.Combine(Application.dataPath, saveFolderName);
            string fullPath = Path.Combine(folderPath, defaultFileName + ".pictureyatai");

            SaveCtrl(fullPath);
        }
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.L))
        {
            string folderPath = Path.Combine(Application.dataPath, saveFolderName);
            string fullPath = Path.Combine(folderPath, defaultFileName + ".pictureyatai");

            LoadCtrl(fullPath);
        }
        if (Input.GetKeyDown(KeyCode.Alpha0)) evenly(new Vector3Flags(true, true, true));
        if (Input.GetKeyDown(KeyCode.Alpha7)) evenly(new Vector3Flags(true, false, false));
        if (Input.GetKeyDown(KeyCode.Alpha8)) evenly(new Vector3Flags(false, true, false));
        if (Input.GetKeyDown(KeyCode.Alpha9)) evenly(new Vector3Flags(false, false, true));

        if (Input.GetKeyDown(KeyCode.Alpha2)) Average(new Vector3Flags(true, false, false));
        if (Input.GetKeyDown(KeyCode.Alpha3)) Average(new Vector3Flags(false, true, false));
        if (Input.GetKeyDown(KeyCode.Alpha4)) Average(new Vector3Flags(false, false, true));

        if ((Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.F)) || (Input.GetKey(KeyCode.RightControl) && Input.GetKeyDown(KeyCode.F)))
        {
            ObjectFocus(SelectObjects);
        }
        //コピー機能
        if ((Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.C)) || (Input.GetKey(KeyCode.RightControl) && Input.GetKeyDown(KeyCode.C)))
        {
            CopyCtrl();
        }
        //ペースト機能
        if ((Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.V)) || (Input.GetKey(KeyCode.RightControl) && Input.GetKeyDown(KeyCode.V)))
        {
            PasteCtrl();
        }
        //巻き戻し機能
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        { }
        if (Input.GetKeyDown(KeyCode.Z)) UndoManager.Instance.Undo();
        if (Input.GetKeyDown(KeyCode.Y)) UndoManager.Instance.Redo();


        InspectorView();

    }
    /// <summary>
    /// 項目に対応するオブジェクトを生成する
    /// </summary>
    public void GenerateCtrl()
    {
        if (rootCategory == null || rootCategory.subCategories == null) return;

        int idx0 = CreatePrimitive_T0.value;
        if (idx0 < 0 || idx0 >= rootCategory.subCategories.Count) return;
        var selectedT0 = rootCategory.subCategories[idx0];

        int idx1 = CreatePrimitive_T1.value;
        if (selectedT0.subCategories == null || idx1 < 0 || idx1 >= selectedT0.subCategories.Count) return;
        var selectedT1 = selectedT0.subCategories[idx1];

        int idx2 = CreatePrimitive_T2.value;
        if (selectedT1.items == null || idx2 < 0 || idx2 >= selectedT1.items.Count)
        {
            Debug.LogWarning("選択されたカテゴリーにアイテムが含まれていないか、インデックスが不正です。");
            return;
        }

        ItemData selectedItem = selectedT1.items[idx2];

        if (selectedItem.prefab == null)
        {
            Debug.LogError($"{selectedItem.itemName} のPrefabがセットされていません！");
            return;
        }

        GameObject ganerateObject = Instantiate(selectedItem.prefab) as GameObject;

        int objectLayer = LayerMask.NameToLayer("Object");
        if (objectLayer != -1)
        {
            ganerateObject.layer = objectLayer;
        }

        ganerateObject.transform.position = Vector3.zero;
        if (YataiParent != null)
        {
            ganerateObject.transform.SetParent(YataiParent.transform);
        }

        YataiObjects.Add(ganerateObject);
        ClickObjects(ganerateObject.transform);

        if (UndoManager.Instance != null)
        {
            List<GameObject> createdList = new List<GameObject> { ganerateObject };
            UndoManager.Instance.PushExistingCommand(new CreateCommand(createdList, YataiObjects));
        }
    }

    /// <summary>
    /// 選択中のオブジェクトのデータを取得(コピー)する
    /// </summary>
    public void CopyCtrl()
    {
        Debug.Log("コピーーーーーーーーーー");
        if (SelectObjects == null || SelectObjects.Count == 0) return;

        CopyObjectsData.Clear();

        foreach (var obj in SelectObjects)
        {
            Transform objTransform = obj.transform;

            CopyObjectsData.Add(new CopyObjectData(obj));
        }
    }
    /// <summary>
    /// コピーされたオブジェクトデータを基に複製を生成する
    /// </summary>
    public void PasteCtrl()
    {
        if (CopyObjectsData == null || CopyObjectsData.Count == 0) return;

        List<YataiItem> newSelectObjects = new List<YataiItem>();
        List<GameObject> createdObjectsUndo = new List<GameObject>();

        foreach (CopyObjectData data in CopyObjectsData)
        {

            GameObject newObject = Instantiate(data.yataiObj, YataiParent.transform);

            Transform newTransform = newObject.transform;
            newTransform.localPosition = data.position;
            newTransform.localRotation = data.rotation;
            newTransform.localScale = data.scale;

            newObject.SetActive(true);

            if (newObject.layer != LayerMask.NameToLayer("Object"))
            {
                newObject.layer = LayerMask.NameToLayer("Object");
            }

            YataiObjects.Add(newObject);
            createdObjectsUndo.Add(newObject);

            YataiItem newItem = newObject.GetComponent<YataiItem>();
            if (newItem != null)
            {
                newSelectObjects.Add(newItem);
            }
        }

        UndoManager.Instance.PushExistingCommand(new CreateCommand(createdObjectsUndo, YataiObjects));

        ClickBackGround();
        foreach (var item in newSelectObjects)
        {
            Add(item);
        }
    }

    public void ClickObjects(Transform objects)
    {
        if (objects.gameObject.layer == LayerMask.NameToLayer("Pivot"))
        {
            return;
        }
        var y = objects.GetComponent<YataiItem>();
        if (y == null)
        {
            do
            {
                objects = objects.parent;
                if (objects == null)
                {
                    ClickBackGround();
                    return;
                }
                else
                {
                    y = objects.GetComponent<YataiItem>();
                    if (y != null)
                    {
                        break;
                    }
                }

            }
            while (true);
        }
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            bool selected = false;
            foreach (var a in YataiObjects)
            {
                if (a != null)
                {
                    if (y == a)
                    {
                        selected = true;
                        break;
                    }
                }
            }
            if (selected)
            {
                ReMove(y);
            }
            else
            {
                Add(y);
            }
        }
        else
        {
            foreach (var a in SelectObjects.ToArray())
            {
                if (a != null)
                {
                    ReMove(a);
                }
            }
            Add(y);
        }
    }

    /// <summary>
    /// 選択されたアイテムの全子オブジェクトにアウトラインを追加する
    /// </summary>
    public void OutlineAdd(YataiItem yataiitem)
    {
        MeshRenderer[] renderers = yataiitem.GetComponentsInChildren<MeshRenderer>();

        foreach (MeshRenderer meshRenderer in renderers)
        {
            List<Material> materialsList = meshRenderer.materials.ToList();

            if (materialsList.Count == 1)
            {
                materialsList.Add(outlineMaterial);
            }
            else if (materialsList.Count >= 2)
            {
                materialsList[1] = outlineMaterial;

                while (materialsList.Count > 2)
                {
                    materialsList.RemoveAt(materialsList.Count - 1);
                }
            }
            meshRenderer.materials = materialsList.ToArray();
        }
    }

    /// <summary>
    /// 選択されたアイテムの全子オブジェクトからアウトラインを削除する
    /// </summary>
    public void OutlineRemove(YataiItem yataiitem)
    {
        MeshRenderer[] renderers = yataiitem.GetComponentsInChildren<MeshRenderer>();

        foreach (MeshRenderer meshRenderer in renderers)
        {
            List<Material> materialsList = meshRenderer.materials.ToList();

            if (materialsList.Count >= 2)
            {
                materialsList.RemoveAt(materialsList.Count - 1);
                meshRenderer.materials = materialsList.ToArray();
            }
        }
    }

    public void ClickBackGround()
    {
        if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
        {
            foreach (var a in SelectObjects.ToArray())
            {
                if (a != null)
                {
                    ReMove(a);
                }
            }
        }
    }
    /// <summary>
    /// 配列に追加しつつアウトラインを表示する
    /// </summary>
    /// <param name="yataiitem"></param>
    void Add(YataiItem yataiitem)
    {
        SelectObjects.Add(yataiitem);
        OutlineAdd(yataiitem);
        isSelectionChanged = true;
    }

    void ReMove(YataiItem yataiitem)
    {
        SelectObjects.Remove(yataiitem);
        OutlineRemove(yataiitem);
        isSelectionChanged = true;
    }
    /// <summary>
    /// 複製したオブジェクト全てを保存する。
    /// </summary>
    public void SaveCtrl(string path)
    {
        try
        {
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                foreach (GameObject obj in YataiObjects)
                {
                    if (obj == null) continue;

                    byte[] bodyData = SerializeItemData(obj);
                    writer.Write((int)1);
                    writer.Write(bodyData.Length);
                    writer.Write(bodyData);
                }
            }
            Debug.Log($"保存完了: {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"保存失敗 ({path}): {e.Message}");
        }
    }
    /// <summary>
    /// バイト配列をロードする処理
    /// </summary> 
    public void LoadCtrl(string path)
    {
        if (!File.Exists(path)) return;

        try
        {
            byte[] allData = File.ReadAllBytes(path);
            if (allData.Length < 8)
            {
                Debug.LogError("ファイルサイズが小さすぎます（ヘッダー不足）。");
                return;
            }

            using (MemoryStream ms = new MemoryStream(allData))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                while (ms.Position <= ms.Length - 8) // 少なくともヘッダー分（8バイト）残っているか
                {
                    int version = reader.ReadInt32();
                    int bodyLength = reader.ReadInt32();

                    if (ms.Position + bodyLength > ms.Length)
                    {
                        Debug.LogError($"データが途切れています。期待される長さ: {bodyLength}, 残り: {ms.Length - ms.Position}");
                        break;
                    }

                    byte[] body = reader.ReadBytes(bodyLength);

                    if (version == 1)
                    {
                        if (items != null) items.LoadYatai(body);
                    }
                }
            }
            Debug.Log($"ロード完了: {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ロード中にエラーが発生しました: {e.Message}\n{e.StackTrace}");
        }
    }
    /// <summary>
    /// 選択中のオブジェクトを削除する
    /// </summary>
    public void DeleteCtrl()
    {
        if (SelectObjects == null || SelectObjects.Count == 0) return;

        List<GameObject> objectsToDelete = new List<GameObject>();
        foreach (var item in SelectObjects)
        {
            objectsToDelete.Add(item.gameObject);
        }

        DeleteCommand cmd = new DeleteCommand(objectsToDelete, YataiObjects);
        cmd.Execute();
        UndoManager.Instance.PushExistingCommand(cmd);

        ClickBackGround();
    }
    /// <summary>
    /// オブジェクトを均等に配置する
    /// </summary>
    void evenly(Vector3Flags flag)
    {
        if (YataiObjects.Count < 3) return;
        BeginTransformChange();

        var list = GetTransforms();
        float maxPoint = float.MinValue;
        float minPoint = float.MaxValue;
        float distance = 0;

        if (flag.x)
        {
            foreach (var i in list)
            {
                if (i.position.x < minPoint)
                {
                    minPoint = i.position.x;
                }
                if (i.position.x > maxPoint)
                {
                    maxPoint = i.position.x;
                }
            }

            distance = Mathf.Abs(maxPoint - minPoint) / (list.Length - 1);
            Transform t;
            for (int a = 0; a < list.Length; a++)
            {
                for (int b = a + 1; b < list.Length; b++)
                {
                    if (list[a].position.x > list[b].position.x)
                    {
                        t = list[a];
                        list[a] = list[b];
                        list[b] = t;
                    }
                }
            }
            for (int a = 0; a < list.Length; a++)
            {
                var pos = list[a].position;
                pos.x = a * distance + minPoint;
                list[a].position = pos;
            }
        }

        maxPoint = float.MinValue;
        minPoint = float.MaxValue;
        distance = 0;
        list = GetTransforms();
        if (flag.y)
        {
            foreach (var i in list)
            {
                if (i.position.y < minPoint)
                {
                    minPoint = i.position.y;
                }
                if (i.position.y > maxPoint)
                {
                    maxPoint = i.position.y;
                }
            }

            distance = Mathf.Abs(maxPoint - minPoint) / (list.Length - 1);

            Transform t;
            for (int a = 0; a < list.Length; a++)
            {
                for (int b = a + 1; b < list.Length; b++)
                {
                    if (list[a].position.y > list[b].position.y)
                    {
                        t = list[a];
                        list[a] = list[b];
                        list[b] = t;
                    }
                }
            }
            for (int a = 0; a < list.Length; a++)
            {
                var pos = list[a].position;
                pos.y = a * distance + minPoint;
                list[a].position = pos;
            }
        }

        maxPoint = float.MinValue;
        minPoint = float.MaxValue;
        distance = 0;
        list = GetTransforms();
        if (flag.z)
        {
            foreach (var i in list)
            {
                if (i.position.z < minPoint)
                {
                    minPoint = i.position.z;
                }
                if (i.position.z > maxPoint)
                {
                    maxPoint = i.position.z;
                }
            }

            distance = Mathf.Abs(maxPoint - minPoint) / (list.Length - 1);

            Transform t;
            for (int a = 0; a < list.Length; a++)
            {
                for (int b = a + 1; b < list.Length; b++)
                {
                    if (list[a].position.z > list[b].position.z)
                    {
                        t = list[a];
                        list[a] = list[b];
                        list[b] = t;
                    }
                }
            }
            for (int a = 0; a < list.Length; a++)
            {
                var pos = list[a].position;
                pos.z = a * distance + minPoint;
                list[a].position = pos;
            }
        }
        EndTransformChange();
    }
    /// <summary>
    /// オブジェクトの位置に移動する
    /// </summary>
    void ObjectFocus(List<YataiItem> yataiObject)
    {
        if (yataiObject.Count == 0) { return; }

        Vector3 CaeraDefaultPosition = new Vector3(0f, 1.65f, 2.0f);
        Vector3 FocusObject = Vector3.zero;
        foreach (var obj in yataiObject) { FocusObject += obj.transform.position; }
        FocusObject = FocusObject / yataiObject.Count;

        MainCamera.transform.position = FocusObject + CaeraDefaultPosition;
        PivotCamera.transform.position = FocusObject + CaeraDefaultPosition;
        MainCamera.transform.rotation = PivotCamera.transform.rotation = Quaternion.Euler(36f, 180f, 0);
    }

    void Average(Vector3Flags flag)
    {
        if (YataiObjects.Count < 3) return;
        BeginTransformChange();

        var list = GetTransforms();

        float add = 0;
        if (flag.x)
        {
            add = 0;
            foreach (var i in list)
            {
                add += i.position.x;
            }
            foreach (var i in list)
            {
                var a = i.position;
                a.x = add / list.Length;
                i.position = a;
            }
        }

        if (flag.y)
        {
            add = 0;
            foreach (var i in list)
            {
                add += i.position.y;
            }
            foreach (var i in list)
            {
                var a = i.position;
                a.y = add / list.Length;
                i.position = a;
            }
        }
        if (flag.z)
        {
            add = 0;
            foreach (var i in list)
            {
                add += i.position.z;
            }
            foreach (var i in list)
            {
                var a = i.position;
                a.z = add / list.Length;
                i.position = a;
            }
        }
        EndTransformChange();
    }
    /// <summary>
    /// Xserverへアップロードするための外部アプリ起動処理 (必須機能)
    /// </summary>
    public void GotoWebPage()
    {
        PrepareUploadDirectory();
        SaveCtrl(filePath);

        Application.OpenURL("http://picture-kit.com/nakasu/upload.html");

        string folderPath = uploadDirectoryPath.Replace("/", "\\");
        System.Diagnostics.Process.Start("explorer.exe", folderPath);
    }

    private void PrepareUploadDirectory()
    {
        uploadDirectoryPath = Path.Combine(Application.persistentDataPath, "upload");

        if (!Directory.Exists(uploadDirectoryPath))
        {
            Directory.CreateDirectory(uploadDirectoryPath);
        }


        filePath = Path.Combine(uploadDirectoryPath, fileName);
    }

    private byte[] SerializeItemData(GameObject obj)
    {
        var yatai = obj.GetComponent<YataiItem>();
        if (yatai == null) return new byte[0];

        byte[] data = new byte[256];
        System.Array.Clear(data, 0, data.Length);

        Vector3 normalizedPos = YataiParent.transform.InverseTransformPoint(obj.transform.position);

        Quaternion normalizedRot = Quaternion.Inverse(YataiParent.transform.rotation) * obj.transform.rotation;
        Vector3 rotEuler = normalizedRot.eulerAngles;

        uint id = 0;
        string cleanName = obj.name.Replace("(Clone)", "").Trim();
        if (uint.TryParse(cleanName, out uint parsedId)) id = parsedId;
        data[6] = (byte)((id >> 8) & 0xFF);
        data[7] = (byte)(id & 0xFF);

        WriteFloatToBytes(data, 8, normalizedPos.x);
        WriteFloatToBytes(data, 12, normalizedPos.y);
        WriteFloatToBytes(data, 16, normalizedPos.z);

        WriteFloatToBytes(data, 20, rotEuler.x);
        WriteFloatToBytes(data, 24, rotEuler.y);
        WriteFloatToBytes(data, 28, rotEuler.z);

        WriteFloatToBytes(data, 32, obj.transform.localScale.x);
        WriteFloatToBytes(data, 36, obj.transform.localScale.y);
        WriteFloatToBytes(data, 40, obj.transform.localScale.z);

        WriteStringToBytes(data, 44, yatai.guid, 32);
        WriteStringToBytes(data, 108, yatai.parentGuid, 32);

        int totalLen = 200;
        data[4] = (byte)((totalLen >> 8) & 0xFF);
        data[5] = (byte)(totalLen & 0xFF);

        byte[] result = new byte[totalLen];
        System.Array.Copy(data, result, totalLen);
        return result;
    }

    private void WriteFloatToBytes(byte[] data, int startIdx, float value)
    {
        byte[] bytes = System.BitConverter.GetBytes(value);

        System.Array.Copy(bytes, 0, data, startIdx, 4);
    }

    private void WriteStringToBytes(byte[] data, int startIdx, string s, int maxChars)
    {
        if (string.IsNullOrEmpty(s)) return;
        for (int i = 0; i < s.Length && i < maxChars; i++)
        {
            data[startIdx + (i * 2)] = (byte)((s[i] >> 8) & 0xFF);
            data[startIdx + (i * 2) + 1] = (byte)(s[i] & 0xFF);
        }
    }
    public void MoveChangeButtonCtrl()
    {
        GizmoModeManager.CurrentMode = PivotStyle.Move;
    }
    public void ScaleChangeButtonCtrl()
    {
        GizmoModeManager.CurrentMode = PivotStyle.Scale;
    }
    public void RotateChangeButtonCtrl()
    {
        GizmoModeManager.CurrentMode = PivotStyle.Rotate;
    }

    Transform[] GetTransforms()
    {
        Transform[] t = new Transform[SelectObjects.Count];
        for (int e = 0; e < t.Length; e++)
        {
            t[e] = SelectObjects[e].transform;
        }
        return t;
    }

    void InspectorView()
    {
        if (isSelectionChanged)
        {
            InspectorGraphicer.inspector_.SearchInspectorTypeClass(
                SelectObjects.Select(x => x.transform).ToArray()
            );

            AttachUndoToGeneratedFields();

            isSelectionChanged = false;
        }

        if (isSelectionChanged)
        {
            InspectorGraphicer.inspector_.SearchInspectorTypeClass(
                SelectObjects.Select(x => x.transform).ToArray()
            );
            isSelectionChanged = false;
        }
        if (SelectObjects.Count == 0)
        {
            if (Inspector.gameObject.activeSelf) Inspector.gameObject.SetActive(false);
        }
        else
        {
            if (!Inspector.gameObject.activeSelf) Inspector.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 自動生成されたすべての入力欄にUndo用のイベントを登録する
    /// </summary>
    private void AttachUndoToGeneratedFields()
    {
        InputField[] allInputs = Inspector.GetComponentsInChildren<InputField>(true);

        foreach (var input in allInputs)
        {
            input.onEndEdit.RemoveAllListeners();

            input.onEndEdit.AddListener(_ =>
            {
                EndTransformChange();
            });

            EventTrigger trigger = input.gameObject.GetComponent<EventTrigger>();
            if (trigger == null) trigger = input.gameObject.AddComponent<EventTrigger>();

            trigger.triggers.Clear();

            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.Select;
            entry.callback.AddListener((data) =>
            {
                BeginTransformChange();
            });
            trigger.triggers.Add(entry);
        }
    }

    public void InitializeDropdowns()
    {
        if (rootCategory == null) return;

        CreatePrimitive_T0.options.Clear();
        foreach (var cat in rootCategory.subCategories)
        {
            CreatePrimitive_T0.options.Add(new Dropdown.OptionData { text = cat.categoryName });
        }

        CreatePrimitive_T0.value = 0;
        CreatePrimitive_T0.RefreshShownValue();

        UpdateT1();
    }
    public void UpdateT1()
    {
        if (rootCategory == null || rootCategory.subCategories.Count == 0) return;

        int idx0 = CreatePrimitive_T0.value;
        var selectedT0 = rootCategory.subCategories[idx0];

        CreatePrimitive_T1.options.Clear();
        if (selectedT0.subCategories != null)
        {
            foreach (var cat in selectedT0.subCategories)
            {
                CreatePrimitive_T1.options.Add(new Dropdown.OptionData { text = cat.categoryName });
            }
        }

        CreatePrimitive_T1.value = 0;
        CreatePrimitive_T1.RefreshShownValue();

        UpdateT2();
    }

    public void UpdateT2()
    {
        if (rootCategory == null) return;

        int idx0 = CreatePrimitive_T0.value;
        int idx1 = CreatePrimitive_T1.value;

        var selectedT0 = rootCategory.subCategories[idx0];

        CreatePrimitive_T2.options.Clear();
        if (selectedT0.subCategories != null && idx1 < selectedT0.subCategories.Count)
        {
            var selectedT1 = selectedT0.subCategories[idx1];
            if (selectedT1.items != null)
            {
                foreach (var item in selectedT1.items)
                {
                    CreatePrimitive_T2.options.Add(new Dropdown.OptionData { text = item.itemName });
                }
            }
        }

        CreatePrimitive_T2.value = 0;
        CreatePrimitive_T2.RefreshShownValue();
    }
    public void ToggleMode()
    {
        bool nextIsCooking = (rootCategory == yataiCategory);

        SetMode(nextIsCooking);
    }

    public void SetMode(bool isCooking)
    {
        rootCategory = isCooking ? foodCategory : yataiCategory;

        CreatePrimitive_T0.value = 0;
        InitializeDropdowns();

        if (modeButtonText != null)
        {
            modeButtonText.text = isCooking ? "建築を選ぶ!!" : "料理を選ぶ!!";
        }
    }
    /// <summary>
    /// 操作開始時に呼ぶ（マウスダウンや、入力欄のクリック時など）
    /// </summary>
    public void BeginTransformChange()
    {
        prePositions.Clear();
        preScales.Clear();
        preRotations.Clear();

        foreach (var item in SelectObjects)
        {
            if (item == null) continue;
            prePositions[item.transform] = item.transform.localPosition;
            preScales[item.transform] = item.transform.localScale;
            preRotations[item.transform] = item.transform.localRotation;
        }
    }

    /// <summary>
    /// 操作終了時に呼ぶ（マウスアップや、入力確定時など）
    /// </summary>
    public void EndTransformChange()
    {
        if (SelectObjects.Count == 0) return;

        var moveRecords = new Dictionary<Transform, (Vector3, Vector3)>();
        var scaleRecords = new Dictionary<Transform, (Vector3, Vector3)>();
        var rotateRecords = new Dictionary<Transform, (Quaternion, Quaternion)>();

        foreach (var item in SelectObjects)
        {
            Transform t = item.transform;

            if (prePositions.ContainsKey(t) && prePositions[t] != t.localPosition)
                moveRecords[t] = (prePositions[t], t.localPosition);

            if (preScales.ContainsKey(t) && preScales[t] != t.localScale)
                scaleRecords[t] = (preScales[t], t.localScale);

            if (preRotations.ContainsKey(t) && preRotations[t] != t.localRotation)
                rotateRecords[t] = (preRotations[t], t.localRotation);
        }

        if (moveRecords.Count > 0) UndoManager.Instance.PushExistingCommand(new MoveCommand(moveRecords));
        if (scaleRecords.Count > 0) UndoManager.Instance.PushExistingCommand(new ScaleCommand(scaleRecords));
        if (rotateRecords.Count > 0) UndoManager.Instance.PushExistingCommand(new RotateCommand(rotateRecords));
    }
}

[System.Serializable]
public class CopyObjectData
{
    public GameObject yataiObj;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
    public uint ObjitemId;

    public CopyObjectData(YataiItem original)
    {
        this.yataiObj = original.gameObject;

        Transform t = original.transform;
        this.position = t.localPosition;
        this.rotation = t.localRotation;
        this.scale = t.localScale;
    }
}

public class Vector3Flags
{
    public bool x, y, z;
    public Vector3Flags() { }
    public Vector3Flags(bool x, bool y, bool z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
}

public static class GizmoModeManager
{
    public static PivotStyle currentMode = PivotStyle.Move;
    public static PivotStyle CurrentMode
    {
        get { return currentMode; }
        set
        {
            if (currentMode != value)
            {
                currentMode = value;
            }
        }
    }
    public static void HandleModeSwitchInput()
    {
        if (Input.GetKeyDown(KeyCode.W)) // Wキーで移動モード
        {
            CurrentMode = PivotStyle.Move;
        }
        else if (Input.GetKeyDown(KeyCode.E)) // Eキーで回転モード
        {
            CurrentMode = PivotStyle.Rotate;
        }
        else if (Input.GetKeyDown(KeyCode.R)) // Rキーでスケールモード
        {
            CurrentMode = PivotStyle.Scale;
        }
    }
}
public enum PivotStyle
{
    Move,
    Rotate,
    Scale
}

[System.Serializable]
public class YataiItemSaveData
{
    public string prefabName;
    public Vector3 position;
    public Vector3 rotation;
    public Vector3 scale;
}

[System.Serializable]
public class YataiUploadPacket
{
    public string uploadTime;
    public List<YataiItemSaveData> items = new List<YataiItemSaveData>();
}