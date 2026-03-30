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
    private Dictionary<int, BlessingUI> blessingUICache = new Dictionary<int, BlessingUI>();
    // 祝福的购买次数映射
    private Dictionary<int, int> blessingStackCounts = new Dictionary<int, int>();

    //初始化标志
    private bool isInitialized = false;

    private void OnEnable()
    {
        if (isInitialized)
        {
            InitializeScrollRect();
            RefreshAllBlessings();
        }
    }
    /// <summary>
    /// 外部初始化方法，应在BlessingManager准备好后调用
    /// </summary>
    public void Initialize()
    {
        if (isInitialized)
        {
            Debug.Log("[ShowMyBlessings] 已经初始化过，跳过重复初始化");
            return;
        }

        Debug.Log("[ShowMyBlessings] 开始初始化");

        // 验证基本组件
        if (contentRoot == null)
        {
            Debug.LogError("[ShowMyBlessings] contentRoot 未赋值！无法初始化");
            return;
        }

        if (blessingUIPrefab == null)
        {
            Debug.LogError("[ShowMyBlessings] blessingUIPrefab 未设置！无法显示祝福");
            return;
        }

        InitializeScrollRect();
        RefreshAllBlessings();
        isInitialized = true;

        Debug.Log("[ShowMyBlessings] 初始化完成");
    }
    /// <summary>
    /// 初始化滚动支持
    /// </summary>
    void InitializeScrollRect()
    {
        if (contentRoot == null)
        {
            Debug.LogError("[ShowMyBlessings] contentRoot 未赋值，无法初始化滚动。请在 Inspector 中关联正确的 Transform");
            return;
        }

        if (scrollRect == null)
        {
            scrollRect = GetComponent<ScrollRect>();
        }

        if (scrollRect == null)
        {
            scrollRect = GetComponentInParent<ScrollRect>();
        }

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
        if (contentRoot == null)
        {
            Debug.LogError("[ShowMyBlessings] contentRoot 为空，无法刷新祝福显示");
            return;
        }

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

        // Prefab 是否有效
        if (blessingUIPrefab == null)
        {
            Debug.LogError("[ShowMyBlessings] blessingUIPrefab 未设置！无法显示祝福");
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

        if (contentRoot == null)
        {
            Debug.LogError("[ShowMyBlessings] contentRoot 为空");
            return;
        }

        try
        {
            // 实例化 BlessingUI 预制件
            GameObject go = Instantiate(blessingUIPrefab, contentRoot);
            go.name = $"BlessingCard_{blessingData.blessingId}_{blessingData.blessingName}";

            //正确设置 RectTransform
            RectTransform rectTransform = go.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.localPosition = Vector3.zero;
                rectTransform.localRotation = Quaternion.identity;
                rectTransform.localScale = Vector3.one * cardScale;

                //// 设置锚点为顶部中心，避免布局问题
                //rectTransform.anchorMin = new Vector2(0.5f, 1);
                //rectTransform.anchorMax = new Vector2(0.5f, 1);
                //rectTransform.pivot = new Vector2(0.5f, 1);
            }

            go.SetActive(true);

            // 获取 BlessingUI 脚本并设置数据
            BlessingUI blessingUI = go.GetComponent<BlessingUI>();
            if (blessingUI != null)
            {
                blessingUI.SetBlessingData(blessingData, stackCount);

                blessingUI.BoundBlessing = blessingData;
                
                // 记录映射关系
                displayedBlessings[blessingData.blessingId] = go;
                blessingUICache[blessingData.blessingId] = blessingUI;

                Debug.Log($"[ShowMyBlessings] 创建祝福卡：{blessingData.blessingName}，叠加数：{stackCount}");
            }
            else
            {
                Debug.LogError($"[ShowMyBlessings] Prefab 缺少 BlessingUI 脚本！");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ShowMyBlessings] 创建祝福卡失败：{e.Message}\n{e.StackTrace}");
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