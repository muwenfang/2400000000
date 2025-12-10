using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

/// <summary>
/// 数字卡数据
/// </summary>
public class NumberCardData
{
    public enum CardType { Fixed, Dice, Incremental }
    public CardType cardType;

    [Header("固定数字")]
    public int fixedValue;

    [Header("骰子类型")]
    public DiceType diceType; // 1-5级对应不同面数
    public enum DiceType { D4 = 4, D6 = 6, D8 = 8, D12 = 12, D20 = 20 }

    [Header("递增数字")]
    public int incrementalValue; // 当前值
    public int incrementalBase;  // 基础递增值

    public string GetDisplayValue()
    {//显示卡牌数值
     //【to do】
        switch (cardType)
        { 
            case CardType.Fixed:
                return fixedValue.ToString();
            case CardType.Dice:
            //显示骰子类型
            case CardType.Incremental:
            //显示递增数字卡牌的当前值
            default: 
                return "" ;
        }
    }

    public BigInteger GetValue()
    {
        //获取卡牌数值
        //【to do】
        switch (cardType)
        {
            case CardType.Fixed:
            //返回固定数字值
            case CardType.Dice:
            //返回掷骰子结果
            case CardType.Incremental:
            //返回递增数字卡牌的当前值
            default:
                return 0;

        }
    }


}
