using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 卡牌事件
/// </summary>
public class CardManager : MonoBehaviour
{
    [Header("卡牌库")]
    public List<NumberCardData> numberCardDeck = new List<NumberCardData>();
    public List<FormulaCardData> formulaCardDeck = new List<FormulaCardData>();

    [Header("当前手牌")]
    public List<NumberCardData> currentNumberCards = new List<NumberCardData>();
    public FormulaCardData currentFormulaCard;

    public void InitializeStarterDeck()
    {
        // 初始化玩家的起始卡组
        // [to do]
    }

    public void DrawCardsForTurn()
    {
        // 抽卡逻辑
        // [to do]
    }

    //洗牌
    //【to do】

}
