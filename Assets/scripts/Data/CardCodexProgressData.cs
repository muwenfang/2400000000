using System;
using System.Collections.Generic;

/// <summary>
/// 图鉴存档的数据载体（就是 cardcodex.json 的结构）。
///
/// 与玩家存档 playerdata.json 完全独立：
///  · 图鉴是"永不重置"的永久收集进度，不能被"重置玩家数据"、开新局、胜负结算影响；
///  · 独立文件也避免了玩家存档整体替换时图鉴被动清空的风险（曾经的缺陷）。
/// </summary>
[Serializable]
public class CardCodexSaveData
{
    /// <summary>存档结构版本（后续若改字段含义，用它做兼容判断）</summary>
    public int version = CardCodexProgress.CurrentVersion;

    /// <summary>已解锁数字卡：NumberCardData.name（资产名，工程内唯一）</summary>
    public List<string> numberCards = new List<string>();

    /// <summary>已解锁填空卡：FormulaCardData.name（资产名，工程内唯一）</summary>
    public List<string> formulaCards = new List<string>();

    /// <summary>已解锁祝福：blessingId</summary>
    public List<int> blessings = new List<int>();
}

/// <summary>
/// 图鉴解锁进度的内存模型（纯 C#，不触碰 Unity API，便于离线回归测试）。
///
/// 身份标识为什么用资产名而不是 cardName：
///  · cardName 存在重复（123/124 都叫 0^$50$；118/120 都是 10$5$），不能当唯一标识；
///  · 资产名（SO.name / 文件名）在工程内唯一，且局内存档（RunSaveManager）已采用同一套标识。
///
/// 解锁一旦写入永不撤销（重复获得不会重复解锁，也不会因为删卡而回退）。
/// </summary>
public class CardCodexProgress
{
    /// <summary>当前存档结构版本</summary>
    public const int CurrentVersion = 1;

    private readonly HashSet<string> numberCards = new HashSet<string>(StringComparer.Ordinal);
    private readonly HashSet<string> formulaCards = new HashSet<string>(StringComparer.Ordinal);
    private readonly HashSet<int> blessings = new HashSet<int>();

    public int NumberCardCount { get { return numberCards.Count; } }
    public int FormulaCardCount { get { return formulaCards.Count; } }
    public int BlessingCount { get { return blessings.Count; } }
    public int TotalCount { get { return numberCards.Count + formulaCards.Count + blessings.Count; } }

    /// <summary>从独立图鉴存档构建进度模型（字段为 null 时统一兜底）</summary>
    public static CardCodexProgress FromData(CardCodexSaveData data)
    {
        CardCodexProgress progress = new CardCodexProgress();
        if (data == null) return progress;

        AddAll(progress.numberCards, data.numberCards);
        AddAll(progress.formulaCards, data.formulaCards);

        if (data.blessings != null)
        {
            for (int i = 0; i < data.blessings.Count; i++)
            {
                progress.blessings.Add(data.blessings[i]);
            }
        }

        return progress;
    }

    /// <summary>
    /// 从旧版 playerdata.json 的三张表读取（仅用于一次性迁移到 cardcodex.json）。
    /// 迁移完成后新代码不再写这三张表，"重置玩家数据"也不会影响图鉴。
    /// </summary>
    public static CardCodexProgress FromLegacy(SavingData save)
    {
        CardCodexProgress progress = new CardCodexProgress();
        if (save == null) return progress;

        AddAll(progress.numberCards, save.unlockedNumberCards);
        AddAll(progress.formulaCards, save.unlockedFormulaCards);

        if (save.unlockedBlessings != null)
        {
            for (int i = 0; i < save.unlockedBlessings.Count; i++)
            {
                progress.blessings.Add(save.unlockedBlessings[i]);
            }
        }

        return progress;
    }

    private static void AddAll(HashSet<string> target, List<string> source)
    {
        if (source == null) return;

        for (int i = 0; i < source.Count; i++)
        {
            string key = source[i];
            if (string.IsNullOrEmpty(key)) continue;
            target.Add(key);
        }
    }

    /// <summary>
    /// 把内存中的解锁状态写回存档载体（写回按字典序/升序排列，保证同进度产生同一份文件内容）。
    /// </summary>
    public void WriteTo(CardCodexSaveData data)
    {
        if (data == null) return;

        if (data.numberCards == null) data.numberCards = new List<string>();
        if (data.formulaCards == null) data.formulaCards = new List<string>();
        if (data.blessings == null) data.blessings = new List<int>();
        data.version = CurrentVersion;

        data.numberCards.Clear();
        List<string> numbers = new List<string>(numberCards);
        numbers.Sort(StringComparer.Ordinal);
        data.numberCards.AddRange(numbers);

        data.formulaCards.Clear();
        List<string> formulas = new List<string>(formulaCards);
        formulas.Sort(StringComparer.Ordinal);
        data.formulaCards.AddRange(formulas);

        data.blessings.Clear();
        List<int> ids = new List<int>(blessings);
        ids.Sort();
        data.blessings.AddRange(ids);
    }

    // ================= 解锁 =================
    // 返回 true 表示"本次才解锁"（重复解锁返回 false，调用方不需要重复落盘）。

    public bool UnlockNumberCard(string assetName)
    {
        if (string.IsNullOrEmpty(assetName)) return false;
        return numberCards.Add(assetName);
    }

    public bool UnlockFormulaCard(string assetName)
    {
        if (string.IsNullOrEmpty(assetName)) return false;
        return formulaCards.Add(assetName);
    }

    public bool UnlockBlessing(int blessingId)
    {
        return blessings.Add(blessingId);
    }

    // ================= 查询 =================

    public bool IsNumberCardUnlocked(string assetName)
    {
        if (string.IsNullOrEmpty(assetName)) return false;
        return numberCards.Contains(assetName);
    }

    public bool IsFormulaCardUnlocked(string assetName)
    {
        if (string.IsNullOrEmpty(assetName)) return false;
        return formulaCards.Contains(assetName);
    }

    public bool IsBlessingUnlocked(int blessingId)
    {
        return blessings.Contains(blessingId);
    }

    /// <summary>清空全部解锁进度（调试用，正常游戏流程不会调用）</summary>
    public void Clear()
    {
        numberCards.Clear();
        formulaCards.Clear();
        blessings.Clear();
    }
}
