using UnityEngine; // Unity 命名空间

/// <summary>
/// 龙卷风体：缓慢前进，持续对触碰的敌人造成风元素伤害并击退
/// 用法：做预制体（大触发器），挂此脚本；技能实例化后 Launch()。
/// </summary>
[RequireComponent(typeof(Collider2D))] // 需要触发器
public class SM_Tornado : MonoBehaviour
{
    public float speed = 2f;          // 前进速度
    public float lifetime = 6f;       // 存活时间
    public float tickDamage = 5f;     // 每次触发伤害
    public float tickInterval = 0.5f; // 触发间隔（秒）
    public float knockback = 8f;      // 击退力度

    private float _t;                 // 生存计时
    private float _tk;                // 间隔计时
    private Vector2 _dir;             // 前进方向

    public void Launch(Vector2 dir)   // 发射（技能调用）
    {
        _dir = dir.normalized;        // 记录方向
    }

    private void Update()
    {
        transform.position += (Vector3)(_dir * speed * Time.deltaTime); // 移动
        _t += Time.deltaTime;                                           // 生存计时
        _tk += Time.deltaTime;                                          // tick 计时
        if (_t >= lifetime) Destroy(gameObject);                        // 超时销毁
    }

    private void OnTriggerStay2D(Collider2D other) // 持续接触触发
    {
        if (_tk < tickInterval) return;                                // 未到触发间隔不结算
        var dmg = other.GetComponent<SM_IDamageable>();                 // 获取受伤接口
        if (dmg != null)                                                // 若可受伤
        {
            _tk = 0f;                                                   // 重置间隔
            dmg.ApplyDamage(new SM_DamageInfo                           // 造成伤害
            {
                Amount = tickDamage,                                    // 数值
                Element = SM_Element.Wind,                              // 风元素
                IgnoreDefense = false,                                  // 不无视防御
                CritChance = 0f,                                        // 无暴击
                CritMultiplier = 1f                                     // 倍率
            });

            // 击退（若实现了击退接口）
            var kb = other.GetComponent<SM_IKnockbackable>();
            if (kb != null) kb.Knockback(_dir, knockback, 0.1f);
        }
    }
}