using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LootTable", menuName = "Loot/Loot Table")]
public class LootTable : ScriptableObject
{
    public List<LootItem> items = new List<LootItem>();

    [Tooltip("��������Ŀ���������������������ֲ�ͬ��Ʒ������Ϊ0��ʾ�����ƣ��᳢�Ե�����������ʵģ���")]
    public int maxDrops = 1;

    [Tooltip("�Ƿ������ظ�����ͬһ��Ʒ��true = ͬһ prefab ���ܵ���ݣ�false = ���һ�ݣ�")]
    public bool allowDuplicates = false;

    // ͨ������������Ȩ��/����ѡ��Ҫ��������� LootItem ������
    public List<(GameObject prefab, int amount)> RollDrops()
    {
        List<(GameObject, int)> results = new List<(GameObject, int)>();

        if (items == null || items.Count == 0) return results;

        // ���ȴ��� explicitChance ��ľ��Ը��ʵ��䣨ÿ�������
        List<int> availableIndices = new List<int>();
        float totalWeight = 0f;
        for (int i = 0; i < items.Count; i++)
        {
            var it = items[i];
            if (it.explicitChance >= 0f)
            {
                // �����ж��Ƿ���䣨�������� [0,1]��
                float r = Random.value;
                if (r <= it.explicitChance)
                {
                    int amt = Random.Range(it.minAmount, it.maxAmount + 1);
                    results.Add((it.prefab, amt));
                    if (!allowDuplicates) availableIndices.Add(i); // mark handled to possibly exclude from further selection
                }
            }
            // ����Ȩ�����ں�����Ȩ�����ѡ��
            if (it.weight > 0f)
            {
                totalWeight += it.weight;
                if (!availableIndices.Contains(i)) // mark as candidate
                    availableIndices.Add(i);
            }
        }

        // ��� maxDrops == 0 ��ʾ�����ƣ����Զ����а�Ȩ�ص������һ��ѡ�񣩣�����ѡ�� up to maxDrops ��
        if (totalWeight > 0f && maxDrops != 0)
        {
            int dropsToDo = Mathf.Max(1, maxDrops - results.Count);
            // ��Ȩ��ѡ�� dropsToDo ������ֱ��û�к�ѡ��
            List<int> candidateIndices = new List<int>();
            for (int i = 0; i < items.Count; i++)
            {
                // ֻ�� weight > 0 �Ž����ѡ
                if (items[i].weight > 0f) candidateIndices.Add(i);
            }

            for (int d = 0; d < dropsToDo && candidateIndices.Count > 0; d++)
            {
                // ���� subtotal weight
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
                    // �Ӻ�ѡ���Ƴ� chosenIdx
                    candidateIndices.Remove(chosenIdx);
                }
            }
        }
        else if (totalWeight > 0f && maxDrops == 0)
        {
            // maxDrops == 0: ��ÿ��Ȩ��>0����Ŀ��һ�ζ���Ȩ���ж������� = weight / totalWeight��
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
