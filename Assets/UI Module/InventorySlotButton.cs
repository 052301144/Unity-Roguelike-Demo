using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 单个格子按钮：展示物品图标/名称/数量，点击事件可自定义（默认空）。
/// </summary>
public class InventorySlotButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text countText;
    [Header("Equip Settings")]
    [SerializeField] private bool enableEquipOnClick = true;
    [SerializeField] private EquipmentSlot equipSlotForArmor = EquipmentSlot.Armor; // 装备默认使用的槽位

    private ItemData item;
    private PlayerItemManager pim;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(OnClick);
    }

    public void Bind(ItemData itemData, PlayerItemManager manager)
    {
        item = itemData;
        pim = manager;

        var displayName = item.itemAsset != null ? item.itemAsset.ItemName : item.itemName;
        var displayIcon = item.itemAsset != null ? item.itemAsset.Icon : item.itemIcon;

        if (nameText != null) nameText.text = displayName;
        if (icon != null)
        {
            icon.sprite = displayIcon;
            icon.enabled = displayIcon != null;
        }
        if (countText != null) countText.text = item.stackCount > 1 ? $"x{item.stackCount}" : string.Empty;

        gameObject.name = $"Slot_{displayName}";
    }

    private void OnClick()
    {
        if (!enableEquipOnClick || pim == null || item == null) return;

        // 武器：装备并必要时从堆中扣 1
        if (item.itemAsset is WeaponItem)
        {
            HandleEquipWeapon();
        }
        // 装备：按照指定槽位装备，必要时从堆中扣 1
        else if (item.itemAsset is EquipmentItem)
        {
            HandleEquipEquipment();
        }
        // 其他类型：保留为 TODO（可扩展消耗/丢弃）
    }

    private void HandleEquipWeapon()
    {
        if (item.stackCount > 1)
        {
            // 先扣 1，再用新的实例装备，避免移除整堆
            pim.RemoveItem(item, 1);
            pim.EquipWeapon(new ItemData(item.itemAsset));
        }
        else
        {
            pim.EquipWeapon(item);
        }
    }

    private void HandleEquipEquipment()
    {
        if (item.stackCount > 1)
        {
            pim.RemoveItem(item, 1);
            pim.EquipItem(new ItemData(item.itemAsset), equipSlotForArmor);
        }
        else
        {
            pim.EquipItem(item, equipSlotForArmor);
        }
    }
}
