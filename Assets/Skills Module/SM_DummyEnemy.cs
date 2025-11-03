using UnityEngine; // Unity 引擎命名空间

/// <summary>
/// 假想敌人（参考用）：实现 SM_IDamageable，能被技能吃到伤害
/// 您可以在 Inspector 中观察 Health 的变化，验证技能是否生效
/// </summary>
[RequireComponent(typeof(Collider2D))] // 需要碰撞器（注意：不要设为 isTrigger）
public class SM_DummyEnemy : MonoBehaviour, SM_IDamageable
{
    [Header("生命值")]
    public float Health = 100f;         // 初始生命
    public bool printLog = true;        // 是否在 Console 中打印技能日志

    public void ApplyDamage(SM_DamageInfo info) // 实现「可伤害」接口
    {
        Health -= info.Amount;                       // 扣血
        if (Health < 0f) Health = 0f;                // 防止负值
        if (printLog)
        {
            Debug.Log($"[DummyEnemy] 受到伤害：{info.Amount:F2}，元素：{info.Element}，当前生命：{Health:F2}");
        }
    }

    public Transform GetTransform() => transform;    // 返回目标 Transform
}
