using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random; // 明确指定使用 UnityEngine.Random
using System.Numerics;
/// <summary>
/// 商店系统。读取database中的商品信息，读取玩家信息，处理购买逻辑Buy
/// </summary>


//商店购买系统
[System.Serializable]
public class ShopItem<T>
{
    public T cardData;
    public BigInteger price;
    public bool sold;

    public ShopItem(T data, BigInteger price)
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
    public int numberCardCount = 3;
    public int formulaCardCount = 1;
    public int blessingCardCount = 2;

    //刷新次数
    public int refreshCount = 0;

    //删除卡牌相关配置
    [Header("删除功能配置")]
    public Button deleteCardButton;          // 删除卡牌按钮
    public Text deleteCardCostText;          // 显示删除消耗的文本CalculatePrice
    public GameObject deleteCostPanel;
    public int totalRemovedNumberCards = 0;
    public int totalRemovedFormulaCards = 0;
    public int baseNumberCardRemoveCost = 10;
    public int baseFormulaCardRemoveCost = 200;

    [Header("槽位解锁配置")]
    public BigInteger baseNumberSlotUnlockCost = 20; // 数字卡槽位基础解锁消耗
    public BigInteger baseFormulaSlotUnlockCost = 5000; // 公式卡槽位基础解锁消耗
    public BigInteger baseBlessingSlotUnlockCost = 2000; //祝福卡槽位基础解锁消耗
    public int blessingSlotUnlockTimes = 0;
    public int numberSlotUnlockTimes = 0; // 数字卡已解锁次数
    public int formulaSlotUnlockTimes = 0; // 公式卡已解锁次数

    [Header("冷却配置")]
    [SerializeField] private float purchaseCooldown = 0.2f;    // 购买冷却
    [SerializeField] private float unlockCooldown = 0.1f;      // 解锁冷却
    [SerializeField] private float cardDeletionCooldown = 0.06f;     // 删卡冷却

    [Tooltip("公式卡库 - 拖入 FormulaCardLibrary 资源")]
    public FormulaCardLibrary formulaCardLibrary;

    [Header("卡牌库引用")]
    public NumberCardLibrary numberCardLibrary; // 数字卡库的引用

    [Header("祝福系统")]
    [Tooltip("祝福卡库 - 拖入 BlessingLibrary 资源")]
    public BlessingLibrary blessingLibrary;

    public ShowMyFormula showFormula;
    public ShowMyNumberCard showNumberCard;

    [Header("本次商店商品")]
    public List<ShopItem<NumberCardInstance>> shopNumberCards = new();
    public List<ShopItem<FormulaCardData>> shopFormulaCards = new();
    public List<ShopItem<BlessingData>> shopBlessings = new();

