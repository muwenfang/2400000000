using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 成就管理器（静态类，无需挂场景）：
///   · 持有全部成就数据（名字 / 描述直接取自《成就.xlsx》表格顺序，内嵌在 BuildAchievements）。
///   · 维护解锁状态与解锁先后序号；解锁唯一入口 UnlockAchievement(id)，满足条件时由各触发点调用。
///   · 解锁进度写入独立文件 achievements.json，跨局、跨启动保留，
///     不受"重置玩家数据"、开新局、胜负结算影响（成就永不重置）。
///   · GetDisplayList 给出展示顺序：解锁的成就前置（最新解锁在最顶），未解锁的保持表格顺序。
///
/// 触发逻辑总览（成就 id = 表格行序，0 起）：
///   通关普通模式时统一检测（无论之后是否进入无尽模式）：
///     0 感谢游玩 / 1 算数专家(难度≥50) / 2 算数大师(难度≥100) /
///     9 真·唯物主义(无祝福) / 10 真·虚无主义(虚无主义≥50) / 11 真·空想主义(全部填空卡各一张) /
///     14 真·暗箱操作(≥30个祝福价格非正) / 15 真·卡牌大师(数字卡≥50) /
///     16 真·众神归位(祝福≥100) / 17 真·赌神传说(所有数字卡含~20~)
///   全程记录 / 任意模式：
///     3 失败乃成功之母(普通模式失败累计3次) / 4 长线运营(普通模式进入无尽且回合≥75) /
///     5 大科研家(游玩内卷模式累计5次) / 6 套娃(连续获得两次纯粹容器) /
///     7 何意味？(许愿币效果选中许愿币) / 8 梦幻联动（(获得{114}*{514}) /
///     12 真·走马观花(一回合内刷新商店50次) / 13 真·理财大师(理财大师≥50)
///   18 感谢陪伴：其它 18 项全部解锁后自动解锁（UnlockAchievement 内部级联）。
/// </summary>
public static class AchievementManager
{
    // ===================== 成就 id 常量（= 表格行序，0 起） =====================
    public const int IdThanksForPlaying = 0;        // 感谢游玩
    public const int IdArithmeticExpert = 1;        // 算数专家
    public const int IdArithmeticMaster = 2;        // 算数大师
    public const int IdMotherOfSuccess = 3;         // 失败乃成功之母
    public const int IdLongTermOperation = 4;       // 长线运营
    public const int IdBigScientist = 5;            // 大科研家
    public const int IdNestingDolls = 6;            // 套娃
    public const int IdMeaningWhat = 7;             // 何意味？
    public const int IdDreamCollaboration = 8;      // 梦幻联动（
    public const int IdTrueMaterialism = 9;         // 真·唯物主义
    public const int IdTrueNihilism = 10;           // 真·虚无主义
    public const int IdTrueUtopianism = 11;         // 真·空想主义
    public const int IdTrueHastyAppreciation = 12;  // 真·走马观花
    public const int IdTrueFinancialMaster = 13;    // 真·理财大师
    public const int IdTrueDarkBoxOperation = 14;   // 真·暗箱操作
    public const int IdTrueCardMaster = 15;         // 真·卡牌大师
    public const int IdTrueAllGodsInPlace = 16;     // 真·众神归位
    public const int IdTrueGodOfGambler = 17;       // 真·赌神传说
    public const int IdThanksForCompany = 18;       // 感谢陪伴

    // ===================== 判定阈值 =====================
    public const int FailCountGoal = 3;                     // 失败乃成功之母
    public const int InvolutionPlayGoal = 5;                // 大科研家
    public const int ShopRefreshGoal = 50;                  // 真·走马观花
    public const int EndlessRoundGoal = 75;                 // 长线运营
    public const int NihilismGoal = 50;                     // 真·虚无主义
    public const int FinancialMasterGoal = 50;              // 真·理财大师
    public const int NumberCardGoal = 50;                   // 真·卡牌大师
    public const int BlessingGoal = 100;                    // 真·众神归位
    public const int NonPositivePriceBlessingGoal = 30;     // 真·暗箱操作
    public const int DifficultyExpertGoal = 50;             // 算数专家
    public const int DifficultyMasterGoal = 100;            // 算数大师
    /// <summary>梦幻联动 / 真·赌神传说 涉及的骰子面数（~20~）</summary>
    public const int DreamDiceSides = 20;
    /// <summary>梦幻联动 目标数字卡的显示名（cardName，工程内唯一）</summary>
    public const string DreamCollabCardName = "{114}*{514}";

