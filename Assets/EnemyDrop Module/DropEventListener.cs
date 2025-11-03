using UnityEngine; // 引入Unity引擎命名空间

/// <summary>
/// 掉落事件监听器 - 监听掉落模块中的所有事件并显示日志
/// </summary>
public class DropEventListener : MonoBehaviour
{
    /// <summary>
    /// Start方法 - 在对象激活时调用
    /// </summary>
    void Start()
    {
        // 注册事件监听 - 检查掉落管理器是否存在
        if (DropManager.Instance != null)
        {
            // 注册金币收集事件的处理函数
            DropManager.Instance.OnCoinCollected += OnCoinCollected;
            // 注册生命值恢复事件的处理函数
            DropManager.Instance.OnHealthRestored += OnHealthRestored;
            // 注册魔法值恢复事件的处理函数
            DropManager.Instance.OnManaRestored += OnManaRestored;

            // 输出注册成功信息
            Debug.Log("掉落事件监听器已注册所有事件");
        }
        else // 如果掉落管理器不存在
        {
            // 输出警告信息
            Debug.LogWarning("掉落管理器未找到，无法注册事件监听器");
        }
    }

    /// <summary>
    /// OnDestroy方法 - 在对象销毁时调用
    /// </summary>
    void OnDestroy()
    {
        // 取消注册事件监听 - 检查掉落管理器是否存在
        if (DropManager.Instance != null)
        {
            // 取消注册金币收集事件
            DropManager.Instance.OnCoinCollected -= OnCoinCollected;
            // 取消注册生命值恢复事件
            DropManager.Instance.OnHealthRestored -= OnHealthRestored;
            // 取消注册魔法值恢复事件
            DropManager.Instance.OnManaRestored -= OnManaRestored;

            // 输出取消注册信息
            Debug.Log("掉落事件监听器已取消所有事件注册");
        }
    }

    /// <summary>
    /// 金币收集事件处理函数
    /// </summary>
    /// <param name="amount">收集的金币数量</param>
    private void OnCoinCollected(int amount)
    {
        // 这里应该连接到玩家的金币系统
        Debug.Log("金币系统: 获得 " + amount + " 金币");

        // 示例代码 - 实际使用时应该连接到真实的金币系统
        // 如果有金币管理器组件
        // CoinManager coinManager = FindObjectOfType<CoinManager>();
        // if (coinManager != null) 
        // {
        //     coinManager.AddCoins(amount);
        // }

        // 或者使用单例模式
        // CoinManager.Instance.AddCoins(amount);
    }

    /// <summary>
    /// 生命值恢复事件处理函数
    /// </summary>
    /// <param name="amount">恢复的生命值数量</param>
    private void OnHealthRestored(int amount)
    {
        // 这里应该连接到玩家的生命值系统
        Debug.Log("生命值系统: 恢复 " + amount + " 生命值");

        // 示例代码 - 实际使用时应该连接到真实的生命值系统
        // 如果玩家的生命值组件
        // HealthComponent health = GameObject.FindGameObjectWithTag("Player").GetComponent<HealthComponent>();
        // if (health != null) 
        // {
        //     health.Heal(amount);
        // }

        // 或者使用事件总线系统
        // EventBus.Publish(new HealthRestoredEvent(amount));
    }

    /// <summary>
    /// 魔法值恢复事件处理函数
    /// </summary>
    /// <param name="amount">恢复的魔法值数量</param>
    private void OnManaRestored(int amount)
    {
        // 这里应该连接到玩家的魔法值系统
        Debug.Log("魔法值系统: 恢复 " + amount + " 魔法值");

        // 示例代码 - 实际使用时应该连接到真实的魔法值系统
        // 如果玩家的魔法值组件
        // ManaComponent mana = GameObject.FindGameObjectWithTag("Player").GetComponent<ManaComponent>();
        // if (mana != null) 
        // {
        //     mana.RestoreMana(amount);
        // }

        // 或者使用现有的Attribute组件（如果有的话）
        // Attribute playerAttribute = GameObject.FindGameObjectWithTag("Player").GetComponent<Attribute>();
        // if (playerAttribute != null)
        // {
        //     // 需要Attribute组件中实现恢复魔法值的方法
        //     playerAttribute.RestoreMana(amount);
        // }
    }
}
