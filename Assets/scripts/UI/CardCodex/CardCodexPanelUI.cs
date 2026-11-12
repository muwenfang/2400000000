using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 图鉴面板：按固定顺序展示全部数字卡 / 公式卡 / 祝福，未解锁的条目显示上锁图标与"未解锁"。
///
/// 展示顺序（由 CardCodexManager 统一给出）：
///   数字卡（按资产编号 002→142）→ 公式卡（按 FormulaCardId）→
///   祝福（总是刷新 → 本回合限购 → 永不刷新，组内按 blessingId）。
///
/// UI 搭建约定（面板本身不生成任何布局，全部在 Inspector 里拖）：
///   · contentRoot       —— 卡片列表容器（Grid/Vertical Layout Group 由你配置）
///   · 三个条目预制体     —— 每个预制体根节点挂 CardCodexEntryUI，内含"卡片挂点 + 上锁 Image + 未解锁 Text"
///   · groupHeaderPrefab —— 可选，分组标题（根节点挂 CardCodexGroupHeaderUI）
///   · progressText      —— 可选，"已解锁 x / y"
///   · scrollRect        —— 可选，留空会自动往上找
///
/// 卡面本身复用现有卡牌预制体（数字卡按 layoutType 取、公式卡与祝福用 UIManager 上的预制体），
/// 因此图鉴里的卡面与商店/卡组保持一致。
/// </summary>
public class CardCodexPanelUI : MonoBehaviour
{
    [Header("列表容器")]
    [Tooltip("图鉴卡片的父节点（由你在 Inspector 配置布局组）")]
    public Transform contentRoot;

    [Tooltip("滚动区域；留空则自动向上查找 ScrollRect")]
    public ScrollRect scrollRect;

    [Header("条目预制体（根节点挂 CardCodexEntryUI）")]
    public GameObject numberCardEntryPrefab;
    public GameObject formulaCardEntryPrefab;
    public GameObject blessingEntryPrefab;

    [Header("分组标题预制体（可选，根节点挂 CardCodexGroupHeaderUI）")]
    public GameObject groupHeaderPrefab;

    [Header("进度文本（可选）")]
    public Text progressText;

    [Header("选项")]
    [Tooltip("面板每次被打开时自动重建列表")]
    public bool rebuildOnEnable = true;

    [Tooltip("在分组标题后附带解锁进度，如「数字卡 3/140」")]
    public bool showProgressInHeader = false;

    private Transform ResolvedContentRoot
    {
        get { return contentRoot != null ? contentRoot : transform; }
    }

    private void OnEnable()
    {
        ResolveScrollRect();

        if (rebuildOnEnable)
        {
            Rebuild();
        }
    }

    /// <summary>打开面板（已打开时原地刷新，保证读到最新解锁状态）</summary>
    public void Open()
    {
        if (gameObject.activeSelf)
        {
            Rebuild();
            return;
        }

        // 激活会触发 OnEnable 里的重建（rebuildOnEnable 关闭时这里补一次）
        gameObject.SetActive(true);
        if (!rebuildOnEnable) Rebuild();
    }

    /// <summary>关闭面板</summary>
    public void Close()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 重建整个图鉴列表。
    /// 顺序：数字卡 → 公式卡 → 祝福（三组）。
    /// </summary>
    public void Rebuild()
    {
        Transform root = ResolvedContentRoot;
        ClearContent(root);

        int unlocked = 0;
        int total = 0;

        // ---------- 1. 数字卡 ----------
        List<NumberCardData> numberCards = CardCodexManager.GetOrderedNumberCards();
        int numberUnlocked = CountUnlockedNumberCards(numberCards);
        AddHeader("数字卡", numberUnlocked, numberCards.Count);

        for (int i = 0; i < numberCards.Count; i++)
        {
            NumberCardData card = numberCards[i];
            if (card == null) continue;

            bool isUnlocked = CardCodexManager.IsNumberCardUnlocked(card);
            CardCodexEntryUI entry = CreateEntry(numberCardEntryPrefab, isUnlocked);
            if (entry == null) continue;

            if (isUnlocked || entry.showCardWhenLocked)
            {
                FillNumberCard(entry, card);
            }
        }

        unlocked += numberUnlocked;
        total += numberCards.Count;

        // ---------- 2. 公式卡 ----------
        List<FormulaCardData> formulaCards = CardCodexManager.GetOrderedFormulaCards();
        int formulaUnlocked = CountUnlockedFormulaCards(formulaCards);
        AddHeader("公式卡", formulaUnlocked, formulaCards.Count);

        for (int i = 0; i < formulaCards.Count; i++)
        {
            FormulaCardData card = formulaCards[i];
            if (card == null) continue;

            bool isUnlocked = CardCodexManager.IsFormulaCardUnlocked(card);
            CardCodexEntryUI entry = CreateEntry(formulaCardEntryPrefab, isUnlocked);
            if (entry == null) continue;

            if (isUnlocked || entry.showCardWhenLocked)
            {
                FillFormulaCard(entry, card);
            }
        }

        unlocked += formulaUnlocked;
        total += formulaCards.Count;

        // ---------- 3. 祝福（总是刷新 → 本回合限购 → 永不刷新） ----------
        List<CardCodexManager.BlessingGroupData> groups = CardCodexManager.GetOrderedBlessingGroups();
        for (int g = 0; g < groups.Count; g++)
        {
            CardCodexManager.BlessingGroupData group = groups[g];
            if (group == null || group.blessings.Count == 0) continue;

            int groupUnlocked = CountUnlockedBlessings(group.blessings);
            AddHeader("祝福 · " + group.title, groupUnlocked, group.blessings.Count);

            for (int i = 0; i < group.blessings.Count; i++)
            {
                BlessingData blessing = group.blessings[i];
                if (blessing == null) continue;

                bool isUnlocked = CardCodexManager.IsBlessingUnlocked(blessing);
                CardCodexEntryUI entry = CreateEntry(blessingEntryPrefab, isUnlocked);
                if (entry == null) continue;

                if (isUnlocked || entry.showCardWhenLocked)
                {
                    FillBlessing(entry, blessing);
                }
            }

            unlocked += groupUnlocked;
            total += group.blessings.Count;
        }

        UpdateProgressText(unlocked, total);
        RebuildLayout();
    }

