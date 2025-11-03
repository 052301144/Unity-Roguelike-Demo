using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 掉落管理器 - 负责全局掉落系统的管理器
/// 重写版本：改进生成逻辑和验证
/// </summary>
public class DropManager : MonoBehaviour
{
    [Header("全局设置")]
    public bool enableDrops = true;
    public float autoPickupRange = 1.5f;
    public float itemLifetime = 30f;

    [Header("调试设置")]
    public bool showDebugInfo = true;
    public bool logDropEvents = true;

    public static DropManager Instance { get; private set; }

    private List<GameObject> activeDrops = new List<GameObject>();
    private GameObject player;
    private int totalCoins = 0;

    // 事件系统
    public System.Action<int> OnCoinCollected;
    public System.Action<int> OnHealthRestored;
    public System.Action<int> OnManaRestored;
    public System.Action<ItemData> OnWeaponPickedUp;      // 武器拾取事件
    public System.Action<ItemData> OnEquipmentPickedUp;   // 装备拾取事件
    public System.Action<ItemData> OnConsumablePickedUp;  // 消耗品拾取事件

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning($"[DropManager] 检测到重复的DropManager实例，销毁 {gameObject.name}");
            Destroy(gameObject);
            return;
        }

        FindPlayer();
    }

    void Update()
    {
        if (player != null)
        {
            CheckAutoPickup();
        }
    }

    /// <summary>
    /// 查找玩家对象
    /// </summary>
    private void FindPlayer()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("[DropManager] 未找到玩家对象，请确保场景中有Player标签");
            }
        }
    }

    /// <summary>
    /// 生成掉落物品
    /// </summary>
    public void SpawnDropItem(DropItem dropItem, Vector3 dropPosition)
    {
        if (!enableDrops)
        {
            if (logDropEvents) Debug.Log("[DropManager] 掉落系统已禁用");
            return;
        }

        if (dropItem == null)
        {
            Debug.LogError("[DropManager] 尝试生成null的掉落物品");
            return;
        }

        if (dropItem.itemPrefab == null)
        {
            Debug.LogError($"[DropManager] 掉落物品 {dropItem.itemName} 的预制体为null，无法生成");
            return;
        }

        int quantity = dropItem.GetDropQuantity();
        if (quantity <= 0)
        {
            if (logDropEvents) Debug.Log($"[DropManager] 掉落物品 {dropItem.itemName} 数量为0，跳过生成");
            return;
        }

        for (int i = 0; i < quantity; i++)
        {
            StartCoroutine(SpawnSingleDrop(dropItem, dropPosition, i * 0.1f));
        }

        if (logDropEvents)
        {
            Debug.Log($"[DropManager] 生成掉落物品: {dropItem.itemName} x{quantity} 位置: {dropPosition}");
        }
    }

    /// <summary>
    /// 生成单个掉落物品的协程 - 改进版本
    /// </summary>
    private IEnumerator SpawnSingleDrop(DropItem dropItem, Vector3 position, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!enableDrops) yield break;

        // 修复Z坐标 - 确保在2D相机视野内
        Vector3 spawnPosition = position;
        spawnPosition.z = 0f; // 强制Z坐标为0
        
        // 实例化预制体
        GameObject dropObject = Instantiate(dropItem.itemPrefab, spawnPosition, Quaternion.identity);
        
        if (dropObject == null)
        {
            Debug.LogError($"[DropManager] 实例化失败：{dropItem.itemPrefab.name}");
            yield break;
        }

        // 添加到活动列表
        activeDrops.Add(dropObject);

        // 获取并初始化控制器
        DropItemController itemController = dropObject.GetComponent<DropItemController>();
        if (itemController == null)
        {
            Debug.LogError($"[DropManager] {dropObject.name} 缺少DropItemController组件！");
            Destroy(dropObject);
            yield break;
        }

        // 立即初始化（在Start之前调用）
        itemController.Initialize(dropItem, itemLifetime);

        // 应用物理力（如果需要）
        if (dropItem.applyForce)
        {
            Rigidbody2D rb = dropObject.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 randomDirection = new Vector2(
                    Random.Range(-1f, 1f),
                    Random.Range(0.5f, 1f)
                ).normalized;

                float force = Random.Range(2f, 5f) * dropItem.forceMultiplier;
                rb.AddForce(randomDirection * force, ForceMode2D.Impulse);
            }
        }

        if (logDropEvents)
        {
            Debug.Log($"[DropManager] 成功生成掉落物品: {dropObject.name} 位置: {position}");
        }
    }

    /// <summary>
    /// 从掉落表生成多个物品
    /// </summary>
    public void SpawnDropsFromTable(DropTable dropTable, Vector3 dropPosition)
    {
        if (!enableDrops)
        {
            if (logDropEvents) Debug.Log("[DropManager] 掉落系统已禁用");
            return;
        }

        if (dropTable == null)
        {
            Debug.LogError("[DropManager] 掉落表为null，无法生成掉落");
            return;
        }

        if (logDropEvents)
        {
            Debug.Log($"[DropManager] 开始从掉落表生成物品 - 敌人: {dropTable.enemyName} 位置: {dropPosition}");
        }

        List<DropItem> drops = dropTable.GetRandomDrops();

        if (drops == null || drops.Count == 0)
        {
            if (logDropEvents) Debug.Log("[DropManager] 掉落表未生成任何物品");
            return;
        }

        if (logDropEvents)
        {
            Debug.Log($"[DropManager] 掉落表生成 {drops.Count} 个物品");
        }

        foreach (var drop in drops)
        {
            if (drop != null)
            {
                SpawnDropItem(drop, dropPosition);
            }
        }
    }

    /// <summary>
    /// 自动拾取检测
    /// </summary>
    private void CheckAutoPickup()
    {
        if (player == null)
        {
            FindPlayer();
            if (player == null) return;
        }

        for (int i = activeDrops.Count - 1; i >= 0; i--)
        {
            GameObject drop = activeDrops[i];
            if (drop == null)
            {
                activeDrops.RemoveAt(i);
                continue;
            }

            DropItemController itemController = drop.GetComponent<DropItemController>();
            if (itemController != null && itemController.CanBePickedUp)
            {
                float distance = Vector3.Distance(player.transform.position, drop.transform.position);
                if (distance <= autoPickupRange)
                {
                    itemController.Pickup(player);
                }
            }
        }
    }

    public void TriggerCoinCollected(int amount)
    {
        totalCoins += amount;
        OnCoinCollected?.Invoke(amount);
        if (logDropEvents) Debug.Log($"[DropManager] 获得金币: {amount} 总计: {totalCoins}");
    }

    public void TriggerHealthRestored(int amount)
    {
        OnHealthRestored?.Invoke(amount);
        if (logDropEvents) Debug.Log($"[DropManager] 恢复生命值: {amount}");
    }

    public void TriggerManaRestored(int amount)
    {
        OnManaRestored?.Invoke(amount);
        if (logDropEvents) Debug.Log($"[DropManager] 恢复魔法值: {amount}");
    }

    public void TriggerWeaponPickedUp(ItemData weapon)
    {
        OnWeaponPickedUp?.Invoke(weapon);
        if (logDropEvents) Debug.Log($"[DropManager] 拾取武器: {weapon.itemName}");
    }

    public void TriggerEquipmentPickedUp(ItemData equipment)
    {
        OnEquipmentPickedUp?.Invoke(equipment);
        if (logDropEvents) Debug.Log($"[DropManager] 拾取装备: {equipment.itemName}");
    }

    public void TriggerConsumablePickedUp(ItemData consumable)
    {
        OnConsumablePickedUp?.Invoke(consumable);
        if (logDropEvents) Debug.Log($"[DropManager] 拾取消耗品: {consumable.itemName}");
    }

    public void ClearAllDrops()
    {
        foreach (var drop in activeDrops)
        {
            if (drop != null)
            {
                Destroy(drop);
            }
        }
        activeDrops.Clear();

        if (logDropEvents) Debug.Log("[DropManager] 已清除所有的掉落物品");
    }

    public void RemoveFromActiveDrops(GameObject dropObject)
    {
        activeDrops.Remove(dropObject);
    }

    public int GetTotalCoins()
    {
        return totalCoins;
    }

    [ContextMenu("测试掉落系统")]
    public void TestDropSystem()
    {
        if (showDebugInfo)
        {
            Debug.Log("=== 掉落系统状态 ===");
            Debug.Log($"活动中的掉落物品: {activeDrops.Count}");
            Debug.Log($"自动拾取范围: {autoPickupRange}");
            Debug.Log($"玩家对象: {(player != null ? player.name : "未找到")}");
            Debug.Log($"总金币数: {totalCoins}");
        }
    }
}
