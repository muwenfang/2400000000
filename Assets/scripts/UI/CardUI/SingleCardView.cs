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
    public Color goldenColor = Color.yellow;       // 黄金数字：黄色

    // 缓存绑定的实例，用于精准刷新UI
    public NumberCardInstance boundInstance;
    Coroutine valueTextPopCoroutine;
    Vector3 cachedValueTextScale = Vector3.one;
    bool hasCachedValueTextScale;
    /// <summary>
    /// 绑定静态卡牌数据（初始值）
    /// </summary>
    public void Bind(NumberCardData data)
    {
        if (valueText == null || data == null) return;

        CacheValueTextScale();
        valueText.text = data.partA.value.ToString();
        valueText.color = data.partA.isGolden ? goldenColor : normalColor;

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
        CacheValueTextScale();
        var component = instance.cardData.partA;
        int currentValue = instance.currentA;

        // 设置文本和颜色
        if (component.isDice)
        {
            // 骰子显示：~面数~ (红色)；面数优先取实例当前值（赌场专员可能已升级）
            int diceSides = instance.currentDiceSidesA > 0 ? instance.currentDiceSidesA : component.diceSides;
            if (!showPreparedDiceValue || !instance.isPrepared)
            {
                // 展示卡牌信息时，或者未结算时：显示最大面数
                valueText.text = $"{diceSides}";
            }
            else
            {
                // 结算时：显示投出的具体数值
                valueText.text = currentValue.ToString();
            }
            valueText.color = component.isGolden ? goldenColor : diceColor;

            // 显示骰子图标
            if (showDiceIcon && DiceIconManager.Instance != null)
            {
                SetDiceIcon(diceSides);
            }
        }
        else if (component.isIncremental)
        {
            // 递增显示：{当前值} (绿色)
            valueText.text = $"{currentValue}";
            valueText.color = component.isGolden ? goldenColor : incrementalColor;

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
            valueText.color = component.isGolden ? goldenColor : normalColor;

            // 隐藏非骰子的图标
            if (diceIcon != null)
            {
                diceIcon.gameObject.SetActive(false);
            }
        }
    }

    public bool PlaySettlementValuePopAnimation(float totalDuration, float peakScaleMultiplier)
    {
        if (!gameObject.activeInHierarchy)
        {
            return false;
        }

        if (valueText == null || boundInstance == null || !ShouldAnimateSettlementValue(boundInstance.cardData.partA))
        {
            return false;
        }

        CacheValueTextScale();

        if (valueTextPopCoroutine != null)
        {
            StopCoroutine(valueTextPopCoroutine);
        }

        valueText.rectTransform.localScale = cachedValueTextScale;

        valueTextPopCoroutine = StartCoroutine(PlayValueTextPopAnimation(totalDuration, peakScaleMultiplier));
        return true;
    }

    IEnumerator PlayValueTextPopAnimation(float totalDuration, float peakScaleMultiplier)
    {
        yield return SettlementValuePopAnimation.Play(valueText.rectTransform, cachedValueTextScale, totalDuration, peakScaleMultiplier);
        valueTextPopCoroutine = null;
    }

    void CacheValueTextScale()
    {
        if (valueText == null || hasCachedValueTextScale)
        {
            return;
        }

        cachedValueTextScale = valueText.rectTransform.localScale;
        hasCachedValueTextScale = true;
    }

    bool ShouldAnimateSettlementValue(NumberComponent component)
    {
        // 金融专家：黄金数字也拥有绿色数字特性，结算时同样播放递增效果动画
        return component != null && (component.isDice || component.isIncremental ||
            (component.isGolden && BlessingManager.Instance != null && BlessingManager.Instance.hasFinancialExpert));
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
        }
        else
        {
            Debug.LogWarning($"[SingleNumberView] 无法找到面数为 {diceSides} 的骰子图标");
            diceIcon.gameObject.SetActive(false);
        }
    }
}

static class SettlementValuePopAnimation
{
    /// <summary>
    /// 结算时让文本快速放大后恢复，并在剩余时间内保持正常大小。
    /// </summary>
    public static IEnumerator Play(Transform target, Vector3 baseScale, float totalDuration, float peakScaleMultiplier)
    {
        if (target == null)
        {
            yield break;
        }

        float clampedDuration = Mathf.Max(0f, totalDuration);
        float clampedScaleMultiplier = Mathf.Max(1f, peakScaleMultiplier);
        Vector3 popScale = baseScale * clampedScaleMultiplier;

        if (clampedDuration <= Mathf.Epsilon)
        {
            target.localScale = baseScale;
            yield break;
        }

        float growDuration = clampedDuration * 0.2f;
        float shrinkDuration = clampedDuration * 0.2f;
        float holdDuration = Mathf.Max(0f, clampedDuration - growDuration - shrinkDuration);

        yield return LerpScale(target, baseScale, popScale, growDuration);
        yield return LerpScale(target, popScale, baseScale, shrinkDuration);

        target.localScale = baseScale;

        if (holdDuration > 0f)
        {
            yield return new WaitForSeconds(holdDuration);
        }
    }

    static IEnumerator LerpScale(Transform target, Vector3 fromScale, Vector3 toScale, float duration)
    {
        if (target == null)
        {
            yield break;
        }

        if (duration <= Mathf.Epsilon)
        {
            target.localScale = toScale;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            target.localScale = Vector3.LerpUnclamped(fromScale, toScale, progress);
            yield return null;
        }

        target.localScale = toScale;
    }
}
