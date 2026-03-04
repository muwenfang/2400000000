using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class CompositeNumberView : MonoBehaviour, NumberCardLayoutView
{
    public Text aText;
    public Text bText;
    public Text priceText;
    public bool IsInShop = false;
    //public Text operatorText; // 新增：用来显示 +、× 或 ^

    [Header("颜色配置")]
    public Color incrementalColor = Color.green;   // 递增数字：绿色
    public Color diceColor = Color.red;            // 骰子数字：红色
    public Color normalColor = Color.black;        // 普通数字：黑色

    private void OnEnable()
    {
        //在启用时检查并修复缺失的组件
        EnsureComponentsExist();
    }
    /// <summary>
    /// 确保必要的 Text 组件存在
    /// 如果 bText 为 null，尝试自动查找或创建
    /// </summary>
    public void EnsureComponentsExist()
    {
        // 如果 aText 为 null，尝试查找
        if (aText == null)
        {
            Text[] allTexts = GetComponentsInChildren<Text>();
            if (allTexts.Length > 0)
            {
                aText = allTexts[0];
                Debug.LogWarning($"[CompositeNumberView] aText 未赋值，自动查找到第一个 Text");
            }
        }
        // 如果 bText 为 null，尝试查找第二个 Text
        if (bText == null)
        {
            Text[] allTexts = GetComponentsInChildren<Text>();
            if (allTexts.Length > 1)
            {
                bText = allTexts[1];
                Debug.LogWarning($"[CompositeNumberView] bText 未赋值，自动查找到第二个 Text");
            }
            else if (allTexts.Length > 0)
            {
                Debug.LogWarning($"[CompositeNumberView] bText 未赋值，且找不到第二个 Text（仅有 {allTexts.Length} 个 Text）");
            }
            else
            {
                Debug.LogError($"[CompositeNumberView] 找不到任何 Text 组件！");
            }
        }
    }
    public void Bind(NumberCardData data)
    {
        // 确保组件存在
        EnsureComponentsExist();

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
        // 确保组件存在
        EnsureComponentsExist();

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
    public void UpdatePrice(NumberCardInstance numberCardInstance)
    {
        if (IsInShop)
        {
            int price = numberCardInstance.GetOutPutValue();
            priceText.text = price.ToString();
        }
    }

}
