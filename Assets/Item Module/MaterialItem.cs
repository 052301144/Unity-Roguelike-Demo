using UnityEngine;

/// <summary>
/// 强化/制作材料物品定义（继承自 ItemBase）。
/// 主要用于在掉落表或背包中配置材料数据，可在 Inspector 调整堆叠上限。
/// </summary>
[CreateAssetMenu(fileName = "Material_", menuName = "Items/Material")]
public class MaterialItem : ItemBase
{
    [Header("Material Data")]
    [Tooltip("可选的材料分类标签，便于策划和掉落表识别。")]
    [SerializeField] private string materialTag = "Material";

    public string MaterialTag => materialTag;
}
