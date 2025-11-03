using UnityEngine; // Unity 引擎命名空间

/// <summary>
/// 风压炮：直线扇形范围，对路径上的敌方造成伤害并击退
/// 并非投射物，直接使用 OverlapBoxAll
/// </summary>
public class SM_Wind_WindBlast : SM_BaseSkill
{
    [Header("风压炮")]
    public float width = 0.7f;        // 直线扇形
    public float length = 6f;         // 直线长度
    public float damage = 12f;        // 伤害
    public float knockbackForce = 12f;// 击退力
    public float knockbackTime = 0.15f;// 击退时长
    public LayerMask enemyMask;       // 敌人图层

    protected override bool DoCast()
    {
        var o = (Vector2)character.AimOrigin.position;                  // 起点
        var d = character.AimDirection.normalized;                      // 方向
        var center = o + d * (length * 0.5f);                           // 盒子中点
        float angleZ = Vector2.SignedAngle(Vector2.right, d);           // 旋转角
        var hits = Physics2D.OverlapBoxAll(center, new Vector2(length, width), angleZ, enemyMask); // 判定

        foreach (var h in hits)
        {
            var dmg = h.GetComponent<SM_IDamageable>();                 // 可伤害接口
            if (dmg != null)
            {
                dmg.ApplyDamage(new SM_DamageInfo
                {
                    Amount = damage,             // 伤害
                    Element = SM_Element.Wind,   // 风元素
                    IgnoreDefense = false,       // 不忽略防御
                    CritChance = 0f,             // 无暴击
                    CritMultiplier = 1f          // 倍率
                });
            }
            h.GetComponent<SM_IKnockbackable>()?.Knockback(d, knockbackForce, knockbackTime); // 尝试击退（若目标有此接口）
        }
        return true;                                                    // 成功
    }

    private void OnDrawGizmosSelected()                                 // 编辑器可视化
    {
        Gizmos.color = Color.cyan;                                      // 青
        Gizmos.DrawWireCube(transform.position + Vector3.right * (length * 0.5f), new Vector3(length, width, 0.1f));
    }
}