    #region 条目创建与填充

    private CardCodexEntryUI CreateEntry(GameObject entryPrefab, bool isUnlocked)
    {
        if (entryPrefab == null)
        {
            Debug.LogWarning("[图鉴] 条目预制体未配置，请在 Inspector 拖入。");
            return null;
        }

        GameObject go = Instantiate(entryPrefab, ResolvedContentRoot);

        CardCodexEntryUI entry = go.GetComponent<CardCodexEntryUI>();
        if (entry == null) entry = go.GetComponentInChildren<CardCodexEntryUI>(true);

        if (entry == null)
        {
            Debug.LogWarning($"[图鉴] 条目预制体「{entryPrefab.name}」没有 CardCodexEntryUI，无法切换上锁显示。");
            return null;
        }

        entry.SetLocked(!isUnlocked);
        return entry;
    }

    /// <summary>数字卡：按 layoutType 取对应卡牌预制体，用实例绑定（骰子显示面数、绿字/黄金取统一颜色）</summary>
    private void FillNumberCard(CardCodexEntryUI entry, NumberCardData data)
    {
        if (entry == null || data == null) return;

        NumberCardUIFactory factory = UIManager.Instance != null ? UIManager.Instance.numberCardLibrary : null;
        GameObject prefab = factory != null ? factory.GetPrefab(data.layoutType) : null;

        if (prefab == null)
        {
            Debug.LogWarning($"[图鉴] 没有配置 {data.layoutType} 类型数字卡的预制体，图鉴里无法显示这张卡。");
            return;
        }

        GameObject card = Instantiate(prefab, entry.CardParent);
        PrepareCardTransform(card);
        PlayerController.SetDragEnabledForHierarchy(card, false);

        NumberCardLayoutView view;
        if (card.TryGetComponent<NumberCardLayoutView>(out view) && view != null)
        {
            // 用"初始值实例"绑定：图鉴展示的是库里的原始数值，不带本局的递增/福星等成长
            view.BindInstance(new NumberCardInstance(data), false);
        }

        HideNumberCardPrice(card);
    }

    /// <summary>公式卡：复用卡组界面的公式卡预制体</summary>
    private void FillFormulaCard(CardCodexEntryUI entry, FormulaCardData data)
    {
        if (entry == null || data == null) return;

        GameObject prefab = UIManager.Instance != null ? UIManager.Instance.formulaCardPrefab : null;
        if (prefab == null)
        {
            Debug.LogWarning("[图鉴] UIManager.formulaCardPrefab 未配置，图鉴里无法显示公式卡。");
            return;
        }

        GameObject card = Instantiate(prefab, entry.CardParent);
        PrepareCardTransform(card);
        PlayerController.SetDragEnabledForHierarchy(card, false);

        FormulaCardUI view = card.GetComponent<FormulaCardUI>();
        if (view == null) view = card.GetComponentInChildren<FormulaCardUI>(true);
        if (view != null) view.Bind(data);
    }

