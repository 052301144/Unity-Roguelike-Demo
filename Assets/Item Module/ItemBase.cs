using UnityEngine;

public enum ItemQuality
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

/// <summary>
/// Base data definition for any item type (weapons, armor, consumables, etc.).
/// Holds shared fields like id, name, quality, description, and stacking rules.
/// </summary>
public abstract class ItemBase : ScriptableObject
{
    [SerializeField] private string itemId;
    [SerializeField] private string itemName; // supports Chinese names via UTF-16 strings
    [SerializeField] private ItemQuality quality = ItemQuality.Common;
    [TextArea]
    [SerializeField] private string description;

    [Header("Stacking")]
    [Min(1)]
    [SerializeField] private int maxStackSize = 1;

    public string ItemId => itemId;
    public string ItemName => itemName;
    public ItemQuality Quality => quality;
    public string Description => description;
    public int MaxStackSize => Mathf.Max(1, maxStackSize);
    public bool IsStackable => MaxStackSize > 1;
}
