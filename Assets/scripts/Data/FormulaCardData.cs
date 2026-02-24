using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UnityEngine;

/// <summary>
/// 填空卡数据
/// </summary>

[CreateAssetMenu(fileName = "NewFormulaCard", menuName = "Cards/FormulaCard")] // 添加这一行
public class FormulaCardData : ScriptableObject
{
    public string Pattern; // 公式，如 "#*#+#"
    public string Name;    // 名称
    public int RequiredCount; // 所需填空数量
    public int CardPrice;     // 价格
    public int FormulaCardId; // 编号

    private readonly List<FormulaCardData> _formulas = new();


    //填空卡数据
    public void DefaultFormulas()
    {
        _formulas.AddRange(new[]
        {
            new FormulaCardData
            {
                Pattern = "#*#*#",
                Name = "#*#*#",
                RequiredCount = 3,
                FormulaCardId = 0,
                CardPrice = 1000,
            },
            new FormulaCardData
            {
                Pattern = "(#+#)*#*#",
                Name = "(#+#)*#*#",
                RequiredCount = 4,
                FormulaCardId = 1,
                CardPrice = 2000,
            },
            //[to do]
             new FormulaCardData
            {
                Pattern = "#*#*#*#",
                Name = "#*#*#*#",
                RequiredCount = 4,
                FormulaCardId = 2,
                CardPrice = 10000,
            },
             new FormulaCardData
            {
                Pattern = "(#+#)*(#+#)",
                Name = "(#+#)*(#+#)",
                RequiredCount = 4,
                FormulaCardId = 3,
                CardPrice = 400,
            },
            new FormulaCardData
            {
                Pattern = "(#+#+#)*#",
                Name = "(#+#+#)*#",
                RequiredCount = 4,
                FormulaCardId = 4,
                CardPrice = 100,
            },
             new FormulaCardData
            {
                Pattern = "(#+#+#+#)*#",
                Name = "(#+#+#+#)*#",
                RequiredCount = 5,
                FormulaCardId = 5,
                CardPrice = 400,
            },
             new FormulaCardData
            {
                Pattern = "(#+#+#)*(#+#)",
                Name = "(#+#+#)*(#+#)",
                RequiredCount = 5,
                FormulaCardId = 6,
                CardPrice = 600,
            },
             new FormulaCardData
            {
                Pattern = "(#+#+#)*#*#",
                Name = "(#+#+#)*#*#",
                RequiredCount = 5,
                FormulaCardId = 7,
                CardPrice = 3000,
            },
             new FormulaCardData
            {
                Pattern = "(#+#)*(#+#)*#",
                Name = "(#+#)*(#+#)*#",
                RequiredCount = 5,
                FormulaCardId = 8,
                CardPrice = 4000,
            },
             new FormulaCardData
            {
                Pattern = "(#+#)*#*#*#",
                Name = "(#+#)*#*#*#",
                RequiredCount = 5,
                FormulaCardId = 9,
                CardPrice = 20000,
            },
             new FormulaCardData
            {
                Pattern = "#*#*#*#*#",
                Name = "#*#*#*#*#",
                RequiredCount = 5,
                FormulaCardId = 10,
                CardPrice = 100000
            },
             new FormulaCardData
            {
                Pattern = "(#+#+#+#+#)*#",
                Name = "(#+#+#+#+#)*#",
                RequiredCount = 6,
                FormulaCardId = 11,
                CardPrice = 500,
            },
             new FormulaCardData
            {
                Pattern = "(#+#+#+#)*(#+#)",
                Name = "(#+#+#+#)*(#+#)",
                RequiredCount = 6,
                FormulaCardId = 12,
                CardPrice = 800,
            },
             new FormulaCardData
            {
                Pattern = "(#+#+#)*(#+#+#)",
                Name = "(#+#+#)*(#+#+#)",
                RequiredCount = 6,
                FormulaCardId = 13,
                CardPrice = 900,
            },
             new FormulaCardData
            {
                Pattern = "(#+#+#+#)*#*#",
                Name = "(#+#+#+#)*#*#",
                RequiredCount = 6,
                FormulaCardId = 14,
                CardPrice = 4000,
            },
            new FormulaCardData
            {
                Pattern = "(#+#+#)*(#+#)*#",
                Name = "(#+#+#)*(#+#)*#",
                RequiredCount = 6,
                FormulaCardId = 15,
                CardPrice = 6000,
            },
            new FormulaCardData
            {
                Pattern = "(#+#)*(#+#)*(#+#)",
                Name = "(#+#)*(#+#)*(#+#)",
                RequiredCount = 6,
                FormulaCardId = 16,
                CardPrice = 8000,
            },
             new FormulaCardData
            {
                Pattern = "(#+#+#)*#*#*#",
                Name = "(#+#+#)*#*#*#",
                RequiredCount = 6,
                FormulaCardId = 17,
                CardPrice = 30000,
            },
             new FormulaCardData
            {
                Pattern = "(#+#)*(#+#)*#*#",
                Name = "(#+#)*(#+#)*#*#",
                RequiredCount = 6,
                FormulaCardId = 18,
                CardPrice = 40000,
            },
             new FormulaCardData
            {
                Pattern = "(#+#)*#*#*#*#",
                Name = "(#+#)*#*#*#*#",
                RequiredCount = 6,
                FormulaCardId = 19,
                CardPrice = 200000,
            },
             new FormulaCardData
            {
                Pattern = "#*#*#*#*#*#",
                Name = "#*#*#*#*#*#",
                RequiredCount = 6,
                FormulaCardId = 20,
                CardPrice = 1000000,
            },
        
        });
    }
}

