using UnityEngine; // Unity 命名空间

/// <summary>
/// 火焰冲击：向前方扇形喷出火焰，造成伤害并高概率点燃
/// 不产生投射物，直接用扇形范围检测
/// </summary>
public class SM_Fire_FlameCone : SM_BaseSkill
{
    [Header("火焰冲击参数")]
    public float range = 4f;           // 扇形半径
    public float angle = 45f;          // 半角（总角度=2*半角）
    public float damage = 15f;         // 直接伤害
    public float burnDPS = 6f;         // 燃烧每秒伤害
    public float burnTime = 5f;        // 燃烧持续时间
    public float burnChance = 0.8f;    // 点燃概率（高）
    public LayerMask enemyMask;        // 敌人图层

    protected override bool DoCast()
    {
        var origin = (Vector2)character.AimOrigin.position;            // 释放原点
        var dir = character.AimDirection.normalized;                   // 朝向
        var hits = Physics2D.OverlapCircleAll(origin, range, enemyMask);// 圆域内候选
        foreach (var h in hits)                                        // 遍历
        {
            var to = (Vector2)h.transform.position - origin;           // 指向向量
            if (Vector2.Angle(dir, to) <= angle)                       // 判断是否在扇形内
            {
                var dmg = h.GetComponent<SM_IDamageable>();            // 受伤接口
                if (dmg != null)
                {
                    dmg.ApplyDamage(new SM_DamageInfo
                    {
                        Amount = damage,            // 伤害
                        Element = SM_Element.Fire,  // 火元素
                        IgnoreDefense = false,      // 不无视防御
                        CritChance = 0f,            // 无暴击
                        CritMultiplier = 1f         // 倍率
                    });
                }
                if (Random.value < burnChance)                          // 判断点燃
                    h.GetComponent<SM_BurnEffect>()?.Apply(burnDPS, burnTime); // 施加燃烧（若目标有该组件）
            }
        }
        return true;                                                    // 成功
    }

    private void OnDrawGizmosSelected()                                 // 编辑器可视化
    {
        var o = transform.position;                                     // 原点
        var dir = Vector2.right;                                        // 显示默认右
        Gizmos.color = new Color(1, 0.5f, 0, 1);                        // 橙色
        var left = Quaternion.Euler(0, 0, -angle) * dir * range;        // 左边界
        var right = Quaternion.Euler(0, 0, angle) * dir * range;        // 右边界
        Gizmos.DrawLine(o, o + (Vector3)left);                          // 画线
        Gizmos.DrawLine(o, o + (Vector3)right);                         // 画线
    }
}