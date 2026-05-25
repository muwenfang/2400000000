using System.Collections.Generic;
using UnityEngine;
using static BlessingData;

/// <summary>
/// 祝福卡库 - 存储所有祝福卡数据
/// </summary>
[CreateAssetMenu(fileName = "BlessingLibrary", menuName = "BlessingLibrary")]
public class BlessingLibrary : ScriptableObject
{
    [Header("所有祝福卡")]
    public List<BlessingData> allBlessings = new List<BlessingData>();

    /// <summary>
    /// 初始化默认祝福卡（Context Menu for testing）
    /// </summary>
    [ContextMenu("Initialize Default Blessings")]
    public void InitializeDefaultBlessings()
    {
        allBlessings.Clear();

        //逢七过
        allBlessings.Add(CreateBlessing(
            id: 2,
            name: "逢七过",
            description: "倍率+7，但计算结果若为7的倍数或含有数字7，本回合的最终计算结果视为0",
            type: BlessingData.BlessingType.Jackpot7,
            basePrice: 0,
            isStackable: false,
            refreshBehavior: BlessingData.RefreshBehavior.NeverRefresh

        ));

        //纯粹容器
        allBlessings.Add(CreateBlessing(
            id: 3,
            name: "纯粹容器",
            description: "下一次购买祝福时不会获得祝福，该祝福变为被购买的祝福",
            type: BlessingData.BlessingType.PureVessel,
            basePrice: 1,
            isStackable: true
        ));    
        
        //倍投
        allBlessings.Add(CreateBlessing(
            id: 4,
            name: "倍投",
            description: "倍率+1，祝福“倍投”的价格翻倍",
            type: BlessingData.BlessingType.DoubleDown,
            basePrice: 200,
            isStackable: true
        ));

        //加注
        allBlessings.Add(CreateBlessing(
            id: 5,
            name: "加注",
            description: "倍率+1，祝福“加注”的价格+500",
            type: BlessingData.BlessingType.Raise,
            basePrice: 500,
            isStackable: true
        ));

        // 转运
        allBlessings.Add(CreateBlessing(
            id: 6,
            name: "转运",
            description: "骰子投到1时，重投一次并将这次的结果作为该骰子的最终判定结果",
            type: BlessingData.BlessingType.LuckTurns,
            basePrice: 10000,
            isStackable: false,
            refreshBehavior: BlessingData.RefreshBehavior.NeverRefresh

        ));

        // 许愿币
        allBlessings.Add(CreateBlessing(
            id: 7,
            name: "许愿币",
            description: "祝福“许愿币”的价格+1000；选择一个你已拥有且可叠加的祝福，下次商店刷新必定刷新出该祝福",
            type: BlessingData.BlessingType.WishingCoin,
            basePrice: 1000,
            isStackable: true
        ));    
        
        // 小卡牌包
        allBlessings.Add(CreateBlessing(
            id: 8,
            name: "小卡牌包",
            description: "立即获得3张随机数字卡",
            type: BlessingData.BlessingType.SmallCardPack,
            basePrice: 50000,
            isStackable: true
        ));

        // 众神归位
        allBlessings.Add(CreateBlessing(
            id: 9,
            name: "众神归位",
            description: "你每拥有一个祝福，倍率+1，祝福“众神归位”的价格为（2000000+已拥有祝福数量*200000）",
            type: BlessingData.BlessingType.AllGodsInPlace,
            basePrice: 2000000,
            isStackable: true
        ));
        
        // 理财大师
        allBlessings.Add(CreateBlessing(
            id: 10,
            name: "理财大师",
            description: "祝福“理财大师”的价格翻倍；你每回合结束时额外获得已拥有点数1%的点数（向下取整）",
            type: BlessingData.BlessingType.FinancialMaster,
            basePrice: 10000,
            isStackable: true,
            effectValue: 0.01f
        ));
        
        // 大卡牌包
        allBlessings.Add(CreateBlessing(
            id: 11,
            name: "大卡牌包",
            description: "立即获得5张随机数字卡",
            type: BlessingData.BlessingType.BigCardPack,
            basePrice: 100000,
            isStackable: true
        ));
        
        // 老千
        allBlessings.Add(CreateBlessing(
            id: 12,
            name: "老千",
            description: "择一张数字卡，将其替换为一张随机的数字卡",
            type: BlessingData.BlessingType.CardCheat,
            basePrice: 10000,
            isStackable: true
        
        ));

        //  多多益善
        allBlessings.Add(CreateBlessing(
            id: 13,
            name: "多多益善",
            description: "选择一张填空卡并将其复制",
            type: BlessingData.BlessingType.MoreMoreBetter,
            basePrice: 5000,
            isStackable: true,
            effectValue: 1f
        ));
        
        //  好事成双
        allBlessings.Add(CreateBlessing(
            id: 14,
            name: "好事成双",
            description: "立即获得你所获得的上一个可叠加的祝福",
            type: BlessingData.BlessingType.DoubleLuck,
            basePrice: 24000,
            isStackable: true
            
        ));
        
        //  卡牌大师
        allBlessings.Add(CreateBlessing(
            id: 15,
            name: "卡牌大师",
            description: "你每拥有一张数字卡，倍率+1",
            type: BlessingData.BlessingType.CardMaster,
            basePrice: 1000000,
            isStackable: true
            
        ));

        //  狂赌之渊
        allBlessings.Add(CreateBlessing(
            id: 16,
            name: "狂赌之渊",
            description: "立即将所有绿色数字变为~20~",
            type: BlessingData.BlessingType.CompulsiveGambler,
            basePrice: 2000000,
            isStackable: false,
            refreshBehavior: BlessingData.RefreshBehavior.CurrentRoundOnly

        ));

        //  赌为赢
        allBlessings.Add(CreateBlessing(
            id: 17,
            name: "赌为赢",
            description: "当你的数字卡总共拥有20个骰子（及以上）时，你的骰子每判定为一次20，你获得2400000000",
            type: BlessingData.BlessingType.GambletoWin,
            basePrice: 240000,
            isStackable: false,
            refreshBehavior: BlessingData.RefreshBehavior.NeverRefresh

        ));

        //  能量扩散
        allBlessings.Add(CreateBlessing(
            id: 18,
            name: "能量扩散",
            description: "不参与计算的绿色数字每回合也会+1",
            type: BlessingData.BlessingType.EnergySpread,
            basePrice: 2000000,
            isStackable: false,
            refreshBehavior: BlessingData.RefreshBehavior.NeverRefresh

        ));

        //  神灯
        allBlessings.Add(CreateBlessing(
            id: 19,
            name: "神灯",
            description: "随机获得三个可叠加祝福",
            type: BlessingData.BlessingType.MagicLamp,
            basePrice: 500000,
            isStackable: true
            
        ));
        
        //  友情折扣
        allBlessings.Add(CreateBlessing(
            id: 20,
            name: "友情折扣",
            description: "你每拥有一个祝福，所有数字卡、填空卡与祝福的价格-1%（最多-80%)",
            type: BlessingData.BlessingType.FriendDiscount,
            basePrice: 30000,
            isStackable: false,
            refreshBehavior: BlessingData.RefreshBehavior.NeverRefresh

        ));

        //  眷顾
        allBlessings.Add(CreateBlessing(
            id: 21,
            name: "眷顾",
            description: "你每拥有一个祝福，所有数字卡、填空卡与祝福的价格-1%（最多-70%）",
            type: BlessingData.BlessingType.Bless,
            basePrice: 150000,
            isStackable: false,
            refreshBehavior: BlessingData.RefreshBehavior.NeverRefresh

        ));

        //  丰盈宝库
        allBlessings.Add(CreateBlessing(
            id: 22,
            name: "丰盈宝库",
            description: "每回合第一次商店刷新免费",
            type: BlessingData.BlessingType.RichTreasury,
            basePrice: 50000,
            isStackable: false,
            refreshBehavior: BlessingData.RefreshBehavior.NeverRefresh

        ));

        //  唯心主义
        allBlessings.Add(CreateBlessing(
            id: 23,
            name: "唯心主义",
            description: "所有同等级的骰子参与运算时的判定结果总是相同的",
            type: BlessingData.BlessingType.Idealism,
            basePrice: 2400,
            isStackable: false,
            refreshBehavior: BlessingData.RefreshBehavior.NeverRefresh

        ));

        //  唯物主义
        allBlessings.Add(CreateBlessing(
            id: 24,
            name: "唯物主义",
            description: "立即获得等同于当前已拥有祝福数量5倍的永久倍率，然后失去所有祝福",
            type: BlessingData.BlessingType.Materialism,
            basePrice: 24000,
            isStackable: false,
            refreshBehavior: BlessingData.RefreshBehavior.CurrentRoundOnly

        ));

        //  虚无主义
        allBlessings.Add(CreateBlessing(
            id: 25,
            name: "虚无主义",
            description: "祝福“虚无主义”的价格翻倍；你每拥有一个“虚无主义”，商店刷新时额外有2%的概率将所有祝福刷新为“虚无主义”",
            type: BlessingData.BlessingType.Nihilism,
            basePrice: 1,
            isStackable: true
            
        ));

        //  辩证主义
        allBlessings.Add(CreateBlessing(
            id: 26,
            name: "辩证主义",
            description: "每回合倍率永久+1，获得24点，所有卡牌与祝福的价格+1%",
            type: BlessingData.BlessingType.DialecticalViewpoint,
            basePrice: 2400,
            isStackable: true,
            effectValue: 1f,
            bonusPoints: 24
        ));

        //  经验主义
        allBlessings.Add(CreateBlessing(
            id: 27,
            name: "经验主义",
            description: "每回合抽取数字卡时先抽取上一回合判定结果最大的数字卡",
            type: BlessingData.BlessingType.Empiricism,
            basePrice: 24000,
            isStackable: false,
            refreshBehavior: BlessingData.RefreshBehavior.NeverRefresh

        ));
        
        //  空想主义
        allBlessings.Add(CreateBlessing(
            id: 28,
            name: "空想主义",
            description: "你立即获得一张你未拥有的填空卡；如果在获得此祝福时拥有了所有类型的填空卡，立即获得2400000000点并失去所有的“空想主义”",
            type: BlessingData.BlessingType.Utopianism,
            basePrice: 240000,
            isStackable: true
            
        ));
        
        //  实用主义
        allBlessings.Add(CreateBlessing(
            id: 29,
            name: "实用主义",
            description: "立即删除除价格最高的填空卡以外的所有填空卡；此后无法购买比已拥有的填空卡价格更低的填空卡，如果你成功购买了一张填空卡，立即删除你之前拥有的那张填空卡",
            type: BlessingData.BlessingType.Pragmatism,
            basePrice: 5000,
            isStackable: false,
            refreshBehavior: BlessingData.RefreshBehavior.NeverRefresh

        ));

        //  短视
        allBlessings.Add(CreateBlessing(
            id: 30,
            name: "短视",
            description: "永久倍率+10；每回合结束永久倍率-1",
            type: BlessingData.BlessingType.ShortSight,
            basePrice: 500,
            isStackable: true

        ));

        //  节节高
        allBlessings.Add(CreateBlessing(
            id: 31,
            name: "节节高",
            description: "大于等于9的绿色数字递增后将变为{1}；触发此效果时，你的倍率永久+50",
            type: BlessingData.BlessingType.RisingUp,
            basePrice: 30000,
            isStackable: false,
            refreshBehavior: BlessingData.RefreshBehavior.NeverRefresh

        ));

        //  打头阵
        allBlessings.Add(CreateBlessing(
            id: 32,
            name: "打头阵",
            description: "你的计算结果的第一位强制变为9",
            type: BlessingData.BlessingType.LeadingCharge,
            basePrice: 20000,
            isStackable: false,
            refreshBehavior: BlessingData.RefreshBehavior.NeverRefresh

        ));

        //  平衡节制
        allBlessings.Add(CreateBlessing(
            id: 33,
            name: "平衡节制",
            description: "每次计算判定结果最大和最小的数字卡的判定结果变为所有参与计算的数字卡本轮判定结果的均值",
            type: BlessingData.BlessingType.Temperlance,
            basePrice: 2400,
            isStackable: false,
            refreshBehavior: BlessingData.RefreshBehavior.NeverRefresh

        ));

        //  赌具升级
        allBlessings.Add(CreateBlessing(
            id: 34,
            name: "赌具升级",
            description: "所有数字卡中的骰子升一级，祝福“赌具升级”的价格翻倍",
            type: BlessingData.BlessingType.GamblingGearUpgraded,
            basePrice: 2000000,
            isStackable: true

        ));

        //  赌神传说
        allBlessings.Add(CreateBlessing(
            id: 35,
            name: "赌神传说",
            description: "你的所有骰子的判定点数都会转化为本回合的额外临时倍率",
            type: BlessingData.BlessingType.GamblingGodSage,
            basePrice: 5000000,
            isStackable: false
        ));

        //  势如破竹
        allBlessings.Add(CreateBlessing(
            id: 36,
            name: "势如破竹",
            description: "你的绿色数字的正增量将转化为永久倍率",
            type: BlessingData.BlessingType.Unstoppable,
            basePrice: 5000000,
            isStackable: false,
            refreshBehavior: BlessingData.RefreshBehavior.NeverRefresh
        ));

        //  贷款钱包
        allBlessings.Add(CreateBlessing(
            id: 37,
            name: "贷款钱包",
            description: "你获得你已获得点数的3倍的绝对值的点数，记录此点数，此后每回合你失去该点数15%的点数",
            type: BlessingData.BlessingType.LoanWallet,
            basePrice: 0,
            isStackable: false,
            refreshBehavior: BlessingData.RefreshBehavior.NeverRefresh));

        //  极简主义
        allBlessings.Add(CreateBlessing(
            id: 38,
            name: "极简主义",
            description: "本局游戏每删除过一次游戏卡或填空卡，获得1永久倍率",
            type: BlessingData.BlessingType.Minimalism,
            basePrice: 100000,
            isStackable: true
        ));

        //  延迟满足
        allBlessings.Add(CreateBlessing(
            id: 39,
            name: "延迟满足",
            description: "立即获得-5永久倍率，5回合后获得10永久倍率",
            type: BlessingData.BlessingType.DelaySatisfaction,
            basePrice: 0,
            isStackable: true
        ));
        
        //  大成功
        allBlessings.Add(CreateBlessing(
            id: 40,
            name: "大成功",
            description: "任意骰子被判定为最大值时，获取等同于该骰子等级的永久倍率",
            type: BlessingData.BlessingType.BigSuccess,
            basePrice: 300000,
            isStackable: false,
            refreshBehavior: BlessingData.RefreshBehavior.NeverRefresh
        ));

        //  走马观花
        allBlessings.Add(CreateBlessing(
            id: 41,
            name: "走马观花",
            description: "每刷新一次商店，下回合获得1临时倍率",
            type: BlessingData.BlessingType.HastyAppreciation,
            basePrice: 50000,
            isStackable: false,
            refreshBehavior: BlessingData.RefreshBehavior.NeverRefresh
        ));

        //  日积月累
        allBlessings.Add(CreateBlessing(
            id: 42,
            name: "日积月累",
            description: "每回合获得1永久倍率",
            type: BlessingData.BlessingType.DayAfterDay,
            basePrice: 100000,
            isStackable: true
        ));

        //  反物质能
        allBlessings.Add(CreateBlessing(
            id: 43,
            name: "反物质能",
            description: "失去240000点数，获得10永久倍率；特殊地，若此时你的点数变为负数，额外获得10永久倍率",
            type: BlessingData.BlessingType.AntimatterEnergy,
            basePrice: 0,
            isStackable: true
        ));

        //  皆空
        allBlessings.Add(CreateBlessing(
            id: 44,
            name: "皆空",
            description: "购买此祝福后，失去所有点数与永久倍率，然后获得失去点数的0.01%的永久倍率（向下取整且不超过24亿）",
            type: BlessingData.BlessingType.AllisVoid,
            basePrice: 2400000000L,
            isStackable: false,
            refreshBehavior: BlessingData.RefreshBehavior.NeverRefresh
        ));

        Debug.Log($" 成功初始化 {allBlessings.Count} 个祝福！");
    }

