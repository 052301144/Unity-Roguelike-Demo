using UnityEngine;

[System.Serializable]
public class LootItem
{
    // 掉落的 prefab（必需）
    public GameObject prefab;

    // 掉落权重（权重大，被选中概率高，如果所有物品权重为 0，则回退到 equal-chance）
    // 也可以使用 explicitChance（见下面），支持直接设置绝对概率
    public float weight = 1f;

    // 当 explicitChance >= 0 时，使用 explicitChance（0~1）作为绝对掉落概率（会优先判断）
    // 如果设为 -1 则视为未使用（默认）
    [Range(-1f, 1f)]
    public float explicitChance = -1f;

    // 最小/最大掉落数量（当掉落时）
    public int minAmount = 1;
    public int maxAmount = 1;

    // 是否被选择到候选列表中，再根据 explicitChance 判断是否掉落（用于两阶段控制）
    // 一般情况不需要使用
}
