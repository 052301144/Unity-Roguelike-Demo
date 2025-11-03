using UnityEngine; // 引入Unity引擎命名空间

/// <summary>
/// 掉落系统测试脚本 - 提供快捷键测试掉落系统功能
/// </summary>
public class DropSystemTester : MonoBehaviour
{
    /// <summary>
    /// Update方法 - 每帧调用，检测按键输入
    /// </summary>
    void Update()
    {
        // 按T键测试敌人掉落
        if (Input.GetKeyDown(KeyCode.T))
        {
            TestEnemyDrop(); // 调用测试掉落方法
        }

        // 按C键清除所有掉落
        if (Input.GetKeyDown(KeyCode.C))
        {
            // 检查掉落管理器是否存在
            if (DropManager.Instance != null)
            {
                // 清除所有的掉落物品
                DropManager.Instance.ClearAllDrops();
                // 输出清除信息
                Debug.Log("已清除所有的掉落物品");
            }
        }

        // 按I键显示掉落信息
        if (Input.GetKeyDown(KeyCode.I))
        {
            ShowDropInfo(); // 调用显示信息方法
        }

        // 按G键显示金币信息
        if (Input.GetKeyDown(KeyCode.G))
        {
            ShowGoldInfo(); // 调用显示金币信息方法
        }
    }

    /// <summary>
    /// 测试敌人掉落功能
    /// </summary>
    void TestEnemyDrop()
    {
        // 查找场景中存在"Enemy"标签的敌人对象
        GameObject enemy = GameObject.FindGameObjectWithTag("Enemy");
        // 如果没有找到敌人
        if (enemy == null)
        {
            // 使用当前游戏对象作为测试目标
            enemy = gameObject;
            // 提示使用当前对象进行测试
            Debug.Log("未找到带Enemy标签的对象，使用当前对象进行测试");
        }

        // 获取敌人的掉落控制器组件
        EnemyDeathDetector dropController = enemy.GetComponent<EnemyDeathDetector>();
        // 如果找到了掉落控制器
        if (dropController != null)
        {
            // 执行测试掉落
            dropController.TestDrop();
        }
        else // 如果没有找到掉落控制器
        {
            // 输出警告信息
            Debug.LogWarning("未找到EnemyDeathDetector组件，请为对象添加该组件");
        }
    }

    /// <summary>
    /// 显示掉落信息
    /// </summary>
    void ShowDropInfo()
    {
        // 查找场景中的敌人对象
        GameObject enemy = GameObject.FindGameObjectWithTag("Enemy");
        // 如果没有找到敌人，使用当前对象
        if (enemy == null)
        {
            enemy = gameObject;
        }

        // 获取掉落控制器组件
        EnemyDeathDetector dropController = enemy.GetComponent<EnemyDeathDetector>();
        // 如果找到了掉落控制器
        if (dropController != null)
        {
            // 显示掉落信息
            dropController.ShowDropInfo();
        }
        else // 如果没有找到掉落控制器
        {
            // 输出警告信息
            Debug.LogWarning("未找到EnemyDeathDetector组件");
        }
    }

    /// <summary>
    /// 显示金币信息
    /// </summary>
    void ShowGoldInfo()
    {
        // 检查掉落管理器是否存在
        if (DropManager.Instance != null)
        {
            // 获取并显示总金币数 - 实际应该调用GetTotalCoins()方法
            Debug.Log("当前总金币: " + DropManager.Instance.GetTotalCoins());
        }
        else // 如果掉落管理器不存在
        {
            // 输出警告信息
            Debug.LogWarning("掉落管理器未找到");
        }
    }

}