    /// <summary>
    /// 创建单个祝福（运行时创建）
    /// </summary>
    private BlessingData CreateBlessing(int id, string name, string description,
        BlessingData.BlessingType type, long basePrice, bool isStackable,
        float effectValue = 0f, int bonusPoints = 0, BlessingData.
        RefreshBehavior refreshBehavior = BlessingData.RefreshBehavior.AlwaysRefresh)
    {
        BlessingData blessing = ScriptableObject.CreateInstance<BlessingData>();
        blessing.blessingId = id;
        blessing.blessingName = name;
        blessing.description = description;
        blessing.blessingType = type;
        blessing.basePrice = basePrice;
        blessing.isStackable = isStackable;
        blessing.effectValue = effectValue;     //效果数值（如倍率、百分比等）
        blessing.bonusPoints = bonusPoints;     //奖励点数
        blessing.refreshBehavior = refreshBehavior;
        return blessing;
    }

    /// <summary>
    /// 根据ID获取祝福
    /// </summary>
    public BlessingData GetBlessingById(int id)
    {
        return allBlessings.Find(b => b.blessingId == id);
    }

    /// <summary>
    /// 根据类型获取祝福
    /// </summary>
    public BlessingData GetBlessingByType(BlessingData.BlessingType type)
    {
        return allBlessings.Find(b => b.blessingType == type);
    }

    /// <summary>
    /// 随机获取一个祝福
    /// </summary>
    public BlessingData GetRandomBlessing()
    {
        if (allBlessings.Count == 0)
        {
            Debug.LogError("祝福库为空！");
            return null;
        }

        int randomIndex = Random.Range(0, allBlessings.Count);
        return allBlessings[randomIndex];
    }

    /// <summary>
    /// 获取所有祝福
    /// </summary>
    public List<BlessingData> GetAllBlessings()
    {
        return new List<BlessingData>(allBlessings);
    }

    /// <summary>
    /// 获取所有可叠加的祝福
    /// </summary>
    public List<BlessingData> GetAllStackableBlessing()
    {
        List<BlessingData> stackableBlessings = allBlessings.FindAll(b => b.isStackable);
        return stackableBlessings;
    }

}
