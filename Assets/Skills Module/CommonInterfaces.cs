using UnityEngine; // Unity 命名空间

/// <summary>
/// 可受伤接口：敌人/目标如果实现本接口，就能被技能模块伤害到
/// （如果你们已有类似接口，可让敌人脚本同时实现这个接口以兼容）

/// </summary>
public interface SM_IDamageable
{
    void ApplyDamage(SM_DamageInfo info); // 应用伤害
    Transform GetTransform();             // 返回 Transform（用于方向/位置计算）
}

/// <summary>
/// 可被击退接口：用于风元素效果（可选）
/// </summary>
public interface SM_IKnockbackable
{
    void Knockback(Vector2 dir, float force, float duration); // 被击退
}

/// <summary>
/// 可被冻结接口：用于冰元素效果（可选）
/// </summary>
public interface SM_IFreezable
{
    void Freeze(float duration); // 冻结
}

/// <summary>
/// 可被点燃接口：用于火元素持续伤害（可选）
/// </summary>
public interface SM_IBurnable
{
    void ApplyBurn(float dps, float duration); // 施加燃烧（每秒伤害，持续时间）
}

/// <summary>
/// 技能系统向“角色控制模块”请求只读信息的接口（解耦）
/// 你们的 PlayerController 不需要实现它；由本技能系统内部提供实现。
/// </summary>
public interface SM_ICharacterProvider
{
    Transform AimOrigin { get; }  // 技能释放起点（一般是角色位置或手部挂点）
    Vector2 AimDirection { get; } // 面朝/瞄准方向（默认向右；可由外部设置）
    float CurrentMP { get; }      // 当前魔法值
    float MaxMP { get; }          // 最大魔法值
    bool ConsumeMP(float amount); // 消耗魔法值（成功返回 true）
}

/// <summary>
/// 技能公共接口：所有技能类都实现它
/// </summary>
public interface SM_ISkill
{
    string SkillName { get; }                        // 技能名
    SM_Element Element { get; }                      // 元素类型
    float ManaCost { get; }                          // MP 消耗
    float Cooldown { get; }                          // 冷却时间
    bool IsOnCooldown { get; }                       // 是否处于冷却
    void Initialize(SM_ICharacterProvider provider); // 初始化（注入只读角色信息）
    bool TryCast();                                  // 尝试施放（内部检查 MP/冷却）
    void Tick(float dt);                             // 每帧更新（处理冷却/持续效果）
}