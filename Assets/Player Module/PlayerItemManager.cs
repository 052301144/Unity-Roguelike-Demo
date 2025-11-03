using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

/// <summary>
/// 玩家物品管理器 - 管理玩家的装备、武器、消耗品等物品
/// 负责接收物品拾取事件并管理玩家的物品库存
/// </summary>
public class PlayerItemManager : MonoBehaviour
{
    [Header("物品管理设置")]
    [SerializeField] private int maxInventorySlots = 30; // 最大背包格子数
    
    [Header("调试设置")]
    [SerializeField] private bool logItemEvents = true; // 是否在控制台输出物品事件

    // 物品存储
    private List<ItemData> inventory = new List<ItemData>(); // 背包物品列表
    private ItemData currentWeapon; // 当前装备的武器
    private Dictionary<EquipmentSlot, ItemData> equippedItems = new Dictionary<EquipmentSlot, ItemData>(); // 已装备的物品

    // 公共属性
    public int InventoryCount => inventory.Count;
    public int MaxInventorySlots => maxInventorySlots;
    public bool IsInventoryFull => inventory.Count >= maxInventorySlots;
    public ItemData CurrentWeapon => currentWeapon;
    
    // 事件系统
    public System.Action<ItemData> OnItemAdded;           // 物品添加事件
    public System.Action<ItemData> OnItemRemoved;        // 物品移除事件
    public System.Action<ItemData> OnWeaponEquipped;      // 武器装备事件
    public System.Action<ItemData> OnEquipmentEquipped;  // 装备穿戴事件
    public System.Action<ItemData> OnItemUsed;           // 物品使用事件

    void Awake()
    {
        // 初始化装备槽位
        InitializeEquipmentSlots();
    }

    void Start()
    {
        // 订阅掉落系统的事件
        if (DropManager.Instance != null)
        {
            DropManager.Instance.OnWeaponPickedUp += HandleWeaponPickedUp;
            DropManager.Instance.OnEquipmentPickedUp += HandleEquipmentPickedUp;
            DropManager.Instance.OnConsumablePickedUp += HandleConsumablePickedUp;
            
            if (logItemEvents)
            {
                Debug.Log("[PlayerItemManager] 已订阅掉落系统的物品拾取事件");
            }
        }
        else
        {
            Debug.LogWarning("[PlayerItemManager] DropManager未找到，无法订阅物品拾取事件");
        }
    }

    void OnDestroy()
    {
        // 取消订阅事件
        if (DropManager.Instance != null)
        {
            DropManager.Instance.OnWeaponPickedUp -= HandleWeaponPickedUp;
            DropManager.Instance.OnEquipmentPickedUp -= HandleEquipmentPickedUp;
            DropManager.Instance.OnConsumablePickedUp -= HandleConsumablePickedUp;
        }
    }

    /// <summary>
    /// 处理武器拾取
    /// </summary>
    private void HandleWeaponPickedUp(ItemData weapon)
    {
        if (AddItem(weapon))
        {
            // 可以选择自动装备新武器，或提示玩家
            // EquipWeapon(weapon); // 自动装备
            Debug.Log($"[PlayerItemManager] 武器已添加到背包: {weapon.itemName}");
        }
    }

    /// <summary>
    /// 处理装备拾取
    /// </summary>
    private void HandleEquipmentPickedUp(ItemData equipment)
    {
        if (AddItem(equipment))
        {
            Debug.Log($"[PlayerItemManager] 装备已添加到背包: {equipment.itemName}");
        }
    }

    /// <summary>
    /// 处理消耗品拾取
    /// </summary>
    private void HandleConsumablePickedUp(ItemData consumable)
    {
        // 消耗品可以选择立即使用或添加到背包
        // 这里默认添加到背包，玩家可以选择何时使用
        if (AddItem(consumable))
        {
            Debug.Log($"[PlayerItemManager] 消耗品已添加到背包: {consumable.itemName}");
        }
    }

    /// <summary>
    /// 初始化装备槽位
    /// </summary>
    private void InitializeEquipmentSlots()
    {
        equippedItems.Clear();
        equippedItems[EquipmentSlot.Weapon] = null;
        equippedItems[EquipmentSlot.Armor] = null;
        equippedItems[EquipmentSlot.Accessory] = null;
    }

    /// <summary>
    /// 添加物品到背包
    /// </summary>
    /// <param name="item">要添加的物品数据</param>
    /// <returns>是否成功添加</returns>
    public bool AddItem(ItemData item)
    {
        if (item == null)
        {
            if (logItemEvents)
            {
                Debug.LogWarning("[PlayerItemManager] 尝试添加null物品");
            }
            return false;
        }

        if (IsInventoryFull)
        {
            if (logItemEvents)
            {
                Debug.LogWarning($"[PlayerItemManager] 背包已满，无法添加物品: {item.itemName}");
            }
            return false;
        }

        inventory.Add(item);
        OnItemAdded?.Invoke(item);
        
        if (logItemEvents)
        {
            Debug.Log($"[PlayerItemManager] 添加物品: {item.itemName}，当前背包: {inventory.Count}/{maxInventorySlots}");
        }
        
        return true;
    }

    /// <summary>
    /// 移除物品
    /// </summary>
    /// <param name="item">要移除的物品</param>
    /// <returns>是否成功移除</returns>
    public bool RemoveItem(ItemData item)
    {
        if (item == null || !inventory.Contains(item))
        {
            return false;
        }

        inventory.Remove(item);
        OnItemRemoved?.Invoke(item);
        
        if (logItemEvents)
        {
            Debug.Log($"[PlayerItemManager] 移除物品: {item.itemName}");
        }
        
        return true;
    }

