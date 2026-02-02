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
    public NumberCardLayoutType layoutType;

    public NumberComponent partA;//骰子
    public NumberComponent partB;//递增

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
        {   
            // 掷骰子
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

    /// 计算卡牌价格
    public int GetNumberCardPrice(NumberCardData card)
    {
        // 简化组件命名，方便后续判断
        NumberComponent a = card.partA;
        NumberComponent b = card.partB;
        NumberCardData.LogicalType logic = card.logicalType;

        // 防御性检查：核心组件A不能为空
        if (a == null) return 0;

        // 1. 普通单数字卡（normal类型：无运算，仅PartA）
        if (logic == NumberCardData.LogicalType.normal)
        {
            // 递增单数字 {0} → 5
            if (a.isIncremental && !a.isDice)
                return 5;
            // 骰子单数字 ~20~ → 10
            else if (a.isDice && !a.isIncremental && a.diceSides == 20)
                return 10;
            // 普通固定数字（10、15、20等）
            else if (!a.isDice && !a.isIncremental)
            {
                switch (a.value)
                {
                    case 10: return 10;
                    case 15: return 15;
                    case 20: return 20;
                    case 25: return 25;
                    case 30: return 30;
                    case 50: return 50;
                    case 100: return 100;
                    default: return 0;
                }
            }
        }

        // 2. 加法运算卡（Addition类型：PartA + PartB）
        if (logic == NumberCardData.LogicalType.Addition && b != null)
        {
            // ~4~+~4~ → 5
            if (a.isDice && a.diceSides == 4 && b.isDice && b.diceSides == 4)
                return 5;
            // ~6~+~12~ → 10
            else if (a.isDice && a.diceSides == 6 && b.isDice && b.diceSides == 12)
                return 10;
            // ~8~+~20~ → 15
            else if (a.isDice && a.diceSides == 8 && b.isDice && b.diceSides == 20)
                return 15;
            // {0}+{0} → 10
            else if (a.isIncremental && b.isIncremental)
                return 10;
            // {0}+~20~ → 15
            else if (a.isIncremental && b.isDice && b.diceSides == 20)
                return 15;
        }

        // 3. 乘法运算卡（Multiplication类型：PartA * PartB）
        if (logic == NumberCardData.LogicalType.Multiplication && b != null)
        {
            // {0}*{0} → 30
            if (a.isIncremental && b.isIncremental)
                return 30;
            // ~20~*~20~ → 110
            else if (a.isDice && a.diceSides == 20 && b.isDice && b.diceSides == 20)
                return 110;
        }

        // 4. 幂运算卡（Power类型：PartA ^ PartB）- 覆盖所有价格表组合
        if (logic == NumberCardData.LogicalType.Power && b != null)
        {
            // 4.1 普通数字^骰子：2^~4~/2^~6~/2^~8~/2^~12~/2^~20~/3^~4~/3^~6~/3^~8~/3^~12~/~4~^~4~
            if (!a.isDice && !a.isIncremental && b.isDice)
            {
                if (a.value == 2 && b.diceSides == 4) return 10;    // 2^~4~ →10
                else if (a.value == 2 && b.diceSides == 6) return 20; // 2^~6~ →20
                else if (a.value == 2 && b.diceSides == 8) return 65; // 2^~8~ →65
                else if (a.value == 2 && b.diceSides == 12) return 700; // 2^~12~ →700
                else if (a.value == 2 && b.diceSides == 20) return 50000; // 2^~20~ →50000
                else if (a.value == 3 && b.diceSides == 4) return 30; // 3^~4~ →30
                else if (a.value == 3 && b.diceSides == 6) return 1100; // 3^~6~ →1100
                else if (a.value == 3 && b.diceSides == 8) return 10000; // 3^~8~ →10000
                else if (a.value == 3 && b.diceSides == 12) return 800000; // 3^~12~ →800000
            }
            // 4.2 骰子^递增：~4~^{0}/~6~^{0}/~8~^{0}
            else if (a.isDice && b.isIncremental)
            {
                if (a.diceSides == 4) return 10000;    // ~4~^{0} →10000
                else if (a.diceSides == 6) return 280000; // ~6~^{0} →280000
                else if (a.diceSides == 8) return 3000000; // ~8~^{0} →3000000
            }
            // 4.3 递增^普通数字：{0}^2/{0}^3/{0}^4/{0}^5/{0}^6/{0}^7/{0}^8
            else if (a.isIncremental && !b.isDice && !b.isIncremental)
            {
                if (b.value == 2) return 30;     // {0}^2 →30
                else if (b.value == 3) return 225; // {0}^3 →225
                else if (b.value == 4) return 1700; // {0}^4 →1700
                else if (b.value == 5) return 13500; // {0}^5 →13500
                else if (b.value == 6) return 110000; // {0}^6 →110000
                else if (b.value == 7) return 900000; // {0}^7 →900000
                else if (b.value == 8) return 7500000; // {0}^8 →7500000
            }
            // 4.4 普通数字^递增：2^{0}/3^{0}/4^{0}/5^{0}/6^{0}/7^{0}/8^{0}
            else if (!a.isDice && !a.isIncremental && b.isIncremental)
            {
                if (a.value == 2) return 110;     // 2^{0} →110
                else if (a.value == 3) return 3250; // 3^{0} →3250
                else if (a.value == 4) return 39000; // 4^{0} →39000
                else if (a.value == 5) return 270000; // 5^{0} →270000
                else if (a.value == 6) return 1350000; // 6^{0} →1350000
                else if (a.value == 7) return 5250000; // 7^{0} →5250000
                else if (a.value == 8) return 17000000; // 8^{0} →17000000
            }
            // 4.5 递增^骰子：{0}^~4~/~6~/{0}^~8~
            else if (a.isIncremental && b.isDice)
            {
                if (b.diceSides == 4) return 500;    // {0}^~4~ →500
                else if (b.diceSides == 6) return 21000; // {0}^~6~ →21000
                else if (b.diceSides == 8) return 1100000; // {0}^~8~ →1100000
            }
            // 4.6 递增^递增：{0}^{0} →45000000
            else if (a.isIncremental && b.isIncremental)
                return 45000000;
            // 4.7 骰子^骰子：~4~^~4~ →35
            else if (a.isDice && b.isDice && a.diceSides == 4 && b.diceSides == 4)
                return 35;
        }

        // 未匹配任何价格表组合时返回0
        return 0;
    }
}
