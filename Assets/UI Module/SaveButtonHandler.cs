using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Hook this to a UI Button (OnClick) to save current player state.
/// </summary>
public class SaveButtonHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private Attribute playerAttribute;
    [SerializeField] private InventoryManager inventory;

    /// <summary>
    /// Called by UI Button OnClick.
    /// </summary>
    public void SaveGame()
    {
        if (player == null)
        {
            player = FindObjectOfType<PlayerController>();
        }

        if (playerAttribute == null && player != null)
        {
            playerAttribute = player.attributeComponent;
        }

        if (inventory == null)
        {
            inventory = InventoryManager.Instance ?? FindObjectOfType<InventoryManager>();
        }

        var data = new SaveData
        {
            player = new PlayerStateData
            {
                health = playerAttribute != null ? playerAttribute.CurrentHealth : 0f,
                mana = playerAttribute != null ? playerAttribute.CurrentMana : 0f,
                coins = inventory != null ? inventory.GetCoins() : 0,
                mapId = SceneManager.GetActiveScene().name,
                position = player != null ? new SerializableVector3(player.transform.position) : default
            },
            inventory = new InventoryData
            {
                items = BuildInventoryItems()
            }
        };

        SaveSystem.Save(data);
    }

    private List<ItemStackData> BuildInventoryItems()
    {
        var list = new List<ItemStackData>();
        if (inventory == null)
        {
            inventory = InventoryManager.Instance ?? FindObjectOfType<InventoryManager>();
        }

        if (inventory == null)
        {
            Debug.LogWarning("[SaveButtonHandler] InventoryManager not found, inventory will not be saved.");
            return list;
        }

        var snapshot = inventory.GetItemsSnapshot();
        foreach (var kvp in snapshot)
        {
            if (string.IsNullOrEmpty(kvp.Key) || kvp.Value <= 0) continue;
            list.Add(new ItemStackData
            {
                itemId = kvp.Key,
                count = kvp.Value,
                enhancementLevel = 0,
                durability = 0
            });
        }

        return list;
    }
}
