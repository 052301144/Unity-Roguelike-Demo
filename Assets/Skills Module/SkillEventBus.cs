using System; // 委托/事件

/// <summary>
/// 技能模块的事件总线：仅对外广播，不依赖其他模块
/// UI 或日志模块可以订阅这些事件而无需引用技能内部细节
/// </summary>
public static class SM_SkillEventBus
{
    public static event Action<string> OnSkillCast;          // 当任意技能成功施放
    public static event Action<float, float> OnMPChanged;     // 当 MP 变化（当前/最大）

    public static void RaiseSkillCast(string name)            // 触发技能施放事件
    {
        OnSkillCast?.Invoke(name);                            // 通知所有订阅者
    }

    public static void RaiseMPChanged(float cur, float max)   // 触发 MP 变化事件
    {
        OnMPChanged?.Invoke(cur, max);                        // 通知所有订阅者
    }
}