using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LootTable", menuName = "Loot/Loot Table")]
public class LootTable : ScriptableObject
{
    public List<LootItem> items = new List<LootItem>();

    [Tooltip("最大掉落数量，控制每次掉落时会尝试掉落多少种不同物品。如果为0，表示不限制，会尝试掉落所有有权重的物品（实际掉落取决于权重概率）。")]
    public int maxDrops = 1;

    [Tooltip("是否允许重复掉落同一物品。true = 同一 prefab 可以掉落多次，false = 只能掉一次。")]
    public bool allowDuplicates = false;

    // 通过权重系统或直接概率选择要掉落的 LootItem 列表
    public List<(GameObject prefab, int amount)> RollDrops()
    {
        List<(GameObject, int)> results = new List<(GameObject, int)>();

        if (items == null || items.Count == 0) return results;

        // 首先处理 explicitChance 的绝对概率掉落（每个独立判断）
        List<int> availableIndices = new List<int>();
        float totalWeight = 0f;
        for (int i = 0; i < items.Count; i++)
        {
            var it = items[i];
            if (it.explicitChance >= 0f)
            {
                // 随机判断是否掉落（概率值在 [0,1]）
                float r = Random.value;
                if (r <= it.explicitChance)
                {
                    int amt = Random.Range(it.minAmount, it.maxAmount + 1);
                    results.Add((it.prefab, amt));
                    if (!allowDuplicates) availableIndices.Add(i); // mark handled to possibly exclude from further selection
                }
            }
            // 收集权重数据，用于后续权重选择
            if (it.weight > 0f)
            {
                totalWeight += it.weight;
                if (!availableIndices.Contains(i)) // mark as candidate
                    availableIndices.Add(i);
            }
        }

        // 如果 maxDrops == 0 表示不限制，会自动尝试所有有权重的物品进行一次选择（实际掉落取决于权重概率），否则选择 up to maxDrops 个
        if (totalWeight > 0f && maxDrops != 0)
        {
            int dropsToDo = Mathf.Max(1, maxDrops - results.Count);
            // 按权重选择 dropsToDo 个物品，直到没有候选物
            List<int> candidateIndices = new List<int>();
            for (int i = 0; i < items.Count; i++)
            {
                // 只将 weight > 0 的加入候选
                if (items[i].weight > 0f) candidateIndices.Add(i);
            }

            for (int d = 0; d < dropsToDo && candidateIndices.Count > 0; d++)
            {
                // 计算 subtotal weight
                float subtotal = 0f;
                foreach (var idx in candidateIndices) subtotal += items[idx].weight;

                float pick = Random.Range(0f, subtotal);
                float acc = 0f;
                int chosenIdx = candidateIndices[0];
                foreach (var idx in candidateIndices)
                {
                    acc += items[idx].weight;
                    if (pick <= acc)
                    {
                        chosenIdx = idx;
                        break;
                    }
                }

                var chosen = items[chosenIdx];
                int amt = Random.Range(chosen.minAmount, chosen.maxAmount + 1);
                results.Add((chosen.prefab, amt));

                if (!allowDuplicates)
                {
                    // 从候选列表移除 chosenIdx
                    candidateIndices.Remove(chosenIdx);
                }
            }
        }
        else if (totalWeight > 0f && maxDrops == 0)
        {
            // maxDrops == 0: 对每个权重>0的物品进行一次独立权重判断掉落概率 = weight / totalWeight。
            float subtotal = totalWeight;
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                if (it.weight <= 0f) continue;
                float prob = it.weight / subtotal;
                if (Random.value <= prob)
                {
                    int amt = Random.Range(it.minAmount, it.maxAmount + 1);
                    results.Add((it.prefab, amt));
                }
            }
        }

        return results;
    }
}
