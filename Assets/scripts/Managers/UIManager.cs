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
    [Header("祝福卡牌预制体")] 
    public GameObject blessingCardPrefab;

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
    public GameObject confirmationPanel;// 确认对话框

    [Header("游戏信息显示")]
    public Text pointsText;              // 总分数
    public Text roundText;               // 当前回合
    public Text stageRequirementText;    // 阶段要求点数
    public Text targetRoundText;        // 显示目标回合的文本组件

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
    public Transform shopBlessArea;

    [Header("商店卡牌Prefab")]
    public GameObject shopNumberCardPrefab; // 数字卡商店槽位Prefab（带ShopNumberCardSlot组件）
    public GameObject shopFormulaCardPrefab; // 公式卡商店槽位Prefab（带ShopFormulaCardSlot组件）
    public GameObject shopBlessingCardPrefab; // 祝福卡商店槽位Prefab（带ShopBlessCardSlot组件）

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
    #region 展示卡牌库
    // 打开数字卡库
    public void OpenNumberCardDeck()
    {
        // 只打开面板，不关闭原来的界面（Shop或Game），实现“不影响进度”
        myNumberCardPanel.SetActive(true);
        myFormulaCardPanel.SetActive(false); // 互斥显示
        myBlessPanel.SetActive(false);
        myNumberCardPanel.transform.SetAsLastSibling(); // 确保数字卡库在其他面板之上显示

        if (myCardButton != null)
        {
            myCardButton.SetActive(true);
            myCardButton.transform.SetAsLastSibling();
        }
        // 调用刷新逻辑 (需要 ShowMyCard 挂在面板上)
        var showScript = myNumberCardPanel.GetComponent<ShowMyNumberCard>();
        if (showScript != null) showScript.RefreshAllCards();
    }

    // 打开公式卡库
    public void OpenFormulaCardDeck()
    {
        myFormulaCardPanel.SetActive(true);
        myNumberCardPanel.SetActive(false);
        myBlessPanel.SetActive(false);
        myFormulaCardPanel.transform.SetAsLastSibling(); // 确保公式卡库在数字卡库之上显示

        if (myCardButton != null)
        {
            myCardButton.SetActive(true);
            myCardButton.transform.SetAsLastSibling();
        }

        var showScript = myFormulaCardPanel.GetComponent<ShowMyFormula>();
        if (showScript != null) showScript.RefreshAllCards();
    }
    public void OpenBlessCardDeck()
    {
        myBlessPanel.SetActive(true);
        myNumberCardPanel.SetActive(false);
        myFormulaCardPanel.SetActive(false);
        myBlessPanel.transform.SetAsLastSibling(); // 确保祝福卡库在其他卡库之上显示

        if (myCardButton != null)
        {
            myCardButton.SetActive(true);
            myCardButton.transform.SetAsLastSibling();
        }

        var showScript = myBlessPanel.GetComponent<ShowMyBlessings>();
        if (showScript != null) showScript.RefreshAllBlessings();
    }

    // 关闭卡牌库（返回原来的界面）
    public void CloseCardDeck()
    {
        myNumberCardPanel.SetActive(false);
        myFormulaCardPanel.SetActive(false);
        myBlessPanel.SetActive(false);
        if(GameManager.Instance.currentState == GameManager.GameState.Shop)
            myCardButton.SetActive(false) ;
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
            multiplierText.text = "×1";
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
    /// 格式化大数字（≤100亿千分位，>100亿科学计数法，四位有效数字）
    /// </summary>
    string FormatBigNumber(System.Numerics.BigInteger number)
    {
        System.Numerics.BigInteger threshold = 10000000000;

        if (System.Numerics.BigInteger.Abs(number) > threshold)
        {
            // 保留4位有效数字
           string numStr = number.ToString();
           bool isNegative = numStr.StartsWith("-");
           if (isNegative) numStr = numStr.Substring(1);

            int len = numStr.Length;
            string digits = numStr.Substring(0, System.Math.Min(4, len)); // 取前4位

           string decimalPart = digits[0] + "." + digits.Substring(1);
            int exponent = len - 1;

            string result = $"{decimalPart}e{exponent}";
            return isNegative ? "-" + result : result;
        }

        // 100亿以内正常显示
        return ((long)number).ToString("N0");
    }

    /*
    //四舍五入的科学计数法（留着看看以后需不需要）
    /// <summary>
    /// 格式化大数字显示（≤100亿用千分位，>100亿用科学计数法，四位有效数字+小写e）
    /// </summary>
    string FormatBigNumber(System.Numerics.BigInteger number)
    {
        // 阈值：100亿
        System.Numerics.BigInteger threshold = 10000000000;

        // 大于100亿 → 科学计数法，四位有效数字 + 小写 e
        if (System.Numerics.BigInteger.Abs(number) > threshold)
        {
            double numDouble = (double)number;
            return numDouble.ToString("0.000e0");
        }

        // 小于等于100亿 → 直接千分位（一定在 long 范围内）
        return ((long)number).ToString("N0");
    }
    */



    /*
    //正常版本
    /// <summary>
    /// 格式化大数字显示（例如：1,234,567）
    /// </summary>
    string FormatBigNumber(System.Numerics.BigInteger number)
    {
        // 如果数字在 long 范围内，直接用内置格式化
        if (number <= long.MaxValue && number >= long.MinValue)
        {
            return ((long)number).ToString("N0");  // N0 格式自动添加千位分隔符
        }

        // 如果超过 long 范围，手动添加逗号
        string numberStr = number.ToString();

        // 处理负号
        bool isNegative = number < 0;
        if (isNegative)
        {
            numberStr = numberStr.Substring(1);  // 移除负号
        }

        // 从右往左插入逗号（每3位）
        var result = new System.Text.StringBuilder();
        int digitCount = numberStr.Length;

        for (int i = 0; i < digitCount; i++)
        {
            // 从右往左数，每3位插入一个逗号
            if (i > 0 && (digitCount - i) % 3 == 0)
            {
                result.Append(',');
            }
            result.Append(numberStr[i]);
        }

        // 添加负号（如果有）
        if (isNegative)
        {
            return "-" + result.ToString();
        }

        return result.ToString();

    }
    */
    #endregion

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
                //Debug.Log($"正在绑定手牌数据：{cardData.cardData.cardName}");
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
        ClearArea(shopBlessArea);

        // 2. 生成数字卡槽位
        var numItems = ShopManager.Instance.shopNumberCards;
        for (int i = 0; i < numItems.Count; i++)
        {
            GameObject slotGo = Instantiate(shopNumberCardPrefab, shopNumberArea);

            // 强制设置Transform
            slotGo.transform.localScale = UnityEngine.Vector3.one;
            slotGo.transform.localPosition = UnityEngine.Vector3.zero;
            slotGo.transform.localRotation = UnityEngine.Quaternion.identity;

            // 强制激活物体
            slotGo.SetActive(true);

            //禁用LayoutGroup的自动调整
            HorizontalLayoutGroup hlg = slotGo.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null) hlg.enabled = false;
            VerticalLayoutGroup vlg = slotGo.GetComponent<VerticalLayoutGroup>();
            if (vlg != null) vlg.enabled = false;

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

        // 生成公式卡槽位
        var formulaItems = ShopManager.Instance.shopFormulaCards;
        for (int i = 0; i < formulaItems.Count; i++)
        {
            GameObject slotGo = Instantiate(shopFormulaCardPrefab, shopFormulaArea);

            // 强制设置Transform
            slotGo.transform.localScale = UnityEngine.Vector3.one;
            slotGo.transform.localPosition = UnityEngine.Vector3.zero;
            slotGo.transform.localRotation = UnityEngine.Quaternion.identity;

            // 强制激活物体
            slotGo.SetActive(true);

            // 禁用LayoutGroup的自动调整
            HorizontalLayoutGroup hlg = slotGo.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null) hlg.enabled = false;
            VerticalLayoutGroup vlg = slotGo.GetComponent<VerticalLayoutGroup>();
            if (vlg != null) vlg.enabled = false;

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
        // 4. 生成祝福卡槽位
        var blessingItems = ShopManager.Instance.shopBlessings;

        for (int i = 0; i < blessingItems.Count; i++)
        {
            GameObject slotGo = Instantiate(shopBlessingCardPrefab, shopBlessArea);

            //强制设置Transform
            slotGo.transform.localScale = UnityEngine.Vector3.one;
            slotGo.transform.localPosition = UnityEngine.Vector3.zero;
            slotGo.transform.localRotation = UnityEngine.Quaternion.identity;

            // 强制激活物体
            slotGo.SetActive(true);

            // 禁用LayoutGroup的自动调整
            HorizontalLayoutGroup hlg = slotGo.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null) hlg.enabled = false;
            VerticalLayoutGroup vlg = slotGo.GetComponent<VerticalLayoutGroup>();
            if (vlg != null) vlg.enabled = false;

            var slotUI = slotGo.GetComponent<BlessingInShop>();

            if (slotUI != null)
            {
                // 绑定祝福卡数据
                slotUI.BindBlessing(blessingItems[i], i);

            }
            else
            {
                Debug.LogError("shopBlessingCardPrefab 缺少 BlessingInShop 组件！");
            }
        }

        // 5. 强制刷新布局
        if (shopNumberArea != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)shopNumberArea);
        }
        if (shopFormulaArea != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)shopFormulaArea);
        }
        if (shopBlessArea != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)shopBlessArea);
        }

    }

    #endregion

    /// <summary>
    /// 刷新选中卡牌的UI显示（结算时投骰子后调用）
    /// 用于显示投掷骰子和递增+1后的新值
    /// </summary>
    public void RefreshSelectedCardsDisplay(List<NumberCardInstance> selectedCards)
    {
        if (selectedCards == null || selectedCards.Count == 0)
        {
            Debug.LogWarning("[UIManager] 选中卡牌列表为空，无法刷新");
            return;
        }

        //Debug.Log($"[UIManager] 开始刷新 {selectedCards.Count} 张选中卡牌的显示");

        //// 刷新handArea中的NumberCardView
        //if (handArea != null)
        //{
        //    var views = handArea.GetComponentsInChildren<NumberCardView>();

        //    for (int i = 0; i < views.Length && i < selectedCards.Count; i++)
        //    {
        //        if (views[i] != null && selectedCards[i] != null)
        //        {
        //            views[i].Bind(selectedCards[i]);
        //            Debug.Log($"[UIManager] 刷新手牌显示 {i}: {selectedCards[i].cardData.cardName}");
        //        }
        //    }
        //}
        var allSingleViews = FindObjectsOfType<SingleNumberView>(true);
        var allCompositeViews = FindObjectsOfType<CompositeNumberView>(true);

        // 刷新单数字卡
        int singleRefreshCount = 0;
        foreach (var view in allSingleViews)
        {
            if (view.boundInstance != null && selectedCards.Contains(view.boundInstance))
            {
                view.BindInstance(view.boundInstance);
                singleRefreshCount++;
            }
        }
        int compositeRefreshCount = 0;
        // 刷新双数字卡 (加法、乘法、复合卡)
        foreach (var view in allCompositeViews)
        {
            if (view.boundInstance != null && selectedCards.Contains(view.boundInstance))
            {
                view.BindInstance(view.boundInstance);
                compositeRefreshCount++;
            }
        }
        Debug.Log("[UIManager] 选中卡牌显示已刷新");
    }
    #region 工具方法
    void ClearArea(Transform area)
    {
        if (area == null) return;

        foreach (Transform child in area)
            Destroy(child.gameObject);
    }
    #endregion
    
    #region 许愿币祝福选择（许愿币专用）
    /// <summary>
    /// 打开许愿币祝福选择界面（只显示可叠加的已拥有祝福）
    /// </summary>
    public void OpenWishCoinBlessSelection()
    {
        // 直接打开你现有的祝福面板
        myBlessPanel.SetActive(true);
        myBlessPanel.transform.SetAsLastSibling();

        // 调用专属刷新（只显示可叠加祝福）
        ShowMyBlessings showScript = myBlessPanel.GetComponent<ShowMyBlessings>();
        if (showScript != null)
        {
            showScript.ShowOnlyStackableOwnedBlessings();
        }
    }

    /// <summary>
    /// 关闭许愿币选择，恢复正常祝福面板显示
    /// </summary>
    public void CloseWishCoinBlessSelection()
    {
        myBlessPanel.SetActive(false);

        // 恢复正常祝福显示
        ShowMyBlessings showScript = myBlessPanel.GetComponent<ShowMyBlessings>();
        if (showScript != null)
        {
            showScript.ClearTempWishCoinButtons();
            showScript.RefreshAllBlessings();
        }
    }
    #endregion
}