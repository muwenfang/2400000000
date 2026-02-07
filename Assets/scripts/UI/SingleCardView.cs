using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public class SingleNumberView : MonoBehaviour, NumberCardLayoutView
{
    public Text valueText;
    public Text priceText;
    public bool IsInShop = false;

    public void Bind(NumberCardData data)
    {
        valueText.text = data.partA.value.ToString();
    }
    //更新价格显示
    public void UpdatePrice(NumberCardInstance numberCardInstance)
    {
        if (IsInShop)
        {
            int price  = numberCardInstance.GetOutPutValue();
            priceText.text = price.ToString();
        }
    }


}
