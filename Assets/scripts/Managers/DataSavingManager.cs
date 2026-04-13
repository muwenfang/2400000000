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
    /// 保存玩家数据
    /// </summary>
    private void SaveDataToFile()
    {
        try
        {
            string json = JsonUtility.ToJson(currentSavingData, true);
            System.IO.File.WriteAllText(savePath, json);
            Debug.Log("数据保存成功！");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"数据保存失败：{e.Message}");
        }
    }

    /// <summary>
    /// 游戏胜利时调用 - 更新游戏数据
    /// </summary>
    /// <param name="gameMode">游戏模式（0=普通模式，1=内卷模式）</param>
    /// <param name="finalPoints">最终点数</param>
    /// <param name="maxMultiplier">本局最大倍率</param>
    /// <param name="formulaCardCount">拥有的公式卡数量</param>
    /// <param name="numberCardMaxValue">本局数字卡最大值</param>
    public void OnGameWin(int gameMode, BigInteger finalPoints, float maxMultiplier, int formulaCardCount, BigInteger numberCardMaxValue)
    {
        Debug.Log($"=== 游戏胜利，更新数据 ===");
        Debug.Log($"游戏模式：{(gameMode == 0 ? "普通" : "内卷")}");
        Debug.Log($"最终点数：{finalPoints}");

        if (gameMode == 0) // 普通模式
        {
            UpdateNormalModeData(finalPoints, maxMultiplier, formulaCardCount, numberCardMaxValue);
        }
        else if (gameMode == 1) // 内卷模式
        {
            UpdateHardModeData(finalPoints, maxMultiplier, formulaCardCount, numberCardMaxValue);
        }

        // 增加通关次数
        currentSavingData.accomplishTimes++;

        // 更新最大点数（全局）
        if (finalPoints > new BigInteger(currentSavingData.MaxPoint))
        {
            currentSavingData.MaxPoint = int.Parse(finalPoints.ToString().Length > 9 ? "999999999" : finalPoints.ToString());
            Debug.Log($"更新全局最大点数：{currentSavingData.MaxPoint}");
        }

        // 保存数据
        SaveDataToFile();

        Debug.Log("=== 数据更新完成 ===");
    }

    /// <summary>
    /// 更新普通模式数据
    /// </summary>
    private void UpdateNormalModeData(BigInteger finalPoints, float maxMultiplier, int formulaCardCount, BigInteger numberCardMaxValue)
    {
        // 更新最大点数
        if (finalPoints > currentSavingData.TotalPointsN)
        {
            currentSavingData.TotalPointsN = finalPoints;
            Debug.Log($"普通模式最大点数：{currentSavingData.TotalPointsN}");
        }

        // 更新最大倍率
        if (maxMultiplier > currentSavingData.RateN)
        {
            currentSavingData.RateN = maxMultiplier;
            Debug.Log($"普通模式最大倍率：{currentSavingData.RateN}");
        }

        // 更新最大数字卡点数
        if (numberCardMaxValue > currentSavingData.NumbercardPointN)
        {
            currentSavingData.NumbercardPointN = numberCardMaxValue;
            Debug.Log($"普通模式最大数字卡点数：{currentSavingData.NumbercardPointN}");
        }

        // 更新最大结算点数（这里可以是最终点数或其他计算值）
        if (finalPoints > currentSavingData.CalculationPointN)
        {
            currentSavingData.CalculationPointN = finalPoints;
            Debug.Log($"普通模式最大结算点数：{currentSavingData.CalculationPointN}");
        }
    }

    /// <summary>
    /// 更新内卷模式数据
    /// </summary>
    private void UpdateHardModeData(BigInteger finalPoints, float maxMultiplier, int formulaCardCount, BigInteger numberCardMaxValue)
    {
        // 更新最大点数
        if (finalPoints > currentSavingData.TotalPointsI)
        {
            currentSavingData.TotalPointsI = finalPoints;
            Debug.Log($"内卷模式最大点数：{currentSavingData.TotalPointsI}");
        }

        // 更新最大倍率
        if (maxMultiplier > currentSavingData.RateI)
        {
            currentSavingData.RateI = maxMultiplier;
            Debug.Log($"内卷模式最大倍率：{currentSavingData.RateI}");
        }

        // 更新最大数字卡点数
        if (numberCardMaxValue > currentSavingData.NumbercardPointI)
        {
            currentSavingData.NumbercardPointI = numberCardMaxValue;
            Debug.Log($"内卷模式最大数字卡点数：{currentSavingData.NumbercardPointI}");
        }

        // 更新最大结算点数
        if (finalPoints > currentSavingData.CalculationPointI)
        {
            currentSavingData.CalculationPointI = finalPoints;
            Debug.Log($"内卷模式最大结算点数：{currentSavingData.CalculationPointI}");
        }
    }

    /// <summary>
    /// 获取当前保存的数据
    /// </summary>
    public SavingData GetCurrentData()
    {
        return currentSavingData;
    }

    /// <summary>
    /// 获取指定模式的最高点数
    /// </summary>
    public BigInteger GetHighestScore(int gameMode)
    {
        if (gameMode == 0)
            return currentSavingData.TotalPointsN;
        else
            return currentSavingData.TotalPointsI;
    }

    /// <summary>
    /// 获取通关次数
    /// </summary>
    public int GetAccomplishTimes()
    {
        return currentSavingData.accomplishTimes;
    }

    /// <summary>
    /// 清除所有数据（谨慎使用）
    /// </summary>
    public void ClearAllData()
    {
        if (System.IO.File.Exists(savePath))
        {
            System.IO.File.Delete(savePath);
            currentSavingData = new SavingData();
            Debug.Log("所有数据已清除");
        }
    }
}