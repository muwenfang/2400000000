using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 卡牌事件（抽卡，展示，储存玩家卡牌库）
/// </summary>
public class CardManager : MonoBehaviour
{
    [Header("卡牌库")]
    public List<NumberCardData> numberCardDeck = new List<NumberCardData>();//数字卡牌库
    public List<FormulaCardData> formulaCardDeck = new List<FormulaCardData>();//填空卡牌库
    public Transform handCardParent;///建立手牌的父对象,作为后续给卡牌排版的容器

    [Header("当前手牌")]
    public List<NumberCardInstance> currentNumberCards = new();
    public FormulaCardData currentFormulaCard;

    public GameObject CardUIPrefab;
    public Transform CardContent;
    public void InitializeStarterDeck()
    {
        // 初始化玩家的起始卡组
        // [to do]
    }

    public void DrawCardsForTurn()
    {
        DrawNumberCards(currentFormulaCard.RequiredCount);  

    }

    void DrawNumberCards(int count)
    {
        // 用临时池，避免一回合内重复抽
        List<NumberCardData> tempDeck = new List<NumberCardData>(numberCardDeck);

        for (int i = 0; i < count; i++)
        {
            if (tempDeck.Count == 0) break;

            int randomIndex = Random.Range(0, tempDeck.Count);
            NumberCardData selectedData = tempDeck[randomIndex];
            tempDeck.RemoveAt(randomIndex);

            // 创建卡牌实例
            NumberCardInstance instance = new NumberCardInstance(selectedData);

            // 抽中,掷骰
            instance.OnDrawn();

            // 放入手牌
            currentNumberCards.Add(instance);

            //  UI
            CreateCardUI(instance);
        }
    }
    void CreateCardUI(NumberCardInstance instance)
    {
        // 实例化卡牌 UI 预制件
        //[to do]
    }




}
