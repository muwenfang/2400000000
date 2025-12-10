using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 卡牌事件
/// </summary>
public class CardManager : MonoBehaviour
{
    [Header("卡牌库")]
    public List<NumberCardData> numberCardDeck = new List<NumberCardData>();//数字卡牌库
    public List<FormulaCardData> formulaCardDeck = new List<FormulaCardData>();//填空卡牌库
    public Transform handCardParent;///建立手牌的父对象,作为后续给卡牌排版的容器

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
