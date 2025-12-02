using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 实时显示角色的生命、攻击、防御。
/// </summary>
public class CharacterStatsDisplayUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Attribute playerAttribute;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text attackText;
    [SerializeField] private TMP_Text defenseText;

    [Header("Fallback (可选，不用则留空)")]
    [SerializeField] private Text hpTextLegacy;
    [SerializeField] private Text attackTextLegacy;
    [SerializeField] private Text defenseTextLegacy;

    private void OnEnable()
    {
        if (playerAttribute == null)
        {
            playerAttribute = FindObjectOfType<Attribute>();
        }

        Subscribe(true);
        RefreshAll();
    }

    private void OnDisable()
    {
        Subscribe(false);
    }

    private void Subscribe(bool subscribe)
    {
        if (playerAttribute == null) return;

        if (subscribe)
        {
            playerAttribute.OnHealthChanged += OnHealthChanged;
            playerAttribute.OnAttackChanged += OnAttackChanged;
            playerAttribute.OnDefenseChanged += OnDefenseChanged;
        }
        else
        {
            playerAttribute.OnHealthChanged -= OnHealthChanged;
            playerAttribute.OnAttackChanged -= OnAttackChanged;
            playerAttribute.OnDefenseChanged -= OnDefenseChanged;
        }
    }

    private void OnHealthChanged(int value) => RefreshHealth();
    private void OnAttackChanged(int value) => RefreshAttack();
    private void OnDefenseChanged(int value) => RefreshDefense();

    private void RefreshAll()
    {
        RefreshHealth();
        RefreshAttack();
        RefreshDefense();
    }

    private void RefreshHealth()
    {
        int hp = playerAttribute != null ? playerAttribute.CurrentHealth : 0;
        SetText(hpText, hpTextLegacy, $"HP: {hp}");
    }

    private void RefreshAttack()
    {
        int atk = playerAttribute != null ? playerAttribute.Attack : 0;
        SetText(attackText, attackTextLegacy, $"Attack: {atk}");
    }

    private void RefreshDefense()
    {
        int def = playerAttribute != null ? playerAttribute.Defense : 0;
        SetText(defenseText, defenseTextLegacy, $"Defense: {def}");
    }

    private void SetText(TMP_Text tmp, Text legacy, string content)
    {
        if (tmp != null) tmp.text = content;
        if (legacy != null) legacy.text = content;
    }
}
