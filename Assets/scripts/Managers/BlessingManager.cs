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
    // 关键变量--永久倍率
    public long totalMultiplierBonus = 0;
    // 其他需要记录的效果变量
    public Dictionary<int, int> idealismDiceResults = new Dictionary<int, int>();  //唯心主义储存不同等级骰子出目的字典 
    private readonly BigInteger GambleToWinReward = 2400000000; // 赌为赢奖励的点数    
    public int totalDialecticalCount = 0;               // 辩证主义购买次数
    private int AllGodsCount = 0;                       // 众神归位数量 
    private int LuckTurnsCount = 0;                     //是否激活转运
    private bool hasJackpot7 = false;                   //是否激活逢7过
    private int CardMasterCount = 0;                    //是否激活卡牌大师 
    public int HasRichTreasure = 0;                     //是否激活丰盈宝库
    private BlessingData wishCoinTargetBlessing = null; //许愿币储存的祝福
    public int wishCoinPurchaseCount = 0;               // 许愿币购买次数
    public int nihilismCount = 0;                       // 虚无主义数量
    private BlessingData darkBoxTargetBlessing = null;  // 暗箱操作选中的目标祝福
    public int darkBoxPurchaseCount = 0;                // 暗箱操作购买次数
    public bool hasLeadingCharge = false;               // 打头阵
    private bool hasGambleToWin = false;                // 是否拥有赌为赢祝福
    public int hasEnergySpread = 0;                     // 是否拥有能量扩散
    public int hasRisingUp = 0;                         // 是否拥有节节高
    public int hasTemperlance = 0;                      // 是否拥有平衡节制
    public bool hasIdealism = false;                    //唯心主义
    public float dialecticalPricePercent = 0f;          // 辩证主义
    public int ApplyPragmatism = 0;                     //实用主义
    public bool hasGodOfGambler = false;                //赌神传说
    public float globalPriceDiscountPercent = 0f;       // 友情折扣
    public float blessDiscountPerBlessing = 0f;         // 眷顾
    public float maxBlessDiscountPercent = 70f;         // 眷顾上限
    public int shortSightCount = 0;                     // 短视数量
    public int reverse = 0;                             // 翻转
    public bool hasYinYang = false;                     // 阴阳
    public bool hasFall = false;                        // 坠落
    public int hasUnstoppable = 0;                      // 势如破竹
    public int hasLoanWallet = 0;                       // 贷款钱包
    public BigInteger loan = BigInteger.Zero;           // 贷款金额
    public int minimalismMultiplier = 0;                //极简主义
    public int minimalismCount = 0;                     //极简主义数量
    public int pragmatismDeleteCount = 0;               //实用主义删除次数
    public int dayAfterDayCount = 0;                    // 日积月累数量
    public int hasHastyAppreciation = 0;                // 走马观花
    public int hastyAppreciationBonus = 0;              // 走马观花的临时倍率
    public int bigSuccessCount = 0;                     // 大成功数量
    public bool hasJustice = false;                     // 绝对正义
    public int luckyStarCount = 0;                      // 幸运星数量
    public int fortuneStarCount = 0;                    // 福星数量
    public int meteor = 0;                              // 流星
    public int wealthStarCount = 0;                     //财星
    public int disasterStarCount = 0;                   //祸星
    public int morningsStarCount = 0;                   //启明星
    public List<NumberCardInstance> morningStarTargetCards = new List<NumberCardInstance>(); //启明星锁定的数字卡（下一回合必抽）
    public int compassionStarCount = 0;                 //慈爱星
    public int bigSevenStarCount = 0;                   //大七星
    public bool hasFinancialExpert = false;             //金融专家
    public bool hasCasinoCommissioner = false;          //赌场专员
    public int luxuriant = 0;                           //琳琅满目
    public int sellOff = 0;                             //变卖
    public bool hasAddictedtoGambling = false;          //嗜赌如命
    public bool hasLovingWealth = false;                //爱财如命
    public int SpiritGodRealm = 0;                      //鬼神境
    public bool hasKingOfTheBoard = false;              //国王棋盘
    public BigInteger kingBoardGain = BigInteger.Zero;  //国王棋盘：当前每回合可获得的点数（每回合翻倍）
    public int AntimatterNucleus = 0;                   //反物质核
    public int Colorful = 0;                            //缤纷多彩
    public int AntimatterCloud = 0;                     //反物质云
    public bool hasRumination = false;                  //反刍
    public int RisingUpStepbyStep = 0;                  //步步高升
    public int EightWaysToWealth = 0;                   //八方来财
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

    // 延迟满足单条记录
    public struct DelayRecord
    {
        public int remainTurn;
        public int rewardMul;
    }
    public List<DelayRecord> delaySatisfactionList = new List<DelayRecord>();
    
    /// <summary>
    /// 初始化祝福系统
    /// </summary>
    public void InitializeBlessingSystem()
    {
        ownedBlessings.Clear();
        blessingTypeCount.Clear();
        blessingsEverPurchased.Clear();
        totalMultiplierBonus = 0;           // 永久倍率

        totalDialecticalCount = 0;          // 辩证主义购买次数
        AllGodsCount = 0;                   // 众神归位数量
        LuckTurnsCount = 0;                 // 转运
        CardMasterCount = 0;                //卡牌大师
        hasJackpot7 = false;                //逢七过
        wishCoinTargetBlessing = null;      //许愿币储存的祝福
        HasRichTreasure = 0;                //丰盈宝库
        nihilismCount = 0;                  //虚无主义数量
        hasLeadingCharge = false;           // 打头阵
        hasGambleToWin = false;             // 是否拥有赌为赢祝福
        hasLoanWallet = 0;                  // 贷款钱包
        hasIdealism = false;                //唯心主义
        hasUnstoppable = 0;                 // 势如破竹
        hasEnergySpread = 0;                // 是否拥有能量扩散
        hasRisingUp = 0;                    // 是否拥有节节高
        hasTemperlance = 0;                 // 是否拥有平衡节制
        wishCoinPurchaseCount = 0;          // 许愿币购买次数
        ApplyPragmatism = 0;                //实用主义
        hasGodOfGambler = false;            //赌神传说
        shortSightCount = 0;                // 短视数量
        dialecticalPricePercent = 0f;       // 辩证主义
        minimalismMultiplier = 0;           //极简主义
        minimalismCount = 0;                //极简主义数量
        pragmatismDeleteCount = 0;          //实用主义删除次数
        dayAfterDayCount = 0;               // 日积月累数量
        hasHastyAppreciation = 0;           // 走马观花
        hastyAppreciationBonus = 0;         // 走马观花的临时倍率
        bigSuccessCount = 0;                // 大成功数量
        delaySatisfactionList.Clear();      //延迟满足
        darkBoxTargetBlessing = null;       // 暗箱操作目标祝福
        darkBoxPurchaseCount = 0;           // 暗箱操作购买次数
        hasYinYang = false;                 // 阴阳
        hasFall = false;                    // 坠落
        reverse = 0;                        // 翻转
        hasJustice = false;                 // 绝对正义
        luckyStarCount = 0;                 // 幸运星数量
        fortuneStarCount = 0;               // 福星数量
        meteor = 0;                         // 流星
        wealthStarCount = 0;                //财星
        disasterStarCount = 0;              //祸星
        morningsStarCount = 0;              //启明星
        morningStarTargetCards.Clear();     //启明星锁定的数字卡
        compassionStarCount = 0;            //慈爱星
        bigSevenStarCount = 0;              //大七星
        hasFinancialExpert = false;         //金融专家
        hasCasinoCommissioner = false;      //赌场专员
        luxuriant = 0;                      //琳琅满目
        sellOff = 0;                        //变卖
        hasAddictedtoGambling = false;      //嗜赌如命
        hasLovingWealth = false;            //爱财如命
        SpiritGodRealm = 0;                 //鬼神境
        hasKingOfTheBoard = false;          //国王棋盘
        kingBoardGain = BigInteger.Zero;    //国王棋盘：每回合点数重置
        AntimatterNucleus = 0;              //反物质核
        Colorful = 0;                       //缤纷多彩
        AntimatterCloud = 0;                //反物质云
        hasRumination = false;              //反刍
        RisingUpStepbyStep = 0;             //步步高升
        EightWaysToWealth = 0;              //八方来财

        GetCurrentPriceMultiplier(); //重置折扣

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

        // 计算当前祝福的价格（和商店显示逻辑保持一致）
        int purchaseCount = GetBlessingCount(blessingData.blessingId);
        float multiplier = ShopManager.Instance != null
            ? ShopManager.Instance.GetCurrentBlessingPriceMultiplier()
            : GetCurrentPriceMultiplier();
        BigInteger finalPrice = (BigInteger)blessingData.CalculatePrice(purchaseCount, multiplier);

        if (finalPrice == 0)
        { 
        // 价格为0的祝福直接购买成功（如祝福逢七过，贷款钱包）
            Debug.Log($"购买【{blessingData.blessingName}】成功！价格：免费");
        }
        // 检查点数是否足够
        else if (GameManager.Instance.currentPoints < finalPrice)
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
        // 刷新商店显示
        // 购买成功后，强制重新计算商店里所有祝福的价格
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.ApplyDifficultySettings();
        }
        
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
    /// 获取已有祝福数量GetBlessingCount
    /// </summary>
    public int GetTotalBlessingCount()
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

    /// <summary>
    /// 应用祝福效果
    /// </summary>
    private void ApplyBlessingEffect(BlessingData blessingData)
    {
        switch (blessingData.blessingType)
        {
            case BlessingData.BlessingType.CardCheat:
                ActivateCardCheatSelection();
                ShowCardSelectionBlessingHint(blessingData);
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
                wishCoinPurchaseCount++;
                ActivateWishCoinSelection();
                ShowCardSelectionBlessingHint(blessingData);
                UIManager.Instance.RefreshShopUI();
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
                ActivateMoreMoreBetter();
                ShowCardSelectionBlessingHint(blessingData);
                Debug.Log("多多益善效果已激活，等待玩家选择填空卡");
                break;

            case BlessingData.BlessingType.CardMaster:
                // 卡牌大师 - 每张数字卡额外提供1倍率
                CardMasterCount++;
               
                Debug.Log("卡牌大师效果已激活，提供额外倍率");
                break;


            case BlessingData.BlessingType.DialecticalViewpoint:
                // 辩证主义 - 开局一次性奖励点数
                Debug.Log($"辩证主义已激活：每回合倍率+1、得24点、全商品价格+1%");
                break;

            case BlessingData.BlessingType.LuckTurns:
                // 转运 - 无法叠加：你的骰子投到1时，重投一次并将这次的结果作为该骰子的最终判定结果
                LuckTurnsCount = 1;
                Debug.Log("转运效果已激活（无法叠加）：骰子投出1时将重投一次！");
                break;

            case BlessingData.BlessingType.DoubleDown:
                // 倍投 - 倍率+1,价格翻倍
                totalMultiplierBonus += 1;
                Debug.Log("倍投效果已激活");
                break;

            case BlessingData.BlessingType.Raise:
                // 加注 - 倍率+1,价格+500
                totalMultiplierBonus += 1;
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
                // 友情折扣 - 所有商品价格下降10%
                globalPriceDiscountPercent = 10f; // 固定 10% 折扣
                Debug.Log("友情折扣效果已激活：所有商品价格 -10%");
                break;

            case BlessingData.BlessingType.Bless:
                // 眷顾：每拥有一个祝福，所有价格-1%
                blessDiscountPerBlessing = 1f;
                Debug.Log("眷顾效果已激活：每个祝福使所有商品价格-1%");
                break;

            case BlessingData.BlessingType.RichTreasury:
                // 丰盈宝库 - 不可叠加：商店刷新永久免费
                HasRichTreasure = 1;
                ShopManager.Instance.InitializeRefreshCost();
                Debug.Log("丰盈宝库效果已激活");
                break;

            case BlessingData.BlessingType.Empiricism:
                // 经验主义 - 不可叠加：每回合抽取数字卡时先抽取上一回合判定结果最大的数字卡
                Debug.Log("经验主义效果已激活");
                break;

            case BlessingData.BlessingType.Materialism:
                // 唯物主义：获得当前祝福数量×5永久倍率 → 清空所有祝福（包括自己）
                int totalBlessCount = GetTotalBlessingCount();
                totalMultiplierBonus += totalBlessCount * 5; // 永久倍率，不会被清空
                ClearAllBlessings();
                Debug.Log($"唯物主义触发！获得永久倍率：{totalBlessCount * 5}");
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
                ApplyUtopianismEffect();
                Debug.Log("空想主义效果已激活");
                break;

            case BlessingData.BlessingType.Pragmatism:
                // 实用主义 - 不可叠加：任意时刻你仅保留价值最高的填空卡并自动删除其它填空卡
                ApplyPragmatism = 1;
                ApplyPragmatismEffect();
                Debug.Log("实用主义效果已激活");
                break;

            case BlessingData.BlessingType.ShortSight:
                //短视 - 可叠加：永久倍率+10；每回合结束永久倍率-1
                totalMultiplierBonus += 10;
                shortSightCount++;
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

            case BlessingData.BlessingType.GamblingGodSage:
                //赌神传说  你的所有骰子的判定点数都会转化为本回合的额外临时倍率
                BlessingManager.Instance.hasGodOfGambler = true;
                Debug.Log("赌神传说效果已激活");
                break;

            case BlessingData.BlessingType.Unstoppable:
                // 势如破竹  不可叠加：你的绿色数字的正增量将转化为永久倍率
                hasUnstoppable = 1;
                Debug.Log("势如破竹效果已激活");
                break;

            case BlessingData.BlessingType.LoanWallet:
                // 贷款钱包 不可叠加：你获得你已获得点数的3倍的绝对值的点数，记录此点数，此后每回合你失去该点数15%的点数
                hasLoanWallet = 1;
                loan = BigInteger.Abs(GameManager.Instance.currentPoints * 3);
                GameManager.Instance.AddPoints(loan);
                Debug.Log("你获得你已获得点数的3倍的绝对值的点数，记录此点数，此后每回合你失去该点数15%的点数");
                break;

            case BlessingData.BlessingType.Minimalism:
                // 极简主义 可叠加：本局游戏每删除过一次游戏卡或填空卡，获得1永久倍率
                minimalismMultiplier = ShopManager.Instance.totalRemovedNumberCards + ShopManager.Instance.totalRemovedFormulaCards + pragmatismDeleteCount;
                totalMultiplierBonus += minimalismMultiplier;
                minimalismCount++;
                Debug.Log($"删除游戏卡次数:{ShopManager.Instance.totalRemovedNumberCards}," +
                    $"删除填空卡次数{ShopManager.Instance.totalRemovedFormulaCards}," +
                    $"实用主义删除次数{pragmatismDeleteCount}," +
                    $"极简主义数量{minimalismCount}"); 
                break;
            
            case BlessingData.BlessingType.DayAfterDay:
                // 日积月累  可叠加：每回合获得1永久倍率
                dayAfterDayCount++;
                break;

            case BlessingData.BlessingType.HastyAppreciation:
                // 走马观花  不可叠加：每刷新一次商店，下回合获得1临时倍率
                hasHastyAppreciation = 1;
                Debug.Log($"走马观花效果已激活,hasHastyAppreciation = {hasHastyAppreciation}");
                break;

        
            case BlessingData.BlessingType.BigSuccess:
                // 大成功  不可叠加：骰子掷出最大值时获得对应等级永久倍率
                bigSuccessCount++;
                Debug.Log("大成功 已激活！骰子掷出最大值时获得对应等级永久倍率");
                break;

            case BlessingData.BlessingType.AntimatterEnergy:
                // 反物质能  可叠加：失去240000点数，获得10永久倍率；特殊地，若此时你的点数变为负数，额外获得20永久倍率
                GameManager.Instance.AddPoints(-240000);
                totalMultiplierBonus += 10;
                Debug.Log($"反物质能已激活！现在的永久倍率为{totalMultiplierBonus}");
                if (GameManager.Instance.currentPoints < 0)
                {
                    totalMultiplierBonus += 20;
                    Debug.Log("反物质能触发额外加成：点数变为负数，额外获得20永久倍率");
                }
                break;

            case BlessingData.BlessingType.AllisVoid:
                // 皆空  不可叠加：购买此祝福后，失去所有点数与永久倍率，然后获得失去点数的0.01%的永久倍率（向下取整且不超过24亿）
                BigInteger newRate = GameManager.Instance.currentPoints / 10000;
                GameManager.Instance.AddPoints(-GameManager.Instance.currentPoints); // 点数变为0
                Debug.Log($"祝福皆空已激活,新增的永久倍率为{newRate}");
                totalMultiplierBonus = (long)(newRate > 2400000000 ? 2400000000 : newRate);
                Debug.Log($"现如今的永久倍率为{totalMultiplierBonus}");
                break;

            case BlessingData.BlessingType.DelaySatisfaction:
                // 延迟满足：立即获得-5永久倍率；5回合后获得10永久倍率
                totalMultiplierBonus -= 5;
                delaySatisfactionList.Add(new DelayRecord
                {
                    remainTurn = 5,
                    rewardMul = 10
                });
                Debug.Log("延迟满足：立即-5永久倍率，5回合后获取+10倍率");
                break;

            case BlessingData.BlessingType.DarkBoxOperation:
                // 暗箱操作：选择一个已拥有的可叠加祝福，下次商店刷新时该祝福以-2倍价格出现并扣2倍点数
                darkBoxPurchaseCount++;
                ActivateDarkBoxSelection();
                ShowCardSelectionBlessingHint(blessingData);
                break;

            case BlessingData.BlessingType.Reverse:
                // 翻转：购买后点数立即变为相反数
                reverse++;
                GameManager.Instance.currentPoints = -GameManager.Instance.currentPoints;
                Debug.Log($"翻转祝福触发：点数取反，当前点数: {GameManager.Instance.currentPoints}");
                break;

            case BlessingData.BlessingType.YinYang:
                // 阴阳：每回合开始时点数取反（逻辑在 GameManager.StartPlayerTurn 中）
                hasYinYang = true;
                Debug.Log("阴阳祝福激活：每回合开始时点数将取反");
                break;

            case BlessingData.BlessingType.Fall:
                // 坠落：计算结果为负数时扣点翻5倍（逻辑在 GameManager.AddPoints 中）
                hasFall = true;
                Debug.Log("坠落祝福激活：负数计算结果将翻5倍");
                break;

            case BlessingData.BlessingType.Justice:
                // 绝对正义：每回合结束时，若点数为负数，则点数变为0
                hasJustice = true;
                Debug.Log("绝对正义祝福激活：每回合结束时，点数取绝对值");
                break;

            case BlessingData.BlessingType.LuckyStar:
                // 幸运星：立即免费刷新商店（不增加刷新次数）
                luckyStarCount++;
                Debug.Log("幸运星祝福激活：商店将免费刷新");
                ShopManager.Instance.FreeRefreshShop();
                break;

            case BlessingData.BlessingType.FortuneStar:
                // 福星：随机选择一个黄金数字+1
                fortuneStarCount++;
                ApplyFortuneStarEffect();
                Debug.Log($"福星祝福激活：当前福星数量 {fortuneStarCount}");
                break;

            case BlessingData.BlessingType.WealthStar:
                // 财星：每回合结算 finalScore × 1.02^wealthStarCount（向下取整）
                wealthStarCount++;
                Debug.Log($"财星祝福激活：当前财星数量 {wealthStarCount}");
                break;

            case BlessingData.BlessingType.DisasterStar:
                // 祸星：此祝福不会随主动的商店刷新而被刷新。
                disasterStarCount++;
                Debug.Log($"祸星祝福激活：当前祸星数量 {disasterStarCount}");
                break;
            
            case BlessingData.BlessingType.CompassionStar:
                // 慈爱星：下一回合参与计算的绿色数字中最小的一个额外+1
                compassionStarCount++;
                Debug.Log($"慈爱星祝福激活：当前慈爱星数量 {compassionStarCount}");
                break;

            case BlessingData.BlessingType.MorningStar:
                // 启明星：购买此祝福后，选择一张数字卡：你在下一回合一定会抽到它
                morningsStarCount++;
                ActivateMorningStarSelection();
                ShowCardSelectionBlessingHint(blessingData);
                Debug.Log($"启明星祝福激活：当前启明星数量 {morningsStarCount}");
                break;

            case BlessingData.BlessingType.BigSevenStar:
                // 大七星：如果可能，失去除自身外所有名称末尾为星字的祝福各一个，然后获得24亿点，你每拥有一个大七星，获得的点数翻10倍
                bigSevenStarCount++;
                ActivateBigSevenStar();
                Debug.Log($"大七星祝福激活：当前大七星数量 {bigSevenStarCount}");
                break;

            case BlessingData.BlessingType.FinancialExpert:
                // 金融专家：你的黄金数字同时也拥有绿色数字的特性
                hasFinancialExpert = true;
                Debug.Log("金融专家祝福激活：你的黄金数字同时也拥有绿色数字的特性");
                break;

            case BlessingData.BlessingType.Luxuriant:
                // 琳琅满目 - 可叠加：祝福“琳琅满目”的价格翻倍，下次商店刷新的所有祝福均为未拥有的祝福
                luxuriant++;
                Debug.Log($"琳琅满目已激活！剩余次数：{luxuriant}");
                break;

            case BlessingData.BlessingType.SellOff:
                // 变卖 - 可叠加：如果至少有六张不含黄金数的数字卡，失去所有含黄金数的数字卡，获得黄金数总和两倍的永久倍率
                sellOff++;
                ApplySellOffEffect();
                Debug.Log($"变卖祝福激活：当前变卖数量 {sellOff}");
                break;

            case BlessingData.BlessingType.AddictedtoGambling:
                // 嗜赌如命 - 不可叠加：倍率+7，若抽卡时未抽取到含有骰子的数字卡，本回合的最终计算结果视为0
                hasAddictedtoGambling = true;
                totalMultiplierBonus += 7;
                Debug.Log("嗜赌如命已激活：倍率+7，未抽到含骰子的数字卡时本回合最终结果视为0");
                break;

            case BlessingData.BlessingType.LovingWealth:
                // 爱财如命 - 不可叠加：倍率+7，若抽卡时未抽取到含有黄金数的数字卡，本回合的最终计算结果视为0
                hasLovingWealth = true;
                totalMultiplierBonus += 7;
                Debug.Log("爱财如命已激活：倍率+7，未抽到含黄金数的数字卡时本回合最终结果视为0");
                break;

            case BlessingData.BlessingType.SpiritGodRealm:
                // 鬼神境 - 可叠加：接下来每回合获得骰子判定点数总和数量的随机可叠加祝福
                SpiritGodRealm++;
                Debug.Log($"鬼神境已激活！接下来每回合获得骰子判定点数总和数量的随机可叠加祝福，当前层数：{SpiritGodRealm}");
                break;

            case BlessingData.BlessingType.KingOfTheBoard:
                // 国王棋盘 - 不可叠加：每回合获得1点，此后每经过一回合依靠此祝福获得的点数翻倍
                hasKingOfTheBoard = true;
                kingBoardGain = BigInteger.One;
                Debug.Log("国王棋盘已激活！每回合获得1点，此后每经过一回合翻倍");
                break;

            case BlessingData.BlessingType.AntimatterNucleus:
                // 反物质核 - 可叠加：结算计算总倍率时使其变为相反数（逻辑在 GameManager.CalculateProcessSequence）
                AntimatterNucleus++;
                Debug.Log($"反物质核已激活！当前数量：{AntimatterNucleus}，此后每次结算总倍率将变为相反数");
                break;

            case BlessingData.BlessingType.Colorful:
                // 缤纷多彩 - 可叠加：每回合抽卡后，若抽到的所有数字卡同时含普通/黄金/绿色/骰子，则每层永久倍率+20且每层20%概率再获得一个缤纷多彩
                Colorful++;
                Debug.Log($"缤纷多彩已激活！当前数量：{Colorful}");
                break;


        }
    }

    /// <summary>
    /// 触发带选择功能的祝福时，在最顶层显示 名字:描述 提示（选择结束后由 EndCardSelection 统一关闭）。
    /// </summary>
    private void ShowCardSelectionBlessingHint(BlessingData blessingData)
    {
        if (blessingData == null) return;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowCardSelectionBlessing(blessingData.blessingName, blessingData.description);
        }
    }

    /// <summary>
    /// 提供转运祝福的激活状态（是否拥有转运祝福）
    /// </summary>
    public bool IsLuckTurnsActive()
    {
        return LuckTurnsCount > 0;
    }
 
    // 多多益善：选择一张公式卡进行复制
    private void ActivateMoreMoreBetter()
    {
        Debug.Log("=== 多多益善：选择要复制的公式卡 ===");

        CardSelectionManager.Instance.StartCardSelection(
            CardSelectionManager.SelectionMode.MoreMoreBetter,
            OnMoreMoreBetterSelected);

        UIManager.Instance.OpenMoreMoreBetterSelection();
    }

    // 多多益善的回调
    private void OnMoreMoreBetterSelected(object selectedObject)
    {
        CardSelectionManager.Instance.EndCardSelection();
        UIManager.Instance.CloseMoreMoreBetterSelection();

        // 适配你项目：用 FormulaCardData
        if (selectedObject is FormulaCardData formulaData)
        {
            Debug.Log("=== 多多益善：复制公式卡成功 ===");
            PlayerCardInventory.Instance.AddFormulaCard(formulaData);
            ForcePragmatismCleanup();// 强制触发实用主义防止bug
        }
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

    // 触发启明星选择流程
    private void ActivateMorningStarSelection()
    {
        Debug.Log("=== 启明星：开始选择数字卡 ===");

        CardSelectionManager.Instance.StartCardSelection(
            CardSelectionManager.SelectionMode.MorningStarSelect,
            OnMorningStarSelected);

        UIManager.Instance.OpenMorningStarNumberSelection();
    }

    // 启明星选择完成回调
    private void OnMorningStarSelected(object selectedObject)
    {
        CardSelectionManager.Instance.EndCardSelection();
        UIManager.Instance.CloseMorningStarNumberSelection();

        if (selectedObject is NumberCardInstance card)
        {
            morningStarTargetCards.Add(card);
            Debug.Log($"启明星已锁定：下一回合必抽【{card.cardData.cardName}】");
        }
    }

    // 触发大七星结算
    private void ActivateBigSevenStar()
    {
        Debug.Log("=== 大七星：开始结算 ===");

        if (blessingLibrary == null)
        {
            Debug.LogWarning("大七星：祝福库(blessingLibrary)为空，无法结算，祝福不生效");
            return;
        }

        // 找出所有名称末尾为"星"字的祝福（含大七星自身）
        List<BlessingData> starBlessings = new List<BlessingData>();
        foreach (var data in blessingLibrary.allBlessings)
        {
            if (data != null && !string.IsNullOrEmpty(data.blessingName) && data.blessingName.EndsWith("星"))
            {
                starBlessings.Add(data);
            }
        }

        // 检查是否拥有所有星字祝福（含大七星自身）
        bool ownsAllStars = true;
        foreach (var star in starBlessings)
        {
            if (GetBlessingCount(star.blessingId) <= 0)
            {
                ownsAllStars = false;
                Debug.Log($"大七星：缺少星字祝福【{star.blessingName}】，本次不生效");
                break;
            }
        }

        if (ownsAllStars)
        {
            // 失去除自身（大七星）外所有星字祝福各一个
            foreach (var star in starBlessings)
            {
                if (star.blessingType == BlessingData.BlessingType.BigSevenStar) continue;
                DecreaseBlessingByOne(star);
            }

            // 获得 24亿 × 10^大七星数量 的点数
            BigInteger reward = (BigInteger)2400000000 * BigInteger.Pow(10, bigSevenStarCount);
            GameManager.Instance.AddPoints(reward);
            Debug.Log($"大七星：获得点数 {reward}（大七星数量 {bigSevenStarCount}）");
        }
      
    }

    /// <summary>
    /// 将某祝福的拥有数量减少 1（用于大七星"失去各一个"）
    /// </summary>
    private void DecreaseBlessingByOne(BlessingData data)
    {
        if (data == null) return;

        // 1. 递减 ownedBlessings（ID -> 数量）
        if (ownedBlessings.ContainsKey(data.blessingId))
        {
            ownedBlessings[data.blessingId]--;
            if (ownedBlessings[data.blessingId] <= 0)
                ownedBlessings.Remove(data.blessingId);
        }

        // 2. 递减 blessingTypeCount（类型 -> 数量）
        if (blessingTypeCount.ContainsKey(data.blessingType))
        {
            blessingTypeCount[data.blessingType]--;
            if (blessingTypeCount[data.blessingType] <= 0)
                blessingTypeCount.Remove(data.blessingType);
        }

        // 3. 从 ownedBlessingInstance 移除一条对应记录
        var instance = ownedBlessingInstance.Find(i => i.data != null && i.data.blessingId == data.blessingId);
        if (instance != null)
            ownedBlessingInstance.Remove(instance);

        // 4. 递减对应的类型计数变量
        DecreaseStarCountVariable(data);
    }

    /// <summary>
    /// 递减星字祝福对应的类型计数变量（按 blessingId 精确匹配，避免类型枚举歧义）
    /// </summary>
    private void DecreaseStarCountVariable(BlessingData data)
    {
        switch (data.blessingId)
        {
            case 49: luckyStarCount = Math.Max(0, luckyStarCount - 1); break;        // 幸运星
            case 50: fortuneStarCount = Math.Max(0, fortuneStarCount - 1); break;    // 福星
            case 51: wealthStarCount = Math.Max(0, wealthStarCount - 1); break;      // 财星
            case 52: disasterStarCount = Math.Max(0, disasterStarCount - 1); break;  // 祸星
            case 53: compassionStarCount = Math.Max(0, compassionStarCount - 1); break; // 慈爱星
            case 54: morningsStarCount = Math.Max(0, morningsStarCount - 1); break; // 启明星
        }
    }

    /// <summary>Price
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

    // ====================== 暗箱操作 ======================
    /// <summary>
    /// 暗箱操作：打开选择UI，从已拥有的可叠加祝福中选择一个
    /// </summary>
    private void ActivateDarkBoxSelection()
    {
        CardSelectionManager.Instance.StartCardSelection(
        CardSelectionManager.SelectionMode.DarkBoxSelect,
        OnDarkBoxBlessingSelected);
        UIManager.Instance.OpenDarkBoxBlessSelection();
    }

    /// <summary>
    /// 暗箱操作：玩家选择祝福后的回调
    /// </summary>
    private void OnDarkBoxBlessingSelected(object selectedObject)
    {
        if (!(selectedObject is BlessingData selectedBlessing) || selectedBlessing == null)
        {
            Debug.LogError("暗箱操作选择无效！");
            return;
        }

        // 保存目标祝福
        darkBoxTargetBlessing = selectedBlessing;
        Debug.Log($"暗箱操作已锁定：下次商店必出【{selectedBlessing.blessingName}】，扣除2倍点数");
        UIManager.Instance.CloseDarkBoxBlessSelection();
    }

    /// <summary>
    /// 商店获取暗箱操作锁定的祝福（ShopManager 调用）
    /// </summary>
    public BlessingData GetDarkBoxTargetBlessing()
    {
        return darkBoxTargetBlessing;
    }

    /// <summary>
    /// 暗箱操作效果已使用（商店刷新后调用）
    /// </summary>
    public void ConsumeDarkBox()
    {
        darkBoxTargetBlessing = null;
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
    private int CalculateJackpot7Bonus()
    {
        if (!hasJackpot7) return 0;
        return 7;
    }

    /// <summary>
    /// 计算“众神归位”的倍率
    /// </summary>
    private int CalculateAllGodsInPlaceBonus()
    {
        if (AllGodsCount <= 0) return 0;
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
    private int CalculateCardMasterBonus()
    {
        if (CardMasterCount <= 0) return 0;
        int numberCardCount = PlayerCardInventory.Instance.GetAllNumberCards().Count;
        return CardMasterCount * numberCardCount;
    }

    public int CalculateHastyAppreciationBonus()
    {
        if (hasHastyAppreciation == 0) 
            return 0;
        else 
            return ShopManager.Instance.refreshCount; // 每刷新一次商店，下回合获得1临时倍率
    }

    /// <summary>
    /// 获取当前总倍率加成
    /// </summary>
    public BigInteger GetTotalMultiplierBonus()
    {
        return totalMultiplierBonus;
    }

    /// <summary>
    /// 神灯
    /// </summary>
    private void AddStackableBlessingsToOwned(int count)
    {
        count *= SpiritGodRealm; // 鬼神境叠加效果
         
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

    // 计算辩证主义总价格涨幅百分比
    private float CalculateDialecticalPricePercent()
    {
        // 获得辩证层数
        int layer = GetBlessingTypeCount(BlessingData.BlessingType.DialecticalViewpoint);
        // 总价格涨幅 = 层数 × 每回合累计百分比
        return layer * dialecticalPricePercent;
    }
    
    /// <summary>
    /// 获取对所有商品的价格折扣
    /// </summary>
    public float GetCurrentPriceMultiplier()
    {
        // 1. 辩证主义：每级 +1% 价格
        // 改用辩证主义每回合+购买累积的百分比
        float dialectPercent = CalculateDialecticalPricePercent();
        float multiplier = 1f + dialectPercent * 0.01f;

        // 2. 友情折扣（固定10%）不可叠加
        float friendDiscount = globalPriceDiscountPercent;

        // 3. 眷顾折扣：每个祝福-1%，上限80%
        int totalBlessings = GetTotalBlessingCount();
        float blessDiscount = totalBlessings * blessDiscountPerBlessing;
        blessDiscount = Mathf.Min(blessDiscount, maxBlessDiscountPercent); // 封顶80%

        // 4. 总折扣：加算
        float totalDiscount = friendDiscount + blessDiscount;

        // 5. 最终价格乘数
        multiplier *= (1f - totalDiscount / 100f);
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

    /// <summary>
    /// 清空所有祝福
    /// </summary>
    public void ClearAllBlessings()
    {
        ownedBlessings.Clear();
        blessingTypeCount.Clear();
        blessingsEverPurchased.Clear();
        totalDialecticalCount = 0;
        AllGodsCount = 0;
        LuckTurnsCount = 0;
        CardMasterCount = 0;
        hasJackpot7 = false;                // 逢七过
        HasRichTreasure = 0;                // 财富宝藏
        wishCoinTargetBlessing = null;      // 许愿币锁定的祝福
        nihilismCount = 0;                  // 虚无主义
        hasLeadingCharge = false;           // 先发制人
        ownedBlessingInstance.Clear();      // 祝福实例列表
        hasGambleToWin = false;             // 赌为赢
        hasIdealism = false;                // 理想主义
        hasEnergySpread = 0;                // 能量扩散
        hasRisingUp = 0;                    //节节高
        hasTemperlance = 0;                 //节制
        wishCoinPurchaseCount = 0;          // 许愿币购买次数
        globalPriceDiscountPercent = 0f;    // 友情折扣
        blessDiscountPerBlessing = 0f;      // 眷顾
        maxBlessDiscountPercent = 80f;      // 眷顾封顶
        shortSightCount = 0;                // 短视         
        GetCurrentPriceMultiplier();        // 强制刷新价格
        hasGodOfGambler = false;            // 赌神
        ApplyPragmatism = 0;                //实用主义
        dialecticalPricePercent = 0f;       // 辩证主义每回合累计百分比
        hasLoanWallet = 0;                  // 贷款钱包
        hasUnstoppable = 0;                 // 势如破竹
        minimalismMultiplier = 0;           //极简主义
        minimalismCount = 0;                //极简主义数量
        pragmatismDeleteCount = 0;          // 实用主义删除数量
        dayAfterDayCount = 0;               // 日积月累数量
        hasHastyAppreciation = 0;           // 走马观花
        hastyAppreciationBonus = 0;         // 走马观花的临时倍率
        bigSuccessCount = 0;                // 大成功数量
        delaySatisfactionList.Clear();      //延迟满足
        darkBoxTargetBlessing = null;       // 暗箱操作目标祝福
        darkBoxPurchaseCount = 0;           // 暗箱操作购买次数
        hasYinYang = false;                 // 阴阳
        hasFall = false;                    // 坠落
        reverse = 0;                        // 翻转
        wealthStarCount = 0;                // 财星
        disasterStarCount = 0;              // 祸星
        morningsStarCount = 0;              // 启明星
        morningStarTargetCards.Clear();     // 启明星锁定的数字卡
        compassionStarCount = 0;            // 慈爱星
        hasFinancialExpert = false;         // 金融专家
        luxuriant = 0;                      // 琳琅满目
        sellOff = 0;                        // 变卖
        hasAddictedtoGambling = false;      // 嗜赌如命
        hasLovingWealth = false;            // 爱财如命
        SpiritGodRealm = 0;                 // 鬼神境
        hasKingOfTheBoard = false;          // 国王棋盘
        kingBoardGain = BigInteger.Zero;    // 国王棋盘：每回合点数重置
        AntimatterNucleus = 0;              // 反物质核
        Colorful = 0;                       // 缤纷多彩
        AntimatterCloud = 0;                // 反物质云
        hasRumination = false;              // 反刍
        RisingUpStepbyStep = 0;             // 步步高升
        EightWaysToWealth = 0;              // 八方来财
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
    /// 获取最终总祝福倍率
    /// </summary>
    public long GetFinalBlessingMultiplier()
    {   // 获取永久倍率
        long total = totalMultiplierBonus;

        // 各类祝福倍率加成
        total += CalculateJackpot7Bonus();
        total += CalculateAllGodsInPlaceBonus();
        total += CalculateCardMasterBonus();
        total += hastyAppreciationBonus;// 走马观花
        return total;
    }

    /// <summary>
    /// 获取当前所有祝福提供的点数加成（理财大师等）
    /// </summary>
    public BigInteger GetBlessingPointBonus(BigInteger currentPoints)
    {
        return CalculateFinancialMasterBonus(currentPoints);
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

    public void AddDialecticalPerRoundMultiplier()
    {
        int layer = GetBlessingTypeCount(BlessingData.BlessingType.DialecticalViewpoint);
        if (layer <= 0) return;

        // 1. 永久倍率+1/每层
        totalMultiplierBonus += layer;
        // 2. 每回合得24点/每层
        GameManager.Instance.AddPoints(24 * layer);
        // 3. 全局价格永久+1%/每层
        dialecticalPricePercent += layer;

        Debug.Log($"辩证回合：层数{layer}，倍率+{layer}，点数+{24*layer}，价格每层+1%");
    }

    /// <summary>
    /// 国王棋盘：每回合开始时结算（本回合获得当前点数，每经过一回合该点数翻倍）
    /// </summary>
    public void ApplyKingOfTheBoardPerRound()
    {
        if (!hasKingOfTheBoard) return;
        if (GameManager.Instance == null) return;

        // 每回合获得当前点数
        GameManager.Instance.AddPoints(kingBoardGain);
        Debug.Log($"国王棋盘：本回合获得 {kingBoardGain} 点，下回合翻倍");
        // 每经过一回合，此祝福获得的点数翻倍
        kingBoardGain *= 2;
    }

    /// <summary>
    /// 缤纷多彩：每回合抽卡后判定一次。
    /// 若抽到的所有数字卡中同时包含普通数字、黄金数、绿色数字和骰子，
    /// 则每拥有1个缤纷多彩：永久倍率+20，并有20%概率再获得一个缤纷多彩。
    /// </summary>
    public void ApplyColorfulPerRound()
    {
        if (Colorful <= 0) return;
        if (CardManager.Instance == null) return;

        // 判定本回合抽到的所有数字卡（按组件互斥分类）是否四类齐全
        bool hasNormal = false, hasGolden = false, hasGreen = false, hasDice = false;
        foreach (var card in CardManager.Instance.currentNumberCards)
        {
            if (card == null || card.cardData == null) continue;
            ClassifyColorfulComponent(card.cardData.partA, ref hasNormal, ref hasGolden, ref hasGreen, ref hasDice);
            if (card.cardData.partB != null)
                ClassifyColorfulComponent(card.cardData.partB, ref hasNormal, ref hasGolden, ref hasGreen, ref hasDice);
        }

        // 四类需同时存在才触发
        if (!(hasNormal && hasGolden && hasGreen && hasDice))
        {
            Debug.Log($"缤纷多彩：本回合抽卡未四类齐全（普通={hasNormal}，黄金={hasGolden}，绿色={hasGreen}，骰子={hasDice}），不触发");
            return;
        }

        int layers = Colorful; // 按本回合开始时的拥有数量结算，本回合新获得的从下回合生效
        totalMultiplierBonus += 20 * layers;

        // 每层 20% 概率获得一个缤纷多彩
        int gain = 0;
        for (int i = 0; i < layers; i++)
        {
            if (UnityEngine.Random.value < 0.2f) gain++;
        }

        Debug.Log($"缤纷多彩触发！拥有 {layers} 层 → 永久倍率+{20 * layers}，{gain} 层roll中20%，获得 {gain} 个缤纷多彩");
        if (gain > 0) GrantColorfulCopies(gain);
    }

    /// <summary>
    /// 缤纷多彩：按组件判定四类（互斥）：骰子 > 绿色(递增) > 黄金数 > 普通数字
    /// </summary>
    private void ClassifyColorfulComponent(NumberComponent comp,
        ref bool hasNormal, ref bool hasGolden, ref bool hasGreen, ref bool hasDice)
    {
        if (comp == null) return;
        if (comp.isDice) hasDice = true;
        else if (comp.isIncremental) hasGreen = true;
        else if (comp.isGolden) hasGolden = true;
        else hasNormal = true;
    }

    /// <summary>
    /// 缤纷多彩：直接添加指定数量的缤纷多彩（只登记拥有数量，不立即触发效果，避免递归）
    /// </summary>
    private void GrantColorfulCopies(int count)
    {
        if (count <= 0 || blessingLibrary == null) return;
        BlessingData data = blessingLibrary.GetBlessingByType(BlessingData.BlessingType.Colorful);
        if (data == null) return;

        if (ownedBlessings.ContainsKey(data.blessingId)) ownedBlessings[data.blessingId] += count;
        else ownedBlessings[data.blessingId] = count;

        blessingsEverPurchased.Add(data.blessingId);

        if (!blessingTypeCount.ContainsKey(data.blessingType)) blessingTypeCount[data.blessingType] = 0;
        blessingTypeCount[data.blessingType] += count;

        for (int i = 0; i < count; i++)
        {
            ownedBlessingInstance.Add(new BlessingInstance(data, ownedBlessings[data.blessingId]));
        }

        Colorful += count;
        Debug.Log($"缤纷多彩：获得 {count} 个新副本，当前拥有 {Colorful} 个");
    }

    /// <summary>
    /// 实用主义：仅保留价值最高的填空卡，删除其它填空卡CalculateBlessingPrice
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

        //增加实用主义删除的卡牌数量（用于极简主义计算）
        pragmatismDeleteCount += formulaCards.Count - 1;
        if (BlessingManager.Instance.minimalismCount != 0)
        {
            totalMultiplierBonus += (formulaCards.Count - 1) * BlessingManager.Instance.minimalismCount;
            Debug.Log($"实用主义删除了{formulaCards.Count - 1}张填空卡，" +
                $"极简主义数量{BlessingManager.Instance.minimalismCount}," +
                $"极简主义获得永久倍率+{(formulaCards.Count - 1) * BlessingManager.Instance.minimalismCount}");
        }

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
    /// <summary>
    /// 获取本回合骰子判定点数总和（参与计算的数字卡中所有骰子的判定结果之和）
    /// </summary>
    public int GetCurrentTurnDiceTotal()
    {
        int totalDiceValue = 0;

        // 遍历本回合使用的所有数字卡
        if (CardManager.Instance != null)
        {
            foreach (var card in CardManager.Instance.selectedNumberCards)
            {
                if (card == null || card.cardData == null) continue;

                // PartA 是骰子 → 加当前掷出值
                if (card.cardData.partA != null && card.cardData.partA.isDice)
                {
                    totalDiceValue += card.currentA;
                }

                // PartB 是骰子 → 加当前掷出值
                if (card.cardData.partB != null && card.cardData.partB.isDice)
                {
                    totalDiceValue += card.currentB;
                }
            }
        }

        return totalDiceValue;
    }

    /// <summary>
    /// 祝福：赌神传说 —— 本回合所有骰子点数 → 额外临时倍率
    /// </summary>
    public int GetGodOfGamblerTempMultiplier()
    {
        if (!hasGodOfGambler) return 0;

        int totalDiceValue = GetCurrentTurnDiceTotal();
        Debug.Log($"【赌神传说】本回合骰子总点数 = {totalDiceValue} → 临时倍率 +{totalDiceValue}");
        return totalDiceValue;
    }

    /// <summary>
    /// 祝福：鬼神境 —— 每回合随机购买骰子判定点数总和数量的可叠加祝福
    /// 在每回合结算完成后调用（骰子判定结果已确定）
    /// 仅排除鬼神境自身；需要选择卡牌的祝福（许愿币/暗箱操作/启明星/老千/多多益善）也可随机购买到
    /// </summary>
    public void ApplySpiritGodRealm()
    {
        if (SpiritGodRealm <= 0) return;

        int diceTotal = GetCurrentTurnDiceTotal();
        if (diceTotal <= 0) return;

        if (blessingLibrary == null) return;

        // 获取所有可叠加祝福，仅排除鬼神境本身
        List<BlessingData> candidates = blessingLibrary.GetAllStackableBlessing();
        candidates.RemoveAll(b => b.blessingType == BlessingData.BlessingType.SpiritGodRealm);
        if (candidates.Count <= 0) return;

        System.Random rnd = new System.Random();
        int buyCount = 0;
        for (int i = 0; i < diceTotal; i++)
        {
            BlessingData selected = candidates[rnd.Next(candidates.Count)];
            if (selected == null) continue;

            // 直接调用购买祝福逻辑
            if (TryBuyBlessing(selected))
            {
                buyCount++;
            }
        }
        Debug.Log($"【鬼神境】本回合骰子判定点数总和 = {diceTotal}，随机购买 {buyCount} 个可叠加祝福");
    }
    
    /// <summary>
    /// 祝福：空想主义
    /// 立即获得一张未拥有的填空卡；
    /// 若已拥有全部种类，获得2400000000点并移除所有“空想主义”
    /// </summary>
    public void ApplyUtopianismEffect()
    {
        if (PlayerCardInventory.Instance == null)
        {
            Debug.LogError("空想主义：PlayerCardInventory 不存在");
            return;
        }

        // 直接用 ShopManager 里的公式卡库
        if (ShopManager.Instance == null || ShopManager.Instance.formulaCardLibrary == null)
        {
            Debug.LogError("空想主义：ShopManager 或公式卡库 missing");
            return;
        }

        FormulaCardLibrary formulaLib = ShopManager.Instance.formulaCardLibrary;

        // 1. 收集玩家已有的公式卡 ID
        HashSet<int> ownedIds = new HashSet<int>();
        foreach (var card in PlayerCardInventory.Instance.formulaCards)
        {
            if (card != null)
                ownedIds.Add(card.FormulaCardId);
        }

        // 2. 找出未拥有的卡
        List<FormulaCardData> missing = new List<FormulaCardData>();
        foreach (var card in formulaLib.allCards)
        {
            if (card != null && !ownedIds.Contains(card.FormulaCardId))
                missing.Add(card);
        }

        // 3. 发奖
        if (missing.Count > 0)
        {
            int r = UnityEngine.Random.Range(0, missing.Count);
            PlayerCardInventory.Instance.AddFormulaCard(missing[r]);

            if (CardManager.Instance != null)
                CardManager.Instance.SyncDeckFromInventory();

            Debug.Log($"【空想主义】获得：{missing[r].Name}");
            ForcePragmatismCleanup(); // 强制触发实用主义防止bug

        }
        else
        {
            // 已集齐 → 24亿 + 清空空想主义
            if (GameManager.Instance != null)
                GameManager.Instance.AddPoints(2400000000);

            RemoveAllUtopianism();
            Debug.Log("【空想主义】已集齐全部公式卡！获得 24 亿点！");
        }
    }

    /// <summary>
    /// 福星祝福效果：随机选择背包中一个黄金数字+1
    /// </summary>
    private void ApplyFortuneStarEffect()
    {
        var inventory = PlayerCardInventory.Instance;
        if (inventory == null || inventory.numberCards.Count == 0)
        {
            Debug.Log("福星祝福：背包中没有数字卡，无法生效");
            return;
        }

        // 筛选出包含黄金数字的卡牌
        var goldenCards = new List<NumberCardInstance>();
        foreach (var card in inventory.numberCards)
        {
            if (card.cardData.partA.isGolden || (card.cardData.partB != null && card.cardData.partB.isGolden))
            {
                goldenCards.Add(card);
            }
        }

        if (goldenCards.Count == 0)
        {
            Debug.Log("福星祝福：背包中没有黄金数字卡，无法生效");
            return;
        }

        // 随机选择一张黄金卡牌
        var selectedCard = goldenCards[UnityEngine.Random.Range(0, goldenCards.Count)];

        // 收集该卡牌中的所有黄金数字组件
        var goldenComponents = new List<NumberComponent>();
        if (selectedCard.cardData.partA.isGolden)
            goldenComponents.Add(selectedCard.cardData.partA);
        if (selectedCard.cardData.partB != null && selectedCard.cardData.partB.isGolden)
            goldenComponents.Add(selectedCard.cardData.partB);

        // 随机选择一个黄金数字组件并+1
        var selectedComponent = goldenComponents[UnityEngine.Random.Range(0, goldenComponents.Count)];
        selectedComponent.value += 1;

        Debug.Log($"福星祝福：{selectedCard.cardData.cardName} 的黄金数字 +1，当前值: {selectedComponent.value}");
    }

    /// <summary>
    /// 变卖祝福效果：如果至少有六张不含黄金数的数字卡，
    /// 失去所有含黄金数的数字卡，获得黄金数总和两倍的永久倍率
    /// </summary>
    private void ApplySellOffEffect()
    {
        var inventory = PlayerCardInventory.Instance;
        if (inventory == null)
        {
            Debug.LogWarning("变卖祝福：玩家卡牌库存为空，无法生效");
            return;
        }

        // 1. 分类：含黄金数的数字卡 / 不含黄金数的数字卡
        var goldenCards = new List<NumberCardInstance>();
        int nonGoldenCardCount = 0;

        foreach (var card in inventory.numberCards)
        {
            if (card == null || card.cardData == null) continue;

            bool hasGolden = card.cardData.partA.isGolden ||
                             (card.cardData.partB != null && card.cardData.partB.isGolden);

            if (hasGolden)
                goldenCards.Add(card);
            else
                nonGoldenCardCount++;
        }

        // 2. 条件判定：至少六张不含黄金数的数字卡
        if (nonGoldenCardCount < 6)
        {
            Debug.LogWarning($"变卖祝福：不含黄金数的数字卡不足6张（当前{nonGoldenCardCount}张），本次不生效");
            return;
        }

        // 3. 计算黄金数总和（所有含黄金数卡片的黄金数字组件值之和）
        long goldenSum = 0;
        foreach (var card in goldenCards)
        {
            if (card.cardData.partA.isGolden)
                goldenSum += card.cardData.partA.value;
            if (card.cardData.partB != null && card.cardData.partB.isGolden)
                goldenSum += card.cardData.partB.value;
        }

        // 4. 失去所有含黄金数的数字卡
        foreach (var card in goldenCards)
        {
            inventory.RemoveNumberCard(card);
        }

        // 5. 获得黄金数总和两倍的永久倍率
        totalMultiplierBonus += goldenSum * 2;

        // 6. 同步牌堆（移除的卡不再进入后续抽卡）
        if (CardManager.Instance != null)
            CardManager.Instance.SyncDeckFromInventory();

        Debug.Log($"变卖祝福生效：失去 {goldenCards.Count} 张含黄金数的数字卡，黄金数总和 {goldenSum}，" +
            $"获得永久倍率 +{goldenSum * 2}，当前永久倍率 {totalMultiplierBonus}");
    }

    /// <summary>
    /// 清空所有空想主义祝福
    /// </summary>
    private void RemoveAllUtopianism()
    {
        List<int> toRemove = new List<int>();

        foreach (var kvp in ownedBlessings)
        {
            BlessingData data = blessingLibrary.GetBlessingById(kvp.Key);
            if (data != null && data.blessingType == BlessingData.BlessingType.Utopianism)
                toRemove.Add(kvp.Key);
        }

        foreach (int id in toRemove)
        {
            ownedBlessings.Remove(id);
            blessingsEverPurchased.Remove(id);
        }

        if (blessingTypeCount.ContainsKey(BlessingData.BlessingType.Utopianism))
            blessingTypeCount[BlessingData.BlessingType.Utopianism] = 0;

        ownedBlessingInstance.RemoveAll(i =>
            i.data != null && i.data.blessingType == BlessingData.BlessingType.Utopianism);
    }
    
    /// <summary>
    /// 通用：强制触发实用主义，防止添加填空卡出bug
    /// </summary>
    public void ForcePragmatismCleanup()
    {
        // 只有激活实用主义才清理
        if (ApplyPragmatism == 1)
        {
            ApplyPragmatismEffect();
        }
    }

    /// <summary>
    /// 骰子面数 → 等级
    /// </summary>
    public int GetDiceRank(int sides)
    {
        return sides switch
        {
            4 => 1,
            6 => 2,
            8 => 3,
            12 => 4,
            20 => 5,
            _ => 0
        };
    }
    //延迟满足分开计数的方法
    public void UpdateDelaySatisfactionPerRound()
    {
        List<int> removeIndex = new List<int>();

        for (int i = 0; i < delaySatisfactionList.Count; i++)
        {
            DelayRecord record = delaySatisfactionList[i];
            record.remainTurn--;

            if (record.remainTurn <= 0)
            {
                totalMultiplierBonus += record.rewardMul;
                removeIndex.Add(i);
                Debug.Log("延迟满足倒计时结束，发放 10 永久倍率");
            }
            else
            {
                delaySatisfactionList[i] = record;
            }
        }

        // 倒序删除索引，防止列表移位错乱
        for (int i = removeIndex.Count - 1; i >= 0; i--)
        {
            delaySatisfactionList.RemoveAt(removeIndex[i]);
        }
    }
}       
