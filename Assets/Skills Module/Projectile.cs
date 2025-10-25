using UnityEngine; // Unity 命名空间

/// <summary>
/// 通用投射物：用于火球/冰锥/风压弹等一次性弹道
/// 使用方法：做一个预制体（含 SpriteRenderer + Collider2D isTrigger），挂此脚本；技能实例化后调用 Launch()
/// </summary>
[RequireComponent(typeof(Collider2D))]      // 需要触发器来检测命中
public class SM_Projectile : MonoBehaviour
{
    [Header("基础参数")]
    public float speed = 10f;               // 飞行速度
    public float lifetime = 3f;             // 存活时间（超时自毁）
    public float damage = 10f;              // 命中伤害
    public SM_Element element = SM_Element.Physical; // 元素类型

    [Header("风元素（可选）")]
    public float knockbackForce = 0f;       // 击退力度
    public float knockbackTime = 0.1f;      // 击退持续时间

    [Header("火元素（可选）")]
    public float burnDPS = 0f;              // 燃烧每秒伤害
    public float burnTime = 0f;             // 燃烧持续时间

    [Header("冰元素（可选）")]
    public float freezeChance = 0f;         // 冻结概率（0~1）
    public float freezeTime = 0f;           // 冻结持续时间

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

    private void OnTriggerEnter2D(Collider2D other) // 命中触发
    {
        var dmg = other.GetComponent<SM_IDamageable>();  // 拿到可受伤接口
        if (dmg != null)                                 // 如果可受伤
        {
            // 伤害（物理默认低暴击并无视防御）
            dmg.ApplyDamage(new SM_DamageInfo
            {
                Amount = damage,                         // 伤害
                Element = element,                       // 元素
                IgnoreDefense = (element == SM_Element.Physical),     // 物理无视防御
                CritChance = (element == SM_Element.Physical ? 0.1f : 0f), // 物理低暴击
                CritMultiplier = 1.5f                    // 暴击倍率
            });

            // 击退（仅当目标有实现击退接口）
            var kb = other.GetComponent<SM_IKnockbackable>();
            if (kb != null && knockbackForce > 0f)
                kb.Knockback(_dir, knockbackForce, knockbackTime);

            // 燃烧（需要目标挂 SM_BurnEffect 才能触发持续伤害）
            if (burnDPS > 0f && burnTime > 0f)
                other.GetComponent<SM_BurnEffect>()?.Apply(burnDPS, burnTime);

            // 冻结（需要目标挂 SM_FreezeEffect 才能显示冻结状态）
            if (freezeChance > 0f && Random.value < freezeChance)
                other.GetComponent<SM_FreezeEffect>()?.Freeze(freezeTime);
        }

        Destroy(gameObject); // 命中后自毁（一次性弹道）
    }
}