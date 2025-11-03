using UnityEngine; // 引入Unity引擎命名空间

/// <summary>
/// 掉落物品数据类 - 用于定义单个掉落物品的数据信息
/// </summary>
[System.Serializable] // 允许该类在Inspector窗口中显示和编辑
public class DropItem
{
    [Header("物品信息")] // 在Inspector中显示分组标题
    public string itemName;              // 物品显示名称
    public GameObject itemPrefab;        // 物品的预制体对象引用
    public Sprite itemIcon;              // 物品的图标精灵

    [Header("掉落概率")] // 掉落概率相关的部分
    [Range(0, 1)] // 在Inspector中显示为0-1的滑动条
    public float dropChance = 0.5f;      // 物品掉落概率（0-1之间）
    public int minQuantity = 1;          // 最小掉落数量
    public int maxQuantity = 1;          // 最大掉落数量

    [Header("物理效果")] // 物理效果相关的部分
    public bool applyForce = true;       // 是否在掉落时施加物理力
    public float forceMultiplier = 1f;   // 力的强度倍数

    [Header("物品类型")] // 物品类型分类
    public DropItemType itemType;        // 物品类型枚举

    /// <summary>
    /// 判断是否应该掉落此物品
    /// </summary>
    /// <returns>true表示应该掉落，false表示不掉落</returns>
    public bool ShouldDrop()
    {
        // 生成0-1随机数，如果小于等于掉落概率则返回true
        return Random.value <= dropChance;
    }

    /// <summary>
    /// 获取随机掉落数量
    /// </summary>
    /// <returns>最小数量和最大数量之间的随机数</returns>
    public int GetDropQuantity()
    {
        // 返回最小数量和最大数量之间的随机数
        return Random.Range(minQuantity, maxQuantity + 1);
    }

    /// <summary>
    /// 获取物品的详细描述信息
    /// </summary>
    /// <returns>格式化后的描述信息，包含物品名称、掉落概率、掉落数量和物品类型</returns>
    public string GetDescription()
    {
        // 将概率转换为百分比字符串，保留1位小数
        string chanceText = (dropChance * 100).ToString("F1") + "%";
        // 如果最小数量和最大数量相同，只显示一个数字，否则显示范围
        string quantityText = minQuantity == maxQuantity ?
            minQuantity.ToString() : minQuantity + "-" + maxQuantity;

        // 返回格式化的描述信息
        return itemName + "\n" +                    // 物品名称
               "掉落概率: " + chanceText + "\n" +   // 掉落概率
               "掉落数量: " + quantityText + "\n" + // 掉落数量
               "类型: " + itemType.ToString();      // 物品类型
    }
}

/// <summary>
/// 掉落物品类型枚举 - 定义所有可能的物品类型
/// </summary>
public enum DropItemType
{
    // 消耗品类
    Health,         // 生命值恢复物品
    Mana,           // 魔法值恢复物品  
    Coin,           // 金币物品
    
    // 装备类
    Weapon,         // 武器
    Equipment,      // 装备（防具、饰品等）
    Consumable      // 其他消耗品（可能包含多种效果）
}
