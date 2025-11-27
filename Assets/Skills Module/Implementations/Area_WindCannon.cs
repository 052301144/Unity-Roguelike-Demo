using UnityEngine;
using System.Collections;

/// <summary>
/// 风压炮 - 使用移动按键控制射击方向，发射高压风弹，命中后产生爆炸并将周围敌人击退
/// </summary>
public class Area_WindCannon : SM_AreaSkillBase
{
    [Header("风压炮属性")]
    public float projectileSpeed = 15f;
    public float directDamage = 25f;
    public float explosionDamage = 35f;
    public float explosionRadius = 3f;
    public float knockbackForce = 8f;
    public float knockbackDuration = 0.5f;

    [Header("方向控制")]
    public bool useMovementKeysForAim = true; // 使用移动按键控制方向

    [Header("目标层级")]
    public LayerMask targetLayer; // 添加目标层级定义

    [Header("视觉效果")]
    public GameObject windProjectile;
    public GameObject explosionEffect;

    protected override void ConfigureEffectInstance(GameObject instance, Vector2 direction, float areaRadius)
    {
        // 确定最终发射方向
        Vector2 finalDirection = DetermineFinalDirection();

        var windCannon = instance.AddComponent<WindCannonController>();
        windCannon.Initialize(finalDirection, projectileSpeed, directDamage, explosionDamage,
                             explosionRadius, knockbackForce, knockbackDuration,
                             targetLayer, windProjectile, explosionEffect);

        Debug.Log($"[风压炮] 发射风弹，方向: {finalDirection}");
    }

    /// <summary>
    /// 确定最终发射方向 - 优先使用移动按键，否则使用角色朝向
    /// </summary>
    private Vector2 DetermineFinalDirection()
    {
        Vector2 finalDir = Vector2.zero;

        // 检测移动按键输入
        if (useMovementKeysForAim)
        {
            finalDir = GetMovementKeyDirection();
        }

        // 如果没有移动按键输入，使用角色朝向
        if (finalDir == Vector2.zero && character != null)
        {
            finalDir = character.AimDirection.normalized;
            Debug.Log($"[风压炮] 使用角色朝向: {finalDir}");
        }

        // 如果还是没有有效方向，使用默认向右
        if (finalDir == Vector2.zero)
        {
            finalDir = Vector2.right;
            Debug.Log($"[风压炮] 使用默认方向: {finalDir}");
        }

        return finalDir.normalized;
    }

    /// <summary>
    /// 获取移动按键方向
    /// </summary>
    private Vector2 GetMovementKeyDirection()
    {
        Vector2 inputDirection = Vector2.zero;

        // 检测WASD按键
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            inputDirection.y += 1f;
        }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            inputDirection.y -= 1f;
        }
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            inputDirection.x -= 1f;
        }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            inputDirection.x += 1f;
        }

        // 如果有输入，记录日志
        if (inputDirection != Vector2.zero)
        {
            Debug.Log($"[风压炮] 检测到移动按键输入: {inputDirection}");
        }

        return inputDirection;
    }
}

public class WindCannonController : MonoBehaviour
{
    private Vector2 _direction;
    private float _speed;
    private float _directDamage;
    private float _explosionDamage;
    private float _explosionRadius;
    private float _knockbackForce;
    private float _knockbackDuration;
    private LayerMask _targetLayer;
    private GameObject _projectileEffect;
    private GameObject _explosionEffect;

    private Rigidbody2D _rb;
    private Vector2 _startPos;
    private bool _hasExploded = false;
    private float _maxDistance = 15f;

