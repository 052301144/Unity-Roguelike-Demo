using UnityEngine; // Unity 命名空间

/// <summary>
/// 静电场：以自身为中心持续对范围内的敌人造成雷元素伤害（按时间累计）
/// 说明：该技能为“持续效果”，在施放后的一段时间内，每帧对范围内的敌人叠加伤害，
///      伤害为 damagePerSecond * dt；冷却计时与持续计时彼此独立。
/// </summary>
public class SM_Lightning_StaticField : SM_BaseSkill
{
    [Header("静电场参数")]
    public float radius = 3f;              // 攻击半径（以施法者为圆心）
    public float damagePerSecond = 5f;     // 每秒总伤害（会乘以 dt）
    public float duration = 6f;            // 持续时间（秒）
    public LayerMask enemyMask;            // 敌人图层（建议设置为你们的 Enemy 层）

    private float _timer = 0f;             // 技能已持续时间
    private bool _active = false;          // 是否处在持续阶段

    /// <summary>
    /// 施放静电场：进入“持续阶段”
    /// </summary>
    protected override bool DoCast()
    {
        _timer = 0f;       // 重置持续计时
        _active = true;    // 标记为激活状态
        return true;       // 返回施放成功
    }

    /// <summary>
    /// 每帧更新：若激活中，则对范围内敌人造成按时间增量的伤害
    /// </summary>
    public override void Tick(float dt)
    {
        base.Tick(dt);                     // 处理冷却计时
        if (!_active) return;              // 未激活即返回

        _timer += dt;                      // 持续时间累计
        if (_timer >= duration)            // 若超过持续时间
        {
            _active = false;               // 结束持续
            return;                        // 返回（等待冷却结束即可再次施放）
        }

        // 在半径范围内寻找敌人（碰撞体需在 enemyMask 指定的层）
        var hits = Physics2D.OverlapCircleAll(character.AimOrigin.position, radius, enemyMask);
        // 对每个敌人按“每秒伤害 * dt”结算伤害，形成平滑的持续伤害
        float tickDamage = damagePerSecond * dt; // 本帧伤害
        foreach (var h in hits)
        {
            var dmg = h.GetComponent<SM_IDamageable>(); // 获取“可受伤接口”
            if (dmg != null)
            {
                dmg.ApplyDamage(new SM_DamageInfo
                {
                    Amount = tickDamage,                // 本帧伤害值
                    Element = SM_Element.Lightning,     // 雷元素
                    IgnoreDefense = false,              // 不无视防御
                    CritChance = 0f,                    // 无暴击
                    CritMultiplier = 1f                 // 普通倍率
                });
            }
        }
    }

    /// <summary>
    /// （可选）在 Scene 视图中绘制一个线框圆，帮助你可视化范围
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;                                // 使用黄色
        Gizmos.DrawWireSphere(transform.position, radius);          // 绘制线框圆
    }
}