    /// <summary>
    /// 装备武器
    /// </summary>
    /// <param name="weapon">要装备的武器</param>
    /// <returns>是否成功装备</returns>
    public bool EquipWeapon(ItemData weapon)
    {
        if (weapon == null || weapon.itemType != DropItemType.Weapon)
        {
            if (logItemEvents)
            {
                Debug.LogWarning("[PlayerItemManager] 尝试装备非武器物品或null物品");
            }
            return false;
        }

        // 如果已有武器，先卸下
        if (currentWeapon != null)
        {
            AddItem(currentWeapon); // 将旧武器放回背包
        }

        currentWeapon = weapon;
        
        // 如果武器在背包中，从背包移除
        if (inventory.Contains(weapon))
        {
            inventory.Remove(weapon);
        }

        OnWeaponEquipped?.Invoke(weapon);
        
        if (logItemEvents)
        {
            Debug.Log($"[PlayerItemManager] 装备武器: {weapon.itemName}");
        }
        
        return true;
    }

    /// <summary>
    /// 装备防具或饰品
    /// </summary>
    /// <param name="equipment">要装备的物品</param>
    /// <param name="slot">装备槽位</param>
    /// <returns>是否成功装备</returns>
    public bool EquipItem(ItemData equipment, EquipmentSlot slot)
    {
        if (equipment == null || equipment.itemType != DropItemType.Equipment)
        {
            if (logItemEvents)
            {
                Debug.LogWarning("[PlayerItemManager] 尝试装备非装备物品或null物品");
            }
            return false;
        }

        // 如果该槽位已有装备，先卸下
        if (equippedItems.ContainsKey(slot) && equippedItems[slot] != null)
        {
            AddItem(equippedItems[slot]); // 将旧装备放回背包
        }

        equippedItems[slot] = equipment;
        
        // 如果装备在背包中，从背包移除
        if (inventory.Contains(equipment))
        {
            inventory.Remove(equipment);
        }

        OnEquipmentEquipped?.Invoke(equipment);
        
        if (logItemEvents)
        {
            Debug.Log($"[PlayerItemManager] 装备物品: {equipment.itemName} 到 {slot}");
        }
        
        return true;
    }

    /// <summary>
    /// 使用消耗品
    /// </summary>
    /// <param name="consumable">要使用的消耗品</param>
    /// <returns>是否成功使用</returns>
    public bool UseConsumable(ItemData consumable)
    {
        if (consumable == null)
        {
            return false;
        }

        // 检查是否是消耗品类型
        if (consumable.itemType != DropItemType.Health && 
            consumable.itemType != DropItemType.Mana && 
            consumable.itemType != DropItemType.Consumable)
        {
            if (logItemEvents)
            {
                Debug.LogWarning($"[PlayerItemManager] 尝试使用非消耗品: {consumable.itemName}");
            }
            return false;
        }

        // 应用消耗品效果（这里应该调用具体的物品效果逻辑）
        ApplyConsumableEffect(consumable);
        
        // 从背包移除（消耗品使用后消失）
        if (inventory.Contains(consumable))
        {
            inventory.Remove(consumable);
        }

        OnItemUsed?.Invoke(consumable);
        
        if (logItemEvents)
        {
            Debug.Log($"[PlayerItemManager] 使用消耗品: {consumable.itemName}");
        }
        
        return true;
    }

    /// <summary>
    /// 应用消耗品效果
    /// </summary>
    private void ApplyConsumableEffect(ItemData consumable)
    {
        // 这里应该根据消耗品类型应用不同的效果
        // 例如恢复生命值、魔法值等
        // 可以通过事件系统通知其他系统（如 Attribute 组件）
    }

    /// <summary>
    /// 获取指定槽位的装备
    /// </summary>
    public ItemData GetEquippedItem(EquipmentSlot slot)
    {
        if (equippedItems.ContainsKey(slot))
        {
            return equippedItems[slot];
        }
        return null;
    }

    /// <summary>
    /// 检查背包中是否有指定物品
    /// </summary>
    public bool HasItem(ItemData item)
    {
        return inventory.Contains(item);
    }

    /// <summary>
    /// 获取所有背包物品（只读）
    /// </summary>
    public IReadOnlyList<ItemData> GetInventoryItems()
    {
        return new ReadOnlyCollection<ItemData>(inventory);
    }
}

/// <summary>
/// 物品数据类 - 用于在库存中存储物品信息
/// </summary>
[System.Serializable]
public class ItemData
{
    public string itemName;           // 物品名称
    public DropItemType itemType;     // 物品类型
    public Sprite itemIcon;            // 物品图标
    public string description;         // 物品描述
    
    // 物品属性（根据类型不同，这些字段的含义也不同）
    public int value;                 // 物品数值（武器：攻击力，防具：防御力等）
    public Dictionary<string, float> customProperties; // 自定义属性
    
    public ItemData(DropItem dropItem)
    {
        if (dropItem != null)
        {
            itemName = dropItem.itemName;
            itemType = dropItem.itemType;
            itemIcon = dropItem.itemIcon;
            description = "";
            value = 0;
            customProperties = new Dictionary<string, float>();
        }
    }
}

/// <summary>
/// 装备槽位枚举
/// </summary>
public enum EquipmentSlot
{
    Weapon,      // 武器槽
    Armor,       // 防具槽
    Accessory    // 饰品槽
}
