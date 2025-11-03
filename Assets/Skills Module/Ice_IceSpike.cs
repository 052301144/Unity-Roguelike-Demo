using UnityEngine; // Unity �����ռ�

/// <summary>
/// ��׶����ֱ��Ͷ�䣬��������˺����и��ʶ���Ŀ��
/// ��Ҫ�� Inspector ���� spikePrefab���� SM_Projectile ��Ԥ���壩
/// </summary>
public class SM_Ice_IceSpike : SM_BaseSkill
{
    [Header("��׶������")]
    public SM_Projectile spikePrefab;    // Ԥ����
    public float damage = 18f;           // �˺�
    public float speed = 14f;            // �ٶ�
    public float lifetime = 3f;          // ���
    public float freezeChance = 0.4f;    // �������
    public float freezeTime = 1.5f;      // ����ʱ��

    protected override bool DoCast()
    {
        if (spikePrefab == null) return false;                               // δ����
        var go = Instantiate(spikePrefab, character.AimOrigin.position, Quaternion.identity);
        go.damage = damage;                                                  // ����
        go.element = SM_Element.Ice;
        go.speed = speed;
        go.lifetime = lifetime;
        go.freezeChance = freezeChance;
        go.freezeTime = freezeTime;
        go.Launch(character.AimDirection);                                    // ����
        return true;
    }
}