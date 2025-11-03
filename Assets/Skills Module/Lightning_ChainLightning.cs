using UnityEngine;                // Unity 引擎命名空间
using System.Collections.Generic; // HashSet/List

/// <summary>
/// 连锁闪电：从第一目标开始跳跃至相邻目标，最多跳 N 次
/// </summary>
public class SM_Lightning_ChainLightning : SM_BaseSkill
{
    [Header("连锁闪")]
    public float firstRange = 6f;     // 第一目标搜寻范围
    public float jumpRange = 5f;      // 跳跃范围（第一处在该范围内）
    public int maxJumps = 4;          // 最多跳跃次数（实际命中数=跳+1）
    public float damage = 14f;        // 每次命中的伤害
    public LayerMask enemyMask;       // 敌人图层

    protected override bool DoCast()
    {
        var origin = (Vector2)character.AimOrigin.position;    // 发射原点
        var dir = character.AimDirection.normalized;           // 朝向方向

        // 1) 找到第一目标：要在 firstRange 内，且最接近前方方向较小角度
        Collider2D first = null;                               // 第一目标
        float bestDot = 0.5f;                                  // 最少点乘积阈值（>0.5 则角度 < ~60°）
        var cands = Physics2D.OverlapCircleAll(origin, firstRange, enemyMask); // 候选
        foreach (var c in cands)
        {
            var v = ((Vector2)c.transform.position - origin).normalized; // 指向敌方
            var d = Vector2.Dot(dir, v);                                 // 与前方方向点积
            if (d > bestDot) { bestDot = d; first = c; }                 // 选择更前方
        }
        if (first == null) return false;                                 // 找不到则失败，不重置冷却/消耗以允许重复尝试

        // 2) 跳连：每对 current 施伤害，然后在邻近找到下一个未访问目标
        var visited = new HashSet<Collider2D>();                         // 防止重复跳连
        var current = first;                                             // 当前目标
        for (int i = 0; i < maxJumps + 1 && current != null; i++)        // 实际次数=跳+1
        {
            if (visited.Contains(current)) break;                        // 访问过，退出
            visited.Add(current);                                        // 添加
            var dmg = current.GetComponent<SM_IDamageable>();            // 可伤害接口
            if (dmg != null)
            {
                dmg.ApplyDamage(new SM_DamageInfo
                {
                    Amount = damage,                 // 每次伤害
                    Element = SM_Element.Lightning,  // 雷元素
                    IgnoreDefense = false,           // 不忽略防御
                    CritChance = 0f,                 // 无暴击
                    CritMultiplier = 1f              // 倍率
                });
            }
            current = FindNextTarget(current.transform.position, visited); // 查找下一个
        }
        return true;                                                      // 成功
    }

    private Collider2D FindNextTarget(Vector2 from, HashSet<Collider2D> visited)
    {
        var targets = Physics2D.OverlapCircleAll(from, jumpRange, enemyMask); // 搜寻 jumpRange 内的目标
        float closest = float.MaxValue;                                       // 最近距离
        Collider2D best = null;                                               // 最佳目标
        foreach (var t in targets)
        {
            if (visited.Contains(t)) continue;                                // 已访问，跳过
            float dist = Vector2.Distance(from, t.transform.position);        // 距离
            if (dist < closest) { closest = dist; best = t; }                 // 取最近
        }
        return best;                                                          // 返回最近目标（可能为空）
    }
}
