using UnityEngine; // Unity �����ռ�

/// <summary>
/// ������壺����ǰ���������Դ����ĵ�����ɷ�Ԫ���˺�������
/// �÷�����Ԥ���壨�󴥷��������Ҵ˽ű�������ʵ������ Launch()��
/// </summary>
[RequireComponent(typeof(Collider2D))] // ��Ҫ������
public class SM_Tornado : MonoBehaviour
{
    public float speed = 2f;          // ǰ���ٶ�
    public float lifetime = 6f;       // ���ʱ��
    public float tickDamage = 5f;     // ÿ�δ����˺�
    public float tickInterval = 0.5f; // ���������룩
    public float knockback = 8f;      // ��������

    private float _t;                 // �����ʱ
    private float _tk;                // ����ʱ
    private Vector2 _dir;             // ǰ������

    public void Launch(Vector2 dir)   // ���䣨���ܵ��ã�
    {
        _dir = dir.normalized;        // ��¼����
    }

    private void Update()
    {
        transform.position += (Vector3)(_dir * speed * Time.deltaTime); // �ƶ�
        _t += Time.deltaTime;                                           // �����ʱ
        _tk += Time.deltaTime;                                          // tick ��ʱ
        if (_t >= lifetime) Destroy(gameObject);                        // ��ʱ����
    }

    private void OnTriggerStay2D(Collider2D other) // �����Ӵ�����
    {
        if (_tk < tickInterval) return;                                // δ��������������
        var dmg = other.GetComponent<SM_IDamageable>();                 // ��ȡ���˽ӿ�
        if (dmg != null)                                                // �������
        {
            _tk = 0f;                                                   // ���ü��
            dmg.ApplyDamage(new SM_DamageInfo                           // ����˺�
            {
                Amount = tickDamage,                                    // ��ֵ
                Element = SM_Element.Wind,                              // ��Ԫ��
                IgnoreDefense = false,                                  // �����ӷ���
                CritChance = 0f,                                        // �ޱ���
                CritMultiplier = 1f                                     // ����
            });

            // ���ˣ���ʵ���˻��˽ӿڣ�
            var kb = other.GetComponent<SM_IKnockbackable>();
            if (kb != null) kb.Knockback(_dir, knockback, 0.1f);
        }
    }
}