using UnityEngine; // Unity �����ռ�

/// <summary>
/// ȼ�ճ����˺������˿�ѡ���ã�
/// �����û��ʵ�� SM_IDamageable����Ч��������Ч�������ᱨ���
/// </summary>
public class SM_BurnEffect : MonoBehaviour
{
    private float _remain; // ʣ�����ʱ��
    private float _dps;    // ÿ���˺�
    private float _tick;   // ÿ������ʱ��

    /// <summary>ʩ��ȼ��</summary>
    public void Apply(float dps, float duration)
    {
        _dps = dps;                                      // ��¼ÿ���˺�
        _remain = Mathf.Max(_remain, duration);          // ����ˢ�³���ʱ�䣨ȡ������
    }

    private void Update()
    {
        if (_remain <= 0f) return;                       // û��ȼ���򷵻�
        _remain -= Time.deltaTime;                       // ����ʱ��ݼ�
        _tick += Time.deltaTime;                         // ÿ���ʱ

        if (_tick >= 1f)                                 // ÿ�봥��һ��
        {
            _tick = 0f;                                  // ��������
            var dmg = GetComponent<SM_IDamageable>();    // ��ȡ�����˽ӿ�
            if (dmg != null)                             // ����������˺�
            {
                dmg.ApplyDamage(new SM_DamageInfo
                {
                    Amount = _dps,                       // �����˺�
                    Element = SM_Element.Fire,           // ��Ԫ��
                    IgnoreDefense = false,               // �����˺������ӷ���
                    CritChance = 0f,                     // �ޱ���
                    CritMultiplier = 1f                  // ����1
                });
            }
        }
    }
}