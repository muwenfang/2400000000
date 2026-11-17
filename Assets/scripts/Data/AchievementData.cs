/// <summary>
/// 单条成就数据（纯展示层，不含判定逻辑）。
/// 名字 / 描述来自《成就.xlsx》表格顺序；解锁状态由 AchievementManager 维护。
/// </summary>
public class AchievementData
{
    /// <summary>成就唯一标识 = 表格行序（0 起）。表格中间插行会改变 id，排序输出用名字展示不受影响。</summary>
    public readonly int id;

    /// <summary>成就名字（固定显示）</summary>
    public readonly string name;

    /// <summary>成就描述（解锁后显示）</summary>
    public readonly string description;

    /// <summary>是否已解锁（当前无判定逻辑，由外部调用 AchievementManager.UnlockAchievement 置位）</summary>
    public bool IsUnlocked { get { return unlockOrder >= 0; } }

    /// <summary>解锁先后序号：-1 = 未解锁；数值越大表示解锁得越晚（用于"最新解锁排最顶"）</summary>
    public int unlockOrder = -1;

    public AchievementData(int id, string name, string description)
    {
        this.id = id;
        this.name = name;
        this.description = description;
    }
}
