using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ShowMyBlessings : MonoBehaviour, ISelectablePanel
{
    public Transform contentRoot;
    public ScrollRect scrollRect;

    private Dictionary<BlessingData, GameObject> cardGameObjects = new Dictionary<BlessingData, GameObject>();

    // 反向映射：GameObject → BlessingData，供 CardClickHandler 查找
    private Dictionary<GameObject, BlessingData> goToData = new Dictionary<GameObject, BlessingData>();

    // 脏标记：记录上次刷新时的 InventoryVersion，避免不必要的重建
    private int lastKnownInventoryVersion = -1;

    // 统一卡牌点击处理器（挂载在 contentRoot 上）
    private CardClickHandler cardClickHandler;

    /// <summary>
    /// 保留以兼容旧代码，不再使用。由 CardClickHandler 统一管理点击。
    /// </summary>
    [System.Obsolete("由 CardClickHandler 统一管理，不再需要临时按钮")]
    private List<BlessingData> displayedBlessings = new List<BlessingData>();
    private Dictionary<int, int> blessingStackCounts = new Dictionary<int, int>();
    public List<Button> tempAddedButtons = new List<Button>();

    public float cardScale = 1.0f;

     [Header("删卡提示")]
     [Tooltip("删卡模式下切到祝福界面时显示的提示面板（无法在此界面删除）")]
     public GameObject deletionUnavailablePanel;

    private void OnEnable()
    {
        // 脏检查：仅当库存版本变化时才重建 UI
        int currentVersion = PlayerCardInventory.Instance != null
            ? PlayerCardInventory.Instance.InventoryVersion
            : 0;

        if (currentVersion != lastKnownInventoryVersion)
        {
            RefreshAllBlessings();
            lastKnownInventoryVersion = currentVersion;
        }

        // 确保 CardClickHandler 存在并绑定
        EnsureCardClickHandler();
    }


    private void OnDisable()
    {
    }

    public void RefreshAllBlessings()
    {
        foreach (var kvp in cardGameObjects)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value);
        }
        cardGameObjects.Clear();
        blessingStackCounts.Clear();

        List<BlessingInstance> ownedList = BlessingManager.Instance.GetOwnedBlessings();
        if (ownedList == null || ownedList.Count == 0) return;

        Dictionary<int, (BlessingData data, int totalCount)> blessingDict =
            new Dictionary<int, (BlessingData, int)>();

        foreach (BlessingInstance instance in ownedList)
        {
            if (instance.data == null) continue;
            if (blessingDict.ContainsKey(instance.data.blessingId))
            {
                var existing = blessingDict[instance.data.blessingId];
                blessingDict[instance.data.blessingId] =
                    (existing.data, existing.totalCount + instance.purchaseCount);
            }
            else
            {
                blessingDict[instance.data.blessingId] =
                    (instance.data, instance.purchaseCount);
            }
        }

        foreach (var kvp in blessingDict.Values)
        {
            CreateBlessingCard(kvp.data, kvp.totalCount);
            if (!blessingStackCounts.ContainsKey(kvp.data.blessingId))
                blessingStackCounts[kvp.data.blessingId] = kvp.totalCount;
        }
    }

    public void ShowOnlyStackableOwnedBlessings()
    {
        foreach (var kvp in cardGameObjects)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value);
        }
        cardGameObjects.Clear();

        List<BlessingData> stackableBlessings = BlessingManager.Instance.blessingLibrary.GetAllStackableBlessing();
        if (stackableBlessings == null) return;

        foreach (BlessingData blessing in stackableBlessings)
        {
            int ownedCount = BlessingManager.Instance.GetBlessingCount(blessing.blessingId);
            if (ownedCount > 0)
            {
                CreateBlessingCard(blessing, ownedCount);
            }
        }
    }

    void CreateBlessingCard(BlessingData blessing, int stackCount = 1)
    {
        if (UIManager.Instance.blessingCardPrefab == null)
        {
            Debug.LogError("Blessing Card Prefab is not assigned!");
            return;
        }
        GameObject newCard = Instantiate(UIManager.Instance.blessingCardPrefab, contentRoot);
        newCard.transform.localScale = Vector3.one;
        BlessingUI blessingUI = newCard.GetComponent<BlessingUI>();
        if (blessingUI != null)
        {
            blessingUI.SetBlessingData(blessing);
        }
        cardGameObjects[blessing] = newCard;

        // 填充反向映射，供 CardClickHandler 查找
        goToData[newCard] = blessing;
    }

    public void ClearTempWishCoinButtons()
    {
        // 清理由 UIManager 在关闭选择面板时调用，此处无需额外操作
    }

    public void ClearTempDarkBoxButtons()
    {
        // 清理由 UIManager 在关闭选择面板时调用，此处无需额外操作
    }

    /// <summary>
    /// CardClickHandler 在 WishCoinSelect / DarkBoxSelect 等选择模式下回调此方法。
    /// </summary>
    public void TryHandleSelectionClick(BlessingData data)
    {
        if (data == null) return;
        CardSelectionManager.Instance.OnCardSelected(data);
    }

    /// <summary>
    /// 暴露反向映射，供 CardClickHandler 在点击时查找对应的 BlessingData。
    /// </summary>
    public Dictionary<GameObject, BlessingData> GetGoToData()
    {
        return goToData;
    }

    /// <summary>
    /// ISelectablePanel 接口：选择模式变更时回调
    /// </summary>
    public void OnSelectionModeChanged(CardSelectionManager.SelectionMode newMode)
    {
        // 祝福面板在许愿币/暗箱操作模式下需要响应选择
        // 具体逻辑由 UIManager 在打开面板时设置
    }

    /// <summary>
    /// ISelectablePanel 接口：处理卡牌点击，由 CardClickHandler 统一分发
    /// </summary>
    public void HandleCardClick(GameObject clickedCardRoot, CardSelectionManager.SelectionMode mode)
    {
        if (goToData.TryGetValue(clickedCardRoot, out BlessingData data))
        {
            switch (mode)
            {
                case CardSelectionManager.SelectionMode.WishCoinSelect:
                case CardSelectionManager.SelectionMode.DarkBoxSelect:
                    TryHandleSelectionClick(data);
                    break;
            }
        }
    }

    /// <summary>
    /// 确保 CardClickHandler 组件存在并绑定当前面板。
    /// </summary>
    private void EnsureCardClickHandler()
    {
        if (cardClickHandler == null)
        {
            cardClickHandler = contentRoot != null
                ? contentRoot.GetComponent<CardClickHandler>()
                : null;

            if (cardClickHandler == null && contentRoot != null)
                cardClickHandler = contentRoot.gameObject.AddComponent<CardClickHandler>();
        }

        if (cardClickHandler != null)
        {
            cardClickHandler.Initialize(this);
        }
    }

    /// <summary>
    /// 强制标记下次 OnEnable 时重建 UI（外部调用，如删卡后）。
    /// </summary>
    public void MarkDirty()
    {
        lastKnownInventoryVersion = -1;
    }

     /// <summary>
     /// 显示/隐藏"无法在此界面删除"提示。
     /// </summary>
     public void SetDeletionUnavailableHintVisible(bool visible)
     {
         if (deletionUnavailablePanel != null)
         {
             deletionUnavailablePanel.SetActive(visible);
         }
     }
 }
