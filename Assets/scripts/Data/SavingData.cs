using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

[System.Serializable]
public class SavingData
{
    [Header("=============== 普通模式数据 ===============")]
    /// 普通模式历史最高点数
    public string TotalPointsN = "0";

    /// 普通模式历史最高倍率
    public float RateN = 0f;

    /// 普通模式历史最高单张数字卡点数
    public string NumbercardPointN = "0";

    /// 普通模式历史最高结算点数（某一轮的计算结果）
    public string CalculationPointN = "0";

    [Header("=============== 内卷模式数据 ===============")]
    /// 内卷模式历史最高点数
    public string TotalPointsI = "0";

    /// 内卷模式历史最高倍率
    public float RateI = 0f;

    /// 内卷模式历史最高单张数字卡点数
    public string NumbercardPointI = "0";

    /// 内卷模式历史最高结算点数
    public string CalculationPointI = "0";

    [Header("=============== 全局统计数据 ===============")]
    /// 累计通关次数（两种模式总计）
    public int accomplishTimes = 0;

    /// 全局最高点数（两种模式中最高的）
    public string MaxPoint = "0";


    /// 将 BigInteger 转换为字符串存储
    public static string BigIntegerToString(BigInteger value)
    {
        return value.ToString();
    }

    /// <summary>
    /// 将字符串转换为 BigInteger
    /// </summary>
    public static BigInteger StringToBigInteger(string value)
    {
        if (string.IsNullOrEmpty(value) || value == "0")
            return BigInteger.Zero;

        if (BigInteger.TryParse(value, out BigInteger result))
            return result;

        Debug.LogWarning($"无法解析字符串为 BigInteger: {value}，返回0");
        return BigInteger.Zero;
    }
    /// <summary>
    /// 比较两个 string 类型的 BigInteger（用于决定是否更新数据）
    /// </summary>
    public static bool IsBigIntegerGreater(BigInteger newValue, string oldValueStr)
    {
        BigInteger oldValue = StringToBigInteger(oldValueStr);
        return newValue > oldValue;
    }

    /// <summary>
    /// 比较两个 string 类型的 BigInteger
    /// </summary>
    public static bool IsBigIntegerGreaterString(string newValueStr, string oldValueStr)
    {
        BigInteger newValue = StringToBigInteger(newValueStr);
        BigInteger oldValue = StringToBigInteger(oldValueStr);
        return newValue > oldValue;
    }
}
