using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.UI;

public enum NumberCardLayoutType
{
    Single,        // a
    Add_AB,        // a + b
    Multiply_AB,   // a × b
    Composite_AB,
}

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("数字卡 UI 库")]
    public NumberCardUIFactory numberCardLibrary;
    [Header("填空卡预制体")]
    public GameObject formulaCardPrefab;

    [Header("UI 面板引用")]
    public GameObject startMenuPanel; // 在 Inspector 中拖入主菜单面板
    public GameObject gameUIPanel; // 游戏内UI面板
    public GameObject gameOverPanel; // 游戏结束面板
    public GameObject shopPanel; // 商店面板
    public GameObject pointstagePanel; // 点数阶段面板
    public GameObject myCardButton;// 我的卡牌按钮面板
    public GameObject myNumberCardPanel;// 数字卡
    public GameObject myFormulaCardPanel;// 公式卡
    public GameObject myBlessPanel;// 祝福

    [Header("游戏信息显示")]
    public Text pointsText;              // 总分数
    public Text roundText;               // 当前回合
    public Text stageRequirementText;    // 阶段要求点数
    public Text targetRoundText; // 拖入用于显示 "目标回合: X" 的文本组件

    [Header("点数获得提示")]
    public Text pointsGainText; // 显示获得的点数数值
    public float pointsGainDisplayTime = 2f; // 显示时长
    public Text multiplierText;          // 倍率显示
    public Button calculateButton;       // 结算按钮

    [Header("手牌和公式区域")]
    public Transform handArea;//手牌区域
    public Transform formulaArea;//填空卡区域

    [Header("商店卡牌显示区域")]
    public Transform shopNumberArea;
    public Transform shopFormulaArea;

    [Header("商店卡牌Prefab")]
    public GameObject shopNumberCardPrefab; // 数字卡商店槽位Prefab（带ShopNumberCardSlot组件）
    public GameObject shopFormulaCardPrefab; // 公式卡商店槽位Prefab（带ShopFormulaCardSlot组件）

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #region 面板切换
    public void ShowPanel(GameObject panelToShow)
    {
        // 隐藏所有面板
        if (startMenuPanel != null) startMenuPanel.SetActive(false);
        if (gameUIPanel != null) gameUIPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
        if (myCardButton != null) myCardButton.SetActive(false);
        if (myNumberCardPanel != null) myNumberCardPanel.SetActive(false);
        if (myFormulaCardPanel != null) myFormulaCardPanel.SetActive(false);
        if (myBlessPanel != null) myBlessPanel.SetActive(false);

        // 激活目标
        panelToShow.SetActive(true);
        // 强制把这个面板排到最前面
        panelToShow.transform.SetAsLastSibling();

        if (panelToShow == gameUIPanel || panelToShow == shopPanel)
        {
            if (pointstagePanel != null)
            {
                pointstagePanel.SetActive(true);
                pointstagePanel.transform.SetAsLastSibling();
                Debug.Log($"已激活pointstagePanel");
            }
        }
        if (panelToShow == gameUIPanel || panelToShow == myBlessPanel || panelToShow == myNumberCardPanel || panelToShow == myFormulaCardPanel)
        {
            if(myCardButton != null)
            {
                myCardButton.SetActive(true);
                myCardButton.transform.SetAsLastSibling();
            }
        }

        Debug.Log($"已激活并置顶面板: {panelToShow.name}");
    }
    // --- 1. 卡牌库显示逻辑 (弹窗模式) ---

    // 打开数字卡库
    public void OpenNumberCardDeck()
    {
        // 只打开面板，不关闭原来的界面（Shop或Game），实现“不影响进度”
        myNumberCardPanel.SetActive(true);
        myFormulaCardPanel.SetActive(false); // 互斥显示
        myBlessPanel.SetActive(false);
        myNumberCardPanel.transform.SetAsLastSibling(); // 确保数字卡库在其他面板之上显示

        // 调用刷新逻辑 (需要 ShowMyCard 挂在面板上)
        var showScript = myNumberCardPanel.GetComponent<ShowMyCard>();
        if (showScript != null) showScript.RefreshAllCards();
    }

    // 打开公式卡库
    public void OpenFormulaCardDeck()
    {
        myFormulaCardPanel.SetActive(true);
        myNumberCardPanel.SetActive(false);
        myBlessPanel.SetActive(false);
        myFormulaCardPanel.transform.SetAsLastSibling(); // 确保公式卡库在数字卡库之上显示

        var showScript = myFormulaCardPanel.GetComponent<ShowMyCard>();
        if (showScript != null) showScript.RefreshAllCards();
    }
    public void OpenBlessCardDeck()
    {
        myBlessPanel.SetActive(true);
        myNumberCardPanel.SetActive(false);
        myFormulaCardPanel.SetActive(false);
        myBlessPanel.transform.SetAsLastSibling(); // 确保祝福卡库在其他卡库之上显示

        var showScript = myBlessPanel.GetComponent<ShowMyCard>();
        if (showScript != null) showScript.RefreshAllCards();
    }

    // 关闭卡牌库（返回原来的界面）
    public void CloseCardDeck()
    {
        myNumberCardPanel.SetActive(false);
        myFormulaCardPanel.SetActive(false);
    }
    #endregion

    #region 游戏信息更新
    /// <summary>
    /// 更新点数显示
    /// </summary>
    public void UpdatePointsDisplay(System.Numerics.BigInteger points)
    {
        if (pointsText != null)
        {
            pointsText.text = $"{FormatBigNumber(points)}";
        }
    }

    /// <summary>
    /// 更新回合数显示
    /// </summary>
    public void UpdateRoundDisplay(int round)
    {
        if (roundText != null)
        {
            roundText.text = $"{round}";
        }
    }


    /// <summary>
    /// 更新阶段要求点数显示
    /// </summary>
    public void UpdateStageRequirementDisplay(System.Numerics.BigInteger requirement)
    {
        if (stageRequirementText != null)
        {
            stageRequirementText.text = $"{FormatBigNumber(requirement)}";
        }
    }
    /// <summary>
    /// 更新目标检查回合显示
    /// </summary>
    public void UpdateTargetRoundDisplay(int targetRound)
    {
        if (targetRoundText != null)
        {
            targetRoundText.text = $"{targetRound}"; 
        }
    }
    /// <summary>
    /// 更新倍率显示
    /// </summary>
    public void UpdateMultiplier(float multiplier)
    {
        if (multiplierText != null)
        {
            multiplierText.text = $"×{multiplier:F1}";

            // 倍率大于1时高亮
            if (multiplier > 1.0f)
            {
                multiplierText.color = Color.yellow;
            }
            else
            {
                multiplierText.color = Color.black;
            }
        }
    }
    /// <summary>
    /// 重置本回合得分和倍率（回合开始时调用）
    /// </summary>
    public void ResetRoundScore()
    {
        if (pointsGainText != null)
        {
            pointsGainText.text = "0";
        }

        if (multiplierText != null)
        {
            multiplierText.text = "×1.0";
            multiplierText.color = Color.black;
        }
    }

    /// <summary>
    /// 显示获得的点数
    /// </summary>
    public void ShowPointsGain(System.Numerics.BigInteger gainedPoints)
    {
        if (pointsGainText == null)
        {
            Debug.LogWarning("pointsGainPanel未设置！");
            return;
        }

        // 设置文本
        pointsGainText.text = $"+{FormatBigNumber(gainedPoints)}";

    }

    /// <summary>
    /// 刷新所有游戏UI（点数、回合、阶段要求等）
    /// </summary>
    public void RefreshAllGameInfo()
    {
        UpdatePointsDisplay(GameManager.Instance.currentPoints);
        UpdateRoundDisplay(GameManager.Instance.currentRound);

        // 获取当前阶段的要求点数
        System.Numerics.BigInteger currentRequirement = GetCurrentStageRequirement();
        UpdateStageRequirementDisplay(currentRequirement);
    }

    /// <summary>
    /// 获取当前阶段的要求点数
    /// </summary>
    System.Numerics.BigInteger GetCurrentStageRequirement()
    {
        int currentRound = GameManager.Instance.currentRound;
        var stageRounds = GameManager.Instance.stageRounds;
        var requirements = GameManager.Instance.stagePointRequirements;

        // 找到当前回合对应的阶段要求
        for (int i = 0; i < stageRounds.Count; i++)
        {
            if (currentRound <= stageRounds[i])
            {
                return requirements[i];
            }
        }

        // 如果超过所有阶段，返回最终目标
        return GameManager.Instance.targetPoints;
    }

    /// <summary>
    /// 格式化大数字显示（例如：1,234,567 或 1.23M）
    /// </summary>
    string FormatBigNumber(System.Numerics.BigInteger number)
    {
            return $"{number}";
        
    }
    #endregion #region 结算按钮控制
    ///// <summary>
    ///// 检查是否可以结算（所有卡牌是否填入）
    ///// </summary>
    //public void CheckCanCalculate()
    //{
    //    if (CardManager.Instance == null || CardManager.Instance.currentFormulaCard == null)
    //    {
    //        SetCalculateButtonEnabled(false);
    //        return;
    //    }

    //    // 检查填入的卡牌数量是否满足要求
    //    int requiredCount = CardManager.Instance.currentFormulaCard.RequiredCount;
    //    int selectedCount = CardManager.Instance.selectedNumberCards.Count;

    //    bool canCalculate = (selectedCount == requiredCount);
    //    SetCalculateButtonEnabled(canCalculate);

    //    Debug.Log($"填入卡牌: {selectedCount}/{requiredCount}，可结算: {canCalculate}");
    //}

    ///// <summary>
    ///// 设置结算按钮可用状态
    ///// </summary>
    //public void SetCalculateButtonEnabled(bool enabled)
    //{
    //    if (calculateButton != null)
    //    {
    //        calculateButton.interactable = enabled;

    //        if (pointsGainText != null)
    //        {
    //            pointsGainText.text = enabled ? "结算" : "填入所有卡牌";
    //            pointsGainText.color = enabled ? Color.black : Color.blue;
    //        }
    //    }
    //}

    #region 手牌显示
    public void RefreshGameUI()
    {
        Debug.Log("开始刷新游戏界面卡牌...");

        // 1. 显示公式卡
        ClearArea(formulaArea);

        if (CardManager.Instance.currentFormulaCard != null)
        {
            var go = Instantiate(formulaCardPrefab, formulaArea);
            go.transform.localScale = UnityEngine.Vector3.one;
            var formulaUI = go.GetComponent<FormulaCardUI>();
            formulaUI.Bind(CardManager.Instance.currentFormulaCard);
        }

        // 2. 显示数字手牌
        foreach (Transform child in handArea) { Destroy(child.gameObject); }
        Debug.Log($"当前手牌数量: {CardManager.Instance.currentNumberCards.Count}");

        // 生成新手牌
        foreach (var cardData in CardManager.Instance.currentNumberCards)
        {
            GameObject prefab = numberCardLibrary.GetPrefab(cardData.cardData.layoutType);
            if (prefab == null)
            {
                Debug.LogError($"找不到布局类型为 {cardData.cardData.layoutType} 的 Prefab！");
                continue;
            }

            GameObject cardGo = Instantiate(prefab, handArea);
            cardGo.transform.localPosition = UnityEngine.Vector3.zero;
            cardGo.transform.localScale = UnityEngine.Vector3.one;
            cardGo.transform.localRotation = UnityEngine.Quaternion.identity;
            cardGo.SetActive(true);

            // 视图绑定
            var view = cardGo.GetComponent<NumberCardLayoutView>();
            if (view != null)
            {
                Debug.Log($"正在绑定手牌数据：{cardData.cardData.cardName}");
                view.Bind(cardData.cardData);
            }
            else
            {
                MonoBehaviour[] components = cardGo.GetComponentsInChildren<MonoBehaviour>();
                string names = "";
                foreach (var c in components) names += c.GetType().Name + ", ";
                Debug.LogWarning($"[警告] {cardGo.name} 缺少接口！现有脚本: {names}");
            }

            // 逻辑绑定
            var controller = cardGo.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.Bind(cardData);
            }
            else
            {
                Debug.LogWarning($"[警告] {cardGo.name} 缺少 PlayerController 组件！");
            }
        }

        // 强制刷新布局
        LayoutRebuilder.ForceRebuildLayoutImmediate(handArea.GetComponent<RectTransform>());

        // 刷新游戏信息
        RefreshAllGameInfo();

        // 重置本回合得分
        ResetRoundScore();

        //// 禁用结算按钮
        //SetCalculateButtonEnabled(false);
    }

    public void ShowHandCards(List<NumberCardInstance> handCards)
    {
        if (handArea == null)
        {
            Debug.LogError("UIManager: HandArea 槽位未赋值！");
            return;
        }
        if (numberCardLibrary == null)
        {
            Debug.LogError("UIManager: numberCardLibrary 槽位未赋值！");
            return;
        }

        ClearArea(handArea);

        foreach (var card in handCards)
        {
            GameObject prefab = numberCardLibrary.GetPrefab(card.cardData.layoutType);

            GameObject go = Instantiate(prefab, handArea);

            if (prefab == null)
            {
                Debug.LogError($"【UIManager报错】工厂里没配置 {card.cardData.layoutType} 类型的预制体！");
                continue;
            }
            // 1. 强制重置缩放为 1 (防止被LayoutGroup压缩成0，或者继承了错误的缩放)
            go.transform.localScale = UnityEngine.Vector3.one;

            // 2. 强制归零局部坐标 (特别是 Z 轴，必须为 0 才能在 UI 上显示)
            go.transform.localPosition = UnityEngine.Vector3.zero;

            // 3. 重置旋转
            go.transform.localRotation = UnityEngine.Quaternion.identity;

            // 4. 确保物体是激活的
            go.SetActive(true);

            if (go.TryGetComponent<NumberCardLayoutView>(out var view))
            {
                view.Bind(card.cardData);
            }
            else
            {
                Debug.LogError($"Prefab {go.name} 缺少 NumberCardLayoutView 脚本！");
            }

            if (go.TryGetComponent<PlayerController>(out var pc))
            {
                pc.Bind(card);
            }
            else
            {
                Debug.LogError($"Prefab {go.name} 缺少 PlayerController 脚本！");
            }
        }
    }

    public void ShowFormulaCard(FormulaCardData formula)
    {
        ClearArea(formulaArea);
        var go = Instantiate(formulaCardPrefab, formulaArea);
        go.GetComponent<FormulaCardUI>().Bind(formula);
    }
    #endregion

    #region 商店UI
    /// <summary>
    /// 刷新商店UI（使用独立的数字卡和公式卡Slot组件）
    /// </summary>
    public void RefreshShopUI()
    {
        // 1. 清理旧槽位
        ClearArea(shopNumberArea);
        ClearArea(shopFormulaArea);

        // 2. 生成数字卡槽位
        var numItems = ShopManager.Instance.shopNumberCards;
        for (int i = 0; i < numItems.Count; i++)
        {
            GameObject slotGo = Instantiate(shopNumberCardPrefab, shopNumberArea);
            var slotUI = slotGo.GetComponent<ShopNumberCardSlot>();

            if (slotUI != null)
            {
                slotUI.BindNumberCard(numItems[i], i);
            }
            else
            {
                Debug.LogError("shopNumberCardPrefab 缺少 ShopNumberCardSlot 组件！");
            }
        }

        // 3. 生成公式卡槽位
        var formulaItems = ShopManager.Instance.shopFormulaCards;
        for (int i = 0; i < formulaItems.Count; i++)
        {
            GameObject slotGo = Instantiate(shopFormulaCardPrefab, shopFormulaArea);
            var slotUI = slotGo.GetComponent<ShopFormulaCardSlot>();

            if (slotUI != null)
            {
                slotUI.BindFormulaCard(formulaItems[i], i);
            }
            else
            {
                Debug.LogError("shopFormulaCardPrefab 缺少 ShopFormulaCardSlot 组件！");
            }
        }

        Debug.Log($"商店UI刷新完成：数字卡{numItems.Count}个，公式卡{formulaItems.Count}个");
    }
    #endregion

    #region 工具方法
    void ClearArea(Transform area)
    {
        if (area == null) return;

        foreach (Transform child in area)
            Destroy(child.gameObject);
    }
    #endregion
}