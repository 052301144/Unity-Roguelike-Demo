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

    [Header("Enhancement")]
    [Min(0)]
    [SerializeField] private int maxEnhancementLevel = 10;
    [Tooltip("Configure the material costs for each target enhancement level. Levels beyond the list use the last entry.")]
    [SerializeField] private List<EnhancementStepCost> enhancementCosts = new List<EnhancementStepCost>();

    [Header("Special Attributes")]
    [Min(0f)]
    [SerializeField] private float elementalResistance = 0f; // flat or percentage based on design
    [Range(0f, 1f)]
    [SerializeField] private float damageReduction = 0f; // 0-1 fraction of damage reduced

    [Tooltip("Additional named attributes (e.g., block chance, regen).")]
    [SerializeField] private List<EquipmentAttribute> extraAttributes = new List<EquipmentAttribute>();

    public int Defense => defense;
    public int MaxHealthBonus => maxHealthBonus;
    public int MaxEnhancementLevel => Mathf.Max(0, maxEnhancementLevel);
    public float ElementalResistance => elementalResistance;
    public float DamageReduction => Mathf.Clamp01(damageReduction);
    public IReadOnlyList<EquipmentAttribute> ExtraAttributes => extraAttributes;

    /// <summary>
    /// Get the material costs to reach the specified target level.
    /// If no exact entry is found, returns the last defined step.
    /// </summary>
    public IReadOnlyList<EnhancementMaterialCost> GetCostForLevel(int targetLevel)
    {
        if (enhancementCosts == null || enhancementCosts.Count == 0)
            return System.Array.Empty<EnhancementMaterialCost>();

        EnhancementStepCost best = enhancementCosts[enhancementCosts.Count - 1];
        foreach (var step in enhancementCosts)
        {
            if (step.targetLevel == targetLevel)
                return step.materials;
            if (step.targetLevel > targetLevel)
                break;
            best = step;
        }
        return best.materials;
    }
}

[System.Serializable]
public struct EquipmentAttribute
{
    public string name;
    public float value;
}
