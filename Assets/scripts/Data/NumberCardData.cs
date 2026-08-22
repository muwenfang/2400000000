using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using System;
using static NumberCardFactory;

/// <summary>
/// 数字卡数据
/// </summary>
[System.Serializable]
public class NumberComponent
{
    public bool isDice = false;//是否为骰子
    public bool isIncremental = false;//是否为递增
    public bool isGolden = false;//是否为黄金数字
    public int value;//数值（基础值保留int，无溢出风险）
    public int diceSides;//骰子面数（基础值保留int）
}

[CreateAssetMenu(fileName = "MyNumberCards", menuName = "CardData/NumberCardData", order = 1)]
public class NumberCardData : ScriptableObject//不挂载在 GameObject 上
{                                           //直接作为数据容器使用即可GetOutPutValue()
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
    // 标记该卡牌本回合是否已经投过骰子/递增过
    public bool isPrepared = false;
    // 当前骰子面数（赌场专员升级后可能高于库中原始面数；独立于 cardData，避免影响共享库数据）
    public int currentDiceSidesA = 0;
    public int currentDiceSidesB = 0;
    public NumberCardInstance(NumberCardData cardData)//构造函数，初始化当前数值
    {
        this.cardData = cardData;

        // partA 是数字卡的核心组件，缺失说明该卡牌资产在 Inspector 中未配置完整
        if (cardData == null || cardData.partA == null)
        {
            Debug.LogError($"[NumberCardInstance] 卡牌数据不完整：cardData={(cardData == null ? "null" : cardData.name)}，partA 为空，currentA 置 0。请检查该卡牌资产。");
            currentA = 0;
        }
        else
        {
            currentA = cardData.partA.value;
        }

        if (cardData != null && cardData.partB != null)
        {
            currentB = cardData.partB.value;
        }

        // 初始化当前骰子面数（与库中数据一致，供赌场专员升级使用）
        if (cardData != null && cardData.partA != null)
            currentDiceSidesA = cardData.partA.isDice ? cardData.partA.diceSides : 0;
        if (cardData != null && cardData.partB != null)
            currentDiceSidesB = cardData.partB.isDice ? cardData.partB.diceSides : 0;
    }

    /// <summary>
    /// 只处理骰子，不处理递增
    /// </summary>
    public void OnDrawn()
    {
        //对于骰子卡，应该使用 diceSides 而不是 value
        if (cardData.partA.isDice)
        {
            currentA = currentDiceSidesA > 0 ? currentDiceSidesA : cardData.partA.diceSides;  // 骰子：用面数初始化
        }


        if (cardData.partB != null)
        {
            if (cardData.partB.isDice)
            {
                currentB = currentDiceSidesB > 0 ? currentDiceSidesB : cardData.partB.diceSides;  // 骰子：用面数初始化
            }

        }

        // 标记为未投掷/未递增状态
        isPrepared = false;
    }

    /// <summary>
    /// 判断该数字组件是否应作为递增数字处理：
    /// 绿色数字（isIncremental）恒递增；拥有金融专家祝福时，黄金数字也视为递增。
    /// 仅作用于本实例的 currentA/currentB，不会修改库中的 NumberCardData。
    /// </summary>
    private bool ShouldIncrement(NumberComponent component)
    {
        if (component == null) return false;
        if (component.isIncremental) return true;
        return component.isGolden &&
               BlessingManager.Instance != null &&
               BlessingManager.Instance.hasFinancialExpert;
    }

