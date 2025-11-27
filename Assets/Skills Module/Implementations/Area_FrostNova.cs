using UnityEngine;
using System.Collections;

/// <summary>
/// 寒冰新星 - 以玩家为中心释放环形冰爆，冻结周围敌人
/// </summary>
public class Area_FrostNova : SM_AreaSkillBase
{
    [Header("寒冰新星属性")]
    public float novaDamage = 30f;
    public float freezeDuration = 2f;
    public float freezeChance = 0.8f;
    public float novaRadius = 4f;

    [Header("目标层级")]
    public LayerMask targetLayer; // 添加目标层级定义

    [Header("视觉效果")]
    public GameObject frostEffect;

    protected override void ConfigureEffectInstance(GameObject instance, Vector2 direction, float areaRadius)
    {
        // 将效果实例放置在玩家位置
        if (character != null && character.AimOrigin != null)
        {
            instance.transform.position = character.AimOrigin.position;
        }

        var frostNova = instance.AddComponent<FrostNovaController>();
        frostNova.Initialize(novaDamage, freezeDuration, freezeChance, novaRadius, targetLayer, frostEffect);

        Debug.Log($"[寒冰新星] 在 {instance.transform.position} 释放");
    }
}

public class FrostNovaController : MonoBehaviour
{
    private float _damage;
    private float _freezeDuration;
    private float _freezeChance;
    private float _radius;
    private LayerMask _targetLayer;
    private GameObject _frostEffect;

    public void Initialize(float damage, float freezeDuration, float freezeChance,
                          float radius, LayerMask targetLayer, GameObject frostEffect)
    {
        _damage = damage;
        _freezeDuration = freezeDuration;
        _freezeChance = freezeChance;
        _radius = radius;
        _targetLayer = targetLayer;
        _frostEffect = frostEffect;

        // 立即应用效果
        ApplyFrostNova();

        // 创建视觉效果
        if (_frostEffect != null)
        {
            var effect = Instantiate(_frostEffect, transform.position, Quaternion.identity);
            effect.transform.localScale = Vector3.one * _radius * 2f;
            Destroy(effect, 2f);
        }

        // 短暂延迟后销毁
        Destroy(gameObject, 0.5f);
    }

    private void ApplyFrostNova()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, _radius, _targetLayer);
        int frozenCount = 0;

        foreach (var hit in hits)
        {
            var damageable = hit.GetComponentInParent<SM_IDamageable>();
            if (damageable != null)
            {
                // 造成伤害
                var info = new SM_DamageInfo
                {
                    Amount = _damage,
                    Element = SM_Element.Ice,
                    IgnoreDefense = false,
                    CritChance = 0.1f,
                    CritMultiplier = 1.5f
                };
                damageable.ApplyDamage(info);

                // 应用冻结效果
                if (Random.value < _freezeChance)
                {
                    var freezable = hit.GetComponentInParent<SM_IFreezable>();
                    if (freezable != null)
                    {
                        freezable.Freeze(_freezeDuration);
                        frozenCount++;
                    }
                }
            }
        }

        Debug.Log($"[寒冰新星] 影响 {hits.Length} 个目标，冻结 {frozenCount} 个敌人");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}