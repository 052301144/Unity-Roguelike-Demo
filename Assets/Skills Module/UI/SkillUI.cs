using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 技能UI管理器
/// 显示MP条、技能冷却时间等
/// </summary>
public class SkillUI : MonoBehaviour
{
    [Header("MP显示")]
    public Slider mpSlider;
    public TextMeshProUGUI mpText;
    
    [Header("技能冷却显示")]
    public Image skillU_Icon;
    public Image skillI_Icon;
    public Image skillO_Icon;
    public Image skillL_Icon;
    
    public TextMeshProUGUI skillU_Cooldown;
    public TextMeshProUGUI skillI_Cooldown;
    public TextMeshProUGUI skillO_Cooldown;
    public TextMeshProUGUI skillL_Cooldown;
    
    [Header("技能系统引用")]
    public SM_SkillSystem skillSystem;
    
    private void Start()
    {
        // 订阅技能系统事件
        SM_SkillEventBus.OnMPChanged += UpdateMPDisplay;
        SM_SkillEventBus.OnSkillCast += OnSkillCast;
        
        // 如果没有指定技能系统，尝试从场景中查找
        if (skillSystem == null)
        {
            skillSystem = FindObjectOfType<SM_SkillSystem>();
        }
    }
    
    private void OnDestroy()
    {
        // 取消订阅事件
        SM_SkillEventBus.OnMPChanged -= UpdateMPDisplay;
        SM_SkillEventBus.OnSkillCast -= OnSkillCast;
    }
    
    private void Update()
    {
        if (skillSystem == null) return;
        
        // 更新技能冷却显示
        UpdateSkillCooldowns();
    }
    
    private void UpdateMPDisplay(float currentMP, float maxMP)
    {
        if (mpSlider != null)
        {
            mpSlider.value = currentMP / maxMP;
        }
        
        if (mpText != null)
        {
            mpText.text = $"{currentMP:F0}/{maxMP:F0}";
        }
    }
    
    private void UpdateSkillCooldowns()
    {
        // 更新U技能冷却
        UpdateSkillCooldown(skillSystem.slotU, skillU_Icon, skillU_Cooldown);
        
        // 更新I技能冷却
        UpdateSkillCooldown(skillSystem.slotI, skillI_Icon, skillI_Cooldown);
        
        // 更新O技能冷却
        UpdateSkillCooldown(skillSystem.slotO, skillO_Icon, skillO_Cooldown);
        
        // 更新L技能冷却
        UpdateSkillCooldown(skillSystem.slotL, skillL_Icon, skillL_Cooldown);
    }
    
    private void UpdateSkillCooldown(SM_BaseSkill skill, Image icon, TextMeshProUGUI cooldownText)
    {
        if (skill == null || icon == null) return;
        
        if (skill.IsOnCooldown)
        {
            // 技能冷却中
            if (cooldownText != null)
            {
                cooldownText.text = skill.Cooldown.ToString("F1");
                cooldownText.gameObject.SetActive(true);
            }
            
            // 图标变暗
            icon.color = Color.gray;
        }
        else
        {
            // 技能可用
            if (cooldownText != null)
            {
                cooldownText.gameObject.SetActive(false);
            }
            
            // 图标正常颜色
            icon.color = Color.white;
        }
    }
    
    private void OnSkillCast(string skillName)
    {
        Debug.Log($"[技能UI] 技能 {skillName} 施放成功！");
        
        // 可以在这里添加技能施放的特效
        // 例如：屏幕震动、粒子效果等
    }
    
    // ========== 公共方法 ==========
    public void SetSkillSystem(SM_SkillSystem system)
    {
        skillSystem = system;
    }
    
    public void ShowSkillInfo(string skillName, float cooldown, float manaCost)
    {
        // 显示技能信息的工具提示
        Debug.Log($"[技能信息] {skillName} - 冷却: {cooldown}s, 消耗: {manaCost} MP");
    }
}
