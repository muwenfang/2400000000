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
    public long price;
    public bool sold;

    public ShopItem(T data, long price)
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
    public Text refreshCostText;

    void Awake()
    {
        Instance = this;
    }

    [Header("配置")]
    // 最大能购买的数量
    public int MaxnumberCardCount = 6;
    public int MaxformulaCardCount = 2;
    public int MaxBlessingCardCount = 4;
    // 当前能购买的数量
    public int numberCardCount = 2;
    public int formulaCardCount = 1;
    public int blessingCardCount = 2;

    //刷新次数
    public int refreshCount = 0;

    //删除卡牌相关配置
    [Header("删除功能配置")]
    public Button deleteCardButton;          // 删除卡牌按钮
    public Text deleteCardCostText;          // 显示删除消耗的文本
    public GameObject deleteCostPanel;
    public int totalRemovedNumberCards = 0;
    public int totalRemovedFormulaCards = 0;
    public int baseNumberCardRemoveCost = 5;
    public int baseFormulaCardRemoveCost = 200;

    [Header("槽位解锁配置")]
    public int baseNumberSlotUnlockCost = 10; // 数字卡槽位基础解锁消耗
    public int baseFormulaSlotUnlockCost = 20; // 公式卡槽位基础解锁消耗
    public int baseBlessingSlotUnlockCost = 2000;
    public int numberSlotUnlockTimes = 0; // 数字卡已解锁次数
    public int formulaSlotUnlockTimes = 0; // 公式卡已解锁次数
    public int blessingSlotUnlockTimes = 0;

    [Tooltip("公式卡库 - 拖入 FormulaCardLibrary 资源")]
    public FormulaCardLibrary formulaCardLibrary;

    [Header("卡牌库引用")]
    public NumberCardLibrary numberCardLibrary; // 数字卡库的引用

    [Header("祝福系统")]
    [Tooltip("祝福卡库 - 拖入 BlessingLibrary 资源")]
    public BlessingLibrary blessingLibrary;

    [Header("本次商店商品")]
    public List<ShopItem<NumberCardInstance>> shopNumberCards = new();
    public List<ShopItem<FormulaCardData>> shopFormulaCards = new();
    public List<ShopItem<BlessingData>> shopBlessings = new();

    // 新增标志位：当前是否处于删卡模式
    public bool isDeletionMode = false;

    
    /// <summary>
    /// 本回合已在当前商店刷新中显示过的祝福ID（用于CurrentRoundOnly类型）
    /// </summary>
    private HashSet<int> currentRoundDisplayedBlessings = new HashSet<int>();


    public void OpenShop()
    {
        GenerateNumberCards();
        GenerateFormulaCards();
        GenerateBlessings();
        InitializeDeletionUI();
        InitializeRefreshCost();


        // ---通知 UI 刷新 ---
        UIManager.Instance.RefreshShopUI();
    }
    public void InitializeRefreshCost()
    {
        int currentRound = GameManager.Instance.currentRound;
        long roundSquare = (long)Mathf.Pow(currentRound, 2);
        long powerOfTwo = (long)Mathf.Pow(2, refreshCount);
        long refreshCost = roundSquare * powerOfTwo;///计算刷新需要的点数


        // 如果拥有丰盈宝库祝福，刷新费用为0
        if (BlessingManager.Instance.HasRichTreasure == 1)
        {
            refreshCost = 0;
        }

        refreshCostText.text = $"cost: {refreshCost}";
    }
    public void InitializeShop()
    {
        numberSlotUnlockTimes = 0; // 数字卡已解锁次数
        formulaSlotUnlockTimes = 0; // 公式卡已解锁次数
        blessingSlotUnlockTimes = 0;
        numberCardCount = 2;
        formulaCardCount = 1;
        blessingCardCount = 2;
        OpenShop();
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

        // 获取价格乘数（如果有祝福影响价格的话）
        float priceMultiplier = BlessingManager.Instance != null
         ? BlessingManager.Instance.GetCurrentPriceMultiplier()
         : 1.0f;

        // 生成所有槽位（包括锁定的）
        for (int i = 0; i < MaxnumberCardCount; i++)
        {
            // 未锁定槽位：随机抽取一张卡
             int randomIndex = Random.Range(0, numberCardLibrary.allCards.Count);
             NumberCardData randomCard = numberCardLibrary.allCards[randomIndex];

            // 推断布局类型
            randomCard.layoutType = InferLayoutType(randomCard);

            // 创建实例并计算价格
            NumberCardInstance instance = new NumberCardInstance(randomCard);
            
            long price = (long)(instance.GetNumberCardPrice(randomCard) * priceMultiplier);
            
            shopNumberCards.Add(new ShopItem<NumberCardInstance>(instance, price));
            

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

        // 获取价格乘数（如果有祝福影响价格的话）
        float priceMultiplier = BlessingManager.Instance != null
            ? BlessingManager.Instance.GetCurrentPriceMultiplier()
            : 1.0f;

        // 创建临时池，避免重复抽取
        List<FormulaCardData> tempPool = new List<FormulaCardData>(formulaCardLibrary.allCards);

        // 生成所有槽位
        for (int i = 0; i < MaxformulaCardCount; i++)
        {
                // 未锁定槽位：随机抽取
                int randomIndex = Random.Range(0, tempPool.Count);
                FormulaCardData randomCard = tempPool[randomIndex];
                tempPool.RemoveAt(randomIndex); // 避免重复

                long finalPrice = (long)(randomCard.CardPrice * priceMultiplier);

                shopFormulaCards.Add(new ShopItem<FormulaCardData>(randomCard, finalPrice));

        }
    }

    /// <summary>
    /// 生成祝福商品
    /// </summary>
    void GenerateBlessings()
    {
        shopBlessings.Clear();

        // 获取价格乘数（如果有祝福影响价格的话）
        float priceMultiplier = BlessingManager.Instance != null
        ? BlessingManager.Instance.GetCurrentPriceMultiplier()
        : 1.0f;

        bool forceNihilism = false;
        int nihilismCount = BlessingManager.Instance.nihilismCount;
        float nihilismChance = nihilismCount * 0.02f;
        
        BlessingData wishBlessing = BlessingManager.Instance.GetWishCoinTargetBlessing();//许愿币效果
        int wishAdded = 0;
        
        if (nihilismCount > 0 && UnityEngine.Random.value < nihilismChance)
        {
            forceNihilism = true;
            Debug.Log($"【虚无主义触发】概率：{nihilismChance*100}% → 全部刷新为虚无主义！");
        }
        
        if (forceNihilism)
        {
            shopBlessings.Clear(); // 清空

            BlessingData nihilism = blessingLibrary.GetBlessingByType(BlessingData.BlessingType.Nihilism);
            int price = nihilism.CalculatePrice(BlessingManager.Instance.GetBlessingCount(nihilism.blessingId), priceMultiplier);

            // 固定生成 4 个
            for (int i = 0; i < 4; i++)
            {
                shopBlessings.Add(new ShopItem<BlessingData>(nihilism, price));
            }

            // 消耗许愿币
            if (wishBlessing != null)
                BlessingManager.Instance.ConsumeWishCoin();

            return;
        }
        
        if (wishBlessing != null)
        {
            // 强制加入本次商品，优先占用第一个槽位
            priceMultiplier = BlessingManager.Instance.GetCurrentPriceMultiplier();
            int currentCount = BlessingManager.Instance.GetBlessingCount(wishBlessing.blessingId);
            int price = wishBlessing.CalculatePrice(currentCount, priceMultiplier);

            shopBlessings.Add(new ShopItem<BlessingData>(wishBlessing, price));
            Debug.Log($"许愿币生效：强制刷新出【{wishBlessing.blessingName}】");

            // 消耗许愿币
            BlessingManager.Instance.ConsumeWishCoin();
             wishAdded = 1; // 标记已占用1个位置
        }
        

        if (blessingLibrary == null || blessingLibrary.GetAllBlessings().Count == 0)
        {
            Debug.LogError("BlessingLibrary 未设置或为空！");
            return;
        }

        // 构建可用祝福池（根据刷新行为过滤）
        List<BlessingData> availableBlessings = BuildAvailableBlessingPool();
        
        int remainingSlots = MaxBlessingCardCount - wishAdded;//减去许愿币占位
        
        // 生成4个槽位（包括锁定的）
        for (int i = 0; i < remainingSlots; i++)
        {
            // 如果是解锁的槽位
            if (i < blessingCardCount)
            {
                BlessingData selectedBlessing = null;

                // 从可用池中选择祝福
                if (availableBlessings.Count > 0)
                {
                    int randomIndex = Random.Range(0, availableBlessings.Count);
                    selectedBlessing = availableBlessings[randomIndex];

                    // 移除已选中的祝福（除非是 AlwaysRefresh 类型）
                    // CurrentRoundOnly 和 NeverRefresh 类型在本次刷新中只能出现一次
                    if (selectedBlessing.refreshBehavior != BlessingData.RefreshBehavior.AlwaysRefresh)
                    {
                        availableBlessings.RemoveAt(randomIndex);
                        currentRoundDisplayedBlessings.Add(selectedBlessing.blessingId);
                        Debug.Log($"[ShopManager] 祝福 '{selectedBlessing.blessingName}' 已在本次刷新中显示，标记为已显示");
                    }

                    // 计算价格
                    int currentCount = BlessingManager.Instance.GetBlessingCount(selectedBlessing.blessingId);
                    int price = selectedBlessing.CalculatePrice(currentCount, priceMultiplier);

                    shopBlessings.Add(new ShopItem<BlessingData>(selectedBlessing, price));
                    Debug.Log($"祝福槽位{i}：{selectedBlessing.blessingName}（{selectedBlessing.refreshBehavior}），价格 {price}");
                }
                else
                {
                    // 可用祝福不足，添加空槽位
                    shopBlessings.Add(new ShopItem<BlessingData>(null, 0));
                    Debug.LogWarning($"[ShopManager] 可用祝福不足！槽位{i}无法填充");
                }
            }
            else
            {
                // 锁定槽位：添加占位符（null）
                shopBlessings.Add(new ShopItem<BlessingData>(null, 0));
            }
        }
    }

    /// <summary>
    /// 构建可用祝福池 - 根据刷新行为和购买历史过滤
    /// </summary>
    private List<BlessingData> BuildAvailableBlessingPool()
    {
        List<BlessingData> availableBlessings = new List<BlessingData>();
        List<BlessingData> allBlessings = blessingLibrary.GetAllBlessings();

        foreach (var blessing in allBlessings)
        {
            if (blessing == null) continue;

            bool isAvailable = false;

            switch (blessing.refreshBehavior)
            {
                case BlessingData.RefreshBehavior.AlwaysRefresh:
                    // 总是可用
                    isAvailable = true;
                    break;

                case BlessingData.RefreshBehavior.NeverRefresh:
                    // 仅当未曾购买时可用
                    isAvailable = !BlessingManager.Instance.HasBlessingEverBeenPurchased(blessing.blessingId);
                    break;

                case BlessingData.RefreshBehavior.CurrentRoundOnly:
                    // 仅当本次刷新中未显示过时可用
                    isAvailable = !currentRoundDisplayedBlessings.Contains(blessing.blessingId);
                    break;
            }

            if (isAvailable)
            {
                availableBlessings.Add(blessing);
            }
            else
            {
                Debug.Log($"[ShopManager] 祝福 '{blessing.blessingName}' 不可用（{blessing.refreshBehavior}）");
            }
        }

        return availableBlessings;
    }

    /// <summary>
    /// 重置本回合显示过的祝福
    /// </summary>
    private void ResetCurrentRoundBlessings()
    {
        currentRoundDisplayedBlessings.Clear();
        Debug.Log("[ShopManager] 本回合已显示的祝福记录已清空");
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
        CardManager.Instance.SyncDeckFromInventory();

        item.sold = true;
        return true;
    }
    /// <summary>
    /// 尝试购买祝福
    /// </summary>
    public bool TryBuyBlessing(ShopItem<BlessingData> item)
    {
        if (item == null || item.cardData == null)
        {
            Debug.LogWarning("祝福商品为空");
            return false;
        }

        if (item.sold)
        {
            Debug.LogWarning("祝福已被购买（本回合）");
            return false;
        }

        // 使用 BlessingManager 的购买逻辑
        bool purchaseSuccess = BlessingManager.Instance.TryBuyBlessing(item.cardData);

        if (purchaseSuccess)
        {
            item.sold = true;
            // 刷新商店显示
            UIManager.Instance.RefreshShopUI();
        }

        return purchaseSuccess;
    }
    //商店刷新
    public void RefreshShop()
    {
        int currentRound = GameManager.Instance.currentRound;
        long roundSquare = (long)Mathf.Pow(currentRound, 2);
        long powerOfTwo = (long)Mathf.Pow(2, refreshCount);
        long refreshCost = roundSquare * powerOfTwo;///计算刷新需要的点数


        // 如果拥有丰盈宝库祝福，刷新费用为0
        if (BlessingManager.Instance.HasRichTreasure == 1)
        {
            refreshCost = 0;
        }

        if (GameManager.Instance.currentPoints < refreshCost)
        {
            Debug.Log("点数不足，无法刷新");
            return;
        }
        GameManager.Instance.AddPoints(-refreshCost);

        refreshCostText.text = $"刷新消耗: {refreshCost}";

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
        //清空祝福已购买标记（下次商店刷新时重新生成）
        foreach (var item in shopBlessings)
        {
            item.sold = false;
        }

        //重置刷新次数
        refreshCount = 0;
    }

    #region 卡牌删除逻辑
    public void InitializeDeletionUI()
    {
        if (deleteCardButton != null)
        {
            deleteCardButton.onClick.AddListener(() => StartCardDeletion());
        }
        else
        {
            Debug.LogWarning("[ShopManager] deleteCardButton 未在Inspector中配置");
        }
}
/// <summary>
/// 进入删卡模式
/// 通过CardSelectionManager启动删卡选择流程
/// 由deleteCardButton的点击事件调用
/// </summary>
    public void StartCardDeletion()
    {
        Debug.Log("[ShopManager] 进入删卡模式");

        deleteCostPanel.SetActive(true);
        // 设置标志位
        isDeletionMode = true;

        // 启动CardSelectionManager的删卡模式
        if (CardSelectionManager.Instance != null)
        {
            CardSelectionManager.Instance.StartCardSelection(
                CardSelectionManager.SelectionMode.RemoveCard,
                null
            );
        }
        else
        {
            Debug.LogError("[ShopManager] CardSelectionManager.Instance 为空");
            return;
        }

        // 显示数字卡面板供用户选择删卡
        if (UIManager.Instance != null && UIManager.Instance.myNumberCardPanel != null)
        {
            UIManager.Instance.OpenNumberCardDeck();
            Debug.Log("[ShopManager] 显示数字卡面板，用户可开始删卡");
        }
        else
        {
            Debug.LogError("[ShopManager] UIManager 或 myNumberCardPanel 为空");
        }

        UpdateDeletionUI();
    }


    /// <summary>
    /// 处理单张卡牌被删除的事件
    /// 由ShowMyNumberCard.cs或ShowMyFormula.cs调用
    /// </summary>
    public void OnCardDeleted(object deletedCard)
    {
        if (deletedCard == null)
        {
            Debug.LogError("[ShopManager] OnCardDeleted: 删除的卡牌为空");
            return;
        }

        // 1. 统计删除的卡牌类型
        if (deletedCard is NumberCardInstance numberCard)
        {
            Debug.Log($"[ShopManager] 数字卡被删除：{numberCard.cardData.cardName}");
            totalRemovedNumberCards++;
        }
        else if (deletedCard is FormulaCardData formulaCard)
        {
            Debug.Log($"[ShopManager] 公式卡被删除：{formulaCard.Name}");
            totalRemovedFormulaCards++;
        }

        // 2. 计算并扣除消耗
        long cost = CalculateDeletionCost(deletedCard);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddPoints(-cost);
            Debug.Log($"[ShopManager] 扣除 {cost} 点，当前点数：{GameManager.Instance.currentPoints}");
        }

        // 3. 更新UI
        UpdateDeletionUI();
    }

    /// <summary>
    /// 计算单张卡牌的删除消耗
    /// </summary>
    private long CalculateDeletionCost(object card)
    {
        if (card is NumberCardInstance)
        {
            // 数字卡删除消耗：基础消耗 + 已删除数量 * 递增值
            // 示例：第1张数字卡消耗 5，第2张消耗 10，第3张消耗 20，以此类推
            int deletedCount = totalRemovedNumberCards;
            long cost = (long)(baseNumberCardRemoveCost * Mathf.Pow(2, deletedCount));

            Debug.Log($"[ShopManager] 计算数字卡删除消耗: " +
                      $"基础{baseNumberCardRemoveCost} * 2^{deletedCount} = {cost}");

            return cost;
        }
        else if (card is FormulaCardData)
        {
            int deletedCount = totalRemovedNumberCards;
            long cost = (long)(baseNumberCardRemoveCost * Mathf.Pow(2, deletedCount));
            Debug.Log($"[ShopManager] 公式卡删除消耗: {cost}");
            return cost;
        }

        Debug.LogWarning("[ShopManager] 未知的卡牌类型");
        return 0;
    }

    /// <summary>
    /// 更新删卡UI显示
    /// 显示下一次删卡的消耗提示
    /// </summary>
    private void UpdateDeletionUI()
    {
        deleteCostPanel.SetActive(true);
        deleteCostPanel.transform.SetAsLastSibling(); // 确保在最前面显示
        if (deleteCardCostText != null)
        {
            // 计算下一次删卡的消耗
            long nextCost = (long)(baseNumberCardRemoveCost * Mathf.Pow(2, totalRemovedNumberCards));

            deleteCardCostText.text = $"{nextCost}";

            Debug.Log($"[ShopManager] 更新UI - 下次删卡消耗: {nextCost}");
        }
        else
        {
            Debug.LogWarning("[ShopManager] deleteCardCostText 未绑定");
        }
    }

    /// <summary>
    /// 结束删卡模式
    /// 返回商店主界面
    /// 可以由返回按钮或自动流程调用
    /// </summary>
    public void EndCardDeletion()
    {
        Debug.Log("[ShopManager] 结束删卡模式");

        // 1. 清除标志位
        isDeletionMode = false;

        // 2. 结束CardSelectionManager的选择模式
        if (CardSelectionManager.Instance != null)
        {
            CardSelectionManager.Instance.EndCardSelection();
        }

        // 3. 隐藏卡牌面板
        if (UIManager.Instance != null)
        {
            if (UIManager.Instance.myNumberCardPanel != null)
                UIManager.Instance.myNumberCardPanel.SetActive(false);

            if (UIManager.Instance.myFormulaCardPanel != null)
                UIManager.Instance.myFormulaCardPanel.SetActive(false);
        }

        deleteCostPanel.SetActive(false);

        Debug.Log("[ShopManager] 删卡模式已结束，返回商店");
    }

    /// <summary>
    /// 取消删卡模式（用户点击取消或返回按钮）
    /// </summary>
    public void CancelCardDeletion()
    {
        Debug.Log("[ShopManager] 取消删卡模式");

        // 清除标志位和统计
        isDeletionMode = false;

        // 结束选择模式
        if (CardSelectionManager.Instance != null)
        {
            CardSelectionManager.Instance.CancelSelection();
        }

        // 隐藏面板并返回商店
        EndCardDeletion();
    }

    #endregion


    #region 槽位解锁逻辑
    /// <summary>
    /// 计算数字卡槽位解锁消耗
    /// </summary>
    public long CalculateNumberSlotUnlockCost()
    {
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

        int newSlotIndex = numberCardCount;  // 记录新槽位的索引
        numberCardCount++;

        Debug.Log($"数字卡槽位解锁成功！当前可购买数量：{numberCardCount}，累计解锁次数：{numberSlotUnlockTimes}");

        // 只生成新槽位的卡牌，不改变现有卡牌
        GenerateNewNumberCardSlot(newSlotIndex);

        // 只刷新UI，不改变卡牌数值
        UIManager.Instance.RefreshShopUI();

        return true;
    }
    /// <summary>
    /// 计算祝福卡槽位解锁消耗
    /// </summary>
    public long CalculateBlessingSlotUnlockCost()
    {
        long powerOfTwo = (long)Mathf.Pow(25, blessingSlotUnlockTimes);
        return baseBlessingSlotUnlockCost * powerOfTwo;
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
        int newSlotIndex = formulaCardCount;  // 记录新槽位的索引
        formulaCardCount++;

        Debug.Log($"公式卡槽位解锁成功！当前可购买数量：{formulaCardCount}，累计解锁次数：{formulaSlotUnlockTimes}");

        //只生成新槽位的卡牌，不改变现有卡牌
        GenerateNewFormulaCardSlot(newSlotIndex);

        // 只刷新UI，不改变卡牌数值
        UIManager.Instance.RefreshShopUI();

        return true;
    }
    /// <summary>
    /// 尝试解锁祝福卡槽位
    /// </summary>
    public bool TryUnlockBlessingSlot()
    {
        // 判定1：是否已达到最大槽位
        if (blessingCardCount >= MaxBlessingCardCount)
        {
            Debug.LogWarning("祝福卡槽位已解锁至最大值，无法继续解锁");
            return false;
        }

        // 判定2：计算消耗并校验点数
        long unlockCost = CalculateBlessingSlotUnlockCost();
        if (GameManager.Instance.currentPoints < unlockCost)
        {
            Debug.LogWarning($"祝福卡槽位解锁失败：点数不足，需要{unlockCost}，当前{GameManager.Instance.currentPoints}");
            return false;
        }

        // 执行解锁：扣除点数、更新解锁次数、增加可购买槽位数量
        GameManager.Instance.AddPoints(-unlockCost);
        blessingSlotUnlockTimes++;

        int newSlotIndex = blessingCardCount;  // 记录新槽位的索引
        blessingCardCount++;

        Debug.Log($"祝福卡槽位解锁成功！当前可购买数量：{blessingCardCount}，累计解锁次数：{blessingSlotUnlockTimes}");

        // 只生成新槽位的祝福，不改变现有祝福
        GenerateNewBlessingSlot(newSlotIndex);

        // 刷新 UI
        UIManager.Instance.RefreshShopUI();

        return true;
    }
    #endregion
    /// <summary>
    ///只生成一个新的公式卡槽位（用于解锁时调用）
    /// 不改变现有的卡牌数值
    /// </summary>
    void GenerateNewFormulaCardSlot(int slotIndex)
    {
        // 验证库是否存在
        if (formulaCardLibrary == null || formulaCardLibrary.allCards == null || formulaCardLibrary.allCards.Count == 0)
        {
            Debug.LogError("FormulaCardLibrary 未设置或为空！");
            return;
        }

        float priceMultiplier = BlessingManager.Instance != null ? BlessingManager.Instance.GetCurrentPriceMultiplier() : 1.0f;

        // 只生成一个新槽位，避免与现有卡牌重复
        FormulaCardData randomCard = formulaCardLibrary.GetRandomCard();

        // 将新卡牌添加到列表
        if (slotIndex < shopFormulaCards.Count)
        {
            shopFormulaCards[slotIndex] = new ShopItem<FormulaCardData>(randomCard, (long)(randomCard.CardPrice * priceMultiplier));
        }
        else
        {
            shopFormulaCards.Add(new ShopItem<FormulaCardData>(randomCard, (long)(randomCard.CardPrice * priceMultiplier)));
        }

        Debug.Log($"槽位{slotIndex}：{randomCard.Name}，价格 {randomCard.CardPrice}");
    }
    /// <summary>
    ///只生成一个新的数字卡槽位（用于解锁时调用）
    /// 不改变现有的卡牌数值
    /// </summary>
    void GenerateNewNumberCardSlot(int slotIndex)
    {
        // 验证库是否存在
        if (numberCardLibrary == null || numberCardLibrary.allCards == null || numberCardLibrary.allCards.Count == 0)
        {
            Debug.LogError("NumberCardLibrary 未设置或为空！");
            return;
        }

        // 只生成一个新槽位
        int randomIndex = Random.Range(0, numberCardLibrary.allCards.Count);
        NumberCardData randomCard = numberCardLibrary.allCards[randomIndex];

        // 推断布局类型
        randomCard.layoutType = InferLayoutType(randomCard);

        // 创建实例并计算价格
        NumberCardInstance instance = new NumberCardInstance(randomCard);
        float priceMultiplier = BlessingManager.Instance != null ? BlessingManager.Instance.GetCurrentPriceMultiplier() : 1.0f;
        long price = (long)(instance.GetNumberCardPrice(randomCard) * priceMultiplier);
        
        // 将新卡牌添加到列表
        if (slotIndex < shopNumberCards.Count)
        {
            shopNumberCards[slotIndex] = new ShopItem<NumberCardInstance>(instance, price);
        }
        else
        {
            shopNumberCards.Add(new ShopItem<NumberCardInstance>(instance, price));
        }

        Debug.Log($"槽位{slotIndex}：{randomCard.cardName}，价格 {price}");
    }
    /// <summary>
    /// 只生成一个新的祝福槽位（用于解锁时调用）
    /// 不改变现有的祝福数值
    /// </summary>
    void GenerateNewBlessingSlot(int slotIndex)
    {
        // 验证库是否存在
        if (blessingLibrary == null || blessingLibrary.GetAllBlessings().Count == 0)
        {
            Debug.LogError("BlessingLibrary 未设置或为空！");
            return;
        }

        float priceMultiplier = BlessingManager.Instance != null
            ? BlessingManager.Instance.GetCurrentPriceMultiplier()
            : 1.0f;

        // 随机获取一个祝福
        BlessingData randomBlessing = blessingLibrary.GetRandomBlessing();

        // 计算价格
        int currentCount = BlessingManager.Instance.GetBlessingCount(randomBlessing.blessingId);
        int price = randomBlessing.CalculatePrice(currentCount, priceMultiplier);

        // 将新祝福添加到列表
        if (slotIndex < shopBlessings.Count)
        {
            shopBlessings[slotIndex] = new ShopItem<BlessingData>(randomBlessing, price);
        }
        else
        {
            shopBlessings.Add(new ShopItem<BlessingData>(randomBlessing, price));
        }

        Debug.Log($"祝福槽位{slotIndex}：{randomBlessing.blessingName}，价格 {price}");
    }
}
