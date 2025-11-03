using UnityEngine; // Unity �����ռ�

/// <summary>
/// �����˽ӿڣ�����/Ŀ�����ʵ�ֱ��ӿڣ����ܱ�����ģ���˺���
/// ����������������ƽӿڣ����õ��˽ű�ͬʱʵ������ӿ��Լ��ݣ�

/// </summary>
public interface SM_IDamageable
{
    void ApplyDamage(SM_DamageInfo info); // Ӧ���˺�
    Transform GetTransform();             // ���� Transform�����ڷ���/λ�ü��㣩
}

/// <summary>
/// �ɱ����˽ӿڣ����ڷ�Ԫ��Ч������ѡ��
/// </summary>
public interface SM_IKnockbackable
{
    void Knockback(Vector2 dir, float force, float duration); // ������
}

/// <summary>
/// �ɱ�����ӿڣ����ڱ�Ԫ��Ч������ѡ��
/// </summary>
public interface SM_IFreezable
{
    void Freeze(float duration); // ����
}

/// <summary>
/// �ɱ���ȼ�ӿڣ����ڻ�Ԫ�س����˺�����ѡ��
/// </summary>
public interface SM_IBurnable
{
    void ApplyBurn(float dps, float duration); // ʩ��ȼ�գ�ÿ���˺�������ʱ�䣩
}

/// <summary>
/// ����ϵͳ�򡰽�ɫ����ģ�顱����ֻ����Ϣ�Ľӿڣ����
/// ���ǵ� PlayerController ����Ҫʵ�������ɱ�����ϵͳ�ڲ��ṩʵ�֡�
/// </summary>
public interface SM_ICharacterProvider
{
    Transform AimOrigin { get; }  // �����ͷ���㣨һ���ǽ�ɫλ�û��ֲ��ҵ㣩
    Vector2 AimDirection { get; } // �泯/��׼����Ĭ�����ң������ⲿ���ã�
    float CurrentMP { get; }      // ��ǰħ��ֵ
    float MaxMP { get; }          // ���ħ��ֵ
    bool ConsumeMP(float amount); // ����ħ��ֵ���ɹ����� true��
}

/// <summary>
/// ���ܹ����ӿڣ����м����඼ʵ����
/// </summary>
public interface SM_ISkill
{
    string SkillName { get; }                        // ������
    SM_Element Element { get; }                      // Ԫ������
    float ManaCost { get; }                          // MP ����
    float Cooldown { get; }                          // ��ȴʱ��
    bool IsOnCooldown { get; }                       // �Ƿ�����ȴ
    void Initialize(SM_ICharacterProvider provider); // ��ʼ����ע��ֻ����ɫ��Ϣ��
    bool TryCast();                                  // ����ʩ�ţ��ڲ���� MP/��ȴ��
    void Tick(float dt);                             // ÿ֡���£�������ȴ/����Ч����
}