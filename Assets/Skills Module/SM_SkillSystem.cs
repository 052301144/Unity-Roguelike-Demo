using UnityEngine;                // Unity 引擎命名空间
using System.Collections.Generic; // 使用字典

/// <summary>
/// 技能系统类
/// - 提供 4 个技能槽位（U/I/O/L）
/// - 提供 MP 管理（自动回复）
/// - 提供 ICharacterProvider 接口以供外部（如UI）使用
/// - 通过事件总线解耦，模块间通信（不影响其他模块）
/// - 依赖瞄准方向/AimDirection，由外部角色控制器每帧调用 SetAim() 设置，
///   如果外部没有调用，则使用默认方向（1,0）（即右方向）
/// </summary>
public class SM_SkillSystem : MonoBehaviour, SM_ICharacterProvider
{
    [Header("瞄准 & MP")]
    public Transform aimOrigin;           // 发射原点（通常交给角色控制器或角色中心）
    public Vector2 defaultAim = Vector2.right; // 默认（角色没方向时使用）
    public float maxMP = 100f;            // 最大 MP
    public float mpRegenPerSec = 5f;      // 每秒回复
    [SerializeField] private float currentMP = 100f; // 当前 MP（序列化后可在运行时观察）

    [Header("技能槽位对应技能（U/I/O/L）")]
    public SM_BaseSkill slotU;            // U 槽技能
    public SM_BaseSkill slotI;            // I 槽技能
    public SM_BaseSkill slotO;            // O 槽技能
    public SM_BaseSkill slotL;            // L 槽技能

    // 槽位映射，运行时
    private readonly Dictionary<KeyCode, SM_BaseSkill> _map = new Dictionary<KeyCode, SM_BaseSkill>();

    // 存储外部设置的瞄准
    private Vector2 _aimDir;

    // ========== SM_ICharacterProvider 实现 ==========
    public Transform AimOrigin => aimOrigin != null ? aimOrigin : transform;   // 如果没设置，会回退到本身
    public Vector2 AimDirection => _aimDir == Vector2.zero ? defaultAim : _aimDir; // 没有设置过，使用默认
    public float CurrentMP => currentMP;                                       // 当前 MP
    public float MaxMP => maxMP;                                               // 最大 MP

    public bool ConsumeMP(float amount)                                        // 消费 MP
    {
        if (currentMP < amount) return false;                                  // 不够
        currentMP -= amount;                                                   // 扣除
        SM_SkillEventBus.RaiseMPChanged(currentMP, maxMP);                     // 广播 MP 变化
        return true;                                                           // 成功
    }

    // ========== 生命周期 ==========
    private void Awake()
    {
        currentMP = Mathf.Clamp(currentMP, 0, maxMP);                          // 校验
        // 初始化槽位映射
        _map[KeyCode.U] = slotU;
        _map[KeyCode.I] = slotI;
        _map[KeyCode.O] = slotO;
        _map[KeyCode.L] = slotL;

        // 注入角色信息
        foreach (var kv in _map)
        {
            if (kv.Value != null) kv.Value.Initialize(this);
        }

        // 初始化广播 MP
        SM_SkillEventBus.RaiseMPChanged(currentMP, maxMP);
    }

    public void Equip(KeyCode key, SM_BaseSkill skill)                         // 运行时装备（暂未用）
    {
        _map[key] = skill;                                                     // 替换映射
        if (skill != null) skill.Initialize(this);                             // 注入
    }

    /// <summary>
    /// 外部（例如 PlayerController）每帧可调用此方法，更新瞄准/朝向方向
    /// 也可以不调用，系统会使用 defaultAim
    /// </summary>
    public void SetAim(Vector2 dir)
    {
        _aimDir = dir.normalized;                                              // 归一化后保存
    }

    private void Update()
    {
        // MP 回复
        if (currentMP < maxMP)
        {
            currentMP = Mathf.Min(maxMP, currentMP + mpRegenPerSec * Time.deltaTime);
            SM_SkillEventBus.RaiseMPChanged(currentMP, maxMP);                 // 广播 MP 回复
        }

        // 更新冷却/技能 Tick
        foreach (var kv in _map)
        {
            if (kv.Value != null) kv.Value.Tick(Time.deltaTime);
        }

        // 监听按键（U/I/O/L）
        if (Input.GetKeyDown(KeyCode.U)) TryCast(KeyCode.U);
        if (Input.GetKeyDown(KeyCode.I)) TryCast(KeyCode.I);
        if (Input.GetKeyDown(KeyCode.O)) TryCast(KeyCode.O);
        if (Input.GetKeyDown(KeyCode.L)) TryCast(KeyCode.L);
    }

    private void TryCast(KeyCode key)
    {
        if (!_map.TryGetValue(key, out var skill) || skill == null) return;    // 未装备或为空
        skill.TryCast();                                                        // 调用技能内部方法施放
    }
}
