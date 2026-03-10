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
    public BigInteger GetOutPutValue()
    {
        BigInteger a = new BigInteger(currentA);
        BigInteger b = new BigInteger(currentB);

        switch (cardData.logicalType)
        {
            case NumberCardData.LogicalType.Addition:
                return a + b;
            case NumberCardData.LogicalType.Multiplication:
                return a * b;
            case NumberCardData.LogicalType.Power:
                return BigInteger.Pow(a, (int)b);
            default:
                return a;
        }
    }

    /// <summary>
    /// 按文档规则计算卡牌价格：期望计算 → 倍率修正 → 舍入
    /// 返回BigInteger，入参不变
    /// </summary>
    public BigInteger GetNumberCardPrice(NumberCardData card)
{
    if (card == null)
    {
        Debug.LogError("卡牌数据为空，无法计算价格！");
        return BigInteger.Zero;
    }

    NumberComponent a = card.partA;
    NumberComponent b = card.partB;
    NumberCardData.LogicalType logic = card.logicalType;

    if (a == null)
    {
        Debug.LogError("卡牌PartA不能为空！");
        return BigInteger.Zero;
    }

    // 第一步：计算数学期望
    BigInteger expectation = CalculateExpectation(a, b, logic);

    // 第二步：倍率修正（修复：不用 decimal，改用整数运算避免类型错误）
    float rate = 1.0f;
    if (a.isIncremental || (b != null && b.isIncremental))
        rate *= 1.0f;
    if (a.isDice || (b != null && b.isDice))
        rate *= 1.0f;
    if (logic == NumberCardData.LogicalType.Power)
        rate *= 1.0f;

    // 用整数运算替代 decimal，兼容 BigInteger
    BigInteger priceAfterRate = expectation * (int)(rate * 100) / 100;

    // 第三步：舍入
    BigInteger finalPrice = RoundPrice(priceAfterRate);

    Debug.Log($"价格计算过程：期望={expectation} → 倍率修正后={priceAfterRate} → 最终价格={finalPrice}");
    return finalPrice;
}

    /// <summary>
    /// 第一步：根据卡牌类型计算数学期望
    /// 返回BigInteger，完全保留原计算逻辑
    /// </summary>
    private BigInteger CalculateExpectation(NumberComponent a, NumberComponent b, NumberCardData.LogicalType logic)
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
                return BigInteger.Zero;
            }
            return CalculatePowerExpectation(a, b);
        }
    }

    /// <summary>
    /// 计算非指数型卡牌期望（加法/乘法/单数字）
    /// 返回BigInteger，先乘后除避免浮点均值误差
    /// </summary>
    private BigInteger CalculateNonPowerExpectation(NumberComponent a, NumberComponent b, NumberCardData.LogicalType logic)
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
                return BigInteger.Zero;
            }

            BigInteger expA = GetComponentExpectation(a);
            BigInteger expB = GetComponentExpectation(b);

            // 特殊处理：{x}*{y}型（按文档公式计算前9次均值）
            if (logic == NumberCardData.LogicalType.Multiplication && a.isIncremental && b.isIncremental)
            {
                BigInteger sum = BigInteger.Zero;
                for (int i = 1; i <= 9; i++)
                {
                    BigInteger valA = new BigInteger(a.value) + i;
                    BigInteger valB = new BigInteger(b.value) + i;
                    sum += valA * valB;
                }
                // 先乘后除：sum/9，BigInteger自动取整，保留原逻辑
                return sum / 9;
            }

            // 普通加法/乘法，直接BigInteger运算
            return logic == NumberCardData.LogicalType.Addition ? expA + expB : expA * expB;
        }
    }

    /// <summary>
    /// 获取单个组件（PartA/PartB）的非指数型期望
    /// 返回BigInteger，原公式数值直接转换
    /// </summary>
    private BigInteger GetComponentExpectation(NumberComponent comp)
    {
        if (comp.isDice)
        {
            // 骰子：(x+1)/2.0 → 转为BigInteger：(x+1)/2（先加后除，与原浮点逻辑一致）
            return (new BigInteger(comp.diceSides) + 1) / 2;
        }
        else if (comp.isIncremental)
        {
            // 递增：y+5 → 直接BigInteger运算
            return new BigInteger(comp.value) + 5;
        }
        else
        {
            // 普通数字：直接转BigInteger
            return new BigInteger(comp.value);
        }
    }

    /// <summary>
    /// 计算指数型卡牌期望（幂运算），对应文档8种指数组合
    /// 返回BigInteger，先累加再取均值（先乘后除），完全保留8种组合逻辑
    /// </summary>
    private BigInteger CalculatePowerExpectation(NumberComponent a, NumberComponent b)
    {
        bool aIsDice = a.isDice;
        bool aIsInc = a.isIncremental;
        bool bIsDice = b.isDice;
        bool bIsInc = b.isIncremental;

        // 骰子取面数，其他取数值，基础值转BigInteger
        BigInteger x = a.isDice ? new BigInteger(a.diceSides) : new BigInteger(a.value);
        BigInteger y = b.isDice ? new BigInteger(b.diceSides) : new BigInteger(b.value);

        // 1. x^~y~ （底数普通，指数骰子）
        if (!aIsDice && !aIsInc && bIsDice && !bIsInc)
        {
            BigInteger sum = BigInteger.Zero;
            for (int j = 1; j <= (int)y; j++)
            {
                sum += BigInteger.Pow((int)x, j);
            }
            return sum / (int)y;
        }
        // 2. ~x~^y （底数骰子，指数普通）
        else if (aIsDice && !aIsInc && !bIsDice && !bIsInc)
        {
            BigInteger sum = BigInteger.Zero;
            for (int i = 1; i <= (int)x; i++)
            {
                sum += BigInteger.Pow(i, (int)y);
            }
            return sum / (int)x;
        }
        // 3. x^{y} （底数普通，指数递增）
        else if (!aIsDice && !aIsInc && bIsInc && !bIsDice)
        {
            BigInteger sum = BigInteger.Zero;
            for (int j = (int)y + 1; j <= (int)y + 9; j++)
            {
                sum += BigInteger.Pow((int)x, j);
            }
            return sum / 9;
        }
        // 4. {x}^y （底数递增，指数普通）
        else if (aIsInc && !aIsDice && !bIsDice && !bIsInc)
        {
            BigInteger sum = BigInteger.Zero;
            for (int i = (int)x + 1; i <= (int)x + 9; i++)
            {
                sum += BigInteger.Pow(i, (int)y);
            }
            return sum / 9;
        }
        // 5. {x}^~y~ （底数递增，指数骰子）
        else if (aIsInc && !aIsDice && bIsDice && !bIsInc)
        {
            BigInteger sum = BigInteger.Zero;
            for (int i = (int)x + 1; i <= (int)x + 9; i++)
            {
                BigInteger innerSum = BigInteger.Zero;
                for (int j = 1; j <= (int)y; j++)
                {
                    innerSum += BigInteger.Pow(i, j);
                }
                sum += innerSum;
            }
            return sum / (9 * (int)y);
        }
        // 6. ~x~^{y} （底数骰子，指数递增）
        else if (aIsDice && !aIsInc && bIsInc && !bIsDice)
        {
            BigInteger sum = BigInteger.Zero;
            for (int i = 1; i <= (int)x; i++)
            {
                BigInteger innerSum = BigInteger.Zero;
                for (int j = (int)y + 1; j <= (int)y + 9; j++)
                {
                    innerSum += BigInteger.Pow(i, j);
                }
                sum += innerSum;
            }
            return sum / (9 * (int)x);
        }
        // 7. ~x~^~y~ （底数骰子，指数骰子）
        else if (aIsDice && !aIsInc && bIsDice && !bIsInc)
        {
            BigInteger sum = BigInteger.Zero;
            for (int i = 1; i <= (int)x; i++)
            {
                BigInteger innerSum = BigInteger.Zero;
                for (int j = 1; j <= (int)y; j++)
                {
                    innerSum += BigInteger.Pow(i, j);
                }
                sum += innerSum;
            }
            return sum / ((int)x * (int)y);
        }
        // 8. {x}^{y} （底数递增，指数递增）
        else if (aIsInc && !aIsDice && bIsInc && !bIsDice)
        {
            BigInteger sum = BigInteger.Zero;
            for (int i = 1; i <= 9; i++)
            {
                BigInteger baseVal = x + i;
                BigInteger expVal = y + i;
                sum += BigInteger.Pow((int)baseVal, (int)expVal);
            }
            return sum / 9;
        }
        // 未匹配的指数组合
        else
        {
            Debug.LogWarning($"未匹配的指数型组合：A(骰子={aIsDice},递增={aIsInc})，B(骰子={bIsDice},递增={bIsInc})");
            return BigInteger.Zero;
        }
    }

    /// <summary>
    /// 第三步：舍入处理（最近5的倍数 + 最多3个有效数字）
    /// </summary>
    private BigInteger RoundPrice(BigInteger price)
    {
        // 输入验证：价格≤0返回0
        if (price <= BigInteger.Zero)
            return BigInteger.Zero;

        // 第一步：四舍五入到最近的5的倍数
        BigInteger roundedTo5 = (price + 2) / 5 * 5; // 等价BigInteger四舍五入：(num + 5/2 -1)/5 *5

        // 第二步：最多保留3个有效数字
        if (roundedTo5 == BigInteger.Zero)
            return BigInteger.Zero;

        try
        {
            // 计算BigInteger的有效数字位数
            int digitCount = GetBigIntegerDigitCount(roundedTo5);

            if (digitCount <= 3)
            {
                // 不足3位有效数字，直接返回5的倍数结果
                return roundedTo5;
            }
            else
            {
                // 超过3位有效数字，保留3位并修正为5的倍数
                BigInteger scale = BigInteger.Pow(10, digitCount - 3);
                // 保留3位有效数字：四舍五入
                BigInteger roundedTo3Sig = (roundedTo5 + scale / 2) / scale * scale;
                // 确保最终结果仍是5的倍数
                return (roundedTo3Sig + 2) / 5 * 5;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"RoundPrice 计算出错：{e.Message}，输入价格：{price}，返回0");
            return BigInteger.Zero;
        }
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