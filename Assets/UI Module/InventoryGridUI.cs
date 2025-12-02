using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 简单的背包网格 UI：监听 PlayerItemManager 的背包变化，生成/复用格子按钮。
/// </summary>
public class InventoryGridUI : MonoBehaviour
{
    [SerializeField] private PlayerItemManager playerItemManager;
    [SerializeField] private Transform gridParent;           // 挂 GridLayoutGroup 的容器
    [SerializeField] private InventorySlotButton slotPrefab; // 预制：Button+图标+名称+数量
    [SerializeField] private bool clearOnDisable = true;
    [Tooltip("可选：若为空将使用 Grid 的第一个子节点作为模板")]
    [SerializeField] private InventorySlotButton runtimeTemplate;

    private readonly List<InventorySlotButton> pool = new List<InventorySlotButton>();

    private void OnEnable()
    {
        if (playerItemManager == null)
            playerItemManager = FindObjectOfType<PlayerItemManager>();

        if (playerItemManager != null)
        {
            playerItemManager.OnInventoryChanged += Refresh;
            Refresh(playerItemManager.GetInventoryItems());
        }
    }

    private void OnDisable()
    {
        if (playerItemManager != null)
            playerItemManager.OnInventoryChanged -= Refresh;

        if (clearOnDisable)
        {
            foreach (var slot in pool)
            {
                if (slot != null) slot.gameObject.SetActive(false);
            }
        }
    }

    private void Refresh(IReadOnlyList<ItemData> items)
    {
        if (items == null) items = new List<ItemData>();
        EnsureTemplate();
        EnsurePoolSize(items.Count);

        for (int i = 0; i < pool.Count; i++)
        {
            if (i < items.Count)
            {
                pool[i].gameObject.SetActive(true);
                pool[i].Bind(items[i], playerItemManager);
            }
            else
            {
                pool[i].gameObject.SetActive(false);
            }
        }
    }

    private void EnsurePoolSize(int needed)
    {
        while (pool.Count < needed)
        {
            var prefab = slotPrefab != null ? slotPrefab : runtimeTemplate;
            if (prefab == null)
            {
                Debug.LogWarning("[InventoryGridUI] 没有可用的 slotPrefab/runtimeTemplate，无法生成格子。");
                return;
            }
            var slot = Instantiate(prefab, gridParent);
            pool.Add(slot);
        }
    }

    private void EnsureTemplate()
    {
        if (slotPrefab != null) return;
        if (runtimeTemplate != null) return;
        if (gridParent != null && gridParent.childCount > 0)
        {
            var child = gridParent.GetChild(0).GetComponent<InventorySlotButton>();
            if (child == null)
            {
                child = gridParent.GetChild(0).gameObject.AddComponent<InventorySlotButton>();
            }
            runtimeTemplate = child;
            runtimeTemplate.gameObject.SetActive(false);
        }
    }
}
