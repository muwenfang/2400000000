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

    [Header("显示格式设置 ==========")]
    [SerializeField] private bool autoRefresh = true;           // 是否自动刷新
    [SerializeField] private float refreshInterval = 1f;        // 自动刷新间隔（秒）

    private float refreshTimer = 0f;
    private GameManager gameManager;
    private DataSavingManager dataSavingManager;

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
            normalModeHighScoreText.text = $"最高分：{scoreDisplay}";
        }

        if (normalModeHighRateText != null)
        {
            normalModeHighRateText.text = $"最高倍率：{data.RateN}x";
        }

        if (normalModeMaxCardText != null)
        {
            string cardDisplay = data.NumbercardPointN == "0"
                ? "未开始"
                : data.NumbercardPointN;
            normalModeMaxCardText.text = $"最高数字卡：{cardDisplay}";
        }

        if (normalModeMaxCalculationText != null)
        {
            string calcDisplay = data.CalculationPointN == "0"
                ? "未开始"
                : data.CalculationPointN;
            normalModeMaxCalculationText.text = $"最高结算：{calcDisplay}";
        }

        // ===== 内卷模式数据 =====
        if (hardModeHighScoreText != null)
        {
            string scoreDisplay = data.TotalPointsI == "0"
                ? "未开始"
                : data.TotalPointsI;
            hardModeHighScoreText.text = $"最高分：{scoreDisplay}";
        }

        if (hardModeHighRateText != null)
        {
            hardModeHighRateText.text = $"最高倍率：{data.RateI}x";
        }

        if (hardModeMaxCardText != null)
        {
            string cardDisplay = data.NumbercardPointI == "0"
                ? "未开始"
                : data.NumbercardPointI;
            hardModeMaxCardText.text = $"最高数字卡：{cardDisplay}";
        }

        if (hardModeMaxCalculationText != null)
        {
            string calcDisplay = data.CalculationPointI == "0"
                ? "未开始"
                : data.CalculationPointI;
            hardModeMaxCalculationText.text = $"最高结算：{calcDisplay}";
        }

        // ===== 全局统计数据 =====
        if (globalHighScoreText != null)
        {
            string scoreDisplay = data.MaxPoint == "0"
                ? "0"
                : data.MaxPoint
                ;
            globalHighScoreText.text = $"全局最高：{scoreDisplay}";
        }

        if (totalWinsText != null)
        {
            totalWinsText.text = $"总通关：{data.accomplishTimes}次";
        }
    }

    ///// <summary>
    ///// 格式化大数字显示
    ///// 999 → 999
    ///// 1,000 → 1K
    ///// 1,000,000 → 1M
    ///// 1,000,000,000 → 1B
    ///// 1,000,000,000,000 → 1T
    ///// </summary>
    //private string FormatNumber(string numberStr)
    //{
    //    if (!useShortFormat)
    //    {
    //        return numberStr;
    //    }

    //    if (!BigInteger.TryParse(numberStr, out BigInteger num))
    //    {
    //        return numberStr;
    //    }

    //    if (num == 0)
    //        return "0";

    //    // 定义单位阈值
    //    BigInteger trillion = BigInteger.Parse("1000000000000");
    //    BigInteger billion = BigInteger.Parse("1000000000");
    //    BigInteger million = BigInteger.Parse("1000000");
    //    BigInteger thousand = BigInteger.Parse("1000");

    //    if (num >= trillion)
    //    {
    //        decimal value = (decimal)num / (decimal)trillion;
    //        return value.ToString("F2") + "T";
    //    }
    //    else if (num >= billion)
    //    {
    //        decimal value = (decimal)num / (decimal)billion;
    //        return value.ToString("F2") + "B";
    //    }
    //    else if (num >= million)
    //    {
    //        decimal value = (decimal)num / (decimal)million;
    //        return value.ToString("F2") + "M";
    //    }
    //    else if (num >= thousand)
    //    {
    //        decimal value = (decimal)num / (decimal)thousand;
    //        return value.ToString("F2") + "K";
    //    }
    //    else
    //    {
    //        return num.ToString();
    //    }
    //}

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

   
}