using UnityEngine;
using System.Collections.Generic;

public class EnemyDrop : MonoBehaviour
{
    [Header("Loot")]
    public LootTable lootTable;

    [Tooltip("����ʱ�ĸ����壨��ѡ����Ϊ����ֱ�ӷŵ��������ڵ�")]
    public Transform dropParent;

    [Tooltip("��������ʱ�� z ������Դ��UseEnemyZ = ʹ�õ��˵�ǰ z��UseFixedZ = ʹ�� fixedZValue")]
    public bool useEnemyZ = true;
    public float fixedZValue = 0f;

    [Header("����")]
    [Tooltip("�Ƿ��ڵ�������ʱ�Զ�ִ�� Drop (��������ֶ����� DropAtPosition(false) ����ȡ����ѡ)")]
    public bool autoDropOnDeath = true;

    // ��������ڵ��ˡ�������ʱ�����ã����������ĵ���Ѫ���ű�����ã�
    public void Drop()
    {
        Vector3 spawnPos = transform.position;
        if (!useEnemyZ)
            spawnPos.z = fixedZValue;

        DoDropAtPosition(spawnPos);
    }

    // �ṩһ���ⲿ�ɵ��õĽӿڣ�������λ�������䣨����Ҫ�� x,y = ����λ�ã�����Ĭ��ʹ�ô���λ�ã�
    public void DropAtPosition(Vector2 xyPosition)
    {
        float z = useEnemyZ ? transform.position.z : fixedZValue;
        Vector3 spawnPos = new Vector3(xyPosition.x, xyPosition.y, z);
        DoDropAtPosition(spawnPos);
    }

    // �ڲ�ʵ��
    private void DoDropAtPosition(Vector3 spawnPos)
    {
        if (lootTable == null)
        {
            Debug.LogWarning($"[{name}] û��ָ�� LootTable���޷����䡣");
            return;
        }

        List<(GameObject prefab, int amount)> drops = lootTable.RollDrops();

        if (drops == null || drops.Count == 0)
        {
            // δ�����κ���Ʒ������Ϊ�޵��䣩
            return;
        }

        foreach (var drop in drops)
        {
            if (drop.prefab == null) continue;

            for (int i = 0; i < drop.amount; i++)
            {
                // ����� x, y �����λ����ͬ���������Ҫ��С��Χ����������΢ƫ�ƣ�����������������ƫ������
                Vector3 pos = new Vector3(spawnPos.x, spawnPos.y, spawnPos.z);

                GameObject go = Instantiate(drop.prefab, pos, Quaternion.identity, dropParent);
                // �ɸ�����Ҫ�������ʼ������������ʰȡ ID�������ת���������ʱ����
            }
        }
    }

    // ���ⲿ���������Ѫ���ű������ã�����������ʱ����
    // ʾ����GetComponent<EnemyDrop>().OnDeath(); �������㵱ǰ�������߼���ֱ�ӵ��� Drop()
    public void OnDeath()
    {
        if (autoDropOnDeath) Drop();
    }
}
