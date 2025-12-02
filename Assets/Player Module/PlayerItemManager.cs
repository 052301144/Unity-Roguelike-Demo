using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

/// <summary>
/// Player item manager: handles inventory, equip/unequip, stacking, and item events.
/// </summary>
public class PlayerItemManager : MonoBehaviour
{
    [Header("Inventory Settings")]
    [SerializeField] private int maxInventorySlots = 30;
    
    [Header("Debug")]
    [SerializeField] private bool logItemEvents = true;
    [Header("Defaults")]
    [SerializeField] private WeaponItem defaultWeapon; // 开局自动装备的默认武器
    [SerializeField] private EquipmentItem defaultEquipment; // 开局自动装备的默认装备（Armor 槽）
    [Header("Item Lookup")]
    [Tooltip("可选：在此填入所有可用 ItemBase 资产，便于存档/读档按 itemId 解析。")]
    [SerializeField] private List<ItemBase> itemDatabase = new List<ItemBase>();
    [Header("Debug/Starter Items")]
    [SerializeField] private bool addStarterItemsOnStart = false;
    [SerializeField] private List<StarterItem> starterItems = new List<StarterItem>();

    // Storage
    private List<ItemData> inventory = new List<ItemData>(); // 按堆栈存储
    private ItemData currentWeapon;
    private Dictionary<EquipmentSlot, ItemData> equippedItems = new Dictionary<EquipmentSlot, ItemData>();
    private Attribute playerAttribute;
    private int baseMaxHealth;
    private int baseDefense;
    private Dictionary<string, ItemBase> itemLookup = new Dictionary<string, ItemBase>();

    // Accessors
    public int InventoryCount => inventory.Count; // 当前占用的格子数（堆栈数量）
    public int MaxInventorySlots => maxInventorySlots;
    public bool IsInventoryFull => inventory.Count >= maxInventorySlots;
    public ItemData CurrentWeapon => currentWeapon;
    
    // Events
    public System.Action<ItemData> OnItemAdded;
    public System.Action<ItemData> OnItemRemoved;
    public System.Action<ItemData> OnWeaponEquipped;
    public System.Action<ItemData> OnEquipmentEquipped;
    public System.Action<ItemData> OnItemUsed;
    public System.Action<IReadOnlyList<ItemData>> OnInventoryChanged;

    void Awake()
    {
        playerAttribute = GetComponent<Attribute>();
        if (playerAttribute != null)
        {
            baseMaxHealth = playerAttribute.MaxHealth;
            baseDefense = playerAttribute.Defense;
        }
        BuildLookup();
        InitializeEquipmentSlots();
    }

    void Start()
    {
        if (DropManager.Instance != null)
        {
            DropManager.Instance.OnWeaponPickedUp += HandleWeaponPickedUp;
            DropManager.Instance.OnEquipmentPickedUp += HandleEquipmentPickedUp;
            DropManager.Instance.OnConsumablePickedUp += HandleConsumablePickedUp;
            
            if (logItemEvents)
            {
                Debug.Log("[PlayerItemManager] 已订阅拾取事件");
            }
        }
        else
        {
            Debug.LogWarning("[PlayerItemManager] DropManager未找到，无法订阅拾取事件");
        }

        // 自动装备默认武器（如果有配置）
        TryEquipDefaultWeapon();

        // 可选：添加起始物品用于测试背包
        if (addStarterItemsOnStart)
        {
            AddStarterItems();
        }

        // 自动装备默认装备（Armor 槽）
        TryEquipDefaultEquipment();
    }

    void OnDestroy()
    {
        if (DropManager.Instance != null)
        {
            DropManager.Instance.OnWeaponPickedUp -= HandleWeaponPickedUp;
            DropManager.Instance.OnEquipmentPickedUp -= HandleEquipmentPickedUp;
            DropManager.Instance.OnConsumablePickedUp -= HandleConsumablePickedUp;
        }
    }

    private void HandleWeaponPickedUp(ItemData weapon)
    {
        AddItem(weapon);
    }

    private void HandleEquipmentPickedUp(ItemData equipment)
    {
        AddItem(equipment);
    }

