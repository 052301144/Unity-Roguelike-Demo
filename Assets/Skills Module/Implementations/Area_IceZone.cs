using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 冰霜领域 - 在目标区域生成冰霜区域，伤害并冻结敌人
/// </summary>
public class Area_IceZone : SM_AreaSkillBase
{
    [Header("寒冰领域属性")]
    public float zoneDuration = 5f;
    public float damagePerSecond = 10f;
    public float freezeChance = 0.1f; // 10%几率冻结
    public float freezeDuration = 2f;
    public float zoneRadius = 4f;
    public LayerMask targetLayer;

    protected override void ConfigureEffectInstance(GameObject instance, Vector2 direction, float areaRadius)
    {
        // 获取或添加控制器
        var iceZone = instance.GetComponent<IceZoneController>();
        if (iceZone == null)
        {
            iceZone = instance.AddComponent<IceZoneController>();
        }

        // 直接设置参数
        iceZone.duration = zoneDuration;
        iceZone.damagePerSecond = damagePerSecond;
        iceZone.freezeChance = freezeChance;
        iceZone.freezeDuration = freezeDuration;
        iceZone.radius = zoneRadius;
        iceZone.targetLayer = targetLayer;

        Debug.Log($"[寒冰领域] 在 {instance.transform.position} 创建冰霜区域");
    }
}

public class IceZoneController : MonoBehaviour
{
    [Header("冰区域配置")]
    public float duration = 5f;
    public float damagePerSecond = 10f;
    public float freezeChance = 0.1f;
    public float freezeDuration = 2f;
    public float radius = 4f;
    public LayerMask targetLayer;

    // 私有变量
    private bool _isActive = true;
    private float _timer;
    private float _damageTimer;
    private readonly List<Collider2D> _insideZone = new List<Collider2D>();
    private CircleCollider2D _circleCollider;

    private void Start()
    {
        InitializeComponents();
        StartCoroutine(ZoneLifecycle());
    }

    private void InitializeComponents()
    {
        // 配置碰撞器 - 逻辑必需
        _circleCollider = GetComponent<CircleCollider2D>();
        if (_circleCollider == null)
        {
            _circleCollider = gameObject.AddComponent<CircleCollider2D>();
        }

        _circleCollider.radius = radius;
        _circleCollider.isTrigger = true;

        // SpriteRenderer 是可选的 - 用于视觉效果
        // 如果预制体没有添加 SpriteRenderer，将不会有视觉效果
        // 如果预制体添加了 SpriteRenderer，会自动使用它
    }

    private IEnumerator ZoneLifecycle()
    {
        Debug.Log($"[寒冰领域] 开始，持续 {duration} 秒");

        // 活跃阶段
        float activeTimer = 0f;
        while (activeTimer < duration && _isActive)
        {
            activeTimer += Time.deltaTime;
            _damageTimer += Time.deltaTime;

            // 每秒造成一次伤害
            if (_damageTimer >= 1f)
            {
                _damageTimer = 0f;
                ApplyZoneDamage();
            }

            yield return null;
        }

        // 结束
        DeactivateZone();
    }

    private void ApplyZoneDamage()
    {
        if (!_isActive) return;

        for (int i = _insideZone.Count - 1; i >= 0; i--)
        {
            var collider = _insideZone[i];
            if (collider == null)
            {
                _insideZone.RemoveAt(i);
                continue;
            }

            var damageable = collider.GetComponentInParent<SM_IDamageable>();
            if (damageable != null)
            {
                var info = new SM_DamageInfo
                {
                    Amount = damagePerSecond,
                    Element = SM_Element.Ice,
                    IgnoreDefense = false,
                    CritChance = 0f,
                    CritMultiplier = 1f
                };
                damageable.ApplyDamage(info);

                // 几率冻结
                if (Random.value < freezeChance)
                {
                    var freezable = collider.GetComponentInParent<SM_IFreezable>();
                    if (freezable != null)
                    {
                        freezable.Freeze(freezeDuration);
                        Debug.Log($"[寒冰领域] 冻结敌人: {collider.name}");
                    }
                }
            }
        }
    }

    private void DeactivateZone()
    {
        if (!_isActive) return;

        _isActive = false;
        _insideZone.Clear();

        // 禁用碰撞器
        if (_circleCollider != null)
            _circleCollider.enabled = false;

        Debug.Log("[寒冰领域] 区域效果结束");

        // 销毁对象
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_isActive) return;

        if (((1 << other.gameObject.layer) & targetLayer) != 0)
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
        if (!_isActive) return;

        if (((1 << other.gameObject.layer) & targetLayer) != 0)
        {
            if (_insideZone.Contains(other))
            {
                _insideZone.Remove(other);
                Debug.Log($"[寒冰领域] 敌人离开区域: {other.name}");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    // 确保在销毁时停止所有协程
    private void OnDestroy()
    {
        _isActive = false;
        StopAllCoroutines();
    }
}