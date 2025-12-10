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
    public enum GameState { PlayerTurn, Calculation, Shop, GameOver, GameLose }
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
        InitializeGame();
    }

    void InitializeGame()
    {
        // 初始化玩家卡组
        cardManager.InitializeStarterDeck();
        // 开始第一回合
        StartPlayerTurn();
    }



    public void StartPlayerTurn()
    {
        currentState = GameState.PlayerTurn;
        // 抽填空计算卡和对应数量的数字卡
        cardManager.DrawCardsForTurn();
    }

    public void CalculatePoints()
    {
        // 计算当前回合的点数
        //[to do]


        BigInteger result;
        // 添加到总点数
        AddPoints(result);

        // 结束回合
        EndTurn();
    }

    public void AddPoints(BigInteger points)
    {
        currentPoints += points;
        // 更新UI显示
        //[to do]
    }

    public void EndTurn()
    {
        currentRound++;

        foreach (int stageRound in stageRounds)
        {
            if (currentRound == stageRound)
            {
                CheckStageRequirement();
                return;
            }
        }

        StartPlayerTurn();
        
    }

    void CheckStageRequirement()
    {
        if (currentPoints < stageRequirement)
        {
            // 游戏失败
            GameOver(false);
        }
        if (currentRound == 75 && currentPoints >= targetPoints)
        {   // 达到最终目标，游戏胜利
            GameOver(true);
        }
        else
        {
            StartPlayerTurn();
        }
    }

    void GameOver(bool isWin)
    {
        if (isWin)
        {
            currentState = GameState.GameOver;
            // 显示游戏结束界面
            //[to do]
        }
        else { 
            currentState = GameState.GameLose;
        }


    }

 

}
