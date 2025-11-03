using UnityEngine; // Unity �����ռ�

/// <summary>
/// ����磺�ٻ�һ�������ƶ��ľ�磬�����ԽӴ������˺�������
/// ��Ҫ�� Inspector ���� tornadoPrefab���� SM_Tornado ��Ԥ���壩
/// </summary>
public class SM_Wind_Tornado : SM_BaseSkill
{
    [Header("��������")]
    public SM_Tornado tornadoPrefab;  // Ԥ����
    public float speed = 2f;          // ����ʵ���ٶ�
    public float lifetime = 6f;       // ����ʵ������
    public float tickDamage = 5f;     // ����ʵ��ÿTick�˺�
    public float tickInterval = 0.5f; // ����ʵ��Tick���
    public float knockback = 8f;      // ����ʵ��������

    protected override bool DoCast()
    {
        if (tornadoPrefab == null) return false;                            // δ����
        var go = Instantiate(tornadoPrefab, character.AimOrigin.position, Quaternion.identity); // ʵ����
        go.speed = speed;                                                   // д�����
        go.lifetime = lifetime;
        go.tickDamage = tickDamage;
        go.tickInterval = tickInterval;
        go.knockback = knockback;
        go.Launch(character.AimDirection);                                  // ����
        return true;                                                        // �ɹ�
    }
}