using UnityEngine;

[System.Serializable]
public class LootItem
{
    // ����� prefab�����룩
    public GameObject prefab;

    // �������Ȩ�أ����ֵ�������ȫ��Ȩ��Ϊ 0������˻ص� equal-chance
    // Ҳ��ʹ�� explicitChance�������棩��֧��ֱ�����þ��Ը���
    public float weight = 1f;

    // �� explicitChance >= 0 ʱ��ʹ�� explicitChance��0~1����Ϊ���Ե�����ʣ���ѡ��
    // �������Ϊ -1 ����Ϊδ���ã�Ĭ�ϣ�
    [Range(-1f, 1f)]
    public float explicitChance = -1f;

    // ��С/����������������ѵ��
    public int minAmount = 1;
    public int maxAmount = 1;

    // �Ƿ���ѡ�񵽸���Ŀ���ٸ��� explicitChance ���������Ƿ���䣨���ڶ��׶ο��ƣ�
    // �������ջ������Ҫʹ�ã�
}
