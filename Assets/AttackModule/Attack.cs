using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attack : MonoBehaviour
{
    [Header("��������")]
    [SerializeField] private AttackType attackType = AttackType.Physical; // �������ͣ�Ĭ��������
    [SerializeField] private KeyCode attackKey = KeyCode.Mouse0; // ����������Ĭ��Ϊ������
    [SerializeField] private float attackRange = 2f; // ������Χ
    [SerializeField] private float attackCooldown = 1f; // ������ȴʱ��
    [SerializeField] private LayerMask enemyLayer; // ���˲㼶

    [Header("�����ж�")]
    [SerializeField] private Transform attackPoint; // �����ж���
    [SerializeField] private float attackRadius = 0.5f; // �����ж��뾶

    [Header("����������")]
    [SerializeField] private float physicalCritRate = 0.1f; // ��������
    [SerializeField] private float physicalCritDamage = 1.5f; // �������˺�����

    [Header("Ԫ�ع�������")]
    [SerializeField] private float elementalEffectChance = 0.7f; // Ԫ��Ч����������
    [SerializeField] private float fireDamageDuration = 3f; // ��Ԫ�س����˺�ʱ��
    [SerializeField] private float iceFreezeDuration = 2f; // ��Ԫ�ض���ʱ��
    [SerializeField] private float windKnockbackForce = 5f; // ��Ԫ�ػ�������
    [SerializeField] private float thunderChainRange = 3f; // ��Ԫ��������Χ

    // ��������ö��
    public enum AttackType
    {
        Physical, // �����������ӷ������ͱ���
        Fire,     // ��Ԫ�أ������˺�
        Wind,     // ��Ԫ�أ�����Ч��
        Ice,      // ��Ԫ�أ��������
        Thunder   // ��Ԫ�أ���Χ����
    }

    private float lastAttackTime; // �ϴι���ʱ��
    private Attribute attackerAttribute; // ����������

    // ���Է�����
    public float AttackRange => attackRange;
    public bool CanAttack => Time.time >= lastAttackTime + attackCooldown;
    public int CurrentAttack => attackerAttribute != null ? attackerAttribute.Attack : 0;
    public AttackType Type => attackType;

    // �¼�
    public System.Action<GameObject, int, AttackType> OnAttackHit; // ���������¼���Ŀ�꣬��ɵ��˺����������ͣ�
    public System.Action<AttackType> OnAttackPerformed; // ����ִ���¼�
    public System.Action<AttackType, GameObject> OnElementEffectApplied; // Ԫ��Ч��Ӧ���¼�

    // ������Ϣ�ṹ��
    public struct AttackInfo
    {
        public GameObject target;
        public int baseDamage;
        public AttackType type;
        public bool isCrit;
        public Vector3 hitPoint;
    }

    //��ȡAttribute���
    private void Awake()
    {
        attackerAttribute = GetComponent<Attribute>();
    }

    private void Update()
    {
        // �������������
        if (Input.GetKeyDown(attackKey))
        {
            PerformAttack();
        }
    }

    // ִ�й���
    public void PerformAttack()
    {
        if (!CanAttack) return;

        // ��������ִ���¼�
        OnAttackPerformed?.Invoke(attackType);

        // ���й����ж�
        Collider[] hitEnemies = GetHitEnemies();

        foreach (Collider enemy in hitEnemies)
        {
            if (enemy.gameObject != gameObject) // �������Լ�
            {
                ProcessAttackHit(enemy.gameObject);
            }
        }

        lastAttackTime = Time.time;
    }

    // �����ض�Ŀ��
    public void AttackTarget(GameObject target)
    {
        if (target == null || !CanAttack) return;

        // ��������ִ���¼�
        OnAttackPerformed?.Invoke(attackType);

        ProcessAttackHit(target);
        lastAttackTime = Time.time;
    }

    // ��ȡ���еĵ���
    private Collider[] GetHitEnemies()
    {
        if (attackPoint != null)
        {
            // ʹ�ù�����������μ��
            return Physics.OverlapSphere(attackPoint.position, attackRadius, enemyLayer);
        }
        else
        {
            // ʹ�ý�ɫλ�ý������μ��
            return Physics.OverlapSphere(transform.position, attackRange, enemyLayer);
        }
    }

    // ����������
    private void ProcessAttackHit(GameObject target)
    {
        if (target == null) return;

        // ��ȡ�����ߵĹ�����
        int baseDamage = attackerAttribute != null ? attackerAttribute.Attack : 0;
        if (baseDamage <= 0) return;

        // ���������˺�
        AttackInfo attackInfo = new AttackInfo
        {
            target = target,
            baseDamage = baseDamage,
            type = attackType,
            hitPoint = target.transform.position
        };

        int finalDamage = CalculateDamage(attackInfo);

        // �������������¼�
        OnAttackHit?.Invoke(target, finalDamage, attackType);

        // Ӧ���˺�
        ApplyDamage(target, finalDamage, attackInfo);

        // Ӧ��Ԫ��Ч��
        if (attackType != AttackType.Physical)
        {
            ApplyElementEffect(target, attackInfo);
        }
    }

    // �����˺�
    private int CalculateDamage(AttackInfo attackInfo)
    {
        int baseDamage = attackInfo.baseDamage;

        switch (attackType)
        {
            case AttackType.Physical:
                // �����������㱩�������ӷ���
                bool isCrit = Random.value < physicalCritRate;
                attackInfo.isCrit = isCrit;
                int physicalDamage = Mathf.RoundToInt(baseDamage * (isCrit ? physicalCritDamage : 1f));
                Debug.Log($"������: ����{baseDamage}, ����{isCrit}, ����{physicalDamage}");
                return physicalDamage;

            default:
                // Ԫ�ع������������������������
                Attribute targetAttribute = attackInfo.target.GetComponent<Attribute>();
                if (targetAttribute != null)
                {
                    // ʹ��Ŀ��ķ������������
                    float defenseMultiplier = Mathf.Clamp(1f - (targetAttribute.Defense * 0.01f), 0.1f, 1f);
                    int elementalDamage = Mathf.RoundToInt(baseDamage * defenseMultiplier);
                    Debug.Log($"Ԫ�ع���: ����{baseDamage}, ��������{defenseMultiplier:P0}, ����{elementalDamage}");
                    return elementalDamage;
                }
                return baseDamage;
        }
    }

    // Ӧ���˺�
    private void ApplyDamage(GameObject target, int damage, AttackInfo attackInfo)
    {
        Attribute targetAttribute = target.GetComponent<Attribute>();
        if (targetAttribute != null)
        {
            if (attackType == AttackType.Physical)
            {
                // �����������ӷ�����ֱ����ɼ������˺�
                targetAttribute.TakeTrueDamage(damage, gameObject);
                Debug.Log($"���������ӷ�������� {damage} ����ʵ�˺�");
            }
            else
            {
                // Ԫ�ع��������������������
                targetAttribute.TakeDamage(damage, gameObject);
                Debug.Log($"Ԫ�ع��������������ˣ���� {damage} ���˺�");
            }
        }
    }

    // Ӧ��Ԫ��Ч��
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

    // ��Ԫ��Ч���������˺�
    private void ApplyFireEffect(GameObject target, AttackInfo attackInfo)
    {
        BurnEffect burnEffect = target.GetComponent<BurnEffect>();
        if (burnEffect == null) burnEffect = target.AddComponent<BurnEffect>();

        int burnDamage = Mathf.RoundToInt(attackInfo.baseDamage * 0.3f);
        burnEffect.StartBurning(burnDamage, fireDamageDuration, gameObject);

        Debug.Log($"��Ԫ��: {target.name} ��ʼȼ�գ�����{fireDamageDuration}��");
    }

    // ��Ԫ��Ч��������
    private void ApplyWindEffect(GameObject target, AttackInfo attackInfo)
    {
        Rigidbody targetRb = target.GetComponent<Rigidbody>();
        if (targetRb != null)
        {
            Vector3 direction = (target.transform.position - transform.position).normalized;
            direction.y = 0.3f; // ��΢���ϻ���
            targetRb.AddForce(direction * windKnockbackForce, ForceMode.Impulse);
        }

        Debug.Log($"��Ԫ��: {target.name} ������");
    }

    // ��Ԫ��Ч��������
    private void ApplyIceEffect(GameObject target, AttackInfo attackInfo)
    {
        FreezeEffect freezeEffect = target.GetComponent<FreezeEffect>();
        if (freezeEffect == null) freezeEffect = target.AddComponent<FreezeEffect>();

        freezeEffect.StartFreeze(iceFreezeDuration);

        Debug.Log($"��Ԫ��: {target.name} ������{iceFreezeDuration}��");
    }

    // ��Ԫ��Ч������������
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
                    Debug.Log($"��Ԫ������: {enemy.name} �ܵ�{chainDamage}�˺�");
                }
            }
        }

        Debug.Log($"��Ԫ��: ��������{nearbyEnemies.Length - 1}��Ŀ��");
    }

    // ���ù�������
    public void SetAttackType(AttackType newType)
    {
        attackType = newType;
    }

    // ���ù�����Χ
    public void SetAttackRange(float newRange)
    {
        attackRange = Mathf.Max(0f, newRange);
    }

    // ���ù�����ȴ
    public void SetAttackCooldown(float newCooldown)
    {
        attackCooldown = Mathf.Max(0f, newCooldown);
    }

    // ���ù�������
    public void SetAttackKey(KeyCode newKey)
    {
        attackKey = newKey;
    }

    // ǿ������������������ȴ��
    public void ForceAttack()
    {
        lastAttackTime = Time.time - attackCooldown;
        PerformAttack();
    }

    // �ڱ༭������ʾ������Χ
    private void OnDrawGizmosSelected()
    {
        // ������Χ
        Gizmos.color = attackType == AttackType.Physical ? Color.red : GetElementColor(attackType);

        if (attackPoint != null)
        {
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }

        // ��Ԫ��������Χ
        if (attackType == AttackType.Thunder)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, thunderChainRange);
        }
    }

    // ��ȡԪ����ɫ
    private Color GetElementColor(AttackType type)
    {
        switch (type)
        {
            case AttackType.Fire: return Color.red;        // ��ɫ
            case AttackType.Wind: return Color.green;      // ��ɫ
            case AttackType.Ice: return Color.blue;        // ��ɫ
            case AttackType.Thunder: return Color.yellow;  // ��ɫ
            default: return Color.white;
        }
    }
}

// ȼ��Ч�����
public class BurnEffect : MonoBehaviour
{
    private float burnTimer;
    private int burnDamage;
    private float interval = 1f; // ÿ��һ���˺�
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
                Debug.Log($"ȼ���˺�: {burnDamage}");
            }

            burnTimer -= interval;
        }

        Destroy(this);
    }
}

// ����Ч�����
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