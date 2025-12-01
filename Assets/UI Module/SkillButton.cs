using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SkillButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image skillIcon; // 技能框自身的图标（需赋值）
    [SerializeField] private SkillSelectKeyPopup selectPopup; // 关联弹窗（需赋值）

    public Sprite SkillSprite => skillIcon.sprite;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (skillIcon.sprite != null)
        {
            selectPopup.OpenPopup(skillIcon.sprite, (key) =>
            {
                Debug.Log($"技能绑定到键位：{key}");
            });
        }
    }
}