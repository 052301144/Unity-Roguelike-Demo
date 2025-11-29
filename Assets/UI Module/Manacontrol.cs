using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MP条控制器 - 连接Attribute系统和UI Slider
/// </summary>
public class ManaBarController : MonoBehaviour
{
    [Header("角色属性")]
    [SerializeField] private Attribute targetAttribute; // 角色的Attribute组件

    [Header("UI组件")]
    [SerializeField] private Slider manaSlider;       // MP条的Slider组件
    [SerializeField] private bool showText = true;    // 是否显示文字
    [SerializeField] private TMPro.TextMeshProUGUI manaText; // 法力值文字（可选）

    [Header("显示设置")]
    [SerializeField] private bool smoothChange = true; // 是否平滑变化
    [SerializeField] private float smoothSpeed = 5f;   // 平滑变化速度

    private float targetManaValue; // 目标法力值

    private void Awake()
    {
        // 自动查找未指定的组件
        if (targetAttribute == null)
            targetAttribute = FindObjectOfType<Attribute>();

        if (manaSlider == null)
            manaSlider = GetComponent<Slider>();

        if (manaText == null && showText)
            manaText = GetComponentInChildren<TMPro.TextMeshProUGUI>();
    }

    private void Start()
    {
        // 组件验证
        if (targetAttribute == null)
        {
            Debug.LogError("ManaBarController: 未找到Attribute组件！");
            return;
        }

        if (manaSlider == null)
        {
            Debug.LogError("ManaBarController: 未找到Slider组件！");
            return;
        }

        // 注册法力值变化事件（假设Attribute中有对应的事件）
        targetAttribute.OnManaChanged += OnManaChanged;
        targetAttribute.OnDeath += OnDeath;

        // 初始化蓝条
        InitializeManaBar();
    }

    private void OnDestroy()
    {
        // 取消事件注册
        if (targetAttribute != null)
        {
            targetAttribute.OnManaChanged -= OnManaChanged;
            targetAttribute.OnDeath -= OnDeath;
        }
    }

    private void Update()
    {
        // 平滑更新蓝条
        if (smoothChange && manaSlider != null)
        {
            manaSlider.value = Mathf.Lerp(manaSlider.value, targetManaValue, smoothSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// 初始化蓝条
    /// </summary>
    private void InitializeManaBar()
    {
        if (targetAttribute == null || manaSlider == null) return;

        // 设置Slider范围
        manaSlider.minValue = 0;
        manaSlider.maxValue = targetAttribute.MaxMana;

        // 设置当前值
        targetManaValue = targetAttribute.CurrentMana;
        if (!smoothChange)
        {
            manaSlider.value = targetManaValue;
        }

        // 更新文字显示
        UpdateManaText();
    }

    /// <summary>
    /// 法力值变化事件处理
    /// </summary>
    private void OnManaChanged(int currentMana)
    {
        if (manaSlider == null) return;

        // 更新目标法力值
        targetManaValue = currentMana;

        // 非平滑模式直接设置值
        if (!smoothChange)
        {
            manaSlider.value = targetManaValue;
        }

        // 更新文字显示
        UpdateManaText();

        // 可选：根据法力值百分比更新颜色
        UpdateManaBarColor();
    }

    /// <summary>
    /// 死亡事件处理
    /// </summary>
    private void OnDeath()
    {
        // 死亡时清空法力值显示
        Debug.Log($"{targetAttribute.gameObject.name} 已死亡，蓝条更新为0");

        if (manaSlider != null)
        {
            manaSlider.value = 0;
            targetManaValue = 0;
        }

        UpdateManaText();
    }

    /// <summary>
    /// 更新法力值文字显示
    /// </summary>
    private void UpdateManaText()
    {
        if (manaText == null || !showText) return;

        manaText.text = $"{targetAttribute.CurrentMana}/{targetAttribute.MaxMana}";
    }

    /// <summary>
    /// 根据法力值百分比更新蓝条颜色
    /// </summary>
    private void UpdateManaBarColor()
    {
        if (manaSlider == null) return;

        float manaPercentage = targetAttribute.GetManaPercentage(); // 假设Attribute中有此方法
        var fillImage = manaSlider.fillRect?.GetComponent<Image>();
        if (fillImage != null)
        {
            // 示例：低法力值时变深（可根据需求调整）
            fillImage.color = Color.Lerp(Color.cyan, Color.blue, 1 - manaPercentage);
        }
    }

    /// <summary>
    /// 手动设置目标角色
    /// </summary>
    public void SetTarget(Attribute newTarget)
    {
        // 取消旧目标的事件
        if (targetAttribute != null)
        {
            targetAttribute.OnManaChanged -= OnManaChanged;
            targetAttribute.OnDeath -= OnDeath;
        }

        // 设置新目标
        targetAttribute = newTarget;

        // 注册新目标的事件
        if (targetAttribute != null)
        {
            targetAttribute.OnManaChanged += OnManaChanged;
            targetAttribute.OnDeath += OnDeath;

            // 重新初始化蓝条
            InitializeManaBar();
        }
    }

    /// <summary>
    /// 上下文菜单：测试蓝条更新
    /// </summary>
    [ContextMenu("测试蓝条更新")]
    private void TestManaBar()
    {
        if (targetAttribute != null)
        {
            OnManaChanged(targetAttribute.CurrentMana);
        }
    }
}