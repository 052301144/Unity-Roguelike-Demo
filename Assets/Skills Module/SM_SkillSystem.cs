using UnityEngine;                // Unity �����ռ�
using System.Collections.Generic; // ʹ���ֵ�

/// <summary>
/// ����ϵͳ��
/// - �ṩ 4 ������λ��U/I/O/L��
/// - �ṩ MP ���ƣ�������������
/// - �ṩ ICharacterProvider ������ʹ�ã�ֻ����
/// - ��ͨ���¼����߶���㲥�����޸�����ģ��
/// - ���泯����/AimDirection�������ⲿ��ɫ������ÿ֡���� SetAim() ���ã�
///   ���ⲿû���ã���Ĭ�����ң�1,0��
/// </summary>
public class SM_SkillSystem : MonoBehaviour, SM_ICharacterProvider
{
    [Header("�� & MP")]
    public Transform aimOrigin;           // �ͷ���㣨�����Ͻ�ɫ�ֲ����ɫ���ģ�
    public Vector2 defaultAim = Vector2.right; // Ĭ�ϳ���û����ʱ�ã�
    public float maxMP = 100f;            // ��� MP
    public float mpRegenPerSec = 5f;      // ÿ�����
    [SerializeField] private float currentMP = 100f; // ��ǰ MP�����л����ڹ۲죩

    [Header("����λ����Ӧ������U/I/O/L��")]
    public SM_BaseSkill slotU;            // U ������
    public SM_BaseSkill slotI;            // I ������
    public SM_BaseSkill slotO;            // O ������
    public SM_BaseSkill slotL;            // L ������

    // ��λӳ��������ʱ��
    private readonly Dictionary<KeyCode, SM_BaseSkill> _map = new Dictionary<KeyCode, SM_BaseSkill>();

    // �����ⲿ���õĳ���
    private Vector2 _aimDir;

    // ========== SM_ICharacterProvider ʵ�� ==========
    public Transform AimOrigin => aimOrigin != null ? aimOrigin : transform;   // ���û�ϣ��˻�Ϊ����
    public Vector2 AimDirection => _aimDir == Vector2.zero ? defaultAim : _aimDir; // û��������Ĭ��
    public float CurrentMP => currentMP;                                       // ��ǰ MP
    public float MaxMP => maxMP;                                               // ��� MP

    public bool ConsumeMP(float amount)                                        // ���� MP
    {
        if (currentMP < amount) return false;                                  // ����
        currentMP -= amount;                                                   // �۳�
        SM_SkillEventBus.RaiseMPChanged(currentMP, maxMP);                     // �㲥 MP �仯
        return true;                                                           // �ɹ�
    }

    // ========== �������� ==========
    private void Awake()
    {
        currentMP = Mathf.Clamp(currentMP, 0, maxMP);                          // ����
        // ������λӳ��
        _map[KeyCode.U] = slotU;
        _map[KeyCode.I] = slotI;
        _map[KeyCode.O] = slotO;
        _map[KeyCode.L] = slotL;

        // ע���ɫֻ����Ϣ
        foreach (var kv in _map)
        {
            if (kv.Value != null) kv.Value.Initialize(this);
        }

        // ���ι㲥 MP
        SM_SkillEventBus.RaiseMPChanged(currentMP, maxMP);
    }

    public void Equip(KeyCode key, SM_BaseSkill skill)                         // ����ʱ��װ����ѡ��
    {
        _map[key] = skill;                                                     // �滻ӳ��
        if (skill != null) skill.Initialize(this);                             // ע��
    }

    /// <summary>
    /// �ⲿ������ PlayerController��ÿ֡�ɵ��ô˷���������׼/�泯����
    /// Ҳ���Բ����ã���ϵͳ��ʹ�� defaultAim��
    /// </summary>
    public void SetAim(Vector2 dir)
    {
        _aimDir = dir.normalized;                                              // ��һ������
    }

    private void Update()
    {
        // ��������
        if (currentMP < maxMP)
        {
            currentMP = Mathf.Min(maxMP, currentMP + mpRegenPerSec * Time.deltaTime);
            SM_SkillEventBus.RaiseMPChanged(currentMP, maxMP);                 // �㲥 MP ����
        }

        // ������ȴ/���� Tick
        foreach (var kv in _map)
        {
            if (kv.Value != null) kv.Value.Tick(Time.deltaTime);
        }

        // �����⣨U/I/O/L��
        if (Input.GetKeyDown(KeyCode.U)) TryCast(KeyCode.U);
        if (Input.GetKeyDown(KeyCode.I)) TryCast(KeyCode.I);
        if (Input.GetKeyDown(KeyCode.O)) TryCast(KeyCode.O);
        if (Input.GetKeyDown(KeyCode.L)) TryCast(KeyCode.L);
    }

    private void TryCast(KeyCode key)
    {
        if (!_map.TryGetValue(key, out var skill) || skill == null) return;    // δװ���������
        skill.TryCast();                                                        // ��������������ʩ��
    }
}