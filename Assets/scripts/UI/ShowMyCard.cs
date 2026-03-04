using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 显示我的卡牌 - 最终修复版
/// </summary>
public class ShowMyCard : MonoBehaviour
{
    [Header("类型设置")]
    public bool showNumberCards = true;
    public bool showFormulaCards = false;

    [Header("容器引用")]
    public Transform contentRoot;

    [Header("显示设置")]
    public float cardScale = 1.0f;

    [Header("颜色配置")]
    public Color incrementalColor = Color.green;   // 递增数字：绿色
    public Color diceColor = Color.red; // 骰子数字：红色
    public Color normalColor = Color.black;        // 普通数字：黑色

    private void OnEnable()
    {
        RefreshAllCards();
    }

    public void RefreshAllCards()
    {
        // 1. 清理旧卡牌
        foreach (Transform child in contentRoot)
        {
            Destroy(child.gameObject);
        }

        // 2. 根据类型生成
        if (showNumberCards)
        {
            GenerateNumberCards();
        }
        if (showFormulaCards)
        {
            GenerateFormulaCards();
        }
    }

    void GenerateNumberCards()
    {
        // 获取玩家库存中的所有实例（包含 currentA, currentB 的实时数据）
        var instances = PlayerCardInventory.Instance.GetAllNumberCards();

        if (instances == null) return;

        foreach (var instance in instances)
        {
            if (instance == null || instance.cardData == null) continue;

            //  获取 Prefab
            GameObject prefab = UIManager.Instance.numberCardLibrary.GetPrefab(instance.cardData.layoutType);
            if (prefab == null) continue;

            // 实例化
            GameObject go = Instantiate(prefab, contentRoot);

            go.transform.localScale = Vector3.one * cardScale;
            go.SetActive(true); // 确保显示

            // 判断是哪种视图组件，分别赋值
            // 尝试获取单数字视图
            var singleView = go.GetComponent<SingleNumberView>();
            if (singleView != null)
            {
                // 设置数值 (使用实例中的 currentA)
                SetPartDisplay(singleView.valueText, instance.cardData.partA, instance.currentA);
                // 可以在这里设置价格文字隐藏或显示
                if (singleView.priceText != null) singleView.priceText.gameObject.SetActive(false);
            }
            // 尝试获取组合视图 (加法/乘法/乘方)
            else
            {
                var compositeView = go.GetComponent<CompositeNumberView>();
                if (compositeView != null)
                {
                    // 设置 Part A (使用 currentA)
                    SetPartDisplay(compositeView.aText, instance.cardData.partA, instance.currentA);

                    // 设置 Part B (使用 currentB)
                    SetPartDisplay(compositeView.bText, instance.cardData.partB, instance.currentB);
                }
            }
        }
    }
    /// <summary>
    ///初始化 RectTransform - 解决红❌问题
    /// </summary>
    void InitializeRectTransform(GameObject cardGo)
    {
        RectTransform rectTransform = cardGo.GetComponent<RectTransform>();
        if (rectTransform == null) return;

        // 设置锚点为铺满父容器（如果使用 LayoutGroup）
        // 或根据需要设置具体的大小
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;

        // 如果父容器使用 LayoutGroup，可以设置 sizeDelta
        // 如果不使用，则铺满父容器
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        // 设置本地变换
        rectTransform.localPosition = Vector3.zero;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localScale = Vector3.one * cardScale;
    }
    /// <summary>
    /// 通用方法：设置文本内容和颜色
    /// </summary>
    void SetPartDisplay(Text textComp, NumberComponent component, int currentValue)
    {
        if (textComp == null || component == null) return;

        if (component.isDice)
        {
            // 骰子显示：~面数~ (黄色)
            textComp.text = $"~{component.diceSides}~";
            textComp.color = diceColor;
        }
        else if (component.isIncremental)
        {
            // 递增显示：{当前值} (绿色) - 这里使用了实例里的 currentValue
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

    void GenerateFormulaCards()
    {
        var deck = CardManager.Instance.formulaCardDeck;
        var prefab = UIManager.Instance.formulaCardPrefab;

        if (deck == null || prefab == null) return;

        foreach (var data in deck)
        {
            GameObject go = Instantiate(prefab, contentRoot);
            go.transform.localScale = Vector3.one * cardScale;

            go.SetActive(true);

            var view = go.GetComponent<FormulaCardUI>();
            if (view != null) view.Bind(data);
        }
    }
}