using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class DeathManager : MonoBehaviour
{
    [Header("死亡设置")]
    [SerializeField] private string deathSceneName = "DeathScene"; // 死亡场景名称
    [SerializeField] private float deathDelay = 1f; // 死亡后延迟时间

    [Header("血量检测")]
    [SerializeField] private Attribute playerAttribute; // 角色属性组件
    [SerializeField] private float checkInterval = 0.1f; // 检测间隔

    private bool isChecking = true;
    private bool isDead = false;

    private void Awake()
    {
        // 如果没有手动指定，自动查找Attribute组件
        if (playerAttribute == null)
            playerAttribute = FindObjectOfType<Attribute>();

        if (playerAttribute == null)
        {
            Debug.LogError("HealthDeathManager: 未找到Attribute组件！");
            return;
        }
    }

    private void Start()
    {
        // 开始血量检测协程
        StartCoroutine(HealthCheckCoroutine());
    }

    /// <summary>
    /// 血量检测协程
    /// </summary>
    private IEnumerator HealthCheckCoroutine()
    {
        while (isChecking)
        {
            // 检测血量是否为零
            if (playerAttribute.CurrentHealth <= 0 && !isDead)
            {
                OnDeath();
            }

            yield return new WaitForSeconds(checkInterval);
        }
    }

    /// <summary>
    /// 死亡处理
    /// </summary>
    private void OnDeath()
    {
        isDead = true;
        isChecking = false;

        Debug.Log("检测到血量为零，准备加载死亡场景");

        // 延迟加载死亡场景
        StartCoroutine(LoadDeathSceneWithDelay());
    }

    /// <summary>
    /// 延迟加载死亡场景
    /// </summary>
    private IEnumerator LoadDeathSceneWithDelay()
    {
        yield return new WaitForSeconds(deathDelay);

        // 加载死亡场景
        if (!string.IsNullOrEmpty(deathSceneName))
        {
            SceneManager.LoadScene(deathSceneName);
        }
        else
        {
            Debug.LogError("死亡场景名称未设置！");
        }
    }

    /// <summary>
    /// 手动触发死亡检测（测试用）
    /// </summary>
    [ContextMenu("测试死亡检测")]
    public void TestDeathDetection()
    {
        if (playerAttribute != null && !isDead)
        {
            // 直接将血量设为0触发检测
            playerAttribute.TakeTrueDamage(playerAttribute.CurrentHealth, gameObject);
        }
    }

    /// <summary>
    /// 设置要检测的Attribute组件
    /// </summary>
    public void SetTargetAttribute(Attribute attribute)
    {
        if (isChecking)
        {
            Debug.LogWarning("正在检测中，请先停止检测再更换目标");
            return;
        }

        playerAttribute = attribute;
        isDead = false;
        isChecking = true;
        StartCoroutine(HealthCheckCoroutine());
    }

    private void OnDestroy()
    {
        isChecking = false;
    }
}