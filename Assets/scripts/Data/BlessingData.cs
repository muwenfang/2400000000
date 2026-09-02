using System;
using System.Collections.Generic;
using UnityEngine;
using BigInteger = System.Numerics.BigInteger;

/// <summary>
/// 祝福卡数据定义
/// </summary>
[CreateAssetMenu(fileName = "NewBlessing", menuName = "Cards/BlessingCard")]
public class BlessingData : ScriptableObject
{
    public int blessingId;           // 祝福ID
    public string blessingName;      // 祝福名称
    public string description;       // 祝福描述
    public long basePrice;           // 基础价格
    public bool isStackable;         // 是否可叠加
    public BlessingType blessingType;// 祝福类型


    /// <summary>
    /// 祝福刷新行为
    /// </summary>
    public enum RefreshBehavior
    {
        AlwaysRefresh,      // 商店购买后会继续刷新（可以多次购买）
        NeverRefresh,       // 不会继续刷新（只能购买一次，之后永不出现）
        CurrentRoundOnly    // 本回合不再刷新（同一次商店开启中最多出现一个，下次刷新可能出现）
    }
    public RefreshBehavior refreshBehavior = 
        RefreshBehavior.AlwaysRefresh; // 默认为总是刷新
    public enum BlessingType
    {
        Jackpot7,             // 逢七过
        PureVessel,           // 纯粹容器
        DoubleDown,           // 倍投
        Raise,                // 加注
        QuitGambling,         // 戒赌
        LuckTurns,            // 转运
        WishingCoin,          // 许愿币
        SmallCardPack,        // 小卡牌包
        AllGodsInPlace,       // 众神归位
        FinancialMaster,      // 理财大师
        BigCardPack,          // 大卡牌包
        CardCheat,            // 老千
        MoreMoreBetter,       // 多多益善
        DoubleLuck,           // 好事成双
        CardMaster,           // 卡牌大师
        CompulsiveGambler,    // 狂赌之渊
        GambletoWin,          // 赌为赢
        EnergySpread,         // 能量扩散
        MagicLamp,            // 神灯
        FriendDiscount,       // 友情折扣
        Bless,                // 眷顾
        RichTreasury,         // 丰盈宝库
        Idealism,             // 唯心主义
        Materialism,          // 唯物主义
        Nihilism,             // 虚无主义
        DialecticalViewpoint, // 辩证主义
        Empiricism,           // 经验主义  
        Utopianism,           // 空想主义
        Pragmatism,           // 实用主义
        LeadingCharge,        // 打头阵 
        ShortSight,           // 短视
        RisingUp,             // 节节高
        Temperlance,          // 平衡节制
        GamblingGearUpgraded, // 赌具升级
        GamblingGodSage,      // 赌神传说
        Unstoppable,          // 势如破竹
        LoanWallet,           // 贷款钱包
        Minimalism,           // 极简主义
        DelaySatisfaction,    // 延迟满足
        DayAfterDay,          // 日积月累
        HastyAppreciation,    // 走马观花
        BigSuccess,           // 大成功
        AntimatterEnergy,     // 反物质能
        AllisVoid,            // 皆空
        DarkBoxOperation,     // 暗箱操作
        Reverse,              // 翻转
        YinYang,              // 阴阳
        Fall,                 // 坠落
        Justice,              // 绝对正义
        LuckyStar,            //幸运星
        FortuneStar,          //福星
        DisasterStar,         //祸星
        WealthStar,           //财星
        CompassionStar,       //慈爱星
        Meteor,               //流星
        MorningStar,          //启明星
        BigSevenStar,         //大七星
        FinancialExpert,      //金融专家
        CasinoCommissioner ,  //赌场专员
        Luxuriant,            //琳琅满目
        SellOff,              //变卖 
        AddictedtoGambling,   //嗜赌如命
        LovingWealth,         //爱财如命
        SpiritGodRealm,       //鬼神境
        KingOfTheBoard,       //国王棋盘
        AntimatterNucleus,     //反物质核
        Colorful,             //缤纷多彩
        AntimatterCloud       //反物质云
    }

    /// <summary>
    /// 计算当前价格（先算自身涨价，再算百分比折扣）
    /// </summary>
    public BigInteger CalculatePrice(int purchaseCount = 0, float priceMultiplier = 1.0f)
    {
        // 1. 基础价
        BigInteger calculatedPrice = (BigInteger)basePrice;

        // 虚无主义价格翻倍逻辑
        if (blessingType == BlessingType.Nihilism)
        {
            int count = BlessingManager.Instance != null ? BlessingManager.Instance.nihilismCount : 0;
            for (int i = 0; i < count; i++)
            {
                calculatedPrice *= 2;
            }
        }

        // 许愿币：每购买一次价格+1000
        if (blessingType == BlessingType.WishingCoin)
            calculatedPrice += (BigInteger)purchaseCount * 1000;

        // 赌具升级：每次购买后价格翻倍
        if (blessingType == BlessingType.GamblingGearUpgraded)
            for (int i = 0; i < purchaseCount; i++)
                calculatedPrice *= 2;

        // 倍投：每次购买后价格翻倍
        if (blessingType == BlessingType.DoubleDown)
            for (int i = 0; i < purchaseCount; i++)
                calculatedPrice *= 2;

        // 加注：每次购买后，价格 +500
        if (blessingType == BlessingType.Raise)
            calculatedPrice += (BigInteger)purchaseCount * 500;

        // 众神归位：每拥有一个祝福，价格+100000
        if (blessingType == BlessingType.AllGodsInPlace)
        {
            int totalCount = BlessingManager.Instance.GetTotalBlessingCount();
            calculatedPrice = 2000000 + (BigInteger)totalCount * 100000;
        }

        // 理财大师：每次购买后价格翻倍
        if (blessingType == BlessingType.FinancialMaster)
            for (int i = 0; i < purchaseCount; i++)
                calculatedPrice *= 2;

        // 虚无主义保底（最后保底）
        if (blessingType == BlessingType.Nihilism)
        {
            if (calculatedPrice <= 0)
            {
                calculatedPrice = 1;
            }
        }

        return calculatedPrice;
    }
}
/// <summary>
/// 玩家已购买的祝福实例
/// </summary>
[System.Serializable]
public class BlessingInstance
{
    public BlessingData data;
    public int purchaseCount = 1; // 购买次数（用于叠加效果计算）

    public BlessingInstance(BlessingData data, int count = 1)
    {
        this.data = data;
        this.purchaseCount = count;
    }
}
