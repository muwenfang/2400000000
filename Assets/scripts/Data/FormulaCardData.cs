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
               
            },
            new FormulaCardData
            {
                Pattern = "(#+#)*#*#",
                Name = "(#+#)*#*#",
                RequiredCount = 4,

            },
            //[to do]


        });
    }
}
