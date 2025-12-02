using UnityEngine;

/// <summary>
/// Consumable item for healing or mana restore.
/// </summary>
[CreateAssetMenu(fileName = "Consumable_", menuName = "Items/Consumable")]
public class ConsumableItem : ItemBase
{
    [Header("Effect")]
    [SerializeField] private int healAmount = 0;
    [SerializeField] private int manaAmount = 0;

    public int HealAmount => Mathf.Max(0, healAmount);
    public int ManaAmount => Mathf.Max(0, manaAmount);
}
