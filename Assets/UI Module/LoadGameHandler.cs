using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Load saved data from SaveSystem and apply to player/inventory.
/// Hook this to a UI Button (OnClick -> LoadGame) or enable auto-load on start.
/// </summary>
public class LoadGameHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private Attribute playerAttribute;
    [SerializeField] private InventoryManager inventory;

    [Header("Options")]
    [SerializeField] private bool loadOnStart = false;
    [SerializeField] private bool teleportPlayer = true;
    [Tooltip("If true and scene name differs from saved mapId, will try to load that scene before applying data.")]
    [SerializeField] private bool loadSceneIfDifferent = false;

    private void Start()
    {
        if (loadOnStart)
        {
            LoadGame();
        }
    }

    /// <summary>
    /// Entry point for UI Button.
    /// </summary>
    public void LoadGame()
    {
        var data = SaveSystem.Load();
        if (data == null)
        {
            Debug.LogWarning("[LoadGameHandler] No data loaded.");
            return;
        }

        // Optional scene switch; if used, apply data again after scene load completes.
        if (loadSceneIfDifferent &&
            data.player != null &&
            !string.IsNullOrEmpty(data.player.mapId) &&
            SceneManager.GetActiveScene().name != data.player.mapId)
        {
            Debug.Log($"[LoadGameHandler] Loading scene {data.player.mapId} from save...");
            SceneManager.sceneLoaded += OnSceneLoadedApplyData;
            SceneManager.LoadSceneAsync(data.player.mapId);
            return;
        }

        ApplyData(data);
    }

    private void OnSceneLoadedApplyData(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoadedApplyData;
        ApplyData(SaveSystem.Load());
    }

    private void ApplyData(SaveData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[LoadGameHandler] No data to apply.");
            return;
        }

        EnsureReferences();
        ApplyPlayerState(data.player);
        ApplyInventory(data.inventory, data.player);
    }

    private void EnsureReferences()
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
    }

    private void ApplyPlayerState(PlayerStateData playerData)
    {
        if (playerData == null) return;

        if (playerAttribute != null)
        {
            int targetHealth = Mathf.RoundToInt(playerData.health);
            int clampedHealth = Mathf.Clamp(targetHealth, 0, playerAttribute.MaxHealth);
            int currentHealth = playerAttribute.CurrentHealth;
            int deltaHealth = clampedHealth - currentHealth;
            if (deltaHealth > 0)
                playerAttribute.Heal(deltaHealth);
            else if (deltaHealth < 0)
                playerAttribute.TakeDamage(-deltaHealth, gameObject);

            int targetMana = Mathf.RoundToInt(playerData.mana);
            int clampedMana = Mathf.Clamp(targetMana, 0, playerAttribute.MaxMana);
            int currentMana = playerAttribute.CurrentMana;
            int deltaMana = clampedMana - currentMana;
            if (deltaMana > 0)
                playerAttribute.AddMana(deltaMana);
            else if (deltaMana < 0)
                playerAttribute.SpendMana(-deltaMana);
        }

        if (player != null && teleportPlayer)
        {
            player.transform.position = playerData.position.ToVector3();
        }
    }

    private void ApplyInventory(InventoryData inventoryData, PlayerStateData playerData)
    {
        if (inventory == null) return;

        inventory.ResetInventory();

        int coins = playerData != null ? playerData.coins : 0;
        if (coins > 0)
        {
            inventory.AddCoins(coins);
        }

        if (inventoryData == null || inventoryData.items == null) return;
        foreach (var item in inventoryData.items)
        {
            if (string.IsNullOrEmpty(item.itemId) || item.count <= 0) continue;
            inventory.AddItem(item.itemId, item.count);
        }
    }
}
