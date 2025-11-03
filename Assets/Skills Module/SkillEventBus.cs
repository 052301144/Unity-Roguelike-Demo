using System; // ί��/�¼�

/// <summary>
/// ����ģ����¼����ߣ�������㲥������������ģ��
/// UI ����־ģ����Զ�����Щ�¼����������ü����ڲ�ϸ��
/// </summary>
public static class SM_SkillEventBus
{
    public static event Action<string> OnSkillCast;          // �����⼼�ܳɹ�ʩ��
    public static event Action<float, float> OnMPChanged;     // �� MP �仯����ǰ/���

    public static void RaiseSkillCast(string name)            // ��������ʩ���¼�
    {
        OnSkillCast?.Invoke(name);                            // ֪ͨ���ж�����
    }

    public static void RaiseMPChanged(float cur, float max)   // ���� MP �仯�¼�
    {
        OnMPChanged?.Invoke(cur, max);                        // ֪ͨ���ж�����
    }
}