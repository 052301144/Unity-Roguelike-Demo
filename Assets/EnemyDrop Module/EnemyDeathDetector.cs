using UnityEngine; // 引入Unity引擎命名空间
using System.Collections; // 引入协程命名空间
using System.Reflection; // 引入反射命名空间

/// <summary>
/// 敌人死亡检测器 - 通过反射检测敌人生命值变化并触发掉落
/// </summary>
public class EnemyDeathDetector : MonoBehaviour
{
    [Header("掉落设置")] // 掉落相关配置部分
    public DropTable dropTable;                          // 敌人的掉落表数据
    public bool enableDrops = true;                      // 是否启用掉落功能
    public Vector3 dropOffset = new Vector3(0, 0.5f, 0); // 掉落位置相对于敌人的偏移

    [Header("检测设置")] // 检测相关的配置部分
    public float healthCheckInterval = 0.5f;             // 生命值检测时间间隔（秒）
    public string healthComponentName = "Attribute";     // 生命值组件的名称

    // 私有字段 - 内部缓存变量
    private MonoBehaviour healthComponent;               // 找到的生命值组件引用
    private PropertyInfo currentHealthProperty;          // 当前生命值属性的反射信息
    private PropertyInfo maxHealthProperty;              // 最大生命值属性的反射信息
    private float lastHealth;                            // 上一帧的生命值
    private bool isDead = false;                         // 标记敌人是否已死亡

    /// <summary>
    /// Start方法 - 在对象激活时调用
    /// </summary>
    void Start()
    {
        // 查找生命值组件
        FindHealthComponent();
        // 启动生命值检测协程
        StartCoroutine(HealthCheckCoroutine());
    }

    /// <summary>
    /// 查找生命值组件，使用反射获取属性值
    /// </summary>
    void FindHealthComponent()
    {
        // 获取游戏对象上的所有MonoBehaviour组件
        MonoBehaviour[] components = GetComponents<MonoBehaviour>();
        // 遍历所有组件
        foreach (var component in components)
        {
            // 检查组件类型名称是否匹配
            if (component.GetType().Name == healthComponentName)
            {
                // 找到匹配的组件，保存引用
                healthComponent = component;
                break; // 找到后退出循环
            }
        }

        // 如果找到了生命值组件
        if (healthComponent != null)
        {
            // 获取组件的类型信息
            var type = healthComponent.GetType();
            // 使用反射获取CurrentHealth属性
            currentHealthProperty = type.GetProperty("CurrentHealth");
            // 使用反射获取MaxHealth属性
            maxHealthProperty = type.GetProperty("MaxHealth");

            // 如果成功获取到当前生命值属性
            if (currentHealthProperty != null)
            {
                // 获取初始生命值并保存
                lastHealth = (int)currentHealthProperty.GetValue(healthComponent);
            }

            // 如果找到组件，输出日志
            Debug.Log("找到生命值组件: " + healthComponent.GetType().Name);
        }
        else // 如果没有找到生命值组件
        {
            // 输出警告信息
            Debug.LogWarning("未找到生命值组件: " + healthComponentName);
        }
    }

    /// <summary>
    /// 生命值检测协程 - 用于检测敌人是否死亡
    /// </summary>
    System.Collections.IEnumerator HealthCheckCoroutine()
    {
        // 循环执行，直到敌人死亡
        while (!isDead)
        {
            // 等待指定的时间间隔
            yield return new WaitForSeconds(healthCheckInterval);

            // 如果生命值组件和当前生命值属性都存在且可用
            if (healthComponent != null && currentHealthProperty != null)
            {
                // 使用反射获取当前生命值
                float currentHealth = (int)currentHealthProperty.GetValue(healthComponent);

                // 如果检测到当前生命值<=0且上一帧生命值>0
                if (currentHealth <= 0 && lastHealth > 0)
                {
                    // 触发敌人死亡事件
                    OnEnemyDeath();
                    // 标记为已死亡
                    isDead = true;
                }

                // 更新上一帧生命值
                lastHealth = currentHealth;
            }
        }
    }

    /// <summary>
    /// 敌人死亡事件处理
    /// </summary>
    void OnEnemyDeath()
    {
        // 检查是否启用掉落且掉落表不为空
        if (enableDrops && dropTable != null)
        {
            // 执行掉落物品生成
            DropItems();
        }
    }

    /// <summary>
    /// 执行物品掉落
    /// </summary>
    public void DropItems()
    {
        // 检查掉落管理器是否存在
        if (DropManager.Instance == null)
        {
            // 输出错误信息
            Debug.LogError("掉落管理器未找到，请确保场景中有DropManager对象");
            return; // 退出方法
        }

        // 计算掉落位置，敌人位置 + 偏移量
        Vector3 dropPosition = transform.position + dropOffset;
        // 通过管理器生成掉落物品
        DropManager.Instance.SpawnDropsFromTable(dropTable, dropPosition);

        // 输出掉落日志
        Debug.Log("敌人死亡掉落: " + gameObject.name + " 位置: " + dropPosition);
    }

    /// <summary>
    /// 测试掉落的上下文菜单方法
    /// </summary>
    [ContextMenu("测试掉落")]
    public void TestDrop()
    {
        // 检查是否有掉落表数据
        if (dropTable != null)
        {
            // 执行掉落
            DropItems();
            // 输出测试信息
            Debug.Log("测试掉落完成");
        }
        else // 如果没有设置掉落表
        {
            // 输出警告信息
            Debug.LogWarning("无法测试掉落：未设置掉落表");
        }
    }

    /// <summary>
    /// 显示掉落信息的上下文菜单方法
    /// </summary>
    [ContextMenu("显示掉落信息")]
    public void ShowDropInfo()
    {
        // 检查是否有掉落表数据
        if (dropTable != null)
        {
            // 输出掉落信息标题
            Debug.Log("=== 敌人掉落信息 ===");
            // 输出敌人名称
            Debug.Log("敌人名称: " + gameObject.name);
            // 输出掉落表名称
            Debug.Log("掉落表: " + dropTable.enemyName);
            // 输出可能掉落物品数量
            Debug.Log("可能掉落物品数: " + dropTable.possibleDrops.Count);
        }
        else // 如果没有设置掉落表
        {
            // 输出警告信息
            Debug.LogWarning("未设置掉落表");
        }
    }
}
