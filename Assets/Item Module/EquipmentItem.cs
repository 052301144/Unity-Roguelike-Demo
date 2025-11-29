using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Equipment item definition extending ItemBase.
/// Adds defensive stats and special resistances.
/// </summary>
[CreateAssetMenu(fileName = "Equipment_", menuName = "Items/Equipment")]
public class EquipmentItem : ItemBase
{
    [Header("Core Stats")]
    [Min(0)]
    [SerializeField] private int defense = 0;
    [Min(0)]
    [SerializeField] private int maxHealthBonus = 0;

    [Header("Special Attributes")]
    [Min(0f)]
    [SerializeField] private float elementalResistance = 0f; // flat or percentage based on design
    [Range(0f, 1f)]
    [SerializeField] private float damageReduction = 0f; // 0-1 fraction of damage reduced

    [Tooltip("Additional named attributes (e.g., block chance, regen).")]
    [SerializeField] private List<EquipmentAttribute> extraAttributes = new List<EquipmentAttribute>();

    public int Defense => defense;
    public int MaxHealthBonus => maxHealthBonus;
    public float ElementalResistance => elementalResistance;
    public float DamageReduction => Mathf.Clamp01(damageReduction);
    public IReadOnlyList<EquipmentAttribute> ExtraAttributes => extraAttributes;
}

[System.Serializable]
public struct EquipmentAttribute
{
    public string name;
    public float value;
}