    private void HandleConsumablePickedUp(ItemData consumable)
    {
        AddItem(consumable);
    }

    private void InitializeEquipmentSlots()
    {
        equippedItems.Clear();
        equippedItems[EquipmentSlot.Weapon] = null;
        equippedItems[EquipmentSlot.Armor] = null;
        equippedItems[EquipmentSlot.Accessory] = null;
    }

    /// <summary>
    /// 添加物品，支持堆叠，不足上限时分堆新建。
    /// </summary>
    public bool AddItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0)
        {
            if (logItemEvents) Debug.LogWarning("[PlayerItemManager] 试图添加空物品或数量<=0");
            return false;
        }

        int remaining = amount;
        int maxStack = GetMaxStackSize(item);

        // 先堆叠到已有堆
        if (maxStack > 1)
        {
            foreach (var stack in inventory)
            {
                if (stack.itemId == item.itemId && stack.itemAsset == item.itemAsset && stack.enhancementLevel == item.enhancementLevel)
                {
                    int space = maxStack - stack.stackCount;
                    if (space <= 0) continue;
                    int add = Mathf.Min(space, remaining);
                    stack.stackCount += add;
                    remaining -= add;
                    if (remaining <= 0)
                    {
                        if (logItemEvents) Debug.Log($"[PlayerItemManager] 堆叠物品 {item.itemName} +{amount}");
                        return true;
                    }
                }
            }
        }

        // 需要新建堆
        while (remaining > 0)
        {
            if (IsInventoryFull)
            {
                if (logItemEvents) Debug.LogWarning($"[PlayerItemManager] 背包已满，剩余未添加: {remaining}");
                return false;
            }

            int addCount = Mathf.Min(remaining, maxStack);
            ItemData newStack = new ItemData(item)
            {
                stackCount = addCount
            };
            inventory.Add(newStack);
            OnItemAdded?.Invoke(newStack);
            if (logItemEvents) Debug.Log($"[PlayerItemManager] 新增堆: {item.itemName} x{addCount} (max {maxStack})");
            remaining -= addCount;
        }

        OnItemsUpdated();
        return true;
    }

    /// <summary>
    /// 移除指定堆栈数量，可能拆分多个堆。返回是否移除成功（数量足够）。
    /// </summary>
    public bool RemoveItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;
        return RemoveItemById(item.itemId, item.itemAsset, amount);
    }

    public bool RemoveItem(string itemId, int amount = 1)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0) return false;
        return RemoveItemById(itemId, null, amount);
    }

    private bool RemoveItemById(string itemId, ItemBase asset, int amount)
    {
        int remaining = amount;
        // 从后往前遍历，便于删除
        for (int i = inventory.Count - 1; i >= 0 && remaining > 0; i--)
        {
            var stack = inventory[i];
            if (stack.itemId != itemId) continue;
            if (asset != null && stack.itemAsset != asset) continue;

            int take = Mathf.Min(stack.stackCount, remaining);
            stack.stackCount -= take;
            remaining -= take;

            if (stack.stackCount <= 0)
            {
                inventory.RemoveAt(i);
                OnItemRemoved?.Invoke(stack);
            }
        }

        if (remaining > 0)
        {
            if (logItemEvents) Debug.LogWarning($"[PlayerItemManager] 物品不足，未能移除 {amount} 个 {itemId}");
            return false;
        }

        OnItemsUpdated();
        return true;
    }

    public bool EquipWeapon(ItemData weapon)
    {
        if (weapon == null || weapon.itemType != DropItemType.Weapon)
        {
            if (logItemEvents) Debug.LogWarning("[PlayerItemManager] 装备失败：物品为空或不是武器");
            return false;
        }

        // 已经装备同一实例则直接返回
        if (currentWeapon == weapon)
        {
            return true;
        }

        // 先从背包移除当前要装备的堆（如果存在）
        RemoveItem(weapon, weapon.stackCount);

        // 处理旧武器放回背包
        if (currentWeapon != null)
        {
            AddItem(currentWeapon);
        }

        currentWeapon = weapon;
        OnItemsUpdated();

        OnWeaponEquipped?.Invoke(weapon);
        if (logItemEvents) Debug.Log($"[PlayerItemManager] 装备武器: {weapon.itemName}");

        ApplyWeaponAttack(weapon);
        return true;
    }

    public bool EquipItem(ItemData equipment, EquipmentSlot slot)
    {
        if (equipment == null || equipment.itemType != DropItemType.Equipment)
        {
            if (logItemEvents) Debug.LogWarning("[PlayerItemManager] 装备失败：物品为空或不是装备");
            return false;
        }

        // 已经装备同一实例则直接返回
        if (equippedItems.ContainsKey(slot) && equippedItems[slot] == equipment)
        {
            return true;
        }

        // 先从背包移除当前要装备的堆（如果存在）
        RemoveItem(equipment, equipment.stackCount);

        // 处理旧装备放回背包
        if (equippedItems.ContainsKey(slot) && equippedItems[slot] != null)
        {
            AddItem(equippedItems[slot]);
        }

        equippedItems[slot] = equipment;
        OnItemsUpdated();

        OnEquipmentEquipped?.Invoke(equipment);
        if (logItemEvents) Debug.Log($"[PlayerItemManager] 装备: {equipment.itemName} 到 {slot}");

        RecalculateEquipmentStats();
        return true;
    }

    public bool UseConsumable(ItemData consumable)
    {
        if (consumable == null) return false;

        if (consumable.itemType != DropItemType.Health &&
            consumable.itemType != DropItemType.Mana &&
            consumable.itemType != DropItemType.Consumable)
        {
            if (logItemEvents) Debug.LogWarning($"[PlayerItemManager] 不能使用此类物品: {consumable.itemName}");
            return false;
        }

        ApplyConsumableEffect(consumable);

        // 消耗一个单位
        RemoveItem(consumable, 1);

        OnItemUsed?.Invoke(consumable);
        if (logItemEvents) Debug.Log($"[PlayerItemManager] 使用物品: {consumable.itemName}");
        return true;
    }

    private void ApplyConsumableEffect(ItemData consumable)
    {
        if (consumable == null) return;

        // 目前仅实现基础的回血/回蓝药剂（ConsumableItem）
        if (consumable.itemAsset is ConsumableItem consumableAsset)
        {
            if (playerAttribute == null)
            {
                playerAttribute = GetComponent<Attribute>();
            }

            if (playerAttribute != null)
            {
                int heal = consumableAsset.HealAmount;
                int mana = consumableAsset.ManaAmount;

                if (heal > 0)
                {
                    playerAttribute.Heal(heal);
                    if (logItemEvents) Debug.Log($"[PlayerItemManager] 使用 {consumable.itemName} 恢复生命 {heal}");
                }

                if (mana > 0)
                {
                    playerAttribute.AddMana(mana);
                    if (logItemEvents) Debug.Log($"[PlayerItemManager] 使用 {consumable.itemName} 恢复法力 {mana}");
                }
            }
        }
    }

    private void TryEquipDefaultWeapon()
    {
        if (currentWeapon != null) return;
        if (defaultWeapon == null) return;

        var itemData = new ItemData(defaultWeapon);
        EquipWeapon(itemData);
    }

    private void TryEquipDefaultEquipment()
    {
        if (defaultEquipment == null) return;
        // 仅当目标槽位为空时装备
        if (GetEquippedItem(EquipmentSlot.Armor) != null) return;

        var itemData = new ItemData(defaultEquipment);
        EquipItem(itemData, EquipmentSlot.Armor);
    }

    /// <summary>
    /// 清空背包（不影响已装备的物品）。
    /// </summary>
    public void ResetInventory()
    {
        inventory.Clear();
        OnItemsUpdated();
    }

    /// <summary>
    /// 获取用于存档的堆栈数据列表。
    /// </summary>
    public List<ItemStackData> GetInventoryStacksForSave()
    {
        var list = new List<ItemStackData>();
        foreach (var stack in inventory)
        {
            if (stack == null || string.IsNullOrEmpty(stack.itemId) || stack.stackCount <= 0) continue;
            list.Add(new ItemStackData
            {
                itemId = stack.itemId,
                count = stack.stackCount,
                enhancementLevel = stack.enhancementLevel,
                durability = 0
            });
        }
        return list;
    }

    /// <summary>
    /// 按存档数据恢复背包（不自动装备）。
    /// </summary>
    public void LoadInventoryFromSave(List<ItemStackData> stacks)
    {
        ResetInventory();
        if (stacks == null) return;
        foreach (var s in stacks)
        {
            if (string.IsNullOrEmpty(s.itemId) || s.count <= 0) continue;
            AddItemById(s.itemId, s.count, s.enhancementLevel);
        }
    }

    private void ApplyWeaponAttack(ItemData weapon)
    {
        if (playerAttribute == null)
        {
            if (logItemEvents) Debug.LogWarning("[PlayerItemManager] 未找到 Attribute，无法更新攻击力");
            return;
        }

        if (weapon == null || weapon.itemAsset == null)
        {
            if (logItemEvents) Debug.LogWarning("[PlayerItemManager] 武器或其资产为空，无法更新攻击力");
            return;
        }

        if (weapon.itemAsset is WeaponItem weaponAsset)
        {
            int runtimeLevel = weapon.enhancementLevel;
            int attackValue = weaponAsset.GetFinalAttack(runtimeLevel);
            playerAttribute.SetAttack(attackValue);
            if (logItemEvents) Debug.Log($"[PlayerItemManager] 攻击力已设为武器 baseAttack: {attackValue}");
        }
        else if (logItemEvents)
        {
            Debug.LogWarning("[PlayerItemManager] itemAsset 不是 WeaponItem，无法更新攻击力");
        }
    }

    /// <summary>
    /// 按已装备的装备重算防御和最大生命（生命加成作用于最大生命值）。
    /// </summary>
    private void RecalculateEquipmentStats()
    {
        if (playerAttribute == null) return;

        int defenseBonus = 0;
        int maxHealthBonus = 0;

        foreach (var kv in equippedItems)
        {
            var item = kv.Value;
            if (item == null || item.itemAsset == null) continue;

            if (item.itemAsset is EquipmentItem equipAsset)
            {
                int level = item.enhancementLevel;
                defenseBonus += equipAsset.GetFinalDefense(level);
                maxHealthBonus += equipAsset.GetFinalMaxHealthBonus(level);
            }
        }

        playerAttribute.SetDefense(baseDefense + defenseBonus);
        playerAttribute.SetMaxHealth(baseMaxHealth + maxHealthBonus, fillHealth: false);

        if (logItemEvents)
        {
            Debug.Log($"[PlayerItemManager] 重算装备属性 -> 防御: {baseDefense}+{defenseBonus}, 最大生命: {baseMaxHealth}+{maxHealthBonus}");
        }
    }

    private int GetMaxStackSize(ItemData item)
    {
        if (item != null && item.itemAsset != null)
        {
            return item.itemAsset.MaxStackSize;
        }
        return 1;
    }

    /// <summary>
    /// 通过 itemId 添加物品，使用 itemLookup 解析资产；若未找到资产，将以纯 id 方式添加。
    /// </summary>
    public bool AddItemById(string itemId, int amount, int enhancementLevel = 0)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0) return false;
        ItemBase asset = ResolveItem(itemId);
        ItemData temp = asset != null ? new ItemData(asset) : new ItemData
        {
            itemId = itemId,
            itemName = itemId,
            itemType = DropItemType.Consumable,
            itemIcon = null,
            description = "",
            itemAsset = null,
            value = 0,
            customProperties = new Dictionary<string, float>(),
            stackCount = 1
        };
        temp.enhancementLevel = enhancementLevel;
        return AddItem(temp, amount);
    }

    private ItemBase ResolveItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;
        if (itemLookup != null && itemLookup.TryGetValue(itemId, out var found))
        {
            return found;
        }
        return null;
    }

    /// <summary>
    /// 直接用 ItemBase 添加物品（便于测试/起始物品）。
    /// </summary>
    public bool AddItem(ItemBase itemBase, int amount = 1)
    {
        if (itemBase == null || amount <= 0) return false;
        return AddItem(new ItemData(itemBase), amount);
    }

    private void AddStarterItems()
    {
        foreach (var s in starterItems)
        {
            if (s.item == null || s.count <= 0) continue;
            AddItem(s.item, s.count);
        }
    }

    private void BuildLookup()
    {
        itemLookup.Clear();
        foreach (var item in itemDatabase)
        {
            if (item == null || string.IsNullOrEmpty(item.ItemId)) continue;
            if (!itemLookup.ContainsKey(item.ItemId))
            {
                itemLookup.Add(item.ItemId, item);
            }
        }
    }

    public ItemData GetEquippedItem(EquipmentSlot slot)
    {
        if (equippedItems.ContainsKey(slot))
        {
            return equippedItems[slot];
        }
        return null;
    }

    public bool HasItem(ItemData item)
    {
        return inventory.Contains(item);
    }

    public IReadOnlyList<ItemData> GetInventoryItems()
    {
        return new ReadOnlyCollection<ItemData>(inventory);
    }

    /// <summary>
    /// 获取指定 itemId 的总数量（跨堆叠）。
    /// </summary>
    public int GetItemCount(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return 0;
        int total = 0;
        foreach (var stack in inventory)
        {
            if (stack != null && stack.itemId == itemId)
            {
                total += stack.stackCount;
            }
        }
        return total;
    }

    /// <summary>
    /// 重新应用当前装备的武器攻击力。
    /// </summary>
    public void ReapplyWeaponAttack()
    {
        if (currentWeapon != null)
        {
            ApplyWeaponAttack(currentWeapon);
        }
    }

    /// <summary>
    /// 重新计算装备提供的防御/生命。
    /// </summary>
    public void RecalculateEquippedStats()
    {
        RecalculateEquipmentStats();
    }

    private void OnItemsUpdated()
    {
        OnInventoryChanged?.Invoke(new ReadOnlyCollection<ItemData>(inventory));
    }
}

