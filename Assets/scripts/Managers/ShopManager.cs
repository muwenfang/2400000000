using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random; // 明确指定使用 UnityEngine.Random

/// <summary>
/// 商店系统。读取database中的商品信息，读取玩家信息，处理购买逻辑
/// </summary>

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
    public List<ShopItem<NumberCardInstance>> shopNumberCards = new();
    public List<ShopItem<FormulaCardData>> shopFormulaCards = new();


    public void OpenShop()
    {
        GenerateNumberCards();
        GenerateFormulaCards();

        // --- 新增：通知 UI 刷新 ---
        UIManager.Instance.RefreshShopUI();
    }

    void GenerateNumberCards()//从工厂生成随机数字卡
    {
        shopNumberCards.Clear();


        // 生成所有最大槽位数量的卡（包括锁定的）
        for (int i = 0; i < MaxnumberCardCount; i++)
        {
            if (i < numberCardCount)
            {
                // 未锁定的槽位：生成真实卡牌
                NumberCardInstance instance = NumberCardFactory.GenerateRandomCard();
                int price = instance.GetNumberCardPrice(instance.cardData);
                shopNumberCards.Add(new ShopItem<NumberCardInstance>(instance, price));
                Debug.Log($"生成商店数字卡槽位{i}：{instance.cardData.layoutType}，价格：{price}");
            }
            else
            {
                // 锁定的槽位：添加null占位
                shopNumberCards.Add(new ShopItem<NumberCardInstance>(null, 0));
                Debug.Log($"槽位{i}：锁定状态");
            }
        }
    }

    void GenerateFormulaCards()//从总库中随机选择公式卡
    {
        shopFormulaCards.Clear();
        List<FormulaCardData> tempPool = new List<FormulaCardData>(allFormulaCards);

        // 生成所有最大槽位数量（包括锁定的）
        for (int i = 0; i < MaxformulaCardCount; i++)
        {
            if (i < formulaCardCount && tempPool.Count > 0)
            {
                // 未锁定的槽位：随机抽取公式卡
                int index = UnityEngine.Random.Range(0, tempPool.Count); // 显式指定 UnityEngine.Random
                FormulaCardData pickedData = tempPool[index];
                tempPool.RemoveAt(index);
                shopFormulaCards.Add(new ShopItem<FormulaCardData>(pickedData, pickedData.CardPrice));
                Debug.Log($"生成商店公式卡槽位{i}：{pickedData.Name}，价格：{pickedData.CardPrice}");
            }
            else
            {
                // 锁定的槽位：添加null占位
                shopFormulaCards.Add(new ShopItem<FormulaCardData>(null, 0));
                Debug.Log($"公式卡槽位{i}：锁定状态");
            }
        }

    }


    public bool TryBuyNumberCard(ShopItem<NumberCardData> item)
    {
        if (item == null || item.cardData == null)
        {
            Debug.Log("槽位已锁定，无法购买");
            return false;
        }
        if (item.sold) 
        {
            Debug.Log("商品已售出");
            return false; 
        }
        if (GameManager.Instance.currentPoints < item.price)
        {
            Debug.Log("点数不足，无法购买");
            return false;
        }
        
        GameManager.Instance.AddPoints(-item.price);//扣除点数
        
        Debug.Log("购买成功");
        PlayerCardInventory.Instance.AddNumberCard(item.cardData);//添加数字卡到卡组

        item.sold = true;
        return true;
    }
    public bool TryBuyFormulaCard(ShopItem<FormulaCardData> item)
    {
        if (item == null || item.cardData == null)
        {
            Debug.Log("槽位已锁定，无法购买");
            return false;
        }

        if (item.sold)
        {
            Debug.Log("商品已售出");
            return false;
        }
        
        if (GameManager.Instance.currentPoints < item.price)
        {
            Debug.Log("点数不足，无法购买");
            return false;
        }
        PlayerCardInventory.Instance.AddFormulaCard(item.cardData);
        GameManager.Instance.AddPoints(-item.price);
        Debug.Log("购买成功");

        item.sold = true;
        return true;
    }
    //商店刷新
    public void RefreshShop()
    {
        int currentRound = GameManager.Instance.currentRound;
        long roundSquare = (long)Mathf.Pow(currentRound, 2);
        long powerOfTwo = (long)Mathf.Pow(2, refreshCount);
        long refreshCost = roundSquare * powerOfTwo;
        if (GameManager.Instance.currentPoints < refreshCost)
        {
            Debug.Log("点数不足，无法刷新");
            return;
        }
        GameManager.Instance.AddPoints(-refreshCost);
        refreshCount++;//刷新次数应该每回合重置
        
        OpenShop();
    }

    public void CloseShop() 
    {
        // 隐藏商店面板
        if (UIManager.Instance.shopPanel != null)
        {
            UIManager.Instance.shopPanel.SetActive(false);
        }
        //重置刷新次数
        refreshCount = 0;
    }

}





