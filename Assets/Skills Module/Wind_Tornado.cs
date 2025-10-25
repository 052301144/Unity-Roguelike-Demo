using UnityEngine; // Unity 命名空间

/// <summary>
/// 龙卷风：召唤一个缓慢移动的卷风，持续对接触敌人伤害并击退
/// 需要在 Inspector 设置 tornadoPrefab（带 SM_Tornado 的预制体）
/// </summary>
public class SM_Wind_Tornado : SM_BaseSkill
{
    [Header("龙卷风参数")]
    public SM_Tornado tornadoPrefab;  // 预制体
    public float speed = 2f;          // 覆盖实例速度
    public float lifetime = 6f;       // 覆盖实例生存
    public float tickDamage = 5f;     // 覆盖实例每Tick伤害
    public float tickInterval = 0.5f; // 覆盖实例Tick间隔
    public float knockback = 8f;      // 覆盖实例击退力

    protected override bool DoCast()
    {
        if (tornadoPrefab == null) return false;                            // 未配置
        var go = Instantiate(tornadoPrefab, character.AimOrigin.position, Quaternion.identity); // 实例化
        go.speed = speed;                                                   // 写入参数
        go.lifetime = lifetime;
        go.tickDamage = tickDamage;
        go.tickInterval = tickInterval;
        go.knockback = knockback;
        go.Launch(character.AimDirection);                                  // 发射
        return true;                                                        // 成功
    }
}