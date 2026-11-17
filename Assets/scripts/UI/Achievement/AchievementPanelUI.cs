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
        }

        if (progressText != null)
        {
            progressText.text = $"已解锁 {AchievementManager.UnlockedCount} / {AchievementManager.Count}";
        }

        RebuildLayout();
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