    private static List<AchievementData> achievements;
    private static int nextUnlockOrder;

    /// <summary>
    /// 成就解锁事件：UnlockAchievement 成功解锁后同步触发（级联解锁也会各触发一次）。
    /// 展示层（如成就解锁提示条）在 OnEnable/OnDisable 里订阅/退订。
    /// </summary>
    public static event Action<AchievementData> AchievementUnlocked;

    #region 进度持久化（achievements.json，独立于玩家存档）

    /// <summary>成就独立存档文件名（放在 Application.persistentDataPath 下）</summary>
    public const string SaveFileName = "achievements.json";

    // 成就以 achievements.json 为唯一权威来源；这里缓存它的内存视图。
    private static AchievementProgress cachedProgress;

    // 受限环境（离线回归/无写权限）下读写会抛异常，标记一次后不再重试，避免每次访问都抛
    private static bool ioUnavailable;

    /// <summary>丢弃进度缓存（下次访问时从磁盘重新载入；测试用）</summary>
    public static void InvalidateCache()
    {
        cachedProgress = null;
    }

    /// <summary>成就存档的完整路径；无法取得可写目录时返回 null</summary>
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

    private static AchievementProgress Progress
    {
        get
        {
            if (cachedProgress == null) cachedProgress = LoadProgress();
            return cachedProgress;
        }
    }

    /// <summary>载入进度：有 achievements.json 直接读；没有（或损坏）按全新进度处理</summary>
    private static AchievementProgress LoadProgress()
    {
        string path = SaveFilePath;
        if (path != null && System.IO.File.Exists(path))
        {
            try
            {
                string json = System.IO.File.ReadAllText(path);
                AchievementSaveData data = JsonUtility.FromJson<AchievementSaveData>(json);
                if (data != null) return AchievementProgress.FromData(data);
            }
            catch (Exception)
            {
                // 文件损坏：先备份再按空进度继续，绝不静默丢掉玩家的成就进度
                TryBackupBrokenFile(path);
                TryLogWarning("[成就] achievements.json 读取失败（文件可能损坏），已备份为 .bak，本次按空进度处理。");
            }
        }

        return new AchievementProgress();
    }

    /// <summary>把内存进度写到 achievements.json（每次解锁/计数变化立即调用，不等退出）</summary>
    private static void SaveProgress()
    {
        if (cachedProgress == null) return;

        string path = SaveFilePath;
        if (path == null) return;

        try
        {
            AchievementSaveData data = new AchievementSaveData();
            cachedProgress.WriteTo(data);
            System.IO.File.WriteAllText(path, JsonUtility.ToJson(data, true));
        }
        catch (Exception)
        {
            ioUnavailable = true;
            TryLogWarning("[成就] 写入 achievements.json 失败，本次会话的进度可能不会保留。");
        }
    }

    /// <summary>离线回归里 Debug.Log 会抛异常，日志本身也必须兜住</summary>
    private static void TryLogWarning(string message)
    {
        try { Debug.LogWarning(message); } catch (Exception) { /* 仅日志，忽略 */ }
    }

    /// <summary>把损坏的成就文件另存为 .bak（保留证据，避免被后续写入直接覆盖）</summary>
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

    #region 展示层（原有 API，保持不变）

    /// <summary>全部成就（表格顺序，只读）</summary>
    public static IReadOnlyList<AchievementData> All
    {
        get
        {
            EnsureBuilt();
            return achievements;
        }
    }

    /// <summary>成就总数</summary>
    public static int Count
    {
        get
        {
            EnsureBuilt();
            return achievements.Count;
        }
    }

    /// <summary>按 id 取单条成就；越界返回 null</summary>
    public static AchievementData GetAchievement(int id)
    {
        EnsureBuilt();
        if (id < 0 || id >= achievements.Count) return null;
        return achievements[id];
    }

