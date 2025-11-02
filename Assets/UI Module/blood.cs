using UnityEngine;
using TMPro;

public class HealthBarController : MonoBehaviour
{
    [Header("血条组件")]
    public UnityEngine.UI.Slider healthSlider;  // 推荐使用 Slider 组件
    public UnityEngine.UI.Image healthFillImage; // 推荐使用 Image 组件
    public TextMeshProUGUI healthText;

    [Header("血条属性")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    void Start()
    {
        // 初始化血条
        UpdateHealthBar();
    }

    // 更新血条显示
    public void UpdateHealthBar()
    {
        float healthPercentage = currentHealth / maxHealth;

        // 更新 Slider 和 Image
        if (healthSlider != null)
            healthSlider.value = healthPercentage;

        if (healthFillImage != null)
            healthFillImage.fillAmount = healthPercentage;

        // 更新文本
        if (healthText != null)
            healthText.text = $"{currentHealth}/{maxHealth}";

        // 可选：更新血条的颜色
        UpdateHealthColor(healthPercentage);
    }

    // 更新血条的颜色
    void UpdateHealthColor(float percentage)
    {
        if (healthFillImage != null)
        {
            if (percentage > 0.6f)
                healthFillImage.color = Color.green;
            else if (percentage > 0.3f)
                healthFillImage.color = Color.yellow;
            else
                healthFillImage.color = Color.red;
        }
    }

    // 受到伤害
    public void TakeDamage(float damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        UpdateHealthBar();
    }

    // 恢复血量
    public void Heal(float healAmount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
        UpdateHealthBar();
    }

    // 设置血量
    public void SetHealth(float health)
    {
        currentHealth = Mathf.Clamp(health, 0, maxHealth);
        UpdateHealthBar();
    }
}
