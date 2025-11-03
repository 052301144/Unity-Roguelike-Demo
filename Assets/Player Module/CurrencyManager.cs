using UnityEngine;

/// <summary>
/// 货币管理器 - 管理玩家的金币系统
/// 负责接收金币拾取事件并实时更新玩家持有的金币数量
/// </summary>
public class CurrencyManager : MonoBehaviour
{
    [Header("金币设置")]
    [SerializeField] private int currentCoins = 0; // 当前持有的金币数量
    [SerializeField] private int initialCoins = 0;  // 初始金币数量（用于重置）
    
    [Header("调试设置")]
    [SerializeField] private bool logCoinEvents = true; // 是否在控制台输出金币事件

    // 公共属性
    public int CurrentCoins => currentCoins;
    
    // 事件系统
    public System.Action<int> OnCoinsChanged;  // 金币数量变化事件（参数为变化量）
    public System.Action<int> OnCoinsUpdated;  // 金币数量更新事件（参数为当前总数）

    void Awake()
    {
        // 初始化金币数量
        currentCoins = initialCoins;
    }

    void Start()
    {
        // 订阅掉落系统的金币收集事件
        if (DropManager.Instance != null)
        {
            DropManager.Instance.OnCoinCollected += AddCoins;
            if (logCoinEvents)
            {
                Debug.Log("[CurrencyManager] 已订阅掉落系统的金币收集事件");
            }
        }
        else
        {
            Debug.LogWarning("[CurrencyManager] DropManager未找到，无法订阅金币收集事件");
        }
    }

    void OnDestroy()
    {
        // 取消订阅事件
        if (DropManager.Instance != null)
        {
            DropManager.Instance.OnCoinCollected -= AddCoins;
        }
    }

    /// <summary>
    /// 添加金币
    /// </summary>
    /// <param name="amount">要添加的金币数量</param>
    public void AddCoins(int amount)
    {
        if (amount <= 0)
        {
            if (logCoinEvents)
            {
                Debug.LogWarning($"[CurrencyManager] 尝试添加无效的金币数量: {amount}");
            }
            return;
        }

        currentCoins += amount;
        
        // 触发事件
        OnCoinsChanged?.Invoke(amount);
        OnCoinsUpdated?.Invoke(currentCoins);
        
        if (logCoinEvents)
        {
            Debug.Log($"[CurrencyManager] 获得金币: {amount}，当前总数: {currentCoins}");
        }
    }

    /// <summary>
    /// 消费金币
    /// </summary>
    /// <param name="amount">要消费的金币数量</param>
    /// <returns>是否成功消费（金币不足时返回false）</returns>
    public bool SpendCoins(int amount)
    {
        if (amount <= 0)
        {
            if (logCoinEvents)
            {
                Debug.LogWarning($"[CurrencyManager] 尝试消费无效的金币数量: {amount}");
            }
            return false;
        }

        if (currentCoins < amount)
        {
            if (logCoinEvents)
            {
                Debug.LogWarning($"[CurrencyManager] 金币不足！当前: {currentCoins}，需要: {amount}");
            }
            return false;
        }

        currentCoins -= amount;
        
        // 触发事件
        OnCoinsChanged?.Invoke(-amount);
        OnCoinsUpdated?.Invoke(currentCoins);
        
        if (logCoinEvents)
        {
            Debug.Log($"[CurrencyManager] 消费金币: {amount}，剩余: {currentCoins}");
        }
        
        return true;
    }

    /// <summary>
    /// 设置金币数量（用于重置或直接设置）
    /// </summary>
    /// <param name="amount">要设置的金币数量</param>
    public void SetCoins(int amount)
    {
        if (amount < 0)
        {
            amount = 0;
        }

        int change = amount - currentCoins;
        currentCoins = amount;
        
        // 触发事件
        if (change != 0)
        {
            OnCoinsChanged?.Invoke(change);
            OnCoinsUpdated?.Invoke(currentCoins);
        }
        
        if (logCoinEvents)
        {
            Debug.Log($"[CurrencyManager] 设置金币数量: {currentCoins}");
        }
    }

    /// <summary>
    /// 重置金币数量为初始值
    /// </summary>
    public void ResetCoins()
    {
        SetCoins(initialCoins);
    }

    /// <summary>
    /// 检查是否有足够的金币
    /// </summary>
    /// <param name="amount">需要检查的金币数量</param>
    /// <returns>是否有足够的金币</returns>
    public bool HasEnoughCoins(int amount)
    {
        return currentCoins >= amount;
    }
}
