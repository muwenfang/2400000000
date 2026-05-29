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
    public long totalMultiplierBonus = 0; // 倍率加成
    public int totalDialecticalCount = 0;   // '辩证主义'购买次数
    private int AllGodsCount = 0;          // 众神归位数量 
    private int LuckTurnsCount = 0;           //是否激活转运，1是激活
    private bool hasJackpot7 = false;        //是否激活逢7过
    private int CardMasterCount = 0;       //是否激活卡牌大师 
    public int HasRichTreasure = 0;        //是否激活丰盈宝库
    private BlessingData wishCoinTargetBlessing = null; //许愿币储存的祝福
    public int wishCoinPurchaseCount = 0; // 许愿币购买次数（每次+1000价格）    // 许愿币购买次数
    public int nihilismCount = 0;       // 虚无主义数量
    public bool hasLeadingCharge = false; // 打头阵
    private bool hasGambleToWin = false; // 是否拥有赌为赢祝福
    public int hasEnergySpread = 0;      // 是否拥有能量扩散
    public int hasRisingUp = 0;          // 是否拥有节节高
    public int hasTemperlance = 0;       // 是否拥有平衡节制
    private readonly BigInteger GambleToWinReward = 2400000000; // 赌为赢奖励的点数    
    public bool hasIdealism = false;  //唯心主义
    public Dictionary<int, int> idealismDiceResults = new Dictionary<int, int>();  //唯心主义储存不同等级骰子出目的字典 
    public float dialecticalPricePercent = 0f; // 辩证主义：每回合累积的价格涨幅（每回合+1%）
    public int ApplyPragmatism = 0;//实用主义
    public bool hasGodOfGambler = false;//赌神传说
    public FormulaCardLibrary formulaCardLibrary;
    public float globalPriceDiscountPercent = 0f;// 友情折扣
    public float blessDiscountPerBlessing = 0f;// 眷顾
    public float maxBlessDiscountPercent = 70f;// 眷顾上限
    public int shortSightCount = 0;// 短视数量
    public int materialismFixedRate = 0;// 唯物主义一次性倍率
    public int hasUnstoppable = 0;// 势如破竹
    public int hasLoanWallet = 0;// 贷款钱包
    public BigInteger loan = BigInteger.Zero;// 贷款金额
    public int minimalismMultiplier = 0;//极简主义
    public int minimalismCount = 0;//极简主义数量
    public int pragmatismDeleteCount = 0;
    public int dayAfterDayCount = 0; // 日积月累数量
    public int hasHastyAppreciation = 0; // 走马观花
    public int hastyAppreciationBonus = 0;// 走马观花的临时倍率
    public int bigSuccessCount = 0;      // 大成功数量
    
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
        totalMultiplierBonus = 0;
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
        hasLoanWallet = 0;
        hasIdealism = false;
        hasUnstoppable = 0;
        hasEnergySpread = 0;
        hasRisingUp = 0;
        hasTemperlance = 0;
        wishCoinPurchaseCount = 0; 
        ApplyPragmatism = 0;    
        hasGodOfGambler = false;  
        shortSightCount = 0;                     
        dialecticalPricePercent = 0f;
        materialismFixedRate = 0; //唯物主义
        minimalismMultiplier = 0;//极简主义
        minimalismCount = 0;//极简主义数量
        pragmatismDeleteCount = 0;
        dayAfterDayCount = 0; // 日积月累数量
        hasHastyAppreciation = 0; // 走马观花
        hastyAppreciationBonus = 0;// 走马观花的临时倍率
        bigSuccessCount = 0;      // 大成功数量
        delaySatisfactionList.Clear();  //延迟满足
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
                materialismFixedRate = totalBlessCount * 5; // 永久倍率，不会被清空
                ClearAllBlessings();
                Debug.Log($"唯物主义触发！获得倍率：{materialismFixedRate}");
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
                // 反物质能  可叠加：失去240000点数，获得10永久倍率；特殊地，若此时你的点数变为负数，额外获得10永久倍率
                GameManager.Instance.AddPoints(-240000);
                totalMultiplierBonus += 10;
                Debug.Log($"反物质能已激活！现在的永久倍率为{totalMultiplierBonus}");
                if (GameManager.Instance.currentPoints < 0)
                {
                    totalMultiplierBonus += 10;
                    Debug.Log("反物质能触发额外加成：点数变为负数，额外获得10永久倍率");
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
        totalMultiplierBonus = 0;
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
        wishCoinPurchaseCount = 0;
        globalPriceDiscountPercent = 0f;    // 友情折扣
        blessDiscountPerBlessing = 0f;      // 眷顾
        maxBlessDiscountPercent = 80f;
        shortSightCount = 0;                     
        GetCurrentPriceMultiplier(); // 强制刷新价格
        hasGodOfGambler = false;
        ApplyPragmatism = 0;//实用主义
        dialecticalPricePercent = 0f;
        hasLoanWallet = 0;  // 贷款钱包
        hasUnstoppable = 0;  // 势如破竹
        minimalismMultiplier = 0; //极简主义
        minimalismCount = 0;//极简主义数量
        pragmatismDeleteCount = 0;
        dayAfterDayCount = 0; // 日积月累数量
        hasHastyAppreciation = 0; // 走马观花
        hastyAppreciationBonus = 0;// 走马观花的临时倍率
        bigSuccessCount = 0;      // 大成功数量
        delaySatisfactionList.Clear();  //延迟满足
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
    {
        long total = totalMultiplierBonus;

        // 各类祝福倍率加成
        total += CalculateJackpot7Bonus();
        total += CalculateAllGodsInPlaceBonus();
        total += CalculateCardMasterBonus();
        total += materialismFixedRate;// 唯物主义
        total += hastyAppreciationBonus;// 走马观花
        Debug.Log($"走马观花临时倍率为{hastyAppreciationBonus}");
        Debug.Log($"走马观花临时倍率为{hastyAppreciationBonus}");
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
    /// 祝福：赌神传说 —— 本回合所有骰子点数 → 额外临时倍率
    /// </summary>
    public int GetGodOfGamblerTempMultiplier()
    {
        if (!hasGodOfGambler) return 0;

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

        Debug.Log($"【赌神传说】本回合骰子总点数 = {totalDiceValue} → 临时倍率 +{totalDiceValue}");
        return totalDiceValue;
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
        List<DelayRecord> toRemove = new List<DelayRecord>();

        // 用 for 循环代替 foreach，就可以安全修改结构体成员
        for (int i = 0; i < delaySatisfactionList.Count; i++)
        {
            DelayRecord record = delaySatisfactionList[i];
            record.remainTurn--;

            if (record.remainTurn <= 0)
            {
                // 倒计时结束，发放倍率
                totalMultiplierBonus += record.rewardMul;
                toRemove.Add(record);
                Debug.Log("延迟满足倒计时结束，发放 10 永久倍率");
            }
            else
            {
                // 更新剩余回合数
                delaySatisfactionList[i] = record;
            }
        }

        // 移除已完成的记录
        foreach (var item in toRemove)
        {
            delaySatisfactionList.Remove(item);
        }
    }
}       
