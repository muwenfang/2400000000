using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
