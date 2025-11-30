using UnityEngine;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    // 单例实例（全局唯一访问点）
    public static InventoryManager Instance;

    // 数据存储（仅在内存中，游戏关闭后自动清除）
    private int _coins; // 金币数量
    private Dictionary<string, int> _items = new Dictionary<string, int>(); // 物品列表

    // 数据变化事件（用于通知UI更新，你已实现UI可直接挂钩）
    public System.Action<int> OnCoinsUpdated;
    public System.Action<Dictionary<string, int>> OnItemsUpdated;

    private void Awake()
    {
        // 单例模式核心：确保整个游戏只有一个管理器实例
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 跨场景不销毁
        }
        else
        {
            Destroy(gameObject); // 防止重复创建
        }
    }

    // 金币操作（直接调用这些方法即可）
    public int GetCoins() => _coins;

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        _coins += amount;
        OnCoinsUpdated?.Invoke(_coins); // 通知UI更新
    }

    public bool SpendCoins(int amount)
    {
        if (amount <= 0 || _coins < amount) return false;
        _coins -= amount;
        OnCoinsUpdated?.Invoke(_coins); // 通知UI更新
        return true;
    }

    // 物品操作（如需扩展物品功能）
    public void AddItem(string itemId, int count)
    {
        if (string.IsNullOrEmpty(itemId) || count <= 0) return;
        if (_items.ContainsKey(itemId))
            _items[itemId] += count;
        else
            _items[itemId] = count;
        OnItemsUpdated?.Invoke(_items);
    }

    public bool RemoveItem(string itemId, int count)
    {
        if (!_items.ContainsKey(itemId) || _items[itemId] < count) return false;
        _items[itemId] -= count;
        if (_items[itemId] <= 0) _items.Remove(itemId);
        OnItemsUpdated?.Invoke(_items);
        return true;
    }

    public Dictionary<string, int> GetItemsSnapshot()
    {
        return new Dictionary<string, int>(_items);
    }

    public void ReplaceItems(Dictionary<string, int> items)
    {
        _items.Clear();
        if (items != null)
        {
            foreach (var kvp in items)
            {
                if (string.IsNullOrEmpty(kvp.Key) || kvp.Value <= 0) continue;
                _items[kvp.Key] = kvp.Value;
            }
        }
        OnItemsUpdated?.Invoke(_items);
    }

    // 新游戏时重置数据（在开始新游戏按钮调用）
    public void ResetInventory()
    {
        _coins = 0;
        _items.Clear();
        OnCoinsUpdated?.Invoke(_coins);
        OnItemsUpdated?.Invoke(_items);
    }
}