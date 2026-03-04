using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FormulaView : MonoBehaviour
{
    public Text formulaText;
    public Text priceText;
    public bool isInShop = false;
    public void Bind(FormulaCardData data)
    {
        formulaText.text = data.Name;
    }
    //更新价格显示
    public void UpdatePrice(FormulaCardData formulaCardData)
    {
        if (isInShop)
        {
            priceText.text = $"${formulaCardData.CardPrice}";
        }
        else
        {
            priceText.text = "error";
        }
    }
}