    /// <summary>
    /// 取展示用列表（新建列表，不影响 All 的表格顺序）：
    ///   1. 已解锁在前，按解锁序号降序 —— 最新解锁的排最顶；
    ///   2. 未解锁按表格原顺序跟在后面。
    /// </summary>
    public static List<AchievementData> GetDisplayList()
    {
        EnsureBuilt();

        List<AchievementData> display = new List<AchievementData>(achievements.Count);
        List<AchievementData> locked = new List<AchievementData>();

        for (int i = 0; i < achievements.Count; i++)
        {
            AchievementData achievement = achievements[i];
            if (achievement.IsUnlocked) display.Add(achievement);
            else locked.Add(achievement);
        }

        // 解锁组：解锁序号降序（最新在最前）。数量小，直接选择排序，稳定且不引入比较器。
        for (int i = 0; i < display.Count; i++)
        {
            int newest = i;
            for (int j = i + 1; j < display.Count; j++)
            {
                if (display[j].unlockOrder > display[newest].unlockOrder) newest = j;
            }
            if (newest != i)
            {
                AchievementData temp = display[i];
                display[i] = display[newest];
                display[newest] = temp;
            }
        }

        display.AddRange(locked);
        return display;
    }

    /// <summary>已解锁数量</summary>
    public static int UnlockedCount
    {
        get
        {
            EnsureBuilt();
            int count = 0;
            for (int i = 0; i < achievements.Count; i++)
            {
                if (achievements[i].IsUnlocked) count++;
            }
            return count;
        }
    }

    #endregion

    #region 解锁（触发系统的唯一出口）

    /// <summary>
    /// 解锁成就（所有触发点的唯一出口）。已解锁时静默跳过（幂等）。
    /// 每次解锁立即落盘；除"感谢陪伴"外，任何一次解锁后都会检查
    /// 其余成就是否全部解锁，满足则级联解锁"感谢陪伴"。
    /// </summary>
    public static void UnlockAchievement(int id)
    {
        AchievementData achievement = GetAchievement(id);
        if (achievement == null || achievement.IsUnlocked) return;

        AchievementProgress progress = Progress;
        if (!progress.Unlock(id)) return;

        achievement.unlockOrder = nextUnlockOrder++;
        SaveProgress();

        // 通知展示层（提示条动画等）。注意"感谢陪伴"的级联解锁在其后单独触发。
        if (AchievementUnlocked != null)
        {
            try { AchievementUnlocked(achievement); }
            catch (Exception) { /* 展示层异常绝不中断解锁主流程 */ }
        }

        // 感谢陪伴：其它全部成就解锁后自动点亮（级联只走一层，避免递归）
        if (id != IdThanksForCompany && progress.AreAllOthersUnlocked(Count, IdThanksForCompany))
        {
            UnlockAchievement(IdThanksForCompany);
        }
    }

    /// <summary>某成就是否已解锁（供 UI / 测试查询）</summary>
    public static bool IsUnlocked(int id)
    {
        return Progress.IsUnlocked(id);
    }

    /// <summary>
    /// 清空全部解锁状态与计数（测试 / 调试用；只清内存缓存，不删除磁盘存档）。
    /// 正式存档由 UnlockAchievement / Record* 在下次写入时重建。
    /// </summary>
    public static void ResetUnlockState()
    {
        EnsureBuilt();
        nextUnlockOrder = 0;
        for (int i = 0; i < achievements.Count; i++)
        {
            achievements[i].unlockOrder = -1;
        }

        if (cachedProgress != null)
        {
            cachedProgress.Clear();
        }
    }

    /// <summary>普通模式累计失败次数（成就进度查询用）</summary>
    public static int NormalModeFailCount { get { return Progress.normalModeFailCount; } }

    /// <summary>游玩内卷模式累计次数（成就进度查询用）</summary>
    public static int InvolutionPlayCount { get { return Progress.involutionPlayCount; } }

    #endregion

    #region 触发点（由各系统调用）

    /// <summary>
    /// 普通模式胜利快照：通关瞬间（进入无尽模式之前）的全局状态。
    /// 由 GameManager.WinGame 在普通模式胜利时采集并传入，
    /// 判定本身是纯逻辑，便于离线回归。
    /// </summary>
    public class NormalModeWinSnapshot
    {
        public int difficultyLevel;                 // 当前难度等级（五个倍率+两个开关折算）
        public int totalBlessingCount;              // 持有祝福总数
        public int nihilismCount;                   // 虚无主义数量
        public int numberCardCount;                 // 数字卡总数
        public int nonPositivePriceBlessingCount;   // 价格非正的祝福种类数
        public bool allNumberCardsContainDice20;    // 所有数字卡均含 ~20~
        public bool allFormulaKindsExactlyOne;      // 全部种类填空卡恰好各一张
    }