//计算
public static class FormulaCalculator
{
    static int index;
    static string expr;
    static bool enableDebugLog = true;  // 是否启用调试日志

    /// <summary>
    /// 计算公式结果
    /// </summary>
    public static BigInteger Calculate(FormulaCardData formula, List<NumberCardInstance> numbers)
    {
        if (enableDebugLog)
        {
            Debug.Log("========== FormulaCalculator 开始计算 ==========");
        }

        // 验证数量
        if (numbers == null || numbers.Count == 0)
        {
            Debug.LogError("卡牌列表为空！");
            throw new Exception("卡牌列表为空");
        }

        if (numbers.Count != formula.RequiredCount)
        {
            Debug.LogError($"数量不匹配！需要 {formula.RequiredCount}，实际 {numbers.Count}");
            throw new Exception("数字数量不匹配公式需求");
        }

        // 1. 提取卡牌值
        var values = numbers.ConvertAll(n => n.GetOutPutValue());

        if (enableDebugLog)
        {
            Debug.Log($"卡牌详情：");
            for (int i = 0; i < numbers.Count; i++)
            {
                var card = numbers[i];
                Debug.Log($"  [{i}] {card.cardData.cardName} → 值: {values[i]}");
            }
            Debug.Log($"值列表：[{string.Join(", ", values)}]");
        }

        // 2. 验证 Pattern 中 # 的数量
        int hashCount = CountHashes(formula.Pattern);
        if (hashCount != values.Count)
        {
            Debug.LogError($"错误：Pattern 中 # 数量 ({hashCount}) 与卡牌数量 ({values.Count}) 不匹配！");
            Debug.LogError($"Pattern: {formula.Pattern}");
            throw new Exception("Pattern 格式错误");
        }

        // 3. 构建表达式
        expr = BuildExpression(formula.Pattern, values);

        if (enableDebugLog)
        {
            Debug.Log($"构建的表达式：{expr}");
        }

        // 4. 验证表达式
        if (!ValidateExpression(expr))
        {
            Debug.LogError($"错误：表达式格式无效：{expr}");
            throw new Exception("表达式格式无效");
        }

        // 5. 解析并计算
        index = 0;
        BigInteger result;

        try
        {
            result = ParseExpression();

            if (enableDebugLog)
            {
                Debug.Log($"计算成功！结果：{result}");
                Debug.Log("================================================");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"解析表达式时出错：{e.Message}");
            Debug.LogError($"表达式：{expr}");
            Debug.LogError($"当前位置：{index}");
            throw;
        }

        return result;
    }

    /// <summary>
    /// 统计 Pattern 中 # 的数量
    /// </summary>
    static int CountHashes(string pattern)
    {
        int count = 0;
        foreach (char c in pattern)
        {
            if (c == '#')
                count++;
        }
        return count;
    }

    /// <summary>
    /// 构建表达式字符串
    /// </summary>
    static string BuildExpression(string pattern, List<int> values)
    {
        if (values == null || values.Count == 0)
        {
            Debug.LogError("BuildExpression: 值列表为空");
            return "";
        }

        int valueIndex = 0;
        var result = "";

        foreach (char c in pattern)
        {
            if (c == '#')
            {
                if (valueIndex >= values.Count)
                {
                    Debug.LogError($"BuildExpression: 值索引 {valueIndex} 超出范围（共 {values.Count} 个值）");
                    break;
                }
                result += values[valueIndex++];
            }
            else
            {
                result += c;
            }
        }

        if (valueIndex != values.Count)
        {
            Debug.LogWarning($"BuildExpression: 使用了 {valueIndex}/{values.Count} 个值");
        }

        return result;
    }

