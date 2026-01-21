using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class YataiDataBase : MonoBehaviour
{
    [Header("データ元（Yatai_Rootを指定）")]
    [SerializeField] private ItemCategory rootCategory;

    [Header("UIパーツ（Dropdown）")]
    [SerializeField] private Dropdown T0; // 建材 など
    [SerializeField] private Dropdown T1; // 角材 など
    [SerializeField] private Dropdown itemname; // アイテム名

    void Start()
    {
        T0.ClearOptions();
        var t0Options = rootCategory.subCategories.Select(c => c.categoryName).ToList();
        T0.AddOptions(t0Options);

        // リスナー登録
        T0.onValueChanged.AddListener(_ => UpdateT1Dropdown());
        T1.onValueChanged.AddListener(_ => UpdateItemDropdown());

        UpdateT1Dropdown();
    }

    void UpdateT1Dropdown()
    {
        T1.ClearOptions();

        var selectedT0 = rootCategory.subCategories[T0.value];

        if (selectedT0.subCategories != null && selectedT0.subCategories.Count > 0)
        {
            var options = selectedT0.subCategories.Select(c => c.categoryName).ToList();
            T1.AddOptions(options);
        }

        T1.value = 0;
        T1.RefreshShownValue();

        UpdateItemDropdown();
    }

    void UpdateItemDropdown()
    {
        itemname.ClearOptions();

        var selectedT0 = rootCategory.subCategories[T0.value];

        // T1が空の場合の安全処理
        if (selectedT0.subCategories == null || selectedT0.subCategories.Count == 0)
        {
            itemname.AddOptions(new List<string> { "カテゴリーなし" });
            itemname.RefreshShownValue();
            return;
        }

        var selectedT1 = selectedT0.subCategories[T1.value];

        if (selectedT1.items == null || selectedT1.items.Count == 0)
        {
            itemname.AddOptions(new List<string> { "アイテムなし" });
        }
        else
        {
            var options = selectedT1.items.Select(i => i.itemName).ToList();
            itemname.AddOptions(options);
        }

        itemname.value = 0;
        itemname.RefreshShownValue();
    }

    public void SpawnItem()
    {
        var selectedT0 = rootCategory.subCategories[T0.value];
        if (selectedT0.subCategories == null || T1.value >= selectedT0.subCategories.Count) return;

        var selectedT1 = selectedT0.subCategories[T1.value];
        if (selectedT1.items == null || itemname.value >= selectedT1.items.Count) return;

        ItemData target = selectedT1.items[itemname.value];
        if (target != null && target.prefab != null)
        {
            Instantiate(target.prefab, Vector3.zero, Quaternion.identity);
        }
    }
}