    /// <summary>祝福：复用祝福卡预制体（叠加数量固定传 1，图鉴里不显示 ×N）</summary>
    private void FillBlessing(CardCodexEntryUI entry, BlessingData data)
    {
        if (entry == null || data == null) return;

        GameObject prefab = UIManager.Instance != null ? UIManager.Instance.blessingCardPrefab : null;
        if (prefab == null)
        {
            Debug.LogWarning("[图鉴] UIManager.blessingCardPrefab 未配置，图鉴里无法显示祝福。");
            return;
        }

        GameObject card = Instantiate(prefab, entry.CardParent);
        PrepareCardTransform(card);
        PlayerController.SetDragEnabledForHierarchy(card, false);

        BlessingUI view = card.GetComponent<BlessingUI>();
        if (view == null) view = card.GetComponentInChildren<BlessingUI>(true);
        if (view != null) view.SetBlessingData(data, 1);
    }

    private static void PrepareCardTransform(GameObject card)
    {
        if (card == null) return;

        card.transform.localScale = Vector3.one;

        RectTransform rect = card.transform as RectTransform;
        if (rect != null)
        {
            // 卡面在自己的挂点里居中，避免沿用预制体在商店/手牌里的偏移
            rect.anchoredPosition = Vector2.zero;
        }
    }

    /// <summary>图鉴里不显示数字卡价格（价格是商店信息）</summary>
    private static void HideNumberCardPrice(GameObject card)
    {
        SingleNumberView[] singles = card.GetComponentsInChildren<SingleNumberView>(true);
        for (int i = 0; i < singles.Length; i++)
        {
            if (singles[i] != null && singles[i].priceText != null)
                singles[i].priceText.gameObject.SetActive(false);
        }

        CompositeNumberView[] composites = card.GetComponentsInChildren<CompositeNumberView>(true);
        for (int i = 0; i < composites.Length; i++)
        {
            if (composites[i] != null && composites[i].priceText != null)
                composites[i].priceText.gameObject.SetActive(false);
        }
    }

    #endregion

    #region 标题 / 进度 / 布局

    private void AddHeader(string title, int unlocked, int total)
    {
        if (groupHeaderPrefab == null) return;

        GameObject go = Instantiate(groupHeaderPrefab, ResolvedContentRoot);
        CardCodexGroupHeaderUI header = go.GetComponent<CardCodexGroupHeaderUI>();
        if (header == null) header = go.GetComponentInChildren<CardCodexGroupHeaderUI>(true);

        if (header == null)
        {
            Debug.LogWarning($"[图鉴] 分组标题预制体「{groupHeaderPrefab.name}」没有 CardCodexGroupHeaderUI。");
        }

        string text = showProgressInHeader ? $"{title}  {unlocked}/{total}" : title;
        if (header != null) header.SetTitle(text);
    }

    private void UpdateProgressText(int unlocked, int total)
    {
        if (progressText == null) return;
        progressText.text = $"已解锁 {unlocked} / {total}";
    }

    private void ClearContent(Transform root)
    {
        if (root == null) return;

        // Destroy 是延迟销毁的：先脱离子节点，否则同一帧重建会让布局把旧条目也算进去
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            GameObject child = root.GetChild(i).gameObject;
            child.SetActive(false);
            child.transform.SetParent(null, false);
            Destroy(child);
        }
    }

    private void ResolveScrollRect()
    {
        if (scrollRect != null) return;

        scrollRect = GetComponent<ScrollRect>();
        if (scrollRect == null) scrollRect = GetComponentInParent<ScrollRect>();
        if (scrollRect == null && contentRoot != null)
            scrollRect = contentRoot.GetComponentInParent<ScrollRect>();
    }

    private void RebuildLayout()
    {
        Transform root = ResolvedContentRoot;
        RectTransform rect = root as RectTransform;
        if (rect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }

        if (scrollRect != null)
        {
            RectTransform scrollRectTransform = scrollRect.transform as RectTransform;
            if (scrollRectTransform != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRectTransform);
            }

            // 每次打开都从顶部开始
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    #endregion

    #region 统计

    private static int CountUnlockedNumberCards(List<NumberCardData> cards)
    {
        int count = 0;
        for (int i = 0; i < cards.Count; i++)
        {
            if (CardCodexManager.IsNumberCardUnlocked(cards[i])) count++;
        }
        return count;
    }

    private static int CountUnlockedFormulaCards(List<FormulaCardData> cards)
    {
        int count = 0;
        for (int i = 0; i < cards.Count; i++)
        {
            if (CardCodexManager.IsFormulaCardUnlocked(cards[i])) count++;
        }
        return count;
    }

    private static int CountUnlockedBlessings(List<BlessingData> blessings)
    {
        int count = 0;
        for (int i = 0; i < blessings.Count; i++)
        {
            if (CardCodexManager.IsBlessingUnlocked(blessings[i])) count++;
        }
        return count;
    }

    #endregion
}
