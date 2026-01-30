using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UnityEngine;

/// <summary>
/// 填空卡数据
/// </summary>
public class FormulaCardData
{   
    public string Pattern { get; set; } // 公式卡，如 "#*#+#" 或 "#*#*#*#"
    public string Name { get; set; }// 公式卡名称
    public int RequiredCount { get; set; } // 公式卡所需填空数量
    public int CardPrice { get; set; } // 公式卡价格
    public int FormulaCardId { get; set; } //公式卡编号
    
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

    public static BigInteger Calculate(
        FormulaCardData formula,
        List<NumberCardInstance> numbers)
    {
        if (numbers.Count != formula.RequiredCount)
            throw new Exception("数字数量不匹配公式需求");

        // 1. 取数值
        var values = numbers.ConvertAll(n => n.GetOutPutValue());

        // 2. 构建表达式
        expr = BuildExpression(formula.Pattern, values);
        index = 0;

        // 3. 解析并计算
        return ParseExpression();
    }

    // =========================
    // 构建字符串
    // =========================
    static string BuildExpression(string pattern, List<int> values)
    {
        int i = 0;
        var result = "";

        foreach (char c in pattern)
        {
            if (c == '#')
                result += values[i++];
            else
                result += c;
        }
        return result;
    }

    // =========================
    // 解析器
    // Grammar:
    // expression = term { + term }
    // term       = factor { * factor }
    // factor     = number | (expression)
    // =========================

    static BigInteger ParseExpression()
    {
        BigInteger value = ParseTerm();

        while (index < expr.Length && expr[index] == '+')
        {
            index++; // skip '+'
            value += ParseTerm();
        }

        return value;
    }

    static BigInteger ParseTerm()
    {
        BigInteger value = ParseFactor();

        while (index < expr.Length && expr[index] == '*')
        {
            index++; // skip '*'
            value *= ParseFactor();
        }

        return value;
    }

    static BigInteger ParseFactor()
    {
        if (expr[index] == '(')
        {
            index++; // skip '('
            BigInteger value = ParseExpression();
            index++; // skip ')'
            return value;
        }

        // 解析数字
        BigInteger number = 0;
        while (index < expr.Length && char.IsDigit(expr[index]))
        {
            number = number * 10 + (expr[index] - '0');
            index++;
        }

        return number;
    }
}

