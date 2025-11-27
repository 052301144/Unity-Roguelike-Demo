using UnityEngine;
using System.Collections;

/// <summary>
///冰霜领域 在目标区域生成冰霜区域，减速并伤害敌人
/// </summary>
public class Area_IceZone : SM_AreaSkillBase
{
    [Header("寒冰领域属性")]
    public float zoneDuration = 5f;
    public float slowFactor = 0.5f; // 50%减速
    public float damagePerSecond = 10f;
    public float freezeChance = 0.1f; // 10%几率冻结
    public float freezeDuration = 2f;

    [Header("范围")]
    public float zoneRadius = 4f;

    [Header("目标层级")]
    public LayerMask targetLayer;

    protected override void ConfigureEffectInstance(GameObject instance, Vector2 direction, float areaRadius)
    {
        var iceZone = instance.AddComponent<IceZoneController>();
        iceZone.Initialize(zoneDuration, slowFactor, damagePerSecond, freezeChance, freezeDuration, zoneRadius, targetLayer);

        Debug.Log($"[寒冰领域] 在 {instance.transform.position} 创建冰霜区域");
    }
}

public class IceZoneController : MonoBehaviour
{
    private float _duration;
    private float _slowFactor;
    private float _damagePerSecond;
    private float _freezeChance;
    private float _freezeDuration;
    private float _radius;
    private LayerMask _targetLayer;

    private float _timer;
    private float _damageTimer;
    private readonly System.Collections.Generic.List<Collider2D> _insideZone =
        new System.Collections.Generic.List<Collider2D>();

    public void Initialize(float duration, float slowFactor, float damagePerSecond,
                          float freezeChance, float freezeDuration, float radius, LayerMask targetLayer)
    {
        _duration = duration;
        _slowFactor = slowFactor;
        _damagePerSecond = damagePerSecond;
        _freezeChance = freezeChance;
        _freezeDuration = freezeDuration;
        _radius = radius;
        _targetLayer = targetLayer;

        // 添加区域碰撞器
        var collider = gameObject.AddComponent<CircleCollider2D>();
        collider.radius = _radius;
        collider.isTrigger = true;

        // 添加视觉效果（这里可以替换为实际的冰霜特效）
        var spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.color = new Color(0.5f, 0.8f, 1f, 0.3f);
        spriteRenderer.drawMode = SpriteDrawMode.Sliced;
        spriteRenderer.size = Vector2.one * _radius * 2f;

        Destroy(gameObject, _duration);
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        _damageTimer += Time.deltaTime;

        // 每秒造成一次伤害
        if (_damageTimer >= 1f)
        {
            _damageTimer = 0f;
            ApplyZoneDamage();
        }

        // 更新区域内的敌人状态
        UpdateEnemiesInZone();
    }

    private void ApplyZoneDamage()
    {
        foreach (var collider in _insideZone)
        {
            if (collider != null)
            {
                var damageable = collider.GetComponentInParent<SM_IDamageable>();
                if (damageable != null)
                {
                    var info = new SM_DamageInfo
                    {
                        Amount = _damagePerSecond,
                        Element = SM_Element.Ice,
                        IgnoreDefense = false,
                        CritChance = 0f,
                        CritMultiplier = 1f
                    };
                    damageable.ApplyDamage(info);

                    // 几率冻结
                    if (Random.value < _freezeChance)
                    {
                        var freezable = collider.GetComponentInParent<SM_IFreezable>();
                        if (freezable != null)
                        {
                            freezable.Freeze(_freezeDuration);
                        }
                    }
                }
            }
        }
    }

    private void UpdateEnemiesInZone()
    {
        // 这里可以实现减速效果，需要扩展敌人移动组件
        foreach (var collider in _insideZone)
        {
            if (collider != null)
            {
                // 应用减速效果
                var enemy = collider.GetComponent<EnemyDamageable>();
                if (enemy != null)
                {
                    // 这里需要敌人有移动组件来应用减速
                    // enemy.ApplySlow(_slowFactor);
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & _targetLayer) != 0)
        {
            if (!_insideZone.Contains(other))
            {
                _insideZone.Add(other);
                Debug.Log($"[寒冰领域] 敌人进入区域: {other.name}");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & _targetLayer) != 0)
        {
            if (_insideZone.Contains(other))
            {
                _insideZone.Remove(other);

                // 移除减速效果
                var enemy = other.GetComponent<EnemyDamageable>();
                if (enemy != null)
                {
                    // enemy.RemoveSlow();
                }

                Debug.Log($"[寒冰领域] 敌人离开区域: {other.name}");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}