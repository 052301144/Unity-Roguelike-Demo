using UnityEngine;
using System.Collections.Generic;

public class SkillSlotManager : MonoBehaviour
{
    public static SkillSlotManager Instance;
    private Dictionary<string, Sprite> keySkillMap = new Dictionary<string, Sprite>
    {
        { "U", null }, { "I", null }, { "O", null }, { "L", null }
    };

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void BindSkillToKey(string key, Sprite skillSprite)
    {
        if (keySkillMap.ContainsKey(key))
        {
            keySkillMap[key] = skillSprite;
            OnSkillBind?.Invoke(key, skillSprite);
        }
    }

    public Sprite GetSkillByKey(string key)
    {
        return keySkillMap.TryGetValue(key, out var sprite) ? sprite : null;
    }

    public System.Action<string, Sprite> OnSkillBind;
}