using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

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

    public GameObject numberCardPrefab;
    public GameObject formulaCardPrefab;

    void Awake()
    {
        Instance = this;//单例模式
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
            var go = Instantiate(numberCardPrefab, handArea);
            go.GetComponent<CardUI>().BindNumberCard(card);
        }
    }


    // 商店显示
    public void ShowShopNumberCards(List<ShopItem<NumberCardData>> items)
    {
        ClearArea(shopNumberArea);

        foreach (var item in items)
        {
            var go = Instantiate(numberCardPrefab, shopNumberArea);// 实例化prefab，使用 shopNumberArea 作为父对象
            go.GetComponent<ShopCardUI>().BindNumberItem(item);// 传递 ShopItem<NumberCardData>
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
}

public class CardUI : MonoBehaviour// 卡牌UI显示脚本
{
    public Text titleText;
    public Text contentText;
    public NumberCardInstance BoundCard { get; private set; }

    public void BindNumberCard(NumberCardInstance card)
    {
        BoundCard = card;
        titleText.text = card.cardData.cardName;
        contentText.text = card.GetOutPutValue().ToString();
    }

    public void BindFormulaCard(FormulaCardData card)
    {
        titleText.text = card.Name;
        contentText.text = card.Pattern;
    }
}


public class ShopCardUI : MonoBehaviour
{
    ShopItem<NumberCardData> numberItem;
    ShopItem<FormulaCardData> formulaItem;

    public Text titleText;
    public Text priceText;
    public Button buyButton;

    public void BindNumberItem(ShopItem<NumberCardData> item)
    {
        numberItem = item;
        formulaItem = null;

        titleText.text = item.cardData.cardName;
        priceText.text = item.price.ToString();
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
        bool success = false;

        if (numberItem != null)
            success = ShopManager.Instance.TryBuyNumberCard(numberItem);

        if (formulaItem != null)
            success = ShopManager.Instance.TryBuyFormulaCard(formulaItem);

        if (success)
        {
            buyButton.interactable = false;
            priceText.text = "已售出";
        }
    }
}

    


