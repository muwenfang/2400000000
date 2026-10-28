using UnityEngine;

/// <summary>
/// 数字卡文本配色（全工程唯一取色来源）。
/// 资源位置约定：Assets/Resources/NumberCardColorPalette.asset —— 运行时自动加载，无需在 Inspector 拖引用。
/// 只改这一个资产，手牌、商店、卡牌面板、公式区的数字颜色会同步变化。
/// 取色逻辑见 <see cref="NumberCardColors"/>。
/// </summary>
[CreateAssetMenu(fileName = "NumberCardColorPalette", menuName = "24亿/数字卡颜色配置")]
public class NumberCardColorPalette : ScriptableObject
{
    [Header("数字卡文本颜色")]
    [Tooltip("普通数字")]
    public Color normalColor = Color.black;

    [Tooltip("黄金数字（优先级最高，覆盖骰子/递增/普通）")]
    public Color goldenColor = Color.yellow;

    [Tooltip("递增数字（绿色数字）")]
    public Color incrementalColor = Color.green;

    [Tooltip("骰子数字")]
    public Color diceColor = Color.red;
}
