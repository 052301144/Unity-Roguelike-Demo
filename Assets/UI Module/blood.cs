using UnityEngine;
using TMPro;

public class HealthBarController : MonoBehaviour
{
    [Header("Ѫ�����")]
    public UnityEngine.UI.Slider healthSlider;  // ���ʹ�� Slider ����
    public UnityEngine.UI.Image healthFillImage; // ���ʹ�� Image ����
    public TextMeshProUGUI healthText;

    [Header("Ѫ������")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    void Start()
    {
        // ��ʼ��Ѫ��
        UpdateHealthBar();
    }

    // ����Ѫ����ʾ
    public void UpdateHealthBar()
    {
        float healthPercentage = currentHealth / maxHealth;

        // ���� Slider �� Image
        if (healthSlider != null)
            healthSlider.value = healthPercentage;

        if (healthFillImage != null)
            healthFillImage.fillAmount = healthPercentage;

        // ��������
        if (healthText != null)
            healthText.text = $"{currentHealth}/{maxHealth}";

        // ��ѡ������Ѫ���ı���ɫ
        UpdateHealthColor(healthPercentage);
    }

    // ����Ѫ���ı���ɫ
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

    // �ܵ��˺�
    public void TakeDamage(float damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        UpdateHealthBar();
    }

    // �ָ�Ѫ��
    public void Heal(float healAmount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
        UpdateHealthBar();
    }

    // ����Ѫ��
    public void SetHealth(float health)
    {
        currentHealth = Mathf.Clamp(health, 0, maxHealth);
        UpdateHealthBar();
    }
}
