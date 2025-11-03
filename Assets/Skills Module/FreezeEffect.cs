using UnityEngine; // Unity �����ռ�

/// <summary>
/// ����Ч�����������ʱ�������ġ�ͣ�ж�����������ǵ��˽ű����ж�ȡ IsFrozen �������
/// ���������������޸����ǵ���ģ���ǰ���£�������չ�㡣
/// </summary>
public class SM_FreezeEffect : MonoBehaviour, SM_IFreezable
{
    private float _remain;                // ʣ�ඳ��ʱ��
    public bool IsFrozen => _remain > 0f; // �ⲿ�ɶ����Ƿ񶳽���

    public void Freeze(float duration)    // ʵ�ֽӿڣ�ʩ�Ӷ���
    {
        _remain = Mathf.Max(_remain, duration); // ˢ�³���ʱ�䣨ȡ������
    }

    private void Update()
    {
        if (_remain > 0f) _remain -= Time.deltaTime; // ����ʱ��ݼ�
    }
}