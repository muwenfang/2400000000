using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public class SingleNumberView : MonoBehaviour, NumberCardLayoutView
{
    public Text valueText;
    public Text priceText;
    public bool IsInShop = false;

    [Header("颜色配置")]
    public Color incrementalColor = Color.green;   // 递增数字：绿色
    public Color diceColor = Color.red;            // 骰子数字：红色
    public Color normalColor = Color.black;        // 普通数字：黑色

    /// <summary>
    /// 绑定静态卡牌数据（初始值）
    /// </summary>
    public void Bind(NumberCardData data)
    {
        if (valueText == null || data == null) return;

        valueText.text = data.partA.value.ToString();
        valueText.color = normalColor;
    }
    /// <summary>
    /// 绑定卡牌实例（当前值 + 颜色）- 用于商店和手牌显示
    /// </summary>
    public void BindInstance(NumberCardInstance instance)
    {
        if (valueText == null || instance == null) return;

        var component = instance.cardData.partA;
        int currentValue = instance.currentA;

        // 设置文本和颜色
        if (component.isDice)
        {
            // 骰子显示：~面数~ (红色)
            valueText.text = $"~{component.diceSides}~";
            valueText.color = diceColor;
        }
        else if (component.isIncremental)
        {
            // 递增显示：{当前值} (绿色)
            valueText.text = $"{{{currentValue}}}";
            valueText.color = incrementalColor;
        }
        else
        {
            // 普通显示：数值 (黑色)
            valueText.text = currentValue.ToString();
            valueText.color = normalColor;
        }
    }
    //更新价格显示
    public void UpdatePrice(NumberCardInstance numberCardInstance)
    {
        if (IsInShop)
        {
            long price  = numberCardInstance.GetOutPutValue();
            priceText.text = price.ToString();
        }
    }


}
