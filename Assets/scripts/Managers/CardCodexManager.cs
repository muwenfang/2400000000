using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 图鉴（收集）系统。
///
/// 展示规则（固定顺序，由本类统一给出）：
///  1. 数字卡：按资产编号升序（资产名 "002" → 2）；
///  2. 公式卡：按 FormulaCardId 升序；
///  3. 祝福：先按刷新行为分组（总是刷新 → 本回合限购 → 永不刷新），组内按 blessingId 升序。
///
/// 解锁规则：
///  · "获得即解锁"：所有获得通路都会点亮图鉴——
///    商店购买（ShopManager.TryBuy*）、卡牌包/多多益善（PlayerCardInventory.AddNumberCard）、
///    空想主义/神灯/好事成双（PlayerCardInventory.AddFormulaCard / BlessingManager.AcquireBlessing）；
///  · 唯一例外是"初始牌组"：开新局时白送的起手牌不解锁（调用方传 unlockCodex:false），
///    否则每开一局都会白点几张；
///  · 解锁是永久进度，写入独立文件 cardcodex.json，跨局、跨启动保留，
///    不受"重置玩家数据"、开新局、胜负结算影响（图鉴永不重置）。
///
/// 未解锁条目由 UI 显示上锁图标与"未解锁"。
/// </summary>
public static class CardCodexManager
{
    /// <summary>未解锁条目上显示的文本（Inspector 中 Text 的初始内容可与之保持一致）</summary>
    public const string LockedText = "未解锁";

    /// <summary>
    /// 祝福分组的展示顺序。
    /// 注意：不能直接用 BlessingData.RefreshBehavior 的枚举值排序——
    /// 它在代码里的声明顺序是 AlwaysRefresh(0) / NeverRefresh(1) / CurrentRoundOnly(2)，
    /// 而需求要求 CurrentRoundOnly 排在 NeverRefresh 之前。
    /// </summary>
    public enum BlessingGroup
    {
        AlwaysRefresh = 0,      // 总是刷新：可反复购买
        CurrentRoundOnly = 1,   // 本回合限购：本次商店最多出现一个
        NeverRefresh = 2        // 永不刷新：只能购买一次
    }

    /// <summary>一个祝福分组（分组标题 + 已排好序的祝福列表）</summary>
    public class BlessingGroupData
    {
        public BlessingGroup group;
        public BlessingData.RefreshBehavior behavior;
        public string title;
        public readonly List<BlessingData> blessings = new List<BlessingData>();

        public int Count { get { return blessings.Count; } }
    }

    private const int GroupCount = 3;

    #region 进度缓存与独立存档

    /// <summary>图鉴独立存档文件名（放在 Application.persistentDataPath 下）</summary>
    public const string SaveFileName = "cardcodex.json";

    // 图鉴以 cardcodex.json 为唯一权威来源；这里缓存它的内存视图。
    // 与 playerdata.json（玩家存档）完全解耦：玩家数据怎么重置都不会影响图鉴。
    private static CardCodexProgress cachedProgress;

    // 受限环境（离线回归/无写权限）下读写会抛异常，标记一次后不再重试，避免每次访问都抛
    private static bool ioUnavailable;

    /// <summary>丢弃进度缓存（下次访问时从磁盘重新载入）</summary>
    public static void InvalidateCache()
    {
        cachedProgress = null;
    }

    /// <summary>图鉴存档的完整路径；无法取得可写目录时返回 null</summary>
    public static string SaveFilePath
    {
        get
        {
            if (ioUnavailable) return null;

            try
            {
                return System.IO.Path.Combine(Application.persistentDataPath, SaveFileName);
            }
            catch (Exception)
            {
                ioUnavailable = true;
                return null;
            }
        }
    }

    private static CardCodexProgress Progress
    {
        get
        {
            if (cachedProgress == null) cachedProgress = LoadProgress();
            return cachedProgress;
        }
    }

