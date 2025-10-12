using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attack : MonoBehaviour
{
    [Header("攻击属性")]
    [SerializeField] private AttackType attackType = AttackType.Physical; // 攻击类型，默认物理攻击
    [SerializeField] private KeyCode attackKey = KeyCode.Mouse0; // 攻击按键，默认为鼠标左键
    [SerializeField] private float attackRange = 2f; // 攻击范围
    [SerializeField] private float attackCooldown = 1f; // 攻击冷却时间
    [SerializeField] private LayerMask enemyLayer; // 敌人层级

    [Header("攻击判定")]
    [SerializeField] private Transform attackPoint; // 攻击判定点
    [SerializeField] private float attackRadius = 0.5f; // 攻击判定半径

    [Header("物理攻击属性")]
    [SerializeField] private float physicalCritRate = 0.1f; // 物理暴击率
    [SerializeField] private float physicalCritDamage = 1.5f; // 物理暴击伤害倍数

    [Header("元素攻击属性")]
    [SerializeField] private float elementalEffectChance = 0.7f; // 元素效果触发概率
    [SerializeField] private float fireDamageDuration = 3f; // 火元素持续伤害时间
    [SerializeField] private float iceFreezeDuration = 2f; // 冰元素冻结时间
    [SerializeField] private float windKnockbackForce = 5f; // 风元素击退力度
    [SerializeField] private float thunderChainRange = 3f; // 雷元素连锁范围

    // 攻击类型枚举
    public enum AttackType
    {
        Physical, // 物理攻击：无视防御，低暴击
        Fire,     // 火元素：持续伤害
        Wind,     // 风元素：击退效果
        Ice,      // 冰元素：冻结控制
        Thunder   // 雷元素：范围连锁
    }

    private float lastAttackTime; // 上次攻击时间
    private Attribute attackerAttribute; // 攻击者属性

    // 属性访问器
    public float AttackRange => attackRange;
    public bool CanAttack => Time.time >= lastAttackTime + attackCooldown;
    public int CurrentAttack => attackerAttribute != null ? attackerAttribute.Attack : 0;
    public AttackType Type => attackType;

    // 事件
    public System.Action<GameObject, int, AttackType> OnAttackHit; // 攻击命中事件（目标，造成的伤害，攻击类型）
    public System.Action<AttackType> OnAttackPerformed; // 攻击执行事件
    public System.Action<AttackType, GameObject> OnElementEffectApplied; // 元素效果应用事件

    // 攻击信息结构体
    public struct AttackInfo
    {
        public GameObject target;
        public int baseDamage;
        public AttackType type;
        public bool isCrit;
        public Vector3 hitPoint;
    }

    //获取Attribute组件
    private void Awake()
    {
        attackerAttribute = GetComponent<Attribute>();
    }

    private void Update()
    {
        // 检测鼠标左键输入
        if (Input.GetKeyDown(attackKey))
        {
            PerformAttack();
        }
    }

    // 执行攻击
    public void PerformAttack()
    {
        if (!CanAttack) return;

        // 触发攻击执行事件
        OnAttackPerformed?.Invoke(attackType);

        // 进行攻击判定
        Collider[] hitEnemies = GetHitEnemies();

        foreach (Collider enemy in hitEnemies)
        {
            if (enemy.gameObject != gameObject) // 不攻击自己
            {
                ProcessAttackHit(enemy.gameObject);
            }
        }

        lastAttackTime = Time.time;
    }

    // 攻击特定目标
    public void AttackTarget(GameObject target)
    {
        if (target == null || !CanAttack) return;

        // 触发攻击执行事件
        OnAttackPerformed?.Invoke(attackType);

        ProcessAttackHit(target);
        lastAttackTime = Time.time;
    }

    // 获取命中的敌人
    private Collider[] GetHitEnemies()
    {
        if (attackPoint != null)
        {
            // 使用攻击点进行球形检测
            return Physics.OverlapSphere(attackPoint.position, attackRadius, enemyLayer);
        }
        else
        {
            // 使用角色位置进行球形检测
            return Physics.OverlapSphere(transform.position, attackRange, enemyLayer);
        }
    }

    // 处理攻击命中
    private void ProcessAttackHit(GameObject target)
    {
        if (target == null) return;

        // 获取攻击者的攻击力
        int baseDamage = attackerAttribute != null ? attackerAttribute.Attack : 0;
        if (baseDamage <= 0) return;

        // 计算最终伤害
        AttackInfo attackInfo = new AttackInfo
        {
            target = target,
            baseDamage = baseDamage,
            type = attackType,
            hitPoint = target.transform.position
        };

        int finalDamage = CalculateDamage(attackInfo);

        // 触发攻击命中事件
        OnAttackHit?.Invoke(target, finalDamage, attackType);

        // 应用伤害
        ApplyDamage(target, finalDamage, attackInfo);

        // 应用元素效果
        if (attackType != AttackType.Physical)
        {
            ApplyElementEffect(target, attackInfo);
        }
    }

    // 计算伤害
    private int CalculateDamage(AttackInfo attackInfo)
    {
        int baseDamage = attackInfo.baseDamage;

        switch (attackType)
        {
            case AttackType.Physical:
                // 物理攻击：计算暴击，无视防御
                bool isCrit = Random.value < physicalCritRate;
                attackInfo.isCrit = isCrit;
                int physicalDamage = Mathf.RoundToInt(baseDamage * (isCrit ? physicalCritDamage : 1f));
                Debug.Log($"物理攻击: 基础{baseDamage}, 暴击{isCrit}, 最终{physicalDamage}");
                return physicalDamage;

            default:
                // 元素攻击：不暴击，计算防御减伤
                Attribute targetAttribute = attackInfo.target.GetComponent<Attribute>();
                if (targetAttribute != null)
                {
                    // 使用目标的防御力计算减伤
                    float defenseMultiplier = Mathf.Clamp(1f - (targetAttribute.Defense * 0.01f), 0.1f, 1f);
                    int elementalDamage = Mathf.RoundToInt(baseDamage * defenseMultiplier);
                    Debug.Log($"元素攻击: 基础{baseDamage}, 防御减伤{defenseMultiplier:P0}, 最终{elementalDamage}");
                    return elementalDamage;
                }
                return baseDamage;
        }
    }

    // 应用伤害
    private void ApplyDamage(GameObject target, int damage, AttackInfo attackInfo)
    {
        Attribute targetAttribute = target.GetComponent<Attribute>();
        if (targetAttribute != null)
        {
            if (attackType == AttackType.Physical)
            {
                // 物理攻击：无视防御，直接造成计算后的伤害
                targetAttribute.TakeTrueDamage(damage, gameObject);
                Debug.Log($"物理攻击无视防御，造成 {damage} 点真实伤害");
            }
            else
            {
                // 元素攻击：正常计算防御减伤
                targetAttribute.TakeDamage(damage, gameObject);
                Debug.Log($"元素攻击经过防御减伤，造成 {damage} 点伤害");
            }
        }
    }

    // 应用元素效果
    private void ApplyElementEffect(GameObject target, AttackInfo attackInfo)
    {
        if (Random.value > elementalEffectChance) return;

        switch (attackType)
        {
            case AttackType.Fire:
                ApplyFireEffect(target, attackInfo);
                break;
            case AttackType.Wind:
                ApplyWindEffect(target, attackInfo);
                break;
            case AttackType.Ice:
                ApplyIceEffect(target, attackInfo);
                break;
            case AttackType.Thunder:
                ApplyThunderEffect(target, attackInfo);
                break;
        }

        OnElementEffectApplied?.Invoke(attackType, target);
    }

    // 火元素效果：持续伤害
    private void ApplyFireEffect(GameObject target, AttackInfo attackInfo)
    {
        BurnEffect burnEffect = target.GetComponent<BurnEffect>();
        if (burnEffect == null) burnEffect = target.AddComponent<BurnEffect>();

        int burnDamage = Mathf.RoundToInt(attackInfo.baseDamage * 0.3f);
        burnEffect.StartBurning(burnDamage, fireDamageDuration, gameObject);

        Debug.Log($"火元素: {target.name} 开始燃烧，持续{fireDamageDuration}秒");
    }

    // 风元素效果：击退
    private void ApplyWindEffect(GameObject target, AttackInfo attackInfo)
    {
        Rigidbody targetRb = target.GetComponent<Rigidbody>();
        if (targetRb != null)
        {
            Vector3 direction = (target.transform.position - transform.position).normalized;
            direction.y = 0.3f; // 稍微向上击飞
            targetRb.AddForce(direction * windKnockbackForce, ForceMode.Impulse);
        }

        Debug.Log($"风元素: {target.name} 被击退");
    }

    // 冰元素效果：冻结
    private void ApplyIceEffect(GameObject target, AttackInfo attackInfo)
    {
        FreezeEffect freezeEffect = target.GetComponent<FreezeEffect>();
        if (freezeEffect == null) freezeEffect = target.AddComponent<FreezeEffect>();

        freezeEffect.StartFreeze(iceFreezeDuration);

        Debug.Log($"冰元素: {target.name} 被冻结{iceFreezeDuration}秒");
    }

    // 雷元素效果：连锁闪电
    private void ApplyThunderEffect(GameObject target, AttackInfo attackInfo)
    {
        Collider[] nearbyEnemies = Physics.OverlapSphere(target.transform.position, thunderChainRange, enemyLayer);
        int chainDamage = Mathf.RoundToInt(attackInfo.baseDamage * 0.5f);

        foreach (Collider enemy in nearbyEnemies)
        {
            if (enemy.gameObject != target && enemy.gameObject != gameObject)
            {
                Attribute enemyAttribute = enemy.GetComponent<Attribute>();
                if (enemyAttribute != null)
                {
                    enemyAttribute.TakeDamage(chainDamage, gameObject);
                    Debug.Log($"雷元素连锁: {enemy.name} 受到{chainDamage}伤害");
                }
            }
        }

        Debug.Log($"雷元素: 连锁攻击{nearbyEnemies.Length - 1}个目标");
    }

    // 设置攻击类型
    public void SetAttackType(AttackType newType)
    {
        attackType = newType;
    }

    // 设置攻击范围
    public void SetAttackRange(float newRange)
    {
        attackRange = Mathf.Max(0f, newRange);
    }

    // 设置攻击冷却
    public void SetAttackCooldown(float newCooldown)
    {
        attackCooldown = Mathf.Max(0f, newCooldown);
    }

    // 设置攻击按键
    public void SetAttackKey(KeyCode newKey)
    {
        attackKey = newKey;
    }

    // 强制立即攻击（无视冷却）
    public void ForceAttack()
    {
        lastAttackTime = Time.time - attackCooldown;
        PerformAttack();
    }

    // 在编辑器中显示攻击范围
    private void OnDrawGizmosSelected()
    {
        // 攻击范围
        Gizmos.color = attackType == AttackType.Physical ? Color.red : GetElementColor(attackType);

        if (attackPoint != null)
        {
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }

        // 雷元素连锁范围
        if (attackType == AttackType.Thunder)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, thunderChainRange);
        }
    }

    // 获取元素颜色
    private Color GetElementColor(AttackType type)
    {
        switch (type)
        {
            case AttackType.Fire: return Color.red;        // 红色
            case AttackType.Wind: return Color.green;      // 绿色
            case AttackType.Ice: return Color.blue;        // 蓝色
            case AttackType.Thunder: return Color.yellow;  // 黄色
            default: return Color.white;
        }
    }
}

