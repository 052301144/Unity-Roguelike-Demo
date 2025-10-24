using UnityEngine; // Unity 命名空间

/// <summary>
/// 燃烧持续伤害（敌人可选复用）
/// 若敌人没有实现 SM_IDamageable，则本效果不会生效，但不会报错。
/// </summary>
public class SM_BurnEffect : MonoBehaviour
{
    private float _remain; // 剩余持续时间
    private float _dps;    // 每秒伤害
    private float _tick;   // 每秒结算计时器

    /// <summary>施加燃烧</summary>
    public void Apply(float dps, float duration)
    {
        _dps = dps;                                      // 记录每秒伤害
        _remain = Mathf.Max(_remain, duration);          // 叠加刷新持续时间（取更长）
    }

    private void Update()
    {
        if (_remain <= 0f) return;                       // 没有燃烧则返回
        _remain -= Time.deltaTime;                       // 持续时间递减
        _tick += Time.deltaTime;                         // 每秒计时

        if (_tick >= 1f)                                 // 每秒触发一次
        {
            _tick = 0f;                                  // 重置秒间隔
            var dmg = GetComponent<SM_IDamageable>();    // 获取可受伤接口
            if (dmg != null)                             // 存在则造成伤害
            {
                dmg.ApplyDamage(new SM_DamageInfo
                {
                    Amount = _dps,                       // 本秒伤害
                    Element = SM_Element.Fire,           // 火元素
                    IgnoreDefense = false,               // 持续伤害不无视防御
                    CritChance = 0f,                     // 无暴击
                    CritMultiplier = 1f                  // 倍率1
                });
            }
        }
    }
}