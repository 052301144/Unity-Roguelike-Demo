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
    }

    
    // 受到伤害
    public void TakeDamage(int rawDamage, GameObject attacker = null)
    {
        if (!IsAlive) return;

        // 计算最终伤害（考虑防御）
        int finalDamage = CalculateFinalDamage(rawDamage);
        int previousHealth = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth - finalDamage, 0, maxHealth);

        // 触发受到伤害事件
        OnTakeDamage?.Invoke(finalDamage, attacker);

        // 触发生命值变化事件
        OnHealthChanged?.Invoke(currentHealth);

        // 检查是否死亡
        if (currentHealth <= 0 && previousHealth > 0)
        {
            OnDeath?.Invoke();
        }
    }

    
    // 受到无视防御的伤害
    
    public void TakeTrueDamage(int rawDamage, GameObject attacker = null)
    {
        if (!IsAlive) return;

        int previousHealth = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth - rawDamage, 0, maxHealth);

        // 触发受到伤害事件
        OnTakeDamage?.Invoke(rawDamage, attacker);

        // 触发生命值变化事件
        OnHealthChanged?.Invoke(currentHealth);

        // 检查是否死亡
        if (currentHealth <= 0 && previousHealth > 0)
        {
            OnDeath?.Invoke();
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

        // 触发生命值变化事件
        if (currentHealth != previousHealth)
        {
            OnHealthChanged?.Invoke(currentHealth);
        }
    }

   
    public void SetMaxHealth(int newMaxHealth, bool fillHealth = false)
    {
        maxHealth = Mathf.Max(0, newMaxHealth);

        //是否将当前生命值填充到新的最大值
        if (fillHealth)
        {
            currentHealth = maxHealth;
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
        attack = Mathf.Max(0, newAttack);
        OnAttackChanged?.Invoke(attack);
    }

   
    // 增加攻击力
    public void AddAttack(int bonusAttack)
    {
        attack += bonusAttack;
        OnAttackChanged?.Invoke(attack);
    }

    // 设置防御力
    public void SetDefense(int newDefense)
    {
        defense = Mathf.Max(0, newDefense);
        OnDefenseChanged?.Invoke(defense);
    }

  
    // 增加防御力
    public void AddDefense(int bonusDefense)
    {
        defense += bonusDefense;
        OnDefenseChanged?.Invoke(defense);
    }

    
    // 重置生命值到最大值
    public void ResetHealth()
    {
        currentHealth = maxHealth;
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
}