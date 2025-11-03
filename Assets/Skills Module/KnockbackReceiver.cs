using UnityEngine; // Unity �����ռ�

/// <summary>
/// ���˽�������������˹��� Rigidbody2D������ƽ���ر����ˣ�����������ٶȿ����޸�
/// </summary>
[RequireComponent(typeof(Collider2D))] // ������Ҫ��ײ�������뼼�ܵ���ײ���
public class SM_KnockbackReceiver : MonoBehaviour, SM_IKnockbackable
{
    private Rigidbody2D _rb;  // ����
    private float _timer;     // ʣ�����ʱ��
    private Vector2 _vel;     // �����ٶ�

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>(); // ���Ի�ȡ����
    }

    public void Knockback(Vector2 dir, float force, float duration) // ʵ�ֽӿ�
    {
        dir.Normalize();             // �����һ��
        _vel = dir * force;          // �����ٶ�����
        _timer = duration;           // ��¼����ʱ��
    }

    private void FixedUpdate()
    {
        if (_timer <= 0f) return;    // �ǻ������򷵻�
        _timer -= Time.fixedDeltaTime; // ʱ��ݼ�
        if (_rb != null) _rb.velocity = _vel; // �Ը���ʩ���ٶ�
    }
}