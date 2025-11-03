using UnityEngine; // ʹ�� Unity ���������ռ�


/// <summary>
/// Ԫ�����Ͷ��壺����/��/��/��/��
/// </summary>
public enum SM_Element // SM = Skills Module������������ģ��������ͻ
{
    Physical,  // ��������ӷ������ͱ���
    Fire,      // �𣺳����˺���ȼ�գ�
    Wind,      // �磺����Ч��
    Ice,       // �����������
    Lightning  // �ף���Χ/����
}

/// <summary>
/// �˺���Ϣ�ṹ�壺��������˺�ʱЯ������Ϣ
/// </summary>
public struct SM_DamageInfo
{
    public float Amount;        // �˺���ֵ
    public SM_Element Element;  // Ԫ������
    public bool IgnoreDefense;  // �Ƿ����ӷ��������������Ҫ��
    public float CritChance;    // �����ʣ��������Ϊ�ͱ���������Ϊ0��
    public float CritMultiplier;// ��������������1.5��
}