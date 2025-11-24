using System.Collections;
using UnityEngine;

/// <summary>
/// Template for movement/displacement skills (dash/blink). Override OnDashApplied for custom effects.
/// </summary>
public abstract class SM_MovementSkillBase : SM_BaseSkill
{
    [Header("Movement Skill")]
    public float dashDistance = 5f;
    public float dashDuration = 0.15f;
    public bool ignoreCollisionDuringDash = false;
    public bool invulnerableDuringDash = false;

    protected Rigidbody2D rbCache;

    protected virtual void Awake()
    {
        rbCache = GetComponent<Rigidbody2D>();
    }

    protected override bool DoCast()
    {
        if (rbCache == null)
        {
            Debug.LogWarning($"[SM_MovementSkillBase] {SkillName} missing Rigidbody2D, cannot dash.");
            return false;
        }

        StartCoroutine(DashRoutine());
        return true;
    }

    private IEnumerator DashRoutine()
    {
        Vector2 start = rbCache.position;
        Vector2 dir = character != null ? character.AimDirection.normalized : Vector2.right;
        Vector2 target = start + dir * dashDistance;

        float elapsed = 0f;
        var originalGravity = rbCache.gravityScale;
        rbCache.gravityScale = 0f;

        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dashDuration);
            Vector2 pos = Vector2.Lerp(start, target, t);
            rbCache.MovePosition(pos);
            yield return null;
        }

        rbCache.gravityScale = originalGravity;
        OnDashApplied(start, target);
    }

    /// <summary>
    /// Hook for VFX/SFX/camera shake after dash completes.
    /// </summary>
    protected virtual void OnDashApplied(Vector2 start, Vector2 end) { }
}
