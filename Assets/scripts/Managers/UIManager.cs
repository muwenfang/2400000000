using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI 面板引用")]
    public GameObject startMenuPanel; // 在 Inspector 中拖入主菜单面板
    public GameObject gameUIPanel; // 游戏内UI面板
    public GameObject gameOverPanel; // 游戏结束面板
    public GameObject shopPanel; // 商店面板
    public GameObject confirmPanel;// 确认面板

    [Header("UI组件")]
    public Text pointsText;//显示当前点数
    public Text roundText;//显示当前回合
    public Transform handArea;//手牌区域
    public Transform formulaArea;//填空卡区域

    public GameObject numberCardPrefab;
    public GameObject formulaCardPrefab;

    void Awake()
    {
        Instance = this;
    }
    public void ShowStartMenu() 
    {
        if (startMenuPanel != null)
        {
            startMenuPanel.SetActive(true);
            Debug.Log("显示主菜单");
        }
    }
    public void HideStartMenu() 
    {
        if (startMenuPanel != null)
        {
            startMenuPanel.SetActive(false);
        }
    }
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
