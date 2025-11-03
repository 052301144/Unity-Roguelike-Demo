using UnityEngine; // Unity �����ռ�

/// <summary>
/// ����ն��ԭ�ػ��ζ�������˺�������=���ӷ������ͱ���
/// </summary>
public class SM_Physical_WhirlwindSlash : SM_BaseSkill
{
    [Header("����ն����")]
    public float radius = 2.5f;      // ���ð뾶
    public int ticks = 5;            // �˺�����
    public float totalTime = 0.8f;   // ������ת����ʱ��
    public float damagePerTick = 8f; // ÿ���˺�
    public LayerMask enemyMask;      // ����ͼ��

    private float _tk;               // ��ʱ��
    private int _done;               // �Ѵ�������
    private bool _active;            // �Ƿ������

    protected override bool DoCast() // ��ʼʩ��
    {
        _active = true;              // ��������
        _tk = 0f;                    // ����ʱ��
        _done = 0;                   // ���ö���
        return true;                 // �ɹ�
    }

    public override void Tick(float dt) // ���Ǹ��� Tick ��ʵ�ֳ�������˺�
    {
        base.Tick(dt);               // �ȴ�����ȴ����ʱ
        if (!_active) return;        // δ����ֱ�ӷ���

        _tk += dt;                   // ������ʱ
        float interval = totalTime / Mathf.Max(1, ticks); // ÿ�μ��
        if (_done < ticks && _tk >= (_done + 1) * interval) // ������һ�δ�����
        {
            _done++;                  // ����+1
            // �Խ�ɫΪԲ����������
            var hits = Physics2D.OverlapCircleAll(character.AimOrigin.position, radius, enemyMask);
            foreach (var h in hits)   // �������еĵ���
            {
                var dmg = h.GetComponent<SM_IDamageable>(); // ��ȡ���˽ӿ�
                if (dmg != null)       // ������������˺�
                {
                    dmg.ApplyDamage(new SM_DamageInfo
                    {
                        Amount = damagePerTick,            // ÿ���˺�
                        Element = SM_Element.Physical,     // ����Ԫ��
                        IgnoreDefense = true,              // ���ӷ���
                        CritChance = 0.1f,                 // �ͱ���
                        CritMultiplier = 1.5f              // ��������
                    });
                }
            }
        }
        if (_tk >= totalTime) _active = false; // ����
    }

    private void OnDrawGizmosSelected() // �༭����Χ���ӻ�
    {
        Gizmos.color = Color.red;       // ��ɫ�߿�
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}