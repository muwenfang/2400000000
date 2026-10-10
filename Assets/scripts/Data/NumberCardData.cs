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
    // 流星：本卡下一次结算时，第一个骰子直接判定为最大值（由 CardManager 结算前标记，一次性）
    public bool forceMaxDiceOnce = false;
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
    /// 判断该数字组件是否具备"递增"特性：
    /// 绿色数字（isIncremental）恒递增；拥有金融专家祝福时，黄金数字也能递增。
    /// 只读标志位，不修改任何数据。
    /// </summary>
    public static bool CanIncrement(NumberComponent component)
    {
        if (component == null) return false;
        if (component.isIncremental) return true;
        return component.isGolden &&
               BlessingManager.Instance != null &&
               BlessingManager.Instance.hasFinancialExpert;
    }

    /// <summary>PartA 是否为黄金数字</summary>
    public bool IsGoldenPartA => cardData != null && cardData.partA != null && cardData.partA.isGolden;

    /// <summary>PartB 是否为黄金数字</summary>
    public bool IsGoldenPartB => cardData != null && cardData.partB != null && cardData.partB.isGolden;

    /// <summary>该卡是否含有黄金数字</summary>
    public bool HasGolden => IsGoldenPartA || IsGoldenPartB;

    /// <summary>该卡是否含有骰子（流星判定用）</summary>
    public bool HasDice => cardData != null &&
        ((cardData.partA != null && cardData.partA.isDice) ||
         (cardData.partB != null && cardData.partB.isDice));

    /// <summary>读取指定黄金组件的当前值（0=PartA，1=PartB）</summary>
    public int GetGoldenValue(int partIndex)
    {
        return partIndex == 0 ? currentA : currentB;
    }

    /// <summary>
    /// 提升本卡黄金数字（0=PartA，1=PartB）。
    /// 只修改本实例的 currentA/currentB —— 该数值未被序列化，与库资产完全解耦，
    /// 因此商店里同款卡仍显示资产原始数值（福星、金融专家等一切"黄金数增加"都走这里）。
    /// </summary>
    public void IncreaseGoldenValue(int partIndex, int amount = 1)
    {
        if (partIndex == 0 && IsGoldenPartA) currentA += amount;
        else if (partIndex == 1 && IsGoldenPartB) currentB += amount;
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

            // 流星：本回合该骰子直接判定为最大值（一次性，用后即清除）
            if (forceMaxDiceOnce)
            {
                forceMaxDiceOnce = false;
                currentA = sides;
                // 唯心主义：同级骰子结果保持一致
                if (BlessingManager.Instance.hasIdealism)
                    BlessingManager.Instance.idealismDiceResults[sides] = sides;
            }
            // 唯心主义：同级骰子结果一致
            else if (BlessingManager.Instance.hasIdealism)
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

            // 流星：本回合该骰子直接判定为最大值（一次性，用后即清除）
            if (forceMaxDiceOnce)
            {
                forceMaxDiceOnce = false;
                currentB = sides;
                if (BlessingManager.Instance.hasIdealism)
                    BlessingManager.Instance.idealismDiceResults[sides] = sides;
            }
            //唯心主义
            else if (BlessingManager.Instance.hasIdealism)
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

        // 更新递增值（+1）：绿色数字恒递增；拥有金融专家祝福时，黄金数字也能递增
        if (CanIncrement(cardData.partA))
        {
            bool isGreenA = cardData.partA.isIncremental;

            //祝福节节高效果：大于等于9的绿色数字递增后将变为绿色的{1}；触发此效果时，你的倍率永久+50
            if (isGreenA && BlessingManager.Instance.hasRisingUp == 1 && currentA >= 9)
            {
                currentA = 1;
                BlessingManager.Instance.totalMultiplierBonus += 50;
            }
            else
            {
                currentA++;
                //祝福势如破竹效果：你的绿色数字的正增量将转化为永久倍率
                if (isGreenA && BlessingManager.Instance.hasUnstoppable == 1)
                    BlessingManager.Instance.totalMultiplierBonus += 1;
            }

        }

        if (CanIncrement(cardData.partB))
        {
            bool isGreenB = cardData.partB.isIncremental;

            if (isGreenB && BlessingManager.Instance.hasRisingUp == 1 && currentB >= 9)
            {
                currentB = 1;
                BlessingManager.Instance.totalMultiplierBonus += 50;
            }
            else
            {
                currentB++;
                //祝福势如破竹效果：你的绿色数字的正增量将转化为永久倍率
                if (isGreenB && BlessingManager.Instance.hasUnstoppable == 1)
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
            // 第一步：计算数学期望（浮点：骰子~x~=(x+1)/2.0，绿色{y}=y+5，均值不做整数截断）
            // 注意：期望按【带符号】求和（负数不提前下沉为绝对值），再对整体结果取绝对值得到 x。
            // 例：2^{0} → x=(2+4+…+2^9)/9；(-2)^{0} → x=|-2+4-8+…+(-2)^9|/9，两者不同。
            double expectation = CalculateExpectation(a, b, logic);
            if (double.IsNaN(expectation) || double.IsInfinity(expectation))
            {
                Debug.LogWarning($"价格计算：期望值非法（{expectation}），按 0 处理。卡牌：{card.cardName}");
                return 0;
            }
            double X = Math.Abs(expectation);
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
            long priceAfterRate = (long)Math.Round(X * rate, MidpointRounding.AwayFromZero);

            // 第三步：黄金数修正：y = 所有黄金数之和，价格增加 100*y*(y+1)
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
    /// 返回double，保留小数精度（骰子期望 (x+1)/2.0，均值不做整数截断）
    /// </summary>
    private double CalculateExpectation(NumberComponent a, NumberComponent b, NumberCardData.LogicalType logic)
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
    /// 将~x~视为(x+1)/2.0，将{y}视为(y+5)，直接代入计算；{x}*{y}型用前九次均值公式 Σ(x+i)(y+i)/9
    /// </summary>
    private double CalculateNonPowerExpectation(NumberComponent a, NumberComponent b, NumberCardData.LogicalType logic)
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

            double expA = GetComponentExpectation(a);
            double expB = GetComponentExpectation(b);

            // 特殊处理：{x}*{y}型（前九次参与运算的均值）
            if (logic == NumberCardData.LogicalType.Multiplication && a.isIncremental && b.isIncremental)
            {
                double sum = 0;
                for (int i = 1; i <= 9; i++)
                {
                    sum += (a.value + i) * (b.value + i);
                }
                return sum / 9.0;
            }

            // 普通加法/乘法，直接代入期望值
            return logic == NumberCardData.LogicalType.Addition ? expA + expB : expA * expB;
        }
    }

    /// <summary>
    /// 获取单个组件（PartA/PartB）的期望值：骰子~x~=(x+1)/2.0，绿色{y}=y+5，普通数字=x
    /// </summary>
    private double GetComponentExpectation(NumberComponent comp)
    {
        if (comp.isDice)
        {
            return (comp.diceSides + 1) / 2.0;
        }
        else if (comp.isIncremental)
        {
            // 绿色（递增）数字期望值 = value + 5
            return comp.value + 5;
        }
        else
        {
            // 普通数字
            return comp.value;
        }
    }

    /// <summary>
    /// 带符号幂运算。本工程的指数恒为整数（数值 / 骰子面数 / 递增量都是整数），
    /// 对「负底数 + 整数指数」用连乘实现，保证 (-2)^3 = -8 这类带符号结果在任何运行时都稳定，
    /// 不依赖 Math.Pow 对负底数的实现差异。
    /// </summary>
    private static double SignedPow(double baseValue, double exponent)
    {
        double rounded = Math.Round(exponent);
        if (Math.Abs(exponent - rounded) < 1e-9)
        {
            int e = (int)rounded;
            if (e == 0) return 1.0;
            double result = 1.0;
            int absE = Math.Abs(e);
            for (int i = 0; i < absE; i++) result *= baseValue;
            return e < 0 ? 1.0 / result : result;
        }
        return Math.Pow(baseValue, exponent);
    }

    /// <summary>
    /// 计算指数型卡牌价格期望（8种组合）
    /// 返回double：指数可能很大（如20^20≈1.05e26），必须用浮点避免整数溢出。
    /// 【带符号求和】此处保留底数/指数的正负号，返回求和后的**有符号**结果，
    /// 不逐项取绝对值——绝对值由调用方对「第一步结果」整体取（见 GetNumberCardPrice）。
    /// </summary>
    private double CalculatePowerExpectation(NumberComponent a, NumberComponent b)
    {
        bool aIsDice = a.isDice;
        bool aIsInc = a.isIncremental;
        bool bIsDice = b.isDice;
        bool bIsInc = b.isIncremental;

        // 骰子取面数，其他取数值（保留符号：负底数/负指数参与带符号求和）
        double x = a.isDice ? a.diceSides : a.value;
        double y = b.isDice ? b.diceSides : b.value;

        double sum;
        try
        {
            // 1. x^~y~ ：Σ_{j=1}^{y} x^j / y
            if (!aIsDice && !aIsInc && bIsDice && !bIsInc)
            {
                sum = 0;
                for (int j = 1; j <= (int)y; j++)
                {
                    sum += SignedPow(x, j);
                }
                return sum / y;
            }
            // 2. ~x~^y ：Σ_{i=1}^{x} i^y / x
            else if (aIsDice && !aIsInc && !bIsDice && !bIsInc)
            {
                sum = 0;
                for (int i = 1; i <= (int)x; i++)
                {
                    sum += SignedPow(i, y);
                }
                return sum / x;
            }
            // 3. x^{y} ：Σ_{i=y+1}^{y+9} x^i / 9
            else if (!aIsDice && !aIsInc && bIsInc && !bIsDice)
            {
                sum = 0;
                for (int j = 1; j <= 9; j++)
                {
                    sum += SignedPow(x, y + j);
                }
                return sum / 9.0;
            }
            // 4. {x}^y ：Σ_{i=x+1}^{x+9} i^y / 9
            else if (aIsInc && !aIsDice && !bIsDice && !bIsInc)
            {
                sum = 0;
                for (int i = 1; i <= 9; i++)
                {
                    sum += SignedPow(x + i, y);
                }
                return sum / 9.0;
            }
            // 5. {x}^~y~ ：Σ_{i=x+1}^{x+9}(Σ_{j=1}^{y} i^j) / (9y)
            else if (aIsInc && !aIsDice && bIsDice && !bIsInc)
            {
                sum = 0;
                for (int i = 1; i <= 9; i++)
                {
                    double innerSum = 0;
                    for (int j = 1; j <= (int)y; j++)
                    {
                        innerSum += SignedPow(x + i, j);
                    }
                    sum += innerSum;
                }
                return sum / (9.0 * y);
            }
            // 6. ~x~^{y} ：Σ_{i=1}^{x}(Σ_{j=y+1}^{y+9} i^j) / (9x)
            else if (aIsDice && !aIsInc && bIsInc && !bIsDice)
            {
                sum = 0;
                for (int i = 1; i <= (int)x; i++)
                {
                    double innerSum = 0;
                    for (int j = 1; j <= 9; j++)
                    {
                        innerSum += SignedPow(i, y + j);
                    }
                    sum += innerSum;
                }
                return sum / (9.0 * x);
            }
            // 7. ~x~^~y~ ：Σ_{i=1}^{x}(Σ_{j=1}^{y} i^j) / (xy)
            else if (aIsDice && !aIsInc && bIsDice && !bIsInc)
            {
                sum = 0;
                for (int i = 1; i <= (int)x; i++)
                {
                    for (int j = 1; j <= (int)y; j++)
                    {
                        sum += SignedPow(i, j);
                    }
                }
                return sum / (x * y);
            }
            // 8. {x}^{y} ：Σ_{i=1}^{9}(x+i)^{y+i} / 9
            else if (aIsInc && !aIsDice && bIsInc && !bIsDice)
            {
                sum = 0;
                for (int i = 1; i <= 9; i++)
                {
                    sum += SignedPow(x + i, y + i);
                }
                return sum / 9.0;
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