    /// <summary>
    /// 结算前调用：投骰子并更新递增值
    /// </summary>
    public void PrepareForCalculation()
    {
        // 投掷骰子（如果是骰子）
        if (cardData.partA.isDice)
        {
            int sides = currentDiceSidesA > 0 ? currentDiceSidesA : cardData.partA.diceSides;
            // 唯心主义：同级骰子结果一致
            if (BlessingManager.Instance.hasIdealism)
            {
                // 储存每骰过的等级的骰子
                if (!BlessingManager.Instance.idealismDiceResults.ContainsKey(sides))
                {
                    int result = DiceHelper.RollDice(sides);
                    BlessingManager.Instance.idealismDiceResults[sides] = result;
                }

                // 所有同面骰子都用全局结果
                currentA = BlessingManager.Instance.idealismDiceResults[sides];
            }
            else
            {
                // 正常掷骰子
                currentA = DiceHelper.RollDice(sides);
            }

            BlessingManager.Instance.CheckGambleToWin(currentA);// 赌为赢判定

            // 大成功（按掷骰前的面数判定）
            if (currentA == sides && BlessingManager.Instance.bigSuccessCount > 0)
            {
                int rank = BlessingManager.Instance.GetDiceRank(sides);
                BlessingManager.Instance.totalMultiplierBonus += rank;
                Debug.Log($"【大成功】{sides}面骰掷出最大值！获得 {rank} 永久倍率");
            }

            // 赌场专员：骰子判定为其最大值后自动升一级，直至20
            if (currentA == sides && BlessingManager.Instance.hasCasinoCommissioner && currentDiceSidesA < 20)
            {
                currentDiceSidesA = UpgradeDiceLevel(currentDiceSidesA);
                Debug.Log($"【赌场专员】{sides}面骰掷出最大值！骰子升级为 {currentDiceSidesA} 面");
            }
        }

        if (cardData.partB != null && cardData.partB.isDice)
        {
            int sides = currentDiceSidesB > 0 ? currentDiceSidesB : cardData.partB.diceSides;

            //唯心主义
            if (BlessingManager.Instance.hasIdealism)
            {
                if (!BlessingManager.Instance.idealismDiceResults.ContainsKey(sides))
                {
                    int result = DiceHelper.RollDice(sides);
                    BlessingManager.Instance.idealismDiceResults[sides] = result;
                }

                currentB = BlessingManager.Instance.idealismDiceResults[sides];
            }
            else
            {
                currentB = DiceHelper.RollDice(sides);
            }

            BlessingManager.Instance.CheckGambleToWin(currentB);// 赌为赢判定

            // 大成功（按掷骰前的面数判定）
            if (currentB == sides && BlessingManager.Instance.bigSuccessCount > 0)
            {
                int rank = BlessingManager.Instance.GetDiceRank(sides);
                BlessingManager.Instance.totalMultiplierBonus += rank;
                Debug.Log($"【大成功】{sides}面骰掷出最大值！获得 {rank} 永久倍率");
            }

            // 赌场专员：骰子判定为其最大值后自动升一级，直至20
            if (currentB == sides && BlessingManager.Instance.hasCasinoCommissioner && currentDiceSidesB < 20)
            {
                currentDiceSidesB = UpgradeDiceLevel(currentDiceSidesB);
                Debug.Log($"【赌场专员】{sides}面骰掷出最大值！骰子升级为 {currentDiceSidesB} 面");
            }
        }

        // 更新递增值（+1）
        if (cardData.partA.isIncremental)
        {
            //祝福节节高效果：大于等于9的绿色数字递增后将变为绿色的{1}；触发此效果时，你的倍率永久+20
            if (BlessingManager.Instance.hasRisingUp == 1 && currentA >= 9)
            {
                currentA = 1;
                BlessingManager.Instance.totalMultiplierBonus += 50;
            }
            else
            {
                currentA++;
                //祝福势如破竹效果：你的绿色数字的正增量将转化为永久倍率
                if (BlessingManager.Instance.hasUnstoppable == 1)
                    BlessingManager.Instance.totalMultiplierBonus += 1;
            }

        }

        if (cardData.partB != null && cardData.partB.isIncremental)
        {
            if (BlessingManager.Instance.hasRisingUp == 1 && currentB >= 9)
            {
                currentB = 1;
                BlessingManager.Instance.totalMultiplierBonus += 50;
            }
            else
            {
                currentB++;
                //祝福势如破竹效果：你的绿色数字的正增量将转化为永久倍率
                if (BlessingManager.Instance.hasUnstoppable == 1)
                    BlessingManager.Instance.totalMultiplierBonus += 1;
            }

        }

        // 标记为已结算
        isPrepared = true;
    }    


    /// <summary>
    /// 骰子面数升级规则：4→6→8→12→20，20不再升
    /// </summary>
    private static int UpgradeDiceLevel(int currentSides)
    {
        switch (currentSides)
        {
            case 4: return 6;
            case 6: return 8;
            case 8: return 12;
            case 12: return 20;
            case 20: return 20;
            default: return currentSides;
        }
    }

