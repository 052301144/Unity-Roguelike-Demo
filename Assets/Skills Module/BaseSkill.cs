using UnityEngine; // Unity 引擎命名空间

/// <summary>
/// 所有技能的抽象基类：统一管理冷却/消耗/事件
/// </summary>
public abstract class SM_BaseSkill : MonoBehaviour, SM_ISkill
{
    [Header("技能基础属性")]
    public string skillName = "Skill";        // 技能名称（供 Inspector 设置）
    public SM_Element element = SM_Element.Physical; // 元素类型
    public float manaCost = 10f;              // MP 消耗
    public float cooldown = 2f;               // 冷却时长（秒）

    protected float cdTimer = 0f;             // 内部冷却计时器
    protected SM_ICharacterProvider character;// 角色信息提供者（由技能系统注入）

    public string SkillName => skillName;     // 接口实现：名称
    public SM_Element Element => element;     // 接口实现：元素
    public float ManaCost => manaCost;        // 接口实现：消耗
    public float Cooldown => cooldown;        // 接口实现：冷却
    public bool IsOnCooldown => cdTimer > 0f; // 接口实现：冷却中

    public void Initialize(SM_ICharacterProvider provider)    // 初始化注入
    {
        character = provider;                                   // 保存引用
    }

    protected abstract bool DoCast();                           // 子类实现施放逻辑

    public virtual bool TryCast()                               // 尝试施放
    {
        if (IsOnCooldown) return false;                         // 冷却中，禁止施放
        if (character == null) return false;                    // 未初始化，安全检查
        if (!character.ConsumeMP(manaCost)) return false;       // MP 不足，施放失败

        var ok = DoCast();                                      // 执行子类逻辑
        if (ok)                                                 // 若施放成功
        {
            cdTimer = cooldown;                                 // 重置冷却
            SM_SkillEventBus.RaiseSkillCast(skillName);         // 广播施放事件
        }
        return ok;                                              // 返回结果
    }

    public virtual void Tick(float dt)                          // 每帧更新
    {
        if (cdTimer > 0f) cdTimer -= dt;                        // 冷却计时递减
    }
}
