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
    [SerializeField] private PlayerItemManager playerItemManager;

    [Header("Options")]
    [SerializeField] private bool loadOnStart = false;
    [SerializeField] private bool teleportPlayer = true;
    [Tooltip("If true and scene name differs from saved mapId, will try to load that scene before applying data.")]
    [SerializeField] private bool loadSceneIfDifferent = false;
    [Tooltip("How many frames to wait after scene load for player/inventory to spawn before giving up.")]
    [SerializeField] private int applyRetryFrames = 60;

    private SaveData pendingData;

    private void Awake()
    {
        // Ensure we mark the root object so DontDestroyOnLoad is valid even if this is on a child.
        var root = transform.root != null ? transform.root.gameObject : gameObject;
        DontDestroyOnLoad(root);
    }

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
        pendingData = SaveSystem.Load();
        if (pendingData == null)
        {
            Debug.LogWarning("[LoadGameHandler] No data loaded.");
            return;
        }
        Debug.Log($"[LoadGameHandler] Loaded save. Map: {pendingData.player?.mapId}, " +
                  $"HP:{pendingData.player?.health}, Mana:{pendingData.player?.mana}, Coins:{pendingData.player?.coins}, " +
                  $"Pos:{pendingData.player?.position.x},{pendingData.player?.position.y},{pendingData.player?.position.z}");

        // Optional scene switch; if used, apply data again after scene load completes.
        if (loadSceneIfDifferent &&
            pendingData.player != null &&
            !string.IsNullOrEmpty(pendingData.player.mapId) &&
            SceneManager.GetActiveScene().name != pendingData.player.mapId)
        {
            Debug.Log($"[LoadGameHandler] Loading scene {pendingData.player.mapId} from save...");
            SceneManager.sceneLoaded += OnSceneLoadedApplyData;
            SceneManager.LoadSceneAsync(pendingData.player.mapId);
            return;
        }

        ApplyDataWhenReady();
    }

    private void OnSceneLoadedApplyData(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoadedApplyData;
        if (pendingData == null)
        {
            pendingData = SaveSystem.Load();
        }
        ApplyDataWhenReady();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoadedApplyData;
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

        if (playerItemManager == null)
        {
            playerItemManager = FindObjectOfType<PlayerItemManager>();
        }
    }

    private bool HasEssentialReferences()
    {
        // Allow inventory to be missing; still apply player state.
        return player != null && playerAttribute != null;
    }

    private void ApplyDataWhenReady()
    {
        Debug.Log("[LoadGameHandler] ApplyDataWhenReady started.");
        StartCoroutine(ApplyDataCoroutine());
    }

    private System.Collections.IEnumerator ApplyDataCoroutine()
    {
        int framesLeft = applyRetryFrames;
        while (framesLeft-- > 0)
        {
            EnsureReferences();
            if (HasEssentialReferences())
                break;
            yield return null; // wait a frame for objects to spawn/enable
        }

        if (!HasEssentialReferences())
        {
            Debug.LogWarning("[LoadGameHandler] Failed to find player/attribute after scene load; data not applied.");
            yield break;
        }

        if (inventory == null)
        {
            Debug.LogWarning("[LoadGameHandler] InventoryManager not found; applying player state/position only.");
        }

        Debug.Log("[LoadGameHandler] References resolved, applying data now.");
        ApplyData(pendingData);
        pendingData = null;
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
            Debug.Log($"[LoadGameHandler] Applying HP. Current:{currentHealth}, Saved:{clampedHealth}, Delta:{deltaHealth}");
            if (deltaHealth > 0)
                playerAttribute.Heal(deltaHealth);
            else if (deltaHealth < 0)
                playerAttribute.TakeDamage(-deltaHealth, gameObject);

            int targetMana = Mathf.RoundToInt(playerData.mana);
            int clampedMana = Mathf.Clamp(targetMana, 0, playerAttribute.MaxMana);
            int currentMana = playerAttribute.CurrentMana;
            int deltaMana = clampedMana - currentMana;
            Debug.Log($"[LoadGameHandler] Applying Mana. Current:{currentMana}, Saved:{clampedMana}, Delta:{deltaMana}");
            if (deltaMana > 0)
                playerAttribute.AddMana(deltaMana);
            else if (deltaMana < 0)
                playerAttribute.SpendMana(-deltaMana);
        }
        else
        {
            Debug.LogWarning("[LoadGameHandler] playerAttribute missing, cannot apply HP/Mana.");
        }

        if (player != null && teleportPlayer)
        {
            player.transform.position = playerData.position.ToVector3();
            Debug.Log($"[LoadGameHandler] Teleport player to {playerData.position.x},{playerData.position.y},{playerData.position.z}");
        }
        else if (player == null)
        {
            Debug.LogWarning("[LoadGameHandler] player missing, cannot teleport or apply stats.");
        }
    }

    private void ApplyInventory(InventoryData inventoryData, PlayerStateData playerData)
    {
        // 优先应用到 PlayerItemManager，如果不存在则退回 InventoryManager
        if (playerItemManager != null)
        {
            playerItemManager.LoadInventoryFromSave(inventoryData != null ? inventoryData.items : null);
        }
        else if (inventory != null)
        {
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
        else
        {
            Debug.LogWarning("[LoadGameHandler] No inventory component found; skip applying items/coins.");
        }
    }
}
