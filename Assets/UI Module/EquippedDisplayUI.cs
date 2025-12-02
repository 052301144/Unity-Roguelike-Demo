using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 显示当前装备的武器和装备图标（从 PlayerItemManager 读取）。
/// </summary>
public class EquippedDisplayUI : MonoBehaviour
{
    [SerializeField] private PlayerItemManager playerItemManager;
    [SerializeField] private Image weaponImage;
    [SerializeField] private Image equipmentImage;
    [SerializeField] private EquipmentSlot equipmentSlot = EquipmentSlot.Armor; // 选择显示哪个装备槽
    [SerializeField] private bool autoFallbackEquipment = true; // 如果指定槽为空，自动找其他槽位的装备

    // 缓存委托，避免 OnDisable 时无法正确移除
    private System.Action<IReadOnlyList<ItemData>> inventoryChangedHandler;

    private void OnEnable()
    {
        if (playerItemManager == null)
            playerItemManager = FindObjectOfType<PlayerItemManager>();

        AutoFindImagesIfMissing();

        // 绑定事件
        inventoryChangedHandler = _ => RefreshAll();
        if (playerItemManager != null)
        {
            playerItemManager.OnWeaponEquipped += OnWeaponChanged;
            playerItemManager.OnEquipmentEquipped += OnEquipmentChanged;
            playerItemManager.OnInventoryChanged += inventoryChangedHandler;
        }

        RefreshAll();
    }

    private void OnDisable()
    {
        if (playerItemManager != null)
        {
            playerItemManager.OnWeaponEquipped -= OnWeaponChanged;
            playerItemManager.OnEquipmentEquipped -= OnEquipmentChanged;
            if (inventoryChangedHandler != null)
                playerItemManager.OnInventoryChanged -= inventoryChangedHandler;
        }
    }

    private void OnWeaponChanged(ItemData item) => RefreshWeapon();
    private void OnEquipmentChanged(ItemData item) => RefreshEquipment();

    private void RefreshAll()
    {
        RefreshWeapon();
        RefreshEquipment();
    }

    private void RefreshWeapon()
    {
        if (weaponImage == null) return;
        var current = playerItemManager != null ? playerItemManager.CurrentWeapon : null;
        SetImage(weaponImage, current);
    }

    private void RefreshEquipment()
    {
        if (equipmentImage == null || playerItemManager == null) return;
        var equip = playerItemManager.GetEquippedItem(equipmentSlot);

        // 如果指定槽为空且允许兜底，尝试找其他槽位
        if (equip == null && autoFallbackEquipment)
        {
            foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
            {
                if (slot == equipmentSlot) continue;
                equip = playerItemManager.GetEquippedItem(slot);
                if (equip != null) break;
            }
        }

        SetImage(equipmentImage, equip);
    }

    private void SetImage(Image img, ItemData item)
    {
        if (item != null)
        {
            var sprite = item.itemAsset != null ? item.itemAsset.Icon : item.itemIcon;
            img.sprite = sprite;
            img.enabled = sprite != null;
        }
        else
        {
            img.sprite = null;
            img.enabled = false;
        }
    }

    private void AutoFindImagesIfMissing()
    {
        if (weaponImage == null)
        {
            var t = transform.Find("weapon") ?? transform.Find("Weapon");
            if (t == null && transform.root != null)
                t = transform.root.Find("Canvas/RoleMenu/Left/weapon") ?? transform.root.Find("Canvas/RoleMenu/Left/Weapon");
            if (t != null) weaponImage = t.GetComponent<Image>();
        }
        if (equipmentImage == null)
        {
            var t = transform.Find("equipment") ?? transform.Find("Equipment");
            if (t == null && transform.root != null)
                t = transform.root.Find("Canvas/RoleMenu/Left/equipment") ?? transform.root.Find("Canvas/RoleMenu/Left/Equipment");
            if (t != null) equipmentImage = t.GetComponent<Image>();
        }
    }
}
