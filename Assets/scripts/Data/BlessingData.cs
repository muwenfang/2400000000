using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 祝福卡数据定义
/// </summary>
[CreateAssetMenu(fileName = "NewBlessing", menuName = "Cards/BlessingCard")]
public class BlessingData : ScriptableObject
{
    public int blessingId;           // 祝福ID
    public string blessingName;      // 祝福名称
    public string description;       // 祝福描述
    public int basePrice;            // 基础价格
    public bool isStackable;         // 是否可叠加
    public BlessingType blessingType;// 祝福类型

    // 特定效果参数
    public float effectValue;        // 效果数值（如倍率、百分比等）
    public int bonusPoints;          // 奖励点数

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
        RapidActivation,      // 高效催化
        FinancialMaster,      // 理财大师
        BigCardPack,          // 大卡牌包
        CardCheat,            // 老千
        GamblingGearUpgraded, // 赌具升级
        MoreMoreBetter,       // 多多益善
        Dyed,                 // 染色
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
        Pragmatism            // 实用主义
    }

    /// <summary>
    /// 计算当前价格（考虑购买次数和价格上升）
    /// </summary>
    public int CalculatePrice(int purchaseCount = 0, float priceMultiplier = 1.0f)
    {
        // 基础价格 * 购买次数倍数 * 价格乘数
        float calculatedPrice = basePrice  * priceMultiplier;
        
        // 虚无主义：自身价格翻倍
        if (blessingType == BlessingType.Nihilism)
        {
        // 安全获取虚无数量，避免空引用
            int nihilismCount = 0;
            if (BlessingManager.Instance != null)
            {
                nihilismCount = BlessingManager.Instance.nihilismCount;
            }
        
            calculatedPrice *= Mathf.Pow(2, nihilismCount);
        }
        
        return Mathf.RoundToInt(calculatedPrice);
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