// 燃烧效果组件
public class BurnEffect : MonoBehaviour
{
    private float burnTimer;
    private int burnDamage;
    private float interval = 1f; // 每秒一次伤害
    private GameObject damageSource;

    public void StartBurning(int damage, float duration, GameObject source)
    {
        burnDamage = damage;
        burnTimer = duration;
        damageSource = source;
        StartCoroutine(BurnCoroutine());
    }

    private IEnumerator BurnCoroutine()
    {
        while (burnTimer > 0)
        {
            yield return new WaitForSeconds(interval);

            Attribute attribute = GetComponent<Attribute>();
            if (attribute != null && attribute.IsAlive)
            {
                attribute.TakeDamage(burnDamage, damageSource);
                Debug.Log($"燃烧伤害: {burnDamage}");
            }

            burnTimer -= interval;
        }

        Destroy(this);
    }
}

// 冻结效果组件
public class FreezeEffect : MonoBehaviour
{
    private float freezeTimer;
    private Vector3 originalVelocity;
    private Rigidbody rb;

    public void StartFreeze(float duration)
    {
        freezeTimer = duration;
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            originalVelocity = rb.velocity;
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
        }

        StartCoroutine(FreezeCoroutine());
    }

    private IEnumerator FreezeCoroutine()
    {
        yield return new WaitForSeconds(freezeTimer);

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.velocity = originalVelocity;
        }

        Destroy(this);
    }
}