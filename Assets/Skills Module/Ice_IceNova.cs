using UnityEngine; // Unity 命名空间

/// <summary>
/// 冰环：以自身为中心，环形范围伤害并冻结周围敌人
/// </summary>
public class SM_Ice_IceNova : SM_BaseSkill
{
    [Header("冰环参数")]
    public float radius = 3f;        // 半径
    public float damage = 10f;       // 伤害
    public float freezeTime = 1.2f;  // 冻结时间
    public LayerMask enemyMask;      // 敌人图层

    protected override bool DoCast()
    {
        var hits = Physics2D.OverlapCircleAll(character.AimOrigin.position, radius, enemyMask); // 搜索周围
        foreach (var h in hits)
        {
            var dmg = h.GetComponent<SM_IDamageable>();    // 受伤接口
            if (dmg != null)                                // 可受伤则伤害
            {
                dmg.ApplyDamage(new SM_DamageInfo
                {
                    Amount = damage,            // 伤害
                    Element = SM_Element.Ice,   // 冰元素
                    IgnoreDefense = false,      // 不无视防御
                    CritChance = 0f,            // 无暴击
                    CritMultiplier = 1f         // 倍率
                });
            }
            h.GetComponent<SM_FreezeEffect>()?.Freeze(freezeTime); // 施加冻结（若有组件）
        }
        return true;
    }

    private void OnDrawGizmosSelected() // 编辑器可视化
    {
        Gizmos.color = Color.blue;      // 蓝色
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}