using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.UI;

public interface NumberCardLayoutView
{
    void Bind(NumberCardData data);
}

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

    [Header("UI 面板引用")]
    public GameObject startMenuPanel; // 在 Inspector 中拖入主菜单面板
    public GameObject gameUIPanel; // 游戏内UI面板
    public GameObject gameOverPanel; // 游戏结束面板
    public GameObject shopPanel; // 商店面板
    public GameObject confirmPanel;// 确认面板

    [Header("UI组件")]
    public Text pointsText;//显示当前点数
    public Text roundText;//显示当前回合
    public Transform handArea;//手牌区域
    public Transform formulaArea;//填空卡区域

    [Header("商店卡牌显示区域")]
    public Transform shopNumberArea;
    public Transform shopFormulaArea;

    public GameObject formulaCardPrefab;
    public GameObject shopSlotPrefab; // 拖入那个带价格显示和购买按钮的 Prefab

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
    public void ShowStartMenu() 
    {
        if (startMenuPanel != null)
        {
            startMenuPanel.SetActive(true);
            Debug.Log("显示主菜单");
        }
    }
    public void HideStartMenu() 
    {
        if (startMenuPanel != null)
        {
            startMenuPanel.SetActive(false);
        }
    }
    public void ShowGameUI() 
    {
        if (gameUIPanel != null)
        {
            gameUIPanel.SetActive(true);
        }
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
        ClearArea(handArea);

        foreach (var card in handCards)
        {
            GameObject prefab =
                numberCardLibrary.GetPrefab(card.cardData.layoutType);

            if (prefab == null)
                continue;

            GameObject go = Instantiate(prefab, handArea);

            // UI 显示
            go.GetComponent<NumberCardView>()
              .Bind(card.cardData);

            // 拖拽 + 数据
            go.GetComponent<PlayerController>()
              .Bind(card);
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
public class NumberCardView : MonoBehaviour
{
    public Transform contentRoot;

    public GameObject singlePrefab;
    public GameObject addPrefab;
    public GameObject multiplyPrefab;
    public GameObject compositePrefab;

    public void Bind(NumberCardData data)
    {
        Clear();

        GameObject prefab = GetPrefab(data.layoutType);
        GameObject ui = Instantiate(prefab, contentRoot);

        ui.GetComponent<NumberCardLayoutView>()
          .Bind(data);
    }

    void Clear()
    {
        foreach (Transform child in contentRoot)
            Destroy(child.gameObject);
    }

    GameObject GetPrefab(NumberCardLayoutType type)
    {
        return type switch
        {
            NumberCardLayoutType.Single => singlePrefab,
            NumberCardLayoutType.Add_AB => addPrefab,
            NumberCardLayoutType.Multiply_AB => multiplyPrefab,
            NumberCardLayoutType.Composite_AB => compositePrefab,
            _ => singlePrefab
        };
    }
}
public class SingleNumberView : MonoBehaviour, NumberCardLayoutView
{
    public Text valueText;

    public void Bind(NumberCardData data)
    {
        valueText.text = data.partA.value.ToString();
    }
}
public class Add_Multi_Power_NumberView : MonoBehaviour, NumberCardLayoutView
{
    public Text aText;
    public Text bText;

    public void Bind(NumberCardData data)
    {
        aText.text = data.partA.value.ToString();
        bText.text = data.partB.value.ToString();
    }
}
public class FormulaCardUI : MonoBehaviour
{
    public Transform formulaArea;          // UI 容器
    public GameObject textPrefab;           // 显示 + * ( )
    public GameObject slotPrefab;           // # 槽位

    private readonly List<FormulaSlot> slots = new();

    public void Bind(FormulaCardData formula)
    {
        Clear();

        foreach (char c in formula.Pattern)
        {
            if (c == '#')
            {
                GameObject go = Instantiate(slotPrefab, formulaArea);
                FormulaSlot slot = go.GetComponent<FormulaSlot>();
                slot.Init(this);
                slots.Add(slot);
            }
            else
            {
                GameObject go = Instantiate(textPrefab, formulaArea);
                go.GetComponent<Text>().text = c.ToString();
            }
        }
    }

    void Clear()
    {
        foreach (Transform child in formulaArea)
            Destroy(child.gameObject);

        slots.Clear();
    }

    // 被 Slot 调用
    public void OnSlotFilled(NumberCardInstance card)
    {
        CardManager.Instance.AddNumberCardToFormula(card);
    }
}





