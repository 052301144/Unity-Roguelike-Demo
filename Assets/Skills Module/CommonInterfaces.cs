using UnityEngine; // Unity 引擎命名空间

/// <summary>
/// 可受伤害接口：战斗/目标单位实现此接口，技能能对目标造成伤害
/// 注意：这是暴力的统一接口，要求用的敌人脚本同时实现此接口并实现，
///
/// </summary>
public interface SM_IDamageable
{
    void ApplyDamage(SM_DamageInfo info); // 应用伤害
    Transform GetTransform();             // 获取 Transform（用于范围/位置计算）
}

/// <summary>
/// 可被击退接口：用于风元素效果（可选实现）
/// </summary>
public interface SM_IKnockbackable
{
    void Knockback(Vector2 dir, float force, float duration); // 击退
}

/// <summary>
/// 可被冰冻接口：用于冰元素效果（可选实现）
/// </summary>
public interface SM_IFreezable
{
    void Freeze(float duration); // 冰冻
}

/// <summary>
/// 可被燃尽接口：用于火元素持续伤害（可选实现）
/// </summary>
public interface SM_IBurnable
{
    void ApplyBurn(float dps, float duration); // 施加燃尽（每段伤害，持续时间）
}

/// <summary>
/// 技能系统依赖「角色信息模块」提供角色只读信息的接口（解耦）
/// 注意到 PlayerController 需要实现此接口，由技能系统内部提供实现。
/// </summary>
public interface SM_ICharacterProvider
{
    Transform AimOrigin { get; }  // 技能发射原点（一般是角色位置或手上发射点）
    Vector2 AimDirection { get; } // 朝向/瞄准方向（默认向右，由外部设置）
    float CurrentMP { get; }      // 当前魔法值
    float MaxMP { get; }          // 最大魔法值
    bool ConsumeMP(float amount); // 消费魔法值（成功返回 true）
}

/// <summary>
/// 技能接口：所有技能类都实现此接口
/// </summary>
public interface SM_ISkill
{
    string SkillName { get; }                        // 名称
    SM_Element Element { get; }                      // 元素类型
    float ManaCost { get; }                          // MP 消耗
    float Cooldown { get; }                          // 冷却时长
    bool IsOnCooldown { get; }                       // 是否在冷却
    void Initialize(SM_ICharacterProvider provider); // 初始化注入角色信息
    bool TryCast();                                  // 尝试施放（内部检查 MP/冷却）
    void Tick(float dt);                             // 每帧更新（更新冷却/技能效果）
}
