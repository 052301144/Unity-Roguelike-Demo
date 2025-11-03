using UnityEngine; // Unity �����ռ�

/// <summary>
/// ͻ�̣���ǰ�̾������λ�ƣ���·���ϵĵ�����������˺�
/// Ϊ�˲��������ǵ��ƶ����ƣ�����ֱ���޸� transform.position������ʱ�䣩
/// </summary>
public class SM_Physical_DashStab : SM_BaseSkill
{
    [Header("ͻ�̲���")]
    public float dashDistance = 4f;    // λ�ƾ���
    public float dashTime = 0.15f;     // λ�ƺ�ʱ
    public float damage = 20f;         // �˺���ֵ
    public LayerMask enemyMask;        // ����ͼ��

    private float _timer;              // ��ʱ
    private Vector2 _start;            // ���
    private Vector2 _end;              // �յ�
    private bool _dashing;             // �Ƿ�ͻ����

    protected override bool DoCast()
    {
        _start = character.AimOrigin.position;                          // ��¼���
        _end = _start + character.AimDirection.normalized * dashDistance; // �����յ�
        _timer = 0f;                                                    // ���ü�ʱ
        _dashing = true;                                                // ��ʼͻ��
        return true;                                                    // �ɹ�
    }

    public override void Tick(float dt)
    {
        base.Tick(dt);                                                  // ��ȴ��ʱ
        if (!_dashing) return;                                          // ��ͻ����
        _timer += dt;                                                   // ����ʱ��
        float t = Mathf.Clamp01(_timer / dashTime);                     // ��һ������
        var pos = Vector2.Lerp(_start, _end, t);                        // ��ֵλ��
        transform.position = pos;                                       // ֱ������λ�ã�����������

        // �ڵ�ǰλ�õ�һ��СԲ�ڼ����ˣ�ģ�⡰·���˺�����
        var hits = Physics2D.OverlapCircleAll(pos, 0.6f, enemyMask);
        foreach (var h in hits)
        {
            var dmg = h.GetComponent<SM_IDamageable>();                 // ���˽ӿ�
            if (dmg != null)
            {
                dmg.ApplyDamage(new SM_DamageInfo
                {
                    Amount = damage,                                    // �˺�
                    Element = SM_Element.Physical,                      // ����
                    IgnoreDefense = true,                               // ���ӷ���
                    CritChance = 0.1f,                                  // �ͱ���
                    CritMultiplier = 1.5f                               // ��������
                });
            }
        }

        if (t >= 1f) _dashing = false;                                  // ����ͻ��
    }
}