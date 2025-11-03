using UnityEngine; // Unity �����ռ�

/// <summary>
/// ���糡��������Ϊ���ĳ����Է�Χ�ڵĵ��������Ԫ���˺�����ʱ���ۼƣ�
/// ˵�����ü���Ϊ������Ч��������ʩ�ź��һ��ʱ���ڣ�ÿ֡�Է�Χ�ڵĵ��˵����˺���
///      �˺�Ϊ damagePerSecond * dt����ȴ��ʱ�������ʱ�˴˶�����
/// </summary>
public class SM_Lightning_StaticField : SM_BaseSkill
{
    [Header("���糡����")]
    public float radius = 3f;              // �����뾶����ʩ����ΪԲ�ģ�
    public float damagePerSecond = 5f;     // ÿ�����˺�������� dt��
    public float duration = 6f;            // ����ʱ�䣨�룩
    public LayerMask enemyMask;            // ����ͼ�㣨��������Ϊ���ǵ� Enemy �㣩

    private float _timer = 0f;             // �����ѳ���ʱ��
    private bool _active = false;          // �Ƿ��ڳ����׶�

    /// <summary>
    /// ʩ�ž��糡�����롰�����׶Ρ�
    /// </summary>
    protected override bool DoCast()
    {
        _timer = 0f;       // ���ó�����ʱ
        _active = true;    // ���Ϊ����״̬
        return true;       // ����ʩ�ųɹ�
    }

    /// <summary>
    /// ÿ֡���£�������У���Է�Χ�ڵ�����ɰ�ʱ���������˺�
    /// </summary>
    public override void Tick(float dt)
    {
        base.Tick(dt);                     // ������ȴ��ʱ
        if (!_active) return;              // δ�������

        _timer += dt;                      // ����ʱ���ۼ�
        if (_timer >= duration)            // ���������ʱ��
        {
            _active = false;               // ��������
            return;                        // ���أ��ȴ���ȴ���������ٴ�ʩ�ţ�
        }

        // �ڰ뾶��Χ��Ѱ�ҵ��ˣ���ײ������ enemyMask ָ���Ĳ㣩
        var hits = Physics2D.OverlapCircleAll(character.AimOrigin.position, radius, enemyMask);
        // ��ÿ�����˰���ÿ���˺� * dt�������˺����γ�ƽ���ĳ����˺�
        float tickDamage = damagePerSecond * dt; // ��֡�˺�
        foreach (var h in hits)
        {
            var dmg = h.GetComponent<SM_IDamageable>(); // ��ȡ�������˽ӿڡ�
            if (dmg != null)
            {
                dmg.ApplyDamage(new SM_DamageInfo
                {
                    Amount = tickDamage,                // ��֡�˺�ֵ
                    Element = SM_Element.Lightning,     // ��Ԫ��
                    IgnoreDefense = false,              // �����ӷ���
                    CritChance = 0f,                    // �ޱ���
                    CritMultiplier = 1f                 // ��ͨ����
                });
            }
        }
    }

    /// <summary>
    /// ����ѡ���� Scene ��ͼ�л���һ���߿�Բ����������ӻ���Χ
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;                                // ʹ�û�ɫ
        Gizmos.DrawWireSphere(transform.position, radius);          // �����߿�Բ
    }
}