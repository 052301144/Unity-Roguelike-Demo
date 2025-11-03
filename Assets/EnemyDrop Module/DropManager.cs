using System.Collections; // 引入协程命名空间
using System.Collections.Generic; // 引入集合命名空间
using UnityEngine; // 引入Unity引擎命名空间

/// <summary>
/// 掉落管理器 - 负责全局掉落系统的管理器
/// </summary>
/// 
public class DropManager : MonoBehaviour
{
    [Header("全局设置")] // 全局配置部分
    public bool enableDrops = true;                      // 是否启用掉落系统
    public float autoPickupRange = 1.5f;                 // 自动拾取范围半径
    public float itemLifetime = 30f;                     // 物品存活时间（秒）

    [Header("调试设置")] // 调试相关配置部分
    public bool showDebugInfo = true;                    // 是否显示调试信息
    public bool logDropEvents = true;                    // 是否记录掉落事件日志

    // 单例实例引用 - 确保在整个游戏中只有一个掉落管理器
    public static DropManager Instance { get; private set; }

    // 活动中的掉落物品列表 - 管理所有当前存在的掉落物品
    private List<GameObject> activeDrops = new List<GameObject>();

    // 玩家对象引用 - 用于自动拾取检测
    private GameObject player;

    //玩家金币字段
    private int totalCoins = 0;

    // 事件系统 - 用于与其他模块通信，避免直接依赖其他模块
    public System.Action<int> OnCoinCollected;           // 金币收集事件
    public System.Action<int> OnHealthRestored;          // 生命值恢复事件  
    public System.Action<int> OnManaRestored;            // 魔法值恢复事件

    /// <summary>
    /// Awake方法 - 在对象创建时调用
    /// </summary>
    void Awake()
    {
        // 单例模式实现
        if (Instance == null) // 如果实例不存在
        {
            Instance = this; // 设置当前对象为实例
        }
        else // 如果实例已存在
        {
            Destroy(gameObject); // 销毁重复的对象
            return; // 直接返回，不执行后续代码
        }

        // 查找场景中的玩家对象
        player = GameObject.FindGameObjectWithTag("Player");
        // 检查是否找到玩家
        if (player == null)
        {
            // 输出警告信息
            Debug.LogWarning("掉落管理器: 未找到玩家对象，请确保场景中有Player标签");
        }
    }

    /// <summary>
    /// Update方法 - 每帧调用
    /// </summary>
    void Update()
    {
        // 如果玩家存在，执行自动拾取检测
        if (player != null)
        {
            CheckAutoPickup(); // 调用自动拾取检测方法
        }
    }

    /// <summary>
    /// 生成掉落物品
    /// </summary>
    /// <param name="dropItem">要生成的物品数据</param>
    /// <param name="dropPosition">掉落位置</param>
    public void SpawnDropItem(DropItem dropItem, Vector3 dropPosition)
    {
        // 检查是否启用掉落系统且物品预制体不为空
        if (!enableDrops || dropItem.itemPrefab == null) return;


        // 获取掉落数量
        int quantity = dropItem.GetDropQuantity();

        // 生成指定数量的掉落物品
        for (int i = 0; i < quantity; i++)
        {
            // 使用协程生成掉落物品，带有延迟避免同时生成
            StartCoroutine(SpawnSingleDrop(dropItem, dropPosition, i * 0.1f));
        }

        // 如果启用日志，记录掉落信息
        if (logDropEvents)
        {
            Debug.Log("生成掉落物品: " + dropItem.itemName + " x" + quantity);
        }
    }

    /// <summary>
    /// 生成单个掉落物品的协程
    /// </summary>
    /// <param name="dropItem">物品数据</param>
    /// <param name="position">掉落位置</param>
    /// <param name="delay">生成延迟</param>
    private IEnumerator SpawnSingleDrop(DropItem dropItem, Vector3 position, float delay)
    {
        // 等待指定的延迟时间
        yield return new WaitForSeconds(delay);

        // 实例化掉落物品预制体
        GameObject dropObject = Instantiate(dropItem.itemPrefab, position, Quaternion.identity);
        // 添加到活动物品列表
        activeDrops.Add(dropObject);

        // 获取物品控制器组件
        DropItemController itemController = dropObject.GetComponent<DropItemController>();
        // 如果控制器存在，初始化物品
        if (itemController != null)
        {
            itemController.Initialize(dropItem, itemLifetime);
        }

        // 检查是否需要应用物理力
        if (dropItem.applyForce)
        {
            // 获取刚体组件
            Rigidbody2D rb = dropObject.GetComponent<Rigidbody2D>();
            // 如果刚体存在
            if (rb != null)
            {
                // 计算随机方向向量
                Vector2 randomDirection = new Vector2(
                    Random.Range(-1f, 1f),     // X轴随机值
                    Random.Range(0.5f, 1f)     // Y轴随机值，偏向上方
                ).normalized; // 标准化方向向量

                // 计算力的大小
                float force = Random.Range(2f, 5f) * dropItem.forceMultiplier;
                // 施加冲力
                rb.AddForce(randomDirection * force, ForceMode2D.Impulse);
            }
        }
    }

