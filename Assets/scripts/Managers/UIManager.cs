using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("UI组件")]
    public Text pointsText;//显示当前点数
    public Text roundText;//显示当前回合
    public Transform handArea;//手牌区域
    public Transform formulaArea;//填空卡区域

    public GameObject numberCardPrefab;
    public GameObject formulaCardPrefab;

    public void UpdatePointsDisplay(BigInteger points)
    {
        pointsText.text = $"点数: {points}";
    }

    public void UpdateRoundDisplay(int round)
    {
        roundText.text = $"回合: {round}";
    }

    public void UpdateFormulaDisplay(FormulaCardData formula) 
    {
        // 更新填空卡显示
        //[to do]
    }

    public void UpdateHandDisplay(NumberCardData numberCards)
    {
        // 更新手牌显示
        //[to do]
    }
}
