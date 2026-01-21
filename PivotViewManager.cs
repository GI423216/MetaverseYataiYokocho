using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PivotViewManager : MonoBehaviour
{
    private MainManager mainmanager;
    [SerializeField, Header("表示するピボットオブジェクト")]
    public GameObject PivotObject;
    [SerializeField, Header("選択中のオブジェクト")]
    public List<YataiItem> _SelectObjects;

    [SerializeField]
    public Camera cameraPos;

    public bool isview = false;
    void Start()
    {
        mainmanager = FindObjectOfType<MainManager>();
        if (cameraPos == null)
        {
            cameraPos = Camera.main;
        }
    }
    void Update()
    {
        _SelectObjects = mainmanager.SelectObjects;

        if ((_SelectObjects == null && isview == true) || (_SelectObjects.Count == 0 && isview == true))
        {
            PivotObject.SetActive(false);
            return;
        }
        else
        {
            isview = true;
        }

        if (_SelectObjects != null && _SelectObjects.Count > 0 && isview == true)
        {
            PivotObject.SetActive(true);
        }
    }
}
