using UnityEngine; // Unity 引擎命名空间

/// <summary>
/// 静电场：以角色为中心，持续时间内对范围内的敌方施加雷元素伤害，持续累加
/// 说明：该技能为持续性效果，施放后一段时间内，每帧对范围内的敌方造成伤害
///      伤害为 damagePerSecond * dt（冷却时长和持续时间独立
/// </summary>
public class SM_Lightning_StaticField : SM_BaseSkill
{
    [Header("静电场")]
    public float radius = 3f;              // 有效半径（以施放者为中心）
    public float damagePerSecond = 5f;     // 每秒伤害值（实际为 dt）
    public float duration = 6f;            // 持续时间（秒）
    public LayerMask enemyMask;            // 敌人图层（一般设为你的 Enemy 层）

    private float _timer = 0f;             // 已经持续时长
    private bool _active = false;          // 是否在持续阶段

    /// <summary>
    /// 施放静电场，进入「持续阶段」
    /// </summary>
    protected override bool DoCast()
    {
        _timer = 0f;       // 重置持续时间
        _active = true;    // 设为激活状态
        return true;       // 返回施放成功
    }

    /// <summary>
    /// 每帧更新，如果激活，对范围内的敌人按时间比例造成伤害
    /// </summary>
    public override void Tick(float dt)
    {
        base.Tick(dt);                     // 先更新冷却计时
        if (!_active) return;              // 未激活即返回

        _timer += dt;                      // 累计时间
        if (_timer >= duration)            // 持续达到最大时间
        {
            _active = false;               // 停止激活
            return;                        // 返回，等待冷却可再次施放
        }

        // 在半径范围内寻找敌人（碰撞体在 enemyMask 指定的层）
        var hits = Physics2D.OverlapCircleAll(character.AimOrigin.position, radius, enemyMask);
        // 对每个敌人施加每秒伤害 * dt，使得伤害随时间，形成平滑的持续伤害
        float tickDamage = damagePerSecond * dt; // 每帧伤害
        foreach (var h in hits)
        {
            var dmg = h.GetComponent<SM_IDamageable>(); // 获取目标的可伤害接口
            if (dmg != null)
            {
                dmg.ApplyDamage(new SM_DamageInfo
                {
                    Amount = tickDamage,                // 每帧伤害值
                    Element = SM_Element.Lightning,     // 雷元素
                    IgnoreDefense = false,              // 不忽略防御
                    CritChance = 0f,                    // 无暴击
                    CritMultiplier = 1f                 // 普通倍数
                });
            }
        }
    }

    /// <summary>
    /// 勾选选中时在 Scene 视图中绘制一个线框圆，用于可视化范围
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;                                // 使用黄色
        Gizmos.DrawWireSphere(transform.position, radius);          // 绘制线框圆
    }
}