    /// <summary>
    /// 普通模式通关统一检测（0/1/2/9/10/11/14/15/16/17）。
    /// 只在普通模式胜利时调用（内卷/无尽不触发），且无论之后是否进入无尽模式。
    /// </summary>
    public static void OnNormalModeWon(NormalModeWinSnapshot snapshot)
    {
        if (snapshot == null) return;

        // 1/2/3 通关类
        UnlockAchievement(IdThanksForPlaying);
        if (snapshot.difficultyLevel >= DifficultyExpertGoal) UnlockAchievement(IdArithmeticExpert);
        if (snapshot.difficultyLevel >= DifficultyMasterGoal) UnlockAchievement(IdArithmeticMaster);

        // 真·唯物主义：通关时未持有任何祝福
        if (snapshot.totalBlessingCount <= 0) UnlockAchievement(IdTrueMaterialism);
        // 真·虚无主义：通关时虚无主义 ≥ 50
        if (snapshot.nihilismCount >= NihilismGoal) UnlockAchievement(IdTrueNihilism);
        // 真·空想主义：恰好持有全部种类填空卡各一张
        if (snapshot.allFormulaKindsExactlyOne) UnlockAchievement(IdTrueUtopianism);
        // 真·暗箱操作：≥30 个祝福的价格均为非正数
        if (snapshot.nonPositivePriceBlessingCount >= NonPositivePriceBlessingGoal) UnlockAchievement(IdTrueDarkBoxOperation);
        // 真·卡牌大师：通关时持有 ≥50 张数字卡
        if (snapshot.numberCardCount >= NumberCardGoal) UnlockAchievement(IdTrueCardMaster);
        // 真·众神归位：通关时持有 ≥100 个祝福
        if (snapshot.totalBlessingCount >= BlessingGoal) UnlockAchievement(IdTrueAllGodsInPlace);
        // 真·赌神传说：通关时所有数字卡均含 ~20~
        if (snapshot.allNumberCardsContainDice20) UnlockAchievement(IdTrueGodOfGambler);
    }

    /// <summary>普通模式失败一次（内卷/无尽失败不计入，由调用方过滤）</summary>
    public static void RecordNormalModeFail()
    {
        AchievementProgress progress = Progress;
        progress.normalModeFailCount++;
        SaveProgress();

        if (progress.normalModeFailCount >= FailCountGoal)
        {
            UnlockAchievement(IdMotherOfSuccess);
        }
    }

    /// <summary>游玩一次内卷模式（开新局时计数）</summary>
    public static void RecordInvolutionPlay()
    {
        AchievementProgress progress = Progress;
        progress.involutionPlayCount++;
        SaveProgress();

        if (progress.involutionPlayCount >= InvolutionPlayGoal)
        {
            UnlockAchievement(IdBigScientist);
        }
    }

    /// <summary>
    /// 无尽模式回合数检测（长线运营：普通模式下进入无尽模式并达到至少75回合）。
    /// 是否"普通模式进入"由调用方判定（currentGameMode == Normal）。
    /// </summary>
    public static void OnEndlessRoundReached(int currentRound)
    {
        if (currentRound >= EndlessRoundGoal)
        {
            UnlockAchievement(IdLongTermOperation);
        }
    }

    /// <summary>
    /// 获得一个祝福后调用（唯一获得入口 BlessingManager.AcquireBlessing 内）：
    ///   · 套娃：本次与前一次连续都是纯粹容器；
    ///   · 真·理财大师：理财大师持有数 ≥ 50。
    /// </summary>
    public static void OnBlessingAcquired(BlessingData acquired, BlessingData previouslyAcquired, int financialMasterCount)
    {
        if (acquired != null
            && previouslyAcquired != null
            && acquired.blessingType == BlessingData.BlessingType.PureVessel
            && previouslyAcquired.blessingType == BlessingData.BlessingType.PureVessel)
        {
            UnlockAchievement(IdNestingDolls);
        }

        if (financialMasterCount >= FinancialMasterGoal)
        {
            UnlockAchievement(IdTrueFinancialMaster);
        }
    }