    /// <summary>
    /// 载入进度：
    ///  1. 有 cardcodex.json → 直接读；
    ///  2. 没有 → 尝试从旧版 playerdata.json 的三张表迁移（只发生一次）；
    ///  3. 都没有 → 全新进度。
    /// </summary>
    private static CardCodexProgress LoadProgress()
    {
        string path = SaveFilePath;
        if (path != null && System.IO.File.Exists(path))
        {
            try
            {
                string json = System.IO.File.ReadAllText(path);
                CardCodexSaveData data = JsonUtility.FromJson<CardCodexSaveData>(json);
                if (data != null) return CardCodexProgress.FromData(data);
            }
            catch (Exception)
            {
                // 文件损坏：先备份再按空进度继续，绝不静默丢掉玩家的收集记录
                TryBackupBrokenFile(path);
                TryLogWarning("[图鉴] cardcodex.json 读取失败（文件可能损坏），已备份为 .bak，本次按空进度处理。");
            }
        }

        CardCodexProgress migrated = TryMigrateFromLegacySave();
        if (migrated != null)
        {
            SaveProgress(migrated);
            return migrated;
        }

        return new CardCodexProgress();
    }

    /// <summary>旧版把图鉴存在 playerdata.json；只在 cardcodex.json 尚不存在时迁移一次</summary>
    private static CardCodexProgress TryMigrateFromLegacySave()
    {
        try
        {
            DataSavingManager saving = DataSavingManager.Instance;
            if (saving == null) return null;

            saving.EnsureLoaded();
            CardCodexProgress legacy = CardCodexProgress.FromLegacy(saving.GetCurrentData());
            if (legacy.TotalCount == 0) return null;

            // 迁移成功后清掉旧字段：避免以后删掉 cardcodex.json 又"复活"一份过期快照
            ClearLegacyLists(saving.GetCurrentData());
            saving.SaveNow();

            return legacy;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static void ClearLegacyLists(SavingData legacy)
    {
        if (legacy == null) return;
        if (legacy.unlockedNumberCards != null) legacy.unlockedNumberCards.Clear();
        if (legacy.unlockedFormulaCards != null) legacy.unlockedFormulaCards.Clear();
        if (legacy.unlockedBlessings != null) legacy.unlockedBlessings.Clear();
    }

    /// <summary>把内存进度写到 cardcodex.json（每次解锁立即调用，不等通关/退出）</summary>
    private static void SaveProgress(CardCodexProgress progress)
    {
        if (progress == null) return;

        string path = SaveFilePath;
        if (path == null) return;

        try
        {
            CardCodexSaveData data = new CardCodexSaveData();
            progress.WriteTo(data);
            System.IO.File.WriteAllText(path, JsonUtility.ToJson(data, true));
        }
        catch (Exception)
        {
            ioUnavailable = true;
            TryLogWarning("[图鉴] 写入 cardcodex.json 失败，本次会话的解锁可能不会保留。");
        }
    }

    /// <summary>离线回归里 Debug.Log 会抛异常，日志本身也必须兜住</summary>
    private static void TryLogWarning(string message)
    {
        try { Debug.LogWarning(message); } catch (Exception) { /* 仅日志，忽略 */ }
    }

    /// <summary>把损坏的图鉴文件另存为 .bak（保留证据，避免被后续写入直接覆盖）</summary>
    private static void TryBackupBrokenFile(string path)
    {
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            if (!System.IO.File.Exists(path)) return;
            System.IO.File.Copy(path, path + ".bak", true);
        }
        catch (Exception)
        {
            // 备份失败不影响主流程
        }
    }

    #endregion

    #region 解锁（由各"获得"入口调用）

    /// <summary>解锁一张数字卡（按资产名记录）。返回 true 表示本次才解锁</summary>
    public static bool UnlockNumberCard(NumberCardData card)
    {
        string assetName = SafeGetAssetName(card, "数字卡");
        if (assetName == null) return false;

        return ApplyUnlock(delegate (CardCodexProgress p) { return p.UnlockNumberCard(assetName); });
    }

    /// <summary>解锁一张公式卡（按资产名记录）。返回 true 表示本次才解锁</summary>
    public static bool UnlockFormulaCard(FormulaCardData card)
    {
        string assetName = SafeGetAssetName(card, "公式卡");
        if (assetName == null) return false;

        return ApplyUnlock(delegate (CardCodexProgress p) { return p.UnlockFormulaCard(assetName); });
    }

    /// <summary>
    /// 取资产名作为图鉴身份。
    /// 图鉴只是附加记录：取不到名字（运行时临时对象、离线回归环境）就静默跳过解锁，
    /// 绝不能因为图鉴出错而让"获得卡牌"这条主流程抛异常。
    /// </summary>
    private static string SafeGetAssetName(UnityEngine.Object asset, string kindLabel, bool logIfMissing = true)
    {
        if (asset == null) return null;

        try
        {
            string assetName = asset.name;
            if (!string.IsNullOrEmpty(assetName)) return assetName;
        }
        catch (Exception)
        {
            // 落到下面的日志分支
        }

        if (logIfMissing)
            TryLogWarning($"[图鉴] {kindLabel}没有可用的资产名（运行时临时创建的对象不入图鉴），已跳过。");
        return null;
    }

    /// <summary>解锁一个祝福（按 blessingId 记录）。返回 true 表示本次才解锁</summary>
    public static bool UnlockBlessing(BlessingData blessing)
    {
        if (blessing == null) return false;

        int blessingId = blessing.blessingId;
        return ApplyUnlock(delegate (CardCodexProgress p) { return p.UnlockBlessing(blessingId); });
    }

    /// <summary>执行解锁并立即落盘（图鉴是跨局进度，不能等胜利/退出时才保存）</summary>
    private static bool ApplyUnlock(Func<CardCodexProgress, bool> unlock)
    {
        if (unlock == null) return false;

        CardCodexProgress progress = Progress;
        if (!unlock(progress)) return false;

        SaveProgress(progress);
        return true;
    }

    #endregion

    #region 查询

    public static bool IsNumberCardUnlocked(NumberCardData card)
    {
        string assetName = SafeGetAssetName(card, "数字卡", false);
        if (assetName == null) return false;
        return Progress.IsNumberCardUnlocked(assetName);
    }

    public static bool IsFormulaCardUnlocked(FormulaCardData card)
    {
        string assetName = SafeGetAssetName(card, "公式卡", false);
        if (assetName == null) return false;
        return Progress.IsFormulaCardUnlocked(assetName);
    }

    public static bool IsBlessingUnlocked(BlessingData blessing)
    {
        if (blessing == null) return false;
        return Progress.IsBlessingUnlocked(blessing.blessingId);
    }

    #endregion

    #region 展示列表（固定顺序）

    /// <summary>
    /// 全部数字卡，按资产编号升序。
    /// 来源是商店卡库（NumberCardLibrary），即"能被买到的卡"；
    /// 资产名重复（同一资产被引用两次）时只保留第一次出现的那张。
    /// </summary>
    public static List<NumberCardData> GetOrderedNumberCards()
    {
        List<NumberCardData> result = new List<NumberCardData>();

        NumberCardLibrary library = FindNumberCardLibrary();
        if (library == null || library.allCards == null)
        {
            Debug.LogWarning("[图鉴] 找不到数字卡库（NumberCardLibrary），图鉴的数字卡列表为空。");
            return result;
        }

        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        List<NumberCardSortEntry> entries = new List<NumberCardSortEntry>(library.allCards.Count);

        for (int i = 0; i < library.allCards.Count; i++)
        {
            NumberCardData card = library.allCards[i];
            if (card == null) continue;

            // partA 缺失的资产属于未配置完整的卡（买不到、也显示不出卡面），不进图鉴
            if (card.partA == null) continue;

            string assetName = card.name;
            // 运行时 CreateInstance 出来的临时资产没有资产名，无法作为图鉴身份，跳过
            if (string.IsNullOrEmpty(assetName)) continue;
            if (!seen.Add(assetName)) continue;

            entries.Add(new NumberCardSortEntry
            {
                card = card,
                assetName = assetName,
                sortId = ParseNumberCardSortId(assetName)
            });
        }

        // 预先算好名字与序号：比较函数里反复访问 SO.name 每次都会走一次原生调用
        entries.Sort(delegate (NumberCardSortEntry a, NumberCardSortEntry b)
        {
            return CompareNumberCardSortKey(a.assetName, a.sortId, b.assetName, b.sortId);
        });

        for (int i = 0; i < entries.Count; i++) result.Add(entries[i].card);
        return result;
    }

    /// <summary>全部公式卡，按 FormulaCardId 升序</summary>
    public static List<FormulaCardData> GetOrderedFormulaCards()
    {
        List<FormulaCardData> result = new List<FormulaCardData>();

        FormulaCardLibrary library = FindFormulaCardLibrary();
        if (library == null || library.allCards == null)
        {
            Debug.LogWarning("[图鉴] 找不到公式卡库（FormulaCardLibrary），图鉴的公式卡列表为空。");
            return result;
        }

        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        List<FormulaCardSortEntry> entries = new List<FormulaCardSortEntry>(library.allCards.Count);

        for (int i = 0; i < library.allCards.Count; i++)
        {
            FormulaCardData card = library.allCards[i];
            if (card == null) continue;

            string assetName = card.name;
            // 兜底身份：资产名缺失时用 id 拼一个，避免所有无名卡被当成同一张
            string key = string.IsNullOrEmpty(assetName)
                ? "#" + card.FormulaCardId.ToString()
                : assetName;
            if (!seen.Add(key)) continue;

            entries.Add(new FormulaCardSortEntry
            {
                card = card,
                assetName = key,
                sortId = card.FormulaCardId
            });
        }

        entries.Sort(delegate (FormulaCardSortEntry a, FormulaCardSortEntry b)
        {
            return CompareFormulaCardSortKey(a.sortId, a.assetName, b.sortId, b.assetName);
        });

        for (int i = 0; i < entries.Count; i++) result.Add(entries[i].card);
        return result;
    }

    /// <summary>
    /// 全部祝福，按"总是刷新 → 本回合限购 → 永不刷新"分三组，组内按 blessingId 升序。
    /// 固定返回 3 个分组（某组为空时 Count 为 0），调用方可直接按顺序铺 UI。
    /// </summary>
    public static List<BlessingGroupData> GetOrderedBlessingGroups()
    {
        List<BlessingData> all = new List<BlessingData>();

        BlessingLibrary library = FindBlessingLibrary();
        if (library != null && library.allBlessings != null)
        {
            all.AddRange(library.allBlessings);
        }
        else
        {
            Debug.LogWarning("[图鉴] 找不到祝福库（BlessingLibrary），图鉴的祝福列表为空。");
        }

        return BuildBlessingGroups(all);
    }

    /// <summary>纯逻辑：分组 + 去重 + 组内排序（不访问 Unity API，可离线回归）</summary>
    public static List<BlessingGroupData> BuildBlessingGroups(IEnumerable<BlessingData> source)
    {
        List<BlessingGroupData> groups = new List<BlessingGroupData>(GroupCount);
        for (int i = 0; i < GroupCount; i++)
        {
            BlessingGroup group = (BlessingGroup)i;
            groups.Add(new BlessingGroupData
            {
                group = group,
                behavior = ToRefreshBehavior(group),
                title = GetBlessingGroupTitle(group)
            });
        }

        HashSet<int> seenIds = new HashSet<int>();
        if (source != null)
        {
            foreach (BlessingData data in source)
            {
                if (data == null) continue;
                // blessingId 是祝福的图鉴身份；同一 id 被库引用两次时只显示一次
                if (!seenIds.Add(data.blessingId)) continue;

                int index = (int)GetBlessingGroup(data.refreshBehavior);
                if (index < 0 || index >= groups.Count) index = 0;
                groups[index].blessings.Add(data);
            }
        }

        for (int i = 0; i < groups.Count; i++)
        {
            groups[i].blessings.Sort(CompareBlessing);
        }

        return groups;
    }

    #endregion

    #region 排序 / 分组（纯函数，离线可测）

    private class NumberCardSortEntry
    {
        public NumberCardData card;
        public string assetName;
        public int sortId;
    }

    private class FormulaCardSortEntry
    {
        public FormulaCardData card;
        public string assetName;
        public int sortId;
    }

    /// <summary>
    /// 从数字卡资产名解析图鉴序号：工程内数字卡资产名形如 "002" / "123"（纯数字）。
    /// 解析失败（如 "{0}"、空名）返回 int.MaxValue，让这类条目排到最后，再由资产名做稳定排序。
    /// 必须按数值而不是字符串排序："10" 的字符串序在 "2" 之前，会让图鉴顺序错乱。
    /// </summary>
    public static int ParseNumberCardSortId(string assetName)
    {
        if (string.IsNullOrEmpty(assetName)) return int.MaxValue;

        int value;
        if (int.TryParse(assetName.Trim(), out value)) return value;
        return int.MaxValue;
    }

    /// <summary>数字卡排序比较：序号升序，序号相同按资产名字典序</summary>
    public static int CompareNumberCardSortKey(string assetNameA, int sortIdA, string assetNameB, int sortIdB)
    {
        int byId = sortIdA.CompareTo(sortIdB);
        if (byId != 0) return byId;
        return string.CompareOrdinal(assetNameA ?? string.Empty, assetNameB ?? string.Empty);
    }

    /// <summary>公式卡排序比较：FormulaCardId 升序，id 相同按资产名字典序（兜底稳定）</summary>
    public static int CompareFormulaCardSortKey(int sortIdA, string assetNameA, int sortIdB, string assetNameB)
    {
        int byId = sortIdA.CompareTo(sortIdB);
        if (byId != 0) return byId;
        return string.CompareOrdinal(assetNameA ?? string.Empty, assetNameB ?? string.Empty);
    }

    /// <summary>祝福组内排序：blessingId 升序</summary>
    public static int CompareBlessing(BlessingData a, BlessingData b)
    {
        if (ReferenceEquals(a, b)) return 0;
        if (a == null) return -1;
        if (b == null) return 1;
        return a.blessingId.CompareTo(b.blessingId);
    }

    /// <summary>把刷新行为映射到展示分组（总是刷新 → 本回合限购 → 永不刷新）</summary>
    public static BlessingGroup GetBlessingGroup(BlessingData.RefreshBehavior behavior)
    {
        switch (behavior)
        {
            case BlessingData.RefreshBehavior.CurrentRoundOnly:
                return BlessingGroup.CurrentRoundOnly;
            case BlessingData.RefreshBehavior.NeverRefresh:
                return BlessingGroup.NeverRefresh;
            default:
                return BlessingGroup.AlwaysRefresh;
        }
    }

    /// <summary>分组 → 刷新行为（与 GetBlessingGroup 互为逆映射）</summary>
    public static BlessingData.RefreshBehavior ToRefreshBehavior(BlessingGroup group)
    {
        switch (group)
        {
            case BlessingGroup.CurrentRoundOnly:
                return BlessingData.RefreshBehavior.CurrentRoundOnly;
            case BlessingGroup.NeverRefresh:
                return BlessingData.RefreshBehavior.NeverRefresh;
            default:
                return BlessingData.RefreshBehavior.AlwaysRefresh;
        }
    }

    /// <summary>分组标题（面板分组头直接用这个字符串）</summary>
    public static string GetBlessingGroupTitle(BlessingGroup group)
    {
        switch (group)
        {
            case BlessingGroup.CurrentRoundOnly:
                return "本回合限购";
            case BlessingGroup.NeverRefresh:
                return "永不刷新";
            default:
                return "总是刷新";
        }
    }

    #endregion

    #region 资产来源

    private static NumberCardLibrary FindNumberCardLibrary()
    {
        CardManager cardManager = CardManager.Instance;
        if (cardManager != null && cardManager.numberCardLibrary != null)
            return cardManager.numberCardLibrary;

        PlayerCardInventory inventory = PlayerCardInventory.Instance;
        if (inventory != null && inventory.numberCardLibrary != null)
            return inventory.numberCardLibrary;

        NumberCardLibrary[] found = Resources.FindObjectsOfTypeAll<NumberCardLibrary>();
        if (found != null && found.Length > 0) return found[0];

        return null;
    }

    private static FormulaCardLibrary FindFormulaCardLibrary()
    {
        CardManager cardManager = CardManager.Instance;
        if (cardManager != null && cardManager.formulaCardLibrary != null)
            return cardManager.formulaCardLibrary;

        FormulaCardLibrary[] found = Resources.FindObjectsOfTypeAll<FormulaCardLibrary>();
        if (found != null && found.Length > 0) return found[0];

        return null;
    }

    private static BlessingLibrary FindBlessingLibrary()
    {
        BlessingManager blessingManager = BlessingManager.Instance;
        if (blessingManager != null && blessingManager.blessingLibrary != null)
            return blessingManager.blessingLibrary;

        BlessingLibrary[] found = Resources.FindObjectsOfTypeAll<BlessingLibrary>();
        if (found != null && found.Length > 0) return found[0];

        return null;
    }

    #endregion
}
