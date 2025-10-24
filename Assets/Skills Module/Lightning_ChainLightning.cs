using UnityEngine;                // Unity 命名空间
using System.Collections.Generic; // HashSet/List

/// <summary>
/// 闪电链：从第一个前方目标开始，向近邻目标跳跃 N 次
/// </summary>
public class SM_Lightning_ChainLightning : SM_BaseSkill
{
    [Header("闪电链参数")]
    public float firstRange = 6f;     // 第一个目标搜索范围
    public float jumpRange = 5f;      // 跳跃范围（下一跳在该范围内）
    public int maxJumps = 4;          // 最大跳跃次数（命中目标数=跳数+1）
    public float damage = 14f;        // 每次命中的伤害
    public LayerMask enemyMask;       // 敌人图层

    protected override bool DoCast()
    {
        var origin = (Vector2)character.AimOrigin.position;    // 释放原点
        var dir = character.AimDirection.normalized;           // 面朝方向

        // 1) 找第一个目标：要求在 firstRange 内、且尽量和前方方向夹角小
        Collider2D first = null;                               // 第一个目标
        float bestDot = 0.5f;                                  // 至少朝向度阈值（>0.5 即夹角 < ~60°）
        var cands = Physics2D.OverlapCircleAll(origin, firstRange, enemyMask); // 候选
        foreach (var c in cands)
        {
            var v = ((Vector2)c.transform.position - origin).normalized; // 指向方向
            var d = Vector2.Dot(dir, v);                                 // 与前方夹角余弦
            if (d > bestDot) { bestDot = d; first = c; }                 // 选更靠前的
        }
        if (first == null) return false;                                 // 找不到则失败（不耗冷却/不再扣蓝，因为已扣蓝）

        // 2) 逐跳：每次对 current 造成伤害，然后从其附近找下一个未访问目标
        var visited = new HashSet<Collider2D>();                         // 避免重复命中
        var current = first;                                             // 当前目标
        for (int i = 0; i < maxJumps + 1 && current != null; i++)        // 命中次数=跳数+1
        {
            if (visited.Contains(current)) break;                        // 已命中则退出
            visited.Add(current);                                        // 标记命中
            var dmg = current.GetComponent<SM_IDamageable>();            // 受伤接口
            if (dmg != null)
            {
                dmg.ApplyDamage(new SM_DamageInfo
                {
                    Amount = damage,                 // 每跳伤害
                    Element = SM_Element.Lightning,  // 雷元素
                    IgnoreDefense = false,           // 不无视防御
                    CritChance = 0f,                 // 无暴击
                    CritMultiplier = 1f              // 倍率
                });
            }
            current = FindNextTarget(current.transform.position, visited); // 查找下一跳
        }
        return true;                                                      // 成功
    }

    private Collider2D FindNextTarget(Vector2 from, HashSet<Collider2D> visited)
    {
        var targets = Physics2D.OverlapCircleAll(from, jumpRange, enemyMask); // 寻找 jumpRange 内的目标
        float closest = float.MaxValue;                                       // 最近距离
        Collider2D best = null;                                               // 最佳目标
        foreach (var t in targets)
        {
            if (visited.Contains(t)) continue;                                // 跳过已命中目标
            float dist = Vector2.Distance(from, t.transform.position);        // 距离
            if (dist < closest) { closest = dist; best = t; }                 // 取最近
        }
        return best;                                                          // 返回最近目标（可能为空）
    }
}