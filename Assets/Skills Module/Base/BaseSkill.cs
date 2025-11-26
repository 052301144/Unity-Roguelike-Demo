using UnityEngine;

/// <summary>
/// Base class for all skills: handles cooldown, mana, and event broadcast.
/// </summary>
public abstract class SM_BaseSkill : MonoBehaviour, SM_ISkill
{
    [Header("Skill Base Properties")]
    public SM_SkillData skillData;                           // Optional data asset that overrides inline fields
    public string skillName = "Skill";                       // Display name
    public SM_Element element = SM_Element.Physical;         // Element type
    public float manaCost = 10f;                             // MP cost
    public float cooldown = 2f;                              // Cooldown duration

    protected float cdTimer = 0f;                            // Internal cooldown timer
    protected SM_ICharacterProvider character;               // Provided by skill system

    // Effective values fall back to inline fields if no data asset is provided
    public string SkillName => skillData != null ? skillData.skillName : skillName;
    public SM_Element Element => skillData != null ? skillData.element : element;
    public float ManaCost => skillData != null ? skillData.manaCost : manaCost;
    public float Cooldown => skillData != null ? skillData.cooldown : cooldown;
    public bool IsOnCooldown => cdTimer > 0f;

    public void Initialize(SM_ICharacterProvider provider)   // Inject character context
    {
        character = provider;
    }

    protected abstract bool DoCast();                        // Child implements actual logic

    public virtual bool TryCast()                            // Entry point to cast a skill
    {
        if (IsOnCooldown)                                    // Cooldown gate
        {
            Debug.LogWarning($"[SM_BaseSkill] {SkillName} is on cooldown, {cdTimer:F2}s left.");
            return false;
        }
        if (character == null)                               // Safety gate
        {
            Debug.LogWarning($"[SM_BaseSkill] {SkillName} has no character provider.");
            return false;
        }
        if (!character.ConsumeMP(ManaCost))                  // Mana gate
        {
            Debug.LogWarning($"[SM_BaseSkill] {SkillName} not enough MP. Need {ManaCost}, current {character.CurrentMP}.");
            return false;
        }

        var ok = DoCast();                                   // Execute subclass logic
        if (ok)                                              // On success
        {
            cdTimer = Cooldown;                              // Start cooldown
            SM_SkillEventBus.RaiseSkillCast(SkillName);      // Broadcast cast event
        }
        else
        {
            Debug.LogWarning($"[SM_BaseSkill] {SkillName} DoCast() returned false.");
        }
        return ok;
    }

    public virtual void Tick(float dt)                       // Per-frame update
    {
        if (cdTimer > 0f) cdTimer -= dt;                     // Reduce cooldown
    }
}
