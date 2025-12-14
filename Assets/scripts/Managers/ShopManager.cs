using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 商店系统。读取database中的商品信息，读取玩家信息，处理购买逻辑
/// </summary>
public class ShopManager : MonoBehaviour
{
    public GameObject Shop;

    public CardManager cardManager;
    public BlessingData blessingData;
    public FormulaCardData formulaCardData;
    public NumberCardData numberCardData;
}
