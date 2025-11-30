using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared enhancement data structures for weapons/equipment.
/// Configure per-item max level and material costs per target level.
/// </summary>
[System.Serializable]
public struct EnhancementMaterialCost
{
    public string itemId;
    public int amount;
}

[System.Serializable]
public struct EnhancementStepCost
{
    [Min(1)]
    public int targetLevel;
    public List<EnhancementMaterialCost> materials;
}

/// <summary>
/// Helper to consume materials for an enhancement attempt.
/// </summary>
public static class ItemEnhancementHelper
{
    /// <summary>
    /// Checks whether the inventory has enough materials for the given cost list.
    /// </summary>
    public static bool HasMaterials(InventoryManager inventory, IReadOnlyList<EnhancementMaterialCost> costs)
    {
        if (inventory == null || costs == null) return false;
        foreach (var cost in costs)
        {
            if (string.IsNullOrEmpty(cost.itemId) || cost.amount <= 0) continue;
            // InventoryManager exposes counts via dictionary; if missing, treat as 0.
            var snapshot = inventory.GetItemsSnapshot();
            if (!snapshot.TryGetValue(cost.itemId, out var have) || have < cost.amount)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Attempts to consume materials from the inventory; returns true on success.
    /// </summary>
    public static bool TryConsumeMaterials(InventoryManager inventory, IReadOnlyList<EnhancementMaterialCost> costs)
    {
        if (!HasMaterials(inventory, costs)) return false;
        foreach (var cost in costs)
        {
            if (string.IsNullOrEmpty(cost.itemId) || cost.amount <= 0) continue;
            inventory.RemoveItem(cost.itemId, cost.amount);
        }
        return true;
    }
}
