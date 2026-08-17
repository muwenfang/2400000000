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

    [Header("骰子图标")]
    public Image diceIconA;  // Part A 的骰子图标
    public Image diceIconB;  // Part B 的骰子图标
    [Tooltip("是否为骰子时显示图标")]
    public bool showDiceIcon = true;

    [Header("颜色配置")]
    public Color incrementalColor = Color.green;   // 递增数字：绿色
    public Color diceColor = Color.red;            // 骰子数字：红色
    public Color normalColor = Color.black;        // 普通数字：黑色
    public Color goldenColor = Color.yellow;       // 黄金数字：黄色

    public Text pointsText;

    //缓存绑定的实例，用于精准刷新UI
    public NumberCardInstance boundInstance;
    Coroutine aTextPopCoroutine;
    Coroutine bTextPopCoroutine;
    Vector3 cachedATextScale = Vector3.one;
    Vector3 cachedBTextScale = Vector3.one;
    bool hasCachedATextScale;
    bool hasCachedBTextScale;

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

        // 初始化图标组件
        if (diceIconA == null && diceIconB == null)
        {
            Image[] allImages = GetComponentsInChildren<Image>();
            if (allImages.Length > 0)
            {
                diceIconA = allImages[0];
                Debug.LogWarning($"[CompositeNumberView] diceIconA 未赋值，自动查找到第一个 Image");
            }
            if (allImages.Length > 1)
            {
                diceIconB = allImages[1];
                Debug.LogWarning($"[CompositeNumberView] diceIconB 未赋值，自动查找到第二个 Image");
            }
        }
    }

    public void Bind(NumberCardData data)
    {
        // 确保组件存在
        EnsureComponentsExist();
        CacheTextScales();

        aText.text = data.partA.value.ToString();
        bText.text = data.partB.value.ToString();

        aText.color = data.partA.isGolden ? goldenColor : normalColor;
        bText.color = data.partB != null && data.partB.isGolden ? goldenColor : normalColor;

        // 隐藏静态数据绑定时的图标
        if (diceIconA != null)
        {
            diceIconA.gameObject.SetActive(false);
        }
        if (diceIconB != null)
        {
            diceIconB.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 绑定卡牌实例（当前值 + 颜色 + 图标）- 用于商店和手牌显示
    /// </summary>
    public void BindInstance(NumberCardInstance instance, bool showPreparedDiceValue = true)
    {
        if (instance == null) return;
        this.boundInstance = instance;
        // 确保组件存在
        EnsureComponentsExist();
        CacheTextScales();

        // 设置 Part A
        SetPartDisplay(aText, diceIconA, instance.cardData.partA, instance.currentA, instance.isPrepared, showPreparedDiceValue);

        // 设置 Part B
        if (instance.cardData.partB != null)
        {
            SetPartDisplay(bText, diceIconB, instance.cardData.partB, instance.currentB, instance.isPrepared, showPreparedDiceValue);
        }
    }

    public bool PlaySettlementValuePopAnimation(float totalDuration, float peakScaleMultiplier)
    {
        if (boundInstance == null)
        {
            return false;
        }

        CacheTextScales();
        bool hasPlayedAnimation = false;

        if (ShouldAnimateSettlementValue(boundInstance.cardData.partA))
        {
            if (aTextPopCoroutine != null)
            {
                StopCoroutine(aTextPopCoroutine);
            }

            aText.rectTransform.localScale = cachedATextScale;
            aTextPopCoroutine = StartCoroutine(PlayATextPopAnimation(totalDuration, peakScaleMultiplier));
            hasPlayedAnimation = true;
        }

        if (ShouldAnimateSettlementValue(boundInstance.cardData.partB))
        {
            if (bTextPopCoroutine != null)
            {
                StopCoroutine(bTextPopCoroutine);
            }

            bText.rectTransform.localScale = cachedBTextScale;
            bTextPopCoroutine = StartCoroutine(PlayBTextPopAnimation(totalDuration, peakScaleMultiplier));
            hasPlayedAnimation = true;
        }

        return hasPlayedAnimation;
    }

    IEnumerator PlayATextPopAnimation(float totalDuration, float peakScaleMultiplier)
    {
        yield return SettlementValuePopAnimation.Play(aText.rectTransform, cachedATextScale, totalDuration, peakScaleMultiplier);
        aTextPopCoroutine = null;
    }

    IEnumerator PlayBTextPopAnimation(float totalDuration, float peakScaleMultiplier)
    {
        yield return SettlementValuePopAnimation.Play(bText.rectTransform, cachedBTextScale, totalDuration, peakScaleMultiplier);
        bTextPopCoroutine = null;
    }

    void CacheTextScales()
    {
        if (aText != null && !hasCachedATextScale)
        {
            cachedATextScale = aText.rectTransform.localScale;
            hasCachedATextScale = true;
        }

        if (bText != null && !hasCachedBTextScale)
        {
            cachedBTextScale = bText.rectTransform.localScale;
            hasCachedBTextScale = true;
        }
    }

    bool ShouldAnimateSettlementValue(NumberComponent component)
    {
        return component != null && (component.isDice || component.isIncremental);
    }

    /// <summary>
    /// 通用方法：设置单个部分的文本内容、颜色和图标
    /// </summary>
    private void SetPartDisplay(Text textComp, Image iconComp, NumberComponent component, int currentValue, bool isPrepared, bool showPreparedDiceValue)
    {
        if (textComp == null || component == null) return;

        if (component.isDice)
        {
            // 骰子显示：~面数~ (红色)
            if (!showPreparedDiceValue || !isPrepared)
            {
                // 展示卡牌信息时，或者未结算时：显示最大面数
                textComp.text = $"{component.diceSides}";
            }
            else
            {
                // 结算时：显示投出的具体数值
                textComp.text = currentValue.ToString();
            }
            textComp.color = component.isGolden ? goldenColor : diceColor;

            // 显示骰子图标
            if (showDiceIcon && DiceIconManager.Instance != null && iconComp != null)
            {
                SetDiceIcon(iconComp, component.diceSides);
            }
        }
        else if (component.isIncremental)
        {
            // 递增显示：{当前值} (绿色)
            textComp.text = $"{currentValue}";
            textComp.color = component.isGolden ? goldenColor : incrementalColor;

            // 隐藏非骰子的图标
            if (iconComp != null)
            {
                iconComp.gameObject.SetActive(false);
            }
        }
        else
        {
            // 普通显示：数值 (黑色)
            textComp.text = currentValue.ToString();
            textComp.color = component.isGolden ? goldenColor : normalColor;

            // 隐藏非骰子的图标
            if (iconComp != null)
            {
                iconComp.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 设置骰子图标
    /// </summary>
    private void SetDiceIcon(Image iconComp, int diceSides)
    {
        if (iconComp == null) return;

        if (DiceIconManager.Instance == null)
        {
            Debug.LogWarning("[CompositeNumberView] DiceIconManager 未初始化");
            iconComp.gameObject.SetActive(false);
            return;
        }

        Sprite icon = DiceIconManager.Instance.GetDiceIcon(diceSides);

        if (icon != null)
        {
            iconComp.sprite = icon;
            iconComp.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"[CompositeNumberView] 无法找到面数为 {diceSides} 的骰子图标");
            iconComp.gameObject.SetActive(false);
        }
    }
}
