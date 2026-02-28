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
    public int diceSides;
}

[CreateAssetMenu(fileName = "MyNumberCards", menuName = "CardData/NumberCardData", order = 1)]
public class NumberCardData : ScriptableObject
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
        Normal
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
        if (cardData.partB != null)
        {
            currentB = cardData.partB.value;
        }
    }


    /// <summary>
    /// 抽中时调用，只处理骰子，不处理递增
    /// 递增在结算后由 UpdateIncrementalCards 处理
    /// </summary>
    public void OnDrawn()
    {
        // 只处理骰子，递增值保持不变
        if (cardData.partA.isDice)
        {
            currentA = DiceHelper.RollDice(cardData.partA.diceSides);
        }

        if (cardData.partB != null && cardData.partB.isDice)
        {
            currentB = DiceHelper.RollDice(cardData.partB.diceSides);
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

    /// <summary>
    /// 按文档规则计算卡牌价格：期望计算 → 倍率修正 → 舍入
    /// </summary>
    public int GetNumberCardPrice(NumberCardData card)
    {
        if (card == null)
        {
            Debug.LogError("卡牌数据为空，无法计算价格！");
            return 0;
        }

        NumberComponent a = card.partA;
        NumberComponent b = card.partB;
        NumberCardData.LogicalType logic = card.logicalType;

        if (a == null)
        {
            Debug.LogError("卡牌PartA不能为空！");
            return 0;
        }

        // 第一步：计算数学期望
        float expectation = CalculateExpectation(a, b, logic);

        // 第二步：倍率修正（文档中所有倍率均为1.0，保留扩展接口）
        float rate = 1.0f;
        // 含绿色数字（递增）倍率
        if (a.isIncremental || (b != null && b.isIncremental))
            rate *= 1.0f;
        // 含骰子倍率
        if (a.isDice || (b != null && b.isDice))
            rate *= 1.0f;
        // 指数型卡牌倍率
        if (logic == NumberCardData.LogicalType.Power)
            rate *= 1.0f;
        float priceAfterRate = expectation * rate;

        // 第三步：舍入（最近5的倍数 + 最多3个有效数字）
        int finalPrice = RoundPrice(priceAfterRate);

        Debug.Log($"价格计算过程：期望={expectation:F2} → 倍率修正后={priceAfterRate:F2} → 最终价格={finalPrice}");
        return finalPrice;
    }

    /// <summary>
    /// 第一步：根据卡牌类型计算数学期望
    /// </summary>
    private float CalculateExpectation(NumberComponent a, NumberComponent b, NumberCardData.LogicalType logic)
    {
        // 非指数型卡牌（加法/乘法/单数字）
        if (logic != NumberCardData.LogicalType.Power)
        {
            return CalculateNonPowerExpectation(a, b, logic);
        }
        // 指数型卡牌（幂运算）
        else
        {
            if (b == null)
            {
                Debug.LogWarning("指数型卡牌PartB不能为空！");
                return 0;
            }
            return CalculatePowerExpectation(a, b);
        }
    }

    /// <summary>
    /// 计算非指数型卡牌期望（加法/乘法/单数字）
    /// </summary>
    private float CalculateNonPowerExpectation(NumberComponent a, NumberComponent b, NumberCardData.LogicalType logic)
    {
        // 单数字卡牌（仅PartA）
        if (logic == NumberCardData.LogicalType.Normal)
        {
            return GetComponentExpectation(a);
        }
        // 加法/乘法卡牌（PartA + PartB / PartA * PartB）
        else
        {
            if (b == null)
            {
                Debug.LogWarning("二元运算卡牌PartB不能为空！");
                return 0;
            }

            float expA = GetComponentExpectation(a);
            float expB = GetComponentExpectation(b);

            // 特殊处理：{x}*{y}型（按文档公式计算前9次均值）
            if (logic == NumberCardData.LogicalType.Multiplication && a.isIncremental && b.isIncremental)
            {
                float sum = 0;
                for (int i = 1; i <= 9; i++)
                {
                    float valA = a.value + i;
                    float valB = b.value + i;
                    sum += valA * valB;
                }
                return sum / 9f;
            }

            // 普通加法/乘法
            return logic == NumberCardData.LogicalType.Addition ? expA + expB : expA * expB;
        }
    }

    /// <summary>
    /// 获取单个组件（PartA/PartB）的非指数型期望
    /// </summary>
    private float GetComponentExpectation(NumberComponent comp)
    {
        if (comp.isDice)
        {
            // 骰子：(x+1)/2.0（x为骰子面数）
            return (comp.diceSides + 1) / 2.0f;
        }
        else if (comp.isIncremental)
        {
            // 递增：y+5（y为初始值）
            return comp.value + 5f;
        }
        else
        {
            // 普通数字：直接取数值
            return comp.value;
        }
    }

    /// <summary>
    /// 计算指数型卡牌期望（幂运算），对应文档8种指数组合
    /// </summary>
    private float CalculatePowerExpectation(NumberComponent a, NumberComponent b)
    {
        bool aIsDice = a.isDice;
        bool aIsInc = a.isIncremental;
        bool bIsDice = b.isDice;
        bool bIsInc = b.isIncremental;

        int x = a.isDice ? a.diceSides : a.value; // 骰子取面数，其他取数值
        int y = b.isDice ? b.diceSides : b.value;

        // 1. x^~y~ （底数普通/递增，指数骰子）
        if (!aIsDice && bIsDice && !bIsInc)
        {
            float sum = 0;
            for (int j = 1; j <= y; j++)
            {
                sum += Mathf.Pow(x, j);
            }
            return sum / y;
        }
        // 2. ~x~^y （底数骰子，指数普通/递增）
        else if (aIsDice && !bIsDice && !bIsInc)
        {
            float sum = 0;
            for (int i = 1; i <= x; i++)
            {
                sum += Mathf.Pow(i, y);
            }
            return sum / x;
        }
        // 3. x^{y} （底数普通，指数递增）
        else if (!aIsDice && !aIsInc && bIsInc && !bIsDice)
        {
            float sum = 0;
            for (int j = y + 1; j <= y + 9; j++)
            {
                sum += Mathf.Pow(x, j);
            }
            return sum / 9f;
        }
        // 4. {x}^y （底数递增，指数普通）
        else if (aIsInc && !aIsDice && !bIsDice && !bIsInc)
        {
            float sum = 0;
            for (int i = x + 1; i <= x + 9; i++)
            {
                sum += Mathf.Pow(i, y);
            }
            return sum / 9f;
        }
        // 5. {x}^~y~ （底数递增，指数骰子）
        else if (aIsInc && !aIsDice && bIsDice && !bIsInc)
        {
            float sum = 0;
            for (int i = x + 1; i <= x + 9; i++)
            {
                float innerSum = 0;
                for (int j = 1; j <= y; j++)
                {
                    innerSum += Mathf.Pow(i, j);
                }
                sum += innerSum;
            }
            return sum / (9f * y);
        }
        // 6. ~x~^{y} （底数骰子，指数递增）
        else if (aIsDice && !aIsInc && bIsInc && !bIsDice)
        {
            float sum = 0;
            for (int i = 1; i <= x; i++)
            {
                float innerSum = 0;
                for (int j = y + 1; j <= y + 9; j++)
                {
                    innerSum += Mathf.Pow(i, j);
                }
                sum += innerSum;
            }
            return sum / (9f * x);
        }
        // 7. ~x~^~y~ （底数骰子，指数骰子）
        else if (aIsDice && !aIsInc && bIsDice && !bIsInc)
        {
            float sum = 0;
            for (int i = 1; i <= x; i++)
            {
                float innerSum = 0;
                for (int j = 1; j <= y; j++)
                {
                    innerSum += Mathf.Pow(i, j);
                }
                sum += innerSum;
            }
            return sum / (x * y);
        }
        // 8. {x}^{y} （底数递增，指数递增）
        else if (aIsInc && !aIsDice && bIsInc && !bIsDice)
        {
            float sum = 0;
            for (int i = 1; i <= 9; i++)
            {
                int baseVal = x + i;
                int expVal = y + i;
                sum += Mathf.Pow(baseVal, expVal);
            }
            return sum / 9f;
        }
        // 未匹配的指数组合
        else
        {
            Debug.LogWarning($"未匹配的指数型组合：A(骰子={aIsDice},递增={aIsInc})，B(骰子={bIsDice},递增={bIsInc})");
            return 0;
        }
    }

    /// <summary>
    /// 第三步：舍入处理（最近5的倍数 + 最多3个有效数字）
    /// </summary>
    private int RoundPrice(float price)
    {
        if (price <= 0)
            return 0;

        // 第一步：四舍五入到最近的5的倍数
        int roundedTo5 = Mathf.RoundToInt(price / 5) * 5;

        // 第二步：最多保留3个有效数字
        if (roundedTo5 == 0)
            return 0;

        // 计算有效数字位数
        int digitCount = (int)Mathf.Log10(Mathf.Abs(roundedTo5)) + 1;
        if (digitCount <= 3)
        {
            // 不足3位有效数字，直接返回5的倍数结果
            return roundedTo5;
        }
        else
        {
            // 超过3位有效数字，保留3位并修正为5的倍数
            float scale = Mathf.Pow(10, digitCount - 3);
            int roundedTo3Sig = Mathf.RoundToInt(roundedTo5 / scale) * (int)scale;
            // 确保最终结果仍是5的倍数
            return Mathf.RoundToInt(roundedTo3Sig / 5) * 5;
        }
    }
}