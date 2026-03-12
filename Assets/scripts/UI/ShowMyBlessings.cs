using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 显示我的祝福 - 在卡牌界面中展示玩家已拥有的所有祝福
/// 功能：
/// 1. 仅显示一次重复的祝福，底部显示数量
/// 2. 可叠加的祝福用绿色标明
/// 3. 使用 BlessingUI 预制件来显示
/// </summary>
public class ShowMyBlessings : MonoBehaviour
{
    [Header("容器引用")]
    public Transform contentRoot;  // 祝福内容的父容器

    [Header("预制件引用")]
    public GameObject blessingUIPrefab;  // BlessingUI 预制件

    [Header("滚动配置")]
    public ScrollRect scrollRect;  // 滚动矩形

    [Header("显示设置")]
    public float cardScale = 1.0f;

    [Header("祝福库引用")]
    public BlessingLibrary blessingLibrary;

    // 缓存已显示的祝福ID，避免重复显示
    private Dictionary<int, GameObject> displayedBlessings = new Dictionary<int, GameObject>();
    // 祝福的购买次数映射
    private Dictionary<int, int> blessingStackCounts = new Dictionary<int, int>();

    private void OnEnable()
    {
        InitializeScrollRect();
        RefreshAllBlessings();
    }

    /// <summary>
    /// 初始化滚动支持
    /// </summary>
    void InitializeScrollRect()
    {
        if (scrollRect == null)
        {
            scrollRect = GetComponent<ScrollRect>();
        }

        if (scrollRect == null)
        {
            scrollRect = GetComponentInParent<ScrollRect>();
        }

        // 为 contentRoot 添加布局组件
        VerticalLayoutGroup vlg = contentRoot.GetComponent<VerticalLayoutGroup>();
        if (vlg == null)
        {
            vlg = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.spacing = 10;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            Debug.Log("[ShowMyBlessings] 自动添加了 VerticalLayoutGroup");
        }

        // 为 contentRoot 添加 LayoutElement
        LayoutElement le = contentRoot.GetComponent<LayoutElement>();
        if (le == null)
        {
            le = contentRoot.gameObject.AddComponent<LayoutElement>();
        }
        le.preferredWidth = -1;
        le.preferredHeight = -1;
        le.flexibleHeight = 1;

        // 如果没有 ScrollRect，创建一个
        if (scrollRect == null && contentRoot != null)
        {
            Transform scrollParent = contentRoot.parent;
            if (scrollParent != null)
            {
                scrollRect = scrollParent.GetComponent<ScrollRect>();

                if (scrollRect == null)
                {
                    scrollRect = scrollParent.gameObject.AddComponent<ScrollRect>();
                    scrollRect.content = (RectTransform)contentRoot;
                    scrollRect.horizontal = false;
                    scrollRect.vertical = true;
                    scrollRect.movementType = ScrollRect.MovementType.Elastic;
                    scrollRect.elasticity = 0.1f;
                    scrollRect.scrollSensitivity = 5;

                    Image image = scrollParent.GetComponent<Image>();
                    if (image == null)
                    {
                        image = scrollParent.gameObject.AddComponent<Image>();
                        image.color = new Color(1, 1, 1, 0.01f);
                    }

                    Debug.Log("[ShowMyBlessings] 自动创建了 ScrollRect 组件");
                }
            }
        }
    }

