using UnityEngine; // Unity 引擎命名空间

/// <summary>
/// 火球术：发射一颗火球并对路径上敌方造成伤害，额外燃尽：持续伤害
/// 需要在 Inspector 中指定 fireballPrefab（一个含 SM_Projectile 的预制体）
/// </summary>
public class SM_Fire_Fireball : SM_BaseSkill
{
    [Header("火球属性")]
    public SM_Projectile fireballPrefab; // 预制体：Sprite + Collider2D(isTrigger) + 子脚本
    public float damage = 25f;           // 直接伤害
    public float burnDPS = 5f;           // 燃尽每秒伤害
    public float burnTime = 4f;          // 燃尽持续时间
    public float speed = 12f;            // 投射速度
    public float lifetime = 3f;          // 存活时长

    protected override bool DoCast()
    {
        if (fireballPrefab == null) return false;                                   // 未设置预制体
        var go = Instantiate(fireballPrefab, character.AimOrigin.position, Quaternion.identity); // 实例化
        go.damage = damage;                                                         // 伤害设置
        go.element = SM_Element.Fire;                                               // 元素（火）
        go.burnDPS = burnDPS;                                                       // 燃尽每秒伤害
        go.burnTime = burnTime;                                                     // 燃尽持续时间
        go.speed = speed;                                                           // 速度
        go.lifetime = lifetime;                                                     // 存活时间
        go.Launch(character.AimDirection);                                          // 发射
        return true;                                                                // 成功
    }
}
