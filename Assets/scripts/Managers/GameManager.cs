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
    public enum GameMode { Normal = 0, Involution = 1, Endless = 2 }
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

    public FloatingMessage floatingMessage;

    public FormulaSlot formulaSlot; // 公式卡槽位引用

    // 记录上一回合最大的数字卡
    public NumberCardInstance lastRoundMaxCard;
    // 记录本局的统计数据
    [Header("本局统计数据")]
    private BigInteger roundMaxNumberCardValue = 0; // 本局数字卡最大值
    private long roundMaxMultiplier = 0; // 本局最高倍率
    private BigInteger roundMaxCalculationValue = 0; // 本局最高单轮计算值

    [Header("结算动画配置")]
    [Tooltip("显示本回合得分的停留时间（秒）")]
    public float roundScoreDisplayTime = 0.4f;

    [Tooltip("显示总分更新的停留时间（秒）")]
    public float totalScoreDisplayTime = 0.6f;

    [Tooltip("按槽位依次显示单卡结算值的间隔（秒）")]
    public float cardScoreDisplayTime = 0.25f;

    [Header("测试选项")]
    [Tooltip("跳过结算动画：不播放数字卡数值弹跳、不逐卡展示点数、不等待得分分步停留。" +
             "只跳过表现与等待，结算数值逻辑完全不变。")]
    public bool skipSettlementAnimation = false;

    /// <summary>
    /// 结算处是否应跳过动画。统一由 GameManager.skipSettlementAnimation 决定，
    /// 其它模块（UIManager 等）一律通过这里判定，避免各处各自读字段造成口径不一致。
    /// </summary>
    public static bool ShouldSkipSettlementAnimation
    {
        get { return Instance != null && Instance.skipSettlementAnimation; }
    }

    // ===================== 结算状态机 =====================
    // 目的：把"算分/加分/进商店"从动画协程里解耦出来，使"点击结算后退出"能够
    // 当场跳出动画、同步完成结算并落盘。各标记保证每一步只执行一次（幂等），
    // 因此正常协程与退出补算可以安全地共用同一套确定性逻辑。
    private Coroutine activeSettlementCoroutine;  // 最外层结算协程（退出时停掉）
    private bool settlementDiceSettled;           // 骰子/递增已定格（PrepareCardsForCalculation 已执行）
    private bool settlementComputed;              // 得分已计算（含财星/反刍/反物质云等一次性效果）
    private bool settlementPointsApplied;         // 得分已入账（坠落/正义修正 + AddPoints）
    private bool settlementFinished;              // 结算收尾已完成（阶段判定/扣点/生成商店）
    private bool settlementAborted;               // 结算被"退出"打断：所有结算协程必须立刻停手
    private BigInteger settlementBaseScore;       // 本次结算基础分（缓存，供退出补算复用）
    private long settlementTotalMultiplier;       // 本次结算总倍率（缓存）
    private BigInteger settlementFinalScore;      // 本次结算最终得分（缓存）

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

    [Header("内卷模式")]
    [Tooltip("只在最后一回合检查是否达标")]
    public static bool isInvolutionMode = false; // 静态，全局可访问
    private readonly int finalRound = 60;

    [Header("无尽模式")]
    public static bool isEndlessMode = false;

    void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        ChangeState(GameState.MainMenu);
    }

    //普通模式
    public void InitializeGame(int gameMode = 0)
    {
        Debug.Log("初始化游戏");
        // 开新游戏：旧局内存档作废（存档只有一个，新游戏自动覆盖）
        RunSaveManager.ClearSave();
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

        // 难度设置：初始数字卡+1
        ApplyOneMoreCardEffect();

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
        roundMaxMultiplier = 0;
        roundMaxCalculationValue = 0;
    }

    /// <summary>
    /// 难度设置：初始数字卡+1效果
    /// </summary>
    private void ApplyOneMoreCardEffect()
    {
        if (DifficultySettingsManager.Instance == null) return;
        if (!DifficultySettingsManager.oneMoreCard) return;

        NumberCardData card = DifficultySettingsManager.Instance.extraStartCard;
        if (card == null)
        {
            // 兜底：从卡牌库中找到值为1的普通数字卡
            if (cardManager != null && cardManager.numberCardLibrary != null)
            {
                foreach (var c in cardManager.numberCardLibrary.allCards)
                {
                    if (c.partA != null && c.partA.value == 1
                        && !c.partA.isDice && !c.partA.isIncremental
                        && c.logicalType == NumberCardData.LogicalType.Normal
                        && c.partB == null)
                    {
                        card = c;
                        break;
                    }
                }
            }
        }

        if (card != null && PlayerCardInventory.Instance != null)
        {
            PlayerCardInventory.Instance.AddNumberCard(card);
            if (cardManager != null) cardManager.SyncDeckFromInventory();
        }
    }
    //内卷模式
    public void InitializeGame_invol()
    {
        Debug.Log("初始化游戏");
        // 开新游戏：旧局内存档作废
        RunSaveManager.ClearSave();
        ResetRoundStatistics();
        shopManager.totalRemovedNumberCards = 0;
        shopManager.totalRemovedFormulaCards = 0;

        currentPoints = 0;
        GameManager.isInvolutionMode = true;
        GameManager.isEndlessMode = false;
        //是内卷模式
        blessingManager.hasGodOfGambler = false;
        currentRound = 1;
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

        // 难度设置：初始数字卡+1
        ApplyOneMoreCardEffect();

        // 开始第一回合
        StartPlayerTurn();

        //  关键：通知 UI 刷新视觉显示
        UIManager.Instance.RefreshGameUI();
    }
    //无尽模式
    public void EnterEndlessMode()
    {
        Debug.Log("进入无尽模式 - 从第61回合开始");
        // 无尽模式延续同一局（点数/卡牌/祝福全部保留，仅回合切到61），不清局内存档：
        // 存档只在普通/内卷开新游戏时作废。此时崩溃退出还能退回进入无尽前的存档。
        GameManager.isEndlessMode = true;
        GameManager.isInvolutionMode = false;
        currentRound = 61;

        // 无尽模式入口视为开新一局：AlwaysRefresh 祝福的出现权重统计随新局一起恢复。
        // （其它入口 InitializeGame / InitializeGame_invol 走 InitializeShop 时已重置）
        if (ShopManager.Instance != null)
            ShopManager.Instance.ResetAlwaysRefreshTracking();

        ChangeState(GameState.PlayerTurn);
        StartPlayerTurn();
        UIManager.Instance.RefreshGameUI();
    }

    public void StartPlayerTurn()
    {
        Debug.Log("开始回合");
        ChangeState(GameState.PlayerTurn);
        Debug.Assert(currentState == GameState.PlayerTurn);

        // 新回合开始：上一回合的结算检查点已失效（此时要么已补完结算，要么按未结算处理）
        RunSaveManager.ClearSettlementCheckpoint();
        // 新回合开始：清空上一回合的结算状态机（幂等标记必须归零）
        ResetSettlementState();

        // 新回合开始：解除上一回合结算判定动画期间的手牌锁定
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetCardDragLocked(false);
        }

        // 阴阳祝福：每回合开始时点数取反（最优先生效，在其他所有祝福之前）
        if (BlessingManager.Instance != null && BlessingManager.Instance.hasYinYang)
        {
            currentPoints = -currentPoints;
        }
        // 祝福：延迟满足
        BlessingManager.Instance.UpdateDelaySatisfactionPerRound();
        // 祝福：反刍（每回合倒计时，到期发放记录点数的相反数）
        BlessingManager.Instance.UpdateRuminationPerRound();
        // 祝福：日积月累倍率
        if(blessingManager.dayAfterDayCount > 0)
        {
            blessingManager.totalMultiplierBonus += blessingManager.dayAfterDayCount;
        }
        
        if (blessingManager != null)
        {   // 辩证主义每回合增加倍率，点数，价格等效果
            blessingManager.AddDialecticalPerRoundMultiplier();
            // 顺带重置唯心主义骰子结果（原有逻辑）
            blessingManager.NewRound_IdealismReset();
        }

        // 祝福国王棋盘：每回合获得1点，每经过一回合此祝福获得的点数翻倍
        if (BlessingManager.Instance != null)
        {
            BlessingManager.Instance.ApplyKingOfTheBoardPerRound();
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

        // 祝福缤纷多彩：抽卡后判定本回合抽到的所有数字卡是否同时包含普通/黄金/绿色/骰子
        if (BlessingManager.Instance != null)
        {
            BlessingManager.Instance.ApplyColorfulPerRound();
        }
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

        // 结算判定动画期间锁住手牌/公式区，禁止拖拽等互动
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetCardDragLocked(true);
        }

        ResetSettlementState();
        activeSettlementCoroutine = StartCoroutine(CalculateProcessSequence(formula));

    }

    /// <summary>重置结算状态机（每次点击结算 / 新回合开始时调用）</summary>
    private void ResetSettlementState()
    {
        activeSettlementCoroutine = null;
        settlementDiceSettled = false;
        settlementComputed = false;
        settlementPointsApplied = false;
        settlementFinished = false;
        settlementAborted = false;
        settlementBaseScore = BigInteger.Zero;
        settlementTotalMultiplier = 0;
        settlementFinalScore = BigInteger.Zero;
    }
    IEnumerator CalculateProcessSequence(CardManager formula)
    {
        // 1. 投骰子和递增数据（定格后立即抓结算检查点）
        SettleDiceAndLastRoundMax(formula);
        if (settlementAborted) yield break;

        // 2. 刷新UI显示投掷和递增后的结果
        yield return StartCoroutine(UIManager.Instance.RefreshSelectedCardsDisplay(formula.selectedNumberCards));
        if (settlementAborted) yield break;

        // 3. 显示逐卡得分并计算最终得分（确定性计算 + 表现动画）
        yield return StartCoroutine(SettlementScoreSequence(formula, true));
    }

    /// <summary>
    /// 骰子/递增定格 + 记录本回合最大卡（经验主义用）+ 抓结算检查点。
    /// 幂等：退出补算时若协程已经执行过，不会二次投骰/二次递增。
    /// </summary>
    private void SettleDiceAndLastRoundMax(CardManager formula)
    {
        if (settlementDiceSettled) return;
        settlementDiceSettled = true;

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

        // 存档检查点：骰子/递增/流星等结算判定已全部定格（点击结算时结果即已决定）。
        // 此后任何时刻退出游戏或返回主菜单，都按"结算完成"处理。
        RunSaveManager.CaptureSettlementCheckpoint();
    }

    /// <summary>
    /// 结算的表现层：逐卡得分展示 + 调用确定性计算 + 分步加分动画。
    /// playSettleAnimation=false 时跳过所有展示与等待（供"继续游戏"补完结算使用），
    /// 数值逻辑与正常结算完全一致。
    /// </summary>
    IEnumerator SettlementScoreSequence(CardManager formula, bool playSettleAnimation)
    {
        if (playSettleAnimation)
        {
            // 先按槽位顺序显示每张卡本次实际结算出的点数（纯表现，无副作用）
            List<BigInteger> rawCardScores = FormulaCalculator.GetCardValuesForDisplay(formula.selectedNumberCards, false);
            List<BigInteger> adjustedCardScores = FormulaCalculator.GetCardValuesForDisplay(formula.selectedNumberCards, true);

            yield return StartCoroutine(UIManager.Instance.ShowSelectedCardScoreSequence(
                formula.selectedNumberCards,
                rawCardScores,
                adjustedCardScores,
                cardScoreDisplayTime));
        }
        if (settlementAborted) yield break;

        // 确定性结算（幂等：退出补算可安全复用，不会二次消耗一次性祝福）
        BigInteger baseScore;
        long totalMultiplier;
        BigInteger finalScore;
        ComputeSettlementScore(formula, out baseScore, out totalMultiplier, out finalScore);
        if (settlementAborted) yield break;

        yield return StartCoroutine(ShowScoreStepByStep(baseScore, totalMultiplier, finalScore, playSettleAnimation));
    }

    /// <summary>
    /// 结算的确定性数值部分（骰子已定格后的全部得分逻辑，含所有祝福结算）。
    /// 幂等：由 settlementComputed 保护，重复调用（正常协程 + 退出补算）不会二次消耗
    /// 财星 / 反刍 / 反物质云等一次性效果，得分恒定。
    /// </summary>
    private void ComputeSettlementScore(CardManager formula, out BigInteger baseScore, out long totalMultiplier, out BigInteger finalScore)
    {
        if (settlementComputed)
        {
            baseScore = settlementBaseScore;
            totalMultiplier = settlementTotalMultiplier;
            finalScore = settlementFinalScore;
            return;
        }

        // 逐卡原始得分（反物质云判定用，纯读取）
        List<BigInteger> rawCardScores = FormulaCalculator.GetCardValuesForDisplay(formula.selectedNumberCards, false);

        // 4. 计算结果
        baseScore = formula.CalculateResult();
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
        int baseMultiplier = PlayerCardInventory.Instance.GetFormulaCardCount();
        long blessingBonusMultiplier = GetCurrentMultiplier();
        totalMultiplier = baseMultiplier + blessingBonusMultiplier;
        
        // 反物质云：每有一张判定结果为负数的数字卡，按永久倍率正负获得 -10 / +5 永久倍率
        if (blessingManager != null && blessingManager.GetBlessingTypeCount(BlessingData.BlessingType.AntimatterCloud) > 0)
        {
            blessingManager.ApplyAntimatterCloud(rawCardScores, totalMultiplier);
        }

        // 黄金数字：仅将黄金数字本身的数值增加到倍率中（不含卡牌其他部分的值）
        // goldenMultiplierTotal：黄金数贡献的倍率（与公式卡数同属“非祝福倍率”，供八方来财区分祝福部分）
        long goldenMultiplierTotal = 0;
        foreach (var card in formula.selectedNumberCards)
        {
            BigInteger goldenValue = BigInteger.Zero;
            if (card.cardData.partA.isGolden)
                goldenValue += card.currentA;
            if (card.cardData.partB != null && card.cardData.partB.isGolden)
                goldenValue += card.currentB;

            if (goldenValue != 0)
            {
                totalMultiplier += (long)goldenValue;
                goldenMultiplierTotal += (long)goldenValue;
                Debug.Log($"黄金数字 {card.cardData.cardName} 的黄金数 {goldenValue} 计入倍率，当前总倍率: {totalMultiplier}");
            }
        }

        // 反物质核祝福：结算计算总倍率时，将总倍率（基础+祝福+黄金等所有来源）变为其相反数
        if (blessingManager != null &&
            blessingManager.GetBlessingTypeCount(BlessingData.BlessingType.AntimatterNucleus)%2 == 1)
        {
            totalMultiplier = -totalMultiplier;
            Debug.Log($"反物质核：总倍率变为相反数，当前总倍率: {totalMultiplier}");
        }

        // 记录本局最高倍率
        if (totalMultiplier > roundMaxMultiplier)
        {
            roundMaxMultiplier = totalMultiplier;
        }

        // 更新UI
        UIManager.Instance.multiplierText.text = "×" + totalMultiplier.ToString();

        // 计算最终得分
        finalScore = baseScore * new BigInteger((decimal)totalMultiplier);

        // 逢七过效果
        if (blessingManager != null && blessingManager.CheckJackpot7Effect(baseScore))
        {
            finalScore = BigInteger.Zero;
        }

        // 嗜赌如命效果：若抽卡时未抽取到含有骰子的数字卡，本回合最终计算结果视为0
        if (blessingManager != null && blessingManager.hasAddictedtoGambling &&
            (formula == null || !formula.drewDiceCardThisRound))
        {
            finalScore = BigInteger.Zero;
            Debug.Log("嗜赌如命：本回合未抽到含有骰子的数字卡，最终计算结果视为0");
        }

        // 爱财如命效果：若抽卡时未抽取到含有黄金数的数字卡，本回合最终计算结果视为0
        if (blessingManager != null && blessingManager.hasLovingWealth &&
            (formula == null || !formula.drewGoldenCardThisRound))
        {
            finalScore = BigInteger.Zero;
            Debug.Log("爱财如命：本回合未抽到含有黄金数的数字卡，最终计算结果视为0");
        }

        // 八方来财效果：本回合得分中“依靠祝福获得的点数”×1.01^层数（向下取整），并入本轮得分
        if (blessingManager != null && blessingManager.EightWaysToWealth > 0)
        {
            // 非祝福来源倍率 = 公式卡数 + 黄金数（不含祝福倍率）
            long nonBlessingMultiplier = baseMultiplier + goldenMultiplierTotal;
            // 祝福获得的点数 = 总点数(finalScore) - 算式点数 × (公式卡数+黄金数)
            BigInteger blessingPortion = finalScore - baseScore * nonBlessingMultiplier;
            if (blessingPortion > 0)
            {
                BigInteger boostedPortion = blessingManager.BoostByEightWaysToWealth(blessingPortion);
                // 非祝福部分保持不变，祝福部分 ×1.01^层数
                finalScore = finalScore - blessingPortion + boostedPortion;
                Debug.Log($"八方来财×{blessingManager.EightWaysToWealth}层：祝福获得点数 {blessingPortion} → {boostedPortion}，最终得分 {finalScore}");
            }
        }

        // 财星效果：仅购买后的下一回合结算时 finalScore *= 1.02^层数（向下取整），用后即失效
        int wealthStarCharges = blessingManager != null ? blessingManager.ConsumeWealthStarCharges() : 0;
        if (wealthStarCharges > 0)
        {
            for (int i = 0; i < wealthStarCharges; i++)
            {
                finalScore = finalScore * 102 / 100;
            }
            Debug.Log($"财星效果：×1.02^{wealthStarCharges}（下一回合生效，本次消耗），最终得分: {finalScore}");
        }

        // 记录本回合的结算点数（baseScore，这是一次完整结算的值）
        if (finalScore > roundMaxCalculationValue)
        {
            roundMaxCalculationValue = finalScore;
            Debug.Log($"【统计数据】更新本局最高结算点: {roundMaxCalculationValue}");
        }

        // 反刍祝福：通过计算获得的点数（合计倍率）为负数时记录，3回合后获得相反数
        if (BlessingManager.Instance != null && BlessingManager.Instance.hasRumination && finalScore < 0)
        {
            BlessingManager.Instance.RecordRumination(finalScore);
        }
        
        {
            BlessingManager.Instance.RecordRumination(finalScore);
        }

        // 缓存结果并封口：后续任何调用（含退出补算）直接复用，不再重复结算
        settlementBaseScore = baseScore;
        settlementTotalMultiplier = totalMultiplier;
        settlementFinalScore = finalScore;
        settlementComputed = true;
    }

    /// <summary>
    /// 分步显示得分的协程（纯表现 + 两步幂等的数值落账）
    /// </summary>
    IEnumerator ShowScoreStepByStep(BigInteger baseScore, long multiplier, BigInteger finalScore, bool playSettleAnimation)
    {
        // 测试用：跳过动画时仍然照常弹出得分提示与加分，只是不做任何停留等待
        // "继续游戏"补完结算（playSettleAnimation=false）时同样只执行数值逻辑
        bool skipAnimation = ShouldSkipSettlementAnimation || !playSettleAnimation;

        // 第1步：显示本回合得分（基础分 × 倍率）
        UIManager.Instance.ShowPointsGain(finalScore); // 弹出 "+XXX" 提示

        floatingMessage.SetScoreMessage(finalScore.ToString());//弹窗显示得分

        if (!skipAnimation)
            yield return new WaitForSeconds(roundScoreDisplayTime);
        if (settlementAborted) yield break;

        // 第2步：加入总分并显示（幂等）
        AddSettlementPoints(finalScore);

        if (!skipAnimation)
            yield return new WaitForSeconds(totalScoreDisplayTime);
        if (settlementAborted) yield break;

        // 第3步：收尾（隐藏卡面分数 / 鬼神境 / 阶段判定 / 生成商店），幂等
        FinishSettlementAfterPoints();
    }

    /// <summary>
    /// 得分入账：坠落/绝对正义修正 + AddPoints。
    /// 幂等——正常协程与退出补算只会真正入账一次。
    /// </summary>
    private void AddSettlementPoints(BigInteger finalScore)
    {
        if (settlementPointsApplied) return;
        settlementPointsApplied = true;

        // 坠落祝福：计算结果为负数时翻5倍
        if (BlessingManager.Instance != null && BlessingManager.Instance.hasFall && finalScore < 0)
        {
            finalScore *= 5;
        }
        //绝对正义
        if (BlessingManager.Instance != null && BlessingManager.Instance.hasJustice && finalScore < 0)
        {
            finalScore = BigInteger.Abs(finalScore);
        }

        AddPoints(finalScore);
    }

    /// <summary>
    /// 得分之后的收尾：隐藏卡面分数、鬼神境、阶段判定/扣点/生成商店。
    /// 幂等——退出补算与动画协程只会执行一次。
    /// </summary>
    private void FinishSettlementAfterPoints()
    {
        if (settlementFinished) return;
        settlementFinished = true;

        UIManager.Instance.HideAllCardScoreOverlays();

        // 鬼神境祝福：每回合获得骰子判定点数总和数量的随机可叠加祝福
        if (BlessingManager.Instance != null && BlessingManager.Instance.SpiritGodRealm > 0)
        {
            BlessingManager.Instance.ApplySpiritGodRealm();
        }

        // 进入商店（阶段判定、扣点、OpenShop 生成商品、切换到商店界面都在这里）
        EndTurn();

        // 结算完成即落盘（每个结算点都是自动存档点）：
        // 继续游戏时直接进入这同一次商店，商品/价格/已售标记原样保留。
        if (currentState == GameState.Shop)
        {
            RunSaveManager.SaveSettledRun();
        }
    }

    /// <summary>
    /// 结算进行中退出（返回主菜单 / 关闭游戏）时调用：跳过全部动画，当场把结算做完并生成商店。
    /// 返回 true 表示结算已完成且已写入"商店已就绪"存档——继续游戏时直接进入那一次商店。
    /// 返回 false 表示无法补算（公式未填满，调用方按未结算存档）或补算触发了对局结束（存档保持不动，只有开新游戏才作废）。
    /// </summary>
    public bool FinishSettlementOnExit()
    {
        if (currentState != GameState.Calculation) return false;

        // 1. 让仍在跑的结算协程就地停手（它可能正卡在某个 WaitForSeconds 里）
        settlementAborted = true;
        if (activeSettlementCoroutine != null)
        {
            StopCoroutine(activeSettlementCoroutine);
            activeSettlementCoroutine = null;
        }

        if (cardManager == null || !IsFormulaComplete())
        {
            Debug.LogWarning("[结算] 退出时公式未填满，无法补算，按未结算状态存档");
            return false;
        }

        // 2. 补上尚未执行的确定性步骤（每一步都幂等）
        SettleDiceAndLastRoundMax(cardManager);

        BigInteger baseScore;
        long totalMultiplier;
        BigInteger finalScore;
        ComputeSettlementScore(cardManager, out baseScore, out totalMultiplier, out finalScore);

        AddSettlementPoints(finalScore);
        FinishSettlementAfterPoints();

        // FinishSettlementAfterPoints 内部已在进入商店时落盘；此处只用状态判断结果
        if (currentState == GameState.Shop)
        {
            Debug.Log("[结算] 退出时已跳过动画完成结算，并写入商店存档（继续游戏直接进商店）");
            return true;
        }

        Debug.Log("[结算] 退出补算触发了对局结束：存档保持不动（只有开新游戏才作废）");
        return false;
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
        // 无尽模式：每回合都是结算回合
        if (GameManager.isEndlessMode)
        {
            return currentRound;
        }

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
    long GetCurrentMultiplier()
    {
        long multiplier = 0;

        // 从祝福管理器获取倍率加成
        if (blessingManager != null)
        {
            long blessingMultiplier = blessingManager.GetFinalBlessingMultiplier();
            //赌神传说
            blessingMultiplier += blessingManager.GetGodOfGamblerTempMultiplier();
            multiplier += blessingMultiplier;
            Debug.Log($"祝福倍率加成: {blessingMultiplier}，总倍率: {multiplier}");
        }

        return multiplier;
    }
    
    public void AddPoints(BigInteger points)
    {
        // 坠落祝福：若计算结果为负数，扣点翻5倍
        if (BlessingManager.Instance != null && BlessingManager.Instance.hasFall && points < 0)
        {
            points = points * 5;
        }
       currentPoints += points;
       UIManager.Instance.UpdatePointsDisplay(currentPoints);
       Debug.Log($"增加点数：{points}，当前总分：{currentPoints}");

        // 立即更新UI显示
        UIManager.Instance.UpdatePointsDisplay(currentPoints);
        Debug.Log($"总分更新: {currentPoints}");
    }

    /// <summary>
    /// 无尽模式每回合需要扣除的点数：第61回合 2400000000，
    /// 此后每过一回合再乘以 10^(当前回合-61)（第62回合 24亿×10、第63回合 24亿×100…）。
    /// UI 显示与实际扣费共用本函数，保证"界面上看到的数"就是"实际要扣的数"。
    /// </summary>
    public BigInteger GetEndlessRoundCost(int round)
    {
        int exponent = round - 61;
        if (exponent < 0) exponent = 0;   // 防御：回合数不足61时按第61回合计，避免 BigInteger.Pow 负指数抛异常
        return (BigInteger)2400000000 * BigInteger.Pow(10, exponent);
    }

    public void EndTurn()
    {
        // 更新UI显示
        UIManager.Instance.UpdatePointsDisplay(currentPoints);
        UIManager.Instance.UpdateRoundDisplay(currentRound);

        //  【内卷模式核心逻辑】
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

        //【无尽模式核心逻辑】 
        if (GameManager.isEndlessMode)
        {
            // 第N回合扣除: 24亿 × 10^(N-61)，每回合指数增长
            stageRequirement = (BigInteger)2400000000 * BigInteger.Pow(10, currentRound - 61);
            if (currentPoints < stageRequirement)
            {
                // 记录无尽模式数据
                if (DataSavingManager.Instance != null)
                {
                    int difficultyLevel = DifficultySettingsManager.Instance != null
                        ? DifficultySettingsManager.Instance.GetCurrentDifficultyLevel() : 0;
                    DataSavingManager.Instance.OnEndlessGameOver(currentRound, difficultyLevel);
                }
                WinGame(false);
                return;
            }
            else
            {
                currentPoints -= stageRequirement;
                UIManager.Instance.UpdatePointsDisplay(GameManager.Instance.currentPoints);
            }
        }

        // 检查是否到达阶段结算点（排除内卷模式和无尽模式，它们有自己的逻辑）
        if (IsStageRound(currentRound) && !GameManager.isInvolutionMode && !GameManager.isEndlessMode)
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
        if (blessingManager != null && BlessingManager.Instance.shortSightCount > 0)
        {
            BlessingManager.Instance.totalMultiplierBonus -= BlessingManager.Instance.shortSightCount;
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


        // 2. 增加回合数
        currentRound++;
        // 3. 更新回合数显示
        UIManager.Instance.UpdateRoundDisplay(currentRound);
        // 4. 开始新回合
        StartPlayerTurn();
    }

    public void ReturnStartMenu()
    {
        if (currentState == GameState.Calculation)
        {
            // 结算动画期间退出：当场跳过动画把结算做完、生成商店并写档。
            // 绝不能任由结算协程继续跑——否则它会走完动画后 OpenShop 并切到商店界面，
            // 把"新刷出来的商店"盖在主菜单上（历史缺陷）。
            bool settledToShop = FinishSettlementOnExit();
            // 补算触发对局结束（GameOver/GameLose）时存档保持不动；
            // 只有公式未填满、无法补算时才按未结算状态正常存档。
            if (!settledToShop && currentState == GameState.Calculation)
            {
                RunSaveManager.SaveRun();
            }
        }
        else
        {
            // 返回主菜单：自动保存最新局内存档（商店/游戏界面各有对应口径）。
            // GameOver/GameLose 状态下 SaveRun 自身不写档（default 分支），存档保持不动。
            RunSaveManager.SaveRun();
        }

        //加载主菜单界面
        ChangeState(GameState.MainMenu);
        shopManager.deleteCostPanel.SetActive(false);

    }

    /// <summary>
    /// 退出游戏前自动保存局内进度（在商店退出保留商品、在游戏界面退出保留手牌、
    /// 结算动画期间退出按已结算处理）。
    /// </summary>
    void OnApplicationQuit()
    {
        // 结算动画期间关闭游戏：当场跳过动画完成结算（含生成商店并写"商店已就绪"存档），
        // 下次启动点"继续游戏"直接进入那一次商店，不再重播动画、不重抽商品。
        if (currentState == GameState.Calculation && FinishSettlementOnExit())
        {
            return; // 存档已写好
        }

        // 补算触发对局结束 / 已处于 GameOver·GameLose 时，
        // SaveRun 的 default 分支不写档——存档保持不动，只有开新游戏才作废。
        RunSaveManager.SaveRun();
    }

    void WinGame(bool isWin)
    {
        // 对局结束不清局内存档：存档只在开新游戏时作废（用户约定）。
        // 历史纪录由 DataSavingManager 落盘，与局内存档相互独立。

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
                    maxCalculationValue:roundMaxCalculationValue,
                    maxRound: currentRound,
                    difficultyLevel: DifficultySettingsManager.Instance != null
                        ? DifficultySettingsManager.Instance.GetCurrentDifficultyLevel() : 0
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
                // 按局内存档是否存在，显示/隐藏“继续游戏”按钮
                UIManager.Instance.UpdateContinueGameButton();
                Debug.Log("进入主菜单界面");
                break;

            case GameState.Shop:
                UIManager.Instance.ShowPanel(UIManager.Instance.shopPanel);
                Debug.Log("进入商店界面");
                break;
        }
    }

    #region 局内存档（RunSave）

    // 本局统计数据的存档访问器（原字段保持 private）
    public string RoundMaxNumberCardValue { get { return roundMaxNumberCardValue.ToString(); } set { roundMaxNumberCardValue = SavingData.StringToBigInteger(value); } }
    public long RoundMaxMultiplier { get { return roundMaxMultiplier; } set { roundMaxMultiplier = value; } }
    public string RoundMaxCalculationValue { get { return roundMaxCalculationValue.ToString(); } set { roundMaxCalculationValue = SavingData.StringToBigInteger(value); } }

    /// <summary>主菜单查询：是否有可继续的局内存档（供"继续游戏"按钮控制可用性）</summary>
    public bool HasContinueSave()
    {
        return RunSaveManager.HasValidSave();
    }

    /// <summary>主菜单"继续游戏"按钮入口（Inspector 绑定用）</summary>
    public void ContinueRun()
    {
        RunSaveManager.ContinueRun();
    }

    /// <summary>捕获全局进度到存档（由 RunSaveManager 编排调用）</summary>
    public void CaptureToSave(RunSaveData d)
    {
        d.gameMode = (int)currentGameMode;
        d.isInvolutionMode = GameManager.isInvolutionMode;
        d.isEndlessMode = GameManager.isEndlessMode;
        d.currentPoints = currentPoints.ToString();
        d.currentRound = currentRound;
        d.stageRequirement = stageRequirement.ToString();
        d.roundMaxNumberCardValue = roundMaxNumberCardValue.ToString();
        d.roundMaxMultiplier = roundMaxMultiplier;
        d.roundMaxCalculationValue = roundMaxCalculationValue.ToString();

        // 经验主义引用的"上一回合最大卡"（库存索引）
        d.lastRoundMaxInventoryIndex = lastRoundMaxCard != null && PlayerCardInventory.Instance != null
            ? PlayerCardInventory.Instance.numberCards.IndexOf(lastRoundMaxCard)
            : -1;
    }

    /// <summary>从存档恢复全局进度（界面恢复由 RestoreTurnUI / RunSaveManager 负责）</summary>
    public void RestoreFromSave(RunSaveData d)
    {
        currentGameMode = (GameMode)d.gameMode;
        GameManager.isInvolutionMode = d.isInvolutionMode;
        GameManager.isEndlessMode = d.isEndlessMode;
        currentPoints = SavingData.StringToBigInteger(d.currentPoints);
        currentRound = d.currentRound;
        stageRequirement = SavingData.StringToBigInteger(d.stageRequirement);
        roundMaxNumberCardValue = SavingData.StringToBigInteger(d.roundMaxNumberCardValue);
        roundMaxMultiplier = d.roundMaxMultiplier;
        roundMaxCalculationValue = SavingData.StringToBigInteger(d.roundMaxCalculationValue);

        if (d.lastRoundMaxInventoryIndex >= 0 &&
            PlayerCardInventory.Instance != null &&
            d.lastRoundMaxInventoryIndex < PlayerCardInventory.Instance.numberCards.Count)
        {
            lastRoundMaxCard = PlayerCardInventory.Instance.numberCards[d.lastRoundMaxInventoryIndex];
        }
        else
        {
            lastRoundMaxCard = null;
        }
    }

    /// <summary>
    /// 恢复游戏界面的回合中状态（点数/回合/手牌/公式卡/已放置卡牌）。
    /// 供"继续游戏"在 sceneState=0（未结算）与 sceneState=2（补完结算）时调用。
    /// restoreSlotPlacements=false（未结算存档）：本回合抽到的全部数字卡重新在手牌区生成，
    /// 公式槽位留空（卡牌不还原到槽位里）；true（结算检查点）：还原已填入公式的卡牌位置。
    /// </summary>
    public void RestoreTurnUI(bool restoreSlotPlacements = false)
    {
        UIManager.Instance.UpdatePointsDisplay(currentPoints);
        UIManager.Instance.UpdateRoundDisplay(currentRound);
        UIManager.Instance.ShowHandCards(cardManager.currentNumberCards);
        UIManager.Instance.ShowFormulaCard(cardManager.currentFormulaCard);
        UIManager.Instance.RefreshAllGameInfo();
        UIManager.Instance.ResetRoundScore();
        UIManager.Instance.UpdateTargetRoundDisplay(GetNextStageRound());
        UIManager.Instance.SetCardDragLocked(false);

        // 结算检查点：已拖入公式槽位的卡牌恢复视觉位置（数据层已在 CardManager 中恢复）
        if (restoreSlotPlacements)
        {
            RunSaveManager.RestorePlacedCardsVisual();
        }
    }

    /// <summary>
    /// "继续游戏"补完结算：恢复的是骰子/递增已定格的检查点状态，
    /// 跳过展示动画直接执行确定性结算（得分计算 → 加分 → 进入商店）。
    /// </summary>
    public void ResumePendingSettlement()
    {
        currentState = GameState.Calculation;

        // 与正常结算一致：结算期间锁定手牌
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetCardDragLocked(true);
        }

        // 恢复的是"骰子已定格"的检查点：不再投骰，直接从确定性计算开始补完结算
        ResetSettlementState();
        settlementDiceSettled = true;
        activeSettlementCoroutine = StartCoroutine(SettlementScoreSequence(cardManager, false));
    }

    #endregion

    //退出游戏
    public void ExitGame()
    {
        Application.Quit();
    }
}
