using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 图鉴面板：按固定顺序展示全部数字卡 / 公式卡 / 祝福，未解锁的条目显示上锁图标与"未解锁"。
///
/// 三个区域在 Inspector 里各配各的引用（互不共用）：
///   · numberCardSection —— 数字卡区域：contentRoot / scrollRect / progressText
///   · formulaCardSection —— 公式卡区域：contentRoot / scrollRect / progressText
///   · blessingSection    —— 祝福区域：contentRoot / scrollRect / progressText
/// 区域里 contentRoot 留空表示该区域不展示（面板会跳过并给出提示）。
///
/// 展示顺序（由 CardCodexManager 统一给出，三个区域都一样）：
///   数字卡（按资产编号 002→142）→ 公式卡（按 FormulaCardId）→
///   祝福（总是刷新 → 本回合限购 → 永不刷新，组内按 blessingId）。
///
/// 每个区域的显示逻辑与改造前完全一致：
///   清空容器 → 按顺序实例化条目 → 按解锁状态切上锁显示 → 填充卡面 → 写进度 → 复位滚动。
///
/// 条目不生成任何 UI：每个预制体根节点挂 CardCodexEntryUI，内含"卡片挂点 + 上锁 Image + 未解锁 Text"。
/// 卡面复用现有卡牌预制体（数字卡按 layoutType 取、祝福用 UIManager 上的预制体），
/// 因此图鉴里的卡面与商店/卡组保持一致。
/// 公式卡例外：仿照商店展示，在条目预制体里拖一个 Text 到 CardCodexEntryUI.formulaNameText，
/// 面板直接显示公式文本（Pattern），不生成槽位式卡面。
/// </summary>
public class CardCodexPanelUI : MonoBehaviour
{
    /// <summary>一个图鉴区域的三件引用（各自独立，互不复用）</summary>
    [System.Serializable]
    public class SectionRefs
    {
        [Tooltip("该区域的卡片父节点（由你在 Inspector 配置 Grid/Vertical Layout Group）")]
        public Transform contentRoot;

        [Tooltip("该区域的滚动区域；留空则自动从 contentRoot 向上查找 ScrollRect")]
        public ScrollRect scrollRect;

        [Tooltip("可选：该区域自己的进度文本，显示「已解锁 x / y」")]
        public Text progressText;

        public bool HasContent
        {
            get { return contentRoot != null; }
        }
    }

    [Header("数字卡区域")]
    public SectionRefs numberCardSection = new SectionRefs();

    [Header("公式卡区域")]
    public SectionRefs formulaCardSection = new SectionRefs();

    [Header("祝福区域")]
    public SectionRefs blessingSection = new SectionRefs();

    [Header("条目预制体（根节点挂 CardCodexEntryUI）")]
    public GameObject numberCardEntryPrefab;
    public GameObject formulaCardEntryPrefab;
    public GameObject blessingEntryPrefab;

    [Header("选项")]
    [Tooltip("面板每次被打开时自动重建列表")]
    public bool rebuildOnEnable = true;

    /// <summary>内容根节点当前不在激活状态（比如另一页签）而推迟重建的区域</summary>
    private readonly List<SectionRefs> pendingSections = new List<SectionRefs>();

