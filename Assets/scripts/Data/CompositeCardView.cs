using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class CompositeNumberView : MonoBehaviour, NumberCardLayoutView
{
    public Text aText;
    public Text bText;
    public Text priceText;
    public bool IsInShop = false;

    public void Bind(NumberCardData data)
    {
        aText.text = data.partA.value.ToString();
        bText.text = data.partB.value.ToString();
    }
    //更新价格显示
    public void UpdatePrice(NumberCardInstance numberCardInstance)
    {
        if (IsInShop)
        {
            int price = numberCardInstance.GetOutPutValue();
            priceText.text = price.ToString();
        }
    }

}
