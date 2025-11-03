using UnityEngine; // Unity �����ռ�

/// <summary>
/// ����������ǰ������������棬����˺����߸��ʵ�ȼ
/// ������Ͷ���ֱ�������η�Χ���
/// </summary>
public class SM_Fire_FlameCone : SM_BaseSkill
{
    [Header("����������")]
    public float range = 4f;           // ���ΰ뾶
    public float angle = 45f;          // ��ǣ��ܽǶ�=2*��ǣ�
    public float damage = 15f;         // ֱ���˺�
    public float burnDPS = 6f;         // ȼ��ÿ���˺�
    public float burnTime = 5f;        // ȼ�ճ���ʱ��
    public float burnChance = 0.8f;    // ��ȼ���ʣ��ߣ�
    public LayerMask enemyMask;        // ����ͼ��

    protected override bool DoCast()
    {
        var origin = (Vector2)character.AimOrigin.position;            // �ͷ�ԭ��
        var dir = character.AimDirection.normalized;                   // ����
        var hits = Physics2D.OverlapCircleAll(origin, range, enemyMask);// Բ���ں�ѡ
        foreach (var h in hits)                                        // ����
        {
            var to = (Vector2)h.transform.position - origin;           // ָ������
            if (Vector2.Angle(dir, to) <= angle)                       // �ж��Ƿ���������
            {
                var dmg = h.GetComponent<SM_IDamageable>();            // ���˽ӿ�
                if (dmg != null)
                {
                    dmg.ApplyDamage(new SM_DamageInfo
                    {
                        Amount = damage,            // �˺�
                        Element = SM_Element.Fire,  // ��Ԫ��
                        IgnoreDefense = false,      // �����ӷ���
                        CritChance = 0f,            // �ޱ���
                        CritMultiplier = 1f         // ����
                    });
                }
                if (Random.value < burnChance)                          // �жϵ�ȼ
                    h.GetComponent<SM_BurnEffect>()?.Apply(burnDPS, burnTime); // ʩ��ȼ�գ���Ŀ���и������
            }
        }
        return true;                                                    // �ɹ�
    }

    private void OnDrawGizmosSelected()                                 // �༭�����ӻ�
    {
        var o = transform.position;                                     // ԭ��
        var dir = Vector2.right;                                        // ��ʾĬ����
        Gizmos.color = new Color(1, 0.5f, 0, 1);                        // ��ɫ
        var left = Quaternion.Euler(0, 0, -angle) * dir * range;        // ��߽�
        var right = Quaternion.Euler(0, 0, angle) * dir * range;        // �ұ߽�
        Gizmos.DrawLine(o, o + (Vector3)left);                          // ����
        Gizmos.DrawLine(o, o + (Vector3)right);                         // ����
    }
}