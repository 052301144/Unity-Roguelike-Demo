using UnityEngine; // Unity 引擎命名空间

/// <summary>
/// 火焰锥：向前方喷射扇面范围，造成伤害并高概率获得燃尽
/// 并非投射物，直接检查扇面范围伤害
/// </summary>
public class SM_Fire_FlameCone : SM_BaseSkill
{
    [Header("火焰锥")]
    public float range = 4f;           // 扇面半径
    public float angle = 45f;          // 扇角（总角度=2*扇角）
    public float damage = 15f;         // 直接伤害
    public float burnDPS = 6f;         // 燃尽每秒伤害
    public float burnTime = 5f;        // 燃尽持续时间
    public float burnChance = 0.8f;    // 燃尽概率（高）
    public LayerMask enemyMask;        // 敌人图层

    protected override bool DoCast()
    {
        var origin = (Vector2)character.AimOrigin.position;            // 发射原点
        var dir = character.AimDirection.normalized;                   // 方向
        var hits = Physics2D.OverlapCircleAll(origin, range, enemyMask);// 圆圈内候选
        foreach (var h in hits)                                        // 遍历
        {
            var to = (Vector2)h.transform.position - origin;           // 指向敌方
            if (Vector2.Angle(dir, to) <= angle)                       // 判断是否在扇面内
            {
                var dmg = h.GetComponent<SM_IDamageable>();            // 可伤害接口
                if (dmg != null)
                {
                    dmg.ApplyDamage(new SM_DamageInfo
                    {
                        Amount = damage,            // 伤害
                        Element = SM_Element.Fire,  // 火元素
                        IgnoreDefense = false,      // 不忽略防御
                        CritChance = 0f,            // 无暴击
                        CritMultiplier = 1f         // 倍率
                    });
                }
                if (Random.value < burnChance)                          // 判断得燃尽
                    h.GetComponent<SM_BurnEffect>()?.Apply(burnDPS, burnTime); // 施加燃尽（若目标有该效果）
            }
        }
        return true;                                                    // 成功
    }

    private void OnDrawGizmosSelected()                                 // 编辑器可视化
    {
        var o = transform.position;                                     // 原点
        var dir = Vector2.right;                                        // 显示默认
        Gizmos.color = new Color(1, 0.5f, 0, 1);                        // 橙
        var left = Quaternion.Euler(0, 0, -angle) * dir * range;        // 左边界
        var right = Quaternion.Euler(0, 0, angle) * dir * range;        // 右边界
        Gizmos.DrawLine(o, o + (Vector3)left);                          // 左线
        Gizmos.DrawLine(o, o + (Vector3)right);                         // 右线
    }
}
