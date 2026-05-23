using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.UI;

public class ShowMyNumberCard : MonoBehaviour
{
    [Header("容器引用")]
    public Transform contentRoot;

    [Header("滚动配置")]
    public ScrollRect scrollRect; // 关键：需要绑定 ScrollRect 组件

    private Dictionary<NumberCardInstance, GameObject> cardGameObjects = new Dictionary<NumberCardInstance, GameObject>();
    [Header("显示设置")]
    public float cardScale = 1.0f;

    [Header("删卡功能配置")]
    [Tooltip("删除卡牌按钮 - 从Inspector拖入")]
    public Button deleteNumberCardButton;

    public GameObject deletionCostPanel;
    public Text deletionCostText;
    public BigInteger deletionCost = 10;

    [Header("颜色配置")]
    public Color incrementalColor = Color.green;   // 递增数字：绿色
    public Color diceColor = Color.red; // 骰子数字：红色
    public Color normalColor = Color.black;        // 普通数字：黑色

    private void OnEnable()
    {
        InitializeScrollRect();
        RefreshAllCards();

        // 根据当前的SelectionMode激活对应的button
        ActivateButtonsBasedOnMode();
    }
    /// <summary>
    /// 初始化滚轮支持
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
        if (scrollRect == null)
        {
            // 尝试在父物体的父物体找
            Transform parent = contentRoot.parent;
            if (parent != null)
            {
                scrollRect = parent.GetComponent<ScrollRect>();
            }
        }

        // 确保 contentRoot 有 LayoutElement
        LayoutElement le = contentRoot.GetComponent<LayoutElement>();
        if (le == null)
        {
            le = contentRoot.gameObject.AddComponent<LayoutElement>();
        }
        le.preferredWidth = -1;      // 不设置宽度约束
        le.preferredHeight = -1;     // 不设置高度约束（由内容决定）
        le.flexibleHeight = 1;       // 允许灵活高度

        // 找到或创建 ScrollRect
        if (scrollRect == null)
        {
            // 尝试在当前物体找
            scrollRect = GetComponent<ScrollRect>();
        }

        // 如果没找到，自动创建
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
                    scrollRect.horizontal = false;      // 禁用水平滚动
                    scrollRect.vertical = true;         // 启用垂直滚动
                    scrollRect.movementType = ScrollRect.MovementType.Elastic;
                    scrollRect.elasticity = 0.1f;
                    scrollRect.scrollSensitivity = 15;   // 调整滚动灵敏度

                    Image image = scrollParent.GetComponent<Image>();
                    if (image == null)
                    {
                        image = scrollParent.gameObject.AddComponent<Image>();
                        image.color = new Color(1, 1, 1, 0.01f);
                    }

