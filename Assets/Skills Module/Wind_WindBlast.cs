using UnityEngine; // Unity 命名空间

/// <summary>
/// 风压炮：直线宽盒判定，对路径敌人造成伤害并击退
/// 不产生投射物，直接用 OverlapBoxAll
/// </summary>
public class SM_Wind_WindBlast : SM_BaseSkill
{
    [Header("风压炮参数")]
    public float width = 0.7f;        // 直线宽度
    public float length = 6f;         // 直线长度
    public float damage = 12f;        // 伤害
    public float knockbackForce = 12f;// 击退力
    public float knockbackTime = 0.15f;// 击退时间
    public LayerMask enemyMask;       // 敌人图层

    protected override bool DoCast()
    {
        var o = (Vector2)character.AimOrigin.position;                  // 起点
        var d = character.AimDirection.normalized;                      // 方向
        var center = o + d * (length * 0.5f);                           // 盒中心
        float angleZ = Vector2.SignedAngle(Vector2.right, d);           // 旋转角
        var hits = Physics2D.OverlapBoxAll(center, new Vector2(length, width), angleZ, enemyMask); // 盒体检测

        foreach (var h in hits)
        {
            var dmg = h.GetComponent<SM_IDamageable>();                 // 受伤接口
            if (dmg != null)
            {
                dmg.ApplyDamage(new SM_DamageInfo
                {
                    Amount = damage,             // 伤害
                    Element = SM_Element.Wind,   // 风元素
                    IgnoreDefense = false,       // 不无视防御
                    CritChance = 0f,             // 无暴击
                    CritMultiplier = 1f          // 倍率
                });
            }
            h.GetComponent<SM_IKnockbackable>()?.Knockback(d, knockbackForce, knockbackTime); // 若可击退则击退
        }
        return true;                                                    // 成功
    }

    private void OnDrawGizmosSelected()                                 // 编辑器可视化
    {
        Gizmos.color = Color.cyan;                                      // 青色
        Gizmos.DrawWireCube(transform.position + Vector3.right * (length * 0.5f), new Vector3(length, width, 0.1f));
    }
}