/// <summary>
/// 物品数据（存储在背包中的实例，支持堆叠）。
/// </summary>
[System.Serializable]
public class ItemData
{
    public string itemId;
    public string itemName;
    public DropItemType itemType;
    public Sprite itemIcon;
    public string description;
    public ItemBase itemAsset;
    
    public int value;
    public Dictionary<string, float> customProperties;
    public int stackCount = 1;
    public int enhancementLevel = 0;

    public ItemData()
    {
        customProperties = new Dictionary<string, float>();
    }
    
    public ItemData(DropItem dropItem)
    {
        if (dropItem != null)
        {
            itemId = dropItem.itemName;
            itemName = dropItem.itemName;
            itemType = dropItem.itemType;
            itemIcon = dropItem.GetIcon();
            description = "";
            value = 0;
            customProperties = new Dictionary<string, float>();
            itemAsset = dropItem.itemAsset;
            stackCount = 1;
            enhancementLevel = 0;
        }
    }

    public ItemData(ItemBase itemBase)
    {
        if (itemBase != null)
        {
            itemId = itemBase.ItemId;
            itemName = itemBase.ItemName;
            if (itemBase is WeaponItem)
                itemType = DropItemType.Weapon;
            else if (itemBase is EquipmentItem)
                itemType = DropItemType.Equipment;
            else
                itemType = DropItemType.Consumable;

            itemIcon = itemBase.Icon;
            description = itemBase.Description;
            value = 0;
            customProperties = new Dictionary<string, float>();
            itemAsset = itemBase;
            stackCount = 1;
            enhancementLevel = 0;
        }
    }

    public ItemData(ItemData other)
    {
        if (other != null)
        {
            itemId = other.itemId;
            itemName = other.itemName;
            itemType = other.itemType;
            itemIcon = other.itemIcon;
            description = other.description;
            itemAsset = other.itemAsset;
            value = other.value;
            customProperties = new Dictionary<string, float>(other.customProperties ?? new Dictionary<string, float>());
            stackCount = other.stackCount;
            enhancementLevel = other.enhancementLevel;
        }
    }
}

public enum EquipmentSlot
{
    Weapon,
    Armor,
    Accessory
}

[System.Serializable]
public struct StarterItem
{
    public ItemBase item;
    public int count;
}