                    Debug.Log("[ShowMyCard] 自动创建了 ScrollRect 组件");
                }
            }
        }

        //确保 contentRoot 父物体的 RectTransform 配置正确
        RectTransform scrollRectTransform = scrollRect.GetComponent<RectTransform>();
        if (scrollRectTransform != null)
        {
            // 设置 ScrollRect 的大小约束
            LayoutElement scrollLE = scrollRect.GetComponent<LayoutElement>();
            if (scrollLE == null)
            {
                scrollLE = scrollRect.gameObject.AddComponent<LayoutElement>();
            }
            scrollLE.preferredHeight = 600;  //设置滚动区域的高度（可根据需要调整）
        }

    }
    public void RefreshAllCards()
    {
        // 1. 清理旧卡牌
        foreach (Transform child in contentRoot)
        {
            Destroy(child.gameObject);
        }
        cardGameObjects.Clear();

        GenerateNumberCards();
        
        // 3. 强制重建布局
        if (contentRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)contentRoot);
        }

        if (scrollRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.GetComponent<RectTransform>());
        }

        // 每次刷新卡牌后，重新激活按钮（防止新生成的卡牌没按钮）
        ActivateButtonsBasedOnMode();

        if (ShopManager.Instance != null)
        {
            deletionCost = ShopManager.Instance.GetNextNumberCardDeletionCost();
        }

        UpdateDeletionUI(deletionCost);
    }
    private void UpdateDeletionUI(BigInteger cost)
    {
        if (ShopManager.Instance.isDeletionMode == false)
        {
            deletionCostPanel.gameObject.SetActive(false);
        }
        else
        {
            deletionCostPanel.gameObject.SetActive(true);
            deletionCostPanel.transform.SetAsLastSibling(); // 确保在最前面显示

            deletionCostText.text = "六          " + FormatBigNumber(cost).ToString();

            Debug.Log($"[ShowMyNumberCard] 更新UI ");
        }
        
    }
    public string FormatBigNumber(BigInteger number)
    {
        BigInteger threshold = 1000000000;

        if (BigInteger.Abs(number) >= threshold)
        {
            string numStr = number.ToString();
            bool isNegative = numStr.StartsWith("-");
            if (isNegative) numStr = numStr.Substring(1);

            int len = numStr.Length;
            string digits = numStr.Substring(0, System.Math.Min(3, len));
            while (digits.Length < 3) digits += "0";

            string decimalPart = digits[0] + "." + digits.Substring(1);
            int exponent = len - 1;
            string result = $"{decimalPart}e{exponent}";
            return isNegative ? "-" + result : result;
        }
        else
        {
            return number.ToString();
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

            go.transform.localScale = UnityEngine.Vector3.one * cardScale;
            go.transform.localPosition = UnityEngine.Vector3.zero;
            go.transform.localRotation = UnityEngine.Quaternion.identity;
            go.SetActive(true); // 确保显示

            // 卡牌展示面板中的数字卡不应参与拖拽，子物体上的 PlayerController 也一并禁用。
            PlayerController.SetDragEnabledForHierarchy(go, false);

            if (go.TryGetComponent<NumberCardLayoutView>(out var view))
            {
                view.BindInstance(instance, false);
            }

            var singleView = go.GetComponent<SingleNumberView>();
            if (singleView != null && singleView.priceText != null)
            {
                singleView.priceText.gameObject.SetActive(false);
            }

            var compositeView = go.GetComponent<CompositeNumberView>();
            if (compositeView != null && compositeView.priceText != null)
            {
                compositeView.priceText.gameObject.SetActive(false);
            }

            cardGameObjects[instance] = go;
        }
    }
    private void OnDisable()
    {
        // 禁用删卡按钮
        if (deleteNumberCardButton != null)
            deleteNumberCardButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// 进入删卡模式时调用 - 由ShopManager.StartCardDeletion()调用
    /// 此时才初始化删卡UI
    /// </summary>
    public void EnterDeletionMode()
    {
        if (ShopManager.Instance == null) return;

        var allCards = PlayerCardInventory.Instance.GetAllNumberCards();
        if (allCards == null || allCards.Count == 0) return;

        // 计算当前状态下删卡的成本（会根据已删除的卡牌数量计算）
        BigInteger initialCost = ShopManager.Instance.CalculateDeletionCost(allCards[0]);

        UpdateDeletionUI(initialCost);
        Debug.Log($"[ShowMyNumberCard] 进入删卡模式，初始消耗: {initialCost}");
    }
    /// <summary>
    /// 根据当前选择模式激活对应的button
    /// </summary>
    private void ActivateButtonsBasedOnMode()
    {
        var mode = CardSelectionManager.Instance.GetCurrentMode();

        // 删卡模式：激活数字卡的删除按钮
        if (mode == CardSelectionManager.SelectionMode.RemoveCard)
        {
            ActivateNumberCardDeletionButtons();
        }
    }

    /// <summary>
    /// 激活数字卡的删除按钮
    /// </summary>
    private void ActivateNumberCardDeletionButtons()
    {
        var instances = PlayerCardInventory.Instance.GetAllNumberCards();

        int activatedCount = 0;

        foreach (var instance in instances)
        {
            if (instance == null || !cardGameObjects.ContainsKey(instance))
                continue;

            GameObject cardGo = cardGameObjects[instance];
            if (cardGo == null) continue;

            // 查找或添加删除按钮
            Button deleteBtn = cardGo.GetComponent<Button>();
            if (deleteBtn == null)
            {
                deleteBtn = cardGo.GetComponentInChildren<Button>(true);
            }

            // 清除之前的监听
            deleteBtn.onClick.RemoveAllListeners();

            // 添加删除回调 - 使用局部变量捕获，避免闭包问题
            NumberCardInstance cardInstance = instance;
            deleteBtn.onClick.AddListener(() => OnNumberCardDeleteSelected(cardInstance));

            // 激活按钮
            deleteBtn.gameObject.SetActive(true);

            activatedCount++;

            Debug.Log($"[ShowMyNumberCard] 激活数字删除按钮");
        }
    }

    /// <summary>
    /// 处理数字卡删除选择
    /// </summary>
    private void OnNumberCardDeleteSelected(NumberCardInstance selectedCard)
    {
        // 把数量判定放在这里。用户点击时如果没达到条件，提示并拒绝删除。
        if (!PlayerCardInventory.Instance.CanRemoveNumberCard())
        {
            Debug.LogWarning($"[ShowMyNumberCard] 无法删除数字卡：最少需要保留 {PlayerCardInventory.Instance.minNumberCardCount} 张");
            return;
        }

        if (selectedCard == null)
        {
            Debug.LogError("[ShowMyNumberCard] 选择的数字卡为空");
            return;
        }

        // 触发CardSelectionManager的回调
        CardSelectionManager.Instance.OnCardSelected(selectedCard);
        deletionCost = ShopManager.Instance.CalculateDeletionCost(selectedCard);

        // 执行删卡逻辑
        ExecuteNumberCardDeletion(selectedCard);
    }

    /// <summary>
    /// 执行数字卡删除逻辑
    /// </summary>
    private void ExecuteNumberCardDeletion(NumberCardInstance cardToDelete)
    {
        // --- 核心修改：先问商店能不能删 ---
        if (ShopManager.Instance != null)
        {
            // 如果商店拦截（没钱或冷却），直接返回，不执行移除
            if (!ShopManager.Instance.OnCardDeleted(cardToDelete))
            {
                return;
            }
        }
        // 删除卡牌
        bool deleted = PlayerCardInventory.Instance.RemoveNumberCard(cardToDelete);

        if (deleted)
        {
            Debug.Log($"[ShowMyNumberCard] 成功删除数字卡：{cardToDelete.cardData.cardName}");

            CardManager.Instance.SyncDeckFromInventory();
            // 刷新显示
            RefreshAllCards();
        }
        else
        {
            Debug.LogWarning("[ShowMyNumberCard] 删除数字卡失败");
        }
    }
    
    // 老千专用：显示数字卡并自动给每张卡加按钮（许愿币同款逻辑）
    public void ShowCardsForCardCheat()
    {
        // 清空旧卡片
        foreach (Transform child in contentRoot)
            Destroy(child.gameObject);
        cardGameObjects.Clear();

        // 正常生成数字卡
        GenerateNumberCards();

        // 给每张卡自己加按钮（核心逻辑，和许愿币一样）
        foreach (var pair in cardGameObjects)
        {
            NumberCardInstance card = pair.Key;
            GameObject cardObj = pair.Value;

            // 获取或添加 Button
            Button btn = cardObj.GetComponent<Button>();
            if (btn == null)
                btn = cardObj.AddComponent<Button>();

            // 清除旧监听，避免重复
            btn.onClick.RemoveAllListeners();

            // 点击后直接提交选择
            btn.onClick.AddListener(() =>
            {
                CardSelectionManager.Instance.OnCardSelected(card);
            });
        }

        // 刷新UI布局
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)contentRoot);
    }

}
