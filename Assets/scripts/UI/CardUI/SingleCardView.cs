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

    [Header("骰子图标")]
    public Image diceIcon;  // 骰子图标显示
    [Tooltip("是否为骰子时显示图标")]
    public bool showDiceIcon = true;

    [Header("颜色配置")]
    public Color incrementalColor = Color.green;   // 递增数字：绿色
    public Color diceColor = Color.red;            // 骰子数字：红色
    public Color normalColor = Color.black;        // 普通数字：黑色

    public Text pointsText;

    // 缓存绑定的实例，用于精准刷新UI
    public NumberCardInstance boundInstance;
    /// <summary>
    /// 绑定静态卡牌数据（初始值）
    /// </summary>
    public void Bind(NumberCardData data)
    {
        if (valueText == null || data == null) return;

        valueText.text = data.partA.value.ToString();
        valueText.color = normalColor;

        // 隐藏静态数据绑定时的图标
        if (diceIcon != null)
        {
            diceIcon.gameObject.SetActive(false);
        }
    }
    /// <summary>
    /// 绑定卡牌实例（当前值 + 颜色）- 用于商店和手牌显示
    /// </summary>
    public void BindInstance(NumberCardInstance instance, bool showPreparedDiceValue = true)
    {
        if (valueText == null || instance == null) return;

        this.boundInstance = instance; // 缓存实例
        var component = instance.cardData.partA;
        int currentValue = instance.currentA;

        // 设置文本和颜色
        if (component.isDice)
        {
            // 骰子显示：~面数~ (红色)
            if (!showPreparedDiceValue || !instance.isPrepared)
            {
                // 展示卡牌信息时，或者未结算时：显示最大面数
                valueText.text = $"{component.diceSides}";
            }
            else
            {
                // 结算时：显示投出的具体数值
                valueText.text = currentValue.ToString();
            }
            valueText.color = diceColor;

            // 显示骰子图标
            if (showDiceIcon && DiceIconManager.Instance != null)
            {
                SetDiceIcon(component.diceSides);
            }
        }
        else if (component.isIncremental)
        {
            // 递增显示：{当前值} (绿色)
            valueText.text = $"{currentValue}";
            valueText.color = incrementalColor;

            // 隐藏非骰子的图标
            if (diceIcon != null)
            {
                diceIcon.gameObject.SetActive(false);
            }
        }
        else
        {
            // 普通显示：数值 (黑色)
            valueText.text = currentValue.ToString();
            valueText.color = normalColor;

            // 隐藏非骰子的图标
            if (diceIcon != null)
            {
                diceIcon.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 设置骰子图标
    /// </summary>
    private void SetDiceIcon(int diceSides)
    {
        if (diceIcon == null) return;

        if (DiceIconManager.Instance == null)
        {
            Debug.LogWarning("[SingleNumberView] DiceIconManager 未初始化");
            diceIcon.gameObject.SetActive(false);
            return;
        }

        Sprite icon = DiceIconManager.Instance.GetDiceIcon(diceSides);

        if (icon != null)
        {
            diceIcon.sprite = icon;
            diceIcon.gameObject.SetActive(true);
            Debug.Log($"[SingleNumberView] 设置骰子图标，面数: {diceSides}");
        }
        else
        {
            Debug.LogWarning($"[SingleNumberView] 无法找到面数为 {diceSides} 的骰子图标");
            diceIcon.gameObject.SetActive(false);
        }
    }
}
