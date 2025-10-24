using UnityEngine;

/// <summary>
/// 敌人伤害接口实现示例
/// 将这个脚本添加到敌人GameObject上，使其能够受到技能伤害
/// </summary>
public class EnemyDamageable : MonoBehaviour, SM_IDamageable, SM_IKnockbackable
{
    [Header("敌人属性")]
    public float maxHealth = 50f;
    [SerializeField] private float currentHealth = 50f;
    public float defense = 5f;
    public float knockbackResistance = 1f; // 击退抗性
    
    [Header("组件")]
    public Rigidbody2D rb;
    public Collider2D enemyCollider;
    
    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (enemyCollider == null) enemyCollider = GetComponent<Collider2D>();
        
        currentHealth = maxHealth;
    }
    
    // ========== SM_IDamageable 接口实现 ==========
    public void ApplyDamage(SM_DamageInfo info)
    {
        float finalDamage = info.Amount;
        
        // 计算防御减免
        if (!info.IgnoreDefense)
        {
            finalDamage = Mathf.Max(1f, finalDamage - defense);
        }
        
        // 计算暴击
        if (Random.value < info.CritChance)
        {
            finalDamage *= info.CritMultiplier;
            Debug.Log($"[敌人伤害] 暴击！受到 {finalDamage} 点伤害");
        }
        
        currentHealth = Mathf.Max(0f, currentHealth - finalDamage);
        Debug.Log($"[敌人伤害] 受到 {finalDamage} 点 {info.Element} 伤害，剩余生命值: {currentHealth}/{maxHealth}");
        
        // 播放受伤效果
        OnTakeDamage(finalDamage, info.Element);
        
        if (currentHealth <= 0f)
        {
            OnDeath();
        }
    }
    
    public Transform GetTransform() => transform;
    
    // ========== SM_IKnockbackable 接口实现 ==========
    public void Knockback(Vector2 dir, float force, float duration)
    {
        if (rb == null) return;
        
        // 应用击退抗性
        float actualForce = force / knockbackResistance;
        
        // 添加击退力
        rb.AddForce(dir * actualForce, ForceMode2D.Impulse);
        
        Debug.Log($"[击退] 受到 {actualForce} 点击退力，方向: {dir}");
        
        // 可以在这里添加击退动画或效果
        StartCoroutine(ApplyKnockbackEffect(duration));
    }
    
    private System.Collections.IEnumerator ApplyKnockbackEffect(float duration)
    {
        // 简单的击退效果：在击退期间禁用AI或移动
        var originalGravity = rb.gravityScale;
        rb.gravityScale = 0f; // 暂时禁用重力
        
        yield return new WaitForSeconds(duration);
        
        rb.gravityScale = originalGravity; // 恢复重力
    }
    
    // ========== 敌人行为 ==========
    private void OnTakeDamage(float damage, SM_Element element)
    {
        // 播放受伤动画或效果
        // 例如：改变颜色、播放音效等
        
        // 简单的受伤效果：短暂变红
        StartCoroutine(FlashRed());
    }
    
    private System.Collections.IEnumerator FlashRed()
    {
        var renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            var originalColor = renderer.color;
            renderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            renderer.color = originalColor;
        }
    }
    
    private void OnDeath()
    {
        Debug.Log("[敌人] 敌人死亡！");
        
        // 播放死亡动画
        // 掉落物品
        // 播放死亡音效
        
        // 简单实现：销毁敌人
        Destroy(gameObject);
    }
    
    // ========== 调试信息 ==========
    private void OnDrawGizmosSelected()
    {
        // 绘制生命值条
        Vector3 healthBarPos = transform.position + Vector3.up * 1f;
        float healthPercent = currentHealth / maxHealth;
        
        Gizmos.color = Color.red;
        Gizmos.DrawLine(healthBarPos, healthBarPos + Vector3.right * healthPercent);
        
        Gizmos.color = Color.green;
        Gizmos.DrawLine(healthBarPos + Vector3.right * healthPercent, healthBarPos + Vector3.right);
    }
}
