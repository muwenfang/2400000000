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

    [Header("游戏信息显示")]
    public Text pointsText;//显示当前点数
    public Text roundText;//显示当前回合
    public Text targetPointsText;//显示目标点数
    public Text stageRequirementText;//显示当前阶段要求点数

    [Header("点数获得提示")]
    public GameObject pointsGainPanel; // 显示获得点数的面板
    public Text pointsGainText; // 显示获得的点数数值
    public float pointsGainDisplayTime = 2f; // 显示时长

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

        Debug.Log($"已激活并置顶面板: {panelToShow.name}");
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
            pointsText.text = $"点数: {FormatBigNumber(points)}";
        }
    }

    /// <summary>
    /// 更新回合数显示
    /// </summary>
    public void UpdateRoundDisplay(int round)
    {
        if (roundText != null)
        {
            roundText.text = $"回合: {round}";
        }
    }

    /// <summary>
    /// 更新目标点数显示
    /// </summary>
    public void UpdateTargetPointsDisplay(System.Numerics.BigInteger targetPoints)
    {
        if (targetPointsText != null)
        {
            targetPointsText.text = $"目标: {FormatBigNumber(targetPoints)}";
        }
    }

    /// <summary>
    /// 更新阶段要求点数显示
    /// </summary>
    public void UpdateStageRequirementDisplay(System.Numerics.BigInteger requirement)
    {
        if (stageRequirementText != null)
        {
            stageRequirementText.text = $"阶段要求: {FormatBigNumber(requirement)}";
        }
    }

    /// <summary>
    /// 显示获得的点数（带动画效果）
    /// </summary>
    public void ShowPointsGain(System.Numerics.BigInteger gainedPoints)
    {
        if (pointsGainPanel == null || pointsGainText == null)
        {
            Debug.LogWarning("pointsGainPanel 或 pointsGainText 未设置！");
            return;
        }

        // 设置文本
        pointsGainText.text = $"+{FormatBigNumber(gainedPoints)}";

        // 显示面板
        pointsGainPanel.SetActive(true);

        // 启动协程在一段时间后隐藏
        StartCoroutine(HidePointsGainAfterDelay());
    }

    /// <summary>
    /// 延迟隐藏点数获得提示
    /// </summary>
    IEnumerator HidePointsGainAfterDelay()
    {
        yield return new WaitForSeconds(pointsGainDisplayTime);

        if (pointsGainPanel != null)
        {
            pointsGainPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 刷新所有游戏UI（点数、回合、阶段要求等）
    /// </summary>
    public void RefreshAllGameInfo()
    {
        UpdatePointsDisplay(GameManager.Instance.currentPoints);
        UpdateRoundDisplay(GameManager.Instance.currentRound);
        UpdateTargetPointsDisplay(GameManager.Instance.targetPoints);

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
        if (number < 1000)
        {
            return number.ToString();
        }
        else if (number < 1000000)
        {
            return $"{number / 1000}K";
        }
        else if (number < 1000000000)
        {
            return $"{number / 1000000}M";
        }
        else if (number < 1000000000000)
        {
            return $"{number / 1000000000}B";
        }
        else
        {
            return $"{number / 1000000000000}T";
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