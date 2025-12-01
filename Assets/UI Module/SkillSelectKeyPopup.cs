using UnityEngine;
using UnityEngine.UI;

public class SkillSelectKeyPopup : MonoBehaviour
{
    [SerializeField] private Button uKeyBtn;
    [SerializeField] private Button iKeyBtn;
    [SerializeField] private Button oKeyBtn;
    [SerializeField] private Button lKeyBtn;

    private Sprite currentSkillSprite;
    private System.Action<string> onKeySelected;

    private void Awake()
    {
        uKeyBtn.onClick.AddListener(() => OnKeyBtnClicked("U"));
        iKeyBtn.onClick.AddListener(() => OnKeyBtnClicked("I"));
        oKeyBtn.onClick.AddListener(() => OnKeyBtnClicked("O"));
        lKeyBtn.onClick.AddListener(() => OnKeyBtnClicked("L"));
        gameObject.SetActive(false);
    }

    public void OpenPopup(Sprite skillSprite, System.Action<string> callback)
    {
        currentSkillSprite = skillSprite;
        onKeySelected = callback;
        gameObject.SetActive(true);
    }

    private void OnKeyBtnClicked(string key)
    {
        onKeySelected?.Invoke(key);
        SkillSlotManager.Instance.BindSkillToKey(key, currentSkillSprite);
        gameObject.SetActive(false);
    }
}