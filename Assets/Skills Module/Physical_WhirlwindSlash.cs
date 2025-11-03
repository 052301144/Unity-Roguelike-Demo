using UnityEngine; // Unity 引擎命名空间

/// <summary>
/// 旋风斩：原位置旋转，造成伤害；物理=附加防御，暴击
/// </summary>
public class SM_Physical_WhirlwindSlash : SM_BaseSkill
{
    [Header("旋风斩")]
    public float radius = 2.5f;      // 有效半径
    public int ticks = 5;            // 伤害次数
    public float totalTime = 0.8f;   // 完成旋转所需时间
    public float damagePerTick = 8f; // 每次伤害
    public LayerMask enemyMask;      // 敌人图层

    private float _tk;               // 计时器
    private int _done;               // 已完成次数
    private bool _active;            // 是否旋转

    protected override bool DoCast() // 开始施放
    {
        _active = true;              // 开始旋转
        _tk = 0f;                    // 重置计时
        _done = 0;                   // 重置次数
        return true;                 // 成功
    }

    public override void Tick(float dt) // 重写 Tick 以实现持续伤害
    {
        base.Tick(dt);               // 先更新冷却计时
        if (!_active) return;        // 未激活直接返回

        _tk += dt;                   // 累计时间
        float interval = totalTime / Mathf.Max(1, ticks); // 每次间隔
        if (_done < ticks && _tk >= (_done + 1) * interval) // 到达下一次触发
        {
            _done++;                  // 次数+1
            // 以角色为圆心进行范围判定
            var hits = Physics2D.OverlapCircleAll(character.AimOrigin.position, radius, enemyMask);
            foreach (var h in hits)   // 遍历所有敌人
            {
                var dmg = h.GetComponent<SM_IDamageable>(); // 获取可伤害接口
                if (dmg != null)       // 如果可伤害
                {
                    dmg.ApplyDamage(new SM_DamageInfo
                    {
                        Amount = damagePerTick,            // 每次伤害
                        Element = SM_Element.Physical,     // 物理元素
                        IgnoreDefense = true,              // 忽略防
                        CritChance = 0.1f,                 // 暴击
                        CritMultiplier = 1.5f              // 暴击倍数
                    });
                }
            }
        }
        if (_tk >= totalTime) _active = false; // 结束
    }

    private void OnDrawGizmosSelected() // 编辑器范围可视化
    {
        Gizmos.color = Color.red;       // 红线框
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
