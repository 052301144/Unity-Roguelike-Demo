using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Skill system:
/// - provides 4 slots (U/I/O/L)
/// - handles MP regen and broadcasts MP/skill events
/// - exposes character info via SM_ICharacterProvider
/// - lets external callers update aim each frame
/// </summary>
public class SM_SkillSystem : MonoBehaviour, SM_ICharacterProvider
{
    [Header("Aim & MP")]
    public Transform aimOrigin;
    public Vector2 defaultAim = Vector2.right;
    public float maxMP = 100f;
    public float mpRegenPerSec = 5f;
    [SerializeField] private float currentMP = 100f;

    [Header("Skill Slots (U/I/O/L)")]
    public SM_BaseSkill slotU;
    public SM_BaseSkill slotI;
    public SM_BaseSkill slotO;
    public SM_BaseSkill slotL;

    // runtime map
    private readonly Dictionary<KeyCode, SM_BaseSkill> _map = new Dictionary<KeyCode, SM_BaseSkill>();

    // cached aim
    private Vector2 _aimDir;

    // ========== SM_ICharacterProvider ==========
    public Transform AimOrigin => aimOrigin != null ? aimOrigin : transform;
    public Vector2 AimDirection => _aimDir == Vector2.zero ? defaultAim : _aimDir;
    public float CurrentMP => currentMP;
    public float MaxMP => maxMP;

    public bool ConsumeMP(float amount)
    {
        if (currentMP < amount) return false;
        currentMP -= amount;
        SM_SkillEventBus.RaiseMPChanged(currentMP, maxMP);
        return true;
    }

    // ========== Unity lifecycle ==========
    private void Awake()
    {
        currentMP = Mathf.Clamp(currentMP, 0, maxMP);

        _map[KeyCode.U] = slotU;
        _map[KeyCode.I] = slotI;
        _map[KeyCode.O] = slotO;
        _map[KeyCode.L] = slotL;

        foreach (var kv in _map)
        {
            if (kv.Value != null) kv.Value.Initialize(this);
        }

        SM_SkillEventBus.RaiseMPChanged(currentMP, maxMP);
    }

    /// <summary>
    /// Equip or replace a skill at runtime. L is reserved for displacement skills that are not implemented yet.
    /// </summary>
    public void Equip(KeyCode key, SM_BaseSkill skill)
    {
        if (key == KeyCode.L && skill != null)
        {
            Debug.LogWarning("[SM_SkillSystem] L slot is reserved for movement skills and is not available yet; clearing assignment.");
            skill = null;
        }

        _map[key] = skill;

        // Keep slot fields in sync for UI binding.
        switch (key)
        {
            case KeyCode.U:
                slotU = skill;
                break;
            case KeyCode.I:
                slotI = skill;
                break;
            case KeyCode.O:
                slotO = skill;
                break;
            case KeyCode.L:
                slotL = skill;
                break;
        }

        if (skill != null) skill.Initialize(this);
    }

    /// <summary>
    /// External caller (e.g. PlayerController) updates aim each frame; falls back to defaultAim when not provided.
    /// </summary>
    public void SetAim(Vector2 dir)
    {
        _aimDir = dir.normalized;
    }

    private void Update()
    {
        if (currentMP < maxMP)
        {
            currentMP = Mathf.Min(maxMP, currentMP + mpRegenPerSec * Time.deltaTime);
            SM_SkillEventBus.RaiseMPChanged(currentMP, maxMP);
        }

        foreach (var kv in _map)
        {
            if (kv.Value != null) kv.Value.Tick(Time.deltaTime);
        }

        if (Input.GetKeyDown(KeyCode.U)) TryCast(KeyCode.U);
        if (Input.GetKeyDown(KeyCode.I)) TryCast(KeyCode.I);
        if (Input.GetKeyDown(KeyCode.O)) TryCast(KeyCode.O);
        if (Input.GetKeyDown(KeyCode.L)) TryCast(KeyCode.L);
    }

    private void TryCast(KeyCode key)
    {
        if (!_map.TryGetValue(key, out var skill) || skill == null)
        {
            Debug.LogWarning($"[SM_SkillSystem] Key {key} has no skill bound or is null.");
            return;
        }

        var ok = skill.TryCast();
        if (ok)
        {
            Debug.Log($"[SM_SkillSystem] Cast skill {skill.SkillName} via key {key}.");
        }
        else
        {
            Debug.LogWarning($"[SM_SkillSystem] Failed to cast skill {skill.SkillName} via key {key}.");
        }
    }
}
