using UnityEngine; // 使用 Unity 引擎命名空间


/// <summary>
/// 元素类型定义：物理/火/风/冰/雷
/// </summary>
public enum SM_Element // SM = Skills Module，避免与其他模块命名冲突
{
    Physical,  // 物理：无视防御、低暴击
    Fire,      // 火：持续伤害（燃烧）
    Wind,      // 风：击退效果
    Ice,       // 冰：冻结控制
    Lightning  // 雷：范围/连锁
}

/// <summary>
/// 伤害信息结构体：技能造成伤害时携带的信息
/// </summary>
public struct SM_DamageInfo
{
    public float Amount;        // 伤害数值
    public SM_Element Element;  // 元素类型
    public bool IgnoreDefense;  // 是否无视防御（物理技能需要）
    public float CritChance;    // 暴击率（物理技能为低暴击，其他为0）
    public float CritMultiplier;// 暴击倍数（例如1.5）
}