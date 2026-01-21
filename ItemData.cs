using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Yatai/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("基本情報")]
    public uint itemId;        //アイテムID
    public string itemName;    //アイテム名
    [TextArea] public string description;

    [Header("プレハブを選択")]
    public GameObject prefab;

    [Header("中項目を選択")]
    public ItemCategory parentCategory;
}