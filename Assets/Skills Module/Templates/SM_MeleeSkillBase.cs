using UnityEngine;

/// <summary>
/// Template for melee skills that sync with animations. Triggers an animator state and waits for animation events.
/// </summary>
public abstract class SM_MeleeSkillBase : SM_BaseSkill
{
    [Header("Melee Skill")]
    public Animator animator;
    public string attackTrigger = "Attack";
    public float hitboxActiveTime = 0.2f;

    protected virtual void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    protected override bool DoCast()
    {
        if (animator != null && !string.IsNullOrEmpty(attackTrigger))
        {
            animator.SetTrigger(attackTrigger);
        }
        StartCoroutine(HandleHitboxWindow());
        return true;
    }

    private System.Collections.IEnumerator HandleHitboxWindow()
    {
        OnHitboxBegin();
        yield return new WaitForSeconds(hitboxActiveTime);
        OnHitboxEnd();
    }

    /// <summary>
    /// Open hitbox / enable damage when animation reaches the strike frame.
    /// Override to connect animation events instead of timer-based window.
    /// </summary>
    protected virtual void OnHitboxBegin() { }

    /// <summary>
    /// Close hitbox / cleanup after strike.
    /// </summary>
    protected virtual void OnHitboxEnd() { }
}
