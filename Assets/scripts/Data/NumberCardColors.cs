using UnityEngine;

/// <summary>
/// 数字卡颜色的唯一入口：颜色数据 + 类型判定都只在这里。
/// <para>约定：任何需要给数字卡文本上色的地方（手牌、商店、卡牌面板、公式区…）
/// 一律调用 <see cref="GetNumberTextColor"/> / <see cref="GetStaticNumberTextColor"/>，
/// 不得再在视图/控制器里硬编码 Color，也不得在 prefab 上保留颜色字段。</para>
/// <para>配色数据来自 <see cref="NumberCardColorPalette"/>：Assets/Resources/NumberCardColorPalette.asset。</para>
/// <para>优先级固定为：黄金数字 &gt; 骰子 &gt; 递增（绿色数字）&gt; 普通。</para>
/// </summary>
public static class NumberCardColors
{
    /// <summary> 配色资产在 Resources 下的路径（不含扩展名） </summary>
    public const string ResourcePath = "NumberCardColorPalette";

    // 兜底默认色：仅当 Resources 下找不到配色资产时使用，与历史默认值保持一致
    static readonly Color DefaultNormal = Color.black;
    static readonly Color DefaultGolden = Color.yellow;
    static readonly Color DefaultIncremental = Color.green;
    static readonly Color DefaultDice = Color.red;

    static NumberCardColorPalette palette;
    static bool loadAttempted;

    /// <summary> 当前生效的配色资产（可能为 null，此时走内置默认色） </summary>
    public static NumberCardColorPalette Palette
    {
        get
        {
            if (palette == null && !loadAttempted)
            {
                loadAttempted = true;
                palette = Resources.Load<NumberCardColorPalette>(ResourcePath);

                if (palette == null)
                {
                    Debug.LogWarning($"[NumberCardColors] 未找到 Resources/{ResourcePath}，已回退到内置默认颜色。" +
                                     "需要调整颜色时，请在 Assets/Resources 下执行 Create → 24亿 → 数字卡颜色配置 生成该资产。");
                }
            }

            return palette;
        }
    }

    /// <summary>
    /// 手动指定配色（运行时替换 / 单元测试注入用）
    /// </summary>
    public static void SetPalette(NumberCardColorPalette newPalette)
    {
        palette = newPalette;
        loadAttempted = true;
    }

    public static Color NormalColor => Palette != null ? Palette.normalColor : DefaultNormal;
    public static Color GoldenColor => Palette != null ? Palette.goldenColor : DefaultGolden;
    public static Color IncrementalColor => Palette != null ? Palette.incrementalColor : DefaultIncremental;
    public static Color DiceColor => Palette != null ? Palette.diceColor : DefaultDice;

    /// <summary>
    /// 统一判定：给定数字组件（卡牌实例的 partA / partB）应显示的文本颜色。
    /// 优先级：黄金数字 &gt; 骰子 &gt; 递增（绿色数字）&gt; 普通。
    /// </summary>
    public static Color GetNumberTextColor(NumberComponent component)
    {
        if (component == null) return NormalColor;
        if (component.isGolden) return GoldenColor;
        if (component.isDice) return DiceColor;
        if (component.isIncremental) return IncrementalColor;
        return NormalColor;
    }

    /// <summary>
    /// 静态卡牌数据（未绑定实例的展示态，如商店预览）只区分黄金/普通，沿用原有显示口径。
    /// </summary>
    public static Color GetStaticNumberTextColor(NumberComponent component)
    {
        if (component == null) return NormalColor;
        return component.isGolden ? GoldenColor : NormalColor;
    }
}
