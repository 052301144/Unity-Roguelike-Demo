using UnityEngine;
using System.Collections;

/// <summary>
/// 陨石术 - 召唤陨石从空中坠落，造成大范围火焰伤害
/// </summary>
public class Area_Meteor : SM_AreaSkillBase
{
    [Header("陨石属性")]
    public float meteorDamage = 60f;
    public float splashDamage = 30f;
    public float splashRadius = 3f;
    public float burnDPS = 10f;
    public float burnDuration = 4f;

    [Header("陨石下落")]
    public float fallHeight = 10f;
    public float fallDuration = 1f;

    [Header("目标层级")]
    public LayerMask targetLayer; // 添加目标层级定义

    [Header("视觉效果")]
    public GameObject meteorPrefab;
    public GameObject impactEffect;

    protected override void ConfigureEffectInstance(GameObject instance, Vector2 direction, float areaRadius)
    {
        StartCoroutine(SpawnMeteor(instance.transform.position));
        Destroy(instance, fallDuration + 0.5f); // 稍后销毁空对象
    }

    private IEnumerator SpawnMeteor(Vector2 targetPosition)
    {
        // 创建陨石对象
        GameObject meteor = null;
        if (meteorPrefab != null)
        {
            meteor = Instantiate(meteorPrefab, targetPosition + Vector2.up * fallHeight, Quaternion.identity);
        }
        else
        {
            meteor = new GameObject("Meteor");
            meteor.transform.position = targetPosition + Vector2.up * fallHeight;
            var renderer = meteor.AddComponent<SpriteRenderer>();
            renderer.color = Color.red;
        }

        // 添加陨石控制器
        var meteorController = meteor.AddComponent<MeteorController>();
        meteorController.Initialize(targetPosition, fallDuration, meteorDamage, splashDamage,
                                  splashRadius, burnDPS, burnDuration, impactEffect, targetLayer);

        Debug.Log($"[陨石术] 在 {targetPosition} 召唤陨石");

        yield return new WaitForSeconds(fallDuration);
    }
}

public class MeteorController : MonoBehaviour
{
    private Vector2 _targetPosition;
    private float _fallDuration;
    private float _meteorDamage;
    private float _splashDamage;
    private float _splashRadius;
    private float _burnDPS;
    private float _burnDuration;
    private GameObject _impactEffect;
    private LayerMask _targetLayer;

    private float _fallTimer;
    private Vector2 _startPosition;
    private bool _hasLanded = false;

    public void Initialize(Vector2 targetPosition, float fallDuration, float meteorDamage,
                          float splashDamage, float splashRadius, float burnDPS, float burnDuration,
                          GameObject impactEffect, LayerMask targetLayer)
    {
        _targetPosition = targetPosition;
        _fallDuration = fallDuration;
        _meteorDamage = meteorDamage;
        _splashDamage = splashDamage;
        _splashRadius = splashRadius;
        _burnDPS = burnDPS;
        _burnDuration = burnDuration;
        _impactEffect = impactEffect;
        _targetLayer = targetLayer;

        _startPosition = transform.position;

        // 添加碰撞器用于检测直接命中
        var collider = gameObject.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;
        collider.isTrigger = true;
    }

    private void Update()
    {
        if (_hasLanded) return;

        _fallTimer += Time.deltaTime;
        float t = _fallTimer / _fallDuration;

        // 下落运动（带有加速效果）
        float height = Mathf.Lerp(_startPosition.y, _targetPosition.y, t);
        float horizontal = Mathf.Lerp(_startPosition.x, _targetPosition.x, t);

        transform.position = new Vector2(horizontal, height);

        // 旋转效果
        transform.Rotate(0, 0, 360 * Time.deltaTime);

        if (t >= 1f)
        {
            OnImpact();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasLanded) return;

        // 直接命中检测
        if (((1 << other.gameObject.layer) & _targetLayer) != 0)
        {
            var damageable = other.GetComponentInParent<SM_IDamageable>();
            if (damageable != null)
            {
                ApplyDirectHit(damageable);
            }
        }

        // 地面碰撞
        if (other.CompareTag("Ground") || other.CompareTag("Wall"))
        {
            OnImpact();
        }
    }

    private void ApplyDirectHit(SM_IDamageable target)
    {
        var info = new SM_DamageInfo
        {
            Amount = _meteorDamage,
            Element = SM_Element.Fire,
            IgnoreDefense = true, // 陨石直接命中无视防御
            CritChance = 0.2f,
            CritMultiplier = 2f
        };
        target.ApplyDamage(info);

        Debug.Log($"[陨石直接命中] 对 {target.GetTransform().name} 造成 {_meteorDamage} 点伤害");
    }

    private void OnImpact()
    {
        if (_hasLanded) return;
        _hasLanded = true;

        // 创建撞击效果
        if (_impactEffect != null)
        {
            Instantiate(_impactEffect, transform.position, Quaternion.identity);
        }

        // 范围伤害
        ApplySplashDamage();

        Debug.Log($"[陨石撞击] 在 {transform.position} 爆炸");
        Destroy(gameObject);
    }

    private void ApplySplashDamage()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, _splashRadius, _targetLayer);

        foreach (var hit in hits)
        {
            var damageable = hit.GetComponentInParent<SM_IDamageable>();
            if (damageable != null)
            {
                var info = new SM_DamageInfo
                {
                    Amount = _splashDamage,
                    Element = SM_Element.Fire,
                    IgnoreDefense = false,
                    CritChance = 0.1f,
                    CritMultiplier = 1.5f
                };
                damageable.ApplyDamage(info);

                // 燃烧效果
                var burnable = hit.GetComponentInParent<SM_IBurnable>();
                if (burnable != null)
                {
                    burnable.ApplyBurn(_burnDPS, _burnDuration);
                }
            }
        }

        Debug.Log($"[陨石爆炸] 影响 {hits.Length} 个目标");
    }

    private void OnDrawGizmosSelected()
    {
        if (!_hasLanded)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);

            // 绘制预测落点
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.DrawWireSphere(_targetPosition, _splashRadius);
        }
    }
}