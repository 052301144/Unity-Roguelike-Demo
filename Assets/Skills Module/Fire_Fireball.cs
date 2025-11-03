using UnityEngine; // Unity �����ռ�

/// <summary>
/// ������������һ�Ż������к�����˺�������ȼ�գ������˺���
/// ��Ҫ�� Inspector ������ fireballPrefab��һ���� SM_Projectile ��Ԥ���壩
/// </summary>
public class SM_Fire_Fireball : SM_BaseSkill
{
    [Header("�������")]
    public SM_Projectile fireballPrefab; // Ԥ���壺Sprite + Collider2D(isTrigger) + ���ű�
    public float damage = 25f;           // ֱ���˺�
    public float burnDPS = 5f;           // ȼ��ÿ���˺�
    public float burnTime = 4f;          // ȼ�ճ���ʱ��
    public float speed = 12f;            // �����ٶ�
    public float lifetime = 3f;          // ���ʱ��

    protected override bool DoCast()
    {
        if (fireballPrefab == null) return false;                                   // δ����Ԥ����
        var go = Instantiate(fireballPrefab, character.AimOrigin.position, Quaternion.identity); // ����
        go.damage = damage;                                                         // ��ֵ����
        go.element = SM_Element.Fire;                                               // Ԫ�أ���
        go.burnDPS = burnDPS;                                                       // ȼ��ÿ���˺�
        go.burnTime = burnTime;                                                     // ȼ�ճ���
        go.speed = speed;                                                           // �ٶ�
        go.lifetime = lifetime;                                                     // ��������
        go.Launch(character.AimDirection);                                          // ����
        return true;                                                                // �ɹ�
    }
}