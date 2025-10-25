using UnityEngine; // Unity 命名空间

/// <summary>
/// 击退接收器：如果敌人挂了 Rigidbody2D，就能平滑地被击退；否则仅设置速度可能无感
/// </summary>
[RequireComponent(typeof(Collider2D))] // 至少需要碰撞体来参与技能的碰撞检测
public class SM_KnockbackReceiver : MonoBehaviour, SM_IKnockbackable
{
    private Rigidbody2D _rb;  // 刚体
    private float _timer;     // 剩余击退时间
    private Vector2 _vel;     // 击退速度

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>(); // 尝试获取刚体
    }

    public void Knockback(Vector2 dir, float force, float duration) // 实现接口
    {
        dir.Normalize();             // 方向归一化
        _vel = dir * force;          // 计算速度向量
        _timer = duration;           // 记录持续时间
    }

    private void FixedUpdate()
    {
        if (_timer <= 0f) return;    // 非击退中则返回
        _timer -= Time.fixedDeltaTime; // 时间递减
        if (_rb != null) _rb.velocity = _vel; // 对刚体施加速度
    }
}