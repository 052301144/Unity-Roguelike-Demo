using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 显示当前装备的武器和装备图标（从 PlayerItemManager 读取）。
/// </summary>
public class EquippedDisplayUI : MonoBehaviour
{
    [SerializeField] private PlayerItemManager playerItemManager;
    [SerializeField] private Image weaponImage;
    [SerializeField] private Image equipmentImage;
    [SerializeField] private EquipmentSlot equipmentSlot = EquipmentSlot.Armor; // 选择显示哪个装备槽

    private void OnEnable()
    {
        if (playerItemManager == null)
            playerItemManager = FindObjectOfType<PlayerItemManager>();

        if (playerItemManager != null)
        {
            playerItemManager.OnWeaponEquipped += OnWeaponChanged;
            playerItemManager.OnEquipmentEquipped += OnEquipmentChanged;
            playerItemManager.OnInventoryChanged += _ => RefreshAll();
        }

        RefreshAll();
    }

    private void OnDisable()
    {
        if (playerItemManager != null)
        {
            playerItemManager.OnWeaponEquipped -= OnWeaponChanged;
            playerItemManager.OnEquipmentEquipped -= OnEquipmentChanged;
            playerItemManager.OnInventoryChanged -= _ => RefreshAll();
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
}
