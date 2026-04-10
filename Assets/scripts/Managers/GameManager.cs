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

    // 记录上一回合最大的数字卡
    public NumberCardInstance lastRoundMaxCard; 

    [Header("结算动画配置")]
    [Tooltip("显示本回合得分的停留时间（秒）")]
    public float roundScoreDisplayTime = 1.0f;

    [Tooltip("显示总分更新的停留时间（秒）")]
    public float totalScoreDisplayTime = 1.0f;

    //阶段点数数据
    public List<BigInteger> stagePointRequirements = new List<BigInteger>()
    {
       24, 240,2400,24000,240000,2400000,24000000,240000000,2400000000
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

    public void InitializeGame()
    {   
        Debug.Log("初始化游戏");
        currentPoints = 9999999999999;
        currentRound = 1;
        // 确保调用了 ChangeState，这样上面的 UI 逻辑才会跑起来
        ChangeState(GameState.PlayerTurn);
        // 清空祝福系统
        if (blessingManager != null)
        {
            blessingManager.ClearAllBlessings();
        }
        ShopManager.Instance.InitializeShop(); // 重置商店状态
        // 初始化UI显示
        UIManager.Instance.UpdatePointsDisplay(currentPoints);
        UIManager.Instance.UpdateRoundDisplay(currentRound);

        // 执行抽卡逻辑
        cardManager.InitializeStarterDeck();

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
        
        // 刷新所有游戏信息UI
        UIManager.Instance.RefreshAllGameInfo();
        // 重置本回合得分
        UIManager.Instance.ResetRoundScore();
        // 更新目标回合显示
        int nextTarget = GetNextStageRound();

        UIManager.Instance.UpdateTargetRoundDisplay(nextTarget);
        // 抽填空计算卡和对应数量的数字卡
        cardManager.DrawCardsForTurn();
    }

    public void CalculatePoints(CardManager formula)
    {
        if (currentState != GameState.PlayerTurn)
            return;
        // 检查是否填满了所有卡牌
        if (!IsFormulaComplete())
        {
            Debug.LogWarning("未填满所有卡牌，无法结算");
            return;
        }

        currentState = GameState.Calculation;
        StartCoroutine(CalculateProcessSequence(formula));

    }
    IEnumerator CalculateProcessSequence(CardManager formula)
    {
        // 1. 投骰子和递增数据
        //    并且获取本回合的最大数字卡,为经验主义祝福提供数据支持
        lastRoundMaxCard = formula.PrepareCardsForCalculation();

        // 2. 刷新UI显示投掷和递增后的结果
        UIManager.Instance.RefreshSelectedCardsDisplay(formula.selectedNumberCards);

        // 3. 停留0.3秒
        yield return new WaitForSeconds(0.3f);

        // 4. 计算结果
        BigInteger baseScore = formula.CalculateResult();
        //打头阵：原始结算结果第一位强制变9 
        if (blessingManager != null && blessingManager.hasLeadingCharge)
        {
            string numStr = baseScore.ToString();

            if (numStr.Length > 0)
            {
                // 第一位 → 9，后面保持不变
                numStr = "9" + numStr.Substring(1);
                baseScore = BigInteger.Parse(numStr);
                Debug.Log($"【打头阵】原始结果修改为：{baseScore}");
            }
        }

        // 计算基础倍率（根据公式卡数量）
        float baseMultiplier = PlayerCardInventory.Instance.GetFormulaCardCount();
        float blessingBonusMultiplier = GetCurrentMultiplier();
        float totalMultiplier = baseMultiplier + blessingBonusMultiplier;

        // 更新UI
        UIManager.Instance.multiplierText.text = "×" + totalMultiplier.ToString();

        // 计算最终得分
        BigInteger finalScore = baseScore * new BigInteger((decimal)totalMultiplier);

        // 逢七过效果
        if (blessingManager != null && blessingManager.CheckJackpot7Effect(baseScore))
        {
            finalScore = BigInteger.Zero;
        }

        // 启动分步显示协程
        StartCoroutine(ShowScoreStepByStep(baseScore, totalMultiplier, finalScore));
    }
    /// <summary>
    /// 分步显示得分的协程
    /// </summary>
    IEnumerator ShowScoreStepByStep(BigInteger baseScore, float multiplier, BigInteger finalScore)
    {
        // 第1步：显示本回合得分（基础分 × 倍率）
        UIManager.Instance.ShowPointsGain(finalScore); // 弹出 "+XXX" 提示

        yield return new WaitForSeconds(roundScoreDisplayTime);

        // 第2步：加入总分并显示
        AddPoints(finalScore);

        yield return new WaitForSeconds(totalScoreDisplayTime);

        // 第3步：进入商店
        EndTurn();
    }

    /// <summary>
    /// 检查公式是否填满
    /// </summary>
    bool IsFormulaComplete()
    {
        if (cardManager.currentFormulaCard == null)
        {
            Debug.LogWarning("没有公式卡");
            return false;
        }

        int requiredCount = cardManager.currentFormulaCard.RequiredCount;
        int filledCount = cardManager.selectedNumberCards.Count;

        Debug.Log($"公式卡要求: {requiredCount}, 已填入: {filledCount}");

        return filledCount >= requiredCount;
    }
    // 添加一个辅助方法，获取下一个结算回合
    public int GetNextStageRound()
    {
        // 遍历 stageRounds 列表 (3, 8, 15...)
        foreach (int roundLimit in stageRounds)
        {
            // 如果列表里的回合数大于或等于当前回合，它就是咱们的下一个目标
            if (roundLimit >= currentRound)
            {
                return roundLimit;
            }
        }
        // 如果超过了所有配置的回合，返回最后一个或显示最大值
        return stageRounds[stageRounds.Count - 1];
    }

    /// <summary>
    /// 获取当前倍率
    /// </summary>
    float GetCurrentMultiplier()
    {
        float multiplier = 0f;

        // 从祝福管理器获取倍率加成
        if (blessingManager != null)
        {
            float blessingMultiplier = blessingManager.GetFinalBlessingMultiplier();
            multiplier += blessingMultiplier;
            Debug.Log($"祝福倍率加成: {blessingMultiplier}，总倍率: {multiplier}");
        }

        return multiplier;
    }
    public void AddPoints(BigInteger points)
    {
        // 如果是扣除点数，直接扣除
        if (points < BigInteger.Zero)
        {
            currentPoints += points;
            UIManager.Instance.UpdatePointsDisplay(currentPoints);
            Debug.Log($"扣除点数：{-points}，当前总分：{currentPoints}");
            return;
        }

        // 获取加成前的点数
        BigInteger pointsBefore = currentPoints;

        // 添加基础点数
        currentPoints += points;

        // 应用祝福效果
        if (blessingManager != null)
        {
            BigInteger financialBonus = blessingManager.CalculateFinancialMasterBonus(pointsBefore);
            currentPoints += financialBonus;
        }

        // 立即更新UI显示
        UIManager.Instance.UpdatePointsDisplay(currentPoints);
        Debug.Log($"总分更新: {currentPoints}");
    }

    public void EndTurn()
    {
        // 更新UI显示
        UIManager.Instance.UpdatePointsDisplay(currentPoints);
        UIManager.Instance.UpdateRoundDisplay(currentRound);// 检查阶段要求



        // 检查是否到达阶段结算点
        if (IsStageRound(currentRound))
        {
            // 获取本阶段的点数要求
            stageRequirement = GetStageRequirementForRound(currentRound);

            Debug.Log($"第 {currentRound} 回合是阶段回合，要求点数: {stageRequirement}，当前点数: {currentPoints}");


            // 检查是否达到最终目标（第75回合）
            if (currentRound == 75 && currentPoints >= targetPoints)
            {
                // 游戏胜利
                Debug.Log("游戏胜利！达到最终目标");
                WinGame(true);
                return; // 不进入商店，直接显示胜利界面
            }
            // 进行阶段检查
            if (!CheckStageRequirement())
            {

               // 检查失败，游戏结束
               return; // 不进入商店
            }
            else   // 检查通过，扣除阶段要求的点数
            { 
                currentPoints -= stageRequirement; 
            }
            
            //// 祝福:能量扩散
            //if (BlessingManager.Instance.HasEnergySpread == 1)
            //{
            //    for (int i = 0; i < PlayerCardInventory.numberCards.Count; i++)
            //    {
            //        if (!cardManager.selectedNumberCards.Contains(PlayerCardInventory.numberCards[i]))
            //        {
            //            PlayerCardInventory.numberCards[i].EnergySpread();
            //        }
            //    }
            //}


        }
        currentState = GameState.Shop;
        shopManager.OpenShop();
        ChangeState(GameState.Shop);
    }
    /// <summary>
    /// 检查某个回合是否是阶段结算回合
    /// </summary>
    private bool IsStageRound(int round)
    {
        return stageRounds.Contains(round);
    }
    /// <summary>
    /// 执行阶段检查 - 返回值表示是否通过检查
    /// </summary>
    bool CheckStageRequirement()
    {
        // 找到当前回合对应的阶段索引
        int stageIndex = stageRounds.IndexOf(currentRound);

        if (stageIndex < 0 || stageIndex > stagePointRequirements.Count)
        {
            Debug.LogError($"无法找到回合 {currentRound} 对应的阶段要求！");
            return false;
        }

        BigInteger requiredPoints = stagePointRequirements[stageIndex];

        // 【关键】检查点数是否足够
        if (currentPoints < requiredPoints)
        {
            Debug.LogWarning($"阶段检查失败！回合{currentRound}: 需要{requiredPoints}点，当前{currentPoints}点");
            WinGame(false); // 显示失败界面
            return false;
        }

        Debug.Log($"阶段检查通过！回合{currentRound}: 点数{currentPoints} >= 需求{requiredPoints}");
        return true;
    }

    /// <summary>
    /// 根据回合数获取该阶段的点数要求
    /// </summary>
    private BigInteger GetStageRequirementForRound(int round)
    {
        // 找到对应的阶段索引
        int stageIndex = stageRounds.IndexOf(round);

        if (stageIndex >= 0 && stageIndex <= stagePointRequirements.Count)
        {
            return stagePointRequirements[stageIndex];
        }
        else
        {
            Debug.LogError($"找不到第 {round} 回合的阶段要求！");
            return BigInteger.Zero;
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
        // 3. 更新回合数显示
        UIManager.Instance.UpdateRoundDisplay(currentRound);
        // 4. 开始新回合
        StartPlayerTurn();
    }
    public void ReturnStartMenu()
    {
        //加载主菜单界面
        ChangeState(GameState.MainMenu);
        UIManager.Instance.pointstagePanel.SetActive(false);
    }
    void WinGame(bool isWin)//之后的panel会改，暂时先用一个。
    {
        if (isWin)
        {
            currentState = GameState.GameOver;
            // 显示游戏结束界面
            UIManager.Instance.ShowPanel(UIManager.Instance.gameOverPanel);
            Debug.Log("游戏胜利，达到最终目标");
        }
        else { 
            currentState = GameState.GameLose;
            // 显示游戏失败界面
            UIManager.Instance.ShowPanel(UIManager.Instance.gameOverPanel);
            UIManager.Instance.pointstagePanel.SetActive(false);
            Debug.Log("游戏失败，未达到阶段要求");
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
