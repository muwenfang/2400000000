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
    public List<NumberCardData> currentNumberCards = new List<NumberCardData>();
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
        DrawRandomCards(currentFormulaCard.RequiredCount);  
        // 抽卡逻辑
        // [to do]

    }

    void DrawRandomCards(int Count)
    {
        List<GameObject> tempCards = new List<GameObject>();///建立一个临时卡组（用于抽牌的卡组），防止破坏原本的Deck

        for (int i = 0; i < Count; i++)///循环Count次
        {
            if (tempCards.Count == 0) break;///如果用于抽牌的卡组空了，就推出循环
            int randomIndex = Random.Range(0, tempCards.Count);///生成一个0-（count-1）的随机数作为列表索引抽牌
            GameObject selectedCard = tempCards[randomIndex];///把用于抽牌的卡组中对应randomIndex索引的牌加入到selectedcard里，作为选中的牌
            tempCards.RemoveAt(randomIndex);
            GameObject HandCardUI = Instantiate(CardUIPrefab, CardContent);
            SpriteRenderer HandcardSprite = selectedCard.GetComponent<SpriteRenderer>();
            if (HandcardSprite != null)///cardSpriteRenderer获取到图像之后执行，if语句是“防御性程序”，确保获取到了卡牌实体的图像
            {
                Image HandcardUIImage = HandCardUI.GetComponent<Image>();///获取卡牌预制体的Image图像（cardUI预制体要加image组件）
                if (HandcardUIImage != null)///预制体获取到图像后执行
                {
                    HandcardUIImage.sprite = HandcardSprite.sprite;///把“实际卡牌的Sprite”赋值给“UI的Image”，让UI显示卡牌图片（直到这一步前，cardUI都是不包含图像的）
                }
            }
        }
    }



}
