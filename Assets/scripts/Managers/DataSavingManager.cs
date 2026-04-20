using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

/// <summary>
/// 数据保存管理器 - 负责玩家数据的保存和加载
/// </summary>
public class DataSavingManager : MonoBehaviour
{
    public static DataSavingManager Instance { get; private set; }

    [Header("数据保存路径")]
    private string savePath;

    [Header("当前玩家数据")]
    private SavingData currentSavingData;

    // 用于显示本局数据的缓存
    private GameSessionStats currentSessionStats;

    /// <summary>
    /// 本局游戏统计数据
    /// </summary>
    public class GameSessionStats
    {
        public int gameMode;
        public BigInteger maxPoints;              // 本局最终点数
        public float maxMultiplier;               // 本局最高倍率
        public BigInteger maxNumberCardValue;     // 本局数字卡最大值
        public BigInteger maxCalculationValue;    // 本局单次结算最大值
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }

        // 设置保存路径
        savePath = Application.persistentDataPath + "/playerdata.json";
        Debug.Log($"数据保存路径：{savePath}");
    }

    private void Start()
    {
        // 游戏启动时加载数据
        LoadData();
    }

    /// <summary>
    /// 加载玩家数据
    /// </summary>
    public void LoadData()
    {
        if (System.IO.File.Exists(savePath))
        {
            try
            {
                string json = System.IO.File.ReadAllText(savePath);
                currentSavingData = JsonUtility.FromJson<SavingData>(json);
                Debug.Log("数据加载成功！");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"数据加载失败：{e.Message}，创建新存档");
                currentSavingData = new SavingData();
            }
        }
        else
        {
            // 第一次游戏，创建新数据
            currentSavingData = new SavingData();
            Debug.Log("首次游戏，创建新存档");
        }
    }

    /// <summary>
    /// 保存玩家数据到文件
    /// </summary>
    private void SaveDataToFile()
    {
        try
        {
            string json = JsonUtility.ToJson(currentSavingData, true);
            System.IO.File.WriteAllText(savePath, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"数据保存失败：{e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// 游戏胜利时调用 - 更新游戏数据
    /// 这是数据更新的唯一入口
    /// </summary>

    public void OnGameWin(int gameMode, BigInteger finalPoints, float maxMultiplier, int formulaCardCount, BigInteger numberCardMaxValue, BigInteger maxCalculationValue)
    {
        Debug.Log($"\n========== 游戏胜利，开始数据更新 ==========");
        Debug.Log($"最终点数：{finalPoints}");
        Debug.Log($"最大倍率：{maxMultiplier}x");
        Debug.Log($"最大数字卡值：{numberCardMaxValue}");

        // 保存本局数据用于显示
        currentSessionStats = new GameSessionStats
        {
            gameMode = gameMode,
            maxPoints = finalPoints,
            maxMultiplier = maxMultiplier,
            maxNumberCardValue = numberCardMaxValue,
            maxCalculationValue = maxCalculationValue
        };
        // 根据游戏模式更新对应的数据
        if (gameMode == 0)
        {
            UpdateNormalModeData(finalPoints, maxMultiplier, formulaCardCount, numberCardMaxValue);
        }
        else if (gameMode == 1)
        {
            UpdateInvolutionModeData(finalPoints, maxMultiplier, formulaCardCount, numberCardMaxValue);
        }

        // 更新通关次数
        currentSavingData.accomplishTimes++;
        Debug.Log($"通关次数 +1");

        // 更新全局最高点数 - 使用 BigInteger 比较
        BigInteger currentMaxPoint = SavingData.StringToBigInteger(currentSavingData.MaxPoint);
        if (finalPoints > currentMaxPoint)
        {
            currentSavingData.MaxPoint = finalPoints.ToString();
        }


        // 保存数据到文件
        SaveDataToFile();

        Debug.Log("========== 数据更新完成 ==========\n");
    }

    /// <summary>
    /// 更新普通模式数据
    /// 使用 BigInteger 进行比较，确保精度不丢失
    /// </summary>
    private void UpdateNormalModeData(BigInteger finalPoints, float maxMultiplier, int formulaCardCount, BigInteger numberCardMaxValue)
    {
        Debug.Log("\n【普通模式数据更新】");

        // 1. 比较和更新最大点数
        BigInteger currentMaxPointsN = SavingData.StringToBigInteger(currentSavingData.TotalPointsN);
        if (finalPoints > currentMaxPointsN)
        {
            currentSavingData.TotalPointsN = finalPoints.ToString();
            Debug.Log($"更新普通模式最大点数：{currentSavingData.TotalPointsN}");
        }
        else
        {
            Debug.Log($"普通模式最大点数未更新（当前：{finalPoints}，历史：{currentMaxPointsN}）");
        }

        // 2. 比较和更新最大倍率
        if (maxMultiplier > currentSavingData.RateN)
        {
            currentSavingData.RateN = maxMultiplier;
            Debug.Log($"更新普通模式最大倍率：{currentSavingData.RateN}x");
        }
        else
        {
            Debug.Log($"普通模式最大倍率未更新（当前：{maxMultiplier}，历史：{currentSavingData.RateN}）");
        }

        // 3. 比较和更新最大数字卡点数
        BigInteger currentMaxNumbercardPointN = SavingData.StringToBigInteger(currentSavingData.NumbercardPointN);
        if (numberCardMaxValue > currentMaxNumbercardPointN)
        {
            currentSavingData.NumbercardPointN = numberCardMaxValue.ToString();
            Debug.Log($"更新普通模式最大数字卡点数：{currentSavingData.NumbercardPointN}");
        }
        else
        {
            Debug.Log($"普通模式最大数字卡未更新（当前：{numberCardMaxValue}，历史：{currentMaxNumbercardPointN}）");
        }

        // 4. 比较和更新最大结算点数（使用最终点数作为结算值）
        BigInteger currentMaxCalculationPointN = SavingData.StringToBigInteger(currentSavingData.CalculationPointN);
        if (finalPoints > currentMaxCalculationPointN)
        {
            currentSavingData.CalculationPointN = finalPoints.ToString();
            Debug.Log($"更新普通模式最大结算点数：{currentSavingData.CalculationPointN}");
        }
        else
        {
            Debug.Log($"普通模式最大结算点未更新（当前：{finalPoints}，历史：{currentMaxCalculationPointN}）");
        }
    }

    /// <summary>
    /// 更新内卷模式数据
    /// 使用 BigInteger 进行比较，确保精度不丢失
    /// </summary>
    private void UpdateInvolutionModeData(BigInteger finalPoints, float maxMultiplier, int formulaCardCount, BigInteger numberCardMaxValue)
    {
        Debug.Log("\n【内卷模式数据更新】");

        // 1. 比较和更新最大点数
        BigInteger currentMaxPointsI = SavingData.StringToBigInteger(currentSavingData.TotalPointsI);
        if (finalPoints > currentMaxPointsI)
        {
            currentSavingData.TotalPointsI = finalPoints.ToString();
            Debug.Log($" 更新内卷模式最大点数：{currentSavingData.TotalPointsI}");
        }
        else
        {
            Debug.Log($"内卷模式最大点数未更新（当前：{finalPoints}，历史：{currentMaxPointsI}）");
        }

        // 2. 比较和更新最大倍率
        if (maxMultiplier > currentSavingData.RateI)
        {
            currentSavingData.RateI = maxMultiplier;
            Debug.Log($"更新内卷模式最大倍率：{currentSavingData.RateI}x");
        }
        else
        {
            Debug.Log($"内卷模式最大倍率未更新（当前：{maxMultiplier}，历史：{currentSavingData.RateI}）");
        }

        // 3. 比较和更新最大数字卡点数
        BigInteger currentMaxNumbercardPointI = SavingData.StringToBigInteger(currentSavingData.NumbercardPointI);
        if (numberCardMaxValue > currentMaxNumbercardPointI)
        {
            currentSavingData.NumbercardPointI = numberCardMaxValue.ToString();
            Debug.Log($"更新内卷模式最大数字卡点数：{currentSavingData.NumbercardPointI}");
        }
        else
        {
            Debug.Log($"内卷模式最大数字卡未更新（当前：{numberCardMaxValue}，历史：{currentMaxNumbercardPointI}）");
        }

        // 4. 比较和更新最大结算点数（使用最终点数作为结算值）
        BigInteger currentMaxCalculationPointI = SavingData.StringToBigInteger(currentSavingData.CalculationPointI);
        if (finalPoints > currentMaxCalculationPointI)
        {
            currentSavingData.CalculationPointI = finalPoints.ToString();
            Debug.Log($"更新内卷模式最大结算点数：{currentSavingData.CalculationPointI}");
        }
        else
        {
            Debug.Log($"内卷模式最大结算点未更新（当前：{finalPoints}，历史：{currentMaxCalculationPointI}）");
        }
    }

    /// <summary>
    /// 获取当前保存的数据
    /// </summary>
    public SavingData GetCurrentData()
    {
        if (currentSavingData == null)
        {
            Debug.LogWarning("当前数据为空，创建新实例");
            currentSavingData = new SavingData();
        }
        return currentSavingData;
    }

    /// <summary>
    /// 获取指定模式的最高点数
    /// 返回 BigInteger 类型，避免精度丢失
    /// </summary>
    public BigInteger GetHighestScore(int gameMode)
    {
        if (gameMode == 0)
            return SavingData.StringToBigInteger(currentSavingData.TotalPointsN);
        else
            return SavingData.StringToBigInteger(currentSavingData.TotalPointsI);
    }

    /// <summary>
    /// 获取全局最高分
    /// 返回 BigInteger 类型
    /// </summary>
    public BigInteger GetGlobalHighestScore()
    {
        return SavingData.StringToBigInteger(currentSavingData.MaxPoint);
    }

    /// <summary>
    /// 获取通关次数
    /// </summary>
    public int GetAccomplishTimes()
    {
        return currentSavingData.accomplishTimes;
    }

    /// <summary>
    /// 清除所有数据（谨慎使用！）
    /// </summary>
    public void ClearAllData()
    {
        try
        {
            if (System.IO.File.Exists(savePath))
            {
                System.IO.File.Delete(savePath);
                currentSavingData = new SavingData();
                Debug.LogWarning(" 所有数据已清除");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"清除数据失败：{e.Message}");
        }
    }

    /// <summary>
    /// 调试方法：获取存档文件路径
    /// </summary>
    public string GetSavePath()
    {
        return savePath;
    }
}