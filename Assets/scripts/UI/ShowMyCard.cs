using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 显示我的卡牌 - 最终修复版
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

    [Header("显示设置")]
    public float cardScale = 1.0f;

    // 删除模式状态
    private bool isDeleteMode = false;
    private Dictionary<NumberCardInstance, GameObject> cardGameObjects = new Dictionary<NumberCardInstance, GameObject>();

    [Header("颜色配置")]
    public Color incrementalColor = Color.green;   // 递增数字：绿色
    public Color diceColor = Color.red; // 骰子数字：红色
    public Color normalColor = Color.black;        // 普通数字：黑色

    [Header("删除功能配置")]
    public GameObject deleteButtonPrefab;           // 删除按钮预制体
    public GameObject deleteConfirmPanelPrefab;    // 确认对话框预制体

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
                    scrollRect.horizontal = false;
                    scrollRect.vertical = true;
                    scrollRect.movementType = ScrollRect.MovementType.Elastic;
                    scrollRect.elasticity = 0.1f;
                    scrollRect.scrollSensitivity = 1;

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

        // 配置 contentRoot 的 LayoutElement
        if (contentRoot != null)
        {
            LayoutElement le = contentRoot.GetComponent<LayoutElement>();
            if (le == null)
            {
                le = contentRoot.gameObject.AddComponent<LayoutElement>();
            }

            le.preferredWidth = -1;
            le.flexibleHeight = 0;

            Debug.Log("[ShowMyCard] 配置了 LayoutElement");
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
        if (showNumberCards)
        {
            GenerateNumberCards();
        }
        if (showFormulaCards)
        {
            GenerateFormulaCards();
        }
        if (scrollRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.GetComponent<RectTransform>());
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
                    deleteButton.onClick.AddListener(() => OnCardDeleteClick(instance, go));
                }
            }

            cardGameObjects[instance] = go;
        }
    }
    /// <summary>
    /// 卡牌删除按钮点击事件
    /// </summary>
    void OnCardDeleteClick(NumberCardInstance card, GameObject cardGo)
    {
        if (!isDeleteMode)
        {
            Debug.LogWarning("[ShowMyCard] 不在删除模式");
            return;
        }

        if (card == null)
        {
            Debug.LogWarning("[ShowMyCard] 卡牌为空");
            return;
        }

        // 显示确认对话框
        ShowDeleteConfirmation(card, cardGo);
    }

    /// <summary>
    /// 显示删除确认对话框（使用现有的confirmationPanel）
    /// </summary>
    void ShowDeleteConfirmation(NumberCardInstance card, GameObject cardGo)
    {
        // 计算删除成本
        BigInteger cost = ShopManager.Instance.CalculateNumberRemoveCost();

        string message = $"确认删除 [{card.cardData.cardName}] 吗？\n消耗点数: {cost}";

        // 使用现有的确认面板
        if (UIManager.Instance.confirmationPanel != null)
        {
            UIManager.Instance.confirmationPanel.SetActive(true);

            // 找到面板上的文本和按钮
            Text confirmText = UIManager.Instance.confirmationPanel.GetComponentInChildren<Text>();
            if (confirmText != null)
            {
                confirmText.text = message;
            }

            Button[] buttons = UIManager.Instance.confirmationPanel.GetComponentsInChildren<Button>();
            if (buttons.Length >= 2)
            {
                // 清除旧监听器
                buttons[0].onClick.RemoveAllListeners();
                buttons[1].onClick.RemoveAllListeners();

                // 确认按钮
                buttons[0].onClick.AddListener(() => ExecuteRemoveCard(card, cost, cardGo));

                // 取消按钮
                buttons[1].onClick.AddListener(() => UIManager.Instance.confirmationPanel.SetActive(false));
            }
        }
        else
        {
            Debug.LogError("[ShowMyCard] confirmationPanel 未配置");
        }
    }

    /// <summary>
    /// 执行删除卡牌
    /// </summary>
    void ExecuteRemoveCard(NumberCardInstance card, BigInteger cost, GameObject cardGo)
    {
        // 检查点数
        if (GameManager.Instance.currentPoints < cost)
        {
            UIManager.Instance.confirmationPanel.SetActive(false);
            Debug.LogWarning("[ShowMyCard] 点数不足，无法删除");
            return;
        }

        // 检查最少保留数量
        int minKeep = 6;
        if (PlayerCardInventory.Instance.numberCards.Count <= minKeep)
        {
            UIManager.Instance.confirmationPanel.SetActive(false);
            Debug.LogWarning($"[ShowMyCard] 至少需要保留 {minKeep} 张数字卡");
            return;
        }

        // 执行删除：从库存中移除
        PlayerCardInventory.Instance.numberCards.Remove(card);

        // 扣除点数
        GameManager.Instance.AddPoints(-cost);

        // 增加删除计数（用于成本计算）
        ShopManager.Instance.totalRemovedNumberCards++;

        // 同步 CardManager
        CardManager.Instance.SyncDeckFromInventory();

        // 刷新UI
        UIManager.Instance.UpdatePointsDisplay(GameManager.Instance.currentPoints);
        UIManager.Instance.confirmationPanel.SetActive(false);

        // 刷新卡牌显示
        RefreshAllCards();

        Debug.Log($"[ShowMyCard] 删除卡牌成功: {card.cardData.cardName}");
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