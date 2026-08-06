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

        NormalizeSettingsData();
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

    private void NormalizeSettingsData()
    {
        if (currentSavingData == null)
        {
            currentSavingData = new SavingData();
        }

        currentSavingData.numberCardPriceDifficultyMultiplier =
            NormalizeDifficultyMultiplier(currentSavingData.numberCardPriceDifficultyMultiplier);
        currentSavingData.formulaCardPriceDifficultyMultiplier =
            NormalizeDifficultyMultiplier(currentSavingData.formulaCardPriceDifficultyMultiplier);
        currentSavingData.blessingPriceDifficultyMultiplier =
            NormalizeDifficultyMultiplier(currentSavingData.blessingPriceDifficultyMultiplier);
        currentSavingData.cardDeletionPriceDifficultyMultiplier =
            NormalizeDifficultyMultiplier(currentSavingData.cardDeletionPriceDifficultyMultiplier);
        currentSavingData.shopRefreshPriceDifficultyMultiplier =
            NormalizeDifficultyMultiplier(currentSavingData.shopRefreshPriceDifficultyMultiplier);
    }

    private float NormalizeDifficultyMultiplier(float value)
    {
        if (value <= 0f)
        {
            value = 1f;
        }

        value = Mathf.Clamp(value, 1f, 10f);
        return Mathf.Round(value * 2f) / 2f;
    }

    /// <summary>
    /// 游戏胜利时调用 - 更新游戏数据
    /// 这是数据更新的唯一入口
    /// </summary>

    public void OnGameWin(int gameMode, BigInteger finalPoints, float maxMultiplier, int formulaCardCount, BigInteger numberCardMaxValue, BigInteger maxCalculationValue, int maxRound = 0, int difficultyLevel = 0)
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

            // 更新通关最高难度数据（仅普通模式）
            if (difficultyLevel > 0)
            {
                BigInteger currentMaxDiffPoints = SavingData.StringToBigInteger(currentSavingData.MaxDifficultyPoints);
                if (difficultyLevel > currentSavingData.MaxDifficultyLevel
                    || (difficultyLevel == currentSavingData.MaxDifficultyLevel && finalPoints > currentMaxDiffPoints))
                {
                    currentSavingData.MaxDifficultyLevel = difficultyLevel;
                    currentSavingData.MaxDifficultyPoints = finalPoints.ToString();
                    Debug.Log($"更新通关最高难度：Lv.{difficultyLevel}，点数 {finalPoints}");
                }
            }
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
    /// 无尽模式结束时调用 - 更新无尽模式数据
    /// </summary>
    public void OnEndlessGameOver(int round, int difficultyLevel)
    {
        Debug.Log($"无尽模式结束 - 回合数：{round}，难度等级：{difficultyLevel}");

        // 更新无尽模式最高回合数
        if (round > currentSavingData.EndlessMaxRound)
        {
            currentSavingData.EndlessMaxRound = round;
            Debug.Log($"更新无尽模式最高回合数：{round}");
        }

        // 更新无尽模式最高难度对应回合数
        if (difficultyLevel > 0)
        {
            if (difficultyLevel > currentSavingData.EndlessMaxDifficultyLevel)
            {
                currentSavingData.EndlessMaxDifficultyLevel = difficultyLevel;
                currentSavingData.EndlessMaxDifficultyRound = round;
                Debug.Log($"更新无尽模式最高难度：Lv.{difficultyLevel}，回合数：{round}");
            }
            else if (difficultyLevel == currentSavingData.EndlessMaxDifficultyLevel
                     && round > currentSavingData.EndlessMaxDifficultyRound)
            {
                currentSavingData.EndlessMaxDifficultyRound = round;
                Debug.Log($"更新无尽模式同难度下最高回合数：{round}");
            }
        }

        SaveDataToFile();
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
            NormalizeSettingsData();
        }
        return currentSavingData;
    }

    public float GetDifficultyMultiplier(DifficultySettingType settingType)
    {
        SavingData data = GetCurrentData();

        switch (settingType)
        {
            case DifficultySettingType.NumberCardPrice:
                return data.numberCardPriceDifficultyMultiplier;
            case DifficultySettingType.FormulaCardPrice:
                return data.formulaCardPriceDifficultyMultiplier;
            case DifficultySettingType.BlessingPrice:
                return data.blessingPriceDifficultyMultiplier;
            case DifficultySettingType.CardDeletionPrice:
                return data.cardDeletionPriceDifficultyMultiplier;
            case DifficultySettingType.ShopRefreshPrice:
                return data.shopRefreshPriceDifficultyMultiplier;
            default:
                return 1f;
        }
    }

    public void SetDifficultyMultiplier(DifficultySettingType settingType, float multiplier)
    {
        float normalizedValue = NormalizeDifficultyMultiplier(multiplier);

        SavingData data = GetCurrentData();
        float currentValue = GetDifficultyMultiplier(settingType);

        if (Mathf.Approximately(currentValue, normalizedValue))
        {
            return;
        }

        switch (settingType)
        {
            case DifficultySettingType.NumberCardPrice:
                data.numberCardPriceDifficultyMultiplier = normalizedValue;
                break;
            case DifficultySettingType.FormulaCardPrice:
                data.formulaCardPriceDifficultyMultiplier = normalizedValue;
                break;
            case DifficultySettingType.BlessingPrice:
                data.blessingPriceDifficultyMultiplier = normalizedValue;
                break;
            case DifficultySettingType.CardDeletionPrice:
                data.cardDeletionPriceDifficultyMultiplier = normalizedValue;
                break;
            case DifficultySettingType.ShopRefreshPrice:
                data.shopRefreshPriceDifficultyMultiplier = normalizedValue;
                break;
        }

        SaveDataToFile();
        Debug.Log($"难度设置已保存：{settingType} = {normalizedValue:F1}x");
    }

    public float GetNumberCardPriceDifficultyMultiplier()
    {
        return GetDifficultyMultiplier(DifficultySettingType.NumberCardPrice);
    }

    public void SetNumberCardPriceDifficultyMultiplier(float multiplier)
    {
        SetDifficultyMultiplier(DifficultySettingType.NumberCardPrice, multiplier);
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
    /// Reset saved player data only.
    /// </summary>
    public void InitializePlayerData()
    {
        currentSavingData = new SavingData();
        NormalizeSettingsData();
        SaveDataToFile();

        if (DataDisplayManager.Instance != null)
        {
            DataDisplayManager.Instance.ManualRefresh();
        }

        Debug.Log("Player save data has been initialized.");
    }

    /// <summary>
    /// UI button entry point.
    /// </summary>
    public void InitializePlayerDataButton()
    {
        InitializePlayerData();
    }

    /// <summary>
    /// 调试方法：获取存档文件路径
    /// </summary>
    public string GetSavePath()
    {
        return savePath;
    }
}
