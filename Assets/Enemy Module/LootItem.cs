using UnityEngine;

[System.Serializable]
public class LootItem
{
    // 掉落的 prefab（必须）
    public GameObject prefab;

    // 掉落概率权重（相对值）；如果全部权重为 0，则会退回到 equal-chance
    // 也可使用 explicitChance（见下面）来支持直接设置绝对概率
    public float weight = 1f;

    // 当 explicitChance >= 0 时，使用 explicitChance（0~1）作为绝对掉落概率（可选）
    // 如果设置为 -1 则视为未设置（默认）
    [Range(-1f, 1f)]
    public float explicitChance = -1f;

    // 最小/最大数量（例如掉落堆叠物）
    public int minAmount = 1;
    public int maxAmount = 1;

    // 是否在选择到该条目后再根据 explicitChance 决定真正是否掉落（用于二阶段控制）
    // （可留空或根据需要使用）
}