    public void Initialize(Vector2 direction, float speed, float directDamage, float explosionDamage,
                          float explosionRadius, float knockbackForce, float knockbackDuration,
                          LayerMask targetLayer, GameObject projectileEffect, GameObject explosionEffect)
    {
        _direction = direction.normalized;
        _speed = speed;
        _directDamage = directDamage;
        _explosionDamage = explosionDamage;
        _explosionRadius = explosionRadius;
        _knockbackForce = knockbackForce;
        _knockbackDuration = knockbackDuration;
        _targetLayer = targetLayer;
        _projectileEffect = projectileEffect;
        _explosionEffect = explosionEffect;

        // 设置刚体
        _rb = GetComponent<Rigidbody2D>();
        if (_rb == null) _rb = gameObject.AddComponent<Rigidbody2D>();

        _rb.gravityScale = 0f;
        _rb.velocity = _direction * _speed;
        _startPos = transform.position;

        // 添加碰撞器
        var collider = gameObject.AddComponent<CircleCollider2D>();
        collider.radius = 0.3f;
        collider.isTrigger = true;

        // 创建投射物视觉效果
        if (_projectileEffect != null)
        {
            var effect = Instantiate(_projectileEffect, transform);
            effect.transform.localPosition = Vector3.zero;

            // 调整方向
            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            effect.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
        else
        {
            // 简单视觉效果
            var renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.color = new Color(0f, 1f, 1f, 0.8f);
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = new Vector2(0.5f, 0.5f);
        }

        Debug.Log($"[风压炮] 初始化完成，速度: {_speed}, 伤害: {_directDamage}, 爆炸伤害: {_explosionDamage}, 方向: {_direction}");

        // 自动销毁
        Destroy(gameObject, 5f);
    }

    private void Update()
    {
        if (_hasExploded) return;

        // 检查距离
        if (Vector2.Distance(_startPos, transform.position) >= _maxDistance)
        {
            Debug.Log($"[风压炮] 达到最大距离，自动引爆");
            Explode();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasExploded) return;

        // 击中敌人
        if (((1 << other.gameObject.layer) & _targetLayer) != 0)
        {
            ApplyDirectDamage(other);
            Explode();
        }
        // 击中障碍物
        else if (other.CompareTag("Wall") || other.CompareTag("Ground"))
        {
            Explode();
        }
    }

    private void ApplyDirectDamage(Collider2D targetCollider)
    {
        var damageable = targetCollider.GetComponentInParent<SM_IDamageable>();
        if (damageable != null)
        {
            var info = new SM_DamageInfo
            {
                Amount = _directDamage,
                Element = SM_Element.Wind,
                IgnoreDefense = false,
                CritChance = 0.2f,
                CritMultiplier = 1.5f
            };
            damageable.ApplyDamage(info);

            Debug.Log($"[风压炮] 直接命中 {targetCollider.name}，造成 {_directDamage} 点风属性伤害");
        }
    }

    private void Explode()
    {
        if (_hasExploded) return;
        _hasExploded = true;

        // 停止移动
        if (_rb != null) _rb.velocity = Vector2.zero;

        // 创建爆炸效果
        if (_explosionEffect != null)
        {
            var explosion = Instantiate(_explosionEffect, transform.position, Quaternion.identity);
            explosion.transform.localScale = Vector3.one * _explosionRadius * 2f;
            Destroy(explosion, 2f);
        }

        // 应用爆炸伤害和击退
        ApplyExplosionDamage();

        Debug.Log($"[风压炮] 在 {transform.position} 爆炸");

        // 销毁自身
        Destroy(gameObject, 0.1f);
    }

    private void ApplyExplosionDamage()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, _explosionRadius, _targetLayer);
        int affectedCount = 0;

        foreach (var hit in hits)
        {
            if (hit == null) continue;

            var damageable = hit.GetComponentInParent<SM_IDamageable>();
            if (damageable != null)
            {
                // 计算距离衰减的伤害
                float distance = Vector2.Distance(transform.position, hit.transform.position);
                float damageMultiplier = 1f - (distance / _explosionRadius);
                damageMultiplier = Mathf.Clamp(damageMultiplier, 0.2f, 1f); // 最少20%伤害

                float actualDamage = _explosionDamage * damageMultiplier;

                var info = new SM_DamageInfo
                {
                    Amount = actualDamage,
                    Element = SM_Element.Wind,
                    IgnoreDefense = false,
                    CritChance = 0f,
                    CritMultiplier = 1f
                };
                damageable.ApplyDamage(info);

                // 应用击退
                var knockbackable = hit.GetComponentInParent<SM_IKnockbackable>();
                if (knockbackable != null)
                {
                    Vector2 knockbackDir = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
                    if (knockbackDir == Vector2.zero) knockbackDir = Vector2.up;

                    // 距离越近，击退力越大
                    float forceMultiplier = 1f - (distance / _explosionRadius);
                    float actualForce = _knockbackForce * forceMultiplier;

                    knockbackable.Knockback(knockbackDir, actualForce, _knockbackDuration);

                    Debug.Log($"[风压炮爆炸] 对 {hit.name} 造成 {actualDamage} 点风属性伤害，击退力: {actualForce}");
                }

                affectedCount++;
            }
        }

        Debug.Log($"[风压炮] 爆炸影响 {affectedCount} 个敌人");
    }

    private void OnDrawGizmosSelected()
    {
        if (_hasExploded) return;

        // 绘制投射物轨迹
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(_startPos, transform.position);

        // 绘制爆炸范围预览
        Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, _explosionRadius);
    }
}