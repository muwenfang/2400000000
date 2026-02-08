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
    Composite_AB,  // 类似右图那种组合
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
    //public GameObject confirmPanel;// 确认面板

    [Header("UI组件")]
    public Text pointsText;//显示当前点数
    public Text roundText;//显示当前回合
    public Transform handArea;//手牌区域
    public Transform formulaArea;//填空卡区域

    [Header("商店卡牌显示区域")]
    public Transform shopNumberArea;
    public Transform shopFormulaArea;

    public GameObject shopSlotPrefab; // 拖入那个带价格显示和购买按钮的 Prefab
    public GameObject formulaCardUIPrefab; // 用于显示商店中的公式卡

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // 保证全局只有一个 UIManager
        }
    }
    public void ShowPanel(GameObject panelToShow)
    {
        // 隐藏所有面板
        if (startMenuPanel != null) startMenuPanel.SetActive(false);
        if (gameUIPanel != null) gameUIPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);

        // 激活目标
        panelToShow.SetActive(true);
        // 强制把这个面板排到最前面（防止被其他没关掉的UI遮挡）
        panelToShow.transform.SetAsLastSibling();
        Debug.Log($"已激活并置顶面板: {panelToShow.name}");
        
    }
    public void RefreshGameUI()
    {
        Debug.Log("开始刷新游戏界面卡牌...");
        // 1. 显示公式卡
        ClearArea(formulaArea);

        if (CardManager.Instance.currentFormulaCard != null)
        {
            var go = Instantiate(formulaCardPrefab, formulaArea);
            go.transform.localScale = UnityEngine.Vector3.one; // 确保缩放正确
            var formulaUI = go.GetComponent<FormulaCardUI>();
            formulaUI.Bind(CardManager.Instance.currentFormulaCard);
        }


        // 2. 显示数字手牌
        // 先清理旧手牌
        foreach (Transform child in handArea) { Destroy(child.gameObject); }
        Debug.Log($"开始刷新游戏界面卡牌... 当前手牌数量: {CardManager.Instance.currentNumberCards.Count}");

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
            cardGo.transform.localScale =  UnityEngine.Vector3.one; // 强制缩放为 1         
            cardGo.SetActive(true); // 确保它是激活状态

            // ==================== 修复点 1: 视图绑定 ====================
            // 不要用 GetComponent<NumberCardView>，改用接口 NumberCardLayoutView
            // 这样无论是 SingleNumberView 还是其他类型的 View 都能被获取到
            var view = cardGo.GetComponent<NumberCardLayoutView>();
            if (view != null)
            {
                Debug.Log($"正在绑定手牌数据：{cardData.cardData.cardName}");
                view.Bind(cardData.cardData);
            }
            else
            {
                // 如果还是找不到，我们可以打印出这个物体上到底挂了哪些脚本，帮你排查
                MonoBehaviour[] components = cardGo.GetComponentsInChildren<MonoBehaviour>();
                string names = "";
                foreach (var c in components) names += c.GetType().Name + ", ";
                Debug.LogWarning($"[警告] {cardGo.name} 及其子物体上找不到接口！现有脚本: {names}");
            }
            // ==================== 修复点 2: 逻辑绑定 (解决移动问题) ====================
            // 必须获取 PlayerController 并将卡牌实例(cardData)传给它
            var controller = cardGo.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.Bind(cardData);
            }
            else
            {
                // 如果你的卡牌不需要移动，可以忽略这个，但通常卡牌游戏都需要
                Debug.LogWarning($"[警告] {cardGo.name} 缺少 PlayerController 组件，无法进行交互！");
            }

        }

        // 强制刷新布局
        LayoutRebuilder.ForceRebuildLayoutImmediate(handArea.GetComponent<RectTransform>());
    }
    public void UpdatePointsDisplay(BigInteger points)
    {
        pointsText.text = $"点数: {points}";
    }

    public void UpdateRoundDisplay(int round)
    {
        roundText.text = $"回合: {round}";
    }
    // 手牌显示
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
            GameObject prefab =
                numberCardLibrary.GetPrefab(card.cardData.layoutType);

            // 检查 Prefab 是否获取成功
            if (prefab == null)
            {
                Debug.LogError($"【UIManager报错】工厂里没配置 {card.cardData.layoutType} 类型的预制体！");
                continue;
            }

            GameObject go = Instantiate(prefab, handArea);

            // 使用 TryGetComponent 更安全，如果没挂脚本就不会崩溃
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
                Debug.LogError($"Prefab {go.name} 缺少 PlayerController 脚本！请在编辑器里挂载。");
            }
        }
    }
    public void ShowFormulaCard(FormulaCardData formula)
    {
        ClearArea(formulaArea);

        var go = Instantiate(formulaCardPrefab, formulaArea);
        go.GetComponent<FormulaCardUI>().Bind(formula);
    }


    // 商店显示
    public void ShowShopNumberCards(List<ShopItem<NumberCardData>> items)
    {
        ClearArea(shopNumberArea);

        foreach (var item in items)
        {
            // 1. 先生成商店的“外壳”（带价格和按钮）
            GameObject slotGo = Instantiate(shopSlotPrefab, shopNumberArea);
            ShopCardUI shopUI = slotGo.GetComponent<ShopCardUI>();

            // 2. 从工厂获取卡牌“主体”
            GameObject cardPrefab = numberCardLibrary.GetPrefab(item.cardData.layoutType);
            if (cardPrefab != null)
            {
                // 3. 将主体生成为外壳的子物体（通常生成在 shopUI 内部指定的 contentRoot 下）
                GameObject cardBody = Instantiate(cardPrefab, shopUI.numberCardView.contentRoot);

                // 4. 绑定卡牌数据（显示数值）
                cardBody.GetComponent<NumberCardLayoutView>().Bind(item.cardData);
            }

            // 5. 绑定商店数据（显示价格、处理点击）
            shopUI.BindNumberItem(item);
        }
    }

    public void ShowShopFormulaCards(List<ShopItem<FormulaCardData>> items)
    {
        ClearArea(shopFormulaArea);

        foreach (var item in items)
        {
            var go = Instantiate(formulaCardPrefab, shopFormulaArea);
            go.GetComponent<ShopCardUI>().BindFormulaItem(item);
        }
    }

    void ClearArea(Transform area)
    {
        foreach (Transform child in area)
            Destroy(child.gameObject);
    }

    // 在 UIManager 类中添加 ShowShopPanel 方法
    public void ShowShopPanel()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
        }
    }
    public void RefreshShopUI()
    {
        // 1. 清理旧的商品格子
        foreach (Transform child in shopNumberArea) Destroy(child.gameObject);
        foreach (Transform child in shopFormulaArea) Destroy(child.gameObject);

        // 2. 生成数字卡商品
        foreach (var item in ShopManager.Instance.shopNumberCards)
        {
            if (item.sold) continue; // 如果卖掉了就不显示
            GameObject slotGo = Instantiate(shopSlotPrefab, shopNumberArea);
            slotGo.GetComponent<ShopSlotUI>().SetItem(item);
        }

        // 3. 生成公式卡商品
        foreach (var item in ShopManager.Instance.shopFormulaCards)
        {
            if (item.sold) continue;
            GameObject slotGo = Instantiate(shopSlotPrefab, shopFormulaArea);
            slotGo.GetComponent<ShopSlotUI>().SetItem(item);
        }
    }
}


public class ShopCardUI : MonoBehaviour
{
    ShopItem<NumberCardData> numberItem;
    ShopItem<FormulaCardData> formulaItem;

    public Text titleText;
    public Text priceText;
    public Button buyButton;

    public NumberCardView numberCardView;


    public void BindNumberItem(ShopItem<NumberCardData> item)
    {
        numberItem = item;
        priceText.text = $"价格: {item.price}"; //

        // 注意：这里不再需要调用 numberCardView.Bind，
        // 因为 UIManager 已经手动把卡牌生成在里面并 Bind 好了。

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnBuyClick);
    }


    public void BindFormulaItem(ShopItem<FormulaCardData> item)
    {
        formulaItem = item;
        numberItem = null;

        titleText.text = item.cardData.Name;
        priceText.text = item.price.ToString();
    }

    public void OnBuyClick()
    {
        // 这里调用你的 ShopManager 逻辑
        if (ShopManager.Instance.TryBuyNumberCard(numberItem))
        {
            buyButton.interactable = false;
            priceText.text = "已售出";
        }
    }
}