    private void OnEnable()
    {
        ResolveScrollRects();

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
    /// 重建全部三个区域。
    /// 每个区域互不影响：某个区域没配 contentRoot 只会跳过该区域。
    /// </summary>
    public void Rebuild()
    {
        pendingSections.Clear();

        RebuildNumberCards();
        RebuildFormulaCards();
        RebuildBlessings();
    }

    /// <summary>
    /// 内容容器尚未激活（页签还没打开）时，布局组算不出尺寸，先记下来，
    /// 等它被激活的那一帧再补建，避免卡片尺寸/位置错乱。
    /// </summary>
    private void LateUpdate()
    {
        if (pendingSections.Count == 0) return;

        for (int i = pendingSections.Count - 1; i >= 0; i--)
        {
            SectionRefs section = pendingSections[i];
            if (section == null || !section.HasContent)
            {
                pendingSections.RemoveAt(i);
                continue;
            }

            if (!section.contentRoot.gameObject.activeInHierarchy) continue;

            pendingSections.RemoveAt(i);
            RebuildSectionContent(section);
        }
    }

    #region 三个区域

    private void RebuildNumberCards()
    {
        if (!CheckSection(numberCardSection, numberCardEntryPrefab, "数字卡")) return;

        List<NumberCardData> cards = CardCodexManager.GetOrderedNumberCards();
        int unlockedCount = CountUnlockedNumberCards(cards);

        BeginSection(numberCardSection);

        if (IsSectionReady(numberCardSection))
        {
            for (int i = 0; i < cards.Count; i++)
            {
                NumberCardData card = cards[i];
                if (card == null) continue;

                bool isUnlocked = CardCodexManager.IsNumberCardUnlocked(card);
                CardCodexEntryUI entry = CreateEntry(numberCardEntryPrefab, isUnlocked, numberCardSection.contentRoot);
                if (entry == null) continue;

                if (isUnlocked || entry.showCardWhenLocked)
                {
                    FillNumberCard(entry, card);
                }
            }
        }

        EndSection(numberCardSection, unlockedCount, cards.Count);
    }

    private void RebuildFormulaCards()
    {
        if (!CheckSection(formulaCardSection, formulaCardEntryPrefab, "公式卡")) return;

        List<FormulaCardData> cards = CardCodexManager.GetOrderedFormulaCards();
        int unlockedCount = CountUnlockedFormulaCards(cards);

        BeginSection(formulaCardSection);

        if (IsSectionReady(formulaCardSection))
        {
            for (int i = 0; i < cards.Count; i++)
            {
                FormulaCardData card = cards[i];
                if (card == null) continue;

                bool isUnlocked = CardCodexManager.IsFormulaCardUnlocked(card);
                CardCodexEntryUI entry = CreateEntry(formulaCardEntryPrefab, isUnlocked, formulaCardSection.contentRoot);
                if (entry == null) continue;

                if (isUnlocked || entry.showCardWhenLocked)
                {
                    FillFormulaCard(entry, card);
                }
            }
        }

        EndSection(formulaCardSection, unlockedCount, cards.Count);
    }

    private void RebuildBlessings()
    {
        if (!CheckSection(blessingSection, blessingEntryPrefab, "祝福")) return;

        List<CardCodexManager.BlessingGroupData> groups = CardCodexManager.GetOrderedBlessingGroups();

        int unlocked = 0;
        int total = 0;
        for (int g = 0; g < groups.Count; g++)
        {
            CardCodexManager.BlessingGroupData group = groups[g];
            if (group == null || group.blessings.Count == 0) continue;

            unlocked += CountUnlockedBlessings(group.blessings);
            total += group.blessings.Count;
        }

        BeginSection(blessingSection);

        if (IsSectionReady(blessingSection))
        {
            // 顺序：总是刷新 → 本回合限购 → 永不刷新（组内按 blessingId）
            for (int g = 0; g < groups.Count; g++)
            {
                CardCodexManager.BlessingGroupData group = groups[g];
                if (group == null || group.blessings.Count == 0) continue;

                for (int i = 0; i < group.blessings.Count; i++)
                {
                    BlessingData blessing = group.blessings[i];
                    if (blessing == null) continue;

                    bool isUnlocked = CardCodexManager.IsBlessingUnlocked(blessing);
                    CardCodexEntryUI entry = CreateEntry(blessingEntryPrefab, isUnlocked, blessingSection.contentRoot);
                    if (entry == null) continue;

                    if (isUnlocked || entry.showCardWhenLocked)
                    {
                        FillBlessing(entry, blessing);
                    }
                }
            }
        }

        EndSection(blessingSection, unlocked, total);
    }

    /// <summary>某区域被推迟重建时（容器未激活），激活后补建的实际入口</summary>
    private void RebuildSectionContent(SectionRefs section)
    {
        if (section == numberCardSection) RebuildNumberCards();
        else if (section == formulaCardSection) RebuildFormulaCards();
        else if (section == blessingSection) RebuildBlessings();
    }

    #endregion

    #region 区域通用流程

    private bool CheckSection(SectionRefs section, GameObject entryPrefab, string label)
    {
        if (section != null && section.HasContent) return true;

        // 引用和预制体都没配 = 这个区域本来就不使用（比如三个页签各挂一个面板组件），静默跳过
        if (entryPrefab == null) return false;

        Debug.LogWarning($"[图鉴] {label}区域的 contentRoot 未配置，该区域不展示。请在 Inspector 把容器拖到对应区域的 Content Root。");
        return false;
    }

    /// <summary>清空容器（或标记为"等容器激活后重建"）</summary>
    private void BeginSection(SectionRefs section)
    {
        ClearContent(section.contentRoot);

        if (section.contentRoot.gameObject.activeInHierarchy) return;

        if (!pendingSections.Contains(section)) pendingSections.Add(section);
    }

    private bool IsSectionReady(SectionRefs section)
    {
        return section.contentRoot.gameObject.activeInHierarchy;
    }

    /// <summary>写该区域自己的进度文本，并复位该区域的滚动位置（容器未激活时只写文本，布局等激活后再算）</summary>
    private void EndSection(SectionRefs section, int unlocked, int total)
    {
        if (section == null) return;

        if (section.progressText != null)
        {
            section.progressText.text = $"已解锁 {unlocked} / {total}";
        }

        if (section.HasContent && section.contentRoot.gameObject.activeInHierarchy)
        {
            RebuildLayout(section);
        }
    }

    #endregion

    #region 条目创建与填充

    private CardCodexEntryUI CreateEntry(GameObject entryPrefab, bool isUnlocked, Transform parent)
    {
        if (entryPrefab == null)
        {
            Debug.LogWarning("[图鉴] 条目预制体未配置，请在 Inspector 拖入。");
            return null;
        }

        GameObject go = Instantiate(entryPrefab, parent);

        // 关键：NumberCardColletction / BlessCollection 这两个条目预制体的根节点在资产里
        // 存的是 localScale = 0（当初当模板用过），不显式置 1 的话整条条目（背景 + 上锁图标 + 文本 + 卡面）
        // 全被缩放成 0，看起来就是"这个区域什么都没显示"。卡牌库那边（ShowMyNumberCard / ShowMyBlessings）
        // 实例化后也有一模一样的 localScale = Vector3.one，原因相同。
        PrepareEntryTransform(go);

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

    /// <summary>条目实例化后的变换归一：必须显式激活并把缩放复位（预制体模板可能存成 0 缩放）</summary>
    private static void PrepareEntryTransform(GameObject entry)
    {
        if (entry == null) return;

        entry.SetActive(true);
        entry.transform.localScale = Vector3.one;
        entry.transform.localRotation = Quaternion.identity;
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
        PrepareCardTransform(card, entry.CardParent as RectTransform);
        PlayerController.SetDragEnabledForHierarchy(card, false);

        NumberCardLayoutView view;
        if (card.TryGetComponent<NumberCardLayoutView>(out view) && view != null)
        {
            // 用"初始值实例"绑定：图鉴展示的是库里的原始数值，不带本局的递增/福星等成长
            view.BindInstance(new NumberCardInstance(data), false);
        }

        HideNumberCardPrice(card);
    }

    /// <summary>
    /// 公式卡：仿照商店展示（ShopFormulaCardSlot 只显示公式文本），
    /// 条目预制体里拖了 formulaNameText 就直接显示 Pattern，不生成槽位式卡面；
    /// 没拖则回退为旧版的槽位式卡面，并提示补拖。
    /// </summary>
    private void FillFormulaCard(CardCodexEntryUI entry, FormulaCardData data)
    {
        if (entry == null || data == null) return;

        // 仿商店：直接显示公式文本（与 ShopFormulaCardSlot.DisplayFormulaInfo 同一字段）
        if (entry.formulaNameText != null)
        {
            entry.formulaNameText.text = data.Pattern;
            return;
        }

        Debug.LogWarning("[图鉴] 公式卡条目预制体未拖 formulaNameText，回退为槽位式卡面。请在公式卡条目预制体里加一个 Text 并拖到 CardCodexEntryUI.formulaNameText。");

        GameObject prefab = UIManager.Instance != null ? UIManager.Instance.formulaCardPrefab : null;
        if (prefab == null)
        {
            Debug.LogWarning("[图鉴] UIManager.formulaCardPrefab 未配置，图鉴里无法显示公式卡。");
            return;
        }

        GameObject card = Instantiate(prefab, entry.CardParent);
        PrepareCardTransform(card, entry.CardParent as RectTransform);
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
        PrepareCardTransform(card, entry.CardParent as RectTransform);
        PlayerController.SetDragEnabledForHierarchy(card, false);

        BlessingUI view = card.GetComponent<BlessingUI>();
        if (view == null) view = card.GetComponentInChildren<BlessingUI>(true);
        if (view != null) view.SetBlessingData(data, 1);
    }

    /// <summary>
    /// 卡面实例化后的变换归一。
    /// 卡牌库是把卡面塞进 GridLayoutGroup，由布局组给锚点/尺寸；图鉴的挂点没有布局组，
    /// 而卡牌预制体根节点普遍是 sizeDelta 0 + 锚点(0,0) 的"空壳"（真正的图形在子物体上），
    /// 不补尺寸的话子物体会以挂点左下角为中心偏移，严重时直接被 Mask 裁掉。
    /// </summary>
    private static void PrepareCardTransform(GameObject card, RectTransform slot)
    {
        if (card == null) return;

        card.SetActive(true);
        card.transform.localScale = Vector3.one;
        card.transform.localRotation = Quaternion.identity;

        RectTransform rect = card.transform as RectTransform;
        if (rect == null) return;

        // 卡面在自己的挂点里居中，避免沿用预制体在商店/手牌里的偏移
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        // 0 尺寸的根节点用挂点尺寸兜底（相当于布局组替它做的事）
        if (rect.sizeDelta.x < 1f || rect.sizeDelta.y < 1f)
        {
            float width = slot != null ? slot.rect.width : 0f;
            float height = slot != null ? slot.rect.height : 0f;

            if (width < 1f) width = rect.sizeDelta.x > 0f ? rect.sizeDelta.x : 200f;
            if (height < 1f) height = rect.sizeDelta.y > 0f ? rect.sizeDelta.y : 280f;

            rect.sizeDelta = new Vector2(width, height);
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

    #region 布局 / 清理

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

    private void ResolveScrollRects()
    {
        ResolveScrollRect(numberCardSection);
        ResolveScrollRect(formulaCardSection);
        ResolveScrollRect(blessingSection);
    }

    private static void ResolveScrollRect(SectionRefs section)
    {
        if (section == null || !section.HasContent) return;

        if (section.scrollRect == null)
        {
            section.scrollRect = section.contentRoot.GetComponentInParent<ScrollRect>();
        }
    }

    private void RebuildLayout(SectionRefs section)
    {
        if (section == null || !section.HasContent) return;

        RectTransform content = section.contentRoot as RectTransform;
        if (content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }

        if (section.scrollRect != null)
        {
            RectTransform scrollRectTransform = section.scrollRect.transform as RectTransform;
            if (scrollRectTransform != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRectTransform);
            }

            // 每次打开都从顶部开始（各区域各管各的滚动位置）
            section.scrollRect.verticalNormalizedPosition = 1f;
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