    // 祝福:能量扩散
    public void EnergySpread()
    {
        // 更新递增值（+1）
        if (cardData.partA.isIncremental)
        {
            if (currentA >= 9 && BlessingManager.Instance.hasRisingUp == 1)
            {
                currentA = 1;
                BlessingManager.Instance.totalMultiplierBonus += 50;
            }
            else
            {
                currentA++;
                //祝福势如破竹效果：你的绿色数字的正增量将转化为永久倍率
                if (BlessingManager.Instance.hasUnstoppable == 1)
                    BlessingManager.Instance.totalMultiplierBonus += 1;
            }
            //Debug.Log($"递增卡更新：{cardData.cardName} Part A: {currentA - 1} → {currentA}");

        }
        if (cardData.partB != null && cardData.partB.isIncremental)
        {
            if (currentB >= 9 && BlessingManager.Instance.hasRisingUp == 1)
            {
             currentB = 1;
             BlessingManager.Instance.totalMultiplierBonus += 50;
            }
            else
            {
              currentB++;
              //祝福势如破竹效果：你的绿色数字的正增量将转化为永久倍率
              if (BlessingManager.Instance.hasUnstoppable == 1)
                  BlessingManager.Instance.totalMultiplierBonus += 1;
            }
                //Debug.Log($"递增卡更新：{cardData.cardName} Part B: {currentB - 1} → {currentB}");

        }
    }
    
    /// <summary>
    /// 获得当前卡牌的输出值（根据逻辑类型计算）
    /// </summary>
    public BigInteger GetOutPutValue()
    {
        int a = currentA;
        int b = currentB;

        switch (cardData.logicalType)
        {
            case NumberCardData.LogicalType.Addition:
                return a + b;
            case NumberCardData.LogicalType.Multiplication:
                return (BigInteger)a * b;
            case NumberCardData.LogicalType.Power:
                return BigInteger.Pow((BigInteger)a, b);
            default:
                return a;
        }
    }

    #region 价格计算逻辑
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

            // 第二步：倍率修正
            double X = Math.Abs((double)expectation); // 用期望作为 X，取绝对值避免负数对数异常
            double rate = 1.0;

            // 1. 所有卡牌 × (log2(X) - 1) 倍
            if (X >= 2) // 防止 log2(1)=0 变成负数
            {
                rate *= (Math.Log(X, 2) - 1.0);
            }

            // 2. 所有含绿色数字(递增) 或 含骰子 的卡牌 再 × 1.5 倍
            bool hasGreenOrDice = a.isIncremental || (b != null && b.isIncremental) || a.isDice || (b != null && b.isDice);
            if (hasGreenOrDice)
            {
                rate *= 1.5;
            }

            // 3. 所有指数型(Power)卡牌 再 × 2.0 倍
            if (logic == NumberCardData.LogicalType.Power)
            {
                rate *= 2.0;
            }

            // 4. 所有 x >= 100 的数字卡 再乘以 (log5(x)-1) 的 1.5 次方
            if (X >= 100)
            {
                double value = Math.Log(X, 5) - 1.0;
                rate *= Math.Pow(value, 3.0 / 2.0); // ^(3/2)
            }

            // 5. 所有 x >= 10000 的数字卡 再乘以 (log10(x)-2) 的 1.5 次方
            if (X >= 10000)
            {
                double value = Math.Log10(X) - 2.0;
                rate *= Math.Pow(value, 3.0 / 2.0); // ^(3/2)
            }

            // 计算倍率后价格
            long priceAfterRate = (long)(X * rate);

            // 黄金数字加成：y = 所有黄金数之和，价格增加 100*y*(y+1)
            int goldenSum = 0;
            if (a.isGolden) goldenSum += a.value;
            if (b != null && b.isGolden) goldenSum += b.value;
            if (goldenSum > 0)
            {
                priceAfterRate += 100L * goldenSum * (goldenSum + 1);
            }

            // 第三步：舍入
            long finalPrice = RoundPrice(priceAfterRate);

