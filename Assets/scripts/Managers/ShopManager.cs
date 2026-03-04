using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random; // 明确指定使用 UnityEngine.Random
using System.Numerics;
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
    //删除卡牌相关配置
    public int totalRemovedNumberCards = 0;
    public int baseNumberCardRemoveCost = 5;

    [Header("槽位解锁配置")]
    public int baseNumberSlotUnlockCost = 10; // 数字卡槽位基础解锁消耗
    public int baseFormulaSlotUnlockCost = 20; // 公式卡槽位基础解锁消耗
    public int numberSlotUnlockTimes = 0; // 数字卡已解锁次数
    public int formulaSlotUnlockTimes = 0; // 公式卡已解锁次数
    
    [Tooltip("公式卡库 - 拖入 FormulaCardLibrary 资源")]
    public FormulaCardLibrary formulaCardLibrary;

    [Header("卡牌库引用")]
    public NumberCardLibrary numberCardLibrary; // 新增：数字卡库的引用

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

    /// <summary>
    /// 生成数字卡商品
    /// </summary>
    void GenerateNumberCards()
    {
        shopNumberCards.Clear();

        // 验证库是否存在
        if (numberCardLibrary == null || numberCardLibrary.allCards == null || numberCardLibrary.allCards.Count == 0)
        {
            Debug.LogError("NumberCardLibrary 未设置或为空！");
            return;
        }

        // 生成所有槽位（包括锁定的）
        for (int i = 0; i < MaxnumberCardCount; i++)
        {
            if (i < numberCardCount)
            {
                // 未锁定槽位：随机抽取一张卡
                int randomIndex = Random.Range(0, numberCardLibrary.allCards.Count);
                NumberCardData randomCard = numberCardLibrary.allCards[randomIndex];

                // 推断布局类型
                randomCard.layoutType = InferLayoutType(randomCard);

                // 创建实例并计算价格
                NumberCardInstance instance = new NumberCardInstance(randomCard);
                int price = instance.GetNumberCardPrice(randomCard);

                shopNumberCards.Add(new ShopItem<NumberCardInstance>(instance, price));
                Debug.Log($"槽位{i}：{randomCard.cardName}，价格 {price}");
            }
            else
            {
                // 锁定槽位
                shopNumberCards.Add(new ShopItem<NumberCardInstance>(null, 0));
            }
        }
    }
    /// <summary>
    /// 根据卡牌的逻辑类型推断布局类型
    /// </summary>
    private NumberCardLayoutType InferLayoutType(NumberCardData card)
    {
        switch (card.logicalType)
        {
            case NumberCardData.LogicalType.Normal:
                return NumberCardLayoutType.Single;

            case NumberCardData.LogicalType.Addition:
                return NumberCardLayoutType.Add_AB;

            case NumberCardData.LogicalType.Multiplication:
                return NumberCardLayoutType.Multiply_AB;

            case NumberCardData.LogicalType.Power:
                return NumberCardLayoutType.Composite_AB;

            default:
                return NumberCardLayoutType.Single;
        }
    }

    /// <summary>
    /// 生成公式卡商品
    /// </summary>
    void GenerateFormulaCards()
    {
        shopFormulaCards.Clear();

        // 验证库是否存在
        if (formulaCardLibrary == null || formulaCardLibrary.allCards == null || formulaCardLibrary.allCards.Count == 0)
        {
            Debug.LogError("FormulaCardLibrary 未设置或为空！");
            return;
        }

        // 创建临时池，避免重复抽取
        List<FormulaCardData> tempPool = new List<FormulaCardData>(formulaCardLibrary.allCards);

        // 生成所有槽位
        for (int i = 0; i < MaxformulaCardCount; i++)
        {
            if (i < formulaCardCount && tempPool.Count > 0)
            {
                // 未锁定槽位：随机抽取
                int randomIndex = Random.Range(0, tempPool.Count);
                FormulaCardData randomCard = tempPool[randomIndex];
                tempPool.RemoveAt(randomIndex); // 避免重复

                shopFormulaCards.Add(new ShopItem<FormulaCardData>(randomCard, randomCard.CardPrice));
                Debug.Log($"槽位{i}：{randomCard.Name}，价格 {randomCard.CardPrice}");
            }
            else
            {
                // 锁定槽位
                shopFormulaCards.Add(new ShopItem<FormulaCardData>(null, 0));
            }
        }
    }

    public bool TryBuyNumberCard(ShopItem<NumberCardInstance> item)
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
            Debug.Log("n点数不足，无法购买");
            return false;
        }
        
        GameManager.Instance.AddPoints(-item.price);//扣除点数
        
        // item.cardData 现在是 Instance，所以要访问 .cardData.cardData.cardName
        Debug.Log($"购买成功: {item.cardData.cardData.cardName}");

        // 添加到背包
        // 注意：item.cardData 是 NumberCardInstance 类型
        // item.cardData.cardData 是 NumberCardData (ScriptableObject) 类型
        PlayerCardInventory.Instance.AddNumberCard(item.cardData.cardData);

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
            Debug.Log("f点数不足，无法购买");
            return false;
        }
        PlayerCardInventory.Instance.AddFormulaCard(item.cardData);
        GameManager.Instance.AddPoints(-item.price);
        Debug.Log("购买成功");
        CardManager.Instance.SyncDeckFromInventory();

        item.sold = true;
        return true;
    }
    //商店刷新
    public void RefreshShop()
    {
        int currentRound = GameManager.Instance.currentRound;
        long roundSquare = (long)Mathf.Pow(currentRound, 2);
        long powerOfTwo = (long)Mathf.Pow(2, refreshCount);
        long refreshCost = roundSquare * powerOfTwo;///计算刷新需要的点数
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

    #region 数字卡删除逻辑
    /// <summary>
    /// 计算删除数字卡卡牌的消耗点数
    /// </summary>
    public BigInteger CalculateNumberRemoveCost()
    {
        BigInteger powerOfTwo = BigInteger.Pow(2, totalRemovedNumberCards);
        return baseNumberCardRemoveCost * powerOfTwo;
    }
    ///删除对应的卡牌实例
    public bool TryRemoveNumberCard(NumberCardInstance NumbercardToRemove)///这里的NumbercardToRemove应该是要跟UI关联起来（？），这个我不太会做
    {
        if (NumbercardToRemove == null)///检测是否接收到实例
        {
            Debug.LogWarning("没有接受到要删除的数字卡");
            return false;
        }

        if (!PlayerCardInventory.Instance.numberCards.Contains(NumbercardToRemove))///检测是否接收到卡组中没有的数字卡
        {
            Debug.LogWarning("接收到了玩家未拥有的数字卡");
            return false;
        }

        int minRequireCards = 6;///设定最少需要保留几张数字卡，目前设定为6张
        if (PlayerCardInventory.Instance.numberCards.Count <= minRequireCards)
        {
            Debug.LogWarning("卡组中数字卡过少");
            return false;
        }
        BigInteger NumberCardremoveCost = CalculateNumberRemoveCost();///计算所需点数
        if (GameManager.Instance.currentPoints < NumberCardremoveCost)///检查点数是否足够
        {
            Debug.LogWarning("点数不足");
            return false;
        }
        ///删除卡组中的对应数字卡
        PlayerCardInventory.Instance.numberCards.Remove(NumbercardToRemove);
        GameManager.Instance.AddPoints(-NumberCardremoveCost);
        totalRemovedNumberCards++;
        CardManager.Instance.SyncDeckFromInventory();///应该要调用这个方法同步牌堆（？）
        return true;
    }
    #endregion



    #region 槽位解锁逻辑
    /// <summary>
    /// 计算数字卡槽位解锁消耗
    /// </summary>
    public long CalculateNumberSlotUnlockCost()
    {
        // 指数增长：基础消耗 * 2^已解锁次数（和 删除/刷新逻辑数值体系 一样？）
        long powerOfTwo = (long)Mathf.Pow(2, numberSlotUnlockTimes);
        return baseNumberSlotUnlockCost * powerOfTwo;
    }

    /// <summary>
    /// 计算公式卡槽位解锁消耗
    /// </summary>
    public long CalculateFormulaSlotUnlockCost()
    {
        long powerOfTwo = (long)Mathf.Pow(2, formulaSlotUnlockTimes);
        return baseFormulaSlotUnlockCost * powerOfTwo;
    }

    /// <summary>
    /// 获取下一个可解锁的数字卡槽位编号
    /// </summary>
    public int GetNextUnlockedNumberSlot()
    {
        return numberCardCount;
    }

    /// <summary>
    /// 获取下一个可解锁的公式卡槽位编号
    /// </summary>
    public int GetNextUnlockedFormulaSlot()
    {
        return formulaCardCount;
    }

    /// <summary>
    /// 尝试解锁数字卡槽位
    /// </summary>
    /// <returns>解锁成功返回true，失败返回false</returns>
    public bool TryUnlockNumberSlot()
    {
        // 判定1：是否已达到最大槽位
        if (numberCardCount >= MaxnumberCardCount)
        {
            Debug.LogWarning("数字卡槽位已解锁至最大值，无法继续解锁");
            return false;
        }

        // 判定2：计算消耗并校验点数
        long unlockCost = CalculateNumberSlotUnlockCost();
        if (GameManager.Instance.currentPoints < unlockCost)
        {
            Debug.LogWarning($"数字卡槽位解锁失败：点数不足，需要{unlockCost}，当前{GameManager.Instance.currentPoints}");
            return false;
        }

        // 执行解锁：扣除点数、更新解锁次数、增加可购买槽位数量
        GameManager.Instance.AddPoints(-unlockCost);
        numberSlotUnlockTimes++;
        numberCardCount++;

        Debug.Log($"数字卡槽位解锁成功！当前可购买数量：{numberCardCount}，累计解锁次数：{numberSlotUnlockTimes}");

        // 刷新商店商品和UI
        OpenShop();
        return true;
    }

    /// <summary>
    /// 尝试解锁公式卡槽位
    /// </summary>
    /// <returns>解锁成功返回true，失败返回false</returns>
    public bool TryUnlockFormulaSlot()
    {
        // 判定1：是否已达到最大槽位
        if (formulaCardCount >= MaxformulaCardCount)
        {
            Debug.LogWarning("公式卡槽位已解锁至最大值，无法继续解锁");
            return false;
        }

        // 判定2：计算消耗并校验点数
        long unlockCost = CalculateFormulaSlotUnlockCost();
        if (GameManager.Instance.currentPoints < unlockCost)
        {
            Debug.LogWarning($"公式卡槽位解锁失败：点数不足，需要{unlockCost}，当前{GameManager.Instance.currentPoints}");
            return false;
        }

        // 执行解锁：扣除点数、更新解锁次数、增加可购买槽位数量
        GameManager.Instance.AddPoints(-unlockCost);
        formulaSlotUnlockTimes++;
        formulaCardCount++;

        Debug.Log($"公式卡槽位解锁成功！当前可购买数量：{formulaCardCount}，累计解锁次数：{formulaSlotUnlockTimes}");

        // 刷新商店商品和UI
        OpenShop();
        return true;
    }
    #endregion
}
