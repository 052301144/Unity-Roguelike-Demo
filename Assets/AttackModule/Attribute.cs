using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attribute : MonoBehaviour
{
    [Header("��������")]
    [SerializeField] private int maxHealth = 100; // �������ֵ
    [SerializeField] private int currentHealth;   // ��ǰ����ֵ
    [SerializeField] private int attack = 10;     // ������
    [SerializeField] private int defense = 0;     // ������

    // ���Է�����
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public int Attack => attack;
    public int Defense => defense;
    public bool IsAlive => currentHealth > 0;

    // �¼�
    public System.Action<int> OnHealthChanged;           // ����ֵ�仯�¼�
    public System.Action<int, GameObject> OnTakeDamage;  // �ܵ��˺��¼�
    public System.Action<int> OnAttackChanged;           // �������仯�¼�
    public System.Action<int> OnDefenseChanged;          // �������仯�¼�
    public System.Action OnDeath;                        // �����¼�

    private void Awake()
    {
        // ��ʼ����ǰ����ֵΪ�������ֵ
        currentHealth = maxHealth;
    }

    
    // �ܵ��˺�
    public void TakeDamage(int rawDamage, GameObject attacker = null)
    {
        if (!IsAlive) return;

        // ���������˺������Ƿ�����
        int finalDamage = CalculateFinalDamage(rawDamage);
        int previousHealth = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth - finalDamage, 0, maxHealth);

        // �����ܵ��˺��¼�
        OnTakeDamage?.Invoke(finalDamage, attacker);

        // ��������ֵ�仯�¼�
        OnHealthChanged?.Invoke(currentHealth);

        // ����Ƿ�����
        if (currentHealth <= 0 && previousHealth > 0)
        {
            OnDeath?.Invoke();
        }
    }

    
    // �ܵ����ӷ������˺�
    
    public void TakeTrueDamage(int rawDamage, GameObject attacker = null)
    {
        if (!IsAlive) return;

        int previousHealth = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth - rawDamage, 0, maxHealth);

        // �����ܵ��˺��¼�
        OnTakeDamage?.Invoke(rawDamage, attacker);

        // ��������ֵ�仯�¼�
        OnHealthChanged?.Invoke(currentHealth);

        // ����Ƿ�����
        if (currentHealth <= 0 && previousHealth > 0)
        {
            OnDeath?.Invoke();
        }
    }

    // ���������˺�
    private int CalculateFinalDamage(int rawDamage)
    {
        // �����������˺���ʽ��ÿ���������1%�˺�
        float defenseMultiplier = Mathf.Clamp(1f - (defense * 0.01f), 0.1f, 1f);
        int finalDamage = Mathf.RoundToInt(rawDamage * defenseMultiplier);
        return Mathf.Max(0, finalDamage);
    }

   
    // ����
    public void Heal(int healAmount)
    {
        if (!IsAlive) return;

        int previousHealth = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth + healAmount, 0, maxHealth);

        // ��������ֵ�仯�¼�
        if (currentHealth != previousHealth)
        {
            OnHealthChanged?.Invoke(currentHealth);
        }
    }

   
    public void SetMaxHealth(int newMaxHealth, bool fillHealth = false)
    {
        maxHealth = Mathf.Max(0, newMaxHealth);

        //�Ƿ񽫵�ǰ����ֵ��䵽�µ����ֵ
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

    
    // ���ù�����
    public void SetAttack(int newAttack)
    {
        attack = Mathf.Max(0, newAttack);
        OnAttackChanged?.Invoke(attack);
    }

   
    // ���ӹ�����
    public void AddAttack(int bonusAttack)
    {
        attack += bonusAttack;
        OnAttackChanged?.Invoke(attack);
    }

    // ���÷�����
    public void SetDefense(int newDefense)
    {
        defense = Mathf.Max(0, newDefense);
        OnDefenseChanged?.Invoke(defense);
    }

  
    // ���ӷ�����
    public void AddDefense(int bonusDefense)
    {
        defense += bonusDefense;
        OnDefenseChanged?.Invoke(defense);
    }

    
    // ��������ֵ�����ֵ
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
    }

    // ��ȡ����ֵ�ٷֱ�
    public float GetHealthPercentage()
    {
        return maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
    }

   
    // ��ȡ�˺�����ٷֱ�
    public float GetDamageReductionPercentage()
    {
        return Mathf.Clamp(defense * 0.01f, 0f, 0.9f); // ���90%�˺�����
    }
}