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
    public bool isDice = false;//是否为骰子
    public bool isIncremental = false;//是否为递增
    public int value;//数值（基础值保留int，无溢出风险）
    public int diceSides;//骰子面数（基础值保留int）
}

[CreateAssetMenu(fileName = "MyNumberCards", menuName = "CardData/NumberCardData", order = 1)]
public class NumberCardData : ScriptableObject//不挂载在 GameObject 上
{                                           //直接作为数据容器使用即可
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

public class NumberCardInstance //数字卡实例，包含当前数值和计算方法
{
    public NumberCardData cardData; //卡牌数据
    //当前数值保留int（基础值，运算时转BigInteger）
    public int currentA = 0;
    public int currentB = 0;

    public NumberCardInstance(NumberCardData cardData)//构造函数，初始化当前数值
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

    /// <summary>
    /// 返回BigInteger解决溢出
    /// </summary>
    public int GetOutPutValue()
    {
        int a = currentA;
        int b = currentB;

        switch (cardData.logicalType)
        {
            case NumberCardData.LogicalType.Addition:
                return a + b;
            case NumberCardData.LogicalType.Multiplication:
                return a * b;
            case NumberCardData.LogicalType.Power:
                return (int)System.Math.Pow(a, b);
            default:
                return a;
        }
    }

    /// <summary>
    /// GetNumberCardPrice 返回 long，避免溢出
    /// 按文档规则计算卡牌价格：期望计算 → 倍率修正 → 舍入
    /// </summary>
    public long GetNumberCardPrice(NumberCardData card)  // 返回值改为 long
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

