using System.Collections.Generic; // 引入集合命名空间
using UnityEngine; // 引入Unity引擎命名空间

/// <summary>
/// 掉落表配置 - 定义了敌人可以掉落的物品数据列表
/// </summary>

    [CreateAssetMenu(fileName = "NewDropTable", menuName = "Roguelike/Drop Table")]
    // 这行代码允许在Unity的Create菜单中添加创建选项
    public class DropTable : ScriptableObject // 继承自ScriptableObject，可以作为资源文件使用
{
    [Header("基本信息")] // 基本信息配置部分
    public string enemyName;              // 使用此掉落表的敌人名称
    public int minTotalDrops = 1;         // 每次掉落的最小物品数
    public int maxTotalDrops = 3;         // 每次掉落的最大物品数

    [Header("可掉落物品列表")] // 可掉落物品列表部分
    public List<DropItem> possibleDrops = new List<DropItem>(); // 所有可能的掉落物品列表

    [Header("金币设置")] // 金币相关配置部分
    public bool alwaysDropCoin = true;    // 是否总是尝试掉落金币
    public int minCoins = 1;              // 最小金币数
    public int maxCoins = 5;              // 最大金币数
    public float coinDropChance = 0.8f;   // 金币掉落概率

    /// <summary>
    /// 获取随机掉落物品列表
    /// </summary>
    /// <returns>应该掉落的物品列表</returns>
    public List<DropItem> GetRandomDrops()
    {
        // 创建空的掉落列表
        List<DropItem> drops = new List<DropItem>();

        // 计算本次掉落的物品数量（随机数，不包括金币）
        int totalDrops = Random.Range(minTotalDrops, maxTotalDrops + 1);

        // 随机选择掉落物品（只选择非金币和魔法物品，不包括金币）
        for (int i = 0; i < totalDrops && possibleDrops.Count > 0; i++)
        {
            // 从可能的掉落列表中随机选择一个物品
            DropItem randomItem = possibleDrops[Random.Range(0, possibleDrops.Count)];
            // 检查是否应该掉落且不是金币类型
            if (randomItem.ShouldDrop() && randomItem.itemType != DropItemType.Coin)
            {
                // 添加到掉落列表
                drops.Add(randomItem);
            }
        }

        // 检查是否需要掉落金币
        if (alwaysDropCoin && Random.value <= coinDropChance)
        {
            // 创建金币掉落对象
            DropItem coinDrop = new DropItem();
            coinDrop.itemName = "金币";           // 设置物品名称
            coinDrop.itemType = DropItemType.Coin; // 设置物品类型为金币
            coinDrop.minQuantity = minCoins;      // 设置最小金币数
            coinDrop.maxQuantity = maxCoins;      // 设置最大金币数
            coinDrop.dropChance = 1f;             // 设置为必定掉落

            // 从可能的掉落列表中查找金币预制体和图标
            foreach (var drop in possibleDrops)
            {
                if (drop.itemType == DropItemType.Coin)
                {
                    // 复制金币的预制体和图标
                    coinDrop.itemPrefab = drop.itemPrefab;
                    coinDrop.itemIcon = drop.itemIcon;
                    break; // 找到后退出循环
                }
            }

            // 将金币添加到掉落列表
            drops.Add(coinDrop);
        }

        // 返回最终的掉落列表
        return drops;
    }

    /// <summary>
    /// 获取特定类型的掉落物品
    /// </summary>
    /// <param name="type">要查找的物品类型</param>
    /// <returns>该类型的掉落物品列表</returns>
    public List<DropItem> GetDropsByType(DropItemType type)
    {
        // 使用FindAll方法筛选出匹配类型的物品
        return possibleDrops.FindAll(item => item.itemType == type);
    }

    /// <summary>
    /// 验证掉落表配置是否正确
    /// </summary>
    public void ValidateTable()
    {
        // 遍历所有可能的掉落物品
        foreach (var drop in possibleDrops)
        {
            // 检查预制体是否为空
            if (drop.itemPrefab == null)
            {
                // 输出警告信息
                Debug.LogWarning("掉落表中存在未设置预制体的物品: " + drop.itemName);
            }

            // 检查数量范围是否正确
            if (drop.minQuantity > drop.maxQuantity)
            {
                // 输出警告并自动修复
                Debug.LogWarning("物品 " + drop.itemName + " 的最小数量大于最大数量，已自动修复");
                drop.maxQuantity = drop.minQuantity; // 将最大数量设为最小数量
            }
        }
    }
}
