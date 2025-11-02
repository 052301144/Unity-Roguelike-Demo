using UnityEngine; // 引入Unity引擎命名空间
using System.Collections; // 引入协程相关命名空间
using System.Reflection; // 引入反射相关命名空间

/// <summary>
/// 独立敌人死亡检测器 - 通过反射检测敌人死亡，不依赖特定组件
/// </summary>
public class EnemyDeathDetector : MonoBehaviour
{
    [Header("掉落设置")] // 掉落相关设置分组
    public DropTable dropTable;                          // 敌人的掉落表配置
    public bool enableDrops = true;                      // 是否启用掉落功能
    public Vector3 dropOffset = new Vector3(0, 0.5f, 0); // 掉落位置相对于敌人的偏移

    [Header("死亡检测")] // 死亡检测相关设置分组
    public float healthCheckInterval = 0.5f;             // 健康值检查的时间间隔（秒）
    public string healthComponentName = "Attribute";     // 健康组件的类型名称

    // 私有字段 - 反射相关变量
    private MonoBehaviour healthComponent;               // 找到的健康组件引用
    private PropertyInfo currentHealthProperty;          // 当前健康值的属性信息
    private PropertyInfo maxHealthProperty;              // 最大健康值的属性信息
    private float lastHealth;                            // 上一帧的健康值
    private bool isDead = false;                         // 标记敌人是否已死亡

    /// <summary>
    /// Start方法 - 在对象首次启用时调用
    /// </summary>
    void Start()
    {
        // 查找健康组件
        FindHealthComponent();
        // 启动健康检查协程
        StartCoroutine(HealthCheckCoroutine());
    }

    /// <summary>
    /// 查找健康组件并使用反射获取健康属性
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
                // 找到健康组件，保存引用
                healthComponent = component;
                break; // 找到后退出循环
            }
        }

        // 如果找到了健康组件
        if (healthComponent != null)
        {
            // 获取组件的类型信息
            var type = healthComponent.GetType();
            // 使用反射获取CurrentHealth属性
            currentHealthProperty = type.GetProperty("CurrentHealth");
            // 使用反射获取MaxHealth属性
            maxHealthProperty = type.GetProperty("MaxHealth");

            // 如果成功获取到当前健康值属性
            if (currentHealthProperty != null)
            {
                // 获取初始健康值并保存
                lastHealth = (int)currentHealthProperty.GetValue(healthComponent);
            }

            // 输出找到组件的日志
            Debug.Log("找到健康组件: " + healthComponent.GetType().Name);
        }
        else // 如果没有找到健康组件
        {
            // 输出警告信息
            Debug.LogWarning("未找到健康组件: " + healthComponentName);
        }
    }

    /// <summary>
    /// 健康检查协程 - 定期检查敌人是否死亡
    /// </summary>
    System.Collections.IEnumerator HealthCheckCoroutine()
    {
        // 循环执行，直到敌人死亡
        while (!isDead)
        {
            // 等待指定的时间间隔
            yield return new WaitForSeconds(healthCheckInterval);

            // 如果健康组件和当前健康值属性都存在
            if (healthComponent != null && currentHealthProperty != null)
            {
                // 使用反射获取当前健康值
                float currentHealth = (int)currentHealthProperty.GetValue(healthComponent);

                // 检测死亡条件：当前健康值<=0且上一帧健康值>0
                if (currentHealth <= 0 && lastHealth > 0)
                {
                    // 调用死亡处理方法
                    OnEnemyDeath();
                    // 标记为已死亡
                    isDead = true;
                }

                // 更新上一帧健康值
                lastHealth = currentHealth;
            }
        }
    }

    /// <summary>
    /// 敌人死亡事件处理
    /// </summary>
    void OnEnemyDeath()
    {
        // 检查是否启用掉落且有掉落表配置
        if (enableDrops && dropTable != null)
        {
            // 执行掉落物品
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
            Debug.LogError("掉落管理器未找到！请确保场景中有DropManager对象");
            return; // 退出方法
        }

        // 计算掉落位置：敌人位置 + 偏移量
        Vector3 dropPosition = transform.position + dropOffset;
        // 通过掉落管理器生成物品
        DropManager.Instance.SpawnDropsFromTable(dropTable, dropPosition);

        // 输出掉落日志
        Debug.Log("敌人死亡掉落: " + gameObject.name + " 在位置: " + dropPosition);
    }

    /// <summary>
    /// 测试掉落的上下文菜单方法
    /// </summary>
    [ContextMenu("测试掉落")]
    public void TestDrop()
    {
        // 检查是否有掉落表配置
        if (dropTable != null)
        {
            // 执行掉落
            DropItems();
            // 输出测试完成信息
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
        // 检查是否有掉落表配置
        if (dropTable != null)
        {
            // 输出掉落信息标题
            Debug.Log("=== 敌人掉落信息 ===");
            // 输出敌人名称
            Debug.Log("敌人名称: " + gameObject.name);
            // 输出掉落表名称
            Debug.Log("掉落表: " + dropTable.enemyName);
            // 输出可掉落物品数量
            Debug.Log("掉落物品数量: " + dropTable.possibleDrops.Count);
        }
        else // 如果没有设置掉落表
        {
            // 输出警告信息
            Debug.LogWarning("未设置掉落表");
        }
    }
}