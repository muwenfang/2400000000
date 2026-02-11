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
        //EnterMainMenu();
        //Debug.Log("进入游戏"); 
        ChangeState(GameState.MainMenu);
    }

    //void EnterMainMenu()
    //{
    //    currentState = GameState.MainMenu;

    //    // 添加空引用检查，防止 UIManager 还没准备好
    //    if (UIManager.Instance != null)
    //    {
    //        UIManager.Instance.ShowStartMenu();
    //    }
    //    else
    //    {
    //        Debug.LogError("UIManager 实例未找到！请检查场景中是否挂载了 UIManager 脚本。");
    //    }
    //}

    public void InitializeGame()
    {   
        Debug.Log("初始化游戏");
        currentPoints = 0;
        currentRound = 1;
        // 确保调用了 ChangeState，这样上面的 UI 逻辑才会跑起来
        ChangeState(GameState.PlayerTurn);

        // 执行抽卡逻辑
        cardManager.InitializeStarterDeck();

        //// 3. 关键：通知 UI 刷新视觉显示
        //UIManager.Instance.RefreshGameUI();

        Debug.Log("UI 刷新请求已发送");
        // 开始第一回合
        StartPlayerTurn();

        //  关键：通知 UI 刷新视觉显示
        UIManager.Instance.RefreshGameUI();
    }

    public void StartPlayerTurn()
    {
        Debug.Log("开始回合");
        ChangeState(GameState.PlayerTurn);
        Debug.Assert(currentState == GameState.PlayerTurn);
        //UIManager.Instance.ShowGameUI();
        // 抽填空计算卡和对应数量的数字卡
        cardManager.DrawCardsForTurn();
    }

    public void CalculatePoints(CardManager formula)
    {
        if (currentState != GameState.PlayerTurn)
            return;

        currentState = GameState.Calculation;
        // 计算填空卡结果
        BigInteger result = formula.CalculateResult();
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

    }

    public void EndTurn()
    {
        // 更新UI显示
        UIManager.Instance.UpdatePointsDisplay(currentPoints);
        // 检查阶段要求
        foreach (int stageRound in stageRounds)
        {
            if (currentRound == stageRound)
            {
                CheckStageRequirement();
            }
        }
        currentState = GameState.Shop;
        shopManager.OpenShop();
        ChangeState(GameState.Shop);
    }

    void CheckStageRequirement()
    {
        if (currentPoints < stageRequirement)
        {
            // 游戏失败
            WinGame(false);
        }
        if (currentRound == 75 && currentPoints == targetPoints)
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
    public void ReturnStartMenu()
    {
        //加载主菜单界面
        ChangeState(GameState.MainMenu);

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
    public void ChangeState(GameState newState)
    {
        // 1. 离开当前状态时的清理 (你已有的代码)
        switch (currentState)
        {
            case GameState.PlayerTurn:
                break;
            case GameState.Shop:
                shopManager.CloseShop();
                break;
        }

        // 更新当前状态
        currentState = newState;

        // 2. 进入新状态的 UI 切换逻辑（需要添加的部分）
        switch (currentState)
        {
            case GameState.PlayerTurn:
                // 调用 UIManager 显示游戏内面板，隐藏主菜单
                UIManager.Instance.ShowPanel(UIManager.Instance.gameUIPanel);
                Debug.Log("进入玩家回合界面");
                break;

            case GameState.MainMenu:
                UIManager.Instance.ShowPanel(UIManager.Instance.startMenuPanel);
                Debug.Log("进入主菜单界面");
                break;

            case GameState.Shop:
                UIManager.Instance.ShowPanel(UIManager.Instance.shopPanel);
                Debug.Log("进入商店界面");
                break;
        }
    }

}
