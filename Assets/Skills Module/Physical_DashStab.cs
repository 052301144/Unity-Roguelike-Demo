using UnityEngine; // Unity 命名空间

/// <summary>
/// 突刺：向前短距离快速位移，对路径上的敌人造成物理伤害
/// 为了不干扰你们的移动控制，这里直接修改 transform.position（极短时间）
/// </summary>
public class SM_Physical_DashStab : SM_BaseSkill
{
    [Header("突刺参数")]
    public float dashDistance = 4f;    // 位移距离
    public float dashTime = 0.15f;     // 位移耗时
    public float damage = 20f;         // 伤害数值
    public LayerMask enemyMask;        // 敌人图层

    private float _timer;              // 计时
    private Vector2 _start;            // 起点
    private Vector2 _end;              // 终点
    private bool _dashing;             // 是否突刺中

    protected override bool DoCast()
    {
        _start = character.AimOrigin.position;                          // 记录起点
        _end = _start + character.AimDirection.normalized * dashDistance; // 计算终点
        _timer = 0f;                                                    // 重置计时
        _dashing = true;                                                // 开始突刺
        return true;                                                    // 成功
    }

    public override void Tick(float dt)
    {
        base.Tick(dt);                                                  // 冷却计时
        if (!_dashing) return;                                          // 非突刺中
        _timer += dt;                                                   // 更新时间
        float t = Mathf.Clamp01(_timer / dashTime);                     // 归一化进度
        var pos = Vector2.Lerp(_start, _end, t);                        // 插值位置
        transform.position = pos;                                       // 直接设置位置（轻量做法）

        // 在当前位置的一个小圆内检测敌人（模拟“路径伤害”）
        var hits = Physics2D.OverlapCircleAll(pos, 0.6f, enemyMask);
        foreach (var h in hits)
        {
            var dmg = h.GetComponent<SM_IDamageable>();                 // 受伤接口
            if (dmg != null)
            {
                dmg.ApplyDamage(new SM_DamageInfo
                {
                    Amount = damage,                                    // 伤害
                    Element = SM_Element.Physical,                      // 物理
                    IgnoreDefense = true,                               // 无视防御
                    CritChance = 0.1f,                                  // 低暴击
                    CritMultiplier = 1.5f                               // 暴击倍数
                });
            }
        }

        if (t >= 1f) _dashing = false;                                  // 结束突刺
    }
}