            if (finalPrice < 0) 
            {
                finalPrice = Math.Abs(finalPrice); // 负数取绝对值
            }
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
                    long valA = Math.Abs(a.value) + i;
                    long valB = Math.Abs(b.value) + i;
                    sum += valA * valB;
                }
                return sum / 9;
            }

            // 普通加法/乘法，直接BigInteger运算，结果取绝对值
            return Math.Abs(logic == NumberCardData.LogicalType.Addition ? expA + expB : expA * expB);
        }
    }

    /// <summary>
    /// 获取单个组件（PartA/PartB）的期望值（用long）
    /// </summary>
    private long GetComponentExpectation(NumberComponent comp)
    {
        if (comp.isDice)
        {
            return Math.Abs((comp.diceSides + 1) / 2);
        }
        else if (comp.isIncremental)
        {
            // 递增数字期望值 = value + 5（平均递增值），取绝对值
            return Math.Abs((long)(comp.value + 5));
        }
        else
        {
            // 普通数字，取绝对值
            return Math.Abs((long)comp.value);
        }
    }

    /// <summary>
    /// 计算指数型卡牌价格期望（8种组合）
    /// 返回long，用整数运算计算
    /// </summary>
    private long CalculatePowerExpectation(NumberComponent a, NumberComponent b)
    {
        bool aIsDice = a.isDice;
        bool aIsInc = a.isIncremental;
        bool bIsDice = b.isDice;
        bool bIsInc = b.isIncremental;

        // 骰子取面数，其他取数值
        long x = Math.Abs(a.isDice ? a.diceSides : a.value);
        long y = Math.Abs(b.isDice ? b.diceSides : b.value);

        long sum = 0;
        try
        {
            // 1. x^~y~ （底数普通，指数骰子）
            if (!aIsDice && !aIsInc && bIsDice && !bIsInc)
            {
                for (long j = 1; j <= y; j++)
                {
                    sum += (long)System.Math.Pow(x, (int)j);
                }
                sum = sum / y;
            }
            // 2. ~x~^y （底数骰子，指数普通）
            else if (aIsDice && !aIsInc && !bIsDice && !bIsInc)
            {
                for (long i = 1; i <= x; i++)
                {
                    sum += (long)System.Math.Pow((int)i, (int)y);
                }
                sum = sum / x;
            }
            // 3. x^{y} （底数普通，指数递增）
            else if (!aIsDice && !aIsInc && bIsInc && !bIsDice)
            {
                for (int j = 1; j <= 9; j++)
                {
                    sum += (long)System.Math.Pow(x, (int)(y + j));
                }
                sum = sum / 9;
            }
            // 4. {x}^y （底数递增，指数普通）
            else if (aIsInc && !aIsDice && !bIsDice && !bIsInc)
            {
                for (int i = 1; i <= 9; i++)
                {
                    sum += (long)System.Math.Pow((int)(x + i), (int)y);
                }
                sum = sum / 9;
            }
            // 5. {x}^~y~ （底数递增，指数骰子）
            else if (aIsInc && !aIsDice && bIsDice && !bIsInc)
            {
                for (int i = 1; i <= 9; i++)
                {
                    long innerSum = 0;
                    for (long j = 1; j <= y; j++)
                    {
                        innerSum += (long)System.Math.Pow((int)(x + i), (int)j);
                    }
                    sum += innerSum;
                }
                sum = sum / (9 * y);
            }
            // 6. ~x~^{y} （底数骰子，指数递增）
            else if (aIsDice && !aIsInc && bIsInc && !bIsDice)
            {
                for (long i = 1; i <= x; i++)
                {
                    long innerSum = 0;
                    for (int j = 1; j <= 9; j++)
                    {
                        innerSum += (long)System.Math.Pow((int)i, (int)(y + j));
                    }
                    sum += innerSum;
                }
                sum = sum / (9 * x);
            }
            // 7. ~x~^~y~ （底数骰子，指数骰子）
            else if (aIsDice && !aIsInc && bIsDice && !bIsInc)
            {
                for (long i = 1; i <= x; i++)
                {
                    long innerSum = 0;
                    for (long j = 1; j <= y; j++)
                    {
                        innerSum += (long)System.Math.Pow((int)i, (int)j);
                    }
                    sum += innerSum;
                }
                sum = sum / (x * y);
            }
            // 8. {x}^{y} （底数递增，指数递增）
            else if (aIsInc && !aIsDice && bIsInc && !bIsDice)
            {
                for (int i = 1; i <= 9; i++)
                {
                    sum += (long)System.Math.Pow((int)(x + i), (int)(y + i));
                }
                sum = sum / 9;
            }
            else
            {
                Debug.LogWarning($"未匹配的指数型组合：A(骰子={aIsDice},递增={aIsInc})，B(骰子={bIsDice},递增={bIsInc})");
                sum = 0;
            }
            return Math.Abs(sum); // 返回绝对值，避免负数
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
        if (price == 0)
            return 0;
        if (price < 0)
            price = Math.Abs(price); // 负数取绝对值后正常舍入

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
    #endregion 

}