using UnityEngine; // Unity 命名空间

/// <summary>
/// 极简敌人（测试用）：实现 SM_IDamageable，能被技能吃到伤害；
/// 你可以在 Inspector 中看到 Health 的变化，验证技能是否生效。
/// </summary>
[RequireComponent(typeof(Collider2D))] // 需要碰撞体参与检测（注意：可设为 isTrigger）
public class SM_DummyEnemy : MonoBehaviour, SM_IDamageable
{
    [Header("测试生命值")]
    public float Health = 100f;         // 初始生命
    public bool printLog = true;        // 是否在 Console 打印受击日志

    public void ApplyDamage(SM_DamageInfo info) // 实现“可受伤”接口
    {
        Health -= info.Amount;                       // 扣血
        if (Health < 0f) Health = 0f;                // 下限保护
        if (printLog)
        {
            Debug.Log($"[DummyEnemy] 受到伤害：{info.Amount:F2}，元素：{info.Element}，当前生命：{Health:F2}");
        }
    }

    public Transform GetTransform() => transform;    // 返回自身 Transform
}