    // 当前是否处于删卡模式
    public bool isDeletionMode = false;

    
    /// <summary>
    /// 本回合已在当前商店刷新中显示过的祝福ID（用于CurrentRoundOnly类型）
    /// </summary>
    private HashSet<int> currentRoundPurchasedBlessings = new HashSet<int>();


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
        BigInteger refreshCost = CalculateRefreshCost();
        refreshCostText.text = "$ " + FormatBigNumber(refreshCost);
    }

    public void InitializeShop()
    {
        numberSlotUnlockTimes = 0; // 数字卡已解锁次数
        formulaSlotUnlockTimes = 0; // 公式卡已解锁次数
        blessingSlotUnlockTimes = 0;

        totalRemovedFormulaCards = 0;
        totalRemovedNumberCards = 0;

        numberCardCount = 3;
        formulaCardCount = 1;
        blessingCardCount = 2;

        isDeletionMode = false;

        ResetCurrentRoundBlessings();
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
        float priceMultiplier = GetCurrentNumberCardPriceMultiplier();

        // 创建临时池，抽到后移除，避免同一批商品重复
        List<NumberCardData> tempPool = new List<NumberCardData>(numberCardLibrary.allCards);

        // 生成所有槽位（包括锁定的）
        for (int i = 0; i < MaxnumberCardCount; i++)
        {
            NumberCardData randomCard = DrawNumberCardFromPool(tempPool);
            shopNumberCards.Add(CreateNumberShopItem(randomCard, priceMultiplier));
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

    #region 抽卡
    private NumberCardData DrawNumberCardFromPool(List<NumberCardData> pool)
    {
        if (pool == null || pool.Count == 0)
        {
            Debug.LogWarning("[ShopManager] 可用数字卡不足，无法继续生成不重复商品");
            return null;
        }

        int randomIndex = Random.Range(0, pool.Count);
        NumberCardData card = pool[randomIndex];
        pool.RemoveAt(randomIndex);
        return card;
    }

    private FormulaCardData DrawFormulaCardFromPool(List<FormulaCardData> pool)
    {
        if (pool == null || pool.Count == 0)
        {
            Debug.LogWarning("[ShopManager] 可用公式卡不足，无法继续生成不重复商品");
            return null;
        }

        int randomIndex = Random.Range(0, pool.Count);
        FormulaCardData card = pool[randomIndex];
        pool.RemoveAt(randomIndex);
        return card;
    }

    private BlessingData DrawBlessingFromPool(List<BlessingData> pool)
    {
        if (pool == null || pool.Count == 0)
        {
            Debug.LogWarning("[ShopManager] 可用祝福不足，无法继续生成不重复商品");
            return null;
        }

        int randomIndex = Random.Range(0, pool.Count);
        BlessingData blessing = pool[randomIndex];
        pool.RemoveAt(randomIndex);
        return blessing;
    }

    private ShopItem<NumberCardInstance> CreateNumberShopItem(NumberCardData card, float priceMultiplier)
    {
        if (card == null)
        {
            return new ShopItem<NumberCardInstance>(null, 0);
        }

        card.layoutType = InferLayoutType(card);
        NumberCardInstance instance = new NumberCardInstance(card);
        long price = (long)(instance.GetNumberCardPrice(card) * priceMultiplier);
        return new ShopItem<NumberCardInstance>(instance, price);
    }

    private ShopItem<FormulaCardData> CreateFormulaShopItem(FormulaCardData card, float priceMultiplier)
    {
        if (card == null)
        {
            return new ShopItem<FormulaCardData>(null, 0);
        }

        long finalPrice = (long)(card.CardPrice * priceMultiplier);
        return new ShopItem<FormulaCardData>(card, finalPrice);
    }

    private ShopItem<BlessingData> CreateBlessingShopItem(BlessingData blessing, float priceMultiplier)
    {
        if (blessing == null)
        {
            return new ShopItem<BlessingData>(null, 0);
        }

        int currentCount = BlessingManager.Instance.GetBlessingCount(blessing.blessingId);
        BigInteger price = (BigInteger)blessing.CalculatePrice(currentCount, priceMultiplier);
        return new ShopItem<BlessingData>(blessing, price);
    }

    private List<NumberCardData> BuildAvailableNumberCardPool(int ignoredSlotIndex = -1)
    {
        List<NumberCardData> pool = new List<NumberCardData>(numberCardLibrary.allCards);

        for (int i = 0; i < shopNumberCards.Count; i++)
        {
            if (i == ignoredSlotIndex) continue;

            if (shopNumberCards[i] == null) continue;

            NumberCardInstance instance = shopNumberCards[i].cardData;
            if (instance != null && instance.cardData != null)
            {
                RemoveNumberCardFromPool(pool, instance.cardData);
            }
        }

        return pool;
    }

    private List<FormulaCardData> BuildAvailableFormulaCardPool(int ignoredSlotIndex = -1)
    {
        List<FormulaCardData> pool = new List<FormulaCardData>(formulaCardLibrary.allCards);

        for (int i = 0; i < shopFormulaCards.Count; i++)
        {
            if (i == ignoredSlotIndex) continue;

            if (shopFormulaCards[i] == null) continue;

            FormulaCardData card = shopFormulaCards[i].cardData;
            if (card != null)
            {
                RemoveFormulaCardFromPool(pool, card);
            }
        }

        return pool;
    }

    private void RemoveDisplayedBlessingsFromPool(List<BlessingData> pool, int ignoredSlotIndex = -1)
    {
        for (int i = 0; i < shopBlessings.Count; i++)
        {
            if (i == ignoredSlotIndex) continue;

            if (shopBlessings[i] == null) continue;

            BlessingData blessing = shopBlessings[i].cardData;
            if (blessing != null)
            {
                RemoveBlessingFromPool(pool, blessing);
            }
        }
    }

    private void RemoveNumberCardFromPool(List<NumberCardData> pool, NumberCardData card)
    {
        for (int i = pool.Count - 1; i >= 0; i--)
        {
            if (pool[i] == card)
            {
                pool.RemoveAt(i);
            }
        }
    }

    private void RemoveFormulaCardFromPool(List<FormulaCardData> pool, FormulaCardData card)
    {
        for (int i = pool.Count - 1; i >= 0; i--)
        {
            if (pool[i] == card || pool[i].FormulaCardId == card.FormulaCardId)
            {
                pool.RemoveAt(i);
            }
        }
    }

    private void RemoveBlessingFromPool(List<BlessingData> pool, BlessingData blessing)
    {
        for (int i = pool.Count - 1; i >= 0; i--)
        {
            if (pool[i] == blessing || pool[i].blessingId == blessing.blessingId)
            {
                pool.RemoveAt(i);
            }
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
        float priceMultiplier = GetCurrentFormulaCardPriceMultiplier();

        // 创建临时池，避免重复抽取
        List<FormulaCardData> tempPool = new List<FormulaCardData>(formulaCardLibrary.allCards);

        // 生成所有槽位
        for (int i = 0; i < MaxformulaCardCount; i++)
        {
            FormulaCardData randomCard = DrawFormulaCardFromPool(tempPool);
            shopFormulaCards.Add(CreateFormulaShopItem(randomCard, priceMultiplier));
        }
    }

    /// <summary>
    /// 生成祝福商品
    /// </summary>
    void GenerateBlessings()
    {
        shopBlessings.Clear();

        // 获取价格乘数（如果有祝福影响价格的话）
        float priceMultiplier = GetCurrentBlessingPriceMultiplier();

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
            BigInteger price = nihilism.CalculatePrice(BlessingManager.Instance.GetBlessingCount(nihilism.blessingId), priceMultiplier);

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
            priceMultiplier = GetCurrentBlessingPriceMultiplier();
            int currentCount = BlessingManager.Instance.GetBlessingCount(wishBlessing.blessingId);
            BigInteger price = wishBlessing.CalculatePrice(currentCount, priceMultiplier);

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
        RemoveDisplayedBlessingsFromPool(availableBlessings);
        
        int remainingSlots = MaxBlessingCardCount - wishAdded;//减去许愿币占位
        
        // 生成4个槽位（包括锁定的）
        for (int i = 0; i < remainingSlots; i++)
        {
            int slotIndex = i + wishAdded;

            // 如果是解锁的槽位
            if (slotIndex < blessingCardCount)
            {
                // 从可用池中选择祝福
                if (availableBlessings.Count > 0)
                {
                    BlessingData selectedBlessing = DrawBlessingFromPool(availableBlessings);
                    ShopItem<BlessingData> item = CreateBlessingShopItem(selectedBlessing, priceMultiplier);

                    shopBlessings.Add(item);
                    Debug.Log($"祝福槽位{slotIndex}：{selectedBlessing.blessingName}（{selectedBlessing.refreshBehavior}），价格 {item.price}");
                }
                else
                {
                    // 可用祝福不足，添加空槽位
                    shopBlessings.Add(new ShopItem<BlessingData>(null, 0));
                    Debug.LogWarning($"[ShopManager] 可用祝福不足！槽位{slotIndex}无法填充");
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
                    isAvailable = !currentRoundPurchasedBlessings.Contains(blessing.blessingId);
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
    #endregion
    /// <summary>
    /// 重置本回合显示过的祝福
    /// </summary>
    private void ResetCurrentRoundBlessings()
    {
        currentRoundPurchasedBlessings.Clear();
        Debug.Log("[ShopManager] 本回合已显示的祝福记录已清空");
    }
    #region 买卡逻辑
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
        if(BlessingManager.Instance.ApplyPragmatism==1){
            BlessingManager.Instance.ApplyPragmatismEffect();
        }
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

            if (item.cardData.refreshBehavior == BlessingData.RefreshBehavior.CurrentRoundOnly)
            {
                currentRoundPurchasedBlessings.Add(item.cardData.blessingId);
            }

            // 刷新商店显示
            UIManager.Instance.RefreshShopUI();
        }

        return purchaseSuccess;
    }
    #endregion
    //商店刷新
    public void RefreshShop()
    {
        BigInteger refreshCost = CalculateRefreshCost();

        if (GameManager.Instance.currentPoints < refreshCost)
        {
            Debug.Log("点数不足");
            return;
        }

        GameManager.Instance.AddPoints(-refreshCost);
        refreshCostText.text = "$ " + FormatBigNumber(refreshCost);
        refreshCount++;
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
        // 每次离开商店时，重置当回合展示过的记录
        ResetCurrentRoundBlessings();

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

            // 通知ShowMyNumberCard进入删卡模式，初始化成本显示
            if (showNumberCard != null)
            {
                showNumberCard.EnterDeletionMode();
            }
        }
        else
        {
            Debug.LogError("[ShopManager] UIManager 或 myNumberCardPanel 为空");
        }

    }


    /// <summary>
    /// 处理单张卡牌被删除的事件
    /// 由ShowMyNumberCard.cs或ShowMyFormula.cs调用
    /// </summary>
    public bool OnCardDeleted(object deletedCard)
    {
        if (deletedCard == null)
        {
            Debug.LogError("[ShopManager] OnCardDeleted: 删除的卡牌为空");
            return false;
        }

        // 冷却检查
        if (CooldownManager.Instance != null &&
            CooldownManager.Instance.IsInCooldown(CooldownManager.CooldownType.CardDeletion))
        {
            Debug.LogWarning($"[ShopManager] 删卡操作在冷却中");
            return false;
        }

        // 1. 先计算本次消耗
        BigInteger cost = CalculateDeletionCost(deletedCard);

        // 2. 点数检查（关键：先判断，再操作）
        if (GameManager.Instance == null || GameManager.Instance.currentPoints < cost)
        {
            Debug.LogWarning($"点数不足，无法删除卡牌。需要 {cost} 点");
            return false;
        }

        // 3. 点数足够，才执行删除、扣点、计数+1
        GameManager.Instance.AddPoints(-cost);
        Debug.Log($"[ShopManager] 扣除 {cost} 点，当前点数：{GameManager.Instance.currentPoints}");

        // 4. 只在成功删除时自增计数
        if (deletedCard is NumberCardInstance)
        {
            totalRemovedNumberCards++;
        }
        else if (deletedCard is FormulaCardData)
        {
            totalRemovedFormulaCards++;
        }

        // 5. 更新UI与冷却
        UpdateDeletionUI(cost);
        UIManager.Instance.UpdatePointsDisplay(GameManager.Instance.currentPoints);

        if (CooldownManager.Instance != null)
        {
            CooldownManager.Instance.StartCooldown(
                CooldownManager.CooldownType.CardDeletion,
                cardDeletionCooldown
            );
        }

        return true;
    }
    #region 删卡成本计算逻辑
    /// <summary>
    /// 获取下一次数字卡删除的消耗
    /// </summary>
    public BigInteger GetNextNumberCardDeletionCost()
    {
        BigInteger cost = baseNumberCardRemoveCost;
        for (int i = 0; i < totalRemovedNumberCards; i++) cost *= 5;
        return ApplyMultiplier(cost, GetCurrentDeletionPriceMultiplier());
    }

    /// <summary>
    /// 获取下一次公式卡删除的消耗
    /// </summary>
    public BigInteger GetNextFormulaCardDeletionCost()
    {
        BigInteger cost = baseFormulaCardRemoveCost;
        for (int i = 0; i < totalRemovedFormulaCards; i++) cost *= 5;
        return ApplyMultiplier(cost, GetCurrentDeletionPriceMultiplier());
    }

    /// <summary>
    /// 重构原有的 CalculateDeletionCost，让其调用上面的新方法以保持代码整洁
    /// </summary>
    public BigInteger CalculateDeletionCost(object card)
    {
        if (card is NumberCardInstance)
        {
            return GetNextNumberCardDeletionCost();
        }
        else if (card is FormulaCardData)
        {
            return GetNextFormulaCardDeletionCost();
        }
        Debug.LogWarning("[ShopManager] 未知的卡牌类型");
        return 0;
    }
    #endregion

    /// <summary>
    /// 更新删卡UI显示
    /// 显示下一次删卡的消耗提示
    /// </summary>
    private void UpdateDeletionUI(BigInteger cost)
    {
        deleteCostPanel.SetActive(true);
        deleteCostPanel.transform.SetAsLastSibling(); // 确保在最前面显示

        deleteCardCostText.text = FormatBigNumber(cost).ToString();
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

        showFormula.deletionCostPanel.SetActive(false);
        showNumberCard.deletionCostPanel.SetActive(false);

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
    public BigInteger CalculateNumberSlotUnlockCost()
    {
        BigInteger finalNumberSlotUnlockCost = 0 ;
        if (numberSlotUnlockTimes == 0)
            finalNumberSlotUnlockCost = 20;
        else if (numberSlotUnlockTimes == 1)
            finalNumberSlotUnlockCost = 500;
        else if (numberSlotUnlockTimes == 2)
            finalNumberSlotUnlockCost = 10000;
        return finalNumberSlotUnlockCost;
    }

    public BigInteger CalculateFormulaSlotUnlockCost()
    {
        BigInteger cost = 5000;
        for (int i = 0; i < formulaSlotUnlockTimes; i++)
            cost *= 2;
        return cost;
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
        // 冷却检查：防止连续快速点击
        if (CooldownManager.Instance != null &&
            CooldownManager.Instance.IsInCooldown(CooldownManager.CooldownType.SlotUnlock))
        {
            Debug.LogWarning($"[ShopManager] 槽位解锁操作在冷却中，剩余时间: {CooldownManager.Instance.GetRemainingTime(CooldownManager.CooldownType.SlotUnlock):F2}秒");
            return false;
        }
        // 判定1：是否已达到最大槽位
        if (numberCardCount >= MaxnumberCardCount)
        {
            Debug.LogWarning("数字卡槽位已解锁至最大值，无法继续解锁");
            return false;
        }

        // 判定2：计算消耗并校验点数
        BigInteger unlockCost = CalculateNumberSlotUnlockCost();
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

        // 开始冷却，防止连续点击
        if (CooldownManager.Instance != null)
        {
            CooldownManager.Instance.StartCooldown(
                CooldownManager.CooldownType.SlotUnlock,
                unlockCooldown
            );
        }
        return true;
    }

    /// <summary>
    /// 计算祝福卡槽位解锁消耗
    /// </summary>
    public BigInteger CalculateBlessingSlotUnlockCost()
    {
        BigInteger cost = baseBlessingSlotUnlockCost;
        for (int i = 0; i < blessingSlotUnlockTimes; i++)
            cost *= 25;
        return cost;
    }
    /// <summary>
    /// 尝试解锁公式卡槽位
    /// </summary>
    /// <returns>解锁成功返回true，失败返回false</returns>
    public bool TryUnlockFormulaSlot()
    {
        // 冷却检查：防止连续快速点击
        if (CooldownManager.Instance != null &&
            CooldownManager.Instance.IsInCooldown(CooldownManager.CooldownType.SlotUnlock))
        {
            Debug.LogWarning($"[ShopManager] 槽位解锁操作在冷却中，剩余时间: {CooldownManager.Instance.GetRemainingTime(CooldownManager.CooldownType.SlotUnlock):F2}秒");
            return false;
        }
        // 判定1：是否已达到最大槽位
        if (formulaCardCount >= MaxformulaCardCount)
        {
            Debug.LogWarning("公式卡槽位已解锁至最大值，无法继续解锁");
            return false;
        }

        // 判定2：计算消耗并校验点数
        BigInteger unlockCost = CalculateFormulaSlotUnlockCost();
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

        // 开始冷却，防止连续点击
        if (CooldownManager.Instance != null)
        {
            CooldownManager.Instance.StartCooldown(
                CooldownManager.CooldownType.SlotUnlock,
                unlockCooldown
            );
        }

        return true;
    }
    /// <summary>
    /// 尝试解锁祝福卡槽位
    /// </summary>
    public bool TryUnlockBlessingSlot()
    {
        // 冷却检查：防止连续快速点击
        if (CooldownManager.Instance != null &&
            CooldownManager.Instance.IsInCooldown(CooldownManager.CooldownType.SlotUnlock))
        {
            Debug.LogWarning($"[ShopManager] 槽位解锁操作在冷却中，剩余时间: {CooldownManager.Instance.GetRemainingTime(CooldownManager.CooldownType.SlotUnlock):F2}秒");
            return false;
        }
        // 判定1：是否已达到最大槽位
        if (blessingCardCount >= MaxBlessingCardCount)
        {
            Debug.LogWarning("祝福卡槽位已解锁至最大值，无法继续解锁");
            return false;
        }

        // 判定2：计算消耗并校验点数
        BigInteger unlockCost = CalculateBlessingSlotUnlockCost();
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

        // 开始冷却，防止连续点击
        if (CooldownManager.Instance != null)
        {
            CooldownManager.Instance.StartCooldown(
                CooldownManager.CooldownType.SlotUnlock,
                unlockCooldown
            );
        }

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

        float priceMultiplier = GetCurrentFormulaCardPriceMultiplier();

        List<FormulaCardData> availableCards = BuildAvailableFormulaCardPool(slotIndex);
        FormulaCardData randomCard = DrawFormulaCardFromPool(availableCards);
        ShopItem<FormulaCardData> item = CreateFormulaShopItem(randomCard, priceMultiplier);

        // 将新卡牌添加到列表
        if (slotIndex < shopFormulaCards.Count)
            shopFormulaCards[slotIndex] = item;
        else
            shopFormulaCards.Add(item);

        if (randomCard != null)
            Debug.Log($"槽位{slotIndex}：{randomCard.Name}，价格 {item.price}");
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

        float priceMultiplier = GetCurrentNumberCardPriceMultiplier();
        List<NumberCardData> availableCards = BuildAvailableNumberCardPool(slotIndex);
        NumberCardData randomCard = DrawNumberCardFromPool(availableCards);
        ShopItem<NumberCardInstance> item = CreateNumberShopItem(randomCard, priceMultiplier);
        
        // 将新卡牌添加到列表
        if (slotIndex < shopNumberCards.Count)
            shopNumberCards[slotIndex] = item;
        else
            shopNumberCards.Add(item);

        if (randomCard != null)
            Debug.Log($"槽位{slotIndex}：{randomCard.cardName}，价格 {item.price}");
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

        float priceMultiplier = GetCurrentBlessingPriceMultiplier();

        List<BlessingData> availableBlessings = BuildAvailableBlessingPool();
        RemoveDisplayedBlessingsFromPool(availableBlessings, slotIndex);

        BlessingData randomBlessing = DrawBlessingFromPool(availableBlessings);
        ShopItem<BlessingData> item = CreateBlessingShopItem(randomBlessing, priceMultiplier);

        // 将新祝福添加到列表
        if (slotIndex < shopBlessings.Count)
            shopBlessings[slotIndex] = item;
        else
            shopBlessings.Add(item);

        if (randomBlessing != null)
            Debug.Log($"祝福槽位{slotIndex}：{randomBlessing.blessingName}，价格 {item.price}");
    }

    #region 冷却系统接口
    // ===== 刷新商店的冷却配置 =====
    public void SetCooldownDuration(float newPurchaseCooldown, float newUnlockCooldown)
    {
        purchaseCooldown = Mathf.Max(0.1f, newPurchaseCooldown);
        unlockCooldown = Mathf.Max(0.1f, newUnlockCooldown);

        if (CooldownManager.Instance != null)
        {
            CooldownManager.Instance.SetCooldownDuration(
                CooldownManager.CooldownType.ShopPurchase,
                purchaseCooldown
            );
            CooldownManager.Instance.SetCooldownDuration(
                CooldownManager.CooldownType.SlotUnlock,
                unlockCooldown
            );
            CooldownManager.Instance.SetCooldownDuration(
                CooldownManager.CooldownType.CardDeletion,
                cardDeletionCooldown
            );
        }

        Debug.Log($"[ShopManager] 已更新冷却配置 - 购买冷却: {purchaseCooldown}秒, 解锁冷却: {unlockCooldown}秒");
    }

    // ===== 重置所有冷却（场景切换时） =====
    public void ResetAllCooldowns()
    {
        if (CooldownManager.Instance != null)
        {
            CooldownManager.Instance.ResetCooldown(CooldownManager.CooldownType.ShopPurchase);
            CooldownManager.Instance.ResetCooldown(CooldownManager.CooldownType.SlotUnlock);
            Debug.Log("[ShopManager] 已重置所有冷却");
        }
    }
    #endregion  

    private float GetBaseBlessingPriceMultiplier()
    {
        return BlessingManager.Instance != null
            ? BlessingManager.Instance.GetCurrentPriceMultiplier()
            : 1f;
    }

    private float GetDifficultyMultiplier(DifficultySettingType settingType)
    {
        if (DataSavingManager.Instance == null)
        {
            return 1f;
        }

        return DataSavingManager.Instance.GetDifficultyMultiplier(settingType);
    }

    public float GetCurrentNumberCardPriceMultiplier()
    {
        return GetBaseBlessingPriceMultiplier() * GetDifficultyMultiplier(DifficultySettingType.NumberCardPrice);
    }

    public float GetCurrentFormulaCardPriceMultiplier()
    {
        return GetBaseBlessingPriceMultiplier() * GetDifficultyMultiplier(DifficultySettingType.FormulaCardPrice) * 100;
    }

    public float GetCurrentBlessingPriceMultiplier()
    {
        return GetBaseBlessingPriceMultiplier() * GetDifficultyMultiplier(DifficultySettingType.BlessingPrice);
    }

    public float GetCurrentDeletionPriceMultiplier()
    {
        return GetDifficultyMultiplier(DifficultySettingType.CardDeletionPrice);
    }

    public float GetCurrentRefreshPriceMultiplier()
    {
        return GetDifficultyMultiplier(DifficultySettingType.ShopRefreshPrice);
    }

    private BigInteger ApplyMultiplier(BigInteger value, float multiplier)
    {
        if (multiplier <= 0f)
        {
            return value;
        }

        return value * (BigInteger)(multiplier * 100f) / 100;
    }

    public BigInteger CalculateRefreshCost()
    {
        int currentRound = GameManager.Instance.currentRound;
        BigInteger roundSquare = (BigInteger)currentRound * currentRound;
        BigInteger powerOfTwo = 1;
        for (int i = 0; i < refreshCount; i++) powerOfTwo *= 2;

        BigInteger refreshCost = roundSquare * powerOfTwo;
        refreshCost = ApplyMultiplier(refreshCost, GetCurrentRefreshPriceMultiplier());

        if (BlessingManager.Instance.HasRichTreasure == 1 && refreshCount < 1)
        {
            refreshCost = 0;
        }

        return refreshCost;
    }

    public void RefreshCurrentNumberCardPrices()
    {
        float priceMultiplier = GetCurrentNumberCardPriceMultiplier();

        foreach (var item in shopNumberCards)
        {
            if (item == null || item.cardData == null || item.cardData.cardData == null)
            {
                continue;
            }

            long originalPrice = item.cardData.GetNumberCardPrice(item.cardData.cardData);
            item.price = (long)(originalPrice * priceMultiplier);
        }
    }

    public void RefreshCurrentFormulaCardPrices()
    {
        float priceMultiplier = GetCurrentFormulaCardPriceMultiplier();

        foreach (var item in shopFormulaCards)
        {
            if (item == null || item.cardData == null)
            {
                continue;
            }

            item.price = (long)(item.cardData.CardPrice * priceMultiplier);
        }
    }

    public void RefreshCurrentBlessingPrices()
    {
        float priceMultiplier = GetCurrentBlessingPriceMultiplier();

        foreach (var item in shopBlessings)
        {
            if (item == null || item.cardData == null)
            {
                continue;
            }

            int count = BlessingManager.Instance.GetBlessingCount(item.cardData.blessingId);
            item.price = item.cardData.CalculatePrice(count, priceMultiplier);
        }
    }

    public void ApplyDifficultySettings()
    {
        RefreshCurrentNumberCardPrices();
        RefreshCurrentFormulaCardPrices();
        RefreshCurrentBlessingPrices();
        InitializeRefreshCost();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.RefreshShopUI();
        }
    }

    public string FormatBigNumber(BigInteger number)
    {
        return NumberDisplayFormatter.Format(number);
    }
}
