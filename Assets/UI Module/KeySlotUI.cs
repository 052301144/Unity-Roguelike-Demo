using UnityEngine;
using UnityEngine.UI;

public class KeySlotUI : MonoBehaviour
{
    [SerializeField] private string keyName; // 必须设为：U/I/O/L（对应键位）
    [SerializeField] private Image skillIcon; // 关联键位上的Image组件（需赋值）
    [SerializeField] private Sprite defaultEmptySprite; // 可选：无技能时的空图标

    private void Start()
    {
        if (skillIcon == null)
        {
            Debug.LogError($"[{keyName}键位] 未绑定Image组件！");
            return;
        }
        SkillSlotManager.Instance.OnSkillBind += UpdateSkillIcon;
        UpdateSkillIcon(keyName, SkillSlotManager.Instance.GetSkillByKey(keyName));
    }

    private void UpdateSkillIcon(string key, Sprite skillSprite)
    {
        if (key != keyName || skillIcon == null) return;

        if (skillSprite != null)
        {
            skillIcon.sprite = skillSprite;
            skillIcon.enabled = true;
            skillIcon.preserveAspect = true; // 保持图标比例
        }
        else
        {
            skillIcon.sprite = defaultEmptySprite;
            skillIcon.enabled = defaultEmptySprite != null;
        }
    }

    private void OnDestroy()
    {
        if (SkillSlotManager.Instance != null)
        {
            SkillSlotManager.Instance.OnSkillBind -= UpdateSkillIcon;
        }
    }
}