using UnityEngine;                // Unity 命名空间
using System.Collections.Generic; // 使用字典

/// <summary>
/// 技能系统：
/// - 提供 4 个技能位（U/I/O/L）
/// - 提供 MP 机制（含被动回蓝）
/// - 提供 ICharacterProvider 给技能使用（只读）
/// - 仅通过事件总线对外广播，不修改其他模块
/// - “面朝方向/AimDirection”可由外部角色控制在每帧调用 SetAim() 设置；
///   若外部没设置，则默认向右（1,0）
/// </summary>
public class SM_SkillSystem : MonoBehaviour, SM_ICharacterProvider
{
    [Header("绑定 & MP")]
    public Transform aimOrigin;           // 释放起点（建议拖角色手部或角色中心）
    public Vector2 defaultAim = Vector2.right; // 默认朝向（没设置时用）
    public float maxMP = 100f;            // 最大 MP
    public float mpRegenPerSec = 5f;      // 每秒回蓝
    [SerializeField] private float currentMP = 100f; // 当前 MP（序列化便于观察）

    [Header("技能位（对应按键：U/I/O/L）")]
    public SM_BaseSkill slotU;            // U 键技能
    public SM_BaseSkill slotI;            // I 键技能
    public SM_BaseSkill slotO;            // O 键技能
    public SM_BaseSkill slotL;            // L 键技能

    // 键位映射表（运行时）
    private readonly Dictionary<KeyCode, SM_BaseSkill> _map = new Dictionary<KeyCode, SM_BaseSkill>();

    // 缓存外部设置的朝向
    private Vector2 _aimDir;

    // ========== SM_ICharacterProvider 实现 ==========
    public Transform AimOrigin => aimOrigin != null ? aimOrigin : transform;   // 如果没拖，退化为自身
    public Vector2 AimDirection => _aimDir == Vector2.zero ? defaultAim : _aimDir; // 没设置则用默认
    public float CurrentMP => currentMP;                                       // 当前 MP
    public float MaxMP => maxMP;                                               // 最大 MP

    public bool ConsumeMP(float amount)                                        // 消耗 MP
    {
        if (currentMP < amount) return false;                                  // 不足
        currentMP -= amount;                                                   // 扣除
        SM_SkillEventBus.RaiseMPChanged(currentMP, maxMP);                     // 广播 MP 变化
        return true;                                                           // 成功
    }

    // ========== 生命周期 ==========
    private void Awake()
    {
        currentMP = Mathf.Clamp(currentMP, 0, maxMP);                          // 防御
        // 构建键位映射
        _map[KeyCode.U] = slotU;
        _map[KeyCode.I] = slotI;
        _map[KeyCode.O] = slotO;
        _map[KeyCode.L] = slotL;

        // 注入角色只读信息
        foreach (var kv in _map)
        {
            if (kv.Value != null) kv.Value.Initialize(this);
        }

        // 初次广播 MP
        SM_SkillEventBus.RaiseMPChanged(currentMP, maxMP);
    }

    public void Equip(KeyCode key, SM_BaseSkill skill)                         // 运行时换装（可选）
    {
        _map[key] = skill;                                                     // 替换映射
        if (skill != null) skill.Initialize(this);                             // 注入
    }

    /// <summary>
    /// 外部（例如 PlayerController）每帧可调用此方法设置瞄准/面朝方向；
    /// 也可以不调用，本系统会使用 defaultAim。
    /// </summary>
    public void SetAim(Vector2 dir)
    {
        _aimDir = dir.normalized;                                              // 归一化保存
    }

    private void Update()
    {
        // 被动回蓝
        if (currentMP < maxMP)
        {
            currentMP = Mathf.Min(maxMP, currentMP + mpRegenPerSec * Time.deltaTime);
            SM_SkillEventBus.RaiseMPChanged(currentMP, maxMP);                 // 广播 MP 更新
        }

        // 技能冷却/持续 Tick
        foreach (var kv in _map)
        {
            if (kv.Value != null) kv.Value.Tick(Time.deltaTime);
        }

        // 输入检测（U/I/O/L）
        if (Input.GetKeyDown(KeyCode.U)) TryCast(KeyCode.U);
        if (Input.GetKeyDown(KeyCode.I)) TryCast(KeyCode.I);
        if (Input.GetKeyDown(KeyCode.O)) TryCast(KeyCode.O);
        if (Input.GetKeyDown(KeyCode.L)) TryCast(KeyCode.L);
    }

    private void TryCast(KeyCode key)
    {
        if (!_map.TryGetValue(key, out var skill) || skill == null) return;    // 未装技能则忽略
        skill.TryCast();                                                        // 交给技能自身检查施放
    }
}