    /// <summary>
    /// 验证表达式格式
    /// </summary>
    static bool ValidateExpression(string expression)
    {
        if (string.IsNullOrEmpty(expression))
        {
            Debug.LogError("表达式为空");
            return false;
        }

        // 检查括号匹配
        int bracketCount = 0;
        foreach (char c in expression)
        {
            if (c == '(') bracketCount++;
            if (c == ')') bracketCount--;
            if (bracketCount < 0)
            {
                Debug.LogError("右括号多于左括号");
                return false;
            }
        }

        if (bracketCount != 0)
        {
            Debug.LogError($"括号不匹配：差 {bracketCount} 个");
            return false;
        }

        // 检查是否包含数字
        bool hasDigit = false;
        foreach (char c in expression)
        {
            if (char.IsDigit(c))
            {
                hasDigit = true;
                break;
            }
        }

        if (!hasDigit)
        {
            Debug.LogError("表达式中没有数字");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 解析表达式（加法层）
    /// </summary>
    static BigInteger ParseExpression()
    {
        if (enableDebugLog)
        {
            Debug.Log($"ParseExpression: 位置 {index}，字符 '{GetCurrentChar()}'");
        }

        BigInteger value = ParseTerm();

        while (index < expr.Length && expr[index] == '+')
        {
            index++; // skip '+'

            if (enableDebugLog)
            {
                Debug.Log($"  加法: {value} + ...");
            }

            BigInteger right = ParseTerm();
            value += right;

            if (enableDebugLog)
            {
                Debug.Log($"  结果: {value}");
            }
        }

        return value;
    }

    /// <summary>
    /// 解析项（乘法层）
    /// </summary>
    static BigInteger ParseTerm()
    {
        if (enableDebugLog)
        {
            Debug.Log($"ParseTerm: 位置 {index}，字符 '{GetCurrentChar()}'");
        }

        BigInteger value = ParseFactor();

        while (index < expr.Length && expr[index] == '*')
        {
            index++; // skip '*'

            if (enableDebugLog)
            {
                Debug.Log($"  乘法: {value} * ...");
            }

            BigInteger right = ParseFactor();
            value *= right;

            if (enableDebugLog)
            {
                Debug.Log($"  结果: {value}");
            }
        }

        return value;
    }

    /// <summary>
    /// 解析因子（数字或括号表达式）
    /// </summary>
    static BigInteger ParseFactor()
    {
        if (index >= expr.Length)
        {
            Debug.LogError($"ParseFactor: 索引 {index} 超出表达式长度 {expr.Length}");
            throw new Exception("表达式意外结束");
        }

        if (enableDebugLog)
        {
            Debug.Log($"ParseFactor: 位置 {index}，字符 '{expr[index]}'");
        }

        // 处理括号
        if (expr[index] == '(')
        {
            index++; 
            BigInteger value = ParseExpression();

            if (index >= expr.Length || expr[index] != ')')
            {
                Debug.LogError($"缺少右括号，位置：{index}");
                throw new Exception("缺少右括号");
            }

            index++; 
            return value;
        }

        // 解析数字
        if (!char.IsDigit(expr[index]))
        {
            Debug.LogError($" 期望数字或'('，但得到 '{expr[index]}'，位置：{index}");
            throw new Exception($"无效字符：{expr[index]}");
        }

        BigInteger number = 0;
        string numberStr = "";

        while (index < expr.Length && char.IsDigit(expr[index]))
        {
            numberStr += expr[index];
            number = number * 10 + (expr[index] - '0');
            index++;
        }

        if (enableDebugLog)
        {
            Debug.Log($"  解析数字: {numberStr} = {number}");
        }

        return number;
    }

    /// <summary>
    /// 获取当前字符（用于调试）
    /// </summary>
    static string GetCurrentChar()
    {
        if (index >= expr.Length)
            return "[结束]";
        return expr[index].ToString();
    }

    /// <summary>
    /// 设置是否启用调试日志
    /// </summary>
    public static void SetDebugMode(bool enabled)
    {
        enableDebugLog = enabled;
        Debug.Log($"FormulaCalculator 调试模式：{(enabled ? "开启" : "关闭")}");
    }
}

