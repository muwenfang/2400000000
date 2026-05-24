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

    // 游戏模式
    public enum GameMode { Normal = 0, Involution = 1 }
    [Header("游戏模式")]
    public GameMode currentGameMode = GameMode.Normal;

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

    public FormulaSlot formulaSlot; // 公式卡槽位引用

    // 记录上一回合最大的数字卡
    public NumberCardInstance lastRoundMaxCard;
    // 记录本局的统计数据
    [Header("本局统计数据")]
    private BigInteger roundMaxNumberCardValue = 0; // 本局数字卡最大值
    private float roundMaxMultiplier = 0f; // 本局最高倍率
    private BigInteger roundMaxCalculationValue = 0; // 本局最高单轮计算值

    [Header("结算动画配置")]
    [Tooltip("显示本回合得分的停留时间（秒）")]
    public float roundScoreDisplayTime = 0.6f;

    [Tooltip("显示总分更新的停留时间（秒）")]
    public float totalScoreDisplayTime = 0.5f;

    [Tooltip("按槽位依次显示单卡结算值的间隔（秒）")]
    public float cardScoreDisplayTime = 0.25f;

    //阶段点数数据
    public List<BigInteger> stagePointRequirements = new List<BigInteger>()
    {
       24, 240,2400,24000,240000,2400000,24000000,240000000,2400000000
    };

    //阶段回合数
    public readonly List<int> stageRounds = new List<int>()
    {
        4,10,18,26,33,40,47,54,60
    };

    // ====================== 内卷模式 ======================
    [Header("内卷模式")]
    [Tooltip("只在最后一回合检查是否达标")]
    public static bool isInvolutionMode = false; // 静态，全局可访问
    private readonly int finalRound = 60;
    // ==========================================================

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

    //普通模式
    public void InitializeGame(int gameMode = 0)
    {   
        Debug.Log("初始化游戏");
        // 重置本局统计数据
        ResetRoundStatistics();

        currentPoints = 0;

        currentRound = 1;

        GameManager.isInvolutionMode = false;
        //不是内卷模式
        blessingManager.hasGodOfGambler = false;

        // 确保调用了 ChangeState，这样上面的 UI 逻辑才会跑起来
        ChangeState(GameState.PlayerTurn);
        // 清空祝福系统
        if (blessingManager != null)
        {
            blessingManager.ClearAllBlessings();
            blessingManager.InitializeBlessingSystem();
        }
        ShopManager.Instance.InitializeShop(); // 重置商店状态
        // 初始化UI显示
        UIManager.Instance.UpdatePointsDisplay(currentPoints);
        UIManager.Instance.UpdateRoundDisplay(currentRound);

        // 执行抽卡逻辑
        cardManager.InitializeStarterDeck();

        // 开始第一回合
        StartPlayerTurn();

        // 通知 UI 刷新视觉显示
        UIManager.Instance.RefreshGameUI();
    }
    /// <summary>
    /// 重置本局统计数据
    /// </summary>
    private void ResetRoundStatistics()
    {
        roundMaxNumberCardValue = 0;
        roundMaxMultiplier = 0f;
        roundMaxCalculationValue = 0;
    }
    //内卷模式
    public void InitializeGame_invol()
    {   
        Debug.Log("初始化游戏");
        ResetRoundStatistics();
        shopManager.totalRemovedNumberCards = 0;
        shopManager.totalRemovedFormulaCards = 0;

        currentPoints = 0;
        GameManager.isInvolutionMode = true;
        //是内卷模式
        blessingManager.hasGodOfGambler = false;
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
        
        if (blessingManager != null)
        {
            blessingManager.AddDialecticalPerRoundMultiplier();
            // 顺带重置唯心主义骰子结果（原有逻辑）
            blessingManager.NewRound_IdealismReset();
        }

        // 祝福贷款钱包：每回合扣除贷款15%的点数
        if (BlessingManager.Instance.hasLoanWallet == 1)
        {
            AddPoints(-BlessingManager.Instance.loan * 15/100);
        }

        // 刷新所有游戏信息UI
        UIManager.Instance.RefreshAllGameInfo();
        // 重置本回合得分
        UIManager.Instance.ResetRoundScore();
        // 更新目标回合显示
        int nextTarget = GetNextStageRound();

        UIManager.Instance.UpdateTargetRoundDisplay(nextTarget);
        // 抽填空计算卡和对应数量的数字卡targetPoints
        cardManager.DrawCardsForTurn();
    }

    #region 计算并显示得分
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

        // 记录本局数字卡的最大值
        if (lastRoundMaxCard != null)
        {
            BigInteger currentRoundMaxValue = lastRoundMaxCard.GetOutPutValue();
            if (currentRoundMaxValue > roundMaxNumberCardValue)
            {
                roundMaxNumberCardValue = currentRoundMaxValue;
                Debug.Log($"更新本局数字卡最大值: {roundMaxNumberCardValue}");
            }
        }

        // 2. 刷新UI显示投掷和递增后的结果
        UIManager.Instance.RefreshSelectedCardsDisplay(formula.selectedNumberCards);

        // 3. 停留0.3秒
        yield return new WaitForSeconds(0.3f);

        // 4. 先按槽位顺序显示每张卡本次实际结算出的点数
        List<BigInteger> rawCardScores = FormulaCalculator.GetCardValuesForDisplay(formula.selectedNumberCards, false);
        List<BigInteger> adjustedCardScores = FormulaCalculator.GetCardValuesForDisplay(formula.selectedNumberCards, true);
        yield return StartCoroutine(UIManager.Instance.ShowSelectedCardScoreSequence(
            formula.selectedNumberCards,
            rawCardScores,
            adjustedCardScores,
            cardScoreDisplayTime));

        // 5. 计算结果
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

        // 记录本局最高倍率
        if (totalMultiplier > roundMaxMultiplier)
        {
            roundMaxMultiplier = totalMultiplier;
        }

        // 更新UI
        UIManager.Instance.multiplierText.text = "×" + totalMultiplier.ToString();

        // 计算最终得分
        BigInteger finalScore = baseScore * new BigInteger((decimal)totalMultiplier);

        // 逢七过效果
        if (blessingManager != null && blessingManager.CheckJackpot7Effect(baseScore))
        {
            finalScore = BigInteger.Zero;
        }

        // 记录本回合的结算点数（baseScore，这是一次完整结算的值）
        if (finalScore > roundMaxCalculationValue)
        {
            roundMaxCalculationValue = finalScore;
            Debug.Log($"【统计数据】更新本局最高结算点: {roundMaxCalculationValue}");
        }

        // 启动原有分步显示协程
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

        UIManager.Instance.HideAllCardScoreOverlays();

        // 第3步：进入商店
        EndTurn();
    }
    #endregion
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

        if (CardManager.Instance != null)
        {
            return CardManager.Instance.HasAllSlotsFilled();
        }

        // 检查填入的卡牌数是否等于需要的数量
        if (cardManager.selectedNumberCards == null ||
            cardManager.selectedNumberCards.Count != cardManager.currentFormulaCard.RequiredCount)
        {
            Debug.LogWarning($"[GameManager] 卡牌数量不足。期望: {cardManager.currentFormulaCard.RequiredCount}，实际: {cardManager.selectedNumberCards?.Count ?? 0}");
            return false;
        }

        // 检查是否存在null值
        for (int i = 0; i < cardManager.selectedNumberCards.Count; i++)
        {
            if (cardManager.selectedNumberCards[i] == null)
            {
                Debug.LogWarning($"[GameManager] 槽位 {i} 为空，还有未填入的卡牌位置");
                return false;
            }
        }

        Debug.Log("[GameManager] 公式完全填满，可以结算");
        return true;
    }

    // 添加一个辅助方法，获取下一个结算回合
    public int GetNextStageRound()
    {
        // 内卷模式
        if (GameManager.isInvolutionMode)
        {
            // 内卷模式：永远只返回最后一回合60
            return 60;
        }

        // 原有普通模式逻辑
        foreach (int roundLimit in stageRounds)
        {
            if (roundLimit >= currentRound)
           {
                return roundLimit;
            }
        }
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
            //赌神传说
            blessingMultiplier += blessingManager.GetGodOfGamblerTempMultiplier();
            multiplier += blessingMultiplier;
            Debug.Log($"祝福倍率加成: {blessingMultiplier}，总倍率: {multiplier}");
        }

        return multiplier;
    }
    
    public void AddPoints(BigInteger points)
    {
       currentPoints += points;
       UIManager.Instance.UpdatePointsDisplay(currentPoints);
       Debug.Log($"增加点数：{points}，当前总分：{currentPoints}");

        // 立即更新UI显示
        UIManager.Instance.UpdatePointsDisplay(currentPoints);
        Debug.Log($"总分更新: {currentPoints}");
    }

    public void EndTurn()
    {
        // 更新UI显示
        UIManager.Instance.UpdatePointsDisplay(currentPoints);
        UIManager.Instance.UpdateRoundDisplay(currentRound);

        // ====================== 【内卷模式核心逻辑】 ======================
        if (GameManager.isInvolutionMode)
        {
            // 只在最后一回合检查
            if (currentRound == finalRound)
            {
                Debug.Log($"【内卷模式】最终回合检查");
                
                if (currentPoints >= targetPoints)
                {
                    WinGame(true); // 胜利
                }
                else
                {
                    WinGame(false); // 失败
                }
                return; // 不进商店
            }
            // 非最后一回合：直接进商店，不做任何阶段检查
            currentState = GameState.Shop;
            shopManager.OpenShop();
            ChangeState(GameState.Shop);
            return;
        }
        // ==================================================================

        // 原有普通模式逻辑
        // 检查是否到达阶段结算点
        if (IsStageRound(currentRound) && !GameManager.isInvolutionMode)
        {
            // 获取本阶段的点数要求
            stageRequirement = GetStageRequirementForRound(currentRound);

            Debug.Log($"第 {currentRound} 回合是阶段回合，要求点数: {stageRequirement}，当前点数: {currentPoints}");

            // 检查是否达到最终目标（第60回合）
            if (currentRound == 60 && currentPoints >= targetPoints)
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
            else   // 检查通过，扣除阶段要求的点数，这里要刷新UI
            {
                currentPoints -= stageRequirement;
                // 刷新点数 UI 显示
                UIManager.Instance.UpdatePointsDisplay(GameManager.Instance.currentPoints);
            }
        }
            
         // 祝福:能量扩散
         if (BlessingManager.Instance.hasEnergySpread == 1)
         {
            for (int i = 0; i < PlayerCardInventory.Instance.numberCards.Count; i++)
            {
               if (!cardManager.selectedNumberCards.Contains(PlayerCardInventory.Instance.numberCards[i]))
               {
                 PlayerCardInventory.Instance.numberCards[i].EnergySpread();
               }
            }
         }
         //祝福:理财大师
         if (BlessingManager.Instance.GetBlessingTypeCount(BlessingData.BlessingType.FinancialMaster) != 0)
         {
            currentPoints += BlessingManager.Instance.CalculateFinancialMasterBonus(currentPoints);

             // 立即更新UI显示
             UIManager.Instance.UpdatePointsDisplay(currentPoints);
             Debug.Log($"总分更新: {currentPoints}");
         }


        //打开商店界面
        currentState = GameState.Shop;
        shopManager.OpenShop();
        ChangeState(GameState.Shop);
        // 短视倍率缩减
        if (blessingManager != null)
        {
            blessingManager.OnNewRound_ShortSightDecay();
        }
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

        // 检查点数是否足够
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
        shopManager.deleteCostPanel.SetActive(false);
        //UIManager.Instance.pointstagePanel.SetActive(false);
    }

    void WinGame(bool isWin)
    {
        if (isWin)
        {
            if (DataSavingManager.Instance != null)
            {
                // 获取本局统计数据
                int formulaCardCount = PlayerCardInventory.Instance.GetFormulaCardCount();

                DataSavingManager.Instance.OnGameWin(
                    gameMode: (int)currentGameMode,
                    finalPoints: currentPoints,
                    maxMultiplier: roundMaxMultiplier,
                    formulaCardCount: formulaCardCount,
                    numberCardMaxValue: roundMaxNumberCardValue ,
                    maxCalculationValue:roundMaxCalculationValue  
                );

                // 显示本局统计数据
                if (DataDisplayManager.Instance != null)
                {
                    DataDisplayManager.Instance.ShowCurrentGameStats(
                        gameMode: (int)currentGameMode,
                        maxPoints: currentPoints,
                        maxMultiplier: roundMaxMultiplier,
                        maxNumberCard: roundMaxNumberCardValue,
                        maxCalculationValue: roundMaxCalculationValue
                    );
                }
            }

            currentState = GameState.GameOver;
            // 显示游戏结束界面
            UIManager.Instance.ShowPanel(UIManager.Instance.gameOverPanel);
            Debug.Log("游戏胜利，达到最终目标");

        }
        else { 
            currentState = GameState.GameLose;
            // 显示游戏失败界面
            UIManager.Instance.ShowPanel(UIManager.Instance.loseGamePanel);
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

    //退出游戏
    public void ExitGame()
    {
        Application.Quit();
    }
}
