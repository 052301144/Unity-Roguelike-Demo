using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attribute : MonoBehaviour
{
    [Header("基础属性")]
    [SerializeField] private int maxHealth = 100; // 最大生命值
    [SerializeField] private int currentHealth;   // 当前生命值
    [SerializeField] private int attack = 10;     // 攻击力
    [SerializeField] private int defense = 0;     // 防御力

    [Header("显示设置")]
    [SerializeField] private bool showHealthBar = true; // 是否显示血条
    [SerializeField] private bool logDamageEvents = true; // 是否在控制台输出伤害事件

    // 属性访问器
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public int Attack => attack;
    public int Defense => defense;
    public bool IsAlive => currentHealth > 0;

    // 事件
    public System.Action<int> OnHealthChanged;           // 生命值变化事件
    public System.Action<int, GameObject> OnTakeDamage;  // 受到伤害事件
    public System.Action<int> OnAttackChanged;           // 攻击力变化事件
    public System.Action<int> OnDefenseChanged;          // 防御力变化事件
    public System.Action OnDeath;                        // 死亡事件

    private void Awake()
    {
        // 初始化当前生命值为最大生命值
        currentHealth = maxHealth;

        // 初始血条显示
        if (showHealthBar)
        {
            Debug.Log($"{gameObject.name} 初始血量: {currentHealth}/{maxHealth}");
        }
    }

    // 受到伤害
    public void TakeDamage(int rawDamage, GameObject attacker = null)
    {
        if (!IsAlive) return;

        // 计算最终伤害（考虑防御）
        int finalDamage = CalculateFinalDamage(rawDamage);
        int previousHealth = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth - finalDamage, 0, maxHealth);

        // 控制台输出伤害信息
        if (logDamageEvents)
        {
            string attackerName = attacker != null ? attacker.name : "未知来源";
            Debug.Log($"{gameObject.name} 受到 {finalDamage} 点伤害 (原始伤害: {rawDamage}, 防御减伤: {defense}) | 攻击者: {attackerName}");
            Debug.Log($"{gameObject.name} 血量变化: {previousHealth} -> {currentHealth}");
        }

        // 显示血条变化
        if (showHealthBar)
        {
            UpdateHealthBar();
        }

        // 触发受到伤害事件
        OnTakeDamage?.Invoke(finalDamage, attacker);

        // 触发生命值变化事件
        OnHealthChanged?.Invoke(currentHealth);

        // 检查是否死亡
        if (currentHealth <= 0 && previousHealth > 0)
        {
            OnDeath?.Invoke();
            if (logDamageEvents)
            {
                Debug.Log($"{gameObject.name} 死亡!");
            }
        }
    }

    // 受到无视防御的伤害
    public void TakeTrueDamage(int rawDamage, GameObject attacker = null)
    {
        if (!IsAlive) return;

        int previousHealth = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth - rawDamage, 0, maxHealth);

        // 控制台输出真实伤害信息
        if (logDamageEvents)
        {
            string attackerName = attacker != null ? attacker.name : "未知来源";
            Debug.Log($"{gameObject.name} 受到真实伤害: {rawDamage} 点 | 攻击者: {attackerName}");
            Debug.Log($"{gameObject.name} 血量变化: {previousHealth} -> {currentHealth}");
        }

        // 显示血条变化
        if (showHealthBar)
        {
            UpdateHealthBar();
        }

        // 触发受到伤害事件
        OnTakeDamage?.Invoke(rawDamage, attacker);

        // 触发生命值变化事件
        OnHealthChanged?.Invoke(currentHealth);

        // 检查是否死亡
        if (currentHealth <= 0 && previousHealth > 0)
        {
            OnDeath?.Invoke();
            if (logDamageEvents)
            {
                Debug.Log($"{gameObject.name} 死亡!");
            }
        }
    }

    // 计算最终伤害
    private int CalculateFinalDamage(int rawDamage)
    {
        // 防御力减少伤害公式：每点防御减少1%伤害
        float defenseMultiplier = Mathf.Clamp(1f - (defense * 0.01f), 0.1f, 1f);
        int finalDamage = Mathf.RoundToInt(rawDamage * defenseMultiplier);
        return Mathf.Max(0, finalDamage);
    }

    // 治疗
    public void Heal(int healAmount)
    {
        if (!IsAlive) return;

        int previousHealth = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth + healAmount, 0, maxHealth);

        // 控制台输出治疗信息
        if (logDamageEvents)
        {
            Debug.Log($"{gameObject.name} 恢复 {healAmount} 点生命值");
            Debug.Log($"{gameObject.name} 血量变化: {previousHealth} -> {currentHealth}");
        }

        // 显示血条变化
        if (showHealthBar)
        {
            UpdateHealthBar();
        }

        // 触发生命值变化事件
        if (currentHealth != previousHealth)
        {
            OnHealthChanged?.Invoke(currentHealth);
        }
    }

    // 更新血条显示
    private void UpdateHealthBar()
    {
        float healthPercentage = GetHealthPercentage();
        string healthBar = CreateHealthBar(healthPercentage);

        Debug.Log($"{gameObject.name} 生命值: {currentHealth}/{maxHealth}");
        Debug.Log(healthBar);
        Debug.Log($"血量百分比: {healthPercentage:P0}");
    }

    // 创建血条可视化
    private string CreateHealthBar(float percentage)
    {
        int barLength = 20; // 血条长度
        int filledLength = Mathf.RoundToInt(barLength * percentage);
        int emptyLength = barLength - filledLength;

        string bar = "[";
        bar += new string('█', filledLength);
        bar += new string('░', emptyLength);
        bar += "]";

        return bar;
    }

    public void SetMaxHealth(int newMaxHealth, bool fillHealth = false)
    {
        int oldMaxHealth = maxHealth;
        maxHealth = Mathf.Max(0, newMaxHealth);

        // 控制台输出最大生命值变化
        if (logDamageEvents && oldMaxHealth != maxHealth)
        {
            Debug.Log($"{gameObject.name} 最大生命值变化: {oldMaxHealth} -> {maxHealth}");
        }

        //是否将当前生命值填充到新的最大值
        if (fillHealth)
        {
            currentHealth = maxHealth;
            if (showHealthBar)
            {
                UpdateHealthBar();
            }
            OnHealthChanged?.Invoke(currentHealth);
        }
        else
        {
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        }
    }

    // 设置攻击力
    public void SetAttack(int newAttack)
    {
        int oldAttack = attack;
        attack = Mathf.Max(0, newAttack);

        if (logDamageEvents && oldAttack != attack)
        {
            Debug.Log($"{gameObject.name} 攻击力变化: {oldAttack} -> {attack}");
        }

        OnAttackChanged?.Invoke(attack);
    }

    // 增加攻击力
    public void AddAttack(int bonusAttack)
    {
        int oldAttack = attack;
        attack += bonusAttack;

        if (logDamageEvents)
        {
            Debug.Log($"{gameObject.name} 攻击力增加: {oldAttack} -> {attack} (+{bonusAttack})");
        }

        OnAttackChanged?.Invoke(attack);
    }

    // 设置防御力
    public void SetDefense(int newDefense)
    {
        int oldDefense = defense;
        defense = Mathf.Max(0, newDefense);

        if (logDamageEvents && oldDefense != defense)
        {
            Debug.Log($"{gameObject.name} 防御力变化: {oldDefense} -> {defense}");
            Debug.Log($"伤害减免: {GetDamageReductionPercentage():P0}");
        }

        OnDefenseChanged?.Invoke(defense);
    }

    // 增加防御力
    public void AddDefense(int bonusDefense)
    {
        int oldDefense = defense;
        defense += bonusDefense;

        if (logDamageEvents)
        {
            Debug.Log($"{gameObject.name} 防御力增加: {oldDefense} -> {defense} (+{bonusDefense})");
            Debug.Log($"伤害减免: {GetDamageReductionPercentage():P0}");
        }

        OnDefenseChanged?.Invoke(defense);
    }

    // 重置生命值到最大值
    public void ResetHealth()
    {
        int oldHealth = currentHealth;
        currentHealth = maxHealth;

        if (logDamageEvents)
        {
            Debug.Log($"{gameObject.name} 生命值重置: {oldHealth} -> {currentHealth}");
        }

        if (showHealthBar)
        {
            UpdateHealthBar();
        }

        OnHealthChanged?.Invoke(currentHealth);
    }

    // 获取生命值百分比
    public float GetHealthPercentage()
    {
        return maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
    }

    // 获取伤害减免百分比
    public float GetDamageReductionPercentage()
    {
        return Mathf.Clamp(defense * 0.01f, 0f, 0.9f); // 最高90%伤害减免
    }

    // 上下文菜单：测试伤害
    [ContextMenu("测试受到10点伤害")]
    private void TestTakeDamage()
    {
        TakeDamage(10, gameObject);
    }

    [ContextMenu("测试治疗20点生命")]
    private void TestHeal()
    {
        Heal(20);
    }

    [ContextMenu("显示当前状态")]
    private void ShowCurrentStatus()
    {
        Debug.Log("=== 当前状态 ===");
        Debug.Log($"{gameObject.name} 状态:");
        Debug.Log($"生命值: {currentHealth}/{maxHealth}");
        Debug.Log($"攻击力: {attack}");
        Debug.Log($"防御力: {defense}");
        Debug.Log($"存活状态: {(IsAlive ? "存活" : "死亡")}");
        Debug.Log($"伤害减免: {GetDamageReductionPercentage():P0}");
        UpdateHealthBar();
    }
}