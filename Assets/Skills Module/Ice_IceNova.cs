using UnityEngine; // Unity 引擎命名空间

/// <summary>
/// 冰霜新星：以角色为中心，较大范围伤害并冻结周围敌人
/// </summary>
public class SM_Ice_IceNova : SM_BaseSkill
{
    [Header("冰霜新星")]
    public float radius = 3f;        // 半径
    public float damage = 10f;       // 伤害
    public float freezeTime = 1.2f;  // 冰冻时长
    public LayerMask enemyMask;      // 敌人图层

    protected override bool DoCast()
    {
        var hits = Physics2D.OverlapCircleAll(character.AimOrigin.position, radius, enemyMask); // 检查范围
        foreach (var h in hits)
        {
            var dmg = h.GetComponent<SM_IDamageable>();    // 可伤害接口
            if (dmg != null)                                // 如果可伤害
            {
                dmg.ApplyDamage(new SM_DamageInfo
                {
                    Amount = damage,            // 伤害
                    Element = SM_Element.Ice,   // 冰元素
                    IgnoreDefense = false,      // 不忽略防御
                    CritChance = 0f,            // 无暴击
                    CritMultiplier = 1f         // 倍率
                });
            }
            h.GetComponent<SM_FreezeEffect>()?.Freeze(freezeTime); // 施加冰冻（若有）
        }
        return true;
    }

    private void OnDrawGizmosSelected() // 编辑器范围可视化
    {
        Gizmos.color = Color.blue;      // 蓝
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
