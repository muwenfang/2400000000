using System;
using System.Collections.Generic;

/// <summary>
/// 成就存档的数据载体（就是 achievements.json 的结构）。
///
/// 与玩家存档 playerdata.json 完全独立（同 cardcodex.json 的做法）：
///  · 成就是"永不重置"的永久进度，不受"重置玩家数据"、开新局、胜负结算影响；
///  · 独立文件避免玩家存档整体替换时成就进度被动清空。
/// </summary>
[Serializable]
public class AchievementSaveData
{
    /// <summary>存档结构版本（后续若改字段含义，用它做兼容判断）</summary>
    public int version = AchievementProgress.CurrentVersion;

    /// <summary>已解锁成就 id（=《成就.xlsx》表格行序，0 起），按解锁先后排列</summary>
    public List<int> unlockedIds = new List<int>();

    /// <summary>普通模式失败累计次数（成就"失败乃成功之母"进度）</summary>
    public int normalModeFailCount = 0;

    /// <summary>游玩内卷模式累计次数（成就"大科研家"进度）</summary>
    public int involutionPlayCount = 0;
}

/// <summary>
/// 成就进度的内存模型（纯 C#，不触碰 Unity API，便于离线回归测试）。
///
///  · 解锁顺序 = unlockedIds 的列表顺序，unlockOrder 即列表下标；
///  · 解锁一旦写入永不撤销（重复触发只落一次盘）；
///  · 计数器（失败次数 / 内卷局数）跨局累计，只增不减。
/// </summary>
public class AchievementProgress
{
    /// <summary>当前存档结构版本</summary>
    public const int CurrentVersion = 1;

    /// <summary>已解锁 id，按解锁先后排列（unlockOrder = 下标）</summary>
    public readonly List<int> unlockedIds = new List<int>();

    public int normalModeFailCount;
    public int involutionPlayCount;

    /// <summary>从存档构建进度模型（越界 id / 脏数据一律跳过，不让坏档炸掉成就系统）</summary>
    public static AchievementProgress FromData(AchievementSaveData data)
    {
        AchievementProgress progress = new AchievementProgress();
        if (data == null) return progress;

        if (data.unlockedIds != null)
        {
            for (int i = 0; i < data.unlockedIds.Count; i++)
            {
                int id = data.unlockedIds[i];
                if (id >= 0 && !progress.unlockedIds.Contains(id))
                {
                    progress.unlockedIds.Add(id);
                }
            }
        }

        progress.normalModeFailCount = data.normalModeFailCount > 0 ? data.normalModeFailCount : 0;
        progress.involutionPlayCount = data.involutionPlayCount > 0 ? data.involutionPlayCount : 0;
        return progress;
    }

    /// <summary>把内存进度写回存档载体（解锁列表保持解锁顺序原样，不做排序）</summary>
    public void WriteTo(AchievementSaveData data)
    {
        if (data == null) return;

        if (data.unlockedIds == null) data.unlockedIds = new List<int>();
        data.version = CurrentVersion;

        data.unlockedIds.Clear();
        data.unlockedIds.AddRange(unlockedIds);
        data.normalModeFailCount = normalModeFailCount;
        data.involutionPlayCount = involutionPlayCount;
    }

    /// <summary>是否已解锁（越界 id 恒为未解锁）</summary>
    public bool IsUnlocked(int id)
    {
        return id >= 0 && unlockedIds.Contains(id);
    }

    /// <summary>登记一次解锁。返回 true 表示本次才解锁（重复解锁返回 false）</summary>
    public bool Unlock(int id)
    {
        if (IsUnlocked(id)) return false;
        unlockedIds.Add(id);
        return true;
    }

    /// <summary>除 excludeId 外的全部成就是否都已解锁（"感谢陪伴"判定）</summary>
    public bool AreAllOthersUnlocked(int totalCount, int excludeId)
    {
        for (int id = 0; id < totalCount; id++)
        {
            if (id == excludeId) continue;
            if (!IsUnlocked(id)) return false;
        }
        return true;
    }

    /// <summary>清空全部进度（调试用，正常游戏流程不会调用）</summary>
    public void Clear()
    {
        unlockedIds.Clear();
        normalModeFailCount = 0;
        involutionPlayCount = 0;
    }
}
