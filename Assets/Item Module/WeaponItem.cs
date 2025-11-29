using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Weapon item definition extending ItemBase.
/// Adds combat-specific stats like attack, enhancement level, and special attributes.
/// </summary>
[CreateAssetMenu(fileName = "Weapon_", menuName = "Items/Weapon")]
public class WeaponItem : ItemBase
{
    [Header("Core Stats")]
    [Min(0)]
    [SerializeField] private int baseAttack = 1;
    [Min(0)]
    [SerializeField] private int enhancementLevel = 0;

    [Header("Special Attributes")]
    [Min(0f)]
    [SerializeField] private float elementalAttack = 0f;
    [Range(0f, 1f)]
    [SerializeField] private float criticalChance = 0f; // 0-1 range
    [Range(0f, 1f)]
    [SerializeField] private float defenseIgnore = 0f; // 0-1 range

    [Tooltip("Additional named attributes (e.g., lifesteal, attack speed).")]
    [SerializeField] private List<WeaponAttribute> extraAttributes = new List<WeaponAttribute>();

    public int BaseAttack => baseAttack;
    public int EnhancementLevel => enhancementLevel;
    public float ElementalAttack => elementalAttack;
    public float CriticalChance => Mathf.Clamp01(criticalChance);
    public float DefenseIgnore => Mathf.Clamp01(defenseIgnore);
    public IReadOnlyList<WeaponAttribute> ExtraAttributes => extraAttributes;
}

[System.Serializable]
public struct WeaponAttribute
{
    public string name;
    public float value;
}
