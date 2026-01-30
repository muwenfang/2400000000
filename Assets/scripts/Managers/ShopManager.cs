using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 商店系统。读取database中的商品信息，读取玩家信息，处理购买逻辑
/// </summary>

//商店抽卡与展示
public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    void Awake()
    {
        Instance = this;
    }

    [Header("配置")]
    // 最大能购买的数量
    public int MaxnumberCardCount = 6;
    public int MaxformulaCardCount = 2;
    // 当前能购买的数量
    public int numberCardCount = 2;
    public int formulaCardCount = 1;

    //刷新次数
    public int refreshCount = 0;

    [Header("公式卡总库")]
    public List<FormulaCardData> allFormulaCards = new();

    [Header("本次商店商品")]
    public List<ShopItem<NumberCardData>> shopNumberCards = new();
    public List<ShopItem<FormulaCardData>> shopFormulaCards = new();

    [Header("UI")]
    public Transform numberArea;
    public Transform formulaArea;
    public GameObject numberCardPrefab;
    public GameObject formulaCardPrefab;


    public void OpenShop()
    {
        ClearShop();
        GenerateNumberCards();
        GenerateFormulaCards();
        CreateShopUI();
    }

    void GenerateNumberCards()//从工厂生成随机数字卡
    {
        shopNumberCards.Clear();

        for (int i = 0; i < numberCardCount; i++)
        {
            NumberCardInstance instance = NumberCardFactory.GenerateRandomCard();
            NumberCardData cardData = instance.cardData;

            int price = instance.GetNumberCardPrice(cardData);
            shopNumberCards.Add(new ShopItem<NumberCardData>(cardData, price)); 
        }
    }

    void GenerateFormulaCards()//从总库中随机选择公式卡
    {
        shopFormulaCards.Clear();
        List<FormulaCardData> temp = new(allFormulaCards);

        for (int i = 0; i < formulaCardCount; i++)
        {
            if (temp.Count == 0) break;

            int index = Random.Range(0, temp.Count);
            FormulaCardData card = temp[index];

            shopFormulaCards.Add(new ShopItem<FormulaCardData>(card, card.CardPrice));
            temp.RemoveAt(index);
        }
    }

    void CreateShopUI()
    {
        foreach (var num in shopNumberCards)
        {
            var go = Instantiate(numberCardPrefab, numberArea);
            go.GetComponent<ShopCardUI>().BindNumberItem(num); // 修正：传递ShopItem<NumberCardData>
        }

        foreach (var formula in shopFormulaCards)
        {
            var go = Instantiate(formulaCardPrefab, formulaArea);
            go.GetComponent<ShopCardUI>().BindFormulaItem(formula); // 修正：传递ShopItem<FormulaCardData>
        }
    }
    
    public bool TryBuyNumberCard(ShopItem<NumberCardData> item)
    {
        if (item.sold) return false;
        
        //[to do]

        item.sold = true;
        return true;
    }
    public bool TryBuyFormulaCard(ShopItem<FormulaCardData> item)
    {
        if (item.sold) return false;
        //[to do]
        item.sold = true;
        return true;
    }
    //商店刷新
    public void RefreshShop()
    {
        //扣点数
        //[to do]
        OpenShop();
    }
    void ClearShop()
    {
        shopNumberCards.Clear();
        shopFormulaCards.Clear();
        foreach (Transform child in numberArea)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in formulaArea)
        {
            Destroy(child.gameObject);
        }
    }
    public void CloseShop() 
    {
        //关闭商店UI

    }

}

//商店购买系统
[System.Serializable]
public class ShopItem<T>
{
    public T cardData;
    public int price;
    public bool sold;

    public ShopItem(T data, int price)
    {
        this.cardData = data;
        this.price = price;
        this.sold = false;
    }
}

