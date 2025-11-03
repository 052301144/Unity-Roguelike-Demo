using UnityEngine;
using System.Collections.Generic;

public class EnemyDrop : MonoBehaviour
{
    [Header("Loot")]
    public LootTable lootTable;

    [Tooltip("掉落时的父对象（可选，留空则直接放到场景根目录）")]
    public Transform dropParent;

    [Tooltip("掉落位置时的 z 坐标来源。UseEnemyZ = 使用敌人的当前 z，UseFixedZ = 使用 fixedZValue")]
    public bool useEnemyZ = true;
    public float fixedZValue = 0f;

    [Header("自动")]
    [Tooltip("是否在敌人死亡时自动执行 Drop（如果不勾选，需要手动调用 DropAtPosition(false) 或使用其他触发方式）")]
    public bool autoDropOnDeath = true;

    // 简单接口：在敌人死亡时调用，通常在敌人生命值脚本中设置
    public void Drop()
    {
        Vector3 spawnPos = transform.position;
        if (!useEnemyZ)
            spawnPos.z = fixedZValue;

        DoDropAtPosition(spawnPos);
    }

    // 提供一个外部可调用的接口，指定位置进行掉落（如果需要指定 x,y = 某个位置，否则默认使用敌人位置）
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
            // 未掉落任何物品（视为无掉落）
            return;
        }

        foreach (var drop in drops)
        {
            if (drop.prefab == null) continue;

            for (int i = 0; i < drop.amount; i++)
            {
                // 如果需要 x, y 的掉落位置相同，如果需要可以添加小范围随机偏移，这里先保持简单，统一使用相同位置
                Vector3 pos = new Vector3(spawnPos.x, spawnPos.y, spawnPos.z);

                GameObject go = Instantiate(drop.prefab, pos, Quaternion.identity, dropParent);
                // 可根据需要初始化掉落物品（如添加拾取 ID、设置旋转角度、添加掉落时间等）
            }
        }
    }

    // 外部调用接口，通常由生命值脚本调用，在死亡时调用
    // 示例：GetComponent<EnemyDrop>().OnDeath(); 或者根据你当前的死亡逻辑，直接调用 Drop()
    public void OnDeath()
    {
        if (autoDropOnDeath) Drop();
    }
}
