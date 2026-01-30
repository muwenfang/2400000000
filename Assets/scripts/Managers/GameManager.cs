using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

/// <summary>
/// 游戏状态
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // 游戏状态
    public enum GameState { MainMenu, PlayerTurn, Calculation, Shop, GameOver, GameLose }
    public GameState currentState;

    // 玩家数据
    [Header("玩家数据")]
    public BigInteger currentPoints = 0; // 使用BigInteger处理大数
    public BigInteger targetPoints = 2400000000;
    public int currentRound = 1;
    public int roundsPerStage;
    public BigInteger stageRequirement; // 每阶段要求点数

    // 卡牌管理器引用
    public CardManager cardManager;
    public BlessingManager blessingManager;
    public ShopManager shopManager;


    //阶段点数数据
    public List<BigInteger> stagePointRequirements = new List<BigInteger>()
    {
       24, 240,2400,24000,2400000,24000000,240000000,2400000000
    };

    //阶段回合数
    public List<int> stageRounds = new List<int>()
    {
        3,8,15,24,35,46,56,66,75
    };

    void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        EnterMainMenu();
        Debug.Log("进入游戏"); 
    }

    void EnterMainMenu()
    {   
        currentState = GameState.MainMenu;
        // 显示主菜单UI
        UIManager.Instance.ShowStartMenu();
    }
    public void OnStartButtonClicked()
    {
        if (GameManager.Instance.currentState != GameState.MainMenu)
            return;

        GameManager.Instance.InitializeGame();
    }

    public void InitializeGame()
    {   
        Debug.Log("初始化游戏");
        currentPoints = 0;
        currentRound = 1;
        // 初始化玩家卡组
        cardManager.InitializeStarterDeck();
        // 隐藏开始界面
        UIManager.Instance.HideStartMenu();
        // 开始第一回合
        StartPlayerTurn();
    }



    public void StartPlayerTurn()
    {
        Debug.Log("开始回合");
        currentState = GameState.PlayerTurn;
        // 抽填空计算卡和对应数量的数字卡
        cardManager.DrawCardsForTurn();
    }

    public void CalculatePoints(CardManager formula, List<NumberCardData> numberCards)
    {
        if (currentState != GameState.PlayerTurn)
            return;

        currentState = GameState.Calculation;
        // 计算填空卡结果
        BigInteger result = formula.CalculateResult(numberCards);
        // 计算祝福加成与倍率
        //[to do]



        // 添加到总点数
        AddPoints(result);

        // 检查点数，开启商店
        EndTurn();
    }

    public void AddPoints(BigInteger points)
    {
        currentPoints += points;
        // 更新UI显示
        UIManager.Instance.UpdatePointsDisplay(currentPoints);
    }

    public void EndTurn()
    {
        foreach (int stageRound in stageRounds)
        {
            if (currentRound == stageRound)
            {
                CheckStageRequirement();
                return;
            }
        }
        currentState = GameState.Shop;
        shopManager.OpenShop();

    }

    void CheckStageRequirement()
    {
        if (currentPoints < stageRequirement)
        {
            // 游戏失败
            WinGame(false);
        }
        if (currentRound == 75 && currentPoints >= targetPoints)
        {   // 达到最终目标，游戏胜利
            WinGame(true);
        }
    }
    public void OnShopConfirm()
    {   //确认离开商店，开始下一回合
        if (currentState != GameState.Shop)
            return;

        // 1. 关闭商店UI
        shopManager.CloseShop();
        // 2. 增加回合数
        currentRound++;
        // 3. 开始新回合
        StartPlayerTurn();
    }
    void WinGame(bool isWin)
    {
        if (isWin)
        {
            currentState = GameState.GameOver;
            // 显示游戏结束界面
            //[to do]
        }
        else { 
            currentState = GameState.GameLose;
            // 显示游戏失败界面
            //[to do]
        }


    }

}
