using UnityEngine; // Unity 引擎命名空间

/// <summary>
/// 龙卷风体：向前方发射小型龙卷，对触碰到它的敌方施加风元素伤害并击退
/// 用法：准备一个预制体（触发一个碰撞器），并此脚本附加，子类需实例化及调用 Launch()
/// </summary>
[RequireComponent(typeof(Collider2D))] // 需要碰撞器
public class SM_Tornado : MonoBehaviour
{
    public float speed = 2f;          // 前进速度
    public float lifetime = 6f;       // 存活时长
    public float tickDamage = 5f;     // 每次伤害
    public float tickInterval = 0.5f; // 多少秒
    public float knockback = 8f;      // 击退力度

    private float _t;                 // 存活时长
    private float _tk;                // 伤害时长
    private Vector2 _dir;             // 前进方向

    public void Launch(Vector2 dir)   // 发射（由技能调用）
    {
        _dir = dir.normalized;        // 记录方向
    }

    private void Update()
    {
        transform.position += (Vector3)(_dir * speed * Time.deltaTime); // 移动
        _t += Time.deltaTime;                                           // 存活时长
        _tk += Time.deltaTime;                                          // tick 时长
        if (_t >= lifetime) Destroy(gameObject);                        // 超时销毁
    }

    private void OnTriggerStay2D(Collider2D other) // 持续接触触发
    {
        if (_tk < tickInterval) return;                                // 未到触发周期
        var dmg = other.GetComponent<SM_IDamageable>();                 // 获取可伤害接口
        if (dmg != null)                                                // 如果可以
        {
            _tk = 0f;                                                   // 重置周期
            dmg.ApplyDamage(new SM_DamageInfo                           // 施加伤害
            {
                Amount = tickDamage,                                    // 伤害值
                Element = SM_Element.Wind,                              // 风元素
                IgnoreDefense = false,                                  // 不忽略防
                CritChance = 0f,                                        // 无暴击
                CritMultiplier = 1f                                     // 倍率
            });

            // 击退（实现击退接口）
            var kb = other.GetComponent<SM_IKnockbackable>();
            if (kb != null) kb.Knockback(_dir, knockback, 0.1f);
        }
    }
}
