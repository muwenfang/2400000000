using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 显示我的卡牌 
/// </summary>
public class ShowMyCard : MonoBehaviour
{
    [Header("类型设置")]
    public bool showNumberCards = true;
    public bool showFormulaCards = false;

    [Header("容器引用")]
    public Transform contentRoot;

    [Header("滚动配置")]
    public ScrollRect scrollRect; // 关键：需要绑定 ScrollRect 组件

    [Header("删除模式")]
    public Text deleteCostText; // 显示删除所需点数的文本
    public GameObject deleteCardSlotPrefab; [Header("删除模式Prefabs")]
    public GameObject deleteNumberCardSlotPrefab;  // 数字卡删除槽位
    public GameObject deleteFormulaCardSlotPrefab; // 公式卡删除槽位
    [Header("显示设置")]
    public float cardScale = 1.0f;

    // 删除模式状态
    private bool isDeleteMode = false;
    private Dictionary<NumberCardInstance, GameObject> cardGameObjects = new Dictionary<NumberCardInstance, GameObject>();

    [Header("颜色配置")]
    public Color incrementalColor = Color.green;   // 递增数字：绿色
    public Color diceColor = Color.red; // 骰子数字：红色
    public Color normalColor = Color.black;        // 普通数字：黑色

    private void OnEnable()
    {
        InitializeScrollRect();
        RefreshAllCards();

        // 检查是否处于删除模式
        isDeleteMode = (GameManager.Instance.currentState == GameManager.GameState.Shop);
        UpdateDeleteModeUI();
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

        // 2. 根据类型生成
        if (isDeleteMode)
        {
            // 删除模式：显示数字卡和公式卡
            if (showNumberCards)
                GenerateNumberCardsForDeletion();
            if (showFormulaCards)
                GenerateFormulaCardsForDeletion();
        }
        else
        {
            // 正常模式
            if (showNumberCards)
                GenerateNumberCards();
            if (showFormulaCards)
                GenerateFormulaCards();
        }
        // 3. 强制重建布局
        if (contentRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)contentRoot);
        }

        if (scrollRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.GetComponent<RectTransform>());
        }
    }
    /// <summary>
    ///  删除模式：生成数字卡槽位
    /// </summary>
    void GenerateNumberCardsForDeletion()
    {
        if (deleteNumberCardSlotPrefab == null)
        {
            Debug.LogError("[ShowMyCard] deleteNumberCardSlotPrefab 未设置");
            return;
        }

        var instances = PlayerCardInventory.Instance.GetAllNumberCards();
        if (instances == null) return;

        foreach (var instance in instances)
        {
            if (instance == null || instance.cardData == null) continue;

            // 实例化 DeleteCardSlot
            GameObject slotGo = Instantiate(deleteNumberCardSlotPrefab, contentRoot);
            slotGo.transform.localScale = UnityEngine.Vector3.one * cardScale;
            slotGo.SetActive(true);

            // 绑定数字卡
            DeleteCardSlot slot = slotGo.GetComponent<DeleteCardSlot>();
            if (slot != null)
            {
                // 注意：DeleteCardSlot 使用 Action<object> 回调，统一以 object 传递，ShowMyCard 统一处理类型
                slot.BindNumberCardForDeletion(instance, (obj) =>
                {
                    OnCardDeleteClick(obj);
                });

                Debug.Log($"[ShowMyCard] 为删除模式创建数字卡槽位：{instance.cardData.cardName}");
            }
            else
            {
                Debug.LogError("[ShowMyCard] deleteNumberCardSlotPrefab 缺少 DeleteCardSlot 组件");
            }

            cardGameObjects[instance] = slotGo;
        }
    }

    /// <summary>
    ///  删除模式：生成公式卡槽位
    /// </summary>
    void GenerateFormulaCardsForDeletion()
    {
        // 确定使用哪个prefab
        GameObject slotPrefab = deleteFormulaCardSlotPrefab ?? deleteNumberCardSlotPrefab;

        if (slotPrefab == null)
        {
            Debug.LogError("[ShowMyCard] deleteFormulaCardSlotPrefab 未设置");
            return;
        }

        var deck = CardManager.Instance.formulaCardDeck;
        if (deck == null) return;

        foreach (var formulaData in deck)
        {
            if (formulaData == null) continue;

            // 实例化 DeleteCardSlot
            GameObject slotGo = Instantiate(slotPrefab, contentRoot);
            slotGo.transform.localScale = UnityEngine.Vector3.one * cardScale;
            slotGo.SetActive(true);

            // 绑定公式卡
            DeleteCardSlot slot = slotGo.GetComponent<DeleteCardSlot>();
            if (slot != null)
            {
                slot.BindFormulaCardForDeletion(formulaData, (obj) =>
                {
                    OnCardDeleteClick(obj);
                });

                Debug.Log($"[ShowMyCard] 为删除模式创建公式卡槽位：{formulaData.Name}");
            }
            else
            {
                Debug.LogError("[ShowMyCard] 公式卡槽位prefab 缺少 DeleteCardSlot 组件");
            }
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
            go.SetActive(true); // 确保显示

            //禁用 PlayerController 脚本
            PlayerController playerController = go.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.enabled = false;  // 禁用拖动功能
                Debug.Log("[ShowMyCard] 禁用了卡牌的拖动功能");
            }

            //禁用 IBeginDragHandler, IDragHandler, IEndDragHandler
            IBeginDragHandler beginDragHandler = go.GetComponent<IBeginDragHandler>();
            IDragHandler dragHandler = go.GetComponent<IDragHandler>();
            IEndDragHandler endDragHandler = go.GetComponent<IEndDragHandler>();

            // 移除这些事件监听（通过禁用脚本）
            foreach (var component in go.GetComponents<MonoBehaviour>())
            {
                if (component is PlayerController)
                {
                    component.enabled = false;
                }
            }

            // 判断是哪种视图组件，分别赋值
            // 尝试获取单数字视图
            var singleView = go.GetComponent<SingleNumberView>();
            if (singleView != null)
            {
                // 设置数值 (使用实例中的 currentA)
                SetPartDisplay(singleView.valueText, instance.cardData.partA, instance.currentA);
                // 可以在这里设置价格文字隐藏或显示
                if (singleView.priceText != null) singleView.priceText.gameObject.SetActive(false);
            }
            // 尝试获取组合视图 (加法/乘法/乘方)
            else
            {
                var compositeView = go.GetComponent<CompositeNumberView>();
                if (compositeView != null)
                {
                    // 设置 Part A (使用 currentA)
                    SetPartDisplay(compositeView.aText, instance.cardData.partA, instance.currentA);

                    // 设置 Part B (使用 currentB)
                    SetPartDisplay(compositeView.bText, instance.cardData.partB, instance.currentB);
                }
            }
            // 在删除模式下，卡牌上的删除按钮处于活跃状态
            if (isDeleteMode)
            {
                // 卡牌按钮已存在，获取它并添加删除回调
                Button deleteButton = go.GetComponentInChildren<Button>();
                if (deleteButton != null)
                {
                    deleteButton.onClick.RemoveAllListeners();
                    // 传递 object，统一由 OnCardDeleteClick(object) 处理类型
                    deleteButton.onClick.AddListener(() => OnCardDeleteClick((object)instance));
                }
            }

            cardGameObjects[instance] = go;
        }
    }
    /// <summary>
    /// 卡牌删除按钮点击事件（统一接收 object，内部按类型分发）
    /// </summary>
    void OnCardDeleteClick(object cardObj)
    {
        if (!isDeleteMode)
        {
            Debug.LogWarning("[ShowMyCard] 不在删除模式");
            return;
        }

        if (cardObj == null)
        {
            Debug.LogWarning("[ShowMyCard] 卡牌为空");
            return;
        }
        // 判断卡牌类型并分发
        if (cardObj is NumberCardInstance numberCard)
        {
            Debug.Log($"[ShowMyCard] 用户选择删除数字卡：{numberCard.cardData.cardName}");
            ShowDeleteConfirmation_Number(numberCard);
        }
        else if (cardObj is FormulaCardData formulaCard)
        {
            Debug.Log($"[ShowMyCard] 用户选择删除公式卡：{formulaCard.Name}");
            ShowDeleteConfirmation_Formula(formulaCard);
        }
        else
        {
            Debug.LogWarning("[ShowMyCard] 未知的卡牌类型，无法删除");
        }
    }

    /// <summary>
    /// 显示数字卡删除确认
    /// </summary>
    void ShowDeleteConfirmation_Number(NumberCardInstance card)
    {
        BigInteger cost = ShopManager.Instance.CalculateNumberRemoveCost();
        string message = $"确认删除 [{card.cardData.cardName}] 吗？\n消耗点数: {cost}";

        if (UIManager.Instance.confirmationPanel != null)
        {
            UIManager.Instance.confirmationPanel.SetActive(true);

            Text confirmText = UIManager.Instance.confirmationPanel.GetComponentInChildren<Text>();
            if (confirmText != null)
                confirmText.text = message;

            Button[] buttons = UIManager.Instance.confirmationPanel.GetComponentsInChildren<Button>();
            if (buttons.Length >= 2)
            {
                buttons[0].onClick.RemoveAllListeners();
                buttons[1].onClick.RemoveAllListeners();

                buttons[0].onClick.AddListener(() => ExecuteRemoveNumberCard(card, cost));
                buttons[1].onClick.AddListener(() => UIManager.Instance.confirmationPanel.SetActive(false));
            }
        }
    }

    /// <summary>
    /// 执行删除数字卡
    /// </summary>
    void ExecuteRemoveNumberCard(NumberCardInstance card, BigInteger cost)
    {
        if (GameManager.Instance.currentPoints < cost)
        {
            UIManager.Instance.confirmationPanel.SetActive(false);
            Debug.LogWarning("[ShowMyCard] 点数不足");
            return;
        }

        if (PlayerCardInventory.Instance.numberCards.Count <= 6)
        {
            UIManager.Instance.confirmationPanel.SetActive(false);
            Debug.LogWarning("[ShowMyCard] 至少需要保留6张数字卡");
            return;
        }

        // 执行删除
        PlayerCardInventory.Instance.numberCards.Remove(card);
        GameManager.Instance.AddPoints(-cost);
        ShopManager.Instance.totalRemovedNumberCards++;
        CardManager.Instance.SyncDeckFromInventory();

        // 刷新
        UIManager.Instance.UpdatePointsDisplay(GameManager.Instance.currentPoints);
        UIManager.Instance.confirmationPanel.SetActive(false);
        RefreshAllCards();

        Debug.Log($"[ShowMyCard] 删除数字卡成功: {card.cardData.cardName}");
    }

    /// <summary>
    /// 显示公式卡删除确认
    /// </summary>
    void ShowDeleteConfirmation_Formula(FormulaCardData card)
    {
        // 计算删除成本（公式卡价格的50%）
        BigInteger cost = (BigInteger)(card.CardPrice * 0.5f);
        string message = $"确认删除 [{card.Name}] 吗？\n消耗点数: {cost}";

        if (UIManager.Instance.confirmationPanel != null)
        {
            UIManager.Instance.confirmationPanel.SetActive(true);

            Text confirmText = UIManager.Instance.confirmationPanel.GetComponentInChildren<Text>();
            if (confirmText != null)
                confirmText.text = message;

            Button[] buttons = UIManager.Instance.confirmationPanel.GetComponentsInChildren<Button>();
            if (buttons.Length >= 2)
            {
                buttons[0].onClick.RemoveAllListeners();
                buttons[1].onClick.RemoveAllListeners();

                buttons[0].onClick.AddListener(() => ExecuteRemoveFormulaCard(card, cost));
                buttons[1].onClick.AddListener(() => UIManager.Instance.confirmationPanel.SetActive(false));
            }
        }
    }
    /// <summary>
    /// 执行删除公式卡
    /// </summary>
    void ExecuteRemoveFormulaCard(FormulaCardData card, BigInteger cost)
    {
        if (GameManager.Instance.currentPoints < cost)
        {
            UIManager.Instance.confirmationPanel.SetActive(false);
            Debug.LogWarning("[ShowMyCard] 点数不足");
            return;
        }

        if (PlayerCardInventory.Instance.formulaCards.Count <= 1)
        {
            UIManager.Instance.confirmationPanel.SetActive(false);
            Debug.LogWarning("[ShowMyCard] 至少需要保留1张公式卡");
            return;
        }

        // 执行删除
        PlayerCardInventory.Instance.formulaCards.Remove(card);
        GameManager.Instance.AddPoints(-cost);
        CardManager.Instance.SyncDeckFromInventory();

        // 刷新
        UIManager.Instance.UpdatePointsDisplay(GameManager.Instance.currentPoints);
        UIManager.Instance.confirmationPanel.SetActive(false);
        RefreshAllCards();

        Debug.Log($"[ShowMyCard] 删除公式卡成功: {card.Name}");
    }
    /// <summary>
    /// 更新删除模式UI
    /// </summary>
    void UpdateDeleteModeUI()
    {
        if (!isDeleteMode)
        {
            // 非删除模式，隐藏删除成本显示
            if (deleteCostText != null)
                deleteCostText.gameObject.SetActive(false);
            return;
        }

        // 显示删除成本
        if (deleteCostText != null)
        {
            BigInteger cost = ShopManager.Instance.CalculateNumberRemoveCost();
            deleteCostText.text = $"删除卡牌消耗: {cost} 点";
            deleteCostText.gameObject.SetActive(true);

            // 根据点数情况改变颜色
            if (GameManager.Instance.currentPoints >= cost)
            {
                deleteCostText.color = Color.green;
            }
            else
            {
                deleteCostText.color = Color.red;
            }
        }
    }
    /// <summary>
    /// 进入删除模式（在商店中调用）
    /// </summary>
    public void EnterDeleteMode()
    {
        Debug.Log("[ShowMyCard] 进入删除模式");
        isDeleteMode = true;
        RefreshAllCards();
        UpdateDeleteModeUI();
    }

    /// <summary>
    /// 退出删除模式
    /// </summary>
    public void ExitDeleteMode()
    {
        Debug.Log("[ShowMyCard] 退出删除模式");
        isDeleteMode = false;
        UpdateDeleteModeUI();
    }
    /// <summary>
    /// 通用方法：设置文本内容和颜色
    /// </summary>
    void SetPartDisplay(Text textComp, NumberComponent component, int currentValue)
    {
        if (textComp == null || component == null) return;

        if (component.isDice)
        {
            // 骰子显示：~面数~ (黄色)
            textComp.text = $"~{component.diceSides}~";
            textComp.color = diceColor;
        }
        else if (component.isIncremental)
        {
            // 递增显示：{当前值} (绿色) - 这里使用了实例里的 currentValue
            textComp.text = $"{{{currentValue}}}";
            textComp.color = incrementalColor;
        }
        else
        {
            // 普通显示：数值 (黑色)
            textComp.text = currentValue.ToString();
            textComp.color = normalColor;
        }
    }

    void GenerateFormulaCards()
    {
        var deck = CardManager.Instance.formulaCardDeck;
        var prefab = UIManager.Instance.formulaCardPrefab;

        if (deck == null || prefab == null) return;

        foreach (var data in deck)
        {
            GameObject go = Instantiate(prefab, contentRoot);
            go.transform.localScale = UnityEngine.Vector3.one * cardScale;

            go.SetActive(true);

            var view = go.GetComponent<FormulaCardUI>();
            if (view != null) view.Bind(data);
        }
    }
}