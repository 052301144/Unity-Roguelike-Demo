using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attack : MonoBehaviour
{
    [Header("��������")]
    [SerializeField] private AttackType attackType = AttackType.Physical; // �������ͣ�Ĭ��������
    [SerializeField] private KeyCode attackKey = KeyCode.J; // ����������Ĭ��ΪJ��
    [SerializeField] private float attackCooldown = 0.5f; // ������ȴʱ��
    [SerializeField] private LayerMask enemyLayer; // ���˲㼶

    [Header("�����ι�����Χ")]
    [SerializeField] private Transform attackPoint; // �����ж���
    [SerializeField] private Vector2 boxSize = new Vector2(2f, 1f); // �����ι��������С
    [SerializeField] private Vector2 attackOffset = Vector2.zero; // ������ƫ����
    [SerializeField] private float attackAngle = 0f; // ����������ת�Ƕ�

    [Header("��������")]
    [SerializeField] private bool showAttackRangeInGame = true; // ����ʱ��ʾ������Χ
    [SerializeField] private Color debugBoxColor =Color.red; // ���Կ���ɫ
    [SerializeField] private bool showDebugInfo = true; // ��ʾ������Ϣ

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
    private int facingDirection = 1; // ���ﳯ��1Ϊ�ң�-1Ϊ��
    private Material debugMaterial; // ���Բ���
    private Mesh debugMesh; // ��������

    // ���Է�����
    public Vector2 AttackBoxSize => boxSize;
    public bool CanAttack => Time.time >= lastAttackTime + attackCooldown;
    public int CurrentAttack => attackerAttribute != null ? attackerAttribute.Attack : 0;
    public AttackType Type => attackType;
    public float AttackRange => boxSize.x;
    public int FacingDirection => facingDirection; 

    // �¼�
    public System.Action<GameObject, int, AttackType> OnAttackHit; // ���������¼�
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

    private void Awake()
    {
        attackerAttribute = GetComponent<Attribute>();

        // ��ȡ��ʼ����
        UpdateFacingDirection();

        // ���������δ���ã�ʹ�ý�ɫ����λ��
        if (attackPoint == null)
        {
            attackPoint = transform;
            Debug.LogWarning("������δ���ã�ʹ�ý�ɫ����λ����Ϊ������");
        }

        // ��ʼ��������Դ
        InitializeDebugResources();
    }

    private void InitializeDebugResources()
    {
        // �������Բ���
        debugMaterial = new Material(Shader.Find("Sprites/Default"));
        debugMaterial.color = debugBoxColor;

        // ������������
        debugMesh = CreateBoxMesh();
    }

    private Mesh CreateBoxMesh()
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[4];
        Vector2[] uv = new Vector2[4];
        int[] triangles = new int[6];

        float halfWidth = boxSize.x * 0.5f;
        float halfHeight = boxSize.y * 0.5f;

        // ���ö���
        vertices[0] = new Vector3(-halfWidth, -halfHeight, 0);
        vertices[1] = new Vector3(halfWidth, -halfHeight, 0);
        vertices[2] = new Vector3(halfWidth, halfHeight, 0);
        vertices[3] = new Vector3(-halfWidth, halfHeight, 0);

        // ����UV
        uv[0] = new Vector2(0, 0);
        uv[1] = new Vector2(1, 0);
        uv[2] = new Vector2(1, 1);
        uv[3] = new Vector2(0, 1);

        // ����������
        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;
        triangles[3] = 0;
        triangles[4] = 2;
        triangles[5] = 3;

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;

        return mesh;
    }

    private void Update()
    {
        // �������ﳯ��
        UpdateFacingDirection();

        if (Input.GetKeyDown(attackKey))
        {
            PerformAttack();
        }

        
    }

    // �������ﳯ�� 
    private void UpdateFacingDirection()
    {
        int newDirection = facingDirection; // Ĭ�ϱ��ֵ�ǰ����

        // ����localScale.x�жϳ�����Ҫ������
        if (transform.localScale.x != 0)
        {
            newDirection = transform.localScale.x > 0 ? 1 : -1;
        }

        // �����Rigidbody2D�������ٶ��жϳ��򣨸���������
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null && Mathf.Abs(rb.velocity.x) > 0.1f)
        {
            newDirection = rb.velocity.x > 0 ? 1 : -1;
        }
        
        // ֻ�е�����ȷʵ�ı�ʱ�Ÿ���
        if (newDirection != facingDirection)
        {
            facingDirection = newDirection;
            if (showDebugInfo)
            {
                Debug.Log($"�������: {(facingDirection > 0 ? "��" : "��")}");
            }
        }
    }

    

    // ��ȡʵ�ʹ�����λ�ã�����ƫ�ƺͳ���
    private Vector2 GetActualAttackPosition()
    {
        Vector2 basePosition = attackPoint != null ? (Vector2)attackPoint.position : (Vector2)transform.position;

        // Ӧ��ƫ������Xƫ�ƻ���ݳ������
        Vector2 adjustedOffset = new Vector2(attackOffset.x * facingDirection, attackOffset.y);

        return basePosition + adjustedOffset;
    }

    // ��ȡ�����������ת�Ƕȣ����ǳ���
    private float GetActualAttackAngle()
    {
        return attackAngle * facingDirection;
    }

    public void PerformAttack()
    {
        if (!CanAttack)
        {
            if (showDebugInfo)
            {
                Debug.Log($"������ȴ�У�ʣ��ʱ�䣺{lastAttackTime + attackCooldown - Time.time:F2}��");
            }
            return;
        }

        if (showDebugInfo)
        {
            Debug.Log($"ִ�й���������{(facingDirection > 0 ? "��" : "��")}");
            Debug.Log($"��������λ��{GetActualAttackPosition()}����С{boxSize}���Ƕ�{GetActualAttackAngle()}��");
        }

        OnAttackPerformed?.Invoke(attackType);

        Collider2D[] hitEnemies = GetHitEnemies();

        if (showDebugInfo)
        {
            Debug.Log($"��⵽{hitEnemies.Length}������");
        }

        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemy != null && enemy.gameObject != gameObject)
            {
                ProcessAttackHit(enemy.gameObject);
            }
        }

        lastAttackTime = Time.time;
    }

    // ����Ϸ����ʱ���ƹ�����Χ
    private void OnRenderObject()
    {
        if (!showAttackRangeInGame || debugMaterial == null || debugMesh == null)
            return;

        // ���ò���
        debugMaterial.SetPass(0);

        // ���㹥�������λ�ú���ת
        Vector3 attackPos = GetActualAttackPosition();
        Quaternion rotation = Quaternion.Euler(0, 0, GetActualAttackAngle());

        // ��������
        Graphics.DrawMeshNow(debugMesh, attackPos, rotation);
    }

    [ContextMenu("�л�������Χ��ʾ")]
    public void ToggleAttackRangeDisplay()
    {
        showAttackRangeInGame = !showAttackRangeInGame;
        Debug.Log($"������Χ��ʾ: {showAttackRangeInGame}");
    }

    [ContextMenu("�л�������Ϣ")]
    public void ToggleDebugInfo()
    {
        showDebugInfo = !showDebugInfo;
        Debug.Log($"������Ϣ��ʾ: {showDebugInfo}");
    }

    [ContextMenu("���Թ�������")]
    private void TestAttackDirection()
    {
        Debug.Log($"��ǰ����: {(facingDirection > 0 ? "��" : "��")}");
        Debug.Log($"������λ��: {GetActualAttackPosition()}");
        Debug.Log($"��������Ƕ�: {GetActualAttackAngle()}��");
        Debug.Log($"LocalScale: {transform.localScale}");
    }

    [ContextMenu("ǿ������")]
    private void ForceRight()
    {
        facingDirection = 1;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x);
        transform.localScale = scale;
        Debug.Log($"ǿ������Ϊ���ң�facingDirection: {facingDirection}");
    }

    [ContextMenu("ǿ������")]
    private void ForceLeft()
    {
        facingDirection = -1;
        Vector3 scale = transform.localScale;
        scale.x = -Mathf.Abs(scale.x);
        transform.localScale = scale;
        Debug.Log($"ǿ������Ϊ����facingDirection: {facingDirection}");
    }

    [ContextMenu("ģ�����ҹ���")]
    private void SimulateRightAttack()
    {
        ForceRight();
        PerformAttack();
    }

    [ContextMenu("ģ�����󹥻�")]
    private void SimulateLeftAttack()
    {
        ForceLeft();
        PerformAttack();
    }

    public void AttackTarget(GameObject target)
    {
        if (target == null || !CanAttack) return;

        OnAttackPerformed?.Invoke(attackType);
        ProcessAttackHit(target);
        lastAttackTime = Time.time;
    }

    // ��ȡ���еĵ��ˣ�ʹ�ó����������⣩
    private Collider2D[] GetHitEnemies()
    {
        Vector2 actualAttackPosition = GetActualAttackPosition();
        float actualAttackAngle = GetActualAttackAngle();

        // ʹ��2D Box��ⳤ��������
        Collider2D[] hitColliders = Physics2D.OverlapBoxAll(
            actualAttackPosition,
            boxSize,
            actualAttackAngle,
            enemyLayer
        );

        // ���˵�����
        List<Collider2D> validEnemies = new List<Collider2D>();
        foreach (Collider2D collider in hitColliders)
        {
            if (collider.gameObject != gameObject)
            {
                validEnemies.Add(collider);
            }
        }

        return validEnemies.ToArray();
    }

    [ContextMenu("���Թ�������")]
    private void TestAttackArea()
    {
        Vector2 actualAttackPosition = GetActualAttackPosition();
        float actualAttackAngle = GetActualAttackAngle();

        Collider2D[] hits = Physics2D.OverlapBoxAll(actualAttackPosition, boxSize, actualAttackAngle, Physics2D.AllLayers);
        Debug.Log($"����������������ײ��: {hits.Length}");

        foreach (Collider2D hit in hits)
        {
            Debug.Log($"��⵽: {hit.gameObject.name} (�㼶: {LayerMask.LayerToName(hit.gameObject.layer)})");
        }
    }

    [ContextMenu("ǿ�����ò��Բ㼶")]
    private void ForceTestLayers()
    {
        enemyLayer = LayerMask.GetMask("Default", "Enemy");
        Debug.Log("������Ϊ���Default��Enemy�㼶");
    }

    private void ProcessAttackHit(GameObject target)
    {
        if (target == null) return;

        int baseDamage = attackerAttribute != null ? attackerAttribute.Attack : 0;
        if (baseDamage <= 0) return;

        AttackInfo attackInfo = new AttackInfo
        {
            target = target,
            baseDamage = baseDamage,
            type = attackType,
            hitPoint = target.transform.position
        };

        int finalDamage = CalculateDamage(attackInfo);
        OnAttackHit?.Invoke(target, finalDamage, attackType);
        ApplyDamage(target, finalDamage, attackInfo);

        if (attackType != AttackType.Physical)
        {
            ApplyElementEffect(target, attackInfo);
        }
    }

    private int CalculateDamage(AttackInfo attackInfo)
    {
        int baseDamage = attackInfo.baseDamage;

        switch (attackType)
        {
            case AttackType.Physical:
                bool isCrit = Random.value < physicalCritRate;
                attackInfo.isCrit = isCrit;
                int physicalDamage = Mathf.RoundToInt(baseDamage * (isCrit ? physicalCritDamage : 1f));
                if (showDebugInfo)
                {
                    Debug.Log($"������: ����{baseDamage}, ����{isCrit}, ����{physicalDamage}");
                }
                return physicalDamage;

            default:
                Attribute targetAttribute = attackInfo.target.GetComponent<Attribute>();
                if (targetAttribute != null)
                {
                    float defenseMultiplier = Mathf.Clamp(1f - (targetAttribute.Defense * 0.01f), 0.1f, 1f);
                    int elementalDamage = Mathf.RoundToInt(baseDamage * defenseMultiplier);
                    if (showDebugInfo)
                    {
                        Debug.Log($"Ԫ�ع���: ����{baseDamage}, ��������{defenseMultiplier:P0}, ����{elementalDamage}");
                    }
                    return elementalDamage;
                }
                return baseDamage;
        }
    }

    private void ApplyDamage(GameObject target, int damage, AttackInfo attackInfo)
    {
        Attribute targetAttribute = target.GetComponent<Attribute>();
        if (targetAttribute != null)
        {
            if (attackType == AttackType.Physical)
            {
                targetAttribute.TakeTrueDamage(damage, gameObject);
                if (showDebugInfo)
                {
                    Debug.Log($"���������ӷ�������� {damage} ����ʵ�˺�");
                }
            }
            else
            {
                targetAttribute.TakeDamage(damage, gameObject);
                if (showDebugInfo)
                {
                    Debug.Log($"Ԫ�ع��������������ˣ���� {damage} ���˺�");
                }
            }
        }
    }

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

    private void ApplyFireEffect(GameObject target, AttackInfo attackInfo)
    {
        BurnEffect burnEffect = target.GetComponent<BurnEffect>();
        if (burnEffect == null) burnEffect = target.AddComponent<BurnEffect>();

        int burnDamage = Mathf.RoundToInt(attackInfo.baseDamage * 0.3f);
        burnEffect.StartBurning(burnDamage, fireDamageDuration, gameObject);
        if (showDebugInfo)
        {
            Debug.Log($"��Ԫ��: {target.name} ��ʼȼ�գ�����{fireDamageDuration}��");
        }
    }

    private void ApplyWindEffect(GameObject target, AttackInfo attackInfo)
    {
        Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
        if (targetRb != null)
        {
            // ���˷�����ڹ�������
            Vector2 direction = new Vector2(facingDirection, 0.3f).normalized;
            targetRb.AddForce(direction * windKnockbackForce, ForceMode2D.Impulse);
        }

        if (showDebugInfo)
        {
            Debug.Log($"��Ԫ��: {target.name} ������");
        }
    }

    private void ApplyIceEffect(GameObject target, AttackInfo attackInfo)
    {
        FreezeEffect freezeEffect = target.GetComponent<FreezeEffect>();
        if (freezeEffect == null) freezeEffect = target.AddComponent<FreezeEffect>();

        freezeEffect.StartFreeze(iceFreezeDuration);
        if (showDebugInfo)
        {
            Debug.Log($"��Ԫ��: {target.name} ������{iceFreezeDuration}��");
        }
    }

    private void ApplyThunderEffect(GameObject target, AttackInfo attackInfo)
    {
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(target.transform.position, thunderChainRange, enemyLayer);
        int chainDamage = Mathf.RoundToInt(attackInfo.baseDamage * 0.5f);

        foreach (Collider2D enemy in nearbyEnemies)
        {
            if (enemy.gameObject != target && enemy.gameObject != gameObject)
            {
                Attribute enemyAttribute = enemy.GetComponent<Attribute>();
                if (enemyAttribute != null)
                {
                    enemyAttribute.TakeDamage(chainDamage, gameObject);
                    if (showDebugInfo)
                    {
                        Debug.Log($"��Ԫ������: {enemy.name} �ܵ�{chainDamage}�˺�");
                    }
                }
            }
        }

        if (showDebugInfo)
        {
            Debug.Log($"��Ԫ��: ��������{nearbyEnemies.Length - 1}��Ŀ��");
        }
    }

    // �ⲿ���ó���ķ���
    public void SetFacingDirection(int direction)
    {
        facingDirection = Mathf.Clamp(direction, -1, 1);
        // ͬ������localScale
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * facingDirection;
        transform.localScale = scale;
        Debug.Log($"���ó���: {(facingDirection > 0 ? "��" : "��")}");
    }

    public void SetAttackType(AttackType newType) => attackType = newType;
    public void SetBoxSize(Vector2 newSize)
    {
        boxSize = new Vector2(Mathf.Max(0.1f, newSize.x), Mathf.Max(0.1f, newSize.y));
        // ���µ�������
        if (debugMesh != null)
        {
            debugMesh = CreateBoxMesh();
        }
    }
    public void SetAttackCooldown(float newCooldown) => attackCooldown = Mathf.Max(0.1f, newCooldown);
    public void SetAttackKey(KeyCode newKey) => attackKey = newKey;
    public void SetAttackOffset(Vector2 newOffset) => attackOffset = newOffset;
    public void SetAttackAngle(float newAngle) => attackAngle = newAngle;

    public void ForceAttack()
    {
        lastAttackTime = Time.time - attackCooldown;
        PerformAttack();
    }

    // �ڳ�����ͼ�л��ƹ�����Χ
    private void OnDrawGizmosSelected()
    {
        // ���ƹ�����Χ
        Gizmos.color = attackType == AttackType.Physical ? Color.red : GetElementColor(attackType);

        Vector2 actualAttackPosition = GetActualAttackPosition();
        float actualAttackAngle = GetActualAttackAngle();

        // ���Ƴ����ι�������
        DrawGizmosBox(actualAttackPosition, boxSize, actualAttackAngle);

        // ���ƹ�����λ��ָʾ��
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(actualAttackPosition, 0.1f);

        if (attackType == AttackType.Thunder)
        {
            Gizmos.color = Color.yellow;
            DrawGizmosCircle(transform.position, thunderChainRange);
        }
    }

    // ���Ƴ����ι�������
    private void DrawGizmosBox(Vector3 center, Vector2 size, float angle)
    {
        Vector3[] corners = new Vector3[4];
        float halfWidth = size.x * 0.5f;
        float halfHeight = size.y * 0.5f;

        // ������ת����ĸ��ǵ�
        Quaternion rotation = Quaternion.Euler(0, 0, angle);

        corners[0] = center + rotation * new Vector3(-halfWidth, -halfHeight, 0);
        corners[1] = center + rotation * new Vector3(halfWidth, -halfHeight, 0);
        corners[2] = center + rotation * new Vector3(halfWidth, halfHeight, 0);
        corners[3] = center + rotation * new Vector3(-halfWidth, halfHeight, 0);

        // ����������
        Gizmos.DrawLine(corners[0], corners[1]);
        Gizmos.DrawLine(corners[1], corners[2]);
        Gizmos.DrawLine(corners[2], corners[3]);
        Gizmos.DrawLine(corners[3], corners[0]);
    }

    // ����2DԲ�εĸ�������
    private void DrawGizmosCircle(Vector3 center, float radius)
    {
        int segments = 32;
        float angle = 0f;

        Vector3 lastPoint = center + new Vector3(Mathf.Cos(0) * radius, Mathf.Sin(0) * radius, 0);

        for (int i = 1; i <= segments; i++)
        {
            angle = i * (2 * Mathf.PI / segments);
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            Gizmos.DrawLine(lastPoint, nextPoint);
            lastPoint = nextPoint;
        }
    }

    private Color GetElementColor(AttackType type)
    {
        switch (type)
        {
            case AttackType.Fire: return  Color.red; 
            case AttackType.Wind: return Color.green;
            case AttackType.Ice: return  Color.blue; 
            case AttackType.Thunder: return Color.yellow;
            default: return Color.white;
        }
    }

    private void OnDestroy()
    {
        // ������Դ
        if (debugMaterial != null)
            Destroy(debugMaterial);
        if (debugMesh != null)
            Destroy(debugMesh);
    }
}


// �޸�Ч������ʹ��2D����
public class BurnEffect : MonoBehaviour
{
    private float burnTimer;
    private int burnDamage;
    private float interval = 1f;
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

public class FreezeEffect : MonoBehaviour
{
    private float freezeTimer;
    private Vector2 originalVelocity;
    private Rigidbody2D rb;

    public void StartFreeze(float duration)
    {
        freezeTimer = duration;
        rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            originalVelocity = rb.velocity;
            rb.velocity = Vector2.zero;
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