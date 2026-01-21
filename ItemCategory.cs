using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCategory", menuName = "Yatai/Category")]
public class ItemCategory : ScriptableObject
{
    public string categoryName;
    public ItemCategory parentCategory;

    public List<ItemCategory> subCategories;
    public List<ItemData> items;
}