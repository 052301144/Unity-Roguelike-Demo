using UnityEngine; // Unity 引擎命名空间

/// <summary>
/// 龙卷风：发射一个持续移动的龙卷风，对触碰到它的敌方造成伤害并击退
/// 需要在 Inspector 指定 tornadoPrefab（含 SM_Tornado 的预制体）
/// </summary>
public class SM_Wind_Tornado : SM_BaseSkill
{
    [Header("龙卷风")]
    public SM_Tornado tornadoPrefab;  // 预制体
    public float speed = 2f;          // 龙卷实例速度
    public float lifetime = 6f;       // 龙卷实例持续时间
    public float tickDamage = 5f;     // 龙卷实例每次伤害
    public float tickInterval = 0.5f; // 龙卷实例周期
    public float knockback = 8f;      // 龙卷实例击退力

    protected override bool DoCast()
    {
        if (tornadoPrefab == null) return false;                            // 未设置
        var go = Instantiate(tornadoPrefab, character.AimOrigin.position, Quaternion.identity); // 实例化
        go.speed = speed;                                                   // 写入属性
        go.lifetime = lifetime;
        go.tickDamage = tickDamage;
        go.tickInterval = tickInterval;
        go.knockback = knockback;
        go.Launch(character.AimDirection);                                  // 发射
        return true;                                                        // 成功
    }
}