    /// <summary>许愿币效果选择回调（选中目标即调用）</summary>
    public static void OnWishCoinSelected(BlessingData selected)
    {
        // 何意味？：使用许愿币的效果选择了许愿币自身
        if (selected != null && selected.blessingType == BlessingData.BlessingType.WishingCoin)
        {
            UnlockAchievement(IdMeaningWhat);
        }
    }

    /// <summary>获得一张数字卡后调用（PlayerCardInventory.AddNumberCard，白送起始牌组不调用）</summary>
    public static void OnNumberCardObtained(NumberCardData card)
    {
        if (card == null || card.cardName == null) return;

        // 梦幻联动（：获得数字卡 {114}*{514}
        // 用 cardName 匹配（成就描述写的是卡面显示名）；该名字在工程内唯一（资产 083）。
        if (card.cardName == DreamCollabCardName)
        {
            UnlockAchievement(IdDreamCollaboration);
        }
    }

    /// <summary>商店刷新一次后调用（传入本次商店内已刷新次数）</summary>
    public static void OnShopRefreshedInCurrentShop(int shopRefreshCount)
    {
        // 真·走马观花：一回合内刷新商店 50 次
        if (shopRefreshCount >= ShopRefreshGoal)
        {
            UnlockAchievement(IdTrueHastyAppreciation);
        }
    }

    #endregion

    #region 纯判定函数（离线可测）

    /// <summary>
    /// 真·赌神传说判定：所有数字卡均包含 ~20~（partA 或 partB 是 20 面骰）。
    /// 库存为空时返回 false（实际对局最少保留 6 张，不会出现空库存）。
    /// </summary>
    public static bool AllNumberCardsContainDice20(List<NumberCardInstance> numberCards)
    {
        if (numberCards == null || numberCards.Count == 0) return false;

        for (int i = 0; i < numberCards.Count; i++)
        {
            NumberCardInstance instance = numberCards[i];
            if (instance == null || instance.cardData == null) return false;
            if (!CardContainsDice20(instance.cardData)) return false;
        }
        return true;
    }

    /// <summary>单张数字卡是否包含 20 面骰（partA / partB 任一）</summary>
    private static bool CardContainsDice20(NumberCardData cardData)
    {
        if (cardData.partA != null && cardData.partA.isDice && cardData.partA.diceSides == DreamDiceSides) return true;
        if (cardData.partB != null && cardData.partB.isDice && cardData.partB.diceSides == DreamDiceSides) return true;
        return false;
    }

    /// <summary>
    /// 真·空想主义判定：恰好持有全部种类的填空卡各一张。
    /// "全部种类" = 填空卡库（FormulaCardLibrary.allCards）∪ 起始填空卡（CardManager.starterFormulaCards），
    /// 与局内存档的资产解析口径一致（卡库之外还有白送的 InitialFormula）。
    /// 条件：每种恰好 1 张，且没有种类之外的填空卡。
    /// </summary>
    public static bool HasExactlyOneOfEachFormulaKind(List<FormulaCardData> ownedFormulaCards, List<FormulaCardData> allFormulaKinds)
    {
        if (allFormulaKinds == null || allFormulaKinds.Count == 0) return false;
        if (ownedFormulaCards == null) return false;

        // 按 SO.name（工程内唯一）取身份；运行时临时对象取不到名字则判定失败（宁缺毋滥）
        List<string> ownedNames = new List<string>(ownedFormulaCards.Count);
        for (int i = 0; i < ownedFormulaCards.Count; i++)
        {
            string assetName = SafeGetAssetName(ownedFormulaCards[i]);
            if (assetName == null) return false;
            ownedNames.Add(assetName);
        }

        List<string> kindNames = new List<string>(allFormulaKinds.Count);
        for (int i = 0; i < allFormulaKinds.Count; i++)
        {
            string assetName = SafeGetAssetName(allFormulaKinds[i]);
            if (assetName == null) return false;
            kindNames.Add(assetName);
        }

        return HasExactlyOneOfEachFormulaKindByName(ownedNames, kindNames);
    }

