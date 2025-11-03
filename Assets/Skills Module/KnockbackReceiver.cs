using UnityEngine; // Unity 引擎命名空间

/// <summary>
/// 击退接收器：简单地将击退施加给 Rigidbody2D，固定势速度击退，攻击者速度可以覆写
/// </summary>
[RequireComponent(typeof(Collider2D))] // 添加需要碰撞器（不是发射到目标技能碰撞器）
public class SM_KnockbackReceiver : MonoBehaviour, SM_IKnockbackable
{
    private Rigidbody2D _rb;  // 刚体
    private float _timer;     // 剩余存活时长
    private Vector2 _vel;     // 击退速度

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>(); // 自动获取刚体
    }

    public void Knockback(Vector2 dir, float force, float duration) // 实现接口
    {
        dir.Normalize();             // 归一化一次
        _vel = dir * force;          // 速度力度
        _timer = duration;           // 记录存活时长
    }

    private void FixedUpdate()
    {
        if (_timer <= 0f) return;    // 非击退状态返回
        _timer -= Time.fixedDeltaTime; // 时长递减
        if (_rb != null) _rb.velocity = _vel; // 对刚体施加速度
    }
}
