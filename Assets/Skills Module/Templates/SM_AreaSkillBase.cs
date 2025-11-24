using UnityEngine;

/// <summary>
/// Template for area or projectile-based skills. Spawns a prefab at aim direction.
/// </summary>
public abstract class SM_AreaSkillBase : SM_BaseSkill
{
    [Header("Area/Projectile Skill")]
    public GameObject effectPrefab;
    public float spawnDistance = 1.5f;
    public float radius = 2f;

    protected override bool DoCast()
    {
        if (effectPrefab == null)
        {
            Debug.LogWarning($"[SM_AreaSkillBase] {SkillName} missing effect prefab.");
            return false;
        }

        Vector2 origin = character != null ? (Vector2)character.AimOrigin.position : (Vector2)transform.position;
        Vector2 dir = character != null ? character.AimDirection.normalized : Vector2.right;
        Vector2 spawnPos = origin + dir * spawnDistance;
        var instance = Object.Instantiate(effectPrefab, spawnPos, Quaternion.identity);
        ConfigureEffectInstance(instance, dir, radius);
        return true;
    }

    /// <summary>
    /// Child classes can initialize damage, lifetime, and collision data on the spawned effect.
    /// </summary>
    protected abstract void ConfigureEffectInstance(GameObject instance, Vector2 direction, float areaRadius);
}
