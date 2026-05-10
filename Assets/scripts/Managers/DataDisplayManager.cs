using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 数据显示管理器 - 将 DataSavingManager 的数据显示在 UI 上
/// 负责实时更新游戏统计数据的显示
/// </summary>
public class DataDisplayManager : MonoBehaviour
{
    public static DataDisplayManager Instance { get; private set; }

    [Header("普通模式数据显示 ==========")]
    [SerializeField] private Text normalModeHighScoreText;      // 普通模式最高分
    [SerializeField] private Text normalModeHighRateText;       // 普通模式最高倍率
    [SerializeField] private Text normalModeMaxCardText;        // 普通模式最高数字卡
    [SerializeField] private Text normalModeMaxCalculationText; // 普通模式最高结算点

    [Header("内卷模式数据显示 ==========")]
    [SerializeField] private Text hardModeHighScoreText;        // 内卷模式最高分
    [SerializeField] private Text hardModeHighRateText;         // 内卷模式最高倍率
    [SerializeField] private Text hardModeMaxCardText;          // 内卷模式最高数字卡
    [SerializeField] private Text hardModeMaxCalculationText;   // 内卷模式最高结算点

    [Header("全局统计数据显示 ==========")]
    [SerializeField] private Text globalHighScoreText;          // 全局最高分
    [SerializeField] private Text totalWinsText;                // 总通关次数

    [Header("本局统计数据显示 ==========")]
    [SerializeField] private GameObject currentGameStatsPanel;   // 本局数据面板
    [SerializeField] private Text currentMaxScoreText;          // 本局最终点数
    [SerializeField] private Text currentMaxRateText;           // 本局最高倍率
    [SerializeField] private Text currentMaxCardText;           // 本局最大数字卡
    [SerializeField] private Text currentMaxCalculationText;    // 本局最高结算点

    [Header("显示格式设置 ==========")]
    [SerializeField] private bool autoRefresh = true;           // 是否自动刷新
    [SerializeField] private float refreshInterval = 1f;        // 自动刷新间隔（秒）

    public GameObject PlayerDataPanel; // 玩家数据面板

    private float refreshTimer = 0f;
    private GameManager gameManager;
    private DataSavingManager dataSavingManager;

    private void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 获取必要的管理器引用
        gameManager = GameManager.Instance;
        dataSavingManager = DataSavingManager.Instance;

        if (dataSavingManager == null)
        {
            Debug.LogError("DataSavingManager 不存在！请确保场景中有 DataSavingManager");
            return;
        }