    /// <summary>
    /// 刷新所有祝福显示
    /// </summary>
    public void RefreshAllBlessings()
    {
        // 1. 清空旧显示
        foreach (Transform child in contentRoot)
        {
            Destroy(child.gameObject);
        }
        displayedBlessings.Clear();
        blessingStackCounts.Clear();

        // 2. 获取玩家已拥有的所有祝福
        if (BlessingManager.Instance == null)
        {
            Debug.LogWarning("[ShowMyBlessings] BlessingManager 未初始化");
            return;
        }

        List<BlessingInstance> ownedBlessings = BlessingManager.Instance.GetOwnedBlessings();

        if (ownedBlessings == null || ownedBlessings.Count == 0)
        {
            Debug.Log("[ShowMyBlessings] 玩家没有任何祝福");
            return;
        }

        // 3. 生成祝福卡（去重，只显示一次）
        foreach (var blessingInstance in ownedBlessings)
        {
            if (blessingInstance.data == null)
                continue;

            int blessingId = blessingInstance.data.blessingId;
            int purchaseCount = blessingInstance.purchaseCount;

            // 检查是否已经显示过这个祝福
            if (!displayedBlessings.ContainsKey(blessingId))
            {
                // 首次显示此祝福
                CreateBlessingCard(blessingInstance.data, purchaseCount);
                displayedBlessings[blessingId] = null; // 占位符，之后会赋值
                blessingStackCounts[blessingId] = purchaseCount;
            }
            else
            {
                // 已经显示过，只更新数量
                blessingStackCounts[blessingId] = purchaseCount;
                UpdateBlessingCardStackCount(blessingId, purchaseCount);
            }
        }

        // 4. 强制重建布局
        if (contentRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)contentRoot);
        }

        if (scrollRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.GetComponent<RectTransform>());
        }

        Debug.Log($"[ShowMyBlessings] 成功显示 {displayedBlessings.Count} 个祝福");
    }

    /// <summary>
    /// 创建单个祝福卡
    /// </summary>
    void CreateBlessingCard(BlessingData blessingData, int stackCount)
    {
        if (blessingUIPrefab == null)
        {
            Debug.LogError("[ShowMyBlessings] blessingUIPrefab 未设置！");
            return;
        }

        if (blessingData == null)
        {
            Debug.LogError("[ShowMyBlessings] 祝福数据为空");
            return;
        }

        // 实例化 BlessingUI 预制件
        GameObject go = Instantiate(blessingUIPrefab, contentRoot);
        go.transform.localScale = Vector3.one * cardScale;
        go.SetActive(true);

        // 获取 BlessingUI 脚本并设置数据
        BlessingUI blessingUI = go.GetComponent<BlessingUI>();
        if (blessingUI != null)
        {
            // 传入祝福数据和叠加数量
            blessingUI.SetBlessingData(blessingData, stackCount);

            // 记录映射关系
            displayedBlessings[blessingData.blessingId] = go;

            Debug.Log($"[ShowMyBlessings] 创建祝福卡：{blessingData.blessingName}，叠加数：{stackCount}");
        }
        else
        {
            Debug.LogError("[ShowMyBlessings] BlessingUI 预制件缺少 BlessingUI 脚本！");
        }
    }

    /// <summary>
    /// 更新祝福卡的叠加数量显示
    /// </summary>
    void UpdateBlessingCardStackCount(int blessingId, int newStackCount)
    {
        if (displayedBlessings.TryGetValue(blessingId, out GameObject cardGo))
        {
            if (cardGo == null)
                return;

            BlessingUI blessingUI = cardGo.GetComponent<BlessingUI>();
            if (blessingUI != null)
            {
                // 更新显示的数量
                if (blessingUI.GetCurrentBlessingData() != null)
                {
                    blessingUI.SetBlessingData(blessingUI.GetCurrentBlessingData(), newStackCount);
                    Debug.Log($"[ShowMyBlessings] 更新祝福卡数量：{blessingUI.GetBlessingName()} × {newStackCount}");
                }
            }
        }
    }

    /// <summary>
    /// 获取祝福卡数量
    /// </summary>
    public int GetBlessingCardCount()
    {
        return displayedBlessings.Count;
    }

    /// <summary>
    /// 获取特定祝福的购买次数
    /// </summary>
    public int GetBlessingStackCount(int blessingId)
    {
        return blessingStackCounts.TryGetValue(blessingId, out int count) ? count : 0;
    }

    /// <summary>
    /// 获取所有显示的祝福
    /// </summary>
    public Dictionary<int, GameObject> GetDisplayedBlessings()
    {
        return new Dictionary<int, GameObject>(displayedBlessings);
    }
}