using UnityEngine; // Unity 引擎命名空间

/// <summary>
/// 通用投射物：用于火球/冰锥/雷击等一次性投射
/// 用法：准备一个预制体（含 SpriteRenderer + Collider2D isTrigger），并此脚本附加。子类需实例化及调用 Launch()
/// </summary>
[RequireComponent(typeof(Collider2D))]      // 需要碰撞器
public class SM_Projectile : MonoBehaviour
{
    [Header("基础属性")]
    public float speed = 10f;               // 飞行速度
    public float lifetime = 3f;             // 存活时长（到期销毁）
    public float damage = 10f;              // 投射伤害
    public SM_Element element = SM_Element.Physical; // 元素类型

    [Header("风元素（可选）")]
    public float knockbackForce = 0f;       // 击退力度
    public float knockbackTime = 0.1f;      // 击退持续时间

    [Header("火元素（可选）")]
    public float burnDPS = 0f;              // 燃尽每秒伤害
    public float burnTime = 0f;             // 燃尽持续时间

    [Header("冰元素（可选）")]
    public float freezeChance = 0f;         // 冰冻概率（0~1）
    public float freezeTime = 0f;           // 冰冻时长

    private Vector2 _dir;                   // 飞行方向
    private float _t;                       // 存活计时

    public void Launch(Vector2 dir)         // 发射（由技能调用）
    {
        _dir = dir.normalized;              // 记录归一化方向
    }

    private void Update()
    {
        transform.position += (Vector3)(_dir * speed * Time.deltaTime); // 每帧位移
        _t += Time.deltaTime;                                            // 计时
        if (_t >= lifetime) Destroy(gameObject);                         // 超时销毁
    }

    private void OnTriggerEnter2D(Collider2D other) // 投射击中
    {
        var dmg = other.GetComponent<SM_IDamageable>();  // 取得可伤害接口
        if (dmg != null)                                 // 如果可伤害
        {
            // 伤害构造（默认低暴击，附加防御）
            dmg.ApplyDamage(new SM_DamageInfo
            {
                Amount = damage,                         // 伤害
                Element = element,                       // 元素
                IgnoreDefense = (element == SM_Element.Physical),     // 物理忽略防
                CritChance = (element == SM_Element.Physical ? 0.1f : 0f), // 仅有暴击
                CritMultiplier = 1.5f                    // 暴击倍率
            });

            // 击退（需要目标实现击退接口）
            var kb = other.GetComponent<SM_IKnockbackable>();
            if (kb != null && knockbackForce > 0f)
                kb.Knockback(_dir, knockbackForce, knockbackTime);

            // 燃尽（需要目标 SM_BurnEffect 辅助实现持续伤害）
            if (burnDPS > 0f && burnTime > 0f)
                other.GetComponent<SM_BurnEffect>()?.Apply(burnDPS, burnTime);

            // 冻结（需要目标 SM_FreezeEffect 显示冰冻状态）
            if (freezeChance > 0f && Random.value < freezeChance)
                other.GetComponent<SM_FreezeEffect>()?.Freeze(freezeTime);
        }

        Destroy(gameObject); // 击中后销毁，一次性投射
    }
}
