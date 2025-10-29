using UnityEngine;
using System.Collections.Generic;

public class EnemyDrop : MonoBehaviour
{
    [Header("Loot")]
    public LootTable lootTable;

    [Tooltip("掉落时的父物体（可选），为空则直接放到场景根节点")]
    public Transform dropParent;

    [Tooltip("掉落生成时的 z 坐标来源：UseEnemyZ = 使用敌人当前 z；UseFixedZ = 使用 fixedZValue")]
    public bool useEnemyZ = true;
    public float fixedZValue = 0f;

    [Header("其他")]
    [Tooltip("是否在敌人死亡时自动执行 Drop (如果你想手动调用 DropAtPosition(false) 可以取消勾选)")]
    public bool autoDropOnDeath = true;

    // 这个方法在敌人“死亡”时被调用（你可以在你的敌人血量脚本里调用）
    public void Drop()
    {
        Vector3 spawnPos = transform.position;
        if (!useEnemyZ)
            spawnPos.z = fixedZValue;

        DoDropAtPosition(spawnPos);
    }

    // 提供一个外部可调用的接口：在任意位置做掉落（但你要求 x,y = 敌人位置，这里默认使用传入位置）
    public void DropAtPosition(Vector2 xyPosition)
    {
        float z = useEnemyZ ? transform.position.z : fixedZValue;
        Vector3 spawnPos = new Vector3(xyPosition.x, xyPosition.y, z);
        DoDropAtPosition(spawnPos);
    }

    // 内部实现
    private void DoDropAtPosition(Vector3 spawnPos)
    {
        if (lootTable == null)
        {
            Debug.LogWarning($"[{name}] 没有指定 LootTable，无法掉落。");
            return;
        }

        List<(GameObject prefab, int amount)> drops = lootTable.RollDrops();

        if (drops == null || drops.Count == 0)
        {
            // 未掉落任何物品（可视为无掉落）
            return;
        }

        foreach (var drop in drops)
        {
            if (drop.prefab == null) continue;

            for (int i = 0; i < drop.amount; i++)
            {
                // 坐标的 x, y 与敌人位置相同；如果你需要做小范围抖动（比如微偏移），可以在这里增加偏移量。
                Vector3 pos = new Vector3(spawnPos.x, spawnPos.y, spawnPos.z);

                GameObject go = Instantiate(drop.prefab, pos, Quaternion.identity, dropParent);
                // 可根据需要做额外初始化，例如设置拾取 ID、随机旋转、添加生命时长等
            }
        }
    }

    // 供外部（例如敌人血量脚本）调用：当敌人死亡时触发
    // 示例：GetComponent<EnemyDrop>().OnDeath(); 或者在你当前的死亡逻辑里直接调用 Drop()
    public void OnDeath()
    {
        if (autoDropOnDeath) Drop();
    }
}
