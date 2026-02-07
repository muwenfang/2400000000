using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class CompositeNumberView : MonoBehaviour, NumberCardLayoutView
{
    public Text aText;
    public Text bText;
    public Text operatorText; // 新增：用来显示 +、× 或 ^

    public void Bind(NumberCardData data)
    {
        aText.text = data.partA.value.ToString();
        bText.text = data.partB.value.ToString();

        // 根据逻辑类型自动切换中间的符号
        if (operatorText != null)
        {
            operatorText.text = data.logicalType switch
            {
                NumberCardData.LogicalType.Addition => "+",
                NumberCardData.LogicalType.Multiplication => "×",
                NumberCardData.LogicalType.Power => "^",
                _ => ""
            };
        }
    }

}