        try
        {
            // 第一步：计算数学期望
            long expectation = CalculateExpectation(a, b, logic);

            // 第二步：倍率修正（文档中所有倍率均为1.0）
            float rate = 2.0f;
            if (a.isIncremental || (b != null && b.isIncremental))
                rate *= 1.0f;
            if (a.isDice || (b != null && b.isDice))
                rate *= 1.0f;
            if (logic == NumberCardData.LogicalType.Power)
                rate *= 2.0f;

            long priceAfterRate = (long)(expectation * rate);

            // 第三步：舍入
            long finalPrice = RoundPrice(priceAfterRate);

            Debug.Log($"价格计算过程：期望={expectation:F2} → 倝率修正后={priceAfterRate:F2} → 最终价格={finalPrice}");
            return finalPrice;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"价格计算异常：{e.Message}，卡牌：{card.cardName}，返回0");
            return 0;
        }
    }

    /// <summary>
    /// 第一步：根据卡牌类型计算数学期望
    /// 返回BigInteger，完全保留原计算逻辑
    /// </summary>
    private long CalculateExpectation(NumberComponent a, NumberComponent b, NumberCardData.LogicalType logic)
    {
        if (logic != NumberCardData.LogicalType.Power)
        {
            return CalculateNonPowerExpectation(a, b, logic);
        }
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
    /// 返回BigInteger，先乘后除避免浮点均值误差
    /// </summary>
    private long CalculateNonPowerExpectation(NumberComponent a, NumberComponent b, NumberCardData.LogicalType logic)
    {
        if (logic == NumberCardData.LogicalType.Normal)
        {
            return GetComponentExpectation(a);
        }
        else
        {
            if (b == null)
            {
                Debug.LogWarning("二元运算卡牌PartB不能为空！");
                return 0;
            }

            long expA = GetComponentExpectation(a);
            long expB = GetComponentExpectation(b);

            // 特殊处理：{x}*{y}型（按文档公式计算前9次均值）
            if (logic == NumberCardData.LogicalType.Multiplication && a.isIncremental && b.isIncremental)
            {
                long sum = 0;
                for (int i = 1; i <= 9; i++)
                {
                    long valA = a.value + i;
                    long valB = b.value + i;
                    sum += valA * valB;
                }
                return sum / 9;
            }

            // 普通加法/乘法，直接BigInteger运算
            return logic == NumberCardData.LogicalType.Addition ? expA + expB : expA * expB;
        }
    }

    /// <summary>
    /// 获取单个组件（PartA/PartB）的期望值（用long）
    /// </summary>
    private long GetComponentExpectation(NumberComponent comp)
    {
        if (comp.isDice)
        {
            // 骰子：(面数+1)/2
            return (comp.diceSides + 1) / 2;
        }
        else if (comp.isIncremental)
        {
            // 递增：value+5
            return comp.value + 5;
        }
        else
        {
            // 普通数字
            return comp.value;
        }
    }

    /// <summary>
    /// 计算指数型卡牌期望（8种组合）
    /// 返回long，用整数运算计算
    /// </summary>
    private long CalculatePowerExpectation(NumberComponent a, NumberComponent b)
    {
        bool aIsDice = a.isDice;
        bool aIsInc = a.isIncremental;
        bool bIsDice = b.isDice;
        bool bIsInc = b.isIncremental;

        // 骰子取面数，其他取数值
        long x = a.isDice ? a.diceSides : a.value;
        long y = b.isDice ? b.diceSides : b.value;

        try
        {
            // 1. x^~y~ （底数普通，指数骰子）
            if (!aIsDice && !aIsInc && bIsDice && !bIsInc)
            {
                long sum = 0;
                for (long j = 1; j <= y; j++)
                {
                    sum += (long)System.Math.Pow(x, (int)j);
                }
                return sum / y;
            }
            // 2. ~x~^y （底数骰子，指数普通）
            else if (aIsDice && !aIsInc && !bIsDice && !bIsInc)
            {
                long sum = 0;
                for (long i = 1; i <= x; i++)
                {
                    sum += (long)System.Math.Pow((int)i, (int)y);
                }
                return sum / x;
            }
            // 3. x^{y} （底数普通，指数递增）
            else if (!aIsDice && !aIsInc && bIsInc && !bIsDice)
            {
                long sum = 0;
                for (int j = 1; j <= 9; j++)
                {
                    sum += (long)System.Math.Pow(x, (int)(y + j));
                }
                return sum / 9;
            }
            // 4. {x}^y （底数递增，指数普通）
            else if (aIsInc && !aIsDice && !bIsDice && !bIsInc)
            {
                long sum = 0;
                for (int i = 1; i <= 9; i++)
                {
                    sum += (long)System.Math.Pow((int)(x + i), (int)y);
                }
                return sum / 9;
            }
            // 5. {x}^~y~ （底数递增，指数骰子）
            else if (aIsInc && !aIsDice && bIsDice && !bIsInc)
            {
                long sum = 0;
                for (int i = 1; i <= 9; i++)
                {
                    long innerSum = 0;
                    for (long j = 1; j <= y; j++)
                    {
                        innerSum += (long)System.Math.Pow((int)(x + i), (int)j);
                    }
                    sum += innerSum;
                }
                return sum / (9 * y);
            }
            // 6. ~x~^{y} （底数骰子，指数递增）
            else if (aIsDice && !aIsInc && bIsInc && !bIsDice)
            {
                long sum = 0;
                for (long i = 1; i <= x; i++)
                {
                    long innerSum = 0;
                    for (int j = 1; j <= 9; j++)
                    {
                        innerSum += (long)System.Math.Pow((int)i, (int)(y + j));
                    }
                    sum += innerSum;
                }
                return sum / (9 * x);
            }
            // 7. ~x~^~y~ （底数骰子，指数骰子）
            else if (aIsDice && !aIsInc && bIsDice && !bIsInc)
            {
                long sum = 0;
                for (long i = 1; i <= x; i++)
                {
                    long innerSum = 0;
                    for (long j = 1; j <= y; j++)
                    {
                        innerSum += (long)System.Math.Pow((int)i, (int)j);
                    }
                    sum += innerSum;
                }
                return sum / (x * y);
            }
            // 8. {x}^{y} （底数递增，指数递增）
            else if (aIsInc && !aIsDice && bIsInc && !bIsDice)
            {
                long sum = 0;
                for (int i = 1; i <= 9; i++)
                {
                    sum += (long)System.Math.Pow((int)(x + i), (int)(y + i));
                }
                return sum / 9;
            }
            else
            {
                Debug.LogWarning($"未匹配的指数型组合：A(骰子={aIsDice},递增={aIsInc})，B(骰子={bIsDice},递增={bIsInc})");
                return 0;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"CalculatePowerExpectation 计算出错：{e.Message}");
            return 0;
        }
    }
    private long RoundPrice(long price)  // 返回值改为 long
    {
        // 输入验证
        if (price <= 0)
            return 0;

        // 第一步：四舍五入到最近的5的倍数
        long roundedTo5 = (price + 2) / 5 * 5;

        // 第二步：最多保留3个有效数字
        if (roundedTo5 == 0)
            return 0;

        try
        {
            int digitCount = GetDigitCount(roundedTo5);

            if (digitCount <= 3)
            {
                return roundedTo5;
            }
            else
            {
                // 保留3位有效数字
                long scale = (long)System.Math.Pow(10, digitCount - 3);
                long roundedTo3Sig = (roundedTo5 + scale / 2) / scale * scale;
                // 确保最终结果仍是5的倍数
                return (roundedTo3Sig + 2) / 5 * 5;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"RoundPrice 计算出错：{e.Message}，输入价格：{price}，返回0");
            return 0;
        }
    }
    /// <summary>
    /// 计算数字的位数（位数 = 有效数字个数）
    /// </summary>
    private int GetDigitCount(long num)
    {
        if (num == 0)
            return 1;

        num = System.Math.Abs(num);
        int count = 0;

        while (num > 0)
        {
            num /= 10;
            count++;
        }

        return count;
    }

    /// <summary>
    /// 辅助方法：计算BigInteger的有效数字位数
    /// 解决Mathf.Log10无法处理超大数的问题
    /// </summary>
    private int GetBigIntegerDigitCount(BigInteger num)
    {
        if (num == BigInteger.Zero)
            return 1;
        num = BigInteger.Abs(num);
        int count = 0;
        while (num > BigInteger.Zero)
        {
            num /= 10;
            count++;
        }
        return count;
    }
}