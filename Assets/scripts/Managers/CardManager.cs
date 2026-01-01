using System.Collections;
using System.Collections.Generic;
using System.Numerics;
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
        Debug.Log("初始化玩家起始卡组");
        // 初始化玩家的起始卡组
        // [to do]
    }

    public void DrawCardsForTurn()
    {
        DrawNumberCards(currentFormulaCard.RequiredCount);
        DrawFormulaCards();

    }

    void DrawNumberCards(int count)
    {
        // 用临时池，避免一回合内重复抽
        List<NumberCardData> tempDeck = new List<NumberCardData>(numberCardDeck);
        Debug.Log("正在抽取数字卡牌");
        for (int i = 0; i < count; i++)
        {
            if (tempDeck.Count == 0) break;

            int randomIndex = Random.Range(0, tempDeck.Count);
            NumberCardData selectedData = tempDeck[randomIndex];
            tempDeck.RemoveAt(randomIndex);// 从临时池中移除已抽取的卡牌

            // 创建卡牌实例
            NumberCardInstance instance = new NumberCardInstance(selectedData);

            // 抽中,掷骰
            instance.OnDrawn();

            // 放入手牌
            currentNumberCards.Add(instance);

            //  UI
            CreateCardUI(instance);
        }
        Debug.Log("数字卡牌抽取完成");
    }
    public void DrawFormulaCards()
    {   
        Debug.Log("正在抽取填空卡牌");
        // 抽取填空卡牌
        //[to do]
        Debug.Log("填空卡牌抽取完成");
    }
    void CreateCardUI(NumberCardInstance instance)
    {
        // 实例化卡牌 UI 预制件
        //[to do]
    }
    public BigInteger CalculateResult(List<NumberCardData> numberCards)
    {   
        Debug.Log("正在计算填空卡牌结果");
        
        BigInteger result = 0;
        //计算逻辑
        //【to do】
        Debug.Log($"点数为{result}");
        return result;
    }
}
