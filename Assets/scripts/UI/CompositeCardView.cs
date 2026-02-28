using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class CompositeNumberView : MonoBehaviour, NumberCardLayoutView
{
    public Text aText;
    public Text bText;
    //public Text operatorText; // 新增：用来显示 +、× 或 ^

    [Header("颜色配置")]
    public Color incrementalColor = Color.green;   // 递增数字：绿色
    public Color diceColor = Color.red;            // 骰子数字：红色
    public Color normalColor = Color.black;        // 普通数字：黑色


    public void Bind(NumberCardData data)
    {
        aText.text = data.partA.value.ToString();
        bText.text = data.partB.value.ToString();

        aText.color = normalColor;
        bText.color = normalColor;

    }
    /// <summary>
    /// 绑定卡牌实例（当前值 + 颜色）- 用于商店和手牌显示
    /// </summary>
    public void BindInstance(NumberCardInstance instance)
    {
        if (instance == null) return;

        // 设置 Part A
        SetPartDisplay(aText, instance.cardData.partA, instance.currentA);

        // 设置 Part B
        if (instance.cardData.partB != null)
        {
            SetPartDisplay(bText, instance.cardData.partB, instance.currentB);
        }
    }

    /// <summary>
    /// 通用方法：设置单个部分的文本内容和颜色
    /// </summary>
    private void SetPartDisplay(Text textComp, NumberComponent component, int currentValue)
    {
        if (textComp == null || component == null) return;

        if (component.isDice)
        {
            // 骰子显示：~面数~ (红色)
            textComp.text = $"~{component.diceSides}~";
            textComp.color = diceColor;
        }
        else if (component.isIncremental)
        {
            // 递增显示：{当前值} (绿色)
            textComp.text = $"{{{currentValue}}}";
            textComp.color = incrementalColor;
        }
        else
        {
            // 普通显示：数值 (黑色)
            textComp.text = currentValue.ToString();
            textComp.color = normalColor;
        }
    }

}
