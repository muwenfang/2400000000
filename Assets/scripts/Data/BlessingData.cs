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

    public enum BlessingType
    {
        FinancialMaster,    // 理财大师
        MoreMoreBetter,     // 多多益善
        DialecticalViewpoint // 辩证主义
    }

    /// <summary>
    /// 计算当前价格（考虑购买次数和价格上升）
    /// </summary>
    public int CalculatePrice(int purchaseCount = 0, float priceMultiplier = 1.0f)
    {
        // 基础价格 * 购买次数倍数 * 价格乘数
        float calculatedPrice = basePrice * Mathf.Pow(1.01f, purchaseCount) * priceMultiplier;
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