    /// <summary>
    /// 从掉落表生成多个物品
    /// </summary>
    /// <param name="dropTable">掉落表数据</param>
    /// <param name="dropPosition">掉落位置</param>
    public void SpawnDropsFromTable(DropTable dropTable, Vector3 dropPosition)
    {
        // 检查是否启用掉落系统且掉落表不为空
        if (!enableDrops || dropTable == null) return;

        Debug.Log("开始生成掉落，敌人: " + dropTable.enemyName);
        Debug.Log("可能的掉落物品数: " + dropTable.possibleDrops.Count);

        // 从掉落表获取随机掉落物品列表
        List<DropItem> drops = dropTable.GetRandomDrops();

        // 如果启用日志，记录掉落信息
        if (logDropEvents)
        {
            Debug.Log("从掉落表生成物品，敌人: " + dropTable.enemyName + ", 掉落数量: " + drops.Count);
        }

        // 生成所有掉落的物品对象
        foreach (var drop in drops)
        {
            SpawnDropItem(drop, dropPosition); // 调用生成掉落物品方法
        }
    }

    /// <summary>
    /// 自动拾取检测
    /// </summary>
    private void CheckAutoPickup()
    {
        // 从后往前遍历活动物品列表（避免删除时产生问题）
        for (int i = activeDrops.Count - 1; i >= 0; i--)
        {
            // 获取物品对象
            GameObject drop = activeDrops[i];
            // 如果物品已被销毁，跳过
            if (drop == null) continue;

            // 获取物品控制器组件
            DropItemController itemController = drop.GetComponent<DropItemController>();
            // 检查物品是否可以被拾取
            if (itemController != null && itemController.CanBePickedUp)
            {
                // 计算玩家与物品的距离
                float distance = Vector3.Distance(player.transform.position, drop.transform.position);
                // 检查距离是否在拾取范围内
                if (distance <= autoPickupRange)
                {
                    // 执行拾取操作
                    itemController.Pickup(player);
                }
            }
        }
    }


    /// <summary>
    /// 触发金币收集事件
    /// </summary>
    /// <param name="amount">金币数量</param>
    public void TriggerCoinCollected(int amount)
    {
        totalCoins += amount;
        // 触发所有注册的金币收集事件
        OnCoinCollected?.Invoke(amount);
        // 输出日志信息
        Debug.Log("获得金币: " + amount);
    }

    /// <summary>
    /// 触发生命值恢复事件
    /// </summary>
    /// <param name="amount">恢复数量</param>
    public void TriggerHealthRestored(int amount)
    {
        // 触发所有注册的生命值恢复事件
        OnHealthRestored?.Invoke(amount);
        // 输出日志信息
        Debug.Log("恢复生命值: " + amount);
    }

    /// <summary>
    /// 触发魔法值恢复事件
    /// </summary>
    /// <param name="amount">恢复数量</param>
    public void TriggerManaRestored(int amount)
    {
        // 触发所有注册的魔法值恢复事件
        OnManaRestored?.Invoke(amount);
        // 输出日志信息
        Debug.Log("恢复魔法值: " + amount);
    }

    /// <summary>
    /// 清除所有掉落物品
    /// </summary>
    public void ClearAllDrops()
    {
        // 销毁所有活动物品
        foreach (var drop in activeDrops)
        {
            // 如果物品存在，销毁它
            if (drop != null)
            {
                Destroy(drop);
            }
        }
        // 清空活动物品列表
        activeDrops.Clear();

        // 如果启用日志，记录清除信息
        if (logDropEvents)
        {
            Debug.Log("已清除所有的掉落物品");
        }
    }

    /// <summary>
    /// 从活动列表中移除物品
    /// </summary>
    /// <param name="dropObject">要移除的掉落物品对象</param>
    public void RemoveFromActiveDrops(GameObject dropObject)
    {
        // 从列表移除指定的掉落物品对象
        activeDrops.Remove(dropObject);
    }

    /// <summary>
    /// 获取总金币数
    /// </summary>
    /// <returns>当前总金币数量</returns>
    public int GetTotalCoins()
    {
        // 返回存储的总金币数量
        return totalCoins;
    }

    /// <summary>
    /// 测试掉落系统的上下文菜单方法
    /// </summary>
    [ContextMenu("测试掉落系统")]
    public void TestDropSystem()
    {
        // 如果启用显示调试信息，输出系统状态
        if (showDebugInfo)
        {
            Debug.Log("=== 掉落系统状态 ===");
            Debug.Log("活动中的掉落物品: " + activeDrops.Count);
            Debug.Log("自动拾取范围: " + autoPickupRange);
        }
    }
}
