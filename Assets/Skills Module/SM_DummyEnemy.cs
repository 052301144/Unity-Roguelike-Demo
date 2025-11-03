using UnityEngine; // Unity �����ռ�

/// <summary>
/// ������ˣ������ã���ʵ�� SM_IDamageable���ܱ����ܳԵ��˺���
/// ������� Inspector �п��� Health �ı仯����֤�����Ƿ���Ч��
/// </summary>
[RequireComponent(typeof(Collider2D))] // ��Ҫ��ײ������⣨ע�⣺����Ϊ isTrigger��
public class SM_DummyEnemy : MonoBehaviour, SM_IDamageable
{
    [Header("��������ֵ")]
    public float Health = 100f;         // ��ʼ����
    public bool printLog = true;        // �Ƿ��� Console ��ӡ�ܻ���־

    public void ApplyDamage(SM_DamageInfo info) // ʵ�֡������ˡ��ӿ�
    {
        Health -= info.Amount;                       // ��Ѫ
        if (Health < 0f) Health = 0f;                // ���ޱ���
        if (printLog)
        {
            Debug.Log($"[DummyEnemy] �ܵ��˺���{info.Amount:F2}��Ԫ�أ�{info.Element}����ǰ������{Health:F2}");
        }
    }

    public Transform GetTransform() => transform;    // �������� Transform
}