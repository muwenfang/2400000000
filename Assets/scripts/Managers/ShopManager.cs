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

    [Header("槽位解锁配置")]
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


        /// <summary>
        /// 在当前商店访问中被排除在刷新之外的祝福ID（如祸星，仅在首次生成时可能出现，后续刷新不再出现）
        /// </summary>
        private HashSet<int> blessingsExcludedFromRefresh = new HashSet<int>();

    public void OpenShop()
    {
        // 清除上次商店访问的排除列表（新回合/新进入商店时重置）
        blessingsExcludedFromRefresh.Clear();

        
        // 祸星：首次生成后即从后续刷新中排除（无论是否出现）
        MarkDisasterStarExcluded();
        
        GenerateNumberCards();
        GenerateFormulaCards();
        GenerateBlessings();
        InitializeDeletionUI();
        InitializeRefreshCost();

        // ---通知 UI 刷新 ---
        UIManager.Instance.RefreshShopUI();
    }
    /// <summary>
    /// 免费刷新商店（幸运星触发，不扣费、不增加刷新次数）
    /// </summary>
    public void FreeRefreshShop()
    {
        // 重新生成商品（不扣费、不增加刷新次数）
        ResetCurrentRoundBlessings();
        GenerateNumberCards();
        GenerateFormulaCards();
        GenerateBlessings();

        // 刷新 UI
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
        refreshCount = 0; // 重置刷新次数

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

        // 暗箱操作：强制加入目标祝福，扣除2倍原价点数
        BlessingData darkBoxBlessing = BlessingManager.Instance.GetDarkBoxTargetBlessing();
        int darkBoxAdded = 0;
        if (darkBoxBlessing != null)
        {
            priceMultiplier = GetCurrentBlessingPriceMultiplier();
            int currentCount = BlessingManager.Instance.GetBlessingCount(darkBoxBlessing.blessingId);
            BigInteger originalPrice = darkBoxBlessing.CalculatePrice(currentCount, priceMultiplier);

            // 扣除 2 倍原价点数
            BigInteger penaltyPoints = originalPrice * 2;
            GameManager.Instance.AddPoints(-penaltyPoints);
            Debug.Log($"暗箱操作生效：扣除 {penaltyPoints} 点数，强制刷新出【{darkBoxBlessing.blessingName}】");

            // 以正常价格加入商店
            shopBlessings.Add(new ShopItem<BlessingData>(darkBoxBlessing, originalPrice));

            // 消耗暗箱操作
            BlessingManager.Instance.ConsumeDarkBox();
            darkBoxAdded = 1;
        }
        

        if (blessingLibrary == null || blessingLibrary.GetAllBlessings().Count == 0)
        {
            Debug.LogError("BlessingLibrary 未设置或为空！");
            return;
        }

        // 构建可用祝福池（根据刷新行为过滤）
        List<BlessingData> availableBlessings = BuildAvailableBlessingPool();
        RemoveDisplayedBlessingsFromPool(availableBlessings);
        
        int remainingSlots = MaxBlessingCardCount - wishAdded - darkBoxAdded;//减去许愿币和暗箱操作占位
        
        // 生成所有槽位（包括锁定的）。
        // 锁定槽位同样填充真实祝福数据，锁定状态由 UI 层通过 blessingCardCount 判断，
        // 这样解锁槽位后无需重新生成商品即可正常显示（与数字卡/公式卡行为一致）。
        for (int i = 0; i < remainingSlots; i++)
        {
            int slotIndex = i + wishAdded + darkBoxAdded;

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

            // 检查是否在当前商店访问中被排除（如祸星仅在首次生成可用）
            if (blessingsExcludedFromRefresh.Contains(blessing.blessingId))
                continue;

            bool isAvailable = false;

            switch (blessing.refreshBehavior)
            {
                case BlessingData.RefreshBehavior.AlwaysRefresh:
                    // 总是可用
                    isAvailable = true;
                    break;

                case BlessingData.RefreshBehavior.NeverRefresh:
                    // 从未购买过 → 可用
                    if (!BlessingManager.Instance.HasBlessingEverBeenPurchased(blessing.blessingId))
                        isAvailable = true;
                    break;

                case BlessingData.RefreshBehavior.CurrentRoundOnly:
                    // 本回合还未显示过 → 可用
                    if (!currentRoundPurchasedBlessings.Contains(blessing.blessingId))
                        isAvailable = true;
                    break;
            }

            if (isAvailable)
            {
                availableBlessings.Add(blessing);
            }
        }

        return availableBlessings;
    }

    /// <summary>
    /// 重置本回合已购买的祝福记录（新回合开始时调用）
    /// </summary>
    public void ResetCurrentRoundBlessings()
    {
        currentRoundPurchasedBlessings.Clear();
    }

    /// <summary>
    /// 记录已购买的祝福（在当前商店刷新中）
    /// </summary>
    public void MarkBlessingAsPurchased(int blessingId)
    {
        currentRoundPurchasedBlessings.Add(blessingId);
    }

    /// <summary>
    /// 标记祸星为已排除（首次生成后调用，后续刷新不再出现）
    /// </summary>
    private void MarkDisasterStarExcluded()
    {
        BlessingData disasterStar = blessingLibrary.GetBlessingByType(BlessingData.BlessingType.DisasterStar);
        if (disasterStar != null)
            blessingsExcludedFromRefresh.Add(disasterStar.blessingId);
    }

    #endregion

    #region 刷新商店
    /// <summary>
    /// 刷新商店
    /// </summary>
    public void RefreshShop()
    {
        // 计算刷新费用
        BigInteger refreshCost = CalculateRefreshCost();

        // 检查点数是否足够
        if (GameManager.Instance.currentPoints < refreshCost && BlessingManager.Instance.HasRichTreasure == 0)
        {
            Debug.Log("点数不足，无法刷新商店");
            return;
        }

        // 扣除刷新费用（丰盈宝库永久免费）
        if (BlessingManager.Instance.HasRichTreasure == 0)
        {
            GameManager.Instance.AddPoints(-refreshCost);
        }

        refreshCount++;

        // 更新刷新费用显示
        InitializeRefreshCost();

        // 重新生成商品
        ResetCurrentRoundBlessings();
        GenerateNumberCards();
        GenerateFormulaCards();
        GenerateBlessings();

        // 刷新 UI
        UIManager.Instance.RefreshShopUI();
    }

    /// <summary>
    /// 计算刷新费用
    /// </summary>
    BigInteger CalculateRefreshCost()
    {
        if (BlessingManager.Instance != null && BlessingManager.Instance.HasRichTreasure == 1)
        {
            return 0; // 丰盈宝库：永久免费
        }

        // 公式: i² × 2^(n-1)，i=当前回合数，n=刷新次数(第1次刷新时n=1)
        int currentRound = GameManager.Instance != null ? GameManager.Instance.currentRound : 1;
        BigInteger roundSquared = (BigInteger)currentRound * currentRound;
        BigInteger refreshMultiplier = BigInteger.Pow(2, refreshCount); // refreshCount=0时 2^0=1
        return roundSquared * refreshMultiplier;
    }

    /// <summary>
    /// 格式化大数字显示
    /// </summary>
    public string FormatBigNumber(BigInteger number)
    {
        return NumberDisplayFormatter.Format(number);
    }

    /// <summary>
    /// 初始化删除卡牌 UI
    /// </summary>
    public void InitializeDeletionUI()
    {
        if (deleteCardButton == null) return;

        // 计算删除消耗
        BigInteger deleteCost = CalculateDeletionCost();

        // 显示删除消耗
        if (deleteCardCostText != null)
        {
            deleteCardCostText.text = "$ " + FormatBigNumber(deleteCost);
        }

        // 设置删除按钮事件
        deleteCardButton.onClick.RemoveAllListeners();
        deleteCardButton.onClick.AddListener(() =>
        {
            if (CooldownManager.Instance != null &&
                CooldownManager.Instance.IsInCooldown(CooldownManager.CooldownType.CardDeletion))
            {
                Debug.LogWarning($"[ShopManager] 卡牌删除操作在冷却中（冷却时间：{CooldownManager.Instance.GetRemainingTime(CooldownManager.CooldownType.CardDeletion):F1}秒）");
                return;
            }

            // 开始冷却
            if (CooldownManager.Instance != null)
            {
                CooldownManager.Instance.StartCooldown(
                    CooldownManager.CooldownType.CardDeletion,
                    cardDeletionCooldown
                );
            }

            // 启动删除流程
            StartDeletion();
        });
    }

    /// <summary>
    /// 计算删除卡牌消耗
    /// </summary>
    public BigInteger CalculateDeletionCost()
    {
        int totalRemoved = totalRemovedNumberCards + totalRemovedFormulaCards;
        BigInteger cost = 1 * (BigInteger)(totalRemoved * totalRemoved);
        return cost;
    }

    /// <summary>
    /// 获取下一个数字卡删除费用（同CalculateDeletionCost）
    /// </summary>
    public BigInteger GetNextNumberCardDeletionCost()
    {
        return CalculateDeletionCost();
    }

    /// <summary>
    /// 计算删除卡牌消耗（带参数版本，与无参版本一致）
    /// </summary>
    public BigInteger CalculateDeletionCost(NumberCardInstance card)
    {
        return CalculateDeletionCost();
    }

    /// <summary>
    /// 卡牌删除回调 - 检查点数、冷却，执行扣除
    /// </summary>
    public bool OnCardDeleted(NumberCardInstance cardToDelete)
    {
        // 检查冷却
        if (CooldownManager.Instance != null &&
            CooldownManager.Instance.IsInCooldown(CooldownManager.CooldownType.CardDeletion))
        {
            Debug.LogWarning("[ShopManager] 卡牌删除操作在冷却中");
            return false;
        }

        // 计算删除费用
        BigInteger deleteCost = CalculateDeletionCost();
        if (GameManager.Instance.currentPoints < deleteCost)
        {
            Debug.Log("点数不足，无法删除卡牌");
            return false;
        }

        // 扣除点数
        GameManager.Instance.AddPoints(-deleteCost);

        // 增加删除计数
        totalRemovedNumberCards++;

        // 开始冷却
        if (CooldownManager.Instance != null)
        {
            CooldownManager.Instance.StartCooldown(
                CooldownManager.CooldownType.CardDeletion,
                cardDeletionCooldown
            );
        }

        // 刷新 UI
        InitializeDeletionUI();
        return true;
    }

    /// <summary>
    /// 开始删除卡牌
    /// </summary>
    void StartDeletion()
    {
        isDeletionMode = true;
        CardSelectionManager.Instance.StartCardSelection(
            CardSelectionManager.SelectionMode.RemoveCard,
            OnCardSelectedForDeletion
        );
    }

    /// <summary>
    /// 删除卡牌选择回调
    /// </summary>
    void OnCardSelectedForDeletion(object selectedObject)
    {
        isDeletionMode = false;

        // 实际的删除操作已通过 CardClickHandler → ShowMyXxx.TryHandleDeleteClick →
        // ShopManager.OnCardDeleted / OnFormulaCardDeleted 路径处理
        // 此处仅做状态清理，避免双重扣费
    }
    #endregion

    #region 获取价格乘数
    /// <summary>
    /// 获取数字卡价格乘数
    /// </summary>
    public float GetCurrentNumberCardPriceMultiplier()
    {
        float multiplier = 1.0f;

        if (DataSavingManager.Instance != null)
        {
            multiplier *= DataSavingManager.Instance.GetDifficultyMultiplier(DifficultySettingType.NumberCardPrice);
            multiplier *= DataSavingManager.Instance.GetDifficultyMultiplier(DifficultySettingType.ShopRefreshPrice);
        }

        return multiplier;
    }

    /// <summary>
    /// 获取公式卡价格乘数
    /// </summary>
    public float GetCurrentFormulaCardPriceMultiplier()
    {
        float multiplier = 1.0f;

        if (DataSavingManager.Instance != null)
        {
            multiplier *= DataSavingManager.Instance.GetDifficultyMultiplier(DifficultySettingType.FormulaCardPrice);
            multiplier *= DataSavingManager.Instance.GetDifficultyMultiplier(DifficultySettingType.ShopRefreshPrice);
        }

        return multiplier;
    }

    /// <summary>
    /// 获取祝福价格乘数
    /// </summary>
    public float GetCurrentBlessingPriceMultiplier()
    {
        float multiplier = 1.0f;

        if (DataSavingManager.Instance != null)
        {
            multiplier *= DataSavingManager.Instance.GetDifficultyMultiplier(DifficultySettingType.BlessingPrice);
            multiplier *= DataSavingManager.Instance.GetDifficultyMultiplier(DifficultySettingType.ShopRefreshPrice);
        }

        return multiplier;
    }
    #endregion

    #region 购买逻辑
    /// <summary>
    /// 尝试购买数字卡
    /// </summary>
    public bool TryBuyNumberCard(ShopItem<NumberCardInstance> item)
    {
        if (item == null || item.sold)
        {
            Debug.LogWarning("[ShopManager] 数字卡无效或已售出");
            return false;
        }

        // 检查点数
        if (GameManager.Instance.currentPoints < item.price)
        {
            Debug.Log("点数不足，无法购买数字卡");
            return false;
        }

        // 扣除点数
        GameManager.Instance.AddPoints(-item.price);

        // 将卡牌添加到玩家库存
        PlayerCardInventory.Instance.AddNumberCard(item.cardData.cardData);
        Debug.Log($"成功购买数字卡：{item.cardData.cardData.cardName}，价格：{item.price}");

        // 标记为已售
        item.sold = true;

        // 刷新 UI
        UIManager.Instance.RefreshShopUI();

        return true;
    }
    
    /// <summary>
    /// 公式卡删除回调 - 检查点数、冷却，执行扣除
    /// </summary>
    public bool OnFormulaCardDeleted(FormulaCardData formulaCardToDelete)
    {
        // 检查冷却
        if (CooldownManager.Instance != null &&
            CooldownManager.Instance.IsInCooldown(CooldownManager.CooldownType.CardDeletion))
        {
            Debug.LogWarning("[ShopManager] 卡牌删除操作在冷却中");
            return false;
        }
    
        // 计算删除费用
        BigInteger deleteCost = CalculateDeletionCost();
        if (GameManager.Instance.currentPoints < deleteCost)
        {
            Debug.Log("点数不足，无法删除公式卡");
            return false;
        }
    
        // 扣除点数
        GameManager.Instance.AddPoints(-deleteCost);
    
        // 增加删除计数
        totalRemovedFormulaCards++;
    
        // 开始冷却
        if (CooldownManager.Instance != null)
        {
            CooldownManager.Instance.StartCooldown(
                CooldownManager.CooldownType.CardDeletion,
                cardDeletionCooldown
            );
        }
    
        // 刷新 UI
        InitializeDeletionUI();
        return true;
    }

    /// <summary>
    /// 尝试购买公式卡
    /// </summary>
    public bool TryBuyFormulaCard(ShopItem<FormulaCardData> item)
    {
        if (item == null || item.sold)
        {
            Debug.LogWarning("[ShopManager] 公式卡无效或已售出");
            return false;
        }

        // 检查点数
        if (GameManager.Instance.currentPoints < item.price)
        {
            Debug.Log("点数不足，无法购买公式卡");
            return false;
        }

        // 扣除点数
        GameManager.Instance.AddPoints(-item.price);

        // 将卡牌添加到玩家库存
        PlayerCardInventory.Instance.AddFormulaCard(item.cardData);
        Debug.Log($"成功购买公式卡：{item.cardData.Name}，价格：{item.price}");

        // 标记为已售
        item.sold = true;

        // 刷新 UI
        UIManager.Instance.RefreshShopUI();

        return true;
    }

    /// <summary>
    /// 尝试购买祝福
    /// </summary>
    public bool TryBuyBlessing(ShopItem<BlessingData> item)
    {
        if (item == null || item.sold)
        {
            Debug.LogWarning("[ShopManager] 祝福无效或已售出");
            return false;
        }

        // 检查点数
        if (GameManager.Instance.currentPoints < item.price)
        {
            Debug.Log("点数不足，无法购买祝福");
            return false;
        }

        // 扣除点数
        GameManager.Instance.AddPoints(-item.price);

        // 应用祝福效果
        if (BlessingManager.Instance != null)
        {
            BlessingManager.Instance.TryBuyBlessing(item.cardData);
        }

        // 记录本回合已购买
        MarkBlessingAsPurchased(item.cardData.blessingId);

        Debug.Log($"成功购买祝福：{item.cardData.blessingName}，价格：{item.price}");

        // 标记为已售
        item.sold = true;

        // 刷新 UI
        UIManager.Instance.RefreshShopUI();

        return true;
    }
    #endregion

    #region 槽位解锁
    /// <summary>
    /// 尝试解锁数字卡槽位（含冷却检查）
    /// </summary>
    public bool TryUnlockNumberSlot()
    {
        if (CooldownManager.Instance != null &&
            CooldownManager.Instance.IsInCooldown(CooldownManager.CooldownType.SlotUnlock))
        {
            Debug.LogWarning("[ShopManager] 解锁操作在冷却中");
            return false;
        }

        if (numberCardCount >= MaxnumberCardCount)
            return false;

        BigInteger cost = CalculateNumberSlotUnlockCost();
        if (GameManager.Instance.currentPoints < cost)
            return false;

        if (CooldownManager.Instance != null)
            CooldownManager.Instance.StartCooldown(CooldownManager.CooldownType.SlotUnlock, unlockCooldown);

        UnlockNumberSlot();
        return true;
    }

    /// <summary>
    /// 尝试解锁公式卡槽位（含冷却检查）
    /// </summary>
    public bool TryUnlockFormulaSlot()
    {
        if (CooldownManager.Instance != null &&
            CooldownManager.Instance.IsInCooldown(CooldownManager.CooldownType.SlotUnlock))
        {
            Debug.LogWarning("[ShopManager] 解锁操作在冷却中");
            return false;
        }

        if (formulaCardCount >= MaxformulaCardCount)
            return false;

        BigInteger cost = CalculateFormulaSlotUnlockCost();
        if (GameManager.Instance.currentPoints < cost)
            return false;

        if (CooldownManager.Instance != null)
            CooldownManager.Instance.StartCooldown(CooldownManager.CooldownType.SlotUnlock, unlockCooldown);

        UnlockFormulaSlot();
        return true;
    }

    /// <summary>
    /// 尝试解锁祝福槽位（含冷却检查）
    /// </summary>
    public bool TryUnlockBlessingSlot()
    {
        if (CooldownManager.Instance != null &&
            CooldownManager.Instance.IsInCooldown(CooldownManager.CooldownType.SlotUnlock))
        {
            Debug.LogWarning("[ShopManager] 解锁操作在冷却中");
            return false;
        }

        if (blessingCardCount >= MaxBlessingCardCount)
            return false;

        BigInteger cost = CalculateBlessingSlotUnlockCost();
        if (GameManager.Instance.currentPoints < cost)
            return false;

        if (CooldownManager.Instance != null)
            CooldownManager.Instance.StartCooldown(CooldownManager.CooldownType.SlotUnlock, unlockCooldown);

        UnlockBlessingSlot();
        return true;
    }

    /// <summary>
    /// 解锁数字卡槽位
    /// </summary>
    public void UnlockNumberSlot()
    {
        if (numberCardCount >= MaxnumberCardCount)
        {
            Debug.Log("数字卡槽位已达上限");
            return;
        }

        BigInteger unlockCost = CalculateNumberSlotUnlockCost();

        if (GameManager.Instance.currentPoints < unlockCost)
        {
            Debug.Log("点数不足，无法解锁数字卡槽位");
            return;
        }

        GameManager.Instance.AddPoints(-unlockCost);
        numberCardCount++;
        numberSlotUnlockTimes++;

        InitializeRefreshCost();
        UIManager.Instance.RefreshShopUI();
    }

    /// <summary>
    /// 刷新解锁费用显示（不重新生成商品，仅更新解锁按钮上的价格文本）
    /// </summary>
    public void RefreshUnlockCostDisplay()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.RefreshShopUI();
        }
    }

    /// <summary>
    /// 解锁公式卡槽位
    /// </summary>
    public void UnlockFormulaSlot()
    {
        if (formulaCardCount >= MaxformulaCardCount)
        {
            Debug.Log("公式卡槽位已达上限");
            return;
        }

        BigInteger unlockCost = CalculateFormulaSlotUnlockCost();

        if (GameManager.Instance.currentPoints < unlockCost)
        {
            Debug.Log("点数不足，无法解锁公式卡槽位");
            return;
        }

        GameManager.Instance.AddPoints(-unlockCost);
        formulaCardCount++;
        formulaSlotUnlockTimes++;

        InitializeRefreshCost();
        UIManager.Instance.RefreshShopUI();
    }

    /// <summary>
    /// 解锁祝福槽位
    /// </summary>
    public void UnlockBlessingSlot()
    {
        if (blessingCardCount >= MaxBlessingCardCount)
        {
            Debug.Log("祝福槽位已达上限");
            return;
        }

        BigInteger unlockCost = CalculateBlessingSlotUnlockCost();

        if (GameManager.Instance.currentPoints < unlockCost)
        {
            Debug.Log("点数不足，无法解锁祝福槽位");
            return;
        }

        GameManager.Instance.AddPoints(-unlockCost);
        blessingCardCount++;
        blessingSlotUnlockTimes++;

        InitializeRefreshCost();
        UIManager.Instance.RefreshShopUI();
    }

    /// <summary>
    /// 计算数字卡槽位解锁费用
    /// </summary>
    public BigInteger CalculateNumberSlotUnlockCost()
    {
        if (DifficultySettingsManager.higherCost)
        {
            if (numberSlotUnlockTimes == 0)
            {
                return 50;
            }
            else if (numberSlotUnlockTimes == 1)
            {
                return 2000;
            }
            else if (numberSlotUnlockTimes == 2)
            {
                return 50000;
            }
        }
        if (numberSlotUnlockTimes == 0)
        {
            return 20;
        }
        else if (numberSlotUnlockTimes == 1)
        {
            return 5000;
        }

        return 10000;
        
    }

    /// <summary>
    /// 计算公式卡槽位解锁费用
    /// </summary>
    public BigInteger CalculateFormulaSlotUnlockCost()
    {
        if (DifficultySettingsManager.higherCost) 
        {
            return 30000;
        }

        return 5000;
    }

    /// <summary>
    /// 计算祝福槽位解锁费用
    /// </summary>
    public BigInteger CalculateBlessingSlotUnlockCost()
    {
        if (DifficultySettingsManager.higherCost)
        {
            if(blessingSlotUnlockTimes == 0)
            {
                return 10000;
            }
            else if (blessingSlotUnlockTimes == 1)
            {
                return 500000;
            }
        }
        if(blessingSlotUnlockTimes == 0)
        {
            return 2000;
        }
        return 50000;
    }
    #endregion

    #region 商店开关和难度设置
    /// <summary>
    /// 关闭商店
    /// </summary>
    public void CloseShop()
    {
        // 如果处于删卡模式，先结束选择模式以通知面板关闭点击行为
        if (isDeletionMode && CardSelectionManager.Instance != null)
        {
            CardSelectionManager.Instance.EndCardSelection();
            isDeletionMode = false;
        }

        if (UIManager.Instance != null)
            UIManager.Instance.CloseShop();
    }

    /// <summary>
    /// 应用难度设置 - 重新刷新商店
    /// </summary>
    public void ApplyDifficultySettings()
    {
        // 难度设置只能在主菜单修改，无需重置商店状态
        // 倍率信息已通过 DataSavingManager 持久化，商店下次打开时自动应用
        // higherCost 即时生效（槽位解锁费用方法实时读取）
    }
    #endregion
}