    /// <summary>
    /// 空想主义判定的纯字符串核心（不触碰 Unity API，离线回归可直接测）：
    /// 每个 kind 恰好出现一次，且没有 kind 之外的条目。
    /// </summary>
    public static bool HasExactlyOneOfEachFormulaKindByName(List<string> ownedNames, List<string> kindNames)
    {
        if (kindNames == null || kindNames.Count == 0) return false;
        if (ownedNames == null || ownedNames.Count != kindNames.Count) return false;

        Dictionary<string, int> ownedCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < ownedNames.Count; i++)
        {
            string name = ownedNames[i];
            if (string.IsNullOrEmpty(name)) return false;

            if (ownedCounts.ContainsKey(name)) ownedCounts[name]++;
            else ownedCounts[name] = 1;
        }

        for (int i = 0; i < kindNames.Count; i++)
        {
            string name = kindNames[i];
            if (string.IsNullOrEmpty(name)) return false;

            int count;
            if (!ownedCounts.TryGetValue(name, out count) || count != 1) return false;
        }

        // 持有总数 == 种类数 且 每种各出现一次 ⇒ 没有种类之外的填空卡
        return true;
    }

    /// <summary>取 SO.name 作为判定身份；运行时临时对象取不到名字则判定失败（宁缺毋滥）</summary>
    private static string SafeGetAssetName(UnityEngine.Object asset)
    {
        if (asset == null) return null;

        try
        {
            string assetName = asset.name;
            if (!string.IsNullOrEmpty(assetName)) return assetName;
        }
        catch (Exception)
        {
            // 落到下面的返回 null
        }
        return null;
    }

    #endregion

    private static void EnsureBuilt()
    {
        if (achievements != null) return;
        achievements = BuildAchievements();

        // 从独立存档恢复解锁状态与顺序（无存档/离线环境按全新进度处理）
        AchievementProgress progress = Progress;
        nextUnlockOrder = 0;
        for (int i = 0; i < progress.unlockedIds.Count; i++)
        {
            int id = progress.unlockedIds[i];
            if (id < 0 || id >= achievements.Count) continue;
            if (achievements[id].IsUnlocked) continue; // 存档脏数据：重复 id 跳过
            achievements[id].unlockOrder = nextUnlockOrder++;
        }
    }

    /// <summary>
    /// 成就总表：内容与顺序 = 《成就.xlsx》Sheet1（A 列名字，B 列描述）。
    /// 表格更新时只需同步此数组，id = 数组下标。
    /// </summary>
    private static List<AchievementData> BuildAchievements()
    {
        string[,] table =
        {
            { "感谢游玩", "任意难度通关普通模式一次" },
            { "算数专家", "至少难度50通关普通模式一次" },
            { "算数大师", "难度100通关普通模式一次" },
            { "失败乃成功之母", "普通模式失败三次" },
            { "长线运营", "任意难度普通模式下进入无尽模式，并达到至少75回合" },
            { "大科研家", "游玩内卷模式5次" },
            { "套娃", "连续获得两次\u201c纯粹容器\u201d" },
            { "何意味？", "使用\u201c许愿币\u201d的效果选择\u201c许愿币\u201d" },
            { "梦幻联动（", "获得数字卡{114}*{514}" },
            { "真·唯物主义", "任意难度通关普通模式时未持有任何祝福" },
            { "真·虚无主义", "任意难度通关普通模式时至少持有50个虚无主义" },
            { "真·空想主义", "任意难度通关普通模式时恰好持有全部种类的填空卡各一张" },
            { "真·走马观花", "一回合内刷新商店50次" },
            { "真·理财大师", "持有至少50个\u201c理财大师\u201d" },
            { "真·暗箱操作", "任意难度通关普通模式时至少有30个祝福的价格均为非正数" },
            { "真·卡牌大师", "任意难度通关普通模式时至少持有50张数字卡" },
            { "真·众神归位", "任意难度通关普通模式时至少持有100个祝福" },
            { "真·赌神传说", "任意难度通关普通模式时，你的所有数字卡均包含~20~" },
            { "感谢陪伴", "获得其它所有成就" },
        };

        List<AchievementData> list = new List<AchievementData>(table.GetLength(0));
        for (int i = 0; i < table.GetLength(0); i++)
        {
            list.Add(new AchievementData(i, table[i, 0], table[i, 1]));
        }
        return list;
    }
}
