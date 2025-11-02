using System.Collections.Generic; // 引入集合类命名空间
using UnityEngine; // 引入Unity引擎命名空间

/// <summary>
/// 掉落配置表 - 定义敌人可以掉落的所有物品及其概率
/// </summary>

    [CreateAssetMenu(fileName = "NewDropTable", menuName = "Roguelike/Drop Table")]
    // 上面这行代码会在Unity的Create菜单中添加创建掉落表的选项
    public class DropTable : ScriptableObject // 继承自ScriptableObject，可以作为资源文件保存
{
    [Header("基础设置")] // 基础设置分组
    public string enemyName;              // 使用这个掉落表的敌人名称
    public int minTotalDrops = 1;         // 每次掉落的最小物品总数
    public int maxTotalDrops = 3;         // 每次掉落的最大物品总数

    [Header("掉落物品列表")] // 可掉落物品列表分组
    public List<DropItem> possibleDrops = new List<DropItem>(); // 所有可能掉落的物品列表

    [Header("金币掉落设置")] // 金币特殊设置分组
    public bool alwaysDropCoin = true;    // 是否总是尝试掉落金币
    public int minCoins = 1;              // 最小金币数量
    public int maxCoins = 5;              // 最大金币数量
    public float coinDropChance = 0.8f;   // 金币掉落概率

    /// <summary>
    /// 获取随机掉落物品列表
    /// </summary>
    /// <returns>本次应该掉落的物品列表</returns>
    public List<DropItem> GetRandomDrops()
    {
        // 创建空的掉落列表
        List<DropItem> drops = new List<DropItem>();

        // 计算本次掉落的总物品数量（不包括金币）
        int totalDrops = Random.Range(minTotalDrops, maxTotalDrops + 1);

        // 随机选择掉落物品（只选择生命和魔法物品，不包括金币）
        for (int i = 0; i < totalDrops && possibleDrops.Count > 0; i++)
        {
            // 从可能掉落列表中随机选择一个物品
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
            // 创建金币掉落项
            DropItem coinDrop = new DropItem();
            coinDrop.itemName = "金币";           // 设置物品名称
            coinDrop.itemType = DropItemType.Coin; // 设置物品类型为金币
            coinDrop.minQuantity = minCoins;      // 设置最小数量
            coinDrop.maxQuantity = maxCoins;      // 设置最大数量
            coinDrop.dropChance = 1f;             // 金币总是掉落

            // 从可能掉落列表中查找金币预制体和图标
            foreach (var drop in possibleDrops)
            {
                if (drop.itemType == DropItemType.Coin)
                {
                    // 复制金币的预制体和图标
                    coinDrop.itemPrefab = drop.itemPrefab;
                    coinDrop.itemIcon = drop.itemIcon;
                    break; // 找到后就退出循环
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
    /// <returns>该类型的所有物品列表</returns>
    public List<DropItem> GetDropsByType(DropItemType type)
    {
        // 使用FindAll方法查找所有匹配类型的物品
        return possibleDrops.FindAll(item => item.itemType == type);
    }

    /// <summary>
    /// 验证掉落表配置是否正确
    /// </summary>
    public void ValidateTable()
    {
        // 遍历所有可能掉落的物品
        foreach (var drop in possibleDrops)
        {
            // 检查预制体是否设置
            if (drop.itemPrefab == null)
            {
                // 输出警告信息
                Debug.LogWarning("掉落表中存在未设置预制体的物品: " + drop.itemName);
            }

            // 检查数量设置是否合理
            if (drop.minQuantity > drop.maxQuantity)
            {
                // 输出警告并自动修正
                Debug.LogWarning("物品 " + drop.itemName + " 的最小数量大于最大数量，已自动修正");
                drop.maxQuantity = drop.minQuantity; // 将最大数量设置为最小数量
            }
        }
    }
}
