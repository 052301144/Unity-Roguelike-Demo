using UnityEngine; // Unity �����ռ�

/// <summary>
/// ������������Ϊ���ģ����η�Χ�˺���������Χ����
/// </summary>
public class SM_Ice_IceNova : SM_BaseSkill
{
    [Header("��������")]
    public float radius = 3f;        // �뾶
    public float damage = 10f;       // �˺�
    public float freezeTime = 1.2f;  // ����ʱ��
    public LayerMask enemyMask;      // ����ͼ��

    protected override bool DoCast()
    {
        var hits = Physics2D.OverlapCircleAll(character.AimOrigin.position, radius, enemyMask); // ������Χ
        foreach (var h in hits)
        {
            var dmg = h.GetComponent<SM_IDamageable>();    // ���˽ӿ�
            if (dmg != null)                                // ���������˺�
            {
                dmg.ApplyDamage(new SM_DamageInfo
                {
                    Amount = damage,            // �˺�
                    Element = SM_Element.Ice,   // ��Ԫ��
                    IgnoreDefense = false,      // �����ӷ���
                    CritChance = 0f,            // �ޱ���
                    CritMultiplier = 1f         // ����
                });
            }
            h.GetComponent<SM_FreezeEffect>()?.Freeze(freezeTime); // ʩ�Ӷ��ᣨ���������
        }
        return true;
    }

    private void OnDrawGizmosSelected() // �༭�����ӻ�
    {
        Gizmos.color = Color.blue;      // ��ɫ
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}