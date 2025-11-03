using UnityEngine; // Unity �����ռ�

/// <summary>
/// ��ѹ�ڣ�ֱ�߿���ж�����·����������˺�������
/// ������Ͷ���ֱ���� OverlapBoxAll
/// </summary>
public class SM_Wind_WindBlast : SM_BaseSkill
{
    [Header("��ѹ�ڲ���")]
    public float width = 0.7f;        // ֱ�߿��
    public float length = 6f;         // ֱ�߳���
    public float damage = 12f;        // �˺�
    public float knockbackForce = 12f;// ������
    public float knockbackTime = 0.15f;// ����ʱ��
    public LayerMask enemyMask;       // ����ͼ��

    protected override bool DoCast()
    {
        var o = (Vector2)character.AimOrigin.position;                  // ���
        var d = character.AimDirection.normalized;                      // ����
        var center = o + d * (length * 0.5f);                           // ������
        float angleZ = Vector2.SignedAngle(Vector2.right, d);           // ��ת��
        var hits = Physics2D.OverlapBoxAll(center, new Vector2(length, width), angleZ, enemyMask); // ������

        foreach (var h in hits)
        {
            var dmg = h.GetComponent<SM_IDamageable>();                 // ���˽ӿ�
            if (dmg != null)
            {
                dmg.ApplyDamage(new SM_DamageInfo
                {
                    Amount = damage,             // �˺�
                    Element = SM_Element.Wind,   // ��Ԫ��
                    IgnoreDefense = false,       // �����ӷ���
                    CritChance = 0f,             // �ޱ���
                    CritMultiplier = 1f          // ����
                });
            }
            h.GetComponent<SM_IKnockbackable>()?.Knockback(d, knockbackForce, knockbackTime); // ��ɻ��������
        }
        return true;                                                    // �ɹ�
    }

    private void OnDrawGizmosSelected()                                 // �༭�����ӻ�
    {
        Gizmos.color = Color.cyan;                                      // ��ɫ
        Gizmos.DrawWireCube(transform.position + Vector3.right * (length * 0.5f), new Vector3(length, width, 0.1f));
    }
}