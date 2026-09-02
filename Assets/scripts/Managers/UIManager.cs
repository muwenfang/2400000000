using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
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
    public DataSavingManager dataSavingManager;

    [Header("数字卡 UI 库")]
    public NumberCardUIFactory numberCardLibrary;
    [Header("填空卡预制体")]
    public GameObject formulaCardPrefab;
    [Header("祝福卡牌预制体")] 
    public GameObject blessingCardPrefab;
    [Header("结算覆盖层预制体")]
    public GameObject scoreDisplayPrefab;
    [Header("结算卡面数字动画")]
    [Tooltip("骰子和递增数字在结算刷新时的放大动画总时长（秒）")]
    public float settlementValuePopDuration = 0.8f;
    [Tooltip("结算刷新时文本放大的倍率")]
    public float settlementValuePopScaleMultiplier = 2.5f;

    [Header("UI 面板引用")]
    public GameObject startMenuPanel; // 在 Inspector 中拖入主菜单面板
    public GameObject gameUIPanel; // 游戏内UI面板targetRoundText
    public GameObject gameOverPanel; // 游戏结束面板
    public GameObject loseGamePanel; // 失败面板
    public GameObject shopPanel; // 商店面板
    public GameObject pointstagePanel; // 点数阶段面板
    public GameObject myCardButton;// 我的卡牌按钮面板
    public GameObject myNumberCardPanel;// 数字卡
    public GameObject myFormulaCardPanel;// 公式卡
    public GameObject myBlessPanel;// 祝福
    public GameObject PlayerDataPanel;// 玩家数据面板
    public Image LockPanel; // 锁定界面
    public GameObject targetPointPanel; // 目标点数界面
    public GameObject settingPanel; // 设置界面
    public GameObject confirmationPanel;// 确认界面
    public GameObject StaffListPanel;// 制作人员界面

    //public GameObject confirmationPanel;// 确认对话框

    [Header("游戏信息显示")]
    public Text pointsText;              // 总分数
    public Text roundText;               // 当前回合
    public Text stageRequirementText;    // 阶段要求点数
    public Text targetRoundText;        // 显示目标回合的文本组件
    public Button courseButtonc;             // 教程按钮
    public Button courseButtons;             // 教程按钮

    [Header("祝福选择提示")]
    [Tooltip("带选择功能的祝福效果提示：在场景中拖入位于最顶层的 Text（如 Canvas 直属子物体），显示格式为 name:description")]
    public Text cardSelectionBlessingText;

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

    class CardScoreOverlayCache
    {
        public GameObject rootObject;
        public Text text;
    }

    readonly Dictionary<GameObject, CardScoreOverlayCache> cardScoreOverlayCache = new Dictionary<GameObject, CardScoreOverlayCache>();

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
        HideAllPanel();
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

                if(GameManager.isInvolutionMode)
                {
                    targetPointPanel.SetActive(false);
                }
                else
                {
                    targetPointPanel.SetActive(true);
                    targetPointPanel.transform.SetAsLastSibling();
                }
            }
            if(panelToShow == gameUIPanel && courseButtonc != null)
            {
                courseButtonc.gameObject.SetActive(true);
                courseButtonc.transform.SetAsLastSibling();
            }
            else if(panelToShow == shopPanel && courseButtons != null)
            {
                courseButtons.gameObject.SetActive(true);
                courseButtons.transform.SetAsLastSibling();
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
        if(panelToShow == startMenuPanel )
        {
            int accomplishTimes = dataSavingManager.GetAccomplishTimes();
            if (accomplishTimes > 0)
            {
                LockPanel.gameObject.SetActive(false);
            }
        }

    }
    public void HideAllPanel()
    {
        // 隐藏所有面板
        if (startMenuPanel != null) startMenuPanel.SetActive(false);
        if (gameUIPanel != null) gameUIPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (loseGamePanel != null) loseGamePanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
        if (myCardButton != null) myCardButton.SetActive(false);
        if (myNumberCardPanel != null) myNumberCardPanel.SetActive(false);
        if (myFormulaCardPanel != null) myFormulaCardPanel.SetActive(false);
        if (myBlessPanel != null) myBlessPanel.SetActive(false);
        if (PlayerDataPanel != null) PlayerDataPanel.SetActive(false);
        if (pointstagePanel != null) pointstagePanel.SetActive(false);
        if(settingPanel != null) settingPanel.SetActive(false);
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        if (StaffListPanel != null) StaffListPanel.SetActive(false);
    }
    #region 展示卡牌库
    // 打开数字卡库
    public void OpenNumberCardDeck()
    {
        myNumberCardPanel.SetActive(true);
        myFormulaCardPanel.SetActive(false);
        myBlessPanel.SetActive(false);
        myNumberCardPanel.transform.SetAsLastSibling();

         // 删卡模式下：切换回数字卡界面时刷新删卡价格显示
         if (ShopManager.Instance != null && ShopManager.Instance.isDeletionMode)
         {
             var numberCardView = myNumberCardPanel.GetComponent<ShowMyNumberCard>();
             if (numberCardView != null)
             {
                 numberCardView.RefreshDeletionCostDisplay();
             }
         }

        if (myCardButton != null)
        {
            myCardButton.SetActive(true);
            myCardButton.transform.SetAsLastSibling();
        }
    }

    // 打开公式卡库
    public void OpenFormulaCardDeck()
    {
        myFormulaCardPanel.SetActive(true);
        myNumberCardPanel.SetActive(false);
        myBlessPanel.SetActive(false);
        myFormulaCardPanel.transform.SetAsLastSibling(); // 确保公式卡库在数字卡库之上显示

         // 删卡模式下：切换回公式卡界面时刷新删卡价格显示
         if (ShopManager.Instance != null && ShopManager.Instance.isDeletionMode)
         {
             var formulaCardView = myFormulaCardPanel.GetComponent<ShowMyFormula>();
             if (formulaCardView != null)
             {
                 formulaCardView.RefreshDeletionCostDisplay();
             }
         }

        if (myCardButton != null)
        {
            myCardButton.SetActive(true);
            myCardButton.transform.SetAsLastSibling();
        }
    }

    private void HideDeletionCostPanelsForBlessView()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.ExitDeletionMode();
        }

        if (ShopManager.Instance != null && ShopManager.Instance.deleteCostPanel != null)
        {
            ShopManager.Instance.deleteCostPanel.SetActive(false);
        }

        var numberCardView = myNumberCardPanel != null ? myNumberCardPanel.GetComponent<ShowMyNumberCard>() : null;
        if (numberCardView != null && numberCardView.deletionCostPanel != null)
        {
            numberCardView.deletionCostPanel.SetActive(false);
        }

        var formulaCardView = myFormulaCardPanel != null ? myFormulaCardPanel.GetComponent<ShowMyFormula>() : null;
        if (formulaCardView != null && formulaCardView.deletionCostPanel != null)
        {
            formulaCardView.deletionCostPanel.SetActive(false);
        }
    }

    public void OpenBlessCardDeck()
    {
        myBlessPanel.SetActive(true);
        myNumberCardPanel.SetActive(false);
        myFormulaCardPanel.SetActive(false);

         // 删卡模式下：切到祝福界面不退出删卡模式，改为显示"无法在此界面删除"提示
         if (ShopManager.Instance != null && ShopManager.Instance.isDeletionMode)
         {
             var blessView = myBlessPanel.GetComponent<ShowMyBlessings>();
             if (blessView != null)
             {
                 blessView.SetDeletionUnavailableHintVisible(true);
             }
         }
         else
         {
             HideDeletionCostPanelsForBlessView();
         }

        myBlessPanel.transform.SetAsLastSibling(); // 确保祝福卡库在其他卡库之上显示

        if (myCardButton != null)
        {
            myCardButton.SetActive(true);
            myCardButton.transform.SetAsLastSibling();
        }
    }

    /// <summary>
    /// 打开删卡选择界面：复用场景中的卡牌库面板，只置顶和刷新，不关闭其它界面。
    /// </summary>
    public void OpenCardDeletionDeck()
    {
        // 打开并刷新数字卡面板
        if (myNumberCardPanel != null)
        {
            myNumberCardPanel.SetActive(true);

            var numberCardView = myNumberCardPanel.GetComponent<ShowMyNumberCard>();
            if (numberCardView != null)
            {
                numberCardView.RefreshAllCards();
            }
        }

        // 打开并刷新公式卡面板（删卡同样支持公式卡，可通过 MyCardButton 切换）
        if (myFormulaCardPanel != null)
        {
            myFormulaCardPanel.SetActive(true);

            var formulaCardView = myFormulaCardPanel.GetComponent<ShowMyFormula>();
            if (formulaCardView != null)
            {
                formulaCardView.RefreshAllCards();
            }
        }

        // 祝福卡不能通过删卡渠道删除，进入删卡模式时关闭祝福面板。
        if (myBlessPanel != null)
        {
            myBlessPanel.SetActive(false);
        }

        // 置顶 myNumberCard 界面（数字卡显示在公式卡之上）。
        if (myNumberCardPanel != null)
        {
            myNumberCardPanel.transform.SetAsLastSibling();
        }

        // MyCardButtonpanel 保留并置顶，用于切换数字卡/公式卡/祝福界面。
        if (myCardButton != null)
        {
            myCardButton.SetActive(true);
            myCardButton.transform.SetAsLastSibling();
        }
    }

    // 关闭卡牌库（返回原来的界面）
    public void CloseCardDeck()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.ExitDeletionMode();
        }

        myNumberCardPanel.SetActive(false);
        myFormulaCardPanel.SetActive(false);
        myBlessPanel.SetActive(false);
        if (CardSelectionManager.Instance.IsSelecting())
            CardSelectionManager.Instance.EndCardSelection();
        
        if(GameManager.Instance.currentState == GameManager.GameState.Shop)
            myCardButton.SetActive(false) ;
    }
    #endregion

    #region 游戏信息更新
    /// <summary>
    /// 更新现有点数显示
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
            stageRequirementText.text = $"$ {FormatBigNumber(requirement)}";
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
            pointsGainText.text = "结算";
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
        return NumberDisplayFormatter.Format(number);
    }
    
    public void ShowPlayerData()
    {
        ShowPanel(PlayerDataPanel);
    }
   
    public void ShowSettings()
    {
        ShowPanel(settingPanel);

        // 使用场景中已配置好的 DifficultySettingsManager 单例（根级 GameObject），
        // 而非在 settingPanel 上 AddComponent 创建无 Inspector 绑定的新实例。
        // settingPanel 和 DifficultySettingManager 在场景中是两个独立的 GameObject。
        if (DifficultySettingsManager.Instance != null)
        {
            DifficultySettingsManager.Instance.Initialize();
        }
    }
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
                view.BindInstance(cardData);
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

            if (cardGo.TryGetComponent<NumberCardLayoutView>(out var refreshedView))
            {
                refreshedView.BindInstance(cardData);
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

            if (prefab == null)
            {
                Debug.LogError($"【UIManager报错】工厂里没配置 {card.cardData.layoutType} 类型的预制体！");
                continue;
            }

            GameObject go = Instantiate(prefab, handArea);
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
                view.BindInstance(card);
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

            if (go.TryGetComponent<NumberCardLayoutView>(out var refreshedView))
            {
                refreshedView.BindInstance(card);
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

    #region 结算动态展示
    /// <summary>
    /// 刷新选中卡牌的UI显示（结算时投骰子后调用）
    /// 用于显示投掷骰子和递增+1后的新值
    /// </summary>
    public IEnumerator RefreshSelectedCardsDisplay(List<NumberCardInstance> selectedCards)
    {
        if (selectedCards == null || selectedCards.Count == 0)
        {
            Debug.LogWarning("[UIManager] 选中卡牌列表为空，无法刷新");
            yield break;
        }

        var allSingleViews = FindObjectsOfType<SingleNumberView>(true);
        var allCompositeViews = FindObjectsOfType<CompositeNumberView>(true);
        bool hasAnimatedValue = false;

        // 刷新单数字卡
        int singleRefreshCount = 0;
        foreach (var view in allSingleViews)
        {
            if (view.boundInstance != null && selectedCards.Contains(view.boundInstance))
            {
                view.BindInstance(view.boundInstance);
                hasAnimatedValue |= view.PlaySettlementValuePopAnimation(settlementValuePopDuration, settlementValuePopScaleMultiplier);
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
                hasAnimatedValue |= view.PlaySettlementValuePopAnimation(settlementValuePopDuration, settlementValuePopScaleMultiplier);
                compositeRefreshCount++;
            }
        }

        if (hasAnimatedValue)
        {
            yield return new WaitForSeconds(settlementValuePopDuration);
        }
    }

    public IEnumerator ShowSelectedCardScoreSequence(List<NumberCardInstance> selectedCards,
        List<System.Numerics.BigInteger> rawValues,
        List<System.Numerics.BigInteger> adjustedValues,
        float perCardDisplayTime)
    {
        if (selectedCards == null || selectedCards.Count == 0)
        {
            yield break;
        }

        HideAllCardScoreOverlays();

        List<Component> orderedViews = GetOrderedSelectedCardViews(selectedCards);

        for (int i = 0; i < selectedCards.Count; i++)
        {
            if (i < rawValues.Count && orderedViews[i] != null)
            {
                ShowCardScoreOverlay(orderedViews[i].gameObject, FormatBigNumber(rawValues[i]));
            }

            yield return new WaitForSeconds(perCardDisplayTime);
        }

        if (ShouldRefreshAdjustedValues(rawValues, adjustedValues))
        {
            for (int i = 0; i < selectedCards.Count; i++)
            {
                if (i < adjustedValues.Count && orderedViews[i] != null)
                {
                    ShowCardScoreOverlay(orderedViews[i].gameObject, FormatBigNumber(adjustedValues[i]));
                }
            }

            yield return new WaitForSeconds(perCardDisplayTime);
        }
    }

    public void HideAllCardScoreOverlays()
    {
        List<GameObject> staleKeys = null;

        foreach (var kvp in cardScoreOverlayCache)
        {
            if (kvp.Key == null || kvp.Value == null || kvp.Value.rootObject == null)
            {
                staleKeys ??= new List<GameObject>();
                staleKeys.Add(kvp.Key);
                continue;
            }

            kvp.Value.rootObject.SetActive(false);
        }

        if (staleKeys != null)
        {
            foreach (var key in staleKeys)
            {
                cardScoreOverlayCache.Remove(key);
            }
        }
    }

    List<Component> GetOrderedSelectedCardViews(List<NumberCardInstance> selectedCards)
    {
        Dictionary<NumberCardInstance, Component> cardToView = new Dictionary<NumberCardInstance, Component>();
        var allSingleViews = FindObjectsOfType<SingleNumberView>(true);
        var allCompositeViews = FindObjectsOfType<CompositeNumberView>(true);

        foreach (var view in allSingleViews)
        {
            if (view != null && view.boundInstance != null && !cardToView.ContainsKey(view.boundInstance))
            {
                cardToView.Add(view.boundInstance, view);
            }
        }

        foreach (var view in allCompositeViews)
        {
            if (view != null && view.boundInstance != null && !cardToView.ContainsKey(view.boundInstance))
            {
                cardToView.Add(view.boundInstance, view);
            }
        }

        List<Component> orderedViews = new List<Component>(selectedCards.Count);
        for (int i = 0; i < selectedCards.Count; i++)
        {
            Component resolvedView = null;
            if (selectedCards[i] != null)
            {
                cardToView.TryGetValue(selectedCards[i], out resolvedView);
            }

            if (resolvedView == null)
            {
                Debug.LogWarning($"[UIManager] 槽位 {i} 未找到对应的卡牌视图，无法显示结算覆盖层");
            }

            orderedViews.Add(resolvedView);
        }

        return orderedViews;
    }

    bool ShouldRefreshAdjustedValues(List<System.Numerics.BigInteger> rawValues, List<System.Numerics.BigInteger> adjustedValues)
    {
        if (rawValues == null || adjustedValues == null || rawValues.Count != adjustedValues.Count)
        {
            return false;
        }

        for (int i = 0; i < rawValues.Count; i++)
        {
            if (rawValues[i] != adjustedValues[i])
            {
                return true;
            }
        }

        return false;
    }

    void ShowCardScoreOverlay(GameObject cardRoot, string displayText)
    {
        CardScoreOverlayCache overlay = GetOrCreateCardScoreOverlay(cardRoot);
        if (overlay == null || overlay.rootObject == null || overlay.text == null)
        {
            Debug.LogWarning("[UIManager] 结算覆盖层创建失败，无法显示卡牌点数");
            return;
        }

        overlay.text.text = displayText;
        overlay.rootObject.SetActive(true);
        overlay.rootObject.transform.SetAsLastSibling();
    }

    CardScoreOverlayCache GetOrCreateCardScoreOverlay(GameObject cardRoot)
    {
        if (cardRoot == null)
        {
            return null;
        }

        if (cardScoreOverlayCache.TryGetValue(cardRoot, out CardScoreOverlayCache cachedOverlay) &&
            cachedOverlay != null &&
            cachedOverlay.rootObject != null &&
            cachedOverlay.text != null)
        {
            return cachedOverlay;
        }

        if (scoreDisplayPrefab == null)
        {
            Debug.LogWarning("[UIManager] scoreDisplayPrefab 未在 Inspector 中绑定，无法显示卡牌结算点数覆盖层");
            return null;
        }

        GameObject overlayRoot = CreateCardScoreOverlayObject(cardRoot);
        if (overlayRoot == null)
        {
            return null;
        }

        Text overlayText = overlayRoot.GetComponentInChildren<Text>(true);
        if (overlayText == null)
        {
            Debug.LogWarning("[UIManager] 结算覆盖层缺少 Text 组件");
            return null;
        }

        DisableOverlayRaycast(overlayRoot);
        overlayRoot.SetActive(false);

        CardScoreOverlayCache newOverlay = new CardScoreOverlayCache
        {
            rootObject = overlayRoot,
            text = overlayText
        };

        cardScoreOverlayCache[cardRoot] = newOverlay;
        return newOverlay;
    }

    GameObject CreateCardScoreOverlayObject(GameObject cardRoot)
    {
        GameObject overlayRoot = Instantiate(scoreDisplayPrefab, cardRoot.transform, false);

        RectTransform overlayRect = overlayRoot.GetComponent<RectTransform>();
        if (overlayRect != null)
        {
            overlayRect.anchoredPosition = UnityEngine.Vector2.zero;
            overlayRect.localScale = UnityEngine.Vector3.one;
            overlayRect.localRotation = UnityEngine.Quaternion.identity;
        }

        return overlayRoot;
    }

    void DisableOverlayRaycast(GameObject overlayRoot)
    {
        if (overlayRoot == null)
        {
            return;
        }

        Graphic[] graphics = overlayRoot.GetComponentsInChildren<Graphic>(true);
        foreach (var graphic in graphics)
        {
            graphic.raycastTarget = false;
        }
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

    #region 祝福选择提示
    /// <summary>
    /// 显示祝福选择提示（格式：name:description）并置顶显示。
    /// 在触发带选择功能的祝福（老千/许愿币/暗箱操作/启明星/多多益善）时调用，
    /// 重复调用会直接更新文本内容；选择结束后由 HideCardSelectionBlessing 关闭。
    /// </summary>
    public void ShowCardSelectionBlessing(string blessingName, string description)
    {
        if (cardSelectionBlessingText == null)
        {
            Debug.LogWarning("[UIManager] cardSelectionBlessingText 未赋值，无法显示祝福选择提示");
            return;
        }

        cardSelectionBlessingText.text = $"{blessingName}:{description}";
        cardSelectionBlessingText.gameObject.SetActive(true);
        cardSelectionBlessingText.transform.SetAsLastSibling(); // 实时置顶显示
    }

    /// <summary>
    /// 关闭祝福选择提示（卡牌/祝福选择结束或界面被关闭时调用）
    /// </summary>
    public void HideCardSelectionBlessing()
    {
        if (cardSelectionBlessingText != null)
        {
            cardSelectionBlessingText.gameObject.SetActive(false);
        }
    }
    #endregion

    #region 祝福选择效果
    #region 许愿币祝福选择（许愿币专用）
    /// <summary>
    /// 打开许愿币祝福选择界面（只显示可叠加的已拥有祝福）
    /// </summary>
    public void OpenWishCoinBlessSelection()
    {
        // 直接打开你现有的祝福面板
        myBlessPanel.SetActive(true);
        HideDeletionCostPanelsForBlessView();
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
    }
    #endregion

    #region 暗箱操作祝福选择（暗箱操作专用）
    /// <summary>
    /// 打开暗箱操作祝福选择界面（只显示可叠加的已拥有祝福）
    /// </summary>
    public void OpenDarkBoxBlessSelection()
    {
        // 直接打开你现有的祝福面板
        myBlessPanel.SetActive(true);
        HideDeletionCostPanelsForBlessView();
        myBlessPanel.transform.SetAsLastSibling();

        // 调用专属刷新（只显示可叠加祝福）
        ShowMyBlessings showScript = myBlessPanel.GetComponent<ShowMyBlessings>();
        if (showScript != null)
        {
            showScript.ShowOnlyStackableOwnedBlessings();
        }
    }

    /// <summary>
    /// 关闭暗箱操作选择，恢复正常祝福面板显示
    /// </summary>
    public void CloseDarkBoxBlessSelection()
    {
        myBlessPanel.SetActive(false);
    }
    #endregion

    #region 老千
    // 老千：打开数字卡选择面板
    public void OpenCardCheatNumberSelection()
    {
        if (myNumberCardPanel != null)
        {
            myNumberCardPanel.SetActive(true);
            myNumberCardPanel.transform.SetAsLastSibling();
        }
    }

    // 老千：关闭数字卡选择面板
    public void CloseCardCheatNumberSelection()
    {
        if (myNumberCardPanel != null)
        {
            myNumberCardPanel.SetActive(false);
        }
    }
    #endregion

    #region 启明星
    // 启明星：打开数字卡选择面板
    public void OpenMorningStarNumberSelection()
    {
        if (myNumberCardPanel != null)
        {
            myNumberCardPanel.SetActive(true);

            // 强制刷新数字卡列表：面板 OnDisable 时会清空 goToInstance 映射，
            // 而 OnEnable 的脏检查在库存版本未变时会跳过重建，导致点击无法定位卡牌。
            // 与 OpenCardDeletionDeck（删卡）保持一致，显式刷新以重建点击映射。
            var numberCardView = myNumberCardPanel.GetComponent<ShowMyNumberCard>();
            if (numberCardView != null)
            {
                numberCardView.RefreshAllCards();
            }

            myNumberCardPanel.transform.SetAsLastSibling();
        }
    }

    // 启明星：关闭数字卡选择面板
    public void CloseMorningStarNumberSelection()
    {
        if (myNumberCardPanel != null)
        {
            myNumberCardPanel.SetActive(false);
        }
    }
    #endregion

    #region 多多益善
    // 多多益善：打开公式卡选择界面
    public void OpenMoreMoreBetterSelection()
    {
        if (myFormulaCardPanel != null)
        {
            myFormulaCardPanel.SetActive(true);
            myFormulaCardPanel.transform.SetAsLastSibling();
        }
    }

    // 关闭多多益善界面
    public void CloseMoreMoreBetterSelection()
    {
        if (myFormulaCardPanel != null)
            myFormulaCardPanel.SetActive(false);
    }
    #endregion
    #endregion
    /// <summary>
    /// 关闭商店界面
    /// </summary>
    public void CloseShop()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.ExitDeletionMode();
        }

        if (shopPanel != null)
            shopPanel.SetActive(false);
        if (myCardButton != null)
            myCardButton.SetActive(false);
    }

}
