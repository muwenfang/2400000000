using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 成就面板：展示全部成就（名字 / 描述 / 是否解锁），不含解锁判定逻辑。
///
/// 展示顺序（AchievementManager.GetDisplayList）：
///   解锁的成就前置 —— 最新解锁的排最顶；未解锁的按《成就.xlsx》表格顺序跟在后面。
///
/// 展示规则：
///   名字固定显示；解锁后描述显示原文，未解锁则描述位置显示"未解锁"。
///
/// UI 搭建约定（面板本身不生成任何布局，全部在 Inspector 里拖）：
///   · contentRoot   —— 条目列表容器（Vertical/Grid Layout Group 由你配置）
///   · entryPrefab   —— 条目预制体：根节点挂 AchievementEntryUI（nameText / descText / lockImage 拖好）
///   · progressText  —— 可选，"已解锁 x / y"
///   · scrollRect    —— 可选，留空会自动往上找
///
/// 打开面板（Open / gameObject 激活）时自动重建列表，实时反映当前解锁状态。
/// </summary>
public class AchievementPanelUI : MonoBehaviour
{
    [Header("列表容器")]
    [Tooltip("成就条目的父节点（由你在 Inspector 配置布局组）")]
    public Transform contentRoot;

    [Tooltip("滚动区域；留空则自动向上查找 ScrollRect")]
    public ScrollRect scrollRect;

    [Header("条目预制体（根节点挂 AchievementEntryUI）")]
    public GameObject entryPrefab;

    [Header("进度文本（可选）")]
    public Text progressText;

    [Header("选项")]
    [Tooltip("面板每次被打开（OnEnable）时自动重建列表")]
    public bool rebuildOnEnable = true;

    [Tooltip("条目预制体根节点高度与内容视觉高度不一致时，按内容实际高度修正，并保持你当前看到的行距（避免首尾条目被视图裁切）")]
    public bool autoFitEntryHeight = true;

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
    /// 重建整个成就列表：解锁的前置（最新在最顶），未解锁按表格顺序。
    /// </summary>
    public void Rebuild()
    {
        Transform root = ResolvedContentRoot;
        ClearContent(root);

        if (entryPrefab == null)
        {
            Debug.LogWarning("[成就] 条目预制体未配置，请在 Inspector 拖入 entryPrefab。");
            return;
        }

        List<AchievementData> display = AchievementManager.GetDisplayList();
        List<RectTransform> entries = new List<RectTransform>(display.Count);

        for (int i = 0; i < display.Count; i++)
        {
            GameObject go = Instantiate(entryPrefab, root);
            AchievementEntryUI entry = go.GetComponent<AchievementEntryUI>();
            if (entry == null) entry = go.GetComponentInChildren<AchievementEntryUI>(true);

            if (entry == null)
            {
                Debug.LogWarning($"[成就] 条目预制体「{entryPrefab.name}」没有 AchievementEntryUI，无法填充显示。");
                continue;
            }

            entry.SetData(display[i]);

            RectTransform entryRect = go.transform as RectTransform;
            if (entryRect != null) entries.Add(entryRect);
        }

        // 条目高度按内容的真实视觉高度修正（见 FitEntryHeights 注释），必须在排版前完成
        FitEntryHeights(root, entries);

        if (progressText != null)
        {
            progressText.text = $"已解锁 {AchievementManager.UnlockedCount} / {AchievementManager.Count}";
        }

        RebuildLayout();
    }

    /// <summary>
    /// 条目高度自适应。解决的问题：
    ///   条目预制体根节点高度（如 40.88）与子节点视觉高度（如 165.86）不一致时，
    ///   布局组按根节点高度排行，子节点居中就会上下各溢出半截，
    ///   首条上沿、末条下沿超出 content 边界，ScrollRect 夹紧后这两段永远滚不到。
    ///
    /// 做法：
    ///   1. 先按当前配置排一次版，量出你实际看到的行距（相邻条目 y 差）；
    ///   2. 量每条条目内部所有子节点的真实视觉高度，取最大值作为行高；
    ///   3. 写回行高：布局组勾了 Control Child Height → 用 LayoutElement.preferredHeight；
    ///      否则直接改条目 RectTransform 高度（Ctrl+H 未勾选时布局组读的就是它）；
    ///   4. 反算间距 = 原行距 - 行高，保证视觉行距与你调好的一致，只是不再裁切。
    /// 任何一步量不出有效值都直接跳过，不影响面板正常显示。
    /// </summary>
    private void FitEntryHeights(Transform root, List<RectTransform> entries)
    {
        if (!autoFitEntryHeight || entries == null || entries.Count == 0) return;

        RectTransform contentRect = root as RectTransform;
        if (contentRect == null) return;

        VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();

        // 1. 按当前配置先排一次，取得"你看到的行距"
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        float pitch = 0f;
        if (entries.Count > 1 && entries[0] != null && entries[1] != null)
        {
            pitch = Mathf.Abs(entries[1].anchoredPosition.y - entries[0].anchoredPosition.y);
        }

        // 2. 量真实视觉高度
        float visualHeight = 0f;
        for (int i = 0; i < entries.Count; i++)
        {
            visualHeight = Mathf.Max(visualHeight, MeasureVisualHeight(entries[i]));
        }
        if (visualHeight <= 0.01f) return;

        // 3. 写回行高
        bool controlHeight = layout != null && layout.childControlHeight;
        for (int i = 0; i < entries.Count; i++)
        {
            RectTransform entry = entries[i];
            if (entry == null) continue;

            if (controlHeight)
            {
                // 被控轴由布局组决定，必须通过 LayoutElement 给首要高度，改 sizeDelta 会被覆盖
                LayoutElement element = entry.GetComponent<LayoutElement>();
                if (element == null) element = entry.gameObject.AddComponent<LayoutElement>();

                element.minHeight = visualHeight;
                element.preferredHeight = visualHeight;
            }
            else
            {
                entry.sizeDelta = new Vector2(entry.sizeDelta.x, visualHeight);
            }
        }

        // 4. 保持原有视觉行距（行距 = 行高 + 间距）
        if (layout != null && pitch > 0f)
        {
            layout.spacing = Mathf.Max(0f, pitch - visualHeight);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    }

    /// <summary>量条目内部所有子节点的真实视觉高度（含未激活的子节点，避免解锁前后行高跳动）</summary>
    private static float MeasureVisualHeight(RectTransform entry)
    {
        if (entry == null) return 0f;

        float top = float.MinValue;
        float bottom = float.MaxValue;
        bool any = false;

        Vector3[] corners = new Vector3[4];
        for (int i = 0; i < entry.childCount; i++)
        {
            RectTransform child = entry.GetChild(i) as RectTransform;
            if (child == null) continue;

            child.GetWorldCorners(corners);
            for (int c = 0; c < corners.Length; c++)
            {
                float y = entry.InverseTransformPoint(corners[c]).y;
                if (y > top) top = y;
                if (y < bottom) bottom = y;
            }
            any = true;
        }

        if (!any) return 0f;
        return top - bottom;
    }

    #region 布局辅助（与图鉴面板同一套做法）

    private static void ClearContent(Transform root)
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
}
