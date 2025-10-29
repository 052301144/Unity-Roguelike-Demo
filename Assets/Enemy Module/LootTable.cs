using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LootTable", menuName = "Loot/Loot Table")]
public class LootTable : ScriptableObject
{
    public List<LootItem> items = new List<LootItem>();

    [Tooltip("最大掉落条目数（单次死亡最多掉多少种不同物品）。设为0表示不限制（会尝试掉所有满足概率的）。")]
    public int maxDrops = 1;

    [Tooltip("是否允许重复掉落同一物品（true = 同一 prefab 可能掉多份；false = 最多一份）")]
    public bool allowDuplicates = false;

    // 通过本方法根据权重/概率选择要掉落的若干 LootItem 与数量
    public List<(GameObject prefab, int amount)> RollDrops()
    {
        List<(GameObject, int)> results = new List<(GameObject, int)>();

        if (items == null || items.Count == 0) return results;

        // 首先处理 explicitChance 类的绝对概率掉落（每项独立）
        List<int> availableIndices = new List<int>();
        float totalWeight = 0f;
        for (int i = 0; i < items.Count; i++)
        {
            var it = items[i];
            if (it.explicitChance >= 0f)
            {
                // 独立判断是否掉落（概率区间 [0,1]）
                float r = Random.value;
                if (r <= it.explicitChance)
                {
                    int amt = Random.Range(it.minAmount, it.maxAmount + 1);
                    results.Add((it.prefab, amt));
                    if (!allowDuplicates) availableIndices.Add(i); // mark handled to possibly exclude from further selection
                }
            }
            // 计算权重用于后续按权重随机选择
            if (it.weight > 0f)
            {
                totalWeight += it.weight;
                if (!availableIndices.Contains(i)) // mark as candidate
                    availableIndices.Add(i);
            }
        }

        // 如果 maxDrops == 0 表示不限制（尝试对所有按权重的项进行一次选择），否则选择 up to maxDrops 次
        if (totalWeight > 0f && maxDrops != 0)
        {
            int dropsToDo = Mathf.Max(1, maxDrops - results.Count);
            // 按权重选择 dropsToDo 个（或直到没有候选）
            List<int> candidateIndices = new List<int>();
            for (int i = 0; i < items.Count; i++)
            {
                // 只有 weight > 0 才进入候选
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
                    // 从候选中移除 chosenIdx
                    candidateIndices.Remove(chosenIdx);
                }
            }
        }
        else if (totalWeight > 0f && maxDrops == 0)
        {
            // maxDrops == 0: 对每个权重>0的条目做一次独立权重判定（概率 = weight / totalWeight）
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
