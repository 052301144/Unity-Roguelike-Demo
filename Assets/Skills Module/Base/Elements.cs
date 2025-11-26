using UnityEngine; // 使用 Unity 引擎命名空间


/// <summary>
/// 元素类型定义：物理/火/风/冰/雷
/// </summary>
public enum SM_Element // SM = Skills Module（技能系统模块，避免命名冲突）
{
    Physical,  // 物理攻击（物理攻击无暴击和额外防御）
    Fire,      // 火：持续伤害和燃尽
    Wind,      // 风：击退效果
    Ice,       // 冰：冰冻控制
    Lightning  // 雷：范围/连锁
}

/// <summary>
/// 伤害信息结构体：当对目标造成伤害时携带的信息
/// </summary>
public struct SM_DamageInfo
{
    public float Amount;        // 伤害数值
    public SM_Element Element;  // 元素类型
    public bool IgnoreDefense;  // 是否忽略防御（物理攻击一般需要）
    public float CritChance;    // 暴击率（概率型为暴击型物理，其余为0）
    public float CritMultiplier;// 暴击倍数（通常为1.5倍）
}
