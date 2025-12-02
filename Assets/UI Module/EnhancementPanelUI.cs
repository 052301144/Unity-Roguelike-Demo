using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 简单的强化面板：针对当前装备的武器或指定装备槽，消耗材料提升等级。
/// </summary>
public class EnhancementPanelUI : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private PlayerItemManager playerItemManager;
    [SerializeField] private bool targetWeapon = true;
    [SerializeField] private EquipmentSlot targetEquipmentSlot = EquipmentSlot.Armor;

    [Header("UI")]
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text currentStatText;
    [SerializeField] private TMP_Text nextStatText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button upgradeButton;

    private ItemData currentItem;

    private void OnEnable()
    {
        if (playerItemManager == null)
            playerItemManager = FindObjectOfType<PlayerItemManager>();

        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(OnUpgrade);

        Refresh();
    }

    private void OnDisable()
    {
        if (upgradeButton != null)
            upgradeButton.onClick.RemoveListener(OnUpgrade);
    }

    private void Refresh()
    {
        currentItem = targetWeapon
            ? playerItemManager?.CurrentWeapon
            : playerItemManager?.GetEquippedItem(targetEquipmentSlot);

        if (currentItem == null || currentItem.itemAsset == null)
        {
            SetTexts("None", "-", "-", "-", "No item equipped", false);
            return;
        }

        int level = Mathf.Max(0, currentItem.enhancementLevel);
        int nextLevel = level + 1;
        StringBuilder sbCost = new StringBuilder();
        bool canUpgrade = false;

        if (currentItem.itemAsset is WeaponItem weapon)
        {
            PopulateStatsWeapon(weapon, level, nextLevel, ref canUpgrade, sbCost);
        }
        else if (currentItem.itemAsset is EquipmentItem equip)
        {
            PopulateStatsEquip(equip, level, nextLevel, ref canUpgrade, sbCost);
        }
        else
        {
            SetTexts(currentItem.itemName, $"Level {level}", "-", "-", "Unsupported item", false);
            return;
        }

        costText.SafeSetText(sbCost.ToString());
        if (upgradeButton != null) upgradeButton.interactable = canUpgrade;
        resultText.SafeSetText(string.Empty);
    }

    private void PopulateStatsWeapon(WeaponItem weapon, int level, int nextLevel, ref bool canUpgrade, StringBuilder sbCost)
    {
        string name = weapon.ItemName;
        int currentAtk = weapon.GetFinalAttack(level);
        int nextAtk = weapon.GetFinalAttack(nextLevel);

        bool atMax = level >= weapon.MaxEnhancementLevel;
        SetTexts(name, $"Level {level}/{weapon.MaxEnhancementLevel}",
            $"Attack: {currentAtk}",
            atMax ? "Max level" : $"Next Attack: {nextAtk}",
            string.Empty,
            !atMax);

        if (atMax) { if (upgradeButton != null) upgradeButton.interactable = false; return; }

        var costs = weapon.GetCostForLevel(nextLevel);
        canUpgrade = BuildCostString(costs, sbCost);
    }

    private void PopulateStatsEquip(EquipmentItem equip, int level, int nextLevel, ref bool canUpgrade, StringBuilder sbCost)
    {
        string name = equip.ItemName;
        int curDef = equip.GetFinalDefense(level);
        int nextDef = equip.GetFinalDefense(nextLevel);
        int curHp = equip.GetFinalMaxHealthBonus(level);
        int nextHp = equip.GetFinalMaxHealthBonus(nextLevel);

        bool atMax = level >= equip.MaxEnhancementLevel;
        SetTexts(name, $"Level {level}/{equip.MaxEnhancementLevel}",
            $"Defense: {curDef}  HP+: {curHp}",
            atMax ? "Max level" : $"Next Defense: {nextDef}  HP+: {nextHp}",
            string.Empty,
            !atMax);

        if (atMax) { if (upgradeButton != null) upgradeButton.interactable = false; return; }

        var costs = equip.GetCostForLevel(nextLevel);
        canUpgrade = BuildCostString(costs, sbCost);
    }

    private bool BuildCostString(System.Collections.Generic.IReadOnlyList<EnhancementMaterialCost> costs, StringBuilder sb)
    {
        if (costs == null || costs.Count == 0)
        {
            sb.Append("No cost");
            return true;
        }

        bool enough = true;
        sb.Length = 0;
        for (int i = 0; i < costs.Count; i++)
        {
            var c = costs[i];
            int have = playerItemManager != null ? playerItemManager.GetItemCount(c.itemId) : 0;
            sb.Append($"{c.itemId}: {have}/{c.amount}");
            if (have < c.amount) enough = false;
            if (i < costs.Count - 1) sb.Append(" | ");
        }
        return enough;
    }

    private void OnUpgrade()
    {
        if (currentItem == null || currentItem.itemAsset == null || playerItemManager == null) return;

        int level = Mathf.Max(0, currentItem.enhancementLevel);
        int nextLevel = level + 1;

        if (currentItem.itemAsset is WeaponItem weapon)
        {
            if (nextLevel > weapon.MaxEnhancementLevel) { resultText.SafeSetText("Already max"); return; }
            var costs = weapon.GetCostForLevel(nextLevel);
            if (!ConsumeMaterials(costs)) { resultText.SafeSetText("Materials not enough"); return; }
            currentItem.enhancementLevel = nextLevel;
            playerItemManager.ReapplyWeaponAttack();
            resultText.SafeSetText($"Weapon upgraded to Lv{nextLevel}");
        }
        else if (currentItem.itemAsset is EquipmentItem equip)
        {
            if (nextLevel > equip.MaxEnhancementLevel) { resultText.SafeSetText("Already max"); return; }
            var costs = equip.GetCostForLevel(nextLevel);
            if (!ConsumeMaterials(costs)) { resultText.SafeSetText("Materials not enough"); return; }
            currentItem.enhancementLevel = nextLevel;
            playerItemManager.RecalculateEquippedStats();
            resultText.SafeSetText($"Equipment upgraded to Lv{nextLevel}");
        }

        Refresh();
    }

    private bool ConsumeMaterials(System.Collections.Generic.IReadOnlyList<EnhancementMaterialCost> costs)
    {
        if (costs == null) return true;
        // 先检查
        foreach (var c in costs)
        {
            if (string.IsNullOrEmpty(c.itemId) || c.amount <= 0) continue;
            int have = playerItemManager.GetItemCount(c.itemId);
            if (have < c.amount) return false;
        }
        // 再扣除
        foreach (var c in costs)
        {
            if (string.IsNullOrEmpty(c.itemId) || c.amount <= 0) continue;
            playerItemManager.RemoveItem(c.itemId, c.amount);
        }
        return true;
    }

    private void SetTexts(string name, string level, string cur, string next, string cost, bool buttonInteractable)
    {
        itemNameText.SafeSetText(name);
        levelText.SafeSetText(level);
        currentStatText.SafeSetText(cur);
        nextStatText.SafeSetText(next);
        costText.SafeSetText(cost);
        if (upgradeButton != null) upgradeButton.interactable = buttonInteractable;
    }
}

public static class TextExtensions
{
    public static void SafeSetText(this TMP_Text tmp, string content)
    {
        if (tmp != null) tmp.text = content;
    }

    public static void SafeSetText(this Text text, string content)
    {
        if (text != null) text.text = content;
    }
}
