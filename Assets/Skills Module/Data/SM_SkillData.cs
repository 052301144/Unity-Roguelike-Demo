using UnityEngine;

/// <summary>
/// ScriptableObject for configuring shared skill parameters.
/// Assign to SM_BaseSkill.skillData to override inline fields.
/// </summary>
[CreateAssetMenu(menuName = "Skills/Skill Data", fileName = "SkillData")]
public class SM_SkillData : ScriptableObject
{
    public string skillName = "Skill";
    public SM_Element element = SM_Element.Physical;
    public float manaCost = 10f;
    public float cooldown = 2f;
    [TextArea]
    public string description;
    public Sprite icon;
}
