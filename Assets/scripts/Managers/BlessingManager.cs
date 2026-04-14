using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using UnityEngine;

/// <summary>
/// 祝福管理器 - 管理玩家拥有的祝福及其效果
/// </summary>
public class BlessingManager : MonoBehaviour
{
    public static BlessingManager Instance;

    [Header("祝福库引用")]
    [Tooltip("祝福卡库 - 拖入 BlessingLibrary 资源")]
    public BlessingLibrary blessingLibrary;

    [Header("玩家已拥有的祝福")]
    // 用字典存储：祝福ID -> 购买次数
    public Dictionary<int, int> ownedBlessings = new Dictionary<int, int>();

    // 跟踪已永久购买过的祝福（用于NeverRefresh类型）
    public HashSet<int> blessingsEverPurchased = new HashSet<int>();

    // 用于快速查询特定祝福的购买次数
    public Dictionary<BlessingData.BlessingType, int> blessingTypeCount =
        new Dictionary<BlessingData.BlessingType, int>();

    // 创建现有的祝福实例列表（包含数据和购买次数）
    public List<BlessingInstance> ownedBlessingInstance = new List<BlessingInstance>();


    [Header("祝福效果累积")]
    public float totalMultiplierBonus = 0f; // 倍率加成
    public int totalDialecticalCount = 0;   // '辩证主义'购买次数
    private float AllGodsCount = 0;          // 众神归位数量 
    private int LuckTurnsCount = 0;           //是否激活转运，1是激活
    private bool hasJackpot7 = false;        //是否激活逢7过
    private int CardMasterCount = 0;       //是否激活卡牌大师 
    public int HasRichTreasure = 0;        //是否激活丰盈宝库
    private BlessingData wishCoinTargetBlessing = null; //许愿币储存的祝福
    public int nihilismCount = 0;       // 虚无主义数量
    public bool hasLeadingCharge = false; // 打头阵
    private bool hasGambleToWin = false; // 是否拥有赌为赢祝福
    public int hasEnergySpread = 0;      // 是否拥有能量扩散
    public int hasRisingUp = 0;          // 是否拥有节节高
    public int hasTemperlance = 0;       // 是否拥有平衡节制
    private readonly BigInteger GambleToWinReward = 2400000000; // 赌为赢奖励的点数    
    public bool hasIdealism = false;  //唯心主义
    public Dictionary<int, int> idealismDiceResults = new Dictionary<int, int>();  //唯心主义储存不同等级骰子出目的字典   
    public int dialecticalPerRoundBonus = 0;  // 每购买1级辩证主义，每回合+1倍率
    public float dialecticalAccumulatedMultiplier = 0f; // 辩证主义累积的回合倍率
    public int ApplyPragmatism = 0;//实用主义
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }

        InitializeBlessingSystem();
    }

    /// <summary>
    /// 初始化祝福系统
    /// </summary>
    private void InitializeBlessingSystem()
    {
        ownedBlessings.Clear();
        blessingTypeCount.Clear();
        totalMultiplierBonus = 0f;
        totalDialecticalCount = 0;
        AllGodsCount = 0;
        LuckTurnsCount = 0;
        CardMasterCount = 0;
        hasJackpot7 = false;
        wishCoinTargetBlessing = null; 
        HasRichTreasure = 0;
        nihilismCount = 0;
        hasLeadingCharge = false;
        hasGambleToWin = false; 

        hasIdealism = false;
        
        hasEnergySpread = 0;
        hasRisingUp = 0;
        hasTemperlance = 0;

        dialecticalPerRoundBonus = 0; 
        dialecticalAccumulatedMultiplier = 0f;  
        ApplyPragmatism = 0;      
    }

    /// <summary>
    /// 购买祝福
    /// </summary>
    public bool TryBuyBlessing(BlessingData blessingData)
    {
        if (blessingData == null)
        {
            Debug.LogError("祝福数据为空！");
            return false;
        }

        // 计算当前祝福的价格（考虑已购买次数）
        int finalPrice = CalculateBlessingPrice(blessingData);

        // 检查点数是否足够
        if (GameManager.Instance.currentPoints < finalPrice)
        {
            Debug.LogWarning($"点数不足！需要{finalPrice}，当前{GameManager.Instance.currentPoints}");
            return false;
        }

        // 扣除点数
        GameManager.Instance.AddPoints(-finalPrice);

        // 记录祝福购买
        if (ownedBlessings.ContainsKey(blessingData.blessingId))
        {
            ownedBlessings[blessingData.blessingId]++;
        }
        else
        {
            ownedBlessings[blessingData.blessingId] = 1;
        }

        // 记录为已购买过（用于NeverRefresh判定）
        blessingsEverPurchased.Add(blessingData.blessingId);

        // 更新类型计数
        if (!blessingTypeCount.ContainsKey(blessingData.blessingType))
        {
            blessingTypeCount[blessingData.blessingType] = 0;
        }
        blessingTypeCount[blessingData.blessingType]++;

        ownedBlessingInstance.Add(new BlessingInstance(blessingData, ownedBlessings[blessingData.blessingId]));

        // 应用祝福效果
        ApplyBlessingEffect(blessingData);

        return true;
    }
    /// <summary>
    /// 检查祝福是否已被购买过（用于NeverRefresh判定）
    /// </summary>
    public bool HasBlessingEverBeenPurchased(int blessingId)
    {
        return blessingsEverPurchased.Contains(blessingId);
    }

    /// <summary>
    /// 获取已有祝福数量
    /// </summary>
    private int GetTotalBlessingCount()
    {
        int totalCount = 0;
        foreach (var kvp in ownedBlessings)
        {
            if (kvp.Value > 0)
            {
                totalCount += kvp.Value;
            }
        }
        return totalCount;
    }

    public int CalculateBlessingPrice(BlessingData data)

    {
        int purchaseCount = GetBlessingCount(data.blessingId);
        float calculatedPrice;
        int currentPrice = data.basePrice;
        float priceMultiplier = GetCurrentPriceMultiplier();
        switch (data.blessingType)
        {
            case BlessingData.BlessingType.Raise:
                currentPrice += purchaseCount * 500;
                calculatedPrice = currentPrice * priceMultiplier;
                return Mathf.RoundToInt(calculatedPrice);

            default:
                calculatedPrice = currentPrice * priceMultiplier;
                return Mathf.RoundToInt(calculatedPrice);
        }
    }


    /// <summary>
    /// 应用祝福效果
    /// </summary>
    private void ApplyBlessingEffect(BlessingData blessingData)
    {
        switch (blessingData.blessingType)
        {
            case BlessingData.BlessingType.CardCheat:
                ActivateCardCheatSelection();
                Debug.Log("老千 已激活！");
                break;
            
            case BlessingData.BlessingType.Idealism:
                hasIdealism = true;
                Debug.Log("唯心主义 已激活！");
                break;
            
            case BlessingData.BlessingType.GambletoWin:
                hasGambleToWin = true;
                Debug.Log("赌为赢 已激活！");
                break;

            case BlessingData.BlessingType.Nihilism:
                // 虚无主义：数量+1，价格翻倍
                nihilismCount++;
                Debug.Log($"虚无主义已激活！当前数量：{nihilismCount}");
                break;
            
            case BlessingData.BlessingType.WishingCoin:
                // 许愿币：选择一个已拥有的可叠加祝福，下回合商店必出
                Debug.Log("许愿币效果激活：请选择一个已拥有的可叠加祝福");
                ActivateWishCoinSelection();
                break;

            case BlessingData.BlessingType.MagicLamp:
                //神灯 - 获取三个随机可叠加祝福
                AddStackableBlessingsToOwned(3);
                Debug.Log("神灯效果已激活");
                break;

            case BlessingData.BlessingType.Jackpot7:
                // 逢七过 - 
                hasJackpot7 = true;
                Debug.Log("逢七过效果已激活");
                break;

            case BlessingData.BlessingType.AllGodsInPlace:
                // 众神归位 - 效果每回合可能要重新检测祝福数量
                AllGodsCount++;
                
                Debug.Log("众神归位效果已激活");
                break;

            case BlessingData.BlessingType.FinancialMaster:
                // 理财大师 - 效果在回合结束时应用（在 GameManager 中调用）
                Debug.Log("理财大师效果已激活");
                break;

            case BlessingData.BlessingType.MoreMoreBetter:
                // 多多益善 - 复制一张填空卡（需要玩家选择）
                //[todo]
                Debug.Log("多多益善效果已激活，等待玩家选择填空卡");
                break;

            case BlessingData.BlessingType.CardMaster:
                // 卡牌大师 - 每张数字卡额外提供1倍率
                CardMasterCount++;
               
                Debug.Log("卡牌大师效果已激活，提供额外倍率");
                break;


            case BlessingData.BlessingType.DialecticalViewpoint:
                // 辩证主义 - 立即应用倍率和点数
                
                dialecticalPerRoundBonus++; // 改为记录每回合倍率基数
                GameManager.Instance.AddPoints(blessingData.bonusPoints);
                totalDialecticalCount++;
                Debug.Log($"辩证主义效果已激活：每回合倍率+1，额外获得{blessingData.bonusPoints}点，价格上升1%");
                break;

            case BlessingData.BlessingType.LuckTurns:
                // 转运 - 无法叠加：你的骰子投到1时，重投一次并将这次的结果作为该骰子的最终判定结果
                LuckTurnsCount = 1;
                Debug.Log("转运效果已激活（无法叠加）：骰子投出1时将重投一次！");
                break;

            case BlessingData.BlessingType.DoubleDown:
                // 倍投 - 倍率+1,价格翻倍
                totalMultiplierBonus += blessingData.effectValue;
                Debug.Log("倍投效果已激活");
                break;

            case BlessingData.BlessingType.Raise:
                // 加注 - 倍率+1,价格+500
                totalMultiplierBonus += blessingData.effectValue;
                Debug.Log("加注效果已激活");
                break;

            case BlessingData.BlessingType.SmallCardPack:
                // 小卡牌包 - 立即获得三张随机数字卡
                PlayerCardInventory.Instance.AddRandomNumberCards(3);
                Debug.Log("小卡牌包效果已激活：立即获得三张随机数字卡");
                break;

            case BlessingData.BlessingType.BigCardPack:
                // 大卡牌包 - 立即获得五张随机数字卡
                PlayerCardInventory.Instance.AddRandomNumberCards(5);
                Debug.Log("大卡牌包效果已激活：立即获得五张随机数字卡");
                break;

            case BlessingData.BlessingType.FriendDiscount:
                // 友情折扣 - 不可叠加：所有数字卡、填空卡与祝福的价格-10%
                totalMultiplierBonus += blessingData.effectValue;
                Debug.Log("友情折扣效果已激活");
                break;

            case BlessingData.BlessingType.Bless:
                // 眷顾 - 不可叠加：你每拥有一个祝福，所有数字卡、填空卡与祝福的价格-1%
                Debug.Log("眷顾效果已激活");
                break;

            case BlessingData.BlessingType.RichTreasury:
                // 丰盈宝库 - 不可叠加：商店刷新永久免费
                HasRichTreasure = 1;
                Debug.Log("丰盈宝库效果已激活");
                break;

            case BlessingData.BlessingType.Empiricism:
                // 经验主义 - 不可叠加：每回合抽取数字卡时先抽取上一回合判定结果最大的数字卡
                Debug.Log("经验主义效果已激活");
                break;

            case BlessingData.BlessingType.Materialism:
                // 唯物主义 - 不可叠加：立即获得等同于当前已拥有祝福数量2倍的永久倍率，然后失去所有祝福
                totalMultiplierBonus += ownedBlessingInstance.Count * 2;
                ClearAllBlessings();
                Debug.Log("唯物主义效果已激活");
                break;

            case BlessingData.BlessingType.QuitGambling:
                // 戒赌 - 可叠加：将数字卡中的一个骰子变为{0}
                Debug.Log("戒赌效果已激活");
                break;

            case BlessingData.BlessingType.GamblingGearUpgraded:
                // 赌具升级 - 可叠加：选择一个骰子使其立即升一级，祝福“赌具升级”的价格翻倍
                BlessingManager.Instance.UpgradeDiceEquipment();
                Debug.Log("赌具升级效果已激活");
                break;

            case BlessingData.BlessingType.CompulsiveGambler:
                // 狂赌之渊 - 不可叠加，本回合商店不刷新：立即将所有绿色数字变为~20~
                BlessingManager.Instance.MadGambler();
                Debug.Log("狂赌之渊效果已激活");
                break;

            case BlessingData.BlessingType.EnergySpread:
                // 能量扩散 - 不可叠加：不参与计算的绿色数字每回合也会+1
                hasEnergySpread = 1;
                Debug.Log("能量扩散效果已激活");
                break;

            case BlessingData.BlessingType.Utopianism:
                // 空想主义 - 可叠加：立即获得一张未拥有的填空卡；如果获得此祝福时拥有了全部种类的填空卡，你立即获得2400000000点并失去所有“空想主义”
                Debug.Log("空想主义效果已激活");
                break;

            case BlessingData.BlessingType.Pragmatism:
                // 实用主义 - 不可叠加：任意时刻你仅保留价值最高的填空卡并自动删除其它填空卡
                ApplyPragmatism = 1;
                ApplyPragmatismEffect();
                Debug.Log("实用主义效果已激活");
                break;

            case BlessingData.BlessingType.ShortSight:
                //短视 - 可叠加：倍率+10；每回合倍率-1
                totalMultiplierBonus += 10;
                Debug.Log("短视效果已激活");
                break;

            case BlessingData.BlessingType.RisingUp:
                //节节高 - 不可叠加：大于9的绿色数字递增后将变为绿色的{1}；触发此效果时，你的倍率永久+20
                hasRisingUp = 1;
                
                Debug.Log("节节高效果已激活");
                break;

            case BlessingData.BlessingType.LeadingCharge:
                // 打头阵 - 不可叠加            
                hasLeadingCharge = true; 
                Debug.Log("打头阵 已激活！");
                break;

            case BlessingData.BlessingType.Temperlance:
                //平衡节制 - 不可叠加：每次计算判定结果最大和最小的数字卡的判定结果变为所有参与计算的数字卡本轮判定结果的均值
                hasTemperlance = 1;
                Debug.Log("平衡节制效果已激活");
                break;
        }
    }

    /// <summary>
    /// 提供转运祝福的激活状态（是否拥有转运祝福）
    /// </summary>
    public bool IsLuckTurnsActive()
    {
        return LuckTurnsCount > 0;
    }
 
    // 触发老千选择流程
    private void ActivateCardCheatSelection()
    {
        Debug.Log("=== 老千：开始选择数字卡 ===");

        CardSelectionManager.Instance.StartCardSelection(
            CardSelectionManager.SelectionMode.CardCheat,
            OnCardCheatSelected);

        UIManager.Instance.OpenCardCheatNumberSelection();
    }

    // 选择完成回调
    private void OnCardCheatSelected(object selectedObject)
    {
        CardSelectionManager.Instance.EndCardSelection();
        UIManager.Instance.CloseCardCheatNumberSelection();

        if (selectedObject is NumberCardInstance card)
        {
            Debug.Log("=== 老千：替换数字卡 ===");

        // 删掉选中的卡
            PlayerCardInventory.Instance.RemoveNumberCard(card);
        // 补发一张新随机数字卡
            PlayerCardInventory.Instance.AddRandomNumberCards(1);
        }
    }
    
    /// <summary>
    /// 许愿币：打开祝福选择界面（只显示玩家已拥有的可叠加祝福）
    /// </summary>
    private void ActivateWishCoinSelection()
    {
        if (CardSelectionManager.Instance == null)
        {
            Debug.LogError("CardSelectionManager 未初始化");
            return;
        }

    // 开启祝福选择模式
        CardSelectionManager.Instance.StartCardSelection(
        CardSelectionManager.SelectionMode.WishCoinSelect,
        OnWishCoinBlessingSelected);
    
        UIManager.Instance.OpenWishCoinBlessSelection();
    }

    /// <summary>
    /// 许愿币：玩家选择祝福后的回调
    /// </summary>
    private void OnWishCoinBlessingSelected(object selectedObject)
    {
        if (!(selectedObject is BlessingData selectedBlessing) || selectedBlessing == null)
        {
            Debug.LogError("许愿币选择无效！");
            return;
        }

        // 保存目标祝福
        wishCoinTargetBlessing = selectedBlessing;
        Debug.Log($"许愿币已锁定：下次商店必出【{selectedBlessing.blessingName}】");
        
        UIManager.Instance.CloseWishCoinBlessSelection();
    }

    /// <summary>
    /// 商店获取许愿币锁定的祝福（ShopManager 调用）
    /// </summary>
    public BlessingData GetWishCoinTargetBlessing()
    {
        return wishCoinTargetBlessing;
    }

    /// <summary>
    /// 许愿币效果已使用（商店刷新后调用）
    /// </summary>
    public void ConsumeWishCoin()
    {
        wishCoinTargetBlessing = null;
    }
    
    
    /// <summary>
    /// 根据id获取特定类型祝福的购买次数
    /// </summary>
    public int GetBlessingCount(int blessingId)
    {
        return ownedBlessings.ContainsKey(blessingId) ? ownedBlessings[blessingId] : 0;
    }

    /// <summary>
    /// 根据blessingtype获取特定类型祝福的购买次数
    /// </summary>
    public int GetBlessingTypeCount(BlessingData.BlessingType type)
    {
        return blessingTypeCount.ContainsKey(type) ? blessingTypeCount[type] : 0;
    }

    /// <summary>
    /// 计算“逢七过”的倍率
    /// </summary>
    private float CalculateJackpot7Bonus()
    {
        if (!hasJackpot7) return 0f;
        return 7f;
    }

    /// <summary>
    /// 计算“众神归位”的倍率
    /// </summary>
    private float CalculateAllGodsInPlaceBonus()
    {
        if (AllGodsCount <= 0) return 0f;
        int totalBlessingCount = GetTotalBlessingCount();
        return totalBlessingCount * AllGodsCount;
    }

    /// <summary>
    /// 计算"理财大师"效果的额外点数
    /// </summary>
    public BigInteger CalculateFinancialMasterBonus(BigInteger currentPoints)
    {
        int financialMasterCount = GetBlessingTypeCount(BlessingData.BlessingType.FinancialMaster);
        if (financialMasterCount == 0)
            return BigInteger.Zero;

        // 每次购买都叠加效果：额外获得已拥有点数的1%
        BigInteger bonusPerCount = currentPoints / 100; // 1% = 1/100
        BigInteger totalBonus = bonusPerCount * financialMasterCount;

        Debug.Log($"理财大师加成：{currentPoints} × 1% × {financialMasterCount} = {totalBonus}");
        return totalBonus;
    }

    /// <summary>
    /// 获取卡牌大师的额外倍率
    /// </summary>
    private float CalculateCardMasterBonus()
    {
        if (CardMasterCount <= 0) return 0f;
        int numberCardCount = PlayerCardInventory.Instance.GetAllNumberCards().Count;
        return CardMasterCount * numberCardCount;
    }

    /// <summary>
    /// 获取当前总倍率加成
    /// </summary>
    public float GetTotalMultiplierBonus()
    {
        return totalMultiplierBonus;
    }

    /// <summary>
    /// 神灯
    /// </summary>
    private void AddStackableBlessingsToOwned(int count)
    {
        // 1. 获取祝福库中所有可叠加祝福
        List<BlessingData> allStackable = blessingLibrary.GetAllStackableBlessing();
        allStackable.RemoveAll(b=>b.blessingType == BlessingData.BlessingType.MagicLamp);
        System.Random rnd = new System.Random();
        // 2. 随机选择 count 个祝福
        for (int i = 0; i < count; i++)
        {
            int randomIdx = rnd.Next(allStackable.Count);
            BlessingData selected = allStackable[randomIdx];
            if (selected == null) continue;

        // 3. 直接添加到 ownedBlessings
            if (ownedBlessings.ContainsKey(selected.blessingId)) ownedBlessings[selected.blessingId]++;
            else ownedBlessings[selected.blessingId] = 1;

        // 4. 同步关联数据
            blessingsEverPurchased.Add(selected.blessingId);
            if (!blessingTypeCount.ContainsKey(selected.blessingType)) blessingTypeCount[selected.blessingType] = 0;
            blessingTypeCount[selected.blessingType]++;

        // 5. 触发该祝福的效果
            ApplyBlessingEffect(selected);
            Debug.Log($"神灯获得：{selected.blessingName}（当前次数：{ownedBlessings[selected.blessingId]}）");
        }
    }   

    
    /// <summary>
    /// 获取当前价格乘数（用于计算商品价格）
    /// 辩证主义：所有卡牌与祝福的价格+1%
    /// </summary>
    public float GetCurrentPriceMultiplier()
    {
         // 辩证主义：每级 +1% 价格（加法叠加）
        int dialecticCount = GetBlessingTypeCount(BlessingData.BlessingType.DialecticalViewpoint);
        float multiplier = 1f + (dialecticCount * 0.01f); 
        
        
        return multiplier;
    }

    /// <summary>
    /// 执行"逢七过"效果 - 判定结果是否符合触发条件
    /// </summary>
    public bool CheckJackpot7Effect(BigInteger score)
    {
        if (!hasJackpot7) return false;
        bool isMultipleOf7 = score % 7 == 0;
        bool ContainsDigit7 = score.ToString().Contains("7");
        bool triggerJackpot7 = isMultipleOf7 || ContainsDigit7;

        if (triggerJackpot7)
        {
            Debug.Log("逢七过触发，本回合得分归0");
        }
        return triggerJackpot7;
    }

    /// <summary>
    /// 执行"多多益善"效果 - 复制指定的填空卡
    /// </summary>
    public void ApplyMoreMoreBetterEffect(FormulaCardData formulaCardToCopy)
    {
        if (formulaCardToCopy == null)
        {
            Debug.LogError("要复制的填空卡为空");
            return;
        }

        int moreMoreCount = GetBlessingTypeCount(BlessingData.BlessingType.MoreMoreBetter);

        // 每次购买都复制一次指定的卡
        for (int i = 0; i < moreMoreCount; i++)
        {
            PlayerCardInventory.Instance.AddFormulaCard(formulaCardToCopy);
            Debug.Log($"已复制填空卡：{formulaCardToCopy.Name}（第{i + 1}次）");
        }

        // 同步牌堆
        CardManager.Instance.SyncDeckFromInventory();
    }

    public int GetBlessingCount(BlessingData.BlessingType type)
    {
        return ownedBlessingInstance.Count(b => b.data.blessingType == type);
    }

    /// <summary>
    /// 清空所有祝福
    /// </summary>
    public void ClearAllBlessings()
    {
        ownedBlessings.Clear();
        blessingTypeCount.Clear();
        totalMultiplierBonus = 0f;
        totalDialecticalCount = 0;
        AllGodsCount = 0;
        LuckTurnsCount = 0;
        CardMasterCount = 0;
        hasJackpot7 = false;
        HasRichTreasure = 0;
        wishCoinTargetBlessing = null;
        nihilismCount = 0;
        hasLeadingCharge = false;
        ownedBlessingInstance.Clear();
        hasGambleToWin = false;
        hasIdealism = false;
        hasEnergySpread = 0;
        hasRisingUp = 0;
        hasTemperlance = 0;
        dialecticalPerRoundBonus = 0;
        dialecticalAccumulatedMultiplier = 0f;
    }

    /// <summary>
    /// 获取玩家已拥有的所有祝福列表
    /// </summary>
    public List<BlessingInstance> GetOwnedBlessings()
    {
        List<BlessingInstance> result = new List<BlessingInstance>();
        foreach (var kvp in ownedBlessings)
        {
            BlessingData blessingData = blessingLibrary.GetBlessingById(kvp.Key);
            if (blessingData != null)
            {
                result.Add(new BlessingInstance(blessingData, kvp.Value));
            }
        }
        return result;
    }

    /// <summary>
    /// 调试：打印当前祝福状态
    /// </summary>
    [ContextMenu("Print Blessing Status")]
    public void PrintBlessingStatus()
    {
        Debug.Log("=== 祝福状态 ===");
        foreach (var kvp in ownedBlessings)
        {
            BlessingData blessing = blessingLibrary.GetBlessingById(kvp.Key);
            if (blessing != null)
            {
                Debug.Log($"{blessing.blessingName}：{kvp.Value}次");
            }
        }
        Debug.Log($"总倍率加成：{totalMultiplierBonus}");
        Debug.Log($"价格乘数：{GetCurrentPriceMultiplier()}");
        Debug.Log($"转运祝福激活状态：{(IsLuckTurnsActive() ? "已激活" : "未激活")}");
    }


    /// <summary>
    /// 获取最终总祝福倍率
    /// </summary>
    public float GetFinalBlessingMultiplier()
    {
        float baseMultiplier = totalMultiplierBonus;
        float jackpotBonus = CalculateJackpot7Bonus();
        float godsBonus = CalculateAllGodsInPlaceBonus();
        float cardMasterBonus = CalculateCardMasterBonus();
        float dialecticalBonus = dialecticalAccumulatedMultiplier;
        
        float final = baseMultiplier + jackpotBonus + godsBonus + cardMasterBonus + dialecticalBonus;
        
        return final;
    }

    /// <summary>
    /// 获取当前所有祝福提供的点数加成（理财大师等）
    /// </summary>
    public BigInteger GetBlessingPointBonus(BigInteger currentPoints)
    {
        return CalculateFinancialMasterBonus(currentPoints);
    }

    /// <summary>
    /// 获取祝福导致的价格提高倍数（辩证主义等）
    /// </summary>
    public float GetBlessingPriceIncreaseMultiplier()
    {
        return GetCurrentPriceMultiplier();
    }

    /// <summary>
    /// 获取价格提高百分比（方便显示用，如 15%）
    /// </summary>
    public float GetBlessingPriceIncreasePercent()
    {
        float multiplier = GetCurrentPriceMultiplier();
        return (multiplier - 1f) * 100f;
    }
    
    /// <summary>
    /// 获取玩家已拥有的可叠加祝福（许愿币）
    /// </summary>
    public List<BlessingData> GetOwnedStackableBlessings()
    {
        List<BlessingData> result = new List<BlessingData>();

        foreach (var kvp in ownedBlessings)
        {
            BlessingData data = blessingLibrary.GetBlessingById(kvp.Key);
            if (data == null) continue;

            // 只保留已拥有且可叠加的祝福
            if (data.isStackable)
            {
                result.Add(data);
            }
        }
        return result;
    }
    
    ///<summary>
    ///赌为赢效果判定
    ///<summary>
    public void CheckGambleToWin(int diceResult)
    {
        // 未解锁赌为赢不生效
        if (!hasGambleToWin) return;

        // 骰子不是20不生效
        if (diceResult != 20) return;

        // 玩家总骰子数 < 20不生效
        int totalDice = PlayerCardInventory.Instance.CountOwnedDiceTotalNumber();
        if (totalDice < 20) return;

        GameManager.Instance.AddPoints(GambleToWinReward);
        Debug.Log($"【赌为赢】触发！骰子=20，总骰子数{totalDice}，获得24亿点！");
    }
    ///<summary>
    ///唯心主义清空每回合储存的骰子结果
    ///<summary>
    public void NewRound_IdealismReset()
    {
        idealismDiceResults.Clear();
    }

    /// <summary>
    /// 每回合开始时叠加辩证主义倍率
    /// </summary>
    public void AddDialecticalPerRoundMultiplier()
    {
        if (dialecticalPerRoundBonus > 0)
        {
            dialecticalAccumulatedMultiplier += dialecticalPerRoundBonus;
            Debug.Log($"辩证主义每回合加成：累积倍率+{dialecticalPerRoundBonus}，当前总累积{dialecticalAccumulatedMultiplier}");
        }
    }

    /// <summary>
    /// 实用主义：仅保留价值最高的填空卡，删除其它填空卡
    /// </summary>
    public void ApplyPragmatismEffect()
    {
        if (PlayerCardInventory.Instance == null)
        {
            Debug.LogError("实用主义：PlayerCardInventory 未找到！");
            return;
        }

        // 获取当前所有公式卡
        List<FormulaCardData> formulaCards = PlayerCardInventory.Instance.formulaCards;
        if (formulaCards == null || formulaCards.Count <= 1)
            return;

        // 按名称排序（保证只留一张）
        formulaCards.Sort((a, b) => b.CardPrice.CompareTo(a.CardPrice));

        // 保留最强卡，清空其他卡
        FormulaCardData bestCard = formulaCards[0];
        formulaCards.Clear();
        formulaCards.Add(bestCard);

        // 同步到卡牌管理器
        CardManager.Instance.SyncDeckFromInventory();

        Debug.Log($"实用主义清理完成：仅保留1张最强公式卡，已删除冗余卡");
    }
    /// <summary>
    /// 祝福：狂赌之渊
    /// 不修改任何卡数据，只删除旧递增卡 → 生成新~20~骰子卡并加入背包
    /// </summary>
    public void MadGambler()
    {
        if (PlayerCardInventory.Instance == null) return;

        // 先拿到所有卡
        var allCards = PlayerCardInventory.Instance.GetAllNumberCards();
        var toRemove = new List<NumberCardInstance>();
        var toAddData = new List<NumberCardData>();

        foreach (var inst in allCards)
        {
            if (inst == null || inst.cardData == null) continue;

            NumberComponent a = inst.cardData.partA;
            NumberComponent b = inst.cardData.partB;

            bool needReplace = a.isIncremental || (b != null && b.isIncremental);
            if (!needReplace) continue;

            // 标记要删除
            toRemove.Add(inst);

            // 克隆新卡，把递增换成~20~骰子，其他完全不变
            NumberCardData newCard = ScriptableObject.CreateInstance<NumberCardData>();
            newCard.cardName = inst.cardData.cardName;
            newCard.logicalType = inst.cardData.logicalType;
            newCard.layoutType = inst.cardData.layoutType;

            // 处理 PartA
            newCard.partA = new NumberComponent();
            newCard.partA.isIncremental = false;
            newCard.partA.isDice = a.isIncremental ? true : a.isDice;
            newCard.partA.diceSides = a.isIncremental ? 20 : a.diceSides;
            newCard.partA.value = a.value;

            // 处理 PartB
            if (b != null)
            {
                newCard.partB = new NumberComponent();
                newCard.partB.isIncremental = false;
                newCard.partB.isDice = b.isIncremental ? true : b.isDice;
                newCard.partB.diceSides = b.isIncremental ? 20 : b.diceSides;
                newCard.partB.value = b.value;
            }

            toAddData.Add(newCard);
            Debug.Log($"[狂赌之渊] 替换：{inst.cardData.cardName} → ~20~ 骰子版");
        }

        // 先删旧的
        foreach (var card in toRemove)
        {
            PlayerCardInventory.Instance.RemoveNumberCard(card);
        }

        // 再加新的
        foreach (var data in toAddData)
        {
           PlayerCardInventory.Instance.AddNumberCard(data);
        }

        Debug.Log($"[狂赌之渊] 生效完成！共替换 {toAddData.Count} 张卡");
    }
    /// <summary>
    /// 祝福：赌具升级
    /// 所有骰子按 4→6→8→12→20 升一级，20不再升
    /// 不修改原卡，只删旧卡+添加新卡，商店完全不变
    /// </summary>
    public void UpgradeDiceEquipment()
    {
        if (PlayerCardInventory.Instance == null) return;

        var allCards = PlayerCardInventory.Instance.GetAllNumberCards();
        var toRemove = new List<NumberCardInstance>();
        var toAddData = new List<NumberCardData>();

        foreach (var inst in allCards)
        {
            if (inst == null || inst.cardData == null) continue;

            NumberComponent a = inst.cardData.partA;
            NumberComponent b = inst.cardData.partB;

            // 判断这张卡有没有骰子
            bool hasDiceA = a != null && a.isDice;
            bool hasDiceB = b != null && b.isDice;
            if (!hasDiceA && !hasDiceB) continue;

            toRemove.Add(inst);

            // 克隆一张新卡，不影响原卡与商店
            NumberCardData newCard = ScriptableObject.CreateInstance<NumberCardData>();
            newCard.cardName = inst.cardData.cardName;
            newCard.logicalType = inst.cardData.logicalType;
            newCard.layoutType = inst.cardData.layoutType;

            // 升级 PartA 骰子
            newCard.partA = new NumberComponent();
            newCard.partA.isIncremental = a.isIncremental;
            newCard.partA.isDice = a.isDice;
            newCard.partA.value = a.value;
            newCard.partA.diceSides = a.isDice ? UpgradeDiceLevel(a.diceSides) : a.diceSides;

            // 升级 PartB 骰子
            if (b != null)
            {
                newCard.partB = new NumberComponent();
                newCard.partB.isIncremental = b.isIncremental;
                newCard.partB.isDice = b.isDice;
                newCard.partB.value = b.value;
               newCard.partB.diceSides = b.isDice ? UpgradeDiceLevel(b.diceSides) : b.diceSides;
            }

            toAddData.Add(newCard);
            Debug.Log($"[赌具升级] 升级卡牌：{inst.cardData.cardName}");
        }

        // 先删旧卡
        foreach (var card in toRemove)
        {
            PlayerCardInventory.Instance.RemoveNumberCard(card);
        }

        // 再加新卡
        foreach (var data in toAddData)
        {
            PlayerCardInventory.Instance.AddNumberCard(data);
        }

        Debug.Log($"[赌具升级] 完成！共升级骰子卡：{toAddData.Count} 张");
    }

    /// <summary>
    /// 骰子等级升级规则：4→6→8→12→20，20不变
    /// </summary>
    private int UpgradeDiceLevel(int currentSides)
    {
        switch (currentSides)
        {
            case 4: return 6;
            case 6: return 8;
            case 8: return 12;
            case 12: return 20;
            case 20: return 20;
            default: return currentSides;
        }
    }
}       