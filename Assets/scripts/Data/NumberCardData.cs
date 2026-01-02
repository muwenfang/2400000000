using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using static NumberCardFactory;

/// <summary>
/// 数字卡数据
/// </summary>

[System.Serializable]
public class NumberComponent
{
    public bool isDice = false;
    public bool isIncremental = false;
    public int value;//数值
    public int price;//价格
    public int diceSides;
}

[CreateAssetMenu(fileName = "MyNumberCards", menuName = "CardData/NumberCardData", order = 1)]
public class NumberCardData:ScriptableObject
{
    public string cardName;
    
    public NumberComponent partA;
    public NumberComponent partB;

    public enum LogicalType
    {
        Addition,
        Multiplication,
        Power,
        normal
    }

    public LogicalType logicalType;

}
public class NumberCardInstance 
{
    public NumberCardData cardData; //卡牌数据
    //当前数值
    public int currentA = 0;
    public int currentB = 0;

    public NumberCardInstance(NumberCardData cardData)
    {
        this.cardData = cardData;
        currentA = cardData.partA.value;
        currentB = cardData.partB.value;
    }


    public void OnDrawn()
    {   // 抽中时调用，处理掷骰和递增
        SpecialNumberHandler(cardData.partA, ref currentA);
        SpecialNumberHandler(cardData.partB, ref currentB);
    }
    private void SpecialNumberHandler(NumberComponent comp, ref int currentValue)
    {
        if (comp.isDice)
        {   // 掷骰子
            currentValue = DiceHelper.RollDice(comp.diceSides);
        }
        else if (comp.isIncremental)
        {
            // 递增
            currentValue++;
        }
    }


    public int GetOutPutValue()
    {

        switch (cardData.logicalType)
        {
            case NumberCardData.LogicalType.Addition:
                return currentA + currentB;

            case NumberCardData.LogicalType.Multiplication:
                return currentA * currentB;

            case NumberCardData.LogicalType.Power:
                return (int)Mathf.Pow(currentA, currentB);

            default:
                return currentA;
        }

    } 
    public int GetNumberCardPrice()
    {   // 计算卡牌价格
        //[to do]
        return 0;
    }

}
