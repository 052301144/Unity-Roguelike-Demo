using UnityEngine; // Unity 命名空间

/// <summary>
/// 旋风斩：原地环形多段物理伤害；物理=无视防御，低暴击
/// </summary>
public class SM_Physical_WhirlwindSlash : SM_BaseSkill
{
    [Header("旋风斩参数")]
    public float radius = 2.5f;      // 作用半径
    public int ticks = 5;            // 伤害段数
    public float totalTime = 0.8f;   // 整个旋转持续时间
    public float damagePerTick = 8f; // 每段伤害
    public LayerMask enemyMask;      // 敌人图层

    private float _tk;               // 计时器
    private int _done;               // 已触发段数
    private bool _active;            // 是否进行中

    protected override bool DoCast() // 开始施放
    {
        _active = true;              // 激活旋风
        _tk = 0f;                    // 重置时间
        _done = 0;                   // 重置段数
        return true;                 // 成功
    }

    public override void Tick(float dt) // 覆盖父类 Tick 以实现持续多段伤害
    {
        base.Tick(dt);               // 先处理冷却倒计时
        if (!_active) return;        // 未激活直接返回

        _tk += dt;                   // 递增计时
        float interval = totalTime / Mathf.Max(1, ticks); // 每段间隔
        if (_done < ticks && _tk >= (_done + 1) * interval) // 到达下一段触发点
        {
            _done++;                  // 段数+1
            // 以角色为圆心搜索敌人
            var hits = Physics2D.OverlapCircleAll(character.AimOrigin.position, radius, enemyMask);
            foreach (var h in hits)   // 遍历命中的敌人
            {
                var dmg = h.GetComponent<SM_IDamageable>(); // 获取受伤接口
                if (dmg != null)       // 可受伤则造成伤害
                {
                    dmg.ApplyDamage(new SM_DamageInfo
                    {
                        Amount = damagePerTick,            // 每段伤害
                        Element = SM_Element.Physical,     // 物理元素
                        IgnoreDefense = true,              // 无视防御
                        CritChance = 0.1f,                 // 低暴击
                        CritMultiplier = 1.5f              // 暴击倍数
                    });
                }
            }
        }
        if (_tk >= totalTime) _active = false; // 结束
    }

    private void OnDrawGizmosSelected() // 编辑器范围可视化
    {
        Gizmos.color = Color.red;       // 红色线框
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}