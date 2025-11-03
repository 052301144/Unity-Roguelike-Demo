using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HP条控制器 - 连接Attribute系统和UI Slider
/// </summary>
public class HealthBarController : MonoBehaviour
{
    [Header("角色属性")]
    [SerializeField] private Attribute targetAttribute; // 角色的Attribute组件

    [Header("UI组件")]
    [SerializeField] private Slider healthSlider;      // HP条的Slider组件
    [SerializeField] private bool showText = true;     // 是否显示文字
    [SerializeField] private TMPro.TextMeshProUGUI healthText; // 血量文字（可选）

    [Header("显示设置")]
    [SerializeField] private bool smoothChange = true; // 是否平滑变化
    [SerializeField] private float smoothSpeed = 5f;   // 平滑变化速度

    private float targetHealthValue; // 目标血量值

    private void Awake()
    {
        // 如果没有手动指定，尝试自动查找组件
        if (targetAttribute == null)
            targetAttribute = FindObjectOfType<Attribute>();

        if (healthSlider == null)
            healthSlider = GetComponent<Slider>();

        if (healthText == null && showText)
            healthText = GetComponentInChildren<TMPro.TextMeshProUGUI>();
    }

    private void Start()
    {
        // 验证组件
        if (targetAttribute == null)
        {
            Debug.LogError("HealthBarController: 未找到Attribute组件！");
            return;
        }

        if (healthSlider == null)
        {
            Debug.LogError("HealthBarController: 未找到Slider组件！");
            return;
        }

        // 注册血量变化事件
        targetAttribute.OnHealthChanged += OnHealthChanged;
        targetAttribute.OnDeath += OnDeath;

        // 初始化血条
        InitializeHealthBar();
    }

    private void OnDestroy()
    {
        // 取消事件注册
        if (targetAttribute != null)
        {
            targetAttribute.OnHealthChanged -= OnHealthChanged;
            targetAttribute.OnDeath -= OnDeath;
        }
    }

    private void Update()
    {
        // 平滑更新血条
        if (smoothChange && healthSlider != null)
        {
            healthSlider.value = Mathf.Lerp(healthSlider.value, targetHealthValue, smoothSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// 初始化血条
    /// </summary>
    private void InitializeHealthBar()
    {
        if (targetAttribute == null || healthSlider == null) return;

        // 设置Slider范围
        healthSlider.minValue = 0;
        healthSlider.maxValue = targetAttribute.MaxHealth;

        // 设置当前值
        targetHealthValue = targetAttribute.CurrentHealth;
        if (!smoothChange)
        {
            healthSlider.value = targetHealthValue;
        }

        // 更新文字显示
        UpdateHealthText();
    }

    /// <summary>
    /// 血量变化事件处理
    /// </summary>
    private void OnHealthChanged(int currentHealth)
    {
        if (healthSlider == null) return;

        // 更新目标血量值
        targetHealthValue = currentHealth;

        // 如果不使用平滑，直接设置值
        if (!smoothChange)
        {
            healthSlider.value = targetHealthValue;
        }

        // 更新文字显示
        UpdateHealthText();

        // 可选：血量低时改变颜色
        UpdateHealthBarColor();
    }

    /// <summary>
    /// 死亡事件处理
    /// </summary>
    private void OnDeath()
    {
        // 死亡时的特殊处理
        Debug.Log($"{targetAttribute.gameObject.name} 已死亡，血条更新为0");

        if (healthSlider != null)
        {
            healthSlider.value = 0;
            targetHealthValue = 0;
        }

        UpdateHealthText();
    }

    /// <summary>
    /// 更新血量文字显示
    /// </summary>
    private void UpdateHealthText()
    {
        if (healthText == null || !showText) return;

        healthText.text = $"{targetAttribute.CurrentHealth}/{targetAttribute.MaxHealth}";
    }

    /// <summary>
    /// 根据血量百分比更新血条颜色
    /// </summary>
    private void UpdateHealthBarColor()
    {
        if (healthSlider == null) return;

        float healthPercentage = targetAttribute.GetHealthPercentage();
        var fillImage = healthSlider.fillRect?.GetComponent<Image>();
    }

    /// <summary>
    /// 手动设置目标角色
    /// </summary>
    public void SetTarget(Attribute newTarget)
    {
        // 取消旧目标的事件
        if (targetAttribute != null)
        {
            targetAttribute.OnHealthChanged -= OnHealthChanged;
            targetAttribute.OnDeath -= OnDeath;
        }

        // 设置新目标
        targetAttribute = newTarget;

        // 注册新目标的事件
        if (targetAttribute != null)
        {
            targetAttribute.OnHealthChanged += OnHealthChanged;
            targetAttribute.OnDeath += OnDeath;

            // 重新初始化血条
            InitializeHealthBar();
        }
    }

    /// <summary>
    /// 上下文菜单：测试血条
    /// </summary>
    [ContextMenu("测试血条更新")]
    private void TestHealthBar()
    {
        if (targetAttribute != null)
        {
            OnHealthChanged(targetAttribute.CurrentHealth);
        }
    }
}