        // 第一次初始化显示
        RefreshAllDisplay();
    }
    private void Update()
    {
        if (!autoRefresh) return;

        refreshTimer += Time.deltaTime;
        if (refreshTimer >= refreshInterval)
        {
            RefreshAllDisplay();
            refreshTimer = 0f;
        }
    }

    /// <summary>
    /// 刷新所有 UI 显示
    /// 在游戏胜利、数据更新等关键时刻调用
    /// </summary>
    public void RefreshAllDisplay()
    {
        if (dataSavingManager == null) return;

        // 刷新历史数据
        RefreshHistoryData();

    }
    /// <summary>
    /// 刷新历史数据显示
    /// </summary>
    private void RefreshHistoryData()
    {
        SavingData data = dataSavingManager.GetCurrentData();

        // ===== 普通模式数据 =====
        if (normalModeHighScoreText != null)
        {
            string scoreDisplay = data.TotalPointsN == "0"
                ? "未开始"
                : data.TotalPointsN;
            normalModeHighScoreText.text = $"{FormatStringToNiceDisplay(scoreDisplay)}";
        }

        if (normalModeHighRateText != null)
        {
            normalModeHighRateText.text = $"{data.RateN}";
        }

        if (normalModeMaxCardText != null)
        {
            string cardDisplay = data.NumbercardPointN == "0"
                ? "未开始"
                : data.NumbercardPointN;
            normalModeMaxCardText.text = $"{FormatStringToNiceDisplay(cardDisplay)}";
        }

        if (normalModeMaxCalculationText != null)
        {
            string calcDisplay = data.CalculationPointN == "0"
                ? "未开始"
                : data.CalculationPointN;
            normalModeMaxCalculationText.text = $"{FormatStringToNiceDisplay(calcDisplay)}";
        }

        // ===== 内卷模式数据 =====
        if (hardModeHighScoreText != null)
        {
            string scoreDisplay = data.TotalPointsI == "0"
                ? "未开始"
                : data.TotalPointsI;
            hardModeHighScoreText.text = $"{FormatStringToNiceDisplay(scoreDisplay)}";
        }

        if (hardModeHighRateText != null)
        {
            hardModeHighRateText.text = $"{data.RateI}";
        }

        if (hardModeMaxCardText != null)
        {
            string cardDisplay = data.NumbercardPointI == "0"
                ? "未开始"
                : data.NumbercardPointI;
            hardModeMaxCardText.text = $"{FormatStringToNiceDisplay(cardDisplay)}";
        }

        if (hardModeMaxCalculationText != null)
        {
            string calcDisplay = data.CalculationPointI == "0"
                ? "未开始"
                : data.CalculationPointI;
            hardModeMaxCalculationText.text = $"{FormatStringToNiceDisplay(calcDisplay)}";
        }

        // ===== 全局统计数据 =====
        if (globalHighScoreText != null)
        {
            string scoreDisplay = data.MaxPoint == "0"
                ? "0"
                : data.MaxPoint
                ;
            globalHighScoreText.text = $"{FormatStringToNiceDisplay(scoreDisplay)}";
        }

        if (totalWinsText != null)
        {
            totalWinsText.text = $"{data.accomplishTimes}次";
        }
    }

    /// <summary>
    /// \显示本局游戏的统计数据
    /// 在游戏胜利后调用此方法
    /// 逻辑和历史数据显示相同，用于展示本局的最大数据
    /// </summary>
    public void ShowCurrentGameStats(int gameMode, BigInteger maxPoints, float maxMultiplier, BigInteger maxNumberCard, BigInteger maxCalculationValue)
    {
        // 显示本局数据面板
        if (currentGameStatsPanel != null)
        {
            currentGameStatsPanel.SetActive(true);
        }

        // 显示本局最终点数
        if (currentMaxScoreText != null)
        {
            currentMaxScoreText.text = $"{FormatToNiceDisplay(maxPoints)}";
        }

        // 显示本局最高倍率
        if (currentMaxRateText != null)
        {
            currentMaxRateText.text = $"{maxMultiplier}";
        }

        // 显示本局最大数字卡
        if (currentMaxCardText != null)
        {
            currentMaxCardText.text = $"{FormatToNiceDisplay(maxNumberCard)}";
        }

        // 显示本局最高结算点
        if (currentMaxCalculationText != null)
        {
            currentMaxCalculationText.text = $"{FormatToNiceDisplay(maxCalculationValue)}";
        }

    }

    /// <summary>
    /// 手动刷新显示（在需要时调用）
    /// 比如游戏结束时调用此方法立即更新 UI
    /// </summary>
    public void ManualRefresh()
    {
        RefreshAllDisplay();
    }

    /// <summary>
    /// 仅刷新历史数据（不刷新当前游戏数据）
    /// 在游戏胜利后使用
    /// </summary>
    public void RefreshHistoryDataOnly()
    {
        RefreshHistoryData();
    }
    /// <summary>
    /// 科学计数法
    /// </summary>
    // 大于9位 → 8.00e12 格式，小于等于9位正常显示
    public static string FormatToNiceDisplay(BigInteger number)
    {
        if (number < 1000000000)
            return number.ToString();

        string s = number.ToString();
        int exponent = s.Length - 1;

        char c0 = s[0];
        char c1 = s.Length > 1 ? s[1] : '0';
        char c2 = s.Length > 2 ? s[2] : '0';
        char c3 = s.Length > 3 ? s[3] : '0';

        return $"{c0}.{c1}{c2}{c3}e{exponent}";
    }

        // 字符串版本（存档用）
        public static string FormatStringToNiceDisplay(string numStr)
    {
        if (string.IsNullOrEmpty(numStr) || numStr == "0")
            return "0";

        if (BigInteger.TryParse(numStr, out BigInteger result))
            return FormatToNiceDisplay(result);

        return numStr;
    }
}