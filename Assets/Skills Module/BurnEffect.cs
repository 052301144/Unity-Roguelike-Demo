using UnityEngine; // Unity 引擎命名空间

/// <summary>
/// 燃尽持续伤害组件，敌人可选添加
/// 注意：如果没有实现 SM_IDamageable，效果无法造成伤害。效果逻辑无法报错
/// </summary>
public class SM_BurnEffect : MonoBehaviour
{
    private float _remain; // 剩余存活时长
    private float _dps;    // 每秒伤害
    private float _tick;   // 每秒计时器时长

    /// <summary>施加燃尽</summary>
    public void Apply(float dps, float duration)
    {
        _dps = dps;                                      // 记录每秒伤害
        _remain = Mathf.Max(_remain, duration);          // 刷新持续时间（取更长）
    }

    private void Update()
    {
        if (_remain <= 0f) return;                       // 没有燃尽即返回
        _remain -= Time.deltaTime;                       // 持续时长递减
        _tick += Time.deltaTime;                         // 每秒计时

        if (_tick >= 1f)                                 // 每秒触发一次
        {
            _tick = 0f;                                  // 重置计时器
            var dmg = GetComponent<SM_IDamageable>();    // 获取可伤害接口
            if (dmg != null)                             // 如果可伤害
            {
                dmg.ApplyDamage(new SM_DamageInfo
                {
                    Amount = _dps,                       // 秒伤害
                    Element = SM_Element.Fire,           // 火元素
                    IgnoreDefense = false,               // 持续伤害忽略防御
                    CritChance = 0f,                     // 无暴击
                    CritMultiplier = 1f                  // 倍数1
                });
            }
        }
    }
}
