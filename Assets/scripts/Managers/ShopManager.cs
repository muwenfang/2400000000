using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 商店系统。读取database中的商品信息，读取玩家信息，处理购买逻辑
/// </summary>
public class ShopManager : MonoBehaviour
{
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
    public List<NumberCardData> shopNumberCards = new();
    public List<FormulaCardData> shopFormulaCards = new();

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

    void GenerateNumberCards()
    {
        shopNumberCards.Clear();

        for (int i = 0; i < numberCardCount; i++)
        {
            NumberCardInstance instance = NumberCardFactory.GenerateRandomCard();
            shopNumberCards.Add(instance.cardData); 
        }
    }

    void GenerateFormulaCards()
    {
        shopFormulaCards.Clear();
        List<FormulaCardData> temp = new(allFormulaCards);

        for (int i = 0; i < formulaCardCount; i++)
        {
            if (temp.Count == 0) break;

            int index = Random.Range(0, temp.Count);
            shopFormulaCards.Add(temp[index]);
            temp.RemoveAt(index);
        }
    }

    void CreateShopUI()
    {
        foreach (var num in shopNumberCards)
        {
            var go = Instantiate(numberCardPrefab, numberArea);
            go.GetComponent<ShopCardUI>().BindNumberCard(num);
        }

        foreach (var formula in shopFormulaCards)
        {
            var go = Instantiate(formulaCardPrefab, formulaArea);
            go.GetComponent<ShopCardUI>().BindFormulaCard(formula);
        }
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

}
