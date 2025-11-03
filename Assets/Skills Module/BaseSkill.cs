using UnityEngine; // Unity �����ռ�

/// <summary>
/// ���м��ܵĳ�����ࣺͳһ������ȴ/����/�¼�
/// </summary>
public abstract class SM_BaseSkill : MonoBehaviour, SM_ISkill
{
    [Header("��������")]
    public string skillName = "Skill";        // �������ƣ����� Inspector ���ã�
    public SM_Element element = SM_Element.Physical; // Ԫ������
    public float manaCost = 10f;              // MP ����
    public float cooldown = 2f;               // ��ȴʱ�����룩

    protected float cdTimer = 0f;             // �ڲ���ȴ��ʱ��
    protected SM_ICharacterProvider character;// ��ɫֻ����Ϣ�ṩ�ߣ��ɼ���ϵͳע�룩

    public string SkillName => skillName;     // �ӿ�ʵ�֣�������
    public SM_Element Element => element;     // �ӿ�ʵ�֣�Ԫ��
    public float ManaCost => manaCost;        // �ӿ�ʵ�֣�����
    public float Cooldown => cooldown;        // �ӿ�ʵ�֣���ȴ
    public bool IsOnCooldown => cdTimer > 0f; // �ӿ�ʵ�֣���ȴ�У�

    public void Initialize(SM_ICharacterProvider provider)    // ��ʼ��ע��
    {
        character = provider;                                   // ��������
    }

    protected abstract bool DoCast();                           // ����ʵ��ʩ���߼�

    public virtual bool TryCast()                               // ����ʩ��
    {
        if (IsOnCooldown) return false;                         // ��ȴ�У�����ʩ��
        if (character == null) return false;                    // δ��ʼ������ȫ����
        if (!character.ConsumeMP(manaCost)) return false;       // MP ���㣬ʩ��ʧ��

        var ok = DoCast();                                      // ִ�������߼�
        if (ok)                                                 // ��ʩ�ųɹ�
        {
            cdTimer = cooldown;                                 // ������ȴ
            SM_SkillEventBus.RaiseSkillCast(skillName);         // �㲥ʩ���¼�
        }
        return ok;                                              // ���ؽ��
    }

    public virtual void Tick(float dt)                          // ÿ֡����
    {
        if (cdTimer > 0f) cdTimer -= dt;                        // ��ȴ��ʱ����
    }
}