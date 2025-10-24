using UnityEngine; // Unity 命名空间

/// <summary>
/// 火球术：发射一颗火球，命中后造成伤害并附带燃烧（持续伤害）
/// 需要在 Inspector 里设置 fireballPrefab（一个带 SM_Projectile 的预制体）
/// </summary>
public class SM_Fire_Fireball : SM_BaseSkill
{
    [Header("火球参数")]
    public SM_Projectile fireballPrefab; // 预制体：Sprite + Collider2D(isTrigger) + 本脚本
    public float damage = 25f;           // 直接伤害
    public float burnDPS = 5f;           // 燃烧每秒伤害
    public float burnTime = 4f;          // 燃烧持续时间
    public float speed = 12f;            // 飞行速度
    public float lifetime = 3f;          // 存活时间

    protected override bool DoCast()
    {
        if (fireballPrefab == null) return false;                                   // 未配置预制体
        var go = Instantiate(fireballPrefab, character.AimOrigin.position, Quaternion.identity); // 生成
        go.damage = damage;                                                         // 赋值参数
        go.element = SM_Element.Fire;                                               // 元素：火
        go.burnDPS = burnDPS;                                                       // 燃烧每秒伤害
        go.burnTime = burnTime;                                                     // 燃烧持续
        go.speed = speed;                                                           // 速度
        go.lifetime = lifetime;                                                     // 生命周期
        go.Launch(character.AimDirection);                                          // 发射
        return true;                                                                // 成功
    }
}